#include "bnd_common.h"
#include "base64.h"
#include "bnd_file3dmrendercontent_gen.h"
#include "bnd_point3d_gen.h"
#include "bnd_geometrybase_gen.h"
#include <set>

#if defined(ON_PYTHON_COMPILE)

BND_DICT RH3DM_DictNew() { return py::dict(); }
bool RH3DM_DictHas(const BND_DICT& d, const char* key) { return d.contains(key); }
void RH3DM_ThrowKeyError(const char* key) { throw py::key_error(key); }

void RH3DM_Deprecated(const char* member, const char* instead)
{
  static std::set<std::string> warned;
  if (!warned.insert(member).second)
    return;
  const std::string msg = std::string("rhino3dm: ") + member + " is deprecated and will be removed; use " + instead;
  if (PyErr_WarnEx(PyExc_DeprecationWarning, msg.c_str(), 2) < 0)
    throw py::error_already_set();   // warnings raised as errors
}

std::vector<std::string> RH3DM_DictKeys(const BND_DICT& d)
{
  std::vector<std::string> rc;
  for (auto item : d)
    rc.push_back(py::cast<std::string>(py::str(item.first)));
  return rc;
}

void RH3DM_DictSetOwned(BND_DICT& d, const char* key, BND_CommonObject* owned)
{
  d[key] = py::cast(owned, py::return_value_policy::take_ownership);
}

void RH3DM_DictSetOwnedList(BND_DICT& d, const char* key, const std::vector<BND_CommonObject*>& owned)
{
  py::list l;
  for (BND_CommonObject* p : owned) l.append(py::cast(p, py::return_value_policy::take_ownership));
  d[key] = l;
}

static RH3DM_ValueKind KindOf(py::handle v)
{
  // bool before int: a Python bool is an int
  if (py::isinstance<py::bool_>(v)) return RH3DM_ValueKind::Bool;
  if (py::isinstance<py::int_>(v)) return RH3DM_ValueKind::Int;
  if (py::isinstance<py::float_>(v)) return RH3DM_ValueKind::Double;
  if (py::isinstance<py::str>(v)) return RH3DM_ValueKind::String;
  if (py::isinstance<BND_Point2d>(v)) return RH3DM_ValueKind::Point2d;
  if (py::isinstance<BND_Point3d>(v)) return RH3DM_ValueKind::Point3d;
  if (py::isinstance<BND_Point4d>(v)) return RH3DM_ValueKind::Point4d;
  if (py::isinstance<BND_Vector2d>(v)) return RH3DM_ValueKind::Vector2d;
  if (py::isinstance<BND_Vector3d>(v)) return RH3DM_ValueKind::Vector3d;
  if (py::isinstance<BND_GeometryBase>(v)) return RH3DM_ValueKind::Geometry;
  if (py::isinstance<py::iterable>(v))
  {
    bool any = false;
    for (auto item : py::iter(v))
    {
      if (!py::isinstance<BND_GeometryBase>(item)) return RH3DM_ValueKind::Other;
      any = true;
    }
    if (any) return RH3DM_ValueKind::GeometryList;
  }
  return RH3DM_ValueKind::Other;
}

RH3DM_ValueKind RH3DM_DictKind(const BND_DICT& d, const char* key)
{
  return RH3DM_DictHas(d, key) ? KindOf(d[key]) : RH3DM_ValueKind::Other;
}

static const ON_Geometry* GeometryOf(py::handle v)
{
  return ON_Geometry::Cast(py::cast<BND_GeometryBase*>(v)->RH3DM_ObjPtr());
}

const ON_Geometry* RH3DM_DictGeometry(const BND_DICT& d, const char* key) { return GeometryOf(d[key]); }

std::vector<const ON_Geometry*> RH3DM_DictGeometryList(const BND_DICT& d, const char* key)
{
  std::vector<const ON_Geometry*> rc;
  for (auto item : py::iter(d[key]))
    rc.push_back(GeometryOf(item));
  return rc;
}

#else

BND_DICT RH3DM_DictNew() { return emscripten::val::object(); }
bool RH3DM_DictHas(const BND_DICT& d, const char* key)
{
  return !d.isNull() && !d.isUndefined() && d.typeOf().as<std::string>() == "object" && d.hasOwnProperty(key);
}
void RH3DM_ThrowKeyError(const char* key) { throw std::invalid_argument(std::string("missing key: ") + key); }

void RH3DM_Deprecated(const char* member, const char* instead)
{
  static std::set<std::string> warned;
  if (!warned.insert(member).second)
    return;
  emscripten::val::global("console").call<void>("warn", std::string("rhino3dm: ") + member
                                                + " is deprecated and will be removed; use " + instead);
}

std::vector<std::string> RH3DM_DictKeys(const BND_DICT& d)
{
  std::vector<std::string> rc;
  emscripten::val keys = emscripten::val::global("Object").call<emscripten::val>("keys", d);
  int n = keys["length"].as<int>();
  for (int i = 0; i < n; i++)
    rc.push_back(keys[i].as<std::string>());
  return rc;
}

void RH3DM_DictSetOwned(BND_DICT& d, const char* key, BND_CommonObject* owned)
{
  d.set(key, owned ? emscripten::val(owned, emscripten::allow_raw_pointers()) : emscripten::val::null());
}

void RH3DM_DictSetOwnedList(BND_DICT& d, const char* key, const std::vector<BND_CommonObject*>& owned)
{
  emscripten::val a = emscripten::val::array();
  for (size_t i = 0; i < owned.size(); i++)
    a.set((unsigned)i, owned[i] ? emscripten::val(owned[i], emscripten::allow_raw_pointers()) : emscripten::val::null());
  d.set(key, a);
}

static bool IsGeometry(const emscripten::val& v)
{
  return v.typeOf().as<std::string>() == "object" && !v.isNull()
    && v.instanceof(emscripten::val::module_property("GeometryBase"));
}

RH3DM_ValueKind RH3DM_DictKind(const BND_DICT& d, const char* key)
{
  if (!RH3DM_DictHas(d, key)) return RH3DM_ValueKind::Other;
  emscripten::val v = d[key];
  if (v.isTrue() || v.isFalse()) return RH3DM_ValueKind::Bool;
  if (v.isNumber()) return RH3DM_ValueKind::Double;   // JS has one number type, as the old binding read it
  if (v.isString()) return RH3DM_ValueKind::String;
  if (IsGeometry(v)) return RH3DM_ValueKind::Geometry;
  if (v.isArray())
  {
    int n = v["length"].as<int>();
    for (int i = 0; i < n; i++)
      if (!IsGeometry(v[i])) return RH3DM_ValueKind::Other;
    if (n > 0) return RH3DM_ValueKind::GeometryList;
  }
  return RH3DM_ValueKind::Other;
}

static const ON_Geometry* GeometryOf(const emscripten::val& v)
{
  return ON_Geometry::Cast(v.as<BND_GeometryBase*>(emscripten::allow_raw_pointers())->RH3DM_ObjPtr());
}

const ON_Geometry* RH3DM_DictGeometry(const BND_DICT& d, const char* key) { return GeometryOf(d[key]); }

std::vector<const ON_Geometry*> RH3DM_DictGeometryList(const BND_DICT& d, const char* key)
{
  std::vector<const ON_Geometry*> rc;
  emscripten::val a = d[key];
  int n = a["length"].as<int>();
  for (int i = 0; i < n; i++)
    rc.push_back(GeometryOf(a[i]));
  return rc;
}

#endif

#if defined(ON_PYTHON_COMPILE)
void RH3DM_DictSetFloats(BND_DICT& d, const char* key, const std::vector<float>& values)
{
  py::list l;
  for (float v : values) l.append(v);
  d[key] = l;
}

void RH3DM_DictSetUints(BND_DICT& d, const char* key, const std::vector<unsigned int>& values)
{
  py::list l;
  for (unsigned int v : values) l.append(v);
  d[key] = l;
}

std::vector<double> RH3DM_DictGetNumbers(const BND_DICT& d, const char* key)
{
  if (!d.contains(key) || d[key].is_none()) return {};
  return py::cast<std::vector<double>>(d[key]);
}
#else
void RH3DM_DictSetFloats(BND_DICT& d, const char* key, const std::vector<float>& values)
{
  emscripten::val a = emscripten::val::global("Float32Array").new_(emscripten::typed_memory_view(values.size(), values.data()));
  d.set(key, a);   // `new Float32Array(view)` copies: the vector may go
}

void RH3DM_DictSetUints(BND_DICT& d, const char* key, const std::vector<unsigned int>& values)
{
  emscripten::val a = emscripten::val::global("Uint32Array").new_(emscripten::typed_memory_view(values.size(), values.data()));
  d.set(key, a);
}

std::vector<double> RH3DM_DictGetNumbers(const BND_DICT& d, const char* key)
{
  emscripten::val v = d[key];
  if (v.isUndefined() || v.isNull()) return {};
  return emscripten::convertJSArrayToNumberVector<double>(v);   // a typed array or a plain array
}
#endif

namespace bnd_shared
{
BND_Point3d PointFromDict(const BND_DICT& d)
{
  return BND_Point3d(RH3DM_DictGet<double>(d, "X"), RH3DM_DictGet<double>(d, "Y"), RH3DM_DictGet<double>(d, "Z"));
}

void SetVersions(BND_DICT& d)
{
  RH3DM_DictSet(d, "version", 10000);
  RH3DM_DictSet(d, "archive3dm", kArchive3dm);
  RH3DM_DictSet(d, "opennurbs", (int)ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(kArchive3dm, ON::Version()));
}

std::string Base64(const ON_Write3dmBufferArchive& archive)
{
  size_t length = archive.SizeOfArchive();
  return length > 0 ? base64_encode((const unsigned char*)archive.Buffer(), (unsigned int)length) : std::string();
}

std::string ArchiveBytes(const BND_DICT& jsonObject, int& rhinoversion, int& opennurbsversion)
{
  std::string decoded = base64_decode(RH3DM_DictGet<std::string>(jsonObject, "data"));
  rhinoversion = RH3DM_DictGet<int>(jsonObject, "archive3dm");
  opennurbsversion = RH3DM_DictGet<int>(jsonObject, "opennurbs");
  // bogus file versions (as RhinoCommon's ON_ReadBufferArchive)
  if (rhinoversion > 5 && rhinoversion < 50)
    throw std::runtime_error("unsupported archive3dm version");
  return decoded;
}

std::string Base64(const RH3DM_Bytes& bytes)
{
  return bytes.size() > 0 ? base64_encode(bytes.data(), (unsigned int)bytes.size()) : std::string();
}

ON_Xform ToYUp() { ON_Xform x(1); x.RotationZYX(0.0, 0.0, -ON_PI / 2.0); return x; }

ON_Xform FromYUp() { ON_Xform x(1); x.RotationZYX(0.0, 0.0, ON_PI / 2.0); return x; }

std::vector<float> Colors3(const ON_SimpleArray<ON_Color>& c)
{
  std::vector<float> out((size_t)c.Count() * 3);
  for (int i = 0; i < c.Count(); i++)
  {
    out[(size_t)i * 3] = c[i].Red() / 255.0f;
    out[(size_t)i * 3 + 1] = c[i].Green() / 255.0f;
    out[(size_t)i * 3 + 2] = c[i].Blue() / 255.0f;
  }
  return out;
}

std::vector<double> Buffer(const BND_DICT& b, const char* key, int per, int count)
{
  std::vector<double> v = RH3DM_DictGetNumbers(b, key);
  if (v.size() % (size_t)per != 0 || (count > 0 && !v.empty() && v.size() != (size_t)count * (size_t)per))
    throw std::invalid_argument(std::string("three.js buffers: '") + key + "' has " + std::to_string(v.size())
                                + " numbers, expected " + (count > 0 ? std::to_string(count * per) : "a multiple of " + std::to_string(per)));
  return v;
}

ON_Color ColorAt(const std::vector<double>& c, int i)
{
  auto byte = [](double x) { return (int)(x <= 0 ? 0 : x >= 1 ? 255 : x * 255.0 + 0.5); };
  return ON_Color(byte(c[(size_t)i * 3]), byte(c[(size_t)i * 3 + 1]), byte(c[(size_t)i * 3 + 2]));
}

BND_DICT JsonAttribute(int itemSize, const std::vector<double>& values)
{
  BND_DICT a = RH3DM_DictNew();
  RH3DM_DictSet(a, "itemSize", itemSize);
  RH3DM_DictSet(a, "type", std::string("Float32Array"));
  RH3DM_DictSetList(a, "array", values);
  return a;
}

std::vector<double> JsonColors(const ON_SimpleArray<ON_Color>& c)
{
  std::vector<double> out((size_t)c.Count() * 3);
  for (int i = 0; i < c.Count(); i++)
  {
    out[(size_t)i * 3] = c[i].Red() / 255.0;
    out[(size_t)i * 3 + 1] = c[i].Green() / 255.0;
    out[(size_t)i * 3 + 2] = c[i].Blue() / 255.0;
  }
  return out;
}

BND_DICT JsonGeometry(const BND_DICT* index, const BND_DICT& attributes)
{
  BND_DICT data = RH3DM_DictNew();
  if (index)
    RH3DM_DictSet(data, "index", *index);
  RH3DM_DictSet(data, "attributes", attributes);
  BND_DICT rc = RH3DM_DictNew();
  RH3DM_DictSet(rc, "data", data);
  return rc;
}

std::vector<double> JsonArray(const BND_DICT& attributes, const char* key, int* itemSize)
{
  if (!RH3DM_DictHas(attributes, key))
    return {};
  const BND_DICT a = RH3DM_DictGet<BND_DICT>(attributes, key);
  if (itemSize)
    *itemSize = RH3DM_DictGet<int>(a, "itemSize");
  return RH3DM_DictGetNumbers(a, "array");
}

void JsonColorsTo(const std::vector<double>& c, int channels, ON_SimpleArray<ON_Color>& out)
{
  if (channels != 3 && channels != 4)
    throw std::invalid_argument("three.js JSON: 'color' itemSize must be 3 or 4");
  for (size_t i = 0; i + channels <= c.size(); i += channels)
  {
    const int r = (int)(c[i] * 255.0), g = (int)(c[i + 1] * 255.0), b = (int)(c[i + 2] * 255.0);
    out.Append(channels == 4 ? ON_Color(r, g, b, 255 - (int)(c[i + 3] * 255.0)) : ON_Color(r, g, b));
  }
}

void AddRenderContent(ONX_Model* model, const BND_File3dmRenderContent& content)
{
  if (nullptr == model)
    throw std::runtime_error("Rhino.FileIO.File3dm: the model is gone");
  model->AddModelComponent(*content.RH3DM_Const());
}

}
