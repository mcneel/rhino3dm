"""Enumerate RH_C_FUNCTION exports from a rhinocommon_c checkout.

Produces the raw inventory that the API manifest is built from: every exported
C entry point, its signature text, source location, and whether it survives a
given set of preprocessor defines.
"""

import os
import re
import glob

from cond import evaluate

# Declarations may be preceded by /*MANUAL*/, which opts them out of
# generation (methodgen honours the same marker).
DECL = re.compile(r'(?P<manual>/\*MANUAL\*/)?[ \t]*RH_C_FUNCTION[ \t]+')
# /*ARRAY*/ distinguishes `T[]` from `ref T` and is the only thing that does,
# so it must survive comment stripping rather than be discarded with it.
ARRAY_MARKER = '/*ARRAY*/'
MANUAL_MARKER = '/*MANUAL*/'
# Markers that carry meaning and must survive comment removal.
KEPT_MARKERS = (ARRAY_MARKER, MANUAL_MARKER)
# Callback parameter types follow a *PROC naming convention. A function
# pointer cannot be expressed as a plain P/Invoke or binding signature, so
# these are recorded and excluded rather than generated (methodgen applies
# the same rule).
FN_POINTER = re.compile(r'\b[A-Za-z_][A-Za-z0-9_]*PROC\b')


class Decl:
    def __init__(self, path, line, text, manual, portable):
        self.path = path
        self.line = line
        self.text = text
        self.manual = manual
        self.portable = portable
        self.name = None
        self.ret = None
        self.params = None
        self._split()
        self.callback = bool(self.params and FN_POINTER.search(self.params))

    @property
    def generatable(self):
        """Whether a binding/P-Invoke can be emitted for this export.

        Excluded: exports guarded out of the portable build, /*MANUAL*/
        opt-outs, and anything taking a function pointer. Each exclusion is
        retained as a flag so it can be reported rather than vanishing.
        """
        return (self.portable and self.name and not self.manual
                and not self.callback)

    def _split(self):
        body = self.text[self.text.index('RH_C_FUNCTION') + len('RH_C_FUNCTION'):]
        open_paren = body.find('(')
        if open_paren < 0 or ')' not in body:
            return
        head = body[:open_paren].strip()
        # Depth-match the closing paren: rindex(')') grabbed the LAST paren,
        # which for a one-line DEFINITION is the body's, so 38 functions had
        # their bodies recorded as parameters (caught by verify_types.py).
        depth = 0
        close_paren = -1
        for i in range(open_paren, len(body)):
            if body[i] == '(':
                depth += 1
            elif body[i] == ')':
                depth -= 1
                if depth == 0:
                    close_paren = i
                    break
        if close_paren < 0:
            return
        self.params = body[open_paren + 1:close_paren].strip()
        parts = head.split()
        if parts:
            self.name = parts[-1].lstrip('*&')
            self.ret = ' '.join(parts[:-1]) + ('*' if parts[-1].startswith('*') else '')
            self.ret = self.ret.strip()

    def __repr__(self):
        return '%s (%s:%d)' % (self.name, os.path.basename(self.path), self.line)


def strip_comments(text):
    """Blank out comments while preserving every newline and /*ARRAY*/.

    Line numbers must survive because declarations are reported by source
    location, and /*ARRAY*/ must survive because it is the only thing
    distinguishing an array parameter from a by-reference one.

    Done as a single pass over the whole file rather than per-declaration:
    a `//` comment inside a multi-line declaration would otherwise swallow
    the rest of the joined text, including the closing parenthesis.
    """
    out = []
    i = 0
    n = len(text)
    while i < n:
        ch = text[i]
        marker = next((m for m in KEPT_MARKERS if text.startswith(m, i)), None)
        if marker:
            out.append(marker)
            i += len(marker)
        elif text.startswith('/*', i):
            end = text.find('*/', i + 2)
            end = n if end < 0 else end + 2
            # Keep newlines so following declarations keep their line numbers.
            out.append(''.join(c if c == '\n' else ' ' for c in text[i:end]))
            i = end
        elif text.startswith('//', i):
            end = text.find('\n', i)
            end = n if end < 0 else end
            out.append(' ' * (end - i))
            i = end
        elif ch in '"\'':
            # Skip string/char literals so a // or /* inside one is not eaten.
            quote = ch
            j = i + 1
            while j < n and text[j] != quote:
                j += 2 if text[j] == '\\' else 1
            j = min(j + 1, n)
            out.append(text[i:j])
            i = j
        else:
            out.append(ch)
            i += 1
    return ''.join(out)


def scan_file(path, defines):
    """Yield Decl objects for every RH_C_FUNCTION in `path`."""
    with open(path, encoding='utf-8', errors='replace') as handle:
        raw = handle.read()

    keep = evaluate(raw, defines)
    # Comments are removed before scanning so that declarations spanning
    # several lines survive intact; line numbering is preserved.
    lines = strip_comments(raw).splitlines()

    out = []
    index = 0
    while index < len(lines):
        line = lines[index]
        # A commented-out declaration is not an export.
        stripped = line.lstrip()
        if stripped.startswith('//'):
            index += 1
            continue
        # The macro's own definition is not an export.
        if stripped.startswith('#'):
            index += 1
            continue
        match = DECL.search(line)
        if not match:
            index += 1
            continue

        # Declarations wrap across lines (665 of them repo-wide), so accumulate
        # until parentheses balance rather than assuming one line.
        chunk = line[match.start():]
        depth = chunk.count('(') - chunk.count(')')
        last = index
        while depth > 0 and last + 1 < len(lines):
            last += 1
            chunk += ' ' + lines[last].strip()
            depth += lines[last].count('(') - lines[last].count(')')

        text = re.sub(r'\s+', ' ', chunk).strip()
        out.append(Decl(path, index + 1, text,
                        manual=bool(match.group('manual')),
                        portable=keep[index]))
        index = last + 1
    return out


def scan_dir(directory, defines, pattern='*.cpp'):
    decls = []
    for path in sorted(glob.glob(os.path.join(directory, pattern))):
        decls.extend(scan_file(path, defines))
    return decls
