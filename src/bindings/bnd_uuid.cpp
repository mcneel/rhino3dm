#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)

static pybind11::object make_uuid;
BND_UUID ON_UUID_to_Binding(const ON_UUID& id)
{
  if (make_uuid.ptr() == nullptr)
  {
    pybind11::module uuid_module = pybind11::module::import("uuid");
    make_uuid = uuid_module.attr("UUID");
  }
  char s[37];
  memset(s, 0, sizeof(s));

  char* suuid = ON_UuidToString(id, s);
  pybind11::object guid = make_uuid(suuid);
  return guid;
}

#endif

#if defined(ON_WASM_COMPILE)
BND_UUID ON_UUID_to_Binding(const ON_UUID& id)
{
  char s[37];
  memset(s, 0, sizeof(s));

  char* suuid = ON_UuidToString(id, s);
  std::string rc(suuid);
  return rc;
}
#endif
