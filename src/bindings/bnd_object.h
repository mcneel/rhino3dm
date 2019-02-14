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

#if defined(__EMSCRIPTEN__)
  emscripten::val toJSON(emscripten::val key);
  emscripten::val Encode() const;
  static BND_CommonObject* Decode(emscripten::val jsonObject);
#endif

#if defined(ON_PYTHON_COMPILE)
  pybind11::dict Encode() const;
  static BND_CommonObject* Decode(pybind11::dict jsonObject);
#endif

  bool SetUserString(std::wstring key, std::wstring value);
  std::wstring GetUserString(std::wstring key);
  int UserStringCount() const { return m_object->UserStringCount(); }
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple GetUserStrings() const;
#endif
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
#if defined(ON_PYTHON_COMPILE)
  static pybind11::dict EncodeFromDictionary(pybind11::dict dict);
  static pybind11::dict DecodeToDictionary(pybind11::dict jsonObject);
#endif

};
