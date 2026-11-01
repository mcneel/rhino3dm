"""Registrations of hand members (G123: gen_registry.HAND_MEMBERS).

A hand member is a plain C++ free function over a generated class or value type;
this module is the ONLY place that knows how each binding registers one, so the
nanobind port (M2) of every hand member is a change here."""
from gen_registry import HAND_CLASSES, HAND_MEMBERS, camel, hand_class_names


def hand_names(reg, strict=False):
    """the Python and JS names reg's hand members take (a generated member must not reuse one).
    `strict`: leaving out Python overloads of a generated member (`overload`: True, G126)"""
    es = HAND_MEMBERS.get(reg, [])
    return ({e['name'] for e in es if e.get('py', True) and not (strict and e.get('overload'))},
            {e.get('js', camel(e['name'])) for e in es if e.get('js', camel(e['name'])) is not None})


def hand_headers(reg):
    """the headers reg's hand members are declared in, as includes from a generated file"""
    return sorted({'../' + e['header'] for e in HAND_MEMBERS.get(reg, []) if 'header' in e})


def hand_registrations(reg, value=False, overloads=False):
    """(Python lines, JS lines) registering reg's hand members, to append to its class_
    chains. `value`: reg is a value type -- JS registers members on its function holder
    (values are plain objects), taking the value first. Python overloads of a generated member
    (`overload`, G126) are left out unless `overloads` -- then they are ALL that is returned (Python
    only): the class appends them after its generated overloads, which pybind11 tries first"""
    py, js = [], []
    for e in HAND_MEMBERS.get(reg, []):
        if bool(e.get('overload')) != overloads:
            if e.get('overload') and not overloads:
                e = dict(e, py=False)          # its JS name is registered here, its Python one later
            else:
                continue
        if e['kind'] == 'alias':
            continue                      # no C++: installed by the JS post-script (hand_js_aliases)
        kind, fn = e['kind'], e['fn']
        ka = ', py::keep_alive<0, 1>()' if e.get('keep_alive') else ''
        args = ''.join(', py::arg("%s")' % a for a in e.get('args') or [])
        if e.get('py', True):
            if kind == 'method':
                py.append('      .def("%s", &%s%s%s)' % (e['name'], fn, ka, args))
            elif kind == 'static':
                py.append('      .def_static("%s", &%s%s)' % (e['name'], fn, args))
            elif e.get('list'):
                # new objects as a list (G129): RH3DM_ListOf (bnd_common.h), as generated sequences
                py.append('      .def_property_readonly("%s", &RH3DM_ListOf<&%s, BND_%s>)' % (e['name'], fn, reg))
            elif e.get('set'):
                py.append('      .def_property("%s", py::cpp_function(&%s%s), &%s)' % (e['name'], fn, ka, e['set']))
            else:
                py.append('      .def_property_readonly("%s", py::cpp_function(&%s%s))' % (e['name'], fn, ka))
        jn = e.get('js', camel(e['name']))
        if jn is None or overloads:
            continue
        if value:
            if kind == 'property':
                raise SystemExit('HAND_MEMBERS %s.%s: a value type has no JS properties (values are plain '
                                 'objects); make it a method, or js: None' % (reg, e['name']))
            js.append('      .class_function("%s", &%s, allow_raw_pointers())' % (jn, fn))
        elif kind == 'method':
            js.append('      .function("%s", &%s, allow_raw_pointers())' % (jn, fn))
        elif kind == 'static':
            js.append('      .class_function("%s", &%s, allow_raw_pointers())' % (jn, fn))
        elif e.get('list'):
            js.append('      .property("%s", &RH3DM_ListOf<&%s, BND_%s>)' % (jn, fn, reg))
        elif e.get('set'):
            js.append('      .property("%s", &%s, &%s)' % (jn, fn, e['set']))
        else:
            # a pointer result (a new object, G129) needs the policy; a value ignores it
            js.append('      .property("%s", &%s, allow_raw_pointers())' % (jn, fn))
    return py, js


def hand_js_aliases():
    """[(class, JS name, method it calls)] for the JS post-script (JS_POST 'aliases')"""
    return [(reg, e.get('js', camel(e['name'])), e['of'])
            for reg, es in sorted(HAND_MEMBERS.items()) for e in es if e['kind'] == 'alias']


def hand_class_registrations():
    """(includes + holder structs, Python statements, JS statements) registering the hand classes
    (gen_registry.HAND_CLASSES): a holder (a name) carries statics only; a hand type (G129) its own
    instance members, on the C++ class its header declares"""
    inc, py, js = [], [], []
    for hc in HAND_CLASSES:
        reg = hc if isinstance(hc, str) else hc['name']
        if isinstance(hc, str):
            bad = [e['name'] for e in HAND_MEMBERS.get(reg, []) if e['kind'] != 'static']
            if bad:
                raise SystemExit('HAND_CLASSES %s: a holder class takes statics only, not %s' % (reg, bad))
            tag = 'BND_%s' % reg          # a holder for its statics (M3: no generated class takes the name)
            inc += ['#include "%s"' % h for h in hand_headers(reg)] + ['struct %s {};' % tag]
        else:
            tag = hc['cpp']
            inc += ['#include "%s"' % h for h in sorted(set(hand_headers(reg)) | {'../' + hc['header']})]
        hpy, hjs = hand_registrations(reg)
        py.append('  py::class_<%s>(m, "%s")\n%s;\n' % (tag, reg, '\n'.join(hpy)))
        js.append('  class_<%s>("%s")\n%s;\n' % (tag, reg, '\n'.join(hjs)))
    return '\n'.join(dict.fromkeys(inc)) + ('\n' if inc else ''), ''.join(py), ''.join(js)


def hand_js_post():
    """the JS post-script facts hand members need (G129): `keep` -- a member marked keep_alive (its
    result holds this object, as Python's keep_alive<0, 1>); `enumerable` -- hand types marked so"""
    keep = [(reg, e.get('js', camel(e['name']))) for reg, es in sorted(HAND_MEMBERS.items()) for e in es
            if e.get('keep_alive') and e.get('js', camel(e['name'])) is not None and e['kind'] != 'alias']
    enum = sorted(hc['name'] for hc in HAND_CLASSES if not isinstance(hc, str) and hc.get('enumerable'))
    return keep, enum
