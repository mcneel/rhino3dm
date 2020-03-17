# setup.py
# created: January 15, 2020
#
# Uses the CMake (https://cmake.org) tools to generate the platform-specific rhino3dm projects
# See related scripts in this folder for other steps in the process.
#
# This script is inspired by - but deviates from - the "Scripts To Rule Them All" pattern:
# https://github.com/github/scripts-to-rule-them-all

import subprocess
import sys
import os
import argparse
from sys import platform as _platform
from subprocess import Popen, PIPE
import shlex
import shutil
import imp

# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
verbose = False
overwrite = False
valid_platform_args = ["js", "ios", "macos", "android"]
platform_full_names = {'js': 'JavaScript', 'ios': 'iOS', 'macos': 'macOS', 'android': 'Android'}
script_folder = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
src_folder = os.path.abspath(os.path.join(script_folder, "..", "src"))
build_folder = os.path.abspath(os.path.join(script_folder, "..", "build"))
path_to_this_file = os.path.realpath(__file__)
path_to_scripts_folder = os.path.dirname(path_to_this_file)

bootstrap = imp.load_source('bootstrap', os.path.join(path_to_scripts_folder, "bootstrap.py"))

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


def run_command(command, suppress_errors=False):
    verbose = True #we don't yet have a command-line switch for this, if we ever need one.
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
                    sys.exit(1)
                else:
                    error = error.decode('utf-8').strip()
                    print_error_message(error)
                    sys.exit(1)
            else:
                continue

    rc = process.poll()
    return rc


# ---------------------------------------------- Platform Setup --------------------------------------------------------
def print_platform_preamble(platform_target_name):
    print("")
    if xcode_logging:
        print("Setting up " + platform_target_name + "...")
    else:
        print(bcolors.BOLD + "Setting up " + platform_target_name + "..." + bcolors.ENDC)


def build_methodgen():
    if xcode_logging:
        print(" Building MethodGen...")
    else:
        print(bcolors.BOLD + " Building MethodGen..." + bcolors.ENDC)

    path_to_methodgen_csproj = os.path.abspath(os.path.join(src_folder, 'methodgen', 'methodgen.csproj'))

    command = 'msbuild ' + path_to_methodgen_csproj +' /p:Configuration=Release'
    run_command(command)

    # Check to see if the MethodGen.exe was written...
    item_to_check = os.path.abspath(os.path.join(src_folder, 'MethodGen.exe'))
    if os.path.exists(item_to_check):
        print_ok_message("successfully built: " + item_to_check)
    else:
        print_error_message("failed to build " + item_to_check + " for macOS build")
        return False

    return True


def run_methodgen():
    if xcode_logging:
        print(" Running MethodGen...")
    else:
        print(bcolors.BOLD + " Running MethodGen..." + bcolors.ENDC)

    path_to_methodgen_exe = os.path.abspath(os.path.join(src_folder, "MethodGen.exe"))

    if not os.path.exists(path_to_methodgen_exe):
        print_error_message("MethodGen.exe not found.")
        return False

    command = ''
    if _platform == "darwin":
        command = command + 'mono '

    path_to_cpp = os.path.abspath(os.path.join(src_folder, 'librhino3dmio_native'))
    path_to_cs = os.path.abspath(os.path.join(src_folder, 'dotnet'))
    path_to_replace = '../lib/opennurbs'
    item_to_check = os.path.abspath(os.path.join(path_to_cs, 'AutoNativeMethods.cs'))

    # remove any older file there...
    if os.path.exists(item_to_check):
        os.remove(item_to_check)

    command = command + path_to_methodgen_exe + " " + path_to_cpp + " " + path_to_cs + " " + path_to_replace
    run_command(command)

    # Check to see if methodgen succeeded
    if os.path.exists(item_to_check):
        print_ok_message("successfully generated: " + item_to_check)
    else:
        print_error_message("failed to generate " + item_to_check + " for macOS build")
        return False


def setup_macos():
    if _platform != "darwin":
        print_error_message("Generating project file for macOS requires that you run this script on macOS")
        return False

    platform_target_path = os.path.join(build_folder, platform_full_names.get("macos").lower())

    target_file_name = "librhino3dmio_native.xcodeproj"

    item_to_check = os.path.abspath(os.path.join(platform_target_path, target_file_name))
    if os.path.exists(item_to_check):
        if not overwrite:
            print_warning_message("A configuration already appears in " + item_to_check + ". Use --overwrite to replace.")
            return False
        if overwrite:
            shutil.rmtree(platform_target_path)

    if not os.path.exists(platform_target_path):
        os.mkdir(platform_target_path)

    os.chdir(platform_target_path)

    # methogen
    build_methodgen()
    run_methodgen()

    # generate the project files
    print("")
    if xcode_logging:
        print("Generating xcodeproj files for macOS...")
    else:
        print(bcolors.BOLD + "Generating xcodeproj files for macOS..." + bcolors.ENDC)

    command = "cmake -G \"Xcode\" -DMACOS_BUILD=1 ../../src/librhino3dmio_native"
    run_command(command)

    # Check to see if the CMakeFiles were written...
    if os.path.exists(item_to_check):
        print_ok_message("successfully wrote: " + item_to_check)
    else:
        print_error_message("failed to configure and generate " + target_file_name + " for macOS build")

    os.chdir(script_folder)


def setup_ios():
    platform_target_path = os.path.join(build_folder, platform_full_names.get("ios").lower())

    target_file_name = "librhino3dmio_native.xcodeproj"

    item_to_check = os.path.abspath(os.path.join(platform_target_path, target_file_name))
    if os.path.exists(item_to_check):
        if not overwrite:
            print_warning_message("A configuration already appears in " + item_to_check + ". Use --overwrite to replace.")
            return False
        if overwrite:
            shutil.rmtree(platform_target_path)

    if not os.path.exists(platform_target_path):
        os.mkdir(platform_target_path)

    os.chdir(platform_target_path)

    # methogen
    build_methodgen()
    run_methodgen()

    # generate the project files
    print("")
    if xcode_logging:
        print("Generating xcodeproj files for iOS...")
    else:
        print(bcolors.BOLD + "Generating xcodeproj files for iOS..." + bcolors.ENDC)
    command = "cmake -G \"Xcode\" -DCMAKE_TOOLCHAIN_FILE=../../src/ios.toolchain.cmake -DPLATFORM=OS64COMBINED -DDEPLOYMENT_TARGET=9.3 ../../src/librhino3dmio_native"
    run_command(command)

    # Check to see if the CMakeFiles were written...
    if os.path.exists(item_to_check):
        print_ok_message("successfully wrote: " + item_to_check)
    else:
        print_error_message("failed to configure and generate " + target_file_name + " for iOS build")

    os.chdir(script_folder)


def setup_js():
    platform_target_path = os.path.join(build_folder, platform_full_names.get("js").lower())

    item_to_check = os.path.abspath(os.path.join(platform_target_path, "CMakeFiles"))
    if os.path.exists(item_to_check):
        if not overwrite:
            print_warning_message("CMakeFiles already appear in " + item_to_check + ". Use --overwrite to replace.")
            return False
        if overwrite:
            shutil.rmtree(platform_target_path)

    if not os.path.exists(platform_target_path):
        os.mkdir(platform_target_path)

    os.chdir(platform_target_path)

    command = "emcmake cmake -DCMAKE_CXX_FLAGS=\"-s MODULARIZE=1 -s 'EXPORT_NAME=\\\"rhino3dm\\\"'\" ../../src"
    try:
        p = subprocess.Popen(shlex.split(command), stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message("could not find emcmake command.  Run the bootstrap.py --check emscripten")
        return False

    if sys.version_info[0] < 3:
        output = p.communicate()[0]
        if output:
            if verbose:
                print(output)
        else:
            print_error_message("failed to run emcmake cmake.")
    else:
        output, err = p.communicate()
        output = output.decode('utf-8')
        err = err.decode('utf-8')
        if output:
            if verbose:
                print(output)
        elif err:
            print_error_message(err)

    # Check to see if the CMakeFiles were written...
    if os.path.exists(item_to_check):
        print_ok_message("make files have been written to: " + platform_target_path)
    else:
        print_error_message("failed to configure and generate CMakeFiles for JavaScript build")

    os.chdir(script_folder)


def setup_android():
    # https://developer.android.com/ndk/guides/cmake.html
    # The Android toolchain file is in: <NDK>/build/cmake/android.toolchain.cmake
    # We need to call the bootstrap script to figure out which ndk is currently in use, in order
    # to set the ndk path
    build_tools = bootstrap.read_required_versions()
    android_ndk_path = bootstrap.check_ndk(build_tools["ndk"])
    android_toolchain_path = os.path.join(android_ndk_path, "build", "cmake", "android.toolchain.cmake")
    
    # setup the build folders and clean previous builds if necessary...
    # TODO: CMake builds for a single target per build. To target more than one Android ABI, you must build once per ABI. 
    # It is recommended to use different build directories for each ABI to avoid collisions between builds.
    platform_target_path = os.path.join(build_folder, platform_full_names.get("android").lower())
    item_to_check = os.path.abspath(os.path.join(platform_target_path, "CMakeFiles"))
    if os.path.exists(item_to_check):
        if not overwrite:
            print_warning_message("CMakeFiles already appear in " + item_to_check + ". Use --overwrite to replace.")
            return False
        if overwrite:
            shutil.rmtree(platform_target_path)

    if not os.path.exists(platform_target_path):
        os.mkdir(platform_target_path)

    os.chdir(platform_target_path)

    # methogen
    build_methodgen()
    run_methodgen()

    print("")
    if xcode_logging:
        print("Generating Makefiles files for Android...")
    else:
        print(bcolors.BOLD + "Generating Makefiles files for Android..." + bcolors.ENDC)
    
    command = "cmake -DCMAKE_TOOLCHAIN_FILE=" + android_toolchain_path + " -DANDROID_ABI=armeabi-v7a -DANDROID_PLATFORM=android-24 -DCMAKE_ANDROID_STL_TYPE=c++_static ../../src/librhino3dmio_native"
    run_command(command)

    #TODO: It's still producing a .a file, when I believe these need to be static-object (so) files

    # Check to see if the CMakeFiles were written...
    if os.path.exists(item_to_check):
        print_ok_message("successfully wrote: " + item_to_check)
    else:
        print_error_message("failed to configure and generate " + target_file_name + " for iOS build")

    os.chdir(script_folder)


def setup_handler(platform_target):
    if not os.path.exists(build_folder):
        os.mkdir(build_folder)

    if platform_target == "all":
        for target in valid_platform_args:
            print_platform_preamble(platform_full_names.get(target))
            getattr(sys.modules[__name__], 'setup_' + target)()
    else:
        print_platform_preamble(platform_full_names.get(platform_target))
        getattr(sys.modules[__name__], 'setup_' + platform_target)()


# --------------------------------------------------- Main -------------------------------------------------------------
def main():
    global valid_platform_args

    # cli metadata
    description = "generate the project files for rhino3dm"
    epilog = "supported platforms: " + ", ".join(valid_platform_args)

    # Parse arguments
    parser = argparse.ArgumentParser(description=description, epilog=epilog)
    parser.add_argument('--platform', '-p', metavar='<platform>', nargs='+',
                        help="generates the project files for the platform(s) specified. valid arguments: all, "
                             + ", ".join(valid_platform_args) + ".")
    parser.add_argument('--verbose', '-v', action='store_true',
                        help="show verbose logging messages")
    parser.add_argument('--overwrite', '-o', action='store_true',
                        help="overwrite existing configurations (if found)")
    parser.add_argument('--xcodelog', '-x', action='store_true',
                        help="generate Xcode-compatible log messages (no colors or other Terminal-friendly gimmicks)")
    args = parser.parse_args()

    # User has not entered any arguments...
    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    global xcode_logging
    xcode_logging = args.xcodelog

    if _platform == "win32":
        xcode_logging = True

    global verbose
    verbose = args.verbose

    global overwrite
    overwrite = args.overwrite

    os.chdir(script_folder)

    # setup platform(s)
    if args.platform is not None:
        for platform_target in args.platform:
            if (platform_target != "all") and (platform_target not in valid_platform_args):
                print_error_message(platform_target + " is not a valid platform argument. valid tool arguments: all, "
                                    + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            setup_handler(platform_target)


if __name__ == "__main__":
    main()


