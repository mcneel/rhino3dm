// Node, CommonJS: require('rhino3dm') resolves to rhino3dm.js
const rhino3dm = require('rhino3dm')
rhino3dm().then(rhino => {
  const sphere = new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5)
  if (rhino.Sphere.diameter(sphere) !== 10) { console.error('FAIL: require'); process.exit(1) }
  console.log('require OK:', require.resolve('rhino3dm'))
})
