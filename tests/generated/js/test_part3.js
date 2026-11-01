// JS port of tests/generated/test_generated.py, part 3: from test_file_dates_and_native_guards
// up to (not including) test_annotation_bounding_box_overrides_geometry_base. Same names, same
// contracts, against the compiled WASM module.
//
// Files: the module has no host file system, but its own (MEMFS) one is real: where Python
// writes to tmp_path and reads back, these write and read under /tmp inside the module (or go
// through File3dm.toByteArray / fromByteArray).
module.exports = (t, ctx) => {
  const { r, assert, approx } = ctx;
  const P = (x, y, z) => ({ X: x, Y: y, Z: z });
  const V = P;
  const I = (a, b) => ({ T0: a, T1: b });
  const ZERO_ID = '00000000-0000-0000-0000-000000000000';

  // --- fixtures (test_generated.py) ---
  const quadMesh = () => {
    const m = new r.Mesh();
    for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]]) m.vertices.add(x, y, z);
    m.faces.addFace(0, 1, 2, 3);
    return m;
  };
  const boxBrep = () => r.Brep.createFromBox(r.BoundingBox.create(0, 0, 0, 1, 2, 3));
  // a RhinoCommon list (count + indexer) as an array: Python iterates these through the item protocol
  const seq = coll => {
    if (coll && typeof coll[Symbol.iterator] === 'function') return [...coll];
    const out = [];
    for (let i = 0; i < coll.count; i++) out.push(coll.get(i));
    return out;
  };
  const isUuid = s => typeof s === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(s);
  // Point3dList / Polyline from points: Python's Polyline([..]) constructor is not
  // registered in JS (see test_polylinecurve_from_points_chooses_its_creation_call), so the
  // fixtures build the list with addRange (the constructor itself is checked at the end of
  // test_point3dlist_is_a_real_list_with_rhinolist_semantics / test_polyline_geometry_members)
  const polyline = pts => { const p = new r.Polyline(); p.addRange(pts); return p; };
  const pointList = pts => { const p = new r.Point3dList(); p.addRange(pts); return p; };
  const xs = pl => pl.toArray().map(p => p.X);

  t('test_file_dates_and_native_guards', () => {
    const f = new r.File3dm();
    assert(f.created instanceof Date && f.lastEdited instanceof Date);
    const path = '/tmp/part3_dated.3dm';
    assert.strictEqual(f.write(path, 7), true);
    const g = r.File3dm.read(path);
    assert(g.created >= new Date(2000, 0, 1) && g.lastEdited >= g.created);
    const m = quadMesh();
    m.vertices.hide(1);
    assert.strictEqual(m.vertices.isHidden(1), true);
    assert.strictEqual(m.vertices.isHidden(0), false);
    m.vertices.hide(1);                                   // already hidden: returns at the guard
    m.vertices.show(1);
    assert.strictEqual(m.vertices.isHidden(1), false);
  });

  t('test_color4f_and_the_members_it_unlocks', () => {
    const c = r.Color4f.create(0.25, 0.5, 0.75, 1.0);
    assert.deepStrictEqual([c.R, c.G, c.B, c.A], [0.25, 0.5, 0.75, 1.0]);
    assert.strictEqual(r.Color4f.equals(c, r.Color4f.create(0.25, 0.5, 0.75, 1.0)), true);
    assert.strictEqual(r.Color4f.equals(r.Color4f.white, r.Color4f.create(1, 1, 1, 1)), true);
    const m = new r.Material();
    m.toPhysicallyBased();
    const pbr = m.physicallyBased;
    pbr.baseColor = c;
    assert.deepStrictEqual(pbr.baseColor, c);
    const s = new r.Sun();
    assert(s.light instanceof r.Light);
    const tx = new r.Texture();
    const fr = tx.fileReference;
    assert(fr == null || fr instanceof r.FileReference);
  });

  t('test_duplicate_returns_an_owned_most_derived_copy', () => {
    const b = boxBrep();
    const d = b.duplicate();
    assert(d instanceof r.Brep && !d.isAliasOf(b) && d.faces.count === b.faces.count);
    const pc = new r.PolyCurve();
    pc.append(new r.LineCurve(P(0, 0, 0), P(1, 0, 0)));
    const pd = pc.duplicate();
    assert(pd instanceof r.PolyCurve && pd.segmentCount === 1);
    const nc = r.NurbsCurve.createFromLine(r.Line.create(P(0, 0, 0), P(0, 1, 0)));
    // Python calls the base's unbound Curve.Duplicate(nc): the same dispatch through the prototype
    const cd = r.Curve.prototype.duplicate.call(nc);
    assert(cd instanceof r.NurbsCurve, 'Curve.duplicate on a NurbsCurve is ' + (cd && cd.constructor.name));
  });

  t('test_value_type_arrays_are_sequences', () => {
    const B = r.BoundingBox;
    let bb = B.create(P(0, 0, 0), P(1, 2, 3));
    const corners = B.getCorners(bb);
    assert(Array.isArray(corners) && corners.length === 8);
    assert.deepStrictEqual(corners[0], P(0, 0, 0));
    assert.deepStrictEqual(corners[6], P(1, 2, 3));
    assert.deepStrictEqual(B.getCorners(B.unset), []);
    const xf = r.Transform.translation(V(1, 0, 0));
    const moved = r.Transform.transformList(xf, [P(0, 0, 0), P(1, 1, 1)]);
    assert.deepStrictEqual(moved, [P(1, 0, 0), P(2, 1, 1)]);
    assert.strictEqual(r.Transform.toFloatArray(xf, true).length, 16);
    const tr = B.transform(bb, xf);                       // a mutating value member: {self, returns}
    assert.strictEqual(tr.returns, true);
    bb = tr.self;
    assert.deepStrictEqual(bb.Min, P(1, 0, 0));
    assert.deepStrictEqual(B.closestPoint(bb, P(-5, 1, 1)), P(bb.Min.X, 1, 1));
    const rot = r.Plane.rotate(r.Plane.worldXY, Math.PI / 2, V(0, 0, 1));
    assert.strictEqual(rot.returns, true);
    assert(Math.abs(rot.self.XAxis.Y - 1.0) < 1e-12);
    const box = r.Box.create(B.create(P(0, 0, 0), P(1, 1, 1)));
    assert.strictEqual(r.Box.contains(box, P(0.5, 0.5, 0.5)), true);
    assert.strictEqual(r.Box.getCorners(box).length, 8);
  });

  t('test_embedded_files_add_and_iterate', () => {
    // no host file in WASM: a file in the module's own file system to embed (any bytes do)
    const data = '/tmp/part3_note.txt';
    assert.strictEqual(new r.File3dm().write(data, 7), true);
    const f = new r.File3dm();
    assert.strictEqual(f.embeddedFiles.add(data), true);
    const files = ctx.list(f.embeddedFiles);
    assert(files.length === 1 && files[0] instanceof r.File3dmEmbeddedFile);
    assert(files[0].filename.endsWith('note.txt'), files[0].filename);
    assert.strictEqual(files[0].componentType, r.ModelComponentType.EmbeddedFile);
    const path = '/tmp/part3_embedded.3dm';
    assert.strictEqual(f.write(path, 7), true);
    const g = r.File3dm.read(path);
    assert.deepStrictEqual(ctx.list(g.embeddedFiles).map(e => e.filename), [files[0].filename]);
  });

  t('test_split_returns_the_halves_or_none', () => {
    const c = new r.LineCurve(P(0, 0, 0), P(2, 0, 0));
    const parts = c.split(1.0);
    assert(parts.length === 2 && parts.every(p => p instanceof r.Curve));
    assert.deepStrictEqual(parts[0].pointAtEnd, P(1, 0, 0));
    assert.deepStrictEqual(parts[1].pointAtStart, P(1, 0, 0));
    assert(c.split(5.0) == null, 'split outside the domain: ' + c.split(5.0));
    const nc = r.NurbsCurve.createFromCircle(r.Circle.create(P(0, 0, 0), 1.0));
    const bz = nc.convertSpanToBezier(0);
    const [ok, left, right] = bz.split(0.5);
    assert(ok === true && left instanceof r.BezierCurve && right instanceof r.BezierCurve);
    assert.deepStrictEqual(left.pointAt(1.0), right.pointAt(0.0));
    assert.deepStrictEqual(right.pointAt(0.0), bz.pointAt(0.5));
    const bad = bz.split(2.0);
    assert(bad.length === 3 && bad[0] === false && bad[1] == null && bad[2] == null, String(bad));
  });

  t('test_explode_duplicates_each_segment', () => {
    const pc = new r.PolyCurve();
    pc.append(new r.LineCurve(P(0, 0, 0), P(1, 0, 0)));
    pc.append(new r.LineCurve(P(1, 0, 0), P(1, 1, 0)));
    const segs = pc.explode();
    assert(segs.length === 2 && segs.every(s => s instanceof r.LineCurve));
    assert.deepStrictEqual(segs[1].pointAtEnd, P(1, 1, 0));
    assert.deepStrictEqual(new r.PolyCurve().explode(), []);
  });

  t('test_vertex_arrays_read_one_vertex_per_call', () => {
    const mx = quadMesh();
    const pts = mx.vertices.toPoint3dArray();
    assert.deepStrictEqual(pts.map(p => [p.X, p.Y, p.Z]), [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]]);
    const pf = mx.vertices.toPoint3fArray();
    // Point3f is a plain {X, Y, Z} value in JS (no class to instanceof)
    assert(pf.every(p => ['X', 'Y', 'Z'].every(k => typeof p[k] === 'number')));
    assert.deepStrictEqual(pf.map(p => [p.X, p.Y]), [[0, 0], [1, 0], [1, 1], [0, 1]]);
    assert.deepStrictEqual(new r.Mesh().vertices.toPoint3dArray(), []);
  });

  t('test_brep_adjacency_holders_are_empty_when_the_native_says_so', () => {
    const b = boxBrep();
    const v = b.vertices.get(0);
    const edges = v.edgeIndices();
    assert(edges.length === 3 && edges.every(i => Number.isInteger(i)));
    const e = b.edges.get(0);
    assert.strictEqual(e.adjacentFaces().length, 2);
    assert.strictEqual(e.trimIndices().length, 2);
    const f = b.faces.get(0);
    assert.strictEqual(f.adjacentEdges().length, 4);
    assert.strictEqual(f.adjacentFaces().length, 4);
  });

  t('test_to_nurbs_surface_leads_its_tuple_with_the_object_and_forwards_without_the_out', () => {
    const srf = boxBrep().faces.get(0).underlyingSurface();
    const res = srf.toNurbsSurface(0.0);
    assert(Array.isArray(res) && res.length === 2, 'toNurbsSurface(tol) -> ' + res);
    const [ns, accuracy] = res;
    assert(ns instanceof r.NurbsSurface && Number.isInteger(accuracy) && accuracy > 0);
    assert(srf.toNurbsSurface() instanceof r.NurbsSurface);
  });

  t('test_mesh_append_skips_null_and_itself_and_default_constructs', () => {
    const m = new r.Mesh();
    assert.strictEqual(m.vertices.count, 0);
    m.append(null);
    m.append(m);
    assert.strictEqual(m.vertices.count, 0);
    const q = quadMesh();
    m.append(q);
    assert.strictEqual(m.vertices.count, 4);
    assert.strictEqual(m.faces.count, 1);
    m.append(q);
    assert.strictEqual(m.vertices.count, 8);
    assert.strictEqual(new r.PointCloud().count, 0);
    assert.strictEqual(new r.Brep().faces.count, 0);
  });

  t('test_translated_members_return_and_construct_class_values', () => {
    const pc = new r.PointCloud();
    const item = pc.appendNew();
    assert(item instanceof r.PointCloudItem && pc.count === 1);
    assert.deepStrictEqual(item.location, P(0, 0, 0));
    pc.add(P(1, 1, 1));
    assert.strictEqual(pc.insertNew(1).index, 1);
    assert.strictEqual(pc.count, 3);
    const nc = r.NurbsCurve.createFromEllipse(r.Ellipse.create(r.Plane.worldXY, 2.0, 1.0));
    assert(nc instanceof r.NurbsCurve && Math.abs(nc.pointAtStart.X - 2.0) < 1e-9);
    const d = boxBrep().duplicateBrep();
    assert(d instanceof r.Brep && d.faces.count === 6);
    assert(quadMesh().duplicateMesh() instanceof r.Mesh);
    const MP = r.MeshingParameters;
    assert(new MP() instanceof MP);
    assert(MP.default instanceof MP);
    const minimal = MP.minimal;
    assert.strictEqual(minimal.gridMinCount, 16);
    assert.strictEqual(minimal.jaggedSeams, true);
    assert.strictEqual(MP.smooth.simplePlanes, true);
    assert.strictEqual(minimal.simplePlanes, false);
    assert(new r.ViewInfo() instanceof r.ViewInfo);
  });

  t('test_annotation_members_read_with_the_parent_dimstyle_null', () => {
    // Python checks the class's property objects (and that the rw ones have a setter); embind
    // installs a throwing "read-only property" setter on every read-only property, so a real
    // setter is one that is not that stub
    const proto = r.AnnotationBase.prototype;
    const desc = name => Object.getOwnPropertyDescriptor(proto, name);
    const writable = d => d && typeof d.set === 'function' && !/read-only/.test(String(d.set));
    for (const name of ['textHeight', 'dimensionScale', 'maskEnabled', 'dimensionStyleId', 'richText']) {
      const d = desc(name);
      assert(d && typeof d.get === 'function', name + ' is not a property');
      assert(writable(d), name + ' has no setter');
    }
    for (const name of ['maskFrame', 'maskColorSource', 'dimensionLengthDisplay', 'alternateDimensionLengthDisplay']) {
      const d = desc(name);
      assert(d && typeof d.get === 'function', name + ' is not a property');
    }
    assert(typeof proto.runReplace === 'function' && typeof proto.setRichText === 'function');
  });

  t('test_face_and_loop_scoped_lists', () => {
    const b = boxBrep();
    assert.strictEqual(b.loops.count, 6);
    for (let fi = 0; fi < b.faces.count; fi++) {
      const f = b.faces.get(fi);
      const loops = f.loops;
      assert(loops instanceof r.BrepLoopList && loops.count === 1);
      const outer = f.outerLoop;
      assert.strictEqual(outer.loopIndex, loops.get(0).loopIndex);
      const trims = outer.trims;
      assert(trims instanceof r.BrepTrimList && trims.count === 4);
      const ts = seq(trims);
      assert(ts.every(tr => tr.loop.loopIndex === outer.loopIndex));
      assert(ts.every(tr => tr.face.faceIndex === fi));
      assert(ts.every(tr => tr.edge instanceof r.BrepEdge));
    }
    assert.strictEqual(b.trims.count, 24);
    assert.deepStrictEqual(seq(b.loops).map(l => l.loopIndex), [0, 1, 2, 3, 4, 5]);
    ctx.raises(() => b.faces.get(0).loops.get(1), /out_of_range/);
  });

  t('test_rdk_float_and_enum_mapped_variant_properties', () => {
    const rs = new r.File3dm().settings.renderSettings;
    const lw = rs.linearWorkflow;
    lw.preProcessGamma = 2.5;
    assert(Math.abs(lw.preProcessGamma - 2.5) < 1e-6, String(lw.preProcessGamma));
    lw.postProcessGamma = 1.75;
    assert(Math.abs(lw.postProcessGamma - 1.75) < 1e-6, String(lw.postProcessGamma));
    const d = rs.dithering;
    const M = r.DitheringMethods;                      // RhinoCommon's Dithering.Methods
    assert.strictEqual(M.SimpleNoise.value, 2);
    assert.strictEqual(M.FloydSteinberg.value, 1);
    d.method = M.SimpleNoise;
    assert.strictEqual(d.method, M.SimpleNoise);
    d.method = M.FloydSteinberg;
    assert.strictEqual(d.method, M.FloydSteinberg);
  });

  t('test_translated_string_members', () => {
    const A = r.AnnotationBase;
    assert.strictEqual(A.plainTextToRtf('a\nb'), '{\\rtf1{\\ltrch a}\\par {b}}');
    assert.strictEqual(A.plainTextToRtf('x{y}\\'), '{\\rtf1{\\ltrch x\\{y\\}\\\\}}');
    // written, then read back through bytes
    const w = new r.File3dm();
    w.strings.setString('plain', 'v');
    w.strings.setString('sec', 'key', 'v2');
    const f = r.File3dm.fromByteArray(w.toByteArray());
    assert.strictEqual(f.strings.documentUserTextCount, 1);
    assert.strictEqual(f.strings.getValue('sec', 'key'), 'v2');
    assert.strictEqual(f.strings.getValue('', 'key'), '');
    const mx = quadMesh();
    assert.strictEqual(mx.vertexColors.add({ r: 255, g: 0, b: 0, a: 255 }), 0);
    assert.strictEqual(mx.vertexColors.count, 1);
  });

  t('test_point3dlist_is_a_real_list_with_rhinolist_semantics', () => {
    const pl = pointList([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)]);
    assert.strictEqual(pl.count, 3);
    assert.deepStrictEqual(pl.get(1), P(1, 0, 0));
    assert.deepStrictEqual(xs(pl), [3, 1, 2]);
    pl.set(0, P(0, 0, 0));
    pl.add(P(5, 0, 0)); pl.add(6, 0, 0);                  // RhinoList.Add(T), Point3dList.Add(x, y, z)
    pl.insert(1, P(9, 0, 0)); pl.removeAt(0);
    assert.deepStrictEqual(xs(pl), [9, 1, 2, 5, 6]);
    assert.strictEqual(pl.indexOf(P(2, 0, 0)), 2);
    assert.strictEqual(pl.contains(P(5, 0, 0)), true);
    assert.strictEqual(pl.contains(P(7, 0, 0)), false);
    pl.reverse();
    assert.deepStrictEqual(xs(pl), [6, 5, 2, 1, 9]);
    pl.sort();                                            // Point3d.CompareTo: X, then Y, then Z
    assert.deepStrictEqual(xs(pl), [1, 2, 5, 6, 9]);
    assert.strictEqual(pl.binarySearch(P(5, 0, 0)), 2);
    assert.strictEqual(pl.binarySearch(P(3, 0, 0)), ~2);
    ctx.raises(() => pl.get(5), /out_of_range/);
    const e = new r.Point3dList();
    assert.strictEqual(e.capacity, 0);
    e.add(P(0, 0, 0));
    assert.strictEqual(e.capacity, 4);
    for (let i = 0; i < 4; i++) e.add(P(i, 0, 0));
    assert.strictEqual(e.capacity, 8);
    assert.strictEqual(e.count, 5);
    const d = pl.duplicate();
    d.set(0, P(42, 0, 0));
    assert.deepStrictEqual(pl.get(0), P(1, 0, 0));
    assert(d instanceof r.Point3dList);
    assert.deepStrictEqual(pl.toArray(), seq(pl));
    const bb = pl.boundingBox;
    assert.deepStrictEqual(bb.Min, P(1, 0, 0));
    assert.deepStrictEqual(bb.Max, P(9, 0, 0));
    // Python's first line: Point3dList([..]) is a 3-point list. JS registers only
    // Point3dList() and Point3dList(int capacity), and an array passed to the latter is
    // silently miscast (an empty list), so the constructor contract is checked last
    assert.strictEqual(new r.Point3dList([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)]).count, 3,
      'new Point3dList(points) is not a list of those points');
  });

  t('test_polyline_geometry_members', () => {
    const poly = polyline([P(0, 0, 0), P(2, 0, 0), P(2, 2, 0)]);
    assert(poly instanceof r.Point3dList);               // Polyline : Point3dList
    assert.strictEqual(poly.segmentCount, 2);
    assert(Math.abs(poly.length - 4.0) < 1e-12);
    assert.deepStrictEqual(poly.pointAt(1.5), P(2, 1, 0));
    assert.deepStrictEqual(poly.segmentAt(1).From, P(2, 0, 0));
    assert(Math.abs(poly.closestParameter(P(1, 5, 0)) - 2.0) < 1e-12);
    assert.deepStrictEqual(poly.closestPoint(P(1, -3, 0)), P(1, 0, 0));
    const tr = poly.trim(I(0.5, 1.5));
    assert(tr instanceof r.Polyline);
    assert.deepStrictEqual(tr.toArray(), [P(1, 0, 0), P(2, 0, 0), P(2, 1, 0)]);
    assert.deepStrictEqual(poly.getSegments().map(s => r.Line.Length(s)), [2.0, 2.0]);
    poly.add(P(2, 2, 0));                                 // a zero-length segment
    assert.strictEqual(poly.deleteShortSegments(1e-9), 1);
    assert.strictEqual(poly.count, 3);
    const c = poly.duplicate();
    assert(c instanceof r.Polyline, 'Polyline.duplicate is ' + (c && c.constructor.name));
    assert.deepStrictEqual(c.toArray(), poly.toArray());
    // Polyline(points): the same silent miscast to Polyline(int capacity) as Point3dList
    assert.strictEqual(new r.Polyline([P(0, 0, 0), P(2, 0, 0), P(2, 2, 0)]).count, 3,
      'new Polyline(points) is not a polyline through those points');
  });

  t('test_sequence_arguments_where_rhinocommon_takes_ienumerable', () => {
    const pc = new r.PointCloud();
    pc.addRange([P(0, 0, 0), P(1, 0, 0)]);
    pc.addRange(polyline([P(2, 0, 0), P(3, 0, 0)]));
    assert.strictEqual(pc.count, 4);
    assert.deepStrictEqual(pc.getPoints().map(p => p.X), [0, 1, 2, 3]);
    // insertRange: generated since U11 fixed its native (test_point_cloud_insert_range_u11)
    const n = new r.PointCloud();
    n.addRange([P(0, 0, 0)], [V(0, 0, 1)]);
    assert.strictEqual(n.count, 1);
    assert.strictEqual(n.containsNormals, true);
    n.addRange([P(0, 0, 0)], []);                          // no normals: C# returns early
    assert.strictEqual(n.count, 1);
    ctx.raises(() => n.addRange([P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1)]), /invalid_argument,Must supply equal number of points and vectors/);
    const f = new r.File3dm();
    const pid = f.objects.addPolyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)], null);
    assert.strictEqual(f.objects.count, 1);
    assert(isUuid(pid) && pid !== ZERO_ID, String(pid));
    assert.strictEqual(f.objects.addPolyline([], null), ZERO_ID);
    assert.strictEqual(f.objects.count, 1);
  });

  t('test_polylinecurve_to_polyline_through_the_translated_static', () => {
    const crv = new r.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]);
    const pl = crv.toPolyline();
    assert(pl instanceof r.Polyline && pl.count === 3);
    assert.deepStrictEqual(pl.get(2), P(3, 4, 0));
    assert(Math.abs(pl.length - 7.0) < 1e-12);
    pl.set(0, P(1, 0, 0));                                 // a copy: the curve is unchanged
    assert.deepStrictEqual(crv.toPolyline().get(0), P(0, 0, 0));
  });

  t('test_polylinecurve_from_points_chooses_its_creation_call', () => {
    // Python: PolylineCurve(points) -- `if (count < 1) New(IntPtr.Zero) else New2(count, pts)`.
    // JS registers only PolylineCurve() and PolylineCurve(old PolylineCurve): the
    // points constructor is the contract here, so its absence fails the test
    const make = pts => {
      try { return new r.PolylineCurve(pts); }
      catch (e) { assert.fail('JS has no PolylineCurve(points) constructor: ' + (e && e.message)); }
    };
    const crv = make([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]);
    assert.strictEqual(crv.pointCount, 3);
    assert.deepStrictEqual(crv.pointAtEnd, P(3, 4, 0));
    assert.strictEqual(make(polyline([P(0, 0, 0), P(1, 0, 0)])).pointCount, 2);
    assert.strictEqual(make([]).pointCount, 0);           // the empty arm: ON_PolylineCurve_New
  });

  t('test_polyline_to_polylinecurve_constructs_the_generated_class', () => {
    const poly = polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]);
    const crv = poly.toPolylineCurve();
    assert(crv instanceof r.PolylineCurve && crv.pointCount === 3);
    assert.deepStrictEqual(crv.pointAtEnd, P(3, 4, 0));
    assert(polyline([P(0, 0, 0)]).toPolylineCurve() == null);   // `if (m_size < 2) return null;`
    assert.deepStrictEqual(crv.toPolyline().toArray(), poly.toArray());
  });

  t('test_polyline_to_nurbscurve_through_a_disposed_polylinecurve', () => {
    const nc = polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]).toNurbsCurve();
    assert(nc instanceof r.NurbsCurve);
    assert.deepStrictEqual(nc.pointAtEnd, P(3, 4, 0));
    assert.deepStrictEqual(nc.pointAtStart, P(0, 0, 0));
    assert(polyline([P(0, 0, 0)]).toNurbsCurve() == null);       // no curve: `pc?.` is null
  });

  t('test_curve_gap_members_orientation_trim_circle_derivatives', () => {
    const circle = new r.ArcCurve(r.Circle.create(P(0, 0, 0), 2.0));
    const O = r.CurveOrientation;
    assert.strictEqual(circle.closedCurveOrientation(), O.CounterClockwise);
    assert.strictEqual(circle.closedCurveOrientation(V(0, 0, -1)), O.Clockwise);
    const line = new r.LineCurve(P(0, 0, 0), P(10, 0, 0));
    assert.strictEqual(line.closedCurveOrientation(), O.Undefined);        // the switch's default
    const piece = line.trim(2.0, 5.0);
    assert(piece instanceof r.Curve);
    assert.deepStrictEqual(piece.pointAtStart, P(2, 0, 0));
    assert.deepStrictEqual(piece.pointAtEnd, P(5, 0, 0));
    assert.deepStrictEqual(line.trim(I(1.0, 3.0)).pointAtEnd, P(3, 0, 0));
    assert.strictEqual(circle.isCircle(), true);
    assert.strictEqual(line.isCircle(), false);
    const [ok, c] = circle.tryGetCircle();
    assert(ok === true && Math.abs(c.Radius - 2.0) < 1e-12);
    assert.strictEqual(line.tryGetCircle()[0], false);
    const d = line.derivativeAt(4.0, 1, r.CurveEvaluationSide.Default);
    assert.strictEqual(d.length, 2);
    assert.deepStrictEqual(d[0], V(4, 0, 0));
    assert.deepStrictEqual(d[1], V(1, 0, 0));
    ctx.raises(() => line.derivativeAt(4.0, -1, r.CurveEvaluationSide.Default), /runtime_error,The derivativeCount must be larger than or equal to zero/);
  });

  t('test_hidden_helpers_child_pointers_and_class_usings', () => {
    const m = new r.Mesh();
    const v = m.vertices;
    v.add(P(0, 0, 0)); v.add(1.0, 0.0, 0.0); v.add(r.Point3f.create(0, 1, 0));
    assert.strictEqual(v.count, 3);
    assert.strictEqual(v.toFloatArray().length, 9);
    const nc = r.NurbsCurve.createFromLine(r.Line.create(P(0, 0, 0), P(3, 0, 0)));
    assert(nc instanceof r.NurbsCurve);
    assert.deepStrictEqual(nc.pointAtEnd, P(3, 0, 0));
    assert(r.NurbsCurve.createFromLine(r.Line.create(P(1, 1, 1), P(1, 1, 1))) == null);   // invalid line
    const box = r.Box.create(r.Plane.worldXY, I(0, 1), I(0, 2), I(0, 3));
    const ext = r.Extrusion.createBoxExtrusion(box, true);
    assert(ext instanceof r.Extrusion);
    assert.strictEqual(ext.isSolid, true);
    assert.strictEqual(ext.capCount, 2);
    const ns = r.NurbsSurface.create(3, false, 2, 2, 2, 2);
    ns.points.setPoint(1, 1, 1.0, 1.0, 0.0);
    const cp = ns.points.getControlPoint(1, 1);
    assert.deepStrictEqual(r.ControlPoint.location(cp), P(1, 1, 0));
    assert.strictEqual(r.ControlPoint.weight(cp), 1.0);
  });

  t('test_g86_singles', () => {
    const f = new r.File3dm();
    const iid = f.objects.addInstanceObject(0, r.Transform.identity, null);
    assert(isUuid(iid), String(iid));
    const fr = r.FileReference.createFromFullPath('/tmp/a.png');
    assert(fr.fullPath.endsWith('a.png'));
    const a = new r.LineCurve(P(0, 0, 0), P(1, 0, 0));
    const b = new r.LineCurve(P(0, 1, 0), P(1, 1, 0));
    assert(r.NurbsSurface.createRuledSurface(a, b) instanceof r.NurbsSurface);
    const tm = r.TextureMapping.createPlaneMapping(r.Plane.worldXY, I(0, 1), I(0, 1), I(0, 1));
    assert.strictEqual(typeof tm.hasId, 'boolean');                         // ModelComponent's, inherited
    // (checked before the point-sequence statics below, which are unbound in JS)
    const pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)];
    const nc = r.NurbsCurve.create(false, 3, pts);
    assert(nc instanceof r.NurbsCurve);
    assert.deepStrictEqual(nc.pointAtEnd, P(3, 1, 0));
    assert(r.NurbsCurve.create(false, 3, [P(0, 0, 0)]) == null);          // count < 2
    const kind = c => c && c.constructor.name;
    let c = r.Curve.createControlPointCurve(pts.slice(0, 2), 3);
    assert(c instanceof r.LineCurve, 'two points: ' + kind(c));
    c = r.Curve.createControlPointCurve(pts, 1);
    assert(c instanceof r.PolylineCurve, 'degree 1: ' + kind(c));
    c = r.Curve.createControlPointCurve(pts, 3);
    assert(c instanceof r.NurbsCurve, 'degree 3: ' + kind(c));
  });

  t('test_g86_outs_rectangles_and_byte_buffers', () => {
    const crv = new r.PolylineCurve([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)]);
    let [ok, pl] = crv.tryGetPolyline();
    assert(ok === true && pl instanceof r.Polyline && pl.count === 3);
    const arc = new r.ArcCurve(r.Circle.create(P(0, 0, 0), 1.0));
    [ok, pl] = arc.tryGetPolyline();
    assert(ok === false && pl == null, String([ok, pl]));
    const vp = new r.ViewportInfo();
    vp.setScreenPort(0, 640, 480, 0, 0, 0);
    const port = vp.getScreenPort();
    // Python accepts (x, y, w, h) or ((x, y, w, h), ...) -- the same shape check
    const rect = Array.isArray(port[0]) ? port[0] : (typeof port[0] === 'object' && port[0] !== null && 'X' in port[0]
      ? [port[0].X, port[0].Y, port[0].Width, port[0].Height] : port);
    assert.deepStrictEqual(Array.from(rect).slice(0, 4), [0, 0, 640, 480], JSON.stringify(port));
    const f = new r.File3dm();
    f.objects.addPoint(P(1, 2, 3), null);
    const path = '/tmp/part3_b.3dm';
    assert.strictEqual(f.write(path, 8), true);
    assert.strictEqual(r.File3dm.read(path).objects.count, 1);
    // the same one-point model as bytes
    const data = f.toByteArray();
    const g = r.File3dm.fromByteArray(data);
    assert(g instanceof r.File3dm && g.objects.count === 1);
    assert.strictEqual(r.File3dm.fromByteArray(new Uint8Array(data)).objects.count, 1);
  });

  t('test_file3dm_render_content_collections', () => {
    const f = ctx.readModel('file3dm_stuff.3dm');
    // the old binding's RenderContent (id, kind, name), frozen at the switch-over (M3)
    const oldRc = [['16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'],
                   ['660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'],
                   ['ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal']];
    const key = c => [String(c.id), c.kind];
    const cmp = (a, b) => (a[0] + a[1] < b[0] + b[1] ? -1 : a[0] + a[1] > b[0] + b[1] ? 1 : 0);
    const oldc = oldRc.map(([id, kind]) => [id, kind]).sort(cmp);
    const mats = ctx.list(f.renderMaterials), envs = ctx.list(f.renderEnvironments), texs = ctx.list(f.renderTextures);
    assert(mats.every(m => m instanceof r.File3dmRenderMaterial));
    assert(envs.every(e => e instanceof r.File3dmRenderEnvironment));
    assert(texs.every(x => x instanceof r.File3dmRenderTexture));
    const gen = [...mats, ...envs, ...texs].map(key).sort(cmp);
    const top = oldc.filter(k => gen.some(g => g[0] === k[0]));
    assert.deepStrictEqual(gen, top);
    assert(gen.length >= 1);
    assert(mats.every(m => m.kind === 'material') && envs.every(e => e.kind === 'environment'));
    for (const m of mats) {
      assert(m.toMaterial() instanceof r.Material && m.isTopLevel === true);
      assert.strictEqual(m.name, oldRc.find(([id]) => id === String(m.id))[2]);
    }
  });

  t('test_annotation_parent_dimension_style_is_modeled', () => {
    const f = ctx.readModel('textEntities_r8.3dm');
    const ann = ctx.list(f.objects).map(o => o.geometry).find(g => g instanceof r.AnnotationBase);
    assert(ann, 'no annotation in textEntities_r8.3dm');
    assert(ann.parentDimensionStyle == null);
    const ds = ctx.list(f.allDimStyles)[0];
    ann.parentDimensionStyle = ds;
    const p = ann.parentDimensionStyle;
    assert(p != null && p.id === ds.id && ann.dimensionStyleId === ds.id);
    // Python: `is p` -- the stored object every time; embind hands out a new handle per read,
    // so the identity is the C++ object's (isAliasOf)
    const again = ann.parentDimensionStyle;
    assert(again.isAliasOf(p), 'parentDimensionStyle is a different object on each read');
    if (!ann.hasPropertyOverrides) approx(ann.textHeight, ds.textHeight, 1e-12, 'the parent\'s value');
    assert.strictEqual(ann.setBold(true), true);
    assert.strictEqual(ann.isAllBold(), true);
    ann.parentDimensionStyle = null;
    assert(ann.parentDimensionStyle == null);
    assert.strictEqual(ann.dimensionStyleId, ZERO_ID);
    assert.strictEqual(p.id, ds.id);                                         // a handed-out wrapper stays valid
  });

  t('test_translated_list_indexers_g101', () => {
    // class-mode indexers (G101): get / set / count, and for...of through the post-js iterator
    const m = quadMesh();
    const vs = m.vertices;
    assert.strictEqual(vs.count, 4);
    assert.deepStrictEqual([...vs].map(p => [p.X, p.Y]), [[0, 0], [1, 0], [1, 1], [0, 1]]);
    vs.set(2, { X: 5, Y: 6, Z: 7 });
    assert.deepStrictEqual([vs.get(2).X, vs.get(2).Z], [5, 7]);
    ctx.raises(() => vs.get(4), /Index was outside the bounds of the array/);
    assert.deepStrictEqual([...m.faces].map(f => f.D), [3]);
    const nc = r.NurbsCurve.create(false, 3, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]);
    const cps = nc.points;
    assert.strictEqual(cps.count, 4);
    assert.deepStrictEqual([...cps].map(c => c.X), [0, 1, 2, 3]);
    ctx.raises(() => cps.get(-1), /greater than or equal to zero/);
  });

  t('test_draco_compression_g107', () => {
    const D = r.DracoCompression;
    const m = quadMesh();
    const d = D.compress(m);
    const b = d.toByteArray();
    assert(b instanceof Uint8Array && b.length > 0);
    const g = D.decompressByteArray(b);
    assert(g instanceof r.Mesh && g.vertices.count === 4);
    assert.strictEqual(D.decompressBase64String(d.toBase64String()).vertices.count, 4);
    const o = new r.DracoCompressionOptions();
    assert.deepStrictEqual([o.compressionLevel, o.positionQuantizationBits, o.includeNormals], [7, 14, true]);
    o.includeNormals = false;
    o.vertexColorFormat = r.DracoColorFormat.RGBA;
    assert.strictEqual(D.compress(m, o).normalAttributePosition, -1);
    const pc = new r.PointCloud();
    for (let i = 0; i < 5; i++) pc.add(P(i, 2 * i, 0));
    const gp = D.decompressByteArray(D.compress(pc).toByteArray());
    assert(gp instanceof r.PointCloud && gp.count === 5);
    // the module's in-memory file system (no host FS in WASM)
    assert.strictEqual(d.write('/tmp/rh3dm_quad.drc'), true);
    assert.strictEqual(D.decompressFile('/tmp/rh3dm_quad.drc').vertices.count, 4);
    ctx.raises(() => D.decompressFile('/tmp/rh3dm_missing.drc'), /FileNotFound|File/);
    ctx.raises(() => D.compress({}), /no overload matches/);
    // (the old binding decoded the generated bytes too: one Draco, one bitstream)
  });

  t('test_color_list_and_surface_point_grid_iterate_g102', () => {
    const m = quadMesh();
    const vc = m.vertexColors;
    for (let i = 0; i < 4; i++) vc.add({ r: 10 * i, g: 20, b: 30, a: 255 });
    assert.strictEqual(vc.count, 4);
    assert.deepStrictEqual(vc.get(2), { r: 20, g: 20, b: 30, a: 255 });
    vc.set(1, { r: 1, g: 2, b: 3, a: 128 });
    assert.deepStrictEqual(vc.get(1), { r: 1, g: 2, b: 3, a: 128 });
    assert.deepStrictEqual([...vc].map(c => c.r), [0, 1, 20, 30]);
    ctx.raises(() => vc.get(4), /out_of_range/);
    const ns = r.NurbsSurface.create(3, false, 2, 2, 3, 2);
    const pts = ns.points;
    pts.setPoint(2, 1, 5, 6, 7);
    const cps = [...pts];
    assert.strictEqual(cps.length, 6);
    assert.deepStrictEqual([cps[2 * pts.countV + 1].X, cps[2 * pts.countV + 1].Z], [5, 7]);
  });
};
