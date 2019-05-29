{
    "conditions": [
        ["OS=='win'", {
            "target_defaults": {
                "default_configuration": "Release_x64",
                "configurations": {
                    "Debug_Win32": {
                        "msvs_configuration_platform": "Win32",
                        "msvs_windows_sdk_version": "v8.1",
                        "msvs_target_platform_version": "v8.1",
                        "defines": [ "WIN32"," _DEBUG", "_LIB"],
                        "msvs_settings": {
                            "VCCLCompilerTool": {
                                "RuntimeLibrary": "3",
                                "Optimization": 0,
                                "MinimalRebuild": "false",
                                "BasicRuntimeChecks": 3
                            }
                        }
                    },
                    "Debug_x64": {
                        "ingerits_from": ["Debug_Win32"],
                        "msvs_configuration_platform": "x64",
                        "defines": [ "WIN64"," _DEBUG", "_LIB"],
                        "msvs_settings": {
                            "VCCLCompilerTool": {
                                "RuntimeLibrary": "3",
                                "Optimization": 0,
                                "MinimalRebuild": "false",
                                "BasicRuntimeChecks": 3
                            }
                        }
                    },
                    "Release_Win32": {
                        "msvs_configuration_platform": "Win32",
                        "msvs_windows_sdk_version": "v8.1",
                        "msvs_target_platform_version": "v8.1",
                        "defines": [ "WIN32"," NDEBUG", "_LIB"],
                        "msvs_settings": {
                            "VCCLCompilerTool": {
                                "RuntimeLibrary": "2",
                                "Optimization": 3,
                                "FavorSizeOrSpeed": 1
                            },
                            "VCLibrarianTool": {
                                "AdditionalOptions": ["/LTCG"]
                            },
                            "VCLinkerTool": {
                                "LinkTimeCodeGeneration": 1,
                                "OptimizeReferences": 2,
                                "EnableCOMDATFolding": 2,
                                "LinkIncremental": 1,
                                "GenerateDebugInformation": "false"
                            }
                        }
                    },
                    "Release_x64": {
                        "inherits_from": ["Release_Win32"],
                        "msvs_configuration_platform": "x64",
                        "defines": [ "WIN64"," NDEBUG", "_LIB"]
                    }
                }
            }
        }, {
            "target_defaults": {
                "default_configuration": "Release",
                "xcode_settings": {},
                "configurations": {
                    "Debug": {
                        "defines": ["DEBUG"],
                        "xcode_settings": {
                            "GCC_OPTIMIZATION_LEVEL": "0",
                            "GCC_GENERATE_DEBUGGING_SYMBOLS": "YES"
                        }
                    },
                    "Release": {
                        "defines": ["NDEBUG"],
                        "xcode_settings": {
                            "GCC_OPTIMIZATION_LEVEL": "3",
                            "GCC_GENERATE_DEBUGGING_SYMBOLS": "NO",
                            "DEAD_CODE_STRIPPING": "YES",
                            "GCC_INLINES_ARE_PRIVATE_EXTERN": "YES"
                        }
                    }
                }
            }
        }]
    ],
    "targets": [
        {
            "target_name": "zlib",
            "type": "static_library",
            "msvs_guid": "7B90C09F-DC78-42B2-AD34-380F6D466B29",
            "msbuild_props": ["../opennurbs_msbuild.Cpp.props"],
            "sources": [
                "crc32.h",
                "deflate.h",
                "inffast.h",
                "inffixed.h",
                "inflate.h",
                "inftrees.h",
                "trees.h",
                "zconf.h",
                "zlib.h",
                "zutil.h",
                "adler32.c",
                "compress.c",
                "crc32.c",
                "deflate.c",
                "infback.c",
                "inffast.c",
                "inflate.c",
                "inftrees.c",
                "trees.c",
                "uncompr.c",
                "zutil.c"
            ],
            "msvs_configuration_attributes": {
                "CharacterSet": "1"
            }
        }
    ]
}
