"""Performance: hand-written bindings vs the vNext generated bindings.

Runs against the built python module; prints a markdown table.

    python3 perf_compare.py <module_dir> [out.md]

Each micro-benchmark does the SAME RhinoCommon operation through both
surfaces and reports the best of several repeats (ns per call), so the
numbers isolate binding overhead + implementation, not interpreter noise.
The bulk rows compare per-point object access against the generated
zero-copy numpy view.
"""

import sys
import time


def best(fn, n, repeats=5):
    out = []
    for _ in range(repeats):
        t = time.perf_counter()
        fn(n)
        out.append(time.perf_counter() - t)
    return min(out) / n * 1e9          # ns per operation


def main(argv):
    sys.path.insert(0, argv[1])
    import _rhino3dm as r
    out_path = argv[2] if len(argv) > 2 else None

    rows = []

    def row(name, hand_fn, gen_fn, n, note=''):
        h = best(hand_fn, n)
        g = best(gen_fn, n)
        rows.append((name, h, g, note))

    # --- value types
    P, PX, VX = r.Point3d, r.Point3dX, r.Vector3dX
    p, q = P(1, 2, 3), P(4, 6, 3)
    px, qx = PX(1, 2, 3), PX(4, 6, 3)

    def h_ctor(n):
        for _ in range(n): P(1.0, 2.0, 3.0)
    def g_ctor(n):
        for _ in range(n): PX(1.0, 2.0, 3.0)
    row('Point3d(x, y, z)', h_ctor, g_ctor, 200_000)

    def h_dist(n):
        for _ in range(n): p.DistanceTo(q)
    def g_dist(n):
        for _ in range(n): px.DistanceTo(qx)
    row('Point3d.DistanceTo', h_dist, g_dist, 200_000,
        'generated also checks IsValid (RhinoCommon semantics)')

    def h_add(n):
        for _ in range(n): p + q
    def g_add(n):
        for _ in range(n): px + qx
    row('Point3d + Point3d', h_add, g_add, 200_000)

    def h_x(n):
        for _ in range(n): p.X
    def g_x(n):
        for _ in range(n): px.X
    row('Point3d.X (get)', h_x, g_x, 500_000)

    # --- a class member through the C layer vs the hand C++ call
    pts = r.Point3dList(0)
    for xyz in [(0, 0, 0), (1, 2, 0), (4, 1, 0), (6, -2, 0)]:
        pts.Add(*xyz)
    c = r.Curve.CreateControlPointCurve(pts, 3)
    cx = r.CurveX(c)
    t = (c.Domain.T0 + c.Domain.T1) / 2

    def h_pa(n):
        for _ in range(n): c.PointAt(t)
    def g_pa(n):
        for _ in range(n): cx.PointAt(t)
    row('Curve.PointAt', h_pa, g_pa, 200_000, 'generated goes through the flat C function')

    # --- bulk: a million points
    try:
        import numpy as np
    except ImportError:
        np = None
    bulk = None
    if np is not None:
        N = 1_000_000
        pc = r.PointCloud()
        for i in range(N):
            pc.Add(r.Point3d(i, 2.0 * i, 3.0))
        pcx = r.PointCloudX(pc)

        def h_bulk(_):
            s = 0.0
            for i in range(N):
                s += pc[i].Location.X
            return s

        def g_bulk(_):
            return np.asarray(pcx)[:, 0].sum()

        assert h_bulk(0) == g_bulk(0) == sum(range(N))
        a = np.asarray(pcx)
        assert a.shape == (N, 3) and not a.flags.owndata     # zero-copy view
        hb = best(h_bulk, 1, repeats=3) / 1e6                 # ms per full pass
        gb = best(g_bulk, 1, repeats=5) / 1e6
        bulk = (N, hb, gb)

    lines = ['# Performance: hand-written vs generated bindings', '',
             'tools/generate/perf_compare.py, Linux arm64 docker, CPython; best of',
             'several repeats. Lower is better; **bold** marks the faster side.', '',
             '| Operation | Hand (ns/call) | Generated (ns/call) | Ratio gen/hand | Note |',
             '|---|---|---|---|---|']
    for name, h, g, note in rows:
        hs, gs = '%.0f' % h, '%.0f' % g
        if h < g * 0.95:
            hs = '**%s**' % hs
        elif g < h * 0.95:
            gs = '**%s**' % gs
        lines.append('| %s | %s | %s | %.2fx | %s |' % (name, hs, gs, g / h, note))
    if bulk:
        N, hb, gb = bulk
        lines += ['', '| Bulk: sum X of %s points | Hand (ms) | Generated (ms) | Speedup |' % format(N, ','),
                  '|---|---|---|---|',
                  '| per-point objects vs zero-copy numpy view | %.1f | **%.2f** | %.0fx |'
                  % (hb, gb, hb / gb)]
    text = '\n'.join(lines) + '\n'
    print(text)
    if out_path:
        open(out_path, 'w').write(text)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
