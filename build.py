#!/usr/bin/env python3
"""Build rhino3dm: the Python wheel, rhino3dm.js, and the .NET native library + Rhino3dm.dll.

    python build.py py [--sdist] [--wheel]              wheel (default) and/or sdist into dist/
    python build.py js [--variant V ...] [--all] [--debug] [--minify]
                                                        rhino3dm.js into build/js/dist
    python build.py dotnet [--arch x64|arm64] [--no-wrapper] [--debug]
                                                        native library into build/native/<rid>,
                                                        then Rhino3dm.dll
    python build.py methodgen                           regenerate src/dotnet/AutoNative*.cs

Every native configuration is a preset in CMakePresets.json (build trees in build/<preset>);
this script picks the preset, runs CMake, and gathers the outputs. Check tools with bootstrap.py.
"""

import argparse
import os
import platform
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
BUILD = os.path.join(ROOT, 'build')
SRC = os.path.join(ROOT, 'src')

JS_VARIANTS = ['wasm32', 'wasm32-module', 'wasm64', 'wasm64-module']


def run(cmd, cwd=ROOT, env=None):
    print('+ ' + ' '.join(cmd), flush=True)
    result = subprocess.run(cmd, cwd=cwd, env=env)
    if result.returncode != 0:
        sys.exit(f'build.py: command failed ({result.returncode}): {" ".join(cmd)}')


def cmake(preset, build_dir=None, defines=(), configure_only=False, config='Release', jobs=None):
    """Configure and build one preset. Returns the build directory."""
    build_dir = build_dir or os.path.join(BUILD, preset)
    configure = ['cmake', '--preset', preset, '-B', build_dir] + [f'-D{d}' for d in defines]
    run(configure)
    if not configure_only:
        # a bare --parallel is Make's unlimited -j (every file at once: out of memory on a small
        # machine or container); one job per CPU, or CMAKE_BUILD_PARALLEL_LEVEL when set
        jobs = jobs or os.environ.get('CMAKE_BUILD_PARALLEL_LEVEL') or os.cpu_count() or 1
        build = ['cmake', '--build', build_dir, '--config', config, '--parallel', str(jobs)]
        run(build + ['--target', target_for(preset)])
    return build_dir


def target_for(preset):
    if preset.startswith('js-'):
        return 'rhino3dm'
    if preset.startswith('dotnet-'):
        return 'librhino3dm_native'
    return '_rhino3dm'


def copy(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copy2(src, dst)
    print(f'  {os.path.relpath(src, ROOT)} -> {os.path.relpath(dst, ROOT)}')


# ---------------------------------------------------------------------------------------------
# .NET


def host_rid(arch):
    system = platform.system()
    machine = platform.machine().lower()
    if arch is None:
        arch = 'arm64' if machine in ('arm64', 'aarch64') else 'x64'
    if system == 'Windows':
        return f'win-{arch}', f'dotnet-windows-{arch}'
    if system == 'Darwin':
        return 'osx', 'dotnet-macos'                  # one universal (arm64 + x86_64) dylib
    if system == 'Linux':
        if arch != ('arm64' if machine in ('arm64', 'aarch64') else 'x64'):
            sys.exit('build.py: the Linux native library builds for the host architecture only')
        return f'linux-{arch}', 'dotnet-linux'
    sys.exit(f'build.py: no .NET native preset for {system}')


def methodgen():
    """AutoNativeMethods.cs / AutoNativeEnums.cs from the C layer's exports (gitignored)."""
    out = os.path.join(SRC, 'dotnet', 'AutoNativeMethods.cs')
    if os.path.exists(out):
        os.remove(out)
    run(['dotnet', 'run', '--project', os.path.join(SRC, 'methodgen', 'methodgen.csproj'), '-c', 'Release', '--',
         os.path.join(SRC, 'librhino3dm_native'), os.path.join(SRC, 'dotnet'), '../lib/opennurbs', 'rhino3dm'])
    if not os.path.exists(out):
        sys.exit('build.py: methodgen did not write ' + out)


def build_dotnet(args):
    rid, preset = host_rid(args.arch)
    if not args.library_only:
        methodgen()
    config = 'Debug' if args.debug else 'Release'
    build_dir = cmake(preset, defines=[f'CMAKE_BUILD_TYPE={config}'], configure_only=args.configure_only,
                      config=config)
    if args.configure_only:
        return
    name = {'win': 'librhino3dm_native.dll', 'osx': 'librhino3dm_native.dylib',
            'linux': 'librhino3dm_native.so'}[rid.split('-')[0]]
    native_dir = os.path.join(build_dir, 'src', 'librhino3dm_native')
    built = [os.path.join(native_dir, config, name), os.path.join(native_dir, name)]
    built = next((p for p in built if os.path.exists(p)), None)
    if not built:
        sys.exit(f'build.py: {name} not found under {native_dir}')
    copy(built, os.path.join(BUILD, 'native', rid, name))
    if not args.library_only:
        run(['dotnet', 'build', os.path.join(SRC, 'dotnet', 'Rhino3dm.csproj'), '-c', config])


# ---------------------------------------------------------------------------------------------
# JavaScript


def build_js(args):
    if not os.environ.get('EMSDK'):
        sys.exit('build.py: EMSDK is not set (activate emsdk, or run in the emscripten/emsdk image)')
    variants = JS_VARIANTS if args.all else (args.variant or ['wasm32'])
    dist = os.path.join(BUILD, 'js', 'dist')
    if os.path.isdir(dist):
        shutil.rmtree(dist)
    for variant in variants:
        module = variant.endswith('-module')
        # the ES module is always Release: a debug build keeps Emscripten's environment check,
        # which stops the browser-only module loading in node (the CI smoke test)
        debug = args.debug and not module
        preset = 'js-' + variant
        build_dir = os.path.join(BUILD, preset + ('-debug' if debug else ''))
        cmake(preset, build_dir, defines=['CMAKE_BUILD_TYPE=Debug'] if debug else [],
              configure_only=args.configure_only)
        if args.configure_only:
            continue
        out = os.path.join(build_dir, 'src')
        sub = 'wasm64' if variant.startswith('wasm64') else ''
        js_name = 'rhino3dm.module.js' if module else 'rhino3dm.js'
        copy(os.path.join(out, 'rhino3dm.js'), os.path.join(dist, sub, js_name))
        if not module:
            copy(os.path.join(out, 'rhino3dm.wasm'), os.path.join(dist, sub, 'rhino3dm.wasm'))
        elif args.debug or (variant.replace('-module', '') not in variants):
            # the module's own Release wasm, for when the classic build beside it is a debug build
            # (or absent); a Release classic build writes the same bytes as rhino3dm.wasm
            copy(os.path.join(out, 'rhino3dm.wasm'), os.path.join(dist, sub, 'rhino3dm.module.wasm'))
    if args.configure_only:
        return
    copy(os.path.join(ROOT, 'package.json'), os.path.join(dist, 'package.json'))
    copy(os.path.join(ROOT, 'docs', 'javascript', 'RHINO3DM.JS.md'), os.path.join(dist, 'README.md'))
    copy(os.path.join(SRC, 'js', 'rhino3dm.d.ts'), os.path.join(dist, 'rhino3dm.d.ts'))
    if args.minify:
        uglify = shutil.which('uglifyjs')
        if not uglify:
            sys.exit('build.py: --minify needs uglifyjs (npm install -g uglify-js)')
        for folder, _, files in os.walk(dist):
            for f in files:
                if f in ('rhino3dm.js', 'rhino3dm.module.js'):
                    src = os.path.join(folder, f)
                    run([uglify, src, '--compress', '-o', src[:-3] + '.min.js'])


# ---------------------------------------------------------------------------------------------
# Python


def build_py(args):
    dist = os.path.join(ROOT, 'dist')
    env = dict(os.environ)
    if platform.system() == 'Darwin':
        # universal module and a universal2 wheel tag, as every macOS wheel since 8.x
        env.setdefault('ARCHFLAGS', '-arch arm64 -arch x86_64')
    os.makedirs(BUILD, exist_ok=True)
    # run from build/: in the repository root, `-m build` would import this file instead of PyPA build
    if args.wheel or not args.sdist:
        run([sys.executable, '-m', 'pip', 'wheel', ROOT, '--no-deps', '-w', dist], cwd=BUILD, env=env)
    if args.sdist:
        run([sys.executable, '-m', 'build', '--sdist', '-o', dist, ROOT], cwd=BUILD, env=env)


# ---------------------------------------------------------------------------------------------


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest='command', required=True)

    p = sub.add_parser('py', help='Python wheel (scikit-build-core) into dist/')
    p.add_argument('--sdist', action='store_true', help='the source distribution (needs: pip install build)')
    p.add_argument('--wheel', action='store_true', help='the wheel; the default unless --sdist')
    p.set_defaults(func=build_py)

    p = sub.add_parser('js', help='rhino3dm.js into build/js/dist')
    p.add_argument('--variant', action='append', choices=JS_VARIANTS, help='repeatable; default wasm32')
    p.add_argument('--all', action='store_true', help='every variant, laid out as the npm package')
    p.add_argument('--debug', action='store_true', help='debug build (not the ES module variants)')
    p.add_argument('--minify', action='store_true', help='also write *.min.js (uglifyjs)')
    p.add_argument('--configure-only', action='store_true')
    p.set_defaults(func=build_js)

    p = sub.add_parser('dotnet', help='.NET native library into build/native/<rid>, then Rhino3dm.dll')
    p.add_argument('--arch', choices=['x64', 'arm64'], help='Windows only; default: the host')
    p.add_argument('--library-only', action='store_true', help='native library only: no methodgen, no Rhino3dm.dll')
    p.add_argument('--debug', action='store_true')
    p.add_argument('--configure-only', action='store_true')
    p.set_defaults(func=build_dotnet)

    p = sub.add_parser('methodgen', help='regenerate src/dotnet/AutoNative*.cs')
    p.set_defaults(func=lambda args: methodgen())

    args = parser.parse_args()
    args.func(args)


if __name__ == '__main__':
    main()
