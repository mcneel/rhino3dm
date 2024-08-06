#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLineCurveBindings(rh3dmpymodule& m);
#else
void initLineCurveBindings(void* m);
#endif

class BND_LineCurve : public BND_Curve
{
  ON_LineCurve* m_linecurve = nullptr;
public:
  BND_LineCurve();
  BND_LineCurve(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref);
  BND_LineCurve(ON_3dPoint start, ON_3dPoint end);

  ON_Line GetLine() const { return m_linecurve->m_line; }
  void SetLine(const ON_Line& line) { m_linecurve->m_line = line; }
protected:
  void SetTrackedPointer(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref);
};
