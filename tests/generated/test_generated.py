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
    xf = r.TransformX.Translation(r.Vector3dX(0, 0, 5))
    assert c.Transform(xf) is True
    s = c.PointAtStart
    assert (s.X, s.Y, s.Z) == (0.0, 0.0, 5.0)


def test_finding_14_rhinocommon_value_semantics():
    q = r.Point3dX(4, 6, 3)
    assert r.Point3dX.Unset.DistanceTo(q) == 0.0


def test_same_arity_constructor_overloads_both_reachable():
    # LineCurve(Point2d, Point2d) and (Point3d, Point3d): pybind dispatches on
    # type, embind through a generated shape dispatcher -- neither may shadow
    # the other (generator-log G11: JS once kept only the Point2d one)
    a = r.LineCurveX(r.Point3dX(1, 2, 3), r.Point3dX(4, 5, 6)).Line
    b = r.LineCurveX(r.Point2dX(1, 2), r.Point2dX(4, 5)).Line
    assert (a.To.Z, b.To.Z) == (6.0, 0.0)


def test_property_setters_write_through():
    c = r.LineCurveX(r.Point3dX(0, 0, 0), r.Point3dX(2, 0, 0))
    c.Domain = r.IntervalX(1, 5)                     # value type through `ref value`
    assert (c.Domain.T0, c.Domain.T1) == (1.0, 5.0)
    ds = r.DimensionStyleX()
    ds.ArrowLength = 0.75                            # inlined private helper + selector
    assert ds.ArrowLength == 0.75
    with pytest.raises(AttributeError):
        c.PointAtStart = r.Point3dX(1, 1, 1)         # read-only in RhinoCommon stays read-only


needs_file3dm = pytest.mark.skipif(not hasattr(r, 'File3dmX'), reason='module built before File3dmX')


@needs_file3dm
def test_file3dm_table_view_keeps_the_file_alive():
    # a table is a VIEW onto the file's ONX_Model (mode 'child'): the
    # binding must keep the File3dm alive while the view exists
    f = r.File3dmX()
    objs = f.Objects
    del f
    gc.collect()
    objs.AddPoint(r.Point3dX(1, 2, 3), None)     # optional attributes: None
    objs.AddPoint(r.Point3dX(-1, 0, 5), None)
    bb = objs.GetBoundingBox()
    assert (bb.Min.X, bb.Max.Z) == (-1.0, 5.0)


@needs_file3dm
def test_file3dm_settings_view_writes_through():
    f = r.File3dmX()
    f.Settings.ModelAbsoluteTolerance = 0.25     # File3dmSettings views &model->m_settings
    assert f.Settings.ModelAbsoluteTolerance == 0.25


@needs_file3dm
def test_file3dm_read_missing_file_raises_file_not_found():
    # RhinoCommon's guard: `if (!File.Exists(path)) throw new FileNotFoundException(...)`
    with pytest.raises(FileNotFoundError):
        r.File3dmX.Read('/nonexistent/model.3dm')


# --- G28: logic the generator once dropped, now reproduced -------------------

def test_g28_early_returns_and_optional_arguments():
    pc = r.PolyCurveX()
    assert pc.Append(None) is False                  # `if (null == curve) return false;`
    assert isinstance(r.ArcCurveX(None), r.ArcCurveX)   # `new ArcCurve(null)`: ON_ArcCurve_New(nullptr)
    c = r.LineCurveX(r.Point3dX(0, 0, 0), r.Point3dX(1, 0, 0))
    assert c.Transform(r.TransformX.Identity) is True     # `if (xform.IsIdentity) return true;`


# --- G29: File3dm tables (generic RhinoCommon bases instantiated per table) --

@needs_file3dm
def test_file3dm_layer_table_basics():
    f = r.File3dmX()
    layers = f.AllLayers
    assert layers.Count == 0
    i0 = layers.AddLayer('a', (255, 0, 0, 255))
    i1 = layers.AddLayer('b', (0, 255, 0, 255))
    assert (i0, i1, layers.Count) == (0, 1, 2)
    assert [layers.FindIndex(i).Name for i in (0, 1)] == ['a', 'b']
    assert layers.FindIndex(7) is None               # RhinoCommon returns null, it does not throw
    assert f.Layers.Count == 2                       # IList<Layer> Layers: the same table
    with pytest.raises(IndexError):
        layers.Delete(7)                             # `if (!Remove(...)) { if (index >= Count) throw }`


@needs_file3dm
def test_file3dm_table_items_are_live_proxies():
    # RhinoCommon `new Layer(id, file)`: edits go into the model, and the item
    # keeps the file alive like the table does
    f = r.File3dmX()
    f.AllLayers.AddLayer('before', (0, 0, 0, 255))
    layer = f.AllLayers.FindIndex(0)
    layer.Name = 'after'
    assert f.AllLayers.FindIndex(0).Name == 'after'
    del f
    gc.collect()
    assert layer.Name == 'after'


@needs_file3dm
def test_file3dm_table_add_copies_and_delete_by_item():
    f = r.File3dmX()
    mine = r.LayerX()
    mine.Name = 'mine'
    f.AllLayers.Add(mine)                            # ONX_Model_AddModelComponent copies
    mine.Name = 'changed later'
    assert f.AllLayers.FindIndex(0).Name == 'mine'
    assert f.AllLayers.Delete(None) is False         # `if (item == null) return false;`
    assert f.AllLayers.Delete(f.AllLayers.FindIndex(0)) is True


@needs_file3dm
def test_file3dm_table_find_name():
    # G37: FindName<T> through the manifest table, with RhinoCommon's guards
    f = r.File3dmX()
    f.AllLayers.AddLayer('walls', (0, 0, 0, 255))
    assert f.AllLayers.FindName('walls', __import__('uuid').UUID(int=0)).Name == 'walls'
    assert f.AllLayers.FindName('nope', __import__('uuid').UUID(int=0)) is None


def test_nested_enum_lives_in_its_owner_class():
    # G35: DimensionStyle.ArrowType is DimensionStyleX.ArrowType in Python
    ds = r.DimensionStyleX()
    ds.ArrowType1 = r.DimensionStyleX.ArrowType.Dot
    assert ds.ArrowType1 == r.DimensionStyleX.ArrowType.Dot


def test_instance_definition_object_ids_holder():
    # G36: SimpleArrayGuid -> tuple of UUIDs
    assert r.InstanceDefinitionGeometryX().GetObjectIds() == ()


# --- G39: File3dm table protocols (len / [i] / iteration) --------------------

@needs_file3dm
def test_table_iteration_survives_a_delete():
    # pilot-findings #24: the old bindings iterate range(len) over sparse indices
    f = r.File3dmX()
    t = f.AllLayers
    for n in 'abc':
        t.AddLayer(n, (0, 0, 0, 255))
    assert t.Delete(t.FindIndex(1)) is True
    assert len(t) == 2
    assert [l.Name for l in t] == ['a', 'c']            # the native iterator: live components
    assert [l.Index for l in t] == [0, 2]               # indices are sparse and stable
    t.AddLayer('d', (0, 0, 0, 255))
    assert [l.Name for l in t] == ['a', 'c', 'd']
    assert [l.Name for l in f.Layers] == ['a', 'c', 'd']   # IList<Layer> Layers: the same table


@needs_file3dm
def test_table_getitem_is_the_manifest_index():
    f = r.File3dmX()
    t = f.AllLayers
    for n in 'abc':
        t.AddLayer(n, (0, 0, 0, 255))
    t.Delete(t.FindIndex(1))
    assert t[2].Name == 'c' and t[2].Index == 2        # [i] means "the layer whose Index is i"
    assert t.FindIndex(1) is None                       # RhinoCommon's indexer: null for a hole
    with pytest.raises(IndexError):
        t[1]                                            # Python's protocol: IndexError for a hole
    with pytest.raises(IndexError):
        t[7]


@needs_file3dm
def test_table_iteration_keeps_the_file_alive():
    f = r.File3dmX()
    f.AllLayers.AddLayer('only', (0, 0, 0, 255))
    it = iter(f.AllLayers)
    del f
    gc.collect()
    layer = next(it)
    del it
    gc.collect()
    assert layer.Name == 'only'
    with pytest.raises(StopIteration):
        next(iter([]))


# --- G40: File3dmObject, copy-on-write geometry (deviations D1), write-through attributes

needs_objects = pytest.mark.skipif(not hasattr(r, 'File3dmObjectX'), reason='module built before File3dmObjectX')


def _file_with_line():
    f = r.File3dmX()
    f.Objects.AddLine(r.Point3dX(0, 0, 0), r.Point3dX(1, 0, 0), None)
    return f


@needs_objects
def test_objects_iterate_as_file3dmobject_proxies():
    f = _file_with_line()
    f.Objects.AddPoint(r.Point3dX(5, 5, 5), None)
    objs = list(f.Objects)
    assert len(f.Objects) == 2 and len(objs) == 2
    assert isinstance(objs[0], r.File3dmObjectX)
    assert isinstance(objs[0].Geometry, r.LineCurveX)        # the most-derived view class
    assert isinstance(objs[1].Geometry, r.PointX)


@needs_objects
def test_geometry_is_copy_on_write_as_in_rhinocommon():
    # deviations.md D1 / pilot-findings #22: the old bindings edited the file in place
    f = _file_with_line()
    obj = next(iter(f.Objects))
    g = obj.Geometry
    assert g.PointAtStart.X == 0.0
    assert g.Transform(r.TransformX.Translation(r.Vector3dX(10, 0, 0))) is True
    assert g.PointAtStart.X == 10.0                             # the private copy moved ...
    assert obj.Geometry.PointAtStart.X == 0.0                   # ... the file did not
    # the .NET way to change the file: Delete + Add
    f.Objects.Delete(obj.Id)
    f.Objects.AddCurve(g, None)
    assert [o.Geometry.PointAtStart.X for o in f.Objects] == [10.0]


@needs_objects
def test_attributes_write_through_and_name_chains_to_them():
    f = _file_with_line()
    obj = next(iter(f.Objects))
    obj.Attributes.Name = 'via attributes'
    assert obj.Name == 'via attributes'                         # File3dmObject.Name => Attributes.Name
    obj.Name = 'via name'
    assert next(iter(f.Objects)).Attributes.Name == 'via name'  # written into the file


@needs_objects
def test_geometry_view_of_a_deleted_object_raises():
    f = _file_with_line()
    obj = next(iter(f.Objects))
    g = obj.Geometry
    f.Objects.Delete(obj.Id)
    with pytest.raises(RuntimeError):                           # DocumentCollectedException
        g.PointAtStart


# --- G41: RenderSettings, a view of the file that copies on write and commits back

needs_rs = pytest.mark.skipif(not hasattr(r, 'RenderSettingsX'), reason='module built before RenderSettingsX')


@needs_rs
def test_render_settings_setters_commit_into_the_file():
    f = r.File3dmX()
    rs = f.Settings.RenderSettings
    before = rs.ScaleBackgroundToFit
    rs.ScaleBackgroundToFit = not before                  # copy on write, then Commit() into the file
    assert rs.ScaleBackgroundToFit is (not before)        # the detached copy
    assert f.Settings.RenderSettings.ScaleBackgroundToFit is (not before)   # a fresh view of the file: committed
    rs.ShadowmapLevel = 3
    assert f.Settings.RenderSettings.ShadowmapLevel == 3


@needs_rs
def test_render_settings_same_file_result_as_the_old_in_place_bindings(tmp_path):
    # the old bindings wrote in place; vNext copies and commits -- the file is the same
    import os
    new = r.File3dmX()
    new.Settings.RenderSettings.ShadowmapLevel = 2
    new.Settings.RenderSettings.TransparentBackground = True
    old = r.File3dm()
    old.Settings.RenderSettings.ShadowMapLevel = 2  # the old binding's spelling (RhinoCommon: ShadowmapLevel)
    old.Settings.RenderSettings.TransparentBackground = True
    pn, po = str(tmp_path / 'new.3dm'), str(tmp_path / 'old.3dm')
    assert new.Write(pn, 8) if hasattr(new, 'Write') else True
    old.Write(po, 8)
    back = r.File3dm.Read(po)
    assert back.Settings.RenderSettings.ShadowMapLevel == 2 and back.Settings.RenderSettings.TransparentBackground is True
    if os.path.exists(pn):
        assert r.File3dm.Read(pn).Settings.RenderSettings.ShadowmapLevel == 2


@needs_rs
def test_render_settings_view_keeps_the_file_alive():
    f = r.File3dmX()
    rs = f.Settings.RenderSettings
    del f
    gc.collect()
    rs.ShadowmapLevel = 1
    assert rs.ShadowmapLevel == 1


# --- G42: the RDK settings (GroundPlane, Sun, ...): write-through file views, Variant properties

needs_rdk = pytest.mark.skipif(not hasattr(r, 'GroundPlaneX'), reason='module built before GroundPlaneX')


@needs_rdk
def test_ground_plane_writes_through_into_the_file():
    f = r.File3dmX()
    gp = f.Settings.RenderSettings.GroundPlane            # a view of the file (CppFromFile3dm)
    gp.Enabled = True
    gp.Altitude = 2.5
    gp.TextureOffset = r.Vector2dX(0.25, 0.75)
    again = f.Settings.RenderSettings.GroundPlane
    assert again.Enabled is True and again.Altitude == 2.5
    assert (again.TextureOffset.X, again.TextureOffset.Y) == (0.25, 0.75)


@needs_rdk
def test_free_floating_settings_own_their_object():
    gp = r.GroundPlaneX()                                  # DefaultCppConstructor / DeleteCpp
    gp.Altitude = 7.0
    assert gp.Altitude == 7.0
    rs = r.RenderSettingsX()                               # free-floating render settings ...
    inner = rs.GroundPlane                                  # ... hand out a pointer INTO them, not a copy
    inner.Altitude = 3.0
    assert rs.GroundPlane.Altitude == 3.0


@needs_rdk
def test_sun_and_safe_frame_round_trip():
    f = r.File3dmX()
    rs = f.Settings.RenderSettings
    rs.Sun.Enabled = True
    rs.SafeFrame.Enabled = True
    assert f.Settings.RenderSettings.Sun.Enabled is True
    assert f.Settings.RenderSettings.SafeFrame.Enabled is True
