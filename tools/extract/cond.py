"""Preprocessor conditional evaluation for the rhino3dm binding extractor.

Determines which lines of a C/C++ translation unit survive a known set of
-D defines, handling nested #if/#elif/#else/#endif correctly.

Why not run a real preprocessor: portability verdicts are needed on Windows,
macOS and Linux with byte-identical results, and `cc -E` would require
resolving the full opennurbs include graph. The conditional vocabulary in
rhinocommon_c is small enough (214 directives across on_*.cpp, expressions
limited to defined()/!/&&/||) that exact evaluation is tractable here.

This is deliberately NOT a general preprocessor: it does no macro expansion
and no #include following. Any condition it cannot evaluate exactly is
reported rather than guessed at -- see UnknownCondition.
"""

import re

DIRECTIVE = re.compile(r'^[ \t]*#[ \t]*(if|ifdef|ifndef|elif|else|endif)\b[ \t]*(.*)$')


class UnknownCondition(Exception):
    """A condition used syntax this evaluator does not implement exactly.

    Raised rather than defaulting, so an unsupported construct surfaces as a
    hard failure instead of silently mis-classifying an export.
    """


class _Expr:
    """Recursive-descent evaluator for C preprocessor conditional expressions.

    Supports defined(), !, &&, ||, parentheses, integer literals and the
    comparison/equality operators. Values are integers, following C: an
    identifier that survives macro expansion is replaced by 0, so an
    undefined macro compares as zero rather than raising.

    Deliberately NOT supported: arithmetic, bitwise ops, and expansion of
    object-like macros defined in headers. Those raise UnknownCondition so
    an unsupported construct fails loudly instead of silently mis-classifying
    an export.
    """

    _TOKEN = re.compile(
        r'\s*(<<|>>|<=|>=|==|!=|\|\||&&|[!()<>]|'
        r'0[xX][0-9a-fA-F]+[uUlL]*|\d+[uUlL]*|[A-Za-z_][A-Za-z0-9_]*)')

    _COMPARE = {
        '<': lambda a, b: a < b,
        '>': lambda a, b: a > b,
        '<=': lambda a, b: a <= b,
        '>=': lambda a, b: a >= b,
    }
    _EQUALITY = {
        '==': lambda a, b: a == b,
        '!=': lambda a, b: a != b,
    }

    def __init__(self, text, defines, bare_is_defined=False):
        # C# conditional compilation has no defined(); a bare identifier IS
        # the membership test. In C, a bare identifier is macro-expanded and
        # an undefined one becomes 0. The two need different handling.
        self.defines = defines
        self.bare_is_defined = bare_is_defined
        self.tokens = []
        pos = 0
        while pos < len(text):
            m = self._TOKEN.match(text, pos)
            if not m:
                if text[pos:].strip():
                    raise UnknownCondition(text)
                break
            self.tokens.append(m.group(1))
            pos = m.end()
        self.i = 0

    def _peek(self):
        return self.tokens[self.i] if self.i < len(self.tokens) else None

    def _next(self):
        tok = self._peek()
        self.i += 1
        return tok

    def parse(self):
        value = self._or()
        if self._peek() is not None:
            raise UnknownCondition(' '.join(self.tokens))
        return bool(value)

    def _or(self):
        value = self._and()
        while self._peek() == '||':
            self._next()
            value = int(bool(self._and()) or bool(value))
        return value

    def _and(self):
        value = self._equality()
        while self._peek() == '&&':
            self._next()
            value = int(bool(self._equality()) and bool(value))
        return value

    def _equality(self):
        value = self._comparison()
        while self._peek() in self._EQUALITY:
            op = self._next()
            value = int(self._EQUALITY[op](value, self._comparison()))
        return value

    def _comparison(self):
        value = self._unary()
        while self._peek() in self._COMPARE:
            op = self._next()
            value = int(self._COMPARE[op](value, self._unary()))
        return value

    def _unary(self):
        tok = self._peek()
        if tok == '!':
            self._next()
            return int(not self._unary())
        if tok == '(':
            self._next()
            value = self._or()
            if self._next() != ')':
                raise UnknownCondition('unbalanced parentheses')
            return value
        tok = self._next()
        if tok is None:
            raise UnknownCondition('truncated expression')
        if tok == 'defined':
            # Both `defined(X)` and the paren-less `defined X` occur here.
            if self._peek() == '(':
                self._next()
                name = self._next()
                if self._next() != ')':
                    raise UnknownCondition('malformed defined()')
            else:
                name = self._next()
            return int(name in self.defines)
        literal = re.fullmatch(r'(0[xX][0-9a-fA-F]+|\d+)[uUlL]*', tok)
        if literal:
            return int(literal.group(1), 0)
        if self.bare_is_defined:
            return int(tok in self.defines)
        if tok in self.defines:
            raise UnknownCondition(
                'bare use of defined macro %r would need expansion' % tok)
        return 0


def evaluate(text, defines, bare_is_defined=False):
    """Return a list of bools, one per line: True if that line is compiled in.

    Directive lines themselves are marked False -- callers want code lines.
    Pass bare_is_defined=True for C# sources, where `#if FOO` tests whether
    FOO is defined rather than expanding it.
    """
    defines = set(defines)
    active = []           # per-nesting-level: is this branch live?
    taken = []            # per-nesting-level: has any branch been taken yet?
    out = []

    def live():
        return all(active)

    for raw in text.splitlines():
        m = DIRECTIVE.match(raw)
        if not m:
            out.append(live())
            continue

        kind, rest = m.group(1), m.group(2)
        # Strip trailing comments; they carry no meaning in a condition.
        rest = re.sub(r'/\*.*?\*/', ' ', rest)
        rest = re.sub(r'//.*$', '', rest).strip()

        if kind in ('if', 'ifdef', 'ifndef'):
            if kind == 'ifdef':
                value = rest.split()[0] in defines if rest else False
            elif kind == 'ifndef':
                value = rest.split()[0] not in defines if rest else False
            else:
                value = _Expr(rest, defines, bare_is_defined).parse()
            # An inactive parent makes the whole subtree inactive, but the
            # nesting level must still be tracked so #endif pairs correctly.
            parent_live = live()
            active.append(value and parent_live)
            taken.append(value and parent_live)
        elif kind == 'elif':
            if not active:
                raise UnknownCondition('#elif without #if')
            parent_live = all(active[:-1])
            value = _Expr(rest, defines, bare_is_defined).parse()
            active[-1] = value and parent_live and not taken[-1]
            taken[-1] = taken[-1] or active[-1]
        elif kind == 'else':
            if not active:
                raise UnknownCondition('#else without #if')
            parent_live = all(active[:-1])
            active[-1] = parent_live and not taken[-1]
            taken[-1] = True
        elif kind == 'endif':
            if not active:
                raise UnknownCondition('#endif without #if')
            active.pop()
            taken.pop()

        out.append(False)

    if active:
        raise UnknownCondition('%d unterminated #if block(s)' % len(active))
    return out
