
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initRenderEnvironmentsBindings(pybind11::module& m);
#else
void initRenderEnvironmentsBindings(void* m);
#endif

class BND_File3dmRenderEnvironments
{
private:
  ON_3dmRenderSettings* _rs = nullptr;
  bool _owned = false;

public:
  BND_File3dmRenderEnvironments() { _rs = new ON_3dmRenderSettings; _owned = true; }
  BND_File3dmRenderEnvironments(const BND_File3dmRenderEnvironments& re) { _rs = new ON_3dmRenderSettings(*re._rs); _owned = true; }
  BND_File3dmRenderEnvironments(ON_3dmRenderSettings* rs) : _rs(rs) { }
  ~BND_File3dmRenderEnvironments() { if (_owned) delete _rs; }

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
