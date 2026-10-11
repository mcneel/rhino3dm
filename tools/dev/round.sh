#!/bin/bash
# Build and validate the vNext generated bindings in Docker (Linux), from the host.
#
#   tools/dev/round.sh check    fast: compile only src/generated/*.cpp (Python toolchain)
#   tools/dev/round.sh build    Python + WASM builds and their tests, in parallel
#   tools/dev/round.sh oracle   regenerate the .NET parity goldens (tests/parity/oracle/run_linux.sh)
#   tools/dev/round.sh test     tests only, against the last builds (no rebuild)
#   tools/dev/round.sh all      oracle, then build + tests, then the coverage reports
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

run() {   # run <image> <script> <args...>
  local image=$1; shift
  docker run --rm -v "$REPO":/src -v "$WORK":/work -v "$VOLUME":/build "$image" bash "$@"
}

py()   { run $PY_IMAGE /src/tools/dev/container/python.sh "$@"; }
wasm() { run $WASM_IMAGE /src/tools/dev/container/wasm.sh "$@"; }

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

case "${1:-}" in
  check)  py check ;;
  build)  both all all ;;
  oracle) oracle ;;
  test)   both test test ;;
  all)    echo "== oracle"; oracle; both all all ;;
  *) sed -n '2,17p' "$0"; exit 2 ;;
esac
