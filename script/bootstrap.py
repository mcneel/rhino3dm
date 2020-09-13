# bootstrap.py
# created: January 8, 2020
#
# This script checks for the necessary tools to construct and build the rhino3dm native and wrapper libraries
# for all supported platforms.  See related scripts in this folder for other steps in the process.
#
# This script is inspired by - but deviates from - the "Scripts To Rule Them All" pattern:
# https://github.com/github/scripts-to-rule-them-all

# ---------------------------------------------------- Imports ---------------------------------------------------------

from __future__ import (division, absolute_import, print_function, unicode_literals)

import subprocess
import sys
import os
from os import listdir
from os.path import isfile, isdir, join
import argparse
import ssl
import re
import glob
import platform
if sys.version_info >= (3,):
    import urllib.request as urllib2
    import urllib.parse as urlparse
else:
    import urllib2
    import urlparse
import urllib
from subprocess import Popen, PIPE
from sys import platform as _platform


# ---------------------------------------------------- Globals ---------------------------------------------------------

xcode_logging = False
valid_platform_args = ["windows", "linux", "macos", "ios", "android", "js", "python"]
submodules = ["opennurbs", "draco", "pybind11"]
script_folder = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
src_folder = os.path.abspath(os.path.join(script_folder, "..", "src"))
lib_folder = os.path.abspath(os.path.join(src_folder, "lib"))

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
        print(bcolors.BOLD + bcolors.WARNING + warning_prefix.upper() + bcolors.ENDC + bcolors.WARNING + warning_message +
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

def split_by_numbers(x):
    r = re.compile('(\d+)')
    l = r.split(x)
    return [int(y) if y.isdigit() else y for y in l]


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
    current_development_tools_file_path = os.path.join(script_folder, '..', 'Current Development Tools.md')
    if not os.path.exists(current_development_tools_file_path):
        print_error_message("Could not find the Current Development Tools.md (rhino3dm) file listing our "
                            "current development tools.\n This file should be in: " +
                            current_development_tools_file_path + "\n Exiting script.")
        sys.exit(1)

    # Windows
    msbuild = BuildTool("msbuild", "msbuild", "", "", "")

    # Linux
    dotnet = BuildTool("dotnet Core SDK", "dotnet", "", "", "")

   # iOS
    xamios = BuildTool("Xamarin.iOS", "xamios", "", "", "")
 
    # Android
    ndk = BuildTool("Android NDK", "ndk", "", "", "")
    xamandroid = BuildTool("Xamarin.Android", "xamandroid", "", "", "")
    
    # Javascript
    emscripten = BuildTool("Emscripten", "emscripten", "", "", "")

    # Shared
    git = BuildTool("Git", "git", "", "", "")
    python = BuildTool("Python", "python", "", "", "")
    cmake = BuildTool("CMake", "cmake", "", "", "")
    mdk = BuildTool("Mono MDK", "mdk", "", "", "")
    macos = BuildTool("macOS", "macos", "", "", "")
    xcode = BuildTool("Xcode", "xcode", "", "", "")

    # create the build tools dictionary
    build_tools = dict(msbuild=msbuild, 
                       dotnet=dotnet, 
                       macos=macos, 
                       xamios=xamios, 
                       ndk=ndk, 
                       xamandroid=xamandroid, 
                       emscripten=emscripten, 
                       git=git, 
                       python=python, 
                       cmake=cmake,                  
                       mdk=mdk, 
                       xcode=xcode)

    # open and read Current Development Tools.md and load required versions
    current_development_tools_file = open(current_development_tools_file_path, "r")
    for line in current_development_tools_file:
        for tool in build_tools:
            tool_prefix = tool
            if tool == "python":
                if sys.version_info[0] < 3:
                    tool_prefix = tool + "2"
                else:
                    tool_prefix = tool + "3"

            ver_prefix = tool_prefix + "_currently_using = "

            archive_prefix = tool_prefix + "_archive_url"
            archive_suffix = ''
            if _platform == "win32":
                archive_suffix = "_windows = "
            if _platform == "darwin":
                archive_suffix = "_macos = "
            if _platform == "linux" or _platform == "linux2":
                archive_suffix = "_linux = "
            archive_string = archive_prefix + " = "
            archive_string_with_platform = archive_prefix + archive_suffix

            install_notes_prefix = tool_prefix + "_install_notes"
            install_notes_suffix = ''
            if _platform == "win32":
                install_notes_suffix = "_windows = "
            if _platform == "darwin":
                install_notes_suffix = "_macos = "
            if _platform == "linux" or _platform == "linux2":
                install_notes_suffix = "_linux = "
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


def format_install_instructions(build_tool):
    install_instructions = ''
    if build_tool.archive_url:
        install_instructions = install_instructions + "You can download " + build_tool.name + " from: " \
                               + build_tool.archive_url
    if build_tool.install_notes:
        install_instructions = install_instructions + " " + build_tool.install_notes

    return install_instructions


def print_version_comparison(build_tool, running_version):
    print("  This system is running " + build_tool.name + " " + running_version)
    print("  We are currently using " + build_tool.name + " " + build_tool.currently_using)

    version_alignment = compare_versions(running_version, build_tool.currently_using)

    if version_alignment == 0:
        print_ok_message(build_tool.name + " version " + running_version + " found.")
    elif version_alignment > 0:
        print_warning_message(
            build_tool.name + " version " + running_version + " found, a newer version. We are currently using "
            + build_tool.currently_using + ". ")
    elif version_alignment < 0:
        print_warning_message(
            build_tool.name + " version " + running_version + " found, an older version. We are currently using "
            + build_tool.currently_using + ". " + format_install_instructions(build_tool))

    return version_alignment


def check_submodules():
    for submodule in submodules:
        path_to_submodule = os.path.abspath(os.path.join(lib_folder, submodule))
        path_to_git = os.path.abspath(os.path.join(path_to_submodule, '.git'))
        if not os.path.exists(path_to_git):
            print_error_message("submodule at: " + path_to_submodule + " does not appear to be initialized.  Please run: git submodule update --init")
            return False
    
    return True


def check_macos(build_tool):
    if _platform != 'darwin':
        print_warning_message("macOS is only supported on macOS...duh.")
        return False

    print_check_preamble(build_tool)

    running_version = platform.mac_ver()[0]

    print_version_comparison(build_tool, running_version)

    return


def check_git(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['git', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    if sys.version_info[0] < 3:
        running_version = p.communicate()[0].splitlines()[0].split('git version ', 1)[1]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return False
        running_version = running_version.decode('utf-8').splitlines()[0].split('git version ', 1)[1]

    if _platform == "win32":
        running_version = running_version.split(".windows")[0]

    print_version_comparison(build_tool, running_version)

    return True


def check_python(build_tool):
    print_check_preamble(build_tool)
    running_version = ".".join([str(sys.version_info.major), str(sys.version_info.minor), str(sys.version_info.micro)])    
    print_version_comparison(build_tool, running_version)
    return True


def check_xcode(build_tool):
    if _platform != 'darwin':
        print_warning_message("Xcode is only supported on macOS.")
        return False

    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['xcodebuild', '-version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message("Error running xcodebuild -version. Do you have Xcode installed? "
                            + format_install_instructions(build_tool))
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

    # check to make sure that the $EMSDK path variable has been set
    if _platform != "win32" and _platform != "win64":
        emsdk_path = ''
        cmd = 'echo $EMSDK'
        p = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE)
        if sys.version_info[0] < 3:
            emsdk_path = p.communicate()[0].strip()                
        else:
            emsdk_path, err = p.communicate()
            emsdk_path = emsdk_path.strip()
            
        if not emsdk_path:
            print_error_message(build_tool.name + " EMSDK path not set." + format_install_instructions(build_tool))
            return False
        
    emcc = 'emcc.bat' if _platform == 'win32' else 'emcc'
    try:
        p = subprocess.Popen([emcc, '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False
        
    if sys.version_info[0] < 3:
        running_version = p.communicate()[0].splitlines()[0].split()[4]
        if not running_version:
            print_error_message(build_tool.name + " not found." + format_install_instructions(build_tool))
            return False
    else:
        running_version, err = p.communicate()
        if err:
            print_error_message(err)
            return False
        running_version = running_version.decode('utf-8').splitlines()[0].split()[4]

    print_version_comparison(build_tool, running_version)

    return True


def check_cmake(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['cmake', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    except OSError:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    if sys.version_info[0] < 3:
        running_version = p.communicate()[0].splitlines()[0].split('cmake version ', 1)[1]
    else:
        running_version, err = p.communicate()
        if err:
            print_warning_message(err)
            return
        running_version = re.split(r'cmake3? version ', running_version.decode('utf-8').splitlines()[0].strip())[1]

    print_version_comparison(build_tool, running_version)

    return True


def check_mdk(build_tool):
    print_check_preamble(build_tool)

    # check to see if the Mono.framework exists at all...
    running_mono_framework_version_file_path = '/Library/Frameworks/Mono.framework/Versions/Current/VERSION'
    if not os.path.exists(running_mono_framework_version_file_path):
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    # read in the contents of /Library/Frameworks/Mono.framework/Versions/Current/VERSION
    running_mono_framework_version_file = open(running_mono_framework_version_file_path, "r")
    running_version = ''
    for line in running_mono_framework_version_file:
        running_version = line.split('\n', 1)[0]

    print_version_comparison(build_tool, running_version)

    return True


def check_xamios(build_tool):
    print_check_preamble(build_tool)

    # check to see if the Xamarin.iOS.framework exists at all...
    running_xamios_framework_version_file_path = os.path.join('/', 'Library', 'Frameworks',
                                                                    'Xamarin.iOS.Framework', "Versions",
                                                                    "Current", "Version")
    if not os.path.exists(running_xamios_framework_version_file_path):
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    # read in the contents of /Library/Frameworks/Xamarin.iOS.framework/Versions/Current/VERSION
    running_xamios_framework_version_file = open(running_xamios_framework_version_file_path, "r")
    running_version = ''
    for line in running_xamios_framework_version_file:
        running_version = line.split('\n', 1)[0]

    print_version_comparison(build_tool, running_version)

    return True


def check_ndk(build_tool):
    print_check_preamble(build_tool)

    # figure out where the NDK might be installed - multiple versions are 
    # frequently installed, so we need to figure out which is the root folder
    # containing these versions...
    ndk_root_path = ''
    ndk_root_path_spaceless = ''
    drive_prefix = ''
    android_ndk_path = ''
    if _platform == "win32":
        program_files = os.environ["ProgramW6432"]
        ndk_root_path = os.path.join(program_files, "Android", "ndk")
        drive_prefix = os.path.splitdrive(sys.executable)[0]
        ndk_root_path_spaceless = drive_prefix + '\\' + 'Android\\' + 'ndk\\'
    if _platform == "darwin":
        home = os.path.expanduser("~")
        ndk_root_path = os.path.join(home, "Library", "Developer", "Xamarin", "android-ndk")

    if not os.path.exists(ndk_root_path):
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False
 
    # we are going to search the root folder for valid ndk versions, so we need to set up
    # a search pattern that is per-platform
    ndk_build_sub_search = ''
    if _platform == "win32":
        ndk_build_sub_search = "\\android-ndk-r??\\ndk-build"
    if _platform == "darwin":
        ndk_build_sub_search = "/android-ndk-r??/ndk-build"
    
    versions_found = dict()
    if glob.glob(ndk_root_path + ndk_build_sub_search):
        ndk_build_sub_search = '' 
        has_ndk = True
        path_to_search = ndk_root_path

        only_folders = [d for d in listdir(path_to_search) if isdir(join(path_to_search, d))]
        
        for folder in only_folders:
            if folder.startswith("android-ndk-r"):
                version_id = folder.split("android-ndk-")[1]
                # create a path to source.properites
                ver_info_file = os.path.join(ndk_root_path, folder, "source.properties")
                if os.path.exists(ver_info_file):
                    src_props_file = open(ver_info_file, "r")
                    for line in src_props_file:
                        if "Pkg.Revision =" in line:
                            build_number = line.strip().split('= ')[1]
                            versions_found[version_id] = build_number
                    src_props_file.close()

    if not versions_found:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False       

    # check to see if we have the current NDK version in the list
    running_version = ''
    for version_id, build_number in versions_found.items():
        if build_number == build_tool.currently_using:
            running_version = build_number
            android_ndk_path = os.path.join(ndk_root_path, "android-ndk-" + version_id, '')
    
    # if we don't find a match, get the highest version we can find...
    if not running_version:
        sorted_versions_found = sorted(versions_found, key=split_by_numbers)
        if sorted_versions_found:
            version_id = sorted_versions_found[-1]
            running_version = versions_found[version_id]
            android_ndk_path = os.path.join(ndk_root_path, "android-ndk-" + version_id, '')

    print_version_comparison(build_tool, running_version)

    android_env_variable = os.environ.get('ANDROID_NDK') #
    if not android_env_variable or os.environ.get('ANDROID_NDK') != android_ndk_path:
        print_warning_message('The NDK was found but the ANDROID_NDK variable is not properly set.  Setting to ' + android_ndk_path + ' now.')
        os.environ["ANDROID_NDK"] = android_ndk_path
    
    return android_ndk_path


def check_xamandroid(build_tool):
    print_check_preamble(build_tool)

    # check to see if the Xamarin.Android.framework exists at all...
    running_xamandroid_framework_version_file_path = os.path.join('/', 'Library', 'Frameworks',
                                                                    'Xamarin.Android.Framework', "Versions",
                                                                    "Current", "Version")
    if not os.path.exists(running_xamandroid_framework_version_file_path):
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    # read in the contents of /Library/Frameworks/Xamarin.Android.framework/Versions/Current/VERSION
    running_xamandroid_framework_version_file = open(running_xamandroid_framework_version_file_path, "r")
    running_version = ''
    for line in running_xamandroid_framework_version_file:
        running_version = line.split('\n', 1)[0]

    print_version_comparison(build_tool, running_version)

    return True


def check_msbuild(build_tool):
    print_check_preamble(build_tool)

    # msbuild ships with the mdk on macOS and Linux, so that check should suffice there.
    if _platform != "win32" and _platform != "win64":
        print_warning_message("Checking for " + build_tool.name + " requires that you run this script on Windows.")
        return False

    # prepare to do some searching
    drive_prefix = os.path.splitdrive(sys.executable)[0]
    program_files = os.environ.get("PROGRAMFILES(X86)")

    running_version = ''
    msbuild_path = ''

    # Check for the Visual Studio MSBuild
    if not msbuild_path:
        visual_studio_path = os.path.join(drive_prefix, program_files, "Microsoft Visual Studio")
        if os.path.exists(visual_studio_path):
            versions_found = []
            vs_ver_subsearch = "\\20??\\Professional"
            if glob.glob(visual_studio_path + vs_ver_subsearch):
                path_to_search = visual_studio_path
                only_folders = [d for d in listdir(path_to_search) if isdir(join(path_to_search, d))]
        
                for folder in only_folders:
                    if folder.startswith("20"):
                        versions_found.append(folder)

            if versions_found:
                latest_version = str(max(versions_found))
                path_to_search = os.path.join(visual_studio_path, latest_version, "Professional", "MSBuild", "Current", "Bin", "MSBuild.exe")
                if os.path.exists(path_to_search):
                    msbuild_path = path_to_search

    #Check if msbuild is in the path
    if not msbuild_path:
        try:
            p = subprocess.Popen(['where', 'msbuild'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
            if sys.version_info[0] < 3:
                response = p.communicate()
                if "Could not find files" not in response:
                    if "MSBuild.exe" in response:
                        msbuild_path = response
            else:
                err, response = p.communicate()
                if "Could not find files" not in response:
                    if "MSBuild.exe" in response:
                        msbuild_path = response
        except:
            msbuild_path = ''

    # Check for the .NET Framework MSBuild (probably not the wisest to use though)
    if not msbuild_path:
        dotnet_framework_path = os.path.join(drive_prefix, "\\", "Windows", "Microsoft.NET", "Framework")
        if os.path.exists(dotnet_framework_path):
            versions_found = dict()
            dotnet_ver_subsearch = "\\v*\\MSBuild.exe"
            if glob.glob(dotnet_framework_path + dotnet_ver_subsearch):
                path_to_search = dotnet_framework_path
                only_folders = [d for d in listdir(path_to_search) if isdir(join(path_to_search, d))]
        
                for folder in only_folders:
                    if folder.startswith("v"):
                        version_id = folder.split("v")[1]
                        versions_found[version_id] = folder

            if versions_found:
                sorted_versions_found = sorted(versions_found, key=split_by_numbers)
                if sorted_versions_found:
                    version_id = sorted_versions_found[-1]
                    path_to_search = os.path.join(dotnet_framework_path, versions_found[version_id], "MSBuild.exe")
                    if os.path.exists(path_to_search):
                        msbuild_path = path_to_search

    if not msbuild_path:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    # Check the msbuild version
    try:
        p = subprocess.Popen([msbuild_path, '/Version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
        if sys.version_info[0] < 3:
            running_version = p.communicate()[0].splitlines()[-1].strip()
        else:
            running_version, err = p.communicate()
            if err:
                print_warning_message(err)
                return
            running_version = running_version.decode('utf-8').splitlines()[-1].strip()
    except:
        running_version = ''

    if not running_version:
        print_error_message("unable to determine the version of " + build_tool.name)
        return False

    print_version_comparison(build_tool, running_version)
    return msbuild_path


def check_dotnet(build_tool):
    print_check_preamble(build_tool)

    try:
        p = subprocess.Popen(['which', 'dotnet'], stdin=PIPE, stdout=PIPE, stderr=PIPE)

        if sys.version_info[0] < 3:
            output = p.communicate()
            if not output[0] and not output[1]:
                print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
                return False
        else:
            output, err = p.communicate()
            if not output and not err:    
                print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
                return False
    except OSError:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    running_version = ''

    # check to see if there are any sdks in the list that match the currently used one
    dotnet_sdks = []
    dotnet_listsdks_process = subprocess.Popen(['dotnet', '--list-sdks'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
    if sys.version_info[0] < 3:
        dotnet_sdks = dotnet_listsdks_process.communicate()[0].splitlines()
    else:
        dotnet_listsdks_output, err = dotnet_listsdks_process.communicate()
        if err:
            print_error_message(err)
            return False
        
        dotnet_sdks = dotnet_listsdks_output.decode('utf').splitlines()

    for sdk in dotnet_sdks:
        version_number = sdk.split(' [')[0]
        if build_tool.currently_using in str(sdk):
            running_version = build_tool.currently_using

    # fall-back: check the version on the system...
    if not running_version:
        dotnet_version_process = subprocess.Popen(['dotnet', '--version'], stdin=PIPE, stdout=PIPE, stderr=PIPE)
        if sys.version_info[0] < 3:
            running_version = dotnet_version_process.communicate()[0].strip()
        else:
            dotnet_version_output, err = dotnet_version_process.communicate()
            running_version = dotnet_version_output.decode('utf-8').strip()

    if not running_version:
        print_error_message(build_tool.name + " not found. " + format_install_instructions(build_tool))
        return False

    print_version_comparison(build_tool, running_version)

    return True


def check_handler(check, build_tools):
    if check == "windows":
        print_platform_preamble("Windows")
        if _platform != "win32" and _platform != "win64":
            print_error_message("Checking dependencies for Windows requires that you run this script on Windows")
            return False
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_cmake(build_tools["cmake"])
        check_msbuild(build_tools["msbuild"])

    if check == "linux":
        print_platform_preamble("Linux")
        if _platform != "linux" and _platform != "linux2":
            print_error_message("Checking dependencies for Linux requires that you run this script on Linux.")
            return False
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_cmake(build_tools["cmake"])
        check_dotnet(build_tools["dotnet"])

    if check == "macos":
        print_platform_preamble("macOS")
        if _platform != "darwin":
            print_error_message("Checking dependencies for macOS requires that you run this script on macOS")
            return False
        check_macos(build_tools["macos"])
        check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_cmake(build_tools["cmake"])
        check_mdk(build_tools["mdk"])

    if check == "ios":
        print_platform_preamble("iOS")
        if _platform != "darwin":
            print_error_message("Checking dependencies for iOS requires that you run this script on macOS")
            return False
        check_macos(build_tools["macos"])
        check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_cmake(build_tools["cmake"])
        check_mdk(build_tools["mdk"])
        check_xamios(build_tools["xamios"])

    if check == "android":
        print_platform_preamble("Android")
        if _platform != "darwin":
            print_error_message("Checking dependencies for Android requires that you run this script on macOS")
            return False
        check_macos(build_tools["macos"])
        check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_cmake(build_tools["cmake"])
        check_mdk(build_tools["mdk"])
        check_ndk(build_tools["ndk"])
        check_xamandroid(build_tools["xamandroid"])
    
    if check == "js":
        print_platform_preamble("JavaScript")
        if _platform == "darwin":
            check_macos(build_tools["macos"])
            check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_emscripten(build_tools["emscripten"])
        check_cmake(build_tools["cmake"])

    if check == "python":
        print_platform_preamble("Python")
        if _platform == "darwin":
            check_macos(build_tools["macos"])
            check_xcode(build_tools["xcode"])
        check_git(build_tools["git"])
        check_python(build_tools["python"])
        check_emscripten(build_tools["emscripten"])
        check_cmake(build_tools["cmake"])

    if check not in valid_platform_args:
        if check == "all":
            for tool in build_tools:
                getattr(sys.modules[__name__], 'check_' + tool)(build_tools[tool])
        else:
            getattr(sys.modules[__name__], 'check_' + check)(build_tools[check])


# ------------------------------------------------- Downloads ----------------------------------------------------------
def print_platform_download_preamble(platform_target_name):
    print("")
    if xcode_logging:
        print("Download all tools for " + platform_target_name + "...")
    else:
        print(bcolors.BOLD + "Download all tools for " + platform_target_name + "..." + bcolors.ENDC)


def connected_to_internet(host='http://google.com'):
    try:
        urllib.urlopen(host)
        return True
    except:
        print_error_message("No internet connection available.")
        return False


def download_file(url, dest=None):
    if not os.environ.get('PYTHONHTTPSVERIFY', '') and getattr(ssl, '_create_unverified_context', None):
        ssl._create_default_https_context = ssl._create_unverified_context

    u = urllib2.urlopen(url)

    scheme, netloc, path, query, fragment = urlparse.urlsplit(url)
    filename = os.path.basename(path)
    if not filename:
        filename = 'downloaded.file'
    if dest:
        filename = os.path.join(dest, filename)

    with open(filename, 'wb') as f:
        meta = u.info()
        meta_func = meta.getheaders if hasattr(meta, 'getheaders') else meta.get_all
        meta_length = meta_func("Content-Length")
        file_size = None
        if meta_length:
            file_size = int(meta_length[0])
        print("URL: {0} Bytes: {1}".format(url, file_size))

        file_size_dl = 0
        block_sz = 8192
        while True:
            buffer = u.read(block_sz)
            if not buffer:
                break

            file_size_dl += len(buffer)
            f.write(buffer)

            status = "{0:16}".format(file_size_dl)
            if file_size:
                status += "   [{0:6.2f}%]".format(file_size_dl * 100 / file_size)
            status += chr(13)
            print(status, end="")
        print()

    return filename


def download_dependency(build_tool):
    print("")
    if xcode_logging:
        print("Downloading " + build_tool.name + "...")
    else:
        print(bcolors.BOLD + "Downloading " + build_tool.name + "..." + bcolors.ENDC)
    downloads_folder = '/Downloads/'
    if _platform == "win32" or _platform == "win64":
        downloads_folder = '\\Downloads\\'
    destination_folder = os.path.expanduser("~") + downloads_folder
    if build_tool.archive_url:
        download_file(build_tool.archive_url, destination_folder)
        print_ok_message('Downloaded to ' + destination_folder + build_tool.archive_url.split('/')[-1])
    else:
        print_warning_message(build_tool.name + ' does not have a url on file. ' + build_tool.install_notes)
        

def download_handler(download, build_tools):
    if download == "windows":
        print_platform_download_preamble("Windows")
        if _platform != "win32" and _platform != "win64":
            print_error_message("Downloading dependencies for Windows requires that you run this script on Windows")
            return False
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])        
        download_dependency(build_tools["cmake"])
        download_dependency(build_tools["msbuild"])

    if download == "linux":
        print_platform_download_preamble("Linux")
        if _platform != "linux" and _platform != "linux2":
            print_error_message("Downloading dependencies for Linux requires that you run this script on Linux")
            return False
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["cmake"])
        download_dependency(build_tools["dotnet"])

    if download == "macos":
        print_platform_download_preamble("macOS")
        if _platform != "darwin":
            print_error_message("Downloading dependencies for macOS requires that you run this script on macOS")
            return False
        download_dependency(build_tools["macos"])
        download_dependency(build_tools["xcode"])
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["cmake"])
        download_dependency(build_tools["mdk"])

    if download == "ios":
        print_platform_download_preamble("iOS")
        if _platform != "darwin":
            print_error_message("Downloading dependencies for iOS requires that you run this script on macOS")
            return False
        download_dependency(build_tools["macos"])
        download_dependency(build_tools["xcode"])
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["cmake"])
        download_dependency(build_tools["mdk"])
        download_dependency(build_tools["xamios"])

    if download == "android":
        print_platform_download_preamble("Android")
        if _platform == "darwin":                      
            download_dependency(build_tools["macos"])
            download_dependency(build_tools["xcode"])
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["cmake"])
        download_dependency(build_tools["mdk"])
        download_dependency(build_tools["ndk"])
        download_dependency(build_tools["xamandroid"])

    if download == "js":
        print_platform_download_preamble("JavaScript")
        if _platform == "darwin":
            download_dependency(build_tools["macos"])
            download_dependency(build_tools["xcode"])
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["emscripten"])
        download_dependency(build_tools["cmake"])

    if download == "python":
        print_platform_download_preamble("Python")
        if _platform == "darwin":
            download_dependency(build_tools["macos"])
            download_dependency(build_tools["xcode"])
        download_dependency(build_tools["git"])
        download_dependency(build_tools["python"])
        download_dependency(build_tools["emscripten"])
        download_dependency(build_tools["cmake"])

    if download not in valid_platform_args:
        if download == "all":
            for tool in build_tools:
                download_dependency(build_tools[tool])
        else:
            download_dependency(build_tools[download])


# --------------------------------------------------- Main -------------------------------------------------------------
def main():
    global valid_platform_args
    build_tools = read_required_versions()

    # cli metadata
    description = "check for and download developer tools for rhino3dm for a specified platform."
    epilog = "supported platforms and tools: " + ", ".join(valid_platform_args) + ", " + ", ".join(build_tools)

    # Parse arguments
    parser = argparse.ArgumentParser(description=description, epilog=epilog)
    parser.add_argument('--platform', '-p', metavar='<platform>', nargs='+',
                        help="checks the specified platform(s) for build dependencies. valid arguments: all, "
                             + ", ".join(valid_platform_args) + ".")
    parser.add_argument('--check', '-c', metavar='<tool>', nargs='+',
                        help="checks for the specified tool(s) and checks the version. valid arguments: all, "
                             + ", ".join(build_tools) + ".")
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

    if _platform == "win32":
        xcode_logging = True

    # check submodules
    if not check_submodules():
        sys.exit(1)

    # check platform(s)
    if args.platform is not None:
        for target_platform in args.platform:
            if (target_platform != "all") and (target_platform not in valid_platform_args)\
                    and (target_platform in build_tools):
                print_error_message(target_platform + " is not a valid platform argument. valid platform arguments: all, "
                                    + ", ".join(valid_platform_args) + ". Are you looking for the -c --check argument?")
                sys.exit(1)
            elif (target_platform != "all") and (target_platform not in valid_platform_args):
                print_error_message(target_platform + " is not a valid platform argument. valid platform arguments: all, "
                                    + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            check_handler(target_platform, build_tools)

    # check tools
    if args.check is not None:
        for check in args.check:
            if (check != "all") and (check not in build_tools) and (check in valid_platform_args):
                print_error_message(check + " is not a valid tool argument. valid tool arguments: all, "
                                    + ", ".join(build_tools) + ". Are you looking for the -p --platform argument?")
                sys.exit(1)
            elif (check != "all") and (check not in build_tools):
                print_error_message(check + " is not a valid tool argument. valid tool arguments: all, "
                                    + ", ".join(build_tools) + ".")
                sys.exit(1)
            check_handler(check, build_tools)

    # downloads
    if args.download is not None:
        for download in args.download:
            if (download != "all") and (download not in build_tools) and (download not in valid_platform_args):
                print_error_message(download + " is not a valid tool (or platform) argument. valid arguments: all, "
                                    + ", ".join(build_tools) + ", " + ", ".join(valid_platform_args) + ".")
                sys.exit(1)
            download_handler(download, build_tools)


if __name__ == "__main__":
    main()
