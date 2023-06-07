#include "bindings.h"

#pragma once

enum class Mappings : int
{
  None        = -1,
  Planar      =  0, // Planar mapping. Uses projection, origin, up and across vectors (not unitized).
  Cylindrical =  1, // Cylindrical mapping. Uses origin, up, across, height, radius, horz-sweep.
  Spherical   =  2, // Spherical mapping. Uses origin, up, across, radius, horz-sweep, vert-sweep.
  UV          =  3, // UV mapping. Uses UV bounds.
};

enum class Projections : int
{
  None     = -1,
  Forward  =  0, // Project forward.
  Backward =  1, // Project backward.
  Both     =  2, // Project forward and backward.
};

#if defined(ON_PYTHON_COMPILE)
void initDecalBindings(pybind11::module& m);
#else
void initDecalBindings(void* m);
#endif

class BND_File3dmDecal
{
private:
  ON_Decal* _decal = nullptr;
  bool _owned = false;

public:
  BND_File3dmDecal();               //BND_File3dmDecal() { _decal = new ON_Decal; _owned = true; }
  BND_File3dmDecal(ON_Decal* d);    //BND_File3dmDecal(ON_Decal* d) : _decal(d) { }
  BND_File3dmDecal(const BND_File3dmDecal& d);
  ~BND_File3dmDecal() { if (_owned) delete _decal; }

  BND_UUID TextureInstanceId() const { return ON_UUID_to_Binding(_decal->TextureInstanceId()); }
  void SetTextureInstanceId(BND_UUID v) { _decal->SetTextureInstanceId(Binding_to_ON_UUID(v)); }

  Mappings Mapping() const;
  void SetMapping(Mappings mapping);

  Projections Projection() const;
  void SetProjection(Projections projection);

  bool MapToInside() const { return _decal->MapToInside(); }
  void SetMapToInside(bool v) { _decal->SetMapToInside(v); }

  double Transparency() const { return _decal->Transparency(); }
  void SetTransparency(double v) { _decal->SetTransparency(v); }

  ON_3dPoint Origin() const { return _decal->Origin(); }
  void SetOrigin(ON_3dPoint v) { _decal->SetOrigin(v); }

  ON_3dVector VectorUp() const { return _decal->VectorUp(); }
  void SetVectorUp(ON_3dVector v) { _decal->SetVectorUp(v); }

  ON_3dVector VectorAcross() const { return _decal->VectorAcross(); }
  void SetVectorAcross(ON_3dVector v) { _decal->SetVectorAcross(v); }

  double Height() const { return _decal->Height(); }
  void SetHeight(double v) { _decal->SetHeight(v); }

  double Radius() const { return _decal->Radius(); }
  void SetRadius(double v) { _decal->SetRadius(v); }

  double HorzSweepStart() const;
  void SetHorzSweepStart(double v);

  double HorzSweepEnd() const;
  void SetHorzSweepEnd(double v);

  double VertSweepStart() const;
  void SetVertSweepStart(double v);

  double VertSweepEnd() const;
  void SetVertSweepEnd(double v);

  double BoundsMinU() const;
  void SetBoundsMinU(double v);

  double BoundsMinV() const;
  void SetBoundsMinV(double v);

  double BoundsMaxU() const;
  void SetBoundsMaxU(double v);

  double BoundsMaxV() const;
  void SetBoundsMaxV(double v);

};

class BND_File3dmDecalTable
{
private:
  ON_3dmObjectAttributes* _attr = nullptr;
  bool _owned = false;

public:
  BND_File3dmDecalTable();
  BND_File3dmDecalTable(ON_3dmObjectAttributes* a);
  BND_File3dmDecalTable(const BND_File3dmDecalTable& d);
  ~BND_File3dmDecalTable() { if (_owned) delete _attr; };

  int Count() const;
  class BND_File3dmDecal* FindIndex(int index);
  class BND_File3dmDecal* IterIndex(int index); // helper function for iterator
};
