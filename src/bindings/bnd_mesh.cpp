#include "bnd_mesh.h"

using namespace bnd_shared;

BND_DICT BND_Mesh_ToThreejsBuffers(const BND_Mesh& mesh, bool rotateToYUp)
{
  // a copy: the rotation, and the normals three.js needs, never touch the caller's mesh (the old
  // rhino3dm 8.x member computed them on the mesh itself)
  ON_Mesh m(*mesh.RH3DM_Const());
  if (rotateToYUp)
    m.Transform(ToYUp());
  if (m.m_N.Count() == 0)
    m.ComputeVertexNormals();

  std::vector<unsigned int> index;
  index.reserve((size_t)m.m_F.Count() * 6);
  for (int i = 0; i < m.m_F.Count(); i++)
  {
    const ON_MeshFace& f = m.m_F[i];
    index.insert(index.end(), { (unsigned int)f.vi[0], (unsigned int)f.vi[1], (unsigned int)f.vi[2] });
    if (f.vi[2] != f.vi[3])
      index.insert(index.end(), { (unsigned int)f.vi[2], (unsigned int)f.vi[3], (unsigned int)f.vi[0] });
  }

  BND_DICT rc = RH3DM_DictNew();
  RH3DM_DictSetFloats(rc, "position", Floats3(m.m_V));
  RH3DM_DictSetFloats(rc, "normal", Floats3(m.m_N));
  RH3DM_DictSetUints(rc, "index", index);
  RH3DM_DictSet(rc, "vertexCount", m.m_V.Count());
  if (m.HasTextureCoordinates())
  {
    std::vector<float> uv((size_t)m.m_T.Count() * 2);
    for (int i = 0; i < m.m_T.Count(); i++)
    {
      uv[(size_t)i * 2] = m.m_T[i].x;
      uv[(size_t)i * 2 + 1] = m.m_T[i].y;
    }
    RH3DM_DictSetFloats(rc, "uv", uv);
  }
  if (m.HasVertexColors())
    RH3DM_DictSetFloats(rc, "color", Colors3(m.m_C));
  return rc;
}

BND_Mesh* BND_Mesh_CreateFromThreejsBuffers(BND_DICT buffers, bool rotateFromYUp)
{
  const std::vector<double> pos = Buffer(buffers, "position", 3, 0);
  const int n = (int)(pos.size() / 3);
  const std::vector<double> nrm = Buffer(buffers, "normal", 3, n);
  const std::vector<double> uv = Buffer(buffers, "uv", 2, n);
  const std::vector<double> col = Buffer(buffers, "color", 3, n);
  std::vector<double> index = Buffer(buffers, "index", 3, 0);
  if (index.empty())                    // non-indexed: every 3 vertices a triangle
  {
    if (n % 3 != 0)
      throw std::invalid_argument("three.js buffers: no 'index', and the vertex count is not a multiple of 3");
    for (int i = 0; i < n; i++) index.push_back(i);
  }

  ON_Mesh* m = new ON_Mesh((int)(index.size() / 3), n, !nrm.empty(), !uv.empty());
  for (int i = 0; i < n; i++)
    m->m_V.Append(ON_3fPoint((float)pos[(size_t)i * 3], (float)pos[(size_t)i * 3 + 1], (float)pos[(size_t)i * 3 + 2]));
  for (size_t i = 0; i < index.size(); i += 3)
  {
    const double a = index[i], b = index[i + 1], c = index[i + 2];
    if (a < 0 || b < 0 || c < 0 || a >= n || b >= n || c >= n)
    {
      delete m;
      throw std::invalid_argument("three.js buffers: an index is outside the vertices");
    }
    ON_MeshFace& f = m->m_F.AppendNew();
    f.vi[0] = (int)a; f.vi[1] = (int)b; f.vi[2] = f.vi[3] = (int)c;
  }
  for (int i = 0; i < (int)nrm.size() / 3; i++)
    m->m_N.Append(ON_3fVector((float)nrm[(size_t)i * 3], (float)nrm[(size_t)i * 3 + 1], (float)nrm[(size_t)i * 3 + 2]));
  for (int i = 0; i < (int)uv.size() / 2; i++)
    m->m_T.Append(ON_2fPoint((float)uv[(size_t)i * 2], (float)uv[(size_t)i * 2 + 1]));
  for (int i = 0; i < (int)col.size() / 3; i++)
    m->m_C.Append(ColorAt(col, i));
  if (rotateFromYUp)
    m->Transform(FromYUp());
  return new BND_Mesh(m);
}

// a mesh's JSON, from a mesh this may change (rotated, normals computed)
static BND_DICT MeshJson(ON_Mesh& m, bool rotateToYUp)
{
  if (rotateToYUp)
    m.Transform(ToYUp());
  if (m.m_N.Count() == 0)
    m.ComputeVertexNormals();

  std::vector<double> indexList;
  indexList.reserve((size_t)m.m_F.Count() * 6);
  for (int i = 0; i < m.m_F.Count(); i++)
  {
    const ON_MeshFace& f = m.m_F[i];
    indexList.insert(indexList.end(), { (double)f.vi[0], (double)f.vi[1], (double)f.vi[2] });
    if (f.vi[2] != f.vi[3])
      indexList.insert(indexList.end(), { (double)f.vi[2], (double)f.vi[3], (double)f.vi[0] });
  }
  BND_DICT index = RH3DM_DictNew();
  RH3DM_DictSet(index, "type", std::string(m.m_V.Count() > 65536 ? "Uint32Array" : "Uint16Array"));
  RH3DM_DictSetList(index, "array", indexList);

  BND_DICT attributes = RH3DM_DictNew();
  RH3DM_DictSet(attributes, "position", JsonAttribute(3, Doubles3(m.m_V)));
  RH3DM_DictSet(attributes, "normal", JsonAttribute(3, Doubles3(m.m_N)));
  if (m.HasTextureCoordinates())
  {
    std::vector<double> uv((size_t)m.m_T.Count() * 2);
    for (int i = 0; i < m.m_T.Count(); i++)
    {
      uv[(size_t)i * 2] = m.m_T[i].x;
      uv[(size_t)i * 2 + 1] = m.m_T[i].y;
    }
    RH3DM_DictSet(attributes, "uv", JsonAttribute(2, uv));
  }
  if (m.HasVertexColors())
    RH3DM_DictSet(attributes, "color", JsonAttribute(3, JsonColors(m.m_C)));
  return JsonGeometry(&index, attributes);
}

BND_DICT BND_Mesh_ToThreejsJSON(const BND_Mesh& mesh)
{
  RH3DM_Deprecated("Mesh.toThreejsJSON", "Mesh.toThreejsBuffers(false)");
  ON_Mesh m(*mesh.RH3DM_Const());
  return MeshJson(m, false);
}

BND_DICT BND_Mesh_ToThreejsJSONRotate(const BND_Mesh& mesh, bool rotateToYUp)
{
  RH3DM_Deprecated("Mesh.toThreejsJSONRotate", "Mesh.toThreejsBuffers(rotateToYUp)");
  ON_Mesh m(*mesh.RH3DM_Const());
  return MeshJson(m, rotateToYUp);
}

BND_DICT BND_Mesh_ToThreejsJSONMerged(RH3DM_SeqPtrArg<BND_Mesh> meshes, bool rotateYUp)
{
  RH3DM_Deprecated("Mesh.toThreejsJSONMerged", "Mesh.toThreejsBuffers on one mesh appended from the others");
  ON_Mesh m;
  for (const BND_Mesh* mesh : RH3DM_SeqPtrVec<BND_Mesh>(meshes))
  {
    if (mesh)
      m.Append(*mesh->RH3DM_Const());
  }
  return MeshJson(m, rotateYUp);
}

BND_Mesh* BND_Mesh_CreateFromThreejsJSON(BND_DICT data)
{
  RH3DM_Deprecated("Mesh.createFromThreejsJSON", "Mesh.createFromThreejsBuffers(buffers, true)");
  if (!RH3DM_DictHas(data, "data"))
    return nullptr;
  const BND_DICT d = RH3DM_DictGet<BND_DICT>(data, "data");
  const BND_DICT attributes = RH3DM_DictGet<BND_DICT>(d, "attributes");
  std::vector<double> index = RH3DM_DictHas(d, "index") ? JsonArray(d, "index") : std::vector<double>();
  const std::vector<double> pos = JsonArray(attributes, "position");
  const std::vector<double> nrm = JsonArray(attributes, "normal");
  const std::vector<double> uv = JsonArray(attributes, "uv");
  int channels = 3;
  const std::vector<double> col = JsonArray(attributes, "color", &channels);
  ON_SimpleArray<ON_Color> colors;
  JsonColorsTo(col, channels, colors);
  const int n = (int)(pos.size() / 3);
  if (index.empty())                    // non-indexed: every 3 vertices a triangle
    for (int i = 0; i + 2 < n; i += 3) index.insert(index.end(), { (double)i, (double)i + 1, (double)i + 2 });

  ON_Mesh* m = new ON_Mesh((int)(index.size() / 3), n, !nrm.empty(), !uv.empty());
  for (int i = 0; i < n; i++)
    m->m_V.Append(ON_3fPoint((float)pos[(size_t)i * 3], (float)pos[(size_t)i * 3 + 1], (float)pos[(size_t)i * 3 + 2]));
  for (size_t i = 0; i + 2 < index.size(); i += 3)
  {
    const double a = index[i], b = index[i + 1], c = index[i + 2];
    if (a < 0 || b < 0 || c < 0 || a >= n || b >= n || c >= n)
    {
      delete m;
      throw std::invalid_argument("three.js JSON: an index is outside the vertices");
    }
    ON_MeshFace& f = m->m_F.AppendNew();
    f.vi[0] = (int)a; f.vi[1] = (int)b; f.vi[2] = f.vi[3] = (int)c;
  }
  for (size_t i = 0; i + 2 < nrm.size(); i += 3)
    m->m_N.Append(ON_3fVector((float)nrm[i], (float)nrm[i + 1], (float)nrm[i + 2]));
  for (size_t i = 0; i + 1 < uv.size(); i += 2)
    m->m_T.Append(ON_2fPoint((float)uv[i], (float)uv[i + 1]));
  m->m_C = colors;
  m->Transform(FromYUp());              // 8.x: always
  return new BND_Mesh(m);
}
