
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDitheringBindings(pybind11::module& m);
#else
void initDitheringBindings(void* m);
#endif

class BND_File3dmDithering
{
public:
  ON_Dithering* m_dithering = nullptr;

protected:
  void SetTrackedPointer(ON_Dithering* dit) { m_dithering = dit; }

public:
  BND_File3dmDithering();
  BND_File3dmDithering(ON_Dithering* dit);

  bool GetOn(void) const { return m_dithering->On(); }
  void SetOn(bool v) const { m_dithering->SetOn(v); }

  ON_Dithering::Methods GetMethod(void) const { return m_dithering->Method(); }
  void SetMethod(ON_Dithering::Methods v) const { m_dithering->SetMethod(v); }
};
