// three.js interop: toThreejsBuffers on Mesh and PointCloud, and its inverse createFromThreejsBuffers.
// JS only. The buffers are typed arrays three.js uses directly (position / normal / uv / color
// Float32Array, index Uint32Array; quads are split into two triangles).
// Hand-maintained members of Mesh; the generated binding is bnd_mesh_gen.h (tools/generate/bnd_gen.py).

#pragma once

#include "bnd_common.h"
#include "bnd_mesh_gen.h"

// rotateToYUp: Rhino's Z-up to three.js' Y-up (a -90 degree turn about X), on a copy
BND_DICT BND_Mesh_ToThreejsBuffers(const BND_Mesh& mesh, bool rotateToYUp);

// the inverse (new objects, owned): rotateFromYUp undoes toThreejsBuffers(true)'s turn. A mesh needs
// `position` (3 numbers a vertex); `index` (3 a triangle) or, without one, every 3 vertices a triangle;
// `normal`, `uv` (2 a vertex), `color` (r, g, b in 0..1) are optional and must match the vertex count
BND_Mesh* BND_Mesh_CreateFromThreejsBuffers(BND_DICT buffers, bool rotateFromYUp);

// Deprecated: the three.js JSON members from rhino3dm 8.x. Each logs a one-time warning pointing to
// the buffer functions above. The JSON is three.js BufferGeometry, as BufferGeometryLoader reads it:
// {data: {index: {type, array}, attributes: {position|normal|uv|color: {itemSize, type, array}}}}.
// Same behaviour as 8.x (createFromThreejsJSON turns meshes from Y-up to Z-up, but not point clouds;
// colors are truncated to bytes; alpha is read as opacity), except that missing normals are computed
// on a copy instead of the caller's mesh, and a face index outside the vertex range throws.
BND_DICT BND_Mesh_ToThreejsJSON(const BND_Mesh& mesh);
BND_DICT BND_Mesh_ToThreejsJSONRotate(const BND_Mesh& mesh, bool rotateToYUp);
// the meshes appended into one (a single mesh: its own JSON)
BND_DICT BND_Mesh_ToThreejsJSONMerged(RH3DM_SeqPtrArg<BND_Mesh> meshes, bool rotateYUp);
// nullptr (null) when `data` has no `data` key
BND_Mesh* BND_Mesh_CreateFromThreejsJSON(BND_DICT data);
