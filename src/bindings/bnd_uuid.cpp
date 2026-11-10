#include "bindings.h"
#include <atomic>

#if defined(ON_PYTHON_COMPILE)

// uuid.UUID, looked up once. An atomic pointer, not a py::object: free-threaded Python (M2) may run
// the first lookup on two threads at once -- the loser drops its reference -- and a leaked static
// reference is never destroyed after the interpreter has finalized
static std::atomic<PyObject*> make_uuid_ptr{nullptr};
BND_UUID ON_UUID_to_Binding(const ON_UUID& id)
{
  PyObject* f = make_uuid_ptr.load(std::memory_order_acquire);
  if (f == nullptr)
  {
    rh3dmpymodule uuid_module = rh3dmpymodule::import_("uuid");
    py::object type = uuid_module.attr("UUID");
    PyObject* mine = type.release().ptr();
    PyObject* expected = nullptr;
    if (make_uuid_ptr.compare_exchange_strong(expected, mine, std::memory_order_acq_rel))
      f = mine;
    else
    {
      Py_DECREF(mine);
      f = expected;
    }
  }
  py::handle make_uuid(f);
  char s[37];
  memset(s, 0, sizeof(s));

  char* suuid = ON_UuidToString(id, s);
  py::object guid = make_uuid(suuid);
  return guid;
}

ON_UUID Binding_to_ON_UUID(const BND_UUID& id)
{
  // str(uuid): the 36-character form ON_UuidFromString reads
  std::string s = py::cast<std::string>(py::str(id));
  return ON_UuidFromString(s.c_str());
}

std::vector<BND_UUID> ON_SimpleArrayUUID_to_Binding(const ON_SimpleArray<ON_UUID>& uuids)
{
	std::vector<BND_UUID> guids;
	for (int i = 0; i < uuids.Count(); i++)
	{
		guids.push_back(ON_UUID_to_Binding(uuids[i]));
	}

	return guids;
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

ON_UUID Binding_to_ON_UUID(const BND_UUID& id)
{
  return ON_UuidFromString(id.c_str());
}
#endif
