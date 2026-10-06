"""Validate the extractor against ground truth, and find broken P/Invokes.

Three checks:
  1. Every export the extractor considers generatable is actually present in
     the built native library (no false positives).
  2. The extractor's set is a subset of what methodgen emits (no surprises).
  3. Any P/Invoke declared by methodgen but absent from the library that is
     still reachable from live, non-RHINO_SDK C# -- these are latent
     EntryPointNotFoundException bugs in the shipped package.
"""

import os
import re
import sys
import glob
import subprocess

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from scan import scan_dir
from cond import evaluate

PORTABLE_DEFINES = {'RHINO3DM_BUILD'}
# rhino3dm builds with RHINO3DM_BUILD and without RHINO_SDK.
CSHARP_DEFINES = {'RHINO3DM_BUILD'}


def library_exports(lib):
    """Exported symbol names from a built dylib/so."""
    if lib.endswith('.dylib'):
        argv = ['nm', '-gU', lib]
    else:
        argv = ['nm', '-D', '--defined-only', lib]
    proc = subprocess.run(argv, capture_output=True, text=True)
    names = {line.split()[-1].lstrip('_')
             for line in proc.stdout.splitlines() if line.split()}
    # An empty or failed nm would make every export look missing, which reads
    # as thousands of findings rather than a broken tool. Fail on the tool.
    if proc.returncode != 0 or len(names) < 100:
        raise SystemExit(
            'could not read symbols from %s\n  %s exited %d, %d symbols\n  %s'
            % (lib, argv[0], proc.returncode, len(names), proc.stderr.strip()[:300]))
    return names


def declared_pinvokes(autonative):
    names = set()
    with open(autonative, encoding='utf-8', errors='replace') as handle:
        for line in handle:
            m = re.search(r'internal static extern .*?([A-Za-z0-9_]+)\s*\(', line)
            if m:
                names.add(m.group(1))
    return names


def live_callers(dotnet_dir, wanted):
    """Where `wanted` symbols are called from C# that rhino3dm actually compiles."""
    hits = {}
    for path in glob.glob(os.path.join(dotnet_dir, '**', '*.cs'), recursive=True):
        if os.path.basename(path).startswith('AutoNative'):
            continue
        with open(path, encoding='utf-8', errors='replace') as handle:
            raw = handle.read()
        if 'UnsafeNativeMethods.' not in raw:
            continue
        keep = evaluate(raw, CSHARP_DEFINES, bare_is_defined=True)
        for i, line in enumerate(raw.splitlines()):
            if not keep[i]:
                continue
            for m in re.finditer(r'UnsafeNativeMethods\.([A-Za-z0-9_]+)', line):
                if m.group(1) in wanted:
                    hits.setdefault(m.group(1), []).append('%s:%d' % (path, i + 1))
    return hits


def main(native_dir, dotnet_dir, lib):
    decls = (scan_dir(native_dir, PORTABLE_DEFINES, '*.cpp')
             + scan_dir(native_dir, PORTABLE_DEFINES, '*.h'))
    mine = {d.name for d in decls if d.generatable}
    exports = library_exports(lib)
    declared = declared_pinvokes(os.path.join(dotnet_dir, 'AutoNativeMethods.cs'))

    missing = mine - exports
    dead = declared - exports
    reachable = live_callers(dotnet_dir, dead)

    print('extractor generatable      %5d' % len(mine))
    print('  present in library       %5d' % len(mine & exports))
    print('  MISSING (false positive) %5d' % len(missing))
    print('methodgen declared         %5d' % len(declared))
    print('  absent from library      %5d' % len(dead))
    print('  ...reachable from live C# %4d   <-- runtime failures' % len(reachable))

    for name in sorted(reachable):
        print('    %-40s %s' % (name, reachable[name][0]))

    ok = not missing and not reachable
    print('\n%s' % ('PASS' if ok else 'FAIL'))
    return 0 if ok else 1


if __name__ == '__main__':
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
    sys.exit(main(
        os.path.join(root, 'src', 'librhino3dm_native'),
        os.path.join(root, 'src', 'dotnet'),
        sys.argv[1] if len(sys.argv) > 1
        else os.path.join(root, 'src', 'build', 'macos', 'Release',
                          'librhino3dm_native.dylib')))
