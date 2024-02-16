#pragma once

#ifndef RH_C_FUNCTION

struct ON_2DPOINT_STRUCT { double val[2]; };
struct ON_2DVECTOR_STRUCT { double val[2]; };
struct ON_INTERVAL_STRUCT { double val[2]; };
struct ON_2FVECTOR_STRUCT { double val[2]; };

struct ON_3DPOINT_STRUCT { double val[3]; };
struct ON_LINE_STRUCT { ON_3DPOINT_STRUCT from; ON_3DPOINT_STRUCT to; };
struct ON_3DVECTOR_STRUCT { double val[3]; };

struct ON_4DPOINT_STRUCT { double val[4]; };
struct ON_4DVECTOR_STRUCT { double val[4]; };
//struct ON_PLANEEQ_STRUCT{ double val[4]; };
struct ON_2INTS { int val[2]; };

struct ON_3FVECTOR_STRUCT { float val[3]; };
struct ON_3FPOINT_STRUCT { float val[3]; };

struct ON_4FVECTOR_STRUCT { float val[4]; };
struct ON_4FPOINT_STRUCT { float val[4]; };

struct ON_XFORM_STRUCT { double val[16]; };

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
#endif

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
  DISPLAY_FILEIO_OPTIONS_DIALOG_PROC displayOptionsDialog
);

RH_C_FUNCTION CRhinoPlugInImpl* CRhinoPlugIn_Pointer(int serialNumber);

RH_C_FUNCTION int CRhinoCommand_New(CRhinoPlugInImpl* pPlugIn, GUID id,
  const RHMONO_STRING* englishName, const RHMONO_STRING* localName, int commandStyle, int commandtype);

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
RH_C_FUNCTION void CRhinoDoc_Redraw(unsigned int docSerialNumber);
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
