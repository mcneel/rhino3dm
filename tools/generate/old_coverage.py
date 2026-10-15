"""Replacement readiness: does the generated surface cover the OLD bindings?

coverage.py measures both surfaces against RhinoCommon, so a generated member
the old bindings never had still raises the generated score. This report
measures the other direction -- the denominator is what the hand bindings
ship TODAY -- because the old bindings can only be retired when everything
they expose is either generated or deliberately handled.

A name counts as RhinoCommon when the type or any of its bases declares it,
walking both RhinoCommon's hierarchy (manifest `type` records) and the hand
class's Python one. Every public member name of every hand-bound Python class lands in exactly
one bucket:

  covered          the generated counterpart class exposes the name
  generator gap    the name is on RhinoCommon's portable surface for that
                   type, but not generated yet (a generator problem)
  class not begun  the type has no generated class yet (registry work)
  not RhinoCommon  the name is not a portable RhinoCommon member of that
                   type: rhino3dm-only API (Encode, ToThreejsJSON, invented
                   names, Python-shape variants). No generator produces
                   these; each needs a decision: keep by hand, rename, drop.

Runs against the BUILT python module (same ground truth as coverage.py):

    python3 old_coverage.py <module_dir> <manifest.json> [out.md]
"""

import datetime
import importlib
import json
import os
import re
import sys


def main(argv):
    module_dir, manifest_path = argv[1], argv[2]
    out_path = argv[3] if len(argv) > 3 else None
    sys.path.insert(0, module_dir)
    r = importlib.import_module('_rhino3dm')
    manifest = json.load(open(manifest_path))
    root = os.path.dirname(os.path.dirname(os.path.abspath(manifest_path)))
    sys.path.insert(0, os.path.join(root, 'tools', 'generate'))
    import bnd_gen

    # hand Python class -> generated class name
    gen_of = {c['cs']: c['reg'] for c in bnd_gen.CLASSES}
    vt_path = os.path.join(root, 'api', 'valuetypes.json')
    for t in json.load(open(vt_path))['types']:
        if not t['IsStaticClass']:
            gen_of[t['Cs']] = t['Cs'] + 'X'
    # an old class registered under another name than its C++ type's RhinoCommon
    # name (`BND_InstanceDefinitionGeometry` as "InstanceDefinition"): pair it
    # through the registry's hand class, not the name
    sys.path.insert(0, os.path.join(root, 'tools', 'generate'))
    import logic_match
    old_cpp2py = logic_match.LogicIndex(root).old_cpp2py
    for c in bnd_gen.CLASSES:
        old = c.get('old') or c.get('hand')          # 'old': a replaced hand class, no bridge
        if old and old_cpp2py.get(old):
            gen_of.setdefault(old_cpp2py[old], c['reg'])
    gen_names = set(gen_of.values())

    # RhinoCommon member names per SIMPLE type name, by variant. Several
    # namespaces can share a simple name; the union is the honest test for
    # "is this a RhinoCommon member" (a false 'not RhinoCommon' is worse
    # than a generous match).
    portable, anyvariant = {}, {}
    for x in manifest['members']:
        if x['Kind'] not in ('method', 'property', 'field', 'static-field', 'event'):
            continue
        simple = x['Type'].replace('+', '.').split('.')[-1]
        anyvariant.setdefault(simple, set()).add(x['Name'])
        if x['Variant'] in ('portable', 'rhino3dm-only'):
            portable.setdefault(simple, set()).add(x['Name'])
    rc_bases = {}
    for x in manifest['members']:
        if x['Kind'] == 'type':
            simple = x['Type'].replace('+', '.').split('.')[-1]
            anyvariant.setdefault(simple, set())
            for b in x.get('Bases') or []:
                rc_bases.setdefault(simple, set()).add(b.split('.')[-1])
    rc_types = set(anyvariant)

    def rc_chain(name, seen=None):
        """the type plus every RhinoCommon base (classes and interfaces)"""
        seen = set() if seen is None else seen
        if name in seen:
            return seen
        seen.add(name)
        for b in rc_bases.get(name, ()):
            rc_chain(b, seen)
        return seen

    def names(cls):
        """public names a Python user can reach: own + inherited, no dunders,
        minus pybind11/object plumbing"""
        base = set(dir(object))
        return {n for n in dir(cls) if not n.startswith('_') and n not in base}

    def declared(cls):
        """public names this class itself binds (inherited ones are counted
        once, at the class that declares them -- GetUserString is ONE
        binding on CommonObject, not one per subclass)"""
        return {n for n in vars(cls) if not n.startswith('_')}

    rows, review = [], []
    for hname in sorted(dir(r)):
        h = getattr(r, hname)
        if not isinstance(h, type) or hname.startswith('_') or hname in gen_names:
            continue
        if hasattr(h, '__members__'):           # enums: values, not API members
            continue
        if hname.endswith('Iterator'):           # Python-protocol helpers
            continue
        hn = declared(h)
        if not hn:
            continue
        gname = gen_of.get(hname)
        g = getattr(r, gname, None) if gname else None
        gn = names(g) if g is not None else set()
        # RhinoCommon declares many members on a base (CommonObject.Decode,
        # GeometryBase.GetBoundingBox, Curve.TryGetArc); the hand classes mirror
        # that hierarchy, so the spec for a class is the union over its MRO
        # ...and RhinoCommon's own hierarchy (recorded by specgen as `type`
        # records), which the hand classes do not always mirror (hand Layer
        # does not derive ModelComponent; Point3dList's Count is RhinoList's)
        chain = set()
        for k in h.__mro__:
            if k.__name__ not in ('object', 'pybind11_object'):
                rc_chain(k.__name__, chain)
                # an old class registered under another name (`Displacement` is
                # BND_File3dmDisplacement): RhinoCommon's type is the paired one
                paired = bnd_gen.BY_REG.get(gen_of.get(k.__name__, ''))
                if paired:
                    rc_chain(paired['cs'], chain)
        spec = set().union(*(portable.get(k, set()) for k in chain))
        rhino_only = set().union(*(anyvariant.get(k, set()) for k in chain)) - spec
        b = {'covered': set(), 'gap': set(), 'notbegun': set(), 'notrc': set(), 'rhinoonly': set()}
        for n in hn:
            if n in gn:
                b['covered'].add(n)
            elif n in spec:
                b['gap' if g is not None else 'notbegun'].add(n)
            elif n in rhino_only:
                b['rhinoonly'].add(n)
            else:
                b['notrc'].add(n)
        # the RhinoCommon type this class stands for: its own name, or the type of the
        # generated class it is paired with (File3dmEmbeddedFileTable -> File3dmEmbeddedFiles)
        paired_h = bnd_gen.BY_REG.get(gen_of.get(hname, ''))
        rc_name = hname if hname in rc_types else (paired_h['cs'] if paired_h and paired_h['cs'] in rc_types else None)
        rows.append((hname, gname if g is not None else None, rc_name is not None, b))
        review.append((hname, h, rc_name or False, set(spec) | set(rhino_only), hn,
                       sorted(b['notrc']), sorted(b['rhinoonly'])))

    tot = {k: sum(len(row[3][k]) for row in rows) for k in rows[0][3]}
    total = sum(tot.values())
    begun = [row for row in rows if row[1]]
    notbegun = [row for row in rows if not row[1]]

    def pct(n, d):
        return '%d%%' % round(100.0 * n / d) if d else '-'

    def size(b):
        return sum(len(v) for v in b.values())

    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%d %H:%M UTC')
    commit = manifest.get('source', {}).get('commit', '?')[:10]
    L = [
        '# Replacement readiness: generated vs the old bindings',
        '',
        '_Generated %s from rhino `%s` by tools/generate/old_coverage.py._' % (stamp, commit),
        '',
        'The denominator is **what the old (hand) Python bindings expose today**:',
        'every public member each hand-bound class declares, counted once at the',
        'declaring class (`GetUserString` is one binding on `CommonObject`, not',
        'one per subclass). Covered means the declaring class\'s generated',
        'counterpart exposes it. Enums and `*Iterator` helpers are excluded. This is the list',
        'that must reach zero before the old bindings can be deleted.',
        '(`coverage.md` measures against RhinoCommon instead, so it also',
        'credits members the old bindings never had.)',
        '',
        '| Bucket | Members | Share | What closes it |',
        '|---|---|---|---|',
        '| Covered by generated | **%d** | %s | done |' % (tot['covered'], pct(tot['covered'], total)),
        '| Generator gap (class generated, member not yet) | %d | %s | generator work |'
        % (tot['gap'], pct(tot['gap'], total)),
        '| Class not begun (in RhinoCommon, no generated class) | %d | %s | registry work |'
        % (tot['notbegun'], pct(tot['notbegun'], total)),
        '| Not RhinoCommon (rhino3dm-only API) | %d | %s | decision per member: hand-keep / rename / drop |'
        % (tot['notrc'], pct(tot['notrc'], total)),
        '| RhinoCommon, but rhino-only there | %d | %s | review: works in rhino3dm today |'
        % (tot['rhinoonly'], pct(tot['rhinoonly'], total)),
        '| **Total old-binding members** | **%d** | | |' % total,
        '',
        '**Generator target: %d / %d (%s)** -- the old members that have a RhinoCommon'
        % (tot['covered'], total - tot['notrc'], pct(tot['covered'], total - tot['notrc'])),
        'counterpart (everything but the rhino3dm-only bucket, which is decided per member,',
        'not generated). %d to go: %d gap + %d not begun + %d rhino-only.'
        % (total - tot['notrc'] - tot['covered'], tot['gap'], tot['notbegun'], tot['rhinoonly']),
        '',
        'Hand classes: %d, of which %d have a generated counterpart.' % (len(rows), len(begun)),
        '',
        '## Classes with a generated counterpart',
        '',
        'Sorted by what is still missing. "Missing" lists every member the old',
        'class declares that the generated class does not expose yet.',
        '',
        '| Old class | Generated | Old members | Covered | Gap | Not RC | Missing (gap) | Missing (not RhinoCommon) |',
        '|---|---|---|---|---|---|---|---|',
    ]
    for hname, gname, _, b in sorted(begun, key=lambda row: (-(len(row[3]['gap']) + len(row[3]['notrc'])), row[0])):
        L.append('| %s | %s | %d | %d (%s) | %d | %d | %s | %s |' % (
            hname, gname, size(b), len(b['covered']), pct(len(b['covered']), size(b)),
            len(b['gap']), len(b['notrc']) + len(b['rhinoonly']),
            ', '.join(sorted(b['gap'])) or '-',
            ', '.join(sorted(b['notrc'] | b['rhinoonly'])) or '-'))
    L += [
        '',
        '## Classes not begun',
        '',
        'No generated class yet. "RC" marks whether RhinoCommon has a type of',
        'that name (no: a rhino3dm-only class, e.g. File3dm table wrappers).',
        '',
        '| Old class | RC | Old members | In RhinoCommon | Rhino-only in RC | Not RhinoCommon | Not-RhinoCommon names |',
        '|---|---|---|---|---|---|---|',
    ]
    for hname, _, inrc, b in sorted(notbegun, key=lambda row: (-size(row[3]), row[0])):
        nr = sorted(b['notrc'])
        L.append('| %s | %s | %d | %d | %d | %d | %s |' % (
            hname, 'yes' if inrc else '**no**', size(b), len(b['notbegun']), len(b['rhinoonly']), len(nr),
            ', '.join(nr[:12]) + (' ... (+%d)' % (len(nr) - 12) if len(nr) > 12 else '') or '-'))
    text = '\n'.join(L) + '\n'
    print(text)
    if out_path:
        with open(out_path, 'w', encoding='utf-8', newline='\n') as f:
            f.write(text)
        write_review(os.path.join(os.path.dirname(out_path), 'rhino3dm-only-members.md'),
                     review, stamp, commit, load_retired(root, r), r, gen_of, root, bnd_gen)
    return 0


SERIALIZATION = {'Encode', 'Decode', 'ToJSON', 'FromJSON', 'RdkXml', 'ToThreejsJSON',
                 'ToThreejsJSONRotate', 'FromThreejsJSON', 'ToNurbsSurfaceThreejsJSON'}


def hint(name, rc_names, declared):
    """a mechanical suggestion for the reviewer -- never a verdict"""
    import difflib
    m = re.match(r'^(.*?\D)(\d+)$', name)
    if m and (m.group(1) in declared or m.group(1) in rc_names):
        return 'numbered variant of `%s` (likely an overload)' % m.group(1)
    if name in SERIALIZATION or 'Threejs' in name:
        return 'serialization / interop helper'
    for cand in ('Is' + name, 'Get' + name, name + 's'):
        if cand in rc_names:
            return 'RhinoCommon has `%s`' % cand
    if name[:3] in ('Get', 'Set') and name[3:] in rc_names:
        return 'RhinoCommon property `%s` (as a %s method here)' % (name[3:], name[:3].lower())
    # `Length` vs `CompressedLength`: a RhinoCommon name that qualifies this one
    longer = sorted(c for c in rc_names if c.endswith(name) and c != name and c[len(c) - len(name) - 1].islower())
    if len(longer) == 1:
        return 'RhinoCommon has `%s`' % longer[0]
    close = difflib.get_close_matches(name, sorted(rc_names), n=2, cutoff=0.75)
    if close:
        return 'closest RhinoCommon: ' + ', '.join('`%s`' % c for c in close)
    return ''


def load_retired(root, r):
    """api/retired.toml -> {(old class, member): entry}, each `by` checked
    against the built generated module: a missing replacement is flagged,
    never reported as replaced"""
    path = os.path.join(root, 'api', 'retired.toml')
    if not os.path.exists(path):
        return {}
    import tomllib
    out = {}
    for e in tomllib.load(open(path, 'rb')).get('retired', []):
        missing = [b for b in e.get('by', [])
                   if not hasattr(getattr(r, b.split('.')[0], None), b.split('.', 1)[1])]
        for m in e.get('members') or [e['member']]:
            out[(e['class'], m)] = dict(e, missing=missing)
    return out


def load_kept(root):
    """api/exceptions.toml [[hand_kept]] -> {(old class, member or None): entry}: the
    rhino3dm-only API Luis decided to keep hand-written (None: the whole class)"""
    path = os.path.join(root, 'api', 'exceptions.toml')
    if not root or not os.path.exists(path):
        return {}
    import tomllib
    out = {}
    for e in tomllib.load(open(path, 'rb')).get('hand_kept', []):
        for m in e.get('members') or [None]:
            out[(e['class'], m)] = e
    return out


def retired_tag(e):
    if e['status'] == 'replaced':
        if e['missing']:
            return '**replacement missing**: %s' % ', '.join('`%s`' % b for b in e['missing'])
        tag = '**replaced** by %s' % ', '.join('`%s`' % b for b in e['by'])
    else:
        tag = '**proposed drop**'
    return tag + (' (%s)' % e['note'] if e.get('note') else '')


def write_review(path, review, stamp, commit, retired=None, r=None, gen_of=None, root=None, bnd_gen=None):
    """every old-binding member that is NOT a portable RhinoCommon member, one
    row each, with a mechanical hint -- the list Luis classifies (keep by hand
    / rename to the RhinoCommon name / drop)"""
    def kind(cls, n):
        a = vars(cls).get(n)
        return 'property' if isinstance(a, property) else 'method' if callable(a) else 'attribute'
    notrc = [(c, h, inrc, rcn, decl, n) for c, h, inrc, rcn, decl, nr, _ in review for n in nr]
    rhonly = [(c, h, n) for c, h, _, _, _, _, ro in review for n in ro]
    cats = {}
    for c, h, inrc, rcn, decl, n in notrc:
        hh = hint(n, rcn, decl) if inrc else 'class not in RhinoCommon'
        key = hh.split(' ')[0] if hh else '(no hint)'
        cats.setdefault('class not in RhinoCommon' if not inrc else
                        'numbered variant' if hh.startswith('numbered') else
                        'serialization / interop' if hh.startswith('serialization') else
                        'likely rename (RhinoCommon name exists)' if hh.startswith(('RhinoCommon has', 'RhinoCommon property', 'closest')) else
                        'no close RhinoCommon match', []).append(n)
    L = ['# rhino3dm members that are not in RhinoCommon', '',
         '_Generated %s from rhino `%s` by tools/generate/old_coverage.py._' % (stamp, commit), '',
         'Every public member the old (hand) Python bindings declare that is **not** a',
         'portable RhinoCommon member of that type or its bases. The generator cannot',
         'produce these: each needs a decision -- keep (hand-written, listed in',
         '`api/exceptions.toml`), rename to the RhinoCommon name, or drop.',
         '',
         'The **Hint** column is mechanical (name similarity, numbered suffixes); it is a',
         'suggestion for the reviewer, not a classification. Fill in **Decision**.',
         '',
         'The **Generated** column says what the generated surface already offers:',
         '- **replaced** -- an equivalent is generated and tested (`api/retired.toml`;',
         '  rationale in `retired-members.md`); **proposed drop** -- no RhinoCommon',
         '  equivalent, awaiting a decision.',
         '- **same logic** / **close** / **partial** -- found by comparing CODE, not names',
         '  (`tools/generate/logic_match.py`): the opennurbs operations the old binding',
         '  performs vs those the generated member reaches through its C function, with',
         "  the C function specialized for the constants the generated call passes (its",
         '  selector branch). *same*: every accessor performs the same operations; *close*:',
         '  the generated member does all of that and more (listed -- often a real',
         '  behavior difference, e.g. `SetFieldOverride()`); *partial*: some accessors',
         '  match, the rest have no generated counterpart. Evidence for review, not a',
         '  decision: argument handling is not compared.',
         '',
         '| Category (by hint) | Members |', '|---|---|']
    for k, v in sorted(cats.items(), key=lambda kv: -len(kv[1])):
        L.append('| %s | %d |' % (k, len(v)))
    L += ['| **Total** | **%d** |' % len(notrc), '']
    retired = retired or {}
    import logic_match
    lm = logic_match.LogicIndex(root) if root else None
    def gen_chain(c):
        """the generated counterpart of old class c and its generated bases"""
        reg = (gen_of or {}).get(c)
        out = []
        while reg:
            out.append(reg)
            cls = bnd_gen.BY_REG.get(reg) if bnd_gen else None
            reg = cls.get('base') if cls else None
        return out
    memo = {}
    def generated(c, n, hh):
        if (c, n) in memo:
            return memo[(c, n)]
        e = retired.get((c, n))
        tag = ''
        if e:
            tag = retired_tag(e)
        elif lm is not None:
            hits = lm.best_match(c, n, gen_chain(c))
            tag = '; '.join(logic_match.describe(*h) for h in hits[:2])
        memo[(c, n)] = tag
        return tag
    tags = [generated(c, n, hint(n, rcn, decl) if inrc else '') for c, h, inrc, rcn, decl, n in notrc]
    L += ['| Generated | Members |', '|---|---|',
          '| replaced (tested) | %d |' % sum(t.startswith('**replaced') for t in tags),
          '| proposed drop | %d |' % sum(t.startswith('**proposed') for t in tags),
          '| same logic (by code) | %d |' % sum(t.startswith('**same logic') for t in tags),
          '| close: generated does more (by code) | %d |' % sum(t.startswith('**close') for t in tags),
          '| partial (by code) | %d |' % sum(t.startswith('**partial') for t in tags),
          '| replacement missing | %d |' % sum(t.startswith('**replacement missing') for t in tags), '']
    kept = load_kept(root)
    def decision(c, n):
        e = kept.get((c, n)) or kept.get((c, None))
        return '**keep** (hand-written; %s)' % e['decided'] if e else ''
    L += ['Kept hand-written (`api/exceptions.toml` `[[hand_kept]]`): %d members.'
          % sum(1 for c, _, _, _, _, n in notrc if decision(c, n)), '']
    by_class = {}
    for c, h, inrc, rcn, decl, n in notrc:
        hh = hint(n, rcn, decl) if inrc else ''
        by_class.setdefault((c, inrc), []).append((n, kind(h, n), hh, generated(c, n, hh)))
    L += ['## By class', '']
    for (c, inrc), items in sorted(by_class.items(), key=lambda kv: (not kv[0][1], kv[0][0])):
        heading = (' -- class not in RhinoCommon' if not inrc else
                   '' if inrc == c else ' -- RhinoCommon `%s`' % inrc)      # paired under another name
        L += ['### %s%s' % (c, heading), '',
              '| Member | Kind | Hint | Generated | Decision |', '|---|---|---|---|---|']
        L += ['| `%s` | %s | %s | %s | %s |' % (n, k, hh, gt, decision(c, n)) for n, k, hh, gt in items]
        L.append('')
    L += ['## In RhinoCommon, but Rhino-only there', '',
          'These ARE RhinoCommon members, but compiled out of the portable build (they need',
          'a running Rhino in .NET). rhino3dm ships them today; they need review too.', '',
          '| Class | Member | Decision |', '|---|---|---|']
    L += ['| %s | `%s` | |' % (c, n) for c, _, n in sorted(rhonly)]
    # an entry of api/retired.toml that matches no row is stale (the member was
    # renamed, or turned out to be RhinoCommon): say so rather than drop it silently
    rows = {(c, n) for c, _, _, _, _, n in notrc}
    stale_kept = sorted('%s.`%s`' % (c, n) if n else '%s (class)' % c for c, n in kept
                        if (n is None and not any(rc == c for rc, _ in rows)) or (n is not None and (c, n) not in rows))
    if stale_kept:
        L += ['', '## api/exceptions.toml [[hand_kept]] entries that match no row', '']
        L += ['- %s' % k for k in stale_kept]
    stale = sorted(k for k in retired if k not in rows)
    if stale:
        L += ['', '## api/retired.toml entries that match no row', '']
        L += ['- %s.`%s`' % k for k in stale]
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(L) + '\n')


if __name__ == '__main__':
    sys.exit(main(sys.argv))
