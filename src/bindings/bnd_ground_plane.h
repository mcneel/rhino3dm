
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initGroundPlaneBindings(pybind11::module& m);
#else
void initGroundPlaneBindings(void* m);
#endif

class BND_File3dmGroundPlane
{
public:
  ON_GroundPlane* m_ground_plane = nullptr;

protected:
  void SetTrackedPointer(ON_GroundPlane* gp) { m_ground_plane = gp; }

public:
  BND_File3dmGroundPlane();
  BND_File3dmGroundPlane(ON_GroundPlane* gp);

  bool GetOn(void) const { return m_ground_plane->On(); }
  void SetOn(bool v) const { m_ground_plane->SetOn(v); }

  bool GetShowUnderside(void) const { return m_ground_plane->ShowUnderside(); }
  void SetShowUnderside(bool b) const { m_ground_plane->SetShowUnderside(b); }

  double GetAltitude(void) const { return m_ground_plane->Altitude(); }
  void SetAltitude(double d) const { m_ground_plane->SetAltitude(d); }

  bool GetAutoAltitude(void) const { return m_ground_plane->AutoAltitude(); }
  void SetAutoAltitude(bool b) const { m_ground_plane->SetAutoAltitude(b); }

  bool GetShadowOnly(void) const { return m_ground_plane->ShadowOnly(); }
  void SetShadowOnly(bool b) const { m_ground_plane->SetShadowOnly(b); }

  BND_UUID GetMaterialInstanceId(void) const { return ON_UUID_to_Binding(m_ground_plane->MaterialInstanceId()); }
  void SetMaterialInstanceId(BND_UUID u) const { m_ground_plane->SetMaterialInstanceId(Binding_to_ON_UUID(u)); }

  ON_2dVector GetTextureOffset(void) const { return m_ground_plane->TextureOffset(); }
  void SetTextureOffset(ON_2dVector v) const { m_ground_plane->SetTextureOffset(v); }

  bool GetTextureOffsetLocked(void) const { return m_ground_plane->TextureOffsetLocked(); }
  void SetTextureOffsetLocked(bool b) const { m_ground_plane->SetTextureOffsetLocked(b); }

  bool GetTextureRepeatLocked(void) const { return m_ground_plane->TextureRepeatLocked(); }
  void SetTextureRepeatLocked(bool b) const { m_ground_plane->SetTextureRepeatLocked(b); }

  ON_2dVector GetTextureSize(void) const { return m_ground_plane->TextureSize(); }
  void SetTextureSize(ON_2dVector v) const { m_ground_plane->SetTextureSize(v); }

  double GetTextureRotation(void) const { return m_ground_plane->TextureRotation(); }
  void SetTextureRotation(double d) const { m_ground_plane->SetTextureRotation(d); }
};
