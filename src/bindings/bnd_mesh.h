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

class BND_Mesh : public BND_GeometryBase
{
  ON_Mesh* m_mesh = nullptr;
public:
  BND_Mesh();
  BND_Mesh(ON_Mesh* mesh, const ON_ModelComponentReference* compref);

  BND_MeshVertexList GetVertices();
  BND_MeshFaceList GetFaces();
  BND_MeshNormalList GetNormals();

protected:
  void SetTrackedPointer(ON_Mesh* mesh, const ON_ModelComponentReference* compref);
};
