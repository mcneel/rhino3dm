# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.0.0-beta.2] - unreleased
### Added
- (.net, js, py) Added RDK objects for ground plane, dithering, linear workflow, safe frame, skylight, sun, render channels, post effects, decals, render environments.
- (js, py) Polyline GetSegments and SegmentAt
- (js, py) NurbsCurve ConvertSpanToBezier
- (js, py) BrepVertex Index, EdgeCount, EdgeIndices
- (js, py) BrepVertexList Count, GetVertex
- (js, py) Brep Vertices
- (js, py) NurbsSurface OrderU, OrderV, KnotsU, KnotsV, Control Points, Points
- (js, py) ON_4fColor bindings
- (js, py) PBR BaseColor, EmissionColor, subsurfaceScatteringColor getter and setter
- (js) Vector2d bindings
- (js, py) Texture Id, Type, and Enabled getter and setter
- (js, py) SetMesh Breps
- (js) tryConvertBrep for Brep
- (js, py) ToPhysicallyBased
- (js, net, py) Added RenderContent
- (js, py) Added LineType bindings (@coditect)
- (js, py) Added BND_GroupTable Delete (@coditect)
- (js, py) Added BND_Xform methods and properties (@coditect)
- (js, py) Added additional file3dm properties (@coditect)

### Changed
- (js) All File3dm table count are properties whereas before they were functions
- (js) PolyCurve Append methods are now appendArc, appendLine, and appendCurve

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
- (js) Fixed some issues in typescript binding generationcd sel

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

