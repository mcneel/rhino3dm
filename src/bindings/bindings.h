#pragma once

// no need to export RH_C_FUNCTION in these libraries
#define RH_C_FUNCTION

#if defined(__EMSCRIPTEN__)
#define ON_WASM_COMPILE
#else
#define ON_PYTHON_COMPILE
#if defined(NANOBIND)
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
#define def_property def_prop_rw
#define def_property_readonly def_prop_ro
#define def_readonly def_ro
#define def_readwrite def_rw
#define def_property_readonly_static def_prop_ro_static
#define import import_
#define UNIMPLEMENTED_EXCEPTION throw std::exception()

#else
  #include <pybind11/pybind11.h>
  #include <pybind11/stl.h>
  #include <pybind11/operators.h>
  namespace py = pybind11;
  #define RH3DM_PYTHON_BINDING(name, variable) PYBIND11_MODULE(name, variable)
  typedef pybind11::module rh3dmpymodule;
#endif

std::string ToStdString(const py::str& str);
  #include "datetime.h"
  #pragma comment(lib, "rpcrt4.lib")
  #pragma comment(lib, "shlwapi.lib")
#endif

#if defined(RHINOCORE_BINDINGS)
#include "../rhinocore_bindings/stdafx.h"
#else
#include "../lib/opennurbs/opennurbs.h"
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
template<typename T>
void SetTuple(BND_TUPLE& tuple, int index, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
  UNIMPLEMENTED_EXCEPTION;
#else
  tuple[index] = value;
#endif
#else
  // embind (emscripten >= ~4.0) forbids implicitly marshaling registered-class
  // raw pointers into a val. Route through the policy-aware val constructor so
  // pointer values (incl. polymorphic wrappers) are wrapped exactly as a
  // function return with allow_raw_pointers() would be. The policy is harmless
  // for non-pointer values.
  tuple.set(index, emscripten::val(value, emscripten::allow_raw_pointers()));
#endif
}

//buffer_info for nanobind
#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)

struct buffer_info {
  std::string format;
  std::vector<ssize_t> shape;
  std::vector<ssize_t> strides;

  buffer_info(
      std::string format,
      std::vector<ssize_t> shape_in,
      std::vector<ssize_t> strides_in)
      : format(std::move(format)),
        shape(std::move(shape_in)),
        strides(std::move(strides_in)) {}

  buffer_info(const buffer_info&) = delete;
  buffer_info& operator=(const buffer_info&) = delete;

  buffer_info(buffer_info&& other) noexcept {
    (*this) = std::move(other);
  }

  buffer_info& operator=(buffer_info&& rhs) noexcept {
    format = std::move(rhs.format);
    shape = std::move(rhs.shape);
    strides = std::move(rhs.strides);
    return *this;
  }
};


#endif

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

BND_DateTime CreateDateTime(struct tm t);

#include "bnd_color.h"
#include "bnd_uuid.h"

