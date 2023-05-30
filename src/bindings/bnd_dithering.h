
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
  //bool _owned = false;

public:
  BND_File3dmDithering() = default;
  BND_File3dmDithering(ON_Dithering* dit);
  

  /*
  BND_File3dmDithering(ON_Dithering* dit) : _dit(dit) { }
  BND_File3dmDithering() { _dit = new ON_Dithering; _owned = true; }
  BND_File3dmDithering(const BND_File3dmDithering& dit) { _dit = new ON_Dithering(*dit._dit); _owned = true; }
  ~BND_File3dmDithering() { if (_owned) delete _dit; }
  */

  bool GetOn() const { return _dit->On(); }
  void SetOn(bool v) { _dit->SetOn(v); }

  ON_Dithering::Methods GetMethod() const { return _dit->Method(); }
  void SetMethod(ON_Dithering::Methods v) { _dit->SetMethod(v); }
};
