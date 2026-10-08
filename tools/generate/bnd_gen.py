"""Pilot binding generator: emit BND_CurveX from api/manifest.json.

Generates src/generated/bnd_curvex_gen.cpp: a parallel class ("CurveX")
registered alongside the hand-written Curve so nothing existing is disturbed,
whose methods call the FLAT C FUNCTIONS (the locked vNext decision) rather
than opennurbs C++ directly. The C functions are declared extern "C" FROM THE
MANIFEST'S SIGNATURES, so the manifest is the declaration source: if it is
wrong, the build breaks -- that is the point of the pilot.

Marshalling rules (tier-1 pilot subset):
  first param `const ON_Curve*`/`ON_Curve*`   -> self (m_curve)
  double/int/bool by value                    -> binding argument
  trailing non-const scalar/point/vector/
    interval pointer                          -> out-param: local, returned
  C returns bool + out-params                 -> BND_TUPLE (success, outs...)
  C returns ON_Curve*                         -> new BND_CurveX (owned)

Members whose C function needs anything else (arrays, structs, strings) are
skipped and reported -- they are later tiers, not failures.
"""

import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

SELF = {'const ON_Curve*', 'ON_Curve*'}
VALUE = {'double': 'double', 'int': 'int', 'bool': 'bool', 'unsigned int': 'unsigned int'}
OUT = {  # C param type -> (local decl, returned expression, tuple set type)
    'double*':      ('double {n} = 0;',        '{n}'),
    'int*':         ('int {n} = 0;',           '{n}'),
    'bool*':        ('bool {n} = false;',      '{n}'),
    'ON_3dPoint*':  ('ON_3dPoint {n};',        '{n}'),
    'ON_3dVector*': ('ON_3dVector {n};',       '{n}'),
    'ON_Interval*': ('ON_Interval {n};',       'BND_Interval({n})'),
}
RET = {'void', 'bool', 'int', 'double', 'ON_Curve*'}


def camel(name):
    return name[0].lower() + name[1:]


def plan_member(member, fn):
    """Decide the marshalling for one member; None if outside the pilot tier."""
    if fn['returns'] not in RET:
        return None
    params = fn['params']
    if not params or params[0]['type'] not in SELF:
        return None
    # ordered plan: the CALL must follow C parameter order exactly (out-params
    # are not always trailing: ON_Curve_PointAt(pCurve, t, pt, which)); only
    # the BINDING signature is values-in-order
    ordered, args, outs = [], [], []
    for i, p in enumerate(params[1:]):
        t, n = p['type'], p['name'] or ('a%d' % i)
        if p.get('array'):
            return None
        if t in VALUE:
            args.append((VALUE[t], n))
            ordered.append(n)
        elif t in OUT:
            outs.append((t, n))
            ordered.append('&' + n)
        else:
            return None
    if fn['returns'] == 'ON_Curve*' and outs:
        return None
    return {'member': member, 'fn': fn, 'args': args, 'outs': outs,
            'ordered': ordered}


def emit_decl(fn):
    ps = ', '.join('%s %s' % (p['type'], p['name'] or 'a') for p in fn['params'])
    return 'extern "C" %s %s(%s);' % (fn['returns'], fn['name'], ps)


def emit_method(plan):
    m, fn = plan['member'], plan['fn']
    args, outs = plan['args'], plan['outs']
    sig_args = ', '.join('%s %s' % (t, n) for t, n in args)
    call_args = ['m_curve'] + plan['ordered']
    call = '%s(%s)' % (fn['name'], ', '.join(call_args))
    name = 'Gen_' + m['Name']
    lines = []
    if fn['returns'] == 'ON_Curve*':
        lines.append('  class BND_CurveX* %s(%s) const {' % (name, sig_args))
        lines.append('    ON_Curve* rc = %s;' % call)
        lines.append('    return rc ? new BND_CurveX(rc) : nullptr;')
        lines.append('  }')
    elif not outs:
        ret = fn['returns']
        lines.append('  %s %s(%s) const {' % (ret, name, sig_args))
        lines.append('    %s%s;' % ('' if ret == 'void' else 'return ', call))
        lines.append('  }')
    else:
        # success + out-params -> tuple, matching the hand-written convention
        lines.append('  BND_TUPLE %s(%s) const {' % (name, sig_args))
        for t, n in outs:
            lines.append('    ' + OUT[t][0].format(n=n))
        n_items = 1 + len(outs) if fn['returns'] == 'bool' else len(outs)
        if fn['returns'] == 'bool':
            lines.append('    bool success = %s;' % call)
        else:
            lines.append('    %s;' % call)
        lines.append('    BND_TUPLE rc = CreateTuple(%d);' % n_items)
        idx = 0
        if fn['returns'] == 'bool':
            lines.append('    SetTuple(rc, 0, success);')
            idx = 1
        for i, (t, n) in enumerate(outs):
            lines.append('    SetTuple(rc, %d, %s);' % (idx + i, OUT[t][1].format(n=n)))
        lines.append('    return rc;')
        lines.append('  }')
    return '\n'.join(lines)


def main():
    manifest = json.load(open(os.path.join(ROOT, 'api', 'manifest.json')))
    c = {f['name']: f for f in manifest['c_surface']}
    curve = [x for x in manifest['members']
             if x['Type'] == 'Rhino.Geometry.Curve' and x['Variant'] == 'portable'
             and x.get('Native') and len(x['Native']) == 1 and x['Kind'] == 'method']

    plans, skipped, seen = [], [], set()
    for x in sorted(curve, key=lambda x: (x['Name'], x['Signature'])):
        fn = c.get(x['Native'][0])
        plan = plan_member(x, fn) if fn else None
        key = x['Name']
        if plan and key not in seen:        # one overload per name for the pilot
            seen.add(key)
            plans.append(plan)
        elif not plan:
            skipped.append((x['Name'], fn['name'] if fn else '?'))

    decls = sorted({emit_decl(p['fn']) for p in plans})
    methods = [emit_method(p) for p in plans]
    py_regs = ['      .def("%s", &BND_CurveX::Gen_%s)'
               % (p['member']['Name'], p['member']['Name']) for p in plans]
    js_regs = ['      .function("%s", &BND_CurveX::Gen_%s)'
               % (camel(p['member']['Name']), p['member']['Name']) for p in plans]

    body = '''// GENERATED by tools/generate/bnd_gen.py -- DO NOT EDIT.
// Pilot: {n} Curve methods calling the flat C layer (on_curve.cpp), which is
// compiled into this module. The extern "C" declarations below come from
// api/manifest.json: the manifest is the declaration source, so a wrong
// manifest breaks this build. Registered as "CurveX" beside the hand-written
// "Curve"; construction copies, so lifetimes are independent.

#include "bindings.h"

{decls}

class BND_CurveX
{{
public:
  ON_Curve* m_curve = nullptr;
  explicit BND_CurveX(ON_Curve* owned) : m_curve(owned) {{}}
  BND_CurveX(const class BND_Curve& source);
  ~BND_CurveX() {{ delete m_curve; }}
  BND_CurveX(const BND_CurveX&) = delete;

{methods}
}};

#include "bnd_curve.h"
BND_CurveX::BND_CurveX(const BND_Curve& source)
  : m_curve(source.m_curve ? source.m_curve->DuplicateCurve() : nullptr) {{}}

#if defined(ON_PYTHON_COMPILE)
void initCurveXBindings(rh3dmpymodule& m)
{{
  py::class_<BND_CurveX>(m, "CurveX")
      .def(py::init<const BND_Curve&>(), py::arg("curve"))
{py_regs};
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
void initCurveXBindings(void)
{{
  class_<BND_CurveX>("CurveX")
      .constructor<const BND_Curve&>()
{js_regs};
}}
#endif
'''.format(n=len(plans),
           decls='\n'.join(decls),
           methods='\n\n'.join(methods),
           py_regs='\n'.join(py_regs),
           js_regs='\n'.join(js_regs))

    out_dir = os.path.join(ROOT, 'src', 'generated')
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, 'bnd_curvex_gen.cpp')
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        f.write(body)

    print('%s: %d methods generated, %d skipped (later tiers)'
          % (os.path.relpath(out, ROOT), len(plans), len(skipped)))
    for name, fn in skipped[:8]:
        print('  skipped %-28s (%s)' % (name, fn))
    return 0


if __name__ == '__main__':
    sys.exit(main())
