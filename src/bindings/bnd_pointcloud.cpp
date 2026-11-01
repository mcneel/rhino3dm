#include "bnd_pointcloud.h"
#include "bnd_point3d_gen.h"

using namespace bnd_shared;

BND_DICT BND_PointCloud_ToThreejsBuffers(const BND_PointCloud& cloud, bool rotateToYUp)
{
  ON_PointCloud pc(*cloud.RH3DM_Const());
  if (rotateToYUp)
    pc.Transform(ToYUp());
  BND_DICT rc = RH3DM_DictNew();
  RH3DM_DictSetFloats(rc, "position", Floats3(pc.m_P));
  RH3DM_DictSet(rc, "pointCount", pc.m_P.Count());
  if (pc.HasPointColors())
    RH3DM_DictSetFloats(rc, "color", Colors3(pc.m_C));
  if (pc.HasPointNormals())
    RH3DM_DictSetFloats(rc, "normal", Floats3(pc.m_N));
  return rc;
}

BND_PointCloud* BND_PointCloud_CreateFromThreejsBuffers(BND_DICT buffers, bool rotateFromYUp)
{
  const std::vector<double> pos = Buffer(buffers, "position", 3, 0);
  const int n = (int)(pos.size() / 3);
  const std::vector<double> nrm = Buffer(buffers, "normal", 3, n);
  const std::vector<double> col = Buffer(buffers, "color", 3, n);
  ON_PointCloud* pc = new ON_PointCloud(n);
  for (int i = 0; i < n; i++)
    pc->m_P.Append(ON_3dPoint(pos[(size_t)i * 3], pos[(size_t)i * 3 + 1], pos[(size_t)i * 3 + 2]));
  for (int i = 0; i < (int)nrm.size() / 3; i++)
    pc->m_N.Append(ON_3dVector(nrm[(size_t)i * 3], nrm[(size_t)i * 3 + 1], nrm[(size_t)i * 3 + 2]));
  for (int i = 0; i < (int)col.size() / 3; i++)
    pc->m_C.Append(ColorAt(col, i));
  if (rotateFromYUp)
    pc->Transform(FromYUp());
  pc->InvalidateBoundingBox();
  return new BND_PointCloud(pc);
}

BND_DICT BND_PointCloud_ToThreejsJSON(const BND_PointCloud& cloud)
{
  RH3DM_Deprecated("PointCloud.toThreejsJSON", "PointCloud.toThreejsBuffers(false)");
  const ON_PointCloud& pc = *cloud.RH3DM_Const();
  BND_DICT attributes = RH3DM_DictNew();
  RH3DM_DictSet(attributes, "position", JsonAttribute(3, Doubles3(pc.m_P)));
  if (pc.HasPointColors())
    RH3DM_DictSet(attributes, "color", JsonAttribute(3, JsonColors(pc.m_C)));
  if (pc.HasPointNormals())
    RH3DM_DictSet(attributes, "normal", JsonAttribute(3, Doubles3(pc.m_N)));
  return JsonGeometry(nullptr, attributes);
}

BND_PointCloud* BND_PointCloud_CreateFromThreejsJSON(BND_DICT json)
{
  RH3DM_Deprecated("PointCloud.createFromThreejsJSON", "PointCloud.createFromThreejsBuffers(buffers, false)");
  if (!RH3DM_DictHas(json, "data"))
    return nullptr;
  const BND_DICT attributes = RH3DM_DictGet<BND_DICT>(RH3DM_DictGet<BND_DICT>(json, "data"), "attributes");
  const std::vector<double> pos = JsonArray(attributes, "position");
  const std::vector<double> nrm = JsonArray(attributes, "normal");
  int channels = 3;
  const std::vector<double> col = JsonArray(attributes, "color", &channels);
  ON_SimpleArray<ON_Color> colors;
  JsonColorsTo(col, channels, colors);
  ON_PointCloud* pc = new ON_PointCloud((int)(pos.size() / 3));
  for (size_t i = 0; i + 2 < pos.size(); i += 3)
    pc->m_P.Append(ON_3dPoint(pos[i], pos[i + 1], pos[i + 2]));
  for (size_t i = 0; i + 2 < nrm.size(); i += 3)
    pc->m_N.Append(ON_3dVector(nrm[i], nrm[i + 1], nrm[i + 2]));
  pc->m_C = colors;
  pc->InvalidateBoundingBox();
  return new BND_PointCloud(pc);
}

// inserts `count` defaults at `index` in every channel that is aligned with the points (count n)
static void InsertChannels(ON_PointCloud* p, int n, int index, int count)
{
  for (int i = 0; i < count; i++)
  {
    if (p->m_N.Count() == n + i) p->m_N.Insert(index + i, ON_3dVector::ZeroVector);
    if (p->m_C.Count() == n + i) p->m_C.Insert(index + i, ON_Color(0));
    if (p->m_H.Count() == n + i) p->m_H.Insert(index + i, false);
  }
}

// values for the n points already there, when the cloud has none
static void EnsureValues(ON_PointCloud* p, int n)
{
  if (p->m_V.Count() == 0 && n > 0)
  {
    p->m_V.Reserve(n);
    for (int i = 0; i < n; i++) p->m_V.Append(0.0);
  }
}

static void InsertOne(ON_PointCloud* p, int index, const ON_3dPoint& point, double value)
{
  const int n = p->m_P.Count();
  EnsureValues(p, n);
  InsertChannels(p, n, index, 1);
  p->m_P.Insert(index, point);
  p->m_V.Insert(index, value);
  p->InvalidateBoundingBox();
}

void BND_PointCloud_AddValue(const BND_PointCloud& cloud, BND_Point3d point, double value)
{
  ON_PointCloud* p = cloud.RH3DM_NonConst();
  InsertOne(p, p->m_P.Count(), RH3DM_Val(point), value);
}

void BND_PointCloud_InsertValue(const BND_PointCloud& cloud, int index, BND_Point3d point, double value)
{
  ON_PointCloud* p = cloud.RH3DM_NonConst();
  if (index < 0) throw std::out_of_range("index must be equal to or larger than zero");
  if (index > p->m_P.Count()) throw std::out_of_range("index must be equal to or smaller than Count");
  InsertOne(p, index, RH3DM_Val(point), value);
}

void BND_PointCloud_AddRangeValues(const BND_PointCloud& cloud, RH3DM_SeqArg<BND_Point3d> points, RH3DM_SeqArg<double> values)
{
  const std::vector<BND_Point3d> pts = RH3DM_SeqVec<BND_Point3d>(points);
  const std::vector<double> vals = RH3DM_SeqVec<double>(values);
  if (pts.empty() || vals.empty()) return;
  if (pts.size() != vals.size()) throw std::invalid_argument("Must supply equal number of points and values");
  ON_PointCloud* p = cloud.RH3DM_NonConst();
  for (size_t i = 0; i < pts.size(); i++)
    InsertOne(p, p->m_P.Count(), RH3DM_Val(pts[i]), vals[i]);
}
