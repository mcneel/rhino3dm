
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initRenderEnvironmentsBindings(rh3dmpymodule& m);
#else
void initRenderEnvironmentsBindings(void* m);
#endif

class BND_File3dmRenderEnvironments
{
private:
  ON_3dmRenderSettings* _rs = nullptr;
  bool _owned = false;

public:
  BND_File3dmRenderEnvironments();
  BND_File3dmRenderEnvironments(ON_3dmRenderSettings* rs);
  BND_File3dmRenderEnvironments(const BND_File3dmRenderEnvironments& re);
  ~BND_File3dmRenderEnvironments() { if (_owned) delete _rs; }

  BND_UUID GetBackgroundId() const;
  void SetBackgroundId(const BND_UUID& id);

  bool GetSkylightingOverride() const;
  void SetSkylightingOverride(bool on);

  BND_UUID GetSkylightingId() const;
  void SetSkylightingId(const BND_UUID& id);

  bool GetReflectionOverride() const;
  void SetReflectionOverride(bool on);

  BND_UUID GetReflectionId() const;
  void SetReflectionId(const BND_UUID& id);
};
