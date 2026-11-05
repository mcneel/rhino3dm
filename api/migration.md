<!--
The source of the Python and JavaScript migration guides, docs/migrating-to-9/python.md and
javascript.md. tools/docs/migration.py renders one page per language (`--check`: stale pages):

  - plain text goes to both pages;
  - a ```python fence goes to the Python page only, a ```js fence to the JavaScript page only;
  - text between the comment markers "py" and "/py" (or "js" and "/js") goes to that language only:
    a word, a bullet or a whole section;
  - [[python text || js text]] picks one side per page;
  - a heading whose section ends up empty is dropped.

Each change is written once, so the pages cannot disagree. The Dn comments tie a paragraph to its
row in docs/vnext/deviations.md and stay in the pages (invisible).
-->

# Migrating to rhino3dm 9.2 ([[Python || JavaScript]])

[← Other languages](../migrating-to-9.md)

rhino3dm 9.2's [[Python || JavaScript]] bindings are generated from RhinoCommon, the .NET SDK of Rhino,
so the API is RhinoCommon's: its names, signatures and behaviour. The
[RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/) describes rhino3dm too, and
the typings that ship with the package ([[`rhino3dm/__init__.pyi` || `rhino3dm.d.ts`]]) list every
member with its documentation.

rhino3dm 8.x was written by hand and often differed from RhinoCommon. Start with the changes below:
they are the ones most scripts hit. [[Renamed and removed members](python-members.md) || [Renamed and removed members](javascript-members.md)]]
maps each 8.x member that is gone to its replacement.

[The changes most scripts hit](#the-changes-most-scripts-hit) ·
<!-- js -->[Values and objects](#values-and-objects) · <!-- /js -->
[Files and tables](#files-and-tables) · [Geometry](#geometry) · [Annotations](#annotations) ·
[Rendering and settings](#rendering-and-settings) · [Enums](#enums) ·
[Errors, values and identity](#errors-values-and-identity) · [Packaging](#packaging)

## The changes most scripts hit

<!-- js -->
### 1. Value types are plain objects

<!-- D11 -->
`Point3d`, `Vector3d`, `Interval`, `Plane`, `Transform`, `BoundingBox` and the other value types are
plain objects with RhinoCommon's field names (`{X, Y, Z}`, `{T0, T1}`), not arrays. Their methods are
functions on the type that take the value first:

```js
// 8.x
const p = [1, 2, 3]
// 9.2
const p = { X: 1, Y: 2, Z: 3 }
const d = rhino.Point3d.distanceTo(p, q)
```

See [Values and objects](#values-and-objects) for constructors, operators and the methods that change
a value.

### 2. Properties are properties

<!-- D91 -->
Where RhinoCommon has a property, so does JS. In 8.x many were functions:

```js
// 8.x
for (let i = 0; i < doc.objects().count; i++) {
  const geometry = doc.objects().get(i).geometry()
}
// 9.2
for (const obj of doc.objects) {
  const geometry = obj.geometry
}
```

The same goes for `doc.layers`, `doc.materials` and the other tables, `obj.attributes`,
`mesh.vertices`, `mesh.faces`, `brep.faces`, `nurbsCurve.points`, `subd.faces`, and statics such as
`Linetype.continuous`, `MeshingParameters.default` and `ViewportInfo.defaultTop` (66 members).
Calling one throws `TypeError: ... is not a function`. Five went the other way and are now
functions: `isValidWithLog()`, `subd.sharpEdgeCount()`, `vertex.surfacePoint()`,
`trim.isReversed()`, `vertex.edgeIndices()`.
<!-- /js -->

### [[1 || 3]]. Editing an object's geometry no longer changes the file

<!-- D1 -->
[[`obj.Geometry` || `obj.geometry`]] is a copy-on-write view, as in RhinoCommon: the first edit makes a
private copy, and the file stays as it was (no error). To change the file, replace the object:

```python
# 8.x
for obj in model.Objects:
    obj.Geometry.Transform(xform)

# 9.2
for obj in list(model.Objects):
    geometry = obj.Geometry
    geometry.Transform(xform)
    model.Objects.Delete(obj.Id)
    model.Objects.Add(geometry, obj.Attributes)
```

```js
// 8.x
const objects = doc.objects()
for (let i = 0; i < objects.count; i++)
  objects.get(i).geometry().transform(xform)

// 9.2
for (const obj of [...doc.objects]) {
  const geometry = obj.geometry
  geometry.transform(xform)
  doc.objects.delete(obj.id)
  doc.objects.add(geometry, obj.attributes)
}
```

[[`obj.Attributes` || `obj.attributes`]] still writes through. Faces, meshes and textures work the same
way ([Geometry](#all-geometry)).

### [[2 || 4]]. `out` parameters come back as [[a tuple || an array]], success first

<!-- D13 -->
```python
ok, plane = curve.TryGetPlane()
ok, left, right, bottom, top, near, far = viewport.GetFrustum()   # 8.x: a dict
```

```js
const [ok, plane] = curve.tryGetPlane()
const [ok2, left, right, bottom, top, near, far] = viewport.getFrustum()   // 8.x: an object
```

### [[3 || 5]]. Editing tables

<!-- D84, D85, D14 -->
```python
model.Write(path, 0)            # 8.x: model.Write(path); 0 is the current version
model.Layers.Add(layer)         # returns nothing now; the index is len(model.Layers) - 1
model.Layers.Delete(layer)      # or Delete(index), which raises for a bad index
```

```js
doc.layers.add(layer)           // returns nothing now; the index is doc.layers.count - 1
doc.layers.delete(layer)        // or delete(index), which throws for a bad index
```

[[`model.Layers.AddLayer(name, color)` || `doc.layers.addLayer(name, color)`]] still returns the index.
The same goes for the other tables (materials, linetypes, dimension styles, ...).

<!-- py -->
### 4. In-place methods return nothing

<!-- D86, D5 -->
```python
p.Transform(xform)      # edits p, returns None (8.35 also returned the result)
ok = v.Unitize()        # bool: False for a zero vector (8.x: None)
```
<!-- /py -->

### [[5 || 6]]. Bad arguments [[raise || throw]]

<!-- D9, D53 -->
RhinoCommon's argument checks run here too: [[`curve.SpanDomain(-1)` raises `IndexError`, and
`File3dm.Read('missing.3dm')` raises `FileNotFoundError` || `curve.spanDomain(-1)` throws]].
[[See [Errors](#errors) for how .NET's exceptions map to Python's. || In 8.x an error inside the
module aborted it; now it is an exception you can catch ([Errors](#errors)).]]

### [[6 || 7]]. [[Arrays are lists || Null arrays are `null`]]

<!-- D87, D35 -->
[[Array results are lists (8.x returned tuples in places), and `None` where RhinoCommon returns `null` ||
Where RhinoCommon returns a `null` array, JS gets `null`, not an empty array]], for example
[[`BoundingBox.GetCorners()` || `rhino.BoundingBox.getCorners(box)`]] of an invalid box.

<!-- js -->
## Values and objects

<!-- D11, D59, D51, D92 -->
- A value type is built with `new`, as in 8.x and RhinoCommon, and gives a plain object
  (`rhino.T.create(...)` is the same call; `instanceof rhino.T` is false). Its operators are functions with RhinoCommon's names:

  ```js
  const box = new rhino.BoundingBox(min, max)   // { Min: {...}, Max: {...} }
  const t = rhino.Transform.multiply(a, b)
  const v = rhino.Vector3d.add(a, b)
  ```

- A method that changes the value in C# changes the object you pass, and returns what C# returns:

  ```js
  rhino.Point3d.transform(point, xform)        // point is moved
  const ok = rhino.Vector3d.unitize(vector)    // vector is unitized; ok is false for a zero vector
  ```

- Static properties are getters: `rhino.Plane.worldXY`, not `rhino.Plane.worldXY()`.
- Every member is camelCase. A name that a JS function already owns keeps RhinoCommon's spelling:
  `rhino.Vector3d.Length(v)`.
- Overloads share one name, chosen by the arguments: `addTriFace` / `addQuadFace` are `addFace`,
  `appendLine` / `appendArc` / `appendCurve` are `append`, and `toByteArrayOptions(options)` is
  `toByteArray(options)`. <!-- D20, D54 -->
- Overloads that differ only in float versus double values (`Point3f` versus `Point3d`) keep the
  double one. <!-- D70 -->
- Views and table items (`doc.objects`, `brep.faces.get(0)`, a layer from a table) keep their parent
  alive, so dropping the parent is safe. `parent.delete()` still frees it at once and invalidates its
  views. <!-- D52 -->
- `SubDEdgeSharpness` is a plain object `{sharpness0, sharpness1}`; build one with
  `new rhino.SubDEdgeSharpness(a, b)`, which checks the values as RhinoCommon does. <!-- D61 -->
<!-- /js -->

## Files and tables

- **Iteration** walks the live components ([[`for layer in model.Layers` || `for (const layer of
  doc.layers)`]]), skipping deleted ones. 8.x [[raised || silently stopped]] at the first deleted item.
  <!-- D6 -->
- <!-- py -->**`FindIndex(i)`** returns `None` for a deleted or out-of-range index. `table[i]` still
  raises `IndexError`. <!-- D7 --><!-- /py --><!-- js -->**`doc.objects.get(i)`** is gone, as in
  RhinoCommon: iterate, or `[...doc.objects][i]`. `doc.objects.count` works. <!-- D17 --><!-- /js -->
- <!-- py -->**`model.Objects[i]`** is gone, as in RhinoCommon: iterate, or `list(model.Objects)[i]`.
  `len(model.Objects)` works. <!-- D17 --><!-- /py -->
- **Table items are live.** A layer you hold reflects later edits to the file, and
  [[raises `RuntimeError` || throws]] once its layer is deleted from the file. In 8.x it kept stale
  data. <!-- D15 -->
- [[`model.Layers` and `model.AllLayers` || `doc.layers` and `doc.allLayers`]] both exist, as in
  RhinoCommon (also materials, linetypes, dimension styles, instance definitions and hatch patterns).
  <!-- D16 -->
- [[`model.Strings` || `doc.strings`]] has RhinoCommon's members: [[`Count`, `GetKey(i)`,
  `GetValue(i)`, `GetValue(key)`, `SetString(key, value)`, `Delete(key)`. There is no `len` and no
  indexing. || `count`, `getKey(i)`, `getValue(i)`, `getValue(key)`, `setString(key, value)`,
  `delete(key)`.]] <!-- D82 -->
- **Bitmaps**: set [[`bitmap.FileFullPath = path` || `bitmap.fileFullPath = path`]], then
  [[`model.Bitmaps.Add(bitmap)` || `doc.bitmaps.add(bitmap)`]] returns the file's copy.
  [[`FindId` || `findId`]] finds images, and [[`FindIndex(i)` / `[i]` || `findIndex(i)`]] give the
  i-th image. <!-- D43 -->
- **Decals**: iterate them ([[`for d in attributes.Decals` || `for (const d of
  attributes.decals)`]]); hidden decals are skipped, as in RhinoCommon. [[`len(attributes.Decals)` ||
  `attributes.decals.count`]] counts what iteration yields. There is no index: use
  [[`list(attributes.Decals)[i]` || `[...attributes.decals][i]`]]. <!-- D31, D74 -->
- Objects come back as their most specific class: a plane surface is a `PlaneSurface`, not a
  `Surface`, so [[`isinstance` || `instanceof`]] checks change. <!-- D8 -->
- [[`model.Views[i]` and `NamedViews[i]` are || `doc.views.get(i)` and `doc.namedViews.get(i)` are]]
  live: the same view while it is in the file, and [[a `RuntimeError` || an exception]] once it is
  not. <!-- D57 -->
- [[`File3dm.Manifest` || `doc.manifest`]] lists every component of the file. Its images are `Bitmap`
  objects. <!-- D44 -->
- [[`File3dm.AllViews` and `AllNamedViews` are || `doc.allViews` and `doc.allNamedViews` are]] not
  available yet ([[`Views` and `NamedViews` || `views` and `namedViews`]] are). <!-- D21 -->
- [[`File3dm.EmbeddedFilePaths()` is `File3dm.EmbeddedFiles` (each item's `Filename`) ||
  `doc.embeddedFilePaths()` is `doc.embeddedFiles` (each item's `filename`)]]. Paths are cleaned for
  the platform, so a path saved on Windows reads with `/` on Linux and macOS. <!-- D69 -->
- [[`File3dm.Created` and `LastEdited` are new: `datetime.datetime` values || `doc.created` and
  `doc.lastEdited` are new: `Date` values]]. <!-- D33 -->
- [[`model.Objects.Add(geometry, attributes)` || `doc.objects.add(geometry, attributes)`]] adds
  leaders, text and radial or ordinate dimensions too, as 8.x did, although RhinoCommon throws for
  them. <!-- D71 -->
- [[`ConstructionPlane` and `File3dmWriteOptions` || `ConstructionPlane` and `File3dmWriteOptions`
  are classes (`new rhino.File3dmWriteOptions()`), not plain objects. They]] gain RhinoCommon's members:
  [[`ShowGrid`, `ShowAxes`, `ShowZAxis` || `showGrid`, `showAxes`, `showZAxis`]], and the write
  options' mesh options. <!-- D32 -->

## Geometry

### All geometry

<!-- D29, D30 -->
- **Copy-on-write.** Like [[`obj.Geometry` || `obj.geometry`]], [[`BrepFace.UnderlyingSurface()`,
  `BrepFace.GetMesh(type)`, `Extrusion.GetMesh(type)` and `Material.GetBitmapTexture()` ||
  `face.underlyingSurface()`, `face.getMesh(type)`, `extrusion.getMesh(type)` and
  `material.getBitmapTexture()`]] (and the other texture getters) return copy-on-write views: an edit
  does not reach the file. Put changes back with [[`BrepFace.SetMesh(type, mesh)` or
  `Material.SetBitmapTexture(texture)` || `face.setMesh(type, mesh)` or
  `material.setBitmapTexture(texture)`]].
- **Values of invalid inputs follow RhinoCommon.** For example the distance to an unset point is `0.0`
  (8.x: infinity). <!-- D2 -->
- <!-- py -->`IsValidWithLog` is a method: `ok, log = obj.IsValidWithLog()`.<!-- /py --><!-- js -->`isValidWithLog()` is a function returning `[ok, log]`.<!-- /js --> <!-- D80 -->
- Constructors RhinoCommon does not have are gone: `Group()`, `Hatch()`, `RevSurface()`,
  `TextureMapping()`, `Decal()`, `Point()`. Use RhinoCommon's factories ([[`model.AllGroups.AddGroup` ||
  `doc.allGroups.addGroup`]], [[`Hatch.CreateFromBrep` || `rhino.Hatch.createFromBrep`]],
  [[`RevSurface.Create` || `rhino.RevSurface.create`]], [[`TextureMapping.Create*Mapping` ||
  `rhino.TextureMapping.create*Mapping`]], [[`Point(location)` || `new rhino.Point(location)`]]).
  <!-- D83 -->
- Some RhinoCommon overloads differ only in their `out` parameters, so [[Python || JS]] can reach only
  one of them under RhinoCommon's name. The others have names of their own, listed in each section
  below.

### Points, vectors and transforms

<!-- py -->
- `Transform` is a value type, so assignment copies it. <!-- D12 -->
- `Point3d.Transform` and `Vector3d.Unitize` change the value in place: see
  [the changes most scripts hit](#4-in-place-methods-return-nothing).
- `Plane.WorldXY`, `Transform.Identity`, `Vector3d.Length` and the other members that are properties
  in RhinoCommon are properties: `Plane.WorldXY`, not `Plane.WorldXY()`. <!-- D91 -->
<!-- /py -->
- [[`Transform.DecomposeAffine` || `rhino.Transform.decomposeAffine`]] with all four `out` values is
  [[`Transform.DecomposeAffineFull` || `rhino.Transform.decomposeAffineFull`]].

### Curves

- [[`Curve.CreateControlPointCurve(points, degree)` || `rhino.Curve.createControlPointCurve(points,
  degree)`]] builds the curve as RhinoCommon does. Two points still give a `LineCurve`, and degree 1
  still gives a `PolylineCurve`. For a NURBS curve: <!-- D3 -->
  - **The domain is `0` to the length of the control polygon.** 8.x used knot spacing 1, so the domain
    was `0` to the number of points minus the degree. The shape is the same, but every parameter
    changes: [[`PointAt(t)`, `Domain` || `pointAt(t)`, `domain`]] and closest-point results.
  - **A closed point list (last point equal to the first) gives a smooth periodic curve.** 8.x made a
    clamped curve with a kink where it closes.
  - **Too few points for the degree lowers the degree** to the number of points minus one. 8.x
    returned [[`None` || `null`]]. The degree is also clamped to 1–11.
- [[`Curve.DerivativeAt` || `curve.derivativeAt`]] returns `Vector3d`s (the point first, as a vector).
  <!-- D78 -->
- [[`NurbsCurvePointList.GetPoint(i)` || `points.getPoint(i)`]] returning a `Point4d` is
  [[`GetPoint4d(i)` || `getPoint4d(i)`]].

### Polylines and point lists

- `Polyline` and `Point3dList` are RhinoCommon's lists, with all of `RhinoList`'s members
  ([[`AddRange`, `RemoveRange`, `IndexOf`, `Sort`, `Reverse`, `First`, `Last`, `ToArray` || `addRange`,
  `removeRange`, `indexOf`, `sort`, `reverse`, `first`, `last`, `toArray`]], ...). They are reference
  types: an assignment shares the list. [[Index with `[]` and `len()`. || Use `get(i)`, `set(i, p)` and
  `count`.]] <!-- D37 -->
- [[`Polyline.GetSegments()` (8.x `GetSegments2()`) || `polyline.getSegments()`]] returns `Line` values,
  not `LineCurve` objects, and [[`None` || `null`]] with fewer than two points.
  [[`Point3dList.Append(points)` || `list.append(points)`]] is [[`AddRange(points)` ||
  `addRange(points)`]]. <!-- D89 -->

### Surfaces

- [[`NurbsSurface.Create(...)` || `rhino.NurbsSurface.create(...)`]] with invalid arguments returns
  [[`None` || `null`]]. <!-- D4 -->
- [[`NurbsSurfacePointList.GetPoint(u, v)` || `points.getPoint(u, v)`]] returning a `Point4d` is
  [[`GetPoint4d(u, v)` || `getPoint4d(u, v)`]].

### Breps

- A brep's surface cannot be replaced outside Rhino, so edit the brep itself (for example
  [[`brep.Transform(xform)` || `brep.transform(xform)`]]). [[`BrepFace.UnderlyingSurface()` ||
  `face.underlyingSurface()`]] returns a copy-on-write view: editing it does not change the brep.
- Editing a `BrepEdge` or `BrepTrim` through a `Curve` method ([[`edge.Reverse()` ||
  `edge.reverse()`]]) [[raises `RuntimeError` || throws]]. <!-- D23 -->
- [[`BrepFace.PerFaceColor` || `face.perFaceColor`]] of a face without one is `(0, 0, 0, 0)` (8.x
  `(255, 255, 255, 0)`), and it can be set. <!-- D28 -->
- [[`BrepFace.GetMesh(type)` || `face.getMesh(type)`]] returns a copy-on-write view: editing it does
  not change the face. Put the change back with [[`BrepFace.SetMesh(type, mesh)` ||
  `face.setMesh(type, mesh)`]].

### Extrusions

- [[`Extrusion.GetMesh(type)` || `extrusion.getMesh(type)`]] returns a copy-on-write view: editing it
  does not change the extrusion.

### Meshes

- <!-- py -->`mesh.Faces[i]` is a `MeshFace` (`A`, `B`, `C`, `D`, `IsQuad`), not a 4-tuple.
  <!-- D79 --><!-- /py --><!-- js -->`mesh.faces.get(i)` is a `MeshFace` object (`{A, B, C, D}`), not an
  array; `rhino.MeshFace.isQuad(face)` tells a quad. <!-- D79 --><!-- /js -->
- [[`MeshVertexNormalList.Add(x, y, z)` || `mesh.normals.add(...)`]] stores the vector as given;
  [[`UnitizeNormals()` || `unitizeNormals()`]] unitizes. 8.x unitized on add. <!-- D66 -->
- [[`Mesh.GetCachedTextureCoordinates(id)` || `mesh.getCachedTextureCoordinates(id)`]] is a view into
  the mesh, not a copy. <!-- D27 -->
- [[`Mesh.GetCachedTextureCoordinatesFromTexture(file, objectId, texture)` ||
  `mesh.getCachedTextureCoordinatesFromTexture(file, objectId, texture)`]] is an overload of
  [[`GetCachedTextureCoordinates` || `getCachedTextureCoordinates`]], and
  [[`SetCachedTextureCoordinatesFromMaterial` || `setCachedTextureCoordinatesFromMaterial`]] returns
  nothing. <!-- D65 -->

### SubD

<!-- D26, D88, D60, D24, D25 -->
- [[`subd.Faces`, `subd.Edges` and `subd.Vertices` are lists with `len`, iteration and `Find(id)` ||
  `subd.faces`, `subd.edges` and `subd.vertices` are lists with `count`, iteration and `find(id)`]].
  The `First` / `Next` / `Current` cursors are gone.

  ```python
  # 8.x
  face = subd.Faces.First()
  while face:
      ...
      face = subd.Faces.Next()

  # 9.2
  for face in subd.Faces:
      ...
  ```

  ```js
  // 8.x
  const faces = subd.faces()
  for (let face = faces.first(); face; face = faces.next()) { ... }

  // 9.2
  for (const face of subd.faces) { ... }
  ```

- A component's neighbours are [[`EdgeCount` plus `EdgeAt(i)` (also `VertexAt`, `FaceAt`) || `edgeCount`
  plus `edgeAt(i)` (also `vertexAt`, `faceAt`)]]. An edge's vertices are [[`VertexFrom` and `VertexTo` ||
  `vertexFrom` and `vertexTo`]].
- [[`SubDVertex.SurfacePoint()` is a method, and `Next`, `Previous` and `First` are properties.
  `SubD.SharpEdgeCount()` is a method. || `vertex.surfacePoint()` and `subd.sharpEdgeCount()` are
  functions, and `vertex.next` and `vertex.previous` are properties.]]
- A component whose SubD changed [[raises `RuntimeError` || throws]]. Components compare equal when
  they are the same component of the same SubD.
- [[`SubDFace.IsNotConvex` and `IsNotPlanar(tol)` are gone: use `not IsConvex` and `not IsPlanar(tol)` ||
  `face.isNotConvex` and `isNotPlanar(tol)` are gone: use `!face.isConvex` and `!face.isPlanar(tol)`]].
  They differ only for a degenerate face; test [[`ControlNetCenterNormal.IsZero` ||
  `face.controlNetCenterNormal`]] first when that matters. <!-- D72 -->
- [[`SubDFace.PerFaceColor` || `face.perFaceColor`]] of a face without one is `(0, 0, 0, 0)` (8.x
  `(255, 255, 255, 0)`), and it can be set. <!-- D28 -->

## Annotations

<!-- D39, D67, D68, D46 -->
- The explicit-parent accessors ([[`GetTextHeight(style)`, `SetMaskColor(style, color)` ||
  `getTextHeight(style)`, `setMaskColor(style, color)`]], ...) are gone. Set the parent, then use the
  property, as in RhinoCommon:

  ```python
  ann.ParentDimensionStyle = model.DimStyles.FindId(ann.DimensionStyleId)
  height = ann.TextHeight
  ```

  ```js
  ann.parentDimensionStyle = doc.dimStyles.findId(ann.dimensionStyleId)
  const height = ann.textHeight
  ```

  Pass the annotation's own style. Assigning a different style makes it the annotation's style and
  drops its overrides. Without a parent, these properties read the defaults, as in RhinoCommon.
  <!-- js -->The style the getter returns belongs to the annotation, so do not `delete()` it.<!-- /js -->
- [[`DimLinear.Points` (and the other dimensions' `Points` dicts) is `Get3dPoints()`, a tuple
  `(success, p1, p2, ...)` || A linear dimension's points are `get3dPoints()`, an array
  `[success, p1, p2, ...]`]]. [[`Leader.Points` is `Points3D` (and `Points2D`) || A leader's points
  are `points3D` (and `points2D`)]], and an angular dimension's angle is
  [[`NumericValue` || `numericValue`]].
- `Arrowhead` has its static [[`GetPoints(type)` || `getPoints(type)`]] only; its constructors and
  properties are not bound yet. <!-- D73 -->
- [[`TextHasRtfFormatting` || `textHasRtfFormatting`]] is true for more than one text run.
  [[`PlainText` || `plainText`]] joins the runs, and for a dimension it returns the authored text (the
  `<>` formula), not the measurement.
- <!-- js -->WebAssembly has no font engine, so text is not measured: annotation bounding boxes are
  empty, and `textModelWidth` is -2. <!-- D90 --><!-- /js -->
<!-- py -->
- On macOS and Windows, text may not be measured: an annotation's `GetBoundingBox` is then empty
  (`IsValid` is false) and `TextModelWidth` is -2. 8.x returned a zero-width box there. Linux
  measures text. <!-- D90 -->
<!-- /py -->

## Rendering and settings

- [[`RenderContent.GetParameter(name)` || `content.getParameter(name)`]] returns the value by its type
  ([[`bool`, `int`, `float`, `str`, `uuid.UUID`, a tuple of doubles, `Color4f`, `Transform`, `bytes` ||
  a boolean, number, string, array of numbers, `Color4f` or `Transform`]]), and [[`None` || `null`]]
  when there is no such parameter. 8.x always returned a string. [[`SetParameter` ||
  `setParameter`]] accepts the same values. <!-- D40 -->
- [[`RenderContent.TopLevel` || `content.topLevel`]] of a grandchild is its parent, as in RhinoCommon.
  <!-- D41 -->
- [[`SetChildSlotOn(name, on)` and `SetChildSlotAmount(name, amount)` || `setChildSlotOn(name, on)`
  and `setChildSlotAmount(name, amount)`]] take the slot name first. [[`ChildSlotAmount` ||
  `childSlotAmount`]] of an unset slot is `0.0`. <!-- D64 -->
- [[`RenderSettings.PostEffects` || `renderSettings.postEffects`]] is a `PostEffectCollection` of
  `PostEffectData`: iterate it, or use [[`PostEffectDataFromId(id)` || `postEffectDataFromId(id)`]].
  <!-- D50 -->
- [[`PhysicallyBasedMaterial.BaseColor` (and `Emission`, `SubsurfaceScatteringColor`) ||
  `pbr.baseColor` (and `emission`, `subsurfaceScatteringColor`)]] is a `Color4f` (`R`, `G`, `B`, `A`),
  not [[a tuple. Python accepts a 4-sequence where a `Color4f` is expected || an array]]. <!-- D34 -->
- **Sun**: the date is a [[`datetime.datetime` || `Date`]]: [[`sun.GetDateTime(DateTimeKind.Local)`
  and `sun.SetDateTime(d, DateTimeKind.Local)` || `sun.getDateTime(rhino.DateTimeKind.Local)` and
  `sun.setDateTime(d, rhino.DateTimeKind.Local)`]], in place of the year, month, day and hours
  properties. Times are whole seconds. [[`SunColorFromAltitude` is `ColorFromAltitude` ||
  `sunColorFromAltitude` is `rhino.Sun.colorFromAltitude`]], which rounds each channel (8.x
  truncated, so a channel can differ by 1). <!-- D45, D47, D48 -->
- [[`TextureMapping.RequiresVertexNormals` is gone. Use `MappingType`, `Projection` and `Capped` ||
  `mapping.requiresVertexNormals` is gone. Use `mappingType`, `projection` and `capped`]].
  <!-- D49 -->
- [[`TextureMapping.TryGetMappingBox`, `TryGetMappingCylinder` and `TryGetMappingPlane` ||
  `tryGetMappingBox`, `tryGetMappingCylinder` and `tryGetMappingPlane`]] with the capped `out` values
  are [[`TryGetMappingBoxCapped`, `TryGetMappingCylinderCapped` and `TryGetMappingPlaneCapped` ||
  `tryGetMappingBoxCapped`, `tryGetMappingCylinderCapped` and `tryGetMappingPlaneCapped`]].
- [[`ObjectAttributes.DrawColor(file)` || `attributes.drawColor(file)`]] for a colour by material uses
  Rhino's rules: the material the attributes select, and its PBR base colour when it is physically
  based. <!-- D63 -->

## Enums

<!-- D76, D77, D18 -->
- Every enum is RhinoCommon's, with RhinoCommon's names and values. Twelve 8.x enums had no
  RhinoCommon namesake and are replaced:

  <!-- py -->
  | 8.x | 9.2 |
  |---|---|
  | `AnnotationTypes` | `AnnotationType` |
  | `ArrowheadTypes` | `DimensionStyle.ArrowType` |
  | `CurvePipingCapTypes` | `File3dmCurvePiping.CapTypes` |
  | `DecalMappings`, `Mappings` | `DecalMapping` |
  | `DecalProjections`, `Projections` | `DecalProjection` |
  | `DisplacementSweepResolutionFormulas` | `File3dmDisplacement.SweepResolutionFormulas` |
  | `DitheringMethods` | `Dithering.Methods` |
  | `EnvironmentBackgroundProjections` | `Environment.BackgroundProjections` |
  | `PostEffectTypes` | `PostEffectType` |
  | `RenderChannelsModes` | `RenderChannels.Modes` |

  An enum is nested in its class, as in RhinoCommon.
  <!-- /py --><!-- js -->
  | 8.x | 9.2 |
  |---|---|
  | `AnnotationTypes` | `AnnotationType` |
  | `ArrowheadTypes` | `DimensionStyleArrowType` |
  | `CurvePipingCapTypes` | `File3dmCurvePipingCapTypes` |
  | `DecalMappings`, `Mappings` | `DecalMapping` |
  | `DecalProjections`, `Projections` | `DecalProjection` |
  | `DisplacementSweepResolutionFormulas` | `File3dmDisplacementSweepResolutionFormulas` |
  | `DitheringMethods` | `DitheringMethods` |
  | `EnvironmentBackgroundProjections` | `EnvironmentBackgroundProjections` |
  | `PostEffectTypes` | `PostEffectType` |
  | `RenderChannelsModes` | `RenderChannelsModes` |

  An enum nested in a class in RhinoCommon joins the two names (`DimensionStyle.ArrowType` is
  `DimensionStyleArrowType`).
  <!-- /js -->
- Some values are numbered differently: [[`Dithering.Methods` || `DitheringMethods`]] is
  `None = 0, FloydSteinberg = 1, SimpleNoise = 2` (8.x: `SimpleNoise = 0`), and `PostEffectType` is
  `Early = 0, ToneMapping = 1, Late = 2`. <!-- D36, D50 -->
- `LightStyle.Unknown` is `LightStyle.None`.
- <!-- py -->`None` is a keyword, so a value named `None` is also available as `None_`
  (`rhino3dm.LightStyle.None_`). `E['None']` and `getattr(E, 'None')` work too, and the value's name
  is still `'None'`.<!-- /py -->
- [[Enums are integer-like (`int(e)` and `e == 2` work), but passing a plain int where an enum is
  expected raises `TypeError` || A number or string where an enum is expected throws `TypeError`; 8.x
  silently used 0]]. <!-- D55 -->

## Errors, values and identity

### Errors

<!-- py -->
<!-- D9, D53, D10 -->
| .NET | Python |
|---|---|
| `IndexOutOfRangeException`, `ArgumentOutOfRangeException` | `IndexError` |
| `ArgumentNullException`, `ArgumentException` | `ValueError` |
| `InvalidOperationException`, `NotSupportedException` | `RuntimeError` |
| `FileNotFoundException` | `FileNotFoundError` |
<!-- /py --><!-- js -->
<!-- D9, D10 -->
An error is a `WebAssembly.Exception`. Read its text with `rhino.getExceptionMessage(e)`, and free it
with `rhino.decrementExceptionRefcount(e)`, or it leaks:

```js
try {
  curve.spanDomain(-1)
} catch (e) {
  console.log(rhino.getExceptionMessage(e))
  rhino.decrementExceptionRefcount(e)
}
```
<!-- /js -->

Where RhinoCommon accepts `null`, [[`None` || `null`]] is accepted and behaves as in .NET:
[[`PolyCurve.Append(None)` || `polyCurve.append(null)`]] returns [[`False` || `false`]], and
[[`model.Objects.AddPoint(p, None)` || `doc.objects.addPoint(p, null)`]] uses the default attributes.

### Arrays and sequences

<!-- D87, D35, D62 -->
Wherever RhinoCommon takes a sequence of points, [[any sequence works (a list, a tuple, a
`Point3dList`) || an array works]]. [[`GetUserStrings()` || `getUserStrings()`]] returns
[[a list of `[key, value]` lists || an array of `[key, value]` arrays]].

### Strings and values

<!-- py -->
- `str(point)` is RhinoCommon's `ToString()`: full precision (`0.30000000000000004`, where 8.x printed 6
  significant digits). `str` works on every value type and on classes (`str(layer)` is its full path).
  <!-- D75 -->
- `repr(Point3d(1, 2, 3))` is `Point3d(1.0, 2.0, 3.0)`. <!-- D81 -->
<!-- /py -->
- [[`ViewportInfo.ScreenPort` and `GetScreenPort()` are || `viewport.screenPort` and
  `getScreenPort()` are]] RhinoCommon's `(x, y, width, height)`, with y the smaller of top and bottom.
  A new [[`ViewportInfo()` || `new rhino.ViewportInfo()`]] reads `(0, 0, 1000, 1000)`. <!-- D58, D38 -->
- [[`File3dm.FromByteArray` takes any buffer (`bytes`, `bytearray`, numpy) ||
  `rhino.File3dm.fromByteArray` takes a `Uint8Array`]]. <!-- D38 -->

### Object identity

<!-- D22, D25, D42, D56 -->
Contents and behaviour follow RhinoCommon, but object identity may not. Accessors such as
[[`obj.Geometry`, `brep.Faces` and `brep.Faces[0]` || `obj.geometry`, `brep.faces` and
`brep.faces.get(0)`]] return a new view each time, so [[`brep.Faces[0] is brep.Faces[0]` is `False` ||
`brep.faces.get(0) === brep.faces.get(0)` is `false`]]. Compare components by their index or id.
[[`Brep.TryConvertBrep(geometry)` || `rhino.Brep.tryConvertBrep(geometry)`]] on a file's brep returns
a new view of it, not `geometry` itself.

## Packaging

<!-- py -->
- The module is built with nanobind. On Python 3.12 and newer it is one stable-ABI (`abi3`) module,
  and it supports free-threaded CPython 3.14t.
- The package ships typings (`__init__.pyi`) with RhinoCommon's documentation, so editors complete
  and type-check rhino3dm code.
<!-- /py --><!-- js -->
- The module is WebAssembly with 64-bit memory (memory64), which openNURBS 9 needs. It runs in
  Chrome 133+, Firefox 134+ and Node 24+. Safari supports memory64 only in its Technology Preview.
- The package ships typings (`rhino3dm.d.ts`) with RhinoCommon's documentation, so editors complete
  and type-check rhino3dm code.
<!-- /js -->
