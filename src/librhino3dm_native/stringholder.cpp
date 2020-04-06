#include "stdafx.h"

#if defined(__APPLE__) && defined(RHINO3DM_BUILD)
ON_wString unichar2on(const unichar* inStr)
{
  // get length of inStr
  int inStrLen;
  for (inStrLen=0; inStr[inStrLen]; inStrLen++)
    ;
  
  // create an ON_wString with sufficient length
  ON_wString wstr;
  wstr.SetLength(inStrLen);
  
  // copy inStr into wstr
  int idx;
  for (idx=0; idx<inStrLen; idx++)
    wstr[idx] = inStr[idx];
  return wstr;
}
#endif

CRhCmnStringHolder::CRhCmnStringHolder()
{
#if defined(__APPLE__)
  m_macString = NULL;
#endif
}

CRhCmnStringHolder::~CRhCmnStringHolder()
{
#if defined(__APPLE__)
  if( NULL!=m_macString )
    free(m_macString);
#endif
}

void CRhCmnStringHolder::Set(const ON_wString& s)
{
#if defined(__APPLE__)
  if( NULL!=m_macString )
  {
    free(m_macString);
    m_macString = NULL;
  }
  
  const wchar_t* inStr = s.Array();
  if( inStr != NULL )
  {
    int strLen = s.Length();
    m_macString = (unichar*) calloc (sizeof(unichar), strLen+1);
    for (int i=0; i<strLen; i++)
      m_macString[i] = inStr[i];
  }
#else
  m_winString = s;
#endif
}

const RHMONO_STRING* CRhCmnStringHolder::Array() const
{
#if defined (_WIN32)
    return m_winString.Array();
#endif
    
#if defined(__APPLE__)
  return m_macString;
#endif

#if defined (ON_COMPILER_ANDROIDNDK) || defined(ON_RUNTIME_LINUX)
  const ON__UINT32* sUTF32 = (const ON__UINT32*)m_winString.Array();
  int length = m_winString.Length();
  CRhCmnStringHolder* pThis = const_cast<CRhCmnStringHolder*>(this);
  pThis->m_android.Reserve(length+1);
  pThis->m_android.Zero();
  ON__UINT16* sUTF16 = pThis->m_android.Array();
  unsigned int error_status = 0;
  unsigned int error_mask = 0xFFFFFFFF;
  ON__UINT32 error_code_point = 0xFFFD;
  ON_ConvertUTF32ToUTF16( 0, sUTF32, length, sUTF16, length,
                         &error_status, error_mask, error_code_point, 0);
  return sUTF16;
#endif
}
