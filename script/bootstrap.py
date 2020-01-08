# bootstrap.py
# created: January 8, 2020
#
# This script checks for the necessary tools to construct and build the rhino3dm native and wrapper libraries
# for all supported platforms.  See related scripts in this folder for other steps in the process.
# This script uses the "Scripts To Rule Them All" pattern: https://github.com/github/scripts-to-rule-them-all

# ---------------------------------------------------- Imports ---------------------------------------------------------

import subprocess
import sys
import argparse
import os
import platform
import urllib
from subprocess import Popen, PIPE
from sys import platform as _platform

# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
valid_platform_args = ["js"]
#TODO:"android", "ios", "dotnet", "linux", "macos", "python", "windows"

class BuildTool:
    def __init__(self, name, abbr, currently_using, archive_url, install_notes):
        self.name = name
        self.abbr = abbr
        self.currently_using = currently_using
        self.archive_url = archive_url
        self.install_notes = install_notes


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


# ------------------------------------------------- Versions -----------------------------------------------------------

def normalize_version(v):
    parts = [int(x) for x in v.split(".")]
    while parts[-1] == 0:
        parts.pop()
    return parts


def compare_versions(v1, v2):
    a = normalize_version(v1)
    b = normalize_version(v2)
    return (a > b) - (a < b)


def read_required_versions():
    # check to make sure that the Current Development Tools.md file exists, exit with error if not
    script_folder = os.getcwd()
    current_development_tools_file_path = os.path.join(script_folder, '..', 'Current Development Tools.md')
    if not os.path.exists(current_development_tools_file_path):
        print_error_message("Could not find the Current Development Tools.md (rhino3dm) file listing our "
                            "current development tools.\n This file should be in: " +
                            current_development_tools_file_path + "\n Exiting script.")
        sys.exit(1)

    # Shared
    macos = BuildTool("macOS", "macos", "", "", "")
    xcode = BuildTool("Xcode", "xcode", "", "", "")
    git = BuildTool("Git", "git", "", "", "")
    python = BuildTool("Python", "python", "", "", "")
    cmake = BuildTool("CMake", "cmake", "", "", "")

    # Javascript
    emscripten = BuildTool("Emscripten", "emscripten", "", "", "")

    # Android
    #TODO: vs = BuildTool("Visual Studio for Mac", "vs", "", "", "")
    #TODO: dotnet = BuildTool(".NET SDK", "dotnet", "", "", "")
    #TODO: msbuild = BuildTool("msbuild", "msbuild", "", "", "")
    #TODO: mdk = BuildTool("Mono MDK", "mdk", "", "", "")
    #TODO: ndk = BuildTool("Android NDK", "ndk", "", "", "")
    #TODO: xamandroid = BuildTool("Xamarin.Android", "xamandroid", "", "", "")

    # iOS
    #TODO: xamios = BuildTool("Xamarin.iOS", "xamios", "", "")

    build_tools = dict(macos=macos, xcode=xcode, git=git, python=python, cmake=cmake, emscripten=emscripten)

    # open and read Current Development Tools.md and load required versions
    current_development_tools_file = open(current_development_tools_file_path, "r")
    for line in current_development_tools_file:
        for tool in build_tools:
            ver_prefix = tool + "_currently_using = "

            archive_prefix = tool + "_archive_url"
            archive_suffix = ''
            if _platform == "win32":
                archive_suffix = "_windows = "
            if _platform == "darwin":
                archive_suffix = "_macos = "
            archive_string = archive_prefix + " = "
            archive_string_with_platform = archive_prefix + archive_suffix

            install_notes_prefix = tool + "_install_notes"
            install_notes_suffix = ''
            if _platform == "win32":
                install_notes_suffix = "_windows = "
            if _platform == "darwin":
                install_notes_suffix = "_macos = "
            install_notes_string = install_notes_prefix + " = "
            install_notes_string_with_platform = install_notes_prefix + install_notes_suffix

            if ver_prefix in line:
                build_tools[str(tool)].currently_using = line.split('= ', 1)[1].split('`', 1)[0]
            if archive_string in line:
                build_tools[str(tool)].archive_url = line.split('= ', 1)[1].split('`', 1)[0]
            if archive_string_with_platform in line:
                build_tools[str(tool)].archive_url = line.split('= ', 1)[1].split('`', 1)[0]
            if install_notes_string in line:
                build_tools[str(tool)].install_notes = line.split('= ', 1)[1].split('`', 1)[0]
            if install_notes_string_with_platform in line:
                build_tools[str(tool)].install_notes = line.split('= ', 1)[1].split('`', 1)[0]

    return build_tools


# -------------------------------------------------- Checks ------------------------------------------------------------
def print_platform_preamble(platform_target_name):
    print("")
    if xcode_logging:
        print("Checking " + platform_target_name + " Dependencies...")
    else:
        print(bcolors.BOLD + "Checking " + platform_target_name + " Dependencies..." + bcolors.ENDC)


def print_check_preamble(build_tool):
    print("")
    if xcode_logging:
        print("Checking " + build_tool.name + "...")
    else:
        print(bcolors.BOLD + "Checking " + build_tool.name + "..." + bcolors.ENDC)


def print_version_comparison(build_tool, running_version):
    print("  This system is running " + build_tool.name + " " + running_version)
    print("  We are currently using " + build_tool.name + " " + build_tool.currently_using)

    version_alignment = compare_versions(running_version, build_tool.currently_using)

    install_instructions = ''
    if build_tool.archive_url:
        install_instructions = install_instructions + "You can download " + build_tool.name + " from: " \
                               + build_tool.archive_url
    if build_tool.install_notes:
        install_instructions = install_instructions + " " + build_tool.install_notes

    if version_alignment == 0:
        print_ok_message(build_tool.name + " version " + running_version + " found.")
    elif version_alignment > 0:
        print_warning_message(
            build_tool.name + " version " + running_version + " found, a newer version. We are currently using "
            + build_tool.currently_using + ". " + install_instructions)
    elif version_alignment < 0:
        print_warning_message(
            build_tool.name + " version " + running_version + " found, an older version. We are currently using "
            + build_tool.currently_using + ". " + install_instructions)

    return version_alignment


def check_macos(build_tool):
    print_check_preamble(build_tool)

    running_version = platform.mac_ver()[0]

    print_version_comparison(build_tool, running_version)

    return


def check_git(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['git', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + build_tool.install_notes)
        return False

    if sys.version_info[0] < 3:
        running_version = p.communicate()[0].splitlines()[0].split('git version ', 1)[1]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return False
        running_version = running_version.decode('utf-8').splitlines()[0].split('git version ', 1)[1]

    print_version_comparison(build_tool, running_version)

    return True


def check_python(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['python', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + build_tool.install_notes)
        return False

    if sys.version_info[0] < 3:
        running_version = p.communicate()[1].splitlines()[0].split('Python ', 1)[1]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return False
        running_version = running_version.decode('utf-8').strip().split('Python ')[1]

    print_version_comparison(build_tool, running_version)

    return True


def check_xcode(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['xcodebuild', '-version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message("Error running xcodebuild -version. Do you have Xcode installed? "
                            + build_tool.install_notes)
        return False

    warning_message_one = "Xcode appears to be in the Applications folder, but Xcode does not know which build " \
                          "tools to use. Please launch Xcode and navigate to Xcode > Preferences > Locations and " \
                          "verify that the Command Line Tools are set to the proper version."
    warning_message_two = "Xcode (or xcodebuild) does not seem to be in the Applications folder. If you believe " \
                          "this is an error, please launch Xcode and navigate to Xcode > Preferences > Locations " \
                          "and verify that the Command Line Tools are set to the proper version."
    if sys.version_info[0] < 3:
        running_version = p.communicate()[0]
        if "Build version " not in running_version:
            if os.path.exists("/Applications/Xcode.app"):
                print_warning_message(warning_message_one)
                return False
            else:
                print_warning_message(warning_message_two)
                return False
        running_version = running_version.split('Build version', 1)[0].split('Xcode ', 1)[1].split('\n', 1)[0]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return False
        if "Build version " not in running_version.decode('utf-8'):
            if os.path.exists("/Applications/Xcode.app"):
                print_warning_message(warning_message_one)
                return False
            else:
                print_warning_message(warning_message_two)
                return False
        running_version = running_version.decode('utf-8')
        running_version = running_version.splitlines()[0].strip().split('Xcode ', 1)[1].split('\n', 1)[0]

    print_version_comparison(build_tool, running_version)

    return True


def check_emscripten(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['emcc', '-v'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + build_tool.install_notes)
        return False

    # emcc -v returns an err in the reverse typical order...
    if sys.version_info[0] < 3:
        running_version = p.communicate()[1].splitlines()[0].split(") ")[1]
        if not running_version:
            print_error_message(build_tool.name + " not found." + build_tool.install_notes)
            return False
    else:
        err, running_version = p.communicate()
        if err:
            print_error_message(err)
            return False
        running_version = running_version.decode('utf-8').splitlines()[0].split(") ")[1]

    print_version_comparison(build_tool, running_version)

    return True


def check_cmake(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['cmake', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + build_tool.install_notes)
        return False

    if sys.version_info[0] < 3:
        running_version = p.communicate()[0].splitlines()[0].split('cmake version ', 1)[1]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return
        running_version = running_version.decode('utf-8').splitlines()[0].strip().split('cmake version ')[1]

    print_version_comparison(build_tool, running_version)

    return True


def check_handler(check, build_tools):
    if check == "js":
        print_platform_preamble("Javascript")
        if _platform == "darwin":
            check_macos(build_tools["macos"])
            check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_emscripten(build_tools["emscripten"])
        check_cmake(build_tools["cmake"])

# ------------------------------------------------- Downloads ----------------------------------------------------------
def connected_to_internet(host='http://google.com'):
    try:
        urllib.urlopen(host)
        return True
    except:
        print_error_message("No internet connection available.")
        return False


# TODO: Adapt this to work in both Python 2 and 3...currently it only works in 2
# def download_file(url, destination_folder):
#     file_name = url.split('/')[-1]
#     u = urllib2.urlopen(url)
#     f = open(destination_folder + file_name, 'wb')
#     meta = u.info()
#     file_size = int(meta.getheaders("Content-Length")[0])
#     if xcode_logging:
#         print("Downloading: %s Bytes: %s" % (file_name, file_size))
#     else:
#         print(bcolors.BOLD + "Downloading: " + bcolors.ENDC + "%s Bytes: %s" % (file_name, file_size))
#
#     file_size_dl = 0
#     block_sz = 8192
#     while True:
#         buffer = u.read(block_sz)
#         if not buffer:
#             break
#
#         file_size_dl += len(buffer)
#         f.write(buffer)
#         status = r"%10d  [%3.2f%%]" % (file_size_dl, file_size_dl * 100. / file_size)
#         status = status + chr(8)*(len(status)+1)
#         print status,
#
#     f.close()

def download_handler(download, build_tools):
    # TODO: implement this
    print("TODO: download_handler")


# --------------------------------------------------- Main -------------------------------------------------------------
def main():
    global valid_platform_args
    description = "check for and download developer tools for rhino3dm for a specified platform."
    epilog = ""

    build_tools = read_required_versions()

    # Parse arguments
    parser = argparse.ArgumentParser(description=description, epilog=epilog)
    parser.add_argument('--check', '-c', metavar='<platform>', nargs='+',
                        help="checks the specified platform(s) for build dependencies. valid platform arguments: all, "
                             + ", ".join(valid_platform_args) + ".")
    parser.add_argument('--download', '-d', metavar='<tool>', nargs='+',
                        help="downloads the specified tool(s). valid tool arguments: all, " +
                             ", ".join(build_tools) + ". You may also specify a platform (" +
                             ", ".join(valid_platform_args) + ") to download all dependencies for that platform.")
    parser.add_argument('--xcodelog', '-x', action='store_true',
                        help="generate Xcode-compatible log messages (no colors or other Terminal-friendly gimmicks)")
    args = parser.parse_args()

    # User has not entered any arguments...
    if len(sys.argv) == 1:
        parser.print_help(sys.stderr)
        sys.exit(1)

    global xcode_logging
    xcode_logging = args.xcodelog

    # checks
    if args.check is not None:
        for check in args.check:
            if check not in valid_platform_args:
                print_error_message(check + " is not a valid platform argument. valid platform arguments: all, "
                                    + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            check_handler(check, build_tools)

    # downloads
    if args.download is not None:
        for download in args.download:
            if (download not in build_tools) and (download not in valid_platform_args):
                print_error_message(download + " is not a valid tool (or platform) argument. valid arguments: all, "
                                    + ", ".join(build_tools) + ", " + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            download_handler(download, build_tools)


if __name__ == "__main__":
    main()
