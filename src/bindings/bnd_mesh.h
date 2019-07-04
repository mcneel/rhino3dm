#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initMeshBindings(pybind11::module& m);
#else
void initMeshBindings(void* m);
#endif

class BND_MeshingParameters
{
  ON_MeshParameters m_mesh_parameters;
public:
  BND_MeshingParameters() = default;
  BND_MeshingParameters(double density) : m_mesh_parameters(density) {}
  BND_MeshingParameters(double density, double minimumEdgeLength) : m_mesh_parameters(density, minimumEdgeLength) {}
  BND_MeshingParameters(const ON_MeshParameters& mp) : m_mesh_parameters(mp) {}

#if defined(ON_PYTHON_COMPILE)
  static BND_MeshingParameters Default(pybind11::object /*self*/) { return BND_MeshingParameters(ON_MeshParameters::DefaultMesh); }
  static BND_MeshingParameters FastRenderMesh(pybind11::object /*self*/) { return BND_MeshingParameters(ON_MeshParameters::FastRenderMesh); }
  static BND_MeshingParameters QualityRenderMesh(pybind11::object /*self*/) { return BND_MeshingParameters(ON_MeshParameters::QualityRenderMesh); }
  static BND_MeshingParameters DefaultAnalysisMesh(pybind11::object /*self*/) { return BND_MeshingParameters(ON_MeshParameters::DefaultAnalysisMesh); }
#else
  static BND_MeshingParameters Default() { return BND_MeshingParameters(ON_MeshParameters::DefaultMesh); }
  static BND_MeshingParameters FastRenderMesh() { return BND_MeshingParameters(ON_MeshParameters::FastRenderMesh); }
  static BND_MeshingParameters QualityRenderMesh() { return BND_MeshingParameters(ON_MeshParameters::QualityRenderMesh); }
  static BND_MeshingParameters DefaultAnalysisMesh() { return BND_MeshingParameters(ON_MeshParameters::DefaultAnalysisMesh); }
#endif

  //public MeshingParameterTextureRange TextureRange | getset
  int GetTextureRange() const { return m_mesh_parameters.TextureRange(); }
  void SetTextureRange(int i) { m_mesh_parameters.SetTextureRange(i); }
  bool GetJaggedSeams() const { return m_mesh_parameters.JaggedSeams(); }
  void SetJaggedSeams(bool b) { m_mesh_parameters.SetJaggedSeams(b); }
  bool GetRefineGrid() const { return m_mesh_parameters.Refine(); }
  void SetRefineGrid(bool b) { m_mesh_parameters.SetRefine(b); }
  bool GetSimplePlanes() const { return m_mesh_parameters.SimplePlanes(); }
  void SetSimplePlanes(bool b) { m_mesh_parameters.SetSimplePlanes(b); }
  bool GetComputeCurvature() const { return m_mesh_parameters.ComputeCurvature(); }
  void SetComputeCurvature(bool b) { m_mesh_parameters.SetComputeCurvature(b); }
  bool GetClosedObjectPostProcess() const { return m_mesh_parameters.ClosedObjectPostProcess(); }
  void SetClosedObjectPostProcess(bool b) { m_mesh_parameters.SetClosedObjectPostProcess(b); }
  int GetGridMinCount() const { return m_mesh_parameters.GridMinCount(); }
  void SetGridMinCount(int val) { m_mesh_parameters.SetGridMinCount(val); }
  int GetGridMaxCount() const { return m_mesh_parameters.GridMaxCount(); }
  void SetGridMaxCount(int val) { m_mesh_parameters.SetGridMaxCount(val); }
  double GetGridAngle() const { return m_mesh_parameters.GridAngleRadians(); }
  void SetGridAngle(double d) { m_mesh_parameters.SetGridAngleRadians(d); }
  double GetGridAspectRatio() const { return m_mesh_parameters.GridAspectRatio(); }
  void SetGridAspectRatio(double d) { m_mesh_parameters.SetGridAspectRatio(d); }
  double GetGridAmplification() const { return m_mesh_parameters.GridAmplification(); }
  void SetGridAmplification(double d) { m_mesh_parameters.SetGridAmplification(d); }
  double GetTolerance() const { return m_mesh_parameters.Tolerance(); }
  void SetTolerance(double d) { m_mesh_parameters.SetTolerance(d); }
  double GetMinimumTolerance() const { return m_mesh_parameters.MinimumTolerance(); }
  void SetMinimumTolerance(double d) { m_mesh_parameters.SetMinimumTolerance(d); }
  double GetRelativeTolerance() const { return m_mesh_parameters.RelativeTolerance(); }
  void SetRelativeTolerance(double d) { m_mesh_parameters.SetRelativeTolerance(d); };
  double GetMinimumEdgeLength() const { return m_mesh_parameters.MinimumEdgeLength(); }
  void SetMinimumEdgeLength(double d) { m_mesh_parameters.SetMinimumEdgeLength(d); }
  double GetMaximumEdgeLength() const { return m_mesh_parameters.MaximumEdgeLength(); }
  void SetMaximumEdgeLength(double d) { m_mesh_parameters.SetMaximumEdgeLength(d); }
  double GetRefineAngle() const { return m_mesh_parameters.RefineAngleRadians(); }
  void SetRefineAngle(double d) { m_mesh_parameters.SetRefineAngleRadians(d); }

#if defined(ON_PYTHON_COMPILE)
  pybind11::dict Encode() const;
  static BND_MeshingParameters* Decode(pybind11::dict jsonObject);
#endif

#if defined(__EMSCRIPTEN__)
  emscripten::val toJSON(emscripten::val key);
  emscripten::val Encode() const;
  static BND_MeshingParameters* Decode(emscripten::val jsonObject);
#endif

};

class BND_MeshVertexList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshVertexList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);

  ON_3fPoint* begin();
  ON_3fPoint* end();

  int Count() const;
  void SetCount(int i);
  ON_3fPoint GetVertex(int i) const;
  void SetVertex(int i, ON_3fPoint pt);
};

class BND_MeshFaceList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshFaceList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);
  int Count() const;
  #if defined(__EMSCRIPTEN__)
  emscripten::val GetFace(int i) const;
  #endif
  #if defined(ON_PYTHON_COMPILE)
  pybind11::list GetFace(int i) const;
  #endif
};

class BND_MeshNormalList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshNormalList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);

  ON_3fVector* begin();
  ON_3fVector* end();

  int Count() const;
  ON_3fVector GetNormal(int i) const;
  void SetNormal(int i, ON_3fVector v);
};

class BND_MeshVertexColorList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshVertexColorList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);
  int Count() const { return m_mesh->m_C.Count(); }
  BND_Color GetColor(int i) const { return ON_Color_to_Binding(m_mesh->m_C[i]); }
  void SetColor(int i, BND_Color color) { m_mesh->m_C[i] = Binding_to_ON_Color(color); }
};


class BND_MeshTextureCoordinateList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshTextureCoordinateList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);

  ON_2fPoint* begin();
  ON_2fPoint* end();

  int Count() const { return m_mesh->m_T.Count(); }
  ON_2fPoint GetTextureCoordinate(int i) const { return m_mesh->m_T[i]; }
  void SetTextureCoordinate(int i, ON_2fPoint tc) { m_mesh->m_T[i] = tc; }
};


class BND_Mesh : public BND_GeometryBase
{
public:
  ON_Mesh* m_mesh = nullptr;
public:
  BND_Mesh();
  BND_Mesh(ON_Mesh* mesh, const ON_ModelComponentReference* compref);

  bool IsClosed() const { return m_mesh->IsClosed(); }
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple IsManifold(bool topologicalTest) const;
#endif
  bool HasCachedTextureCoordinates() const { return m_mesh->HasCachedTextureCoordinates(); }

  BND_MeshVertexList GetVertices();
  //public Collections.MeshTopologyVertexList TopologyVertices
  //public Collections.MeshTopologyEdgeList TopologyEdges
  BND_MeshNormalList GetNormals();
  BND_MeshFaceList GetFaces();
  //public Collections.MeshNgonList Ngons
  //public Collections.MeshFaceNormalList FaceNormals
  BND_MeshVertexColorList VertexColors();
  BND_MeshTextureCoordinateList TextureCoordinates();
  //public Collections.MeshVertexStatusList ComponentStates

  void ClearTextureData();
  void ClearSurfaceData();
  void DestroyTopology() { m_mesh->DestroyTopology(); }
  void DestroyTree() { m_mesh->DestroyTree(); }
  void DestroyPartition() { m_mesh->DestroyPartition(); }
  //public bool EvaluateMeshGeometry(Surface surface)
  //public void SetTextureCoordinates(TextureMapping tm, Transform xf, bool lazy)
  //public void SetCachedTextureCoordinates(TextureMapping tm, ref Transform xf)
  //public CachedTextureCoordinates GetCachedTextureCoordinates(Guid textureMappingId)
  bool Compact() { return m_mesh->Compact(); }
  //void Flip(bool vertexNormals, bool faceNormals, bool faceOrientation);
  //public int SolidOrientation()
  void Append(const BND_Mesh& other) { m_mesh->Append(*other.m_mesh); }
  //public void Append(IEnumerable<Mesh> meshes)
  //public bool[] GetNakedEdgePointStatus()
  bool CreatePartitions(int maximumVertexCount, int maximumTriangleCount) { return m_mesh->CreatePartition(maximumVertexCount, maximumTriangleCount); }
  int PartitionCount() const;
  //public MeshPart GetPartition(int which)
  //public IEnumerable<MeshNgon> GetNgonAndFacesEnumerable()
  //public int GetNgonAndFacesCount()


protected:
  void SetTrackedPointer(ON_Mesh* mesh, const ON_ModelComponentReference* compref);
};
