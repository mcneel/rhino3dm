// three.js interop (Luis 2026-10-08, decided again 2026-10-28 JS2/three.js): keep toThreejsBuffers on
// Mesh and PointCloud, and add its inverse createFromThreejsBuffers, before the JSON family
// (toThreejsJSON, ...) is deprecated. JS only: the buffers are typed arrays three.js takes as they are
// (position / normal / uv / color Float32Array, index Uint32Array; a mesh's quads as two triangles).
// rhino3dm overloads RhinoCommon lacks (Luis 2026-10-30: kept by hand, upstream U38): a point cloud
// point with a value but no normal or color, and a group deleted by id. Python registers them as
// overloads of the generated Add / Insert / AddRange / Delete (as the old bindings did); JS keeps
// the old names (addPointValue, ...), as embind cannot hold two overloads of one arity.
// Hand-maintained members of PointCloud; the generated binding is bnd_pointcloud_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_pointcloud_gen.h"
#include "bnd_point3d_gen.h"

BND_DICT BND_PointCloud_ToThreejsBuffers(const BND_PointCloud& cloud, bool rotateToYUp);
BND_PointCloud* BND_PointCloud_CreateFromThreejsBuffers(BND_DICT buffers, bool rotateFromYUp);
BND_DICT BND_PointCloud_ToThreejsJSON(const BND_PointCloud& cloud);
BND_PointCloud* BND_PointCloud_CreateFromThreejsJSON(BND_DICT json);

// every channel the cloud has (normals, colors, hidden flags, values) stays aligned with its points;
// values are added when the cloud has none (0 for the points already there)
void BND_PointCloud_AddValue(const BND_PointCloud& cloud, BND_Point3d point, double value);
void BND_PointCloud_InsertValue(const BND_PointCloud& cloud, int index, BND_Point3d point, double value);
void BND_PointCloud_AddRangeValues(const BND_PointCloud& cloud, RH3DM_SeqArg<BND_Point3d> points, RH3DM_SeqArg<double> values);
