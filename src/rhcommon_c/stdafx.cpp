/////////////////////////////////////////////////////////////////////////////
// stdafx.cpp : source file that includes just the standard includes
// rhcommon_c.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

// These defines disable the CDynLinkLibrary tests
// because that test is not needed for Rhino.exe.
#define ON_NEW_MFC_EARLY_ALLOCATION_MAX 0
#define ON_NEW_MFC_EARLY_ALLOCATION_MIN 2

#if !defined(RHINO3DMIO_BUILD)
#include "../../../opennurbs/opennurbs_plus_memory_new.cpp"
#endif

#if defined(RHINO3DMIO_BUILD)
class Rhino3dmIoInitializer
{
public:
  Rhino3dmIoInitializer()
  {
    ON::Begin();
  }

  ~Rhino3dmIoInitializer()
  {
    ON::End();
  }
};

static Rhino3dmIoInitializer theInitializer;
#endif