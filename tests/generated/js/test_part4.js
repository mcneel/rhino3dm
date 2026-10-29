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
  // ctx.list, plus a bare iterator object (ManifestTableX.getEnumerator returns one: next() only)
  const list = c => {
    if (c && typeof c.next === 'function' && typeof c.iterator !== 'function') {
      const out = [];
      for (let n = c.next(); !n.done; n = c.next()) out.push(n.value);
      return out;
    }
    return ctx.list(c);
  };
  const I = () => r.TransformX.identity;
  // an old-binding member registered as a function or as a property
  const call = (obj, name, ...a) => (typeof obj[name] === 'function' ? obj[name](...a) : obj[name]);
  // an old-binding table (count / get(i)) as an array
  const oldList = tbl => { const out = []; for (let i = 0; i < tbl.count; i++) out.push(tbl.get(i)); return out; };
  const bytesOf = name => new Uint8Array(fs.readFileSync(path.join(ctx.MODELS, name)));
  const objs = f => list(f.objects);

  const boxBrep = () => r.BrepX.createFromBox(r.BoundingBoxX.create(0, 0, 0, 1, 2, 3));
  const subdFromFile = () => {
    const f = ctx.readModel('subdBox.3dm');
    for (const o of objs(f)) {
      const g = o.geometry;
      if (g instanceof r.SubDX) return [f, g];
    }
    ctx.skip('no SubD in subdBox.3dm');
  };

  t('test_annotation_bounding_box_overrides_geometry_base', () => {
    if (!r.AnnotationBaseX || !r.AnnotationBaseX.prototype.getBoundingBox) ctx.skip('G89');
    const f = ctx.readModel('textEntities_r8.3dm');
    const ann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBaseX);
    assert(ann);
    ann.parentDimensionStyle = list(f.allDimStyles)[0];
    const bb = ann.getBoundingBox(I());
    // the base's member reaches the override (a virtual on the generated base)
    assert.deepStrictEqual(r.GeometryBaseX.prototype.getBoundingBox.call(ann, I()), bb);
    if (!r.BoundingBoxX.isValid(bb)) {
      // WASM has no font glyph metrics: a text's box is empty -- in the old binding too
      const old = ctx.readOld('textEntities_r8.3dm');
      const ot = old.objects();
      const oldBoxes = [];
      for (let i = 0; i < ot.count; i++) {
        const g = ot.get(i).geometry();
        if (g instanceof r.AnnotationBase) oldBoxes.push(g.getBoundingBox());
      }
      if (oldBoxes.length && oldBoxes.every(b => !b.isValid)) ctx.skip('WASM: text bounding boxes are empty without font metrics (the old binding\'s too); the virtual-dispatch part passed');
    }
    assert.strictEqual(r.BoundingBoxX.isValid(bb), true);
    const moved = ann.getBoundingBox(r.TransformX.translation({ X: 10, Y: 0, Z: 0 }));
    approx(moved.Min.X - bb.Min.X, 10, 1e-9, 'min.x');
    approx(moved.Max.X - bb.Max.X, 10, 1e-9, 'max.x');
  });

  t('test_render_content_find_and_variant_parameters', () => {
    if (!r.File3dmRenderContentX || !r.File3dmRenderContentX.prototype.getParameter) ctx.skip('G89');
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
    const old = ctx.readOld('file3dm_stuff.3dm');
    const oldContent = oldList(call(old, 'renderContent'));
    const tops = [...list(f.renderMaterials), ...list(f.renderEnvironments), ...list(f.renderTextures)];
    const withKids = tops.filter(c => c.children.length > 0);
    assert(withKids.length > 0);
    for (const top of tops) {
      assert.strictEqual(top.parent, null);
      assert(top.topLevel.isAliasOf(top), 'topLevel is `return this`');
      const o = oldContent.find(c => String(c.id) === String(top.id));
      assert(o, 'old content ' + top.id);
      const chain = [];
      for (let k = o.getFirstChild(); k !== null && k !== undefined; k = k.getNextSibling()) chain.push(String(k.id));
      const kids = top.children;
      assert(Array.isArray(kids));
      assert.deepStrictEqual(kids.map(c => String(c.id)), chain);
      for (const c of kids) {
        assert(c instanceof r.File3dmRenderContentX && c.isChild && !c.isTopLevel);
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
    if (!r.File3dmRenderContentX || !r.File3dmRenderContentX.prototype.findChild) ctx.skip('G89');
    renderContentKid();
  });
  t('test_render_content_child_keeps_the_file_alive', async () => {
    if (!r.File3dmRenderContentX || !r.File3dmRenderContentX.prototype.findChild) ctx.skip('G89');
    const [kid, pid] = renderContentKid();
    await ctx.gcSettle();
    assert.strictEqual(kid.parent.id, pid);                    // the file is kept alive
    assert(kid.name !== null && kid.name !== undefined);
  });

  t('test_subd_vertex_edges_iterator', () => {
    if (!r.BrepFaceX) ctx.skip('components not generated');
    if (!r.SubDVertexX || !('edges' in r.SubDVertexX.prototype)) ctx.skip('G89');
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
    if (!r.ViewInfoX || !('viewport' in r.ViewInfoX.prototype)) ctx.skip('G89');
    const old = ctx.readOld('file3dm_stuff.3dm');
    const ov = call(old, 'views').get(0);
    let v = new r.ViewInfoX(ov);
    const vp = v.viewport;
    assert(vp.isAliasOf(v.viewport), 'the held object, every time');
    const loc = ov.getViewport().cameraLocation;              // old binding: [x, y, z]
    const cl = vp.cameraLocation;
    assert.deepStrictEqual([cl.X, cl.Y, cl.Z], [loc[0], loc[1], loc[2]]);
    assert.strictEqual(vp.setCameraLocation({ X: 1, Y: 2, Z: 3 }), true);
    const again = v.viewport.cameraLocation;
    assert.deepStrictEqual([again.X, again.Y, again.Z], [1, 2, 3]);   // the edit stays with the held view
    v = null;
    await ctx.gcSettle();
    assert.strictEqual(vp.cameraLocation.Z, 3);                // the view keeps its ViewInfo alive
  });

  t('test_try_convert_brep_views_and_copies', () => {
    if (!r.BrepX || !r.BrepX.tryConvertBrep) ctx.skip('G89');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const g = objs(f).map(o => o.geometry).find(x => x instanceof r.BrepX);
    assert(g);
    const b = r.BrepX.tryConvertBrep(g);
    assert(b instanceof r.BrepX && !b.isAliasOf(g) && b.faces.count === g.faces.count);
    const x0 = g.getBoundingBox(I()).Min.X;
    assert.strictEqual(b.transform(r.TransformX.translation({ X: 100, Y: 0, Z: 0 })), true);
    approx(b.getBoundingBox(I()).Min.X - x0, 100, 1e-6);
    assert.strictEqual(g.getBoundingBox(I()).Min.X, x0);        // the argument is untouched
    g.delete(); f.delete();
    assert(b.faces.count > 0);                                  // the view kept the file alive
    const own = boxBrep();                                      // owns its pointer: BrepForm
    const c = r.BrepX.tryConvertBrep(own);
    assert(!c.isAliasOf(own) && c.faces.count === own.faces.count);
  });

  t('test_material_render_material_instance_id_u1', () => {
    const m = new r.MaterialX();
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
    for (const name of ['subd.3dm', 'subdBox.3dm', 'subd_creases.3dm']) {
      const subds = objs(ctx.readModel(name)).map(o => o.geometry).filter(g => g instanceof r.SubDX);
      const ot = ctx.readOld(name).objects();
      const olds = [];
      for (let i = 0; i < ot.count; i++) {
        const g = ot.get(i).geometry();
        if (g instanceof r.SubD) olds.push(g);
      }
      assert.strictEqual(subds.length, olds.length);
      subds.forEach((s, k) => {
        const n = s.sharpEdgeCount();
        const edges = [...s.edges];
        assert.strictEqual(n, edges.filter(e => e.isSharp).length);
        assert.strictEqual(n, olds[k].sharpEdgeCount);
        for (const e of edges) assert.deepStrictEqual(Object.keys(e.sharpness).sort(), ['sharpness0', 'sharpness1']);
        const errors = r.SubDX.errorCount;
        assert.strictEqual(s.clearEdgeSharpness(), n);
        assert.strictEqual(s.sharpEdgeCount(), 0);
        assert.strictEqual(r.SubDX.errorCount, errors);
        total += n;
      });
    }
    ctx.log && ctx.log('sharp edges in the SubD fixtures: ' + total);
  });

  t('test_subd_edge_sharpness_value_type_g109', () => {
    // D61: a plain object {sharpness0, sharpness1} (raw ends); create() is RhinoCommon's construction
    const S = r.SubDEdgeSharpnessX;
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
      const pc = new r.PointCloudX();
      pc.addRange(range(0, n).map(P), range(0, n).map(j => ({ X: 0, Y: 0, Z: j + 1 })));
      pc.insertRange(i, range(0, k).map(j => P(90 + j)));
      assert.deepStrictEqual(Array.from(pc.getPoints()).map(p => p.X), [...range(0, i), ...range(0, k).map(j => 90 + j), ...range(i, n)]);
      assert.deepStrictEqual(Array.from(pc.getNormals()).map(v => v.Z), [...range(0, i).map(j => j + 1), ...range(0, k).map(() => 0), ...range(i, n).map(j => j + 1)]);
      pc.delete();
    }
    const pc = new r.PointCloudX();
    pc.addRange([P(0), P(1)]);
    ctx.raises(() => pc.insertRange(-1, [P(9)]), /larger than or equal to zero/);
    ctx.raises(() => pc.insertRange(3, [P(9)]), /smaller than or equal to Count/);
    pc.delete();
  });

  t('test_mesh_normal_list_is_mesh_vertex_normal_list', () => {
    // the old MeshNormalList is RhinoCommon's MeshVertexNormalList (registry 'old' pairing)
    const folded = () => {
      const m = new r.Mesh();
      for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0], [2, 0, 1], [2, 1, 1]]) m.vertices().add(x, y, z);
      m.faces().addQuadFace(0, 1, 2, 3);
      m.faces().addQuadFace(1, 4, 5, 2);
      return m;
    };
    const xyz = v => Array.isArray(v) ? Array.from(v) : [v.X ?? v.x, v.Y ?? v.y, v.Z ?? v.z];
    const vals = l => Array.from({ length: l.count }, (_, i) => xyz(l.get(i)));
    let old = folded().normals(), neu = new r.MeshX(folded()).normals;
    assert(old.computeNormals() && neu.computeNormals());
    assert.strictEqual(old.count, 6);
    assert.deepStrictEqual(vals(old), vals(neu));
    old.flip(); neu.flip();
    assert.deepStrictEqual(vals(old), vals(neu));
    // D66: the old add unitized (ON_Mesh::SetVertexNormal); RhinoCommon's stores the vector as given
    assert.strictEqual(old.add(0, 0, 2), 6);
    assert.strictEqual(neu.add(0, 0, 2), 6);
    assert.deepStrictEqual(xyz(old.get(6)), [0, 0, 1]);
    assert.deepStrictEqual(xyz(neu.get(6)), [0, 0, 2]);
    assert.deepStrictEqual(vals(old).slice(0, 6), vals(neu).slice(0, 6));
    old.clear(); neu.clear();
    assert.strictEqual(old.count, 0); assert.strictEqual(neu.count, 0);
  });

  t('test_dimension_classes_and_display_lines_g111', () => {
    // triage batch B: generated dimension classes, handed out by files as their own class;
    // GetDisplayLines -> [rc, lines] (values by parity against .NET)
    const S = new r.DimensionStyleX();
    const P = (x, y) => ({ X: x, Y: y, Z: 0 });
    const od = r.OrdinateDimensionX.create(S, r.PlaneX.worldXY, r.OrdinateDimensionMeasuredDirection.Xaxis, P(0, 0), P(5, 3), P(5, 8), 1.0, 1.0);
    const ld = r.LinearDimensionX.create(r.AnnotationType.Aligned, S, r.PlaneX.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0);
    const f = new r.File3dmX();
    const ids = [f.objects.addOrdinateDimension(od), f.objects.addLinearDimension(ld)];
    const back = r.File3dmX.fromByteArray(f.toByteArray(null));
    const geom = {};
    for (const o of objs(back)) geom[String(o.attributes.objectId)] = o.geometry;
    assert(geom[String(ids[0])] instanceof r.OrdinateDimensionX, 'ordinate: ' + Object.keys(geom).join(',') + ' vs ' + ids);
    assert(geom[String(ids[1])] instanceof r.LinearDimensionX);
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
    let ld = r.LinearDimensionX.create(r.AnnotationType.Aligned, new r.DimensionStyleX(), r.PlaneX.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0);
    await ctx.gcSettle();
    assert(ld.parentDimensionStyle.textHeight > 0);
    ld.parentDimensionStyle = new r.DimensionStyleX();
    await ctx.gcSettle();
    assert(ld.parentDimensionStyle.textHeight > 0);
    assert.deepStrictEqual(r.rh3dmPostUnresolved, []);
  });

  t('test_user_strings_kept_where_rhino3dm_had_them_d62', () => {
    // D62: user strings on Viewport / RenderSettings / Bitmap / TextureMapping; getUserStrings pairs
    const f = new r.File3dmX();
    const I = (a, b) => ({ T0: a, T1: b });
    for (const o of [new r.ViewportX(), f.settings.renderSettings, new r.BitmapX(),
                     r.TextureMappingX.createPlaneMapping(r.PlaneX.worldXY, I(0, 1), I(0, 1), I(0, 1))]) {
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
    const f = new r.File3dmX();
    const m = new r.MaterialX(); m.diffuseColor = { r: 200, g: 0, b: 0, a: 255 }; f.materials.add(m);
    const a = new r.ObjectAttributesX();
    a.colorSource = r.ObjectColorSource.ColorFromMaterial;
    a.materialSource = r.ObjectMaterialSource.MaterialFromObject; a.materialIndex = 0;
    const c = a.drawColor(f);
    assert.deepStrictEqual([c.r, c.g, c.b], [200, 0, 0]);
  });

  t('test_viewport_screen_port_g105', () => {
    const v = new r.ViewportX();
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
    const o = new r.ViewportInfo();
    o.screenPort = [10, 20, 640, 480];
    v.screenPort = [10, 20, 640, 480];
    assert.deepStrictEqual(Array.from(o.screenPort), v.screenPort);
  });

  t('test_view_table_items_are_live_views_and_plugin_data_g104', async () => {
    const v = (() => {
      const f = ctx.readModel('file3dm_stuff.3dm');
      const views = [...f.views];
      assert(views.length === f.views.count && views.length > 0 && views.every(x => x instanceof r.ViewInfoX));
      const v0 = f.views.get(0);
      v0.name = 'rh3dm-live';
      assert.strictEqual(f.views.get(0).name, 'rh3dm-live');   // write-through, not a copy
      return v0;
    })();
    await ctx.gcSettle();
    assert.strictEqual(v.name, 'rh3dm-live');                   // the view keeps its file
    assert.strictEqual(new r.ViewInfoX().name, '');
    ctx.raises(() => ctx.readModel('file3dm_stuff.3dm').views.get(99), /out_of_range/);
    const g = ctx.readModel('file3dm_stuff.3dm');
    const pd = [...g.plugInData];
    assert.strictEqual(pd.length, g.plugInData.count);
    assert(pd.every(p => p instanceof r.File3dmPlugInDataX && /^[0-9a-f-]{36}$/i.test(p.plugInId)));
  });

  t('test_hand_bitmaps_add_find_iterate_round_trip', async () => {
    if (!r.BitmapX) ctx.skip('G90 hand bitmaps');
    const f = new r.File3dmX();
    const paths = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png'];
    const ids = [];
    for (const p of paths) {
      const b = new r.BitmapX();
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
      const g = r.File3dmX.read(out);
      assert.deepStrictEqual(list(g.bitmaps).map(b => b.fileFullPath).sort(), paths.slice(1).sort());
      return list(g.bitmaps)[0];
    })();
    await ctx.gcSettle();
    assert(paths.includes(bm.fileFullPath));                    // the view keeps the file alive
  });

  t('test_manifest_table_find_count_enumerate', async () => {
    if (!r.ManifestTableX) ctx.skip('G91');
    let f = ctx.readModel('file3dm_stuff.3dm');
    const m = f.manifest;
    const T = r.ModelComponentType;
    assert(Number.isInteger(m.count) && m.count > 0);            // Python's len(m) is Count
    assert(Number(m.longCount) === m.count, 'longCount');          // a BigInt in JS (C# long)
    assert.strictEqual(m.componentType, T.Mixed);
    const layers = list(f.allLayers);
    assert(layers.length > 0 && m.activeObjectCount(T.Layer) === f.allLayers.count);
    const viaenum = list(m.getEnumerator(T.Layer));
    assert(viaenum.every(x => x instanceof r.LayerX));
    assert.deepStrictEqual(viaenum.map(x => String(x.id)), layers.map(l => String(l.id)));
    const l0 = layers[0];
    for (const found of [m.findId(l0.id), m.findId(l0.id, T.Layer), m.findIndex(l0.index, T.Layer),
      m.findName(l0.name, T.Layer, NIL)]) {
      assert(found instanceof r.LayerX && found.id === l0.id && found.name === l0.name);
    }
    assert.strictEqual(m.findId(uuid4()), null);
    const mats = list(m.getEnumerator(T.RenderMaterial));
    assert(mats.every(x => x instanceof r.MaterialX));
    const os = list(m.getEnumerator(T.ModelGeometry));
    assert(os.every(x => x instanceof r.File3dmObjectX) && os.length === objs(f).length);
    ctx.raises(() => m.findIndex(0, T.Unset), /out_of_range,.*Parameter 'type'/);
    const unmade = [T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle].filter(x => m.activeObjectCount(x));
    if (unmade.length) {
      ctx.raises(() => list(m), /runtime_error/);
    } else {
      assert(list(m).every(x => x instanceof r.ModelComponentX));
    }
    f = null;
    await ctx.gcSettle();
    assert.strictEqual(viaenum[0].name, l0.name);               // items keep the file alive
  });

  t('test_manifest_images_are_bitmapx', () => {
    if (!r.ManifestTableX || !r.BitmapX) ctx.skip('G91');
    const f = new r.File3dmX();
    const b = new r.BitmapX();
    b.fileFullPath = '/tmp/rh3dm_manifest.png';
    const added = f.bitmaps.add(b);
    const imgs = list(f.manifest.getEnumerator(r.ModelComponentType.Image));
    assert(imgs.length === 1 && imgs[0] instanceof r.BitmapX);
    assert(imgs[0].fileFullPath === '/tmp/rh3dm_manifest.png' && imgs[0].id === added.id);
    assert.strictEqual(f.manifest.findId(added.id).fileFullPath, '/tmp/rh3dm_manifest.png');
  });

  t('test_layer_full_path_and_name_override', () => {
    if (!r.LayerX || !('fullPath' in r.LayerX.prototype)) ctx.skip('G92');
    const f = new r.File3dmX();
    const tb = f.allLayers;
    const black = { r: 0, g: 0, b: 0, a: 255 };
    tb.addLayer('top', black);
    const top = list(tb)[0];
    tb.addLayer('mid', black, top.id);
    const mid = list(tb).find(l => l.name === 'mid');
    tb.addLayer('leaf', black, mid.id);
    const leaf = list(tb).find(l => l.name === 'leaf');
    const sep = ctx.val(r.ModelComponentX.namePathSeparator);
    assert.strictEqual(top.fullPath, 'top');
    assert.strictEqual(leaf.fullPath, ['top', 'mid', 'leaf'].join(sep));
    assert.strictEqual(leaf.toString(), leaf.fullPath);
    leaf.name = 'leaf2';                                         // the override keeps the base setter
    assert(list(tb).find(l => l.id === leaf.id).fullPath.endsWith(sep + 'leaf2'));
    assert.strictEqual(new r.LayerX().fullPath, new r.LayerX().name);   // not in a file: its name
  });

  t('test_group_members_and_find_by_group', async () => {
    if (!r.File3dmGroupTableX || !r.File3dmGroupTableX.prototype.groupMembers) ctx.skip('G92');
    let f = new r.File3dmX();
    const g0 = f.allGroups.addGroup();
    const g1 = f.allGroups.addGroup();
    const add = groups => {
      const a = new r.ObjectAttributesX();
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
    // (class and value type alike: r.Point3dX.unset), so nothing here is callable.
    assert(r.MeshingParametersX.default instanceof r.MeshingParametersX);
    assert.strictEqual(r.MaterialX.maxShine, 255.0);
    assert.strictEqual(typeof r.ModelComponentX.namePathSeparator, 'string');
    assert.notStrictEqual(r.ObjectAttributesX.oCSMappingChannelId, undefined);
    assert(r.SubDX.empty instanceof r.SubDX);
    assert(typeof r.Point3dX.unset === 'object' && r.Point3dX.unset.X < -1e300);
    assert(typeof r.PlaneX.worldXY === 'object' && r.PlaneX.worldXY.ZAxis.Z === 1);
  });

  t('test_retired_arc_arccurve_brep_members', () => {
    const oa = r.Arc.createFromPoints([0, 0, 0], [1, 1, 0], [2, 0, 0]);
    const na = r.ArcX.create({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 });
    approx(oa.angle, r.ArcX.angle(na), 1e-12, 'Arc.AngleRadians -> Arc.Angle');
    // Brep.CreateFromBoundingBox -> Brep.CreateFromBox(BoundingBox) / CreateFromBox(Box)
    const ob = r.Brep.createFromBoundingBox(new r.BoundingBox([0, 0, 0], [1, 2, 3]));
    const bb = r.BoundingBoxX.create({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 });
    const nb = r.BrepX.createFromBox(bb);
    const obFaces = ob.faces();
    assert.strictEqual(obFaces.count, 6);
    assert.strictEqual(nb.faces.count, 6);
    assert.strictEqual(r.BrepX.createFromBox(r.BoxX.create(bb)).faces.count, 6);
    // BrepTrim.EdgeIndex / StartVertexIndex / EndVertexIndex -> trim.Edge.EdgeIndex, ...
    const key = a => a.join(',');
    const oldTrims = [];
    for (let i = 0; i < obFaces.count; i++) {
      const loops = call(obFaces.get(i), 'loops');
      for (let j = 0; j < loops.count; j++) {
        const trims = call(loops.get(j), 'trims');
        for (let k = 0; k < trims.count; k++) {
          const tr = trims.get(k);
          oldTrims.push([tr.edgeIndex, tr.startVertexIndex, tr.endVertexIndex]);
        }
      }
    }
    const newTrims = [];
    for (let i = 0; i < nb.trims.count; i++) {
      const tr = nb.trims.get(i);
      newTrims.push([tr.edge ? tr.edge.edgeIndex : -1, tr.startVertex.vertexIndex, tr.endVertex.vertexIndex]);
    }
    assert.deepStrictEqual(oldTrims.map(key).sort(), newTrims.map(key).sort());
    // BrepVertex.EdgeCount -> vertex.EdgeIndices().length
    const ov = ob.vertices();
    const oldCounts = [], newCounts = [];
    for (let i = 0; i < ov.count; i++) oldCounts.push(ov.get(i).edgeCount);
    for (let i = 0; i < nb.vertices.count; i++) newCounts.push(nb.vertices.get(i).edgeIndices().length);
    assert.deepStrictEqual(oldCounts, newCounts);
    // ArcCurve.CreateFromArc / ...Params / CreateFromCircle / ...Params -> the ArcCurve
    // constructors (last: the JS registration may lack them -- see the report)
    const same = (oc, nc) => {
      const od = oc.domain, nd = nc.domain;
      approx(od[0], nd.T0, 1e-12, 'T0'); approx(od[1], nd.T1, 1e-12, 'T1');
      const a = oc.pointAtEnd, b = nc.pointAtEnd;
      approx(a[0], b.X, 1e-9); approx(a[1], b.Y, 1e-9); approx(a[2], b.Z, 1e-9);
    };
    const circ = r.CircleX.create(3.0);
    same(r.ArcCurve.createFromArcParams(oa, 2.0, 5.0), new r.ArcCurveX(na, 2.0, 5.0));
    same(r.ArcCurve.createFromArc(oa), new r.ArcCurveX(na));                          // ArcCurve(Arc)
    same(r.ArcCurve.createFromCircle(new r.Circle(3.0)), new r.ArcCurveX(circ));          // ArcCurve(Circle)
    same(r.ArcCurve.createFromCircleParams(new r.Circle(3.0), 1.0, 4.0), new r.ArcCurveX(circ, 1.0, 4.0));
  });

  t('test_retired_viewinfo_vector_sun_shutlining_members', () => {
    const old = ctx.readOld('file3dm_stuff.3dm');
    const ov = call(old, 'views').get(0);
    assert.strictEqual(new r.ViewInfoX(ov).wallpaperFilename, ov.wallpaperName);   // ViewInfo.WallpaperName
    // Vector3d.DotProduct -> the * operator (old JS has no Vector3d class: the dot by hand)
    const dot = 1 * -4 + 2 * 5 + 3 * 0.5;
    assert.strictEqual(r.Vector3dX.multiply({ X: 1, Y: 2, Z: 3 }, { X: -4, Y: 5, Z: 0.5 }), dot);
    const rgb = c => (Array.isArray(c) ? c.slice(0, 3) : [c.r !== undefined ? c.r : c.R, c.g !== undefined ? c.g : c.G, c.b !== undefined ? c.b : c.B]);
    for (const alt of [-10.0, 0.0, 5.0, 12.5, 30.0, 60.0, 89.0]) {
      const oc = rgb(r.Sun.sunColorFromAltitude(alt)), nc = rgb(r.SunX.colorFromAltitude(alt));
      // RhinoCommon rounds 255*c to the nearest integer; the old binding truncated 255.5*c (D45)
      assert(oc.every((x, i) => Math.abs(x - nc[i]) <= 1), JSON.stringify([alt, oc, nc]));
    }
    // ShutLining.Curves -> GetCurves() ids + the id-keyed accessors, written by the old binding
    // (CreateShutLining / CreateThickening: kept by Luis, their trailing space fixed -- pilot #42)
    const cid = uuid4();
    const of = new r.File3dm();
    const oa = new r.ObjectAttributes();
    const mm = call(oa, 'meshModifiers');
    assert(call(mm, 'thickening') == null);
    mm.createThickening();
    assert(call(mm, 'thickening') != null);
    mm.createShutLining();
    const osl = call(mm, 'shutLining');
    const curves = call(osl, 'curves');
    curves.add(cid);
    const c = curves.findIndex(0);
    c.radius = 2.5; c.profile = 3; c.pull = true; c.isBump = true; c.enabled = false;
    call(of, 'objects').addPoint([0, 0, 0], oa);
    const f2 = r.File3dmX.fromByteArray(of.toByteArray());
    const g = objs(f2)[0];
    const sl = g.attributes.file3dmMeshModifiers.shutLining;
    if (sl === null || sl === undefined) {
      // no mesh modifier made through rhino3dm survives a write and read (pilot-findings #43)
      ctx.skip('xfail pilot-findings #43: mesh modifiers are lost on write/read');
    }
    assert.deepStrictEqual(list(sl.getCurves()).map(String), [String(cid)]);
    assert.deepStrictEqual([sl.curveRadius(cid), sl.curveProfile(cid), sl.curvePull(cid), sl.curveIsBump(cid), sl.curveEnabled(cid)],
      [2.5, 3, true, true, false]);
  });

  t('test_class_sequence_parameters', () => {
    if (!r.BrepX || !r.BrepX.createTrimmedPlane) ctx.skip('G95');
    // the circle curve through the bridge constructor: ArcCurveX(CircleX) is not registered in
    // JS (test_retired_arc_arccurve_brep_members covers that)
    const circle = new r.ArcCurveX(r.ArcCurve.createFromCircle(new r.Circle(2.0)));
    const W = r.PlaneX.worldXY;
    const one = r.BrepX.createTrimmedPlane(W, circle);
    const many = r.BrepX.createTrimmedPlane(W, [circle]);
    const old = r.Brep.createTrimmedPlane(r.Plane.worldXY(), r.ArcCurve.createFromCircle(new r.Circle(2.0)));
    assert.strictEqual(one.faces.count, 1);
    assert.strictEqual(many.faces.count, 1);
    assert.strictEqual(old.faces().count, 1);
    assert.strictEqual(one.edges.count, call(old, 'edges').count);
    // File3dmInstanceDefinitionTable.Add(name, description, basePoint, geometry, attributes)
    const f = new r.File3dmX();
    const n0 = f.allInstanceDefinitions.count;
    const idx = f.allInstanceDefinitions.add('blk', 'desc', { X: 0, Y: 0, Z: 0 }, [circle], [new r.ObjectAttributesX()]);
    assert(idx >= 0 && f.allInstanceDefinitions.count === n0 + 1);
    // File3dm.WriteMultipleObjects(path, geometry) -- the module's in-memory file system
    const out = '/tmp/rh3dm_multi_' + process.pid + '.3dm';
    assert.strictEqual(r.File3dmX.writeMultipleObjects(out, [circle, one]), true);
    assert.strictEqual(objs(r.File3dmX.read(out)).length, 2);
  });

  t('test_annotation_text_from_text_runs', () => {
    if (!r.AnnotationBaseX || !('textHasRtfFormatting' in r.AnnotationBaseX.prototype)) ctx.skip('G96 not generated');
    const f = ctx.readModel('textEntities_r8.3dm');
    const old = ctx.readOld('textEntities_r8.3dm');
    const oldText = {};
    for (const o of oldList(call(old, 'objects'))) {
      const g = call(o, 'geometry');
      if (g instanceof r.Text) oldText[String(call(o, 'attributes').id)] = g.plainText;
    }
    const nl = s => s.replace(/\r\n/g, '\n');
    let seen = 0;
    for (const o of objs(f)) {
      const g = o.geometry;
      if (!(g instanceof r.AnnotationBaseX)) continue;
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
    if (!r.TextureX || !('projectionMode' in r.TextureX.prototype)) ctx.skip('G96 not generated');
    const tx = new r.TextureX();
    const m = r.TextureProjectionModes;
    assert.strictEqual(tx.projectionMode, m.MappingChannel);
    tx.projectionMode = m.Wcs;
    assert.strictEqual(tx.projectionMode, m.Wcs);
  });

  t('test_sun_date_time', () => {
    if (!r.SunX || !r.SunX.prototype.getDateTime) ctx.skip('G97 not generated');
    // JS: a Date is an instant; local time is the process zone (Local), as .NET's
    const s = new r.SunX();
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
    const a = r.SunX.altitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, false);
    assert(a >= -90.0 && a <= 90.0, String(a));
  });

  t('test_retired_sun_date_members', () => {
    if (!r.SunX || !r.SunX.prototype.getDateTime) ctx.skip('G97 not generated');
    const of = new r.File3dm();
    const osun = call(call(of, 'settings'), 'renderSettings').sun;
    osun.year = 2023; osun.month = 11; osun.day = 5; osun.hours = 9.75;
    // the old getter may hand out a copy: re-read what the file holds
    const held = call(call(of, 'settings'), 'renderSettings').sun;
    assert.deepStrictEqual([held.year, held.month, held.day, held.hours], [2023, 11, 5, 9.75], 'old Sun setters reach the file');
    const f = r.File3dmX.fromByteArray(of.toByteArray());
    const d = f.settings.renderSettings.sun.getDateTime(r.DateTimeKind.Local);
    assert.deepStrictEqual([d.getFullYear(), d.getMonth() + 1, d.getDate()], [2023, 11, 5]);
    assert(Math.abs(d.getHours() + d.getMinutes() / 60 + d.getSeconds() / 3600 - 9.75) < 1 / 3600);
  });

  t('test_retired_texturemapping_members', () => {
    if (!r.TextureMappingX || !('mappingType' in r.TextureMappingX.prototype)) ctx.skip('G97 not generated');
    const T = r.TextureMappingType;
    const Iv = [0, 1];
    const TM = r.TextureMapping;
    const olds = [TM.createSurfaceParameterMapping(),
      TM.createPlaneMapping(r.Plane.worldXY(), Iv, Iv, Iv),
      TM.createCylinderMapping(new r.Cylinder(new r.Circle(1.0), 2.0), true),
      TM.createCylinderMapping(new r.Cylinder(new r.Circle(1.0), 2.0), false),
      TM.createSphereMapping(new r.Sphere([0, 0, 0], 2.0)),
      TM.CreateBoxMapping(r.Plane.worldXY(), Iv, Iv, Iv, true)];      // sic: the old JS name
    const kinds = olds.map(o => new r.TextureMappingX(o).mappingType);
    assert.deepStrictEqual(kinds.map(k => k.value),
      [T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping].map(k => k.value));
    olds.forEach((o, i) => {
      const k = kinds[i];
      assert.strictEqual(o.isPeriodic, k === T.SphereMapping || k === T.CylinderMapping);
      // RequiresVertexNormals (D49): RhinoCommon's properties, as ON_TextureMapping decides it
      const n = new r.TextureMappingX(o);
      const derived = k !== T.SurfaceParameters && (n.projection === r.Projection.Ray || k === T.BoxMapping
        || (k === T.CylinderMapping && n.capped));
      assert.strictEqual(o.requiresVertexNormals, derived, String(k.value));
    });
    const composed = x => {
      const n = new r.TextureMappingX(TM.createPlaneMapping(r.Plane.worldXY(), Iv, Iv, Iv));
      n.uvwTransform = r.TransformX.multiply(x, n.uvwTransform);
      return n.uvwTransform;
    };
    const matrix = m => { const a = []; for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) a.push(m['M' + i + j]); return a; };
    for (const [name, args, build] of [
      ['reverseTextureCoordinate', [1], x => { x.M11 = -1.0; x.M13 = 1.0; }],
      ['swapTextureCoordinate', [0, 2], x => { x.M00 = 0.0; x.M22 = 0.0; x.M02 = 1.0; x.M20 = 1.0; }],
      ['tileTextureCoordinate', [0, 3.0, 0.25], x => { x.M00 = 3.0; x.M03 = 0.25; }]]) {
      const o = TM.createPlaneMapping(r.Plane.worldXY(), Iv, Iv, Iv);
      assert(o[name](...args), name);
      const x = r.TransformX.create(1.0);
      build(x);
      assert.deepStrictEqual(matrix(new r.TextureMappingX(o).uvwTransform), matrix(composed(x)), name);
    }
  });

  t('test_retired_rhino_only_pairings', () => {
    assert.strictEqual(ctx.val(r.ModelComponentX.namePathSeparator), '::');
    const path3 = 'textEntities_r8.3dm';
    const old = ctx.readOld(path3), f = ctx.readModel(path3);
    const ds = list(f.allDimStyles)[0];
    const ods = oldList(call(old, 'dimstyles')).find(s => String(s.id) === String(ds.id));
    assert(ods);
    const oann = {};
    for (const o of oldList(call(old, 'objects'))) {
      const g = call(o, 'geometry');
      if (g instanceof r.AnnotationBase) oann[String(call(o, 'attributes').id)] = g;
    }
    let n = 0;
    for (const o of objs(f)) {
      const g = o.geometry;
      const oid = String(o.attributes.objectId);
      if (g instanceof r.AnnotationBaseX && oid in oann) {
        g.parentDimensionStyle = ds;
        assert.strictEqual(g.dimensionScale, oann[oid].getDimensionScale(ods));
        n++;
      }
    }
    assert(n > 0);
    // a static inherited from ModelComponent, reached through the derived class (last)
    assert.strictEqual(r.LayerX.namePathSeparator, r.ModelComponentX.namePathSeparator, 'LayerX.namePathSeparator is not reachable on the derived class');
    assert.strictEqual(ctx.val(r.LayerX.namePathSeparator), '::');
  });

  t('test_post_effect_collection_and_data', () => {
    if (!r.PostEffectCollectionX) ctx.skip('G98 not generated');
    let seen = 0;
    for (const name of ['file3dm_stuff.3dm', 'textEntities_r8.3dm', 'sphereDecals.3dm']) {
      // Type (D50): opennurbs Unset 0, Early 1, ToneMapping 2, Late 3; RhinoCommon Early 0, ...
      const oldFile = ctx.readOld(name);
      const ope = call(call(call(oldFile, 'settings'), 'renderSettings'), 'postEffects');
      const old = oldList(ope).map(p => [String(p.id), p.localName, p.on, p.shown, Math.max(p.type.value - 1, 0)]);
      const f = ctx.readModel(name);
      const coll = f.settings.renderSettings.postEffects;
      const nw = list(coll).map(p => [String(p.id), p.localName, p.on, p.shown, p.type.value]);
      assert.deepStrictEqual(nw, old, name);
      seen += nw.length;
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
    assert.deepStrictEqual(list(new r.PostEffectCollectionX()), []);   // free-floating: empty
    if (seen === 0) ctx.skip('no model with post effects in tests/models: per-item parity needs a Rhino-saved fixture');
  });

  t('test_font_is_managed_and_dimension_style_font', () => {
    if (!r.DimensionStyleX || !('font' in r.DimensionStyleX.prototype)) ctx.skip('G99 not generated');
    const f = ctx.readModel('textEntities_r8.3dm');
    const old = ctx.readOld('textEntities_r8.3dm');
    const odss = oldList(call(old, 'dimstyles'));
    for (const ds of list(f.allDimStyles)) {
      const ods = odss.find(s => String(s.id) === String(ds.id));
      assert.strictEqual(ds.font.faceName, ods.getFont().faceName);   // the same managed font
    }
    const q = r.FontX.fromQuartetProperties('Arial', false, false);
    assert(q === null || q === undefined || q.familyName);
    // G99 side effect: setters whose C argument is an opennurbs scoped enum
    const s2 = new r.DimensionStyleX();
    s2.fitText = s2.fitText;
    s2.fitArrow = s2.fitArrow;
    // a managed font outlives the style it came from
    const s0 = new r.DimensionStyleX();
    const viaStyle = s0.font;
    s0.delete();
    assert(viaStyle.familyName);
    // Font(familyName): RhinoCommon's one-string constructor (last: see the report)
    assert.strictEqual(r.FontX.prototype.constructor.length >= 0, true);
    const a = new r.FontX('Arial'), b = new r.FontX('Arial');
    assert(a.familyName === b.familyName && a.description === b.description);
    a.delete();                                                  // dropping one frees nothing shared
    assert(b.familyName && new r.FontX('Arial').description === b.description);
    const s = new r.DimensionStyleX();
    s.font = new r.FontX('Courier New');
    const got = s.font;
    s.delete();
    assert(got.familyName);                                       // managed: outlives the style
  });

  // ---- retirement batch 1 (Luis 2026-10-28): the JS halves of the Python tests of the same names
  const oldGeoms = name => { const ot = ctx.readOld(name).objects(); const out = []; for (let i = 0; i < ot.count; i++) out.push(ot.get(i).geometry()); return out; };
  const ev = v => (v && typeof v === 'object' && 'value' in v ? v.value : v);

  t('test_retired_annotation_explicit_parent_accessors_c', () => {
    // C (drop): getX(parentStyle) / setX(parentStyle, v) -> ann.parentDimensionStyle = style; ann.x (D39, D67)
    const f = ctx.readModel('textEntities_r8.3dm');
    const old = ctx.readOld('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBaseX);
    const ot = old.objects();
    let oann = null;
    for (let i = 0; i < ot.count && !oann; i++) { const g = ot.get(i).geometry(); if (g instanceof r.AnnotationBase) oann = g; }
    assert(nann && oann);
    const ostyle = oldList(call(old, 'dimstyles')).find(s => String(s.id) === String(oann.dimensionStyleId));
    const nstyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    assert(ostyle && nstyle);
    nann.parentDimensionStyle = nstyle;
    assert.strictEqual(nann.textHeight, oann.getTextHeight(ostyle));
    assert.strictEqual(nann.textHeight, 10.0);                       // the override survives (G114)
    for (const n of ['TextHeight', 'MaskOffset', 'MaskEnabled', 'MaskColorSource', 'MaskFrame',
                     'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay']) {
      const p = n[0].toLowerCase() + n.slice(1);
      assert.strictEqual(ev(nann[p]), ev(oann['get' + n](ostyle)), n);
    }
    for (const [n, v] of [['TextHeight', 7.5], ['MaskOffset', 0.25], ['MaskEnabled', true]]) {
      const p = n[0].toLowerCase() + n.slice(1);
      oann['set' + n](ostyle, v);
      nann[p] = v;
      assert.strictEqual(nann[p], oann['get' + n](ostyle), n);
    }
    oann.setDimensionScale(ostyle, 2.0);
    nann.dimensionScale = 2.0;
    assert.strictEqual(nann.dimensionScale, oann.getDimensionScale(ostyle));
  });

  t('test_retired_subd_counts_and_ids_g1', () => {
    // G1 (map): subd.edgeCount / faceCount / vertexCount -> edges / faces / vertices .count; index -> id
    const [, n] = subdFromFile();
    const o = oldGeoms('subdBox.3dm').find(g => g instanceof r.SubD);
    assert.deepStrictEqual([o.edgeCount, o.faceCount, o.vertexCount], [n.edges.count, n.faces.count, n.vertices.count]);
    // the old JS iterator's get(i) looks a component up by ID (FaceFromId): walk it with first/next
    const ids = it => { const out = []; for (let c = it.first(), i = 0; i < it.count; c = it.next(), i++) out.push(c.index); return out.sort((a, b) => a - b); };
    assert.deepStrictEqual(ids(call(o, 'faces')), [...n.faces].map(c => c.id).sort((a, b) => a - b));
    assert.deepStrictEqual(ids(call(o, 'edges')), [...n.edges].map(c => c.id).sort((a, b) => a - b));
    assert.deepStrictEqual(ids(call(o, 'vertices')), [...n.vertices].map(c => c.id).sort((a, b) => a - b));
  });

  t('test_retired_likely_renames_l1_and_other_maps_m1', () => {
    // L1 / M1 (map): each old member against its RhinoCommon member
    const obox = r.Brep.createFromBoundingBox(new r.BoundingBox([0, 0, 0], [1, 2, 3]));
    const nbox = boxBrep();
    assert.strictEqual(call(call(obox, 'faces').get(0), 'outerLoop').trimCount, nbox.faces.get(0).outerLoop.trims.count);
    const pts = [[0, 0, 0], [4, 0, 0], [4, 4, 0]];
    const opl = r.Polyline.createFromPoints(pts);
    const npl = new r.PolylineX(pts.map(([X, Y, Z]) => ({ X, Y, Z })));
    assert.strictEqual(npl.count, opl.count);
    const oc = opl.closesPoint([5, 1, 0]), nc = npl.closestPoint({ X: 5, Y: 1, Z: 0 });
    assert.deepStrictEqual([oc[0], oc[1]], [nc.X, nc.Y]);
    const ol = new r.Point3dList(), nl = new r.Point3dListX();
    ol.append([[1, 2, 3]]); nl.add(1.0, 2.0, 3.0);
    assert.deepStrictEqual([ol.get(0)[0], ol.get(0)[2]], [nl.get(0).X, nl.get(0).Z]);
    const opc = new r.PointCloud(); opc.addPointValue([0, 0, 0], 2.5); opc.addPointValue([1, 0, 0], -1.0);
    const npc = new r.PointCloudX(opc);              // RhinoCommon adds a value only with a normal and a color
    assert.strictEqual(opc.containsValues, npc.containsPointValues);
    assert.deepStrictEqual(Array.from(opc.getValues()), Array.from(npc.getPointValues()));
    const osrf = r.NurbsSurface.createFromSphere(new r.Sphere([0, 0, 0], 2));
    const nsrf = r.NurbsSurfaceX.createFromSphere(r.SphereX.create({ X: 0, Y: 0, Z: 0 }, 2));
    assert.deepStrictEqual([call(osrf.knotsU(), 'isClampedStart'), call(osrf.knotsU(), 'isClampedEnd')],
                           [nsrf.knotsU.clampedAtStart, nsrf.knotsU.clampedAtEnd]);
    assert.deepStrictEqual(Array.from(osrf.knotsU().toList()), [...nsrf.knotsU]);
    assert.strictEqual(osrf.points().makeNonRational(), nsrf.makeNonRational());
    const m = new r.Material(); m.toPhysicallyBased(); const nm = new r.MaterialX(); nm.toPhysicallyBased();
    assert.strictEqual(call(m, 'physicallyBased').supported, nm.isPhysicallyBased);
    const oe = r.Extrusion.createWithPlane(new r.Circle(2).toNurbsCurve(), r.Plane.worldXY(), 3.0, true);
    const ne = r.ExtrusionX.create(r.CircleX.toNurbsCurve(r.CircleX.create(2)), r.PlaneX.worldXY, 3.0, true);
    approx(oe.getBoundingBox().max[2], ne.getBoundingBox(I()).Max.Z, 1e-9, 'extrusion height');
    const mesh = new r.Mesh(), nmesh = new r.MeshX();
    assert.strictEqual(mesh.vertices().addPoint3d(0.1, 0.2, 0.3), nmesh.vertices.add(0.1, 0.2, 0.3));
    assert.strictEqual(nmesh.vertices.point3dAt(0).X, 0.1);         // double precision kept, as addPoint3d did
    const od = [], nd = [];
    const ot = ctx.readOld('sphereDecals.3dm').objects();
    for (let i = 0; i < ot.count; i++) { const ds = ot.get(i).attributes().decals(); for (let k = 0; k < ds.count; k++) od.push(ds.get(k)); }
    for (const o of objs(ctx.readModel('sphereDecals.3dm'))) nd.push(...list(o.attributes.decals));
    assert(od.length > 0 && od.length === nd.length);
    od.forEach((a, k) => {
      const b = nd[k];
      assert.deepStrictEqual([a.horzSweepStart, a.horzSweepEnd], Array.from(b.horzSweep()));
      assert.deepStrictEqual([a.vertSweepStart, a.vertSweepEnd], Array.from(b.vertSweep()));
      assert.deepStrictEqual([a.boundsMinU, a.boundsMinV, a.boundsMaxU, a.boundsMaxV], Array.from(b.getUVBounds()));
    });
  });

  t('test_retired_tight_bounding_box_l1', () => {
    // L1 (map): getTightBoundingBox -> getBoundingBox(true); G115: a virtual override's JS
    // registration no longer hides the base's overloads
    const ob = r.Brep.createFromBoundingBox(new r.BoundingBox([0, 0, 0], [1, 2, 3])).getTightBoundingBox();
    const nb = boxBrep().getBoundingBox(true);
    assert.deepStrictEqual([ob.min[0], ob.max[1], ob.max[2]], [nb.Min.X, nb.Max.Y, nb.Max.Z]);
    const f = ctx.readModel('textEntities_r8.3dm');
    const ann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBaseX);
    ann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === ann.dimensionStyleId);
    assert.deepStrictEqual(ann.getBoundingBox(true), ann.getBoundingBox(I()));       // both reach the annotation's box
    assert(ann.getBoundingBox(r.PlaneX.worldXY));                                      // the plane overload too
    const pc = new r.PolyCurveX();
    pc.append(new r.LineCurveX({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }));
    const d = pc.duplicate();
    assert(d instanceof r.PolyCurveX && d.segmentCount === 1);                         // through CurveX's registration
  });

  t('test_annotation_font_and_held_back_c_members_g115', () => {
    // G115: AnnotationBase.font (a managed font); C's getFont / setFont(style, font) -> font
    const f = ctx.readModel('textEntities_r8.3dm');
    const old = ctx.readOld('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBaseX);
    const oann = oldGeoms('textEntities_r8.3dm').find(g => g instanceof r.AnnotationBase);
    const ostyle = oldList(call(old, 'dimstyles')).find(s => String(s.id) === String(oann.dimensionStyleId));
    nann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    const of = oann.getFont(ostyle), nf = nann.font;
    assert.deepStrictEqual([of.familyName, of.faceName, of.bold], [nf.familyName, nf.faceName, nf.bold]);
    assert(nann.firstCharFont.familyName);
    oann.setFont(ostyle, new r.Font('Courier New'));
    nann.font = new r.FontX('Courier New');
    assert.strictEqual(nann.font.familyName, oann.getFont(ostyle).familyName);
    ctx.raises(() => { nann.font = null; });
  });

  t('test_file3dm_earth_anchor_point_g115', () => {
    // G115: File3dm.earthAnchorPoint, a copy out and a copy in; the old settings.earthAnchorPoint agrees
    const f = ctx.readModel('file3dm_stuff.3dm');
    const o = call(call(ctx.readOld('file3dm_stuff.3dm'), 'settings'), 'earthAnchorPoint');
    const a = f.earthAnchorPoint;
    assert.deepStrictEqual([a.earthBasepointLatitude, a.earthBasepointLongitude], [o.earthBasepointLatitude, o.earthBasepointLongitude]);
    a.earthBasepointLatitude = 12.5;
    assert.strictEqual(f.earthAnchorPoint.earthBasepointLatitude, o.earthBasepointLatitude);   // a copy
    f.earthAnchorPoint = a;
    assert.strictEqual(r.File3dmX.fromByteArray(f.toByteArray(null)).earthAnchorPoint.earthBasepointLatitude, 12.5);
  });

  t('test_translated_setters_beside_planned_getters_g115', () => {
    // G115: MeshVertexList.count is settable (D1: setCount -> count)
    const m = new r.MeshX(), om = new r.Mesh();
    for (let i = 0; i < 4; i++) { m.vertices.add(i, 0, 0); om.vertices().add(i, 0, 0); }
    m.vertices.count = 2; om.vertices().setCount(2);
    assert.strictEqual(m.vertices.count, om.vertices().count);
    m.vertices.count = -1;
    assert.strictEqual(m.vertices.count, 2);
    const v = new r.ViewportX();
    v.isPerspectiveProjection = false;
    assert.strictEqual(v.isParallelProjection, true);
  });

  t('test_retired_file3dm_tables_and_settings_h1', () => {
    // H1 (map): renderContent table -> renderMaterials / Environments / Textures (+ find); settings
    // modelBasePoint / earthAnchorPoint; renderEnvironments.* -> RenderSettings members; addObject -> add
    const old = ctx.readOld('file3dm_stuff.3dm'), f = ctx.readModel('file3dm_stuff.3dm');
    const rc = call(old, 'renderContent');
    const oids = []; for (let i = 0; i < rc.count; i++) oids.push(String(rc.get(i).id));
    const kinds = [f.renderMaterials, f.renderEnvironments, f.renderTextures];
    assert(oids.length > 0);
    assert.deepStrictEqual(oids.sort(), kinds.flatMap(k => list(k).map(c => c.id)).sort());
    for (const id of oids) {
      const hit = kinds.map(k => k.find(id)).filter(x => x !== null && x !== undefined);
      assert(hit.length === 1 && hit[0].id === String(rc.findId(id).id));
    }
    for (let i = 0; i < rc.count; i++) {
      const nc = f.manifest.findIndex(i, r.ModelComponentType.RenderContent);   // FindIndex -> the manifest's
      assert(nc instanceof r.File3dmRenderContentX && nc.id === String(rc.get(i).id));
    }
    const os = call(old, 'settings');
    const ob = os.modelBasePoint, nb = f.settings.modelBasepoint;
    assert.deepStrictEqual([ob[0], ob[1], ob[2]], [nb.X, nb.Y, nb.Z]);
    assert.strictEqual(os.earthAnchorPoint.earthBasepointLongitude, f.earthAnchorPoint.earthBasepointLongitude);
    const U = r.RenderSettingsEnvironmentUsage, Pp = r.RenderSettingsEnvironmentPurpose;
    const oe = call(call(os, 'renderSettings'), 'renderEnvironments'), rs = f.settings.renderSettings;
    for (const [n, u] of [['background', U.Background], ['reflection', U.Reflection], ['skylighting', U.Skylighting]])
      assert.strictEqual(String(oe[n + 'Id']), rs.renderEnvironmentId(u, Pp.Standard), n);
    for (const [n, u] of [['reflection', U.Reflection], ['skylighting', U.Skylighting]])
      assert.strictEqual(oe[n + 'Override'], rs.renderEnvironmentOverride(u), n);
    const eid = uuid4();
    rs.setRenderEnvironmentId(U.Skylighting, eid); rs.setRenderEnvironmentOverride(U.Skylighting, true);
    const brs = r.File3dmX.fromByteArray(f.toByteArray()).settings.renderSettings;
    assert.strictEqual(brs.renderEnvironmentId(U.Skylighting, Pp.Standard), eid);
    assert.strictEqual(brs.renderEnvironmentOverride(U.Skylighting), true);
    const ot = old.objects(), n0 = ot.count, m0 = list(f.objects).length;
    ot.addObject(ot.get(0));                          // AddObject(obj) -> add(File3dmObject)
    const no = list(f.objects)[0];
    f.objects.add(no);
    assert.strictEqual(old.objects().count, n0 + 1);
    assert.strictEqual(list(f.objects).length, m0 + 1);
    const nid = f.objects.add(no.geometry, null);     // Add(geometry, attributes) (G117), attributes optional
    assert(f.objects.findId(nid).geometry instanceof r.GeometryBaseX);
    // a leader too: RhinoCommon's Add throws for it, rhino3dm keeps 8.x's hand path (G119, D71)
    const S = new r.DimensionStyleX();
    const le = r.LeaderX.create('x', r.PlaneX.worldXY, S, [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }]);
    const lid = f.objects.add(le, null);
    assert(f.objects.findId(lid).geometry instanceof r.LeaderX);
  });

  t('test_retired_mesh_modifiers_i1_and_shut_lining_i3', () => {
    // I1: attributes.meshModifiers -> file3dmMeshModifiers; I3: the shut-lining curve table -> id-based members
    const oa = new r.ObjectAttributes();
    const sl = call(oa, 'meshModifiers').createShutLining();
    sl.on = true;
    const cid = uuid4();
    call(sl, 'curves').add(cid);
    const c = call(sl, 'curves').findId(cid);
    c.radius = 0.75; c.profile = 2; c.enabled = true; c.pull = true; c.isBump = true;
    const nsl = new r.ObjectAttributesX(oa).file3dmMeshModifiers.shutLining;
    assert.strictEqual(nsl.on, true);
    assert.deepStrictEqual(list(nsl.getCurves()), [cid]);
    assert.strictEqual(String(call(sl, 'curves').findIndex(0).id), cid);
    assert.deepStrictEqual([nsl.curveRadius(cid), nsl.curveProfile(cid), nsl.curveEnabled(cid), nsl.curvePull(cid), nsl.curveIsBump(cid)],
                           [c.radius, c.profile, c.enabled, c.pull, c.isBump]);
    nsl.setCurveRadius(cid, 1.25);
    assert.strictEqual(nsl.curveRadius(cid), 1.25);
  });

  t('test_retired_annotation_geometry_j1', () => {
    // J1 (map): leader.points -> points3D; dim.points (an object of named points) -> get3dPoints()
    // (an array led by the success flag: D68); DimAngular.angle -> numericValue
    const P = (X, Y, Z) => ({ X, Y, Z });
    const f = new r.File3dmX();
    f.allDimStyles.add(new r.DimensionStyleX());
    const style = list(f.allDimStyles)[0];
    const W = r.PlaneX.worldXY;
    const geos = [
      r.LinearDimensionX.create(r.AnnotationType.Aligned, style, W, P(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0),
      r.AngularDimensionX.create(style.id, W, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0)),
      r.RadialDimensionX.create(style, r.AnnotationType.Radius, W, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)),
      r.LeaderX.create('note', W, style, [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])];
    f.objects.add(geos[0], null);                      // RhinoCommon's add(geometry, attributes) takes these two (G117)
    f.objects.add(geos[1], null);
    f.objects.addRadialDimension(geos[2]);
    f.objects.addLeader('note', W, [{ X: 0, Y: 0 }, { X: 2, Y: 1 }, { X: 4, Y: 1 }], null);
    assert.strictEqual(list(geos[3].points3D)[2].X, 4);   // Create's own points
    const data = f.toByteArray();
    const olds = []; const ot = r.File3dm.fromByteArray(data).objects();
    for (let i = 0; i < ot.count; i++) olds.push(ot.get(i).geometry());
    const news = list(r.File3dmX.fromByteArray(data).objects).map(o => o.geometry);
    const rnd = v => Math.round(v * 1e9) / 1e9;
    const xyz = q => (Array.isArray(q) ? q.map(rnd) : [rnd(q.X), rnd(q.Y), rnd(q.Z)]);
    const ld = news.find(g => g instanceof r.LeaderX), old = olds.find(g => g instanceof r.Leader);
    assert.deepStrictEqual(Array.from(old.points).map(xyz), list(ld.points3D).map(xyz));
    const keys = [[r.LinearDimensionX, r.DimLinear, ['defpt1', 'defpt2', 'arrowpt1', 'arrowpt2']],
                  [r.AngularDimensionX, r.DimAngular, ['centerpt', 'defpt1', 'defpt2', 'arrowpt1', 'arrowpt2', 'dimlinept', 'textpt']]];
    for (const [N, O, ks] of keys) {
      const n = news.find(g => g instanceof N), o = olds.find(g => g instanceof O);
      const rc = n.get3dPoints(), op = o.points;
      assert.strictEqual(rc[0], true);
      ks.forEach((k, i) => assert.deepStrictEqual(xyz([op[k].X, op[k].Y, op[k].Z]), xyz(rc[i + 1]), k));
    }
    const an = news.find(g => g instanceof r.AngularDimensionX), ao = olds.find(g => g instanceof r.DimAngular);
    approx(ao.angle, an.numericValue, 1e-12, 'angle');
  });

  t('test_retired_set_point3d_at_d2', () => {
    // D2 (rename): vertices.setPoint3dAt(i, p) -> setVertex(i, Point3d)
    const om = new r.Mesh(), nm = new r.MeshX();
    // a coordinate float cannot hold picks Add(double, ..), as C# does for a double: the mesh keeps
    // double precision, as the Python test's Add(float(i), ..) does (whole numbers pick Add(float, ..))
    for (let i = 0; i < 3; i++) { om.vertices().add(i + 0.1, 0, 0); nm.vertices.add(i + 0.1, 0, 0); }
    om.vertices().setPoint3dAt(1, [0.1, 0.2, 0.3]);
    nm.vertices.setVertex(1, { X: 0.1, Y: 0.2, Z: 0.3 });
    const o = om.vertices().point3dAt(1), n = nm.vertices.point3dAt(1);
    assert.deepStrictEqual([o[0], o[1], o[2]], [n.X, n.Y, n.Z]);
  });

  t('test_retired_rhino3dm_only_overloads_from_the_audit', () => {
    // Luis-accepted maps from the overload audit (the JS halves; AddPoint(x, y, z, attrs) was Python-only)
    const f = ctx.readModel('textEntities_r8.3dm'), old = ctx.readOld('textEntities_r8.3dm');
    const nann = objs(f).map(o => o.geometry).find(g => g instanceof r.AnnotationBaseX);
    const oann = oldGeoms('textEntities_r8.3dm').find(g => g instanceof r.AnnotationBase);
    const ostyle = oldList(call(old, 'dimstyles')).find(s => String(s.id) === String(oann.dimensionStyleId));
    nann.parentDimensionStyle = list(f.allDimStyles).find(s => s.id === nann.dimensionStyleId);
    assert.strictEqual(oann.runReplace(ostyle, 'ZZ', 0, 0, 0, 1), nann.runReplace('ZZ', 0, 0, 0, 1));
    assert.strictEqual(oann.plainText, nann.plainText);
    const ob = r.Brep.createFromBoundingBox(new r.BoundingBox([0, 0, 0], [1, 2, 3])).getBoundingBox();
    const nb = boxBrep().getBoundingBox(false);                     // getBoundingBox() -> (false)
    assert.deepStrictEqual([ob.min[0], ob.max[1], ob.max[2]], [nb.Min.X, nb.Max.Y, nb.Max.Z]);
    const om = new r.Material(), nm = new r.MaterialX();
    const ot = new r.Texture(), nt = new r.TextureX();
    ot.fileName = nt.fileName = '/tmp/bump.png';
    // the old setter takes an int (pilot #52: the enum object itself silently sets None in 8.x JS)
    ot.textureType = r.TextureType.Bump.value; nt.textureType = r.TextureType.Bump;
    om.setTexture(ot);
    assert.strictEqual(nm.setTexture(nt, nt.textureType), true);  // setTexture(t) -> (t, t.textureType)
    assert.strictEqual(om.getTexture(r.TextureType.Bump).fileName, nm.getTexture(r.TextureType.Bump).fileName);
    const [, ns] = [null, objs(ctx.readModel('subd_creases.3dm')).map(o => o.geometry).find(g => g instanceof r.SubDX)];
    const os = oldGeoms('subd_creases.3dm').find(g => g instanceof r.SubD);
    const oes = []; const it = os.edges(); for (let c = it.first(), i = 0; i < it.count; c = it.next(), i++) oes.push(c);
    oes.sort((a, b) => a.index - b.index);
    const nes = [...ns.edges].sort((a, b) => a.id - b.id);
    assert.deepStrictEqual(oes.map(e => [e.endSharpness(0), e.endSharpness(1)]), nes.map(e => [e.endSharpness(0, false), e.endSharpness(1, false)]));
  });

  t('test_retired_mesh_modifier_creators_i2_and_add_curve_rh99854', () => {
    // RH-99854: File3dmMeshModifiers.create*() and File3dmShutLining.addCurve(id) (in memory: pilot #43)
    const na = new r.ObjectAttributesX(), nmm = na.file3dmMeshModifiers;
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
    // createFromSubDControlNet(subd, flag) -> createFromSubDControlNet / ...WithTextureCoordinates (Luis 2026-10-29)
    for (const name of ['subd.3dm', 'subdBox.3dm', 'subd_creases.3dm']) {
      const ns = objs(ctx.readModel(name)).map(o => o.geometry).find(g => g instanceof r.SubDX);
      const os = oldGeoms(name).find(g => g instanceof r.SubD);
      for (const [flag, make] of [[false, s => r.MeshX.createFromSubDControlNet(s)], [true, s => r.MeshX.createFromSubDControlNetWithTextureCoordinates(s)]]) {
        const o = r.Mesh.createFromSubDControlNet(os, flag), n = make(ns);
        assert.deepStrictEqual([call(o, 'vertices').count, call(o, 'faces').count, call(o, 'textureCoordinates').count],
                               [n.vertices.count, n.faces.count, n.textureCoordinates.count], name + ' ' + flag);
      }
    }
  });

  t('test_retired_file3dm_to_byte_array_options_js3', () => {
    // JS3 (map): File3dm.toByteArrayOptions(options) -> File3dmX.toByteArray(options) (D54)
    const old = ctx.readOld('file3dm_stuff.3dm');
    const f = ctx.readModel('file3dm_stuff.3dm');
    const oo = new r.File3dmWriteOptions(); oo.version = 7;
    const no = new r.File3dmWriteOptionsX(); no.version = 7;
    const ob = r.File3dmX.fromByteArray(old.toByteArrayOptions(oo));
    const nb = r.File3dmX.fromByteArray(f.toByteArray(no));
    assert.strictEqual(ob.archiveVersion, nb.archiveVersion);
    assert.strictEqual(list(ob.objects).length, list(nb.objects).length);
  });
};
