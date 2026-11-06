const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('enumArgumentsAreTypeChecked', () => {
  // an enum parameter accepts the enum value and throws a TypeError for a number, string or object (D55)
  const x = helpers.curve(rhino)
  expect(x.isContinuous(rhino.Continuity.C2_continuous, 0.5)).toBe(true)
  helpers.raises(rhino, () => x.isContinuous(3, 0.5), /expected a .*Continuity value, got number/)
  helpers.raises(rhino, () => x.isContinuous('C1', 0.5), /got string/)
  helpers.raises(rhino, () => x.isContinuous({}, 0.5), /got object/)
})

test('enumPropertySettersAreTypeChecked', () => {
  // an enum property setter accepts the enum value and throws for a number (D55)
  const ds = new rhino.DimensionStyle()
  ds.arrowType1 = rhino.DimensionStyleArrowType.Dot
  helpers.raises(rhino, () => { ds.arrowType1 = 3 }, /got number/)
  expect(ds.arrowType1).toBe(rhino.DimensionStyleArrowType.Dot)
})

test('abstractBasesAreNotConstructible', () => {
  // abstract RhinoCommon bases (GeometryBase, ModelComponent) have no constructor
  expect(rhino.GeometryBase.prototype.constructor).toBe(rhino.GeometryBase)
  helpers.raises(rhino, () => new rhino.GeometryBase(), /GeometryBase has no accessible constructor/)
  helpers.raises(rhino, () => new rhino.ModelComponent(), /ModelComponent has no accessible constructor/)
})

test('subclassesReachBaseClassMembers', () => {
  // a derived class is an instance of its bases and reaches their members
  const pc = new rhino.PolyCurve()
  pc.append(new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(2, 0, 0)))
  expect(pc).toBeInstanceOf(rhino.Curve)
  expect(pc).toBeInstanceOf(rhino.GeometryBase)
  expect(pc.pointAt(1.0)).toEqual({ X: 1, Y: 0, Z: 0 })      // declared on Curve
})

test('readOnlyPropertiesHaveNoSetter', () => {
  // a property that is read-only in RhinoCommon throws when assigned
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(2, 0, 0))
  let desc
  for (let p = rhino.Curve.prototype; p && !desc; p = Object.getPrototypeOf(p)) desc = Object.getOwnPropertyDescriptor(p, 'pointAtStart')
  expect(desc).toBeDefined()
  // embind gives a read-only property a setter that only throws "is a read-only property"
  expect(desc.set === undefined || /read-only/.test(String(desc.set))).toBe(true)
  helpers.raises(rhino, () => { c.pointAtStart = new rhino.Point3d(1, 1, 1) }, /pointAtStart is a read-only property/)
})

test('classStaticPropertiesAreProperties', () => {
  // a class's static properties are JS static getters, not methods, as on the value types
  expect(rhino.MeshingParameters.default).toBeInstanceOf(rhino.MeshingParameters)
  expect(rhino.Material.maxShine).toBe(255.0)
  expect(typeof rhino.ModelComponent.namePathSeparator).toBe('string')
  expect(rhino.ObjectAttributes.oCSMappingChannelId).not.toBeUndefined()
  expect(rhino.SubD.empty).toBeInstanceOf(rhino.SubD)
  expect(typeof rhino.Point3d.unset).toBe('object')
  expect(rhino.Point3d.unset.X).toBeLessThan(-1e300)
  expect(typeof rhino.Plane.worldXY).toBe('object')
  expect(rhino.Plane.worldXY.ZAxis.Z).toBe(1)
})

test('postScriptResolvedEveryName', () => {
  // the module's post-script found every name it patches
  expect(rhino.rh3dmPostUnresolved).toEqual([])
})

test('valueTypesEncodeToThe8xJsonForm', () => {
  // a value type's encode writes the 8.x JSON object, key for key
  expect(rhino.Point3d.encode(new rhino.Point3d(1, 2, 3))).toEqual({ X: 1, Y: 2, Z: 3 })
  expect(rhino.Point4d.encode(new rhino.Point4d(1, 2, 3, 4))).toEqual({ X: 1, Y: 2, Z: 3, W: 4 })
  expect(rhino.Vector2d.encode(new rhino.Vector2d(1, 2))).toEqual({ X: 1, Y: 2 })
  expect(rhino.Sphere.encode(new rhino.Sphere(new rhino.Point3d(1, 2, 3), 4))).toEqual({ Radius: 4,
    EquatorialPlane: { Origin: { X: 1, Y: 2, Z: 3 }, XAxis: { X: 1, Y: 0, Z: 0 }, YAxis: { X: 0, Y: 1, Z: 0 }, ZAxis: { X: 0, Y: 0, Z: 1 } } })
  expect(Object.keys(rhino.Circle.encode(new rhino.Circle(2.5)))).toEqual(['radius', 'plane'])
})

test('valueTypesStringifyToThe8xJsonForm', () => {
  // a value type is a plain object: JSON.stringify writes 8.x's toJSON() form
  const J = v => JSON.parse(JSON.stringify(v))
  expect(J(new rhino.BoundingBox(new rhino.Point3d(0, 1, 2), new rhino.Point3d(3, 4, 5))))
    .toEqual({ Min: { X: 0, Y: 1, Z: 2 }, Max: { X: 3, Y: 4, Z: 5 } })
  expect(J(new rhino.Sphere(new rhino.Point3d(1, 2, 3), 4))).toEqual({ Radius: 4,
    EquatorialPlane: { Origin: { X: 1, Y: 2, Z: 3 }, XAxis: { X: 1, Y: 0, Z: 0 }, YAxis: { X: 0, Y: 1, Z: 0 }, ZAxis: { X: 0, Y: 0, Z: 1 } } })
})

test('strOfAValueIsRhinoCommonsToString', () => {
  // a value type's ToString is RhinoCommon's (on the class: a constructor's own toString stays untouched) (D75)
  const P = (X, Y, Z) => ({ X, Y, Z })
  expect(rhino.Point3d.ToString(P(1, 2, 3))).toBe('1,2,3')
  expect(rhino.Vector2d.ToString({ X: 4, Y: 5 })).toBe('4,5')
  expect(rhino.Line.ToString({ From: P(0, 0, 0), To: P(1, 2, 3) })).toBe('0,0,0,1,2,3')
  expect(rhino.Transform.ToString(rhino.Transform.identity)).toBe('R0=(1,0,0,0), R1=(0,1,0,0), R2=(0,0,1,0), R3=(0,0,0,1)')
  expect(typeof rhino.Point3d.toString()).toBe('string')
})

test('strUsesDotNetNumberForms', () => {
  // ToString prints numbers as .NET does: shortest round-trip, scientific from 1E+17 and below 1E-04, -0, NaN, Infinity (D75)
  expect(rhino.Point3d.ToString({ X: 0.1 + 0.2, Y: 1e15, Z: 1e-5 })).toBe('0.30000000000000004,1000000000000000,1E-05')
  expect(rhino.Point3d.ToString({ X: 1e16, Y: 1e17, Z: -1.5e17 })).toBe('10000000000000000,1E+17,-1.5E+17')
  expect(rhino.Point3d.ToString({ X: -0, Y: NaN, Z: Infinity })).toBe('-0,NaN,Infinity')
})

test('valueTypesConstructWithNew', () => {
  // `new T(...)` returns the same plain object as T.create(...); the statics stay on the type (D92)
  expect(new rhino.Point3d(1, 2, 3)).toEqual({ X: 1, Y: 2, Z: 3 })
  expect(new rhino.Point3d()).toEqual(rhino.Point3d.create())
  expect(new rhino.Interval(1, 4)).toEqual({ T0: 1, T1: 4 })
  const c = new rhino.Circle(rhino.Plane.worldXY, 2)
  expect(c).toEqual(rhino.Circle.create(rhino.Plane.worldXY, 2))
  expect(rhino.Circle.circumference(c)).toBe(4 * Math.PI)
  expect(new rhino.BoundingBox({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }).Max).toEqual({ X: 1, Y: 2, Z: 3 })
  expect(rhino.Transform.isIdentity(new rhino.Transform(1))).toBe(true)
  expect(rhino.Plane.isValid(rhino.Plane.worldXY)).toBe(true)
  expect(rhino.Circle.name).toBe('Circle')
  helpers.raises(rhino, () => new rhino.Point3d(1, 2), /./)
})

test('overloadDispatchPointCloudAddTellsANormalFromAColor', () => {
  // PointCloud.add / insert pick the overload by argument shape: a vector is a normal, an {r, g, b, a} a color
  const a = new rhino.PointCloud()
  a.add(new rhino.Point3d(0, 0, 0), new rhino.Vector3d(0, 0, 1))              // (point, normal)
  expect(a.count).toBe(1)
  expect(a.containsNormals).toBe(true)
  expect(a.containsColors).toBe(false)
  const b = new rhino.PointCloud()
  b.add(new rhino.Point3d(0, 0, 0), { r: 255, g: 0, b: 0, a: 0 })              // (point, color)
  expect(b.containsColors).toBe(true)
  expect(b.containsNormals).toBe(false)
  a.insert(0, new rhino.Point3d(1, 1, 1), new rhino.Vector3d(1, 0, 0))        // (index, point, normal)
  expect(a.count).toBe(2)
  expect(a.pointAt(0).X).toBeCloseTo(1, 9)
})

test('overloadDispatchSetObjectFrameTakesAPlaneOrATransform', () => {
  // ObjectAttributes.setObjectFrame reaches both its Plane and its Transform overload
  const oa = new rhino.ObjectAttributes()
  expect(() => oa.setObjectFrame(rhino.Plane.worldXY)).not.toThrow()
  expect(() => oa.setObjectFrame(rhino.Transform.identity)).not.toThrow()
})

test('overloadDispatchBoxContainsTellsABoxFromAPoint', () => {
  // Box.contains takes the Box overload for a box (it has interval axes) and the point overload for a point
  const iv = (a, b) => new rhino.Interval(a, b)
  const big = new rhino.Box(rhino.Plane.worldXY, iv(0, 10), iv(0, 10), iv(0, 10))
  const small = new rhino.Box(rhino.Plane.worldXY, iv(1, 2), iv(1, 2), iv(1, 2))
  expect(rhino.Box.contains(big, small)).toBe(true)
  expect(rhino.Box.contains(big, new rhino.Point3d(5, 5, 5))).toBe(true)
  expect(rhino.Box.contains(big, new rhino.Point3d(50, 5, 5))).toBe(false)
})

test('overloadDispatchNullReachesTheObjectOverload', () => {
  // null goes to the overload taking a class object, as in .NET: PolyCurve.append(null) returns false (D10)
  const pc = new rhino.PolyCurve()
  expect(pc.append(null)).toBe(false)
})
