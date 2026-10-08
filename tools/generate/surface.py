"""Generate api/surface.json: the per-language binding surface.

Input is api/manifest.json (the spec, extracted from rhino) plus
api/exceptions.toml (the escape hatches). The hand-maintained rhino3dm.d.ts is
deliberately NOT an input: it descends from docgen's unsound parser plus years
of hand edits, so it can serve as a comparison target later, never as a spec.

For every member rhino3dm ships (variant portable or rhino3dm-only), emit the
name each language uses:

  dotnet  verbatim RhinoCommon (locked decision: .NET stays verbatim)
  python  identity -- PascalCase, matching today's Python API
  js      camelCase: first character lowered, nothing else invented

Members that exist only in a binding language (the threejs family, Python
iterators) are not generated from RhinoCommon at all; they live in
exceptions.toml [[js_extra]] / [[py_extra]] and are appended here from that
list, so the surface file is the complete contract and CI can insist that
anything not in it is either generated or listed.

    python3 tools/generate/surface.py

Deterministic: same inputs, byte-identical output, one member per line.
"""

import json
import os
import sys
import tomllib

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def js_name(dotnet_name):
    """The one mechanical rule: lower the first character. Anything the rule
    cannot produce (invented names like addPointXYZ) is a legacy exception and
    belongs in the migration guide, not in the rule."""
    return dotnet_name[0].lower() + dotnet_name[1:] if dotnet_name else dotnet_name


def main():
    manifest = json.load(open(os.path.join(ROOT, 'api', 'manifest.json')))
    exceptions = tomllib.load(open(os.path.join(ROOT, 'api', 'exceptions.toml'), 'rb'))

    surface = []
    for m in manifest['members']:
        if m['Variant'] not in ('portable', 'rhino3dm-only'):
            continue
        rec = {
            'type': m['Type'],
            'kind': m['Kind'],
            'dotnet': m['Name'],
            'py': m['Name'],                      # identity, by locked decision
            'js': js_name(m['Name']),
            'signature': m['Signature'],
        }
        if m.get('Since'):
            rec['since'] = m['Since']
        if m.get('Native'):
            rec['native'] = m['Native']
        if m.get('Values'):
            rec['values'] = m['Values']
        if m['Kind'] == 'operator':
            # operators do not spell mechanically in either binding language;
            # each needs an explicit mapping (py __add__ etc.) in generation
            rec['js'] = None
            rec['py'] = None
        surface.append(rec)

    # language-only members: hand-written, enumerated, appended -- never silent
    for entry in exceptions.get('js_extra', []):
        surface.append({
            'type': entry['type'], 'kind': entry.get('kind', 'method'),
            'dotnet': None, 'py': None, 'js': entry['js'],
            'signature': None, 'hand_written': entry['reason'],
        })
    for entry in exceptions.get('py_extra', []):
        surface.append({
            'type': entry['type'], 'kind': entry.get('kind', 'method'),
            'dotnet': None, 'js': None, 'py': entry['py'],
            'signature': None, 'hand_written': entry['reason'],
        })

    surface.sort(key=lambda r: (r['type'], r['kind'],
                                r['dotnet'] or r['js'] or r['py'] or '',
                                r['signature'] or ''))

    out_path = os.path.join(ROOT, 'api', 'surface.json')
    with open(out_path, 'w', encoding='utf-8', newline='\n') as out:
        out.write('{\n"schema": 1,\n')
        out.write('"source": %s,\n' % json.dumps(manifest['source'], sort_keys=True))
        out.write('"counts": %s,\n' % json.dumps({
            'members': len(surface),
            'js_extra': len(exceptions.get('js_extra', [])),
            'py_extra': len(exceptions.get('py_extra', [])),
        }, sort_keys=True))
        out.write('"surface": [\n')
        out.write(',\n'.join(json.dumps(r, sort_keys=True) for r in surface))
        out.write('\n]\n}\n')

    print('api/surface.json: %d members' % len(surface))
    ops = sum(1 for r in surface if r['kind'] == 'operator')
    print('  operators needing explicit per-language mapping: %d' % ops)
    print('  hand-written extras from exceptions.toml: %d'
          % sum(1 for r in surface if r.get('hand_written')))
    return 0


if __name__ == '__main__':
    sys.exit(main())
