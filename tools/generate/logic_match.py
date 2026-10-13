"""Does a generated member do what an old (hand-bound) member does?

Evidence, not names: both sides end in opennurbs. The old binding calls it
directly (src/bindings: `bool IsVisible() const { return m_layer->IsVisible(); }`);
the generated binding calls a C function (src/generated: `ON_Layer_GetSetBool(p,
(LayerBool)0, false, false)`) whose body (src/librhino3dm_native) reaches
opennurbs only on the branch its constant arguments select (`idxIsVisible ==
which`, `set == false` -> `pLayer->IsVisible()`). This module extracts, per
member and per accessor (get / set / call), the opennurbs operations each side
performs -- member calls and field accesses -- with the native body specialized
for the constants the generated call passes, and compares them.

    match = LogicIndex(root)
    match.compare(old_class='Layer', old_member='Visible', gen_class='LayerX', gen_member='IsVisible')

Verdicts: 'same' (every accessor performs the same opennurbs operations),
'partial' (some accessors match, none conflict), or None. The operations are a
summary, not a proof: two bodies with the same calls can still differ in
argument handling, so 'same' is strong evidence for a reviewer, never a
decision.
"""

import glob
import os
import re

# ---- a tolerant C/C++ statement reader ------------------------------------

TOKEN = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d[\w.]*|->|::|&&|\|\||==|!=|<=|>=|[{}()\[\];,?:<>=!&|+\-*/%.^~]',
                   re.S)


def tokens(text):
    return [t for t in TOKEN.findall(text) if not t.startswith(('//', '/*'))]


def match_close(toks, i, open_='{', close='}'):
    """index of the token closing toks[i] (an `open_`)"""
    depth = 0
    for j in range(i, len(toks)):
        if toks[j] == open_:
            depth += 1
        elif toks[j] == close:
            depth -= 1
            if depth == 0:
                return j
    return len(toks) - 1


class Env:
    """what is known about a body's parameters: bool literals, enum names"""
    def __init__(self, known=None):
        self.known = known or {}      # param -> 'true' | 'false' | enumerator name | int literal

    def decide(self, cond):
        """True / False when the condition is decidable from known params, else None"""
        c = [t for t in cond if t not in ('(', ')')]
        # a || b ... / a && b ...: decide the pieces
        for op, absorb in (('||', True), ('&&', False)):
            if op in c:
                parts, cur = [], []
                for t in c:
                    if t == op:
                        parts.append(cur); cur = []
                    else:
                        cur.append(t)
                parts.append(cur)
                vals = [self.decide(p) for p in parts]
                if absorb in vals:
                    return absorb
                if all(v is (not absorb) for v in vals):
                    return not absorb
                return None
        if len(c) == 1 and c[0] in self.known and self.known[c[0]] in ('true', 'false'):
            return self.known[c[0]] == 'true'
        if len(c) == 2 and c[0] == '!' and c[1] in self.known and self.known[c[1]] in ('true', 'false'):
            return self.known[c[1]] != 'true'
        if len(c) == 3 and c[1] in ('==', '!='):
            a, b = c[0], c[2]
            if b in self.known:
                a, b = b, a
            if a in self.known:
                v = self.known[a]
                if b in ('true', 'false') or re.match(r'^\d+$', b) or re.match(r'^[A-Za-z_]\w*$', b):
                    eq = (v == b) or (v == self.enum_value_of(b))
                    return eq if c[1] == '==' else not eq
        return None

    def enum_value_of(self, name):
        return self.known.get('@' + name)


def reachable(toks, env):
    """the tokens of the statements that run, given what env knows"""
    out = []
    i = 0
    n = len(toks)
    while i < n:
        t = toks[i]
        if t == 'if' and i + 1 < n and toks[i + 1] == '(':
            ce = match_close(toks, i + 1, '(', ')')
            cond = toks[i + 2:ce]
            then_s, then_e = statement_span(toks, ce + 1)
            j = then_e + 1
            else_span = None
            if j < n and toks[j] == 'else':
                else_span = statement_span(toks, j + 1)
                j = else_span[1] + 1
            d = env.decide(cond)
            out += cond                                   # the condition itself runs
            if d is not False:
                out += reachable(toks[then_s:then_e + 1], env)
            if d is not True and else_span:
                out += reachable(toks[else_span[0]:else_span[1] + 1], env)
            i = j
            continue
        if t == 'switch' and i + 1 < n and toks[i + 1] == '(':
            ce = match_close(toks, i + 1, '(', ')')
            sel = [x for x in toks[i + 2:ce] if x not in ('(', ')')]
            bs = ce + 1
            be = match_close(toks, bs) if bs < n and toks[bs] == '{' else bs
            body = toks[bs + 1:be]
            known = env.known.get(sel[0]) if len(sel) == 1 else None
            out += switch_reachable(body, known, env)
            i = be + 1
            continue
        out.append(t)
        i += 1
    return out


def statement_span(toks, i):
    """(start, end) of the statement starting at toks[i]"""
    n = len(toks)
    if i >= n:
        return i, i
    if toks[i] == '{':
        return i, match_close(toks, i)
    if toks[i] in ('if', 'switch', 'for', 'while'):
        ce = match_close(toks, i + 1, '(', ')')
        s, e = statement_span(toks, ce + 1)
        if toks[i] == 'if' and e + 1 < n and toks[e + 1] == 'else':
            _, e = statement_span(toks, e + 2)
        return i, e
    j = i
    depth = 0
    while j < n:
        if toks[j] in ('(', '{', '['):
            depth += 1
        elif toks[j] in (')', '}', ']'):
            depth -= 1
        elif toks[j] == ';' and depth == 0:
            return i, j
        j += 1
    return i, n - 1


def switch_reachable(body, known, env):
    """a switch body: the case(s) the known selector reaches, else all of it"""
    if known is None:
        return reachable([t for t in body if t not in ('case', 'default')], env)
    cases = []          # (labels, start index)
    i = 0
    while i < len(body):
        if body[i] in ('case', 'default'):
            j = i
            while j < len(body) and body[j] != ':':
                j += 1
            label = [t for t in body[i + 1:j] if t not in ('::',)]
            cases.append((body[i], label[-1] if label else None, j + 1))
            i = j + 1
            continue
        if body[i] == '{':
            i = match_close(body, i) + 1
            continue
        i += 1
    start = None
    for kind, label, pos in cases:
        if kind == 'case' and (label == known or env.enum_value_of(label) == known):
            start = pos
            break
    if start is None:
        start = next((pos for kind, _, pos in cases if kind == 'default'), None)
    if start is None:
        return []
    out, depth, returning = [], 0, False
    for t in body[start:]:
        if returning:
            out.append(t)
            if t == ';' and depth == 0:
                break                                     # `return x;` ends the case
            continue
        if depth == 0 and t == 'break':
            break
        if depth == 0 and t == 'return':
            returning = True
            out.append(t)
            continue
        if t in ('case', 'default') and depth == 0:
            continue                                      # fall-through labels
        if t == '{':
            depth += 1
        elif t == '}':
            depth -= 1
        out.append(t)
    return reachable(out, env)


# ---- opennurbs operations of a body ---------------------------------------

# binding / interop plumbing on either side: never evidence of logic
NOISE = {
    'c_str', 'data', 'size', 'push_back', 'append', 'Array', 'Cast', 'cast', 'ptr', 'get', 'attr',
    'Append', 'SetCount', 'Count', 'Length', 'IsEmpty', 'Destroy', 'Empty', 'EmptyCount',
    'Duplicate', 'IsValid', 'ToString', 'Array_', 'm_ptr', 'm_val', 'GetONSubDComponent', 'SubD',
    'Last', 'First', 'Set', 'GetValue', 'SetValue',
    # ON_XMLVariant accessors (the RDK settings' value carrier)
    'AsBool', 'AsInteger', 'AsDouble', 'AsFloat', 'AsString', 'AsUuid', 'AsColor', 'As2dVector',
    'As3dVector', 'As2dPoint', 'As3dPoint', 'As4dPoint', 'AsTime', 'AsXform', 'AsBuffer', 'AsDoubleArray',
    'AsDoubleColor4', 'AsTransform', 'SetValue', 'IsEqual',
}


def ops_of(toks, cpp2py=None):
    """the opennurbs operations a token stream performs: `x->F(` / `x.F(`
    calls, `x->m_f` field accesses, `ON_X::F(` / `ON_X::k` statics -- and, given
    cpp2py, the binding views it constructs (`new BND_File3dmGroupTable(...)`
    as `new:File3dmGroupTable`)"""
    out = set()
    if 'for' in toks or 'while' in toks:
        out.add('(loop)')        # a loop is logic: ToList over Knot() is not this[i] over Knot()
    for i, t in enumerate(toks):
        if cpp2py and t == 'new' and i + 1 < len(toks) and toks[i + 1] in cpp2py:
            out.add('new:' + cpp2py[toks[i + 1]])
            continue
        if cpp2py and t in cpp2py and i + 1 < len(toks) and toks[i + 1] == '(' \
                and i > 0 and toks[i - 1] in ('return', '=', '(', ','):
            out.add('new:' + cpp2py[t])                   # `return BND_File3dmGroupTable(m_model);`
            continue
        if t in ('->', '.') and i + 1 < len(toks) and re.match(r'^[A-Za-z_]\w*$', toks[i + 1]):
            name = toks[i + 1]
            if name in NOISE:
                continue
            is_call = i + 2 < len(toks) and toks[i + 2] == '('
            if is_call:
                out.add(name + '()')
            elif name.startswith('m_'):
                out.add(name)
        elif t == '::' and i > 0 and toks[i - 1].startswith('ON_') and i + 1 < len(toks) \
                and toks[i - 1] not in ('ON_wString', 'ON_String') and re.match(r'^\w+$', toks[i + 1]):
            if i + 2 < len(toks) and toks[i + 2] == '(':
                out.add('%s::%s()' % (toks[i - 1], toks[i + 1]))   # ON_Brep::New(...) and other statics
            elif toks[i + 1][0].isupper() and (i + 2 >= len(toks) or toks[i + 2] != '::'):
                out.add('%s::%s' % (toks[i - 1], toks[i + 1]))     # ON_Linetype::Border
    return out


# ---- the two code bases ---------------------------------------------------

def statement_end(text, i):
    """index of the `;` ending the statement that starts at text[i] (brackets,
    strings and comments respected: a lambda's own `;` does not end it)"""
    depth, j, n = 0, i, len(text)
    while j < n:
        ch = text[j]
        if ch in '"\'':
            q = ch; j += 1
            while j < n and text[j] != q:
                j += 2 if text[j] == '\\' else 1
        elif text.startswith('//', j):
            j = text.find('\n', j)
            j = n if j < 0 else j
        elif text.startswith('/*', j):
            j = text.find('*/', j)
            j = n if j < 0 else j + 1
        elif ch in '({[':
            depth += 1
        elif ch in ')}]':
            depth -= 1
        elif ch == ';' and depth <= 0:
            return j
        j += 1
    return n


def function_body(text, header_re):
    """the tokens of the body of the first definition matching header_re"""
    m = re.search(header_re, text)
    if not m:
        return None
    k = text.find('{', m.end() - 1)
    if k < 0:
        return None
    # header up to the brace must not end the declaration first
    if ';' in text[m.end():k]:
        return None
    toks = tokens(text[k:k + 20000])
    return toks[1:match_close(toks, 0)]


def expand_macros(srcs):
    """expand the bindings' function-like macros (`DIMSTYLE_PROPERTY(bool, X)` ->
    `bool GetX() const { return m_dimstyle->X(); } ...`), `##` pasting included"""
    macros = {}
    for text in srcs.values():
        for m in re.finditer(r'^[ \t]*#define[ \t]+(\w+)\(([^)]*)\)[ \t]*((?:[^\n]*\\\n)*[^\n]*)', text, re.M):
            macros[m.group(1)] = ([p.strip() for p in m.group(2).split(',')], m.group(3).replace('\\\n', '\n'))
    if not macros:
        return srcs
    def expand(text):
        for name, (params, body) in macros.items():
            def sub(mm):
                args = [a.strip() for a in mm.group(1).split(',')]
                if len(args) != len(params):
                    return mm.group(0)
                out = body
                for p_, a in zip(params, args):
                    out = re.sub(r'\s*##\s*%s\b' % re.escape(p_), a, out)
                    out = re.sub(r'\b%s\s*##\s*' % re.escape(p_), a, out)
                    out = re.sub(r'\b%s\b' % re.escape(p_), a, out)
                return out
            text = re.sub(r'(?<!define )\b%s\(([^()]*)\)' % re.escape(name), sub, text)
        return text
    return {f: expand(t) for f, t in srcs.items()}


class LogicIndex:
    def __init__(self, root):
        self.root = root
        self.bind_src = expand_macros({f: open(f, errors='replace').read()
                                       for f in glob.glob(os.path.join(root, 'src', 'bindings', 'bnd_*.*'))})
        self.gen_src = {f: open(f, errors='replace').read()
                        for f in glob.glob(os.path.join(root, 'src', 'generated', 'gen_*.cpp'))
                        + glob.glob(os.path.join(root, 'src', 'generated', 'gen_*.h'))}
        self.native_src = '\n'.join(open(f, errors='replace').read()
                                    for f in sorted(glob.glob(os.path.join(root, 'src', 'librhino3dm_native', '*.cpp'))
                                                    + glob.glob(os.path.join(root, 'src', 'librhino3dm_native', '*.h'))))
        self.enums = self._enums()
        self.old = self._registrations(self.bind_src, gen=False)
        self.gen = self._registrations(self.gen_src, gen=True)
        # binding class -> python name, both sides in the OLD names (FooX -> Foo)
        self.old_cpp2py = dict(self._cpp2py)
        self._cpp2py = {}
        self._registrations(self.gen_src, gen=True)
        self.gen_cpp2py = {c: (p[:-1] if p.endswith('X') else p) for c, p in self._cpp2py.items()}
        self._cache = {}

    # enumerator name <-> value, from the native layer's enum definitions
    def _enums(self):
        out = {}
        # opennurbs' nested enums (`ON_DimStyle::field`), from the headers
        for f in glob.glob(os.path.join(self.root, 'src', 'lib', 'opennurbs', 'opennurbs*.h')):
            text = open(f, errors='replace').read()
            for cm in re.finditer(r'\bclass\s+(?:ON_CLASS\s+|ON_DECL\s+)?(ON_\w+)\b[^;{]*\{', text):
                ctoks_start = cm.end() - 1
                body = text[ctoks_start:ctoks_start + 400000]
                depth, end = 0, len(body)
                for i, ch in enumerate(body):
                    if ch == '{':
                        depth += 1
                    elif ch == '}':
                        depth -= 1
                        if depth == 0:
                            end = i
                            break
                for em in re.finditer(r'\benum\s+(?:class\s+)?(\w+)\s*(?::\s*[\w\s]+?)?\s*\{([^}]*)\}', body[:end]):
                    out.setdefault('%s::%s' % (cm.group(1), em.group(1)), self._enum_values(em.group(2)))
        for m in re.finditer(r'\benum\s+(?:class\s+)?(\w+)\s*(?::\s*[\w\s]+?)?\s*\{([^}]*)\}', self.native_src):
            out.setdefault(m.group(1), self._enum_values(m.group(2)))
        return out

    @staticmethod
    def _enum_values(text):
        text = re.sub(r'//[^\n]*|/\*.*?\*/', '', text, flags=re.S)
        nxt, vals = 0, {}
        for part in text.split(','):
            part = part.strip()
            if not part:
                continue
            nm, _, v = part.partition('=')
            nm, v = nm.strip(), v.strip()
            if not re.match(r'^\w+$', nm):
                continue
            try:
                val = int(v.rstrip('uU'), 0) if v else nxt
            except ValueError:
                val = vals.get(v, nxt)
            vals[nm] = val
            nxt = val + 1
        return vals

    # python class name -> {member name: [(accessor, C++ class, method or lambda tokens)]}
    _cpp2py = {}

    def _registrations(self, srcs, gen):
        out = {}
        for f, text in srcs.items():
            blocks = []
            for m in re.finditer(r'py::class_<\s*(\w+)[^>]*>\s*\(\s*m\s*,\s*"(\w+)"', text):
                blocks.append((m.group(1), m.group(2), text[m.end():statement_end(text, m.end())]))
            # `py::class_<BND_X, ...> pyX(m, "X"); pyX.def(...)...;` -- every `pyX.` statement
            for m in re.finditer(r'py::class_<\s*(\w+)[^>]*>\s*(\w+)\s*\(\s*m\s*,\s*"(\w+)"', text):
                var = m.group(2)
                stmts = [text[s_.end():statement_end(text, s_.end())] for s_ in re.finditer(r'\b%s\s*\.' % re.escape(var), text)]
                blocks.append((m.group(1), m.group(3), '\n.'.join([''] + stmts)))
            for cpp, pyname, block in blocks:
                self._cpp2py[cpp] = pyname
                mems = out.setdefault(pyname, {})
                for d in re.finditer(r'\.(def_property_readonly_static|def_property_readonly|def_property_static|def_property|def_static|def)\(\s*"(\w+)"\s*,', block):
                    # the def's own arguments (up to its closing paren)
                    depth, j = 1, d.end()
                    while j < len(block) and depth:
                        depth += {'(': 1, ')': -1}.get(block[j], 0)
                        j += 1
                    args = block[d.end():j - 1]
                    kinds = ['get', 'set'] if d.group(1) in ('def_property', 'def_property_static') else \
                            ['get'] if d.group(1).startswith('def_property_readonly') else ['call']
                    # targets in order: &Class::Method, &FreeFunction, or a lambda body
                    targets = []
                    for t in re.finditer(r'&(\w+)::(\w+)|&(\w+)\b(?!::)|\[[^\]]*\]\s*\([^)]*\)\s*(?:->\s*[\w:<>\*& ]+)?\{', args):
                        if t.group(1):
                            targets.append((t.group(1), t.group(2)))
                        elif t.group(3):
                            targets.append((None, t.group(3)))
                        else:
                            k = t.end() - 1
                            ltoks = tokens(args[k:])
                            targets.append(('<lambda>', ltoks[1:match_close(ltoks, 0)]))
                    for acc, (c, meth) in zip(kinds, targets):
                        mems.setdefault(d.group(2), []).append((acc, c, meth))
        return out

    def _method_tokens(self, srcs, cpp, meth):
        """bodies of every overload of cpp::meth (out-of-line or inline in the class);
        a lambda's tokens as given; a free function's definition"""
        if cpp == '<lambda>':
            return [meth]
        bodies = []
        if cpp is None:
            for text in srcs.values():
                for m in re.finditer(r'(?m)^[\w:<>\*&\s]*?\b%s\s*\([^;{]*\)\s*\{' % re.escape(meth), text):
                    toks = tokens(text[m.end() - 1:m.end() - 1 + 20000])
                    bodies.append(toks[1:match_close(toks, 0)])
            return bodies
        for f, text in srcs.items():
            for m in re.finditer(r'\b%s::%s\s*\(' % (re.escape(cpp), re.escape(meth)), text):
                b = function_body(text[m.start():], r'^%s::%s\s*\([^;{]*\)[^;{]*' % (re.escape(cpp), re.escape(meth)))
                if b is not None:
                    bodies.append(b)
            cm = re.search(r'\bclass\s+%s\b[^;{]*\{' % re.escape(cpp), text)
            if cm:
                ctoks = tokens(text[cm.end() - 1:cm.end() - 1 + 200000])
                ce = match_close(ctoks, 0)
                ctoks = ctoks[1:ce]
                for i in range(len(ctoks) - 1):
                    if ctoks[i] == meth and ctoks[i + 1] == '(' and (i == 0 or ctoks[i - 1] not in ('.', '->', '::', 'new', 'return', '=', ',', '(')):
                        pe = match_close(ctoks, i + 1, '(', ')')
                        j = pe + 1
                        while j < len(ctoks) and ctoks[j] in ('const', 'override', 'noexcept'):
                            j += 1
                        if j < len(ctoks) and ctoks[j] == '{':
                            bodies.append(ctoks[j + 1:match_close(ctoks, j)])
        return bodies

    def _native(self, name, consts):
        """opennurbs ops of native function `name`, specialized for consts
        {param index: literal or (enum type, int value)}"""
        key = (name, tuple(sorted(consts.items())))
        if key in self._cache:
            return self._cache[key]
        m = re.search(r'RH_C_FUNCTION\s+[\w\s\*&<>:,]*?\b%s\s*\(([^)]*)\)\s*\{' % re.escape(name), self.native_src)
        if not m:
            self._cache[key] = None
            return None
        params = [p.strip() for p in m.group(1).split(',')]
        pnames = [re.findall(r'(\w+)\s*(?:\[\])?$', p)[0] if re.findall(r'(\w+)\s*(?:\[\])?$', p) else '' for p in params]
        toks = tokens(self.native_src[m.end() - 1:m.end() - 1 + 60000])
        body = toks[1:match_close(toks, 0)]
        env = Env()
        for idx, v in consts.items():
            if idx >= len(pnames):
                continue
            if isinstance(v, tuple):
                # an enum selector resolves only through its definition: unknown
                # otherwise (never a bare number -- every branch would read false)
                etype, ival = v
                names = {val: nm for nm, val in self.enums.get(etype, {}).items()}
                if ival not in names:
                    continue
                env.known[pnames[idx]] = names[ival]
                for nm, val in self.enums[etype].items():
                    env.known['@' + nm] = names.get(val)
            else:
                env.known[pnames[idx]] = v
        out = ops_of(reachable(body, env))
        self._cache[key] = out
        return out

    def gen_ops(self, gen_class, member):
        """{accessor: ops} of a generated member (through its native calls)"""
        if ('g', gen_class, member) in self._cache:
            return self._cache[('g', gen_class, member)]
        out = self._cache[('g', gen_class, member)] = {}
        for acc, cpp, meth in self.gen.get(gen_class, {}).get(member, []):
            ops = set()
            for b in self._method_tokens(self.gen_src, cpp, meth):
                ops |= self._gen_body_ops(b, cpp, depth=0)
            out[acc] = ops
        return out

    def _gen_body_ops(self, b, cpp, depth):
        ops = {o for o in ops_of(b, self.gen_cpp2py) if o.startswith('new:') or o == '(loop)'}
        for i, t in enumerate(b):
            if re.match(r'^(ON_|ONX_|Rdk_|Draco_)\w+$', t) and i + 1 < len(b) and b[i + 1] == '(' \
                    and (i == 0 or b[i - 1] not in ('::', '.', '->')):
                ce = match_close(b, i + 1, '(', ')')
                args, cur, depth_ = [], [], 0
                for x in b[i + 2:ce]:
                    if x in ('(', '[') or x == '{':
                        depth_ += 1
                    elif x in (')', ']') or x == '}':
                        depth_ -= 1
                    if x == ',' and depth_ == 0:
                        args.append(cur); cur = []
                    else:
                        cur.append(x)
                args.append(cur)
                consts = {}
                for k, a in enumerate(args):
                    if len(a) == 1 and a[0] in ('true', 'false'):
                        consts[k] = a[0]
                    elif len(a) == 1 and re.match(r'^\d+$', a[0]):
                        consts[k] = a[0]
                    elif len(a) >= 4 and a[0] == '(' and a[-2] == ')' and re.match(r'^\d+$', a[-1]) \
                            and all(re.match(r'^\w+$', x) or x == '::' for x in a[1:-2]):
                        consts[k] = (''.join(a[1:-2]), int(a[-1]))   # (LayerBool)0, (ON_DimStyle::field)9
                nat = self._native(t, consts)
                if nat:
                    ops |= nat
            elif depth < 2 and t.startswith('Gen_') and i + 1 < len(b) and b[i + 1] == '(':
                # a forward to a sibling generated member
                for bb in self._method_tokens(self.gen_src, cpp, t):
                    ops |= self._gen_body_ops(bb, cpp, depth + 1)
        return ops

    def old_ops(self, old_class, member):
        if ('o', old_class, member) in self._cache:
            return self._cache[('o', old_class, member)]
        out = self._cache[('o', old_class, member)] = {}
        for acc, cpp, meth in self.old.get(old_class, {}).get(member, []):
            ops = set()
            for b in self._method_tokens(self.bind_src, cpp, meth):
                ops |= ops_of(b, self.old_cpp2py)
            out[acc] = ops
        return out

    def compare(self, old_class, old_member, gen_class, gen_member):
        """(verdict, per-accessor evidence). Per accessor (get / set / call):
        'same' (the same opennurbs operations; a setter may also read first, as
        RhinoCommon's IsValueEqual does), 'more' (the generated one does everything
        the old one does, plus the listed extras), 'missing' (no generated
        counterpart accessor), 'differs' -- 'more' only for set/call: a getter
        that reads more computes something else. Verdict: 'same' when every accessor is
        same; 'more' when none differs and one does more; 'partial' when the rest
        are missing; None when any differs (other logic)."""
        o, g = self.old_ops(old_class, old_member), self.gen_ops(gen_class, gen_member)
        if not o or not g or not any(o.values()):
            return None, {}
        ev = {}
        for acc, ops in o.items():
            if not ops:
                continue
            gops = g.get(acc)
            if gops is None:
                ev[acc] = ('missing', ops, set())
                continue
            reads = {r for w in ops if w.startswith('Set') for r in (w[3:], 'Is' + w[3:], 'Get' + w[3:])}
            extra = gops - ops
            if gops == ops or (acc == 'set' and ops <= gops and extra <= reads):
                ev[acc] = ('same', ops, gops)
            elif ops <= gops and gops and acc != 'get':
                # a write that does more (marks an override, checks a bound): worth
                # showing. A getter that reads more returns something else: differs.
                ev[acc] = ('more', ops, gops)
            else:
                ev[acc] = ('differs', ops, gops)
        kinds = {k for k, _, _ in ev.values()}
        if not ev or 'differs' in kinds or not kinds & {'same', 'more'}:
            return None, ev
        if kinds == {'same'}:
            return 'same', ev
        if 'more' in kinds:
            return 'more', ev
        return 'partial', ev

    def best_match(self, old_class, old_member, gen_classes):
        """the generated members (of gen_classes, nearest first) doing the same:
        [(verdict, gen class, gen member, evidence)] -- the best verdict found"""
        hits = []
        for gc in gen_classes:
            for gm in self.gen.get(gc, {}):
                v, ev = self.compare(old_class, old_member, gc, gm)
                if v:
                    hits.append((v, gc, gm, ev))
        for want in ('same', 'more', 'partial'):
            got = [h for h in hits if h[0] == want]
            if got:
                return got
        return []


def describe(verdict, gc, gm, ev):
    """one table cell of evidence"""
    def ops(x):
        return ', '.join('`%s`' % o for o in sorted(x))
    if verdict == 'same':
        return '**same logic** as `%s.%s` (both: %s)' % (gc, gm, ' / '.join(ops(e[1]) for e in ev.values()))
    parts = []
    for acc, (k, o, g) in ev.items():
        if k == 'same':
            parts.append('%s same (%s)' % (acc, ops(o)))
        elif k == 'more':
            parts.append('%s: generated also calls %s' % (acc, ops(g - o)))
        elif k == 'missing':
            parts.append('%s: no generated counterpart' % acc)
    return '**%s** `%s.%s`: %s' % ('close' if verdict == 'more' else 'partial', gc, gm, '; '.join(parts))
