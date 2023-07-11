
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initDitheringBindings(pybind11::module& m);
#else
void initDitheringBindings(void* m);
#endif

class BND_File3dmDithering
{
private:
  ON_Dithering* _dit = nullptr;
  bool _owned = false;

public:
  BND_File3dmDithering();
  BND_File3dmDithering(ON_Dithering* dit);
  BND_File3dmDithering(const BND_File3dmDithering& dit);
  ~BND_File3dmDithering() { if (_owned) delete _dit; }

  bool GetEnabled(void) const { return _dit->Enabled(); }
  void SetEnabled(bool v) const { _dit->SetEnabled(v); }

  ON_Dithering::Methods GetMethod() const { return _dit->Method(); }
  void SetMethod(ON_Dithering::Methods v) { _dit->SetMethod(v); }
};
