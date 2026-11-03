#include "bindings.h"

const std::string version = ON::VersionQuartetAsString();

#if defined(ON_PYTHON_COMPILE)

#if defined(RHINOCORE_BINDINGS)
RH3DM_PYTHON_BINDING(rhinocore_py_bindings, m) {
  m.doc() = "rhinocore python package. Direc in-rhino C++ class bindings";
  m.attr("Version") = py::cast(version);
#else
RH3DM_PYTHON_BINDING(_rhino3dm, m) {
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";
  m.attr("Version") = py::cast(version);
#endif

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

EMSCRIPTEN_BINDINGS(rhino3dm) {
  emscripten::constant("Version", version);
  void* m = nullptr;
#endif

  ON::Begin();
  // every class, enum and hand-maintained member: tools/generate/bnd_gen.py (bnd_init_gen.cpp)
#if defined(ON_PYTHON_COMPILE)
  { extern void initGeneratedBindings(rh3dmpymodule& m); initGeneratedBindings(m); }
#else
  { extern void initGeneratedBindings(void); initGeneratedBindings(); (void)m; }
#endif
}

#if defined(ON_PYTHON_COMPILE)
std::string ToStdString(const py::str& str)
{
  std::string rc = py::cast<std::string>(str);
  return rc;
}
#endif
  
BND_TUPLE CreateTuple(int count)
{
#if defined(ON_PYTHON_COMPILE)
  BND_TUPLE rc = py::steal<py::tuple>(PyTuple_New(count));   // filled by SetTuple / RH3DM_TupleSetObj
#else
  emscripten::val rc(emscripten::val::array());
#endif
  return rc;
}

BND_TUPLE NullTuple()
{
#if defined(ON_PYTHON_COMPILE)
  return py::tuple();
#else
  return emscripten::val::null();
#endif
}

BND_LIST CreateList()
{
#if defined(ON_PYTHON_COMPILE)
  BND_LIST rc = py::list();
#else
  emscripten::val rc(emscripten::val::array());
#endif
  return rc;
}

BND_LIST NullList()
{
#if defined(ON_PYTHON_COMPILE)
  return py::list();
#else
  return emscripten::val::null();
#endif
}

