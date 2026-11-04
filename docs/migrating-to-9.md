# Migrating to rhino3dm 9.2

rhino3dm 9.2's Python and JavaScript bindings are generated from RhinoCommon, the .NET SDK of Rhino.
The names, signatures and behaviour of the API are RhinoCommon's, so the
[RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/) describes rhino3dm too.
The typings that ship with the packages (`rhino3dm/__init__.pyi` for Python, `rhino3dm.d.ts` for
JavaScript) list every member with its documentation.

rhino3dm 8.x was written by hand, and in many places it differed from RhinoCommon: other names,
other return shapes, other results. This guide lists every difference a script can notice, most
common first. The [appendix](#appendix-renamed-and-removed-members) maps each renamed or removed
member to its replacement.

## The changes most scripts hit

### Editing an object's geometry no longer changes the file

<!-- D1 -->
In 8.x, `obj.Geometry` was the geometry inside the file, so editing it edited the file. In 9.2 it is
a copy-on-write view, as in RhinoCommon: reading it reads the file, and the first edit makes a
private copy. The file does not change, and no error is raised.

```python
# 8.x: transformed the objects in the file
for obj in model.Objects:
    obj.Geometry.Transform(xform)

# 9.2: replace the object
for obj in list(model.Objects):
    geometry = obj.Geometry
    geometry.Transform(xform)
    model.Objects.Delete(obj.Id)
    model.Objects.Add(geometry, obj.Attributes)
```

`obj.Attributes` still writes through to the file. The same copy-on-write rule applies to
`BrepFace.UnderlyingSurface()`, `BrepFace.GetMesh(type)`, `Extrusion.GetMesh(type)` and
`Material.GetBitmapTexture()` (and the other texture getters). Put changes back with
`BrepFace.SetMesh(type, mesh)` or `Material.SetBitmapTexture(texture)`. A brep's surface cannot be
replaced outside Rhino, so edit the brep itself (for example `brep.Transform(xform)`).
<!-- D29, D30 -->

### Out parameters are a tuple, success first

<!-- D13 -->
A RhinoCommon method with `out` parameters returns a tuple in Python (an array in JS): the return
value first, then each `out` value in order. This includes failure.

```python
ok, plane = curve.TryGetPlane()
ok, left, right, bottom, top, near, far = viewport.GetFrustum()   # 8.x: a dict, raised on failure
```

### Writing files and adding table items

- `File3dm.Write(path)` with one argument is gone. Pass the version (`model.Write(path, 0)` for the
  current one) or a `File3dmWriteOptions`. In JS: `model.write(path, 0)`. <!-- D84 -->
- Table `Add(item)` (`model.Layers.Add(layer)`, `Materials`, `DimStyles`, `Linetypes`, ...) returns
  nothing, as in RhinoCommon. The new item's index is `len(model.Layers) - 1`, or use
  `FindName`/`FindIndex`. `model.Layers.AddLayer(name, color)` still returns the index. <!-- D85 -->
- Table `Delete` takes the item or its index: `model.Layers.Delete(layer)` returns `bool`, and
  `Delete(index)` raises for a bad index. `Objects.Delete(id)` is unchanged. <!-- D14 -->

### Methods that edit a value in place return nothing

<!-- D86, D5 -->
`Point3d.Transform(xform)` and `Vector3d.Transform(xform)` transform the value in place and return
`None`. In 8.35 they also returned the result. `Vector3d.Unitize()` returns `bool` (`False` for a
zero vector), not `None`.

### Errors are raised where RhinoCommon raises them

<!-- D9, D53 -->
RhinoCommon's argument checks run here too, as Python exceptions:

| .NET | Python |
|---|---|
| `IndexOutOfRangeException`, `ArgumentOutOfRangeException` | `IndexError` |
| `ArgumentNullException`, `ArgumentException` | `ValueError` |
| `InvalidOperationException`, `NotSupportedException` | `RuntimeError` |
| `FileNotFoundException` | `FileNotFoundError` |

Examples: `curve.SpanDomain(-1)`, `File3dm.Read('missing.3dm')`, `table.Delete(bad_index)`.

In JS a C++ exception used to abort the module. It is now a catchable `WebAssembly.Exception`: read
its text with `rhino.getExceptionMessage(e)`, and free it with `rhino.decrementExceptionRefcount(e)`,
or it leaks.

Where RhinoCommon accepts `null`, `None` is accepted and behaves as in .NET. For example
`PolyCurve.Append(None)` returns `False`, and `Objects.AddPoint(p, None)` uses the default
attributes. <!-- D10 -->

### Arrays

<!-- D87, D35 -->
Every array result is a Python list (8.x returned tuples in places), and a JS array. Where
RhinoCommon returns `null` (for example `BoundingBox.GetCorners()` of an invalid box, or
`Polyline.GetSegments()` with fewer than two points), the result is `None` / `null`. Wherever
RhinoCommon takes a sequence of points, Python accepts any sequence (a list, a tuple, a
`Point3dList`), and JS accepts an array. `GetUserStrings()` returns a list of `[key, value]` lists. <!-- D62 -->

## Enums

<!-- D76, D77, D18 -->
- Every enum is RhinoCommon's, with RhinoCommon's names and values. Twelve 8.x enums had no
  RhinoCommon namesake and are replaced:

  | 8.x | 9.2 Python | 9.2 JS |
  |---|---|---|
  | `AnnotationTypes` | `AnnotationType` | `AnnotationType` |
  | `ArrowheadTypes` | `DimensionStyle.ArrowType` | `DimensionStyleArrowType` |
  | `CurvePipingCapTypes` | `File3dmCurvePiping.CapTypes` | `File3dmCurvePipingCapTypes` |
  | `DecalMappings`, `Mappings` | `DecalMapping` | `DecalMapping` |
  | `DecalProjections`, `Projections` | `DecalProjection` | `DecalProjection` |
  | `DisplacementSweepResolutionFormulas` | `File3dmDisplacement.SweepResolutionFormulas` | `File3dmDisplacementSweepResolutionFormulas` |
  | `DitheringMethods` | `Dithering.Methods` | `DitheringMethods` |
  | `EnvironmentBackgroundProjections` | `Environment.BackgroundProjections` | `EnvironmentBackgroundProjections` |
  | `PostEffectTypes` | `PostEffectType` | `PostEffectType` |
  | `RenderChannelsModes` | `RenderChannels.Modes` | `RenderChannelsModes` |

  Python nests an enum in its class, as RhinoCommon does. JS has no nesting, so it joins the names.
- Some values are numbered differently: `Dithering.Methods` is `None = 0, FloydSteinberg = 1,
  SimpleNoise = 2` (8.x: `SimpleNoise = 0`), and `PostEffectType` is `Early = 0, ToneMapping = 1,
  Late = 2`. <!-- D36, D50 -->
- `LightStyle.Unknown` is `LightStyle.None`.
- Python: `None` is a keyword, so a value named `None` is also available as `None_`
  (`rhino3dm.LightStyle.None_`). `E['None']` and `getattr(E, 'None')` work too, and the value's name
  is still `'None'`.
- Enums are integer-like in Python (`int(e)` and `e == 2` work), but passing a plain int where an
  enum is expected raises `TypeError`. JS now checks too: a number or string where an enum is
  expected throws `TypeError` (8.x silently used 0). <!-- D55 -->

## JavaScript

<!-- D11, D59, D51 -->
- Value types (`Point3d`, `Vector3d`, `Interval`, `Plane`, `Transform`, `BoundingBox`, ...) are plain
  objects with RhinoCommon's field names: `{X, Y, Z}`, `{T0, T1}`, `{M00, ..., M33}`. In 8.x a
  `Point3d` was an array `[x, y, z]`. Their methods are static functions that take the value first:

  ```js
  const d = rhino.Point3d.distanceTo(p, q)
  const t = rhino.Transform.multiply(a, b)          // operators by RhinoCommon's names
  const box = rhino.BoundingBox.create(min, max)    // constructors are create(...)
  ```

  A method that changes the value returns `{self, returns}`, because a JS value is a copy:
  `const { self: unit, returns: ok } = rhino.Vector3d.unitize(v)`.
- Static properties are getters: `rhino.Plane.worldXY`, not `rhino.Plane.worldXY()`.
- Every member is camelCase. A name that a JS function already owns keeps RhinoCommon's spelling:
  `rhino.Vector3d.Length(v)`.
- Some invented names are gone: `addTriFace` / `addQuadFace` are `addFace`, and `appendLine` /
  `appendArc` / `appendCurve` are `append`. <!-- D20 -->
- `toByteArrayOptions(options)` is `toByteArray(options)`. <!-- D54 -->
- Views and table items (`model.objects`, `brep.faces.get(0)`, a layer from a table) keep their
  parent alive, so dropping the parent is safe. `parent.delete()` still frees it at once and
  invalidates its views. <!-- D52 -->
- Overloads that differ only in float versus double values (`Point3f` versus `Point3d`) keep the
  double one. <!-- D70 -->
- `SubDEdgeSharpness` is a plain object `{sharpness0, sharpness1}`; build one with
  `rhino.SubDEdgeSharpness.create(a, b)`, which checks the values as RhinoCommon does. <!-- D61 -->
- WebAssembly has no font engine, so text is not measured: annotation bounding boxes are empty
  (`textModelWidth` is -2). Python measures text. <!-- D90 -->

## Files and tables

- **Iteration** walks the live components (`for layer in model.Layers`, `for (const l of
  model.layers)`), skipping deleted ones. 8.x raised, or silently stopped, at the first hole.
  <!-- D6 -->
- **`FindIndex(i)`** returns `None` / `null` for a deleted or out-of-range index. `table[i]` still
  raises `IndexError` in Python. <!-- D7 -->
- **`model.Objects[i]`** is gone, as in RhinoCommon: iterate, or `list(model.Objects)[i]`.
  `len(model.Objects)` works. <!-- D17 -->
- **Table items are live proxies** into the file. A layer you hold reflects later edits to the file
  and raises `RuntimeError` once its layer is deleted from the file. In 8.x it kept stale data.
  <!-- D15 -->
- `model.Layers` and `model.AllLayers` both exist, as in RhinoCommon (also `Materials`, `Linetypes`,
  `DimStyles`, `InstanceDefinitions`, `HatchPatterns`). <!-- D16 -->
- `model.Strings` has RhinoCommon's members: `Count`, `GetKey(i)`, `GetValue(i)`, `GetValue(key)`,
  `SetString(key, value)`, `Delete(key)`. There is no `len` and no indexing. <!-- D82 -->
- **Bitmaps**: set `bitmap.FileFullPath = path`, then `model.Bitmaps.Add(bitmap)` returns the file's
  copy. `FindId` finds images, and `FindIndex(i)` / `[i]` give the i-th image. <!-- D43 -->
- **Decals**: `attributes.Decals` is iterable (`for d in attributes.Decals`), and hidden decals are
  skipped, as in RhinoCommon. `len(attributes.Decals)` (JS `attributes.decals.count`) counts what
  iteration yields; there is no index (use `list(attributes.Decals)[i]`). <!-- D31, D74 -->
- Objects come back as their most specific class: a plane surface is a `PlaneSurface`, not a
  `Surface`, so `isinstance` checks change. <!-- D8 -->
- `model.Views[i]` and `NamedViews[i]` are live: the same view while it is in the file, and a
  `RuntimeError` once it is not. <!-- D57 -->
- `File3dm.Manifest` lists every component of the file. Its images are `Bitmap` objects. <!-- D44 -->
- `File3dm.AllViews` and `AllNamedViews` are not available yet (`Views` and `NamedViews` are).
  <!-- D21 -->
- `File3dm.EmbeddedFilePaths()` is `File3dm.EmbeddedFiles` (each item's `Filename`). Paths are cleaned
  for the platform, so a path saved on Windows reads with `/` on Linux and macOS. <!-- D69 -->
- `File3dm.Created` and `LastEdited` are new: a `datetime.datetime` in Python and a `Date` in JS.
  <!-- D33 -->
- `File3dmObjectTable.Add(geometry, attributes)` adds leaders, text and radial or ordinate dimensions
  too, as 8.x did, although RhinoCommon throws for them. <!-- D71 -->

## Geometry

- **Values of invalid inputs follow RhinoCommon**. For example `Point3d.DistanceTo` of an unset point
  is `0.0` (8.x: `inf`). <!-- D2 -->
- `Curve.CreateControlPointCurve` makes RhinoCommon's curve, which differs from 8.x's for the same
  points. <!-- D3 -->
- `NurbsSurface.Create(...)` with invalid arguments returns `None`. <!-- D4 -->
- `Curve.DerivativeAt` returns `Vector3d`s (the point first, as a vector). <!-- D78 -->
- `mesh.Faces[i]` is a `MeshFace` (`A`, `B`, `C`, `D`, `IsQuad`), not a 4-tuple. <!-- D79 -->
- `MeshVertexNormalList.Add(x, y, z)` stores the vector as given; `UnitizeNormals()` unitizes.
  8.x unitized on add. <!-- D66 -->
- `IsValidWithLog` is a method: `ok, log = obj.IsValidWithLog()`. <!-- D80 -->
- `Polyline.GetSegments()` (8.x `GetSegments2()`) returns `Line` values, not `LineCurve` objects.
  `Point3dList.Append(points)` is `AddRange(points)`. <!-- D89 -->
- `Polyline` and `Point3dList` are RhinoCommon's lists, with all of `RhinoList`'s members
  (`AddRange`, `RemoveRange`, `IndexOf`, `Sort`, `Reverse`, `First`, `Last`, `ToArray`, ...). They
  are reference types: an assignment shares the list. In Python index with `[]` and `len()`; in JS
  use `get(i)`, `set(i, p)` and `count`. <!-- D37 -->
- `Transform` is a value type in Python, so assignment copies it. <!-- D12 -->
- Editing a `BrepEdge` or `BrepTrim` through a `Curve` method (`edge.Reverse()`) raises
  `RuntimeError`. <!-- D23 -->
- **SubD**: `subd.Faces`, `subd.Edges` and `subd.Vertices` are lists with `len`, iteration and
  `Find(id)`. A component's neighbours are `EdgeCount` plus `EdgeAt(i)` (also `VertexAt`, `FaceAt`).
  An edge's vertices are `VertexFrom` and `VertexTo`. The `First` / `Next` / `Current` cursors are
  gone. `SubDVertex.SurfacePoint()` is a method, and `Next`, `Previous` and `First` are properties.
  `SubD.SharpEdgeCount()` is a method. A component whose SubD changed raises `RuntimeError`.
  Components compare equal when they are the same component of the same SubD. <!-- D26, D88, D60, D24, D25 -->
- `SubDFace.IsNotConvex` and `IsNotPlanar(tol)` are gone: use `not IsConvex` and
  `not IsPlanar(tol)`. They differ only for a degenerate face; test `ControlNetCenterNormal.IsZero`
  first when that matters. <!-- D72 -->
- `SubDFace.PerFaceColor` and `BrepFace.PerFaceColor` of a face without one are `(0, 0, 0, 0)`
  (8.x `(255, 255, 255, 0)`), and they can be set. <!-- D28 -->
- `Mesh.GetCachedTextureCoordinates(id)` is a view into the mesh, not a copy. <!-- D27 -->
- `Mesh.GetCachedTextureCoordinatesFromTexture(file, objectId, texture)` is an overload of
  `GetCachedTextureCoordinates`, and `SetCachedTextureCoordinatesFromMaterial` returns nothing.
  <!-- D65 -->
- Constructors RhinoCommon does not have are gone: `Group()`, `Hatch()`, `RevSurface()`,
  `TextureMapping()`, `Decal()`, `Point()`. Use RhinoCommon's factories (`model.AllGroups.AddGroup`,
  `Hatch.CreateFromBrep`, `RevSurface.Create`, `TextureMapping.Create*Mapping`, `Point(location)`).
  <!-- D83 -->
- Some RhinoCommon overloads differ only in their `out` parameters, so Python and JS could reach only
  one of them. The others have names of their own: `NurbsCurvePointList.GetPoint4d`,
  `NurbsSurfacePointList.GetPoint4d`, `TextureMapping.TryGetMappingBoxCapped` (also `Cylinder`,
  `Plane`) and `Transform.DecomposeAffineFull`.

## Annotations

<!-- D39, D67, D68, D46 -->
- The explicit-parent accessors (`GetTextHeight(style)`, `SetMaskColor(style, color)`, ...) are gone.
  Set the parent, then use the property, as in RhinoCommon:

  ```python
  ann.ParentDimensionStyle = model.DimStyles.FindId(ann.DimensionStyleId)
  height = ann.TextHeight
  ```

  Pass the annotation's own style. Assigning a different style makes it the annotation's style and
  drops its overrides. Without a parent, these properties read the defaults, as in RhinoCommon. In
  JS the object the getter returns belongs to the annotation, so do not `delete()` it.
- `DimLinear.Points` (and the other dimensions' `Points` dicts) is `Get3dPoints()`, a tuple
  `(success, p1, p2, ...)`. `Leader.Points` is `Points3D` (and `Points2D`), and
  `DimAngular.Angle` is `NumericValue`.
- `Arrowhead` has its static `GetPoints(type)` only; its constructors and properties are not bound
  yet. <!-- D73 -->
- `TextHasRtfFormatting` is true for more than one text run. `PlainText` joins the runs, and for a
  dimension it returns the authored text (the `<>` formula), not the measurement.

## Rendering and settings

- `RenderContent.GetParameter(name)` returns the value by its type (`bool`, `int`, `float`, `str`,
  `uuid.UUID`, a tuple of doubles, `Color4f`, `Transform`, `bytes`), and `None` when there is no such
  parameter. 8.x always returned a string. `SetParameter` accepts the same values. <!-- D40 -->
- `RenderContent.TopLevel` of a grandchild is its parent, as in RhinoCommon. <!-- D41 -->
- `SetChildSlotOn(name, on)` and `SetChildSlotAmount(name, amount)` take the slot name first.
  `ChildSlotAmount` of an unset slot is `0.0`. <!-- D64 -->
- `RenderSettings.PostEffects` is a `PostEffectCollection` of `PostEffectData`: iterate it, or use
  `PostEffectDataFromId(id)`. <!-- D50 -->
- `PhysicallyBasedMaterial.BaseColor` (and `Emission`, `SubsurfaceScatteringColor`) is a `Color4f`
  (`R`, `G`, `B`, `A`), not a tuple. Python accepts a 4-sequence where a `Color4f` is expected. <!-- D34 -->
- **Sun**: the date is a `datetime.datetime` (JS `Date`): `sun.GetDateTime(DateTimeKind.Local)` and
  `sun.SetDateTime(d, DateTimeKind.Local)`, in place of `Year`, `Month`, `Day` and `Hours`. Times are
  whole seconds. `SunColorFromAltitude` is `ColorFromAltitude`, which rounds each channel (8.x
  truncated, so a channel can differ by 1). <!-- D45, D47, D48 -->
- `TextureMapping.RequiresVertexNormals` is gone. Use `MappingType`, `Projection` and `Capped`.
  <!-- D49 -->
- `ConstructionPlane` gains `ShowGrid`, `ShowAxes` and `ShowZAxis`. `File3dmWriteOptions` gains the
  mesh options. In JS both are classes (`new rhino.File3dmWriteOptions()`), not plain objects.
  <!-- D32 -->
- `ObjectAttributes.DrawColor(file)` for a colour by material uses Rhino's rules: the material the
  attributes select, and its PBR base colour when it is physically based. <!-- D63 -->

## Strings and values

- `str(point)` is RhinoCommon's `ToString()`: full precision (`0.30000000000000004`, where 8.x
  printed 6 significant digits). `str` works on every value type and on classes (`str(layer)` is its
  full path). <!-- D75 -->
- `repr(Point3d(1, 2, 3))` is `Point3d(1.0, 2.0, 3.0)`. <!-- D81 -->
- `ViewportInfo.ScreenPort` and `GetScreenPort()` are RhinoCommon's `(x, y, width, height)`, with y
  the smaller of top and bottom. A new `ViewportInfo()` reads `(0, 0, 1000, 1000)`. <!-- D58, D38 -->
- `File3dm.FromByteArray` takes any Python buffer (`bytes`, `bytearray`, numpy), and in JS a
  `Uint8Array`. <!-- D38 -->

## Object identity

<!-- section 4: D22, D25, D42, D56 -->
Contents and behaviour follow RhinoCommon, but object identity may not. Accessors such as
`obj.Geometry`, `brep.Faces`, `brep.Faces[0]` and `brep.Surfaces[i]` return a new view each time,
so `brep.Faces[0] is brep.Faces[0]` is `False`. Compare components by their index or id.
`Brep.TryConvertBrep(geometry)` on a file's brep returns a new view of it, not `geometry` itself.

## Python packaging

- The module is built with nanobind. On Python 3.12 and newer it is one stable-ABI (`abi3`) module,
  and it supports free-threaded CPython 3.14t.
- The package ships typings (`__init__.pyi`) with RhinoCommon's documentation, so editors complete
  and type-check rhino3dm code.

## Appendix: renamed and removed members

<!-- D19 -->
Members of 8.x that 9.2 replaces with RhinoCommon's (or removes). Members that rhino3dm keeps
alongside RhinoCommon's API are not listed. "JS only" marks a member that 8.x had in JavaScript
only.

<!-- retired:begin (tools/docs/migration.py) -->

| rhino3dm 8.x | 9.2 | Note |
|---|---|---|
| `AnnotationBase.GetAlternateDimensionLengthDisplay` | `AnnotationBase.AlternateDimensionLengthDisplay`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetDimensionLengthDisplay` | `AnnotationBase.DimensionLengthDisplay`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetDimensionScale` | `AnnotationBase.DimensionScale`, `AnnotationBase.ParentDimensionStyle` |  |
| `AnnotationBase.GetFont` | `AnnotationBase.Font`, `AnnotationBase.ParentDimensionStyle` |  |
| `AnnotationBase.GetMaskColor` | `AnnotationBase.MaskColor`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetMaskColorSource` | `AnnotationBase.MaskColorSource`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetMaskEnabled` | `AnnotationBase.MaskEnabled`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetMaskFrame` | `AnnotationBase.MaskFrame`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetMaskOffset` | `AnnotationBase.MaskOffset`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.GetTextHeight` | `AnnotationBase.TextHeight`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.RunReplace` | `AnnotationBase.RunReplace`, `AnnotationBase.ParentDimensionStyle` | ParentDimensionStyle = dimstyle; RunReplace(str, ...) -- RhinoCommon passes the parent style |
| `AnnotationBase.SetAlternateDimensionLengthDisplay` | `AnnotationBase.AlternateDimensionLengthDisplay`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetDimensionLengthDisplay` | `AnnotationBase.DimensionLengthDisplay`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetDimensionScale` | `AnnotationBase.DimensionScale`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetFont` | `AnnotationBase.Font`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style; ann.Font = font`; None raises, as C#'s NullReferenceException |
| `AnnotationBase.SetMaskColor` | `AnnotationBase.MaskColor`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetMaskColorSource` | `AnnotationBase.MaskColorSource`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetMaskEnabled` | `AnnotationBase.MaskEnabled`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetMaskFrame` | `AnnotationBase.MaskFrame`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetMaskOffset` | `AnnotationBase.MaskOffset`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `AnnotationBase.SetTextHeight` | `AnnotationBase.TextHeight`, `AnnotationBase.ParentDimensionStyle` | `ann.ParentDimensionStyle = style`, then the property; pass the annotation's own style |
| `Arc.AngleRadians` | `Arc.Angle` |  |
| `Arc.createFromPoints` | `Arc(...)` | JS only; Arc.create(p1, p2, p3) |
| `ArcCurve.CreateFromArc`, `ArcCurve.CreateFromArcParams`, `ArcCurve.CreateFromCircle`, `ArcCurve.CreateFromCircleParams` | `ArcCurve(...)` | the constructors ArcCurve(arc), ArcCurve(arc, t0, t1), ArcCurve(circle), ArcCurve(circle, t0, t1) |
| `ArchivableDictionary.writeGeometry` | removed |  |
| `BoundingBox.inflateXYZ` | `BoundingBox.Inflate` | JS only; inflate(box, x, y, z): the value is returned inflated |
| `BoundingBox.toJSON` | `BoundingBox.Encode` | JS only; a BoundingBox is a plain object JSON.stringify serializes as the old toJSON() did ({Min, Max}) |
| `Brep.CreateFromBoundingBox` | `Brep.CreateFromBox` | CreateFromBox(BoundingBox) and CreateFromBox(Box) |
| `BrepLoop.TrimCount` | `BrepLoop.Trims` | `loop.Trims.Count` |
| `BrepTrim.EdgeIndex`, `BrepTrim.StartVertexIndex`, `BrepTrim.EndVertexIndex` | `BrepTrim.Edge`, `BrepTrim.StartVertex`, `BrepTrim.EndVertex` | trim.Edge.EdgeIndex, trim.StartVertex.VertexIndex, trim.EndVertex.VertexIndex |
| `BrepVertex.EdgeCount` | `BrepVertex.EdgeIndices` | len(vertex.EdgeIndices()) |
| `CachedTextureCoordinates.Contains` | `CachedTextureCoordinates.Contains` | Contains(Point3d) |
| `CachedTextureCoordinates.IndexOf` | `CachedTextureCoordinates.IndexOf` | IndexOf(Point3d) |
| `Centermark.GetDisplayLines` | `Centermark.GetDisplayLines` |  |
| `CommonObject.GetUserString` | `GeometryBase.GetUserString`, `ObjectAttributes.GetUserString`, `Layer.GetUserString`, `Material.GetUserString`, `Linetype.GetUserString`, `Group.GetUserString`, `HatchPattern.GetUserString`, `DimensionStyle.GetUserString`, `InstanceDefinitionGeometry.GetUserString`, `TextureMapping.GetUserString`, `ViewportInfo.GetUserString`, `RenderSettings.GetUserString`, `Bitmap.GetUserString` |  |
| `CommonObject.GetUserStrings` | `GeometryBase.GetUserStrings`, `ObjectAttributes.GetUserStrings`, `Layer.GetUserStrings`, `Material.GetUserStrings`, `Linetype.GetUserStrings`, `Group.GetUserStrings`, `HatchPattern.GetUserStrings`, `DimensionStyle.GetUserStrings`, `InstanceDefinitionGeometry.GetUserStrings`, `TextureMapping.GetUserStrings`, `ViewportInfo.GetUserStrings`, `RenderSettings.GetUserStrings`, `Bitmap.GetUserStrings` |  |
| `CommonObject.GetUserStrings2` | `GeometryBase.GetUserStrings`, `ObjectAttributes.GetUserStrings` |  |
| `CommonObject.SetUserString` | `GeometryBase.SetUserString`, `ObjectAttributes.SetUserString`, `Layer.SetUserString`, `Material.SetUserString`, `Linetype.SetUserString`, `Group.SetUserString`, `HatchPattern.SetUserString`, `DimensionStyle.SetUserString`, `InstanceDefinitionGeometry.SetUserString`, `TextureMapping.SetUserString`, `ViewportInfo.SetUserString`, `RenderSettings.SetUserString`, `Bitmap.SetUserString` |  |
| `CommonObject.UserStringCount` | `GeometryBase.UserStringCount`, `ObjectAttributes.UserStringCount`, `Layer.UserStringCount`, `Material.UserStringCount`, `Linetype.UserStringCount`, `Group.UserStringCount`, `HatchPattern.UserStringCount`, `DimensionStyle.UserStringCount`, `InstanceDefinitionGeometry.UserStringCount`, `TextureMapping.UserStringCount`, `ViewportInfo.UserStringCount`, `RenderSettings.UserStringCount`, `Bitmap.UserStringCount` |  |
| `Curve.closedCurveOrientationPlane` | `Curve.ClosedCurveOrientation` | JS only; closedCurveOrientation(plane) |
| `Curve.derivativeAtSide` | `Curve.DerivativeAt` | JS only; derivativeAt(t, count, side) |
| `Curve.toNurbsCurveSubDomain` | `Curve.ToNurbsCurve` | JS only; toNurbsCurve(subdomain) |
| `Decal.BoundsMaxU` | `Decal.GetUVBounds` | GetUVBounds() -> (minU, minV, maxU, maxV) |
| `Decal.BoundsMaxV` | `Decal.GetUVBounds` | GetUVBounds() -> (minU, minV, maxU, maxV) |
| `Decal.BoundsMinU` | `Decal.GetUVBounds` | GetUVBounds() -> (minU, minV, maxU, maxV) |
| `Decal.BoundsMinV` | `Decal.GetUVBounds` | GetUVBounds() -> (minU, minV, maxU, maxV) |
| `Decal.HorzSweepEnd` | `Decal.HorzSweep` | HorzSweep() -> (start, end) |
| `Decal.HorzSweepStart` | `Decal.HorzSweep` | HorzSweep() -> (start, end) |
| `Decal.VertSweepEnd` | `Decal.VertSweep` | VertSweep() -> (start, end) |
| `Decal.VertSweepStart` | `Decal.VertSweep` | VertSweep() -> (start, end) |
| `DimAngular.Angle` | `Dimension.NumericValue` | the same Measurement() |
| `DimAngular.Points` | `AngularDimension.Get3dPoints` | Get3dPoints() -> (success, points...) in RhinoCommon's order |
| `DimLinear.Points` | `LinearDimension.Get3dPoints` | Get3dPoints() -> (success, points...) in RhinoCommon's order |
| `DimOrdinate.Points` | `OrdinateDimension.Get3dPoints` | Get3dPoints() -> (success, points...) in RhinoCommon's order |
| `DimRadial.Points` | `RadialDimension.Get3dPoints` | Get3dPoints() -> (success, points...) in RhinoCommon's order |
| `DimensionStyle.Field` | `DimensionStyle.Field` |  |
| `DracoCompression.compressOptions` | `DracoCompression.Compress` | JS only; compress(mesh, options): the same bytes |
| `Extrusion.CreateWithPlane` | `Extrusion.Create` | Create(curve, plane, height, cap) |
| `File3dm.EmbeddedFilePaths` | `File3dm.EmbeddedFiles` | [f.Filename for f in file.EmbeddedFiles]; a Windows path reads in this platform's form |
| `File3dm.EmbeddedFilePaths2` | `File3dm.EmbeddedFiles` | as EmbeddedFilePaths |
| `File3dm.RenderContent` | `File3dm.RenderMaterials`, `File3dm.RenderEnvironments`, `File3dm.RenderTextures` | one collection per kind of render content |
| `File3dm.encodeOptions` | `File3dm.Encode` | JS only; encode(options), one name |
| `File3dm.toByteArrayOptions` | `File3dm.ToByteArray` | JS only; toByteArray(options) |
| `File3dmDecalTable.FindIndex` | `Decals.__iter__` |  |
| `File3dmGroupTable.GroupMembers2` | `File3dmGroupTable.GroupMembers` |  |
| `File3dmGroupTable.deleteIndex` | `File3dmGroupTable.Delete` |  |
| `File3dmInstanceDefinitionTable.AddInstanceDefinition` | `File3dmInstanceDefinitionTable.Add` |  |
| `File3dmLinetypeTable.Current` | `File3dmSettings.CurrentLinePatternId` | the id; AllLinetypes.FindId(id) for the linetype |
| `File3dmLinetypeTable.CurrentSource` | `File3dmSettings.CurrentLinetypeSource` |  |
| `File3dmLinetypeTable.Scale` | `File3dmSettings.LinetypeDisplayScale` |  |
| `File3dmMeshModifiers.CreateCurvePiping` | `File3dmMeshModifiers.CreateCurvePiping` |  |
| `File3dmMeshModifiers.CreateDisplacement` | `File3dmMeshModifiers.CreateDisplacement` |  |
| `File3dmMeshModifiers.CreateEdgeSoftening` | `File3dmMeshModifiers.CreateEdgeSoftening` |  |
| `File3dmMeshModifiers.CreateShutLining` | `File3dmMeshModifiers.CreateShutLining` |  |
| `File3dmMeshModifiers.CreateThickening` | `File3dmMeshModifiers.CreateThickening` |  |
| `File3dmObjectTable.AddObject` | `File3dmObjectTable.Add` | Add(File3dmObject); Add(geometry, attributes) also takes any geometry |
| `File3dmObjectTable.AddPoint` | `File3dmObjectTable.AddPoint` | the (x, y, z, attributes) overload -> AddPoint(Point3d, attributes) |
| `File3dmObjectTable.addPointXYZ` | `File3dmObjectTable.AddPoint` | JS only; addPoint(x, y, z) |
| `File3dmPostEffectTable.FindId` | `PostEffectCollection.PostEffectDataFromId` | RenderSettings.PostEffects.PostEffectDataFromId(id) |
| `File3dmPostEffectTable.FindIndex` | `PostEffectCollection.__iter__` | the i-th item of RenderSettings.PostEffects, in the same order |
| `File3dmRenderContentTable.FindId` | `File3dmRenderMaterials.Find`, `File3dmRenderEnvironments.Find`, `File3dmRenderTextures.Find` | Find(id) on the kind's collection |
| `File3dmRenderContentTable.FindIndex` | `ManifestTable.FindIndex` |  |
| `File3dmSettings.EarthAnchorPoint` | `File3dm.EarthAnchorPoint` | on File3dm, a copy out and in |
| `File3dmSettings.ModelBasePoint` | `File3dmSettings.ModelBasepoint` | RhinoCommon spells it ModelBasepoint |
| `File3dmShutLiningCurveTable.Add` | `File3dmShutLining.AddCurve` | AddCurve(Guid curveId) -- a curve following the curve object curveId (no duplicate id) |
| `File3dmShutLiningCurveTable.FindId` | `File3dmShutLining.GetCurves` | curves are addressed by id (Curve*(id) / SetCurve*(id, v)) |
| `File3dmShutLiningCurveTable.FindIndex` | `File3dmShutLining.GetCurves` | GetCurves()[i] is the id |
| `GeometryBase.GetBoundingBox` | `GeometryBase.GetBoundingBox` |  |
| `GeometryBase.GetTightBoundingBox` | `GeometryBase.GetBoundingBox` | GetBoundingBox(true); an annotation answers with its own box |
| `Intersection.lineLineTolerance` | `Intersection.LineLine` |  |
| `Layer.PathSeparator` | `ModelComponent.NamePathSeparator` |  |
| `Leader.Points` | `Leader.Points3D` | Points3D (and Points2D, in the leader plane) |
| `Material.RenderMaterialInstanceId` | `Material.RenderMaterialInstanceId` |  |
| `Material.SetTexture` | `Material.SetTexture` |  |
| `Material.setBitmapTextureFilename` | `Material.SetBitmapTexture` |  |
| `Material.setBumpTextureFilename` | `Material.SetBumpTexture` |  |
| `Material.setEnvironmentTextureFilename` | `Material.SetEnvironmentTexture` |  |
| `Material.setTransparencyTextureFilename` | `Material.SetTransparencyTexture` |  |
| `Mesh.CreateFromSubDControlNet` | `Mesh.CreateFromSubDControlNet`, `Mesh.CreateFromSubDControlNetWithTextureCoordinates` |  |
| `Mesh.GetCachedTextureCoordinatesFromTexture` | `Mesh.GetCachedTextureCoordinates` | RhinoCommon's name, an overload of GetCachedTextureCoordinates |
| `Mesh.SetCachedTextureCoordinatesFromMaterial` | `Mesh.SetCachedTextureCoordinatesFromMaterial` | RhinoCommon returns nothing where the old returned bool |
| `MeshFaceList.addQuadFace` | `MeshFaceList.AddFace` |  |
| `MeshFaceList.addTriFace` | `MeshFaceList.AddFace` |  |
| `MeshFaceList.setQuadFace` | `MeshFaceList.SetFace` |  |
| `MeshFaceList.setTriFace` | `MeshFaceList.SetFace` |  |
| `MeshVertexList.AddPoint3d` | `MeshVertexList.Add` | Add(x, y, z) with doubles keeps double precision |
| `MeshVertexList.SetCount` | `MeshVertexList.Count` | RhinoCommon's settable Count; a negative count is ignored, as in C# |
| `MeshVertexList.SetPoint3dAt` | `MeshVertexList.SetVertex` | SetVertex(index, Point3d) |
| `NurbsCurveKnotList.ToList` | `NurbsCurveKnotList.__getitem__` | iterate the list |
| `NurbsCurveKnotList.ToList2` | `NurbsCurveKnotList.__getitem__` |  |
| `NurbsSurfaceKnotList.IsClampedEnd` | `NurbsSurfaceKnotList.ClampedAtEnd` |  |
| `NurbsSurfaceKnotList.IsClampedStart` | `NurbsSurfaceKnotList.ClampedAtStart` |  |
| `NurbsSurfaceKnotList.ToList` | `NurbsSurfaceKnotList.__getitem__` | iterate the list |
| `NurbsSurfaceKnotList.ToList2` | `NurbsSurfaceKnotList.__getitem__` |  |
| `NurbsSurfacePointList.MakeNonRational` | `NurbsSurface.MakeNonRational` | on the surface |
| `ObjectAttributes.DrawColor` | `ObjectAttributes.DrawColor` | Rhino's rules over the file; by-material now follows the material source and PBR base color |
| `ObjectAttributes.MeshModifiers` | `ObjectAttributes.File3dmMeshModifiers` | RhinoCommon's name |
| `PhysicallyBasedMaterial.EmissionColor` | `PhysicallyBasedMaterial.Emission` | a Color4f |
| `PhysicallyBasedMaterial.Supported` | `Material.IsPhysicallyBased` | on the material |
| `Point3dList.Append` | `Point3dList.Add` |  |
| `PointCloud.ContainsValues` | `PointCloud.ContainsPointValues` |  |
| `PointCloud.GetColors2` | `PointCloud.GetColors` |  |
| `PointCloud.GetNormals2` | `PointCloud.GetNormals` |  |
| `PointCloud.GetPoints2` | `PointCloud.GetPoints` |  |
| `PointCloud.GetValues` | `PointCloud.GetPointValues` |  |
| `PointCloud.GetValues2` | `PointCloud.GetPointValues` |  |
| `PointCloud.InsertRange` | `PointCloud.InsertRange` |  |
| `PointCloud.addPointColor` | `PointCloud.Add` | JS only; add(point, color) |
| `PointCloud.addPointNormal` | `PointCloud.Add` | JS only; add(point, normal) |
| `PointCloud.addPointNormalColor` | `PointCloud.Add` | JS only; add(point, normal, color) |
| `PointCloud.addPointNormalColorValue` | `PointCloud.Add` | JS only; add(point, normal, color, value) |
| `PointCloud.addRangePoints` | `PointCloud.AddRange` | JS only; addRange(points) |
| `PointCloud.addRangePointsColors` | `PointCloud.AddRange` | JS only; addRange(points, colors) |
| `PointCloud.addRangePointsNormals` | `PointCloud.AddRange` | JS only; addRange(points, normals) |
| `PointCloud.addRangePointsNormalsColors` | `PointCloud.AddRange` |  |
| `PointCloud.addRangePointsNormalsColorsValues` | `PointCloud.AddRange` | JS only; addRange(points, normals, colors, values) |
| `PointCloud.insertPointColor` | `PointCloud.Insert` | JS only; insert(index, point, color) |
| `PointCloud.insertPointNormal` | `PointCloud.Insert` | JS only; insert(index, point, normal) |
| `PointCloud.insertPointNormalColor` | `PointCloud.Insert` | JS only; insert(index, point, normal, color) |
| `PointCloud.insertPointNormalColorValue` | `PointCloud.Insert` | JS only; insert(index, point, normal, color, value) |
| `PolyCurve.Explode2` | `PolyCurve.Explode` |  |
| `PolyCurve.appendArc` | `PolyCurve.Append` | JS only; append(arc) |
| `PolyCurve.appendCurve` | `PolyCurve.Append` | JS only; append(curve) |
| `PolyCurve.appendLine` | `PolyCurve.Append` |  |
| `Polyline.ClosesPoint` | `Polyline.ClosestPoint` | the old name was a typo |
| `Polyline.CreateFromPoints` | `Polyline(...)` | Polyline(points) |
| `Polyline.GetSegments2` | `Polyline.GetSegments` |  |
| `PostEffect.GetParameter` | `PostEffectData.GetParameter` |  |
| `PostEffect.Id` | `PostEffectData.Id` |  |
| `PostEffect.LocalName` | `PostEffectData.LocalName` |  |
| `PostEffect.On` | `PostEffectData.On` |  |
| `PostEffect.SetParameter` | `PostEffectData.SetParameter` |  |
| `PostEffect.Shown` | `PostEffectData.Shown` |  |
| `PostEffect.Type` | `PostEffectData.Type` |  |
| `RenderContent.ChildSlotAmount` | `File3dmRenderContent.ChildSlotAmount` | RenderContent's shape, slot name first; ChildSlotAmount of an unset slot is 0.0, not 100.0 |
| `RenderContent.FirstChild` | `File3dmRenderContent.Children` |  |
| `RenderContent.NextSibling` | `File3dmRenderContent.Children` | see FirstChild |
| `RenderContent.SetChild` | `File3dmRenderContent.SetChild` | RenderContent's shape, slot name first; ChildSlotAmount of an unset slot is 0.0, not 100.0 |
| `RenderContent.SetChildSlotAmount` | `File3dmRenderContent.SetChildSlotAmount` | RenderContent's shape, slot name first; ChildSlotAmount of an unset slot is 0.0, not 100.0 |
| `RenderContent.SetChildSlotOn` | `File3dmRenderContent.SetChildSlotOn` | RenderContent's shape, slot name first; ChildSlotAmount of an unset slot is 0.0, not 100.0 |
| `RenderEnvironments.BackgroundId` | `RenderSettings.RenderEnvironmentId`, `RenderSettings.SetRenderEnvironmentId` | RenderEnvironmentId(EnvironmentUsage.Background, EnvironmentPurpose.Standard) / SetRenderEnvironmentId |
| `RenderEnvironments.ReflectionId` | `RenderSettings.RenderEnvironmentId`, `RenderSettings.SetRenderEnvironmentId` | RenderEnvironmentId(EnvironmentUsage.Reflection, EnvironmentPurpose.Standard) / SetRenderEnvironmentId |
| `RenderEnvironments.ReflectionOverride` | `RenderSettings.RenderEnvironmentOverride`, `RenderSettings.SetRenderEnvironmentOverride` | per usage, EnvironmentUsage.Reflection |
| `RenderEnvironments.SkylightingId` | `RenderSettings.RenderEnvironmentId`, `RenderSettings.SetRenderEnvironmentId` | RenderEnvironmentId(EnvironmentUsage.Skylighting, EnvironmentPurpose.Standard) / SetRenderEnvironmentId |
| `RenderEnvironments.SkylightingOverride` | `RenderSettings.RenderEnvironmentOverride`, `RenderSettings.SetRenderEnvironmentOverride` | per usage, EnvironmentUsage.Skylighting |
| `RenderSettings.RenderEnvironments` | `RenderSettings.RenderEnvironmentId`, `RenderSettings.RenderEnvironmentOverride` | per usage |
| `ShutLining.Curves` | `File3dmShutLining.GetCurves`, `File3dmShutLining.CurveRadius`, `File3dmShutLining.AddCurve` | GetCurves() ids, then CurveEnabled/CurveRadius/CurveProfile/CurvePull/CurveIsBump(id) and their setters; AddCurve() |
| `ShutLiningCurve.Enabled` | `File3dmShutLining.CurveEnabled`, `File3dmShutLining.SetCurveEnabled` | CurveEnabled(id) / SetCurveEnabled(id, v) |
| `ShutLiningCurve.Id` | `File3dmShutLining.GetCurves` | the getter is GetCurves()[i]; the SETTER (retarget) has no RhinoCommon counterpart -- DeleteAllCurves() + AddCurve(newId) () + the settings |
| `ShutLiningCurve.IsBump` | `File3dmShutLining.CurveIsBump`, `File3dmShutLining.SetCurveIsBump` | CurveIsBump(id) / SetCurveIsBump(id, v) |
| `ShutLiningCurve.Profile` | `File3dmShutLining.CurveProfile`, `File3dmShutLining.SetCurveProfile` | CurveProfile(id) / SetCurveProfile(id, v) |
| `ShutLiningCurve.Pull` | `File3dmShutLining.CurvePull`, `File3dmShutLining.SetCurvePull` | CurvePull(id) / SetCurvePull(id, v) |
| `ShutLiningCurve.Radius` | `File3dmShutLining.CurveRadius`, `File3dmShutLining.SetCurveRadius` | CurveRadius(id) / SetCurveRadius(id, v) |
| `Sphere.toJSON` | `Sphere.Encode` | JS only; a Sphere plain object stringifies as the old toJSON() ({EquatorialPlane, Radius}) |
| `SubD.EdgeCount` | `SubD.Edges` | `subd.Edges.Count`; a component's Id |
| `SubD.FaceCount` | `SubD.Faces` | `subd.Edges.Count`; a component's Id |
| `SubD.SharpEdgeCount` | `SubD.SharpEdgeCount` |  |
| `SubD.VertexCount` | `SubD.Vertices` | `subd.Edges.Count`; a component's Id |
| `SubDEdge.ControlNetDirection` | `SubDEdge.ControlNetLine` |  |
| `SubDEdge.ControlNetPoint` | `SubDEdge.VertexFrom`, `SubDEdge.VertexTo` |  |
| `SubDEdge.EndSharpness` | `SubDEdge.EndSharpness` |  |
| `SubDEdge.Index` | `SubDEdge.Id` | `subd.Edges.Count`; a component's Id |
| `SubDEdge.Vertex` | `SubDEdge.VertexFrom`, `SubDEdge.VertexTo` | index 0 / 1 |
| `SubDEdge.VertexCount` | `SubDEdge.VertexFrom`, `SubDEdge.VertexTo` | always 2 |
| `SubDEdge.VertexId` | `SubDEdge.VertexFrom`, `SubDEdge.VertexTo` | VertexId(0) / (1) -> VertexFrom.Id / VertexTo.Id |
| `face.Edges.Count` | `SubDFace.Edges` | face.Edges is a list: len(face.Edges) / .length |
| `face.Edges.First`, `face.Edges.Next`, `face.Edges.Last`, `face.Edges.Current`, `face.Edges.CurrentIndex` | removed | face.Edges is a list -- iterate it, or index it ([0], [-1] / [length - 1]) |
| `subd.Edges.Count` | `SubDEdgeList.Count` |  |
| `subd.Edges.First`, `subd.Edges.Next`, `subd.Edges.Last`, `subd.Edges.Current`, `subd.Edges.CurrentIndex` | removed | no RhinoCommon cursor; iterate subd.Edges |
| `vertex.Edges.Count` | `SubDVertex.EdgeCount` |  |
| `vertex.Edges.First`, `vertex.Edges.Next`, `vertex.Edges.Last`, `vertex.Edges.Current`, `vertex.Edges.CurrentIndex` | removed | no RhinoCommon cursor; vertex.EdgeAt(i) for i in range(vertex.EdgeCount) |
| `SubDFace.ControlNetCenterFrame` | `SubDFace.ControlNetCenterFrame` |  |
| `SubDFace.ControlNetCenterNormal` | `SubDFace.ControlNetCenterNormal` |  |
| `SubDFace.ControlNetCenterPoint` | `SubDFace.ControlNetCenterPoint` |  |
| `SubDFace.ControlNetPoint` | `SubDFace.VertexAt` |  |
| `SubDFace.Edge` | `SubDFace.EdgeAt` |  |
| `SubDFace.Index` | `SubDFace.Id` | `subd.Edges.Count`; a component's Id |
| `SubDFace.IsNotConvex` | removed |  |
| `SubDFace.IsNotPlanar` | removed |  |
| `SubDFace.Vertex` | `SubDFace.VertexAt` |  |
| `edge.Faces.Count` | `SubDEdge.Faces` | edge.Faces is a list: len(edge.Faces) / .length |
| `edge.Faces.First`, `edge.Faces.Next`, `edge.Faces.Last`, `edge.Faces.Current`, `edge.Faces.CurrentIndex` | removed | edge.Faces is a list -- iterate it, or index it ([0], [-1] / [length - 1]) |
| `subd.Faces.Count` | `SubDFaceList.Count` |  |
| `subd.Faces.First`, `subd.Faces.Next`, `subd.Faces.Last`, `subd.Faces.Current`, `subd.Faces.CurrentIndex` | removed | no RhinoCommon cursor; iterate subd.Faces |
| `vertex.Faces.Count` | `SubDVertex.Faces` | vertex.Faces is a list: len(vertex.Faces) / .length |
| `vertex.Faces.First`, `vertex.Faces.Next`, `vertex.Faces.Last`, `vertex.Faces.Current`, `vertex.Faces.CurrentIndex` | removed | vertex.Faces is a list -- iterate it, or index it ([0], [-1] / [length - 1]) |
| `SubDVertex.Edge` | `SubDVertex.EdgeAt` |  |
| `SubDVertex.Index` | `SubDVertex.Id` | `subd.Edges.Count`; a component's Id |
| `edge.Vertices.Count` | `SubDEdge.Vertices` | edge.Vertices is a list: len(edge.Vertices) / .length |
| `edge.Vertices.First`, `edge.Vertices.Next`, `edge.Vertices.Last`, `edge.Vertices.Current`, `edge.Vertices.CurrentIndex` | removed | edge.Vertices is a list -- iterate it, or index it ([0], [-1] / [length - 1]) |
| `face.Vertices.Count` | `SubDFace.Vertices` | face.Vertices is a list: len(face.Vertices) / .length |
| `face.Vertices.First`, `face.Vertices.Next`, `face.Vertices.Last`, `face.Vertices.Current`, `face.Vertices.CurrentIndex` | removed | face.Vertices is a list -- iterate it, or index it ([0], [-1] / [length - 1]) |
| `subd.Vertices.Count` | `SubDVertexList.Count` |  |
| `subd.Vertices.First` | `SubDVertexList.First` |  |
| `subd.Vertices.Next`, `subd.Vertices.Last`, `subd.Vertices.Current`, `subd.Vertices.CurrentIndex` | removed | no RhinoCommon cursor; iterate subd.Vertices |
| `Sun.Day` | `Sun.GetDateTime`, `Sun.SetDateTime` | GetDateTime(DateTimeKind.Local).day |
| `Sun.Hours` | `Sun.GetDateTime`, `Sun.SetDateTime` | hour + minute/60 + second/3600 of GetDateTime(DateTimeKind.Local): RhinoCommon's DateTime holds whole seconds |
| `Sun.Month` | `Sun.GetDateTime`, `Sun.SetDateTime` | GetDateTime(DateTimeKind.Local).month |
| `Sun.SunColorFromAltitude` | `Sun.ColorFromAltitude` |  |
| `Sun.Year` | `Sun.GetDateTime`, `Sun.SetDateTime` |  |
| `Surface.GetSpanVector2` | `Surface.GetSpanVector` |  |
| `Surface.toNurbsSurfaceTolerance` | `Surface.ToNurbsSurface` | JS only; toNurbsSurface(tolerance) -> [surface, accuracy] |
| `TextureMapping.IsPeriodic` | `TextureMapping.MappingType` |  |
| `TextureMapping.RequiresVertexNormals` | `TextureMapping.MappingType`, `TextureMapping.Projection`, `TextureMapping.Capped` | not SurfaceParameters and (Projection == Ray, BoxMapping, or a capped CylinderMapping); a WCS-box projection makes MappingType throw, as RhinoCommon's |
| `TextureMapping.ReverseTextureCoordinate` | `TextureMapping.UvwTransform` |  |
| `TextureMapping.SwapTextureCoordinate` | `TextureMapping.UvwTransform` | UvwTransform = S * UvwTransform, S the identity with rows i and j swapped |
| `TextureMapping.TileTextureCoordinate` | `TextureMapping.UvwTransform` | UvwTransform = T * UvwTransform, T the identity with T[dir][dir] = count, T[dir][3] = offset |
| `Transform.ToFloatArray2` | `Transform.ToFloatArray` |  |
| `Transform.mirrorPlane` | `Transform.Mirror` | JS only; mirror(plane) |
| `Transform.rotationVectors` | `Transform.Rotation` | JS only; rotation(start, end, center) |
| `Transform.translationVector` | `Transform.Translation` | JS only; translation(vector) |
| `Transform.translationXYZ` | `Transform.Translation` | JS only; translation(x, y, z) |
| `Vector3d.DotProduct` | `Vector3d * ` | the * operator: a * b |
| `ViewInfo.WallpaperName` | `ViewInfo.WallpaperFilename` |  |
| `ViewportInfo.screenPort` | `ViewportInfo.ScreenPort` | JS only; the same (x, y, width, height) set; the read differs for a y-up or degenerate port |

<!-- retired:end -->
