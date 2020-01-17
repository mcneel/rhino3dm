# build.py
# created: January 15, 2020
#
# This script builds the native library for rhino3dm for the platforms that we target. It requires that the setup
# script has previously been run in order to generate the project files for the the specific platform(s).
# See bootstrap script for required tools.  This script cannot be moved from its current location without
# reworking the relative paths that point to the build locations of the platform project files.
#
# This script uses the "Scripts To Rule Them All" pattern: https://github.com/github/scripts-to-rule-them-all

from __future__ import (division, absolute_import, print_function, unicode_literals)

import subprocess
import sys
import os
import argparse
import platform
from sys import platform as _platform
import shlex
import shutil
from subprocess import Popen, PIPE

# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
verbose = False
overwrite = False
valid_platform_args = ["js"]
platform_full_names = {'js': 'JavaScript', 'ios': 'iOS'}
script_folder = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
build_folder = os.path.abspath(os.path.join(script_folder, "..", "build"))
docs_folder = os.path.abspath(os.path.join(script_folder, "..", "docs"))

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


def run_command_show_output(command):
    if verbose:
        process = subprocess.Popen(shlex.split(command), stdout=subprocess.PIPE)
    else:
        dev_null = open(os.devnull, 'w')  # sending to dev/null here because sending everything to pipe causes hang
        process = subprocess.Popen(shlex.split(command), stdout=subprocess.PIPE, stderr=dev_null)
    while True:
        output = process.stdout.readline()
        if not output:
            break
        if len(output) == 0 and process.poll() is not None:
            break
        if output:
            if sys.version_info[0] < 3:
                print(output.strip())
            else:
                output = output.decode('utf-8').strip()
                print(output)

    rc = process.poll()
    return rc


def build_js():
    platform_target_path = os.path.join(build_folder, platform_full_names.get("js").lower())

    previous_build = os.path.abspath(os.path.join(platform_target_path, "artifacts_js"))
    if os.path.exists(previous_build):
        if not overwrite:
            print_warning_message("build already appears in " + previous_build + ". Use --overwrite to replace.")
            return False
        if overwrite:
            shutil.rmtree(previous_build)

    item_to_check = os.path.abspath(os.path.join(platform_target_path, "CMakeFiles"))
    if not os.path.exists(item_to_check):
        print_error_message("CMakeFiles not found in " + item_to_check + ". Did you run setup.py?")
        return False

    os.chdir(platform_target_path)

    if overwrite:
        try:
            subprocess.Popen(['make', 'clean'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
        except OSError:
            print_error_message("unable to run make clean in " + platform_target_path)
            return False

    run_command_show_output("make")

    # Check to see if the build succeeded and move into artifacts_js
    items_to_check = ['rhino3dm.wasm', 'rhino3dm.js']
    all_items_built = True
    for item in items_to_check:
        path_to_item = os.path.abspath(os.path.join(platform_target_path, item))
        if not os.path.exists(path_to_item):
            print_error_message("failed to create " + path_to_item)
            all_items_built = False
        else:
            artifacts_folder_path = os.path.abspath(os.path.join(platform_target_path, "artifacts_js"))
            if not os.path.exists(artifacts_folder_path):
                os.mkdir(artifacts_folder_path)
            shutil.move(path_to_item, os.path.abspath(os.path.join(artifacts_folder_path, item)))

    if all_items_built:
        print_ok_message("built target rhino3dm succeeded. see: " + artifacts_folder_path)
    else:
        print_error_message("failed to build all rhino3dm build artifacts.")
        return False

    # Copy artifacts into samples folder
    for item in items_to_check:
        artifacts_folder_path = os.path.abspath(os.path.join(platform_target_path, "artifacts_js"))
        path_to_item = os.path.abspath(os.path.join(artifacts_folder_path, item))
        if os.path.exists(path_to_item):
            resources_path = os.path.abspath(os.path.join(docs_folder, platform_full_names.get("js").lower(),
                                                          "samples", "resources"))
            destination_path = os.path.abspath(os.path.join(resources_path, item))
            shutil.copyfile(path_to_item, destination_path)
            if os.path.exists(destination_path):
                print_ok_message("copied " + item + " to: " + destination_path)

    os.chdir(script_folder)


def build_handler(platform_target):
    if platform_target == "all":
        for target in valid_platform_args:
            print_platform_preamble(platform_full_names.get(target))
            getattr(sys.modules[__name__], 'build_' + target)()
    else:
        print_platform_preamble(platform_full_names.get(platform_target))
        getattr(sys.modules[__name__], 'build_' + platform_target)()

    os.chdir(script_folder)


# --------------------------------------------------- Main -------------------------------------------------------------
def main():
    global valid_platform_args

    # cli metadata
    description = "builds the native libraries for rhino3dm"
    epilog = ""

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
    if args.platform is not None:
        for platform_target in args.platform:
            if (platform_target != "all") and (platform_target not in valid_platform_args):
                print_error_message(platform_target + " is not a valid platform argument. valid tool arguments: all, "
                                    + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            build_handler(platform_target)


if __name__ == "__main__":
    main()
