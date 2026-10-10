#!/bin/bash
# Canonical golden generation: a FRESHLY built librhino3dm_native (gcc) next to
# the .NET oracle, in the official .NET 10 SDK image. A stale native library
# silently changes goldens (an 8.x-era dylib throws EntryPointNotFoundException
# for Curve.SpanVector), so never generate goldens against a prebuilt one.
#
#   docker run --rm -v <repo>:/src -v <scratch>:/out mcr.microsoft.com/dotnet/sdk:10.0 \
#       bash /src/tests/parity/oracle/run_linux.sh

set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >/dev/null 2>&1; apt-get install -yqq cmake g++ make >/dev/null 2>&1
# native C layer, gcc -- same compiler family as the bindings under test
mkdir -p /out/native && cd /out/native
# -ffp-contract=off: no fused multiply-add, so results are IEEE-exact and
# reproducible across compilers, optimization levels and CPUs -- the
# bindings under test are built the same way (exact comparison needs it)
[ -f Makefile ] || cmake /src/src/librhino3dm_native -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_CXX_FLAGS=-ffp-contract=off -DCMAKE_C_FLAGS=-ffp-contract=off > /out/native_cfg.log 2>&1
make -j$(nproc) > /out/native_make.log 2>&1 || { echo "NATIVE BUILD FAILED"; grep -E "error" /out/native_make.log | head; exit 1; }
ls -la librhino3dm_native.so | awk '{print "native:", $5, $9}'
# oracle, built in a container-local copy so Linux build caches never land in the host tree
rm -rf /tmp/w && mkdir -p /tmp/w/src /tmp/w/tests
cp -r /src/src/dotnet /tmp/w/src/ && rm -rf /tmp/w/src/dotnet/obj /tmp/w/src/dotnet/bin
cp -r /src/tests/parity /tmp/w/tests/ && rm -rf /tmp/w/tests/parity/oracle/obj /tmp/w/tests/parity/oracle/bin
cd /tmp/w && dotnet build tests/parity/oracle -c Release -o /tmp/oracle_out > /out/oracle_build.log 2>&1 || { echo "ORACLE BUILD FAILED"; grep -E " error " /out/oracle_build.log | head; exit 1; }
cp /out/native/librhino3dm_native.so /tmp/oracle_out/
cd /tmp/w && dotnet /tmp/oracle_out/oracle.dll
cp /tmp/w/tests/parity/golden/*.json /src/tests/parity/golden/
echo "ORACLE DONE"
