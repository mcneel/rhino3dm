#include "stdafx.h"

RH_C_FUNCTION ON_TextLog* ON_TextLog_New(ON_wString* pString)
{
  if( pString )
    return new ON_TextLog(*pString);
  return nullptr;
}

class HackTextLog : public ON_TextLog
{
public:
  FILE* GetFile(){ return this->m_pFile; }
};

RH_C_FUNCTION void ON_TextLog_Delete(ON_TextLog* pTextLog)
{
  if( pTextLog )
  {
    HackTextLog* pHack = (HackTextLog*)(pTextLog);
    FILE* f = pHack->GetFile();
    if( f )
      ON::CloseFile(f);
    delete pTextLog;
  }
}

RH_C_FUNCTION ON_TextLog* ON_TextLog_New2(const RHMONO_STRING* _filename)
{
  INPUTSTRINGCOERCE(filename, _filename);
  if( filename && filename[0] )
  {
    FILE* text_fp = ON::OpenFile(filename,L"w");
    if( !text_fp )
      return nullptr;
    return new ON_TextLog(text_fp);
  }
  return new ON_TextLog();
}

RH_C_FUNCTION void ON_TextLog_PushPopIndent(ON_TextLog* pTextLog, bool push)
{
  if( pTextLog )
  {
    if( push )
      pTextLog->PushIndent();
    else
      pTextLog->PopIndent();
  }
}

RH_C_FUNCTION int ON_TextLog_IndentSize_Get(const ON_TextLog* pConstTextLog)
{
  if( pConstTextLog )
    return pConstTextLog->IndentSize();
  return 0;
}

RH_C_FUNCTION void ON_TextLog_IndentSize_Set(ON_TextLog* pTextLog, int s)
{
  if( pTextLog )
    pTextLog->SetIndentSize(s);
}

RH_C_FUNCTION void ON_TextLog_PrintWrappedText(ON_TextLog* pTextLog, const RHMONO_STRING* _text, int line_length)
{
  if( pTextLog )
  {
    INPUTSTRINGCOERCE(text, _text);
    pTextLog->PrintWrappedText(text, line_length);
  }
}

RH_C_FUNCTION void ON_TextLog_Print(ON_TextLog* pTextLog, const RHMONO_STRING* _text)
{
  if( pTextLog )
  {
    INPUTSTRINGCOERCE(text, _text);
    pTextLog->Print(text);
  }
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_TextLog* ON_TextLog_GetNewCRhinoDump()
{
  return new CRhinoDump();
}
#endif
