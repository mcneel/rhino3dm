#include "bindings.h"

BND_BrepEdge::BND_BrepEdge(ON_BrepEdge* edge, const ON_ModelComponentReference* compref)
{
  m_edge = edge;
  BND_CurveProxy::SetTrackedPointer(edge, compref);
}

BND_Brep::BND_Brep()
{
  SetTrackedPointer(new ON_Brep(), nullptr);
}

BND_Brep::BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brep, compref);
}

BND_Brep* BND_Brep::TryConvertBrep(const BND_GeometryBase& geometry)
{
  const ON_Geometry* g = geometry.GeometryPointer();
  ON_Brep* brep = g ? g->BrepForm(nullptr) : nullptr;
  if (brep)
    return new BND_Brep(brep, nullptr);
  return nullptr;
}

BND_Brep* BND_Brep::CreateFromMesh(const BND_Mesh& mesh, bool trimmedTriangles)
{
  const ON_MeshTopology& top = mesh.m_mesh->Topology();
  ON_Brep* brep = ON_BrepFromMesh(top, trimmedTriangles);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromBox(const BND_BoundingBox& box)
{
  const ON_3dPoint* _boxmin = (const ON_3dPoint*)&box.m_bbox.m_min;
  const ON_3dPoint* _boxmax = (const ON_3dPoint*)&box.m_bbox.m_max;

  ON_3dPoint corners[8];
  corners[0] = *_boxmin;
  corners[1].x = _boxmax->x;
  corners[1].y = _boxmin->y;
  corners[1].z = _boxmin->z;

  corners[2].x = _boxmax->x;
  corners[2].y = _boxmax->y;
  corners[2].z = _boxmin->z;

  corners[3].x = _boxmin->x;
  corners[3].y = _boxmax->y;
  corners[3].z = _boxmin->z;

  corners[4].x = _boxmin->x;
  corners[4].y = _boxmin->y;
  corners[4].z = _boxmax->z;

  corners[5].x = _boxmax->x;
  corners[5].y = _boxmin->y;
  corners[5].z = _boxmax->z;

  corners[6].x = _boxmax->x;
  corners[6].y = _boxmax->y;
  corners[6].z = _boxmax->z;

  corners[7].x = _boxmin->x;
  corners[7].y = _boxmax->y;
  corners[7].z = _boxmax->z;
  ON_Brep* brep = ::ON_BrepBox(corners);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromBox2(const BND_Box& box)
{
  ON_SimpleArray<ON_3dPoint> points;
  if (!box.m_box.GetCorners(points))
    return nullptr;

  ON_Brep* brep = ::ON_BrepBox(points.Array());
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromCylinder(const BND_Cylinder& cylinder, bool capBottom, bool capTop)
{
  ON_Cylinder c = cylinder.m_cylinder;
  c.circle.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepCylinder(c, capBottom, capTop);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromSphere(const BND_Sphere& sphere)
{
  ON_Sphere s = sphere.m_sphere;
  s.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepSphere(s);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateQuadSphere(const BND_Sphere& sphere)
{
  ON_Sphere s = sphere.m_sphere;
  s.plane.UpdateEquation();
  if (s.IsValid())
  {
    ON_3dPoint origin = ON_3dPoint::Origin;
    ON_Brep* brep = ON_BrepQuadSphere(origin, 1.0);
    if (brep)
    {
      ON_Xform rotate_xform;
      rotate_xform.Rotation(ON_Plane::World_xy, s.plane);
      ON_Xform scale_xform = ON_Xform::ScaleTransformation(s.plane, s.radius, s.radius, s.radius);
      ON_Xform xform = scale_xform * rotate_xform;
      brep->Transform(xform);
      return new BND_Brep(brep, nullptr);
    }
  }
  return nullptr;
}

BND_Brep* BND_Brep::CreateFromCone(const BND_Cone& cone, bool capBottom)
{
  ON_Cone c = cone.m_cone;
  c.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepCone(c, capBottom);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateFromRevSurface(const BND_RevSurface& surface, bool capStart, bool capEnd)
{
  ON_RevSurface* rev = surface.m_revsurface->Duplicate();
  ON_Brep* brep = ON_BrepRevSurface(rev, capStart, capEnd);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateFromSurface(const BND_Surface& surface)
{
  ON_Surface* surf = surface.m_surface->DuplicateSurface();
  ON_Brep* brep = ON_Brep::New();
  if (!brep->Create(surf))
  {
    delete surf;
    delete brep;
    return nullptr;
  }
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateTrimmedPlane(const BND_Plane& plane, const BND_Curve& curve)
{
  ON_Plane p = plane.ToOnPlane();
  ON_Brep* brep = ON_BrepTrimmedPlane(p, *(curve.m_curve));
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}


void BND_Brep::SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  m_brep = brep;
  BND_GeometryBase::SetTrackedPointer(brep, compref);
}

BND_BrepFaceList BND_Brep::GetFaces()
{
  return BND_BrepFaceList(m_brep, m_component_ref);
}

BND_BrepFaceList::BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_BrepFace* BND_BrepFaceList::GetFace(int i)
{
#if defined(ON_PYTHON_COMPILE)
  if (i >= Count())
    throw pybind11::index_error();
#endif

  ON_BrepFace* face = m_brep->Face(i);
  if (nullptr == face)
    return nullptr;
  return new BND_BrepFace(face, &m_component_reference);
}

BND_BrepFace::BND_BrepFace(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brepface, compref);
}

void BND_BrepFace::SetTrackedPointer(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  m_brepface = brepface;
  BND_SurfaceProxy::SetTrackedPointer(brepface, compref);
}

BND_Brep* BND_BrepFace::CreateExtrusion(const class BND_Curve* pathCurve, bool cap) const
{
  BND_Brep* rc = nullptr;
  const ON_Brep* pConstBrep = m_brepface->Brep();
  const ON_Curve* pConstCurve = pathCurve ? pathCurve->m_curve : nullptr;
  if (pConstBrep && pConstCurve)
  {
    ON_Brep* pNewBrep = ON_Brep::New(*pConstBrep);
    if (pNewBrep)
    {
      pNewBrep->DestroyMesh(ON::any_mesh);
      int result = ON_BrepExtrudeFace(*pNewBrep, m_brepface->m_face_index, *pConstCurve, cap);
      // 0 == failure, 1 or 2 == success
      if (0 == result)
        delete pNewBrep;
      else
        rc = new BND_Brep(pNewBrep, nullptr);
    }
  }
  return rc;
}

BND_Brep* BND_BrepFace::DuplicateFace(bool duplicateMeshes)
{
  const ON_Brep* parentBrep = m_brepface->Brep();
  if (parentBrep)
  {
    ON_Brep* rc = parentBrep->DuplicateFace(m_brepface->m_face_index, duplicateMeshes);
    if (rc)
      return new BND_Brep(rc, nullptr);
  }
  return nullptr;
}

BND_Surface* BND_BrepFace::DuplicateSurface()
{
  BND_CommonObject* co = BND_CommonObject::CreateWrapper(m_brepface->DuplicateSurface(), nullptr);
  return dynamic_cast<BND_Surface*>(co);
}

BND_Surface* BND_BrepFace::UnderlyingSurface()
{
  ON_Surface* srf = const_cast<ON_Surface*>(m_brepface->SurfaceOf());
  return new BND_Surface(srf, &m_component_ref);
}

BND_Mesh* BND_BrepFace::GetMesh(ON::mesh_type mt)
{
  ON_Mesh* mesh = const_cast<ON_Mesh*>(m_brepface->Mesh(mt));
  if (nullptr == mesh)
    return nullptr;
  return new BND_Mesh(mesh, &m_component_ref);
}

/*
bool BND_BrepFace::SetMesh(const class BNC_Mesh* mesh, ON::mesh_type mt)
{
  bool rc = false;
  if( mesh )
  {
    rc = m_brepface->SetMesh( mt, mesh );
  }
  return rc;
}
*/


BND_BrepSurfaceList BND_Brep::GetSurfaces()
{
  return BND_BrepSurfaceList(m_brep, m_component_ref);
}

BND_BrepEdgeList BND_Brep::GetEdges()
{
  return BND_BrepEdgeList(m_brep, m_component_ref);
}


BND_BrepSurfaceList::BND_BrepSurfaceList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_Surface* BND_BrepSurfaceList::GetSurface(int i)
{
#if defined(ON_PYTHON_COMPILE)
  if (i >= Count())
    throw pybind11::index_error();
#endif

  ON_Surface* surface = nullptr;
  if (i >= 0 && i < m_brep->m_S.Count())
  {
    surface = m_brep->m_S[i];
  }
  if (nullptr == surface)
    return nullptr;
  return new BND_Surface(surface, &m_component_reference);
}

BND_BrepEdgeList::BND_BrepEdgeList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_BrepEdge* BND_BrepEdgeList::GetEdge(int i)
{
#if defined(ON_PYTHON_COMPILE)
  if (i >= Count())
    throw pybind11::index_error();
#endif

  ON_BrepEdge* edge = m_brep->Edge(i);
  if (nullptr == edge)
    return nullptr;
  return new BND_BrepEdge(edge, &m_component_reference);
}

BND_BrepVertex::BND_BrepVertex(ON_BrepVertex* vertex, const ON_ModelComponentReference* compref)
  :BND_Point(vertex, compref)
{
  m_vertex = vertex;
}


BND_BrepVertexList::BND_BrepVertexList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_BrepVertexList BND_Brep::GetVertices()
{
  return BND_BrepVertexList(m_brep, m_component_ref);
}


BND_BrepVertex* BND_BrepVertexList::GetVertex(int i) {

#if defined(ON_PYTHON_COMPILE)
  if (i >= Count())
    throw pybind11::index_error();
#endif

  ON_BrepVertex* vertex = m_brep->Vertex(i);
  if (nullptr == vertex)
    return nullptr;
  return new BND_BrepVertex(vertex, &m_component_reference);

}

BND_TUPLE BND_BrepVertex::EdgeIndices() const {

  int count = m_vertex->m_ei.Count();
  BND_TUPLE rc = CreateTuple(count);
  for (int i = 0; i < count; i++)
    SetTuple(rc, i, m_vertex->m_ei[i]);
  return rc;

}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBrepBindings(pybind11::module& m)
{
  py::class_<BND_BrepEdge, BND_CurveProxy>(m, "BrepEdge")
    ;

  py::class_<BND_BrepVertex, BND_Point>(m, "BrepVertex")
    .def_property_readonly("VertexIndex", &BND_BrepVertex::VertexIndex)
    .def_property_readonly("EdgeCount", &BND_BrepVertex::EdgeCount)
    .def("EdgeIndices", &BND_BrepVertex::EdgeIndices)
    ;

  py::class_<BND_BrepFace, BND_SurfaceProxy>(m, "BrepFace")
    .def("UnderlyingSurface", &BND_BrepFace::UnderlyingSurface)
    .def("CreateExtrusion", &BND_BrepFace::CreateExtrusion, py::arg("pathCurve"), py::arg("cap"))
    .def("DuplicateFace", &BND_BrepFace::DuplicateFace, py::arg("duplicateMeshes"))
    .def("DuplicateSurface", &BND_BrepFace::DuplicateSurface)
    .def("GetMesh", &BND_BrepFace::GetMesh, py::arg("meshType"))
    //.def("SetMesh", &BND_BrepFace::SetMesh, py::arg("mesh"), py::arg("meshType"))
    ;

  py::class_<BND_BrepFaceList>(m, "BrepFaceList")
    .def("__len__", &BND_BrepFaceList::Count)
    .def("__getitem__", &BND_BrepFaceList::GetFace)
    ;
  
  py::class_<BND_BrepSurfaceList>(m, "BrepSurfaceList")
    .def("__len__", &BND_BrepSurfaceList::Count)
    .def("__getitem__", &BND_BrepSurfaceList::GetSurface)
    ;

  py::class_<BND_BrepEdgeList>(m, "BrepEdgeList")
    .def("__len__", &BND_BrepEdgeList::Count)
    .def("__getitem__", &BND_BrepEdgeList::GetEdge)
    ;

  py::class_<BND_BrepVertexList>(m, "BrepVertexList")
    .def("__len__", &BND_BrepVertexList::Count)
    .def("__getitem__", &BND_BrepVertexList::GetVertex)
    ;

  py::class_<BND_Brep, BND_GeometryBase>(m, "Brep")
    .def(py::init<>())
    .def_static("TryConvertBrep", &BND_Brep::TryConvertBrep, py::arg("geometry"))
    .def_static("CreateFromMesh", &BND_Brep::CreateFromMesh, py::arg("mesh"), py::arg("trimmedTriangles"))
    .def_static("CreateFromBox", &BND_Brep::CreateFromBox, py::arg("box"))
    .def_static("CreateFromBox", &BND_Brep::CreateFromBox2, py::arg("box"))
    .def_static("CreateFromCylinder", &BND_Brep::CreateFromCylinder, py::arg("cylinder"), py::arg("capBottom"), py::arg("capTop"))
    .def_static("CreateFromSphere", &BND_Brep::CreateFromSphere, py::arg("sphere"))
    .def_static("CreateQuadSphere", &BND_Brep::CreateQuadSphere, py::arg("sphere"))
    .def_static("CreateFromCone", &BND_Brep::CreateFromCone, py::arg("cone"), py::arg("capBottom"))
    .def_static("CreateFromRevSurface", &BND_Brep::CreateFromRevSurface, py::arg("surface"), py::arg("capStart"), py::arg("capEnd"))
    .def_static("CreateFromSurface", &BND_Brep::CreateFromSurface, py::arg("surface"))
    .def_static("CreateTrimmedPlane", &BND_Brep::CreateTrimmedPlane, py::arg("plane"), py::arg("curve"))
    .def_property_readonly("Faces", &BND_Brep::GetFaces)
    .def_property_readonly("Surfaces", &BND_Brep::GetSurfaces)
    .def_property_readonly("Edges", &BND_Brep::GetEdges)
    .def_property_readonly("Vertices", &BND_Brep::GetVertices)
    .def_property_readonly("IsSolid", &BND_Brep::IsSolid)
    .def_property_readonly("IsManifold", &BND_Brep::IsManifold)
    .def_property_readonly("IsSurface", &BND_Brep::IsSurface)
    .def("Flip", &BND_Brep::Flip)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBrepBindings(void*)
{
  class_<BND_BrepEdge, base<BND_CurveProxy>>("BrepEdge")
    ;

  class_<BND_BrepVertex, base<BND_Point>>("BrepVertex")
    .property("vertexIndex", &BND_BrepVertex::VertexIndex)
    .property("edgeCount", &BND_BrepVertex::EdgeCount)
    .property("edgeIndices", &BND_BrepVertex::EdgeIndices)
    ;

  class_<BND_BrepFace, base<BND_SurfaceProxy>>("BrepFace")
    .function("underlyingSurface", &BND_BrepFace::UnderlyingSurface, allow_raw_pointers())
    .function("createExtrusion", &BND_BrepFace::CreateExtrusion, allow_raw_pointers())
    .function("duplicateFace", &BND_BrepFace::DuplicateFace, allow_raw_pointers())
    .function("duplicateSurface", &BND_BrepFace::DuplicateSurface, allow_raw_pointers())
    .function("getMesh", &BND_BrepFace::GetMesh, allow_raw_pointers())
    //.function("setMesh", &BND_BrepFace::SetMesh, allow_raw_pointers())
    ;

  class_<BND_BrepFaceList>("BrepFaceList")
    .property("count", &BND_BrepFaceList::Count)
    .function("get", &BND_BrepFaceList::GetFace, allow_raw_pointers())
    ;

  class_<BND_BrepSurfaceList>("BrepSurfaceList")
    .property("count", &BND_BrepSurfaceList::Count)
    .function("get", &BND_BrepSurfaceList::GetSurface, allow_raw_pointers())
    ;

  class_<BND_BrepEdgeList>("BrepEdgeList")
    .property("count", &BND_BrepEdgeList::Count)
    .function("get", &BND_BrepEdgeList::GetEdge, allow_raw_pointers())
    ;

  class_<BND_BrepVertexList>("BrepVertexList")
    .property("count", &BND_BrepVertexList::Count)
    .function("get", &BND_BrepVertexList::GetVertex, allow_raw_pointers())
    ;

  class_<BND_Brep, base<BND_GeometryBase>>("Brep")
    .constructor<>()
    .class_function("createFromMesh", &BND_Brep::CreateFromMesh, allow_raw_pointers())
    .class_function("createFromBox", &BND_Brep::CreateFromBox, allow_raw_pointers())
    //.class_function("CreateFromBox", &BND_Brep::CreateFromBox2, allow_raw_pointers())
    .class_function("createFromCylinder", &BND_Brep::CreateFromCylinder, allow_raw_pointers())
    .class_function("createFromSphere", &BND_Brep::CreateFromSphere, allow_raw_pointers())
    .class_function("createQuadSphere", &BND_Brep::CreateQuadSphere, allow_raw_pointers())
    .class_function("createFromCone", &BND_Brep::CreateFromCone, allow_raw_pointers())
    .class_function("createFromRevSurface", &BND_Brep::CreateFromRevSurface, allow_raw_pointers())
    .class_function("createFromSurface", &BND_Brep::CreateFromSurface, allow_raw_pointers())
    .class_function("createTrimmedPlane", &BND_Brep::CreateTrimmedPlane, allow_raw_pointers())
    .function("faces", &BND_Brep::GetFaces)
    .function("surfaces", &BND_Brep::GetSurfaces)
    .function("edges", &BND_Brep::GetEdges)
    .function("vertices", &BND_Brep::GetVertices)
    .property("isSolid", &BND_Brep::IsSolid)
    .property("isManifold", &BND_Brep::IsManifold)
    .property("isSurface", &BND_Brep::IsSurface)
    .function("flip", &BND_Brep::Flip)
    ;
}
#endif
