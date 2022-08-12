
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDecalBindings(pybind11::module& m);
#else
void initDecalBindings(void* m);
#endif

class BND_File3dmDecal
{
public:
  ON_Decal* m_decal = nullptr;

protected:
  void SetTrackedPointer(ON_Decal* decal);

public:
  BND_File3dmDecal();
  BND_File3dmDecal(const BND_File3dmDecal& other);
  BND_File3dmDecal(ON_Decal* decal);

  BND_UUID ModelComponentId(void) const { return ON_UUID_to_Binding(m_decal->ModelComponentId()); }

  BND_UUID TextureInstanceId(void) const { return ON_UUID_to_Binding(m_decal->TextureInstanceId()); }
  void SetTextureInstanceId(BND_UUID v) const { m_decal->SetTextureInstanceId(Binding_to_ON_UUID(v)); }

  ON_Decal::Mappings Mapping(void) const { return m_decal->Mapping(); }
  void SetMapping(ON_Decal::Mappings m) const { m_decal->SetMapping(m); }

  ON_Decal::Projections Projection(void) const { return m_decal->Projection(); }
  void SetProjection(ON_Decal::Projections p) const { m_decal->SetProjection(p); }

  bool MapToInside(void) const { return m_decal->MapToInside(); }
  void SetMapToInside(bool v) const { m_decal->SetMapToInside(v); }

  double Transparency(void) const { return m_decal->Transparency(); }
  void SetTransparency(double v) const { m_decal->SetTransparency(v); }

  ON_3dPoint Origin(void) const { return m_decal->Origin(); }
  void SetOrigin(ON_3dPoint v) const { m_decal->SetOrigin(v); }

  ON_3dVector VectorUp(void) const { return m_decal->VectorUp(); }
  void SetVectorUp(ON_3dVector v) const { m_decal->SetVectorUp(v); }

  ON_3dVector VectorAcross(void) const { return m_decal->VectorAcross(); }
  void SetVectorAcross(ON_3dVector v) const { m_decal->SetVectorAcross(v); }

  double Height(void) const { return m_decal->Height(); }
  void SetHeight(double v) const { m_decal->SetHeight(v); }

  double Radius(void) const { return m_decal->Radius(); }
  void SetRadius(double v) const { m_decal->SetRadius(v); }

  double HorzSweepStart(void) const;
  void SetHorzSweepStart(double v) const;

  double HorzSweepEnd(void) const;
  void SetHorzSweepEnd(double v) const;

  double VertSweepStart(void) const;
  void SetVertSweepStart(double v) const;

  double VertSweepEnd(void) const;
  void SetVertSweepEnd(double v) const;

  double BoundsMinU(void) const;
  void SetBoundsMinU(double v) const;

  double BoundsMinV(void) const;
  void SetBoundsMinV(double v) const;

  double BoundsMaxU(void) const;
  void SetBoundsMaxU(double v) const;

  double BoundsMaxV(void) const;
  void SetBoundsMaxV(double v) const;
};
