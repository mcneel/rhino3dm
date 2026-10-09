"""Regression tests for the vNext generated Python bindings.

Behavior (numbers) is covered by tests/parity against .NET; these pin the
STRUCTURAL contracts of the generated surface and every bug the effort has
found (numbers refer to docs/vnext/pilot-findings.md), so each stays fixed.

    RH3DM_MODULE_DIR=<dir containing _rhino3dm*.so> pytest tests/generated
"""

import gc
import os
import sys

import pytest

sys.path.insert(0, os.environ.get('RH3DM_MODULE_DIR', '.'))
r = pytest.importorskip('_rhino3dm')


def curve():
    pts = r.Point3dList(0)
    for xyz in [(0, 0, 0), (1, 2, 0), (4, 1, 0), (6, -2, 0)]:
        pts.Add(*xyz)
    return r.Curve.CreateControlPointCurve(pts, 3)


def val(v):
    return v() if callable(v) else v


# --- marshalling shapes -----------------------------------------------------

def test_guard_raises_like_rhinocommon():
    with pytest.raises(IndexError):
        r.CurveX(curve()).SpanDomain(-1)


def test_holder_array_return():
    c = curve()
    assert r.CurveX(c).SpanVector() == (c.Domain.T0, c.Domain.T1)


def test_enum_parameters_are_type_checked():
    x = r.CurveX(curve())
    assert x.IsContinuous(r.Continuity.C2_continuous, 0.5) is True
    with pytest.raises(TypeError):
        x.IsContinuous(3, 0.5)          # a raw int is not a Continuity


def test_string_argument_roundtrip():
    # string INPUT marshalling (RH3DM_S); the name check is RhinoCommon's
    assert r.ModelComponentX.IsValidComponentName("Layer 01") is True
    assert r.ModelComponentX.IsValidComponentName("") is False


# --- classes ---------------------------------------------------------------

def test_abstract_bases_are_not_constructible():
    for base in (r.GeometryBaseX, r.ModelComponentX):
        with pytest.raises(TypeError):
            base()


def test_inheritance_reaches_base_members():
    pc = r.PolyCurveX()
    pc.Append(r.LineCurveX((0, 0, 0), (2, 0, 0)))
    assert isinstance(pc, r.CurveX) and isinstance(pc, r.GeometryBaseX)
    mid = pc.PointAt(1.0)                       # declared on CurveX
    assert (mid.X, mid.Y, mid.Z) == (1.0, 0.0, 0.0)


def test_bridge_free_classes_reject_old_binding_objects():
    with pytest.raises(TypeError):
        r.ViewportX(r.ViewportInfo())
    v = r.ViewportX()
    assert v.SetCameraLocation((7, 8, 9)) is True
    loc = v.CameraLocation
    assert (loc.X, loc.Y, loc.Z) == (7.0, 8.0, 9.0)


def test_subclass_aware_wrapping():
    srf = r.SurfaceX(r.Sphere(r.Point3d(0, 0, 0), 5).ToNurbsSurface())
    assert type(srf.IsoCurve(1, 0.5)).__name__ == 'NurbsCurveX'


def test_class_argument_is_copied_not_adopted():
    pc = r.PolyCurveX()
    seg = r.LineCurveX((0, 0, 0), (1, 0, 0))
    assert pc.Append(seg) is True
    del seg
    gc.collect()                                # the C layer duplicated it
    assert pc.SegmentCount == 1


def test_rhinocommon_copy_constructor_is_deep():
    a = r.LinetypeX()
    a.AppendSegment(1.5, True)
    b = r.LinetypeX(a)
    b.AppendSegment(2.0, False)
    assert (val(a.PatternLength), val(b.PatternLength)) == (1.5, 3.5)


def test_zero_copy_point_buffer():
    np = pytest.importorskip('numpy')
    pc = r.PointCloud()
    for i in range(10):
        pc.Add(r.Point3d(i, 2 * i, 3))
    a = np.asarray(r.PointCloudX(pc))
    assert a.shape == (10, 3) and not a.flags.owndata
    assert a[7].tolist() == [7.0, 14.0, 3.0]


# --- found bugs ------------------------------------------------------------

def test_finding_01_linetype_double_free():
    lt = r.Linetype()
    del lt                                      # segfaulted before the fix
    gc.collect()


def test_ref_of_by_value_transform_uses_the_argument():
    # `ref xform` on a by-value parameter: must transform by the ARGUMENT,
    # not by a fresh identity matrix
    c = r.LineCurveX((0, 0, 0), (1, 0, 0))
    xf = r.TransformX(r.Transform.Translation(r.Vector3d(0, 0, 5)))
    assert c.Transform(xf) is True
    s = c.PointAtStart
    assert (s.X, s.Y, s.Z) == (0.0, 0.0, 5.0)


def test_finding_14_rhinocommon_value_semantics():
    q = r.Point3dX(4, 6, 3)
    assert r.Point3dX.Unset.DistanceTo(q) == 0.0
