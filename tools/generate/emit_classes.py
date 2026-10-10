"""Class emission: plans -> C++ class declarations/definitions and the
pybind11 / embind registrations."""

from gen_registry import *


def emit(p):
    m = p['m']
    name = p['cpp']
    sig = ', '.join('%s %s' % (t, n) for t, n in p['bind_args'])
    call = '%s(%s)' % (p['fn']['name'], ', '.join(p['call_exprs'])) if p['fn'] else None
    shape = p['shape']
    cq = ' const' if not p['static'] and not p.get('setter_of') else ''
    st = 'static ' if p['static'] else ''
    L = []
    guard_lines = [
        '    if (%s) throw %s("%s");'
        % (g['cond_cpp'], EXMAP[g['Exception']],
           (g['Message'] or m['Name']).replace('"', "'"))
        for g in p['guards']]
    if shape[0] == 'direct':
        ret = shape[1]
        L.append('  %s%s %s(%s)%s {' % (st, ret, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s%s;' % ('' if ret == 'void' else 'return ', call))
    elif shape[0] == 'fromout':
        cs, out = shape[1], shape[2]
        L.append('  %s%s %s(%s)%s {' % (st, CS_RET_CPP[cs], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        if p['success']:
            L.append('    if (%s) return %s;' % (call, CS_RET_EXPR[cs].format(n=out)))
            fb = p['success']['Fallback']
            fb_cpp = (CS_RET_EXPR[cs].format(n=out) if fb == out
                      else local_value(fb)[0])
            L.append('    return %s;' % fb_cpp)
        else:
            L.append('    %s;' % call)
            L.append('    return %s;' % CS_RET_EXPR[cs].format(n=out))
    elif shape[0] == 'forward':
        # the target's own C++ return type (wrap shapes return BND_*X*),
        # resolved here: C++ names are assigned after planning
        target = shape[2]
        tdecl = emit_decl_def(target, p['cls'])[0]
        ret = tdecl[:tdecl.index(' %s(' % target['cpp'])].replace('static ', '').strip()
        L.append('  %s%s %s(%s)%s {' % (st, ret, name, sig, cq))
        args = [re.sub(r'@@(\d+)@@', lambda mm: p['sib_refs'][int(mm.group(1))]['cpp'] + '()', a)
                for a in p['forward_args']]
        L.append('    return %s(%s);' % (target['cpp'], ', '.join(args)))
    elif shape[0] == 'fill':
        target, new_expr, fail = shape[1], shape[2], shape[3]
        L.append('  %s%s* %s(%s)%s {' % (st, target['gen'], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s* fill_ptr = %s;' % (target['on'], new_expr))
        cond = ('!(%s)' % call) if fail == '!' else '(%s) %s' % (call, fail)
        L.append('    if (%s) { delete fill_ptr; return nullptr; }' % cond)
        L.append('    return RH3DM_Wrap_%s(fill_ptr);' % target['reg'])
    elif shape[0] == 'child':
        child = shape[1]
        L.append('  %s* %s()%s {' % (child['gen'], name, cq))
        L.append('    return new %s(%s);' % (child['gen'], p['self_ptr']))
    elif shape[0] == 'conv':
        L.append('  %s%s %s(%s)%s {' % (st, shape[1], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    return %s;' % shape[2].format(c=call))
    elif shape[0] == 'enumret':
        cpp = CS_ENUMS[shape[1]]['cpp']
        L.append('  %s%s %s(%s)%s {' % (st, cpp, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    return (%s)%s;' % (cpp, call))
    elif shape[0] == 'wrap':
        target = BY_CS[shape[1]]
        L.append('  %sclass %s* %s(%s)%s {' % (st, target['gen'], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s* rc = %s;' % (target['on'], call))
        # most-derived generated class (an ON_Curve* may be a NURBS curve);
        # pybind11/embind then downcast the polymorphic result automatically
        L.append('    return rc ? RH3DM_Wrap_%s(rc) : nullptr;' % target['reg'])
    elif shape[0] == 'holder':
        hp = HOLDERS[shape[1]]
        if 'conv' in hp:                      # string
            L.append('  %sstd::wstring %s(%s)%s {' % (st, name, sig, cq))
            L += guard_lines
            L += ['    %s' % x for x in p['locals']]
            L.append('    %s;' % call)
            L.append('    return %s;' % hp['conv'].format(n='holder_out'))
        else:                                 # array -> tuple of elements
            L.append('  %sBND_TUPLE %s(%s)%s {' % (st, name, sig, cq))
            L += guard_lines
            L += ['    %s' % x for x in p['locals']]
            L.append('    %s;' % call)
            L.append('    BND_TUPLE rc = CreateTuple(holder_out.Count());')
            L.append('    for (int i = 0; i < holder_out.Count(); i++)')
            L.append('      SetTuple(rc, i, %s);' % hp['elem'].format(n='holder_out'))
            L.append('    return rc;')
    elif shape[0] == 'holdertuple':           # bool X(out string log)
        hp = HOLDERS[shape[1]]
        L.append('  %sBND_TUPLE %s(%s)%s {' % (st, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    bool success = %s;' % call)
        L.append('    BND_TUPLE rc = CreateTuple(2);')
        L.append('    SetTuple(rc, 0, success);')
        L.append('    SetTuple(rc, 1, %s);' % hp['conv'].format(n='holder_out'))
        L.append('    return rc;')
    else:  # tuple
        _, has_success, outs = shape
        L.append('  %sBND_TUPLE %s(%s)%s {' % (st, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s%s;' % ('bool success = ' if has_success else '', call))
        L.append('    BND_TUPLE rc = CreateTuple(%d);' % (len(outs) + (1 if has_success else 0)))
        i = 0
        if has_success:
            L.append('    SetTuple(rc, 0, success);')
            i = 1
        for j, (n, t) in enumerate(outs):
            L.append('    SetTuple(rc, %d, %s);' % (i + j, OUTLOCAL[t][1].format(n=n)))
        L.append('    return rc;')
    L.append('  }')
    return '\n'.join(L)


def emit_decl_def(p, cls):
    """One member as (header declaration, cpp definition). emit() produces
    the inline form; the first line carries the whole signature, so the
    declaration is that line with `;` and the definition is the same text
    with the name qualified, `static` dropped, and the indent removed."""
    lines = emit(p).split('\n')
    first = lines[0]
    decl = first[:first.rfind('{')].rstrip() + ';'
    qualified = first.replace(' %s(' % p['cpp'],
                              ' %s::%s(' % (cls['gen'], p['cpp']), 1)
    body = [qualified] + lines[1:]
    body = [l[2:] if l.startswith('  ') else l for l in body]
    if body[0].startswith('static '):
        body[0] = body[0][len('static '):]
    return decl, '\n'.join(body)


def emit_class(cls, plans, has_default_ctor=False, arg_ctors=()):
    methods = '\n'.join(emit_decl_def(p, cls)[0] for p in plans)
    dctor = '\n  %s();' % cls['gen'] if has_default_ctor else ''
    for p in arg_ctors:
        dctor += '\n  %s(%s);' % (cls['gen'], ', '.join('%s %s' % a for a in p['bind_args']))
    # RhinoCommon's own copy ctor (`X(X other)` -> ON_X_New(other)) replaces
    # the deleted C++ copy: deep copy through the C layer, as in .NET
    self_copy = any(len(p['bind_args']) == 1 and p['bind_args'][0][0] == 'const %s&' % cls['gen']
                    for p in arg_ctors)
    # the bridge ctor copies from the hand BND class; 'bridge': False drops
    # it (and with it every dependency on the hand header) for classes the
    # generated surface can construct and populate on its own
    bridge = ('\n  %s(const class %s& source);' % (cls['gen'], cls['hand'])
              if cls.get('bridge', True) and cls['mode'] != 'abstract' else '')
    if cls['mode'] == 'static':
        # a C# static class: a namespace of static members
        return ('class {gen}\n{{\npublic:\n  {gen}() = delete;\n\n{methods}\n}};\n').format(
            gen=cls['gen'], methods=methods)
    if cls['mode'] == 'abstract':
        storage = '''  virtual ~{gen}() = default;
  virtual {on}* {getter}() const = 0;'''
        return ('class {gen}\n{{\npublic:\n' + storage
                + '\n\n{methods}\n}};\n').format(
            gen=cls['gen'], on=cls['on'], getter=cls['getter'],
            methods=methods)
    base = BY_REG.get(cls.get('base', ''))
    inherit = ' : public %s' % base['gen'] if base else ''
    if base and base['mode'] != 'abstract':
        # concrete base owns the pointer (and the abstract getter override);
        # this class only adds members and a forwarding constructor
        storage = '''  explicit {gen}({on}* owned) : {basegen}(owned) {{}}{bridge}{dctor}'''
        return ('class {gen}{inherit}\n{{\npublic:\n' + storage
                + '\n\n{methods}\n}};\n').format(
            gen=cls['gen'], on=cls['on'], hand=cls['hand'],
            basegen=base['gen'], methods=methods, inherit=inherit,
            dctor=dctor, bridge=bridge)
    override = ('\n  %s* %s() const override { return m_ptr; }'
                % (base['on'], base['getter'])) if base else ''
    if cls['mode'] == 'child':
        # a VIEW onto the parent's object (Mesh.Vertices): borrows the
        # parent's pointer, never deletes it; the binding keeps the parent alive
        storage = '''  {on}* m_ptr = nullptr;
  explicit {gen}({on}* parent) : m_ptr(parent) {{}}'''
    elif cls['mode'] == 'pointer':
        storage = '''  {on}* m_ptr = nullptr;
  explicit {gen}({on}* owned) : m_ptr(owned) {{}}{bridge}{dctor}
  ~{gen}() {{ delete {delete_expr}; }}{nocopy}{override}'''
    else:
        storage = '''  {on} m_val;{bridge}{override}'''
    return ('class {gen}{inherit}\n{{\npublic:\n' + storage
            + '\n\n{methods}\n}};\n').format(
        gen=cls['gen'], on=cls['on'], hand=cls['hand'], methods=methods,
        inherit=inherit, override=override, dctor=dctor, bridge=bridge,
        # ON_Annotation's destructor is protected: delete through ON_Object (public, virtual)
        delete_expr='static_cast<ON_Object*>(m_ptr)' if cls.get('delete_as_object') else 'm_ptr',
        nocopy='' if self_copy else '\n  %s(const %s&) = delete;' % (cls['gen'], cls['gen']))


def js_class_shape(t):
    """how a JS argument for binding type `t` can be recognized at run time:
    (shape key for collapsing indistinguishable overloads, C++ check on
    emscripten::val `{v}`, C++ conversion of `{v}` to `t`)"""
    if t in ('double', 'float', 'int', 'unsigned int', 'long long'):
        return 'number', '{v}.isNumber()', '{v}.as<%s>()' % t
    if t == 'bool':
        return 'boolean', '({v}.typeOf().as<std::string>() == "boolean")', '{v}.as<bool>()'
    enum_js = {v['cpp']: k for k, v in CS_ENUMS.items()}
    if t in enum_js:
        return ('enum:' + enum_js[t],
                '{v}.instanceof(emscripten::val::module_property("%s"))' % enum_js[t],
                '{v}.as<%s>()' % t)
    # {X,Y,Z} is keyed on Z so a 3D object never passes the {X,Y} test
    # JS shape policy: {X,Y,Z} objects key on Z (so a 3D value never passes
    # the {X,Y} test below); everything else keys on its first field
    vt = dict(VT_JS_KEY, **{'BND_Point3dX': 'Z', 'BND_Vector3dX': 'Z', 'BND_IntervalX': 'T0'})
    if t in vt:
        lacks = ' && !{v}.hasOwnProperty("Z")' if t == 'BND_Point2dX' else ''
        return ('object:' + vt[t],
                '({v}.typeOf().as<std::string>() == "object" && {v}.hasOwnProperty("%s")%s)' % (vt[t], lacks),
                '{v}.as<%s>()' % t)
    m = re.match(r'const (BND_(\w+))\*$', t)
    if m:                                  # an optional class argument: null or an instance
        reg = m.group(2)
        return ('class:' + reg,
                '({v}.isNull() || {v}.instanceof(emscripten::val::module_property("%s")))' % reg,
                '({v}.isNull() ? nullptr : {v}.as<%s*>(emscripten::allow_raw_pointers()))' % m.group(1))
    m = re.match(r'const (BND_(\w+))&$', t)
    if m:
        reg = m.group(2)
        return ('class:' + reg,
                '{v}.instanceof(emscripten::val::module_property("%s"))' % reg,
                '*{v}.as<%s*>(emscripten::allow_raw_pointers())' % m.group(1))
    return 'any:' + t, 'true', '{v}.as<%s>()' % t


def emit_registrations(cls, plans, js_pruned, has_default_ctor=False, arg_ctors=()):
    py, js = [], []
    js_methods = {}            # (camel name, arity, static) -> plans, in order
    instance_names = {p['m']['Name'] for p in plans if not p['static'] and p['m']['Kind'] != 'property'
                      and not p.get('setter_of')}
    live = {id(p) for p in plans}
    for p in plans:
        if p.get('setter_of'):
            continue           # registered with its getter below
        n, g = p['m']['Name'], p['cpp']
        pyargs = ''.join(', py::arg("%s")' % a for _, a in p['bind_args'])
        if p['static'] and n in instance_names:
            pass               # Python: one name cannot be static AND instance; JS keeps it
        elif p['static']:
            py.append('      .def_static("%s", &%s::%s%s)' % (n, cls['gen'], g, pyargs))
        elif p['shape'][0] == 'child':
            # a view onto this object: Python owns the view and keeps the parent alive
            # def_property* cannot take keep_alive itself: wrap the getter
            py.append('      .def_property_readonly("%s", py::cpp_function(&%s::%s, py::return_value_policy::take_ownership, py::keep_alive<0, 1>()))'
                      % (n, cls['gen'], g))
            js.append('      .property("%s", &%s::%s, allow_raw_pointers())' % (camel(n), cls['gen'], g))
            continue
        elif p['m']['Kind'] == 'indexer':
            sp = p.get('setter')
            py.append('      .def("__getitem__", &%s::%s)' % (cls['gen'], g))
            js.append('      .function("get", &%s::%s)' % (cls['gen'], g))
            if sp is not None and id(sp) in live:
                py.append('      .def("__setitem__", &%s::%s)' % (cls['gen'], sp['cpp']))
                js.append('      .function("set", &%s::%s)' % (cls['gen'], sp['cpp']))
            cnt = [q for q in plans if q['m']['Name'] == 'Count' and q['m']['Kind'] == 'property']
            if cnt:
                py.append('      .def("__len__", &%s::%s)' % (cls['gen'], cnt[0]['cpp']))
            continue
        elif p['m']['Kind'] == 'property':
            sp = p.get('setter')
            if sp is not None and id(sp) in live:
                py.append('      .def_property("%s", &%s::%s, &%s::%s)'
                          % (n, cls['gen'], g, cls['gen'], sp['cpp']))
                js.append('      .property("%s", &%s::%s, &%s::%s)'
                          % (camel(n), cls['gen'], g, cls['gen'], sp['cpp']))
            else:
                py.append('      .def_property_readonly("%s", &%s::%s)' % (n, cls['gen'], g))
                js.append('      .property("%s", &%s::%s)' % (camel(n), cls['gen'], g))
            continue
        else:
            py.append('      .def("%s", &%s::%s%s)' % (n, cls['gen'], g, pyargs))
        js_methods.setdefault((camel(n), len(p['bind_args']), p['static']), []).append(p)

    # embind keeps ONE function per name+arity. Same-arity overloads get a
    # generated C++ dispatcher on argument shape (isoCurve(int, t) vs
    # isoCurve(IsoStatus, t)); overloads JS cannot tell apart collapse to the
    # first. (Before: all but the first were pruned -- unreachable in JS.)
    for (jn, arity, static), ps in js_methods.items():
        by_shape = {}
        for p in ps:
            by_shape.setdefault(tuple(js_class_shape(t)[0] for t, _ in p['bind_args']), p)
        ps = list(by_shape.values())
        if len(ps) == 1:
            p = ps[0]
            rawp = (', allow_raw_pointers()' if p['shape'][0] in ('wrap', 'fill')
                    or any(t.endswith('*') for t, _ in p['bind_args']) else '')
            js.append('      .%s("%s", &%s::%s%s)'
                      % ('class_function' if static else 'function', jn, cls['gen'], p['cpp'], rawp))
            continue
        js_pruned.append('%s.%s/%d (dispatched)' % (cls['reg'], jn, arity))
        vparams = ', '.join('emscripten::val v%d' % i for i in range(arity))
        selfp = '' if static else '%s& self%s' % (cls['gen'], ', ' if arity else '')
        # one C++ return type across the overloads (isoCurve: CurveX* both):
        # return it directly -- embind cannot wrap a raw pointer in a val
        rets = set()
        for p in ps:
            d = emit_decl_def(p, cls)[0]
            rets.add(d[:d.index(' %s(' % p['cpp'])].replace('static ', '').strip())
        common = rets.pop() if len(rets) == 1 else None
        branches = []
        for p in ps:
            shapes = [js_class_shape(t) for t, _ in p['bind_args']]
            cond = ' && '.join(s[1].format(v='v%d' % i) for i, s in enumerate(shapes)) or 'true'
            args = ', '.join(s[2].format(v='v%d' % i) for i, s in enumerate(shapes))
            call = ('%s::%s(%s)' if static else 'self.%s(%s)') % (
                *(([cls['gen']] if static else [])), p['cpp'], args)
            if common is not None:
                branches.append('if (%s) %s%s;' % (cond, '' if common == 'void' else 'return ', call)
                                + (' return;' if common == 'void' else ''))
            elif p['shape'][0] == 'direct' and p['shape'][1] == 'void':
                branches.append('if (%s) { %s; return emscripten::val::undefined(); }' % (cond, call))
            else:
                branches.append('if (%s) return emscripten::val(%s);' % (cond, call))
        rtype = common if common is not None else 'emscripten::val'
        fallback = ('return emscripten::val::undefined();' if common is None else
                    'return;' if common == 'void' else
                    'return nullptr;' if common.endswith('*') else 'return %s{};' % common)
        js.append('      .%s("%s", optional_override([](%s%s) -> %s {\n'
                  '        %s\n        %s }), allow_raw_pointers())'
                  % ('class_function' if static else 'function', jn, selfp, vparams, rtype,
                     '\n        '.join(branches), fallback))
    base = BY_REG.get(cls.get('base', ''))
    if cls['mode'] == 'abstract':
        # no constructor: instances only exist as derived objects
        py_block = ('  py::class_<{gen}>(m, "{reg}")\n{defs};').format(
            gen=cls['gen'], reg=cls['reg'], defs='\n'.join(py))
        js_block = ('  class_<{gen}>("{reg}")\n{defs};').format(
            gen=cls['gen'], reg=cls['reg'], defs='\n'.join(js))
        return py_block, js_block
    py_base = ', %s' % base['gen'] if base else ''
    js_base = '<%s, base<%s>>' % (cls['gen'], base['gen']) if base \
        else '<%s>' % cls['gen']
    py_dctor = '      .def(py::init<>())\n' if has_default_ctor else ''
    js_dctor = '      .constructor<>()\n' if has_default_ctor else ''
    # embind constructors dispatch on arity only: default (0) and bridge (1)
    # keep their slots; a same-arity argument ctor is pruned in JS, counted
    js_arity = ({0} if has_default_ctor else set()) | ({1} if cls.get('bridge', True) else set())
    js_ctors = {}                         # arity -> plans, in order
    for p in arg_ctors:
        types = ', '.join(t for t, _ in p['bind_args'])
        py_dctor += '      .def(py::init<%s>()%s)\n' % (
            types, ''.join(', py::arg("%s")' % n for _, n in p['bind_args']))
        if len(p['bind_args']) in js_arity:
            js_pruned.append('%s.constructor/%d' % (cls['reg'], len(p['bind_args'])))
        else:
            js_ctors.setdefault(len(p['bind_args']), []).append(p)
    # same-arity constructors (LineCurve(Point2d, Point2d) / (Point3d,
    # Point3d)) get a factory dispatching on argument shape, as methods do
    for arity, ps in js_ctors.items():
        by_shape = {}
        for p in ps:
            by_shape.setdefault(tuple(js_class_shape(t)[0] for t, _ in p['bind_args']), p)
        ps = list(by_shape.values())
        if len(ps) == 1:
            js_dctor += '      .constructor<%s>()\n' % ', '.join(t for t, _ in ps[0]['bind_args'])
            continue
        js_pruned.append('%s.constructor/%d (dispatched)' % (cls['reg'], arity))
        branches = []
        for p in ps:
            shapes = [js_class_shape(t) for t, _ in p['bind_args']]
            cond = ' && '.join(sh[1].format(v='v%d' % i) for i, sh in enumerate(shapes))
            args = ', '.join(sh[2].format(v='v%d' % i) for i, sh in enumerate(shapes))
            branches.append('if (%s) return new %s(%s);' % (cond, cls['gen'], args))
        js_dctor += ('      .constructor(optional_override([](%s) -> %s* {\n'
                     '        %s\n'
                     '        throw std::invalid_argument("%s: no constructor matches the arguments"); }),'
                     ' allow_raw_pointers())\n'
                     % (', '.join('emscripten::val v%d' % i for i in range(arity)), cls['gen'],
                        '\n        '.join(branches), cls['reg']))
    has_bridge = cls.get('bridge', True)
    if not has_bridge and not has_default_ctor and cls['mode'] not in ('child', 'static'):
        raise SystemExit('%s: bridge-free class needs a generated constructor'
                         % cls['reg'])
    py_bridge = ('      .def(py::init<const %s&>(), py::arg("source"))\n'
                 % cls['hand']) if has_bridge else ''
    js_bridge = ('      .constructor<const %s&>()\n' % cls['hand']
                 ) if has_bridge else ''
    py_buf, py_bufdef = '', ''
    if cls.get('buffer'):
        py_buf = ', py::buffer_protocol()'
        py_bufdef = ('      .def_buffer([]({gen}& c) -> py::buffer_info {{\n'
                     '        ON_3dPointArray& a = c.{expr};\n'
                     '        return py::buffer_info(a.Array(), sizeof(double),\n'
                     '          py::format_descriptor<double>::format(), 2,\n'
                     '          {{(py::ssize_t)a.Count(), (py::ssize_t)3}},\n'
                     '          {{(py::ssize_t)sizeof(ON_3dPoint), (py::ssize_t)sizeof(double)}}); }})\n'
                     ).format(gen=cls['gen'], expr=cls['buffer'])
    py_block = ('  py::class_<{gen}{py_base}>(m, "{reg}"{py_buf})\n{py_dctor}{py_bridge}{py_bufdef}'
                '{defs};').format(gen=cls['gen'], py_base=py_base,
                                  reg=cls['reg'], hand=cls['hand'],
                                  py_dctor=py_dctor, py_bridge=py_bridge, py_buf=py_buf, py_bufdef=py_bufdef,
                                  defs='\n'.join(py))
    js_block = ('  class_{js_base}("{reg}")\n{js_dctor}{js_bridge}'
                '{defs};').format(js_base=js_base, reg=cls['reg'],
                                  hand=cls['hand'], js_dctor=js_dctor, js_bridge=js_bridge,
                                  defs='\n'.join(js))
    return py_block, js_block
