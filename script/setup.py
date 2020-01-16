# setup.py
# created: January 15, 2020
#
# Uses the CMake (https://cmake.org) tools to generate the platform-specific rhino3dm projects
#
# This script uses the "Scripts To Rule Them All" pattern: https://github.com/github/scripts-to-rule-them-all

import subprocess
import sys
import os
import argparse
from sys import platform as _platform
from subprocess import Popen, PIPE
import shlex
import shutil

# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
verbose = False
overwrite = False
valid_platform_args = ["js"]
platform_full_names = {'js': 'JavaScript', 'ios': 'iOS'}
script_folder = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
build_folder = os.path.abspath(os.path.join(script_folder, "..", "build"))

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


# ---------------------------------------------- Platform Setup --------------------------------------------------------
def print_platform_preamble(platform_target_name):
    print("")
    if xcode_logging:
        print("Setting up " + platform_target_name + "...")
    else:
        print(bcolors.BOLD + "Setting up " + platform_target_name + "..." + bcolors.ENDC)


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
        print_ok_message("build files have been written to: " + platform_target_path)
    else:
        print_error_message("failed to configure and generate CMakeFiles for JavaScript build")

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


