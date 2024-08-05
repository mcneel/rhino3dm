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

BND_Mesh* BND_Mesh::CreateFromSubDControlNet(class BND_SubD* subd, bool includeTextureCoordinates)
{
  if (subd)
  {
    const ON_SubD* _subd = ON_SubD::Cast(subd->GeometryPointer());
    ON_Mesh* mesh = _subd->GetControlNetMesh(nullptr, includeTextureCoordinates ? ON_SubDGetControlNetMeshPriority::TextureCoordinates : ON_SubDGetControlNetMeshPriority::Geometry);
    if (mesh)
      return new BND_Mesh(mesh, nullptr);
  }
  return nullptr;
}


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

BND_MeshTopologyEdgeList BND_Mesh::GetTopologyEdges()
{
  return BND_MeshTopologyEdgeList(m_mesh, m_component_ref);
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

BND_TUPLE BND_Mesh::IsManifold(bool topologicalTest) const
{
  bool oriented = false;
  bool hasboundary = false;
  BND_TUPLE rc = CreateTuple(3);
  SetTuple<bool>(rc, 0, m_mesh->IsManifold(topologicalTest, &oriented, &hasboundary));
  SetTuple<bool>(rc, 1, oriented);
  SetTuple<bool>(rc, 2, hasboundary);
  return rc;
}

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

void BND_Mesh::SetTextureCoordinates(class BND_TextureMapping* tm, class BND_Transform* xf, bool lazy)
{
  if (tm)
  {
    const ON_Xform* xform = xf ? &(xf->m_xform) : nullptr;
    m_mesh->SetTextureCoordinates(*tm->m_mapping, xform, lazy);
  }
}

int BND_Mesh::PartitionCount() const
{
  const ON_MeshPartition* pPartition = m_mesh->Partition();
  if (pPartition)
    return pPartition->m_part.Count();
  return 0;
}

#if defined(ON_WASM_COMPILE)
BND_DICT BND_Mesh::ToThreejsJSON() const
{
  return ToThreejsJSONRotate(false);
}
BND_DICT BND_Mesh::ToThreejsJSONRotate(bool rotateToYUp) const
{
  ON_Mesh* pMesh = m_mesh;
  ON_Mesh tempMesh;
  if (rotateToYUp)
  {
    tempMesh = *m_mesh;
    ON_Xform rotation(1);
    rotation.RotationZYX(0.0, 0.0, -ON_PI / 2.0);
    tempMesh.Transform(rotation);
    pMesh = &tempMesh;
  }
  
  // build face index array
  emscripten::val index(emscripten::val::object());
  emscripten::val indexList(emscripten::val::array());
  int current = 0;
  for (int i = 0; i < pMesh->m_F.Count(); i++)
  {
    const ON_MeshFace& face = pMesh->m_F[i];
    indexList.set(current++, face.vi[0]);
    indexList.set(current++, face.vi[1]);
    indexList.set(current++, face.vi[2]);
    if (face.vi[2] != face.vi[3])
    {
      indexList.set(current++, face.vi[2]);
      indexList.set(current++, face.vi[3]);
      indexList.set(current++, face.vi[0]);
    }
  }
  if (pMesh->m_V.Count() > 65536) {
    index.set("type", "Uint32Array");
  } else {
    index.set("type", "Uint16Array");
  }
  index.set("array", indexList);

  emscripten::val attributes(emscripten::val::object());

  emscripten::val position(emscripten::val::object());
  position.set("itemSize", 3);
  position.set("type", "Float32Array");
  emscripten::val positionList(emscripten::val::array());
  for (int i = 0; i < pMesh->m_V.Count(); i++)
  {
    positionList.set(i * 3, pMesh->m_V[i].x);
    positionList.set(i * 3+1, pMesh->m_V[i].y);
    positionList.set(i * 3+2, pMesh->m_V[i].z);
  }
  position.set("array", positionList);
  attributes.set("position", position);

  emscripten::val normal(emscripten::val::object());
  normal.set("itemSize", 3);
  normal.set("type", "Float32Array");
  emscripten::val normalList(emscripten::val::array());
  if (pMesh->m_N.Count() == 0)
    pMesh->ComputeVertexNormals();
  for (int i = 0; i < pMesh->m_N.Count(); i++)
  {
    normalList.set(i * 3, pMesh->m_N[i].x);
    normalList.set(i * 3 + 1, pMesh->m_N[i].y);
    normalList.set(i * 3 + 2, pMesh->m_N[i].z);
  }
  normal.set("array", normalList);
  attributes.set("normal", normal);

  if (pMesh->HasTextureCoordinates())
  {
    emscripten::val tcs(emscripten::val::object());
    tcs.set("itemSize", 2);
    tcs.set("type", "Float32Array");
    emscripten::val tcList(emscripten::val::array());
    for (int i = 0; i < pMesh->m_T.Count(); i++)
    {
      tcList.set(i * 2, pMesh->m_T[i].x);
      tcList.set(i * 2 + 1, pMesh->m_T[i].y);
    }
    tcs.set("array", tcList);
    attributes.set("uv", tcs);
  }

  if (pMesh->HasVertexColors())
  {
    emscripten::val vcs(emscripten::val::object());
    vcs.set("itemSize", 3);
    vcs.set("type", "Float32Array");
    emscripten::val vcsList(emscripten::val::array());
    for (int i = 0; i < pMesh->m_C.Count(); i++)
    {
      vcsList.set(i * 3, pMesh->m_C[i].Red() / 255.0);
      vcsList.set(i * 3 + 1, pMesh->m_C[i].Green() / 255.0);
      vcsList.set(i * 3 + 2, pMesh->m_C[i].Blue() / 255.0);
    }
    vcs.set("array", vcsList);
    attributes.set("color",vcs);
  }

  // need data.index and data.attributes
  emscripten::val data(emscripten::val::object());
  data.set("index", index);
  data.set("attributes", attributes);

  emscripten::val rc(emscripten::val::object());
  rc.set("data", data);
  
  return rc;
}
BND_DICT BND_Mesh::ToThreejsJSONMerged(BND_TUPLE meshes, bool rotateYUp)
{
  int length = meshes["length"].as<int>();
  if (1 == length)
  {
    BND_Mesh mesh = meshes[0].as<BND_Mesh>();
    return mesh.ToThreejsJSONRotate(rotateYUp);
  }
  BND_Mesh tempMesh;
  for (int i=0; i<length; i++)
  {
    BND_Mesh mesh = meshes[i].as<BND_Mesh>();
    tempMesh.m_mesh->Append(*(mesh.m_mesh));
  }
  return tempMesh.ToThreejsJSONRotate(rotateYUp);
}



BND_Mesh* BND_Mesh::CreateFromThreejsJSON(BND_DICT data)
{
  if (emscripten::val::undefined() == data["data"])
    return nullptr;
  emscripten::val attributes = data["data"]["attributes"];

  std::vector<int> index_array;
  emscripten::val index = data["data"]["index"];
  if (emscripten::val::undefined() != index && emscripten::val::undefined() != index["array"])
  {
    index_array = emscripten::vecFromJSArray<int>(index["array"]);
  }

  std::vector<float> position_array = emscripten::vecFromJSArray<float>(attributes["position"]["array"]);

  std::vector<float> normal_array;
  if (emscripten::val::undefined() != attributes["normal"])
  {
    normal_array = emscripten::vecFromJSArray<float>(attributes["normal"]["array"]);
  }

  std::vector<float> uv_array;
  if (emscripten::val::undefined() != attributes["uv"])
  {
    uv_array = emscripten::vecFromJSArray<float>(attributes["uv"]["array"]);
  }

  ON_Mesh* mesh = new ON_Mesh();

  bool has_index_array = index_array.size() > 0;

  int face_count = has_index_array ? (int)(index_array.size() / 3) : (int)(position_array.size() / 3);

  mesh->m_F.SetCapacity(face_count);
  mesh->m_F.SetCount(face_count);
  for (int i = 0; i < face_count; i++)
  {
    ON_MeshFace& face = mesh->m_F[i];
    if (has_index_array)
    {
      face.vi[0] = index_array[i * 3];
      face.vi[1] = index_array[i * 3 + 1];
      face.vi[2] = index_array[i * 3 + 2];
    }
    else
    {
      face.vi[0] = i * 3;
      face.vi[1] = i * 3 + 1;
      face.vi[2] = i * 3 + 2;
    }
    face.vi[3] = face.vi[2]; //all triangles
  }

  const int vertex_count = position_array.size() / 3;
  mesh->m_V.SetCapacity(vertex_count);
  mesh->m_V.SetCount(vertex_count);
  memcpy(mesh->m_V.Array(), position_array.data(), sizeof(float) * position_array.size());

  const int normal_count = normal_array.size() / 3;
  mesh->m_N.SetCapacity(normal_count);
  mesh->m_N.SetCount(normal_count);
  memcpy(mesh->m_N.Array(), normal_array.data(), sizeof(float) * normal_array.size());

  const int uv_count = uv_array.size() / 2;
  if (uv_count > 0)
  {
    mesh->m_T.SetCapacity(uv_count);
    mesh->m_T.SetCount(uv_count);
    memcpy(mesh->m_T.Array(), uv_array.data(), sizeof(float) * uv_array.size());
  }

  ON_Xform rotation(1);
  rotation.RotationZYX(0.0, 0.0, ON_PI / 2.0);
  mesh->Transform(rotation);

  return new BND_Mesh(mesh, nullptr);
}

#endif



BND_MeshVertexList::BND_MeshVertexList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}


BND_MeshFaceList::BND_MeshFaceList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
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
#if defined(ON_PYTHON_COMPILE)
  if (i<0 || i>=m_mesh->m_V.Count())
    throw pybind11::index_error();
#endif
  return m_mesh->m_V[i];
}

void BND_MeshVertexList::SetVertex(int i, ON_3fPoint pt)
{
#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= m_mesh->m_V.Count())
    throw pybind11::index_error();
#endif
  m_mesh->m_V[i] = pt;
}

void BND_MeshVertexList::SetUseDoublePrecisionVertices(bool use)
{
  if (use)
  {
    if (!m_mesh->HasDoublePrecisionVertices())
    {
      m_mesh->DoublePrecisionVertices();
    }
  }
  else
  {
    m_mesh->DestroyDoublePrecisionVertices();
  }
}

static void ON_Mesh_RepairHiddenArray(ON_Mesh* pMesh)
{
  if (!pMesh)
    return;

  int v_count = pMesh->m_V.Count();
  int h_count = pMesh->m_H.Count();

  // No hidden flags equals a valid mesh.
  // An equal amount of vertices and hidden flags equal a valid mesh.
  if (0 == h_count || v_count == h_count)
    return;

  if (h_count > v_count)
  {
    // Remove the trailing hidden flags.
    pMesh->m_H.SetCount(v_count);
  }
  else
  {
    // Add new hidden flags to account for unhandled vertices.
    int count_to_add = v_count - h_count;
    pMesh->m_H.SetCapacity(v_count);
    for (int i = 0; i < count_to_add; i++)
    {
      pMesh->m_H.Append(false);
    }
  }
}

void BND_MeshVertexList::Clear()
{
  m_mesh->m_V.SetCount(0);
  ON_Mesh_RepairHiddenArray(m_mesh);
}

void BND_MeshVertexList::Destroy()
{
  const bool hasDoublePrecisionVerts = m_mesh->HasDoublePrecisionVertices();
  m_mesh->m_V.SetCapacity(0);
  if (hasDoublePrecisionVerts)
    m_mesh->DoublePrecisionVertices().SetCapacity(0);
}

int BND_MeshVertexList::Add(float x, float y, float z)
{
  m_mesh->SetVertex(m_mesh->VertexCount(), ON_3fPoint(x, y, z));
  return m_mesh->VertexCount() - 1;
}

bool BND_MeshVertexList::IsHidden(int index) const
{
  bool rc = false;
  if (m_mesh && index >= 0 && index < m_mesh->m_H.Count())
  {
    rc = m_mesh->m_H[index];
  }
  return rc;
}

enum MeshHiddenVertexOpConst : int
{
  mhvoHideVertex = 0,
  mhvoShowVertex = 1,
  mhvoHideAll = 2,
  mhvoShowAll = 3,
  mhvoEnsureHiddenList = 4,
  mhvoCleanHiddenList = 5
};

static void ON_Mesh_HiddenVertexOp(ON_Mesh* pMesh, int index, MeshHiddenVertexOpConst op)
{
  if (!pMesh)
    return;

  if (mhvoHideVertex == op || mhvoShowVertex == op)
  {
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-51975
    // Show (false) or hide (true) the vertex at [index]
    bool hide = (mhvoHideVertex == op);
    if (index >= 0 && index < pMesh->m_H.Count())
      pMesh->m_H[index] = hide;
  }
  else if (mhvoHideAll == op || mhvoShowAll == op)
  {
    // Show (false) or hide (true) all vertices
    bool hide = (mhvoHideAll == op);
    int count = pMesh->m_H.Count();
    for (int i = 0; i < count; i++)
      pMesh->m_H[i] = hide;
  }
  else if (mhvoEnsureHiddenList == op)
  {
    // Make sure the m_H array contains the same amount of entries as the m_V array. 
    // This function leaves the contents of m_H untouched, so they will be garbage when 
    // the function grew the m_H array.
    int count = pMesh->m_V.Count();
    if (pMesh->m_H.Count() != count)
    {
      pMesh->m_H.SetCapacity(count);
      pMesh->m_H.SetCount(count);
    }
  }
  else if (mhvoCleanHiddenList == op)
  {
    // If the m_H array contains only false values, erase it.
    int count = pMesh->m_H.Count();
    if (count > 0)
    {
      bool clean = true;
      for (int i = 0; i < count; i++)
      {
        if (pMesh->m_H[i])
        {
          clean = false;
          break;
        }
      }

      if (clean)
        pMesh->m_H.SetCount(0);
    }
  }
}

void BND_MeshVertexList::Hide(int index)
{
  ON_Mesh_HiddenVertexOp(m_mesh, index, MeshHiddenVertexOpConst::mhvoHideVertex);
}

void BND_MeshVertexList::Show(int index)
{
  ON_Mesh_HiddenVertexOp(m_mesh, index, MeshHiddenVertexOpConst::mhvoShowVertex);
}

void BND_MeshVertexList::HideAll()
{
  ON_Mesh_HiddenVertexOp(m_mesh, 0, MeshHiddenVertexOpConst::mhvoHideAll);
}

void BND_MeshVertexList::ShowAll()
{
  ON_Mesh_HiddenVertexOp(m_mesh, 0, MeshHiddenVertexOpConst::mhvoShowAll);
}

void BND_MeshNormalList::Clear()
{
  m_mesh->m_N.SetCount(0);
  ON_Mesh_RepairHiddenArray(m_mesh);
}

void BND_MeshNormalList::Destroy()
{
  m_mesh->m_N.SetCapacity(0);
}

int BND_MeshNormalList::Add(float x, float y, float z)
{
  int index = m_mesh->m_N.Count();
  m_mesh->SetVertexNormal(index, ON_3fVector(x, y, z));
  return index;
}


int BND_MeshFaceList::AddFace2(int vertex1, int vertex2, int vertex3, int vertex4)
{
  int rc = -1;
  int faceIndex = m_mesh->m_F.Count();
  if (m_mesh->SetQuad(faceIndex, vertex1, vertex2, vertex3, vertex4))
    rc = faceIndex;
  m_mesh->DestroyRuntimeCache();
  return rc;
}

bool BND_MeshFaceList::SetFace2(int index, int vertex1, int vertex2, int vertex3, int vertex4)
{
  bool rc = m_mesh->SetQuad(index, vertex1, vertex2, vertex3, vertex4);
  m_mesh->DestroyRuntimeCache();
  return rc;
}

bool BND_MeshFaceList::HasNakedEdges(int index)
{
  bool rc = false;
  const ON_MeshTopology& topology = m_mesh->Topology();
  const ON_MeshTopologyFace* face_top = topology.m_topf.At(index);
  if (face_top)
  {
    for (int i = 0; i < 4; i++)
    {
      int edge = face_top->m_topei[i];
      if (topology.m_tope[edge].m_topf_count == 1)
      {
        rc = true;
        break;
      }
    }
  }
  return rc;
}

BND_TUPLE BND_MeshFaceList::GetFace(int i) const
{
#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= m_mesh->m_F.Count())
    throw pybind11::index_error();
#endif

  ON_MeshFace& face = m_mesh->m_F[i];
  BND_TUPLE rc = CreateTuple(4);
  for (int i = 0; i < 4; i++)
    SetTuple<int>(rc, i, face.vi[i]);
  return rc;
}

BND_TUPLE BND_MeshFaceList::GetFaceVertices(int faceIndex) const
{
  int count = m_mesh->m_F.Count();
  if(faceIndex >= 0 && faceIndex < count)
  {
    ON_MeshFace& face = m_mesh->m_F[faceIndex];
    BND_TUPLE rc = CreateTuple(5);
    SetTuple(rc, 0, true);
    for (int i = 0; i < 4; i++)
      SetTuple(rc, i+1, m_mesh->m_V[ face.vi[i] ]);
    return rc;
  }
  return NullTuple();
}

ON_3dPoint BND_MeshFaceList::GetFaceCenter(int faceIndex) const
{
  int count = m_mesh->m_F.Count();
  if(faceIndex >= 0 && faceIndex < count)
  {
    ON_3dPoint center;
    ON_MeshFace& face = m_mesh->m_F[faceIndex];
    if (face.IsQuad())
      center = 0.25 * (m_mesh->m_V[face.vi[0]] + m_mesh->m_V[face.vi[1]] + m_mesh->m_V[face.vi[2]] + m_mesh->m_V[face.vi[3]]);
    else
      center = (1.0 / 3.0) * (m_mesh->m_V[face.vi[0]] + m_mesh->m_V[face.vi[1]] + m_mesh->m_V[face.vi[2]]);

    return center;
  }
  return ON_3dPoint::UnsetPoint;
}

BND_MeshTopologyEdgeList::BND_MeshTopologyEdgeList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_Line BND_MeshTopologyEdgeList::EdgeLine(int topologyEdgeIndex) const
{
  const ON_MeshTopology& top = m_mesh->Topology();
  const ON_MeshTopologyEdge& edge = top.m_tope[topologyEdgeIndex];
  const ON_MeshTopologyVertex& v0 = top.m_topv[edge.m_topvi[0]];
  const ON_MeshTopologyVertex& v1 = top.m_topv[edge.m_topvi[1]];
  ON_Line rc;
  rc.from = m_mesh->m_V[v0.m_vi[0]];
  rc.to = m_mesh->m_V[v1.m_vi[0]];
  return rc;
}


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

BND_Color BND_MeshVertexColorList::GetColor(int index) const
{
#if defined(ON_PYTHON_COMPILE)
  if (index < 0 || index >= m_mesh->m_C.Count())
    throw pybind11::index_error();
#endif
  return ON_Color_to_Binding(m_mesh->m_C[index]);
}

void BND_MeshVertexColorList::SetColor(int index, BND_Color color)
{
#if defined(ON_PYTHON_COMPILE)
  if (index < 0 || index >= m_mesh->m_C.Count())
    throw pybind11::index_error();
#endif

  // if index == count, then we are appending
  if (index >= 0)
  {
    if (index < m_mesh->m_C.Count())
      m_mesh->m_C[index] = Binding_to_ON_Color(color);
    else if (index == m_mesh->m_C.Count())
      m_mesh->m_C.Append(Binding_to_ON_Color(color));
  }
}


int BND_MeshNormalList::Count() const
{
  return m_mesh->m_N.Count();
}

ON_3fVector BND_MeshNormalList::GetNormal(int i) const
{
#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= m_mesh->m_N.Count())
    throw pybind11::index_error();
#endif

  return m_mesh->m_N[i];
}

void BND_MeshNormalList::SetNormal(int i, ON_3fVector v)
{
#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= m_mesh->m_N.Count())
    throw pybind11::index_error();
#endif
  m_mesh->m_N[i] = v;
}

BND_MeshTextureCoordinateList::BND_MeshTextureCoordinateList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_2fPoint BND_MeshTextureCoordinateList::GetTextureCoordinate(int i) const
{
#if defined(ON_PYTHON_COMPILE)
  if (i<0 || i >= m_mesh->m_T.Count())
    throw pybind11::index_error();
#endif
  return m_mesh->m_T[i];
}

void BND_MeshTextureCoordinateList::SetTextureCoordinate(int i, ON_2fPoint tc)
{
#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= m_mesh->m_T.Count())
    throw pybind11::index_error();
#endif
  m_mesh->m_T[i] = tc;
}


int BND_MeshTextureCoordinateList::Add(float s, float t)
{
  m_mesh->SetTextureCoord(m_mesh->m_T.Count(), s, t);
  return m_mesh->m_T.Count() - 1;
}

BND_CachedTextureCoordinates::BND_CachedTextureCoordinates(const ON_TextureCoordinates& tc) : m_ctc(tc){}

BND_TUPLE BND_CachedTextureCoordinates::TryGetAt(int index) const
{

  if ( index < 0 || index >= m_ctc.m_T.Count() ) return NullTuple();

  bool success = false;
  double u = 0;
  double v = 0;
  double w = 0;
  
  u = m_ctc.m_T[index].x;
  v = m_ctc.m_T[index].y;
  w = m_ctc.m_T[index].z;

  success = m_ctc.m_dim > 0 ? true : false;

  BND_TUPLE rc = CreateTuple(4);
  SetTuple<bool>(rc, 0, success);
  SetTuple<double>(rc, 1, u);
  SetTuple<double>(rc, 2, v);
  SetTuple<double>(rc, 3, w);
  return rc;

}

int BND_CachedTextureCoordinates::IndexOf(double x, double y, double z) const
{

  int count = m_ctc.m_T.Count();

  for (int i = 0; i < count; i++)
  {
    double u, v, w;
    u = m_ctc.m_T[i].x;
    v = m_ctc.m_T[i].y;
    w = m_ctc.m_T[i].z;
    bool success = m_ctc.m_dim > 0? true : false;

    if (success && u == x && v == y && ( m_ctc.m_dim < 3 || w == z))
      return i;

  }

  return -1;
  
}

bool BND_CachedTextureCoordinates::Contains(double x, double y, double z) const
{

  int index = IndexOf(x, y, z);
  return (index >= 0);

}

BND_CachedTextureCoordinates BND_Mesh::GetCachedTextureCoordinates( BND_UUID mappingId )
{
  const ON_TextureCoordinates* tc = m_mesh->CachedTextureCoordinates( Binding_to_ON_UUID(mappingId) );
  return BND_CachedTextureCoordinates(*tc);
}

void BND_Mesh::SetCachedTextureCoordinates(class BND_TextureMapping* tm, class BND_Transform* xf)
{
  if (tm)
  {
    const ON_Xform* xform = xf ? &(xf->m_xform) : nullptr;
    m_mesh->SetCachedTextureCoordinates(*tm->m_mapping, xform, false);
  }
}



#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initMeshBindings(py::module_& m){}
#else
namespace py = pybind11;
void initMeshBindings(py::module& m)
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
    .def_property("UseDoublePrecisionVertices", &BND_MeshVertexList::UseDoublePrecisionVertices, &BND_MeshVertexList::SetUseDoublePrecisionVertices)
    .def("Clear", &BND_MeshVertexList::Clear)
    .def("Destroy", &BND_MeshVertexList::Destroy)
    .def("Add", &BND_MeshVertexList::Add, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("IsHidden", &BND_MeshVertexList::IsHidden, py::arg("vertexIndex"))
    .def("Hide", &BND_MeshVertexList::Hide, py::arg("vertexIndex"))
    .def("Show", &BND_MeshVertexList::Show, py::arg("vertexIndex"))
    .def("HideAll", &BND_MeshVertexList::HideAll)
    .def("ShowAll", &BND_MeshVertexList::ShowAll)
    .def("CullUnused", &BND_MeshVertexList::CullUnused)
    .def("CombineIdentical", &BND_MeshVertexList::CombineIdentical)
    ;

  py::class_<BND_MeshTopologyEdgeList>(m, "MeshTopologyEdgeList")
    .def("__len__", &BND_MeshTopologyEdgeList::Count)
    .def("EdgeLine", &BND_MeshTopologyEdgeList::EdgeLine, py::arg("topologyEdgeIndex"))
    ;

  py::class_<BND_MeshFaceList>(m, "MeshFaceList")
    .def("__len__", &BND_MeshFaceList::Count)
    .def("__getitem__", &BND_MeshFaceList::GetFace)
    .def("GetFaceVertices", &BND_MeshFaceList::GetFaceVertices, py::arg("faceIndex"))
    .def("GetFaceCenter", &BND_MeshFaceList::GetFaceCenter, py::arg("faceIndex"))
    .def_property("Count", &BND_MeshFaceList::Count, &BND_MeshFaceList::SetCount)
    .def_property_readonly("QuadCount", &BND_MeshFaceList::QuadCount)
    .def_property_readonly("TriangleCount", &BND_MeshFaceList::TriangleCount)
    .def_property("Capacity", &BND_MeshFaceList::Capacity, &BND_MeshFaceList::SetCapacity)
    .def("Clear", &BND_MeshFaceList::Clear)
    .def("Destroy", &BND_MeshFaceList::Destroy)
    .def("AddFace", &BND_MeshFaceList::AddFace, py::arg("vertex1"), py::arg("vertex2"), py::arg("vertex3"))
    .def("AddFace", &BND_MeshFaceList::AddFace2, py::arg("vertex1"), py::arg("vertex2"), py::arg("vertex3"), py::arg("vertex4"))
    .def("SetFace", &BND_MeshFaceList::SetFace, py::arg("index"), py::arg("vertex1"), py::arg("vertex2"), py::arg("vertex3"))
    .def("SetFace", &BND_MeshFaceList::SetFace2, py::arg("index"), py::arg("vertex1"), py::arg("vertex2"), py::arg("vertex3"), py::arg("vertex4"))
    .def("ConvertQuadsToTriangles", &BND_MeshFaceList::ConvertQuadsToTriangles)
    .def("ConvertNonPlanarQuadsToTriangles", &BND_MeshFaceList::ConvertNonPlanarQuadsToTriangles)
    .def("ConvertTrianglesToQuads", &BND_MeshFaceList::ConvertTrianglesToQuads)
    .def("CullDegenerateFaces", &BND_MeshFaceList::CullDegenerateFaces)
    .def("IsHidden", &BND_MeshFaceList::IsHidden)
    .def("HasNakedEdges", &BND_MeshFaceList::HasNakedEdges)
    ;

  py::class_<BND_MeshNormalList>(m, "MeshNormalList")
    .def("__len__", &BND_MeshNormalList::Count)
    .def("__getitem__", &BND_MeshNormalList::GetNormal)
    .def("__setitem__", &BND_MeshNormalList::SetNormal)
    .def("Clear", &BND_MeshNormalList::Clear)
    .def("Destroy", &BND_MeshNormalList::Destroy)
    .def("Add", &BND_MeshNormalList::Add, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("ComputeNormals", &BND_MeshNormalList::ComputeNormals)
    .def("UnitizeNormals", &BND_MeshNormalList::UnitizeNormals)
    .def("Flip", &BND_MeshNormalList::Flip)
    ;

  py::class_<BND_MeshVertexColorList>(m, "MeshVertexColorList")
    .def("__len__", &BND_MeshVertexColorList::Count)
    .def("__getitem__", &BND_MeshVertexColorList::GetColor)
    .def("__setitem__", &BND_MeshVertexColorList::SetColor)
    .def_property("Count", &BND_MeshVertexColorList::Count, &BND_MeshVertexColorList::SetCount)
    .def("Clear", &BND_MeshVertexColorList::Clear)
    .def("Add", &BND_MeshVertexColorList::Add, py::arg("red"), py::arg("green"), py::arg("blue"))
    .def_property("Capacity", &BND_MeshVertexColorList::Capacity, &BND_MeshVertexColorList::SetCapacity)
    ;

  py::class_<BND_MeshTextureCoordinateList>(m, "MeshTextureCoordinateList")
    .def("__len__", &BND_MeshTextureCoordinateList::Count)
    .def("__getitem__", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .def("__setitem__", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    .def("__add__", &BND_MeshTextureCoordinateList::Add);

  py::class_<BND_CachedTextureCoordinates>(m, "CachedTextureCoordinates")
    .def("__len__", &BND_CachedTextureCoordinates::Count)
    .def_property_readonly("Dimension", &BND_CachedTextureCoordinates::Dim)
    .def_property_readonly("MappingId", &BND_CachedTextureCoordinates::MappingId)
    .def_property_readonly("IsReadOnly", &BND_CachedTextureCoordinates::IsReadOnly)
    .def("TryGetAt", &BND_CachedTextureCoordinates::TryGetAt, py::arg("index"))
    .def("IndexOf", &BND_CachedTextureCoordinates::IndexOf, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("Contains", &BND_CachedTextureCoordinates::Contains, py::arg("x"), py::arg("y"), py::arg("z"))
    ;

  py::class_<BND_Mesh, BND_GeometryBase>(m, "Mesh")
    .def(py::init<>())
    .def_static("CreateFromSubDControlNet", &BND_Mesh::CreateFromSubDControlNet, py::arg("subd"), py::arg("includeTextureCoordinates"))
    .def_property_readonly("IsClosed", &BND_Mesh::IsClosed)
    .def("IsManifold", &BND_Mesh::IsManifold, py::arg("topologicalTest"))
    .def_property_readonly("HasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .def_property_readonly("HasPrincipalCurvatures", &BND_Mesh::HasPrincipalCurvatures)
    .def_property_readonly("Vertices", &BND_Mesh::GetVertices)
    .def_property_readonly("TopologyEdges", &BND_Mesh::GetTopologyEdges)
    .def_property_readonly("Faces", &BND_Mesh::GetFaces)
    .def_property_readonly("Normals", &BND_Mesh::GetNormals)
    .def_property_readonly("VertexColors", &BND_Mesh::VertexColors)
    .def_property_readonly("TextureCoordinates", &BND_Mesh::TextureCoordinates)
    .def("ClearTextureData", &BND_Mesh::ClearTextureData)
    .def("ClearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .def("DestroyTopology", &BND_Mesh::DestroyTopology)
    .def("DestroyTree", &BND_Mesh::DestroyTree)
    .def("DestroyPartition", &BND_Mesh::DestroyPartition)
    .def("SetTextureCoordinates", &BND_Mesh::SetTextureCoordinates, py::arg("tm"), py::arg("xf"), py::arg("lazy"))
    .def("SetCachedTextureCoordinates", &BND_Mesh::SetCachedTextureCoordinates, py::arg("tm"), py::arg("xf"))
    .def("GetCachedTextureCoordinates", &BND_Mesh::GetCachedTextureCoordinates, py::arg("id"))
    .def("Compact", &BND_Mesh::Compact)
    .def("Append", &BND_Mesh::Append, py::arg("other"))
    .def("CreatePartitions", &BND_Mesh::CreatePartitions, py::arg("maximumVertexCount"), py::arg("maximumTriangleCount"))
    .def_property_readonly("PartitionCount", &BND_Mesh::PartitionCount)
    ;
}
#endif
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
    .property("useDoublePrecisionVertices", &BND_MeshVertexList::UseDoublePrecisionVertices, &BND_MeshVertexList::SetUseDoublePrecisionVertices)
    .function("clear", &BND_MeshVertexList::Clear)
    .function("destroy", &BND_MeshVertexList::Destroy)
    .function("add", &BND_MeshVertexList::Add)
    .function("isHidden", &BND_MeshVertexList::IsHidden)
    .function("hide", &BND_MeshVertexList::Hide)
    .function("show", &BND_MeshVertexList::Show)
    .function("hideAll", &BND_MeshVertexList::HideAll)
    .function("showAll", &BND_MeshVertexList::ShowAll)
    .function("cullUnused", &BND_MeshVertexList::CullUnused)
    .function("combineIdentical", &BND_MeshVertexList::CombineIdentical)
    ;

  class_<BND_MeshTopologyEdgeList>("MeshTopologyEdgeList")
    .property("count", &BND_MeshTopologyEdgeList::Count)
    .function("edgeLine", &BND_MeshTopologyEdgeList::EdgeLine)
    ;

  class_<BND_MeshFaceList>("MeshFaceList")
    .property("count", &BND_MeshFaceList::Count, &BND_MeshFaceList::SetCount)
    .function("get", &BND_MeshFaceList::GetFace)
    .function("getFaceVertices", &BND_MeshFaceList::GetFaceVertices)
    .function("getFaceCenter", &BND_MeshFaceList::GetFaceCenter)
    .property("quadCount", &BND_MeshFaceList::QuadCount)
    .property("triangleCount", &BND_MeshFaceList::TriangleCount)
    .property("capacity", &BND_MeshFaceList::Capacity, &BND_MeshFaceList::SetCapacity)
    .function("clear", &BND_MeshFaceList::Clear)
    .function("destroy", &BND_MeshFaceList::Destroy)
    .function("addTriFace", &BND_MeshFaceList::AddFace)
    .function("addQuadFace", &BND_MeshFaceList::AddFace2)
    .function("setTriFace", &BND_MeshFaceList::SetFace)
    .function("setQuadFace", &BND_MeshFaceList::SetFace2)
    .function("convertQuadsToTriangles", &BND_MeshFaceList::ConvertQuadsToTriangles)
    .function("convertNonPlanarQuadsToTriangles", &BND_MeshFaceList::ConvertNonPlanarQuadsToTriangles)
    .function("convertTrianglesToQuads", &BND_MeshFaceList::ConvertTrianglesToQuads)
    .function("cullDegenerateFaces", &BND_MeshFaceList::CullDegenerateFaces)
    .function("isHidden", &BND_MeshFaceList::IsHidden)
    .function("hasNakedEdges", &BND_MeshFaceList::HasNakedEdges)
    ;

  class_<BND_MeshNormalList>("MeshNormalList")
    .property("count", &BND_MeshNormalList::Count)
    .function("get", &BND_MeshNormalList::GetNormal)
    .function("set", &BND_MeshNormalList::SetNormal)
    .function("clear", &BND_MeshNormalList::Clear)
    .function("destroy", &BND_MeshNormalList::Destroy)
    .function("add", &BND_MeshNormalList::Add)
    .function("computeNormals", &BND_MeshNormalList::ComputeNormals)
    .function("unitizeNormals", &BND_MeshNormalList::UnitizeNormals)
    .function("flip", &BND_MeshNormalList::Flip)
    ;

  class_<BND_MeshVertexColorList>("MeshVertexColorList")
    .property("count", &BND_MeshVertexColorList::Count, &BND_MeshVertexColorList::SetCount)
    .function("get", &BND_MeshVertexColorList::GetColor)
    .function("set", &BND_MeshVertexColorList::SetColor)
    .function("clear", &BND_MeshVertexColorList::Clear)
    .function("add", &BND_MeshVertexColorList::Add)
    .property("capacity", &BND_MeshVertexColorList::Capacity, &BND_MeshVertexColorList::SetCapacity)
    ;

  class_<BND_MeshTextureCoordinateList>("MeshTextureCoordinateList")
    .property("count", &BND_MeshTextureCoordinateList::Count)
    .function("get", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .function("set", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    .function("add", &BND_MeshTextureCoordinateList::Add)
    ;

  class_<BND_CachedTextureCoordinates>("CachedTextureCoordinates")
    .property("count", &BND_CachedTextureCoordinates::Count)
    .property("dimension", &BND_CachedTextureCoordinates::Dim)
    .property("mappingId", &BND_CachedTextureCoordinates::MappingId)
    .property("isReadOnly", &BND_CachedTextureCoordinates::IsReadOnly)
    .function("tryGetAt", &BND_CachedTextureCoordinates::TryGetAt)
    .function("indexOf", &BND_CachedTextureCoordinates::IndexOf)
    .function("contains", &BND_CachedTextureCoordinates::Contains)
    ;

  class_<BND_Mesh, base<BND_GeometryBase>>("Mesh")
    .constructor<>()
    .class_function("createFromSubDControlNet", &BND_Mesh::CreateFromSubDControlNet, allow_raw_pointers())
    .class_function("toThreejsJSONMerged", &BND_Mesh::ToThreejsJSONMerged)
    .property("isClosed", &BND_Mesh::IsClosed)
    .function("isManifold", &BND_Mesh::IsManifold)
    .property("hasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .property("hasPrincipalCurvatures", &BND_Mesh::HasPrincipalCurvatures)
    .function("vertices", &BND_Mesh::GetVertices)
    .function("topologyEdges", &BND_Mesh::GetTopologyEdges)
    .function("faces", &BND_Mesh::GetFaces)
    .function("normals", &BND_Mesh::GetNormals)
    .function("vertexColors", &BND_Mesh::VertexColors)
    .function("textureCoordinates", &BND_Mesh::TextureCoordinates)
    .function("clearTextureData", &BND_Mesh::ClearTextureData)
    .function("clearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .function("destroyTopology", &BND_Mesh::DestroyTopology)
    .function("destroyTree", &BND_Mesh::DestroyTree)
    .function("destroyPartition", &BND_Mesh::DestroyPartition)
    .function("setTextureCoordinates", &BND_Mesh::SetTextureCoordinates, allow_raw_pointers())
    .function("setCachedTextureCoordinates", &BND_Mesh::SetCachedTextureCoordinates, allow_raw_pointers())
    .function("getCachedTextureCoordinates", &BND_Mesh::GetCachedTextureCoordinates)
    .function("compact", &BND_Mesh::Compact)
    .function("append", &BND_Mesh::Append)
    .function("createPartitions", &BND_Mesh::CreatePartitions)
    .property("partitionCount", &BND_Mesh::PartitionCount)
    .function("toThreejsJSON", &BND_Mesh::ToThreejsJSON)
    .function("toThreejsJSONRotate", &BND_Mesh::ToThreejsJSONRotate)
    .class_function("createFromThreejsJSON", &BND_Mesh::CreateFromThreejsJSON, allow_raw_pointers())
    ;
}
#endif
