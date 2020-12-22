# build.py
# created: January 15, 2020
#
# This script builds the native (and wrapper) libraries for rhino3dm for the platforms that we target. It requires that 
# the setup script has previously been run in order to generate the project files for the target platform(s).
# See bootstrap script for required tools.  This script cannot be moved from its current location without
# reworking the relative paths that point to the build locations of the platform project files.
#
# See related scripts in this folder for other steps in the process.
#
# This script is inspired by - but deviates from - the "Scripts To Rule Them All" pattern:
# https://github.com/github/scripts-to-rule-them-all

from __future__ import (division, absolute_import, print_function, unicode_literals)

import subprocess
import sys
import os
import argparse
from sys import platform as _platform
import shlex
import shutil
from subprocess import Popen, PIPE
import time
if sys.version_info[0] < 3:
    import imp
else:
    from importlib.machinery import SourceFileLoader
import time
# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
verbose = False
overwrite = False
valid_platform_args = ["windows", "linux", "macos", "ios", "android", "js", "python"]
platform_full_names = {'windows':'Windows', 'linux':'Linux', 'macos': 'macOS', 'ios': 'iOS', 'android': 'Android', 'js': 'JavaScript' }
script_folder = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
src_folder = os.path.abspath(os.path.join(script_folder, "..", "src"))
dotnet_folder = os.path.abspath(os.path.join(src_folder, "dotnet"))
build_folder = os.path.abspath(os.path.join(src_folder, "build"))
docs_folder = os.path.abspath(os.path.join(script_folder, "..", "docs"))
librhino3dm_native_folder = os.path.abspath(os.path.join(src_folder, "librhino3dm_native"))
native_lib_name = 'librhino3dm_native'

if sys.version_info[0] < 3:
    bootstrap = imp.load_source('bootstrap', os.path.join(script_folder, "bootstrap.py"))
else:
    bootstrap = SourceFileLoader('bootstrap', os.path.join(script_folder, "bootstrap.py")).load_module()
# ---------------------------------------------------- Logging ---------------------------------------------------------
# colors for terminal reporting
class bcolors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'


def print_warning_message(warning_message):
    warning_prefix = " warning: "
    if xcode_logging:
        print(warning_prefix + warning_message)
    else:
        print(bcolors.BOLD + bcolors.FAIL + warning_prefix.upper() + bcolors.ENDC + bcolors.FAIL + warning_message +
              bcolors.ENDC)


def print_error_message(error_message):
    error_prefix = " error: "
    if xcode_logging:
        print(error_prefix + error_message)
    else:
        print(bcolors.BOLD + bcolors.FAIL + error_prefix.upper() + bcolors.ENDC + bcolors.FAIL + error_message +
              bcolors.ENDC)


def print_ok_message(ok_message):
    ok_prefix = " ok: "
    if xcode_logging:
        print(ok_prefix + ok_message)
    else:
        print(bcolors.BOLD + bcolors.OKBLUE + ok_prefix.upper() + bcolors.ENDC + bcolors.OKBLUE + ok_message +
              bcolors.ENDC)


# ---------------------------------------------- Platform Build --------------------------------------------------------
def print_platform_preamble(platform_target_name):
    print("")
    if xcode_logging:
        print("Building " + platform_target_name + "...")
    else:
        print(bcolors.BOLD + "Building " + platform_target_name + "..." + bcolors.ENDC)


def run_command(command, suppress_errors=False):
    if suppress_errors == True:                
        dev_null = open(os.devnull, 'w')
        process = subprocess.Popen(shlex.split(command), stdout=subprocess.PIPE, stderr=dev_null)
    else:
        process = subprocess.Popen(shlex.split(command), stdout=subprocess.PIPE, stderr=subprocess.PIPE)    
    
    while True:
        line = process.stdout.readline()               
        if process.poll() is not None:
            break   
        if line:
            if sys.version_info[0] < 3:
                if verbose:
                    print(line.strip())
            else:
                if verbose:
                    line = line.decode('utf-8').strip()
                    print(line)
        elif suppress_errors == False:
            error = process.stderr.readline()                
            if error:
                if sys.version_info[0] < 3:
                    print_error_message(error.strip())
                    delete_cache_file()
                    sys.exit(1)
                else:
                    error = error.decode('utf-8').strip()
                    print_error_message(error)
                    delete_cache_file()
                    sys.exit(1)
            else:
                continue

    rc = process.poll()
    return rc


def check_for_setup_files(item_to_check):
    if not os.path.exists(item_to_check):
        print_error_message(item_to_check + " was not found. Did you run setup.py?")
        return False
    else:
        return True


def overwrite_check(item_to_check):
    if os.path.exists(item_to_check):
        if not overwrite:
            print_warning_message("A build already appears in " + item_to_check + 
                                  ". Use --overwrite to replace.")
            return False
        if overwrite:
            if os.path.isfile(item_to_check):
                os.remove(item_to_check)
            if os.path.isdir(item_to_check):
                shutil.rmtree(item_to_check)
                time.sleep(2) # avoid any race-conditions with large folders
            return True
    else:
        return True


def build_did_succeed(item_to_check):
    if os.path.exists(item_to_check):
        print_ok_message("successfully built: " + item_to_check)
        return True
    else:
        print_error_message("failed to build: " + item_to_check)
        return False


def build_windows():
    if _platform != "win32" and _platform != "win64":
        print_error_message("Building for Windows requires that you run this script on Windows")
        return False

    global native_lib_name
    ext = 'dll'
    native_lib_filename = native_lib_name + '.' + ext

    print(" Building Windows 32-bit native library...")
    target_path = os.path.join(build_folder, platform_full_names.get("windows").lower(), "win32")
    vcxproj_path = os.path.abspath(os.path.join(target_path, native_lib_name + '.vcxproj'))

    if not check_for_setup_files(vcxproj_path):
        return False
    
    item_to_check = os.path.abspath(os.path.join(target_path, "Release", native_lib_filename))    
    if not overwrite_check(item_to_check):
        return False

    os.chdir(target_path)
    
    run_command("cmake --build . --config Release --target librhino3dm_native", False)

    if not build_did_succeed(item_to_check):                
        return False

    print(" Building Windows 64-bit native library...")
    target_path = os.path.join(build_folder, platform_full_names.get("windows").lower(), "win64")
    vcxproj_path = os.path.abspath(os.path.join(target_path, native_lib_name + '.vcxproj'))

    if not check_for_setup_files(vcxproj_path):
        return False
    
    item_to_check = os.path.abspath(os.path.join(target_path, "Release", native_lib_filename))    
    if not overwrite_check(item_to_check):
        return False

    os.chdir(target_path)
    
    run_command("cmake --build . --config Release --target librhino3dm_native", False)

    if not build_did_succeed(item_to_check):                
        return False

    print(" Building Rhino3dm.dll...")
    csproj_path = os.path.abspath(os.path.join(dotnet_folder, "Rhino3dm.csproj")).replace('\\', '//')
    target_path = os.path.join(build_folder, platform_full_names.get("windows").lower())

    command = 'dotnet build ' + csproj_path + ' /p:Configuration=Release'
    rv = run_command(command)

    return rv == 0 # two target frameworks built, so just use the dotnet return value


def build_linux():
    if _platform != "linux" and _platform != "linux2":
        print_error_message("Building for Linux requires that you run this script on Linux")
        return False

    global native_lib_name
    ext = 'so'
    native_lib_filename = native_lib_name + '.' + ext

    target_path = os.path.abspath(os.path.join(build_folder, platform_full_names.get("linux").lower()))
    makefile_path = os.path.abspath(os.path.join(target_path, "Makefile"))
    
    if not check_for_setup_files(makefile_path):
        return False

    item_to_check = os.path.abspath(os.path.join(target_path, native_lib_filename))    
    if not overwrite_check(item_to_check):
        return False

    os.chdir(target_path)

    print(" Building Linux native library...")
    command = 'make'
    run_command(command, True)

    if not build_did_succeed(item_to_check):                
        return False

    print(" Building Rhino3dm.dll...")
    csproj_path = os.path.abspath(os.path.join(dotnet_folder, "Rhino3dm.csproj"))
    target_path = os.path.join(build_folder, platform_full_names.get("linux").lower())
    output_dir = os.path.abspath(os.path.join(target_path, "dotnet"))

    command = 'dotnet build -f netstandard2.0 ' + csproj_path + ' /p:Configuration=Release;OutDir=' + output_dir
    run_command(command)

    item_to_check = os.path.abspath(os.path.join(output_dir, "Rhino3dm.dll"))

    return build_did_succeed(item_to_check)


def build_macos():
    if _platform != "darwin":
        print_error_message("Building for macOS requires that you run this script on macOS")
        return False

    target_path = os.path.abspath(os.path.join(build_folder, platform_full_names.get("macos").lower()))
    global native_lib_name
    ext = 'dylib'
    native_lib_filename = native_lib_name + '.' + ext
    xcodeproj_path = os.path.abspath(os.path.join(target_path, native_lib_name +'.xcodeproj'))

    if not check_for_setup_files(xcodeproj_path):
        return False

    item_to_check = os.path.abspath(os.path.join(target_path, "Release", native_lib_filename))
    if not overwrite_check(item_to_check):
        return False

    command = 'xcodebuild -UseModernBuildSystem=NO -project ' + xcodeproj_path + ' -target ' + native_lib_name + \
              ' -arch x86_64 -configuration Release clean build'
    run_command(command)

    if not build_did_succeed(item_to_check):                
        return False

    print(" Building Rhino3dm.dll...")
    csproj_path = os.path.abspath(os.path.join(dotnet_folder, "Rhino3dm.csproj"))
    output_dir = os.path.abspath(os.path.join(target_path, "dotnet"))

    command = 'dotnet build -f netstandard2.0 ' + csproj_path + ' /p:Configuration=Release;OutDir=' + output_dir
    run_command(command)

    item_to_check = os.path.abspath(os.path.join(output_dir, "Rhino3dm.dll"))

    return build_did_succeed(item_to_check)


def build_ios():
    if _platform != "darwin":
        print_error_message("Building for iOS requires that you run this script on macOS")
        return False

    target_path = os.path.abspath(os.path.join(build_folder, platform_full_names.get("ios").lower()))
    global native_lib_name
    ext = 'a'
    native_lib_filename = native_lib_name + '.' + ext
    xcodeproj_path = os.path.abspath(os.path.join(target_path, native_lib_name + '.xcodeproj'))

    if not check_for_setup_files(xcodeproj_path):
        return False

    item_to_check = os.path.abspath(os.path.join(target_path, "Release", native_lib_filename))
    if not overwrite_check(item_to_check):
        return False

    if not os.path.exists(os.path.join(target_path, "Release")):
        os.mkdir(os.path.join(target_path, "Release"))

    print(" Building x86_64 (Simulator)...")
    command = 'xcodebuild -UseModernBuildSystem=NO -project ' + xcodeproj_path + ' -target ' + native_lib_name + \
             ' -sdk iphonesimulator -arch x86_64 -configuration Release clean build'
    run_command(command)
    if os.path.exists(os.path.join(target_path, "Release-iphonesimulator", native_lib_filename)):
        shutil.move(os.path.join(target_path, "Release-iphonesimulator", native_lib_filename), os.path.join(target_path, "Release", native_lib_name + "-x86_64.a"))
        shutil.rmtree(os.path.join(target_path, "Release-iphonesimulator"))
        print_ok_message("Successfully created x64_86 (Simulator) version.")
    else:
        print_error_message("Failed")
        return False

    print(" Building arm64 version...")
    command = 'xcodebuild -UseModernBuildSystem=NO -project ' + xcodeproj_path + ' -target ' + native_lib_name + \
              ' -sdk iphoneos -arch arm64 -configuration Release clean build'
    run_command(command)
    if os.path.exists(os.path.join(target_path, "Release-iphoneos", native_lib_filename)):
        shutil.move(os.path.join(target_path, "Release-iphoneos", native_lib_filename), os.path.join(target_path, "Release", native_lib_name + "-arm64.a"))
        shutil.rmtree(os.path.join(target_path, "Release-iphoneos"))
        print_ok_message("Successfully created arm64 version.")
    else:
        print_error_message("Failed")
        return False

    print(" Building Universal Binary...")
    command = 'lipo -create -output ' + os.path.join(target_path, "Release", native_lib_filename) + ' ' + os.path.join(target_path, "Release", native_lib_name + "-x86_64.a") + ' ' + os.path.join(target_path, "Release", native_lib_name + "-arm64.a")
    run_command(command)    

    if not build_did_succeed(item_to_check):                
        return False

    print(" Building Rhino3dm.iOS.dll...")
    csproj_path = os.path.abspath(os.path.join(dotnet_folder, "Rhino3dm.iOS.csproj"))
    output_dir = os.path.abspath(os.path.join(target_path, "dotnet"))
    command = 'msbuild ' + csproj_path + ' /p:Configuration=Release;OutDir=' + output_dir
    run_command(command)

    item_to_check = os.path.abspath(os.path.join(output_dir, "Rhino3dm.iOS.dll"))

    return build_did_succeed(item_to_check)


def build_android():
    target_path = os.path.abspath(os.path.join(build_folder, platform_full_names.get("android").lower()))
    global native_lib_name
    ext = 'so'
    native_lib_filename = native_lib_name + '.' + ext

    # check to see if a libs folder exists
    libs_folder_path = os.path.abspath(os.path.join(target_path, "libs"))
    if not overwrite_check(libs_folder_path):
        return False
            
    # CMake builds for a single target per build. To target more than one Android ABI, you must build once per ABI. 
    # It is recommended to use different build directories for each ABI to avoid collisions between builds.
    app_abis = ['armeabi-v7a', 'arm64-v8a', 'x86_64', 'x86']
    for app_abi in app_abis:
        abi_target_path = os.path.join(target_path, app_abi)         
        item_to_check = os.path.abspath(os.path.join(abi_target_path, "Makefile"))
        if not check_for_setup_files(item_to_check):
            return False
        
        # check for a previous build
        item_to_check = os.path.abspath(os.path.join(abi_target_path, native_lib_filename))
        if not overwrite_check(item_to_check):
            return False
                
        print(" Building Android (" + app_abi + ")...")
        os.chdir(abi_target_path)
        run_command("make", True)

        # Check to see if the build succeeded
        if not build_did_succeed(item_to_check):
            return False

    # package it all up the way that Android likes it - in a libs folder - for easier reference into the .csproj
    if not os.path.exists(libs_folder_path):
        os.mkdir(libs_folder_path)

    did_succeed = []
    for app_abi in app_abis:
        libs_abi_path = os.path.abspath(os.path.join(libs_folder_path, app_abi))
        os.mkdir(os.path.join(libs_folder_path, app_abi))
        source = os.path.abspath(os.path.join(target_path, app_abi, native_lib_filename))
        destination = os.path.abspath(os.path.join(libs_abi_path, native_lib_filename))
        shutil.move(source, destination)
        rv = build_did_succeed(destination)
        did_succeed.append(rv)

    if not all(item == True for (item) in did_succeed):                
        return False

    print(" Building Rhino3dm.Android.dll...")
    csproj_path = os.path.abspath(os.path.join(dotnet_folder, "Rhino3dm.Android.csproj"))
    output_dir = os.path.abspath(os.path.join(target_path, "dotnet"))
    run_command(command)

    item_to_check = os.path.abspath(os.path.join(output_dir, "Rhino3dm.Android.dll"))

    return build_did_succeed(item_to_check)


def build_js():
    target_path = os.path.join(build_folder, platform_full_names.get("js").lower())
    item_to_check = os.path.abspath(os.path.join(target_path, "Makefile"))

    if not check_for_setup_files(item_to_check):
        return False

    item_to_check = os.path.abspath(os.path.join(target_path, "artifacts_js"))
    if not overwrite_check(item_to_check):
        return False

    os.chdir(target_path)

    if overwrite:
        try:
            subprocess.Popen(['make', 'clean'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
        except OSError:
            print_error_message("unable to run make clean in " + target_path)
            return False

    # The javascript make build hangs after about 10 lines when outputting stderr the pipe so
    # we'll pass suppress_errors argument as True here...
    run_command("make", True)

    # Check to see if the build succeeded and move into artifacts_js
    items_to_check = ['rhino3dm.wasm', 'rhino3dm.js']
    all_items_built = True
    for item in items_to_check:
        path_to_item = os.path.abspath(os.path.join(target_path, item))
        if not os.path.exists(path_to_item):
            print_error_message("failed to create " + path_to_item)
            all_items_built = False
            break
        else:
            artifacts_folder_path = os.path.abspath(os.path.join(target_path, "artifacts_js"))
            if not os.path.exists(artifacts_folder_path):
                os.mkdir(artifacts_folder_path)
            shutil.copyfile(path_to_item, os.path.abspath(os.path.join(artifacts_folder_path, item)))

    if all_items_built:
        print_ok_message("built target rhino3dm succeeded. see: " + artifacts_folder_path)
    else:
        print_error_message("failed to build all rhino3dm build artifacts.")
        return False
    

def build_handler(platform_target):
    did_succeed = []

    if platform_target == "all":
        for target in valid_platform_args:
            print_platform_preamble(platform_full_names.get(target))
            rv = getattr(sys.modules[__name__], 'build_' + target)()
            did_succeed.append(rv)
    else:
        print_platform_preamble(platform_full_names.get(platform_target))
        rv = getattr(sys.modules[__name__], 'build_' + platform_target)()
        did_succeed.append(rv)

    return all(item == True for (item) in did_succeed)


def delete_cache_file():
    # delete the bootstrapc cache file
    path_to_bootstrapc_file = os.path.join(script_folder, "bootstrap.pyc")
    if os.path.exists(path_to_bootstrapc_file):
        os.remove(path_to_bootstrapc_file)


# --------------------------------------------------- Main -------------------------------------------------------------
def main():
    global valid_platform_args

    # cli metadata
    description = "builds the libraries for rhino3dm"
    epilog = "supported platforms: " + ", ".join(valid_platform_args)

    # Parse arguments
    parser = argparse.ArgumentParser(description=description, epilog=epilog)
    parser.add_argument('--platform', '-p', metavar='<platform>', nargs='+',
                        help="build the native library for the platform(s) specified. valid arguments: all, "
                             + ", ".join(valid_platform_args) + ".")
    parser.add_argument('--overwrite', '-o', action='store_true',
                        help="overwrite existing builds (if found)")
    parser.add_argument('--verbose', '-v', action='store_true',
                        help="show verbose logging messages")
    parser.add_argument('--xcodelog', '-x', action='store_true',
                        help="generate Xcode-compatible log messages (no colors or other Terminal-friendly gimmicks)")
    args = parser.parse_args()

    # User has not entered any arguments...
    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        delete_cache_file()
        sys.exit(1)

    global xcode_logging
    xcode_logging = args.xcodelog

    if _platform == "win32":
        xcode_logging = True

    global verbose
    verbose = args.verbose

    global overwrite
    overwrite = args.overwrite

    # build platform(s)
    did_succeed = []
    if args.platform is not None:
        for platform_target in args.platform:
            if (platform_target != "all") and (platform_target not in valid_platform_args):
                print_error_message(platform_target + " is not a valid platform argument. valid tool arguments: all, "
                                    + ", ".join(valid_platform_args) + ".")
                delete_cache_file()
                sys.exit(1)
            rv = build_handler(platform_target)
            did_succeed.append(rv)

    delete_cache_file()

    sys.exit(0) if all(item == True for (item) in did_succeed) else sys.exit(1)


if __name__ == "__main__":
    main()
