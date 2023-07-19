#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPolyCurveBindings(pybind11::module& m);
#else
void initPolyCurveBindings(void* m);
#endif

class BND_PolyCurve : public BND_Curve
{
  ON_PolyCurve* m_polycurve = nullptr;
public:
  BND_PolyCurve();
  BND_PolyCurve(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref);

  int SegmentCount() const { return m_polycurve->Count(); }
  BND_Curve* SegmentCurve(int index) const;
  bool IsNested() const { return m_polycurve->IsNested(); }
  bool HasGap() const { return m_polycurve->FindNextGap(0); }
  bool RemoveNesting() { return m_polycurve->RemoveNesting(); }
  std::vector<BND_Curve*> Explode() const;
  bool Append1(const ON_Line& line);
  bool Append2(BND_Arc& arc);
  bool Append3(const BND_Curve& curve);
  bool AppendSegment(const BND_Curve& curve);
  double SegmentCurveParameter(double polycurveParameter) const;
  double PolyCurveParameter(int segmentIndex, double segmentCurveParameter) const;
  BND_Interval SegmentDomain(int segmentIndex) const;
  int SegmentIndex(double polycurveParameter) const;
  //int SegmentIndexes(BND_Interval subdomain, out int segmentIndex0, out int segmentIndex1)

protected:
  void SetTrackedPointer(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref);
};
