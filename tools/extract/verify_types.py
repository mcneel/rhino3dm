"""Cross-check the manifest's parameter parsing against libclang.

split_params() in manifest.py is a careful comma/depth splitter, not a parser.
Before a generator consumes its output, prove it against a real C++ parser:
every RH_C_FUNCTION declaration is parsed with the pinned libclang wheel
(see Current_Development_Tools.md; bootstrap.py -p generator provisions it)
and the two views of (name, return type, parameter types and names) are
compared. A mismatch is a scanner bug caught before codegen bakes it in.

Declarations are parsed standalone against a synthesized preamble of forward
declarations -- legal because C++ permits incomplete types in a function
DECLARATION's parameters and return. No opennurbs headers are involved, so the
check is fast and hermetic.

    python3 tools/extract/verify_types.py [path-to-rhino-checkout]

Exit 1 on any mismatch, listing each.
"""

import os
import re
import sys

from scan import scan_dir, ARRAY_MARKER
from manifest import split_params, PORTABLE_DEFINES, C_SUBDIR

try:
    import clang.cindex as ci
except ImportError:
    raise SystemExit('clang.cindex missing -- run: python3 script/bootstrap.py -p generator')

BUILTIN = {
    'void', 'bool', 'char', 'short', 'int', 'long', 'float', 'double',
    'signed', 'unsigned', 'const', 'struct', 'class', 'enum', 'wchar_t',
    'size_t', 'true', 'false', 'nullptr', 'NULL',
}
IDENT = re.compile(r'\b[A-Za-z_][A-Za-z0-9_]*\b')
TEMPLATED = re.compile(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*<')


def normalize(t):
    """One spelling for a type: collapse spaces, '*'/'&' attached left,
    elaboration keywords (enum/struct/class X) dropped on both sides."""
    t = re.sub(r'\b(enum|struct|class)\s+', '', t)
    t = re.sub(r'\s+', ' ', t).strip()
    t = re.sub(r'\s*([*&])', r'\1', t)
    # clang spells 'const T' and sources sometimes 'T const'; unify the common case
    t = re.sub(r'^([A-Za-z_][\w:<>, ]*?) const\b', r'const \1', t)
    return t


def declaration_only(text):
    """The decl text minus markers and default values (identifiers in default
    expressions would need declaring; types are what is being verified)."""
    body = text[text.index('RH_C_FUNCTION') + len('RH_C_FUNCTION'):].strip()
    body = body.replace(ARRAY_MARKER, ' ')
    # 38 exports are one-line DEFINITIONS; cut at the param list's closing
    # paren so function bodies never reach the parse or the preamble.
    start = body.find('(')
    if start >= 0:
        depth = 0
        for i in range(start, len(body)):
            if body[i] == '(':
                depth += 1
            elif body[i] == ')':
                depth -= 1
                if depth == 0:
                    body = body[:i + 1]
                    break
    out, depth, i = [], 0, 0
    while i < len(body):
        ch = body[i]
        if ch in '<(':
            depth += 1
        elif ch in '>)':
            depth -= 1
        elif ch == '=' and depth == 1:        # a default value inside the param list
            j = i
            d = depth
            while j < len(body):
                cj = body[j]
                if cj in '<(':
                    d += 1
                elif cj in '>)':
                    d -= 1
                    if d == 0:
                        break
                elif cj == ',' and d == 1:
                    break
                j += 1
            i = j
            continue
        out.append(ch)
        i += 1
    return ''.join(out)


QUALIFIED = re.compile(r'\b[A-Za-z_]\w*(?:::[A-Za-z_]\w*)+')
ELAB_ENUM = re.compile(r'\benum\s+([A-Za-z_]\w*)')


def build_preamble(decls):
    names, templates, enums = set(), set(), set()
    nested = {}   # outer -> set of nested type names (ON_Font::Weight etc.)
    for d in decls:
        text = declaration_only(d.text)
        templates.update(TEMPLATED.findall(text))
        enums.update(ELAB_ENUM.findall(text))
        for chain in QUALIFIED.findall(text):
            parts = chain.split('::')
            node = nested.setdefault(parts[0], {})
            for part in parts[1:]:
                node = node.setdefault(part, {})
        names.update(IDENT.findall(text))
        names.discard(d.name)
    # a name used with :: needs a DEFINITION carrying using-aliases: a mere
    # forward declaration makes every Outer::Inner a hard error and clang
    # drops the whole function (3,427 of them, before this)
    def all_segments(tree):
        for k, v in tree.items():
            yield k
            yield from all_segments(v)
    names -= BUILTIN | templates | enums | set(all_segments(nested))
    lines = []
    # std:: is a real namespace of templates, not a struct of aliases: a
    # `struct std` poisons every later std::vector<...> in the file.
    std_members = nested.pop('std', {})
    if std_members:
        lines.append('namespace std { %s }' % ' '.join(
            'template<class T> class %s;' % m for m in sorted(std_members)))
        templates -= set(std_members)

    def emit(name, children):
        if not children:
            return 'using %s = int;' % name
        return 'struct %s { %s };' % (
            name, ' '.join(emit(k, v) for k, v in sorted(children.items())))

    lines += ['template<class T> class %s;' % t for t in sorted(templates)]
    lines += ['enum %s : int;' % e for e in sorted(enums)]
    lines += [emit(outer, children) for outer, children in sorted(nested.items())]
    lines += ['class %s;' % n for n in sorted(names)
              if n not in ('operator', 'unsigned')]
    return '\n'.join(lines) + '\n'


def main(argv):
    rhino_root = os.path.expanduser(argv[1] if len(argv) > 1 else '~/dev/rhino')
    cdir = os.path.join(rhino_root, C_SUBDIR)

    decls = (scan_dir(cdir, PORTABLE_DEFINES, '*.cpp')
             + scan_dir(cdir, PORTABLE_DEFINES, '*.h'))
    by_name = {}
    for d in decls:
        prev = by_name.get(d.name)
        if prev is None or (prev.path.endswith('.h') and d.path.endswith('.cpp')):
            by_name[d.name] = d
    checkable = [d for d in by_name.values()
                 if d.name and not d.callback and not d.manual]

    preamble = build_preamble(checkable)
    ordered = sorted(checkable, key=lambda d: d.name)

    # ONE translation unit holding every declaration. Per-declaration parsing
    # (7,700 Index.parse calls) segfaults libclang; a single parse of a ~10k
    # line file is also two orders of magnitude faster.
    source = preamble + ''.join(declaration_only(d.text) + ';\n' for d in ordered)
    index = ci.Index.create()
    tu = index.parse('decls.cpp',
                     args=['-std=c++17', '-fsyntax-only',
                           # Pinned triple: unpinned libclang uses the HOST
                           # target, and the generator contract is identical
                           # behavior on Windows/macOS/Linux. Declarations
                           # only, so the choice is arbitrary -- sameness is
                           # what matters. Parsing with clang's frontend does
                           # not bind the BUILD to clang.
                           '--target=x86_64-pc-linux-gnu'],
                     unsaved_files=[('decls.cpp', source)])

    errs = [x for x in tu.diagnostics if x.severity >= ci.Diagnostic.Error]
    fns = {}
    for c in tu.cursor.get_children():
        if c.kind == ci.CursorKind.FUNCTION_DECL:
            fns[c.spelling] = c

    mismatches, parse_failures = [], []
    for d in ordered:
        fn = fns.get(d.name)
        if fn is None:
            parse_failures.append((d, []))
            continue
        clang_params = [(normalize(a.type.spelling), a.spelling or None)
                        for a in fn.get_arguments()]
        ours = [(normalize(p['type']), p.get('name'))
                for p in split_params(d.params)]
        clang_ret = normalize(fn.result_type.spelling)
        our_ret = normalize(d.ret or '')
        if clang_params != ours or clang_ret != our_ret:
            mismatches.append((d, our_ret, clang_ret, ours, clang_params))

    if errs:
        print('%d parse diagnostics (first 5):' % len(errs))
        src_lines = source.splitlines()
        for e in errs[:5]:
            line = e.location.line or 0
            ctx = src_lines[line-1][:90] if 0 < line <= len(src_lines) else ''
            print('  %s\n    at: %s' % (e.spelling, ctx))

    print('checked %d declarations against libclang' % len(checkable))
    print('  parse failures : %d' % len(parse_failures))
    print('  type mismatches: %d' % len(mismatches))

    for d, e in parse_failures[:10]:
        print('  PARSE %s (%s:%d): %s'
              % (d.name, os.path.basename(d.path), d.line,
                 e[0].spelling if e else 'function not found'))
    for d, our_ret, clang_ret, ours, theirs in mismatches[:10]:
        print('  DIFF  %s (%s:%d)' % (d.name, os.path.basename(d.path), d.line))
        if our_ret != clang_ret:
            print('        ret: scanner=%r clang=%r' % (our_ret, clang_ret))
        for a, b in zip(ours, theirs):
            if a != b:
                print('        arg: scanner=%r clang=%r' % (a, b))
        if len(ours) != len(theirs):
            print('        arity: scanner=%d clang=%d' % (len(ours), len(theirs)))

    return 1 if (mismatches or parse_failures) else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
