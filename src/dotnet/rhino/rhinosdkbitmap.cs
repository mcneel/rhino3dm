using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Rhino.DocObjects.Tables.BitmapTable entry
  /// </summary>
  [Serializable]
  public sealed class BitmapEntry : ModelComponent
  {
    #region members
    // Represents both a CRhinoBitmap and an ON_Bitmap. When m_ptr is
    // null, the object uses m_doc and m_id to look up the const
    // CRhinoBitmap in the bitmap table.
#if RHINO_SDK
    readonly RhinoDoc m_doc;
#endif
    readonly int m_index = -1;
    #endregion

    #region constructors
#if RHINO_SDK
    internal BitmapEntry(int index, RhinoDoc doc) : base()
    {
      m_index = index;
      m_doc = doc;
      m__parent = m_doc;
    }
#endif

    /// <summary>
    /// serialization constructor
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    private BitmapEntry(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal BitmapEntry(int index, FileIO.File3dm onxModel)
    {
      m_index = index;
      m__parent = onxModel;
    }

    #endregion

    internal override IntPtr _InternalGetConstPointer()
    {
      throw new NotImplementedException("Tell steve@mcneel.com if you need access to this method");
    }

    internal override IntPtr NonConstPointer()
    {
      throw new NotImplementedException("Tell steve@mcneel.com if you need access to this method");
    }

    #region properties
    /// <summary>
    /// Returns <see cref="ModelComponentType.Image"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.Image;

    /// <summary>
    /// Gets a value indicting whether this bitmap is a referenced bitmap. 
    /// Referenced bitmaps are part of referenced documents.
    /// </summary>
    /// <since>5.1</since>
    public override bool IsReference => base.IsReference;

    /// <summary>The name of this bitmap.</summary>
    /// <since>5.1</since>
    public string FileName
    {
      get
      {
#if RHINO_SDK
        if (null != m_doc)
          using (var sh = new StringHolder())
          {
            IntPtr pString = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoBitmap_GetBitmapName(m_doc.RuntimeSerialNumber, m_index, pString);
            return sh.ToString();
          }
#endif
        return string.Empty;
      }
    }
    #endregion

    #region methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <since>5.1</since>
    public bool Save(string fileName)
    {
#if RHINO_SDK
      if (null != m_doc)
        return UnsafeNativeMethods.CRhinoBitmap_ExportToFile(m_doc.RuntimeSerialNumber, m_index, fileName);
#endif
      return false;
    }
    #endregion
  }
}

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <summary>
  /// Stores the list of bitmaps in a Rhino document.
  /// </summary>
  public sealed class BitmapTable :
    RhinoDocCommonTable<BitmapEntry>,
    ICollection<BitmapEntry>
  {
    internal BitmapTable(RhinoDoc doc) : base(doc)
    {
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.Image"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.Image;
      }
    }

    /// <summary>
    /// Conceptually, the bitmap table is an array of bitmaps.  The operator[]
    /// can be used to get individual bitmaps.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>
    /// Reference to the bitmap.  If index is out of range, then null is
    /// returned. Note that this reference may become invalid after AddBitmap()
    /// is called.
    /// </returns>
    public BitmapEntry this[int index]
    {
      get
      {
        return __FindIndexInternal(index);
      }
    }

    /// <summary>
    /// Retrieves a BitmapEntry object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A BitmapEntry object, or null if none was found.</returns>
    /// <since>6.0</since>
    public BitmapEntry FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }


    /// <summary>
    /// This function first attempts to find the file with "name" on the disk.
    /// If it does find it, "fileName" is set to the full path of the file and
    /// the BitmapEntry returned will be null, even if there was a BitmapEntry
    /// with "name" in the bitmap table.
    /// If the function cannot find the file on the disk, it searches the bitmap
    /// table.  If it finds it, the returned BitmapEntry entry will be the entry
    /// in the table with that name.
    /// Additionally, if "createFile" is true, and an entry is found, the file
    /// will be written to the disk and it's full path will be contained in "fileName".
    /// </summary>
    /// <param name="name">
    /// Name of the file to search for including file extension.
    /// </param>
    /// <param name="createFile">
    /// If this is true, and the file is not found on the disk but is found in
    /// the BitmapTable, then the BitmapEntry will get saved to the Rhino bitmap
    /// file cache and fileName will contain the full path to the cached file.
    /// </param>
    /// <param name="fileName">
    /// The full path to the current location of this file or an empty string
    /// if the file was not found and/or not extracted successfully.
    /// </param>
    /// <returns>
    /// Returns null if "name" was found on the disk.  If name was not found on the disk,
    /// returns the BitmapEntry with the specified name if it is found in the bitmap table
    /// and null if it was not found in the bitmap table.
    /// </returns>
    /// <since>5.1</since>
    public BitmapEntry Find(string name, bool createFile, out string fileName)
    {
      fileName = string.Empty;
      int index = -1;
      if (null != Document)
      {
        fileName = Document.FindFile(name);
        if (string.IsNullOrEmpty(fileName))
          index = UnsafeNativeMethods.CRhinoBitmapTable_BitmapFromFileName(Document.RuntimeSerialNumber, name);
        if (createFile && string.IsNullOrEmpty(fileName) && index >= 0)
        {
          string tempFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "embedded_files");
          tempFileName = System.IO.Path.Combine(tempFileName, System.IO.Path.GetFileName(name));
          if (System.IO.File.Exists(tempFileName))
            fileName = tempFileName;
          else if (UnsafeNativeMethods.CRhinoBitmap_ExportToFile(m_doc.RuntimeSerialNumber, index, tempFileName))
            fileName = tempFileName;
        }
      }
      if (index >= 0)
        return new BitmapEntry(index, m_doc);
      return null;
    }

    /// <summary>Adds a new bitmap with specified name to the bitmap table.</summary>
    /// <param name="bitmapFilename">
    /// If NULL or empty, then a unique name of the form "Bitmap 01" will be automatically created.
    /// </param>
    /// <param name="replaceExisting">
    /// If true and the there is already a bitmap using the specified name, then that bitmap is replaced.
    /// If false and there is already a bitmap using the specified name, then -1 is returned.
    /// </param>
    /// <returns>
    /// index of new bitmap in table on success. -1 on error.
    /// </returns>
    /// <since>5.0</since>
    public int AddBitmap(string bitmapFilename, bool replaceExisting)
    {
      return UnsafeNativeMethods.CRhinoBitmapTable_AddBitmap(m_doc.RuntimeSerialNumber, bitmapFilename, replaceExisting);
    }

    void ICollection<BitmapEntry>.Add(BitmapEntry entry)
    {
      AddBitmap(entry.FileName, false);
    }

    /// <summary>Deletes a bitmap.</summary>
    /// <param name="bitmapFilename">The bitmap file name.</param>
    /// <returns>
    /// true if successful. false if the bitmap cannot be deleted because it
    /// is the current bitmap or because it bitmap contains active geometry.
    /// </returns>
    /// <since>5.0</since>
    public bool DeleteBitmap(string bitmapFilename)
    {
      return UnsafeNativeMethods.CRhinoBitmapTable_DeleteBitmap(m_doc.RuntimeSerialNumber, bitmapFilename);
    }

    /// <summary>
    /// Removes the bitmap from the table.
    /// </summary>
    /// <param name="item">The item to remove. Null will always return false.</param>
    /// <returns>True if the item could be deleted; otherwise, false.</returns>
    /// <since>6.0</since>
    public override bool Delete(BitmapEntry item)
    {
      if (item == null) return false;
      return DeleteBitmap(item.FileName);
    }

    /// <summary>Exports all the bitmaps in the table to files.</summary>
    /// <param name="directoryPath">
    /// full path to the directory where the bitmaps should be saved.
    /// If NULL, a dialog is used to interactively get the directory name.
    /// </param>
    /// <param name="overwrite">0 = no, 1 = yes, 2 = ask.</param>
    /// <returns>Number of bitmaps written.</returns>
    /// <since>5.0</since>
    public int ExportToFiles(string directoryPath, int overwrite)
    {
      return UnsafeNativeMethods.CRhinoBitmapTable_ExportToFiles(m_doc.RuntimeSerialNumber, directoryPath, overwrite);
    }

    /// <summary>Writes a bitmap to a file.</summary>
    /// <param name="index">The index of the bitmap to be written.</param>
    /// <param name="path">
    /// The full path, including file name and extension, name of the file to write.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool ExportToFile(int index, string path)
    {
      return UnsafeNativeMethods.CRhinoBitmapTable_ExportToFile(m_doc.RuntimeSerialNumber, index, path);
    }

    //[skipping]
    //  void SetRemapIndex( int, // bitmap_index
  }
}
#endif
