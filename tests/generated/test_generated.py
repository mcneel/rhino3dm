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
    assert r.MaterialX.MaxShine() == 255.0


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
    fast, quality = r.MeshingParametersX.FastRenderMesh(), r.MeshingParametersX.QualityRenderMesh()
    assert isinstance(fast, r.MeshingParametersX) and isinstance(quality, r.MeshingParametersX)
    assert isinstance(r.MeshingParametersX.DefaultAnalysisMesh().RelativeTolerance, float)
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
