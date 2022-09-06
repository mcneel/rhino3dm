
#include "bindings.h"

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

BND_UUID BND_File3dmRenderEnvironments::GetBackgroundId(void) const
{
  return ON_UUID_to_Binding(m_model->BackgroundRenderEnvironment());
}

void BND_File3dmRenderEnvironments::SetBackgroundId(const BND_UUID& id) const
{
  m_model->SetBackgroundRenderEnvironment(Binding_to_ON_UUID(id));
}

bool BND_File3dmRenderEnvironments::GetSkylightingOverride(void) const
{
  return m_model->SkylightingRenderEnvironmentOverride();
}

void BND_File3dmRenderEnvironments::SetSkylightingOverride(bool on) const
{
  m_model->SetSkylightingRenderEnvironmentOverride(on);
}

BND_UUID BND_File3dmRenderEnvironments::GetSkylightingId(void) const
{
  return ON_UUID_to_Binding(m_model->SkylightingRenderEnvironment());
}

void BND_File3dmRenderEnvironments::SetSkylightingId(const BND_UUID& id) const
{
  m_model->SetSkylightingRenderEnvironment(Binding_to_ON_UUID(id));
}

bool BND_File3dmRenderEnvironments::GetReflectionOverride(void) const
{
  return m_model->ReflectionRenderEnvironmentOverride();
}

void BND_File3dmRenderEnvironments::SetReflectionOverride(bool on) const
{
  m_model->SetReflectionRenderEnvironmentOverride(on);
}

BND_UUID BND_File3dmRenderEnvironments::GetReflectionId(void) const
{
  return ON_UUID_to_Binding(m_model->ReflectionRenderEnvironment());
}

void BND_File3dmRenderEnvironments::SetReflectionId(const BND_UUID& id) const
{
  m_model->SetReflectionRenderEnvironment(Binding_to_ON_UUID(id));
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

  // This will move to a different file. Not sure yet.
  py::class_<BND_File3dmRenderEnvironments>(m, "RenderEnvironments")
    .def(py::init<>())
    .def(py::init<const BND_File3dmRenderEnvironments&>(), py::arg("other"))
    .def_property("BackgroundId", &BND_File3dmRenderEnvironments::GetBackgroundId, &BND_File3dmRenderEnvironments::SetBackgroundId)
    .def_property("SkylightingOverride", &BND_File3dmRenderEnvironments::GetSkylightingOverride, &BND_File3dmRenderEnvironments::SetSkylightingOverride)
    .def_property("SkylightingId", &BND_File3dmRenderEnvironments::GetSkylightingId, &BND_File3dmRenderEnvironments::SetSkylightingId)
    .def_property("ReflectionOverride", &BND_File3dmRenderEnvironments::GetReflectionOverride, &BND_File3dmRenderEnvironments::SetReflectionOverride)
    .def_property("ReflectionId", &BND_File3dmRenderEnvironments::GetReflectionId, &BND_File3dmRenderEnvironments::SetReflectionId)
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
