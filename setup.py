import os
import re
import sys
import sysconfig
import platform
import subprocess as sp
import glob
import shutil
import struct
import fileinput
from typing import List
from pathlib import Path
from distutils.version import LooseVersion
from setuptools import setup, find_packages, Extension
from setuptools.command.build_ext import build_ext

pyexec = Path(sys.executable).resolve()

print(f"Using Python executable: {pyexec}")

def system(cmd : List, **kwargs):
    print(f"Running {' '.join(cmd)}")
    if "cwd" in kwargs.keys():
        print(f"Using 'cwd'={kwargs['cwd']}")
    try:
        cmd_out = sp.run(cmd, encoding='utf-8', check=True, stdout=sp.PIPE, stderr=sp.PIPE, **kwargs)
    except sp.CalledProcessError as e:
        print(e.stderr)
        print(e.output)
        raise RuntimeError(f"Command execution failed.\n\n{e.stderr}\n\n{e.output}")
    print(cmd_out.stdout)



class CMakeExtension(Extension):
    def __init__(self, name, sourcedir=''):
        Extension.__init__(self, name, sources=[])
        self.sourcedir = os.path.abspath(sourcedir)


class CMakeBuild(build_ext):
    def run(self):
        try:
            out = sp.check_output(['cmake', '--version'])
        except OSError:
            raise RuntimeError(
                "CMake must be installed to build the following extensions: " +
                ", ".join(e.name for e in self.extensions))

        if platform.system() == "Windows":
            cmake_version = LooseVersion(re.search(r'version\s*([\d.]+)',
                                         out.decode()).group(1))
            if cmake_version < '3.21.0':
                raise RuntimeError("CMake >= 3.21.0 is required on Windows")

        for ext in self.extensions:
            self.build_extension(ext)

        if self.inplace:
            self.copy_extensions_to_source()

    def build_extension(self, ext):
        extdir = os.path.abspath(
            os.path.dirname(self.get_ext_fullpath(ext.name)))
        print("extdir = " + extdir)
        print("sourcedir" + ext.sourcedir)

        # nanobind (M2) finds Python with CMake's FindPython: point it at the interpreter running setup.py
        cmake_args = ['cmake', f'-DPython_EXECUTABLE:FILEPATH={pyexec}', f'-DCMAKE_VERBOSE_MAKEFILE:BOOL=ON']

        cfg = 'Debug' if self.debug else 'Release'
        #cfg = 'Debug'
        build_args = ['--config', cfg]

        if platform.system() == "Windows":
            cmake_args += ['-DCMAKE_LIBRARY_OUTPUT_DIRECTORY_{}={}'.format(
                cfg.upper(),
                extdir)]
            if platform.machine().upper() == 'ARM64':
                cmake_args += ['-A', 'ARM64']
            elif sys.maxsize > 2**32:
                cmake_args += ['-A', 'x64']
            build_args += ['--', '/m']
        else:
            cmake_args += ['-DCMAKE_BUILD_TYPE=' + cfg]
            if platform.system() == "Darwin":
                cmake_args += ['-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64']
            build_args += ['--', f'-j{max(1,os.cpu_count()-1)}']

        env = os.environ.copy()
        env['CXXFLAGS'] = '{} -DVERSION_INFO=\\"{}\\"'.format(
            env.get('CXXFLAGS', ''),
            self.distribution.get_version())
        build_dir = Path(self.build_temp).resolve()
        build_dir.mkdir(parents=True, exist_ok=True)
        draco_static_dir = build_dir / "draco_static"
        draco_static_dir.mkdir(parents=True, exist_ok=True)
        draco_src_dir = Path(ext.sourcedir) / "src" / "lib" / "draco"
        build_temp_dir = Path(self.build_temp).resolve()
        src_dir = Path(ext.sourcedir).resolve() / "src"

        current_dir = Path.cwd()

        if os.name == 'nt':  # windows
            bitness = 8 * struct.calcsize("P")
            # RH3DM-205: pick the MSVC target platform from the interpreter's own architecture.
            # A native ARM64 CPython (windows-11-arm) reports platform.machine() == 'ARM64'; an
            # x64 CPython running emulated on ARM reports 'AMD64' and correctly gets x64.
            if platform.machine().upper() == "ARM64":
                osplatform = "ARM64"
            else:
                osplatform = "win32" if bitness == 32 else "x64"


            command = ['cmake', '-A', osplatform, f"{draco_src_dir}"]
            system(command, cwd=draco_static_dir)
            system(["cmake", "--build", ".", "--config", "Release"], cwd=draco_static_dir)

            command = ['cmake', '-A',
                        f"{osplatform}",
                        f'-DPython_EXECUTABLE:FILEPATH={pyexec}',
                        ext.sourcedir+"/src"]
            system(command, cwd=build_temp_dir)
            if bitness == 64:
                _rhino3dmvcxproj = build_temp_dir / "_rhino3dm.vcxproj"
                opennurbs_staticvcxproj = build_temp_dir / "opennurbs_static.vcxproj"
                _rhino3dmvcxproj.write_text(_rhino3dmvcxproj.read_text().replace("WIN32;", "WIN64;"))
                opennurbs_staticvcxproj.write_text(opennurbs_staticvcxproj.read_text().replace("WIN32;", "WIN64;"))
                system(["cmake","--build",".", "--config","Release","--target","_rhino3dm"], cwd=build_temp_dir)
        else:
            # first build draco
            system(cmake_args + [f"{draco_src_dir}"], cwd=draco_static_dir)
            system(["cmake", "--build", "."] + build_args, cwd=draco_static_dir)

            # then build rhino3dm
            system(cmake_args + [f"{src_dir}"], cwd=build_temp_dir)
            system(["cmake", "--build", "."] + build_args, cwd=build_temp_dir)

        os.chdir(current_dir)
        if not os.path.exists(self.build_lib + "/rhino3dm"):
            os.makedirs(self.build_lib + "/rhino3dm")
        for file in glob.glob(self.build_temp + "/Release/*.pyd"):
            shutil.copy(file, self.build_lib + "/rhino3dm")
        for file in glob.glob(self.build_temp + "/*.so"):
            if platform.system() == "Linux" and self.debug:
                print("debug linux")
                system(["stat", file])
                system(["strip", "--strip-unneeded", file])
                system(["stat", file])
            shutil.copy(file, self.build_lib + "/rhino3dm")
        print()  # Add an empty line for cleaner output


setup(
    name='rhino3dm',
    version='9.2.0b0',
    python_requires='>=3.10',
    author='Robert McNeel & Associates',
    author_email='steve@mcneel.com',
    license="MIT",
    classifiers=[
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Programming Language :: Python :: 3.13",
        "Programming Language :: Python :: 3.14",
        "Operating System :: Microsoft :: Windows",
        "Operating System :: MacOS",
        "Operating System :: POSIX :: Linux",
        "Topic :: Multimedia :: Graphics :: 3D Modeling",
        "Topic :: Scientific/Engineering",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "Typing :: Typed",
    ],
    keywords=["rhino", "3dm", "opennurbs", "nurbs", "cad", "3d", "geometry", "rhinocommon"],
    project_urls={
        "Documentation": "https://mcneel.github.io/rhino3dm/python/api/index.html",
        "Migration guide": "https://mcneel.github.io/rhino3dm/python/api/migrating.html",
        "Source": "https://github.com/mcneel/rhino3dm",
        "Issues": "https://github.com/mcneel/rhino3dm/issues",
        "Changelog": "https://github.com/mcneel/rhino3dm/blob/main/CHANGELOG.md",
        "Forum": "https://discourse.mcneel.com/c/rhino-developer/rhino3dm/",
    },
    description='Python library based on OpenNURBS with a RhinoCommon style',
    long_description=
"""# rhino3dm

**Read, write and create Rhino 3D models (`.3dm` files) in Python.**

rhino3dm brings [openNURBS](https://www.rhino3d.com/opennurbs), the library behind Rhino's file
format, to Python with [RhinoCommon](https://developer.rhino3d.com/api/rhinocommon/)'s API (Rhino's
.NET SDK). Use it in scripts, notebooks and web services on Windows, macOS and Linux.

## Coming from rhino3dm 8.x?

Most 8.x scripts need changes. For example, editing an object's geometry no longer changes the file,
`out` parameters come back as a tuple with the success flag first, and bad arguments raise.

* [Migrating to rhino3dm 9.2](https://mcneel.github.io/rhino3dm/python/api/migrating.html): what
  changed, with before-and-after code
* [Renamed and removed members](https://mcneel.github.io/rhino3dm/python/api/migrating-members.html):
  every 8.x name and what replaces it

## Install

```
pip install rhino3dm
```

Pre-releases, such as betas, install with `pip install --pre rhino3dm`.

## Example

```python
import rhino3dm

# build a model
model = rhino3dm.File3dm()
model.Objects.AddSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5))
model.Objects.AddCircle(rhino3dm.Circle(rhino3dm.Point3d(20, 0, 0), 3))
model.Objects.AddPoint(rhino3dm.Point3d(10, 10, 10))
model.Write("example.3dm", 0)

# read it back
model = rhino3dm.File3dm.Read("example.3dm")
for obj in model.Objects:
    geometry = obj.Geometry
    bbox = geometry.GetBoundingBox(True)
    print(geometry.ObjectType.name, bbox.Min, bbox.Max)

# Surface -5,-5,-5 5,5,5
# Curve 17,-3,0 23,3,0
# Point 10,10,10 10,10,10
```

## Features

* **Files**: read `.3dm` files from any Rhino version and write new ones, on disk or in memory.
* **Geometry**: points, curves, NURBS curves and surfaces, breps, extrusions, meshes, SubD,
  point clouds, text and dimensions, with RhinoCommon's members.
* **Model content**: layers, materials, linetypes, groups, blocks, views, user strings and
  embedded files.
* **Typed**: the package ships type hints (`__init__.pyi`) with documentation for every member.

## Documentation

* [API reference](https://mcneel.github.io/rhino3dm/python/api/index.html)
* [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/): rhino3dm follows its
  names and behaviour
* [Samples](https://github.com/mcneel/rhino-developer-samples/tree/8/rhino3dm/py)

## Platforms

| | Python 3.10 to 3.14 |
|---|---|
| Windows | x64, Arm64 |
| macOS 14 or later | universal (Apple silicon and Intel) |
| Linux | x86_64, aarch64 (manylinux_2_28) |

Anywhere else, pip builds the package from source, which needs CMake and a C++17 compiler.

## Also for JavaScript and .NET

rhino3dm is also available as [rhino3dm.js](https://www.npmjs.com/package/rhino3dm) (WebAssembly,
for browsers and Node.js) and [Rhino3dm](https://www.nuget.org/packages/Rhino3dm) for .NET.

## Help

Ask on the [forum](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/), and report bugs on
[GitHub](https://github.com/mcneel/rhino3dm/issues).
""",
    long_description_content_type="text/markdown",
    packages=find_packages('src'),
    package_dir={'': 'src'},
    ext_modules=[CMakeExtension('rhino3dm._rhino3dm')],
    cmdclass=dict(build_ext=CMakeBuild),
    zip_safe=False,
    include_package_data=True
)
