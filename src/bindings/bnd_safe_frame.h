#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSafeFrameBindings(rh3dmpymodule& m);
#else
void initSafeFrameBindings(void* m);
#endif

class BND_File3dmSafeFrame
{
private:
  ON_SafeFrame* _sf = nullptr;
  bool _owned = false;

public:
  BND_File3dmSafeFrame();
  BND_File3dmSafeFrame(ON_SafeFrame* sf);
  BND_File3dmSafeFrame(const BND_File3dmSafeFrame& sf);
  ~BND_File3dmSafeFrame() { if (_owned) delete _sf; }

  bool GetEnabled(void) const { return _sf->Enabled(); }
  void SetEnabled(bool v) { _sf->SetEnabled(v); }

  bool GetPerspectiveOnly(void) const { return _sf->PerspectiveOnly(); }
  void SetPerspectiveOnly(bool b) { _sf->SetPerspectiveOnly(b); }

  bool GetFieldGridOn(void) const { return _sf->FieldGridOn(); }
  void SetFieldGridOn(bool b) { _sf->SetFieldGridOn(b); }

  bool GetLiveFrameOn(void) const { return _sf->LiveFrameOn(); }
  void SetLiveFrameOn(bool b) { _sf->SetLiveFrameOn(b); }

  bool GetActionFrameOn(void) const { return _sf->ActionFrameOn(); }
  void SetActionFrameOn(bool b) { _sf->SetActionFrameOn(b); }

  bool GetActionFrameLinked(void) const { return _sf->ActionFrameLinked(); }
  void SetActionFrameLinked(bool b) { _sf->SetActionFrameLinked(b); }

  double GetActionFrameXScale(void) const { return _sf->ActionFrameXScale(); }
  void SetActionFrameXScale(double d) { _sf->SetActionFrameXScale(d); }

  double GetActionFrameYScale(void) const { return _sf->ActionFrameYScale(); }
  void SetActionFrameYScale(double d) { _sf->SetActionFrameYScale(d); }

  bool GetTitleFrameOn(void) const { return _sf->TitleFrameOn(); }
  void SetTitleFrameOn(bool b) { _sf->SetTitleFrameOn(b); }

  bool GetTitleFrameLinked(void) const { return _sf->TitleFrameLinked(); }
  void SetTitleFrameLinked(bool b) { _sf->SetTitleFrameLinked(b); }

  double GetTitleFrameXScale(void) const { return _sf->TitleFrameXScale(); }
  void SetTitleFrameXScale(double d) { _sf->SetTitleFrameXScale(d); }

  double GetTitleFrameYScale(void) const { return _sf->TitleFrameYScale(); }
  void SetTitleFrameYScale(double d) { _sf->SetTitleFrameYScale(d); }
};
