# Perf run 2026-11-10: 9.2.0-beta.0 vs 8.35.0

| | |
|---|---|
| Date | 2026-11-10 22:15 +0100 |
| Commit | `fbde8a4b814ea9859feb1bede6a26b4ef26e46ca` (9.x) |
| Environment | `m1max-mixed`: Apple M1 Max; Python and .NET in Docker (8 CPUs), JS on the macOS host |
| Builds | Python and .NET native: Release, no LTO (as setup.py); JS: the round.sh dev wasm32 and wasm64 builds; vs the published 8.35.0 packages |
| Data | model scale `full` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 2 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 8.35.0 time / 9.2.0-beta.0 time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | First comparison, made with an earlier version of these benches; JS before the overload-dispatch fix (RH3DM-217). No shuffled PointAt task yet |

## Python

8.35.0 is `8.35.0`, 9.2.0-beta.0 is `9.2.0b0`; CPython 3.12.3. 35 tasks: 33 faster, 1 slower, 1 the same; geometric mean 4.66x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 2,706 | 895 | 3.02x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 319 | 452 | **1.41x slower** |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 2,678 | 829 | 3.23x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 82,998 | 38,445 | 2.16x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 207 | 23.3 | 8.86x faster |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 163 | 19.9 | 8.18x faster |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 2.3 | 0.58 | 3.91x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 320 | 76.0 | 4.21x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 6,706 | 1,522 | 4.41x faster |
| `create.write_50_regular_grid_meshes_v8 (zlib worst case)` |  | 2,396 | 1,015 | 2.36x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 6,574 | 1,511 | 4.35x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 234 | 42.1 | 5.55x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 81.1 | 10.8 | 7.53x faster |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 241 | 35.0 | 6.89x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 168 | 19.4 | 8.64x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 289 | 48.5 | 5.96x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 237 | 8.4 | 28.20x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 267 | 44.6 | 5.99x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 328 | 49.4 | 6.64x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 284 | 40.9 | 6.95x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 495 | 64.5 | 7.68x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 316 | 56.6 | 5.58x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 7.1 | 3.1 | 2.30x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 119 | 8.7 | 13.58x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 61.5 | 7.8 | 7.86x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 53.4 | 9.6 | 5.58x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 20.4 | 7.2 | 2.82x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 31.5 | 14.3 | 2.20x faster |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 2.6 | 0.33 | 7.96x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.41 | 0.21 | 1.91x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 210 | 13.3 | 15.81x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 69.8 | 9.5 | 7.32x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 10.4 | 5.3 | 1.96x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 24.7 | 12.2 | 2.03x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,624 | 3,595 | same (1.01x) |
| `mesh.vertices_numpy_view_sum_40k (9.2 only)` |  | - | 56.4 | - |

## .NET

8.35.0 is `8.35`, 9.2.0-beta.0 is `9.2`; .NET 10.0.12. 29 tasks: 24 faster, 3 slower, 2 the same; geometric mean 1.58x faster.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 2,568 | 921 | 2.79x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 376 | 181 | 2.08x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 2,542 | 833 | 3.05x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 83,548 | 40,251 | 2.08x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 26.4 | 17.8 | 1.48x faster |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 18.9 | 11.6 | 1.63x faster |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.68 | 0.47 | 1.45x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 47.1 | 32.6 | 1.44x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,670 | 1,690 | same (0.99x) |
| `create.write_50_regular_grid_meshes_v8 (zlib worst case)` |  | 2,263 | 1,007 | 2.25x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,656 | 1,710 | same (0.97x) |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.09 | 0.09 | **1.07x slower** † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.40 | 0.52 | **1.31x slower** † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 15.2 | 8.3 | 1.83x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 29.3 | 12.3 | 2.38x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 8.7 | 7.1 | 1.23x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 5.4 | 1.8 | 2.99x faster † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 8.3 | 4.0 | 2.08x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 8.2 | 4.6 | 1.77x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 1.2 | 0.64 | 1.93x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 0.92 | 0.72 | 1.27x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.90 | 0.48 | 1.85x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.5 | 5.5 | 1.73x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 3.0 | 2.0 | 1.48x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.09 | 0.13 | **1.36x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.62 | 0.44 | 1.41x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 5.1 | 2.7 | 1.88x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 15.0 | 13.9 | 1.08x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,940 | 2,796 | 1.41x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 17.8 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 11.4 | - |

## JavaScript (wasm32)

8.35.0 is `8.35`, 9.2.0-beta.0 is `9.2`; node v24.14.1 (macOS arm64 host). 31 tasks: 7 faster, 21 slower, 3 the same; geometric mean **1.29x slower**.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 53.7 | 64.0 | **1.19x slower** |
| `io.read_model_file` | File3dm.Read of the model | 2,713 | 2,087 | 1.30x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 287 | 138 | 2.08x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 72,389 | 75,011 | same (0.97x) |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 28.6 | 46.1 | **1.61x slower** |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 27.9 | 64.7 | **2.32x slower** |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.80 | 1.5 | **1.85x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 54.5 | 75.7 | **1.39x slower** |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,670 | 1,783 | 3.18x faster |
| `create.write_50_regular_grid_meshes_v8 (zlib worst case)` |  | 2,278 | 2,318 | same (0.98x) |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,644 | 1,875 | 3.01x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 47.1 | 2,780 | **59.09x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 20.5 | 154 | **7.50x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 17.1 | 25.6 | **1.50x slower** |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 19.4 | 28.2 | **1.45x slower** |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 46.9 | 51.7 | **1.10x slower** |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 7.4 | 8.1 | **1.09x slower** † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 2.8 | 3.0 | **1.11x slower** † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 5.2 | 7.5 | **1.45x slower** † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 22.9 | 7.3 | 3.12x faster † |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 61.9 | 68.8 | **1.11x slower** |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 0.78 | 0.74 | 1.06x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 7.9 | 11.1 | **1.40x slower** † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 4.5 | 6.3 | **1.41x slower** † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.67 | 1.5 | **2.24x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.43 | 0.52 | **1.20x slower** † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 35.3 | 52.2 | **1.48x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 25.9 | 32.4 | **1.25x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 8.0 | 6.5 | 1.23x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 18.5 | 21.7 | **1.18x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,629 | 1,579 | same (1.03x) |
| `mesh.vertices_toFloatArray_40k (9.2 only)` |  | - | 20.1 | - |

## JavaScript (wasm64 / memory64)

8.35.0 is `8.35`, 9.2.0-beta.0 is `9.2`; node v24.14.1 (macOS arm64 host). 31 tasks: 7 faster, 18 slower, 6 the same; geometric mean **1.25x slower**.

| Task | Parameters | 8.35.0 (ms) | 9.2.0-beta.0 (ms) | Factor |
|---|---|--:|--:|---|
| `module.init` | load and instantiate the wasm module | 44.4 | 72.0 | **1.62x slower** |
| `io.read_model_file` | File3dm.Read of the model | 2,795 | 2,161 | 1.29x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 321 | 144 | 2.22x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 75,886 | 74,888 | same (1.01x) |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 63.9 | 96.3 | **1.51x slower** |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 57.6 | 156 | **2.71x slower** |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 1.3 | 2.3 | **1.74x slower** † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 156 | 174 | **1.11x slower** |
| `create.write_populated_v8` | write that populated model, archive version 8 | 5,773 | 1,886 | 3.06x faster |
| `create.write_50_regular_grid_meshes_v8 (zlib worst case)` |  | 2,291 | 2,299 | same (1.00x) |
| `create.write_populated_default_version` | write the populated model, default archive version | 5,772 | 1,891 | 3.05x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 77.0 | 6,427 | **83.48x slower** |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 113 | 535 | **4.75x slower** |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 75.7 | 84.5 | **1.12x slower** |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 83.0 | 89.4 | **1.08x slower** |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 174 | 175 | same (1.00x) |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 15.4 | 16.1 | same (0.95x) |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 3.0 | 2.6 | 1.12x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 23.3 | 26.1 | **1.12x slower** |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 120 | 30.1 | 3.98x faster |
| `mesh.toThreejsJSON_40k` | toThreejsJSON of the 200x200 mesh | 346 | 352 | same (0.98x) |
| `mesh.toThreejsBuffers_40k` | toThreejsBuffers of the 200x200 mesh | 0.85 | 0.94 | **1.10x slower** † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 10.5 | 23.2 | **2.20x slower** |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 9.7 | 10.9 | **1.12x slower** † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 1.1 | 2.5 | **2.26x slower** † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.39 | 0.45 | **1.13x slower** † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 41.4 | 57.7 | **1.39x slower** |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 29.4 | 38.6 | **1.31x slower** |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 11.2 | 9.8 | 1.14x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 22.9 | 31.2 | **1.36x slower** |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 1,642 | 1,580 | same (1.04x) |
| `mesh.vertices_toFloatArray_40k (9.2 only)` |  | - | 78.1 | - |
