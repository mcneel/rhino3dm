# Perf run 2026-11-11: 9.2.0-beta.0 vs 8.35.0

| | |
|---|---|
| Date | 2026-11-11 12:02 +0100 |
| Commit | `d42a65070f2b25bc3560a475ef0d492682b50d47` (9.x) |
| Environment | `linux-arm64-docker`: Apple M1 Max, Docker 8 CPUs |
| Builds | round.sh dev builds (-ffp-contract=off, no LTO) vs the published 8.35.0 packages |
| Data | model scale `small` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 3 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 8.35.0 time / 9.2.0-beta.0 time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | Baseline run: tests/perf/baseline/linux-arm64-docker.json holds its ratios |

## Python

8.35.0 is `8.35.0`, 9.2.0-beta.0 is `9.2.0b0`; CPython 3.12.3. 36 tasks: 36 faster, 0 slower, 0 the same; geometric mean 5.10x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 501 | 179 | 2.81x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 297 | 100 | 2.97x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 481 | 161 | 2.99x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 16,098 | 7,036 | 2.29x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 38.5 | 3.9 | 9.94x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 28.5 | 3.7 | 7.75x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.42 | 0.11 | 3.93x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 263 | 66.0 | 3.99x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 6,298 | 1,395 | 4.51x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,322 | 955 | 2.43x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 6,258 | 1,382 | 4.53x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 190 | 33.0 | 5.75x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 78.0 | 10.6 | 7.35x faster |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 218 | 20.8 | 10.49x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 162 | 18.1 | 8.92x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 262 | 34.3 | 7.65x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 224 | 8.4 | 26.75x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 233 | 39.3 | 5.94x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 285 | 35.7 | 7.97x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 254 | 56.6 | 4.48x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 255 | 39.7 | 6.41x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 443 | 56.5 | 7.84x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 305 | 53.4 | 5.71x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 6.7 | 3.0 | 2.20x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 102 | 8.6 | 11.79x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 54.9 | 5.1 | 10.67x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 51.0 | 9.0 | 5.70x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 18.9 | 5.2 | 3.62x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 6.2 | 2.6 | 2.36x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 2.6 | 0.29 | 8.96x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.42 | 0.19 | 2.26x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 199 | 12.0 | 16.61x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 66.5 | 9.4 | 7.09x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 10.8 | 5.1 | 2.09x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 25.7 | 11.5 | 2.23x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,438 | 2,939 | 1.17x faster |
| `mesh.vertices_numpy_view_sum_40k` | numpy.asarray(Vertices).sum(), 40,401 vertices (9.x only) | - | 52.5 | - |

## .NET

8.35.0 is `8.35.0.0`, 9.2.0-beta.0 is `9.2.0.0`; .NET 10.0.12. 30 tasks: 24 faster, 1 slower, 5 the same; geometric mean 1.55x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 512 | 184 | 2.78x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 306 | 148 | 2.06x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 488 | 163 | 3.00x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 15,125 | 7,077 | 2.14x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 4.4 | 2.9 | 1.53x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 3.9 | 2.4 | 1.61x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.16 | 0.09 | 1.70x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 43.7 | 28.6 | 1.53x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,635 | 1,580 | same (1.04x) |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,242 | 938 | 2.39x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,650 | 1,581 | same (1.04x) |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.08 | 0.08 | same (1.01x) † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.38 | 0.37 | same (1.02x) † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 14.5 | 8.9 | 1.63x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 28.6 | 12.6 | 2.27x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 21.1 | 12.8 | 1.65x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 7.6 | 6.8 | 1.12x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 2.4 | 3.4 | **1.39x slower** † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 6.9 | 5.7 | 1.21x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 7.9 | 4.0 | 1.96x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 0.90 | 0.58 | 1.56x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 0.94 | 0.66 | 1.42x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.60 | 0.44 | 1.35x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 8.9 | 5.0 | 1.80x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 0.63 | 0.37 | 1.70x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.10 | 0.10 | same (1.05x) † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.63 | 0.33 | 1.92x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 5.4 | 2.7 | 2.00x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 15.8 | 12.3 | 1.28x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,775 | 2,490 | 1.52x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 15.4 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 10.1 | - |

## JavaScript (wasm32)

8.35.0 is `8.35.26251.13000`, 9.2.0-beta.0 is `9.2.26314.02300`; node v24.22.2. 32 tasks: 8 faster, 22 slower, 2 the same; geometric mean **1.24x slower**.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 91.4 | 108 | **1.18x slower** |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 541 | 430 | 1.26x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 292 | 138 | 2.11x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,306 | 15,234 | **1.06x slower** |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 10.2 | 15.8 | **1.54x slower** |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 9.2 | 19.4 | **2.11x slower** † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.39 | 0.58 | **1.51x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 95.1 | 118 | **1.24x slower** |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,798 | 1,876 | 3.09x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,227 | 2,313 | same (0.96x) |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,635 | 1,923 | 2.93x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 62.6 | 521 | **8.33x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 26.3 | 232 | **8.83x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 19.5 | 38.2 | **1.96x slower** |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 22.2 | 41.0 | **1.84x slower** |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 29.8 | 61.9 | **2.08x slower** |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 64.5 | 82.7 | **1.28x slower** |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 11.5 | 13.6 | **1.19x slower** |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 4.4 | 2.7 | 1.65x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 8.7 | 13.5 | **1.55x slower** † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 27.2 | 14.4 | 1.88x faster |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 91.5 | 117 | **1.28x slower** |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 1.00 | 0.99 | same (1.01x) † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 11.9 | 15.7 | **1.32x slower** |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 5.1 | 2.2 | 2.30x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 1.3 | 2.4 | **1.88x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.47 | 0.54 | **1.16x slower** † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 37.8 | 58.0 | **1.54x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 26.2 | 35.0 | **1.33x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 9.4 | 8.7 | 1.09x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 19.9 | 25.1 | **1.26x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,645 | 2,789 | **1.70x slower** |
| `mesh.vertices_toFloatArray_40k` | Vertices.toFloatArray, 40,401 vertices (9.x only) | - | 26.2 | - |
