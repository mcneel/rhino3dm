
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initRenderChannelsBindings(rh3dmpymodule& m);
#else
void initRenderChannelsBindings(void* m);
#endif

class BND_File3dmRenderChannels
{
private:
  ON_RenderChannels* _rch = nullptr;
  bool _owned = false;

public:
  BND_File3dmRenderChannels();
  BND_File3dmRenderChannels(const BND_File3dmRenderChannels& rch);
  BND_File3dmRenderChannels(ON_RenderChannels* rch);
  ~BND_File3dmRenderChannels() { if (_owned) delete _rch; }

public:
  ON_RenderChannels::Modes GetMode() const { return _rch->Mode(); }
  void SetMode(ON_RenderChannels::Modes v) { _rch->SetMode(v); }

  BND_TUPLE GetCustomList() const;
  std::vector<BND_UUID> GetCustomList2() const;
  void SetCustomList(BND_TUPLE v);
};
