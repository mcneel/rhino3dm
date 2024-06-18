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

        cmake_args = ['cmake',
                      f'-DPYTHON_EXECUTABLE:FILEPATH={pyexec}']

        cfg = 'Debug' if self.debug else 'Release'
        build_args = ['--config', cfg]

        if platform.system() == "Windows":
            cmake_args += ['-DCMAKE_LIBRARY_OUTPUT_DIRECTORY_{}={}'.format(
                cfg.upper(),
                extdir)]
            if sys.maxsize > 2**32:
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
            osplatform = "win32" if bitness == 32 else "x64"


            command = ['cmake', '-A', osplatform, f"{draco_src_dir}"]
            system(command, cwd=draco_static_dir)
            system(["cmake", "--build", ".", "--config", "Release"], cwd=draco_static_dir)

            command = ['cmake', '-A',
                        f"{osplatform}",
                        f'-DPYTHON_EXECUTABLE:FILEPATH={pyexec}',
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
            shutil.copy(file, self.build_lib + "/rhino3dm")
        print()  # Add an empty line for cleaner output


setup(
    name='rhino3dm',
    version='8.9.0-beta',
    author='Robert McNeel & Associates',
    author_email='steve@mcneel.com',
    description='Python library based on OpenNURBS with a RhinoCommon style',
    long_description=
"""# rhino3dm.py
CPython package based on OpenNURBS with a RhinoCommon style

* Project Homepage at: https://github.com/mcneel/rhino3dm
* Developer samples at: https://github.com/mcneel/rhino-developer-samples/tree/8/rhino3dm/py
* Forums at: https://discourse.mcneel.com/c/rhino-developer/rhino3dm/
* Report issue: https://github.com/mcneel/rhino3dm/issues

### Supported platforms
* Python 3.7, 3.8, 3.9, 3.10, 3.11, 3.12 - Windows (32 and 64 bit)
* Python 3.7, 3.8, 3.9, 3.10, 3.11, 3.12 - macos 12 (installed through homebrew)
* Python 3.8, 3.9, 3.10, 3.11, 3.12 - macos 14 arm 64
* Python 3.8, 3.9, 3.10, 3.11 - Linux via manylinux2014_x86_64
* other architectures, operating systems, and python versions are supported through source distributions\

## Test

* start `python`
```
from rhino3dm import *
import requests  # pip install requests

req = requests.get("https://files.mcneel.com/TEST/Rhino Logo.3dm")
model = File3dm.FromByteArray(req.content)
for obj in model.Objects:
    geometry = obj.Geometry
    bbox = geometry.GetBoundingBox()
    print("{}, {}".format(bbox.Min, bbox.Max))
```
""",
    long_description_content_type="text/markdown",
    packages=find_packages('src'),
    package_dir={'': 'src'},
    ext_modules=[CMakeExtension('rhino3dm._rhino3dm')],
    cmdclass=dict(build_ext=CMakeBuild),
    zip_safe=False,
    include_package_data=True
)
