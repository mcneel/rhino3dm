
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initGroundPlaneBindings(pybind11::module& m);
#else
void initGroundPlaneBindings(void* m);
#endif

class BND_File3dmGroundPlane
{
private:
  ON_GroundPlane* _gp = nullptr;
  bool _owned = false;

public:
  BND_File3dmGroundPlane() { _gp = new ON_GroundPlane; _owned = true; }
  BND_File3dmGroundPlane(const BND_File3dmGroundPlane& gp) { _gp = new ON_GroundPlane(*gp._gp); _owned = true; }
  BND_File3dmGroundPlane(ON_GroundPlane* gp) : _gp(gp) { }
  ~BND_File3dmGroundPlane() { if (_owned) delete _gp; }

  bool GetOn(void) const { return _gp->On(); }
  void SetOn(bool v) const { _gp->SetOn(v); }

  bool GetShowUnderside(void) const { return _gp->ShowUnderside(); }
  void SetShowUnderside(bool b) const { _gp->SetShowUnderside(b); }

  double GetAltitude(void) const { return _gp->Altitude(); }
  void SetAltitude(double d) const { _gp->SetAltitude(d); }

  bool GetAutoAltitude(void) const { return _gp->AutoAltitude(); }
  void SetAutoAltitude(bool b) const { _gp->SetAutoAltitude(b); }

  bool GetShadowOnly(void) const { return _gp->ShadowOnly(); }
  void SetShadowOnly(bool b) const { _gp->SetShadowOnly(b); }

  BND_UUID GetMaterialInstanceId(void) const { return ON_UUID_to_Binding(_gp->MaterialInstanceId()); }
  void SetMaterialInstanceId(BND_UUID u) const { _gp->SetMaterialInstanceId(Binding_to_ON_UUID(u)); }

  ON_2dVector GetTextureOffset(void) const { return _gp->TextureOffset(); }
  void SetTextureOffset(ON_2dVector v) const { _gp->SetTextureOffset(v); }

  bool GetTextureOffsetLocked(void) const { return _gp->TextureOffsetLocked(); }
  void SetTextureOffsetLocked(bool b) const { _gp->SetTextureOffsetLocked(b); }

  bool GetTextureSizeLocked(void) const { return _gp->TextureSizeLocked(); }
  void SetTextureSizeLocked(bool b) const { _gp->SetTextureSizeLocked(b); }

  ON_2dVector GetTextureSize(void) const { return _gp->TextureSize(); }
  void SetTextureSize(ON_2dVector v) const { _gp->SetTextureSize(v); }

  double GetTextureRotation(void) const { return _gp->TextureRotation(); }
  void SetTextureRotation(double d) const { _gp->SetTextureRotation(d); }
};
