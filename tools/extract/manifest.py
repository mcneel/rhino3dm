"""Build api/manifest.json: the machine-readable inventory of the C surface.

The rhino repo is the spec (read-only input); rhino3dm's copy of the C layer
is a *product* of the sync, so the manifest is extracted from rhino itself:

    python3 tools/extract/manifest.py [path-to-rhino-checkout]

Every RH_C_FUNCTION in src4/DotNetSDK/rhinocommon/c is recorded, tagged with a
variant rather than filtered:

  portable    defined in on_*.cpp AND survives preprocessing with
              RHINO3DM_BUILD defined -- the surface rhino3dm ships
  rhino-only  everything else: the rh_* families, and on_* exports inside
              #if !defined(RHINO3DM_BUILD) / #if defined(OPENNURBS_PLUS) blocks

Recording the rhino-only set instead of discarding it is what makes a future
Rhino-SDK-backed build a configuration change instead of a re-derivation.

Output is deterministic (sorted by path then line, one function per line) so
regenerating against the same rhino commit is byte-identical and a regeneration
against a newer commit reads as a reviewable per-function diff.
"""

import json
import os
import re
import subprocess
import sys

from scan import scan_dir, ARRAY_MARKER

PORTABLE_DEFINES = {'RHINO3DM_BUILD'}
C_SUBDIR = os.path.join('src4', 'DotNetSDK', 'rhinocommon', 'c')
SCHEMA = 1


def split_params(raw):
    """Split a parameter list on commas at angle-bracket/paren depth 0.

    Returns [{type, name, array}] records. /*ARRAY*/ is semantic (the only
    thing distinguishing `T[]` from `ref T`) and becomes a flag.
    """
    raw = (raw or '').strip()
    if raw in ('', 'void'):
        return []
    parts = []
    depth = 0
    start = 0
    for i, ch in enumerate(raw):
        if ch in '<(':
            depth += 1
        elif ch in '>)':
            depth -= 1
        elif ch == ',' and depth == 0:
            parts.append(raw[start:i])
            start = i + 1
    parts.append(raw[start:])

    out = []
    for part in parts:
        part = part.strip()
        is_array = ARRAY_MARKER in part
        if is_array:
            part = part.replace(ARRAY_MARKER, ' ').strip()
        part = re.sub(r'\s+', ' ', part)
        # default value, if any, stays with the record verbatim
        default = None
        if '=' in part:
            part, default = (p.strip() for p in part.split('=', 1))
        # the name is the last identifier; * and & belong to the type
        m = re.match(r'^(.*?)([A-Za-z_][A-Za-z0-9_]*)$', part)
        if m and m.group(1).strip():
            ptype = re.sub(r'\s*([*&])\s*', r'\1', m.group(1).strip())
            pname = m.group(2)
        else:
            ptype, pname = part, None   # unnamed parameter
        rec = {'type': ptype, 'name': pname}
        if is_array:
            rec['array'] = True
        if default is not None:
            rec['default'] = default
        out.append(rec)
    return out


def rhino_commit(rhino_root):
    try:
        return subprocess.check_output(
            ['git', '-C', rhino_root, 'rev-parse', 'HEAD'],
            text=True).strip()
    except Exception:
        return None


def build(rhino_root):
    cdir = os.path.join(rhino_root, C_SUBDIR)
    if not os.path.isdir(cdir):
        raise SystemExit('not a rhino checkout (missing %s): %s'
                         % (C_SUBDIR, rhino_root))

    # Headers matter: rhcommon_c.h declares exports whose definitions carry no
    # RH_C_FUNCTION macro (methodgen reads them from the header too). Where a
    # name appears in both a header and a .cpp, the definition site wins.
    decls = (scan_dir(cdir, PORTABLE_DEFINES, '*.cpp')
             + scan_dir(cdir, PORTABLE_DEFINES, '*.h'))
    by_name = {}
    for d in decls:
        prev = by_name.get(d.name)
        if prev is None or (prev.path.endswith('.h') and d.path.endswith('.cpp')):
            by_name[d.name] = d
    functions = []
    for d in sorted(by_name.values(),
                    key=lambda d: (os.path.basename(d.path), d.line)):
        base = os.path.basename(d.path)
        # The portable layer is the on_* families plus rhcommon_c.h, the
        # header rhino3dm ships. rh_* files are Rhino-only wholesale.
        portable = (base.startswith('on_') or base == 'rhcommon_c.h') and d.portable
        rec = {
            'name': d.name,
            'returns': d.ret,
            'params': split_params(d.params),
            'file': base,
            'line': d.line,
            'variant': 'portable' if portable else 'rhino-only',
        }
        if d.manual:
            rec['manual'] = True        # /*MANUAL*/ opt-out, never generated
        if d.callback:
            rec['callback'] = True      # takes a *PROC fn pointer, never generated
        functions.append(rec)

    counts = {
        'total': len(functions),
        'portable': sum(1 for f in functions if f['variant'] == 'portable'),
        'rhino_only': sum(1 for f in functions if f['variant'] == 'rhino-only'),
        'manual': sum(1 for f in functions if f.get('manual')),
        'callback': sum(1 for f in functions if f.get('callback')),
    }
    header = {
        'schema': SCHEMA,
        'source': {
            'repo': 'rhino',
            'path': C_SUBDIR.replace(os.sep, '/'),
            'commit': rhino_commit(rhino_root),
        },
        'defines': sorted(PORTABLE_DEFINES),
        'counts': counts,
    }
    return header, functions


def write(path, header, functions):
    """One function per line: regeneration diffs are per-function."""
    with open(path, 'w', encoding='utf-8', newline='\n') as out:
        out.write('{\n')
        for key in ('schema', 'source', 'defines', 'counts'):
            out.write('"%s": %s,\n' % (key, json.dumps(header[key], sort_keys=True)))
        out.write('"c_surface": [\n')
        body = ',\n'.join(json.dumps(f, sort_keys=True) for f in functions)
        out.write(body)
        out.write('\n]\n}\n')


def main(argv):
    rhino_root = os.path.expanduser(argv[1] if len(argv) > 1
                                    else '~/dev/rhino')
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(
        os.path.abspath(__file__))))
    out_path = os.path.join(repo_root, 'api', 'manifest.json')
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    header, functions = build(rhino_root)
    write(out_path, header, functions)

    c = header['counts']
    print('api/manifest.json written from %s' % rhino_root)
    print('  rhino commit : %s' % header['source']['commit'])
    print('  functions    : %(total)d  (portable %(portable)d, '
          'rhino-only %(rhino_only)d)' % c)
    print('  excluded from generation: %(manual)d manual, %(callback)d callback' % c)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
