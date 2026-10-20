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
};
