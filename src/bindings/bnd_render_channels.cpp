
#include "bindings.h"

BND_File3dmRenderChannels::BND_File3dmRenderChannels()
{
}

BND_File3dmRenderChannels::BND_File3dmRenderChannels(ON_RenderChannels* rch)
{
  SetTrackedPointer(rch);
}

BND_TUPLE BND_File3dmRenderChannels::GetCustomList(void) const
{
  ON_SimpleArray<ON_UUID> list;
  m_render_channels->GetCustomList(list);

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

  for (auto elem: tuple)
  {
    list.Append(Binding_to_ON_UUID(elem.cast<BND_UUID>()));
  }

  m_render_channels->SetCustomList(list);
}

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
    .constructor<const BND_File3dmRenderChannels&>()
    .property("Mode", &BND_File3dmRenderChannels::GetMode, &BND_File3dmRenderChannels::SetMode)
    .property("CustomIds", &BND_File3dmRenderChannels::GetCustomList, &BND_File3dmRenderChannels::SetCustomList)
    ;
}
#endif
