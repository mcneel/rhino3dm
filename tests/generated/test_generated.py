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
    return r.Curve.CreateControlPointCurve([r.Point3d(*xyz) for xyz in [(0, 0, 0), (1, 2, 0), (4, 1, 0), (6, -2, 0)]], 3)


def val(v):
    return v() if callable(v) else v


# --- marshalling shapes -----------------------------------------------------

def test_guard_raises_like_rhinocommon():
    with pytest.raises(IndexError):
        curve().SpanDomain(-1)


def test_holder_array_return():
    c = curve()
    assert c.SpanVector() == (c.Domain.T0, c.Domain.T1)


def test_enum_parameters_are_type_checked():
    x = curve()
    assert x.IsContinuous(r.Continuity.C2_continuous, 0.5) is True
    with pytest.raises(TypeError):
        x.IsContinuous(3, 0.5)          # a raw int is not a Continuity


def test_string_argument_roundtrip():
    # string INPUT marshalling (RH3DM_S); the name check is RhinoCommon's
    assert r.ModelComponent.IsValidComponentName("Layer 01") is True
    assert r.ModelComponent.IsValidComponentName("") is False


# --- classes ---------------------------------------------------------------

def test_abstract_bases_are_not_constructible():
    for base in (r.GeometryBase, r.ModelComponent):
        with pytest.raises(TypeError):
            base()


def test_inheritance_reaches_base_members():
    pc = r.PolyCurve()
    pc.Append(r.LineCurve((0, 0, 0), (2, 0, 0)))
    assert isinstance(pc, r.Curve) and isinstance(pc, r.GeometryBase)
    mid = pc.PointAt(1.0)                       # declared on Curve
    assert (mid.X, mid.Y, mid.Z) == (1.0, 0.0, 0.0)


def test_viewport_set_camera_location():
    v = r.ViewportInfo()
    assert v.SetCameraLocation((7, 8, 9)) is True
    loc = v.CameraLocation
    assert (loc.X, loc.Y, loc.Z) == (7.0, 8.0, 9.0)


def test_subclass_aware_wrapping():
    srf = r.Sphere(r.Point3d(0, 0, 0), 5).ToNurbsSurface()
    assert type(srf.IsoCurve(1, 0.5)).__name__ == 'NurbsCurve'


def test_class_argument_is_copied_not_adopted():
    pc = r.PolyCurve()
    seg = r.LineCurve((0, 0, 0), (1, 0, 0))
    assert pc.Append(seg) is True
    del seg
    gc.collect()                                # the C layer duplicated it
    assert pc.SegmentCount == 1


def test_rhinocommon_copy_constructor_is_deep():
    a = r.Linetype()
    a.AppendSegment(1.5, True)
    b = r.Linetype(a)
    b.AppendSegment(2.0, False)
    assert (val(a.PatternLength), val(b.PatternLength)) == (1.5, 3.5)


def test_zero_copy_point_buffer():
    np = pytest.importorskip('numpy')
    pc = r.PointCloud()
    for i in range(10):
        pc.Add(r.Point3d(i, 2 * i, 3))
    a = np.asarray(pc)
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
    c = r.LineCurve((0, 0, 0), (1, 0, 0))
    xf = r.Transform.Translation(r.Vector3d(0, 0, 5))
    assert c.Transform(xf) is True
    s = c.PointAtStart
    assert (s.X, s.Y, s.Z) == (0.0, 0.0, 5.0)


def test_finding_14_rhinocommon_value_semantics():
    q = r.Point3d(4, 6, 3)
    assert r.Point3d.Unset.DistanceTo(q) == 0.0


def test_same_arity_constructor_overloads_both_reachable():
    # LineCurve(Point2d, Point2d) and (Point3d, Point3d): pybind dispatches on
    # type, embind through a generated shape dispatcher -- neither may shadow
    # the other (generator-log G11: JS once kept only the Point2d one)
    a = r.LineCurve(r.Point3d(1, 2, 3), r.Point3d(4, 5, 6)).Line
    b = r.LineCurve(r.Point2d(1, 2), r.Point2d(4, 5)).Line
    assert (a.To.Z, b.To.Z) == (6.0, 0.0)


def test_property_setters_write_through():
    c = r.LineCurve(r.Point3d(0, 0, 0), r.Point3d(2, 0, 0))
    c.Domain = r.Interval(1, 5)                     # value type through `ref value`
    assert (c.Domain.T0, c.Domain.T1) == (1.0, 5.0)
    ds = r.DimensionStyle()
    ds.ArrowLength = 0.75                            # inlined private helper + selector
    assert ds.ArrowLength == 0.75
    with pytest.raises(AttributeError):
        c.PointAtStart = r.Point3d(1, 1, 1)         # read-only in RhinoCommon stays read-only


needs_file3dm = pytest.mark.skipif(not hasattr(r, 'File3dm'), reason='module built before File3dm')


@needs_file3dm
def test_file3dm_table_view_keeps_the_file_alive():
    # a table is a VIEW onto the file's ONX_Model (mode 'child'): the
    # binding must keep the File3dm alive while the view exists
    f = r.File3dm()
    objs = f.Objects
    del f
    gc.collect()
    objs.AddPoint(r.Point3d(1, 2, 3), None)     # optional attributes: None
    objs.AddPoint(r.Point3d(-1, 0, 5), None)
    bb = objs.GetBoundingBox()
    assert (bb.Min.X, bb.Max.Z) == (-1.0, 5.0)


@needs_file3dm
def test_file3dm_settings_view_writes_through():
    f = r.File3dm()
    f.Settings.ModelAbsoluteTolerance = 0.25     # File3dmSettings views &model->m_settings
    assert f.Settings.ModelAbsoluteTolerance == 0.25


@needs_file3dm
def test_file3dm_read_missing_file_raises_file_not_found():
    # RhinoCommon's guard: `if (!File.Exists(path)) throw new FileNotFoundException(...)`
    with pytest.raises(FileNotFoundError):
        r.File3dm.Read('/nonexistent/model.3dm')


# --- G28: logic the generator once dropped, now reproduced -------------------

def test_g28_early_returns_and_optional_arguments():
    pc = r.PolyCurve()
    assert pc.Append(None) is False                  # `if (null == curve) return false;`
    assert isinstance(r.ArcCurve(None), r.ArcCurve)   # `new ArcCurve(null)`: ON_ArcCurve_New(nullptr)
    c = r.LineCurve(r.Point3d(0, 0, 0), r.Point3d(1, 0, 0))
    assert c.Transform(r.Transform.Identity) is True     # `if (xform.IsIdentity) return true;`


# --- G29: File3dm tables (generic RhinoCommon bases instantiated per table) --

@needs_file3dm
def test_file3dm_layer_table_basics():
    f = r.File3dm()
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
    f = r.File3dm()
    f.AllLayers.AddLayer('before', (0, 0, 0, 255))
    layer = f.AllLayers.FindIndex(0)
    layer.Name = 'after'
    assert f.AllLayers.FindIndex(0).Name == 'after'
    del f
    gc.collect()
    assert layer.Name == 'after'


@needs_file3dm
def test_file3dm_table_add_copies_and_delete_by_item():
    f = r.File3dm()
    mine = r.Layer()
    mine.Name = 'mine'
    f.AllLayers.Add(mine)                            # ONX_Model_AddModelComponent copies
    mine.Name = 'changed later'
    assert f.AllLayers.FindIndex(0).Name == 'mine'
    assert f.AllLayers.Delete(None) is False         # `if (item == null) return false;`
    assert f.AllLayers.Delete(f.AllLayers.FindIndex(0)) is True


@needs_file3dm
def test_file3dm_table_find_name():
    # G37: FindName<T> through the manifest table, with RhinoCommon's guards
    f = r.File3dm()
    f.AllLayers.AddLayer('walls', (0, 0, 0, 255))
    assert f.AllLayers.FindName('walls', __import__('uuid').UUID(int=0)).Name == 'walls'
    assert f.AllLayers.FindName('nope', __import__('uuid').UUID(int=0)) is None


def test_nested_enum_lives_in_its_owner_class():
    # G35: DimensionStyle.ArrowType is DimensionStyle.ArrowType in Python
    ds = r.DimensionStyle()
    ds.ArrowType1 = r.DimensionStyle.ArrowType.Dot
    assert ds.ArrowType1 == r.DimensionStyle.ArrowType.Dot


def test_instance_definition_object_ids_holder():
    # G36: SimpleArrayGuid -> tuple of UUIDs
    assert r.InstanceDefinitionGeometry().GetObjectIds() == ()


# --- G39: File3dm table protocols (len / [i] / iteration) --------------------

@needs_file3dm
def test_table_iteration_survives_a_delete():
    # pilot-findings #24: the old bindings iterate range(len) over sparse indices
    f = r.File3dm()
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
    f = r.File3dm()
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
    f = r.File3dm()
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

needs_objects = pytest.mark.skipif(not hasattr(r, 'File3dmObject'), reason='module built before File3dmObject')


def _file_with_line():
    f = r.File3dm()
    f.Objects.AddLine(r.Point3d(0, 0, 0), r.Point3d(1, 0, 0), None)
    return f


@needs_objects
def test_objects_iterate_as_file3dmobject_proxies():
    f = _file_with_line()
    f.Objects.AddPoint(r.Point3d(5, 5, 5), None)
    objs = list(f.Objects)
    assert len(f.Objects) == 2 and len(objs) == 2
    assert isinstance(objs[0], r.File3dmObject)
    assert isinstance(objs[0].Geometry, r.LineCurve)        # the most-derived view class
    assert isinstance(objs[1].Geometry, r.Point)


@needs_objects
def test_geometry_is_copy_on_write_as_in_rhinocommon():
    # deviations.md D1 / pilot-findings #22: the old bindings edited the file in place
    f = _file_with_line()
    obj = next(iter(f.Objects))
    g = obj.Geometry
    assert g.PointAtStart.X == 0.0
    assert g.Transform(r.Transform.Translation(r.Vector3d(10, 0, 0))) is True
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

needs_rs = pytest.mark.skipif(not hasattr(r, 'RenderSettings'), reason='module built before RenderSettings')


@needs_rs
def test_render_settings_setters_commit_into_the_file():
    f = r.File3dm()
    rs = f.Settings.RenderSettings
    before = rs.ScaleBackgroundToFit
    rs.ScaleBackgroundToFit = not before                  # copy on write, then Commit() into the file
    assert rs.ScaleBackgroundToFit is (not before)        # the detached copy
    assert f.Settings.RenderSettings.ScaleBackgroundToFit is (not before)   # a fresh view of the file: committed
    rs.ShadowmapLevel = 3
    assert f.Settings.RenderSettings.ShadowmapLevel == 3


@needs_rs
def test_render_settings_written_and_read_back(tmp_path):
    # vNext copies and commits where the old bindings wrote in place: the file carries the values (the
    # same file the old bindings wrote: checked against them before the switch-over, M3)
    new = r.File3dm()
    new.Settings.RenderSettings.ShadowmapLevel = 2
    new.Settings.RenderSettings.TransparentBackground = True
    pn = str(tmp_path / 'new.3dm')
    assert new.Write(pn, 8)
    back = r.File3dm.Read(pn).Settings.RenderSettings
    assert back.ShadowmapLevel == 2 and back.TransparentBackground is True


@needs_rs
def test_render_settings_view_keeps_the_file_alive():
    f = r.File3dm()
    rs = f.Settings.RenderSettings
    del f
    gc.collect()
    rs.ShadowmapLevel = 1
    assert rs.ShadowmapLevel == 1


# --- G42: the RDK settings (GroundPlane, Sun, ...): write-through file views, Variant properties

needs_rdk = pytest.mark.skipif(not hasattr(r, 'GroundPlane'), reason='module built before GroundPlane')


@needs_rdk
def test_ground_plane_writes_through_into_the_file():
    f = r.File3dm()
    gp = f.Settings.RenderSettings.GroundPlane            # a view of the file (CppFromFile3dm)
    gp.Enabled = True
    gp.Altitude = 2.5
    gp.TextureOffset = r.Vector2d(0.25, 0.75)
    again = f.Settings.RenderSettings.GroundPlane
    assert again.Enabled is True and again.Altitude == 2.5
    assert (again.TextureOffset.X, again.TextureOffset.Y) == (0.25, 0.75)


@needs_rdk
def test_free_floating_settings_own_their_object():
    gp = r.GroundPlane()                                  # DefaultCppConstructor / DeleteCpp
    gp.Altitude = 7.0
    assert gp.Altitude == 7.0
    rs = r.RenderSettings()                               # free-floating render settings ...
    inner = rs.GroundPlane                                  # ... hand out a pointer INTO them, not a copy
    inner.Altitude = 3.0
    assert rs.GroundPlane.Altitude == 3.0


@needs_rdk
def test_sun_and_safe_frame_round_trip():
    f = r.File3dm()
    rs = f.Settings.RenderSettings
    rs.Sun.Enabled = True
    rs.SafeFrame.Enabled = True
    assert f.Settings.RenderSettings.Sun.Enabled is True
    assert f.Settings.RenderSettings.SafeFrame.Enabled is True


# --- G43: components -- (owner, key), resolved through the owner on every access

needs_comp = pytest.mark.skipif(not hasattr(r, 'BrepFace'), reason='components not generated')
MODELS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'models')


def box_brep():
    return r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))


@needs_comp
def test_brep_faces_are_components_of_the_brep():
    b = box_brep()
    faces = b.Faces
    assert len(faces) == 6
    assert [f.FaceIndex for f in faces] == list(range(6))          # sequence protocol over this[int]
    f = faces[2]
    assert isinstance(f, r.SurfaceProxy) and isinstance(f, r.Surface)   # RhinoCommon's inheritance
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
    assert e.EdgeIndex == 3 and isinstance(e, r.CurveProxy) and isinstance(e, r.Curve)
    assert b.Vertices[7].VertexIndex == 7


def subd_from_file():
    f = r.File3dm.Read(os.path.join(MODELS, 'subdBox.3dm'))
    for o in f.Objects:
        g = o.Geometry
        if isinstance(g, r.SubD):
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
    v.ControlNetPoint = r.Point3d(10, 20, 30)
    p = next(iter(s.Vertices)).ControlNetPoint
    assert (p.X, p.Y, p.Z) == (10, 20, 30)


@needs_comp
def test_point_cloud_items_and_knot_lists():
    cloud = r.PointCloud()
    for i in range(4):
        cloud.Add(r.Point3d(i, 2 * i, 0))
    item = cloud[2]
    assert item.Index == 2 and (item.Location.X, item.Location.Y) == (2, 4)
    item.Location = r.Point3d(9, 9, 9)
    assert cloud[2].Location.Z == 9
    with pytest.raises(IndexError):
        cloud[4]
    ns = r.NurbsSurface.CreateFromSphere(r.Sphere(r.Point3d(0, 0, 0), 2.0))
    ku = ns.KnotsU
    assert len(ku) == ku.Count > 0 and ku.ClampedAtStart in (True, False)
    assert ns.KnotsV.Count > 0


@needs_comp
def test_physically_based_material_is_a_conditional_child():
    m = r.Material()
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

needs_g46 = pytest.mark.skipif(not hasattr(r, 'FileReference'), reason='G46 classes not generated')


@needs_g46
def test_environment_owns_its_object_and_fills_a_new_texture():
    e = r.Environment()
    assert e.BackgroundProjection is not None
    tex = e.BackgroundImage                       # `var tex = new Texture(); F(..., tex.NonConstPointer())`
    assert isinstance(tex, r.Texture)
    assert e.BackgroundImage is not tex           # a new texture per access, as in .NET


@needs_g46
def test_file_reference_is_a_handle_with_rhinocommons_paths():
    fr = r.FileReference.CreateFromFullAndRelativePaths('/tmp/textures/wood.png', 'textures/wood.png')
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
    assert isinstance(faces[0], r.SubDComponent)
    assert s.Faces.Find(ids[0]).Id == ids[0]
    v = next(iter(s.Vertices))
    assert v.IsSelected in (False, True) and v.IsDeleted is False


@needs_g46
def test_mesh_modifiers_are_children_of_the_attributes():
    a = r.ObjectAttributes()
    mm = a.File3dmMeshModifiers
    # `if (!HasDisplacement(attr)) return null;` -- a fresh attributes object has none
    assert mm.Displacement is None and mm.EdgeSoftening is None and mm.Thickening is None
    assert mm.CurvePiping is None and mm.ShutLining is None
    # objects read from files: reachable, and present modifiers read back
    for name in ('subd.3dm', 'subd_creases.3dm', 'mesh.3dm'):
        f = r.File3dm.Read(os.path.join(MODELS, name))
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
    assert all(isinstance(t, r.BrepTrim) and isinstance(t, r.Curve) for t in trims)
    assert trims[5].TrimIndex == 5
    assert loops[0].Face.FaceIndex == 0                     # BrepLoop.Face: through the brep's face list
    with pytest.raises(IndexError):
        loops[6]


# --- G48: borrowed pointers (CachedTextureCoordinates), literal constants -----

needs_g48 = pytest.mark.skipif(not hasattr(r, 'CachedTextureCoordinates'), reason='G48 classes not generated')


@needs_g48
def test_cached_texture_coordinates_are_a_view_into_the_mesh():
    import uuid
    f = r.File3dm.Read(os.path.join(MODELS, 'meshWithTexture.3dm'))
    meshes = [o.Geometry for o in f.Objects if isinstance(o.Geometry, r.Mesh)]
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


@pytest.mark.skipif(not hasattr(r.Material, 'MaxShine'), reason='G48 not generated')
def test_literal_constant_properties():
    lt = r.Linetype()
    assert lt.IsModified is False and lt.IsPatternLocked is False    # RhinoCommon's portable `return false;`
    assert r.Material.MaxShine == 255.0


# --- G49: category N idioms (native call plus a little logic around it) -----

@pytest.mark.skipif(not hasattr(r.TextureMapping, 'CreatePlaneMapping'), reason='G49 not generated')
def test_texture_mapping_factories_return_none_on_failure():
    # `if (!SetPlaneMapping(...)) { rc.Dispose(); rc = null; }`
    i = r.Interval(0, 1)
    tm = r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, i, i, i, True)
    assert isinstance(tm, r.TextureMapping) and tm.Capped is True
    assert isinstance(r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, i, i, i), r.TextureMapping)
    assert r.TextureMapping.CreatePlaneMapping(r.Plane.Unset, i, i, i, False) is None   # origin unset
    assert isinstance(r.TextureMapping.CreateSurfaceParameterMapping(), r.TextureMapping)
    assert isinstance(r.TextureMapping.CreateSphereMapping(r.Sphere(r.Point3d(0, 0, 0), 2)), r.TextureMapping)


@pytest.mark.skipif(not hasattr(r.Extrusion, 'IsCappedAtBottom'), reason='G49 not generated')
def test_extrusion_caps_decode_the_native_flags():
    # `return (1 == rc || 3 == rc);` over ON_Extrusion::IsCapped's 0/1/2/3
    cyl = r.Cylinder(r.Circle(r.Point3d(0, 0, 0), 1.0), 4.0)
    for bottom, top in ((False, False), (True, False), (False, True), (True, True)):
        e = r.Extrusion.CreateCylinderExtrusion(cyl, bottom, top)
        assert (e.IsCappedAtBottom, e.IsCappedAtTop) == (bottom, top)
        assert (e.IsMiteredAtStart, e.IsMiteredAtEnd) == (False, False)


@pytest.mark.skipif(not hasattr(r.Extrusion, 'IsCappedAtBottom'), reason='G49 not generated')
def test_out_param_tuples_keep_the_csharp_result():
    # G49: a non-bool C# result leads the tuple (it used to be dropped)
    a = r.Circle(r.Point3d(0, 0, 0), 1.0)
    b = r.Circle(r.Point3d(1, 0, 0), 1.0)
    res = r.Intersection.CircleCircle(a, b)
    assert len(res) == 3 and isinstance(res[0], r.CircleCircleIntersection) and int(res[0]) == 2   # Multiple
    pc = r.PolyCurve()
    seg = pc.SegmentIndexes(r.Interval(0, 1))
    assert len(seg) == 3 and isinstance(seg[0], int)


@pytest.mark.skipif(not hasattr(r.Texture, 'UvwTransform'), reason='G49 not generated')
def test_value_out_initializers_are_reproduced():
    # `Transform value = new Transform(1); ON_Texture_uvw(p, ref value);`
    xf = r.Texture().UvwTransform
    assert [xf.M00, xf.M11, xf.M22, xf.M33] == [1.0, 1.0, 1.0, 1.0]
    tm = r.TextureMapping.CreateSurfaceParameterMapping()
    t = tm.Evaluate(r.Point3d(0.25, 0.5, 0), r.Vector3d(0, 0, 1))
    assert len(t) == 2 and isinstance(t[0], int)


@pytest.mark.skipif(not hasattr(r.Surface, 'Domain'), reason='G51 not generated')
def test_parameter_normalization_conditional_args_and_ordered_throws():
    ns = r.NurbsSurface.CreateFromSphere(r.Sphere(r.Point3d(0, 0, 0), 2.0))
    # `if (direction != 0) direction = 1;`
    d1, d7 = ns.Domain(1), ns.Domain(7)
    assert (d1.T0, d1.T1) == (d7.T0, d7.T1)
    # `start ? 0 : 1`
    ku = ns.KnotsU
    assert isinstance(ku.SuperfluousKnot(True), float)
    assert ku.SuperfluousKnot(True) <= ku.SuperfluousKnot(False)
    # `if (!GetPoint(...)) { if (index < 0) throw; if (index >= Count) throw; }`
    cloud = r.PointCloud()
    for i in range(3):
        cloud.Add(r.Point3d(i, 0, 0))
    assert cloud.PointAt(1).X == 1
    for bad in (-1, 3):
        with pytest.raises(IndexError):
            cloud.PointAt(bad)


@needs_comp
@pytest.mark.skipif(not hasattr(r, 'SubDEdge') or not hasattr(r.SubDEdge, 'EndSharpness'), reason='G51 not generated')
def test_guard_message_after_nameof():
    # `throw new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be 0 or 1.")`
    f, s = subd_from_file()
    e = next(iter(s.Edges))
    assert isinstance(e.EndSharpness(0, False), float)
    with pytest.raises(IndexError, match='endIndex must be 0 or 1'):
        e.EndSharpness(2, False)


@pytest.mark.skipif(not hasattr(r.MeshingParameters, 'FastRenderMesh'), reason='G51 not generated')
def test_value_classes_adopt_native_results():
    # `var p = UNM.F(); var mp = new MeshingParameters(p); return mp;`: copied in, released
    fast, quality = r.MeshingParameters.FastRenderMesh, r.MeshingParameters.QualityRenderMesh
    assert isinstance(fast, r.MeshingParameters) and isinstance(quality, r.MeshingParameters)
    assert isinstance(r.MeshingParameters.DefaultAnalysisMesh.RelativeTolerance, float)
    # `if (IntPtr.Zero == p) return null;` -> None
    attrs = r.ObjectAttributes()
    assert attrs.CustomMeshingParameters is None
    attrs.CustomMeshingParameters = quality
    got = attrs.CustomMeshingParameters
    assert got is not None and got.RelativeTolerance == quality.RelativeTolerance
    nc = r.NurbsCurve.CreateFromCircle(r.Circle(r.Point3d(0, 0, 0), 1.0))
    assert isinstance(nc.ConvertSpanToBezier(0), r.BezierCurve)
    assert nc.ConvertSpanToBezier(99) is None


# --- G53/G54: colors from outs, opennurbs enums, array fills, call sequences --

def quad_mesh():
    m = r.Mesh()
    for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)]:
        m.Vertices.Add(*[float(c) for c in xyz])
    m.Faces.AddFace(0, 1, 2, 3)
    return m


@pytest.mark.skipif(not hasattr(r.PointCloud, 'GetPoints'), reason='G54 not generated')
def test_array_fills_return_what_the_native_wrote():
    cloud = r.PointCloud()
    for i in range(3):
        cloud.Add(r.Point3d(i, 2 * i, 0))
    pts = cloud.GetPoints()
    assert [(p.X, p.Y) for p in pts] == [(0, 0), (1, 2), (2, 4)]
    assert cloud.GetNormals() == ()                     # `if (count < 1) return new Vector3d[0];`
    assert r.ObjectAttributes().GetGroupList() is None  # `if (count < 1) return null;`
    mx = quad_mesh()
    assert mx.VertexColors.ToARGBArray() == ()
    faces = mx.TopologyEdges.GetConnectedFaces(0)
    assert faces == (0,)
    # `int count = SpanCount(direction) + 1; ... bool ok = GetSpanVector(...); if (ok) return rc; return null;`
    ns = r.NurbsSurface.CreateFromSphere(r.Sphere(r.Point3d(0, 0, 0), 2.0))
    sv = ns.GetSpanVector(0)
    assert len(sv) == ns.SpanCount(0) + 1 and list(sv) == sorted(sv)


@pytest.mark.skipif(not hasattr(r.MeshVertexList, 'HideAll'), reason='G54 not generated')
def test_native_call_sequences():
    mx = quad_mesh()
    mx.Vertices.HideAll()                               # EnsureHiddenList, then HideAll
    assert mx.Vertices.Count == 4
    mx.Vertices.Clear()                                 # ClearList, then RepairHiddenArray
    assert mx.Vertices.Count == 0
    t = box_brep().Trims[0]
    t.SetTolerances(0.25, 0.5)                          # two SetTolerance calls, u then v


@pytest.mark.skipif(not hasattr(r.PointCloudItem, 'Color'), reason='G53 not generated')
def test_colors_from_out_locals_and_enum_results():
    # `int argb = 0; GetColor(p, i, ref argb); return Color.FromArgb(argb);`
    pc = r.PointCloud()
    pc.Add(r.Point3d(0, 0, 0))
    item = pc[0]
    c = item.Color
    assert len(c) == 4
    # `if (!GetPerFaceColor(p, ref argb)) return Color.Empty;`
    if hasattr(r, 'SubDFace'):
        f, s = subd_from_file()
        assert tuple(next(iter(s.Faces)).PerFaceColor) == (0, 0, 0, 0)
    # `int rc = (int)ON_Mesh_ConvertNonPlanarQuadsToTriangles(...); return rc;`
    assert quad_mesh().Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.1, 0) == 0


@pytest.mark.skipif(not hasattr(r.PointCloud, 'GetColors') or not hasattr(r.Texture, 'WrapU'), reason='G55 not generated')
def test_converted_array_fill_and_result_select():
    # `res[i] = Color.FromArgb(rc[i])` over the filled ints
    pc = r.PointCloud()
    pc.Add(r.Point3d(0, 0, 0))
    assert pc.GetColors() == ()          # no colors: `return new Color[0];`
    # `value == (int)TextureUvwWrapping.Clamp ? Clamp : Repeat`
    t = r.Texture()
    t.WrapU = r.TextureUvwWrapping.Clamp
    assert t.WrapU == r.TextureUvwWrapping.Clamp
    t.WrapU = r.TextureUvwWrapping.Repeat
    assert t.WrapU == r.TextureUvwWrapping.Repeat


@pytest.mark.skipif(not hasattr(r, 'NurbsCurvePointList') or not hasattr(r.NurbsCurvePointList, 'GetWeight'),
                    reason='G56 not generated')
def test_guard_only_helpers_become_guards():
    # `ValidateIndex(index);`: `if (index < 0) throw ...; if (index >= Count) throw ...;`
    nc = r.NurbsCurve.CreateFromCircle(r.Circle(r.Point3d(0, 0, 0), 1.0))
    pts = nc.Points
    assert isinstance(pts.GetWeight(0), float)
    for bad in (-1, pts.Count):
        with pytest.raises(IndexError):
            pts.GetWeight(bad)
    ns = r.NurbsSurface.CreateFromSphere(r.Sphere(r.Point3d(0, 0, 0), 2.0))
    ok, p = ns.Points.GetPoint(0, 0)
    assert ok is True and isinstance(p, r.Point3d)
    with pytest.raises(IndexError, match='u must be less than CountU'):
        ns.Points.GetPoint(ns.Points.CountU, 0)


# --- G57: the items left open in N --------------------------------------------

@pytest.mark.skipif(not hasattr(r.Intersection, 'LineCircle'), reason='G57 not generated')
def test_guards_in_tuple_members_and_result_clamps():
    circle = r.Circle(r.Point3d(0, 0, 0), 1.0)
    rc = r.Intersection.LineCircle(r.Line(r.Point3d(-2, 0, 0), r.Point3d(2, 0, 0)), circle)
    assert len(rc) == 5 and int(rc[0]) == 2                       # Multiple
    # `if (!line.IsValid || !circle.IsValid) return None;` -- the outs as initialized
    bad = r.Intersection.LineCircle(r.Line(r.Point3d(0, 0, 0), r.Point3d(0, 0, 0)), circle)
    assert int(bad[0]) == 0 and bad[1] == 0.0 and bad[3] == 0.0
    # `if (rc <= 0 || rc > 3) return None; return (SphereSphereIntersection)rc;`
    a, b = r.Sphere(r.Point3d(0, 0, 0), 1.0), r.Sphere(r.Point3d(1, 0, 0), 1.0)
    ss = r.Intersection.SphereSphere(a, b)
    assert len(ss) == 2 and int(ss[0]) == 2 and isinstance(ss[1], r.Circle)   # Circle
    far = r.Intersection.SphereSphere(a, r.Sphere(r.Point3d(10, 0, 0), 1.0))
    assert int(far[0]) == 0


@pytest.mark.skipif(not hasattr(r.TextureMapping, 'Id'), reason='G57 not generated')
def test_owned_objects_are_never_document_controlled():
    # `IsDocumentControlled ? m_id : ON_TextureMapping_GetId(...)`: always the native here
    import uuid
    assert isinstance(r.TextureMapping.CreateSurfaceParameterMapping().Id, uuid.UUID)


@pytest.mark.skipif(not hasattr(r.Material, 'GetBitmapTexture'), reason='G57 not generated')
def test_material_textures_are_views_copied_on_write():
    # RhinoCommon `new Texture(index, material)`: reads resolve through the material;
    # the first write copies (NonConstOperation) and leaves the material alone
    m = r.Material()
    assert m.GetBitmapTexture() is None
    t = r.Texture()
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


@pytest.mark.skipif(not hasattr(r.PolyCurve, 'SegmentCurve'), reason='G58 not generated')
def test_child_geometry_views_follow_rhinocommon():
    # PolyCurve.SegmentCurve: a view of the segment that WRITES THROUGH (CommonObject's
    # PolyCurve special case), as in 8.x
    pc = r.PolyCurve()
    pc.Append(r.LineCurve((0, 0, 0), (2, 0, 0)))
    pc.Append(r.LineCurve((2, 0, 0), (2, 3, 0)))
    seg = pc.SegmentCurve(0)
    assert isinstance(seg, r.LineCurve)
    seg.Reverse()
    s = pc.SegmentCurve(0).PointAtStart
    assert (s.X, s.Y) == (2, 0)
    assert pc.SegmentCurve(5) is None
    # BrepFace.UnderlyingSurface: a view that COPIES on the first write (holder parent)
    face = box_brep().Faces[0]
    srf = face.UnderlyingSurface()
    before = face.UnderlyingSurface().PointAt(0.0, 0.0)
    srf.Translate(r.Vector3d(0, 0, 5))
    moved = srf.PointAt(0.0, 0.0)
    after = face.UnderlyingSurface().PointAt(0.0, 0.0)
    assert moved.Z == before.Z + 5 and (after.X, after.Y, after.Z) == (before.X, before.Y, before.Z)
    # BrepFace.GetMesh: no render mesh on a new box
    assert face.GetMesh(r.MeshType.Render) is None


@pytest.mark.skipif(not hasattr(r.BrepFace, 'SetMesh'), reason='G59 not generated')
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


@pytest.mark.skipif(not hasattr(r, 'Decals'), reason='G60 not generated')
def test_decals_iterate_a_snapshot_of_copies():
    # RhinoCommon's DecalEnumerator: a snapshot of the attributes' decals, each
    # item its own shared_ptr copy -- it outlives the file it came from
    import gc
    f = r.File3dm.Read(os.path.join(MODELS, 'sphereDecals.3dm'))
    objs = list(f.Objects)
    decals = list(objs[0].Attributes.Decals)
    assert len(decals) == 2
    assert all(d.Mapping == r.DecalMapping.UV for d in decals)
    ids = [d.TextureInstanceId for d in decals]
    assert len(set(ids)) == 2
    del f, objs
    gc.collect()
    assert [d.TextureInstanceId for d in decals] == ids


@pytest.mark.skipif(not hasattr(r, 'Decals'), reason='G60 not generated')
def test_decals_of_attributes_are_writable_with_rhinocommons_guards():
    # `new Decals(this, false, doc_sn)`: not read-only, so the m_read_only guards pass
    a = r.ObjectAttributes()
    assert list(a.Decals) == []
    a.Decals.RemoveAllDecals()
    a.Decals.Clear()
    assert a.Decals.Remove(None) is False          # `if (decal == null) return false;`


@pytest.mark.skipif(not hasattr(r, 'ComponentIndex'), reason='ComponentIndex not generated')
def test_component_index_value_type():
    # a translated value struct; its constructor takes the public ComponentIndexType
    ci = r.ComponentIndex(r.ComponentIndexType.BrepFace, 3)
    assert ci.Index == 3 and ci.ComponentIndexType == r.ComponentIndexType.BrepFace and ci.IsSet
    u = r.ComponentIndex.Unset                        # `new ComponentIndex(InvalidType, -1)`
    assert u.IsUnset() and not ci.IsUnset() and u.Index == -1
    assert ci == r.ComponentIndex(r.ComponentIndexType.BrepFace, 3) and ci != u and u < ci
    face = box_brep().Faces[1]
    assert face.ComponentIndex() == r.ComponentIndex(r.ComponentIndexType.BrepFace, 1)


@pytest.mark.skipif(not hasattr(r, 'RevSurface'), reason='RevSurface not generated')
def test_rev_surface_from_rhinocommon_shapes():
    import math
    s = r.RevSurface.CreateFromSphere(r.Sphere(r.Point3d(0, 0, 0), 2.0))
    assert isinstance(s, r.RevSurface) and isinstance(s, r.Surface)
    assert abs((s.Angle.T1 - s.Angle.T0) - 2 * math.pi) < 1e-9
    assert s.AxisAndCurveCoplanar() is True
    assert isinstance(r.Sphere(r.Point3d(0, 0, 0), 1.0).ToRevSurface(), r.RevSurface)


@pytest.mark.skipif(not hasattr(r, 'RenderChannels'), reason='RenderChannels not generated')
def test_render_channels_of_render_settings():
    rc = r.RenderSettings().RenderChannels
    assert rc.Mode == r.RenderChannels.Modes.Automatic   # `(m == 0) ? Modes.Automatic : Modes.Custom`
    import uuid
    assert all(isinstance(i, uuid.UUID) for i in rc.CustomList)   # opennurbs' default custom channels
    assert r.RenderChannels().Mode == r.RenderChannels.Modes.Automatic


@pytest.mark.skipif(not hasattr(r, 'CommonObject'), reason='G62 not generated')
def test_common_object_is_the_root_of_object_classes():
    # RhinoCommon's CommonObject: IsValid / IsValidWithLog on everything that is one
    b = box_brep()
    assert isinstance(b, r.CommonObject) and isinstance(r.ObjectAttributes(), r.CommonObject)
    assert b.IsValid is True
    ok, log = b.IsValidWithLog()
    assert ok is True and isinstance(log, str)
    assert r.CommonObject.ComputeMemoryEstimate(b) > 0
    # ON_TextureCoordinates is no ON_Object: no CommonObject base (pilot-findings #31)
    if hasattr(r, 'CachedTextureCoordinates'):
        assert not issubclass(r.CachedTextureCoordinates, r.CommonObject)
    a = r.ObjectAttributes()
    assert a.SetUserString('k', 'v') is True and a.GetUserString('k') == 'v' and a.UserStringCount == 1


@pytest.mark.skipif(not hasattr(r.Light, 'IsPointLight'), reason='G63 not generated')
def test_translated_members_built_from_generated_members():
    # RhinoCommon's own C# bodies, translated (G63): `LightStyle == LightStyle.WorldPoint || ...`
    import math
    light = r.Light()
    light.LightStyle = r.LightStyle.WorldPoint
    assert light.IsPointLight is True and light.IsSpotLight is False and light.IsLinearLight is False
    light.LightStyle = r.LightStyle.WorldSpot
    assert light.IsSpotLight is True and light.IsPointLight is False
    # RhinoMath.ToDegrees(ModelAngleToleranceRadians), and the setter back through ToRadians
    f = r.File3dm()
    f.Settings.ModelAngleToleranceDegrees = 3.0
    assert abs(f.Settings.ModelAngleToleranceRadians - math.radians(3.0)) < 1e-12
    assert abs(f.Settings.ModelAngleToleranceDegrees - 3.0) < 1e-12
    # `Translate(new Vector3d(x, y, z))`
    b = box_brep()
    assert b.Translate(1.0, 2.0, 3.0) is True
    # a switch over TrimCount (BrepEdge.Valence): a box's edges are interior
    assert all(b.Edges[i].Valence == r.EdgeAdjacency.Interior for i in range(b.Edges.Count))


@pytest.mark.skipif(not hasattr(r, 'File3dmWriteOptions'), reason='G64 not generated')
def test_data_classes_are_rhinocommons_translated_whole(tmp_path):
    # G64: File3dmWriteOptions, its constructor (`SaveUserData = true`), the [Flags]
    # arithmetic of EnableRenderMeshes, and `options = options ?? new ...` in Write
    o = r.File3dmWriteOptions()
    assert o.SaveUserData is True and o.SaveRenderMeshes is True and o.Version == 0
    o.EnableRenderMeshes(r.ObjectType.AnyObject, False)
    assert o.SaveRenderMeshes is False
    o.Version = 7
    f = r.File3dm()
    path = str(tmp_path / 'opts.3dm')
    assert f.Write(path, o) is True and f.Write(str(tmp_path / 'default.3dm'), None) is True
    ok, log = f.WriteWithLog(str(tmp_path / 'log.3dm'), None)
    assert ok is True and isinstance(log, str)
    assert r.File3dm.Read(path) is not None
    # PageSettings: a data class handed to a native setter member by member
    ps = r.PageSettings()
    ps.PageNumber, ps.WidthMillimeters, ps.PrinterName = 3, 210.0, 'laser'
    v = r.ViewInfo()
    v.SetPageSettings(ps)
    # ConstructionPlane: RhinoCommon's constructor defaults, and the named-cplane
    # table of a fresh file (its indexer builds data objects from the native)
    cp = r.ConstructionPlane()
    assert cp.GridSpacing == 1.0 and cp.GridLineCount == 70 and cp.ThickLineFrequency == 5 and cp.DepthBuffered is True
    assert cp.Plane.Origin == r.Point3d(0, 0, 0) and cp.Name == ''
    cp.Name = 'top'
    assert cp.Name == 'top'
    assert len(f.NamedConstructionPlanes) == 0


@pytest.mark.skipif(not hasattr(r.ViewInfo, 'GetPageSettings'), reason='G65 not generated')
def test_data_objects_cross_the_native_boundary_both_ways(tmp_path):
    # G65: a data object built from the native (GetPageSettings, GridDefaults, the
    # named-cplane indexer) and handed to it (SetPageSettings, GridDefaults=, Add)
    v = r.ViewInfo()
    ps = r.PageSettings()
    ps.PageNumber, ps.WidthMillimeters, ps.PrinterName = 3, 210.0, 'laser'
    v.SetPageSettings(ps)
    back = v.GetPageSettings()
    assert (back.PageNumber, back.WidthMillimeters, back.PrinterName) == (3, 210.0, 'laser')
    f = r.File3dm()
    gd = f.Settings.GridDefaults
    assert gd is not None and gd.GridLineCount == 70
    gd.GridLineCount = 12
    f.Settings.GridDefaults = gd
    assert f.Settings.GridDefaults.GridLineCount == 12
    cp = r.ConstructionPlane()
    cp.Name, cp.GridSpacing = 'top', 2.5
    f.NamedConstructionPlanes.Add(cp)
    assert len(f.NamedConstructionPlanes) == 1
    got = f.NamedConstructionPlanes[0]
    assert got.Name == 'top' and got.GridSpacing == 2.5
    # `Write(path, new File3dmWriteOptions { Version = version })`
    path = str(tmp_path / 'v.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dm.Read(path)
    assert g.NamedConstructionPlanes[0].Name == 'top'


@pytest.mark.skipif(not hasattr(r.File3dmLayerTable, 'FindId'), reason='G66 not generated')
def test_find_id_on_the_tables_skips_deleted_components():
    # G66: `m_manifest.FindId(id, ComponentType) is T item && !item.IsDeleted`
    import uuid
    f = r.File3dm()
    layer = r.Layer()
    layer.Name = 'walls'
    f.Layers.Add(layer)                                 # RhinoCommon's ICollection Add: void
    found = f.Layers.FindIndex(f.Layers.Count - 1)
    got = f.Layers.FindId(found.Id)
    assert got is not None and got.Name == 'walls'
    assert f.Layers.FindId(uuid.uuid4()) is None
    mat = r.Material()
    f.Materials.Add(mat)
    mi = f.Materials.Count - 1
    mid = f.Materials.FindIndex(mi).Id
    assert f.Materials.FindId(mid) is not None
    f.Materials.Delete(mi)                              # ModelComponentTable.Delete(int): void in RhinoCommon
    assert f.Materials.FindId(mid) is None                  # deleted: no result, as in RhinoCommon


@pytest.mark.skipif(not hasattr(r.PointCloudItem, 'X'), reason='G67 not generated')
def test_point_cloud_item_coordinates_chain_through_location():
    # G67: `X => Location.X`; the setter copies Location, sets X, writes it back
    pc = r.PointCloud()
    pc.Add(r.Point3d(1, 2, 3))
    item = pc[0]
    assert (item.X, item.Y, item.Z) == (1.0, 2.0, 3.0)
    item.X = 7.5
    assert item.Location == r.Point3d(7.5, 2, 3) and pc[0].X == 7.5


@pytest.mark.skipif(not hasattr(r.File3dm, 'Created'), reason='G68 not generated')
def test_file_dates_and_native_guards(tmp_path):
    # G68: `if (GetDate(...)) return new DateTime(year, month+1, ...); return DateTime.MinValue;`
    import datetime
    f = r.File3dm()
    assert isinstance(f.Created, datetime.datetime) and isinstance(f.LastEdited, datetime.datetime)
    path = str(tmp_path / 'dated.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dm.Read(path)
    assert g.Created >= datetime.datetime(2000, 1, 1) and g.LastEdited >= g.Created
    # `if (UnsafeNativeMethods.ON_Mesh_GetHiddenValue(p, i)) return;` then the two ops
    m = quad_mesh()
    m.Vertices.Hide(1)
    assert m.Vertices.IsHidden(1) is True and m.Vertices.IsHidden(0) is False
    m.Vertices.Hide(1)                                  # already hidden: returns at the guard
    m.Vertices.Show(1)
    assert m.Vertices.IsHidden(1) is False


@pytest.mark.skipif(not hasattr(r, 'Color4f'), reason='G69 not generated')
def test_color4f_and_the_members_it_unlocks():
    # G69: Color4f translated as a value class (D34); PhysicallyBasedMaterial's colours
    # come and go as Color4f through ON_4fPoint by layout
    c = r.Color4f(0.25, 0.5, 0.75, 1.0)
    assert (c.R, c.G, c.B, c.A) == (0.25, 0.5, 0.75, 1.0) and c == r.Color4f(0.25, 0.5, 0.75, 1.0)
    assert r.Color4f.White == r.Color4f(1, 1, 1, 1)
    m = r.Material()
    m.ToPhysicallyBased()
    pbr = m.PhysicallyBased
    pbr.BaseColor = c
    assert pbr.BaseColor == c
    # Sun.Light: `var light = new Light(); UNM.ON_Sun_Light(this, light.NonConstPointer()); return light;`
    s = r.Sun()
    assert isinstance(s.Light, r.Light)
    # Texture.FileReference: a handle from a const pointer the C side allocated for it
    t = r.Texture()
    fr = t.FileReference
    assert fr is None or isinstance(fr, r.FileReference)


@pytest.mark.skipif(not hasattr(r.GeometryBase, 'Duplicate'), reason='G69 Duplicate not generated')
def test_duplicate_returns_an_owned_most_derived_copy():
    # G69: GeometryBase.Duplicate -> CreateGeometryHelper(p, null) through RH3DM_Wrap_<root>;
    # the overrides construct their own class (Brep, Mesh, Curve, PolyCurve via its copy ctor)
    b = box_brep()
    d = b.Duplicate()
    assert isinstance(d, r.Brep) and d is not b and d.Faces.Count == b.Faces.Count
    pc = r.PolyCurve()
    pc.Append(r.LineCurve(r.Point3d(0, 0, 0), r.Point3d(1, 0, 0)))
    pd = pc.Duplicate()
    assert isinstance(pd, r.PolyCurve) and pd.SegmentCount == 1
    nc = r.NurbsCurve.CreateFromLine(r.Line(r.Point3d(0, 0, 0), r.Point3d(0, 1, 0))) if hasattr(r.NurbsCurve, 'CreateFromLine') else None
    if nc is not None:
        cd = r.Curve.Duplicate(nc)                       # the base's: dispatched on the runtime type
        assert isinstance(cd, r.NurbsCurve)


@pytest.mark.skipif(not hasattr(r.BoundingBox, 'GetCorners'), reason='G70 not generated')
def test_value_type_arrays_are_sequences():
    # G70: Point3d[] / IEnumerable<Point3d> are std::vector: lists in Python. C#'s
    # null result is the empty list (D35)
    import math
    bb = r.BoundingBox(r.Point3d(0, 0, 0), r.Point3d(1, 2, 3))
    corners = bb.GetCorners()
    assert len(corners) == 8 and corners[0] == r.Point3d(0, 0, 0) and corners[6] == r.Point3d(1, 2, 3)
    assert r.BoundingBox.Unset.GetCorners() == []
    xf = r.Transform.Translation(r.Vector3d(1, 0, 0))
    moved = xf.TransformList([r.Point3d(0, 0, 0), r.Point3d(1, 1, 1)])
    assert moved == [r.Point3d(1, 0, 0), r.Point3d(2, 1, 1)]
    assert len(xf.ToFloatArray(True)) == 16
    if hasattr(bb, 'Transform'):
        assert bb.Transform(xf) is True and bb.Min == r.Point3d(1, 0, 0)
    assert bb.ClosestPoint(r.Point3d(-5, 1, 1)) == r.Point3d(bb.Min.X, 1, 1)
    p = r.Plane.WorldXY
    assert p.Rotate(math.pi / 2, r.Vector3d(0, 0, 1)) is True
    assert abs(p.XAxis.Y - 1.0) < 1e-12
    box = r.Box(r.BoundingBox(r.Point3d(0, 0, 0), r.Point3d(1, 1, 1)))
    assert box.Contains(r.Point3d(0.5, 0.5, 0.5)) is True and len(box.GetCorners()) == 8


@pytest.mark.skipif(not hasattr(r.File3dm, 'EmbeddedFiles'), reason='G71 not generated')
def test_embedded_files_add_and_iterate(tmp_path):
    # G71: File3dmEmbeddedFiles.Add(filename) and its manifest enumerator (no Count in
    # RhinoCommon, so no len()); File3dmEmbeddedFile is a File3dm proxy
    data = tmp_path / 'note.txt'
    data.write_text('hello')
    f = r.File3dm()
    assert f.EmbeddedFiles.Add(str(data)) is True
    files = list(f.EmbeddedFiles)
    assert len(files) == 1 and isinstance(files[0], r.File3dmEmbeddedFile)
    assert files[0].Filename.endswith('note.txt') and files[0].ComponentType == r.ModelComponentType.EmbeddedFile
    path = str(tmp_path / 'embedded.3dm')
    assert f.Write(path, 7) is True
    g = r.File3dm.Read(path)
    assert [e.Filename for e in g.EmbeddedFiles] == [files[0].Filename]


# --- G72: tuples of geometry from native pointers; per-element native loops ---

@pytest.mark.skipif(not hasattr(r.Curve, 'Split'), reason='G72 not generated')
def test_split_returns_the_halves_or_none():
    # Curve.Split(double): `bool rc = F(ptr, t, ref left, ref right); Curve[] output = new Curve[2];
    # if (left != IntPtr.Zero) output[0] = CreateGeometryHelper(left, null) as Curve; ... return rc ? output : null;`
    c = r.LineCurve((0, 0, 0), (2, 0, 0))
    parts = c.Split(1.0)
    assert len(parts) == 2 and all(isinstance(p, r.Curve) for p in parts)
    assert parts[0].PointAtEnd == r.Point3d(1, 0, 0) and parts[1].PointAtStart == r.Point3d(1, 0, 0)
    assert c.Split(5.0) is None                           # outside the domain: the native fails
    # BezierCurve.Split(t, out left, out right): (success, left, right), the halves None on failure
    nc = r.NurbsCurve.CreateFromCircle(r.Circle(r.Point3d(0, 0, 0), 1.0))
    bz = nc.ConvertSpanToBezier(0)
    ok, left, right = bz.Split(0.5)
    assert ok is True and isinstance(left, r.BezierCurve) and isinstance(right, r.BezierCurve)
    assert left.PointAt(1.0) == right.PointAt(0.0) == bz.PointAt(0.5)
    assert bz.Split(2.0) == (False, None, None)


@pytest.mark.skipif(not hasattr(r.PolyCurve, 'Explode'), reason='G72 not generated')
def test_explode_duplicates_each_segment():
    # PolyCurve.Explode: the segment pointers read back through a SimpleArrayCurvePointer,
    # each duplicated (ON_Curve_DuplicateCurve) and wrapped as its own class
    pc = r.PolyCurve()
    pc.Append(r.LineCurve((0, 0, 0), (1, 0, 0)))
    pc.Append(r.LineCurve((1, 0, 0), (1, 1, 0)))
    segs = pc.Explode()
    assert len(segs) == 2 and all(isinstance(s, r.LineCurve) for s in segs)
    assert segs[1].PointAtEnd == r.Point3d(1, 1, 0)
    assert r.PolyCurve().Explode() == ()


@pytest.mark.skipif(not hasattr(r.MeshVertexList, 'ToPoint3dArray'), reason='G72 not generated')
def test_vertex_arrays_read_one_vertex_per_call():
    # MeshVertexList.ToPoint3dArray: `for (i < Count) { var pt = new Point3d(); ON_Mesh_Vertex(ptr, i, ref pt); rc[i] = pt; }`
    mx = quad_mesh()
    pts = mx.Vertices.ToPoint3dArray()
    assert [(p.X, p.Y, p.Z) for p in pts] == [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)]
    # ToPoint3fArray: the same vertices, `new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z)`
    pf = mx.Vertices.ToPoint3fArray()
    assert all(isinstance(p, r.Point3f) for p in pf) and [(p.X, p.Y) for p in pf] == [(0, 0), (1, 0), (1, 1), (0, 1)]
    assert r.Mesh().Vertices.ToPoint3dArray() == ()


# --- G73: holders empty on a status, tuples led by an object, discarded-out forwards,
#          self-identity guards, GC-hint plumbing in constructors ---

@pytest.mark.skipif(not hasattr(r.BrepVertex, 'EdgeIndices'), reason='G73 not generated')
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


@pytest.mark.skipif(not hasattr(r.Surface, 'ToNurbsSurface'), reason='G73 not generated')
def test_to_nurbs_surface_leads_its_tuple_with_the_object_and_forwards_without_the_out():
    # ToNurbsSurface(tol, out accuracy) -> (NurbsSurface | None, accuracy);
    # ToNurbsSurface() -> `int accuracy; return ToNurbsSurface(0.0, out accuracy);` -> NurbsSurface
    srf = box_brep().Faces[0].UnderlyingSurface()
    ns, accuracy = srf.ToNurbsSurface(0.0)
    assert isinstance(ns, r.NurbsSurface) and isinstance(accuracy, int) and accuracy > 0
    assert isinstance(srf.ToNurbsSurface(), r.NurbsSurface)


@pytest.mark.skipif(not hasattr(r.Mesh, 'Append'), reason='G73 not generated')
def test_mesh_append_skips_null_and_itself_and_default_constructs():
    # `if (null == other || other.ConstPointer() == ConstPointer()) return;` -- and the
    # default constructors (`ON_Mesh_New(); ConstructNonConstObject(p); ApplyMemoryPressure();`)
    m = r.Mesh()
    assert m.Vertices.Count == 0
    m.Append(None)
    m.Append(m)
    assert m.Vertices.Count == 0
    q = quad_mesh()
    m.Append(q)
    assert m.Vertices.Count == 4 and m.Faces.Count == 1
    m.Append(q)
    assert m.Vertices.Count == 8
    assert r.PointCloud().Count == 0 and r.Brep().Faces.Count == 0


# --- G74: class-typed values in translated members ---

@pytest.mark.skipif(not hasattr(r.PointCloud, 'AppendNew'), reason='G74 not generated')
def test_translated_members_return_and_construct_class_values():
    # PointCloud.AppendNew: `Add(Point3d.Origin); return this[Count - 1];` -- a sibling call and the indexer
    pc = r.PointCloud()
    item = pc.AppendNew()
    assert isinstance(item, r.PointCloudItem) and pc.Count == 1 and item.Location == r.Point3d(0, 0, 0)
    pc.Add(r.Point3d(1, 1, 1))
    assert pc.InsertNew(1).Index == 1 and pc.Count == 3
    # NurbsCurve.CreateFromEllipse: `NurbsCurve nc = CreateFromCircle(..); if (nc == null) return null;
    # nc.Transform(scale); return nc;` -- a class-typed local, null-tested, mutated, returned
    nc = r.NurbsCurve.CreateFromEllipse(r.Ellipse(r.Plane.WorldXY, 2.0, 1.0))
    assert isinstance(nc, r.NurbsCurve) and abs(nc.PointAtStart.X - 2.0) < 1e-9
    # `Duplicate() as Brep`: the fresh object downcast (RH3DM_As frees it when it is not one)
    d = box_brep().DuplicateBrep()
    assert isinstance(d, r.Brep) and d.Faces.Count == 6
    assert isinstance(quad_mesh().DuplicateMesh(), r.Mesh)
    # MeshingParameters: a value-mode class default-constructed; the static presets are
    # object initializers, translated as setter calls on the fresh value
    assert isinstance(r.MeshingParameters(), r.MeshingParameters)
    assert isinstance(r.MeshingParameters.Default, r.MeshingParameters)
    minimal = r.MeshingParameters.Minimal
    assert minimal.GridMinCount == 16 and minimal.JaggedSeams is True
    assert r.MeshingParameters.Smooth.SimplePlanes is True and minimal.SimplePlanes is False
    assert isinstance(r.ViewInfo(), r.ViewInfo)


# --- G75-G77: managed fields, private-helper inlining, scoped Brep views ---

@pytest.mark.skipif(not hasattr(r.AnnotationBase, 'RunReplace'), reason='G75 not generated')
def test_annotation_members_read_with_the_parent_dimstyle_null():
    # G75/G76: AnnotationBase.m_parent_dimstyle is written only by ParentDimensionStyle.set
    # (not generated), so it is null on every generated annotation: ConstParentDimStylePointer()
    # folds to IntPtr.Zero. No concrete annotation class is generated yet, so the members
    # are checked on the registered class (their parity is the bodies' one native call each)
    A = r.AnnotationBase
    for name in ('TextHeight', 'DimensionScale', 'MaskEnabled', 'DimensionStyleId', 'RichText'):
        assert isinstance(getattr(A, name), property) and getattr(A, name).fset is not None, name
    for name in ('MaskFrame', 'MaskColorSource', 'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay'):
        assert isinstance(getattr(A, name), property), name
    assert callable(A.RunReplace) and callable(A.SetRichText)


@pytest.mark.skipif(not hasattr(r.BrepFace, 'Loops'), reason='G77 not generated')
def test_face_and_loop_scoped_lists():
    # G77: BrepFace.Loops => new BrepLoopList(this): the brep's loop list scoped to the
    # face (Count = ON_BrepFace_LoopCount, element i = ON_BrepFace_LoopIndex(face, i))
    b = box_brep()
    assert len(b.Loops) == 6
    for fi in range(len(b.Faces)):
        f = b.Faces[fi]
        loops = f.Loops
        assert isinstance(loops, r.BrepLoopList) and len(loops) == 1
        outer = f.OuterLoop
        assert outer.LoopIndex == loops[0].LoopIndex
        # BrepLoop.Trims: the brep's trims scoped to the loop
        trims = outer.Trims
        assert isinstance(trims, r.BrepTrimList) and len(trims) == 4
        assert all(t.Loop.LoopIndex == outer.LoopIndex for t in trims)
        assert all(t.Face.FaceIndex == fi for t in trims)
        assert all(isinstance(t.Edge, r.BrepEdge) for t in trims)
    # the whole-brep lists are unchanged
    assert len(b.Trims) == 24 and [l.LoopIndex for l in b.Loops] == list(range(6))
    with pytest.raises(IndexError):
        b.Faces[0].Loops[1]


# --- G78/G79: RDK Variant floats and enum maps; strings in translated members ---

@pytest.mark.skipif(not hasattr(r.LinearWorkflow, 'PreProcessGamma'), reason='G78 not generated')
def test_rdk_float_and_enum_mapped_variant_properties():
    rs = r.File3dm().Settings.RenderSettings
    lw = rs.LinearWorkflow
    lw.PreProcessGamma = 2.5
    assert abs(lw.PreProcessGamma - 2.5) < 1e-6
    lw.PostProcessGamma = 1.75
    assert abs(lw.PostProcessGamma - 1.75) < 1e-6
    # Dithering.Method: the RDK stores 0 for SimpleNoise, anything else is FloydSteinberg;
    # the enum is RhinoCommon's (None = 0, FloydSteinberg = 1, SimpleNoise = 2; D36)
    d = rs.Dithering
    M = r.Dithering.Methods
    assert int(M.SimpleNoise) == 2 and int(M.FloydSteinberg) == 1
    d.Method = M.SimpleNoise
    assert d.Method == M.SimpleNoise
    d.Method = M.FloydSteinberg
    assert d.Method == M.FloydSteinberg


@pytest.mark.skipif(not hasattr(r.AnnotationBase, 'PlainTextToRtf'), reason='G79 not generated')
def test_translated_string_members(tmp_path):
    # PlainTextToRtf: `@"{\rtf1{\ltrch " + str.Replace("\n", Ph)...Replace(Ph, @"}\par {") + "}}"`
    assert r.AnnotationBase.PlainTextToRtf('a\nb') == '{\\rtf1{\\ltrch a}\\par {b}}'
    assert r.AnnotationBase.PlainTextToRtf('x{y}\\') == '{\\rtf1{\\ltrch x\\{y\\}\\\\}}'
    # DocumentUserTextCount: keys containing a backslash (section\entry)
    # (written, then read back)
    w = r.File3dm()
    w.Strings.SetString('plain', 'v')
    w.Strings.SetString('sec', 'key', 'v2')
    path = str(tmp_path / 'strings.3dm')
    assert w.Write(path, 8)
    f = r.File3dm.Read(path)
    assert f.Strings.DocumentUserTextCount == 1
    assert f.Strings.GetValue('sec', 'key') == 'v2' and f.Strings.GetValue('', 'key') == ''
    # MeshVertexColorList.Add(Color): `SetColor(Count, color); return Count - 1;`
    mx = quad_mesh()
    assert mx.VertexColors.Add((255, 0, 0, 255)) == 0 and mx.VertexColors.Count == 1


# --- G80: Point3dList / Polyline -- managed-state classes (option 4) ---

@pytest.mark.skipif(not hasattr(r, 'Polyline'), reason='G80 not generated')
def test_point3dlist_is_a_real_list_with_rhinolist_semantics():
    P = r.Point3d
    pl = r.Point3dList([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)])
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
    e = r.Point3dList()
    assert e.Capacity == 0
    e.Add(P(0, 0, 0))
    assert e.Capacity == 4
    for i in range(4):
        e.Add(P(i, 0, 0))
    assert e.Capacity == 8 and e.Count == 5
    # a copy is a new list; ToArray a plain sequence
    d = pl.Duplicate()
    d[0] = P(42, 0, 0)
    assert pl[0] == P(1, 0, 0) and isinstance(d, r.Point3dList)
    assert list(pl.ToArray()) == list(pl)
    bb = pl.BoundingBox
    assert bb.Min == P(1, 0, 0) and bb.Max == P(9, 0, 0)


@pytest.mark.skipif(not hasattr(r, 'Polyline'), reason='G80 not generated')
def test_polyline_geometry_members():
    P = r.Point3d
    poly = r.Polyline([P(0, 0, 0), P(2, 0, 0), P(2, 2, 0)])
    assert isinstance(poly, r.Point3dList)                    # Polyline : Point3dList
    assert poly.SegmentCount == 2 and abs(poly.Length - 4.0) < 1e-12
    assert poly.PointAt(1.5) == P(2, 1, 0)
    assert poly.SegmentAt(1).From == P(2, 0, 0)
    assert abs(poly.ClosestParameter(P(1, 5, 0)) - 2.0) < 1e-12
    assert poly.ClosestPoint(P(1, -3, 0)) == P(1, 0, 0)
    t = poly.Trim(r.Interval(0.5, 1.5))
    assert isinstance(t, r.Polyline) and list(t) == [P(1, 0, 0), P(2, 0, 0), P(2, 1, 0)]
    assert [s.Length for s in poly.GetSegments()] == [2.0, 2.0]
    poly.Add(P(2, 2, 0))                                       # a zero-length segment
    assert poly.DeleteShortSegments(1e-9) == 1 and poly.Count == 3
    c = poly.Duplicate()
    assert isinstance(c, r.Polyline) and list(c) == list(poly)


@pytest.mark.skipif(not hasattr(r.PointCloud, 'AddRange'), reason='G82 not generated')
def test_sequence_arguments_where_rhinocommon_takes_ienumerable():
    # G82: GetConstArray members take any sequence of points -- a list, a tuple,
    # or a Point3dList / Polyline (decision 4 of option 4)
    P, V = r.Point3d, r.Vector3d
    pc = r.PointCloud()
    pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
    pc.AddRange(r.Polyline([P(2, 0, 0), P(3, 0, 0)]))
    assert pc.Count == 4 and [p.X for p in pc.GetPoints()] == [0, 1, 2, 3]
    # InsertRange: generated since U11 fixed its native (test_point_cloud_insert_range_u11)
    n = r.PointCloud()
    n.AddRange([P(0, 0, 0)], [V(0, 0, 1)])
    assert n.Count == 1 and n.ContainsNormals
    n.AddRange([P(0, 0, 0)], [])                         # no normals: C# returns early
    assert n.Count == 1
    with pytest.raises(ValueError):
        n.AddRange([P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1)])
    f = r.File3dm()
    pid = f.Objects.AddPolyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)], None)
    assert f.Objects.Count == 1 and pid != uuid.UUID(int=0)
    assert f.Objects.AddPolyline([], None) == uuid.UUID(int=0) and f.Objects.Count == 1


@pytest.mark.skipif(not hasattr(r, 'PolylineCurve') or not hasattr(r.PolylineCurve, 'ToPolyline'),
                    reason='G82 ToPolyline not generated')
def test_polylinecurve_to_polyline_through_the_translated_static():
    # G82: `using (var h = new SimpleArrayPoint3d()) { CopyValues(ptr, h); return
    # Polyline.PolyLineFromNativeArray(h); }` -- the holder handed to the data
    # class's translated static; Python owns the new Polyline
    crv = r.PolylineCurve([r.Point3d(0, 0, 0), r.Point3d(3, 0, 0), r.Point3d(3, 4, 0)])
    pl = crv.ToPolyline()
    assert isinstance(pl, r.Polyline) and pl.Count == 3
    assert pl[2] == r.Point3d(3, 4, 0) and abs(pl.Length - 7.0) < 1e-12
    pl[0] = r.Point3d(1, 0, 0)                          # a copy: the curve is unchanged
    assert crv.ToPolyline()[0] == r.Point3d(0, 0, 0)



@pytest.mark.skipif(not hasattr(r, 'PolylineCurve'), reason='G82')
def test_polylinecurve_from_points_chooses_its_creation_call():
    # G82: `if (count < 1) ptr = New(IntPtr.Zero); else ptr = New2(count, pts);`
    P = r.Point3d
    crv = r.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
    assert crv.PointCount == 3 and crv.PointAtEnd == P(3, 4, 0)
    assert r.PolylineCurve(r.Polyline([P(0, 0, 0), P(1, 0, 0)])).PointCount == 2
    assert r.PolylineCurve([]).PointCount == 0                 # the empty arm: ON_PolylineCurve_New



@pytest.mark.skipif(not hasattr(r, 'Polyline') or not hasattr(r.Polyline, 'ToPolylineCurve'), reason='G82')
def test_polyline_to_polylinecurve_constructs_the_generated_class():
    # G82: a data class member naming a generated class: `new PolylineCurve(this)`
    # through class markers the generator resolves against PolylineCurve(points)
    P = r.Point3d
    poly = r.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
    crv = poly.ToPolylineCurve()
    assert isinstance(crv, r.PolylineCurve) and crv.PointCount == 3 and crv.PointAtEnd == P(3, 4, 0)
    assert r.Polyline([P(0, 0, 0)]).ToPolylineCurve() is None        # `if (m_size < 2) return null;`
    assert list(crv.ToPolyline()) == list(poly)                         # and back (ToPolyline)



@pytest.mark.skipif(not hasattr(r, 'Polyline') or not hasattr(r.Polyline, 'ToNurbsCurve'), reason='G82')
def test_polyline_to_nurbscurve_through_a_disposed_polylinecurve():
    # G82: `using (var pc = ToPolylineCurve()) { return pc?.ToNurbsCurve(); }`: the
    # temporary curve deleted on the way out, the NURBS curve owned by the caller
    P = r.Point3d
    nc = r.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]).ToNurbsCurve()
    assert isinstance(nc, r.NurbsCurve) and nc.PointAtEnd == P(3, 4, 0) and nc.PointAtStart == P(0, 0, 0)
    assert r.Polyline([P(0, 0, 0)]).ToNurbsCurve() is None           # no curve: `pc?.` is null


@pytest.mark.skipif(not hasattr(r.Curve, 'ClosedCurveOrientation'), reason='G83')
def test_curve_gap_members_orientation_trim_circle_derivatives():
    # G83: a switch onto an enum with a default; a private helper wrapping its result;
    # an out-argument call to a tuple sibling; G84: managed control flow around natives
    P = r.Point3d
    circle = r.ArcCurve(r.Circle(P(0, 0, 0), 2.0))
    assert circle.ClosedCurveOrientation() == r.CurveOrientation.CounterClockwise
    assert circle.ClosedCurveOrientation(r.Vector3d(0, 0, -1)) == r.CurveOrientation.Clockwise
    line = r.LineCurve(P(0, 0, 0), P(10, 0, 0))
    assert line.ClosedCurveOrientation() == r.CurveOrientation.Undefined     # the switch's default
    piece = line.Trim(2.0, 5.0)
    assert isinstance(piece, r.Curve) and piece.PointAtStart == P(2, 0, 0) and piece.PointAtEnd == P(5, 0, 0)
    assert line.Trim(r.Interval(1.0, 3.0)).PointAtEnd == P(3, 0, 0)
    assert circle.IsCircle() and not line.IsCircle()
    ok, c = circle.TryGetCircle()
    assert ok and abs(c.Radius - 2.0) < 1e-12
    assert line.TryGetCircle()[0] is False
    d = line.DerivativeAt(4.0, 1, r.CurveEvaluationSide.Default)
    assert len(d) == 2 and d[0] == r.Vector3d(4, 0, 0) and d[1] == r.Vector3d(1, 0, 0)   # domain [0, length]
    with pytest.raises(RuntimeError):
        line.DerivativeAt(4.0, -1, r.CurveEvaluationSide.Default)                    # InvalidOperationException in .NET


@pytest.mark.skipif(not hasattr(r.NurbsCurve, 'CreateFromLine'), reason='G85')
def test_hidden_helpers_child_pointers_and_class_usings():
    # G85: bodies calling non-public helpers (SetVertex, GetPoint), a child view's
    # parent pointer (`m_mesh.NonConstPointer()`), `using` over a class local
    P = r.Point3d
    m = r.Mesh()
    v = m.Vertices
    v.Add(P(0, 0, 0)); v.Add(1.0, 0.0, 0.0); v.Add(r.Point3f(0, 1, 0))
    assert v.Count == 3 and len(v.ToFloatArray()) == 9
    nc = r.NurbsCurve.CreateFromLine(r.Line(P(0, 0, 0), P(3, 0, 0)))
    assert isinstance(nc, r.NurbsCurve) and nc.PointAtEnd == P(3, 0, 0)
    assert r.NurbsCurve.CreateFromLine(r.Line(P(1, 1, 1), P(1, 1, 1))) is None   # invalid line
    box = r.Box(r.Plane.WorldXY, r.Interval(0, 1), r.Interval(0, 2), r.Interval(0, 3))
    ext = r.Extrusion.CreateBoxExtrusion(box, True)
    assert isinstance(ext, r.Extrusion) and ext.IsSolid and ext.CapCount == 2
    ns = r.NurbsSurface.Create(3, False, 2, 2, 2, 2)
    ns.Points.SetPoint(1, 1, 1.0, 1.0, 0.0)
    cp = ns.Points.GetControlPoint(1, 1)
    assert cp.Location == P(1, 1, 0) and cp.Weight == 1.0


@pytest.mark.skipif(not hasattr(r.NurbsSurface, 'CreateRuledSurface'), reason='G86')
def test_g86_singles():
    # G86: optional arguments as `x?.ConstPointer() ?? IntPtr.Zero`; a public target
    # inlined for a null argument; construct-as-wrap; GcProtect; GetConstArray, an
    # object's own pointer and internal constructors in class bodies; wrapped natives
    P = r.Point3d
    f = r.File3dm()
    iid = f.Objects.AddInstanceObject(0, r.Transform.Identity, None)
    assert isinstance(iid, uuid.UUID)
    fr = r.FileReference.CreateFromFullPath('/tmp/a.png')
    assert fr.FullPath.endswith('a.png')
    a = r.LineCurve(P(0, 0, 0), P(1, 0, 0))
    b = r.LineCurve(P(0, 1, 0), P(1, 1, 0))
    rs = r.NurbsSurface.CreateRuledSurface(a, b)
    assert isinstance(rs, r.NurbsSurface)
    pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
    nc = r.NurbsCurve.Create(False, 3, pts)
    assert isinstance(nc, r.NurbsCurve) and nc.PointAtEnd == P(3, 1, 0)
    assert r.NurbsCurve.Create(False, 3, [P(0, 0, 0)]) is None       # count < 2
    assert isinstance(r.Curve.CreateControlPointCurve(pts[:2], 3), r.LineCurve)   # two points: a line
    assert isinstance(r.Curve.CreateControlPointCurve(pts, 1), r.PolylineCurve)  # degree 1: a polyline
    assert isinstance(r.Curve.CreateControlPointCurve(pts, 3), r.NurbsCurve)
    tm = r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, r.Interval(0, 1), r.Interval(0, 1), r.Interval(0, 1))
    assert tm.HasId in (True, False)                                    # ModelComponent's, inherited now


@pytest.mark.skipif(not hasattr(r.File3dm, 'FromByteArray'), reason='G86')
def test_g86_outs_rectangles_and_byte_buffers(tmp_path):
    # G86: an `out Polyline` held by pointer and owned by the caller; Rectangle as the
    # old bindings' (x, y, width, height); a byte[] argument from any Python buffer
    P = r.Point3d
    crv = r.PolylineCurve([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
    ok, pl = crv.TryGetPolyline()
    assert ok and isinstance(pl, r.Polyline) and pl.Count == 3
    ok, pl = r.ArcCurve(r.Circle(P(0, 0, 0), 1.0)).TryGetPolyline()
    assert not ok and pl is None
    vp = r.ViewportInfo()
    vp.SetScreenPort(0, 640, 480, 0, 0, 0)
    port = vp.GetScreenPort()
    assert tuple(port[0] if isinstance(port[0], tuple) else port)[:4] == (0, 0, 640, 480)
    f = r.File3dm()
    f.Objects.AddPoint(P(1, 2, 3), None)
    path = str(tmp_path / 'b.3dm')
    assert f.Write(path, 8)
    data = open(path, 'rb').read()
    g = r.File3dm.FromByteArray(data)
    assert isinstance(g, r.File3dm) and g.Objects.Count == 1
    assert r.File3dm.FromByteArray(bytearray(data)).Objects.Count == 1


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderMaterials'), reason='G87')
def test_file3dm_render_content_collections():
    # G87: File3dmRenderContent proxies of (file, id); RenderMaterials/Environments/Textures
    # walk the manifest's RenderContent components and keep their own kind (`is T`)
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(path)
    # the old binding's RenderContent (id, kind, name), frozen at the switch-over (M3)
    old_rc = [('16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'),
              ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'),
              ('ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal')]
    oldc = [(i, k) for i, k, _ in old_rc]
    mats, envs, texs = list(f.RenderMaterials), list(f.RenderEnvironments), list(f.RenderTextures)
    assert all(isinstance(m, r.File3dmRenderMaterial) for m in mats)
    assert all(isinstance(e, r.File3dmRenderEnvironment) for e in envs)
    assert all(isinstance(t, r.File3dmRenderTexture) for t in texs)
    gen = sorted((str(c.Id), c.Kind) for c in mats + envs + texs)
    top = [k for k in oldc if any(k[0] == g[0] for g in gen)]
    assert gen == top and len(gen) >= 1                     # the same contents, the same kinds
    assert {c.Kind for c in mats} <= {'material'} and {c.Kind for c in envs} <= {'environment'}
    for m in mats:
        assert isinstance(m.ToMaterial(), r.Material) and m.IsTopLevel
        assert m.Name == next(n for i, _, n in old_rc if i == str(m.Id))


@pytest.mark.skipif(not hasattr(r, 'AnnotationBase') or not hasattr(r.AnnotationBase, 'ParentDimensionStyle'), reason='G88')
def test_annotation_parent_dimension_style_is_modeled():
    # G88: RhinoCommon's managed m_parent_dimstyle -- the setter stores the style (a File3dm
    # view as a light copy: a new view of the same file and id) and sets DimensionStyleId;
    # every member that passes the parent style to its native now passes this one
    f = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    ann = next(g for g in (o.Geometry for o in f.Objects) if isinstance(g, r.AnnotationBase))
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


@pytest.mark.skipif(not hasattr(r.AnnotationBase, 'GetBoundingBox'), reason='G89')
def test_annotation_bounding_box_overrides_geometry_base():
    # G89: AnnotationBase overrides GeometryBase.GetBoundingBox(Transform) (a virtual on the
    # generated base, so a GeometryBase-typed call reaches it) and measures with the
    # modeled parent dimension style
    f = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    ann = next(g for g in (o.Geometry for o in f.Objects) if isinstance(g, r.AnnotationBase))
    ann.ParentDimensionStyle = list(f.AllDimStyles)[0]
    bb = ann.GetBoundingBox(r.Transform.Identity)
    assert bb.IsValid
    moved = ann.GetBoundingBox(r.Transform.Translation(r.Vector3d(10, 0, 0)))
    assert abs(moved.Min.X - bb.Min.X - 10) < 1e-9 and abs(moved.Max.X - bb.Max.X - 10) < 1e-9
    assert r.GeometryBase.GetBoundingBox(ann, r.Transform.Identity).Min.X == bb.Min.X


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderContent') or
                    not hasattr(r.File3dmRenderContent, 'GetParameter'), reason='G89')
def test_render_content_find_and_variant_parameters():
    # G89: collections Find(Guid) via the manifest; Get/SetParameter carry RhinoCommon's
    # Variant as a language value by its runtime type (D40)
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
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


@pytest.mark.skipif(not hasattr(r, 'File3dmRenderContent') or
                    not hasattr(r.File3dmRenderContent, 'FindChild'), reason='G89')
def test_render_content_parent_link_and_children():
    # G89: RhinoCommon's managed m__parent for a child content -- a light copy of the parent
    # proxy the child holds (Parent / TopLevel hand it out); Children / FindChild make new
    # child proxies of the same file. The oracle: the old FirstChild / NextSibling chains, frozen (M3).
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(path)
    old_chains = {'ab776f2b-7763-4c53-ac15-d02d3040ff26': [],
                  '660433b2-4c4f-459e-962c-8f70942c5ccd': ['f3785eac-0099-4df4-9666-81c5158011a9'],
                  '16421078-5d0e-6851-2d34-062d266c3949': ['63bae601-b151-4a2a-bf62-9e5df2dc5293']}
    tops = [c for c in list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)]
    with_kids = [c for c in tops if list(c.Children)]
    assert with_kids
    for top in tops:
        assert top.Parent is None and top.TopLevel is top              # `return this` (borrowed)
        chain = old_chains[str(top.Id)]
        kids = list(top.Children)
        assert [str(c.Id) for c in kids] == chain
        for c in kids:
            assert isinstance(c, r.File3dmRenderContent) and c.IsChild and not c.IsTopLevel
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
@pytest.mark.skipif(not hasattr(r.SubDVertex, 'Edges'), reason='G89')
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


@pytest.mark.skipif(not hasattr(r.ViewInfo, 'Viewport'), reason='G89')
def test_view_info_viewport_is_a_held_view():
    # G89: `m_viewport ?? (m_viewport = new ViewportInfo(this))` -- a view of the ViewInfo's
    # own viewport, made once and held; its first write copies (RhinoCommon has no writable
    # branch), so the ViewInfo itself keeps its camera, and the held view keeps the edit
    v = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm')).Views[0]
    vp = v.Viewport
    assert vp is v.Viewport                                       # the held object, every time
    # the camera the old binding read from the file (frozen, M3)
    assert (vp.CameraLocation.X, vp.CameraLocation.Y, vp.CameraLocation.Z) == (-1039.436783184503, -313.98699366950444, 743.7751118904666)
    assert vp.SetCameraLocation(r.Point3d(1, 2, 3))
    again = v.Viewport.CameraLocation
    assert (again.X, again.Y, again.Z) == (1, 2, 3)               # the edit stays with the held view
    del v
    gc.collect()
    assert vp.CameraLocation.Z == 3                               # the view keeps its ViewInfo alive


@pytest.mark.skipif(not hasattr(r.Brep, 'TryConvertBrep'), reason='G89')
def test_try_convert_brep_views_and_copies():
    # G89 (Identity, D42): RhinoCommon returns the argument itself when it is a File3dm view;
    # here a NEW view of the same file brep -- equal content, its own copy-on-write
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    g = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, r.Brep))
    b = r.Brep.TryConvertBrep(g)
    assert isinstance(b, r.Brep) and b is not g and b.Faces.Count == g.Faces.Count
    x0 = g.GetBoundingBox(r.Transform.Identity).Min.X
    assert b.Transform(r.Transform.Translation(r.Vector3d(100, 0, 0)))
    assert abs(b.GetBoundingBox(r.Transform.Identity).Min.X - x0 - 100) < 1e-6
    assert g.GetBoundingBox(r.Transform.Identity).Min.X == x0      # the argument is untouched
    del f, g
    gc.collect()
    assert b.Faces.Count > 0                                         # the view kept the file alive
    own = box_brep()                                                 # owns its pointer: BrepForm
    c = r.Brep.TryConvertBrep(own)
    assert c is not own and c.Faces.Count == own.Faces.Count


@pytest.mark.skipif(not hasattr(r, 'Bitmap'), reason='G90 hand bitmaps')
def test_hand_bitmaps_add_find_iterate_round_trip(tmp_path):
    # G90: the kept hand-written bitmap API with the review fixes -- a Bitmap can be added
    # (the old Bitmap() was an ON_Bitmap, which ONX_Model cannot copy: #38) and Add returns
    # the file's copy; FindId finds images (#35); iteration survives a Delete (#24);
    # FindIndex is positional (images carry no model index) and returns None past the end
    f = r.File3dm()
    paths = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png']
    ids = []
    for p in paths:
        b = r.Bitmap()
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
    g = r.File3dm.Read(out)
    assert sorted(b.FileFullPath for b in g.Bitmaps) == sorted(paths[1:])
    bm = next(iter(g.Bitmaps))
    del g
    gc.collect()
    assert bm.FileFullPath in paths                                # the view keeps the file alive


@pytest.mark.skipif(not hasattr(r, 'ManifestTable'), reason='G91')
def test_manifest_table_find_count_enumerate():
    # G91: File3dm.Manifest, generated from RhinoCommon's File3dmManifestTable -- Find* make
    # the proxy Instantiate makes for each type; GetEnumerator(type) walks one type
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    m = f.Manifest
    T = r.ModelComponentType
    assert m.Count == len(m) and m.LongCount == m.Count and m.ComponentType == T.Mixed
    layers = list(f.AllLayers)
    assert layers and m.ActiveObjectCount(T.Layer) == f.AllLayers.Count
    viaenum = list(m.GetEnumerator(T.Layer))
    assert all(isinstance(x, r.Layer) for x in viaenum)
    assert [str(x.Id) for x in viaenum] == [str(l.Id) for l in layers]
    l0 = layers[0]
    for found in (m.FindId(l0.Id), m.FindId(l0.Id, T.Layer), m.FindIndex(l0.Index, T.Layer),
                  m.FindName(l0.Name, T.Layer, uuid.UUID(int=0))):
        assert isinstance(found, r.Layer) and found.Id == l0.Id and found.Name == l0.Name
    assert m.FindId(uuid.uuid4()) is None
    mats = list(m.GetEnumerator(T.RenderMaterial))
    assert all(isinstance(x, r.Material) for x in mats)
    objs = list(m.GetEnumerator(T.ModelGeometry))
    assert all(isinstance(x, r.File3dmObject) for x in objs) and len(objs) == len(list(f.Objects))
    with pytest.raises(IndexError):
        m.FindIndex(0, T.Unset)                                      # ArgumentOutOfRangeException
    # the whole manifest: Instantiate throws for a type it cannot make (TextureMapping,
    # HistoryRecord, ...), exactly as RhinoCommon's foreach does (pilot #39)
    unmade = [t for t in (T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle) if m.ActiveObjectCount(t)]
    if unmade:
        with pytest.raises(RuntimeError):
            list(m)
    else:
        assert all(isinstance(x, r.ModelComponent) for x in m)
    del f
    gc.collect()
    assert viaenum[0].Name == l0.Name                               # items keep the file alive


@pytest.mark.skipif(not hasattr(r, 'ManifestTable') or not hasattr(r, 'Bitmap'), reason='G91')
def test_manifest_images_are_bitmapx():
    # G91, D44: RhinoCommon's Instantiate makes a BitmapEntry for an image (no working member
    # outside Rhino: pilot #37); here the kept Bitmap stands in, made from the image's id
    f = r.File3dm()
    b = r.Bitmap()
    b.FileFullPath = '/tmp/rh3dm_manifest.png'
    added = f.Bitmaps.Add(b)
    imgs = list(f.Manifest.GetEnumerator(r.ModelComponentType.Image))
    assert len(imgs) == 1 and isinstance(imgs[0], r.Bitmap)
    assert imgs[0].FileFullPath == '/tmp/rh3dm_manifest.png' and imgs[0].Id == added.Id
    assert f.Manifest.FindId(added.Id).FileFullPath == '/tmp/rh3dm_manifest.png'


@pytest.mark.skipif(not hasattr(r.Layer, 'FullPath'), reason='G92')
def test_layer_full_path_and_name_override():
    # G92: Layer.FullPath walks the file's layer table through parents (RhinoCommon's
    # File3dm_GetLayerPathName); Layer.Name is RhinoCommon's override (base.Name) and stays
    # writable; ToString is FullPath
    f = r.File3dm()
    t = f.AllLayers
    black = (0, 0, 0, 255)
    t.AddLayer('top', black)
    top = list(t)[0]
    t.AddLayer('mid', black, top.Id)
    mid = next(l for l in t if l.Name == 'mid')
    t.AddLayer('leaf', black, mid.Id)
    leaf = next(l for l in t if l.Name == 'leaf')
    sep = r.ModelComponent.NamePathSeparator
    assert top.FullPath == 'top'
    assert leaf.FullPath == sep.join(['top', 'mid', 'leaf'])
    assert leaf.ToString() == leaf.FullPath
    leaf.Name = 'leaf2'                                    # the override keeps the base setter
    assert next(l for l in t if l.Id == leaf.Id).FullPath.endswith(sep + 'leaf2')
    assert r.Layer().FullPath == r.Layer().Name           # not in a file: its name


@pytest.mark.skipif(not hasattr(r.File3dmGroupTable, 'GroupMembers'), reason='G92')
def test_group_members_and_find_by_group():
    # G92: File3dmGroupTable.GroupMembers / File3dmObjectTable.FindByGroup -- RhinoCommon's
    # loop over the objects; an object in no group has no group list (null in .NET, which
    # makes RhinoCommon's own loop throw: pilot #40) and is skipped here (D35)
    f = r.File3dm()
    g0 = f.AllGroups.AddGroup()
    g1 = f.AllGroups.AddGroup()
    def add(groups):
        a = r.ObjectAttributes()
        for g in groups:
            a.AddToGroup(g)
        return f.Objects.AddPoint(r.Point3d(len(groups), 0, 0), a)
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
    # types' are (BoundingBox.Empty) -- not a method
    assert isinstance(r.MeshingParameters.Default, r.MeshingParameters)
    assert r.Material.MaxShine == 255.0
    assert isinstance(r.ModelComponent.NamePathSeparator, str)
    assert isinstance(r.ObjectAttributes.OCSMappingChannelId, int)
    assert isinstance(r.SubD.Empty, r.SubD)
    assert not callable(r.MeshingParameters.Default)


def test_retired_arc_arccurve_brep_members():
    # Luis, 2026-10-16 (api/retired.toml): rhino3dm-only members and their RhinoCommon forms, on the same
    # input; the old members' results frozen at the switch-over (M3)
    na = r.Arc(r.Point3d(0, 0, 0), r.Point3d(1, 1, 0), r.Point3d(2, 0, 0))
    assert abs(na.Angle - 3.141592653589793) < 1e-12                     # Arc.AngleRadians -> Arc.Angle
    def same(old, nc):                                                   # old: (T0, T1, end X, Y, Z)
        assert abs(old[0] - nc.Domain.T0) < 1e-12 and abs(old[1] - nc.Domain.T1) < 1e-12
        b = nc.PointAtEnd
        assert abs(old[2] - b.X) < 1e-9 and abs(old[3] - b.Y) < 1e-9 and abs(old[4] - b.Z) < 1e-9
    same((0.0, 3.141592653589793, 2.0, 0.0, 0.0), r.ArcCurve(na))                       # CreateFromArc -> ArcCurve(arc)
    same((2.0, 5.0, 2.0, 0.0, 0.0), r.ArcCurve(na, 2.0, 5.0))                           # CreateFromArcParams
    same((0.0, 18.84955592153876, 3.0, 0.0, 0.0), r.ArcCurve(r.Circle(3.0)))           # CreateFromCircle
    same((1.0, 4.0, 3.0, 0.0, 0.0), r.ArcCurve(r.Circle(3.0), 1.0, 4.0))               # CreateFromCircleParams
    # Brep.CreateFromBoundingBox -> Brep.CreateFromBox(BoundingBox) / CreateFromBox(Box)
    bb = r.BoundingBox(r.Point3d(0, 0, 0), r.Point3d(1, 2, 3))
    nb = r.Brep.CreateFromBox(bb)
    assert nb.Faces.Count == r.Brep.CreateFromBox(r.Box(bb)).Faces.Count == 6
    # BrepTrim.EdgeIndex / StartVertexIndex / EndVertexIndex -> trim.Edge.EdgeIndex, trim.StartVertex.VertexIndex, ...
    old_trims = [(0, 0, 1), (0, 1, 0), (1, 1, 2), (1, 2, 1), (2, 2, 3), (2, 3, 2), (3, 0, 3), (3, 3, 0), (4, 4, 5),
                 (4, 5, 4), (5, 5, 6), (5, 6, 5), (6, 6, 7), (6, 7, 6), (7, 4, 7), (7, 7, 4), (8, 0, 4), (8, 4, 0),
                 (9, 1, 5), (9, 5, 1), (10, 2, 6), (10, 6, 2), (11, 3, 7), (11, 7, 3)]
    new_trims = sorted((t.Edge.EdgeIndex if t.Edge else -1, t.StartVertex.VertexIndex, t.EndVertex.VertexIndex)
                       for t in (nb.Trims[i] for i in range(len(nb.Trims))))
    assert old_trims == new_trims
    # BrepVertex.EdgeCount -> len(vertex.EdgeIndices())
    assert [len(nb.Vertices[i].EdgeIndices()) for i in range(len(nb.Vertices))] == [3] * 8


def test_retired_viewinfo_vector_sun_shutlining_members(tmp_path):
    # Luis, 2026-10-16 (api/retired.toml); the old members' results frozen at the switch-over (M3)
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    assert f.Views[0].WallpaperFilename == ''                               # ViewInfo.WallpaperName
    assert r.Vector3d(1, 2, 3) * r.Vector3d(-4, 5, 0.5) == 7.5           # Vector3d.DotProduct -> the * operator
    old_sun = {-10.0: (0, 0, 64), 0.0: (255, 160, 24), 5.0: (255, 197, 69), 12.5: (255, 233, 162),
               30.0: (251, 252, 255), 60.0: (251, 252, 255), 89.0: (251, 252, 255)}
    for alt, oc in old_sun.items():
        nc = r.Sun.ColorFromAltitude(alt)                                  # Sun.SunColorFromAltitude
        # RhinoCommon rounds 255*c to the nearest integer; the old binding truncated 255.5*c (D45)
        assert all(abs(x - y) <= 1 for x, y in zip(oc, nc[:3])), (alt, oc, nc)
    # ShutLining.Curves (a table of curve objects) -> GetCurves() ids + the id-keyed accessors, through a file
    cid = uuid.uuid4()
    wf = r.File3dm()
    attrs = r.ObjectAttributes()
    mm = attrs.File3dmMeshModifiers
    assert mm.Thickening is None
    assert mm.CreateThickening() is not None and mm.Thickening is not None
    sl0 = mm.CreateShutLining()
    assert sl0.AddCurve(cid) == cid
    sl0.SetCurveRadius(cid, 2.5); sl0.SetCurveProfile(cid, 3); sl0.SetCurvePull(cid, True)
    sl0.SetCurveIsBump(cid, True); sl0.SetCurveEnabled(cid, False)
    wf.Objects.AddPoint(r.Point3d(0, 0, 0), attrs)
    out = str(tmp_path / 'shutlining.3dm')
    assert wf.Write(out, 0)
    g = next(iter(r.File3dm.Read(out).Objects))
    sl = g.Attributes.File3dmMeshModifiers.ShutLining
    if sl is None:
        # no mesh modifier made through rhino3dm survives a write and read (pilot-findings #43, U29 in
        # opennurbs); the checks below run once rhino3dm's opennurbs has it
        pytest.xfail('pilot-findings #43: mesh modifiers are lost on write/read')
    assert [str(x) for x in sl.GetCurves()] == [str(cid)]
    assert (sl.CurveRadius(cid), sl.CurveProfile(cid), sl.CurvePull(cid), sl.CurveIsBump(cid), sl.CurveEnabled(cid)) \
        == (2.5, 3, True, True, False)


@pytest.mark.skipif(not hasattr(r.Brep, 'CreateTrimmedPlane'), reason='G95')
def test_class_sequence_parameters(tmp_path):
    # G95: `IEnumerable<Curve>` / `IEnumerable<GeometryBase>` parameters -- the caller's objects,
    # passed as a native pointer array (SimpleArray*Pointer); `new[] { curve }` likewise
    circle = r.ArcCurve(r.Circle(2.0))
    one = r.Brep.CreateTrimmedPlane(r.Plane.WorldXY, circle)
    many = r.Brep.CreateTrimmedPlane(r.Plane.WorldXY, [circle])
    # the old binding's CreateTrimmedPlane: 1 face, 1 edge (frozen, M3)
    assert one.Faces.Count == many.Faces.Count == 1
    assert len(one.Edges) == 1
    # File3dm.WriteMultipleObjects(path, geometry)
    out = str(tmp_path / 'multi.3dm')
    assert r.File3dm.WriteMultipleObjects(out, [circle, one])
    assert len(list(r.File3dm.Read(out).Objects)) == 2
    # File3dmInstanceDefinitionTable.Add(name, description, basePoint, geometry, attributes)
    f = r.File3dm()
    n0 = len(f.AllInstanceDefinitions)
    idx = f.AllInstanceDefinitions.Add('blk', 'desc', r.Point3d(0, 0, 0), [circle], [r.ObjectAttributes()])
    assert idx >= 0 and len(f.AllInstanceDefinitions) == n0 + 1


@pytest.mark.skipif(not hasattr(r.AnnotationBase, 'TextHasRtfFormatting'), reason='G96 not generated')
def test_annotation_text_from_text_runs():
    # G96: RhinoCommon 9 reads an annotation's plain text from its text runs (LINQ over the
    # internal TextRun objects GetTextRuns makes and the pass frees): Text and PlainText are the
    # text runs joined (a newline or paragraph run as \r\n); TextHasRtfFormatting is "more than
    # one text run" (the old binding tests RichText for an {\rtf prefix -- the V8 rule)
    f = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    # the old binding's PlainText of each text entity (frozen, M3)
    old_text = {'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 'Hello World!',
                'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 'Hello Cruel World!'}
    seen = 0
    for o in f.Objects:
        g = o.Geometry
        if not isinstance(g, r.AnnotationBase):
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


@pytest.mark.skipif(not hasattr(r.Texture, 'ProjectionMode'), reason='G96 not generated')
def test_texture_projection_mode_c_layer_enum():
    # G96: ON_Texture_GetProjectionMode returns the C layer's own enum (P/Invoke declares the C#
    # enum itself): the result converts to the binding's enum, the setter's argument back
    t = r.Texture()
    m = r.TextureProjectionModes
    assert t.ProjectionMode == m.MappingChannel
    t.ProjectionMode = m.Wcs
    assert t.ProjectionMode == m.Wcs


@pytest.mark.skipif(not hasattr(r.Sun, 'GetDateTime'), reason='G97 not generated')
def test_sun_date_time():
    # G97: a DateTime is the binding's date -- Python a datetime (naive = the machine's local
    # time, .NET's Local), JS a Date -- through RhinoCommon's Variant time_t, both ways
    import datetime
    s = r.Sun()
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
    a = r.Sun.AltitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, False)
    assert -90.0 <= a <= 90.0


@pytest.mark.skipif(not hasattr(r.Sun, 'GetDateTime'), reason='G97 not generated')
def test_retired_sun_date_members(tmp_path):
    # G97: the old Sun.Year/Month/Day/Hours are Sun.GetDateTime(DateTimeKind.Local)'s fields --
    # the same ON_Sun::LocalDateTime through the C layer's mktime / localtime (Hours to the second;
    # checked against the old setters before the switch-over, M3): through a file and back
    import datetime
    wf = r.File3dm()
    wf.Settings.RenderSettings.Sun.SetDateTime(datetime.datetime(2023, 11, 5, 9, 45), r.DateTimeKind.Local)
    out = str(tmp_path / 'sun.3dm')
    assert wf.Write(out, 0)
    f = r.File3dm.Read(out)
    d = f.Settings.RenderSettings.Sun.GetDateTime(r.DateTimeKind.Local)
    assert (d.year, d.month, d.day) == (2023, 11, 5)
    assert abs(d.hour + d.minute / 60 + d.second / 3600 - 9.75) < 1 / 3600


@pytest.mark.skipif(not hasattr(r.TextureMapping, 'MappingType'), reason='G97 not generated')
def test_retired_texturemapping_members():
    # G97: TextureMapping.MappingType (a switch over the C layer's enum) -- the old IsPeriodic is
    # MappingType in (Sphere, Cylinder); Reverse/Swap/TileTextureCoordinate left-multiply
    # UvwTransform (opennurbs ON_TextureMapping), composed here on the generated object. The old
    # members' results are frozen (M3).
    T = r.TextureMappingType
    I = r.Interval(0, 1)
    news = [r.TextureMapping.CreateSurfaceParameterMapping(),
            r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, I, I, I),
            r.TextureMapping.CreateCylinderMapping(r.Cylinder(r.Circle(1.0), 2.0), True),
            r.TextureMapping.CreateCylinderMapping(r.Cylinder(r.Circle(1.0), 2.0), False),
            r.TextureMapping.CreateSphereMapping(r.Sphere(r.Point3d(0, 0, 0), 2.0)),
            r.TextureMapping.CreateBoxMapping(r.Plane.WorldXY, I, I, I, True)]
    kinds = [n.MappingType for n in news]
    assert kinds == [T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping]
    # the old (IsPeriodic, RequiresVertexNormals) of each
    old = [(False, False), (False, False), (True, True), (True, False), (True, False), (False, True)]
    for n, k, (periodic, normals) in zip(news, kinds, old):
        assert periodic == (k in (T.SphereMapping, T.CylinderMapping))
        # RequiresVertexNormals (D49): RhinoCommon's properties, as ON_TextureMapping decides it
        derived = k != T.SurfaceParameters and (n.Projection == r.Projection.Ray or k == T.BoxMapping
                                                or (k == T.CylinderMapping and n.Capped))
        assert normals == derived, k
    def composed(x):
        n = r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, I, I, I)
        n.UvwTransform = x * n.UvwTransform
        return n.UvwTransform
    def matrix(t):
        return [getattr(t, 'M%d%d' % (i, j)) for i in range(4) for j in range(4)]
    # the old Reverse / Swap / TileTextureCoordinate's UvwTransform on a plane mapping
    old_m = {'ReverseTextureCoordinate': [1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0],
             'SwapTextureCoordinate': [0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0],
             'TileTextureCoordinate': [3.0, 0.0, 0.0, 0.25, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]}
    for name, build in (
            ('ReverseTextureCoordinate', lambda x: (setattr(x, 'M11', -1.0), setattr(x, 'M13', 1.0))),
            ('SwapTextureCoordinate', lambda x: (setattr(x, 'M00', 0.0), setattr(x, 'M22', 0.0), setattr(x, 'M02', 1.0), setattr(x, 'M20', 1.0))),
            ('TileTextureCoordinate', lambda x: (setattr(x, 'M00', 3.0), setattr(x, 'M03', 0.25)))):
        x = r.Transform(1.0)
        build(x)
        assert old_m[name] == matrix(composed(x)), name

def test_retired_rhino_only_pairings():
    # Luis 2026-10-16: old members matched by name to RHINO_SDK-only RhinoCommon members, whose
    # portable equivalents are generated -- Layer.PathSeparator is RhinoCommon's
    # ModelComponent.NamePathSeparator; the old AnnotationBase.GetDimensionScale(parentStyle) is
    # DimensionScale with that parent (D39), not RhinoCommon's view-dependent GetDimensionScale
    # (the old Layer.PathSeparator itself raises TypeError: a static property bound to a
    # zero-argument getter -- pilot #45; the value is opennurbs' ON_ModelComponent::NamePathSeparator)
    assert r.ModelComponent.NamePathSeparator == '::'
    assert r.Layer.NamePathSeparator == '::'
    path = os.path.join(MODELS, 'textEntities_r8.3dm')
    f = r.File3dm.Read(path)
    ds = list(f.AllDimStyles)[0]
    # the old GetDimensionScale(first style) of each annotation (frozen, M3)
    old_scale = {'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 10.0, 'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 10.0}
    n = 0
    for o in f.Objects:
        g = o.Geometry
        if isinstance(g, r.AnnotationBase) and str(o.Attributes.ObjectId) in old_scale:
            g.ParentDimensionStyle = ds
            assert g.DimensionScale == old_scale[str(o.Attributes.ObjectId)]
            n += 1
    assert n > 0


@pytest.mark.skipif(not hasattr(r, 'PostEffectCollection'), reason='G98 not generated')
def test_post_effect_collection_and_data():
    # G98: RenderSettings.PostEffects -- RhinoCommon's portable PostEffectCollection (free-
    # floating: the file's ON_PostEffects) and its PostEffectData items, components of
    # (collection, id) resolved through ON_PostEffects_PostEffectFromId; foreach is the internal
    # PostEffectDataEnumerator (GetAt by position). The oracle: the old PostEffect table, frozen (M3).
    # Type (D50): opennurbs numbers Unset 0, Early 1, ToneMapping 2, Late 3; RhinoCommon's
    # PostEffectType Early 0, ToneMapping 1, Late 2 (an unset type reads as Early)
    old_pe = {'posteffects.3dm': [
        ('57baba9e-fc02-40d2-9682-5f61b9049eaa', 'Despeckle', True, True, 0), ('ce2f1c40-7f25-4c2d-b1d3-271e118d843b', 'Bloom', True, False, 0),
        ('7243f003-c4f1-415c-8228-35e5efaabd60', 'Glow', True, False, 0), ('b4d7b2f3-fa0d-47fb-8063-6869e96dbf35', 'Fog', True, False, 0),
        ('ab3b31ab-7d40-4a59-aa5c-5df833a81e8f', 'Depth of field', True, False, 0), ('f8e993bf-3407-4121-b71a-6876c2541cc7', 'Multiplier', True, False, 0),
        ('1ec87a84-f61f-43dc-b331-92d8b81ed468', 'Denoise', True, True, 0), ('acb8d258-c1d6-499d-aa23-02dcdea2b0a2', 'Clamp', False, False, 1),
        ('64e53275-0583-4ec4-9bde-b9413df25f08', 'Black/White point', False, False, 1), ('a51d4149-3685-463e-b32f-d9ac986b7c1b', 'Logarithmic', False, False, 1),
        ('ec302d96-402e-4244-bf25-5d0859ebf37c', 'Filmic', False, False, 1), ('4723b167-50a6-42ce-bba6-ce4f8013495b', 'False color', False, False, 1),
        ('c65eb6ba-ee66-4c48-9457-95f0936eca08', 'Hue/Saturation/Luminance', True, False, 2), ('0034b235-c351-4674-be3f-afec691187b3', 'Brightness/Contrast', True, False, 2),
        ('4398d28e-ed29-4974-9304-d56d0e61cc15', 'Noise', True, False, 2), ('38986982-3eab-4988-b8cb-32dd4ad0baa3', 'Gaussian blur', True, False, 2)],
        'file3dm_stuff.3dm': [], 'textEntities_r8.3dm': [], 'sphereDecals.3dm': []}
    seen = 0
    for name, old in old_pe.items():
        path = os.path.join(MODELS, name)
        f = r.File3dm.Read(path)
        coll = f.Settings.RenderSettings.PostEffects
        new = [(str(p.Id), p.LocalName, p.On, p.Shown, int(p.Type)) for p in coll]
        assert new == old, name
        seen += len(new)
        # the old table's FindId / FindIndex (triage H1: map) -> PostEffectDataFromId / the i-th item
        items = list(coll)
        for i, (oid, oname, _, _, _) in enumerate(old):
            assert str(items[i].Id) == oid and items[i].LocalName == oname, (name, i)
            assert coll.PostEffectDataFromId(uuid.UUID(oid)).LocalName == oname
        ghost = coll.PostEffectDataFromId(uuid.uuid4())
        with pytest.raises(RuntimeError):
            ghost.LocalName                                  # "PostEffectData.CppPointer failed"
        if new:
            d = coll.PostEffectDataFromId(uuid.UUID(new[0][0]))
            assert str(d.Id) == new[0][0]
            on = d.On
            d.On = not on                                   # writes go into the file's object
            assert next(p for p in coll if str(p.Id) == new[0][0]).On == (not on)
    assert list(r.PostEffectCollection()) == []             # free-floating: an empty ON_PostEffects
    assert seen > 0          # tests/models/posteffects.3dm (Luis 2026-10-29) carries post effects


@pytest.mark.skipif(not hasattr(r.DimensionStyle, 'Font'), reason='G99 not generated')
def test_font_is_managed_and_dimension_style_font():
    # G99 (Luis: option A): RhinoCommon's Font is a MANAGED font -- an entry of opennurbs' process-
    # wide list, shared, immutable, never freed -- and so is Font (a borrowed pointer, never
    # deleted). DimensionStyle.Font hands out the style's managed font; the constructors and the
    # finders return managed fonts too (`Font(familyName)` delegates with RhinoCommon's defaults)
    a, b = r.Font('Arial'), r.Font('Arial')
    assert a.FamilyName == b.FamilyName and a.Description == b.Description
    del a
    gc.collect()                                             # dropping one frees nothing shared
    assert b.FamilyName and r.Font('Arial').Description == b.Description
    f = r.File3dm.Read(os.path.join(MODELS, 'textEntities_r8.3dm'))
    old_faces = {'3eb2bbaf-5018-485f-be99-568fd0918a84': 'Regular', '3ca66641-8cfc-447e-9ae9-ca6a21cfb828': 'Regular',
                 '6d104644-18b4-464c-a49c-dd4892fcbe39': 'Regular', '7f6f1c3a-f6a7-4c30-ae0e-44b5631d9c65': 'Regular'}
    for ds in f.AllDimStyles:
        assert ds.Font.FaceName == old_faces[str(ds.Id)]     # the same managed font (the old binding's, frozen: M3)
    s = r.DimensionStyle()
    s.Font = r.Font('Courier New')
    got = s.Font
    del s
    gc.collect()
    assert got.FamilyName                                     # managed: outlives the style
    for name in ('FromQuartetProperties',):
        q = r.Font.FromQuartetProperties('Arial', False, False)
        assert q is None or q.FamilyName
    # G99 side effect: setters whose C argument is an opennurbs scoped enum (P/Invoke passes the value)
    s2 = r.DimensionStyle()
    s2.FitText = s2.FitText
    s2.FitArrow = s2.FitArrow


def test_translated_list_indexers_g101():
    # class-mode indexers (G101): a list's `this[int]` gives __getitem__ / __setitem__ / __len__,
    # so the list iterates (Python's item protocol stops at IndexError: IndexOutOfRangeException)
    P = r.Point3d
    m = quad_mesh()
    vs = m.Vertices
    assert len(vs) == 4
    assert [(p.X, p.Y) for p in vs] == [(0, 0), (1, 0), (1, 1), (0, 1)]
    vs[2] = r.Point3f(5, 6, 7)
    assert (vs[2].X, vs[2].Z) == (5, 7)
    with pytest.raises(IndexError):
        vs[4]
    assert len(m.Faces) == 1 and [f.D for f in m.Faces] == [3]
    nc = r.NurbsCurve.Create(False, 3, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)])
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
    ns = r.NurbsSurface.Create(3, False, 2, 2, 3, 2)
    pts = ns.Points
    ns.Points.SetPoint(2, 1, 5.0, 6.0, 7.0)
    cps = list(pts)
    assert len(cps) == pts.CountU * pts.CountV == 6
    assert cps[2 * pts.CountV + 1].Location == r.Point3d(5, 6, 7)


def test_brep_surfaces_and_view_table_g103():
    # BrepSurfaceList: RhinoCommon's lazily filled wrapper cache is analyzed as its per-item view
    # (SurfaceHolder(brep, index)); a view per access, keeping the brep alive
    import gc
    b = box_brep()
    srfs = b.Surfaces
    assert len(srfs) == 6
    items = list(srfs)
    assert len(items) == 6 and all(isinstance(s, r.Surface) and s.IsPlanar() for s in items)
    with pytest.raises(IndexError):
        srfs[6]
    s0 = srfs[0]
    del b, srfs, items
    gc.collect()
    assert s0.IsPlanar()                                   # the view keeps its brep
    # File3dmViewTable: the stored m_named_views flag (registry state) reaches translated bodies
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
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
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    views = list(f.Views)
    assert len(views) == f.Views.Count > 0 and all(isinstance(v, r.ViewInfo) for v in views)
    v = f.Views[0]
    v.Name = 'rh3dm-live'
    assert f.Views[0].Name == 'rh3dm-live'                 # write-through, not a copy
    del f, views
    gc.collect()
    assert v.Name == 'rh3dm-live'                          # the view keeps its file
    assert r.ViewInfo().Name == ''                        # a free-standing ViewInfo still owns its own
    with pytest.raises(IndexError):
        r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm')).Views[99]
    # File3dmPlugInDataTable[i] (Luis: generate it): a (file, plug-in id) record
    g = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
    pd = list(g.PlugInData)
    assert len(pd) == g.PlugInData.Count
    assert all(isinstance(p, r.File3dmPlugInData) and isinstance(p.PlugInId, uuid.UUID) for p in pd)


def test_viewport_screen_port_g105():
    # RhinoCommon's ScreenPort (get/set), GetScreenPort() -- its plain overload, not the
    # (rect, near, far) out form (G105) -- and the Rectangle overloads: a Rectangle is (x, y, w, h)
    v = r.ViewportInfo()
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
    d = r.DracoCompression.Compress(m)
    b = d.ToByteArray()
    assert isinstance(b, bytes) and len(b) > 0
    g = r.DracoCompression.DecompressByteArray(b)
    assert isinstance(g, r.Mesh) and g.Vertices.Count == 4 and g.Faces.Count >= 1
    g64 = r.DracoCompression.DecompressBase64String(d.ToBase64String())
    assert isinstance(g64, r.Mesh) and g64.Vertices.Count == 4
    assert d.VertexAttributePosition >= 0
    # options: the data class (properties and public fields; DracoColorFormat a generated enum)
    o = r.DracoCompressionOptions()
    assert (o.CompressionLevel, o.PositionQuantizationBits, o.IncludeNormals) == (7, 14, True)
    o.IncludeNormals = False
    o.VertexColorFormat = r.DracoColorFormat.RGBA
    o.PositionQuantizationBits = 11
    d2 = r.DracoCompression.Compress(m, o)
    assert d2.NormalAttributePosition == -1                   # normals left out
    # a point cloud
    pc = r.PointCloud()
    for i in range(5):
        pc.Add(r.Point3d(i, 2 * i, 0))
    dp = r.DracoCompression.Compress(pc)
    gp = r.DracoCompression.DecompressByteArray(dp.ToByteArray())
    assert isinstance(gp, r.PointCloud) and gp.Count == 5
    # files: Write / DecompressFile; a missing file is FileNotFoundError, as in .NET
    path = str(tmp_path / 'quad.drc')
    assert d.Write(path) is True
    assert r.DracoCompression.DecompressFile(path).Vertices.Count == 4
    with pytest.raises(FileNotFoundError):
        r.DracoCompression.DecompressFile(str(tmp_path / 'missing.drc'))


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
    # the old binding's SharpEdgeCount property per SubD (frozen, M3)
    for name, olds in (('subd.3dm', [90]), ('subdBox.3dm', [0]), ('subd_creases.3dm', [3])):
        path = os.path.join(MODELS, name)
        subds = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD)]
        assert len(subds) == len(olds)
        for s, old in zip(subds, olds):
            n = s.SharpEdgeCount()
            assert n == sum(1 for e in s.Edges if e.IsSharp)
            assert n == old
            assert all(isinstance(e.Sharpness, r.SubDEdgeSharpness) for e in s.Edges)
            errors = r.SubD.ErrorCount
            assert s.ClearEdgeSharpness() == n             # the number of edges changed
            assert s.SharpEdgeCount() == 0
            assert r.SubD.ErrorCount == errors            # a process-wide counter: no new errors
            total += n
    print('sharp edges in the SubD fixtures:', total)


def test_subd_edge_sharpness_value_type_g109():
    # G109: SubDEdgeSharpness as a translated value type (two floats, as ON_SUBD_EDGE_SHARPNESS_STRUCT)
    S = r.SubDEdgeSharpness
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
    P, V = r.Point3d, r.Vector3d
    for n, i, k in ((4, 1, 1), (4, 0, 2), (4, 3, 1), (2, 0, 5), (5, 2, 3), (6, 1, 2), (3, 3, 2)):
        pc = r.PointCloud()
        pc.AddRange([P(j, 0, 0) for j in range(n)], [V(0, 0, j + 1) for j in range(n)])
        pc.InsertRange(i, [P(90 + j, 0, 0) for j in range(k)])
        assert [p.X for p in pc.GetPoints()] == list(range(i)) + [90 + j for j in range(k)] + list(range(i, n)), (n, i, k)
        assert [v.Z for v in pc.GetNormals()] == [j + 1 for j in range(i)] + [0] * k + [j + 1 for j in range(i, n)], (n, i, k)
        assert pc.Count == n + k
    pc = r.PointCloud()
    pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
    pc.InsertRange(1, [])                                  # nothing to insert: unchanged
    assert [p.X for p in pc.GetPoints()] == [0, 1]
    with pytest.raises(IndexError, match='larger than or equal to zero'):
        pc.InsertRange(-1, [P(9, 9, 9)])
    with pytest.raises(IndexError, match='smaller than or equal to Count'):
        pc.InsertRange(3, [P(9, 9, 9)])


def test_mesh_normal_list_is_mesh_vertex_normal_list():
    # the old MeshNormalList is RhinoCommon's MeshVertexNormalList (registry 'old' pairing): the same
    # operations on the same mesh give the same normals, step by step (the old values frozen, M3)
    def folded():
        m = r.Mesh()
        for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (2, 0, 1), (2, 1, 1)]:
            m.Vertices.Add(*[float(c) for c in xyz])
        m.Faces.AddFace(0, 1, 2, 3)
        m.Faces.AddFace(1, 4, 5, 2)
        return m
    xyz = lambda v: (v.X, v.Y, v.Z)
    vals = lambda l: [xyz(l[i]) for i in range(len(l))]
    a, b = 0.3826834261417389, 0.9238795638084412
    c = 0.7071067690849304
    computed = [(0.0, 0.0, 1.0), (-a, 0.0, b), (-a, 0.0, b), (0.0, 0.0, 1.0), (-c, 0.0, c), (-c, 0.0, c)]
    new = folded().Normals
    assert new.ComputeNormals()
    assert len(new) == 6 and vals(new) == computed
    assert len(set(vals(new))) > 1                       # the fold: not all one normal
    new.Flip()
    assert vals(new) == [(-x, -y, -z) for x, y, z in computed]
    # D66: the old Add went through ON_Mesh::SetVertexNormal, which unitizes (it stored (0, 0, 1));
    # RhinoCommon's appends the vector as given (UnitizeNormals then refuses: 7 normals, 6 vertices --
    # as the old one did)
    assert new.Add(0.0, 0.0, 2.0) == 6
    assert xyz(new[6]) == (0, 0, 2)
    assert new.UnitizeNormals() is False
    assert vals(new)[:6] == [(-x, -y, -z) for x, y, z in computed]
    new.Clear()
    assert len(new) == 0
    new = folded().Normals
    new.ComputeNormals()
    new.Destroy()
    assert len(new) == 0



def test_instance_reference_geometry_matches_old_instance_reference():
    # triage batch B: RhinoCommon's InstanceReferenceGeometry is the old InstanceReference (its
    # ParentIdefId and Xform, frozen at the switch-over: M3)
    def xf(m03, m13):
        return [1.0, 0.0, 0.0, m03, 0.0, 1.0, 0.0, m13, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]
    old = {'blocks.3dm': [('388f436f-d108-4b1a-9a98-bc48f9593dc4', xf(-78.83008444266686, 0.0)),
                          ('388f436f-d108-4b1a-9a98-bc48f9593dc4', xf(42.29887373318087, 0.0))],
           'file3dm_stuff.3dm': [('a6a736ea-89f8-4d26-badc-d7556278f055', xf(0.0, 0.0)),
                                 ('a6a736ea-89f8-4d26-badc-d7556278f055', xf(0.0, -190.71648594710535))]}
    for name, olds in old.items():
        path = os.path.join(MODELS, name)
        new = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.InstanceReferenceGeometry)]
        assert len(new) == len(olds) == 2
        for n, (idef, cells_old) in zip(new, olds):
            assert str(n.ParentIdefId) == idef
            cells = ['M%d%d' % (i, j) for i in range(4) for j in range(4)]
            assert [getattr(n.Xform, c) for c in cells] == cells_old



def test_dimension_classes_and_display_lines_g111():
    # triage batch B: the dimension classes are generated, files hand them out as their own class,
    # and GetDisplayLines returns (rc, lines) -- lines in the dimension's plane (parity: dim.*.displaylines)
    P, S = r.Point3d, r.DimensionStyle()
    od = r.OrdinateDimension.Create(S, r.Plane.WorldXY, r.OrdinateDimension.MeasuredDirection.Xaxis,
                                     P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
    ld = r.LinearDimension.Create(r.AnnotationType.Aligned, S, r.Plane.WorldXY, r.Vector3d(1, 0, 0),
                                   P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    for d in (od, ld):
        d.UpdateDimensionText(S, r.UnitSystem.Millimeters)      # the text rect needs formatted text
    rc, lines = ld.GetDisplayLines(S, 1.0)
    assert rc and len(lines) > 0 and all(isinstance(l, r.Line) for l in lines)
    orc, olines = od.GetDisplayLines(S, 1.0)                    # (rc, lines): values by parity, not here
    assert isinstance(orc, bool) and isinstance(olines, (list, tuple))
    f = r.File3dm()
    ids = [f.Objects.AddOrdinateDimension(od), f.Objects.AddLinearDimension(ld)]
    back = r.File3dm.FromByteArray(f.ToByteArray(None))
    kinds = {o.Attributes.ObjectId: type(o.Geometry).__name__ for o in back.Objects}
    assert [kinds[i] for i in ids] == ['OrdinateDimension', 'LinearDimension']


def test_create_keeps_its_style_alive_g112():
    # G112: Create stores dimStyle in the modeled ParentDimensionStyle (borrowed, D39): the result
    # keeps the argument alive -- a temporary style must survive the call and a collection
    import gc
    P = r.Point3d
    ld = r.LinearDimension.Create(r.AnnotationType.Aligned, r.DimensionStyle(), r.Plane.WorldXY,
                                   r.Vector3d(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    gc.collect()
    s = ld.ParentDimensionStyle
    assert s is not None and s.TextHeight > 0
    ann = ld
    ann.ParentDimensionStyle = r.DimensionStyle()
    gc.collect()
    assert ann.ParentDimensionStyle.TextHeight > 0


def test_draw_color_from_a_file_u5():
    # #5 (upstream RH-99693): ObjectAttributes.DrawColor(File3dm[, viewportId]) -- Rhino's rules over
    # the model: by layer, by object, by material (the material the attributes SELECT, its PBR base
    # color if physically based). The old extension used m_material_index and Diffuse only (D63).
    f = r.File3dm()
    for c in ((200, 0, 0, 255), (0, 0, 200, 255)):
        m = r.Material(); m.DiffuseColor = c; f.Materials.Add(m)
    layer = r.Layer(); layer.Name = 'L'; layer.Color = (10, 20, 30, 255); layer.RenderMaterialIndex = 1
    f.Layers.Add(layer)
    pbr = r.Material(); pbr.DiffuseColor = (9, 9, 9, 255); pbr.ToPhysicallyBased()
    pbr.PhysicallyBased.BaseColor = r.Color4f(1.0, 0.5, 0.0, 1.0)
    f.Materials.Add(pbr)
    rgb = lambda c: tuple(c)[:3]
    a = r.ObjectAttributes(); a.LayerIndex = 0
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
    # opennurbs calls, now RhinoCommon's (the old getter was GetCachedTextureCoordinatesFromTexture;
    # on this file it gave 92 coordinates for object 0: frozen, M3)
    path = os.path.join(MODELS, 'meshWithTexture.3dm')
    f = r.File3dm.Read(path)
    meshes = [(k, o) for k, o in enumerate(f.Objects) if isinstance(o.Geometry, r.Mesh)]
    assert [k for k, _ in meshes] == [0]
    seen = 0
    for k, o in meshes:
        mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
        assert mat is not None and mat.GetBitmapTexture() is not None
        mesh = o.Geometry
        assert mesh.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat) is None   # void
        tc = mesh.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
        assert tc is not None and tc.Count == 92
        seen += 1
    assert seen >= 1



def test_render_content_child_slots_u4():
    # #7 (U4, upstream RH-99692): File3dmRenderContent.SetChildSlotOn / ChildSlotAmount /
    # SetChildSlotAmount / SetChild -- RhinoCommon's RenderContent shape (slot name first, D64);
    # a slot with no amount reads 0.0 as RenderContent does (the old binding read 100.0)
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(path)
    tops = list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)
    top = next(c for c in tops if list(c.Children))
    slot = list(top.Children)[0].ChildSlotName
    # the old binding: material 660433b2's slot 'bitmap-texture' was off (frozen, M3)
    assert (str(top.Id), slot) == ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'bitmap-texture')
    assert top.ChildSlotOn(slot) is False
    on = top.ChildSlotOn(slot)
    assert top.SetChildSlotOn(slot, not on) and top.ChildSlotOn(slot) == (not on)
    assert top.SetChildSlotAmount(slot, 42.0) and top.ChildSlotAmount(slot) == 42.0
    assert top.ChildSlotAmount('rh3dm-no-such-slot') == 0.0       # the old binding read 100.0 (D64)
    n = len(list(top.Children))
    other = next(c for c in tops if c.Id != top.Id)
    assert top.SetChild(other, 'rh3dm-slot')
    assert len(list(top.Children)) == n + 1 and top.FindChild('rh3dm-slot') is not None
    back = r.File3dm.FromByteArray(f.ToByteArray(None))
    btop = next(c for c in list(back.RenderMaterials) + list(back.RenderEnvironments) + list(back.RenderTextures) if c.Id == top.Id)
    assert btop.ChildSlotAmount(slot) == 42.0 and btop.FindChild('rh3dm-slot') is not None


@pytest.mark.xfail(reason='pilot-findings #50: SetChild copies the child WITH its id (ON_RenderContent copies through XML), '
                          'so a child set from a file content duplicates that id; FindChild then resolves the top-level one',
                   strict=True)
def test_render_content_set_child_gives_the_copy_its_own_id_u4():
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
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
    f = r.File3dm()
    objs = [r.ViewportInfo(), f.Settings.RenderSettings, r.Bitmap(),
            r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, r.Interval(0, 1), r.Interval(0, 1), r.Interval(0, 1))]
    for o in objs:
        assert o.UserStringCount == 0 and o.GetUserStrings() == ()
        assert o.SetUserString('k', 'v') and o.GetUserString('k') == 'v'
        assert o.UserStringCount == 1 and o.GetUserStrings() == (('k', 'v'),)
    layer = r.Layer(); layer.SetUserString('a', '1'); layer.SetUserString('b', '2')
    assert sorted(layer.GetUserStrings()) == [('a', '1'), ('b', '2')]
    f.Layers.Add(layer)
    back = r.File3dm.FromByteArray(f.ToByteArray(None))
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
    # C (Luis: drop): GetX(parentStyle) / SetX(parentStyle, v) -> ParentDimensionStyle = style; X (D39).
    # The old getters' values, after each old setter, are frozen (M3)
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    new = r.File3dm.Read(p)
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBase))
    # the annotation's OWN style (D67): assigning a different ParentDimensionStyle changes the
    # annotation's style and drops its overrides (RhinoCommon); the old form evaluated any style
    assert str(nann.DimensionStyleId) == '6d104644-18b4-464c-a49c-dd4892fcbe39'
    nann.ParentDimensionStyle = new.DimStyles.FindId(nann.DimensionStyleId)
    assert nann.TextHeight == 10.0                                    # the override survives
    old = {'TextHeight': 10.0, 'MaskColor': (255, 255, 255, 255), 'MaskColorSource': 0, 'MaskEnabled': False,
           'MaskFrame': 0, 'MaskOffset': 0.8, 'DimensionLengthDisplay': 3, 'AlternateDimensionLengthDisplay': 7}
    for n, v in old.items():
        assert v == _num(getattr(nann, n)), n
    for n, v in (('TextHeight', 7.5), ('MaskOffset', 0.25), ('MaskEnabled', True), ('MaskColor', (10, 20, 30, 255))):
        setattr(nann, n, v)
        assert v == _num(getattr(nann, n)), n                         # the old getter returned v
    # an enum set to another value: the old member's choice (its first other value), read back
    for n, other in (('MaskColorSource', 1), ('MaskFrame', 1), ('DimensionLengthDisplay', 0), ('AlternateDimensionLengthDisplay', 0)):
        setattr(nann, n, type(getattr(nann, n))(other))
        assert _num(getattr(nann, n)) == other, n
    nann.DimensionScale = 2.0
    assert nann.DimensionScale == 2.0



def test_retired_numbered_list_duplicates_e():
    # E (Luis: drop): the Python-only `...2` list variants -> the generated originals (lists, D35).
    # The old variants' results frozen (M3)
    P = r.Point3d
    npc = r.PointCloud()
    for i in range(4):
        npc.Add(P(i, 0, 0), r.Vector3d(0, 0, 1), (10 * i, 0, 0, 255))
    xyz = lambda q: (q.X, q.Y, q.Z)
    assert [xyz(q) for q in npc.GetPoints()] == [(0.0, 0.0, 0.0), (1.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 0.0, 0.0)]   # GetPoints2
    assert [xyz(q) for q in npc.GetNormals()] == [(0.0, 0.0, 1.0)] * 4                                             # GetNormals2
    assert [tuple(c) for c in npc.GetColors()] == [(0, 0, 0, 255), (10, 0, 0, 255), (20, 0, 0, 255), (30, 0, 0, 255)]  # GetColors2
    assert list(npc.GetPointValues()) == []                                                                        # GetValues2
    ncrv = r.NurbsCurve.Create(False, 3, [P(0, 0, 0), P(1, 2, 0), P(3, 1, 0), P(4, 3, 0), P(6, 0, 0)])
    assert list(ncrv.Knots) == [0.0, 0.0, 0.0, 1.0, 2.0, 2.0, 2.0]                                                 # Knots.ToList2
    pi = 3.141592653589793
    assert list(r.NurbsSurface.CreateFromSphere(r.Sphere(P(0, 0, 0), 2)).KnotsU) == \
        [0.0, 0.0, pi / 2, pi / 2, pi, pi, 4.71238898038469, 4.71238898038469, 2 * pi, 2 * pi]
    nbox = r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))
    assert list(nbox.Faces[0].GetSpanVector(0)) == [0.0, 1.0]                                                      # GetSpanVector2
    assert list(r.Transform.Translation(1, 2, 3).ToFloatArray(True)) == \
        [1.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 2.0, 0.0, 0.0, 1.0, 3.0, 0.0, 0.0, 0.0, 1.0]                          # ToFloatArray2
    npl = r.Polyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
    assert [(s.From.X, s.To.Y) for s in npl.GetSegments()] == [(0.0, 0.0), (1.0, 1.0)]                             # GetSegments2
    npc2 = r.PolyCurve()
    npc2.Append(r.Line(P(0, 0, 0), P(1, 0, 0))); npc2.Append(r.Line(P(1, 0, 0), P(1, 1, 0)))
    assert len(npc2.Explode()) == 2                                                                                # Explode2
    p = os.path.join(MODELS, 'object_UserStrings.3dm')
    assert sorted(next(o.Geometry for o in r.File3dm.Read(p).Objects).GetUserStrings()) == []                     # GetUserStrings2
    nf = r.File3dm.Read(os.path.join(MODELS, 'groups.3dm'))
    assert sorted(str(o.Attributes.ObjectId) for o in nf.AllGroups.GroupMembers(0)) == \
        ['439da937-a505-4be1-8c9b-36e40c4ee057', '674d5429-34d2-4029-9835-d86be8eb9fa5']                           # GroupMembers2



def test_retired_subd_counts_and_ids_g1():
    # G1 (Luis: map): SubD.Edge/Face/VertexCount -> Edges/Faces/Vertices.Count; component Index -> Id
    # (the old counts and indices: 48 / 24 / 26, numbered from 1 -- frozen, M3)
    p = os.path.join(MODELS, 'subdBox.3dm')
    n = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
    assert (n.Edges.Count, n.Faces.Count, n.Vertices.Count) == (48, 24, 26)
    assert sorted(f.Id for f in n.Faces) == list(range(1, 25))
    assert sorted(e.Id for e in n.Edges) == list(range(1, 49))
    assert sorted(v.Id for v in n.Vertices) == list(range(1, 27))



def test_retired_tight_bounding_box_l1():
    # L1 (Luis: map): GeometryBase.GetTightBoundingBox -> GetBoundingBox(true) (the old box: (0, 0, 2, 3))
    nbox = r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))
    nb = nbox.GetBoundingBox(True)
    assert (nb.Min.X, nb.Min.Y, nb.Max.Y, nb.Max.Z) == (0, 0, 2, 3)
    # G115: the body's `this as AnnotationBase` -- an annotation answers with its own box
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    f = r.File3dm.Read(p)
    ann = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, r.AnnotationBase))
    ann.ParentDimensionStyle = f.DimStyles.FindId(ann.DimensionStyleId)
    a, b = ann.GetBoundingBox(True), ann.GetBoundingBox(r.Transform.Identity)
    assert (a.Min.X, a.Max.X, a.Max.Y) == (b.Min.X, b.Max.X, b.Max.Y)
    assert ann.TextModelWidth == a.Max.X - a.Min.X


def test_retired_likely_renames_l1_and_other_maps_m1():
    # L1 / M1 (Luis: map): each old member against its RhinoCommon member (the old results frozen, M3)
    P = r.Point3d
    nbox = r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))
    assert nbox.Faces[0].OuterLoop.Trims.Count == 4                                   # OuterLoop.TrimCount
    npl = r.Polyline([P(0, 0, 0), P(4, 0, 0), P(4, 4, 0)])
    nc = npl.ClosestPoint(P(5, 1, 0))
    assert (nc.X, nc.Y) == (4.0, 1.0)                                                  # Polyline.ClosesPoint
    nl = r.Point3dList()
    nl.Add(1.0, 2.0, 3.0)
    assert (nl[0].X, nl[0].Z) == (1.0, 3.0)                                            # Point3dList.Append
    npc = r.PointCloud(); npc.Add(P(0, 0, 0))
    assert npc.ContainsPointValues is False                                            # ContainsValues
    assert list(npc.GetPointValues()) == []                       # the old GetValues raised (pilot #29)
    npc = r.PointCloud(); npc.Add(P(0, 0, 0), 2.5); npc.Add(P(1, 0, 0), -1.0)
    assert npc.ContainsPointValues is True
    assert list(npc.GetPointValues()) == [2.5, -1.0]                                   # GetValues
    srf_new = r.NurbsSurface.CreateFromSphere(r.Sphere(P(0, 0, 0), 2))
    assert (srf_new.KnotsU.ClampedAtStart, srf_new.KnotsU.ClampedAtEnd) == (True, True)   # IsClampedStart / End
    assert srf_new.MakeNonRational() is True                                           # Points.MakeNonRational
    nm = r.Material(); nm.ToPhysicallyBased()
    assert nm.IsPhysicallyBased is True                                                # PhysicallyBased.Supported
    nm.PhysicallyBased.Emission = r.Color4f(1.0, 0.5, 0.25, 1.0)                      # EmissionColor
    e = nm.PhysicallyBased.Emission
    assert (round(e.R, 6), round(e.G, 6), round(e.B, 6)) == (1.0, 0.5, 0.25)
    ne = r.Extrusion.Create(r.Circle(2).ToNurbsCurve(), r.Plane.WorldXY, 3.0, True)   # Extrusion.CreateWithPlane
    assert ne is not None and abs(ne.GetBoundingBox(r.Transform.Identity).Max.Z - 3.0) < 1e-9
    nmesh = r.Mesh()
    assert nmesh.Vertices.Add(0.1, 0.2, 0.3) == 0                                     # Vertices.AddPoint3d
    assert nmesh.Vertices.Point3dAt(0).X == 0.1                # double precision kept, as AddPoint3d did
    p = os.path.join(MODELS, 'sphereDecals.3dm')
    nd = [d for o in r.File3dm.Read(p).Objects for d in o.Attributes.Decals]
    # the old Horz/VertSweepStart/End and BoundsMin/MaxU/V of each decal
    od = [((0.0, 0.0), (0.0, 0.0), (0.6, 0.6, 0.7, 0.7)),
          ((0.0, 0.0), (0.0, 0.0), (0.2, 0.2, 0.300000001490116, 0.300000001490116))]
    assert len(nd) == len(od)
    for (horz, vert, bounds), b in zip(od, nd):
        assert horz == tuple(b.HorzSweep()) and vert == tuple(b.VertSweep()) and bounds == tuple(b.GetUVBounds())
    # DimensionStyle.Field -> the nested enum on the generated class (D18), registered by the
    # generator on ON_DimStyle::field with all of RhinoCommon's values (the old one had 105 of 119,
    # `ArrowSize` for `Arrowsize`)
    F = r.DimensionStyle.Field
    assert len(F.__members__) == 119
    assert (int(F.Arrowsize), int(F.ZeroSuppress), int(F.ClippingArrowSize), int(F.Count)) == (5, 76, 119, 122)
    # the other nested enums the old bindings registered at module level are RhinoCommon's, nested
    # in their owner (M3, D76): generated with RhinoCommon's values, not the old registrations
    assert [m for m in r.Environment.BackgroundProjections.__members__] == \
        ['Planar', 'Spherical', 'Emap', 'Box', 'Automatic', 'LightProbe', 'CubeMap', 'VerticalCrossCubeMap',
         'HorizontalCrossCubeMap', 'Hemispherical']
    assert [m for m in r.RenderChannels.Modes.__members__] == ['Automatic', 'Custom']
    ds = r.DimensionStyle()
    ds.SetFieldOverride(r.DimensionStyle.Field.TextHeight)
    assert ds.IsFieldOverriden(r.DimensionStyle.Field.TextHeight)
    assert not ds.IsFieldOverriden(r.DimensionStyle.Field.Arrowsize)



def test_annotation_font_and_held_back_c_members_g115():
    # G115: AnnotationBase.Font (a managed font adopted from the native's pointer, never owned);
    # with it the last two C members retire: GetFont / SetFont(style, font) -> Font
    # (the old GetFont / SetFont results frozen, M3)
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    new = r.File3dm.Read(p)
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBase))
    nann.ParentDimensionStyle = new.DimStyles.FindId(nann.DimensionStyleId)
    nf = nann.Font
    assert (nf.FamilyName, nf.FaceName, nf.Bold) == ('Avenir', 'Black', True)
    assert nann.FirstCharFont.FamilyName
    nann.Font = r.Font('Courier New')
    assert nann.Font.FamilyName == 'Courier New'
    del nf; gc.collect()                                   # a managed font: dropping it frees nothing
    assert nann.Font.FamilyName
    with pytest.raises(RuntimeError):
        nann.Font = None                                   # C#: NullReferenceException


def test_file3dm_earth_anchor_point_g115():
    # G115: File3dm.EarthAnchorPoint => new EarthAnchorPoint(this) (an internal constructor inlined:
    # the model's anchor COPIED out); the setter copies one in. The old Settings.EarthAnchorPoint agreed
    # (0, 0, 0 on this file: frozen, M3)
    p = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(p)
    a = f.EarthAnchorPoint
    assert (a.EarthBasepointLatitude, a.EarthBasepointLongitude, a.EarthBasepointElevation) == (0.0, 0.0, 0.0)
    a.EarthBasepointLatitude = 12.5
    assert f.EarthAnchorPoint.EarthBasepointLatitude == 0.0                    # a copy
    f.EarthAnchorPoint = a
    assert f.EarthAnchorPoint.EarthBasepointLatitude == 12.5
    back = r.File3dm.FromByteArray(f.ToByteArray(None))
    assert back.EarthAnchorPoint.EarthBasepointLatitude == 12.5


def test_translated_setters_beside_planned_getters_g115():
    # G115: a getter another shape planned keeps RhinoCommon's setter when only the translated body
    # can express it (MeshVertexList.Count: guarded, two calls) -- D1's SetCount -> Count
    m = r.Mesh()
    for i in range(4):
        m.Vertices.Add(float(i), 0.0, 0.0)
    m.Vertices.Count = 2                                   # the old SetCount(2): 2 vertices
    assert m.Vertices.Count == 2
    m.Vertices.Count = -1                                  # C#: ignored
    assert m.Vertices.Count == 2
    v = r.ViewportInfo()
    v.IsPerspectiveProjection = True
    assert v.IsPerspectiveProjection and not v.IsParallelProjection
    v.IsPerspectiveProjection = False
    assert v.IsParallelProjection
    import uuid
    at, vid = r.ObjectAttributes(), uuid.uuid4()
    at.ViewportId = vid
    assert at.ViewportId == vid
    mp = r.MeshingParameters(); mp.CopyFrom(r.MeshingParameters.QualityRenderMesh)
    assert mp.GridAngle == r.MeshingParameters.QualityRenderMesh.GridAngle


def test_retired_file3dm_tables_and_settings_h1():
    # H1 (Luis: map): the old File3dm table/settings helpers -> RhinoCommon's members (the old
    # results on this file frozen, M3)
    import uuid
    p = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(p)
    # RenderContent table -> RenderMaterials / RenderEnvironments / RenderTextures; FindId -> Find;
    # the old table in index order:
    old_rc = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd',
              '16421078-5d0e-6851-2d34-062d266c3949']
    kinds = (f.RenderMaterials, f.RenderEnvironments, f.RenderTextures)
    assert sorted(old_rc) == sorted(str(c.Id) for k in kinds for c in k)
    for oid in old_rc:
        hit = [k.Find(uuid.UUID(oid)) for k in kinds if k.Find(uuid.UUID(oid)) is not None]
        assert len(hit) == 1 and str(hit[0].Id) == oid
    # FindIndex(i) -> Manifest.FindIndex(i, RenderContent): the same ComponentFromIndex, the right kind
    for i, oid in enumerate(old_rc):
        nc = f.Manifest.FindIndex(i, r.ModelComponentType.RenderContent)
        assert isinstance(nc, r.File3dmRenderContent) and str(nc.Id) == oid
    # linetype Current / Scale -> Settings.CurrentLinePatternId / LinetypeDisplayScale (old: None, 1.0)
    assert str(f.Settings.CurrentLinePatternId) == str(uuid.UUID(int=0))
    assert f.Settings.LinetypeDisplayScale == 1.0
    # Settings.ModelBasePoint / EarthAnchorPoint -> Settings.ModelBasepoint / File3dm.EarthAnchorPoint (G115)
    nb = f.Settings.ModelBasepoint
    assert (nb.X, nb.Y, nb.Z) == (0.0, 0.0, 0.0)
    assert f.EarthAnchorPoint.EarthBasepointLongitude == 0.0
    # RenderSettings.RenderEnvironments.<Usage>Id / Override -> RenderEnvironmentId(usage, Standard),
    # SetRenderEnvironmentId (G116: commits into the file), RenderEnvironmentOverride(usage)
    U, Pp = r.RenderSettings.EnvironmentUsage, r.RenderSettings.EnvironmentPurpose
    rs = f.Settings.RenderSettings
    for u in (U.Background, U.Reflection, U.Skylighting):
        assert str(rs.RenderEnvironmentId(u, Pp.Standard)) == '16421078-5d0e-6851-2d34-062d266c3949', u
    for u in (U.Reflection, U.Skylighting):
        assert rs.RenderEnvironmentOverride(u) is True, u
    eid = uuid.uuid4()
    rs.SetRenderEnvironmentId(U.Skylighting, eid); rs.SetRenderEnvironmentOverride(U.Skylighting, True)
    back = r.File3dm.FromByteArray(f.ToByteArray())                 # the no-argument form (G116)
    brs = back.Settings.RenderSettings
    assert str(brs.RenderEnvironmentId(U.Skylighting, Pp.Standard)) == str(eid)
    assert brs.RenderEnvironmentOverride(U.Skylighting) is True
    # ObjectTable.AddObject(obj) -> Objects.Add(File3dmObject); and Add(geometry, attributes) (G117),
    # attributes optional as in C# (None)
    no = list(f.Objects)[0]
    m0 = len(list(f.Objects))
    f.Objects.Add(no)
    assert len(list(f.Objects)) == m0 + 1
    nid = f.Objects.Add(no.Geometry, None)
    assert type(f.Objects.FindId(nid).Geometry).__name__ == 'Extrusion'        # the old one: Extrusion
    # RhinoCommon's Add throws NotSupportedException for a leader, text, radial or ordinate dimension;
    # rhino3dm keeps 8.x's hand path behind it (G119, D71; upstream U30)
    S = r.DimensionStyle()
    P = r.Point3d
    extra = [r.Leader.Create('x', r.Plane.WorldXY, S, [P(0, 0, 0), P(1, 0, 0)]),
             r.TextEntity.Create('t', r.Plane.WorldXY, S, False, 0.0, 0.0),
             r.RadialDimension.Create(S, r.AnnotationType.Radius, r.Plane.WorldXY, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)),
             r.OrdinateDimension.Create(S, r.Plane.WorldXY, r.OrdinateDimension.MeasuredDirection.Xaxis,
                                         P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)]
    ids = [f.Objects.Add(g, None) for g in extra]
    back = r.File3dm.FromByteArray(f.ToByteArray())
    kinds = {str(o.Attributes.ObjectId): type(o.Geometry).__name__ for o in back.Objects}
    assert [kinds.get(str(i)) for i in ids] == ['Leader', 'TextEntity', 'RadialDimension', 'OrdinateDimension']


def test_retired_mesh_modifiers_i1_and_shut_lining_i3():
    # I1: ObjectAttributes.MeshModifiers -> File3dmMeshModifiers; I3: the ShutLiningCurve table and
    # its curves -> File3dmShutLining's id-based members (GetCurves, Curve*(id), SetCurve*(id, v)).
    # Checked against the old table before the switch-over (M3); here on the generated side alone
    import uuid
    na = r.ObjectAttributes()
    nsl = na.File3dmMeshModifiers.CreateShutLining()
    nsl.On = True
    cid = uuid.uuid4()
    assert nsl.AddCurve(cid) == cid
    nsl = na.File3dmMeshModifiers.ShutLining
    assert nsl.On is True
    assert list(nsl.GetCurves()) == [cid]
    nsl.SetCurveRadius(cid, 0.75); nsl.SetCurveProfile(cid, 2); nsl.SetCurveEnabled(cid, True)
    nsl.SetCurvePull(cid, True); nsl.SetCurveIsBump(cid, True)
    assert (nsl.CurveRadius(cid), nsl.CurveProfile(cid), nsl.CurveEnabled(cid), nsl.CurvePull(cid), nsl.CurveIsBump(cid)) == \
           (0.75, 2, True, True, True)
    nsl.SetCurveRadius(cid, 1.25); nsl.SetCurveProfile(cid, 1); nsl.SetCurveEnabled(cid, False)
    nsl.SetCurvePull(cid, False); nsl.SetCurveIsBump(cid, False)
    assert (nsl.CurveRadius(cid), nsl.CurveProfile(cid), nsl.CurveEnabled(cid), nsl.CurvePull(cid), nsl.CurveIsBump(cid)) == \
           (1.25, 1, False, False, False)



def _annotation_file():
    """a file with one of each dimension and a leader, made by the generated API"""
    P, S = r.Point3d, r.DimensionStyle()
    f = r.File3dm()
    f.AllDimStyles.Add(S)
    style = list(f.AllDimStyles)[0]
    ld = r.LinearDimension.Create(r.AnnotationType.Aligned, style, r.Plane.WorldXY, r.Vector3d(1, 0, 0),
                                   P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    ad = r.AngularDimension.Create(style.Id, r.Plane.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
    rd = r.RadialDimension.Create(style, r.AnnotationType.Radius, r.Plane.WorldXY, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0))
    od = r.OrdinateDimension.Create(style, r.Plane.WorldXY, r.OrdinateDimension.MeasuredDirection.Xaxis,
                                     P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
    le = r.Leader.Create('note', r.Plane.WorldXY, style, [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])
    for g in (ld, ad, rd, od, le):
        assert g is not None
    f.Objects.Add(ld, None)                    # RhinoCommon's Add(geometry, attributes) takes these two (G117)
    f.Objects.Add(ad, None)
    f.Objects.AddRadialDimension(rd)           # ... and throws NotSupportedException for the rest
    f.Objects.AddOrdinateDimension(od)
    f.Objects.AddLeader('note', r.Plane.WorldXY, [r.Point2d(0, 0), r.Point2d(2, 1), r.Point2d(4, 1)], None)
    assert le.Points3D[2].X == 4.0             # Create's own points too
    return f.ToByteArray()


def test_retired_annotation_geometry_j1():
    # J1 (Luis: map): Leader.Points -> Points3D (and Points2D); Dim*.Points (a dict) -> Get3dPoints()
    # (a tuple led by the success flag: D68); DimAngular.Angle -> NumericValue. The old members' results
    # on the same file frozen (M3)
    data = _annotation_file()
    news = [o.Geometry for o in r.File3dm.FromByteArray(data).Objects]
    xyz = lambda q: (round(q.X, 9), round(q.Y, 9), round(q.Z, 9))
    pairs = {type(n).__name__: n for n in news}
    assert set(pairs) == {'LinearDimension', 'AngularDimension', 'RadialDimension', 'OrdinateDimension', 'Leader'}
    n = pairs['Leader']
    assert [xyz(q) for q in n.Points3D] == [(0, 0, 0), (2, 1, 0), (4, 1, 0)]
    assert [(round(q.X, 9), round(q.Y, 9)) for q in n.Points2D] == [(0, 0), (2, 1), (4, 1)]
    old = {'LinearDimension': [(0.0, 0.0, 0.0), (4.0, 1.0, 0.0), (0.0, 3.0, 0.0), (4.0, 3.0, 0.0), (2.0, 3.0, 0.0), (2.0, 3.0, 0.0)],
           'AngularDimension': [(0.0, 4.0, 0.0), (1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.766964989, 0.932140045, 0.0),
                                 (0.0, 0.83772234, 0.0), (2.445086463, 6.005380809, 0.0), (-0.386377405, 7.138584474, 0.0)],
           'RadialDimension': [(0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 1.0, 0.0), (2.0, 1.0, 0.0)]}
    for name, pts in old.items():
        rc = pairs[name].Get3dPoints()
        assert rc[0] is True and pts == [xyz(q) for q in rc[1:]], name
    rc = pairs['OrdinateDimension'].Get3dPoints()
    assert rc[0] is True and sorted(xyz(q) for q in rc[1:]) == \
        [(0.0, 0.0, 0.0), (5.0, 3.0, 0.0), (5.0, 6.0, 0.0), (5.0, 7.0, 0.0), (5.0, 8.0, 0.0)]
    assert abs(6.038206644052722 - pairs['AngularDimension'].NumericValue) < 1e-12



def test_retired_set_point3d_at_d2():
    # D2 (Luis: rename to SetVertex): MeshVertexList.SetPoint3dAt(i, p) -> SetVertex(i, Point3d)
    nm = r.Mesh()
    for i in range(3):
        nm.Vertices.Add(float(i), 0.0, 0.0)
    assert nm.Vertices.SetVertex(1, r.Point3d(0.1, 0.2, 0.3))
    n = nm.Vertices.Point3dAt(1)
    assert (n.X, n.Y, n.Z) == (0.1, 0.2, 0.3)                 # as the old SetPoint3dAt stored it



def test_embedded_file_paths_vs_embedded_files_h1():
    # H1 / E (Luis: map): File3dm.EmbeddedFilePaths (and ...2) -> EmbeddedFiles: the old ones read the
    # RDK's embedded-file paths, the new ones the file's ON_EmbeddedFile components -- the same files, in
    # the same order (the old paths' file names, frozen: M3). A path saved on Windows reads in this
    # platform's form (ON_EmbeddedFile cleans it, as RhinoCommon on Mac does; D69): compare the file names
    old = {'file3dm_stuff.3dm': ['hops.png'], 'sphereDecals.3dm': ['E6AD.jpg', 'E6EC.jpg'],
           'subd.3dm': ['bump_texture.png']}
    base = lambda s: s.replace('\\', '/').split('/')[-1]
    for name, files in old.items():
        f = r.File3dm.Read(os.path.join(MODELS, name))
        assert files == [base(e.Filename) for e in f.EmbeddedFiles], name



def test_retired_rhino3dm_only_overloads_from_the_audit():
    # the overload audit (2026-10-29): old overloads counted covered by name; Luis accepted these
    # maps where the functionality is the same (WrapText(width) is not: upstream U31). The old
    # overloads' results frozen (M3)
    PX = r.Point3d
    # AnnotationBase.RunReplace(dimstyle, str, ...) -> ParentDimensionStyle = dimstyle; RunReplace(str, ...)
    p = os.path.join(MODELS, 'textEntities_r8.3dm')
    new = r.File3dm.Read(p)
    nann = next(o.Geometry for o in new.Objects if isinstance(o.Geometry, r.AnnotationBase))
    nann.ParentDimensionStyle = new.DimStyles.FindId(nann.DimensionStyleId)
    assert nann.RunReplace('ZZ', 0, 0, 0, 1) is True
    assert nann.PlainText == 'ZZllo World!'
    # GeometryBase.GetBoundingBox() -> GetBoundingBox(False) (ON_Geometry::BoundingBox)
    nbx = r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))
    n = nbx.GetBoundingBox(False)
    assert (n.Min.X, n.Min.Y, n.Max.Y, n.Max.Z) == (0, 0, 2, 3)
    # File3dmObjectTable.AddPoint(x, y, z, attributes) -> AddPoint(Point3d, attributes)
    nf = r.File3dm()
    na = r.ObjectAttributes()
    na.Name = 'pt'
    ng = nf.Objects.FindId(nf.Objects.AddPoint(PX(1.0, 2.0, 3.0), na))
    assert (ng.Geometry.Location.Z, ng.Attributes.Name) == (3.0, 'pt')
    # Material.SetTexture(texture) -> SetTexture(texture, texture.TextureType) (the old one returned
    # AddTexture's index as a bool: False for index 0)
    nm, ntex = r.Material(), r.Texture()
    ntex.FileName = '/tmp/bump.png'
    ntex.TextureType = r.TextureType.Bump
    assert nm.SetTexture(ntex, ntex.TextureType) is True
    assert nm.GetTexture(r.TextureType.Bump).FileName == '/tmp/bump.png'
    # SubDEdge.EndSharpness(i) -> EndSharpness(i, False) (opennurbs' own one-argument form)
    p = os.path.join(MODELS, 'subd_creases.3dm')
    ns_ = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
    ne = sorted(ns_.Edges, key=lambda e: e.Id)
    assert [(e.EndSharpness(0, False), e.EndSharpness(1, False)) for e in ne] == \
        [(0.0, 0.0)] * 4 + [(1.0, 1.0), (0.0, 0.0), (3.0, 3.0), (2.5, 0.5)] + [(0.0, 0.0)] * 4
    # CachedTextureCoordinates.IndexOf(x, y, z) / Contains(x, y, z) -> IndexOf(Point3d) / Contains(Point3d)
    path = os.path.join(MODELS, 'meshWithTexture.3dm')
    f = r.File3dm.Read(path)
    o = next(o for o in f.Objects if isinstance(o.Geometry, r.Mesh))
    mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
    m = o.Geometry                                # one reference: each .Geometry is a new view (#22)
    m.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat)
    tc = m.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
    q = tc[3]
    assert tc.IndexOf(q) == 3 and tc.Contains(q) is True        # the old IndexOf(x, y, z): 3, Contains: True
    assert tc.Contains(PX(9e9, 0, 0)) is False



def test_mesh_set_cached_texture_coordinates_takes_the_transform_g120():
    # G120: SetCachedTextureCoordinates(tm, ref Transform xf) took only tm -- a zero transform reached
    # the native; now the caller's transform goes in (and comes back, as C#'s ref)
    mesh = r.Mesh()
    for x, y in ((0, 0), (1, 0), (1, 1), (0, 1)):
        mesh.Vertices.Add(float(x), float(y), 0.0)
    mesh.Faces.AddFace(0, 1, 2, 3)
    tm = r.TextureMapping.CreatePlaneMapping(r.Plane.WorldXY, r.Interval(0, 1), r.Interval(0, 1), r.Interval(0, 1))
    xf = r.Transform.Translation(r.Vector3d(0.5, 0, 0))
    (back,) = mesh.SetCachedTextureCoordinates(tm, xf)
    assert r.Transform.Equals(back, xf)
    xs = [c.X for c in mesh.GetCachedTextureCoordinates(tm.Id)]
    assert len(xs) > 0


def test_retired_mesh_modifier_creators_i2_and_add_curve_rh99854():
    # RH-99854 (rhino 9.x 3255f409fa6): File3dmMeshModifiers.Create*() and File3dmShutLining.AddCurve(Guid)
    # -- the old creators (I2) and ShutLiningCurveTable.Add(id) (I3) retire. Round-tripping through a
    # file needs U29 in rhino3dm's opennurbs (pilot #43): in memory here
    # (the old creators and Curves table agreed, in the same order: checked before the switch-over, M3)
    import uuid
    na = r.ObjectAttributes()
    nmm = na.File3dmMeshModifiers
    assert nmm.ShutLining is None                               # none until created
    for name in ('Displacement', 'EdgeSoftening', 'Thickening', 'CurvePiping', 'ShutLining'):
        n = getattr(nmm, 'Create' + name)()
        assert n is not None and getattr(nmm, name) is not None, name
        n.On = True
        assert getattr(nmm, 'Create' + name)().On is True, name     # an existing one is kept
    cid, cid2 = uuid.uuid4(), uuid.uuid4()
    nsl = nmm.ShutLining
    assert nsl.AddCurve(cid) == cid and nsl.AddCurve(cid2) == cid2
    assert nsl.AddCurve(cid) == cid                             # no second curve for an id
    assert list(nsl.GetCurves()) == [cid, cid2]
    nsl.SetCurveRadius(cid, 2.0)
    assert (nsl.CurveRadius(cid), nsl.CurveRadius(cid2)) == (2.0, 1.0)   # each its own (pilot #51)


def test_retired_create_from_subd_control_net_flag():
    # Luis 2026-10-29: Mesh.CreateFromSubDControlNet(subd, includeTextureCoordinates) -> RhinoCommon's two
    # methods, one per value of the flag (both ON_SubD_GetControlNetMesh(subd, flag)); the old member's
    # (vertices, faces, texture coordinates) for flag False, True -- frozen (M3)
    old = {'subd.3dm': [(738, 279, 0), (2485, 868, 0)], 'subdBox.3dm': [(47, 24, 0), (96, 24, 96)],
           'subd_creases.3dm': [(15, 6, 0), (24, 6, 24)]}
    for name, counts in old.items():
        p = os.path.join(MODELS, name)
        ns_ = next(x.Geometry for x in r.File3dm.Read(p).Objects if isinstance(x.Geometry, r.SubD))
        for want, new in zip(counts, (r.Mesh.CreateFromSubDControlNet, r.Mesh.CreateFromSubDControlNetWithTextureCoordinates)):
            n = new(ns_)
            assert want == (n.Vertices.Count, n.Faces.Count, n.TextureCoordinates.Count), (name, new)
    # the flag matters: subdBox's control net is 47 vertices, 96 (with as many texture coordinates) with it
    box = next(x.Geometry for x in r.File3dm.Read(os.path.join(MODELS, 'subdBox.3dm')).Objects if isinstance(x.Geometry, r.SubD))
    assert r.Mesh.CreateFromSubDControlNetWithTextureCoordinates(box).TextureCoordinates.Count == 96
    assert r.Mesh.CreateFromSubDControlNet(box).TextureCoordinates.Count == 0


def test_material_render_material_instance_id_u1():
    # U1 (upstream, 2026-10-20): RhinoCommon's Material.RenderMaterialInstanceId over portable natives;
    # setting it also sets the universal render engine as the plug-in id (ON_Material's contract)
    import uuid
    m = r.Material()
    assert m.RenderMaterialInstanceId == uuid.UUID(int=0)
    uid = uuid.uuid4()
    m.RenderMaterialInstanceId = uid
    assert m.RenderMaterialInstanceId == uid
    assert str(m.RenderPlugInId) == '99999999-9999-9999-9999-999999999999'   # the old binding's contract too


# --- RH-99868 (rhino 9.x da1b2185e6c): 26 SubD component queries, old binding as the oracle ---

def _p3(p):
    # NaN-aware (an unset texture point is NaN), so tuples compare equal when both are unset
    return tuple('nan' if v != v else v for v in (p.X, p.Y, p.Z))


def test_subd_component_queries_rh99868():
    # The old binding called the same opennurbs queries directly; the generated members reach them
    # through RhinoCommon's new C functions. Compared component by component (same order, same ids)
    # with the old binding's answers, frozen in old_oracle/subd_queries_rh99868.json (M3)
    import json
    oracle = json.load(open(os.path.join(os.path.dirname(__file__), 'old_oracle', 'subd_queries_rh99868.json')))['fixtures']
    p3 = lambda p: list(_p3(p))
    checked = 0
    for name, olds in oracle.items():
        path = os.path.join(MODELS, name)
        news = [o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD)]
        assert news and len(news) == len(olds)
        for s, old in zip(news, olds):
            nf = list(s.Faces)
            assert [f.Id for f in nf] == [f['id'] for f in old['faces']]
            for n, o in zip(nf, old['faces']):
                assert [n.HasEdges, n.HasSharpEdges, n.SharpEdgeCount, n.MaximumEdgeSharpness, n.IsConvex,
                        n.IsPlanar(1e-8), n.IsPlanar(1.0), n.MaterialChannelIndex, n.TexturePointsAreSet,
                        n.TexturePointsCapacity] == o['q']
                assert p3(n.SubdivisionPoint) == o['sub'] and p3(n.TextureCenterPoint) == o['tc']
                assert [p3(n.TexturePoint(i)) for i in range(n.TexturePointsCapacity)] == o['tp']
            nv = list(s.Vertices)
            assert [v.Id for v in nv] == [v['id'] for v in old['vertices']]
            for n, o in zip(nv, old['vertices']):
                assert [n.IsSmooth, n.IsCrease, n.IsCorner, n.IsDart, n.IsSharp(True), n.IsSharp(False), n.VertexSharpness] == o['q']
            ne = list(s.Edges)
            assert [e.Id for e in ne] == [e['id'] for e in old['edges']]
            for n, o in zip(ne, old['edges']):
                assert [n.IsSmooth, n.IsCrease, n.IsHardCrease, n.IsDartCrease, n.DartCount] == o['q']
                assert p3(n.SubdivisionPoint) == o['sub'] and p3(n.ControlNetCenterPoint) == o['cc']
                assert [p3(n.ControlNetCenterNormal(i)) for i in range(n.FaceCount)] == o['cn']
            checked += len(nf) + len(nv) + len(ne)
    assert checked > 100
    # the crease fixture has creases and corners, so the predicates are not all False
    path = os.path.join(MODELS, 'subd_creases.3dm')
    s = next(o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD))
    assert any(e.IsCrease for e in s.Edges) and any(e.IsSmooth for e in s.Edges)



# --- RH-99870 (rhino 9.x da1b2185e6c): small additions (upstream-fixes U32) ---

def test_linetype_builtins_and_clear_pattern_rh99870():
    # the old binding's built-ins (Name, Index, Id, SegmentCount, PatternLength), frozen (M3)
    old = {'Continuous': ('Continuous', -1, '3999bed5-78ee-4d73-a059-032224c6fd55', 1, 1.0),
           'ByLayer': ('By Layer', -2, '913882da-bce9-4a67-8d86-d449fd5850b8', 1, 1.0),
           'ByParent': ('By Parent', -3, 'ef59d771-5099-4f60-9914-c1836aebe484', 1, 1.0),
           'Hidden': ('Hidden', -4, '7a55ac0f-803c-431b-a7fe-a43319436c8a', 2, 4.0),
           'Dashed': ('Dashed', -5, '864526fb-1eec-40b9-85e1-4619c1f670c5', 2, 10.0),
           'DashDot': ('DashDot', -6, '249ad50d-96c7-44a7-ab20-136eae74c34d', 4, 6.0),
           'Center': ('Center', -7, 'b59b1b65-f88f-4854-bed4-e52c51970d0c', 4, 4.0),
           'Border': ('Border', -8, '93721775-52ea-4193-83a5-3a541a335630', 6, 12.0),
           'Dots': ('Dots', -9, 'e6a283a4-ebbe-4c25-a20c-b1135a791908', 2, 1.0)}
    for i, (name, o) in enumerate(old.items()):
        n = getattr(r.Linetype, name)
        assert isinstance(n, r.Linetype) and n.Index == -(i + 1)
        assert (n.Name, n.Index, str(n.Id), n.SegmentCount, n.PatternLength) == o
    assert r.Linetype.Border is not r.Linetype.Border            # a copy per access
    b = r.Linetype.Border
    b.Name = 'mine'
    assert r.Linetype.Border.Name == 'Border'
    # ClearPattern: true when no pattern is set afterwards; a built-in's pattern is locked (unchanged)
    lt = r.Linetype()
    lt.AppendSegment(1.0, True)
    lt.AppendSegment(0.5, False)
    assert lt.ClearPattern() is True and lt.SegmentCount == 0
    border = r.Linetype.Border
    assert border.ClearPattern() is True and border.SegmentCount == 6


def test_material_compare_appearance_rh99870():
    a, b = r.Material(), r.Material()
    a.Name, b.Name = 'one', 'two'
    assert r.Material.CompareAppearance(a, b) == 0            # names do not affect the appearance
    b.DiffuseColor = r.ColorX.FromArgb(255, 255, 0, 0) if hasattr(r, 'ColorX') else (255, 0, 0, 255)
    c = r.Material.CompareAppearance(a, b)
    assert c != 0 and r.Material.CompareAppearance(b, a) == -c
    assert c > 0                                               # as the old CompareAppearance (8289663)


def test_instance_definitions_find_index_and_geometry_ids_rh99870():
    f = r.File3dm()
    circle = r.ArcCurve(r.Circle(r.Plane.WorldXY, 1.0))
    idx = f.AllInstanceDefinitions.Add('blk', 'desc', r.Point3d(0, 0, 0), [circle], [r.ObjectAttributes()])
    idef = f.AllInstanceDefinitions.FindIndex(idx)
    assert isinstance(idef, r.InstanceDefinitionGeometry) and idef.Name == 'blk'
    assert f.AllInstanceDefinitions.FindIndex(idx + 1) is None
    ids = idef.GetObjectIds()
    assert len(ids) == 1 and idef.IsInstanceGeometryId(ids[0])
    # U33 (opennurbs, in this pin): nil and unknown ids are not geometry ids (pilot #54)
    assert not idef.IsInstanceGeometryId(uuid.UUID(int=0)) and not idef.IsInstanceGeometryId(uuid.uuid4())
    assert not r.InstanceDefinitionGeometry().IsInstanceGeometryId(uuid.uuid4())   # an empty definition


def test_embedded_files_count_length_clear_rh99870(tmp_path):
    data = tmp_path / 'note.txt'
    data.write_text('hello world')
    f = r.File3dm()
    assert f.EmbeddedFiles.Count == 0
    assert f.EmbeddedFiles.Add(str(data)) is True
    assert f.EmbeddedFiles.Count == 1
    ef = list(f.EmbeddedFiles)[0]
    assert ef.Length == len('hello world')
    # FindIndex (U35, pin 5b68aa2623e: it threw NotSupportedException in .NET before, pilot #56)
    found = f.EmbeddedFiles.FindIndex(0)
    assert isinstance(found, r.File3dmEmbeddedFile) and found.Filename == ef.Filename
    assert f.EmbeddedFiles.FindIndex(5) is None
    assert ef.Clear() is True and ef.Length == 0
    path = str(tmp_path / 'embedded.3dm')
    g = r.File3dm()
    g.EmbeddedFiles.Add(str(data))
    assert g.Write(path, 7)
    h = r.File3dm.Read(path)
    assert h.EmbeddedFiles.Count == 1 and list(h.EmbeddedFiles)[0].Length == len('hello world')   # the old binding read it too


def test_settings_current_linetype_source_rh99870(tmp_path):
    S = r.ObjectLinetypeSource
    f = r.File3dm()
    assert f.Settings.CurrentLinetypeSource == S.LinetypeFromLayer      # opennurbs' default
    f.Settings.CurrentLinetypeSource = S.LinetypeFromObject
    assert f.Settings.CurrentLinetypeSource == S.LinetypeFromObject
    path = str(tmp_path / 'source.3dm')
    assert f.Write(path, 8)
    assert r.File3dm.Read(path).Settings.CurrentLinetypeSource == S.LinetypeFromObject   # the old Linetypes.CurrentSource read it


def test_render_content_set_xml_rh99870():
    f = r.File3dm.Read(os.path.join(MODELS, 'file3dm_stuff.3dm'))
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
    P = r.Point3d
    f = r.File3dm()
    f.AllDimStyles.Add(r.DimensionStyle())
    style = list(f.AllDimStyles)[0]
    ad = r.AngularDimension.Create(style.Id, r.Plane.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
    d = ad.DimlinePoint
    assert abs(ad.Radius - (d.X * d.X + d.Y * d.Y) ** 0.5) < 1e-12     # |dimension line point| in the plane
    le = r.Leader.Create('note', r.Plane.WorldXY, style, [P(0, 0, 0), P(10, 5, 0)])
    tp = le.GetTextPoint2d(style, 1.0)
    assert tp.IsValid and (tp.X, tp.Y) != (0.0, 0.0)
    # WrapText(width) wraps only text already marked wrapped (ON_TextContent::WrapText), as the old binding
    t = r.TextEntity.Create('one two three four five six seven', r.Plane.WorldXY, style, False, 0.0, 0.0)
    t.TextIsWrapped = True
    t.WrapText(3.0)
    assert t.TextIsWrapped


def test_arrowhead_and_viewport_and_render_settings_rh99870():
    vp = r.ViewportInfo()
    assert vp.IsValidCameraFrame is True                       # as the old ViewportInfo's
    lw = r.File3dm().Settings.RenderSettings.LinearWorkflow
    on = lw.PreProcessGammaOn
    lw.PreProcessGammaOn = not on
    assert lw.PreProcessGammaOn is (not on)
    sun = r.File3dm().Settings.RenderSettings.Sun
    assert (r.Sun.MinYear, r.Sun.MaxYear) == (1971, 2199)    # the old Sun's too
    assert sun.IsValid is True
    for prop in ('EnableAllowed', 'ManualControlAllowed'):
        v = getattr(sun, prop)
        setattr(sun, prop, not v)
        assert getattr(sun, prop) is (not v)
    T = r.TextureMappingType
    assert (int(T.WcsProjection), int(T.WcsBoxProjection)) == (11, 12)


# --- G122: the RH-99870 members that needed generator work ---

def test_linetype_and_material_resolvers_g122():
    f = r.File3dm()
    # a default object: its layer is missing, so opennurbs resolves to Continuous / the default material.
    # The old binding returns None here (pilot #53: opennurbs' LinePatternFrom* fall back to an empty reference)
    a = r.ObjectAttributes()
    lt = f.AllLinetypes.FromAttributes(a)
    assert isinstance(lt, r.Linetype) and (lt.Name, lt.Index) == ('Continuous', -1)
    assert f.AllLinetypes.FromLayerIndex(0).Name == 'Continuous'
    m = f.AllMaterials.FindFromAttributes(a)
    assert isinstance(m, r.Material) and m.Index == -1
    assert str(m.Id).lower() == 'cf2be1de-b81c-4d69-9418-dde91d266086'      # ON_Material::Default
    # a model linetype through the object, then through its layer
    mine = r.Linetype()
    mine.Name = 'mine'
    mine.AppendSegment(1.0, True)
    mine.AppendSegment(0.5, False)
    f.AllLinetypes.Add(mine)
    li = next(x.Index for x in f.AllLinetypes if x.Name == 'mine')
    a.LinetypeSource = r.ObjectLinetypeSource.LinetypeFromObject
    a.LinetypeIndex = li
    got = f.AllLinetypes.FromAttributes(a)
    assert (got.Name, got.Index) == ('mine', li)
    layer = r.Layer()
    layer.Name = 'L'
    layer.LinetypeIndex = li
    f.AllLayers.Add(layer)
    lyr = next(x.Index for x in f.AllLayers if x.Name == 'L')
    assert f.AllLinetypes.FromLayerIndex(lyr).Name == 'mine'
    # a model material through the object
    mat = r.Material()
    mat.Name = 'red'
    mi = f.AllMaterials.AddMaterial(mat)
    a.MaterialSource = r.ObjectMaterialSource.MaterialFromObject
    a.MaterialIndex = mi
    assert f.AllMaterials.FindFromAttributes(a).Name == 'red'


def test_viewport_defaults_and_arrowhead_points_g122():
    # the old ViewportInfo.DefaultTop() / DefaultPerspective() camera (direction, location), frozen (M3)
    for new, (d, loc) in ((r.ViewportInfo.DefaultTop, ((-0.0, -0.0, -1.0), (0.0, 0.0, 100.0))),
                          (r.ViewportInfo.DefaultPerspective, ((-0.4330127018922193, 0.75, -0.5), (43.30127018922193, -75.0, 50.0)))):
        assert _p3(new.CameraDirection) == d
        assert _p3(new.CameraLocation) == loc
        assert new.IsParallelProjection           # opennurbs' DefaultPerspectiveViewZUp too (pilot #55)
    top = r.ViewportInfo.DefaultTop.CameraDirection
    assert (top.X, top.Y, top.Z) == (0.0, 0.0, -1.0)
    assert r.ViewportInfo.DefaultTop is not r.ViewportInfo.DefaultTop      # a new viewport per access
    T = r.DimensionStyle.ArrowType
    pts = r.Arrowhead.GetPoints(T.SolidTriangle)
    assert [(p.X, p.Y) for p in pts] == [(0.0, 0.0), (-1.0, 0.25), (-1.0, -0.25)]   # the old GetPoints(.., 1.0) too
    assert list(r.Arrowhead.GetPoints(T.UserBlock)) == []
    # DuplicateLinetype (now generated through the owned-pointer adopt): the pattern, with the name,
    # id and locked bits cleared (RhinoCommon's contract) -- so a built-in's copy can be cleared
    d = r.Linetype.Dashed
    lt = d.DuplicateLinetype()
    assert isinstance(lt, r.Linetype) and lt.Name == '' and str(lt.Id) == str(uuid.UUID(int=0))
    assert (lt.SegmentCount, lt.PatternLength) == (d.SegmentCount, d.PatternLength) and lt.SegmentCount > 0
    assert lt.ClearPattern() and lt.SegmentCount == 0


def test_retired_subd_control_net_g3_and_table_moves():
    # triage G3 (Luis 2026-10-28: map), against the old binding on every SubD fixture (its answers
    # frozen in old_oracle/subd_controlnet_g3.json, M3)
    import json
    oracle = json.load(open(os.path.join(os.path.dirname(__file__), 'old_oracle', 'subd_controlnet_g3.json')))['fixtures']
    p3 = lambda p: list(_p3(p))
    checked = 0
    for name, old in oracle.items():
        path = os.path.join(MODELS, name)
        s = next(o.Geometry for o in r.File3dm.Read(path).Objects if isinstance(o.Geometry, r.SubD))
        assert s.Edges.Count == len(old['edges']) and s.Faces.Count == len(old['faces'])
        for n, o in zip(list(s.Edges), old['edges']):
            a, b = n.VertexFrom, n.VertexTo
            assert [p3(a.ControlNetPoint), p3(b.ControlNetPoint)] == o['p']
            line = n.ControlNetLine
            assert [p3(line.From), p3(line.To)] == o['p']
            assert p3(line.Direction) == o['dir']
            assert [a.Id, b.Id] == o['v']
            checked += 1
        for n, o in zip(list(s.Faces), old['faces']):
            assert [p3(n.VertexAt(i).ControlNetPoint) for i in range(n.VertexCount)] == o
            checked += 1
    assert checked > 100
    # AddInstanceDefinition(idef) -> the table's Add(item) (both ONX_Model::AddModelComponent)
    f = r.File3dm()
    idef = r.InstanceDefinitionGeometry()
    idef.Name = 'blk'
    n0 = len(f.AllInstanceDefinitions)
    f.AllInstanceDefinitions.Add(idef)
    assert len(f.AllInstanceDefinitions) == n0 + 1 and f.AllInstanceDefinitions.FindIndex(n0).Name == 'blk'


def test_retired_decal_table_find_index_follows_rhinocommon():
    # Luis 2026-10-29: follow RhinoCommon -- index / count by iterating Attributes.Decals (visible decals;
    # the old table counted hidden ones too: the fixture has none hidden, so both agree here)
    path = os.path.join(MODELS, 'sphereDecals.3dm')
    new = list(next(iter(r.File3dm.Read(path).Objects)).Attributes.Decals)
    assert len(new) == 2
    assert all(d.IsVisible for d in new)
    # the old table's FindIndex(i) and [i], frozen (M3)
    assert [str(d.TextureInstanceId) for d in new] == ['84eda676-69e1-4fa5-b81e-412676e88b31', '7339f997-0433-4591-811f-3cf66b6d41fe']


def test_hand_json_bounding_box_and_meshing_parameters_g123():
    # F1 hand members (G123: binding-agnostic, src/hand/hand_json.*): the old bindings' JSON form, key for
    # key (the old forms frozen, M3)
    new = r.BoundingBox(1, 2, 3, 4, 5.5, 6)
    old_enc = {'Min': {'X': 1, 'Y': 2, 'Z': 3}, 'Max': {'X': 4, 'Y': 5.5, 'Z': 6}}
    assert new.Encode() == old_enc
    back = r.BoundingBox.Decode(old_enc)
    assert isinstance(back, r.BoundingBox)
    assert (back.Min.X, back.Min.Y, back.Min.Z, back.Max.X, back.Max.Y, back.Max.Z) == (1, 2, 3, 4, 5.5, 6)
    with pytest.raises(KeyError):
        r.BoundingBox.Decode({'Min': {'X': 0, 'Y': 0, 'Z': 0}})
    with pytest.raises(KeyError):
        r.BoundingBox.Decode({'Min': {'X': 0, 'Y': 0}, 'Max': {'X': 1, 'Y': 1, 'Z': 1}})

    old_mp = {'TextureRange': 2, 'JaggedSeams': False, 'RefineGrid': True, 'SimplePlanes': False, 'ComputeCurvature': False,
              'ClosedObjectPostProcess': False, 'GridMinCount': 0, 'GridMaxCount': 0, 'GridAngle': 0.3490658503988659,
              'GridAspectRatio': 6.0, 'GridAmplification': 1.0, 'Tolerance': 0.0, 'MinimumTolerance': 0.0,
              'RelativeTolerance': 0.0, 'MinimumEdgeLength': 0.0001, 'MaximumEdgeLength': 0.0, 'RefineAngle': 0.3490658503988659}
    assert r.MeshingParameters().Encode() == old_mp
    mp = r.MeshingParameters()
    mp.JaggedSeams, mp.RefineGrid, mp.GridMinCount, mp.GridMaxCount = True, False, 17, 300
    mp.Tolerance, mp.MinimumEdgeLength, mp.MaximumEdgeLength, mp.RefineAngle = 0.25, 0.01, 12.5, 0.3
    d = mp.Encode()
    assert (d['JaggedSeams'], d['RefineGrid'], d['GridMinCount'], d['GridMaxCount']) == (True, False, 17, 300)
    assert (d['Tolerance'], d['MinimumEdgeLength'], d['MaximumEdgeLength'], d['RefineAngle']) == (0.25, 0.01, 12.5, 0.3)
    assert set(d) == set(old_mp)
    back = r.MeshingParameters.Decode(d)
    assert isinstance(back, r.MeshingParameters)
    assert back.Encode() == d                 # (the old Decode cast GridMinCount to bool: pilot #57)



def test_hand_json_value_types_f2():
    # F2 hand members (G123): every value type's Encode is the old one, key for key (frozen, M3)
    plane = {'Origin': {'X': 1.0, 'Y': 2.0, 'Z': 3.0}, 'XAxis': {'X': 1.0, 'Y': 0.0, 'Z': -0.0},
             'YAxis': {'X': -0.0, 'Y': 1.0, 'Z': 0.0}, 'ZAxis': {'X': 0.0, 'Y': 0.0, 'Z': 1.0}}
    world = lambda o: {'Origin': o, 'XAxis': {'X': 1.0, 'Y': 0.0, 'Z': 0.0}, 'YAxis': {'X': 0.0, 'Y': 1.0, 'Z': 0.0},
                       'ZAxis': {'X': 0.0, 'Y': 0.0, 'Z': 1.0}}
    old_sphere = {'Radius': 4.0, 'EquatorialPlane': world({'X': 1.0, 'Y': 2.0, 'Z': 3.0})}
    pairs = [(r.Point2d(1, 2), {'X': 1.0, 'Y': 2.0}), (r.Point2f(1.5, 2.5), {'X': 1.5, 'Y': 2.5}),
             (r.Point3d(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}), (r.Point3f(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}),
             (r.Point4d(1, 2, 3, 4), {'X': 1.0, 'Y': 2.0, 'Z': 3.0, 'W': 4.0}), (r.Vector2d(1, 2), {'X': 1.0, 'Y': 2.0}),
             (r.Vector3d(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}), (r.Vector3f(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}),
             (r.Circle(2.5), {'radius': 2.5, 'plane': world({'X': 0.0, 'Y': 0.0, 'Z': 0.0})}),
             (r.Plane(r.Point3d(1, 2, 3), r.Vector3d(0, 0, 1)), plane),
             (r.Sphere(r.Point3d(1, 2, 3), 4), old_sphere)]
    for new, old in pairs:
        assert new.Encode() == old, type(new).__name__
    # Decode reads the old dicts; a sphere keeps its whole equatorial plane
    tilted = r.Plane(r.Point3d(1, 2, 3), r.Vector3d(1, 1, 1))
    assert r.Plane.Decode(tilted.Encode()).Encode() == tilted.Encode()
    s = r.Sphere(tilted, 4)
    assert r.Sphere.Decode(s.Encode()).Encode() == s.Encode()
    assert isinstance(r.Sphere.Decode(s.Encode()), r.Sphere) and isinstance(r.Plane.Decode(tilted.Encode()), r.Plane)
    assert r.Sphere.Decode(old_sphere).Radius == 4



def _first_geometry(f):
    return next(iter(f.Objects)).Geometry


def test_hand_json_common_object_and_file3dm_f1():
    # F1 hand members (G123): CommonObject / File3dm Encode, Decode -- the old forms. The old binding's
    # Encode of this object is frozen in old_oracle/json_forms_f1_f4.json (M3): the generated Decode reads
    # it, and the generated Encode writes the same keys (the bytes carry opennurbs' version, so the
    # objects, not the bytes, are compared)
    import json
    oracle = json.load(open(os.path.join(os.path.dirname(__file__), 'old_oracle', 'json_forms_f1_f4.json')))
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    new = _first_geometry(r.File3dm.Read(path))
    d = new.Encode()
    old = oracle['common_object_encode']
    assert set(d) == set(old) == {'version', 'archive3dm', 'opennurbs', 'data'} and d['archive3dm'] == old['archive3dm'] == 60
    assert d['version'] == old['version']
    back = r.CommonObject.Decode(d)
    assert type(back).__name__ == type(new).__name__ == oracle['common_object_type']   # the most derived class
    assert back.Encode() == d
    from_old = r.CommonObject.Decode(old)
    assert type(from_old).__name__ == type(new).__name__
    box = lambda g: [round(c, 9) for p in (g.GetBoundingBox(False).Min, g.GetBoundingBox(False).Max) for c in (p.X, p.Y, p.Z)]
    assert box(from_old) == box(new)
    # File3dm: base64 of the model's bytes (any 3dm, the old binding's included)
    f = r.File3dm.Read(path)
    s = f.Encode()
    assert isinstance(s, str) and len(s) > 1000
    g = r.File3dm.Decode(s)
    assert isinstance(g, r.File3dm)
    assert len(list(g.Objects)) == len(list(f.Objects))
    import base64
    with open(path, 'rb') as fh:
        assert len(list(r.File3dm.Decode(base64.b64encode(fh.read()).decode()).Objects)) == len(list(f.Objects))
    opts = r.File3dmWriteOptions()
    opts.Version = 7
    assert len(list(r.File3dm.Decode(f.Encode(opts)).Objects)) == len(list(f.Objects))



def test_hand_rdk_xml_and_embedded_files_f3_f4(tmp_path):
    # F3 RdkXml (model, object attributes, plug-in data entry); F4 GetEmbeddedFileAsBase64, EmbeddedFile.Read
    # (the old binding's strings, by sha256, frozen: M3)
    import hashlib
    sha = lambda s: hashlib.sha256(s.encode()).hexdigest()
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    f = r.File3dm.Read(path)
    rdk = '9f8be1e4d91bee9b901a1d0d0a72e454bf6045bd4c6075e295b4c95e0370e6ba'
    assert sha(f.RdkXml()) == rdk and f.RdkXml().startswith('<xml>')
    entries = [p.RdkXml() for p in f.PlugInData]
    assert [sha(x) for x in entries if x] == [rdk]               # the old PlugInData[0].RdkXml()
    decals = os.path.join(MODELS, 'sphereDecals.3dm')
    na = next(iter(r.File3dm.Read(decals).Objects)).Attributes
    assert sha(na.RdkXml()) == 'be505faefb52c2faacf3dbf1b766542bbdb034017bd6f349edb0e8e326f200b8'
    assert _first_geometry(f).RdkXml() == ''
    p = '/Users/luis.fraguada/Dropbox/screenshots_MBPM1/hops.png'           # the old EmbeddedFilePaths2()[0]
    b = f.GetEmbeddedFileAsBase64(p)
    assert sha(b) == '157d84ea10981b7d76ec4dd1e2298a1e3ab672937135ced3ae40f287d7c35723' and len(b) == 196824
    name = p.rsplit('/', 1)[-1]
    assert f.GetEmbeddedFileAsBase64(name.upper()) == b                 # not strict: the file name, any case
    assert f.GetEmbeddedFileAsBase64(name, True) == ''                  # strict: the full path (as the old one)
    assert f.GetEmbeddedFileAsBase64(p, True) == b
    assert f.GetEmbeddedFileAsBase64('nothing.png') == ''
    note = tmp_path / 'note.txt'
    note.write_text('hello')
    ef = r.File3dmEmbeddedFile.Read(str(note))
    assert isinstance(ef, r.File3dmEmbeddedFile) and ef.Length == 5 and ef.Filename.endswith('note.txt')
    assert r.File3dmEmbeddedFile.Read(str(tmp_path / 'missing.txt')) is None


def test_hand_archivable_dictionary_f4():
    # F4 ArchivableDictionary.EncodeDict / DecodeDict: RhinoCommon's dictionary archive, read by both bindings
    path = os.path.join(MODELS, 'file3dm_stuff.3dm')
    g = _first_geometry(r.File3dm.Read(path))
    d = {'b': True, 'i': 7, 'f': 2.5, 's': 'héllo', 'p': r.Point3d(1, 2, 3), 'v': r.Vector3d(0, 0, 1),
         'g': g, 'gl': [g, g]}
    enc = r.ArchivableDictionary.EncodeDict(d)
    assert set(enc) == {'version', 'archive3dm', 'opennurbs', 'data'}
    back = r.ArchivableDictionary.DecodeDict(enc)
    assert {k: back[k] for k in 'bifs'} == {'b': True, 'i': 7, 'f': 2.5, 's': 'héllo'}
    assert isinstance(back['p'], r.Point3d) and (back['p'].X, back['p'].Y, back['p'].Z) == (1, 2, 3)
    assert isinstance(back['v'], r.Vector3d) and back['v'].Z == 1
    assert type(back['g']).__name__ == type(g).__name__ and back['g'].Encode() == g.Encode()
    assert [type(x).__name__ for x in back['gl']] == [type(g).__name__] * 2
    # (the old binding read it too, with pilot #58: a geometry list got `count` Nones in front)
    # the new one reads the old one's: its EncodeDict({'i': 3, 'p': Point3d(4, 5, 6), 'gl': [g]}), frozen (M3)
    import json
    oracle = json.load(open(os.path.join(os.path.dirname(__file__), 'old_oracle', 'json_forms_f1_f4.json')))
    back2 = r.ArchivableDictionary.DecodeDict(oracle['archivable_dictionary_encode'])
    assert back2['i'] == 3 and (back2['p'].X, back2['p'].Y, back2['p'].Z) == (4, 5, 6) and len(back2['gl']) == 1
    assert type(back2['gl'][0]).__name__ == type(g).__name__
    with pytest.raises(RuntimeError, match="Unable to serialize 'x'"):
        r.ArchivableDictionary.EncodeDict({'x': {}})
    with pytest.raises(RuntimeError):
        r.ArchivableDictionary.DecodeDict(r.CommonObject.Decode(g.Encode()).Encode())   # an object, not a dictionary


def test_hand_render_content_add_g124():
    # File3dmRenderContentTable.Add (hand-kept): on the kind's collection, where FindId went (H1);
    # a copy, as the old Add (ONX_Model::AddModelComponent: an id already there is made unique)
    src = os.path.join(MODELS, 'file3dm_stuff.3dm')
    new_src, new = r.File3dm.Read(src), r.File3dm()
    # the old Add of each top-level content: (id, kind, name), frozen (M3)
    old_added = [('16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'),
                 ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'),
                 ('ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal')]
    kinds = ((new_src.RenderMaterials, new.RenderMaterials), (new_src.RenderEnvironments, new.RenderEnvironments),
             (new_src.RenderTextures, new.RenderTextures))
    for have, into in kinds:
        for c in have:
            into.Add(c)
    def contents(f, col):
        return sorted((str(c.Id), c.Kind, c.Name) for c in col)
    got = contents(new, [c for k in (new.RenderMaterials, new.RenderEnvironments, new.RenderTextures) for c in k])
    assert got and got == old_added
    assert got == contents(new_src, [c for k, _ in kinds for c in k])          # ids kept: none conflict
    # added twice: a second copy, a new id
    m = next(iter(new_src.RenderMaterials))
    new.RenderMaterials.Add(m)
    ids = [str(c.Id) for c in new.RenderMaterials]
    assert len(ids) == len(set(ids)) == len(list(new_src.RenderMaterials)) + 1
    # written and read back
    back = r.File3dm.FromByteArray(new.ToByteArray())
    assert sorted(str(c.Id) for c in back.RenderMaterials) == sorted(ids)
    with pytest.raises(TypeError):
        new.RenderMaterials.Add(next(iter(new_src.RenderEnvironments)))   # the kind's own content only


def test_hand_point_cloud_values_and_group_delete_id_g126():
    # kept by hand (Luis 2026-10-30, upstream U38): overloads RhinoCommon lacks, as Python overloads
    PX = r.Point3d
    n = r.PointCloud()
    n.Add(PX(1, 0, 0), 1.5)
    n.AddRange([PX(2, 0, 0), PX(3, 0, 0)], [2.5, 3.5])
    n.Insert(0, PX(0, 0, 0), 0.5)
    assert list(n.GetPointValues()) == [0.5, 1.5, 2.5, 3.5]   # the old Insert misaligned them: [0.5, 2.5, 3.5, 0.0] (pilot #64)
    assert [q.X for q in n.GetPoints()] == [0, 1, 2, 3]       # the same points as the old one
    # RhinoCommon's overloads still win their own calls
    n.Add(PX(4, 0, 0), r.Vector3d(0, 0, 1))
    assert n.ContainsNormals and n.Count == 5
    with pytest.raises(ValueError, match='equal number'):
        n.AddRange([PX(0, 0, 0)], [1.0, 2.0])
    # every channel stays aligned with its points (pilot #64: RhinoCommon's Insert does not)
    c = r.PointCloud()
    c.Add(PX(1, 0, 0), r.Vector3d(0, 0, 1)); c.Add(PX(2, 0, 0), r.Vector3d(0, 1, 0))
    c.Insert(0, PX(0, 0, 0), 9.0)
    assert [(q.X, q.Y, q.Z) for q in c.GetNormals()] == [(0, 0, 0), (0, 0, 1), (0, 1, 0)]
    assert list(c.GetPointValues()) == [9.0, 0.0, 0.0]
    g = r.PointCloud()
    g.Add(PX(1, 0, 0), r.Vector3d(0, 0, 1)); g.Add(PX(2, 0, 0), r.Vector3d(0, 1, 0))
    g.Insert(0, PX(0, 0, 0), r.Vector3d(1, 0, 0))
    assert [(q.X, q.Y, q.Z) for q in g.GetNormals()] == [(1, 0, 0), (0, 0, 1), (0, 1, 0)]   # U37 (pilot #64) fixed
    # File3dmGroupTable.Delete(id) (as the old Groups.Delete(id): True, then False for an unknown id)
    nf = r.File3dm()
    for _ in range(3):
        nf.AllGroups.AddGroup()
    assert nf.AllGroups.Delete(nf.AllGroups.FindIndex(1).Id) is True
    assert nf.AllGroups.Delete(uuid.uuid4()) is False
    nf.AllGroups.Delete(0)                                  # RhinoCommon's Delete(int) still wins an int
    assert nf.AllGroups.Count == 1


@pytest.mark.skipif(not hasattr(r, 'Centermark'), reason='G127')
def test_centermark_get_display_lines_u12_g127():
    # U12 (RH-99905): RhinoCommon's Centermark.GetDisplayLines, uncommented upstream with its loop
    # over the 6 lines, now generates; the kept hand member retires. The same centermark, saved by the
    # generated binding: the old one read it back as a Centermark with these lines (frozen, M3)
    cm = r.Centermark()
    cm.AdjustFromPoints(r.Plane.WorldXY, r.Point3d(1, 2, 0))
    cm.Radius = 3.0
    f = r.File3dm()
    f.Objects.Add(cm, None)
    new = next(o.Geometry for o in r.File3dm.FromByteArray(f.ToByteArray()).Objects)
    assert isinstance(new, r.Centermark)
    ok, lines = new.GetDisplayLines(r.DimensionStyle(), 1.0)
    key = lambda l: (round(l.From.X, 9), round(l.From.Y, 9), round(l.To.X, 9), round(l.To.Y, 9))
    assert ok and sorted(key(l) for l in lines) == [(0.5, 2.0, 1.5, 2.0), (1.0, 1.5, 1.0, 2.5)]


def test_point_cloud_insert_and_add_range_keep_channels_aligned_u37():
    # U37 (RH-99914, pilot #64): RhinoCommon's C layer inserted only the point (the normals / colors /
    # values padded at the END), and a range adding a channel the cloud lacked put it on the FIRST points
    PX, VX = r.Point3d, r.Vector3d
    c = r.PointCloud()
    c.Add(PX(1, 0, 0), VX(0, 0, 1)); c.Add(PX(2, 0, 0), VX(0, 1, 0))
    c.Insert(0, PX(0, 0, 0), VX(1, 0, 0))
    assert [(q.X, q.Y, q.Z) for q in c.GetNormals()] == [(1, 0, 0), (0, 0, 1), (0, 1, 0)]
    c.Insert(1, PX(9, 0, 0))                                # point only: a zero normal at 1
    assert [(q.X, q.Y, q.Z) for q in c.GetNormals()] == [(1, 0, 0), (0, 0, 0), (0, 0, 1), (0, 1, 0)]
    assert [q.X for q in c.GetPoints()] == [0, 9, 1, 2]
    d = r.PointCloud()
    d.Add(PX(1, 0, 0)); d.Add(PX(2, 0, 0))
    d.AddRange([PX(3, 0, 0), PX(4, 0, 0)], [VX(0, 0, 1), VX(0, 0, 1)])
    assert [(q.X, q.Y, q.Z) for q in d.GetNormals()] == [(0, 0, 0), (0, 0, 0), (0, 0, 1), (0, 0, 1)]



def test_mesh_vertex_color_add_rgb_g128():
    # overload-audit gap: MeshVertexColorList.Add(r, g, b) / SetColor(index, r, g, b) -- RhinoCommon builds
    # the color with Color.FromArgb(r, g, b), which the translator now reads (G128)
    new = r.Mesh()
    for x, y in [(0, 0), (1, 0), (1, 1)]:
        new.Vertices.Add(x, y, 0)
    assert [new.VertexColors.Add(*c) for c in [(255, 0, 0), (0, 128, 0)]] == [0, 1]     # as the old Add(r, g, b)
    assert new.VertexColors.SetColor(2, 0, 0, 255)
    # the colors the old Add(r, g, b) stored for the same three (frozen, M3)
    assert [c & 0xFFFFFFFF for c in new.VertexColors.ToARGBArray()] == [0xFFFF0000, 0xFF008000, 0xFF0000FF]
    with pytest.raises(ValueError, match="Value of '300' is not valid for 'red'"):
        new.VertexColors.Add(300, 0, 0)


def test_intersection_line_line_tolerance_and_plane_circle_g128():
    # overload-audit gap: Intersection.LineLine(a, b, tolerance, finiteSegments) -- a static with
    # out parameters calling its sibling's (G128); PlaneCircle comes with it
    PX, LX = r.Point3d, r.Line
    # each case with the old LineLine's (ok, a, b) for (tol, finite) = (0, False), (0.001, True), (5, True) (frozen, M3)
    cases = [((0, 0, 0), (4, 0, 0), (2, -1, 0), (2, 3, 0), [(True, 0.5, 0.25)] * 3),                       # crossing inside both
             ((0, 0, 0), (1, 0, 0), (3, -1, 0), (3, 1, 0), [(True, 3.0, 0.5), (False, 1.0, 0.5), (True, 1.0, 0.5)]),   # past the end of the first
             ((0, 0, 0), (1, 0, 0), (0, 0, 1), (1, 0, 1), [(False, 0.0, 0.0)] * 3)]                        # parallel
    for a0, a1, b0, b1, olds in cases:
        for (tol, finite), old in zip([(0.0, False), (0.001, True), (5.0, True)], olds):
            new = r.Intersection.LineLine(LX(PX(*a0), PX(*a1)), LX(PX(*b0), PX(*b1)), tol, finite)
            assert tuple(new) == old
    ok, a, b = r.Intersection.LineLine(LX(PX(0, 0, 0), PX(1, 0, 0)), LX(PX(3, -1, 0), PX(3, 1, 0)), 0.001, True)
    assert (ok, a, b) == (False, 1.0, 0.5)                 # clamped to the segment: 2 apart
    # PlaneCircle: a circle of radius 2 in the world XZ plane against the XY plane meets it at x = +-2
    circle = r.Circle(r.Plane.WorldZX, 2.0)
    kind, t0, t1 = r.Intersection.PlaneCircle(r.Plane.WorldXY, circle)
    assert kind == r.PlaneCircleIntersection.Secant
    xs = sorted(round(circle.PointAt(t).X, 9) for t in (t0, t1))
    assert xs == [-2.0, 2.0]
    kind, t0, t1 = r.Intersection.PlaneCircle(r.Plane.WorldXY, r.Circle(r.Plane.WorldXY, 2.0))
    assert kind == r.PlaneCircleIntersection.Coincident and t0 == t1 < -1e300   # RhinoMath.UnsetValue


def _grid_mesh():
    # 3 x 3 vertices (index j*3 + i at (i, j)), four quads; vertex 4 (1, 1) is the only interior one
    m = r.Mesh()
    for j in range(3):
        for i in range(3):
            m.Vertices.Add(float(i), float(j), 0.0)
    for j in range(2):
        for i in range(2):
            a = j * 3 + i
            m.Faces.AddFace(a, a + 1, a + 4, a + 3)
    return m


def test_members_unlocked_by_array_natives_g128():
    # G128 (Luis: option 1): a body passing an int[] / double[] local to a native translates; every
    # member it unlocked, on inputs whose results are known by hand (and the old binding's, where it has one)
    PX, VX = r.Point3d, r.Vector3d
    m = _grid_mesh()
    assert list(m.Faces.ToIntArray(False)) == [0, 1, 4, 3, 1, 2, 5, 4, 3, 4, 7, 6, 4, 5, 8, 7]
    assert list(m.Faces.ToIntArray(True))[:6] == [0, 1, 4, 4, 3, 0]           # a quad as two triangles
    assert len(m.Faces.ToIntArray(True)) == 4 * 6
    assert list(m.GetNakedEdgePointStatus()) == [True] * 4 + [False] + [True] * 4
    tv = list(m.Faces.GetTopologicalVertices(0))
    assert len(tv) == 4 and len(set(tv)) == 4 and all(0 <= t < m.Vertices.Count for t in tv)   # no duplicate vertices here
    # DeleteFaces / RemoveAt: compact (the default) drops the vertex only the deleted face used
    for op, faces, verts in [(lambda x: x.Faces.DeleteFaces([0], True), 3, 8), (lambda x: x.Faces.DeleteFaces([0, 3]), 2, 7),
                             (lambda x: x.Faces.RemoveAt(0), 3, 8), (lambda x: x.Faces.RemoveAt(0, False), 3, 9)]:
        g = _grid_mesh(); op(g)
        assert (g.Faces.Count, g.Vertices.Count) == (faces, verts)
    assert _grid_mesh().Faces.DeleteFaces([0, 1]) == 2
    # vertex colors: SetColors replaces, AppendColors / AddRange append
    vc = m.VertexColors
    assert vc.SetColors([(255, 0, 0, 255)] * 9) and vc.Count == 9
    assert vc.AppendColors([(0, 0, 255, 255)] * 2) and vc.AddRange([(0, 255, 0, 255)])
    argb = [c & 0xFFFFFFFF for c in vc.ToARGBArray()]
    assert argb == [0xFFFF0000] * 9 + [0xFF0000FF] * 2 + [0xFF00FF00]
    # NurbsCurve.GrevilleParameters: the average of each control point's `degree` knots
    c = r.NurbsCurve.CreateFromCircle(r.Circle(2.0))
    k, d = list(c.Knots), c.Degree
    expect = [sum(k[i:i + d]) / d for i in range(c.Points.Count)]
    assert [round(x, 12) for x in c.GrevilleParameters()] == [round(x, 12) for x in expect]
    # Surface.Evaluate: a plane surface's point and first derivatives
    s = r.PlaneSurface(r.Plane.WorldXY, r.Interval(0, 2), r.Interval(0, 3))
    ok, pt, ders = s.Evaluate(0.5, 1.5, 1)
    assert ok and (pt.X, pt.Y, pt.Z) == (0.5, 1.5, 0.0)
    assert [(v.X, v.Y, v.Z) for v in ders] == [(1, 0, 0), (0, 1, 0)]
    # Brep.DuplicateSubBrep: the chosen faces only
    box = r.Brep.CreateFromBox(r.BoundingBox(0, 0, 0, 1, 2, 3))
    sub = box.DuplicateSubBrep([0, 1])
    assert sub.Faces.Count == 2 and sub.IsValid and box.Faces.Count == 6
    # Linetype.SetSegments: lengths >= 0 dashes, < 0 gaps
    lt = r.Linetype()
    assert lt.SetSegments([1.0, -0.5, 0.25, -0.5])
    assert lt.SegmentCount == 4
    assert [tuple(lt.GetSegment(i)) for i in range(4)] == [(1.0, True), (0.5, False), (0.25, True), (0.5, False)]
    # PointCloud.AddRange(points, normals, colors[, values]), against the old binding's (frozen, M3)
    pts, nrm = [(0, 0, 0), (1, 0, 0)], [(0, 0, 1), (0, 1, 0)]
    cols, vals = [(255, 0, 0, 255), (0, 0, 255, 255)], [1.5, 2.5]
    n3 = r.PointCloud()
    n3.AddRange([PX(*p) for p in pts], [VX(*v) for v in nrm], cols)
    n4 = r.PointCloud()
    n4.AddRange([PX(*p) for p in pts], [VX(*v) for v in nrm], cols, vals)
    sig = lambda c: ([(q.X, q.Y, q.Z) for q in c.GetNormals()], list(c.GetColors()), list(c.GetPointValues()))
    assert sig(n3) == ([(0.0, 0.0, 1.0), (0.0, 1.0, 0.0)], [(255, 0, 0, 255), (0, 0, 255, 255)], [])
    assert list(n4.GetPointValues()) == [1.5, 2.5] and n4.Count == 2
    # (the old (points, normals, colors, values) added nothing: pilot #66)
    with pytest.raises(ValueError, match='equal number of points, vectors, and colors'):
        n3.AddRange([PX(0, 0, 0)], [VX(0, 0, 1)], cols)


def test_subd_neighbour_lists_g129():
    # G129 (Luis 2026-10-30, option 1): rhino3dm 8.32's neighbour properties as lists of the generated
    # components, as RhinoCommon's vertex.Edges -- face.Edges / .Vertices, edge.Faces / .Vertices,
    # vertex.Faces; every neighbour list against the old binding's iterator, by id, in order (frozen in
    # old_oracle/subd_neighbours_g129.json, M3)
    import json
    oracle = json.load(open(os.path.join(os.path.dirname(__file__), 'old_oracle', 'subd_neighbours_g129.json')))['subdBox.3dm']
    p = os.path.join(MODELS, 'subdBox.3dm')
    new = next(o.Geometry for o in r.File3dm.Read(p).Objects if isinstance(o.Geometry, r.SubD))
    ids = lambda it: [c.Id for c in it]
    of, oe, ov = oracle['faces'], oracle['edges'], oracle['vertices']
    checked = 0
    for comps, olds, props in [(new.Faces, of, ('Edges', 'Vertices')), (new.Edges, oe, ('Faces', 'Vertices')),
                               (new.Vertices, ov, ('Faces',))]:
        assert len(comps) == len(olds)
        for c in comps:
            for prop in props:
                assert ids(getattr(c, prop)) == olds[str(c.Id)][prop], (type(c).__name__, prop, c.Id)
                checked += 1
    assert checked > 50
    # vertex.Edges: RhinoCommon's (a list), the same edges in the same order
    for v in new.Vertices:
        assert ids(v.Edges) == ov[str(v.Id)]['Edges']
    # lists, like RhinoCommon's vertex.Edges: the same kind of value on every component
    face = next(iter(new.Faces))
    v = next(iter(new.Vertices))
    for lst in (face.Edges, face.Vertices, v.Faces, v.Edges):
        assert isinstance(lst, list)
    assert len(face.Edges) == face.EdgeCount and [e.Id for e in face.Edges] == [face.EdgeAt(i).Id for i in range(face.EdgeCount)]
    assert face.Edges[-1].Id == face.EdgeAt(face.EdgeCount - 1).Id
    assert all(isinstance(e, r.SubDEdge) and e.FaceCount >= 1 for e in face.Edges)
    # lifetimes: each item keeps its component (and so the SubD) alive
    faces = next(iter(new.Edges)).Faces
    del new, face, v; gc.collect()
    assert len(faces) >= 1 and faces[0].EdgeCount >= 3


def test_polycurve_append_line_and_point_cloud_add_range_colors_g130():
    # overload-audit gaps, part 2 (G130): PolyCurve.Append(Line) -- `Append(new LineCurve(line))`, the
    # temporary freed after a callee that only reads it -- and PointCloud.AddRange(points, colors)
    PX = r.Point3d
    new = r.PolyCurve()
    assert new.Append(r.Line(PX(0, 0, 0), PX(2, 0, 0))) is True           # the old Append(Line): True, True, 2 segments
    assert new.Append(r.Line(PX(2, 0, 0), PX(2, 3, 0))) is True
    assert new.SegmentCount == 2
    e = new.PointAtEnd
    assert (e.X, e.Y, e.Z) == (2, 3, 0)
    assert new.Append(r.Line(PX(0, 0, 0), PX(0, 0, 0))) is False          # `if (!line.IsValid) return false;`
    assert new.SegmentCount == 2
    # AddRange(points, colors): RhinoCommon swaps to ABGR for the native, as .NET does
    cols = [(255, 0, 0, 255), (0, 128, 255, 255)]
    n = r.PointCloud()
    n.AddRange([PX(0, 0, 0), PX(1, 0, 0)], cols)
    rgb = lambda c: [k[:3] for k in c.GetColors()]
    assert rgb(n) == [(255, 0, 0), (0, 128, 255)]                           # as the old AddRange's
    with pytest.raises(ValueError, match='equal number of points and colors'):
        n.AddRange([PX(0, 0, 0)], cols)


@pytest.mark.xfail(reason='pilot #67 / U40: RhinoCommon AddRange(points, colors) on rhino3dm / .NET Core builds stores '
                          '.NET opacity in ON_Color\'s transparency byte -- an opaque color comes back fully transparent. '
                          'Won\'t fix (Luis 2026-10-31): historical, callers may rely on it; pinned so a change shows',
                   strict=True)
def test_point_cloud_add_range_colors_keeps_opacity_u40():
    n = r.PointCloud()
    n.AddRange([r.Point3d(0, 0, 0)], [(255, 0, 0, 255)])
    assert list(n.GetColors()) == [(255, 0, 0, 255)]



def test_static_and_instance_method_one_name_g132():
    # RhinoCommon's BoundingBox.Union(a, b) (static, a new box) and bbox.Union(other) (instance, in place):
    # pybind11 and nanobind cannot overload one name across the two; the module joins them (G132)
    a, b = r.BoundingBox(0, 0, 0, 1, 1, 1), r.BoundingBox(2, 2, 2, 3, 3, 3)
    u = r.BoundingBox.Union(a, b)
    box = lambda x: (x.Min.X, x.Min.Y, x.Min.Z, x.Max.X, x.Max.Y, x.Max.Z)
    assert box(u) == (0, 0, 0, 3, 3, 3)
    assert box(a) == (0, 0, 0, 1, 1, 1)                     # the static leaves its arguments alone
    # (the same box as the old, static-only BoundingBox.Union(a, b))
    assert box(r.BoundingBox.Union(a, r.Point3d(-1, 0, 5))) == (-1, 0, 0, 1, 1, 5)
    assert a.Union(b) is None                                # the instance form grows the box itself
    assert box(a) == (0, 0, 0, 3, 3, 3)
    a.Union(r.Point3d(5, 0, 0))
    assert box(a) == (0, 0, 0, 5, 3, 3)
    with pytest.raises(TypeError):
        r.BoundingBox.Union(a)                              # on the class, only the static's overloads
    assert '_static_Union' in vars(r.BoundingBox)


def test_subd_create_from_cylinder_and_texture_coordinate_type_m3():
    # unblocked in M3 step 1 (G137): every enum generated, SubDEndCapStyle / SubDComponentLocation /
    # SubDTextureCoordinateType included. Counts from opennurbs' ON_SubD::CreateCylinder, 8 x 2 faces
    cyl = r.Cylinder(r.Circle(r.Plane.WorldXY, 2.0), 3.0)
    C, L = r.SubDEndCapStyle, r.SubDComponentLocation
    def make(cap, loc=L.ControlNet):
        return r.SubD.CreateFromCylinder(cyl, 8, 2, getattr(C, cap), r.SubDEdgeTag.Crease, loc)
    got = {cap: (s.Faces.Count, s.Edges.Count, s.Vertices.Count) for cap in ['None', 'Quads', 'Triangles', 'Ngon']
           for s in [make(cap)]}
    assert got == {'None': (16, 40, 24), 'Quads': (24, 48, 26), 'Triangles': (32, 56, 26), 'Ngon': (18, 40, 24)}
    # radiusLocation: the control net on the radius, or the limit surface (the net lies outside it)
    net, srf = make('None').GetBoundingBox(True), make('None', L.Surface).GetBoundingBox(True)
    assert (net.Min.X, net.Max.X, net.Max.Z) == (-2.0, 2.0, 3.0)
    assert srf.Max.X == pytest.approx(2.216388375108776, abs=1e-12) and srf.Max.Z == 3.0
    with pytest.raises(TypeError):
        r.SubD.CreateFromCylinder(cyl, 8, 2, 1, r.SubDEdgeTag.Crease, L.ControlNet)    # an int is not an end cap style
    assert make('None').TextureCoordinateType == r.SubDTextureCoordinateType.Unset


def test_str_is_rhinocommons_to_string_g136():
    # pilot #68 (G136): str() of a value is RhinoCommon's ToString -- invariant culture, a double in its
    # shortest round-trip form (8.x's RH3DM-180 "1,2,3" too, but 8.x printed 6 significant digits: D75)
    P = r.Point3d
    assert str(P(1, 2, 3)) == '1,2,3' and P(1, 2, 3).ToString() == '1,2,3'
    assert str(r.Point2d(1, 2)) == '1,2' and str(r.Vector2d(4, 5)) == '4,5' and str(r.Vector3d(1, 2, 3)) == '1,2,3'
    assert str(P(1.5, 2.25, 3.75)) == '1.5,2.25,3.75'
    assert repr(P(1, 2, 3)).startswith('Point3d(') and str(P(1, 2, 3)) != repr(P(1, 2, 3))
    # .NET's number forms: shortest round-trip, scientific from 1E+15 and from 1E-05 down, -0, NaN, Infinity
    assert str(P(0.1 + 0.2, 1e15, 1e-05)) == '0.30000000000000004,1E+15,1E-05'
    assert str(P(1e14, 0.0001, -1.5e-7)) == '100000000000000,0.0001,-1.5E-07'
    assert str(P(-0.0, float('nan'), float('inf'))) == '-0,NaN,Infinity'
    assert str(P(1.2345678901234567e20, -float('inf'), 123.456)) == '1.2345678901234567E+20,-Infinity,123.456'
    # a float: its own shortest digits, scientific from 1E+07
    assert str(r.Point3f(1.1, 1e7, 1234567.0)) == '1.1,1E+07,1234567'
    assert str(r.Point4d(1, 2, 3, 4)) == '1,2,3,4'
    assert str(r.Interval(0, 1.5)) == '0,1.5'
    assert str(r.Line(P(0, 0, 0), P(1, 2, 3))) == '0,0,0,1,2,3'
    assert str(r.BoundingBox(0, 0, 0, 1, 1, 1)) == '0,0,0 - 1,1,1'
    assert str(r.Plane.WorldXY) == 'Origin=0,0,0 XAxis=1,0,0, YAxis=0,1,0, ZAxis=0,0,1'
    assert str(r.Transform.Identity) == 'R0=(1,0,0,0), R1=(0,1,0,0), R2=(0,0,1,0), R3=(0,0,0,1)'
    assert str(r.MeshFace(0, 1, 2)) == 'T(0, 1, 2)' and str(r.MeshFace(0, 1, 2, 3)) == 'Q(0, 1, 2, 3)'
    layer = r.Layer()
    layer.Name = 'Walls'
    assert str(layer) == layer.ToString() == 'Walls'                  # a class's ToString too (Layer: FullPath)
