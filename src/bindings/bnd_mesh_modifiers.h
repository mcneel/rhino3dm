
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initMeshModifierBindings(rh3dmpymodule& m);
#else
void initMeshModifierBindings(void* m);
#endif

class BND_File3dmMeshModifier
{
public:
  virtual ~BND_File3dmMeshModifier() { }

protected:
  ON_MeshModifier* m_mm = nullptr;
  ON_3dmObjectAttributes* m_attr = nullptr;

  BND_File3dmMeshModifier(ON_3dmObjectAttributes* attr) : m_attr(attr) { }

public:
  bool Exists(void) const { return (nullptr != m_mm); }
  void EnsureExists() { if (nullptr == m_mm) CreateNew(); }

protected:
  virtual void CreateNew() = 0;
};

class BND_File3dmDisplacement : public BND_File3dmMeshModifier
{
public:
  BND_File3dmDisplacement(ON_3dmObjectAttributes* attr=nullptr);

  using SRF = ON_Displacement::SweepResolutionFormulas;
  bool     On()                 const { return (nullptr != DSP()) ? DSP()->On()                : false; }
  double   BlackPoint()         const { return (nullptr != DSP()) ? DSP()->BlackPoint()        : 0.0; }
  double   WhitePoint()         const { return (nullptr != DSP()) ? DSP()->WhitePoint()        : 0.0; }
  double   PostWeldAngle()      const { return (nullptr != DSP()) ? DSP()->PostWeldAngle()     : 0.0; }
  bool     FairingOn()          const { return (nullptr != DSP()) ? DSP()->FairingOn()         : false; }
  int      Fairing()            const { return (nullptr != DSP()) ? DSP()->Fairing()           : 0; }
  int      FinalMaxFaces()      const { return (nullptr != DSP()) ? DSP()->FinalMaxFaces()     : 0; }
  bool     FinalMaxFacesOn()    const { return (nullptr != DSP()) ? DSP()->FinalMaxFacesOn()   : false; }
  int      InitialQuality()     const { return (nullptr != DSP()) ? DSP()->InitialQuality()    : 0; }
  int      MappingChannel()     const { return (nullptr != DSP()) ? DSP()->MappingChannel()    : 0; }
  int      MeshMemoryLimit()    const { return (nullptr != DSP()) ? DSP()->MeshMemoryLimit()   : 0; }
  int      RefineSteps()        const { return (nullptr != DSP()) ? DSP()->RefineSteps()       : 0; }
  double   RefineSensitivity()  const { return (nullptr != DSP()) ? DSP()->RefineSensitivity() : 0.0; }
  SRF  SweepResolutionFormula() const { return (nullptr != DSP()) ? DSP()->SweepResolutionFormula() : SRF::Default; }
  BND_UUID Texture()            const { return ON_UUID_to_Binding((nullptr != DSP()) ? DSP()->Texture() : ON_nil_uuid); }

  void SetOn(bool v)                     { if (nullptr != DSP()) DSP()->SetOn(v); }
  void SetBlackPoint(double v)           { if (nullptr != DSP()) DSP()->SetBlackPoint(v); }
  void SetWhitePoint(double v)           { if (nullptr != DSP()) DSP()->SetWhitePoint(v); }
  void SetFairing(int v)                 { if (nullptr != DSP()) DSP()->SetFairing(v); }
  void SetFairingOn(bool v)              { if (nullptr != DSP()) DSP()->SetFairingOn(v); }
  void SetFinalMaxFaces(int v)           { if (nullptr != DSP()) DSP()->SetFinalMaxFaces(v); }
  void SetFinalMaxFacesOn(bool v)        { if (nullptr != DSP()) DSP()->SetFinalMaxFacesOn(v); }
  void SetInitialQuality(int v)          { if (nullptr != DSP()) DSP()->SetInitialQuality(v); }
  void SetMappingChannel(int v)          { if (nullptr != DSP()) DSP()->SetMappingChannel(v); }
  void SetMeshMemoryLimit(int v)         { if (nullptr != DSP()) DSP()->SetMeshMemoryLimit(v); }
  void SetPostWeldAngle(double v)        { if (nullptr != DSP()) DSP()->SetPostWeldAngle(v); }
  void SetRefineSteps(int v)             { if (nullptr != DSP()) DSP()->SetRefineSteps(v); }
  void SetRefineSensitivity(double v)    { if (nullptr != DSP()) DSP()->SetRefineSensitivity(v); }
  void SetSweepResolutionFormula(SRF v)  { if (nullptr != DSP()) DSP()->SetSweepResolutionFormula(v); }
  void SetTexture(const BND_UUID& v)     { if (nullptr != DSP()) DSP()->SetTexture(Binding_to_ON_UUID(v)); }

private:
        ON_Displacement* DSP()       { return dynamic_cast<ON_Displacement*>(m_mm); }
  const ON_Displacement* DSP() const { return dynamic_cast<ON_Displacement*>(m_mm); }

protected:
  virtual void CreateNew(void) override;
};

class BND_File3dmEdgeSoftening : public BND_File3dmMeshModifier
{
public:
  BND_File3dmEdgeSoftening(ON_3dmObjectAttributes* attr=nullptr);

  bool    On()                 const { return (nullptr != ES()) ? ES()->On()                 : false; }
  double  Softening()          const { return (nullptr != ES()) ? ES()->Softening()          : 0.0; }
  bool    Chamfer()            const { return (nullptr != ES()) ? ES()->Chamfer()            : false; }
  bool    Faceted()            const { return (nullptr != ES()) ? ES()->Faceted()            : false; }
  double  EdgeAngleThreshold() const { return (nullptr != ES()) ? ES()->EdgeAngleThreshold() : 0.0; }
  bool    ForceSoftening()     const { return (nullptr != ES()) ? ES()->ForceSoftening()     : false; }

  void SetOn(bool v)                   { if (nullptr != ES()) ES()->SetOn(v); }
  void SetSoftening(double v)          { if (nullptr != ES()) ES()->SetSoftening(v); }
  void SetChamfer(bool v)              { if (nullptr != ES()) ES()->SetChamfer(v); }
  void SetFaceted(bool v)              { if (nullptr != ES()) ES()->SetFaceted(v); }
  void SetEdgeAngleThreshold(double v) { if (nullptr != ES()) ES()->SetEdgeAngleThreshold(v); }
  void SetForceSoftening(bool v)       { if (nullptr != ES()) ES()->SetForceSoftening(v); }

private:
        ON_EdgeSoftening* ES()       { return dynamic_cast<ON_EdgeSoftening*>(m_mm); }
  const ON_EdgeSoftening* ES() const { return dynamic_cast<ON_EdgeSoftening*>(m_mm); }

protected:
  virtual void CreateNew() override;
};

class BND_File3dmThickening : public BND_File3dmMeshModifier
{
public:
  BND_File3dmThickening(ON_3dmObjectAttributes* attr=nullptr);

  bool   On()         const { return (nullptr != TH()) ? TH()->On()         : false; }
  double Distance()   const { return (nullptr != TH()) ? TH()->Distance()   : 0.0;   }
  bool   Solid()      const { return (nullptr != TH()) ? TH()->Solid()      : false; }
  bool   OffsetOnly() const { return (nullptr != TH()) ? TH()->OffsetOnly() : false; }
  bool   BothSides()  const { return (nullptr != TH()) ? TH()->BothSides()  : false; }

  void SetOn(bool v)          { if (nullptr != TH()) TH()->SetOn(v); }
  void SetDistance(double v)  { if (nullptr != TH()) TH()->SetDistance(v); }
  void SetSolid(bool v)       { if (nullptr != TH()) TH()->SetSolid(v); }
  void SetOffsetOnly(bool v)  { if (nullptr != TH()) TH()->SetOffsetOnly(v); }
  void SetBothSides(bool v)   { if (nullptr != TH()) TH()->SetBothSides(v) ; }

private:
        ON_Thickening* TH()       { return dynamic_cast<ON_Thickening*>(m_mm); }
  const ON_Thickening* TH() const { return dynamic_cast<ON_Thickening*>(m_mm); }

protected:
  virtual void CreateNew(void) override;
};

class BND_File3dmCurvePiping : public BND_File3dmMeshModifier
{
public:
  BND_File3dmCurvePiping(ON_3dmObjectAttributes* attr=nullptr);
  BND_File3dmCurvePiping(const BND_File3dmCurvePiping&);

  using CT = ON_CurvePiping::CapTypes;

  bool   On()       const { return (nullptr != CP()) ? CP()->On()       : false; }
  double Radius()   const { return (nullptr != CP()) ? CP()->Radius()   : 0.0; }
  int    Segments() const { return (nullptr != CP()) ? CP()->Segments() : 0; }
  bool   Faceted()  const { return (nullptr != CP()) ? CP()->Faceted()  : false; }
  int    Accuracy() const { return (nullptr != CP()) ? CP()->Accuracy() : 0; }
  CT     CapType()  const { return (nullptr != CP()) ? CP()->CapType()  : CT::None; }

  void SetOn(bool v)        { if (nullptr != CP()) CP()->SetOn(v); }
  void SetRadius(double v)  { if (nullptr != CP()) CP()->SetRadius(v); }
  void SetSegments(int v)   { if (nullptr != CP()) CP()->SetSegments(v); }
  void SetFaceted(bool v)   { if (nullptr != CP()) CP()->SetFaceted(v); }
  void SetAccuracy(int v)   { if (nullptr != CP()) CP()->SetAccuracy(v); }
  void SetCapType(CT v)     { if (nullptr != CP()) CP()->SetCapType(v); }

private:
        ON_CurvePiping* CP()       { return dynamic_cast<ON_CurvePiping*>(m_mm); }
  const ON_CurvePiping* CP() const { return dynamic_cast<ON_CurvePiping*>(m_mm); }

protected:
  virtual void CreateNew(void) override;
};

class BND_File3dmShutLiningCurve
{
private:
  ON_ShutLining::Curve* m_c = nullptr;

public:
  BND_File3dmShutLiningCurve(ON_ShutLining::Curve* c=nullptr) : m_c(c) { }

  double Radius()  const { return (nullptr != m_c) ? m_c->Radius()  : 0.0;   }
  int    Profile() const { return (nullptr != m_c) ? m_c->Profile() : 0;     }
  bool   Enabled() const { return (nullptr != m_c) ? m_c->Enabled() : false; }
  bool   Pull()    const { return (nullptr != m_c) ? m_c->Pull()    : false; }
  bool   IsBump()  const { return (nullptr != m_c) ? m_c->IsBump()  : false; }
  BND_UUID Id()    const { return ON_UUID_to_Binding((nullptr != m_c) ? m_c->Id() : ON_nil_uuid); }

  void SetRadius(double v)       { if (nullptr != m_c) m_c->SetRadius(v); }
  void SetProfile(int v)         { if (nullptr != m_c) m_c->SetProfile(v); }
  void SetEnabled(bool v)        { if (nullptr != m_c) m_c->SetEnabled(v); }
  void SetPull(bool v)           { if (nullptr != m_c) m_c->SetPull(v); }
  void SetIsBump(bool v)         { if (nullptr != m_c) m_c->SetIsBump(v); }
  void SetId(const BND_UUID& v)  { if (nullptr != m_c) m_c->SetId(Binding_to_ON_UUID(v)); }
};

class BND_File3dmShutLiningCurveTable
{
private:
  ON_ShutLining* m_sl = nullptr;

public:
  BND_File3dmShutLiningCurveTable(const ON_ShutLining* sl=nullptr) : m_sl(const_cast<ON_ShutLining*>(sl)) { }

  int Count() const;
  void Add(BND_UUID id);
  BND_File3dmShutLiningCurve* FindIndex(int index);
  BND_File3dmShutLiningCurve* IterIndex(int index); // helper function for iterator
  BND_File3dmShutLiningCurve* FindId(BND_UUID id);
};

class BND_File3dmShutLining : public BND_File3dmMeshModifier
{
public:
  BND_File3dmShutLining(ON_3dmObjectAttributes* attr=nullptr);

  bool On()          const { return (nullptr != SL()) ? SL()->On()          : false; }
  bool Faceted()     const { return (nullptr != SL()) ? SL()->Faceted()     : false; }
  bool AutoUpdate()  const { return (nullptr != SL()) ? SL()->AutoUpdate()  : false; }
  bool ForceUpdate() const { return (nullptr != SL()) ? SL()->ForceUpdate() : false; }

  void SetOn(bool v)           { if (nullptr != SL()) SL()->SetOn(v); }
  void SetFaceted(bool v)      { if (nullptr != SL()) SL()->SetFaceted(v); }
  void SetAutoUpdate(bool v)   { if (nullptr != SL()) SL()->SetAutoUpdate(v); }
  void SetForceUpdate(bool v)  { if (nullptr != SL()) SL()->SetForceUpdate(v); }

  BND_File3dmShutLiningCurveTable Curves() const { return BND_File3dmShutLiningCurveTable(SL()); }

  void DeleteAllCurves() { if (nullptr != SL()) SL()->DeleteAllCurves(); }

private:
        ON_ShutLining* SL()       { return dynamic_cast<ON_ShutLining*>(m_mm); }
  const ON_ShutLining* SL() const { return dynamic_cast<ON_ShutLining*>(m_mm); }

protected:
  virtual void CreateNew() override;
};

class BND_File3dmMeshModifiers
{
private:
  BND_File3dmDisplacement  m_displacement;
  BND_File3dmEdgeSoftening m_edge_softening;
  BND_File3dmThickening    m_thickening;
  BND_File3dmCurvePiping   m_curve_piping;
  BND_File3dmShutLining    m_shutlining;
  ON_3dmObjectAttributes*  m_attr;

public:
  BND_File3dmMeshModifiers() = default;
  BND_File3dmMeshModifiers(ON_3dmObjectAttributes* attr);
  BND_File3dmMeshModifiers(const BND_File3dmMeshModifiers&);

  BND_File3dmDisplacement*  Displacement()  { return m_displacement  .Exists() ? &m_displacement   : nullptr; }
  BND_File3dmEdgeSoftening* EdgeSoftening() { return m_edge_softening.Exists() ? &m_edge_softening : nullptr; }
  BND_File3dmThickening*    Thickening()    { return m_thickening    .Exists() ? &m_thickening     : nullptr; }
  BND_File3dmCurvePiping*   CurvePiping()   { return m_curve_piping  .Exists() ? &m_curve_piping   : nullptr; }
  BND_File3dmShutLining*    ShutLining()    { return m_shutlining    .Exists() ? &m_shutlining     : nullptr; }

  BND_File3dmDisplacement&  CreateDisplacement()  { m_displacement  .EnsureExists(); return m_displacement; }
  BND_File3dmEdgeSoftening& CreateEdgeSoftening() { m_edge_softening.EnsureExists(); return m_edge_softening; }
  BND_File3dmThickening&    CreateThickening()    { m_thickening    .EnsureExists(); return m_thickening; }
  BND_File3dmCurvePiping&   CreateCurvePiping()   { m_curve_piping  .EnsureExists(); return m_curve_piping; }
  BND_File3dmShutLining&    CreateShutLining()    { m_shutlining    .EnsureExists(); return m_shutlining; }
};
