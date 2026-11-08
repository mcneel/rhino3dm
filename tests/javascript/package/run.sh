#!/bin/bash
# Check the npm package as its users load it: run.sh <dir with the package's files>
#   npm pack -> install the tarball -> Node require, Node import, a webpack 5 browser bundle
# Needs Node 24+ and network access (npm install). Exits non-zero on the first failure.
set -euo pipefail
PKG=$(cd "$1" && pwd)
HERE=$(cd "$(dirname "$0")" && pwd)
WORK=$(mktemp -d)
cp -r "$HERE"/package.json "$HERE"/check.cjs "$HERE"/check.mjs "$HERE"/webpack.config.cjs "$HERE"/web "$WORK"/
TGZ=$(cd "$PKG" && npm pack --pack-destination "$WORK" --silent | tail -1)
echo "packed $TGZ:"; tar -tzf "$WORK/$TGZ" | sed 's/^/  /'
cd "$WORK"
npm install --no-audit --no-fund --silent "./$TGZ"
node check.cjs
node check.mjs
node node_modules/webpack-cli/bin/cli.js --config webpack.config.cjs
if grep -Eq '"node:|require\("ws"\)' dist/*.js; then echo "FAIL: the browser bundle imports Node modules"; exit 1; fi
ls dist | grep -q '\.wasm$' || { echo "FAIL: webpack did not emit the .wasm"; exit 1; }
echo "webpack OK: $(ls dist | tr '\n' ' ')"
