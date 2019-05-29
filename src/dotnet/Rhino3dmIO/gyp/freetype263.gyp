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
            "target_name": "freetype263_staticlib",
            "type": "static_library",
            "msvs_guid": "F28EFCCD-948B-425C-B9FC-112D84A6498D",
            "msbuild_props": ["../opennurbs_msbuild.Cpp.props"],
            "include_dirs": ["./include"],
            "defines": ["FT2_BUILD_LIBRARY"],
            "sources": [
                "include/freetype/config/ftconfig.h",
                "include/freetype/config/ftheader.h",
                "include/freetype/config/ftmodule.h",
                "include/freetype/config/ftoption.h",
                "include/freetype/config/ftstdlib.h",
                "include/freetype/freetype.h",
                "include/freetype/ftadvanc.h",
                "include/freetype/ftautoh.h",
                "include/freetype/ftbbox.h",
                "include/freetype/ftbdf.h",
                "include/freetype/ftbitmap.h",
                "include/freetype/ftbzip2.h",
                "include/freetype/ftcache.h",
                "include/freetype/ftcffdrv.h",
                "include/freetype/ftchapters.h",
                "include/freetype/ftcid.h",
                "include/freetype/fterrdef.h",
                "include/freetype/fterrors.h",
                "include/freetype/ftfntfmt.h",
                "include/freetype/ftgasp.h",
                "include/freetype/ftglyph.h",
                "include/freetype/ftgxval.h",
                "include/freetype/ftgzip.h",
                "include/freetype/ftimage.h",
                "include/freetype/ftincrem.h",
                "include/freetype/ftlcdfil.h",
                "include/freetype/ftlist.h",
                "include/freetype/ftlzw.h",
                "include/freetype/ftmac.h",
                "include/freetype/ftmm.h",
                "include/freetype/ftmodapi.h",
                "include/freetype/ftmoderr.h",
                "include/freetype/ftotval.h",
                "include/freetype/ftoutln.h",
                "include/freetype/ftpfr.h",
                "include/freetype/ftrender.h",
                "include/freetype/ftsizes.h",
                "include/freetype/ftsnames.h",
                "include/freetype/ftstroke.h",
                "include/freetype/ftsynth.h",
                "include/freetype/ftsystem.h",
                "include/freetype/fttrigon.h",
                "include/freetype/ftttdrv.h",
                "include/freetype/fttypes.h",
                "include/freetype/ftwinfnt.h",
                "include/freetype/internal/autohint.h",
                "include/freetype/internal/ftcalc.h",
                "include/freetype/internal/ftdebug.h",
                "include/freetype/internal/ftdriver.h",
                "include/freetype/internal/ftgloadr.h",
                "include/freetype/internal/fthash.h",
                "include/freetype/internal/ftmemory.h",
                "include/freetype/internal/ftobjs.h",
                "include/freetype/internal/ftpic.h",
                "include/freetype/internal/ftrfork.h",
                "include/freetype/internal/ftserv.h",
                "include/freetype/internal/ftstream.h",
                "include/freetype/internal/fttrace.h",
                "include/freetype/internal/ftvalid.h",
                "include/freetype/internal/internal.h",
                "include/freetype/internal/psaux.h",
                "include/freetype/internal/pshints.h",
                "include/freetype/internal/services/svbdf.h",
                "include/freetype/internal/services/svcid.h",
                "include/freetype/internal/services/svfntfmt.h",
                "include/freetype/internal/services/svgldict.h",
                "include/freetype/internal/services/svgxval.h",
                "include/freetype/internal/services/svkern.h",
                "include/freetype/internal/services/svmm.h",
                "include/freetype/internal/services/svotval.h",
                "include/freetype/internal/services/svpfr.h",
                "include/freetype/internal/services/svpostnm.h",
                "include/freetype/internal/services/svprop.h",
                "include/freetype/internal/services/svpscmap.h",
                "include/freetype/internal/services/svpsinfo.h",
                "include/freetype/internal/services/svsfnt.h",
                "include/freetype/internal/services/svttcmap.h",
                "include/freetype/internal/services/svtteng.h",
                "include/freetype/internal/services/svttglyf.h",
                "include/freetype/internal/services/svwinfnt.h",
                "include/freetype/internal/sfnt.h",
                "include/freetype/internal/t1types.h",
                "include/freetype/internal/tttypes.h",
                "include/freetype/t1tables.h",
                "include/freetype/ttnameid.h",
                "include/freetype/tttables.h",
                "include/freetype/tttags.h",
                "include/freetype/ttunpat.h",
                "include/ft2build.h",
                "stdafx.h",
                "targetver.h",
                "src/autofit/autofit.c",
                "src/base/ftbase.c",
                "src/base/ftbbox.c",
                "src/base/ftbitmap.c",
                "src/base/ftfntfmt.c",
                "src/base/ftfstype.c",
                "src/base/ftgasp.c",
                "src/base/ftglyph.c",
                "src/base/ftgxval.c",
                "src/base/ftinit.c",
                "src/base/ftlcdfil.c",
                "src/base/ftmm.c",
                "src/base/ftotval.c",
                "src/base/ftpatent.c",
                "src/base/ftpfr.c",
                "src/base/ftstroke.c",
                "src/base/ftsynth.c",
                "src/base/ftsystem.c",
                "src/base/fttype1.c",
                "src/base/ftwinfnt.c",
                "src/bdf/bdf.c",
                "src/cache/ftcache.c",
                "src/cff/cff.c",
                "src/cid/type1cid.c",
                "src/gzip/ftgzip.c",
                "src/lzw/ftlzw.c",
                "src/pcf/pcf.c",
                "src/pfr/pfr.c",
                "src/psaux/psaux.c",
                "src/pshinter/pshinter.c",
                "src/psnames/psmodule.c",
                "src/raster/raster.c",
                "src/sfnt/sfnt.c",
                "src/smooth/smooth.c",
                "src/truetype/truetype.c",
                "src/type1/type1.c",
                "src/type42/type42.c",
                "src/winfonts/winfnt.c",
                "stdafx.cpp"
            ],
            "conditions": [
                ["OS != 'win'", {
                    "sources!": [
                      "stdafx.h",
                      "targetver.h",
                      "stdafx.cpp"
                    ]
                }]
            ],
            "msvs_configuration_attributes": {
                "CharacterSet": "1"
            }
        }
    ]
}
