#!/usr/bin/env python3
"""The retired-members appendix of docs/migrating-to-9.md (M5), from api/retired.toml.

Rewrites the block between the `retired:begin` / `retired:end` markers; the rest of the guide is
written by hand (its source is docs/vnext/deviations.md). A row's note is kept for the reader,
minus the references that only mean something inside this repo (decision dates, triage ids,
generator-log and deviation numbers).

    python3 tools/docs/migration.py           # rewrite the appendix
    python3 tools/docs/migration.py --check   # exit 1 when it is stale
"""
import os
import re
import sys
import tomllib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
GUIDE = os.path.join(ROOT, 'docs', 'migrating-to-9.md')
BEGIN, END = '<!-- retired:begin (tools/docs/migration.py) -->', '<!-- retired:end -->'

# the old SubD iterator classes had no public name of their own: what a script wrote to get one
OLD_CLASS = {
    'BND_SubDFaceIteratorFromSubD': 'subd.Faces', 'BND_SubDEdgeIteratorFromSubD': 'subd.Edges',
    'BND_SubDVertexIteratorFromSubD': 'subd.Vertices',
    'BND_SubDEdgeIteratorFromVertex': 'vertex.Edges', 'BND_SubDFaceIteratorFromVertex': 'vertex.Faces',
    'BND_SubDEdgeIteratorFromFace': 'face.Edges', 'BND_SubDVertexIteratorFromFace': 'face.Vertices',
    'BND_SubDFaceIteratorFromEdge': 'edge.Faces', 'BND_SubDVertexIteratorFromEdge': 'edge.Vertices',
}


# a note that still talks about the implementation after clean() is left out
INTERNAL = re.compile(r'\b(?:old binding|old one|generated|hand|registry|scoreboard|ON_\w+|ONX_\w+|RHINO_SDK|'
                      r'opennurbs|RDK|native|DecalEnumerator|sibling|Luis|triage|pilot|audit|managed|internal|C\+\+|U\d+)\b', re.I)


def clean(note):
    """a retired.toml note, for a reader outside the repo ('' when it is about the implementation)"""
    s, prev = note or '', None
    while s != prev:
        prev, s = s, _clean_once(s)
    s = s.strip(' ;:,-')
    return '' if INTERNAL.search(s) else s.replace('|', '\\|')


def _clean_once(s):
    s = re.sub(r'(?:^|(?<=\s))#\d+(?:\s*\(U\d+\))?\s*:?\s*', '', s)
    s = re.sub(r'\bRH-\d+\s*', '', s)
    s = re.sub(r'^\s*/\s*[A-Z]+\d*\s*:\s*', '', s)
    s = re.sub(r'\btriage [A-Z]+\d*(?:\s*/\s*[A-Z]+\d*)*\s*(?:\([^)]*\))?\s*[:;]?\s*', '', s)
    s = re.sub(r'\baudit\s*:\s*', '', s)
    s = re.sub(r'\s*\(?pilot #\d+\)?[:;]?', '', s)
    s = re.sub(r'\(hand member,?\)', '', s)
    s = re.sub(r'\((?:[^()]|\([^()]*\))*\b(?:Luis|triage)\b(?:[^()]|\([^()]*\))*\)', '', s)
    s = re.sub(r'^\s*(?:triage [A-Z]+\d*\s*(?:\([^)]*\))?|Luis[^:]*):\s*', '', s)
    s = re.sub(r'\s*\((?:[DG]\d+|pilot #\d+)(?:[,;] *(?:[DG]\d+|pilot #\d+))*\)', '', s)
    s = re.sub(r'\b(?:G|D)\d+\b', '', s)
    s = re.sub(r'\s*\(commits;?\)', '', s)
    return re.sub(r'\s+([,;:)])', r'\1', re.sub(r'\s{2,}', ' ', s)).strip(' ;,-')


def rows():
    out = []
    for x in tomllib.load(open(os.path.join(ROOT, 'api', 'retired.toml'), 'rb'))['retired']:
        cls = OLD_CLASS.get(x['class'], x['class'])
        names = x.get('members') or [x['member']]
        old = ', '.join('`%s.%s`' % (cls, n) for n in names)
        if x['status'] == 'replaced':
            new = ', '.join('`%s`' % b.replace('.__init__', '(...)').replace('.__mul__', ' * ') for b in x['by'])
        else:
            new = 'removed'
        out.append((x['class'].replace('BND_', ''), names[0], '| %s | %s | %s |' % (old, new, clean(x.get('note')))))
    return [r for *_, r in sorted(out)]


def block():
    return '\n'.join([BEGIN, '', '| rhino3dm 8.x | 9.2 | Note |', '|---|---|---|'] + rows() + ['', END])


def main():
    text = open(GUIDE).read()
    i, j = text.index(BEGIN), text.index(END) + len(END)
    new = text[:i] + block() + text[j:]
    if '--check' in sys.argv:
        sys.exit(0 if new == text else 1)
    open(GUIDE, 'w').write(new)
    print('%s: %d retired members' % (os.path.relpath(GUIDE, ROOT), len(rows())))


if __name__ == '__main__':
    main()
