"""Unit tests for tools/generate/old_coverage.py's JS retirement check (retired.toml in both
languages): synthetic JS surface dumps, no build needed.

    python3 -m pytest tests/generator
"""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'tools', 'generate'))
import old_coverage as oc  # noqa: E402

JSM = {
    'SubDFaceIteratorFromSubD': ['count', 'first', 'next'],
    'SubDFaceListX': ['count', 'find', 'iterator'],
    'CurveX': ['pointAt', 'domain'],
    'ArcCurveX': ['radius'],
    'ModelComponentX': ['namePathSeparator'],
    'File3dmLayerTableX': ['get', 'count'],
    '$bases': {'ArcCurveX': 'CurveX'},
}


def test_spellings_cover_case_dunders_and_accessors():
    assert oc.js_has(JSM, 'SubDFaceListX', 'Count')               # PascalCase vs camelCase
    assert oc.js_has(JSM, 'File3dmLayerTableX', '__getitem__')   # protocol dunder -> get
    assert oc.js_has(JSM, 'File3dmLayerTableX', '__len__')       # -> count
    assert oc.js_has(JSM, 'SubDFaceListX', '__iter__')           # -> iterator
    assert oc.js_has(JSM, 'ModelComponentX', 'NamePathSeparator')


def test_inherited_members_and_constructors_count():
    assert oc.js_has(JSM, 'ArcCurveX', 'PointAt')                # through the embind base
    assert oc.js_has(JSM, 'ArcCurveX', '__init__')               # `new ArcCurveX(..)`


def test_old_bnd_class_resolves_to_its_js_name():
    assert oc.js_has(JSM, 'BND_SubDFaceIteratorFromSubD', 'Next')


def test_missing_is_false_and_unregistered_class_is_none():
    assert oc.js_has(JSM, 'SubDFaceListX', 'Nonexistent') is False
    assert oc.js_has(JSM, 'CurveX', 'Radius') is False           # a DERIVED class's member
    assert oc.js_has(JSM, 'Vector3d', 'DotProduct') is None      # not a JS class: no JS check
