
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSafeFrameBindings(pybind11::module& m);
#else
void initSafeFrameBindings(void* m);
#endif

class BND_File3dmSafeFrame
{
public:
  ON_SafeFrame* m_safe_frame = nullptr;

protected:
  void SetTrackedPointer(ON_SafeFrame* sf) { m_safe_frame = sf; }

public:
  BND_File3dmSafeFrame();
  BND_File3dmSafeFrame(ON_SafeFrame* sf);

  bool GetOn(void) const { return m_safe_frame->On(); }
  void SetOn(bool v) const { m_safe_frame->SetOn(v); }

  bool GetPerspectiveOnly(void) const { return m_safe_frame->PerspectiveOnly(); }
  void SetPerspectiveOnly(bool b) const { m_safe_frame->SetPerspectiveOnly(b); }

  bool GetFieldGridOn(void) const { return m_safe_frame->FieldGridOn(); }
  void SetFieldGridOn(bool b) const { m_safe_frame->SetFieldGridOn(b); }

  bool GetLiveFrameOn(void) const { return m_safe_frame->LiveFrameOn(); }
  void SetLiveFrameOn(bool b) const { m_safe_frame->SetLiveFrameOn(b); }

  bool GetActionFrameOn(void) const { return m_safe_frame->ActionFrameOn(); }
  void SetActionFrameOn(bool b) const { m_safe_frame->SetActionFrameOn(b); }

  bool GetActionFrameLinked(void) const { return m_safe_frame->ActionFrameLinked(); }
  void SetActionFrameLinked(bool b) const { m_safe_frame->SetActionFrameLinked(b); }

  double GetActionFrameXScale(void) const { return m_safe_frame->ActionFrameXScale(); }
  void SetActionFrameXScale(double d) const { m_safe_frame->SetActionFrameXScale(d); }

  double GetActionFrameYScale(void) const { return m_safe_frame->ActionFrameYScale(); }
  void SetActionFrameYScale(double d) const { m_safe_frame->SetActionFrameYScale(d); }

  bool GetTitleFrameOn(void) const { return m_safe_frame->TitleFrameOn(); }
  void SetTitleFrameOn(bool b) const { m_safe_frame->SetTitleFrameOn(b); }

  bool GetTitleFrameLinked(void) const { return m_safe_frame->TitleFrameLinked(); }
  void SetTitleFrameLinked(bool b) const { m_safe_frame->SetTitleFrameLinked(b); }

  double GetTitleFrameXScale(void) const { return m_safe_frame->TitleFrameXScale(); }
  void SetTitleFrameXScale(double d) const { m_safe_frame->SetTitleFrameXScale(d); }

  double GetTitleFrameYScale(void) const { return m_safe_frame->TitleFrameYScale(); }
  void SetTitleFrameYScale(double d) const { m_safe_frame->SetTitleFrameYScale(d); }
};
