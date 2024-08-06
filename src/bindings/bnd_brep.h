#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initBrepBindings(rh3dmpymodule& m);
#else
void initBrepBindings(void* m);
#endif

class BND_BrepEdge : public BND_CurveProxy {
  ON_BrepEdge* m_edge;
public:
  BND_BrepEdge(ON_BrepEdge* edge, const ON_ModelComponentReference* compref);
};

class BND_BrepFaceList {
  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;

 public:
  BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_F.Count(); }
  class BND_BrepFace* GetFace(int i);
};

class BND_BrepVertex: public BND_Point {
  ON_BrepVertex* m_vertex;
public:
  BND_BrepVertex(ON_BrepVertex* vertex, const ON_ModelComponentReference* compref);
  //public Brep Brep { get; }
  int VertexIndex() const { return m_vertex->m_vertex_index; }
  BND_TUPLE EdgeIndices() const;
  int EdgeCount() const { return m_vertex->m_ei.Count();}
};

class BND_BrepVertexList {

  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;

public:
  BND_BrepVertexList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_V.Count(); }
  class BND_BrepVertex* GetVertex(int i);
};

class BND_BrepSurfaceList {
  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;

 public:
  BND_BrepSurfaceList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_S.Count(); }
  class BND_Surface* GetSurface(int i);
};

class BND_BrepEdgeList {
  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;

public:
  BND_BrepEdgeList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_E.Count(); }
  class BND_BrepEdge* GetEdge(int i);
};

class BND_Brep : public BND_GeometryBase
{
  ON_Brep* m_brep = nullptr;
public:
  BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref);

  static BND_Brep* TryConvertBrep(const BND_GeometryBase& geometry);
  static BND_Brep* CreateFromMesh(const class BND_Mesh& mesh, bool trimmedTriangles);
  static BND_Brep* CreateFromBox(const class BND_BoundingBox& box);
  static BND_Brep* CreateFromBox2(const class BND_Box& box);
  //static BND_Brep* CreateFromBox(IEnumerable<Point3d> corners)
  static BND_Brep* CreateFromCylinder(const class BND_Cylinder& cylinder, bool capBottom, bool capTop);
  static BND_Brep* CreateFromSphere(const class BND_Sphere& sphere);
  static BND_Brep* CreateQuadSphere(const class BND_Sphere& sphere);
  static BND_Brep* CreateFromCone(const class BND_Cone& cone, bool capBottom);
  static BND_Brep* CreateFromRevSurface(const class BND_RevSurface& surface, bool capStart, bool capEnd);
  static BND_Brep* CreateFromSurface(const class BND_Surface& surface);
  static BND_Brep* CreateTrimmedPlane(const class BND_Plane& plane, const class BND_Curve& curve);
  //static BND_Brep* CreateTrimmedPlane(Plane plane, IEnumerable<Curve> curves)
  BND_Brep();
  //public Collections.BrepVertexList Vertices
  BND_BrepSurfaceList GetSurfaces();
  BND_BrepEdgeList GetEdges();
  BND_BrepVertexList GetVertices();
  //public Collections.BrepTrimList Trims
  //public Collections.BrepLoopList Loops
  BND_BrepFaceList GetFaces();
  //public Collections.BrepCurveList Curves2D
  //public Collections.BrepCurveList Curves3D
  bool IsSolid() const { return m_brep->IsSolid(); }
  //public BrepSolidOrientation SolidOrientation
  bool IsManifold() const { return m_brep->IsManifold(); }
  bool IsSurface() const { return m_brep->IsSurface(); }
  //public override GeometryBase Duplicate()
  //public Brep DuplicateSubBrep(IEnumerable<int> faceIndices)
  //public Curve[] DuplicateEdgeCurves()
  //public Curve[] DuplicateEdgeCurves(bool nakedOnly)
  //public Curve[] DuplicateNakedEdgeCurves(bool outer, bool inner)
  //public Point3d[] DuplicateVertices()
  void Flip() { m_brep->Flip(); }
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
  bool GetOrientationIsReversed() const { return m_brepface->m_bRev; }
  void SetOrientationIsReversed(bool reversed) { m_brepface->m_bRev = reversed; }
  //bool IsSurface
  //int FaceIndex{ get; }
  //int SurfaceIndex
  //Collections.BrepLoopList Loops
  //BrepLoop OuterLoop
  class BND_Brep* CreateExtrusion(const class BND_Curve* pathCurve, bool cap) const;
  //bool ShrinkFace(ShrinkDisableSide disableSide)
  //override bool SetDomain(int direction, Interval domain)
  class BND_Brep* DuplicateFace(bool duplicateMeshes);
  class BND_Surface* DuplicateSurface();
  class BND_Surface* UnderlyingSurface();
  class BND_Mesh* GetMesh(ON::mesh_type mt);
  //bool SetMesh(MeshType meshType, Mesh mesh)
  bool SetMesh(const class BND_Mesh* m, ON::mesh_type mt);
  //int[] AdjacentEdges()
  //int[] AdjacentFaces()

protected:
  void SetTrackedPointer(ON_BrepFace* brepface, const ON_ModelComponentReference* compref);
};
