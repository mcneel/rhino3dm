# Perf run 2026-11-13: 9.2.0-beta.0 vs 8.35.0

| | |
|---|---|
| Date | 2026-11-13 12:35 +0100 |
| Commit | `351d7e0c9116315ddab8a4273e4d8188af004b5e` (luis/9.x/RH3DM-219 + uncommitted changes in src/) |
| Environment | `linux-arm64-docker`: Apple M1 Max, Docker 8 CPUs |
| Builds | round.sh dev builds (-ffp-contract=off, no LTO) vs the published 8.35.0 packages |
| Data | model scale `small` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 3 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 8.35.0 time / 9.2.0-beta.0 time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | wasm -O3 for zlib/Draco/unicode, G154 table iterators, G155 view cache, G156 overload dispatch and strings (this commit); the only uncommitted change in src/ is the opennurbs submodule's working tree |

## Python

8.35.0 is `8.35.0`, 9.2.0-beta.0 is `9.2.0b0`; CPython 3.12.3. 36 tasks: 36 faster, 0 slower, 0 the same; geometric mean 5.61x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 494 | 172 | 2.88x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 288 | 89.6 | 3.21x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 475 | 156 | 3.04x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,001 | 6,588 | 2.28x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 36.6 | 3.1 | 11.71x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 25.9 | 3.1 | 8.25x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.41 | 0.07 | 5.71x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 252 | 65.8 | 3.83x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 6,109 | 772 | 7.91x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,213 | 921 | 2.40x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 6,135 | 771 | 7.96x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 183 | 27.6 | 6.63x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 76.1 | 10.3 | 7.41x faster |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 209 | 20.1 | 10.43x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 162 | 17.6 | 9.20x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 264 | 29.6 | 8.93x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 233 | 8.0 | 29.24x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 225 | 30.5 | 7.41x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 279 | 33.4 | 8.36x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 245 | 38.3 | 6.38x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 245 | 35.2 | 6.98x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 400 | 43.4 | 9.21x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 287 | 52.6 | 5.45x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 6.7 | 2.9 | 2.27x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 99.4 | 8.5 | 11.75x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 52.9 | 4.7 | 11.19x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 49.0 | 9.0 | 5.47x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 18.2 | 4.4 | 4.12x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 5.7 | 2.6 | 2.24x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 2.4 | 0.27 | 8.81x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.38 | 0.18 | 2.10x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 204 | 11.6 | 17.57x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 65.0 | 8.9 | 7.27x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 9.8 | 4.9 | 2.00x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 22.9 | 10.7 | 2.14x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,381 | 1,887 | 1.79x faster |
| `mesh.vertices_numpy_view_sum_40k` | numpy.asarray(Vertices).sum(), 40,401 vertices (9.x only) | - | 49.5 | - |

## .NET

8.35.0 is `8.35.0.0`, 9.2.0-beta.0 is `9.2.0.0`; .NET 10.0.12. 30 tasks: 29 faster, 1 slower, 0 the same; geometric mean 1.73x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 522 | 180 | 2.90x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 324 | 146 | 2.22x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 506 | 168 | 3.01x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,328 | 6,744 | 2.27x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 4.5 | 2.7 | 1.71x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 4.1 | 2.3 | 1.78x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.15 | 0.08 | 1.84x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 44.4 | 31.0 | 1.43x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,660 | 821 | 2.02x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,262 | 981 | 2.31x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,753 | 851 | 2.06x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.09 | 0.08 | 1.06x faster † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.41 | 0.38 | 1.08x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 15.0 | 8.2 | 1.84x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 29.4 | 12.2 | 2.41x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 26.8 | 14.6 | 1.84x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 8.2 | 5.8 | 1.42x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 2.5 | 1.7 | 1.46x faster † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 7.3 | 5.7 | 1.28x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 7.7 | 4.4 | 1.75x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 0.92 | 0.62 | 1.50x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 1.0 | 0.69 | 1.44x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.60 | 0.43 | 1.39x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.5 | 5.0 | 1.89x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 0.63 | 0.37 | 1.72x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.09 | 0.11 | **1.24x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.67 | 0.34 | 1.96x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 5.2 | 2.6 | 2.01x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 15.4 | 12.1 | 1.27x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,844 | 1,573 | 2.44x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 16.6 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 8.3 | - |

## JavaScript (wasm32)

8.35.0 is `8.35.26251.13000`, 9.2.0-beta.0 is `9.2.26314.02300`; node v24.22.2. 32 tasks: 15 faster, 11 slower, 6 the same; geometric mean 1.27x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 75.6 | 110 | **1.45x slower** |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 542 | 210 | 2.59x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 297 | 116 | 2.56x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,607 | 9,179 | 1.59x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 10.4 | 8.5 | 1.23x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 10.2 | 8.8 | 1.16x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.38 | 0.44 | **1.18x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 89.5 | 86.7 | same (1.03x) |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,777 | 914 | 6.32x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,282 | 1,404 | 1.63x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,766 | 909 | 6.34x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 61.7 | 98.2 | **1.59x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 26.1 | 31.8 | **1.22x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 19.4 | 16.7 | 1.16x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 22.4 | 20.0 | 1.12x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 36.9 | 29.7 | 1.24x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 63.3 | 60.4 | same (1.05x) |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 11.6 | 13.5 | **1.16x slower** |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 3.4 | 2.9 | 1.19x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 8.8 | 5.9 | 1.50x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 36.4 | 5.3 | 6.82x faster † |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 104 | 122 | **1.16x slower** |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 1.0 | 1.0 | same (1.04x) † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 11.5 | 11.4 | same (1.01x) |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 1.9 | 2.2 | **1.17x slower** † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 1.3 | 1.3 | same (1.00x) † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.44 | 0.54 | **1.22x slower** † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 36.5 | 49.5 | **1.36x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 27.2 | 30.3 | **1.11x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 9.1 | 8.2 | 1.11x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 20.5 | 26.2 | **1.28x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,653 | 1,678 | same (0.99x) |
| `mesh.vertices_toFloatArray_40k` | Vertices.toFloatArray, 40,401 vertices (9.x only) | - | 29.5 | - |
