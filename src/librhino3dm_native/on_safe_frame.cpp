
#include "stdafx.h"

enum class SafeFrameSetting : int
{
  On,
  PerspectiveOnly,
  FieldGridOn,
  LiveFrameOn,
  ActionFrameOn,
  ActionFrameLinked,
  ActionFrameXScale,
  ActionFrameYScale,
  TitleFrameOn,
  TitleFrameLinked,
  TitleFrameXScale,
  TitleFrameYScale,
};

RH_C_FUNCTION void ON_SafeFrame_GetValue(const ON_SafeFrame* sf, SafeFrameSetting which, ON_XMLVariant* v)
{
  if (sf && v)
  {
    switch (which)
    {
    case SafeFrameSetting::On:                *v = sf->On();                break;
    case SafeFrameSetting::PerspectiveOnly:   *v = sf->PerspectiveOnly();   break;
    case SafeFrameSetting::FieldGridOn:       *v = sf->FieldGridOn();       break;
    case SafeFrameSetting::LiveFrameOn:       *v = sf->LiveFrameOn();       break;
    case SafeFrameSetting::ActionFrameOn:     *v = sf->ActionFrameOn();     break;
    case SafeFrameSetting::ActionFrameLinked: *v = sf->ActionFrameLinked(); break;
    case SafeFrameSetting::ActionFrameXScale: *v = sf->ActionFrameXScale(); break;
    case SafeFrameSetting::ActionFrameYScale: *v = sf->ActionFrameYScale(); break;
    case SafeFrameSetting::TitleFrameOn:      *v = sf->TitleFrameOn();      break;
    case SafeFrameSetting::TitleFrameLinked:  *v = sf->TitleFrameLinked();  break;
    case SafeFrameSetting::TitleFrameXScale:  *v = sf->TitleFrameXScale();  break;
    case SafeFrameSetting::TitleFrameYScale:  *v = sf->TitleFrameYScale();  break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_SafeFrame_SetValue(ON_SafeFrame* sf, SafeFrameSetting which, const ON_XMLVariant* v)
{
  if (sf && v)
  {
    switch (which)
    {
    case SafeFrameSetting::On:                sf->SetOn(v->AsBool());                  break;
    case SafeFrameSetting::PerspectiveOnly:   sf->SetPerspectiveOnly(v->AsBool());     break;
    case SafeFrameSetting::FieldGridOn:       sf->SetFieldGridOn(v->AsBool());         break;
    case SafeFrameSetting::LiveFrameOn:       sf->SetLiveFrameOn(v->AsBool());         break;
    case SafeFrameSetting::ActionFrameOn:     sf->SetActionFrameOn(v->AsBool());       break;
    case SafeFrameSetting::ActionFrameLinked: sf->SetActionFrameLinked(v->AsBool());   break;
    case SafeFrameSetting::ActionFrameXScale: sf->SetActionFrameXScale(v->AsDouble()); break;
    case SafeFrameSetting::ActionFrameYScale: sf->SetActionFrameYScale(v->AsDouble()); break;
    case SafeFrameSetting::TitleFrameOn:      sf->SetTitleFrameOn(v->AsBool());        break;
    case SafeFrameSetting::TitleFrameLinked:  sf->SetTitleFrameLinked(v->AsBool());    break;
    case SafeFrameSetting::TitleFrameXScale:  sf->SetTitleFrameXScale(v->AsDouble());  break;
    case SafeFrameSetting::TitleFrameYScale:  sf->SetTitleFrameYScale(v->AsDouble());  break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_SafeFrame_SetValue(ON_3dmRenderSettings* rs, SafeFrameSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_SafeFrame_SetValue(&rs->SafeFrame(), which, v);
  }
}

RH_C_FUNCTION const ON_SafeFrame* ON_SafeFrame_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.SafeFrame();
}

RH_C_FUNCTION ON_SafeFrame* ON_SafeFrame_New()
{
  return new ON_SafeFrame;
}

RH_C_FUNCTION void ON_SafeFrame_Delete(ON_SafeFrame* p)
{
  delete p;
}

RH_C_FUNCTION void ON_SafeFrame_CopyFrom(ON_SafeFrame* target, const ON_SafeFrame* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
