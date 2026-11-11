# Perf run 2026-11-11: 9.2.0-beta.0 vs 8.35.0

| | |
|---|---|
| Date | 2026-11-11 12:30 +0100 |
| Commit | `d42a65070f2b25bc3560a475ef0d492682b50d47` (9.x) |
| Environment | `linux-arm64-docker`: Apple M1 Max, Docker 8 CPUs |
| Builds | round.sh dev builds (-ffp-contract=off, no LTO) vs the published 8.35.0 packages |
| Data | model scale `small` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 3 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 8.35.0 time / 9.2.0-beta.0 time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | Gate check of the unchanged tree right after the baseline: PASSED (56 gated tasks, ratio to baseline 0.74-1.41) |

## Python

8.35.0 is `8.35.0`, 9.2.0-beta.0 is `9.2.0b0`; CPython 3.12.3. 36 tasks: 35 faster, 0 slower, 1 the same; geometric mean 4.64x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 510 | 174 | 2.93x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 312 | 92.9 | 3.36x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 497 | 161 | 3.08x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,336 | 7,334 | 2.09x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 40.8 | 4.1 | 9.92x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 28.5 | 3.9 | 7.27x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.45 | 0.13 | 3.39x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 276 | 71.9 | 3.83x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 6,160 | 1,485 | 4.15x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,225 | 977 | 2.28x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 6,139 | 1,469 | 4.18x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 191 | 32.6 | 5.85x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 77.6 | 10.9 | 7.14x faster |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 212 | 30.2 | 7.00x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 162 | 18.6 | 8.75x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 265 | 41.6 | 6.38x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 223 | 8.3 | 26.72x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 233 | 43.3 | 5.38x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 284 | 46.4 | 6.11x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 240 | 73.3 | 3.27x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 255 | 39.2 | 6.51x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 414 | 65.7 | 6.31x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 300 | 54.6 | 5.49x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 6.9 | 3.2 | 2.17x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 103 | 8.8 | 11.72x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 54.7 | 7.1 | 7.75x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 50.1 | 9.1 | 5.49x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 20.6 | 7.3 | 2.83x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 5.8 | 2.8 | 2.07x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 2.6 | 0.32 | 8.07x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.40 | 0.22 | 1.87x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 196 | 13.5 | 14.48x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 67.9 | 9.8 | 6.90x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 10.4 | 5.5 | 1.89x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 25.3 | 11.5 | 2.21x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,410 | 3,295 | same (1.03x) |
| `mesh.vertices_numpy_view_sum_40k` | numpy.asarray(Vertices).sum(), 40,401 vertices (9.x only) | - | 54.7 | - |

## .NET

8.35.0 is `8.35.0.0`, 9.2.0-beta.0 is `9.2.0.0`; .NET 10.0.12. 30 tasks: 27 faster, 0 slower, 3 the same; geometric mean 1.64x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 507 | 177 | 2.86x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 297 | 140 | 2.12x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 484 | 162 | 2.98x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,113 | 7,050 | 2.14x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 4.4 | 2.8 | 1.58x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 3.9 | 2.3 | 1.69x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.14 | 0.09 | 1.60x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 41.7 | 28.8 | 1.45x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,660 | 1,485 | 1.12x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,234 | 940 | 2.38x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,634 | 1,488 | 1.10x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.08 | 0.08 | same (1.00x) † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.37 | 0.38 | same (0.98x) † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 14.4 | 7.8 | 1.84x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 28.3 | 11.7 | 2.43x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 20.8 | 12.7 | 1.64x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 7.5 | 5.7 | 1.31x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 2.5 | 1.7 | 1.42x faster † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 7.0 | 4.8 | 1.45x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 7.7 | 4.1 | 1.90x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 0.90 | 0.59 | 1.53x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 0.93 | 0.66 | 1.41x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.59 | 0.42 | 1.42x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.0 | 4.5 | 2.01x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 0.63 | 0.32 | 1.97x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.09 | 0.09 | same (0.96x) † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.60 | 0.31 | 1.92x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 5.1 | 2.2 | 2.31x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 14.0 | 11.2 | 1.25x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,777 | 2,489 | 1.52x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 15.3 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 10.0 | - |

## JavaScript (wasm32)

8.35.0 is `8.35.26251.13000`, 9.2.0-beta.0 is `9.2.26314.02300`; node v24.22.2. 32 tasks: 7 faster, 21 slower, 4 the same; geometric mean **1.25x slower**.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 70.2 | 102 | **1.45x slower** |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 538 | 431 | 1.25x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 295 | 137 | 2.15x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,090 | 14,284 | same (0.99x) |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 9.3 | 14.3 | **1.54x slower** † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 9.6 | 18.7 | **1.95x slower** † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.33 | 0.52 | **1.55x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 98.1 | 101 | same (0.97x) |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,585 | 1,798 | 3.11x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,213 | 2,215 | same (1.00x) |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,568 | 1,789 | 3.11x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 57.6 | 494 | **8.56x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 24.9 | 221 | **8.87x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 19.3 | 37.5 | **1.94x slower** |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 22.3 | 39.8 | **1.79x slower** |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 28.9 | 54.2 | **1.87x slower** |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 60.3 | 82.0 | **1.36x slower** |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 11.4 | 12.0 | **1.05x slower** |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 3.3 | 2.6 | 1.23x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 8.4 | 12.5 | **1.50x slower** † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 34.8 | 13.5 | 2.57x faster |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 107 | 118 | **1.10x slower** |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 0.83 | 0.86 | same (0.97x) † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 11.0 | 15.0 | **1.36x slower** |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 1.9 | 2.2 | **1.14x slower** † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 1.2 | 2.2 | **1.85x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.41 | 0.47 | **1.14x slower** † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 34.7 | 53.6 | **1.54x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 25.6 | 33.0 | **1.29x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 9.1 | 8.0 | 1.13x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 19.5 | 24.1 | **1.24x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,594 | 3,024 | **1.90x slower** |
| `mesh.vertices_toFloatArray_40k` | Vertices.toFloatArray, 40,401 vertices (9.x only) | - | 25.5 | - |
