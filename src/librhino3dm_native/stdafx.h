/////////////////////////////////////////////////////////////////////////////
// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently

#pragma once

#if defined(_WIN32) && !defined(RHINO3DM_BUILD)

#define RHINO_SDK_MFC

#include "../../../rhino4/RhinoCorePlugInStdAfx.h"

// TODO: Add additional include files here
#include <afxadv.h>
#include <afxwin.h>

#else

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN		      // Exclude rarely-used stuff from Windows headers
#endif

#endif //(_WIN32) && !(RHINO3DM_BUILD)


#if !defined(RHINO3DM_BUILD)

#if defined(_WIN32)
#include "../../../rhino4/SDK/inc/RhinoSdkUiFile.h"
#endif

#if defined (__linux__) || defined(__ANDROID__) || defined(ANDROID)
#define RHINO_SDK_MFC
#define OPENNURBS_PLUS
#define ON_RUNTIME_LINUX
#include "../../../rhino4/RhinoCorePlugInStdAfx.h"
#include "../../../rhino4/AfxLinux.h"
#endif

#if defined (__APPLE__)
#define RHINO_SDK_MFC
#include "../../../rhino4/RhinoCorePlugInStdAfx.h"
#include "../../../rhino4/AfxMac.h"
#include "../../../rhino4/MacOS/MacHelpers.h"
#endif

#else

// NOTE: THIS NEEDS TO BE CHANGED WHEN WE RELEASE A PUBLIC BUILD
#define RHINO_CORE_COMPONENT 1
#include "../lib/opennurbs/opennurbs.h"
#endif

// Rhino System Plug-in linking pragmas
#include "plugin_linking_pragmas.h"

#include "rhcommon_c/rhcommon_c_api.h"
#include "rhcommon_c.h"

#if defined(ON_COMPILER_MSC)
// Enable
// C4100 'identifier' : unreferenced formal parameter
//
//  This warning means a dev changed the body of a .NET pinvoked function and
//  didn't bother to adjust the declaration which would expose places where
//  the .NET function(s) that use the pinvoked function are obsolete or broken.
//
//  This warning is off in Rhino code because it is generated by virtual
//  interface functions that intentionally ignore the parameter and 
//  keeping intellisense working requires a token after the parameter.
#pragma warning(default:4100)
#endif
