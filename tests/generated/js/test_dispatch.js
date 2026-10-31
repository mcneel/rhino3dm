// JS overload dispatch (Luis 2026-10-16: PointCloud.add(point, color) shadowed add(point, normal)
// -- an unbraced `return`, a color checked as `true`). Each case calls the overload a shape check
// must pick and verifies the effect that only that overload has. Not in the Python suite: pybind
// dispatches overloads natively.
module.exports = (t, ctx) => {
  const { r, assert } = ctx;
  const P = (x, y, z) => ({ X: x, Y: y, Z: z });

  t('test_js_point_cloud_add_insert_pick_the_overload_by_shape', () => {
    const a = new r.PointCloudX();
    a.add(P(0, 0, 0), P(0, 0, 1));                     // (point, normal)
    assert.strictEqual(a.count, 1);
    assert.strictEqual(a.containsNormals, true, 'add(point, normal) must take the normal overload');
    assert.strictEqual(a.containsColors, false);
    const b = new r.PointCloudX();
    b.add(P(0, 0, 0), { r: 255, g: 0, b: 0, a: 0 });    // (point, color)
    assert.strictEqual(b.containsColors, true, 'add(point, color) must take the color overload');
    assert.strictEqual(b.containsNormals, false);
    a.insert(0, P(1, 1, 1), P(1, 0, 0));               // (index, point, normal)
    assert.strictEqual(a.count, 2);
    ctx.approx(a.pointAt(0).X, 1);
  });

  t('test_js_object_frame_accepts_a_plane_and_a_transform', () => {
    const oa = new r.ObjectAttributesX();
    oa.setObjectFrame(r.PlaneX.worldXY);              // the Plane overload (unreachable before)
    oa.setObjectFrame(r.TransformX.identity);         // the Transform overload
  });

  t('test_js_box_contains_box_takes_the_box_overload', () => {
    const big = r.BoxX.create(r.PlaneX.worldXY, r.IntervalX.create(0, 10), r.IntervalX.create(0, 10), r.IntervalX.create(0, 10));
    const small = r.BoxX.create(r.PlaneX.worldXY, r.IntervalX.create(1, 2), r.IntervalX.create(1, 2), r.IntervalX.create(1, 2));
    assert.strictEqual(r.BoxX.contains(big, small), true, 'a Box has a Z (an interval): it is not a point');
    assert.strictEqual(r.BoxX.contains(big, P(5, 5, 5)), true);
    assert.strictEqual(r.BoxX.contains(big, P(50, 5, 5)), false);
  });

  t('test_js_null_reaches_the_optional_overload', () => {
    const pc = new r.PolyCurveX();
    assert.strictEqual(pc.append(null), false, 'null is not a value object');
  });

  t('test_js_mesh_face_list_old_names_are_addface_setface', () => {
    // retired (Luis 2026-10-18): the old JS addTriFace / addQuadFace / setTriFace / setQuadFace are
    // RhinoCommon's AddFace / SetFace overloads, dispatched by arity -- same faces, same results
    const old = new r.Mesh();
    const neu = new r.MeshX();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) {
      old.vertices().add(x, y, 0);
      neu.vertices.add(x, y, 0);
    }
    const of = old.faces(), nf = neu.faces;
    assert.strictEqual(nf.addFace(0, 1, 2), of.addTriFace(0, 1, 2));
    assert.strictEqual(nf.addFace(0, 1, 2, 3), of.addQuadFace(0, 1, 2, 3));
    assert.strictEqual(nf.setFace(0, 1, 4, 2), of.setTriFace(0, 1, 4, 2));
    assert.strictEqual(nf.setFace(1, 3, 2, 1, 0), of.setQuadFace(1, 3, 2, 1, 0));
    const abcd = f => [f.A, f.B, f.C, f.D];
    assert.deepStrictEqual([0, 1].map(i => abcd(nf.get(i))), [0, 1].map(i => of.get(i)));
    assert.deepStrictEqual(abcd(nf.get(0)), [1, 4, 2, 2]);     // a triangle repeats its last vertex
  });

  t('test_retired_js_overload_splits_js1', () => {
    // JS1 (Luis 2026-10-28): the old JS-only split names retire for RhinoCommon's single name; each
    // old member against the overload the dispatcher picks, same input, same result
    const A = v => [v.X, v.Y, v.Z];
    const cloud = pc => { const c = new r.PointCloudX(pc);
      return [c.count, Array.from(c.getPoints()).map(A), Array.from(c.getNormals()).map(A),
              Array.from(c.getColors()), Array.from(c.getPointValues())]; };
    const sig = c => [c.count, Array.from(c.getPoints()).map(A), Array.from(c.getNormals()).map(A),
                      Array.from(c.getColors()), Array.from(c.getPointValues())];
    const red = { r: 255, g: 0, b: 0, a: 255 };
    // PointCloud add* / insert*
    const o = new r.PointCloud(), n = new r.PointCloudX();
    o.addPointNormal([1, 0, 0], [0, 0, 1]); n.add(P(1, 0, 0), P(0, 0, 1));
    o.addPointNormalColor([2, 0, 0], [0, 1, 0], red); n.add(P(2, 0, 0), P(0, 1, 0), red);
    o.addPointNormalColorValue([3, 0, 0], [1, 0, 0], red, 2.5); n.add(P(3, 0, 0), P(1, 0, 0), red, 2.5);
    // inserts at the end: the old ones misalign the channels anywhere else (pilot #64, U37 --
    // test_point_cloud_insert_and_add_range_keep_channels_aligned_u37 covers the front)
    o.insertPointNormal(3, [4, 0, 0], [0, 0, 1]); n.insert(3, P(4, 0, 0), P(0, 0, 1));
    o.insertPointNormalColor(4, [5, 0, 0], [0, 0, 1], red); n.insert(4, P(5, 0, 0), P(0, 0, 1), red);
    o.insertPointNormalColorValue(5, [6, 0, 0], [0, 0, 1], red, 1.5); n.insert(5, P(6, 0, 0), P(0, 0, 1), red, 1.5);
    assert.deepStrictEqual(sig(n), cloud(o));
    const oc = new r.PointCloud(), nc = new r.PointCloudX();
    oc.addPointColor([1, 2, 3], red); nc.add(P(1, 2, 3), red);
    oc.insertPointColor(1, [4, 5, 6], red); nc.insert(1, P(4, 5, 6), red);
    assert.deepStrictEqual(sig(nc), cloud(oc));
    // addRange(points) / (points, normals)
    const pts = [[0, 0, 0], [1, 1, 1]], nrm = [[0, 0, 1], [0, 1, 0]];
    const or1 = new r.PointCloud(), nr1 = new r.PointCloudX();
    or1.addRangePoints(pts); nr1.addRange(pts.map(q => P(...q)));
    assert.deepStrictEqual(sig(nr1), cloud(or1));
    const or2 = new r.PointCloud(), nr2 = new r.PointCloudX();
    or2.addRangePointsNormals(pts, nrm); nr2.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)));
    assert.deepStrictEqual(sig(nr2), cloud(or2));
    // Transform mirrorPlane / rotationVectors / translationVector / translationXYZ
    const M = x => [x.M00, x.M01, x.M02, x.M03, x.M10, x.M11, x.M12, x.M13, x.M20, x.M21, x.M22, x.M23];
    const OM = x => { const a = x.toFloatArray(true); return [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11].map(i => a[i]); };
    const opl = r.Plane.worldXY(), npl = r.PlaneX.worldXY;
    assert.deepStrictEqual(M(r.TransformX.mirror(npl)), OM(r.Transform.mirrorPlane(opl)));
    assert.deepStrictEqual(M(r.TransformX.rotation(P(1, 0, 0), P(0, 1, 0), P(1, 2, 3))),
                           OM(r.Transform.rotationVectors([1, 0, 0], [0, 1, 0], [1, 2, 3])));
    assert.deepStrictEqual(M(r.TransformX.translation(P(1, 2, 3))), OM(r.Transform.translationVector([1, 2, 3])));
    assert.deepStrictEqual(M(r.TransformX.translation(1, 2, 3)), OM(r.Transform.translationXYZ(1, 2, 3)));
    // BoundingBox inflateXYZ (a value: inflate returns the inflated box)
    const ob = new r.BoundingBox([0, 0, 0], [1, 1, 1]); ob.inflateXYZ(1, 2, 3);
    const nb = r.BoundingBoxX.inflate(r.BoundingBoxX.create(P(0, 0, 0), P(1, 1, 1)), 1, 2, 3);
    assert.deepStrictEqual([A(nb.Min), A(nb.Max)], [Array.from(ob.min), Array.from(ob.max)]);
    // Curve closedCurveOrientationPlane / derivativeAtSide / toNurbsCurveSubDomain
    const ocrv = new r.Circle(5).toNurbsCurve(), ncrv = new r.NurbsCurveX(ocrv);
    assert.strictEqual(ncrv.closedCurveOrientation(npl).value, ocrv.closedCurveOrientationPlane(opl).value);
    const od = ocrv.derivativeAtSide(0.5, 1, r.CurveEvaluationSide.Below);
    const nd = ncrv.derivativeAt(0.5, 1, r.CurveEvaluationSide.Below);
    assert.deepStrictEqual(Array.from(nd).map(A), Array.from(od).map(v => Array.from(v)));
    const dom = ocrv.domain;
    const osub = ocrv.toNurbsCurveSubDomain([dom[0], (dom[0] + dom[1]) / 2]);
    const nsub = ncrv.toNurbsCurve(r.IntervalX.create(dom[0], (dom[0] + dom[1]) / 2));
    assert.strictEqual(nsub.points.count, osub.points().count);
    assert.deepStrictEqual(A(nsub.pointAtEnd).map(x => +x.toFixed(9)), Array.from(osub.pointAtEnd).map(x => +x.toFixed(9)));
    // Surface toNurbsSurfaceTolerance
    const osrf = new r.Sphere([0, 0, 0], 2).toNurbsSurface();
    const ot = osrf.toNurbsSurfaceTolerance(0.01), nt = new r.SurfaceX(osrf).toNurbsSurface(0.01);
    assert.strictEqual(nt[1], ot[1]);                                  // the accuracy
    assert.deepStrictEqual([nt[0].points.countU, nt[0].points.countV], [ot[0].points().countU, ot[0].points().countV]);
    // PolyCurve appendArc / appendCurve
    const opc = new r.PolyCurve(), npc = new r.PolyCurveX();
    const arc = r.Arc.createFromPoints([0, 0, 0], [1, 1, 0], [2, 0, 0]);
    assert.strictEqual(npc.append(r.ArcX.create(P(0, 0, 0), P(1, 1, 0), P(2, 0, 0))), opc.appendArc(arc));
    const line = new r.LineCurve([2, 0, 0], [4, 0, 0]);
    assert.strictEqual(npc.append(new r.CurveX(line)), opc.appendCurve(line));
    assert.strictEqual(npc.segmentCount, opc.segmentCount);
    assert.deepStrictEqual(A(npc.pointAtEnd), Array.from(opc.pointAtEnd));
    // Material set*TextureFilename
    const om = new r.Material(), nm = new r.MaterialX();
    // pilot #62: the old ones return ON_Material::AddTexture's INDEX as a bool -- the first texture
    // (index 0) reports false on success; RhinoCommon's report success
    const okOld = [];
    for (const k of ['Bitmap', 'Bump', 'Environment', 'Transparency']) {
      assert.strictEqual(nm['set' + k + 'Texture']('/tmp/' + k + '.png'), true);
      okOld.push(om['set' + k + 'TextureFilename']('/tmp/' + k + '.png'));
      assert.strictEqual(nm['get' + k + 'Texture']().fileName, om['get' + k + 'Texture']().fileName);
    }
    assert.deepStrictEqual(okOld, [false, true, true, true]);
    // File3dmObjectTable addPointXYZ; File3dmGroupTable deleteId / deleteIndex
    const of = new r.File3dm(), nf = new r.File3dmX();
    of.objects().addPointXYZ(1, 2, 3); nf.objects.addPoint(1, 2, 3);
    const op = of.objects().get(0).geometry().location, np = Array.from(nf.objects)[0].geometry.location;
    assert.deepStrictEqual(A(np), Array.from(op));
    for (let i = 0; i < 3; i++) { of.groups().add(new r.Group()); nf.allGroups.addGroup(); }
    // deleteIndex -> delete(index): RhinoCommon's File3dmCommonComponentTable.Delete(int) returns
    // nothing (the old one returned true); deleteId has no RhinoCommon overload (reported, kept)
    assert.strictEqual(of.groups().deleteIndex(0), true);
    assert.strictEqual(nf.allGroups.delete(0), undefined);
    assert.strictEqual(nf.allGroups.count, of.groups().count);
    assert.strictEqual(nf.allGroups.count, 2);
    // DracoCompression compressOptions
    const omesh = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1]]) omesh.vertices().add(x, y, 0);
    omesh.faces().addQuadFace(0, 1, 2, 3);
    const oopt = new r.DracoCompressionOptions(), nopt = new r.DracoCompressionOptionsX();
    const odr = r.DracoCompression.compressOptions(omesh, oopt), ndr = r.DracoCompressionX.compress(new r.MeshX(omesh), nopt);
    assert(odr && ndr);
    // the same compressed bytes (the old one exposes them as base64)
    assert.strictEqual(Buffer.from(Array.from(ndr.toByteArray())).toString('base64'), odr.toBase64String());
  });

  t('test_hand_point_cloud_values_and_group_delete_id_g126', () => {
    // kept by hand under the old JS names (Luis 2026-10-30, upstream U38)
    const o = new r.PointCloud(), n = new r.PointCloudX();
    o.addPointValue([1, 0, 0], 1.5); n.addPointValue(P(1, 0, 0), 1.5);
    o.addRangePointsValues([[2, 0, 0], [3, 0, 0]], [2.5, 3.5]); n.addRangePointsValues([P(2, 0, 0), P(3, 0, 0)], [2.5, 3.5]);
    o.insertPointValue(0, [0, 0, 0], 0.5); n.insertPointValue(0, P(0, 0, 0), 0.5);
    assert.deepStrictEqual(Array.from(n.getPointValues()), [0.5, 1.5, 2.5, 3.5]);
    assert.deepStrictEqual(Array.from(new r.PointCloudX(o).getPointValues()), [0.5, 2.5, 3.5, 0]);   // pilot #64 (old)
    assert.deepStrictEqual(Array.from(n.getPoints()).map(q => q.X), [0, 1, 2, 3]);
    // channels stay aligned (pilot #64: the generated insert does not)
    const c = new r.PointCloudX();
    c.add(P(1, 0, 0), P(0, 0, 1)); c.add(P(2, 0, 0), P(0, 1, 0));
    c.insertPointValue(0, P(0, 0, 0), 9);
    assert.deepStrictEqual(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 0], [0, 0, 1], [0, 1, 0]]);
    assert.deepStrictEqual(Array.from(c.getPointValues()), [9, 0, 0]);
    // deleteId
    const of = new r.File3dm(), nf = new r.File3dmX();
    for (let i = 0; i < 3; i++) { of.groups().add(new r.Group()); nf.allGroups.addGroup(); }
    assert.strictEqual(nf.allGroups.deleteId(nf.allGroups.findIndex(1).id), of.groups().deleteId(of.groups().findIndex(1).id));
    assert.strictEqual(nf.allGroups.deleteId(require('crypto').randomUUID()), false);
    assert.strictEqual(nf.allGroups.count, 2);
  });

  t('test_point_cloud_insert_and_add_range_keep_channels_aligned_u37', () => {
    // U37 (RH-99914, pilot #64): insert keeps every channel with its point; a range adding a
    // channel the cloud lacked puts it on the NEW points
    const red = { r: 255, g: 0, b: 0, a: 255 }, lime = { r: 0, g: 255, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 };
    const c = new r.PointCloudX();
    c.add(P(1, 0, 0), P(0, 0, 1), red, 1); c.add(P(2, 0, 0), P(0, 1, 0), lime, 2);
    c.insert(0, P(0, 0, 0), P(1, 0, 0), blue, 9);
    assert.deepStrictEqual(Array.from(c.getPointValues()), [9, 1, 2]);
    assert.deepStrictEqual(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z]), [[1, 0, 0], [0, 0, 1], [0, 1, 0]]);
    assert.deepStrictEqual(Array.from(c.getColors()).map(k => [k.r, k.g, k.b]), [[0, 0, 255], [255, 0, 0], [0, 255, 0]]);
    const d = new r.PointCloudX();
    d.add(P(1, 0, 0)); d.add(P(2, 0, 0));
    d.addRange([P(3, 0, 0), P(4, 0, 0)], [P(0, 0, 1), P(0, 0, 1)]);
    assert.deepStrictEqual(Array.from(d.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 0], [0, 0, 0], [0, 0, 1], [0, 0, 1]]);
  });

  t('test_retired_line_line_tolerance_and_plane_circle_g128', () => {
    // JS1: lineLineTolerance -> lineLine(a, b, tolerance, finiteSegments), now generated (G128)
    const L = (a, b) => ({ From: P(...a), To: P(...b) });
    const cases = [[[0, 0, 0], [4, 0, 0], [2, -1, 0], [2, 3, 0]], [[0, 0, 0], [1, 0, 0], [3, -1, 0], [3, 1, 0]],
                   [[0, 0, 0], [1, 0, 0], [0, 0, 1], [1, 0, 1]]];
    for (const [a0, a1, b0, b1] of cases)
      for (const [tol, finite] of [[0.0, false], [0.001, true], [5.0, true]]) {
        const o = r.Intersection.lineLineTolerance(new r.Line(a0, a1), new r.Line(b0, b1), tol, finite);
        const n = r.IntersectionX.lineLine(L(a0, a1), L(b0, b1), tol, finite);
        assert.deepStrictEqual(Array.from(n), Array.from(o));
      }
    const circle = r.CircleX.create(r.PlaneX.worldZX, 2.0);
    const [kind, t0, t1] = r.IntersectionX.planeCircle(r.PlaneX.worldXY, circle);
    assert.strictEqual(kind, r.PlaneCircleIntersection.Secant);
    assert.deepStrictEqual([t0, t1].map(t => Math.round(r.CircleX.pointAt(circle, t).X * 1e9) / 1e9).sort((x, y) => x - y), [-2, 2]);
  });

  t('test_retired_add_range_with_colors_and_array_natives_g128', () => {
    // JS1: addRangePointsNormalsColors / ...Values -> addRange(points, normals, colors[, values]) (G128)
    const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 };
    const pts = [[0, 0, 0], [1, 0, 0]], nrm = [[0, 0, 1], [0, 1, 0]];
    const sig = c => JSON.stringify([Array.from(c.getNormals()).map(v => [v.X, v.Y, v.Z]),
                                     Array.from(c.getColors()).map(k => [k.r, k.g, k.b]), Array.from(c.getPointValues())]);
    const o3 = new r.PointCloud(), n3 = new r.PointCloudX();
    o3.addRangePointsNormalsColors(pts, nrm, [red, blue]); n3.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)), [red, blue]);
    assert.strictEqual(sig(n3), sig(new r.PointCloudX(o3)));
    const o4 = new r.PointCloud(), n4 = new r.PointCloudX();
    o4.addRangePointsNormalsColorsValues(pts, nrm, [red, blue], [1.5, 2.5]);
    n4.addRange(pts.map(q => P(...q)), nrm.map(q => P(...q)), [red, blue], [1.5, 2.5]);
    assert.deepStrictEqual(Array.from(n4.getPointValues()), [1.5, 2.5]);
    assert.strictEqual(o4.count, 0);            // pilot #66: the old one adds nothing (a chained != in its size check)
    // int / double sequences in and out through the members the same rule unlocked
    const m = new r.MeshX();
    for (let j = 0; j < 3; j++) for (let i = 0; i < 3; i++) m.vertices.add(i, j, 0);
    for (let j = 0; j < 2; j++) for (let i = 0; i < 2; i++) { const a = j * 3 + i; m.faces.addFace(a, a + 1, a + 4, a + 3); }
    assert.deepStrictEqual(Array.from(m.faces.toIntArray(false)).slice(0, 8), [0, 1, 4, 3, 1, 2, 5, 4]);
    assert.deepStrictEqual(Array.from(m.getNakedEdgePointStatus()), [true, true, true, true, false, true, true, true, true]);
    assert.strictEqual(m.faces.deleteFaces([0, 3], true), 2);
    assert.deepStrictEqual([m.faces.count, m.vertices.count], [2, 7]);
    const lt = new r.LinetypeX();
    assert.strictEqual(lt.setSegments([1.0, -0.5]), true);
    assert.deepStrictEqual([0, 1].map(i => Array.from(lt.getSegment(i))), [[1.0, true], [0.5, false]]);
  });

  t('test_retired_append_line_and_add_range_colors_g130', () => {
    // JS1: appendLine -> append(line), addRangePointsColors -> addRange(points, colors) (G130)
    const L = (a, b) => ({ From: P(...a), To: P(...b) });
    const opc = new r.PolyCurve(), npc = new r.PolyCurveX();
    assert.strictEqual(npc.append(L([0, 0, 0], [2, 0, 0])), opc.appendLine(new r.Line([0, 0, 0], [2, 0, 0])));
    assert.strictEqual(npc.append(L([2, 0, 0], [2, 3, 0])), opc.appendLine(new r.Line([2, 0, 0], [2, 3, 0])));
    assert.deepStrictEqual([npc.segmentCount, opc.segmentCount], [2, 2]);
    assert.deepStrictEqual([npc.pointAtEnd.X, npc.pointAtEnd.Y], [2, 3]);
    const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 128, b: 255, a: 255 };
    const o = new r.PointCloud(), n = new r.PointCloudX();
    o.addRangePointsColors([[0, 0, 0], [1, 0, 0]], [red, blue]);
    n.addRange([P(0, 0, 0), P(1, 0, 0)], [red, blue]);
    const cols = c => Array.from(c.getColors()).map(k => [k.r, k.g, k.b]);
    assert.deepStrictEqual(cols(n), cols(new r.PointCloudX(o)));
    assert.deepStrictEqual(cols(n), [[255, 0, 0], [0, 128, 255]]);
  });

  t('test_threejs_buffers_round_trip_g131', () => {
    // three.js (Luis 2026-10-08/-28): toThreejsBuffers kept (as the old one: typed arrays, quads as two
    // triangles), createFromThreejsBuffers its inverse; on a mesh with uvs and colors, both rotations
    const m = new r.MeshX();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.vertices.add(x, y, 0);
    m.faces.addFace(0, 1, 2, 3); m.faces.addFace(1, 4, 2);
    m.normals.computeNormals();
    for (const [u, v] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.textureCoordinates.add(u, v);
    for (let i = 0; i < 5; i++) m.vertexColors.add(255, i * 50, 0);
    const old = new r.Mesh();
    for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) old.vertices().add(x, y, 0);
    old.faces().addQuadFace(0, 1, 2, 3); old.faces().addTriFace(1, 4, 2);
    for (const rot of [false, true]) {
      const b = m.toThreejsBuffers(rot);
      assert(b.position instanceof Float32Array && b.index instanceof Uint32Array && b.normal instanceof Float32Array);
      assert(b.uv instanceof Float32Array && b.color instanceof Float32Array);
      assert.deepStrictEqual(Array.from(b.index), [0, 1, 2, 2, 3, 0, 1, 4, 2]);
      const ob = old.toThreejsBuffers(rot);
      assert.deepStrictEqual(Array.from(b.position), Array.from(ob.position));     // the same as the old one
      assert.deepStrictEqual(Array.from(b.index), Array.from(ob.index));
      // and back: the same vertices, triangles, uvs and colors
      const back = r.MeshX.createFromThreejsBuffers(b, rot);
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
    const tri = r.MeshX.createFromThreejsBuffers({ position: new Float32Array([0, 0, 0, 1, 0, 0, 0, 1, 0]) }, false);
    assert.deepStrictEqual([tri.vertices.count, tri.faces.count], [3, 1]);
    ctx.raises(() => r.MeshX.createFromThreejsBuffers({ position: [0, 0, 0, 1] }, false), /'position' has 4 numbers/);
    ctx.raises(() => r.MeshX.createFromThreejsBuffers({ position: [0, 0, 0, 1, 0, 0, 0, 1, 0], index: [0, 1, 7] }, false), /outside the vertices/);
    // point clouds: positions, colors, normals both ways
    const pc = new r.PointCloudX();
    pc.add(P(1, 2, 3), P(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 }); pc.add(P(4, 5, 6), P(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 });
    const pb = pc.toThreejsBuffers(false);
    assert.deepStrictEqual(Array.from(pb.position), [1, 2, 3, 4, 5, 6]);
    assert.deepStrictEqual(Array.from(pb.color), [1, 0, 0, 0, 0, 1]);
    const pback = r.PointCloudX.createFromThreejsBuffers(pb, false);
    assert.deepStrictEqual(Array.from(pback.getPoints()).map(q => [q.X, q.Y, q.Z]), [[1, 2, 3], [4, 5, 6]]);
    assert.deepStrictEqual(Array.from(pback.getColors()).map(k => [k.r, k.g, k.b]), [[255, 0, 0], [0, 0, 255]]);
    assert.deepStrictEqual(Array.from(pback.getNormals()).map(q => [q.X, q.Y, q.Z]), [[0, 0, 1], [0, 1, 0]]);
    const oldPc = new r.PointCloud(); oldPc.addPointNormalColor([1, 2, 3], [0, 0, 1], { r: 255, g: 0, b: 0, a: 255 });
    oldPc.addPointNormalColor([4, 5, 6], [0, 1, 0], { r: 0, g: 0, b: 255, a: 255 });
    const opb = oldPc.toThreejsBuffers(true), npb = pc.toThreejsBuffers(true);
    assert.deepStrictEqual(Array.from(npb.position), Array.from(opb.position));
    assert.deepStrictEqual(Array.from(npb.normal), Array.from(opb.normal));
  });

  t('test_threejs_json_family_deprecated_g133', () => {
    // the JSON family (Luis 2026-10-31): shipped deprecated in 9.2 -- 8.x's output, a warning once each
    const warned = [];
    const warn = console.warn;
    console.warn = (...a) => warned.push(a.join(' '));
    try {
      const quad = [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]];
      const m = new r.MeshX(), old = new r.Mesh();
      for (const [x, y] of quad) { m.vertices.add(x, y, 0); old.vertices().add(x, y, 0); }
      m.faces.addFace(0, 1, 2, 3); m.faces.addFace(1, 4, 2);
      old.faces().addQuadFace(0, 1, 2, 3); old.faces().addTriFace(1, 4, 2);
      for (let i = 0; i < 5; i++) { m.vertexColors.add(255, i * 50, 0); old.vertexColors().add(255, i * 50, 0); }
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
      same(j, old.toThreejsJSON());
      assert.deepStrictEqual(j.data.index, { type: 'Uint16Array', array: [0, 1, 2, 2, 3, 0, 1, 4, 2] });
      assert.strictEqual(m.normals.count, 0, "the normals are computed on a copy, not on the caller's mesh");
      same(m.toThreejsJSONRotate(true), old.toThreejsJSONRotate(true));
      const m2 = new r.MeshX(), old2 = new r.Mesh();
      for (const [x, y] of [[5, 0], [6, 0], [5, 1]]) { m2.vertices.add(x, y, 0); old2.vertices().add(x, y, 0); }
      m2.faces.addFace(0, 1, 2); old2.faces().addTriFace(0, 1, 2);
      // (8.x's merge mixes a colored mesh with an uncolored one into no colors: compare uncolored)
      const plain = new r.MeshX(), oldPlain = new r.Mesh();
      for (const [x, y] of quad) { plain.vertices.add(x, y, 0); oldPlain.vertices().add(x, y, 0); }
      plain.faces.addFace(0, 1, 2, 3); oldPlain.faces().addQuadFace(0, 1, 2, 3);
      same(r.MeshX.toThreejsJSONMerged([plain, m2], true), r.Mesh.toThreejsJSONMerged([oldPlain, old2], true));
      // and back, as 8.x: a mesh always turned from Y-up
      const back = r.MeshX.createFromThreejsJSON(j), oldBack = r.Mesh.createFromThreejsJSON(j);
      assert.deepStrictEqual([back.vertices.count, back.faces.count], [5, 3]);
      assert.deepStrictEqual(Array.from(back.toThreejsBuffers(false).position), Array.from(oldBack.toThreejsBuffers(false).position));
      assert.deepStrictEqual(Array.from(back.vertexColors.toARGBArray()), Array.from(m.vertexColors.toARGBArray()));
      assert.strictEqual(r.MeshX.createFromThreejsJSON({}), null);
      ctx.raises(() => r.MeshX.createFromThreejsJSON({ data: { index: { array: [0, 1, 7] },
        attributes: { position: { itemSize: 3, array: [0, 0, 0, 1, 0, 0, 0, 1, 0] } } } }), /outside the vertices/);
      // point clouds; an RGBA color's 4th channel is opacity
      const pc = new r.PointCloudX(), oldPc = new r.PointCloud();
      pc.add(P(1, 2, 3), P(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 }); pc.add(P(4, 5, 6), P(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 });
      oldPc.addPointNormalColor([1, 2, 3], [0, 0, 1], { r: 255, g: 0, b: 0, a: 255 });
      oldPc.addPointNormalColor([4, 5, 6], [0, 1, 0], { r: 0, g: 0, b: 255, a: 255 });
      same(pc.toThreejsJSON(), oldPc.toThreejsJSON());
      const rgba = { data: { attributes: { position: { itemSize: 3, array: [1, 2, 3] },
                                           color: { itemSize: 4, array: [1, 0, 0, 0.5] } } } };
      const cols = c => Array.from(c.getColors()).map(k => [k.r, k.g, k.b, k.a]);
      assert.deepStrictEqual(cols(r.PointCloudX.createFromThreejsJSON(rgba)), cols(r.PointCloud.createFromThreejsJSON(rgba)));
      assert.deepStrictEqual(Array.from(r.PointCloudX.createFromThreejsJSON(pc.toThreejsJSON()).getPoints()).map(q => [q.X, q.Y, q.Z]),
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
