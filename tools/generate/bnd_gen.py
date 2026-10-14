"""Pilot binding generator v3: registry-driven, multi-class.

v2 proved the pipeline on Curve but was Curve-shaped throughout. v3 moves
everything class-specific into CLASSES, the wrapper-type registry:

    C# type <-> ON_* type <-> hand BND class (copy source) <-> generated class

One generated file defines every class (in registry order, so cross-class
returns like Surface.IsoCurve -> CurveX see a complete type) and registers
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


def _derives(d, base):
    b = BY_REG.get(d.get('base', ''))
    while b is not None:
        if b is base:
            return True
        b = BY_REG.get(b.get('base', ''))
    return False


def scope_pass(per_class, manifest, c_by_name, skipped):
    """G77: a scoped child view (BrepLoopListX: the brep's loops, or one face's) plans
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


def main():
    # RH3DM_MANIFEST: dry runs against a candidate manifest (with RH3DM_GEN_OUT)
    manifest = json.load(open(os.environ.get('RH3DM_MANIFEST') or os.path.join(ROOT, 'api', 'manifest.json')))
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
    _planner.CTORS = {cls['reg']: bool(default_ctor_call(cls, manifest, c_by_name)
                                       or (cls.get('ff') and cls['ff']['New'] in c_by_name)) for cls in CLASSES}
    _planner.ARGCTORS = {cls['reg']: arg_ctor_plans(cls, manifest, c_by_name, {}) for cls in CLASSES}
    translated_pass(per_class, manifest, skipped)        # G63: translated pure members
    refuse_overridden(manifest, per_class, skipped)
    refuse_overridden_count(per_class, skipped)
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
        li = linked_iter(cls, plans) or snapshot_iter(cls, plans, manifest)
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
    enum_decls = sorted({t[5:] for p in native_plans if p['fn'] for t in
                         ([p['fn']['returns']] + [q['type'] for q in p['fn']['params']])
                         if t.startswith('enum ')})
    # C enums spelled bare in a signature (TextureMappingGetTransform) and
    # defined only inside a C .cpp: opaque-declare with their fixed type
    c_enum_defs = {}
    for f in glob.glob(os.path.join(ROOT, 'src', 'librhino3dm_native', '*.cpp')):
        for cls_kw, ename, under in re.findall(r'\benum\s+(class\s+)?(\w+)\s*:\s*([\w ]+?)\s*\{',
                                               open(f, errors='replace').read()):
            c_enum_defs.setdefault(ename, (bool(cls_kw), under.strip()))
    bare_enums = sorted({t for p in native_plans if p['fn'] for t in
                         ([p['fn']['returns']] + [q['type'] for q in p['fn']['params']])
                         if t in c_enum_defs})

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
                return bool(re.match(r'^-?(\d+|0x[0-9A-Fa-f]+)$', e)) or e in names[:i]
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
            gen_enum_py.append('  py::enum_<%s>(m, "%s")\n%s;' % (cpp, ename, vals))
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
            mut = {k for m in vt['members'] if m['Mutates'] for k in m['Keys']}
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
        + ([c['liter']['first']['name'], c['liter']['next']['name']] if c.get('liter') and not c['liter'].get('snapshot') else [])
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
        vt_regs = [t['Cs'] + 'X' for t in vt['types'] if not t['IsStaticClass']]
        files.update(emit_valuetypes(vt, emit_valuetype_bindings(vt, skipped)))
    else:
        vt_regs = []

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
''' + vt_includes + vt_pub + '''inline BND_Point3dX RH3DM_P(const ON_3dPoint& p) { BND_Point3dX r; r.m_x = p.x; r.m_y = p.y; r.m_z = p.z; return r; }
inline BND_Vector3dX RH3DM_V(const ON_3dVector& v) { BND_Vector3dX r; r.m_x = v.x; r.m_y = v.y; r.m_z = v.z; return r; }
inline BND_IntervalX RH3DM_I(const ON_Interval& i) { BND_IntervalX r; r.m_t0 = i.m_t[0]; r.m_t1 = i.m_t[1]; return r; }

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
// a DateTime result (File3dm.Created; G68): a JS Date (local time, month 0-based)
typedef emscripten::val RH3DM_DATETIME;
inline RH3DM_DATETIME RH3DM_DateTime(int y, int mo, int d, int h, int mi, int s) {
  return emscripten::val::global("Date").new_(y, mo - 1, d, h, mi, s); }
inline RH3DM_DATETIME RH3DM_DateTimeMin() { return RH3DM_DateTime(1, 1, 1, 0, 0, 0); }
#endif
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

// System.Drawing.Color across the C layer, exactly as the C# call sites do
// it. BND_Color's alpha is OPACITY (the old bindings' convention, = .NET's
// Color.A); ON_Color's is transparency.
inline BND_Color RH3DM_ColorFromArgb(int argb)       // Color.FromArgb(int)
{ return ON_Color_to_Binding(ON_Color((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF, 255 - ((argb >> 24) & 0xFF))); }
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
               for e in bare_enums] + gen_enum_defs
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
                       # objects placed in tuples (SetTuple needs the complete class): a tuple led
                       # by a wrapped result (G73), a wrap tuple's elements (G72)
                       | {m for p in plans if p['shape'][0] == 'tuple' and isinstance(p['shape'][1], tuple)
                          for m in re.findall(r'RH3DM_Wrap_(\w+)\(', p['shape'][1][2])}
                       | {m for p in plans if p['shape'][0] == 'wraptuple'
                          for pair in re.findall(r'RH3DM_Wrap_(\w+)\(|new (BND_\w+)\(', p['shape'][1]['item'])
                          for m in [pair[0] or next((c['reg'] for c in CLASSES if c['gen'] == pair[1]), None)] if m}
                       | ({cls['proxy']['Owner']} if (cls.get('proxy') or {}).get('Origin') == 'parent' else set())
                       # classes a translated body names (G74)
                       | {m for p in plans if p['shape'][0] == 'translated'
                          for m in re.findall(r'\bBND_(\w+X)\b', p['shape'][1] + ' ' + p['shape'][2] + ' ' + ' '.join(t for t, _ in p['bind_args']))
                          if m in BY_REG}
                       | arg_classes) - {cls['reg']})
        # data classes (G64): returned by a bridge, or taken as arguments (dereferenced here)
        data_deps = sorted({p['shape'][1] + 'X' for p in plans if p['shape'][0] in ('datafrom', 'datafill')}
                           | {cs + 'X' for p in plans for t, _ in p['bind_args'] for cs, cpp in DATA_CPP.items()
                              if t in ('const %s*' % cpp, 'const %s&' % cpp)}
                           | {cs + 'X' for p in plans for l in p.get('locals') or [] for cs, cpp in DATA_CPP.items()
                              if re.match(r'^%s\b' % re.escape(cpp), l)})
        dep_includes = ''.join('#include "gen_%s.h"\n' % d.lower() for d in deps + data_deps)
        defs = '\n\n'.join(emit_decl_def(p, cls)[1] for p in plans) + table_def
        pvp = cls.get('proxy') or {}
        if pvp.get('Origin') == 'parent':
            # the owner's const pointer, where the owner's class is complete (G57)
            own = BY_REG[pvp['Owner']]
            oarg = C_BY_NAME[pvp['Resolver']]['params'][0]['type']
            defs = ('%s %s::RH3DM_PvOwner(const %s* owner)\n{\n  return (%s)owner->%s;\n}\n\n'
                    % (oarg, cls['gen'], own['gen'], oarg,
                       'RH3DM_Const()' if own.get('proxy') else 'm_ptr')) + defs

        py_block, js_block = reg_blocks[cls['reg']]
        if cls['mode'] == 'abstract':
            hand_inc, ctor = '', ''
        else:
            bridged = cls.get('bridge', True)
            hand_inc = ('#include "%s"\n' % cls['hand_header']) if bridged else ''
            basecls = BY_REG.get(cls.get('base', ''))
            if basecls and basecls['mode'] != 'abstract':
                member = basecls['gen']       # forward to the owning base
            else:
                member = 'm_ptr' if cls['mode'] == 'pointer' else 'm_val'
            ctor = ('\n{gen}::{gen}(const {hand}& source)\n  : {member}({copy}) {{}}\n'.format(
                gen=cls['gen'], hand=cls['hand'], member=member,
                copy=cls['copy']) if bridged else '')
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
{dep_includes}{hand_inc}{ctor}
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
'''.format(hdr=fname(cls), dep_includes=dep_includes, hand_inc=hand_inc,
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
    files['gen_init.cpp'] = BANNER + '''#include "gen_common.h"

#if defined(ON_PYTHON_COMPILE)
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
{enums_py_nested}
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
{js_iter}}}
#endif
'''.format(
        py_decls='\n'.join('void initGen%s(rh3dmpymodule&);' % cls['reg']
                           for cls in CLASSES),
        js_decls='\n'.join('void initGen%s(void);' % cls['reg']
                           for cls in CLASSES),
        enums_py='\n'.join(gen_enum_py), enums_js='\n'.join(gen_enum_js),
        enums_py_nested='\n'.join(gen_enum_py_nested),
        inits_py=inits_py, inits_js=inits_js,
        vt_py_decls=''.join('void initGen%s(rh3dmpymodule&);\n' % r for r in vt_regs),
        vt_js_decls=''.join('void initGen%s(void);\n' % r for r in vt_regs),
        vt_py=''.join('  initGen%s(m);\n' % r for r in vt_regs),
        vt_js=''.join('  initGen%s();\n' % r for r in vt_regs),
        # `for (const x of table)`: embind cannot register a Symbol-keyed method,
        # so once every class exists, alias the registered `iterator()` under
        # Symbol.iterator on the table's prototype
        js_iter=''.join('  { emscripten::val proto = emscripten::val::module_property("%s")["prototype"];\n'
                        '    proto.set(emscripten::val::global("Symbol")["iterator"], proto["iterator"]); }\n'
                        % c['reg'] for c in CLASSES if c.get('table') or c.get('liter')))

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

    # RH3DM_GEN_OUT: dry-run elsewhere (e.g. while a build reads src/generated)
    dry_run = bool(os.environ.get('RH3DM_GEN_OUT'))
    out_dir = os.environ.get('RH3DM_GEN_OUT') or os.path.join(ROOT, 'src', 'generated')
    # the surface lock: a member that disappears or changes is a regression
    # unless accepted explicitly (RH3DM_ACCEPT=1); nothing is written then
    import surface_lock
    ok, lost, gained = surface_lock.check(files, os.path.join(ROOT, 'api', 'generated-surface.lock'), dry_run)
    if lost:
        print('surface lock: %d registration(s) %s:' % (len(lost), 'ACCEPTED as removed' if ok else 'REMOVED or changed'))
        for l in lost:
            print('  - ' + l)
    if not ok:
        print('refusing to write src/generated: rerun with RH3DM_ACCEPT=1 if intended')
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
    # would compile silently -- remove anything unexpected (old layouts too)
    removed = []
    for name in sorted(os.listdir(out_dir)):
        if name.endswith(('.cpp', '.h', '.cmake')) and name not in files:
            os.remove(os.path.join(out_dir, name))
            removed.append(name)

    for cls, plans in per_class:
        props = sum(1 for p in plans if p['m']['Kind'] == 'property')
        print('  %-10s %d members (%d properties) -> %s.{h,cpp}'
              % (cls['reg'], len(plans), props, fname(cls)))
    print('src/generated/: %d members, %d files (%d written, %d unchanged%s)'
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
