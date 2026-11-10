#pragma once

// no need to export RH_C_FUNCTION in these libraries
#define RH_C_FUNCTION

#if defined(__EMSCRIPTEN__)
#define ON_WASM_COMPILE
#else
#define ON_PYTHON_COMPILE
  #include <nanobind/nanobind.h>
  #include <nanobind/stl/string.h>
  #include <nanobind/stl/wstring.h>
  #include <nanobind/stl/tuple.h>
  #include <nanobind/operators.h>
  #include <nanobind/stl/vector.h>
  #include <vector>
  #include <tuple>
  namespace py = nanobind;
  typedef nanobind::module_ rh3dmpymodule;
#define RH3DM_PYTHON_BINDING(name, variable) NB_MODULE(name, variable)
// Python is nanobind (M2, docs/vnext/nanobind-port.md); `py` is the namespace prefix everywhere
#define UNIMPLEMENTED_EXCEPTION throw std::exception()


std::string ToStdString(const py::str& str);
  #pragma comment(lib, "rpcrt4.lib")
  #pragma comment(lib, "shlwapi.lib")
#endif

#if defined(RHINOCORE_BINDINGS)
#include "../rhinocore_bindings/stdafx.h"
#else
#include "../lib/opennurbs/opennurbs.h"
#endif

// <windows.h> (through opennurbs) still defines the 16-bit `near` and `far` as empty macros, which
// eat RhinoCommon's parameter names (ViewportInfo.GetScreenPort(out int near, out int far)) in the
// generated code. Nothing here uses the macros.
#if defined(_WIN32)
#undef near
#undef far
#endif

#if defined(ON_WASM_COMPILE)
#include <emscripten/bind.h>
#include <emscripten/emscripten.h>
#endif

#if defined(ON_PYTHON_COMPILE)
typedef py::dict BND_DICT;
typedef py::tuple BND_Color;
typedef py::tuple BND_TUPLE;
typedef py::handle BND_DateTime;
typedef py::list BND_LIST;
#endif

#if defined(ON_WASM_COMPILE)
typedef emscripten::val BND_DICT;
typedef emscripten::val BND_Color;
typedef emscripten::val BND_TUPLE;
typedef emscripten::val BND_DateTime;
typedef emscripten::val BND_LIST;
#endif

BND_TUPLE CreateTuple(int count);
BND_TUPLE NullTuple();
#if defined(ON_PYTHON_COMPILE)
// fill item `index` of a tuple from CreateTuple
inline void RH3DM_TupleSetObj(BND_TUPLE& tuple, int index, py::object value)
{
  if (PyTuple_SetItem(tuple.ptr(), index, value.release().ptr()) != 0)
    throw py::python_error();
}
#endif
template<typename T>
void SetTuple(BND_TUPLE& tuple, int index, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
  // nanobind's tuple is immutable; CreateTuple made a fresh one (PyTuple_New), which the C API fills
  RH3DM_TupleSetObj(tuple, index, py::cast(value));
#else
  // embind (emscripten >= ~4.0) forbids implicitly marshaling registered-class
  // raw pointers into a val. Route through the policy-aware val constructor so
  // pointer values (incl. polymorphic wrappers) are wrapped exactly as a
  // function return with allow_raw_pointers() would be. The policy is harmless
  // for non-pointer values.
  tuple.set(index, emscripten::val(value, emscripten::allow_raw_pointers()));
#endif
}

BND_LIST CreateList(int count);
BND_LIST NullList();
template<typename T>
void Insert(BND_LIST& list, int index, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
  list.insert(index, value);
#else
  list.set(index, emscripten::val(value, emscripten::allow_raw_pointers()));
#endif
}

template<typename T>
void Append(BND_LIST& list, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
  list.append(value);
#else
  int count = list["length"].as<int>();
  list.set(count++, emscripten::val(value, emscripten::allow_raw_pointers()));
#endif
}


#include "bnd_color.h"
#include "bnd_uuid.h"

