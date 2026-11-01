// three.js interop (Luis 2026-10-08, decided again 2026-10-28 JS2/three.js): keep toThreejsBuffers on
// Mesh and PointCloud, and add its inverse createFromThreejsBuffers, before the JSON family
// (toThreejsJSON, ...) is deprecated. JS only: the buffers are typed arrays three.js takes as they are
// (position / normal / uv / color Float32Array, index Uint32Array; a mesh's quads as two triangles).
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

// the deprecated JSON family (G133; Luis 2026-10-31: shipped deprecated in 9.2, removed later): rhino3dm
// 8.x's members, each warning once (RH3DM_Deprecated) that the buffers above replace it. The objects are
// BufferGeometry JSON as three.js' BufferGeometryLoader reads it: {data: {index: {type, array},
// attributes: {position|normal|uv|color: {itemSize, type, array}}}}, plain arrays of numbers.
// 8.x's behaviour is kept -- createFromThreejsJSON always turns a mesh from Y-up (a point cloud never),
// colors are truncated to bytes and an RGBA color's alpha read as opacity -- except that the normals a
// mesh lacks are computed on a copy (8.x computed them on the caller's mesh), and a face index outside
// the vertices throws (8.x built a mesh that crashed later)
BND_DICT BND_Mesh_ToThreejsJSON(const BND_Mesh& mesh);
BND_DICT BND_Mesh_ToThreejsJSONRotate(const BND_Mesh& mesh, bool rotateToYUp);
// the meshes appended into one (a single mesh: its own JSON)
BND_DICT BND_Mesh_ToThreejsJSONMerged(RH3DM_SeqPtrArg<BND_Mesh> meshes, bool rotateYUp);
// nullptr (null) when `data` has no `data` key
BND_Mesh* BND_Mesh_CreateFromThreejsJSON(BND_DICT data);
