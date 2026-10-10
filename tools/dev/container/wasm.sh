#!/bin/bash
# WASM (embind) build + tests, inside emscripten/emsdk:5.0.7. Mounts: /src = repo, /work = build dir.
#   wasm.sh build   full build (WASM64)
#   wasm.sh test    smoke + parity (+ the points micro-benchmark)
#   wasm.sh all     build, test
# Node 24 is fetched: the image's node is too old for WASM64
# (memory: rhino3dm-wasm-build-gotchas).
set -u
B=/work/wasm
mkdir -p $B
export EMSCRIPTEN=$(dirname "$(which emcc)")

build() {
  cd $B
  emcmake cmake /src/src -D WASM64=TRUE > /work/wasm_cfg.log 2>&1 \
    || { echo "CONFIGURE FAIL"; tail -8 /work/wasm_cfg.log; exit 1; }
  make -j"$(nproc)" > /work/wasm_make.log 2>&1
  ec=$?; echo "JS MAKE EXIT = $ec"
  [ $ec -ne 0 ] && { grep -E "error" /work/wasm_make.log | sort -u | head -14; exit 1; }
  return 0
}

node24() {
  A=$(uname -m); [ "$A" = aarch64 ] && N=arm64 || N=x64
  NODE=/opt/node-v24.1.0-linux-$N/bin/node
  [ -x $NODE ] || { curl -fsSL https://nodejs.org/dist/v24.1.0/node-v24.1.0-linux-$N.tar.xz -o /tmp/node.tar.xz \
                    && tar -xJf /tmp/node.tar.xz -C /opt; }
}

test_() {
  node24
  $NODE /src/tools/dev/wasm_smoke.js $B/rhino3dm.js 2>&1 | grep -v "^    at " | tail -6
  $NODE /src/tests/parity/parity_js.js $B/rhino3dm.js 2>&1 | tail -20
  $NODE /src/tools/dev/js_points_bench.js $B/rhino3dm.js 2>&1 | tail -5
}

case "${1:-all}" in
  build) build ;;
  test)  test_ ;;
  all)   build && test_ ;;
  *) echo "usage: wasm.sh build|test|all"; exit 2 ;;
esac
