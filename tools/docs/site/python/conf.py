# Sphinx configuration for the rhino3dm Python API reference (M5): sphinx-autoapi reads the generated
# typings (src/rhino3dm/__init__.pyi, tools/docs/pyi.py) statically -- nothing is imported -- so the
# reference is the same one source as the typings. Built by tools/docs/make_docs.sh.
import os

project = 'rhino3dm'
copyright = '2026, Robert McNeel & Associates'
author = 'Robert McNeel & Associates'
release = version = os.environ.get('RH3DM_VERSION', '')

extensions = ['autoapi.extension', 'sphinx.ext.napoleon']
autoapi_type = 'python'
autoapi_dirs = [os.environ['RH3DM_PKG']]          # a copy of src/rhino3dm holding __init__.pyi
autoapi_file_patterns = ['*.pyi']
autoapi_root = 'reference'
autoapi_own_page_level = 'class'
autoapi_member_order = 'groupwise'
autoapi_options = ['members', 'undoc-members', 'show-inheritance', 'show-module-summary', 'imported-members']
autoapi_add_toctree_entry = True
autoapi_python_class_content = 'class'
autoapi_keep_files = False
napoleon_google_docstring = True
suppress_warnings = ['autoapi.python_import_resolution']

html_theme = 'sphinx_rtd_theme'
html_title = 'rhino3dm.py %s' % release
templates_path = []
exclude_patterns = []


# The docstrings are plain text (RhinoCommon's prose, `name` for code); reStructuredText would read
# `*`, `|` and single backticks as markup. Literals become ``code``; everything else is escaped.
import re as _re


def _plain_to_rst(app, what, name, obj, options, lines):
    out = []
    for line in lines:
        parts = _re.split(r'(`[^`]+`)', line)
        res = []
        for i, p in enumerate(parts):
            if p.startswith('`') and p.endswith('`') and len(p) > 2:
                lit = '``%s``' % p[1:-1]
                # a literal touching a word character (`Point3d`s): an escaped space separates them
                if i > 0 and _re.search(r'\w$', parts[i - 1]):
                    lit = '\\ ' + lit
                if i + 1 < len(parts) and _re.match(r'\w', parts[i + 1]):
                    lit += '\\ '
                res.append(lit)
            else:
                res.append(_re.sub(r'([*|_\\])', r'\\\1', p))
        out.append(''.join(res))
    lines[:] = out


def setup(app):
    app.connect('autodoc-process-docstring', _plain_to_rst, priority=100)
