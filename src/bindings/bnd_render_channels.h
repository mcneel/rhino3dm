
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initRenderChannelsBindings(pybind11::module& m);
#else
void initRenderChannelsBindings(void* m);
#endif

class BND_File3dmRenderChannels
{
public:
  ON_RenderChannels* m_render_channels = nullptr;

public:
  BND_File3dmRenderChannels() { }
  BND_File3dmRenderChannels(ON_RenderChannels* rch) : m_render_channels(rch) { }

  ON_RenderChannels::Modes GetMode(void) const { return m_render_channels->Mode(); }
  void SetMode(ON_RenderChannels::Modes v) { m_render_channels->SetMode(v); }

  BND_TUPLE GetCustomList(void) const;
  void SetCustomList(BND_TUPLE v);
};

// This will go in bnd_render_content.h once I pop the stash and get back to the render content stuff.
class BND_File3dmRenderEnvironments
{
public:
  ONX_Model* m_model = nullptr;

public:
  BND_File3dmRenderEnvironments() { }
  BND_File3dmRenderEnvironments(ONX_Model* m) : m_model(m) { }

  BND_UUID GetBackgroundId(void) const;
  void SetBackgroundId(const BND_UUID& id) const;
  bool GetSkylightingOverride(void) const;
  void SetSkylightingOverride(bool on) const;
  BND_UUID GetSkylightingId(void) const;
  void SetSkylightingId(const BND_UUID& id) const;
  bool GetReflectionOverride(void) const;
  void SetReflectionOverride(bool on) const;
  BND_UUID GetReflectionId(void) const;
  void SetReflectionId(const BND_UUID& id) const;
};
