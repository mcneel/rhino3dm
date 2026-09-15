#!/usr/bin/env bash
#
# Regenerate the rhino3dm API documentation (Python + JavaScript).
#
# Automates the "Updating Documentation with docgen" steps from docs/publishing.md:
#   1. build + run docgen                -> src/docgen/out/{py_apidocs,js_apidocs,...}
#   2. sphinx-build the Python .rst       -> docs/python/api
#   3. typedoc the hand-maintained d.ts   -> docs/javascript/api
#
# The docgen executable embeds src/version.txt at build time, so the version shown
# in the generated docs always matches the current version.txt (it is rebuilt here).
#
# NOTE: src/js/rhino3dm.d.ts is HAND-MAINTAINED. This script never regenerates it;
#       the JavaScript docs are produced by running typedoc against it.
#
# Usage:
#   ./make_docs.sh [all|docgen|python|js]   (default: all)
#
set -euo pipefail

TARGET="${1:-all}"

# --- locations -------------------------------------------------------------
DOCGEN_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"   # src/docgen
REPO_ROOT="$(cd "$DOCGEN_DIR/../.." && pwd)"
OUT_DIR="$DOCGEN_DIR/out"
PY_RST="$OUT_DIR/py_apidocs"
PY_HTML="$PY_RST/sphinxout"
PY_DEST="$REPO_ROOT/docs/python/api"
JS_DOCS_DIR="$REPO_ROOT/src/js/docs"
JS_DEST="$REPO_ROOT/docs/javascript/api"

step() { printf '\n\033[1;34m==> %s\033[0m\n' "$*"; }
die()  { printf '\033[1;31mERROR: %s\033[0m\n' "$*" >&2; exit 1; }

# --- 1. docgen -------------------------------------------------------------
run_docgen() {
  step "Building and running docgen"
  command -v dotnet >/dev/null || die "dotnet not found on PATH."
  dotnet build "$DOCGEN_DIR/docgen.csproj" -c Debug
  # docgen locates its source/out dirs from a cwd containing "docgen", so run it there.
  ( cd "$DOCGEN_DIR/bin/Debug" && ./docgen )
  [ -f "$PY_RST/conf.py" ] || die "docgen did not produce $PY_RST (conf.py missing)."
}

# --- 2. python (sphinx) ----------------------------------------------------
pick_sphinx() {
  if [ -x "$REPO_ROOT/.venv/bin/sphinx-build" ]; then echo "$REPO_ROOT/.venv/bin/sphinx-build"
  elif [ -x "$DOCGEN_DIR/.venv/bin/sphinx-build" ]; then echo "$DOCGEN_DIR/.venv/bin/sphinx-build"
  elif command -v sphinx-build >/dev/null; then command -v sphinx-build
  else return 1; fi
}

build_python() {
  step "Building Python API docs (sphinx)"
  [ -f "$PY_RST/conf.py" ] || die "$PY_RST not found - run docgen first (./make_docs.sh docgen)."
  local sphinx
  sphinx="$(pick_sphinx)" || die "sphinx-build not found. Create a venv and install it:
    python3 -m venv .venv && source .venv/bin/activate
    pip install sphinx sphinx-rtd-theme"
  echo "using: $sphinx"
  rm -rf "$PY_HTML"
  "$sphinx" -M html "$PY_RST" "$PY_HTML"
  step "Replacing docs/python/api"
  rm -rf "$PY_DEST"; mkdir -p "$PY_DEST"
  cp -R "$PY_HTML/html/." "$PY_DEST/"
  echo "wrote $(find "$PY_DEST" -name '*.html' | wc -l | tr -d ' ') html files -> ${PY_DEST#$REPO_ROOT/}"
}

# --- 3. javascript (typedoc) ----------------------------------------------
build_js() {
  step "Building JavaScript API docs (typedoc on the hand-maintained d.ts)"
  command -v npm >/dev/null || die "npm not found on PATH."
  [ -f "$REPO_ROOT/src/js/rhino3dm.d.ts" ] || die "src/js/rhino3dm.d.ts not found."
  ( cd "$JS_DOCS_DIR"
    [ -d node_modules ] || npm install
    mkdir -p "$JS_DEST"
    # 'npm run docs' runs typedoc then copies ./docs/* into docs/javascript/api
    npm run docs )
  echo "wrote $(find "$JS_DEST" -name '*.html' | wc -l | tr -d ' ') html files -> ${JS_DEST#$REPO_ROOT/}"
}

# --- dispatch --------------------------------------------------------------
case "$TARGET" in
  all)    run_docgen; build_python; build_js ;;
  docgen) run_docgen ;;
  python) run_docgen; build_python ;;
  js)     build_js ;;                # typedoc needs only the hand-maintained d.ts
  -h|--help|help)
    grep '^#' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' | sed '/^!/d'
    exit 0 ;;
  *) die "unknown target '$TARGET' (expected: all | docgen | python | js)" ;;
esac

step "Done. Review changes under docs/python/api and docs/javascript/api, then commit."
