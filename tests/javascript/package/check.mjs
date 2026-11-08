// Node, ES modules: import rhino3dm from 'rhino3dm' resolves to rhino3dm.js too (the "node" export)
import rhino3dm from 'rhino3dm'
const rhino = await rhino3dm()
const sphere = new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5)
if (rhino.Sphere.diameter(sphere) !== 10) { console.error('FAIL: import'); process.exit(1) }
console.log('import OK:', import.meta.resolve('rhino3dm'))
