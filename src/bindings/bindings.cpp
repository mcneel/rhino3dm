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

#if defined(NANOBIND)
  BND_TUPLE rc = py::tuple();
#else
  BND_TUPLE rc = py::tuple(count);
#endif
#else
  emscripten::val rc(emscripten::val::array());
#endif
  return rc;
}

BND_TUPLE NullTuple()
{
#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
  return py::tuple();
#else
  return py::none();
#endif
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
#if defined(NANOBIND)
  return py::list();
#else
  return py::none();
#endif
#else
  return emscripten::val::null();
#endif
}

BND_DateTime CreateDateTime(struct tm t)
{
#if defined(ON_PYTHON_COMPILE)
  if (!PyDateTimeAPI) {
    PyDateTime_IMPORT;
  }
  return PyDateTime_FromDateAndTime(t.tm_year + 1900,
                                    t.tm_mon + 1,
                                    t.tm_mday,
                                    t.tm_hour,
                                    t.tm_min,
                                    t.tm_sec,
                                    0);
#else
  emscripten::val Date = emscripten::val::global("Date");
  return Date.new_(t.tm_year + 1900,
                   t.tm_mon,
                   t.tm_mday,
                   t.tm_hour,
                   t.tm_min,
                   t.tm_sec,
                   0);
#endif
}
