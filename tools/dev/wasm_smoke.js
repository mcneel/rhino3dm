// usage: node wasm_smoke.js <path to rhino3dm.js>
// WASM smoke for the generated bindings: the embind half of every
// registration the pilot emits. Mirrors the python regression set.
const factory = require(require('path').resolve(process.argv[2]));

function members(cls) {
  return Object.getOwnPropertyNames(cls.prototype).filter(n => n !== 'constructor');
}

factory().then(rhino => {
  const assert = require('assert');

  // generated classes registered at all?
  for (const cls of ['CurveX','SurfaceX','BrepX','TransformX','MeshX',
                     'ViewportX','ExtrusionX','PointCloudX',
                     'FontX','BezierCurveX','PolyCurveX','LinetypeX',
                     'DimensionStyleX']) {
    assert(rhino[cls], cls + ' missing');
  }
  console.log('  all 13 generated classes registered');

  // generated enum + hand enum
  assert.strictEqual(rhino.Continuity.C2_continuous.value, 3);
  assert.strictEqual(rhino.IsoStatus.X.value, 1);
  console.log('  generated enums present (Continuity, IsoStatus)');

  // Surface -> IsoCurve -> CurveX chain (wrap shape via allow_raw_pointers)
  const sphere = new rhino.Sphere([0, 0, 0], 5);
  const srf = sphere.toNurbsSurface();
  const sx = new rhino.SurfaceX(srf);
  // the JS same-arity prune kept the IsoStatus overload; embind enums
  // require the enum OBJECT (a raw number silently miscasts to 0 = not_iso)
  const iso = sx.isoCurve(rhino.IsoStatus.X, 0.5);
  assert(iso instanceof rhino.CurveX, 'isoCurve did not return CurveX');
  const p = iso.pointAt(0.3);
  assert(typeof p.X === 'number' && typeof p.Z === 'number');   // generated Point3dX value object
  console.log('  SurfaceX.isoCurve -> CurveX.pointAt:', [p.X, p.Y, p.Z].map(v => v.toFixed(4)));

  // holder shape: spanVector -> JS array
  const sv = iso.spanVector();
  assert(Array.isArray(sv) && sv.length >= 2);
  console.log('  CurveX.spanVector (holder shape):', sv);

  // enum-typed arg through embind
  const dom = iso.domain !== undefined ? null : null; // domain not generated; use fixed t
  assert.strictEqual(typeof iso.isContinuous(rhino.Continuity.C1_continuous, 0.5), 'boolean');
  console.log('  CurveX.isContinuous(Continuity, t): enum arg dispatches');

  // Mesh topology verdicts vs hand
  const mesh = new rhino.Mesh();
  mesh.vertices().add(0, 0, 0); mesh.vertices().add(1, 0, 0);
  mesh.vertices().add(1, 1, 0); mesh.vertices().add(0, 1, 0);
  mesh.faces().addQuadFace(0, 1, 2, 3);  // hand JS invented name (W2b catalog)
  const mx = new rhino.MeshX(mesh);
  assert.strictEqual(mx.isClosed, false);
  assert.strictEqual(mx.isManifold(), true);
  console.log('  MeshX: isClosed/isManifold verdicts correct');

  // ViewportX: blittable point arg + property + multi-out tuple
  const vp = new rhino.ViewportInfo();
  const vx = new rhino.ViewportX();
  assert.strictEqual(vx.setCameraLocation({ X: 1, Y: 2, Z: 3 }), true);
  assert.deepStrictEqual(vx.cameraLocation, { X: 1, Y: 2, Z: 3 });
  assert.strictEqual(vx.setFrustum(-1, 1, -0.5, 0.5, 0.1, 100), true);
  const fr = vx.getFrustum();
  assert.deepStrictEqual(fr, [true, -1, 1, -0.5, 0.5, 0.1, 100]);
  console.log('  ViewportX: camera roundtrip + (success, 6-out) tuple exact');

  // new batch: Linetype mutation-on-copy through the generated class
  const lt = new rhino.Linetype();
  const ltx = new rhino.LinetypeX();
  assert.strictEqual(ltx.appendSegment(1.5, true), 0);
  let pl = ltx.patternLength;
  if (typeof pl === 'function') pl = ltx.patternLength();
  assert.strictEqual(pl, 1.5);
  console.log('  LinetypeX: appendSegment mutates the copy; patternLength 1.5');

  // inheritance tier
  assert(rhino.GeometryBaseX && rhino.ModelComponentX, 'bases missing');
  const sphere2 = new rhino.Sphere([0, 0, 0], 3);
  const sx2 = new rhino.SurfaceX(sphere2.toNurbsSurface());
  assert(sx2 instanceof rhino.GeometryBaseX, 'SurfaceX not a GeometryBaseX');
  let deform = sx2.isDeformable;
  if (typeof deform === 'function') deform = sx2.isDeformable();
  assert(typeof deform === 'boolean');
  const ltx2 = new rhino.LinetypeX();
  assert(ltx2 instanceof rhino.ModelComponentX, 'LinetypeX not a ModelComponentX');
  let hn = ltx2.hasName;
  if (typeof hn === 'function') hn = ltx2.hasName();
  assert.strictEqual(hn, false);
  console.log('  inheritance: instanceof + inherited members through embind base<>');

  // deep chain: PolyCurveX : CurveX : GeometryBaseX
  const pcrv = new rhino.PolyCurve();
  pcrv.appendCurve(new rhino.LineCurve([0, 0, 0], [2, 0, 0]));  // hand JS invented name
  const pcx = new rhino.PolyCurveX(pcrv);
  assert(pcx instanceof rhino.CurveX && pcx instanceof rhino.GeometryBaseX);
  const mid = pcx.pointAt(1.0);   // domain [0,2]           // inherited from CurveX
  assert(Math.abs(mid.X - 1.0) < 1e-9);
  console.log('  deep chain: PolyCurveX inherits CurveX.pointAt through two-level base<>');

  // constructor tier: standalone construction, no hand bridge
  const vx3 = new rhino.ViewportX();
  assert.strictEqual(vx3.setCameraLocation({ X: 7, Y: 8, Z: 9 }), true);
  assert.deepStrictEqual(vx3.cameraLocation, { X: 7, Y: 8, Z: 9 });
  const pcx2 = new rhino.PolyCurveX();
  assert.strictEqual(pcx2.segmentCount, 0);
  console.log('  default ctors: ViewportX/PolyCurveX constructible standalone');

  // value types: plain GC'd {X,Y,Z} objects + functions from the C# bodies
  const P = rhino.Point3dX;
  const vp1 = { X: 1, Y: 2, Z: 3 }, vq1 = { X: 4, Y: 6, Z: 3 };
  assert.strictEqual(P.DistanceTo(vp1, vq1), 5);
  const un = P.Unset();
  assert.strictEqual(P.DistanceTo(un, vq1), 0);          // RhinoCommon semantics
  assert.strictEqual(P.IsValid(un), false);
  const vd = P.op_Subtraction(vq1, vp1);                       // Point3d - Point3d -> Vector3d
  assert.deepStrictEqual(vd, { X: 3, Y: 4, Z: 0 });
  assert.strictEqual(rhino.Vector3dX.Length(vd), 5);
  const viv = { T0: 2, T1: 10 };
  assert.strictEqual(rhino.IntervalX.ParameterAt(viv, 0.25), 4);
  console.log('  value types: plain objects', JSON.stringify(vd), '| Unset.DistanceTo ->', P.DistanceTo(un, vq1));

  // classes as arguments: populate a bridge-free PolyCurveX with generated curves
  const pcx3 = new rhino.PolyCurveX();
  const seg = new rhino.CurveX(new rhino.LineCurve([0, 0, 0], [2, 0, 0]));
  assert.strictEqual(pcx3.append(seg), true);
  seg.delete();                                         // polycurve holds a copy
  assert.strictEqual(pcx3.segmentCount, 1);
  console.log('  class args: PolyCurveX().append(CurveX) -> segmentCount', pcx3.segmentCount);

  // concrete subtypes: a populated curve with nothing from the old bindings
  const lcx = new rhino.LineCurveX({ X: 0, Y: 0, Z: 0 }, { X: 2, Y: 0, Z: 0 });
  assert(lcx instanceof rhino.CurveX);
  assert.deepStrictEqual(lcx.pointAt(1.0), { X: 1, Y: 0, Z: 0 });
  const pcx4 = new rhino.PolyCurveX();
  assert.strictEqual(pcx4.append(lcx), true);
  const isoN = new rhino.SurfaceX(new rhino.Sphere([0, 0, 0], 5).toNurbsSurface()).isoCurve(rhino.IsoStatus.X, 0.5);
  assert(isoN instanceof rhino.NurbsCurveX, 'wrap did not downcast');
  console.log('  subtypes: LineCurveX standalone, PolyCurveX.append, isoCurve ->', isoN.constructor.name);

  // G39 table protocols: iteration over the native component list survives a
  // delete (pilot-findings #24: the old `for (i < count) get(i)` silently drops items)
  if (rhino.File3dmX) {
    const f = new rhino.File3dmX();
    const t = f.allLayers;
    for (const n of 'abc') t.addLayer(n, { r: 0, g: 0, b: 0, a: 255 });
    assert.strictEqual(t.delete(t.findIndex(1)), true);
    assert.strictEqual(t.count, 2);
    assert.deepStrictEqual([...t].map(l => l.name), ['a', 'c']);
    assert.deepStrictEqual([...f.layers].map(l => l.index), [0, 2]);
    assert.strictEqual(t.get(1), null);                   // a hole: RhinoCommon's indexer
    assert.strictEqual(t.get(2).name, 'c');
    t.addLayer('d', { r: 0, g: 0, b: 0, a: 255 });
    assert.deepStrictEqual(Array.from(t, l => l.name), ['a', 'c', 'd']);
    console.log('  File3dmLayerTableX: for...of after a delete ->', [...t].map(l => l.name));
  }

  // option 4 (G80/G81): Point3dList and Polyline are RhinoCommon's classes,
  // a C++-owned list with RhinoList members; PolylineX derives Point3dListX
  if (rhino.PolylineX) {
    const pl = new rhino.PolylineX();
    pl.add(0, 0, 0); pl.add(3, 0, 0); pl.add({ X: 3, Y: 4, Z: 0 }); pl.add(0, 0, 0);
    assert(pl instanceof rhino.Point3dListX);
    assert.strictEqual(pl.count, 4);
    assert.strictEqual(pl.segmentCount, 3);
    assert.strictEqual(pl.length, 12);
    assert.strictEqual(pl.isClosed, true);
    const dup = pl.duplicate();                           // a copy: the original is untouched
    dup.set(1, { X: 6, Y: 0, Z: 0 });
    assert.deepStrictEqual(pl.get(1), { X: 3, Y: 0, Z: 0 });
    assert.deepStrictEqual(dup.get(1), { X: 6, Y: 0, Z: 0 });
    assert.deepStrictEqual(pl.toArray().length, 4);
    console.log('  PolylineX: count', pl.count, '| length', pl.length, '| isClosed', pl.isClosed);
    // G82/G83: a sequence argument takes a JS array or a point list
    if (rhino.PointCloudX && rhino.PointCloudX.prototype.addRange) {
      const pcl = new rhino.PointCloudX();
      pcl.addRange([{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }]);
      pcl.addRange(pl);
      assert.strictEqual(pcl.count, 6);
      console.log('  sequences: PointCloudX.addRange(array | PolylineX) -> count', pcl.count);
      pcl.delete();
    }
    dup.delete(); pl.delete();
  }

  console.log('WASM TIER: PASS');
}).catch(e => { console.error('SMOKE FAIL:', e); process.exit(1); });
