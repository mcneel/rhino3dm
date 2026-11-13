# Perf run history

One page per recorded run of tests/perf (`python tests/perf/record.py`, or `round.sh perf record`).
Columns per library: the geometric mean over its tasks of reference time / current time (above 1:
the current build is faster). Runs differ in environment, builds and model scale, so compare
rows of the same environment.

| Date | Commit | Env | Versions | Scale | Python | .NET | JavaScript (wasm32) | JavaScript (wasm64 / memory64) | Notes |
|---|---|---|---|---|--:|--:|--:|--:|---|
| [2026-11-13 12:35](2026-11-13_351d7e0c_linux-arm64-docker.md) | `351d7e0c` | linux-arm64-docker | 9.2.0-beta.0 vs 8.35.0 | small | 5.61 | 1.73 | 1.27 |  | wasm -O3 for zlib/Draco/unicode, G154 table iterators, G155 view cache, G156 overload dispatch and strings (this commit); the only uncommitted change in src/ is the opennurbs submodule's working tree |
| [2026-11-11 17:23](2026-11-11_dd8908dd_linux-arm64-docker.md) | `dd8908dd` | linux-arm64-docker | 9.2.0-beta.0 vs 8.35.0 | small | 5.07 | 1.63 | 0.86 |  | RH-100236 (opennurbs mesh-modifier user data on every object, temporarily patched into the opennurbs submodule): model writes no longer attach five default mesh-modifier records per object |
| [2026-11-11 12:30](2026-11-11_d42a6507_linux-arm64-docker_check.md) | `d42a6507` | linux-arm64-docker | 9.2.0-beta.0 vs 8.35.0 | small | 4.64 | 1.64 | 0.80 |  | Gate check of the unchanged tree right after the baseline: PASSED (56 gated tasks, ratio to baseline 0.74-1.41) |
| [2026-11-11 12:02](2026-11-11_d42a6507_linux-arm64-docker_baseline.md) | `d42a6507` | linux-arm64-docker | 9.2.0-beta.0 vs 8.35.0 | small | 5.10 | 1.55 | 0.81 |  | Baseline run: tests/perf/baseline/linux-arm64-docker.json holds its ratios |
| [2026-11-10 23:55](2026-11-10_c594c917_m1max-host-node.md) | `c594c917` | m1max-host-node | 9.2.0-beta.0 vs 8.35.0 | full |  |  | 0.81 |  | JS after the overload-dispatch fix (RH3DM-217), one round |
| [2026-11-10 22:15](2026-11-10_fbde8a4b_m1max-mixed.md) | `fbde8a4b` | m1max-mixed | 9.2.0-beta.0 vs 8.35.0 | full | 4.66 | 1.58 | 0.78 | 0.80 | First comparison, made with an earlier version of these benches; JS before the overload-dispatch fix (RH3DM-217) |
