// The neutral vocabulary hand members are written in (G123; tools/generate/gen_registry.py
// HAND_MEMBERS). Hand members are plain C++ over the generated classes and value types,
// registered by the generator in every binding; this shim is the ONLY file under src/hand/
// that names a binding (the generator checks the rest). The nanobind port (M2) of every hand
// member is this file plus tools/generate/emit_hand.py.
#pragma once

#include "bnd_common_gen.h"
#include <string>
#include <vector>

class BND_CommonObject;

// a JSON-like dictionary (BND_DICT): a Python dict / a JS object
BND_DICT RH3DM_DictNew();
bool RH3DM_DictHas(const BND_DICT& d, const char* key);
std::vector<std::string> RH3DM_DictKeys(const BND_DICT& d);
// a missing key: Python KeyError, JS Error
[[noreturn]] void RH3DM_ThrowKeyError(const char* key);

template <class T>
void RH3DM_DictSet(BND_DICT& d, const char* key, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
  d[key] = value;
#else
  d.set(key, emscripten::val(value));
#endif
}

template <class T>
T RH3DM_DictGet(const BND_DICT& d, const char* key)
{
  if (!RH3DM_DictHas(d, key))
    RH3DM_ThrowKeyError(key);
#if defined(ON_PYTHON_COMPILE)
  return py::cast<T>(d[key]);
#else
  return d[key].template as<T>();
#endif
}

// a sequence value: a Python list / a JS array
template <class T>
void RH3DM_DictSetList(BND_DICT& d, const char* key, const std::vector<T>& values)
{
#if defined(ON_PYTHON_COMPILE)
  py::list l;
  for (const T& v : values) l.append(v);
  d[key] = l;
#else
  emscripten::val a = emscripten::val::array();
  for (size_t i = 0; i < values.size(); i++) a.set((unsigned)i, emscripten::val(values[i]));
  d.set(key, a);
#endif
}

// a fixed-size record of numbers: a Python tuple / a JS array
template <class T>
void RH3DM_DictSetTuple(BND_DICT& d, const char* key, const std::vector<T>& values)
{
#if defined(ON_PYTHON_COMPILE)
  py::tuple t(values.size());
  for (size_t i = 0; i < values.size(); i++) t[i] = values[i];
  d[key] = t;
#else
  RH3DM_DictSetList(d, key, values);
#endif
}

// a sequence argument: a Python list (converted by the binding, so overloads still resolve) / a
// JS array; RH3DM_SeqVec reads either as a std::vector
#if defined(ON_PYTHON_COMPILE)
template <class T> using RH3DM_SeqArg = std::vector<T>;
template <class T> std::vector<T> RH3DM_SeqVec(const RH3DM_SeqArg<T>& s) { return s; }
#else
template <class T> using RH3DM_SeqArg = emscripten::val;
template <class T> std::vector<T> RH3DM_SeqVec(const emscripten::val& s) { return RH3DM_JsSeq<T>(s); }
#endif

// a sequence of class objects, read as pointers to them (a Python list / a JS array); valid while the
// argument is
#if defined(ON_PYTHON_COMPILE)
template <class T> using RH3DM_SeqPtrArg = std::vector<const T*>;
template <class T> std::vector<const T*> RH3DM_SeqPtrVec(const RH3DM_SeqPtrArg<T>& s) { return s; }
#else
template <class T> using RH3DM_SeqPtrArg = emscripten::val;
template <class T> std::vector<const T*> RH3DM_SeqPtrVec(const emscripten::val& s) { return RH3DM_JsSeqPtr<T>(s); }
#endif

// a hand property returning NEW class objects (G129: the SubD neighbours), returned as generated
// sequences of new objects are (SubDVertex.Edges): a Python list whose items each keep `self` alive /
// a JS array. Registered by the generator for hand members marked `list`.
#if defined(ON_PYTHON_COMPILE)
template <auto F, class S> py::list RH3DM_ListOf(const S& self) { return RH3DM_PyOwnedList(F(self), py::cast(&self)); }
#else
template <auto F, class S> emscripten::val RH3DM_ListOf(const S& self) { return RH3DM_JsArray(F(self)); }
#endif

// number arrays in a dictionary (G131: three.js buffers): set as a JS Float32Array / Uint32Array (a
// Python list), read from a typed array or a plain array of numbers (a Python sequence); a missing
// key reads as empty
void RH3DM_DictSetFloats(BND_DICT& d, const char* key, const std::vector<float>& values);
void RH3DM_DictSetUints(BND_DICT& d, const char* key, const std::vector<unsigned int>& values);
std::vector<double> RH3DM_DictGetNumbers(const BND_DICT& d, const char* key);

// new objects the caller owns (nullptr: None / null)
void RH3DM_DictSetOwned(BND_DICT& d, const char* key, BND_CommonObject* owned);
void RH3DM_DictSetOwnedList(BND_DICT& d, const char* key, const std::vector<BND_CommonObject*>& owned);

// what a dictionary value is, as far as a hand member can store it
enum class RH3DM_ValueKind
{
  Other, Bool, Int, Double, String,
  Point2d, Point3d, Point4d, Vector2d, Vector3d,  // Python only: a JS value is a plain object
  Geometry, GeometryList                          // a GeometryBase / a non-empty sequence of them
};
RH3DM_ValueKind RH3DM_DictKind(const BND_DICT& d, const char* key);
// the geometry of a Geometry / GeometryList value
const ON_Geometry* RH3DM_DictGeometry(const BND_DICT& d, const char* key);
std::vector<const ON_Geometry*> RH3DM_DictGeometryList(const BND_DICT& d, const char* key);

// a deprecated hand member was called (G133: the three.js JSON family): warns once per member -- a
// Python DeprecationWarning / a JS console.warn -- naming its replacement
void RH3DM_Deprecated(const char* member, const char* instead);

// helpers several classes' hand-maintained members share (moved out of the per-class files, M3)
class BND_File3dmRenderContent;
struct BND_Point3d;
namespace bnd_shared
{

// ---- F2: values ------------------------------------------------------------

template <class P>
BND_DICT XY(const P& p)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "X", (double)p.m_x);
  RH3DM_DictSet(d, "Y", (double)p.m_y);
  return d;
}

template <class P>
BND_DICT XYZ(const P& p)
{
  BND_DICT d = XY(p);
  RH3DM_DictSet(d, "Z", (double)p.m_z);
  return d;
}

BND_Point3d PointFromDict(const BND_DICT& d);

// ---- archives (the old bindings' helpers, private here: their C names collide) ---------

// what the old Encode writes: Rhino 6 archives, the opennurbs version for that
const int kArchive3dm = 60;

void SetVersions(BND_DICT& d);

std::string Base64(const ON_Write3dmBufferArchive& archive);

// the "data" of an encoded dict, decoded; throws when it is not an archive the old Decode read
std::string ArchiveBytes(const BND_DICT& jsonObject, int& rhinoversion, int& opennurbsversion);

std::string Base64(const RH3DM_Bytes& bytes);

ON_Xform ToYUp();
ON_Xform FromYUp();

std::vector<float> Colors3(const ON_SimpleArray<ON_Color>& c);

template <class P>
std::vector<float> Floats3(const ON_SimpleArray<P>& a)
{
  std::vector<float> out((size_t)a.Count() * 3);
  for (int i = 0; i < a.Count(); i++)
  {
    out[(size_t)i * 3] = (float)a[i].x;
    out[(size_t)i * 3 + 1] = (float)a[i].y;
    out[(size_t)i * 3 + 2] = (float)a[i].z;
  }
  return out;
}

// a buffer of `per` numbers an element, `count` elements (0: any whole number of elements)
std::vector<double> Buffer(const BND_DICT& b, const char* key, int per, int count);

ON_Color ColorAt(const std::vector<double>& c, int i);

// ---- the deprecated JSON family (G133) ----

// {itemSize, type: "Float32Array", array: [...]}
BND_DICT JsonAttribute(int itemSize, const std::vector<double>& values);

template <class P>
std::vector<double> Doubles3(const ON_SimpleArray<P>& a)
{
  std::vector<double> out((size_t)a.Count() * 3);
  for (int i = 0; i < a.Count(); i++)
  {
    out[(size_t)i * 3] = a[i].x;
    out[(size_t)i * 3 + 1] = a[i].y;
    out[(size_t)i * 3 + 2] = a[i].z;
  }
  return out;
}

std::vector<double> JsonColors(const ON_SimpleArray<ON_Color>& c);

// {data: {[index,] attributes}}
BND_DICT JsonGeometry(const BND_DICT* index, const BND_DICT& attributes);

// an attribute's `array` (empty when the attribute is missing), and its itemSize
std::vector<double> JsonArray(const BND_DICT& attributes, const char* key, int* itemSize = nullptr);

// 8.x: each channel truncated to a byte; a 4th channel is opacity (opennurbs' alpha is transparency)
void JsonColorsTo(const std::vector<double>& c, int channels, ON_SimpleArray<ON_Color>& out);

void AddRenderContent(ONX_Model* model, const BND_File3dmRenderContent& content);

// the neighbours of a component as new generated components of `owner`, in opennurbs' order
// (null slots skipped)
template <class T, class C, class N>
std::vector<T*> Neighbours(ON_SubD* owner, const C* c, unsigned int count, const N* (C::*at)(unsigned int) const)
{
  std::vector<T*> out;
  out.reserve(count);
  for (unsigned int i = 0; i < count; i++)
  {
    const N* n = (c->*at)(i);
    if (nullptr != n)
      out.push_back(new T(owner, n->m_id));
  }
  return out;
}
}
