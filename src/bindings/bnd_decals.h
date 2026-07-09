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
void initDecalBindings(rh3dmpymodule& m);
#else
void initDecalBindings(void* m);
#endif

class BND_File3dmDecal
{
private:
  // RH3DM-159: own the decal via shared_ptr instead of a raw ON_Decal* with an _owned flag. The old
  // code wrapped a borrowed pointer from the deprecated GetDecalArray() with _owned=true, so
  // destroying the wrapper deleted a decal owned by the attributes (dangling + double-free).
  //
  // Note shared ownership of the ON_Decal object is necessary but NOT sufficient: an ON_Decal read
  // from the table holds a raw _model_node pointer into the collection's XML tree, which
  // GetDecalArray() rebuilds (freeing the old nodes) on every call. So the table hands us a
  // self-contained COPY (ON_Decal copy ctor -> owned _local_node) rather than the cached decal; see
  // BND_File3dmDecalTable::FindIndex. This shared_ptr then owns that independent copy outright.
  std::shared_ptr<ON_Decal> _decal;

public:
  BND_File3dmDecal();
  BND_File3dmDecal(std::shared_ptr<ON_Decal> d);
  BND_File3dmDecal(const BND_File3dmDecal& d);

  BND_UUID TextureInstanceId() const { return ON_UUID_to_Binding(_decal->TextureInstanceId()); }
  void SetTextureInstanceId(BND_UUID v) { _decal->SetTextureInstanceId(Binding_to_ON_UUID(v)); }

  Mappings Mapping() const;
  ON_Decal::Mappings GetMapping() const;
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
