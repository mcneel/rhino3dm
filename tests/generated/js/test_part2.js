// JS port of tests/generated/test_generated.py, part 2:
// test_subd_neighbours_replace_the_old_component_iterators .. test_point_cloud_item_coordinates_chain_through_location
// (same names, same contracts, against the compiled WASM module).
const crypto = require('crypto');

module.exports = (t, ctx) => {
  const { r, assert, approx, list } = ctx;
  const P = (x, y, z) => ({ X: x, Y: y, Z: z });
  const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const isUuid = v => typeof v === 'string' && UUID_RE.test(v);

  // --- helpers ported from test_generated.py --------------------------------
  const box_brep = () => r.Brep.createFromBox(r.BoundingBox.create(0, 0, 0, 1, 2, 3));
  // a list type registered with count/get but no iterator (BrepLoopList, BrepTrimList ...)
  const items = coll => {
    if (coll && typeof coll.iterator === 'function') return list(coll);
    const out = [];
    for (let i = 0; i < coll.count; i++) out.push(coll.get(i));
    return out;
  };
  const subd_from_file = () => {
    const f = ctx.readModel('subdBox.3dm');
    for (const o of list(f.objects)) {
      const g = o.geometry;
      if (g instanceof r.SubD) return [f, g];
    }
    ctx.skip('no SubD in subdBox.3dm');
  };
  const quad_mesh = () => {
    const m = new r.Mesh();
    for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]]) m.vertices.add(x, y, z);
    m.faces.addFace(0, 1, 2, 3);
    return m;
  };
  const cloudOf = pts => {
    const pc = new r.PointCloud();
    for (const [x, y, z] of pts) pc.add(P(x, y, z));
    return pc;
  };
  const sphereX = (c, rad) => r.Sphere.create(c, rad);
  const circleX = (c, rad) => r.Circle.create(c, rad);

  // --- components --------------------------------------------------------------

  t('test_subd_neighbours_replace_the_old_component_iterators', () => {
    const [, s] = subd_from_file();
    const face = list(s.faces)[0];
    const edges = [], verts = [];
    for (let i = 0; i < face.edgeCount; i++) edges.push(face.edgeAt(i));
    for (let i = 0; i < face.vertexCount; i++) verts.push(face.vertexAt(i));
    assert(edges.length === verts.length && verts.length === face.edgeCount && face.edgeCount >= 3);
    const v = verts[0];
    for (let i = 0; i < v.edgeCount; i++) assert(v.edgeAt(i) !== null && v.edgeAt(i) !== undefined);
    for (let i = 0; i < v.faceCount; i++) assert(v.faceAt(i) !== null && v.faceAt(i) !== undefined);
    const e = edges[0];
    for (let i = 0; i < e.faceCount; i++) assert(e.faceAt(i) !== null && e.faceAt(i) !== undefined);
    ctx.raises(() => v.edgeAt(-1), /out_of_range,index cannot be negative/);
    ctx.raises(() => v.edgeAt(v.edgeCount), /out_of_range,index is greater than or equal to EdgeCount/);
    assert.strictEqual(s.vertices.find(0), null);          // ids start at 1
    assert(s.vertices.find(1) !== null);
  });

  // --- G46: native-backed classes ------------------------------------------------

  t('test_environment_owns_its_object_and_fills_a_new_texture', () => {
    const e = new r.Environment();
    assert(e.backgroundProjection !== null && e.backgroundProjection !== undefined);
    const tex = e.backgroundImage;
    assert(tex instanceof r.Texture);
    assert(e.backgroundImage !== tex);                       // a new texture per access, as in .NET
  });

  t('test_file_reference_is_a_handle_with_rhinocommons_paths', () => {
    const fr = r.FileReference.createFromFullAndRelativePaths('/tmp/textures/wood.png', 'textures/wood.png');
    assert(fr !== null && fr.isSet === true);
    assert.strictEqual(fr.fullPath, '/tmp/textures/wood.png');
    assert.strictEqual(fr.relativePath, 'textures/wood.png');
    fr.delete();                                              // freed through ON_FileReference_Delete
  });

  t('test_subd_components_have_ids_and_status_from_their_base', () => {
    const [, s] = subd_from_file();
    const faces = list(s.faces);
    const ids = faces.map(x => x.id);
    assert(new Set(ids).size === ids.length && Math.min(...ids) >= 1);
    assert(faces[0] instanceof r.SubDComponent);
    assert.strictEqual(s.faces.find(ids[0]).id, ids[0]);
    const v = list(s.vertices)[0];
    assert(v.isSelected === false || v.isSelected === true);
    assert.strictEqual(v.isDeleted, false);
  });

  t('test_mesh_modifiers_are_children_of_the_attributes', () => {
    const a = new r.ObjectAttributes();
    const mm = a.file3dmMeshModifiers;
    assert.strictEqual(mm.displacement, null);
    assert.strictEqual(mm.edgeSoftening, null);
    assert.strictEqual(mm.thickening, null);
    assert.strictEqual(mm.curvePiping, null);
    assert.strictEqual(mm.shutLining, null);
    for (const name of ['subd.3dm', 'subd_creases.3dm', 'mesh.3dm']) {
      const f = ctx.readModel(name);
      for (const o of list(f.objects)) {
        const m = o.attributes.file3dmMeshModifiers;
        const d = m.displacement;
        if (d !== null) assert(d.on === true || d.on === false);
      }
    }
  });

  // --- G47: constructor-specialized lists (Brep.Loops / Brep.Trims) -------------

  t('test_brep_loops_and_trims_are_reachable_components', () => {
    const b = box_brep();
    const loops = b.loops, trims = b.trims;
    assert.strictEqual(loops.count, 6);
    assert.strictEqual(trims.count, 24);                     // a box: one loop per face, 4 trims each
    assert.deepStrictEqual(items(loops).map(l => l.loopIndex), [0, 1, 2, 3, 4, 5]);
    for (const tr of items(trims)) assert(tr instanceof r.BrepTrim && tr instanceof r.Curve);
    assert.strictEqual(trims.get(5).trimIndex, 5);
    assert.strictEqual(loops.get(0).face.faceIndex, 0);     // BrepLoop.Face: through the brep's face list
    ctx.raises(() => loops.get(6), /out_of_range/);
  });

  // --- G48: borrowed pointers, literal constants ------------------------------------

  t('test_cached_texture_coordinates_are_a_view_into_the_mesh', () => {
    const f = ctx.readModel('meshWithTexture.3dm');
    const objs = list(f.objects);
    const meshes = objs.map(o => o.geometry).filter(g => g instanceof r.Mesh);
    if (!meshes.length) ctx.skip('no mesh in meshWithTexture.3dm');
    assert.strictEqual(meshes[0].getCachedTextureCoordinates(crypto.randomUUID()), null);
    const ids = new Set(objs.map(o => o.attributes.objectId));
    for (const m of meshes) {
      for (const mid of ids) {
        const tc = m.getCachedTextureCoordinates(mid);
        if (tc !== null) {
          assert(tc.isReadOnly === true && tc.count >= 0 && (tc.dim === 2 || tc.dim === 3));
          assert.deepStrictEqual(tc.tryGetAt(-1), [false, -1.0, -1.0, -1.0]);   // RhinoCommon's -1 defaults
          return;
        }
      }
    }
  });

  t('test_literal_constant_properties', () => {
    const lt = new r.Linetype();
    assert.strictEqual(lt.isModified, false);
    assert.strictEqual(lt.isPatternLocked, false);
    assert.strictEqual(ctx.val(r.Material.maxShine), 255.0);
  });

  // --- G49: category N idioms ------------------------------------------------------

  t('test_texture_mapping_factories_return_none_on_failure', () => {
    const i = { T0: 0, T1: 1 };
    const tm = r.TextureMapping.createPlaneMapping(r.Plane.worldXY, i, i, i, true);
    assert(tm instanceof r.TextureMapping && tm.capped === true);
    assert(r.TextureMapping.createPlaneMapping(r.Plane.worldXY, i, i, i) instanceof r.TextureMapping);
    assert.strictEqual(r.TextureMapping.createPlaneMapping(r.Plane.unset, i, i, i, false), null);   // origin unset
    assert(r.TextureMapping.createSurfaceParameterMapping() instanceof r.TextureMapping);
    assert(r.TextureMapping.createSphereMapping(sphereX(P(0, 0, 0), 2)) instanceof r.TextureMapping);
  });

  t('test_extrusion_caps_decode_the_native_flags', () => {
    const cyl = r.Cylinder.create(circleX(P(0, 0, 0), 1.0), 4.0);
    for (const [bottom, top] of [[false, false], [true, false], [false, true], [true, true]]) {
      const e = r.Extrusion.createCylinderExtrusion(cyl, bottom, top);
      assert.deepStrictEqual([e.isCappedAtBottom, e.isCappedAtTop], [bottom, top]);
      assert.deepStrictEqual([e.isMiteredAtStart, e.isMiteredAtEnd], [false, false]);
    }
  });

  t('test_out_param_tuples_keep_the_csharp_result', () => {
    const a = circleX(P(0, 0, 0), 1.0);
    const b = circleX(P(1, 0, 0), 1.0);
    const res = r.Intersection.circleCircle(a, b);
    assert.strictEqual(res.length, 3);
    assert.strictEqual(res[0], r.CircleCircleIntersection.Multiple);
    assert.strictEqual(res[0].value, 2);
    const pc = new r.PolyCurve();
    const seg = pc.segmentIndexes({ T0: 0, T1: 1 });
    assert(seg.length === 3 && Number.isInteger(seg[0]));
  });

  t('test_value_out_initializers_are_reproduced', () => {
    const xf = new r.Texture().uvwTransform;
    assert.deepStrictEqual([xf.M00, xf.M11, xf.M22, xf.M33], [1.0, 1.0, 1.0, 1.0]);
    const tm = r.TextureMapping.createSurfaceParameterMapping();
    const res = tm.evaluate(P(0.25, 0.5, 0), P(0, 0, 1));
    assert(res.length === 2 && Number.isInteger(res[0]));
  });

  t('test_parameter_normalization_conditional_args_and_ordered_throws', () => {
    const ns = r.NurbsSurface.createFromSphere(sphereX(P(0, 0, 0), 2.0));
    const d1 = ns.domain(1), d7 = ns.domain(7);
    assert.deepStrictEqual([d1.T0, d1.T1], [d7.T0, d7.T1]);
    const ku = ns.knotsU;
    assert.strictEqual(typeof ku.superfluousKnot(true), 'number');
    assert(ku.superfluousKnot(true) <= ku.superfluousKnot(false));
    const cloud = cloudOf([[0, 0, 0], [1, 0, 0], [2, 0, 0]]);
    assert.strictEqual(cloud.pointAt(1).X, 1);
    for (const bad of [-1, 3]) ctx.raises(() => cloud.pointAt(bad), /out_of_range,index must be (larger|smaller)/);
  });

  t('test_guard_message_after_nameof', () => {
    const [, s] = subd_from_file();
    const e = list(s.edges)[0];
    assert.strictEqual(typeof e.endSharpness(0, false), 'number');
    ctx.raises(() => e.endSharpness(2, false), /out_of_range,endIndex must be 0 or 1/);
  });

  t('test_value_classes_adopt_native_results', () => {
    const fast = ctx.val(r.MeshingParameters.fastRenderMesh);
    const quality = ctx.val(r.MeshingParameters.qualityRenderMesh);
    assert(fast instanceof r.MeshingParameters && quality instanceof r.MeshingParameters);
    assert.strictEqual(typeof ctx.val(r.MeshingParameters.defaultAnalysisMesh).relativeTolerance, 'number');
    const attrs = new r.ObjectAttributes();
    assert.strictEqual(attrs.customMeshingParameters, null);
    attrs.customMeshingParameters = quality;
    const got = attrs.customMeshingParameters;
    assert(got !== null && got.relativeTolerance === quality.relativeTolerance);
    const nc = r.NurbsCurve.createFromCircle(circleX(P(0, 0, 0), 1.0));
    assert(nc.convertSpanToBezier(0) instanceof r.BezierCurve);
    assert.strictEqual(nc.convertSpanToBezier(99), null);
  });

  // --- G53/G54 ------------------------------------------------------------------------

  t('test_array_fills_return_what_the_native_wrote', () => {
    const cloud = cloudOf([[0, 0, 0], [1, 2, 0], [2, 4, 0]]);
    const pts = cloud.getPoints();
    assert.deepStrictEqual(Array.from(pts, p => [p.X, p.Y]), [[0, 0], [1, 2], [2, 4]]);
    assert.deepStrictEqual(Array.from(cloud.getNormals()), []);      // `return new Vector3d[0];`
    assert.strictEqual(new r.ObjectAttributes().getGroupList(), null);  // `if (count < 1) return null;`
    const mx = quad_mesh();
    assert.deepStrictEqual(Array.from(mx.vertexColors.toARGBArray()), []);
    assert.deepStrictEqual(Array.from(mx.topologyEdges.getConnectedFaces(0)), [0]);
    const ns = r.NurbsSurface.createFromSphere(sphereX(P(0, 0, 0), 2.0));
    const sv = Array.from(ns.getSpanVector(0));
    assert.strictEqual(sv.length, ns.spanCount(0) + 1);
    assert.deepStrictEqual(sv, [...sv].sort((x, y) => x - y));
  });

  t('test_native_call_sequences', () => {
    const mx = quad_mesh();
    mx.vertices.hideAll();
    assert.strictEqual(mx.vertices.count, 4);
    mx.vertices.clear();
    assert.strictEqual(mx.vertices.count, 0);
    const tr = box_brep().trims.get(0);
    tr.setTolerances(0.25, 0.5);
  });

  t('test_colors_from_out_locals_and_enum_results', () => {
    const item = cloudOf([[0, 0, 0]]).get(0);
    const c = item.color;
    assert.strictEqual(Object.keys(c).length, 4);
    if (r.SubDFace) {
      const [, s] = subd_from_file();
      const pfc = list(s.faces)[0].perFaceColor;
      assert.deepStrictEqual([pfc.r, pfc.g, pfc.b, pfc.a], [0, 0, 0, 0]);
    }
    assert.strictEqual(quad_mesh().faces.convertNonPlanarQuadsToTriangles(0.01, 0.1, 0), 0);
  });

  t('test_converted_array_fill_and_result_select', () => {
    assert.deepStrictEqual(Array.from(cloudOf([[0, 0, 0]]).getColors()), []);
    const tx = new r.Texture();
    tx.wrapU = r.TextureUvwWrapping.Clamp;
    assert.strictEqual(tx.wrapU, r.TextureUvwWrapping.Clamp);
    tx.wrapU = r.TextureUvwWrapping.Repeat;
    assert.strictEqual(tx.wrapU, r.TextureUvwWrapping.Repeat);
  });

  t('test_guard_only_helpers_become_guards', () => {
    const nc = r.NurbsCurve.createFromCircle(circleX(P(0, 0, 0), 1.0));
    const pts = nc.points;
    assert.strictEqual(typeof pts.getWeight(0), 'number');
    for (const bad of [-1, pts.count]) ctx.raises(() => pts.getWeight(bad), /out_of_range,Index must be (greater|less)/);
    const ns = r.NurbsSurface.createFromSphere(sphereX(P(0, 0, 0), 2.0));
    const res = ns.points.getPoint(0, 0);
    assert.strictEqual(res.length, 2);
    const [ok, p] = res;
    assert.strictEqual(ok, true);
    assert(typeof p.X === 'number' && typeof p.Y === 'number' && typeof p.Z === 'number');
    ctx.raises(() => ns.points.getPoint(ns.points.countU, 0), /out_of_range,u must be less than CountU/);
  });

  // --- G57 ------------------------------------------------------------------------------

  t('test_guards_in_tuple_members_and_result_clamps', () => {
    const circle = circleX(P(0, 0, 0), 1.0);
    const rc = r.Intersection.lineCircle({ From: P(-2, 0, 0), To: P(2, 0, 0) }, circle);
    assert.strictEqual(rc.length, 5);
    assert.strictEqual(rc[0].value, 2);                                  // Multiple
    const bad = r.Intersection.lineCircle({ From: P(0, 0, 0), To: P(0, 0, 0) }, circle);
    assert.strictEqual(bad[0].value, 0);
    assert.strictEqual(bad[1], 0.0);
    assert.strictEqual(bad[3], 0.0);
    const a = sphereX(P(0, 0, 0), 1.0), b = sphereX(P(1, 0, 0), 1.0);
    const ss = r.Intersection.sphereSphere(a, b);
    assert.strictEqual(ss.length, 2);
    assert.strictEqual(ss[0].value, 2);                                  // Circle
    assert(ss[1] && typeof ss[1].Radius === 'number' && ss[1].Plane !== undefined);   // a Circle value
    const far = r.Intersection.sphereSphere(a, sphereX(P(10, 0, 0), 1.0));
    assert.strictEqual(far[0].value, 0);
  });

  t('test_owned_objects_are_never_document_controlled', () => {
    assert(isUuid(r.TextureMapping.createSurfaceParameterMapping().id));
  });

  t('test_material_textures_are_views_copied_on_write', () => {
    const m = new r.Material();
    assert.strictEqual(m.getBitmapTexture(), null);
    const tx = new r.Texture();
    tx.fileName = 'a.png';
    assert(m.setBitmapTexture(tx));
    const v = m.getBitmapTexture();
    assert.strictEqual(v.fileName, 'a.png');
    v.fileName = 'c.png';
    assert.strictEqual(v.fileName, 'c.png');
    assert.strictEqual(m.getBitmapTexture().fileName, 'a.png');
    assert(m.setBitmapTexture(v));
    assert.strictEqual(m.getBitmapTexture().fileName, 'c.png');
  });

  t('test_child_geometry_views_follow_rhinocommon', () => {
    const pc = new r.PolyCurve();
    pc.append(new r.LineCurve(P(0, 0, 0), P(2, 0, 0)));
    pc.append(new r.LineCurve(P(2, 0, 0), P(2, 3, 0)));
    const seg = pc.segmentCurve(0);
    assert(seg instanceof r.LineCurve);
    seg.reverse();
    const s = pc.segmentCurve(0).pointAtStart;
    assert.deepStrictEqual([s.X, s.Y], [2, 0]);
    assert.strictEqual(pc.segmentCurve(5), null);
    const face = box_brep().faces.get(0);
    const srf = face.underlyingSurface();
    const before = face.underlyingSurface().pointAt(0.0, 0.0);
    srf.translate(P(0, 0, 5));
    const moved = srf.pointAt(0.0, 0.0);
    const after = face.underlyingSurface().pointAt(0.0, 0.0);
    assert.strictEqual(moved.Z, before.Z + 5);
    assert.deepStrictEqual([after.X, after.Y, after.Z], [before.X, before.Y, before.Z]);
    assert.strictEqual(face.getMesh(r.MeshType.Render), null);
  });

  t('test_set_mesh_hands_the_mesh_to_the_face', () => {
    const face = box_brep().faces.get(0);
    const m = quad_mesh();
    assert.strictEqual(face.setMesh(r.MeshType.Render, m), true);
    const got = face.getMesh(r.MeshType.Render);
    assert(got !== null && got.vertices.count === 4);
    assert.strictEqual(m.vertices.count, 4);
    m.vertices.clear();                                        // a write: m detaches
    assert.strictEqual(m.vertices.count, 0);
    assert.strictEqual(face.getMesh(r.MeshType.Render).vertices.count, 4);
  });

  t('test_decals_iterate_a_snapshot_of_copies', () => {
    const f = ctx.readModel('sphereDecals.3dm');
    const objs = list(f.objects);
    const decals = list(objs[0].attributes.decals);
    assert.strictEqual(decals.length, 2);
    for (const d of decals) assert.strictEqual(d.mapping, r.DecalMapping.UV);
    const ids = decals.map(d => d.textureInstanceId);
    assert.strictEqual(new Set(ids).size, 2);
    // Python: `del f, objs; gc.collect()` -- here the wrappers are deleted explicitly
    for (const o of objs) o.delete();
    f.delete();
    assert.deepStrictEqual(decals.map(d => d.textureInstanceId), ids);
  });

  t('test_decals_of_attributes_are_writable_with_rhinocommons_guards', () => {
    const a = new r.ObjectAttributes();
    assert.deepStrictEqual(list(a.decals), []);
    a.decals.removeAllDecals();
    a.decals.clear();
    assert.strictEqual(a.decals.remove(null), false);         // `if (decal == null) return false;`
  });

  t('test_component_index_value_type', () => {
    const CI = r.ComponentIndex;
    const ci = CI.create(r.ComponentIndexType.BrepFace, 3);
    assert.strictEqual(ci.Index, 3);
    assert.strictEqual(CI.componentIndexType(ci), r.ComponentIndexType.BrepFace);
    assert.strictEqual(CI.isSet(ci), true);
    const u = CI.unset;                                          // `new ComponentIndex(InvalidType, -1)`
    assert(CI.isUnset(u) && !CI.isUnset(ci) && u.Index === -1);
    assert(CI.equals(ci, CI.create(r.ComponentIndexType.BrepFace, 3)));
    assert(!CI.equals(ci, u) && CI.compareTo(u, ci) < 0);   // != and < : no JS operators (G106)
    const face = box_brep().faces.get(1);
    assert(CI.equals(face.componentIndex(), CI.create(r.ComponentIndexType.BrepFace, 1)));
  });

  t('test_rev_surface_from_rhinocommon_shapes', () => {
    const s = r.RevSurface.createFromSphere(sphereX(P(0, 0, 0), 2.0));
    assert(s instanceof r.RevSurface && s instanceof r.Surface);
    approx(s.angle.T1 - s.angle.T0, 2 * Math.PI, 1e-9);
    assert.strictEqual(s.axisAndCurveCoplanar(), true);
    assert(r.Sphere.toRevSurface(sphereX(P(0, 0, 0), 1.0)) instanceof r.RevSurface);
  });

  t('test_render_channels_of_render_settings', () => {
    const rc = new r.RenderSettings().renderChannels;
    assert.strictEqual(rc.mode, r.RenderChannelsModes.Automatic);
    for (const i of Array.from(rc.customList)) assert(isUuid(i), 'not a uuid: ' + i);
    assert.strictEqual(new r.RenderChannels().mode, r.RenderChannelsModes.Automatic);
  });

  t('test_common_object_is_the_root_of_object_classes', () => {
    const b = box_brep();
    assert(b instanceof r.CommonObject && new r.ObjectAttributes() instanceof r.CommonObject);
    assert.strictEqual(b.isValid, true);
    const [ok, log] = b.isValidWithLog();
    assert(ok === true && typeof log === 'string');
    assert(r.CommonObject.computeMemoryEstimate(b) > 0);
    if (r.CachedTextureCoordinates) {
      assert(!(r.CachedTextureCoordinates.prototype instanceof r.CommonObject));
    }
    const a = new r.ObjectAttributes();
    assert.strictEqual(a.setUserString('k', 'v'), true);
    assert.strictEqual(a.getUserString('k'), 'v');
    assert.strictEqual(a.userStringCount, 1);
  });

  t('test_translated_members_built_from_generated_members', () => {
    const light = new r.Light();
    light.lightStyle = r.LightStyle.WorldPoint;
    assert(light.isPointLight === true && light.isSpotLight === false && light.isLinearLight === false);
    light.lightStyle = r.LightStyle.WorldSpot;
    assert(light.isSpotLight === true && light.isPointLight === false);
    const f = new r.File3dm();
    f.settings.modelAngleToleranceDegrees = 3.0;
    approx(f.settings.modelAngleToleranceRadians, 3.0 * Math.PI / 180, 1e-12);
    approx(f.settings.modelAngleToleranceDegrees, 3.0, 1e-12);
    const b = box_brep();
    assert.strictEqual(b.translate(1.0, 2.0, 3.0), true);
    for (let i = 0; i < b.edges.count; i++) assert.strictEqual(b.edges.get(i).valence, r.EdgeAdjacency.Interior);
  });

  // Python writes to tmp_path; the WASM module's own in-memory file system (MEMFS /tmp) is the
  // equivalent (the FS object is not exported, so no subdirectory: a unique file prefix instead)
  const tmpDir = () => '/tmp/rh3dm_js_' + crypto.randomUUID().slice(0, 8);

  t('test_data_classes_are_rhinocommons_translated_whole', () => {
    const o = new r.File3dmWriteOptions();
    assert(o.saveUserData === true && o.saveRenderMeshes === true && o.version === 0);
    o.enableRenderMeshes(r.ObjectType.AnyObject, false);
    assert.strictEqual(o.saveRenderMeshes, false);
    o.version = 7;
    const f = new r.File3dm();
    const dir = tmpDir();
    const path = dir + '_opts.3dm';
    assert.strictEqual(f.write(path, o), true);
    assert.strictEqual(f.write(dir + '_default.3dm', null), true);
    const [ok, log] = f.writeWithLog(dir + '_log.3dm', null);
    assert(ok === true && typeof log === 'string');
    assert(r.File3dm.read(path) !== null);
    const ps = new r.PageSettings();
    ps.pageNumber = 3; ps.widthMillimeters = 210.0; ps.printerName = 'laser';
    const v = new r.ViewInfo();
    v.setPageSettings(ps);
    const cp = new r.ConstructionPlane();
    assert(cp.gridSpacing === 1.0 && cp.gridLineCount === 70 && cp.thickLineFrequency === 5 && cp.depthBuffered === true);
    assert.deepStrictEqual(cp.plane.Origin, P(0, 0, 0));
    assert.strictEqual(cp.name, '');
    cp.name = 'top';
    assert.strictEqual(cp.name, 'top');
    assert.strictEqual(f.namedConstructionPlanes.count, 0);
  });

  t('test_data_objects_cross_the_native_boundary_both_ways', () => {
    const v = new r.ViewInfo();
    const ps = new r.PageSettings();
    ps.pageNumber = 3; ps.widthMillimeters = 210.0; ps.printerName = 'laser';
    v.setPageSettings(ps);
    const back = v.getPageSettings();
    assert.deepStrictEqual([back.pageNumber, back.widthMillimeters, back.printerName], [3, 210.0, 'laser']);
    const f = new r.File3dm();
    const gd = f.settings.gridDefaults;
    assert(gd !== null && gd.gridLineCount === 70);
    gd.gridLineCount = 12;
    f.settings.gridDefaults = gd;
    assert.strictEqual(f.settings.gridDefaults.gridLineCount, 12);
    const cp = new r.ConstructionPlane();
    cp.name = 'top'; cp.gridSpacing = 2.5;
    f.namedConstructionPlanes.add(cp);
    assert.strictEqual(f.namedConstructionPlanes.count, 1);
    const got = f.namedConstructionPlanes.get(0);
    assert(got.name === 'top' && got.gridSpacing === 2.5);
    // `Write(path, new File3dmWriteOptions { Version = version })` -- the int overload
    const path = tmpDir() + '_v.3dm';
    assert.strictEqual(f.write(path, 7), true);
    const g = r.File3dm.read(path);
    assert.strictEqual(g.namedConstructionPlanes.get(0).name, 'top');
  });

  t('test_find_id_on_the_tables_skips_deleted_components', () => {
    const f = new r.File3dm();
    const layer = new r.Layer();
    layer.name = 'walls';
    f.layers.add(layer);
    const found = f.layers.findIndex(f.layers.count - 1);
    const got = f.layers.findId(found.id);
    assert(got !== null && got.name === 'walls');
    assert.strictEqual(f.layers.findId(crypto.randomUUID()), null);
    const mat = new r.Material();
    f.materials.add(mat);
    const mi = f.materials.count - 1;
    const mid = f.materials.findIndex(mi).id;
    assert(f.materials.findId(mid) !== null);
    f.materials.delete(mi);
    assert.strictEqual(f.materials.findId(mid), null);       // deleted: no result, as in RhinoCommon
  });

  t('test_point_cloud_item_coordinates_chain_through_location', () => {
    const pc = new r.PointCloud();
    pc.add(P(1, 2, 3));
    const item = pc.get(0);
    assert.deepStrictEqual([item.x, item.y, item.z], [1.0, 2.0, 3.0]);
    item.x = 7.5;
    assert.deepStrictEqual(item.location, P(7.5, 2, 3));
    assert.strictEqual(pc.get(0).x, 7.5);
  });
};
