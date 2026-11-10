#!/bin/bash
# The migration guide (docs/migrating-to-9/<lang>.md and <lang>-members.md, generated from
# api/migration.md by tools/docs/migration.py) as two pages of a reference site:
#   guide_pages.sh python|javascript <dir>   -> <dir>/migrating.md, <dir>/migrating-members.md
# The links between the two pages follow the rename; "Other languages" becomes the other language's
# guide, relative within the published docs/ (python/api/migrating.html,
# javascript/api/documents/Migrating_to_9_2.html).
# Run by make_docs.sh inside its containers (/src is the repository).
set -eu
LANG_=$1; DIR=$2
SRC=$(cd "$(dirname "$0")/../.." && pwd)/docs/migrating-to-9
mkdir -p "$DIR"
# Sphinx keeps a relative link written as raw HTML (it rewrites a Markdown one); typedoc rewrites any
# relative link outside its own site, so the JavaScript page names the published URL
if [ "$LANG_" = python ]; then OTHER='<a href="../../javascript/api/documents/Migrating_to_9_2.html">JavaScript version</a>'
else OTHER='[Python version](https://mcneel.github.io/rhino3dm/python/api/migrating.html)'; fi
for f in "$LANG_" "$LANG_-members"; do
  out=$DIR/migrating${f#$LANG_}.md
  # typedoc names a page from its front matter (Sphinx reads the first heading and ignores this)
  [ "$LANG_" = javascript ] && printf -- '---\ntitle: "%s"\n---\n\n' \
    "$([ "$f" = "$LANG_" ] && echo 'Migrating to 9.2' || echo 'Migrating: renamed and removed members')" > "$out" || : > "$out"
  sed -e "s#(${LANG_}-members\.md#(migrating-members.md#g" \
      -e "s#(${LANG_}\.md#(migrating.md#g" \
      -e "s#\[← Other languages\](\.\./migrating-to-9\.md)#$OTHER#" \
      "$SRC/$f.md" >> "$out"
done
