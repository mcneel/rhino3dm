using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Rhino.FileIO
{
  /// <summary>
  /// Manages a reference to an existing or non-existing file,
  /// using either or both absolute or relative paths.
  /// Once constructed, this class is immutable.
  /// </summary>
  public sealed class FileReference : IDisposable
  {
    private IntPtr m_const_ptr;

    /// <summary>
    /// Constructs a new instance of the FileReference class,
    /// given a fullPath, a relativePath a content hash and a status value.
    /// </summary>
    /// <since>6.0</since>
    public FileReference(
      string fullPath,
      string relativePath,
      ContentHash hash,
      FileReferenceStatus status
      )
    {
      if (hash == null) throw new ArgumentNullException();

      using (IntPtrSafeHandle handle = hash.GetDisposableHandle())
      {
        m_const_ptr = UnsafeNativeMethods.ON_FileReference_New2(
          fullPath, relativePath, handle, status);
      }
    }

    private FileReference(IntPtr constPtr)
    {
      if (constPtr == IntPtr.Zero) throw new ApplicationException("FileReference pointer is null.");

      m_const_ptr = constPtr;
    }

    /// <summary>
    /// No need to check for IntPtr.Zero. The resulting FileReference will be subject to garbage collection.
    /// </summary>
    internal static FileReference ConstructAndOwnFromConstPtr(IntPtr constPtr)
    {
      if (constPtr == IntPtr.Zero) return null;
      return new FileReference(constPtr);
    }

    /// <summary>
    /// Returns a new file reference. This returns a new instance even if the path does not exist.
    /// </summary>
    /// <param name="fullPath">A full path.</param>
    /// <returns>A file reference to the specified path.</returns>
    /// <since>6.0</since>
    public static FileReference CreateFromFullPath(string fullPath)
    {
      return CreateFromFullAndRelativePaths(fullPath, null);
    }

    /// <summary>
    /// Returns a new file reference. This returns a new instance even if the path does not exist.
    /// </summary>
    /// <param name="fullPath">A full path. This parameter cannot be null.</param>
    /// <param name="relativePath">A relative path. This parameter can be null.</param>
    /// <returns>A file reference to the specified paths.</returns>
    /// <since>6.0</since>
    public static FileReference CreateFromFullAndRelativePaths(string fullPath, string relativePath)
    {
      if (fullPath == null) throw new ArgumentNullException("fullPath");

      IntPtr ptr = UnsafeNativeMethods.ON_FileReference_CreateFromFullAndRelativePaths(fullPath, relativePath);

      return (ptr != IntPtr.Zero) ? new FileReference(ptr) : null;
    }

    /// <summary>
    /// Gets the absolute path of this file reference.
    /// </summary>
    /// <since>6.0</since>
    public string FullPath
    {
      get
      {
        return GetPath(false);
      }
    }

    /// <summary>
    /// Gets the relative path of this file reference.
    /// </summary>
    /// <since>6.0</since>
    public string RelativePath
    {
      get
      {
        return GetPath(true);
      }
    }

    private string GetPath(bool relative)
    {
      using (var holder = new StringHolder())
      {
        if (UnsafeNativeMethods.ON_FileReference_GetFullOrRelativePath(
          m_const_ptr, holder.NonConstPointer(), relative))
        {
          return holder.ToString();
        }

        throw new ApplicationException("Error occurred while marshaling path.");
      }
    }

    /// <summary>
    /// Gets the content hash.
    /// </summary>
    /// <since>6.0</since>
    public ContentHash ContentHash
    {
      get
      {
        IntPtr hash_ptr = UnsafeNativeMethods.ON_FileReference_GetContentHash(m_const_ptr);

        ContentHash hash = Rhino.FileIO.ContentHash.ReadPtr(hash_ptr);

        if (hash == null) throw new ApplicationException("Error occurred while marshaling ContentHash.");

        return hash;
      }
    }

    /// <summary>
    /// Gets the file reference status.
    /// </summary>
    /// <since>6.0</since>
    public FileReferenceStatus FullPathStatus
    {
      get
      {
        IntPtr constPtr = ConstPtr();
        FileReferenceStatus status = UnsafeNativeMethods.ON_FileReference_GetFullPathStatus(constPtr);
        return status;
      }
    }

    /// <summary>
    /// Returns an indication of the fact that the reference is actually set to a non-null value.
    /// </summary>
    /// <since>6.0</since>
    public bool IsSet
    {
      get
      {
        IntPtr constPtr = ConstPtr();
        return UnsafeNativeMethods.ON_FileReference_GetIsSet(constPtr);
      }
    }

    /// <summary>
    /// Reclaims unmanaged resources used by this object.
    /// </summary>
    /// <since>6.0</since>
    public void Dispose()
    {
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        GC.SuppressFinalize(this);
      }

      if (m_const_ptr != IntPtr.Zero)
        UnsafeNativeMethods.ON_FileReference_Delete(m_const_ptr);
      m_const_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// 
    /// </summary>
    ~FileReference()
    {
      Dispose(false);
    }

    internal IntPtr ConstPtr()
    {
      return m_const_ptr;
    }
  }



  /// <summary>
  /// Contains information that is useful to uniquely identify an object name.
  /// <remarks>This object is immutable.</remarks>
  /// </summary>
  public class NameHash : ICloneable, IEquatable<NameHash>
  {
    /// <summary>SHA-1 hash of ordinal minimum mapped Unicode (UTF-32) code points.</summary>
    readonly byte[] m_sha1_hash;

    /// <summary> m_flags - used internally </summary>
    readonly uint m_flags;

    /// <summary> When names appear in a tree structure,
    /// m_parent_id identifies the parent node. </summary>
    readonly Guid m_parent_id;

    /// <summary>
    /// Creates a new NameHash, representing a piece of text.
    /// </summary>
    /// <param name="name">A name. This can be null and can refer to a non-existing path.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public NameHash(string name) : this(name, Guid.Empty)
    {
    }

    /// <summary>
    /// Creates a new NameHash, representing a piece of text.
    /// </summary>
    /// <param name="name">A name. This can be null and can refer to a non-existing path.</param>
    /// <param name="parentId">The id of the parent layer. This is only useful with layers.</param>
    /// <returns>A new hash</returns>
    /// <since>6.0</since>
    public NameHash(string name, Guid parentId) : this(name, parentId, true)
    {
    }

    /// <summary>
    /// Creates a new NameHash, representing a piece of text.
    /// </summary>
    /// <param name="name">A name. This can be null and can refer to a non-existing path.</param>
    /// <param name="parentId">The id of the parent layer. This is only useful with layers.</param>
    /// <param name="type">Calls <see cref="DocObjects.ModelComponent.ModelComponentTypeIgnoresCase"/> to determine if case should be used in search.</param>
    /// <returns>A new hash</returns>
    /// <since>6.0</since>
    public NameHash(string name, Guid parentId, DocObjects.ModelComponentType type) : this(name, parentId, DocObjects.ModelComponent.ModelComponentTypeIgnoresCase(type))
    {
    }

    /// <summary>
    /// Creates a new NameHash, representing a piece of text.
    /// </summary>
    /// <param name="name">A name. This can be null and can refer to a non-existing path.</param>
    /// <param name="parentId">The id of the parent layer. This is only useful with layers.</param>
    /// <param name="ignoreCase">All manifest searches currently ignore case, except for groups.</param>
    /// <returns>A new hash</returns>
    /// <since>6.0</since>
    public NameHash(string name, Guid parentId, bool ignoreCase)
    {
      IntPtr content_ptr = UnsafeNativeMethods.ON_NameHash_CreateNameHash(parentId, name, ignoreCase);
      m_sha1_hash = new byte[20];
      using (var disposable = new NameHashUnmanagedHandle(content_ptr))
      {
        UnsafeNativeMethods.ON_NameHash_Read
          (
          disposable,
          m_sha1_hash,
          ref m_flags,
          ref m_parent_id
        );
      }
    }

    /* Do not allow public creation unless someone asks.
    
    /// <summary>
    /// Constructs a new instance of the name hash class.
    /// </summary>
    /// <param name="sha1Hash">The 20-bytes long SHA1 hash of ordinal minimum mapped unicod
    /// (UTF-32) code points. The array will be copied.</param>
    /// <param name="codePoints">Number of unicode (UTF-32) code points.</param>
    /// <param name="parentId"> When names appear in a tree structure,
    /// m_parent_id identifies the parent node.</param>
    [CLSCompliant(false)]
    public NameHash(
      byte[] sha1Hash,
      uint codePoints,
      Guid parentId
      )
      : this(
          parentId,
          sha1Hash == null ? null : sha1Hash.ToArray(), //copy
          codePoints
          )
    {
      if (sha1Hash == null) throw new ArgumentNullException("sha1Hash");
      if (sha1Hash.Length != 20) throw new ArgumentException(
        "Hash must have a length of exactly 20", "sha1Hash");
    }
    */

    /// <summary>
    /// Constructs a copy of a content hash.
    /// </summary>
    /// <param name="other">The other content hash to copy.</param>
    protected NameHash(NameHash other)
      : this(
          other.m_parent_id,
          other.m_sha1_hash,
          other.m_flags
      )
    {
    }

    /// <summary>
    /// Constructs a new instance of the content hash class. Fields are NOT copied.
    /// </summary>
    internal NameHash(
        Guid parentId,
        byte[] sha1Hash,
        uint flags
        )
    {
      m_parent_id = parentId;
      m_sha1_hash = sha1Hash;
      m_flags = flags;
    }

    private NameHash(IntPtr toBeReadContentHash)
      : this(Guid.Empty, new byte[20], 0)
    {
      UnsafeNativeMethods.ON_NameHash_Read
        (
        toBeReadContentHash,
        m_sha1_hash,
        ref m_flags,
        ref m_parent_id
      );
    }

    /// <summary>
    /// Gets the 20-bytes long SHA-1 hash of ordinal minimum mapped Unicode (UTF-32) code points.
    /// </summary>
    /// <since>6.0</since>
    public byte[] Sha1Hash
    {
      get
      {
        return (byte[])m_sha1_hash.Clone();
      }
    }

    /// <summary>
    /// Gets the NameHash flags. In some cases = number of mapped code points.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint MappedCodePoints
    {
      get
      {
        return m_flags;
      }
    }

    /// <summary>
    /// Only useful if this participates in a tree structure, as with layers.
    /// </summary>
    /// <since>6.0</since>
    public Guid ParentId
    {
      get
      {
        return m_parent_id;
      }
    }

    /// <summary>
    /// Be responsible: call Dispose()
    /// </summary>
    internal IntPtrSafeHandle GetDisposableHandle()
    {
      return new NameHashUnmanagedHandle(this);
    }

    private class NameHashUnmanagedHandle : IntPtrSafeHandle
    {
      internal NameHashUnmanagedHandle(NameHash hash)
      {
        // This calls ON_NameHash::Internal_CreateForDotNetInterface() through
        // pInvoke. It is critical that the three parameters are byte-by-byte
        // identical to information copied from a valid instance of a C++
        // ON_NameHash object. Otherwise, it is nearly certain an invalid
        // ON_NameHash will be created and it will not be able to provide the
        // expected services.
        UnsafePointer = UnsafeNativeMethods.ON_NameHash_Create(
          hash.m_sha1_hash,
          hash.m_flags,
          hash.m_parent_id);

        if (UnsafePointer == IntPtr.Zero)
          throw new NotSupportedException("An error happened when constructing a marshaled ON_NameHash.");
      }

      internal NameHashUnmanagedHandle(IntPtr createdPtr)
      {
        UnsafePointer = createdPtr;
      }

      protected override void ReleaseUnsafePointer()
      {
        UnsafeNativeMethods.ON_ContentHash_Delete(UnsafePointer);
      }
    }

    /// <summary>
    /// Creates a new NameHash, representing the name of a file.
    /// </summary>
    /// <param name="path">A path. This can be null and can refer to a non-existing path.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static NameHash CreateFilePathHash(string path)
    {
      IntPtr content_ptr = UnsafeNativeMethods.ON_NameHash_CreateFilePathHash(path);
      using (var disposable = new NameHashUnmanagedHandle(content_ptr))
      {
        return new NameHash(content_ptr);
      }
    }

    /// <since>6.0</since>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Creates a copy of this name hash.
    /// Because content hash is immutable, this can be used as a deep copy.
    /// </summary>
    /// <returns>A different instance of the same name hash.</returns>
    /// <since>6.0</since>
    public NameHash Clone()
    {
      return this;
    }

    /// <summary>
    /// Determines if another name hash has the same value.
    /// </summary>
    /// <param name="other">The other name hash to compare.</param>
    /// <returns>True if the two hashes are equal.</returns>
    /// <since>6.0</since>
    public bool Equals(NameHash other)
    {
      if (other == null)
        return false;

      return
        Enumerable.SequenceEqual(m_sha1_hash, other.m_sha1_hash) &&
        m_flags == other.m_flags &&
        m_parent_id == other.m_parent_id;
    }

    /// <summary>
    /// Determines if another object is a name hash with same value.
    /// </summary>
    /// <param name="obj">The other content hash to compare.</param>
    /// <returns>True if the two hashes are equal.</returns>
    public override bool Equals(object obj)
    {
      return Equals(obj as NameHash);
    }

    /// <summary>
    /// Gets an hash code for this name hash.
    /// Two equal content hashes have equal hash code. The other way around might not be true.
    /// </summary>
    /// <returns>An hash code value.</returns>
    public override int GetHashCode()
    {
      return
        ((m_sha1_hash[0]) << 24 ^
        (m_sha1_hash[1]) << 16 ^
        (m_sha1_hash[2]) << 8 ^
        (m_sha1_hash[3])) ^
        (int)m_flags    ^
        m_parent_id.GetHashCode()
      ;
    }

    /// <summary>
    /// Determines if two NameHash instances are equal by value.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>True if they are equal by value, otherwise false.</returns>
    /// <since>6.0</since>
    public static bool operator ==(NameHash left, NameHash right)
    {
      if ((object)left == null) return (object)right == null;
      return left.Equals(right);
    }

    /// <summary>
    /// Determines if two NameHash instances are different by value.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>True if they are different by value, otherwise false.</returns>
    /// <since>6.0</since>
    public static bool operator !=(NameHash left, NameHash right)
    {
      return !(left == right);
    }
  }





  /// <summary>
  /// Contains information that is useful to uniquely identify an object.
  /// <remarks>This object is immutable.</remarks>
  /// </summary>
  public class ContentHash : ICloneable, IEquatable<ContentHash>
  {
    readonly byte[] m_sha1NameHash;
    readonly ulong m_byteCount;
    readonly byte[] m_sha1ContentHash;
    readonly ulong m_hashTime;
    readonly ulong m_contentLastModifiedTime;

    /* Do not allow public creation unless someone asks.
    
    /// <summary>
    /// Constructs a new instance of the content hash class.
    /// </summary>
    /// <param name="sha1NameHash">The 20-bytes long SHA1 hash of the name. The array will be copied.</param>
    /// <param name="byteCount">Length in bytes.</param>
    /// <param name="sha1ContentHash">The 20-bytes long SHA1 hash of the content. The array will be copied.</param>
    /// <param name="hashTime">Time of hashing. Will be rounded to seconds.</param>
    /// <param name="contentLastModifiedTime">Time of last modification. Will be rounded to seconds.</param>
    [CLSCompliant(false)]
    public ContentHash(
      byte[] sha1NameHash,
      ulong byteCount,
      byte[] sha1ContentHash,
      DateTime hashTime,
      DateTime contentLastModifiedTime
      )
      : this(
        sha1NameHash == null ? null : sha1NameHash.ToArray(), //copy
        byteCount,
        sha1ContentHash == null ? null : sha1ContentHash.ToArray(), //copy
        TimeHelpers.ToUnixEpoch(hashTime),
        TimeHelpers.ToUnixEpoch(contentLastModifiedTime)
          )
    {
      if (sha1NameHash == null) throw new ArgumentNullException("sha1NameHash");
      if (sha1ContentHash == null) throw new ArgumentNullException("sha1ContentHash");
      if (sha1NameHash.Length != 20) throw new ArgumentException(
        "Hash must have a length of exactly 20", "sha1NameHash");
      if (sha1ContentHash.Length != 20) throw new ArgumentException(
        "Hash must have a length of exactly 20", "sha1ContentHash");
    }
    */

    /// <summary>
    /// Constructs a copy of a content hash.
    /// </summary>
    /// <param name="other">The other content hash to copy.</param>
    protected ContentHash(ContentHash other)
      : this(other.m_sha1NameHash,
      other.m_byteCount,
      other.m_sha1ContentHash,
      other.m_hashTime,
      other.m_contentLastModifiedTime)
    {
    }

    /// <summary>
    /// Constructs a new instance of the content hash class.
    /// </summary>
    /// <param name="sha1NameHash">Provide an immutable copy of this array or use the public constructor.</param>
    /// <param name="byteCount">Length in bytes.</param>
    /// <param name="sha1ContentHash">Provide an immutable copy of this array or use the public constructor.</param>
    /// <param name="hashTime">Seconds from UNIX epoch.</param>
    /// <param name="contentLastModifiedTime">Seconds from UNIX epoch.</param>
    internal ContentHash(
        byte[] sha1NameHash,
        ulong byteCount,
        byte[] sha1ContentHash,
        ulong hashTime,
        ulong contentLastModifiedTime)
    {
      m_sha1NameHash = sha1NameHash;
      m_byteCount = byteCount;
      m_sha1ContentHash = sha1ContentHash;
      m_hashTime = hashTime;
      m_contentLastModifiedTime = contentLastModifiedTime;
    }

    private ContentHash(IntPtr toBeReadContentHash)
      : this(new byte[20], 0, new byte[20], 0UL, 0UL)
    {
      if(!UnsafeNativeMethods.ON_ContentHash_Read(
        toBeReadContentHash,
        m_sha1NameHash,
        ref m_byteCount,
        m_sha1ContentHash,
        ref m_hashTime,
        ref m_contentLastModifiedTime
      ))
        throw new ApplicationException("Could not read ContentHash");
    }

    internal static ContentHash ReadPtr(IntPtr toBeReadConstContentHash)
    {
      ContentHash ch = null;
      if (toBeReadConstContentHash != IntPtr.Zero)
        ch = new ContentHash(toBeReadConstContentHash);
      return ch;
    }

    /// <summary>
    /// Gets the 20-bytes long SHA1 hash of the name.
    /// </summary>
    /// <since>6.0</since>
    public byte[] Sha1NameHash
    {
      get
      {
        return (byte[])m_sha1NameHash.Clone();
      }
    }

    /// <summary>
    /// Gets the length of the content, in bytes.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public ulong ByteCount
    {
      get
      {
        return m_byteCount;
      }
    }

    /// <summary>
    /// Gets the 20-bytes long SHA1 hash of the content.
    /// </summary>
    /// <since>6.0</since>
    public byte[] Sha1ContentHash
    {
      get
      {
        return (byte[])m_sha1ContentHash.Clone();
      }
    }

    /// <summary>
    /// Gets the hash time, rounded to seconds.
    /// </summary>
    /// <since>6.0</since>
    public DateTime HashTime
    {
      get
      {
        return TimeHelpers.FromUnixEpoch(m_hashTime);
      }
    }

    /// <summary>
    /// Be responsible: call Dispose()
    /// </summary>
    internal IntPtrSafeHandle GetDisposableHandle()
    {
      return new ContentHashUnmanagedHandle(this);
    }

    private class ContentHashUnmanagedHandle : IntPtrSafeHandle
    {
      internal ContentHashUnmanagedHandle(ContentHash hash)
      {
        UnsafePointer = UnsafeNativeMethods.ON_ContentHash_Create(
          hash.m_sha1NameHash,
          hash.m_byteCount,
          hash.m_sha1ContentHash,
          hash.m_hashTime,
          hash.m_contentLastModifiedTime);

        if (UnsafePointer == IntPtr.Zero)
          throw new NotSupportedException("An error happened when constructing a marshaled ON_ContentHash.");
      }

      internal ContentHashUnmanagedHandle(IntPtr createdPtr)
      {
        UnsafePointer = createdPtr;
      }

      protected override void ReleaseUnsafePointer()
      {
        UnsafeNativeMethods.ON_ContentHash_Delete(UnsafePointer);
      }
    }

    /// <summary>
    /// Creates a new ContentHash, representing the content of a file.
    /// </summary>
    /// <param name="path">A path. This can be null and can refer to a non-existing path.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static ContentHash CreateFromFile(string path)
    {
      IntPtr content_ptr = UnsafeNativeMethods.ON_ContentHash_CreateFromFile(path);
      using (var disposable = new ContentHashUnmanagedHandle(content_ptr))
      {
        return new ContentHash(content_ptr);
      }
    }

    /// <since>6.0</since>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Creates a copy of this content hash.
    /// Because content hash is immutable, this can be used as a deep copy.
    /// </summary>
    /// <returns>A different instance of the same content hash.</returns>
    /// <since>6.0</since>
    public ContentHash Clone()
    {
      return this;
    }

    /// <summary>
    /// Determines if another content hash has the same value.
    /// </summary>
    /// <param name="other">The other content hash to compare.</param>
    /// <returns>True if the two hashes are equal.</returns>
    /// <since>6.0</since>
    public bool Equals(ContentHash other)
    {
      if (other == null)
        return false;

      return
        Enumerable.SequenceEqual(m_sha1NameHash, other.m_sha1NameHash) &&
        m_byteCount == other.m_byteCount &&
        Enumerable.SequenceEqual(m_sha1ContentHash, other.m_sha1ContentHash) &&
        m_hashTime == other.m_hashTime &&
        m_contentLastModifiedTime == other.m_contentLastModifiedTime;
    }

    /// <summary>
    /// Determines if another object is a content hash with same value.
    /// </summary>
    /// <param name="obj">The other content hash to compare.</param>
    /// <returns>True if the two hashes are equal.</returns>
    public override bool Equals(object obj)
    {
      return Equals(obj as ContentHash);
    }

    /// <summary>
    /// Gets an hash code for this content hash.
    /// Two equal content hashes have equal hash code. The other way around might not be true.
    /// </summary>
    /// <returns>An hash code value.</returns>
    public override int GetHashCode()
    {
      return
        ((m_sha1NameHash[0]) << 24 ^
        (m_sha1NameHash[1]) << 16 ^
        (m_sha1ContentHash[0]) << 8 ^
        (m_sha1ContentHash[1])) ^
        (int)m_hashTime ^
        (int)m_byteCount ^
        (int)m_contentLastModifiedTime
      ;
    }

    /// <summary>
    /// Determines if two ContentHash instances are equal by value.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>True if they are equal by value, otherwise false.</returns>
    /// <since>6.0</since>
    public static bool operator ==(ContentHash left, ContentHash right)
    {
      if ((object)left == null) return (object)right == null;
      return left.Equals(right);
    }

    /// <summary>
    /// Determines if two ContentHash instances are different by value.
    /// </summary>
    /// <param name="left">The first hash.</param>
    /// <param name="right">The second hash.</param>
    /// <returns>True if they are different by value, otherwise false.</returns>
    /// <since>6.0</since>
    public static bool operator !=(ContentHash left, ContentHash right)
    {
      return !(left == right);
    }
  }






#if RHINO_SDK
  /// <summary>
  /// Provides the OpenNURBS implementation of SHA1.
  /// 
  /// <para>This class is provided only with the purpose of hashing. It is not meant to be
  /// used for any cryptographic purpose.</para>
  /// </summary>
  public sealed class SHA1OpenNURBS : SHA1
  {
    readonly IntPtr m_non_const_ptr; //always non-const
    bool m_disposed;

    /// <summary>
    /// Constructs a new instance of the SHA1 algorithm.
    /// </summary>
    /// <since>6.0</since>
    public SHA1OpenNURBS()
    {
      m_non_const_ptr = UnsafeNativeMethods.ON_SHA1_New(IntPtr.Zero);
      if (m_non_const_ptr == IntPtr.Zero)
        throw new InvalidOperationException("ON_SHA1 cannot be constructed.");

      base.HashSizeValue = 160;
    }

    /// <summary>
    /// Resets this instance of the algorithm, so that it can be used again.
    /// It is not required to call this method after creation.
    /// </summary>
    /// <since>6.0</since>
    public override void Initialize()
    {
      if (m_disposed) throw new ObjectDisposedException("SHA1OpenNURBS");

      UnsafeNativeMethods.ON_SHA1_Reset(m_non_const_ptr);
    }

    /// <summary>
    /// Advances the hash by an input array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="start">The start of the data to consider in the array.</param>
    /// <param name="length">The used data length on the array.</param>
    protected override void HashCore(byte[] array, int start, int length)
    {
      if (m_disposed) throw new ObjectDisposedException("SHA1OpenNURBS");
      if (length < 0) throw new ArgumentOutOfRangeException("cbSize");
      if ((start + length) > array.Length) throw new ArgumentException("ibStart + cbSize must be <= array.Length");

      unsafe
      {
        fixed (byte* ptr = array)
        {
          byte* new_from = ptr + start;
          UnsafeNativeMethods.ON_SHA1_Update(m_non_const_ptr, new IntPtr(new_from), (ulong)length);
        }
      }
    }

    /// <summary>
    /// Returns the currently computed hash.
    /// </summary>
    /// <returns>The final hash.</returns>
    protected override byte[] HashFinal()
    {
      if (m_disposed) throw new ObjectDisposedException("SHA1OpenNURBS");

      var bytes = new byte[20];
      UnsafeNativeMethods.ON_SHA1_GetHash(m_non_const_ptr, bytes);
      return bytes;
    }

    /// <summary>
    /// Reclaims unmanaged resources used by this instance, and invalidates
    /// the instance. After calling this method, all other methods will always
    /// call ObjectDisposedException.
    /// </summary>
    /// <param name="disposing">true if the class user called Dispose.</param>
    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      if (!m_disposed) UnsafeNativeMethods.ON_SHA1_Delete(m_non_const_ptr);
      m_disposed = true;
    }

    /// <summary>
    /// Instructs the runtime to reclaim unmanaged resources if the developer
    /// forgot to call Dispose().
    /// </summary>
    ~SHA1OpenNURBS()
    {
      Dispose(false);
    }


    /// <summary>
    /// Computes the SHA1 hash of a string, converted to UTF8.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A 20-byte long SHA1 hash.</returns>
    /// <exception cref="ArgumentNullException">When input is null.</exception>
    /// <since>6.0</since>
    public static byte[] StringHash(string input)
    {
      if (input == null)
        throw new ArgumentNullException("input");

      using (HashAlgorithm sha1 = new SHA1OpenNURBS())
      {
        return sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
      }
    }

    /// <summary> 
    /// Computes the SHA1 hash of a file system path, converted to UTF8.
    ///     <para>These file system paths have identical values of FileSystemPathHash():</para>
    ///     <para>/x/y/z/name.ext</para>
    ///     <para>\x\y\z\name.ext</para>
    ///     <para>/x//y//z/name.ext</para>
    ///     <para>/x/y/a/b/c/../../../z/name.ext</para>
    ///     <para>/X/Y/Z/NAME.EXT (When ignoreCase is true)</para>
    /// </summary>
    /// <param name="path">A non-null path string.</param>
    /// <param name="ignoreCase">If case should be ignored.
    /// If this is null or unspecified, the operating system default is used.</param>
    /// <returns>A 20-byte long SHA1 hash.</returns>
    /// <exception cref="ArgumentNullException">When input is null.</exception>
    public static byte[] FileSystemPathHash(string path, bool? ignoreCase = null)
    {
      if (path == null)
        throw new ArgumentNullException("path");

      var bytes = new byte[20];
      UnsafeNativeMethods.ON_SHA1_FileSystemPathHash(path,
        bytes,
        ignoreCase ?? PathHelpers.PlatformPathIgnoreCase);

      return bytes;
    }


  }


#endif

  static class TimeHelpers
  {
    public static DateTime FromUnixEpoch(ulong secondsFromEpoch)
    {
      var origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      DateTime dateTimeFromEpoch = origin + new TimeSpan((long)secondsFromEpoch * TimeSpan.TicksPerSecond);
      return dateTimeFromEpoch.ToLocalTime();
    }

    public static ulong ToUnixEpoch(DateTime time)
    {
      long universal_requested = time.ToUniversalTime().Ticks;
      long epochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
      long unixTicks = universal_requested - epochTicks;
      return (ulong)(unixTicks / TimeSpan.TicksPerSecond);
    }
  }


  static class PathHelpers
  {
    public static bool PlatformPathIgnoreCase
    {
      get
      {
        return UnsafeNativeMethods.ON_FileSystemPath_PlatformPathIgnoreCase();
      }
    }
  }

}


