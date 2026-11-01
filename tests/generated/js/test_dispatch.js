// JS overload dispatch (Luis 2026-10-16: PointCloud.add(point, color) shadowed add(point, normal)
// -- an unbraced `return`, a color checked as `true`). Each case calls the overload a shape check
// must pick and verifies the effect that only that overload has. Not in the Python suite: pybind
// dispatches overloads natively.
module.exports = (t, ctx) => {
  const { r, assert } = ctx;
  const P = (x, y, z) => ({ X: x, Y: y, Z: z });

  t('test_js_point_cloud_add_insert_pick_the_overload_by_shape', () => {
    const a = new r.PointCloud();
    a.add(P(0, 0, 0), P(0, 0, 1));                     // (point, normal)
    assert.strictEqual(a.count, 1);
    assert.strictEqual(a.containsNormals, true, 'add(point, normal) must take the normal overload');
    assert.strictEqual(a.containsColors, false);
    const b = new r.PointCloud();
    b.add(P(0, 0, 0), { r: 255, g: 0, b: 0, a: 0 });    // (point, color)
    assert.strictEqual(b.containsColors, true, 'add(point, color) must take the color overload');
    assert.strictEqual(b.containsNormals, false);
    a.insert(0, P(1, 1, 1), P(1, 0, 0));               // (index, point, normal)
    assert.strictEqual(a.count, 2);
    ctx.approx(a.pointAt(0).X, 1);
  });

  t('test_js_object_frame_accepts_a_plane_and_a_transform', () => {
    const oa = new r.ObjectAttributes();
    oa.setObjectFrame(r.Plane.worldXY);              // the Plane overload (unreachable before)
    oa.setObjectFrame(r.Transform.identity);         // the Transform overload
  });

  t('test_js_box_contains_box_takes_the_box_overload', () => {
    const big = r.Box.create(r.Plane.worldXY, r.Interval.create(0, 10), r.Interval.create(0, 10), r.Interval.create(0, 10));
    const small = r.Box.create(r.Plane.worldXY, r.Interval.create(1, 2), r.Interval.create(1, 2), r.Interval.create(1, 2));
    assert.strictEqual(r.Box.contains(big, small), true, 'a Box has a Z (an interval): it is not a point');
    assert.strictEqual(r.Box.contains(big, P(5, 5, 5)), true);
    assert.strictEqual(r.Box.contains(big, P(50, 5, 5)), false);
  });

  t('test_js_null_reaches_the_optional_overload', () => {
    const pc = new r.PolyCurve();
    assert.strictEqual(pc.append(null), false, 'null is not a value object');
  });

  t('test_js_mesh_face_list_old_names_are_addface_setface', () => {
    // retired (Luis 2026-10-18): the old JS addTriFace / addQuadFace / setTriFace / setQuadFace are
    // RhinoCommon's AddFace / SetFace overloads, dispatched by arity -- same faces, same results (the
    // old members' results frozen, M3)
    const neu = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) neu.vertices.add(x, y, 0);
    const nf = neu.faces;
    assert.strictEqual(nf.addFace(0, 1, 2), 0);                // addTriFace
    assert.strictEqual(nf.addFace(0, 1, 2, 3), 1);             // addQuadFace
    assert.strictEqual(nf.setFace(0, 1, 4, 2), true);          // setTriFace
    assert.strictEqual(nf.setFace(1, 3, 2, 1, 0), true);       // setQuadFace
    const abcd = f => [f.A, f.B, f.C, f.D];
    assert.deepStrictEqual([0, 1].map(i => abcd(nf.get(i))), [[1, 4, 2, 2], [3, 2, 1, 0]]);   // the old get(i)
    assert.deepStrictEqual(abcd(nf.get(0)), [1, 4, 2, 2]);     // a triangle repeats its last vertex
  });

  t('test_retired_js_overload_splits_js1', () => {
    // JS1 (Luis 2026-10-28): the old JS-only split names retire for RhinoCommon's single name; each
    // old member against the overload the dispatcher picks, same input, same result (the old members'
    // results frozen, M3)
    const A = v => [v.X, v.Y, v.Z];
    const sig = c => [c.count, Array.from(c.getPoints()).map(A), Array.from(c.getNormals()).map(A),
                      Array.from(c.getColors()), Array.from(c.getPointValues())];
    const red = { r: 255, g: 0, b: 0, a: 255 }, black = { r: 0, g: 0, b: 0, a: 255 };
    // PointCloud add* / insert*
    const n = new r.PointCloud();
    n.add(P(1, 0, 0), P(0, 0, 1));                      // addPointNormal
    n.add(P(2, 0, 0), P(0, 1, 0), red);                 // addPointNormalColor
    n.add(P(3, 0, 0), P(1, 0, 0), red, 2.5);            // addPointNormalColorValue
    // inserts at the end: the old ones misalign the channels anywhere else (pilot #64, U37 --
    // test_point_cloud_insert_and_add_range_keep_channels_aligned_u37 covers the front)
    n.insert(3, P(4, 0, 0), P(0, 0, 1));                // insertPointNormal
    n.insert(4, P(5, 0, 0), P(0, 0, 1), red);           // insertPointNormalColor
    n.insert(5, P(6, 0, 0), P(0, 0, 1), red, 1.5);      // insertPointNormalColorValue
    assert.deepStrictEqual(sig(n), [6, [[1, 0, 0], [2, 0, 0], [3, 0, 0], [4, 0, 0], [5, 0, 0], [6, 0, 0]],
      [[0, 0, 1], [0, 1, 0], [1, 0, 0], [0, 0, 1], [0, 0, 1], [0, 0, 1]],
      [black, red, red, black, red, red], [0, 0, 2.5, 0, 0, 1.5]]);
    const nc = new r.PointCloud();
    nc.add(P(1, 2, 3), red);                            // addPointColor
    nc.insert(1, P(4, 5, 6), red);                      // insertPointColor
    assert.deepStrictEqual(sig(nc), [2, [[1, 2, 3], [4, 5, 6]], [], [red, red], []]);
    // addRange(points) / (points, normals)
    const pts = [[0, 0, 0], [1, 1, 1]], nrm = [[0, 0, 1], [0, 1, 0]];
    const nr1 = new r.PointCloud();
    nr1.addRange(pts.map(q => P(...q)));                // addRangePoints
    assert.deepStrictEqual(sig(nr1), [2, pts, [], [], []]);
    const nr2 = new r.PointCloud();
    nr2.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)));   // addRangePointsNormals
    assert.deepStrictEqual(sig(nr2), [2, pts, nrm, [], []]);
    // Transform mirrorPlane / rotationVectors / translationVector / translationXYZ
    const M = x => [x.M00, x.M01, x.M02, x.M03, x.M10, x.M11, x.M12, x.M13, x.M20, x.M21, x.M22, x.M23];
    const npl = r.Plane.worldXY;
    assert.deepStrictEqual(M(r.Transform.mirror(npl)), [1, -0, -0, 0, -0, 1, -0, 0, -0, -0, -1, 0]);
    assert.deepStrictEqual(M(r.Transform.rotation(P(1, 0, 0), P(0, 1, 0), P(1, 2, 3))), [0, -1, 0, 3, 1, 0, 0, 1, 0, 0, 1, -0]);
    assert.deepStrictEqual(M(r.Transform.translation(P(1, 2, 3))), [1, 0, 0, 1, 0, 1, 0, 2, 0, 0, 1, 3]);
    assert.deepStrictEqual(M(r.Transform.translation(1, 2, 3)), [1, 0, 0, 1, 0, 1, 0, 2, 0, 0, 1, 3]);
    // BoundingBox inflateXYZ (a value: inflate returns the inflated box)
    const nb = r.BoundingBox.inflate(r.BoundingBox.create(P(0, 0, 0), P(1, 1, 1)), 1, 2, 3);
    assert.deepStrictEqual([A(nb.Min), A(nb.Max)], [[-1, -2, -3], [2, 3, 4]]);
    // Curve closedCurveOrientationPlane / derivativeAtSide / toNurbsCurveSubDomain
    const ncrv = r.Circle.toNurbsCurve(r.Circle.create(5));
    assert.strictEqual(ncrv.closedCurveOrientation(npl).value, 1);
    const nd = ncrv.derivativeAt(0.5, 1, r.CurveEvaluationSide.Below);
    assert.deepStrictEqual(Array.from(nd).map(A), [[4.419622907229764, 2.338147420050742, 0], [-2.4116096580979542, 4.558483009593529, 0]]);
    const dom = ncrv.domain;
    assert.deepStrictEqual([dom.T0, dom.T1], [0, 6.283185307179586]);
    const nsub = ncrv.toNurbsCurve(r.Interval.create(dom.T0, (dom.T0 + dom.T1) / 2));
    assert.strictEqual(nsub.points.count, 5);
    assert.deepStrictEqual(A(nsub.pointAtEnd).map(x => +x.toFixed(9)), [-5, 0, 0]);
    // Surface toNurbsSurfaceTolerance
    const nt = r.Sphere.toNurbsSurface(r.Sphere.create(P(0, 0, 0), 2)).toNurbsSurface(0.01);
    assert.strictEqual(nt[1], 1);                                      // the accuracy
    assert.deepStrictEqual([nt[0].points.countU, nt[0].points.countV], [9, 5]);
    // PolyCurve appendArc / appendCurve
    const npc = new r.PolyCurve();
    assert.strictEqual(npc.append(r.Arc.create(P(0, 0, 0), P(1, 1, 0), P(2, 0, 0))), true);
    assert.strictEqual(npc.append(new r.LineCurve(P(2, 0, 0), P(4, 0, 0))), true);
    assert.strictEqual(npc.segmentCount, 2);
    assert.deepStrictEqual(A(npc.pointAtEnd), [4, 0, 0]);
    // Material set*TextureFilename
    const nm = new r.Material();
    // pilot #62: the old ones returned ON_Material::AddTexture's INDEX as a bool -- the first texture
    // (index 0) reported false on success ([false, true, true, true]); RhinoCommon's report success
    for (const k of ['Bitmap', 'Bump', 'Environment', 'Transparency']) {
      assert.strictEqual(nm['set' + k + 'Texture']('/tmp/' + k + '.png'), true);
      assert.strictEqual(nm['get' + k + 'Texture']().fileName, '/tmp/' + k + '.png');
    }
    // File3dmObjectTable addPointXYZ; File3dmGroupTable deleteId / deleteIndex
    const nf = new r.File3dm();
    nf.objects.addPoint(1, 2, 3);
    assert.deepStrictEqual(A(Array.from(nf.objects)[0].geometry.location), [1, 2, 3]);
    for (let i = 0; i < 3; i++) nf.allGroups.addGroup();
    // deleteIndex -> delete(index): RhinoCommon's File3dmCommonComponentTable.Delete(int) returns
    // nothing (the old one returned true); deleteId has no RhinoCommon overload (reported, kept)
    assert.strictEqual(nf.allGroups.delete(0), undefined);
    assert.strictEqual(nf.allGroups.count, 2);                         // as the old deleteIndex left it
    // DracoCompression compressOptions
    const nmesh = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1]]) nmesh.vertices.add(x, y, 0);
    nmesh.faces.addFace(0, 1, 2, 3);
    const ndr = r.DracoCompression.compress(nmesh, new r.DracoCompressionOptions());
    assert(ndr);
    // the same compressed bytes as the old compressOptions (it exposed them as base64; frozen, M3)
    assert.strictEqual(Buffer.from(Array.from(ndr.toByteArray())).toString('base64'),
      'RFJBQ08CAgEBAAAABAIAAgAAAR//AREB/wAAAQAJAwAAAgEBAQADAwEwARADACiCmAAAAAAA/z8AAAAAAAAAAAAAAAAAAAAAgD8O');
  });

  t('test_hand_point_cloud_values_and_group_delete_id_g126', () => {
    // kept by hand under the old JS names (Luis 2026-10-30, upstream U38)
    const n = new r.PointCloud();
    n.addPointValue(P(1, 0, 0), 1.5);
    n.addRangePointsValues([P(2, 0, 0), P(3, 0, 0)], [2.5, 3.5]);
    n.insertPointValue(0, P(0, 0, 0), 0.5);
    assert.deepStrictEqual(Array.from(n.getPointValues()), [0.5, 1.5, 2.5, 3.5]);   // the old one misaligned them: [0.5, 2.5, 3.5, 0] (pilot #64)
    assert.deepStrictEqual(Array.from(n.getPoints()).map(q => q.X), [0, 1, 2, 3]);   // the same points as the old one
    // channels stay aligned (pilot #64: the generated insert does not)
    const c = new r.PointCloud();
    c.add(P(1, 0, 0), P(0, 0, 1)); c.add(P(2, 0, 0), P(0, 1, 0));
    c.insertPointValue(0, P(0, 0, 0), 9);
    assert.deepStrictEqual(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 0], [0, 0, 1], [0, 1, 0]]);
    assert.deepStrictEqual(Array.from(c.getPointValues()), [9, 0, 0]);
    // deleteId (as the old groups().deleteId: true, then false for an unknown id)
    const nf = new r.File3dm();
    for (let i = 0; i < 3; i++) nf.allGroups.addGroup();
    assert.strictEqual(nf.allGroups.deleteId(nf.allGroups.findIndex(1).id), true);
    assert.strictEqual(nf.allGroups.deleteId(require('crypto').randomUUID()), false);
    assert.strictEqual(nf.allGroups.count, 2);
  });

  t('test_point_cloud_insert_and_add_range_keep_channels_aligned_u37', () => {
    // U37 (RH-99914, pilot #64): insert keeps every channel with its point; a range adding a
    // channel the cloud lacked puts it on the NEW points
    const red = { r: 255, g: 0, b: 0, a: 255 }, lime = { r: 0, g: 255, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 };
    const c = new r.PointCloud();
    c.add(P(1, 0, 0), P(0, 0, 1), red, 1); c.add(P(2, 0, 0), P(0, 1, 0), lime, 2);
    c.insert(0, P(0, 0, 0), P(1, 0, 0), blue, 9);
    assert.deepStrictEqual(Array.from(c.getPointValues()), [9, 1, 2]);
    assert.deepStrictEqual(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z]), [[1, 0, 0], [0, 0, 1], [0, 1, 0]]);
    assert.deepStrictEqual(Array.from(c.getColors()).map(k => [k.r, k.g, k.b]), [[0, 0, 255], [255, 0, 0], [0, 255, 0]]);
    const d = new r.PointCloud();
    d.add(P(1, 0, 0)); d.add(P(2, 0, 0));
    d.addRange([P(3, 0, 0), P(4, 0, 0)], [P(0, 0, 1), P(0, 0, 1)]);
    assert.deepStrictEqual(Array.from(d.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 0], [0, 0, 0], [0, 0, 1], [0, 0, 1]]);
  });

  t('test_retired_line_line_tolerance_and_plane_circle_g128', () => {
    // JS1: lineLineTolerance -> lineLine(a, b, tolerance, finiteSegments), now generated (G128); each
    // case with the old lineLineTolerance's [ok, a, b] for (tol, finite) = (0, false), (0.001, true),
    // (5, true) (frozen, M3)
    const L = (a, b) => ({ From: P(...a), To: P(...b) });
    const cases = [[[0, 0, 0], [4, 0, 0], [2, -1, 0], [2, 3, 0], [[true, 0.5, 0.25], [true, 0.5, 0.25], [true, 0.5, 0.25]]],
                   [[0, 0, 0], [1, 0, 0], [3, -1, 0], [3, 1, 0], [[true, 3, 0.5], [false, 1, 0.5], [true, 1, 0.5]]],
                   [[0, 0, 0], [1, 0, 0], [0, 0, 1], [1, 0, 1], [[false, 0, 0], [false, 0, 0], [false, 0, 0]]]];
    for (const [a0, a1, b0, b1, olds] of cases)
      [[0.0, false], [0.001, true], [5.0, true]].forEach(([tol, finite], k) => {
        const n = r.Intersection.lineLine(L(a0, a1), L(b0, b1), tol, finite);
        assert.deepStrictEqual(Array.from(n), olds[k]);
      });
    const circle = r.Circle.create(r.Plane.worldZX, 2.0);
    const [kind, t0, t1] = r.Intersection.planeCircle(r.Plane.worldXY, circle);
    assert.strictEqual(kind, r.PlaneCircleIntersection.Secant);
    assert.deepStrictEqual([t0, t1].map(t => Math.round(r.Circle.pointAt(circle, t).X * 1e9) / 1e9).sort((x, y) => x - y), [-2, 2]);
  });

  t('test_retired_add_range_with_colors_and_array_natives_g128', () => {
    // JS1: addRangePointsNormalsColors / ...Values -> addRange(points, normals, colors[, values]) (G128)
    const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 };
    const pts = [[0, 0, 0], [1, 0, 0]], nrm = [[0, 0, 1], [0, 1, 0]];
    const sig = c => JSON.stringify([Array.from(c.getNormals()).map(v => [v.X, v.Y, v.Z]),
                                     Array.from(c.getColors()).map(k => [k.r, k.g, k.b]), Array.from(c.getPointValues())]);
    const n3 = new r.PointCloud();
    n3.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)), [red, blue]);
    // the old addRangePointsNormalsColors' cloud (frozen, M3)
    assert.strictEqual(sig(n3), JSON.stringify([[[0, 0, 1], [0, 1, 0]], [[255, 0, 0], [0, 0, 255]], []]));
    const n4 = new r.PointCloud();
    n4.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)), [red, blue], [1.5, 2.5]);
    assert.deepStrictEqual(Array.from(n4.getPointValues()), [1.5, 2.5]);
    // (pilot #66: the old addRangePointsNormalsColorsValues added nothing -- a chained != in its size check)
    // int / double sequences in and out through the members the same rule unlocked
    const m = new r.Mesh();
    for (let j = 0; j < 3; j++) for (let i = 0; i < 3; i++) m.vertices.add(i, j, 0);
    for (let j = 0; j < 2; j++) for (let i = 0; i < 2; i++) { const a = j * 3 + i; m.faces.addFace(a, a + 1, a + 4, a + 3); }
    assert.deepStrictEqual(Array.from(m.faces.toIntArray(false)).slice(0, 8), [0, 1, 4, 3, 1, 2, 5, 4]);
    assert.deepStrictEqual(Array.from(m.getNakedEdgePointStatus()), [true, true, true, true, false, true, true, true, true]);
    assert.strictEqual(m.faces.deleteFaces([0, 3], true), 2);
    assert.deepStrictEqual([m.faces.count, m.vertices.count], [2, 7]);
    const lt = new r.Linetype();
    assert.strictEqual(lt.setSegments([1.0, -0.5]), true);
    assert.deepStrictEqual([0, 1].map(i => Array.from(lt.getSegment(i))), [[1.0, true], [0.5, false]]);
  });

  t('test_retired_append_line_and_add_range_colors_g130', () => {
    // JS1: appendLine -> append(line), addRangePointsColors -> addRange(points, colors) (G130)
    const L = (a, b) => ({ From: P(...a), To: P(...b) });
    const npc = new r.PolyCurve();
    // the old appendLine: true, true, 2 segments (frozen, M3)
    assert.strictEqual(npc.append(L([0, 0, 0], [2, 0, 0])), true);
    assert.strictEqual(npc.append(L([2, 0, 0], [2, 3, 0])), true);
    assert.strictEqual(npc.segmentCount, 2);
    assert.deepStrictEqual([npc.pointAtEnd.X, npc.pointAtEnd.Y], [2, 3]);
    const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 128, b: 255, a: 255 };
    const n = new r.PointCloud();
    n.addRange([P(0, 0, 0), P(1, 0, 0)], [red, blue]);
    const cols = c => Array.from(c.getColors()).map(k => [k.r, k.g, k.b]);
    assert.deepStrictEqual(cols(n), [[255, 0, 0], [0, 128, 255]]);      // as the old addRangePointsColors'
  });

  t('test_threejs_buffers_round_trip_g131', () => {
    // three.js (Luis 2026-10-08/-28): toThreejsBuffers kept (as the old one: typed arrays, quads as two
    // triangles), createFromThreejsBuffers its inverse; on a mesh with uvs and colors, both rotations
    const m = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.vertices.add(x, y, 0);
    m.faces.addFace(0, 1, 2, 3); m.faces.addFace(1, 4, 2);
    m.normals.computeNormals();
    for (const [u, v] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.textureCoordinates.add(u, v);
    for (let i = 0; i < 5; i++) m.vertexColors.add(255, i * 50, 0);
    // the old toThreejsBuffers(rot).position of the same (uncolored) mesh, frozen (M3)
    const oldPosition = { false: [0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 2, 0, 0], true: [0, 0, 0, 1, 0, 0, 1, 0, -1, 0, 0, -1, 2, 0, 0] };
    for (const rot of [false, true]) {
      const b = m.toThreejsBuffers(rot);
      assert(b.position instanceof Float32Array && b.index instanceof Uint32Array && b.normal instanceof Float32Array);
      assert(b.uv instanceof Float32Array && b.color instanceof Float32Array);
      assert.deepStrictEqual(Array.from(b.index), [0, 1, 2, 2, 3, 0, 1, 4, 2]);
      assert.deepStrictEqual(Array.from(b.position), oldPosition[rot]);           // the same as the old one (its index too)
      // and back: the same vertices, triangles, uvs and colors
      const back = r.Mesh.createFromThreejsBuffers(b, rot);
      assert.strictEqual(back.vertices.count, 5);
      assert.strictEqual(back.faces.count, 3);                                   // a quad comes back as two triangles
      for (let i = 0; i < 5; i++) {
        const p = back.vertices.point3dAt(i), q = m.vertices.point3dAt(i);
        ctx.approx(p.X, q.X, 1e-6); ctx.approx(p.Y, q.Y, 1e-6); ctx.approx(p.Z, q.Z, 1e-6);
      }
      assert.deepStrictEqual(Array.from(back.vertexColors.toARGBArray()), Array.from(m.vertexColors.toARGBArray()));
      assert.strictEqual(back.textureCoordinates.count, 5);
    }
    // non-indexed buffers: every 3 vertices a triangle; bad lengths are refused
    const tri = r.Mesh.createFromThreejsBuffers({ position: new Float32Array([0, 0, 0, 1, 0, 0, 0, 1, 0]) }, false);
    assert.deepStrictEqual([tri.vertices.count, tri.faces.count], [3, 1]);
    ctx.raises(() => r.Mesh.createFromThreejsBuffers({ position: [0, 0, 0, 1] }, false), /'position' has 4 numbers/);
    ctx.raises(() => r.Mesh.createFromThreejsBuffers({ position: [0, 0, 0, 1, 0, 0, 0, 1, 0], index: [0, 1, 7] }, false), /outside the vertices/);
    // point clouds: positions, colors, normals both ways
    const pc = new r.PointCloud();
    pc.add(P(1, 2, 3), P(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 }); pc.add(P(4, 5, 6), P(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 });
    const pb = pc.toThreejsBuffers(false);
    assert.deepStrictEqual(Array.from(pb.position), [1, 2, 3, 4, 5, 6]);
    assert.deepStrictEqual(Array.from(pb.color), [1, 0, 0, 0, 0, 1]);
    const pback = r.PointCloud.createFromThreejsBuffers(pb, false);
    assert.deepStrictEqual(Array.from(pback.getPoints()).map(q => [q.X, q.Y, q.Z]), [[1, 2, 3], [4, 5, 6]]);
    assert.deepStrictEqual(Array.from(pback.getColors()).map(k => [k.r, k.g, k.b]), [[255, 0, 0], [0, 0, 255]]);
    assert.deepStrictEqual(Array.from(pback.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 1], [0, 1, 0]]);
    // the old toThreejsBuffers(true) of the same cloud (frozen, M3)
    const npb = pc.toThreejsBuffers(true);
    assert.deepStrictEqual(Array.from(npb.position), [1, 3, -2, 4, 6, -5]);
    assert.deepStrictEqual(Array.from(npb.normal), [0, 0, 1, 0, 1, 0]);
  });

  t('test_threejs_json_family_deprecated_g133', () => {
    // the JSON family (Luis 2026-10-31): shipped deprecated in 9.2 -- 8.x's output, a warning once each.
    // 8.x's (the old binding's) results frozen in old_oracle/threejs_json_g133.json (M3)
    const oracle = JSON.parse(require('fs').readFileSync(require('path').join(__dirname, '..', 'old_oracle', 'threejs_json_g133.json'), 'utf8'));
    const warned = [];
    const warn = console.warn;
    console.warn = (...a) => warned.push(a.join(' '));
    try {
      const quad = [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]];
      const m = new r.Mesh();
      for (const [x, y] of quad) m.vertices.add(x, y, 0);
      m.faces.addFace(0, 1, 2, 3); m.faces.addFace(1, 4, 2);
      for (let i = 0; i < 5; i++) m.vertexColors.add(255, i * 50, 0);
      const same = (a, b) => {
        // the same object as 8.x's: keys, types, item sizes, and the numbers (float precision)
        assert.deepStrictEqual(Object.keys(a.data).sort(), Object.keys(b.data).sort());
        if (b.data.index) assert.deepStrictEqual(a.data.index, b.data.index);
        assert.deepStrictEqual(Object.keys(a.data.attributes).sort(), Object.keys(b.data.attributes).sort());
        for (const k of Object.keys(b.data.attributes)) {
          const x = a.data.attributes[k], y = b.data.attributes[k];
          assert.deepStrictEqual([x.itemSize, x.type, x.array.length], [y.itemSize, y.type, y.array.length], k);
          x.array.forEach((v, i) => ctx.approx(v, y.array[i], 1e-6));
        }
      };
      const j = m.toThreejsJSON();
      same(j, oracle.meshJSON);
      assert.deepStrictEqual(j.data.index, { type: 'Uint16Array', array: [0, 1, 2, 2, 3, 0, 1, 4, 2] });
      assert.strictEqual(m.normals.count, 0, "the normals are computed on a copy, not on the caller's mesh");
      same(m.toThreejsJSONRotate(true), oracle.meshJSONRotate);
      const m2 = new r.Mesh();
      for (const [x, y] of [[5, 0], [6, 0], [5, 1]]) m2.vertices.add(x, y, 0);
      m2.faces.addFace(0, 1, 2);
      // (8.x's merge mixes a colored mesh with an uncolored one into no colors: compare uncolored)
      const plain = new r.Mesh();
      for (const [x, y] of quad) plain.vertices.add(x, y, 0);
      plain.faces.addFace(0, 1, 2, 3);
      same(r.Mesh.toThreejsJSONMerged([plain, m2], true), oracle.mergedRotate);
      // and back, as 8.x: a mesh always turned from Y-up
      const back = r.Mesh.createFromThreejsJSON(j);
      assert.deepStrictEqual([back.vertices.count, back.faces.count], [5, 3]);
      assert.deepStrictEqual(Array.from(back.toThreejsBuffers(false).position), oracle.createFromNewJSONPosition);
      assert.deepStrictEqual(Array.from(back.vertexColors.toARGBArray()), Array.from(m.vertexColors.toARGBArray()));
      assert.strictEqual(r.Mesh.createFromThreejsJSON({}), null);
      ctx.raises(() => r.Mesh.createFromThreejsJSON({ data: { index: { array: [0, 1, 7] },
        attributes: { position: { itemSize: 3, array: [0, 0, 0, 1, 0, 0, 0, 1, 0] } } } }), /outside the vertices/);
      // point clouds; an RGBA color's 4th channel is opacity
      const pc = new r.PointCloud();
      pc.add(P(1, 2, 3), P(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 }); pc.add(P(4, 5, 6), P(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 });
      same(pc.toThreejsJSON(), oracle.pcJSON);
      const rgba = { data: { attributes: { position: { itemSize: 3, array: [1, 2, 3] },
                                           color: { itemSize: 4, array: [1, 0, 0, 0.5] } } } };
      const cols = c => Array.from(c.getColors()).map(k => [k.r, k.g, k.b, k.a]);
      assert.deepStrictEqual(cols(r.PointCloud.createFromThreejsJSON(rgba)), oracle.pcFromRGBAColors);
      assert.deepStrictEqual(Array.from(r.PointCloud.createFromThreejsJSON(pc.toThreejsJSON()).getPoints()).map(q => [q.X, q.Y, q.Z]),
                             [[1, 2, 3], [4, 5, 6]]);
      // each member warned once, naming its replacement
      const mine = warned.filter(w => /^rhino3dm: .* is deprecated/.test(w));
      for (const name of ['Mesh.toThreejsJSON ', 'Mesh.toThreejsJSONRotate', 'Mesh.toThreejsJSONMerged', 'Mesh.createFromThreejsJSON',
                          'PointCloud.toThreejsJSON', 'PointCloud.createFromThreejsJSON'])
        assert.strictEqual(mine.filter(w => w.includes(name)).length, 1, name);
      assert(mine.every(w => /use .*Buffers/.test(w)));
    } finally {
      console.warn = warn;
    }
  });
};
