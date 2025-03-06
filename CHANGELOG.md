# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.17.0-beta] - 2025.03.05

diff: https://github.com/mcneel/rhino3dm/compare/8.9.0...8.17.0-beta

### Added

- (js, py) DimensionStyle.Id
- (js, py) Several delete methods for File3dm Tables: File3dmMaterialTable::Delete, BND_File3dmLayerTable::Delete, BND_File3dmDimStyleTable::Delete
- (js, py) Added tests for various ::Delete methods.
- (js, py) Extrusion::CreateWithPlane #636
- (js, py) BND_Bitmap also inherits from Common object and now exposes an Id property.
- (js, py) DimensionStyle now has an Id property
- (js, py) File3dm.ObjectTable.AddPoint now supports attributes #665 @StudioWEngineers
- (js, py) File3dm.ObjectTable.AddLine now supports attributes #666 @StudioWEngineers
- (js) Layer.Index #655
- (js) BND_PointCloud::CreateFromThreeJSON #642  @pedrocortesark
- (js) Added several methods and properties for Planes #568
- (js) Layer.index
- (js) Mesh.CreateFromThreeJSON inclides vertex color information
- (js) calling rhino.Version will now return the openNURBS version the library is built against
- (py) Improved stubs. WIP. #668, #669, #682 and #685 @StudioWEngineers
- (py) Added python 3.13 target #654
- (py) BND_MeshingParameters::Decode now supports more properties
- (py) Exposed a LightStyle enum which was previously only used internally

### Changed

- (py) switching from pybind11 to nanobind. WIP. This affects a lot of the src/binding files, which now include many `#if defined()`. When the switch is complete these will be cleaned up. This involved adding conditions for methods that returned BND_TUPLE and adding new methods for where we were using TUPLES as arrays. For this release, we still use pybind11.
- (py) BrepVertex.EdgeIndices() now returns a list
- (py) Curve.DerivitiveAt() now returns a list
- (py) File3dmObjectTable now accepts negative indexing #651 @StudioWEngineers
- (js) File3dm.objects().deleteItem(id) -> File3dm.objects().delete(id)
- (dotnet) Linux release builds in an Amazon Linux 2023 container

### Fixed

- (py) uuid conversion in c++ was broken
- (js, py) Changes to ViewInfo.Viewport would not set.
- (js) BND_Mesh::CreateFromThreeJSON did not pay attention to vertex colors #641
- (js) BND_PointCloud::CreateFromThreeJSON did not pay attention to RGBA (4 channel) colors #641

### Removed

- (py) GitHub is deprecating macos-12 runners, so they have been removed from the python builds


## [8.9.0] - 2024.07.19

diff: https://github.com/mcneel/rhino3dm/compare/8.6.1...8.9.0

See changes in 8.9.0-beta.

### Fixed

- (js) AnnotationBase objects would be undefined due to new cast to Text in the bindings but no Text class exposed to emscripten

## [8.9.0-beta] - 2024.07.12

diff: https://github.com/mcneel/rhino3dm/compare/8.6.1...8.9.0-beta

### Added
 - (py, js) CachedTextureCoordinates class
 - (py, js) Mesh.SetCachedTextureCoordinates and Mesh.GetCachedTextureCoordinates
 - (py, js) TextureMapping.HasId and TextureMapping.Id
 - (py, js) Added many Annotation classes and enums [#627](https://github.com/mcneel/rhino3dm/pull/627) @jesterKing
 - (all) A series of automated tests have been added and are run on all ci builds

 ### Changed
 - Updated OpenNURBS to v8.9 diff: https://github.com/mcneel/opennurbs/compare/v8.6.24101.05001...update-1718616159-8.9
 - (py, js) Several methods that take in arrays/lists of points have been updated to either be overloaded and take `Point3dList` and `std::vector<T>` (for py) or `emscripten::val` and the type is checked in cpp (for js). This means the SDK is not broken and users can pass in language specific lists / arrays of points. [#620](https://github.com/mcneel/rhino3dm/issues/620), [#616](https://github.com/mcneel/rhino3dm/issues/616)
 - (py) x86_64 Linux builds are now build using the manylinux_2_28_x86_64 docker image


 ### Fixed
 - (py, js) Polyline.CreateFromPoints now works [#616](https://github.com/mcneel/rhino3dm/issues/616)
 - (js) None of the PointCloud.AddRange* methods were working in js. This is fixed. [#620](https://github.com/mcneel/rhino3dm/issues/620)
 - (py, js) Polyline.GetSegments() always returned an extra NULL at the end of the segment array. [#623](https://github.com/mcneel/rhino3dm/issues/623)
 - (py) Universal builds for native library on macos. (#617)[https://github.com/mcneel/rhino3dm/pull/617] @jesterKing

 ### Removed
 - (py, dotnet) GitHub has deprecated building on macos-11 so from this point forward we will not build python wheels for macos-11. macos-14 will be used to build the dotnet library for macos.
 - (dotnet) GitHub deprecated actions that run on node.js < 20 which means that we cannot build on Amazon Linux 2 in with ci workflow. For now the ci linux build uses ubuntu-latest.

## [8.6.1] - 2024.05.10
diff: https://github.com/mcneel/rhino3dm/compare/8.6.0...8.6.1

### Added
- (py) macos 14 arm64 python wheels

### Fixed
- (js) Regenerated js files that load wasm as those checked into src were outdated.

## [8.6.0] - 2024.04.12
diff: https://github.com/mcneel/rhino3dm/compare/8.4.0...8.6.0

## [8.6.0-beta1] - 2024.04.05
diff: https://github.com/mcneel/rhino3dm/compare/8.4.0...8.6.0-beta1

### Added
- (py, js) CommonObject.IsValidWithLog that returns a tuple {bool valid, string log} [#598](https://github.com/mcneel/rhino3dm/issues/598)
- (dotnet, py, js) Material.RenderMaterialInstanceId [#596](https://github.com/mcneel/rhino3dm/issues/596)
- (py, js) InstanceDefinitionTable.Add [#436](https://github.com/mcneel/rhino3dm/issues/436) (see Changed below for consequential changes related to this)
- (py, js) File3dmObjectTable.AddInstanceObject()
- (py, js) EmbeddedFile.SetFilename.
- (js) EmbeddedFile.WasmFromByteArray() to add embedded from a js Uint8Array. [#523](https://github.com/mcneel/rhino3dm/issues/523)
- (py) EmbeddedFile.Read()

### Changed
- (js) js docs now use typedoc for generating documentation from `src/js/rhino3dm.d.ts` [#594](https://github.com/mcneel/rhino3dm/issues/594)
- (js, py) InstanceDefinitionTable.Add(idef) is now InstanceDefinitionTable.AddInstanceDefinition(idef). This aligns dotnet, js, and py InstanceDefinitionTable.Add method args.
- (py, js) EmbeddedFile.FileName is no longer read only and is Filename for py and fileName for js.

### Fixed
- (py, js) File3dmObjectTable.AddSurface was incorrectly calling File3dmObjectTable.AddSphere

## [8.6.0-beta] - 2024.03.15
diff: https://github.com/mcneel/rhino3dm/compare/8.4.0...8.6.0-beta

### Added
- (py, js) Added Annotation.PlainTextWithFields

### Fixed
- (dotnet, py, js) Annotation.PlainText, PlanTextWithFields, and RichText was returning empty strings, or gibberish across all languages and platforms. [#585](https://github.com/mcneel/rhino3dm/issues/585)

## [8.4.0] - 2024.02.19
diff: https://github.com/mcneel/rhino3dm/compare/8.0.1...8.4.0

### Added
- (py) added macos-12 to python builds
- (dotnet) File3dm.EarthAnchorPoint [#554](https://github.com/mcneel/rhino3dm/issues/554)
- (dotnet) Added File3dmMaterialTable.AddMaterial() that returns an index [#547](https://github.com/mcneel/rhino3dm/issues/547)
- (dotnet) Added File3dmGroupTable.Add() that returns an index [#417](https://github.com/mcneel/rhino3dm/issues/417)
- (py, js) File3dmObjectTable.Add( file3dmobject ) that returns an index [#517](https://github.com/mcneel/rhino3dm/issues/517)
- (dotnet) Sync'ed with Rhino DotNetSDK 8.4 and OpenNURBS 8.4
    - Include RevSurface.IsTransposed [#578](https://github.com/mcneel/rhino3dm/issues/578)

### Changed
- (py, js) Mesh::CreateFromSubDControlNet now has a second bool argument. If true, the resulting subd will include texture coordinates. [#573](https://github.com/mcneel/rhino3dm/issues/573)
- (py, js) Changed File3dmMaterialTable.Add() to return an int [#547](https://github.com/mcneel/rhino3dm/issues/547)


## [8.0.1] - 2023-11-17

### Changed
- (js) added -Oz flag for workflow_release builds resulting in a smaller size .wasm file (appx 3mb down from 10mb)

### Notes
- Using 8.0.1 because the js version was published as 8.0.0 then deprecated.

## [8.0.0-beta3] - 2023-09-10
diff: https://github.com/mcneel/rhino3dm/pull/567/files

### Added
- (dotnet) Added -l/--library flag to the build scripts for building only the native library.
- (dotnet) Added linux arm64 build in release workflow
- (dotnet) Build now generates XML documentation (#425)
- (dotnet) Added Extrusion.SetMesh() (#544)
- (js, py) Added Polyline.Append(points)
- (js, py) Added BrepFace.OrientationIsReversed property (#419)
- (js) Added Curve.createControlPointCurve static function
- (js) Added File3dm.Objects.AddPolyline (#559)
- (js) Added ViewInfo.Viewport (#302)

### Changed
- (py) Pypi release now includes manylinux bdist wheels cp38 - cp311 (#565)

### Fixed
- (dotnet) some runtime native libraries were corrupted (not dynamic libraries) due to packaging process. This should be fixed

## [8.0.0-beta2] - 2023-08-31
diff: https://github.com/mcneel/rhino3dm/pull/561/files
### Added
- (.net, js, py) RDK objects for ground plane, dithering, linear workflow, safe frame, skylight, sun, render channels, post effects, decals, render environments, render content, mesh modifiers
- (js, py) Polyline GetSegments and SegmentAt #534
- (js, py) NurbsCurve ConvertSpanToBezier
- (js, py) BrepVertex Index, EdgeCount, EdgeIndices
- (js, py) BrepVertexList Count, GetVertex
- (js, py) Brep Vertices
- (js, py) NurbsSurface OrderU, OrderV, KnotsU, KnotsV, Control Points, Points
- (js, py) ON_4fColor bindings
- (js, py) PBR BaseColor, EmissionColor, subsurfaceScatteringColor getter and setter
- (js) Vector2d bindings
- (js, py) Texture Id, Type, and Enabled getter and setter
- (js, py) SetMesh BrepFace and Extrusions
- (js) tryConvertBrep for Brep
- (js, py) Material.ToPhysicallyBased()
- (js, py) LineType bindings [@coditect](https://github.com/coditect)
- (js, py) BND_GroupTable Delete [@coditect](https://github.com/coditect)
- (js, py) BND_Xform methods and properties [@coditect](https://github.com/coditect)
- (js, py) additional file3dm properties [@coditect](https://github.com/coditect)
- (js, py) DateTime bindings [@coditect](https://github.com/coditect)
- (.net, js, py) Texture Repeat, Offset, and Rotation properties
- (js, py) Several methods related to PointClouds and values ( Add(point, value), etc )
- (js) toList() for NurbsSurfaceKnotList and NurbsCurveKnotList

### Changed
- (js) All File3dm table count are properties whereas before they were functions
- (js) PolyCurve Append methods are now appendArc, appendLine, and appendCurve. Related to #550
- (py) Updated PyBind11 from 2.9.1 to 2.11.1
- (js) CI builds for rhino3dm.js include debug information for debugging with Chrome. Hence, the resulting wasm file is much larger (40+mb) than the release build. This is triggered in the setup step with the -d or --debug flag: `python3 script/setup.py -p js -d`
- (js, py) bnd_anotationbase.cpp renamed to bnd_anotationbase.cpp

### Fixed
- (js, py) BND_Box.PointAt returned incorrect coordinates. #556
- (js) any method returning std::vector would be undefined. This was switched to return BND_TUPLE. No adverse effects on python build. #553
- (js) emscripten does not support overloaded ctors or methods that have the same number of arguments. This means many of the bindings had unusable ctors or methods. We've fixed this by giving each element a unique name (in the case of methods) or creating uniquely named static ctors. This will create some additional challenges for the documentation, but will result in more available ctors and methods. Meshes, ArcCurve, PolyCurve, Transforms, and several other objects were affected by this change. #550
- (js, py) BND_PointCloud::Add4 add color twice and does nothing with normal #551

## [8.0.0-beta.1] - 2023-04-17
### Added
- (.net) linux-arm64 native lib

### fixed
- (.net) linux-amd64 now loads in amazonlinux

## [8.0.0-beta] - 2023-03-22
### Added
- (js) ci and release builds include minified js and d.ts
- Updated rhino3dm to be based on the Rhino 8 version of OpenNURBS
- (py) Linux version now includes draco support
- (py) Added Line.Transform, Brep.TryConvertBrep
- (py) Added 3.11 support
- (.net) Added macOS arm64 (Apple Silicon) builds
- (.net) Addded .net 7.0 support

### Removed

- (py) Removed python 2.7 support

### Changed
- (js, py) Use draco 1.5.4
- (js) Fixed some issues in typescript binding generation

## [7.15.0] - 2022-03-23
### Added
- (js, py) Added ViewportInfo.TargetPoint
- (js, py) Added BrepFace.CreateExtrusion
- (.net) Added two new Hatch creation routines (CreateFromBrep and Create). The Create version takes curve loop inputs to create the hatch.

### Changed
- Updated opennurbs source to be based on Rhino 7.15 version

## [7.14.2] - 2022-03-22
### Added
- (js, py) Added Circle.Plane and Circle.BoundingBox properties; Circle.IsInPlane and Circle.Transform functions
- (py) Added Curve.ClosedCurveOrientation overload supporting an input plane. Added Curve.DerivativeAt
- (py) Added Vector3d.IsParallelTo and VectorAngle functions
- (py) Added Transform.Multiply as well as access properties to all values in the transform
- (.net) Add Curve.JoinCurves, Ellipse.Center, Ellipse.FocalDistance, ArcArc and CircleCircle intersections, NurbsCurve.Append
- (js, py) Added ModelComponent.DataCRC, ModelComponent.IsSystemComponent, ModelComponent.ClearId
- (py) Apple Silicon builds possible

### Changed
- Updated opennurbs source to be based on Rhino 7.14 version

## [7.11.0] - 2021-10-21
### Added
- (.net) Added Quaternion.GetRotation

### Changed
- Updated opennurbs source to be based on Rhino 7.11 version

## [7.7.0] - 2021-07-02
### Added
- (js, py) Circle.ClosestParameter, Light.GetSpotLightRadii, MeshFaceList.GetFaceVertices, MeshFaceList.GetFaceCenter, Sphere.ClosestParameter from [@fraguada](https://github.com/fraguada)

### Changed
- (js) BezierCurve.toNurbsCurve changed ToNurbsCurve to toNurbsCurve to stay consistent with function naming in library
### Fixed
- (js) File3dm.strings table was always reporting a count of 0 [@fraguada](https://github.com/fraguada)
- (py) Point3d.Transform was not callable

## [7.6.0] - 2021-05-28
### Added
- (js, py) AnnotationBase.RichText and PlainText properties
- (js, py) Arc.AngleDomain, StartAngle, EndAngle, StartAngleRadians, EndAngleRadians properties
- (js, py) Arc.ClosestParameter function
- (js, py) BezierCurve.ToNurbsCurve and Split function
- (js, py) Surface.SetDomain, GetSpanVector, IsoCurve, GetSurfaceParameterFromNurbsFormParameter, and GetNurbsFormParameterFromSurfaceParameter functions
- (js, py) Curve.TangentAtStart and TangentAtEnd properties
- (js, py) Curve.FrameAt, GetCurveParameterFromNurbsFormParameter, and GetNurbsFormParameterFromCurveParameter functions

### Changed
- Adjusted version number of library to be based on the underlying Rhino version that source is based on
- Use pybind11 2.6.1 for python compile

## [0.16.1] - 2021-05-25
### Fixed
- (js) Bumped version to 0.16.1 to fix missing file in npm distribution (javascript only)

## [0.16.0] - 2021-05-24
### Added
- (js, py) EarthAnchorPoint class
- (js, py) Surface.FrameAt function from [@fraguada](https://github.com/fraguada)
- (js, py) InstanceDefinition.SourceArchive and InstanceDefinition.UpdateType properties from [@s3ththompson](https://github.com/s3ththompson)
- (js, py) MeshTextureCoordinateList.Add function from [@GeertArien](https://github.com/GeertArien)
- (.NET) Updated all .NET classes/functions to match what has been added up to Rhino 7.6
- (js, py) Surface.Domain function from [@fraguada](https://github.com/fraguada)
- (js, py) File3dmLayerTable.AddLayer function
- (js, py) CommonObject.IsValid property

### Fixed
- (py) docgen generates a better typehint file for python to improve autocomplete in IDEs
- (js) Got web assembly compilation to work on Windows from [@kovacsv](https://github.com/kovacsv)
- (js) Fixed File3dm.ToByteArray memory corruption from [@kovacsv](https://github.com/kovacsv)
- (js, py) Get correct vertex colors from Draco compressed mesh from [@pearswj](https://github.com/pearswj)
- (.NET) Win32 native dlls included in nuget package

### Changed
- Based on public opennurbs from Rhino 7.6
- docgen now based on .NET 5 from [@pearswj](https://github.com/pearswj)
- Use emscripten 2.0.10 for web assembly compile from [@pearswj](https://github.com/pearswj)

## [0.14.0] - 2020-12-16
### Added
- (js/py) ViewInfo constructor
- (js/py) File3dmGroupTable.GroupMembers function
- (js/py) Transform.Translation, Scale and Mirror functions
- (js/py) BrepFace.DuplicateFace and BrepFace.DuplicateSutrface functions
- (js/py) Interval(double, double) constructor from [@pearswj](https://github.com/pearswj)
- (js/py) File3dm.Destroy function
- (js/py) Material.CompareAppearance function
- (js) Mesh.toThreejsJSONMerged function

### Changed
- Based on public opennurbs from Rhino 7.1
- Use pybind11 2.6.1 for python compile
- (py/js) Improve pointer tracking for CommonObject classes

## [0.13.0] - 2020-09-11
### Added
- (js/py) LightStyle enum
- (js/py) Light.LightStyle, Ambient, Diffuse and Specular properties
- (js/py) Mesh.CreateFromSubDControlNet function
- (js/py) Mesh.HasPrincipleCurvatures property
- (js/py) SubD class
- (js) PointCloud.toThreejsJSON function from [@fraguada](https://github.com/fraguada)

### Fixed
- (js) Mesh.thThreejsJSON function includes vertex colors when available from [@fraguada](https://github.com/fraguada)

### Changed
- Based on public opennurbs from Rhino 7.0

## [0.12.0] - 2020-06-27
## [0.11.0] - 2020-03-03
## [0.10.0] - 2020-01-29
## [0.9.0] - 2019-12-20
## [0.8.1] - 2019-10-30
## [0.8.0] - 2019-10-30
## [0.7.3] - 2019-10-11
## [0.7.2] - 2019-10-05
## [0.7.1] - 2019-09-05
## [0.7.0] - 2019-09-03
## [0.6.0] - 2019-09-01
## [0.5.0] - 2019-08-16
## [0.4.0] - 2019-04-29
## [0.3.1] - 2019-04-16
## [0.3.0] - 2019-04-10
## [0.2.1] - 2019-03-26
## [0.2.0] - 2019-03-26
## [0.1.9] - 2019-03-22
## [0.1.8] - 2019-02-13
## [0.1.7] - 2019-01-17
## [0.1.6] - 2019-01-11
## [0.1.5] - 2019-01-04
## [0.1.4] - 2018-12-06
## [0.1.3] - 2018-12-03
## [0.1.2] - 2018-12-01
## [0.1.1] - 2018-11-28
## [0.1.0] - 2018-11-06

