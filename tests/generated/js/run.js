// JS regression tests for the vNext generated bindings -- the JS port of
// tests/generated/test_generated.py (same names, same contracts), run against the COMPILED
// module by the round's wasm phase.
//
//   node tests/generated/js/run.js <rhino3dm.js> [name filter]
//
// Each test_*.js file exports `(t, ctx) => { t('test_name', () => { ... }); }` (async allowed). A
// test may call ctx.skip(reason). C++ exceptions reach JS as catchable errors (-fwasm-exceptions,
// G101): ctx.raises(fn, /message/) is the port of `pytest.raises`. Lifetime: run node with
// --expose-gc; ctx.gcSettle() collects and lets FinalizationRegistry callbacks run -- the port of
// Python's `del x; gc.collect()` (dropping the last reference, not an explicit delete()).
const path = require('path');
const fs = require('fs');
const assert = require('assert');

const modPath = path.resolve(process.argv[2]);
const filter = process.argv[3] || '';
const factory = require(modPath);

class Skip extends Error {}

factory().then(async r => {
  const tests = [];
  const ctx = {
    r, assert,
    MODELS: path.join(__dirname, '..', '..', 'models'),
    // a .3dm from tests/models, through the generated reader (bytes: no host file system in WASM)
    readModel: name => r.File3dmX.fromByteArray(new Uint8Array(fs.readFileSync(path.join(__dirname, '..', '..', 'models', name)))),
    readOld: name => r.File3dm.fromByteArray(new Uint8Array(fs.readFileSync(path.join(__dirname, '..', '..', 'models', name)))),
    approx: (a, b, tol = 1e-9, msg) => assert(Math.abs(a - b) <= tol, (msg || '') + ` ${a} != ${b} (tol ${tol})`),
    // a property generated as a JS property, or (older shape) a zero-argument function
    val: v => (typeof v === 'function' ? v() : v),
    skip: why => { throw new Skip(why); },
    noThrowAssert: () => {},
    // `pytest.raises`: fn throws a C++ exception (or a JS error); its message matches `re`
    raises: (fn, re) => {
      let threw = false, msg = '';
      try { fn(); } catch (e) {
        threw = true;
        try { msg = r.getExceptionMessage ? String(r.getExceptionMessage(e)) : String(e); } catch (e2) { msg = String(e); }
        // a caught C++ exception is refcounted in wasm memory: release it (emscripten's contract)
        try { if (r.decrementExceptionRefcount && e instanceof WebAssembly.Exception) r.decrementExceptionRefcount(e); } catch (e3) { /* not a C++ exception */ }
      }
      assert(threw, 'expected a throw');
      if (re) assert(re.test(msg), 'message ' + msg + ' does not match ' + re);
    },
    gcSettle: async () => {
      if (typeof global.gc !== 'function') throw new Skip('node without --expose-gc');
      for (let i = 0; i < 3; i++) { global.gc(); await new Promise(res => setTimeout(res, 10)); }
    },
    // an iterable generated table / collection as an array (`iterator()` -> next())
    list: coll => {
      if (Array.isArray(coll)) return coll;
      const out = [];
      if (coll && typeof coll.iterator === 'function') {
        const it = coll.iterator();
        for (let n = it.next(); !n.done; n = it.next()) out.push(n.value);
        return out;
      }
      if (coll && typeof coll[Symbol.iterator] === 'function') return [...coll];
      throw new Error('not iterable');
    },
    // the old binding's curve the Python tests start from (curve())
    curve: () => {
      const pts = new r.Point3dList(0);
      for (const p of [[0, 0, 0], [1, 2, 0], [4, 1, 0], [6, -2, 0]]) pts.add(p[0], p[1], p[2]);
      return r.Curve.createControlPointCurve(pts, 3);
    },
  };
  const t = (name, fn) => tests.push({ name, fn });
  const files = fs.readdirSync(__dirname).filter(f => /^test_.*\.js$/.test(f)).sort();
  for (const f of files) require(path.join(__dirname, f))(t, ctx);

  let passed = 0, failed = 0, skipped = 0;
  const failures = [];
  // the post-script's facts all resolved (G101): views keep their parent, static getters, for...of
  tests.unshift({ name: 'test_js_post_script_resolved', fn: () => assert.deepStrictEqual(r.rh3dmPostUnresolved, []) });
  for (const { name, fn } of tests) {
    if (filter && !filter.split('|').some(f => name.includes(f))) continue;   // 'a|b': any of the names
    try { await fn(); passed++; }
    catch (e) {
      if (e instanceof Skip) { skipped++; console.log('SKIP ' + name + ': ' + e.message); }
      else { failed++; failures.push(name); console.log('FAIL ' + name + ': ' + (e && e.stack ? e.stack.split('\n').slice(0, 3).join(' | ') : String(e))); }
    }
  }
  console.log(`JS TESTS: ${passed} passed, ${failed} failed, ${skipped} skipped (${files.length} files)`);
  if (failed) { console.log('FAILED: ' + failures.join(', ')); process.exitCode = 1; }
});
