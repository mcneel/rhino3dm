// JS parity runner: the generated bindings must reproduce the .NET goldens.
//
//   node tests/parity/parity_js.js <path/to/rhino3dm.js>
//
// Generated value types are plain {X, Y, Z} objects; behavior is
// rhino.Point3dX.DistanceTo(p, q), mutators return the new value. Cases that
// expect an exception are skipped: the wasm build has C++ exceptions off
// (a tracked JS policy question), so a throw would abort the module. The old
// JS bindings are not compared -- their Point3d is a bare array with no
// members.

const fs = require('fs');
const path = require('path');

const UNSET = -1.23432101234321e+308;
const FIELDS = { Point3d: ['X', 'Y', 'Z'], Vector3d: ['X', 'Y', 'Z'], Interval: ['T0', 'T1'] };
const OPS = { '+': 'op_Addition', '-': 'op_Subtraction', '*': 'op_Multiply', '/': 'op_Division',
              '==': 'op_Equality', '!=': 'op_Inequality', '<': 'op_LessThan', '>': 'op_GreaterThan',
              '<=': 'op_LessThanOrEqual', '>=': 'op_GreaterThanOrEqual' };

function decode(rhino, v) {
  if (typeof v === 'string') return { NaN: NaN, Inf: Infinity, '-Inf': -Infinity }[v];
  if (v !== null && typeof v === 'object') {
    const [[k, x]] = Object.entries(v);
    if (k === 'static') {
      const [tn, mn] = x.split('.');
      if (tn === 'RhinoMath' && mn === 'UnsetValue') return UNSET;
      return rhino[tn + 'X'][mn]();
    }
    const o = {};
    FIELDS[k].forEach((f, i) => { o[f] = decode(rhino, x[i]); });
    return o;
  }
  return v;
}

function encodeLike(want, got) {
  // JS objects carry no type tag: shape the result like the golden
  if (got === null || got === undefined) return null;
  if (typeof got === 'number')
    return Number.isNaN(got) ? 'NaN' : got === Infinity ? 'Inf' : got === -Infinity ? '-Inf' : got;
  if (typeof got === 'boolean') return got;
  if (want && typeof want === 'object' && !Array.isArray(want)) {
    const [k] = Object.keys(want);
    if (FIELDS[k]) return { [k]: FIELDS[k].map(f => encodeLike(null, got[f])) };
    const out = {};
    for (const key of Object.keys(want)) out[key] = encodeLike(want[key], got[key]);
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

function evaluate(rhino, c) {
  const NS = rhino[c.type + 'X'];
  let self = null;
  if (c.self !== undefined)
    self = Array.isArray(c.self)
      ? Object.fromEntries(FIELDS[c.type].map((f, i) => [f, decode(rhino, c.self[i])]))
      : NS[c.self.static]();
  if (c.op) {
    const a = c.args.map(x => decode(rhino, x));
    const name = a.length === 1 ? 'op_UnaryNegation' : OPS[c.op];
    // the operator lives on the C# type that declares it: try every operand type
    const owners = [c.type, ...c.args.map(x => (x && typeof x === 'object') ? Object.keys(x)[0] : null)];
    for (const t of owners) {
      const fn = t && rhino[t + 'X'] && rhino[t + 'X'][name];
      if (fn) { try { return fn(...a); } catch (e) { if (!/argument|parameter|expected/i.test(String(e))) throw e; } }
    }
    throw new Error('missing operator ' + name);
  }
  if (c.index !== undefined) throw new Error('missing indexer');
  const fields = FIELDS[c.type];
  if (!('args' in c)) {
    if (self && fields.includes(c.member)) return self[c.member];
    const fn = NS[c.member];
    if (!fn) throw new Error('missing ' + c.member);
    return self ? fn(self) : fn();
  }
  const fn = NS[c.member];
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
  let pass = 0, fail = 0, skipped = 0, missing = [];
  for (const f of fs.readdirSync(path.join(here, 'cases')).sort()) {
    const cases = JSON.parse(fs.readFileSync(path.join(here, 'cases', f))).cases;
    const golden = JSON.parse(fs.readFileSync(path.join(here, 'golden', f)));
    for (const c of cases) {
      const want = golden[c.id];
      if (want && want.throws) { skipped++; continue; }
      let got;
      try { got = encodeLike(want, evaluate(rhino, c)); }
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
  console.log(`parity vs .NET (js): generated ${pass}/${pass + fail} match, ${skipped} throw-cases skipped, ${missing.length} not reachable in JS`);
  for (const m of missing) console.log('  not reachable:', m);
  process.exit(fail ? 1 : 0);
}

main().catch(e => { console.error(e); process.exit(2); });
