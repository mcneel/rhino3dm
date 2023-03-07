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

  BND_DICT Encode() const;
  static BND_MeshingParameters* Decode(BND_DICT jsonObject);

#if defined(__EMSCRIPTEN__)
  emscripten::val toJSON(emscripten::val key);
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

  int Count() const { return m_mesh->VertexCount(); }
  void SetCount(int i);
  //    public int Capacity get;set;
  ON_3fPoint GetVertex(int i) const;
  void SetVertex(int i, ON_3fPoint pt);
  bool UseDoublePrecisionVertices() const { return m_mesh->HasDoublePrecisionVertices(); }
  void SetUseDoublePrecisionVertices(bool b);
  void Clear();
  void Destroy();
  int Add(float x, float y, float z);
  //    public int Add(double x, double y, double z)
  //    public int Add(Point3f vertex)
  //    public int Add(Point3d vertex)
  //    public void AddVertices(IEnumerable<Point3d> vertices)
  //    public void AddVertices(IEnumerable<Point3f> vertices)
  //bool SetVertex(int index, float x, float y, float z) { return m_mesh->SetVertex(index, ON_3fPoint(x, y, z)); }
  //    public bool SetVertex(int index, double x, double y, double z, bool updateNormals)
  //    public bool SetVertex(int index, double x, double y, double z)
  //    public bool SetVertex(int index, Point3f vertex)
  //    public bool SetVertex(int index, Point3d vertex)
  bool IsHidden(int vertexIndex) const;
  void Hide(int vertexIndex);
  void Show(int vertexIndex);
  void HideAll();
  void ShowAll();
  int CullUnused() { return m_mesh->CullUnusedVertices(); }
  bool CombineIdentical(bool ignoreNormals, bool ignoreAdditional) { return m_mesh->CombineIdenticalVertices(ignoreNormals, ignoreAdditional); }
  //    public int[] GetVertexFaces(int vertexIndex)
  //    public int[] GetTopologicalIndenticalVertices(int vertexIndex)
  //    public int[] GetConnectedVertices(int vertexIndex)
  //    public Point3d Point3dAt(int index)
  //    public Point3f[] ToPoint3fArray()
  //    public Point3d[] ToPoint3dArray()
  //    public float[] ToFloatArray()
  //    public bool Remove(int index, bool shrinkFaces)
  //    public bool Remove(IEnumerable<int> indices, bool shrinkFaces)
};

class BND_MeshTopologyEdgeList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshTopologyEdgeList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);

  int Count() const { return m_mesh->Topology().m_tope.Count(); }
  //public IndexPair GetTopologyVertices(int topologyEdgeIndex)
  //public int[] GetConnectedFaces(int topologyEdgeIndex)
  //public int[] GetConnectedFaces(int topologyEdgeIndex, out bool[] faceOrientationMatchesEdgeDirection)
  //public int[] GetEdgesForFace(int faceIndex)
  //public int[] GetEdgesForFace(int faceIndex, out bool[] sameOrientation)
  //public int GetEdgeIndex(int topologyVertex1, int topologyVertex2)
  ON_Line EdgeLine(int topologyEdgeIndex) const;
  //public bool CollapseEdge(int topologyEdgeIndex)
  //public bool IsSwappableEdge(int topologyEdgeIndex)
  //public bool SwapEdge(int topologyEdgeIndex)
  //public bool IsHidden(int topologyEdgeIndex)

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
  // SetCount
  //    public int Capacity get;set;
  ON_3fVector GetNormal(int i) const;
  void SetNormal(int i, ON_3fVector v);
  void Clear();
  void Destroy();
  int Add(float x, float y, float z);
  //    public int Add(double x, double y, double z)
  //    public int Add(Vector3f normal)
  //    public int Add(Vector3d normal)
  //    public bool AddRange(Vector3f[] normals)
  //    public bool SetNormal(int index, float x, float y, float z)
  //    public bool SetNormal(int index, double x, double y, double z)
  //    public bool SetNormal(int index, Vector3f normal)
  //    public bool SetNormal(int index, Vector3d normal)
  //    public bool SetNormals(Vector3f[] normals)
  //    public float[] ToFloatArray()
  bool ComputeNormals() { return m_mesh->ComputeVertexNormals(); }
  bool UnitizeNormals() { return m_mesh->UnitizeVertexNormals(); }
  void Flip() { m_mesh->FlipVertexNormals(); }
};

class BND_MeshFaceList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshFaceList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);
  int Count() const { return m_mesh->FaceCount(); }
  void SetCount(int value) {
    m_mesh->m_F.Reserve(value);
    m_mesh->m_F.SetCount(value);
  }
  int QuadCount() const { return m_mesh->QuadCount(); }
  int TriangleCount() const { return m_mesh->TriangleCount(); }
  //int VertexCount() const { return m_mesh->VertexCount(); }
  int Capacity() const { return m_mesh->m_F.Capacity(); }
  void SetCapacity(int c) { m_mesh->m_F.SetCapacity(c); }
  void Clear() { m_mesh->m_F.SetCount(0); }
  void Destroy() { m_mesh->m_F.SetCapacity(0); }
  //    public int AddFace(MeshFace face)
  int AddFace(int vertex1, int vertex2, int vertex3) { return AddFace2(vertex1, vertex2, vertex3, vertex3); }
  int AddFace2(int vertex1, int vertex2, int vertex3, int vertex4);
  //    public int[] AddFaces(IEnumerable<MeshFace> faces)
  //    public void Insert(int index, MeshFace face)
  //    public bool SetFace(int index, MeshFace face)
  bool SetFace(int index, int vertex1, int vertex2, int vertex3) { return SetFace2(index, vertex1, vertex2, vertex3, vertex3); }
  bool SetFace2(int index, int vertex1, int vertex2, int vertex3, int vertex4);
  BND_TUPLE GetFace(int i) const;
  //    public MeshFace this[int index]
  BND_TUPLE GetFaceVertices(int faceIndex) const;
//    public BoundingBox GetFaceBoundingBox(int faceIndex)
  ON_3dPoint GetFaceCenter(int faceIndex) const;
//    public int[] AdjacentFaces(int faceIndex)
//    BND_TUPLE ToIntArray(bool asTriangles) const;
//    public int[] ToIntArray(bool asTriangles)
//    public int[] ToIntArray(bool asTriangles, ref List<int> replacedIndices)


  //int DeleteFaces(IEnumerable<int> faceIndexes)
  //int DeleteFaces(IEnumerable<int> faceIndexes, bool compact)
  void RemoveAt(int index) { m_mesh->m_F.Remove(index); }
  //public void RemoveAt(int index, bool compact)
  bool ConvertQuadsToTriangles() { return m_mesh->ConvertQuadsToTriangles(); }
  int ConvertNonPlanarQuadsToTriangles(double planarTolerance, double angleToleranceRadians, int splitMethod) {
    return m_mesh->ConvertNonPlanarQuadsToTriangles(planarTolerance, angleToleranceRadians, splitMethod);
  }
  bool ConvertTrianglesToQuads(double angleToleranceRadians, double minimumDiagonalLengthRatio) {
    return m_mesh->ConvertTrianglesToQuads(angleToleranceRadians, minimumDiagonalLengthRatio);
  }
  int CullDegenerateFaces() { return m_mesh->CullDegenerateFaces(); }
  bool IsHidden(int faceIndex) { return m_mesh->FaceIsHidden(faceIndex); }
  bool HasNakedEdges(int faceIndex);
  //int[] GetTopologicalVertices(int faceIndex)
};

class BND_MeshVertexColorList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshVertexColorList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);
  int Count() const { return m_mesh->m_C.Count(); }
  void SetCount(int c) { m_mesh->m_C.SetCount(c); }
  BND_Color GetColor(int i) const;
  void SetColor(int index, BND_Color color);
  //    public int[] ToARGBArray()
  void Clear() { m_mesh->m_C.SetCount(0); }
  int Add(int red, int green, int blue) {
    m_mesh->m_C.Append(ON_Color(red, green, blue));
    return m_mesh->m_C.Count() - 1;
  }
  //    public int Add(Color color)
  //    public bool SetColor(MeshFace face, Color color)
  int Capacity() const { return m_mesh->m_C.Capacity(); }
  void SetCapacity(int c) { m_mesh->m_C.SetCapacity(c); }
  //    public bool CreateMonotoneMesh(Color baseColor)
  //    public bool SetColors(Color[] colors)
  //    public bool AppendColors(Color[] colors)
  //    public void Destroy()
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
  ON_2fPoint GetTextureCoordinate(int i) const;
  void SetTextureCoordinate(int i, ON_2fPoint tc);
  int Add(float s, float t);
};

class BND_Mesh : public BND_GeometryBase
{
public:
  ON_Mesh* m_mesh = nullptr;
public:
  static BND_Mesh* CreateFromSubDControlNet(class BND_SubD* subd);

  BND_Mesh();
  BND_Mesh(ON_Mesh* mesh, const ON_ModelComponentReference* compref);

  bool IsClosed() const { return m_mesh->IsClosed(); }
  BND_TUPLE IsManifold(bool topologicalTest) const;
  bool HasCachedTextureCoordinates() const { return m_mesh->HasCachedTextureCoordinates(); }
  bool HasPrincipalCurvatures() const { return m_mesh->HasPrincipalCurvatures(); }

  BND_MeshVertexList GetVertices();
  //public Collections.MeshTopologyVertexList TopologyVertices
  BND_MeshTopologyEdgeList GetTopologyEdges();
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
  void SetTextureCoordinates(class BND_TextureMapping* tm, class BND_Transform* xf, bool lazy);
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

#if defined(ON_WASM_COMPILE)
  BND_DICT ToThreejsJSON() const;
  BND_DICT ToThreejsJSONRotate(bool rotateToYUp) const;
  static BND_DICT ToThreejsJSONMerged(BND_TUPLE meshes, bool rotateYUp);
  static BND_Mesh* CreateFromThreejsJSON(BND_DICT data);
#endif

protected:
  void SetTrackedPointer(ON_Mesh* mesh, const ON_ModelComponentReference* compref);
};
