# Performance regression tests

The same tasks run on the **reference release** (rhino3dm 8.35.0 from PyPI, NuGet and npm) and on
**this tree's build**, on the same machine, in the same run. Each task's result is the ratio
`current / reference` (below 1: this tree is faster). A task fails when its ratio grows past the
stored baseline ratio times a tolerance.

Comparing against the reference in the same run cancels most of the machine's own speed, so the
check works on noisy machines. It still depends on the environment, since different CPUs and runtimes
don't slow down the same tasks equally. That's why each environment has its own baseline in
`baseline/`.

## Files

| File | |
|---|---|
| `gen_model.py` | writes the mixed model the read/write tasks use, with the reference release, as a version 8 archive |
| `bench.py` | Python tasks |
| `bench.mjs` | JavaScript tasks (`node --expose-gc`) |
| `dotnet/` | .NET tasks (`PerfBench.csproj`: `-p:Rhino3dmVersion=8.35.0` builds against NuGet, otherwise against `src/dotnet`) |
| `compare.py` | gates a results directory on a baseline; `--update` writes the baseline |
| `baseline/<env>.json` | per-task ratios accepted for that environment |
| `record.py` | writes a run's record to `history/`: one page per run, tables per library |
| `tasks.py` | what each task does and with which data (the records' Parameters column) |
| `history/` | the recorded runs; `history/README.md` is the index |

## Running

Locally (Linux arm64 in Docker, after `tools/dev/round.sh build` and `tools/dev/round.sh dotnet`):

```
tools/dev/round.sh perf            # gate on baseline/linux-arm64-docker.json
tools/dev/round.sh perf update     # accept the current ratios as the baseline
PERF_SCALE=full PERF_ROUNDS=1 tools/dev/round.sh perf   # the ~145 MB model, one round
```

Results go to `build/dev/perf/results/`, and the report to `build/dev/perf/report.md`. This uses the dev
builds (`-ffp-contract=off`, no LTO). The baseline was taken with the same builds, so the ratios compare
like with like.

In CI: run the **workflow perf** workflow by hand (Actions tab). It builds Release versions of all
three libraries, gates on `baseline/ci-ubuntu-x64.json` with a 1.75x tolerance, and uploads
`perf-baseline` (this run's ratios as a baseline file). While there is no baseline for an environment,
the check only reports.

## Recording a run

The gate keeps only ratios. To keep a run on record, with its timings, add it to `history/`:

```
tools/dev/round.sh perf record                       # run, gate, record
PERF_NOTES="after the zlib change" tools/dev/round.sh perf record
python3 tests/perf/record.py build/dev/perf/results  # record the last run afterwards
```

Each record has the date, commit, branch, machine, builds, model scale and method, then one table per
library: task, parameters, 8.35 time, current time, and the factor (`3.0x faster`, `1.4x slower`, or
`same` within 5%). Tasks under 10 ms on either side are marked as noisy. The JSON beside each page
holds the same numbers. `round.sh perf` writes the run's details to `results/run.json`. Options on
`record.py` override them, which is how the runs made before it existed were recorded.

The CI workflow uploads its record as the `perf-record` artifact. To keep it, copy its `.md` and
`.json` into `history/`, then run `python3 tests/perf/record.py --reindex`.

## Method

- Each task runs once to warm up, then `repeats` times (default 5; 2 when the warm-up took over a
  second). The median is reported. There are 3 rounds by default: the reference and current runs
  alternate, and each task keeps its lowest median.
- Local tolerance is 1.5x and CI's is 1.75x. Two one-round runs of an unchanged tree moved some ratios
  by up to 1.5x, mostly small JS tasks under the garbage collector, which is why there are three rounds
  and a 10 ms floor.
- All writes target archive version 8, so both versions do the same work. One task writes with the
  default version for comparison.
- Synthetic geometry comes from one 32-bit LCG (`s = s * 1664525 + 1013904223 mod 2^32`), identical in
  all three languages. Random-looking vertices keep zlib realistic. A perfectly regular grid is zlib's
  worst case and is measured on its own (`create.write_50_regular_grid_meshes_v8`).
- `curve.point_at_100k` evaluates one small curve at sorted parameters, the best case for the CPU's
  caches. `curve.point_at_100k_shuffled_1k_curves` is the realistic case.
- Tasks faster than `--floor-ms` (10 ms) on either side are reported but not gated, because timer and
  GC noise dominate them. This includes the .NET `values.*` tasks, which run managed code in both versions.
- Tasks only one side has (Draco in .NET, the numpy view, `toFloatArray`) are reported without a ratio.

## Adding a task

Add it to all three benches under the same name, with the same data. Then update the baselines: run
`round.sh perf update` locally, and run the CI workflow and commit its `perf-baseline` artifact.
