#include "bindings.h"
#include "base64.h"



#if defined(ON_PYTHON_COMPILE)
pybind11::dict BND_MeshingParameters::Encode() const
{
  pybind11::dict d;
  d["TextureRange"] = GetTextureRange();
  d["JaggedSeams"] = GetJaggedSeams();
  d["RefineGrid"] = GetRefineGrid();
  d["SimplePlanes"] = GetSimplePlanes();
  d["ComputeCurvature"] = GetComputeCurvature();
  d["ClosedObjectPostProcess"] = GetClosedObjectPostProcess();
  d["GridMinCount"] = GetGridMinCount();
  d["GridMaxCount"] = GetGridMaxCount();
  d["GridAngle"] = GetGridAngle();
  d["GridAspectRatio"] = GetGridAspectRatio();
  d["GridAmplification"] = GetGridAmplification();
  d["Tolerance"] = GetTolerance();
  d["MinimumTolerance"] = GetMinimumTolerance();
  d["RelativeTolerance"] = GetRelativeTolerance();
  d["MinimumEdgeLength"] = GetMinimumEdgeLength();
  d["MaximumEdgeLength"] = GetMaximumEdgeLength();
  d["RefineAngle"] = GetRefineAngle();
  return d;
}

BND_MeshingParameters* BND_MeshingParameters::Decode(pybind11::dict jsonObject)
{
  BND_MeshingParameters* mp = new BND_MeshingParameters();
  mp->SetTextureRange(jsonObject["TextureRange"].cast<int>());
  mp->SetJaggedSeams(jsonObject["JaggedSeams"].cast<bool>());
  mp->SetRefineGrid(jsonObject["RefineGrid"].cast<bool>());
  mp->SetSimplePlanes(jsonObject["SimplePlanes"].cast<bool>());
  mp->SetComputeCurvature(jsonObject["ComputeCurvature"].cast<bool>());
  mp->SetClosedObjectPostProcess(jsonObject["ClosedObjectPostProcess"].cast<bool>());
  mp->SetGridMinCount(jsonObject["GridMinCount"].cast<int>());
  mp->SetGridMaxCount(jsonObject["GridMaxCount"].cast<int>());
  mp->SetGridAngle(jsonObject["GridAngle"].cast<double>());
  mp->SetGridAspectRatio(jsonObject["GridAspectRatio"].cast<double>());
  mp->SetGridAmplification(jsonObject["GridAmplification"].cast<double>());
  mp->SetTolerance(jsonObject["Tolerance"].cast<double>());
  mp->SetMinimumTolerance(jsonObject["MinimumTolerance"].cast<double>());
  mp->SetRelativeTolerance(jsonObject["RelativeTolerance"].cast<double>());
  mp->SetMinimumEdgeLength(jsonObject["MinimumEdgeLength"].cast<double>());
  mp->SetMaximumEdgeLength(jsonObject["MaximumEdgeLength"].cast<double>());
  mp->SetRefineAngle(jsonObject["RefineAngle"].cast<double>());
  return mp;
}
#endif

#if defined(__EMSCRIPTEN__)
emscripten::val BND_MeshingParameters::Encode() const
{
  emscripten::val v(emscripten::val::object());
  v.set("TextureRange", emscripten::val(GetTextureRange()));
  v.set("JaggedSeams", emscripten::val(GetJaggedSeams()));
  v.set("RefineGrid", emscripten::val(GetRefineGrid()));
  v.set("SimplePlanes", emscripten::val(GetSimplePlanes()));
  v.set("ComputeCurvature", emscripten::val(GetComputeCurvature()));
  v.set("ClosedObjectPostProcess", emscripten::val(GetClosedObjectPostProcess()));
  v.set("GridMinCount", emscripten::val(GetGridMinCount()));
  v.set("GridMaxCount", emscripten::val(GetGridMaxCount()));
  v.set("GridAngle", emscripten::val(GetGridAngle()));
  v.set("GridAspectRatio", emscripten::val(GetGridAspectRatio()));
  v.set("GridAmplification", emscripten::val(GetGridAmplification()));
  v.set("Tolerance", emscripten::val(GetTolerance()));
  v.set("MinimumTolerance", emscripten::val(GetMinimumTolerance()));
  v.set("RelativeTolerance", emscripten::val(GetRelativeTolerance()));
  v.set("MinimumEdgeLength", emscripten::val(GetMinimumEdgeLength()));
  v.set("MaximumEdgeLength", emscripten::val(GetMaximumEdgeLength()));
  v.set("RefineAngle", emscripten::val(GetRefineAngle()));
  return v;
}

emscripten::val BND_MeshingParameters::toJSON(emscripten::val key)
{
  return Encode();
}

BND_MeshingParameters* BND_MeshingParameters::Decode(emscripten::val jsonObject)
{
  BND_MeshingParameters* mp = new BND_MeshingParameters();
  mp->SetTextureRange(jsonObject["TextureRange"].as<int>());
  mp->SetJaggedSeams(jsonObject["JaggedSeams"].as<bool>());
  mp->SetRefineGrid(jsonObject["RefineGrid"].as<bool>());
  mp->SetSimplePlanes(jsonObject["SimplePlanes"].as<bool>());
  mp->SetComputeCurvature(jsonObject["ComputeCurvature"].as<bool>());
  mp->SetClosedObjectPostProcess(jsonObject["ClosedObjectPostProcess"].as<bool>());
  mp->SetGridMinCount(jsonObject["GridMinCount"].as<int>());
  mp->SetGridMaxCount(jsonObject["GridMaxCount"].as<int>());
  mp->SetGridAngle(jsonObject["GridAngle"].as<double>());
  mp->SetGridAspectRatio(jsonObject["GridAspectRatio"].as<double>());
  mp->SetGridAmplification(jsonObject["GridAmplification"].as<double>());
  mp->SetTolerance(jsonObject["Tolerance"].as<double>());
  mp->SetMinimumTolerance(jsonObject["MinimumTolerance"].as<double>());
  mp->SetRelativeTolerance(jsonObject["RelativeTolerance"].as<double>());
  mp->SetMinimumEdgeLength(jsonObject["MinimumEdgeLength"].as<double>());
  mp->SetMaximumEdgeLength(jsonObject["MaximumEdgeLength"].as<double>());
  mp->SetRefineAngle(jsonObject["RefineAngle"].as<double>());
  return mp;
}

#endif



BND_Mesh::BND_Mesh()
{
  SetTrackedPointer(new ON_Mesh(), nullptr);
}

BND_Mesh::BND_Mesh(ON_Mesh* mesh, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(mesh, compref);
}

void BND_Mesh::SetTrackedPointer(ON_Mesh* mesh, const ON_ModelComponentReference* compref)
{
  m_mesh = mesh;
  BND_GeometryBase::SetTrackedPointer(mesh, compref);
}

BND_MeshVertexList BND_Mesh::GetVertices()
{
  return BND_MeshVertexList(m_mesh, m_component_ref);
}

BND_MeshFaceList BND_Mesh::GetFaces()
{
  return BND_MeshFaceList(m_mesh, m_component_ref);
}

BND_MeshNormalList BND_Mesh::GetNormals()
{
  return BND_MeshNormalList(m_mesh, m_component_ref);
}

BND_MeshVertexColorList BND_Mesh::VertexColors()
{
  return BND_MeshVertexColorList(m_mesh, m_component_ref);
}

BND_MeshTextureCoordinateList BND_Mesh::TextureCoordinates()
{
  return BND_MeshTextureCoordinateList(m_mesh, m_component_ref);
}

#if defined(ON_PYTHON_COMPILE)
pybind11::tuple BND_Mesh::IsManifold(bool topologicalTest) const
{
  bool oriented = false;
  bool hasboundary = false;
  pybind11::tuple rc(3);
  rc[0] = m_mesh->IsManifold(topologicalTest, &oriented, &hasboundary);
  rc[1] = oriented;
  rc[2] = hasboundary;
  return rc;
}
#endif

static void ON_Mesh_DestroyTextureData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_Ttag.Default();
    pMesh->m_T.Destroy();
    pMesh->m_TC.Destroy();
    pMesh->m_packed_tex_domain[0].Set(0, 1);
    pMesh->m_packed_tex_domain[1].Set(0, 1);
    pMesh->InvalidateTextureCoordinateBoundingBox();
  }
}

void BND_Mesh::ClearTextureData()
{
  ON_Mesh_DestroyTextureData(m_mesh);
}

void ON_Mesh_DestroySurfaceData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_S.Destroy();
    pMesh->m_srf_domain[0] = ON_Interval::EmptyInterval;
    pMesh->m_srf_domain[1] = ON_Interval::EmptyInterval;
    pMesh->m_srf_scale[0] = 0;
    pMesh->m_srf_scale[1] = 0;
    pMesh->InvalidateCurvatureStats();
    pMesh->m_K.Destroy();
  }
}

void BND_Mesh::ClearSurfaceData()
{
  ON_Mesh_DestroySurfaceData(m_mesh);
}

int BND_Mesh::PartitionCount() const
{
  const ON_MeshPartition* pPartition = m_mesh->Partition();
  if (pPartition)
    return pPartition->m_part.Count();
  return 0;
}



BND_MeshVertexList::BND_MeshVertexList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_3fPoint* BND_MeshVertexList::begin()
{
  return m_mesh->m_V.At(0);
}
ON_3fPoint* BND_MeshVertexList::end()
{
  int count = m_mesh->m_V.Count();
  if( 0==count )
    return nullptr;
  return m_mesh->m_V.At(count-1);
}

BND_MeshFaceList::BND_MeshFaceList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

int BND_MeshVertexList::Count() const
{
  return m_mesh->VertexCount();
}

int BND_MeshFaceList::Count() const
{
  return m_mesh->FaceCount();
}

void BND_MeshVertexList::SetCount(int value)
{
  ON_Mesh* pMesh = m_mesh;
  const bool hasDoublePrecisionVerts = pMesh->HasDoublePrecisionVertices();
  pMesh->m_V.Reserve(value);
  pMesh->m_V.SetCount(value);
  if (hasDoublePrecisionVerts)
  {
    pMesh->DoublePrecisionVertices().Reserve(value);
    pMesh->DoublePrecisionVertices().SetCount(value);
  }
}

ON_3fPoint BND_MeshVertexList::GetVertex(int i) const
{
  return m_mesh->m_V[i];
}

void BND_MeshVertexList::SetVertex(int i, ON_3fPoint pt)
{
  m_mesh->m_V[i] = pt;
}

#if defined(__EMSCRIPTEN__)
emscripten::val BND_MeshFaceList::GetFace(int i) const
{
  ON_MeshFace& face = m_mesh->m_F[i];
  emscripten::val v(emscripten::val::array());
  v.call<void>("push", face.vi[0]);
  v.call<void>("push", face.vi[1]);
  v.call<void>("push", face.vi[2]);
  v.call<void>("push", face.vi[3]);
  return v;
}
#endif

BND_MeshNormalList::BND_MeshNormalList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

BND_MeshVertexColorList::BND_MeshVertexColorList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}


ON_3fVector* BND_MeshNormalList::begin()
{
  return m_mesh->m_N.At(0);
}
ON_3fVector* BND_MeshNormalList::end()
{
  int count = m_mesh->m_N.Count();
  if (0 == count)
    return nullptr;
  return m_mesh->m_N.At(count - 1);
}

int BND_MeshNormalList::Count() const
{
  return m_mesh->m_N.Count();
}

ON_3fVector BND_MeshNormalList::GetNormal(int i) const
{
  return m_mesh->m_N[i];
}

void BND_MeshNormalList::SetNormal(int i, ON_3fVector v)
{
  m_mesh->m_N[i] = v;
}

BND_MeshTextureCoordinateList::BND_MeshTextureCoordinateList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_2fPoint* BND_MeshTextureCoordinateList::begin()
{
  return m_mesh->m_T.At(0);
}
ON_2fPoint* BND_MeshTextureCoordinateList::end()
{
  int count = m_mesh->m_T.Count();
  if (0 == count)
    return nullptr;
  return m_mesh->m_T.At(count - 1);
}

#if defined(ON_PYTHON_COMPILE)
pybind11::list BND_MeshFaceList::GetFace(int i) const
{
  pybind11::list rc;
  ON_MeshFace& face = m_mesh->m_F[i];
  rc.append(face.vi[0]);
  rc.append(face.vi[1]);
  rc.append(face.vi[2]);
  rc.append(face.vi[3]);
  return rc;
}
#endif


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMeshBindings(pybind11::module& m)
{
  py::class_<BND_MeshingParameters>(m, "MeshingParameters")
    .def(py::init<>())
    .def(py::init<double>(), py::arg("density"))
    .def(py::init<double, double>(), py::arg("density"), py::arg("minimumEdgeLength"))
    .def_property_readonly_static("Default", &BND_MeshingParameters::Default)
    .def_property_readonly_static("FastRenderMesh", &BND_MeshingParameters::FastRenderMesh)
    .def_property_readonly_static("QualityRenderMesh", &BND_MeshingParameters::QualityRenderMesh)
    .def_property_readonly_static("DefaultAnalysisMesh", &BND_MeshingParameters::DefaultAnalysisMesh)
    .def_property("TextureRange", &BND_MeshingParameters::GetTextureRange, &BND_MeshingParameters::SetTextureRange)
    .def_property("JaggedSeams", &BND_MeshingParameters::GetJaggedSeams, &BND_MeshingParameters::SetJaggedSeams)
    .def_property("RefineGrid", &BND_MeshingParameters::GetRefineGrid, &BND_MeshingParameters::SetRefineGrid)
    .def_property("SimplePlanes", &BND_MeshingParameters::GetSimplePlanes, &BND_MeshingParameters::SetSimplePlanes)
    .def_property("ComputeCurvature", &BND_MeshingParameters::GetComputeCurvature, &BND_MeshingParameters::SetComputeCurvature)
    .def_property("ClosedObjectPostProcess", &BND_MeshingParameters::GetClosedObjectPostProcess, &BND_MeshingParameters::SetClosedObjectPostProcess)
    .def_property("GridMinCount", &BND_MeshingParameters::GetGridMinCount, &BND_MeshingParameters::SetGridMinCount)
    .def_property("GridMaxCount", &BND_MeshingParameters::GetGridMaxCount, &BND_MeshingParameters::SetGridMaxCount)
    .def_property("GridAngle", &BND_MeshingParameters::GetGridAngle, &BND_MeshingParameters::SetGridAngle)
    .def_property("GridAspectRatio", &BND_MeshingParameters::GetGridAspectRatio, &BND_MeshingParameters::SetGridAspectRatio)
    .def_property("GridAmplification", &BND_MeshingParameters::GetGridAmplification, &BND_MeshingParameters::SetGridAmplification)
    .def_property("Tolerance", &BND_MeshingParameters::GetTolerance, &BND_MeshingParameters::SetTolerance)
    .def_property("MinimumTolerance", &BND_MeshingParameters::GetMinimumTolerance, &BND_MeshingParameters::SetMinimumTolerance)
    .def_property("RelativeTolerance", &BND_MeshingParameters::GetRelativeTolerance, &BND_MeshingParameters::SetRelativeTolerance)
    .def_property("MinimumEdgeLength", &BND_MeshingParameters::GetMinimumEdgeLength, &BND_MeshingParameters::SetMinimumEdgeLength)
    .def_property("MaximumEdgeLength", &BND_MeshingParameters::GetMaximumEdgeLength, &BND_MeshingParameters::SetMaximumEdgeLength)
    .def_property("RefineAngle", &BND_MeshingParameters::GetRefineAngle, &BND_MeshingParameters::SetRefineAngle)
    .def("Encode", &BND_MeshingParameters::Encode)
    .def_static("Decode", &BND_MeshingParameters::Decode, py::arg("jsonObject"))
    ;

  py::class_<BND_MeshVertexList>(m, "MeshVertexList")
    .def("__len__", &BND_MeshVertexList::Count)
    .def("SetCount", &BND_MeshVertexList::SetCount)
    .def("__getitem__", &BND_MeshVertexList::GetVertex)
    .def("__setitem__", &BND_MeshVertexList::SetVertex)
    .def("__iter__", [](BND_MeshVertexList &s) { return py::make_iterator(s.begin(), s.end()); },
      py::keep_alive<0, 1>() /* Essential: keep object alive while iterator exists */)
    ;

  py::class_<BND_MeshFaceList>(m, "MeshFaceList")
    .def("__len__", &BND_MeshFaceList::Count)
    .def("__getitem__", &BND_MeshFaceList::GetFace)
    ;

  py::class_<BND_MeshNormalList>(m, "MeshNormalList")
    .def("__len__", &BND_MeshNormalList::Count)
    .def("__getitem__", &BND_MeshNormalList::GetNormal)
    .def("__setitem__", &BND_MeshNormalList::SetNormal)
    .def("__iter__", [](BND_MeshNormalList &s) { return py::make_iterator(s.begin(),
        s.end()); },
         py::keep_alive<0, 1>())
    ;

  py::class_<BND_MeshVertexColorList>(m, "MeshVertexColorList")
    .def("__len__", &BND_MeshVertexColorList::Count)
    .def("__getitem__", &BND_MeshVertexColorList::GetColor)
    .def("__setitem__", &BND_MeshVertexColorList::SetColor)
    ;

  py::class_<BND_MeshTextureCoordinateList>(m, "MeshTextureCoordinateList")
    .def("__len__", &BND_MeshTextureCoordinateList::Count)
    .def("__getitem__", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .def("__setitem__", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    .def("__iter__", [](BND_MeshTextureCoordinateList &s) { return py::make_iterator(s.begin(), s.end()); },
      py::keep_alive<0, 1>() /* Essential: keep object alive while iterator exists */)
    ;

  py::class_<BND_Mesh, BND_GeometryBase>(m, "Mesh")
    .def(py::init<>())
    .def_property_readonly("IsClosed", &BND_Mesh::IsClosed)
    .def("IsManifold", &BND_Mesh::IsManifold, py::arg("topologicalTest"))
    .def_property_readonly("HasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .def_property_readonly("Vertices", &BND_Mesh::GetVertices)
    .def_property_readonly("Faces", &BND_Mesh::GetFaces)
    .def_property_readonly("Normals", &BND_Mesh::GetNormals)
    .def_property_readonly("VertexColors", &BND_Mesh::VertexColors)
    .def_property_readonly("TextureCoordinates", &BND_Mesh::TextureCoordinates)
    .def("ClearTextureData", &BND_Mesh::ClearTextureData)
    .def("ClearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .def("DestroyTopology", &BND_Mesh::DestroyTopology)
    .def("DestroyTree", &BND_Mesh::DestroyTree)
    .def("DestroyPartition", &BND_Mesh::DestroyPartition)
    .def("Compact", &BND_Mesh::Compact)
    .def("Append", &BND_Mesh::Append, py::arg("other"))
    .def("CreatePartitions", &BND_Mesh::CreatePartitions, py::arg("maximumVertexCount"), py::arg("maximumTriangleCount"))
    .def_property_readonly("PartitionCount", &BND_Mesh::PartitionCount)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMeshBindings(void*)
{
  class_<BND_MeshingParameters>("MeshingParameters")
    .constructor<>()
    .constructor<double>()
    .constructor<double, double>()
    .class_function("default", &BND_MeshingParameters::Default, allow_raw_pointers())
    .class_function("fastRenderMesh", &BND_MeshingParameters::FastRenderMesh, allow_raw_pointers())
    .class_function("qualityRenderMesh", &BND_MeshingParameters::QualityRenderMesh, allow_raw_pointers())
    .class_function("defaultAnalysisMesh", &BND_MeshingParameters::DefaultAnalysisMesh, allow_raw_pointers())
    .property("textureRange", &BND_MeshingParameters::GetTextureRange, &BND_MeshingParameters::SetTextureRange)
    .property("jaggedSeams", &BND_MeshingParameters::GetJaggedSeams, &BND_MeshingParameters::SetJaggedSeams)
    .property("refineGrid", &BND_MeshingParameters::GetRefineGrid, &BND_MeshingParameters::SetRefineGrid)
    .property("simplePlanes", &BND_MeshingParameters::GetSimplePlanes, &BND_MeshingParameters::SetSimplePlanes)
    .property("computeCurvature", &BND_MeshingParameters::GetComputeCurvature, &BND_MeshingParameters::SetComputeCurvature)
    .property("closedObjectPostProcess", &BND_MeshingParameters::GetClosedObjectPostProcess, &BND_MeshingParameters::SetClosedObjectPostProcess)
    .property("gridMinCount", &BND_MeshingParameters::GetGridMinCount, &BND_MeshingParameters::SetGridMinCount)
    .property("gridMaxCount", &BND_MeshingParameters::GetGridMaxCount, &BND_MeshingParameters::SetGridMaxCount)
    .property("gridAngle", &BND_MeshingParameters::GetGridAngle, &BND_MeshingParameters::SetGridAngle)
    .property("gridAspectRatio", &BND_MeshingParameters::GetGridAspectRatio, &BND_MeshingParameters::SetGridAspectRatio)
    .property("gridAmplification", &BND_MeshingParameters::GetGridAmplification, &BND_MeshingParameters::SetGridAmplification)
    .property("tolerance", &BND_MeshingParameters::GetTolerance, &BND_MeshingParameters::SetTolerance)
    .property("minimumTolerance", &BND_MeshingParameters::GetMinimumTolerance, &BND_MeshingParameters::SetMinimumTolerance)
    .property("relativeTolerance", &BND_MeshingParameters::GetRelativeTolerance, &BND_MeshingParameters::SetRelativeTolerance)
    .property("minimumEdgeLength", &BND_MeshingParameters::GetMinimumEdgeLength, &BND_MeshingParameters::SetMinimumEdgeLength)
    .property("maximumEdgeLength", &BND_MeshingParameters::GetMaximumEdgeLength, &BND_MeshingParameters::SetMaximumEdgeLength)
    .property("refineAngle", &BND_MeshingParameters::GetRefineAngle, &BND_MeshingParameters::SetRefineAngle)
    .function("toJSON", &BND_MeshingParameters::toJSON)
    .function("encode", &BND_MeshingParameters::Encode)
    .class_function("decode", &BND_MeshingParameters::Decode, allow_raw_pointers())
    ;


  class_<BND_MeshVertexList>("MeshVertexList")
    .property("count", &BND_MeshVertexList::Count)
    .function("setCount", &BND_MeshVertexList::SetCount)
    .function("get", &BND_MeshVertexList::GetVertex)
    .function("set", &BND_MeshVertexList::SetVertex)
    ;

  class_<BND_MeshFaceList>("MeshFaceList")
    .property("count", &BND_MeshFaceList::Count)
    .function("get", &BND_MeshFaceList::GetFace)
    ;

  class_<BND_MeshNormalList>("MeshNormalList")
    .property("count", &BND_MeshNormalList::Count)
    .function("get", &BND_MeshNormalList::GetNormal)
    .function("set", &BND_MeshNormalList::SetNormal)
    ;

  class_<BND_MeshTextureCoordinateList>("MeshTextureCoordinateList")
    .property("count", &BND_MeshTextureCoordinateList::Count)
    .function("get", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .function("set", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    ;

  class_<BND_Mesh, base<BND_GeometryBase>>("Mesh")
    .constructor<>()
    .property("isClosed", &BND_Mesh::IsClosed)
    //.function("isManifold", &BND_Mesh::IsManifold)
    .property("hasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .function("vertices", &BND_Mesh::GetVertices)
    .function("faces", &BND_Mesh::GetFaces)
    .function("normals", &BND_Mesh::GetNormals)
    .function("textureCoordinates", &BND_Mesh::TextureCoordinates)
    .function("clearTextureData", &BND_Mesh::ClearTextureData)
    .function("clearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .function("destroyTopology", &BND_Mesh::DestroyTopology)
    .function("destroyTree", &BND_Mesh::DestroyTree)
    .function("destroyPartition", &BND_Mesh::DestroyPartition)
    .function("compact", &BND_Mesh::Compact)
    .function("append", &BND_Mesh::Append)
    .function("createPartitions", &BND_Mesh::CreatePartitions)
    .property("partitionCount", &BND_Mesh::PartitionCount)
    ;
}
#endif
