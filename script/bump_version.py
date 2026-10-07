#!/usr/bin/env python3
"""Propagate the version in src/version.txt to every file that carries one.

src/version.txt is the single source of truth. Each consumer gets the spelling
its ecosystem requires:

  src/version.txt                         9.0.0-beta   (semver, as authored)
  package.json                            9.0.0-beta   npm
  src/dotnet/Rhino3dm.csproj              9.0.0-beta   NuGet
  src/dotnet/Properties/AssemblyInfo.cs   9.0.0.0      AssemblyVersion is 4 numeric
                                                       parts and cannot carry a suffix
  setup.py                                9.0.0b0      PEP 440
  src/rhino3dm/__init__.py                9.0.0b0      PEP 440

Usage:
  python3 script/bump_version.py                 # propagate version.txt everywhere
  python3 script/bump_version.py 9.1.0-beta2     # set version.txt first, then propagate
  python3 script/bump_version.py --check         # verify only; non-zero exit if out of sync
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VERSION_FILE = os.path.join(ROOT, 'src', 'version.txt')

# release part, optional pre-release kind + number: 9.0.0, 9.0.0-beta, 9.0.0-rc2
SEMVER = re.compile(r'^(\d+)\.(\d+)\.(\d+)(?:-(alpha|beta|rc)\.?(\d*))?$')

PEP440_KIND = {'alpha': 'a', 'beta': 'b', 'rc': 'rc'}


class VersionError(Exception):
    pass


def parse(version):
    m = SEMVER.match(version.strip())
    if not m:
        raise VersionError(
            'cannot parse %r; expected MAJOR.MINOR.PATCH with an optional '
            '-alpha/-beta/-rc suffix, e.g. 9.0.0-beta or 9.1.0-rc2' % version)
    major, minor, patch, kind, num = m.groups()
    return major, minor, patch, kind, num


def spellings(version):
    """The version as each ecosystem needs it."""
    major, minor, patch, kind, num = parse(version)
    release = '%s.%s.%s' % (major, minor, patch)
    pep440 = release if not kind else '%s%s%s' % (release, PEP440_KIND[kind], num or '0')
    return {
        'semver': version.strip(),      # npm + NuGet take the authored string
        'pep440': pep440,
        'assembly': release + '.0',     # AssemblyVersion: 4 numeric parts, no suffix
    }


# (path, regex with one capture group for the version, spelling key)
TARGETS = [
    ('package.json',                       r'("version"\s*:\s*")([^"]+)(")',              'semver'),
    ('src/dotnet/Rhino3dm.csproj',          r'(<Version>)([^<]+)(</Version>)',             'semver'),
    # anchored to line start so the commented-out sample AssemblyVersion is skipped
    ('src/dotnet/Properties/AssemblyInfo.cs',
     r'(?m)^(\[assembly: AssemblyVersion\(")([^"]+)("\)\])',                                'assembly'),
    ('setup.py',                            r"(version=')([^']+)(')",                      'pep440'),
    ('src/rhino3dm/__init__.py',            r"(__version__ = ')([^']+)(')",                'pep440'),
]


def apply(check_only=False):
    version = io.open(VERSION_FILE, encoding='utf-8').read().strip()
    want = spellings(version)
    print('src/version.txt: %s' % version)
    print('  semver/npm/NuGet %s | PEP 440 %s | AssemblyVersion %s\n'
          % (want['semver'], want['pep440'], want['assembly']))

    stale = 0
    for rel, pattern, key in TARGETS:
        path = os.path.join(ROOT, rel)
        text = io.open(path, encoding='utf-8').read()
        matches = list(re.finditer(pattern, text))
        if len(matches) != 1:
            raise VersionError('%s: expected exactly 1 version match, found %d'
                               % (rel, len(matches)))
        current = matches[0].group(2)
        target = want[key]
        if current == target:
            print('  ok      %-40s %s' % (rel, current))
            continue
        stale += 1
        if check_only:
            print('  STALE   %-40s %s (want %s)' % (rel, current, target))
        else:
            io.open(path, 'w', encoding='utf-8').write(
                text[:matches[0].start(2)] + target + text[matches[0].end(2):])
            print('  updated %-40s %s -> %s' % (rel, current, target))

    if check_only and stale:
        print('\n%d file(s) out of sync with src/version.txt' % stale)
        return 1
    print('\n%s' % ('all in sync' if not stale else 'updated %d file(s)' % stale))
    return 0


def main(argv):
    args = [a for a in argv[1:]]
    check_only = '--check' in args
    args = [a for a in args if a != '--check']
    if args:
        if check_only:
            raise VersionError('--check cannot be combined with setting a new version')
        spellings(args[0])          # validate before writing
        io.open(VERSION_FILE, 'w', encoding='utf-8').write(args[0].strip() + '\n')
        print('set src/version.txt to %s\n' % args[0].strip())
    return apply(check_only)


if __name__ == '__main__':
    try:
        sys.exit(main(sys.argv))
    except VersionError as e:
        print('error: %s' % e, file=sys.stderr)
        sys.exit(2)
