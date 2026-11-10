#!/bin/bash
# tests/dotnet against this tree: Rhino3dm from src/dotnet (the test project's ProjectReference) and a
# librhino3dm_native built here with gcc, in the .NET 10 SDK image. The native build tree is the one the
# parity oracle uses (/build/oracle/native, incremental), so after `round.sh oracle` it is up to date.
# Sources are copied to container-local disk so Linux build caches never land in the host tree.
set -e
export DEBIAN_FRONTEND=noninteractive
N=/build/oracle/native
if ! command -v cmake >/dev/null; then
  apt-get update -qq >/dev/null 2>&1; apt-get install -yqq cmake g++ make >/dev/null 2>&1
fi
mkdir -p $N && cd $N
[ -f Makefile ] || cmake /src/src/librhino3dm_native -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_CXX_FLAGS=-ffp-contract=off -DCMAKE_C_FLAGS=-ffp-contract=off > /work/dotnet_native_cfg.log 2>&1
make -j$(nproc) > /work/dotnet_native_make.log 2>&1 || { echo "NATIVE BUILD FAILED"; grep -E "error" /work/dotnet_native_make.log | head; exit 1; }
rm -rf /tmp/w && mkdir -p /tmp/w/src /tmp/w/tests
cp -r /src/src/dotnet /tmp/w/src/ && rm -rf /tmp/w/src/dotnet/obj /tmp/w/src/dotnet/bin
cp -r /src/tests/dotnet /tmp/w/tests/ && rm -rf /tmp/w/tests/dotnet/obj /tmp/w/tests/dotnet/bin /tmp/w/tests/dotnet/lib
cp -r /src/tests/models /tmp/w/tests/
cd /tmp/w/tests/dotnet
dotnet build -c Release > /work/dotnet_build.log 2>&1 || { echo "DOTNET BUILD FAILED"; grep -E " error " /work/dotnet_build.log | sort -u | head -20; exit 1; }
cp $N/librhino3dm_native.so bin/Release/net10.0/
dotnet test -c Release --no-build --logger "console;verbosity=normal" > /work/dotnet_test.log 2>&1 && rc=0 || rc=$?
grep -E "^\s*Failed [A-Za-z_]" /work/dotnet_test.log | head -40
count() { v=$(grep -E "^\s*$1:" /work/dotnet_test.log | awk '{print $2}'); echo ${v:-0}; }
echo "DOTNET TESTS: $(count Passed) passed, $(count Failed) failed, $(count Skipped) skipped (exit $rc)"
