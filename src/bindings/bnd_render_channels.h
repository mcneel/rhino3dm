
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

protected:
  void SetTrackedPointer(ON_RenderChannels* rch) { m_render_channels = rch; }

public:
  BND_File3dmRenderChannels();
  BND_File3dmRenderChannels(ON_RenderChannels* rch);

  ON_RenderChannels::Modes GetMode(void) const { return m_render_channels->Mode(); }
  void SetMode(ON_RenderChannels::Modes v) { m_render_channels->SetMode(v); }

  BND_TUPLE GetCustomList(void) const;
  void SetCustomList(BND_TUPLE v);
};
