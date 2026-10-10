"""Coverage of the RhinoCommon portable surface: generated vs hand bindings.

Runs against the BUILT python module (runtime introspection is ground truth
for what each surface actually registers -- no .cpp parsing), with
api/manifest.json as the RhinoCommon denominator. Python member names are
identity to RhinoCommon by locked decision, so name-level matching is exact.

    python3 coverage.py <module_dir> <manifest.json> [out.md]

Intended to run in the Linux build container:
    python3 /src/tools/generate/coverage.py /sp/pylin /src/api/manifest.json /sp/coverage.md

Per registry class: unique RhinoCommon member names (methods | properties),
how many the hand class and the generated class expose, as counts and
percentages. A totals row, plus a whole-portable-surface row that measures
every manifest type against the module.
"""

import importlib
import json
import os
import sys


def main(argv):
    module_dir = argv[1]
    manifest_path = argv[2]
    out_path = argv[3] if len(argv) > 3 else None
    sys.path.insert(0, module_dir)
    r = importlib.import_module('_rhino3dm')

    manifest = json.load(open(manifest_path))
    root = os.path.dirname(os.path.dirname(os.path.abspath(manifest_path)))

    # the generator registry is the single source for cs-name -> generated-name
    sys.path.insert(0, os.path.join(root, 'tools', 'generate'))
    import bnd_gen
    registry = [c for c in bnd_gen.CLASSES]
    # translated value types (api/valuetypes.json): generated <Cs>X vs old <Cs>
    vt_path = os.path.join(root, 'api', 'valuetypes.json')
    if os.path.exists(vt_path):
        for t in json.load(open(vt_path))['types']:
            if t['IsStaticClass']:
                continue
            registry.append({'type': 'Rhino.Geometry.' + t['Cs'], 'cs': t['Cs'],
                             'reg': t['Cs'] + 'X', 'value_type': True})

    def loc(*paths):
        n = 0
        for p in paths:
            try:
                n += sum(1 for _ in open(p, encoding='utf-8', errors='replace'))
            except OSError:
                pass
        return n

    HAND_VT_FILES = {'Point3d': 'bnd_point', 'Vector3d': 'bnd_point', 'Point2d': 'bnd_point',
                     'Interval': 'bnd_point', 'Plane': 'bnd_plane', 'Line': 'bnd_defines',
                     'BoundingBox': 'bnd_boundingbox', 'Circle': 'bnd_circle', 'Arc': 'bnd_arc',
                     'Sphere': 'bnd_sphere', 'Transform': 'bnd_xform', 'Ellipse': 'bnd_ellipse',
                     'Cylinder': 'bnd_cylinder', 'Cone': 'bnd_cone'}

    def hand_loc(c):
        if c.get('value_type') and c['cs'] in HAND_VT_FILES:
            h = os.path.join(root, 'src', 'bindings', HAND_VT_FILES[c['cs']] + '.h')
            return loc(h, h[:-2] + '.cpp')
        if 'hand_header' not in c:
            return 0
        h = os.path.join(root, 'src', 'bindings', c['hand_header'])
        return loc(h, h[:-2] + '.cpp')

    def gen_loc(c):
        g = os.path.join(root, 'src', 'generated', 'gen_%s' % c['reg'].lower())
        return loc(g + '.h', g + '.cpp')

    def spec_names(cs_type):
        """unique member names on the portable surface, split by kind"""
        methods, props, settable = set(), set(), set()
        for x in manifest['members']:
            if (x['Type'] == cs_type
                    and x['Variant'] in ('portable', 'rhino3dm-only')):
                if x['Kind'] == 'method':
                    methods.add(x['Name'])
                elif x['Kind'] == 'property':
                    props.add(x['Name'])
                    if x.get('Setter'):           # public `set` accessor
                        settable.add(x['Name'])
        return methods, props, settable

    def exposed(cls):
        return set() if cls is None else {n for n in dir(cls)
                                          if not n.startswith('_')}

    def writable(cls):
        """property names with a setter (inherited included)"""
        out = set()
        for k in (getattr(cls, '__mro__', ()) if cls is not None else ()):
            for n, a in vars(k).items():
                if isinstance(a, property) and a.fset is not None:
                    out.add(n)
        return out

    rows = []
    tot = {'m': 0, 'p': 0, 's': 0, 'hm': 0, 'hp': 0, 'hs': 0,
           'gm': 0, 'gp': 0, 'gs': 0, 'hloc': 0, 'gloc': 0}
    for c in registry:
        methods, props, settable = spec_names(c['type'])
        hand_cls, gen_cls = getattr(r, c['cs'], None), getattr(r, c['reg'], None)
        hand, gen = exposed(hand_cls), exposed(gen_cls)
        hm, hp, hs = len(methods & hand), len(props & hand), len(settable & writable(hand_cls))
        gm, gp, gs = len(methods & gen), len(props & gen), len(settable & writable(gen_cls))
        hloc, gloc = hand_loc(c), gen_loc(c)
        row = dict(m=len(methods), p=len(props), s=len(settable), hm=hm, hp=hp, hs=hs,
                   gm=gm, gp=gp, gs=gs, hloc=hloc, gloc=gloc)
        rows.append((c['cs'], row))
        for k, v in row.items():
            tot[k] += v

    # whole portable surface: every type in the manifest vs the module
    all_names, all_hand = 0, 0
    by_type = {}
    for x in manifest['members']:
        if (x['Variant'] in ('portable', 'rhino3dm-only')
                and x['Kind'] in ('method', 'property')):
            by_type.setdefault(x['Type'], set()).add(x['Name'])
    gen_types = {c['type'] for c in registry}
    all_gen = tot['gm'] + tot['gp']
    for t, names in by_type.items():
        all_names += len(names)
        hand_cls = getattr(r, t.split('.')[-1], None)
        if hand_cls is not None:
            all_hand += len(names & exposed(hand_cls))

    def pct(n, d):
        return '%d%%' % round(100.0 * n / d) if d else '-'

    def duel(hand_n, gen_n, denom):
        """format the hand/gen cell pair, highlighting the winner --
        bold + green dot on whichever covers more, nothing when tied"""
        h = '%d (%s)' % (hand_n, pct(hand_n, denom))
        g = '%d (%s)' % (gen_n, pct(gen_n, denom))
        if hand_n > gen_n:
            h = '**%s** \U0001F7E2' % h
        elif gen_n > hand_n:
            g = '**%s** \U0001F7E2' % g
        return h, g

    import datetime
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%d %H:%M UTC')
    src_commit = manifest.get('source', {}).get('commit', '?')[:10]
    lines = [
        '# Binding coverage vs RhinoCommon (portable surface)',
        '',
        '_Generated %s from rhino `%s` (api/manifest.json)._' % (stamp, src_commit),
        '',
        'Generated by tools/generate/coverage.py against the built python',
        'module; unique member NAMES per type (overloads collapse to one),',
        'methods and properties only. Hand = the shipped bindings; Gen = the',
        'vNext generated classes (inherited members included). Value types',
        '(Point3d, Plane, ...) are the translated RhinoCommon bodies.',
        'Settable = properties with a public setter in RhinoCommon; a binding',
        'covers one only if its property is writable (has a setter).',
        '',
        '| Class | Methods | Props | Settable | Hand methods | Hand props | Hand set | Gen methods | Gen props | Gen set | Hand LOC | Gen LOC |',
        '|---|---|---|---|---|---|---|---|---|---|---|---|',
    ]

    def row_line(label, d, bold=False):
        h_m, g_m = duel(d['hm'], d['gm'], d['m'])
        h_p, g_p = duel(d['hp'], d['gp'], d['p'])
        h_s, g_s = duel(d['hs'], d['gs'], d['s'])
        b = (lambda x: '**%s**' % x) if bold else str
        return ('| %s | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s |'
                % (label, b(d['m']), b(d['p']), b(d['s']), h_m, h_p, h_s, g_m, g_p, g_s,
                   b(d['hloc']) if d['hloc'] else '-', b(d['gloc'])))
    for cs, d in rows:
        lines.append(row_line(cs, d))
    lines.append(row_line('**registry total**', tot, bold=True))
    lines.append('')
    lines.append('\U0001F7E2 **bold** marks the higher coverage of each'
                 ' hand/generated pair; unmarked pairs are tied.')
    all_hand_loc = loc(*[os.path.join(root, 'src', 'bindings', f)
                         for f in os.listdir(os.path.join(root, 'src', 'bindings'))
                         if f.endswith(('.h', '.cpp'))])
    all_gen_loc = loc(*[os.path.join(root, 'src', 'generated', f)
                        for f in os.listdir(os.path.join(root, 'src', 'generated'))
                        if f.endswith(('.h', '.cpp'))])
    lines += [
        '',
        'Hand LOC counts that class\'s bnd_*.h + .cpp pair; several classes',
        'share a file, so per-class hand numbers overlap and the whole-layer',
        'figure below is the honest total.',
        '',
        'Whole portable surface (%d unique member names across %d types):'
        % (all_names, len(by_type)),
        '- hand bindings cover **%d (%s)** in **%d lines** (src/bindings, all files)'
        % (all_hand, pct(all_hand, all_names), all_hand_loc),
        '- generated bindings cover **%d (%s)** in **%d lines** (src/generated), from %d registry types'
        % (all_gen, pct(all_gen, all_names), all_gen_loc, len(registry)),
        '',
    ]
    text = '\n'.join(lines)
    print(text)
    if out_path:
        with open(out_path, 'w', encoding='utf-8', newline='\n') as f:
            f.write(text + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
