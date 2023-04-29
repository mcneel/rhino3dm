using System;
using System.IO;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using Rhino.DocObjects;
using System.Collections;
using System.Linq;
using Rhino.Collections;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents a 3dm file, which is stored using the OpenNURBS file standard.
  /// <para>The 3dm format is the main Rhinoceros storage format.</para>
  /// <para>Visit http://www.opennurbs.com/ for more details.</para>
  /// </summary>
  public class File3dm : IDisposable
  {
    /// <summary></summary>
    /// <since>5.9</since>
    [CLSCompliant(false)]
    [Flags]
    public enum TableTypeFilter : uint
    {
      /// <summary></summary>
      None = UnsafeNativeMethods.ReadFileTableTypeFilter.None,
      /// <summary></summary>
      Properties = UnsafeNativeMethods.ReadFileTableTypeFilter.PropertiesTable,
      /// <summary></summary>
      Settings = UnsafeNativeMethods.ReadFileTableTypeFilter.SettingsTable,
      /// <summary></summary>
      Bitmap = UnsafeNativeMethods.ReadFileTableTypeFilter.BitmapTable,
      /// <summary></summary>
      TextureMapping = UnsafeNativeMethods.ReadFileTableTypeFilter.TextureMappingTable,
      /// <summary></summary>
      Material = UnsafeNativeMethods.ReadFileTableTypeFilter.MaterialTable,
      /// <summary></summary>
      Linetype = UnsafeNativeMethods.ReadFileTableTypeFilter.LinetypeTable,
      /// <summary></summary>
      Layer = UnsafeNativeMethods.ReadFileTableTypeFilter.LayerTable,
      /// <summary></summary>
      Group = UnsafeNativeMethods.ReadFileTableTypeFilter.GroupTable,
      /// <summary></summary>
      Font = UnsafeNativeMethods.ReadFileTableTypeFilter.FontTable,
      /// <summary></summary>
      FutureFont = UnsafeNativeMethods.ReadFileTableTypeFilter.FutureFontTable,
      /// <summary></summary>
      Dimstyle = UnsafeNativeMethods.ReadFileTableTypeFilter.DimstyleTable,
      /// <summary></summary>
      Light = UnsafeNativeMethods.ReadFileTableTypeFilter.LightTable,
      /// <summary></summary>
      Hatchpattern = UnsafeNativeMethods.ReadFileTableTypeFilter.HatchpatternTable,
      /// <summary></summary>
      InstanceDefinition = UnsafeNativeMethods.ReadFileTableTypeFilter.InstanceDefinitionTable,
      /// <summary></summary>
      ObjectTable = UnsafeNativeMethods.ReadFileTableTypeFilter.ObjectTable,
      /// <summary></summary>
      Historyrecord = UnsafeNativeMethods.ReadFileTableTypeFilter.HistoryrecordTable,
      /// <summary></summary>
      UserTable = UnsafeNativeMethods.ReadFileTableTypeFilter.UserTable
    }

    /// <summary></summary>
    /// <since>5.9</since>
    [CLSCompliant(false)]
    [Flags]
    public enum ObjectTypeFilter : uint
    {
      /// <summary></summary>
      None = UnsafeNativeMethods.ObjectTypeFilter.None,
      /// <summary>some type of Point</summary>
      Point = UnsafeNativeMethods.ObjectTypeFilter.Point,
      /// <summary>some type of PointCloud, PointGrid, ...</summary>
      Pointset = UnsafeNativeMethods.ObjectTypeFilter.Pointset,
      /// <summary>some type of Curve like LineCurve, NurbsCurve, etc.</summary>
      Curve = UnsafeNativeMethods.ObjectTypeFilter.Curve,
      /// <summary>some type of Surface like PlaneSurface, NurbsSurface, etc.</summary>
      Surface = UnsafeNativeMethods.ObjectTypeFilter.Surface,
      /// <summary>some type of Brep</summary>
      Brep = UnsafeNativeMethods.ObjectTypeFilter.Brep,
      /// <summary>some type of Mesh</summary>
      Mesh = UnsafeNativeMethods.ObjectTypeFilter.Mesh,
      /// <summary>some type of Annotation</summary>
      Annotation = UnsafeNativeMethods.ObjectTypeFilter.Annotation,
      /// <summary>some type of InstanceDefinition</summary>
      InstanceDefinition = UnsafeNativeMethods.ObjectTypeFilter.InstanceDefinition,
      /// <summary>some type of InstanceReference</summary>
      InstanceReference = UnsafeNativeMethods.ObjectTypeFilter.InstanceReference,
      /// <summary>some type of TextDot</summary>
      TextDot = UnsafeNativeMethods.ObjectTypeFilter.TextDot,
      /// <summary>some type of DetailView</summary>
      DetailView = UnsafeNativeMethods.ObjectTypeFilter.Detail,
      /// <summary>some type of Hatch</summary>
      Hatch = UnsafeNativeMethods.ObjectTypeFilter.Hatch,
      /// <summary>some type of Extrusion</summary>
      Extrusion = UnsafeNativeMethods.ObjectTypeFilter.Extrusion,
      /// <summary></summary>
      Any = UnsafeNativeMethods.ObjectTypeFilter.Any
    }

    IntPtr m_ptr; //ONX_Model*
    File3dmManifestTable m_manifest_table;
    File3dmObjectTable m_object_table;
    File3dmMaterialTable m_material_table;
    File3dmLinetypeTable m_linetype_table;
    File3dmLayerTable m_layer_table;
    File3dmGroupTable m_group_table;
    File3dmDimStyleTable m_dimstyle_table;
    File3dmHatchPatternTable m_hatchpattern_table;
    File3dmInstanceDefinitionTable m_instance_definition_table;
    File3dmPlugInDataTable m_userdata_table;
    File3dmViewTable m_view_table;
    File3dmViewTable m_named_view_table;
    File3dmStringTable m_string_table;
    File3dmNamedConstructionPlanes m_named_cplanes;
    File3dmEmbeddedFiles m_embedded_files;
    File3dmRenderMaterials m_render_materials;
    File3dmRenderEnvironments m_render_environments;
    File3dmRenderTextures m_render_textures;

    internal IntPtr ConstPointer()
    {
      return NonConstPointer(); // all ONX_Models are non-const
    }
    internal IntPtr NonConstPointer()
    {
      if (m_ptr == IntPtr.Zero)
        throw new ObjectDisposedException("File3dm");
      return m_ptr;
    }

    #region statics
    /// <summary>
    /// Reads a 3dm file from a specified location.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <returns>new File3dm on success, null on error.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist.</exception>
    /// <since>5.0</since>
    public static File3dm Read(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);
      IntPtr ptr_onx_model = UnsafeNativeMethods.ONX_Model_ReadFile(path, IntPtr.Zero);
      return ptr_onx_model == IntPtr.Zero ? null : new File3dm(ptr_onx_model);
    }

    /// <summary>
    /// Reads a 3dm file from a specified location.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="tableTypeFilterFilter">
    /// If tableTypeFilterFilter is None, then everything in the archive is read.
    /// Otherwise tableTypeFilterFilter identifies what tables should be read.
    /// </param>
    /// <param name="objectTypeFilter">
    /// If objectTypeFilter is not None, then is a filter made by bitwise or-ing
    /// values to select which types of objects will be read from the model object
    /// table.
    /// </param>
    /// <returns>new File3dm on success, null on error.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist.</exception>
    /// <since>5.9</since>
    [CLSCompliant(false)]
    public static File3dm Read(string path, TableTypeFilter tableTypeFilterFilter, ObjectTypeFilter objectTypeFilter)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);

      IntPtr ptr_onx_model = UnsafeNativeMethods.ONX_Model_ReadFile2(path, (UnsafeNativeMethods.ReadFileTableTypeFilter)tableTypeFilterFilter, (UnsafeNativeMethods.ObjectTypeFilter)objectTypeFilter, IntPtr.Zero);
      return ptr_onx_model == IntPtr.Zero ? null : new File3dm(ptr_onx_model);
    }

    /// <summary>
    /// Reads a 3dm file from a specified location.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="tableTypeFilterFilter">
    /// If tableTypeFilterFilter is None, then everything in the archive is read.
    /// Otherwise tableTypeFilterFilter identifies what tables should be read.
    /// </param>
    /// <param name="objectTypeFilter">
    /// If objectTypeFilter is not None, then is a filter made by bitwise or-ing
    /// values to select which types of objects will be read from the model object
    /// table.
    /// </param>
    /// <param name="errorLog">Any archive reading errors are logged here.</param>
    /// <returns>new File3dm on success, null on error.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist.</exception>
    /// <since>5.9</since>
    [CLSCompliant(false)]
    public static File3dm ReadWithLog(string path, TableTypeFilter tableTypeFilterFilter, ObjectTypeFilter objectTypeFilter, out string errorLog)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);

      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_onx_model = UnsafeNativeMethods.ONX_Model_ReadFile2(path, (UnsafeNativeMethods.ReadFileTableTypeFilter)tableTypeFilterFilter, (UnsafeNativeMethods.ObjectTypeFilter)objectTypeFilter, ptr_string);
        errorLog = sh.ToString();
        return ptr_onx_model == IntPtr.Zero ? null : new File3dm(ptr_onx_model);
      }
    }

    /// <summary>
    /// Read a 3dm file from a specified location and log any archive
    /// reading errors.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="errorLog">Any archive reading errors are logged here.</param>
    /// <returns>New File3dm on success, null on error.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist.</exception>
    /// <since>5.0</since>
    public static File3dm ReadWithLog(string path, out string errorLog)
    {
      errorLog = string.Empty;
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_onx_model = UnsafeNativeMethods.ONX_Model_ReadFile(path, ptr_string);
        errorLog = sh.ToString();
        return ptr_onx_model == IntPtr.Zero ? null : new File3dm(ptr_onx_model);
      }
    }

    /// <summary>
    /// Read a 3dm file from a byte array
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>New File3dm on success, null on error.</returns>
    /// <since>7.0</since>
    public static File3dm FromByteArray(byte[] bytes)
    {
      IntPtr ptr_onx_model = UnsafeNativeMethods.ONX_Model_FromByteArray(bytes.Length, bytes);
      return ptr_onx_model == IntPtr.Zero ? null : new File3dm(ptr_onx_model);
    }

    /// <summary>Reads only the notes from an existing 3dm file.</summary>
    /// <param name="path">The file from which to read the notes.</param>
    /// <returns>The 3dm file notes.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist, is null or cannot be accessed because of permissions.</exception>
    /// <since>5.0</since>
    public static string ReadNotes(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);

      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_ReadNotes(path, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary> Reads only the archive 3dm version from an existing 3dm file. </summary>
    /// <param name="path">The file from which to read the archive version.</param>
    /// <returns>The 3dm file archive version.</returns>
    /// <exception cref="FileNotFoundException">If path does not exist, is null or cannot be accessed because of permissions.</exception>
    /// <since>5.10</since>
    public static int ReadArchiveVersion(string path)
    {
      if (!File.Exists (path))
        throw new FileNotFoundException ("The provided path is null, does not exist or cannot be accessed.", path);

      return UnsafeNativeMethods.ONX_Model_ReadArchiveVersion (path);
    }

    /// <summary>
    /// Quickly check a file for it's revision information.  This function does
    /// not read the entire file, just what it needs to get revision information out
    /// </summary>
    /// <param name="path">path to the 3dm file</param>
    /// <param name="createdBy">original author of the file</param>
    /// <param name="lastEditedBy">last person to edit the file</param>
    /// <param name="revision">which revision this file is at</param>
    /// <param name="createdOn">date file was created (DateTime.MinValue if not set in file)</param>
    /// <param name="lastEditedOn">date file was last edited (DateTime.MinValue if not set in file)</param>
    /// <returns>true on success</returns>
    /// <since>5.6</since>
    public static bool ReadRevisionHistory(string path, out string createdBy, out string lastEditedBy, out int revision, out DateTime createdOn, out DateTime lastEditedOn)
    {
      createdBy = "";
      lastEditedBy = "";
      revision = 0;
      createdOn = DateTime.MinValue;
      lastEditedOn = DateTime.MinValue;
      using (var sh_created = new StringHolder())
      using (var sh_edited = new StringHolder())
      {
        IntPtr ptr_created = sh_created.NonConstPointer();
        IntPtr ptr_edited = sh_edited.NonConstPointer();
        IntPtr ptr_revhist = UnsafeNativeMethods.ONX_Model_ReadRevisionHistory(path, ptr_created, ptr_edited, ref revision);
        bool rc = ptr_revhist != IntPtr.Zero;
        if (rc)
        {
          int second = 0, minute = 0, hour = 0, month = 0, day = 0, year = 0;
          if (UnsafeNativeMethods.ON_3dmRevisionHistory_GetDate(ptr_revhist, true, ref second, ref minute, ref hour, ref day, ref month, ref year))
            createdOn = new DateTime(year, month+1, day, hour, minute, second);
          if (UnsafeNativeMethods.ON_3dmRevisionHistory_GetDate(ptr_revhist, false, ref second, ref minute, ref hour, ref day, ref month, ref year))
            lastEditedOn = new DateTime(year, month+1, day, hour, minute, second);
          createdBy = sh_created.ToString();
          lastEditedBy = sh_edited.ToString();
          UnsafeNativeMethods.ON_3dmRevisionHistory_Delete(ptr_revhist);
        }
        return rc;
      }
    }

    /// <summary>
    /// Reads only the application information from an existing 3dm file.
    /// </summary>
    /// <param name="path">A location on disk or network.</param>
    /// <param name="applicationName">The application name. This out parameter is assigned during this call.</param>
    /// <param name="applicationUrl">The application URL. This out parameter is assigned during this call.</param>
    /// <param name="applicationDetails">The application details. This out parameter is assigned during this call.</param>
    /// <since>5.0</since>
    public static void ReadApplicationData(string path, out string applicationName, out string applicationUrl, out string applicationDetails)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);
      using (var name = new StringHolder())
      using (var url = new StringHolder())
      using (var details = new StringHolder())
      {
        IntPtr ptr_name = name.NonConstPointer();
        IntPtr ptr_url = url.NonConstPointer();
        IntPtr ptr_details = url.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_ReadApplicationDetails(path, ptr_name, ptr_url, ptr_details);
        applicationName = name.ToString();
        applicationUrl = url.ToString();
        applicationDetails = details.ToString();
      }
    }

    /// <summary>
    /// Creates a simple 3dm file that contains a single geometric object.
    /// </summary>
    /// <param name="path">Path to the 3dm file to create.</param>
    /// <param name="geometry">
    /// The geometry to be saved in the archive's object table.
    /// This is typically a Curve, Surface, Brep, Mesh, or SubD.
    /// </param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public static bool WriteOneObject(string path, GeometryBase geometry)
    {
      if (string.IsNullOrEmpty(path) || null == geometry)
        return false;

      IntPtr ptr_const_geometry = geometry.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_WriteOneObject(path, ptr_const_geometry);
    }

    /// <summary>
    /// Creates a simple 3dm file that contains a multiple geometric objects.
    /// </summary>
    /// <param name="path">Path to the 3dm file to create.</param>
    /// <param name="geometry">
    /// The geometry to be saved in the archive's object table.
    /// This is typically some Curves, Surfaces, Breps, Meshs, or SubDs.
    /// </param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public static bool WriteMultipleObjects(string path, IEnumerable<GeometryBase> geometry)
    {
      if (string.IsNullOrEmpty(path))
        return false;

      IntPtr ptr_object_array = UnsafeNativeMethods.ON_ObjectArray_New();
      foreach(var geo in geometry)
      {
        IntPtr ptr_const_geometry = geo.ConstPointer();
        UnsafeNativeMethods.ON_ObjectArray_Append(ptr_object_array, ptr_const_geometry);
      }
      bool rc = UnsafeNativeMethods.ONX_Model_WriteMultipleObjects(path, ptr_object_array);
      UnsafeNativeMethods.ON_ObjectArray_Delete(ptr_object_array);
      return rc;
    }


#if RHINO_SDK
#if !MOBILE_BUILD
    /// <summary>
    /// Attempts to read the preview image out of a 3dm file.
    /// </summary>
    /// <param name="path">The location of the file.</param>
    /// <returns>A bitmap, or null on failure.</returns>
    /// <exception cref="FileNotFoundException">If the provided path is null, does not exist or cannot be accessed.</exception>
    /// <since>5.0</since>
    public static System.Drawing.Bitmap ReadPreviewImage(string path)
    {
      // 4-28-2021 Dale Fugier, as of today this code only run on Windows,
      // either with or without Rhino.

      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);

      System.Drawing.Bitmap rc = null;
      if(Rhino.Runtime.HostUtils.RunningOnWindows)
      {
        IntPtr ptr_bitmap = UnsafeNativeMethods.ONX_Model_WinReadPreviewImage(path);
        if (ptr_bitmap != IntPtr.Zero)
        {
          rc = System.Drawing.Image.FromHbitmap(ptr_bitmap);
        }
      }
      // 4-28-2021 Dale Fugier, until librhino3dm_native included the AppKit framework
      // leave this commented out
      //else if(Rhino.Runtime.HostUtils.RunningOnOSX)
      //{
      //  var nsimage = UnsafeNativeMethods.ONX_Model_MacReadPreviewImage(path);
      //  if(nsimage != IntPtr.Zero)
      //    rc = System.Drawing.Image.FromHbitmap(nsimage);
      //}
      return rc;
    }
#endif // #if !MOBILE_BUILD
#endif

#if RHINO_SDK
    /// <summary>
    /// Read the dimension styles table out of a 3dm file.
    /// </summary>
    /// <param name="path">The location of the file.</param>
    /// <exception cref="FileNotFoundException">If the provided path is null, does not exist or cannot be accessed.</exception>
    /// <returns>
    /// Array of dimension styles on success (empty array if file does not contain dimension styles)
    /// null on error
    /// </returns>
    /// <since>6.0</since>
    public static DimensionStyle[] ReadDimensionStyles(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException("The provided path is null, does not exist or cannot be accessed.", path);
      IntPtr ptr_dimstyle_array = UnsafeNativeMethods.ON_DimStyleArray_New();
      DimensionStyle[] rc = null;
      if(UnsafeNativeMethods.RHC_ImportAnnotationStyles(path, ptr_dimstyle_array))
      {
        int count = UnsafeNativeMethods.ON_DimStyleArray_Count(ptr_dimstyle_array);
        List<DimensionStyle> dimstyle_list = new List<DimensionStyle>(count);
        for (int i = 0; i < count; i++)
        {
          IntPtr ptr_dimstyle = UnsafeNativeMethods.ON_DimStyleArray_Get(ptr_dimstyle_array, i);
          if (ptr_dimstyle != IntPtr.Zero)
            dimstyle_list.Add(new DimensionStyle(ptr_dimstyle));
        }
        rc = dimstyle_list.ToArray();
      }
      UnsafeNativeMethods.ON_DimStyleArray_Delete(ptr_dimstyle_array, false);
      return rc;
    }
#endif

    #endregion

    /// <summary>
    /// Writes contents of this model to an openNURBS archive.
    /// If the model is not valid, then Write will refuse to write it.
    /// </summary>
    /// <param name="path">The file name to use for writing.</param>
    /// <param name="version">
    /// Version of the openNURBS archive to write.  Must be [2; current version].
    /// Rhino can read its current version, plus earlier file versions except 1.
    /// Use latest version when possible.
    /// <para>Alternatively, 0 is a placeholder for the last valid version.</para>
    /// </param>
    /// <returns>
    /// true if archive is written with no error.
    /// false if errors occur.
    /// </returns>
    /// <since>5.0</since>
    public bool Write(string path, int version)
    {
      return Write(path, new File3dmWriteOptions { Version = version });
    }

    /// <summary>
    /// Writes contents of this model to an openNURBS archive.
    /// If the model is not valid, then Write will refuse to write it.
    /// </summary>
    /// <param name="path">The file name to use for writing.</param>
    /// <param name="options">An options instance, or null for default.</param>
    /// <returns>
    /// true if archive is written with no error.
    /// false if errors occur.
    /// </returns>
    /// <since>5.9</since>
    public bool Write(string path, File3dmWriteOptions options)
    {
      options = options ?? new File3dmWriteOptions();

      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_WriteFile(ptr_this, path, options.Version,
        (uint)options.RenderMeshFlags, 
        (uint)options.AnalysisMeshFlags, 
        options.SaveUserData, IntPtr.Zero);
    }

    /// <summary>
    /// Writes contents of this model to an openNURBS archive.
    /// If the model is not valid, then Write will refuse to write it.
    /// </summary>
    /// <param name="path">The file name to use for writing.</param>
    /// <param name="version">
    /// Version of the openNURBS archive to write.  Must be [2; current version].
    /// Rhino can read its current version, plus earlier file versions except 1.
    /// Use latest version when possible.
    /// <para>Alternatively, 0 is a placeholder for the last valid version.</para>
    /// </param>
    /// <param name="errorLog">This argument will be filled by out reference.</param>
    /// <returns>
    /// true if archive is written with no error.
    /// false if errors occur.
    /// </returns>
    /// <since>5.0</since>
    public bool WriteWithLog(string path, int version, out string errorLog)
    {
      return WriteWithLog(path, new File3dmWriteOptions { Version = version }, out errorLog);
    }

    /// <summary>
    /// Writes contents of this model to an openNURBS archive.
    /// If the model is not valid, then Write will refuse to write it.
    /// </summary>
    /// <param name="path">The file name to use for writing.</param>
    /// <param name="options">An options instance, or null for default.</param>
    /// <param name="errorLog">This argument will be filled by out reference.</param>
    /// <returns>
    /// true if archive is written with no error.
    /// false if errors occur.
    /// </returns>
    /// <since>6.0</since>
    public bool WriteWithLog(string path, File3dmWriteOptions options, out string errorLog)
    {
      options = options ?? new File3dmWriteOptions();

      using (var sh = new StringHolder())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.ONX_Model_WriteFile(
          ptr_const_this,
          path,
          options.Version,
          (uint)options.RenderMeshFlags,
          (uint)options.AnalysisMeshFlags,
          options.SaveUserData,
          ptr_string);
        errorLog = sh.ToString();
        return rc;
      }
    }

    /// <summary>
    /// Write to an in-memory byte[]
    /// </summary>
    /// <returns></returns>
    /// <since>7.1</since>
    public byte[] ToByteArray()
    {
      return ToByteArray(null);
    }

    /// <summary>
    /// Write to an in-memory byte[]
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <since>7.1</since>
    public byte[] ToByteArray(File3dmWriteOptions options)
    {
      options = options ?? new File3dmWriteOptions();
      int rhino_version = options.Version;
      if (rhino_version > 4 && rhino_version < 50)
        rhino_version *= 10;
      IntPtr ptrBufferArchive = UnsafeNativeMethods.ON_WriteBufferArchive_NewMemoryWriter(rhino_version);
      IntPtr constPtrThis = ConstPointer();
      bool success = UnsafeNativeMethods.ONX_Model_ToByteArray(constPtrThis, ptrBufferArchive, rhino_version, (uint)options.RenderMeshFlags, (uint)options.AnalysisMeshFlags, options.SaveUserData);
      byte[] bytearray = null;
      if(success)
      {
        int sz = (int)UnsafeNativeMethods.ON_WriteBufferArchive_SizeOfArchive(ptrBufferArchive);
        IntPtr pByteArray = UnsafeNativeMethods.ON_WriteBufferArchive_Buffer(ptrBufferArchive);
        bytearray = new byte[sz];
        System.Runtime.InteropServices.Marshal.Copy(pByteArray, bytearray, 0, sz);
      }
      UnsafeNativeMethods.ON_WriteBufferArchive_Delete(ptrBufferArchive);
      return bytearray;
    }

    /// <summary>
    /// The File3dm object is kept consistent during its creation.
    /// Therefore, this function now returns only true.
    /// </summary>
    /// <param name="errors">
    /// No errors are found.
    /// </param>
    /// <returns>true in any case.</returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("IsValid now returns always true.")]
    public bool IsValid(out string errors)
    {
      errors = null;
      return true;
    }

    /// <summary>
    /// The File3dm object is kept consistent during its creation.
    /// Therefore, this function now returns only true.
    /// </summary>
    /// <param name="errors">
    /// No errors are found.
    /// </param>
    /// <returns>>true in any case.</returns>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("IsValid now returns always true.")]
    public bool IsValid(TextLog errors)
    {
      string _;
      return IsValid(out _);
    }

    /// <summary>
    /// This function is only kept for forward assembly compatibility.
    /// </summary>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Polish and Audit functionality no longer exist.")]
    public void Polish()
    {
    }

    /// <summary>
    /// This function is only kept for forward assembly compatibility.
    /// </summary>
    /// <param name="attemptRepair">
    /// Ignored.
    /// </param>
    /// <param name="repairCount">Is set to 0.</param>
    /// <param name="errors">
    /// Contains no meaningful error.
    /// </param>
    /// <param name="warnings">Is set to null.
    /// </param>
    /// <returns>
    /// Returns 0.
    /// </returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Polish and Audit functionality no longer exist.")]
    public int Audit(bool attemptRepair, out int repairCount, out string errors, out int[] warnings)
    {
      repairCount = 0;
      if (attemptRepair)
        errors = "Audit functionality is disabled.";
      else
        errors = null;
      warnings = null;
      return 0;
    }

    //int m_3dm_file_version;
    //int m_3dm_opennurbs_version;

    /// <summary>
    /// Gets or sets the start section comments, which are the comments with which the 3dm file begins.
    /// </summary>
    /// <since>5.0</since>
    public string StartSectionComments
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ONX_Model_GetStartSectionComments(ptr_const_this, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ONX_Model_SetStartSectionComments(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets the 3dm file archive version.
    /// </summary>
    /// <since>7.9</since>
    public int ArchiveVersion
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetArchiveVersion(ptr_const_this);
      }
    }

    File3dmNotes m_notes;
    /// <summary>
    /// Gets or sets the model notes.
    /// </summary>
    /// <since>5.0</since>
    public File3dmNotes Notes
    {
      get
      {
        return m_notes ?? (m_notes = new File3dmNotes(this));
      }
      set
      {
        value.SetParent(this);
        m_notes = value;
      }
    }

    const int idxApplicationName = 0;
    const int idxApplicationUrl = 1;
    const int idxApplicationDetails = 2;
    const int idxCreatedBy = 3;
    const int idxLastCreatedBy = 4;

    string GetString(int which)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_GetString(ptr_const_this, which, ptr_string);
        return sh.ToString();
      }
    }
    void SetString(int which, string val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ONX_Model_SetString(ptr_this, which, val);
    }

    /// <summary>
    /// Gets or sets the name of the application that wrote this file.
    /// </summary>
    /// <since>5.0</since>
    public string ApplicationName
    {
      get { return GetString(idxApplicationName); }
      set { SetString(idxApplicationName, value); }
    }

    /// <summary>
    /// Gets or sets a URL for the application that wrote this file.
    /// </summary>
    /// <since>5.0</since>
    public string ApplicationUrl
    {
      get { return GetString(idxApplicationUrl); }
      set { SetString(idxApplicationUrl, value); }
    }

    /// <summary>
    /// Gets or sets details for the application that wrote this file.
    /// </summary>
    /// <since>5.0</since>
    public string ApplicationDetails
    {
      get { return GetString(idxApplicationDetails); }
      set { SetString(idxApplicationDetails, value); }
    }

    /// <summary>
    /// Gets a string that names the user who created the file.
    /// </summary>
    /// <since>5.0</since>
    public string CreatedBy
    {
      get { return GetString(idxCreatedBy); }
    }

    /// <summary>
    /// Gets a string that names the user who last edited the file.
    /// </summary>
    /// <since>5.0</since>
    public string LastEditedBy
    {
      get { return GetString(idxLastCreatedBy); }
    }

    /// <summary>
    /// Get the DateTime that this file was originally created. If the
    /// value is not set in the 3dm file, then DateTime.MinValue is returned
    /// </summary>
    /// <since>5.6</since>
    public DateTime Created
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_revhist = UnsafeNativeMethods.ONX_Model_RevisionHistory(ptr_const_this);
        int second = 0, minute = 0, hour = 0, month = 0, day = 0, year = 0;
        if (UnsafeNativeMethods.ON_3dmRevisionHistory_GetDate(ptr_revhist, true, ref second, ref minute, ref hour, ref day, ref month, ref year))
          return new DateTime(year, month, day, hour, minute, second);
        return DateTime.MinValue;
      }
    }

    /// <summary>
    /// Get the DateTime that this file was last edited. If the
    /// value is not set in the 3dm file, then DateTime.MinValue is returned
    /// </summary>
    /// <since>5.6</since>
    public DateTime LastEdited
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_revhist = UnsafeNativeMethods.ONX_Model_RevisionHistory(ptr_const_this);
        int second = 0, minute = 0, hour = 0, month = 0, day = 0, year = 0;
        if (UnsafeNativeMethods.ON_3dmRevisionHistory_GetDate(ptr_revhist, false, ref second, ref minute, ref hour, ref day, ref month, ref year))
          return new DateTime(year, month, day, hour, minute, second);
        return DateTime.MinValue;
      }
    }

    /// <summary>
    /// Gets or sets the revision number.
    /// </summary>
    /// <since>5.0</since>
    public int Revision
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetRevision(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ONX_Model_SetRevision(ptr_this, value);
      }
    }

#if RHINO_SDK
    /// <summary> Preview image used for file explorer </summary>
    /// <since>6.0</since>
    public System.Drawing.Bitmap GetPreviewImage()
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var dib = new RhinoDib())
      {
        IntPtr ptr_dib = dib.NonConstPointer;
        if( UnsafeNativeMethods.ONX_Model_GetPreviewImage(const_ptr_this, ptr_dib))
          return dib.ToBitmap();
      }
      return null;
    }

    /// <summary> Preview image used for file explorer </summary>
    /// <since>6.0</since>
    public void SetPreviewImage(System.Drawing.Bitmap image)
    {
      IntPtr ptr_this = NonConstPointer();
      using (var dib = RhinoDib.FromBitmap(image))
      {
        IntPtr const_ptr_dib = dib.ConstPointer;
        UnsafeNativeMethods.ONX_Model_SetPreviewImage(ptr_this, const_ptr_dib);
      }
    }
#endif

    File3dmSettings m_settings;

    /// <summary>
    /// Settings include tolerance, and unit system, and defaults used
    /// for creating views and objects.
    /// </summary>
    /// <since>5.0</since>
    public File3dmSettings Settings
    {
      get
      {
        return m_settings ?? (m_settings = new File3dmSettings(this));
      }
    }

    /// <summary>
    /// Retrieves the manifest with all object descriptions in this file.
    /// </summary>
    /// <since>6.0</since>
    public ManifestTable Manifest
    {
      get { return m_manifest_table ?? (m_manifest_table = new File3dmManifestTable(this)); }
    }

    /// <summary>
    /// Gets access to the <see cref="File3dmObjectTable"/> class associated with this file,
    /// which contains all objects.
    /// </summary>
    /// <since>5.0</since>
    public File3dmObjectTable Objects
    {
      get { return m_object_table ?? (m_object_table = new File3dmObjectTable(this)); }
    }

    /// <summary>
    /// Materials in this file.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use the new AllMaterials property")]
    public IList<Material> Materials
    {
      get { return m_material_table ?? (m_material_table = new File3dmMaterialTable(this)); }
    }

    /// <summary>
    /// Materials in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmMaterialTable AllMaterials
    {
      get { return m_material_table ?? (m_material_table = new File3dmMaterialTable(this)); }
    }

    /// <summary>
    /// Linetypes in this file.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use the new AllLinetypes property")]
    public IList<Linetype> Linetypes
    {
      get { return m_linetype_table ?? (m_linetype_table = new File3dmLinetypeTable(this)); }
    }

    /// <summary>
    /// Linetypes in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmLinetypeTable AllLinetypes
    {
      get { return m_linetype_table ?? (m_linetype_table = new File3dmLinetypeTable(this)); }
    }

    /// <summary>
    /// Layers in this file.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use the new AllLayers property")]
    public IList<Layer> Layers
    {
      get { return m_layer_table ?? (m_layer_table = new File3dmLayerTable(this)); }
    }

    /// <summary>
    /// Layers in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmLayerTable AllLayers
    {
      get { return m_layer_table ?? (m_layer_table = new File3dmLayerTable(this)); }
    }

    /// <summary>
    /// Groups in this file.
    /// </summary>
    /// <since>6.5</since>
    public File3dmGroupTable AllGroups
    {
      get { return m_group_table ?? (m_group_table = new File3dmGroupTable(this)); }
    }

    /// <summary>
    /// Dimension Styles in this file.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use the new AllDimStyles property")]
    public IList<DimensionStyle> DimStyles
    {
      get { return m_dimstyle_table ?? (m_dimstyle_table = new File3dmDimStyleTable(this)); }
    }

    /// <summary>
    /// Dimension Styles in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmDimStyleTable AllDimStyles
    {
      get { return m_dimstyle_table ?? (m_dimstyle_table = new File3dmDimStyleTable(this)); }
    }

    /// <summary>
    /// Hatch patterns in this file
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use the new AllHatchPatterns property")]
    public IList<HatchPattern> HatchPatterns
    {
      get { return m_hatchpattern_table ?? (m_hatchpattern_table = new File3dmHatchPatternTable(this)); }
    }

    /// <summary>
    /// Hatch patterns in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmHatchPatternTable AllHatchPatterns
    {
      get { return m_hatchpattern_table ?? (m_hatchpattern_table = new File3dmHatchPatternTable(this)); }
    }

    /// <summary>
    /// Instance definitions in this file.
    /// </summary>
    /// <since>5.6</since>
    [Obsolete("Use the new AllInstanceDefinitions property")]
    public IList<InstanceDefinitionGeometry> InstanceDefinitions
    {
      get
      {
        return m_instance_definition_table ?? (m_instance_definition_table = new File3dmInstanceDefinitionTable(this));
      }
    }

    /// <summary>
    /// Instance definitions in this file
    /// </summary>
    /// <since>6.0</since>
    public File3dmInstanceDefinitionTable AllInstanceDefinitions
    {
      get
      {
        return m_instance_definition_table ?? (m_instance_definition_table = new File3dmInstanceDefinitionTable(this));
      }
    }

    /// <summary>
    /// Views that represent the RhinoViews which are displayed when Rhino loads this file.
    /// </summary>
    /// <since>5.0</since>
    public IList<ViewInfo> Views
    {
      get { return m_view_table ?? (m_view_table = new File3dmViewTable(this, false)); }
    }

    /// <summary>
    /// Views that represent the RhinoViews which are displayed when Rhino loads this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmViewTable AllViews
    {
      get { return m_named_view_table ?? (m_named_view_table = new File3dmViewTable(this, false)); }
    }

    /// <summary>
    /// Named views in this file.
    /// </summary>
    /// <since>5.0</since>
    public IList<ViewInfo> NamedViews
    {
      get { return m_named_view_table ?? (m_named_view_table = new File3dmViewTable(this, true)); }
    }

    /// <summary>
    /// Named views in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmViewTable AllNamedViews
    {
      get { return m_named_view_table ?? (m_named_view_table = new File3dmViewTable(this, true)); }
    }

    /// <summary>
    /// Named construction planes in this file.
    /// </summary>
    /// <since>6.0</since>
    public IList<ConstructionPlane> NamedConstructionPlanes
    {
      get
      {
        // 27-Sep-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-41623
        // I'm only doing this because to be consistent with what is done above.
        return m_named_cplanes ?? (m_named_cplanes = new File3dmNamedConstructionPlanes(this));
      }
    }

    /// <summary>
    /// Named construction planes in this file.
    /// </summary>
    /// <since>6.0</since>
    public File3dmNamedConstructionPlanes AllNamedConstructionPlanes
    {
      get
      {
        // 27-Sep-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-41623
        return m_named_cplanes ?? (m_named_cplanes = new File3dmNamedConstructionPlanes(this)); 
      }
    }

    /// <summary>
    /// Custom plug-in data in this file.  This data is not attached to any geometry or attributes
    /// </summary>
    /// <since>5.0</since>
    public File3dmPlugInDataTable PlugInData
    {
      get { return m_userdata_table ?? (m_userdata_table = new File3dmPlugInDataTable(this)); }
    }

    /// <summary>
    /// Document user strings in this file
    /// </summary>
    /// <since>6.0</since>
    public File3dmStringTable Strings
    {
      get { return m_string_table ?? (m_string_table = new File3dmStringTable(this)); }
    }

    /// <summary>
    /// The embedded files in this file.
    /// </summary>
    /// <since>8.0</since>
    public File3dmEmbeddedFiles EmbeddedFiles
    {
      get { return m_embedded_files ?? (m_embedded_files = new File3dmEmbeddedFiles(this)); }
    }

    /// <summary>
    /// The render materials in this file.
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderMaterials RenderMaterials
    {
      get { return m_render_materials ?? (m_render_materials = new File3dmRenderMaterials(this)); }
    }

    /// <summary>
    /// The render environments in this file.
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderEnvironments RenderEnvironments
    {
      get { return m_render_environments ?? (m_render_environments = new File3dmRenderEnvironments(this)); }
    }

    /// <summary>
    /// The render textures in this file.
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderTextures RenderTextures
    {
      get { return m_render_textures ?? (m_render_textures = new File3dmRenderTextures(this)); }
    }

#region diagnostic dumps
    const int idxDumpAll = 0;
    const int idxDumpSummary = 1;

    internal const int idxUserDataTable = 15;
    internal const int idxViewTable = 16;
    internal const int idxNamedViewTable = 17;
    internal string Dump(int which)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pConstThis = ConstPointer();
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_Dump(pConstThis, which, pString);
        return sh.ToString();
      }
    }

    internal string Dump(ModelComponentType type)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pConstThis = ConstPointer();
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_Dump3(pConstThis, type, pString);
        return sh.ToString();
      }
    }


    /// <summary>Prepares a text dump of the entire model.</summary>
    /// <returns>The text dump.</returns>
    /// <since>5.0</since>
    public string Dump()
    {
      return Dump(idxDumpAll);
    }

    /// <summary>Prepares a text dump of model properties and settings.</summary>
    /// <returns>The text dump.</returns>
    /// <since>5.0</since>
    public string DumpSummary()
    {
      return Dump(idxDumpSummary);
    }

    /// <summary>Prepares a text dump of the entire model.</summary>
    /// <param name="log"></param>
    /// <since>5.1</since>
    public void DumpToTextLog(TextLog log)
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pTextLog = log.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_Dump2(pConstThis, pTextLog);
    }
    /*
    /// <summary>text dump of bitmap table.</summary>
    /// <returns>-</returns>
    public string DumpBitmapTable()
    {
      return Dump(idxDumpBitmapTable);
    }

    /// <summary>text dump of texture mapping table.</summary>
    /// <returns>-</returns>
    public string DumpTextureMappingTable()
    {
      return Dump(idxDumpTextureMappingTable);
    }

    /// <summary>text dump of render material table.</summary>
    /// <returns>-</returns>
    public string DumpMaterialTable()
    {
      return Dump(idxDumpMaterialTable);
    }

    /// <summary>text dump of line type table.</summary>
    /// <returns>-</returns>
    public string DumpLinetypeTable()
    {
      return Dump(idxDumpLinetypeTable);
    }

    /// <summary>text dump of layer table.</summary>
    /// <returns>-</returns>
    public string DumpLayerTable()
    {
      return Dump(idxDumpLayerTable);
    }

    /// <summary>text dump of light table.</summary>
    /// <returns>-</returns>
    public string DumpLightTable()
    {
      return Dump(idxDumpLightTable);
    }

    /// <summary>text dump of group table.</summary>
    /// <returns>-</returns>
    public string DumpGroupTable()
    {
      return Dump(idxDumpGroupTable);
    }

    /// <summary>text dump of font table.</summary>
    /// <returns>-</returns>
    public string DumpFontTable()
    {
      return Dump(idxDumpFontTable);
    }

    /// <summary>text dump of dimstyle table.</summary>
    /// <returns>-</returns>
    public string DumpDimStyleTable()
    {
      return Dump(idxDumpDimStyleTable);
    }

    /// <summary>text dump of hatch pattern table.</summary>
    /// <returns>-</returns>
    public string DumpHatchPatternTable()
    {
      return Dump(idxDumpHatchPatternTable);
    }

    /// <summary>text dump of instance definition table.</summary>
    /// <returns>-</returns>
    public string DumpIDefTable()
    {
      return Dump(idxDumpIDefTable);
    }

    /// <summary>text dump of history record table.</summary>
    /// <returns>-</returns>
    public string DumpHistoryRecordTable()
    {
      return Dump(idxDumpHistoryRecordTable);
    }

    /// <summary>text dump of user data table.</summary>
    /// <returns>-</returns>
    public string DumpUserDataTable()
    {
      return Dump(idxDumpUserDataTable);
    }
    */
#endregion

#region constructor-dispose logic
    /// <summary>
    /// Initializes a new instance of a 3dm file.
    /// </summary>
    /// <since>5.0</since>
    public File3dm()
    {
      m_ptr = UnsafeNativeMethods.ONX_Model_New();
    }
    private File3dm(IntPtr pONX_Model)
    {
      m_ptr = pONX_Model;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~File3dm() { Dispose(false); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ONX_Model_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }
#endregion
  }

  /// <summary>Options used by File3dm.Write</summary>
  public class File3dmWriteOptions
  {
    /// <summary>
    /// Initializes properties to defaults.
    /// </summary>
    /// <since>5.9</since>
    public File3dmWriteOptions()
    {
#if RHINO_SDK
      Version = RhinoApp.ExeVersion;
      // It's OK not to specify a version. If it is 0,
      // then ON_BinaryArchive::CurrentArchiveVersion() is used.
      // This is here just so that previous behavior is kept unchanged in Rhino.
#endif
      SaveUserData = true;
    }

    /// <summary>
    /// [Giulio, 2016 04 01]
    /// Please keep these in sync with ON_BinaryArchive::m_save_3dm_render_mesh_flags
    /// in opennurbs_archive.h
    /// </summary>
    internal DocObjects.ObjectType RenderMeshFlags = DocObjects.ObjectType.Brep | DocObjects.ObjectType.Extrusion;
    internal DocObjects.ObjectType AnalysisMeshFlags = DocObjects.ObjectType.Brep | DocObjects.ObjectType.Extrusion;

    /// <summary>
    /// <para>File version. Default is major version number of this assembly version.</para>
    /// <para>Must be in range [2; current version].</para>
    /// <para>Alternatively, 0 is a placeholder for the last valid version.</para>
    /// <para>Rhino can read its current version, plus earlier file versions except 1.</para>
    /// <para>Use latest version when possible.</para>
    /// </summary>
    /// <since>5.9</since>
    public int Version { get; set; }

    /// <summary>
    /// Include Render meshes in the file. Default is true
    /// </summary>
    /// <since>5.9</since>
    [Obsolete("Specify analysis meshes for object types individually. Use EnableAnalysisMeshes()")]
    public bool SaveRenderMeshes
    {
      get
      {
        return RenderMeshFlags != 0;
      }
      set
      {
        EnableRenderMeshes(ObjectType.AnyObject, value);
      }
    }

    /// <summary>
    /// Activates saving of render meshes for specific types of objects.
    /// If you do not specify the state for an object type, its default is used.
    /// Specifically, currently SubD mesh saving is disabled by default, while Brep and Extrusion is on.
    /// </summary>
    /// <param name="objectType">The object type. Mostly brep, extrusion and SubD (or their flag combinations) make sense here.
    /// <para>DO NOT specify a 'filter' or sub-object type.</para></param>
    /// <param name="enable">If false, disables saving for this object type.</param>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public void EnableRenderMeshes(ObjectType objectType, bool enable)
    {
      if (enable)
      {
        RenderMeshFlags |= objectType;
      }
      else
      {
        RenderMeshFlags &= (~objectType);
      }
    }

    /// <summary>
    /// Include analysis meshes in the file. Default is true
    /// </summary>
    /// <since>5.9</since>
    [Obsolete("Specify analysis meshes for object types individually. Use EnableAnalysisMeshes()")]
    public bool SaveAnalysisMeshes
    {
      get
      {
        return (AnalysisMeshFlags != 0);
      }
      set
      {
        EnableAnalysisMeshes(ObjectType.AnyObject, value);
      }
    }

    /// <summary>
    /// Activates saving of analysis meshes for specific types of objects.
    /// If you do not specify the state for an object type, its default is used.
    /// Currently SubD mesh saving is disabled by default, while Brep and Extrusion is enabled.
    /// </summary>
    /// <param name="objectType">The object type. Mostly mesh, brep, extrusion and SubD (or their flag combinations) make sense here.
    /// <para>DO NOT specify a 'filter' or sub-object type.</para></param>
    /// <param name="enable">If false, disables saving for this object type.</param>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public void EnableAnalysisMeshes(ObjectType objectType, bool enable)
    {
      if (enable)
      {
        AnalysisMeshFlags |= objectType;
      }
      else
      {
        AnalysisMeshFlags &= (~objectType);
      }
    }

    /// <summary>
    /// Include custom user data in the file. Default is true
    /// </summary>
    /// <since>5.9</since>
    public bool SaveUserData { get; set; }
  }

  /// <summary>
  /// Used to store geometry table object definition and attributes in a File3dm.
  /// </summary>
  public sealed class File3dmObject : ModelComponent, IEquatable<File3dmObject>
  {
    readonly Guid m_id;
    readonly File3dm m_parent;
    GeometryBase m_geometry;
    ObjectAttributes m_attributes;

    internal File3dmObject(Guid id, File3dm parent)
    {
      m_id = id;
      m_parent = parent;
    }

    internal IntPtr GetGeometryConstPointer()
    {
      IntPtr const_ptr_model = m_parent.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ModelObjectGeometry(const_ptr_model, m_id);
    }

    internal IntPtr GetAttributesConstPointer()
    {
      IntPtr const_ptr_model = m_parent.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ModelObjectAttributes(const_ptr_model, m_id);
    }

    internal IntPtr GetAttributesNonConstPointer()
    {
      IntPtr ptr_model = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ModelObjectAttributes(ptr_model, m_id);
    }

    /// <summary>
    /// Gets the geometry that is linked with this document object.
    /// </summary>
    /// <since>5.0</since>
    public GeometryBase Geometry
    {
      get
      {
        IntPtr pGeometry = GetGeometryConstPointer();
        if( m_geometry==null || m_geometry.ConstPointer()!=pGeometry )
          m_geometry = Rhino.Geometry.GeometryBase.CreateGeometryHelper(pGeometry, this);
        return m_geometry;
      }
    }

    /// <summary>
    /// Gets the attributes that are linked with this document object.
    /// </summary>
    /// <since>5.0</since>
    public ObjectAttributes Attributes
    {
      get
      {
        IntPtr pAttributes = GetAttributesConstPointer();
        if (m_attributes == null || m_attributes.ConstPointer() != pAttributes)
          m_attributes = new ObjectAttributes(this);
        return m_attributes;
      }
    }

    /// <summary>
    /// Gets or sets the Name of the object. Equivalent to this.Attributes.Name.
    /// </summary>
    /// <since>5.0</since>
    public override string Name
    {
      get { return Attributes.Name; }
      set { Attributes.Name = value; }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.ModelGeometry"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.ModelGeometry;

    /// <summary>
    /// Verified that two objects refer to the same object in a document.
    /// </summary>
    /// <param name="obj">The other item to test.</param>
    /// <returns>true is the two objects coincide.</returns>
    public override bool Equals(object obj)
    {
      return Equals(obj as File3dmObject);
    }

    /// <summary>
    /// Verified that two File3dmObject items refer to the same object in a document.
    /// </summary>
    /// <param name="other">The other item to test.</param>
    /// <returns>true is the two objects coincide.</returns>
    /// <since>6.0</since>
    public bool Equals(File3dmObject other)
    {
      return other != null && m_id == other.m_id && m_parent == other.m_parent;
    }

    /// <summary>
    /// Provides an hash code for this item.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
      return m_id.GetHashCode() ^ (int)m_parent.ConstPointer().ToInt64();
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      var x_model_const_ptr = m_parent.ConstPointer();
      var const_ptr = UnsafeNativeMethods.ONX_Model_ModelObjectGeometryConstPtrFromId(x_model_const_ptr, m_id);
      return const_ptr;
    }

    /// <summary>
    /// Attempts to read a Rhino plug-in's custom userdata from the <see cref="File3dmObject"/> object.
    /// </summary>
    /// <param name="userDataId">The id of the custom userdata object whose data you want to try to read</param>
    /// <param name="readFromAttributes">
    /// Set true to attempt to read custom userdata object from the object's <see cref="Attributes"/>.
    /// Set false to attempt to read custom userdata object from the object's <see cref="Geometry"/>.
    /// </param>
    /// <param name="dataReader">
    /// The function that will read the data.
    /// This function must be implemented identical to the the originating <see cref="DocObjects.Custom.UserData"/>-inherited class's Read method.
    /// </param>
    /// <returns>The value returned by the data reading function if successful, false otherwise.</returns>
    public bool TryReadUserData(Guid userDataId, bool readFromAttributes, Func<File3dm, BinaryArchiveReader, bool> dataReader)
    {
      if (null == dataReader)
        return false;

      IntPtr ptr_const_model = m_parent.ConstPointer();
      IntPtr ptr_buffer_archive = UnsafeNativeMethods.ONX_Model_ModelGeometry_UserData_NewArchive(ptr_const_model, m_id, userDataId, readFromAttributes);
      if (IntPtr.Zero == ptr_buffer_archive)
        return false;

      BinaryArchiveReader archive = new BinaryArchiveReader(ptr_buffer_archive);
      bool rc = dataReader(m_parent, archive);
      UnsafeNativeMethods.ONX_Model_UserData_DeleteArchive(ptr_buffer_archive);
      return rc;
    }

  }

  /// <summary>
  /// Provides methods to use all File3dm and RhinoDoc tables under the same contract.
  /// Do not derive from this interface. This is to ensure all tables can be used with the same method list.
  /// </summary>
  public interface ICommonComponentTable<T> : IReadOnlyCollection<T>, ICollection<T>
    where T : ModelComponent
  {
    /// <summary>
    /// Retrieves an object based on ID. You should prefer ID search over Index search.
    /// </summary>
    /// <param name="id">The id to search for.</param>
    /// <returns>A model component, or null if none was found.</returns>
    T FindId(Guid id);

    /// <summary>
    /// Retrieves an object based on Name.
    /// </summary>
    /// <param name="nameHash">The name hash for which to search.</param>
    T FindNameHash(NameHash nameHash);

    /// <summary>
    /// Returns the model component type the table handles.
    /// </summary>
    ModelComponentType ComponentType { get; }
  }

  /// <summary>
  /// Provides a base table type that encompasses all document tables, both in RhinoDoc and File3dm.
  /// </summary>
  /// <typeparam name="T">A model component.</typeparam>
  public abstract class CommonComponentTable<T> :
    ICommonComponentTable<T>,
    IList<T>, IReadOnlyList<T> //we implement IList<T> so that we can use this as a base for all File3dm tables that used to return IList<T>s.
    where T : ModelComponent
  {
    internal ManifestTable m_manifest;

    internal CommonComponentTable(ManifestTable manifest)
    {
      m_manifest = manifest;
    }

    // It is important to check whether the new inherited table should actually override this explicit implementation.
    // If the table allows first creation of items and then population, then this should be overridden.
    void ICollection<T>.Add(T item)
    {
      throw new NotSupportedException(
        "This table does not support addition via item. If you think this is an error, email giulio@mcneel.com");
    }

    /// <summary>
    /// Returns the count of all items, including deleted ones.
    /// </summary>
    public virtual int Count
    {
      get
      {
        return m_manifest.ActiveObjectCount(ComponentType);
      }
    }

    /// <summary>
    /// Returns the enumerator that yields all items.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator<T> GetEnumerator()
    {
      return m_manifest.GetEnumerator<T>();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Deletes an item. Items that are deleted are still keeping their space, but the 'IsDeleted' flag is checked.
    /// </summary>
    /// <param name="item">An item to delete.</param>
    /// <returns>True if an items could be deleted (e.g., it was not locked).</returns>
    public abstract bool Delete(T item);

    bool ICollection<T>.Remove(T item)
    {
      return Delete(item);
    }

    /// <summary>
    /// Returns the actual component type of a table.
    /// </summary>
    public abstract ModelComponentType ComponentType { get; }

    /// <summary>
    /// Uses the guid to find a model component. Deleted objects cannot be found by id.
    /// The guid is the value that is stored in the .Id property.
    /// In a single document, no two active objects have the same guid. If an object is
    /// replaced with a new object, then the guid  persists. For example, if the _Move command
    /// moves an object, then the moved object inherits its guid from the starting object.
    /// If the Copy command copies an object, then the copy gets a new guid. This guid persists
    /// through file saving/opening operations. This function will not find grip objects.
    /// </summary>
    /// <param name="id">ID of model component to search for.</param>
    /// <returns>Reference to the rhino object with the objectId or null if no such object could be found.</returns>
    public virtual T FindId(Guid id)
    {
      return (T)m_manifest.FindId(id, ComponentType);
    }

    /// <summary>
    /// This is internal so that we can choose when to deliver the functionality for each table.
    /// Object, history and instance tables have no indices.
    /// </summary>
    internal T __FindIndexInternal(int index)
    {
      return (T)m_manifest.FindIndex(index, ComponentType);
    }

    /// <summary>
    /// Uses the hash of the name to find a model component.
    /// Deleted objects have no name.
    /// </summary>
    /// <param name="nameHash">NameHash of model component to search for.</param>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    T ICommonComponentTable<T>.FindNameHash(NameHash nameHash)
    {
      return (T)m_manifest.FindNameHash(nameHash, ComponentType);
    }

#region explicit IList<T> implementation
    T IList<T>.this[int index]
    {
      get
      {
        return __FindIndexInternal(index);
      }

      set
      {
        throw new NotSupportedException("You cannot set an item by index with this table.");
      }
    }

    T IReadOnlyList<T>.this[int index]
    {
      get
      {
        return __FindIndexInternal(index);
      }
    }

    bool ICollection<T>.IsReadOnly
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Marks all items as deleted.
    /// </summary>
    public void Clear()
    {
      GenericIListImplementation.ClearByItems<T>(this);
    }

    bool ICollection<T>.Contains(T item)
    {
      return m_manifest.FindId(item.Id, ComponentType) != null;
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo<T>(this, array, arrayIndex);
    }

    int IList<T>.IndexOf(T item)
    {
      if (item == null) return -1;

      if (FindId(item.Id) != null)
        return item.Index;
      else
        return -1;
    }

    void IList<T>.Insert(int index, T item)
    {
      throw new NotSupportedException("It is not possible to insert an item at a specific position.");
    }

    void IList<T>.RemoveAt(int index)
    {
      T item = __FindIndexInternal(index);
      if(item != null) Delete(item);
    }
#endregion
  }

  /// <summary>
  /// Provides a base table type that is shared among all File3dm tables.
  /// </summary>
  /// <typeparam name="T">A model component.</typeparam>
  public abstract class File3dmCommonComponentTable<T> : CommonComponentTable<T>, ICollection<T>
    where T : ModelComponent
  {
    internal File3dm m_parent;

    internal File3dmCommonComponentTable(File3dm file) : base(file.Manifest)
    {
      m_parent = file;
    }

    /// <summary>
    /// Flags a component as deleted.
    /// </summary>
    /// <param name="item">The item to flag.</param>
    /// <returns>True on success.</returns>
    public override bool Delete(T item)
    {
      if (item == null) return false;

      IntPtr pParent = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_RemoveModelComponent_Id(pParent, ComponentType, item.Id);
    }

    /// <summary>
    /// Flags a component as deleted.
    /// </summary>
    /// <param name="index">The index of the item to flag.</param>
    /// <returns>True on success.</returns>
    public virtual void Delete(int index)
    {
      IntPtr pParent = m_parent.NonConstPointer();
      if (!UnsafeNativeMethods.ONX_Model_RemoveModelComponent(pParent, ComponentType, index))
      {
        if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Adds an item.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public virtual void Add(T item)
    {
      if (item == null)
        throw new ArgumentNullException("item");

      IntPtr pParent = m_parent.NonConstPointer();
      IntPtr pConstComponent = item.ConstPointer();
      if (!UnsafeNativeMethods.ONX_Model_AddModelComponent(pParent, pConstComponent))
        throw new NotSupportedException("Addition of model component failed.");
    }

    void ICollection<T>.Add(T item)
    {
      Add(item);
    }

    /// <summary>Prepares a text dump of object table.</summary>
    /// <returns>A string containing the dump.</returns>
    public string Dump()
    {
      return m_parent.Dump(ComponentType);
    }
  }

  /// <summary>
  /// Maintains an index to every model component that is in the 3dm file.
  /// This is the "more comprehensive" table that contains all objects in all other tables.
  /// </summary>
  public abstract class ManifestTable :
    ICommonComponentTable<ModelComponent>
  {
    internal ManifestTable() { }

    /// <summary>
    /// Returns the parent object. This is the RhinoDoc, or the File3md file. 
    /// </summary>
    /// <since>6.0</since>
    public abstract object Parent { get; }

    /// <summary>
    /// Uses the guid to find a model component. Deleted objects cannot be found by id.
    /// The guid is the value that is stored in the .Id property.
    /// In a single document, no two active objects have the same guid. If an object is
    /// replaced with a new object, then the guid  persists. For example, if the _Move command
    /// moves an object, then the moved object inherits its guid from the starting object.
    /// If the Copy command copies an object, then the copy gets a new guid. This guid persists
    /// through file saving/opening operations. This function will not find grip objects.
    /// </summary>
    /// <param name="id">ID of model component to search for.</param>
    /// <returns>Reference to the rhino object with the objectId or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public abstract ModelComponent FindId(Guid id);

    /// <summary>
    /// Uses the guid to find a model component. Deleted objects cannot be found by id.
    /// The guid is the value that is stored in the .Id property.
    /// In a single document, no two active objects have the same guid. If an object is
    /// replaced with a new object, then the guid  persists. For example, if the _Move command
    /// moves an object, then the moved object inherits its guid from the starting object.
    /// If the Copy command copies an object, then the copy gets a new guid. This guid persists
    /// through file saving/opening operations. This function will not find grip objects.
    /// </summary>
    /// <param name="id">ID of model component to search for.</param>
    /// <param name="type">The type to be searched. If this is <see cref="ModelComponentType.Unset"/>
    /// then all types are searched.</param>
    /// <returns>Reference to the rhino object with the objectId or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public abstract ModelComponent FindId(Guid id, ModelComponentType type);

    /// <summary>
    /// Uses the guid to find a model component. Deleted objects cannot be found by id.
    /// The guid is the value that is stored in the .Id property.
    /// In a single document, no two active objects have the same guid. If an object is
    /// replaced with a new object, then the guid  persists. For example, if the _Move command
    /// moves an object, then the moved object inherits its guid from the starting object.
    /// If the Copy command copies an object, then the copy gets a new guid. This guid persists
    /// through file saving/opening operations. This function will not find grip objects.
    /// </summary>
    /// <param name="id">Index of model component to search for.</param>
    /// <typeparam name="T">The type, derived from ModelComponent or ModelComponent itself.</typeparam>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public T FindId<T>(Guid id)
      where T : ModelComponent
    {
      var type = GetModelComponentTypeFromGenericType<T>();
      return (T)FindId(id, type);
    }

    /// <summary>
    /// Uses the index to find a model component.
    /// The index is the value that is stored in the .Index property.
    /// </summary>
    /// <param name="index">Index of model component to search for.</param>
    /// <param name="type">The type to be searched. Cannot be <see cref="ModelComponentType.Unset"/>.</param>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public abstract ModelComponent FindIndex(int index, ModelComponentType type);

    /// <summary>
    /// Uses the index to find a model component.
    /// The index is the value that is stored in the .Index property.
    /// </summary>
    /// <param name="index">Index of model component to search for.</param>
    /// <typeparam name="T">The type, derived from ModelComponent. Cannot be ModelComponent itself.</typeparam>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public T FindIndex<T>(int index)
      where T : ModelComponent
    {
      var type = GetModelComponentTypeFromGenericType<T>();
      return (T)FindIndex(index, type);
    }

    /// <summary>
    /// Uses the name to find a model component.
    /// The name is the value that is stored in the .Name property.
    /// Deleted objects have no name.
    /// </summary>
    /// <param name="name">Name of model component to search for.</param>
    /// <param name="type">The type to be searched. Cannot be <see cref="ModelComponentType.Unset"/>.</param>
    /// <param name="parent">Parent object id. This is only required for layers.</param>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public abstract ModelComponent FindName(string name, ModelComponentType type, Guid parent);

    /// <summary>
    /// Uses the name to find a model component.
    /// The name is the value that is stored in the .Name property.
    /// Deleted objects have no name.
    /// </summary>
    /// <typeparam name="T">The type, derived from ModelComponent. Cannot be ModelComponent itself.</typeparam>
    /// <param name="name">Name of model component to search for.</param>
    /// <param name="parent">Parent object id. This is only required for layers.</param>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public T FindName<T>(string name, Guid parent)
      where T : ModelComponent
    {
      var type = GetModelComponentTypeFromGenericType<T>();
      return (T)FindName(name, type, parent);
    }

    /// <summary>
    /// Uses the hash of the name to find a model component.
    /// Deleted objects have no name.
    /// </summary>
    /// <param name="nameHash">NameHash of model component to search for.</param>
    /// <param name="type">The type to be searched. Cannot be <see cref="ModelComponentType.Unset"/>.</param>
    /// <returns>Reference to the rhino object or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public abstract ModelComponent FindNameHash(NameHash nameHash, ModelComponentType type);

    /// <summary>
    /// Uses the hash of the name to find a model component.
    /// Deleted objects have no name.
    /// </summary>
    /// <typeparam name="T">The type, derived from ModelComponent.</typeparam>
    /// <param name="nameHash">Name hash of model component to search for.</param>
    /// <since>6.0</since>
    public T FindNameHash<T>(NameHash nameHash)
      where T : ModelComponent
    {
      var type = GetModelComponentTypeFromGenericType<T>();
      return (T)FindNameHash(nameHash, type);
    }

    internal abstract IntPtr GetConstOnComponentManifestPtr();

    /// <summary>
    /// Total number of items in the manifest, including deleted items.
    /// </summary>
    /// <since>6.0</since>
    public int Count
    {
      get
      {
        IntPtr manifest_const_ptr = GetConstOnComponentManifestPtr();
        return (int)UnsafeNativeMethods.ON_ComponentManifest_ActiveAndDeletedComponentCount(manifest_const_ptr, ModelComponentType.Unset);
      }
    }

    /// <summary>
    /// Total number of items in the manifest, including deleted items.
    /// </summary>
    /// <since>6.0</since>
    public long LongCount
    {
      get
      {
        IntPtr manifest_const_ptr = GetConstOnComponentManifestPtr();
        return (long)UnsafeNativeMethods.ON_ComponentManifest_ActiveAndDeletedComponentCount(manifest_const_ptr, ModelComponentType.Unset);
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.Mixed"/>.
    /// </summary>
    /// <since>6.0</since>
    public ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.Mixed;
      }
    }

    /// <summary>
    /// Total number of items in the manifest, including deleted items.
    /// </summary>
    /// <since>6.0</since>
    public int ActiveObjectCount(ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentOutOfRangeException(nameof(type));

      IntPtr manifest_const_ptr = GetConstOnComponentManifestPtr();
      return (int)UnsafeNativeMethods.ON_ComponentManifest_ActiveAndDeletedComponentCount(manifest_const_ptr, type);
    }

    /// <since>6.0</since>
    ModelComponent ICommonComponentTable<ModelComponent>.FindNameHash(NameHash nameHash)
    {
      throw new NotSupportedException("You can search by NameHash and without type only in a typed manifest table.");
    }

    /// <since>6.0</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Visits all model components in the document, including default ones.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <since>6.0</since>
    public virtual IEnumerator<T> GetEnumerator<T>()
      where T : ModelComponent
    {
      var type = GetModelComponentTypeFromGenericType<T>();

      var enumer = GetEnumerator(type);
      while (enumer.MoveNext())
      {
        yield return (T)enumer.Current;
      }
    }

    /// <summary>
    /// Returns the result of the ComponentType property of a ModelComponent.
    /// </summary>
    /// <typeparam name="T">A model component type.</typeparam>
    /// <returns>A <see cref="ModelComponentType"/>.</returns>
    /// <since>6.0</since>
    public static ModelComponentType GetModelComponentTypeFromGenericType<T>()
      where T : ModelComponent
    {
      if (typeof(T) == typeof(ModelComponent))
        return ModelComponentType.Unset;

      if (typeof(T) == typeof(DimensionStyle))
        return ModelComponentType.DimStyle;

      if (typeof(T) == typeof(BitmapEntry))
        return ModelComponentType.Image;
      if (typeof(T) == typeof(Render.TextureMapping))
        return ModelComponentType.TextureMapping;
      if (typeof(T) == typeof(Material))
        return ModelComponentType.RenderMaterial;
      if (typeof(T) == typeof(Linetype))
        return ModelComponentType.LinePattern;
      if (typeof(T) == typeof(Layer))
        return ModelComponentType.Layer;
      if (typeof(T) == typeof(Group))
        return ModelComponentType.Group;
      //if (typeof(T) == typeof(TextStyle))
      //  return ModelComponentType.TextStyle;

      if (typeof(T) == typeof(Font))
        return ModelComponentType.TextStyle;

#if RHINO_SDK
      if (typeof(T) == typeof(LightObject))
        return ModelComponentType.RenderLight;
#endif

      if (typeof(T) == typeof(HatchPattern))
        return ModelComponentType.HatchPattern;

#if RHINO_SDK
      if (typeof(T) == typeof(InstanceDefinition))
        return ModelComponentType.InstanceDefinition;
#endif
      if (typeof(T) == typeof(InstanceDefinitionGeometry))
        return ModelComponentType.InstanceDefinition;

      if (typeof(T) == typeof(File3dmObject))
        return ModelComponentType.ModelGeometry;

#if RHINO_SDK
      if (typeof(RhinoObject).IsAssignableFrom(typeof(T)))
        return ModelComponentType.ModelGeometry;

      if (typeof(T) == typeof(ReplayHistoryData))//impossible right now
        return ModelComponentType.HistoryRecord;
#endif

      throw new NotSupportedException(
        string.Format(
          "Error in GetModelComponentTypeFromGenericType: {0} not specified.",
          typeof(T).FullName
        ));
    }

    /// <summary>
    /// Visits all model components in the document, including default ones.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <since>6.0</since>
    public virtual IEnumerator<ModelComponent> GetEnumerator()
    {
      for(ModelComponentType type = ModelComponentType.Unset + 1; type < ModelComponentType.NumOf; type++)
      {
        var enumer = GetEnumerator(type);
        while (enumer.MoveNext()) yield return enumer.Current;
      }
    }

    /// <summary>
    /// Returns an enumerators that yields all model components, including default ones,
    /// relating to a particular type.
    /// </summary>
    /// <param name="type">The model component type.</param>
    /// <returns>An enumerator.</returns>
    /// <since>6.0</since>
    public abstract IEnumerator<ModelComponent> GetEnumerator(ModelComponentType type);

    /// <summary>
    /// Marks all items as deleted.
    /// </summary>
    /// <since>6.0</since>
    public virtual void Clear()
    {
      GenericIListImplementation.ClearByItems<ModelComponent>(this);
    }

    /// <summary>
    /// Determines if an items is contained in this table.
    /// </summary>
    /// <param name="item">An item, or null. Null is never contained.</param>
    /// <returns>True if the item is contained; otherwise, false.</returns>
    /// <since>6.0</since>
    public bool Contains(ModelComponent item)
    {
      if (item == null) return false;

      return FindId(item.Id) != null;
    }

    /// <summary>
    /// Copies the content of this table to an array.
    /// </summary>
    /// <param name="array">The array to copy to.</param>
    /// <param name="arrayIndex">The position in the array from which to start copying.</param>
    /// <since>6.0</since>
    public void CopyTo(ModelComponent[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyToFromReadOnlyCollection<ModelComponent>(this, array, arrayIndex);
    }

    bool ICollection<ModelComponent>.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    void ICollection<ModelComponent>.Add(ModelComponent item)
    {
      throw new NotSupportedException("You cannot add directly to the manifest. Add to a specific table instead.");
    }

    bool ICollection<ModelComponent>.Remove(ModelComponent item)
    {
      throw new NotSupportedException("You cannot add directly to the manifest. Add to a specific table instead.");
    }
  }


  internal class File3dmManifestTable : ManifestTable
  {
    readonly File3dm m_parent;
    internal File3dmManifestTable(File3dm parent)
    {
      m_parent = parent;
    }

    public override ModelComponent FindId(Guid id)
    {
      return FindId(id, ModelComponentType.Unset);
    }

    public override ModelComponent FindId(Guid id, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentOutOfRangeException("type");

      IntPtr file3dm_ptr = m_parent.ConstPointer();
      int index = RhinoMath.UnsetIntIndex;
      IntPtr ptr_comp = UnsafeNativeMethods.ONX_Model_AnyTable_FindId(file3dm_ptr, ref type, id, ref index);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_parent, type, id, index);
    }

    public override ModelComponent FindIndex(int index, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentOutOfRangeException("type");

      if (ModelComponentType.Unset == type || ModelComponentType.Mixed == type)
        throw new ArgumentOutOfRangeException("type", "type cannot be Unset or Mixed.");

      IntPtr file3dm_ptr = m_parent.ConstPointer();
      Guid id = default(Guid);
      IntPtr ptr_comp = UnsafeNativeMethods.ONX_Model_AnyTable_FindIndex(file3dm_ptr, type, index, ref id);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_parent, type, id, index);
    }

    public override ModelComponent FindName(string name, ModelComponentType type, Guid parentId)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentOutOfRangeException("type");

      if (ModelComponentType.Unset == type || ModelComponentType.Mixed == type)
        throw new ArgumentOutOfRangeException("type", "type cannot be Unset.");

      if (!ModelComponent.ModelComponentTypeRequiresUniqueName(type))
        throw new ArgumentOutOfRangeException("type", "Cannot use a non-unique name for the search. This type does not require uniqueness.");

      IntPtr file3dm_ptr = m_parent.ConstPointer();
      Guid id = default(Guid);
      int index = default(int);
      IntPtr ptr_comp = UnsafeNativeMethods.ONX_Model_AnyTable_FindName(file3dm_ptr, type, parentId, name, ref index, ref id);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_parent, type, id, index);
    }

    public override ModelComponent FindNameHash(NameHash nameHash, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentOutOfRangeException("type");

      if (ModelComponentType.Unset == type || ModelComponentType.Mixed == type)
        throw new ArgumentOutOfRangeException("type", "type cannot be Unset.");

      if (!ModelComponent.ModelComponentTypeRequiresUniqueName(type))
        throw new ArgumentOutOfRangeException("type", "Cannot use a non-unique name for the search. This type does not require uniqueness.");

      if (nameHash == null) throw new ArgumentNullException("nameHash");

      IntPtr file3dm_ptr = m_parent.ConstPointer();
      Guid id = default(Guid);
      int index = default(int);

      IntPtr ptr_comp;
      using (var nameHashPtr = nameHash.GetDisposableHandle())
      {
        ptr_comp = UnsafeNativeMethods.ONX_Model_AnyTable_FindNameHash(file3dm_ptr, type, nameHashPtr, ref index, ref id);
      }

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_parent, type, id, index);
    }

    internal static ModelComponent Instantiate(File3dm parent, ModelComponentType type, Guid id, int index)
    {
      switch (type)
      {
      case ModelComponentType.Unset:
      case ModelComponentType.Mixed:
        throw new NotImplementedException("ModelComponentType must be a concrete type.");

      case ModelComponentType.Image:
        return new BitmapEntry(index, parent);

      case ModelComponentType.RenderMaterial:
        return new Material(id, parent);

      case ModelComponentType.LinePattern:
        return new Linetype(id, parent);

      case ModelComponentType.Layer:
        return new Layer(id, parent);

      case ModelComponentType.Group:
        return new Group(id, parent);

      //case ModelComponentType.TextStyle:
      //  return new TextStyle(id, parent);

      case ModelComponentType.DimStyle:
        return new DimensionStyle(id, parent);

      case ModelComponentType.RenderLight:
        return new File3dmObject(id, parent);

      case ModelComponentType.HatchPattern:
        return new HatchPattern(id, parent);

      case ModelComponentType.InstanceDefinition:
        return new InstanceDefinitionGeometry(id, parent);

      case ModelComponentType.ModelGeometry:
        return new File3dmObject(id, parent);

      case ModelComponentType.EmbeddedFile:
        return new File3dmEmbeddedFile(id, parent);

      case ModelComponentType.RenderContent:
        return NewFile3dmRenderContent(parent, id);

      case ModelComponentType.TextureMapping:
      case ModelComponentType.HistoryRecord: // Not yet ON_ModelComponent derived
        // Continues to default...
      default:
        throw new NotImplementedException(
          string.Format("Tell giulio@mcneel.com if you need access to this ModelComponentType: {0}.",
            type.ToString()));
      }
    }

    internal static File3dmRenderContent NewFile3dmRenderContent(File3dm parent, Guid id)
    {
      IntPtr model = parent.ConstPointer();

      switch (UnsafeNativeMethods.ONX_Model_GetFile3dmRenderContentKind(model, id))
      {
      case 0: return new File3dmRenderMaterial(id, parent);
      case 1: return new File3dmRenderEnvironment(id, parent);
      case 2: return new File3dmRenderTexture(id, parent);
      }

      return null;
    }

    internal override IntPtr GetConstOnComponentManifestPtr()
    {
      IntPtr file_3dm_ptr = m_parent.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_GetConstOnComponentManifestPtr(file_3dm_ptr);
    }

    public override object Parent
    { get { return m_parent;} }

    public override IEnumerator<ModelComponent> GetEnumerator(ModelComponentType type)
    {
      IntPtr model_ptr = m_parent.ConstPointer();
      IntPtr iteratorPtr = UnsafeNativeMethods.ONX_ModelComponentIterator_New(model_ptr, type);

      if (iteratorPtr != IntPtr.Zero)
      {
        try
        {
          int index = 0; Guid id = Guid.Empty; ModelComponentType outType = ModelComponentType.Unset;

          while (true)
          {
            IntPtr component_ptr = UnsafeNativeMethods.ONX_ModelComponentIterator_GetNext(iteratorPtr, ref index, ref outType, ref id);

            if (component_ptr == IntPtr.Zero) break;
            if (outType != type) throw new ArgumentOutOfRangeException("type");

            yield return Instantiate(m_parent, type, id, index);
          }
        }
        finally
        {
          UnsafeNativeMethods.ONX_ModelComponentIterator_Delete(iteratorPtr);
        }
      }
    }
  }

  // Can't add a cref to an XML comment here since the ObjectTable is not included in the
  // OpenNURBS flavor build of RhinoCommon

  /// <summary>
  /// Represents a simple object table for a file that is open externally.
  /// <para>This class mimics Rhino.DocObjects.Tables.ObjectTable while providing external access to the file.</para>
  /// </summary>
  public class File3dmObjectTable :
    File3dmCommonComponentTable<File3dmObject>,
    IEnumerable<File3dmObject>
  {
    internal File3dmObjectTable(File3dm parent) : base(parent)
    {
      m_parent = parent;
    }

#region properties

    /// <summary>
    /// Returns <see cref="ModelComponentType.ModelGeometry"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.ModelGeometry;
      }
    }

#endregion

#region methods

    /// <summary>
    /// Finds all File3dmObject that are in a given layer.
    /// </summary>
    /// <param name="layer">Layer to search.</param>
    /// <returns>
    /// Array of objects that belong to the specified layer or empty array if no objects could be found.
    /// </returns>
    /// <since>5.0</since>
    public File3dmObject[] FindByLayer(string layer)
    {
      var layer_item = m_parent.AllLayers.FindName(layer, Guid.Empty);
      if (layer_item == null) return new File3dmObject[0];

      return FindByLayer(layer_item);
    }

    /// <summary>
    /// Finds all File3dmObject that are in a given layer.
    /// </summary>
    /// <param name="layer">A layer instance.</param>
    /// <returns>Array of objects that belong to the specified layer or empty array if no objects could be found.</returns>
    /// <exception cref="ArgumentNullException">If layer is null.</exception>
    /// <since>6.0</since>
    public File3dmObject[] FindByLayer(Layer layer)
    {
      if (layer == null) throw new ArgumentNullException("layer");

      int layer_index = layer.Index;
      var list = new List<File3dmObject>();
      foreach (var obj in this)
      {
        if (obj.Attributes.LayerIndex == layer_index)
        {
          list.Add(obj);
        }
      }
      return list.ToArray();
    }

    /// <summary>
    /// Finds all File3dmObject that are in a given group.
    /// </summary>
    /// <param name="group">A group instance.</param>
    /// <returns>Array of objects that belong to the specified group or empty array if no objects could be found.</returns>
    /// <exception cref="ArgumentNullException">If group is null.</exception>
    /// <since>6.20</since>
    public File3dmObject[] FindByGroup(Group group)
    {
      if (group == null)
        throw new ArgumentNullException(nameof(group));

      var group_index = group.Index;
      var list = new List<File3dmObject>();
      foreach (var obj in m_parent.Objects)
      {
        if (obj.Attributes.GetGroupList().Contains(group_index))
          list.Add(obj);
      }
      return list.Count > 0 ? list.ToArray() : new File3dmObject[0];
    }

    /// <summary>Gets the bounding box containing every object in this table.</summary>
    /// <returns>The computed bounding box.</returns>
    /// <since>5.0</since>
    public Rhino.Geometry.BoundingBox GetBoundingBox()
    {
      Rhino.Geometry.BoundingBox bbox = new Geometry.BoundingBox();
      IntPtr pConstModel = m_parent.ConstPointer();
      UnsafeNativeMethods.ONX_Model_BoundingBox(pConstModel, ref bbox);
      return bbox;
    }
#endregion

#region Object addition

    /// <summary>
    /// Duplicates the object, then adds a copy of the object to the document.
    /// </summary>
    /// <param name="item">The item to duplicate and add.</param>
    /// <since>6.0</since>
    public override void Add(File3dmObject item)
    {
      // 14-Oct-2020 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-61014
      // ONX_Model::AddModelComponent make a copy of the input object. So, making
      // a copy here just causes a memory leak.
      //bool mem;
      //IntPtr ptr_item = item._InternalDuplicate(out mem);
      IntPtr ptr_item = item.ConstPointer();
      IntPtr parent = m_parent.NonConstPointer();
      if (!UnsafeNativeMethods.ONX_Model_AddModelComponent(parent, ptr_item))
        throw new NotSupportedException("Addition of model component failed.");
    }

    /// <summary>
    /// Duplicates the object, then adds a copy of the object to the document.
    /// </summary>
    /// <param name="item">The item to duplicate and add.</param>
    /// <param name="attributes">The attributes to link with geometry.</param>
    /// <since>6.0</since>
    public Guid Add(GeometryBase item, ObjectAttributes attributes)
    {
      switch(item.ObjectType)
      {
        case ObjectType.Annotation:
          if (item is AngularDimension ad) return AddAngularDimension(ad, attributes);
          else if (item is LinearDimension ld) return AddLinearDimension(ld, attributes);
          break;
        case ObjectType.Brep:
          return AddBrep((Brep)item, attributes);
        case ObjectType.Curve:
          return AddCurve((Curve)item, attributes);
        case ObjectType.Extrusion:
          return AddExtrusion((Extrusion)item, attributes);
        case ObjectType.Hatch:
          return AddHatch((Hatch)item, attributes);
        case ObjectType.InstanceReference:
          return AddInstanceObject((InstanceReferenceGeometry)item, attributes);
        case ObjectType.Mesh:
          return AddMesh((Mesh)item, attributes);
        case ObjectType.Point:
          return AddPoint(((Point)item).Location, attributes);
        case ObjectType.PointSet:
          return AddPointCloud((PointCloud)item, attributes);
        case ObjectType.SubD:
          return AddSubD((SubD)item, attributes);
        case ObjectType.Surface:
          return AddSurface((Surface)item, attributes);
        case ObjectType.TextDot:
          return AddTextDot((TextDot)item, attributes);
      }
      throw new NotSupportedException($"Addition of model component not supported for type: {item.GetType().FullName}");
    }

    /// <summary>
    /// Adds a point object to the table.
    /// </summary>
    /// <param name="x">X component of point coordinate.</param>
    /// <param name="y">Y component of point coordinate.</param>
    /// <param name="z">Z component of point coordinate.</param>
    /// <returns>id of new object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(double x, double y, double z)
    {
      return AddPoint(new Point3d(x, y, z));
    }
    /// <summary>Adds a point object to the table.</summary>
    /// <param name="point">A location for point.</param>
    /// <returns>Id of new object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3d point)
    {
      return AddPoint(point, null);
    }

    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">A location for point.</param>
    /// <param name="attributes">attributes to apply to point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3d point, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes==null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddPoint(pThis, point, pConstAttributes);
    }

    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3f point)
    {
      Point3d p3d = new Point3d(point);
      return AddPoint(p3d);
    }
    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <param name="attributes">attributes to apply to point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3f point, DocObjects.ObjectAttributes attributes)
    {
      Point3d p3d = new Point3d(point);
      return AddPoint(p3d, attributes);
    }

    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <returns>List of object ids.</returns>
    /// <since>5.0</since>
    public Guid[] AddPoints(IEnumerable<Point3d> points)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      List<Guid> ids = new List<Guid>();
      foreach (Point3d pt in points)
      {
        ids.Add(AddPoint(pt));
      }

      return ids.ToArray();
    }
    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="attributes">Attributes to apply to point objects.</param>
    /// <returns>An array of object unique identifiers.</returns>
    /// <since>5.0</since>
    public Guid[] AddPoints(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      List<Guid> ids = new List<Guid>();
      foreach (Point3d pt in points)
      {
        ids.Add(AddPoint(pt, attributes));
      }

      return ids.ToArray();
    }

    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <returns>An array of object unique identifiers.</returns>
    /// <since>5.0</since>
    public Guid[] AddPoints(IEnumerable<Point3f> points)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      List<Guid> ids = new List<Guid>();
      foreach (Point3f pt in points)
      {
        ids.Add(AddPoint(pt));
      }

      return ids.ToArray();
    }
    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="attributes">Attributes to apply to point objects.</param>
    /// <returns>An array of object unique identifiers.</returns>
    /// <since>5.0</since>
    public Guid[] AddPoints(IEnumerable<Point3f> points, DocObjects.ObjectAttributes attributes)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      List<Guid> ids = new List<Guid>();
      foreach (Point3f pt in points)
      {
        ids.Add(AddPoint(pt, attributes));
      }

      return ids.ToArray();
    }

    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="cloud">PointCloud to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(PointCloud cloud)
    {
      return AddPointCloud(cloud, null);
    }
    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="cloud">PointCloud to add.</param>
    /// <param name="attributes">attributes to apply to point cloud.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(PointCloud cloud, DocObjects.ObjectAttributes attributes)
    {
      if (cloud == null) { throw new ArgumentNullException("cloud"); }

      IntPtr pCloud = cloud.ConstPointer();

      IntPtr pThis = m_parent.NonConstPointer();

      IntPtr pAttrs = IntPtr.Zero;
      if (null != attributes)
        pAttrs = attributes.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddPointCloud2(pThis, pCloud, pAttrs);
    }
    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="points">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(IEnumerable<Point3d> points)
    {
      return AddPointCloud(points, null);
    }
    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="points">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    /// <param name="attributes">Attributes to apply to point cloud.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
    {
      int count;
      Point3d[] ptArray = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 1)
        return Guid.Empty;

      IntPtr pThis = m_parent.NonConstPointer();

      IntPtr pAttrs = IntPtr.Zero;
      if (null != attributes)
        pAttrs = attributes.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddPointCloud(pThis, count, ptArray, pAttrs);
    }

    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <param name="uMagnitude">The size in U direction.</param>
    /// <param name="vMagnitude">The size in V direction.</param>
    /// <param name="clippedViewportId">The viewport id that the new clipping plane will clip.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addclippingplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addclippingplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_addclippingplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId)
    {
      return AddClippingPlane(plane, uMagnitude, vMagnitude, new Guid[] { clippedViewportId });
    }
    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <param name="uMagnitude">The size in U direction.</param>
    /// <param name="vMagnitude">The size in V direction.</param>
    /// <param name="clippedViewportIds">A list, an array or any enumerable of viewport ids that the new clipping plane will clip.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds)
    {
      return AddClippingPlane(plane, uMagnitude, vMagnitude, clippedViewportIds, null);
    }
    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <param name="uMagnitude">The size in U direction.</param>
    /// <param name="vMagnitude">The size in V direction.</param>
    /// <param name="clippedViewportIds">list of viewport ids that the new clipping plane will clip.</param>
    /// <param name="attributes">Attributes to apply to point cloud.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttrs = (null == attributes) ? IntPtr.Zero : attributes.ConstPointer();
      List<Guid> ids = new List<Guid>();
      foreach (Guid item in clippedViewportIds)
        ids.Add(item);
      Guid[] clippedIds = ids.ToArray();
      int count = clippedIds.Length;
      if (count < 1)
        return Guid.Empty;
      IntPtr pThis = m_parent.NonConstPointer();
      Guid rc = UnsafeNativeMethods.ONX_Model_ObjectTable_AddClippingPlane(pThis, ref plane, uMagnitude, vMagnitude, count, clippedIds, pAttrs);
      return rc;
    }

    /// <summary>
    /// Adds a linear dimension to the 3dm file object table.
    /// </summary>
    /// <param name="dimension">A dimension.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLinearDimension(LinearDimension dimension)
    {
      return AddLinearDimension(dimension, null);
    }

    /// <summary>
    /// Adds a linear dimension to the 3dm file object table.
    /// </summary>
    /// <param name="dimension">A dimension.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLinearDimension(LinearDimension dimension, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstDimension = dimension.ConstPointer();
      IntPtr pAttributes = (attributes==null)?IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddLinearDimension(pThis, pConstDimension, pAttributes);
    }

    /// <summary>
    /// Adds a angular dimension object to the 3dm file object table.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddAngularDimension(AngularDimension dimension)
    {
      return AddAngularDimension(dimension, null);
    }

    /// <summary>
    /// Adds a angular dimension object to the 3dm file object table.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddAngularDimension(AngularDimension dimension, ObjectAttributes attributes)
    {
      IntPtr ptr_const_dim = dimension.ConstPointer();
      IntPtr ptr_const_atts = (null == attributes) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_this = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddAngularDimension(ptr_this, ptr_const_dim, ptr_const_atts);
    }

    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="from">A line start point.</param>
    /// <param name="to">A line end point.</param>
    /// <returns>A unique identifier of new rhino object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Point3d from, Point3d to)
    {
      return AddLine(from, to, null);
    }
    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="from">The start point of the line.</param>
    /// <param name="to">The end point of the line.</param>
    /// <param name="attributes">Attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Point3d from, Point3d to, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (null == attributes) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddLine(pThis, from, to, pAttr);
    }
    /// <summary>Adds a line object to Rhino.</summary>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Line line)
    {
      return AddLine(line.From, line.To);
    }
    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="line">A line.</param>
    /// <param name="attributes">Attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Line line, DocObjects.ObjectAttributes attributes)
    {
      return AddLine(line.From, line.To, attributes);
    }

    /// <summary>Adds a polyline object to Rhino.</summary>
    /// <param name="points">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPolyline(IEnumerable<Point3d> points)
    {
      return AddPolyline(points, null);
    }
    /// <summary>Adds a polyline object to Rhino.</summary>
    /// <param name="points">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    /// <param name="attributes">Attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPolyline(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
    {
      int count;
      Point3d[] ptArray = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 1)
        return Guid.Empty;

      IntPtr pAttrs = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddPolyLine(pThis, count, ptArray, pAttrs);
    }

    /// <summary>Adds a curve object to the document representing an arc.</summary>
    /// <param name="arc">An arc.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddArc(Arc arc)
    {
      return AddArc(arc, null);
    }
    /// <summary>Adds a curve object to the document representing an arc.</summary>
    /// <param name="arc">An arc to add.</param>
    /// <param name="attributes">attributes to apply to arc.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddArc(Arc arc, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddArc(pThis, ref arc, pAttr);
    }

    /// <summary>Adds a curve object to the document representing a circle.</summary>
    /// <param name="circle">A circle to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCircle(Circle circle)
    {
      return AddCircle(circle, null);
    }
    /// <summary>Adds a curve object to the document representing a circle.</summary>
    /// <param name="circle">A circle to add.</param>
    /// <param name="attributes">attributes to apply to circle.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCircle(Circle circle, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddCircle(pThis, ref circle, pAttr);
    }

    /// <summary>Adds a curve object to the document representing an ellipse.</summary>
    /// <param name="ellipse">An ellipse to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddEllipse(Ellipse ellipse)
    {
      return AddEllipse(ellipse, null);
    }
    /// <summary>Adds a curve object to the document representing an ellipse.</summary>
    /// <param name="ellipse">An ellipse to add.</param>
    /// <param name="attributes">attributes to apply to ellipse.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddEllipse(Ellipse ellipse, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddEllipse(pThis, ref ellipse, pAttr);
    }
    /// <summary>
    /// Adds a surface object to the document representing a sphere.
    /// </summary>
    /// <param name="sphere">A sphere to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddSphere(Sphere sphere)
    {
      return AddSphere(sphere, null);
    }
    /// <summary>
    /// Adds a surface object to the document representing a sphere.
    /// </summary>
    /// <param name="sphere">A sphere to add.</param>
    /// <param name="attributes">Attributes to link with the sphere.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddSphere(Sphere sphere, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddSphere(pThis, ref sphere, pAttr);
    }

    /// <summary>Adds a curve object to the table.</summary>
    /// <param name="curve">A curve to add.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCurve(Geometry.Curve curve)
    {
      return AddCurve(curve, null);
    }
    /// <summary>Adds a curve object to the table.</summary>
    /// <param name="curve">A duplicate of this curve is added to Rhino.</param>
    /// <param name="attributes">Attributes to apply to curve.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCurve(Geometry.Curve curve, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr curvePtr = curve.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddCurve(pThis, curvePtr, pAttr);
    }

    /// <summary>Adds a text dot object to the table.</summary>
    /// <param name="text">The text.</param>
    /// <param name="location">The location.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(string text, Point3d location)
    {
      Geometry.TextDot dot = new Rhino.Geometry.TextDot(text, location);
      Guid rc = AddTextDot(dot);
      dot.Dispose();
      return rc;
    }
    /// <summary>Adds a text dot object to the table.</summary>
    /// <param name="text">The text.</param>
    /// <param name="location">The location.</param>
    /// <param name="attributes">Attributes to link with curve.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(string text, Point3d location, DocObjects.ObjectAttributes attributes)
    {
      Geometry.TextDot dot = new Rhino.Geometry.TextDot(text, location);
      Guid rc = AddTextDot(dot, attributes);
      dot.Dispose();
      return rc;
    }
    /// <summary>Adds a text dot object to the table.</summary>
    /// <param name="dot">The text dot.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(Geometry.TextDot dot)
    {
      return AddTextDot(dot, null);
    }
    /// <summary>Adds a text dot object to the table.</summary>
    /// <param name="dot">The text dot.</param>
    /// <param name="attributes">Attributes to link with text dot.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(Geometry.TextDot dot, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr pDot = dot.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddTextDot(pThis, pDot, pAttr);
    }

    /// <summary>
    /// Adds an instance reference geometry object to the table.
    /// </summary>
    /// <param name="instanceReference">The instance reference geometry object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddInstanceObject(InstanceReferenceGeometry instanceReference)
    {
      return AddInstanceObject(instanceReference, null);
    }

    /// <summary>
    /// Adds an instance reference geometry object to the table.
    /// </summary>
    /// <param name="instanceReference">The instance reference geometry object.</param>
    /// <param name="attributes">The attributes to link with the object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddInstanceObject(InstanceReferenceGeometry instanceReference, ObjectAttributes attributes)
    {
      if (null == instanceReference) throw new ArgumentNullException(nameof(instanceReference));
      IntPtr ptr_const_iref = instanceReference.ConstPointer();
      IntPtr ptr_const_attributes = attributes?.ConstPointer() ?? IntPtr.Zero;
      IntPtr ptr_this = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddInstanceRef(ptr_this, ptr_const_iref, ptr_const_attributes);
    }

    /// <summary>
    /// Adds an instance reference geometry object to the table.
    /// </summary>
    /// <param name="instanceDefinitionIndex">The index of the instance definition geometry object.</param>
    /// <param name="instanceXform">The transformation.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform)
    {
      return AddInstanceObject(instanceDefinitionIndex, instanceXform, null);
    }

    /// <summary>
    /// Adds an instance reference geometry object to the table.
    /// </summary>
    /// <param name="instanceDefinitionIndex">The index of the instance definition geometry object.</param>
    /// <param name="instanceXform">The transformation.</param>
    /// <param name="attributes">The object attributes.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>6.5</since>
    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform, ObjectAttributes attributes)
    {
      IntPtr ptr_const_attributes = attributes?.ConstPointer() ?? IntPtr.Zero;
      IntPtr ptr_this = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddInstanceRef2(ptr_this, instanceDefinitionIndex, ref instanceXform, ptr_const_attributes);
    }

#if RHINO_SDK
    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text3d">The text object to add.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(Rhino.Display.Text3d text3d)
    {
      return AddText(text3d, null);
    }
    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text3d">The text object to add.</param>
    /// <param name="attributes">Attributes to link to the object.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(Rhino.Display.Text3d text3d, DocObjects.ObjectAttributes attributes)
    {
      TextJustification justification = TextJustification.None;
      switch (text3d.HorizontalAlignment)
      {
        case TextHorizontalAlignment.Center:
          justification |= TextJustification.Center;
          break;
        case TextHorizontalAlignment.Right:
          justification |= TextJustification.Right;
          break;
        default:
          justification |= TextJustification.Left;
          break;
      }

      switch (text3d.VerticalAlignment)
      {
        case TextVerticalAlignment.Middle:
          justification |= TextJustification.Middle;
          break;
        case TextVerticalAlignment.Top:
          justification |= TextJustification.Top;
          break;
        default:
          justification |= TextJustification.Bottom;
          break;
      }

      return AddText(text3d.Text, text3d.TextPlane, text3d.Height, text3d.FontFace, text3d.Bold, text3d.Italic, justification, attributes);
    }
#endif

    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text">Text string.</param>
    /// <param name="plane">Plane of text.</param>
    /// <param name="height">Height of text.</param>
    /// <param name="fontName">Name of FontFace.</param>
    /// <param name="bold">Bold flag.</param>
    /// <param name="italic">Italic flag.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic)
    {
      return AddText(text, plane, height, fontName, bold, italic, null);
    }
    
    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text">Text string.</param>
    /// <param name="plane">Plane of text.</param>
    /// <param name="height">Height of text.</param>
    /// <param name="fontName">Name of FontFace.</param>
    /// <param name="bold">Bold flag.</param>
    /// <param name="italic">Italic flag.</param>
    /// <param name="justification">The justification of the text.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification)
    {
      return AddText(text, plane, height, fontName, bold, italic, justification, null);
    }

    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text">Text string.</param>
    /// <param name="plane">Plane of text.</param>
    /// <param name="height">Height of text.</param>
    /// <param name="fontName">Name of FontFace.</param>
    /// <param name="bold">Bold flag.</param>
    /// <param name="italic">Italic flag.</param>
    /// <param name="justification">The justification of the text.</param>
    /// <param name="attributes">Attributes to link to the object.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification, DocObjects.ObjectAttributes attributes)
    {
      if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fontName))
        return Guid.Empty;
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      int fontStyle = 0;
      if (bold)
        fontStyle |= 1;
      if (italic)
        fontStyle |= 2;
      Guid rc = UnsafeNativeMethods.ONX_Model_ObjectTable_AddText(pThis, text, ref plane, height, fontName, fontStyle, (int)justification, pAttr);
      return rc;
    }

    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text">Text string.</param>
    /// <param name="plane">Plane of text.</param>
    /// <param name="height">Height of text.</param>
    /// <param name="fontName">Name of FontFace.</param>
    /// <param name="bold">Bold flag.</param>
    /// <param name="italic">Italic flag.</param>
    /// <param name="attributes">Object Attributes.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, DocObjects.ObjectAttributes attributes)
    {
      return AddText(text, plane, height, fontName, bold, italic, TextJustification.None, attributes);
    }

    /// <summary>Adds a surface object to Rhino.</summary>
    /// <param name="surface">A duplicate of this surface is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddSurface(Geometry.Surface surface)
    {
      return AddSurface(surface, null);
    }
    /// <summary>Adds a surface object to Rhino.</summary>
    /// <param name="surface">A duplicate of this surface is added to Rhino.</param>
    /// <param name="attributes">Attributes to link to the object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddSurface(Geometry.Surface surface, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr pSurface = surface.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddSurface(pThis, pSurface, pAttr);
    }

    /// <summary>Adds an extrusion object to Rhino.</summary>
    /// <param name="extrusion">A duplicate of this extrusion is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddExtrusion(Geometry.Extrusion extrusion)
    {
      return AddExtrusion(extrusion, null);
    }
    /// <summary>Adds an extrusion object to Rhino.</summary>
    /// <param name="extrusion">A duplicate of this extrusion is added to Rhino.</param>
    /// <param name="attributes">Attributes to link to the object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddExtrusion(Geometry.Extrusion extrusion, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr pConstExtrusion = extrusion.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddExtrusion(pThis, pConstExtrusion, pAttr);
    }

    /// <summary>Adds a mesh object to Rhino.</summary>
    /// <param name="mesh">A duplicate of this mesh is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddMesh(Geometry.Mesh mesh)
    {
      return AddMesh(mesh, null);
    }
    /// <summary>Adds a mesh object to Rhino.</summary>
    /// <param name="mesh">A duplicate of this mesh is added to Rhino.</param>
    /// <param name="attributes">Attributes to link to the object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddMesh(Geometry.Mesh mesh, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr pConstMesh = mesh.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddMesh(pThis, pConstMesh, pAttr);
    }

    /// <summary>Adds a brep object to Rhino.</summary>
    /// <param name="brep">A duplicate of this brep is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddBrep(Geometry.Brep brep)
    {
      return AddBrep(brep, null);
    }
    /// <summary>Adds a brep object to Rhino.</summary>
    /// <param name="brep">A duplicate of this brep is added to Rhino.</param>
    /// <param name="attributes">Attributes to apply to brep.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddBrep(Geometry.Brep brep, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      IntPtr pConstBrep = brep.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddBrep(pThis, pConstBrep, pAttr);
    }

    /// <summary>
    /// Adds an annotation leader to the document.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points)
    {
      return AddLeader(null, plane, points);
    }

    /// <summary>
    /// Adds an annotation leader to the document.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <param name="attributes">Attributes to apply to brep.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
    {
      return AddLeader(null, plane, points, attributes);
    }

    /// <summary>
    /// Adds an annotation leader to the document.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="plane">A plane.</param>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <param name="attributes">Attributes to apply to brep.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
    {
      string s = null;
      if (!string.IsNullOrEmpty(text))
        s = text;
      Rhino.Collections.RhinoList<Point2d> pts = new Rhino.Collections.RhinoList<Point2d>();
      foreach (Point2d pt in points)
        pts.Add(pt);
      int count = pts.Count;
      if (count < 1)
        return Guid.Empty;

      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();

      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddLeader(pThis, s, ref plane, count, pts.m_items, pAttr);
    }

    /// <summary>
    /// Adds an annotation leader to the document.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="plane">A plane.</param>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points)
    {
      return AddLeader(text, plane, points, null);
    }

#if RHINO_SDK

    /// <summary>
    /// Adds an annotation leader to the document. This overload is only provided in the Rhino SDK.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(string text, IEnumerable<Point3d> points)
    {
      Plane plane;
      //double max_deviation;
      PlaneFitResult rc = Plane.FitPlaneToPoints(points, out plane);//, out max_deviation);
      if (rc != PlaneFitResult.Success)
        return Guid.Empty;

      Rhino.Collections.RhinoList<Point2d> points2d = new Rhino.Collections.RhinoList<Point2d>();
      foreach (Point3d point3d in points)
      {
        double s, t;
        if (plane.ClosestParameter(point3d, out s, out t))
        {
          Point2d newpoint = new Point2d(s, t);
          if (points2d.Count > 0 && points2d.Last.DistanceTo(newpoint) < Rhino.RhinoMath.SqrtEpsilon)
            continue;
          points2d.Add(new Point2d(s, t));
        }
      }
      return AddLeader(text, plane, points2d);
    }

    /// <summary>
    /// Adds an annotation leader to the document. This overload is only provided in the Rhino SDK.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable set of 2d points.</param>
    /// <returns>A unique identifier for the object; or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLeader(IEnumerable<Point3d> points)
    {
      return AddLeader(null, points);
    }
#endif

    /// <summary>
    /// Adds a hatch to the document.
    /// </summary>
    /// <param name="hatch">A hatch.</param>
    /// <returns>A unique identifier for the hatch, or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddHatch(Hatch hatch)
    {
      return AddHatch(hatch, null);
    }

    /// <summary>
    /// Adds a hatch to the document.
    /// </summary>
    /// <param name="hatch">A hatch.</param>
    /// <param name="attributes">Attributes to apply</param>
    /// <returns>A unique identifier for the hatch, or <see cref="Guid.Empty"/> on failure.</returns>
    /// <since>5.0</since>
    public Guid AddHatch(Hatch hatch, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstHatch = hatch.ConstPointer();
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddHatch(pThis, pConstHatch, pAttr);
    }

    /// <summary>
    /// Adds a SubD to the document
    /// </summary>
    /// <param name="subd">the Subd to add</param>
    /// <returns>A unique identifier for the SubD, or <see cref="Guid.Empty"/> on failure</returns>
    /// <since>7.1</since>
    public Guid AddSubD(SubD subd)
    {
      return AddSubD(subd, null);
    }

    /// <summary>
    /// Adds a SubD to the document
    /// </summary>
    /// <param name="subd">the Subd to add</param>
    /// <param name="attributes">Attributes to apply</param>
    /// <returns>A unique identifier for the SubD, or <see cref="Guid.Empty"/> on failure</returns>
    /// <since>7.1</since>
    public Guid AddSubD(SubD subd, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstSubD = subd.ConstPointer();
      IntPtr pAttr = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_AddSubD(pThis, pConstSubD, pAttr);
    }
    #endregion

    #region Object deletion
    /// <summary>
    /// Deletes object from document.
    /// </summary>
    /// <param name="objectId">Id of the object to delete.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.2</since>
    public bool Delete(Guid objectId)
    {
      IntPtr pThis = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ObjectTable_Delete(pThis, objectId);
    }
    /// <summary>
    /// Deletes a collection of objects from the document.
    /// </summary>
    /// <param name="objectIds">Ids of all objects to delete.</param>
    /// <returns>The number of successfully deleted objects.</returns>
    /// <since>5.2</since>
    public int Delete(IEnumerable<Guid> objectIds)
    {
      if (objectIds == null) { throw new ArgumentNullException("objectIds"); }

      int count = 0;
      foreach (Guid id in objectIds)
      {
        if (Delete(id)) { count++; }
      }
      return count;
    }
    #endregion

#region Light fixes
    /// <summary>
    /// Returns the total amount of items in the object table, including lights.
    /// </summary>
    /// <since>5.0</since>
    public override int Count => base.Count + m_manifest.ActiveObjectCount(ModelComponentType.RenderLight);

    /// <summary>
    /// Returns an enumerator that yields all objects in this document.
    /// Like in Rhino, this includes lights. Unlike in Rhino, however, all lights are returned in the end of the list.
    /// </summary>
    /// <returns>An enumerator that yields all objects in a document.</returns>
    /// <since>5.0</since>
    public override IEnumerator<File3dmObject> GetEnumerator()
    {
      var base_enum = base.GetEnumerator();
      while (base_enum.MoveNext())
      {
        yield return base_enum.Current;
      }

      var light_enum = m_manifest.GetEnumerator(ModelComponentType.RenderLight);
      while (light_enum.MoveNext())
      {
        yield return new File3dmObject(light_enum.Current.Id, m_parent);
      }
    }
    #endregion
  }

  /// <summary>
  /// Represents custom plug-in data, in the 3dm file, written by a plug-in.
  /// </summary>
  public class File3dmPlugInData
  {
    readonly File3dm m_parent;
    readonly Guid m_id;

    internal File3dmPlugInData(File3dm parent, Guid id)
    {
      m_parent = parent;
      m_id = id;
    }

    /// <summary>
    /// Gets the id of the plug-in that is associated with this custom data.
    /// </summary>
    /// <since>5.0</since>
    public Guid PlugInId => m_id;
  }

  /// <summary>
  /// Table of custom data provided by plug-ins
  /// </summary>
  public class File3dmPlugInDataTable :
    IEnumerable<File3dmPlugInData>, Collections.IRhinoTable<File3dmPlugInData>
  {
    readonly File3dm m_parent;

    internal File3dmPlugInDataTable(File3dm parent)
    {
      m_parent = parent;
    }

    /// <summary>Prepares a text dump of table.</summary>
    /// <returns>A string containing the dump.</returns>
    /// <since>5.0</since>
    public string Dump()
    {
      return m_parent.Dump(File3dm.idxUserDataTable);
    }

    #region properties
    /// <summary>
    /// Gets the number of <see cref="File3dmPlugInData"/> objects in this table.
    /// </summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        IntPtr pConstParent = m_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_ExtraTableCount(pConstParent, File3dm.idxUserDataTable);
      }
    }

    /// <summary>
    /// Gets the <see cref="File3dmPlugInData"/> object at the given index. 
    /// </summary>
    /// <param name="index">Index of File3dmPlugInData to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The File3dmPlugInData at [index].</returns>
    public File3dmPlugInData this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        IntPtr pModel = m_parent.ConstPointer();
        Guid id = UnsafeNativeMethods.ONX_Model_UserDataTable_Uuid(pModel, index);
        if (Guid.Empty == id)
          throw new IndexOutOfRangeException();
        return new File3dmPlugInData(m_parent, id);
      }
    }
    #endregion

    /// <summary>
    /// Attempts to read a Rhino plug-in's custom data from the <see cref="File3dm"/> file.
    /// </summary>
    /// <param name="pluginData">The plug-in whose data you want to try to read.</param>
    /// <param name="dataReader">
    /// The function that will read the data.
    /// This function must be implemented identical to the the originating plug-in's <see cref="PlugIns.PlugIn.ReadDocument(RhinoDoc, BinaryArchiveReader, FileReadOptions)"/> method.
    /// </param>
    /// <returns>The value returned by the data reading function if successful, false otherwise.</returns>
    public bool TryRead(File3dmPlugInData pluginData, Func<File3dm, BinaryArchiveReader, bool> dataReader)
    {
      if (null == pluginData || null == dataReader)
        return false;

      IntPtr ptr_const_model = m_parent.ConstPointer();
      IntPtr ptr_buffer_archive = UnsafeNativeMethods.ONX_Model_PlugIn_UserData_NewArchive(ptr_const_model, pluginData.PlugInId);
      if (IntPtr.Zero == ptr_buffer_archive)
        return false;

      BinaryArchiveReader archive = new BinaryArchiveReader(ptr_buffer_archive);
      bool rc = dataReader(m_parent, archive);
      UnsafeNativeMethods.ONX_Model_UserData_DeleteArchive(ptr_buffer_archive);
      return rc;
    }

    /// <summary>
    /// Remove all entries from this table.
    /// </summary>
    /// <since>5.0</since>
    public void Clear()
    {
      IntPtr pParent = m_parent.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_UserDataTable_Clear(pParent);
    }

#region IEnumerable Implementation
    /// <summary>
    /// Gets the enumerator that visits any <see cref="File3dmPlugInData"/> in this table.
    /// </summary>
    /// <returns>The enumerator.</returns>
    /// <since>5.0</since>
    public IEnumerator<File3dmPlugInData> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<File3dmPlugInDataTable, File3dmPlugInData>(this);
    }
    /// <since>5.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<File3dmPlugInDataTable, File3dmPlugInData>(this);
    }
#endregion

  }

  /// <summary>
  /// Provides access to materials in the 3dm file.
  /// </summary>
  public class File3dmMaterialTable :
    File3dmCommonComponentTable<Material>,
    IList<DocObjects.Material>
  {
    internal File3dmMaterialTable(File3dm parent) : base(parent){}

    /// <summary>
    /// Returns <see cref="ModelComponentType.RenderMaterial"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.RenderMaterial;
      }
    }

    /// <summary>
    /// Retrieves a material based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A material, or null if none was found.</returns>
    /// <since>6.0</since>
    public DocObjects.Material FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }
  }

  /// <summary>
  /// Provides access to Linetypes in the 3dm file.
  /// </summary>
  public class File3dmLinetypeTable :
    File3dmCommonComponentTable<Linetype>,
    IList<DocObjects.Linetype>
  {
    internal File3dmLinetypeTable(File3dm parent) : base(parent){}

    /// <summary>
    /// Returns <see cref="ModelComponentType.LinePattern"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.LinePattern;
      }
    }

    /// <summary>
    /// Finds a Linetype given its name.
    /// </summary>
    /// <param name="name">The name of the Linetype to be searched.</param>
    /// <returns>A Linetype, or null on error.</returns>
    /// <since>6.0</since>
    public Linetype FindName(string name)
    {
      return m_manifest.FindName<Linetype>(name, Guid.Empty);
    }

    /// <summary>
    /// Finds a Linetype given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the Linetype to be searched.</param>
    /// <returns>An Linetype, or null on error.</returns>
    /// <since>6.0</since>
    public Linetype FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<Linetype>(nameHash);
    }

    /// <summary>
    /// Retrieves a Linetype object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A Linetype, or null if none was found.</returns>
    /// <since>6.0</since>
    public Linetype FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }
  }

  /// <summary>
  /// Provides access to layers in the 3dm file.
  /// </summary>
  public class File3dmLayerTable :
   File3dmCommonComponentTable<Layer>,
   IList<Layer>
  {
    internal File3dmLayerTable(File3dm parent) : base(parent)
    {
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.Layer"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.Layer;
      }
    }

    /// <summary>
    /// Easy way to add a layer to the model
    /// </summary>
    /// <param name="name">new layer name</param>
    /// <param name="color">new layer color</param>
    /// <returns>
    /// If layer_name is valid, the layer's index (>=0) is returned. Otherwise,
    /// RhinoMath.UnsetIntIndex is returned.
    /// </returns>
    /// <since>7.6</since>
    public int AddLayer(string name, System.Drawing.Color color)
    {
      IntPtr ptrFile3dm = m_parent.NonConstPointer();
      int index = UnsafeNativeMethods.ONX_Model_AddLayer(ptrFile3dm, name, color.ToArgb());
      return index;
    }

    /// <summary>
    /// Finds a Layer given its name.
    /// </summary>
    /// <param name="name">The name of the Layer to be searched.</param>
    /// <param name="parentId">The id of the parent Layer to be searched.</param>
    /// <returns>A Layer, or null on error.</returns>
    /// <since>6.0</since>
    public Layer FindName(string name, Guid parentId)
    {
      return m_manifest.FindName<Layer>(name, parentId);
    }

    /// <summary>
    /// Finds a Layer given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the Layer to be searched.</param>
    /// <returns>An Layer, or null on error.</returns>
    /// <since>6.0</since>
    public Layer FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<Layer>(nameHash);
    }

    /// <summary>
    /// Retrieves a Layer object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A Layer object, or null if none was found.</returns>
    /// <since>6.0</since>
    public Layer FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }
  }


  /// <summary>
  /// Provides access to groups in the 3dm file.
  /// </summary>
  public class File3dmGroupTable :
    File3dmCommonComponentTable<Group>,
    IList<Group>
  {
    internal File3dmGroupTable(File3dm parent) : base(parent) { }

    /// <summary>
    /// Returns <see cref="ModelComponentType.Group"/>.
    /// </summary>
    /// <since>6.5</since>
    public override ModelComponentType ComponentType
    {
      get { return ModelComponentType.Group; }
    }

    /// <summary>
    /// Finds a Group given its name.
    /// </summary>
    /// <param name="name">The name of the Group to be searched.</param>
    /// <returns>A Group, or null on error.</returns>
    /// <since>6.5</since>
    public Group FindName(string name)
    {
      return m_manifest.FindName<Group>(name, Guid.Empty);
    }

    /// <summary>
    /// Finds a Group given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the Group to be searched.</param>
    /// <returns>A Group, or null on error.</returns>
    /// <since>6.5</since>
    public Group FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<Group>(nameHash);
    }

    /// <summary>
    /// Retrieves a Group object based on an index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="groupIndex">The index to search for.</param>
    /// <returns>A Group object, or null if none was found.</returns>
    /// <since>6.5</since>
    public Group FindIndex(int groupIndex)
    {
      return __FindIndexInternal(groupIndex);
    }

    /// <summary>
    /// Gets an array of all of the objects in a group.
    /// </summary>
    /// <param name="groupIndex">The index of the group in this table.</param>
    /// <returns>Array of objects that belong to the specified group or empty array if no objects could be found.</returns>
    /// <since>6.20</since>
    public File3dmObject[] GroupMembers(int groupIndex)
    {
      var list = new List<File3dmObject>();
      foreach (var obj in m_parent.Objects)
      {
        if (obj.Attributes.GetGroupList().Contains(groupIndex))
        {
          list.Add(obj);
        }
      }
      return list.Count > 0 ? list.ToArray() : new File3dmObject[0];
    }
  }


  /// <summary>
  /// Provides access to annotation styles in the 3dm file.
  /// </summary>
  public class File3dmDimStyleTable :
    File3dmCommonComponentTable<DimensionStyle>,
    IList<DimensionStyle>
  {
    internal File3dmDimStyleTable(File3dm parent) : base(parent) {}

    /// <summary>
    /// Returns <see cref="ModelComponentType.DimStyle"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.DimStyle;
      }
    }

    /// <summary>
    /// Finds a DimensionStyle given its name.
    /// </summary>
    /// <param name="name">The name of the DimensionStyle to be searched.</param>
    /// <returns>An DimensionStyle, or null on error.</returns>
    /// <since>6.0</since>
    public DimensionStyle FindName(string name)
    {
      return m_manifest.FindName<DimensionStyle>(name, Guid.Empty);
    }

    /// <summary>
    /// Finds a DimensionStyle given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the DimensionStyle to be searched.</param>
    /// <returns>An DimensionStyle, or null on error.</returns>
    /// <since>6.0</since>
    public DimensionStyle FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<DimensionStyle>(nameHash);
    }

    /// <summary>
    /// Retrieves a DimensionStyle object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A DimensionStyle object, or null if none was found.</returns>
    /// <since>6.0</since>
    public DimensionStyle FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }
  }

  /// <summary>
  /// Provides access to hatch pattern definitions in the 3dm file.
  /// </summary>
  public class File3dmHatchPatternTable :
    File3dmCommonComponentTable<HatchPattern>,
    IList<HatchPattern>
  {
    internal File3dmHatchPatternTable(File3dm parent) : base(parent) {}

    /// <summary>
    /// Returns <see cref="ModelComponentType.HatchPattern"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.HatchPattern;
      }
    }

    /// <summary>
    /// Finds a HatchPattern given its name.
    /// </summary>
    /// <param name="name">The name of the HatchPattern to be searched.</param>
    /// <returns>An HatchPattern, or null on error.</returns>
    /// <since>6.0</since>
    public HatchPattern FindName(string name)
    {
      return m_manifest.FindName<HatchPattern>(name, Guid.Empty);
    }

    /// <summary>
    /// Finds a HatchPattern given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the HatchPattern to be searched.</param>
    /// <returns>An HatchPattern, or null on error.</returns>
    /// <since>6.0</since>
    public HatchPattern FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<HatchPattern>(nameHash);
    }

    /// <summary>
    /// Retrieves a HatchPattern object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A HatchPattern object, or null if none was found.</returns>
    /// <since>6.0</since>
    public HatchPattern FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }
  }

  /// <summary>
  /// Provides access to instance (block) definitions in the 3dm file.
  /// </summary>
  public class File3dmInstanceDefinitionTable :
    File3dmCommonComponentTable<InstanceDefinitionGeometry>, 
    IList<InstanceDefinitionGeometry>
  {
    internal File3dmInstanceDefinitionTable(File3dm parent) : base(parent){}

    /// <summary>
    /// Returns <see cref="ModelComponentType.InstanceDefinition"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.InstanceDefinition;
      }
    }

    /// <summary>
    /// Finds an InstanceDefinitionGeometry given its name.
    /// </summary>
    /// <param name="name">The name of the InstanceDefinitionGeometry to be searched.</param>
    /// <returns>An InstanceDefinitionGeometry, or null on error.</returns>
    /// <since>6.0</since>
    public InstanceDefinitionGeometry FindName(string name)
    {
      return m_manifest.FindName<InstanceDefinitionGeometry>(name, Guid.Empty);
    }

    /// <summary>
    /// Finds a InstanceDefinitionGeometry given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the InstanceDefinitionGeometry to be searched.</param>
    /// <returns>An InstanceDefinitionGeometry, or null on error.</returns>
    /// <since>6.0</since>
    public InstanceDefinitionGeometry FindNameHash(NameHash nameHash)
    {
      return m_manifest.FindNameHash<InstanceDefinitionGeometry>(nameHash);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="url">A URL or hyperlink.</param>
    /// <param name="urlTag">A description of the URL or hyperlink.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <param name="attributes">An array, a list or any enumerable set of attributes.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <since>7.0</since>
    public int Add(string name, string description, string url, string urlTag, Point3d basePoint, IEnumerable<GeometryBase> geometry, IEnumerable<ObjectAttributes> attributes)
    {
      using (SimpleArrayGeometryPointer g = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr ptr_array_attributes = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        if (attributes != null)
        {
          foreach (ObjectAttributes att in attributes)
          {
            IntPtr const_ptr_attributes = att.ConstPointer();
            UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Add(ptr_array_attributes, const_ptr_attributes);
          }
        }
        IntPtr const_ptr_geometry = g.ConstPointer();
        IntPtr ptr_this = m_parent.NonConstPointer();
        int rc = UnsafeNativeMethods.ONX_Model_File3dmInstanceDefinitionTable_Add(ptr_this, name, description, url, urlTag, basePoint, const_ptr_geometry, ptr_array_attributes);

        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_array_attributes);
        return rc;
      }
    }


    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <param name="attributes">An array, a list or any enumerable set of attributes.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <since>6.5</since>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry, IEnumerable<ObjectAttributes> attributes)
    {
      return Add(name, description, string.Empty, string.Empty, basePoint, geometry, attributes);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <since>6.5</since>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry)
    {
      return Add(name, description, string.Empty, string.Empty, basePoint, geometry, null);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An element.</param>
    /// <param name="attributes">An attribute.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <since>6.5</since>
    public int Add(string name, string description, Point3d basePoint, GeometryBase geometry, ObjectAttributes attributes)
    {
      return Add(name, description, string.Empty, string.Empty, basePoint, new GeometryBase[] { geometry }, new ObjectAttributes[] { attributes });
    }

    /// <summary>
    /// Adds a linked instance definition to the instance definition table.
    /// </summary>
    /// <param name="filename">Full path of the file to link.</param>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <returns></returns>
    /// <since>6.13</since>
    public int AddLinked(string filename, string name, string description)
    {
      IntPtr ptr_this = m_parent.NonConstPointer();
      int rc = UnsafeNativeMethods.ONX_Model_File3dmInstanceDefinitionTable_AddLinked(ptr_this, filename, name, description);
      return rc;
    }
  }

  /// <summary>
  /// Provides access to views in the 3dm file.
  /// </summary>
  public class File3dmViewTable :
    IList<ViewInfo>, Collections.IRhinoTable<DocObjects.ViewInfo>
  {
    readonly File3dm m_parent;
    readonly bool m_named_views;
    internal File3dmViewTable(File3dm parent, bool namedViews)
    {
      m_parent = parent;
      m_named_views = namedViews;
    }

    internal int Find(string name)
    {
      int cnt = Count;
      for (int i = 0; i < cnt; i++)
      {
        if (this[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Finds a ViewInfo given its name.
    /// </summary>
    /// <param name="name">The name of the ViewInfo to be searched.</param>
    /// <returns>An ViewInfo, or null on error.</returns>
    /// <since>6.0</since>
    public ViewInfo FindName(string name)
    {
      var view_index = Find(name);
      return view_index >= 0 ? this[view_index] : null;
    }

    /// <summary>
    /// Returns the index of the current ViewInfo.
    /// </summary>
    /// <param name="item">The item to be searched.</param>
    /// <returns>The index of the ViewInfo.</returns>
    /// <since>6.0</since>
    public int IndexOf(DocObjects.ViewInfo item)
    {
      File3dm file = item.m_parent as File3dm;
      if (file == m_parent && m_parent!=null)
      {
        IntPtr pViewPtr = item.ConstPointer();
        IntPtr pModelPtr = m_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_ViewTable_Index(pModelPtr, pViewPtr, m_named_views);
      }
      return -1;
    }

    void IList<ViewInfo>.Insert(int index, DocObjects.ViewInfo item)
    {
      if (index < 0 || index > Count)
        throw new IndexOutOfRangeException();

      IntPtr pParent = m_parent.NonConstPointer();
      IntPtr pConstView = item.ConstPointer();
      UnsafeNativeMethods.ONX_Model_ViewTable_Insert(pParent, pConstView, index, m_named_views);
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    /// <returns>True if the item was removed.</returns>
    /// <since>6.0</since>
    public bool Delete(int index)
    {
      if (index < 0 || index >= Count)
        throw new IndexOutOfRangeException();
      IntPtr pParent = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_ViewTable_RemoveAt(pParent, index, m_named_views);
    }

    /// <summary>
    /// Gets the view info at an index. The set method always throws NotSupportedException.
    /// </summary>
    /// <param name="index">The index of the item to search for.</param>
    /// <returns>A non-null instance, or an exception is thrown.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If index is invalid.</exception>
    public DocObjects.ViewInfo this[int index]
    {
      get
      {
        IntPtr pConstParent = m_parent.ConstPointer();
        Guid id = UnsafeNativeMethods.ONX_Model_ViewTable_Id(pConstParent, index, m_named_views);
        IntPtr pView = UnsafeNativeMethods.ONX_Model_ViewTable_Pointer(pConstParent, index, m_named_views);
        if (IntPtr.Zero == pView)
          throw new IndexOutOfRangeException();
        return new DocObjects.ViewInfo(m_parent, id, pView, m_named_views);
      }
    }

    DocObjects.ViewInfo IList<DocObjects.ViewInfo>.this[int index]
    {
      get { return this[index]; }
      set { throw new NotSupportedException(); }
    }

    /// <summary>
    /// Adds a 
    /// </summary>
    /// <param name="item"></param>
    /// <since>6.0</since>
    public void Add(DocObjects.ViewInfo item)
    {
      if (item == null)
        throw new ArgumentNullException("item");

      IntPtr pParent = m_parent.NonConstPointer();
      IntPtr pConstView = item.ConstPointer();
      UnsafeNativeMethods.ONX_Model_ViewTable_Add(pParent, pConstView, m_named_views);
    }

    /// <summary>
    /// Removes all items from the table.
    /// </summary>
    /// <since>6.0</since>
    public void Clear()
    {
      IntPtr pParent = m_parent.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_ViewTable_Clear(pParent, m_named_views);
    }

    /// <summary>
    /// Returns an indication of the presence of a view in the table.
    /// </summary>
    /// <param name="item">The view to check.</param>
    /// <returns>true if the item is in the table; false otherwise.</returns>
    /// <since>6.0</since>
    public bool Contains(DocObjects.ViewInfo item)
    {
      return IndexOf(item) != -1;
    }

    /// <summary>
    /// Copies the content of the table to an array.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    /// <since>6.0</since>
    public void CopyTo(DocObjects.ViewInfo[] array, int arrayIndex)
    {
      int available = array.Length - arrayIndex;
      int cnt = Count;
      if (available < cnt)
        throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.");
      for (int i = 0; i < cnt; i++)
      {
        array[arrayIndex++] = this[i];
      }
    }

    /// <summary>
    /// Gets the amount of items in the table.
    /// </summary>
    /// <since>6.0</since>
    public int Count
    {
      get
      {
        IntPtr pConstParent = m_parent.ConstPointer();
        int which = m_named_views ? File3dm.idxNamedViewTable : File3dm.idxViewTable;
        return UnsafeNativeMethods.ONX_Model_ExtraTableCount(pConstParent, which);
      }
    }

    bool ICollection<ViewInfo>.IsReadOnly
    {
      get { return false; }
    }

    /// <summary>
    /// Deletes an item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool Delete(DocObjects.ViewInfo item)
    {
      int index = (this as IList<ViewInfo>).IndexOf(item);
      if (index >= 0)
        Delete(index);
      return (index >= 0);
    }

    bool ICollection<ViewInfo>.Remove(ViewInfo item)
    {
      return Delete(item);
    }

    /// <summary>
    /// Returns an enumerator that yields all views in the table.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <since>6.0</since>
    public IEnumerator<DocObjects.ViewInfo> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<File3dmViewTable, Rhino.DocObjects.ViewInfo>(this);
    }

    /// <since>6.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    void IList<ViewInfo>.RemoveAt(int index)
    {
      Delete(index);
    }
  }

  /// <summary>
  /// Provides access to named construction planes in the 3dm file.
  /// </summary>
  public class File3dmNamedConstructionPlanes :
    IList<ConstructionPlane>, Collections.IRhinoTable<ConstructionPlane>
  {
    // 27-Sep-2017 Dale Fugier https://mcneel.myjetbrains.com/youtrack/issue/RH-41623

    readonly File3dm m_parent;
    internal File3dmNamedConstructionPlanes(File3dm parent)
    {
      m_parent = parent;
    }

    internal int Find(string name)
    {
      for (int i = 0; i < Count; i++)
      {
        if (this[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Finds a named construction plane given its name.
    /// </summary>
    /// <param name="name">The name of the construction plane to be searched.</param>
    /// <returns>A ConstructionPlane, or null if not found.</returns>
    /// <since>6.0</since>
    public ConstructionPlane FindName(string name)
    {
      var view_index = Find(name);
      return view_index >= 0 ? this[view_index] : null;
    }

    /// <summary>
    /// Returns the index of a named construction plane.
    /// </summary>
    /// <param name="cplane">The construction plane to be searched.</param>
    /// <returns>The index of the named construction plane, -1 if not found.</returns>
    /// <since>6.0</since>
    public int IndexOf(ConstructionPlane cplane)
    {
      // 27-Sep-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-41623
      // Currently, a ConstructionPlane does not maintain an IntPtr to an ON_3dmConstructionPlane
      // Thus comparing addresses is not possible. For for now just look for a named
      // construction plane with a matching name. If somebody whines about this, we can fix.
      if (null == cplane)
        throw new ArgumentNullException(nameof(cplane));
      return Find(cplane.Name);
    }

    void IList<ConstructionPlane>.Insert(int index, ConstructionPlane cplane)
    {
      if (null == cplane)
        throw new ArgumentNullException(nameof(cplane));

      if (index < 0 || index > Count)
        throw new IndexOutOfRangeException();

      IntPtr ptr_cplane = cplane.CopyToNative();
      if (ptr_cplane != IntPtr.Zero)
      {
        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Insert(ptr_parent, ptr_cplane, index);
        UnsafeNativeMethods.ON_3dmConstructionPlane_Delete(ptr_cplane);
      }
    }

    /// <summary>
    /// Remove a named construction plane from the table.
    /// </summary>
    /// <param name="index">Zero based array index.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Delete(int index)
    {
      if (index < 0 || index > Count)
        throw new IndexOutOfRangeException();

      IntPtr ptr_parent = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Delete(ptr_parent, index);
    }

    /// <summary>
    /// Deletes a named construction plane from the table.
    /// </summary>
    /// <param name="cplane">The construction plane to delete.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool Delete(DocObjects.ConstructionPlane cplane)
    {
      int index = (this as IList<ConstructionPlane>).IndexOf(cplane);
      if (index >= 0)
        Delete(index);
      return (index >= 0);
    }

    /// <summary>
    /// Gets the named construction plane at an index.
    /// </summary>
    /// <param name="index">Zero based array index.</param>
    /// <returns>
    /// A construction plane at the index, or null on error.
    /// </returns>
    public ConstructionPlane this[int index]
    {
      get
      {
        if (index < 0 || index > Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_const_parent = m_parent.ConstPointer();
        IntPtr ptr_construction_plane = UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Get(ptr_const_parent, index);
        return ConstructionPlane.FromIntPtr(ptr_construction_plane);
      }
    }

    ConstructionPlane IList<ConstructionPlane>.this[int index]
    {
      get { return this[index]; }
      set { throw new NotSupportedException(); }
    }

    /// <summary>
    /// Adds a named construction plane to the table.
    /// </summary>
    /// <param name="name">
    /// The name of the named construction plane.
    /// </param>
    /// <param name="plane">The plane value.</param>
    /// <returns>
    /// 0 based index of the named construction plane.
    /// -1 on failure.
    /// </returns>
    /// <since>6.0</since>
    public int Add(string name, Geometry.Plane plane)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException(nameof(name));

      IntPtr ptr_parent = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Add(ptr_parent, name, ref plane);
    }

    /// <summary>
    /// Adds a named construction plane to the table.
    /// </summary>
    /// <param name="cplane">The construction plane to add.</param>
    /// <since>6.0</since>
    public void Add(ConstructionPlane cplane)
    {
      if (null == cplane)
        throw new ArgumentNullException(nameof(cplane));

      IntPtr ptr_cplane = cplane.CopyToNative();
      if (ptr_cplane != IntPtr.Zero)
      {
        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Add2(ptr_parent, ptr_cplane);
        UnsafeNativeMethods.ON_3dmConstructionPlane_Delete(ptr_cplane);
      }
    }

    /// <summary>
    /// Removes all named construction planes from the table.
    /// </summary>
    /// <since>6.0</since>
    public void Clear()
    {
      IntPtr ptr_parent = m_parent.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Clear(ptr_parent);
    }

    /// <summary>
    /// Returns an indication of the presence of a named construction plane in the table.
    /// </summary>
    /// <param name="cplane">The construction plane to check.</param>
    /// <returns>true if the named construction plane is in the table; false otherwise.</returns>
    /// <since>6.0</since>
    public bool Contains(ConstructionPlane cplane)
    {
      return IndexOf(cplane) != -1;
    }

    /// <summary>
    /// Copies the content of the table to an array.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    /// <since>6.0</since>
    public void CopyTo(ConstructionPlane[] array, int arrayIndex)
    {
      int available = array.Length - arrayIndex;
      int cnt = Count;
      if (available < cnt)
        throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.");
      for (int i = 0; i < cnt; i++)
      {
        array[arrayIndex++] = this[i];
      }
    }

    /// <summary>
    /// Number of named construction planes in the table.
    /// </summary>
    /// <since>6.0</since>
    public int Count
    {
      get
      {
        IntPtr ptr_const_parent = m_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_NamedCPlaneTable_Count(ptr_const_parent);
      }
    }

    bool ICollection<ConstructionPlane>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<ConstructionPlane>.Remove(ConstructionPlane cplane)
    {
      return Delete(cplane);
    }

    void IList<ConstructionPlane>.RemoveAt(int index)
    {
      Delete(index);
    }

#region enumerator
    /// <summary>
    /// Gets an enumerator that yields all construction planes in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    /// <since>6.0</since>
    public IEnumerator<ConstructionPlane> GetEnumerator()
    {
      return new Collections.TableEnumerator<File3dmNamedConstructionPlanes, ConstructionPlane>(this);
    }
    /// <since>6.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Collections.TableEnumerator<File3dmNamedConstructionPlanes, ConstructionPlane>(this);
    }
#endregion
  }

  /// <summary>
  /// Provides access to document strings in the 3dm file.
  /// </summary>
  public class File3dmStringTable
  {
    readonly File3dm m_parent;
    internal File3dmStringTable(File3dm parent)
    {
      m_parent = parent;
    }

    /// <summary>
    /// Returns the number of document strings in the 3dm file.
    /// </summary>
    /// <since>6.0</since>
    public int Count
    {
      get
      {
        IntPtr ptr_parent = m_parent.NonConstPointer();
        return UnsafeNativeMethods.ONX_Model_DocumentUserString_Count(ptr_parent);
      }
    }

    /// <summary>
    /// Returns the number of Section/Entry-style key values.
    /// </summary>
    /// <since>6.0</since>
    public int DocumentUserTextCount
    {
      get
      {
        var cnt = 0;
        for (var i = 0; i < Count; i++)
          if ((GetKey(i) ?? "").Contains("\\")) cnt++;
        return cnt;
      }
    }

    /// <summary>
    /// Returns a key value at a given index.
    /// </summary>
    /// <param name="i">The index.</param>
    /// <returns>The key if successful.</returns>
    /// <since>6.0</since>
    public string GetKey(int i)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_DocumentUserString_GetString(ptr_parent, i, true, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Returns a string value at a given index.
    /// </summary>
    /// <param name="i">The index at which to get the value.</param>
    /// <returns>The string value if successful.</returns>
    /// <since>6.0</since>
    public string GetValue(int i)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_DocumentUserString_GetString(ptr_parent, i, false, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Returns a string value at a key.
    /// </summary>
    /// <param name="key">The key at which to get the value.</param>
    /// <returns>The string value if successful.</returns>
    /// <since>6.0</since>
    public string GetValue(string key)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_DocumentUserString_GetString2(ptr_parent, key, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Returns a string value given a section and entry.
    /// </summary>
    /// <param name="section">The section at which to get the value.</param>
    /// <param name="entry">The entry to search for.</param>
    /// <returns>The string value if successful.</returns>
    /// <since>6.0</since>
    public string GetValue(string section, string entry)
    {
      if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(entry))
        return string.Empty;
      string key = section + "\\" + entry;
      return GetValue(key);
    }

    /// <summary>
    /// Returns a list of all the section names for document strings in the 3dm file.
    /// <para>By default a section name is a key that is prefixed with a string separated by a backslash.</para>
    /// </summary>
    /// <returns>An array of section names. This can be empty, but not null.</returns>
    /// <since>6.0</since>
    public string[] GetSectionNames()
    {
      int count = Count;
      var section_dict = new SortedDictionary<string, bool>();
      for (int i = 0; i < count; i++)
      {
        string key = GetKey(i);
        if (string.IsNullOrEmpty(key))
          continue;
        int index = key.IndexOf("\\", StringComparison.Ordinal);
        if (index > 0)
        {
          string section = key.Substring(0, index);
          if (!section_dict.ContainsKey(section))
            section_dict.Add(section, true);
        }
      }
      return section_dict.Keys.ToArray();
    }

    /// <summary>
    /// Return list of all entry names for a given section of document strings in the 3dm file.
    /// </summary>
    /// <param name="section">The section from which to retrieve section names.</param>
    /// <returns>An array of section names. This can be empty, but not null.</returns>
    /// <since>6.0</since>
    public string[] GetEntryNames(string section)
    {
      section += "\\";
      int count = Count;
      var rc = new List<string>();
      for (int i = 0; i < count; i++)
      {
        string key = GetKey(i);
        if (key != null && key.StartsWith(section))
        {
          rc.Add(key.Substring(section.Length));
        }
      }
      return rc.ToArray();
    }

    /// <summary>
    /// Adds or sets a document string in the 3dm file.
    /// </summary>
    /// <param name="section">The section.</param>
    /// <param name="entry">The entry name.</param>
    /// <param name="value">The entry value.</param>
    /// <returns>
    /// The previous value if successful.
    /// </returns>
    /// <since>6.0</since>
    public string SetString(string section, string entry, string value)
    {
      string key = section;
      if (!string.IsNullOrEmpty(entry))
        key = section + "\\" + entry;
      var rc = GetValue(key);
      SetString(key, value);
      return rc;
    }

    /// <summary>
    /// Adds or sets a a document string in the 3dm file.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The entry value.</param>
    /// <returns>
    /// The previous value if successful.
    /// </returns>
    /// <since>6.0</since>
    public string SetString(string key, string value)
    {
      string rc = GetValue(key);
      IntPtr ptr_parent = m_parent.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_DocumentUserString_SetString(ptr_parent, key, value);
      return rc;
    }

    /// <summary>
    /// Removes document strings from the 3dm file.
    /// </summary>
    /// <param name="section">name of section to delete. If null, all sections will be deleted.</param>
    /// <param name="entry">name of entry to delete. If null, all entries will be deleted for a given section.</param>
    /// <since>6.0</since>
    public void Delete(string section, string entry)
    {
      if (null == section && null != entry)
        throw new ArgumentNullException(nameof(section), "'section' cannot be null if an 'entry' is passed");

      if (null == section)
      {
        foreach (var section_name in GetSectionNames())
          foreach (var entry_name in GetEntryNames(section_name))
            Delete(section_name + "\\" + entry_name);
      }
      else if (null == entry)
      {
        var entries = GetEntryNames(section);
        for (var i = 0; i < entries.Length; i++)
          Delete(section + "\\" + entries[i]);
        Delete(section);
      }
      else
      {
        Delete(section + "\\" + entry);
      }
    }

    /// <summary>
    /// Removes a document string from the 3dm file.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <since>6.0</since>
    public void Delete(string key)
    {
      IntPtr ptr_parent = m_parent.NonConstPointer();
      UnsafeNativeMethods.ONX_Model_DocumentUserString_Delete(ptr_parent, key);
    }
  }

}
