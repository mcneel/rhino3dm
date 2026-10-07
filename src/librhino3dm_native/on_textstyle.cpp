#include "stdafx.h"

// TODO: Move the following to on_textrun.cpp at some point. This
// will require updating all of the build configuration

RH_C_FUNCTION ON_TextRun* ON_TextRun_GetManagedTextRun()
{
  return ON_TextRun::GetManagedTextRun();
}

RH_C_FUNCTION ON_TextRun* ON_TextRunInit(const ON_Font* font, double height, double stackscale, int argb, bool bold, bool italic, bool underlined, bool strikethrough, const RHMONO_STRING* _text)
{
  INPUTSTRINGCOERCE(text, _text);
  ON_TextRun* rc = ON_TextRun::GetManagedTextRun();
  if (rc)
  {
    ON_Color color = ARGB_to_ABGR(argb);
    rc->Init(font, height, stackscale, color, bold, italic, underlined, strikethrough);
    if (nullptr != _text)
    {
      ON__UINT32* cp = nullptr;
      int cpcount = ON_TextContext::ConvertStringToCodepoints(text, cp);
      rc->SetUnicodeString(cpcount, cp);
      rc->SetType(ON_TextRun::RunType::kText);
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_TextRun_ReturnManagedTextRun(ON_TextRun* textrun)
{
  ON_TextRun::ReturnManagedTextRun(textrun);
}

enum TextRunBool : int
{
  trbIsText = 0,
  trbIsNewLine = 1,
  trbIsColumn = 2,
  trbIsValid = 3,
  trbApplyKerning = 4,
};

RH_C_FUNCTION bool ON_TextRun_GetBool(const ON_TextRun* textrun, enum TextRunBool which)
{
  if (nullptr == textrun)
    return false;

  bool rc = false;
  switch (which) 
  {
  case trbIsText:
    rc = textrun->IsText();
    break;
  case trbIsNewLine:
    rc = textrun->IsNewline();
    break;
  case trbIsColumn:
    rc = textrun->IsColumn();
    break;
  case trbIsValid:
    rc = textrun->IsValid();
    break;
  case trbApplyKerning:
    rc = textrun->ApplyKerning();
    break;
  default:
    break;
  }
  return rc;
}

RH_C_FUNCTION void ON_TextRun_SetBool(ON_TextRun* textrun, enum TextRunBool which, bool val)
{
  if (nullptr == textrun)
    return;

  switch (which)
  {
  case trbApplyKerning:
    textrun->SetApplyKerning(val);
    break;
  default:
    break;
  }
}
