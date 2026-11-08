"""The JavaScript typings (M5): src/js/rhino3dm.d.ts from the generated registrations.

    python3 tools/docs/dts.py [<__init__.pyi>] [<out.d.ts>]

What exists in JS is the surface lock's `js` lines (api/generated-surface.lock: every class, function
and property embind registers). Each JS member is the same generated member as its Python one (one
plan, two bindings; JS names are camelCase), so its parameter names and types are read from the
finished Python typings (tools/docs/pyi.py output) and spelled for TypeScript; the docs come from
RhinoCommon's comments in JS vocabulary (`null`, `true`). JS-only shapes: value types are plain
objects (`{X, Y, Z}`) whose members are static functions taking the value first (G106), colors are
`{r, g, b, a}`, Guids strings, byte arrays Uint8Array.
"""

import ast
import glob
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
from apimodel import Api, ROOT  # noqa: E402
from pyi import py_type as pyi_type, KNOWN  # noqa: E402

BINDINGS = os.path.join(ROOT, 'src', 'bindings')

NESTED = {}            # nested enums: `DimensionStyle.ArrowFit` -> `DimensionStyleArrowFit` (filled by Py)
COLOR = '{ r: number, g: number, b: number, a: number }'
CS_TS = {'Color': COLOR, 'System.Drawing.Color': COLOR}


def camel(n):
    return n[0].lower() + n[1:] if n else n


class Js:
    """the JS registrations: classes, members and kinds (lock), bases and value-object fields (sources)"""

    def __init__(self):
        # the surface lock's parser over every bindings source: the generated files (the lock itself)
        # and the hand-written ones (Bitmap, File3dmBitmapTable), which the lock does not cover
        import surface_lock
        files = {os.path.basename(p): open(p).read() for p in glob.glob(os.path.join(BINDINGS, '*.cpp'))}
        self.classes = {}
        for line in sorted(surface_lock.surface(files)):
            m = re.match(r'^js  (\w+)\.(\S+?)(\(.*?\))? \[(\w+)\]', line) or re.match(r'^js  (\w+)\.(constructor)(.*)$', line)
            if not m:
                continue
            cls, name = m.group(1), m.group(2)
            kind = m.group(4) if m.lastindex and m.lastindex >= 4 else 'constructor'
            self.classes.setdefault(cls, {}).setdefault(name, set()).add(kind)
        for text in files.values():
            for c in re.finditer(r'(?<!py::)\bclass_<[^;]*?>\("(\w+)"\)', text):
                self.classes.setdefault(c.group(1), {})
        self.bases, self.values, self.enums_js = {}, {}, set()
        vt_types = {}
        for path in sorted(glob.glob(os.path.join(BINDINGS, '*_gen.cpp'))):
            text = open(path).read()
            for b in re.finditer(r'class_<BND_(\w+), base<BND_(\w+)>>\("(\w+)"\)', text):
                self.bases[b.group(3)] = b.group(2)
            # a field is a member (`&BND_Arc::m_radius`) or a getter/setter pair whose setter names its
            # C++ type (`+[](BND_Cylinder& s, BND_Plane v)`): ControlPoint, Cylinder, ComponentIndex
            for v in re.finditer(r'value_object<(BND_\w+)>\("(\w+)Value"\)(.*?);\n', text, re.S):
                self.values[v.group(2)] = (v.group(1), [
                    (jn, mem or None, (sett or '').replace('RH3DM_', '') or None)
                    for jn, mem, sett in re.findall(r'\.field\("(\w+)", (?:&\w+::(\w+)\)|\+\[\].*?, \+\[\]\(\w+& s, (\w+) v\))',
                                                    v.group(3))])
            for e in re.finditer(r'enum_<[^>]+>\("(\w+)"\)', text):
                self.enums_js.add(e.group(1))
        # the classes the module makes iterable at load (G101 post-js: ENUMERABLE, ITERS, GRID)
        post = open(os.path.join(BINDINGS, 'bnd_post_gen.js')).read()
        self.iterable = set()
        for name in ('ENUMERABLE', 'ITERS'):
            self.iterable.update(json.loads(re.search(r'var %s = (\[.*?\]);' % name, post).group(1)))
        self.grid = {g[0]: g[3] for g in json.loads(re.search(r'var GRID = (\[.*?\]\]);', post).group(1))}
        self.iterable.update(self.grid)


def item_type(py, pyc, grid=None):
    """the TypeScript type of what iterating a class yields, from its Python typings (`grid`: the JS
    getter a grid enumerator calls for each (u, v))"""
    if grid:
        g = pyc.get(grid[0].upper() + grid[1:])
        if g:
            return ts_of(g[0][1].returns)
    if '__next__' in pyc:
        return ts_of(pyc['__next__'][0][1].returns)
    if '__iter__' in pyc:
        it = ast.unparse(pyc['__iter__'][0][1].returns)
        n = py.classes.get(it, {}).get('__next__')
        if n:
            return ts_of(n[0][1].returns)
    if '__getitem__' in pyc:
        return ts_of(pyc['__getitem__'][0][1].returns)
    return 'any'


def ts_of(node, cs=None):
    """a Python annotation (ast) -> TypeScript; `cs`, the C# type, where JS differs from Python"""
    if cs in CS_TS:
        return CS_TS[cs]
    if node is None:
        return 'any'
    t = ast.unparse(node)
    return ts_text(t)


def ts_text(t):
    t = t.strip().replace('_rh.', '')          # the stub's self-qualified names (a member shadows the class)
    parts = _split(t, '|')
    if len(parts) > 1:
        return ' | '.join('null' if p.strip() == 'None' else ts_text(p) for p in parts)
    m = re.match(r'^(list|Sequence)\[(.*)\]$', t)
    if m:
        inner = ts_text(m.group(2))
        return '(%s)[]' % inner if ' ' in inner else inner + '[]'
    m = re.match(r'^tuple\[(.*), \.\.\.\]$', t)
    if m:
        inner = ts_text(m.group(1))
        return '(%s)[]' % inner if ' ' in inner else inner + '[]'
    m = re.match(r'^dict\[str, (.*)\]$', t)
    if m:
        return 'Record<string, %s>' % ts_text(m.group(1))
    m = re.match(r'^Iterator\[(.*)\]$', t)
    if m:
        return 'Iterator<%s>' % ts_text(m.group(1))
    m = re.match(r'^tuple\[(.*)\]$', t)
    if m:
        return '[%s]' % ', '.join(ts_text(x) for x in _split(m.group(1)))
    if t in NESTED:
        return NESTED[t]
    return {
        'float': 'number', 'int': 'number', 'bool': 'boolean', 'str': 'string', 'None': 'void',
        'uuid.UUID': 'string', 'datetime.datetime': 'Date', 'bytes': 'Uint8Array', 'dict': 'Record<string, any>',
        'object': 'any', 'Any': 'any', 'tuple': 'any[]', 'list': 'any[]', 'Sequence': 'any[]',
    }.get(t, t)


def _split(s, sep=','):
    out, depth, cur = [], 0, ''
    for ch in s:
        depth += ch == '['
        depth -= ch == ']'
        if ch == sep and depth == 0:
            out.append(cur)
            cur = ''
        else:
            cur += ch
    return out + [cur] if cur.strip() else out


class Py:
    """the finished Python typings, per class: overloads of each member, properties, enums"""

    def __init__(self, path):
        # the stub's `None_` is Python's alias of RhinoCommon's `None`, which JS spells as is
        text = open(path).read()
        tree = ast.parse(text)
        self.classes, self.enums = {}, {}
        for st in tree.body:
            if isinstance(st, ast.ClassDef):
                self._cls(st)

    def _cls(self, c, outer=''):
        if any(ast.unparse(b).endswith('Enum') for b in c.bases):
            vals = []
            for s in c.body:
                if isinstance(s, ast.Assign):
                    n = s.targets[0].id
                    vals.append(('None' if n == 'None_' else n, ast.unparse(s.value)))
            # JS registers a nested enum flat: DimensionStyle.ArrowFit -> DimensionStyleArrowFit
            self.enums[outer + c.name] = vals
            if outer:
                NESTED[outer[:-1] + '.' + c.name] = outer[:-1] + c.name
            return
        members = self.classes.setdefault(c.name, {})
        for s in c.body:
            if isinstance(s, ast.ClassDef):
                self._cls(s, c.name + '.')
            elif isinstance(s, ast.FunctionDef):
                decos = [ast.unparse(d) for d in s.decorator_list]
                if any(d.endswith('.setter') for d in decos):
                    members.setdefault(s.name, []).append(('setter', s))
                elif 'property' in decos:
                    members.setdefault(s.name, []).append(('property', s))
                else:
                    members.setdefault(s.name, []).append(('static' if 'staticmethod' in decos else 'method', s))
            elif isinstance(s, ast.AnnAssign) and isinstance(s.target, ast.Name):
                members.setdefault(s.target.id, []).append(('attr', s))


def tsdoc(d, params=(), indent='\t\t'):
    if not d:
        return []
    lines = []
    if d.get('summary'):
        lines += d['summary'].split('\n')
    if d.get('remarks'):
        lines += [''] + d['remarks'].split('\n')
    for p in params:
        if d.get('params', {}).get(p):
            lines.append('@param %s %s' % (p, ' '.join(d['params'][p].split())))
    if d.get('returns'):
        lines.append('@returns ' + ' '.join(d['returns'].split()))
    if d.get('deprecated'):
        lines.append('@deprecated since %s' % d['deprecated'])
    if not lines:
        return []
    return [indent + '/**'] + [indent + (' * ' + l if l else ' *') for l in lines] + [indent + ' */']


def _params(fn, skip_self=True, lead=None):
    """(TS parameter list, RhinoCommon names); `lead`: (name, type) of a value type's value argument"""
    args = fn.args.args[1:] if skip_self and fn.args.args and fn.args.args[0].arg == 'self' else fn.args.args
    out = []
    if lead:
        ln = lead[0] if lead[0] not in [a.arg for a in args] else lead[0] + 'Value'
        out.append('%s: %s' % (ln, lead[1]))
    names = []
    for a in args:
        n = a.arg[:-1] if a.arg.endswith('_') else a.arg
        names.append(n)
        out.append('%s: %s' % (n if n not in ('function', 'var', 'new', 'delete', 'default', 'in') else n + '_', ts_of(a.annotation)))
    return out, names


def _cs_params(ps, m):
    """`ps` (from _params) with each parameter whose C# type JS spells differently retyped: a colour is
    `{r, g, b, a}`, though Python spells it like a Rectangle (`tuple[int, int, int, int]`; pilot #101)"""
    ins = [p for p in ((m or {}).get('Params') or []) if p.get('Mode') not in ('out', 'ref')]
    lead = len(ps) - len(ins)
    if not ins or lead < 0:
        return ps
    out = list(ps)
    for i, p in enumerate(ins):
        if p.get('Type') in CS_TS:
            out[lead + i] = out[lead + i].split(': ', 1)[0] + ': ' + CS_TS[p['Type']]
    return out


# a JS name for one Python overload (HAND_MEMBERS `js`: JS cannot overload on argument types)
def _hand_js():
    from gen_registry import HAND_MEMBERS
    out = {}
    for cls, es in HAND_MEMBERS.items():
        for e in es:
            if e.get('js') and e['js'] != camel(e['name']):
                out[(cls, e['js'])] = (e['name'], e.get('args') or [])
    return out


HAND_JS = None
# value-type operators by RhinoCommon method name (G106) when the type has no such method
VT_OPS = {'add': '__add__', 'subtract': '__sub__', 'multiply': '__mul__', 'divide': '__truediv__',
          'negate': '__neg__', 'equals': '__eq__'}


def js_only(api, cls, jn, static, value, pyc):
    """declarations of a JS member with no Python member of its name, or None"""
    global HAND_JS
    if HAND_JS is None:
        HAND_JS = _hand_js()
    t = '\t\t'
    s = 'static ' if static else ''
    if (cls, jn) in HAND_JS:
        pn, args = HAND_JS[(cls, jn)]
        for kind, fn in pyc.get(pn, []):
            ps, names = _params(fn)
            if names == list(args):
                d = api.hand_doc(cls, pn, names)
                return tsdoc(d, names) + [t + '%s%s(%s): %s;' % (s, jn, ', '.join(ps), ts_of(fn.returns))]
    d = api.hand_doc(cls, jn)
    if d.get('ts'):
        return tsdoc(d, list(d.get('params') or {})) + [t + d['ts'] + ';']
    if jn in ('get', 'set') and ('__getitem__' in pyc):
        g = pyc['__getitem__'][0][1]
        it = ts_of(g.returns)
        ix = ts_of(g.args.args[1].annotation) if len(g.args.args) > 1 else 'number'
        if jn == 'get':
            return [t + '/** The item at `index` (Python: `x[index]`). */', t + 'get(index: %s): %s;' % (ix, it)]
        return [t + '/** Replaces the item at `index` (Python: `x[index] = value`). */',
                t + 'set(index: %s, value: %s): void;' % (ix, it)]
    if jn == 'iterator':
        return [t + '/** An iterator over the items (`for...of` works on the object itself). */',
                t + 'iterator(): %s;' % (cls + 'Iter' if (cls + 'Iter') in KNOWN else 'any')]
    if jn == 'next' and cls.endswith('Iter'):
        n = pyc.get('__next__', [(None, None)])[0][1]
        return [t + '/** The next item: `{done: true}` at the end. */',
                t + 'next(): { done: boolean, value?: %s };' % (ts_of(n.returns) if n is not None else 'any')]
    if value and jn in VT_OPS and VT_OPS[jn] in pyc:
        res = []
        for kind, fn in pyc[VT_OPS[jn]]:
            ps, names = _params(fn, lead=(camel(cls), cls))
            res.append(t + 'static %s(%s): %s;' % (jn, ', '.join(ps).replace('arg: ', 'other: '), ts_of(fn.returns)))
        return res
    return None


def build(api, js, py):
    out = []
    w = out.append
    w('// rhino3dm.js typings: generated by tools/docs/dts.py from the generated registrations, the Python')
    w('// typings (types) and RhinoCommon\'s doc comments. Do not edit.')
    w("declare module 'rhino3dm' {")
    w('')
    w(OPTIONS)
    w('\texport default function rhino3dm(moduleOptions?: RhinoModuleOptions): Promise<RhinoModule>;')
    w('')
    stats = {'members': 0, 'typed': 0, 'documented': 0}
    # enums: the Python IntEnum classes that JS registers too
    enums = []
    for e in sorted(py.enums):
        flat = e.replace('.', '')
        if flat not in js.enums_js:
            continue
        enums.append(flat)
        w('\tenum %s {' % flat)
        w(',\n'.join('\t\t%s = %s' % (n, v) for n, v in py.enums[e]))
        w('\t}')
        w('')
    def chain(c):
        out = []
        while c:
            out.append(c)
            c = js.bases.get(c)
        return out

    for cls in sorted(js.classes):
        members = js.classes[cls]
        # the Python members of the class and its bases (the stub lists an inherited member once, on
        # the base); TypeScript also needs a base's overloads repeated on an override
        pyc = {}
        for c in chain(cls):
            for n, ovs in py.classes.get(c, {}).items():
                if n not in pyc:
                    pyc[n] = list(ovs)
                elif c != cls and pyc[n] and pyc[n][0][0] in ('method', 'static'):
                    have = {ast.unparse(fn.args) for _, fn in pyc[n]}
                    pyc[n] += [(k, fn) for k, fn in ovs if ast.unparse(fn.args) not in have]
        value = cls in js.values
        if value:
            cpp, fields = js.values[cls]
            ftypes = {f['Name']: f['Type'] for t in api.vt_raw if t['Cpp'] == cpp for f in t['Fields']}
            w('\t/** The plain-object form of a %s value (fields as in RhinoCommon). */' % cls)
            w('\tinterface %s {' % cls)
            for jn, f, st in fields:
                ct = ftypes.get(f, 'double') if f else st
                w('\t\t%s: %s;' % (jn, 'number' if ct in ('double', 'float', 'int') else ct.replace('BND_', '')))
            w('\t}')
        doc = api.type_doc(cls, 'js') or api.hand_doc(cls, None)
        out.extend(tsdoc(doc, indent='\t'))
        base = js.bases.get(cls)
        w('\tclass %s%s {' % (cls, ' extends %s' % base if base else ''))
        if value and 'create' not in members:
            w('\t\tprivate constructor();')
        for jn in sorted(members):
            kinds = members[jn]
            stats['members'] += 1
            if 'constructor' in kinds:
                for kind, fn in pyc.get('__init__', []):
                    ps, names = _params(fn)
                    m = api.member(cls, '__init__', 'constructor', params=names)
                    out.extend(tsdoc(api.doc(m, 'js') or api.hand_doc(cls, '__init__', names), names))
                    w('\t\tconstructor(%s);' % ', '.join(_cs_params(ps, m)))
                if not pyc.get('__init__'):
                    w('\t\tconstructor(...args: any[]);')
                continue
            pn = next((p for p in pyc if camel(p) == jn or p == jn), None)
            if value and jn == 'create' and pn is None:
                # a value type's constructors are `create` in JS (G106): Python's __init__ overloads
                for kind, fn in pyc.get('__init__', []):
                    ps, names = _params(fn)
                    if names == ['arg']:
                        continue                    # Python's copy / sequence forms
                    m = api.member(cls, '__init__', 'constructor', params=names)
                    d = api.doc(m, 'js') or api.hand_doc(cls, '__init__', names)
                    ps = _cs_params(ps, m)
                    stats['typed'] += 1
                    stats['documented'] += bool(d)
                    out.extend(tsdoc(d, names))
                    w('\t\tstatic create(%s): %s;' % (', '.join(ps), cls))
                    # `new` calls create and yields the same plain object (D92)
                    out.extend(tsdoc(d, names))
                    w('\t\tconstructor(%s);' % ', '.join(ps))
                continue
            overloads = pyc.get(pn, []) if pn else []
            static = 'class_function' in kinds
            if overloads and overloads[0][0] == 'attr' and ast.unparse(overloads[0][1].annotation) == '_StaticOrInstance':
                # one name, static AND instance (G132: BoundingBox.Union): both, from RhinoCommon
                for m in [m for t in api.base_types(api.cs_of[cls]) for m in api.by_type.get(t, [])
                          if m['Name'] == pn and m['Kind'] == 'method']:
                    ps = [('%s: %s' % (p['Name'], ts_text(pyi_type(p['Type']) or 'object'))) for p in m['Params'] or []]
                    if not m.get('Static') and value:
                        ps.insert(0, '%s: %s' % (camel(cls), cls))
                    d = api.doc(m, 'js')
                    stats['typed'] += 1
                    stats['documented'] += bool(d)
                    out.extend(tsdoc(d, [p['Name'] for p in m['Params'] or []]))
                    w('\t\t%s%s(%s): %s;' % ('static ' if m.get('Static') or value else '', jn, ', '.join(ps),
                                              ts_text(pyi_type(m['Returns']) or 'object')))
                continue
            if 'property' in kinds or (overloads and overloads[0][0] in ('property', 'attr')):
                kind, fn = overloads[0] if overloads else (None, None)
                ann = fn.returns if kind == 'property' else (fn.annotation if kind == 'attr' else None)
                m = api.member(cls, pn, 'property') if pn else None
                ro = not any(k == 'setter' for k, _ in overloads)
                if fn is not None:
                    stats['typed'] += 1
                d = api.doc(m, 'js') or (api.hand_doc(cls, pn) if pn else {})
                stats['documented'] += bool(d)
                out.extend(tsdoc(d))
                pre = 'static ' if static or kind == 'attr' else ''
                if value and kind == 'property':
                    # a value type's property is a function of the value (G106)
                    w('\t\tstatic %s(%s: %s): %s;' % (jn, camel(cls), cls, ts_of(ann, m and m.get('Returns'))))
                else:
                    w('\t\t%s%s%s: %s;' % (pre, 'readonly ' if ro else '', jn, ts_of(ann, m and m.get('Returns'))))
                continue
            if not overloads:
                lines = js_only(api, cls, jn, static, value, pyc)
                if lines:
                    stats['typed'] += 1
                    stats['documented'] += any('/**' in l for l in lines)
                    out.extend(lines)
                    continue
                d = api.hand_doc(cls, jn[0].upper() + jn[1:])
                out.extend(tsdoc(d))
                w('\t\t%s%s(...args: any[]): any;' % ('static ' if static else '', jn))
                continue
            for kind, fn in overloads:
                if kind not in ('method', 'static'):
                    continue
                lead = (camel(cls), cls) if value and kind == 'method' else None
                ps, names = _params(fn, lead=lead)
                m = api.member(cls, pn, 'method', params=names, static=(kind == 'static'))
                ps = _cs_params(ps, m)
                d = api.doc(m, 'js') or api.hand_doc(cls, pn, names)
                stats['typed'] += 1
                stats['documented'] += bool(d)
                out.extend(tsdoc(d, names))
                ret = ts_of(fn.returns, m and m.get('Returns'))
                w('\t\t%s%s(%s): %s;' % ('static ' if static or value else '', jn, ', '.join(ps), ret))
        if cls in js.iterable:
            # the module makes these iterable at load (bnd_post_gen.js); pilot #89
            w('\t\t/** Iterates the items: `for (const x of %s)`. */' % camel(cls))
            w('\t\t[Symbol.iterator](): Iterator<%s>;' % item_type(py, pyc, js.grid.get(cls)))
        w('\t}')
        w('')
    w('\tclass RhinoModule {')
    for e in enums:
        w('\t\t%s: typeof %s;' % (e, e))
    for cls in sorted(js.classes):
        w('\t\t%s: typeof %s;' % (cls, cls))
    w('\t}')
    w('}')
    return '\n'.join(out) + '\n', stats


OPTIONS = '''\t/**
\t * Options accepted by the `rhino3dm()` module factory: the standard Emscripten module-configuration
\t * object. The commonly used members are typed here; any other Emscripten `Module` member may be passed.
\t */
\texport interface RhinoModuleOptions {
\t\t/**
\t\t * Override where `rhino3dm.wasm` is loaded from. Receives the file name (`'rhino3dm.wasm'`) and the
\t\t * directory the script was loaded from, and returns the URL to fetch. Use this when a bundler (Vite,
\t\t * webpack, Next.js, ...) does not serve the .wasm next to the .js.
\t\t * @example
\t\t * const rhino = await rhino3dm({ locateFile: (file) => `/wasm/${file}` });
\t\t */
\t\tlocateFile?: (path: string, scriptDirectory: string) => string;
\t\t/** Supply the wasm bytes directly instead of having the module fetch `rhino3dm.wasm`. */
\t\twasmBinary?: ArrayBuffer | Uint8Array;
\t\t/** Called once the wasm runtime is initialized (before the returned promise resolves). */
\t\tonRuntimeInitialized?: () => void;
\t\t/** Redirect the module's stdout. */
\t\tprint?: (text: string) => void;
\t\t/** Redirect the module's stderr. */
\t\tprintErr?: (text: string) => void;
\t\t[key: string]: unknown;
\t}
'''


def main():
    pyi = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'src', 'rhino3dm', '__init__.pyi')
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'src', 'js', 'rhino3dm.d.ts')
    api = Api()
    py = Py(pyi)
    KNOWN.update(py.classes)
    KNOWN.update(py.enums)
    text, s = build(api, Js(), py)
    open(out, 'w').write(text)
    print('d.ts: %d JS members, %d typed from the Python typings, %d documented -> %s'
          % (s['members'], s['typed'], s['documented'], os.path.relpath(out, ROOT)))


if __name__ == '__main__':
    main()
