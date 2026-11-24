# Perf run 2026-11-24: 9.2.0-beta.2 + build overhaul (Release) vs 9.2.0-beta.2

| | |
|---|---|
| Date | 2026-11-24 15:55 |
| Commit | `cc0818a8` (9.x) |
| Environment | `linux-arm64-docker` |
| Builds | reference: the published 9.2.0-beta.2 Linux packages (PyPI manylinux_2_28 aarch64 abi3 wheel, NuGet linux-arm64 native library); current: Release builds of this tree through the new root CMake (python build.py py, python build.py dotnet), working tree uncommitted |
| Data | model scale `small` (tests/perf/gen_model.py, written by 8.35.0) + tests/models |
| Method | one warm-up, median of 5 repeats (2 when the warm-up took over 1 s); 3 round(s), reference and current alternating; each task keeps its lowest median |
| Factor | 9.2.0-beta.2 time / 9.2.0-beta.2 + build overhaul (Release) time; "same" is within 5%; † = under 10 ms on a side (noisy) |
| Notes | Build overhaul: the published 9.2.0-beta.2 Linux builds against Release builds of the same sources. The released manylinux wheels were Debug builds (setup.py bdist_wheel build --debug) and the Linux .NET native library had no CMAKE_BUILD_TYPE; the overhaul builds both Release |

## Python

9.2.0-beta.2 is `9.2.0b2`, 9.2.0-beta.2 + build overhaul (Release) is `9.2.0b2`; CPython 3.12.16. 37 tasks: 37 faster, 0 slower, 0 the same; geometric mean 3.01x faster.

| Task | Parameters | 9.2.0-beta.2 (ms) | 9.2.0-beta.2 + build overhaul (Release) (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 357 | 170 | 2.10x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 160 | 96.0 | 1.67x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 344 | 155 | 2.21x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,962 | 6,602 | 2.27x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 9.5 | 1.8 | 5.21x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 9.1 | 2.0 | 4.57x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.16 | 0.05 | 3.04x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 131 | 56.9 | 2.30x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,569 | 774 | 2.03x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,200 | 932 | 2.36x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,572 | 761 | 2.07x faster |
| `values.point3d_construct_100k` | 100,000 Point3d(i, i, i) | 65.6 | 18.6 | 3.53x faster |
| `values.point3d_distance_100k` | 100,000 Point3d.DistanceTo | 15.5 | 6.8 | 2.29x faster † |
| `values.point3d_add_100k` | 100,000 Point3d + Point3d | 64.4 | 14.1 | 4.56x faster |
| `values.point3d_get_xyz_100k` | 100,000 reads of X + Y + Z | 18.8 | 10.6 | 1.78x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 71.6 | 25.8 | 2.77x faster |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 9.7 | 4.5 | 2.15x faster † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 86.7 | 22.7 | 3.81x faster |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 95.3 | 25.9 | 3.67x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 101 | 31.9 | 3.15x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 65.5 | 26.9 | 2.43x faster |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 167 | 26.8 | 6.22x faster |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 119 | 40.9 | 2.91x faster |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 6.7 | 3.0 | 2.22x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 20.3 | 4.1 | 4.95x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 16.2 | 3.2 | 4.99x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 19.8 | 4.8 | 4.15x faster † |
| `mesh.vertices_numpy_view_sum_40k` | numpy.asarray(Vertices).sum(), 40,401 vertices (9.x only) | 48.9 | 11.8 | 4.13x faster |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.2 | 4.3 | 2.16x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 4.0 | 2.4 | 1.64x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.78 | 0.15 | 5.18x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.40 | 0.19 | 2.06x faster † |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | 197 | 11.8 | 16.72x faster |
| `draco.decompress_grid_200` | Draco decompress that base64 string | 67.7 | 8.9 | 7.58x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 7.5 | 4.8 | 1.57x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 19.9 | 10.4 | 1.92x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,445 | 1,946 | 1.77x faster |

## .NET

9.2.0-beta.2 is `9.2.0.0`, 9.2.0-beta.2 + build overhaul (Release) is `9.2.0.0`; .NET 10.0.12. 30 tasks: 28 faster, 0 slower, 2 the same; geometric mean 1.64x faster.

| Task | Parameters | 9.2.0-beta.2 (ms) | 9.2.0-beta.2 + build overhaul (Release) (ms) | Factor |
|---|---|--:|--:|---|
| `io.read_model_file` | File3dm.Read of the model | 360 | 177 | 2.03x faster |
| `io.read_test_models` | read every tests/models/*.3dm | 173 | 99.0 | 1.75x faster |
| `io.from_byte_array_model` | File3dm.FromByteArray of the model bytes | 349 | 160 | 2.19x faster |
| `io.write_model_file_v8` | write the loaded model, archive version 8 | 14,935 | 6,583 | 2.27x faster |
| `table.iterate_objects_geometry_attrs` | every object of the model: Geometry, Attributes.LayerIndex, ObjectType | 3.9 | 2.8 | 1.40x faster † |
| `table.iterate_objects_bbox` | every object of the model: Geometry.GetBoundingBox | 3.7 | 2.2 | 1.68x faster † |
| `table.iterate_layers` | every layer of the model: Name, Index, visibility | 0.13 | 0.09 | 1.37x faster † |
| `create.populate_model` | new File3dm: 1,000 layers, 3,000 points, 3,000 curves (20 cps, degree 3), 300 spheres, 300 meshes 30x30 | 43.3 | 28.2 | 1.53x faster |
| `create.write_populated_v8` | write that populated model, archive version 8 | 1,590 | 772 | 2.06x faster |
| `create.write_50_regular_grid_meshes_v8` | 50 meshes 30x30 on an integer grid (zlib worst case), version 8 | 2,215 | 924 | 2.40x faster |
| `create.write_populated_default_version` | write the populated model, default archive version | 1,587 | 774 | 2.05x faster |
| `values.transform_multiply_100k` | 100,000 Transform.Multiply (translation x translation) | 0.08 | 0.08 | same (1.05x) † |
| `values.point3d_transform_100k` | 100,000 Point3d.Transform(translation) | 0.39 | 0.38 | same (1.03x) † |
| `curve.point_at_100k` | one curve (200 cps, degree 3), 100,000 PointAt at sorted parameters | 14.3 | 7.8 | 1.84x faster † |
| `curve.tangent_at_100k` | the same curve, 100,000 TangentAt at sorted parameters | 28.3 | 11.7 | 2.42x faster |
| `curve.point_at_100k_shuffled_1k_curves` | 1,000 curves (200 cps), 100,000 PointAt in shuffled order (seed 7) | 21.6 | 11.8 | 1.83x faster |
| `curve.create_control_point_5k` | 5,000 Curve.CreateControlPointCurve (20 cps, degree 3) | 8.1 | 6.0 | 1.34x faster † |
| `curve.nurbs_points_read_200x500` | 500 times: read all 200 control points of a NURBS curve | 3.8 | 3.3 | 1.13x faster † |
| `mesh.build_grid_200` | build a 200x200 quad mesh (40,401 vertices) vertex by vertex | 8.2 | 5.3 | 1.53x faster † |
| `mesh.compute_normals_200` | ComputeNormals on a copy of the 200x200 mesh | 7.7 | 4.1 | 1.89x faster † |
| `mesh.vertices_index_loop_40k` | Vertices[i].X for all 40,401 vertices | 0.88 | 0.58 | 1.51x faster † |
| `mesh.vertices_to_point3d_array_40k` | Vertices.ToPoint3dArray, 40,401 vertices | 0.91 | 0.66 | 1.38x faster † |
| `mesh.faces_index_loop_40k` | Faces[i] first index for all 40,000 faces | 0.56 | 0.42 | 1.33x faster † |
| `brep.create_from_sphere_1k` | 1,000 Brep.CreateFromSphere | 9.7 | 4.9 | 1.99x faster † |
| `brep.walk_faces_edges` | every brep of the model: faces (UnderlyingSurface degree), edges (PointAtStart), vertex count | 0.51 | 0.34 | 1.52x faster † |
| `subd.walk_components` | every SubD in tests/models/subd.3dm: faces, edges, vertices | 0.11 | 0.10 | 1.09x faster † |
| `subd.subdivide_1` | Subdivide(1) on a copy of the first SubD | 0.62 | 0.32 | 1.96x faster † |
| `json.encode_1k_curves` | Encode 1,000 curves (20 cps) | 4.0 | 2.4 | 1.62x faster † |
| `json.decode_1k_curves` | CommonObject.Decode of those 1,000 | 13.1 | 11.4 | 1.16x faster |
| `attrs.user_strings_10k` | ObjectAttributes: set then get 10,000 user strings | 3,700 | 1,516 | 2.44x faster |
| `draco.compress_grid_200` | Draco compress the 200x200 mesh, to base64 | - | 14.4 | - |
| `draco.decompress_grid_200` | Draco decompress that base64 string | - | 7.8 | - |
