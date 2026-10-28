// JS parity runner: the generated bindings must reproduce the .NET goldens.
//
//   node tests/parity/parity_js.js <path/to/rhino3dm.js>
//
// Generated value types are plain {X, Y, Z} objects; behavior is
// rhino.Point3dX.distanceTo(p, q) (camelCase: G106), mutators return the new value. Cases that
// expect an exception are skipped: the wasm build has C++ exceptions off
// (a tracked JS policy question), so a throw would abort the module. The old
// JS bindings are not compared -- their Point3d is a bare array with no
// members.

const fs = require('fs');
const path = require('path');

const UNSET = -1.23432101234321e+308;
const FIELDS = { Point3d: ['X', 'Y', 'Z'], Vector3d: ['X', 'Y', 'Z'], Interval: ['T0', 'T1'], Point2d: ['X', 'Y'],
                 Point3f: ['X', 'Y', 'Z'], Vector3f: ['X', 'Y', 'Z'], Point2f: ['X', 'Y'],
                 Line: ['From', 'To'], Plane: ['Origin', 'XAxis', 'YAxis'] };
const VALUE_TYPES = new Set(['Point2d', 'Point3d', 'Vector3d', 'Point3f', 'Vector3f', 'Point2f', 'Interval', 'Line', 'BoundingBox', 'Plane', 'Circle', 'Arc', 'Sphere', 'Transform']);
const OPS = { '+': 'op_Addition', '-': 'op_Subtraction', '*': 'op_Multiply', '/': 'op_Division',
              '==': 'op_Equality', '!=': 'op_Inequality', '<': 'op_LessThan', '>': 'op_GreaterThan',
              '<=': 'op_LessThanOrEqual', '>=': 'op_GreaterThanOrEqual' };

const SPECIAL = { NaN: NaN, Inf: Infinity, '-Inf': -Infinity };

// a value type's members in JS (G106): RhinoCommon's names in camelCase, operators by the
// RhinoCommon method that names them; != and the orderings through equals / compareTo
const JS_VT_OPS = { op_Addition: 'add', op_Subtraction: 'subtract', op_Multiply: 'multiply',
                    op_Division: 'divide', op_UnaryNegation: 'negate', op_Equality: 'equals' };
const JS_FN_OWN = new Set(['length', 'name', 'prototype', 'caller', 'arguments', 'apply', 'call', 'bind', 'toString']);
const jsVt = n => { const j = JS_VT_OPS[n] || (n[0].toLowerCase() + n.slice(1)); return JS_FN_OWN.has(j) ? n : j; };
const DERIVED_OPS = {
  op_Inequality: (T, a, b) => !T.equals(a, b),
  op_LessThan: (T, a, b) => T.compareTo(a, b) < 0, op_GreaterThan: (T, a, b) => T.compareTo(a, b) > 0,
  op_LessThanOrEqual: (T, a, b) => T.compareTo(a, b) <= 0, op_GreaterThanOrEqual: (T, a, b) => T.compareTo(a, b) >= 0,
};

function decode(rhino, v) {
  if (typeof v === 'string') return v in SPECIAL ? SPECIAL[v] : v;
  if (v !== null && typeof v === 'object' && 'new' in v) {
    // {"new": T, "args": [...]}: a value type through its RhinoCommon constructor (`create`), a
    // class (G113: a DimensionStyle argument) through `new`
    const C = rhino[v.new + 'X'], a = v.args.map(x => decode(rhino, x));
    return typeof C.create === 'function' ? C.create(...a) : new C(...a);
  }
  if (v !== null && typeof v === 'object') {
    const [[k, x]] = Object.entries(v);
    if (k === 'enum') { const [tn, vn] = x.split('.'); return rhino[tn][vn]; }
    if (k === 'Color') return { r: x[0], g: x[1], b: x[2], a: x[3] };
    if (k === 'static') {
      const [tn, mn] = x.split('.');
      if (tn === 'RhinoMath' && mn === 'UnsetValue') return UNSET;
      const sv = rhino[tn + 'X'][jsVt(mn)];      // a static property is a getter (G101); camelCase (G106)
      return typeof sv === 'function' ? sv() : sv;
    }
    const o = {};
    FIELDS[k].forEach((f, i) => { o[f] = decode(rhino, x[i]); });
    if (k === 'Plane') {        // RhinoCommon's Plane(origin, x, y): z = x cross y (cases use orthonormal axes)
      const a = o.XAxis, b = o.YAxis;
      o.ZAxis = { X: a.Y * b.Z - a.Z * b.Y, Y: a.Z * b.X - a.X * b.Z, Z: a.X * b.Y - a.Y * b.X };
    }
    return o;
  }
  return v;
}

const ENUMS = ['Continuity', 'IsoStatus', 'TransformSimilarityType', 'TransformRigidType', 'CoordinateSystem'];

function encodeLike(want, got, rhino) {
  // JS objects carry no type tag: shape the result like the golden
  if (got === null || got === undefined) return null;
  if (Array.isArray(want)) {
    const g = Array.isArray(got) ? got : [got];
    return g.map((x, i) => encodeLike(want[i], x, rhino));
  }
  if (want && typeof want === 'object' && 'class' in want) {
    const n = got && got.$$ && got.$$.ptrType && got.$$.ptrType.registeredClass.name;
    return { class: n ? n.replace(/X$/, '') : String(got) };
  }
  if (want && typeof want === 'object' && 'enum' in want) {
    for (const en of ENUMS) for (const [k, v] of Object.entries(rhino[en] || {}))
      if (v === got) return { enum: k };
    // any other registered enum (generated RH3DM_ enums included)
    for (const en of Object.keys(rhino)) {
      const e = rhino[en];
      if (e && typeof e === 'function' && e.values) for (const [k, v] of Object.entries(e))
        if (v === got) return { enum: k };
    }
    return { enum: String(got) };
  }
  if (want && typeof want === 'object' && 'Plane' in want)
    return got.Origin   // generated PlaneX value object vs old JS SimplePlane
      ? { Plane: [got.Origin.X, got.Origin.Y, got.Origin.Z, got.XAxis.X, got.XAxis.Y, got.XAxis.Z, got.YAxis.X, got.YAxis.Y, got.YAxis.Z] }
      : { Plane: [...got.origin, ...got.xAxis, ...got.yAxis] };
  if (want && typeof want === 'object' && 'Color' in want)
    return { Color: [got.r, got.g, got.b, got.a] };
  if (want && typeof want === 'object' && 'Arc' in want) {   // plane, radius, angle domain (G113)
    const pl = got.Plane, d = got.AngleDomain;
    return { Arc: [pl.Origin.X, pl.Origin.Y, pl.Origin.Z, pl.XAxis.X, pl.XAxis.Y, pl.XAxis.Z, pl.YAxis.X, pl.YAxis.Y, pl.YAxis.Z, got.Radius, d.T0, d.T1] };
  }
  if (want && typeof want === 'object' && 'Line' in want)
    return { Line: [got.From.X, got.From.Y, got.From.Z, got.To.X, got.To.Y, got.To.Z] };
  if (want && typeof want === 'object' && 'BoundingBox' in want)
    return { BoundingBox: [got.Min.X, got.Min.Y, got.Min.Z, got.Max.X, got.Max.Y, got.Max.Z] };
  if (want && typeof want === 'object' && 'Transform' in want)
    return { Transform: Array.from({ length: 16 }, (_, i) => {
      const k = Math.floor(i / 4) + '' + (i % 4);
      return 'M' + k in got ? got['M' + k] : got['m' + k];     // generated value object / old class
    }) };
  if (typeof got === 'number')
    return Number.isNaN(got) ? 'NaN' : got === Infinity ? 'Inf' : got === -Infinity ? '-Inf' : got;
  if (typeof got === 'boolean') return got;
  if (want && typeof want === 'object' && !Array.isArray(want)) {
    const [k] = Object.keys(want);
    if (FIELDS[k]) return { [k]: FIELDS[k].map(f => encodeLike(null, got[f])) };
    const out = {};
    for (const key of Object.keys(want)) out[key] = encodeLike(want[key], got[key], rhino);
    return out;
  }
  return got;
}

function same(a, b) {
  if (a === b) return true;
  if (typeof a === 'object' && a && b && typeof b === 'object')
    return JSON.stringify(Object.keys(a)) === JSON.stringify(Object.keys(b))
      && Object.keys(a).every(k => same(a[k], b[k]));
  return false;
}

const ALIAS = { ViewportInfoX: 'ViewportX' };
const camel = s => s[0].toLowerCase() + s.slice(1);

// class cases: the object is the oracle's serialized FIXTURE, decoded with the
// old JS API and wrapped into the generated class (bridge-free classes are
// default-constructed, matching the oracle's default object)
function fixtureObject(rhino, c, fixtures) {
  const name = ALIAS[c.type + 'X'] || (c.type + 'X');
  const Gen = rhino[name];
  if (!Gen) throw new Error('missing class ' + name);
  const fx = fixtures[c.self];
  let hand;
  if (fx.Transform) {
    hand = new rhino.Transform(1.0);
    fx.Transform.forEach((m, i) => { hand['m' + Math.floor(i / 4) + (i % 4)] = m; });
  } else {
    hand = rhino.CommonObject.decode(fx);
  }
  try { return new Gen(hand); } catch (e) { return new Gen(); }
}

// `"own": true` (G108): the case's object is built with the GENERATED classes from its recipe -- a class
// with no old-binding bridge or serializable fixture (DracoCompression) -- RhinoCommon names, camelCase
function ownRecipe(rhino, v, recipes) {
  if (typeof v === 'string') v = recipes[v];
  const arg = a => (a && typeof a === 'object' && !Array.isArray(a) && ('call' in a || 'new' in a)) ? ownRecipe(rhino, a, recipes)
    : (a && typeof a === 'object' && 'Point3dList' in a) ? a.Point3dList.map(p => ({ X: p[0], Y: p[1], Z: p[2] }))
    : decode(rhino, a);
  const args = (v.args || []).map(arg);
  if ('new' in v) return new rhino[v.new + 'X'](...args);
  if ('on' in v) return ownRecipe(rhino, v.on, recipes)[camel(v.call)](...args);
  const dot = v.call.lastIndexOf('.');
  const T = rhino[v.call.slice(0, dot) + 'X'];
  return T[camel(v.call.slice(dot + 1))](...args);
}

function evaluateClass(rhino, c, fixtures, recipes) {
  if (c.own) {
    const self = ownRecipe(rhino, c.self, recipes);
    const own = a => (a && typeof a === 'object' && !Array.isArray(a) && ('call' in a || 'new' in a)) ? ownRecipe(rhino, a, recipes) : decode(rhino, a);
    const mem = self[camel(c.member)];
    if (mem === undefined) throw new Error('missing ' + c.member);
    return typeof mem === 'function' ? mem.apply(self, (c.args || []).map(own)) : mem;
  }
  if (c.self === undefined) {
    // a static member (Intersection.LineLine): call it on the class
    const cls = rhino[(ALIAS[c.type + 'X'] || (c.type + 'X'))];
    const fn = cls && cls[camel(c.member)];
    if (!fn) throw new Error('missing ' + c.member);
    return fn(...(c.args || []).map(x => decode(rhino, x)));
  }
  if (c.via) {
    const parent = fixtureObject(rhino, c, fixtures);
    const view = parent[camel(c.via)];
    if (view === undefined) throw new Error('missing ' + c.via);
    if (c.index !== undefined) return view.get(c.index);
    const mem = view[camel(c.member)];
    if (mem === undefined) throw new Error('missing ' + c.via + '.' + c.member);
    return typeof mem === 'function' ? mem.apply(view, (c.args || []).map(x => decode(rhino, x))) : mem;
  }
  if (VALUE_TYPES.has(c.type)) {
    // a value type from a fixture (Transform: 16 numbers -> {M00..M33})
    const fx = fixtures[c.self];
    const self = Object.fromEntries(fx.Transform.map((m, i) => ['M' + Math.floor(i / 4) + (i % 4), m]));
    return evaluate(rhino, Object.assign({}, c, { self: undefined }), self);
  }
  const obj = fixtureObject(rhino, c, fixtures);
  const name = camel(c.member);
  const member = obj[name];
  if (member === undefined) throw new Error('missing ' + name);
  if ('set' in c) {
    obj[name] = decode(rhino, c.set);
    if (obj[name] === member && typeof member !== 'object') throw new Error('missing setter ' + name);
    const t = obj[camel(c.then)];
    return { then: typeof t === 'function' ? t.call(obj) : t };
  }
  let ret;
  if (typeof member === 'function') {
    try { ret = member.apply(obj, (c.args || []).map(x => decode(rhino, x))); }
    catch (e) { if (/argument|parameter|expected|function/i.test(String(e))) throw new Error('missing overload ' + name + ': ' + String(e).slice(0, 60)); throw e; }
  } else {
    ret = member;
  }
  if (c.then) {
    const t = obj[camel(c.then)];
    return { returns: ret, then: typeof t === 'function' ? t.call(obj) : t };
  }
  return ret;
}

function evaluate(rhino, c, prebuilt) {
  const NS = rhino[c.type + 'X'];
  let self = prebuilt || null;
  if (c.self !== undefined)
    self = Array.isArray(c.self)
      ? Object.fromEntries(FIELDS[c.type].map((f, i) => [f, decode(rhino, c.self[i])]))
      : c.self.new
        ? rhino[c.self.new + 'X'].create(...c.self.args.map(x => decode(rhino, x)))   // RhinoCommon ctor
        : (typeof NS[jsVt(c.self.static)] === 'function' ? NS[jsVt(c.self.static)]() : NS[jsVt(c.self.static)]);
  if (c.op) {
    const a = c.args.map(x => decode(rhino, x));
    const name = a.length === 1 ? 'op_UnaryNegation' : OPS[c.op];
    // the operator lives on the C# type that declares it: try every operand type
    const owners = [c.type, ...c.args.map(x => (x && typeof x === 'object') ? Object.keys(x)[0] : null)];
    for (const t of owners) {
      const T = t && rhino[t + 'X'];
      if (T && DERIVED_OPS[name] && typeof T.compareTo === 'function') {
        try { return DERIVED_OPS[name](T, ...a); } catch (e) { if (!/argument|parameter|expected/i.test(String(e))) throw e; }
      }
      const fn = T && T[jsVt(name)];
      if (fn) { try { return fn(...a); } catch (e) { if (!/argument|parameter|expected/i.test(String(e))) throw e; } }
    }
    throw new Error('missing operator ' + name);
  }
  if (c.index !== undefined) throw new Error('missing indexer');
  if (!('args' in c)) {
    // field-backed properties are plain fields of the value object
    if (self && typeof self === 'object' && c.member in self) return self[c.member];
    const fn = NS[jsVt(c.member)];
    if (fn === undefined) throw new Error('missing ' + c.member);
    if (typeof fn !== 'function') return fn;            // a static property: a getter (G101)
    return self ? fn(self) : fn();
  }
  const fn = NS[jsVt(c.member)];
  if (!fn) throw new Error('missing ' + c.member);
  const args = c.args.map(x => decode(rhino, x));
  const ret = self ? fn(self, ...args) : fn(...args);
  // mutators: void ones return the new value; ones that also return
  // something return {self, returns} (generator-log G6)
  if (c.mutates) return (ret && typeof ret === 'object' && 'self' in ret) ? ret : { self: ret, returns: null };
  return ret;
}

async function main() {
  const factory = require(path.resolve(process.argv[2]));
  const rhino = await factory();
  const here = __dirname;
  let pass = 0, fail = 0, skipped = 0, missing = [], pending = [];
  for (const f of fs.readdirSync(path.join(here, 'cases')).sort()) {
    const doc = JSON.parse(fs.readFileSync(path.join(here, 'cases', f)));
    const cases = doc.cases;
    const golden = JSON.parse(fs.readFileSync(path.join(here, 'golden', f)));
    const fixtures = golden.__fixtures__ || {};
    for (const c of cases) {
      if (!(c.id in golden)) { pending.push(c.id); continue; }   // the oracle has not run it yet
      const want = golden[c.id];
      if (want && want.throws) { skipped++; continue; }
      if (c.construct) { skipped++; continue; }     // construction tests the old API: python only
      let got;
      try { got = encodeLike(want, doc.recipes ? evaluateClass(rhino, c, fixtures, doc.recipes) : evaluate(rhino, c), rhino); }
      catch (e) {
        if (/^Error: missing/.test(String(e))) { missing.push(c.id + ' (' + String(e).slice(7) + ')'); continue; }
        got = { error: String(e) };
      }
      // mutators: JS returns the new value; the C# return value is not compared
      if (c.mutates && want && want.returns === null) { got.returns = null; }
      if (same(want, got)) pass++;
      else { fail++; console.log('  GENERATED MISMATCH', c.id, JSON.stringify(want), JSON.stringify(got)); }
    }
  }
  console.log(`parity vs .NET (js): generated ${pass}/${pass + fail} match, ${skipped} skipped (throw / construction cases), ${missing.length} not reachable in JS`);
  for (const m of missing) console.log('  not reachable:', m);
  if (pending.length) console.log('  pending (no golden yet; run the oracle):', pending.join(', '));
  process.exit(fail ? 1 : 0);
}

main().catch(e => { console.error(e); process.exit(2); });
