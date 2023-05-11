
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
  ON_Decal* _decal = nullptr;
  bool _owned = false;

public:
  BND_File3dmDecal() { _decal = new ON_Decal; _owned = true; }
  BND_File3dmDecal(const BND_File3dmDecal& d) { _decal = new ON_Decal(*d._decal); _owned = true; }
  BND_File3dmDecal(ON_Decal* d) : _decal(d) { }
  ~BND_File3dmDecal() { if (_owned) delete _decal; }

  BND_UUID TextureInstanceId(void) const { return ON_UUID_to_Binding(_decal->TextureInstanceId()); }
  void SetTextureInstanceId(BND_UUID v) const { _decal->SetTextureInstanceId(Binding_to_ON_UUID(v)); }

  ON_Decal::Mappings Mapping(void) const { return _decal->Mapping(); }
  void SetMapping(ON_Decal::Mappings m) const { _decal->SetMapping(m); }

  ON_Decal::Projections Projection(void) const { return _decal->Projection(); }
  void SetProjection(ON_Decal::Projections p) const { _decal->SetProjection(p); }

  bool MapToInside(void) const { return _decal->MapToInside(); }
  void SetMapToInside(bool v) const { _decal->SetMapToInside(v); }

  double Transparency(void) const { return _decal->Transparency(); }
  void SetTransparency(double v) const { _decal->SetTransparency(v); }

  ON_3dPoint Origin(void) const { return _decal->Origin(); }
  void SetOrigin(ON_3dPoint v) const { _decal->SetOrigin(v); }

  ON_3dVector VectorUp(void) const { return _decal->VectorUp(); }
  void SetVectorUp(ON_3dVector v) const { _decal->SetVectorUp(v); }

  ON_3dVector VectorAcross(void) const { return _decal->VectorAcross(); }
  void SetVectorAcross(ON_3dVector v) const { _decal->SetVectorAcross(v); }

  double Height(void) const { return _decal->Height(); }
  void SetHeight(double v) const { _decal->SetHeight(v); }

  double Radius(void) const { return _decal->Radius(); }
  void SetRadius(double v) const { _decal->SetRadius(v); }

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

class BND_File3dmDecalTable
{
private:
  ON_3dmObjectAttributes* _attr = nullptr;
  bool _owned = false;

public:
  BND_File3dmDecalTable() { _attr = new ON_3dmObjectAttributes; _owned = true; }
  BND_File3dmDecalTable(const BND_File3dmDecalTable& d) { _attr = new ON_3dmObjectAttributes(*d._attr); _owned = true; }
  BND_File3dmDecalTable(ON_3dmObjectAttributes* a);
  ~BND_File3dmDecalTable() { if (_owned) delete _attr; }

  int Count() const;
  class BND_File3dmDecal* FindIndex(int index);
  class BND_File3dmDecal* IterIndex(int index); // helper function for iterator
};
