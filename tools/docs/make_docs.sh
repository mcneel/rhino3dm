#!/bin/bash
# The rhino3dm API reference (M5), from the generated typings -- one source for the typings and the
# reference:
#   Python: Sphinx + sphinx-autoapi over src/rhino3dm/__init__.pyi   -> build/dev/site/python
#   JS:     typedoc over src/js/rhino3dm.d.ts                        -> build/dev/site/javascript
# Both run in Docker (python:3.12, node:24; pinned tool versions). Regenerate the typings first
# (tools/dev/round.sh typings).
#
#   tools/docs/make_docs.sh [python|js|all] [--publish]
#
# --publish replaces docs/python/9.x/api and docs/javascript/9.x/api (served by GitHub Pages) with the
# result; without it nothing outside build/dev changes. The 8.x reference stays at docs/*/api until the
# 9.2 release, when the two swap (Luis 2026-11-04).
set -eu
REPO=$(cd "$(dirname "$0")/../.." && pwd)
OUT=$REPO/build/dev/site
WHAT=${1:-all}
PUBLISH=0
for a in "$@"; do [ "$a" = --publish ] && PUBLISH=1; done
VERSION=$(cat "$REPO/src/version.txt")
mkdir -p "$OUT"

python_docs() {
  rm -rf "$OUT/python" "$OUT/py_pkg" && mkdir -p "$OUT/py_pkg/rhino3dm"
  cp "$REPO/src/rhino3dm/__init__.pyi" "$OUT/py_pkg/rhino3dm/"
  docker run --rm -v "$REPO":/src:ro -v "$OUT":/out -e RH3DM_PKG=/out/py_pkg/rhino3dm -e RH3DM_VERSION="$VERSION" \
    python:3.12-bookworm bash -c '
      pip install -q sphinx==8.1.3 sphinx-autoapi==3.4.0 astroid==3.3.8 sphinx-rtd-theme==3.0.2 >/dev/null 2>&1
      cp -r /src/tools/docs/site/python /tmp/site       # autoapi writes its pages beside the sources
      sphinx-build -q -b html /tmp/site /out/python 2> /out/python_warnings.log
      echo "SPHINX EXIT = $?"; echo "warnings: $(grep -c . /out/python_warnings.log)"'
  echo "python: $(find "$OUT/python" -name '*.html' | wc -l | tr -d ' ') pages -> ${OUT#$REPO/}/python"
}

js_docs() {
  rm -rf "$OUT/javascript"
  docker run --rm -v "$REPO":/src:ro -v "$OUT":/out -w /tmp node:24 bash -c '
      npm i -g typedoc@0.26.11 typescript@5.6.3 >/dev/null 2>&1
      cp /src/src/js/rhino3dm.d.ts /src/docs/javascript/RHINO3DM.JS.md /tmp/
      typedoc /tmp/rhino3dm.d.ts --readme /tmp/RHINO3DM.JS.md --name "rhino3dm.js '"$VERSION"'" \
        --out /out/javascript --skipErrorChecking > /out/js_warnings.log 2>&1
      echo "TYPEDOC EXIT = $?"; echo "warnings: $(grep -c -i warning /out/js_warnings.log)"'
  echo "javascript: $(find "$OUT/javascript" -name '*.html' | wc -l | tr -d ' ') pages -> ${OUT#$REPO/}/javascript"
}

case "$WHAT" in
  python) python_docs ;;
  js)     js_docs ;;
  all)    python_docs; js_docs ;;
  *) sed -n '2,13p' "$0"; exit 2 ;;
esac

if [ $PUBLISH = 1 ]; then
  DEST=9.x/api
  if [ "$WHAT" != js ]; then rm -rf "$REPO/docs/python/$DEST" && mkdir -p "$REPO/docs/python/9.x" && cp -R "$OUT/python" "$REPO/docs/python/$DEST"; fi
  if [ "$WHAT" != python ]; then rm -rf "$REPO/docs/javascript/$DEST" && mkdir -p "$REPO/docs/javascript/9.x" && cp -R "$OUT/javascript" "$REPO/docs/javascript/$DEST"; fi
  echo "published into docs/python/$DEST, docs/javascript/$DEST"
fi
