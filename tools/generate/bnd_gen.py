"""Pilot binding generator v3: registry-driven, multi-class.

v2 proved the pipeline on Curve but was Curve-shaped throughout. v3 moves
everything class-specific into CLASSES, the wrapper-type registry:

    C# type <-> ON_* type <-> hand BND class (copy source) <-> generated class

One generated file defines every class (in registry order, so cross-class
returns like Surface.IsoCurve -> Curve see a complete type) and registers
them all through a single initGeneratedBindings entry point.

Everything else is unchanged from v2: C# signatures shape the API, Calls[]
maps parameters onto the C call, guards/success patterns reproduce the C#
contract, and anything unrecognized is skipped with a counted reason.
"""

import glob
import json
import os
import re
import sys

from gen_registry import *
from planner import *
from emit_classes import *
from emit_values import *
from emit_hand import hand_class_registrations, hand_js_aliases, hand_js_post


def _derives(d, base):
    b = BY_REG.get(d.get('base', ''))
    while b is not None:
        if b is base:
            return True
        b = BY_REG.get(b.get('base', ''))
    return False


def scope_pass(per_class, manifest, c_by_name, skipped):
    """G77: a scoped child view (BrepLoopList: the brep's loops, or one face's) plans
    its members a second time from the bodies specialized for the scoping constructor;
    a member whose body reads the scope field dispatches on m_scope (scope_plan), or
    refuses at run time on a scoped view when that variant is not generated."""
    def sig(p):
        return (p['m']['Kind'], p['m']['Name'], tuple(q['Type'] for q in p['m'].get('Params') or []), bool(p.get('setter_of')))
    for cls, plans in per_class:
        sc = cls.get('scope')
        if not sc:
            continue
        scls = dict(cls, ctor_key=sc['key'], scoped=True)
        sk = {}
        splans = plan_class(scls, {'members': class_members(scls, manifest)}, c_by_name, sk)
        by = {sig(q): q for q in splans}
        for p in plans:
            if sc['key'] not in (p['m'].get('Specialized') or {}):
                continue
            q = by.get(sig(p))
            if q is not None and q['bind_args'] == p['bind_args'] and q['shape'][0] == p['shape'][0]:
                p['scope_plan'] = q
            else:
                p['scope_plan'] = False
                skipped['scope: variant not generated'] = skipped.get('scope: variant not generated', 0) + 1


def _writers_planned(per_class):
    return {(p['m']['Type'], p['m']['Name']) for _, plans in per_class for p in plans}


def data_marker_pass(vt, per_class, manifest, skipped):
    """a data class member naming a generated class (`new PolylineCurve(this)`;
    G82) carries class-mode markers: resolved against what was generated, or
    the member is refused -- and so is every member that depends on it"""
    per_reg = {cls['reg']: plans for cls, plans in per_class}
    data = {t['Cs'] for t in vt['types'] if t.get('IsData')}
    dropped = set()
    for m in vt['members']:
        if not m.get('Ok') or m['Type'] not in data or '$$' not in (m.get('Decl') or '') + (m.get('Def') or ''):
            continue
        d, dwhy, _ = resolve_markers(m['Decl'], None, per_reg, manifest)
        f, fwhy, _ = resolve_markers(m['Def'], None, per_reg, manifest)
        rt, rwhy, _ = resolve_markers(m.get('Ret') or '', None, per_reg, manifest)
        pts = [resolve_markers(q['Type'], None, per_reg, manifest) for q in m.get('Params') or []]
        if d is None or f is None or rt is None or any(x[0] is None for x in pts):
            m['Ok'], m['Reason'] = False, 'marker: %s' % (dwhy if d is None else fwhy if f is None else rwhy if rt is None
                                                          else next(x[1] for x in pts if x[0] is None))
            dropped.update(m.get('Keys') or [])
            skipped['data member: marker'] = skipped.get('data member: marker', 0) + 1
            continue
        m['Decl'], m['Def'], m['Ret'] = d, f, rt
        for q, x in zip(m.get('Params') or [], pts):
            q['Type'] = x[0]
        m['ClassDeps'] = sorted({g[4:] for g in re.findall(r'\bBND_\w+\b', f + d)} & {c['reg'] for c in CLASSES})
    while dropped:
        more = set()
        for m in vt['members']:
            if m.get('Ok') and any(r in dropped for r in m.get('Refs') or []):
                m['Ok'], m['Reason'] = False, 'depends on a refused member'
                more.update(m.get('Keys') or [])
                skipped['data member: marker'] = skipped.get('data member: marker', 0) + 1
        dropped = more


def managed_null_pass(per_class, manifest, c_by_name, skipped):
    """G75: a class whose RhinoCommon type has MANAGED fields (written only by members,
    specgen ManagedFields) re-plans with its members' `(managed-null)` analyses when
    no writer of those fields is generated: on every generated object they are null.
    Checked again after the re-plan; a generated writer would make this unsound."""
    planned = _writers_planned(per_class)
    for i, (cls, plans) in enumerate(per_class):
        mf = (TYPE_RECS.get(cls['type']) or {}).get('ManagedFields') or {}
        if not mf or cls.get('null_key'):
            continue
        if set(mf) <= set(cls.get('modeled') or {}):
            # every managed field is modeled (G88): the members' own analyses, the
            # fields resolved to the generated members -- never the null specialization
            cls['mf_active'] = True
            plans = plan_class(cls, {'members': class_members(cls, manifest)}, c_by_name, skipped)
            plans += modeled_plans(cls, plans, manifest)
            PLANNED[cls['reg']] = plans
            per_class[i] = (cls, plans)
            continue
        writers = {tuple(w.rsplit('.', 1)) for ws in mf.values() for w in ws}
        if writers & planned:
            skipped['managed fields: writer generated'] = skipped.get('managed fields: writer generated', 0) + 1
            continue
        if not any('(managed-null)' in (x.get('Specialized') or {}) for x in class_members(cls, manifest)):
            continue
        cls['null_key'] = '(managed-null)'
        plans = plan_class(cls, {'members': class_members(cls, manifest)}, c_by_name, skipped)
        PLANNED[cls['reg']] = plans
        per_class[i] = (cls, plans)
    planned = _writers_planned(per_class)
    for cls, _ in per_class:
        if cls.get('null_key'):
            mf = (TYPE_RECS.get(cls['type']) or {}).get('ManagedFields') or {}
            bad = {tuple(w.rsplit('.', 1)) for ws in mf.values() for w in ws} & planned
            if bad:
                raise SystemExit('%s: managed-field writer generated %s -- the (managed-null) analyses are unsound' % (cls['reg'], sorted(bad)))


def refuse_overridden(manifest, per_class, skipped):
    """A base member that a derived generated class OVERRIDES in RhinoCommon
    with logic the generator does not produce is refused on the base (G49:
    GeometryBase.GetBoundingBox(Transform) vs AnnotationBase's managed
    override): C++ inheritance would hand derived objects the base logic.
    Exact signatures; a `base` forward or a generated override keeps it.
    check_override_shadowing stays the final assertion."""
    def sig(x):
        return (x['Kind'], x['Name'], tuple(q['Type'] for q in x.get('Params') or []))
    def acc(p):
        return 'set' if p.get('setter_of') else 'get'
    by_type = {}
    for x in manifest['members']:
        if x['Variant'] == 'portable' and x['Kind'] in ('method', 'property'):
            by_type.setdefault(x['Type'], []).append(x)
    plans_of = {cls['reg']: plans for cls, plans in per_class}
    changed = True
    while changed:
        changed = False
        gen = {reg: {(acc(p), sig(p['m'])) for p in plans} for reg, plans in plans_of.items()}
        for cls, _ in per_class:
            own = {sig(x): x for x in by_type.get(cls['type'], [])}
            b = BY_REG.get(cls.get('base', ''))
            while b is not None:
                bplans = plans_of.get(b['reg'], [])
                for p in list(bplans):
                    if p['shape'][0] == 'forward':
                        continue
                    o = own.get(sig(p['m']))
                    if o is None or (acc(p), sig(p['m'])) in gen[cls['reg']]:
                        continue
                    body = o.get('Body') if acc(p) == 'get' else (o.get('Setter') or {}).get('Body')
                    if body == 'base':
                        continue
                    gone = [q for q in bplans if q is p or (q['shape'][0] == 'forward' and q['shape'][2] is p)]
                    for q in gone:
                        bplans.remove(q)
                    key = 'overridden in a derived class'
                    skipped[key] = skipped.get(key, 0) + len(gone)
                    changed = True
                b = BY_REG.get(b.get('base', ''))


def refuse_overridden_count(per_class, skipped):
    """An array fill sized by a sibling (`SpanCount(direction) + 1`) calls the
    generated sibling non-virtually; if a derived generated class has its own
    member of that name, C# would dispatch to it: refused (G54)."""
    for cls, plans in per_class:
        for p in list(plans):
            if p['shape'][0] != 'arrayfill':
                continue
            af = p['shape'][1]
            name = af.get('count_prop') or (af.get('count_method') or (None,))[0]
            if name and any(_derives(d, cls) and any(q['m']['Name'] == name for q in dplans)
                            for d, dplans in per_class):
                plans.remove(p)
                skipped['arrayfill: count overridden'] = skipped.get('arrayfill: count overridden', 0) + 1


def check_override_shadowing(manifest, per_class):
    """A derived class inherits its generated base's members through C++. If
    RhinoCommon OVERRIDES one in the derived class and that override is not
    generated, Python/JS would silently run the BASE logic on derived objects.
    Allowed only when the override is a pure forward to `base` (specgen
    Body "base"); anything else stops generation with the list."""
    names = {cls['reg']: {('set ' if p.get('setter_of') else '') + p['m']['Name'] for p in plans}
             for cls, plans in per_class}
    def sig(x):
        return (x['Kind'], x['Name'], tuple(q['Type'] for q in x.get('Params') or []))
    by_type = {}
    for x in manifest['members']:
        if x['Variant'] == 'portable' and x['Kind'] in ('method', 'property'):
            by_type.setdefault(x['Type'], []).append(x)
    bad = []
    for cls, _ in per_class:
        own = {sig(x): x for x in by_type.get(cls['type'], [])}
        b = BY_REG.get(cls.get('base', ''))
        while b is not None:
            for x in by_type.get(b['type'], []):
                o = own.get(sig(x))
                if o is None:
                    continue
                for acc, prefix in (('get', ''), ('set', 'set ')):
                    if prefix + x['Name'] in names[b['reg']] and prefix + x['Name'] not in names[cls['reg']]:
                        body = o.get('Body') if acc == 'get' else (o.get('Setter') or {}).get('Body')
                        if body != 'base':
                            bad.append('%s.%s%s (%s overrides %s: %s)' % (cls['reg'], prefix, x['Name'], cls['cs'], b['cs'], body))
            b = BY_REG.get(b.get('base', ''))
    if bad:
        raise SystemExit('override shadowed by an inherited generated member:\n  ' + '\n  '.join(bad))


def js_pre_script():
    """src/generated/gen_pre.js (linked with --pre-js; G101, pilot #47): embind converts an enum
    argument with `c => c.value` (a number, string or {} became an undefined / 0 enum) and BINDS that
    conversion into every function's invoker as the function registers -- so the check must be in
    place when the enum type itself registers, before the wasm runs its registrations. This wraps
    embind's registerType (a module-scope function, called by name) and gives each "object" enum
    type an instanceof check: one comparison per argument, no extra call. gen_post.js verifies every
    enum type carries it (Module.rh3dmPostUnresolved)."""
    return '''// GENERATED by tools/generate/bnd_gen.py -- DO NOT EDIT. Linked with --pre-js (G101).
if (typeof registerType === 'function') {
  registerType = (function (orig) {
    return function (rawType, info, options) {
      if (info && info.valueType === 'object' && Object.prototype.hasOwnProperty.call(info, 'constructor')
          && typeof info.toWireType === 'function') {
        var E = info.constructor, name = info.name;
        var checked = function (destructors, c) {
          if (c instanceof E) return c.value;
          throw new TypeError('expected a ' + name + ' value, got ' + (c === null ? 'null' : typeof c));
        };
        checked.rh3dmChecked = true;
        info.toWireType = checked;
      }
      return orig.apply(this, arguments);
    };
  })(registerType);
}
'''


def js_post_script():
    """src/generated/gen_post.js (linked with --post-js; G101): what embind cannot say, installed on
    the registered classes once the runtime is up -- views hold their parent (Python's keep_alive),
    RhinoCommon static properties are getters, IEnumerable classes are iterable (for...of). Every
    entry that does not resolve is recorded in Module.rh3dmPostUnresolved (a test asserts it empty)"""
    j = lambda v: json.dumps(sorted(set(v)), separators=(',', ':'))
    return '''// GENERATED by tools/generate/bnd_gen.py -- DO NOT EDIT. Linked with --post-js (G101).
(function () {
  var KEEP = %s;
  var ITERS = %s;
  var STATIC_PROPS = %s;
  var ENUMERABLE = %s;
  var GRID = %s;
  var KEEP_ARGS = %s;
  var KEEP_SET = %s;
  var ALIASES = %s;
  function install() {
    var M = Module, missing = [];
    M.rh3dmPostUnresolved = missing;
    function hold(r, parent) {   // the result keeps its parent alive (a JS reference: GC-safe)
      if (r !== null && typeof r === 'object') {
        try { Object.defineProperty(r, '__rh3dm_parent', { value: parent, configurable: true }); } catch (e) {}
      }
      return r;
    }
    // enum arguments are type-checked (G101, pilot #47; gen_pre.js installs the check as each
    // enum type registers): every enum type must carry it, else it is reported
    if (typeof registeredTypes !== 'object' || registeredTypes === null) missing.push('enum registry');
    else {
      var nEnums = 0;
      Object.keys(registeredTypes).forEach(function (k) {
        var t = registeredTypes[k];
        if (!t || t.valueType !== 'object' || !Object.prototype.hasOwnProperty.call(t, 'constructor')) return;
        nEnums++;
        if (!t.toWireType || !t.toWireType.rh3dmChecked) missing.push('enum check ' + t.name);
      });
      if (nEnums === 0) missing.push('enum types');
    }
    KEEP.forEach(function (km) {
      var C = M[km[0]], proto = C && C.prototype;
      var d = proto && Object.getOwnPropertyDescriptor(proto, km[1]);
      if (!d) { missing.push('keep ' + km[0] + '.' + km[1]); return; }
      if (d.get) {
        var g = d.get;
        Object.defineProperty(proto, km[1], { get: function () { return hold(g.call(this), this); }, set: d.set,
                                              enumerable: d.enumerable, configurable: true });
      } else if (typeof d.value === 'function') {
        var f = d.value;
        // embind's arity dispatcher reads proto[name].overloadTable: the wrapper carries it
        var w = function () { return hold(f.apply(this, arguments), this); };
        if (f.overloadTable) w.overloadTable = f.overloadTable;
        proto[km[1]] = w;
      } else missing.push('keep ' + km[0] + '.' + km[1]);
    });
    // G112: a result that keeps an ARGUMENT alive (a static Create storing its style in a modeled
    // field) and a borrowing setter's value, held as JS references (GC-safe)
    function keep(o, key, v) {
      if (o !== null && typeof o === 'object' && v !== null && typeof v === 'object') {
        try { Object.defineProperty(o, key, { value: v, configurable: true }); } catch (e) {}
      }
    }
    KEEP_ARGS.forEach(function (ka) {
      var C = M[ka[0]], target = ka[3] ? C : (C && C.prototype), f = target && target[ka[1]];
      if (typeof f !== 'function') { missing.push('keep arg ' + ka[0] + '.' + ka[1]); return; }
      var w = function () { var r = f.apply(this, arguments); keep(r, '__rh3dm_arg' + ka[2], arguments[ka[2]]); return r; };
      if (f.overloadTable) w.overloadTable = f.overloadTable;
      if (f.argCount !== undefined) w.argCount = f.argCount;
      target[ka[1]] = w;
    });
    KEEP_SET.forEach(function (ks) {
      var C = M[ks[0]], proto = C && C.prototype;
      var d = proto && Object.getOwnPropertyDescriptor(proto, ks[1]);
      if (!d || !d.set) { missing.push('keep set ' + ks[0] + '.' + ks[1]); return; }
      var s = d.set, g = Object.getOwnPropertyDescriptor(proto, ks[1]).get;
      Object.defineProperty(proto, ks[1], { get: g, set: function (v) { s.call(this, v); keep(this, '__rh3dm_set_' + ks[1], v); },
                                            enumerable: d.enumerable, configurable: true });
    });
    // G124: JS-only names calling another method (File3dm.destroy -> embind's delete)
    ALIASES.forEach(function (a) {
      var C = M[a[0]], proto = C && C.prototype;
      if (!proto || typeof proto[a[2]] !== 'function') { missing.push('alias ' + a[0] + '.' + a[1]); return; }
      proto[a[1]] = function () { return this[a[2]].apply(this, arguments); };
    });
    ITERS.forEach(function (c) {
      var C = M[c], proto = C && C.prototype, f = proto && proto.next;
      if (typeof f !== 'function') { missing.push('iter ' + c); return; }
      var w = function () { var r = f.apply(this, arguments); if (r && r.value) hold(r.value, this); return r; };
      if (f.overloadTable) w.overloadTable = f.overloadTable;
      proto.next = w;
      proto[Symbol.iterator] = function () { return this; };
    });
    STATIC_PROPS.forEach(function (sp) {
      var C = M[sp[0]], f = C && C[sp[1]];
      if (typeof f !== 'function') { missing.push('static ' + sp[0] + '.' + sp[1]); return; }
      // an arity dispatcher would read C[name] -- the getter itself: call its zero-argument entry
      if (f.overloadTable) f = f.overloadTable[0];
      if (typeof f !== 'function') { missing.push('static ' + sp[0] + '.' + sp[1]); return; }
      Object.defineProperty(C, sp[1], { get: function () { return f.call(C); }, configurable: true, enumerable: true });
    });
    // RhinoCommon's grid enumerators (G102: NurbsSrfEnum): every (u, v), u outermost
    GRID.forEach(function (g) {
      var C = M[g[0]], proto = C && C.prototype;
      if (!proto || !(g[1] in proto) || !(g[2] in proto) || typeof proto[g[3]] !== 'function') { missing.push('grid ' + g[0]); return; }
      proto.iterator = function () {
        var self = this, nu = self[g[1]], nv = self[g[2]], p = 0;
        return { next: function () { return p < nu * nv ? { done: false, value: self[g[3]](Math.floor(p / nv), p++ %% nv) } : { done: true }; },
                 [Symbol.iterator]: function () { return this; } };
      };
      proto[Symbol.iterator] = function () { return this.iterator(); };
    });
    ENUMERABLE.forEach(function (c) {
      var C = M[c], proto = C && C.prototype;
      if (!proto) { missing.push('enumerable ' + c); return; }
      if (typeof proto.iterator !== 'function') {
        // a list with count and an indexer (RhinoCommon's TableEnumerator): iterate them
        if (!('count' in proto) || typeof proto.get !== 'function') { missing.push('enumerable ' + c); return; }
        proto.iterator = function () {
          var self = this, i = 0;
          return { next: function () { return i < self.count ? { done: false, value: hold(self.get(i++), self) } : { done: true }; },
                   [Symbol.iterator]: function () { return this; } };
        };
      }
      proto[Symbol.iterator] = function () { return this.iterator(); };
    });
  }
  // emscripten's modularized output calls run() BEFORE the post-js code: the runtime is usually
  // up already, and the module promise's consumers have not seen Module yet (they resume later)
  if (Module.calledRun) install();
  else if (typeof addOnPostRun === 'function') addOnPostRun(install);
  else Module.postRun = [].concat(Module.postRun || [], [install]);
})();
''' % (j(JS_POST['keep']), j(JS_POST['iters']), j(JS_POST['static_props']), j(JS_POST['enumerable']),
       json.dumps(sorted(set(map(tuple, JS_POST['grid']))), separators=(',', ':')),
       json.dumps(sorted(set(map(tuple, JS_POST['keep_args']))), separators=(',', ':')),
       json.dumps(sorted(set(map(tuple, JS_POST['keep_set']))), separators=(',', ':')),
       json.dumps(sorted(set(map(tuple, JS_POST['aliases']))), separators=(',', ':')))


def hand_chain(cls):
    """cls and its bases: JS re-registers a base's class functions on each derived class (G123:
    CommonObject.decode on every class), so a class file includes its bases' hand headers"""
    out, c = [], cls
    while c:
        out.append(c['reg'])
        c = BY_REG.get(c.get('base') or '')
    return out


def gen_stem(cs):
    """the generated file stem for RhinoCommon type `cs` (M3 layout): bnd_mesh_gen"""
    return 'bnd_%s_gen' % cs.lower()


# the generated files that are not one type's
GEN_FIXED_NAMES = {'gen_common.h': 'bnd_common_gen.h', 'gen_vt_common.h': 'bnd_values_gen.h',
                   'gen_init.cpp': 'bnd_init_gen.cpp', 'gen_wrap.cpp': 'bnd_wrap_gen.cpp',
                   'gen_pre.js': 'bnd_pre_gen.js', 'gen_post.js': 'bnd_post_gen.js',
                   'clayer_sources.cmake': 'clayer_sources_gen.cmake'}


def gen_layout(files, vt_types):
    """the M3 layout (Luis 2026-11-01): {emitted name: content} -> {bnd_<cs>_gen.*: content}, the
    includes between them rewritten, and `../bindings/x.h` (a hand file, same directory) as `x.h`.
    The emitters keep their working names (gen_meshx.h); this is the one place that maps them"""
    stems = {'gen_%s' % c['reg'].lower(): c['cs'] for c in CLASSES}
    stems.update((vt_file(t), t['Cs']) for t in vt_types)
    ren = {}
    for name in files:
        stem, ext = os.path.splitext(name)
        if name in GEN_FIXED_NAMES:
            ren[name] = GEN_FIXED_NAMES[name]
        elif stem in stems:
            ren[name] = gen_stem(stems[stem]) + ext
        else:
            raise SystemExit('layout: no RhinoCommon type for the generated file %s' % name)
    if len(set(ren.values())) != len(ren):
        dup = sorted(v for v in set(ren.values()) if list(ren.values()).count(v) > 1)
        raise SystemExit('layout: two generated files map to %s' % dup)

    def include(m):
        p = m.group(1)
        if p.startswith('../bindings/'):
            p = p[len('../bindings/'):]
        return '#include "%s"' % ren.get(p, p)
    return {ren[n]: re.sub(r'#include "([^"]+)"', include, c) for n, c in files.items()}


def main():
    for k in JS_POST:
        JS_POST[k].clear()
    # RH3DM_MANIFEST: dry runs against a candidate manifest (with RH3DM_GEN_OUT)
    manifest = with_system_enums(json.load(open(os.environ.get('RH3DM_MANIFEST') or os.path.join(ROOT, 'api', 'manifest.json'))))
    c_by_name = {f['name']: f for f in manifest['c_surface']}
    C_BY_NAME.clear(); C_BY_NAME.update(c_by_name)
    GUARD_ENUMS.clear()
    # rhcommon_c.h declares some exports with opaque handle types
    # (ON_LineCurveImpl*) while the definitions use the real ones
    # (ON_LineCurve*); same C symbol. Generate against the real types.
    unimpl = lambda t: re.sub(r'\b(ON_\w+?)Impl\*', r'\1*', t or '')
    for f in c_by_name.values():
        f['returns'] = unimpl(f['returns'])
        for q in f['params']:
            q['type'] = unimpl(q['type'])

    skipped = {}
    extend_enum_table(manifest)
    vt_path = (os.environ.get('RH3DM_VALUETYPES') or os.path.join(ROOT, 'api', 'valuetypes.json'))
    if os.path.exists(vt_path):
        register_value_types(json.load(open(vt_path)))
    index_types(manifest)
    per_class = []
    for cls in CLASSES:
        # own members plus generic bases' (File3dmCommonComponentTable<T>), instantiated
        plans = plan_class(cls, {'members': class_members(cls, manifest)}, c_by_name, skipped)
        PLANNED[cls['reg']] = plans
        per_class.append((cls, plans))
    managed_null_pass(per_class, manifest, c_by_name, skipped)
    scope_pass(per_class, manifest, c_by_name, skipped)

    # the generated constructors, for a translated `new T(..)` (G74); computed again below
    import planner as _planner
    def internal_new(cls):
        # a non-public default ctor (specgen InternalNew) a C# body may still call (G86)
        n = (TYPE_RECS.get(cls['type']) or {}).get('InternalNew')
        fn = c_by_name.get(n or '')
        return cls['mode'] == 'pointer' and not cls.get('ff') and fn is not None and fn['returns'] == '%s*' % cls['on']
    _planner.CTORS = {cls['reg']: bool(default_ctor_call(cls, manifest, c_by_name)
                                       or (cls.get('ff') and cls['ff']['New'] in c_by_name)
                                       or internal_new(cls)) for cls in CLASSES}
    _planner.ARGCTORS = {cls['reg']: arg_ctor_plans(cls, manifest, c_by_name, {}) for cls in CLASSES}
    translated_pass(per_class, manifest, skipped)        # G63: translated pure members
    refuse_overridden(manifest, per_class, skipped)
    refuse_overridden_count(per_class, skipped)
    drop_setterless_overrides(per_class, skipped)       # G92: never hide a base setter
    hide_shadowed_out_overloads(per_class, skipped)     # G105: GetScreenPort() over GetScreenPort(out, out)
    drop_orphaned_translations(per_class, skipped)
    all_plans = [p for _, plans in per_class for p in plans]
    # a scoped view's variants call natives of their own (G77): declared, not counted
    native_plans = all_plans + [p['scope_plan'] for p in all_plans if p.get('scope_plan')]
    check_override_shadowing(manifest, per_class)
    # File3dm table protocols (len / [i] / iteration): a concession granted
    # only where RhinoCommon's own protocols are fully generated
    for cls, plans in per_class:
        cls.pop('table', None)
        tp = table_protocol(cls, manifest, plans)
        if tp:
            cls['table'] = tp
    # File3dm.Manifest (G91): RhinoCommon's ManifestTable.GetEnumerator() -- every live
    # component of every type from Unset+1 up to NumOf, each made by the translated
    # File3dmManifestTable.Instantiate (which throws for a type it cannot make, as in .NET);
    # GetEnumerator(type) the same over one type
    for cls, plans in per_class:
        if cls.get('impl') != 'File3dmManifestTable':
            continue
        inst = next((p for p in plans if p['m']['Name'] == 'Instantiate' and p.get('hidden') and p['static']), None)
        cnt = next((p for p in plans if p['m']['Name'] == 'Count' and p['m']['Kind'] == 'property'), None)
        named = ENUM_NAMED.get('ModelComponentType') or {}
        if inst is not None and 'NumOf' in named:
            cls['table'] = {'item': BY_REG['ModelComponent'], 'count': cnt['cpp'] if cnt else None, 'find': None,
                            'types': [str(v) for v in range(1, int(str(named['NumOf']), 0))],
                            'dispatch': inst['cpp'], 'per_type': True,
                            'fns': [C_BY_NAME[n] for n in ('ONX_ModelComponentIterator_New',
                                                           'ONX_ModelComponentIterator_GetNext',
                                                           'ONX_ModelComponentIterator_Delete')]}
    # `return Brep.Faces[index];` (G43): through the list's own generated
    # indexer -- or not at all
    for cls, plans in per_class:
        keep = []
        for p in plans:
            if p['shape'][0] == 'via':
                ips = [q for q in PLANNED.get(p['shape'][1]['reg'], [])
                       if q['m']['Kind'] == 'indexer' and q['shape'][0] == 'construct']
                if len(ips) != 1:
                    skipped['via: list indexer not generated'] = skipped.get('via: list indexer not generated', 0) + 1
                    continue
                p['via_plan'] = ips[0]
            keep.append(p)
        plans[:] = keep
    # linked enumerators (G43: SubDFaceList foreach -- First/Next over the owner's
    # components): a list of generated components gets len and iteration
    for cls, plans in per_class:
        cls.pop('liter', None)
        # or a snapshot (G60: Decals -- DecalEnumerator's copy of the owner's items)
        li = linked_iter(cls, plans) or snapshot_iter(cls, plans, manifest) or index_iter(cls, plans, manifest)
        if li:
            cls['liter'] = li
    # a base's guard asks the derived storage for IsDocumentControlled: every
    # derived class must resolve it to the same idiom (no portable override)
    for cls in CLASSES:
        b = BY_REG.get(cls.get('base', ''))
        if b and any(a['reg'] in NO_OWNED_ABSTRACT for a in abstract_chain(b)) \
                and no_owned_pointer_cpp(cls, 'IsDocumentControlled') is None:
            raise SystemExit('%s overrides IsDocumentControlled; %s asks it through RH3DM_NoOwnedPtr'
                             % (cls['cs'], b['cs']))
    # opaque declarations for C enums appearing in planned signatures
    # (the natives translated bodies call too: G96 ON_TextRun_GetBool's `enum TextRunBool`)
    sig_fns = [p['fn'] for p in native_plans if p['fn']] + [f for p in native_plans for f in p.get('extra_fns') or []]
    enum_decls = sorted({t[5:] for f in sig_fns for t in
                         ([f['returns']] + [q['type'] for q in f['params']])
                         if t.startswith('enum ')})
    # C enums spelled bare in a signature (TextureMappingGetTransform) and
    # defined only inside a C .cpp: opaque-declare with their fixed type
    c_enum_defs = {}
    for f in glob.glob(os.path.join(ROOT, 'src', 'librhino3dm_native', '*.cpp')):
        for cls_kw, ename, under in re.findall(r'\benum\s+(class\s+)?(\w+)\s*:\s*([\w ]+?)\s*\{',
                                               open(f, errors='replace').read()):
            c_enum_defs.setdefault(ename, (bool(cls_kw), under.strip()))
    bare_enums = sorted({t for f in sig_fns for t in
                         ([f['returns']] + [q['type'] for q in f['params']])
                         if t in c_enum_defs})
    # classes defined only inside a C .cpp (draco.cpp's BND_Draco) that planned signatures pass by
    # pointer: opaque-declared (M3: before, the old bnd_draco.h declared them)
    c_class_defs = set()
    for f in glob.glob(os.path.join(ROOT, 'src', 'librhino3dm_native', '*.cpp')):
        c_class_defs |= set(re.findall(r'^(?:class|struct)\s+(\w+)\s*$', open(f, errors='replace').read(), re.M))
    opaque_classes = sorted({n for f in sig_fns for t in ([f['returns']] + [q['type'] for q in f['params']])
                             for n in re.findall(r'\b(\w+)\s*\*', t) if n in c_class_defs})

    # C# enums the plans use that no hand binding registers: define an
    # RH3DM_-prefixed enum class from the manifest's Values and register it
    # under the RhinoCommon name (verified free of hand collisions).
    cpp_to_enum = {v['cpp']: k for k, v in CS_ENUMS.items() if v['gen']}
    used_enums = set()
    for p in all_plans:
        for t, _ in p['bind_args']:
            if t in cpp_to_enum:
                used_enums.add(cpp_to_enum[t])
        if p['shape'][0] == 'enumret' and CS_ENUMS[p['shape'][1]]['gen']:
            used_enums.add(p['shape'][1])
        # a tuple led by an enum result (G49: Intersection.CircleCircle)
        if p['shape'][0] == 'tuple' and isinstance(p['shape'][1], tuple) and p['shape'][1][1] in cpp_to_enum:
            used_enums.add(cpp_to_enum[p['shape'][1][1]])
    used_enums |= {e for e in GUARD_ENUMS if CS_ENUMS[e]['gen']}
    used_enums |= VT_DEFINED_ENUMS          # registered here, defined by the values header (G107)
    enum_values = {}
    type_names = {x['Type'] for x in manifest['members'] if x['Kind'] == 'type'}
    for x in manifest['members']:
        if x['Kind'] != 'enum' or x['Variant'] != 'portable' or not x.get('Values'):
            continue
        # nested enums are keyed by owner (DimensionStyle.ArrowType), as in CS_ENUMS
        key = (x['Type'].replace('+', '.').split('.')[-1] + '.' + x['Name']
               if x['Type'] in type_names else x['Name'])
        if key in used_enums and key not in enum_values:
            vals = x['Values']
            names = [v.split('=')[0].strip() for v in vals]
            def verbatim(i, v):     # a literal, or an earlier member of this enum (`RenderMaterial = Material`)
                if '=' not in v:
                    return True
                e = v.split('=', 1)[1].strip()
                if bool(re.match(r'^-?(\d+|0x[0-9A-Fa-f]+)$', e)) or e in names[:i]:
                    return True
                # `1 << 16`, `Bottom | Left` (G92: TextJustification): literals and earlier members
                # under integer operators -- inside a C++ enum definition they mean the same
                toks = re.findall(r'0x[0-9A-Fa-f]+|\d+|[A-Za-z_]\w*|<<|>>|[|&+~()\-]|\S', e)
                return all(re.match(r'^(0x[0-9A-Fa-f]+|\d+|<<|>>|[|&+~()\-])$', t) or t in names[:i] for t in toks)
            if not all(verbatim(i, v) for i, v in enumerate(vals)):
                # a member defined through another enum (`None = (int)UnsafeNativeMethods...`):
                # declared with specgen's resolved integers, or not at all (G60)
                ints = x.get('EnumInts')
                if not ints or len(ints) != len(vals):
                    raise SystemExit('enum %s: values specgen could not resolve: %s' % (key, vals))
                vals = ['%s = %d' % (v.split('=')[0].strip(), n) for v, n in zip(vals, ints)]
            enum_values[key] = vals
    missing = used_enums - set(enum_values)
    if missing:
        raise SystemExit('no manifest Values for enums: %s' % sorted(missing))
    gen_enum_defs, gen_enum_py, gen_enum_js, gen_enum_py_nested = [], [], [], []
    for ename in sorted(used_enums):
        cpp = CS_ENUMS[ename]['cpp']
        # verbatim: C++ enum classes number implicit enumerators exactly as
        # C# does (first 0, then previous + 1)
        pairs = [(v.split('=')[0].strip(), v) for v in enum_values[ename]]
        # else the values header defines it (G107), or opennurbs does (OWNED_ENUMS)
        if ename not in VT_DEFINED_ENUMS and not CS_ENUMS[ename].get('defined'):
            gen_enum_defs.append('enum class %s : int { %s };'
                                 % (cpp, ', '.join(v for _, v in pairs)))
        vals = '\n'.join('      .value("%s", %s::%s)' % (n, cpp, n)
                         for n, _ in pairs)
        owner = CS_ENUMS[ename].get('owner')
        if owner:
            # nested: registered in the owner's generated class, after the classes exist
            if owner not in BY_CS:
                raise SystemExit('nested enum %s: owner %s is not a generated class' % (ename, owner))
            gen_enum_py_nested.append('  py::enum_<%s>(m.attr("%s"), "%s")\n%s;'
                                      % (cpp, BY_CS[owner]['reg'], ename.split('.')[1], vals))
        else:
            gen_enum_py.append('  py::enum_<%s>(m, "%s")\n%s;' % (cpp, CS_ENUMS[ename].get('py', ename), vals))
        gen_enum_js.append('  enum_<%s>("%s")\n%s;' % (cpp, CS_ENUMS[ename].get('js', ename), vals))
    vt_path = (os.environ.get('RH3DM_VALUETYPES') or os.path.join(ROOT, 'api', 'valuetypes.json'))
    if os.path.exists(vt_path):
        vt = json.load(open(vt_path))
        # a translated member calling a flat C function rhino3dm does not
        # ship (rhino-only) cannot be generated; prune it and its dependents
        portable = {n for n, f in c_by_name.items() if f.get('variant') == 'portable'}
        for m in vt['members']:
            if m['Ok'] and any(n not in portable for n in m.get('Native', [])):
                m['Ok'], m['Reason'] = False, 'native not portable'
        changed = True
        while changed:
            changed = False
            have = {k for m in vt['members'] if m['Ok'] for k in m['Keys']}
            for m in vt['members']:
                if m['Ok'] and any(not (r in have or (r.startswith('*.') and any(h.endswith(r[1:]) for h in have)))
                                   for r in m['Refs']):
                    m['Ok'], m['Reason'], changed = False, 'depends on pruned', True
        vt_natives = sorted({n for m in vt['members'] if m['Ok'] for n in m.get('Native', [])})
        # mutability: own field writes, or `ref this` into a C function whose
        # first struct pointer is NON-const (ON_3dVector_Unitize(ON_3dVector*)
        # mutates; ON_Plane_DistanceTo(const ON_PLANE_STRUCT*) does not --
        # RhinoCommon passes `ref this` to avoid copies), then propagated
        # through calls to the type's own mutating members
        def mutating_c(fn):
            # does the C function WRITE its first (the `ref this`) parameter?
            # const-ness is unreliable in the C layer (ON_Plane_DistanceTo
            # takes a non-const ON_PLANE_STRUCT* and only reads it), so read
            # the body: write-backs, assignments, or non-const method calls
            f = c_by_name.get(fn)
            if not f or not f['params']:
                return False
            q = f['params'][0]
            if q['type'].startswith('const ') or not q['type'].endswith('*'):
                return False
            body = c_body(fn)
            if body is None:
                return True                         # unknown: assume it mutates (safe)
            pn = re.escape(q['name'] or '')
            writes = (r'CopyTo\w*Struct\s*\(\s*\*?\s*{p}\b|memcpy\s*\(\s*{p}\b'
                      r'|\*\s*{p}\s*=[^=]|\b{p}\s*->\s*\w+(\[[^]]*\])?\s*=[^=]').format(p=pn)
            if re.search(writes, body):
                return True
            calls = re.findall(r'\b%s\s*->\s*(\w+)\s*\(' % pn, body)
            pointee = q['type'].rstrip('*').strip()
            return any(m in non_const_methods(pointee) for m in calls)
        for m in vt['members']:
            m['Mutates'] = bool(m.get('MutatesDirect')) or any(mutating_c(n) for n in m.get('NativeRefThis', []))
        changed = True
        while changed:
            changed = False
            # reading a property calls its getter, not its setter: a getter key
            # mutates only when the getter does (G81)
            mut = {k for m in vt['members'] for k in m['Keys']
                   if (m.get('GetterMutates') if m['Kind'] in ('property', 'indexer') and '.set_' not in k
                       else m['Mutates'])}
            for m in vt['members']:
                if not m['Mutates'] and not m['Static'] and any(c in mut for c in m.get('SelfCallsTmp', [])):
                    m['Mutates'] = changed = True
    else:
        vt_natives = []

    decls = {'extern "C" %s %s(%s);' % (
        c_by_name[n]['returns'], n,
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in c_by_name[n]['params']))
        for n in vt_natives} | {'extern "C" %s %s(%s);' % (
        p['fn']['returns'], p['fn']['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in p['fn']['params']))
        for p in native_plans if p['fn']} | {'extern "C" %s %s(%s);' % (
        f['returns'], f['name'], ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in f['params']))
        for p in native_plans for f in p.get('extra_fns') or []}

    ctors = {cls['reg']: default_ctor_call(cls, manifest, c_by_name)
             for cls in CLASSES}
    for cls in CLASSES:                      # free-floating: DefaultCppConstructor (emitted in the class)
        if cls.get('ff') and cls['ff']['New'] in c_by_name:
            ctors[cls['reg']] = ('%s()' % cls['ff']['New'], c_by_name[cls['ff']['New']])
    argctors = {cls['reg']: arg_ctor_plans(cls, manifest, c_by_name, skipped)
                for cls in CLASSES}
    # a factory's `new T()` through a NON-public constructor (specgen
    # InternalNew): a C++ default constructor, never registered -- C# callers
    # cannot construct T that way either (G49)
    hidden_ctors = set()
    newobj_targets = {p['shape'][1]['reg'] for _, plans in per_class for p in plans if p['shape'][0] == 'newobj'}
    # ... and a translated body's `new T()` through one (G86)
    newobj_targets |= {c['reg'] for c in CLASSES for _, plans in per_class for p in plans
                       if p['shape'][0] in ('translated', 'translatedout') and ('new %s()' % c['gen']) in p['shape'][2]}
    for cls in CLASSES:
        if cls['reg'] not in newobj_targets or ctors.get(cls['reg']) or cls.get('ff') or cls['mode'] != 'pointer':
            continue
        n = (TYPE_RECS.get(cls['type']) or {}).get('InternalNew')
        fn = c_by_name.get(n or '')
        if fn and fn['returns'] == '%s*' % cls['on']:
            ctors[cls['reg']] = ('%s(%s)' % (n, ', '.join(['nullptr'] * len(fn['params']))), fn)
            hidden_ctors.add(cls['reg'])
    # `var t = new T(); F(..., t.NonConstPointer()); return t;` needs T's generated
    # default constructor -- or nothing
    for cls, plans in per_class:
        keep = [p for p in plans if p['shape'][0] != 'newobj' or ctors.get(p['shape'][1]['reg'])]
        # a forward whose target went with them goes too (G49: CreatePlaneMapping's
        # 4-argument overload outlived its 5-argument target and called nothing)
        gone = {id(p) for p in plans} - {id(p) for p in keep}
        keep = [p for p in keep if not (p['shape'][0] == 'forward' and id(p['shape'][2]) in gone)]
        if len(keep) != len(plans):
            skipped['newobj: no generated default ctor'] = skipped.get('newobj: no generated default ctor', 0) + len(plans) - len(keep)
            plans[:] = keep
    decls |= {'extern "C" %s %s(%s);' % (
        p['fn']['returns'], p['fn']['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in p['fn']['params']))
        for ps in argctors.values() for p in ps}
    decls |= {'extern "C" %s %s(%s);' % (       # a conditional ctor's other arm (G82)
        f['returns'], f['name'], ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in f['params']))
        for ps in argctors.values() for p in ps for f in p.get('extra_fns') or []}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for expr_fn in ctors.values() if expr_fn
        for fn in [expr_fn[1]]}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('table') for fn in c['table']['fns']}
    # components (G43): their resolvers; linked lists: First/Next
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES for n in ([c['comp']['Const'], c['comp']['NonConst']] if c.get('comp') else [])
        + ([c['liter']['first']['name'], c['liter']['next']['name']] if c.get('liter') and 'first' in c['liter'] else [])
        + ([c['liter']['index'][k]['name'] for k in ('at', 'key')] if (c.get('liter') or {}).get('index') else [])
        + ([c['liter']['snapshot'][k]['name'] for k in ('fill', 'copy', 'delete')] if (c.get('liter') or {}).get('snapshot') else [])
        if n for fn in [c_by_name[n]]}
    # tripwire: a C function rhino3dm does not ship (variant rhino-only) would only
    # fail at link time; refuse here, naming it
    shipped_not = sorted({re.search(r'\b(\w+)\(', d.split('"C"')[1]).group(1) for d in decls}
                         & {n for n, f in c_by_name.items() if f.get('variant') == 'rhino-only'})
    if shipped_not:
        raise SystemExit('generated code calls C functions rhino3dm does not ship: ' + ', '.join(shipped_not))
    # the views' resolvers and duplicators (called by the class headers' accessors)
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('proxy')
        for n in [c['proxy']['Resolver'], c['proxy'].get('Dup'), c['proxy'].get('Commit')] if n
        for fn in [c_by_name[n]]}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('fileview') for fn in [c_by_name[c['fileview']['resolver']]]}      # G104
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('ff')
        for n in [c['ff']['New'], c['ff']['Delete'], c['ff'].get('FromFile')] if n and n in c_by_name
        for fn in [c_by_name[n]]}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('handle') for fn in [c_by_name[c['handle']['Delete']]]}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for c in CLASSES if c.get('shared') for n in (c['shared']['Raw'], c['shared']['Delete'], c['shared']['Copy'])
        if n for fn in [c_by_name[n]]}

    js_pruned = []
    reg_blocks = {cls['reg']: emit_registrations(cls, plans, js_pruned,
                                                 bool(ctors[cls['reg']]) and cls['reg'] not in hidden_ctors,
                                                 argctors[cls['reg']])
                  for cls, plans in per_class}

    # view roots: an abstract class whose derived classes are views of a
    # File3dmObject's geometry (one resolver for all of them)
    view_roots = []
    def storage_owner(d):            # the class holding m_ptr: itself or its concrete base
        while True:
            b = BY_REG.get(d.get('base', ''))
            if b is None or b['mode'] == 'abstract':
                return d
            d = b
    for root in [c for c in CLASSES if c['mode'] == 'abstract']:
        viewers = [d for d in CLASSES if d['mode'] == 'pointer' and _derives(d, root) and not d.get('comp')
                   and (storage_owner(d).get('proxy') or {}).get('Origin') == 'object']
        resolvers = {storage_owner(d)['proxy']['Resolver'] for d in viewers}
        if viewers and len(resolvers) == 1:
            view_roots.append((root, viewers, c_by_name[resolvers.pop()]))
    # views inside another object (G58): one factory per returned class
    sub_targets = sorted({p['shape'][1]['reg'] for p in all_plans if p['shape'][0] == 'subview'})
    sub_targets = [BY_REG[r] for r in sub_targets]

    BANNER = ('// GENERATED by tools/generate/bnd_gen.py (v3) -- DO NOT EDIT.\n'
              '// Shaped by RhinoCommon signatures, calling the flat C layer with\n'
              '// constants and guards baked from the C# call sites '
              '(api/manifest.json).\n')
    files = {}   # filename -> content

    for cls, plans in per_class:
        for p in plans:
            if p['static'] and p['shape'][0] in ('wrap', 'fill'):
                BRIDGE_STATICS.setdefault((cls['cs'], p['m']['Name']), []).append(p)
    vt_names = {t['Cs'] for t in vt['types']} if os.path.exists(vt_path) else set()
    for x in manifest['members']:
        tcs = x['Type'].split('.')[-1]
        if (tcs in vt_names and x['Variant'] == 'portable' and x['Kind'] == 'method'
                and x.get('Body') == 'delegates' and '.' in (x.get('Delegates') or '')
                and x['Delegates'].split('.')[0] in BY_CS):
            BRIDGE_CANDIDATES.setdefault(tcs, []).append(x)
    if os.path.exists(vt_path):
        vt_regs = [t['Cs'] for t in vt['types'] if not t['IsStaticClass']]
        data_marker_pass(vt, per_class, manifest, skipped)
        files.update(emit_valuetypes(vt, emit_valuetype_bindings(vt, skipped)))
    else:
        vt_regs = []
    check_hand_members({c['reg'] for c in CLASSES} | set(vt_regs), os.path.join(ROOT, 'src'))   # G123

    # translated enums reach the bindings as the type registered under the
    # RhinoCommon name (today the old bindings' enum): RH3DM_Pub casts
    vt_pub = ('\n// translated enums -> their registered public type; identity otherwise\n'
              'template <class T> inline T RH3DM_Pub(T v) { return v; }\n'
              + ''.join('inline %s RH3DM_Pub(%s v) { return (%s)(int)v; }\n' % (pt, e, pt)
                        for e, pt in sorted(VT_ENUMS.items())) + '\n')
    vt_includes = ''.join('#include "%s.h"\n' % vt_file(t) for t in vt['types']
                          if all(f['Type'] for f in t['Fields'])) if os.path.exists(vt_path) else ''
    files['gen_common.h'] = BANNER + '''#pragma once
#include "bindings.h"
#include <stdexcept>
#include <new>
#include <type_traits>
#include <algorithm>
#include <ctime>
#include <cstring>

// The real C-layer contract header: blittable structs, RHMONO_STRING, and
// CRhCmnStringHolder (whose layout is platform-conditional, so a local copy
// would be an ABI trap). It has no includes of its own; bindings.h already
// provided opennurbs. Include under RHINO3DM_BUILD, scoped: that is the
// branch the C layer itself compiles this header with (the other branch
// declares Win32 types like HBITMAP), so CRhCmnStringHolder gets the
// identical declaration here.
#if !defined(RHINO3DM_BUILD)
#define RHINO3DM_BUILD
#define RH3DM_GEN_TEMP_DEF
#endif
#include "../librhino3dm_native/rhcommon_c/rhcommon_c_api.h"
#if defined(RH3DM_GEN_TEMP_DEF)
#undef RHINO3DM_BUILD
#undef RH3DM_GEN_TEMP_DEF
#endif
ON_Plane FromPlaneStruct(const ON_PLANE_STRUCT& ps);

// generated value types (translated RhinoCommon bodies) and the conversions
// from the C layer's opennurbs values; field-wise, so independent of which
// C# constructors translated
''' + vt_includes + vt_pub + '''inline BND_Point3d RH3DM_P(const ON_3dPoint& p) { BND_Point3d r; r.m_x = p.x; r.m_y = p.y; r.m_z = p.z; return r; }
inline BND_Vector3d RH3DM_V(const ON_3dVector& v) { BND_Vector3d r; r.m_x = v.x; r.m_y = v.y; r.m_z = v.z; return r; }
inline BND_Interval RH3DM_I(const ON_Interval& i) { BND_Interval r; r.m_t0 = i.m_t[0]; r.m_t1 = i.m_t[1]; return r; }

// a tuple result C# may return as null (`if (count < 1) return null;`): BND_TUPLE
// is py::tuple, which cannot hold None -- the old NullTuple() raises TypeError
// in Python (pilot-findings #29) -- so these return an object (G54)
#if defined(ON_PYTHON_COMPILE)
typedef py::object RH3DM_NTUPLE;
inline RH3DM_NTUPLE RH3DM_NoneTuple() { return py::none(); }
// a DateTime result (File3dm.Created; G68): a naive datetime.datetime; DateTime.MinValue is datetime.min
typedef py::object RH3DM_DATETIME;
inline RH3DM_DATETIME RH3DM_DateTime(int y, int mo, int d, int h, int mi, int s) {
  return py::module_::import("datetime").attr("datetime")(y, mo, d, h, mi, s); }
inline RH3DM_DATETIME RH3DM_DateTimeMin() { return RH3DM_DateTime(1, 1, 1, 0, 0, 0); }
#else
typedef emscripten::val RH3DM_NTUPLE;
inline RH3DM_NTUPLE RH3DM_NoneTuple() { return emscripten::val::null(); }
// a std::vector result of a translated member (G70): a JS array of its elements
template <class T> inline emscripten::val RH3DM_JsArray(const std::vector<T>& v) {
  emscripten::val a = emscripten::val::array();
  for (size_t i = 0; i < v.size(); i++) a.call<void>("push", emscripten::val(v[i]));
  return a; }
// a sequence of new class objects (G89: SubDVertex.Edges): the caller owns each
template <class T> inline emscripten::val RH3DM_JsArray(const std::vector<T*>& v) {
  emscripten::val a = emscripten::val::array();
  for (size_t i = 0; i < v.size(); i++) a.call<void>("push", v[i] ? emscripten::val(v[i], emscripten::allow_raw_pointers()) : emscripten::val::null());
  return a; }
// a sequence argument (G82/G83): a JS array, or a list object with toArray()
// (Point3dList, Polyline: decision 4 of option 4) -- anything else throws
inline bool RH3DM_JsIsSeq(const emscripten::val& v) {
  return v.isArray() || (v.typeOf().as<std::string>() == "object" && !v.isNull()
                         && v["toArray"].typeOf().as<std::string>() == "function"); }
// a sequence argument's first element, or undefined (G130: same-arity overloads told apart by their
// sequences' element shape)
inline emscripten::val RH3DM_JsSeqFirst(const emscripten::val& v) {
  emscripten::val a = v.isArray() ? v : v.call<emscripten::val>("toArray");
  return a["length"].as<int>() > 0 ? a[0] : emscripten::val::undefined(); }
template <class T> inline std::vector<T> RH3DM_JsSeq(const emscripten::val& v) {
  if (v.isArray()) return emscripten::vecFromJSArray<T>(v);
  if (RH3DM_JsIsSeq(v)) return emscripten::vecFromJSArray<T>(v.call<emscripten::val>("toArray"));
  throw std::invalid_argument("expected an array or a point list"); }
// a DateTime result (File3dm.Created; G68): a JS Date (local time, month 0-based)
typedef emscripten::val RH3DM_DATETIME;
inline RH3DM_DATETIME RH3DM_DateTime(int y, int mo, int d, int h, int mi, int s) {
  return emscripten::val::global("Date").new_(y, mo - 1, d, h, mi, s); }
inline RH3DM_DATETIME RH3DM_DateTimeMin() { return RH3DM_DateTime(1, 1, 1, 0, 0, 0); }
#endif
// RhinoCommon's SimpleArrayPoint3d in translated data-class bodies (G81): an
// ON_3dPointArray filled as its C# constructor appends; the pointers it hands a
// native call are untyped, as an IntPtr is
struct RH3DM_Points
{
  ON_3dPointArray a;
  RH3DM_Points() {}
  explicit RH3DM_Points(const std::vector<BND_Point3d>& v)
  {
    a.Reserve((int)v.size());
    for (const BND_Point3d& p : v) a.Append(ON_3dPoint(p.m_x, p.m_y, p.m_z));
  }
  int Count() const { return a.Count(); }
  void* ConstPointer() const { return (void*)&a; }
  void* NonConstPointer() { return (void*)&a; }
  // SimpleArrayPoint3d.ToArray() (G84): the points, as Point3d[]
  std::vector<BND_Point3d> ToArray() const
  {
    std::vector<BND_Point3d> v;
    v.reserve((size_t)a.Count());
    for (int i = 0; i < a.Count(); i++) { BND_Point3d p; p.m_x = a[i].x; p.m_y = a[i].y; p.m_z = a[i].z; v.push_back(p); }
    return v;
  }
};

// SimpleArrayPoint2d on the stack (G116: Leader.Points2D): the native fills it, ToArray() reads it
struct RH3DM_Points2d
{
  ON_2dPointArray a;
  int Count() const { return a.Count(); }
  void* ConstPointer() const { return (void*)&a; }
  void* NonConstPointer() { return (void*)&a; }
  std::vector<BND_Point2d> ToArray() const
  {
    std::vector<BND_Point2d> v;
    v.reserve((size_t)a.Count());
    for (int i = 0; i < a.Count(); i++) { BND_Point2d p; p.m_x = a[i].x; p.m_y = a[i].y; v.push_back(p); }
    return v;
  }
};

// `RhinoListHelpers.GetConstArray(points, out count)` in a translated class body (G86)
template <class T> inline const std::vector<T>& RH3DM_ConstArray(const std::vector<T>& v, int& n) { n = (int)v.size(); return v; }

// a byte[] argument (File3dm.FromByteArray; G86): Python any buffer (bytes,
// bytearray, numpy), JS a Uint8Array or an array of numbers
struct RH3DM_Bytes { std::vector<unsigned char> v;
  RH3DM_Bytes() {}
  explicit RH3DM_Bytes(size_t n) : v(n) {}       // `new byte[n]` (G101)
  const unsigned char* data() const { return v.data(); }
  size_t size() const { return v.size(); } };
// GetUserStrings() as rhino3dm returns it (D62; RhinoCommon's NameValueCollection is generated nowhere):
// a tuple of (key, value) pairs, Python and JS alike
inline BND_TUPLE RH3DM_UserStrings(const ON_Object* o) {
  ON_ClassArray<ON_wString> keys;
  if (o) o->GetUserStringKeys(keys);
  BND_TUPLE rc = CreateTuple(keys.Count());
  for (int i = 0; i < keys.Count(); i++) {
    ON_wString v; o->GetUserString(keys[i].Array(), v);
    BND_TUPLE kv = CreateTuple(2);
    SetTuple(kv, 0, std::wstring(keys[i].Array()));
    SetTuple(kv, 1, std::wstring(v.Array()));
    SetTuple(rc, i, kv); }
  return rc; }
// a class argument for a pointer-taking setter in a translated body (G112: `rc.ParentDimensionStyle =
// dimStyle`, a const& parameter): its address; a pointer or null passes through
template <class T> inline const T* RH3DM_AddrOf(const T& v) { return &v; }
template <class T> inline const T* RH3DM_AddrOf(const T* p) { return p; }
template <class T> inline const T* RH3DM_AddrOf(T* p) { return p; }
inline std::nullptr_t RH3DM_AddrOf(std::nullptr_t) { return nullptr; }
// a bool[] local in a translated class body (G111: `bool[] isline = { false, false, false, false };`
// filled by ON_V6_DimLinear_GetDisplayLines): std::vector<bool> has no data(), so bytes viewed as bool
struct RH3DM_BoolVec { std::vector<unsigned char> v;
  RH3DM_BoolVec() {}
  explicit RH3DM_BoolVec(size_t n) : v(n, 0) {}  // `new bool[n]`
  RH3DM_BoolVec(std::initializer_list<bool> l) { for (bool b : l) v.push_back(b ? 1 : 0); }
  bool* data() { static_assert(sizeof(bool) == 1, "bool is not one byte"); return reinterpret_cast<bool*>(v.data()); }
  bool& operator[](size_t i) { return data()[i]; }
  size_t size() const { return v.size(); } };
// `using (var w = new StdVectorByte())` in a translated body (G107): the std::vector<unsigned char> a
// native fills (Draco_ToByteArray), handed out as bytes
struct RH3DM_ByteVec { std::vector<unsigned char> v;
  std::vector<unsigned char>* NonConstPointer() { return &v; }
  const std::vector<unsigned char>* ConstPointer() const { return &v; }
  RH3DM_Bytes ToArray() const { RH3DM_Bytes b; b.v = v; return b; } };
#if defined(ON_PYTHON_COMPILE)
namespace pybind11 { namespace detail {
template <> struct type_caster<RH3DM_Bytes> {
  PYBIND11_TYPE_CASTER(RH3DM_Bytes, const_name("bytes"));
  bool load(handle src, bool) {
    if (!PyObject_CheckBuffer(src.ptr())) return false;
    buffer_info info = reinterpret_borrow<buffer>(src).request();
    const unsigned char* p = (const unsigned char*)info.ptr;
    value.v.assign(p, p + info.size * info.itemsize);
    return true; }
  static handle cast(const RH3DM_Bytes& b, return_value_policy, handle) {
    return pybind11::bytes((const char*)b.v.data(), b.v.size()).release(); }
}; } }
#else
inline RH3DM_Bytes RH3DM_JsBytes(const emscripten::val& v) {
  RH3DM_Bytes b; b.v = emscripten::convertJSArrayToNumberVector<unsigned char>(v); return b; }
// a byte[] result (G101: File3dm.ToByteArray): a Uint8Array of its own (a copy, not a heap view)
inline emscripten::val RH3DM_JsBytesOut(const RH3DM_Bytes& b) {
  return emscripten::val(emscripten::typed_memory_view(b.v.size(), b.v.data())).call<emscripten::val>("slice"); }
#endif

// System.Drawing.Rectangle in a translated body (G86): the old bindings' tuple
// (x, y, width, height); FromLTRB as .NET makes it
inline BND_TUPLE RH3DM_RectLTRB(int l, int t, int r, int b) {
  BND_TUPLE rc = CreateTuple(4);
  SetTuple(rc, 0, l); SetTuple(rc, 1, t); SetTuple(rc, 2, r - l); SetTuple(rc, 3, b - t);
  return rc; }

// a Rectangle ARGUMENT (G105: SetScreenPort(Rectangle), ScreenPort's setter): the same tuple
// (x, y, width, height); its sides as System.Drawing.Rectangle defines them
inline int RH3DM_RectAt(const BND_TUPLE& t, int i) {
#if defined(ON_PYTHON_COMPILE)
  if (py::len(t) != 4) throw std::invalid_argument("a rectangle is (x, y, width, height)");
  return py::cast<int>(t[i]);
#else
  if (t["length"].as<int>() != 4) throw std::invalid_argument("a rectangle is [x, y, width, height]");
  return t[i].as<int>();
#endif
}
inline int RH3DM_RectX(const BND_TUPLE& t) { return RH3DM_RectAt(t, 0); }
inline int RH3DM_RectY(const BND_TUPLE& t) { return RH3DM_RectAt(t, 1); }
inline int RH3DM_RectWidth(const BND_TUPLE& t) { return RH3DM_RectAt(t, 2); }
inline int RH3DM_RectHeight(const BND_TUPLE& t) { return RH3DM_RectAt(t, 3); }
inline int RH3DM_RectLeft(const BND_TUPLE& t) { return RH3DM_RectX(t); }
inline int RH3DM_RectTop(const BND_TUPLE& t) { return RH3DM_RectY(t); }
inline int RH3DM_RectRight(const BND_TUPLE& t) { return RH3DM_RectX(t) + RH3DM_RectWidth(t); }
inline int RH3DM_RectBottom(const BND_TUPLE& t) { return RH3DM_RectY(t) + RH3DM_RectHeight(t); }

// a translated member's out object (G86): the tuple's caller owns it
template <class T> inline void RH3DM_SetTupleOwned(BND_TUPLE& t, int i, T* p) {
#if defined(ON_PYTHON_COMPILE)
  t[i] = p ? py::cast(p, py::return_value_policy::take_ownership) : py::none();
#else
  t.set(i, p ? emscripten::val(p, emscripten::allow_raw_pointers()) : emscripten::val::null());
#endif
}
// a translated member's out sequence (G86): a list in Python, an array in JS
#if defined(ON_PYTHON_COMPILE)
template <class T> inline py::list RH3DM_Seq(const std::vector<T>& v) { py::list l; for (const T& x : v) l.append(py::cast(x)); return l; }
#else
template <class T> inline emscripten::val RH3DM_Seq(const std::vector<T>& v) { return RH3DM_JsArray(v); }
// a JS array of class instances (G95): borrowed pointers for the call
template<class T> inline std::vector<const T*> RH3DM_JsSeqPtr(const emscripten::val& v) {
  emscripten::val a = v.isArray() ? v : v.call<emscripten::val>("toArray");
  std::vector<const T*> r;
  unsigned n = a["length"].as<unsigned>();
  for (unsigned i = 0; i < n; i++) r.push_back(a[i].isNull() ? nullptr : a[i].as<T*>(emscripten::allow_raw_pointers()));
  return r; }
#endif

// a C# cache field holding a view of this object's sub-object (G89: ViewInfo.m_viewport):
// made on first access, owned here; a copy of the owner starts without one (it views the copy)
template <class T> struct RH3DM_Cache
{
  std::unique_ptr<T> p;
  RH3DM_Cache() {}
  RH3DM_Cache(const RH3DM_Cache&) {}
  RH3DM_Cache& operator=(const RH3DM_Cache&) { p.reset(); return *this; }
};

// SimpleArrayGuid in a class body (G89: File3dmRenderContent.Children): the uuids on the stack
struct RH3DM_Guids
{
  ON_SimpleArray<ON_UUID> a;
  void* NonConstPointer() { return &a; }
  void* ConstPointer() const { return (void*)&a; }
  int Count() const { return a.Count(); }
  ON_UUID operator[](int i) const { return a[i]; }
};

// RhinoCommon's RDK Variant (G89, D40): `IConvertible` out and `object` in as a language
// value, converted by the variant's runtime type -- scalars native, an id a uuid.UUID (JS
// a string), 2/3/4 doubles a tuple (JS an array), a color a Color4f, a matrix a
// Transform, a buffer bytes (JS a Uint8Array); a time is refused (no faithful mapping)
#if defined(ON_PYTHON_COMPILE)
// a sequence of new class objects (G89: SubDVertex.Edges): Python owns each, and each
// keeps the object it came from alive (a list cannot be a keep_alive nurse)
template <class T> inline py::list RH3DM_PyOwnedList(const std::vector<T*>& v, py::handle owner) {
  py::list l;
  for (T* e : v) {
    py::object o = py::cast(e, py::return_value_policy::take_ownership);
    if (e) py::detail::keep_alive_impl(o, owner);
    l.append(o);
  }
  return l; }
typedef py::object RH3DM_Value;
inline RH3DM_Value RH3DM_ValueNone() { return py::none(); }
#else
typedef emscripten::val RH3DM_Value;
inline RH3DM_Value RH3DM_ValueNone() { return emscripten::val::null(); }
#endif

// a DateTime (G97): the instant as Unix seconds -- RhinoCommon's DateTimes here come from the C
// layer's time_t through TimeHelpers (FromUnixEpoch: UTC, then .ToLocalTime(); ToUnixEpoch:
// .ToUniversalTime()), so a date round-trips through the machine's time zone exactly as .NET's.
// Public signatures take and give the language's date: Python a datetime (naive = local time,
// .NET's Local/Unspecified; aware = its instant), JS a Date (an instant). Sub-second parts are
// dropped, as ToUnixEpoch's integer division drops them.
typedef RH3DM_Value RH3DM_DateTimeObj;
struct RH3DM_Time
{
  long long t = 0;
  RH3DM_Time() {}
  explicit RH3DM_Time(long long s) : t(s) {}
  struct tm Local() const
  {
    time_t tt = (time_t)t;
    struct tm r;
    memset(&r, 0, sizeof(r));
#if defined(_WIN32)
    localtime_s(&r, &tt);
#else
    localtime_r(&tt, &r);
#endif
    return r;
  }
  int Year() const { return Local().tm_year + 1900; }
  int Month() const { return Local().tm_mon + 1; }
  int Day() const { return Local().tm_mday; }
  int Hour() const { return Local().tm_hour; }
  int Minute() const { return Local().tm_min; }
  int Second() const { return Local().tm_sec; }
  operator RH3DM_DateTimeObj() const;     // into a public member's parameter
};
inline RH3DM_DateTimeObj RH3DM_DateTimeOut(const RH3DM_Time& d)
{
  struct tm r = d.Local();
#if defined(ON_PYTHON_COMPILE)
  return py::module_::import("datetime").attr("datetime")(r.tm_year + 1900, r.tm_mon + 1, r.tm_mday, r.tm_hour, r.tm_min, r.tm_sec);
#else
  (void)r;
  return emscripten::val::global("Date").new_((double)d.t * 1000.0);
#endif
}
inline RH3DM_Time::operator RH3DM_DateTimeObj() const { return RH3DM_DateTimeOut(*this); }
inline RH3DM_Time RH3DM_DateTimeIn(const RH3DM_DateTimeObj& o)
{
#if defined(ON_PYTHON_COMPILE)
  py::object dt = py::module_::import("datetime").attr("datetime");
  if (!py::isinstance(o, dt)) throw py::type_error("a datetime.datetime is required");
  if (!o.attr("tzinfo").is_none())
    return RH3DM_Time((long long)o.attr("timestamp")().cast<double>());   // an aware datetime: its instant
  struct tm r;
  memset(&r, 0, sizeof(r));
  r.tm_year = o.attr("year").cast<int>() - 1900;
  r.tm_mon = o.attr("month").cast<int>() - 1;
  r.tm_mday = o.attr("day").cast<int>();
  r.tm_hour = o.attr("hour").cast<int>();
  r.tm_min = o.attr("minute").cast<int>();
  r.tm_sec = o.attr("second").cast<int>();
  r.tm_isdst = -1;                       // local time: the zone's rule decides (as .NET's ToUniversalTime)
  return RH3DM_Time((long long)mktime(&r));
#else
  if (!o.instanceof(emscripten::val::global("Date"))) throw std::invalid_argument("a Date is required");
  return RH3DM_Time((long long)(o.call<double>("getTime") / 1000.0));
#endif
}
inline RH3DM_Time RH3DM_ToDateTime(const RH3DM_Time& d) { return d; }
inline RH3DM_Time RH3DM_ToDateTime(const RH3DM_DateTimeObj& o) { return RH3DM_DateTimeIn(o); }

struct RH3DM_Variant
{
  ON_XMLVariant v;
  RH3DM_Variant() {}
  explicit RH3DM_Variant(const RH3DM_Value& x)
  {
#if defined(ON_PYTHON_COMPILE)
    if (x.is_none()) { v.SetNull(); return; }
    if (py::isinstance<py::bool_>(x)) { v.SetValue(x.cast<bool>()); return; }
    if (py::isinstance<py::int_>(x)) { v.SetValue(x.cast<int>()); return; }
    if (py::isinstance<py::float_>(x)) { v.SetValue(x.cast<double>()); return; }
    if (py::isinstance<py::str>(x)) { v.SetValue(ON_wString(x.cast<std::wstring>().c_str())); return; }
    if (py::isinstance(x, py::module_::import("uuid").attr("UUID"))) { v.SetValue(Binding_to_ON_UUID(x)); return; }
    if (py::isinstance<BND_Color4f>(x)) { BND_Color4f c = x.cast<BND_Color4f>(); v.SetValue(ON_4fColor(c.m_r, c.m_g, c.m_b, c.m_a)); return; }
    if (py::isinstance<BND_Transform>(x)) { BND_Transform t = x.cast<BND_Transform>(); ON_Xform xf = RH3DM_Val(t); v.SetValue(xf); return; }
    if (py::isinstance<py::bytes>(x) || py::isinstance<py::bytearray>(x)) { std::string b = x.cast<std::string>(); v.SetValue(b.data(), b.size()); return; }
    if (py::isinstance(x, py::module_::import("datetime").attr("datetime"))) { v.SetValue((time_t)RH3DM_DateTimeIn(x).t); return; }   // G97
    if (py::isinstance<py::tuple>(x) || py::isinstance<py::list>(x)) {
      py::sequence q = x.cast<py::sequence>();
      size_t n = (size_t)py::len(q);
      if (n >= 2 && n <= 4) {
        double d[4] = {0, 0, 0, 0};
        for (size_t i = 0; i < n; i++) d[i] = q[i].cast<double>();
        v.SetValue(d, n == 2 ? ON_XMLVariant::ArrayTypes::Array2 : n == 3 ? ON_XMLVariant::ArrayTypes::Array3 : ON_XMLVariant::ArrayTypes::Array4);
        return;
      }
    }
    throw py::type_error("Variant: unsupported value type");
#else
    std::string t = x.typeOf().as<std::string>();
    if (x.isNull() || x.isUndefined()) { v.SetNull(); return; }
    if (t == "boolean") { v.SetValue(x.as<bool>()); return; }
    if (t == "number") {
      double d = x.as<double>();
      if (emscripten::val::global("Number").call<bool>("isInteger", x)) v.SetValue((int)d); else v.SetValue(d);
      return;
    }
    if (t == "string") { std::wstring w = x.as<std::wstring>(); v.SetValue(ON_wString(w.c_str())); return; }
    if (x.instanceof(emscripten::val::global("Date"))) { v.SetValue((time_t)RH3DM_DateTimeIn(x).t); return; }   // G97
    if (x.isArray()) {
      unsigned n = x["length"].as<unsigned>();
      if (n >= 2 && n <= 4) {
        double d[4] = {0, 0, 0, 0};
        for (unsigned i = 0; i < n; i++) d[i] = x[i].as<double>();
        v.SetValue(d, n == 2 ? ON_XMLVariant::ArrayTypes::Array2 : n == 3 ? ON_XMLVariant::ArrayTypes::Array3 : ON_XMLVariant::ArrayTypes::Array4);
        return;
      }
    }
    throw std::invalid_argument("Variant: unsupported value type");
#endif
  }
  // a time (G97): RhinoCommon's Variant(DateTime) / ToDateTime -- ON_XMLVariant's time_t
  explicit RH3DM_Variant(const RH3DM_Time& d) { v.SetValue((time_t)d.t); }
  RH3DM_Time ToDateTime() const { return RH3DM_Time((long long)v.AsTime()); }
  void* NonConstPointer() { return &v; }
  void* ConstPointer() const { return (void*)&v; }
  operator RH3DM_Value() const
  {
    switch (v.Type())
    {
    case ON_XMLVariant::Types::Null: return RH3DM_ValueNone();
    case ON_XMLVariant::Types::Time: return RH3DM_DateTimeOut(ToDateTime());
#if defined(ON_PYTHON_COMPILE)
    case ON_XMLVariant::Types::Bool: return py::bool_(v.AsBool());
    case ON_XMLVariant::Types::Integer: return py::int_(v.AsInteger());
    case ON_XMLVariant::Types::Float: return py::float_((double)v.AsFloat());
    case ON_XMLVariant::Types::Double: return py::float_(v.AsDouble());
    case ON_XMLVariant::Types::String: return py::cast(std::wstring(static_cast<const wchar_t*>(v.AsString())));
    case ON_XMLVariant::Types::Uuid: return ON_UUID_to_Binding(v.AsUuid());
    case ON_XMLVariant::Types::DoubleArray2: { ON_2dPoint p = v.As2dPoint(); return py::make_tuple(p.x, p.y); }
    case ON_XMLVariant::Types::DoubleArray3: { ON_3dPoint p = v.As3dPoint(); return py::make_tuple(p.x, p.y, p.z); }
    case ON_XMLVariant::Types::DoubleArray4: { ON_4dPoint p = v.As4dPoint(); return py::make_tuple(p.x, p.y, p.z, p.w); }
    case ON_XMLVariant::Types::DoubleColor4: { ON_4fColor c = v.AsColor(); BND_Color4f r; r.m_r = c.Red(); r.m_g = c.Green(); r.m_b = c.Blue(); r.m_a = c.Alpha(); return py::cast(r); }
    case ON_XMLVariant::Types::Matrix: { ON_Xform xf = v.AsXform(); BND_Transform t = RH3DM_Val(xf); return py::cast(t); }
    case ON_XMLVariant::Types::Buffer: { ON_Buffer b = v.AsBuffer(); std::string s((size_t)b.Size(), '\0'); b.SeekFromStart(0); b.Read((ON__UINT64)s.size(), &s[0]); return py::bytes(s); }
#else
    case ON_XMLVariant::Types::Bool: return emscripten::val(v.AsBool());
    case ON_XMLVariant::Types::Integer: return emscripten::val(v.AsInteger());
    case ON_XMLVariant::Types::Float: return emscripten::val((double)v.AsFloat());
    case ON_XMLVariant::Types::Double: return emscripten::val(v.AsDouble());
    case ON_XMLVariant::Types::String: return emscripten::val(std::wstring(static_cast<const wchar_t*>(v.AsString())));
    case ON_XMLVariant::Types::Uuid: return emscripten::val(ON_UUID_to_Binding(v.AsUuid()));
    case ON_XMLVariant::Types::DoubleArray2: { ON_2dPoint p = v.As2dPoint(); emscripten::val a = emscripten::val::array(); a.call<void>("push", p.x); a.call<void>("push", p.y); return a; }
    case ON_XMLVariant::Types::DoubleArray3: { ON_3dPoint p = v.As3dPoint(); emscripten::val a = emscripten::val::array(); a.call<void>("push", p.x); a.call<void>("push", p.y); a.call<void>("push", p.z); return a; }
    case ON_XMLVariant::Types::DoubleArray4: { ON_4dPoint p = v.As4dPoint(); emscripten::val a = emscripten::val::array(); a.call<void>("push", p.x); a.call<void>("push", p.y); a.call<void>("push", p.z); a.call<void>("push", p.w); return a; }
    case ON_XMLVariant::Types::DoubleColor4: { ON_4fColor c = v.AsColor(); BND_Color4f r; r.m_r = c.Red(); r.m_g = c.Green(); r.m_b = c.Blue(); r.m_a = c.Alpha(); return emscripten::val(r); }
    case ON_XMLVariant::Types::Matrix: { ON_Xform xf = v.AsXform(); BND_Transform t = RH3DM_Val(xf); return emscripten::val(t); }
    case ON_XMLVariant::Types::Buffer: { ON_Buffer b = v.AsBuffer(); std::vector<unsigned char> s((size_t)b.Size()); b.SeekFromStart(0); if (!s.empty()) b.Read((ON__UINT64)s.size(), s.data()); return emscripten::val::global("Uint8Array").new_(emscripten::typed_memory_view(s.size(), s.data())); }
#endif
    default: break;
    }
    throw std::runtime_error("Variant: an unknown type has no language value here");
  }
};

// a modeled managed field (G88: AnnotationBase.m_parent_dimstyle): a borrowed object or
// an owned one (a light copy); an owned one replaced stays alive until the owner dies,
// so a wrapper the getter handed out never dangles
template <class T> struct RH3DM_MField {
  T* p = nullptr;
  bool owned = false;
  std::vector<T*> old;
  RH3DM_MField() = default;
  RH3DM_MField(const RH3DM_MField&) = delete;
  RH3DM_MField& operator=(const RH3DM_MField&) = delete;
  ~RH3DM_MField() { if (owned) delete p; for (T* q : old) delete q; }
  void set(T* q, bool own) { if (owned && p && p != q) old.push_back(p); p = q; owned = own; }
};

// a native's result in a translated class body (G85): a pointer is C#'s IntPtr
template <class T, typename std::enable_if<!std::is_enum<T>::value, int>::type = 0> inline T RH3DM_R(T v) { return v; }
// a temporary as an lvalue for the rest of the full expression (G98: `ref id` of a converted parameter)
template <class T> inline T& RH3DM_Lv(T&& v) { return v; }
template <class T> inline void* RH3DM_R(T* p) { return (void*)p; }

// translated string members (G79), with .NET's semantics: Replace replaces every
// occurrence and refuses an empty pattern; NewLine is the platform's
inline std::wstring RH3DM_StrReplace(std::wstring s, const std::wstring& a, const std::wstring& b)
{
  if (a.empty()) throw std::invalid_argument("String cannot be of zero length.");
  for (size_t i = s.find(a); i != std::wstring::npos; i = s.find(a, i + b.size()))
    s.replace(i, a.size(), b);
  return s;
}
inline bool RH3DM_StrContains(const std::wstring& s, const std::wstring& a) { return s.find(a) != std::wstring::npos; }
inline bool RH3DM_StrStartsWith(const std::wstring& s, const std::wstring& a) { return s.compare(0, a.size(), a) == 0; }
inline bool RH3DM_StrEndsWith(const std::wstring& s, const std::wstring& a)
{ return s.size() >= a.size() && s.compare(s.size() - a.size(), a.size(), a) == 0; }
#if defined(_WIN32)
inline std::wstring RH3DM_NewLine() { return L"\\r\\n"; }
#else
inline std::wstring RH3DM_NewLine() { return L"\\n"; }
#endif

// `new SimpleArrayCurvePointer(curves)` (G95): an ON_SimpleArray of the objects' const pointers
template<class O> struct RH3DM_PtrArray
{
  ON_SimpleArray<const O*> a;
  void* ConstPointer() const { return (void*)&a; }
  void* NonConstPointer() { return &a; }
};
template<class O, class F> struct RH3DM_PtrsFn
{
  F f;
  template<class V> RH3DM_PtrArray<O> operator()(const V& v) const {
    RH3DM_PtrArray<O> r;
    for (auto p : v) r.a.Append(p ? (const O*)f(p) : nullptr);
    return r;
  }
};
template<class O, class F> inline RH3DM_PtrsFn<O, F> RH3DM_PtrsOf(F f) { return RH3DM_PtrsFn<O, F>{f}; }

// a C# enum into a native (G91): the C enum parameter (ON_ModelComponent::Type) or int it takes
struct RH3DM_EnumArg
{
  int v;
  explicit RH3DM_EnumArg(int x) : v(x) {}
  operator int() const { return v; }
  template<class E, class = typename std::enable_if<std::is_enum<E>::value>::type>
  operator E() const { return (E)v; }
};
// a C-layer enum result (G96: ON_Texture_GetProjectionMode's TextureProjectionModes, which
// P/Invoke declares as the C# enum itself): converts to the binding's enum, or to int
template <class T, typename std::enable_if<std::is_enum<T>::value, int>::type = 0> inline RH3DM_EnumArg RH3DM_R(T v) { return RH3DM_EnumArg((int)v); }

// a class temporary in mid-expression (G92: `obj.Attributes.GetGroupList()`): freed at the end of
// the full expression when the callee returned a new object (Own), kept when borrowed; a null
// receiver throws, as C#'s NullReferenceException
template<bool Own, class T> struct RH3DM_TmpT
{
  T* p;
  RH3DM_TmpT(T* x) : p(x) {}
  RH3DM_TmpT(const RH3DM_TmpT&) = delete;
  ~RH3DM_TmpT() { if (Own) delete p; }
  T* operator->() const { if (!p) throw std::runtime_error("null reference"); return p; }
};
template<bool Own, class T> inline RH3DM_TmpT<Own, T> RH3DM_Tmp(T* p) { return RH3DM_TmpT<Own, T>(p); }
// `list.Contains(x)` on a sequence (G92)
template<class V, class X> inline bool RH3DM_Contains(const V& v, const X& x) { return std::find(v.begin(), v.end(), x) != v.end(); }
// `var list = new List<File3dmObject>()` (G92): the class objects it collects are owned until
// ToArray hands them out
template<class P> struct RH3DM_OwnedVec
{
  std::vector<P> v;
  RH3DM_OwnedVec() {}
  // `new T[n]` (G96): n null slots, each `a[i] = new T(..)` set once (an old one is freed;
  // an index out of range throws, as C#'s)
  explicit RH3DM_OwnedVec(int n) : v(n > 0 ? (size_t)n : 0, nullptr) {}
  RH3DM_OwnedVec(const RH3DM_OwnedVec&) = delete;
  ~RH3DM_OwnedVec() { for (P p : v) delete p; }
  void Set(int i, P p) { if (i < 0 || i >= (int)v.size()) { delete p; throw std::out_of_range("index"); } delete v[i]; v[i] = p; }
  void Add(P p) { v.push_back(p); }
  int Count() const { return (int)v.size(); }
  std::vector<P> ToArray() { std::vector<P> r; r.swap(v); return r; }
};

// a LINQ source (G96): the class objects a call returned, freed after the pass when the callee
// made them
template<class V> struct RH3DM_SeqGuard
{
  V& v;
  bool own;
  RH3DM_SeqGuard(V& x, bool o) : v(x), own(o) {}
  RH3DM_SeqGuard(const RH3DM_SeqGuard&) = delete;
  ~RH3DM_SeqGuard() { if (own) for (auto p : v) delete p; }
};

// `new TextRun(ptr)` (G96): a handle class adopting the native's object (null stays null)
template<class B, class O> inline B* RH3DM_Adopt(void* p) { return p ? new B((O*)p) : nullptr; }
// `(Brep)item` on a class argument (G117): the object as the derived class, or C#'s InvalidCastException
template<class T> inline T& RH3DM_CastChecked(T* p) { if (!p) throw std::runtime_error("Specified cast is not valid."); return *p; }
// G122: an OWNED generated object as a derived generated class: (T) throws, `as T` is null (the object is freed)
template<class T, class B> inline T* RH3DM_OwnedCast(B* p, bool must) { if (!p) return nullptr; T* r = dynamic_cast<T*>(p); if (!r) { delete p; if (must) throw std::runtime_error("Specified cast is not valid."); } return r; }
// a native's new object adopted into a class stored BY VALUE (G115: `new EarthAnchorPoint(ptr)`):
// copied in, then released with the type's own Delete; C# would wrap a null pointer -- an error here
// `x.ConstPointer()` on a class stored BY VALUE (G115): its stored object, by reference or by
// pointer (a null one throws, as C#'s NullReferenceException)
template<class G> inline void* RH3DM_ValPtr(const G& o) { return (void*)&o.m_val; }
template<class G> inline void* RH3DM_ValPtr(const G* o) { if (!o) throw std::runtime_error("null reference"); return (void*)&o->m_val; }
template<class B, class O, void (*D)(O*)> inline B RH3DM_ValAdopt(void* p)
{
  if (!p) throw std::runtime_error("null native object");
  B rc(*(O*)p);
  D((O*)p);
  return rc;
}

// a class parameter (a const reference) into a generated member taking it by reference or
// as an optional pointer (G91)
template<class T> struct RH3DM_CArg
{
  const T& r;
  operator const T&() const { return r; }
  operator const T*() const { return &r; }
};
template<class T> inline RH3DM_CArg<T> RH3DM_ClassArg(const T& r) { return RH3DM_CArg<T>{r}; }

// a C# enum passed `ref` to a native (G91): the native's own enum type (any underlying size)
// as a temporary, written back to the binding's enum when the call's full expression ends
template<class E> struct RH3DM_ERef
{
  E& e;
  alignas(8) unsigned char buf[8];
  void (*back)(const unsigned char*, E&) = nullptr;
  explicit RH3DM_ERef(E& x) : e(x) {}
  RH3DM_ERef(const RH3DM_ERef&) = delete;
  ~RH3DM_ERef() { if (back) back(buf, e); }
  template<class T, class = typename std::enable_if<std::is_enum<T>::value || std::is_integral<T>::value>::type>
  operator T*()
  {
    static_assert(sizeof(T) <= sizeof(buf), "enum too large");
    T* t = new (buf) T((T)(int)e);
    back = [](const unsigned char* b, E& out) { out = (E)(int)*(const T*)b; };
    return t;
  }
};
template<class E> inline RH3DM_ERef<E> RH3DM_EnumRef(E& e) { return RH3DM_ERef<E>(e); }

// `Enum.IsDefined(typeof(E), x)` (G91): x is one of E's declared values
inline bool RH3DM_IsDefined(int v, std::initializer_list<int> values) { for (int x : values) if (x == v) return true; return false; }

// a Guid in a class body (G91): ON_UUID inside; the binding's uuid (BND_UUID) at a public
// generated member -- RH3DM_ToUuid reads either, RH3DM_Uuid passes to either
inline ON_UUID RH3DM_ToUuid(const ON_UUID& u) { return u; }
inline ON_UUID RH3DM_ToUuid(const BND_UUID& u) { return Binding_to_ON_UUID(u); }
struct RH3DM_Uuid
{
  ON_UUID u;
  RH3DM_Uuid(const ON_UUID& x) : u(x) {}
  operator ON_UUID() const { return u; }
  operator BND_UUID() const { return ON_UUID_to_Binding(u); }
};

// a File3dm view handed back (G89: TryConvertBrep): a new view of the same object
template<class T> T* RH3DM_ViewCopyOf(const T* p) { return p->RH3DM_ViewCopy(); }

// `Duplicate() as Brep` (G74): the fresh object downcast; freed when it is not one
template<class T, class S> T* RH3DM_As(S* p)
{
  if (!p) return nullptr;
  T* t = dynamic_cast<T*>(p);
  if (!t) delete p;
  return t;
}

// `return (1 == rc || 3 == rc);` -- the C# result test, as written (G49)
template <typename T, typename... V> inline bool RH3DM_In(T v, V... vals) { return ((v == vals) || ...); }

// a guard's File.Exists(path) / FileNotFoundException (File3dm.Read)
struct RH3DM_FileNotFound : std::runtime_error { using std::runtime_error::runtime_error; };
inline bool RH3DM_FileExists(const std::wstring& path) { return ON_FileSystem::IsFile(path.c_str()); }

// `File.ReadAllBytes(path)` in a translated body (G107): the file's bytes, FileNotFoundException if absent
inline RH3DM_Bytes RH3DM_ReadAllBytes(const std::wstring& path) {
  if (!ON_FileSystem::IsFile(path.c_str())) throw RH3DM_FileNotFound(ON_String(path.c_str()).Array());
  FILE* fp = ON_FileStream::Open(path.c_str(), L"rb");
  if (!fp) throw RH3DM_FileNotFound(ON_String(path.c_str()).Array());
  // straight into the heap vector: no stack buffer (a 64 KB one overflowed WASM's 64 KB stack, G107)
  RH3DM_Bytes b;
  if (fseek(fp, 0, SEEK_END) == 0) {
    long size = ftell(fp);
    if (size > 0 && fseek(fp, 0, SEEK_SET) == 0) {
      b.v.resize((size_t)size);
      b.v.resize(fread(b.v.data(), 1, (size_t)size, fp));
    }
  }
  ON_FileStream::Close(fp);
  return b; }

// System.Drawing.Color across the C layer, exactly as the C# call sites do
// it. BND_Color's alpha is OPACITY (the old bindings' convention, = .NET's
// Color.A); ON_Color's is transparency.
inline BND_Color RH3DM_ColorFromArgb(int argb)       // Color.FromArgb(int)
{ return ON_Color_to_Binding(ON_Color((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF, 255 - ((argb >> 24) & 0xFF))); }
// Color.FromArgb(r, g, b) / (a, r, g, b): each component 0..255, else .NET's ArgumentException (G128)
inline int RH3DM_ColorByte(int v, const char* name)
{
  if (v < 0 || v > 255)
    throw std::invalid_argument("Value of '" + std::to_string(v) + "' is not valid for '" + name + "'. '" + name
                                + "' should be greater than or equal to 0 and less than or equal to 255.");
  return v;
}
inline BND_Color RH3DM_ColorFromArgb4(int a, int r, int g, int b)
{ return ON_Color_to_Binding(ON_Color(RH3DM_ColorByte(r, "red"), RH3DM_ColorByte(g, "green"), RH3DM_ColorByte(b, "blue"), 255 - RH3DM_ColorByte(a, "alpha"))); }
// an object made only to pass to a call (G130: `Append(new LineCurve(line))`): owned, freed when the
// call's full expression ends -- the callee copies it (each member this unlocks is reviewed)
template<class T> struct RH3DM_TmpArgT
{
  T* p;
  explicit RH3DM_TmpArgT(T* x) : p(x) {}
  RH3DM_TmpArgT(const RH3DM_TmpArgT&) = delete;
  ~RH3DM_TmpArgT() { delete p; }
  operator const T*() const { return p; }
  operator const T&() const { return *p; }
};
template<class T> inline RH3DM_TmpArgT<T> RH3DM_TmpArg(T* p) { return RH3DM_TmpArgT<T>(p); }
// a Color's components as .NET reads them (G130): bytes, A the opacity (ON_Color's alpha is transparency)
inline int RH3DM_ColorA(const BND_Color& v) { return 255 - Binding_to_ON_Color(v).Alpha(); }
inline int RH3DM_ColorR(const BND_Color& v) { return Binding_to_ON_Color(v).Red(); }
inline int RH3DM_ColorG(const BND_Color& v) { return Binding_to_ON_Color(v).Green(); }
inline int RH3DM_ColorB(const BND_Color& v) { return Binding_to_ON_Color(v).Blue(); }
inline BND_Color RH3DM_ColorFromWin32(int abgr)      // Interop.ColorFromWin32: always opaque
{ return ON_Color_to_Binding(ON_Color(abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF, 0)); }
inline int RH3DM_ColorToArgb(const BND_Color& v)     // Color.ToArgb()
{ ON_Color c = Binding_to_ON_Color(v);
  return (int)(((unsigned int)(255 - c.Alpha()) << 24) | ((unsigned int)c.Red() << 16) | ((unsigned int)c.Green() << 8) | (unsigned int)c.Blue()); }

// CRhCmnStringHolder::Array() yields RHMONO_STRING: wchar_t on Windows,
// UTF-16 (ON__UINT16) on Linux/Android/WASM -- convert accordingly.
inline std::wstring RH3DM_W(const CRhCmnStringHolder& h)
{
  const RHMONO_STRING* a = h.Array();
  if (!a) return std::wstring();
#if defined(ON_RUNTIME_LINUX) || defined(ON_RUNTIME_ANDROID) || defined(ON_RUNTIME_WASM)
  int n = 0;
  while (a[n]) n++;
  std::wstring out;
  out.resize((size_t)n + 8);
  unsigned int err = 0;
  int wn = ON_ConvertUTF16ToWideChar(false, (const ON__UINT16*)a, n,
                                     &out[0], (int)out.size(), &err, 0xFFFFFFFF,
                                     0xFFFD, nullptr);
  out.resize(wn > 0 ? (size_t)wn : 0);
  return out;
#else
  return std::wstring((const wchar_t*)a);
#endif
}

// The inverse: a binding-side std::wstring becomes the C layer's
// RHMONO_STRING* (wchar_t on Windows; UTF-16 elsewhere). Returned as a
// vector (not basic_string: char_traits for non-char types is gone from
// newer standard libraries); the temporary lives to the end of the full
// expression, which is exactly the C call.
// NOT via ON_ConvertWideCharToUTF16: its opennurbs definition takes char*
// where the extern "C" declaration says ON__UINT16*, so the declared symbol
// exists in no build (pilot-findings.md #13). wchar_t is UTF-32 on every
// non-Windows target, so call the branch it would have dispatched to.
inline std::vector<RHMONO_STRING> RH3DM_S(const std::wstring& s)
{
  std::vector<RHMONO_STRING> out(s.size() * 2 + 2, 0);
#if defined(ON_RUNTIME_LINUX) || defined(ON_RUNTIME_ANDROID) || defined(ON_RUNTIME_WASM) || defined(__APPLE__)
  unsigned int err = 0;
  ON_ConvertUTF32ToUTF16(false, (const ON__UINT32*)s.c_str(), (int)s.size(),
                         (ON__UINT16*)out.data(), (int)out.size() - 1,
                         &err, 0xFFFFFFFF, 0xFFFD, nullptr);
#else
  for (size_t i = 0; i < s.size(); i++) out[i] = (RHMONO_STRING)s[i];
#endif
  return out;
}

''' + '\n'.join(['enum %s : int;' % e for e in enum_decls]
            + ['enum %s%s : %s;' % ('class ' if c_enum_defs[e][0] else '', e, c_enum_defs[e][1])
               for e in bare_enums] + ['class %s;' % c for c in opaque_classes] + gen_enum_defs
                + ['']) \
        + '\n'.join(sorted(decls)) + '\n\n' \
        + '\n'.join('class %s;' % cls['gen'] for cls in CLASSES) + '\n\n' \
        + '// subclass-aware wrapping: the most-derived generated class (gen_wrap.cpp)\n' \
        + '\n'.join('%s* RH3DM_Wrap_%s(%s* p);' % (c['gen'], c['reg'], c['on'])
                    for c in CLASSES if c['mode'] in ('pointer', 'abstract') and not c.get('comp') and not c.get('handle') and not c.get('borrowed') and not c.get('shared')) + '\n' \
        + '// views inside another object (G58): the most-derived generated class (gen_wrap.cpp)\n' \
        + '\n'.join('%s* RH3DM_Sub_%s(ON_Object* (*fn)(void*, int, bool), void* owner, int key, bool cow);'
                    % (t['gen'], t['reg']) for t in sub_targets) + '\n' \
        + '// views of a File3dmObject\'s geometry: the most-derived generated class (gen_wrap.cpp)\n' \
        + '\n'.join('%s* RH3DM_View_%s(ONX_Model* model, ON_UUID id);' % (root['gen'], root['reg'])
                    for root, _, _ in view_roots) + '\n'

    def fname(cls):
        return 'gen_%s' % cls['reg'].lower()

    for cls, plans in per_class:
        # the header carries declarations only: it needs just gen_common.h
        # (forward decls cover pointer returns) and the base class, whose
        # complete type inheritance requires
        base_inc = ('#include "gen_%s.h"\n' % cls['base'].lower()
                    if cls.get('base') else '')
        table_decl, table_def = (emit_table_iter(cls) if cls.get('table') else
                                 emit_linked_iter(cls) if cls.get('liter') else ('', ''))
        files[fname(cls) + '.h'] = (
            BANNER + '#pragma once\n#include "gen_common.h"\n' + base_inc
            # a modeled field's class (G88): complete here, the field deletes what it owns
            + ''.join('#include "gen_%s.h"\n' % BY_CS[v['cs']]['reg'].lower()
                      for v in (cls.get('modeled') or {}).values() if cls.get('mf_active') and v['cs'] in BY_CS)
            # a cached view's class (G89): complete here, the cache deletes what it holds
            + ''.join('#include "gen_%s.h"\n' % r.lower() for r in sorted({p['shape'][1]['reg'] for p in plans if p['shape'][0] == 'cachedview'}))
            + '\n' + emit_class(cls, plans, bool(ctors[cls['reg']]), argctors[cls['reg']]) + table_decl)

        # cross-class returns need the target's complete type where the
        # BODIES live -- the cpp
        # (an optional argument, `const X*`, dereferences too: `x ? x->m_ptr : nullptr`)
        arg_classes = {c['reg'] for p in plans + argctors[cls['reg']] for t, _ in p['bind_args']
                       for c in CLASSES if t in ('const %s&' % c['gen'], 'const %s*' % c['gen'])}
        deps = sorted(({BY_CS[p['shape'][1]]['reg'] for p in plans
                        if p['shape'][0] == 'wrap'}
                       | {p['shape'][1]['reg'] for p in plans if p['shape'][0] == 'fill'}
                       | {p['shape'][1]['reg'] for p in plans if p['shape'][0] in ('child', 'construct', 'via', 'newobj', 'valadopt', 'subview')}
                       | {p['via_plan']['shape'][1]['reg'] for p in plans if p['shape'][0] == 'via'}
                       | {p['shape'][1]['reg'] for p in plans if p['shape'][0] == 'via'}           # the list it builds
                       | ({cls['liter']['item']['reg']} if cls.get('liter') else set())
                       | {p['shape'][1]['reg'] for p in plans if p['shape'][0] in ('constview', 'fileview', 'settingsview')}   # the view it constructs
                       | ({cls['table']['item']['reg']} if cls.get('table') else set())
                       # every class a translated body calls into (G92: a temporary's members,
                       # `obj.Attributes->Gen_GetGroupList_vec()`), and the classes those return
                       | {d['cls']['reg'] for p in plans if p.get('translated') for d in p.get('deps') or []
                          if d.get('cls') and d['cls']['reg'] != cls['reg']}
                       | {c['reg'] for p in plans if p.get('translated') for c in CLASSES
                          if re.search(r'\b%s\b' % re.escape(c['gen']), p['shape'][2] if len(p['shape']) > 2 and isinstance(p['shape'][2], str) else '')
                          and c['reg'] != cls['reg']}
                       # objects placed in tuples (SetTuple needs the complete class): a tuple led
                       # by a wrapped result (G73), a wrap tuple's elements (G72)
                       | {m for p in plans if p['shape'][0] == 'tuple' and isinstance(p['shape'][1], tuple)
                          for m in re.findall(r'RH3DM_Wrap_(\w+)\(', p['shape'][1][2])}
                       | {m for p in plans if p['shape'][0] == 'wraptuple'
                          for pair in re.findall(r'RH3DM_Wrap_(\w+)\(|new (BND_\w+)\(', p['shape'][1]['item'])
                          for m in [pair[0] or next((c['reg'] for c in CLASSES if c['gen'] == pair[1]), None)] if m}
                       | ({cls['proxy']['Owner']} if (cls.get('proxy') or {}).get('Origin') == 'parent' else set())
                       # classes a translated body names (G74)
                       | {m for p in plans if p['shape'][0] in ('translated', 'translatedout')
                          for m in re.findall(r'\bBND_(\w+)\b', p['shape'][1] + ' ' + p['shape'][2] + ' ' + ' '.join(t for t, _ in p['bind_args']))
                          if m in BY_REG}
                       | arg_classes) - {cls['reg']})
        # data classes (G64): returned by a bridge, or taken as arguments (dereferenced here)
        data_deps = sorted({p['shape'][1] for p in plans if p['shape'][0] in ('datafrom', 'datafill', 'holderdata')}
                           | {cs for p in plans for t, _ in p['bind_args'] for cs, cpp in DATA_CPP.items()
                              if t in ('const %s*' % cpp, 'const %s&' % cpp)}
                           | {cs for p in plans for l in p.get('locals') or [] for cs, cpp in DATA_CPP.items()
                              if re.match(r'^%s\b' % re.escape(cpp), l)})
        dep_includes = ''.join('#include "gen_%s.h"\n' % d.lower() for d in deps + data_deps) \
            + ''.join('#include "../%s"\n' % h for h in sorted({x['header'] for r in hand_chain(cls) for x in HAND_MEMBERS.get(r, []) if 'header' in x}
                                                                       | {x['header'] for x in HAND_FALLBACKS.get(cls['reg'], [])}
                | {st['header'] for st in HAND_STANDINS.values()
                   if any('new %s(' % st['gen'] in (p['shape'][2] if p['shape'][0] == 'translated' else '') for p in plans)}))
        defs = '\n\n'.join(emit_decl_def(p, cls)[1] for p in plans) + table_def
        pvp = cls.get('proxy') or {}
        if pvp.get('Origin') == 'parent':
            # the owner's const pointer, where the owner's class is complete (G57)
            own = BY_REG[pvp['Owner']]
            oarg = C_BY_NAME[pvp['Resolver']]['params'][0]['type']
            defs = ('%s %s::RH3DM_PvOwner(const %s* owner)\n{\n  return (%s)%s;\n}\n\n'
                    % (oarg, cls['gen'], own['gen'], oarg,
                       'owner->RH3DM_Const()' if own.get('proxy') else 'owner->RH3DM_Val()' if own.get('fileview')
                       else '&owner->m_val' if own['mode'] == 'value' else 'owner->m_ptr')) + defs

        py_block, js_block = reg_blocks[cls['reg']]
        if cls['mode'] == 'abstract':
            ctor = ''
        else:
            basecls = BY_REG.get(cls.get('base', ''))
            if basecls and basecls['mode'] != 'abstract':
                member = basecls['gen']       # forward to the owning base
            else:
                member = 'm_ptr' if cls['mode'] == 'pointer' else 'm_val'
            ctor = ''
            # a value-mode class copies the created object in and frees it (G74)
            body = ('\n  : {member}({expr}) {{}}\n' if cls['mode'] != 'value'
                    else '\n{{ {on}* rh3dm_p = {expr}; m_val = *rh3dm_p; delete rh3dm_p; }}\n')
            if ctors[cls['reg']] and not cls.get('ff'):      # a free-floating class declares its own
                ctor += ('\n{gen}::{gen}()' + body).format(
                    gen=cls['gen'], member=member, on=cls['on'], expr=ctors[cls['reg']][0])
            for p in argctors[cls['reg']]:
                ctor += ('\n{gen}::{gen}({sig})' + body).format(
                    gen=cls['gen'], member=member, on=cls['on'], expr=p['ctor_call'],
                    sig=', '.join('%s %s' % a for a in p['bind_args']))
        files[fname(cls) + '.cpp'] = BANNER + '''#include "{hdr}.h"
{dep_includes}{ctor}
{defs}

#if defined(ON_PYTHON_COMPILE)
void initGen{reg}(rh3dmpymodule& m)
{{
{py_block}
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
void initGen{reg}(void)
{{
{js_block}
}}
#endif
'''.format(hdr=fname(cls), dep_includes=dep_includes,
           ctor=ctor, defs=defs, reg=cls['reg'],
           py_block=py_block, js_block=js_block)

    def depth(c):
        n, b = 0, c.get('base')
        while b:
            n, b = n + 1, BY_REG[b].get('base')
        return n

    def derives(d, c):
        b = d.get('base')
        while b:
            if b == c['reg']:
                return True
            b = BY_REG[b].get('base')
        return False

    wrap = []
    # components (G43) are never owned objects: an ON_BrepFace* wraps as its first non-component base
    for c in [c for c in CLASSES if c['mode'] in ('pointer', 'abstract') and not c.get('comp') and not c.get('handle') and not c.get('borrowed') and not c.get('shared')]:
        body = ['%s* RH3DM_Wrap_%s(%s* p)\n{' % (c['gen'], c['reg'], c['on']),
                '  if (!p) return nullptr;']                    # a null result is None/null (G73)
        for d in sorted((d for d in CLASSES if d['mode'] == 'pointer' and derives(d, c) and not d.get('comp') and not d.get('handle') and not d.get('borrowed') and not d.get('shared')),
                        key=lambda d: (-depth(d), d['reg'])):
            body.append('  if (%s* d = %s::Cast(p)) return new %s(d);'
                        % (d['on'], d['on'], d['gen']))
        if c['mode'] == 'abstract':
            # an abstract root (GeometryBase; G69): a type with no generated class is
            # no object -- freed, since the caller owns it
            body.append('  delete p;\n  return nullptr;   // a geometry type with no generated class\n}')
        else:
            body.append('  return new %s(p);\n}' % c['gen'])
        wrap.append('\n'.join(body))
    # RH3DM_View_<root>(model, id): CreateGeometryHelper(ptr, fileObject) -- the
    # most-derived VIEW class of the object's geometry (resolved once to choose)
    for root, viewers, r in view_roots:
        vb = ['%s* RH3DM_View_%s(ONX_Model* model, ON_UUID id)\n{' % (root['gen'], root['reg']),
              '  const %s* p = %s(model, id);' % (r['returns'].replace('const ', '').rstrip('*').strip(), r['name']),
              '  if (!p) return nullptr;']
        for d in sorted(viewers, key=lambda d: (-depth(d), d['reg'])):
            vb.append('  if (%s::Cast(p)) return new %s(model, id);' % (d['on'], d['gen']))
        vb.append('  return nullptr;   // a geometry type with no generated class\n}')
        wrap.append('\n'.join(vb))
    # RH3DM_Sub_<T>(fn, owner, key, cow): CreateGeometryHelper on the resolved
    # pointer -- the most-derived generated class hosting the view state (G58)
    for t in sub_targets:
        hosts = [d for d in CLASSES if d['mode'] == 'pointer' and (d is t or derives(d, t)) and not d.get('comp')
                 and storage_owner(d).get('comp_host')]
        sb = ['%s* RH3DM_Sub_%s(ON_Object* (*fn)(void*, int, bool), void* owner, int key, bool cow)\n{' % (t['gen'], t['reg']),
              '  ON_Object* p = fn(owner, key, false);',
              '  if (!p) return nullptr;']
        for d in sorted(hosts, key=lambda d: (-depth(d), d['reg'])):
            sb.append('  if (%s::Cast(p)) return new %s(fn, owner, key, cow);' % (d['on'], d['gen']))
        sb.append('  return nullptr;   // a geometry type with no generated class\n}')
        wrap.append('\n'.join(sb))
    files['gen_wrap.cpp'] = (BANNER + ''.join('#include "gen_%s.h"\n' % c['reg'].lower()
                                             for c in CLASSES) + '\n' + '\n\n'.join(wrap) + '\n')

    inits_py = '\n'.join('  initGen%s(m);' % cls['reg'] for cls in CLASSES)
    inits_js = '\n'.join('  initGen%s();' % cls['reg'] for cls in CLASSES)
    # iterable in JS only with a generated iterator, or count + get to build one from: a list whose
    # indexer is not generated is reported, not promised (G101). Embind classes inherit instance
    # members, so a base's registrations count.
    js_members, js_base = {}, {}
    for text in files.values():
        if 'emscripten' not in text:
            continue
        for blk in re.split(r'(?=\bclass_<)', text.split('#if defined(ON_WASM_COMPILE)')[-1])[1:]:
            mm = re.match(r'class_<\s*\w+(?:\s*,\s*base<(\w+)>)?\s*>\s*\("(\w+)"\)', blk)
            if not mm:
                continue
            js_members.setdefault(mm.group(2), set()).update(re.findall(r'\.(?:function|property)\("(\w+)"', blk))
            if mm.group(1):
                js_base[mm.group(2)] = mm.group(1)
    gen_to_js = {}
    for text in files.values():
        for g, n in re.findall(r'class_<(\w+)[^>]*>\("(\w+)"\)', text):
            gen_to_js[g] = n
    def has(c, name):
        while c:
            if name in js_members.get(c, ()):
                return True
            c = gen_to_js.get(js_base.get(c))
        return False
    grids = {g[0] for g in JS_POST['grid']}
    keep = [c for c in JS_POST['enumerable'] if c not in grids and (has(c, 'iterator') or (has(c, 'count') and has(c, 'get')))]
    JS_POST['not_iterable'] = sorted(set(JS_POST['enumerable']) - set(keep) - grids)
    JS_POST['enumerable'] = keep
    JS_POST['aliases'].extend(hand_js_aliases())   # G124
    _hk, _he = hand_js_post()                          # G129
    JS_POST['keep'].extend(_hk)
    JS_POST['enumerable'].extend(_he)
    files['gen_post.js'] = js_post_script()
    files['gen_pre.js'] = js_pre_script()
    if JS_POST['not_iterable']:
        # IEnumerable in C#, no for...of in JS: the list's indexer (or enumerator) is not generated
        print('  not iterable in JS (no indexer generated): %s' % ', '.join(sorted(JS_POST['not_iterable'])))
    hand_cls = hand_class_registrations()   # G123: holders for hand members with no generated class
    # G132: RhinoCommon's static AND instance method under one name (BoundingBox.Union(a, b) and
    # bbox.Union(other)). pybind11 and nanobind both refuse to overload a name across the two, so the
    # emitters register the static as `_static_<Name>`; once every class exists, one descriptor per
    # pair answers the static on the class and the instance method on an instance. Run at module init
    # (not in rhino3dm/__init__.py), so `import _rhino3dm` has it too.
    PY_STATIC_OR_INSTANCE = '''
  py::exec(R"py(
class _StaticOrInstance(object):
    def __init__(self, instance_method, static_method):
        self._instance, self._static = instance_method, static_method
        self.__doc__ = '\\n'.join(d for d in (static_method.__doc__, instance_method.__doc__) if d)
    def __get__(self, obj, cls=None):
        return self._static if obj is None else self._instance.__get__(obj, cls)
for _cls in list(vars(_m).values()):
    if isinstance(_cls, type):
        for _attr in [a for a in vars(_cls) if a.startswith('_static_')]:
            _inst = vars(_cls).get(_attr[len('_static_'):])
            if _inst is not None:
                setattr(_cls, _attr[len('_static_'):], _StaticOrInstance(_inst, getattr(_cls, _attr)))
)py", py::dict(py::arg("_m") = m));
'''
    files['gen_init.cpp'] = BANNER + '''#include "gen_common.h"

{hand_inc}
#if defined(ON_PYTHON_COMPILE)
#include <pybind11/eval.h>   // py::exec (G132)
{py_decls}
{vt_py_decls}
void initGeneratedBindings(rh3dmpymodule& m)
{{
  // a guard's FileNotFoundException (File3dm.Read) is Python's FileNotFoundError
  py::register_exception_translator([](std::exception_ptr p) {{
    try {{ if (p) std::rethrow_exception(p); }}
    catch (const RH3DM_FileNotFound& e) {{ PyErr_SetString(PyExc_FileNotFoundError, e.what()); }}
  }});
{enums_py}
{vt_py}{inits_py}
{hand_py}{enums_py_nested}{py_static_or_instance}
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
{js_decls}
{vt_js_decls}
void initGeneratedBindings(void)
{{
{enums_js}
{vt_js}{inits_js}
{hand_js}{js_iter}}}
#endif
'''.format(
        py_decls='\n'.join('void initGen%s(rh3dmpymodule&);' % cls['reg']
                           for cls in CLASSES),
        js_decls='\n'.join('void initGen%s(void);' % cls['reg']
                           for cls in CLASSES),
        enums_py='\n'.join(gen_enum_py), enums_js='\n'.join(gen_enum_js),
        enums_py_nested='\n'.join(gen_enum_py_nested),
        py_static_or_instance=PY_STATIC_OR_INSTANCE,
        inits_py=inits_py, inits_js=inits_js,
        # hand-written bindings (G90): after every generated class (they may derive from one)
        hand_inc=''.join('#include "../%s"\n' % h['header'] for h in HAND_BINDINGS) + hand_cls[0],
        hand_py=''.join('  %s(m);\n' % h['init'] for h in HAND_BINDINGS) + hand_cls[1],
        hand_js=''.join('  %s();\n' % h['init'] for h in HAND_BINDINGS) + hand_cls[2],
        vt_py_decls=''.join('void initGen%s(rh3dmpymodule&);\n' % r for r in vt_regs),
        vt_js_decls=''.join('void initGen%s(void);\n' % r for r in vt_regs),
        vt_py=''.join('  initGen%s(m);\n' % r for r in vt_regs),
        vt_js=''.join('  initGen%s();\n' % r for r in vt_regs),
        # `for (const x of table)`: embind cannot register a Symbol-keyed method,
        # so once every class exists, alias the registered `iterator()` under
        # Symbol.iterator on the table's prototype
        js_iter=''.join('  { emscripten::val proto = emscripten::val::module_property("%s")["prototype"];\n'
                        '    proto.set(emscripten::val::global("Symbol")["iterator"], proto["iterator"]); }\n'
                        % r for r in [c['reg'] for c in CLASSES if c.get('table') or c.get('liter')]
                                      + [t for h in HAND_BINDINGS for t in h.get('iter_tables', [])]))

    # the C-layer translation units the generated code calls into, from the
    # manifest's defining file per function -- replaces discovering the link
    # closure one linker error at a time
    used = {re.search(r'\b(\w+)\(', d.split('"C"')[1]).group(1) for d in decls}
    native = os.path.join(ROOT, 'src', 'librhino3dm_native')
    # 55 exports are declared RH_C_FUNCTION in rhcommon_c.h but DEFINED in a
    # .cpp without the macro (the manifest records the header): find the
    # definition -- a line opening with the return type, not a call
    defined_in = {}
    for f in sorted(os.listdir(native)):
        if f.endswith('.cpp'):
            for line in open(os.path.join(native, f), errors='replace'):
                mm = re.match(r'^(?:RH_C_FUNCTION\s+)?[A-Za-z_][\w:<> ]*[\s*&]+(\w+)\s*\(', line)
                if mm and not line.rstrip().endswith(';'):
                    defined_in.setdefault(mm.group(1), f)
    tus = set()
    for n in used:
        f = c_by_name[n]['file'] if n in c_by_name else ''
        if not f.endswith('.cpp') or not os.path.exists(os.path.join(native, f)):
            f = defined_in.get(n, '')
        if f:
            tus.add(f)
        else:
            raise SystemExit('no defining C-layer file for %s' % n)
    # transitive closure over free-function helpers the C layer calls across
    # files (FromCircleStruct -> on_circle.cpp, ...): each listed file's
    # calls to functions DEFINED in another C-layer file pull that file in
    texts = {}
    def text(f):
        if f not in texts:
            texts[f] = open(os.path.join(native, f), errors='replace').read()
        return texts[f]
    frontier = set(tus)
    while frontier:
        nxt = set()
        for f in frontier:
            for ident in set(re.findall(r'\b([A-Za-z_]\w+)\s*\(', text(f))):
                g = defined_in.get(ident)
                if g and g not in tus and not re.search(r'^[A-Za-z_][\w:<> ]*[\s*&]+%s\s*\(' % ident,
                                                        text(f), re.M):
                    nxt.add(g)
        tus |= nxt
        frontier = nxt
    tus = sorted(tus)
    files['clayer_sources.cmake'] = (
        '# GENERATED by tools/generate/bnd_gen.py -- DO NOT EDIT.\n'
        '# C-layer sources defining every flat C function the generated bindings\n'
        '# call (%d functions, %d files). Helper TUs those reach indirectly are\n'
        '# listed by hand in src/CMakeLists.txt.\n'
        'set(generated_clayer_SRC\n%s)\n'
        % (len(used), len(tus), ''.join('  "${CMAKE_CURRENT_SOURCE_DIR}/librhino3dm_native/%s"\n' % t
                                        for t in tus)))

    # M3 layout (Luis 2026-11-01): the generated files sit beside the hand-maintained ones in
    # src/bindings, named bnd_<RhinoCommon name>_gen.*
    files = gen_layout(files, vt['types'] if os.path.exists(vt_path) else [])
    # RH3DM_GEN_OUT: dry-run elsewhere (e.g. while a build reads src/bindings)
    dry_run = bool(os.environ.get('RH3DM_GEN_OUT'))
    out_dir = os.environ.get('RH3DM_GEN_OUT') or os.path.join(ROOT, 'src', 'bindings')
    # the surface lock: a member that disappears or changes is a regression
    # unless accepted explicitly (RH3DM_ACCEPT=1); nothing is written then
    import surface_lock
    ok, lost, gained = surface_lock.check(files, os.path.join(ROOT, 'api', 'generated-surface.lock'), dry_run)
    if lost:
        print('surface lock: %d registration(s) %s:' % (len(lost), 'ACCEPTED as removed' if ok else 'REMOVED or changed'))
        for l in lost:
            print('  - ' + l)
    if not ok:
        print('refusing to write the generated files: rerun with RH3DM_ACCEPT=1 if intended')
        return 1
    if gained:
        print('surface lock: +%d registration(s)' % len(gained))
    os.makedirs(out_dir, exist_ok=True)
    wrote, kept = 0, 0
    for name, content in files.items():
        path = os.path.join(out_dir, name)
        if os.path.exists(path) and open(path, encoding='utf-8').read() == content:
            kept += 1          # unchanged: keep mtime, incremental build skips it
            continue
        with open(path, 'w', encoding='utf-8', newline='\n') as f:
            f.write(content)
        wrote += 1
    # output hygiene: a generated file this run did not produce is STALE and
    # would compile silently -- remove it. Only *_gen.* files: the hand-maintained
    # files share the directory
    removed = []
    for name in sorted(os.listdir(out_dir)):
        if re.search(r'_gen\.(cpp|h|js|cmake)$', name) and name not in files:
            os.remove(os.path.join(out_dir, name))
            removed.append(name)

    for cls, plans in per_class:
        props = sum(1 for p in plans if p['m']['Kind'] == 'property')
        print('  %-10s %d members (%d properties) -> %s.{h,cpp}'
              % (cls['reg'], len(plans), props, gen_stem(cls['cs'])))
    print('src/bindings/*_gen.*: %d members, %d files (%d written, %d unchanged%s)'
          % (len(all_plans), len(files), wrote, kept,
             ', removed stale: ' + ', '.join(removed) if removed else ''))
    if js_pruned:
        print('  js-only same-arity prunes (%d): %s'
              % (len(js_pruned), ', '.join(js_pruned)))
    for why, n in sorted(skipped.items(), key=lambda kv: -kv[1]):
        print('  skipped %-38s %d' % (why, n))
    return 0


if __name__ == '__main__':
    sys.exit(main())
