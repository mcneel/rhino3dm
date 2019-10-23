#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initObjectBindings(pybind11::module& m);
#else
void initObjectBindings(void* m);
#endif

class BND_CommonObject
{
protected:
  BND_CommonObject();
  void SetTrackedPointer(ON_Object* obj, const ON_ModelComponentReference* compref);
public:
  virtual ~BND_CommonObject();

  static BND_CommonObject* CreateWrapper(ON_Object* obj, const ON_ModelComponentReference* compref);
  static BND_CommonObject* CreateWrapper(const ON_ModelComponentReference& compref);

  BND_DICT Encode() const;
  static BND_CommonObject* Decode(BND_DICT jsonObject);

#if defined(__EMSCRIPTEN__)
  BND_DICT toJSON(BND_DICT key);
#endif

  bool SetUserString(std::wstring key, std::wstring value);
  std::wstring GetUserString(std::wstring key);
  int UserStringCount() const { return m_object->UserStringCount(); }
  BND_TUPLE GetUserStrings() const;
  std::wstring RdkXml() const;
protected:
  ON_ModelComponentReference m_component_ref; // holds shared pointer for this class

private:
  BND_CommonObject(ON_Object* obj, const ON_ModelComponentReference* compref);
  ON_Object* m_object = nullptr;
};

class BND_ArchivableDictionary
{
public:
  static BND_DICT EncodeFromDictionary(BND_DICT dict);
  static BND_DICT DecodeToDictionary(BND_DICT jsonObject);

#if defined(__EMSCRIPTEN__)
  static void WriteGeometry(class BND_GeometryBase* geometry);
#endif

};

enum class ItemType : int
{
  // values <= 0 are considered bogus
  // each supported object type has an associated ItemType enum value
  // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
  // NEVER EVER Change ItemType values as this will break I/O code
  // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
  Undefined = 0,
  // some basic types
  Bool = 1, // bool
  Byte = 2, // unsigned char
  SByte = 3, // char
  Short = 4, // short
  UShort = 5, // unsigned short
  Int32 = 6, // int
  UInt32 = 7, // unsigned int
  Int64 = 8, // time_t
  Single = 9, // float
  Double = 10, // double
  Guid = 11,
  String = 12,

  // array of basic .NET data types
  ArrayBool = 13,
  ArrayByte = 14,
  ArraySByte = 15,
  ArrayShort = 16,
  ArrayInt32 = 17,
  ArraySingle = 18,
  ArrayDouble = 19,
  ArrayGuid = 20,
  ArrayString = 21,

  // System::Drawing structs
  Color = 22,
  Point = 23,
  PointF = 24,
  Rectangle = 25,
  RectangleF = 26,
  Size = 27,
  SizeF = 28,
  Font = 29,

  // RMA::OpenNURBS::ValueTypes structs
  Interval = 30,
  Point2d = 31,
  Point3d = 32,
  Point4d = 33,
  Vector2d = 34,
  Vector3d = 35,
  BoundingBox = 36,
  Ray3d = 37,
  PlaneEquation = 38,
  Xform = 39,
  Plane = 40,
  Line = 41,
  Point3f = 42,
  Vector3f = 43,

  // RMA::OpenNURBS classes
  OnBinaryArchiveDictionary = 44,
  OnObject = 45, // don't use this anymore
  OnMeshParameters = 46,
  OnGeometry = 47,
  OnObjRef = 48,
  ArrayObjRef = 49,
  ArrayGeometry = 50,
  MAXVALUE = 50
};
