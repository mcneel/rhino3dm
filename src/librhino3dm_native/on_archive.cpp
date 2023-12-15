#include "stdafx.h"

#if defined(ON_RUNTIME_APPLE_MACOS) && !defined(RHINO3DM_BUILD)
#import "../../../rhino4/MacOS/NSImage+QuickLook.h"
#endif

RH_C_FUNCTION CRhCmnStringHolder* StringHolder_New()
{
  return new CRhCmnStringHolder();
}
RH_C_FUNCTION void StringHolder_Delete(CRhCmnStringHolder* pStringHolder)
{
  if( pStringHolder )
    delete pStringHolder;
}
RH_C_FUNCTION const RHMONO_STRING* StringHolder_Get(CRhCmnStringHolder* pStringHolder)
{
  const RHMONO_STRING* rc = nullptr;
  if( pStringHolder )
    rc = pStringHolder->Array();
  return rc;
}


RH_C_FUNCTION bool ON_BinaryArchive_AtEnd(const ON_BinaryArchive* pConstArchive)
{
  if( pConstArchive )
    return pConstArchive->AtEnd();
  return true;
}

RH_C_FUNCTION bool ON_BinaryArchive_Read3dmStartSection(ON_BinaryArchive* pBinaryArchive, int* version, CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;
  if( pBinaryArchive && version && pStringHolder )
  {
    ON_String comment;
    rc = pBinaryArchive->Read3dmStartSection(version, comment);
    ON_wString wComment(comment);
    pStringHolder->Set(wComment);
  }
  return rc;
}



RH_C_FUNCTION unsigned int ON_BinaryArchive_Dump3dmChunk(ON_BinaryArchive* pBinaryArchive, ON_TextLog* pTextLog)
{
  unsigned int rc = 0;
  if( pBinaryArchive && pTextLog )
    rc = pBinaryArchive->Dump3dmChunk(*pTextLog);
  return rc;
}

int ON_BinaryArchive_Archive3dmVersion(const ON_BinaryArchive* constArchive)
{
  if(constArchive)
    return constArchive->Archive3dmVersion();
  return 0;
}

bool ON_BinaryArchive_Write3dmChunkVersion(ON_BinaryArchive* archive, int major, int minor)
{
  bool rc = false;
  if(archive)
    rc = archive->Write3dmChunkVersion(major, minor);
  return rc;
}

bool ON_BinaryArchive_Read3dmChunkVersion(ON_BinaryArchive* archive, int* major, int* minor)
{
  bool rc = false;
  if(archive && major && minor )
    rc = archive->Read3dmChunkVersion(major, minor);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginRead3dmChunk(ON_BinaryArchive* pArchive, unsigned int typecode, int* major, int* minor)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->BeginRead3dmChunk(typecode, major, minor);
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_BeginRead3dmBigChunk(ON_BinaryArchive* pArchive, unsigned int* typecode, ON__INT64* value)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->BeginRead3dmBigChunk(typecode, value);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginWrite3dmChunk(ON_BinaryArchive* pArchive, unsigned int typecode, int major, int minor)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->BeginWrite3dmChunk(typecode, major, minor);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginWrite3dmBigChunk(ON_BinaryArchive* pArchive, unsigned int typecode, ON__INT64 value)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->BeginWrite3dmBigChunk(typecode, value);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EndRead3dmChunk(ON_BinaryArchive* pArchive, bool suppressWarning)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->EndRead3dmChunk(suppressWarning);
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_EndWrite3dmChunk(ON_BinaryArchive* pArchive)
{
  bool rc = false;
  if(pArchive)
  {
    rc = pArchive->EndWrite3dmChunk();
  }
  return rc;
}

RH_C_FUNCTION ON__UINT64 ON_BinaryArchive_CurrentPosition(const ON_BinaryArchive* pConstArchive)
{
  if(pConstArchive)
    return pConstArchive->CurrentPosition();
  return 0;
}

RH_C_FUNCTION bool ON_BinaryArchive_SeekFromCurrentPosition(ON_BinaryArchive* pArchive, ON__INT64 offset)
{
  bool rc = false;
  if (pArchive)
  {
    rc = (offset >= 0)
      ? pArchive->SeekForward((ON__UINT64)offset)
      : pArchive->SeekBackward((ON__UINT64)(-offset));
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_SeekFromCurrentPosition2(ON_BinaryArchive* pArchive, ON__UINT64 offset, bool forward)
{
  bool rc = false;
  if(pArchive)
  {
    if(forward)
      rc = pArchive->SeekForward(offset);
    else
      rc = pArchive->SeekBackward(offset);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_SeekFromStart(ON_BinaryArchive* pArchive, ON__UINT64 offset)
{
  if(pArchive)
    return pArchive->SeekFromStart(offset);
  return false;
}


RH_C_FUNCTION bool ON_BinaryArchive_ReadBool(ON_BinaryArchive* pArchive, bool* readBool)
{
  bool rc = false;
  if( pArchive && readBool )
    rc = pArchive->ReadBool(readBool);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteBool(ON_BinaryArchive* pArchive, bool val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteBool(val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadBool2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/bool* readBool)
{
  bool rc = false;
  if( pArchive && count>0 && readBool )
  {
    char* c = (char*)readBool;
    rc = pArchive->ReadChar(count, c);
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteBool2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const bool* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
  {
    const char* c = (const char*)val;
    rc = pArchive->WriteChar(count, c);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadByte(ON_BinaryArchive* pArchive, char* readByte)
{
  bool rc = false;
  if( pArchive && readByte )
    rc = pArchive->ReadByte(1, readByte);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteByte(ON_BinaryArchive* pArchive, char val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteByte(1, &val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadByte2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/char* readByte)
{
  bool rc = false;
  if( pArchive && count>0 && readByte )
    rc = pArchive->ReadByte(count, readByte);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteByte2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const char* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
    rc = pArchive->WriteByte(count, val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EnableCRCCalculation(ON_BinaryArchive* pArchive, bool enable)
{
  if(pArchive)
    return pArchive->EnableCRCCalculation(enable);
  return false;
}

// 10-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24156
RH_C_FUNCTION bool ON_BinaryArchive_ReadCompressedBufferSize( ON_BinaryArchive* pArchive, unsigned int* size )
{
  bool rc = false;
  if( pArchive && size )
  {
    size_t sizeof_outbuffer = 0;
    rc = pArchive->ReadCompressedBufferSize( &sizeof_outbuffer );
    if( rc )
      *size = (unsigned int)sizeof_outbuffer;
  }
  return rc;
}

// 10-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24156
RH_C_FUNCTION bool ON_BinaryArchive_ReadCompressedBuffer( ON_BinaryArchive* pArchive, unsigned int size, /*ARRAY*/char* pBuffer )
{
  bool rc = false;
  if( pArchive && size > 0 && pBuffer )
  {
    bool bFailedCRC = false;
    rc = pArchive->ReadCompressedBuffer( size, pBuffer, &bFailedCRC );
  }
  return rc;
}

// 10-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24156
RH_C_FUNCTION bool ON_BinaryArchive_WriteCompressedBuffer( ON_BinaryArchive* pArchive, unsigned int size, /*ARRAY*/const char* pBuffer )
{
  bool rc = false;
  if( pArchive && size > 0 && pBuffer )
    rc = pArchive->WriteCompressedBuffer( size, pBuffer );
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadShort(ON_BinaryArchive* pArchive, short* readShort)
{
  bool rc = false;
  if( pArchive && readShort )
    rc = pArchive->ReadShort(readShort);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteShort(ON_BinaryArchive* pArchive, short val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteShort(val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadShort2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/short* readShort)
{
  bool rc = false;
  if( pArchive && count>0 && readShort )
    rc = pArchive->ReadShort(count, readShort);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteShort2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const short* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
    rc = pArchive->WriteShort(count, val);
  return rc;
}

bool ON_BinaryArchive_ReadInt(ON_BinaryArchive* pArchive, int* readInt)
{
  bool rc = false;
  if( pArchive && readInt )
    rc = pArchive->ReadInt(readInt);
  return rc;
}
bool ON_BinaryArchive_WriteInt(ON_BinaryArchive* pArchive, int val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteInt(val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadInt2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/int* readInt)
{
  bool rc = false;
  if( pArchive && count>0 && readInt )
    rc = pArchive->ReadInt(count, readInt);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteInt2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const int* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
    rc = pArchive->WriteInt(count, val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadInt64(ON_BinaryArchive* pArchive, ON__INT64* readInt)
{
  bool rc = false;
  if( pArchive && readInt )
  {
    time_t t;
    rc = pArchive->ReadBigTime(&t);
    *readInt = (ON__INT64)t;
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteInt64(ON_BinaryArchive* pArchive, ON__INT64 val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteBigTime((time_t)val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadSingle(ON_BinaryArchive* pArchive, float* readFloat)
{
  bool rc = false;
  if( pArchive && readFloat )
    rc = pArchive->ReadFloat(readFloat);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteSingle(ON_BinaryArchive* pArchive, float val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteFloat(val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadSingle2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/float* readFloat)
{
  bool rc = false;
  if( pArchive && count>0 && readFloat )
    rc = pArchive->ReadFloat(count, readFloat);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteSingle2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const float* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
    rc = pArchive->WriteFloat(count, val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadDouble(ON_BinaryArchive* pArchive, double* readDouble)
{
  bool rc = false;
  if( pArchive && readDouble )
    rc = pArchive->ReadDouble(readDouble);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteDouble(ON_BinaryArchive* pArchive, double val)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->WriteDouble(val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadDouble2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/double* readDouble)
{
  bool rc = false;
  if( pArchive && count>0 && readDouble )
    rc = pArchive->ReadDouble(count,readDouble);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteDouble2(ON_BinaryArchive* pArchive, int count, /*ARRAY*/const double* val)
{
  bool rc = false;
  if( pArchive && count>0 && val )
    rc = pArchive->WriteDouble(count,val);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadGuid(ON_BinaryArchive* pArchive, ON_UUID* readGuid)
{
  bool rc = false;
  if( pArchive && readGuid )
    rc = pArchive->ReadUuid(*readGuid);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteGuid(ON_BinaryArchive* pArchive, const ON_UUID* val)
{
  bool rc = false;
  if( pArchive && val )
    rc = pArchive->WriteUuid(*val);
  return rc;
}


RH_C_FUNCTION bool ON_BinaryArchive_ReadString(ON_BinaryArchive* pArchive, CRhCmnStringHolder* pStringHolder, bool wide)
{
  bool rc = false;
  if( pArchive && pStringHolder )
  {
    if( wide)
    {
      ON_wString str;
      rc = pArchive->ReadString(str);
      pStringHolder->Set(str);
    }
    else
    {
      ON_String str;
      rc = pArchive->ReadString(str);
      pStringHolder->Set(str);
    }
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteString(ON_BinaryArchive* pArchive, const RHMONO_STRING* str, bool wide)
{
  bool rc = false;
  if( pArchive && str )
  {
    INPUTSTRINGCOERCE(_str, str);
    if( wide )
    {
      ON_wString s(_str);
      rc = pArchive->WriteString(s);
    }
    else
    {
      ON_String s(_str);
      rc = pArchive->WriteString(s);
    }
  }
  return rc;
}


RH_C_FUNCTION bool ON_BinaryArchive_ReadColor(ON_BinaryArchive* pArchive, int* argb)
{
  bool rc = false;
  if( pArchive && argb )
  {
    ON_Color c;
    rc = pArchive->ReadColor(c);
    if( rc )
    {
      unsigned int _c = (unsigned int)c;
      *argb = (int)ABGR_to_ARGB(_c);
    }
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteColor(ON_BinaryArchive* pArchive, int argb)
{
  bool rc = false;
  if( pArchive  )
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    ON_Color c(abgr);
    rc = pArchive->WriteColor(c);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadTransform(ON_BinaryArchive* pArchive, ON_Xform* xf)
{
  bool rc = false;
  if( pArchive && xf )
    rc = pArchive->ReadXform(*xf);
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WriteTransform(ON_BinaryArchive* pArchive, const ON_Xform* xf)
{
  bool rc = false;
  if( pArchive && xf )
    rc = pArchive->WriteXform(*xf);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadPlane(ON_BinaryArchive* pArchive, ON_PLANE_STRUCT* plane)
{
  bool rc = false;
  if( pArchive && plane )
  {
    ON_Plane _plane;
    rc = pArchive->ReadPlane(_plane);
    CopyToPlaneStruct(*plane, _plane);
  }
  return rc;
}
RH_C_FUNCTION bool ON_BinaryArchive_WritePlane(ON_BinaryArchive* pArchive, const ON_PLANE_STRUCT* plane)
{
  bool rc = false;
  if( pArchive && plane )
  {
    ON_Plane _plane = FromPlaneStruct(*plane);
    rc = pArchive->WritePlane(_plane);
  }
  return rc;
}

RH_C_FUNCTION ON_Object* ON_BinaryArchive_ReadObject(ON_BinaryArchive* pArchive, int* read_rc)
{
  ON_Object* rc = nullptr;
  if( pArchive && read_rc )
  {
    *read_rc = pArchive->ReadObject(&rc);
  }
  return rc;
}

RH_C_FUNCTION ON_Geometry* ON_BinaryArchive_ReadGeometry(ON_BinaryArchive* pArchive, int* read_rc)
{
  ON_Geometry* rc = nullptr;
  if( pArchive && read_rc )
  {
    ON_Object* pObject = nullptr;
    *read_rc = pArchive->ReadObject(&pObject);
    rc = ON_Geometry::Cast(pObject);
    if(nullptr == rc)
    {
      *read_rc = 0;
      delete pObject;
    }
  }
  return rc;
}


RH_C_FUNCTION ON_MeshParameters* ON_BinaryArchive_ReadMeshParameters(ON_BinaryArchive* pArchive)
{
  ON_MeshParameters* rc = nullptr;
  if( pArchive )
  {
    rc = new ON_MeshParameters();
    if( !rc->Read(*pArchive) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_WriteMeshParameters(ON_BinaryArchive* pArchive, const ON_MeshParameters* pConstMeshParameters)
{
  bool rc = false;
  if( pArchive && pConstMeshParameters )
  {
    rc = pConstMeshParameters->Write(*pArchive);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_WriteGeometry(ON_BinaryArchive* pArchive, const ON_Geometry* pConstGeometry)
{
  bool rc = false;
  if( pArchive && pConstGeometry )
  {
    // 13 March 2013 (S. Baer) RH-16957
    // The geometry reader was using ReadObject, so we need to use the
    // WriteObject function instead of the ON_Geometry::Write function
    rc = pArchive->WriteObject(pConstGeometry);
    //rc = pConstGeometry->Write(*pArchive) ? true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_WriteOn3dmRenderSettings(ON_BinaryArchive* archivePointer, const ON_3dmRenderSettings* valueConstPointer)
{
  if (archivePointer == nullptr || valueConstPointer == nullptr)
    return false;
  bool success = valueConstPointer->Write(*archivePointer) ? true:false;
  return success;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadOn3dmRenderSettings(ON_BinaryArchive* archivePointer, ON_3dmRenderSettings* valuePointer)
{
  if (archivePointer == nullptr || valuePointer == nullptr)
    return false;
  bool success = valuePointer->Read(*archivePointer) ? true:false;
  return success;
}

// Only provided for backwards compatibility
RH_C_FUNCTION bool ON_BinaryArchive_ReadOnCheckSum(ON_BinaryArchive* archivePointer)
{
  if (archivePointer == nullptr)
    return false;
  ON_CheckSum check_sum;
  bool success = check_sum.Read(*archivePointer);
  return success;
}

// Only provided for backwards compatibility
RH_C_FUNCTION bool ON_BinaryArchive_WriteEmptyCheckSum(ON_BinaryArchive* archivePointer)
{
  if (archivePointer == nullptr)
    return false;
  ON_CheckSum check_sum;
  bool success = check_sum.Write(*archivePointer);
  return success;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadObjRef(ON_BinaryArchive* pArchive, ON_ObjRef* pObjRef)
{
  bool rc = false;
  if( pArchive && pObjRef )
    rc = pObjRef->Read(*pArchive);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_WriteObjRef(ON_BinaryArchive* pArchive, const ON_ObjRef* pConstObjRef)
{
  bool rc = false;
  if( pArchive && pConstObjRef )
    rc = pConstObjRef->Write(*pArchive);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_ReadObjRefArray(ON_BinaryArchive* pArchive, ON_ClassArray<ON_ObjRef>* pObjRefArray)
{
  bool rc = false;
  if( pArchive && pObjRefArray )
    rc = pArchive->ReadArray(*pObjRefArray);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_WriteObjRefArray(ON_BinaryArchive* pArchive, const ON_ClassArray<ON_ObjRef>* pConstObjRefArray)
{
  bool rc = false;
  if( pArchive && pConstObjRefArray )
    rc = pArchive->WriteArray(*pConstObjRefArray);
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginReadDictionary(ON_BinaryArchive* pArchive, ON_UUID* dictionary_id, unsigned int* version, CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;
  if( pArchive && dictionary_id && version && pStringHolder )
  {
    ON_wString name;
    rc = pArchive->BeginReadDictionary(dictionary_id, version, name);
    if( rc )
      pStringHolder->Set(name);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EndReadDictionary(ON_BinaryArchive* pArchive)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->EndReadDictionary();
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginWriteDictionary(ON_BinaryArchive* pArchive, ON_UUID dictionary_id, unsigned int version, const RHMONO_STRING* name )
{
  bool rc = false;
  if( pArchive && name )
  {
    INPUTSTRINGCOERCE(_name, name);
    rc = pArchive->BeginWriteDictionary(dictionary_id, version, _name);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EndWriteDictionary(ON_BinaryArchive* pArchive)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->EndWriteDictionary();
  return rc;
}

RH_C_FUNCTION int ON_BinaryArchive_BeginReadDictionaryEntry(ON_BinaryArchive* pArchive, int* de_type, CRhCmnStringHolder* pStringHolder)
{
  int rc = 0;
  if( pArchive && de_type && pStringHolder )
  {
    ON_wString name;
    rc = pArchive->BeginReadDictionaryEntry(de_type, name);
    pStringHolder->Set(name);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EndReadDictionaryEntry(ON_BinaryArchive* pArchive)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->EndReadDictionaryEntry();
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_BeginWriteDictionaryEntry(ON_BinaryArchive* pArchive, int de_type, const RHMONO_STRING* entry_name)
{
  bool rc = false;
  if( pArchive && entry_name )
  {
    INPUTSTRINGCOERCE(_entry_name, entry_name);
    rc = pArchive->BeginWriteDictionaryEntry(de_type, _entry_name);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BinaryArchive_EndWriteDictionaryEntry(ON_BinaryArchive* pArchive)
{
  bool rc = false;
  if( pArchive )
    rc = pArchive->EndWriteDictionaryEntry();
  return rc;
}

RH_C_FUNCTION ON_Object* ON_ReadBufferArchive(int archive_3dm_version, unsigned int archive_on_version, int length, /*ARRAY*/const unsigned char* buffer)
{
  // Eliminate potential bogus file versions written
  if (archive_3dm_version > 5 && archive_3dm_version < 50)
    return nullptr;
  ON_Object* rc = nullptr;
  if( length>0 && buffer )
  {
    ON_Read3dmBufferArchive archive(length, buffer, false, archive_3dm_version, archive_on_version);
    archive.ReadObject( &rc );
  }
  return rc;
}

RH_C_FUNCTION ON_Read3dmBufferArchive* ON_ReadBufferArchiveFromStream(int archive_3dm_version, unsigned int archive_on_version, int length, /*ARRAY*/const unsigned char* buffer)
{
  // Eliminate potential bogus file versions written
  if (archive_3dm_version > 5 && archive_3dm_version < 50)
    return nullptr;
  return new ON_Read3dmBufferArchive(length, buffer, false, archive_3dm_version, archive_on_version);
}

RH_C_FUNCTION void ON_ReadBufferArchive_Delete(ON_Read3dmBufferArchive* pReadBufferArchive)
{
  if (pReadBufferArchive)
    delete pReadBufferArchive;
}

RH_C_FUNCTION ON_Write3dmBufferArchive* ON_WriteBufferArchive_NewWriter(const ON_Object* pConstObject, int* rhinoversion, bool writeuserdata, unsigned int* length)
{
  ON_Write3dmBufferArchive* rc = nullptr;

  if( pConstObject && length && nullptr != rhinoversion)
  {
    ON_UserDataHolder holder;
    if( !writeuserdata )
      holder.MoveUserDataFrom(*pConstObject);
    *length = 0;
    size_t sz = pConstObject->SizeOf() + 512; // 256 was too small on x86 builds to account for extra data written

    // 23 Oct 2019 - If .NET user passes a huge value, just clamp it and continue.
    const int current_3dm_version = ON_BinaryArchive::CurrentArchiveVersion();
    if (*rhinoversion > current_3dm_version)
      *rhinoversion = current_3dm_version;

    if (*rhinoversion < 70 && nullptr != ON_SubD::Cast(pConstObject))
    {
      // SubD objects require at least a version 7 3dm archive.
      *rhinoversion = 70;
    }

    // figure out the appropriate version number
    unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(*rhinoversion, ON::Version());

    rc = new ON_Write3dmBufferArchive(sz, 0, *rhinoversion, on_version__to_write);
    if( rc->WriteObject(pConstObject) )
    {
      *length = (unsigned int)rc->SizeOfArchive();
    }
    else
    {
      delete rc;
      rc = nullptr;
    }
    if( !writeuserdata )
      holder.MoveUserDataTo(*pConstObject, false);
  }
  return rc;
}

RH_C_FUNCTION ON_Write3dmBufferArchive* ON_WriteBufferArchive_NewMemoryWriter(int rhinoversion)
{
  // figure out the appropriate version number
  unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(rhinoversion, ON::Version());

  ON_Write3dmBufferArchive* rc = new ON_Write3dmBufferArchive(0, 0, rhinoversion, on_version__to_write);
  return rc;
}

RH_C_FUNCTION unsigned int ON_WriteBufferArchive_SizeOfArchive(const ON_Write3dmBufferArchive* pWrite3dmBufferArchive)
{
  if (pWrite3dmBufferArchive)
    return (unsigned int)pWrite3dmBufferArchive->SizeOfArchive();
  return 0;
}

RH_C_FUNCTION unsigned int ON_WriteBufferArchive_OpenNURBSVersion(const ON_Write3dmBufferArchive* archive)
{
  if (archive)
    return archive->ArchiveOpenNURBSVersion();
  return 0;
}

RH_C_FUNCTION void ON_WriteBufferArchive_Delete(ON_BinaryArchive* pBinaryArchive)
{
  if( pBinaryArchive )
    delete pBinaryArchive;
}

RH_C_FUNCTION unsigned char* ON_WriteBufferArchive_Buffer(const ON_Write3dmBufferArchive* pBinaryArchive)
{
  unsigned char* rc = nullptr;
  if( pBinaryArchive )
  {
    rc = (unsigned char*)pBinaryArchive->Buffer();
  }
  return rc;
}

RH_C_FUNCTION ON_BinaryArchiveBuffer* ON_BinaryArchiveBuffer_NewSwapArchive()
{
  ON_BinaryArchiveBuffer* pBinaryArchive = new ON_BinaryArchiveBuffer(ON::archive_mode::readwrite, new ON_Buffer());
  pBinaryArchive->SetArchive3dmVersion(ON_BinaryArchive::CurrentArchiveVersion());

  return pBinaryArchive;
}

RH_C_FUNCTION void ON_BinaryArchiveBuffer_DeleteSwapArchive(ON_BinaryArchiveBuffer* pBinaryArchive)
{
  if (pBinaryArchive)
  {
    delete pBinaryArchive->Buffer();
    delete pBinaryArchive;
  }
}

/////////////////////////////////
// move to on_extensions.cpp

RH_C_FUNCTION ONX_Model* ONX_Model_New()
{
  return new ONX_Model();
}

RH_C_FUNCTION void ONX_Model_ReadNotes(const RHMONO_STRING* path, CRhCmnStringHolder* pString)
{
  if( path && pString )
  {
    INPUTSTRINGCOERCE(_path, path);

    FILE* fp = ON::OpenFile( _path, L"rb" );
    if( fp )
    {
      ON_BinaryFile file( ON::archive_mode::read3dm, fp);
      int version = 0;
      ON_String comments;
      bool rc = file.Read3dmStartSection( &version, comments );
      if(rc)
      {
        ON_3dmProperties prop;
        file.Read3dmProperties(prop);
        if( prop.m_Notes.IsValid() )
        {
          pString->Set( prop.m_Notes.m_notes );
        }
      }
      ON::CloseFile(fp);
    }
  }
}

RH_C_FUNCTION int ONX_Model_ReadArchiveVersion(const RHMONO_STRING* path)
{
  if( path )
  {
    INPUTSTRINGCOERCE(_path, path);
    
    FILE* fp = ON::OpenFile( _path, L"rb" );
    if( fp )
    {
      ON_BinaryFile file( ON::archive_mode::read3dm, fp);
      int version = 0;
      ON_String comment_block;
      bool rc = file.Read3dmStartSection( &version, comment_block );
      if(rc)
      {
        ON::CloseFile(fp);
        return version;
      }
      ON::CloseFile(fp);
    }
  }
  
  return 0;
}

RH_C_FUNCTION bool ONX_Model_AddEmbeddedFile(ONX_Model* model, const RHMONO_STRING* filename)
{
  if ((nullptr == model) || (nullptr == filename))
    return false;

  INPUTSTRINGCOERCE(_filename, filename);

  ON_EmbeddedFile ef;
  if (!ef.LoadFromFile(_filename))
    return false;

  model->AddModelComponent(ef);

  return true;
}

// note: This object must be deleted with ON_3dmSettings_Delete
RH_C_FUNCTION ON_3dmSettings* ONX_Model_ReadSettings(const RHMONO_STRING* path)
{
  ON_3dmSettings* rc = nullptr;
  if (path)
  {
    INPUTSTRINGCOERCE(_path, path);

    FILE* fp = ON::OpenFile(_path, L"rb");
    if (fp)
    {
      ON_BinaryFile file(ON::archive_mode::read3dm, fp);
      int version = 0;
      ON_String comments;

      if (file.Read3dmStartSection(&version, comments))
      {
        ON_3dmProperties prop;
        if (file.Read3dmProperties(prop))
        {
          ON_3dmSettings settings;
          file.Read3dmSettings(settings);

          rc = new ON_3dmSettings(settings);
        }
      }
      ON::CloseFile(fp);
    }
  }
  return rc;
}

RH_C_FUNCTION ON_EarthAnchorPoint* ONX_Model_GetEarthAnchorPoint(const ONX_Model* pConstModel)
{
  ON_EarthAnchorPoint* rc = nullptr;
  if (pConstModel)
    rc = new ON_EarthAnchorPoint(pConstModel->m_settings.m_earth_anchor_point);
  return rc;
}

RH_C_FUNCTION void ONX_Model_SetEarthAnchorPoint(ONX_Model* pModel, const ON_EarthAnchorPoint* earthAnchorPt)
{
  if (nullptr != pModel && nullptr != earthAnchorPt)
      pModel->m_settings.m_earth_anchor_point = *earthAnchorPt;
}

RH_C_FUNCTION ON_3dmRevisionHistory* ONX_Model_ReadRevisionHistory(const RHMONO_STRING* path, CRhCmnStringHolder* pStringCreated, CRhCmnStringHolder* pStringLastEdited, int* revision)
{
  ON_3dmRevisionHistory* rc = nullptr;
  if( path && pStringCreated && pStringLastEdited && revision )
  {
    INPUTSTRINGCOERCE(_path, path);

    FILE* fp = ON::OpenFile( _path, L"rb" );
    if( fp )
    {
      ON_BinaryFile file( ON::archive_mode::read3dm, fp);
      int version = 0;
      ON_String comments;
      
      if(file.Read3dmStartSection( &version, comments ))
      {
        ON_3dmProperties prop;
        file.Read3dmProperties(prop);

        rc = new ON_3dmRevisionHistory(prop.m_RevisionHistory);
        pStringCreated->Set(prop.m_RevisionHistory.m_sCreatedBy);
        pStringLastEdited->Set(prop.m_RevisionHistory.m_sLastEditedBy);
        *revision = prop.m_RevisionHistory.m_revision_count;
      }
      ON::CloseFile(fp);
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_3dmRevisionHistory_GetDate(const ON_3dmRevisionHistory* pConstRevisionHistory, bool created, int* seconds, int* minutes,
                                                 int* hours, int* days, int* months, int* years)
{
  bool rc = false;
  if( pConstRevisionHistory && seconds && minutes && hours && days && months && years )
  {
    rc = created ? pConstRevisionHistory->CreateTimeIsSet() : pConstRevisionHistory->LastEditedTimeIsSet();
    if( rc )
    {
      tm revdate = created ? pConstRevisionHistory->m_create_time : pConstRevisionHistory->m_last_edit_time;
      *seconds = revdate.tm_sec;
      *minutes = revdate.tm_min;
      *hours = revdate.tm_hour;
      *days = revdate.tm_mday;
      *months = revdate.tm_mon;
      *years = revdate.tm_year + 1900;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_3dmRevisionHistory* ONX_Model_RevisionHistory(ONX_Model* pModel)
{
  if( pModel )
    return &(pModel->m_properties.m_RevisionHistory);
  return nullptr;
}

RH_C_FUNCTION void ON_3dmRevisionHistory_Delete(ON_3dmRevisionHistory* pRevisionHistory)
{
  if( pRevisionHistory )
    delete pRevisionHistory;
}

RH_C_FUNCTION void ONX_Model_ReadApplicationDetails(const RHMONO_STRING* path, CRhCmnStringHolder* pApplicationName, CRhCmnStringHolder* pApplicationUrl, CRhCmnStringHolder* pApplicationDetails)
{
  if( path && pApplicationName && pApplicationUrl && pApplicationDetails )
  {
    INPUTSTRINGCOERCE(_path, path);

    FILE* fp = ON::OpenFile( _path, L"rb" );
    if( fp )
    {
      ON_BinaryFile file( ON::archive_mode::read3dm, fp);
      int version = 0;
      ON_String comments;
      bool rc = file.Read3dmStartSection( &version, comments );
      if(rc)
      {
        ON_3dmProperties prop;
        file.Read3dmProperties(prop);
        pApplicationName->Set(prop.m_Application.m_application_name);
        pApplicationUrl->Set(prop.m_Application.m_application_URL);
        pApplicationDetails->Set(prop.m_Application.m_application_details);
      }
      ON::CloseFile(fp);
    }
  }
}

RH_C_FUNCTION ONX_Model* ONX_Model_ReadFile(const RHMONO_STRING* path, CRhCmnStringHolder* pStringHolder)
{
  ONX_Model* rc = nullptr;
  if( path )
  {
    INPUTSTRINGCOERCE(_path, path);
    rc = new ONX_Model();
    ON_wString s;
    ON_TextLog log(s);
    ON_TextLog* pLog = pStringHolder ? &log : nullptr;
    if( !rc->Read(_path, pLog) )
    {
      delete rc;
      rc = nullptr;
    }
    if( pStringHolder )
      pStringHolder->Set(s);
  }
  return rc;
}

RH_C_FUNCTION bool ONX_Model_WriteOneObject(const RHMONO_STRING* pPath, const ON_Geometry* pGeometry)
{
  bool rc = false;
  if (pPath && pGeometry)
  {
    INPUTSTRINGCOERCE(path, pPath);
    FILE* fp = ON::OpenFile(path, L"wb");
    if (fp)
    {
      ON_BinaryFile out_file(ON::archive_mode::write3dm, fp);
      rc = ON_WriteOneObjectArchive(out_file, 0, *pGeometry);
      ON::CloseFile(fp);
    }
  }
  return rc;
}

RH_C_FUNCTION bool ONX_Model_WriteMultipleObjects(const RHMONO_STRING* pPath, const ON_SimpleArray<const ON_Object*>* pObjects)
{
  bool rc = false;
  if (pPath && pObjects)
  {
    INPUTSTRINGCOERCE(path, pPath);
    FILE* fp = ON::OpenFile(path, L"wb");
    if (fp)
    {
      ON_BinaryFile out_file(ON::archive_mode::write3dm, fp);
      rc = ON_WriteMultipleObjectArchive(out_file, 0, *pObjects);
      ON::CloseFile(fp);
    }
  }
  return rc;
}


RH_C_FUNCTION ONX_Model* ONX_Model_FromByteArray(int length, /*ARRAY*/ const unsigned char* buffer)
{
  //ON_Read3dmBufferArchive archive(length, buffer, false, ON_BinaryArchive::CurrentArchiveVersion(), ON::Version());
  // 20 Aug 2020 S. Baer
  // Setting the archive version and on version on the buffer class is just plain weird. It also
  // causes the reader to fail. This is a bug we should eventually fix, but for now 0, 0 will work
  // just fine
  ON_Read3dmBufferArchive archive(length, buffer, false, 0, 0);

  ONX_Model* model = new ONX_Model();
  if (!model->Read(archive)) {
    delete model;
    return nullptr;
  }
  return model;
}


enum ReadFileTableTypeFilter : int
{
  ttfNone = 0,
  ttfPropertiesTable          = 0x000001,
  ttfSettingsTable            = 0x000002,
  ttfBitmapTable              = 0x000004,
  ttfTextureMappingTable      = 0x000008,
  ttfMaterialTable            = 0x000010,
  ttfLinetypeTable            = 0x000020,
  ttfLayerTable               = 0x000040,
  ttfGroupTable               = 0x000080,
  ttfFontTable                = 0x000100,
  ttfFutureFontTable          = 0x000200,
  ttfDimstyleTable            = 0x000400,
  ttfLightTable               = 0x000800,
  ttfHatchpatternTable        = 0x001000,
  ttfInstanceDefinitionTable  = 0x002000,
  ttfObjectTable              = 0x004000, 
  ttfHistoryrecordTable       = 0x008000,
  ttfUserTable                = 0x010000
};

enum ObjectTypeFilter : unsigned int
{
  otNone  =          0,
  otPoint         =          1, // some type of ON_Point
  otPointset      =          2, // some type of ON_PointCloud, ON_PointGrid, ...
  otCurve         =          4, // some type of ON_Curve like ON_LineCurve, ON_NurbsCurve, etc.
  otSurface       =          8, // some type of ON_Surface like ON_PlaneSurface, ON_NurbsSurface, etc.
  otBrep          =       0x10, // some type of ON_Brep
  otMesh          =       0x20, // some type of ON_Mesh
  otAnnotation    =      0x200, // some type of ON_Annotation
  otInstanceDefinition  =      0x800, // some type of ON_InstanceDefinition
  otInstanceReference   =     0x1000, // some type of ON_InstanceRef
  otTextDot             =     0x2000, // some type of ON_TextDot
  otDetail        =     0x8000, // some type of ON_DetailView
  otHatch         =    0x10000, // some type of ON_Hatch
  otExtrusion     = 0x40000000, // some type of ON_Extrusion
  otAny           = 0xFFFFFFFF
};

RH_C_FUNCTION ONX_Model* ONX_Model_ReadFile2(const RHMONO_STRING* path, ReadFileTableTypeFilter tableFilter, ObjectTypeFilter objectTypeFilter, CRhCmnStringHolder* pStringHolder)
{
  ONX_Model* rc = nullptr;
  if( path )
  {
    INPUTSTRINGCOERCE(_path, path);
    rc = new ONX_Model();
    ON_wString s;
    ON_TextLog log(s);
    ON_TextLog* pLog = pStringHolder ? &log : nullptr;
    unsigned int table_filter = (unsigned int)tableFilter;
    unsigned int obj_filter = (unsigned int)objectTypeFilter;
    if( !rc->Read(_path, table_filter, obj_filter, pLog) )
    {
      delete rc;
      rc = nullptr;
    }
    if( pStringHolder )
      pStringHolder->Set(s);
  }
  return rc;
}


RH_C_FUNCTION bool ONX_Model_ToByteArray(const ONX_Model* model, ON_Write3dmBufferArchive* archive,
  int version,
  unsigned int enableRenderMeshesForThese,
  unsigned int enableAnalysisMeshesForThese,
  bool writeUserData)
{
  if (nullptr == model || nullptr == archive)
    return false;

  archive->SetShouldSerializeUserDataDefault(writeUserData);
  archive->EnableSave3dmRenderMeshes(enableRenderMeshesForThese, true);
  archive->EnableSave3dmRenderMeshes((~enableRenderMeshesForThese), false);

  archive->EnableSave3dmAnalysisMeshes(enableAnalysisMeshesForThese, true);
  archive->EnableSave3dmAnalysisMeshes((~enableAnalysisMeshesForThese), false);

  bool success = model->Write(*archive, version);
  return success;
}


RH_C_FUNCTION bool ONX_Model_WriteFile(ONX_Model* pModel, const RHMONO_STRING* path, int version,
  unsigned int enableRenderMeshesForThese,
  unsigned int enableAnalysisMeshesForThese,
  bool writeUserData,
  CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;
  INPUTSTRINGCOERCE(_path, path);
  if( pModel && _path )
  {
    FILE* fp = ON::OpenFile(_path, L"wb");
    if( 0==fp )
      return false;

    ON_wString s;
    ON_TextLog log(s);
    ON_TextLog* pLog = pStringHolder ? &log : nullptr;

    // Default ON_BinaryFile construction enables ON_Brep and ON_Extrusion mesh saving.
    //
    // ON_SubD mesh saving is disabled by default and for a good reason.
    // It bloats files, makes file IO slower, and ON_SubD meshes can be calculated
    // very quickly by Rhino when the file is read.  The only reason to save an ON_SubD
    // render mesh in a file is if the file will be used some some non-Rhino app
    // that is run on a lame CPU (iPhone 5 and perhaps 6?) that can't do enough 
    // double precision ops to fight its way out of a wet paper bag.
    //
    ON_BinaryFile binary_file(ON::archive_mode::write3dm, fp);
    binary_file.SetArchiveFullPath(_path);

    binary_file.EnableSave3dmRenderMeshes(enableRenderMeshesForThese, true);
    binary_file.EnableSave3dmRenderMeshes((~enableRenderMeshesForThese), false);

    binary_file.EnableSave3dmAnalysisMeshes(enableAnalysisMeshesForThese, true);
    binary_file.EnableSave3dmAnalysisMeshes((~enableAnalysisMeshesForThese), false);

    binary_file.SetShouldSerializeUserDataDefault(writeUserData);

    rc = pModel->Write(binary_file, version, pLog);
    ON::CloseFile(fp);

    if (pStringHolder)
      pStringHolder->Set(s);
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_Delete(ONX_Model* pModel)
{
  if( pModel )
    delete pModel;
}

RH_C_FUNCTION void ONX_Model_GetStartSectionComments(const ONX_Model* pConstModel, CRhCmnStringHolder* pString)
{
  if( pConstModel && pString )
    pString->Set(pConstModel->m_sStartSectionComments);
}

RH_C_FUNCTION void ONX_Model_SetStartSectionComments(ONX_Model* pModel, const RHMONO_STRING* comments)
{
  if( pModel )
  {
    INPUTSTRINGCOERCE(_comments, comments);
    ON_String ssc(_comments);
    pModel->m_sStartSectionComments = ssc;
  }
}

RH_C_FUNCTION int ONX_Model_GetArchiveVersion(const ONX_Model* pConstModel)
{
  int rc = 0;
  if (pConstModel)
    rc = pConstModel->m_3dm_file_version;
  return rc;
}

RH_C_FUNCTION void ONX_Model_GetNotes(const ONX_Model* pConstModel, CRhCmnStringHolder* pString, bool* visible, bool* html, int* left, int* top, int* right, int* bottom)
{
  if( pConstModel && visible && html && left && top && right && bottom )
  {
    const ON_3dmNotes& notes = pConstModel->m_properties.m_Notes;
    if( pString )
      pString->Set(notes.m_notes);
    *visible = notes.m_bVisible?true:false;
    *html = notes.m_bHTML?true:false;
    *left = notes.m_window_left;
    *top = notes.m_window_top;
    *right = notes.m_window_right;
    *bottom = notes.m_window_bottom;
  }
}

RH_C_FUNCTION void ONX_Model_SetNotesString(ONX_Model* pModel, const RHMONO_STRING* notes)
{
  if( pModel )
  {
    INPUTSTRINGCOERCE(_notes, notes);
    pModel->m_properties.m_Notes.m_notes = _notes;
  }
}

RH_C_FUNCTION void ONX_Model_SetNotes(ONX_Model* pModel, bool visible, bool html, int left, int top, int right, int bottom)
{
  if( pModel )
  {
    pModel->m_properties.m_Notes.m_bHTML = html?1:0;
    pModel->m_properties.m_Notes.m_bVisible = visible?1:0;
    pModel->m_properties.m_Notes.m_window_left = left;
    pModel->m_properties.m_Notes.m_window_top = top;
    pModel->m_properties.m_Notes.m_window_right = right;
    pModel->m_properties.m_Notes.m_window_bottom = bottom;
  }
}

RH_C_FUNCTION int ONX_Model_ExtraTableCount(const ONX_Model* pConstModel, int which)
{
  const int idxUserDataTable = 15;
  const int idxViewTable = 16;
  const int idxNamedViewTable = 17;

  int rc = 0;
  if( pConstModel )
  {
    switch(which)
    {
    case idxUserDataTable:
      rc = pConstModel->m_userdata_table.Count();
      break;
    case idxViewTable:
      rc = pConstModel->m_settings.m_views.Count();
      break;
    case idxNamedViewTable:
      rc = pConstModel->m_settings.m_named_views.Count();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_Dump(const ONX_Model* pConstModel, int which, CRhCmnStringHolder* pStringHolder)
{
  const int idxDumpAll = 0;
  const int idxDumpSummary = 1;
  const int idxUserDataTable = 15;

  if (pConstModel && pStringHolder)
  {
    ON_wString s;
    ON_TextLog log(s);
    ON_ModelComponent::Type component_type = ON_ModelComponent::Type::Unset;
    switch (which)
    {
    case idxDumpAll:
      pConstModel->Dump(log);
      break;
    case idxDumpSummary:
      pConstModel->DumpSummary(log);
      break;
    case idxUserDataTable:
      pConstModel->DumpUserDataTable(log);
      break;
    default:
      break;
    }
    if (ON_ModelComponent::Type::Unset != component_type)
      pConstModel->DumpComponentList(component_type, log);
    pStringHolder->Set(s);
  }
}

RH_C_FUNCTION void ONX_Model_Dump3(const ONX_Model* pConstModel, ON_ModelComponent::Type type, CRhCmnStringHolder* pStringHolder)
{
  if (pConstModel && pStringHolder && ON_ModelComponent::ComponentTypeIsValidAndNotMixed(type))
  {
    ON_wString s;
    ON_TextLog log(s);

    pConstModel->DumpComponentList(type, log);
    pStringHolder->Set(s);
  }
}



RH_C_FUNCTION void ONX_Model_Dump2(const ONX_Model* pConstModel, ON_TextLog* pTextLog)
{
  if( pConstModel && pTextLog )
    pConstModel->Dump(*pTextLog);
}

RH_C_FUNCTION const ON_Geometry* ONX_Model_ModelObjectGeometry(const ONX_Model* pConstModel, ON_UUID id)
{
  const ON_Geometry* rc = nullptr;
  if( pConstModel )
  {
    ON_ModelComponent::Type type = ON_ModelComponent::Type::ModelGeometry;
    ON_ModelComponentReference ref = pConstModel->ComponentFromId(type, id);

    if (ref.IsEmpty())
    {
      ON_ModelComponent::Type type = ON_ModelComponent::Type::RenderLight; //lights are special cased
      ref = pConstModel->ComponentFromId(type, id);
    }

    const ON_ModelGeometryComponent* component_ptr = ON_ModelGeometryComponent::FromModelComponentRef(ref, &ON_ModelGeometryComponent::Unset);
    if (component_ptr)
    {
      rc = component_ptr->Geometry(nullptr);
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_3dmObjectAttributes* ONX_Model_ModelObjectAttributes(const ONX_Model* pConstModel, ON_UUID id)
{
  const ON_3dmObjectAttributes* rc = nullptr;
  if( pConstModel)
  {
    ON_ModelComponent::Type type = ON_ModelComponent::Type::ModelGeometry;
    ON_ModelComponentReference ref = pConstModel->ComponentFromId(type, id);

    if (ref.IsEmpty())
    {
      ON_ModelComponent::Type type = ON_ModelComponent::Type::RenderLight; //lights are special cased
      ref = pConstModel->ComponentFromId(type, id);
    }

    const ON_ModelGeometryComponent* component_ptr = ON_ModelGeometryComponent::FromModelComponentRef(ref, &ON_ModelGeometryComponent::Unset);
    if (component_ptr)
    {
      rc = component_ptr->Attributes(nullptr);
    }
  }
  return rc;
}

RH_C_FUNCTION ON_UUID ONX_Model_ModelObjectGeometryFindFromIndex(const ONX_Model* pConstModel, int index)
{
  ON_UUID rc = ON_nil_uuid;
  if (pConstModel)
  {
    ON_ModelComponent::Type type = ON_ModelComponent::Type::ModelGeometry;
    ON_ModelComponentReference ref = pConstModel->ComponentFromIndex(type, index);

    if (ref.IsEmpty())
    {
      ON_ModelComponent::Type type = ON_ModelComponent::Type::RenderLight; //lights are special cased
      ref = pConstModel->ComponentFromIndex(type, index);
    }

    const ON_ModelGeometryComponent* component_ptr = ON_ModelGeometryComponent::FromModelComponentRef(ref, &ON_ModelGeometryComponent::Unset);

    rc = component_ptr->Id();
  }
  return rc;
}

RH_C_FUNCTION const ON_ModelGeometryComponent* ONX_Model_ModelObjectGeometryConstPtrFromId(const ONX_Model* pConstModel, ON_UUID id)
{
  const ON_ModelGeometryComponent* rc = nullptr;
  if (pConstModel)
  {
    ON_ModelComponent::Type type = ON_ModelComponent::Type::ModelGeometry;
    ON_ModelComponentReference ref = pConstModel->ComponentFromId(type, id);

    if (ref.IsEmpty())
    {
      ON_ModelComponent::Type type = ON_ModelComponent::Type::RenderLight; //lights are special cased
      ref = pConstModel->ComponentFromId(type, id);
    }

    rc = ON_ModelGeometryComponent::FromModelComponentRef(ref, &ON_ModelGeometryComponent::Unset);
  }
  return rc;
}

RH_C_FUNCTION int ONX_Model_ObjectTable_LayerIndexFromId(const ONX_Model* pConstModel, ON_UUID objectId)
{
  int rc = -1;
  if (pConstModel && ON_UuidIsNotNil(objectId))
  {
    ON_ModelComponentReference ref = pConstModel->LayerFromId(objectId);
    if (!ref.IsEmpty())
      rc = ref.ModelComponent()->Index(rc);
  }
  return rc;
}

static ON_UUID Internal_ONX_Model_AddModelGeometry(
  ONX_Model* model,
  const ON_Geometry* geometry,
  const ON_3dmObjectAttributes* attributes
  )
{
  if ( nullptr == model )
    return ON_nil_uuid;
  if ( nullptr == geometry )
    return ON_nil_uuid;
  ON_ModelComponentReference model_component_reference = model->AddModelGeometryComponent(geometry,attributes);
  return ON_ModelGeometryComponent::FromModelComponentRef(model_component_reference, &ON_ModelGeometryComponent::Unset)->Id();
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddPoint(ONX_Model* pModel, ON_3DPOINT_STRUCT point, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel )
  {
    ON_Point point_geometry(point.val[0], point.val[1], point.val[2]);
    return Internal_ONX_Model_AddModelGeometry(pModel,&point_geometry,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddPointCloud(ONX_Model* pModel, int count, /*ARRAY*/const ON_3dPoint* points, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && count>0 && points )
  {
    ON_PointCloud point_cloud_geometry;
    point_cloud_geometry.m_P.Append(count, points);
    return Internal_ONX_Model_AddModelGeometry(pModel,&point_cloud_geometry,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddPointCloud2(ONX_Model* pModel, const ON_PointCloud* pConstPointCloud, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && pConstPointCloud )
  {
    ON_PointCloud point_cloud_geometry(*pConstPointCloud);
    ON_ModelComponentReference model_component_reference = pModel->AddModelGeometryComponent(&point_cloud_geometry,pConstAttributes);
    return ON_ModelGeometryComponent::FromModelComponentRef(model_component_reference, &ON_ModelGeometryComponent::Unset)->Id();
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddClippingPlane(ONX_Model* pModel, const ON_PLANE_STRUCT* plane, double du, double dv, int count, /*ARRAY*/const ON_UUID* clippedViewportIds, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && plane && du>0.0 && dv>0.0 && count>0 && clippedViewportIds )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    if( temp.IsValid() )
    {
      ON_Interval domain0( 0.0, du );
      ON_Interval domain1( 0.0, dv );

      ON_PlaneSurface srf( temp );
      srf.SetExtents( 0, domain0, true );
      srf.SetExtents( 1, domain1, true );
      srf.SetDomain( 0, domain0.Min(), domain0.Max() );
      srf.SetDomain( 1, domain1.Min(), domain1.Max() );
      if( srf.IsValid() )
      {
        ON_ClippingPlaneSurface cps(srf);
        for( int i=0; i<count; i++ )
          cps.m_clipping_plane.m_viewport_ids.AddUuid(clippedViewportIds[i]);
        return Internal_ONX_Model_AddModelGeometry(pModel,&cps,pConstAttributes);
      }
    }
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddLinearDimension( ONX_Model* pModel, const ON_DimLinear* pConstDimension, const ON_3dmObjectAttributes* pConstAttributes )
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstDimension,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddAngularDimension(ONX_Model* pModel, const ON_DimAngular* pConstDimension, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel, pConstDimension, pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddOrdinateDimension(ONX_Model* pModel, const ON_DimOrdinate* pConstDimension, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel, pConstDimension, pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddRadialDimension(ONX_Model* pModel, const ON_DimRadial* pConstDimension, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel, pConstDimension, pConstAttributes);
}


RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddLine( ONX_Model* pModel, ON_3DPOINT_STRUCT pt0, ON_3DPOINT_STRUCT pt1, const ON_3dmObjectAttributes* pConstAttributes )
{
  if( pModel )
  {
    ON_3dPoint _pt0(pt0.val);
    ON_3dPoint _pt1(pt1.val);
    ON_LineCurve line_curve(_pt0, _pt1);
    return Internal_ONX_Model_AddModelGeometry(pModel,&line_curve,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddPolyline( ONX_Model* pModel, int count, /*ARRAY*/const ON_3dPoint* points, const ON_3dmObjectAttributes* pConstAttributes )
{
  if( pModel && count>0 && points )
  {
    ON_PolylineCurve C;
    C.m_pline.Append(count, points);
    return Internal_ONX_Model_AddModelGeometry(pModel,&C,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddArc(ONX_Model* pModel, ON_Arc* pArc, const ON_3dmObjectAttributes* pConstAttributes )
{
  if( pModel && pArc )
  {
    ON_Arc arc(*pArc);
    arc.plane.UpdateEquation();
    ON_ArcCurve C(arc);
    return Internal_ONX_Model_AddModelGeometry(pModel,&C,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddCircle(ONX_Model* pModel, const ON_CIRCLE_STRUCT* pCircle, const ON_3dmObjectAttributes* pConstAttributes )
{
  if( pModel && pCircle )
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    ON_ArcCurve C(circle);
    return Internal_ONX_Model_AddModelGeometry(pModel,&C,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddEllipse(ONX_Model* pModel, ON_Ellipse* pEllipse, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && pEllipse )
  {
    ON_Ellipse ellipse(*pEllipse);
    ellipse.plane.UpdateEquation();
    ON_NurbsCurve nc;
    if(2 == ellipse.GetNurbForm(nc) )
      return Internal_ONX_Model_AddModelGeometry(pModel,&nc,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddSphere(ONX_Model* pModel, ON_Sphere* pSphere, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && pSphere )
  {
    // make sure the plane equation is in-sync for this sphere
    ON_Sphere sphere(*pSphere);
    sphere.plane.UpdateEquation();
    ON_RevSurface S;
    if( sphere.RevSurfaceForm(false,&S) )
      return Internal_ONX_Model_AddModelGeometry(pModel,&S,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddCurve(ONX_Model* pModel, const ON_Curve* pConstCurve, const ON_3dmObjectAttributes* pConstAttributes)
{
  ON_UUID id = ::ON_nil_uuid;
  if( pModel && pConstCurve )
  {
    ON_Curve* pCurve = pConstCurve->DuplicateCurve();
    id = Internal_ONX_Model_AddModelGeometry(pModel,pCurve,pConstAttributes);
    delete pCurve;
  }
  return id;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddTextDot(ONX_Model* pModel, const ON_TextDot* pConstDot, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstDot,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddInstanceRef(ONX_Model* pModel, const ON_InstanceRef* pConstInstanceRef, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel, pConstInstanceRef, pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddInstanceRef2(ONX_Model* pModel, int idefIndex, const ON_Xform* pConstXform, const ON_3dmObjectAttributes* pConstAttributes)
{
  ON_UUID rc = ON_nil_uuid;
  if (nullptr != pModel && nullptr != pConstXform && pConstXform->IsValid())
  {
    const ON_ModelComponentReference& idef_component_ref = pModel->ComponentFromIndex(ON_ModelComponent::Type::InstanceDefinition, idefIndex);
    if (!idef_component_ref.IsEmpty())
    {
      const ON_InstanceDefinition* idef = ON_InstanceDefinition::Cast(idef_component_ref.ModelComponent());
      if (nullptr != idef)
      {
        ON_InstanceRef iref;
        iref.m_instance_definition_uuid = idef->Id();
        iref.m_xform = *pConstXform;
        // Internal_ONX_Model_AddModelGeometry makes a copy
        rc = Internal_ONX_Model_AddModelGeometry(pModel, &iref, pConstAttributes);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION int ONX_Model_File3dmInstanceDefinitionTable_Add(
  ONX_Model* pModel, 
  const RHMONO_STRING* name, 
  const RHMONO_STRING* description, 
  const RHMONO_STRING* url,
  const RHMONO_STRING* url_tag,
  ON_3DPOINT_STRUCT base_point,
  ON_SimpleArray<const ON_Geometry*>* pGeometry, 
  ON_SimpleArray<const ON_3dmObjectAttributes*>* pAttributes
)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_name, name);
  INPUTSTRINGCOERCE(_description, description);
  INPUTSTRINGCOERCE(_url, url);
  INPUTSTRINGCOERCE(_url_tag, url_tag);
  if (pModel && pGeometry)
  {
    // Determine if we need to transform geometry to world origin
    ON_3dPoint pt(base_point.val);
    ON_Xform xf;
    ON_Xform* pXform = nullptr;
    if (pt.IsValid() && pt != ON_3dPoint::Origin)
    {
      xf = ON_Xform::TranslationTransformation(ON_3dPoint::Origin - pt);
      pXform = &xf;
    }

    // Default attributes, in case attributes are omitted
    const int attr_count = pAttributes ? pAttributes->Count() : 0;

    ON_SimpleArray<ON_UUID> object_uuids;
    for (int i = 0; i < pGeometry->Count(); i++)
    {
      const ON_Geometry* pConstGeom = (*pGeometry)[i];
      const ON_3dmObjectAttributes* pConstAtts = i < attr_count ? (*pAttributes)[i] : &ON_3dmObjectAttributes::DefaultAttributes;
      if (pConstGeom && pConstAtts)
      {
        ON_Geometry* pGeom = pConstGeom->Duplicate(); // Copy so we can transform
        if (pGeom)
        {
          // Make certain that proper flags are set for instance definiton geometry
          ON_3dmObjectAttributes atts(*pConstAtts);
          atts.m_uuid = ON_nil_uuid;
          atts.SetMode(ON::object_mode::idef_object);
          atts.RemoveFromAllGroups();
          atts.m_space = ON::model_space;
          atts.m_viewport_id = ON_nil_uuid;

          // Transform if needed
          if (pXform)
          {
            atts.Transform(pGeom, *pXform);
            pGeom->Transform(*pXform);
          }

          ON_UUID uuid = Internal_ONX_Model_AddModelGeometry(pModel, pGeom, &atts);
          if (ON_UuidIsNotNil(uuid))
            object_uuids.Append(uuid);

          delete pGeom; // Don't leak...
        }
      }
    }

    if (object_uuids.Count())
    {
      ON_InstanceDefinition* idef = new ON_InstanceDefinition();
      if (nullptr != idef)
      {
        idef->SetInstanceGeometryIdList(object_uuids);
        idef->SetInstanceDefinitionType(ON_InstanceDefinition::IDEF_UPDATE_TYPE::Static);
        idef->SetName(_name);
        idef->SetDescription(_description);
        idef->SetURL(_url);
        idef->SetURL_Tag(_url_tag);
        ON_ModelComponentReference model_component_reference = pModel->AddManagedModelComponent(idef, true);
        if (!model_component_reference.IsEmpty())
        {
          const ON_ModelComponent* model_component = model_component_reference.ModelComponent();
          if (nullptr != model_component)
            rc = model_component->Index();
        }
      }
    }
  }

  return rc;
}

RH_C_FUNCTION int ONX_Model_File3dmInstanceDefinitionTable_AddLinked(
  ONX_Model* pModel,
  const RHMONO_STRING* filename,
  const RHMONO_STRING* name,
  const RHMONO_STRING* description
  )
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-49968
  int rc = -1;
  if (pModel && filename)
  {
    INPUTSTRINGCOERCE(_filename, filename);
    INPUTSTRINGCOERCE(_name, name);
    INPUTSTRINGCOERCE(_description, description);
    
    ON_InstanceDefinition* idef = new ON_InstanceDefinition();
    if (nullptr != idef)
    {
      if (!idef->SetLinkedFileReference(ON_InstanceDefinition::IDEF_UPDATE_TYPE::Linked, _filename))
      {
        delete idef;
        return -1;
      }
      idef->SetName(_name);
      idef->SetDescription(_description);
      ON_ModelComponentReference model_component_reference = pModel->AddManagedModelComponent(idef, true);
      if (!model_component_reference.IsEmpty())
      {
        const ON_ModelComponent* model_component = model_component_reference.ModelComponent();
        if (nullptr != model_component)
          rc = model_component->Index();
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddText(
  ONX_Model* pModel,
  const RHMONO_STRING* _text,
  const ON_PLANE_STRUCT* plane,
  double height, 
  const RHMONO_STRING* _fontName, 
  int fontStyle,
  int V5_justification_bits, 
  const ON_3dmObjectAttributes* pConstAttributes
)
{
  // Dale Lear remarks: 
  //   Why doesn't this function pass a dim style id for the font?
  //   If you insist on passing enough parameters to choke a very large and healthy horse, then you need to add
  //   model_view_text_scale and replace obsolete V5_justification_bits with
  //   ON::TextHorizontalAlignment halign and ON::TextVerticalAlignment valign parameters.
  //   _fontName needs some serious thought as well. Is it a FamilyName, PostScriptName(), LOGFONT.lfFaceName, ...?
  
  // These should be paramters to ONX_Model_ObjectTable_AddText().
  // V5_justification_bits should be removed.
  const ON::TextHorizontalAlignment halign = ON::TextHorizontalAlignmentFromV5Justification(V5_justification_bits);
  const ON::TextVerticalAlignment valign = ON::TextVerticalAlignmentFromV5Justification(V5_justification_bits);
  const double model_view_text_scale = ON_UNSET_VALUE;

  INPUTSTRINGCOERCE(text, _text);
  INPUTSTRINGCOERCE(fontName, _fontName);
  if( pModel && plane && text && text[0]!=0 )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    if( temp.IsValid() )
    {
      if( height <= 0.0 )
        height = 1.0;
      bool bBold = (0!=(fontStyle&1));
      bool bItalic = (0!=(fontStyle&2));
      bool bUnderlined = false;
      bool bStrikethrough = false;

      ON_wString font_str( fontName );
      font_str.TrimLeftAndRight();
      if( font_str.IsEmpty() )
        font_str = ON_Font::Default.WindowsLogfontName();
      ON_Font font_characteristics;
      font_characteristics.SetFontCharacteristics(font_str,bBold,bItalic,bUnderlined,bStrikethrough);

      const ON_ModelComponentReference dimstyle_with_font = pModel->DimensionStyleWithFontCharacteristics(font_characteristics, model_view_text_scale );
      const ON_DimStyle* dim_style = ON_DimStyle::Cast(dimstyle_with_font.ModelComponent());

      ON_Text text_object;
      text_object.Create(text, dim_style, temp);

      if (height > 0.0 && height != text_object.TextHeight(dim_style))
        text_object.SetTextHeight(dim_style, height);

      if (halign != text_object.TextHorizontalAlignment(dim_style))
        text_object.SetTextHorizontalAlignment(dim_style, halign);

      if (valign != text_object.TextVerticalAlignment(dim_style))
        text_object.SetTextVerticalAlignment(dim_style, valign);
      
      return Internal_ONX_Model_AddModelGeometry(pModel,&text_object,pConstAttributes);
    }
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddSurface(ONX_Model* pModel, const ON_Surface* pConstSurface, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstSurface,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddExtrusion(ONX_Model* pModel, const ON_Extrusion* pConstExtrusion, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstExtrusion,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddMesh(ONX_Model* pModel, const ON_Mesh* pConstMesh, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstMesh,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddBrep(ONX_Model* pModel, const ON_Brep* pConstBrep, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstBrep,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddLeader(ONX_Model* pModel, const RHMONO_STRING* _text, const ON_PLANE_STRUCT* plane, int count, /*ARRAY*/const ON_2dPoint* points2d, const ON_3dmObjectAttributes* pConstAttributes)
{
  INPUTSTRINGCOERCE(text, _text);
  
  if( pModel && plane && count>1 && points2d )
  {
    ON_Leader leader;

    ON_Plane temp = FromPlaneStruct(*plane);
    const bool bWrapped = false;
    const double rect_width = 0.0;
    
    ON_SimpleArray<ON_3dPoint> points(count);
    for (int i = 0; i < count; i++)
    {
      ON_2dPoint p = points2d[i];
      if (p.IsValid())
        points.Append(temp.PointAt(p.x, p.y));
    }

    if (leader.Create(
      text,
      ON_DimStyle::Cast(pModel->CurrentDimensionStyle().ModelComponent()),
      points.Count(),
      points.Array(),
      temp,
      bWrapped,
      rect_width
    ))
    {
      return Internal_ONX_Model_AddModelGeometry(pModel, &leader, pConstAttributes);
    }
  }

  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddHatch(ONX_Model* pModel, const ON_Hatch* pConstHatch, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel,pConstHatch,pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddSubD(ONX_Model* pModel, const ON_SubD* pConstSubD, const ON_3dmObjectAttributes* pConstAttributes)
{
  return Internal_ONX_Model_AddModelGeometry(pModel, pConstSubD, pConstAttributes);
}

RH_C_FUNCTION ON_UUID ONX_Model_ObjectTable_AddPolyLine(ONX_Model* pModel, int count, /*ARRAY*/const ON_3dPoint* points, const ON_3dmObjectAttributes* pConstAttributes)
{
  if( pModel && points && count>1)
  {
    CHack3dPointArray pl(count, (ON_3dPoint*)points);

    ON_PolylineCurve C(pl);
   return Internal_ONX_Model_AddModelGeometry(pModel,&C,pConstAttributes);
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION bool ONX_Model_ObjectTable_Delete(ONX_Model* pModel, ON_UUID object_id)
{
  bool rc = false;
  if( pModel )
  {
    ON_ModelComponentReference model_component = pModel->RemoveModelComponent(ON_ModelComponent::Type::ModelGeometry,object_id);
    rc = (false == model_component.IsEmpty());

    if (!rc) //lights are special cased.
    {
      ON_ModelComponentReference model_component = pModel->RemoveModelComponent(ON_ModelComponent::Type::RenderLight, object_id);
      rc = (false == model_component.IsEmpty());
    }
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_BoundingBox(const ONX_Model* pConstModel, ON_BoundingBox* pBBox)
{
  if( pConstModel && pBBox )
  {
    *pBBox = pConstModel->ModelGeometryBoundingBox();
  }
}

static const ON_ModelComponent* Internal_ModelComponentFromId(
  const ONX_Model* model,
  ON_ModelComponent::Type component_type,
  ON_UUID component_id
  )
{
  if (nullptr == model)
    return nullptr;
  return model->ComponentFromId(component_type, component_id).ModelComponent();
}

static int Internal_ModelComponentIndexFromId(
  const ONX_Model* model,
  ON_ModelComponent::Type component_type,
  ON_UUID component_id
  )
{
  const ON_ModelComponent* model_component = Internal_ModelComponentFromId(model,component_type,component_id);
  return
    ( nullptr == model_component )
    ? ON_UNSET_INT_INDEX
    : model_component->Index();
}

static const ON_ModelComponent* Internal_ModelComponentFromIndex(
  const ONX_Model* model,
  ON_ModelComponent::Type component_type,
  int component_index
  )
{
  if ( nullptr == model )
    return nullptr;
  return model->ComponentFromIndex(component_type, component_index).ModelComponent();
}

RH_C_FUNCTION const ON_Linetype* ONX_Model_GetLinetypePointer(ONX_Model* pModel, ON_UUID id)
{
  const ON_Linetype* p = ON_Linetype::Cast(Internal_ModelComponentFromId(pModel,ON_ModelComponent::Type::LinePattern,id));
  return p;
}

RH_C_FUNCTION const ON_Bitmap* ONX_Model_GetBitmapPointer(ONX_Model* pModel, int index)
{
  const ON_Bitmap* p = ON_Bitmap::Cast(Internal_ModelComponentFromIndex(pModel,ON_ModelComponent::Type::Image,index));
  return p;
}

RH_C_FUNCTION const ON_Bitmap* ONX_Model_GetBitmapPointerFromId(ONX_Model* pModel, ON_UUID id)
{
  const ON_Bitmap* p = ON_Bitmap::Cast(Internal_ModelComponentFromId(pModel,ON_ModelComponent::Type::Image,id));
  return p;
}

RH_C_FUNCTION const ON_ModelComponent* ONX_Model_GetModelComponentPointer(const ONX_Model* pModel, ON_UUID id)
{
  const ON_ModelComponent* rc = nullptr;
  if (pModel)
  {
    ON_ComponentManifestItem item = pModel->Manifest().ItemFromId(id);
    if (!item.IsUnset())
    {
      ON_ModelComponentReference ref = pModel->ComponentFromId(item.ComponentType(), id);
      rc = ref.ModelComponent();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ONX_Model_AddModelComponent(ONX_Model* pModel, const ON_ModelComponent* pConstModelComponent)
{
  bool rc = false;
  if (pModel && pConstModelComponent)
  {
    rc = !pModel->AddModelComponent(*pConstModelComponent, true).IsEmpty();
  }
  return rc;
}

static ON_UUID Internal_ModelComponentIdFromIndex(
  const ONX_Model* model,
  ON_ModelComponent::Type component_type,
  int index
  )
{
  if ( nullptr == model )
    return ON_nil_uuid;
  return model->Manifest().ItemFromIndex(component_type,index).Id();
}

RH_C_FUNCTION bool ONX_Model_RemoveModelComponent_Id(
  ONX_Model* model,
  ON_ModelComponent::Type component_type,
  ON_UUID id
  )
{
  if ( nullptr == model )
    return false;
  return !model->RemoveModelComponent(component_type,id).IsEmpty();
}

//includes deleted objects
RH_C_FUNCTION unsigned int ON_ComponentManifest_ActiveAndDeletedComponentCount(const ON_ComponentManifest* manifest, ON_ModelComponent::Type type)
{
  unsigned int rc = 0;
  if (manifest)
  {
    rc = manifest->ActiveAndDeletedComponentCount(type);
  }

  return rc;
}

RH_C_FUNCTION bool ON_ComponentManifest_ItemFromComponentRuntimeSerialNumber(const ON_ComponentManifest* manifest, ON__UINT64 number, ON_ModelComponent::Type* type, ON_UUID* id, int* index)
{
  bool rc = false;
  // 29-Jul-2016 Dale Fugier, added null checks
  if (nullptr != manifest)
  {
    const ON_ComponentManifestItem item = manifest->ItemFromComponentRuntimeSerialNumber(number);
    if (nullptr != type)
      *type = item.ComponentType();
    if (nullptr != id)
      *id = item.Id();
    if (nullptr != index)
      *index = item.Index();
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION ONX_ModelComponentIterator* ONX_ModelComponentIterator_New(const ONX_Model* model, ON_ModelComponent::Type type)
{
  ONX_ModelComponentIterator* rc = nullptr;

  if (nullptr != model &&
    ON_ModelComponent::ComponentTypeIsValidAndNotMixed(type)
    )
  {
    rc = new ONX_ModelComponentIterator(*model, type);
  }

  return rc;
}

RH_C_FUNCTION ON_ModelComponent* ONX_ModelComponentIterator_GetNext(ONX_ModelComponentIterator* modelIterator, int* outIndex, ON_ModelComponent::Type* outType, ON_UUID* outId)
{
  ON_ModelComponent* rc = nullptr;
  // 29-Jul-2016 Dale Fugier, added null checks
  if (nullptr != modelIterator)
  {
    rc = const_cast<ON_ModelComponent*>(modelIterator->NextComponent());
    if (nullptr != rc)
    {
      if (nullptr != outIndex)
        *outIndex = rc->Index();
      if (nullptr != outId)
        *outId = rc->Id();
      if (nullptr != outType)
        *outType = rc->ComponentType();
    }
  }
  return rc;
}

RH_C_FUNCTION void ONX_ModelComponentIterator_Delete(ONX_ModelComponentIterator* modelIterator)
{
  // 29-Jul-2016 Dale Fugier, added null checks
  if (nullptr != modelIterator)
    delete modelIterator;
}


RH_C_FUNCTION const ON_ModelComponent* ONX_Model_AnyTable_FindId(
  const ONX_Model* const_model,
  ON_ModelComponent::Type* type,
  ON_UUID id,
  int* index
  )
{
  const ON_ModelComponent* rc = nullptr;

  if (nullptr == type || nullptr == index)
    rc = nullptr;
  else if (nullptr == const_model)
    rc = nullptr;
  else if (!(ON_ModelComponent::ComponentTypeIsValid(*type) ||
    ON_ModelComponent::Type::Unset == *type))
    rc = nullptr;
  else if (ON_UuidIsNil(id))
    rc = nullptr;
  else
  {
    const ON_ComponentManifestItem mi = const_model->Manifest().ItemFromId(id);
    if (mi.IsUnset())
      rc = nullptr;
    else
    {
      if (ON_ModelComponent::Type::Unset == *type || ON_ModelComponent::Type::Mixed == *type)
        *type = mi.ComponentType();
      const ON_ModelComponentReference ref = const_model->ComponentFromId(*type, id);

      if (!ref.IsEmpty())
      {
        rc = ref.ModelComponent();
        *index = rc->Index();
      }
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_ModelComponent* ONX_Model_AnyTable_FindIndex(
  const ONX_Model* const_model,
  ON_ModelComponent::Type type,
  int index,
  ON_UUID* outId
)
{
  const ON_ModelComponent* rc = nullptr;

  if (nullptr == const_model)
    rc = nullptr;
  else if (!ON_ModelComponent::ComponentTypeIsValidAndNotMixed(type))
    rc = nullptr;
  else
  {
    const ON_ModelComponentReference ref = const_model->ComponentFromIndex(type, index);

    if (!ref.IsEmpty())
    {
      rc = ref.ModelComponent();
      *outId = ref.ModelComponentId();
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_ModelComponent* ONX_Model_AnyTable_FindName(
  const ONX_Model* const_model,
  ON_ModelComponent::Type type,
  ON_UUID parentId,
  const RHMONO_STRING* name,
  int* outIndex,
  ON_UUID* outId
)
{
  const ON_ModelComponent* rc = nullptr;
  INPUTSTRINGCOERCE(_name, name);

  if (nullptr == const_model)
    rc = nullptr;
  else if (nullptr == _name)
    rc = nullptr;
  else if (!ON_ModelComponent::ComponentTypeIsValidAndNotMixed(type))
    rc = nullptr;
  else
  {
    const ON_ModelComponentReference ref = const_model->ComponentFromName(type, parentId, _name);

    if (!ref.IsEmpty())
    {
      rc = ref.ModelComponent();
      *outIndex = rc->Index();
      *outId = ref.ModelComponentId();
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_ModelComponent* ONX_Model_AnyTable_FindNameHash(
  const ONX_Model* const_model,
  ON_ModelComponent::Type type,
  ON_NameHash* hash,
  int* outIndex,
  ON_UUID* outId
)
{
  const ON_ModelComponent* rc = nullptr;

  if (nullptr == const_model)
    rc = nullptr;
  else if (nullptr == hash)
    rc = nullptr;
  else if (!ON_ModelComponent::ComponentTypeIsValidAndNotMixed(type))
    rc = nullptr;
  else
  {
    const ON_ModelComponentReference ref = const_model->ComponentFromNameHash(type, *hash);

    if (!ref.IsEmpty())
    {
      rc = ref.ModelComponent();
      *outIndex = rc->Index();
      *outId = ref.ModelComponentId();
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_ComponentManifest* ONX_Model_GetConstOnComponentManifestPtr(const ONX_Model* const_model)
{
  const ON_ComponentManifest* rc = nullptr;
  if (const_model) rc = &const_model->Manifest();
  return rc;
}


RH_C_FUNCTION bool ONX_Model_RemoveModelComponent(
  ONX_Model* model,
  ON_ModelComponent::Type component_type,
  int index
  )
{
  if ( nullptr == model )
    return false;
  const ON_ModelComponent* model_component = model->ComponentFromIndex(component_type, index).ModelComponent();
  ON_UUID id 
     = ( nullptr == model_component ) 
     ? Internal_ModelComponentIdFromIndex(model,component_type,index)
     : model_component->Id();
  return ONX_Model_RemoveModelComponent_Id(model, component_type, id);
}


RH_C_FUNCTION ON_UUID ONX_Model_LinetypeTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::LinePattern,index);
}

RH_C_FUNCTION ON_UUID ONX_Model_LayerTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::Layer,index);
}

RH_C_FUNCTION ON_UUID ONX_Model_DimStyleTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::DimStyle,index);
}

RH_C_FUNCTION ON_UUID ONX_Model_HatchPatternTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::HatchPattern,index);
}

RH_C_FUNCTION ON_UUID ONX_Model_InstanceDefinitionTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::InstanceDefinition,index);
}

RH_C_FUNCTION int ONX_Model_InstanceDefinitionTable_Index(const ONX_Model* pConstModel, ON_UUID id)
{
  return Internal_ModelComponentIndexFromId(pConstModel,ON_ModelComponent::Type::InstanceDefinition,id);
}

RH_C_FUNCTION const ON_InstanceDefinition* ONX_Model_GetInstanceDefinitionPointer(const ONX_Model* pConstModel, ON_UUID id)
{
  return ON_InstanceDefinition::Cast( Internal_ModelComponentFromId(pConstModel,ON_ModelComponent::Type::InstanceDefinition,id) );
}

RH_C_FUNCTION ON_UUID ONX_Model_MaterialTable_Id(const ONX_Model* pConstModel, int index)
{
  return Internal_ModelComponentIdFromIndex(pConstModel,ON_ModelComponent::Type::RenderMaterial,index);
}

RH_C_FUNCTION void ONX_Model_GetString( const ONX_Model* pConstModel, int which, CRhCmnStringHolder* pString )
{
  const int idxApplicationName = 0;
  const int idxApplicationUrl = 1;
  const int idxApplicationDetails = 2;
  const int idxCreatedBy = 3;
  const int idxLastCreatedBy = 4;

  if( pConstModel && pString )
  {
    switch(which)
    {
    case idxApplicationName:
      pString->Set( pConstModel->m_properties.m_Application.m_application_name );
      break;
    case idxApplicationUrl:
      pString->Set( pConstModel->m_properties.m_Application.m_application_URL );
      break;
    case idxApplicationDetails:
      pString->Set( pConstModel->m_properties.m_Application.m_application_details );
      break;
    case idxCreatedBy:
      pString->Set( pConstModel->m_properties.m_RevisionHistory.m_sCreatedBy );
      break;
    case idxLastCreatedBy:
      pString->Set( pConstModel->m_properties.m_RevisionHistory.m_sLastEditedBy );
      break;
    }
  }
}

RH_C_FUNCTION void ONX_Model_SetString( ONX_Model* pModel, int which, const RHMONO_STRING* str )
{
  const int idxApplicationName = 0;
  const int idxApplicationUrl = 1;
  const int idxApplicationDetails = 2;
  const int idxCreatedBy = 3;
  const int idxLastCreatedBy = 4;
  INPUTSTRINGCOERCE(_str, str);

  if( pModel )
  {
    switch(which)
    {
    case idxApplicationName:
      pModel->m_properties.m_Application.m_application_name = _str;
      break;
    case idxApplicationUrl:
      pModel->m_properties.m_Application.m_application_URL = _str;
      break;
    case idxApplicationDetails:
      pModel->m_properties.m_Application.m_application_details = _str;
      break;
    case idxCreatedBy:
      pModel->m_properties.m_RevisionHistory.m_sCreatedBy = _str;
      break;
    case idxLastCreatedBy:
      pModel->m_properties.m_RevisionHistory.m_sLastEditedBy = _str;
      break;
    }
  }
}

RH_C_FUNCTION int ONX_Model_GetRevision(const ONX_Model* pConstModel)
{
  int rc = 0;
  if( pConstModel )
    rc = pConstModel->m_properties.m_RevisionHistory.m_revision_count;
  return rc;
}

RH_C_FUNCTION void ONX_Model_SetRevision(ONX_Model* pModel, int rev)
{
  if( pModel )
    pModel->m_properties.m_RevisionHistory.m_revision_count = rev;
}

RH_C_FUNCTION ON_3dmSettings* ONX_Model_3dmSettingsPointer(ONX_Model* pModel)
{
  if (nullptr == pModel)
    return nullptr;

  return &pModel->m_settings;
}

RH_C_FUNCTION ON_3dmView* ONX_Model_ViewPointer(ONX_Model* pModel, ON_UUID id, const ON_3dmView* pConstView, bool named_view_table)
{
  ON_3dmView* rc = nullptr;
  if( pModel )
  {
    ON_ClassArray<ON_3dmView>* views = named_view_table ? &(pModel->m_settings.m_named_views) : &(pModel->m_settings.m_views);
    if( views )
    {
      for( int i=0; i<views->Count(); i++ )
      {
        ON_3dmView* pView = views->At(i);
        if( pView && pView->m_vp.ViewportId() == id )
        {
          if( ::ON_UuidIsNil(id) && pConstView!=pView )
            continue;
          rc = pView;
          break;
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_3dmView* ONX_Model_ViewTable_Pointer(ONX_Model* pModel, int index, bool named_view_table)
{
  ON_3dmView* pView = nullptr;
  if( pModel )
  {
    pView = named_view_table ? pModel->m_settings.m_named_views.At(index) :
                               pModel->m_settings.m_views.At(index);
  }
  return pView;
}

RH_C_FUNCTION ON_UUID ONX_Model_ViewTable_Id(const ONX_Model* pConstModel, int index, bool named_view_table)
{
  if( pConstModel )
  {
    const ON_3dmView* pView = named_view_table ? pConstModel->m_settings.m_named_views.At(index) :
                                                 pConstModel->m_settings.m_views.At(index);
    if( pView )
      return pView->m_vp.ViewportId();
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION void ONX_Model_ViewTable_Clear(ONX_Model* pModel, bool named_view_table)
{
  if( pModel )
  {
    if( named_view_table )
      pModel->m_settings.m_named_views.Empty();
    else
      pModel->m_settings.m_views.Empty();
  }
}

RH_C_FUNCTION int ONX_Model_ViewTable_Index(const ONX_Model* pConstModel, const ON_3dmView* pConstView, bool named_view_table)
{
  int rc = -1;
  if( pConstModel && pConstView )
  {
    const ON_ClassArray<ON_3dmView>* views = named_view_table ? &(pConstModel->m_settings.m_named_views) : &(pConstModel->m_settings.m_views);
    if( views )
    {
      for( int i=0; i<views->Count(); i++ )
      {
        if( views->At(i) == pConstView )
        {
          rc = i;
          break;
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_ViewTable_Add(ONX_Model* pModel, const ON_3dmView* pConstView, bool named_view_table)
{
  if (pModel && pConstView)
  {
    ON_ClassArray<ON_3dmView>* views = named_view_table ? &(pModel->m_settings.m_named_views) : &(pModel->m_settings.m_views);
    if (views)
    {
      views->Append(*pConstView);
    }
  }
}

RH_C_FUNCTION void ONX_Model_ViewTable_Insert(ONX_Model* pModel, const ON_3dmView* pConstView, int index, bool named_view_table)
{
  if( pModel && pConstView && index>=0)
  {
    ON_ClassArray<ON_3dmView>* views = named_view_table ? &(pModel->m_settings.m_named_views) : &(pModel->m_settings.m_views);
    if( views )
    {
      views->Insert(index, *pConstView);
    }
  }
}

RH_C_FUNCTION bool ONX_Model_ViewTable_RemoveAt(ONX_Model* pModel, int index, bool named_view_table)
{
  bool rc = false;
  if( pModel && index>=0)
  {
    ON_ClassArray<ON_3dmView>* views = named_view_table ? &(pModel->m_settings.m_named_views) : &(pModel->m_settings.m_views);
    if (views && index < views->Count())
    {
      views->Remove(index);
      rc = true;
    }
  }
  return rc;
}

// 27-Sep-2017 Dale Fugier https://mcneel.myjetbrains.com/youtrack/issue/RH-41623

RH_C_FUNCTION int ONX_Model_NamedCPlaneTable_Count(const ONX_Model* pModel)
{
  int rc = 0;
  if (nullptr != pModel)
    rc = pModel->m_settings.m_named_cplanes.Count();
  return rc;
}

RH_C_FUNCTION const ON_3dmConstructionPlane* ONX_Model_NamedCPlaneTable_Get(const ONX_Model* pModel, int index)
{
  const ON_3dmConstructionPlane* rc = nullptr;
  if (nullptr != pModel && index >= 0 && index < pModel->m_settings.m_named_cplanes.Count())
    rc = &(pModel->m_settings.m_named_cplanes[index]);
  return rc;
}

RH_C_FUNCTION int ONX_Model_NamedCPlaneTable_Add(ONX_Model* pModel, const RHMONO_STRING* pName, const ON_PLANE_STRUCT* pPlane)
{
  int rc = -1;
  if (nullptr != pModel && nullptr != pName && nullptr != pPlane)
  {
    INPUTSTRINGCOERCE(name, pName);
    rc = pModel->m_settings.m_named_cplanes.Count();
    ON_3dmConstructionPlane& cp = pModel->m_settings.m_named_cplanes.AppendNew();
    cp.m_name = name;
    cp.m_plane = FromPlaneStruct(*pPlane);
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_NamedCPlaneTable_Add2(ONX_Model* pModel, const ON_3dmConstructionPlane* pConstPlane)
{
  if (nullptr != pModel && nullptr != pConstPlane)
    pModel->m_settings.m_named_cplanes.Append(*pConstPlane);
}


RH_C_FUNCTION void ONX_Model_NamedCPlaneTable_Insert(ONX_Model* pModel, const ON_3dmConstructionPlane* pConstPlane, int index)
{
  if (nullptr != pModel && nullptr != pConstPlane)
    pModel->m_settings.m_named_cplanes.Insert(index, *pConstPlane);
}

RH_C_FUNCTION bool ONX_Model_NamedCPlaneTable_Delete(ONX_Model* pModel, int index)
{
  bool rc = false;
  if (nullptr != pModel && index >= 0 && index < pModel->m_settings.m_named_cplanes.Count())
  {
    const int cplane_count = pModel->m_settings.m_named_cplanes.Count();
    if (index >= 0 && index < cplane_count)
    {
      pModel->m_settings.m_named_cplanes.Remove(index);
      rc = cplane_count != pModel->m_settings.m_named_cplanes.Count();
    }
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_NamedCPlaneTable_Clear(ONX_Model* pModel)
{
  if (nullptr != pModel)
    pModel->m_settings.m_named_cplanes.Empty();
}

// 22-Mar-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-38134

RH_C_FUNCTION int ONX_Model_DocumentUserString_Count(ONX_Model* pModel)
{
  int rc = 0;
  if (nullptr != pModel)
  {
    ON_ClassArray<ON_UserString> user_strings;
    rc = pModel->GetDocumentUserStrings(user_strings);
  }
  return rc;
}

RH_C_FUNCTION void ONX_Model_DocumentUserString_GetString(ONX_Model* pModel, int index, bool bKey, CRhCmnStringHolder* pString)
{
  if (nullptr != pModel && nullptr != pString && index >= 0)
  {
    ON_ClassArray<ON_UserString> user_strings;
    pModel->GetDocumentUserStrings(user_strings);
    if (index < user_strings.Count())
    {
      if (bKey)
        pString->Set(user_strings[index].m_key);
      else
        pString->Set(user_strings[index].m_string_value);
    }
  }
}

RH_C_FUNCTION void ONX_Model_DocumentUserString_GetString2(ONX_Model* pModel, const RHMONO_STRING* key, CRhCmnStringHolder* pString)
{
  if (nullptr != pModel && nullptr != key && nullptr != pString)
  {
    INPUTSTRINGCOERCE(_key, key);
    ON_wString value;
    pModel->GetDocumentUserString(_key, value);
    pString->Set(value);
  }
}

RH_C_FUNCTION void ONX_Model_DocumentUserString_SetString(ONX_Model* pModel, const RHMONO_STRING* key, const RHMONO_STRING* value)
{
  if (nullptr != pModel && nullptr != key && nullptr != value)
  {
    INPUTSTRINGCOERCE(_key, key);
    INPUTSTRINGCOERCE(_value, value);
    pModel->SetDocumentUserString(_key, _value);
  }
}

RH_C_FUNCTION void ONX_Model_DocumentUserString_Delete(ONX_Model* pModel, const RHMONO_STRING* key)
{
  if (nullptr != pModel)
  {
    if (NULL == key)
    {
      ON_ClassArray<ON_UserString> user_strings;
      pModel->GetDocumentUserStrings(user_strings);
      for (int i = 0; i< user_strings.Count(); i++)
        pModel->SetDocumentUserString(user_strings[i].m_key, nullptr);
    }
    else
    {
      INPUTSTRINGCOERCE(_key, key);
      pModel->SetDocumentUserString(_key, nullptr);
    }
  }
}


RH_C_FUNCTION ON_UUID ONX_Model_UserDataTable_Uuid(const ONX_Model* pConstModel, int index)
{
  if( pConstModel && index >= 0 && index < pConstModel->m_userdata_table.Count())
  {
    const ONX_Model_UserData* pUD = pConstModel->m_userdata_table[index];
    if( pUD )
      return pUD->m_uuid;
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION ON_Read3dmBufferArchive* ONX_Model_PlugIn_UserData_NewArchive(const ONX_Model* pConstModel, ON_UUID id)
{
  // 27-Aug-2020 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-60129
  ON_Read3dmBufferArchive* rc = nullptr;
  if (pConstModel)
  {
    // Search for plug-in's document user data
    ONX_Model_UserData* model_ud = nullptr;
    for (unsigned int i = 0; i < pConstModel->m_userdata_table.UnsignedCount(); i++)
    {
      ONX_Model_UserData* ptr = pConstModel->m_userdata_table[i];
      if (ptr && ptr->m_uuid == id)
      {
        model_ud = ptr;
        break;
      }
    }

    if (model_ud && model_ud->m_goo.m_value > 0 && model_ud->m_goo.m_goo)
    {
      rc = new ON_Read3dmBufferArchive(
        model_ud->m_goo.m_value,
        (const void*)model_ud->m_goo.m_goo,
        false, // Do not copy the imput buffer
        pConstModel->m_3dm_file_version,
        pConstModel->m_3dm_opennurbs_version
      );
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Read3dmBufferArchive* ONX_Model_ModelComponent_UserData_NewArchive(const ONX_Model* pConstModel, const ON_ModelComponent* pConstModelComponent, ON_UUID userdata_id)
{
  // 9-Sep-2020 Dale Fugier
  ON_Read3dmBufferArchive* rc = nullptr;
  if (pConstModel && pConstModelComponent)
  {
    const ON_UserData* user_data = pConstModelComponent->GetUserData(userdata_id);
    if (user_data && user_data->IsUnknownUserData())
    {
      const ON_UnknownUserData* unknown_data = ON_UnknownUserData::Cast(user_data);
      if (unknown_data && unknown_data->m_sizeof_buffer > 0 && unknown_data->m_buffer)
      {
        rc = new ON_Read3dmBufferArchive(
          unknown_data->m_sizeof_buffer,
          unknown_data->m_buffer,
          false, // Do not copy the imput buffer
          unknown_data->m_3dm_version,
          unknown_data->m_3dm_opennurbs_version_number
        );
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Read3dmBufferArchive* ONX_Model_ModelGeometry_UserData_NewArchive(const ONX_Model* pConstModel, ON_UUID model_geometry_id, ON_UUID userdata_id, bool bFromAttributes)
{
  // 9-Sep-2020 Dale Fugier
  ON_Read3dmBufferArchive* rc = nullptr;

  const ON_ModelGeometryComponent* model_geometry = nullptr;
  if (pConstModel)
  {
    ON_ModelComponentReference model_component_ref = pConstModel->ComponentFromId(ON_ModelComponent::Type::ModelGeometry, model_geometry_id);
    if (model_component_ref.IsEmpty())
      model_component_ref = pConstModel->ComponentFromId(ON_ModelComponent::Type::RenderLight, model_geometry_id);
    model_geometry = ON_ModelGeometryComponent::FromModelComponentRef(model_component_ref, &ON_ModelGeometryComponent::Unset);
  }
  if (nullptr == model_geometry)
    return rc;

  const ON_UserData* user_data = nullptr;
  if (bFromAttributes)
  {
    const ON_3dmObjectAttributes* attributes = model_geometry->Attributes(nullptr);
    if (attributes)
      user_data = attributes->GetUserData(userdata_id);
  }
  else
  {
    const ON_Geometry* geometry = model_geometry->Geometry(nullptr);
    if (geometry)
      user_data = geometry->GetUserData(userdata_id);
  }

  if (user_data && user_data->IsUnknownUserData())
  {
    const ON_UnknownUserData* unknown_data = ON_UnknownUserData::Cast(user_data);
    if (unknown_data && unknown_data->m_sizeof_buffer > 0 && unknown_data->m_buffer)
    {
      rc = new ON_Read3dmBufferArchive(
        unknown_data->m_sizeof_buffer,
        unknown_data->m_buffer,
        false, // Do not copy the imput buffer
        unknown_data->m_3dm_version,
        unknown_data->m_3dm_opennurbs_version_number
      );
    }
  }

  return rc;
}

RH_C_FUNCTION void ONX_Model_UserData_DeleteArchive(ON_Read3dmBufferArchive* pArchive)
{
  if (pArchive)
    delete pArchive;
}

RH_C_FUNCTION void ONX_Model_UserDataTable_Clear(ONX_Model* pModel)
{
  if( pModel )
    pModel->m_userdata_table.Empty();
}

RH_C_FUNCTION int ONX_Model_AddLayer(ONX_Model* pModel, const RHMONO_STRING* layerName, int argb, bool bDefault)
{
  if (nullptr == pModel)
    return ON_UNSET_INT_INDEX;

  INPUTSTRINGCOERCE(_layerName, layerName);
  unsigned int abgr = ARGB_to_ABGR(argb);
  ON_Color c(abgr);
  int index = bDefault
    ? pModel->AddDefaultLayer(_layerName, c)
    : pModel->AddLayer(_layerName, c);
  return index;
}

RH_C_FUNCTION int ONX_Model_AddLayer2(ONX_Model* pModel, const RHMONO_STRING* pLayerName, int argb, ON_UUID parentId)
{
  if (nullptr == pModel || nullptr == pLayerName)
    return ON_UNSET_INT_INDEX;

  INPUTSTRINGCOERCE(_layerName, pLayerName);
  unsigned int abgr = ARGB_to_ABGR(argb);
  ON_Color color(abgr);

  ON_Layer layer;
  layer.ClearId();
  layer.ClearIndex();
  layer.SetName(_layerName);
  layer.SetColor(color);
  layer.SetParentId(parentId);
  layer.SetVisible(true);
  layer.SetLocked(false);

  // Do not automatically resolve conflicts
  ON_ModelComponentReference rc = pModel->AddModelComponent(layer, false);
  const ON_Layer* model_layer = ON_Layer::FromModelComponentRef(rc, nullptr);
  if (model_layer)
    return model_layer->Index();

  return ON_UNSET_INT_INDEX;
}

RH_C_FUNCTION int ONX_Model_AddGroup(ONX_Model* pModel) 
{

  if (nullptr == pModel)
    return ON_UNSET_INT_INDEX;

  ON_Group group;
  ON_ModelComponentReference mr = pModel->AddModelComponent(group);
  const ON_Group* managed_group = ON_Group::FromModelComponentRef(mr, nullptr);
  int group_index = (nullptr != managed_group) ? managed_group->Index() : ON_UNSET_INT_INDEX;
  if ( group_index < 0 )
  {
    ON_ERROR("failed to add group.");
  }
  return group_index;

}

RH_C_FUNCTION int ONX_Model_AddMaterial(ONX_Model* pModel, ON_Material* material) 
{
  ON_ModelComponentReference mr = pModel->AddModelComponent(*material, true);
  const ON_Material* managed_material = ON_Material::FromModelComponentRef(mr, nullptr);
  int material_index = (nullptr != managed_material) ? managed_material->Index() : ON_UNSET_INT_INDEX;
  if ( material_index < 0 )
  {
    ON_ERROR("failed to add group.");
  }
  return material_index;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION bool ONX_Model_GetPreviewImage(const ONX_Model* constModel, CRhinoDib* pRhinoDib)
{
  bool rc = false;
  if (nullptr == constModel || nullptr == pRhinoDib)
    return false;

  BITMAPINFO* pBMI = constModel->m_properties.m_PreviewImage.m_bmi;
  if (pBMI)
  {
    pRhinoDib->SetDib(pBMI, false);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ONX_Model_SetPreviewImage(ONX_Model* pModel, const CRhinoDib* constRhinoDib)
{
  bool rc = false;
  if (nullptr == pModel || nullptr == constRhinoDib)
    return false;

#if defined(ON_RUNTIME_WIN)
  ON_WindowsBitmap* onbmp = constRhinoDib->ToOnWindowsBitmap();
  if (onbmp)
  {
    pModel->m_properties.m_PreviewImage = *onbmp;
    delete onbmp;
    rc = true;
  }
#endif
  return rc;
}
#endif

// 4-28-2021 Dale Fugier, this work on Window, with our without Rhino
#if defined(ON_RUNTIME_WIN)
RH_C_FUNCTION HBITMAP ONX_Model_WinReadPreviewImage(const RHMONO_STRING* path)
{
  HBITMAP rc = nullptr;
  INPUTSTRINGCOERCE(_path, path);
  FILE* fp = ON::OpenFile(_path, L"rb");
  if (fp)
  {
    ON_BinaryFile file(ON::archive_mode::read3dm, fp);
    int version = 0;
    ON_String comments;
    if (file.Read3dmStartSection(&version, comments))
    {
      ON_3dmProperties props;
      if (file.Read3dmProperties(props))
      {
        // 1-Mar-2021 Dale Fugier,
        // This non-CRhinoDib code is also used by the Windows
        // Shell extension that displays thumbnails in Explorer.
        if (props.m_PreviewImage.IsValid())
        {
          HDC hdc = ::GetDC(0);
          rc = ::CreateDIBitmap(
            hdc,                                      // handle to DC
            &props.m_PreviewImage.m_bmi->bmiHeader,   // bitmap data
            CBM_INIT,                                 // initialization option
            (const void*)props.m_PreviewImage.m_bits, // initialization data
            props.m_PreviewImage.m_bmi,               // color-format data
            DIB_RGB_COLORS                            // color-data usage
          );
          ::ReleaseDC(0, hdc);
        }
      }
    }
    ON::CloseFile(fp);
  }
  return rc;
}
#endif // if defined(ON_RUNTIME_WIN)

// 4-28-2021 Dale Fugier, this work on Mac, only with Rhino
// When librhino3dm_native included the AppKit framework we
// can revisit this.
#if !defined(RHINO3DM_BUILD)
#if defined(ON_RUNTIME_APPLE_MACOS)
RH_C_FUNCTION NSImage* ONX_Model_MacReadPreviewImage(const RHMONO_STRING* path)
{
  INPUTSTRINGCOERCE(_path, path);
  int width = 128, height = 128;

  // Use OpenNURBS to extract the bitmap first so we can get the preview image size
  FILE* fp = ON::OpenFile(_path, L"rb");
  if (fp)
  {
    ON_BinaryFile file(ON::archive_mode::read3dm, fp);
    int version = 0;
    ON_String comments;
    if (file.Read3dmStartSection(&version, comments))
    {
      ON_3dmProperties props;
      if (file.Read3dmProperties(props))
      {
        width = props.m_PreviewImage.Width();
        height = props.m_PreviewImage.Height();
      }
    }
    ON::CloseFile(fp);
  }

  // This will try to get the preview image as a NSImage regardless of the
  // success of the OpenNURBS attempt above.  It will default to trying
  // to get a image 128x128

  // It can take some time to generate a thumbnail, so generate it on a background thread
  NSSize maxThumbnailSize = { static_cast<CGFloat>(width), static_cast<CGFloat>(height) };
  NSString* ns_path = [NSString stringWithLPCTSTR : _path];
  NSImage* thumbnailImage = [NSImage imageWithPreviewOfFileAtPath : ns_path ofSize : maxThumbnailSize asIcon : NO];
  return thumbnailImage;
}
#endif // #if defined(ON_RUNTIME_APPLE_MACOS)
#endif


class CBinaryFileHelper : public ON_BinaryFile
{
public:
  CBinaryFileHelper(ON::archive_mode mode, FILE* fp)
  : ON_BinaryFile(mode, fp)
  {
    m_file_pointer = fp;
  }

  FILE* m_file_pointer;
};

RH_C_FUNCTION ON_BinaryFile* ON_BinaryFile_Open(const RHMONO_STRING* path, int mode)
{
  // 22-Jan-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-23765

  ON::archive_mode archive_mode = ON::ArchiveMode(mode);
  if (archive_mode == ON::archive_mode::unset_archive_mode)
    return nullptr;

  INPUTSTRINGCOERCE(_path, path);

  FILE* fp;
  if( archive_mode == ON::archive_mode::read || archive_mode == ON::archive_mode::read3dm )
    fp = ON::OpenFile( _path, L"rb" );
  else if( archive_mode == ON::archive_mode::write || archive_mode == ON::archive_mode::write3dm )
    fp = ON::OpenFile( _path, L"wb" );
  else
    fp = ON::OpenFile( _path, L"r+b" );
  
  if( fp )
    return new CBinaryFileHelper( archive_mode, fp );

  return nullptr;
}

RH_C_FUNCTION void ON_BinaryFile_Close(ON_BinaryFile* pBinaryFile)
{
  CBinaryFileHelper* bf = dynamic_cast<CBinaryFileHelper*>(pBinaryFile);
  if( bf )
  {
    if( bf->m_file_pointer )
      ON::CloseFile(bf->m_file_pointer);
    delete bf;
  }
}


RH_C_FUNCTION ON_WindowsBitmap* ON_WindowsBitmap_Get(const ONX_Model* pConstModel)
{
  ON_WindowsBitmap* rc = nullptr;
  if (nullptr != pConstModel)
  {
    // Make sure there is a preview image
    if (!pConstModel->m_properties.m_PreviewImage.IsEmpty())
      rc = new ON_WindowsBitmap(pConstModel->m_properties.m_PreviewImage);
  }
  return rc;
}

RH_C_FUNCTION void ON_WindowsBitmap_SizeAndColorDepth(const ON_WindowsBitmap* pBitmap, int* width, int* height, int* color_depth)
{
  if (pBitmap && width && height && color_depth)
  {
    *width = pBitmap->Width();
    *height = pBitmap->Height();
    *color_depth = (pBitmap->m_bmi) ? pBitmap->m_bmi->bmiHeader.biBitCount : 0;
  }
}

// 16-Aug-2018 Dale Fugier. The commented code (below) is fast. But Rhino3dmIO isn't
// build with the "Allow unsafe code" option. So this block of code causes a build error.
// If somebody complains about this being slow, we can revisit this.
//RH_C_FUNCTION void ON_WindowsBitmap_CopyBytes(const ON_WindowsBitmap* pBitmap, void* systemBitmapBytes, int lineLength, int bitsPerPixel)
//{
//  const bool y_down = lineLength > 0;
//  if (lineLength < 0)
//    lineLength = -lineLength;
//  
//  if (pBitmap == nullptr || systemBitmapBytes == nullptr || lineLength < bitsPerPixel || bitsPerPixel < 3)
//    return;
//  
//  unsigned char* dib_bits = pBitmap->m_bits;
//  if (dib_bits == nullptr)
//    return;
//  
//  const int width = pBitmap->Width();
//  const int height = pBitmap->Height();
//
//  const int bit_count = pBitmap->m_bmi->bmiHeader.biBitCount;
//  const int scan_width = (((bit_count * width)+31) / 32 * 4);
//
//  const int bits_per_pixel = bit_count > 24 ? 4 : 3;
//  for (int y = 0; y < height; y++)
//  {
//    unsigned char* source = dib_bits + (y * scan_width);
//    unsigned char* destination = (unsigned char*)systemBitmapBytes + ((y_down ? (height - (y + 1)) : y) * lineLength);
//    for (int x = 0; x < width; x++)
//    {
//      destination[x * bitsPerPixel] = source[x * bits_per_pixel];
//      destination[(x * bitsPerPixel) + 1] = source[(x * bits_per_pixel) + 1];
//      destination[(x * bitsPerPixel) + 2] = source[(x * bits_per_pixel) + 2];
//      if (bits_per_pixel == bitsPerPixel && bits_per_pixel > 3)
//        destination[(x * bitsPerPixel) + 3] = source[(x * bits_per_pixel) + 3];
//    }
//  }
//}

RH_C_FUNCTION int ON_WindowsBitmap_GetPixel(const ON_WindowsBitmap* pBitmap, int x, int y)
{
  ON_Color rc = ON_UNSET_COLOR;
  if (pBitmap)
    rc = pBitmap->Pixel(x, y);
  unsigned int abgr = (unsigned int)rc;
  return (int)abgr;
}

RH_C_FUNCTION void ON_WindowsBitmap_Delete(ON_WindowsBitmap* pBitmap)
{
  if (nullptr != pBitmap)
    delete pBitmap;
}

