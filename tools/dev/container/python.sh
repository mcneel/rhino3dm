#!/bin/bash
# Python (nanobind) build + tests, inside ubuntu:24.04. Mounts: /src = repo,
# /work = logs + built module (host-visible), /build = build trees (a named
# volume: container-local disk, see tools/dev/round.sh).
#   python.sh check   compile only the generated objects (fast: catches generator output errors)
#   python.sh build   full module build
#   python.sh test    regression + parity + planner tests against the built module
#   python.sh all     build, test
#   python.sh stubs   the raw stub of the built module (build does it too) -> build/dev/python/_rhino3dm.raw.pyi
# The system Python is 3.12, so this builds the abi3 module (M2). PY_FT=1: the same against a
# free-threaded CPython (3.14t, installed with uv in the container; its own build tree), tested with
# PYTHON_GIL=0 -- the module must load without re-enabling the GIL.
# -ffp-contract=off: no fused multiply-add, so results are bitwise comparable
# with the .NET oracle (tests/parity/README.md).
set -u
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >/dev/null 2>&1
apt-get install -yqq build-essential cmake python3 python3-dev python3-numpy python3-pytest >/dev/null 2>&1
B=/build/python
PY=python3
PYCFG=""
LOG=/work/python
if [ "${PY_FT:-0}" = 1 ]; then
  apt-get install -yqq curl ca-certificates >/dev/null 2>&1
  export UV_PYTHON_INSTALL_DIR=/build/uv-python UV_CACHE_DIR=/build/uv-cache
  [ -x /build/uv/uv ] || { mkdir -p /build/uv && curl -LsSf https://github.com/astral-sh/uv/releases/download/0.9.5/uv-$(uname -m)-unknown-linux-gnu.tar.gz | tar -xz -C /build/uv --strip-components=1; }
  /build/uv/uv python install 3.14t > /work/python_ft_uv.log 2>&1 || { echo "FT: uv python install failed"; tail -5 /work/python_ft_uv.log; exit 1; }
  [ -d /build/ft-venv ] || /build/uv/uv venv --python "$(/build/uv/uv python find 3.14t)" /build/ft-venv > /dev/null 2>&1
  /build/uv/uv pip install --python /build/ft-venv/bin/python pytest numpy > /work/python_ft_pip.log 2>&1
  PY=/build/ft-venv/bin/python
  PYCFG="-DPython_EXECUTABLE=$PY"
  B=/build/python_ft
  LOG=/work/python_ft
  echo "FT: $($PY -c 'import sys, sysconfig; print(sys.version.split()[0], "free-threaded" if sysconfig.get_config_var("Py_GIL_DISABLED") else "GIL")')"
fi
mkdir -p $B $LOG

publish() {   # the module where host-side tools expect it (build/dev/python, build/dev/python_ft)
  cp -f $B/_rhino3dm*.so $LOG/ 2>/dev/null || true
}

configure() {
  # IPO off: LTO moves all the optimization into the LINK -- 16 minutes to
  # re-optimize opennurbs after a one-file change (8.x's pybind11_add_module
  # turned it on). A dev round wants fast incremental builds.
  cd $B && cmake /src/src -DRHINO3DM_PY=TRUE $PYCFG \
    -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=OFF \
    -DCMAKE_CXX_FLAGS=-ffp-contract=off -DCMAKE_C_FLAGS=-ffp-contract=off > $LOG/cfg.log 2>&1 \
    || { echo "PY CONFIGURE FAIL"; tail -12 $LOG/cfg.log; exit 1; }
}

check() {
  configure
  targets=$(cd /src/src/bindings && ls *.cpp | sed 's#^#bindings/#; s#\.cpp$#.o#')
  make -k -j"$(nproc)" $targets > $LOG/check.log 2>&1
  ec=$?; echo "BINDINGS OBJECTS EXIT = $ec"
  grep -E "error:" $LOG/check.log | sed -E 's#^/src/src/bindings/##' | sort -u | head -20
  return $ec
}

build() {
  configure
  # one module per tree: an older file beside the new one wins the import (8.x's version-specific
  # `cpython-312` module shadows `abi3`), so the tests would run against it
  rm -f $B/_rhino3dm*.so $LOG/_rhino3dm*.so
  make -k -j"$(nproc)" > $LOG/make.log 2>&1   # -k: every error of a round, not the first
  ec=$?; echo "PY MAKE EXIT = $ec"
  [ $ec -ne 0 ] && { grep -E "error" $LOG/make.log | sed -E 's#^/src/src/##' | sort -u | head -20; exit 1; }
  publish
  stubs
  return 0
}

stubs() {   # nanobind's stub of the built module: tools/docs/pyi.py's input (M5)
  ( cd $B && $PY /src/src/lib/nanobind/src/stubgen.py -q -m _rhino3dm -o $LOG/_rhino3dm.raw.pyi ) \
    || echo "STUBGEN FAIL"
}

test_() {
  cd /src
  if [ "${PY_FT:-0}" = 1 ]; then
    # loading must not turn the GIL back on (an extension not declared free-threading safe does,
    # with a RuntimeWarning)
    PYTHON_GIL=0 $PY -W error::RuntimeWarning -c "import sys; sys.path.insert(0, '$B'); import _rhino3dm; print('FT: GIL enabled after import:', sys._is_gil_enabled())"
    export PYTHON_GIL=0
  fi
  RH3DM_MODULE_DIR=$B $PY -m pytest -q -p no:cacheprovider tests/parity tests/generator 2>&1 | tail -4
  $PY tests/parity/test_parity.py $B 2>&1 | tail -14
  # tests/python as CI runs it (unittest), against the built module as the `rhino3dm` package
  mkdir -p $B/pkg/rhino3dm && cp -f /src/src/rhino3dm/__init__.py $B/_rhino3dm*.so $B/pkg/rhino3dm/
  ( cd /src/tests/python && PYTHONPATH=$B/pkg $PY -m unittest discover . 2>&1 | grep -E "^(FAIL|ERROR):|^Ran |^OK|^FAILED" | tail -30 )
}

case "${1:-all}" in
  check)  check ;;
  build)  build ;;
  stubs)  stubs ;;
  test)   test_ ;;
  all)    build && test_ ;;
  *) echo "usage: [PY_FT=1] python.sh check|build|test|all|stubs"; exit 2 ;;
esac
