#!/bin/bash
# The Python typings against the built module, inside ubuntu:24.04 (the module's glibc). Mounts:
# /src = repo (read-only), /work = build/dev. Strict mypy on the stub, then mypy's stubtest reduced to
# what it can judge for a nanobind module: names missing on either side. (nanobind functions are not
# Python functions, so their signatures, staticmethod-ness and the classes' metaclass are reported for
# every member; `__nb_enum__` is nanobind's, and enum values named `None` are kept as comments.)
set -u
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq >/dev/null 2>&1
apt-get install -yqq python3 python3-venv >/dev/null 2>&1
[ -x /tmp/v/bin/python ] || { python3 -m venv /tmp/v && /tmp/v/bin/pip install -q mypy==1.13.0 >/dev/null 2>&1; }
P=/tmp/pkg/rhino3dm
rm -rf /tmp/pkg && mkdir -p $P
cp /src/src/rhino3dm/__init__.py /src/src/rhino3dm/__init__.pyi /src/src/rhino3dm/py.typed $P/
cp /work/python/_rhino3dm*.so $P/
cd /tmp/pkg
/tmp/v/bin/python -m mypy --strict rhino3dm/__init__.pyi 2>&1 | tail -1 | sed 's/^/MYPY: /'
/tmp/v/bin/python -m mypy.stubtest rhino3dm > /tmp/st.txt 2>&1
grep -E '^error: .* (is not present in stub|is not present at runtime)' /tmp/st.txt \
  | grep -v -E '\.__nb_enum__ is not present|\.None is not present in stub' > /tmp/drift.txt
echo "STUBTEST: $(wc -l < /tmp/drift.txt | tr -d ' ') names missing on one side"
head -20 /tmp/drift.txt
