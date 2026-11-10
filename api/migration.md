<!--
The source of the Python and JavaScript migration guides, docs/migrating-to-9/python.md and
javascript.md. tools/docs/migration.py renders one page per language (`--check`: stale pages):

  - plain text goes to both pages;
  - a ```python fence goes to the Python page only, a ```js fence to the JavaScript page only;
  - text between the comment markers "py" and "/py" (or "js" and "/js") goes to that language only:
    a word, a bullet or a whole section;
  - [[python text || js text]] picks one side per page;
  - a heading whose section ends up empty is dropped.

Each change is written once, so the pages cannot disagree.
-->

# Migrating to rhino3dm 9.2 ([[Python || JavaScript]])

[← Other languages](../migrating-to-9.md)

rhino3dm 9.2's [[Python || JavaScript]] bindings are generated from RhinoCommon, the .NET SDK of Rhino,
so the API is RhinoCommon's: its names, signatures and behaviour. The typings that ship with the package ([[`rhino3dm/__init__.pyi` || `rhino3dm.d.ts`]]) list every
member with its documentation.

rhino3dm 8.x was written by hand and often differed from RhinoCommon. The changes below are the ones most scripts will hit. [[[Renamed and removed members](python-members.md) || [Renamed and removed members](javascript-members.md)]]
maps each 8.x member that is gone to its replacement.

[The changes most scripts hit](#the-changes-most-scripts-hit) ·
<!-- js -->[Values and objects](#values-and-objects) · <!-- /js -->
[Files and tables](#files-and-tables) · [Geometry](#geometry) · [Annotations](#annotations) ·
[Rendering and settings](#rendering-and-settings) · [Renamed classes](#renamed-classes) · [Enums](#enums) ·
[Errors, values and identity](#errors-values-and-identity) · [Packaging](#packaging)

## The changes most scripts hit

<!-- js -->
### 1. Value types are plain objects

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
a value. [Renamed and removed members](javascript-members.md) lists every 8.x member of these types
with its 9.2 form: `arc.circumference` is `rhino.Arc.circumference(arc)`, `arc.radius` is `arc.Radius`.

### 2. Properties are properties

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

[[`obj.Geometry` || `obj.geometry`]] is now a copy-on-write view, like it is in RhinoCommon. This means that the first edit makes a
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

[[`obj.Attributes` || `obj.attributes`]] still writes through. A brep face's surface and mesh, an
extrusion's mesh and a material's textures are copy-on-write views too ([Geometry](#all-geometry)).

### [[2 || 4]]. `out` parameters come back as [[a tuple || an array]], success first

```python
# 8.x
frustum = viewport.GetFrustum()   # a dict
left, near = frustum['left'], frustum['near']

# 9.2
ok, left, right, bottom, top, near, far = viewport.GetFrustum()
ok, plane = curve.TryGetPlane()   # new in 9.2, same pattern
```

```js
// 8.x
const frustum = viewport.getFrustum()   // an object, or null
const left = frustum.left, near = frustum.near

// 9.2
const [ok, left, right, bottom, top, near, far] = viewport.getFrustum()
const [ok2, plane] = curve.tryGetPlane()   // new in 9.2, same pattern
```

### [[3 || 5]]. Editing tables

```python
# 8.x
model.Write(path)
index = model.Layers.Add(layer)
model.Layers.Delete(layer.Id)

# 9.2
model.Write(path, 0)              # 0 is the current version
model.Layers.Add(layer)           # returns nothing
index = len(model.Layers) - 1
model.Layers.Delete(layer)        # or Delete(index), which raises for a bad index
```

```js
// 8.x
const index = doc.layers().add(layer)
doc.layers().delete(layer.id)

// 9.2
doc.layers.add(layer)             // returns nothing
const index = doc.layers.count - 1
doc.layers.delete(layer)          // or delete(index), which throws for a bad index
```

[[`model.Layers.AddLayer(name, color)` || `doc.layers.addLayer(name, color)`]] still returns the index.
The same goes for the other tables (materials, linetypes, dimension styles, ...).

<!-- py -->
### 4. In-place methods return nothing

```python
# 8.x
p = p.Transform(xform)  # edited p and returned it
v.Unitize()             # returned None

# 9.2
p.Transform(xform)      # edits p, returns None
ok = v.Unitize()        # False for a zero vector
```
<!-- /py -->

### [[5 || 6]]. Bad arguments [[raise || throw]]

RhinoCommon's argument checks run here too: [[`curve.SpanDomain(-1)` raises `IndexError`, and
`File3dm.Read('missing.3dm')` raises `FileNotFoundError` || `curve.spanDomain(-1)` throws]].
[[See [Errors](#errors) for how .NET's exceptions map to Python's. || In 8.x an error inside the
module aborted it; now it is an exception you can catch ([Errors](#errors)).]]

### [[6 || 7]]. [[Arrays are lists || Null arrays are `null`]]

[[Array results are lists (8.x returned tuples in places), and `None` where RhinoCommon returns `null` ||
Where RhinoCommon returns a `null` array, JS gets `null`, not an empty array]], for example
[[`BoundingBox.GetCorners()` || `rhino.BoundingBox.getCorners(box)`]] of an invalid box.

<!-- js -->
## Values and objects

- A value type is built with `new`, as in 8.x and RhinoCommon, and gives a plain object
  (`rhino.T.create(...)` is the same call; `instanceof rhino.T` is false). Its operators are functions with RhinoCommon's names:

  ```js
  // 8.x
  const box = new rhino.BoundingBox([0, 0, 0], [1, 1, 1])   // box.min is [0, 0, 0]
  const v = [a[0] + b[0], a[1] + b[1], a[2] + b[2]]

  // 9.2
  const box = new rhino.BoundingBox(min, max)   // { Min: {...}, Max: {...} }: box.Min
  const t = rhino.Transform.multiply(a, b)
  const v = rhino.Vector3d.add(a, b)
  ```

- A method that changes the value in C# changes the object you pass, and returns what C# returns:

  ```js
  // 8.x
  point = rhino.Point3d.transform(point, xform)   // returned a moved copy

  // 9.2
  rhino.Point3d.transform(point, xform)        // point is moved
  const ok = rhino.Vector3d.unitize(vector)    // vector is unitized; ok is false for a zero vector
  ```

- Static properties are getters: `rhino.Plane.worldXY`, not `rhino.Plane.worldXY()`.
- Every member is camelCase. A name that a JS function already owns keeps RhinoCommon's spelling:
  `rhino.Vector3d.Length(v)`.
- Overloads share one name, chosen by the arguments: `addTriFace` / `addQuadFace` are `addFace`,
  `appendLine` / `appendArc` / `appendCurve` are `append`, and `toByteArrayOptions(options)` is
  `toByteArray(options)`.
- Overloads that differ only in float versus double values (`Point3f` versus `Point3d`) keep the
  double one.
- Views and table items (`doc.objects`, `brep.faces.get(0)`, a layer from a table) keep their parent
  alive, so dropping the parent is safe. `parent.delete()` still frees it at once and invalidates its
  views.
- `SubDEdgeSharpness` is a plain object `{sharpness0, sharpness1}`; build one with
  `new rhino.SubDEdgeSharpness(a, b)`, which checks the values as RhinoCommon does.
- A color is `{r, g, b, a}` (`a` optional). Anything else, such as an array, throws `TypeError`; 8.x read
  it as black.
<!-- /js -->

## Files and tables

### Getting items: iterating and indexing

```python
# 8.x
for i in range(len(model.Objects)):
    obj = model.Objects[i]

# 9.2
for obj in model.Objects:
    ...
third = list(model.Objects)[2]
```

```js
// 8.x
for (let i = 0; i < doc.objects().count; i++) {
  const obj = doc.objects().get(i)
}

// 9.2
for (const obj of doc.objects) {
  ...
}
const third = [...doc.objects][2]
```

- **Iteration** walks the live components ([[`for layer in model.Layers` || `for (const layer of
  doc.layers)`]]), skipping deleted ones. 8.x [[raised || silently stopped]] at the first deleted item.
- <!-- py -->**`model.Objects[i]`** is gone, as in RhinoCommon: iterate, or `list(model.Objects)[i]`.
  `len(model.Objects)` works.<!-- /py --><!-- js -->**`doc.objects.get(i)`** is gone, as in
  RhinoCommon: iterate, or `[...doc.objects][i]`. `doc.objects.count` works.<!-- /js -->
- <!-- py -->**`FindIndex(i)`** returns `None` for a deleted or out-of-range index. `table[i]` still
  raises `IndexError`.<!-- /py -->
- **Bitmaps**: [[`FindId` || `findId`]] finds images, and [[`FindIndex(i)` / `[i]` || `findIndex(i)`]]
  give the i-th image.
- **Decals**: iterate them ([[`for d in attributes.Decals` || `for (const d of
  attributes.decals)`]]); hidden decals are skipped, as in RhinoCommon. [[`len(attributes.Decals)` ||
  `attributes.decals.count`]] counts what iteration yields. There is no index: use
  [[`list(attributes.Decals)[i]` || `[...attributes.decals][i]`]].

### Table items are live

- A layer you hold reflects later edits to the file, and [[raises `RuntimeError` || throws]] once its
  layer is deleted from the file. In 8.x it kept stale data.
- [[`model.Views[i]` and `NamedViews[i]` are || `doc.views.get(i)` and `doc.namedViews.get(i)` are]]
  live too: the same view while it is in the file, and [[a `RuntimeError` || an exception]] once it is
  not.
- Objects come back as their most specific class: a plane surface is a `PlaneSurface`, not a
  `Surface`, so [[`isinstance` || `instanceof`]] checks change.

### Tables and their members

- [[`model.Layers` and `model.AllLayers` || `doc.layers` and `doc.allLayers`]] both exist, as in
  RhinoCommon (also materials, linetypes, dimension styles, instance definitions and hatch patterns).
- [[`model.Strings` || `doc.strings`]] has RhinoCommon's members: [[`Count`, `GetKey(i)`,
  `GetValue(i)`, `GetValue(key)`, `SetString(key, value)`, `Delete(key)`. There is no `len` and no
  indexing. || `count`, `getKey(i)`, `getValue(i)`, `getValue(key)`, `setString(key, value)`,
  `delete(key)`.]]
- [[`File3dm.Manifest` || `doc.manifest`]] lists every component of the file. Its images are `Bitmap`
  objects.
- [[`File3dm.AllViews` and `AllNamedViews` are || `doc.allViews` and `doc.allNamedViews` are]] not
  available yet ([[`Views` and `NamedViews` || `views` and `namedViews`]] are).
- [[`model.Objects.Add(geometry, attributes)` || `doc.objects.add(geometry, attributes)`]] adds
  leaders, text and radial or ordinate dimensions too, as 8.x did, although RhinoCommon throws for
  them.
- **Adding a bitmap**: set [[`bitmap.FileFullPath = path` || `bitmap.fileFullPath = path`]], then
  [[`model.Bitmaps.Add(bitmap)` || `doc.bitmaps.add(bitmap)`]] returns the file's copy.

### Embedded files

```python
# 8.x
file = rhino3dm.EmbeddedFile.Read(path)
model.EmbeddedFiles.Add(file)
paths = model.EmbeddedFilePaths()

# 9.2
file = rhino3dm.File3dmEmbeddedFile.Read(path)   # named after path
model.EmbeddedFiles.Add(file)
paths = [f.Filename for f in model.EmbeddedFiles]
```

```js
// 8.x
const file = rhino.EmbeddedFile.fromByteArray(bytes)
file.fileName = 'logo.png'
doc.embeddedFiles().add(file)
const paths = doc.embeddedFilePaths()

// 9.2
const file = rhino.File3dmEmbeddedFile.fromByteArray(bytes, 'logo.png')
doc.embeddedFiles.add(file)
const paths = [...doc.embeddedFiles].map(f => f.filename)
```

- An embedded file's [[`Filename` || `filename`]] is read-only: the name comes from [[the path given
  to `File3dmEmbeddedFile.Read(path)` || `File3dmEmbeddedFile.fromByteArray(bytes, filename)`]].
- Paths are cleaned for the platform, so a path saved on Windows reads with `/` on Linux and macOS.

### File settings

- [[`File3dm.Created` and `LastEdited` are new: `datetime.datetime` values || `doc.created` and
  `doc.lastEdited` are new: `Date` values]].
- [[`ConstructionPlane` and `File3dmWriteOptions` || `ConstructionPlane` and `File3dmWriteOptions`
  are classes (`new rhino.File3dmWriteOptions()`), not plain objects. They]] gain RhinoCommon's members:
  [[`ShowGrid`, `ShowAxes`, `ShowZAxis` || `showGrid`, `showAxes`, `showZAxis`]], and the write
  options' mesh options.

## Geometry

### All geometry

- **Copy-on-write.** Like [[`obj.Geometry` || `obj.geometry`]], [[`BrepFace.UnderlyingSurface()`,
  `BrepFace.GetMesh(type)`, `Extrusion.GetMesh(type)` and `Material.GetBitmapTexture()` ||
  `face.underlyingSurface()`, `face.getMesh(type)`, `extrusion.getMesh(type)` and
  `material.getBitmapTexture()`]] (and the other texture getters) return copy-on-write views: an edit
  does not reach the file. Put changes back with [[`BrepFace.SetMesh(type, mesh)` or
  `Material.SetBitmapTexture(texture)` || `face.setMesh(type, mesh)` or
  `material.setBitmapTexture(texture)`]].
- **Values of invalid inputs follow RhinoCommon.** For example the distance to an unset point is `0.0`
  (8.x: infinity).
- <!-- py -->`IsValidWithLog` is a method: `ok, log = obj.IsValidWithLog()`.<!-- /py --><!-- js -->`isValidWithLog()` is a function returning `[ok, log]`.<!-- /js -->
- Constructors RhinoCommon does not have are gone: `Group()`, `Hatch()`, `RevSurface()`,
  `TextureMapping()`, `Decal()`, `Point()`. Use RhinoCommon's factories ([[`model.AllGroups.AddGroup` ||
  `doc.allGroups.addGroup`]], [[`Hatch.CreateFromBrep` || `rhino.Hatch.createFromBrep`]],
  [[`RevSurface.Create` || `rhino.RevSurface.create`]], [[`TextureMapping.Create*Mapping` ||
  `rhino.TextureMapping.create*Mapping`]], [[`Point(location)` || `new rhino.Point(location)`]]).
- Some RhinoCommon overloads differ only in their `out` parameters, so [[Python || JS]] can reach only
  one of them under RhinoCommon's name. The others have names of their own, listed in each section
  below.

### Points, vectors and transforms

<!-- py -->
- `Transform` is a value type, so assignment copies it.
- `Point3d.Transform` and `Vector3d.Unitize` change the value in place: see
  [the changes most scripts hit](#4-in-place-methods-return-nothing).
- `Plane.WorldXY`, `Transform.Identity`, `Vector3d.Length` and the other members that are properties
  in RhinoCommon are properties: `Plane.WorldXY`, not `Plane.WorldXY()`.
<!-- /py -->
- [[`Transform.DecomposeAffine` || `rhino.Transform.decomposeAffine`]] with all four `out` values is
  [[`Transform.DecomposeAffineFull` || `rhino.Transform.decomposeAffineFull`]].

### Curves

- [[`Curve.CreateControlPointCurve(points, degree)` || `rhino.Curve.createControlPointCurve(points,
  degree)`]] builds the curve as RhinoCommon does. Two points still give a `LineCurve`, and degree 1
  still gives a `PolylineCurve`. For a NURBS curve:
  - **The domain is `0` to the length of the control polygon.** 8.x used knot spacing 1, so the domain
    was `0` to the number of points minus the degree. The shape is the same, but every parameter
    changes: [[`PointAt(t)`, `Domain` || `pointAt(t)`, `domain`]] and closest-point results.
  - **A closed point list (last point equal to the first) gives a smooth periodic curve.** 8.x made a
    clamped curve with a kink where it closes.
  - **Too few points for the degree lowers the degree** to the number of points minus one. 8.x
    returned [[`None` || `null`]]. The degree is also clamped to 1–11.
- [[`Curve.DerivativeAt` || `curve.derivativeAt`]] returns `Vector3d`s (the point first, as a vector).
- [[`NurbsCurvePointList.GetPoint(i)` || `points.getPoint(i)`]] returning a `Point4d` is
  [[`GetPoint4d(i)` || `getPoint4d(i)`]].

### Polylines and point lists

- `Polyline` and `Point3dList` are RhinoCommon's lists, with all of `RhinoList`'s members
  ([[`AddRange`, `RemoveRange`, `IndexOf`, `Sort`, `Reverse`, `First`, `Last`, `ToArray` || `addRange`,
  `removeRange`, `indexOf`, `sort`, `reverse`, `first`, `last`, `toArray`]], ...). They are reference
  types: an assignment shares the list. [[Index with `[]` and `len()`. || Use `get(i)`, `set(i, p)` and
  `count`.]]
- [[`Polyline.GetSegments()` (8.x `GetSegments2()`) || `polyline.getSegments()`]] returns `Line` values,
  not `LineCurve` objects, and [[`None` || `null`]] with fewer than two points.
  [[`Point3dList.Append(points)` || `list.append(points)`]] is [[`AddRange(points)` ||
  `addRange(points)`]].

### Surfaces

- [[`NurbsSurface.Create(...)` || `rhino.NurbsSurface.create(...)`]] with invalid arguments returns
  [[`None` || `null`]].
- [[`NurbsSurfacePointList.GetPoint(u, v)` || `points.getPoint(u, v)`]] returning a `Point4d` is
  [[`GetPoint4d(u, v)` || `getPoint4d(u, v)`]].

### Breps

- Editing a face's surface no longer changes the brep. [[`BrepFace.UnderlyingSurface()` and
  `brep.Surfaces[i]` || `face.underlyingSurface()` and `brep.surfaces.get(i)`]] return copy-on-write
  views. In 8.x an edit moved the surface inside the brep but left its edges and trims
  where they were, which made the brep invalid ([[`brep.IsValid` || `brep.isValid`]] became false) and
  could write a broken brep to the file. To move the brep, transform
  it ([[`brep.Transform(xform)` || `brep.transform(xform)`]]). To use the edited surface, build a new
  brep from it:

  ```python
  # 8.x
  surface = face.UnderlyingSurface()
  surface.Transform(xform)      # moved the face's surface inside the brep, not its edges and trims
  brep.IsValid                  # False: the brep is now invalid

  # 9.2
  surface = face.UnderlyingSurface()
  surface.Transform(xform)      # moves a copy: the brep is unchanged
  new_brep = rhino3dm.Brep.CreateFromSurface(surface)
  ```

  ```js
  // 8.x
  const surface = face.underlyingSurface()
  surface.transform(xform)      // moved the face's surface inside the brep, not its edges and trims
  brep.isValid                  // false: the brep is now invalid

  // 9.2
  const surface = face.underlyingSurface()
  surface.transform(xform)      // moves a copy: the brep is unchanged
  const newBrep = rhino.Brep.createFromSurface(surface)
  ```

- Editing a `BrepEdge` or `BrepTrim` through a `Curve` method ([[`edge.Reverse()` ||
  `edge.reverse()`]]) [[raises `RuntimeError` || throws]].
- [[`BrepFace.PerFaceColor` || `face.perFaceColor`]] of a face without one is `(0, 0, 0, 0)` (8.x
  `(255, 255, 255, 0)`), and it can be set.
- [[`BrepFace.GetMesh(type)` || `face.getMesh(type)`]] returns a copy-on-write view: editing it does
  not change the face. Put the change back with [[`BrepFace.SetMesh(type, mesh)` ||
  `face.setMesh(type, mesh)`]].

### Extrusions

- Editing the mesh [[`Extrusion.GetMesh(type)` || `extrusion.getMesh(type)`]] returns no longer
  changes the extrusion or the meshes it stores: it is a copy-on-write view. In 8.x the edit changed
  the extrusion's stored mesh. Put the change back with [[`Extrusion.SetMesh(mesh, type)` ||
  `extrusion.setMesh(mesh, type)`]].

### Meshes

- <!-- py -->`mesh.Faces[i]` is a `MeshFace` (`A`, `B`, `C`, `D`, `IsQuad`), not a 4-tuple:

  ```python
  # 8.x
  a, b, c, d = mesh.Faces[i]
  is_quad = c != d

  # 9.2
  face = mesh.Faces[i]
  a, b, c, d = face.A, face.B, face.C, face.D
  is_quad = face.IsQuad
  ```
  <!-- /py --><!-- js -->`mesh.faces.get(i)` is a `MeshFace` object (`{A, B, C, D}`), not an
  array:

  ```js
  // 8.x
  const [a, b, c, d] = mesh.faces().get(i)
  const isQuad = c !== d

  // 9.2
  const face = mesh.faces.get(i)
  const { A, B, C, D } = face
  const isQuad = rhino.MeshFace.isQuad(face)
  ```
  <!-- /js -->
- [[`mesh.Normals.Add(x, y, z)` || `mesh.normals.add(x, y, z)`]] stores the vector as given. 8.x
  unitized it on add. Unitize afterwards, once the mesh has a normal for each vertex:

  ```python
  # 8.x
  mesh.Normals.Add(0.0, 0.0, 5.0)   # stored as (0, 0, 1)

  # 9.2
  mesh.Normals.Add(0.0, 0.0, 5.0)   # stored as (0, 0, 5)
  mesh.Normals.UnitizeNormals()     # now (0, 0, 1)
  ```

  ```js
  // 8.x
  mesh.normals().add(0, 0, 5)       // stored as (0, 0, 1)

  // 9.2
  mesh.normals.add(0, 0, 5)         // stored as (0, 0, 5)
  mesh.normals.unitizeNormals()     // now (0, 0, 1)
  ```

- [[`Mesh.GetCachedTextureCoordinates(id)` || `mesh.getCachedTextureCoordinates(id)`]] is a view into
  the mesh, not a copy.
- [[`Mesh.GetCachedTextureCoordinatesFromTexture(file, objectId, texture)` ||
  `mesh.getCachedTextureCoordinatesFromTexture(file, objectId, texture)`]] is an overload of
  [[`GetCachedTextureCoordinates` || `getCachedTextureCoordinates`]], and
  [[`SetCachedTextureCoordinatesFromMaterial` || `setCachedTextureCoordinatesFromMaterial`]] returns
  nothing.

### SubD

- [[`subd.Faces`, `subd.Edges` and `subd.Vertices` are lists with `len`, iteration and `Find(id)` ||
  `subd.faces`, `subd.edges` and `subd.vertices` are lists with `count`, iteration and `find(id)`]].
  The `First` / `Next` / `Current` cursors are gone. In 8.x, `Next()` did not stop after the last
  component: [[a `while` loop over it never ended, or crashed || it returned empty components (`id`
  0) forever]]. <!-- py -->`for face in subd.Faces` works in both.<!-- /py -->

  ```python
  # 8.x
  count = subd.Faces.Count
  face = subd.Faces[face_id]          # by id

  # 9.2
  count = len(subd.Faces)
  face = subd.Faces.Find(face_id)
  ```

  ```js
  // 8.x
  const faces = subd.faces()          // a cursor
  const count = faces.count
  const face = faces.get(faceId)      // by id

  // 9.2
  for (const face of subd.faces) { ... }
  const count = subd.faces.count
  const face = subd.faces.find(faceId)
  ```

- A component's neighbours are [[lists: `face.Edges`, `face.Vertices`, `edge.Faces`, `edge.Vertices`,
  `vertex.Edges` and `vertex.Faces` || arrays: `face.edges`, `face.vertices`, `edge.faces`,
  `edge.vertices`, `vertex.edges` and `vertex.faces`]], not 8.x's cursors. RhinoCommon's
  [[`EdgeCount` plus `EdgeAt(i)` (also `VertexAt`, `FaceAt`) || `edgeCount` plus `edgeAt(i)` (also
  `vertexAt`, `faceAt`)]] work too, and an edge's vertices are also [[`VertexFrom` and `VertexTo` ||
  `vertexFrom` and `vertexTo`]].

  ```python
  # 8.x
  edges = face.Edges                  # a cursor
  count = edges.Count
  edge = edges[0]

  # 9.2
  edges = face.Edges                  # a list
  count = len(edges)                  # or face.EdgeCount
  edge = edges[0]                     # or face.EdgeAt(0)
  start, end = edge.VertexFrom, edge.VertexTo
  ```

  ```js
  // 8.x
  const edges = face.edges()          // a cursor
  const count = edges.count
  const edge = edges.get(0)

  // 9.2
  const edges = face.edges            // an array
  const count = edges.length          // or face.edgeCount
  const edge = edges[0]               // or face.edgeAt(0)
  const start = edge.vertexFrom, end = edge.vertexTo
  ```

- [[`SubDVertex.SurfacePoint()` is a method, and `Next`, `Previous` and `First` are properties.
  `SubD.SharpEdgeCount()` is a method. || `vertex.surfacePoint()` and `subd.sharpEdgeCount()` are
  functions, and `vertex.next` and `vertex.previous` are properties.]]

  ```python
  # 8.x
  point = vertex.SurfacePoint
  following = vertex.Next()
  first = subd.Vertices.First()
  sharp = subd.SharpEdgeCount

  # 9.2
  point = vertex.SurfacePoint()
  following = vertex.Next
  first = subd.Vertices.First
  sharp = subd.SharpEdgeCount()
  ```

  ```js
  // 8.x
  const point = vertex.surfacePoint
  const following = vertex.next()
  const sharp = subd.sharpEdgeCount

  // 9.2
  const point = vertex.surfacePoint()
  const following = vertex.next
  const sharp = subd.sharpEdgeCount()
  ```

- A component whose SubD changed [[raises `RuntimeError` || throws]]; 8.x kept answering as if
  nothing had changed. Components compare equal when they are the same component of the same SubD
  [[(`==`) || (`face.equals(other)`)]].

  ```python
  # 8.x
  face = subd.Faces[1]
  subd.Subdivide(1)
  face.EdgeCount         # still answered

  # 9.2
  face = subd.Faces.Find(1)
  subd.Subdivide(1)
  face.EdgeCount         # RuntimeError: the component is no longer in its owner
  ```

  ```js
  // 8.x
  const face = subd.faces().get(1)
  subd.subdivide(1)
  face.edgeCount         // still answered

  // 9.2
  const face = subd.faces.find(1)
  subd.subdivide(1)
  face.edgeCount         // throws: the component is no longer in its owner
  ```

- [[`SubDFace.IsNotConvex` and `IsNotPlanar(tol)` are gone: use `not IsConvex` and `not IsPlanar(tol)` ||
  `face.isNotConvex` and `isNotPlanar(tol)` are gone: use `!face.isConvex` and `!face.isPlanar(tol)`]].
  They differ only for a degenerate face; test [[`ControlNetCenterNormal.IsZero` ||
  `face.controlNetCenterNormal`]] first when that matters.

  ```python
  # 8.x
  if face.IsNotConvex or face.IsNotPlanar(tol):
      ...

  # 9.2
  if not face.IsConvex or not face.IsPlanar(tol):
      ...
  ```

  ```js
  // 8.x
  if (face.isNotConvex || face.isNotPlanar(tol)) { ... }

  // 9.2
  if (!face.isConvex || !face.isPlanar(tol)) { ... }
  ```

- [[`SubDFace.PerFaceColor` || `face.perFaceColor`]] of a face without one is `(0, 0, 0, 0)` (8.x
  `(255, 255, 255, 0)`), and it can be set.

  ```python
  # 8.x
  face.PerFaceColor                       # (255, 255, 255, 0) when unset; read-only

  # 9.2
  face.PerFaceColor                       # (0, 0, 0, 0) when unset
  face.PerFaceColor = (255, 0, 0, 255)
  ```

  ```js
  // 8.x
  face.perFaceColor                       // {r: 255, g: 255, b: 255, a: 0} when unset; read-only

  // 9.2
  face.perFaceColor                       // {r: 0, g: 0, b: 0, a: 0} when unset
  face.perFaceColor = { r: 255, g: 0, b: 0, a: 255 }
  ```

## Annotations

- The explicit-parent accessors ([[`GetTextHeight(style)`, `SetMaskColor(style, color)` ||
  `getTextHeight(style)`, `setMaskColor(style, color)`]], ...) are gone. Set the parent, then use the
  property, as in RhinoCommon:

  ```python
  # 8.x
  style = model.DimStyles.FindId(ann.DimensionStyleId)
  height = ann.GetTextHeight(style)

  # 9.2
  ann.ParentDimensionStyle = model.DimStyles.FindId(ann.DimensionStyleId)
  height = ann.TextHeight
  ```

  ```js
  // 8.x
  const style = doc.dimstyles().findId(ann.dimensionStyleId)
  const height = ann.getTextHeight(style)

  // 9.2
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
  properties are not bound yet.
- [[`TextHasRtfFormatting` || `textHasRtfFormatting`]] is true for more than one text run.
  [[`PlainText` || `plainText`]] joins the runs, and for a dimension it returns the authored text (the
  `<>` formula), not the measurement.
- <!-- js -->WebAssembly has no font engine, so text is not measured: annotation bounding boxes are
  empty, and `textModelWidth` is -2.<!-- /js -->
<!-- py -->
- On macOS and Windows, text may not be measured: an annotation's `GetBoundingBox` is then empty
  (`IsValid` is false) and `TextModelWidth` is -2. 8.x returned a zero-width box there. Linux
  measures text.
<!-- /py -->

## Rendering and settings

- [[`RenderContent.GetParameter(name)` || `content.getParameter(name)`]] returns the value by its type
  ([[`bool`, `int`, `float`, `str`, `uuid.UUID`, a tuple of doubles, `Color4f`, `Transform`, `bytes` ||
  a boolean, number, string, array of numbers, `Color4f` or `Transform`]]), and [[`None` || `null`]]
  when there is no such parameter. 8.x always returned a string. [[`SetParameter` ||
  `setParameter`]] accepts the same values.
- [[`RenderContent.TopLevel` || `content.topLevel`]] of a grandchild is its parent, as in RhinoCommon.
- [[`SetChildSlotOn(name, on)` and `SetChildSlotAmount(name, amount)` || `setChildSlotOn(name, on)`
  and `setChildSlotAmount(name, amount)`]] take the slot name first. [[`ChildSlotAmount` ||
  `childSlotAmount`]] of an unset slot is `0.0`.
- [[`RenderSettings.PostEffects` || `renderSettings.postEffects`]] is a `PostEffectCollection` of
  `PostEffectData`: iterate it, or use [[`PostEffectDataFromId(id)` || `postEffectDataFromId(id)`]].
- [[`PhysicallyBasedMaterial.BaseColor` (and `Emission`, `SubsurfaceScatteringColor`) ||
  `pbr.baseColor` (and `emission`, `subsurfaceScatteringColor`)]] is a `Color4f` (`R`, `G`, `B`, `A`),
  not [[a tuple. Python accepts a 4-sequence where a `Color4f` is expected || an array]].
- **Sun**: the date is a [[`datetime.datetime` || `Date`]]: [[`sun.GetDateTime(DateTimeKind.Local)`
  and `sun.SetDateTime(d, DateTimeKind.Local)` || `sun.getDateTime(rhino.DateTimeKind.Local)` and
  `sun.setDateTime(d, rhino.DateTimeKind.Local)`]], in place of the year, month, day and hours
  properties. Times are whole seconds. [[`SunColorFromAltitude` is `ColorFromAltitude` ||
  `sunColorFromAltitude` is `rhino.Sun.colorFromAltitude`]], which rounds each channel (8.x
  truncated, so a channel can differ by 1).
- [[`TextureMapping.RequiresVertexNormals` is gone. Use `MappingType`, `Projection` and `Capped` ||
  `mapping.requiresVertexNormals` is gone. Use `mappingType`, `projection` and `capped`]].
- [[`TextureMapping.TryGetMappingBox`, `TryGetMappingCylinder` and `TryGetMappingPlane` ||
  `tryGetMappingBox`, `tryGetMappingCylinder` and `tryGetMappingPlane`]] with the capped `out` values
  are [[`TryGetMappingBoxCapped`, `TryGetMappingCylinderCapped` and `TryGetMappingPlaneCapped` ||
  `tryGetMappingBoxCapped`, `tryGetMappingCylinderCapped` and `tryGetMappingPlaneCapped`]].
- [[`ObjectAttributes.DrawColor(file)` || `attributes.drawColor(file)`]] for a colour by material uses
  Rhino's rules: the material the attributes select, and its PBR base colour when it is physically
  based.

## Renamed classes

- These 8.x classes have RhinoCommon's names. Their members keep their names, unless
  [[[Renamed and removed members](python-members.md) || [Renamed and removed members](javascript-members.md)]]
  lists them:

  | 8.x | 9.2 |
  |---|---|
  | `Text` | `TextEntity` |
  | `DimLinear`, `DimAngular`, `DimRadial`, `DimOrdinate` | `LinearDimension`, `AngularDimension`, `RadialDimension`, `OrdinateDimension` |
  | `InstanceDefinition` | `InstanceDefinitionGeometry` |
  | `InstanceReference` | `InstanceReferenceGeometry` |
  | `EmbeddedFile` | `File3dmEmbeddedFile` |
  | `File3dmEmbeddedFileTable` | `File3dmEmbeddedFiles` |
  | `MeshNormalList` | `MeshVertexNormalList` |
  | `CurvePiping`, `Displacement`, `EdgeSoftening`, `ShutLining`, `Thickening` | `File3dmCurvePiping`, `File3dmDisplacement`, `File3dmEdgeSoftening`, `File3dmShutLining`, `File3dmThickening` |
  | `RenderContent` | `File3dmRenderContent` |
  | `RenderMaterial`, `RenderEnvironment`, `RenderTexture` | `File3dmRenderMaterial`, `File3dmRenderEnvironment`, `File3dmRenderTexture` |
  | `PostEffect` | `PostEffectData` |
  | `File3dmDecalTable` | `Decals` |
  | `File3dmPostEffectTable` | `PostEffectCollection` |

- `PointGrid` and `File3dmRdkDocumentData` are gone. [[`File3dm.RdkXml` || `file3dm.rdkXml`]] still
  returns the document's RDK XML.

## Enums

- Every enum is RhinoCommon's, with RhinoCommon's names and values. Fifteen 8.x enums had no
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
  | `LengthDisplay` | `DimensionStyle.LengthDisplay` |
  | `MaskFrame` | `DimensionStyle.MaskFrame` |
  | `MaskType` | `DimensionStyle.MaskType` |
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
  | `LengthDisplay` | `DimensionStyleLengthDisplay` |
  | `MaskFrame` | `DimensionStyleMaskFrame` |
  | `MaskType` | `DimensionStyleMaskType` |
  | `PostEffectTypes` | `PostEffectType` |
  | `RenderChannelsModes` | `RenderChannelsModes` |

  An enum nested in a class in RhinoCommon joins the two names (`DimensionStyle.ArrowType` is
  `DimensionStyleArrowType`).
  <!-- /js -->
- Some values are numbered differently: [[`Dithering.Methods` || `DitheringMethods`]] is
  `None = 0, FloydSteinberg = 1, SimpleNoise = 2` (8.x: `SimpleNoise = 0`), and `PostEffectType` is
  `Early = 0, ToneMapping = 1, Late = 2`.
- `LightStyle.Unknown` is `LightStyle.None`.
- `BlendContinuity`, `CurveExtensionStyle`, `CurveKnotStyle`, `CurveOffsetCornerStyle`, `LoftType`,
  `PointContainment` and `RegionContainment` are gone: no 9.2 member takes or returns them.
- <!-- js -->`DimensionStyleField.ArrowSize` and `LeaderArrowSize` are `Arrowsize` and
  `LeaderArrowsize`, RhinoCommon's spelling.<!-- /js -->
- <!-- py -->`None` is a keyword, so a value named `None` is also available as `None_`
  (`rhino3dm.LightStyle.None_`). `E['None']` and `getattr(E, 'None')` work too, and the value's name
  is still `'None'`.<!-- /py -->
- [[Enums are integer-like (`int(e)` and `e == 2` work), but passing a plain int where an enum is
  expected raises `TypeError` || A number or string where an enum is expected throws `TypeError`; 8.x
  silently used 0]].

## Errors, values and identity

### Errors

<!-- py -->
| .NET | Python |
|---|---|
| `IndexOutOfRangeException`, `ArgumentOutOfRangeException` | `IndexError` |
| `ArgumentNullException`, `ArgumentException` | `ValueError` |
| `InvalidOperationException`, `NotSupportedException` | `RuntimeError` |
| `FileNotFoundException` | `FileNotFoundError` |
<!-- /py --><!-- js -->
An error is a `WebAssembly.Exception`. Read its text with `rhino.getExceptionMessage(e)`, and free it
with `rhino.decrementExceptionRefcount(e)`, or it leaks:

```js
// 8.x
curve.spanDomain(-1)   // aborted the module: nothing to catch

// 9.2
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

Wherever RhinoCommon takes a sequence of points, [[any sequence works (a list, a tuple, a
`Point3dList`) || an array works]]. [[`GetUserStrings()` || `getUserStrings()`]] returns
[[a list of `[key, value]` lists || an array of `[key, value]` arrays]].

### Strings and values

<!-- py -->
- `str(point)` is RhinoCommon's `ToString()`: full precision (`0.30000000000000004`, where 8.x printed 6
  significant digits). `str` works on every value type and on classes (`str(layer)` is its full path).
- `repr(Point3d(1, 2, 3))` is `Point3d(1.0, 2.0, 3.0)`.
<!-- /py -->
- An empty string reads `''` where RhinoCommon returns `null` ([[`ViewInfo().Name` || `new rhino.ViewInfo().name`]],
  the log of [[`IsValidWithLog()` || `isValidWithLog()`]] when there is nothing to report): test with
  [[`if not name:` || `if (!name)`]], which is true for both.
- [[`ViewportInfo.ScreenPort` and `GetScreenPort()` are || `viewport.screenPort` and
  `getScreenPort()` are]] RhinoCommon's `(x, y, width, height)`, with y the smaller of top and bottom.
  A new [[`ViewportInfo()` || `new rhino.ViewportInfo()`]] reads `(0, 0, 1000, 1000)`.
- [[`File3dm.FromByteArray` takes any buffer (`bytes`, `bytearray`, numpy) ||
  `rhino.File3dm.fromByteArray` takes a `Uint8Array` or an `ArrayBuffer`; anything else throws
  `TypeError`]].

### Object identity

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
- The package's main module is 32-bit WebAssembly, as in 8.x: it runs in every current browser,
  Safari and iOS included, and in Node 22+. A 64-bit-memory (memory64) build is in `wasm64/`, as in
  8.x, for models that need more than 4 GB; it runs in Chrome 133+, Firefox 134+ and Node 24+
  (Safari supports memory64 only in its Technology Preview) and is slower.
- The package ships typings (`rhino3dm.d.ts`) with RhinoCommon's documentation, so editors complete
  and type-check rhino3dm code.
<!-- /js -->
