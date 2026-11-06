const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

const MESHING_PARAMETERS_8X_JSON = { TextureRange: 2, JaggedSeams: false, RefineGrid: true, SimplePlanes: false,
  ComputeCurvature: false, ClosedObjectPostProcess: false, GridMinCount: 0, GridMaxCount: 0, GridAngle: 0.3490658503988659,
  GridAspectRatio: 6, GridAmplification: 1, Tolerance: 0, MinimumTolerance: 0, RelativeTolerance: 0,
  MinimumEdgeLength: 0.0001, MaximumEdgeLength: 0, RefineAngle: 0.3490658503988659 }

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('meshingParametersPresetsAreMeshingParameters', () => {
  // the static presets (fastRenderMesh, qualityRenderMesh, defaultAnalysisMesh) are MeshingParameters
  expect(rhino.MeshingParameters.fastRenderMesh).toBeInstanceOf(rhino.MeshingParameters)
  expect(rhino.MeshingParameters.qualityRenderMesh).toBeInstanceOf(rhino.MeshingParameters)
  expect(typeof rhino.MeshingParameters.defaultAnalysisMesh.relativeTolerance).toBe('number')
})

test('meshingParametersPresetsHaveRhinoCommonsValues', () => {
  // MeshingParameters constructs, and the default, minimal and smooth presets carry RhinoCommon's settings
  const MP = rhino.MeshingParameters
  expect(new MP()).toBeInstanceOf(MP)
  expect(MP.default).toBeInstanceOf(MP)
  const minimal = MP.minimal
  expect(minimal.gridMinCount).toBe(16)
  expect(minimal.jaggedSeams).toBe(true)
  expect(minimal.simplePlanes).toBe(false)
  expect(MP.smooth.simplePlanes).toBe(true)
})

test('meshingParametersEncodeIsThe8xJsonForm', () => {
  // new MeshingParameters().encode() is the 8.x JSON object, key for key
  expect(new rhino.MeshingParameters().encode()).toEqual(MESHING_PARAMETERS_8X_JSON)
})

test('meshingParametersEncodeDecodeRoundTrip', () => {
  // encode writes the edited settings, and decode reads every key back
  const mp = new rhino.MeshingParameters()
  mp.jaggedSeams = true; mp.refineGrid = false; mp.gridMinCount = 17; mp.gridMaxCount = 300
  mp.tolerance = 0.25; mp.minimumEdgeLength = 0.01; mp.maximumEdgeLength = 12.5; mp.refineAngle = 0.3
  const d = mp.encode()
  expect([d.JaggedSeams, d.RefineGrid, d.GridMinCount, d.GridMaxCount]).toEqual([true, false, 17, 300])
  expect([d.Tolerance, d.MinimumEdgeLength, d.MaximumEdgeLength, d.RefineAngle]).toEqual([0.25, 0.01, 12.5, 0.3])
  expect(Object.keys(d).sort()).toEqual(Object.keys(MESHING_PARAMETERS_8X_JSON).sort())
  const back = rhino.MeshingParameters.decode(d)
  expect(back).toBeInstanceOf(rhino.MeshingParameters)
  expect(back.encode()).toEqual(d)
})

test('meshingParametersToJsonIsItsEncode', () => {
  // JSON.stringify of MeshingParameters writes its encode(), the 8.x JSON object
  const mp = new rhino.MeshingParameters()
  const J = v => JSON.parse(JSON.stringify(v))
  expect(J(mp)).toEqual(J(mp.encode()))
  expect(J(mp)).toEqual(MESHING_PARAMETERS_8X_JSON)
})

test('meshingParametersConstructFromDensity', () => {
  // new MeshingParameters(density[, minimumEdgeLength]) sets the minimum edge length when given
  expect(new rhino.MeshingParameters(0.5, 0.25).minimumEdgeLength).toBe(0.25)
  expect(typeof new rhino.MeshingParameters(0.5).minimumEdgeLength).toBe('number')
})

test('meshingParametersCopyConstructorCopies', () => {
  // new MeshingParameters(other) is an independent copy
  const mp2 = new rhino.MeshingParameters(0.5, 0.25)
  const mp3 = new rhino.MeshingParameters(mp2)
  mp2.minimumEdgeLength = 1.0
  expect(mp3.minimumEdgeLength).toBe(0.25)
})
