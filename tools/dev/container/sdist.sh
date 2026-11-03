#!/bin/bash
# The Python sdist round-trip, inside python:3.12-bookworm (as CI's test_install): package the
# working tree with setup.py sdist (MANIFEST.in decides what ships), unpack it elsewhere, and
# configure + build the module from the unpacked copy only. Catches files the build needs that
# the sdist leaves out (pilot #82). Mounts: /src = repo (read-only), /work = logs.
#   sdist.sh            configure + build
#   sdist.sh configure  configure only (fast: a missing CMake input)
set -u
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >/dev/null 2>&1
apt-get install -yqq cmake build-essential >/dev/null 2>&1
pip install -q setuptools >/dev/null 2>&1
mkdir -p /work /tmp/repo /tmp/dist /tmp/x
# the tree as setup.py sees it, without build outputs (setup.py writes egg-info next to itself)
tar -C /src --exclude=./build --exclude=./dist --exclude=./.venv --exclude='*/node_modules' --exclude=./.git \
    -cf - . | tar -C /tmp/repo -xf -
cd /tmp/repo && python3 setup.py -q sdist -d /tmp/dist > /work/sdist.log 2>&1 \
  || { echo "SDIST FAIL"; tail -8 /work/sdist.log; exit 1; }
tar -xzf /tmp/dist/*.tar.gz -C /tmp/x && cd /tmp/x/* && mkdir b && cd b
cmake -DCMAKE_BUILD_TYPE=Release ../src > /work/sdist_cfg.log 2>&1
ec=$?; echo "SDIST CONFIGURE EXIT = $ec"
[ $ec -ne 0 ] && { grep -A4 "CMake Error" /work/sdist_cfg.log | head -12; exit 1; }
[ "${1:-}" = configure ] && exit 0
make -j"$(nproc)" > /work/sdist_make.log 2>&1
ec=$?; echo "SDIST BUILD EXIT = $ec"
[ $ec -ne 0 ] && grep -E "error|No such file" /work/sdist_make.log | sort -u | head -12
exit $ec
