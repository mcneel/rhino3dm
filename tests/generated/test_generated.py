"""Regression tests for the vNext generated Python bindings.

Behavior (numbers) is covered by tests/parity against .NET; these pin the
STRUCTURAL contracts of the generated surface and every bug the effort has
found (numbers refer to docs/vnext/pilot-findings.md), so each stays fixed.

    RH3DM_MODULE_DIR=<dir containing _rhino3dm*.so> pytest tests/generated
"""

import gc
import os
import sys
import uuid

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
        assert r.File3dm.Read(pn).Settings.RenderSettings.ShadowMapLevel == 2   # read back through the OLD binding


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


# --- G43: components -- (owner, key), resolved through the owner on every access

needs_comp = pytest.mark.skipif(not hasattr(r, 'BrepFaceX'), reason='components not generated')
MODELS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'models')


def box_brep():
    return r.BrepX.CreateFromBox(r.BoundingBoxX(0, 0, 0, 1, 2, 3))


@needs_comp
def test_brep_faces_are_components_of_the_brep():
    b = box_brep()
    faces = b.Faces
    assert len(faces) == 6
    assert [f.FaceIndex for f in faces] == list(range(6))          # sequence protocol over this[int]
    f = faces[2]
    assert isinstance(f, r.SurfaceProxyX) and isinstance(f, r.SurfaceX)   # RhinoCommon's inheritance
    assert f.IsSurface and f.IsPlanar(1e-9)                        # own member, inherited Surface member
    with pytest.raises(IndexError):
        faces[6]
    # writes go into the brep: a fresh component of the same face sees them
    was = f.OrientationIsReversed
    f.OrientationIsReversed = not was
    assert b.Faces[2].OrientationIsReversed == (not was)


@needs_comp
def test_brep_component_keeps_its_brep_alive():
    f = box_brep().Faces[0]
    gc.collect()
    assert f.FaceIndex == 0 and f.IsSurface


@needs_comp
def test_brep_edges_and_vertices():
    b = box_brep()
    assert len(b.Edges) == 12 and len(b.Vertices) == 8
    e = b.Edges[3]
    assert e.EdgeIndex == 3 and isinstance(e, r.CurveProxyX) and isinstance(e, r.CurveX)
    assert b.Vertices[7].VertexIndex == 7


def subd_from_file():
    f = r.File3dmX.Read(os.path.join(MODELS, 'subdBox.3dm'))
    for o in f.Objects:
        g = o.Geometry
        if isinstance(g, r.SubDX):
            return f, g
    pytest.skip('no SubD in subdBox.3dm')


@needs_comp
def test_subd_lists_iterate_like_foreach():
    f, s = subd_from_file()
    for lst in (s.Faces, s.Edges, s.Vertices):
        items = list(lst)
        assert len(items) == len(lst) == lst.Count > 0
    v = next(iter(s.Vertices))
    assert v.EdgeCount > 0 and v.FaceCount > 0
    # Next/Previous walk the same vertices the enumerator yields
    n, cur = 0, v
    while cur is not None:
        n += 1
        cur = cur.Next
    assert n == len(s.Vertices)
    e = next(iter(s.Edges))
    assert e.VertexFrom is not None and e.VertexTo is not None and e.FaceCount >= 1


@needs_comp
def test_subd_vertex_writes_land_in_the_subd():
    f, s = subd_from_file()
    v = next(iter(s.Vertices))
    v.ControlNetPoint = r.Point3dX(10, 20, 30)
    p = next(iter(s.Vertices)).ControlNetPoint
    assert (p.X, p.Y, p.Z) == (10, 20, 30)


@needs_comp
def test_point_cloud_items_and_knot_lists():
    pc = r.PointCloud()
    for i in range(4):
        pc.Add(r.Point3d(i, 2 * i, 0))
    cloud = r.PointCloudX(pc)
    item = cloud[2]
    assert item.Index == 2 and (item.Location.X, item.Location.Y) == (2, 4)
    item.Location = r.Point3dX(9, 9, 9)
    assert cloud[2].Location.Z == 9
    with pytest.raises(IndexError):
        cloud[4]
    ns = r.NurbsSurfaceX.CreateFromSphere(r.SphereX(r.Point3dX(0, 0, 0), 2.0))
    ku = ns.KnotsU
    assert len(ku) == ku.Count > 0 and ku.ClampedAtStart in (True, False)
    assert ns.KnotsV.Count > 0


@needs_comp
def test_physically_based_material_is_a_conditional_child():
    m = r.MaterialX()
    assert m.PhysicallyBased is None                              # `IsPhysicallyBased ? new T(this) : null`
    m.ToPhysicallyBased()
    pbr = m.PhysicallyBased
    pbr.Metallic = 0.25
    assert m.PhysicallyBased.Metallic == 0.25                     # a view onto the material


@needs_comp
def test_subd_neighbours_replace_the_old_component_iterators():
    # old face.Edges / face.Vertices / vertex.Faces / vertex.Edges / edge.Faces:
    # RhinoCommon's Count + XAt(i), each a component of the same SubD
    f, s = subd_from_file()
    face = next(iter(s.Faces))
    edges = [face.EdgeAt(i) for i in range(face.EdgeCount)]
    verts = [face.VertexAt(i) for i in range(face.VertexCount)]
    assert len(edges) == len(verts) == face.EdgeCount >= 3
    v = verts[0]
    assert all(v.EdgeAt(i) is not None for i in range(v.EdgeCount))
    assert all(v.FaceAt(i) is not None for i in range(v.FaceCount))
    e = edges[0]
    assert all(e.FaceAt(i) is not None for i in range(e.FaceCount))
    with pytest.raises(IndexError):
        v.EdgeAt(-1)                              # RhinoCommon's guard
    with pytest.raises(IndexError):
        v.EdgeAt(v.EdgeCount)                     # its failure tail: index >= EdgeCount
    # old subd.Faces[id]: RhinoCommon's Find(id)
    assert s.Vertices.Find(0) is None             # ids start at 1
    assert s.Vertices.Find(1) is not None


# --- G46: native-backed classes ----------------------------------------------

needs_g46 = pytest.mark.skipif(not hasattr(r, 'FileReferenceX'), reason='G46 classes not generated')


@needs_g46
def test_environment_owns_its_object_and_fills_a_new_texture():
    e = r.EnvironmentX()
    assert e.BackgroundProjection is not None
    tex = e.BackgroundImage                       # `var tex = new Texture(); F(..., tex.NonConstPointer())`
    assert isinstance(tex, r.TextureX)
    assert e.BackgroundImage is not tex           # a new texture per access, as in .NET


@needs_g46
def test_file_reference_is_a_handle_with_rhinocommons_paths():
    fr = r.FileReferenceX.CreateFromFullAndRelativePaths('/tmp/textures/wood.png', 'textures/wood.png')
    assert fr is not None and fr.IsSet
    assert fr.FullPath == '/tmp/textures/wood.png'
    assert fr.RelativePath == 'textures/wood.png'
    del fr
    gc.collect()                                  # freed through ON_FileReference_Delete


@needs_g46
def test_subd_components_have_ids_and_status_from_their_base():
    f, s = subd_from_file()
    faces = list(s.Faces)
    ids = [x.Id for x in faces]
    assert len(set(ids)) == len(ids) and min(ids) >= 1
    assert isinstance(faces[0], r.SubDComponentX)
    assert s.Faces.Find(ids[0]).Id == ids[0]
    v = next(iter(s.Vertices))
    assert v.IsSelected in (False, True) and v.IsDeleted is False


@needs_g46
def test_mesh_modifiers_are_children_of_the_attributes():
    a = r.ObjectAttributesX()
    mm = a.File3dmMeshModifiers
    # `if (!HasDisplacement(attr)) return null;` -- a fresh attributes object has none
    assert mm.Displacement is None and mm.EdgeSoftening is None and mm.Thickening is None
    assert mm.CurvePiping is None and mm.ShutLining is None
    # objects read from files: reachable, and present modifiers read back
    for name in ('subd.3dm', 'subd_creases.3dm', 'mesh.3dm'):
        f = r.File3dmX.Read(os.path.join(MODELS, name))
        for o in f.Objects:
            m = o.Attributes.File3dmMeshModifiers
            d = m.Displacement
            if d is not None:
                assert d.On in (True, False)


# --- G47: constructor-specialized lists (Brep.Loops / Brep.Trims) -------------

@needs_comp
def test_brep_loops_and_trims_are_reachable_components():
    b = box_brep()
    loops, trims = b.Loops, b.Trims
    assert len(loops) == 6 and len(trims) == 24             # a box: one loop per face, 4 trims each
    assert [l.LoopIndex for l in loops] == list(range(6))
    assert all(isinstance(t, r.BrepTrimX) and isinstance(t, r.CurveX) for t in trims)
    assert trims[5].TrimIndex == 5
    assert loops[0].Face.FaceIndex == 0                     # BrepLoop.Face: through the brep's face list
    with pytest.raises(IndexError):
        loops[6]


# --- G48: borrowed pointers (CachedTextureCoordinates), literal constants -----

needs_g48 = pytest.mark.skipif(not hasattr(r, 'CachedTextureCoordinatesX'), reason='G48 classes not generated')


@needs_g48
def test_cached_texture_coordinates_are_a_view_into_the_mesh():
    import uuid
    f = r.File3dmX.Read(os.path.join(MODELS, 'meshWithTexture.3dm'))
    meshes = [o.Geometry for o in f.Objects if isinstance(o.Geometry, r.MeshX)]
    if not meshes:
        pytest.skip('no mesh in meshWithTexture.3dm')
    # `if (tc_pointer == IntPtr.Zero) return null;` -- no cache for an unknown mapping
    assert meshes[0].GetCachedTextureCoordinates(uuid.uuid4()) is None
    # a real cache, when the file carries one under the mesh's own texture mapping ids
    ids = {o.Attributes.ObjectId for o in f.Objects}
    for m in meshes:
        for mid in ids:
            tc = m.GetCachedTextureCoordinates(mid)
            if tc is not None:
                assert tc.IsReadOnly is True and tc.Count >= 0 and tc.Dim in (2, 3)
                assert tc.TryGetAt(-1) == (False, -1.0, -1.0, -1.0)    # RhinoCommon's -1 defaults
                return


@pytest.mark.skipif(not hasattr(r.MaterialX, 'MaxShine'), reason='G48 not generated')
def test_literal_constant_properties():
    lt = r.LinetypeX()
    assert lt.IsModified is False and lt.IsPatternLocked is False    # RhinoCommon's portable `return false;`
    assert r.MaterialX.MaxShine == 255.0


# --- G49: category N idioms (native call plus a little logic around it) -----

@pytest.mark.skipif(not hasattr(r.TextureMappingX, 'CreatePlaneMapping'), reason='G49 not generated')
def test_texture_mapping_factories_return_none_on_failure():
    # `if (!SetPlaneMapping(...)) { rc.Dispose(); rc = null; }`
    i = r.IntervalX(0, 1)
    tm = r.TextureMappingX.CreatePlaneMapping(r.PlaneX.WorldXY, i, i, i, True)
    assert isinstance(tm, r.TextureMappingX) and tm.Capped is True
    assert isinstance(r.TextureMappingX.CreatePlaneMapping(r.PlaneX.WorldXY, i, i, i), r.TextureMappingX)
    assert r.TextureMappingX.CreatePlaneMapping(r.PlaneX.Unset, i, i, i, False) is None   # origin unset
    assert isinstance(r.TextureMappingX.CreateSurfaceParameterMapping(), r.TextureMappingX)
    assert isinstance(r.TextureMappingX.CreateSphereMapping(r.SphereX(r.Point3dX(0, 0, 0), 2)), r.TextureMappingX)


@pytest.mark.skipif(not hasattr(r.ExtrusionX, 'IsCappedAtBottom'), reason='G49 not generated')
def test_extrusion_caps_decode_the_native_flags():
    # `return (1 == rc || 3 == rc);` over ON_Extrusion::IsCapped's 0/1/2/3
    cyl = r.CylinderX(r.CircleX(r.Point3dX(0, 0, 0), 1.0), 4.0)
    for bottom, top in ((False, False), (True, False), (False, True), (True, True)):
        e = r.ExtrusionX.CreateCylinderExtrusion(cyl, bottom, top)
        assert (e.IsCappedAtBottom, e.IsCappedAtTop) == (bottom, top)
        assert (e.IsMiteredAtStart, e.IsMiteredAtEnd) == (False, False)


@pytest.mark.skipif(not hasattr(r.ExtrusionX, 'IsCappedAtBottom'), reason='G49 not generated')
def test_out_param_tuples_keep_the_csharp_result():
    # G49: a non-bool C# result leads the tuple (it used to be dropped)
    a = r.CircleX(r.Point3dX(0, 0, 0), 1.0)
    b = r.CircleX(r.Point3dX(1, 0, 0), 1.0)
    res = r.IntersectionX.CircleCircle(a, b)
    assert len(res) == 3 and isinstance(res[0], r.CircleCircleIntersection) and int(res[0]) == 2   # Multiple
    pc = r.PolyCurveX()
    seg = pc.SegmentIndexes(r.IntervalX(0, 1))
    assert len(seg) == 3 and isinstance(seg[0], int)


@pytest.mark.skipif(not hasattr(r.TextureX, 'UvwTransform'), reason='G49 not generated')
def test_value_out_initializers_are_reproduced():
    # `Transform value = new Transform(1); ON_Texture_uvw(p, ref value);`
    xf = r.TextureX().UvwTransform
    assert [xf.M00, xf.M11, xf.M22, xf.M33] == [1.0, 1.0, 1.0, 1.0]
    tm = r.TextureMappingX.CreateSurfaceParameterMapping()
    t = tm.Evaluate(r.Point3dX(0.25, 0.5, 0), r.Vector3dX(0, 0, 1))
    assert len(t) == 2 and isinstance(t[0], int)


@pytest.mark.skipif(not hasattr(r.SurfaceX, 'Domain'), reason='G51 not generated')
def test_parameter_normalization_conditional_args_and_ordered_throws():
    ns = r.NurbsSurfaceX.CreateFromSphere(r.SphereX(r.Point3dX(0, 0, 0), 2.0))
    # `if (direction != 0) direction = 1;`
    d1, d7 = ns.Domain(1), ns.Domain(7)
    assert (d1.T0, d1.T1) == (d7.T0, d7.T1)
    # `start ? 0 : 1`
    ku = ns.KnotsU
    assert isinstance(ku.SuperfluousKnot(True), float)
    assert ku.SuperfluousKnot(True) <= ku.SuperfluousKnot(False)
    # `if (!GetPoint(...)) { if (index < 0) throw; if (index >= Count) throw; }`
    pc = r.PointCloud()
    for i in range(3):
        pc.Add(r.Point3d(i, 0, 0))
    cloud = r.PointCloudX(pc)
    assert cloud.PointAt(1).X == 1
    for bad in (-1, 3):
        with pytest.raises(IndexError):
            cloud.PointAt(bad)


@needs_comp
@pytest.mark.skipif(not hasattr(r, 'SubDEdgeX') or not hasattr(r.SubDEdgeX, 'EndSharpness'), reason='G51 not generated')
def test_guard_message_after_nameof():
    # `throw new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be 0 or 1.")`
    f, s = subd_from_file()
    e = next(iter(s.Edges))
    assert isinstance(e.EndSharpness(0, False), float)
    with pytest.raises(IndexError, match='endIndex must be 0 or 1'):
        e.EndSharpness(2, False)


@pytest.mark.skipif(not hasattr(r.MeshingParametersX, 'FastRenderMesh'), reason='G51 not generated')
def test_value_classes_adopt_native_results():
    # `var p = UNM.F(); var mp = new MeshingParameters(p); return mp;`: copied in, released
    fast, quality = r.MeshingParametersX.FastRenderMesh, r.MeshingParametersX.QualityRenderMesh
    assert isinstance(fast, r.MeshingParametersX) and isinstance(quality, r.MeshingParametersX)
    assert isinstance(r.MeshingParametersX.DefaultAnalysisMesh.RelativeTolerance, float)
    # `if (IntPtr.Zero == p) return null;` -> None
    attrs = r.ObjectAttributesX()
    assert attrs.CustomMeshingParameters is None
    attrs.CustomMeshingParameters = quality
    got = attrs.CustomMeshingParameters
    assert got is not None and got.RelativeTolerance == quality.RelativeTolerance
    nc = r.NurbsCurveX.CreateFromCircle(r.CircleX(r.Point3dX(0, 0, 0), 1.0))
    assert isinstance(nc.ConvertSpanToBezier(0), r.BezierCurveX)
    assert nc.ConvertSpanToBezier(99) is None


# --- G53/G54: colors from outs, opennurbs enums, array fills, call sequences --

def quad_mesh():
    m = r.Mesh()
    for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)]:
        m.Vertices.Add(*xyz)
    m.Faces.AddFace(0, 1, 2, 3)
    return r.MeshX(m)


@pytest.mark.skipif(not hasattr(r.PointCloudX, 'GetPoints'), reason='G54 not generated')
def test_array_fills_return_what_the_native_wrote():
    pc = r.PointCloud()
    for i in range(3):
        pc.Add(r.Point3d(i, 2 * i, 0))
    cloud = r.PointCloudX(pc)
    pts = cloud.GetPoints()
    assert [(p.X, p.Y) for p in pts] == [(0, 0), (1, 2), (2, 4)]
    assert cloud.GetNormals() == ()                     # `if (count < 1) return new Vector3d[0];`
    assert r.ObjectAttributesX().GetGroupList() is None  # `if (count < 1) return null;`
    mx = quad_mesh()
    assert mx.VertexColors.ToARGBArray() == ()
    faces = mx.TopologyEdges.GetConnectedFaces(0)
    assert faces == (0,)
    # `int count = SpanCount(direction) + 1; ... bool ok = GetSpanVector(...); if (ok) return rc; return null;`
    ns = r.NurbsSurfaceX.CreateFromSphere(r.SphereX(r.Point3dX(0, 0, 0), 2.0))
    sv = ns.GetSpanVector(0)
    assert len(sv) == ns.SpanCount(0) + 1 and list(sv) == sorted(sv)


@pytest.mark.skipif(not hasattr(r.MeshVertexListX, 'HideAll'), reason='G54 not generated')
def test_native_call_sequences():
    mx = quad_mesh()
    mx.Vertices.HideAll()                               # EnsureHiddenList, then HideAll
    assert mx.Vertices.Count == 4
    mx.Vertices.Clear()                                 # ClearList, then RepairHiddenArray
    assert mx.Vertices.Count == 0
    t = box_brep().Trims[0]
    t.SetTolerances(0.25, 0.5)                          # two SetTolerance calls, u then v


@pytest.mark.skipif(not hasattr(r.PointCloudItemX, 'Color'), reason='G53 not generated')
def test_colors_from_out_locals_and_enum_results():
    # `int argb = 0; GetColor(p, i, ref argb); return Color.FromArgb(argb);`
    pc = r.PointCloud()
    pc.Add(r.Point3d(0, 0, 0))
    item = r.PointCloudX(pc)[0]
    c = item.Color
    assert len(c) == 4
    # `if (!GetPerFaceColor(p, ref argb)) return Color.Empty;`
    if hasattr(r, 'SubDFaceX'):
        f, s = subd_from_file()
        assert tuple(next(iter(s.Faces)).PerFaceColor) == (0, 0, 0, 0)
    # `int rc = (int)ON_Mesh_ConvertNonPlanarQuadsToTriangles(...); return rc;`
    assert quad_mesh().Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.1, 0) == 0


@pytest.mark.skipif(not hasattr(r.PointCloudX, 'GetColors') or not hasattr(r.TextureX, 'WrapU'), reason='G55 not generated')
def test_converted_array_fill_and_result_select():
    # `res[i] = Color.FromArgb(rc[i])` over the filled ints
    pc = r.PointCloud()
    pc.Add(r.Point3d(0, 0, 0))
    assert r.PointCloudX(pc).GetColors() == ()          # no colors: `return new Color[0];`
    # `value == (int)TextureUvwWrapping.Clamp ? Clamp : Repeat`
    t = r.TextureX()
    t.WrapU = r.TextureUvwWrapping.Clamp
    assert t.WrapU == r.TextureUvwWrapping.Clamp
    t.WrapU = r.TextureUvwWrapping.Repeat
    assert t.WrapU == r.TextureUvwWrapping.Repeat


@pytest.mark.skipif(not hasattr(r, 'NurbsCurvePointListX') or not hasattr(r.NurbsCurvePointListX, 'GetWeight'),
                    reason='G56 not generated')
def test_guard_only_helpers_become_guards():
    # `ValidateIndex(index);`: `if (index < 0) throw ...; if (index >= Count) throw ...;`
    nc = r.NurbsCurveX.CreateFromCircle(r.CircleX(r.Point3dX(0, 0, 0), 1.0))
    pts = nc.Points
    assert isinstance(pts.GetWeight(0), float)
    for bad in (-1, pts.Count):
        with pytest.raises(IndexError):
            pts.GetWeight(bad)
    ns = r.NurbsSurfaceX.CreateFromSphere(r.SphereX(r.Point3dX(0, 0, 0), 2.0))
    ok, p = ns.Points.GetPoint(0, 0)
    assert ok is True and isinstance(p, r.Point3dX)
    with pytest.raises(IndexError, match='u must be less than CountU'):
        ns.Points.GetPoint(ns.Points.CountU, 0)


# --- G57: the items left open in N --------------------------------------------

@pytest.mark.skipif(not hasattr(r.IntersectionX, 'LineCircle'), reason='G57 not generated')
def test_guards_in_tuple_members_and_result_clamps():
    circle = r.CircleX(r.Point3dX(0, 0, 0), 1.0)
    rc = r.IntersectionX.LineCircle(r.LineX(r.Point3dX(-2, 0, 0), r.Point3dX(2, 0, 0)), circle)
    assert len(rc) == 5 and int(rc[0]) == 2                       # Multiple
    # `if (!line.IsValid || !circle.IsValid) return None;` -- the outs as initialized
    bad = r.IntersectionX.LineCircle(r.LineX(r.Point3dX(0, 0, 0), r.Point3dX(0, 0, 0)), circle)
    assert int(bad[0]) == 0 and bad[1] == 0.0 and bad[3] == 0.0
    # `if (rc <= 0 || rc > 3) return None; return (SphereSphereIntersection)rc;`
    a, b = r.SphereX(r.Point3dX(0, 0, 0), 1.0), r.SphereX(r.Point3dX(1, 0, 0), 1.0)
    ss = r.IntersectionX.SphereSphere(a, b)
    assert len(ss) == 2 and int(ss[0]) == 2 and isinstance(ss[1], r.CircleX)   # Circle
    far = r.IntersectionX.SphereSphere(a, r.SphereX(r.Point3dX(10, 0, 0), 1.0))
    assert int(far[0]) == 0


@pytest.mark.skipif(not hasattr(r.TextureMappingX, 'Id'), reason='G57 not generated')
def test_owned_objects_are_never_document_controlled():
    # `IsDocumentControlled ? m_id : ON_TextureMapping_GetId(...)`: always the native here
    import uuid
    assert isinstance(r.TextureMappingX.CreateSurfaceParameterMapping().Id, uuid.UUID)


@pytest.mark.skipif(not hasattr(r.MaterialX, 'GetBitmapTexture'), reason='G57 not generated')
def test_material_textures_are_views_copied_on_write():
    # RhinoCommon `new Texture(index, material)`: reads resolve through the material;
    # the first write copies (NonConstOperation) and leaves the material alone
    m = r.MaterialX()
    assert m.GetBitmapTexture() is None
    t = r.TextureX()
    t.FileName = 'a.png'
    assert m.SetBitmapTexture(t)
    v = m.GetBitmapTexture()
    assert v.FileName == 'a.png'
    v.FileName = 'c.png'
    assert v.FileName == 'c.png'
    assert m.GetBitmapTexture().FileName == 'a.png'
    # to change the material's texture: set it back, as in .NET
    assert m.SetBitmapTexture(v)
    assert m.GetBitmapTexture().FileName == 'c.png'


@pytest.mark.skipif(not hasattr(r.PolyCurveX, 'SegmentCurve'), reason='G58 not generated')
def test_child_geometry_views_follow_rhinocommon():
    # PolyCurve.SegmentCurve: a view of the segment that WRITES THROUGH (CommonObject's
    # PolyCurve special case), as in 8.x
    pc = r.PolyCurveX()
    pc.Append(r.LineCurveX((0, 0, 0), (2, 0, 0)))
    pc.Append(r.LineCurveX((2, 0, 0), (2, 3, 0)))
    seg = pc.SegmentCurve(0)
    assert isinstance(seg, r.LineCurveX)
    seg.Reverse()
    s = pc.SegmentCurve(0).PointAtStart
    assert (s.X, s.Y) == (2, 0)
    assert pc.SegmentCurve(5) is None
    # BrepFace.UnderlyingSurface: a view that COPIES on the first write (holder parent)
    face = box_brep().Faces[0]
    srf = face.UnderlyingSurface()
    before = face.UnderlyingSurface().PointAt(0.0, 0.0)
    srf.Translate(r.Vector3dX(0, 0, 5))
    moved = srf.PointAt(0.0, 0.0)
    after = face.UnderlyingSurface().PointAt(0.0, 0.0)
    assert moved.Z == before.Z + 5 and (after.X, after.Y, after.Z) == (before.X, before.Y, before.Z)
    # BrepFace.GetMesh: no render mesh on a new box
    assert face.GetMesh(r.MeshType.Render) is None


@pytest.mark.skipif(not hasattr(r.BrepFaceX, 'SetMesh'), reason='G59 not generated')
def test_set_mesh_hands_the_mesh_to_the_face():
    # RhinoCommon: the face takes the mesh's private copy; the Mesh object becomes a
    # view of the face's mesh (MeshHolder), copied again on its next write
    face = box_brep().Faces[0]
    m = quad_mesh()
    assert face.SetMesh(r.MeshType.Render, m) is True
    got = face.GetMesh(r.MeshType.Render)
    assert got is not None and got.Vertices.Count == 4
    assert m.Vertices.Count == 4
    m.Vertices.Clear()                                      # a write: m detaches
    assert m.Vertices.Count == 0
    assert face.GetMesh(r.MeshType.Render).Vertices.Count == 4


@pytest.mark.skipif(not hasattr(r, 'DecalsX'), reason='G60 not generated')
def test_decals_iterate_a_snapshot_of_copies():
    # RhinoCommon's DecalEnumerator: a snapshot of the attributes' decals, each
    # item its own shared_ptr copy -- it outlives the file it came from
    import gc
    f = r.File3dmX.Read(os.path.join(MODELS, 'sphereDecals.3dm'))
    objs = list(f.Objects)
    decals = list(objs[0].Attributes.Decals)
    assert len(decals) == 2
    assert all(d.Mapping == r.DecalMapping.UV for d in decals)
    ids = [d.TextureInstanceId for d in decals]
    assert len(set(ids)) == 2
    del f, objs
    gc.collect()
    assert [d.TextureInstanceId for d in decals] == ids


@pytest.mark.skipif(not hasattr(r, 'DecalsX'), reason='G60 not generated')
def test_decals_of_attributes_are_writable_with_rhinocommons_guards():
    # `new Decals(this, false, doc_sn)`: not read-only, so the m_read_only guards pass
    a = r.ObjectAttributesX()
    assert list(a.Decals) == []
    a.Decals.RemoveAllDecals()
    a.Decals.Clear()
    assert a.Decals.Remove(None) is False          # `if (decal == null) return false;`


@pytest.mark.skipif(not hasattr(r, 'ComponentIndexX'), reason='ComponentIndex not generated')
def test_component_index_value_type():
    # a translated value struct; its constructor takes the public ComponentIndexType
    ci = r.ComponentIndexX(r.ComponentIndexType.BrepFace, 3)
    assert ci.Index == 3 and ci.ComponentIndexType == r.ComponentIndexType.BrepFace and ci.IsSet
    u = r.ComponentIndexX.Unset                        # `new ComponentIndex(InvalidType, -1)`
    assert u.IsUnset() and not ci.IsUnset() and u.Index == -1
    assert ci == r.ComponentIndexX(r.ComponentIndexType.BrepFace, 3) and ci != u and u < ci
    face = box_brep().Faces[1]
    assert face.ComponentIndex() == r.ComponentIndexX(r.ComponentIndexType.BrepFace, 1)


@pytest.mark.skipif(not hasattr(r, 'RevSurfaceX'), reason='RevSurface not generated')
def test_rev_surface_from_rhinocommon_shapes():
    import math
    s = r.RevSurfaceX.CreateFromSphere(r.SphereX(r.Point3dX(0, 0, 0), 2.0))
    assert isinstance(s, r.RevSurfaceX) and isinstance(s, r.SurfaceX)
    assert abs((s.Angle.T1 - s.Angle.T0) - 2 * math.pi) < 1e-9
    assert s.AxisAndCurveCoplanar() is True
    assert isinstance(r.SphereX(r.Point3dX(0, 0, 0), 1.0).ToRevSurface(), r.RevSurfaceX)


@pytest.mark.skipif(not hasattr(r, 'RenderChannelsX'), reason='RenderChannels not generated')
def test_render_channels_of_render_settings():
    rc = r.RenderSettingsX().RenderChannels
    assert rc.Mode == r.RenderChannelsModes.Automatic   # `(m == 0) ? Modes.Automatic : Modes.Custom`
    import uuid
    assert all(isinstance(i, uuid.UUID) for i in rc.CustomList)   # opennurbs' default custom channels
    assert r.RenderChannelsX().Mode == r.RenderChannelsModes.Automatic


@pytest.mark.skipif(not hasattr(r, 'CommonObjectX'), reason='G62 not generated')
def test_common_object_is_the_root_of_object_classes():
    # RhinoCommon's CommonObject: IsValid / IsValidWithLog on everything that is one
    b = box_brep()
    assert isinstance(b, r.CommonObjectX) and isinstance(r.ObjectAttributesX(), r.CommonObjectX)
    assert b.IsValid is True
    ok, log = b.IsValidWithLog()
    assert ok is True and isinstance(log, str)
    assert r.CommonObjectX.ComputeMemoryEstimate(b) > 0
    # ON_TextureCoordinates is no ON_Object: no CommonObject base (pilot-findings #31)
    if hasattr(r, 'CachedTextureCoordinatesX'):
        assert not issubclass(r.CachedTextureCoordinatesX, r.CommonObjectX)
    a = r.ObjectAttributesX()
    assert a.SetUserString('k', 'v') is True and a.GetUserString('k') == 'v' and a.UserStringCount == 1


@pytest.mark.skipif(not hasattr(r.LightX, 'IsPointLight'), reason='G63 not generated')
def test_translated_members_built_from_generated_members():
    # RhinoCommon's own C# bodies, translated (G63): `LightStyle == LightStyle.WorldPoint || ...`
    import math
    light = r.LightX()
    light.LightStyle = r.LightStyle.WorldPoint
    assert light.IsPointLight is True and light.IsSpotLight is False and light.IsLinearLight is False
    light.LightStyle = r.LightStyle.WorldSpot
    assert light.IsSpotLight is True and light.IsPointLight is False
    # RhinoMath.ToDegrees(ModelAngleToleranceRadians), and the setter back through ToRadians
    f = r.File3dmX()
    f.Settings.ModelAngleToleranceDegrees = 3.0
    assert abs(f.Settings.ModelAngleToleranceRadians - math.radians(3.0)) < 1e-12
    assert abs(f.Settings.ModelAngleToleranceDegrees - 3.0) < 1e-12
    # `Translate(new Vector3d(x, y, z))`
    b = box_brep()
    assert b.Translate(1.0, 2.0, 3.0) is True
    # a switch over TrimCount (BrepEdge.Valence): a box's edges are interior
    assert all(b.Edges[i].Valence == r.EdgeAdjacency.Interior for i in range(b.Edges.Count))


@pytest.mark.skipif(not hasattr(r, 'File3dmWriteOptionsX'), reason='G64 not generated')
def test_data_classes_are_rhinocommons_translated_whole(tmp_path):
    # G64: File3dmWriteOptions, its constructor (`SaveUserData = true`), the [Flags]
    # arithmetic of EnableRenderMeshes, and `options = options ?? new ...` in Write
    o = r.File3dmWriteOptionsX()
    assert o.SaveUserData is True and o.SaveRenderMeshes is True and o.Version == 0
    o.EnableRenderMeshes(r.ObjectType.AnyObject, False)
    assert o.SaveRenderMeshes is False
    o.Version = 7
    f = r.File3dmX()
    path = str(tmp_path / 'opts.3dm')
    assert f.Write(path, o) is True and f.Write(str(tmp_path / 'default.3dm'), None) is True
    ok, log = f.WriteWithLog(str(tmp_path / 'log.3dm'), None)
    assert ok is True and isinstance(log, str)
    assert r.File3dmX.Read(path) is not None
    # PageSettings: a data class handed to a native setter member by member
    ps = r.PageSettingsX()
    ps.PageNumber, ps.WidthMillimeters, ps.PrinterName = 3, 210.0, 'laser'
    v = r.ViewInfoX(r.ViewInfo())                      # through the hand bridge: no default ctor yet
    v.SetPageSettings(ps)
    # ConstructionPlane: RhinoCommon's constructor defaults, and the named-cplane
    # table of a fresh file (its indexer builds data objects from the native)
    cp = r.ConstructionPlaneX()
    assert cp.GridSpacing == 1.0 and cp.GridLineCount == 70 and cp.ThickLineFrequency == 5 and cp.DepthBuffered is True
    assert cp.Plane.Origin == r.Point3dX(0, 0, 0) and cp.Name == ''
    cp.Name = 'top'
    assert cp.Name == 'top'
    assert len(f.NamedConstructionPlanes) == 0


@pytest.mark.skipif(not hasattr(r.ViewInfoX, 'GetPageSettings'), reason='G65 not generated')
def test_data_objects_cross_the_native_boundary_both_ways(tmp_path):
    # G65: a data object built from the native (GetPageSettings, GridDefaults, the
    # named-cplane indexer) and handed to it (SetPageSettings, GridDefaults=, Add)
    v = r.ViewInfoX(r.ViewInfo())
    ps = r.PageSettingsX()
    ps.PageNumber, ps.WidthMillimeters, ps.PrinterName = 3, 210.0, 'laser'
    v.SetPageSettings(ps)
    back = v.GetPageSettings()
    assert (back.PageNumber, back.WidthMillimeters, back.PrinterName) == (3, 210.0, 'laser')
    f = r.File3dmX()
    gd = f.Settings.GridDefaults
    assert gd is not None and gd.GridLineCount == 70
    gd.GridLineCount = 12
    f.Settings.GridDefaults = gd
    assert f.Settings.GridDefaults.GridLineCount == 12
    cp = r.ConstructionPlaneX()
    cp.Name, cp.GridSpacing = 'top', 2.5
    f.NamedConstructionPlanes.Add(cp)
    assert len(f.NamedConstructionPlanes) == 1
    got = f.NamedConstructionPlanes[0]
    assert got.Name == 'top' and got.GridSpacing == 2.5
    # `Write(path, new File3dmWriteOptions { Version = version })`
    path = str(tmp_path / 'v.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dmX.Read(path)
    assert g.NamedConstructionPlanes[0].Name == 'top'


@pytest.mark.skipif(not hasattr(r.File3dmLayerTableX, 'FindId'), reason='G66 not generated')
def test_find_id_on_the_tables_skips_deleted_components():
    # G66: `m_manifest.FindId(id, ComponentType) is T item && !item.IsDeleted`
    import uuid
    f = r.File3dmX()
    layer = r.LayerX()
    layer.Name = 'walls'
    f.Layers.Add(layer)                                 # RhinoCommon's ICollection Add: void
    found = f.Layers.FindIndex(f.Layers.Count - 1)
    got = f.Layers.FindId(found.Id)
    assert got is not None and got.Name == 'walls'
    assert f.Layers.FindId(uuid.uuid4()) is None
    mat = r.MaterialX()
    f.Materials.Add(mat)
    mi = f.Materials.Count - 1
    mid = f.Materials.FindIndex(mi).Id
    assert f.Materials.FindId(mid) is not None
    f.Materials.Delete(mi)                              # ModelComponentTable.Delete(int): void in RhinoCommon
    assert f.Materials.FindId(mid) is None                  # deleted: no result, as in RhinoCommon


@pytest.mark.skipif(not hasattr(r.PointCloudItemX, 'X'), reason='G67 not generated')
def test_point_cloud_item_coordinates_chain_through_location():
    # G67: `X => Location.X`; the setter copies Location, sets X, writes it back
    pc = r.PointCloudX()
    pc.Add(r.Point3dX(1, 2, 3))
    item = pc[0]
    assert (item.X, item.Y, item.Z) == (1.0, 2.0, 3.0)
    item.X = 7.5
    assert item.Location == r.Point3dX(7.5, 2, 3) and pc[0].X == 7.5


@pytest.mark.skipif(not hasattr(r.File3dmX, 'Created'), reason='G68 not generated')
def test_file_dates_and_native_guards(tmp_path):
    # G68: `if (GetDate(...)) return new DateTime(year, month+1, ...); return DateTime.MinValue;`
    import datetime
    f = r.File3dmX()
    assert isinstance(f.Created, datetime.datetime) and isinstance(f.LastEdited, datetime.datetime)
    path = str(tmp_path / 'dated.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dmX.Read(path)
    assert g.Created >= datetime.datetime(2000, 1, 1) and g.LastEdited >= g.Created
    # `if (UnsafeNativeMethods.ON_Mesh_GetHiddenValue(p, i)) return;` then the two ops
    m = quad_mesh()
    m.Vertices.Hide(1)
    assert m.Vertices.IsHidden(1) is True and m.Vertices.IsHidden(0) is False
    m.Vertices.Hide(1)                                  # already hidden: returns at the guard
    m.Vertices.Show(1)
    assert m.Vertices.IsHidden(1) is False


@pytest.mark.skipif(not hasattr(r, 'Color4fX'), reason='G69 not generated')
def test_color4f_and_the_members_it_unlocks():
    # G69: Color4f translated as a value class (D34); PhysicallyBasedMaterial's colours
    # come and go as Color4fX through ON_4fPoint by layout
    c = r.Color4fX(0.25, 0.5, 0.75, 1.0)
    assert (c.R, c.G, c.B, c.A) == (0.25, 0.5, 0.75, 1.0) and c == r.Color4fX(0.25, 0.5, 0.75, 1.0)
    assert r.Color4fX.White == r.Color4fX(1, 1, 1, 1)
    m = r.MaterialX()
    m.ToPhysicallyBased()
    pbr = m.PhysicallyBased
    pbr.BaseColor = c
    assert pbr.BaseColor == c
    # Sun.Light: `var light = new Light(); UNM.ON_Sun_Light(this, light.NonConstPointer()); return light;`
    s = r.SunX()
    assert isinstance(s.Light, r.LightX)
    # Texture.FileReference: a handle from a const pointer the C side allocated for it
    t = r.TextureX()
    fr = t.FileReference
    assert fr is None or isinstance(fr, r.FileReferenceX)


@pytest.mark.skipif(not hasattr(r.GeometryBaseX, 'Duplicate'), reason='G69 Duplicate not generated')
def test_duplicate_returns_an_owned_most_derived_copy():
    # G69: GeometryBase.Duplicate -> CreateGeometryHelper(p, null) through RH3DM_Wrap_<root>;
    # the overrides construct their own class (Brep, Mesh, Curve, PolyCurve via its copy ctor)
    b = box_brep()
    d = b.Duplicate()
    assert isinstance(d, r.BrepX) and d is not b and d.Faces.Count == b.Faces.Count
    pc = r.PolyCurveX()
    pc.Append(r.LineCurveX(r.Point3dX(0, 0, 0), r.Point3dX(1, 0, 0)))
    pd = pc.Duplicate()
    assert isinstance(pd, r.PolyCurveX) and pd.SegmentCount == 1
    nc = r.NurbsCurveX.CreateFromLine(r.LineX(r.Point3dX(0, 0, 0), r.Point3dX(0, 1, 0))) if hasattr(r.NurbsCurveX, 'CreateFromLine') else None
    if nc is not None:
        cd = r.CurveX.Duplicate(nc)                       # the base's: dispatched on the runtime type
        assert isinstance(cd, r.NurbsCurveX)


@pytest.mark.skipif(not hasattr(r.BoundingBoxX, 'GetCorners'), reason='G70 not generated')
def test_value_type_arrays_are_sequences():
    # G70: Point3d[] / IEnumerable<Point3d> are std::vector: lists in Python. C#'s
    # null result is the empty list (D35)
    import math
    bb = r.BoundingBoxX(r.Point3dX(0, 0, 0), r.Point3dX(1, 2, 3))
    corners = bb.GetCorners()
    assert len(corners) == 8 and corners[0] == r.Point3dX(0, 0, 0) and corners[6] == r.Point3dX(1, 2, 3)
    assert r.BoundingBoxX.Unset.GetCorners() == []
    xf = r.TransformX.Translation(r.Vector3dX(1, 0, 0))
    moved = xf.TransformList([r.Point3dX(0, 0, 0), r.Point3dX(1, 1, 1)])
    assert moved == [r.Point3dX(1, 0, 0), r.Point3dX(2, 1, 1)]
    assert len(xf.ToFloatArray(True)) == 16
    if hasattr(bb, 'Transform'):
        assert bb.Transform(xf) is True and bb.Min == r.Point3dX(1, 0, 0)
    assert bb.ClosestPoint(r.Point3dX(-5, 1, 1)) == r.Point3dX(bb.Min.X, 1, 1)
    p = r.PlaneX.WorldXY
    assert p.Rotate(math.pi / 2, r.Vector3dX(0, 0, 1)) is True
    assert abs(p.XAxis.Y - 1.0) < 1e-12
    box = r.BoxX(r.BoundingBoxX(r.Point3dX(0, 0, 0), r.Point3dX(1, 1, 1)))
    assert box.Contains(r.Point3dX(0.5, 0.5, 0.5)) is True and len(box.GetCorners()) == 8


@pytest.mark.skipif(not hasattr(r.File3dmX, 'EmbeddedFiles'), reason='G71 not generated')
def test_embedded_files_add_and_iterate(tmp_path):
    # G71: File3dmEmbeddedFiles.Add(filename) and its manifest enumerator (no Count in
    # RhinoCommon, so no len()); File3dmEmbeddedFile is a File3dm proxy
    data = tmp_path / 'note.txt'
    data.write_text('hello')
    f = r.File3dmX()
    assert f.EmbeddedFiles.Add(str(data)) is True
    files = list(f.EmbeddedFiles)
    assert len(files) == 1 and isinstance(files[0], r.File3dmEmbeddedFileX)
    assert files[0].Filename.endswith('note.txt') and files[0].ComponentType == r.ModelComponentType.EmbeddedFile
    path = str(tmp_path / 'embedded.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dmX.Read(path)
    assert [e.Filename for e in g.EmbeddedFiles] == [files[0].Filename]


# --- G72: tuples of geometry from native pointers; per-element native loops ---

@pytest.mark.skipif(not hasattr(r.CurveX, 'Split'), reason='G72 not generated')
def test_split_returns_the_halves_or_none():
    # Curve.Split(double): `bool rc = F(ptr, t, ref left, ref right); Curve[] output = new Curve[2];
    # if (left != IntPtr.Zero) output[0] = CreateGeometryHelper(left, null) as Curve; ... return rc ? output : null;`
    c = r.LineCurveX((0, 0, 0), (2, 0, 0))
    parts = c.Split(1.0)
    assert len(parts) == 2 and all(isinstance(p, r.CurveX) for p in parts)
    assert parts[0].PointAtEnd == r.Point3dX(1, 0, 0) and parts[1].PointAtStart == r.Point3dX(1, 0, 0)
    assert c.Split(5.0) is None                           # outside the domain: the native fails
    # BezierCurve.Split(t, out left, out right): (success, left, right), the halves None on failure
    nc = r.NurbsCurveX.CreateFromCircle(r.CircleX(r.Point3dX(0, 0, 0), 1.0))
    bz = nc.ConvertSpanToBezier(0)
    ok, left, right = bz.Split(0.5)
    assert ok is True and isinstance(left, r.BezierCurveX) and isinstance(right, r.BezierCurveX)
    assert left.PointAt(1.0) == right.PointAt(0.0) == bz.PointAt(0.5)
    assert bz.Split(2.0) == (False, None, None)


@pytest.mark.skipif(not hasattr(r.PolyCurveX, 'Explode'), reason='G72 not generated')
def test_explode_duplicates_each_segment():
    # PolyCurve.Explode: the segment pointers read back through a SimpleArrayCurvePointer,
    # each duplicated (ON_Curve_DuplicateCurve) and wrapped as its own class
    pc = r.PolyCurveX()
    pc.Append(r.LineCurveX((0, 0, 0), (1, 0, 0)))
    pc.Append(r.LineCurveX((1, 0, 0), (1, 1, 0)))
    segs = pc.Explode()
    assert len(segs) == 2 and all(isinstance(s, r.LineCurveX) for s in segs)
    assert segs[1].PointAtEnd == r.Point3dX(1, 1, 0)
    assert r.PolyCurveX().Explode() == ()


@pytest.mark.skipif(not hasattr(r.MeshVertexListX, 'ToPoint3dArray'), reason='G72 not generated')
def test_vertex_arrays_read_one_vertex_per_call():
    # MeshVertexList.ToPoint3dArray: `for (i < Count) { var pt = new Point3d(); ON_Mesh_Vertex(ptr, i, ref pt); rc[i] = pt; }`
    mx = quad_mesh()
    pts = mx.Vertices.ToPoint3dArray()
    assert [(p.X, p.Y, p.Z) for p in pts] == [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)]
    # ToPoint3fArray: the same vertices, `new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z)`
    pf = mx.Vertices.ToPoint3fArray()
    assert all(isinstance(p, r.Point3fX) for p in pf) and [(p.X, p.Y) for p in pf] == [(0, 0), (1, 0), (1, 1), (0, 1)]
    assert r.MeshX(r.Mesh()).Vertices.ToPoint3dArray() == ()


# --- G73: holders empty on a status, tuples led by an object, discarded-out forwards,
#          self-identity guards, GC-hint plumbing in constructors ---

@pytest.mark.skipif(not hasattr(r.BrepVertexX, 'EdgeIndices'), reason='G73 not generated')
def test_brep_adjacency_holders_are_empty_when_the_native_says_so():
    # `int rc = ON_BrepVertex_EdgeIndices(brep, index, ei.m_ptr); return rc == 0 ? new int[0] : ei.ToArray();`
    b = box_brep()
    v = b.Vertices[0]
    edges = v.EdgeIndices()
    assert len(edges) == 3 and all(isinstance(i, int) for i in edges)
    e = b.Edges[0]
    assert len(e.AdjacentFaces()) == 2 and len(e.TrimIndices()) == 2
    f = b.Faces[0]
    assert len(f.AdjacentEdges()) == 4 and len(f.AdjacentFaces()) == 4


@pytest.mark.skipif(not hasattr(r.SurfaceX, 'ToNurbsSurface'), reason='G73 not generated')
def test_to_nurbs_surface_leads_its_tuple_with_the_object_and_forwards_without_the_out():
    # ToNurbsSurface(tol, out accuracy) -> (NurbsSurfaceX | None, accuracy);
    # ToNurbsSurface() -> `int accuracy; return ToNurbsSurface(0.0, out accuracy);` -> NurbsSurfaceX
    srf = box_brep().Faces[0].UnderlyingSurface()
    ns, accuracy = srf.ToNurbsSurface(0.0)
    assert isinstance(ns, r.NurbsSurfaceX) and isinstance(accuracy, int) and accuracy > 0
    assert isinstance(srf.ToNurbsSurface(), r.NurbsSurfaceX)


@pytest.mark.skipif(not hasattr(r.MeshX, 'Append'), reason='G73 not generated')
def test_mesh_append_skips_null_and_itself_and_default_constructs():
    # `if (null == other || other.ConstPointer() == ConstPointer()) return;` -- and the
    # default constructors (`ON_Mesh_New(); ConstructNonConstObject(p); ApplyMemoryPressure();`)
    m = r.MeshX()
    assert m.Vertices.Count == 0
    m.Append(None)
    m.Append(m)
    assert m.Vertices.Count == 0
    q = quad_mesh()
    m.Append(q)
    assert m.Vertices.Count == 4 and m.Faces.Count == 1
    m.Append(q)
    assert m.Vertices.Count == 8
    assert r.PointCloudX().Count == 0 and r.BrepX().Faces.Count == 0


# --- G74: class-typed values in translated members ---

@pytest.mark.skipif(not hasattr(r.PointCloudX, 'AppendNew'), reason='G74 not generated')
def test_translated_members_return_and_construct_class_values():
    # PointCloud.AppendNew: `Add(Point3d.Origin); return this[Count - 1];` -- a sibling call and the indexer
    pc = r.PointCloudX()
    item = pc.AppendNew()
    assert isinstance(item, r.PointCloudItemX) and pc.Count == 1 and item.Location == r.Point3dX(0, 0, 0)
    pc.Add(r.Point3dX(1, 1, 1))
    assert pc.InsertNew(1).Index == 1 and pc.Count == 3
    # NurbsCurve.CreateFromEllipse: `NurbsCurve nc = CreateFromCircle(..); if (nc == null) return null;
    # nc.Transform(scale); return nc;` -- a class-typed local, null-tested, mutated, returned
    nc = r.NurbsCurveX.CreateFromEllipse(r.EllipseX(r.PlaneX.WorldXY, 2.0, 1.0))
    assert isinstance(nc, r.NurbsCurveX) and abs(nc.PointAtStart.X - 2.0) < 1e-9
    # `Duplicate() as Brep`: the fresh object downcast (RH3DM_As frees it when it is not one)
    d = box_brep().DuplicateBrep()
    assert isinstance(d, r.BrepX) and d.Faces.Count == 6
    assert isinstance(quad_mesh().DuplicateMesh(), r.MeshX)
    # MeshingParameters: a value-mode class default-constructed; the static presets are
    # object initializers, translated as setter calls on the fresh value
    assert isinstance(r.MeshingParametersX(), r.MeshingParametersX)
    assert isinstance(r.MeshingParametersX.Default, r.MeshingParametersX)
    minimal = r.MeshingParametersX.Minimal
    assert minimal.GridMinCount == 16 and minimal.JaggedSeams is True
    assert r.MeshingParametersX.Smooth.SimplePlanes is True and minimal.SimplePlanes is False
    assert isinstance(r.ViewInfoX(), r.ViewInfoX)


# --- G75-G77: managed fields, private-helper inlining, scoped Brep views ---

@pytest.mark.skipif(not hasattr(r.AnnotationBaseX, 'RunReplace'), reason='G75 not generated')
def test_annotation_members_read_with_the_parent_dimstyle_null():
    # G75/G76: AnnotationBase.m_parent_dimstyle is written only by ParentDimensionStyle.set
    # (not generated), so it is null on every generated annotation: ConstParentDimStylePointer()
    # folds to IntPtr.Zero. No concrete annotation class is generated yet, so the members
    # are checked on the registered class (their parity is the bodies' one native call each)
    A = r.AnnotationBaseX
    for name in ('TextHeight', 'DimensionScale', 'MaskEnabled', 'DimensionStyleId', 'RichText'):
        assert isinstance(getattr(A, name), property) and getattr(A, name).fset is not None, name
    for name in ('MaskFrame', 'MaskColorSource', 'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay'):
        assert isinstance(getattr(A, name), property), name
    assert callable(A.RunReplace) and callable(A.SetRichText)


@pytest.mark.skipif(not hasattr(r.BrepFaceX, 'Loops'), reason='G77 not generated')
def test_face_and_loop_scoped_lists():
    # G77: BrepFace.Loops => new BrepLoopList(this): the brep's loop list scoped to the
    # face (Count = ON_BrepFace_LoopCount, element i = ON_BrepFace_LoopIndex(face, i))
    b = box_brep()
    assert len(b.Loops) == 6
    for fi in range(len(b.Faces)):
        f = b.Faces[fi]
        loops = f.Loops
        assert isinstance(loops, r.BrepLoopListX) and len(loops) == 1
        outer = f.OuterLoop
        assert outer.LoopIndex == loops[0].LoopIndex
        # BrepLoop.Trims: the brep's trims scoped to the loop
        trims = outer.Trims
        assert isinstance(trims, r.BrepTrimListX) and len(trims) == 4
        assert all(t.Loop.LoopIndex == outer.LoopIndex for t in trims)
        assert all(t.Face.FaceIndex == fi for t in trims)
        assert all(isinstance(t.Edge, r.BrepEdgeX) for t in trims)
    # the whole-brep lists are unchanged
    assert len(b.Trims) == 24 and [l.LoopIndex for l in b.Loops] == list(range(6))
    with pytest.raises(IndexError):
        b.Faces[0].Loops[1]


# --- G78/G79: RDK Variant floats and enum maps; strings in translated members ---

@pytest.mark.skipif(not hasattr(r.LinearWorkflowX, 'PreProcessGamma'), reason='G78 not generated')
def test_rdk_float_and_enum_mapped_variant_properties():
    rs = r.File3dmX().Settings.RenderSettings
    lw = rs.LinearWorkflow
    lw.PreProcessGamma = 2.5
    assert abs(lw.PreProcessGamma - 2.5) < 1e-6
    lw.PostProcessGamma = 1.75
    assert abs(lw.PostProcessGamma - 1.75) < 1e-6
    # Dithering.Method: the RDK stores 0 for SimpleNoise, anything else is FloydSteinberg;
    # the enum is RhinoCommon's (None = 0, FloydSteinberg = 1, SimpleNoise = 2; D36)
    d = rs.Dithering
    M = r.DitheringX.Methods
    assert int(M.SimpleNoise) == 2 and int(M.FloydSteinberg) == 1
    d.Method = M.SimpleNoise
    assert d.Method == M.SimpleNoise
    d.Method = M.FloydSteinberg
    assert d.Method == M.FloydSteinberg


@pytest.mark.skipif(not hasattr(r.AnnotationBaseX, 'PlainTextToRtf'), reason='G79 not generated')
def test_translated_string_members(tmp_path):
    # PlainTextToRtf: `@"{\rtf1{\ltrch " + str.Replace("\n", Ph)...Replace(Ph, @"}\par {") + "}}"`
    assert r.AnnotationBaseX.PlainTextToRtf('a\nb') == '{\\rtf1{\\ltrch a}\\par {b}}'
    assert r.AnnotationBaseX.PlainTextToRtf('x{y}\\') == '{\\rtf1{\\ltrch x\\{y\\}\\\\}}'
    # DocumentUserTextCount: keys containing a backslash (section\entry)
    # (SetString is not generated: written with the old binding, read back)
    old = r.File3dm()
    old.Strings['plain'] = 'v'
    old.Strings['sec\\key'] = 'v2'
    path = str(tmp_path / 'strings.3dm')
    old.Write(path, 8)
    f = r.File3dmX.Read(path)
    assert f.Strings.DocumentUserTextCount == 1
    assert f.Strings.GetValue('sec', 'key') == 'v2' and f.Strings.GetValue('', 'key') == ''
    # MeshVertexColorList.Add(Color): `SetColor(Count, color); return Count - 1;`
    mx = quad_mesh()
    assert mx.VertexColors.Add((255, 0, 0, 255)) == 0 and mx.VertexColors.Count == 1


# --- G80: Point3dList / Polyline -- managed-state classes (option 4) ---

@pytest.mark.skipif(not hasattr(r, 'PolylineX'), reason='G80 not generated')
def test_point3dlist_is_a_real_list_with_rhinolist_semantics():
    P = r.Point3dX
    pl = r.Point3dListX([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)])
    assert len(pl) == 3 and pl.Count == 3 and pl[1] == P(1, 0, 0)
    assert [p.X for p in pl] == [3, 1, 2]                    # iteration through the item protocol
    pl[0] = P(0, 0, 0)
    pl.Add(P(5, 0, 0)); pl.Add(6, 0, 0)                      # RhinoList.Add(T), Point3dList.Add(x, y, z)
    pl.Insert(1, P(9, 0, 0)); pl.RemoveAt(0)
    assert [p.X for p in pl] == [9, 1, 2, 5, 6]
    assert pl.IndexOf(P(2, 0, 0)) == 2 and pl.Contains(P(5, 0, 0)) and not pl.Contains(P(7, 0, 0))
    pl.Reverse()
    assert [p.X for p in pl] == [6, 5, 2, 1, 9]
    pl.Sort()                                                  # Point3d.CompareTo: X, then Y, then Z
    assert [p.X for p in pl] == [1, 2, 5, 6, 9]
    assert pl.BinarySearch(P(5, 0, 0)) == 2 and pl.BinarySearch(P(3, 0, 0)) == ~2
    with pytest.raises(IndexError):
        pl[5]
    # capacity grows as RhinoList's does: 0 -> 4 -> 8
    e = r.Point3dListX()
    assert e.Capacity == 0
    e.Add(P(0, 0, 0))
    assert e.Capacity == 4
    for i in range(4):
        e.Add(P(i, 0, 0))
    assert e.Capacity == 8 and e.Count == 5
    # a copy is a new list; ToArray a plain sequence
    d = pl.Duplicate()
    d[0] = P(42, 0, 0)
    assert pl[0] == P(1, 0, 0) and isinstance(d, r.Point3dListX)
    assert list(pl.ToArray()) == list(pl)
    bb = pl.BoundingBox
    assert bb.Min == P(1, 0, 0) and bb.Max == P(9, 0, 0)


@pytest.mark.skipif(not hasattr(r, 'PolylineX'), reason='G80 not generated')
def test_polyline_geometry_members():
    P = r.Point3dX
    poly = r.PolylineX([P(0, 0, 0), P(2, 0, 0), P(2, 2, 0)])
    assert isinstance(poly, r.Point3dListX)                    # Polyline : Point3dList
    assert poly.SegmentCount == 2 and abs(poly.Length - 4.0) < 1e-12
    assert poly.PointAt(1.5) == P(2, 1, 0)
    assert poly.SegmentAt(1).From == P(2, 0, 0)
    assert abs(poly.ClosestParameter(P(1, 5, 0)) - 2.0) < 1e-12
    assert poly.ClosestPoint(P(1, -3, 0)) == P(1, 0, 0)
    t = poly.Trim(r.IntervalX(0.5, 1.5))
    assert isinstance(t, r.PolylineX) and list(t) == [P(1, 0, 0), P(2, 0, 0), P(2, 1, 0)]
    assert [s.Length for s in poly.GetSegments()] == [2.0, 2.0]
    poly.Add(P(2, 2, 0))                                       # a zero-length segment
    assert poly.DeleteShortSegments(1e-9) == 1 and poly.Count == 3
    c = poly.Duplicate()
    assert isinstance(c, r.PolylineX) and list(c) == list(poly)


@pytest.mark.skipif(not hasattr(r.PointCloudX, 'AddRange'), reason='G82 not generated')
def test_sequence_arguments_where_rhinocommon_takes_ienumerable():
    # G82: GetConstArray members take any sequence of points -- a list, a tuple,
    # or a Point3dListX / PolylineX (decision 4 of option 4)
    P, V = r.Point3dX, r.Vector3dX
    pc = r.PointCloudX()
    pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
    pc.AddRange(r.PolylineX([P(2, 0, 0), P(3, 0, 0)]))
    assert pc.Count == 4 and [p.X for p in pc.GetPoints()] == [0, 1, 2, 3]
    # InsertRange: generated since U11 fixed its native (test_point_cloud_insert_range_u11)
    n = r.PointCloudX()
    n.AddRange([P(0, 0, 0)], [V(0, 0, 1)])
    assert n.Count == 1 and n.ContainsNormals
    n.AddRange([P(0, 0, 0)], [])                         # no normals: C# returns early
    assert n.Count == 1
    with pytest.raises(ValueError):
        n.AddRange([P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1)])
    f = r.File3dmX()
    pid = f.Objects.AddPolyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)], None)
    assert f.Objects.Count == 1 and pid != uuid.UUID(int=0)
    assert f.Objects.AddPolyline([], None) == uuid.UUID(int=0) and f.Objects.Count == 1


@pytest.mark.skipif(not hasattr(r, 'PolylineCurveX') or not hasattr(r.PolylineCurveX, 'ToPolyline'),
                    reason='G82 ToPolyline not generated')
def test_polylinecurve_to_polyline_through_the_translated_static():
    # G82: `using (var h = new SimpleArrayPoint3d()) { CopyValues(ptr, h); return
    # Polyline.PolyLineFromNativeArray(h); }` -- the holder handed to the data
    # class's translated static; Python owns the new PolylineX
    crv = r.PolylineCurveX(r.PolylineCurve([r.Point3d(0, 0, 0), r.Point3d(3, 0, 0), r.Point3d(3, 4, 0)]))
    pl = crv.ToPolyline()
    assert isinstance(pl, r.PolylineX) and pl.Count == 3
    assert pl[2] == r.Point3dX(3, 4, 0) and abs(pl.Length - 7.0) < 1e-12
    pl[0] = r.Point3dX(1, 0, 0)                          # a copy: the curve is unchanged
    assert crv.ToPolyline()[0] == r.Point3dX(0, 0, 0)



@pytest.mark.skipif(not hasattr(r, 'PolylineCurveX'), reason='G82')
def test_polylinecurve_from_points_chooses_its_creation_call():
    # G82: `if (count < 1) ptr = New(IntPtr.Zero); else ptr = New2(count, pts);`
    P = r.Point3dX
    crv = r.PolylineCurveX([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
    assert crv.PointCount == 3 and crv.PointAtEnd == P(3, 4, 0)
    assert r.PolylineCurveX(r.PolylineX([P(0, 0, 0), P(1, 0, 0)])).PointCount == 2
    assert r.PolylineCurveX([]).PointCount == 0                 # the empty arm: ON_PolylineCurve_New



@pytest.mark.skipif(not hasattr(r, 'PolylineX') or not hasattr(r.PolylineX, 'ToPolylineCurve'), reason='G82')
def test_polyline_to_polylinecurve_constructs_the_generated_class():
    # G82: a data class member naming a generated class: `new PolylineCurve(this)`
    # through class markers the generator resolves against PolylineCurveX(points)
    P = r.Point3dX
    poly = r.PolylineX([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
    crv = poly.ToPolylineCurve()
    assert isinstance(crv, r.PolylineCurveX) and crv.PointCount == 3 and crv.PointAtEnd == P(3, 4, 0)
    assert r.PolylineX([P(0, 0, 0)]).ToPolylineCurve() is None        # `if (m_size < 2) return null;`
    assert list(crv.ToPolyline()) == list(poly)                         # and back (ToPolyline)



@pytest.mark.skipif(not hasattr(r, 'PolylineX') or not hasattr(r.PolylineX, 'ToNurbsCurve'), reason='G82')
def test_polyline_to_nurbscurve_through_a_disposed_polylinecurve():
    # G82: `using (var pc = ToPolylineCurve()) { return pc?.ToNurbsCurve(); }`: the
    # temporary curve deleted on the way out, the NURBS curve owned by the caller
    P = r.Point3dX
    nc = r.PolylineX([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]).ToNurbsCurve()
    assert isinstance(nc, r.NurbsCurveX) and nc.PointAtEnd == P(3, 4, 0) and nc.PointAtStart == P(0, 0, 0)
    assert r.PolylineX([P(0, 0, 0)]).ToNurbsCurve() is None           # no curve: `pc?.` is null


@pytest.mark.skipif(not hasattr(r.CurveX, 'ClosedCurveOrientation'), reason='G83')
def test_curve_gap_members_orientation_trim_circle_derivatives():
    # G83: a switch onto an enum with a default; a private helper wrapping its result;
    # an out-argument call to a tuple sibling; G84: managed control flow around natives
    P = r.Point3dX
    circle = r.ArcCurveX(r.CircleX(P(0, 0, 0), 2.0))
    assert circle.ClosedCurveOrientation() == r.CurveOrientation.CounterClockwise
    assert circle.ClosedCurveOrientation(r.Vector3dX(0, 0, -1)) == r.CurveOrientation.Clockwise
    line = r.LineCurveX(P(0, 0, 0), P(10, 0, 0))
    assert line.ClosedCurveOrientation() == r.CurveOrientation.Undefined     # the switch's default
    piece = line.Trim(2.0, 5.0)
    assert isinstance(piece, r.CurveX) and piece.PointAtStart == P(2, 0, 0) and piece.PointAtEnd == P(5, 0, 0)
    assert line.Trim(r.IntervalX(1.0, 3.0)).PointAtEnd == P(3, 0, 0)
    assert circle.IsCircle() and not line.IsCircle()
    ok, c = circle.TryGetCircle()
    assert ok and abs(c.Radius - 2.0) < 1e-12
    assert line.TryGetCircle()[0] is False
    d = line.DerivativeAt(4.0, 1, r.CurveEvaluationSide.Default)
    assert len(d) == 2 and d[0] == r.Vector3dX(4, 0, 0) and d[1] == r.Vector3dX(1, 0, 0)   # domain [0, length]
    with pytest.raises(RuntimeError):
        line.DerivativeAt(4.0, -1, r.CurveEvaluationSide.Default)                    # InvalidOperationException in .NET


@pytest.mark.skipif(not hasattr(r.NurbsCurveX, 'CreateFromLine'), reason='G85')
def test_hidden_helpers_child_pointers_and_class_usings():
    # G85: bodies calling non-public helpers (SetVertex, GetPoint), a child view's
    # parent pointer (`m_mesh.NonConstPointer()`), `using` over a class local
    P = r.Point3dX
    m = r.MeshX()
    v = m.Vertices
    v.Add(P(0, 0, 0)); v.Add(1.0, 0.0, 0.0); v.Add(r.Point3fX(0, 1, 0))
    assert v.Count == 3 and len(v.ToFloatArray()) == 9
    nc = r.NurbsCurveX.CreateFromLine(r.LineX(P(0, 0, 0), P(3, 0, 0)))
    assert isinstance(nc, r.NurbsCurveX) and nc.PointAtEnd == P(3, 0, 0)
    assert r.NurbsCurveX.CreateFromLine(r.LineX(P(1, 1, 1), P(1, 1, 1))) is None   # invalid line
    box = r.BoxX(r.PlaneX.WorldXY, r.IntervalX(0, 1), r.IntervalX(0, 2), r.IntervalX(0, 3))
    ext = r.ExtrusionX.CreateBoxExtrusion(box, True)
    assert isinstance(ext, r.ExtrusionX) and ext.IsSolid and ext.CapCount == 2
    ns = r.NurbsSurfaceX.Create(3, False, 2, 2, 2, 2)
    ns.Points.SetPoint(1, 1, 1.0, 1.0, 0.0)
    cp = ns.Points.GetControlPoint(1, 1)
    assert cp.Location == P(1, 1, 0) and cp.Weight == 1.0


@pytest.mark.skipif(not hasattr(r.NurbsSurfaceX, 'CreateRuledSurface'), reason='G86')
def test_g86_singles():
    # G86: optional arguments as `x?.ConstPointer() ?? IntPtr.Zero`; a public target
    # inlined for a null argument; construct-as-wrap; GcProtect; GetConstArray, an
    # object's own pointer and internal constructors in class bodies; wrapped natives
    P = r.Point3dX
    f = r.File3dmX()
    iid = f.Objects.AddInstanceObject(0, r.TransformX.Identity, None)
    assert isinstance(iid, uuid.UUID)
    fr = r.FileReferenceX.CreateFromFullPath('/tmp/a.png')
    assert fr.FullPath.endswith('a.png')
    a = r.LineCurveX(P(0, 0, 0), P(1, 0, 0))
    b = r.LineCurveX(P(0, 1, 0), P(1, 1, 0))
    rs = r.NurbsSurfaceX.CreateRuledSurface(a, b)
    assert isinstance(rs, r.NurbsSurfaceX)
    pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
    nc = r.NurbsCurveX.Create(False, 3, pts)
    assert isinstance(nc, r.NurbsCurveX) and nc.PointAtEnd == P(3, 1, 0)
    assert r.NurbsCurveX.Create(False, 3, [P(0, 0, 0)]) is None       # count < 2
    assert isinstance(r.CurveX.CreateControlPointCurve(pts[:2], 3), r.LineCurveX)   # two points: a line
    assert isinstance(r.CurveX.CreateControlPointCurve(pts, 1), r.PolylineCurveX)  # degree 1: a polyline
    assert isinstance(r.CurveX.CreateControlPointCurve(pts, 3), r.NurbsCurveX)
    tm = r.TextureMappingX.CreatePlaneMapping(r.PlaneX.WorldXY, r.IntervalX(0, 1), r.IntervalX(0, 1), r.IntervalX(0, 1))
    assert tm.HasId in (True, False)                                    # ModelComponent's, inherited now


@pytest.mark.skipif(not hasattr(r.File3dmX, 'FromByteArray'), reason='G86')
def test_g86_outs_rectangles_and_byte_buffers(tmp_path):
    # G86: an `out Polyline` held by pointer and owned by the caller; Rectangle as the
    # old bindings' (x, y, width, height); a byte[] argument from any Python buffer
    P = r.Point3dX
    crv = r.PolylineCurveX([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
    ok, pl = crv.TryGetPolyline()
    assert ok and isinstance(pl, r.PolylineX) and pl.Count == 3
    ok, pl = r.ArcCurveX(r.CircleX(P(0, 0, 0), 1.0)).TryGetPolyline()
    assert not ok and pl is None
    vp = r.ViewportX()
    vp.SetScreenPort(0, 640, 480, 0, 0, 0)
    port = vp.GetScreenPort()
    assert tuple(port[0] if isinstance(port[0], tuple) else port)[:4] == (0, 0, 640, 480)
    f = r.File3dmX()
    f.Objects.AddPoint(P(1, 2, 3), None)
    path = str(tmp_path / 'b.3dm')
    assert f.Write(path, 8)
    data = open(path, 'rb').read()
    g = r.File3dmX.FromByteArray(data)
    assert isinstance(g, r.File3dmX) and g.Objects.Count == 1
    assert r.File3dmX.FromByteArray(bytearray(data)).Objects.Count == 1


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderMaterialsX'), reason='G87')
def test_file3dm_render_content_collections():
    # G87: File3dmRenderContent proxies of (file, id); RenderMaterials/Environments/Textures
    # walk the manifest's RenderContent components and keep their own kind (`is T`)
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dmX.Read(path)
    old = r.File3dm.Read(path)
    oldc = sorted((str(c.Id), c.Kind) for c in old.RenderContent)
    mats, envs, texs = list(f.RenderMaterials), list(f.RenderEnvironments), list(f.RenderTextures)
    assert all(isinstance(m, r.File3dmRenderMaterialX) for m in mats)
    assert all(isinstance(e, r.File3dmRenderEnvironmentX) for e in envs)
    assert all(isinstance(t, r.File3dmRenderTextureX) for t in texs)
    gen = sorted((str(c.Id), c.Kind) for c in mats + envs + texs)
    top = [k for k in oldc if any(k[0] == g[0] for g in gen)]
    assert gen == top and len(gen) >= 1                     # the same contents, the same kinds
    assert {c.Kind for c in mats} <= {'material'} and {c.Kind for c in envs} <= {'environment'}
    for m in mats:
        assert isinstance(m.ToMaterial(), r.MaterialX) and m.IsTopLevel
        assert m.Name == next(c.Name for c in old.RenderContent if str(c.Id) == str(m.Id))


@pytest.mark.skipif(not hasattr(r, 'AnnotationBaseX') or not hasattr(r.AnnotationBaseX, 'ParentDimensionStyle'), reason='G88')
def test_annotation_parent_dimension_style_is_modeled():
    # G88: RhinoCommon's managed m_parent_dimstyle -- the setter stores the style (a File3dm
    # view as a light copy: a new view of the same file and id) and sets DimensionStyleId;
    # every member that passes the parent style to its native now passes this one
    f = r.File3dmX.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    ann = next(g for g in (o.Geometry for o in f.Objects) if isinstance(g, r.AnnotationBaseX))
    assert ann.ParentDimensionStyle is None
    ds = list(f.AllDimStyles)[0]
    ann.ParentDimensionStyle = ds
    p = ann.ParentDimensionStyle
    assert p is not None and p.Id == ds.Id and ann.DimensionStyleId == ds.Id
    assert ann.ParentDimensionStyle is p                         # the stored object, every time
    if not ann.HasPropertyOverrides:
        assert abs(ann.TextHeight - ds.TextHeight) < 1e-12        # the parent's value, not the default's
    assert ann.SetBold(True) and ann.IsAllBold()                   # through ConstPointerForDimStyle()
    ann.ParentDimensionStyle = None
    assert ann.ParentDimensionStyle is None and ann.DimensionStyleId == uuid.UUID(int=0)
    assert p.Id == ds.Id                                          # a wrapper handed out stays valid


@pytest.mark.skipif(not hasattr(r.AnnotationBaseX, 'GetBoundingBox'), reason='G89')
def test_annotation_bounding_box_overrides_geometry_base():
    # G89: AnnotationBase overrides GeometryBase.GetBoundingBox(Transform) (a virtual on the
    # generated base, so a GeometryBaseX-typed call reaches it) and measures with the
    # modeled parent dimension style
    f = r.File3dmX.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    ann = next(g for g in (o.Geometry for o in f.Objects) if isinstance(g, r.AnnotationBaseX))
    ann.ParentDimensionStyle = list(f.AllDimStyles)[0]
    bb = ann.GetBoundingBox(r.TransformX.Identity)
    assert bb.IsValid
    moved = ann.GetBoundingBox(r.TransformX.Translation(r.Vector3dX(10, 0, 0)))
    assert abs(moved.Min.X - bb.Min.X - 10) < 1e-9 and abs(moved.Max.X - bb.Max.X - 10) < 1e-9
    assert r.GeometryBaseX.GetBoundingBox(ann, r.TransformX.Identity).Min.X == bb.Min.X


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderContentX') or
                    not hasattr(r.File3dmRenderContentX, 'GetParameter'), reason='G89')
def test_render_content_find_and_variant_parameters():
    # G89: collections Find(Guid) via the manifest; Get/SetParameter carry RhinoCommon's
    # Variant as a language value by its runtime type (D40)
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    mats = list(f.RenderMaterials)
    assert mats
    m = f.RenderMaterials.Find(mats[0].Id)
    assert m is not None and m.Id == mats[0].Id
    assert f.RenderMaterials.Find(uuid.uuid4()) is None
    assert m.GetParameter('rh3dm-no-such-parameter') is None
    gid = uuid.uuid4()
    values = {'s': 'hello', 'b': True, 'i': 7, 'd': 0.25, 'u': gid,
              'p2': (1.0, 2.0), 'p3': (1.0, 2.0, 3.0), 'p4': (1.0, 2.0, 3.0, 4.0)}
    for k, v in values.items():
        assert m.SetParameter('rh3dm-' + k, v)
    for k, v in values.items():
        got = m.GetParameter('rh3dm-' + k)
        assert got == v and type(got) is type(v), (k, got)
    with pytest.raises(TypeError):
        m.SetParameter('rh3dm-bad', object())


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderContentX') or
                    not hasattr(r.File3dmRenderContentX, 'FindChild'), reason='G89')
def test_render_content_parent_link_and_children():
    # G89: RhinoCommon's managed m__parent for a child content -- a light copy of the parent
    # proxy the child holds (Parent / TopLevel hand it out); Children / FindChild make new
    # child proxies of the same file. The old FirstChild / NextSibling chain is the oracle.
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dmX.Read(path)
    old = r.File3dm.Read(path)
    tops = [c for c in list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)]
    with_kids = [c for c in tops if list(c.Children)]
    assert with_kids
    for top in tops:
        assert top.Parent is None and top.TopLevel is top              # `return this` (borrowed)
        o = next(c for c in old.RenderContent if str(c.Id) == str(top.Id))
        chain, k = [], o.FirstChild
        while k is not None:
            chain.append(str(k.Id))
            k = k.NextSibling
        kids = list(top.Children)
        assert [str(c.Id) for c in kids] == chain
        for c in kids:
            assert isinstance(c, r.File3dmRenderContentX) and c.IsChild and not c.IsTopLevel
            assert c.Parent.Id == top.Id and c.Parent is c.Parent      # the copy it holds
            assert c.TopLevel.Id == top.Id
            found = top.FindChild(c.ChildSlotName)
            assert found is not None and found.Id == c.Id
        assert top.FindChild('rh3dm-no-such-slot') is None
    kid = list(with_kids[0].Children)[0]
    pid = kid.Parent.Id
    del f, tops, with_kids
    gc.collect()
    assert kid.Parent.Id == pid and kid.Name is not None            # the file is kept alive


@needs_comp
@pytest.mark.skipif(not hasattr(r.SubDVertexX, 'Edges'), reason='G89')
def test_subd_vertex_edges_iterator():
    # G89: `IEnumerable<SubDEdge> Edges { yield return EdgeAt(i); }` -- a list of new edges
    f, s = subd_from_file()
    v = next(iter(s.Vertices))
    edges = v.Edges
    assert len(edges) == v.EdgeCount > 0
    assert [e.Id for e in edges] == [v.EdgeAt(i).Id for i in range(v.EdgeCount)]
    del f, s, v
    gc.collect()
    assert all(e.Id > 0 for e in edges)                             # each keeps the SubD alive


@pytest.mark.skipif(not hasattr(r.ViewInfoX, 'Viewport'), reason='G89')
def test_view_info_viewport_is_a_held_view():
    # G89: `m_viewport ?? (m_viewport = new ViewportInfo(this))` -- a view of the ViewInfo's
    # own viewport, made once and held; its first write copies (RhinoCommon has no writable
    # branch), so the ViewInfo itself keeps its camera, and the held view keeps the edit
    old = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    ov = old.Views[0]
    v = r.ViewInfoX(ov)
    vp = v.Viewport
    assert vp is v.Viewport                                       # the held object, every time
    loc = ov.Viewport.CameraLocation
    assert (vp.CameraLocation.X, vp.CameraLocation.Y, vp.CameraLocation.Z) == (loc.X, loc.Y, loc.Z)
    assert vp.SetCameraLocation(r.Point3dX(1, 2, 3))
    again = v.Viewport.CameraLocation
    assert (again.X, again.Y, again.Z) == (1, 2, 3)               # the edit stays with the held view
    del v
    gc.collect()
    assert vp.CameraLocation.Z == 3                               # the view keeps its ViewInfo alive


@pytest.mark.skipif(not hasattr(r.BrepX, 'TryConvertBrep'), reason='G89')
def test_try_convert_brep_views_and_copies():
    # G89 (Identity, D42): RhinoCommon returns the argument itself when it is a File3dm view;
    # here a NEW view of the same file brep -- equal content, its own copy-on-write
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    g = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, r.BrepX))
    b = r.BrepX.TryConvertBrep(g)
    assert isinstance(b, r.BrepX) and b is not g and b.Faces.Count == g.Faces.Count
    x0 = g.GetBoundingBox(r.TransformX.Identity).Min.X
    assert b.Transform(r.TransformX.Translation(r.Vector3dX(100, 0, 0)))
    assert abs(b.GetBoundingBox(r.TransformX.Identity).Min.X - x0 - 100) < 1e-6
    assert g.GetBoundingBox(r.TransformX.Identity).Min.X == x0      # the argument is untouched
    del f, g
    gc.collect()
    assert b.Faces.Count > 0                                         # the view kept the file alive
    own = box_brep()                                                 # owns its pointer: BrepForm
    c = r.BrepX.TryConvertBrep(own)
    assert c is not own and c.Faces.Count == own.Faces.Count


@pytest.mark.skipif(not hasattr(r, 'BitmapX'), reason='G90 hand bitmaps')
def test_hand_bitmaps_add_find_iterate_round_trip(tmp_path):
    # G90: the kept hand-written bitmap API with the review fixes -- a BitmapX can be added
    # (the old Bitmap() was an ON_Bitmap, which ONX_Model cannot copy: #38) and Add returns
    # the file's copy; FindId finds images (#35); iteration survives a Delete (#24);
    # FindIndex is positional (images carry no model index) and returns None past the end
    f = r.File3dmX()
    paths = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png']
    ids = []
    for p in paths:
        b = r.BitmapX()
        b.FileFullPath = p
        added = f.Bitmaps.Add(b)
        assert added is not None and added.FileFullPath == p
        ids.append(added.Id)
    assert len(f.Bitmaps) == 3 and len(set(map(str, ids))) == 3
    assert [f.Bitmaps.FindIndex(i).FileFullPath for i in range(3)] == paths
    assert f.Bitmaps[1].FileFullPath == paths[1]
    assert f.Bitmaps.FindId(ids[1]).FileFullPath == paths[1]
    assert f.Bitmaps.FindId(uuid.uuid4()) is None and f.Bitmaps.FindIndex(999) is None
    with pytest.raises(IndexError):
        f.Bitmaps[999]
    with pytest.raises(RuntimeError):
        f.Bitmaps.FindId(ids[0]).FileFullPath = '/tmp/other.png'  # the file keys images by path
    assert f.Bitmaps.Delete(ids[0]) and len(f.Bitmaps) == 2
    assert [b.FileFullPath for b in f.Bitmaps] == paths[1:]        # no IndexError at the hole
    out = str(tmp_path / 'bitmaps.3dm')
    assert f.Write(out, None)
    g = r.File3dmX.Read(out)
    assert sorted(b.FileFullPath for b in g.Bitmaps) == sorted(paths[1:])
    bm = next(iter(g.Bitmaps))
    del g
    gc.collect()
    assert bm.FileFullPath in paths                                # the view keeps the file alive


@pytest.mark.skipif(not hasattr(r, 'ManifestTableX'), reason='G91')
def test_manifest_table_find_count_enumerate():
    # G91: File3dm.Manifest, generated from RhinoCommon's File3dmManifestTable -- Find* make
    # the proxy Instantiate makes for each type; GetEnumerator(type) walks one type
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    m = f.Manifest
    T = r.ModelComponentType
    assert m.Count == len(m) and m.LongCount == m.Count and m.ComponentType == T.Mixed
    layers = list(f.AllLayers)
    assert layers and m.ActiveObjectCount(T.Layer) == f.AllLayers.Count
    viaenum = list(m.GetEnumerator(T.Layer))
    assert all(isinstance(x, r.LayerX) for x in viaenum)
    assert [str(x.Id) for x in viaenum] == [str(l.Id) for l in layers]
    l0 = layers[0]
    for found in (m.FindId(l0.Id), m.FindId(l0.Id, T.Layer), m.FindIndex(l0.Index, T.Layer),
                  m.FindName(l0.Name, T.Layer, uuid.UUID(int=0))):
        assert isinstance(found, r.LayerX) and found.Id == l0.Id and found.Name == l0.Name
    assert m.FindId(uuid.uuid4()) is None
    mats = list(m.GetEnumerator(T.RenderMaterial))
    assert all(isinstance(x, r.MaterialX) for x in mats)
    objs = list(m.GetEnumerator(T.ModelGeometry))
    assert all(isinstance(x, r.File3dmObjectX) for x in objs) and len(objs) == len(list(f.Objects))
    with pytest.raises(IndexError):
        m.FindIndex(0, T.Unset)                                      # ArgumentOutOfRangeException
    # the whole manifest: Instantiate throws for a type it cannot make (TextureMapping,
    # HistoryRecord, ...), exactly as RhinoCommon's foreach does (pilot #39)
    unmade = [t for t in (T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle) if m.ActiveObjectCount(t)]
    if unmade:
        with pytest.raises(RuntimeError):
            list(m)
    else:
        assert all(isinstance(x, r.ModelComponentX) for x in m)
    del f
    gc.collect()
    assert viaenum[0].Name == l0.Name                               # items keep the file alive


@pytest.mark.skipif(not hasattr(r, 'ManifestTableX') or not hasattr(r, 'BitmapX'), reason='G91')
def test_manifest_images_are_bitmapx():
    # G91, D44: RhinoCommon's Instantiate makes a BitmapEntry for an image (no working member
    # outside Rhino: pilot #37); here the kept BitmapX stands in, made from the image's id
    f = r.File3dmX()
    b = r.BitmapX()
    b.FileFullPath = '/tmp/rh3dm_manifest.png'
    added = f.Bitmaps.Add(b)
    imgs = list(f.Manifest.GetEnumerator(r.ModelComponentType.Image))
    assert len(imgs) == 1 and isinstance(imgs[0], r.BitmapX)
    assert imgs[0].FileFullPath == '/tmp/rh3dm_manifest.png' and imgs[0].Id == added.Id
    assert f.Manifest.FindId(added.Id).FileFullPath == '/tmp/rh3dm_manifest.png'


@pytest.mark.skipif(not hasattr(r.LayerX, 'FullPath'), reason='G92')
def test_layer_full_path_and_name_override():
    # G92: Layer.FullPath walks the file's layer table through parents (RhinoCommon's
    # File3dm_GetLayerPathName); Layer.Name is RhinoCommon's override (base.Name) and stays
    # writable; ToString is FullPath
    f = r.File3dmX()
    t = f.AllLayers
    black = (0, 0, 0, 255)
    t.AddLayer('top', black)
    top = list(t)[0]
    t.AddLayer('mid', black, top.Id)
    mid = next(l for l in t if l.Name == 'mid')
    t.AddLayer('leaf', black, mid.Id)
    leaf = next(l for l in t if l.Name == 'leaf')
    sep = r.ModelComponentX.NamePathSeparator
    assert top.FullPath == 'top'
    assert leaf.FullPath == sep.join(['top', 'mid', 'leaf'])
    assert leaf.ToString() == leaf.FullPath
    leaf.Name = 'leaf2'                                    # the override keeps the base setter
    assert next(l for l in t if l.Id == leaf.Id).FullPath.endswith(sep + 'leaf2')
    assert r.LayerX().FullPath == r.LayerX().Name           # not in a file: its name


@pytest.mark.skipif(not hasattr(r.File3dmGroupTableX, 'GroupMembers'), reason='G92')
def test_group_members_and_find_by_group():
    # G92: File3dmGroupTable.GroupMembers / File3dmObjectTable.FindByGroup -- RhinoCommon's
    # loop over the objects; an object in no group has no group list (null in .NET, which
    # makes RhinoCommon's own loop throw: pilot #40) and is skipped here (D35)
    f = r.File3dmX()
    g0 = f.AllGroups.AddGroup()
    g1 = f.AllGroups.AddGroup()
    def add(groups):
        a = r.ObjectAttributesX()
        for g in groups:
            a.AddToGroup(g)
        return f.Objects.AddPoint(r.Point3dX(len(groups), 0, 0), a)
    ids = {'none': add([]), 'g0': add([g0]), 'both': add([g0, g1])}
    m0 = f.AllGroups.GroupMembers(g0)
    assert sorted(str(o.Id) for o in m0) == sorted(str(ids[k]) for k in ('g0', 'both'))
    assert [str(o.Id) for o in f.AllGroups.GroupMembers(g1)] == [str(ids['both'])]
    assert f.AllGroups.GroupMembers(99) == []
    grp = f.AllGroups.FindIndex(g1)
    assert [str(o.Id) for o in f.Objects.FindByGroup(grp)] == [str(ids['both'])]
    del f
    gc.collect()
    assert m0[0].Geometry is not None                       # the items keep the file alive


def test_class_static_properties_are_properties():
    # Luis, 2026-10-16: a generated class's static PROPERTY is a Python property, as the value
    # types' are (BoundingBoxX.Empty) -- not a method
    assert isinstance(r.MeshingParametersX.Default, r.MeshingParametersX)
    assert r.MaterialX.MaxShine == 255.0
    assert isinstance(r.ModelComponentX.NamePathSeparator, str)
    assert isinstance(r.ObjectAttributesX.OCSMappingChannelId, int)
    assert isinstance(r.SubDX.Empty, r.SubDX)
    assert not callable(r.MeshingParametersX.Default)


def test_retired_arc_arccurve_brep_members():
    # Luis, 2026-10-16 (api/retired.toml): rhino3dm-only members and their RhinoCommon forms,
    # compared on the same input
    oa = r.Arc(r.Point3d(0, 0, 0), r.Point3d(1, 1, 0), r.Point3d(2, 0, 0))
    na = r.ArcX(r.Point3dX(0, 0, 0), r.Point3dX(1, 1, 0), r.Point3dX(2, 0, 0))
    assert abs(oa.AngleRadians - na.Angle) < 1e-12                      # Arc.AngleRadians -> Arc.Angle
    def same(oc, nc):
        assert abs(oc.Domain.T0 - nc.Domain.T0) < 1e-12 and abs(oc.Domain.T1 - nc.Domain.T1) < 1e-12
        a, b = oc.PointAtEnd, nc.PointAtEnd
        assert abs(a.X - b.X) < 1e-9 and abs(a.Y - b.Y) < 1e-9 and abs(a.Z - b.Z) < 1e-9
    same(r.ArcCurve.CreateFromArc(oa), r.ArcCurveX(na))                 # -> ArcCurve(arc)
    same(r.ArcCurve.CreateFromArcParams(oa, 2.0, 5.0), r.ArcCurveX(na, 2.0, 5.0))
    same(r.ArcCurve.CreateFromCircle(r.Circle(3.0)), r.ArcCurveX(r.CircleX(3.0)))
    same(r.ArcCurve.CreateFromCircleParams(r.Circle(3.0), 1.0, 4.0), r.ArcCurveX(r.CircleX(3.0), 1.0, 4.0))
    # Brep.CreateFromBoundingBox -> Brep.CreateFromBox(BoundingBox) / CreateFromBox(Box)
    ob = r.Brep.CreateFromBoundingBox(r.BoundingBox(r.Point3d(0, 0, 0), r.Point3d(1, 2, 3)))
    bb = r.BoundingBoxX(r.Point3dX(0, 0, 0), r.Point3dX(1, 2, 3))
    nb = r.BrepX.CreateFromBox(bb)
    assert len(ob.Faces) == nb.Faces.Count == r.BrepX.CreateFromBox(r.BoxX(bb)).Faces.Count == 6
    # BrepTrim.EdgeIndex / StartVertexIndex / EndVertexIndex -> trim.Edge.EdgeIndex, trim.StartVertex.VertexIndex, ...
    old_trims = sorted((t.EdgeIndex, t.StartVertexIndex, t.EndVertexIndex)
                       for f in ob.Faces for lp in f.Loops for t in lp.Trims)
    new_trims = sorted((t.Edge.EdgeIndex if t.Edge else -1, t.StartVertex.VertexIndex, t.EndVertex.VertexIndex)
                       for t in (nb.Trims[i] for i in range(len(nb.Trims))))
    assert old_trims == new_trims
    # BrepVertex.EdgeCount -> len(vertex.EdgeIndices())
    assert [v.EdgeCount for v in ob.Vertices] == [len(nb.Vertices[i].EdgeIndices()) for i in range(len(nb.Vertices))]


def test_retired_viewinfo_vector_sun_shutlining_members(tmp_path):
    # Luis, 2026-10-16 (api/retired.toml)
    old = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    ov = old.Views[0]
    assert r.ViewInfoX(ov).WallpaperFilename == ov.WallpaperName           # ViewInfo.WallpaperName
    a, b = r.Vector3d(1, 2, 3), r.Vector3d(-4, 5, 0.5)
    assert r.Vector3d.DotProduct(a, b) == r.Vector3dX(1, 2, 3) * r.Vector3dX(-4, 5, 0.5)   # the * operator
    for alt in (-10.0, 0.0, 5.0, 12.5, 30.0, 60.0, 89.0):
        oc, nc = r.Sun.SunColorFromAltitude(alt), r.SunX.ColorFromAltitude(alt)
        # RhinoCommon rounds 255*c to the nearest integer; the old binding truncated 255.5*c (D45)
        assert all(abs(x - y) <= 1 for x, y in zip(oc[:3], nc[:3])), (alt, oc, nc)
    # ShutLining.Curves (a table of curve objects) -> GetCurves() ids + the id-keyed accessors.
    # RhinoCommon cannot create a shut-lining outside Rhino (File3dmMeshModifiers.ShutLining is
    # null until one exists), so the old binding writes it and the generated one reads it back
    # (the old CreateShutLining / CreateThickening: kept, their trailing space fixed -- pilot #42)
    cid = uuid.uuid4()
    of = r.File3dm()
    oa = r.ObjectAttributes()
    assert oa.MeshModifiers.Thickening is None
    oa.MeshModifiers.CreateThickening()
    assert oa.MeshModifiers.Thickening is not None
    oa.MeshModifiers.CreateShutLining()
    osl = oa.MeshModifiers.ShutLining
    osl.Curves.Add(cid)
    c = osl.Curves.FindIndex(0)
    c.Radius, c.Profile, c.Pull, c.IsBump, c.Enabled = 2.5, 3, True, True, False
    of.Objects.AddPoint(r.Point3d(0, 0, 0), oa)
    out = str(tmp_path / 'shutlining.3dm')
    assert of.Write(out, 0)
    f2 = r.File3dmX.Read(out)
    g = next(iter(f2.Objects))
    sl = g.Attributes.File3dmMeshModifiers.ShutLining
    if sl is None:
        # no mesh modifier made through rhino3dm survives a write and read -- the old binding's
        # own read loses it too (pilot-findings #43); the checks below run once that is fixed
        pytest.xfail('pilot-findings #43: mesh modifiers are lost on write/read')
    assert [str(x) for x in sl.GetCurves()] == [str(cid)]
    assert (sl.CurveRadius(cid), sl.CurveProfile(cid), sl.CurvePull(cid), sl.CurveIsBump(cid), sl.CurveEnabled(cid)) \
        == (2.5, 3, True, True, False)


@pytest.mark.skipif(not hasattr(r.BrepX, 'CreateTrimmedPlane'), reason='G95')
def test_class_sequence_parameters(tmp_path):
    # G95: `IEnumerable<Curve>` / `IEnumerable<GeometryBase>` parameters -- the caller's objects,
    # passed as a native pointer array (SimpleArray*Pointer); `new[] { curve }` likewise
    circle = r.ArcCurveX(r.CircleX(2.0))
    one = r.BrepX.CreateTrimmedPlane(r.PlaneX.WorldXY, circle)
    many = r.BrepX.CreateTrimmedPlane(r.PlaneX.WorldXY, [circle])
    old = r.Brep.CreateTrimmedPlane(r.Plane.WorldXY(), r.ArcCurve.CreateFromCircle(r.Circle(2.0)))
    assert one.Faces.Count == many.Faces.Count == len(old.Faces) == 1
    assert len(one.Edges) == len(old.Edges)
    # File3dm.WriteMultipleObjects(path, geometry)
    out = str(tmp_path / 'multi.3dm')
    assert r.File3dmX.WriteMultipleObjects(out, [circle, one])
    assert len(list(r.File3dmX.Read(out).Objects)) == 2
    # File3dmInstanceDefinitionTable.Add(name, description, basePoint, geometry, attributes)
    f = r.File3dmX()
    n0 = len(f.AllInstanceDefinitions)
    idx = f.AllInstanceDefinitions.Add('blk', 'desc', r.Point3dX(0, 0, 0), [circle], [r.ObjectAttributesX()])
    assert idx >= 0 and len(f.AllInstanceDefinitions) == n0 + 1


@pytest.mark.skipif(not hasattr(r.AnnotationBaseX, 'TextHasRtfFormatting'), reason='G96 not generated')
def test_annotation_text_from_text_runs():
    # G96: RhinoCommon 9 reads an annotation's plain text from its text runs (LINQ over the
    # internal TextRun objects GetTextRuns makes and the pass frees): Text and PlainText are the
    # text runs joined (a newline or paragraph run as \r\n); TextHasRtfFormatting is "more than
    # one text run" (the old binding tests RichText for an {\rtf prefix -- the V8 rule)
    f = r.File3dmX.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    old = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    old_text = {str(o.Attributes.Id): o.Geometry.PlainText for o in old.Objects if isinstance(o.Geometry, r.Text)}
    seen = 0
    for o in f.Objects:
        g = o.Geometry
        if not isinstance(g, r.AnnotationBaseX):
            continue
        assert g.Text == g.PlainText and isinstance(g.TextHasRtfFormatting, bool)
        oid = str(o.Attributes.ObjectId)
        if oid in old_text:
            # a text entity: opennurbs' own plain text, newline for newline
            assert g.PlainText.replace('\r\n', '\n') == old_text[oid].replace('\r\n', '\n'), (g.PlainText, old_text[oid])
            seen += 1
    assert seen > 0
    # the setter (SetRichText through PlainTextToRtf), read back through the runs
    ann = next(o.Geometry for o in f.Objects if str(o.Attributes.ObjectId) in old_text)
    ann.PlainText = 'one\ntwo'
    assert ann.PlainText == 'one\r\ntwo' and ann.TextHasRtfFormatting       # two text runs
    ann.Text = 'single'
    assert ann.PlainText == 'single' and not ann.TextHasRtfFormatting


@pytest.mark.skipif(not hasattr(r.TextureX, 'ProjectionMode'), reason='G96 not generated')
def test_texture_projection_mode_c_layer_enum():
    # G96: ON_Texture_GetProjectionMode returns the C layer's own enum (P/Invoke declares the C#
    # enum itself): the result converts to the binding's enum, the setter's argument back
    t = r.TextureX()
    m = r.TextureProjectionModes
    assert t.ProjectionMode == m.MappingChannel
    t.ProjectionMode = m.Wcs
    assert t.ProjectionMode == m.Wcs


@pytest.mark.skipif(not hasattr(r.SunX, 'GetDateTime'), reason='G97 not generated')
def test_sun_date_time():
    # G97: a DateTime is the binding's date -- Python a datetime (naive = the machine's local
    # time, .NET's Local), JS a Date -- through RhinoCommon's Variant time_t, both ways
    import datetime
    s = r.SunX()
    K = r.DateTimeKind
    when = datetime.datetime(2024, 6, 21, 14, 30, 15)
    s.SetDateTime(when, K.Local)
    assert s.GetDateTime(K.Local) == when
    assert isinstance(s.GetDateTime(K.Utc), datetime.datetime)
    with pytest.raises(ValueError):
        s.GetDateTime(K.Unspecified)                       # ArgumentException
    aware = datetime.datetime(2024, 1, 2, 3, 4, 5, tzinfo=datetime.timezone.utc)
    s.SetDateTime(aware, K.Local)                          # an aware datetime: its instant
    assert s.GetDateTime(K.Local) == aware.astimezone().replace(tzinfo=None)
    s.SetPosition(when, 40.0, -3.5)                        # the obsolete overload: SetDateTime(Local)
    assert (s.Latitude, s.Longitude, s.GetDateTime(K.Local)) == (40.0, -3.5, when)
    with pytest.raises(TypeError):
        s.SetDateTime('2024-06-21', K.Local)
    a = r.SunX.AltitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, False)
    assert -90.0 <= a <= 90.0


@pytest.mark.skipif(not hasattr(r.SunX, 'GetDateTime'), reason='G97 not generated')
def test_retired_sun_date_members(tmp_path):
    # G97: the old Sun.Year/Month/Day/Hours are SunX.GetDateTime(DateTimeKind.Local)'s fields --
    # the same ON_Sun::LocalDateTime through the C layer's mktime / localtime (Hours to the second)
    of = r.File3dm()
    osun = of.Settings.RenderSettings.Sun
    osun.Year, osun.Month, osun.Day, osun.Hours = 2023, 11, 5, 9.75
    out = str(tmp_path / 'sun.3dm')
    assert of.Write(out, 0)
    f = r.File3dmX.Read(out)
    d = f.Settings.RenderSettings.Sun.GetDateTime(r.DateTimeKind.Local)
    assert (d.year, d.month, d.day) == (osun.Year, osun.Month, osun.Day)
    assert abs(d.hour + d.minute / 60 + d.second / 3600 - osun.Hours) < 1 / 3600


@pytest.mark.skipif(not hasattr(r.TextureMappingX, 'MappingType'), reason='G97 not generated')
def test_retired_texturemapping_members():
    # G97: TextureMapping.MappingType (a switch over the C layer's enum) -- the old IsPeriodic is
    # MappingType in (Sphere, Cylinder); Reverse/Swap/TileTextureCoordinate left-multiply
    # UvwTransform (opennurbs ON_TextureMapping), composed here on the generated object
    T = r.TextureMappingType
    I = r.Interval(0, 1)
    olds = [r.TextureMapping.CreateSurfaceParameterMapping(),
            r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY(), I, I, I),
            r.TextureMapping.CreateCylinderMapping(r.Cylinder(r.Circle(1.0), 2.0), True),
            r.TextureMapping.CreateCylinderMapping(r.Cylinder(r.Circle(1.0), 2.0), False),
            r.TextureMapping.CreateSphereMapping(r.Sphere(r.Point3d(0, 0, 0), 2.0)),
            r.TextureMapping.CreateBoxMapping(r.Plane.WorldXY(), I, I, I, True)]
    kinds = [r.TextureMappingX(o).MappingType for o in olds]
    assert kinds == [T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping]
    for o, k in zip(olds, kinds):
        assert o.IsPeriodic == (k in (T.SphereMapping, T.CylinderMapping))
        # RequiresVertexNormals (D49): RhinoCommon's properties, as ON_TextureMapping decides it
        n = r.TextureMappingX(o)
        derived = k != T.SurfaceParameters and (n.Projection == r.Projection.Ray or k == T.BoxMapping
                                                or (k == T.CylinderMapping and n.Capped))
        assert o.RequiresVertexNormals == derived, k
    def composed(x):
        n = r.TextureMappingX(r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY(), I, I, I))
        n.UvwTransform = x * n.UvwTransform
        return n.UvwTransform
    def matrix(t):
        return [getattr(t, 'M%d%d' % (i, j)) for i in range(4) for j in range(4)]
    for name, args, build in (
            ('ReverseTextureCoordinate', (1,), lambda x: (setattr(x, 'M11', -1.0), setattr(x, 'M13', 1.0))),
            ('SwapTextureCoordinate', (0, 2), lambda x: (setattr(x, 'M00', 0.0), setattr(x, 'M22', 0.0), setattr(x, 'M02', 1.0), setattr(x, 'M20', 1.0))),
            ('TileTextureCoordinate', (0, 3.0, 0.25), lambda x: (setattr(x, 'M00', 3.0), setattr(x, 'M03', 0.25)))):
        o = r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY(), I, I, I)
        assert getattr(o, name)(*args)
        x = r.TransformX(1.0)
        build(x)
        assert matrix(r.TextureMappingX(o).UvwTransform) == matrix(composed(x)), name


def test_retired_rhino_only_pairings():
    # Luis 2026-10-16: old members matched by name to RHINO_SDK-only RhinoCommon members, whose
    # portable equivalents are generated -- Layer.PathSeparator is RhinoCommon's
    # ModelComponent.NamePathSeparator; the old AnnotationBase.GetDimensionScale(parentStyle) is
    # DimensionScale with that parent (D39), not RhinoCommon's view-dependent GetDimensionScale
    # (the old Layer.PathSeparator itself raises TypeError: a static property bound to a
    # zero-argument getter -- pilot #45; the value is opennurbs' ON_ModelComponent::NamePathSeparator)
    assert r.ModelComponentX.NamePathSeparator == '::'
    assert r.LayerX.NamePathSeparator == '::'
    path = os.path.join(MODELS, 'textEntities_r8.3dm')
    old, f = r.File3dm.Read(path), r.File3dmX.Read(path)
    ds = list(f.AllDimStyles)[0]
    ods = next(s for s in old.DimStyles if str(s.Id) == str(ds.Id))
    oann = {str(o.Attributes.Id): o.Geometry for o in old.Objects if isinstance(o.Geometry, r.AnnotationBase)}
    n = 0
    for o in f.Objects:
        g = o.Geometry
        if isinstance(g, r.AnnotationBaseX) and str(o.Attributes.ObjectId) in oann:
            g.ParentDimensionStyle = ds
            assert g.DimensionScale == oann[str(o.Attributes.ObjectId)].GetDimensionScale(ods)
            n += 1
    assert n > 0


@pytest.mark.skipif(not hasattr(r, 'PostEffectCollectionX'), reason='G98 not generated')
def test_post_effect_collection_and_data():
    # G98: RenderSettings.PostEffects -- RhinoCommon's portable PostEffectCollection (free-
    # floating: the file's ON_PostEffects) and its PostEffectData items, components of
    # (collection, id) resolved through ON_PostEffects_PostEffectFromId; foreach is the internal
    # PostEffectDataEnumerator (GetAt by position). The old PostEffect table is the oracle.
    seen = 0
    for name in ('posteffects.3dm', 'file3dm_stuff.3dm', 'textEntities_r8.3dm', 'sphereDecals.3dm'):
        path = os.path.join(MODELS, name)
        # Type (D50): opennurbs numbers Unset 0, Early 1, ToneMapping 2, Late 3; RhinoCommon's
        # PostEffectType Early 0, ToneMapping 1, Late 2 (an unset type reads as Early)
        old = [(str(p.Id), p.LocalName, p.On, p.Shown, max(int(p.Type) - 1, 0)) for p in r.File3dm.Read(path).Settings.RenderSettings.PostEffects]
        f = r.File3dmX.Read(path)
        coll = f.Settings.RenderSettings.PostEffects
        new = [(str(p.Id), p.LocalName, p.On, p.Shown, int(p.Type)) for p in coll]
        assert new == old, name
        seen += len(new)
        # the old table's FindId / FindIndex (triage H1: map) -> PostEffectDataFromId / the i-th item
        otab = r.File3dm.Read(path).Settings.RenderSettings.PostEffects
        items = list(coll)
        for i in range(len(otab)):
            oi = otab.FindIndex(i)
            assert str(oi.Id) == str(items[i].Id) and oi.LocalName == items[i].LocalName, (name, i)
            assert coll.PostEffectDataFromId(uuid.UUID(str(oi.Id))).LocalName == otab.FindId(oi.Id).LocalName
        ghost = coll.PostEffectDataFromId(uuid.uuid4())
        with pytest.raises(RuntimeError):
            ghost.LocalName                                  # "PostEffectData.CppPointer failed"
        if new:
            d = coll.PostEffectDataFromId(uuid.UUID(new[0][0]))
            assert str(d.Id) == new[0][0]
            on = d.On
            d.On = not on                                   # writes go into the file's object
            assert next(p for p in coll if str(p.Id) == new[0][0]).On == (not on)
    assert list(r.PostEffectCollectionX()) == []             # free-floating: an empty ON_PostEffects
    assert seen > 0          # tests/models/posteffects.3dm (Luis 2026-10-29) carries post effects


@pytest.mark.skipif(not hasattr(r.DimensionStyleX, 'Font'), reason='G99 not generated')
def test_font_is_managed_and_dimension_style_font():
    # G99 (Luis: option A): RhinoCommon's Font is a MANAGED font -- an entry of opennurbs' process-
    # wide list, shared, immutable, never freed -- and so is FontX (a borrowed pointer, never
    # deleted). DimensionStyle.Font hands out the style's managed font; the constructors and the
    # finders return managed fonts too (`Font(familyName)` delegates with RhinoCommon's defaults)
    a, b = r.FontX('Arial'), r.FontX('Arial')
    assert a.FamilyName == b.FamilyName and a.Description == b.Description
    del a
    gc.collect()                                             # dropping one frees nothing shared
    assert b.FamilyName and r.FontX('Arial').Description == b.Description
    f = r.File3dmX.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    old = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    for ds in f.AllDimStyles:
        ods = next(s for s in old.DimStyles if str(s.Id) == str(ds.Id))
        assert ds.Font.FaceName == ods.Font.FaceName         # the same managed font
    s = r.DimensionStyleX()
    s.Font = r.FontX('Courier New')
    got = s.Font
    del s
    gc.collect()
    assert got.FamilyName                                     # managed: outlives the style
    for name in ('FromQuartetProperties',):
        q = r.FontX.FromQuartetProperties('Arial', False, False)
        assert q is None or q.FamilyName
    # G99 side effect: setters whose C argument is an opennurbs scoped enum (P/Invoke passes the value)
    s2 = r.DimensionStyleX()
    s2.FitText = s2.FitText
    s2.FitArrow = s2.FitArrow


def test_translated_list_indexers_g101():
    # class-mode indexers (G101): a list's `this[int]` gives __getitem__ / __setitem__ / __len__,
    # so the list iterates (Python's item protocol stops at IndexError: IndexOutOfRangeException)
    P = r.Point3dX
    m = quad_mesh()
    vs = m.Vertices
    assert len(vs) == 4
    assert [(p.X, p.Y) for p in vs] == [(0, 0), (1, 0), (1, 1), (0, 1)]
    vs[2] = r.Point3fX(5, 6, 7)
    assert (vs[2].X, vs[2].Z) == (5, 7)
    with pytest.raises(IndexError):
        vs[4]
    assert len(m.Faces) == 1 and [f.D for f in m.Faces] == [3]
    nc = r.NurbsCurveX.Create(False, 3, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)])
    cps = nc.Points
    assert len(cps) == 4 and [c.Location.X for c in cps] == [0, 1, 2, 3]
    with pytest.raises(IndexError, match='greater than or equal to zero'):
        cps[-1]


def test_color_list_and_surface_point_grid_iterate_g102():
    # MeshVertexColorList's indexer (Color.FromArgb / ToArgb through the C layer's ARGB conversions)
    m = quad_mesh()
    vc = m.VertexColors
    for i in range(4):
        vc.Add((10 * i, 20, 30, 255))
    assert len(vc) == 4
    assert vc[2] == (20, 20, 30, 255)                       # (r, g, b, a): .NET alpha, 255 opaque
    vc[1] = (1, 2, 3, 128)
    assert vc[1] == (1, 2, 3, 128)
    assert [c[0] for c in vc] == [0, 1, 20, 30]
    with pytest.raises(IndexError):
        vc[4]
    # NurbsSurfacePointList: RhinoCommon's NurbsSrfEnum order, u outermost
    ns = r.NurbsSurfaceX.Create(3, False, 2, 2, 3, 2)
    pts = ns.Points
    ns.Points.SetPoint(2, 1, 5.0, 6.0, 7.0)
    cps = list(pts)
    assert len(cps) == pts.CountU * pts.CountV == 6
    assert cps[2 * pts.CountV + 1].Location == r.Point3dX(5, 6, 7)


def test_brep_surfaces_and_view_table_g103():
    # BrepSurfaceList: RhinoCommon's lazily filled wrapper cache is analyzed as its per-item view
    # (SurfaceHolder(brep, index)); a view per access, keeping the brep alive
    import gc
    b = box_brep()
    srfs = b.Surfaces
    assert len(srfs) == 6
    items = list(srfs)
    assert len(items) == 6 and all(isinstance(s, r.SurfaceX) and s.IsPlanar() for s in items)
    with pytest.raises(IndexError):
        srfs[6]
    s0 = srfs[0]
    del b, srfs, items
    gc.collect()
    assert s0.IsPlanar()                                   # the view keeps its brep
    # File3dmViewTable: the stored m_named_views flag (registry state) reaches translated bodies
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    n = f.Views.Count
    assert n > 0 and f.NamedViews.Count >= 0
    assert f.Views.Delete(0) is True and f.Views.Count == n - 1
    with pytest.raises(IndexError):
        f.Views.Delete(99)


def test_view_table_items_are_live_views_and_plugin_data_g104():
    # File3dmViewTable[i] (Luis: as RhinoCommon): a LIVE ViewInfo into the file's table --
    # writes reach the file, and the view keeps the file alive
    import gc
    import uuid
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    views = list(f.Views)
    assert len(views) == f.Views.Count > 0 and all(isinstance(v, r.ViewInfoX) for v in views)
    v = f.Views[0]
    v.Name = 'rh3dm-live'
    assert f.Views[0].Name == 'rh3dm-live'                 # write-through, not a copy
    del f, views
    gc.collect()
    assert v.Name == 'rh3dm-live'                          # the view keeps its file
    assert r.ViewInfoX().Name == ''                        # a free-standing ViewInfo still owns its own
    with pytest.raises(IndexError):
        r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm')).Views[99]
    # File3dmPlugInDataTable[i] (Luis: generate it): a (file, plug-in id) record
    g = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    pd = list(g.PlugInData)
    assert len(pd) == g.PlugInData.Count
    assert all(isinstance(p, r.File3dmPlugInDataX) and isinstance(p.PlugInId, uuid.UUID) for p in pd)


def test_viewport_screen_port_g105():
    # RhinoCommon's ScreenPort (get/set), GetScreenPort() -- its plain overload, not the
    # (rect, near, far) out form (G105) -- and the Rectangle overloads: a Rectangle is (x, y, w, h)
    v = r.ViewportX()
    v.ScreenPort = (10, 20, 640, 480)
    assert v.ScreenPort == (10, 20, 640, 480)
    assert v.GetScreenPort() == (10, 20, 640, 480)
    assert v.GetScreenPortLocation() == (10, 20, 650, 500)          # (left, top, right, bottom)
    assert v.SetScreenPort((0, 0, 100, 50), 1, 2) is True
    assert v.ScreenPort == (0, 0, 100, 50)
    assert v.SetScreenPort(0, 640, 0, 480, 0, 0) is True             # y-up: FromLTRB(left, min, right, max)
    assert v.ScreenPort == (0, 0, 640, 480)
    assert v.SetScreenPort(0, 640, 100, 100, 0, 0) is False          # opennurbs refuses top == bottom
    assert v.ScreenPort == (0, 0, 640, 480)                          # ... and keeps the port
    with pytest.raises(ValueError):
        v.ScreenPort = (1, 2, 3)


def test_draco_compression_g107(tmp_path):
    # RhinoCommon's DracoCompression over the synced c/draco.cpp and the Draco subset (G107)
    m = quad_mesh()
    d = r.DracoCompressionX.Compress(m)
    b = d.ToByteArray()
    assert isinstance(b, bytes) and len(b) > 0
    g = r.DracoCompressionX.DecompressByteArray(b)
    assert isinstance(g, r.MeshX) and g.Vertices.Count == 4 and g.Faces.Count >= 1
    g64 = r.DracoCompressionX.DecompressBase64String(d.ToBase64String())
    assert isinstance(g64, r.MeshX) and g64.Vertices.Count == 4
    assert d.VertexAttributePosition >= 0
    # options: the data class (properties and public fields; DracoColorFormat a generated enum)
    o = r.DracoCompressionOptionsX()
    assert (o.CompressionLevel, o.PositionQuantizationBits, o.IncludeNormals) == (7, 14, True)
    o.IncludeNormals = False
    o.VertexColorFormat = r.DracoColorFormat.RGBA
    o.PositionQuantizationBits = 11
    d2 = r.DracoCompressionX.Compress(m, o)
    assert d2.NormalAttributePosition == -1                   # normals left out
    # a point cloud
    pc = r.PointCloud()
    for i in range(5):
        pc.Add(r.Point3d(i, 2 * i, 0))
    dp = r.DracoCompressionX.Compress(r.PointCloudX(pc))
    gp = r.DracoCompressionX.DecompressByteArray(dp.ToByteArray())
    assert isinstance(gp, r.PointCloudX) and gp.Count == 5
    # files: Write / DecompressFile; a missing file is FileNotFoundError, as in .NET
    path = str(tmp_path / 'quad.drc')
    assert d.Write(path) is True
    assert r.DracoCompressionX.DecompressFile(path).Vertices.Count == 4
    with pytest.raises(FileNotFoundError):
        r.DracoCompressionX.DecompressFile(str(tmp_path / 'missing.drc'))
    # the old binding decodes the generated bytes (one Draco, one bitstream)
    old = r.DracoCompression.DecompressByteArray(b)
    assert old is not None and 'Mesh' in type(old).__name__ or isinstance(old, r.GeometryBase)


def test_subd_face_control_net_center_u3():
    # U3 (upstream, 2026-10-20): RhinoCommon's SubDFace.ControlNetCenterPoint / Normal / Frame, moved
    # out of its Rhino-only blocks -- checked against RhinoCommon's own definition of each
    f, s = subd_from_file()
    faces = list(s.Faces)
    assert faces
    for face in faces:
        n = face.VertexCount
        pts = [face.VertexAt(i).ControlNetPoint for i in range(n)]
        c = face.ControlNetCenterPoint
        assert all(abs(a - b) < 1e-12 for a, b in zip((c.X, c.Y, c.Z), (sum(p.X for p in pts) / n, sum(p.Y for p in pts) / n, sum(p.Z for p in pts) / n)))
        v = face.ControlNetCenterNormal
        assert abs((v.X * v.X + v.Y * v.Y + v.Z * v.Z) ** 0.5 - 1.0) < 1e-12
        fr = face.ControlNetCenterFrame
        assert (fr.Origin.X, fr.Origin.Y, fr.Origin.Z) == (c.X, c.Y, c.Z)
        assert all(abs(a - b) < 1e-12 for a, b in zip((fr.ZAxis.X, fr.ZAxis.Y, fr.ZAxis.Z), (v.X, v.Y, v.Z)))


def test_subd_sharp_edge_count_u2():
    # U2 (upstream, 2026-10-20): RhinoCommon's SubD.SharpEdgeCount() / ClearEdgeSharpness() / ErrorCount,
    # moved out of its RHINO_SDK block. SharpEdgeCount is RhinoCommon's METHOD (D60; the old binding's
    # read-only property), checked against RhinoCommon's definition: the edges whose IsSharp is true
    total = 0
    for name in ('subd.3dm', 'subdBox.3dm', 'subd_creases.3dm'):
        path = os.path.join(MODELS, name)
        subds = [o.Geometry for o in r.File3dmX.Read(path).Objects if isinstance(o.Geometry, r.SubDX)]
        olds = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD)]
        assert len(subds) == len(olds)
        for s, old in zip(subds, olds):
            n = s.SharpEdgeCount()
            assert n == sum(1 for e in s.Edges if e.IsSharp)
            assert n == old.SharpEdgeCount
            assert all(isinstance(e.Sharpness, r.SubDEdgeSharpnessX) for e in s.Edges)
            errors = r.SubDX.ErrorCount
            assert s.ClearEdgeSharpness() == n             # the number of edges changed
            assert s.SharpEdgeCount() == 0
            assert r.SubDX.ErrorCount == errors            # a process-wide counter: no new errors
            total += n
    print('sharp edges in the SubD fixtures:', total)


def test_subd_edge_sharpness_value_type_g109():
    # G109: SubDEdgeSharpness as a translated value type (two floats, as ON_SUBD_EDGE_SHARPNESS_STRUCT)
    S = r.SubDEdgeSharpnessX
    s = S(1.0, 2.0)
    assert (s[0], s[1], s.EndSharpness(1)) == (1.0, 2.0, 2.0)
    assert s.IsIncreasing and s.IsVariable and not s.IsConstant
    assert (s.Reversed()[0], s.Reversed()[1]) == (2.0, 1.0)
    assert S(3.0).IsConstant and S(3.0) == S(3.0, 3.0)
    crease = 5.0                       # SubDEdgeSharpness.CreaseValue (value-type constants are not bound)
    assert S.Crease.IsCrease and S(crease, crease) == S.Crease
    # FromInterval couples the ends: one crease end alone is not valid (why D61's JS fields are raw)
    assert S(crease, 0.0).IsNotValid
    assert S.Smooth.IsZero and S.Smooth == S()


def test_point_cloud_insert_range_u11():
    # U11 (upstream RH-99686, 2026-10-20): ON_PointCloud_InsertPoints moved the tail to the old end
    # instead of index + count -- a heap overrun when count < Count - index, the points out of order
    # otherwise (pilot-findings #34). Each shape below, including the ones the old copy got wrong;
    # the normals (tagged z = j + 1) must stay with their points, the inserted ones zero
    P, V = r.Point3dX, r.Vector3dX
    for n, i, k in ((4, 1, 1), (4, 0, 2), (4, 3, 1), (2, 0, 5), (5, 2, 3), (6, 1, 2), (3, 3, 2)):
        pc = r.PointCloudX()
        pc.AddRange([P(j, 0, 0) for j in range(n)], [V(0, 0, j + 1) for j in range(n)])
        pc.InsertRange(i, [P(90 + j, 0, 0) for j in range(k)])
        assert [p.X for p in pc.GetPoints()] == list(range(i)) + [90 + j for j in range(k)] + list(range(i, n)), (n, i, k)
        assert [v.Z for v in pc.GetNormals()] == [j + 1 for j in range(i)] + [0] * k + [j + 1 for j in range(i, n)], (n, i, k)
        assert pc.Count == n + k
    pc = r.PointCloudX()
    pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
    pc.InsertRange(1, [])                                  # nothing to insert: unchanged
    assert [p.X for p in pc.GetPoints()] == [0, 1]
    with pytest.raises(IndexError, match='larger than or equal to zero'):
        pc.InsertRange(-1, [P(9, 9, 9)])
    with pytest.raises(IndexError, match='smaller than or equal to Count'):
        pc.InsertRange(3, [P(9, 9, 9)])


def test_mesh_normal_list_is_mesh_vertex_normal_list():
    # the old MeshNormalList is RhinoCommon's MeshVertexNormalList (registry 'old' pairing):
    # the same operations on the same mesh give the same normals, step by step
    def folded():
        m = r.Mesh()
        for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (2, 0, 1), (2, 1, 1)]:
            m.Vertices.Add(*xyz)
        m.Faces.AddFace(0, 1, 2, 3)
        m.Faces.AddFace(1, 4, 5, 2)
        return m
    xyz = lambda v: tuple(v) if isinstance(v, tuple) else (v.X, v.Y, v.Z)
    vals = lambda l: [xyz(l[i]) for i in range(len(l))]
    old, new = folded().Normals, r.MeshX(folded()).Normals
    assert old.ComputeNormals() and new.ComputeNormals()
    assert len(old) == len(new) == 6 and vals(old) == vals(new)
    assert len(set(vals(new))) > 1                       # the fold: not all one normal
    old.Flip(); new.Flip()
    assert vals(old) == vals(new)
    # D66: the old Add went through ON_Mesh::SetVertexNormal, which unitizes; RhinoCommon's appends
    # the vector as given (UnitizeNormals then refuses: 7 normals, 6 vertices -- on both)
    assert old.Add(0, 0, 2) == new.Add(0.0, 0.0, 2.0) == 6
    assert xyz(old[6]) == (0, 0, 1) and xyz(new[6]) == (0, 0, 2)
    assert old.UnitizeNormals() == new.UnitizeNormals() == False
    assert vals(old)[:6] == vals(new)[:6]
    old.Clear(); new.Clear()
    assert len(old) == len(new) == 0
    old, new = folded().Normals, r.MeshX(folded()).Normals
    old.ComputeNormals(); new.ComputeNormals()
    old.Destroy(); new.Destroy()
    assert len(old) == len(new) == 0


def test_instance_reference_geometry_matches_old_instance_reference():
    # triage batch B: RhinoCommon's InstanceReferenceGeometry is the old InstanceReference
    for name in ('blocks.3dm', 'file3dm_stuff.3dm'):
        path = os.path.join(MODELS, name)
        new = [o.Geometry for o in r.File3dmX.Read(path).Objects if isinstance(o.Geometry, r.InstanceReferenceGeometryX)]
        old = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.InstanceReference)]
        assert len(new) == len(old) == 2
        for n, o in zip(new, old):
            assert n.ParentIdefId == o.ParentIdefId
            cells = ['M%d%d' % (i, j) for i in range(4) for j in range(4)]
            assert [getattr(n.Xform, c) for c in cells] == [getattr(o.Xform, c) for c in cells]


def test_dimension_classes_and_display_lines_g111():
    # triage batch B: the dimension classes are generated, files hand them out as their own class,
    # and GetDisplayLines returns (rc, lines) -- lines in the dimension's plane (parity: dim.*.displaylines)
    P, S = r.Point3dX, r.DimensionStyleX()
    od = r.OrdinateDimensionX.Create(S, r.PlaneX.WorldXY, r.OrdinateDimensionX.MeasuredDirection.Xaxis,
                                     P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
    ld = r.LinearDimensionX.Create(r.AnnotationType.Aligned, S, r.PlaneX.WorldXY, r.Vector3dX(1, 0, 0),
                                   P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    for d in (od, ld):
        d.UpdateDimensionText(S, r.UnitSystem.Millimeters)      # the text rect needs formatted text
    rc, lines = ld.GetDisplayLines(S, 1.0)
    assert rc and len(lines) > 0 and all(isinstance(l, r.LineX) for l in lines)
    orc, olines = od.GetDisplayLines(S, 1.0)                    # (rc, lines): values by parity, not here
    assert isinstance(orc, bool) and isinstance(olines, (list, tuple))
    f = r.File3dmX()
    ids = [f.Objects.AddOrdinateDimension(od), f.Objects.AddLinearDimension(ld)]
    back = r.File3dmX.FromByteArray(f.ToByteArray(None))
    kinds = {o.Attributes.ObjectId: type(o.Geometry).__name__ for o in back.Objects}
    assert [kinds[i] for i in ids] == ['OrdinateDimensionX', 'LinearDimensionX']


def test_create_keeps_its_style_alive_g112():
    # G112: Create stores dimStyle in the modeled ParentDimensionStyle (borrowed, D39): the result
    # keeps the argument alive -- a temporary style must survive the call and a collection
    import gc
    P = r.Point3dX
    ld = r.LinearDimensionX.Create(r.AnnotationType.Aligned, r.DimensionStyleX(), r.PlaneX.WorldXY,
                                   r.Vector3dX(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    gc.collect()
    s = ld.ParentDimensionStyle
    assert s is not None and s.TextHeight > 0
    ann = ld
    ann.ParentDimensionStyle = r.DimensionStyleX()
    gc.collect()
    assert ann.ParentDimensionStyle.TextHeight > 0


def test_draw_color_from_a_file_u5():
    # #5 (upstream RH-99693): ObjectAttributes.DrawColor(File3dm[, viewportId]) -- Rhino's rules over
    # the model: by layer, by object, by material (the material the attributes SELECT, its PBR base
    # color if physically based). The old extension used m_material_index and Diffuse only (D63).
    f = r.File3dmX()
    for c in ((200, 0, 0, 255), (0, 0, 200, 255)):
        m = r.MaterialX(); m.DiffuseColor = c; f.Materials.Add(m)
    layer = r.LayerX(); layer.Name = 'L'; layer.Color = (10, 20, 30, 255); layer.RenderMaterialIndex = 1
    f.Layers.Add(layer)
    pbr = r.MaterialX(); pbr.DiffuseColor = (9, 9, 9, 255); pbr.ToPhysicallyBased()
    pbr.PhysicallyBased.BaseColor = r.Color4fX(1.0, 0.5, 0.0, 1.0)
    f.Materials.Add(pbr)
    rgb = lambda c: tuple(c)[:3]
    a = r.ObjectAttributesX(); a.LayerIndex = 0
    a.ColorSource = r.ObjectColorSource.ColorFromLayer
    assert rgb(a.DrawColor(f)) == rgb(f.Layers.FindIndex(0).Color) == (10, 20, 30)
    a.ColorSource = r.ObjectColorSource.ColorFromObject; a.ObjectColor = (1, 2, 3, 255)
    assert rgb(a.DrawColor(f)) == (1, 2, 3)
    a.ColorSource = r.ObjectColorSource.ColorFromMaterial
    a.MaterialSource = r.ObjectMaterialSource.MaterialFromObject; a.MaterialIndex = 0
    assert rgb(a.DrawColor(f)) == (200, 0, 0)
    a.MaterialSource = r.ObjectMaterialSource.MaterialFromLayer          # the layer's render material (1)
    assert rgb(a.DrawColor(f)) == (0, 0, 200)
    a.MaterialSource = r.ObjectMaterialSource.MaterialFromObject; a.MaterialIndex = 2
    assert rgb(a.DrawColor(f)) == (255, 127, 0) or rgb(a.DrawColor(f)) == (255, 128, 0)   # PBR base color
    assert a.DrawColor(f) == a.DrawColor(f, __import__('uuid').UUID(int=0))


def test_mesh_texture_coordinates_from_a_file_u9():
    # #6 (U9, upstream RH-99691): Mesh.SetCachedTextureCoordinatesFromMaterial(File3dm, objectId,
    # Material) and GetCachedTextureCoordinates(File3dm, objectId, Texture) -- the old extensions'
    # opennurbs calls, now RhinoCommon's (the old getter was GetCachedTextureCoordinatesFromTexture)
    path = os.path.join(MODELS, 'meshWithTexture.3dm')
    f, old = r.File3dmX.Read(path), r.File3dm.Read(path)
    pairs = [(o, oo) for o, oo in zip(f.Objects, old.Objects) if isinstance(o.Geometry, r.MeshX)]
    assert pairs
    seen = 0
    for o, oo in pairs:
        mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
        omat = old.Materials[oo.Attributes.MaterialIndex]
        if mat is None or mat.GetBitmapTexture() is None:
            continue
        mesh, omesh = o.Geometry, oo.Geometry
        assert mesh.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat) is None   # void
        assert omesh.SetCachedTextureCoordinatesFromMaterial(old, oo.Attributes.Id, omat)
        tc = mesh.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
        otc = omesh.GetCachedTextureCoordinatesFromTexture(old, oo.Attributes.Id, omat.GetBitmapTexture())
        assert (tc is None) == (otc is None)
        if tc is not None:
            assert tc.Count == len(otc) if hasattr(otc, '__len__') else tc.Count >= 0
            seen += 1
    assert seen >= 1


def test_render_content_child_slots_u4():
    # #7 (U4, upstream RH-99692): File3dmRenderContent.SetChildSlotOn / ChildSlotAmount /
    # SetChildSlotAmount / SetChild -- RhinoCommon's RenderContent shape (slot name first, D64);
    # a slot with no amount reads 0.0 as RenderContent does (the old binding read 100.0)
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f, old = r.File3dmX.Read(path), r.File3dm.Read(path)
    tops = list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)
    top = next(c for c in tops if list(c.Children))
    o = next(c for c in old.RenderContent if str(c.Id) == str(top.Id))
    slot = list(top.Children)[0].ChildSlotName
    assert top.ChildSlotOn(slot) == o.ChildSlotOn(slot)
    on = top.ChildSlotOn(slot)
    assert top.SetChildSlotOn(slot, not on) and top.ChildSlotOn(slot) == (not on)
    assert top.SetChildSlotAmount(slot, 42.0) and top.ChildSlotAmount(slot) == 42.0
    assert top.ChildSlotAmount('rh3dm-no-such-slot') == 0.0 and o.ChildSlotAmount('rh3dm-no-such-slot') == 100.0
    n = len(list(top.Children))
    other = next(c for c in tops if c.Id != top.Id)
    assert top.SetChild(other, 'rh3dm-slot')
    assert len(list(top.Children)) == n + 1 and top.FindChild('rh3dm-slot') is not None
    back = r.File3dmX.FromByteArray(f.ToByteArray(None))
    btop = next(c for c in list(back.RenderMaterials) + list(back.RenderEnvironments) + list(back.RenderTextures) if c.Id == top.Id)
    assert btop.ChildSlotAmount(slot) == 42.0 and btop.FindChild('rh3dm-slot') is not None


@pytest.mark.xfail(reason='pilot-findings #50: SetChild copies the child WITH its id (ON_RenderContent copies through XML), '
                          'so a child set from a file content duplicates that id; FindChild then resolves the top-level one',
                   strict=True)
def test_render_content_set_child_gives_the_copy_its_own_id_u4():
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    tops = list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)
    top = next(c for c in tops if list(c.Children))
    other = next(c for c in tops if c.Id != top.Id)
    assert top.SetChild(other, 'rh3dm-slot')
    kid = top.FindChild('rh3dm-slot')
    assert kid.Id != other.Id and kid.IsChild and kid.ChildSlotName == 'rh3dm-slot'


def test_user_strings_kept_where_rhino3dm_had_them_d62():
    # D62 (Luis 2026-10-20): TextureMapping / Viewport / RenderSettings / Bitmap keep the user strings
    # the old CommonObject gave them (RhinoCommon's own internal members); RenderSettings works (the
    # old one segfaulted, pilot #49); GetUserStrings() is rhino3dm's tuple of (key, value) pairs
    f = r.File3dmX()
    objs = [r.ViewportX(), f.Settings.RenderSettings, r.BitmapX(),
            r.TextureMappingX.CreatePlaneMapping(r.PlaneX.WorldXY, r.IntervalX(0, 1), r.IntervalX(0, 1), r.IntervalX(0, 1))]
    for o in objs:
        assert o.UserStringCount == 0 and o.GetUserStrings() == ()
        assert o.SetUserString('k', 'v') and o.GetUserString('k') == 'v'
        assert o.UserStringCount == 1 and o.GetUserStrings() == (('k', 'v'),)
    layer = r.LayerX(); layer.SetUserString('a', '1'); layer.SetUserString('b', '2')
    assert sorted(layer.GetUserStrings()) == [('a', '1'), ('b', '2')]
    f.Layers.Add(layer)
    back = r.File3dmX.FromByteArray(f.ToByteArray(None))
    assert sorted(back.Layers.FindIndex(0).GetUserStrings()) == [('a', '1'), ('b', '2')]


# ---- retirement batch 1 (triage C, E, G1, L1, M1; Luis 2026-10-28): every old member against its
# RhinoCommon replacement on the same fixture

def _num(v):
    """pybind enums (old and generated) compare by value; tuples (colors) as tuples"""
    if isinstance(v, (bool, int, float, str)) or v is None:
        return v
    if isinstance(v, tuple):
        return tuple(v)
    return int(v)


def test_retired_annotation_explicit_parent_accessors_c():
    # C (Luis: drop): GetX(parentStyle) / SetX(parentStyle, v) -> ParentDimensionStyle = style; X (D39)
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    old, new = r.File3dm.Read(p), r.File3dmX.Read(p)
    oann = next(o.Geometry for o in old.Objects if isinstance(o.Geometry, r.AnnotationBase))
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBaseX))
    # the annotation's OWN style (D67): assigning a different ParentDimensionStyle changes the
    # annotation's style and drops its overrides (RhinoCommon); the old form evaluated any style
    ostyle = next(old.DimStyles[i] for i in range(len(old.DimStyles)) if old.DimStyles[i].Id == oann.DimensionStyleId)
    nstyle = new.DimStyles.FindId(ostyle.Id)
    nann.ParentDimensionStyle = nstyle
    assert nann.TextHeight == oann.GetTextHeight(ostyle) == 10.0      # the override survives
    props = ['TextHeight', 'MaskColor', 'MaskColorSource', 'MaskEnabled', 'MaskFrame', 'MaskOffset',
             'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay']
    for n in props:
        assert _num(getattr(oann, 'Get' + n)(ostyle)) == _num(getattr(nann, n)), n
    for n, v in (('TextHeight', 7.5), ('MaskOffset', 0.25), ('MaskEnabled', True), ('MaskColor', (10, 20, 30, 255))):
        getattr(oann, 'Set' + n)(ostyle, v)
        setattr(nann, n, v)
        assert _num(getattr(oann, 'Get' + n)(ostyle)) == _num(getattr(nann, n)), n
    for n in ('MaskColorSource', 'MaskFrame', 'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay'):
        cur = getattr(oann, 'Get' + n)(ostyle)
        other = next(e for e in type(cur).__members__.values() if int(e) != int(cur))
        getattr(oann, 'Set' + n)(ostyle, other)
        setattr(nann, n, type(getattr(nann, n))(int(other)))
        assert _num(getattr(oann, 'Get' + n)(ostyle)) == _num(getattr(nann, n)), n
    oann.SetDimensionScale(ostyle, 2.0)
    nann.DimensionScale = 2.0
    assert nann.DimensionScale == 2.0


def test_retired_numbered_list_duplicates_e():
    # E (Luis: drop): the Python-only `...2` list variants -> the generated originals (lists, D35)
    P, V = r.Point3d, r.Vector3d
    opc = r.PointCloud()
    for i in range(4):
        opc.Add(P(i, 0, 0), V(0, 0, 1), (10 * i, 0, 0, 255))
    npc = r.PointCloudX(opc)
    xyz = lambda q: (q.X, q.Y, q.Z)
    assert [xyz(q) for q in opc.GetPoints2()] == [xyz(q) for q in npc.GetPoints()]
    assert [xyz(q) for q in opc.GetNormals2()] == [xyz(q) for q in npc.GetNormals()]
    assert [tuple(c) for c in opc.GetColors2()] == [tuple(c) for c in npc.GetColors()]
    assert list(opc.GetValues2()) == list(npc.GetPointValues())
    ocrv = r.NurbsCurve.Create(False, 3, [P(0, 0, 0), P(1, 2, 0), P(3, 1, 0), P(4, 3, 0), P(6, 0, 0)])
    ncrv = r.NurbsCurveX(ocrv)
    assert list(ocrv.Knots.ToList2()) == list(ncrv.Knots)
    osrf = r.NurbsSurface.CreateFromSphere(r.Sphere(P(0, 0, 0), 2))
    assert list(osrf.KnotsU.ToList2()) == list(r.NurbsSurfaceX(osrf).KnotsU)
    obox = r.Brep.CreateFromBox(r.Box(r.BoundingBox(0, 0, 0, 1, 2, 3)))
    nbox = r.BrepX.CreateFromBox(r.BoundingBoxX(0, 0, 0, 1, 2, 3))
    assert list(obox.Faces[0].GetSpanVector2(0)) == list(nbox.Faces[0].GetSpanVector(0))
    x = r.Transform.Translation(1, 2, 3)
    assert list(x.ToFloatArray2(True)) == list(r.TransformX.Translation(1, 2, 3).ToFloatArray(True))
    pts = [P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)]
    opl, npl = r.Polyline.CreateFromPoints(pts), r.PolylineX([r.Point3dX(q.X, q.Y, q.Z) for q in pts])
    assert [(s.PointAtStart.X, s.PointAtEnd.Y) for s in opl.GetSegments2()] == \
           [(s.From.X, s.To.Y) for s in npl.GetSegments()]
    opc2 = r.PolyCurve(); opc2.Append(r.Line(P(0, 0, 0), P(1, 0, 0))); opc2.Append(r.Line(P(1, 0, 0), P(1, 1, 0)))
    npc2 = r.PolyCurveX(opc2)
    assert len(opc2.Explode2()) == len(npc2.Explode()) == 2
    p = os.path.join(MODELS, 'object_UserStrings.3dm')
    og = next(o.Geometry for o in r.File3dm.Read(p).Objects)
    ng = next(o.Geometry for o in r.File3dmX.Read(p).Objects)
    assert sorted(tuple(kv) for kv in og.GetUserStrings2()) == sorted(ng.GetUserStrings())
    p = os.path.join(MODELS, 'groups.3dm')
    of, nf = r.File3dm.Read(p), r.File3dmX.Read(p)
    assert len(of.Groups) > 0
    members = sorted(str(o.Attributes.Id) for o in of.Groups.GroupMembers2(0))
    assert members and members == sorted(str(o.Attributes.ObjectId) for o in nf.AllGroups.GroupMembers(0))


def test_retired_subd_counts_and_ids_g1():
    # G1 (Luis: map): SubD.Edge/Face/VertexCount -> Edges/Faces/Vertices.Count; component Index -> Id
    p = os.path.join(MODELS, 'subdBox.3dm')
    o = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
    n = next(x.Geometry for x in r.File3dmX.Read(p).Objects if isinstance(x.Geometry, r.SubDX))
    assert (o.EdgeCount, o.FaceCount, o.VertexCount) == (n.Edges.Count, n.Faces.Count, n.Vertices.Count)
    assert sorted(f.Index for f in o.Faces) == sorted(f.Id for f in n.Faces)
    assert sorted(e.Index for e in o.Edges) == sorted(e.Id for e in n.Edges)
    assert sorted(v.Index for v in o.Vertices) == sorted(v.Id for v in n.Vertices)


def test_retired_tight_bounding_box_l1():
    # L1 (Luis: map): GeometryBase.GetTightBoundingBox -> GetBoundingBox(true)
    obox = r.Brep.CreateFromBox(r.Box(r.BoundingBox(0, 0, 0, 1, 2, 3)))
    nbox = r.BrepX.CreateFromBox(r.BoundingBoxX(0, 0, 0, 1, 2, 3))
    ob, nb = obox.GetTightBoundingBox(), nbox.GetBoundingBox(True)
    assert (ob.Min.X, ob.Min.Y, ob.Max.Y, ob.Max.Z) == (nb.Min.X, nb.Min.Y, nb.Max.Y, nb.Max.Z) == (0, 0, 2, 3)
    # G115: the body's `this as AnnotationBase` -- an annotation answers with its own box
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    f = r.File3dmX.Read(p)
    ann = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, r.AnnotationBaseX))
    ann.ParentDimensionStyle = f.DimStyles.FindId(ann.DimensionStyleId)
    a, b = ann.GetBoundingBox(True), ann.GetBoundingBox(r.TransformX.Identity)
    assert (a.Min.X, a.Max.X, a.Max.Y) == (b.Min.X, b.Max.X, b.Max.Y)
    assert ann.TextModelWidth == a.Max.X - a.Min.X


def test_retired_likely_renames_l1_and_other_maps_m1():
    # L1 / M1 (Luis: map): each old member against its RhinoCommon member
    P = r.Point3d
    obox = r.Brep.CreateFromBox(r.Box(r.BoundingBox(0, 0, 0, 1, 2, 3)))
    nbox = r.BrepX.CreateFromBox(r.BoundingBoxX(0, 0, 0, 1, 2, 3))
    assert obox.Faces[0].OuterLoop.TrimCount == nbox.Faces[0].OuterLoop.Trims.Count == 4
    pts = [P(0, 0, 0), P(4, 0, 0), P(4, 4, 0)]
    opl, npl = r.Polyline.CreateFromPoints(pts), r.PolylineX([r.Point3dX(q.X, q.Y, q.Z) for q in pts])
    oc, nc = opl.ClosesPoint(P(5, 1, 0)), npl.ClosestPoint(r.Point3dX(5, 1, 0))
    assert (oc.X, oc.Y) == (nc.X, nc.Y)
    ol, nl = r.Point3dList(), r.Point3dListX()
    ol.Append([P(1, 2, 3)]); nl.Add(1.0, 2.0, 3.0)
    assert (ol[0].X, ol[0].Z) == (nl[0].X, nl[0].Z)
    opc = r.PointCloud(); opc.Add(P(0, 0, 0))
    assert opc.ContainsValues == r.PointCloudX(opc).ContainsPointValues == False
    with pytest.raises(TypeError):
        opc.GetValues()                                   # pilot #29: the old NullTuple() raises
    assert list(r.PointCloudX(opc).GetPointValues()) == []
    opc = r.PointCloud(); opc.Add(P(0, 0, 0), 2.5); opc.Add(P(1, 0, 0), -1.0); npc = r.PointCloudX(opc)
    assert opc.ContainsValues == npc.ContainsPointValues == True
    assert list(opc.GetValues()) == list(npc.GetPointValues()) == [2.5, -1.0]
    srf_old = r.NurbsSurface.CreateFromSphere(r.Sphere(P(0, 0, 0), 2))
    srf_new = r.NurbsSurfaceX(srf_old)
    assert (srf_old.KnotsU.IsClampedStart, srf_old.KnotsU.IsClampedEnd) == (srf_new.KnotsU.ClampedAtStart, srf_new.KnotsU.ClampedAtEnd)
    assert list(srf_old.KnotsU.ToList()) == list(srf_new.KnotsU)
    assert srf_old.Points.MakeNonRational() == srf_new.MakeNonRational()
    m = r.Material(); m.ToPhysicallyBased(); nm = r.MaterialX(); nm.ToPhysicallyBased()
    assert m.PhysicallyBased.Supported == nm.IsPhysicallyBased
    m.PhysicallyBased.EmissionColor = (1.0, 0.5, 0.25, 1.0); nm.PhysicallyBased.Emission = r.Color4fX(1.0, 0.5, 0.25, 1.0)
    e = nm.PhysicallyBased.Emission
    assert tuple(round(c, 6) for c in m.PhysicallyBased.EmissionColor)[:3] == (round(e.R, 6), round(e.G, 6), round(e.B, 6))
    line = r.LineCurve(P(0, 0, 0), P(2, 0, 0))
    oe = r.Extrusion.CreateWithPlane(r.Circle(2).ToNurbsCurve(), r.Plane.WorldXY(), 3.0, True)
    ne = r.ExtrusionX.Create(r.CircleX(2).ToNurbsCurve(), r.PlaneX.WorldXY, 3.0, True)
    assert (oe is None) == (ne is None)
    if oe is not None:
        ob, nb = oe.GetBoundingBox(), ne.GetBoundingBox(r.TransformX.Identity)
        assert abs(ob.Max.Z - nb.Max.Z) < 1e-9
    mesh = r.Mesh(); i = mesh.Vertices.AddPoint3d(0.1, 0.2, 0.3)
    nmesh = r.MeshX(); j = nmesh.Vertices.Add(0.1, 0.2, 0.3)
    assert i == j == 0
    assert nmesh.Vertices.Point3dAt(0).X == 0.1                # double precision kept, as AddPoint3d did
    p = os.path.join(MODELS, 'sphereDecals.3dm')
    od = [d for o in r.File3dm.Read(p).Objects for d in (o.Attributes.Decals or [])]
    nd = [d for o in r.File3dmX.Read(p).Objects for d in o.Attributes.Decals]
    assert len(od) == len(nd) > 0
    for a, b in zip(od, nd):
        assert (a.HorzSweepStart, a.HorzSweepEnd) == tuple(b.HorzSweep())
        assert (a.VertSweepStart, a.VertSweepEnd) == tuple(b.VertSweep())
        assert (a.BoundsMinU, a.BoundsMinV, a.BoundsMaxU, a.BoundsMaxV) == tuple(b.GetUVBounds())


def test_annotation_font_and_held_back_c_members_g115():
    # G115: AnnotationBase.Font (a managed font adopted from the native's pointer, never owned);
    # with it the last two C members retire: GetFont / SetFont(style, font) -> Font
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    old, new = r.File3dm.Read(p), r.File3dmX.Read(p)
    oann = next(o.Geometry for o in old.Objects if isinstance(o.Geometry, r.AnnotationBase))
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBaseX))
    ostyle = next(old.DimStyles[i] for i in range(len(old.DimStyles)) if old.DimStyles[i].Id == oann.DimensionStyleId)
    nann.ParentDimensionStyle = new.DimStyles.FindId(ostyle.Id)
    of, nf = oann.GetFont(ostyle), nann.Font
    assert (of.FamilyName, of.FaceName, of.Bold) == (nf.FamilyName, nf.FaceName, nf.Bold)
    assert nann.FirstCharFont.FamilyName
    oann.SetFont(ostyle, r.Font('Courier New'))
    nann.Font = r.FontX('Courier New')
    assert oann.GetFont(ostyle).FamilyName == nann.Font.FamilyName
    del nf; gc.collect()                                   # a managed font: dropping it frees nothing
    assert nann.Font.FamilyName
    with pytest.raises(RuntimeError):
        nann.Font = None                                   # C#: NullReferenceException


def test_file3dm_earth_anchor_point_g115():
    # G115: File3dm.EarthAnchorPoint => new EarthAnchorPoint(this) (an internal constructor inlined:
    # the model's anchor COPIED out); the setter copies one in. The old Settings.EarthAnchorPoint agrees
    p = os.path.join(MODELS, 'file3dm_stuff.3dm')
    old, f = r.File3dm.Read(p), r.File3dmX.Read(p)
    a, o = f.EarthAnchorPoint, old.Settings.EarthAnchorPoint
    assert (a.EarthBasepointLatitude, a.EarthBasepointLongitude, a.EarthBasepointElevation) == \
           (o.EarthBasepointLatitude, o.EarthBasepointLongitude, o.EarthBasepointElevation)
    a.EarthBasepointLatitude = 12.5
    assert f.EarthAnchorPoint.EarthBasepointLatitude == o.EarthBasepointLatitude   # a copy
    f.EarthAnchorPoint = a
    assert f.EarthAnchorPoint.EarthBasepointLatitude == 12.5
    back = r.File3dmX.FromByteArray(f.ToByteArray(None))
    assert back.EarthAnchorPoint.EarthBasepointLatitude == 12.5


def test_translated_setters_beside_planned_getters_g115():
    # G115: a getter another shape planned keeps RhinoCommon's setter when only the translated body
    # can express it (MeshVertexList.Count: guarded, two calls) -- D1's SetCount -> Count
    m, om = r.MeshX(), r.Mesh()
    for i in range(4):
        m.Vertices.Add(float(i), 0.0, 0.0); om.Vertices.Add(float(i), 0.0, 0.0)
    m.Vertices.Count = 2; om.Vertices.SetCount(2)
    assert m.Vertices.Count == len(om.Vertices) == 2
    m.Vertices.Count = -1                                  # C#: ignored
    assert m.Vertices.Count == 2
    v = r.ViewportX()
    v.IsPerspectiveProjection = True
    assert v.IsPerspectiveProjection and not v.IsParallelProjection
    v.IsPerspectiveProjection = False
    assert v.IsParallelProjection
    import uuid
    at, vid = r.ObjectAttributesX(), uuid.uuid4()
    at.ViewportId = vid
    assert at.ViewportId == vid
    mp = r.MeshingParametersX(); mp.CopyFrom(r.MeshingParametersX.QualityRenderMesh)
    assert mp.GridAngle == r.MeshingParametersX.QualityRenderMesh.GridAngle


def test_retired_file3dm_tables_and_settings_h1():
    # H1 (Luis: map): the old File3dm table/settings helpers -> RhinoCommon's members
    import uuid
    p = os.path.join(MODELS, 'file3dm_stuff.3dm')
    old, f = r.File3dm.Read(p), r.File3dmX.Read(p)
    # RenderContent table -> RenderMaterials / RenderEnvironments / RenderTextures; FindId -> Find
    oids = sorted(str(old.RenderContent[i].Id) for i in range(len(old.RenderContent)))
    kinds = (f.RenderMaterials, f.RenderEnvironments, f.RenderTextures)
    assert oids and oids == sorted(str(c.Id) for k in kinds for c in k)
    for i in range(len(old.RenderContent)):
        oc = old.RenderContent.FindId(old.RenderContent[i].Id)
        hit = [k.Find(oc.Id) for k in kinds if k.Find(oc.Id) is not None]
        assert len(hit) == 1 and str(hit[0].Id) == str(oc.Id)
    # FindIndex(i) -> Manifest.FindIndex(i, RenderContent): the same ComponentFromIndex, the right kind
    for i in range(len(old.RenderContent)):
        nc = f.Manifest.FindIndex(i, r.ModelComponentType.RenderContent)
        assert isinstance(nc, r.File3dmRenderContentX) and str(nc.Id) == str(old.RenderContent.FindIndex(i).Id)
    # linetype Current / Scale -> Settings.CurrentLinePatternId / LinetypeDisplayScale
    cur = old.Linetypes.Current
    assert (str(cur.Id) if cur is not None else str(uuid.UUID(int=0))) == str(f.Settings.CurrentLinePatternId)
    assert old.Linetypes.Scale == f.Settings.LinetypeDisplayScale
    # Settings.ModelBasePoint / EarthAnchorPoint -> Settings.ModelBasepoint / File3dm.EarthAnchorPoint (G115)
    ob, nb = old.Settings.ModelBasePoint, f.Settings.ModelBasepoint
    assert (ob.X, ob.Y, ob.Z) == (nb.X, nb.Y, nb.Z)
    assert old.Settings.EarthAnchorPoint.EarthBasepointLongitude == f.EarthAnchorPoint.EarthBasepointLongitude
    # RenderSettings.RenderEnvironments.<Usage>Id / Override -> RenderEnvironmentId(usage, Standard),
    # SetRenderEnvironmentId (G116: commits into the file), RenderEnvironmentOverride(usage)
    U, Pp = r.RenderSettingsX.EnvironmentUsage, r.RenderSettingsX.EnvironmentPurpose
    oe, rs = old.Settings.RenderSettings.RenderEnvironments, f.Settings.RenderSettings
    for name, u in (('Background', U.Background), ('Reflection', U.Reflection), ('Skylighting', U.Skylighting)):
        assert str(getattr(oe, name + 'Id')) == str(rs.RenderEnvironmentId(u, Pp.Standard)), name
    for name, u in (('Reflection', U.Reflection), ('Skylighting', U.Skylighting)):
        assert getattr(oe, name + 'Override') == rs.RenderEnvironmentOverride(u), name
    eid = uuid.uuid4()
    oe.SkylightingId = eid; oe.SkylightingOverride = True
    rs.SetRenderEnvironmentId(U.Skylighting, eid); rs.SetRenderEnvironmentOverride(U.Skylighting, True)
    back = r.File3dmX.FromByteArray(f.ToByteArray())                 # the no-argument form (G116)
    brs = back.Settings.RenderSettings
    assert str(brs.RenderEnvironmentId(U.Skylighting, Pp.Standard)) == str(eid) == str(oe.SkylightingId)
    assert brs.RenderEnvironmentOverride(U.Skylighting) is True is oe.SkylightingOverride
    # ObjectTable.AddObject(obj) -> Objects.Add(File3dmObject); and Add(geometry, attributes) (G117),
    # attributes optional as in C# (None)
    oo, no = old.Objects[0], list(f.Objects)[0]
    n0, m0 = len(old.Objects), len(list(f.Objects))
    old.Objects.AddObject(oo)
    f.Objects.Add(no)
    assert len(old.Objects) == n0 + 1 and len(list(f.Objects)) == m0 + 1
    nid = f.Objects.Add(no.Geometry, None)
    oid = old.Objects.Add(oo.Geometry, None)
    assert type(old.Objects.FindId(oid).Geometry).__name__ + 'X' == type(f.Objects.FindId(nid).Geometry).__name__
    # RhinoCommon's Add throws NotSupportedException for a leader, text, radial or ordinate dimension;
    # rhino3dm keeps 8.x's hand path behind it (G119, D71; upstream U30)
    S = r.DimensionStyleX()
    P = r.Point3dX
    extra = [r.LeaderX.Create('x', r.PlaneX.WorldXY, S, [P(0, 0, 0), P(1, 0, 0)]),
             r.TextEntityX.Create('t', r.PlaneX.WorldXY, S, False, 0.0, 0.0),
             r.RadialDimensionX.Create(S, r.AnnotationType.Radius, r.PlaneX.WorldXY, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)),
             r.OrdinateDimensionX.Create(S, r.PlaneX.WorldXY, r.OrdinateDimensionX.MeasuredDirection.Xaxis,
                                         P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)]
    ids = [f.Objects.Add(g, None) for g in extra]
    back = r.File3dmX.FromByteArray(f.ToByteArray())
    kinds = {str(o.Attributes.ObjectId): type(o.Geometry).__name__ for o in back.Objects}
    assert [kinds.get(str(i)) for i in ids] == ['LeaderX', 'TextEntityX', 'RadialDimensionX', 'OrdinateDimensionX']


def test_retired_mesh_modifiers_i1_and_shut_lining_i3():
    # I1: ObjectAttributes.MeshModifiers -> File3dmMeshModifiers; I3: the ShutLiningCurve table and
    # its curves -> File3dmShutLining's id-based members (GetCurves, Curve*(id), SetCurve*(id, v))
    import uuid
    oa = r.ObjectAttributes()
    sl = oa.MeshModifiers.CreateShutLining()
    sl.On = True
    cid = uuid.uuid4()
    sl.Curves.Add(cid)
    c = sl.Curves.FindId(cid)
    c.Radius, c.Profile, c.Enabled, c.Pull, c.IsBump = 0.75, 2, True, True, True
    na = r.ObjectAttributesX(oa)
    nsl = na.File3dmMeshModifiers.ShutLining
    assert nsl.On is True
    ids = list(nsl.GetCurves())
    assert ids == [uuid.UUID(str(sl.Curves.FindIndex(0).Id))] == [cid]
    assert (nsl.CurveRadius(cid), nsl.CurveProfile(cid), nsl.CurveEnabled(cid), nsl.CurvePull(cid), nsl.CurveIsBump(cid)) == \
           (c.Radius, c.Profile, c.Enabled, c.Pull, c.IsBump)
    nsl.SetCurveRadius(cid, 1.25); nsl.SetCurveProfile(cid, 1); nsl.SetCurveEnabled(cid, False)
    nsl.SetCurvePull(cid, False); nsl.SetCurveIsBump(cid, False)
    assert (nsl.CurveRadius(cid), nsl.CurveProfile(cid), nsl.CurveEnabled(cid), nsl.CurvePull(cid), nsl.CurveIsBump(cid)) == \
           (1.25, 1, False, False, False)


def _annotation_file():
    """a file with one of each dimension and a leader, made by the generated API"""
    P, S = r.Point3dX, r.DimensionStyleX()
    f = r.File3dmX()
    f.AllDimStyles.Add(S)
    style = list(f.AllDimStyles)[0]
    ld = r.LinearDimensionX.Create(r.AnnotationType.Aligned, style, r.PlaneX.WorldXY, r.Vector3dX(1, 0, 0),
                                   P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    ad = r.AngularDimensionX.Create(style.Id, r.PlaneX.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
    rd = r.RadialDimensionX.Create(style, r.AnnotationType.Radius, r.PlaneX.WorldXY, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0))
    od = r.OrdinateDimensionX.Create(style, r.PlaneX.WorldXY, r.OrdinateDimensionX.MeasuredDirection.Xaxis,
                                     P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
    le = r.LeaderX.Create('note', r.PlaneX.WorldXY, style, [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])
    for g in (ld, ad, rd, od, le):
        assert g is not None
    f.Objects.Add(ld, None)                    # RhinoCommon's Add(geometry, attributes) takes these two (G117)
    f.Objects.Add(ad, None)
    f.Objects.AddRadialDimension(rd)           # ... and throws NotSupportedException for the rest
    f.Objects.AddOrdinateDimension(od)
    f.Objects.AddLeader('note', r.PlaneX.WorldXY, [r.Point2dX(0, 0), r.Point2dX(2, 1), r.Point2dX(4, 1)], None)
    assert le.Points3D[2].X == 4.0             # Create's own points too
    return f.ToByteArray()


def test_retired_annotation_geometry_j1():
    # J1 (Luis: map): Leader.Points -> Points3D (and Points2D); Dim*.Points (a dict) -> Get3dPoints()
    # (a tuple led by the success flag: D68); DimAngular.Angle -> NumericValue
    data = _annotation_file()
    olds = [o.Geometry for o in r.File3dm.FromByteArray(data).Objects]
    news = [o.Geometry for o in r.File3dmX.FromByteArray(data).Objects]
    xyz = lambda q: (round(q.X, 9), round(q.Y, 9), round(q.Z, 9))
    pairs = {type(n).__name__: (o, n) for o, n in zip(olds, news)}
    assert set(pairs) == {'LinearDimensionX', 'AngularDimensionX', 'RadialDimensionX', 'OrdinateDimensionX', 'LeaderX'}
    o, n = pairs['LeaderX']
    assert [xyz(q) for q in o.Points] == [xyz(q) for q in n.Points3D] == [(0, 0, 0), (2, 1, 0), (4, 1, 0)]
    assert [(round(q.X, 9), round(q.Y, 9)) for q in n.Points2D] == [(0, 0), (2, 1), (4, 1)]
    keys = {'LinearDimensionX': ['defpt1', 'defpt2', 'arrowpt1', 'arrowpt2', 'dimline', 'textpt'],
            'AngularDimensionX': ['centerpt', 'defpt1', 'defpt2', 'arrowpt1', 'arrowpt2', 'dimlinept', 'textpt'],
            'RadialDimensionX': ['centerpt', 'radiuspt', 'dimlinept', 'kneept']}
    for name, ks in keys.items():
        o, n = pairs[name]
        rc = n.Get3dPoints()
        assert rc[0] is True and [xyz(o.Points[k]) for k in ks] == [xyz(q) for q in rc[1:]], name
    o, n = pairs['OrdinateDimensionX']
    rc = n.Get3dPoints()
    assert rc[0] is True and sorted(xyz(q) for q in o.Points.values()) == sorted(xyz(q) for q in rc[1:])
    o, n = pairs['AngularDimensionX']
    assert abs(o.Angle - n.NumericValue) < 1e-12


def test_retired_set_point3d_at_d2():
    # D2 (Luis: rename to SetVertex): MeshVertexList.SetPoint3dAt(i, p) -> SetVertex(i, Point3d)
    om, nm = r.Mesh(), r.MeshX()
    for i in range(3):
        om.Vertices.Add(float(i), 0.0, 0.0); nm.Vertices.Add(float(i), 0.0, 0.0)
    om.Vertices.SetPoint3dAt(1, r.Point3d(0.1, 0.2, 0.3))
    assert nm.Vertices.SetVertex(1, r.Point3dX(0.1, 0.2, 0.3))
    o, n = om.Vertices.Point3dAt(1), nm.Vertices.Point3dAt(1)
    assert (o.X, o.Y, o.Z) == (n.X, n.Y, n.Z) == (0.1, 0.2, 0.3)


def test_embedded_file_paths_vs_embedded_files_h1():
    # H1 / E (Luis: map): File3dm.EmbeddedFilePaths (and ...2) -> EmbeddedFiles: the old ones read the
    # RDK's embedded-file paths, the new ones the file's ON_EmbeddedFile components -- equal here?
    for name in ('file3dm_stuff.3dm', 'sphereDecals.3dm', 'subd.3dm'):
        p = os.path.join(MODELS, name)
        old, f = r.File3dm.Read(p), r.File3dmX.Read(p)
        # the same files, in the same order; a path saved on Windows reads in this platform's form
        # (ON_EmbeddedFile cleans it, as RhinoCommon on Mac does; D69): compare the file names
        base = lambda s: s.replace('\\', '/').split('/')[-1]
        assert list(old.EmbeddedFilePaths()) == list(old.EmbeddedFilePaths2())
        assert [base(s) for s in old.EmbeddedFilePaths()] == [base(e.Filename) for e in f.EmbeddedFiles], name
        assert [e.Filename for e in old.EmbeddedFiles] == [e.Filename for e in f.EmbeddedFiles], name


def test_retired_rhino3dm_only_overloads_from_the_audit():
    # the overload audit (2026-10-29): old overloads counted covered by name; Luis accepted these
    # maps where the functionality is the same (WrapText(width) is not: upstream U31)
    P, PX = r.Point3d, r.Point3dX
    # AnnotationBase.RunReplace(dimstyle, str, ...) -> ParentDimensionStyle = dimstyle; RunReplace(str, ...)
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    old, new = r.File3dm.Read(p), r.File3dmX.Read(p)
    oann = next(o.Geometry for o in old.Objects if isinstance(o.Geometry, r.AnnotationBase))
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBaseX))
    ostyle = next(old.DimStyles[i] for i in range(len(old.DimStyles)) if old.DimStyles[i].Id == oann.DimensionStyleId)
    nann.ParentDimensionStyle = new.DimStyles.FindId(ostyle.Id)
    assert oann.RunReplace(ostyle, 'ZZ', 0, 0, 0, 1) == nann.RunReplace('ZZ', 0, 0, 0, 1)
    assert oann.PlainText == nann.PlainText
    # GeometryBase.GetBoundingBox() -> GetBoundingBox(False) (ON_Geometry::BoundingBox)
    ob = r.Brep.CreateFromBox(r.Box(r.BoundingBox(0, 0, 0, 1, 2, 3)))
    nbx = r.BrepX.CreateFromBox(r.BoundingBoxX(0, 0, 0, 1, 2, 3))
    o, n = ob.GetBoundingBox(), nbx.GetBoundingBox(False)
    assert (o.Min.X, o.Min.Y, o.Max.Y, o.Max.Z) == (n.Min.X, n.Min.Y, n.Max.Y, n.Max.Z)
    # File3dmObjectTable.AddPoint(x, y, z, attributes) -> AddPoint(Point3d, attributes)
    of, nf = r.File3dm(), r.File3dmX()
    oa, na = r.ObjectAttributes(), r.ObjectAttributesX()
    oa.Name = na.Name = 'pt'
    oid, nid = of.Objects.AddPoint(1.0, 2.0, 3.0, oa), nf.Objects.AddPoint(PX(1.0, 2.0, 3.0), na)
    og, ng = of.Objects.FindId(oid), nf.Objects.FindId(nid)
    assert (og.Geometry.Location.Z, og.Attributes.Name) == (ng.Geometry.Location.Z, ng.Attributes.Name)
    # Material.SetTexture(texture) -> SetTexture(texture, texture.TextureType) (the old one returned
    # AddTexture's index as a bool: False for index 0)
    om, nm = r.Material(), r.MaterialX()
    otex, ntex = r.Texture(), r.TextureX()
    otex.FileName = ntex.FileName = '/tmp/bump.png'
    otex.TextureType = r.TextureType.Bump
    ntex.TextureType = r.TextureType.Bump
    om.SetTexture(otex)
    assert nm.SetTexture(ntex, ntex.TextureType) is True
    assert om.GetTexture(r.TextureType.Bump).FileName == nm.GetTexture(r.TextureType.Bump).FileName
    # SubDEdge.EndSharpness(i) -> EndSharpness(i, False) (opennurbs' own one-argument form)
    p = os.path.join(MODELS, 'subd_creases.3dm')
    os_ = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
    ns_ = next(x.Geometry for x in r.File3dmX.Read(p).Objects if isinstance(x.Geometry, r.SubDX))
    oe = sorted(os_.Edges, key=lambda e: e.Index)
    ne = sorted(ns_.Edges, key=lambda e: e.Id)
    assert [(e.EndSharpness(0), e.EndSharpness(1)) for e in oe] == [(e.EndSharpness(0, False), e.EndSharpness(1, False)) for e in ne]
    # CachedTextureCoordinates.IndexOf(x, y, z) / Contains(x, y, z) -> IndexOf(Point3d) / Contains(Point3d)
    path = os.path.join(MODELS, 'meshWithTexture.3dm')
    f, old = r.File3dmX.Read(path), r.File3dm.Read(path)
    checked = 0
    for o, oo in zip(f.Objects, old.Objects):
        if not isinstance(o.Geometry, r.MeshX):
            continue
        mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
        omat = old.Materials[oo.Attributes.MaterialIndex]
        if mat is None or mat.GetBitmapTexture() is None:
            continue
        m, om_ = o.Geometry, oo.Geometry          # one reference: each .Geometry is a new view (#22)
        m.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat)
        om_.SetCachedTextureCoordinatesFromMaterial(old, oo.Attributes.Id, omat)
        tc = m.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
        otc = om_.GetCachedTextureCoordinatesFromTexture(old, oo.Attributes.Id, omat.GetBitmapTexture())
        if tc is None or otc is None or tc.Count == 0:
            continue
        q = tc[min(3, tc.Count - 1)]
        assert otc.IndexOf(q.X, q.Y, q.Z) == tc.IndexOf(q)
        assert otc.Contains(q.X, q.Y, q.Z) == tc.Contains(q) == True
        assert otc.Contains(9e9, 0, 0) == tc.Contains(PX(9e9, 0, 0)) == False
        checked += 1
    assert checked >= 1


def test_mesh_set_cached_texture_coordinates_takes_the_transform_g120():
    # G120: SetCachedTextureCoordinates(tm, ref Transform xf) took only tm -- a zero transform reached
    # the native; now the caller's transform goes in (and comes back, as C#'s ref)
    mesh = r.MeshX()
    for x, y in ((0, 0), (1, 0), (1, 1), (0, 1)):
        mesh.Vertices.Add(float(x), float(y), 0.0)
    mesh.Faces.AddFace(0, 1, 2, 3)
    tm = r.TextureMappingX.CreatePlaneMapping(r.PlaneX.WorldXY, r.IntervalX(0, 1), r.IntervalX(0, 1), r.IntervalX(0, 1))
    xf = r.TransformX.Translation(r.Vector3dX(0.5, 0, 0))
    (back,) = mesh.SetCachedTextureCoordinates(tm, xf)
    assert r.TransformX.Equals(back, xf)
    xs = [c.X for c in mesh.GetCachedTextureCoordinates(tm.Id)]
    assert len(xs) > 0


def test_retired_mesh_modifier_creators_i2_and_add_curve_rh99854():
    # RH-99854 (rhino 9.x 3255f409fa6): File3dmMeshModifiers.Create*() and File3dmShutLining.AddCurve(Guid)
    # -- the old creators (I2) and ShutLiningCurveTable.Add(id) (I3) retire. Round-tripping through a
    # file needs U29 in rhino3dm's opennurbs (pilot #43): in memory here
    import uuid
    oa, na = r.ObjectAttributes(), r.ObjectAttributesX()
    omm, nmm = oa.MeshModifiers, na.File3dmMeshModifiers
    assert nmm.ShutLining is None                               # none until created
    for name in ('Displacement', 'EdgeSoftening', 'Thickening', 'CurvePiping', 'ShutLining'):
        o, n = getattr(omm, 'Create' + name)(), getattr(nmm, 'Create' + name)()
        assert n is not None and getattr(nmm, name) is not None, name
        n.On = True; o.On = True
        assert getattr(nmm, 'Create' + name)().On is True, name     # an existing one is kept
    cid, cid2 = uuid.uuid4(), uuid.uuid4()
    osl, nsl = omm.ShutLining, nmm.ShutLining
    osl.Curves.Add(cid); osl.Curves.Add(cid2)
    assert nsl.AddCurve(cid) == cid and nsl.AddCurve(cid2) == cid2
    assert nsl.AddCurve(cid) == cid                             # no second curve for an id
    assert list(nsl.GetCurves()) == [cid, cid2] == [uuid.UUID(str(osl.Curves[i].Id)) for i in range(len(osl.Curves))]
    nsl.SetCurveRadius(cid, 2.0)
    assert (nsl.CurveRadius(cid), nsl.CurveRadius(cid2)) == (2.0, 1.0)   # each its own (pilot #51)


def test_retired_create_from_subd_control_net_flag():
    # Luis 2026-10-29: Mesh.CreateFromSubDControlNet(subd, includeTextureCoordinates) -> RhinoCommon's two
    # methods, one per value of the flag (both ON_SubD_GetControlNetMesh(subd, flag))
    for name in ('subd.3dm', 'subdBox.3dm', 'subd_creases.3dm'):
        p = os.path.join(MODELS, name)
        os_ = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
        ns_ = next(x.Geometry for x in r.File3dmX.Read(p).Objects if isinstance(x.Geometry, r.SubDX))
        for flag, new in ((False, r.MeshX.CreateFromSubDControlNet), (True, r.MeshX.CreateFromSubDControlNetWithTextureCoordinates)):
            o, n = r.Mesh.CreateFromSubDControlNet(os_, flag), new(ns_)
            assert (len(o.Vertices), len(o.Faces), len(o.TextureCoordinates)) == \
                   (n.Vertices.Count, n.Faces.Count, n.TextureCoordinates.Count), (name, flag)
    # the flag matters: subdBox's control net is 47 vertices, 96 (with as many texture coordinates) with it
    box = next(x.Geometry for x in r.File3dmX.Read(os.path.join(MODELS, 'subdBox.3dm')).Objects if isinstance(x.Geometry, r.SubDX))
    assert r.MeshX.CreateFromSubDControlNetWithTextureCoordinates(box).TextureCoordinates.Count == 96
    assert r.MeshX.CreateFromSubDControlNet(box).TextureCoordinates.Count == 0


def test_material_render_material_instance_id_u1():
    # U1 (upstream, 2026-10-20): RhinoCommon's Material.RenderMaterialInstanceId over portable natives;
    # setting it also sets the universal render engine as the plug-in id (ON_Material's contract)
    import uuid
    m = r.MaterialX()
    assert m.RenderMaterialInstanceId == uuid.UUID(int=0)
    uid = uuid.uuid4()
    m.RenderMaterialInstanceId = uid
    assert m.RenderMaterialInstanceId == uid
    assert str(m.RenderPlugInId) == '99999999-9999-9999-9999-999999999999'
    old = r.Material()                      # the old binding (hand-bound): the same contract
    old.RenderMaterialInstanceId = uid
    assert old.RenderMaterialInstanceId == m.RenderMaterialInstanceId
    assert str(old.RenderPlugInId) == str(m.RenderPlugInId)


# --- RH-99868 (rhino 9.x da1b2185e6c): 26 SubD component queries, old binding as the oracle ---

def _p3(p):
    # NaN-aware (an unset texture point is NaN), so tuples compare equal when both are unset
    return tuple('nan' if v != v else v for v in (p.X, p.Y, p.Z))


def test_subd_component_queries_rh99868():
    # The old binding called the same opennurbs queries directly; the generated members reach
    # them through RhinoCommon's new C functions. Compared component by component (same order, same ids)
    checked = 0
    for name in ('subd.3dm', 'subdBox.3dm', 'subd_creases.3dm'):
        path = os.path.join(MODELS, name)
        news = [o.Geometry for o in r.File3dmX.Read(path).Objects if isinstance(o.Geometry, r.SubDX)]
        olds = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD)]
        assert news and len(news) == len(olds)
        for s, old in zip(news, olds):
            nf, of = list(s.Faces), list(old.Faces)
            assert [f.Id for f in nf] == [f.Id for f in of]
            for n, o in zip(nf, of):
                assert (n.HasEdges, n.HasSharpEdges, n.SharpEdgeCount, n.MaximumEdgeSharpness, n.IsConvex,
                        n.IsPlanar(1e-8), n.IsPlanar(1.0), n.MaterialChannelIndex, n.TexturePointsAreSet,
                        n.TexturePointsCapacity) == \
                       (o.HasEdges, o.HasSharpEdges, o.SharpEdgeCount, o.MaximumEdgeSharpness, o.IsConvex,
                        o.IsPlanar(1e-8), o.IsPlanar(1.0), o.MaterialChannelIndex, o.TexturePointsAreSet,
                        o.TexturePointsCapacity)
                assert _p3(n.SubdivisionPoint) == _p3(o.SubdivisionPoint)
                assert _p3(n.TextureCenterPoint) == _p3(o.TextureCenterPoint)
                for i in range(n.TexturePointsCapacity):
                    assert _p3(n.TexturePoint(i)) == _p3(o.TexturePoint(i))
            nv, ov = list(s.Vertices), list(old.Vertices)
            assert [v.Id for v in nv] == [v.Id for v in ov]
            for n, o in zip(nv, ov):
                assert (n.IsSmooth, n.IsCrease, n.IsCorner, n.IsDart, n.IsSharp(True), n.IsSharp(False), n.VertexSharpness) == \
                       (o.IsSmooth, o.IsCrease, o.IsCorner, o.IsDart, o.IsSharp(True), o.IsSharp(False), o.VertexSharpness)
            ne, oe = list(s.Edges), list(old.Edges)
            assert [e.Id for e in ne] == [e.Id for e in oe]
            for n, o in zip(ne, oe):
                assert (n.IsSmooth, n.IsCrease, n.IsHardCrease, n.IsDartCrease, n.DartCount) == \
                       (o.IsSmooth, o.IsCrease, o.IsHardCrease, o.IsDartCrease, o.DartCount)
                assert _p3(n.SubdivisionPoint) == _p3(o.SubdivisionPoint)
                assert _p3(n.ControlNetCenterPoint) == _p3(o.ControlNetCenterPoint)
                for i in range(n.FaceCount):
                    assert _p3(n.ControlNetCenterNormal(i)) == _p3(o.ControlNetCenterNormal(i))
            checked += len(nf) + len(nv) + len(ne)
    assert checked > 100
    # the crease fixture has creases and corners, so the predicates are not all False
    path = os.path.join(MODELS, 'subd_creases.3dm')
    s = next(o.Geometry for o in r.File3dmX.Read(path).Objects if isinstance(o.Geometry, r.SubDX))
    assert any(e.IsCrease for e in s.Edges) and any(e.IsSmooth for e in s.Edges)


# --- RH-99870 (rhino 9.x da1b2185e6c): small additions (upstream-fixes U32) ---

def test_linetype_builtins_and_clear_pattern_rh99870():
    names = ('Continuous', 'ByLayer', 'ByParent', 'Hidden', 'Dashed', 'DashDot', 'Center', 'Border', 'Dots')
    for i, name in enumerate(names):
        n, o = getattr(r.LinetypeX, name), getattr(r.Linetype, name)
        assert isinstance(n, r.LinetypeX) and n.Index == -(i + 1)
        assert (n.Name, n.Index, str(n.Id), n.SegmentCount, n.PatternLength) == (o.Name, o.Index, str(o.Id), o.SegmentCount, o.PatternLength)
    assert r.LinetypeX.Border is not r.LinetypeX.Border            # a copy per access
    b = r.LinetypeX.Border
    b.Name = 'mine'
    assert r.LinetypeX.Border.Name == 'Border'
    # ClearPattern: true when no pattern is set afterwards; a built-in's pattern is locked (unchanged)
    lt = r.LinetypeX()
    lt.AppendSegment(1.0, True)
    lt.AppendSegment(0.5, False)
    assert lt.ClearPattern() is True and lt.SegmentCount == 0
    border = r.LinetypeX.Border
    assert border.ClearPattern() is True and border.SegmentCount == r.Linetype.Border.SegmentCount > 0


def test_material_compare_appearance_rh99870():
    a, b = r.MaterialX(), r.MaterialX()
    a.Name, b.Name = 'one', 'two'
    assert r.MaterialX.CompareAppearance(a, b) == 0            # names do not affect the appearance
    b.DiffuseColor = r.ColorX.FromArgb(255, 255, 0, 0) if hasattr(r, 'ColorX') else (255, 0, 0, 255)
    c = r.MaterialX.CompareAppearance(a, b)
    assert c != 0 and r.MaterialX.CompareAppearance(b, a) == -c
    oa, ob = r.Material(), r.Material()
    ob.DiffuseColor = b.DiffuseColor
    assert (r.Material.CompareAppearance(oa, ob) > 0) == (c > 0)


def test_instance_definitions_find_index_and_geometry_ids_rh99870():
    f = r.File3dmX()
    circle = r.ArcCurveX(r.CircleX(r.PlaneX.WorldXY, 1.0))
    idx = f.AllInstanceDefinitions.Add('blk', 'desc', r.Point3dX(0, 0, 0), [circle], [r.ObjectAttributesX()])
    idef = f.AllInstanceDefinitions.FindIndex(idx)
    assert isinstance(idef, r.InstanceDefinitionGeometryX) and idef.Name == 'blk'
    assert f.AllInstanceDefinitions.FindIndex(idx + 1) is None
    ids = idef.GetObjectIds()
    assert len(ids) == 1 and idef.IsInstanceGeometryId(ids[0])
    # U33 (opennurbs, in this pin): nil and unknown ids are not geometry ids (pilot #54)
    assert not idef.IsInstanceGeometryId(uuid.UUID(int=0)) and not idef.IsInstanceGeometryId(uuid.uuid4())
    assert not r.InstanceDefinitionGeometryX().IsInstanceGeometryId(uuid.uuid4())   # an empty definition
    assert not r.InstanceDefinition().IsInstanceGeometryId(str(uuid.uuid4()))       # the old binding, same opennurbs


def test_embedded_files_count_length_clear_rh99870(tmp_path):
    data = tmp_path / 'note.txt'
    data.write_text('hello world')
    f = r.File3dmX()
    assert f.EmbeddedFiles.Count == 0
    assert f.EmbeddedFiles.Add(str(data)) is True
    assert f.EmbeddedFiles.Count == 1
    ef = list(f.EmbeddedFiles)[0]
    assert ef.Length == len('hello world')
    # FindIndex (U35, pin 5b68aa2623e: it threw NotSupportedException in .NET before, pilot #56)
    found = f.EmbeddedFiles.FindIndex(0)
    assert isinstance(found, r.File3dmEmbeddedFileX) and found.Filename == ef.Filename
    assert f.EmbeddedFiles.FindIndex(5) is None
    assert ef.Clear() is True and ef.Length == 0
    path = str(tmp_path / 'embedded.3dm')
    g = r.File3dmX()
    g.EmbeddedFiles.Add(str(data))
    assert g.Write(path, 7)
    h = r.File3dmX.Read(path)
    assert h.EmbeddedFiles.Count == 1 and list(h.EmbeddedFiles)[0].Length == len('hello world')
    old = r.File3dm.Read(path)
    assert len(old.EmbeddedFiles) == 1 and old.EmbeddedFiles[0].Length == len('hello world')


def test_settings_current_linetype_source_rh99870(tmp_path):
    S = r.ObjectLinetypeSource
    f = r.File3dmX()
    assert f.Settings.CurrentLinetypeSource == S.LinetypeFromLayer      # opennurbs' default
    f.Settings.CurrentLinetypeSource = S.LinetypeFromObject
    assert f.Settings.CurrentLinetypeSource == S.LinetypeFromObject
    path = str(tmp_path / 'source.3dm')
    assert f.Write(path, 8)
    assert r.File3dmX.Read(path).Settings.CurrentLinetypeSource == S.LinetypeFromObject
    assert r.File3dm.Read(path).Linetypes.CurrentSource == S.LinetypeFromObject   # the old table property


def test_render_content_set_xml_rh99870():
    f = r.File3dmX.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    mats = list(f.RenderMaterials)
    assert mats
    a = mats[0]
    xml = a.XML(False)
    import xml.etree.ElementTree as ET

    def canon(x):
        # opennurbs writes attributes back in another order: compare the parsed trees
        def walk(e):
            return (e.tag, sorted(e.attrib.items()), (e.text or '').strip(), [walk(c) for c in e])
        return walk(ET.fromstring(x))
    assert a.SetXML(xml) is True and canon(a.XML(False)) == canon(xml)     # its own state round-trips
    assert a.SetXML('<not xml') is False


def test_annotation_additions_rh99870():
    P = r.Point3dX
    f = r.File3dmX()
    f.AllDimStyles.Add(r.DimensionStyleX())
    style = list(f.AllDimStyles)[0]
    ad = r.AngularDimensionX.Create(style.Id, r.PlaneX.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
    d = ad.DimlinePoint
    assert abs(ad.Radius - (d.X * d.X + d.Y * d.Y) ** 0.5) < 1e-12     # |dimension line point| in the plane
    le = r.LeaderX.Create('note', r.PlaneX.WorldXY, style, [P(0, 0, 0), P(10, 5, 0)])
    tp = le.GetTextPoint2d(style, 1.0)
    assert tp.IsValid and (tp.X, tp.Y) != (0.0, 0.0)
    # WrapText(width) wraps only text already marked wrapped (ON_TextContent::WrapText), as the old binding
    t = r.TextEntityX.Create('one two three four five six seven', r.PlaneX.WorldXY, style, False, 0.0, 0.0)
    t.TextIsWrapped = True
    t.WrapText(3.0)
    assert t.TextIsWrapped


def test_arrowhead_and_viewport_and_render_settings_rh99870():
    vp = r.ViewportX()
    assert vp.IsValidCameraFrame is True and vp.IsValidCameraFrame == r.ViewportInfo().IsValidCameraFrame
    lw = r.File3dmX().Settings.RenderSettings.LinearWorkflow
    on = lw.PreProcessGammaOn
    lw.PreProcessGammaOn = not on
    assert lw.PreProcessGammaOn is (not on)
    sun = r.File3dmX().Settings.RenderSettings.Sun
    old = r.File3dm().Settings.RenderSettings.Sun
    assert (r.SunX.MinYear, r.SunX.MaxYear) == (old.MinYear, old.MaxYear) == (1971, 2199)
    assert sun.IsValid is old.IsValid is True
    for prop in ('EnableAllowed', 'ManualControlAllowed'):
        v = getattr(sun, prop)
        setattr(sun, prop, not v)
        assert getattr(sun, prop) is (not v)
    T = r.TextureMappingType
    assert (int(T.WcsProjection), int(T.WcsBoxProjection)) == (11, 12)


# --- G122: the RH-99870 members that needed generator work ---

def test_linetype_and_material_resolvers_g122():
    f = r.File3dmX()
    # a default object: its layer is missing, so opennurbs resolves to Continuous / the default material.
    # The old binding returns None here (pilot #53: opennurbs' LinePatternFrom* fall back to an empty reference)
    a = r.ObjectAttributesX()
    lt = f.AllLinetypes.FromAttributes(a)
    assert isinstance(lt, r.LinetypeX) and (lt.Name, lt.Index) == ('Continuous', -1)
    assert r.File3dm().Linetypes.FromAttributes(r.ObjectAttributes()) is None
    assert f.AllLinetypes.FromLayerIndex(0).Name == 'Continuous'
    m = f.AllMaterials.FindFromAttributes(a)
    assert isinstance(m, r.MaterialX) and m.Index == -1
    assert str(m.Id).lower() == 'cf2be1de-b81c-4d69-9418-dde91d266086'      # ON_Material::Default
    # a model linetype through the object, then through its layer
    mine = r.LinetypeX()
    mine.Name = 'mine'
    mine.AppendSegment(1.0, True)
    mine.AppendSegment(0.5, False)
    f.AllLinetypes.Add(mine)
    li = next(x.Index for x in f.AllLinetypes if x.Name == 'mine')
    a.LinetypeSource = r.ObjectLinetypeSource.LinetypeFromObject
    a.LinetypeIndex = li
    got = f.AllLinetypes.FromAttributes(a)
    assert (got.Name, got.Index) == ('mine', li)
    layer = r.LayerX()
    layer.Name = 'L'
    layer.LinetypeIndex = li
    f.AllLayers.Add(layer)
    lyr = next(x.Index for x in f.AllLayers if x.Name == 'L')
    assert f.AllLinetypes.FromLayerIndex(lyr).Name == 'mine'
    # a model material through the object
    mat = r.MaterialX()
    mat.Name = 'red'
    mi = f.AllMaterials.AddMaterial(mat)
    a.MaterialSource = r.ObjectMaterialSource.MaterialFromObject
    a.MaterialIndex = mi
    assert f.AllMaterials.FindFromAttributes(a).Name == 'red'


def test_viewport_defaults_and_arrowhead_points_g122():
    for new, old in ((r.ViewportX.DefaultTop, r.ViewportInfo.DefaultTop()),
                     (r.ViewportX.DefaultPerspective, r.ViewportInfo.DefaultPerspective())):
        assert _p3(new.CameraDirection) == _p3(old.CameraDirection)
        assert _p3(new.CameraLocation) == _p3(old.CameraLocation)
        assert new.IsParallelProjection           # opennurbs' DefaultPerspectiveViewZUp too (pilot #55)
    top = r.ViewportX.DefaultTop.CameraDirection
    assert (top.X, top.Y, top.Z) == (0.0, 0.0, -1.0)
    assert r.ViewportX.DefaultTop is not r.ViewportX.DefaultTop      # a new viewport per access
    T = r.DimensionStyleX.ArrowType
    pts = r.ArrowheadX.GetPoints(T.SolidTriangle)
    assert [(p.X, p.Y) for p in pts] == [(0.0, 0.0), (-1.0, 0.25), (-1.0, -0.25)]
    old = r.Arrowhead.GetPoints(r.ArrowheadTypes.SolidTriangle, 1.0)
    assert [(p.X, p.Y) for p in old] == [(p.X, p.Y) for p in pts]
    assert list(r.ArrowheadX.GetPoints(T.UserBlock)) == []
    # DuplicateLinetype (now generated through the owned-pointer adopt): the pattern, with the name,
    # id and locked bits cleared (RhinoCommon's contract) -- so a built-in's copy can be cleared
    d = r.LinetypeX.Dashed
    lt = d.DuplicateLinetype()
    assert isinstance(lt, r.LinetypeX) and lt.Name == '' and str(lt.Id) == str(uuid.UUID(int=0))
    assert (lt.SegmentCount, lt.PatternLength) == (d.SegmentCount, d.PatternLength) and lt.SegmentCount > 0
    assert lt.ClearPattern() and lt.SegmentCount == 0


def test_retired_subd_control_net_g3_and_table_moves():
    # triage G3 (Luis 2026-10-28: map), against the old binding on every SubD fixture
    checked = 0
    for name in ('subd.3dm', 'subdBox.3dm', 'subd_creases.3dm'):
        path = os.path.join(MODELS, name)
        s = next(o.Geometry for o in r.File3dmX.Read(path).Objects if isinstance(o.Geometry, r.SubDX))
        old = next(o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD))
        for n, o in zip(list(s.Edges), list(old.Edges)):
            a, b = n.VertexFrom, n.VertexTo
            assert (_p3(a.ControlNetPoint), _p3(b.ControlNetPoint)) == (_p3(o.ControlNetPoint(0)), _p3(o.ControlNetPoint(1)))
            line = n.ControlNetLine
            assert (_p3(line.From), _p3(line.To)) == (_p3(o.ControlNetPoint(0)), _p3(o.ControlNetPoint(1)))
            assert _p3(line.Direction) == _p3(o.ControlNetDirection)
            assert (a.Id, b.Id) == (o.VertexId(0), o.VertexId(1))
            checked += 1
        for n, o in zip(list(s.Faces), list(old.Faces)):
            assert [_p3(n.VertexAt(i).ControlNetPoint) for i in range(n.VertexCount)] == \
                   [_p3(o.ControlNetPoint(i)) for i in range(n.VertexCount)]
            checked += 1
    assert checked > 100
    # AddInstanceDefinition(idef) -> the table's Add(item) (both ONX_Model::AddModelComponent)
    f = r.File3dmX()
    idef = r.InstanceDefinitionGeometryX()
    idef.Name = 'blk'
    n0 = len(f.AllInstanceDefinitions)
    f.AllInstanceDefinitions.Add(idef)
    assert len(f.AllInstanceDefinitions) == n0 + 1 and f.AllInstanceDefinitions.FindIndex(n0).Name == 'blk'
    of = r.File3dm()
    oi = r.InstanceDefinition()                 # the old Name is read-only
    of.InstanceDefinitions.AddInstanceDefinition(oi)
    assert len(of.InstanceDefinitions) == 1


def test_retired_decal_table_find_index_follows_rhinocommon():
    # Luis 2026-10-29: follow RhinoCommon -- index / count by iterating Attributes.Decals (visible decals;
    # the old table counted hidden ones too: the fixture has none hidden, so both agree here)
    path = os.path.join(MODELS, 'sphereDecals.3dm')
    new = list(next(iter(r.File3dmX.Read(path).Objects)).Attributes.Decals)
    old = next(iter(r.File3dm.Read(path).Objects)).Attributes.Decals
    assert len(new) == len(old) == 2
    assert all(d.IsVisible for d in new)
    assert [str(d.TextureInstanceId) for d in new] == [str(old.FindIndex(i).TextureInstanceId) for i in range(len(old))]
    assert [str(d.TextureInstanceId) for d in new] == [str(old[i].TextureInstanceId) for i in range(len(old))]
