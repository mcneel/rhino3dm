# Perf run 2026-11-11: 9.2.0-beta.0 vs 8.35.0

| | |
|---|---|
| Date | 2026-11-11 17:23 +0100 |
| Commit | `dd8908dd3943d2916e23ebd6deca2e96efd4fd65` (9.x + uncommitted changes in src/) |
| Environment | `linux-arm64-docker`: Apple M1 Max, Docker 8 CPUs |
| Builds | round.sh dev builds (-ffp-contract=off, no LTO) vs the published 8.35.0 packages |
| Data | model scale `small` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 3 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 8.35.0 time / 9.2.0-beta.0 time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | RH-100236 (opennurbs mesh-modifier user data on every object, temporarily patched into the opennurbs submodule): model writes no longer attach five default mesh-modifier records per object |

## Python

8.35.0 is `8.35.0`, 9.2.0-beta.0 is `9.2.0b0`; CPython 3.12.3. 36 tasks: 36 faster, 0 slower, 0 the same; geometric mean 5.07x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 507 | 175 | 2.90x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 305 | 102 | 2.99x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 487 | 165 | 2.95x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,181 | 6,968 | 2.18x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 39.1 | 4.0 | 9.67x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 27.7 | 3.8 | 7.24x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.44 | 0.13 | 3.34x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 261 | 71.6 | 3.65x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 6,311 | 816 | 7.73x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,276 | 963 | 2.36x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 6,342 | 843 | 7.52x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 196 | 35.5 | 5.51x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 79.9 | 11.0 | 7.26x faster |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 229 | 32.2 | 7.10x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 167 | 19.1 | 8.73x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 290 | 42.2 | 6.88x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 231 | 8.4 | 27.40x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 233 | 42.2 | 5.53x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 295 | 46.2 | 6.39x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 272 | 54.4 | 5.00x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 256 | 37.9 | 6.75x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 448 | 62.4 | 7.17x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 302 | 57.3 | 5.28x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 6.8 | 3.1 | 2.23x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 104 | 8.9 | 11.72x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 57.6 | 5.8 | 9.98x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 50.4 | 9.5 | 5.32x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 20.2 | 5.1 | 3.95x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 6.2 | 2.8 | 2.22x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 2.6 | 0.31 | 8.61x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.42 | 0.21 | 2.06x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 204 | 12.6 | 16.23x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 67.2 | 9.5 | 7.07x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 10.3 | 5.4 | 1.89x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 24.3 | 11.3 | 2.14x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,523 | 2,025 | 1.74x faster |
| `mesh.vertices_numpy_view_sum_40k` | numpy.asarray(Vertices).sum(), 40,401 vertices (9.x only) | - | 54.6 | - |

## .NET

8.35.0 is `8.35.0.0`, 9.2.0-beta.0 is `9.2.0.0`; .NET 10.0.12. 30 tasks: 27 faster, 2 slower, 1 the same; geometric mean 1.63x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 521 | 185 | 2.81x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 321 | 149 | 2.16x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 503 | 168 | 2.99x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,909 | 6,897 | 2.31x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 4.9 | 3.3 | 1.49x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 4.3 | 2.5 | 1.71x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.17 | 0.10 | 1.60x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 47.4 | 31.1 | 1.53x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,715 | 820 | 2.09x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,252 | 962 | 2.34x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,673 | 829 | 2.02x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.08 | 0.08 | same (1.01x) † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.38 | 0.40 | **1.07x slower** † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 15.2 | 8.5 | 1.80x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 29.0 | 12.4 | 2.34x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 21.5 | 15.3 | 1.41x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 8.6 | 6.7 | 1.29x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 3.9 | 3.5 | 1.12x faster † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 8.5 | 6.6 | 1.30x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 8.0 | 4.7 | 1.71x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 0.91 | 0.63 | 1.44x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 0.94 | 0.74 | 1.28x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.60 | 0.46 | 1.30x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.3 | 5.3 | 1.75x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 0.62 | 0.39 | 1.60x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.10 | 0.12 | **1.20x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.63 | 0.36 | 1.77x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 5.3 | 2.7 | 1.92x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 14.8 | 12.7 | 1.17x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,870 | 1,562 | 2.48x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 16.9 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 8.5 | - |

## JavaScript (wasm32)

8.35.0 is `8.35.26251.13000`, 9.2.0-beta.0 is `9.2.26314.02300`; node v24.22.2. 32 tasks: 9 faster, 18 slower, 5 the same; geometric mean **1.16x slower**.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 97.7 | 124 | **1.27x slower** |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 550 | 434 | 1.27x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 302 | 143 | 2.11x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,699 | 14,514 | same (1.01x) |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 11.1 | 15.4 | **1.38x slower** |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 10.1 | 18.4 | **1.81x slower** |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.40 | 0.61 | **1.50x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 124 | 104 | 1.19x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,831 | 1,444 | 4.04x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,277 | 2,276 | same (1.00x) |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,835 | 1,445 | 4.04x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 61.6 | 519 | **8.42x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 26.9 | 236 | **8.77x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 19.4 | 40.7 | **2.10x slower** |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 22.5 | 43.0 | **1.91x slower** |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 35.9 | 53.6 | **1.49x slower** |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 63.4 | 84.5 | **1.33x slower** |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 12.1 | 12.9 | **1.07x slower** |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 4.5 | 2.7 | 1.64x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 8.7 | 13.9 | **1.60x slower** † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 27.5 | 14.4 | 1.91x faster |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 90.1 | 121 | **1.35x slower** |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 1.1 | 1.0 | same (1.04x) † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 11.8 | 15.1 | **1.28x slower** |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 2.3 | 2.4 | same (0.99x) † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 1.3 | 2.2 | **1.64x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.54 | 0.40 | 1.37x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 37.4 | 56.8 | **1.52x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 26.9 | 35.1 | **1.30x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 9.4 | 7.7 | 1.22x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 20.7 | 24.6 | **1.19x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,685 | 1,649 | same (1.02x) |
| `mesh.vertices_toFloatArray_40k` | Vertices.toFloatArray, 40,401 vertices (9.x only) | - | 25.3 | - |
