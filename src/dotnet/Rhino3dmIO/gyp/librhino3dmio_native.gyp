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
                        "defines": [ "WIN32", "DEBUG","_DEBUG"],
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
                        "msvs_configuration_platform": "x64",
                        "msvs_windows_sdk_version": "v8.1",
                        "msvs_target_platform_version": "v8.1",
                        "defines": [ "WIN64", "DEBUG","_DEBUG"],
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
                        "defines": ["WIN32", "NDEBUG"],
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
                        "defines": ["WIN64", "NDEBUG"]
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
                            "GCC_GENERATE_DEBUGGING_SYMBOLS": "YES",
                            "CLANG_CXX_LANGUAGE_STANDARD" : "c++14",
                            "CLANG_CXX_LIBRARY" : "libc++"
                        }
                    },
                    "Release": {
                        "defines": ["NDEBUG"],
                        "xcode_settings": {
                            "GCC_OPTIMIZATION_LEVEL": "3",
                            "GCC_GENERATE_DEBUGGING_SYMBOLS": "NO",
                            "DEAD_CODE_STRIPPING": "YES",
                            "GCC_INLINES_ARE_PRIVATE_EXTERN": "YES",
                            "CLANG_CXX_LANGUAGE_STANDARD" : "c++14",
                            "CLANG_CXX_LIBRARY" : "libc++"
                        }
                    }
                }
            }
        }]
    ],
    "targets": [
        {
            "target_name": "librhino3dmio_native",
            "type": "shared_library",
            "dependencies": [
                "./opennurbs_public/opennurbs_public.gyp:opennurbs_public_staticlib",
                "./opennurbs_public/zlib/zlib.gyp:zlib",
                "./opennurbs_public/freetype263/freetype263.gyp:freetype263_staticlib"
            ],
            "defines": ["RHINO3DMIO_BUILD"],
            "sources": [
                "on_3dm_attributes.cpp",
                "on_3dm_settings.cpp",
                "on_annotation2.cpp",
                "on_arc.cpp",
                "on_arccurve.cpp",
                "on_archive.cpp",
                "on_array.cpp",
                "on_beam.cpp",
                "on_bezier.cpp",
                "on_brep.cpp",
                "on_circle.cpp",
                "on_compstat.cpp",
                "on_curve.cpp",
                "on_defines.cpp",
                "on_detail.cpp",
                "on_dimension.cpp",
                "on_dimstyle.cpp",
                "on_file_utilities.cpp",
                "on_font.cpp",
                "on_geometry.cpp",
                "on_hatch.cpp",
                "on_hiddenlinedrawing.cpp",
                "on_instance.cpp",
                "on_intersect.cpp",
                "on_layer.cpp",
                "on_leader.cpp",
                "on_light.cpp",
                "on_line.cpp",
                "on_linecurve.cpp",
                "on_linetype.cpp",
                "on_massprop.cpp",
                "on_material.cpp",
                "on_mesh.cpp",
                "on_model_component.cpp",
                "on_nurbscurve.cpp",
                "on_nurbssurface.cpp",
                "on_object.cpp",
                "on_parse.cpp",
                "on_plane.cpp",
                "on_planesurface.cpp",
                "on_plus.cpp",
                "on_point.cpp",
                "on_pointcloud.cpp",
                "on_pointgeometry.cpp",
                "on_pointgrid.cpp",
                "on_polycurve.cpp",
                "on_polylinecurve.cpp",
                "on_progress_reporter.cpp",
                "on_quaternion.cpp",
                "on_revsurface.cpp",
                "on_rtree.cpp",
                "on_sphere.cpp",
                "on_string_value.cpp",
                "on_subd.cpp",
                "on_surface.cpp",
                "on_terminator.cpp",
                "on_text.cpp",
                "on_textlog.cpp",
                "on_textstyle.cpp",
                "on_userdata.cpp",
                "on_viewport.cpp",
                "on_xform.cpp",
                "stringholder.cpp",
                "plugin_linking_pragmas.h",
                "Resource.h",
                "rhcommon_c/rhcommon_c_api.h",
                "stdafx.cpp",
                "stdafx.h"
            ],
            "msvs_precompiled_header": "stdafx.h",
            "msvs_precompiled_source": "stdafx.cpp",
            "msvs_configuration_attributes": {
                "CharacterSet": "1"
            }
        }
    ]
}
