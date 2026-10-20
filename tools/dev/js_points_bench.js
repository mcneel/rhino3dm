// usage: node js_points_bench.js <path to rhino3dm.js>
// JS: points as embind value_array (hand: [x, y, z]) vs value_object
// (generated: {X, Y, Z}) -- same curve, same C++ evaluation, only the
// marshalling of the returned point differs.
const factory = require(require('path').resolve(process.argv[2]));
factory().then(rhino => {
  const hand = new rhino.LineCurve([0, 0, 0], [10, 0, 0]);
  const gen = new rhino.CurveX(hand);
  const N = 200000;
  function best(fn) {
    let b = Infinity;
    for (let r = 0; r < 5; r++) { const t = process.hrtime.bigint(); fn(); b = Math.min(b, Number(process.hrtime.bigint() - t)); }
    return b / N;
  }
  let s = 0;
  const h = best(() => { for (let i = 0; i < N; i++) { const p = hand.pointAt(i / N * 10); s += p[0]; } });
  const g = best(() => { for (let i = 0; i < N; i++) { const p = gen.pointAt(i / N * 10); s += p.X; } });
  // pure marshalling cost: value type through a trivial generated function
  const P = rhino.Point3dX;
  const q = { X: 1, Y: 2, Z: 3 };
  const o = best(() => { for (let i = 0; i < N; i++) { s += P.add(q, q).X; } });
  console.log('| pointAt returning [x,y,z] (hand, value_array)  | %s ns |', h.toFixed(0));
  console.log('| pointAt returning {X,Y,Z} (generated, value_object) | %s ns | ratio %sx |', g.toFixed(0), (g / h).toFixed(2));
  console.log('| Point3dX.add({X,Y,Z}, {X,Y,Z}) round trip | %s ns |', o.toFixed(0));
  if (s === 42) console.log('');
});
