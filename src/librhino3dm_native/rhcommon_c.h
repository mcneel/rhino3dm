#pragma once

#ifndef RH_C_FUNCTION

struct ON_2DPOINT_STRUCT { double val[2]; };
struct ON_3DPOINT_STRUCT { double val[3]; };

struct ON_2DVECTOR_STRUCT { double val[2]; };
struct ON_3DVECTOR_STRUCT { double val[3]; };

struct ON_4DPOINT_STRUCT { double val[4]; };
struct ON_4DVECTOR_STRUCT { double val[4]; };

struct ON_2FVECTOR_STRUCT { float val[2]; };

struct ON_3FPOINT_STRUCT { float val[3]; };
struct ON_3FVECTOR_STRUCT { float val[3]; };

struct ON_4FVECTOR_STRUCT { float val[4]; };
struct ON_4FPOINT_STRUCT { float val[4]; };

struct ON_XFORM_STRUCT { double val[16]; };

struct ON_INTERVAL_STRUCT { double val[2]; };
struct ON_LINE_STRUCT { ON_3DPOINT_STRUCT from; ON_3DPOINT_STRUCT to; };

//struct ON_PLANEEQ_STRUCT{ double val[4]; };
struct ON_2INTS { int val[2]; };

// Blittable stand-in for ON_SubDEdgeSharpness (two floats: start and end sharpness).
struct ON_SUBD_EDGE_SHARPNESS_STRUCT { float val[2]; };

struct ON_PLANE_STRUCT
{
  double origin[3];
  double xaxis[3];
  double yaxis[3];
  double zaxis[3];
  double eq[4];
};

struct ON_CIRCLE_STRUCT
{
  ON_PLANE_STRUCT plane;
  double radius;
};

// Flat form of ON_SubDComponentParameter.
//
// component_type is an ON_SubDComponentPtr::Type value
// (0 = unset, 2 = vertex, 4 = edge, 6 = face).
// component_id and component_dir identify the referenced SubD component.
//
// The remaining fields depend on component_type:
//   vertex: value_a = active edge id, value_b = active face id.
//   edge:   value_a = active face id, p[0] = edge parameter in [0,1].
//   face:   value_a = face corner index, value_b = face edge count,
//           p[0] = corner s, p[1] = corner t, both in [0,1/2].
//
// The uint count is even so that p[] is 8 byte aligned with no implicit padding.
struct ON_SUBD_COMPONENT_PARAMETER_STRUCT
{
  unsigned int component_type;
  unsigned int component_id;
  unsigned int component_dir;
  unsigned int value_a;
  unsigned int value_b;
  unsigned int reserved;
  double p[2];
};

#ifdef USING_RH_C_SDK
#define RH_C_FUNCTION extern "C" __declspec(dllimport)
#else
#define RH_C_FUNCTION extern "C" __declspec(dllexport)
#endif
class ON_ObjectImpl { ON_ObjectImpl() = delete; };
class ON_GeometryImpl : public ON_ObjectImpl { ON_GeometryImpl() = delete; };
class ON_CurveImpl : public ON_GeometryImpl { ON_CurveImpl() = delete; };
class ON_ArcCurveImpl : public ON_CurveImpl { ON_ArcCurveImpl() = delete; };
class ON_LineCurveImpl : public ON_CurveImpl { ON_LineCurveImpl() = delete; };
class ON_3dmObjectAttributesImpl;
class ON_BinaryArchiveImpl;
class ON_SimpleArray_CRhinoObjectPairImpl;
class ON_wStringImpl;

class CArgsRhinoGetCircleImpl;
class CRhinoDocImpl;
class CRhinoFileReadOptionsImpl;
class CRhinoFileWriteOptionsImpl;
class CRhinoHistoryImpl;
class CRhinoHistoryRecordImpl;
class CRhinoObjectImpl;
class CRhinoPlugInImpl;
class CRhinoSettingsImpl;

typedef int (CALLBACK* CRHINOPLUGIN_ONCALLWRITEDOCPROC)(int pluginSerialNumber, const class CRhinoFileWriteOptionsImpl*);
typedef int (CALLBACK* CRHINOPLUGIN_WRITEDOCPROC)(int pluginSerialNumber, unsigned int docSerialNumber, class ON_BinaryArchiveImpl*, const class CRhinoFileWriteOptionsImpl*);
typedef int (CALLBACK* CRHINOPLUGIN_READDOCPROC)(int pluginSerialNumber, unsigned int docSerialNumber, class ON_BinaryArchiveImpl*, const class CRhinoFileReadOptionsImpl*);
#else

typedef ON_3dmObjectAttributes ON_3dmObjectAttributesImpl;
typedef ON_ArcCurve ON_ArcCurveImpl;
typedef ON_BinaryArchive ON_BinaryArchiveImpl;
typedef ON_Curve ON_CurveImpl;
typedef ON_Geometry ON_GeometryImpl;
typedef ON_LineCurve ON_LineCurveImpl;
typedef ON_Object ON_ObjectImpl;
typedef ON_wString ON_wStringImpl;

#if !defined(RHINO3DM_BUILD)
typedef ON_SimpleArray<CRhinoObjectPair> ON_SimpleArray_CRhinoObjectPairImpl;
typedef CArgsRhinoGetCircle CArgsRhinoGetCircleImpl;
typedef CRhinoPlugIn CRhinoPlugInImpl;
typedef CRhinoObject CRhinoObjectImpl;
typedef CRhinoHistoryRecord CRhinoHistoryRecordImpl;
typedef CRhinoDoc CRhinoDocImpl;
typedef CRhinoHistory CRhinoHistoryImpl;
typedef CRhinoFileWriteOptions CRhinoFileWriteOptionsImpl;
typedef CRhinoFileReadOptions CRhinoFileReadOptionsImpl;
typedef CRhinoSettings CRhinoSettingsImpl;

typedef int (CALLBACK* CRHINOPLUGIN_ONCALLWRITEDOCPROC)(int pluginSerialNumber, const class CRhinoFileWriteOptions*);
typedef int (CALLBACK* CRHINOPLUGIN_WRITEDOCPROC)(int pluginSerialNumber, unsigned int docSerialNumber, class ON_BinaryArchive*, const class CRhinoFileWriteOptions*);
typedef int (CALLBACK* CRHINOPLUGIN_READDOCPROC)(int pluginSerialNumber, unsigned int docSerialNumber, class ON_BinaryArchive*, const class CRhinoFileReadOptions*);
typedef bool (CALLBACK* CRHINOPLUGIN_DISPLAYHELP)(int pluginSerialNumber, HWND hwndParent);
#endif

//////////////////////////////////////////////////////////////////////////
//
// Marshalling invariants
//
// The *_STRUCT types above are blittable stand-ins for opennurbs classes. RhinoCommon
// declares a matching C# struct for each one and passes it across the interop boundary by
// value, so a size change on either side silently corrupts arguments instead of failing to
// build. Assert the sizes here, where both the stand-in and the real class are visible.
//
// Small classes are also asserted to be trivially copyable. Both the Microsoft x64 and the
// Itanium C++ ABIs pass a trivially copyable class of 1, 2, 4 or 8 bytes in a register;
// giving such a class a copy constructor or a destructor switches it to pass-by-address and
// breaks every by-value RH_C_FUNCTION taking it. Note that std::is_trivially_copyable
// already requires a trivial destructor, so there is no separate destructible assert.
//
#include <type_traits>

static_assert(sizeof(ON_2DPOINT_STRUCT) == sizeof(ON_2dPoint), "ON_2DPOINT_STRUCT no longer matches ON_2dPoint.");
static_assert(16 == sizeof(ON_2DPOINT_STRUCT), "ComponentIndex marshalling assumes ON_2DPOINT_STRUCT is 16 bytes.");
static_assert(std::is_trivially_copyable<ON_2DPOINT_STRUCT>::value, "ON_2DPOINT_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_3DPOINT_STRUCT) == sizeof(ON_3dPoint), "ON_3DPOINT_STRUCT no longer matches ON_3dPoint.");
static_assert(24 == sizeof(ON_3DPOINT_STRUCT), "ComponentIndex marshalling assumes ON_3DPOINT_STRUCT is 24 bytes.");
static_assert(std::is_trivially_copyable<ON_3DPOINT_STRUCT>::value, "ON_3DPOINT_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_2DVECTOR_STRUCT) == sizeof(ON_2dVector), "ON_2DVECTOR_STRUCT no longer matches ON_2dVector.");
static_assert(16 == sizeof(ON_2DVECTOR_STRUCT), "ComponentIndex marshalling assumes ON_2DVECTOR_STRUCT is 16 bytes.");
static_assert(std::is_trivially_copyable<ON_2DVECTOR_STRUCT>::value, "ON_2DVECTOR_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_3DVECTOR_STRUCT) == sizeof(ON_3dVector), "ON_3DVECTOR_STRUCT no longer matches ON_3dVector.");
static_assert(24 == sizeof(ON_3DVECTOR_STRUCT), "ComponentIndex marshalling assumes ON_3DVECTOR_STRUCT is 24 bytes.");
static_assert(std::is_trivially_copyable<ON_3DVECTOR_STRUCT>::value, "ON_3DVECTOR_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_4DPOINT_STRUCT) == sizeof(ON_4dPoint), "ON_4DPOINT_STRUCT no longer matches ON_4dPoint.");
static_assert(32 == sizeof(ON_4DPOINT_STRUCT), "ComponentIndex marshalling assumes ON_4DPOINT_STRUCT is 32 bytes.");
static_assert(std::is_trivially_copyable<ON_4DPOINT_STRUCT>::value, "ON_4DPOINT_STRUCT must stay trivially copyable to be passed by value.");

// ON_4DVECTOR_STRUCT has no opennurbs counterpart to compare against; opennurbs has no
// ON_4dVector, and the struct is only used through the C# Vector4d type.

static_assert(sizeof(ON_4FVECTOR_STRUCT) == sizeof(ON_4fColor), "ON_4FVECTOR_STRUCT no longer matches ON_4fColor.");
static_assert(16 == sizeof(ON_4FVECTOR_STRUCT), "ComponentIndex marshalling assumes ON_4FVECTOR_STRUCT is 16 bytes.");
static_assert(std::is_trivially_copyable<ON_4FVECTOR_STRUCT>::value, "ON_4FVECTOR_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_2FVECTOR_STRUCT) == sizeof(ON_2fVector), "ON_2FVECTOR_STRUCT no longer matches ON_2fVector.");
static_assert(8 == sizeof(ON_2FVECTOR_STRUCT), "ComponentIndex marshalling assumes ON_2FVECTOR_STRUCT is 8 bytes.");
static_assert(std::is_trivially_copyable<ON_2FVECTOR_STRUCT>::value, "ON_2FVECTOR_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_3FPOINT_STRUCT) == sizeof(ON_3fPoint), "ON_3FPOINT_STRUCT no longer matches ON_3fPoint.");
static_assert(12 == sizeof(ON_3FPOINT_STRUCT), "ComponentIndex marshalling assumes ON_3FPOINT_STRUCT is 12 bytes.");
static_assert(std::is_trivially_copyable<ON_3FPOINT_STRUCT>::value, "ON_3FPOINT_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_3FVECTOR_STRUCT) == sizeof(ON_3fVector), "ON_3FVECTOR_STRUCT no longer matches ON_3fVector.");
static_assert(12 == sizeof(ON_3FVECTOR_STRUCT), "ComponentIndex marshalling assumes ON_3FVECTOR_STRUCT is 12 bytes.");
static_assert(std::is_trivially_copyable<ON_3FVECTOR_STRUCT>::value, "ON_3FVECTOR_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_4FPOINT_STRUCT) == sizeof(ON_4fPoint), "ON_4FPOINT_STRUCT no longer matches ON_4fPoint.");
static_assert(16 == sizeof(ON_4FPOINT_STRUCT), "ComponentIndex marshalling assumes ON_4FPOINT_STRUCT is 16 bytes.");
static_assert(std::is_trivially_copyable<ON_4FPOINT_STRUCT>::value, "ON_4FPOINT_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_XFORM_STRUCT) == sizeof(ON_Xform), "ON_XFORM_STRUCT no longer matches ON_Xform.");
static_assert(128 == sizeof(ON_XFORM_STRUCT), "ComponentIndex marshalling assumes ON_XFORM_STRUCT is 128 bytes.");
static_assert(std::is_trivially_copyable<ON_XFORM_STRUCT>::value, "ON_XFORM_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_INTERVAL_STRUCT) == sizeof(ON_Interval), "ON_INTERVAL_STRUCT no longer matches ON_Interval.");
static_assert(16 == sizeof(ON_INTERVAL_STRUCT), "ComponentIndex marshalling assumes ON_INTERVAL_STRUCT is 16 bytes.");
static_assert(std::is_trivially_copyable<ON_INTERVAL_STRUCT>::value, "ON_INTERVAL_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_LINE_STRUCT) == sizeof(ON_Line), "ON_LINE_STRUCT no longer matches ON_Line.");
static_assert(48 == sizeof(ON_LINE_STRUCT), "ComponentIndex marshalling assumes ON_LINE_STRUCT is 48 bytes.");
static_assert(std::is_trivially_copyable<ON_LINE_STRUCT>::value, "ON_LINE_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_PLANE_STRUCT) == sizeof(ON_Plane), "ON_PLANE_STRUCT no longer matches ON_Plane.");
static_assert(128 == sizeof(ON_PLANE_STRUCT), "ComponentIndex marshalling assumes ON_PLANE_STRUCT is 128 bytes.");
static_assert(std::is_trivially_copyable<ON_PLANE_STRUCT>::value, "ON_PLANE_STRUCT must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_CIRCLE_STRUCT) == sizeof(ON_Circle), "ON_CIRCLE_STRUCT no longer matches ON_Circle.");
static_assert(136 == sizeof(ON_CIRCLE_STRUCT), "ComponentIndex marshalling assumes ON_CIRCLE_STRUCT is 136 bytes.");
static_assert(std::is_trivially_copyable<ON_CIRCLE_STRUCT>::value, "ON_CIRCLE_STRUCT must stay trivially copyable to be passed by value.");

// ON_2INTS is the legacy stand-in for ON_COMPONENT_INDEX, and both are marshalled as the
// C# ComponentIndex struct.
static_assert(sizeof(ON_2INTS) == sizeof(ON_COMPONENT_INDEX), "ON_2INTS no longer matches ON_COMPONENT_INDEX.");
static_assert(8 == sizeof(ON_COMPONENT_INDEX), "ComponentIndex marshalling assumes ON_COMPONENT_INDEX is 8 bytes.");
static_assert(std::is_trivially_copyable<ON_COMPONENT_INDEX>::value, "ON_COMPONENT_INDEX must stay trivially copyable to be passed by value.");

static_assert(sizeof(ON_SUBD_EDGE_SHARPNESS_STRUCT) == sizeof(ON_SubDEdgeSharpness), "ON_SUBD_EDGE_SHARPNESS_STRUCT no longer matches ON_SubDEdgeSharpness.");
static_assert(8 == sizeof(ON_SubDEdgeSharpness), "SubDEdgeSharpness marshalling assumes ON_SubDEdgeSharpness is 8 bytes.");
static_assert(std::is_trivially_copyable<ON_SubDEdgeSharpness>::value, "ON_SubDEdgeSharpness must stay trivially copyable to be passed by value.");

#endif

#ifndef RHMONO_STRING
#define RHMONO_STRING wchar_t
#endif

enum PlugInType : int
{
  UtilityPlugIn = 0,
  FileImportPlugIn = 1,
  FileExportPlugIn = 2,
  DigitizerPlugIn = 3,
  RenderPlugIn = 4
};

RH_C_FUNCTION int ON_BinaryArchive_Archive3dmVersion(const ON_BinaryArchiveImpl* constArchive);
RH_C_FUNCTION bool ON_BinaryArchive_Write3dmChunkVersion(ON_BinaryArchiveImpl* archive, int major, int minor);
RH_C_FUNCTION bool ON_BinaryArchive_Read3dmChunkVersion(ON_BinaryArchiveImpl* archive, int* major, int* minor);
RH_C_FUNCTION bool ON_BinaryArchive_ReadInt(ON_BinaryArchiveImpl* archive, int* val);
RH_C_FUNCTION bool ON_BinaryArchive_WriteInt(ON_BinaryArchiveImpl* archive, int val);

RH_C_FUNCTION ON_wStringImpl* ON_wString_New(const RHMONO_STRING* _text);
RH_C_FUNCTION void ON_wString_Delete(ON_wStringImpl* pString);


RH_C_FUNCTION void ON_Object_Delete(ON_ObjectImpl* pObject);

RH_C_FUNCTION int ON_Geometry_Dimension(const ON_GeometryImpl* constGeometry);
RH_C_FUNCTION bool ON_Geometry_IsDeformable(const ON_GeometryImpl* constGeometry);
RH_C_FUNCTION bool ON_Geometry_MakeDeformable(ON_GeometryImpl* geometry);


RH_C_FUNCTION bool ON_Curve_ChangeClosedCurveSeam(ON_CurveImpl* curve, double t);
RH_C_FUNCTION int ON_Curve_Degree(const ON_CurveImpl* constCurve);
RH_C_FUNCTION int ON_Curve_HasNurbForm(const ON_CurveImpl* constCurve);
RH_C_FUNCTION bool ON_Curve_IsLinear(const ON_CurveImpl* constCurve, double tolerance);
RH_C_FUNCTION bool ON_Curve_ChangeDimension(ON_CurveImpl* curve, int desiredDimension);
RH_C_FUNCTION bool ON_Curve_IsClosed(const ON_CurveImpl* constCurve);
RH_C_FUNCTION bool ON_Curve_IsPeriodic(const ON_CurveImpl* constCurve);


RH_C_FUNCTION ON_ArcCurveImpl* ON_ArcCurve_New4(const ON_CIRCLE_STRUCT* pCircle);
RH_C_FUNCTION bool ON_ArcCurve_IsCircle(const ON_ArcCurveImpl* constArcCurve);


RH_C_FUNCTION ON_LineCurveImpl* ON_LineCurve_New(const ON_LineCurveImpl* constOtherLineCurve);
RH_C_FUNCTION ON_LineCurveImpl* ON_LineCurve_New2(ON_2DPOINT_STRUCT from, ON_2DPOINT_STRUCT to);
RH_C_FUNCTION ON_LineCurveImpl* ON_LineCurve_New3(ON_3DPOINT_STRUCT from, ON_3DPOINT_STRUCT to);


#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_Geometry_IsMorphable(const ON_GeometryImpl* constGeometry);
RH_C_FUNCTION int CRhinoPlugIn_New(GUID id, const RHMONO_STRING* name, const RHMONO_STRING* version, enum PlugInType kind, int loadtime);

typedef int (CALLBACK* CRHINOPLUGIN_ONLOADPROC)(int pluginSerialNumber);
typedef void (CALLBACK* CRHINOPLUGIN_SHUTDOWNPROC)(int pluginSerialNumber);
typedef LPUNKNOWN (CALLBACK* CRHINOPLUGIN_GETPLUGINOBJECTPROC)(int pluginSerialNumber);
typedef int (CALLBACK* DISPLAY_FILEIO_OPTIONS_DIALOG_QUERY_PROC)(int serialNumber);
typedef void (CALLBACK* DISPLAY_FILEIO_OPTIONS_DIALOG_PROC)(int serialNumber, HWND parent, const wchar_t* fileDescription, const wchar_t* fileExtension);

typedef void (CALLBACK* UNKNOWN_USERDATA_PROC)(unsigned int rsn, ON_UUID plugin_id);

RH_C_FUNCTION void CRhinoPlugIn_SetCallbacks(
  int plugInSerialNumber,
  CRHINOPLUGIN_ONLOADPROC onload,
  CRHINOPLUGIN_SHUTDOWNPROC shutdown,
  CRHINOPLUGIN_GETPLUGINOBJECTPROC getobj,
  CRHINOPLUGIN_ONCALLWRITEDOCPROC callwritedoc,
  CRHINOPLUGIN_WRITEDOCPROC writedoc,
  CRHINOPLUGIN_READDOCPROC readdoc,
  DISPLAY_FILEIO_OPTIONS_DIALOG_PROC displayOptionsDialog,
  CRHINOPLUGIN_DISPLAYHELP displayhelp
);

RH_C_FUNCTION CRhinoPlugInImpl* CRhinoPlugIn_Pointer(int serialNumber);

RH_C_FUNCTION int CRhinoCommand_New(CRhinoPlugInImpl* pPlugIn, GUID id,
  const RHMONO_STRING* englishName, const RHMONO_STRING* localName, int commandStyle, int commandtype, bool callRegister);

typedef int (CALLBACK* CRHINOCOMMAND_RUNPROC)(int commandSerialNumber, unsigned int docSerialNumber, int mode);
typedef int (CALLBACK* CRHINOCOMMAND_SELPROC)(int commandSerialNumber, const CRhinoObjectImpl* pConstRhinoObject);
typedef int (CALLBACK* CRHINOCOMMAND_SELSUBOBJECTPROC)(int commandSerialNumber, const CRhinoObjectImpl* pConstRhinoObject, ON_SimpleArray<ON_COMPONENT_INDEX>* indices);
typedef void (CALLBACK* CRHINOCOMMAND_DOHELPPROC)(int commandSerialNumber);
typedef int (CALLBACK* CRHINOCOMMAND_CONTEXTHELPPROC)(int commandSerialNumber, ON_wStringImpl*);
typedef int (CALLBACK* CRHINOCOMMAND_REPLAYHISTORYPROC)(int commandSerialNumber, const CRhinoHistoryRecordImpl* pConstRhinoHistoryRecord, ON_SimpleArray_CRhinoObjectPairImpl* results);
RH_C_FUNCTION void CRhinoCommand_SetCallbacks(
  int commandSerialNumber,
  CRHINOCOMMAND_RUNPROC run_func,
  CRHINOCOMMAND_DOHELPPROC dohelp_func,
  CRHINOCOMMAND_CONTEXTHELPPROC contexthelp_func,
  CRHINOCOMMAND_REPLAYHISTORYPROC replayhistory_func,
  CRHINOCOMMAND_SELPROC sel_func,
  CRHINOCOMMAND_SELSUBOBJECTPROC sel_subobject_func
);
RH_C_FUNCTION CRhinoSettingsImpl* CRhinoCommand_Settings(int commandSerialNumber);

RH_C_FUNCTION bool CRhinoApp_GetRhinoSchemeRegistryPath(bool fullPath, ON_wStringImpl* wstring);
RH_C_FUNCTION void CRhinoApp_Print(const RHMONO_STRING* s);
RH_C_FUNCTION void CRhinoApp_SetCommandPrompt(const RHMONO_STRING* prompt, const RHMONO_STRING* prompt_default);


RH_C_FUNCTION CRhinoDocImpl* CRhinoDoc_GetFromId(unsigned int docSerialNumber);
RH_C_FUNCTION void CRhinoDoc_Redraw(unsigned int docSerialNumber, bool deferred);
RH_C_FUNCTION GUID CRhinoDoc_AddPoint(unsigned int docSerialNumber, ON_3DPOINT_STRUCT point, const ON_3dmObjectAttributesImpl* attrs, CRhinoHistoryImpl* pHistory, bool reference);
RH_C_FUNCTION GUID CRhinoDoc_AddCircle(unsigned int docSerialNumber, const ON_CIRCLE_STRUCT* pCircle, const ON_3dmObjectAttributesImpl* attr, CRhinoHistoryImpl* pHistory, bool reference);
RH_C_FUNCTION GUID CRhinoDoc_AddCurve(unsigned int docSerialNumber, const ON_CurveImpl* pCurve, const ON_3dmObjectAttributesImpl* attr, CRhinoHistoryImpl* pHistory, bool reference);
RH_C_FUNCTION unsigned int CRhinoDoc_BeginUndoRecord(unsigned int docSerialNumber, const RHMONO_STRING* description);
RH_C_FUNCTION bool CRhinoDoc_EndUndoRecord(unsigned int docSerialNumber, unsigned int sn);


RH_C_FUNCTION bool CRhinoSettings_SetDouble(CRhinoSettingsImpl* settings, const RHMONO_STRING* key, double value);
RH_C_FUNCTION bool CRhinoSettings_SetInteger(CRhinoSettingsImpl* settings, const RHMONO_STRING* key, int value);
RH_C_FUNCTION bool CRhinoSettings_SetBool(CRhinoSettingsImpl* settings, const RHMONO_STRING* key, bool value);
RH_C_FUNCTION bool CRhinoSettings_GetDouble(const CRhinoSettingsImpl* constSettings, const RHMONO_STRING* key, double* value, double defaultValue);
RH_C_FUNCTION bool CRhinoSettings_GetInteger(const CRhinoSettingsImpl* constSettings, const RHMONO_STRING* key, int* value, int defaultValue, int lowerBound, int upperBound);
RH_C_FUNCTION bool CRhinoSettings_GetBool(const CRhinoSettingsImpl* constSettings, const RHMONO_STRING* key, bool* value, bool defaultValue);


RH_C_FUNCTION CArgsRhinoGetCircleImpl* CArgsRhinoGetCircle_New();
RH_C_FUNCTION void CArgsRhinoGetCircle_Delete(CArgsRhinoGetCircleImpl* pArgsRhinoGetCircle);
RH_C_FUNCTION void CArgsRhinoGetCircle_SetDefaultSize(CArgsRhinoGetCircleImpl* pArgsGetCircle, double size);
RH_C_FUNCTION double CArgsRhinoGetCircle_DefaultSize(const CArgsRhinoGetCircleImpl* pConstArgsGetCircle);

enum ArgsGetCircleBoolConsts : int
{
  agcAllowDeformable = 0,
  agcDeformable = 1,
  agcUseDiameterMode = 2,
  agcCap = 3
};
RH_C_FUNCTION bool CArgsRhinoGetCircle_GetBool(const CArgsRhinoGetCircleImpl* pConstArgsGetCircle, enum ArgsGetCircleBoolConsts which);
RH_C_FUNCTION void CArgsRhinoGetCircle_SetBool(CArgsRhinoGetCircleImpl* pArgsGetCircle, enum ArgsGetCircleBoolConsts which, bool value);

//TODO: we should probably return an enum for these functions that are returning a CRhinoCommand::result
RH_C_FUNCTION unsigned int RHC_RhinoGetCircle(ON_CIRCLE_STRUCT* circle, CArgsRhinoGetCircleImpl* pArgsGetCircle);
enum ArgsGetCircleIntConsts : int
{
  agcPointCount = 0,
  agcDegree = 1,
};
RH_C_FUNCTION int CArgsRhinoGetCircle_GetInt(const CArgsRhinoGetCircleImpl* pConstArgsGetCircle, enum ArgsGetCircleIntConsts which);
RH_C_FUNCTION void CArgsRhinoGetCircle_SetInt(CArgsRhinoGetCircleImpl* pArgsGetCircle, enum ArgsGetCircleIntConsts which, int value);

#endif
