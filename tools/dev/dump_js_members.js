// The built JS module's surface, per registered class (prototype members and statics), for
// tools/generate/old_coverage.py: members registered only in JS are invisible to the Python
// introspection it otherwise uses. usage: node dump_js_members.js <rhino3dm.js> <out.json>
const factory = require(require('path').resolve(process.argv[2]));
const SKIP = new Set(['length', 'name', 'prototype', 'arguments', 'caller', 'argCount', 'constructor']);
factory().then(r => {
  const out = {};
  for (const k of Object.keys(r)) {
    const v = r[k];
    if (typeof v !== 'function' || !v.prototype) continue;
    const names = new Set();
    for (const n of Object.getOwnPropertyNames(v.prototype)) if (!SKIP.has(n)) names.add(n);
    for (const n of Object.getOwnPropertyNames(v)) if (!SKIP.has(n)) names.add(n);
    out[k] = [...names].sort();
    // the registered base (embind's prototype chain): inherited members count (retired.toml check)
    const bp = Object.getPrototypeOf(v.prototype);
    const bn = bp && bp.constructor && bp.constructor.name;
    if (bn && bn !== 'Object' && bn !== 'ClassHandle' && r[bn]) (out['$bases'] = out['$bases'] || {})[k] = bn;
  }
  require('fs').writeFileSync(process.argv[3], JSON.stringify(out, null, 0));
  console.log('JS MEMBERS: ' + Object.keys(out).length + ' classes');
});
