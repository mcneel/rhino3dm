
#include "stdafx.h"

static ON_MeshModifiers* GetMeshModifiers(const ON_3dmObjectAttributes* attr)
{
  if (nullptr == attr)
    return nullptr;

  return &attr->MeshModifiers();
}

static ON_Displacement* Displacement(const ON_3dmObjectAttributes* attr)
{
  auto* mm = GetMeshModifiers(attr);
  if (nullptr == mm)
    return nullptr;

  return mm->Displacement();
}

static ON_EdgeSoftening* EdgeSoftening(const ON_3dmObjectAttributes* attr)
{
  auto* mm = GetMeshModifiers(attr);
  if (nullptr == mm)
    return nullptr;

  return mm->EdgeSoftening();
}

static ON_Thickening* Thickening(const ON_3dmObjectAttributes* attr)
{
  auto* mm = GetMeshModifiers(attr);
  if (nullptr == mm)
    return nullptr;

  return mm->Thickening();
}

static ON_CurvePiping* CurvePiping(const ON_3dmObjectAttributes* attr)
{
  auto* mm = GetMeshModifiers(attr);
  if (nullptr == mm)
    return nullptr;

  return mm->CurvePiping();
}

static ON_ShutLining* Shutlining(const ON_3dmObjectAttributes* attr)
{
  auto* mm = GetMeshModifiers(attr);
  if (nullptr == mm)
    return nullptr;

  return mm->ShutLining();
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasDisplacement(const ON_3dmObjectAttributes* attr)
{
  return (nullptr != attr) ? (nullptr != attr->MeshModifiers().Displacement()) : false;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Displacement_GetOn(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return false;

  return dsp->On();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetOn(b);
  }
}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_Displacement_GetTextureId(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return ON_nil_uuid;

  return dsp->Texture();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetTextureId(ON_3dmObjectAttributes* attr, ON_UUID id)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetTexture(id);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetMappingChannel(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->MappingChannel();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetMappingChannel(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetMappingChannel(i);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetBlackPoint(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0.0;

  return dsp->BlackPoint();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetBlackPoint(ON_3dmObjectAttributes* attr, double d)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetBlackPoint(d);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetWhitePoint(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0.0;

  return dsp->WhitePoint();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetWhitePoint(ON_3dmObjectAttributes* attr, double d)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetWhitePoint(d);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetInitialQuality(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->InitialQuality();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetInitialQuality(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetInitialQuality(i);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Displacement_GetFinalMaxFacesOn(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return false;

  return dsp->FinalMaxFacesOn();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetFinalMaxFacesOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetFinalMaxFacesOn(b);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetFinalMaxFaces(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->FinalMaxFaces();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetFinalMaxFaces(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetFinalMaxFaces(i);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Displacement_GetFairingOn(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return false;

  return dsp->FairingOn();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetFairingOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetFairingOn(b);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetFairing(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->Fairing();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetFairing(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetFairing(i);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetPostWeldAngle(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0.0;

  return dsp->PostWeldAngle();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetPostWeldAngle(ON_3dmObjectAttributes* attr, double d)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetPostWeldAngle(d);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetMeshMemoryLimit(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->MeshMemoryLimit();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetMeshMemoryLimit(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetMeshMemoryLimit(i);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetRefineSteps(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0;

  return dsp->RefineSteps();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetRefineSteps(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetRefineSteps(i);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetRefineSensitivity(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return 0.0;

  return dsp->RefineSensitivity();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetRefineSensitivity(ON_3dmObjectAttributes* attr, double d)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetRefineSensitivity(d);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetSweepResolutionFormula(const ON_3dmObjectAttributes* attr)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return int(ON_Displacement::SweepResolutionFormulas::Default);

  return int(dsp->SweepResolutionFormula());
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSweepResolutionFormula(ON_3dmObjectAttributes* attr, int i)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->SetSweepResolutionFormula(ON_Displacement::SweepResolutionFormulas(i));
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_GetSubItems(const ON_3dmObjectAttributes* attr, ON_SimpleArray<int>* a)
{
  const auto* dsp = Displacement(attr);
  if ((nullptr != dsp) && (nullptr != a))
  {
    auto it = dsp->GetSubItemIterator();
    ON_Displacement::SubItem* sub;
    while (nullptr != (sub = it.Next()))
    {
      a->Append(sub->FaceIndex());
    }
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Displacement_AddSubItem(ON_3dmObjectAttributes* attr, int face_index,
                   bool on, ON_UUID texture, int mapping_channel, double black_point, double white_point)
{
  auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return false;

  auto& sub_item = dsp->AddSubItem();
  sub_item.SetOn(on);
  sub_item.SetTexture(texture);
  sub_item.SetFaceIndex(face_index);
  sub_item.SetBlackPoint(black_point);
  sub_item.SetWhitePoint(white_point);
  sub_item.SetMappingChannel(mapping_channel);

  return true;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_DeleteSubItem(ON_3dmObjectAttributes* attr, int face_index)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->DeleteSubItem(face_index);
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_DeleteAllSubItems(ON_3dmObjectAttributes* attr)
{
  auto* dsp = Displacement(attr);
  if (nullptr != dsp)
  {
    dsp->DeleteAllSubItems();
  }
}

static ON_Displacement::SubItem* FindDisplacementSubItem(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* dsp = Displacement(attr);
  if (nullptr == dsp)
    return nullptr;

  return dsp->FindSubItem(face_index);
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Displacement_GetSubItemOn(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr == sub_item)
    return false;

  return sub_item->On();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSubItemOn(ON_3dmObjectAttributes* attr, int face_index, bool b)
{
  auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr != sub_item)
  {
    sub_item->SetOn(b);
  }
}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_Displacement_GetSubItemTexture(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr == sub_item)
    return ON_nil_uuid;

  return sub_item->Texture();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSubItemTexture(ON_3dmObjectAttributes* attr, int face_index, ON_UUID id)
{
  auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr != sub_item)
  {
    sub_item->SetTexture(id);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_Displacement_GetSubItemMappingChannel(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr == sub_item)
    return -1;

  return sub_item->MappingChannel();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSubItemMappingChannel(ON_3dmObjectAttributes* attr, int face_index, int i)
{
  auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr != sub_item)
  {
    sub_item->SetMappingChannel(i);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetSubItemBlackPoint(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr == sub_item)
    return 0.0;

  return sub_item->BlackPoint();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSubItemBlackPoint(ON_3dmObjectAttributes* attr, int face_index, double d)
{
  auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr != sub_item)
  {
    sub_item->SetBlackPoint(d);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Displacement_GetSubItemWhitePoint(const ON_3dmObjectAttributes* attr, int face_index)
{
  const auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr == sub_item)
    return 0.0;

  return sub_item->WhitePoint();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Displacement_SetSubItemWhitePoint(ON_3dmObjectAttributes* attr, int face_index, double d)
{
  auto* sub_item = FindDisplacementSubItem(attr, face_index);
  if (nullptr != sub_item)
  {
    sub_item->SetWhitePoint(d);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasEdgeSoftening(const ON_3dmObjectAttributes* attr)
{
  return (nullptr != attr) ? (nullptr != attr->MeshModifiers().EdgeSoftening()) : false;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_EdgeSoftening_GetOn(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return false;

  return es->On();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetOn(b);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_EdgeSoftening_GetSoftening(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return 0.0;

  return es->Softening();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetSoftening(ON_3dmObjectAttributes* attr, double d)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetSoftening(d);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_EdgeSoftening_GetChamfer(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return false;

  return es->Chamfer();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetChamfer(ON_3dmObjectAttributes* attr, bool b)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetChamfer(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_EdgeSoftening_GetFaceted(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return false;

  return es->Faceted();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetFaceted(ON_3dmObjectAttributes* attr, bool b)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetFaceted(b);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_EdgeSoftening_GetEdgeAngleThreshold(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return 0.0;

  return es->EdgeAngleThreshold();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetEdgeAngleThreshold(ON_3dmObjectAttributes* attr, double d)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetEdgeAngleThreshold(d);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_EdgeSoftening_GetForceSoftening(const ON_3dmObjectAttributes* attr)
{
  const auto* es = EdgeSoftening(attr);
  if (nullptr == es)
    return false;

  return es->ForceSoftening();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EdgeSoftening_SetForceSoftening(ON_3dmObjectAttributes* attr, bool b)
{
  auto* es = EdgeSoftening(attr);
  if (nullptr != es)
  {
    es->SetForceSoftening(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Thickening_GetOn(const ON_3dmObjectAttributes* attr)
{
  const auto* th = Thickening(attr);
  if (nullptr == th)
    return false;

  return th->On();
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasThickening(const ON_3dmObjectAttributes* attr)
{
  return (nullptr != attr) ? (nullptr != attr->MeshModifiers().Thickening()) : false;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Thickening_SetOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* th = Thickening(attr);
  if (nullptr != th)
  {
    th->SetOn(b);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_Thickening_GetDistance(const ON_3dmObjectAttributes* attr)
{
  const auto* th = Thickening(attr);
  if (nullptr == th)
    return 0.0;

  return th->Distance();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Thickening_SetDistance(ON_3dmObjectAttributes* attr, double d)
{
  auto* th = Thickening(attr);
  if (nullptr != th)
  {
    th->SetDistance(d);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Thickening_GetSolid(const ON_3dmObjectAttributes* attr)
{
  const auto* th = Thickening(attr);
  if (nullptr == th)
    return false;

  return th->Solid();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Thickening_SetSolid(ON_3dmObjectAttributes* attr, bool b)
{
  auto* th = Thickening(attr);
  if (nullptr != th)
  {
    th->SetSolid(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Thickening_GetOffsetOnly(const ON_3dmObjectAttributes* attr)
{
  const auto* th = Thickening(attr);
  if (nullptr == th)
    return false;

  return th->OffsetOnly();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Thickening_SetOffsetOnly(ON_3dmObjectAttributes* attr, bool b)
{
  auto* th = Thickening(attr);
  if (nullptr != th)
  {
    th->SetOffsetOnly(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_Thickening_GetBothSides(const ON_3dmObjectAttributes* attr)
{
  const auto* th = Thickening(attr);
  if (nullptr == th)
    return false;

  return th->BothSides();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Thickening_SetBothSides(ON_3dmObjectAttributes* attr, bool b)
{
  auto* th = Thickening(attr);
  if (nullptr != th)
  {
    th->SetBothSides(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasCurvePiping(const ON_3dmObjectAttributes* attr)
{
  return (nullptr != attr) ? (nullptr != attr->MeshModifiers().CurvePiping()) : false;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_CurvePiping_GetOn(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return false;

  return cp->On();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetOn(b);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_CurvePiping_GetRadius(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return 0.0;

  return cp->Radius();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetRadius(ON_3dmObjectAttributes* attr, double d)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetRadius(d);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_CurvePiping_GetSegments(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return 0;

  return cp->Segments();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetSegments(ON_3dmObjectAttributes* attr, int i)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetSegments(i);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_CurvePiping_GetFaceted(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return false;

  return cp->Faceted();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetFaceted(ON_3dmObjectAttributes* attr, bool b)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetFaceted(b);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_CurvePiping_GetAccuracy(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return 0;

  return cp->Accuracy();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetAccuracy(ON_3dmObjectAttributes* attr, int i)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetAccuracy(i);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_CurvePiping_GetCapType(const ON_3dmObjectAttributes* attr)
{
  const auto* cp = CurvePiping(attr);
  if (nullptr == cp)
    return int(ON_CurvePiping::CapTypes::None);

  return int(cp->CapType());
}

RH_C_FUNCTION void ON_3dmObjectAttributes_CurvePiping_SetCapType(ON_3dmObjectAttributes* attr, int i)
{
  auto* cp = CurvePiping(attr);
  if (nullptr != cp)
  {
    cp->SetCapType(ON_CurvePiping::CapTypes(i));
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasShutLining(const ON_3dmObjectAttributes* attr)
{
  return (nullptr != attr) ? (nullptr != attr->MeshModifiers().ShutLining()) : false;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetOn(const ON_3dmObjectAttributes* attr)
{
  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return false;

  return sl->On();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetOn(ON_3dmObjectAttributes* attr, bool b)
{
  auto* sl = Shutlining(attr);
  if (nullptr != sl)
  {
    sl->SetOn(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetFaceted(const ON_3dmObjectAttributes* attr)
{
  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return false;

  return sl->Faceted();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetFaceted(ON_3dmObjectAttributes* attr, bool b)
{
  auto* sl = Shutlining(attr);
  if (nullptr != sl)
  {
    sl->SetFaceted(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetAutoUpdate(const ON_3dmObjectAttributes* attr)
{
  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return false;

  return sl->AutoUpdate();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetAutoUpdate(ON_3dmObjectAttributes* attr, bool b)
{
  auto* sl = Shutlining(attr);
  if (nullptr != sl)
  {
    sl->SetAutoUpdate(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetForceUpdate(const ON_3dmObjectAttributes* attr)
{
  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return false;

  return sl->ForceUpdate();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetForceUpdate(ON_3dmObjectAttributes* attr, bool b)
{
  auto* sl = Shutlining(attr);
  if (nullptr != sl)
  {
    sl->SetForceUpdate(b);
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_GetCurves(const ON_3dmObjectAttributes* attr, ON_SimpleArray<ON_UUID>* a)
{
  if (nullptr == a)
    return;

  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return;

  auto it = sl->GetCurveIterator();
  ON_ShutLining::Curve* curve;
  while (nullptr != (curve = it.Next()))
  {
    a->Append(curve->Id());
  }
}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_ShutLining_AddCurve(ON_3dmObjectAttributes* attr)
{
  auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return ON_nil_uuid;

  return sl->AddCurve().Id();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_DeleteAllCurves(ON_3dmObjectAttributes* attr)
{
  auto* sl = Shutlining(attr);
  if (nullptr != sl)
  {
    return sl->DeleteAllCurves();
  }
}

static ON_ShutLining::Curve* FindShutliningCurve(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* sl = Shutlining(attr);
  if (nullptr == sl)
    return nullptr;

  return sl->FindCurve(id);
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetCurveEnabled(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* curve = FindShutliningCurve(attr, id);
  if (nullptr == curve)
    return false;

  return curve->Enabled();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetCurveEnabled(const ON_3dmObjectAttributes* attr, ON_UUID id, bool b)
{
  auto* curve = FindShutliningCurve(attr, id);
  if (nullptr != curve)
  {
    curve->SetEnabled(b);
  }
}

RH_C_FUNCTION double ON_3dmObjectAttributes_ShutLining_GetCurveRadius(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* curve = FindShutliningCurve(attr, id);
  if (nullptr == curve)
    return 0.0;

  return curve->Radius();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetCurveRadius(const ON_3dmObjectAttributes* attr, ON_UUID id, double d)
{
  auto* curve = FindShutliningCurve(attr, id);
  if (nullptr != curve)
  {
    curve->SetRadius(d);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_ShutLining_GetCurveProfile(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* curve = FindShutliningCurve(attr, id);
  if (nullptr == curve)
    return 0;

  return curve->Profile();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetCurveProfile(ON_3dmObjectAttributes* attr, ON_UUID id, int i)
{
  auto* curve = FindShutliningCurve(attr, id);
  if (nullptr != curve)
  {
    curve->SetProfile(i);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetCurvePull(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* curve = FindShutliningCurve(attr, id);
  if (nullptr == curve)
    return false;

  return curve->Pull();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetCurvePull(const ON_3dmObjectAttributes* attr, ON_UUID id, bool b)
{
  auto* curve = FindShutliningCurve(attr, id);
  if (nullptr != curve)
  {
    curve->SetPull(b);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ShutLining_GetCurveIsBump(const ON_3dmObjectAttributes* attr, ON_UUID id)
{
  const auto* curve = FindShutliningCurve(attr, id);
  if (nullptr == curve)
    return false;

  return curve->IsBump();
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ShutLining_SetCurveIsBump(ON_3dmObjectAttributes* attr, ON_UUID id, bool b)
{
  auto* curve = FindShutliningCurve(attr, id);
  if (nullptr != curve)
  {
    curve->SetIsBump(b);
  }
}
