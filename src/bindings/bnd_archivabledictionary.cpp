#include "bnd_archivabledictionary.h"
#include "bnd_boundingbox_gen.h"
#include "bnd_commonobject_gen.h"
#include "bnd_interval_gen.h"
#include "bnd_line_gen.h"
#include "bnd_meshingparameters_gen.h"
#include "bnd_plane_gen.h"
#include "bnd_point2d_gen.h"
#include "bnd_point3d_gen.h"
#include "bnd_point3f_gen.h"
#include "bnd_point4d_gen.h"
#include "bnd_transform_gen.h"
#include "bnd_vector2d_gen.h"
#include "bnd_vector3d_gen.h"
#include "bnd_vector3f_gen.h"

using namespace bnd_shared;

// ---- F4: ArchivableDictionary --------------------------------------------------

// RhinoCommon's ArchivableDictionary item types (never change: they are on disk); the old
// bindings' `ItemType` is the same list
enum class DictItem : int
{
  Bool = 1, Byte = 2, SByte = 3, Short = 4, UShort = 5, Int32 = 6, UInt32 = 7, Int64 = 8,
  Single = 9, Double = 10, Guid = 11, String = 12,
  ArrayBool = 13, ArrayByte = 14, ArraySByte = 15, ArrayShort = 16, ArrayInt32 = 17,
  ArraySingle = 18, ArrayDouble = 19, ArrayGuid = 20, ArrayString = 21,
  Color = 22, Point = 23, PointF = 24, Rectangle = 25, RectangleF = 26, Size = 27, SizeF = 28, Font = 29,
  Interval = 30, Point2d = 31, Point3d = 32, Point4d = 33, Vector2d = 34, Vector3d = 35,
  BoundingBox = 36, Ray3d = 37, PlaneEquation = 38, Xform = 39, Plane = 40, Line = 41,
  Point3f = 42, Vector3f = 43,
  OnBinaryArchiveDictionary = 44, OnObject = 45, OnMeshParameters = 46, OnGeometry = 47,
  OnObjRef = 48, ArrayObjRef = 49, ArrayGeometry = 50,
  MAXVALUE = 50
};

// the .NET dictionary id RhinoCommon writes: 21EE7933-1E2D-4047-869E-6BDBF986EA11
static ON_UUID DotNetDictionaryId()
{
  static const ON_UUID id = { 0x21ee7933, 0x1e2d, 0x4047, { 0x86, 0x9e, 0x6b, 0xdb, 0xf9, 0x86, 0xea, 0x11 } };
  return id;
}

static bool WriteEntry(ON_Write3dmBufferArchive& archive, const BND_DICT& dict, const std::string& key, RH3DM_ValueKind kind)
{
  const char* k = key.c_str();
  DictItem it;
  switch (kind)
  {
  case RH3DM_ValueKind::Bool: it = DictItem::Bool; break;
  case RH3DM_ValueKind::Int: it = DictItem::Int32; break;
  case RH3DM_ValueKind::Double: it = DictItem::Double; break;
  case RH3DM_ValueKind::String: it = DictItem::String; break;
  case RH3DM_ValueKind::Point2d: it = DictItem::Point2d; break;
  case RH3DM_ValueKind::Point3d: it = DictItem::Point3d; break;
  case RH3DM_ValueKind::Point4d: it = DictItem::Point4d; break;
  case RH3DM_ValueKind::Vector2d: it = DictItem::Vector2d; break;
  case RH3DM_ValueKind::Vector3d: it = DictItem::Vector3d; break;
  case RH3DM_ValueKind::Geometry: it = DictItem::OnGeometry; break;
  case RH3DM_ValueKind::GeometryList: it = DictItem::ArrayGeometry; break;
  default: return false;
  }
  if (!archive.BeginWriteDictionaryEntry((int)it, ON_wString(k)))
    return false;
  bool rc = false;
  switch (it)
  {
  case DictItem::Bool: rc = archive.WriteBool(RH3DM_DictGet<bool>(dict, k)); break;
  case DictItem::Int32: rc = archive.WriteInt(RH3DM_DictGet<int>(dict, k)); break;
  case DictItem::Double: rc = archive.WriteDouble(RH3DM_DictGet<double>(dict, k)); break;
  case DictItem::String: rc = archive.WriteString(ON_wString(RH3DM_DictGet<std::wstring>(dict, k).c_str())); break;
  case DictItem::Point2d:
    { BND_Point2d p = RH3DM_DictGet<BND_Point2d>(dict, k); rc = archive.WritePoint(ON_2dPoint(p.m_x, p.m_y)); } break;
  case DictItem::Point3d:
    { BND_Point3d p = RH3DM_DictGet<BND_Point3d>(dict, k); rc = archive.WritePoint(ON_3dPoint(p.m_x, p.m_y, p.m_z)); } break;
  case DictItem::Point4d:
    { BND_Point4d p = RH3DM_DictGet<BND_Point4d>(dict, k); rc = archive.WritePoint(ON_4dPoint(p.m_x, p.m_y, p.m_z, p.m_w)); } break;
  case DictItem::Vector2d:
    { BND_Vector2d v = RH3DM_DictGet<BND_Vector2d>(dict, k); rc = archive.WriteVector(ON_2dVector(v.m_x, v.m_y)); } break;
  case DictItem::Vector3d:
    { BND_Vector3d v = RH3DM_DictGet<BND_Vector3d>(dict, k); rc = archive.WriteVector(ON_3dVector(v.m_x, v.m_y, v.m_z)); } break;
  case DictItem::OnGeometry:
    { const ON_Geometry* g = RH3DM_DictGeometry(dict, k); rc = g && archive.WriteObject(*g); } break;
  case DictItem::ArrayGeometry:
    {
      std::vector<const ON_Geometry*> gs = RH3DM_DictGeometryList(dict, k);
      rc = archive.WriteInt((int)gs.size());
      for (const ON_Geometry* g : gs)
        rc = rc && g && archive.WriteObject(*g);
    }
    break;
  default: break;
  }
  return archive.EndWriteDictionaryEntry() && rc;
}

BND_DICT BND_ArchivableDictionary_EncodeDict(BND_DICT dict)
{
  BND_DICT rc = RH3DM_DictNew();
  SetVersions(rc);
  unsigned int on_version = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(kArchive3dm, ON::Version());
  ON_Write3dmBufferArchive archive(0, 0, kArchive3dm, on_version);
  if (!archive.BeginWriteDictionary(DotNetDictionaryId(), 0, L""))
    return rc;
  for (const std::string& key : RH3DM_DictKeys(dict))
  {
    RH3DM_ValueKind kind = RH3DM_DictKind(dict, key.c_str());
    if (kind == RH3DM_ValueKind::Other)
      throw std::runtime_error("Unable to serialize '" + key + "'. Allowed value types are bool, int, float, str,"
                               " Point2d, Point3d, Point4d, Vector2d, Vector3d (Python), and GeometryBase or a list of them.");
    WriteEntry(archive, dict, key, kind);
  }
  archive.EndWriteDictionary();
  RH3DM_DictSet(rc, "data", Base64(archive));
  return rc;
}

static BND_Point3d P3(const ON_3dPoint& p) { return BND_Point3d(p.x, p.y, p.z); }
static BND_Vector3d V3(const ON_3dVector& v) { BND_Vector3d r; r.m_x = v.x; r.m_y = v.y; r.m_z = v.z; return r; }

static BND_Plane ToPlane(const ON_Plane& p)
{
  BND_Plane r;
  r.m_origin = P3(p.origin); r.m_xaxis = V3(p.xaxis); r.m_yaxis = V3(p.yaxis); r.m_zaxis = V3(p.zaxis);
  return r;
}

template <class T>
static bool ReadArray(ON_Read3dmBufferArchive& archive, std::vector<T>& out, bool (ON_BinaryArchive::*read)(size_t, T*))
{
  int count = 0;
  if (!archive.ReadInt(&count) || count <= 0)
    return false;
  out.resize(count);
  return (archive.*read)((size_t)count, out.data());
}

BND_DICT BND_ArchivableDictionary_DecodeDict(BND_DICT jsonObject)
{
  BND_DICT rc = RH3DM_DictNew();
  int rhinoversion = 0, opennurbsversion = 0;
  std::string bytes = ArchiveBytes(jsonObject, rhinoversion, opennurbsversion);
  if (bytes.empty())
    throw std::runtime_error("Unable to decode ArchivableDictionary: no data");
  ON_Read3dmBufferArchive archive(bytes.size(), bytes.data(), false, rhinoversion, (unsigned int)opennurbsversion);
  ON_UUID dictionaryId;
  unsigned int dictionaryVersion = 0;
  ON_wString dictionaryName;
  if (!archive.BeginReadDictionary(&dictionaryId, &dictionaryVersion, dictionaryName) || dictionaryId != DotNetDictionaryId())
    throw std::runtime_error("Unable to decode ArchivableDictionary: not a RhinoCommon dictionary");

  while (true)
  {
    int i_type = 0;
    ON_wString entryName;
    int read_rc = archive.BeginReadDictionaryEntry(&i_type, entryName);
    if (0 == read_rc)
      throw std::runtime_error("Unable to decode ArchivableDictionary: a corrupt entry");
    if (1 != read_rc)
      break;
    const ON_String key8(entryName);
    const char* k = key8.Array();
    switch (i_type > 0 && i_type <= (int)DictItem::MAXVALUE ? (DictItem)i_type : (DictItem)0)
    {
    case DictItem::Bool: { bool b; if (archive.ReadBool(&b)) RH3DM_DictSet(rc, k, b); } break;
    case DictItem::Byte:
    case DictItem::SByte: { char c; if (archive.ReadByte(1, &c)) RH3DM_DictSet(rc, k, (int)c); } break;
    case DictItem::Short:
    case DictItem::UShort: { short s; if (archive.ReadShort(&s)) RH3DM_DictSet(rc, k, (int)s); } break;
    case DictItem::Int32:
    case DictItem::UInt32: { int i; if (archive.ReadInt(&i)) RH3DM_DictSet(rc, k, i); } break;
    case DictItem::Int64: { time_t t; if (archive.ReadBigTime(&t)) RH3DM_DictSet(rc, k, (double)t); } break;
    case DictItem::Single: { float f; if (archive.ReadFloat(&f)) RH3DM_DictSet(rc, k, (double)f); } break;
    case DictItem::Double: { double d; if (archive.ReadDouble(&d)) RH3DM_DictSet(rc, k, d); } break;
    case DictItem::Guid: { ON_UUID id; if (archive.ReadUuid(id)) RH3DM_DictSet(rc, k, ON_UUID_to_Binding(id)); } break;
    case DictItem::String: { ON_wString s; if (archive.ReadString(s)) RH3DM_DictSet(rc, k, std::wstring(s.Array())); } break;
    case DictItem::ArrayBool:
      {
        std::vector<char> c;
        if (ReadArray(archive, c, &ON_BinaryArchive::ReadChar))
          RH3DM_DictSetList(rc, k, std::vector<bool>(c.begin(), c.end()));
      }
      break;
    case DictItem::ArrayByte:
    case DictItem::ArraySByte:
      {
        std::vector<char> c;
        if (ReadArray(archive, c, &ON_BinaryArchive::ReadChar))
          RH3DM_DictSetList(rc, k, std::vector<int>(c.begin(), c.end()));
      }
      break;
    case DictItem::ArrayShort:
      {
        std::vector<short> s;
        if (ReadArray(archive, s, &ON_BinaryArchive::ReadShort))
          RH3DM_DictSetList(rc, k, std::vector<int>(s.begin(), s.end()));
      }
      break;
    case DictItem::ArrayInt32:
      { std::vector<int> i; if (ReadArray(archive, i, &ON_BinaryArchive::ReadInt)) RH3DM_DictSetList(rc, k, i); } break;
    case DictItem::ArraySingle:
      {
        std::vector<float> f;
        if (ReadArray(archive, f, &ON_BinaryArchive::ReadFloat))
          RH3DM_DictSetList(rc, k, std::vector<double>(f.begin(), f.end()));
      }
      break;
    case DictItem::ArrayDouble:
      { std::vector<double> d; if (ReadArray(archive, d, &ON_BinaryArchive::ReadDouble)) RH3DM_DictSetList(rc, k, d); } break;
    case DictItem::ArrayGuid:
      {
        int count = 0;
        if (archive.ReadInt(&count) && count > 0)
        {
          std::vector<BND_UUID> ids;
          for (int i = 0; i < count; i++)
          {
            ON_UUID id;
            if (archive.ReadUuid(id)) ids.push_back(ON_UUID_to_Binding(id));
          }
          RH3DM_DictSetList(rc, k, ids);
        }
      }
      break;
    case DictItem::ArrayString:
      {
        int count = 0;
        if (archive.ReadInt(&count) && count > 0)
        {
          std::vector<std::wstring> strings;
          for (int i = 0; i < count; i++)
          {
            ON_wString s;
            if (archive.ReadString(s)) strings.push_back(std::wstring(s.Array()));
          }
          RH3DM_DictSetList(rc, k, strings);
        }
      }
      break;
    case DictItem::Color: { ON_Color c; if (archive.ReadColor(c)) RH3DM_DictSet(rc, k, ON_Color_to_Binding(c)); } break;
    case DictItem::Point:
    case DictItem::Size:
      { int i[2]; if (archive.ReadInt(2, i)) RH3DM_DictSetTuple(rc, k, std::vector<int>(i, i + 2)); } break;
    case DictItem::PointF:
    case DictItem::SizeF:
      { float f[2]; if (archive.ReadFloat(2, f)) RH3DM_DictSetTuple(rc, k, std::vector<double>(f, f + 2)); } break;
    case DictItem::Rectangle:
      { int i[4]; if (archive.ReadInt(4, i)) RH3DM_DictSetTuple(rc, k, std::vector<int>(i, i + 4)); } break;
    case DictItem::RectangleF:
      { float f[4]; if (archive.ReadFloat(4, f)) RH3DM_DictSetTuple(rc, k, std::vector<double>(f, f + 4)); } break;
    case DictItem::Interval:
      {
        double d[2];
        if (archive.ReadDouble(2, d)) { BND_Interval iv; iv.m_t0 = d[0]; iv.m_t1 = d[1]; RH3DM_DictSet(rc, k, iv); }
      }
      break;
    case DictItem::Point2d: { ON_2dPoint p; if (archive.ReadPoint(p)) { BND_Point2d r; r.m_x = p.x; r.m_y = p.y; RH3DM_DictSet(rc, k, r); } } break;
    case DictItem::Point3d: { ON_3dPoint p; if (archive.ReadPoint(p)) RH3DM_DictSet(rc, k, P3(p)); } break;
    case DictItem::Point4d:
      {
        ON_4dPoint p;
        if (archive.ReadPoint(p)) { BND_Point4d r; r.m_x = p.x; r.m_y = p.y; r.m_z = p.z; r.m_w = p.w; RH3DM_DictSet(rc, k, r); }
      }
      break;
    case DictItem::Vector2d: { ON_2dVector v; if (archive.ReadVector(v)) { BND_Vector2d r; r.m_x = v.x; r.m_y = v.y; RH3DM_DictSet(rc, k, r); } } break;
    case DictItem::Vector3d: { ON_3dVector v; if (archive.ReadVector(v)) RH3DM_DictSet(rc, k, V3(v)); } break;
    case DictItem::BoundingBox:
      {
        ON_BoundingBox bb;
        if (archive.ReadBoundingBox(bb)) { BND_BoundingBox r; r.m_min = P3(bb.m_min); r.m_max = P3(bb.m_max); RH3DM_DictSet(rc, k, r); }
      }
      break;
    case DictItem::Xform:
      {
        ON_Xform xf;
        if (archive.ReadXform(xf))
        {
          BND_Transform t;
          double* m[16] = { &t.m_00, &t.m_01, &t.m_02, &t.m_03, &t.m_10, &t.m_11, &t.m_12, &t.m_13,
                            &t.m_20, &t.m_21, &t.m_22, &t.m_23, &t.m_30, &t.m_31, &t.m_32, &t.m_33 };
          for (int i = 0; i < 16; i++) *m[i] = xf.m_xform[i / 4][i % 4];
          RH3DM_DictSet(rc, k, t);
        }
      }
      break;
    case DictItem::Plane: { ON_Plane p; if (archive.ReadPlane(p)) RH3DM_DictSet(rc, k, ToPlane(p)); } break;
    case DictItem::Line:
      { ON_Line l; if (archive.ReadLine(l)) { BND_Line r; r.m_from = P3(l.from); r.m_to = P3(l.to); RH3DM_DictSet(rc, k, r); } } break;
    case DictItem::Point3f:
      {
        float f[3];
        if (archive.ReadFloat(3, f)) { BND_Point3f r; r.m_x = f[0]; r.m_y = f[1]; r.m_z = f[2]; RH3DM_DictSet(rc, k, r); }
      }
      break;
    case DictItem::Vector3f:
      {
        float f[3];
        if (archive.ReadFloat(3, f)) { BND_Vector3f r; r.m_x = f[0]; r.m_y = f[1]; r.m_z = f[2]; RH3DM_DictSet(rc, k, r); }
      }
      break;
    case DictItem::OnMeshParameters:
      { ON_MeshParameters mp; if (mp.Read(archive)) RH3DM_DictSet(rc, k, BND_MeshingParameters(mp)); } break;
    case DictItem::OnObject:
    case DictItem::OnGeometry:
      { ON_Object* o = nullptr; if (archive.ReadObject(&o)) RH3DM_DictSetOwned(rc, k, RH3DM_Wrap_CommonObject(o)); } break;
    case DictItem::ArrayGeometry:
      {
        int count = 0;
        if (archive.ReadInt(&count) && count > 0)
        {
          // the objects only (the old binding put `count` Nones before them: pilot #58)
          std::vector<BND_CommonObject*> objects;
          for (int i = 0; i < count; i++)
          {
            ON_Object* o = nullptr;
            if (archive.ReadObject(&o)) objects.push_back(RH3DM_Wrap_CommonObject(o));
          }
          RH3DM_DictSetOwnedList(rc, k, objects);
        }
      }
      break;
    default:   // Font, Ray3d, PlaneEquation, dictionaries, object references: skipped, as before
      break;
    }
    if (!archive.EndReadDictionaryEntry())
      throw std::runtime_error("Unable to decode ArchivableDictionary: a corrupt entry");
  }
  archive.EndReadDictionary();
  return rc;
}
