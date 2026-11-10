// Node, CommonJS: require('rhino3dm') resolves to rhino3dm.js
const fs = require('fs')
const path = require('path')
const rhino3dm = require('rhino3dm')
rhino3dm().then(rhino => {
  const sphere = new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5)
  if (rhino.Sphere.diameter(sphere) !== 10) { console.error('FAIL: require'); process.exit(1) }
  console.log('require OK:', require.resolve('rhino3dm'))
})

// The memory64 variant, require('rhino3dm/wasm64/rhino3dm.js'), when the package has one.
// memory64 needs Node 24+.
const wasm64 = path.join(path.dirname(require.resolve('rhino3dm')), 'wasm64', 'rhino3dm.js')
if (fs.existsSync(wasm64)) {
  if (Number(process.versions.node.split('.')[0]) < 24) {
    console.log('wasm64 skipped: needs Node 24+, this is', process.version)
  } else {
    require('rhino3dm/wasm64/rhino3dm.js')().then(rhino => {
      const sphere = new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5)
      if (rhino.Sphere.diameter(sphere) !== 10) { console.error('FAIL: require wasm64'); process.exit(1) }
      console.log('require wasm64 OK:', require.resolve('rhino3dm/wasm64/rhino3dm.js'))
    })
  }
}
