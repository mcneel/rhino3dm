#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initBrepBindings(pybind11::module& m);
#else
void initBrepBindings(void* m);
#endif

class BND_BrepFaceList
{
  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;
public:
  BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_F.Count(); }
  class BND_BrepFace* GetFace(int i);
};

class BND_Brep : public BND_GeometryBase
{
  ON_Brep* m_brep = nullptr;
public:
  BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref);

  //public static Brep TryConvertBrep(GeometryBase geometry)
  //public static Brep CreateFromMesh(Mesh mesh, bool trimmedTriangles)
  //public static Brep CreateFromBox(BoundingBox box)
  //public static Brep CreateFromBox(Box box)
  //public static Brep CreateFromBox(IEnumerable<Point3d> corners)
  //public static Brep CreateFromCylinder(Cylinder cylinder, bool capBottom, bool capTop)
  //public static Brep CreateFromSphere(Sphere sphere)
  //public static Brep CreateQuadSphere(Sphere sphere)
  //public static Brep CreateFromCone(Cone cone, bool capBottom)
  //public static Brep CreateFromRevSurface(RevSurface surface, bool capStart, bool capEnd)
  //public static Brep CreateFromSurface(Surface surface)
  //public static Brep CreateTrimmedPlane(Plane plane, Curve curve)
  //public static Brep CreateTrimmedPlane(Plane plane, IEnumerable<Curve> curves)
  BND_Brep();
  //public Collections.BrepVertexList Vertices
  //public Collections.BrepSurfaceList Surfaces
  //public Collections.BrepEdgeList Edges
  //public Collections.BrepTrimList Trims
  //public Collections.BrepLoopList Loops
  BND_BrepFaceList GetFaces();
  //public Collections.BrepCurveList Curves2D
  //public Collections.BrepCurveList Curves3D
  //public bool IsSolid
  //public BrepSolidOrientation SolidOrientation
  //public bool IsManifold
  //public bool IsSurface
  //public override GeometryBase Duplicate()
  //public Brep DuplicateSubBrep(IEnumerable<int> faceIndices)
  //public Curve[] DuplicateEdgeCurves()
  //public Curve[] DuplicateEdgeCurves(bool nakedOnly)
  //public Curve[] DuplicateNakedEdgeCurves(bool outer, bool inner)
  //public Point3d[] DuplicateVertices()
  //public void Flip()
  //public bool IsDuplicate(Brep other, double tolerance)
  //public bool IsValidTopology(out string log)
  //public bool IsValidGeometry(out string log)
  //public bool IsValidTolerancesAndFlags(out string log)
  //public int AddTrimCurve(Curve curve)
  //public int AddEdgeCurve(Curve curve)
  //public int AddSurface(Surface surface)
  //public void Append(Brep other)
  //public void SetVertices()
  //public void SetTrimIsoFlags()
  //public void SetTolerancesBoxesAndFlags()
  //public void SetTolerancesBoxesAndFlags(
  //  bool bLazy,
  //  bool bSetVertexTolerances,
  //  bool bSetEdgeTolerances,
  //  bool bSetTrimTolerances,
  //  bool bSetTrimIsoFlags,
  //  bool bSetTrimTypeFlags,
  //  bool bSetLoopTypeFlags,
  //  bool bSetTrimBoxes
  //)
  //public void Compact()
  //public bool CullUnusedFaces()
  //public bool CullUnusedLoops()
  //public bool CullUnusedTrims()
  //public bool CullUnusedEdges()
  //public bool CullUnusedVertices()
  //public bool CullUnused3dCurves()
  //public bool CullUnused2dCurves()
  //public bool CullUnusedSurfaces()
  //public void Standardize()


protected:
  void SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref);
};

class BND_BrepFace : public BND_SurfaceProxy
{
  ON_BrepFace* m_brepface = nullptr;
public:
  BND_BrepFace(ON_BrepFace* brepface, const ON_ModelComponentReference* compref);

  //Brep Brep = > m_brep;
  //bool OrientationIsReversed
  //bool IsSurface
  //int FaceIndex{ get; }
  //int SurfaceIndex
  //Collections.BrepLoopList Loops
  //BrepLoop OuterLoop
  //Brep CreateExtrusion(Curve pathCurve, bool cap)
  //bool ShrinkFace(ShrinkDisableSide disableSide)
  //override bool SetDomain(int direction, Interval domain)
  //Brep DuplicateFace(bool duplicateMeshes)
  //Surface DuplicateSurface()
  //Surface UnderlyingSurface()
  class BND_Mesh* GetMesh(ON::mesh_type mt);
  //bool SetMesh(MeshType meshType, Mesh mesh)
  //int[] AdjacentEdges()
  //int[] AdjacentFaces()

protected:
  void SetTrackedPointer(ON_BrepFace* brepface, const ON_ModelComponentReference* compref);
};
