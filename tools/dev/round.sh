#!/bin/bash
# Build and validate the vNext generated bindings in Docker (Linux), from the host.
#
#   tools/dev/round.sh check    fast: compile only src/bindings/*.cpp (Python toolchain)
#   tools/dev/round.sh build    Python + WASM (wasm32, the package main) builds and their tests, in parallel
#   tools/dev/round.sh wasm64 [build|test|all]   the WASM64 (memory64) build the package ships in wasm64/
#   tools/dev/round.sh oracle   regenerate the .NET parity goldens (tests/parity/oracle/run_linux.sh)
#   tools/dev/round.sh test     tests only, against the last builds (no rebuild)
#   tools/dev/round.sh dotnet   tests/dotnet against src/dotnet + a fresh native library (.NET 10 SDK)
#   tools/dev/round.sh all      oracle, then build + tests, then wasm64, then dotnet
#   tools/dev/round.sh ft [check|build|test|all]   the Python build against free-threaded CPython 3.14t
#                               (the import must turn the GIL back on)
#   tools/dev/round.sh sdist    the Python sdist round-trip: package, unpack, build from the package
#   tools/dev/round.sh typings  src/rhino3dm/__init__.pyi + src/js/rhino3dm.d.ts from the last Python
#                               build's stub (tools/docs); tsc --strict on the .d.ts, mypy --strict +
#                               stubtest (missing names) on the .pyi
#   tools/dev/round.sh drift    the typings regenerated into build/dev/typings and compared with the
#                               committed ones; fails when they differ (also the last step of build/all)
#
# Regenerate first (python3 tools/generate/bnd_gen.py); the generated sources are
# read from this checkout. Logs and the built modules land in $RH3DM_DEV_WORK
# (default: <repo>/build/dev, gitignored). The BUILD TREES live in the Docker
# named volume $RH3DM_DEV_VOLUME (default rhino3dm-dev-build), so rounds are
# incremental AND fast: on Docker Desktop's bind mount (gRPC-FUSE) the linker
# writing the 34 MB module took 12.5 minutes; on the container's own disk, 9 s.
# `docker volume rm rhino3dm-dev-build` starts from scratch.
#
# Docker: set DOCKER_CONTEXT if your default context is not running (e.g.
# DOCKER_CONTEXT=desktop-linux). Images are pinned below.
set -u
REPO=$(cd "$(dirname "$0")/../.." && pwd)
WORK=${RH3DM_DEV_WORK:-$REPO/build/dev}
VOLUME=${RH3DM_DEV_VOLUME:-rhino3dm-dev-build}
mkdir -p "$WORK"
PY_IMAGE=ubuntu:24.04
WASM_IMAGE=emscripten/emsdk:5.0.7
DOTNET_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
NODE_IMAGE=node:24

run() {   # run <image> <script> <args...>
  local image=$1; shift
  docker run --rm -v "$REPO":/src -v "$WORK":/work -v "$VOLUME":/build "$image" bash "$@"
}

py()   { run $PY_IMAGE /src/tools/dev/container/python.sh "$@"; }
wasm() { run $WASM_IMAGE /src/tools/dev/container/wasm.sh "$@"; }
dotnet_tests() { echo "== dotnet"; run $DOTNET_IMAGE /src/tools/dev/container/dotnet.sh; }

oracle() {
  mkdir -p "$WORK/oracle"
  docker run --rm -v "$REPO":/src -v "$WORK/oracle":/out -v "$VOLUME":/build \
    -e NATIVE_BUILD_DIR=/build/oracle/native $DOTNET_IMAGE \
    bash /src/tests/parity/oracle/run_linux.sh 2>&1 | tail -8
}

both() {  # run python and wasm in parallel; print each when both are done
  py "$1" > "$WORK/round_python.log" 2>&1 &
  wasm "$2" > "$WORK/round_wasm.log" 2>&1 &
  wait
  echo "== python"; cat "$WORK/round_python.log"
  echo "== wasm";   grep -vE "^\| " "$WORK/round_wasm.log"
}

drift() {  # the committed typings are what the last Python build generates
  echo "== typings drift"
  [ -f "$WORK/python/_rhino3dm.raw.pyi" ] || { echo "DRIFT: no $WORK/python/_rhino3dm.raw.pyi (build Python first)"; return 1; }
  local d=$WORK/typings; mkdir -p "$d"
  python3 "$REPO/tools/docs/pyi.py" "$WORK/python/_rhino3dm.raw.pyi" "$d/__init__.pyi" >/dev/null &&
    python3 "$REPO/tools/docs/dts.py" "$d/__init__.pyi" "$d/rhino3dm.d.ts" >/dev/null || { echo "DRIFT: generation failed"; return 1; }
  local bad=0 f
  for f in src/rhino3dm/__init__.pyi:__init__.pyi src/js/rhino3dm.d.ts:rhino3dm.d.ts; do
    diff -q "$REPO/${f%%:*}" "$d/${f##*:}" >/dev/null ||
      { echo "DRIFT: ${f%%:*} differs from the build's (diff it with $d/${f##*:}; tools/dev/round.sh typings regenerates)"; bad=1; }
  done
  [ $bad = 0 ] && echo "TYPINGS OK: committed typings match the build"
  return $bad
}

case "${1:-}" in
  check)  py check ;;
  build)  both all all; drift ;;
  oracle) oracle ;;
  test)   both test test ;;
  dotnet) dotnet_tests ;;
  ft)     docker run --rm -v "$REPO":/src -v "$WORK":/work -v "$VOLUME":/build -e PY_FT=1 $PY_IMAGE \
            bash /src/tools/dev/container/python.sh "${2:-all}" ;;
  all)    echo "== oracle"; oracle; both all all; drift; echo "== wasm64"; wasm all 64 | grep -vE "^\| "; dotnet_tests ;;
  wasm64) wasm "${2:-all}" 64 | grep -vE "^\| " ;;
  drift)  drift ;;
  typings) [ -f "$WORK/python/_rhino3dm.raw.pyi" ] || { echo "no $WORK/python/_rhino3dm.raw.pyi: build Python first"; exit 1; }
          python3 "$REPO/tools/docs/pyi.py" "$WORK/python/_rhino3dm.raw.pyi" && python3 "$REPO/tools/docs/dts.py" || exit 1
          mkdir -p "$WORK/tsc" && cp "$REPO/src/js/rhino3dm.d.ts" "$WORK/tsc/"
          docker run --rm -v "$WORK/tsc":/w -w /w $NODE_IMAGE bash -c \
            'npm i -g typescript@5.6.3 >/dev/null 2>&1; tsc --noEmit --strict --target es2020 rhino3dm.d.ts && echo "TSC OK"'
          docker run --rm -v "$REPO":/src:ro -v "$WORK":/work $PY_IMAGE bash /src/tools/docs/check_pyi.sh ;;
  sdist)  docker run --rm -v "$REPO":/src:ro -v "$WORK":/work python:3.12-bookworm bash /src/tools/dev/container/sdist.sh "${2:-}" ;;
  *) sed -n '2,24p' "$0"; exit 2 ;;
esac
