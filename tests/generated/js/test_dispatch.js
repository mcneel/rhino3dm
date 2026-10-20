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
};
