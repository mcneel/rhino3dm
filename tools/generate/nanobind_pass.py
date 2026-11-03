"""The structural part of the nanobind spelling of the generated Python registrations (M2,
docs/vnext/nanobind-port.md), run over every generated file before it is written.

The emitters write nanobind's names (def_prop_ro, rv_policy, ...) themselves. This stage does the
rewrites that need a whole registration, or the generated headers, to see: a property's getter and
setter policies (for_getter / for_setter), factory constructors as `__init__`, IntEnum enums, and the
None / no-conversion flags each argument needs from its C++ parameter type. The surface lock parses
the result, so a rewrite that loses or changes a registration fails the lock.

`py` is the namespace prefix: src/bindings/bindings.h aliases it to `nanobind`.
"""

import re


def _close(text, i):
    """index just past the parenthesis matching text[i] == '(' (strings and chars skipped)"""
    depth, q = 0, None
    while i < len(text):
        ch = text[i]
        if q:
            if ch == '\\':
                i += 1
            elif ch == q:
                q = None
        elif ch in '"\'':
            q = ch
        elif ch in '([{':
            depth += 1
        elif ch in ')]}':
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    raise ValueError('unbalanced: ' + text[:80])


def _split(args):
    """top-level comma split"""
    out, depth, q, cur = [], 0, None, ''
    for ch in args:
        if q:
            cur += ch
            if ch == q and not cur.endswith('\\' + q):
                q = None
            continue
        if ch in '"\'':
            q = ch
        elif ch in '([{<':
            depth += 1
        elif ch in ')]}>':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(cur.strip())
            cur = ''
        else:
            cur += ch
    if cur.strip():
        out.append(cur.strip())
    return out


def _unwrap(arg):
    """`py::cpp_function(f, p...)` -> (f, [p...]); anything else -> (arg, [])"""
    if arg.startswith('py::cpp_function(') and _close(arg, arg.index('(')) == len(arg):
        parts = _split(arg[arg.index('(') + 1:-1])
        return parts[0], parts[1:]
    return arg, []


_PROP = re.compile(r'\.(def_prop_ro|def_prop_rw|def_prop_ro_static|def_prop_rw_static)\(')


def _props(text):
    """nanobind's properties take the getter (and setter) themselves: the getter's policies follow
    it (def_prop_ro) or go in for_getter / for_setter (def_prop_rw)"""
    out, i = [], 0
    while True:
        m = _PROP.search(text, i)
        if not m:
            return ''.join(out) + text[i:]
        start = m.end() - 1
        end = _close(text, start)
        parts = _split(text[start + 1:end - 1])
        name, rest = parts[0], parts[1:]
        if m.group(1).endswith('ro') or m.group(1).endswith('ro_static'):
            g, gp = _unwrap(rest[0])
            new = [name, g] + gp + rest[1:]
        else:
            g, gp = _unwrap(rest[0])
            st, sp = _unwrap(rest[1])
            # for_getter / for_setter each wrap ONE annotation
            new = [name, g, st] + ['py::for_getter(%s)' % x for x in gp] + \
                ['py::for_setter(%s)' % x for x in sp] + rest[2:]
        out.append(text[i:start] + '(' + ', '.join(new) + ')')
        i = end


_CLASS = re.compile(r'py::class_<([\w:]+)')
_FACTORY = re.compile(r'\.def\(py::init\(\[')


def _factories(text):
    """`.def(py::init([](A a) { ...; return T(..); }), extra)` -> nanobind's
    `.def("__init__", [](T* self, A a) { new (self) T(<the lambda>(a)); }, extra)`"""
    out, i = [], 0
    while True:
        m = _FACTORY.search(text, i)
        if not m:
            return ''.join(out) + text[i:]
        cls = None
        for c in _CLASS.finditer(text, 0, m.start()):
            cls = c.group(1)
        init_open = m.start() + len('.def(py::init')
        init_end = _close(text, init_open)
        lam = text[init_open + 1:init_end - 1]
        params = lam[lam.index('(') + 1:_close(lam, lam.index('(')) - 1]
        ps = _split(params)
        names = [re.findall(r'(\w+)\s*$', x)[0] for x in ps]
        wrapped = '"__init__", [](%s* rh3dm_self%s) { new (rh3dm_self) %s((%s)(%s)); }' % (
            cls, ''.join(', ' + x for x in ps), cls, lam, ', '.join(names))
        out.append(text[i:m.start()] + '.def(' + wrapped)
        i = init_end


# Luis 2026-11-03: every enum integer-like (IntEnum): `e == 2`, `int(e)` keep working. (8.x's
# pybind11 enums had no `|` either; flags with nb::is_flag() would need specgen to
# record [Flags], so not now)
_ENUM = re.compile(r'(py::enum_<[^>]+>\((?:m|m\.attr\("\w+"\)), "\w+")\)')


def transform(text):
    text = _ENUM.sub(r'\1, py::is_arithmetic())', text)
    text = _props(text)
    text = _factories(text)
    return text


_DECL_CLASS = re.compile(r'^(?:class|struct)\s+(BND_\w+)\b', re.M)
_DECL = re.compile(r'\b(Gen_\w+)\(([^()]*)\)')
_ENUM_T = re.compile(r'py::enum_<([\w:]+)>')
_DEF_M = re.compile(r'\.def(?:_static)?\("\w+", &(BND_\w+)::(\w+)((?:, [^\n]*?)?)\)\s*$', re.M)


def _signatures(files):
    """(class, method) -> [parameter types], from the generated headers"""
    sig = {}
    for name, text in files.items():
        if not name.endswith('.h'):
            continue
        marks = [(m.start(), m.group(1)) for m in _DECL_CLASS.finditer(text)]
        for m in _DECL.finditer(text):
            cls = None
            for pos, c in marks:
                if pos < m.start():
                    cls = c
            if cls:
                ps = [x.strip() for x in _split(m.group(2))] if m.group(2).strip() else []
                sig[(cls, m.group(1))] = [re.sub(r'\s*\b\w+$', '', x) if re.search(r'\w\s+\w+$', x) else x for x in ps]
    return sig


def _arg_flags(text, sig, enums):
    """nanobind refuses None for a pointer unless `.none()`, and converts an int to an enum on its
    second pass unless `.noconvert()`: give each argument 8.x's (pybind11) behaviour (None reaches
    a pointer; a raw int is not an enum -- TypeError, as the tests pin)"""
    def flag(ptype):
        if '*' in ptype:
            return '.none()'
        if ptype.replace('const', '').replace('&', '').strip() in enums:
            return '.noconvert()'
        return ''

    def one(m):
        params = sig.get((m.group(1), m.group(2)))
        rest = m.group(3)
        if params is None or len(re.findall(r'py::arg\("\w+"\)', rest)) != len(params):
            return m.group(0)
        out = rest
        for i, a in enumerate(re.findall(r'py::arg\("(\w+)"\)(\.\w+\(\))?', rest)):
            f = flag(params[i])
            if f and not a[1]:
                out = out.replace('py::arg("%s")' % a[0], 'py::arg("%s")%s' % (a[0], f), 1)
        return m.group(0).replace(rest, out, 1) if out != rest else m.group(0)
    text = _DEF_M.sub(one, text)

    # property setters: `&BND_X::Gen_set_Y` taking a pointer or an enum
    def setter(m):
        cls, meth = m.group(1), m.group(2)
        params = sig.get((cls, meth)) or []
        f = flag(params[0]) if len(params) == 1 else ''
        if not f:
            return m.group(0)
        return m.group(0) + ', py::for_setter(py::arg("value")%s)' % f
    return re.sub(r'\.def_prop_rw\("\w+", [^\n]*?, &(BND_\w+)::(Gen_set_\w+)', setter, text)


def apply(files):
    """{name: content} -> the nanobind spelling of every generated C++ file"""
    out = {n: (transform(c) if n.endswith(('.cpp', '.h')) else c) for n, c in files.items()}
    sig = _signatures(out)
    enums = set()
    for c in out.values():
        enums.update(_ENUM_T.findall(c))
    return {n: (_arg_flags(c, sig, enums) if n.endswith('.cpp') else c) for n, c in out.items()}
