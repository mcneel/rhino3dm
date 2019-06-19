#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_file_utilities.h")



// ON_FileReference

RH_C_FUNCTION ON_FileReference* ON_FileReference_New(const ON_FileReference* constPtr)
{
  if (constPtr) return new ON_FileReference(*constPtr);
  return new ON_FileReference();
}

RH_C_FUNCTION void ON_FileReference_Delete(ON_FileReference* reference)
{
  delete reference;
}

RH_C_FUNCTION ON_FileReference* ON_FileReference_New2(
  const RHMONO_STRING* fullPath,
  const RHMONO_STRING* relativePath,
  const ON_ContentHash* constHash,
  const ON_FileReference::Status fullPathStatus)
{
  ON_FileReference* rc = nullptr;
  if (constHash)
  {
    INPUTSTRINGCOERCE(full_path, fullPath);
    INPUTSTRINGCOERCE(relative_path, relativePath);
    rc = new ON_FileReference(
      full_path,
      relative_path,
      *constHash,
      fullPathStatus);
  }
  return rc;
}

RH_C_FUNCTION ON_FileReference* ON_FileReference_CreateFromFullAndRelativePaths(
  const RHMONO_STRING* fullPath, const RHMONO_STRING* relativePath)
{
  INPUTSTRINGCOERCE(full_path, fullPath);
  INPUTSTRINGCOERCE(relative_path, relativePath);

  ON_FileReference* rc = nullptr;

  if (full_path)
  {
    ON_FileReference temp;
    temp.SetFullPath(full_path, true);

    if (relative_path) temp.SetRelativePath(relative_path);

    rc = new ON_FileReference(temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_FileReference_GetFullOrRelativePath(
  const ON_FileReference* const_reference_ptr, CRhCmnStringHolder* non_const_holder, bool relative)
{
  bool rc = false;
  if (const_reference_ptr && non_const_holder)
  {
    if (relative)
      non_const_holder->Set(const_reference_ptr->RelativePath());
    else
      non_const_holder->Set(const_reference_ptr->FullPath());
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION const ON_ContentHash* ON_FileReference_GetContentHash(const ON_FileReference* const_reference_ptr)
{
  const ON_ContentHash* rc = nullptr;
  if (const_reference_ptr)
  {
    const ON_ContentHash& hashref = const_reference_ptr->ContentHash();
    rc = &hashref;
  }
  return rc;
}

RH_C_FUNCTION ON_FileReference::Status ON_FileReference_GetFullPathStatus(const ON_FileReference* const_reference_ptr)
{
  ON_FileReference::Status rc = ON_FileReference::Status::Unknown;
  if (const_reference_ptr)
  {
    rc = const_reference_ptr->FullPathStatus();
  }
  return rc;
}

RH_C_FUNCTION bool ON_FileReference_GetIsSet(const ON_FileReference* const_reference_ptr)
{
  bool rc = false;
  if (const_reference_ptr)
  {
    rc = const_reference_ptr->IsSet();
  }
  return rc;
}


// ON_SHA1_Hash

static const ON_SHA1_Hash* ON_SHA1_Hash_From_Array_Reinterpret(const unsigned char* hash)
{
  // digest hash bytes must be 20, or behavior is undefined.
  const ON_SHA1_Hash* digest = reinterpret_cast<const ON_SHA1_Hash*>(hash);
  return digest;
}

// digest hash bytes must be 20, or behavior is undefined.
RH_C_FUNCTION void ON_SHA1_Hash_Copy_From_Array(ON_SHA1_Hash* sha1, const unsigned char* hash)
{
  const ON_SHA1_Hash* digest = ON_SHA1_Hash_From_Array_Reinterpret(hash);
  *sha1 = *digest;
}

// digest hash bytes must be 20, or behavior is undefined.
RH_C_FUNCTION void ON_SHA1_Hash_Copy_To_Array(const ON_SHA1_Hash* const_sha1, unsigned char* hash)
{
  ON_SHA1_Hash* digest = const_cast<ON_SHA1_Hash*>(ON_SHA1_Hash_From_Array_Reinterpret(hash));
  *digest = *const_sha1;
}


// ON_SHA1

RH_C_FUNCTION ON_SHA1* ON_SHA1_New(ON_SHA1* hash)
{
  if (hash)
    return new ON_SHA1(*hash);
  return new ON_SHA1();
}

RH_C_FUNCTION void ON_SHA1_Delete(ON_SHA1* hash)
{
  delete hash;
}

RH_C_FUNCTION void ON_SHA1_Reset(ON_SHA1* sha1)
{
  sha1->Reset();
}

RH_C_FUNCTION void ON_SHA1_Update(ON_SHA1* sha1, const void* buffer, ON__UINT64 length)
{
  sha1->AccumulateBytes(buffer, length);
}

RH_C_FUNCTION void ON_SHA1_GetHash(ON_SHA1* sha1, /*ARRAY*/unsigned char* array)
{
  ON_SHA1_Hash hash = sha1->Hash();
  ON_SHA1_Hash_Copy_To_Array(&hash, array);
}

RH_C_FUNCTION void ON_SHA1_FileSystemPathHash(const RHMONO_STRING* path, /*ARRAY*/unsigned char* array, bool bIgnoreCase)
{
  INPUTSTRINGCOERCE(_path, path);

  ON_SHA1_Hash hash = ON_SHA1_Hash::FileSystemPathHash(_path, bIgnoreCase);
  ON_SHA1_Hash_Copy_To_Array(&hash, array);
}



// ON_FileSystemPath

RH_C_FUNCTION bool ON_FileSystemPath_PlatformPathIgnoreCase()
{
  return ON_FileSystemPath::PlatformPathIgnoreCase();
}


// ON_ContentHash

RH_C_FUNCTION ON_ContentHash* ON_ContentHash_Create(
  /*ARRAY*/const unsigned char* sha1_name_hash_20, //length must be 20 or behavior is undefined
  ON__UINT64 byte_count,
  /*ARRAY*/const unsigned char* sha1_content_hash_20, //length must be 20 or behavior is undefined
  ON__UINT64 hash_time,
  ON__UINT64 content_last_modified_time)
{
  if (sha1_name_hash_20 == nullptr || sha1_content_hash_20 == nullptr)
    return nullptr;

  ON_ContentHash h = ON_ContentHash::Create(
    *ON_SHA1_Hash_From_Array_Reinterpret(sha1_name_hash_20),
    byte_count,
    *ON_SHA1_Hash_From_Array_Reinterpret(sha1_content_hash_20),
    hash_time,
    content_last_modified_time
    );

  ON_ContentHash* on_heap = new ON_ContentHash(h);
  return on_heap;
}

RH_C_FUNCTION bool ON_ContentHash_Read(
  const ON_ContentHash* to_be_read_content_hash,
  /*ARRAY*/unsigned char* sha1_name_hash_20, //length must be 20 or behavior is undefined
  ON__UINT64* byte_count,
  /*ARRAY*/unsigned char* sha1_content_hash_20, //length must be 20 or behavior is undefined
  ON__UINT64* hash_time,
  ON__UINT64* content_last_modified_time)
{
  bool rc = false;

  if (to_be_read_content_hash && sha1_name_hash_20 &&
    byte_count && sha1_content_hash_20 && hash_time && content_last_modified_time)
  {
    ON_SHA1_Hash tmp_hash = to_be_read_content_hash->NameHash();
    ON_SHA1_Hash_Copy_To_Array(&tmp_hash, sha1_name_hash_20);
    *byte_count = to_be_read_content_hash->ByteCount();

    tmp_hash = to_be_read_content_hash->ContentHash();
    ON_SHA1_Hash_Copy_To_Array(&tmp_hash, sha1_content_hash_20);
    *hash_time = to_be_read_content_hash->HashCalculationTime();
    *content_last_modified_time = to_be_read_content_hash->ContentLastModifiedTime();

   rc = true;
  }

  return rc;
}

RH_C_FUNCTION void ON_ContentHash_Delete(ON_ContentHash* hash)
{
  delete hash;
}

RH_C_FUNCTION ON_ContentHash* ON_ContentHash_CreateFromFile(const RHMONO_STRING* path)
{
  INPUTSTRINGCOERCE(_path, path);

  ON_ContentHash hash = ON_ContentHash::CreateFromFile(_path);
  return new ON_ContentHash(hash);
}




// ON_NameHash

RH_C_FUNCTION ON_NameHash* ON_NameHash_Create(
  /*ARRAY*/const unsigned char* sha1_name_hash_20, //length must be 20 or behavior is undefined
  unsigned int name_hash_flags,
  ON_UUID parent_id)
{
  if (sha1_name_hash_20 == nullptr || sha1_name_hash_20 == nullptr)
    return nullptr;

  ON_NameHash h = ON_NameHash::Internal_DotNetInterfaceSet(
    parent_id,
    *ON_SHA1_Hash_From_Array_Reinterpret(sha1_name_hash_20),
    name_hash_flags
  );

  ON_NameHash* on_heap = new ON_NameHash(h);
  return on_heap;
}

RH_C_FUNCTION bool ON_NameHash_Read(
  const ON_NameHash* to_be_read_name_hash,
  /*ARRAY*/unsigned char* sha1_name_hash_20, //length must be 20 or behavior is undefined
  unsigned int* flags,
  ON_UUID* parent_id)
{
  bool rc = false;

  if (to_be_read_name_hash && sha1_name_hash_20 &&
    flags)
  {
    ON_SHA1_Hash tmp_hash = to_be_read_name_hash->MappedNameSha1Hash();
    ON_SHA1_Hash_Copy_To_Array(&tmp_hash, sha1_name_hash_20);
    *flags = to_be_read_name_hash->Internal_DotNetInterfaceGetFlags();
    *parent_id = to_be_read_name_hash->ParentId();

    rc = true;
  }

  return rc;
}

RH_C_FUNCTION void ON_NameHash_Delete(ON_NameHash* hash)
{
  delete hash;
}

RH_C_FUNCTION ON_NameHash* ON_NameHash_CreateFilePathHash(const RHMONO_STRING* path)
{
  INPUTSTRINGCOERCE(_path, path);

  ON_NameHash hash = ON_NameHash::CreateFilePathHash(_path);
  return new ON_NameHash(hash);
}

RH_C_FUNCTION ON_NameHash* ON_NameHash_CreateNameHash(const ON_UUID parentId, const RHMONO_STRING* name, bool ignoreCase)
{
  INPUTSTRINGCOERCE(_name, name);

  ON_NameHash hash;
  if (nullptr == _name)
  {
    hash = ON_NameHash::CreateIdAndUnsetName(parentId);
  }
  else
  {
    if (ON_UuidIsNotNil(parentId))
      hash = ON_NameHash::Create(parentId, _name, ignoreCase);
    else
      hash = ON_NameHash::Create(_name, ignoreCase);
  }
  return new ON_NameHash(hash);
}
