#!/bin/bash
# Python (pybind11) build + tests, inside ubuntu:24.04. Mounts: /src = repo,
# /work = logs + built module (host-visible), /build = build trees (a named
# volume: container-local disk, see tools/dev/round.sh).
#   python.sh check   compile only the generated objects (fast: catches generator output errors)
#   python.sh build   full module build
#   python.sh test    regression + parity + planner tests against the built module
#   python.sh report  regenerate docs/vnext/coverage.md, old-coverage.md, rhino3dm-only-members.md
#   python.sh all     build, test, report
# -ffp-contract=off: no fused multiply-add, so results are bitwise comparable
# with the .NET oracle (tests/parity/README.md).
set -u
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >/dev/null 2>&1
apt-get install -yqq build-essential cmake python3 python3-dev python3-numpy python3-pytest >/dev/null 2>&1
B=/build/python
mkdir -p $B /work/python

publish() {   # the module where host-side tools expect it (build/dev/python)
  cp -f $B/_rhino3dm*.so /work/python/ 2>/dev/null || true
}

configure() {
  # IPO off: pybind11_add_module enables -flto by default, which moves all the
  # optimization into the LINK -- 16 minutes to re-optimize opennurbs after a
  # one-file change. A dev round wants fast incremental builds; the release
  # wheels (setup.py) keep LTO.
  cd $B && cmake /src/src -DRHINO3DM_PY=TRUE -DPYBIND11_FINDPYTHON=ON \
    -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=OFF \
    -DCMAKE_CXX_FLAGS=-ffp-contract=off -DCMAKE_C_FLAGS=-ffp-contract=off > /work/python_cfg.log 2>&1 \
    || { echo "PY CONFIGURE FAIL"; tail -8 /work/python_cfg.log; exit 1; }
}

check() {
  configure
  targets=$(cd /src/src/generated && ls *.cpp | sed 's#^#generated/#; s#\.cpp$#.o#')
  make -k -j"$(nproc)" $targets > /work/python_check.log 2>&1
  ec=$?; echo "GENERATED OBJECTS EXIT = $ec"
  grep -E "error:" /work/python_check.log | sed -E 's#^/src/src/generated/##' | sort -u | head -20
  return $ec
}

build() {
  configure
  make -j"$(nproc)" > /work/python_make.log 2>&1
  ec=$?; echo "PY MAKE EXIT = $ec"
  [ $ec -ne 0 ] && { grep -E "error" /work/python_make.log | sort -u | head -14; exit 1; }
  publish
  return 0
}

test_() {
  cd /src
  RH3DM_MODULE_DIR=$B python3 -m pytest -q -p no:cacheprovider tests/generated tests/parity tests/generator 2>&1 | tail -4
  python3 tests/parity/test_parity.py $B 2>&1 | tail -14
}

report() {
  cd /src
  python3 tools/generate/coverage.py $B api/manifest.json docs/vnext/coverage.md > /dev/null && echo "docs/vnext/coverage.md updated"
  python3 tools/generate/old_coverage.py $B api/manifest.json docs/vnext/old-coverage.md > /dev/null \
    && echo "docs/vnext/old-coverage.md + rhino3dm-only-members.md updated"
}

case "${1:-all}" in
  check)  check ;;
  build)  build ;;
  test)   test_ ;;
  report) report ;;
  all)    build && test_ && report ;;
  *) echo "usage: python.sh check|build|test|report|all"; exit 2 ;;
esac
