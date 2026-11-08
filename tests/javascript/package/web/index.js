// a browser app as a bundler sees it: the "browser" export, rhino3dm.module.js
import rhino3dm from 'rhino3dm'
rhino3dm().then(rhino => console.log(rhino.Sphere.diameter(new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5))))
