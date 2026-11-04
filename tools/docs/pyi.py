"""The Python typings (M5): src/rhino3dm/__init__.pyi from the built module and the doc comments.

    python3 tools/docs/pyi.py <raw.pyi> [<out.pyi>]

<raw.pyi> is nanobind's stub of the built `_rhino3dm` (tools/dev/container/python.sh stubs): the
truth for what exists -- every class, overload, parameter name and Python type, as registered. This
adds what the module cannot say: RhinoCommon's doc comments (api/docs.json, through tools/docs/
apimodel.py) as docstrings, and the C# types where nanobind only knows `object` (Guid -> uuid.UUID,
DateTime -> datetime.datetime). Default <out.pyi>: src/rhino3dm/__init__.pyi.
"""

import ast
import keyword
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
from apimodel import Api, ROOT  # noqa: E402

# C# types nanobind sees as `object`
OBJECT_TYPES = {
    'Guid': 'uuid.UUID', 'System.Guid': 'uuid.UUID',
    'Guid[]': 'list[uuid.UUID]', 'IEnumerable<Guid>': 'list[uuid.UUID]',
    'DateTime': 'datetime.datetime', 'System.DateTime': 'datetime.datetime',
}
NONE_VALUE = 'None_'   # RhinoCommon's `None` (a Python keyword): the module's alias of it (bnd_gen.py, M5)


def docstring(d, params=(), kind='method'):
    """a Google-style docstring from parsed docs"""
    if not d:
        return None
    parts = []
    if d.get('summary'):
        parts.append(d['summary'])
    if kind == 'property' and d.get('value') and d['value'] != d.get('summary'):
        parts.append(d['value'])
    if d.get('remarks'):
        parts.append(d['remarks'])
    ps = [(p, d['params'][p]) for p in params if d.get('params', {}).get(p)]
    if ps:
        parts.append('Args:\n' + '\n'.join('    %s: %s' % (p, t.replace('\n\n', '\n        ')) for p, t in ps))
    if d.get('returns') and kind != 'property':
        parts.append('Returns:\n    ' + d['returns'].replace('\n\n', '\n    '))
    if d.get('exceptions'):
        parts.append('Raises:\n' + '\n'.join('    %s: %s' % e for e in d['exceptions'] if e[1]))
    if d.get('deprecated'):
        parts.append('Deprecated since %s.' % d['deprecated'])
    return '\n\n'.join(p for p in parts if p.strip()) or None


def _set_doc(node, text, indent):
    """the docstring as the node's first statement; continuation lines at the body's indent
    (ast.unparse writes a string constant verbatim)"""
    if not text:
        return
    text = '\n'.join((' ' * indent + l) if l.strip() and i else l for i, l in enumerate(text.split('\n')))
    if '\n' in text:
        text += '\n' + ' ' * indent
    doc = ast.Expr(ast.Constant(text))
    body = node.body
    if body and isinstance(body[0], ast.Expr) and isinstance(getattr(body[0], 'value', None), ast.Constant) \
            and isinstance(body[0].value.value, str):
        body[0] = doc
    elif len(body) == 1 and isinstance(body[0], ast.Expr) and isinstance(body[0].value, ast.Constant) \
            and body[0].value.value is Ellipsis:
        node.body = [doc]
    else:
        node.body = [doc] + body


def camel_lower(n):
    return n[0].lower() + n[1:]


def value_ctor_doc(api, reg, fn, args, kind):
    """the constructors the bindings add to a value type: default, copy, from a sequence"""
    if kind != 'constructor':
        return {}
    fields = api.vt_fields.get(reg)
    if not args:
        return {'summary': "Every field zero: C#'s `default(%s)`." % reg, 'params': {}}
    if args == ['arg']:
        ann = ast.unparse(fn.args.args[1].annotation) if fn.args.args[1].annotation else ''
        if ann == reg:
            return {'summary': 'A copy of `arg`.', 'params': {}}
        if ann == 'Sequence' and fields:
            names = ', '.join(f[2:] if f.startswith('m_') else f for f in fields)
            return {'summary': 'From a sequence of %d numbers, in order: %s (rhino3dm only). '
                               'Raises ValueError for any other length.' % (len(fields), names), 'params': {}}
    return {}


def _kind(fn):
    decos = [ast.unparse(d) for d in fn.decorator_list]
    if fn.name == '__init__':
        return 'constructor', False
    if 'property' in decos or any(d.endswith('.setter') for d in decos):
        return 'property', 'staticmethod' in decos
    return 'method', 'staticmethod' in decos


class Stats:
    def __init__(self):
        self.members = self.documented = self.matched = self.retyped = 0
        self.unreachable = []           # (class, member, signature): overloads Python can never select


PRIMS = {'double': 'float', 'float': 'float', 'int': 'int', 'uint': 'int', 'long': 'int', 'ulong': 'int',
         'short': 'int', 'ushort': 'int', 'byte': 'int', 'bool': 'bool', 'string': 'str',
         'Guid': 'uuid.UUID', 'System.Guid': 'uuid.UUID', 'DateTime': 'datetime.datetime',
         # System.Drawing values are tuples in rhino3dm: (r, g, b, a) and (x, y, width, height) (D38)
         'Color': 'tuple[int, int, int, int]', 'System.Drawing.Color': 'tuple[int, int, int, int]',
         'Rectangle': 'tuple[int, int, int, int]', 'System.Drawing.Rectangle': 'tuple[int, int, int, int]'}
KNOWN = set()          # the stub's classes, filled by build()


def py_type(cs):
    """a C# type's Python spelling in the stub, or None when unsure"""
    cs = (cs or '').strip()
    if cs.endswith('?'):
        t = py_type(cs[:-1])
        return t and t + ' | None'
    m = re.match(r'^(.+)\[\]$', cs) or re.match(r'^(?:IEnumerable|IList|List|IReadOnlyList)<(.+)>$', cs)
    if m:
        t = py_type(m.group(1))
        return t and 'list[%s]' % t
    if cs in PRIMS:
        return PRIMS[cs]
    short = cs.split('.')[-1]
    return short if short in KNOWN else None


def _results(m):
    """`(result, out..., ref...)`: the tuple a member with out / ref parameters returns"""
    outs = [p for p in (m.get('Params') or []) if p.get('Mode') in ('out', 'ref')]
    if not outs:
        return None
    ts = ([] if m.get('Returns') in (None, 'void') else [py_type(m['Returns'])]) + [py_type(p['Type']) for p in outs]
    return 'tuple[%s]' % ', '.join(ts) if ts and all(ts) else None


def _retype(ann, cs_type, stats):
    """`object` -> the C# type's Python spelling, when known"""
    if ann is None or cs_type is None:
        return ann
    if ast.unparse(ann) == 'object' and cs_type in OBJECT_TYPES:
        stats.retyped += 1
        return ast.parse(OBJECT_TYPES[cs_type], mode='eval').body
    if ast.unparse(ann) == 'tuple' and py_type(cs_type) and py_type(cs_type).startswith('tuple['):
        stats.retyped += 1                      # a System.Drawing value as its tuple (D38)
        return ast.parse(py_type(cs_type), mode='eval').body
    return ann


def process_class(api, cls, stats, depth=0):
    reg = cls.name
    is_enum = any(ast.unparse(b).endswith('Enum') for b in cls.bases)
    ind = 4 * (depth + 1)
    _set_doc(cls, docstring(api.type_doc(reg) or api.hand_doc(reg, None)), ind)
    if is_enum:
        body, names = [], set()
        for st in cls.body:
            if isinstance(st, ast.Assign) and isinstance(st.targets[0], ast.Name):
                if st.targets[0].id in names:
                    continue                        # the alias, listed by stubgen too
                names.add(st.targets[0].id)
            body.append(st)
            if isinstance(st, ast.Assign) and isinstance(st.targets[0], ast.Name):
                v = st.targets[0].id
                if v == NONE_VALUE:
                    d = api.enum_value_doc(reg, 'None')
                    body.append(ast.Expr(ast.Constant(
                        "RhinoCommon's `None` (`%s.None` is not Python syntax; `%s['None']` is the same member). %s"
                        % (reg, reg, d.get('summary', '')))))
                    continue
                d = api.enum_value_doc(reg, v)
                if d.get('summary'):
                    body.append(ast.Expr(ast.Constant(d['summary'].replace('\n', '\n' + ' ' * ind))))
        cls.body = body
        return
    body = []
    for st in cls.body:
        body.append(st)
        if isinstance(st, ast.AnnAssign) and ast.unparse(st.annotation) == '_StaticOrInstance':
            # one name, static AND instance (G132: BoundingBox.Union): no type checker spells that,
            # so `Any`, and both forms in the docstring
            name = st.target.id
            st.annotation = ast.Name('Any')
            forms = [m for t in api.base_types(api.cs_of.get(reg, '')) for m in api.by_type.get(t, [])
                     if m['Name'] == name and m['Kind'] == 'method'] if api.cs_of.get(reg) else []
            lines = []
            for m in forms:
                ps = ', '.join(p['Name'] for p in m.get('Params') or [])
                call = '%s.%s(%s)' % (reg, name, ps) if m.get('Static') else '%s.%s(%s)' % (camel_lower(reg), name, ps)
                d = api.doc(m)
                lines.append('%s: %s' % (call, d.get('summary', '') if d else ''))
            if lines:
                body.append(ast.Expr(ast.Constant(('Both forms (static and instance):\n\n' + '\n'.join(lines))
                                                  .replace('\n', '\n' + ' ' * ind) + '\n' + ' ' * ind)))
    cls.body = body
    for st in cls.body:
        if isinstance(st, ast.ClassDef):
            process_class(api, st, stats, depth + 1)
            continue
        if isinstance(st, ast.FunctionDef) and st.name == '__iter__' and st.returns is not None \
                and ast.unparse(st.returns) == 'object':
            it = reg + 'Iter' if reg + 'Iter' in KNOWN else (reg if reg.endswith('Iter') else None)
            if it:
                st.returns = ast.parse(it, mode='eval').body
        if not isinstance(st, ast.FunctionDef) or (st.name.startswith('__') and st.name != '__init__'):
            continue
        kind, static = _kind(st)
        args = [a.arg[:-1] if a.arg[:-1] in keyword.kwlist else a.arg for a in st.args.args + st.args.kwonlyargs]
        if args and args[0] == 'self':
            args = args[1:]
        stats.members += 1
        if any(ast.unparse(d).endswith('.setter') for d in st.decorator_list):
            stats.members -= 1                  # the property's getter carries its docs; the value's type
            m = api.member(reg, st.name, 'property')
            v = st.args.args[1] if len(st.args.args) > 1 else None
            t = py_type(m.get('Returns')) if m and v is not None and v.annotation is not None \
                and ast.unparse(v.annotation) in ('object', 'tuple') else None
            if t:
                stats.retyped += 1
                v.annotation = ast.parse(t, mode='eval').body
            continue
        m = api.member(reg, st.name, kind, params=args if kind != 'property' else None,
                       static=static if kind == 'method' else None)
        if m is None:
            # a rhino3dm member's Guid parameters (`id`, `parentId`) are uuid.UUID like generated ones
            for a in st.args.args:
                if a.annotation is not None and ast.unparse(a.annotation) == 'object' and \
                        (a.arg == 'id' or a.arg.endswith('Id')):
                    stats.retyped += 1
                    a.annotation = ast.parse('uuid.UUID', mode='eval').body
            d = api.hand_doc(reg, st.name, args) or value_ctor_doc(api, reg, st, args, kind)
            if d:
                stats.documented += 1
                _set_doc(st, docstring(d, args, kind), ind + 4)
            continue
        stats.matched += 1
        # parameter and return types nanobind left as `object`
        ptypes = {p['Name']: p['Type'] for p in (m.get('Params') or [])}
        for a in st.args.args + st.args.kwonlyargs:
            a.annotation = _retype(a.annotation, ptypes.get(a.arg.rstrip('_')), stats)
        if kind != 'constructor':
            ret = ast.unparse(st.returns) if st.returns else ''
            precise = None
            if ret == 'tuple':
                precise = _results(m) or py_type(m.get('Returns'))
            elif ret == 'object' and (m.get('Returns') or '').endswith('[]'):
                t = py_type(m['Returns'])
                precise = t and t + ' | None'              # an array C# may return as null
            if precise:
                stats.retyped += 1
                st.returns = ast.parse(precise, mode='eval').body
            else:
                st.returns = _retype(st.returns, m.get('Returns'), stats)
        d = api.doc(m) or api.hand_doc(reg, st.name, args)
        text = docstring(d, args, kind)
        if text:
            stats.documented += 1
            _set_doc(st, text, ind + 4)


BARE = {'tuple': 'tuple[Any, ...]', 'list': 'list[Any]', 'dict': 'dict[str, Any]', 'Sequence': 'Sequence[Any]',
        'Iterator': 'Iterator[Any]'}


def _sig(fn):
    """an overload's parameter types: what selects it at run time"""
    return tuple(ast.unparse(a.annotation) if a.annotation is not None else ''
                 for a in fn.args.posonlyargs + fn.args.args if a.arg != 'self') + (bool([d for d in fn.decorator_list if ast.unparse(d) == 'staticmethod']),)


def tidy(tree, stats):
    """what a type checker needs that the module cannot say: unreachable overloads dropped, bare
    generics parameterized, class names a member shadows qualified, buffer slots typed, `__eq__` on
    `object`"""
    classes = {c.name: c for c in ast.walk(tree) if isinstance(c, ast.ClassDef)}
    module_names = {c.name for c in tree.body if isinstance(c, ast.ClassDef)}

    def members(c):
        return {s.name for s in c.body if isinstance(s, (ast.FunctionDef, ast.ClassDef))} | \
            {s.target.id for s in c.body if isinstance(s, ast.AnnAssign) and isinstance(s.target, ast.Name)}

    for c in classes.values():
        shadow = members(c) & module_names
        body, seen = [], {}
        for st in c.body:
            if isinstance(st, ast.FunctionDef) and any(ast.unparse(d) == 'overload' for d in st.decorator_list):
                k = (st.name, _sig(st))
                if k in seen:
                    # nanobind tries overloads in order: the first with these parameter types wins
                    stats.unreachable.append((c.name, st.name, ast.unparse(st.args)))
                    continue
                seen[k] = st
            body.append(st)
        c.body = body
        # an overload left alone is a plain function
        count = {}
        for st in c.body:
            if isinstance(st, ast.FunctionDef):
                count[st.name] = count.get(st.name, 0) + 1
        for st in c.body:
            if isinstance(st, ast.FunctionDef) and count[st.name] == 1:
                st.decorator_list = [d for d in st.decorator_list if ast.unparse(d) != 'overload']
            if isinstance(st, ast.FunctionDef) and st.name in ('__eq__', '__ne__'):
                # object's signature (any other object compares unequal); typeshed's convention
                (st.args.posonlyargs + st.args.args)[1].annotation = ast.Name('object')
        for st in c.body:
            if isinstance(st, ast.FunctionDef) and st.name in ('__buffer__', '__release_buffer__'):
                (st.args.posonlyargs + st.args.args)[1].annotation = ast.parse('int' if st.name == '__buffer__' else 'memoryview', mode='eval').body
                st.returns = ast.parse('memoryview' if st.name == '__buffer__' else 'None', mode='eval').body
            for node in ast.walk(st):
                for attr in ('annotation', 'returns'):
                    ann = getattr(node, attr, None)
                    if ann is None:
                        continue
                    for n in ast.walk(ann):
                        if isinstance(n, ast.Name) and n.id in shadow:
                            n.id = '_rh.' + n.id            # a member named like the class it returns
                    if isinstance(ann, ast.Name) and ann.id in BARE:
                        setattr(node, attr, ast.parse(BARE[ann.id], mode='eval').body)
                    _param_bare(getattr(node, attr))


def _param_bare(ann):
    """`tuple[bool, list]` -> `tuple[bool, list[Any]]`: bare generics inside a subscript"""
    for n in ast.walk(ann):
        if isinstance(n, ast.Subscript):
            sl = n.slice
            items = sl.elts if isinstance(sl, ast.Tuple) else [sl]
            for i, it in enumerate(items):
                if isinstance(it, ast.Name) and it.id in BARE:
                    new = ast.parse(BARE[it.id], mode='eval').body
                    if isinstance(sl, ast.Tuple):
                        sl.elts[i] = new
                    else:
                        n.slice = new


def _triple_quote(text):
    """attribute and enum-value docstrings (a string statement after an assignment) as ordinary
    triple-quoted docstrings: ast.unparse writes any string but a body's first one as a repr"""
    out = []
    for line in text.split('\n'):
        m = re.match(r"^(\s+)('(?:[^'\\]|\\.)*'|\"(?:[^\"\\]|\\.)*\")$", line)
        if m:
            v = ast.literal_eval(m.group(2))
            if '"""' not in v and not v.endswith('"') and '\\' not in v:
                line = m.group(1) + '"""' + v + '"""'
        out.append(line)
    return '\n'.join(out)


def build(raw_text, api):
    src = re.sub(r'^(\s+)None = ', r'\1%s = ' % NONE_VALUE, raw_text, flags=re.M)
    src = src.replace('"std::pair<int, int>"', 'tuple[int, int]')
    # a C# parameter named after a Python keyword (Line(from, to)): positional only in Python; the
    # stub spells it `from_` (PEP 8), matched back to RhinoCommon's name below
    kw = '|'.join(k for k in keyword.kwlist if k not in ('None', 'True', 'False'))
    src = re.sub(r'([(,] *)(%s)(: )' % kw, r'\1\2_\3', src)
    tree = ast.parse(src)
    KNOWN.update(n.name for n in ast.walk(tree) if isinstance(n, ast.ClassDef))
    stats = Stats()
    for st in tree.body:
        if isinstance(st, ast.ClassDef):
            process_class(api, st, stats)
    tidy(tree, stats)
    imports = [ast.parse('import datetime').body[0], ast.parse('import uuid').body[0],
               ast.parse('from typing import Any').body[0], ast.parse('import rhino3dm as _rh').body[0]]
    tree.body = [s for s in tree.body if isinstance(s, (ast.Import, ast.ImportFrom))] + imports + \
                [s for s in tree.body if not isinstance(s, (ast.Import, ast.ImportFrom))]
    out = ast.unparse(tree)
    out = re.sub(r'\b_rhino3dm\.', '', out)          # the native module's qualifier: the package re-exports it
    out = _triple_quote(out)
    out = re.sub(r'[ \t]+$', '', out, flags=re.M)
    header = ('"""rhino3dm: Python bindings generated from RhinoCommon (rhino3dm 9.x).\n\n'
              'Generated by tools/docs/pyi.py from the built module and RhinoCommon\'s doc comments;\n'
              'do not edit."""\n')
    return header + out + '\n', stats


def main():
    raw = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'src', 'rhino3dm', '__init__.pyi')
    text, s = build(open(raw).read(), Api())
    open(out, 'w').write(text)
    print('pyi: %d members, %d matched to RhinoCommon, %d documented, %d object types made precise, '
          '%d unreachable overloads dropped -> %s'
          % (s.members, s.matched, s.documented, s.retyped, len(s.unreachable), os.path.relpath(out, ROOT)))
    for u in s.unreachable:
        print('  unreachable: %s.%s(%s)' % u)


if __name__ == '__main__':
    main()
