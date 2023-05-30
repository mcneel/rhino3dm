
#include "bindings.h"

//#if !defined ON_WASM_COMPILE // Why do I need this?

BND_TUPLE BND_File3dmRenderChannels::GetCustomList() const
{
  ON_SimpleArray<ON_UUID> list;
  _rch->GetCustomList(list);

  const int count = list.Count();
  auto tuple = CreateTuple(count);
  for (int i = 0; i < count; i++)
  {
    SetTuple(tuple, i, ON_UUID_to_Binding(list[i]));
  }

  return tuple;
}

void BND_File3dmRenderChannels::SetCustomList(BND_TUPLE tuple)
{
  ON_SimpleArray<ON_UUID> list;

  // John C - compiler complaining here. are you trying to use BND_Tuple?
  /*
  for (auto elem: tuple)
  {
    list.Append(Binding_to_ON_UUID(elem.cast<BND_UUID>()));
  }

  _rch->SetCustomList(list);
  */
}

//#endif

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initRenderChannelsBindings(pybind11::module& m)
{
  py::class_<BND_File3dmRenderChannels>(m, "RenderChannels")
    .def(py::init<>())
    .def(py::init<const BND_File3dmRenderChannels&>(), py::arg("other"))
    .def_property("Mode", &BND_File3dmRenderChannels::GetMode, &BND_File3dmRenderChannels::SetMode)
    .def_property("CustomIds", &BND_File3dmRenderChannels::GetCustomList, &BND_File3dmRenderChannels::SetCustomList)
   ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRenderChannelsBindings(void*)
{
  class_<BND_File3dmRenderChannels>("RenderChannels")
    .constructor<>()
    //.constructor<const BND_File3dmRenderChannels&>()
    .property("mode", &BND_File3dmRenderChannels::GetMode, &BND_File3dmRenderChannels::SetMode)
    .property("customIds", &BND_File3dmRenderChannels::GetCustomList, &BND_File3dmRenderChannels::SetCustomList)
    ;
}
#endif
