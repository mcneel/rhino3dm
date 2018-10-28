#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initMeshBindings(pybind11::module& m);
#else
void initMeshBindings(void* m);
#endif

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

class BND_MeshTextureCoordinateList
{
  ON_ModelComponentReference m_component_reference;
  ON_Mesh* m_mesh = nullptr;
public:
  BND_MeshTextureCoordinateList(ON_Mesh* mesh, const ON_ModelComponentReference& compref);
  int Count() const { return m_mesh->m_T.Count(); }
  ON_2fPoint GetTextureCoordinate(int i) const { return m_mesh->m_T[i]; }
  void SetTextureCoordinate(int i, ON_2fPoint tc) { m_mesh->m_T[i] = tc; }
};


class BND_Mesh : public BND_GeometryBase
{
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
  //public Collections.MeshVertexColorList VertexColors
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
