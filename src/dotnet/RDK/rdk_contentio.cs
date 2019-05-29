#if RHINO_SDK
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rhino.Render
{
  /// <summary>
  /// Used to import and export custom render content types such as
  /// materials, environments and textures.  You must override
  /// RenderPlugIn.RenderContentSerializers() and return an array of
  /// derived RenderContentSerializer class types to add to the content
  /// browsers.
  /// </summary>
  public abstract class RenderContentSerializer
  {
    //Currently IOPlugIn cannot be initialized from native IO plugins because we don't allow access
    //to the list - so you only get to define custom RhinoCommon ones.
    /// <summary>
    /// Protected constructor to be called from derived class
    /// </summary>
    /// <param name="fileExtension">
    /// File extension associated with this serialize object
    /// </param>
    /// <param name="contentKind">
    /// Type of content created when importing or exporting this file type.
    /// </param>
    /// <param name="canRead">
    /// If true then the file type can be imported and will be included in the
    /// file open box when importing the specified render content type.
    /// </param>
    /// <param name="canWrite">
    /// If true then the file type can be exported and will be included in the
    /// file save box when exporting the specified render content type.
    /// </param>
    protected RenderContentSerializer(string fileExtension, RenderContentKind contentKind, bool canRead, bool canWrite)
    {
      m_runtime_serial_number = m_current_serial_number++;
      m_all_custom_content_io_plugins.Add(m_runtime_serial_number, this);
      m_file_extension = fileExtension;
      m_content_kind = contentKind;
      m_can_read = canRead;
      m_can_write = canWrite;
    }

    /// <summary>
    /// Register the RenderContentSerializer
    /// </summary>
    /// <param name="id">
    /// Plug-in id
    /// </param>
    /// <return>True if registered successful and false if unsuccessful</return>
    public bool RegisterSerializer(Guid id)
    {
      // Make sure a file extension is provided and that it was not previously registered.
      var extension = FileExtension;
      if (string.IsNullOrEmpty(extension) || UnsafeNativeMethods.Rdk_RenderContentIo_IsExtensionRegistered(extension))
        return false;
      
      // Create a C++ object, attach it to the serialize object and call RhRdkAddExtension()
      // using the new C++ object.
      Construct(id);
      return true;
    }

    #region private members
    private readonly string m_file_extension;
    private readonly RenderContentKind m_content_kind;
    private readonly bool m_can_read;
    private readonly bool m_can_write;
    #endregion private members

    #region public properties
    /// <summary>
    /// File extension associated with this serialize object
    /// </summary>
    public string FileExtension { get { return m_file_extension; } }

    /// <summary>
    /// Type of content created when importing or exporting this file type.
    /// </summary>
    public RenderContentKind ContentType { get { return m_content_kind; } }

    /// <summary>
    /// If true then the file type can be imported and will be included in the
    /// file open box when importing the specified render content type.
    /// </summary>
    public bool CanRead { get { return m_can_read; } }

    /// <summary>
    /// If true then the file type can be exported and will be included in the
    /// file save box when exporting the specified render content type.
    /// </summary>
    public bool CanWrite { get { return m_can_write; } }
    #endregion public properties

    #region Abstract methods and properties
    /// <summary>
    /// Called to when importing a file, file should be parsed and converted to
    /// a valid RenderContent object.
    /// </summary>
    /// <param name="pathToFile">
    /// Full path of the file to load.
    /// </param>
    /// <returns>
    /// Returns a valid RenderContent object such as RenderMaterial if the file
    /// was successfully parsed otherwise returns null.
    /// </returns>
    public abstract RenderContent Read(String pathToFile);

    /// <summary>
    /// Called to save a custom RenderContent object as an external file.
    /// </summary>
    /// <param name="pathToFile">
    /// Full path of file to write
    /// </param>
    /// <param name="renderContent">
    /// Render content to save
    /// </param>
    /// <param name="previewArgs">
    /// Parameters used to generate a preview image which may be embedded in
    /// the exported file.
    /// </param>
    /// <returns></returns>
    public abstract bool Write(String pathToFile, RenderContent renderContent, CreatePreviewEventArgs previewArgs);
    
    /// <summary>
    /// English string describing this plug-in
    /// </summary>
    public abstract String EnglishDescription { get; }
    
    /// <summary>
    /// Localized plug-in description
    /// </summary>
    public virtual String LocalDescription { get { return EnglishDescription; } }
    #endregion Abstract methods and properties

    #region InternalRegistration

    private int m_runtime_serial_number;// = 0; initialized by runtime
    private static int m_current_serial_number = 1;
    private static readonly Dictionary<int, RenderContentSerializer> m_all_custom_content_io_plugins = new Dictionary<int, RenderContentSerializer>();

    private String m_ext = string.Empty;
    private bool m_load;
    private bool m_save;

    private RenderContentKind m_kind = RenderContentKind.None;

    internal void Destroy()
    {
      var success = m_all_custom_content_io_plugins.Remove(m_runtime_serial_number);
      Debug.Assert(success);
    }

    internal void Construct(Guid pluginId)
    {
      m_ext = FileExtension;
      m_load = CanRead;
      m_save = CanWrite;
      m_kind = ContentType;
      UnsafeNativeMethods.CRhCmnContentIOPlugIn_New(m_runtime_serial_number, m_ext, string.Empty, (int)m_kind, m_save, m_load, pluginId);
    }
    #endregion

    #region statics
    internal static RenderContentSerializer FromSerialNumber(int serial)
    {
      RenderContentSerializer rc;
      m_all_custom_content_io_plugins.TryGetValue(serial, out rc);
      return rc;
    }
    #endregion

    #region callbacks
    internal delegate void DeleteThisCallback(int serialNumber);
    internal static DeleteThisCallback m_DeleteThis = OnDeleteThis;
    static void OnDeleteThis(int serialNumber)
    {
      try
      {
        var io = FromSerialNumber(serialNumber);
        if (io != null)
        {
          io.Destroy();
        }
      }
      catch
      {
      }
    }

    internal delegate int LoadCallback(int serialNumber, IntPtr filename);
    internal static LoadCallback m_Load = OnLoad;
    static int OnLoad(int serialNumber, IntPtr filename)
    {
      try
      {
        var io = FromSerialNumber(serialNumber);
        if (io != null)
        {
          var _filename = Marshal.PtrToStringUni(filename);
          var content = io.Read(_filename);
          if (content != null)
            return content.RuntimeSerialNumber;
        }
      }
      catch
      {
      }
      return 0;
    }


    internal delegate int SaveCallback(int serialNumber, IntPtr filename, IntPtr contentPtr, IntPtr sceneServerPtr);
    internal static SaveCallback m_Save = OnSave;
    static int OnSave(int serialNumber, IntPtr filename, IntPtr contentPtr, IntPtr sceneServerPtr)
    {
      try
      {
        var io = FromSerialNumber(serialNumber);
        var content = RenderContent.FromPointer(contentPtr);

        CreatePreviewEventArgs pc = null;
        
        if (sceneServerPtr != IntPtr.Zero)
          pc = new CreatePreviewEventArgs(sceneServerPtr, IntPtr.Zero, new System.Drawing.Size(100, 100), PreviewSceneQuality.Full, UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.Other);
        
        if (io != null && content != null)
        {
          var _filename = Marshal.PtrToStringUni(filename);
          return (io.Write(_filename, content, pc) ? 1 : 0);
        }
      }
      catch
      {
      }
      return 0;
    }

    internal delegate void GetRenderContentIoStringCallback(int serialNumber, bool isName, IntPtr pOnWString);
    internal static GetRenderContentIoStringCallback m_GetRenderContentIoString = OnGetRenderContentIoString;
    static void OnGetRenderContentIoString(int serialNumber, bool local, IntPtr pOnWString)
    {
      try
      {
        var io = FromSerialNumber(serialNumber);
        if (io != null)
        {
          var str = local ? io.LocalDescription : io.EnglishDescription;
          if (!string.IsNullOrEmpty(str))
            UnsafeNativeMethods.ON_wString_Set(pOnWString, str);
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    #endregion

  }
}

#endif
