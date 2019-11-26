import os
import re
import sys
import sysconfig
import platform
import subprocess
import glob
import shutil
import struct
import fileinput
from distutils.version import LooseVersion
from setuptools import setup, find_packages, Extension
from setuptools.command.build_ext import build_ext


def system(cmd):
    rv = os.system(cmd)
    rc = rv if os.name == 'nt' else os.WEXITSTATUS(rv)
    if (rc != 0):
        raise RuntimeError('The command "{}" exited with {}'.format(cmd, rc))


class CMakeExtension(Extension):
    def __init__(self, name, sourcedir=''):
        Extension.__init__(self, name, sources=[])
        self.sourcedir = os.path.abspath(sourcedir)


class CMakeBuild(build_ext):
    def run(self):
        try:
            out = subprocess.check_output(['cmake', '--version'])
        except OSError:
            raise RuntimeError(
                "CMake must be installed to build the following extensions: " +
                ", ".join(e.name for e in self.extensions))

        if platform.system() == "Windows":
            cmake_version = LooseVersion(re.search(r'version\s*([\d.]+)',
                                         out.decode()).group(1))
            if cmake_version < '3.1.0':
                raise RuntimeError("CMake >= 3.1.0 is required on Windows")

        for ext in self.extensions:
            self.build_extension(ext)

    def build_extension(self, ext):
        extdir = os.path.abspath(
            os.path.dirname(self.get_ext_fullpath(ext.name)))
        print("extdir = " + extdir)
        print("sourcedir" + ext.sourcedir)

        cmake_args = ['-DCMAKE_LIBRARY_OUTPUT_DIRECTORY=' + extdir,
                      '-DPYTHON_EXECUTABLE:FILEPATH="{}"'.format(sys.executable)]

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
            build_args += ['--', '-j2']

        env = os.environ.copy()
        env['CXXFLAGS'] = '{} -DVERSION_INFO=\\"{}\\"'.format(
            env.get('CXXFLAGS', ''),
            self.distribution.get_version())
        if not os.path.exists(self.build_temp):
            os.makedirs(self.build_temp)

        current_dir = os.getcwd()
        os.chdir(self.build_temp)

        windows_build = os.name == 'nt'

        if windows_build:
            bitness = 8 * struct.calcsize("P")
            command = 'cmake -A {} -DPYTHON_EXECUTABLE:FILEPATH="{}" "{}"'.format("win32" if bitness == 32 else "x64",
                                                                                   sys.executable,
                                                                                   ext.sourcedir+"/src")
            system(command)
            if bitness == 64:
                for line in fileinput.input("_rhino3dm.vcxproj", inplace=1):
                    print(line.replace("WIN32;", "WIN64;"))
                for line in fileinput.input("opennurbs_static.vcxproj", inplace=1):
                    print(line.replace("WIN32;", "WIN64;"))
            system("cmake --build . --config Release --target _rhino3dm")
        else:
            system("cmake -DPYTHON_EXECUTABLE:FILEPATH={} {}".format(sys.executable, ext.sourcedir+"/src"))
            system("make -j4")

        os.chdir(current_dir)
        for file in glob.glob(self.build_temp + "/Release/*.pyd"):
            shutil.copy(file, self.build_lib + "/rhino3dm")
        for file in glob.glob(self.build_temp + "/*.so"):
            shutil.copy(file, self.build_lib + "/rhino3dm")
        print()  # Add an empty line for cleaner output


setup(
    name='rhino3dm',
    version='0.8.1',
    author='Robert McNeel & Associates',
    author_email='steve@mcneel.com',
    description='Python library based on OpenNURBS with a RhinoCommon style',
    long_description=
"""# rhino3dm.py
CPython package based on OpenNURBS with a RhinoCommon style

Project Hompage at: https://github.com/mcneel/rhino3dm

### Supported platforms
* Python27 - Windows (32 and 64 bit)
* Python37 - Windows (32 and 64 bit)
* Python27 - OSX (installed through homebrew)
* Python37 - OSX (installed through homebrew)
* Linux and other python versions are supported through source distributions\


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
    ext_modules=[CMakeExtension('rhino3dm/_rhino3dm')],
    cmdclass=dict(build_ext=CMakeBuild),
    zip_safe=False,
    include_package_data=True
)
