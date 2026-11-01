// JS port of tests/generated/test_generated.py, part 4: from
// test_annotation_bounding_box_overrides_geometry_base to the end of the file (same names, same
// contracts, against the compiled WASM module). Python `x is y` on two wrappers of one native
// object is embind's `x.isAliasOf(y)` (embind hands out a new handle per return); Python's
// `del x; gc.collect()` keep-alive checks are `x.delete()` followed by the same use.
module.exports = (t, ctx) => {
  const { r, assert, approx } = ctx;
  const fs = require('fs');
  const path = require('path');
  const NIL = '00000000-0000-0000-0000-000000000000';
  const uuid4 = () => require('crypto').randomUUID();
  // ctx.list, plus a bare iterator object (ManifestTable.getEnumerator returns one: next() only)
  const list = c => {
    if (c && typeof c.next === 'function' && typeof c.iterator !== 'function') {
      const out = [];
      for (let n = c.next(); !n.done; n = c.next()) out.push(n.value);
      return out;
    }
    return ctx.list(c);
  };
  const I = () => r.Transform.identity;
  const bytesOf = name => new Uint8Array(fs.readFileSync(path.join(ctx.MODELS, name)));
  const objs = f => list(f.objects);

  const boxBrep = () => r.Brep.createFromBox(r.BoundingBox.create(0, 0, 0, 1, 2, 3));
  const subdFromFile = () => {
    const f = ctx.readModel('subdBox.3dm');
    for (const o of objs(f)) {
      const g = o.geometry;
      if (g instanceof r.SubD) return [f, g];
    }
    ctx.skip('no SubD in subdBox.3dm');
  };

  t('test_annotation_bounding_box_overrides_geometry_base', () => {
    if (!r.AnnotationBase || !r.AnnotationBase.prototype.getBoundingBox) ctx.skip('G89');
    const f = ctx.readModel('textEntities_r8.3dm');
    const ann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    assert(ann);
    ann.parentDimensionStyle = list(f.allDimStyles)[0];
    const bb = ann.getBoundingBox(I());
    // the base's member reaches the override (a virtual on the generated base)
    assert.deepStrictEqual(r.GeometryBase.prototype.getBoundingBox.call(ann, I()), bb);
    // WASM has no font glyph metrics: a text's box is empty -- in the old binding too (both of this
    // file's annotations had an invalid box there: frozen, M3)
    if (!r.BoundingBox.isValid(bb)) ctx.skip('WASM: text bounding boxes are empty without font metrics (the old binding\'s too); the virtual-dispatch part passed');
    assert.strictEqual(r.BoundingBox.isValid(bb), true);
    const moved = ann.getBoundingBox(r.Transform.translation({ X: 10, Y: 0, Z: 0 }));
    approx(moved.Min.X - bb.Min.X, 10, 1e-9, 'min.x');
    approx(moved.Max.X - bb.Max.X, 10, 1e-9, 'max.x');
  });

  t('test_render_content_find_and_variant_parameters', () => {
    if (!r.File3dmRenderContent || !r.File3dmRenderContent.prototype.getParameter) ctx.skip('G89');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const mats = list(f.renderMaterials);
    assert(mats.length > 0);
    const m = f.renderMaterials.find(mats[0].id);
    assert(m !== null && m !== undefined && m.id === mats[0].id);
    assert.strictEqual(f.renderMaterials.find(uuid4()), null);
    assert.strictEqual(m.getParameter('rh3dm-no-such-parameter'), null);
    // JS has no uuid type: a uuid is a string, so 'u' round-trips as a string variant; a
    // number is an int variant when Number.isInteger, else a double
    const gid = uuid4();
    const values = { s: 'hello', b: true, i: 7, d: 0.25, u: gid,
      p2: [1.0, 2.0], p3: [1.0, 2.0, 3.0], p4: [1.0, 2.0, 3.0, 4.0] };
    for (const [k, v] of Object.entries(values)) assert.strictEqual(m.setParameter('rh3dm-' + k, v), true, k);
    for (const [k, v] of Object.entries(values)) {
      const got = m.getParameter('rh3dm-' + k);
      assert.deepStrictEqual(got, v, k);
      assert.strictEqual(typeof got, typeof v, k);
      assert.strictEqual(Array.isArray(got), Array.isArray(v), k);
    }
    ctx.raises(() => m.setParameter('rh3dm-bad', {}), /invalid_argument,Variant: unsupported value type/);
  });

  const renderContentKid = () => {
    const f = ctx.readModel('file3dm_stuff.3dm');
    // the oracle: the old FirstChild / NextSibling chain of each top-level content, frozen (M3)
    const oldChains = { 'ab776f2b-7763-4c53-ac15-d02d3040ff26': [],
                        '660433b2-4c4f-459e-962c-8f70942c5ccd': ['f3785eac-0099-4df4-9666-81c5158011a9'],
                        '16421078-5d0e-6851-2d34-062d266c3949': ['63bae601-b151-4a2a-bf62-9e5df2dc5293'] };
    const tops = [...list(f.renderMaterials), ...list(f.renderEnvironments), ...list(f.renderTextures)];
    const withKids = tops.filter(c => c.children.length > 0);
    assert(withKids.length > 0);
    for (const top of tops) {
      assert.strictEqual(top.parent, null);
      assert(top.topLevel.isAliasOf(top), 'topLevel is `return this`');
      const chain = oldChains[String(top.id)];
      assert(chain, 'old content ' + top.id);
      const kids = top.children;
      assert(Array.isArray(kids));
      assert.deepStrictEqual(kids.map(c => String(c.id)), chain);
      for (const c of kids) {
        assert(c instanceof r.File3dmRenderContent && c.isChild && !c.isTopLevel);
        assert.strictEqual(c.parent.id, top.id);
        assert(c.parent.isAliasOf(c.parent), 'the copy it holds');
        assert.strictEqual(c.topLevel.id, top.id);
        const found = top.findChild(c.childSlotName);
        assert(found !== null && found.id === c.id);
      }
      assert.strictEqual(top.findChild('rh3dm-no-such-slot'), null);
    }
    return [withKids[0].children[0], withKids[0].children[0].parent.id];
  };
  t('test_render_content_parent_link_and_children', () => {
    if (!r.File3dmRenderContent || !r.File3dmRenderContent.prototype.findChild) ctx.skip('G89');
    renderContentKid();
  });
  t('test_render_content_child_keeps_the_file_alive', async () => {
    if (!r.File3dmRenderContent || !r.File3dmRenderContent.prototype.findChild) ctx.skip('G89');
    const [kid, pid] = renderContentKid();
    await ctx.gcSettle();
    assert.strictEqual(kid.parent.id, pid);                    // the file is kept alive
    assert(kid.name !== null && kid.name !== undefined);
  });

  t('test_subd_vertex_edges_iterator', () => {
    if (!r.BrepFace) ctx.skip('components not generated');
    if (!r.SubDVertex || !('edges' in r.SubDVertex.prototype)) ctx.skip('G89');
    const [f, s] = subdFromFile();
    const v = list(s.vertices)[0];
    const edges = v.edges;
    assert(Array.isArray(edges));
    assert(edges.length === v.edgeCount && v.edgeCount > 0);
    const byIndex = [];
    for (let i = 0; i < v.edgeCount; i++) byIndex.push(v.edgeAt(i).id);
    assert.deepStrictEqual(edges.map(e => e.id), byIndex);
    v.delete(); s.delete(); f.delete();
    assert(edges.every(e => e.id > 0));                         // each keeps the SubD alive
  });

  t('test_view_info_viewport_is_a_held_view', async () => {
    if (!r.ViewInfo || !('viewport' in r.ViewInfo.prototype)) ctx.skip('G89');
    let v = ctx.readModel('file3dm_stuff.3dm').views.get(0);
    const vp = v.viewport;
    assert(vp.isAliasOf(v.viewport), 'the held object, every time');
    // the camera the old binding read from the file (frozen, M3)
    const cl = vp.cameraLocation;
    assert.deepStrictEqual([cl.X, cl.Y, cl.Z], [-1039.436783184503, -313.98699366950444, 743.7751118904666]);
    assert.strictEqual(vp.setCameraLocation({ X: 1, Y: 2, Z: 3 }), true);
    const again = v.viewport.cameraLocation;
    assert.deepStrictEqual([again.X, again.Y, again.Z], [1, 2, 3]);   // the edit stays with the held view
    v = null;
    await ctx.gcSettle();
    assert.strictEqual(vp.cameraLocation.Z, 3);                // the view keeps its ViewInfo alive
  });

  t('test_try_convert_brep_views_and_copies', () => {
    if (!r.Brep || !r.Brep.tryConvertBrep) ctx.skip('G89');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const g = objs(f).map(o => o.geometry).find(x => x instanceof r.Brep);
    assert(g);
    const b = r.Brep.tryConvertBrep(g);
    assert(b instanceof r.Brep && !b.isAliasOf(g) && b.faces.count === g.faces.count);
    const x0 = g.getBoundingBox(I()).Min.X;
    assert.strictEqual(b.transform(r.Transform.translation({ X: 100, Y: 0, Z: 0 })), true);
    approx(b.getBoundingBox(I()).Min.X - x0, 100, 1e-6);
    assert.strictEqual(g.getBoundingBox(I()).Min.X, x0);        // the argument is untouched
    g.delete(); f.delete();
    assert(b.faces.count > 0);                                  // the view kept the file alive
    const own = boxBrep();                                      // owns its pointer: BrepForm
    const c = r.Brep.tryConvertBrep(own);
    assert(!c.isAliasOf(own) && c.faces.count === own.faces.count);
  });

  t('test_material_render_material_instance_id_u1', () => {
    const m = new r.Material();
    assert.strictEqual(m.renderMaterialInstanceId, '00000000-0000-0000-0000-000000000000');
    const uid = '3f2504e0-4f89-11d3-9a0c-0305e82c3301';
    m.renderMaterialInstanceId = uid;
    assert.strictEqual(m.renderMaterialInstanceId, uid);
    assert.strictEqual(m.renderPlugInId, '99999999-9999-9999-9999-999999999999');
  });

  t('test_subd_face_control_net_center_u3', () => {
    const [, s] = subdFromFile();
    const faces = [...s.faces];
    assert(faces.length > 0);
    for (const face of faces) {
      const n = face.vertexCount;
      const pts = Array.from({ length: n }, (_, i) => face.vertexAt(i).controlNetPoint);
      const c = face.controlNetCenterPoint;
      ['X', 'Y', 'Z'].forEach(k => ctx.approx(c[k], pts.reduce((a, p) => a + p[k], 0) / n, 1e-12, 'center ' + k));
      const v = face.controlNetCenterNormal;
      ctx.approx(Math.hypot(v.X, v.Y, v.Z), 1.0, 1e-12, 'unit normal');
      const fr = face.controlNetCenterFrame;
      assert.deepStrictEqual([fr.Origin.X, fr.Origin.Y, fr.Origin.Z], [c.X, c.Y, c.Z]);
      ['X', 'Y', 'Z'].forEach(k => ctx.approx(fr.ZAxis[k], v[k], 1e-12, 'frame normal ' + k));
    }
  });

  t('test_subd_sharp_edge_count_u2', () => {
    // U2: RhinoCommon's SharpEdgeCount() is a method (D60; the old binding a property)
    let total = 0;
    // the old binding's sharpEdgeCount property per SubD (frozen, M3)
    for (const [name, olds] of [['subd.3dm', [90]], ['subdBox.3dm', [0]], ['subd_creases.3dm', [3]]]) {
      const subds = objs(ctx.readModel(name)).map(o => o.geometry).filter(g => g instanceof r.SubD);
      assert.strictEqual(subds.length, olds.length);
      subds.forEach((s, k) => {
        const n = s.sharpEdgeCount();
        const edges = [...s.edges];
        assert.strictEqual(n, edges.filter(e => e.isSharp).length);
        assert.strictEqual(n, olds[k]);
        for (const e of edges) assert.deepStrictEqual(Object.keys(e.sharpness).sort(), ['sharpness0', 'sharpness1']);
        const errors = r.SubD.errorCount;
        assert.strictEqual(s.clearEdgeSharpness(), n);
        assert.strictEqual(s.sharpEdgeCount(), 0);
        assert.strictEqual(r.SubD.errorCount, errors);
        total += n;
      });
    }
    ctx.log && ctx.log('sharp edges in the SubD fixtures: ' + total);
  });

  t('test_subd_edge_sharpness_value_type_g109', () => {
    // D61: a plain object {sharpness0, sharpness1} (raw ends); create() is RhinoCommon's construction
    const S = r.SubDEdgeSharpness;
    const s = S.create(1, 2);
    assert.deepStrictEqual(s, { sharpness0: 1, sharpness1: 2 });
    assert.strictEqual(S.endSharpness(s, 1), 2);
    assert.strictEqual(S.isIncreasing(s), true);
    assert.deepStrictEqual(S.reversed(s), { sharpness0: 2, sharpness1: 1 });
    assert.strictEqual(S.isCrease(S.crease), true);
    assert.deepStrictEqual(S.create(5, 5), S.crease);
    assert.strictEqual(S.isNotValid(S.create(5, 0)), true);    // FromInterval couples the ends
    assert.deepStrictEqual(S.reversed({ sharpness0: 0.5, sharpness1: 1.5 }), { sharpness0: 1.5, sharpness1: 0.5 });
  });

  t('test_point_cloud_insert_range_u11', () => {
    // U11: the tail moves to index + count (pilot-findings #34); normals stay with their points
    const P = (x) => ({ X: x, Y: 0, Z: 0 });
    const range = (a, b) => Array.from({ length: b - a }, (_, j) => a + j);
    for (const [n, i, k] of [[4, 1, 1], [4, 0, 2], [4, 3, 1], [2, 0, 5], [5, 2, 3], [6, 1, 2], [3, 3, 2]]) {
      const pc = new r.PointCloud();
      pc.addRange(range(0, n).map(P), range(0, n).map(j => ({ X: 0, Y: 0, Z: j + 1 })));
      pc.insertRange(i, range(0, k).map(j => P(90 + j)));
      assert.deepStrictEqual(Array.from(pc.getPoints()).map(p => p.X), [...range(0, i), ...range(0, k).map(j => 90 + j), ...range(i, n)]);
      assert.deepStrictEqual(Array.from(pc.getNormals()).map(v => v.Z), [...range(0, i).map(j => j + 1), ...range(0, k).map(() => 0), ...range(i, n).map(j => j + 1)]);
      pc.delete();
    }
    const pc = new r.PointCloud();
    pc.addRange([P(0), P(1)]);
    ctx.raises(() => pc.insertRange(-1, [P(9)]), /larger than or equal to zero/);
    ctx.raises(() => pc.insertRange(3, [P(9)]), /smaller than or equal to Count/);
    pc.delete();
  });

  t('test_mesh_normal_list_is_mesh_vertex_normal_list', () => {
    // the old MeshNormalList is RhinoCommon's MeshVertexNormalList (registry 'old' pairing): the same
    // operations on the same mesh give the same normals, step by step (the old values frozen, M3)
    const folded = () => {
      const m = new r.Mesh();
      for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0], [2, 0, 1], [2, 1, 1]]) m.vertices.add(x, y, z);
      m.faces.addFace(0, 1, 2, 3);
      m.faces.addFace(1, 4, 5, 2);
      return m;
    };
    const xyz = v => [v.X, v.Y, v.Z];
    const vals = l => Array.from({ length: l.count }, (_, i) => xyz(l.get(i)));
    const a = 0.3826834261417389, b = 0.9238795638084412, c = 0.7071067690849304;
    const computed = [[0, 0, 1], [-a, 0, b], [-a, 0, b], [0, 0, 1], [-c, 0, c], [-c, 0, c]];
    const flipped = computed.map(v => v.map(x => -x));
    const neu = folded().normals;
    assert(neu.computeNormals());
    assert.strictEqual(neu.count, 6);
    assert.deepStrictEqual(vals(neu), computed);
    neu.flip();
    assert.deepStrictEqual(vals(neu), flipped);
    // D66: the old add unitized (ON_Mesh::SetVertexNormal: it stored [0, 0, 1]); RhinoCommon's stores
    // the vector as given
    assert.strictEqual(neu.add(0, 0, 2), 6);
    assert.deepStrictEqual(xyz(neu.get(6)), [0, 0, 2]);
    assert.deepStrictEqual(vals(neu).slice(0, 6), flipped);
    neu.clear();
    assert.strictEqual(neu.count, 0);
  });
  t('test_dimension_classes_and_display_lines_g111', () => {
    // triage batch B: generated dimension classes, handed out by files as their own class;
    // GetDisplayLines -> [rc, lines] (values by parity against .NET)
    const S = new r.DimensionStyle();
    const P = (x, y) => ({ X: x, Y: y, Z: 0 });
    const od = r.OrdinateDimension.create(S, r.Plane.worldXY, r.OrdinateDimensionMeasuredDirection.Xaxis, P(0, 0), P(5, 3), P(5, 8), 1.0, 1.0);
    const ld = r.LinearDimension.create(r.AnnotationType.Aligned, S, r.Plane.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0);
    const f = new r.File3dm();
    const ids = [f.objects.addOrdinateDimension(od), f.objects.addLinearDimension(ld)];
    const back = r.File3dm.fromByteArray(f.toByteArray(null));
    const geom = {};
    for (const o of objs(back)) geom[String(o.attributes.objectId)] = o.geometry;
    assert(geom[String(ids[0])] instanceof r.OrdinateDimension, 'ordinate: ' + Object.keys(geom).join(',') + ' vs ' + ids);
    assert(geom[String(ids[1])] instanceof r.LinearDimension);
    for (const d of [od, ld]) d.updateDimensionText(S, r.UnitSystem.Millimeters);
    const [orc] = od.getDisplayLines(S, 1.0);
    assert.strictEqual(typeof orc, 'boolean');
    const [trc] = ld.getTextRectangle();
    if (!trc) ctx.skip('WASM: no font metrics, so no text rectangle and no display lines (as for text bounding boxes); the class and file parts passed');
    const [rc, lines] = ld.getDisplayLines(S, 1.0);
    assert.strictEqual(rc, true);
    assert(lines.length > 0);
  });

  t('test_create_keeps_its_style_alive_g112', async () => {
    // G112: the result holds the style its Create stored, and the setter holds its value (GC-safe)
    const P = (x, y) => ({ X: x, Y: y, Z: 0 });
    let ld = r.LinearDimension.create(r.AnnotationType.Aligned, new r.DimensionStyle(), r.Plane.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0);
    await ctx.gcSettle();
    assert(ld.parentDimensionStyle.textHeight > 0);
    ld.parentDimensionStyle = new r.DimensionStyle();
    await ctx.gcSettle();
    assert(ld.parentDimensionStyle.textHeight > 0);
    assert.deepStrictEqual(r.rh3dmPostUnresolved, []);
  });

  t('test_user_strings_kept_where_rhino3dm_had_them_d62', () => {
    // D62: user strings on Viewport / RenderSettings / Bitmap / TextureMapping; getUserStrings pairs
    const f = new r.File3dm();
    const I = (a, b) => ({ T0: a, T1: b });
    for (const o of [new r.ViewportInfo(), f.settings.renderSettings, new r.Bitmap(),
                     r.TextureMapping.createPlaneMapping(r.Plane.worldXY, I(0, 1), I(0, 1), I(0, 1))]) {
      assert.strictEqual(o.userStringCount, 0);
      assert.strictEqual(o.setUserString('k', 'v'), true);
      assert.strictEqual(o.getUserString('k'), 'v');
      assert.deepStrictEqual(Array.from(o.getUserStrings(), p => Array.from(p)), [['k', 'v']]);
    }
  });

  t('test_render_content_child_slots_u4', () => {
    // #7 (U4): RenderContent's shape (slot name first); an unset slot's amount is 0.0 (D64)
    const f = ctx.readModel('file3dm_stuff.3dm');
    const tops = [...f.renderMaterials, ...f.renderEnvironments, ...f.renderTextures];
    const top = tops.find(c => [...c.children].length > 0);
    const slot = [...top.children][0].childSlotName;
    const n = [...top.children].length;
    const on = top.childSlotOn(slot);
    assert.strictEqual(top.setChildSlotOn(slot, !on), true);
    assert.strictEqual(top.childSlotOn(slot), !on);
    assert.strictEqual(top.setChildSlotAmount(slot, 42.0), true);
    assert.strictEqual(top.childSlotAmount(slot), 42.0);
    assert.strictEqual(top.childSlotAmount('rh3dm-no-such-slot'), 0.0);
    const other = tops.find(c => c.id !== top.id);
    assert.strictEqual(top.setChild(other, 'rh3dm-slot'), true);
    assert.strictEqual([...top.children].length, n + 1);       // ids: pilot #50
    assert(top.findChild('rh3dm-slot') !== null);
  });

  t('test_draw_color_from_a_file_u5', () => {
    // #5: by object and by material (the material the attributes select; D63)
    const f = new r.File3dm();
    const m = new r.Material(); m.diffuseColor = { r: 200, g: 0, b: 0, a: 255 }; f.materials.add(m);
    const a = new r.ObjectAttributes();
    a.colorSource = r.ObjectColorSource.ColorFromMaterial;
    a.materialSource = r.ObjectMaterialSource.MaterialFromObject; a.materialIndex = 0;
    const c = a.drawColor(f);
    assert.deepStrictEqual([c.r, c.g, c.b], [200, 0, 0]);
  });

  t('test_viewport_screen_port_g105', () => {
    const v = new r.ViewportInfo();
    v.screenPort = [10, 20, 640, 480];
    assert.deepStrictEqual(v.screenPort, [10, 20, 640, 480]);
    assert.deepStrictEqual(v.getScreenPort(), [10, 20, 640, 480]);
    assert.deepStrictEqual(v.getScreenPortLocation(), [10, 20, 650, 500]);
    assert.strictEqual(v.setScreenPort([0, 0, 100, 50], 1, 2), true);
    assert.deepStrictEqual(v.screenPort, [0, 0, 100, 50]);
    assert.strictEqual(v.setScreenPort(0, 640, 0, 480, 0, 0), true);   // y-up
    assert.deepStrictEqual(v.screenPort, [0, 0, 640, 480]);
    assert.strictEqual(v.setScreenPort(0, 640, 100, 100, 0, 0), false); // opennurbs refuses top == bottom
    assert.deepStrictEqual(v.screenPort, [0, 0, 640, 480]);
    ctx.raises(() => { v.screenPort = [1, 2, 3]; }, /rectangle/);
    // the old JS screenPort (retired for ScreenPort, Luis 2026-10-18): the same set, the same read
    // (the old ViewportInfo read [10, 20, 640, 480] back: frozen, M3)
    v.screenPort = [10, 20, 640, 480];
    assert.deepStrictEqual(v.screenPort, [10, 20, 640, 480]);
  });

  t('test_view_table_items_are_live_views_and_plugin_data_g104', async () => {
    const v = (() => {
      const f = ctx.readModel('file3dm_stuff.3dm');
      const views = [...f.views];
      assert(views.length === f.views.count && views.length > 0 && views.every(x => x instanceof r.ViewInfo));
      const v0 = f.views.get(0);
      v0.name = 'rh3dm-live';
      assert.strictEqual(f.views.get(0).name, 'rh3dm-live');   // write-through, not a copy
      return v0;
    })();
    await ctx.gcSettle();
    assert.strictEqual(v.name, 'rh3dm-live');                   // the view keeps its file
    assert.strictEqual(new r.ViewInfo().name, '');
    ctx.raises(() => ctx.readModel('file3dm_stuff.3dm').views.get(99), /out_of_range/);
    const g = ctx.readModel('file3dm_stuff.3dm');
    const pd = [...g.plugInData];
    assert.strictEqual(pd.length, g.plugInData.count);
    assert(pd.every(p => p instanceof r.File3dmPlugInData && /^[0-9a-f-]{36}$/i.test(p.plugInId)));
  });

  t('test_hand_bitmaps_add_find_iterate_round_trip', async () => {
    if (!r.Bitmap) ctx.skip('G90 hand bitmaps');
    const f = new r.File3dm();
    const paths = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png'];
    const ids = [];
    for (const p of paths) {
      const b = new r.Bitmap();
      b.fileFullPath = p;
      const added = f.bitmaps.add(b);
      assert(added !== null && added !== undefined && added.fileFullPath === p);
      ids.push(added.id);
    }
    assert(f.bitmaps.count === 3 && new Set(ids.map(String)).size === 3);
    assert.deepStrictEqual([0, 1, 2].map(i => f.bitmaps.findIndex(i).fileFullPath), paths);
    assert.strictEqual(f.bitmaps.get(1).fileFullPath, paths[1]);
    assert.strictEqual(f.bitmaps.findId(ids[1]).fileFullPath, paths[1]);
    assert.strictEqual(f.bitmaps.findId(uuid4()), null);
    assert.strictEqual(f.bitmaps.findIndex(999), null);
    // Python's Bitmaps[999] raises IndexError (Python sequence protocol); JS get(i) is RhinoCommon's
    // indexer (FindIndex), which returns null
    assert.strictEqual(f.bitmaps.get(999), null);
    ctx.raises(() => { f.bitmaps.findId(ids[0]).fileFullPath = '/tmp/other.png'; }, /runtime_error,Bitmap: the path of an image in a File3dm cannot be changed/);
    assert(f.bitmaps.delete(ids[0]) && f.bitmaps.count === 2);
    assert.deepStrictEqual(list(f.bitmaps).map(b => b.fileFullPath), paths.slice(1));   // no hole
    // round trip through the module's in-memory file system (no host FS in WASM)
    const out = '/tmp/rh3dm_bitmaps_' + process.pid + '.3dm';
    assert.strictEqual(f.write(out, null), true);
    const bm = (() => {
      const g = r.File3dm.read(out);
      assert.deepStrictEqual(list(g.bitmaps).map(b => b.fileFullPath).sort(), paths.slice(1).sort());
      return list(g.bitmaps)[0];
    })();
    await ctx.gcSettle();
    assert(paths.includes(bm.fileFullPath));                    // the view keeps the file alive
  });

  t('test_manifest_table_find_count_enumerate', async () => {
    if (!r.ManifestTable) ctx.skip('G91');
    let f = ctx.readModel('file3dm_stuff.3dm');
    const m = f.manifest;
    const T = r.ModelComponentType;
    assert(Number.isInteger(m.count) && m.count > 0);            // Python's len(m) is Count
    assert(Number(m.longCount) === m.count, 'longCount');          // a BigInt in JS (C# long)
    assert.strictEqual(m.componentType, T.Mixed);
    const layers = list(f.allLayers);
    assert(layers.length > 0 && m.activeObjectCount(T.Layer) === f.allLayers.count);
    const viaenum = list(m.getEnumerator(T.Layer));
    assert(viaenum.every(x => x instanceof r.Layer));
    assert.deepStrictEqual(viaenum.map(x => String(x.id)), layers.map(l => String(l.id)));
    const l0 = layers[0];
    for (const found of [m.findId(l0.id), m.findId(l0.id, T.Layer), m.findIndex(l0.index, T.Layer),
      m.findName(l0.name, T.Layer, NIL)]) {
      assert(found instanceof r.Layer && found.id === l0.id && found.name === l0.name);
    }
    assert.strictEqual(m.findId(uuid4()), null);
    const mats = list(m.getEnumerator(T.RenderMaterial));
    assert(mats.every(x => x instanceof r.Material));
    const os = list(m.getEnumerator(T.ModelGeometry));
    assert(os.every(x => x instanceof r.File3dmObject) && os.length === objs(f).length);
    ctx.raises(() => m.findIndex(0, T.Unset), /out_of_range,.*Parameter 'type'/);
    const unmade = [T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle].filter(x => m.activeObjectCount(x));
    if (unmade.length) {
      ctx.raises(() => list(m), /runtime_error/);
    } else {
      assert(list(m).every(x => x instanceof r.ModelComponent));
    }
    f = null;
    await ctx.gcSettle();
    assert.strictEqual(viaenum[0].name, l0.name);               // items keep the file alive
  });

  t('test_manifest_images_are_bitmapx', () => {
    if (!r.ManifestTable || !r.Bitmap) ctx.skip('G91');
    const f = new r.File3dm();
    const b = new r.Bitmap();
    b.fileFullPath = '/tmp/rh3dm_manifest.png';
    const added = f.bitmaps.add(b);
    const imgs = list(f.manifest.getEnumerator(r.ModelComponentType.Image));
    assert(imgs.length === 1 && imgs[0] instanceof r.Bitmap);
    assert(imgs[0].fileFullPath === '/tmp/rh3dm_manifest.png' && imgs[0].id === added.id);
    assert.strictEqual(f.manifest.findId(added.id).fileFullPath, '/tmp/rh3dm_manifest.png');
  });

  t('test_layer_full_path_and_name_override', () => {
    if (!r.Layer || !('fullPath' in r.Layer.prototype)) ctx.skip('G92');
    const f = new r.File3dm();
    const tb = f.allLayers;
    const black = { r: 0, g: 0, b: 0, a: 255 };
    tb.addLayer('top', black);
    const top = list(tb)[0];
    tb.addLayer('mid', black, top.id);
    const mid = list(tb).find(l => l.name === 'mid');
    tb.addLayer('leaf', black, mid.id);
    const leaf = list(tb).find(l => l.name === 'leaf');
    const sep = ctx.val(r.ModelComponent.namePathSeparator);
    assert.strictEqual(top.fullPath, 'top');
    assert.strictEqual(leaf.fullPath, ['top', 'mid', 'leaf'].join(sep));
    assert.strictEqual(leaf.toString(), leaf.fullPath);
    leaf.name = 'leaf2';                                         // the override keeps the base setter
    assert(list(tb).find(l => l.id === leaf.id).fullPath.endsWith(sep + 'leaf2'));
    assert.strictEqual(new r.Layer().fullPath, new r.Layer().name);   // not in a file: its name
  });

  t('test_group_members_and_find_by_group', async () => {
    if (!r.File3dmGroupTable || !r.File3dmGroupTable.prototype.groupMembers) ctx.skip('G92');
    let f = new r.File3dm();
    const g0 = f.allGroups.addGroup();
    const g1 = f.allGroups.addGroup();
    const add = groups => {
      const a = new r.ObjectAttributes();
      for (const g of groups) a.addToGroup(g);
      return f.objects.addPoint({ X: groups.length, Y: 0, Z: 0 }, a);
    };
    const ids = { none: add([]), g0: add([g0]), both: add([g0, g1]) };
    const m0 = f.allGroups.groupMembers(g0);
    assert(Array.isArray(m0));
    assert.deepStrictEqual(m0.map(o => String(o.id)).sort(), [ids.g0, ids.both].map(String).sort());
    assert.deepStrictEqual(f.allGroups.groupMembers(g1).map(o => String(o.id)), [String(ids.both)]);
    assert.deepStrictEqual(f.allGroups.groupMembers(99), []);
    const grp = f.allGroups.findIndex(g1);
    assert.deepStrictEqual(f.objects.findByGroup(grp).map(o => String(o.id)), [String(ids.both)]);
    f = null;
    await ctx.gcSettle();
    assert(m0[0].geometry !== null && m0[0].geometry !== undefined);   // the items keep the file alive
  });

  t('test_class_static_properties_are_properties', () => {
    // "a static PROPERTY is a property, not a method": G101 installs JS static getters
    // (class and value type alike: r.Point3d.unset), so nothing here is callable.
    assert(r.MeshingParameters.default instanceof r.MeshingParameters);
    assert.strictEqual(r.Material.maxShine, 255.0);
    assert.strictEqual(typeof r.ModelComponent.namePathSeparator, 'string');
    assert.notStrictEqual(r.ObjectAttributes.oCSMappingChannelId, undefined);
    assert(r.SubD.empty instanceof r.SubD);
    assert(typeof r.Point3d.unset === 'object' && r.Point3d.unset.X < -1e300);
    assert(typeof r.Plane.worldXY === 'object' && r.Plane.worldXY.ZAxis.Z === 1);
  });

  t('test_retired_arc_arccurve_brep_members', () => {
    // rhino3dm-only members and their RhinoCommon forms, on the same input; the old members' results
    // frozen at the switch-over (M3)
    const na = r.Arc.create({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 });
    approx(3.141592653589793, r.Arc.angle(na), 1e-12, 'Arc.AngleRadians -> Arc.Angle');
    // Brep.CreateFromBoundingBox -> Brep.CreateFromBox(BoundingBox) / CreateFromBox(Box)
    const bb = r.BoundingBox.create({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 });
    const nb = r.Brep.createFromBox(bb);
    assert.strictEqual(nb.faces.count, 6);                         // the old box: 6 faces too
    assert.strictEqual(r.Brep.createFromBox(r.Box.create(bb)).faces.count, 6);
    // BrepTrim.EdgeIndex / StartVertexIndex / EndVertexIndex -> trim.Edge.EdgeIndex, ...
    const key = a => a.join(',');
    const oldTrims = [[0, 0, 1], [0, 1, 0], [1, 1, 2], [1, 2, 1], [2, 2, 3], [2, 3, 2], [3, 0, 3], [3, 3, 0], [4, 4, 5],
                      [4, 5, 4], [5, 5, 6], [5, 6, 5], [6, 6, 7], [6, 7, 6], [7, 4, 7], [7, 7, 4], [8, 0, 4], [8, 4, 0],
                      [9, 1, 5], [9, 5, 1], [10, 2, 6], [10, 6, 2], [11, 3, 7], [11, 7, 3]];
    const newTrims = [];
    for (let i = 0; i < nb.trims.count; i++) {
      const tr = nb.trims.get(i);
      newTrims.push([tr.edge ? tr.edge.edgeIndex : -1, tr.startVertex.vertexIndex, tr.endVertex.vertexIndex]);
    }
    assert.deepStrictEqual(oldTrims.map(key).sort(), newTrims.map(key).sort());
    // BrepVertex.EdgeCount -> vertex.EdgeIndices().length (the old edgeCount: 3 on each vertex)
    const newCounts = [];
    for (let i = 0; i < nb.vertices.count; i++) newCounts.push(nb.vertices.get(i).edgeIndices().length);
    assert.deepStrictEqual(newCounts, [3, 3, 3, 3, 3, 3, 3, 3]);
    // ArcCurve.CreateFromArc / ...Params / CreateFromCircle / ...Params -> the ArcCurve constructors
    const same = (old, nc) => {                                     // old: [T0, T1, end x, y, z]
      const nd = nc.domain;
      approx(old[0], nd.T0, 1e-12, 'T0'); approx(old[1], nd.T1, 1e-12, 'T1');
      const b = nc.pointAtEnd;
      approx(old[2], b.X, 1e-9); approx(old[3], b.Y, 1e-9); approx(old[4], b.Z, 1e-9);
    };
    const circ = r.Circle.create(3.0);
    same([2, 5, 2, 0, 0], new r.ArcCurve(na, 2.0, 5.0));                          // createFromArcParams
    same([0, 3.141592653589793, 2, 0, 0], new r.ArcCurve(na));                    // createFromArc -> ArcCurve(Arc)
    same([0, 18.84955592153876, 3, 0, 0], new r.ArcCurve(circ));                  // createFromCircle -> ArcCurve(Circle)
    same([1, 4, 3, 0, 0], new r.ArcCurve(circ, 1.0, 4.0));                         // createFromCircleParams
  });
  t('test_retired_viewinfo_vector_sun_shutlining_members', () => {
    // the old members' results frozen at the switch-over (M3)
    const f = ctx.readModel('file3dm_stuff.3dm');
    assert.strictEqual(f.views.get(0).wallpaperFilename, '');               // ViewInfo.WallpaperName
    // Vector3d.DotProduct -> the * operator (old JS has no Vector3d class: the dot by hand)
    const dot = 1 * -4 + 2 * 5 + 3 * 0.5;
    assert.strictEqual(r.Vector3d.multiply({ X: 1, Y: 2, Z: 3 }, { X: -4, Y: 5, Z: 0.5 }), dot);
    const rgb = c => (Array.isArray(c) ? c.slice(0, 3) : [c.r !== undefined ? c.r : c.R, c.g !== undefined ? c.g : c.G, c.b !== undefined ? c.b : c.B]);
    // the old Sun.sunColorFromAltitude
    const oldSun = [[-10.0, [0, 0, 64]], [0.0, [255, 160, 24]], [5.0, [255, 197, 69]], [12.5, [255, 233, 162]],
                    [30.0, [251, 252, 255]], [60.0, [251, 252, 255]], [89.0, [251, 252, 255]]];
    for (const [alt, oc] of oldSun) {
      const nc = rgb(r.Sun.colorFromAltitude(alt));
      // RhinoCommon rounds 255*c to the nearest integer; the old binding truncated 255.5*c (D45)
      assert(oc.every((x, i) => Math.abs(x - nc[i]) <= 1), JSON.stringify([alt, oc, nc]));
    }
    // ShutLining.Curves -> GetCurves() ids + the id-keyed accessors, through a file
    const cid = uuid4();
    const wf = new r.File3dm();
    const attrs = new r.ObjectAttributes();
    const mm = attrs.file3dmMeshModifiers;
    assert(mm.thickening == null);
    assert(mm.createThickening() != null && mm.thickening != null);
    const sl0 = mm.createShutLining();
    assert.strictEqual(sl0.addCurve(cid), cid);
    sl0.setCurveRadius(cid, 2.5); sl0.setCurveProfile(cid, 3); sl0.setCurvePull(cid, true);
    sl0.setCurveIsBump(cid, true); sl0.setCurveEnabled(cid, false);
    wf.objects.addPoint({ X: 0, Y: 0, Z: 0 }, attrs);
    const f2 = r.File3dm.fromByteArray(wf.toByteArray());
    const g = objs(f2)[0];
    const sl = g.attributes.file3dmMeshModifiers.shutLining;
    if (sl === null || sl === undefined) {
      // no mesh modifier made through rhino3dm survives a write and read (pilot-findings #43, U29 in
      // opennurbs); the checks below run once rhino3dm's opennurbs has it
      ctx.skip('xfail pilot-findings #43: mesh modifiers are lost on write/read');
    }
    assert.deepStrictEqual(list(sl.getCurves()).map(String), [String(cid)]);
    assert.deepStrictEqual([sl.curveRadius(cid), sl.curveProfile(cid), sl.curvePull(cid), sl.curveIsBump(cid), sl.curveEnabled(cid)],
      [2.5, 3, true, true, false]);
  });
  t('test_class_sequence_parameters', () => {
    if (!r.Brep || !r.Brep.createTrimmedPlane) ctx.skip('G95');
    const circle = new r.ArcCurve(r.Circle.create(2.0));
    const W = r.Plane.worldXY;
    const one = r.Brep.createTrimmedPlane(W, circle);
    const many = r.Brep.createTrimmedPlane(W, [circle]);
    // the old binding's createTrimmedPlane: 1 face, 1 edge (frozen, M3)
    assert.strictEqual(one.faces.count, 1);
    assert.strictEqual(many.faces.count, 1);
    assert.strictEqual(one.edges.count, 1);
    // File3dmInstanceDefinitionTable.Add(name, description, basePoint, geometry, attributes)
    const f = new r.File3dm();
    const n0 = f.allInstanceDefinitions.count;
    const idx = f.allInstanceDefinitions.add('blk', 'desc', { X: 0, Y: 0, Z: 0 }, [circle], [new r.ObjectAttributes()]);
    assert(idx >= 0 && f.allInstanceDefinitions.count === n0 + 1);
    // File3dm.WriteMultipleObjects(path, geometry) -- the module's in-memory file system
    const out = '/tmp/rh3dm_multi_' + process.pid + '.3dm';
    assert.strictEqual(r.File3dm.writeMultipleObjects(out, [circle, one]), true);
    assert.strictEqual(objs(r.File3dm.read(out)).length, 2);
  });

  t('test_annotation_text_from_text_runs', () => {
    if (!r.AnnotationBase || !('textHasRtfFormatting' in r.AnnotationBase.prototype)) ctx.skip('G96 not generated');
    const f = ctx.readModel('textEntities_r8.3dm');
    // the old binding's plainText of each text entity (frozen, M3)
    const oldText = { 'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 'Hello World!',
                      'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 'Hello Cruel World!' };
    const nl = s => s.replace(/\r\n/g, '\n');
    let seen = 0;
    for (const o of objs(f)) {
      const g = o.geometry;
      if (!(g instanceof r.AnnotationBase)) continue;
      assert.strictEqual(g.text, g.plainText);
      assert.strictEqual(typeof g.textHasRtfFormatting, 'boolean');
      const oid = String(o.attributes.objectId);
      if (oid in oldText) {
        // a text entity: opennurbs' own plain text, newline for newline
        assert.strictEqual(nl(g.plainText), nl(oldText[oid]));
        seen++;
      }
    }
    assert(seen > 0);
    const ann = objs(f).find(o => String(o.attributes.objectId) in oldText).geometry;
    ann.plainText = 'one\ntwo';
    assert.strictEqual(ann.plainText, 'one\r\ntwo');
    assert.strictEqual(ann.textHasRtfFormatting, true);         // two text runs
    ann.text = 'single';
    assert.strictEqual(ann.plainText, 'single');
    assert.strictEqual(ann.textHasRtfFormatting, false);
  });

  t('test_texture_projection_mode_c_layer_enum', () => {
    if (!r.Texture || !('projectionMode' in r.Texture.prototype)) ctx.skip('G96 not generated');
    const tx = new r.Texture();
    const m = r.TextureProjectionModes;
    assert.strictEqual(tx.projectionMode, m.MappingChannel);
    tx.projectionMode = m.Wcs;
    assert.strictEqual(tx.projectionMode, m.Wcs);
  });

  t('test_sun_date_time', () => {
    if (!r.Sun || !r.Sun.prototype.getDateTime) ctx.skip('G97 not generated');
    // JS: a Date is an instant; local time is the process zone (Local), as .NET's
    const s = new r.Sun();
    const K = r.DateTimeKind;
    const when = new Date(2024, 5, 21, 14, 30, 15);
    s.setDateTime(when, K.Local);
    const got = s.getDateTime(K.Local);
    assert(got instanceof Date);
    assert.strictEqual(got.getTime(), when.getTime());
    assert(s.getDateTime(K.Utc) instanceof Date);
    ctx.raises(() => s.getDateTime(K.Unspecified), /invalid_argument,DateTimeKind must be specified/);
    const aware = new Date(Date.UTC(2024, 0, 2, 3, 4, 5));
    s.setDateTime(aware, K.Local);
    assert.strictEqual(s.getDateTime(K.Local).getTime(), aware.getTime());
    s.setPosition(when, 40.0, -3.5);                           // the obsolete overload: SetDateTime(Local)
    assert.deepStrictEqual([s.latitude, s.longitude, s.getDateTime(K.Local).getTime()], [40.0, -3.5, when.getTime()]);
    ctx.raises(() => s.setDateTime('2024-06-21', K.Local), /invalid_argument,a Date is required/);
    const a = r.Sun.altitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, false);
    assert(a >= -90.0 && a <= 90.0, String(a));
  });

  t('test_retired_sun_date_members', () => {
    if (!r.Sun || !r.Sun.prototype.getDateTime) ctx.skip('G97 not generated');
    // the old sun.year / month / day / hours are getDateTime(Local)'s fields -- the same ON_Sun
    // LocalDateTime (hours to the second; checked against the old setters before the switch-over, M3):
    // through a file and back
    const wf = new r.File3dm();
    wf.settings.renderSettings.sun.setDateTime(new Date(2023, 10, 5, 9, 45), r.DateTimeKind.Local);
    const f = r.File3dm.fromByteArray(wf.toByteArray());
    const d = f.settings.renderSettings.sun.getDateTime(r.DateTimeKind.Local);
    assert.deepStrictEqual([d.getFullYear(), d.getMonth() + 1, d.getDate()], [2023, 11, 5]);
    assert(Math.abs(d.getHours() + d.getMinutes() / 60 + d.getSeconds() / 3600 - 9.75) < 1 / 3600);
  });
  t('test_retired_texturemapping_members', () => {
    if (!r.TextureMapping || !('mappingType' in r.TextureMapping.prototype)) ctx.skip('G97 not generated');
    // the old IsPeriodic is mappingType in (Sphere, Cylinder); Reverse/Swap/TileTextureCoordinate
    // left-multiply uvwTransform, composed here on the generated object. The old members' results are
    // frozen (M3)
    const T = r.TextureMappingType;
    const Iv = { T0: 0, T1: 1 };
    const TM = r.TextureMapping;
    const news = [TM.createSurfaceParameterMapping(),
      TM.createPlaneMapping(r.Plane.worldXY, Iv, Iv, Iv),
      TM.createCylinderMapping(r.Cylinder.create(r.Circle.create(1.0), 2.0), true),
      TM.createCylinderMapping(r.Cylinder.create(r.Circle.create(1.0), 2.0), false),
      TM.createSphereMapping(r.Sphere.create({ X: 0, Y: 0, Z: 0 }, 2.0)),
      TM.createBoxMapping(r.Plane.worldXY, Iv, Iv, Iv, true)];
    const kinds = news.map(n => n.mappingType);
    assert.deepStrictEqual(kinds.map(k => k.value),
      [T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping].map(k => k.value));
    // the old [isPeriodic, requiresVertexNormals] of each
    const old = [[false, false], [false, false], [true, true], [true, false], [true, false], [false, true]];
    news.forEach((n, i) => {
      const k = kinds[i];
      assert.strictEqual(old[i][0], k === T.SphereMapping || k === T.CylinderMapping);
      // RequiresVertexNormals (D49): RhinoCommon's properties, as ON_TextureMapping decides it
      const derived = k !== T.SurfaceParameters && (n.projection === r.Projection.Ray || k === T.BoxMapping
        || (k === T.CylinderMapping && n.capped));
      assert.strictEqual(old[i][1], derived, String(k.value));
    });
    const composed = x => {
      const n = TM.createPlaneMapping(r.Plane.worldXY, Iv, Iv, Iv);
      n.uvwTransform = r.Transform.multiply(x, n.uvwTransform);
      return n.uvwTransform;
    };
    const matrix = m => { const a = []; for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) a.push(m['M' + i + j]); return a; };
    // the old reverse / swap / tileTextureCoordinate's uvwTransform on a plane mapping
    for (const [name, oldM, build] of [
      ['reverseTextureCoordinate', [1, 0, 0, 0, 0, -1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1], x => { x.M11 = -1.0; x.M13 = 1.0; }],
      ['swapTextureCoordinate', [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1], x => { x.M00 = 0.0; x.M22 = 0.0; x.M02 = 1.0; x.M20 = 1.0; }],
      ['tileTextureCoordinate', [3, 0, 0, 0.25, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1], x => { x.M00 = 3.0; x.M03 = 0.25; }]]) {
      const x = r.Transform.create(1.0);
      build(x);
      assert.deepStrictEqual(oldM, matrix(composed(x)), name);
    }
  });
  t('test_retired_rhino_only_pairings', () => {
    assert.strictEqual(ctx.val(r.ModelComponent.namePathSeparator), '::');
    const f = ctx.readModel('textEntities_r8.3dm');
    const ds = list(f.allDimStyles)[0];
    // the old getDimensionScale(first style) of each annotation (frozen, M3)
    const oldScale = { 'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 10.0, 'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 10.0 };
    let n = 0;
    for (const o of objs(f)) {
      const g = o.geometry;
      const oid = String(o.attributes.objectId);
      if (g instanceof r.AnnotationBase && oid in oldScale) {
        g.parentDimensionStyle = ds;
        assert.strictEqual(g.dimensionScale, oldScale[oid]);
        n++;
      }
    }
    assert(n > 0);
    // a static inherited from ModelComponent, reached through the derived class (last)
    assert.strictEqual(r.Layer.namePathSeparator, r.ModelComponent.namePathSeparator, 'Layer.namePathSeparator is not reachable on the derived class');
    assert.strictEqual(ctx.val(r.Layer.namePathSeparator), '::');
  });

  t('test_post_effect_collection_and_data', () => {
    if (!r.PostEffectCollection) ctx.skip('G98 not generated');
    // the oracle: the old PostEffect table, frozen (M3). Type (D50): opennurbs Unset 0, Early 1,
    // ToneMapping 2, Late 3; RhinoCommon Early 0, ToneMapping 1, Late 2 (an unset type reads as Early)
    const oldPe = { 'posteffects.3dm': [
      ['57baba9e-fc02-40d2-9682-5f61b9049eaa', 'Despeckle', true, true, 0], ['ce2f1c40-7f25-4c2d-b1d3-271e118d843b', 'Bloom', true, false, 0],
      ['7243f003-c4f1-415c-8228-35e5efaabd60', 'Glow', true, false, 0], ['b4d7b2f3-fa0d-47fb-8063-6869e96dbf35', 'Fog', true, false, 0],
      ['ab3b31ab-7d40-4a59-aa5c-5df833a81e8f', 'Depth of field', true, false, 0], ['f8e993bf-3407-4121-b71a-6876c2541cc7', 'Multiplier', true, false, 0],
      ['1ec87a84-f61f-43dc-b331-92d8b81ed468', 'Denoise', true, true, 0], ['acb8d258-c1d6-499d-aa23-02dcdea2b0a2', 'Clamp', false, false, 1],
      ['64e53275-0583-4ec4-9bde-b9413df25f08', 'Black/White point', false, false, 1], ['a51d4149-3685-463e-b32f-d9ac986b7c1b', 'Logarithmic', false, false, 1],
      ['ec302d96-402e-4244-bf25-5d0859ebf37c', 'Filmic', false, false, 1], ['4723b167-50a6-42ce-bba6-ce4f8013495b', 'False color', false, false, 1],
      ['c65eb6ba-ee66-4c48-9457-95f0936eca08', 'Hue/Saturation/Luminance', true, false, 2], ['0034b235-c351-4674-be3f-afec691187b3', 'Brightness/Contrast', true, false, 2],
      ['4398d28e-ed29-4974-9304-d56d0e61cc15', 'Noise', true, false, 2], ['38986982-3eab-4988-b8cb-32dd4ad0baa3', 'Gaussian blur', true, false, 2]],
      'file3dm_stuff.3dm': [], 'textEntities_r8.3dm': [], 'sphereDecals.3dm': [] };
    let seen = 0;
    for (const [name, old] of Object.entries(oldPe)) {
      const f = ctx.readModel(name);
      const coll = f.settings.renderSettings.postEffects;
      const nw = list(coll).map(p => [String(p.id), p.localName, p.on, p.shown, p.type.value]);
      assert.deepStrictEqual(nw, old, name);
      seen += nw.length;
      // the old table's findId / findIndex (triage H1: map) -> postEffectDataFromId / the i-th item
      const items = list(coll);
      old.forEach(([oid, oname], i) => {
        assert.strictEqual(oid, String(items[i].id), name + ' ' + i);
        assert.strictEqual(coll.postEffectDataFromId(oid).localName, oname);
      });
      const ghost = coll.postEffectDataFromId(uuid4());
      assert(ghost !== undefined);
      ctx.raises(() => ghost.localName, /runtime_error,.*no longer in its owner/);
      if (nw.length) {
        const d = coll.postEffectDataFromId(nw[0][0]);
        assert.strictEqual(String(d.id), nw[0][0]);
        const on = d.on;
        d.on = !on;                                              // writes go into the file's object
        assert.strictEqual(list(coll).find(p => String(p.id) === nw[0][0]).on, !on);
      }
    }
    assert.deepStrictEqual(list(new r.PostEffectCollection()), []);   // free-floating: empty
    assert(seen > 0);       // tests/models/posteffects.3dm (Luis 2026-10-29) carries post effects
  });

  t('test_font_is_managed_and_dimension_style_font', () => {
    if (!r.DimensionStyle || !('font' in r.DimensionStyle.prototype)) ctx.skip('G99 not generated');
    const f = ctx.readModel('textEntities_r8.3dm');
    // the old binding's font face of each style (frozen, M3)
    const oldFaces = { '3eb2bbaf-5018-485f-be99-568fd0918a84': 'Regular', '3ca66641-8cfc-447e-9ae9-ca6a21cfb828': 'Regular',
                       '6d104644-18b4-464c-a49c-dd4892fcbe39': 'Regular', '7f6f1c3a-f6a7-4c30-ae0e-44b5631d9c65': 'Regular' };
    for (const ds of list(f.allDimStyles)) assert.strictEqual(ds.font.faceName, oldFaces[String(ds.id)]);   // the same managed font
    const q = r.Font.fromQuartetProperties('Arial', false, false);
    assert(q === null || q === undefined || q.familyName);
    // G99 side effect: setters whose C argument is an opennurbs scoped enum
    const s2 = new r.DimensionStyle();
    s2.fitText = s2.fitText;
    s2.fitArrow = s2.fitArrow;
    // a managed font outlives the style it came from
    const s0 = new r.DimensionStyle();
    const viaStyle = s0.font;
    s0.delete();
    assert(viaStyle.familyName);
    // Font(familyName): RhinoCommon's one-string constructor (last: see the report)
    assert.strictEqual(r.Font.prototype.constructor.length >= 0, true);
    const a = new r.Font('Arial'), b = new r.Font('Arial');
    assert(a.familyName === b.familyName && a.description === b.description);
    a.delete();                                                  // dropping one frees nothing shared
    assert(b.familyName && new r.Font('Arial').description === b.description);
    const s = new r.DimensionStyle();
    s.font = new r.Font('Courier New');
    const got = s.font;
    s.delete();
    assert(got.familyName);                                       // managed: outlives the style
  });

  // ---- retirement batch 1 (Luis 2026-10-28): the JS halves of the Python tests of the same names
  const ev = v => (v && typeof v === 'object' && 'value' in v ? v.value : v);

  t('test_retired_annotation_explicit_parent_accessors_c', () => {
    // C (drop): getX(parentStyle) / setX(parentStyle, v) -> ann.parentDimensionStyle = style; ann.x (D39, D67).
    // The old getters' values, after each old setter, are frozen (M3)
    const f = ctx.readModel('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    assert(nann);
    assert.strictEqual(String(nann.dimensionStyleId), '6d104644-18b4-464c-a49c-dd4892fcbe39');
    const nstyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    assert(nstyle);
    nann.parentDimensionStyle = nstyle;
    assert.strictEqual(nann.textHeight, 10.0);                       // the override survives (G114)
    const old = { TextHeight: 10.0, MaskOffset: 0.8, MaskEnabled: false, MaskColorSource: 0, MaskFrame: 0,
                  DimensionLengthDisplay: 3, AlternateDimensionLengthDisplay: 7 };
    for (const [n, v] of Object.entries(old)) {
      const p = n[0].toLowerCase() + n.slice(1);
      assert.strictEqual(ev(nann[p]), v, n);
    }
    for (const [n, v] of [['TextHeight', 7.5], ['MaskOffset', 0.25], ['MaskEnabled', true]]) {
      const p = n[0].toLowerCase() + n.slice(1);
      nann[p] = v;
      assert.strictEqual(nann[p], v, n);                             // the old getter returned v
    }
    nann.dimensionScale = 2.0;
    assert.strictEqual(nann.dimensionScale, 2.0);                    // the old getDimensionScale: 2
  });
  t('test_retired_subd_counts_and_ids_g1', () => {
    // G1 (map): subd.edgeCount / faceCount / vertexCount -> edges / faces / vertices .count; index -> id
    // (the old counts and indices: 48 / 24 / 26, numbered from 1 -- frozen, M3)
    const [, n] = subdFromFile();
    const upTo = k => Array.from({ length: k }, (_, i) => i + 1);
    assert.deepStrictEqual([n.edges.count, n.faces.count, n.vertices.count], [48, 24, 26]);
    assert.deepStrictEqual([...n.faces].map(c => c.id).sort((a, b) => a - b), upTo(24));
    assert.deepStrictEqual([...n.edges].map(c => c.id).sort((a, b) => a - b), upTo(48));
    assert.deepStrictEqual([...n.vertices].map(c => c.id).sort((a, b) => a - b), upTo(26));
  });
  t('test_retired_likely_renames_l1_and_other_maps_m1', () => {
    // L1 / M1 (map): each old member against its RhinoCommon member (the old results frozen, M3)
    const nbox = boxBrep();
    assert.strictEqual(nbox.faces.get(0).outerLoop.trims.count, 4);                    // outerLoop.trimCount
    const pts = [[0, 0, 0], [4, 0, 0], [4, 4, 0]];
    const npl = new r.Polyline(pts.map(([X, Y, Z]) => ({ X, Y, Z })));
    assert.strictEqual(npl.count, 3);
    const nc = npl.closestPoint({ X: 5, Y: 1, Z: 0 });
    assert.deepStrictEqual([nc.X, nc.Y], [4, 1]);                                       // Polyline.closesPoint
    const nl = new r.Point3dList();
    nl.add(1.0, 2.0, 3.0);
    assert.deepStrictEqual([nl.get(0).X, nl.get(0).Z], [1, 3]);                         // Point3dList.append
    const npc = new r.PointCloud();
    npc.addPointValue({ X: 0, Y: 0, Z: 0 }, 2.5); npc.addPointValue({ X: 1, Y: 0, Z: 0 }, -1.0);
    assert.strictEqual(npc.containsPointValues, true);                                  // containsValues
    assert.deepStrictEqual(Array.from(npc.getPointValues()), [2.5, -1.0]);              // getValues
    const nsrf = r.NurbsSurface.createFromSphere(r.Sphere.create({ X: 0, Y: 0, Z: 0 }, 2));
    assert.deepStrictEqual([nsrf.knotsU.clampedAtStart, nsrf.knotsU.clampedAtEnd], [true, true]);   // isClampedStart / End
    const pi = Math.PI;
    assert.deepStrictEqual([...nsrf.knotsU], [0, 0, pi / 2, pi / 2, pi, pi, 4.71238898038469, 4.71238898038469, 2 * pi, 2 * pi]);   // toList
    assert.strictEqual(nsrf.makeNonRational(), true);                                   // points().makeNonRational
    const nm = new r.Material(); nm.toPhysicallyBased();
    assert.strictEqual(nm.isPhysicallyBased, true);                                     // physicallyBased.supported
    const ne = r.Extrusion.create(r.Circle.toNurbsCurve(r.Circle.create(2)), r.Plane.worldXY, 3.0, true);   // createWithPlane
    approx(3.0, ne.getBoundingBox(I()).Max.Z, 1e-9, 'extrusion height');
    const nmesh = new r.Mesh();
    assert.strictEqual(nmesh.vertices.add(0.1, 0.2, 0.3), 0);                           // vertices().addPoint3d
    assert.strictEqual(nmesh.vertices.point3dAt(0).X, 0.1);         // double precision kept, as addPoint3d did
    const nd = [];
    for (const o of objs(ctx.readModel('sphereDecals.3dm'))) nd.push(...list(o.attributes.decals));
    // the old horz/vertSweepStart/End and boundsMin/MaxU/V of each decal
    const od = [[[0, 0], [0, 0], [0.6, 0.6, 0.7, 0.7]],
                [[0, 0], [0, 0], [0.2, 0.2, 0.300000001490116, 0.300000001490116]]];
    assert.strictEqual(nd.length, od.length);
    od.forEach(([horz, vert, bounds], k) => {
      const b = nd[k];
      assert.deepStrictEqual(horz, Array.from(b.horzSweep()));
      assert.deepStrictEqual(vert, Array.from(b.vertSweep()));
      assert.deepStrictEqual(bounds, Array.from(b.getUVBounds()));
    });
    // DimensionStyle.Field: the generator registers DimensionStyleField (the old name) with all of
    // RhinoCommon's values (the old one had 105 of 119, `ArrowSize` for `Arrowsize`)
    const F = r.DimensionStyleField;
    assert.strictEqual(Object.keys(F.values).length, 119);
    assert.deepStrictEqual([F.Arrowsize.value, F.ZeroSuppress.value, F.ClippingArrowSize.value, F.Count.value], [5, 76, 119, 122]);
    const nds = new r.DimensionStyle(); nds.setFieldOverride(F.TextHeight);
    assert(nds.isFieldOverriden(F.TextHeight) && !nds.isFieldOverriden(F.Arrowsize));
  });
  t('test_retired_tight_bounding_box_l1', () => {
    // L1 (map): getTightBoundingBox -> getBoundingBox(true); G115: a virtual override's JS
    // registration no longer hides the base's overloads
    const nb = boxBrep().getBoundingBox(true);
    assert.deepStrictEqual([nb.Min.X, nb.Max.Y, nb.Max.Z], [0, 2, 3]);              // the old box: [0, 0, 0] - [1, 2, 3]
    const f = ctx.readModel('textEntities_r8.3dm');
    const ann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    ann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === ann.dimensionStyleId);
    assert.deepStrictEqual(ann.getBoundingBox(true), ann.getBoundingBox(I()));       // both reach the annotation's box
    assert(ann.getBoundingBox(r.Plane.worldXY));                                      // the plane overload too
    const pc = new r.PolyCurve();
    pc.append(new r.LineCurve({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }));
    const d = pc.duplicate();
    assert(d instanceof r.PolyCurve && d.segmentCount === 1);                         // through Curve's registration
  });

  t('test_annotation_font_and_held_back_c_members_g115', () => {
    // G115: AnnotationBase.font (a managed font); C's getFont / setFont(style, font) -> font
    // (the old getFont / setFont results frozen, M3)
    const f = ctx.readModel('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    nann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    const nf = nann.font;
    assert.deepStrictEqual([nf.familyName, nf.faceName, nf.bold], ['Avenir', 'Black', true]);
    assert(nann.firstCharFont.familyName);
    nann.font = new r.Font('Courier New');
    assert.strictEqual(nann.font.familyName, 'Courier New');
    ctx.raises(() => { nann.font = null; });
  });
  t('test_file3dm_earth_anchor_point_g115', () => {
    // G115: File3dm.earthAnchorPoint, a copy out and a copy in; the old settings.earthAnchorPoint agreed
    // (0, 0 on this file: frozen, M3)
    const f = ctx.readModel('file3dm_stuff.3dm');
    const a = f.earthAnchorPoint;
    assert.deepStrictEqual([a.earthBasepointLatitude, a.earthBasepointLongitude], [0, 0]);
    a.earthBasepointLatitude = 12.5;
    assert.strictEqual(f.earthAnchorPoint.earthBasepointLatitude, 0);   // a copy
    f.earthAnchorPoint = a;
    assert.strictEqual(r.File3dm.fromByteArray(f.toByteArray(null)).earthAnchorPoint.earthBasepointLatitude, 12.5);
  });
  t('test_translated_setters_beside_planned_getters_g115', () => {
    // G115: MeshVertexList.count is settable (D1: setCount -> count)
    const m = new r.Mesh();
    for (let i = 0; i < 4; i++) m.vertices.add(i, 0, 0);
    m.vertices.count = 2;                                   // the old setCount(2): 2 vertices
    assert.strictEqual(m.vertices.count, 2);
    m.vertices.count = -1;
    assert.strictEqual(m.vertices.count, 2);
    const v = new r.ViewportInfo();
    v.isPerspectiveProjection = false;
    assert.strictEqual(v.isParallelProjection, true);
  });

  t('test_retired_file3dm_tables_and_settings_h1', () => {
    // H1 (map): renderContent table -> renderMaterials / Environments / Textures (+ find); settings
    // modelBasePoint / earthAnchorPoint; renderEnvironments.* -> RenderSettings members; addObject -> add
    // (the old results on this file frozen, M3)
    const f = ctx.readModel('file3dm_stuff.3dm');
    // the old renderContent table in index order (its findId(id) found each)
    const oids = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd', '16421078-5d0e-6851-2d34-062d266c3949'];
    const kinds = [f.renderMaterials, f.renderEnvironments, f.renderTextures];
    assert.deepStrictEqual(oids.slice().sort(), kinds.flatMap(k => list(k).map(c => c.id)).sort());
    for (const id of oids) {
      const hit = kinds.map(k => k.find(id)).filter(x => x !== null && x !== undefined);
      assert(hit.length === 1 && hit[0].id === id);
    }
    oids.forEach((id, i) => {
      const nc = f.manifest.findIndex(i, r.ModelComponentType.RenderContent);   // FindIndex -> the manifest's
      assert(nc instanceof r.File3dmRenderContent && nc.id === id);
    });
    const nb = f.settings.modelBasepoint;
    assert.deepStrictEqual([nb.X, nb.Y, nb.Z], [0, 0, 0]);                       // the old modelBasePoint
    assert.strictEqual(f.earthAnchorPoint.earthBasepointLongitude, 0);           // the old earthAnchorPoint's
    const U = r.RenderSettingsEnvironmentUsage, Pp = r.RenderSettingsEnvironmentPurpose;
    const rs = f.settings.renderSettings;
    // the old renderEnvironments.backgroundId / reflectionId / skylightingId, reflection / skylightingOverride
    for (const [n, u] of [['background', U.Background], ['reflection', U.Reflection], ['skylighting', U.Skylighting]])
      assert.strictEqual(rs.renderEnvironmentId(u, Pp.Standard), '16421078-5d0e-6851-2d34-062d266c3949', n);
    for (const [n, u] of [['reflection', U.Reflection], ['skylighting', U.Skylighting]])
      assert.strictEqual(rs.renderEnvironmentOverride(u), true, n);
    const eid = uuid4();
    rs.setRenderEnvironmentId(U.Skylighting, eid); rs.setRenderEnvironmentOverride(U.Skylighting, true);
    const brs = r.File3dm.fromByteArray(f.toByteArray()).settings.renderSettings;
    assert.strictEqual(brs.renderEnvironmentId(U.Skylighting, Pp.Standard), eid);
    assert.strictEqual(brs.renderEnvironmentOverride(U.Skylighting), true);
    const m0 = list(f.objects).length;
    const no = list(f.objects)[0];
    f.objects.add(no);                                // addObject(obj) -> add(File3dmObject): one more, as the old one
    assert.strictEqual(list(f.objects).length, m0 + 1);
    const nid = f.objects.add(no.geometry, null);     // Add(geometry, attributes) (G117), attributes optional
    assert(f.objects.findId(nid).geometry instanceof r.GeometryBase);
    // a leader too: RhinoCommon's Add throws for it, rhino3dm keeps 8.x's hand path (G119, D71)
    const S = new r.DimensionStyle();
    const le = r.Leader.create('x', r.Plane.worldXY, S, [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }]);
    const lid = f.objects.add(le, null);
    assert(f.objects.findId(lid).geometry instanceof r.Leader);
  });

  t('test_retired_mesh_modifiers_i1_and_shut_lining_i3', () => {
    // I1: attributes.meshModifiers -> file3dmMeshModifiers; I3: the shut-lining curve table -> id-based
    // members. Checked against the old table before the switch-over (M3); here on the generated side alone
    const na = new r.ObjectAttributes();
    const sl = na.file3dmMeshModifiers.createShutLining();
    sl.on = true;
    const cid = uuid4();
    assert.strictEqual(sl.addCurve(cid), cid);
    const nsl = na.file3dmMeshModifiers.shutLining;
    assert.strictEqual(nsl.on, true);
    assert.deepStrictEqual(list(nsl.getCurves()), [cid]);
    nsl.setCurveRadius(cid, 0.75); nsl.setCurveProfile(cid, 2); nsl.setCurveEnabled(cid, true);
    nsl.setCurvePull(cid, true); nsl.setCurveIsBump(cid, true);
    assert.deepStrictEqual([nsl.curveRadius(cid), nsl.curveProfile(cid), nsl.curveEnabled(cid), nsl.curvePull(cid), nsl.curveIsBump(cid)],
                           [0.75, 2, true, true, true]);
    nsl.setCurveRadius(cid, 1.25);
    assert.strictEqual(nsl.curveRadius(cid), 1.25);
  });
  t('test_retired_annotation_geometry_j1', () => {
    // J1 (map): leader.points -> points3D; dim.points (an object of named points) -> get3dPoints()
    // (an array led by the success flag: D68); DimAngular.angle -> numericValue
    const P = (X, Y, Z) => ({ X, Y, Z });
    const f = new r.File3dm();
    f.allDimStyles.add(new r.DimensionStyle());
    const style = list(f.allDimStyles)[0];
    const W = r.Plane.worldXY;
    const geos = [
      r.LinearDimension.create(r.AnnotationType.Aligned, style, W, P(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0),
      r.AngularDimension.create(style.id, W, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0)),
      r.RadialDimension.create(style, r.AnnotationType.Radius, W, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)),
      r.Leader.create('note', W, style, [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])];
    f.objects.add(geos[0], null);                      // RhinoCommon's add(geometry, attributes) takes these two (G117)
    f.objects.add(geos[1], null);
    f.objects.addRadialDimension(geos[2]);
    f.objects.addLeader('note', W, [{ X: 0, Y: 0 }, { X: 2, Y: 1 }, { X: 4, Y: 1 }], null);
    assert.strictEqual(list(geos[3].points3D)[2].X, 4);   // Create's own points
    const data = f.toByteArray();
    // the old members' results on the same file, frozen (M3)
    const news = list(r.File3dm.fromByteArray(data).objects).map(o => o.geometry);
    const rnd = v => Math.round(v * 1e9) / 1e9;
    const xyz = q => [rnd(q.X), rnd(q.Y), rnd(q.Z)];
    const ld = news.find(g => g instanceof r.Leader);
    assert.deepStrictEqual(list(ld.points3D).map(xyz), [[0, 0, 0], [2, 1, 0], [4, 1, 0]]);     // the old leader.points
    // the old dim.points (defpt1, defpt2, arrowpt1, arrowpt2 / centerpt, defpt1, defpt2, arrowpt1, arrowpt2, dimlinept, textpt)
    const keys = [[r.LinearDimension, [[0, 0, 0], [4, 1, 0], [0, 3, 0], [4, 3, 0]]],
                  [r.AngularDimension, [[0, 4, 0], [1, 0, 0], [0, 1, 0], [0.766964989, 0.932140045, 0], [0, 0.83772234, 0],
                                         [2.445086463, 6.005380809, 0], [-0.386377405, 7.138584474, 0]]]];
    for (const [N, op] of keys) {
      const n = news.find(g => g instanceof N);
      const rc = n.get3dPoints();
      assert.strictEqual(rc[0], true);
      op.forEach((p, i) => assert.deepStrictEqual(p, xyz(rc[i + 1]), String(i)));
    }
    const an = news.find(g => g instanceof r.AngularDimension);
    approx(6.038206644052722, an.numericValue, 1e-12, 'angle');                                  // the old DimAngular.angle
  });

  t('test_retired_set_point3d_at_d2', () => {
    // D2 (rename): vertices.setPoint3dAt(i, p) -> setVertex(i, Point3d)
    const nm = new r.Mesh();
    // a coordinate float cannot hold picks Add(double, ..), as C# does for a double: the mesh keeps
    // double precision, as the Python test's Add(float(i), ..) does (whole numbers pick Add(float, ..))
    for (let i = 0; i < 3; i++) nm.vertices.add(i + 0.1, 0, 0);
    nm.vertices.setVertex(1, { X: 0.1, Y: 0.2, Z: 0.3 });
    const n = nm.vertices.point3dAt(1);
    assert.deepStrictEqual([n.X, n.Y, n.Z], [0.1, 0.2, 0.3]);       // as the old setPoint3dAt stored it (frozen, M3)
  });
  t('test_retired_rhino3dm_only_overloads_from_the_audit', () => {
    // Luis-accepted maps from the overload audit (the JS halves; AddPoint(x, y, z, attrs) was Python-only).
    // The old overloads' results frozen (M3)
    const f = ctx.readModel('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    nann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    assert.strictEqual(nann.runReplace('ZZ', 0, 0, 0, 1), true);          // runReplace(style, ...)
    assert.strictEqual(nann.plainText, 'ZZllo World!');
    const nb = boxBrep().getBoundingBox(false);                     // getBoundingBox() -> (false)
    assert.deepStrictEqual([nb.Min.X, nb.Max.Y, nb.Max.Z], [0, 2, 3]);
    const nm = new r.Material(), nt = new r.Texture();
    nt.fileName = '/tmp/bump.png';
    nt.textureType = r.TextureType.Bump;
    assert.strictEqual(nm.setTexture(nt, nt.textureType), true);  // setTexture(t) -> (t, t.textureType)
    assert.strictEqual(nm.getTexture(r.TextureType.Bump).fileName, '/tmp/bump.png');
    // SubDEdge.endSharpness(i) -> endSharpness(i, false) (the old one-argument form's values)
    const ns = objs(ctx.readModel('subd_creases.3dm')).map(o => o.geometry).find(g => g instanceof r.SubD);
    const nes = [...ns.edges].sort((a, b) => a.id - b.id);
    assert.deepStrictEqual(nes.map(e => [e.endSharpness(0, false), e.endSharpness(1, false)]),
      [[0, 0], [0, 0], [0, 0], [0, 0], [1, 1], [0, 0], [3, 3], [2.5, 0.5], [0, 0], [0, 0], [0, 0], [0, 0]]);
  });
  t('test_retired_mesh_modifier_creators_i2_and_add_curve_rh99854', () => {
    // RH-99854: File3dmMeshModifiers.create*() and File3dmShutLining.addCurve(id) (in memory: pilot #43)
    const na = new r.ObjectAttributes(), nmm = na.file3dmMeshModifiers;
    assert(nmm.shutLining === null || nmm.shutLining === undefined);
    for (const n of ['Displacement', 'EdgeSoftening', 'Thickening', 'CurvePiping', 'ShutLining']) {
      const m = nmm['create' + n]();
      m.on = true;
      assert.strictEqual(nmm['create' + n]().on, true, n);       // an existing one is kept
    }
    const a = uuid4(), b = uuid4(), sl = nmm.shutLining;
    assert.strictEqual(sl.addCurve(a), a); assert.strictEqual(sl.addCurve(b), b); assert.strictEqual(sl.addCurve(a), a);
    assert.deepStrictEqual(list(sl.getCurves()), [a, b]);
    sl.setCurveRadius(a, 2.0);
    assert.deepStrictEqual([sl.curveRadius(a), sl.curveRadius(b)], [2.0, 1.0]);
  });

  t('test_retired_create_from_subd_control_net_flag', () => {
    // createFromSubDControlNet(subd, flag) -> createFromSubDControlNet / ...WithTextureCoordinates (Luis 2026-10-29);
    // the old member's [vertices, faces, texture coordinates] for flag false, true -- frozen (M3)
    const old = { 'subd.3dm': [[738, 279, 0], [2485, 868, 0]], 'subdBox.3dm': [[47, 24, 0], [96, 24, 96]],
                  'subd_creases.3dm': [[15, 6, 0], [24, 6, 24]] };
    for (const [name, counts] of Object.entries(old)) {
      const ns = objs(ctx.readModel(name)).map(o => o.geometry).find(g => g instanceof r.SubD);
      [s => r.Mesh.createFromSubDControlNet(s), s => r.Mesh.createFromSubDControlNetWithTextureCoordinates(s)].forEach((make, k) => {
        const n = make(ns);
        assert.deepStrictEqual([n.vertices.count, n.faces.count, n.textureCoordinates.count], counts[k], name + ' ' + (k === 1));
      });
    }
  });
  t('test_retired_file3dm_to_byte_array_options_js3', () => {
    // JS3 (map): File3dm.toByteArrayOptions(options) -> File3dm.toByteArray(options) (D54)
    const f = ctx.readModel('file3dm_stuff.3dm');
    const no = new r.File3dmWriteOptions(); no.version = 7;
    const nb = r.File3dm.fromByteArray(f.toByteArray(no));
    // the old toByteArrayOptions (version 7) of this file read back as archive 70, 22 objects (frozen, M3)
    assert.strictEqual(nb.archiveVersion, 70);
    assert.strictEqual(list(nb.objects).length, 22);
  });
  // --- RH-99868 / RH-99870 (rhino 9.x da1b2185e6c) ---
  const stat = (C, n) => (typeof C[n] === 'function' ? C[n]() : C[n]);   // a class static: a getter (post-js) or a function
  const p3 = (p) => (Array.isArray(p) ? [p[0], p[1], p[2]] : [p.X, p.Y, p.Z]);
  // the old bindings' answers, frozen at the switch-over (M3): tests/generated/old_oracle/<name>.json
  const oracle = name => JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'old_oracle', name), 'utf8'));
  const nanp = p => p3(p).map(c => (Number.isNaN(c) ? 'nan' : c));             // the oracles write an unset coordinate as "nan"

  t('test_subd_component_queries_rh99868', () => {
    // the old binding's answers, component by component (by id), frozen in old_oracle/subd_queries_rh99868.json
    // (M3; faces q: HasEdges, HasSharpEdges, SharpEdgeCount, MaximumEdgeSharpness, IsConvex, IsPlanar(1e-8),
    // IsPlanar(1.0), MaterialChannelIndex, TexturePointsAreSet, TexturePointsCapacity; vertices q: IsSmooth,
    // IsCrease, IsCorner, IsDart, IsSharp(true), IsSharp(false), VertexSharpness; edges q: IsSmooth, IsCrease,
    // IsHardCrease, IsDartCrease, DartCount)
    const fixtures = oracle('subd_queries_rh99868.json').fixtures;
    let checked = 0;
    for (const [name, olds] of Object.entries(fixtures)) {
      const ns = objs(ctx.readModel(name)).map(o => o.geometry).filter(g => g instanceof r.SubD);
      assert(ns.length === olds.length && ns.length > 0);
      ns.forEach((s, k) => {
        const old = olds[k];
        const byId = (a) => a.sort((x, y) => x.id - y.id);
        const of = old.faces.slice().sort((x, y) => x.id - y.id), nf = byId([...s.faces]);
        assert.deepStrictEqual(nf.map(n => n.id), of.map(o => o.id));
        of.forEach((o, i) => {
          const n = nf[i], q = o.q;
          assert.deepStrictEqual([n.hasEdges, n.hasSharpEdges, n.sharpEdgeCount, n.maximumEdgeSharpness, n.isConvex, n.isPlanar(1e-8),
                                  n.materialChannelIndex, n.texturePointsAreSet, n.texturePointsCapacity, nanp(n.subdivisionPoint)],
                                 [q[0], q[1], q[2], q[3], q[4], q[5], q[7], q[8], q[9], o.sub]);
        });
        const ov = old.vertices.slice().sort((x, y) => x.id - y.id), nv = byId([...s.vertices]);
        assert.deepStrictEqual(nv.map(n => n.id), ov.map(o => o.id));
        ov.forEach((o, i) => {
          const n = nv[i], q = o.q;
          assert.deepStrictEqual([n.isSmooth, n.isCrease, n.isCorner, n.isDart, n.isSharp(true), n.vertexSharpness],
                                 [q[0], q[1], q[2], q[3], q[4], q[6]]);
        });
        const oe = old.edges.slice().sort((x, y) => x.id - y.id), ne = byId([...s.edges]);
        assert.deepStrictEqual(ne.map(n => n.id), oe.map(o => o.id));
        oe.forEach((o, i) => {
          const n = ne[i];
          assert.deepStrictEqual([n.isSmooth, n.isCrease, n.isHardCrease, n.isDartCrease, n.dartCount, nanp(n.subdivisionPoint), nanp(n.controlNetCenterPoint)],
                                 [...o.q, o.sub, o.cc]);
        });
        checked += of.length + ov.length + oe.length;
      });
    }
    assert(checked > 100);
  });
  t('test_linetype_builtins_and_clear_pattern_rh99870', () => {
    // the old binding's built-ins [name, index, segmentCount] (8.x JS: `dashdot`), frozen (M3)
    const old = [['Continuous', 'Continuous', -1, 1], ['ByLayer', 'By Layer', -2, 1], ['ByParent', 'By Parent', -3, 1],
                 ['Hidden', 'Hidden', -4, 2], ['Dashed', 'Dashed', -5, 2], ['DashDot', 'DashDot', -6, 4],
                 ['Center', 'Center', -7, 4], ['Border', 'Border', -8, 6], ['Dots', 'Dots', -9, 2]];
    old.forEach(([name, ...o], i) => {
      const js = name[0].toLowerCase() + name.slice(1);
      const n = stat(r.Linetype, js);
      assert(n instanceof r.Linetype, name);
      assert.deepStrictEqual([n.name, n.index, n.segmentCount], o, name);
      assert.strictEqual(n.index, -(i + 1));
    });
    const lt = new r.Linetype();
    lt.appendSegment(1.0, true); lt.appendSegment(0.5, false);
    assert.strictEqual(lt.clearPattern(), true); assert.strictEqual(lt.segmentCount, 0);
  });
  t('test_material_instance_settings_sun_rh99870', () => {
    const a = new r.Material(), b = new r.Material();
    a.name = 'one'; b.name = 'two';
    assert.strictEqual(r.Material.compareAppearance(a, b), 0);
    const f = new r.File3dm();
    const circle = new r.ArcCurve(r.Circle.create(1.0));
    const idx = f.allInstanceDefinitions.add('blk', 'desc', { X: 0, Y: 0, Z: 0 }, [circle], [new r.ObjectAttributes()]);
    const idef = f.allInstanceDefinitions.findIndex(idx);
    assert(idef instanceof r.InstanceDefinitionGeometry);
    const ids = list(idef.getObjectIds());
    assert.strictEqual(ids.length, 1);
    assert.strictEqual(idef.isInstanceGeometryId(ids[0]), true);
    assert.strictEqual(idef.isInstanceGeometryId('00000000-0000-0000-0000-000000000000'), false);   // U33 (pilot #54)
    const S = r.ObjectLinetypeSource;
    assert.strictEqual(f.settings.currentLinetypeSource, S.LinetypeFromLayer);
    f.settings.currentLinetypeSource = S.LinetypeFromObject;
    assert.strictEqual(f.settings.currentLinetypeSource, S.LinetypeFromObject);
    const sun = f.settings.renderSettings.sun;
    assert.deepStrictEqual([stat(r.Sun, 'minYear'), stat(r.Sun, 'maxYear')], [1971, 2199]);
    assert.strictEqual(sun.isValid, true);
    const ea = sun.enableAllowed; sun.enableAllowed = !ea; assert.strictEqual(sun.enableAllowed, !ea);
    const lw = f.settings.renderSettings.linearWorkflow;
    const on = lw.preProcessGammaOn; lw.preProcessGammaOn = !on; assert.strictEqual(lw.preProcessGammaOn, !on);
    assert.strictEqual(new r.ViewportInfo().isValidCameraFrame, true);
  });
  t('test_resolvers_viewport_defaults_arrowhead_g122', () => {
    const f = new r.File3dm();
    const a = new r.ObjectAttributes();
    const lt = f.allLinetypes.fromAttributes(a);
    assert(lt instanceof r.Linetype);
    assert.deepStrictEqual([lt.name, lt.index], ['Continuous', -1]);
    assert.strictEqual(f.allLinetypes.fromLayerIndex(0).name, 'Continuous');
    const m = f.allMaterials.findFromAttributes(a);
    assert(m instanceof r.Material);
    assert.strictEqual(String(m.id).toLowerCase(), 'cf2be1de-b81c-4d69-9418-dde91d266086');
    const mat = new r.Material(); mat.name = 'red';
    const mi = f.allMaterials.addMaterial(mat);
    a.materialSource = r.ObjectMaterialSource.MaterialFromObject; a.materialIndex = mi;
    assert.strictEqual(f.allMaterials.findFromAttributes(a).name, 'red');
    const xy = (p) => (Array.isArray(p) ? [p[0], p[1]] : [p.X, p.Y]);
    const top = stat(r.ViewportInfo, 'defaultTop'), persp = stat(r.ViewportInfo, 'defaultPerspective');
    // the old ViewportInfo.defaultTop() / defaultPerspective() camera direction, frozen (M3)
    assert.deepStrictEqual(p3(top.cameraDirection), [-0, -0, -1]);
    assert.deepStrictEqual(p3(persp.cameraDirection), [-0.4330127018922193, 0.75, -0.5]);
    const pts = list(r.Arrowhead.getPoints(r.DimensionStyleArrowType.SolidTriangle)).map(xy);
    assert.deepStrictEqual(pts, [[0, 0], [-1, 0.25], [-1, -0.25]]);           // the old getPoints(.., 1.0) too
  });
  t('test_embedded_files_find_index_u35', () => {
    // U35 (pin 5b68aa2623e): File3dmEmbeddedFiles.findIndex -- .NET threw for every index before (pilot #56)
    const f = new r.File3dm();
    // no host file in WASM: a file in the module's own file system to embed (any bytes do)
    const path = '/tmp/part4_u35_note.3dm';
    assert.strictEqual(new r.File3dm().write(path, 7), true);
    assert.strictEqual(f.embeddedFiles.add(path), true);
    assert.strictEqual(f.embeddedFiles.count, 1);
    const ef = f.embeddedFiles.findIndex(0);
    assert(ef instanceof r.File3dmEmbeddedFile);
    assert.strictEqual(ef.filename, ctx.list(f.embeddedFiles)[0].filename);
    assert(ef.length > 0);
    assert(f.embeddedFiles.findIndex(5) === null || f.embeddedFiles.findIndex(5) === undefined);
  });
  t('test_retired_subd_control_net_g3', () => {
    // triage G3 (map): edge controlNetPoint(i) / controlNetDirection / vertexId(i) -> vertexFrom / vertexTo / controlNetLine
    // (the old binding's answers frozen in old_oracle/subd_controlnet_g3.json, M3)
    for (const [name, old] of Object.entries(oracle('subd_controlnet_g3.json').fixtures)) {
      const ns = objs(ctx.readModel(name)).map(o => o.geometry).find(g => g instanceof r.SubD);
      const ne = [...ns.edges].sort((x, y) => x.id - y.id);
      assert.strictEqual(ne.length, old.edges.length);
      old.edges.forEach((o, i) => {
        const n = ne[i], line = n.controlNetLine;
        assert.deepStrictEqual([nanp(line.From), nanp(line.To)], o.p);
        assert.deepStrictEqual([n.vertexFrom.id, n.vertexTo.id], o.v);
        assert.deepStrictEqual(nanp(n.vertexTo.controlNetPoint), o.p[1]);
      });
    }
  });
  t('test_retired_decal_table_find_index_follows_rhinocommon', () => {
    // Luis: follow RhinoCommon -- count / get(i) / findIndex(i) by iterating attributes.decals (visible decals)
    const nd = [...objs(ctx.readModel('sphereDecals.3dm'))[0].attributes.decals];
    // the old table's count and findIndex(i).textureInstanceId, frozen (M3)
    assert.deepStrictEqual(nd.map(d => String(d.textureInstanceId)), ['84eda676-69e1-4fa5-b81e-412676e88b31', '7339f997-0433-4591-811f-3cf66b6d41fe']);
  });
  // the old MeshingParameters().encode() (and toJSON), frozen (M3)
  const OLD_MP = { TextureRange: 2, JaggedSeams: false, RefineGrid: true, SimplePlanes: false, ComputeCurvature: false,
    ClosedObjectPostProcess: false, GridMinCount: 0, GridMaxCount: 0, GridAngle: 0.3490658503988659, GridAspectRatio: 6,
    GridAmplification: 1, Tolerance: 0, MinimumTolerance: 0, RelativeTolerance: 0, MinimumEdgeLength: 0.0001,
    MaximumEdgeLength: 0, RefineAngle: 0.3490658503988659 };
  // the old Sphere([1, 2, 3], 4).encode() (and toJSON), frozen (M3)
  const OLD_SPHERE = { Radius: 4, EquatorialPlane: { Origin: { X: 1, Y: 2, Z: 3 }, XAxis: { X: 1, Y: 0, Z: 0 },
    YAxis: { X: 0, Y: 1, Z: 0 }, ZAxis: { X: 0, Y: 0, Z: 1 } } };
  t('test_hand_json_bounding_box_and_meshing_parameters_g123', () => {
    // F1 hand members (G123: binding-agnostic, src/hand/hand_json.*): the old bindings' JSON form, key for key
    // (the old forms frozen, M3); on a value type a member is a class function taking the value (as the generated ones)
    const nb = r.BoundingBox.create(1, 2, 3, 4, 5.5, 6);
    const oldEnc = { Min: { X: 1, Y: 2, Z: 3 }, Max: { X: 4, Y: 5.5, Z: 6 } };
    assert.deepStrictEqual(r.BoundingBox.encode(nb), oldEnc);
    const back = r.BoundingBox.decode(oldEnc);
    assert.deepStrictEqual([back.Min.X, back.Min.Y, back.Min.Z, back.Max.X, back.Max.Y, back.Max.Z], [1, 2, 3, 4, 5.5, 6]);
    ctx.raises(() => r.BoundingBox.decode({ Min: { X: 0, Y: 0, Z: 0 } }), /missing key: Max/);

    assert.deepStrictEqual(new r.MeshingParameters().encode(), OLD_MP);
    const mp = new r.MeshingParameters();
    mp.jaggedSeams = true; mp.refineGrid = false; mp.gridMinCount = 17; mp.gridMaxCount = 300;
    mp.tolerance = 0.25; mp.minimumEdgeLength = 0.01; mp.maximumEdgeLength = 12.5; mp.refineAngle = 0.3;
    const d = mp.encode();
    assert.deepStrictEqual([d.JaggedSeams, d.RefineGrid, d.GridMinCount, d.GridMaxCount], [true, false, 17, 300]);
    assert.deepStrictEqual([d.Tolerance, d.MinimumEdgeLength, d.MaximumEdgeLength, d.RefineAngle], [0.25, 0.01, 12.5, 0.3]);
    assert.deepStrictEqual(Object.keys(d).sort(), Object.keys(OLD_MP).sort());
    const nback = r.MeshingParameters.decode(d);
    assert(nback instanceof r.MeshingParameters);
    assert.deepStrictEqual(nback.encode(), d);
    // (the old JS decode read every key too -- only Python's ignored them: pilot #57)
  });
  t('test_hand_json_value_types_f2', () => {
    // F2 hand members (G123): the value types' encode / decode (Python's old dicts; JS had only BoundingBox, Sphere)
    const P = (x, y, z) => r.Point3d.create(x, y, z);
    assert.deepStrictEqual(r.Point3d.encode(P(1, 2, 3)), { X: 1, Y: 2, Z: 3 });
    assert.deepStrictEqual(r.Point4d.encode(r.Point4d.create(1, 2, 3, 4)), { X: 1, Y: 2, Z: 3, W: 4 });
    assert.deepStrictEqual(r.Vector2d.encode(r.Vector2d.create(1, 2)), { X: 1, Y: 2 });
    const pl = r.Plane.create(P(1, 2, 3), r.Vector3d.create(1, 1, 1));
    assert.deepStrictEqual(r.Plane.encode(r.Plane.decode(r.Plane.encode(pl))), r.Plane.encode(pl));
    const sp = r.Sphere.create(pl, 4);
    assert.deepStrictEqual(r.Sphere.encode(r.Sphere.create(P(1, 2, 3), 4)), OLD_SPHERE);
    // the whole plane comes back (pilot #59: the old JS decode kept only the plane's origin -- a tilted
    // sphere came back world-aligned)
    assert.deepStrictEqual(r.Sphere.encode(r.Sphere.decode(r.Sphere.encode(sp))), r.Sphere.encode(sp));
    assert.strictEqual(r.Sphere.decode(OLD_SPHERE).Radius, 4);           // and it reads the old form
    assert.deepStrictEqual(Object.keys(r.Circle.encode(r.Circle.create(2.5))), ['radius', 'plane']);
  });
  t('test_hand_json_common_object_and_file3dm_f1', () => {
    // F1 hand members (G123): CommonObject / File3dm encode, decode -- the old forms. The old binding's
    // encode of this object is frozen in old_oracle/json_forms_f1_f4.json (M3): the generated decode reads
    // it, and the generated encode writes the same keys (the bytes carry opennurbs' version, so the
    // objects, not the bytes, are compared)
    const forms = oracle('json_forms_f1_f4.json');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const g = objs(f)[0].geometry;
    const d = g.encode();
    const old = forms.common_object_encode;
    assert.deepStrictEqual(Object.keys(d).sort(), ['archive3dm', 'data', 'opennurbs', 'version']);
    assert.deepStrictEqual(Object.keys(old).sort(), Object.keys(d).sort());
    assert.deepStrictEqual([d.archive3dm, d.version], [old.archive3dm, old.version]);
    assert.strictEqual(d.archive3dm, 60);
    const back = r.CommonObject.decode(d);
    assert.strictEqual(back.constructor.name, g.constructor.name);
    assert.strictEqual(g.constructor.name, forms.common_object_type);      // the most derived class
    assert.deepStrictEqual(back.encode(), d);
    const fromOld = r.CommonObject.decode(old);
    assert.strictEqual(fromOld.constructor.name, g.constructor.name);
    const box = x => { const b = x.getBoundingBox(false); return [b.Min.X, b.Min.Y, b.Min.Z, b.Max.X, b.Max.Y, b.Max.Z].map(c => Math.round(c * 1e9) / 1e9); };
    assert.deepStrictEqual(box(fromOld), box(g));
    // File3dm: base64 of the model's bytes (any 3dm, the old binding's included)
    const s = f.encode();
    assert(typeof s === 'string' && s.length > 1000);
    assert.strictEqual(objs(r.File3dm.decode(s)).length, objs(f).length);
    assert.strictEqual(objs(r.File3dm.decode(Buffer.from(bytesOf('file3dm_stuff.3dm')).toString('base64'))).length, objs(f).length);
    const opts = new r.File3dmWriteOptions();
    opts.version = 7;
    assert.strictEqual(objs(r.File3dm.decode(f.encode(opts))).length, objs(f).length);
  });
  t('test_hand_rdk_xml_and_embedded_files_f3_f4', () => {
    // F3 rdkXml (model, object attributes, plug-in data entry); F4 getEmbeddedFileAsBase64, EmbeddedFile.read
    // (the old binding's strings, by sha256, frozen: M3)
    const sha = s => require('crypto').createHash('sha256').update(s).digest('hex');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const rdk = '9f8be1e4d91bee9b901a1d0d0a72e454bf6045bd4c6075e295b4c95e0370e6ba';
    assert.strictEqual(sha(f.rdkXml()), rdk);
    assert(f.rdkXml().startsWith('<xml>'));
    assert.deepStrictEqual(list(f.plugInData).map(p => p.rdkXml()).filter(x => x).map(sha), [rdk]);   // the old plugInData().get(0).rdkXml()
    const na = objs(ctx.readModel('sphereDecals.3dm'))[0].attributes;
    assert.strictEqual(sha(na.rdkXml()), 'be505faefb52c2faacf3dbf1b766542bbdb034017bd6f349edb0e8e326f200b8');
    assert(na.rdkXml().length > 0);
    const p = '/Users/luis.fraguada/Dropbox/screenshots_MBPM1/hops.png';             // the old embeddedFilePaths()[0]
    const b = f.getEmbeddedFileAsBase64(p);
    assert.strictEqual(sha(b), '157d84ea10981b7d76ec4dd1e2298a1e3ab672937135ced3ae40f287d7c35723');
    assert.strictEqual(b.length, 196824);
    const name = p.split('/').pop();
    assert.strictEqual(f.getEmbeddedFileAsBase64(name.toUpperCase()), b);
    assert.strictEqual(f.getEmbeddedFileAsBase64Strict(name, true), '');           // strict: the full path (as the old one)
    assert.strictEqual(f.getEmbeddedFileAsBase64Strict(p, true), b);
    // no host file system in WASM: a file in the module's own
    assert.strictEqual(new r.File3dm().write('/tmp/part4_f4.3dm', 7), true);
    const ef = r.File3dmEmbeddedFile.read('/tmp/part4_f4.3dm');
    assert(ef instanceof r.File3dmEmbeddedFile && ef.length > 0);
    assert.strictEqual(r.File3dmEmbeddedFile.read('/tmp/missing_f4.txt'), null);
  });
  t('test_hand_archivable_dictionary_f4', () => {
    const g = objs(ctx.readModel('file3dm_stuff.3dm'))[0].geometry;
    const enc = r.ArchivableDictionary.encodeDict({ b: true, f: 2.5, s: 'héllo', g: g, gl: [g, g] });
    const back = r.ArchivableDictionary.decodeDict(enc);
    assert.deepStrictEqual([back.b, back.f, back.s], [true, 2.5, 'héllo']);
    assert.strictEqual(back.g.constructor.name, g.constructor.name);
    assert.deepStrictEqual(back.g.encode(), g.encode());
    assert.deepStrictEqual(back.gl.map(x => x.constructor.name), [g.constructor.name, g.constructor.name]);
    // (the old decodeDict read the generated one's b, f, s; pilot #58 (JS): it could not return a geometry
    // list at all -- an unregistered vector type)
    // the new one reads the old one's: its encodeDict({i: 3, p: Point3d(4, 5, 6), gl: [g]}), frozen (M3)
    const back2 = r.ArchivableDictionary.decodeDict(oracle('json_forms_f1_f4.json').archivable_dictionary_encode);
    assert.deepStrictEqual([back2.i, back2.p.X, back2.p.Y, back2.p.Z, back2.gl.length], [3, 4, 5, 6, 1]);
    assert.strictEqual(back2.gl[0].constructor.name, g.constructor.name);
    ctx.raises(() => r.ArchivableDictionary.encodeDict({ x: {} }), /Unable to serialize 'x'/);
  });

  t('test_hand_render_content_add_g124', () => {
    // File3dmRenderContentTable.add (hand-kept) -> add on the kind's collection, a copy as before
    const src = ctx.readModel('file3dm_stuff.3dm'), f = new r.File3dm();
    const pairs = [[src.renderMaterials, f.renderMaterials], [src.renderEnvironments, f.renderEnvironments],
                   [src.renderTextures, f.renderTextures]];
    for (const [have, to] of pairs) for (const c of list(have)) to.add(c);
    const got = pairs.flatMap(([, to]) => list(to).map(c => `${c.id} ${c.kind}`)).sort();
    // the old add of each top-level content: `id kind`, frozen (M3)
    const want = ['16421078-5d0e-6851-2d34-062d266c3949 environment', '660433b2-4c4f-459e-962c-8f70942c5ccd material',
                  'ab776f2b-7763-4c53-ac15-d02d3040ff26 material'];
    assert(got.length > 0);
    assert.deepStrictEqual(got, want);
    const m = list(src.renderMaterials)[0];
    f.renderMaterials.add(m);
    const ids = list(f.renderMaterials).map(c => c.id);
    assert.strictEqual(new Set(ids).size, list(src.renderMaterials).length + 1);
    const back = r.File3dm.fromByteArray(f.toByteArray(null));
    assert.deepStrictEqual(list(back.renderMaterials).map(c => c.id).sort(), ids.slice().sort());
  });

  t('test_hand_js_only_members_js2_g124', () => {
    // JS2: toJSON kept on CommonObject / MeshingParameters (over encode; JSON.stringify calls it)
    const J = v => JSON.parse(JSON.stringify(v));
    // (the old toJSON() results frozen, M3: the old CommonObject's is its encode, in old_oracle/json_forms_f1_f4.json --
    // compared as test_hand_json_common_object_and_file3dm_f1 does: the bytes carry opennurbs' version)
    const g = objs(ctx.readModel('file3dm_stuff.3dm'))[0].geometry;
    const og = oracle('json_forms_f1_f4.json').common_object_encode;
    assert.deepStrictEqual(J(g), J(g.encode()));
    assert.deepStrictEqual(Object.keys(J(g)).sort(), Object.keys(og).sort());
    assert.deepStrictEqual([J(g).archive3dm, J(g).version], [og.archive3dm, og.version]);
    const mp = new r.MeshingParameters();
    assert.deepStrictEqual(J(mp), J(mp.encode()));
    assert.deepStrictEqual(J(mp), OLD_MP);
    // JS2: dropped on the value types -- a plain object stringifies to the old toJSON() dict
    assert.deepStrictEqual(J(r.BoundingBox.create(r.Point3d.create(0, 1, 2), r.Point3d.create(3, 4, 5))),
                           { Min: { X: 0, Y: 1, Z: 2 }, Max: { X: 3, Y: 4, Z: 5 } });
    assert.deepStrictEqual(J(r.Sphere.create(r.Point3d.create(1, 2, 3), 4)), OLD_SPHERE);
    // EmbeddedFile.fromByteArray (kept): the bytes become the embedded file's content
    const bytes = new Uint8Array(Array.from({ length: 300 }, (_, i) => i % 251));
    const ne = r.File3dmEmbeddedFile.fromByteArray(bytes);
    assert(ne instanceof r.File3dmEmbeddedFile);
    assert.strictEqual(ne.length, BigInt(300));                       // the old EmbeddedFile's length too
    // JS3: encodeOptions(options) -> encode(options): the same archive (the old encodeOptions, version 7:
    // archive 70, 22 objects -- frozen, M3)
    const src = ctx.readModel('file3dm_stuff.3dm');
    const no = new r.File3dmWriteOptions();
    no.version = 7;
    const nb = r.File3dm.decode(src.encode(no));
    assert.deepStrictEqual([nb.archiveVersion, objs(nb).length], [70, 22]);
    // File3dm.destroy (kept): embind's delete() under the old name; a later use throws
    const f = new r.File3dm();
    f.destroy();
    assert.throws(() => f.objects, /deleted/);
  });

  t('test_centermark_get_display_lines_u12_g127', () => {
    // U12 (RH-99905): generated from RhinoCommon; the same centermark read back by the old binding
    if (!r.Centermark) ctx.skip('G127');
    const cm = new r.Centermark();
    cm.adjustFromPoints(r.Plane.worldXY, { X: 1, Y: 2, Z: 0 });
    cm.radius = 3.0;
    const f = new r.File3dm();
    f.objects.add(cm, null);
    // the old binding read it back as a Centermark with these lines (frozen, M3)
    const bytes = f.toByteArray(null);
    const neu = objs(r.File3dm.fromByteArray(bytes))[0].geometry;
    assert(neu instanceof r.Centermark);
    const [ok, lines] = neu.getDisplayLines(new r.DimensionStyle(), 1.0);
    const R = x => Math.round(x * 1e9) / 1e9;
    const key = l => [R(l.From.X), R(l.From.Y), R(l.To.X), R(l.To.Y)].join(',');
    assert(ok);
    assert.deepStrictEqual(Array.from(lines).map(key).sort(), ['0.5,2,1.5,2', '1,1.5,1,2.5']);
  });

  t('test_mesh_vertex_color_add_rgb_g128', () => {
    // MeshVertexColorList.add(r, g, b) / setColor(index, r, g, b): generated (Color.FromArgb(r, g, b), G128)
    const neu = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1]]) neu.vertices.add(x, y, 0);
    assert.deepStrictEqual([neu.vertexColors.add(255, 0, 0), neu.vertexColors.add(0, 128, 0)], [0, 1]);   // as the old add(r, g, b)
    assert.strictEqual(neu.vertexColors.setColor(2, 0, 0, 255), true);
    // the colors the old add(r, g, b) stored for the same three (frozen, M3)
    assert.deepStrictEqual(Array.from(neu.vertexColors.toARGBArray()).map(c => c >>> 0), [0xFFFF0000, 0xFF008000, 0xFF0000FF]);
    ctx.raises(() => neu.vertexColors.add(300, 0, 0), /Value of '300' is not valid for 'red'/);
  });

  t('test_subd_neighbour_lists_g129', () => {
    // G129 (option 1): neighbour lists of the generated components, against the old binding's iterators
    // (by id, in order: frozen in old_oracle/subd_neighbours_g129.json, M3)
    const newSubD = objs(ctx.readModel('subdBox.3dm')).map(o => o.geometry).find(g => g instanceof r.SubD);
    const old = oracle('subd_neighbours_g129.json')['subdBox.3dm'];
    let checked = 0;
    for (const [comps, olds, props] of [[newSubD.faces, old.faces, ['edges', 'vertices']], [newSubD.edges, old.edges, ['faces', 'vertices']],
                                        [newSubD.vertices, old.vertices, ['faces']]]) {
      const all = list(comps);
      assert.strictEqual(all.length, Object.keys(olds).length);
      for (const c of all) {
        for (const prop of props) {
          assert.deepStrictEqual(c[prop].map(x => x.id), olds[String(c.id)][prop[0].toUpperCase() + prop.slice(1)], `${prop} of ${c.id}`);
          checked++;
        }
      }
    }
    assert(checked > 50);
    const face = list(newSubD.faces)[0];
    const v = list(newSubD.vertices)[0];
    for (const a of [face.edges, face.vertices, v.faces, v.edges]) assert(Array.isArray(a));   // as RhinoCommon's vertex.edges
    assert.strictEqual(face.edges.length, face.edgeCount);
    assert.deepStrictEqual(face.edges.map(e => e.id), Array.from({ length: face.edgeCount }, (_, i) => face.edgeAt(i).id));
  });

  t('test_str_is_rhinocommons_to_string_g136', () => {
    // pilot #68 (G136): RhinoCommon's ToString on the values, RhinoCommon's spelling (a JS class function
    // cannot be `toString`: the constructor's own); the same invariant forms as Python's str()
    const P = (X, Y, Z) => ({ X, Y, Z });
    assert.strictEqual(r.Point3d.ToString(P(1, 2, 3)), '1,2,3');
    assert.strictEqual(r.Point3d.ToString(P(0.1 + 0.2, 1e15, 1e-5)), '0.30000000000000004,1E+15,1E-05');
    assert.strictEqual(r.Point3d.ToString(P(-0, NaN, Infinity)), '-0,NaN,Infinity');
    assert.strictEqual(r.Vector2d.ToString({ X: 4, Y: 5 }), '4,5');
    assert.strictEqual(r.Line.ToString({ From: P(0, 0, 0), To: P(1, 2, 3) }), '0,0,0,1,2,3');
    assert.strictEqual(r.Transform.ToString(r.Transform.identity), 'R0=(1,0,0,0), R1=(0,1,0,0), R2=(0,0,1,0), R3=(0,0,0,1)');
    assert.strictEqual(typeof r.Point3d.toString(), 'string');      // the constructor's own toString is untouched
  });

  t('test_subd_create_from_cylinder_and_texture_coordinate_type_m3', () => {
    // unblocked in M3 step 1 (G137): every enum generated; counts from ON_SubD::CreateCylinder, 8 x 2 faces
    const cyl = r.Cylinder.create(r.Circle.create(r.Plane.worldXY, 2.0), 3.0);
    const C = r.SubDEndCapStyle, L = r.SubDComponentLocation;
    const make = (cap, loc = L.ControlNet) => r.SubD.createFromCylinder(cyl, 8, 2, C[cap], r.SubDEdgeTag.Crease, loc);
    const got = {};
    for (const cap of ['None', 'Quads', 'Triangles', 'Ngon']) { const s = make(cap); got[cap] = [s.faces.count, s.edges.count, s.vertices.count]; }
    assert.deepStrictEqual(got, { None: [16, 40, 24], Quads: [24, 48, 26], Triangles: [32, 56, 26], Ngon: [18, 40, 24] });
    const net = make('None').getBoundingBox(true), srf = make('None', L.Surface).getBoundingBox(true);
    assert.deepStrictEqual([net.Min.X, net.Max.X, net.Max.Z], [-2, 2, 3]);
    approx(srf.Max.X, 2.216388375108776, 1e-12);
    ctx.raises(() => r.SubD.createFromCylinder(cyl, 8, 2, 1, r.SubDEdgeTag.Crease, L.ControlNet), /expected a SubDEndCapStyle value/);
    assert.strictEqual(make('None').textureCoordinateType, r.SubDTextureCoordinateType.Unset);
  });

};
