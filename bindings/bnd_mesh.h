#include "bindings.h"

#pragma once

class BND_MeshVertexList
{
  std::shared_ptr<ON_Mesh> m_mesh;
public:
  BND_MeshVertexList(const std::shared_ptr<ON_Mesh>& mesh);

  ON_3fPoint* begin();
  ON_3fPoint* end();

  int Count() const;
  void SetCount(int i);
  ON_3fPoint GetVertex(int i) const;
  void SetVertex(int i, ON_3fPoint pt);
};

class BND_MeshFaceList
{
  std::shared_ptr<ON_Mesh> m_mesh;
public:
  BND_MeshFaceList(const std::shared_ptr<ON_Mesh>& mesh);
  int Count() const;
  #if defined(__EMSCRIPTEN__)
  emscripten::val GetFace(int i) const;
  #endif
  #if defined(ON_PYTHON_COMPILE)
  pybind11::list GetFace(int i) const;
  #endif
};

class BND_Mesh : public BND_Geometry
{
  std::shared_ptr<ON_Mesh> m_mesh;
public:
  BND_Mesh();
  BND_Mesh(ON_Mesh* mesh);

  BND_MeshVertexList GetVertices();
  BND_MeshFaceList GetFaces();
};
