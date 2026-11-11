#!/bin/bash
# Performance tasks (tests/perf) against the reference release and this tree's last dev builds.
# Mounts: /src = repo, /work = logs + built modules (host-visible), /build = the named volume.
#   perf.sh model    [ubuntu:24.04]  write the shared model with the reference rhino3dm (cached)
#   perf.sh python   [ubuntu:24.04]  bench.py: reference wheel vs /work/python/_rhino3dm*.so
#   perf.sh dotnet   [dotnet sdk]    dotnet/: reference NuGet vs src/dotnet + /build/oracle/native
#   perf.sh js       [node:24]       bench.mjs: reference npm package vs /work/wasm/rhino3dm.js
# Results: /work/perf/results/<lib>_{ref,cur}_r<n>.json; tests/perf/compare.py gates them.
# Env: PERF_REF (8.35.0), PERF_SCALE (small|full), PERF_REPEATS (5), PERF_ROUNDS (3).
# The rounds alternate reference and current, so a slow spell on the machine hits both.
set -eu
export DEBIAN_FRONTEND=noninteractive
REF=${PERF_REF:-8.35.0}; SCALE=${PERF_SCALE:-small}; REPEATS=${PERF_REPEATS:-5}; ROUNDS=${PERF_ROUNDS:-3}
P=/build/perf; OUT=/work/perf/results; MODEL=$P/model_${SCALE}_${REF}.3dm
mkdir -p $P $OUT
# container-local copies: Docker Desktop's bind mount makes small reads and writes very slow
rm -rf /tmp/models && cp -r /src/tests/models /tmp/models

py_setup() {
  apt-get update -qq >/dev/null 2>&1
  apt-get install -yqq python3 python3-venv python3-numpy >/dev/null 2>&1
  if [ ! -x $P/venv-ref-$REF/bin/python ]; then
    python3 -m venv --system-site-packages $P/venv-ref-$REF
    $P/venv-ref-$REF/bin/pip install -q rhino3dm==$REF
  fi
}

case "${1:-}" in
  model)
    py_setup
    [ -f $MODEL ] || $P/venv-ref-$REF/bin/python /src/tests/perf/gen_model.py /tmp/model.3dm $SCALE
    [ -f $MODEL ] || cp /tmp/model.3dm $MODEL
    ls -la $MODEL ;;
  python)
    py_setup
    so=$(ls /work/python/_rhino3dm*.so 2>/dev/null | head -1) || true
    [ -n "$so" ] || { echo "no /work/python/_rhino3dm*.so: run round.sh build first"; exit 1; }
    rm -rf /tmp/pkg && mkdir -p /tmp/pkg/rhino3dm && cp /src/src/rhino3dm/__init__.py "$so" /tmp/pkg/rhino3dm/
    cp $MODEL /tmp/model.3dm
    for r in $(seq $ROUNDS); do
      $P/venv-ref-$REF/bin/python /src/tests/perf/bench.py /tmp/models /tmp/model.3dm $REPEATS > $OUT/python_ref_r$r.json 2> $OUT/python_ref_r$r.err
      PYTHONPATH=/tmp/pkg python3 /src/tests/perf/bench.py /tmp/models /tmp/model.3dm $REPEATS > $OUT/python_cur_r$r.json 2> $OUT/python_cur_r$r.err
    done ;;
  dotnet)
    N=/build/oracle/native/librhino3dm_native.so
    [ -f $N ] || { echo "no $N: run round.sh dotnet first"; exit 1; }
    rm -rf /tmp/w && mkdir -p /tmp/w/src /tmp/w/tests/perf
    cp -r /src/src/dotnet /tmp/w/src/ && rm -rf /tmp/w/src/dotnet/obj /tmp/w/src/dotnet/bin
    for s in ref cur; do cp -r /src/tests/perf/dotnet /tmp/w/tests/perf/$s && rm -rf /tmp/w/tests/perf/$s/obj /tmp/w/tests/perf/$s/bin; done
    # the reference project sits beside the current one, so both resolve ../../../src/dotnet the same
    (cd /tmp/w/tests/perf/ref && dotnet build -c Release -p:Rhino3dmVersion=$REF -o out > /work/perf/dotnet_ref_build.log 2>&1) \
      || { tail -20 /work/perf/dotnet_ref_build.log; exit 1; }
    (cd /tmp/w/tests/perf/cur && dotnet build -c Release -o out > /work/perf/dotnet_cur_build.log 2>&1) \
      || { grep -E " error " /work/perf/dotnet_cur_build.log | sort -u | head; exit 1; }
    cp -f $N /tmp/w/tests/perf/cur/out/
    cp $MODEL /tmp/model.3dm
    for r in $(seq $ROUNDS); do
      for s in ref cur; do
        dotnet /tmp/w/tests/perf/$s/out/PerfBench.dll /tmp/models /tmp/model.3dm $REPEATS > $OUT/dotnet_${s}_r$r.json 2> $OUT/dotnet_${s}_r$r.err
      done
    done ;;
  js)
    [ -f /work/wasm/rhino3dm.js ] || { echo "no /work/wasm/rhino3dm.js: run round.sh build first"; exit 1; }
    if [ ! -f $P/npm-ref-$REF/package/rhino3dm.js ]; then
      mkdir -p $P/npm-ref-$REF && (cd $P/npm-ref-$REF && npm pack -q rhino3dm@$REF >/dev/null && tar -xzf rhino3dm-$REF.tgz)
    fi
    rm -rf /tmp/cur && mkdir /tmp/cur && cp /work/wasm/rhino3dm.js /work/wasm/rhino3dm.wasm /tmp/cur/
    cp $MODEL /tmp/model.3dm
    for r in $(seq $ROUNDS); do
      node --expose-gc /src/tests/perf/bench.mjs $P/npm-ref-$REF/package/rhino3dm.js /tmp/models /tmp/model.3dm $REPEATS > $OUT/javascript_ref_r$r.json 2> $OUT/javascript_ref_r$r.err
      node --expose-gc /src/tests/perf/bench.mjs /tmp/cur/rhino3dm.js /tmp/models /tmp/model.3dm $REPEATS > $OUT/javascript_cur_r$r.json 2> $OUT/javascript_cur_r$r.err
    done ;;
  *) echo "usage: perf.sh model|python|dotnet|js"; exit 2 ;;
esac
