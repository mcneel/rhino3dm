#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initObjectBindings(pybind11::module& m);
#endif

class BND_Object
{
protected:
  BND_Object();
  void SetTrackedPointer(ON_Object* obj, const ON_ModelComponentReference* compref);
public:
  virtual ~BND_Object();

  static BND_Object* CreateWrapper(ON_Object* obj, const ON_ModelComponentReference* compref);
  static BND_Object* CreateWrapper(const ON_ModelComponentReference& compref);

#if defined(__EMSCRIPTEN__)
  emscripten::val Encode() const;
  static BND_Object* Decode(emscripten::val jsonObject);
#endif

#if defined(ON_PYTHON_COMPILE)
  pybind11::dict Encode() const;
  static BND_Object* Decode(pybind11::dict jsonObject);
#endif

protected:
  ON_ModelComponentReference m_component_ref; // holds shared pointer for this class

private:
  BND_Object(ON_Object* obj, const ON_ModelComponentReference* compref);
  ON_Object* m_object = nullptr;
};
