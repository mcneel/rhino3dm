
#include "bindings.h"

using U = ON_3dmRenderSettings::EnvironmentUsage;
using P = ON_3dmRenderSettings::EnvironmentPurpose;

BND_File3dmRenderEnvironments::BND_File3dmRenderEnvironments()
{
  _rs = new ON_3dmRenderSettings;
  _owned = true;
}

BND_File3dmRenderEnvironments::BND_File3dmRenderEnvironments(const BND_File3dmRenderEnvironments& re)
{
   // see bnd_ground_plane.cpp for justification
  _rs = re._rs;

  if (re._owned)
  {
    // Tell the original owner that it no longer owns it.
    const_cast<BND_File3dmRenderEnvironments&>(re)._owned = false;

    // This object now owns it instead.
    _owned = true;
  }

  // Old code makes an actual copy of the native object -- which means changes don't stick.
  //_rs = new ON_3dmRenderSettings(*re._rs);
  //_owned = true;
}

BND_File3dmRenderEnvironments::BND_File3dmRenderEnvironments(ON_3dmRenderSettings* rs)
: _rs(rs)
{
}

BND_UUID BND_File3dmRenderEnvironments::GetBackgroundId() const
{
  return ON_UUID_to_Binding(_rs->RenderEnvironmentId(U::Background, P::Standard));
}

void BND_File3dmRenderEnvironments::SetBackgroundId(const BND_UUID& id)
{
  _rs->SetRenderEnvironmentId(U::Background, Binding_to_ON_UUID(id));
}

bool BND_File3dmRenderEnvironments::GetSkylightingOverride() const
{
  return _rs->RenderEnvironmentOverride(U::Skylighting);
}

void BND_File3dmRenderEnvironments::SetSkylightingOverride(bool on)
{
  _rs->SetRenderEnvironmentOverride(U::Skylighting, on);
}

BND_UUID BND_File3dmRenderEnvironments::GetSkylightingId() const
{
  return ON_UUID_to_Binding(_rs->RenderEnvironmentId(U::Skylighting, P::Standard));
}

void BND_File3dmRenderEnvironments::SetSkylightingId(const BND_UUID& id)
{
  _rs->SetRenderEnvironmentId(U::Skylighting, Binding_to_ON_UUID(id));
}

bool BND_File3dmRenderEnvironments::GetReflectionOverride() const
{
  return _rs->RenderEnvironmentOverride(U::Reflection);
}

void BND_File3dmRenderEnvironments::SetReflectionOverride(bool on)
{
  _rs->SetRenderEnvironmentOverride(U::Reflection, on);
}

BND_UUID BND_File3dmRenderEnvironments::GetReflectionId() const
{
  return ON_UUID_to_Binding(_rs->RenderEnvironmentId(U::Reflection, P::Standard));
}

void BND_File3dmRenderEnvironments::SetReflectionId(const BND_UUID& id)
{
  _rs->SetRenderEnvironmentId(U::Reflection, Binding_to_ON_UUID(id));
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)

void initRenderEnvironmentsBindings(rh3dmpymodule& m)
{
  py::class_<BND_File3dmRenderEnvironments>(m, "RenderEnvironments")
    .def(py::init<>())
    .def(py::init<const BND_File3dmRenderEnvironments&>(), py::arg("other"))
    .def_property("BackgroundId", &BND_File3dmRenderEnvironments::GetBackgroundId, &BND_File3dmRenderEnvironments::SetBackgroundId)
    .def_property("SkylightingId", &BND_File3dmRenderEnvironments::GetSkylightingId, &BND_File3dmRenderEnvironments::SetSkylightingId)
    .def_property("SkylightingOverride", &BND_File3dmRenderEnvironments::GetSkylightingOverride, &BND_File3dmRenderEnvironments::SetSkylightingOverride)
    .def_property("ReflectionId", &BND_File3dmRenderEnvironments::GetReflectionId, &BND_File3dmRenderEnvironments::SetReflectionId)
    .def_property("ReflectionOverride", &BND_File3dmRenderEnvironments::GetReflectionOverride, &BND_File3dmRenderEnvironments::SetReflectionOverride)
   ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRenderEnvironmentsBindings(void*)
{
  class_<BND_File3dmRenderEnvironments>("RenderEnvironments")
    //.constructor<>()
    //.constructor<const BND_File3dmRenderEnvironments&>()
    /*
    .property("backgroundId", &BND_File3dmRenderEnvironments::GetBackgroundId, &BND_File3dmRenderEnvironments::SetBackgroundId)
    .property("skylightingId", &BND_File3dmRenderEnvironments::GetSkylightingId, &BND_File3dmRenderEnvironments::SetSkylightingId)
    .property("skylightingOverride", &BND_File3dmRenderEnvironments::GetSkylightingOverride, &BND_File3dmRenderEnvironments::SetSkylightingOverride)
    .property("reflectionId", &BND_File3dmRenderEnvironments::GetReflectionId, &BND_File3dmRenderEnvironments::SetReflectionId)
    .property("reflectionOverride", &BND_File3dmRenderEnvironments::GetReflectionOverride, &BND_File3dmRenderEnvironments::SetReflectionOverride)
  */
    .property("backgroundId", &BND_File3dmRenderEnvironments::GetBackgroundId)
    .property("skylightingId", &BND_File3dmRenderEnvironments::GetSkylightingId)
    .property("skylightingOverride", &BND_File3dmRenderEnvironments::GetSkylightingOverride)
    .property("reflectionId", &BND_File3dmRenderEnvironments::GetReflectionId)
    .property("reflectionOverride", &BND_File3dmRenderEnvironments::GetReflectionOverride)
  ;
}
#endif
