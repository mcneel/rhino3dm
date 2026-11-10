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
# --publish replaces docs/python/api and docs/javascript/api (served by GitHub Pages) with the result;
# without it nothing outside build/dev changes. The last 8.x reference is kept in docs/8/python/api and
# docs/8/javascript/api.
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
      pip install -q sphinx==8.1.3 sphinx-autoapi==3.4.0 astroid==3.3.8 sphinx-rtd-theme==3.0.2 myst-parser==4.0.0 >/dev/null 2>&1
      cp -r /src/tools/docs/site/python /tmp/site       # autoapi writes its pages beside the sources
      bash /src/tools/docs/guide_pages.sh python /tmp/site   # the migration guide, as site pages
      # -d: the doctree cache stays out of the site (23 MB the published pages never use)
      sphinx-build -q -b html -d /tmp/doctrees /tmp/site /out/python 2> /out/python_warnings.log
      echo "SPHINX EXIT = $?"; echo "warnings: $(grep -c . /out/python_warnings.log)"'
  echo "python: $(find "$OUT/python" -name '*.html' | wc -l | tr -d ' ') pages -> ${OUT#$REPO/}/python"
}

js_docs() {
  rm -rf "$OUT/javascript"
  docker run --rm -v "$REPO":/src:ro -v "$OUT":/out -w /tmp node:24 bash -c '
      npm i -g typedoc@0.26.11 typescript@5.6.3 >/dev/null 2>&1
      cp /src/src/js/rhino3dm.d.ts /src/docs/javascript/RHINO3DM.JS.md /tmp/
      bash /src/tools/docs/guide_pages.sh javascript /tmp/guide   # the migration guide, as site pages
      # the README (also the npm page) links the guide on the published site; here, the pages built with it
      sed -i -e "s#https://mcneel.github.io/rhino3dm/javascript/api/documents/Migrating__renamed_and_removed_members.html#guide/migrating-members.md#g" \
             -e "s#https://mcneel.github.io/rhino3dm/javascript/api/documents/Migrating_to_9_2.html#guide/migrating.md#g" /tmp/RHINO3DM.JS.md
      # attached to the module (@document), not the project (--projectDocuments): project documents give
      # the site a module level whose page has no description; this keeps it flat, as 8.x was
      { printf "/**\n * @packageDocumentation\n * @document guide/migrating.md\n * @document guide/migrating-members.md\n */\n"
        cat /tmp/rhino3dm.d.ts; } > /tmp/doc.d.ts && mv /tmp/doc.d.ts /tmp/rhino3dm.d.ts
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
  DEST=api
  if [ "$WHAT" != js ]; then rm -rf "$REPO/docs/python/$DEST" && cp -R "$OUT/python" "$REPO/docs/python/$DEST"; fi
  if [ "$WHAT" != python ]; then rm -rf "$REPO/docs/javascript/$DEST" && cp -R "$OUT/javascript" "$REPO/docs/javascript/$DEST"; fi
  echo "published into docs/python/$DEST, docs/javascript/$DEST"
fi
