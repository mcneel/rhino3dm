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
    oa.setObjectFrame(r.PlaneX.WorldXY);              // the Plane overload (unreachable before)
    oa.setObjectFrame(r.TransformX.Identity);         // the Transform overload
  });

  t('test_js_box_contains_box_takes_the_box_overload', () => {
    const big = r.BoxX.Create(r.PlaneX.WorldXY, r.IntervalX.Create(0, 10), r.IntervalX.Create(0, 10), r.IntervalX.Create(0, 10));
    const small = r.BoxX.Create(r.PlaneX.WorldXY, r.IntervalX.Create(1, 2), r.IntervalX.Create(1, 2), r.IntervalX.Create(1, 2));
    assert.strictEqual(r.BoxX.Contains(big, small), true, 'a Box has a Z (an interval): it is not a point');
    assert.strictEqual(r.BoxX.Contains(big, P(5, 5, 5)), true);
    assert.strictEqual(r.BoxX.Contains(big, P(50, 5, 5)), false);
  });

  t('test_js_null_reaches_the_optional_overload', () => {
    const pc = new r.PolyCurveX();
    assert.strictEqual(pc.append(null), false, 'null is not a value object');
  });
};
