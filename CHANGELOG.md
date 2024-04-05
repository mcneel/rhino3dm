# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.6.0-beta1] - Unreleased

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

