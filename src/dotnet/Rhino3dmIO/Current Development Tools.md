# Current Development Tools (Rhino3dmIO)

Last updated by dan@mcneel.com on September 8th, 2017

**NOTE**: Change this file at your own risk.  This file is read by scripts to determine the required development tools and versions of those tools.  Though it is human-readable, it is part of build processes. Renaming or reformatting this file may cause undesired results.

## GYP

We are currently using GYP for development.  Scripts read this:

`gyp_archive_url = http://files.mcneel.com/rhino/6.0/developer-tools/gyp.zip`

## msbuild

We are currently using msbuild 15 for development.  Scripts read this:

`currently_using_msbuild = 15`


## Android

We are currently using Android NDK r15c.  Scripts read this:

`currently_using_android_ndk = r15c`
`android_ndk_archive_url_win = http://files.mcneel.com/rhino/6.0/developer-tools/android-ndk-r15c-windows-x86_64.zip`
`android_ndk_archive_url_mac = http://files.mcneel.com/rhino/6.0/developer-tools/android-ndk-r15c-darwin-x86_64.zip`

---

## Related Topics

- [README.md](README.md) for an overview of the Rhino3dmIO build process
