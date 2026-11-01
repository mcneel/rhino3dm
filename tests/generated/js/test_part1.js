// JS port of tests/generated/test_generated.py, part 1: from test_guard_raises_like_rhinocommon
// up to (not including) test_subd_neighbours_replace_the_old_component_iterators. Same names,
// same structural contracts, checked against the COMPILED (WASM) module.
module.exports = (t, ctx) => {
  const { r, assert, approx, val, skip, list } = ctx;
  const P = (X, Y, Z) => ({ X, Y, Z });
  const BLACK = { r: 0, g: 0, b: 0, a: 255 };
  const ZERO_ID = '00000000-0000-0000-0000-000000000000';
  // a sequence over count/get(i) (BrepFaceList & co. are this[int] lists, not iterators)
  const seq = coll => { const out = []; for (let i = 0; i < coll.count; i++) out.push(coll.get(i)); return out; };

  // --- marshalling shapes ---------------------------------------------------

  t('test_guard_raises_like_rhinocommon', () => {
    const x = ctx.curve();
    // in range the call works (the guard is only on the bad index)
    const d = x.spanDomain(0);
    assert.strictEqual(typeof d.T0, 'number');
    ctx.raises(() => x.spanDomain(-1), /out_of_range,spanIndex must be larger than or equal to zero/);
  });

  t('test_holder_array_return', () => {
    const c = ctx.curve();
    const dom = val(c.domain);
    assert.deepStrictEqual(c.spanVector(), [dom.T0, dom.T1]);
  });

  t('test_enum_parameters_are_type_checked', () => {
    const x = ctx.curve();
    assert.strictEqual(x.isContinuous(r.Continuity.C2_continuous, 0.5), true);
    // pilot #47 / G101: enum arguments are checked (Python raises TypeError; JS a TypeError too)
    ctx.raises(() => x.isContinuous(3, 0.5), /expected a .*Continuity value, got number/);
    ctx.raises(() => x.isContinuous('C1', 0.5), /got string/);
    ctx.raises(() => x.isContinuous({}, 0.5), /got object/);
    // a property setter goes through the same conversion
    const ds = new r.DimensionStyle();
    ds.arrowType1 = r.DimensionStyleArrowType.Dot;
    ctx.raises(() => { ds.arrowType1 = 3; }, /got number/);
    assert.strictEqual(ds.arrowType1, r.DimensionStyleArrowType.Dot);
  });

  t('test_string_argument_roundtrip', () => {
    assert.strictEqual(r.ModelComponent.isValidComponentName('Layer 01'), true);
    assert.strictEqual(r.ModelComponent.isValidComponentName(''), false);
  });

  // --- classes --------------------------------------------------------------

  t('test_abstract_bases_are_not_constructible', () => {
    // no constructor is registered for the abstract bases (the surface lock has none)
    assert.strictEqual(r.GeometryBase.prototype.constructor, r.GeometryBase);
    ctx.raises(() => new r.GeometryBase(), /GeometryBase has no accessible constructor/);
    ctx.raises(() => new r.ModelComponent(), /ModelComponent has no accessible constructor/);
  });

  t('test_inheritance_reaches_base_members', () => {
    const pc = new r.PolyCurve();
    pc.append(new r.LineCurve(P(0, 0, 0), P(2, 0, 0)));
    assert(pc instanceof r.Curve && pc instanceof r.GeometryBase);
    assert.deepStrictEqual(pc.pointAt(1.0), P(1, 0, 0));      // declared on Curve
  });

  t('test_bridge_free_classes_reject_old_binding_objects', () => {
    // (the old ViewportInfo was refused here: "Expected null or instance of ViewportInfo" -- old-binding only, M3)
    const v = new r.ViewportInfo();
    assert.strictEqual(v.setCameraLocation(P(7, 8, 9)), true);
    assert.deepStrictEqual(v.cameraLocation, P(7, 8, 9));
  });

  t('test_subclass_aware_wrapping', () => {
    const srf = r.Sphere.toNurbsSurface(r.Sphere.create(P(0, 0, 0), 5));
    const iso = srf.isoCurve(r.IsoStatus.X, 0.5);
    assert(iso instanceof r.NurbsCurve, 'isoCurve did not downcast');
    assert.strictEqual(iso.constructor.name, 'NurbsCurve');
  });

  t('test_class_argument_is_copied_not_adopted', () => {
    const pc = new r.PolyCurve();
    const seg = new r.LineCurve(P(0, 0, 0), P(1, 0, 0));
    assert.strictEqual(pc.append(seg), true);
    seg.delete();                                     // the C layer duplicated it
    assert.strictEqual(pc.segmentCount, 1);
    assert.deepStrictEqual(pc.pointAtEnd, P(1, 0, 0));
  });

  t('test_rhinocommon_copy_constructor_is_deep', () => {
    const a = new r.Linetype();
    a.appendSegment(1.5, true);
    const b = new r.Linetype(a);
    b.appendSegment(2.0, false);
    assert.deepStrictEqual([val(a.patternLength), val(b.patternLength)], [1.5, 3.5]);
  });

  t('test_zero_copy_point_buffer', () => {
    skip('python buffer protocol (numpy zero-copy view) only');
  });

  // --- found bugs -----------------------------------------------------------

  t('test_finding_01_linetype_double_free', () => {
    const lx = new r.Linetype();
    lx.delete();                                      // the old Linetype double-freed before the fix
  });

  t('test_ref_of_by_value_transform_uses_the_argument', () => {
    const c = new r.LineCurve(P(0, 0, 0), P(1, 0, 0));
    const xf = r.Transform.translation({ X: 0, Y: 0, Z: 5 });
    assert.strictEqual(c.transform(xf), true);
    assert.deepStrictEqual(c.pointAtStart, P(0, 0, 5));
  });

  t('test_finding_14_rhinocommon_value_semantics', () => {
    const q = P(4, 6, 3);
    assert.strictEqual(r.Point3d.distanceTo(r.Point3d.unset, q), 0.0);
  });

  t('test_same_arity_constructor_overloads_both_reachable', () => {
    const a = new r.LineCurve(P(1, 2, 3), P(4, 5, 6)).line;
    const b = new r.LineCurve({ X: 1, Y: 2 }, { X: 4, Y: 5 }).line;
    assert.deepStrictEqual([a.To.Z, b.To.Z], [6.0, 0.0]);
    assert.deepStrictEqual([a.To.X, a.To.Y, b.To.X, b.To.Y], [4, 5, 4, 5]);
  });

  t('test_property_setters_write_through', () => {
    const c = new r.LineCurve(P(0, 0, 0), P(2, 0, 0));
    c.domain = { T0: 1, T1: 5 };                       // value type through `ref value`
    assert.deepStrictEqual([c.domain.T0, c.domain.T1], [1.0, 5.0]);
    const ds = new r.DimensionStyle();
    ds.arrowLength = 0.75;                             // inlined private helper + selector
    assert.strictEqual(ds.arrowLength, 0.75);
    // read-only in RhinoCommon stays read-only: embind registers no setter
    const desc = (() => {
      for (let p = r.Curve.prototype; p; p = Object.getPrototypeOf(p)) {
        const d = Object.getOwnPropertyDescriptor(p, 'pointAtStart');
        if (d) return d;
      }
    })();
    assert(desc, 'pointAtStart not found');
    // embind gives a read-only property a setter that only raises "is a read-only property"
    assert(desc.set === undefined || /read-only/.test(String(desc.set)), 'pointAtStart has a real setter');
    ctx.raises(() => { c.pointAtStart = P(1, 1, 1); }, /pointAtStart is a read-only property/);
  });

  // Lifetime contract (G101, option a): a view holds its parent through `__rh3dm_parent`, so
  // dropping every JS reference to the parent and letting GC run must leave the view usable.
  // An explicit .delete() of the parent is still immediate (embind) and is not tested here.
  t('test_file3dm_table_view_keeps_the_file_alive', async () => {
    const objs = (() => new r.File3dm().objects)();
    assert(objs.__rh3dm_parent instanceof r.File3dm);
    await ctx.gcSettle();                              // the view must keep the model alive
    objs.addPoint(P(1, 2, 3), null);
    objs.addPoint(P(-1, 0, 5), null);
    const bb = objs.getBoundingBox();
    assert.deepStrictEqual([bb.Min.X, bb.Max.Z], [-1.0, 5.0]);
  });

  t('test_file3dm_settings_view_writes_through', () => {
    const f = new r.File3dm();
    f.settings.modelAbsoluteTolerance = 0.25;
    assert.strictEqual(f.settings.modelAbsoluteTolerance, 0.25);
  });

  t('test_file3dm_read_missing_file_raises_file_not_found', () => {
    skip('no host file system in WASM; the Python test only checks the FileNotFoundError raise');
  });

  // --- G28 ------------------------------------------------------------------

  t('test_g28_early_returns_and_optional_arguments', () => {
    // three independent contracts: run each so one failure does not hide the others
    const bad = [];
    const check = (what, fn) => { try { fn(); } catch (e) { bad.push(what + ': ' + String(e).split('\n')[0]); } };
    check('PolyCurve.append(null) is false', () => {
      assert.strictEqual(new r.PolyCurve().append(null), false);   // `if (null == curve) return false;`
    });
    check('new ArcCurve(null)', () => {
      assert(new r.ArcCurve(null) instanceof r.ArcCurve);          // `new ArcCurve(null)`: ON_ArcCurve_New(nullptr)
    });
    check('transform(Identity) is true', () => {
      const c = new r.LineCurve(P(0, 0, 0), P(1, 0, 0));
      assert.strictEqual(c.transform(r.Transform.identity), true);   // `if (xform.IsIdentity) return true;`
    });
    assert.strictEqual(bad.length, 0, bad.join(' ; '));
  });

  // --- G29: File3dm tables --------------------------------------------------

  t('test_file3dm_layer_table_basics', () => {
    const f = new r.File3dm();
    const layers = f.allLayers;
    assert.strictEqual(layers.count, 0);
    const i0 = layers.addLayer('a', { r: 255, g: 0, b: 0, a: 255 });
    const i1 = layers.addLayer('b', { r: 0, g: 255, b: 0, a: 255 });
    assert.deepStrictEqual([i0, i1, layers.count], [0, 1, 2]);
    assert.deepStrictEqual([0, 1].map(i => layers.findIndex(i).name), ['a', 'b']);
    assert.strictEqual(layers.findIndex(7), null);     // RhinoCommon returns null
    assert.strictEqual(f.layers.count, 2);             // IList<Layer> Layers: the same table
    ctx.raises(() => layers.delete(7), /out_of_range,/);
  });

  t('test_file3dm_table_items_are_live_proxies', async () => {
    const layer = (() => {
      const f = new r.File3dm();
      f.allLayers.addLayer('before', BLACK);
      const l = f.allLayers.findIndex(0);
      l.name = 'after';
      assert.strictEqual(f.allLayers.findIndex(0).name, 'after');
      return l;
    })();
    await ctx.gcSettle();                              // the item keeps the file alive
    assert.strictEqual(layer.name, 'after');
  });

  t('test_file3dm_table_add_copies_and_delete_by_item', () => {
    const f = new r.File3dm();
    const mine = new r.Layer();
    mine.name = 'mine';
    f.allLayers.add(mine);                             // ONX_Model_AddModelComponent copies
    mine.name = 'changed later';
    assert.strictEqual(f.allLayers.findIndex(0).name, 'mine');
    assert.strictEqual(f.allLayers.delete(null), false);
    assert.strictEqual(f.allLayers.delete(f.allLayers.findIndex(0)), true);
  });

  t('test_file3dm_table_find_name', () => {
    const f = new r.File3dm();
    f.allLayers.addLayer('walls', BLACK);
    assert.strictEqual(f.allLayers.findName('walls', ZERO_ID).name, 'walls');
    assert.strictEqual(f.allLayers.findName('nope', ZERO_ID), null);
  });

  t('test_nested_enum_lives_in_its_owner_class', () => {
    // JS has no nested enums: DimensionStyle.ArrowType is r.DimensionStyleArrowType
    const E = r.DimensionStyleArrowType;
    assert(E && E.Dot, 'DimensionStyleArrowType.Dot missing');
    const ds = new r.DimensionStyle();
    ds.arrowType1 = E.Dot;
    assert.strictEqual(ds.arrowType1, E.Dot);
  });

  t('test_instance_definition_object_ids_holder', () => {
    assert.deepStrictEqual(new r.InstanceDefinitionGeometry().getObjectIds(), []);
  });

  // --- G39: table protocols -------------------------------------------------

  t('test_table_iteration_survives_a_delete', () => {
    const f = new r.File3dm();
    const tb = f.allLayers;
    for (const n of 'abc') tb.addLayer(n, BLACK);
    assert.strictEqual(tb.delete(tb.findIndex(1)), true);
    assert.strictEqual(tb.count, 2);
    assert.deepStrictEqual(list(tb).map(l => l.name), ['a', 'c']);
    assert.deepStrictEqual(list(tb).map(l => l.index), [0, 2]);
    assert.deepStrictEqual([...tb].map(l => l.name), ['a', 'c']);      // for...of protocol
    tb.addLayer('d', BLACK);
    assert.deepStrictEqual(list(tb).map(l => l.name), ['a', 'c', 'd']);
    assert.deepStrictEqual(list(f.layers).map(l => l.name), ['a', 'c', 'd']);
  });

  t('test_table_getitem_is_the_manifest_index', () => {
    const f = new r.File3dm();
    const tb = f.allLayers;
    for (const n of 'abc') tb.addLayer(n, BLACK);
    tb.delete(tb.findIndex(1));
    const c = tb.get(2);                               // get(i): "the layer whose Index is i"
    assert(c && c.name === 'c' && c.index === 2);
    assert.strictEqual(tb.findIndex(1), null);         // RhinoCommon's indexer: null for a hole
    assert.strictEqual(tb.get(1), null);               // JS get(): the indexer's null for a hole
    // Python's t[1] / t[7] raise IndexError only because Python's sequence protocol needs it; JS
    // get(i) is RhinoCommon's indexer itself, which returns null past the end as for a hole
    assert.strictEqual(tb.get(7), null);
  });

  t('test_table_iteration_keeps_the_file_alive', async () => {
    const it = (() => {
      const f = new r.File3dm();
      f.allLayers.addLayer('only', BLACK);
      return f.allLayers.iterator();
    })();
    await ctx.gcSettle();
    const n = it.next();
    assert.strictEqual(n.done, false);
    const layer = n.value;
    if (typeof it.delete === 'function') it.delete();
    assert.strictEqual(layer.name, 'only');
  });

  // --- G40: File3dmObject ---------------------------------------------------

  const fileWithLine = () => {
    const f = new r.File3dm();
    f.objects.addLine(P(0, 0, 0), P(1, 0, 0), null);
    return f;
  };
  const first = coll => coll.iterator().next().value;

  t('test_objects_iterate_as_file3dmobject_proxies', () => {
    const f = fileWithLine();
    f.objects.addPoint(P(5, 5, 5), null);
    const objs = list(f.objects);
    assert.strictEqual(f.objects.count, 2);
    assert.strictEqual(objs.length, 2);
    assert(objs[0] instanceof r.File3dmObject);
    assert(objs[0].geometry instanceof r.LineCurve, 'geometry 0: ' + objs[0].geometry.constructor.name);
    assert(objs[1].geometry instanceof r.Point, 'geometry 1: ' + objs[1].geometry.constructor.name);
  });

  t('test_geometry_is_copy_on_write_as_in_rhinocommon', () => {
    const f = fileWithLine();
    const obj = first(f.objects);
    const g = obj.geometry;
    assert.strictEqual(g.pointAtStart.X, 0.0);
    assert.strictEqual(g.transform(r.Transform.translation({ X: 10, Y: 0, Z: 0 })), true);
    assert.strictEqual(g.pointAtStart.X, 10.0);        // the private copy moved ...
    assert.strictEqual(obj.geometry.pointAtStart.X, 0.0);   // ... the file did not
    f.objects.delete(obj.id);
    f.objects.addCurve(g, null);
    assert.deepStrictEqual(list(f.objects).map(o => o.geometry.pointAtStart.X), [10.0]);
  });

  t('test_attributes_write_through_and_name_chains_to_them', () => {
    const f = fileWithLine();
    const obj = first(f.objects);
    obj.attributes.name = 'via attributes';
    assert.strictEqual(obj.name, 'via attributes');
    obj.name = 'via name';
    assert.strictEqual(first(f.objects).attributes.name, 'via name');
  });

  t('test_geometry_view_of_a_deleted_object_raises', () => {
    const f = fileWithLine();
    const obj = first(f.objects);
    const g = obj.geometry;
    assert.strictEqual(g.pointAtStart.X, 0.0);
    f.objects.delete(obj.id);
    assert.strictEqual(f.objects.count, 0);
    ctx.raises(() => g.pointAtStart, /runtime_error,.*no longer in the File3dm/);
  });

  // --- G41: RenderSettings --------------------------------------------------

  t('test_render_settings_setters_commit_into_the_file', () => {
    const f = new r.File3dm();
    const rs = f.settings.renderSettings;
    const before = rs.scaleBackgroundToFit;
    rs.scaleBackgroundToFit = !before;
    assert.strictEqual(rs.scaleBackgroundToFit, !before);
    assert.strictEqual(f.settings.renderSettings.scaleBackgroundToFit, !before);
    rs.shadowmapLevel = 3;
    assert.strictEqual(f.settings.renderSettings.shadowmapLevel, 3);
  });

  t('test_render_settings_same_file_result_as_the_old_in_place_bindings', () => {
    // vNext copies and commits where the old bindings wrote in place: the file carries the values (the
    // same file the old bindings wrote: checked against them before the switch-over, M3)
    const nw = new r.File3dm();
    nw.settings.renderSettings.shadowmapLevel = 2;
    nw.settings.renderSettings.transparentBackground = true;
    const back = r.File3dm.fromByteArray(nw.toByteArray()).settings.renderSettings;
    assert.strictEqual(back.shadowmapLevel, 2);
    assert.strictEqual(back.transparentBackground, true);
  });

  t('test_render_settings_view_keeps_the_file_alive', () => {
    const f = new r.File3dm();
    const rs = f.settings.renderSettings;
    f.delete();
    rs.shadowmapLevel = 1;
    assert.strictEqual(rs.shadowmapLevel, 1);
  });

  // --- G42: RDK settings ----------------------------------------------------

  t('test_ground_plane_writes_through_into_the_file', () => {
    const f = new r.File3dm();
    const gp = f.settings.renderSettings.groundPlane;
    gp.enabled = true;
    gp.altitude = 2.5;
    gp.textureOffset = { X: 0.25, Y: 0.75 };
    const again = f.settings.renderSettings.groundPlane;
    assert.strictEqual(again.enabled, true);
    assert.strictEqual(again.altitude, 2.5);
    assert.deepStrictEqual([again.textureOffset.X, again.textureOffset.Y], [0.25, 0.75]);
  });

  t('test_free_floating_settings_own_their_object', () => {
    const gp = new r.GroundPlane();
    gp.altitude = 7.0;
    assert.strictEqual(gp.altitude, 7.0);
    const rs = new r.RenderSettings();
    const inner = rs.groundPlane;                       // a pointer INTO them, not a copy
    inner.altitude = 3.0;
    assert.strictEqual(rs.groundPlane.altitude, 3.0);
  });

  t('test_sun_and_safe_frame_round_trip', () => {
    const f = new r.File3dm();
    const rs = f.settings.renderSettings;
    rs.sun.enabled = true;
    rs.safeFrame.enabled = true;
    assert.strictEqual(f.settings.renderSettings.sun.enabled, true);
    assert.strictEqual(f.settings.renderSettings.safeFrame.enabled, true);
  });

  // --- G43: components ------------------------------------------------------

  const boxBrep = () => r.Brep.createFromBox(r.BoundingBox.create(0, 0, 0, 1, 2, 3));

  t('test_brep_faces_are_components_of_the_brep', () => {
    const b = boxBrep();
    const faces = b.faces;
    assert.strictEqual(faces.count, 6);
    assert.deepStrictEqual(seq(faces).map(f => f.faceIndex), [0, 1, 2, 3, 4, 5]);
    const f = faces.get(2);
    assert(f instanceof r.SurfaceProxy && f instanceof r.Surface);
    assert(f.isSurface && f.isPlanar(1e-9));
    ctx.raises(() => faces.get(6), /out_of_range/);
    const was = f.orientationIsReversed;
    f.orientationIsReversed = !was;
    assert.strictEqual(b.faces.get(2).orientationIsReversed, !was);
  });

  t('test_brep_component_keeps_its_brep_alive', async () => {
    const f = (() => boxBrep().faces.get(0))();
    await ctx.gcSettle();                               // the component keeps the brep alive
    assert.strictEqual(f.faceIndex, 0);
    assert.strictEqual(f.isSurface, true);
  });

  t('test_brep_surfaces_and_view_table_g103', async () => {
    const srfs0 = boxBrep().surfaces;
    assert.strictEqual(srfs0.count, 6);
    const items = [...srfs0];
    assert(items.length === 6 && items.every(s => s instanceof r.Surface && s.isPlanar()));
    ctx.raises(() => srfs0.get(6), /out_of_range/);
    const s0 = (() => boxBrep().surfaces.get(0))();
    await ctx.gcSettle();
    assert(s0.isPlanar());                                   // the view keeps its list and brep
    const f = ctx.readModel('file3dm_stuff.3dm');
    const n = f.views.count;
    assert(n > 0);
    assert.strictEqual(f.views.delete(0), true);
    assert.strictEqual(f.views.count, n - 1);
    ctx.raises(() => f.views.delete(99), /out_of_range/);
  });

  t('test_brep_edges_and_vertices', () => {
    const b = boxBrep();
    assert.strictEqual(b.edges.count, 12);
    assert.strictEqual(b.vertices.count, 8);
    const e = b.edges.get(3);
    assert(e.edgeIndex === 3 && e instanceof r.CurveProxy && e instanceof r.Curve);
    assert.strictEqual(b.vertices.get(7).vertexIndex, 7);
  });

  const subdFromFile = () => {
    const f = ctx.readModel('subdBox.3dm');
    for (const o of list(f.objects)) {
      const g = o.geometry;
      if (g instanceof r.SubD) return [f, g];
    }
    skip('no SubD in subdBox.3dm');
  };

  t('test_subd_lists_iterate_like_foreach', () => {
    const [, s] = subdFromFile();
    for (const lst of [s.faces, s.edges, s.vertices]) {
      const items = list(lst);
      assert(items.length === lst.count && lst.count > 0, `${items.length} vs ${lst.count}`);
    }
    const v = first(s.vertices);
    assert(v.edgeCount > 0 && v.faceCount > 0);
    let n = 0;
    for (let cur = v; cur !== null && cur !== undefined; cur = cur.next) n++;
    assert.strictEqual(n, s.vertices.count);
    const e = first(s.edges);
    assert(e.vertexFrom !== null && e.vertexTo !== null && e.faceCount >= 1);
  });

  t('test_subd_vertex_writes_land_in_the_subd', () => {
    const [, s] = subdFromFile();
    const v = first(s.vertices);
    v.controlNetPoint = P(10, 20, 30);
    assert.deepStrictEqual(first(s.vertices).controlNetPoint, P(10, 20, 30));
  });

  t('test_point_cloud_items_and_knot_lists', () => {
    const cloud = new r.PointCloud();
    for (let i = 0; i < 4; i++) cloud.add(P(i, 2 * i, 0));
    const item = cloud.get(2);
    assert.strictEqual(item.index, 2);
    assert.deepStrictEqual([item.location.X, item.location.Y], [2, 4]);
    item.location = P(9, 9, 9);
    assert.strictEqual(cloud.get(2).location.Z, 9);
    ctx.raises(() => cloud.get(4), /out_of_range,index must be smaller than the Number of points/);
    const ns = r.NurbsSurface.createFromSphere(r.Sphere.create(P(0, 0, 0), 2.0));
    const ku = ns.knotsU;
    assert(ku.count > 0 && typeof ku.clampedAtStart === 'boolean');
    assert(ns.knotsV.count > 0);
  });

  t('test_physically_based_material_is_a_conditional_child', () => {
    const m = new r.Material();
    assert.strictEqual(m.physicallyBased, null);       // `IsPhysicallyBased ? new T(this) : null`
    m.toPhysicallyBased();
    const pbr = m.physicallyBased;
    pbr.metallic = 0.25;
    assert.strictEqual(m.physicallyBased.metallic, 0.25);
  });
};
