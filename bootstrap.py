#!/usr/bin/env python3
"""Check, download and install the tools that build rhino3dm.

    python bootstrap.py [py|js|dotnet|generator ...]    check (default: py, js and dotnet)
    python bootstrap.py js --install                    install what is missing or too old
    python bootstrap.py --install cmake                 install one tool
    python bootstrap.py --download node                 download only (into ~/Downloads)
    python bootstrap.py --set xcode 26.6                select an installed Xcode (macOS)

Versions, URLs and checksums come from Current_Development_Tools.md (the oldest .NET SDK from
global.json). Installs are system-wide and ask for administrator rights (sudo), as rhino's
bootstrap does. Exit code: 1 if a tool is missing or older than its minimum, 0 otherwise
(--strict: 2 if a tool is a different version than the one CI uses).
"""

import argparse
import datetime
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import urllib.request
import zipfile

ROOT = os.path.dirname(os.path.abspath(__file__))
TOOLS_MD = os.path.join(ROOT, 'Current_Development_Tools.md')
GLOBAL_JSON = os.path.join(ROOT, 'global.json')

SYSTEM = {'Darwin': 'macos', 'Linux': 'linux', 'Windows': 'windows'}.get(platform.system(), 'unknown')
ARCH = 'arm64' if platform.machine().lower() in ('arm64', 'aarch64') else 'x64'

LIBRARIES = {
    'py': ['cmake', 'python', 'compiler', 'git'],
    'js': ['cmake', 'python', 'emscripten', 'node', 'git'],
    'dotnet': ['cmake', 'dotnet', 'compiler', 'git'],
    'generator': ['python', 'libclang'],
}
INSTALLABLE = ['cmake', 'dotnet', 'emscripten', 'node', 'libclang']

COLOR = sys.stdout.isatty() and os.environ.get('NO_COLOR') is None


def paint(code, text):
    return f'\033[{code}m{text}\033[0m' if COLOR else text


class Report:
    def __init__(self):
        self.errors = self.warnings = 0

    def ok(self, msg):
        print(paint('32', 'OK:      ') + msg)

    def warn(self, msg):
        self.warnings += 1
        print(paint('33', 'WARNING: ') + msg)

    def error(self, msg):
        self.errors += 1
        print(paint('31', 'ERROR:   ') + msg)

    def note(self, msg):
        print('         ' + msg)


# ---------------------------------------------------------------------------------------------
# Current_Development_Tools.md


def read_records():
    """`key = value` lines (rhino's key style) plus the 'Last updated' date."""
    records = {}
    with open(TOOLS_MD, encoding='utf-8') as f:
        text = f.read()
    for m in re.finditer(r'^`([a-z0-9_]+)\s*=\s*([^`]*)`\s*$', text, re.M):
        records[m.group(1)] = m.group(2).strip()
    m = re.search(r'^Last updated by (\S+) on (.+)$', text, re.M)
    if m:
        try:
            records['_updated'] = datetime.datetime.strptime(m.group(2).strip(), '%B %d, %Y').date()
        except ValueError:
            pass
    with open(GLOBAL_JSON, encoding='utf-8') as f:
        records['minimum_dotnet'] = json.load(f)['sdk']['version']
    return records


def archive(records, tool):
    """(url, algorithm, digest) for this OS and architecture, or None."""
    for suffix in (f'_{SYSTEM}_{ARCH}', f'_{SYSTEM}_universal', ''):
        url = records.get(f'{tool}_archive_url{suffix}')
        if url:
            for algo in ('sha256', 'sha512'):
                digest = records.get(f'{tool}_{algo}{suffix}')
                if digest:
                    return url, algo, digest
            return url, None, None
    return None


def version_tuple(text):
    return tuple(int(x) for x in re.findall(r'\d+', text)[:3])


def satisfies(found, wanted):
    """found matches wanted at wanted's precision ('24' matches 24.22.3; '3.12' matches 3.12.7)."""
    w = version_tuple(wanted)
    return version_tuple(found)[:len(w)] == w


def at_least(found, minimum):
    w = version_tuple(minimum)
    return version_tuple(found)[:len(w)] >= w


# ---------------------------------------------------------------------------------------------
# finding installed versions


def output(cmd, cwd=None):
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=60, cwd=cwd)
    except (OSError, subprocess.TimeoutExpired):
        return None
    return (result.stdout + result.stderr).strip() if result.returncode == 0 else None


def first_version(text):
    m = re.search(r'(\d+\.\d+(?:\.\d+)?)', text or '')
    return m.group(1) if m else None


def find_cmake():
    return first_version(output(['cmake', '--version']))


def find_python():
    return platform.python_version()


def find_dotnet():
    # the SDK global.json resolves to, run beside it (an SDK that does not satisfy it fails)
    text = output(['dotnet', '--version'], cwd=ROOT)
    if text is None and output(['dotnet', '--list-sdks'], cwd=ROOT):
        return '0'  # installed, but no SDK satisfies global.json
    return first_version(text)


def find_emscripten():
    return first_version(output(['emcc', '--version']))


def find_node():
    return first_version(output(['node', '--version']))


def find_git():
    return first_version(output(['git', '--version']))


def find_xcode():
    return first_version(output(['xcodebuild', '-version']))


def vswhere(*args):
    exe = os.path.join(os.environ.get('ProgramFiles(x86)', r'C:\Program Files (x86)'),
                       'Microsoft Visual Studio', 'Installer', 'vswhere.exe')
    return output([exe, '-latest', '-products', '*', *args]) if os.path.exists(exe) else None


def find_msvc():
    return first_version(vswhere('-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64',
                                 '-property', 'catalog_productDisplayVersion'))


def find_libclang():
    try:
        from importlib.metadata import version, PackageNotFoundError
        return version('libclang')
    except Exception:
        return None


FINDERS = {
    'cmake': find_cmake, 'python': find_python, 'dotnet': find_dotnet, 'emscripten': find_emscripten,
    'node': find_node, 'git': find_git, 'xcode': find_xcode, 'msvc': find_msvc, 'libclang': find_libclang,
}


# ---------------------------------------------------------------------------------------------
# checking


def check_tool(tool, records, report):
    if tool == 'compiler':
        if SYSTEM == 'macos':
            return check_tool('xcode', records, report)
        if SYSTEM == 'windows':
            check_tool('msvc', records, report)
            if ARCH == 'x64' and not vswhere('-requires', 'Microsoft.VisualStudio.Component.VC.v142.x86.x64',
                                            '-property', 'installationPath'):
                report.warn('msvc: the v142 toolset (MSVC v142 - VS 2019 C++ x64/x86 build tools) is not '
                            'installed; the Windows x64 .NET native library is built with it')
            return
        if shutil.which('c++') or shutil.which('g++') or shutil.which('clang++'):
            return report.ok('compiler: ' + (shutil.which('c++') or shutil.which('g++') or shutil.which('clang++')))
        return report.error('compiler: no C++ compiler (c++, g++ or clang++) on PATH')

    found = FINDERS[tool]()
    current = records.get(f'currently_using_{tool}')
    minimum = records.get(f'minimum_{tool}')
    fix = f' (python bootstrap.py --install {tool})' if tool in INSTALLABLE else ''
    if found is None:
        return report.error(f'{tool}: not found{fix}')
    if tool == 'dotnet' and found == '0':
        return report.error(f'dotnet: no installed SDK satisfies global.json ({minimum} or a later 10.0 SDK){fix}')
    if minimum and not at_least(found, minimum):
        return report.error(f'{tool}: {found} is older than the minimum {minimum}{fix}')
    if current and not satisfies(found, current):
        return report.warn(f'{tool}: {found} (rhino3dm uses {current})')
    report.ok(f'{tool}: {found}')


def check_records(records, report):
    updated = records.get('_updated')
    if updated and (datetime.date.today() - updated).days > 59:
        report.warn(f'Current_Development_Tools.md was last updated {updated:%B %d, %Y}; versions may be stale')
    for sub in ('src/lib/opennurbs', 'src/lib/nanobind', 'src/lib/draco'):
        if not os.path.exists(os.path.join(ROOT, sub, '.git')):
            report.error(f'submodule {sub} is missing: git submodule update --init --recursive')


# ---------------------------------------------------------------------------------------------
# downloading and installing


def sudo(cmd):
    """Run with administrator rights: sudo on macOS / Linux (unless root); on Windows the shell must
    already be elevated."""
    if SYSTEM != 'windows' and hasattr(os, 'geteuid') and os.geteuid() != 0:
        cmd = ['sudo'] + cmd
    print('+ ' + ' '.join(cmd), flush=True)
    subprocess.run(cmd, check=True)


def download(records, tool):
    found = archive(records, tool)
    if not found:
        sys.exit(f'bootstrap.py: no {tool} download for {SYSTEM} {ARCH} in Current_Development_Tools.md')
    url, algo, digest = found
    dest_dir = os.path.join(os.path.expanduser('~'), 'Downloads')
    os.makedirs(dest_dir, exist_ok=True)
    dest = os.path.join(dest_dir, url.rsplit('/', 1)[1])
    if not (os.path.exists(dest) and digest and file_digest(dest, algo) == digest):
        print(f'downloading {url}', flush=True)
        urllib.request.urlretrieve(url, dest)
    if digest:
        actual = file_digest(dest, algo)
        if actual != digest:
            os.remove(dest)
            sys.exit(f'bootstrap.py: {algo} of {os.path.basename(dest)} is {actual}, expected {digest}; deleted')
        print(f'{algo} verified: {dest}')
    else:
        print(f'no checksum listed for {tool}: not verified')
    return dest


def file_digest(path, algo):
    h = hashlib.new(algo)
    with open(path, 'rb') as f:
        for block in iter(lambda: f.read(1 << 20), b''):
            h.update(block)
    return h.hexdigest()


def extract(path, into):
    if path.endswith('.zip'):
        with zipfile.ZipFile(path) as z:
            z.extractall(into)
    else:
        with tarfile.open(path) as t:
            t.extractall(into, filter='data') if sys.version_info >= (3, 12) else t.extractall(into)
    entries = os.listdir(into)
    return os.path.join(into, entries[0]) if len(entries) == 1 else into


def link_bin(bin_dir, names=None):
    for name in names or os.listdir(bin_dir):
        sudo(['ln', '-sfn', os.path.join(bin_dir, name), os.path.join('/usr/local/bin', name)])


def install_cmake(records):
    path = download(records, 'cmake')
    version = records['currently_using_cmake']
    if SYSTEM == 'windows':
        return sudo(['msiexec', '/i', path, '/quiet', '/norestart', 'ADD_CMAKE_TO_PATH=System'])
    with tempfile.TemporaryDirectory() as tmp:
        top = extract(path, tmp)
        if SYSTEM == 'macos':
            app = os.path.join(top, 'CMake.app')
            subprocess.run(['xattr', '-dr', 'com.apple.quarantine', app])
            sudo(['rm', '-rf', '/Applications/CMake.app'])
            sudo(['mv', app, '/Applications/CMake.app'])
            # /usr/local/bin: Xcode build phases search it and /usr/bin, not Homebrew's prefix
            link_bin('/Applications/CMake.app/Contents/bin')
        else:
            dest = f'/opt/cmake-{version}'
            sudo(['rm', '-rf', dest])
            sudo(['mv', top, dest])
            link_bin(os.path.join(dest, 'bin'))


def install_dotnet(records):
    path = download(records, 'dotnet')
    dest = {'macos': '/usr/local/share/dotnet', 'linux': '/usr/share/dotnet',
            'windows': os.path.join(os.environ.get('ProgramFiles', r'C:\Program Files'), 'dotnet')}[SYSTEM]
    if SYSTEM == 'windows':
        # the SDK zip unpacks beside any SDKs already installed
        return sudo(['powershell', '-NoProfile', '-Command',
                     f'Expand-Archive -Force -Path "{path}" -DestinationPath "{dest}"'])
    sudo(['mkdir', '-p', dest])
    sudo(['tar', '-xzf', path, '-C', dest])
    link_bin(dest, ['dotnet'])


def install_node(records):
    path = download(records, 'node')
    version = records['currently_using_node']
    if SYSTEM == 'windows':
        dest = os.path.join(os.environ.get('ProgramFiles', r'C:\Program Files'), 'nodejs')
        with tempfile.TemporaryDirectory() as tmp:
            top = extract(path, tmp)
            sudo(['powershell', '-NoProfile', '-Command',
                  f'Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "{dest}"; Move-Item "{top}" "{dest}"'])
        print(f'add {dest} to PATH if it is not there')
        return
    dest = f'/usr/local/lib/node-{version}'
    with tempfile.TemporaryDirectory() as tmp:
        top = extract(path, tmp)
        sudo(['rm', '-rf', dest])
        sudo(['mv', top, dest])
    link_bin(os.path.join(dest, 'bin'), ['node', 'npm', 'npx'])


def install_emscripten(records):
    version = records['currently_using_emscripten']
    dest = r'C:\emsdk' if SYSTEM == 'windows' else '/opt/emsdk'
    if not os.path.isdir(dest):
        sudo(['git', 'clone', '--depth', '1', '--branch', version, records['emscripten_archive_url'], dest])
        if SYSTEM != 'windows':
            sudo(['chown', '-R', f'{os.getuid()}:{os.getgid()}', dest])
    emsdk = os.path.join(dest, 'emsdk.bat' if SYSTEM == 'windows' else 'emsdk')
    for step in ('install', 'activate'):
        subprocess.run([emsdk, step, version], check=True, cwd=dest)
    env = os.path.join(dest, 'emsdk_env.bat' if SYSTEM == 'windows' else 'emsdk_env.sh')
    print(f'emscripten {version} is in {dest}; before build.py js: ' +
          (env if SYSTEM == 'windows' else f'source {env}'))


def install_libclang(records):
    subprocess.run([sys.executable, '-m', 'pip', 'install', f'libclang=={records["currently_using_libclang"]}'],
                   check=True)


def set_xcode(version):
    if SYSTEM != 'macos':
        sys.exit('bootstrap.py: --set xcode is macOS only')
    candidates = [f'/Applications/Xcode-{version}.app', f'/Applications/Xcode_{version}.app', '/Applications/Xcode.app']
    for app in candidates:
        plist = os.path.join(app, 'Contents', 'version.plist')
        found = output(['defaults', 'read', plist, 'CFBundleShortVersionString']) if os.path.exists(plist) else None
        if found and satisfies(found, version):
            return sudo(['xcode-select', '--switch', app])
    sys.exit(f'bootstrap.py: no Xcode {version} in /Applications ({", ".join(candidates)})')


INSTALLERS = {'cmake': install_cmake, 'dotnet': install_dotnet, 'node': install_node,
              'emscripten': install_emscripten, 'libclang': install_libclang}


# ---------------------------------------------------------------------------------------------


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('libraries', nargs='*', metavar='LIBRARY',
                        help='py, js, dotnet, generator or all (default: py js dotnet)')
    parser.add_argument('--install', nargs='?', const='missing', metavar='TOOL',
                        help='install TOOL, or (without TOOL) every tool the check finds missing or too old')
    parser.add_argument('--download', metavar='TOOL', choices=[t for t in INSTALLABLE if t != 'libclang'])
    parser.add_argument('--set', nargs=2, metavar=('TOOL', 'VERSION'), help='only: xcode <version>')
    parser.add_argument('--strict', action='store_true', help='exit 2 when a tool is not the version CI uses')
    args = parser.parse_args()

    unknown = [x for x in args.libraries if x not in LIBRARIES and x != 'all']
    if unknown:
        parser.error(f'unknown library {", ".join(unknown)}; choose from {", ".join(LIBRARIES)}, all')
    records = read_records()

    if args.set:
        tool, version = args.set
        if tool != 'xcode':
            sys.exit('bootstrap.py: --set supports xcode only (the .NET SDK is chosen by global.json)')
        return set_xcode(version)
    if args.download:
        return download(records, args.download)
    if args.install and args.install != 'missing':
        if args.install not in INSTALLERS:
            sys.exit(f'bootstrap.py: cannot install {args.install}; installable: {", ".join(INSTALLABLE)}')
        INSTALLERS[args.install](records)
        return

    libraries = list(LIBRARIES) if 'all' in args.libraries else (args.libraries or ['py', 'js', 'dotnet'])
    tools = []
    for library in libraries:
        tools += [t for t in LIBRARIES[library] if t not in tools]

    report = Report()
    print(f'rhino3dm tools for {", ".join(libraries)} ({SYSTEM} {ARCH})')
    check_records(records, report)
    for tool in tools:
        check_tool(tool, records, report)

    if args.install == 'missing':
        todo = [t for t in tools if t in INSTALLERS and needs_install(t, records)]
        for tool in todo:
            print(f'\ninstalling {tool}')
            INSTALLERS[tool](records)
        if todo:
            print('\nafter installing:')
            report = Report()
            check_records(records, report)
            for tool in tools:
                check_tool(tool, records, report)

    print(f'\n{report.errors} error(s), {report.warnings} warning(s)')
    if report.errors:
        sys.exit(1)
    if args.strict and report.warnings:
        sys.exit(2)


def needs_install(tool, records):
    found = FINDERS[tool]()
    minimum = records.get(f'minimum_{tool}')
    return found is None or found == '0' or (minimum and not at_least(found, minimum))


if __name__ == '__main__':
    main()
