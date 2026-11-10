#!/bin/bash
# WASM (embind) build + tests, inside emscripten/emsdk:5.0.7. Mounts: /src = repo,
# /work = logs + built module (host-visible), /build = build tree + node cache
# (a named volume: container-local disk, see tools/dev/round.sh).
#   wasm.sh build [32|64]   full build: wasm32 (default, the package main) or WASM64 (wasm64/)
#   wasm.sh test  [32|64]   parity + tests/javascript; wasm32 on Node 22 and 24, WASM64 on 24
#   wasm.sh all   [32|64]   build, test
# Node 24 is fetched: the image's node (22) is too old for WASM64
# (memory: rhino3dm-wasm-build-gotchas).
set -u
FLAVOR=${2:-32}
case "$FLAVOR" in
  32) B=/build/wasm32; OUT=/work/wasm;   W64=FALSE ;;
  64) B=/build/wasm;   OUT=/work/wasm64; W64=TRUE ;;   # /build/wasm: the tree from before 9.2's wasm32 default
  *) echo "usage: wasm.sh build|test|all [32|64]"; exit 2 ;;
esac
mkdir -p $B $OUT
export EMSCRIPTEN=$(dirname "$(which emcc)")

build() {
  cd $B
  emcmake cmake /src/src -D WASM64=$W64 > /work/wasm${FLAVOR}_cfg.log 2>&1 \
    || { echo "CONFIGURE FAIL"; tail -8 /work/wasm${FLAVOR}_cfg.log; exit 1; }
  make -j"$(nproc)" > /work/wasm${FLAVOR}_make.log 2>&1
  ec=$?; echo "JS (wasm$FLAVOR) MAKE EXIT = $ec"
  [ $ec -ne 0 ] && { grep -E "error" /work/wasm${FLAVOR}_make.log | sort -u | head -14; exit 1; }
  # the memory section's flags byte: bit 0x04 is memory64. Checked so a stale cache cannot pass
  # one flavor off as the other.
  python3 - $B/rhino3dm.wasm $FLAVOR <<'PY' || exit 1
import sys
b = open(sys.argv[1], 'rb').read()
def leb(i):
    r = s = 0
    while True:
        x = b[i]; i += 1; r |= (x & 0x7f) << s; s += 7
        if x < 0x80: return r, i
i = 8
while i < len(b):
    sid = b[i]; i += 1; size, i = leb(i)
    if sid == 5:
        got = '64' if b[i + 1] & 0x04 else '32'
        print('memory: wasm' + got)
        sys.exit(0 if got == sys.argv[2] else 1)
    i += size
print('memory: no memory section'); sys.exit(1)
PY
  cp -f $B/rhino3dm.js $B/rhino3dm.wasm $OUT/ 2>/dev/null || true
  return 0
}

node24() {
  A=$(uname -m); [ "$A" = aarch64 ] && N=arm64 || N=x64
  # cached in the volume: the image's node is too old, and ~40 MB per run adds up
  NODE=/build/node/node-v24.1.0-linux-$N/bin/node
  [ -x $NODE ] || { mkdir -p /build/node && curl -fsSL https://nodejs.org/dist/v24.1.0/node-v24.1.0-linux-$N.tar.xz -o /tmp/node.tar.xz \
                    && tar -xJf /tmp/node.tar.xz -C /build/node; }
}

test_() {
  node24
  local nodes=$NODE
  [ $FLAVOR = 32 ] && nodes="$(ls -d /emsdk/node/*/bin | head -1)/node $NODE"
  for NODE in $nodes; do
  echo "-- wasm$FLAVOR, node $($NODE --version)"
  $NODE /src/tests/parity/parity_js.js $B/rhino3dm.js 2>&1 | tail -20
  # tests/javascript as CI runs it (jest), on a copy whose lib is this build: tests/javascript/lib
  # (the module the suite was last run against by hand) is never touched. jest and three come
  # from tests/javascript/node_modules (plain JS)
  J=/build/jsrun; rm -rf $J; mkdir -p $J/lib $J/node_modules
  ( cd /src/tests/javascript && tar --exclude=./lib --exclude=./node_modules -cf - . ) | tar -xf - -C $J
  cp -f $B/rhino3dm.js $B/rhino3dm.wasm $J/lib/
  echo '{"name": "rhino3dm", "main": "rhino3dm.js"}' > $J/lib/package.json
  for p in /src/tests/javascript/node_modules/* /src/tests/javascript/node_modules/.bin; do
    [ "$(basename $p)" = rhino3dm ] || ln -s $p $J/node_modules/
  done
  ln -s ../lib $J/node_modules/rhino3dm
  ln -sfn /src/tests/models /build/models          # the tests read ../models
  ( cd $J && $NODE --expose-gc node_modules/jest/bin/jest.js --ci 2>&1 | grep -E "✕|●.*›|^Tests:|^Test Suites:" | tail -40 )
  done
}

case "${1:-all}" in
  build) build ;;
  test)  test_ ;;
  all)   build && test_ ;;
  *) echo "usage: wasm.sh build|test|all [32|64]"; exit 2 ;;
esac
