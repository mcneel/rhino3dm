#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.Collections;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Render;
using Rhino.Render.PostEffects;
using Rhino.Runtime.InteropWrappers;
using Rhino.DocObjects.Tables;
using Rhino.FileIO;
using System.Diagnostics;
using Rhino.Render.CustomRenderMeshes;

namespace Rhino.Commands
{
  /// <summary>
  /// Argument package that is passed to a custom undo delegate
  /// </summary>
  public class CustomUndoEventArgs : EventArgs
  {
    internal CustomUndoEventArgs(Guid commandId, string description, bool createdByRedo, uint eventSn, object tag, RhinoDoc doc)
    {
      CommandId = commandId;
      ActionDescription = description;
      CreatedByRedo = createdByRedo;
      UndoSerialNumber = eventSn;
      Tag = tag;
      Document = doc;
    }

    /// <since>5.0</since>
    public Guid CommandId { get; }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint UndoSerialNumber { get; }

    /// <since>5.0</since>
    public string ActionDescription { get; }

    /// <since>5.0</since>
    public bool CreatedByRedo { get; }

    /// <since>5.0</since>
    public object Tag { get; }

    /// <since>5.0</since>
    public RhinoDoc Document { get; }
  }
}

namespace Rhino
{
  class CustomUndoCallback
  {
    public CustomUndoCallback(uint serialNumber, EventHandler<Commands.CustomUndoEventArgs> handler, object tag, string description, RhinoDoc document)
    {
      Handler = handler;
      SerialNumber = serialNumber;
      Tag = tag;
      Description = description;
      Document = document;
    }
    public uint SerialNumber { get; }
    public EventHandler<Commands.CustomUndoEventArgs> Handler { get; }
    public object Tag { get; }
    public RhinoDoc Document { get; }
    public string Description { get; }
  }

  namespace Render
  {
    public class ImageFileEventArgs : EventArgs
    {
      internal ImageFileEventArgs(
        ImageFileEvent eventType,
        string fileName,
        string renderEngine,
        Guid renderEngineId,
        Guid sessionId,
        int ellapsedTime)
      {
        Event = eventType;
        FileName = fileName;
        RenderEngine = renderEngine;
        RenderEngineId = renderEngineId;
        SessionId = sessionId;
        EllapsedTime = ellapsedTime;
      }
      /// <since>5.11</since>
      public ImageFileEvent Event { get; private set; }
      /// <since>5.11</since>
      public string FileName { get; private set; }
      /// <since>5.11</since>
      public string RenderEngine { get; private set; }
      /// <since>5.11</since>
      public Guid RenderEngineId { get; private set; }
      /// <since>5.11</since>
      public Guid SessionId { get; private set; }
      /// <since>5.11</since>
      public int EllapsedTime { get; private set; }
    }

    /// <since>5.11</since>
    public enum ImageFileEvent
    {
      /// <summary>
      /// Render image file has been successfully written
      /// </summary>
      Saved,
      /// <summary>
      /// Render image file has been successfully loaded
      /// </summary>
      Loaded,
      /// <summary>
      /// Render image file was just deleted
      /// </summary>
      Deleted,
    }

    /// <summary>
    /// Controls interaction with RDK render image files
    /// </summary>
    public static class ImageFile
    {
      /// <summary>
      /// Render image file saved, happens when a rendering completes.
      /// If a plug-in needs to save additional file information it should
      /// write it to the same folder as the Rhino render image file.  Rhino
      /// will take care of deleting old data.
      /// </summary>
      /// <since>5.11</since>
      public static event EventHandler<ImageFileEventArgs> Saved
      {
        add
        {
          OnAddEvent();
          SavedEvent -= value;
          SavedEvent += value;
        }
        remove
        {
          SavedEvent -= value;
          OnRemoveEvent();
        }
      }
      private static event EventHandler<ImageFileEventArgs> SavedEvent;

      /// <summary>
      /// Generally called when the "RenderOpenLastRender" command is run,
      /// this event is raised after the render window has been created and the
      /// saved scene has been loaded.
      /// </summary>
      /// <since>5.11</since>
      public static event EventHandler<ImageFileEventArgs> Loaded
      {
        add
        {
          OnAddEvent();
          LoadedEvent -= value;
          LoadedEvent += value;
        }
        remove
        {
          LoadedEvent -= value;
          OnRemoveEvent();
        }
      }
      private static event EventHandler<ImageFileEventArgs> LoadedEvent;

      /// <summary>
      /// Called when the RDK is cleaning up old render image files, a
      /// plug-in should delete any plug-in specific image files at this
      /// time.
      /// </summary>
      /// <since>5.11</since>
      public static event EventHandler<ImageFileEventArgs> Deleted
      {
        add
        {
          OnAddEvent();
          DeletedEvent -= value;
          DeletedEvent += value;
        }
        remove
        {
          DeletedEvent -= value;
          OnRemoveEvent();
        }
      }
      private static event EventHandler<ImageFileEventArgs> DeletedEvent;
      
      private static void OnAddEvent()
      {
        if (g_on_render_image_event != null)
          return;
        lock (g_event_lock)
        {
          g_on_render_image_event = OnRenderImageEvent;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnRenderImageEventCallback(
            g_on_render_image_event,
            Runtime.HostUtils.m_rdk_ew_report);
        }
      }

      private static void OnRemoveEvent()
      {
        if (g_on_render_image_event == null || SavedEvent != null || LoadedEvent != null || DeletedEvent != null)
          return;
        lock (g_event_lock)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnRenderImageEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          g_on_render_image_event = null;
        }
      }

      private static readonly object g_event_lock = new object();
      internal delegate void OnRenderImageCallback(UnsafeNativeMethods.OnRenderingEventType eventType, IntPtr args);
      private static OnRenderImageCallback g_on_render_image_event;
      private static void OnRenderImageEvent(UnsafeNativeMethods.OnRenderingEventType eventType, IntPtr args)
      {
        var event_id = ImageFileEvent.Saved;
        // Convert the unmanaged event UUID to a RenderHistoryEvent value and bail
        // if the event callback has not been set
        switch (eventType)
        {
          case UnsafeNativeMethods.OnRenderingEventType.Saved:
            if (SavedEvent == null)
              return; // No events registered so bail
            break;
          case UnsafeNativeMethods.OnRenderingEventType.Loaded:
            if (LoadedEvent == null)
              return; // No events registered so bail
            event_id = ImageFileEvent.Loaded;
            break;
          case UnsafeNativeMethods.OnRenderingEventType.Deleted:
            if (DeletedEvent == null)
              return; // No events registered so bail
            event_id = ImageFileEvent.Deleted;
            break;
          default:
            throw new Exception("Unknown OnRenderingEventType");
        }
        // Get the unmanaged history event arguments
        using (var file_name_string_holder = new StringHolder())
        using (var render_engine_string_holder = new StringHolder())
        {
          var file_name_pointer = file_name_string_holder.NonConstPointer();
          var render_engine_pointer = render_engine_string_holder.NonConstPointer();
          var render_engine = Guid.Empty;
          var session_id = Guid.Empty;
          var elapsed_time = 0; // Legacy.
          var sta_time_ms = (ulong)0;
          var end_time_ms = (ulong)0;
          UnsafeNativeMethods.CRdkCmnEventWatcher_GetRenderingFileInfoArgs(
            args,
            file_name_pointer,
            render_engine_pointer,
            ref render_engine,
            ref session_id,
            ref elapsed_time,
            ref sta_time_ms,
            ref end_time_ms);
          // Data passed to the Rhino Common event
          var event_args = new ImageFileEventArgs(
            event_id,
            file_name_string_holder.ToString(),
            render_engine_string_holder.ToString(),
            render_engine,
            session_id,
            elapsed_time
//          sta_time_ms, // TODO: [MAXLOOK] Not sure if it would break the SDK.
//          end_time_ms, // TODO:
            );
          // Raise the event in RhinoCommon
          switch (event_id)
          {
            case ImageFileEvent.Saved:
              SavedEvent.Invoke(null, event_args);
              break;
            case ImageFileEvent.Loaded:
              LoadedEvent.Invoke(null, event_args);
              break;
            case ImageFileEvent.Deleted:
              DeletedEvent.Invoke(null, event_args);
              break;
          }
        }
      }
    }
  }

  /// <summary>
  /// Represents an active model.
  /// </summary>
  public sealed class RhinoDoc : IDisposable
  {
#region statics
    /// <summary>
    /// Do not make this public; it is used for legacy functions that assumed
    /// an ActiveDoc.  This gives me a single location to fix things since I
    /// can probably figure out a "scoped" document in many cases
    /// </summary>
    /// <param name="tolerance"></param>
    /// <param name="angleTolerance"></param>
    internal static void ActiveDocTolerances(out double tolerance, out double angleTolerance)
    {
      // 16 May 2013 - S. Baer
      // Dale Lear recommends that we always use constant tolerance values for the
      // deprecated functions that used ActiveDoc tolerances.
      tolerance = 0.001;
      angleTolerance = 0.5 * Math.PI / 180.0;
    }

    /// <summary>
    /// Opens a 3dm file and makes it the active document. If called on
    /// windows the active document will be saved and closed and the new
    /// document will be opened and become the active document.  If called
    /// on the Mac the file will be opened in a new document window.
    /// </summary>
    /// <param name="filePath">Full path to the 3dm file to open</param>
    /// <param name="wasAlreadyOpen">
    /// Will get set to true if there is a currently open document with the
    /// specified path; otherwise it will get set to false.
    /// </param>
    /// <returns>
    /// Returns the newly opened document on success or null on error.
    /// </returns>
    /// <since>6.0</since>
    public static RhinoDoc Open(string filePath, out bool wasAlreadyOpen)
    {
      wasAlreadyOpen = (null != FromFilePath(filePath));
      if (string.IsNullOrWhiteSpace(filePath))
        return null;
      if (!System.IO.File.Exists(filePath))
        return null;
      if (!UnsafeNativeMethods.CRhinoFileMenu_Open(filePath))
        return null;
      var doc = FromFilePath(filePath);
      return doc;
    }

    /// <summary>
    /// Search the open document list for a document with a Path equal
    /// to the specified file path.
    /// </summary>
    /// <returns>The file name to search for</returns>
    /// <param name="filePath">The full path to the file to search for.</param>
    /// <since>6.0</since>
    public static RhinoDoc FromFilePath(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
        return null;
      var open_docs = OpenDocuments(true);
      foreach (var doc in open_docs)
      {
        var path = doc == null ? string.Empty : doc.Path;
        if (filePath.Equals(path, StringComparison.Ordinal))
          return doc;
      }
      return null;
    }

    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("OpenFile is obsolete, use Open instead")]
    public static bool OpenFile(string path)
    {
      return UnsafeNativeMethods.CRhinoFileMenu_Open(path);
    }

    /// <since>5.0</since>
    public static bool ReadFile(string path, FileReadOptions options)
    {
      if (ActiveDoc == null)
        return false;

      IntPtr const_ptr_options = options.ConstPointer(true);
      return UnsafeNativeMethods.RHC_RhinoReadFile(ActiveDoc.RuntimeSerialNumber, path, const_ptr_options);
    }

    #endregion

    /// <summary>
    /// MAC ONLY, always returns false on Windows.
    /// Check to see if this document is in the process of being reverted
    /// indicating it will get closed when the revert is completed.
    /// </summary>
    /// <returns>
    /// MAC ONLY, always returns false on Windows.
    /// Returns true if running on Mac and the document is being reverted to a
    /// previous revision.
    /// </returns>
    internal bool IsReverting => UnsafeNativeMethods.CRhinoDoc_IsReverting(RuntimeSerialNumber);

    /// <since>7.0</since>
    public bool IsHeadless => UnsafeNativeMethods.CRhinoDoc_IsHeadless(RuntimeSerialNumber) != 0;

    /// <since>7.0</since>
    public void Dispose()
    {
      if (!IsHeadless)
        return; // Managed by Rhino

      if (RuntimeSerialNumber == 0)
        return; // Already disposed

      var activeDoc = ActiveDoc;
      bool changeActiveDoc = false;
      if (activeDoc != null && activeDoc.RuntimeSerialNumber == this.RuntimeSerialNumber)
        changeActiveDoc = true;

      GC.SuppressFinalize(this);

      UnsafeNativeMethods.CRhinoDoc_Delete(RuntimeSerialNumber);
      RuntimeSerialNumber = 0;

      if( changeActiveDoc )
      {
        RhinoDoc[] docs = OpenDocuments();
        if (docs.Length > 0)
          ActiveDoc = docs[0];
      }
    }

    ~RhinoDoc()
    {
      Debug.Assert(IsHeadless);

      if (!RhinoApp.IsRunningHeadless)
      {
        EventHandler FinalizeOnUIThread = null;
        RhinoApp.Idle += FinalizeOnUIThread = (sender, args) =>
        {
          RhinoApp.Idle -= FinalizeOnUIThread;

          // Destroy the object in UI thread if we are not in Headless mode
          UnsafeNativeMethods.CRhinoDoc_Delete(RuntimeSerialNumber);
        };
      }
      else
      {
        // Destroy the object here if we are in Headless mode
        UnsafeNativeMethods.CRhinoDoc_Delete(RuntimeSerialNumber);
      }
    }

    #region IO Methods

    /// <summary>
    /// Create a new headless RhinoDoc from a template file
    /// </summary>
    /// <param name="file3dmTemplatePath">
    /// Name of a Rhino model to use as a template to initialize the document.
    /// If null, an empty document is created
    /// </param>
    /// <returns>
    /// New RhinoDoc on success. Note that this is a "headless" RhinoDoc and it's
    /// lifetime is under your control. 
    /// </returns>
    /// <since>7.0</since>
    public static RhinoDoc CreateHeadless(string file3dmTemplatePath)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      if (!string.IsNullOrWhiteSpace(file3dmTemplatePath))
      {
        var info = new System.IO.FileInfo(file3dmTemplatePath);

        if (string.Compare(info.Extension, ".3DM", true) != 0)
          throw new ArgumentException("Template file path should have a 3DM extension", "file3DMTemplatePath");
      }

      uint serial_number = UnsafeNativeMethods.CRhinoDoc_New(file3dmTemplatePath);
      return RhinoDoc.FromRuntimeSerialNumber(serial_number);
    }

    /// <summary>
    /// Opens a 3DM file into a new headless RhinoDoc.
    /// </summary>
    /// <param name="file3dmPath">
    /// Path of a Rhino model to load.
    /// </param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static RhinoDoc OpenHeadless(string file3dmPath)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      if (file3dmPath != null)
      {
        var info = new System.IO.FileInfo(file3dmPath);

        if (string.Compare(info.Extension, ".3DM", true) != 0)
          throw new ArgumentException("Source file path should have a 3DM extension", "file3DMPath");
      }

      uint serial_number = UnsafeNativeMethods.CRhinoDoc_Load(file3dmPath);
      return RhinoDoc.FromRuntimeSerialNumber(serial_number);
    }

    /// <summary>
    /// Import geometry into a RhinoDoc from a file. This can be any file format
    /// that Rhino can import
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool Import(string filePath)
    {
      return Import(filePath, null);
    }

    /// <summary>
    /// Import geometry into a RhinoDoc from a file. This can be any file format
    /// that Rhino can import
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="System.IO.FileNotFoundException"></exception>
    /// <since>8.0</since>
    public bool Import(string filePath, ArchivableDictionary options)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      var fileInfo = new System.IO.FileInfo(filePath);

      if (!fileInfo.Exists)
        throw new System.IO.FileNotFoundException(string.Empty, filePath);

      using
      (
        var readOptions = new FileReadOptions()
        {
          ImportMode = true,
          BatchMode = true,
          UseScaleGeometry = true,
          ScaleGeometry = true
        }
      )
      {
        if (options!=null)
        {
          readOptions.OptionsDictionary.AddContentsFrom(options);
        }
        IntPtr const_ptr_options = readOptions.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoReadFile(RuntimeSerialNumber, filePath, const_ptr_options);
      }
    }
    /// <summary>
    /// Save doc to disk using the document's Path
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool Save()
    {
      if (string.IsNullOrEmpty(Path))
        throw new InvalidOperationException("RhinoDoc.Path has no value.");

      using
      (
        var options = new FileWriteOptions()
        {
          UpdateDocumentPath = true,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = false,
        }
      )
      {
        IntPtr const_ptr_options = options.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWrite3dmFile(RuntimeSerialNumber, Path, const_ptr_options);
      }
    }

    //public bool SaveIncremental()

    // 20 June 2019 S. Baer
    // Making private for now. I would like to figure out why we have a version parameter here.
    bool SaveSmall(int version = 0)
    {
      if (string.IsNullOrEmpty(Path))
        throw new InvalidOperationException("RhinoDoc.Path has no value.");

      using
      (
        var options = new FileWriteOptions()
        {
          UpdateDocumentPath = true,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = false,
          WriteGeometryOnly = true,
          FileVersion = version
        }
      )
      {
        IntPtr const_ptr_options = options.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWrite3dmFile(RuntimeSerialNumber, Path, const_ptr_options);
      }
    }

    /// <summary>
    /// Save doc as a 3dm to a specified path using the current Rhino file version
    /// </summary>
    /// <param name="file3dmPath"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool SaveAs(string file3dmPath)
    {
      return SaveAs(file3dmPath, 0);
    }

    /// <summary>
    /// Save doc as a 3dm to a specified path
    /// </summary>
    /// <param name="file3dmPath"></param>
    /// <param name="version">Rhino file version</param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool SaveAs(string file3dmPath, int version)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      var info = new System.IO.FileInfo(file3dmPath);

      if (string.Compare(info.Extension, ".3DM", true) != 0)
        throw new ArgumentException("Destination file path should have a 3DM extension", "file3DMPath");

      using
      (
        var options = new FileWriteOptions()
        {
          UpdateDocumentPath = true,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = false,
          FileVersion = version
        }
      )
      {
        IntPtr const_ptr_options = options.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWrite3dmFile(RuntimeSerialNumber, file3dmPath, const_ptr_options);
      }
    }

    /// <summary>
    /// Save this document as a template
    /// </summary>
    /// <param name="file3dmTemplatePath"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool SaveAsTemplate(string file3dmTemplatePath)
    {
      return SaveAsTemplate(file3dmTemplatePath, 0);
    }

    /// <summary>
    /// Save this document as a template to a specific Rhino file version
    /// </summary>
    /// <param name="file3dmTemplatePath"></param>
    /// <param name="version"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool SaveAsTemplate(string file3dmTemplatePath, int version)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      var info = new System.IO.FileInfo(file3dmTemplatePath);

      if (string.Compare(info.Extension, ".3DM", true) != 0)
        throw new ArgumentException("Destination file path should have a 3DM extension", "file3DMTemplatePath");

      using
      (
        var options = new FileWriteOptions()
        {
          UpdateDocumentPath = false,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = false,
          FileVersion = version
        }
      )
      {
        IntPtr const_ptr_options = options.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWrite3dmFile(RuntimeSerialNumber, file3dmTemplatePath, const_ptr_options);
      }
    }

    /// <summary>
    /// Export the entire document to a file. All file formats that Rhino can export to
    /// are supported by this function.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool Export(string filePath)
    {
      ArchivableDictionary dict = null;
      return Export(filePath, dict);
    }

    /// <summary>
    /// Export the entire document to a file. All file formats that Rhino can export to
    /// are supported by this function.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="options">
    /// Options to help define how data should be exported.
    /// </param>
    /// <returns>true on success</returns>
    /// <since>8.0</since>
    public bool Export(string filePath, ArchivableDictionary options)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      new System.IO.FileInfo(filePath);

      using
      (
        var fileWriteOptions = new FileWriteOptions()
        {
          UpdateDocumentPath = false,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = false,
        }
      )
      {
        if (options != null)
        {
          fileWriteOptions.OptionsDictionary.AddContentsFrom(options);
        }
        IntPtr const_ptr_options = fileWriteOptions.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWriteFile(RuntimeSerialNumber, filePath, const_ptr_options);
      }
    }

    /// <summary>
    /// Export selected geometry to a file. All file formats that Rhino can export
    /// to are supported by this function.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool ExportSelected(string filePath)
    {
      ArchivableDictionary dict = null;
      return ExportSelected(filePath, dict);
    }

    /// <summary>
    /// Export selected geometry to a file. All file formats that Rhino can export
    /// to are supported by this function.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="options">
    /// Options to help define how data should be exported.
    /// </param>
    /// <returns>true on success</returns>
    /// <since>8.0</since>
    public bool ExportSelected(string filePath, ArchivableDictionary options)
    {
      // This line checks filePath is a valid path, well formatted, not too long...
      new System.IO.FileInfo(filePath);

      using
      (
        var fileWriteOptions = new FileWriteOptions()
        {
          UpdateDocumentPath = false,
          SuppressAllInput = true,
          SuppressDialogBoxes = true,
          WriteSelectedObjectsOnly = true,
        }
      )
      {
        if (options!=null)
        {
          fileWriteOptions.OptionsDictionary.AddContentsFrom(options);
        }

        IntPtr const_ptr_options = fileWriteOptions.ConstPointer(true);
        return UnsafeNativeMethods.RHC_RhinoWriteFile(RuntimeSerialNumber, filePath, const_ptr_options);
      }
    }
    //public bool ExportWithOrigin(string filePath, Point3d Origin)

    /// <summary>
    /// Write information in this document to a file. 
    /// Note, the active document's name will be changed to that
    /// of the path provided.
    /// </summary>
    /// <param name="path">The name of the file to write.</param>
    /// <param name="options">The file writing options.</param>
    /// <returns>true if successful, false on failure.</returns>
    /// <remarks>
    /// This is the best choice for general file writing.
    /// It handles making backups using temporary files,
    /// locking and unlocking, loading file writing plug-ins,
    /// and many other details.
    /// </remarks>
    /// <since>5.0</since>
    public bool WriteFile(string path, FileWriteOptions options)
    {
      IntPtr const_ptr_options = options.ConstPointer(true);
      return UnsafeNativeMethods.RHC_RhinoWriteFile(RuntimeSerialNumber, path, const_ptr_options);
    }

    /// <summary>
    /// Write information in this document to a .3dm file. 
    /// Note, the active document's name will not be changed.
    /// </summary>
    /// <param name="path">The name of the .3dm file to write.</param>
    /// <param name="options">The file writing options.</param>
    /// <returns>true if successful, false on failure.</returns>
    /// <since>6.5</since>
    public bool Write3dmFile(string path, FileWriteOptions options)
    {
      IntPtr const_ptr_options = options.ConstPointer(true);
      return UnsafeNativeMethods.RHC_RhinoWrite3dmFile(RuntimeSerialNumber, path, const_ptr_options);
    }
    #endregion

    /// <summary>
    /// Search for a file using Rhino's search path.  Rhino will look in the
    /// following places:
    /// 1. Current model folder
    /// 2. Path specified in options dialog/File tab
    /// 3. Rhino system folders
    /// 4. Rhino executable folder
    /// </summary>
    /// <param name="filename"></param>
    /// <returns>
    /// Path to existing file if found, an empty string if no file was found
    /// </returns>
    /// <since>5.0</since>
    public string FindFile(string filename)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoFileUtilities_FindFile(filename, ptr_string);
        return sh.ToString();
      }
    }

    private RhinoDoc(uint serialNumber)
    {
      // cast to int until I can get all of the other calls to m_runtime_serial_number switched over
      RuntimeSerialNumber = serialNumber;

      if (!IsHeadless)
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a new RhinoDoc
    /// </summary>
    /// <param name="modelTemplateFileName">
    /// Name of a Rhino model to use as a template to initialize the document.
    /// If the template contains views, those views are created.
    /// If null, an empty document with no views is created
    /// </param>
    /// <returns></returns>
    /// <since>6.4</since>
    public static RhinoDoc Create(string modelTemplateFileName)
    {
      uint serial_number = UnsafeNativeMethods.CRhinoDoc_Create(modelTemplateFileName);
      return RhinoDoc.FromRuntimeSerialNumber(serial_number);
    }

    /// <summary>
    /// Returns a list of currently open Rhino documents
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static RhinoDoc[] OpenDocuments()
    {
      return OpenDocuments(false);
    }

    /// <summary>
    /// Returns a list of currently open Rhino documents
    /// </summary>
    /// <param name="includeHeadless">pass true to include headless docs in the list</param>
    /// <returns></returns>
    /// <since>7.2</since>
    public static RhinoDoc[] OpenDocuments(bool includeHeadless)
    {
      var list = new List<RhinoDoc>();
      var iterator = UnsafeNativeMethods.CRhinoDocIterator_New(includeHeadless);
      if (iterator != IntPtr.Zero)
      {
        try
        {
          for (var sn = UnsafeNativeMethods.RhinoDocIterator_FirstOrNext(iterator, 1); sn > 0; sn = UnsafeNativeMethods.RhinoDocIterator_FirstOrNext(iterator, 0))
          {
            var doc = RhinoDoc.FromRuntimeSerialNumber(sn);
            if (doc != null)
              list.Add(doc);
          }
        }
        finally
        {
          UnsafeNativeMethods.CRhinoDocIterator_Delete(iterator);
        }
      }

      return list.ToArray();
    }

    /// <summary>
    /// WARNING!! Do not use the ActiveDoc if you don't have to. Under Mac Rhino the ActiveDoc
    /// can change while a command is running. Use the doc that is passed to you in your RunCommand
    /// function or continue to use the same doc after the first call to ActiveDoc.
    /// </summary>
    /// <since>5.0</since>
    public static RhinoDoc ActiveDoc
    {
      get
      {
        uint id = UnsafeNativeMethods.CRhinoDoc_ActiveDocId();
        return FromRuntimeSerialNumber(id);
      }
      set
      {
        uint serialNumber = 0;
        if( value!=null )
          serialNumber = value.RuntimeSerialNumber;
        UnsafeNativeMethods.CRhinoDoc_MakeActive(serialNumber);
      }
    }

    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use FromRuntimeSerialNumber")]
    public static RhinoDoc FromId(int docId)
    {
      if( docId<0 )
        return null;
      return FromRuntimeSerialNumber((uint)docId);
    }

    static int HeadlessDocumentCountOnLastCull = 0;

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static RhinoDoc FromRuntimeSerialNumber(uint serialNumber)
    {
      if (serialNumber == 0)
        return null;

      RhinoDoc value = null;
      int headless = UnsafeNativeMethods.CRhinoDoc_IsHeadless(serialNumber);

      if (headless == 1)
      {
        lock (HeadlessDocuments)
        {
          if (HeadlessDocuments.TryGetValue(serialNumber, out WeakReference<RhinoDoc> reference))
          {
            reference.TryGetTarget(out value);
            // if 'value' is null here means the object is collected
          }
          else
          {
            value = new RhinoDoc(serialNumber);
            HeadlessDocuments[serialNumber] = new WeakReference<RhinoDoc>(value);
          }

          // If we have more than 1/8 potential garbage entries we try to cull those entries
          if (HeadlessDocuments.Count - HeadlessDocumentCountOnLastCull > HeadlessDocuments.Count / 8)
          {
            // Remove collected entries
            foreach (var key in HeadlessDocuments.Where(x => !x.Value.TryGetTarget(out RhinoDoc doc)).Select(x => x.Key).ToArray())
              HeadlessDocuments.Remove(key);

            HeadlessDocumentCountOnLastCull = HeadlessDocuments.Count;
          }
        }
      }
      else if (headless == 0)
      {
        // Hook document closed to remove closed RhinoDoc objects from the
        // runtime dictionary
        if (g_on_close_doc_remove == null)
        {
          g_on_close_doc_remove = OnCloseDocRemoveFromDictionary;
          CloseDocument += g_on_close_doc_remove;
        }

        // Check to see if there is a document or create and register a new one
        if (!UIDocuments.TryGetValue(serialNumber, out value))
        {
          UIDocuments[serialNumber] = value = new RhinoDoc(serialNumber);
        }
      }

      return value;
    }

    static readonly Dictionary<uint, RhinoDoc> UIDocuments = new Dictionary<uint, RhinoDoc>();
    static readonly Dictionary<uint, WeakReference<RhinoDoc>> HeadlessDocuments = new Dictionary<uint, WeakReference<RhinoDoc>>();

    private static EventHandler<DocumentEventArgs> g_on_close_doc_remove;
    static void OnCloseDocRemoveFromDictionary(object sender, DocumentEventArgs e)
    {
      // If the document runtime serial number is in the dictionary then remove it
      if (UIDocuments.ContainsKey(e.DocumentSerialNumber))
        UIDocuments.Remove(e.DocumentSerialNumber);

      // If all runtime documents are closed then remove the CloseDocument hook
      if (UIDocuments.Count < 1)
      {
        CloseDocument -= g_on_close_doc_remove;
        // Setting the hook to null will signal FromRuntimeSerialNumber to set the
        // hook the next time a runtime document object is created
        g_on_close_doc_remove = null;
      }
    }

    public override int GetHashCode()
    {
      return (int)RuntimeSerialNumber;
    }

    public override bool Equals(object obj)
    {
      var other = obj as RhinoDoc;
      return (other != null && other.RuntimeSerialNumber == RuntimeSerialNumber);
    }
#region docproperties
    string GetString(int which)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_GetSetString(RuntimeSerialNumber, which, false, null, ptr_string);
        return sh.ToString();
      }
    }
    //const int idxName = 0;
    const int IDX_PATH = 1;
    //const int idxUrl = 2;
    const int IDX_NOTES = 3;
    const int IDX_TEMPLATE_FILE_USED = 4;

    ///<summary>Returns the name of the currently loaded Rhino document (3DM file).</summary>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        string path = Path;
        if (!string.IsNullOrEmpty(path))
        {
          path = System.IO.Path.GetFileName(path);
        }
        return path;
      }
    }

    ///<summary>Returns the path of the currently loaded Rhino document (3DM file).</summary>
    /// <since>5.0</since>
    public string Path
    {
      get
      {
        return GetString(IDX_PATH);
      }
    }
    /*
        ///<summary>
        ///Returns or sets the uniform resource locator (URL) of the currently
        ///loaded Rhino document (3DM file).
        ///</summary>
        public string URL
        {
          get { return GetString(idxUrl); }
          set { CRhinoDoc_GetSetString(m_doc.RuntimeSerialNumber, idxUrl, true, value); }
        }
    */
    ///<summary>Returns or sets the document&apos;s notes.</summary>
    /// <since>5.0</since>
    public string Notes
    {
      get { return GetString(IDX_NOTES); }
      set { UnsafeNativeMethods.CRhinoDoc_GetSetString(RuntimeSerialNumber, IDX_NOTES, true, value, IntPtr.Zero); }
    }

    /// <since>5.0</since>
    public DateTime DateCreated
    {
      get
      {
        int year = 0;
        int month = 0;
        int day = 0;
        int hour = 0;
        int minute = 0;
        UnsafeNativeMethods.CRhinoDoc_GetRevisionDate(RuntimeSerialNumber, ref year, ref month, ref day, ref hour, ref minute, true);
        if (year < 1980)
          return DateTime.MinValue;
        return new DateTime(year, month, day, hour, minute, 0);
      }
    }

    /// <summary>
    /// Returns the date the document was created in Coordinated Universal Time (UTC).
    /// Use DateTime.ToLocalTime to convert to local time.
    /// </summary>
    /// <since>5.0</since>
    public DateTime DateLastEdited
    {
      get
      {
        int year = 0;
        int month = 0;
        int day = 0;
        int hour = 0;
        int minute = 0;
        UnsafeNativeMethods.CRhinoDoc_GetRevisionDate(RuntimeSerialNumber, ref year, ref month, ref day, ref hour, ref minute, false);
        if (year < 1980)
          return DateTime.MinValue;
        return new DateTime(year, month, day, hour, minute, 0);
      }
    }

    /// <summary>
    /// Get the ID of the active command.
    /// </summary>
    /// <since>8.0</since>
    public Guid ActiveCommandId
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDoc_ActiveCommandId(RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Returns the active plane of Rhino's auto-gumball widget.
    /// Note, when calling from a Rhino command, make sure the command 
    /// class has the Rhino.Commands.Style.Transparent command style attribute.
    /// </summary>
    /// <param name="plane">The active plane.</param>
    /// <returns>true if the auto-gumball widget is enabled and visible. False otherwise.</returns>
    /// <since>6.0</since>
    public bool GetGumballPlane(out Plane plane)
    {
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoDoc_GetGumballPlane(RuntimeSerialNumber, ref plane);
    }

    double GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts which)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_GetSetDouble(RuntimeSerialNumber, which, false, 0.0);
    }
    void SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts which, double val)
    {
      UnsafeNativeMethods.CRhinoDocProperties_GetSetDouble(RuntimeSerialNumber, which, true, val);
    }

    internal const double DefaultModelAbsoluteTolerance = 0.001;
    internal const double DefaultModelAngleToleranceRadians = Math.PI / 180.0; 

    /// <summary>Model space absolute tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAbsoluteTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelAbsTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelAbsTol, value); }
    }
    /// <summary>Model space angle tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAngleToleranceRadians
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelAngleTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelAngleTol, value); }
    }
    /// <summary>Model space angle tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAngleToleranceDegrees
    {
      get
      {
        double rc = ModelAngleToleranceRadians;
        rc = RhinoMath.ToDegrees(rc);
        return rc;
      }
      set
      {
        double radians = RhinoMath.ToRadians(value);
        ModelAngleToleranceRadians = radians;
      }
    }
    /// <summary>Model space relative tolerance.</summary>
    /// <since>5.0</since>
    public double ModelRelativeTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelRelTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelRelTol, value); }
    }

    /// <example>
    /// <code source='examples\vbnet\ex_displayprecision.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_displayprecision.cs' lang='cs'/>
    /// <code source='examples\py\ex_displayprecision.py' lang='py'/>
    /// </example>
    /// <since>5.8</since>
    public int ModelDistanceDisplayPrecision
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_DistanceDisplayPrecision(RuntimeSerialNumber, true, 0, false); }
      set { UnsafeNativeMethods.CRhinoDocProperties_DistanceDisplayPrecision(RuntimeSerialNumber, true, value, true); }
    }

    /// <since>5.8</since>
    public int PageDistanceDisplayPrecision
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_DistanceDisplayPrecision(RuntimeSerialNumber, false, 0, false); }
      set { UnsafeNativeMethods.CRhinoDocProperties_DistanceDisplayPrecision(RuntimeSerialNumber, false, value, true); }
    }

    /// <summary>Page space absolute tolerance.</summary>
    /// <since>5.0</since>
    public double PageAbsoluteTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageAbsTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageAbsTol, value); }
    }
    /// <summary>Page space angle tolerance.</summary>
    /// <since>5.0</since>
    public double PageAngleToleranceRadians
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageAngleTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageAngleTol, value); }
    }
    /// <summary>Page space angle tolerance.</summary>
    /// <since>5.0</since>
    public double PageAngleToleranceDegrees
    {
      get
      {
        double rc = PageAngleToleranceRadians;
        rc = RhinoMath.ToDegrees(rc);
        return rc;
      }
      set
      {
        double radians = RhinoMath.ToRadians(value);
        PageAngleToleranceRadians = radians;
      }
    }
    /// <summary>Page space relative tolerance.</summary>
    /// <since>5.0</since>
    public double PageRelativeTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageRelTol); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.PageRelTol, value); }
    }

    /// <summary>
    /// The base point in the model that is used when inserting the model into another as a block definition.
    /// By default the base point in any model is 0,0,0.
    /// </summary>
    /// <since>6.10</since>
    public Point3d ModelBasepoint
    {
      get
      {
        Point3d rc = Point3d.Origin;
        UnsafeNativeMethods.CRhinoDocProperties_ModelBasePoint(RuntimeSerialNumber, false, ref rc);
        return rc;
      }
      set
      {
        UnsafeNativeMethods.CRhinoDocProperties_ModelBasePoint(RuntimeSerialNumber, true, ref value);
      }
    }

    internal bool GetBool(UnsafeNativeMethods.DocumentStatusBool which)
    {
      return UnsafeNativeMethods.CRhinoDoc_GetSetBool(RuntimeSerialNumber, which, false, false);
    }

    /// <summary>
    /// Returns or sets the document's modified flag.
    /// </summary>
    /// <since>5.0</since>
    public bool Modified
    {
      get { return GetBool(UnsafeNativeMethods.DocumentStatusBool.idxModified); }
      set { UnsafeNativeMethods.CRhinoDoc_GetSetBool(RuntimeSerialNumber, UnsafeNativeMethods.DocumentStatusBool.idxModified, true, value); }
    }

    /// <summary>
    /// Returns or sets the appearance of all SubD objects in the document.
    /// </summary>
    /// <remarks>
    /// When setting this property, if the document contains SubD objects, then the document will be redrawn automatically.
    /// </remarks>
    /// <since>7.5</since>
    public Rhino.Geometry.SubDComponentLocation SubDAppearance
    {
      get { return UnsafeNativeMethods.CRhinoDoc_SubDAppearance(RuntimeSerialNumber); }
      set { UnsafeNativeMethods.CRhinoDoc_SetSubDAppearance(RuntimeSerialNumber, value); }
    }

    ///<summary>
    ///Returns the file version of the current document.  
    ///Use this function to determine which version of Rhino last saved the document.
    ///</summary>
    ///<returns>
    ///The file version (e.g. 1, 2, 3, 4, etc.) or -1 if the document has not been read from disk.
    ///</returns>
    /// <since>5.0</since>
    public int ReadFileVersion()
    {
      return UnsafeNativeMethods.CRhinoDocProperties_ReadFileVersion(RuntimeSerialNumber);
    }

    /// <since>5.0</since>
    public UnitSystem ModelUnitSystem
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_GetUnitSystem(RuntimeSerialNumber, true); }
      set { UnsafeNativeMethods.CRhinoDocProperties_SetUnitSystem(RuntimeSerialNumber, true, value); }
    }

    /// <since>5.0</since>
    public string GetUnitSystemName(bool modelUnits, bool capitalize, bool singular, bool abbreviate)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoDocProperties_GetUnitSystemName(RuntimeSerialNumber, modelUnits, capitalize, singular, abbreviate, ptr_string);
        return sh.ToString();
      }
    }

    /// <since>5.0</since>
    public void AdjustModelUnitSystem(UnitSystem newUnitSystem, bool scale)
    {
      UnsafeNativeMethods.CRhinoDocProperties_AdjustUnitSystem(RuntimeSerialNumber, true, newUnitSystem, scale);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public UnitSystem PageUnitSystem
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_GetUnitSystem(RuntimeSerialNumber, false); }
      set { UnsafeNativeMethods.CRhinoDocProperties_SetUnitSystem(RuntimeSerialNumber, false, value); }
    }

    /// <since>5.0</since>
    public void AdjustPageUnitSystem(UnitSystem newUnitSystem, bool scale)
    {
      UnsafeNativeMethods.CRhinoDocProperties_AdjustUnitSystem(RuntimeSerialNumber, false, newUnitSystem, scale);
    }

    /// <since>5.0</since>
    public int DistanceDisplayPrecision => ModelDistanceDisplayPrecision;

    /// <summary>
    /// Get the custom unit system name and custom unit scale.
    /// </summary>
    /// <param name="modelUnits">
    /// Set true to get values from the document's model unit system.
    /// Set false to get values from the document's page unit system.
    /// </param>
    /// <param name="customUnitName">The custom unit system name.</param>
    /// <param name="metersPerCustomUnit">The meters per custom unit scale.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.9</since>
    public bool GetCustomUnitSystem(bool modelUnits, out string customUnitName, out double metersPerCustomUnit)
    {
      customUnitName = null;
      metersPerCustomUnit = RhinoMath.UnsetValue;
      var rc = false;
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        rc = UnsafeNativeMethods.CRhinoDocProperties_GetCustomUnitSystem(RuntimeSerialNumber, modelUnits, ptr_string, ref metersPerCustomUnit);
        if (rc)
          customUnitName = sh.ToString();
      }
      return rc;
    }

    /// <summary>
    /// Changes the unit system to custom units and sets the custom unit scale.
    /// </summary>
    /// <param name="modelUnits">
    /// Set true to set values from the document's model unit system.
    /// Set false to set values from the document's page unit system.
    /// </param>
    /// <param name="customUnitName">The custom unit system name.</param>
    /// <param name="metersPerCustomUnit">The meters per custom unit scale.</param>
    /// <param name="scale">Set true to scale existing objects.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.9</since>
    public bool SetCustomUnitSystem(bool modelUnits, string customUnitName, double metersPerCustomUnit, bool scale)
    {
      if (string.IsNullOrEmpty(customUnitName))
        throw new ArgumentNullException(nameof(customUnitName));
      return UnsafeNativeMethods.CRhinoDocProperties_SetCustomUnitSystem(RuntimeSerialNumber, modelUnits, customUnitName, metersPerCustomUnit, scale);
    }

    #endregion

    /// <summary>
    /// Call this method to get string representing the specified value using
    /// the documents display coordinate system and display precision.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="appendUnitSystemName"></param>
    /// <param name="abbreviate"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public string FormatNumber(double value, bool appendUnitSystemName, bool abbreviate)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoFormatNumberUseDocSettingsEx(RuntimeSerialNumber, value, appendUnitSystemName, abbreviate, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Call this method to get string representing the specified value using
    /// the documents display coordinate system and display precision.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public string FormatNumber(double value)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoFormatNumberUseDocSettings(RuntimeSerialNumber, value, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Get the space associated with the active view for this document
    /// </summary>
    /// <since>8.0</since>
    public ActiveSpace ActiveSpace => (ActiveSpace)UnsafeNativeMethods.RHC_GetActiveSpce(RuntimeSerialNumber);

    /// <summary>
    /// Current read-only mode for this document.
    /// true if the document can be viewed but NOT saved.
    /// false if document can be viewed and saved.
    /// </summary>
    /// <since>5.0</since>
    public bool IsReadOnly => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsDocumentReadOnly);

    /// <summary>
    /// Check to see if the file associated with this document is locked.  If it is
    /// locked then this is the only document that will be able to write the file.  Other
    /// instances of Rhino will fail to write this document.
    /// </summary>
    /// <since>5.0</since>
    public bool IsLocked => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsDocumentLocked);

    /// <since>6.0</since>
    public bool IsInitializing => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsInitializing);

    /// <since>6.0</since>
    public bool IsCreating => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsCreating);

    /// <since>6.0</since>
    public bool IsOpening => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsOpening);

    /// <since>6.0</since>
    public bool IsAvailable => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsAvailable);

    /// <since>6.0</since>
    public bool IsClosing => GetBool(UnsafeNativeMethods.DocumentStatusBool.IsClosing);

    /// <summary>
    /// Gets the Document Id.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use RuntimeSerialNumber instead")]
    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int DocumentId => (int)RuntimeSerialNumber;

    /// <summary>
    /// Unique serialNumber for the document while the application is running.
    /// This is not a persistent value.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber { get; private set; }

    /// <since>5.0</since>
    public DocObjects.EarthAnchorPoint EarthAnchorPoint
    {
      get { return new DocObjects.EarthAnchorPoint(this); }
      set
      {
        IntPtr const_ptr_anchor = value.ConstPointer();
        UnsafeNativeMethods.CRhinoDocProperties_SetEarthAnchorPoint(RuntimeSerialNumber, const_ptr_anchor);
      }
    }

    /// <since>5.0</since>
    public RenderSettings RenderSettings
    {
      get { return new RenderSettings(this); }
      set
      {
        IntPtr const_ptr_rendersettings = value.ConstPointer();
        UnsafeNativeMethods.CRhinoDocProperties_SetRenderSettings(RuntimeSerialNumber, const_ptr_rendersettings);
      }
    }

    /// <since>6.11</since>
    public AnimationProperties AnimationProperties
    {
      get { return new AnimationProperties(this); }
      set
      {
        IntPtr const_ptr = value.ConstPointer();
        UnsafeNativeMethods.CRhinoDocProperties_SetAnimationProperties(RuntimeSerialNumber, const_ptr);
      }
    }

    /// <since>6.0</since>
    public List<System.Drawing.Size> CustomRenderSizes
    {
      get
      {
        var list = new List<System.Drawing.Size>();
        int width = 0, height = 0, index = 0;
        while (UnsafeNativeMethods.CRhinoDocProperties_GetCustomRenderSizes(RuntimeSerialNumber, index++, ref width, ref height))
        {
          list.Add(new System.Drawing.Size(width, height));
        }
        return list;
      }
    }

    /// <summary>
    /// Type of MeshingParameters currently used by the document to mesh objects
    /// </summary>
    /// <since>5.1</since>
    public MeshingParameterStyle MeshingParameterStyle
    {
      get
      {
        int rc = UnsafeNativeMethods.CRhinoDocProperties_GetRenderMeshStyle(RuntimeSerialNumber);
        return (MeshingParameterStyle)rc;
      }
      set
      {
        if (!UnsafeNativeMethods.CRhinoDocProperties_SetRenderMeshStyle(RuntimeSerialNumber, (int)value))
        {
          if (!Enum.IsDefined(typeof(MeshingParameterStyle), value) || value == MeshingParameterStyle.None)
          {
            throw new ArgumentOutOfRangeException("value", "The MeshingParameterStyle is invalid.");
          }
          else throw new NotSupportedException("Impossible to set Render Mesh Style.");
        }
      }
    }

    /// <summary>
    /// Get MeshingParameters currently used by the document
    /// </summary>
    /// <param name="style"></param>
    /// <returns></returns>
    /// <since>5.1</since>
    public MeshingParameters GetMeshingParameters(MeshingParameterStyle style)
    {
      IntPtr ptr_meshingparameters = UnsafeNativeMethods.CRhinoDocProperties_GetRenderMeshParameters(RuntimeSerialNumber, (int)style);
      if (IntPtr.Zero == ptr_meshingparameters)
        return null;
      return new MeshingParameters(ptr_meshingparameters);
    }


    /// <summary>
    /// Get analysis meshing parameters currently used by the document
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public MeshingParameters GetAnalysisMeshingParameters()
    {
      IntPtr ptr_meshingparameters = UnsafeNativeMethods.CRhinoDocProperties_GetAnalysisMeshParameters(RuntimeSerialNumber);
      if (IntPtr.Zero == ptr_meshingparameters)
        return null;
      return new MeshingParameters(ptr_meshingparameters);
    }

    /// <summary>
    /// Set the custom meshing parameters that this document will use. You must also modify the
    /// MeshingParameterStyle property on the document to Custom if you want these meshing
    /// parameters to be used
    /// </summary>
    /// <param name="mp"></param>
    /// <since>5.1</since>
    public void SetCustomMeshingParameters(MeshingParameters mp)
    {
      IntPtr const_ptr_meshingparameters = mp.ConstPointer();
      UnsafeNativeMethods.CRhinoDocProperties_SetCustomRenderMeshParameters(RuntimeSerialNumber, const_ptr_meshingparameters);
    }

    /// <summary>
    /// Get the custom meshing parameters that this document will use.
    /// </summary>
    /// <since>6.0</since>
    public MeshingParameters GetCurrentMeshingParameters()
    {
      return MeshingParameters.DocumentCurrentSetting(this);
    }

    /// <summary>
    /// The scale factor for hatches in model space when Hatch Scaling is enabled
    /// </summary>
    /// <since>6.1</since>
    public double ModelSpaceHatchScale
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelSpaceHatchScale); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelSpaceHatchScale, value); }
    }

    /// <summary>
    /// True if hatch scaling is enabled, false if not.
    /// </summary>
    /// <since>6.16</since>
    public bool ModelSpaceHatchScalingEnabled
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_IsHatchScalingEnabled(RuntimeSerialNumber); }
      set { UnsafeNativeMethods.CRhinoDocProperties_EnableHatchScaling(RuntimeSerialNumber, value); }
    }

    /// <summary>
    /// The scale factor for text in model space when Annotation Scaling is enabled
    /// </summary>
    /// <since>6.1</since>
    public double ModelSpaceTextScale
    {
      get { return GetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelSpaceTextScale); }
      set { SetDouble(UnsafeNativeMethods.CRhDocPropertiesDoubleConsts.ModelSpaceTextScale, value); }
    }

    /// <summary>
    /// If ModelSpaceAnnotationScaling is on, sizes in dimstyles are multiplied by 
    /// dimscale when the annotation is displayed in a model space viewport not in a detail
    /// </summary>
    /// <since>6.0</since>
    public bool ModelSpaceAnnotationScalingEnabled
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_IsModelSpaceAnnotationScalingEnabled(RuntimeSerialNumber); }
      set { UnsafeNativeMethods.CRhinoDocProperties_SetModelSpaceAnnotationScaleEnabled(RuntimeSerialNumber, value); }
    }

    /// <summary>
    /// If LayoutSpaceAnnotationScaling is on, sizes in dimstyles are multiplied by 
    /// dimscale when the annotation is displayed in a detail viewport not in a detail
    /// </summary>
    /// <since>6.0</since>
    public bool LayoutSpaceAnnotationScalingEnabled
    {
      get { return UnsafeNativeMethods.CRhinoDocProperties_IsLayoutSpaceAnnotationScalingEnabled(RuntimeSerialNumber); }
      set { UnsafeNativeMethods.CRhinoDocProperties_SetLayoutSpaceAnnotationScaleEnabled(RuntimeSerialNumber, value); }
    }

    private Worksession m_worksession;
    /// <summary>
    /// Provides access to the document's worksession.
    /// </summary>
    /// <since>6.0</since>
    public Worksession Worksession => m_worksession ?? (m_worksession = new Worksession(this));

    #region tables
    private ViewTable m_view_table;
    /// <since>5.0</since>
    public ViewTable Views => m_view_table ?? (m_view_table = new ViewTable(this));

    private ObjectTable m_object_table;
    /// <since>5.0</since>
    public ObjectTable Objects => m_object_table ?? (m_object_table = new ObjectTable(this));

    private ManifestTable m_manifest_table;
    /// <since>6.0</since>
    public ManifestTable Manifest => m_manifest_table ?? (m_manifest_table = new RhinoDocManifestTable(this));

    /// <summary>
    /// Gets the default object attributes for this document. 
    /// The attributes will be linked to the currently active layer 
    /// and they will inherit the Document WireDensity setting.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_objectdecoration.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectdecoration.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectdecoration.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public DocObjects.ObjectAttributes CreateDefaultAttributes()
    {
      var rc = new DocObjects.ObjectAttributes();
      IntPtr ptr_attributes = rc.NonConstPointer();
      UnsafeNativeMethods.CRhinoDoc_GetDefaultObjectAttributes(RuntimeSerialNumber, ptr_attributes);
      return rc;
    }

    private BitmapTable m_bitmap_table;
    /// <summary>
    /// bitmaps used in textures, backgrounds, wallpapers, ...
    /// </summary>
    /// <since>5.0</since>
    public BitmapTable Bitmaps => m_bitmap_table ?? (m_bitmap_table = new BitmapTable(this));

    //[skipping]
    //  CRhinoTextureMappingTable m_texture_mapping_table;

    private MaterialTable m_material_table;

    /// <summary>Materials in the document.</summary>
    /// <since>5.0</since>
    public MaterialTable Materials => m_material_table ?? (m_material_table = new MaterialTable(this));

    private LinetypeTable m_linetype_table;
    /// <summary>
    /// Linetypes in the document.
    /// </summary>
    /// <since>5.0</since>
    public LinetypeTable Linetypes => m_linetype_table ?? (m_linetype_table = new LinetypeTable(this));

    private LayerTable m_layer_table;
    /// <summary>
    /// Layers in the document.
    /// </summary>
    /// <since>5.0</since>
    public LayerTable Layers => m_layer_table ?? (m_layer_table = new LayerTable(this));

    private GroupTable m_group_table;
    /// <example>
    /// <code source='examples\vbnet\ex_addobjectstogroup.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addobjectstogroup.cs' lang='cs'/>
    /// <code source='examples\py\ex_addobjectstogroup.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GroupTable Groups => m_group_table ?? (m_group_table = new GroupTable(this));

    private FontTable m_font_table;
    
    /// <since>5.0</since>
    [Obsolete("Use DimStyles table instead")]
    public FontTable Fonts => m_font_table ?? (m_font_table = new FontTable(this));

    private DimStyleTable m_dimstyle_table;
    /// <example>
    /// <code source='examples\vbnet\ex_dimstyle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dimstyle.cs' lang='cs'/>
    /// <code source='examples\py\ex_dimstyle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public DimStyleTable DimStyles => m_dimstyle_table ?? (m_dimstyle_table = new DimStyleTable(this));

    private LightTable m_light_table;
    /// <since>5.0</since>
    public LightTable Lights => m_light_table ?? (m_light_table = new LightTable(this));

    private HatchPatternTable m_hatchpattern_table;
    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public HatchPatternTable HatchPatterns => m_hatchpattern_table ?? (m_hatchpattern_table = new HatchPatternTable(this));

    private InstanceDefinitionTable m_instance_definition_table;

    /// <example>
    /// <code source='examples\vbnet\ex_printinstancedefinitiontree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_printinstancedefinitiontree.cs' lang='cs'/>
    /// <code source='examples\py\ex_printinstancedefinitiontree.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public InstanceDefinitionTable InstanceDefinitions => m_instance_definition_table ?? (m_instance_definition_table = new InstanceDefinitionTable(this));

    //[skipping]
    //  CRhinoHistoryRecordTable m_history_record_table;

    private NamedConstructionPlaneTable m_named_cplane_table;
    /// <summary>
    /// Collection of named construction planes.
    /// </summary>
    /// <since>5.0</since>
    public NamedConstructionPlaneTable NamedConstructionPlanes => m_named_cplane_table ?? (m_named_cplane_table = new NamedConstructionPlaneTable(this));

    private NamedViewTable m_named_view_table;
    /// <summary>
    /// Collection of named views.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public NamedViewTable NamedViews => m_named_view_table ?? (m_named_view_table = new NamedViewTable(this));

    private StringTable m_strings;

    /// <summary>
    /// Collection of document user data strings in this document
    /// </summary>
    /// <since>5.0</since>
    public StringTable Strings => m_strings ?? (m_strings = new StringTable(this));

    /// <summary>
    /// Collection of document runtime data. This is a good place to
    /// put non-serializable, per document data, such as panel view models.  
    /// Note well: This data will be dispose with the document and does not
    /// get serialized.
    /// </summary>
    /// <value>The runtime document data table.</value>
    /// <since>6.3</since>
    public RuntimeDocumentDataTable RuntimeData => m_runtime_data ?? (m_runtime_data = new RuntimeDocumentDataTable(this));
    RuntimeDocumentDataTable m_runtime_data;

    //private TextStyleTable m_textstyle_table;
    //public TextStyleTable TextStyleTable
    //{
    //  get { return m_textstyle_table ?? (m_textstyle_table = new TextStyleTable(this)); }
    //}

    private NamedPositionTable m_named_position_table;
    /// <summary>
    /// Collection of named positions.
    /// </summary>
    /// <since>6.0</since>
    public NamedPositionTable NamedPositions => m_named_position_table ?? (m_named_position_table = new NamedPositionTable(this));

    private SnapshotTable m_snapshot_table;
    /// <summary>
    /// Collection of snapshots.
    /// </summary>
    /// <since>6.7</since>
    public SnapshotTable Snapshots => m_snapshot_table ?? (m_snapshot_table = new SnapshotTable(this));

    private NamedLayerStateTable m_named_layer_state_table;
    /// <summary>
    /// Collection of named layer states.
    /// </summary>
    /// <since>6.14</since>
    public NamedLayerStateTable NamedLayerStates => m_named_layer_state_table ?? (m_named_layer_state_table = new NamedLayerStateTable(this));

    #endregion

    private RenderMaterialTable m_render_materials;
    /// <since>5.7</since>
    public RenderMaterialTable RenderMaterials => (m_render_materials ?? (m_render_materials = new RenderMaterialTable(this)));
    private RenderEnvironmentTable m_render_environments;
    /// <since>5.7</since>
    public RenderEnvironmentTable RenderEnvironments => (m_render_environments ?? (m_render_environments = new RenderEnvironmentTable(this)));
    private RenderTextureTable m_render_textures;
    /// <since>5.7</since>
    public RenderTextureTable RenderTextures => (m_render_textures ?? (m_render_textures = new RenderTextureTable(this)));

    /// <summary>
    /// Access to the current environment for various uses
    /// </summary>
    /// <since>6.0</since>
    [Obsolete("Please use Rhino.Render.RenderSettings methods")]
    public ICurrentEnvironment CurrentEnvironment => new CurrentEnvironmentImpl(RuntimeSerialNumber);

    /// <summary>
    /// Access to the post effects
    /// </summary>
    /// <since>7.0</since>
    [Obsolete("Please use Rhino.Render.RenderSettings methods")]
    public IPostEffects PostEffects => new PostEffectsImpl(RuntimeSerialNumber);

    /// <summary>
    /// Get a enumerable list of custom mesh primitives
    /// </summary>
    /// <param name="forceTriangleMeshes">
    /// If true all mesh faces will be triangulated
    /// </param>
    /// <param name="quietly">
    /// Iterate quietly, if true then no user interface will be displayed
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete("This version is obsolete because - uses the old school custom render meshes.  Prefer CustomRenderMeshes")]
    public IEnumerable<RenderPrimitive> GetRenderPrimitives(bool forceTriangleMeshes, bool quietly)
    {
      return new RenderPrimitiveEnumerable(RuntimeSerialNumber, Guid.Empty, null, forceTriangleMeshes, quietly);
    }
    /// <summary>
    /// Get a enumerable list of custom mesh primitives
    /// </summary>
    /// <param name="viewport">
    /// The rendering view camera.
    /// </param>
    /// <param name="forceTriangleMeshes">
    /// If true all mesh faces will be triangulated
    /// </param>
    /// <param name="quietly">
    /// Iterate quietly, if true then no user interface will be displayed
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete ("This version is obsolete because - uses the old school custom render meshes.  Prefer RenderMeshes")]
    public IEnumerable<RenderPrimitive> GetRenderPrimitives(DocObjects.ViewportInfo viewport, bool forceTriangleMeshes, bool quietly)
    {
      return new RenderPrimitiveEnumerable(RuntimeSerialNumber, Guid.Empty, viewport, forceTriangleMeshes, quietly);
    }
    /// <summary>
    /// Get a enumerable list of custom mesh primitives
    /// </summary>
    /// <param name="plugInId">
    /// The Id of the plug-in creating the iterator.
    /// </param>
    /// <param name="viewport">
    /// The rendering view camera.
    /// </param>
    /// <param name="forceTriangleMeshes">
    /// If true all mesh faces will be triangulated
    /// </param>
    /// <param name="quietly">
    /// Iterate quietly, if true then no user interface will be displayed
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete("This version is obsolete because - uses the old school custom render meshes.  Prefer RenderPrimitives")]
    public IEnumerable<RenderPrimitive> GetRenderPrimitives(Guid plugInId, DocObjects.ViewportInfo viewport, bool forceTriangleMeshes, bool quietly) 
    {
      return new RenderPrimitiveEnumerable(RuntimeSerialNumber, plugInId, viewport, forceTriangleMeshes, quietly);
    }

    /// <summary>
    /// Returns true if the document has a set of custom render primitives - ie, CustomRenderMeshes will return non-null.
    /// </summary>
    /// <param name="mt">The mesh type requested (render or analysis).</param>
    /// <param name="vp">The viewport being rendered.</param>
    /// <param name="flags">See MeshProvider.Flags</param>
    /// <param name="plugin">The requesting plug-in (typically the calling plugin)</param>
    /// <param name="attrs">Display attributes for the caller - null if this is a full rendering.</param>
    /// <returns>Returns true if the object will has a set of custom render primitives</returns>
    /// /// <seealso cref="RenderMeshes"/>
    /// <since>8.0</since>
    public bool HasCustomRenderMeshes(MeshType mt, ViewportInfo vp, ref RenderMeshProvider.Flags flags, PlugIns.PlugIn plugin, Display.DisplayPipelineAttributes attrs)
    {
      uint f = (uint)flags;

      var guids = new SimpleArrayGuid();

      UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_NonObjectIds(guids.NonConstPointer());

      var a = guids.ToArray();

      bool ret = false;

      foreach (var id in a)
      {
        ret = UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_HasCustomMeshes((int)mt, vp.ConstPointer(), RuntimeSerialNumber, id, ref f, plugin.NonConstPointer(), attrs.ConstPointer());
        if (ret)
          break;
      }

      flags = (RenderMeshProvider.Flags)f;

      return ret;
    }


    /// <summary>
    /// Returns a set of non-object custom render primitives for this document.
    /// </summary>
    /// <param name="mt">The mesh type requested (render or analysis).</param>
    /// <param name="vp">The viewport being rendered</param>
    /// <param name="flags">See MeshProvider.Flags</param>
    /// <param name="plugin">The requesting plug-in (typically the calling plugin)</param>
    /// <param name="attrs">Display attributes for the caller - null if this is a full rendering.</param>
    /// <returns> Returns a set of custom render primitives for this object</returns>
    /// <seealso cref="HasCustomRenderMeshes"/>
    /// <since>8.0</since>
    public RenderMeshes[] RenderMeshes(MeshType mt, ViewportInfo vp, ref RenderMeshProvider.Flags flags, PlugIns.PlugIn plugin, Display.DisplayPipelineAttributes attrs)
    {
      var outList = new List<RenderMeshes>();

      var guids = new SimpleArrayGuid();

      UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_NonObjectIds(guids.NonConstPointer());

      var a = guids.ToArray();

      uint f = (uint)flags;

      foreach (var id in a)
      {
        var primitives = new RenderMeshes(this, id, Guid.Empty, 0);

        UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_CustomMeshes((int)mt, primitives.NonConstPointer(), vp.ConstPointer(), RuntimeSerialNumber, id, ref f, plugin.NonConstPointer(), attrs.ConstPointer(), IntPtr.Zero);

        if (0 != primitives.InstanceCount)
        {
          outList.Add(primitives);
        }
      }
      
      
      flags = (RenderMeshProvider.Flags)f;

      return outList.ToArray();
    }

    /// <summary>
    /// Returns the bounding box of custom render primitives for this object .
    /// </summary>
    /// <param name="mt">The mesh type requested (render or analysis).</param>
    /// <param name="vp">The viewport being rendered</param>
    /// <param name="flags">See MeshProvider.Flags</param>
    /// <param name="plugin">The requesting plug-in (typically the calling plugin)</param>
    /// <param name="attrs">Display attributes for the caller - null if this is a full rendering.</param>
    /// <param name="boundingBox">The requested bounding box</param>
    /// <returns>True if the process was a success</returns>
    /// <since>8.0</since>
    public bool CustomRenderMeshesBoundingBox(MeshType mt, ViewportInfo vp, ref RenderMeshProvider.Flags flags, PlugIns.PlugIn plugin, Display.DisplayPipelineAttributes attrs, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;

      var guids = new SimpleArrayGuid();

      UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_NonObjectIds(guids.NonConstPointer());

      var a = guids.ToArray();

      uint f = (uint)flags;

      foreach (var id in a)
      {

        var min = new Point3d();
        var max = new Point3d();

        UnsafeNativeMethods.Rdk_CustomRenderMeshes_IManager_BoundingBox(ref min, ref max, (int)mt, vp.ConstPointer(), RuntimeSerialNumber, id, ref f, plugin.NonConstPointer(), attrs.ConstPointer(), IntPtr.Zero);

        var bb = new BoundingBox(min, max);

        if (boundingBox.IsValid)
        {
          boundingBox.Union(bb);
        }
        else
        {
          boundingBox = bb;
        }
      }

      flags = (RenderMeshProvider.Flags)f;

      return boundingBox.IsValid;
    }

    private GroundPlane m_ground_plane;

    /// <summary>Gets the ground plane of this document.</summary>
    /// <exception cref="Rhino.Runtime.RdkNotLoadedException">If the RDK is not loaded.</exception>
    /// <since>5.0</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete]
    public GroundPlane GroundPlane
    {
      get
      {
        if (m_ground_plane == null)
        {
          Runtime.HostUtils.CheckForRdk(true, true);
          m_ground_plane = new GroundPlane(this);
        }
        return m_ground_plane;
      }
    }

    /// <since>6.0</since>
    public string[] GetEmbeddedFilesList(bool missingOnly)
    {
        using (var list = new ClassArrayString())
        {
            var ptr_list = list.NonConstPointer();
            UnsafeNativeMethods.Rdk_Doc_GetEmbeddedFilesList(RuntimeSerialNumber, missingOnly, ptr_list);
            return list.ToArray();
        }
    }

    #region Getter context utility methods
    /// <summary>
    /// Returns true if currently in a GetPoint.Get(), GetObject.GetObjects(), or GetString.Get()
    /// </summary>
    internal bool InGet => GetBool(UnsafeNativeMethods.DocumentStatusBool.InGet);

    /// <summary>
    /// Returns true if currently in a GetPoint.Get(), GetObject.GetObjects(), or GetString.Get()
    /// </summary>
    internal bool InGetPoint => GetBool(UnsafeNativeMethods.DocumentStatusBool.InGetPoint);

    /// <summary>
    /// Returns true if currently in a GetPoint.Get(), GetObject.GetObjects(), or GetString.Get()
    /// </summary>
    internal bool InGetPointWithBasePoint(out Point3d basePoint)
    {
      basePoint = Point3d.Unset;
      return UnsafeNativeMethods.CRhinoDoc_InGetPoint(RuntimeSerialNumber, ref basePoint);
    }

    /// <summary>
    /// Returns true if currently in a GetPoint.Get(), GetObject.GetObjects(), or GetString.Get()
    /// </summary>
    internal bool InGetObject => GetBool(UnsafeNativeMethods.DocumentStatusBool.InGetObject);

    #endregion

    /// <summary>
    /// true if Rhino is in the process of sending this document as an email attachment.
    /// </summary>
    /// <since>5.0</since>
    public bool IsSendingMail
    {
      get
      {
        if (Runtime.HostUtils.RunningOnOSX)
          throw new NotSupportedException();
        return GetBool(UnsafeNativeMethods.DocumentStatusBool.IsSendingMail);
      }
    }

    /// <summary>
    /// name of the template file used to create this document. This is a runtime value
    /// only present if the document was newly created.
    /// </summary>
    /// <since>5.0</since>
    public string TemplateFileUsed => GetString(IDX_TEMPLATE_FILE_USED);

    /// <since>5.0</since>
    public void ClearUndoRecords(bool purgeDeletedObjects)
    {
      UnsafeNativeMethods.CRhinoDoc_ClearUndoRecords(RuntimeSerialNumber, purgeDeletedObjects);
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public void ClearUndoRecords(uint undoSerialNumber, bool purgeDeletedObjects)
    {
      UnsafeNativeMethods.CRhinoDoc_ClearUndoRecordsSN(RuntimeSerialNumber, undoSerialNumber, purgeDeletedObjects);
    }

    /// <since>5.0</since>
    public void ClearRedoRecords()
    {
      UnsafeNativeMethods.CRhinoDoc_ClearRedoRecords(RuntimeSerialNumber);
    }

    /// <since>5.0</since>
    public bool UndoRecordingEnabled
    {
      get { return GetBool(UnsafeNativeMethods.DocumentStatusBool.UndoRecordingEnable); }
      set { UnsafeNativeMethods.CRhinoDoc_GetSetBool(RuntimeSerialNumber, UnsafeNativeMethods.DocumentStatusBool.UndoRecordingEnable, true, value); }
    }

    /// <summary>
    /// true if undo recording is actually happening now.
    /// </summary>
    /// <since>5.0</since>
    public bool UndoRecordingIsActive => GetBool(UnsafeNativeMethods.DocumentStatusBool.UndoRecordingIsActive);

    /// <summary>
    /// Instructs Rhino to begin recording undo information when the document
    /// is changed outside of a command. We use this, e.g., to save changes
    /// caused by the modeless layer or object properties dialogs
    /// when commands are not running.
    /// </summary>
    /// <param name="description">A text describing the record.</param>
    /// <returns>
    /// Serial number of record.  Returns 0 if record is not started
    /// because undo information is already being recorded or
    /// undo is disabled.
    /// </returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint BeginUndoRecord(string description)
    {
      return UnsafeNativeMethods.CRhinoDoc_BeginUndoRecord(RuntimeSerialNumber, description);
    }

    /// <summary>
    /// The serial number that will be assigned to the next undo record that is
    /// constructed.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint NextUndoRecordSerialNumber => UnsafeNativeMethods.CRhinoDoc_NextUndoRecordSerialNumber(RuntimeSerialNumber);

    /// <summary>
    /// >0: undo recording is active and being saved on the undo record with
    ///     the specified serial number.
    /// 0: undo recording is not active. (Disabled or nothing is being
    ///    recorded.)
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint CurrentUndoRecordSerialNumber
    {
      get { return UnsafeNativeMethods.CRhinoDoc_CurrentUndoRecordSerialNumber(RuntimeSerialNumber); }
    }

    /// <summary> Undo the last action </summary>
    /// <returns> true on success </returns>
    /// <since>6.16</since>
    public bool Undo()
    {
      return UnsafeNativeMethods.CRhinoDoc_Undo(RuntimeSerialNumber);
    }

    /// <summary> Redo the last action that was "undone" </summary>
    /// <returns> true on success </returns>
    /// <since>6.16</since>
    public bool Redo()
    {
      return UnsafeNativeMethods.CRhinoDoc_Redo(RuntimeSerialNumber);
    }

    static List<CustomUndoCallback> g_custom_undo_callbacks;
    internal delegate void RhinoUndoEventHandlerCallback(Guid commandId, IntPtr actionDescription, int createdByRedo, uint sn);
    internal delegate void RhinoDeleteUndoEventHandlerCallback(uint sn);

    static RhinoUndoEventHandlerCallback g_undo_event_handler;
    static RhinoDeleteUndoEventHandlerCallback g_delete_undo_event_handler;

    static void OnUndoEventHandler(Guid commandId, IntPtr actionDescription, int createdByRedo, uint sn)
    {
      if (g_custom_undo_callbacks != null)
      {
        foreach (CustomUndoCallback callback in g_custom_undo_callbacks)
        {
          if (callback.SerialNumber == sn)
          {
            var handler = callback.Handler;
            if( handler!=null )
            {
              try
              {
                object tag = callback.Tag;
                string description = callback.Description;
                RhinoDoc doc = callback.Document;
                handler(null, new Commands.CustomUndoEventArgs(commandId, description, createdByRedo == 1, sn, tag, doc));
              }
              catch (Exception ex)
              {
                Runtime.HostUtils.ExceptionReport("OnUndoEventHandler", ex);
              }
            }
            break;
          }
        }
      }
    }

    static void OnDeleteUndoEventHandler(uint sn)
    {
      if (g_custom_undo_callbacks != null)
      {
        for (int i = 0; i < g_custom_undo_callbacks.Count; i++)
        {
          if (g_custom_undo_callbacks[i].SerialNumber == sn)
          {
            g_custom_undo_callbacks.RemoveAt(i);
            return;
          }
        }
      }
    }

    /// <since>5.0</since>
    public bool AddCustomUndoEvent(string description, EventHandler<Commands.CustomUndoEventArgs> handler)
    {
      return AddCustomUndoEvent(description, handler, null);
    }

    /// <summary>
    /// Add a custom undo event so you can undo private plug-in data
    /// when the user performs an undo or redo
    /// </summary>
    /// <param name="description"></param>
    /// <param name="handler"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    /// <example>
    /// <code source='examples\vbnet\ex_customundo.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_customundo.cs' lang='cs'/>
    /// <code source='examples\py\ex_customundo.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool AddCustomUndoEvent(string description, EventHandler<Commands.CustomUndoEventArgs> handler, object tag)
    {
      if (string.IsNullOrEmpty(description) || handler == null)
        return false;

      g_undo_event_handler = OnUndoEventHandler;
      g_delete_undo_event_handler = OnDeleteUndoEventHandler;

      uint rc = UnsafeNativeMethods.CRhinoDoc_AddCustomUndoEvent(RuntimeSerialNumber, description, g_undo_event_handler, g_delete_undo_event_handler);
      if (rc == 0)
        return false;

      if (g_custom_undo_callbacks == null)
        g_custom_undo_callbacks = new List<CustomUndoCallback>();
      g_custom_undo_callbacks.Add(new CustomUndoCallback(rc, handler, tag, description, this));
      return true;
    }

    /// <summary>
    /// Ends the undo record.
    /// </summary>
    /// <param name="undoRecordSerialNumber">The serial number of the undo record.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool EndUndoRecord(uint undoRecordSerialNumber)
    {
      return UnsafeNativeMethods.CRhinoDoc_EndUndoRecord(RuntimeSerialNumber, undoRecordSerialNumber);
    }

    /// <summary>
    /// Returns true if Undo is currently active. 
    /// </summary>
    /// <since>6.0</since>
    public bool UndoActive
    {
      get { return UnsafeNativeMethods.CRhinoDoc_UndoRedoActive(RuntimeSerialNumber, true); }
    }

    /// <summary>
    /// Returns true if Redo is currently active. 
    /// </summary>
    /// <since>6.0</since>
    public bool RedoActive
    {
      get { return UnsafeNativeMethods.CRhinoDoc_UndoRedoActive(RuntimeSerialNumber, false); }
    }

    //  bool Undo( CRhUndoRecord* = NULL );
    //  bool Redo();
    //
    //  Returns:
    //    Record being currently recorded.  If undo recording is disabled
    //    or nothing is currently being recorded, then NULL is returned.
    //  */
    //  CRhUndoRecord* CurrentUndoRecord() const;
    //  /*
    //  Returns:
    //    >0: undo recording is active and being saved on
    //        the undo record with the specified serial number.
    //     0: undo recording is not active. (Disabled or nothing is
    //        being recorded.)
    //  */
    //  unsigned int CurrentUndoRecordSerialNumber() const;

    //  /*
    //  Returns:
    //    Number of undo records.
    //  */
    //  int GetUndoRecords( ON_SimpleArray<CRhUndoRecord* >& ) const;

    //  /*
    //  Returns: 
    //    Number of undo records.
    //  */
    //  int UndoRecordCount() const;

    //  /*
    //  Returns: 
    //    Number bytes in used by undo records
    //  */
    //  size_t UndoRecordMemorySize() const;

    //  /*
    //  Description:
    //    Culls the undo list to release memory.
    //  Parameters:
    //    min_step_count - [in] 
    //      minimum number of undo steps to keep.
    //    max_memory_size_bytes - [in] 
    //      maximum amount of memory, in bytes, for undo list to use.
    //  Returns:
    //    Number of culled records.    
    //  Remarks:
    //    In the version with no arguments, the settings in
    //    RhinoApp().AppSettings().GeneralSettings() are used.
    //  */
    //  int CullUndoRecords();

    //  int CullUndoRecords( 
    //        int min_step_count, 
    //        size_t max_memory_size_bytes
    //        );


    //  /*
    //  Returns true if document contains undo records.
    //  */
    //  bool HasUndoRecords() const;

    //  /*
    //  Returns:
    //    Number of undo records.
    //  */
    //  int GetRedoRecords( ON_SimpleArray<CRhUndoRecord* >& ) const;

    //  class CRhSelSetManager* m_selset_manager;

    //  void ChangeTitleToUnNamed();

    //  /*
    //  Universal construction plane stack operators
    //  */
    //  void PushConstructionPlane( const ON_Plane& plane );
    //  bool ActiveConstructionPlane( ON_Plane& plane );
    //  bool NextConstructionPlane( ON_Plane& plane );
    //  bool PrevConstructionPlane( ON_Plane& plane );
    //  int ConstructionPlaneCount() const;


    internal bool CreatePreviewImage(string imagePath, Guid viewportId, System.Drawing.Size size, int settings, bool wireframe)
    {
      int width = size.Width;
      int height = size.Height;
      bool rc = UnsafeNativeMethods.CRhinoDoc_CreatePreviewImage(RuntimeSerialNumber, imagePath, viewportId, width, height, settings, wireframe);
      return rc;
    }

    ///<summary>Extracts the bitmap preview image from the specified model (3DM).</summary>
    ///<param name='path'>
    ///The model (3DM) from which to extract the preview image.
    ///If null, the currently loaded model is used.
    ///</param>
    ///<returns>true on success.</returns>
    /// <since>5.0</since>
    static public System.Drawing.Bitmap ExtractPreviewImage(string path)
    {
      return File3dm.ReadPreviewImage (path);
    }

    /// <summary>
    /// Selects a collection of contents in any editors they appear in.
    /// </summary>
    /// <param name="collection">A collection of RenderContents to select</param>
    /// <param name="append">Append to current selection</param>
    /// <since>7.20</since>
    public void SelectRenderContentInEditor(Rhino.Render.RenderContentCollection collection, bool append)
    {
      SimpleArrayGuid guids = new SimpleArrayGuid();

      foreach(Rhino.Render.RenderContent content in collection)
      {
        guids.Append(content.Id);
      }

      var pointer_to_id_list = guids.ConstPointer();
      UnsafeNativeMethods.RdkSelectContentsInEditor(RuntimeSerialNumber, pointer_to_id_list, append);
    }

#pragma warning disable 0618

    /// <summary>
    /// Determines if custom render meshes will be built for this document (i.e. - GH meshes).
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Type of mesh to build. If attrs is non-null then a smaller mesh may be
    /// generated in less time, false is meant when actually rendering.
    /// </param>
    /// <returns>
    /// Returns true if custom render mesh(es) will get built for this document.
    /// </returns>
    /// <since>6.9</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete]
    public bool SupportsRenderPrimitiveList(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      return UnsafeNativeMethods.Rdk_CRMManager_WillBuildCustomMesh(viewport.ConstPointer(), IntPtr.Zero, RuntimeSerialNumber, Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer());
    }

    /// <summary>
    /// Build custom render mesh(es) for this document (i.e. - GH meshes).
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Attributes for the view mode you are supplying meshes for.  Will be null if this is a modal rendering.
    /// </param>
    /// <returns>
    /// Returns a RenderPrimitiveList if successful otherwise returns null.
    /// </returns>
    /// <since>6.9</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete]
    public RenderPrimitiveList GetRenderPrimitiveList(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      var primitives = new RenderPrimitiveList(null);
      var success = UnsafeNativeMethods.Rdk_CRMManager_BuildCustomMeshes(viewport.ConstPointer(), RuntimeSerialNumber, primitives.NonConstPointer(), Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer());
      if (success)
        return primitives;
      primitives.Dispose();
      return null;
    }

    /// <summary>
    /// Get the bounding box for the custom render meshes associated with this
    /// document (i.e. - GH meshes).
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Attributes for the view mode you are supplying meshes for.  Will be null if this is a modal rendering.
    /// </param>
    /// <param name="boundingBox">
    /// This will be set to BoundingBox.Unset on failure otherwise it will be
    /// the bounding box for the custom render meshes associated with this
    /// object.
    /// </param>
    /// <returns>
    /// Returns true if the bounding box was successfully calculated otherwise
    /// returns false on error.
    /// </returns>
    /// <since>6.9</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete]
    public bool TryGetRenderPrimitiveBoundingBox(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;

      var min = new Point3d();
      var max = new Point3d();

      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      if (UnsafeNativeMethods.Rdk_CRMManager_BoundingBox(viewport.ConstPointer(), IntPtr.Zero, RuntimeSerialNumber, Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer(), ref min, ref max))
      {
        boundingBox = new BoundingBox(min, max);
        return boundingBox.IsValid;
      }

      return false;
    }
#pragma warning restore 0618

    /// <summary>
    /// Returns true if Rhino is currently running a command.
    /// </summary>
    /// <since>7.0</since>
    public bool IsCommandRunning => UnsafeNativeMethods.CRhinoDoc_InCommand(RuntimeSerialNumber, false) > 0;

    /// <summary>
    /// This is a low level tool to determine if Rhino is currently running a command.
    /// </summary>
    /// <param name="bIgnoreScriptRunnerCommands">
    /// If true, script running commands, like "ReadCommandFile" and the 
    /// RhinoScript plug-ins "RunScript" command, are not counted.
    /// </param>
    /// <returns>Number of active commands.</returns>
    /// <since>8.0</since>
    public int InCommand(bool bIgnoreScriptRunnerCommands)
    {
      return UnsafeNativeMethods.CRhinoDoc_InCommand(RuntimeSerialNumber, bIgnoreScriptRunnerCommands);
    }

#region events
    internal delegate void DocumentCallback(uint docSerialNumber);
    private static DocumentCallback g_on_close_document_callback;
    private static DocumentCallback g_on_new_document_callback;
    private static DocumentCallback g_on_set_active_document_callback;
    private static DocumentCallback g_on_document_properties_changed;
    private static void OnCloseDocument(uint docSerialNumber)
    {
      if (m_close_document != null)
      {
        try
        {
          m_close_document(null, new DocumentEventArgs(docSerialNumber));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnNewDocument(uint docSerialNumber)
    {
      if (m_new_document != null)
      {
        try
        {
          m_new_document(null, new DocumentEventArgs(docSerialNumber));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnSetActiveDocument(uint docSerialNumber)
    {
      if (g_set_active_document != null)
      {
        try
        {
          g_set_active_document(null, new DocumentEventArgs(docSerialNumber));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnDocumentPropertiesChanged(uint docSerialNumber)
    {
      if (m_document_properties_changed != null)
      {
        try
        {
          m_document_properties_changed(null, new DocumentEventArgs(docSerialNumber));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    // https://mcneel.myjetbrains.com/youtrack/issue/RH-68860
    internal delegate void UnitsChangedWithScalingCallback(uint docSerialNumber, double scale);
    private static UnitsChangedWithScalingCallback g_on_units_changed_with_scaling_callback;
    private static void OnUnitChangedWithScaling(uint docSerialNumber, double scale)
    {
      if (m_units_changed_with_scaling != null)
      {
        try
        {
          m_units_changed_with_scaling(null, new UnitsChangedWithScalingEventArgs(docSerialNumber, scale));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    internal delegate void DocumentIoCallback(uint docSerialNumber, IntPtr pointerToWString, int b1, int b2);
    private static void OnBeginOpenDocument(uint docSerialNumber, IntPtr pointerToWString, int bMerge, int bReference)
    {
      if (m_begin_open_document != null)
      {
        try
        {
          m_begin_open_document(null, new DocumentOpenEventArgs(docSerialNumber, pointerToWString, bMerge != 0, bReference != 0));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnEndOpenDocument(uint docSerialNumber, IntPtr pointerToWString, int bMerge, int bReference)
    {
      if (m_end_open_document != null)
      {
        try
        {
          m_end_open_document(null, new DocumentOpenEventArgs(docSerialNumber, pointerToWString, bMerge != 0, bReference != 0));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnEndOpenDocumentInitialiViewUpdate(uint docSerialNumber, IntPtr pointerToWString, int bMerge, int bReference)
    {
      if (m_after_post_read_view_update_document != null)
      {
        try
        {
          m_after_post_read_view_update_document(null, new DocumentOpenEventArgs(docSerialNumber, pointerToWString, bMerge != 0, bReference != 0));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static void OnBeginSaveDocument(uint docSerialNumber, IntPtr pointerToWString, int bExportSelected, int bUnused)
    {
      if (m_begin_save_document != null)
      {
        try
        {
          m_begin_save_document(null, new DocumentSaveEventArgs(docSerialNumber, pointerToWString, bExportSelected != 0));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnEndSaveDocument(uint docSerialNumber, IntPtr pointerToWString, int bExportSelected, int bUnused)
    {
      if (m_end_save_document != null)
      {
        try
        {
          m_end_save_document(null, new DocumentSaveEventArgs(docSerialNumber, pointerToWString, bExportSelected != 0));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static readonly object g_event_lock = new object();
    internal static EventHandler<DocumentEventArgs> m_close_document;
    /// <since>5.0</since>
    public static event EventHandler<DocumentEventArgs> CloseDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_close_document == null)
          {
            g_on_close_document_callback = OnCloseDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetCloseDocumentCallback(g_on_close_document_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_close_document -= value;
          m_close_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_close_document -= value;
          if (m_close_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetCloseDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_close_document_callback = null;
          }
        }
      }
    }

    internal static EventHandler<DocumentEventArgs> m_new_document;
    /// <since>5.0</since>
    public static event EventHandler<DocumentEventArgs> NewDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_new_document == null)
          {
            g_on_new_document_callback = OnNewDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetNewDocumentCallback(g_on_new_document_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_new_document -= value;
          m_new_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_new_document -= value;
          if (m_new_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetNewDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_new_document_callback = null;
          }
        }
      }
    }

    /// <summary>
    /// This event is raised when the active document used by modeless user
    /// interface changes.  On Mac Rhino this will get raised before the
    /// <see cref="NewDocument"/>, <see cref="BeginOpenDocument"/> and
    /// <see cref="EndOpenDocument"/> events.  Mac Rhino will also raise this
    /// event with 0 for the document Id and a null document pointer when the
    /// last document is closed.  Windows Rhino will raise this event after the
    /// <see cref="NewDocument"/>, <see cref="BeginOpenDocument"/> and
    /// <see cref="EndOpenDocument"/> events when a new or existing model is
    /// opened.
    /// </summary>
    /// <since>6.0</since>
    public static event EventHandler<DocumentEventArgs> ActiveDocumentChanged
    {
      add
      {
        lock (g_event_lock)
        {
          if (g_set_active_document == null)
          {
            g_on_set_active_document_callback = OnSetActiveDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetActiveDocumentCallback(g_on_set_active_document_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_set_active_document -= value;
          g_set_active_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_set_active_document -= value;
          if (g_set_active_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetActiveDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_set_active_document_callback = null;
          }
        }
      }
    }
    internal static EventHandler<DocumentEventArgs> g_set_active_document;

    internal static EventHandler<DocumentEventArgs> m_document_properties_changed;
    /// <since>5.0</since>
    public static event EventHandler<DocumentEventArgs> DocumentPropertiesChanged
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_document_properties_changed == null)
          {
            g_on_document_properties_changed = OnDocumentPropertiesChanged;
            UnsafeNativeMethods.CRhinoEventWatcher_SetDocPropChangeCallback(g_on_document_properties_changed, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_document_properties_changed -= value;
          m_document_properties_changed += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_document_properties_changed -= value;
          if (m_document_properties_changed == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetDocPropChangeCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_document_properties_changed = null;
          }
        }
      }
    }

    internal static EventHandler<UnitsChangedWithScalingEventArgs> m_units_changed_with_scaling;
    /// <summary>
    /// Called when a change in the model units results in a scaling operation on all of the objects in the document.
    /// This call is made before any of the objects are scaled.  
    /// A call to RhinoDoc.DocumentPropertiesChanged follows.
    /// </summary>
    /// <since>7.20</since>
    public static event EventHandler<UnitsChangedWithScalingEventArgs> UnitsChangedWithScaling
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_units_changed_with_scaling == null)
          {
            g_on_units_changed_with_scaling_callback = OnUnitChangedWithScaling;
            UnsafeNativeMethods.CRhinoEventWatcher_SetUnitsChangedWithScaling(g_on_units_changed_with_scaling_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_units_changed_with_scaling -= value;
          m_units_changed_with_scaling += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_units_changed_with_scaling -= value;
          if (m_units_changed_with_scaling == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetUnitsChangedWithScaling(null, Runtime.HostUtils.m_ew_report);
            g_on_units_changed_with_scaling_callback = null;
          }
        }
      }

    }

    /// <summary>
    /// This event is raised when document user text strings are changed
    /// </summary>
    public class UserStringChangedArgs : EventArgs
    {
      internal UserStringChangedArgs(RhinoDoc doc, string key)
      {
        Document = doc;
        Key = key;
      }
      /// <summary>
      /// Document containing the user string
      /// </summary>
      /// <since>7.7</since>
      public RhinoDoc Document { get; }
      /// <summary>
      /// Key for the string being changed
      /// </summary>
      /// <since>7.7</since>
      public string Key { get; }
    }

    /// <summary>
    /// This event is raised when document user text strings are changed
    /// </summary>
    public static event EventHandler<UserStringChangedArgs> UserStringChanged
    {
      add
      {
        lock (g_event_lock)
        {
          if (_userStringChangedCallback == null)
          {
            _userStringChangedCallback = OnUserStringChanged;
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnIDocUserStringChangedCallback(_userStringChangedCallback);
          }
          _userStringChangedEvent -= value;
          _userStringChangedEvent += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          _userStringChangedEvent -= value;
          if (_userStringChangedEvent == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnIDocUserStringChangedCallback(null);
            _userStringChangedCallback = null;
          }
        }
      }
    }
    private static event EventHandler<UserStringChangedArgs> _userStringChangedEvent;
    internal delegate void UserStringChangedCallback(uint docRuntimeSerialNumber, [MarshalAs(UnmanagedType.LPWStr)] string key);
    private static UserStringChangedCallback _userStringChangedCallback = null;
    private static void OnUserStringChanged(uint docRuntimeSerialNumber, [MarshalAs(UnmanagedType.LPWStr)] string key)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docRuntimeSerialNumber);
      if (doc != null)
        _userStringChangedEvent?.Invoke(doc, new UserStringChangedArgs(doc, key));
    }

    private static DocumentIoCallback g_on_begin_open_document;
    private static DocumentIoCallback g_on_end_open_document;
    private static DocumentIoCallback g_on_end_open_document_initial_view_update;
    private static DocumentIoCallback g_on_begin_save_document;
    private static DocumentIoCallback g_on_end_save_document;
    internal static EventHandler<DocumentOpenEventArgs> m_begin_open_document;
    /// <summary>
    /// This event is raised when the document open operation begins.
    /// NOTE: On Windows, this event will be fired when a clipboard paste 
    /// operation occurs, as Rhino opens a .tmp file in the User's
    /// Local folder with the contents of the pasted document.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocumentOpenEventArgs> BeginOpenDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_begin_open_document == null)
          {
            g_on_begin_open_document = OnBeginOpenDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetBeginOpenDocumentCallback(g_on_begin_open_document, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_begin_open_document -= value;
          m_begin_open_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_begin_open_document -= value;
          if (m_begin_open_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetBeginOpenDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_begin_open_document = null;
          }
        }
      }
    }

    internal static EventHandler<DocumentOpenEventArgs> m_end_open_document;
    /// <since>5.0</since>
    public static event EventHandler<DocumentOpenEventArgs> EndOpenDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_end_open_document == null)
          {
            g_on_end_open_document = OnEndOpenDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetEndOpenDocumentCallback(g_on_end_open_document, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_end_open_document -= value;
          m_end_open_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_end_open_document -= value;
          if (m_end_open_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetEndOpenDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_end_open_document = null;
          }
        }
      }
    }

    internal static EventHandler<DocumentOpenEventArgs> m_after_post_read_view_update_document;
    /// <summary>
    /// This event is raised after <see cref="EndOpenDocument"/> when the
    /// documents initial views have been created and initialized.
    /// </summary>
    /// <since>5.11</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Typo: use EndOpenDocumentInitialViewUpdate")]
    public static event EventHandler<DocumentOpenEventArgs> EndOpenDocumentInitialiViewUpdate
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_after_post_read_view_update_document == null)
          {
            g_on_end_open_document_initial_view_update = OnEndOpenDocumentInitialiViewUpdate;
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnAfterPostReadViewUpdateCallback(g_on_end_open_document_initial_view_update);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_after_post_read_view_update_document -= value;
          m_after_post_read_view_update_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_after_post_read_view_update_document -= value;
          if (m_after_post_read_view_update_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnAfterPostReadViewUpdateCallback(null);
            g_on_end_open_document_initial_view_update = null;
          }
        }
      }
    }

    /// <summary>
    /// This event is raised after <see cref="EndOpenDocument"/> when the
    /// documents initial views have been created and initialized.
    /// </summary>
    /// <since>6.18</since>
    public static event EventHandler<DocumentOpenEventArgs> EndOpenDocumentInitialViewUpdate
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_after_post_read_view_update_document == null)
          {
            g_on_end_open_document_initial_view_update = OnEndOpenDocumentInitialiViewUpdate;
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnAfterPostReadViewUpdateCallback(g_on_end_open_document_initial_view_update);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_after_post_read_view_update_document -= value;
          m_after_post_read_view_update_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_after_post_read_view_update_document -= value;
          if (m_after_post_read_view_update_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnAfterPostReadViewUpdateCallback(null);
            g_on_end_open_document_initial_view_update = null;
          }
        }
      }
    }

    internal static EventHandler<DocumentSaveEventArgs> m_begin_save_document;
    /// <since>5.0</since>
    public static event EventHandler<DocumentSaveEventArgs> BeginSaveDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_begin_save_document == null)
          {
            g_on_begin_save_document = OnBeginSaveDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetBeginSaveDocumentCallback(g_on_begin_save_document, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_begin_save_document -= value;
          m_begin_save_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_begin_save_document -= value;
          if (m_begin_save_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetBeginSaveDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_begin_save_document = null;
          }
        }
      }
    }

    internal static EventHandler<DocumentSaveEventArgs> m_end_save_document;
    /// <since>5.0</since>
    public static event EventHandler<DocumentSaveEventArgs> EndSaveDocument
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_end_save_document == null)
          {
            g_on_end_save_document = OnEndSaveDocument;
            UnsafeNativeMethods.CRhinoEventWatcher_SetEndSaveDocumentCallback(g_on_end_save_document, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_end_save_document -= value;
          m_end_save_document += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_end_save_document -= value;
          if (m_end_save_document == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetEndSaveDocumentCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_end_save_document = null;
          }
        }
      }
    }

    internal delegate void RhinoObjectCallback(uint docSerialNumber, IntPtr pObject, IntPtr pObject2);

    private static RhinoObjectCallback g_on_add_rhino_object;
    private static RhinoObjectCallback g_on_delete_object_callback;
    private static RhinoObjectCallback g_on_replace_object;
    private static RhinoObjectCallback g_on_undelete_object;
    private static RhinoObjectCallback g_on_purge_object;
    private static void OnAddObject(uint docSerialNumber, IntPtr pObject, IntPtr pObject2)
    {
      if (m_add_object != null)
      {
        try
        {
          m_add_object(null, new DocObjects.RhinoObjectEventArgs(docSerialNumber, pObject));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    internal static EventHandler<DocObjects.RhinoObjectEventArgs> m_add_object;
    /// <summary>Called if a new object is added to the document.</summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectEventArgs> AddRhinoObject
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_add_object == null)
          {
            g_on_add_rhino_object = OnAddObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetAddObjectCallback(g_on_add_rhino_object, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_add_object -= value;
          m_add_object += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_add_object -= value;
          if (m_add_object == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetAddObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_add_rhino_object = null;
          }
        }
      }
    }

    private static void OnDeleteObject(uint docSerialNumber, IntPtr pObject, IntPtr pObject2)
    {
      if (m_delete_object != null)
      {
        bool old_state = RhinoApp.InEventWatcher;
        RhinoApp.InEventWatcher = true;
        try
        {
          m_delete_object(null, new DocObjects.RhinoObjectEventArgs(docSerialNumber, pObject));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
        RhinoApp.InEventWatcher = old_state;
      }
    }
    internal static EventHandler<DocObjects.RhinoObjectEventArgs> m_delete_object;
    /// <summary>
    /// Called if an object is deleted. At some later point the object can be un-deleted.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectEventArgs> DeleteRhinoObject
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_delete_object == null)
          {
            g_on_delete_object_callback = OnDeleteObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetDeleteObjectCallback(g_on_delete_object_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_delete_object -= value;
          m_delete_object += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_delete_object -= value;
          if (m_delete_object == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetDeleteObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_delete_object_callback = null;
          }
        }
      }
    }

    private static void OnReplaceObject(uint docSerialNumber, IntPtr pOldObject, IntPtr pNewObject)
    {
      if (m_replace_object != null)
      {
        try
        {
          m_replace_object(null, new DocObjects.RhinoReplaceObjectEventArgs(docSerialNumber, pOldObject, pNewObject));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoReplaceObjectEventArgs> m_replace_object;
    /// <summary>
    /// Called if an object is about to be replaced.
    /// If both RhinoDoc.UndoActive() and RhinoDoc.RedoActive() return false,
    /// then immediately after the ReplaceObject event, there will be a DeleteObject
    /// event followed by an AddObject event.
    ///
    /// If either RhinoDoc.UndoActive() or RhinoDoc::RedoActive() return true,
    /// then immediately after the ReplaceObject event, there will be a DeleteObject
    /// event followed by an UndeleteObject event.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoReplaceObjectEventArgs> ReplaceRhinoObject
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_replace_object == null)
          {
            g_on_replace_object = OnReplaceObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetReplaceObjectCallback(g_on_replace_object, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_replace_object -= value;
          m_replace_object += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_replace_object -= value;
          if (m_replace_object == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetReplaceObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_replace_object = null;
          }
        }
      }
    }

    private static void OnUndeleteObject(uint docSerialNumber, IntPtr pObject, IntPtr pObject2)
    {
      if (m_undelete_object != null)
      {
        try
        {
          m_undelete_object(null, new DocObjects.RhinoObjectEventArgs(docSerialNumber, pObject));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoObjectEventArgs> m_undelete_object;
    /// <summary>Called if an object is un-deleted.</summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectEventArgs> UndeleteRhinoObject
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_undelete_object == null)
          {
            g_on_undelete_object = OnUndeleteObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetUnDeleteObjectCallback(g_on_undelete_object, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_undelete_object -= value;
          m_undelete_object += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_undelete_object -= value;
          if (m_undelete_object == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetUnDeleteObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_undelete_object = null;
          }
        }
      }
    }

    private static void OnPurgeObject(uint docSerialNumber, IntPtr pObject, IntPtr pObject2)
    {
      if (m_purge_object != null)
      {
        try
        {
          m_purge_object(null, new DocObjects.RhinoObjectEventArgs(docSerialNumber, pObject));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoObjectEventArgs> m_purge_object;
    /// <summary>
    /// Called if an object is being purged from a document. The object will cease to exist forever.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectEventArgs> PurgeRhinoObject
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_purge_object == null)
          {
            g_on_purge_object = OnPurgeObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetPurgeObjectCallback(g_on_purge_object, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_purge_object -= value;
          m_purge_object += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_purge_object -= value;
          if (m_purge_object == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetPurgeObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_purge_object = null;
          }
        }
      }
    }

    internal delegate void RhinoObjectSelectionCallback(uint docSerialNumber, int select, IntPtr pObject, IntPtr pObjects);

    private static RhinoObjectSelectionCallback g_on_select_rhino_object_callback;

    private static void OnSelectObject(uint docSerialNumber, int bSelect, IntPtr pObject, IntPtr pObjects)
    {
      if (m_select_objects != null && bSelect == 1)
      {
        try
        {
          var args = new DocObjects.RhinoObjectSelectionEventArgs(true, docSerialNumber, pObject, pObjects);
          m_select_objects(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      else if (m_deselect_objects != null && bSelect == 0)
      {
        try
        {
          var args = new DocObjects.RhinoObjectSelectionEventArgs(false, docSerialNumber, pObject, pObjects);
          m_deselect_objects(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoObjectSelectionEventArgs> m_select_objects;
    internal static EventHandler<DocObjects.RhinoObjectSelectionEventArgs> m_deselect_objects;

    /// <summary>
    /// Called when object(s) are selected.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectSelectionEventArgs> SelectObjects
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_select_objects == null)
          {
            g_on_select_rhino_object_callback = OnSelectObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetSelectObjectCallback(g_on_select_rhino_object_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_select_objects -= value;
          m_select_objects += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_select_objects -= value;
          if (m_select_objects == null && m_deselect_objects == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetSelectObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_select_rhino_object_callback = null;
          }
        }
      }
    }

    /// <summary>
    /// Called when object(s) are deselected.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoObjectSelectionEventArgs> DeselectObjects
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_deselect_objects == null)
          {
            g_on_select_rhino_object_callback = OnSelectObject;
            UnsafeNativeMethods.CRhinoEventWatcher_SetSelectObjectCallback(g_on_select_rhino_object_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_deselect_objects -= value;
          m_deselect_objects += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_deselect_objects -= value;
          if (m_select_objects == null && m_deselect_objects == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetSelectObjectCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_select_rhino_object_callback = null;
          }
        }
      }
    }

    internal delegate void RhinoDeselectAllObjectsCallback(uint docSerialNumber, int objectCount);
    private static RhinoDeselectAllObjectsCallback g_on_deselect_all_rhino_objects_callback;

    private static void OnDeselectAllObjects(uint docSerialNumber, int count)
    {
      if (m_deselect_allobjects != null)
      {
        try
        {
          var args = new DocObjects.RhinoDeselectAllObjectsEventArgs(docSerialNumber, count);
          m_deselect_allobjects(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoDeselectAllObjectsEventArgs> m_deselect_allobjects;

    /// <summary>
    /// Called when all objects are deselected.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoDeselectAllObjectsEventArgs> DeselectAllObjects
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_deselect_allobjects == null)
          {
            g_on_deselect_all_rhino_objects_callback = OnDeselectAllObjects;
            UnsafeNativeMethods.CRhinoEventWatcher_SetDeselectAllObjectsCallback(g_on_deselect_all_rhino_objects_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_deselect_allobjects -= value;
          m_deselect_allobjects += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_deselect_allobjects -= value;
          if (m_deselect_allobjects == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetDeselectAllObjectsCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_deselect_all_rhino_objects_callback = null;
          }
        }
      }
    }


    internal delegate void RhinoModifyObjectAttributesCallback(uint docSerialNumber, IntPtr pRhinoObject, IntPtr pConstRhinoObjectAttributes);
    private static RhinoModifyObjectAttributesCallback g_on_modify_object_attributes_callback;

    private static void OnModifyObjectAttributes(uint docSerialNumber, IntPtr pRhinoObject, IntPtr pConstRhinoObjectAttributes)
    {
      if (m_modify_object_attributes != null)
      {
        try
        {
          var args = new DocObjects.RhinoModifyObjectAttributesEventArgs(docSerialNumber, pRhinoObject, pConstRhinoObjectAttributes);
          m_modify_object_attributes(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DocObjects.RhinoModifyObjectAttributesEventArgs> m_modify_object_attributes;

    /// <summary>
    /// Called when all object attributes are changed.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DocObjects.RhinoModifyObjectAttributesEventArgs> ModifyObjectAttributes
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_modify_object_attributes == null)
          {
            g_on_modify_object_attributes_callback = OnModifyObjectAttributes;
            UnsafeNativeMethods.CRhinoEventWatcher_SetModifyObjectAttributesCallback(g_on_modify_object_attributes_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_modify_object_attributes -= value;
          m_modify_object_attributes += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_modify_object_attributes -= value;
          if (m_modify_object_attributes == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetModifyObjectAttributesCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_modify_object_attributes_callback = null;
          }
        }
      }
    }

    internal delegate void RhinoTransformObjectsCallback(IntPtr pRhinoOnTransformObject);
    private static RhinoTransformObjectsCallback g_on_before_transform_objects_callback;
    private static void OnBeforeTransformObjects(IntPtr pRhinoOnTransformObject)
    {
      if (g_before_transform_objects != null)
      {
        try
        {
          var args = new DocObjects.RhinoTransformObjectsEventArgs(pRhinoOnTransformObject);
          g_before_transform_objects(null, args);
          args.CleanUp();
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    static EventHandler<DocObjects.RhinoTransformObjectsEventArgs> g_before_transform_objects;
    /// <summary>
    /// Called before objects are being transformed
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_rhinogettransform.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_rhinogettransform.cs' lang='cs'/>
    /// </example>
    /// <since>5.10</since>
    public static event EventHandler<DocObjects.RhinoTransformObjectsEventArgs> BeforeTransformObjects
    {
      add
      {
        lock (g_event_lock)
        {
          if (g_before_transform_objects == null)
          {
            g_on_before_transform_objects_callback = OnBeforeTransformObjects;
            UnsafeNativeMethods.CRhinoEventWatcher_SetTransformObjectsCallback(g_on_before_transform_objects_callback);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_before_transform_objects -= value;
          g_before_transform_objects += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_before_transform_objects -= value;
          if (g_before_transform_objects == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetTransformObjectsCallback(null);
            g_on_before_transform_objects_callback = null;
          }
        }
      }
    }

    internal delegate void RhinoTableCallback(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings);
    private static RhinoTableCallback g_on_layer_table_event_callback;
    private static void OnLayerTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_layer_table_event != null)
      {
        try
        {
          var args = new LayerTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_layer_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<LayerTableEventArgs> m_layer_table_event;

    /// <summary>
    /// Called when any modification happens to a document's layer table.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<LayerTableEventArgs> LayerTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_layer_table_event == null)
          {
            g_on_layer_table_event_callback = OnLayerTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetLayerTableEventCallback(g_on_layer_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_layer_table_event -= value;
          m_layer_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_layer_table_event -= value;
          if (m_layer_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetLayerTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_layer_table_event_callback = null;
          }
        }
      }
    }


    #region Linetype table event
    private static RhinoTableCallback g_on_linetype_table_event_callback;
    private static GCHandle g_linetype_callback_gchandle;
    private static void OnLinetypeTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_linetype_table_event != null)
      {
        try
        {
          var args = new LinetypeTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_linetype_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<LinetypeTableEventArgs> m_linetype_table_event;

    /// <summary>
    /// Called when any modification happens to a document's linetype table.
    /// </summary>
    /// <since>8.0</since>
    public static event EventHandler<LinetypeTableEventArgs> LinetypeTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_linetype_table_event == null)
          {
            g_on_linetype_table_event_callback = OnLinetypeTableEvent;
            g_linetype_callback_gchandle = GCHandle.Alloc(g_on_linetype_table_event_callback);

            UnsafeNativeMethods.CRhinoEventWatcher_SetLinetypeTableEventCallback(g_on_linetype_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_linetype_table_event -= value;
          m_linetype_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_linetype_table_event -= value;
          if (m_linetype_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetLinetypeTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_linetype_table_event_callback = null;
            if (g_linetype_callback_gchandle.IsAllocated)
            {
              g_linetype_callback_gchandle.Free();
            }
          }
        }
      }
    }
    #endregion


    #region Dimension style table event
    private static RhinoTableCallback g_on_dim_style_table_event_callback;
    private static void OnDimStyleTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_dim_style_table_event != null)
      {
        try
        {
          var args = new DimStyleTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_dim_style_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<DimStyleTableEventArgs> m_dim_style_table_event;

    /// <summary>
    /// Called when any modification happens to a document's dimension style table.
    /// </summary>
    /// <since>6.0</since>
    public static event EventHandler<DimStyleTableEventArgs> DimensionStyleTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_dim_style_table_event == null)
          {
            g_on_dim_style_table_event_callback = OnDimStyleTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetDimStyleTableEventCallback(g_on_dim_style_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_dim_style_table_event -= value;
          m_dim_style_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_dim_style_table_event -= value;
          if (m_dim_style_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetDimStyleTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_dim_style_table_event_callback = null;
          }
        }
      }
    }
    #endregion

    private static RhinoTableCallback g_on_idef_table_event_callback;
    private static void OnIdefTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_idef_table_event != null)
      {
        try
        {
          var args = new InstanceDefinitionTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_idef_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<InstanceDefinitionTableEventArgs> m_idef_table_event;

    /// <summary>
    /// Called when any modification happens to a document's instance definition table.
    /// </summary>
    /// <since>5.3</since>
    public static event EventHandler<InstanceDefinitionTableEventArgs> InstanceDefinitionTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_idef_table_event == null)
          {
            g_on_idef_table_event_callback = OnIdefTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetIdefTableEventCallback(g_on_idef_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_idef_table_event -= value;
          m_idef_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_idef_table_event -= value;
          if (m_idef_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetIdefTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_idef_table_event_callback = null;
          }
        }
      }
    }

    private static RhinoTableCallback g_on_light_table_event_callback;
    private static void OnLightTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_light_table_event != null)
      {
        try
        {
          var args = new LightTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_light_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<LightTableEventArgs> m_light_table_event;

    /// <summary>
    /// Called when any modification happens to a document's light table.
    /// </summary>
    /// <since>5.3</since>
    public static event EventHandler<LightTableEventArgs> LightTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_light_table_event == null)
          {
            g_on_light_table_event_callback = OnLightTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetLightTableEventCallback(g_on_light_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_light_table_event -= value;
          m_light_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_light_table_event -= value;
          if (m_light_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetLightTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_light_table_event_callback = null;
          }
        }
      }
    }


    private static RhinoTableCallback g_on_material_table_event_callback;
    private static void OnMaterialTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_material_table_event != null)
      {
        try
        {
          var args = new MaterialTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_material_table_event(null, args);
          args.Done();
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<MaterialTableEventArgs> m_material_table_event;

    /// <summary>
    /// Called when any modification happens to a document's material table.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<MaterialTableEventArgs> MaterialTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_material_table_event == null)
          {
            g_on_material_table_event_callback = OnMaterialTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetMaterialTableEventCallback(g_on_material_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_material_table_event -= value;
          m_material_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_material_table_event -= value;
          if (m_material_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetMaterialTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_material_table_event_callback = null;
          }
        }
      }
    }


    private static RhinoTableCallback g_on_group_table_event_callback;
    private static void OnGroupTableEvent(uint docSerialNumber, int eventType, int index, IntPtr pConstOldSettings)
    {
      if (m_group_table_event != null)
      {
        try
        {
          var args = new GroupTableEventArgs(docSerialNumber, eventType, index, pConstOldSettings);
          m_group_table_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<GroupTableEventArgs> m_group_table_event;

    /// <summary>
    /// Called when any modification happens to a document's group table.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<GroupTableEventArgs> GroupTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (m_group_table_event == null)
          {
            g_on_group_table_event_callback = OnGroupTableEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetGroupTableEventCallback(g_on_group_table_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_group_table_event -= value;
          m_group_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          m_group_table_event -= value;
          if (m_group_table_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetGroupTableEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_on_group_table_event_callback = null;
          }
        }
      }
    }

    #endregion

    #region RenderContentTable events
    /// <summary>
    /// Type of content table event
    /// </summary>
    /// <since>5.7</since>
    public enum RenderContentTableEventType
    {
      /// <summary>
      /// The document has been read and the table has been loaded
      /// </summary>
      Loaded,
      /// <summary>
      /// The table is about to be cleared
      /// </summary>
      Clearing,
      /// <summary>
      /// The table has been cleared
      /// </summary>
      Cleared,
      /// <summary>
      /// Object or layer material assignment changed
      /// </summary>
      MaterialAssignmentChanged,
    }
    /// <summary>
    /// Passed to the <see cref="RenderMaterialsTableEvent"/>, <see cref="RenderEnvironmentTableEvent"/> and the
    /// <see cref="RenderTextureTableEvent"/> events.
    /// </summary>
    public class RenderContentTableEventArgs : EventArgs
    {
      internal RenderContentTableEventArgs(RhinoDoc document, RenderContentTableEventType eventType)
      {
        m_rhino_doc = document;
        m_event_type = eventType;
      }

      /// <summary>
      /// Document the table belongs to
      /// </summary>
      /// <since>5.7</since>
      public RhinoDoc Document { get { return m_rhino_doc; } }
      /// <summary>
      /// Event type
      /// </summary>
      /// <since>5.7</since>
      public RenderContentTableEventType EventType { get { return m_event_type; } }

      private readonly RhinoDoc m_rhino_doc;
      private readonly RenderContentTableEventType m_event_type;
    }
    private static RenderContentTableEventForwarder.ContentListLoadedCallback g_on_render_content_loaded_event_callback;
    private static void OnRenderContentdLoadedEvent(int kind, uint docSerialNumber)
    {
      var document = FromRuntimeSerialNumber(docSerialNumber);
      switch ((RenderContentKind)kind)
      {
        case RenderContentKind.Material:
          OnRenderMaterialTabledEvent(document, RenderContentTableEventType.Loaded);
          break;
        case RenderContentKind.Environment:
          OnRenderEnvironmentTabledEvent(document, RenderContentTableEventType.Loaded);
          break;
        case RenderContentKind.Texture:
          OnRenderTextureTabledEvent(document, RenderContentTableEventType.Loaded);
          break;
      }
    }
    private static RenderContentTableEventForwarder.MaterialAssigmentChangedCallback g_on_object_material_assignment_changed_event_callback;
    private static void OnObjectMaterialAssignmentChangedEvent(uint docSerialNumber, Guid objectId, Guid newMaterialId, Guid oldMaterialId)
    {
      OnMaterialAssignmentChangedCustomEvent(docSerialNumber, true, objectId, newMaterialId, oldMaterialId);
    }
    private static RenderContentTableEventForwarder.MaterialAssigmentChangedCallback g_on_layer_material_assignment_changed_event_callback;
    private static void OnLayerMaterialAssignmentChangedEvent(uint docSerialNumber, Guid layerId, Guid newMaterialId, Guid oldMaterialId)
    {
      OnMaterialAssignmentChangedCustomEvent(docSerialNumber, false, layerId, newMaterialId, oldMaterialId);
    }

    private static RenderContentTableEventForwarder.ContentListClearingCallback g_on_render_content_clearing_event_callback;
    private static void OnRenderContentdClearingEvent(int kind, uint docSerialNumber)
    {
      var document = FromRuntimeSerialNumber(docSerialNumber);
      switch ((RenderContentKind)kind)
      {
        case RenderContentKind.Material:
          OnRenderMaterialTabledEvent(document, RenderContentTableEventType.Clearing);
          break;
        case RenderContentKind.Environment:
          OnRenderEnvironmentTabledEvent(document, RenderContentTableEventType.Clearing);
          break;
        case RenderContentKind.Texture:
          OnRenderTextureTabledEvent(document, RenderContentTableEventType.Clearing);
          break;
      }
    }

    private static RenderContentTableEventForwarder.ContentListClearedCallback g_on_render_content_cleared_event_callback;
    private static void OnRenderContentdClearedEvent(int kind, uint docSerialNumber)
    {
      var document = FromRuntimeSerialNumber(docSerialNumber);
      switch ((RenderContentKind)kind)
      {
        case RenderContentKind.Material:
          OnRenderMaterialTabledEvent(document, RenderContentTableEventType.Cleared);
          break;
        case RenderContentKind.Environment:
          OnRenderEnvironmentTabledEvent(document, RenderContentTableEventType.Cleared);
          break;
        case RenderContentKind.Texture:
          OnRenderTextureTabledEvent(document, RenderContentTableEventType.Cleared);
          break;
      }
    }
    #endregion RenderContentTable events

    #region RenderContent event helpers
    private static void ContentTableAddEventHelper()
    {
      if (g_on_object_material_assignment_changed_event_callback == null)
      {
        g_on_object_material_assignment_changed_event_callback = OnObjectMaterialAssignmentChangedEvent;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetObjectMaterialAssignmentChangedEventCallback(
          g_on_object_material_assignment_changed_event_callback, Runtime.HostUtils.m_rdk_ew_report);
      }
      if (g_on_layer_material_assignment_changed_event_callback == null)
      {
        g_on_layer_material_assignment_changed_event_callback = OnLayerMaterialAssignmentChangedEvent;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetLayerMaterialAssignmentChangedEventCallback(
          g_on_layer_material_assignment_changed_event_callback, Runtime.HostUtils.m_rdk_ew_report);
      }
      if (g_on_render_content_loaded_event_callback == null)
      {
        g_on_render_content_loaded_event_callback = OnRenderContentdLoadedEvent;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListLoadedEventCallback(
          g_on_render_content_loaded_event_callback, Runtime.HostUtils.m_rdk_ew_report);
      }
      if (g_on_render_content_clearing_event_callback == null)
      {
        g_on_render_content_clearing_event_callback = OnRenderContentdClearingEvent;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListClearingEventCallback(
          g_on_render_content_clearing_event_callback, Runtime.HostUtils.m_rdk_ew_report);
      }
      if (g_on_render_content_cleared_event_callback == null)
      {
        g_on_render_content_cleared_event_callback = OnRenderContentdClearedEvent;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListClearedEventCallback(
          g_on_render_content_cleared_event_callback, Runtime.HostUtils.m_rdk_ew_report);
      }
    }
    private static void ContentTableRemoveEventHelper()
    {
      if (g_render_materials_table_event != null ||
          g_render_environment_table_event != null ||
          g_render_texture_table_event != null ||
          g_on_object_material_assignment_changed_event_callback != null ||
          g_on_layer_material_assignment_changed_event_callback == null)
        return;
      UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListLoadedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
      UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListClearingEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
      UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentListClearedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
      UnsafeNativeMethods.CRdkCmnEventWatcher_SetObjectMaterialAssignmentChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
      UnsafeNativeMethods.CRdkCmnEventWatcher_SetLayerMaterialAssignmentChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
      g_on_render_content_loaded_event_callback = null;
      g_on_render_content_clearing_event_callback = null;
      g_on_render_content_cleared_event_callback = null;
      g_on_object_material_assignment_changed_event_callback = null;
      g_on_layer_material_assignment_changed_event_callback = null;
    }
    #endregion RenderContent event helpers

    #region RenderMaterialsTable events
    private static void OnMaterialAssignmentChangedCustomEvent(uint docSerialNumber, bool objectChanged, Guid objectOrLayerId, Guid newMaterialId, Guid oldMaterialId)
    {
      if (g_render_materials_table_event == null)
        return;
      try
      {
        var document = FromRuntimeSerialNumber(docSerialNumber);
        var layer_id = objectChanged ? Guid.Empty : objectOrLayerId;
        var object_id = objectChanged ? objectOrLayerId : Guid.Empty;
        var args = new RenderMaterialAssignmentChangedEventArgs(document, RenderContentTableEventType.MaterialAssignmentChanged, layer_id, object_id, oldMaterialId, newMaterialId);
        g_render_materials_table_event(null == document ? null : document.RenderMaterials, args);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    private static void OnRenderMaterialTabledEvent(RhinoDoc document, RenderContentTableEventType eventType)
    {
      if (g_render_materials_table_event == null)
        return;
      try
      {
        var args = new RenderContentTableEventArgs(document, eventType);
        g_render_materials_table_event(null == document ? null : document.RenderMaterials, args);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    private static event EventHandler<RenderContentTableEventArgs> g_render_materials_table_event;

    public class RenderMaterialAssignmentChangedEventArgs : RenderContentTableEventArgs
    {
      internal RenderMaterialAssignmentChangedEventArgs(
        RhinoDoc document,
        RenderContentTableEventType eventType,
        Guid layerId,
        Guid objectId,
        Guid oldMaterialContentId,
        Guid newMaterialContentId)
        : base(document, eventType)
      {
        m_layer_id = layerId;
        m_object_id = objectId;
        m_old_material_content_id = oldMaterialContentId;
        m_new_material_content_id = newMaterialContentId;
      }
      /// <since>5.10</since>
      public bool IsLayer{get { return LayerId != Guid.Empty; } }
      /// <since>5.10</since>
      public bool IsObject { get { return ObjectId != Guid.Empty; } }
      /// <since>5.10</since>
      public Guid LayerId { get { return m_layer_id; } }
      /// <since>5.10</since>
      public Guid ObjectId { get { return m_object_id; } }
      /// <since>5.10</since>
      public Guid OldRenderMaterial { get { return m_old_material_content_id; } }
      /// <since>5.10</since>
      public Guid NewRenderMaterial { get { return m_new_material_content_id; } }

      private readonly Guid m_layer_id;
      private readonly Guid m_object_id;
      private readonly Guid m_old_material_content_id;
      private readonly Guid m_new_material_content_id;
    }

    /// Called when the <see cref="RenderMaterialTable"/> has been loaded, is
    /// about to be cleared or has been cleared.  See <see cref="RenderContentTableEventType"/> for more
    /// information.
    /// <since>5.7</since>
    public static event EventHandler<RenderContentTableEventArgs> RenderMaterialsTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          ContentTableAddEventHelper();
          g_render_materials_table_event -= value;
          g_render_materials_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          g_render_materials_table_event -= value;
          ContentTableRemoveEventHelper();
        }
      }
    }
    #endregion RenderMaterialsTable events

    #region RenderEnvironmentsTable events

    private static void OnRenderEnvironmentTabledEvent(RhinoDoc document, RenderContentTableEventType eventType)
    {
      if (g_render_environment_table_event == null)
        return;
      try
      {
        var args = new RenderContentTableEventArgs(document, eventType);
        g_render_environment_table_event(null == document ? null : document.RenderEnvironments, args);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    private static event EventHandler<RenderContentTableEventArgs> g_render_environment_table_event;
    /// Called when the <see cref="RenderEnvironmentTable"/> has been loaded, is
    /// about to be cleared or has been cleared.  See <see cref="RenderContentTableEventType"/> for more
    /// information.
    /// <since>5.7</since>
    public static event EventHandler<RenderContentTableEventArgs> RenderEnvironmentTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          ContentTableAddEventHelper();
          g_render_environment_table_event -= value;
          g_render_environment_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          g_render_environment_table_event -= value;
          ContentTableRemoveEventHelper();
        }
      }
    }

    #endregion RenderEnvironmentsTable events

    #region RenderTexturesTable events

    private static void OnRenderTextureTabledEvent(RhinoDoc document, RenderContentTableEventType eventType)
    {
      if (g_render_texture_table_event == null)
        return;
      try
      {
        var args = new RenderContentTableEventArgs(document, eventType);
        g_render_texture_table_event(null == document ? null : document.RenderTextures, args);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    private static event EventHandler<RenderContentTableEventArgs> g_render_texture_table_event;
    /// <summary>
    /// Called when the <see cref="RenderTextureTable"/> has been loaded, is
    /// about to be cleared or has been cleared.  See <see cref="RenderContentTableEventType"/> for more
    /// information.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentTableEventArgs> RenderTextureTableEvent
    {
      add
      {
        lock (g_event_lock)
        {
          ContentTableAddEventHelper();
          g_render_texture_table_event -= value;
          g_render_texture_table_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          g_render_texture_table_event -= value;
          ContentTableRemoveEventHelper();
        }
      }
    }


    #endregion RenderTexturesTable events

    /// <since>5.8</since>
    public enum TextureMappingEventType
    {
      /// <summary>
      /// Adding texture mapping to a document
      /// </summary>
      Added = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Added,
      /// <summary>
      /// A texture mapping was deleted from a document
      /// </summary>
      Deleted = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Deleted,
      /// <summary>
      /// A texture mapping was undeleted in a document
      /// </summary>
      Undeleted = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Undeleted,
      /// <summary>
      /// A texture mapping was modified in a document
      /// </summary>
      Modified = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Modified,
      //Sorted = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Sorted,
      //Current = UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Current,
    }
    /// <summary>
    /// Event arguments passed to the RhinoDoc.TextureMappingEvent.
    /// </summary>
    public class TextureMappingEventArgs : EventArgs
    {
      readonly uint m_doc_serial_number;
      readonly TextureMappingEventType m_event_type = TextureMappingEventType.Modified;
      readonly IntPtr m_const_pointer_new_mapping;
      readonly IntPtr m_const_pointer_old_mapping;

      internal TextureMappingEventArgs(uint docSerialNumber, UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts eventType, IntPtr constPointerConstNewMapping, IntPtr pConstOldMapping)
      {
        m_doc_serial_number = docSerialNumber;
        switch (eventType)
        {
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Added:
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Deleted:
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Modified:
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Undeleted:
            m_event_type = (TextureMappingEventType)eventType;
            break;
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Current:
          case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Sorted:
            break;
        }
        m_const_pointer_new_mapping = constPointerConstNewMapping;
        m_const_pointer_old_mapping = m_const_pointer_new_mapping;
      }

      RhinoDoc m_doc;
      /// <since>5.8</since>
      public RhinoDoc Document
      {
        get { return m_doc ?? (m_doc = FromRuntimeSerialNumber(m_doc_serial_number)); }
      }

      /// <since>5.8</since>
      public TextureMappingEventType EventType
      {
        get { return m_event_type; }
      }

      /// <since>5.8</since>
      public TextureMapping OldMapping
      {
        get { return (m_old_mapping ?? (m_old_mapping = new TextureMapping(m_const_pointer_old_mapping))); }
      }
      private TextureMapping m_old_mapping;

      /// <since>5.8</since>
      public TextureMapping NewMapping
      {
        get { return (m_new_mapping ?? (m_new_mapping = new TextureMapping(m_const_pointer_new_mapping))); }
      }
      TextureMapping m_new_mapping;
    }
    /// <summary>
    /// Called when any modification happens to a document objects texture mapping.
    /// </summary>
    /// <since>5.8</since>
    public static event EventHandler<TextureMappingEventArgs> TextureMappingEvent
    {
      add
      {
        lock (g_event_lock)
        {
          if (g_texture_mapping_event == null)
          {
            g_on_texture_mapping_event_callback = OnTextureMappingEvent;
            UnsafeNativeMethods.CRhinoEventWatcher_SetTextureMappingEventCallback(g_on_texture_mapping_event_callback, Runtime.HostUtils.m_ew_report);
          }
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_texture_mapping_event -= value;
          g_texture_mapping_event += value;
        }
      }
      remove
      {
        lock (g_event_lock)
        {
          // ReSharper disable once DelegateSubtraction - okay for single value
          g_texture_mapping_event -= value;
          if (g_texture_mapping_event == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetTextureMappingEventCallback(null, Runtime.HostUtils.m_ew_report);
            g_texture_mapping_event = null;
          }
        }
      }
    }
    internal delegate void TextureMappingEventCallback(uint docSerialNumber, UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts eventType, IntPtr pConstNewSettings, IntPtr pConstOldSettings);
    private static TextureMappingEventCallback g_on_texture_mapping_event_callback;
    private static void OnTextureMappingEvent(uint docSerialNumber, UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts eventType, IntPtr pConstNewSettings, IntPtr pConstOldSettings)
    {
      if (g_texture_mapping_event != null)
      {
        try
        {
          switch (eventType)
          {
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Added:
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Deleted:
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Modified:
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Undeleted:
              break;
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Current:
            case UnsafeNativeMethods.RhinoEventWatcherTextureMappingEventConsts.Sorted:
              return; // Ignore these for now
          }
          var args = new TextureMappingEventArgs(docSerialNumber, eventType, pConstNewSettings, pConstOldSettings);
          g_texture_mapping_event(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static EventHandler<TextureMappingEventArgs> g_texture_mapping_event;
  }

  /// <summary>
  /// Arguments passed to <see cref="IRhinoDocObserver"/> methods.
  /// </summary>
  public class RhinoDocObserverArgs
  {
    /// <since>6.0</since>
    public RhinoDocObserverArgs(RhinoDoc doc)
    {
      m_doc = doc;
    }
    private RhinoDoc m_doc;
    /// <summary>
    /// Document
    /// </summary>
    /// <since>6.0</since>
    public RhinoDoc Doc { get { return m_doc; } }
    /// <summary>
    /// Document runtime serial number, will be different across Rhino sessions.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber { get { return (m_doc == null ? 0u : m_doc.RuntimeSerialNumber); } }
  }

  /// <summary>
  /// Implement this interface if you are a modeless interface to aid in
  /// handling multiple document implementations
  /// </summary>
  public interface IRhinoDocObserver
  {
    /// <summary>
    /// When a document is closed
    /// </summary>
    /// <param name="e"></param>
    /// <since>6.0</since>
    void RhinoDocClosed(RhinoDocObserverArgs e);
    /// <summary>
    /// In Windows Rhino this will mean a new document has been created or
    /// opened.  In Mac Rhino this can mean the same thing as well it can
    /// indicate switching from one active open document to another.
    /// </summary>
    /// <param name="e"></param>
    /// <since>6.0</since>
    void ActiveRhinoDocChanged(RhinoDocObserverArgs e);
  }

  /// <summary>
  /// Provides document information for RhinoDoc events.
  /// </summary>
  public class DocumentEventArgs : EventArgs
  {
    private RhinoDoc m_doc;
    internal DocumentEventArgs(uint docSerialNumber)
    {
      DocumentSerialNumber = docSerialNumber;
    }

    /// <summary>
    /// Gets the document Id of the document for this event.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use DocumentSerialNumber or Document properties")]
    public int DocumentId => (int)DocumentSerialNumber;

    /// <summary>
    /// Gets the uniques document serial number for this event
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint DocumentSerialNumber { get; }

    /// <summary>
    /// Gets the document for this event. This field might be null.
    /// </summary>
    /// <since>5.0</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber)); }
    }
  }

  /// <summary>
  /// Provides information about UnitsChangedWithScaling events.
  /// </summary>
  /// <since>7.20</since>
  public class UnitsChangedWithScalingEventArgs : EventArgs
  {
    private RhinoDoc m_doc;
    internal UnitsChangedWithScalingEventArgs(uint docSerialNumber, double scale)
    {
      DocumentSerialNumber = docSerialNumber;
      Scale = scale;
    }

    /// <summary>
    /// Gets the uniques document serial number for this event.
    /// </summary>
    /// <since>7.20</since>
    [CLSCompliant(false)]
    public uint DocumentSerialNumber { get; }

    /// <summary>
    /// Gets the document for this event. This field might be null.
    /// </summary>
    /// <since>7.20</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber)); }
    }

    /// <summary>
    /// The scale factor.
    /// </summary>
    /// <since>7.20</since>
    public double Scale { get; }
  }

  /// <summary>
  /// Provides document information for RhinoDoc events.
  /// </summary>
  public class DocumentOpenEventArgs : DocumentEventArgs
  {
    internal DocumentOpenEventArgs(uint docSerialNumber, IntPtr pointerToWString, bool merge, bool reference)
      : base(docSerialNumber)
    {
      FileName = StringWrapper.GetStringFromPointer(pointerToWString);
      Merge = merge;
      Reference = reference;
    }

    /// <summary>
    /// Name of file being opened.
    /// </summary>
    /// <since>5.0</since>
    public string FileName { get; }

    /// <summary>
    /// true if file is being merged into the current document. This
    /// occurs during the "Import" command.
    /// </summary>
    /// <since>5.0</since>
    public bool Merge { get; }

    /// <summary>
    /// true if file is opened as a reference file.
    /// </summary>
    /// <since>5.0</since>
    public bool Reference { get; }
  }

  /// <summary>
  /// Provides document information for RhinoDoc events.
  /// </summary>
  public class DocumentSaveEventArgs : DocumentEventArgs
  {
    internal DocumentSaveEventArgs(uint docSerialNumber, IntPtr pointerToWString, bool exportSelected)
      : base(docSerialNumber)
    {
      FileName = StringWrapper.GetStringFromPointer(pointerToWString);
      ExportSelected = exportSelected;
    }

    /// <summary>
    /// Name of file being written.
    /// </summary>
    /// <since>5.0</since>
    public string FileName { get; }

    /// <summary>
    /// true if only selected objects are being written to a file.
    /// </summary>
    /// <since>5.0</since>
    public bool ExportSelected { get; }
  }

  namespace DocObjects
  {
    public class RhinoObjectEventArgs : EventArgs
    {
      private readonly IntPtr m_pRhinoObject;
      private RhinoObject m_rhino_object;
      private Guid m_ObjectID = Guid.Empty;

      internal RhinoObjectEventArgs(uint docSerialNumber, IntPtr pRhinoObject)
      {
        m_pRhinoObject = pRhinoObject;
      }

      /// <since>5.0</since>
      public Guid ObjectId
      {
        get
        {
          if (m_ObjectID == Guid.Empty)
          {
            m_ObjectID = UnsafeNativeMethods.CRhinoObject_Id(m_pRhinoObject);
          }
          return m_ObjectID;
        }
      }

      /// <since>5.0</since>
      public RhinoObject TheObject
      {
        get
        {
          if (null == m_rhino_object || m_rhino_object.ConstPointer() != m_pRhinoObject)
          {
            m_rhino_object = RhinoObject.CreateRhinoObjectHelper(m_pRhinoObject);
          }
          return m_rhino_object;
        }
      }
    }

    public class RhinoObjectSelectionEventArgs : EventArgs
    {
      private readonly uint m_doc_serial_number;
      private readonly IntPtr m_pRhinoObject;
      private readonly IntPtr m_pRhinoObjectList;

      internal RhinoObjectSelectionEventArgs(bool select, uint docSerialNumber, IntPtr pRhinoObject, IntPtr pRhinoObjects)
      {
        Selected = select;
        m_doc_serial_number = docSerialNumber;
        m_pRhinoObject = pRhinoObject;
        m_pRhinoObjectList = pRhinoObjects;
      }

      /// <summary>
      /// Returns true if objects are being selected.
      /// Returns false if objects are being deselected.
      /// </summary>
      /// <since>5.0</since>
      public bool Selected { get; }

      RhinoDoc m_doc;
      /// <since>5.0</since>
      public RhinoDoc Document
      {
        get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_serial_number)); }
      }

      List<RhinoObject> m_objects;
      /// <since>5.0</since>
      public RhinoObject[] RhinoObjects
      {
        get
        {
          if (m_objects == null)
          {
            m_objects = new List<RhinoObject>();
            if (m_pRhinoObject != IntPtr.Zero)
            {
              RhinoObject rhobj = RhinoObject.CreateRhinoObjectHelper(m_pRhinoObject);
              if (rhobj != null)
                m_objects.Add(rhobj);
            }
            if (m_pRhinoObjectList != IntPtr.Zero)
            {
              RhinoObject[] rhobjs = Runtime.InternalRhinoObjectArray.ToArrayFromPointer(m_pRhinoObjectList, true);
              m_objects.AddRange(rhobjs);
            }
          }
          return m_objects.ToArray();
        }
      }
    }

    public class RhinoReplaceObjectEventArgs : EventArgs
    {
      private readonly IntPtr m_pOldRhinoObject;
      private RhinoObject m_old_rhino_object;

      private readonly IntPtr m_pNewRhinoObject;
      private RhinoObject m_new_rhino_object;
      private Guid m_ObjectId = Guid.Empty;
      private readonly uint m_doc_serial_number;

      internal RhinoReplaceObjectEventArgs(uint docSerialNumber, IntPtr pOldRhinoObject, IntPtr pNewRhinoObject)
      {
        m_doc_serial_number = docSerialNumber;
        m_pOldRhinoObject = pOldRhinoObject;
        m_pNewRhinoObject = pNewRhinoObject;
      }

      /// <since>5.0</since>
      public Guid ObjectId
      {
        get
        {
          if (m_ObjectId == Guid.Empty)
          {
            m_ObjectId = UnsafeNativeMethods.CRhinoObject_Id(m_pOldRhinoObject);
          }
          return m_ObjectId;
        }
      }

      /// <since>5.0</since>
      public RhinoObject OldRhinoObject
      {
        get
        {
          if (null == m_old_rhino_object || m_old_rhino_object.ConstPointer() != m_pOldRhinoObject)
          {
            m_old_rhino_object = RhinoObject.CreateRhinoObjectHelper(m_pOldRhinoObject);
            if( m_old_rhino_object!=null )
              m_old_rhino_object.m_pRhinoObject = m_pOldRhinoObject;
          }
          return m_old_rhino_object;
        }
      }

      /// <since>5.0</since>
      public RhinoObject NewRhinoObject
      {
        get
        {
          if (null == m_new_rhino_object || m_new_rhino_object.ConstPointer() != m_pNewRhinoObject)
          {
            m_new_rhino_object = RhinoObject.CreateRhinoObjectHelper(m_pNewRhinoObject);
            // Have to explicitly set the pointer since the object is not "officially"
            // in the document yet (can't find it by runtime serial number)
            if (m_new_rhino_object != null)
              m_new_rhino_object.m_pRhinoObject = m_pNewRhinoObject;
          }
          return m_new_rhino_object;
        }
      }

      /// <since>5.0</since>
      public RhinoDoc Document => RhinoDoc.FromRuntimeSerialNumber(m_doc_serial_number);
    }

    public class RhinoDeselectAllObjectsEventArgs : EventArgs
    {
      private readonly uint m_doc_serial_number;

      internal RhinoDeselectAllObjectsEventArgs(uint docSerialNumber, int count)
      {
        m_doc_serial_number = docSerialNumber;
        ObjectCount = count;
      }

      /// <since>5.0</since>
      public int ObjectCount { get; }

      RhinoDoc m_doc;
      /// <since>5.0</since>
      public RhinoDoc Document
      {
        get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_serial_number)); }
      }
    }

    public class RhinoModifyObjectAttributesEventArgs : EventArgs
    {
      private readonly uint m_doc_serial_number;
      private readonly IntPtr m_pRhinoObject;
      private readonly IntPtr m_pOldObjectAttributes;

      internal RhinoModifyObjectAttributesEventArgs(uint docSerialNumber, IntPtr pRhinoObject, IntPtr pOldObjectAttributes)
      {
        m_doc_serial_number = docSerialNumber;
        m_pRhinoObject = pRhinoObject;
        m_pOldObjectAttributes = pOldObjectAttributes;
      }

      RhinoDoc m_doc;
      /// <since>5.0</since>
      public RhinoDoc Document
      {
        get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_serial_number)); }
      }

      RhinoObject m_object;
      /// <since>5.0</since>
      public RhinoObject RhinoObject
      {
        get { return m_object ?? (m_object = RhinoObject.CreateRhinoObjectHelper(m_pRhinoObject)); }
      }

      ObjectAttributes m_old_attributes;
      /// <since>5.0</since>
      public ObjectAttributes OldAttributes
      {
        get
        {
          if( m_old_attributes==null )
          {
            m_old_attributes = new ObjectAttributes(m_pOldObjectAttributes);
            m_old_attributes.DoNotDestructOnDispose();
          }
          return m_old_attributes;
        }
      }

      /// <since>5.0</since>
      public ObjectAttributes NewAttributes => RhinoObject.Attributes;
    }

    /// <summary>
    /// EventArgs passed to RhinoDoc.BeforeTransform.
    /// </summary>
    public class RhinoTransformObjectsEventArgs : EventArgs
    {
      IntPtr m_ptr_transform_object;
      RhinoObject[] m_objects;
      GripObject[] m_grips;
      RhinoObject[] m_grip_owners;

      internal RhinoTransformObjectsEventArgs(IntPtr pRhinoOnTransformObject)
      {
        m_ptr_transform_object = pRhinoOnTransformObject;
      }

      internal void CleanUp()
      {
        m_ptr_transform_object = IntPtr.Zero;
      }

      /// <summary>
      /// The transformation to be applied.
      /// </summary>
      /// <since>5.10</since>
      public Transform Transform
      {
        get
        {
          Transform xf = Transform.Identity;
          UnsafeNativeMethods.CRhinoOnTransformObject_Transform(m_ptr_transform_object, ref xf);
          return xf;
        }
      }

      /// <summary>
      /// True if the objects will be copied.
      /// </summary>
      /// <since>5.10</since>
      public bool ObjectsWillBeCopied => UnsafeNativeMethods.CRhinoOnTransformObject_Copy(m_ptr_transform_object);

      private const int idxObjectCount = 0;
      private const int idxGripCount = 1;
      private const int idxGripOwnerCount = 2;

      /// <summary>
      /// The number of Rhino objects that will be transformed.
      /// </summary>
      /// <since>5.10</since>
      public int ObjectCount => UnsafeNativeMethods.CRhinoOnTransformObject_ObjectCount(m_ptr_transform_object, idxObjectCount);

      /// <summary>
      /// The number of Rhino object grips that will be transformed.
      /// </summary>
      /// <since>7.0</since>
      public int GripCount => UnsafeNativeMethods.CRhinoOnTransformObject_ObjectCount(m_ptr_transform_object, idxGripCount);

      /// <summary>
      /// The number of Rhino object grip owners that will be changed when the grips are transformed.
      /// </summary>
      /// <since>7.0</since>
      public int GripOwnerCount => UnsafeNativeMethods.CRhinoOnTransformObject_ObjectCount(m_ptr_transform_object, idxGripOwnerCount);

      /// <summary>
      /// An array of Rhino objects to be transformed.
      /// WARNING: these objects may be deleted at anytime after the event handler has been notified.
      /// Do not save references to these objects. Use the object's runtime serial number
      /// to safely reference these objects at a later time.
      /// </summary>
      /// <since>5.10</since>
      public RhinoObject[] Objects
      {
        get
        {
          if (m_objects == null)
          {
            int count = ObjectCount;
            m_objects = new RhinoObject[count];
            for (int i = 0; i < count; i++)
            {
              IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoOnTransformObject_Object(m_ptr_transform_object, i);
              m_objects[i] = RhinoObject.CreateRhinoObjectHelper(ptr_rhino_object);
            }
          }
          return m_objects;
        }
      }

      /// <summary>
      /// An array of Rhino object grips that will be transformed.
      /// WARNING: these objects may be deleted at anytime after the event handler has been notified.
      /// Do not save references to these objects. Use the object's runtime serial number
      /// to safely reference these objects at a later time.
      /// </summary>
      /// <since>7.0</since>
      public GripObject[] Grips
      {
        get
        {
          if (m_grips == null)
          {
            int count = GripCount;
            m_grips = new GripObject[count];
            for (int i = 0; i < count; i++)
            {
              IntPtr ptr_grip_object = UnsafeNativeMethods.CRhinoOnTransformObject_Grip(m_ptr_transform_object, i);
              var sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr_grip_object);
              if (IntPtr.Zero != ptr_grip_object && sn > 0)
              {
                var g = new GripObject(sn);
                m_grips[i] = g;
              }
            }
          }
          return m_grips;
        }
      }

      /// <summary>
      /// An array of Rhino object grip owners that will be changed when the grips are transformed.
      /// WARNING: these objects may be deleted at anytime after the event handler has been notified.
      /// Do not save references to these objects. Use the object's runtime serial number
      /// to safely reference these objects at a later time.
      /// </summary>
      /// <since>7.0</since>
      public RhinoObject[] GripOwners
      {
        get
        {
          if (m_grip_owners == null)
          {
            int count = GripOwnerCount;
            m_grip_owners = new RhinoObject[count];
            for (int i = 0; i < count; i++)
            {
              IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoOnTransformObject_GripOwner(m_ptr_transform_object, i);
              m_grip_owners[i] = RhinoObject.CreateRhinoObjectHelper(ptr_rhino_object);
            }
          }
          return m_grip_owners;
        }
      }
    }
  }
}


namespace Rhino.DocObjects.Tables
{
  public abstract class RhinoDocCommonTable<T> : CommonComponentTable<T>
    where T : ModelComponent
  {
    internal readonly RhinoDoc m_doc;

    internal RhinoDocCommonTable(RhinoDoc doc) : base(doc.Manifest)
    {
      m_doc = doc;
    }

    /// <summary>Document that owns this table.</summary>
    public RhinoDoc Document => m_doc;

    /// <summary>
    /// Expose this only in tables that have UNIQUE names
    /// and where uniqueness is not derived from parent ID (layers).
    /// </summary>
    internal T __FindNameInternal(string name)
    {
      return m_manifest.FindName<T>(name, Guid.Empty);
    }

    /// <summary>
    /// Expose this only in tables that have UNIQUE names
    /// and where uniqueness is not derived from parent ID (layers).
    /// </summary>
    internal T __FindNameHashInternal(NameHash hash)
    {
      return m_manifest.FindNameHash<T>(hash);
    }
  }

  /// <summary>
  /// Collection of document runtime data. This is a good place to
  /// put non-serialized, per document data.
  /// </summary>
  public sealed class RuntimeDocumentDataTable : Dictionary<object, object>
  {
    /// <summary>
    /// Internal constructor called by the parent RhinoDoc
    /// </summary>
    /// <param name="doc">RhinoDoc that owns this table</param>
    internal RuntimeDocumentDataTable(RhinoDoc doc)
    {
      Document = doc;
    }

    /// Document this user data table is associated with
    /// <since>6.3</since>
    public RhinoDoc Document { get; }

    /// <summary>
    /// Checks the dictionary for the specified key, if found and the value is not
    /// null then the value is returned.  If the key is not found or its value
    /// is null then newT(Document) is called to create a new value instance which
    /// is put in the dictionary and returned.
    /// </summary>
    /// <typeparam name="T">
    /// Class type created as necessary and returned.
    /// </typeparam>
    /// <param name="key">
    /// Key to search for.
    /// </param>
    /// <param name="newT">
    /// Function called to create new value
    /// </param>
    /// <returns>
    /// Returns the document specific instance of type T using the specified
    /// dictionary key.
    /// </returns>
    /// <example>
    /// var data = doc?.RuntimeData.GetValue(typeof(MyClass), rhinoDoc => new MyClass(rhinoDoc));
    /// </example>
    public T GetValue<T>(object key, Func<RhinoDoc, T> newT) where T : class
    {
      if (TryGetValue(key, out object value) && value != null && value is T)
        return value as T;
      var value_t = newT(Document);
      this[key] = value_t;
      return value_t;
    }

    /// <summary>
    /// Check dictionary for value and return it properly cast if
    /// found.
    /// </summary>
    /// <param name="key">
    /// Key to search for.
    /// </param>
    /// <typeparam name="T">
    /// Class type created as necessary and returned.
    /// </typeparam>
    /// <returns>
    /// Returns the document specific instance of type T using the specified
    /// dictionary key or null if not found.
    /// </returns>
    /// <since>6.15</since>
    public T TryGetValue<T>(object key) where T : class
    {
      TryGetValue(key, out object value);
      return value as T;
    }
  }

  public sealed class ViewTable : IEnumerable<RhinoView>
  {
    internal ViewTable(RhinoDoc doc)
    {
      Document = doc;
    }

    /// <summary>Document that owns this object table.</summary>
    /// <since>5.0</since>
    public RhinoDoc Document { get; }

    /// <summary>
    /// Gets or Sets the active view.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoView ActiveView
    {
      get
      {
        IntPtr ptr = UnsafeNativeMethods.CRhinoDoc_ActiveView(Document.RuntimeSerialNumber);
        if (ptr == IntPtr.Zero)
          return null;
        return RhinoView.FromIntPtr(ptr);
      }
      set
      {
        uint sn = value == null ? 0 : value.RuntimeSerialNumber;
        UnsafeNativeMethods.CRhinoDoc_SetActiveView(sn);
      }
    }

    /// <summary> Determine if a camera icon is being shown </summary>
    /// <param name="view">
    /// if null, then all views are tested. If not null, then just view is tested.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool IsCameraIconVisible(RhinoView view)
    {
      uint view_sn = view == null ? 0 : view.RuntimeSerialNumber;
      return UnsafeNativeMethods.CRhinoDoc_CameraIconVisible(Document.RuntimeSerialNumber, view_sn);
    }

    /// <summary>
    /// Use to turn camera icon on and off
    /// </summary>
    /// <param name="view">
    /// If null, any camera icon is turned off. If not null, the camera icon for that
    /// view is turned on.
    /// </param>
    /// <since>6.0</since>
    public void EnableCameraIcon(RhinoView view)
    {
      uint view_sn = view == null ? 0 : view.RuntimeSerialNumber;
      UnsafeNativeMethods.CRhinoDoc_EnableCameraIcon(Document.RuntimeSerialNumber, view_sn);
    }

    /// <summary>
    /// Cause objects selection state to change momentarily so the object
    /// appears to flash on the screen.
    /// </summary>
    /// <param name="list">An array, a list or any enumerable set of Rhino objects.</param>
    /// <param name="useSelectionColor">
    /// If true, flash between object color and selection color. If false,
    /// flash between visible and invisible.
    /// </param>
    /// <since>5.0</since>
    public void FlashObjects(IEnumerable<RhinoObject> list, bool useSelectionColor)
    {
      var rharray = new Runtime.InternalRhinoObjectArray(list);
      IntPtr ptr_array = rharray.NonConstPointer();
      UnsafeNativeMethods.CRhinoDoc_FlashObjectList(Document.RuntimeSerialNumber, ptr_array, useSelectionColor);
    }

    /// <summary>Redraws all views.</summary>
    /// <remarks>
    /// If you change something in the document -- like adding objects,
    /// deleting objects, modifying layer or object display attributes, etc.,
    /// then you need to call Redraw to redraw all the views.
    ///
    /// If you change something in a particular view like the projection,
    /// construction plane, background bitmap, etc., then you need to
    /// call CRhinoView::Redraw to redraw that particular view.
    ///</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addcircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void Redraw()
    {
      UnsafeNativeMethods.CRhinoDoc_Redraw(Document.RuntimeSerialNumber);
    }

    /// <summary>Gets an array of all the views.</summary>
    /// <param name="includeStandardViews">true if "Right", "Perspective", etc., view should be included; false otherwise.</param>
    /// <param name="includePageViews">true if page-related views should be included; false otherwise.</param>
    /// <returns>A array of Rhino views. This array can be empty, but not null.</returns>
    /// <since>5.0</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete]
    public RhinoView[] GetViewList(bool includeStandardViews, bool includePageViews)
    {
      var vtf = ViewTypeFilter.All;

      if (!includeStandardViews && includePageViews)
        vtf = ViewTypeFilter.Page;

      if (includeStandardViews && !includePageViews)
        vtf = ViewTypeFilter.ModelStyleViews;

      if (!includeStandardViews && !includePageViews)
      {
        vtf = ViewTypeFilter.None;
      }

      return GetViewList(vtf);
    }

    /// <summary>
    /// Gets an array of all the views.
    /// </summary>
    /// <param name="filter">View types to include</param>
    /// <returns>An array of Rhino views. This array can be empty, but not null.</returns>
    /// <since>8.0</since>
    public RhinoView[] GetViewList(ViewTypeFilter filter)
    {
      int count = UnsafeNativeMethods.CRhinoDoc_ViewListBuild(Document.RuntimeSerialNumber, (int)filter);
      if (count < 1)
        return new RhinoView[0];
      var views = new List<RhinoView>(count);
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr_view = UnsafeNativeMethods.CRhinoDoc_ViewListGet(Document.RuntimeSerialNumber, i);
        RhinoView view = RhinoView.FromIntPtr(ptr_view);
        if (view != null)
          views.Add(view);
      }
      UnsafeNativeMethods.CRhinoDoc_ViewListBuild(Document.RuntimeSerialNumber, (int)ViewTypeFilter.None); // calling with None empties the static list used by ViewListGet
      return views.ToArray();
    }

    /// <since>5.0</since>
    public RhinoView[] GetStandardRhinoViews()
    {
      return GetViewList(ViewTypeFilter.Model);
    }
    
    /// <summary>
    /// Gets all page views in the document.
    /// </summary>
    /// <returns>An array with all page views. The return value can be an empty array but not null.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoPageView[] GetPageViews()
    {
      var views = GetViewList(ViewTypeFilter.Page);
      if (null == views || views.Length < 1)
        return new RhinoPageView[0];
      var pages = new RhinoPageView[views.Length];
      for (int i = 0; i < views.Length; i++)
      {
        pages[i] = views[i] as RhinoPageView;
      }
      return pages;
    }

    /// <summary>
    /// Finds a view in this document with a given main viewport Id.
    /// </summary>
    /// <param name="mainViewportId">The ID of the main viewport looked for.</param>
    /// <returns>View on success. null if the view could not be found in this document.</returns>
    /// <since>5.0</since>
    public RhinoView Find(Guid mainViewportId)
    {
      IntPtr ptr_view = UnsafeNativeMethods.CRhinoDoc_FindView(Document.RuntimeSerialNumber, mainViewportId);
      return RhinoView.FromIntPtr(ptr_view);
    }

    /// <summary>
    /// Finds a view in this document with a main viewport that has a given name. Note that there
    /// may be multiple views in this document that have the same name. This function only returns
    /// the first view found. If you want to find all the views with a given name, use the GetViewList
    /// function and iterate through the views.
    /// </summary>
    /// <param name="mainViewportName">The name of the main viewport.</param>
    /// <param name="compareCase">true if capitalization influences comparison; otherwise, false.</param>
    /// <returns>A Rhino view on success; null on error.</returns>
    /// <since>5.0</since>
    public RhinoView Find(string mainViewportName, bool compareCase)
    {
      IntPtr ptr_view = UnsafeNativeMethods.CRhinoDoc_FindView2(Document.RuntimeSerialNumber, mainViewportName, compareCase);
      return RhinoView.FromIntPtr(ptr_view);
    }

    const int idxDefaultViewLayout = 0;
    const int idxFourViewLayout = 1;
    const int idxThreeViewLayout = 2;

    /// <since>5.0</since>
    public void DefaultViewLayout()
    {
      UnsafeNativeMethods.CRhinoDoc_ViewLayout(Document.RuntimeSerialNumber, idxDefaultViewLayout, false);
    }
    /// <since>5.0</since>
    public void FourViewLayout(bool useMatchingViews)
    {
      UnsafeNativeMethods.CRhinoDoc_ViewLayout(Document.RuntimeSerialNumber, idxFourViewLayout, useMatchingViews);
    }
    /// <since>5.0</since>
    public void ThreeViewLayout(bool useMatchingViews)
    {
      UnsafeNativeMethods.CRhinoDoc_ViewLayout(Document.RuntimeSerialNumber, idxThreeViewLayout, useMatchingViews);
    }

    ///<summary>Returns or sets (enable or disables) screen redrawing.</summary>
    /// <since>5.0</since>
    public bool RedrawEnabled
    {
      get { return Document.GetBool(UnsafeNativeMethods.DocumentStatusBool.RedrawEnabled); }
      set { UnsafeNativeMethods.CRhinoDoc_GetSetBool(Document.RuntimeSerialNumber, UnsafeNativeMethods.DocumentStatusBool.RedrawEnabled, true, value); }
    }

    /// <summary>
    /// Enables or disables screen redrawing.
    /// </summary>
    /// <param name="enable">Enable redrawing.</param>
    /// <param name="redrawDocument">If enabling, set to true to have the document redrawn.</param>
    /// <param name="redrawLayers">If enabling, set to true to have the layer user interface redrawn.</param>
    /// <since>7.3</since>
    public void EnableRedraw(bool enable, bool redrawDocument, bool redrawLayers)
    {
      UnsafeNativeMethods.CRhinoDoc_EnableRedraw(Document.RuntimeSerialNumber, enable, redrawDocument, redrawLayers);
    }

    /// <summary>
    /// Constructs a new Rhino view and, at the same time, adds it to the list.
    /// </summary>
    /// <param name="title">The title of the new Rhino view.</param>
    /// <param name="projection">A basic projection type.</param>
    /// <param name="position">A position.</param>
    /// <param name="floating">true if the view floats; false if it is docked.</param>
    /// <returns>The newly constructed Rhino view; or null on error.</returns>
    /// <since>5.0</since>
    public RhinoView Add(string title, DefinedViewportProjection projection, System.Drawing.Rectangle position, bool floating)
    {
      uint view_sn = UnsafeNativeMethods.CRhinoView_Create(Document.RuntimeSerialNumber, position.Left, position.Top, position.Right, position.Bottom, floating);
      var rc = RhinoView.FromRuntimeSerialNumber(view_sn);
      if (rc != null)
      {
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-49159, default to wireframe display mode.
        rc.MainViewport.DisplayMode = DisplayModeDescription.GetDisplayMode(DisplayModeDescription.WireframeId);
        rc.MainViewport.SetCameraLocations(Point3d.Origin, rc.MainViewport.CameraLocation);
        rc.MainViewport.SetProjection(projection, title, true);
        rc.MainViewport.ZoomExtents();
      }
      return rc;
    }

    /// <summary>
    /// Constructs a new page view with a given title and, at the same time, adds it to the list.
    /// </summary>
    /// <param name="title">
    /// If null or empty, a name will be generated as "Page #" where # is the largest page number.
    /// </param>
    /// <returns>The newly created page view on success; or null on error.</returns>
    /// <since>5.0</since>
    public RhinoPageView AddPageView(string title)
    {
      IntPtr ptr_page_view = UnsafeNativeMethods.CRhinoPageView_CreateView(Document.RuntimeSerialNumber, title, 0, 0);
      return RhinoView.FromIntPtr(ptr_page_view) as RhinoPageView;
    }

    /// <summary>
    /// Constructs a new page view with a given title and size and, at the same time, adds it to the list.
    /// </summary>
    /// <param name="title">
    /// If null or empty, a name will be generated as "Page #" where # is the largest page number.
    /// </param>
    /// <param name="pageWidth">The page total width.</param>
    /// <param name="pageHeight">The page total height.</param>
    /// <returns>The newly created page view on success; or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoPageView AddPageView(string title, double pageWidth, double pageHeight)
    {
      IntPtr ptr_page_view = UnsafeNativeMethods.CRhinoPageView_CreateView(Document.RuntimeSerialNumber, title, pageWidth, pageHeight);
      return RhinoView.FromIntPtr(ptr_page_view) as RhinoPageView;
    }

    /// <since>5.0</since>
    public bool ModelSpaceIsActive
    {
      get
      {
        return !(ActiveView is RhinoPageView);
      }
    }
#region IEnumerable<RhinoView> Members

    /// <since>5.0</since>
    public IEnumerator<RhinoView> GetEnumerator()
    {
      RhinoView[] views = GetViewList(ViewTypeFilter.All);
      return new List<RhinoView>(views).GetEnumerator();
    }

    /// <since>5.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      RhinoView[] views = GetViewList(ViewTypeFilter.All);
      return new List<RhinoView>(views).GetEnumerator();
    }
    #endregion
  }

  public sealed class ObjectTable :
    RhinoDocCommonTable<RhinoObject>,
    ICollection<RhinoObject>
  {
    internal ObjectTable(RhinoDoc doc) : base(doc){}

    /// <summary>
    /// Gets the document that owns this object table.
    /// </summary>
    /// <since>5.0</since>
    public new RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>
    /// Please use FindId().
    /// </summary>
    /// <param name="objectId">Do not use this method.</param>
    /// <returns>Do not use this method.</returns>
    /// <since>5.0</since>
    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public RhinoObject Find(Guid objectId)
    {
      return FindId(objectId);
    }

    /// <summary>
    /// <para>Uses the object guid to find a rhino object. Deleted objects cannot be found by id.
    /// The guid is the value that is stored on RhinoObject.Id</para>
    /// In a single document, no two active objects have the same guid. If an object is
    /// replaced with a new object, then the guid  persists. For example, if the _Move command
    /// moves an object, then the moved object inherits it's guid from the starting object.
    /// If the Copy command copies an object, then the copy gets a new guid. This guid persists
    /// through file saving/opening operations. This function will not find grip objects.
    /// </summary>
    /// <param name="id">ID of object to search for.</param>
    /// <returns>Reference to the rhino object with the objectId or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public override RhinoObject FindId(Guid id)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoDoc_LookupObject(m_doc.RuntimeSerialNumber, id);
      return RhinoObject.CreateRhinoObjectHelper(ptr);
    }

    /// <summary>
    /// Finds the location of a point, if a point exists in the document.
    /// </summary>
    /// <param name="id">ID of point object to search for.</param>
    /// <param name="point">The point will be passed here.</param>
    /// <returns>true on success; false if point was not found, id represented another geometry type, or on error.</returns>
    /// <since>6.0</since>
    public bool TryFindPoint(Guid id, out Point3d point)
    {
      bool rc = false;
      point = Point3d.Unset;
      if (UnsafeNativeMethods.CRhinoDoc_LookupObjectPoint3d(m_doc.RuntimeSerialNumber, id, ref point) && point != Point3d.Unset)
        rc = true;
      return rc;
    }

    /// <summary>
    /// Same as FindId, but returns the Geometry property directly, if it exists.
    /// </summary>
    /// <param name="id">ID of object to search for.</param>
    /// <returns>Reference to the geometry in the rhino object with the objectId or null if no such object could be found.</returns>
    /// <since>6.0</since>
    public GeometryBase FindGeometry(Guid id)
    {
      var temp = this.FindId(id);

      return temp?.Geometry;
    }

    //[skipping]
    //  const ON_Object* LookupDocumentObject( ON_UUID id, bool bIgnoreDeleted ) const;

    /// <summary>
    /// Use the object runtime serial number to find a rhino object in the document. This is the value stored on
    /// RhinoObject.RuntimeObjectSerialNumber. The RhinoObject constructor sets the runtime serial number and every
    /// instance of a RhinoObject class will have a unique serial number for the duration of the Rhino application.
    /// If an object is replaced with a new object, then the new object will have a different runtime serial number.
    /// Deleted objects stored in the undo list maintain their runtime serial numbers and this function will return
    /// pointers to these objects. Call RhinoObject.IsDeleted if you need to determine if the returned object is
    /// active or deleted.  The runtime serial number is not saved in files.
    /// </summary>
    /// <param name="runtimeSerialNumber">Runtime serial number to search for.</param>
    /// <returns>Reference to the rhino object with the objectId or null if no such object could be found.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public RhinoObject Find(uint runtimeSerialNumber)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoDoc_LookupObjectByRuntimeSerialNumber(m_doc.RuntimeSerialNumber, runtimeSerialNumber);
      return RhinoObject.CreateRhinoObjectHelper(ptr);
    }

    /// <summary>
    /// Finds all RhinoObjects that are in a given group.
    /// </summary>
    /// <param name="groupIndex">Index of group to search for.</param>
    /// <returns>An array of objects that belong to the specified group or null if no objects could be found.</returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByGroup(int groupIndex)
    {
      var rhobjs = new Runtime.InternalRhinoObjectArray();
      IntPtr pArray = rhobjs.NonConstPointer();
      UnsafeNativeMethods.CRhinoDoc_LookupObjectsByGroup(m_doc.RuntimeSerialNumber, groupIndex, pArray);
      RhinoObject[] rc = rhobjs.ToArray();
      rhobjs.Dispose();
      return rc;
    }

    /// <summary>
    /// Finds all RhinoObjects that are in a given layer.
    /// </summary>
    /// <param name="layer">Layer to search.</param>
    /// <returns>
    /// Array of objects that belong to the specified group or null if no objects could be found.
    /// </returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByLayer(Layer layer)
    {
      int layer_index = layer.Index;
      var rhobjs = new Runtime.InternalRhinoObjectArray();
      IntPtr ptr_array = rhobjs.NonConstPointer();
      UnsafeNativeMethods.CRhinoDoc_LookupObjectsByLayer(m_doc.RuntimeSerialNumber, layer_index, ptr_array);
      RhinoObject[] rc = rhobjs.ToArray();
      rhobjs.Dispose();
      return rc;
    }

    /// <summary>
    /// Finds all RhinoObjects that are in a given layer.
    /// </summary>
    /// <param name="layerName">Name of layer to search.</param>
    /// <returns>
    /// Array of objects that belong to the specified group or null if no objects could be found.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_sellayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_sellayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_sellayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoObject[] FindByLayer(string layerName)
    {
      Layer layer = Document.Layers.FindName(layerName);
      if (layer == null) return null;

      return FindByLayer(layer);
    }

    /// <summary>
    /// Same as GetObjectList but converts the result to an array.
    /// </summary>
    /// <param name="filter">The <see cref="Rhino.DocObjects.ObjectEnumeratorSettings"/> filter to customize inclusion requirements.</param>
    /// <returns>A Rhino object array. This array can be empty but not null.</returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByFilter(ObjectEnumeratorSettings filter)
    {
      var list = new List<RhinoObject>(GetObjectList(filter));
      return list.ToArray();
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByObjectType(ObjectType typeFilter)
    {
      var list = new List<RhinoObject>(GetObjectList(typeFilter));
      return list.ToArray();
    }

    /// <summary>
    /// Finds all objects whose UserString matches the search patterns.
    /// </summary>
    /// <param name="key">Search pattern for UserString keys (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="value">Search pattern for UserString values (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="caseSensitive">If true, string comparison will be case sensitive.</param>
    /// <returns>An array of all objects whose UserString matches with the search patterns or null when no such objects could be found.</returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByUserString(string key, string value, bool caseSensitive)
    {
      return FindByUserString(key, value, caseSensitive, true, true, ObjectType.AnyObject);
    }
    /// <summary>
    /// Finds all objects whose UserString matches the search patterns.
    /// </summary>
    /// <param name="key">Search pattern for UserString keys (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="value">Search pattern for UserString values (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="caseSensitive">If true, string comparison will be case sensitive.</param>
    /// <param name="searchGeometry">If true, UserStrings attached to the geometry of an object will be searched.</param>
    /// <param name="searchAttributes">If true, UserStrings attached to the attributes of an object will be searched.</param>
    /// <param name="filter">Object type filter.</param>
    /// <returns>An array of all objects whose UserString matches with the search patterns or null when no such objects could be found.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByUserString(string key, string value, bool caseSensitive, bool searchGeometry, bool searchAttributes, ObjectType filter)
    {
      var oes = new ObjectEnumeratorSettings
      {
        ActiveObjects = true,
        HiddenObjects = true,
        LockedObjects = true,
        NormalObjects = true,
        IncludeLights = true,
        ReferenceObjects = true,
        IdefObjects = false,
        IncludeGrips = false,
        DeletedObjects = false,
        IncludePhantoms = false,
        SelectedObjectsFilter = false,
        ObjectTypeFilter = filter
      };

      return FindByUserString(key, value, caseSensitive, searchGeometry, searchAttributes, oes);
    }
    /// <summary>
    /// Finds all objects whose UserString matches the search patterns.
    /// </summary>
    /// <param name="key">Search pattern for UserString keys (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="value">Search pattern for UserString values (supported wildcards are: ? = any single character, * = any sequence of characters).</param>
    /// <param name="caseSensitive">If true, string comparison will be case sensitive.</param>
    /// <param name="searchGeometry">If true, UserStrings attached to the geometry of an object will be searched.</param>
    /// <param name="searchAttributes">If true, UserStrings attached to the attributes of an object will be searched.</param>
    /// <param name="filter"><see cref="Rhino.DocObjects.ObjectEnumeratorSettings"/> filter used to restrict the number of objects searched.</param>
    /// <returns>An array of all objects whose UserString matches with the search patterns or null when no such objects could be found.</returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByUserString(string key, string value, bool caseSensitive, bool searchGeometry, bool searchAttributes, ObjectEnumeratorSettings filter)
    {
      var rhobjs = new Runtime.InternalRhinoObjectArray();
      IntPtr ptr_array = rhobjs.NonConstPointer();

      var it = new ObjectIterator(m_doc, filter);
      IntPtr ptr_iterator = it.NonConstPointer();

      UnsafeNativeMethods.CRhinoDoc_LookupObjectsByUserText(key, value, caseSensitive, searchGeometry, searchAttributes, ptr_iterator, ptr_array);

      RhinoObject[] objs = rhobjs.ToArray();
      rhobjs.Dispose();
      return objs;
    }

    /// <summary>
    /// Finds all objects whose draw color matches a given color.
    /// </summary>
    /// <param name="drawColor">The alpha value of this color is ignored.</param>
    /// <param name="includeLights">true if lights should be included.</param>
    /// <returns>An array of Rhino document objects. This array can be empty.</returns>
    /// <since>5.0</since>
    public RhinoObject[] FindByDrawColor(System.Drawing.Color drawColor, bool includeLights)
    {
      var it = new ObjectEnumeratorSettings
      {
        IncludeLights = includeLights,
        IncludeGrips = false,
        IncludePhantoms = true
      };
      var rc = new List<RhinoObject>();
      foreach (RhinoObject obj in GetObjectList(it))
      {
        System.Drawing.Color object_color = obj.Attributes.DrawColor(m_doc);
        if (object_color.R == drawColor.R && object_color.G == drawColor.G && object_color.B == drawColor.B)
          rc.Add(obj);
      }
      return rc.ToArray();
    }

    RhinoObject[] FindByRegion(RhinoViewport viewport, IEnumerable<Point3d> region, int mode, ObjectType filter)
    {
      IntPtr ptr_const_viewport = viewport.ConstPointer();
      var list_region = new List<Point3d>(region);
      Point3d[] array_points = list_region.ToArray();
      using (var objrefs = new ClassArrayObjRef())
      {
        IntPtr ptr_objref_array = objrefs.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoRegionSelect(ptr_const_viewport, array_points.Length, array_points, mode, (uint)filter, ptr_objref_array);
        var objref_array = objrefs.ToNonConstArray();
        var rc = new List<RhinoObject>();
        for (int i = 0; i < objref_array.Length; i++)
        {
          RhinoObject rhobj = objref_array[i].Object();
          if (rhobj != null)
            rc.Add(rhobj);
        }
        return rc.ToArray();
      }
    }

    RhinoObject[] FindByRegion(RhinoViewport viewport, Point2d screen1, Point2d screen2, int mode, ObjectType filter)
    {
      double min_x = screen1.X < screen2.X ? screen1.X : screen2.X;
      double max_x = screen1.X > screen2.X ? screen1.X : screen2.X;
      double min_y = screen1.Y < screen2.Y ? screen1.Y : screen2.Y;
      double max_y = screen1.Y > screen2.Y ? screen1.Y : screen2.Y;
      var screen_to_world = viewport.GetTransform(CoordinateSystem.Screen, CoordinateSystem.World);
      var pts = new Point3d[]{new Point3d(min_x, min_y, 0),
        new Point3d(max_x, min_y, 0),
        new Point3d(max_x, max_y, 0),
        new Point3d(min_x, max_y, 0)};
      for (int i = 0; i < pts.Length; i++)
      {
        pts[i].Transform(screen_to_world);
      }
      return FindByRegion(viewport, pts, mode, filter);
    }

    /// <summary>
    /// Finds objects bounded by a polyline region
    /// </summary>
    /// <param name="viewport">viewport to use for selection</param>
    /// <param name="region">list of points that define the </param>
    /// <param name="inside">should objects returned be the ones inside of this region (or outside)</param>
    /// <param name="filter">filter down list by object type</param>
    /// <returns>An array of RhinoObjects that are inside of this region</returns>
    /// <since>5.7</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByWindowRegion(RhinoViewport viewport, IEnumerable<Point3d> region, bool inside, ObjectType filter)
    {
      // 0=window, 1=crossing, 2=outside window, 3=outside crossing window
      return FindByRegion(viewport, region, inside ? 0 : 2, filter);
    }

    /// <summary>
    /// Finds objects bounded by a polyline region
    /// </summary>
    /// <param name="viewport">viewport to use for selection</param>
    /// <param name="screen1">first screen corner</param>
    /// <param name="screen2">second screen corner</param>
    /// <param name="inside">should objects returned be the ones inside of this region (or outside)</param>
    /// <param name="filter">filter down list by object type</param>
    /// <returns>An array of RhinoObjects that are inside of this region</returns>
    /// <since>5.8</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByWindowRegion(RhinoViewport viewport, Point2d screen1, Point2d screen2, bool inside, ObjectType filter)
    {
      // 0=window, 1=crossing, 2=outside window, 3=outside crossing window
      return FindByRegion(viewport, screen1, screen2, inside ? 0 : 2, filter);
    }

    /// <summary>
    /// Finds objects bounded by a polyline region
    /// </summary>
    /// <param name="viewport">viewport to use for selection</param>
    /// <param name="region">list of points that define the </param>
    /// <param name="inside">should objects returned be the ones inside of this region (or outside)</param>
    /// <param name="filter">filter down list by object type</param>
    /// <returns>An array of RhinoObjects that are inside of this region</returns>
    /// <since>5.7</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByCrossingWindowRegion(RhinoViewport viewport, IEnumerable<Point3d> region, bool inside, ObjectType filter)
    {
      // 0=window, 1=crossing, 2=outside window, 3=outside crossing window
      return FindByRegion(viewport, region, inside ? 1 : 3, filter);
    }

    /// <summary>
    /// Finds objects bounded by a region
    /// </summary>
    /// <param name="viewport">viewport to use for selection</param>
    /// <param name="screen1">first screen corner</param>
    /// <param name="screen2">second screen corner</param>
    /// <param name="inside">should objects returned be the ones inside of this region (or outside)</param>
    /// <param name="filter">filter down list by object type</param>
    /// <returns>An array of RhinoObjects that are inside of this region</returns>
    /// <since>5.8</since>
    [CLSCompliant(false)]
    public RhinoObject[] FindByCrossingWindowRegion(RhinoViewport viewport, Point2d screen1, Point2d screen2, bool inside, ObjectType filter)
    {
      // 0=window, 1=crossing, 2=outside window, 3=outside crossing window
      return FindByRegion(viewport, screen1, screen2, inside ? 1 : 3, filter);
    }

    /// <summary>
    /// Finds all of the clipping plane objects that actively clip a viewport.
    /// </summary>
    /// <param name="viewport">The viewport in which clipping planes are searched.</param>
    /// <returns>An array of clipping plane objects. The array can be empty but not null.</returns>
    /// <since>5.0</since>
    public ClippingPlaneObject[] FindClippingPlanesForViewport(RhinoViewport viewport)
    {
      Guid id = viewport.Id;

      RhinoObject[] clipping_planes = FindByObjectType(ObjectType.ClipPlane);
      if (clipping_planes.Length == 0)
        return new ClippingPlaneObject[0];

      var rc = new List<ClippingPlaneObject>();
      for (int i = 0; i < clipping_planes.Length; i++)
      {
        var cp = clipping_planes[i] as ClippingPlaneObject;
        if (cp != null)
        {
          Guid[] ids = cp.ClippingPlaneGeometry.ViewportIds();
          for (int j = 0; j < ids.Length; j++)
          {
            if (ids[j] == id)
            {
              rc.Add(cp);
              break;
            }
          }
        }
      }
      return rc.ToArray();
    }

    #region Object addition
    /// <since>5.0</since>
    public void AddRhinoObject(Custom.CustomMeshObject meshObject)
    {
      AddRhinoObjectHelper(meshObject, null, null);
    }

    /// <since>6.1</since>
    public void AddRhinoObject(Custom.CustomMeshObject meshObject, HistoryRecord history)
    {
      AddRhinoObjectHelper(meshObject, null, history);
    }

    /// <since>5.0</since>
    public void AddRhinoObject(MeshObject meshObject, Mesh mesh)
    {
      AddRhinoObjectHelper(meshObject, mesh, null);
    }

    /// <since>5.0</since>
    public void AddRhinoObject(Custom.CustomBrepObject brepObject)
    {
      //helper will use geometry already hung off of the custom object
      AddRhinoObjectHelper(brepObject, null, null);
    }

    /// <since>6.1</since>
    public void AddRhinoObject(Custom.CustomBrepObject brepObject, HistoryRecord history)
    {
      //helper will use geometry already hung off of the custom object
      AddRhinoObjectHelper(brepObject, null, history);
    }

    /// <since>5.0</since>
    public void AddRhinoObject(BrepObject brepObject, Brep brep)
    {
      AddRhinoObjectHelper(brepObject, brep, null);
    }
    /*
    public void AddRhinoObject(Rhino.DocObjects.PointCloudObject pointCloudObject, Rhino.Geometry.PointCloud pointCloud)
    {
      AddRhinoObjectHelper(pointCloudObject, pointCloud);
    }
    */
    /// <since>5.6</since>
    public void AddRhinoObject(Custom.CustomPointObject pointObject)
    {
      AddRhinoObjectHelper(pointObject, null, null);
    }
    /// <since>6.1</since>
    public void AddRhinoObject(Custom.CustomPointObject pointObject, HistoryRecord history)
    {
      AddRhinoObjectHelper(pointObject, null, history);
    }

    /// <since>5.6</since>
    public void AddRhinoObject(PointObject pointObject, Point point)
    {
      AddRhinoObjectHelper(pointObject, point, null);
    }
    /// <since>5.0</since>
    public void AddRhinoObject(CurveObject curveObject, Curve curve)
    {
      AddRhinoObjectHelper(curveObject, curve, null);
    }
    /// <since>6.1</since>
    public void AddRhinoObject(Custom.CustomCurveObject curveObject, HistoryRecord history)
    {
      AddRhinoObjectHelper(curveObject, null, history);
    }

    void AddRhinoObjectHelper(RhinoObject rhinoObject, GeometryBase geometry, HistoryRecord history)
    {
      bool is_proper_subclass = rhinoObject is BrepObject ||
                                rhinoObject is Custom.CustomCurveObject ||
                                rhinoObject is MeshObject ||
                                rhinoObject is PointObject;

      // Once the deprecated functions are removed, we should switch to checking for custom subclasses
      //bool is_proper_subclass = rhinoObject is Rhino.DocObjects.Custom.CustomBrepObject ||
      //                          rhinoObject is Rhino.DocObjects.Custom.CustomCurveObject ||
      //                          rhinoObject is Rhino.DocObjects.Custom.CustomMeshObject;
      if (!is_proper_subclass)
        throw new NotImplementedException();

      if (rhinoObject.Document != null)
        throw new NotImplementedException();

      Type t = rhinoObject.GetType();
      if (t.GetConstructor(Type.EmptyTypes) == null)
        throw new NotImplementedException("class must have a public parameterless constructor");

      IntPtr pRhinoObject = rhinoObject.m_pRhinoObject;
      if (geometry != null)
      {
        if ((rhinoObject is BrepObject && !(geometry is Brep)) ||
            (rhinoObject is CurveObject && !(geometry is Curve)) ||
            (rhinoObject is MeshObject && !(geometry is Mesh)) ||
            (rhinoObject is PointObject && !(geometry is Point)))
        {
          throw new NotImplementedException("geometry type does not match rhino object class");
        }
        IntPtr const_ptr_geometry = geometry.ConstPointer();
        pRhinoObject = UnsafeNativeMethods.CRhinoCustomObject_New(pRhinoObject, const_ptr_geometry);
      }
      else
      {
        GeometryBase g = rhinoObject.Geometry;
        if (g == null)
          throw new NotImplementedException("no geometry associated with this RhinoObject");
      }

      uint serial_number = UnsafeNativeMethods.CRhinoObject_RuntimeSN(pRhinoObject);
      if (serial_number > 0)
      {
        rhinoObject.m_rhinoobject_serial_number = serial_number;
        rhinoObject.m_pRhinoObject = IntPtr.Zero;
        GC.SuppressFinalize(rhinoObject);
        AddCustomObjectForTracking(serial_number, rhinoObject, pRhinoObject);
        IntPtr ptrHistory = (history!=null) ? history.Handle : IntPtr.Zero;
        UnsafeNativeMethods.CRhinoDoc_AddRhinoObject(m_doc.RuntimeSerialNumber, pRhinoObject, ptrHistory);
      }
    }

    SortedList<uint, RhinoObject> m_custom_objects;
    internal void AddCustomObjectForTracking(uint serialNumber, RhinoObject rhobj, IntPtr pRhinoObject)
    {
      if (m_custom_objects == null)
        m_custom_objects = new SortedList<uint, RhinoObject>();

      // 27 May 2014 S. Baer (RH-27356)
      // This function is just responsible for tracking Custom RhinoObject classes
      // and may be called multiple times with the same object. That's fine, we just
      // want to be able to track and being called several times is much better than
      // never being called.
      //
      // If the dictionary already contains the custom object, just bail out
      if (m_custom_objects.ContainsKey(serialNumber))
        return;

      m_custom_objects.Add(serialNumber, rhobj);

      // 17 Sept 2012 S. Baer
      // This seems like the best spot to get everything in sync.
      // Update the description strings when replacing the object
      Type base_type = typeof(RhinoObject);
      Type t = rhobj.GetType();
      const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
      MethodInfo mi = t.GetMethod("ShortDescription", flags);
      // Don't set description strings if the function has not been overloaded
      if (mi.DeclaringType != base_type)
      {
        string description = rhobj.ShortDescription(false);
        string description_plural = rhobj.ShortDescription(true);
        UnsafeNativeMethods.CRhinoCustomObject_SetDescriptionStrings(pRhinoObject, description, description_plural);
      }
    }
    internal RhinoObject FindCustomObject(uint serialNumber)
    {
      RhinoObject rc = null;
      if (m_custom_objects != null)
        m_custom_objects.TryGetValue(serialNumber, out rc);
      return rc;
    }

    void ICollection<RhinoObject>.Add(RhinoObject item)
    {
      if (item == null) throw new ArgumentNullException("item");
      var dup_geom = item.Geometry.Duplicate();
      var dup_attr = item.Attributes.Duplicate();
      if (Add(dup_geom, dup_attr) == Guid.Empty) throw new NotSupportedException("Could not add duplicate of RhinoObject.");
    }

    /// <summary>
    /// Adds geometry that is not further specified.
    /// <para>This is meant, for example, to handle addition of sets of different geometrical entities.</para>
    /// </summary>
    /// <param name="geometry">The base geometry. This cannot be null.</param>
    /// <returns>The new object ID on success.</returns>
    /// <exception cref="ArgumentNullException">If geometry is null.</exception>
    /// <since>5.0</since>
    public Guid Add(GeometryBase geometry)
    {
      return Add(geometry, null);
    }

    /// <summary>
    /// Adds geometry that is not further specified.
    /// <para>This is meant, for example, to handle addition of sets of different geometrical entities.</para>
    /// </summary>
    /// <param name="geometry">The base geometry. This cannot be null.</param>
    /// <param name="attributes">The object attributes. This can be null.</param>
    /// <returns>The new object ID on success.</returns>
    /// <exception cref="ArgumentNullException">If geometry is null.</exception>
    /// <since>5.0</since>
    public Guid Add(GeometryBase geometry, ObjectAttributes attributes)
    {
      return Add(geometry, attributes, null, false);
    }

    /// <summary>
    /// Adds geometry that is not further specified.
    /// <para>This is meant, for example, to handle addition of sets of different geometrical entities.</para>
    /// </summary>
    /// <param name="geometry">The base geometry. This cannot be null.</param>
    /// <param name="attributes">The object attributes. This can be null.</param>
    /// <param name="history">The history information that will be saved.</param>
    /// <param name="reference">If reference is true, object will not be saved in the 3dm file.</param>
    /// <returns>The new object ID on success.</returns>
    /// <exception cref="ArgumentNullException">If geometry is null.</exception>
    /// <since>6.0</since>
    public Guid Add(GeometryBase geometry, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (geometry == null)
      {
        throw new ArgumentNullException(nameof(geometry));
      }

      Guid obj_id;
      switch (geometry.ObjectType)
      {
        case ObjectType.Point:
          obj_id = AddPoint((Point)geometry, attributes, history, reference);
          break;
        case ObjectType.PointSet:
          obj_id = AddPointCloud((PointCloud)geometry, attributes, history, reference);
          break;
        case ObjectType.Curve:
          obj_id = AddCurve((Curve)geometry, attributes, history, reference);
          break;
        case ObjectType.Surface:
          obj_id = AddSurface((Surface)geometry, attributes, history, reference);
          break;
        case ObjectType.Brep:
          obj_id = AddBrep((Brep)geometry, attributes, history, reference);
          break;
        case ObjectType.Mesh:
          obj_id = AddMesh((Mesh)geometry, attributes, history, reference);
          break;
        case ObjectType.Light:
          Light light = (Light)geometry;
          if (history != null)
            throw new NotImplementedException("Add currently does not support history for lights.");
          if (reference)
            throw new NotImplementedException("Add currently does not support reference for lights.");
          var index = Document.Lights.Add(light, attributes);
          if (index < 0) return Guid.Empty;
          obj_id = Document.Lights[index].Id;
          break;

        case ObjectType.Annotation:
          switch (geometry)
          {
            case TextEntity te:         obj_id = AddText(te, attributes, history, reference); break;
            case Leader le:             obj_id = AddLeader(le, attributes, history, reference); break;
            case Centermark cm:         obj_id = AddCentermark(cm, attributes, history, reference); break;
            case AngularDimension ad:   obj_id = AddAngularDimension(ad, attributes, history, reference); break;
            case LinearDimension ld:    obj_id = AddLinearDimension(ld, attributes, history, reference); break;
            case RadialDimension rd:    obj_id = AddRadialDimension(rd, attributes, history, reference); break;
            case OrdinateDimension od:  obj_id = AddOrdinateDimension(od, attributes, history, reference); break;
            default: throw new NotImplementedException("Add currently does not support this annotation type.");
          }
          break;
          
        case ObjectType.InstanceDefinition:
          throw new NotImplementedException("Add currently does not support instance definition types.");
        case ObjectType.InstanceReference:
          var iref = (InstanceReferenceGeometry) geometry;
          var idef = Document.InstanceDefinitions.FindId(iref.ParentIdefId);
          if (idef is null) return Guid.Empty;
          obj_id = AddInstanceObject(idef.Index, iref.Xform, attributes, history, reference);
          break;
        case ObjectType.TextDot:
          obj_id = AddTextDot((TextDot)geometry, attributes, history, reference);
          break;
        case ObjectType.Grip:
          throw new NotImplementedException("Add currently does not support grip types.");
        case ObjectType.Detail:
          throw new NotImplementedException("Add currently does not support detail types.");
        case ObjectType.Hatch:
          obj_id = AddHatch((Hatch)geometry, attributes, history, reference);
          break;
        case ObjectType.MorphControl:
          obj_id = AddMorphControl((MorphControl)geometry, attributes, history, reference);
          break;
        case ObjectType.SubD:
          obj_id = AddSubD(geometry as SubD, attributes, history, reference);
          break;
        case ObjectType.Cage:
          throw new NotImplementedException("Add currently does not support cage types.");
        case ObjectType.Phantom:
          throw new NotImplementedException("Add currently does not support phantom types.");
        case ObjectType.ClipPlane:
          throw new NotSupportedException("Add currently does not support clipping planes.");
        case ObjectType.Extrusion:
          obj_id = AddExtrusion((Extrusion)geometry, attributes, history, reference);
          break;
        default:
          throw new NotSupportedException("Only native types can be added to the document.");
      }
      return obj_id;
    }

    /// <summary>
    /// Returns the amount of history records in this document.
    /// </summary>
    /// <since>6.0</since>
    public int HistoryRecordCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHistoryRecordTable_GetCount(m_doc.RuntimeSerialNumber);
      }
    }


    /// <summary>
    /// Adds a point object to the document.
    /// </summary>
    /// <param name="x">X component of point coordinate.</param>
    /// <param name="y">Y component of point coordinate.</param>
    /// <param name="z">Z component of point coordinate.</param>
    /// <returns>A unique identifier for the object..</returns>
    /// <since>5.0</since>
    public Guid AddPoint(double x, double y, double z)
    {
      return AddPoint(new Point3d(x, y, z));
    }
    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddPoint(Point3d point)
    {
      return AddPoint(point, null);
    }
    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <param name="attributes">attributes to apply to point. null is acceptable</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3d point, ObjectAttributes attributes)
    {
      return AddPoint(point, attributes, null, false);
    }

    /// <summary>Adds a point object to the document</summary>
    /// <param name="point">location of point</param>
    /// <param name="attributes">attributes to apply to point. null is acceptable</param>
    /// <param name="history">history associated with this point. null is acceptable</param>
    /// <param name="reference">
    /// true if the object is from a reference file.  Reference objects do
    /// not persist in archives
    /// </param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3d point, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr const_ptr_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_history = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddPoint(m_doc.RuntimeSerialNumber, point, const_ptr_attributes, ptr_history, reference);
    }

    /// <summary>Adds a point object and its geometry-linked information to the document</summary>
    /// <param name="point">A point geometry class.</param>
    /// <param name="attributes">attributes to apply to point. null is acceptable</param>
    /// <param name="history">history associated with this point. null is acceptable</param>
    /// <param name="reference">
    /// true if the object is from a reference file.  Reference objects do
    /// not persist in archives
    /// </param>
    /// <returns>A unique identifier for the object.</returns>
    /// <exception cref="ArgumentNullException">If point is null.</exception>
    /// <since>6.0</since>
    public Guid AddPoint(Point point, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (point == null)
        throw new ArgumentNullException("point");

      IntPtr const_ptr_pt = point.ConstPointer();
      IntPtr const_ptr_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_history = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddPoint2(m_doc.RuntimeSerialNumber, const_ptr_pt, const_ptr_attributes, ptr_history, reference);
    }

    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddPoint(Point3f point)
    {
      Point3d pt = new Point3d(point);
      return AddPoint(pt);
    }
    /// <summary>Adds a point object to the document.</summary>
    /// <param name="point">location of point.</param>
    /// <param name="attributes">attributes to apply to point.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPoint(Point3f point, ObjectAttributes attributes)
    {
      var pt = new Point3d(point);
      return AddPoint(pt, attributes);
    }


    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <returns>List of object ids.</returns>
    /// <since>5.0</since>
    public RhinoList<Guid> AddPoints(IEnumerable<Point3d> points)
    {
      if (points == null)
        throw new ArgumentNullException("points");

      var ids = new RhinoList<Guid>();
      foreach (var pt in points)
      {
        ids.Add(AddPoint(pt));
      }

      return ids;
    }
    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="attributes">Attributes to apply to point objects.</param>
    /// <returns>List of object ids.</returns>
    /// <since>5.0</since>
    public RhinoList<Guid> AddPoints(IEnumerable<Point3d> points, ObjectAttributes attributes)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      var ids = new RhinoList<Guid>();
      foreach (var pt in points)
      {
        ids.Add(AddPoint(pt, attributes));
      }

      return ids;
    }

    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <returns>List of object ids.</returns>
    /// <since>5.0</since>
    public RhinoList<Guid> AddPoints(IEnumerable<Point3f> points)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      var ids = new RhinoList<Guid>();
      foreach (var pt in points)
      {
        ids.Add(AddPoint(pt));
      }

      return ids;
    }
    /// <summary>
    /// Adds multiple points to the document.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="attributes">Attributes to apply to point objects.</param>
    /// <returns>List of object ids.</returns>
    /// <since>5.0</since>
    public RhinoList<Guid> AddPoints(IEnumerable<Point3f> points, ObjectAttributes attributes)
    {
      if (points == null) { throw new ArgumentNullException("points"); }

      var ids = new RhinoList<Guid>();
      foreach (var pt in points)
      {
        ids.Add(AddPoint(pt, attributes));
      }

      return ids;
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
    /// <param name="attributes">Attributes to apply to point cloud.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(PointCloud cloud, ObjectAttributes attributes)
    {
      return AddPointCloud(cloud, attributes, null, false);
    }

    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="cloud">PointCloud to add.</param>
    /// <param name="attributes">Attributes to apply to point cloud. null is acceptable</param>
    /// <param name="history">history associated with this point cloud. null is acceptable</param>
    /// <param name="reference">
    /// true if the object is from a reference file.  Reference objects do
    /// not persist in archives
    /// </param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(PointCloud cloud, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (cloud == null)
        throw new ArgumentNullException("cloud");

      IntPtr const_ptr_cloud = cloud.ConstPointer();
      IntPtr const_ptr_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_history = (history == null) ? IntPtr.Zero : history.Handle;

      return UnsafeNativeMethods.CRhinoDoc_AddPointCloud2(m_doc.RuntimeSerialNumber, const_ptr_cloud, const_ptr_attributes, ptr_history, reference);
    }

    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="points">A list, an array or any enumerable set of points.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(IEnumerable<Point3d> points)
    {
      return AddPointCloud(points, null);
    }
    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="points">A list, an array or any enumerable set of points.</param>
    /// <param name="attributes">attributes to apply to point cloud.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(IEnumerable<Point3d> points, ObjectAttributes attributes)
    {
      int count;
      var point_array = RhinoListHelpers.GetConstArray(points, out count);
      if (null == point_array || count < 1)
        return Guid.Empty;

      IntPtr const_ptr_attributes = IntPtr.Zero;
      if (null != attributes)
        const_ptr_attributes = attributes.ConstPointer();

      return UnsafeNativeMethods.CRhinoDoc_AddPointCloud(m_doc.RuntimeSerialNumber, count, point_array, const_ptr_attributes, IntPtr.Zero, false);
    }

    /// <summary>Adds a point cloud object to the document.</summary>
    /// <param name="points">A list, an array or any enumerable set of points</param>
    /// <param name="attributes">Attributes to apply to point cloud. null is acceptable</param>
    /// <param name="history">history associated with this point cloud. null is acceptable</param>
    /// <param name="reference">
    /// true if the object is from a reference file.  Reference objects do
    /// not persist in archives
    /// </param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPointCloud(IEnumerable<Point3d> points, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      int count;
      var point_array = RhinoListHelpers.GetConstArray(points, out count);
      if (null == point_array || count < 1)
        return Guid.Empty;

      IntPtr const_ptr_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_history = (history == null) ? IntPtr.Zero : history.Handle;

      return UnsafeNativeMethods.CRhinoDoc_AddPointCloud(m_doc.RuntimeSerialNumber, count, point_array, const_ptr_attributes, ptr_history, reference);
    }

    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">The plane value.</param>
    /// <param name="uMagnitude">The size in the U direction.</param>
    /// <param name="vMagnitude">The size in the V direction.</param>
    /// <param name="clippedViewportId">Viewport ID that the new clipping plane will clip.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addclippingplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addclippingplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_addclippingplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId)
    {
      return AddClippingPlane(plane, uMagnitude, vMagnitude, new [] { clippedViewportId });
    }

    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">The plane value.</param>
    /// <param name="uMagnitude">The size in the U direction.</param>
    /// <param name="vMagnitude">The size in the V direction.</param>
    /// <param name="clippedViewportIds">A list, an array or any enumerable set of viewport IDs
    /// that the new clipping plane will clip.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds)
    {
      if (null == clippedViewportIds)
        return Guid.Empty;
      var ids = new List<Guid>();
      foreach (Guid item in clippedViewportIds)
        ids.Add(item);
      Guid[] clippedIds = ids.ToArray();
      int count = clippedIds.Length;
      if (count < 1)
        return Guid.Empty;
      Guid rc = UnsafeNativeMethods.CRhinoDoc_AddClippingPlane(m_doc.RuntimeSerialNumber, ref plane, uMagnitude, vMagnitude, count, clippedIds, IntPtr.Zero, IntPtr.Zero, false);
      return rc;
    }
    /// <summary>
    /// Adds a clipping plane object to Rhino.
    /// </summary>
    /// <param name="plane">The plane value.</param>
    /// <param name="uMagnitude">The size in the U direction.</param>
    /// <param name="vMagnitude">The size in the V direction.</param>
    /// <param name="clippedViewportIds">A list, an array or any enumerable set of viewport IDs
    /// that the new clipping plane will clip.</param>
    /// <param name="attributes">Document attributes for the plane.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, ObjectAttributes attributes)
    {
      if (null == attributes)
        return AddClippingPlane(plane, uMagnitude, vMagnitude, clippedViewportIds);
      if (null == clippedViewportIds)
        return Guid.Empty;
      List<Guid> ids = new List<Guid>();
      foreach (Guid item in clippedViewportIds)
        ids.Add(item);
      Guid[] clippedIds = ids.ToArray();
      int count = clippedIds.Length;
      if (count < 1)
        return Guid.Empty;
      IntPtr pAttrs = attributes.ConstPointer();
      Guid rc = UnsafeNativeMethods.CRhinoDoc_AddClippingPlane(m_doc.RuntimeSerialNumber, ref plane, uMagnitude, vMagnitude, count, clippedIds, pAttrs, IntPtr.Zero, false);
      return rc;
    }

    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      return AddClippingPlane(plane, uMagnitude, vMagnitude, new Guid[] { clippedViewportId }, attributes, history, reference);
    }
    /// <since>5.0</since>
    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      List<Guid> ids = new List<Guid>(clippedViewportIds);
      Guid[] clippedIds = ids.ToArray();
      int count = clippedIds.Length;
      if (count < 1)
        return Guid.Empty;

      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      Guid rc = UnsafeNativeMethods.CRhinoDoc_AddClippingPlane(m_doc.RuntimeSerialNumber, ref plane, uMagnitude, vMagnitude, count, clippedIds, pConstAttributes, pHistory, reference);
      return rc;
    }


    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddRadialDimension(RadialDimension dimension)
    {
      return AddRadialDimension(dimension, null);
    }

    /// <since>5.0</since>
    public Guid AddRadialDimension(RadialDimension dimension, ObjectAttributes attributes)
    {
      return AddRadialDimension(dimension, attributes, null, false);
    }

    /// <summary>
    /// Adds a radial dimension object to the document.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddRadialDimension(RadialDimension dimension, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == dimension)
        throw new ArgumentNullException(nameof(dimension));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var const_ptr_dimradial = dimension.ConstPointer();
      var attributes_pointer = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddV6RadialDimension(doc_id, const_ptr_dimradial, attributes_pointer, p_history, reference);
      return success;
    }

    /// <summary>
    /// Adds a rectangle to the object table.
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddRectangle(Rectangle3d rectangle)
    {
      return AddRectangle(rectangle, null);
    }

    /// <summary>
    /// Adds a rectangle to the object table.
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <param name="attributes">Attributes that will be linked with the surface object.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddRectangle(Rectangle3d rectangle, ObjectAttributes attributes)
    {
      return AddRectangle(rectangle, attributes, null, false);
    }

    /// <summary>
    /// Adds a rectangle to the object table.
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <param name="attributes">Attributes that will be linked with the surface object.</param>
    /// <param name="history">History data records.</param>
    /// <param name="reference">If a reference, object will not be saved in the document.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddRectangle(Rectangle3d rectangle, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (!rectangle.IsValid)
        throw new ArgumentException("Rectangle is invalid.", nameof(rectangle));

      var pl = rectangle.ToPolyline();
      return AddPolyline(pl, attributes, history, reference);
    }

    /// <summary>
    /// Adds a box to the object table.
    /// </summary>
    /// <param name="box">The box.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddBox(Box box)
    {
      return AddBox(box, null);
    }

    /// <summary>
    /// Adds a box to the object table.
    /// </summary>
    /// <param name="box">The box.</param>
    /// <param name="attributes">Attributes that will be linked with the surface object.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddBox(Box box, ObjectAttributes attributes)
    {
      return AddBox(box, attributes, null, false);
    }

    /// <summary>
    /// Adds a box to the object table, as an extrusion.
    /// </summary>
    /// <param name="box">The box.</param>
    /// <param name="attributes">Attributes that will be linked with the surface object.</param>
    /// <param name="history">History data records.</param>
    /// <param name="reference">If a reference, object will not be saved in the document.</param>
    /// <returns>The ID.</returns>
    /// <since>6.0</since>
    public Guid AddBox(Box box, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (!box.IsValid)
        throw new ArgumentException("Box is invalid.", nameof(box));
      
      var extr = box.ToExtrusion();
      return AddExtrusion(extr, attributes, history, reference);
    }


    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="from">The line origin.</param>
    /// <param name="to">The line end.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddLine(Point3d from, Point3d to)
    {
      return AddLine(from, to, null);
    }
    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="from">The line origin.</param>
    /// <param name="to">The line end.</param>
    /// <param name="attributes">Attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Point3d from, Point3d to, ObjectAttributes attributes)
    {
      return AddLine(from, to, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddLine(Point3d from, Point3d to, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddLine(m_doc.RuntimeSerialNumber, from, to, pAttributes, pHistory, reference);
    }

    /// <summary>Adds a line object to Rhino.</summary>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Line line)
    {
      return AddLine(line.From, line.To);
    }
    /// <summary>Adds a line object to Rhino.</summary>
    /// <param name="line">The line value.</param>
    /// <param name="attributes">Attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddLine(Line line, ObjectAttributes attributes)
    {
      return AddLine(line.From, line.To, attributes);
    }


    /// <summary>Adds a polyline object to Rhino.</summary>
    /// <param name="points">A <see cref="Polyline"/>; a list, an array, or any enumerable set of <see cref="Point3d"/>.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_tightboundingbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_tightboundingbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_tightboundingbox.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddPolyline(IEnumerable<Point3d> points)
    {
      return AddPolyline(points, null);
    }
    /// <summary>Adds a polyline object to Rhino.</summary>
    /// <param name="points">A <see cref="Polyline"/>; a list, an array, or any enumerable set of <see cref="Point3d"/>.</param>
    /// <param name="attributes">attributes to apply to line.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddPolyline(IEnumerable<Point3d> points, ObjectAttributes attributes)
    {
      return AddPolyline(points, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddPolyline(IEnumerable<Point3d> points, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      int count;
      Point3d[] ptArray = RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 1)
        return Guid.Empty;

      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddPolyLine(m_doc.RuntimeSerialNumber, count, ptArray, pAttributes, pHistory, reference);
    }

    /// <summary>Adds a curve object to the document representing an arc.</summary>
    /// <param name="arc">An arc value.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddArc(Arc arc)
    {
      return AddArc(arc, null, null, false);
    }
    /// <summary>Adds a curve object to the document representing an arc.</summary>
    /// <param name="arc">An arc value.</param>
    /// <param name="attributes">Attributes to apply to arc.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddArc(Arc arc, ObjectAttributes attributes)
    {
      return AddArc(arc, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddArc(Arc arc, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddArc(m_doc.RuntimeSerialNumber, ref arc, pAttributes, pHistory, reference);
    }

    /// <summary>Adds a curve object to the document representing a circle.</summary>
    /// <param name="circle">A circle value.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addcircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddCircle(Circle circle)
    {
      return AddCircle(circle, null, null, false);
    }
    /// <summary>Adds a curve object to the document representing a circle.</summary>
    /// <param name="circle">A circle value.</param>
    /// <param name="attributes">Attributes to apply to circle.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCircle(Circle circle, ObjectAttributes attributes)
    {
      return AddCircle(circle, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddCircle(Circle circle, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddCircle(m_doc.RuntimeSerialNumber, ref circle, pAttributes, pHistory, reference);
    }


    /// <summary>Adds a curve object to the document representing an ellipse.</summary>
    /// <param name="ellipse">An ellipse value.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddEllipse(Ellipse ellipse)
    {
      return AddEllipse(ellipse, null, null, false);
    }
    /// <summary>Adds a curve object to the document representing an ellipse.</summary>
    /// <param name="ellipse">An ellipse value.</param>
    /// <param name="attributes">Attributes to apply to ellipse.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddEllipse(Ellipse ellipse, ObjectAttributes attributes)
    {
      return AddEllipse(ellipse, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddEllipse(Ellipse ellipse, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddEllipse(m_doc.RuntimeSerialNumber, ref ellipse, pAttributes, pHistory, reference);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_addsphere.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addsphere.cs' lang='cs'/>
    /// <code source='examples\py\ex_addsphere.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddSphere(Sphere sphere)
    {
      return AddSphere(sphere, null, null, false);
    }
    /// <since>5.0</since>
    public Guid AddSphere(Sphere sphere, ObjectAttributes attributes)
    {
      return AddSphere(sphere, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddSphere(Sphere sphere, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddSphere(m_doc.RuntimeSerialNumber, ref sphere, pAttributes, pHistory, reference);
    }

    //[skipping]
    //  CRhinoCurveObject* AddCurveObject( const ON_BezierCurve& bezier_curve, const ON_3dmObjectAttributes* pAttributes = NULL, CRhinoHistory* pHistory = NULL,  BOOL bReference = NULL );


    /// <summary>Adds a curve object to Rhino.</summary>
    /// <param name="curve">A curve. A duplicate of this curve is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addnurbscircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnurbscircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnurbscircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddCurve(Curve curve)
    {
      return AddCurve(curve, null);
    }
    /// <summary>Adds a curve object to Rhino.</summary>
    /// <param name="curve">A curve. A duplicate of this curve is added to Rhino.</param>
    /// <param name="attributes">Attributes to apply to curve.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddCurve(Curve curve, ObjectAttributes attributes)
    {
      return AddCurve(curve, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddCurve(Curve curve, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == curve)
        return Guid.Empty;
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      IntPtr curvePtr = curve.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_AddCurve(m_doc.RuntimeSerialNumber, curvePtr, pAttributes, pHistory, reference);
    }

    /// <summary>Adds a text dot object to Rhino.</summary>
    /// <param name="text">A text string.</param>
    /// <param name="location">A point position.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(string text, Point3d location)
    {
      return AddTextDot(text, location, null);
    }
    /// <summary>Adds a text dot object to Rhino.</summary>
    /// <param name="text">A text string.</param>
    /// <param name="location">A point position.</param>
    /// <param name="attributes">Attributes to apply to curve.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(string text, Point3d location, ObjectAttributes attributes)
    {
      using (var dot = new TextDot(text, location))
      {
        Guid rc = AddTextDot(dot, attributes);
        dot.Dispose();
        return rc;
      }
    }
    /// <summary>Adds a text dot object to Rhino.</summary>
    /// <param name="dot">A text dot that will be copied.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(TextDot dot)
    {
      return AddTextDot(dot, null);
    }
    /// <summary>Adds a text dot object to Rhino.</summary>
    /// <param name="dot">A text dot that will be copied.</param>
    /// <param name="attributes">Attributes to apply to text dot.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddTextDot(TextDot dot, ObjectAttributes attributes)
    {
      return AddTextDot(dot, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddTextDot(TextDot dot, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == dot)
        throw new ArgumentNullException(nameof(dot));
      IntPtr cpnst_ptr_dot = dot.ConstPointer();
      IntPtr const_ptr_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr ptr_history = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddTextDot(m_doc.RuntimeSerialNumber, cpnst_ptr_dot, const_ptr_attributes, ptr_history, reference);
    }

    /// <summary> Adds a v6_TextObject to the document. </summary>
    /// <param name="text"> Text object to add. </param>
    /// <returns> The Id of the newly added object or Guid.Empty on failure. </returns>
    /// <since>5.0</since>
    public Guid AddText(TextEntity text)
    {
      return AddText(text, null);
    }

    /// <summary> Adds a text object to the document. </summary>
    /// <param name="text">Text object to add.</param>
    /// <param name="attributes"></param>
    /// <returns>The Id of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(TextEntity text, ObjectAttributes attributes)
    {
      return AddText(text, attributes, null, false);
    }

    /// <summary> Adds a text object to the document. </summary>
    /// <param name="text">Text object to add.</param>
    /// <param name="attributes"></param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Id of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(TextEntity text, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == text)
        throw new ArgumentNullException(nameof(text));
      var doc = Document;
      var doc_id = null == doc ? 0 : doc.RuntimeSerialNumber;
      var const_ptr_text = text.ConstPointer();
      var const_ptr_attributes = null == attributes ? IntPtr.Zero : attributes.ConstPointer();
      var ptr_history = null == history ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddRichTextObject(doc_id, const_ptr_text, const_ptr_attributes, ptr_history, reference);
      return success;
    }

    /// <summary>
    /// Adds a Leader object to the document.
    /// </summary>
    /// <param name="leader">The leader object.</param>
    /// <returns>The Id of the newly added object or Guid.Empty on failure.</returns>
    /// <since>6.0</since>
    public Guid AddLeader(Leader leader)
    {
      return AddLeader(leader, null);
    }
    /// <summary> Adds Leader object to the document. </summary>
    /// <param name="leader">The leader object.</param>
    /// <param name="attributes">Attributes to apply to rich text.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>6.0</since>
    public Guid AddLeader(Leader leader, ObjectAttributes attributes)
    {
      return AddLeader(leader, attributes, null, false);
    }

    /// <summary>
    /// Adds a Leader object to the document.
    /// </summary>
    /// <param name="leader">The leader object.</param>
    /// <param name="attributes">Attributes to apply to rich text.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>6.0</since>
    public Guid AddLeader(Leader leader, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == leader)
        throw new ArgumentNullException(nameof(leader));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var const_ptr_leader = leader.ConstPointer();
      var const_ptr_attributes = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr ptr_history = (null == history) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddRhinoLeader(doc_id, const_ptr_leader, const_ptr_attributes, ptr_history, reference);
      return success;
    }

    /// <summary>
    /// Adds a linear dimension object to the document.
    /// </summary>
    /// <param name="dimension">
    /// Dimension object to add.
    /// </param>
    /// <returns>
    /// The Id of the newly added object or Guid.Empty on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlineardimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlineardimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlineardimension.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddLinearDimension(LinearDimension dimension)
    {
      return AddLinearDimension(dimension, null);
    }

    /// <summary>
    /// Adds a linear dimension object to the document.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes"></param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLinearDimension(LinearDimension dimension, ObjectAttributes attributes)
    {
      return AddLinearDimension(dimension, attributes, null, false);
    }

    /// <summary>
    /// Adds a linear dimension object to the document.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddLinearDimension(LinearDimension dimension, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == dimension)
        throw new ArgumentNullException(nameof(dimension));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var pointer = dimension.ConstPointer();
      var attributes_pointer = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddV6LinearDimension(doc_id, pointer, attributes_pointer, p_history, reference);
      return success;
    }

    /// <summary>
    /// Adds a angular dimension object to the document.
    /// </summary>
    /// <param name="dimension">
    /// Dimension object to add.
    /// </param>
    /// <returns>
    /// The Id of the newly added object or Guid.Empty on failure.
    /// </returns>
    /// <since>5.0</since>
    public Guid AddAngularDimension(AngularDimension dimension)
    {
      return AddAngularDimension(dimension, null);
    }

    /// <summary>
    /// Adds a angular dimension object to the document.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes"></param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddAngularDimension(AngularDimension dimension, ObjectAttributes attributes)
    {
      return AddAngularDimension(dimension, attributes, null, false);
    }

    /// <summary>
    /// Adds an angular dimension object to the document.
    /// </summary>
    /// <param name="dimension">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddAngularDimension(AngularDimension dimension, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == dimension)
        throw new ArgumentNullException(nameof(dimension));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var pointer = dimension.ConstPointer();
      var attributes_pointer = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddV6AngularDimension(doc_id, pointer, attributes_pointer, p_history, reference);
      return success;
    }


    /// <summary>
    /// Adds an ordinate dimension object to the document.
    /// </summary>
    /// <param name="dimordinate">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>6.0</since>
    public Guid AddOrdinateDimension(Geometry.OrdinateDimension dimordinate, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == dimordinate)
        throw new ArgumentNullException(nameof(dimordinate));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var pointer = dimordinate.ConstPointer();
      var attributes_pointer = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddV6OrdinateDimension(doc_id, pointer, attributes_pointer, p_history, reference);
      return success;
    }

    /// <summary>
    /// Adds an ordinate dimension object to the document.
    /// </summary>
    /// <param name="centermark">Dimension object to add.</param>
    /// <param name="attributes">Attributes to apply to dimension.</param>
    /// <param name="history">Object history to save.</param>
    /// <param name="reference">If reference, then object will not be saved into the 3dm file.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>6.0</since>
    public Guid AddCentermark(Centermark centermark, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == centermark)
        throw new ArgumentNullException(nameof(centermark));
      var doc = Document;
      var doc_id = (null == doc ? 0 : doc.RuntimeSerialNumber);
      var pointer = centermark.ConstPointer();
      var attributes_pointer = (null == attributes ? IntPtr.Zero : attributes.ConstPointer());
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      var success = UnsafeNativeMethods.CRhinoDoc_AddV6Centermark(doc_id, pointer, attributes_pointer, p_history, reference);
      return success;
    }
    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text3d">The text object to add.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_textjustify.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_textjustify.cs' lang='cs'/>
    /// <code source='examples\py\ex_textjustify.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddText(Text3d text3d)
    {
      return AddText(text3d, null);
    }
    /// <summary>
    /// Adds an annotation text object to the document.
    /// </summary>
    /// <param name="text3d">The text object to add.</param>
    /// <param name="attributes">Object Attributes.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(Text3d text3d, ObjectAttributes attributes)
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
    /// <example>
    /// <code source='examples\vbnet\ex_addtext.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addtext.cs' lang='cs'/>
    /// <code source='examples\py\ex_addtext.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic)
    {
      return AddText(text, plane, height, fontName, bold, italic, null);
    }

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
    /// <param name="attributes">Attributes that will be linked with the object.</param>
    /// <returns>The Guid of the newly added object or Guid.Empty on failure.</returns>
    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, ObjectAttributes attributes)
    {
      return AddText(text, plane, height, fontName, bold, italic, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification, ObjectAttributes attributes)
    {
      return AddText(text, plane, height, fontName, bold, italic, justification, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      return AddText(text, plane, height, fontName, bold, italic, (TextJustification)(-1), attributes, history, reference);
    }

    /// <since>5.0</since>
    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fontName))
        return Guid.Empty;

      int fontStyle = 0;
      if (bold)
        fontStyle |= 1;
      if (italic)
        fontStyle |= 2;

      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddText(m_doc.RuntimeSerialNumber, AnnotationBase.PlainTextToRtf(text), ref plane, height, fontName, fontStyle, (int)justification, pConstAttributes, pHistory, reference);
    }

    /// <summary>Adds a SubD object to Rhino.</summary>
    /// <param name="subD">A duplicate of this SubD is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>7.0</since>
    public Guid AddSubD(SubD subD)
    {
      return AddSubD(subD, null);
    }

    /// <summary>Adds a SubD object to Rhino.</summary>
    /// <param name="subD">A duplicate of this SubD is added to Rhino.</param>
    /// <param name="attributes">Attributes that will be linked with the object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>7.0</since>
    public Guid AddSubD(SubD subD, ObjectAttributes attributes)
    {
      return AddSubD(subD, attributes, null, false);
    }

    /// <summary>Adds a SubD object to Rhino.</summary>
    /// <param name="subD">A duplicate of this SubD is added to Rhino.</param>
    /// <param name="attributes">Attributes that will be linked with the object.</param>
    /// <param name="history"></param>
    /// <param name="reference"></param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>7.0</since>
    public Guid AddSubD(SubD subD, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (subD == null) throw new ArgumentNullException(nameof(subD));
      Guid id = Guid.Empty;
      IntPtr p_const_attributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;

      IntPtr p_const_subdref = subD.SubDRefPointer();
      if (IntPtr.Zero == p_const_subdref)
      {
        IntPtr p_const_subd = subD.ConstPointer();
        id = UnsafeNativeMethods.CRhinoDoc_AddSubD2(m_doc.RuntimeSerialNumber, p_const_subd, p_const_attributes, pHistory, reference);
      }
      else
      {
        id = UnsafeNativeMethods.CRhinoDoc_AddSubD(m_doc.RuntimeSerialNumber, p_const_subdref, p_const_attributes, pHistory, reference);
      }
      if (id != Guid.Empty)
      {
        subD.ChangeToConstObject(null);
      }
      return id;
    }

    /// <summary>Adds a surface object to Rhino.</summary>
    /// <param name="surface">A duplicate of this surface is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addtorus.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addtorus.cs' lang='cs'/>
    /// <code source='examples\py\ex_addtorus.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddSurface(Surface surface)
    {
      return AddSurface(surface, null);
    }
    /// <summary>Adds a surface object to Rhino.</summary>
    /// <param name="surface">A duplicate of this surface is added to Rhino.</param>
    /// <param name="attributes">Attributes that will be linked with the surface object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddSurface(Surface surface, ObjectAttributes attributes)
    {
      return AddSurface(surface, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddSurface(Surface surface, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == surface)
        return Guid.Empty;
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      IntPtr surfacePtr = surface.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_AddSurface(m_doc.RuntimeSerialNumber, surfacePtr, pConstAttributes, pHistory, reference);
    }

    /// <summary>Adds an extrusion object to Rhino.</summary>
    /// <param name="extrusion">A duplicate of this extrusion is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddExtrusion(Extrusion extrusion)
    {
      return AddExtrusion(extrusion, null);
    }
    /// <summary>Adds an extrusion object to Rhino.</summary>
    /// <param name="extrusion">A duplicate of this extrusion is added to Rhino.</param>
    /// <param name="attributes">Attributes that will be linked with the extrusion object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddExtrusion(Extrusion extrusion, ObjectAttributes attributes)
    {
      return AddExtrusion(extrusion, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddExtrusion(Extrusion extrusion, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == extrusion)
        return Guid.Empty;
      IntPtr pConstExtrusion = extrusion.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddExtrusion(m_doc.RuntimeSerialNumber, pConstExtrusion, pConstAttributes, pHistory, reference);
    }

    /// <summary>Adds a mesh object to Rhino.</summary>
    /// <param name="mesh">A duplicate of this mesh is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddMesh(Geometry.Mesh mesh)
    {
      return AddMesh(mesh, null);
    }
    /// <summary>Adds a mesh object to Rhino.</summary>
    /// <param name="mesh">A duplicate of this mesh is added to Rhino.</param>
    /// <param name="attributes">Attributes that will be linked with the mesh object.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddMesh(Mesh mesh, ObjectAttributes attributes)
    {
      return AddMesh(mesh, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddMesh(Mesh mesh, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      return AddMesh(mesh, attributes, history, reference, true);
    }


    /// <since>6.0</since>
    public Guid AddMesh(Mesh mesh, ObjectAttributes attributes, HistoryRecord history, bool reference, bool requireValidMesh)
    {
      if (null == mesh)
        return Guid.Empty;
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      IntPtr meshPtr = mesh.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_AddMesh(m_doc.RuntimeSerialNumber, meshPtr, pConstAttributes, pHistory, reference, requireValidMesh);
    }

    /// <summary>Adds a brep object to Rhino.</summary>
    /// <param name="brep">A duplicate of this brep is added to Rhino.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbrepbox.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddBrep(Brep brep)
    {
      return AddBrep(brep, null);
    }
    /// <summary>Adds a brep object to Rhino.</summary>
    /// <param name="brep">A duplicate of this brep is added to Rhino.</param>
    /// <param name="attributes">attributes to apply to brep.</param>
    /// <returns>A unique identifier for the object.</returns>
    /// <since>5.0</since>
    public Guid AddBrep(Brep brep, ObjectAttributes attributes)
    {
      return AddBrep(brep, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddBrep(Brep brep, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      if (null == brep)
        return Guid.Empty;
      IntPtr brepPtr = brep.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddBrep(m_doc.RuntimeSerialNumber, brepPtr, pConstAttributes, pHistory, reference, -1);
    }

    /// <since>5.0</since>
    public Guid AddBrep(Brep brep, ObjectAttributes attributes, HistoryRecord history, bool reference, bool splitKinkySurfaces)
    {
      if (null == brep)
        return Guid.Empty;
      IntPtr brepPtr = brep.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddBrep(m_doc.RuntimeSerialNumber, brepPtr, pConstAttributes, pHistory, reference, splitKinkySurfaces ? 1 : 0);
    }

    /// <since>5.11</since>
    public Guid[] AddExplodedInstancePieces(InstanceObject instance, bool explodeNestedInstances = true, bool deleteInstance = false)
    {
      var guids = new List<Guid>();
      if (instance != null)
      {
        RhinoObject[] rhino_objects;
        ObjectAttributes[] object_attributes;
        Transform[] transforms;
        instance.Explode(explodeNestedInstances, out rhino_objects, out object_attributes, out transforms);

        for (int i = 0; i < rhino_objects.Length; i++)
        {
          var guid = Guid.Empty;
          if (rhino_objects[i] is InstanceObject)
          {
            var instance_object = rhino_objects[i] as InstanceObject;
            var combined_xforms = transforms[i] * instance_object.InstanceXform;
            guid = Document.Objects.AddInstanceObject(instance_object.InstanceDefinition.Index, combined_xforms, object_attributes[i]);
          }
          else
          {
            var geometry_base = rhino_objects[i].DuplicateGeometry();
            geometry_base.Transform(transforms[i]);
            guid = Document.Objects.Add(geometry_base, object_attributes[i]);
          }
          // 31-Oct-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-42305
          if (guid != Guid.Empty) guids.Add(guid);
        }

        if (deleteInstance)
          Document.Objects.Delete(instance, true);
      }
      return guids.Count == 0 ? null : guids.ToArray();
    }

    /// <summary>
    /// Adds an instance object to the document.
    /// </summary>
    /// <param name="instanceDefinitionIndex">The index of the instance definition.</param>
    /// <param name="instanceXform">The instance transformation.</param>
    /// <returns>A unique identifier for the object if successful. Guid.Empty it not successful.</returns>
    /// <since>5.0</since>
    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform)
    {
      return UnsafeNativeMethods.CRhinoDoc_AddInstanceObject(m_doc.RuntimeSerialNumber, instanceDefinitionIndex, ref instanceXform, IntPtr.Zero);
    }

    /// <summary>
    /// Adds an instance object to the document.
    /// </summary>
    /// <param name="instanceDefinitionIndex">The index of the instance definition.</param>
    /// <param name="instanceXform">The instance transformation.</param>
    /// <param name="attributes">The attributes to apply to the instance object.</param>
    /// <returns>A unique identifier for the object if successful. Guid.Empty it not successful.</returns>
    /// <since>5.0</since>
    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform, ObjectAttributes attributes)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_AddInstanceObject(m_doc.RuntimeSerialNumber, instanceDefinitionIndex, ref instanceXform, pAttributes);
    }

    /// <summary>
    /// Adds an instance object to the document.
    /// </summary>
    /// <param name="instanceDefinitionIndex">The index of the instance definition.</param>
    /// <param name="instanceXform">The instance transformation.</param>
    /// <param name="attributes">The attributes to apply to the instance object.</param>
    /// <param name="history">The history record associated with this instance object.</param>
    /// <param name="reference">true if the object is from a reference file. Reference objects do not persist in archives.</param>
    /// <returns>A unique identifier for the object if successful. Guid.Empty it not successful.</returns>
    /// <since>6.24</since>
    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddInstanceObject2(m_doc.RuntimeSerialNumber, instanceDefinitionIndex, ref instanceXform, pAttributes, pHistory, reference);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_leader.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_leader.cs' lang='cs'/>
    /// <code source='examples\py\ex_leader.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points)
    {
      return AddLeader(null, plane, points);
    }
    /// <since>5.0</since>
    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points, ObjectAttributes attributes)
    {
      return AddLeader(null, plane, points, attributes);
    }

    /// <since>5.0</since>
    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points, ObjectAttributes attributes)
    {
      return AddLeader(text, plane, points, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      string s = null;
      if (!string.IsNullOrEmpty(text))
        s = text;
      var pts = new RhinoList<Point2d>(points);
      int count = pts.Count;
      if (count < 1)
        return Guid.Empty;
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddLeader(m_doc.RuntimeSerialNumber, s, ref plane, count, pts.m_items, pConstAttributes, pHistory, reference);
    }

    /// <since>5.0</since>
    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points)
    {
      return AddLeader(text, plane, points, null);
    }

    /// <since>5.0</since>
    public Guid AddLeader(string text, IEnumerable<Point3d> points)
    {
      Plane plane;
      //double max_deviation;
      PlaneFitResult rc = Plane.FitPlaneToPoints(points, out plane);//, out max_deviation);
      if (rc != PlaneFitResult.Success)
        return Guid.Empty;

      var points2d = new RhinoList<Point2d>();
      foreach (Point3d point3d in points)
      {
        double s, t;
        if (plane.ClosestParameter(point3d, out s, out t))
        {
          Point2d newpoint = new Point2d(s, t);
          if (points2d.Count > 0 && points2d.Last.DistanceTo(newpoint) < RhinoMath.SqrtEpsilon)
            continue;
          points2d.Add(new Point2d(s, t));
        }
      }
      return AddLeader(text, plane, points2d);
    }
    /// <since>5.0</since>
    public Guid AddLeader(IEnumerable<Point3d> points)
    {
      return AddLeader(null, points);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid AddHatch(Hatch hatch)
    {
      return AddHatch(hatch, null);
    }
    /// <since>5.0</since>
    public Guid AddHatch(Hatch hatch, ObjectAttributes attributes)
    {
      return AddHatch(hatch, attributes, null, false);
    }

    /// <since>5.0</since>
    public Guid AddHatch(Hatch hatch, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pConstHatch = hatch.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pHistory = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddHatch(m_doc.RuntimeSerialNumber, pConstHatch, pConstAttributes, pHistory, reference);
    }

    /// <since>5.0</since>
    public Guid AddMorphControl(MorphControl morphControl)
    {
      return AddMorphControl(morphControl, null);
    }

    /// <since>5.0</since>
    public Guid AddMorphControl(MorphControl morphControl, ObjectAttributes attributes)
    {
      return AddMorphControl(morphControl, attributes, null, false);
    }

    /// <since>6.0</since>
    public Guid AddMorphControl(MorphControl morphControl, ObjectAttributes attributes, HistoryRecord history, bool reference)
    {
      IntPtr pConstMorph = morphControl.ConstPointer();
      IntPtr pAttributes = IntPtr.Zero;
      if (attributes != null)
        pAttributes = attributes.ConstPointer();
      IntPtr p_history = (history == null) ? IntPtr.Zero : history.Handle;
      return UnsafeNativeMethods.CRhinoDoc_AddMorphControl(m_doc.RuntimeSerialNumber, pConstMorph, pAttributes, p_history, reference);
    }

    //public Guid AddMorphControl(MorphControl morphControl, IEnumerable<RhinoObject> captives)

    /// <summary>
    /// Creates a PictureFrame object from a plane and a path to an image file,
    /// Note, a PictureFrame object is just a Plane surface or mesh that has a
    /// material with a texture assigned to it that displays in all display
    /// modes.
    /// </summary>
    /// <param name="plane">
    /// Plane in which the PictureFrame will be created.  Bottom left corner of
    /// picture will be at plane's origin, width will be in the plane's x axis
    /// direction, height will be in the plane's y axis direction.
    /// </param>
    /// <param name="texturePath">path to an image file</param>
    /// <param name="asMesh">
    /// If true, the function will make a MeshObject rather than a surface
    /// </param>
    /// <param name="width">
    /// Width of the resulting PictureFrame. If 0.0, the width of the picture frame
    /// is the width of the image if height is also 0.0 or calculated from the
    /// height and aspect ratio of the image if height is not 0.0.
    /// </param>
    /// <param name="height">
    /// Height of the resulting PictureFrame. If 0.0, the height of the picture frame
    /// is the height of the image if width is also 0.0 or calculated from the width
    /// and aspect ratio of the image if width is not 0.0.
    /// </param>
    /// <param name="selfIllumination">
    /// If true, the image mapped to the picture frame plane always displays at full
    /// intensity and is not affected by light or shadow.
    /// </param>
    /// <param name="embedBitmap">
    /// If true, the function adds the image to the bitmap table of the document
    /// to which the PictureFrame will be added
    /// </param>
    /// <returns>A unique identifier for the object</returns>
    /// <since>5.10</since>
    public Guid AddPictureFrame(Plane plane, string texturePath, bool asMesh, double width, double height, bool selfIllumination, bool embedBitmap)
    {
      return UnsafeNativeMethods.RHC_RhinoCreatePictureFrame(m_doc.RuntimeSerialNumber, ref plane, texturePath, asMesh, false, width, height, selfIllumination, embedBitmap);
    }

    #endregion

    #region Object deletion
    /// <summary>
    /// Deletes objref.Object(). The deletion can be undone by calling UndeleteObject(). 
    /// </summary>
    /// <param name="objref">objref.Object() will be deleted.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <param name="ignoreModes">
    /// If true, locked and hidden objects are deleted.  If false objects that
    /// are locked, hidden, or on locked or hidden layers are not deleted.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool Delete(ObjRef objref, bool quiet, bool ignoreModes)
    {
      if (null == objref)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_DeleteObject(m_doc.RuntimeSerialNumber, pObjRef, quiet, ignoreModes);
    }
    /// <summary>
    /// Deletes objref.Object(). The deletion can be undone by calling UndeleteObject().
    /// </summary>
    /// <param name="objref">objref.Object() will be deleted.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Delete(ObjRef objref, bool quiet)
    {
      return Delete(objref, quiet, false);
    }
    /// <summary>
    /// Deletes object from document. The deletion can be undone by calling UndeleteObject().
    /// </summary>
    /// <param name="obj">The object to delete.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Delete(RhinoObject obj, bool quiet)
    {
      return Delete(obj, quiet, false);
    }
    /// <summary>
    /// Deletes object from document. The deletion can be undone by calling UndeleteObject().
    /// </summary>
    /// <param name="obj">The object to delete.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <param name="ignoreModes">
    /// If true, locked and hidden objects are deleted.  If false objects that
    /// are locked, hidden, or on locked or hidden layers are not deleted.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool Delete(RhinoObject obj, bool quiet, bool ignoreModes)
    {
      if (null == obj)
        return false;
      var objref = new ObjRef(obj);
      var rc = Delete(objref, quiet, ignoreModes);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// Deletes object from document. The deletion can be undone by calling UndeleteObject().
    /// </summary>
    /// <param name="objectId">Id of the object to delete.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Delete(Guid objectId, bool quiet)
    {
      var objref = new ObjRef(Document, objectId);
      bool rc = Delete(objref, quiet);
      objref.Dispose();
      return rc;
    }
    /// <summary>
    /// Deletes a collection of objects from the document.
    /// </summary>
    /// <param name="objectIds">Ids of all objects to delete.</param>
    /// <param name="quiet">If false, a message box will appear when an object cannot be deleted.</param>
    /// <returns>The number of successfully deleted objects.</returns>
    /// <since>5.0</since>
    public int Delete(IEnumerable<Guid> objectIds, bool quiet)
    {
      if (objectIds == null) { throw new ArgumentNullException("objectIds"); }

      int count = 0;
      foreach (Guid id in objectIds)
      {
        if (Delete(id, quiet)) { count++; }
      }
      return count;
    }

    /// <summary>
    /// Deletes an object, taking into account modes and not showing error message boxes.
    /// </summary>
    /// <param name="item">The object to delete.</param>
    /// <returns>True on success.</returns>
    /// <since>6.0</since>
    public override bool Delete(RhinoObject item)
    {
      return Delete(item, true);
    }


    /// <summary>
    /// Deletes a grip object.
    /// </summary>
    /// <param name="gripId">The id of the grip object to delete.</param>
    /// <returns>True on success.</returns>
    /// <since>6.5</since>
    public bool DeleteGrip(Guid gripId)
    {
      if (null == gripId) { throw new ArgumentNullException(nameof(gripId)); }
      return DeleteGrip(new ObjRef(Document, gripId));
    }

    /// <summary>
    /// Deletes a grip object.
    /// </summary>
    /// <param name="gripRef">A reference to the grip object to delete.</param>
    /// <returns>True on success.</returns>
    /// <since>6.5</since>
    public bool DeleteGrip(ObjRef gripRef)
    {
      if (null == gripRef) { throw new ArgumentNullException(nameof(gripRef)); }

      RhinoObject rh_object = gripRef.Object();
      if (null == rh_object) { throw new ArgumentNullException(nameof(rh_object)); }

      GripObject grip_object = rh_object as GripObject;
      if (null == rh_object) { throw new NotSupportedException("Object is not a GripObject."); }

      return DeleteGrip(grip_object);
    }

    /// <summary>
    /// Deletes a grip object.
    /// </summary>
    /// <param name="grip">The grip object to delete.</param>
    /// <returns>True on success.</returns>
    /// <since>6.5</since>
    public bool DeleteGrip(GripObject grip)
    {
      if (null == grip) { throw new ArgumentNullException(nameof(grip)); }

      ObjRef objref_owner = new ObjRef(Document, grip.OwnerId);
      List<int> list = new List<int> { grip.Index };

      return DeleteGrips(objref_owner.Object(), list.ToArray()) > 0 ? true : false;
    }


    /// <summary>
    /// Internal class to track grip object owners and their indices
    /// </summary>
    internal class GripMap
    {
      public Dictionary<Guid, List<int>> Dictionary = new Dictionary<Guid, List<int>>();

      public void Add(Guid key, int value)
      {
        if (Dictionary.ContainsKey(key))
        {
          List<int> list = Dictionary[key];
          if (!list.Contains(value))
            list.Add(value);
        }
        else
        {
          List<int> list = new List<int> { value };
          Dictionary.Add(key, list);
        }
      }
    }


    /// <summary>
    /// Deletes one or more grip objects.
    /// </summary>
    /// <param name="gripIds">The ids of the grip objects to delete.</param>
    /// <returns>The number of successfully deleted grip objects.</returns>
    /// <since>6.5</since>
    public int DeleteGrips(IEnumerable<Guid> gripIds)
    {
      if (null == gripIds) { throw new ArgumentNullException(nameof(gripIds)); }

      List<ObjRef> list = new List<ObjRef>();
      foreach (Guid id in gripIds)
        list.Add(new ObjRef(Document, id));

      return DeleteGrips(list.ToArray());
    }

    /// <summary>
    /// Deletes one or more grip objects.
    /// </summary>
    /// <param name="gripRefs">References to the grip objects to delete.</param>
    /// <returns>The number of successfully deleted grip objects.</returns>
    /// <since>6.5</since>
    public int DeleteGrips(IEnumerable<ObjRef> gripRefs)
    {
      if (null == gripRefs) { throw new ArgumentNullException(nameof(gripRefs)); }

      List<GripObject> list = new List<GripObject>();

      foreach (ObjRef objref in gripRefs)
      {
        RhinoObject rh_object = objref.Object();
        if (null == rh_object) { throw new ArgumentNullException(nameof(rh_object)); }

        GripObject grip_object = rh_object as GripObject;
        if (null == rh_object) { throw new NotSupportedException("Object is not a GripObject."); }

        list.Add(grip_object);
      }

      return DeleteGrips(list.ToArray());
    }

    /// <summary>
    /// Deletes one or more grip objects.
    /// </summary>
    /// <param name="grips">The grip objects to delete.</param>
    /// <returns>The number of successfully deleted grip objects.</returns>
    /// <since>6.5</since>
    public int DeleteGrips(IEnumerable<GripObject> grips)
    {
      if (null == grips) { throw new ArgumentNullException(nameof(grips)); }

      GripMap map = new GripMap();
      foreach (GripObject grip in grips)
        map.Add(grip.OwnerId, grip.Index);

      int rc = 0;
      foreach (var item in map.Dictionary)
      {
        var objref = new ObjRef(Document, item.Key);
        rc += DeleteGrips(objref.Object(), item.Value.ToArray());
      }

      return rc;
    }

    /// <summary>
    /// Deletes one or more grip objects.
    /// </summary>
    /// <param name="owner">The owner of the grip objects.</param>
    /// <param name="gripIndices">The indices of the grip objects to delete.</param>
    /// <returns>The number of successfully deleted grip objects.</returns>
    /// <since>6.5</since>
    public int DeleteGrips(RhinoObject owner, IEnumerable<int> gripIndices)
    {
      if (null == owner) { throw new ArgumentNullException(nameof(owner)); }
      if (null == gripIndices) { throw new ArgumentNullException(nameof(gripIndices)); }

      var list_grip_indices = new List<int>(gripIndices);
      int[] array_grip_indices = list_grip_indices.ToArray();

      var objref = new ObjRef(owner);
      IntPtr ptr_const_owner = objref.ConstPointer();

      return UnsafeNativeMethods.CRhinoDoc_DeleteGripObjects(m_doc.RuntimeSerialNumber, ptr_const_owner, array_grip_indices.Length, array_grip_indices);
    }


    /// <summary>
    /// Removes object from document and deletes the pointer. Typically you will
    /// want to call Delete instead in order to keep the object on the undo list.
    /// </summary>
    /// <param name="runtimeSerialNumber">A runtime serial number of the object that will be deleted.</param>
    /// <returns>true if the object was purged; otherwise false.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Purge(uint runtimeSerialNumber)
    {
      return UnsafeNativeMethods.CRhinoDoc_PurgeObject(m_doc.RuntimeSerialNumber, runtimeSerialNumber);
    }

    /// <summary>
    /// Removes object from document and deletes the pointer. Typically you will
    /// want to call Delete instead in order to keep the object on the undo list.
    /// </summary>
    /// <param name="rhinoObject">A Rhino object that will be deleted.</param>
    /// <returns>true if the object was purged; otherwise false.</returns>
    /// <since>5.0</since>
    public bool Purge(RhinoObject rhinoObject)
    {
      return Purge(rhinoObject.RuntimeSerialNumber);
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Undelete(uint runtimeSerialNumber)
    {
      return UnsafeNativeMethods.CRhinoDoc_UndeleteObject(m_doc.RuntimeSerialNumber, runtimeSerialNumber);
    }

    /// <since>5.0</since>
    public bool Undelete(RhinoObject rhinoObject)
    {
      return Undelete(rhinoObject.RuntimeSerialNumber);
    }
    #endregion

    #region Object selection
    /// <summary>
    /// Select a single object.
    /// </summary>
    /// <param name="objref">Object represented by this ObjRef is selected.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(ObjRef objref)
    {
      return Select(objref, true);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objref">Object represented by this ObjRef is selected.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(ObjRef objref, bool select)
    {
      return Select(objref, select, true);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objref">Object represented by this ObjRef is selected.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(ObjRef objref, bool select, bool syncHighlight)
    {
      return Select(objref, select, syncHighlight, true);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objref">Object represented by this ObjRef is selected.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <param name="persistentSelect">
    /// Objects that are persistently selected stay selected when a command terminates.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(ObjRef objref, bool select, bool syncHighlight, bool persistentSelect)
    {
      return Select(objref, select, syncHighlight, persistentSelect, false, false, false);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objref">Object represented by this ObjRef is selected.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <param name="persistentSelect">
    /// Objects that are persistently selected stay selected when a command terminates.
    /// </param>
    /// <param name="ignoreGripsState">
    /// If true, then objects with grips on can be selected.
    /// If false, then the value returned by the object's IsSelectableWithGripsOn() function
    /// decides if the object can be selected when it has grips turned on.
    /// </param>
    /// <param name="ignoreLayerLocking">
    /// If true, then objects on locked layers can be selected. 
    /// </param>
    /// <param name="ignoreLayerVisibility">
    /// If true, then objects on hidden layers can be selectable.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(ObjRef objref, bool select, bool syncHighlight, bool persistentSelect, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility)
    {
      if (objref == null) { throw new ArgumentNullException("objref"); }
      var obj = objref.Object();
      if (obj == null)
        return false;
      return obj.Select(select, syncHighlight, persistentSelect, ignoreGripsState, ignoreLayerLocking, ignoreLayerVisibility) != 0;
    }

    /// <summary>
    /// Selects a collection of objects.
    /// </summary>
    /// <param name="objRefs">References to objects to select.</param>
    /// <returns>Number of objects successfully selected.</returns>
    /// <since>5.0</since>
    public int Select(IEnumerable<ObjRef> objRefs)
    {
      return Select(objRefs, true);
    }
    /// <summary>
    /// Selects or deselects a collection of objects.
    /// </summary>
    /// <param name="objRefs">References to objects to select or deselect.</param>
    /// <param name="select">
    /// If true, objects will be selected. 
    /// If false, objects will be deselected.
    /// </param>
    /// <returns>Number of objects successfully selected or deselected.</returns>
    /// <since>5.0</since>
    public int Select(IEnumerable<ObjRef> objRefs, bool select)
    {
      if (objRefs == null) { throw new ArgumentNullException("objRefs"); }
      int count = 0;
      foreach (var objref in objRefs)
      {
        if (Select(objref, select)) { count++; }
      }
      return count;
    }

    /// <summary>
    /// Select a single object.
    /// </summary>
    /// <param name="objectId">Id of object to select.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Select(Guid objectId)
    {
      ObjRef objref = new ObjRef(Document, objectId);
      return Select(objref);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objectId">Id of object to select.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(Guid objectId, bool select)
    {
      ObjRef objref = new ObjRef(Document, objectId);
      return Select(objref, select);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objectId">Id of object to select.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(Guid objectId, bool select, bool syncHighlight)
    {
      ObjRef objref = new ObjRef(Document, objectId);
      return Select(objref, select, syncHighlight);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objectId">Id of object to select.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <param name="persistentSelect">
    /// Objects that are persistently selected stay selected when a command terminates.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(Guid objectId, bool select, bool syncHighlight, bool persistentSelect)
    {
      ObjRef objref = new ObjRef(Document, objectId);
      return Select(objref, select, syncHighlight, persistentSelect);
    }
    /// <summary>
    /// Select or deselects a single object.
    /// </summary>
    /// <param name="objectId">Id of object to select.</param>
    /// <param name="select">If true, the object will be selected, if false, it will be deselected.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <param name="persistentSelect">
    /// Objects that are persistently selected stay selected when a command terminates.
    /// </param>
    /// <param name="ignoreGripsState">
    /// If true, then objects with grips on can be selected.
    /// If false, then the value returned by the object's IsSelectableWithGripsOn() function
    /// decides if the object can be selected when it has grips turned on.
    /// </param>
    /// <param name="ignoreLayerLocking">
    /// If true, then objects on locked layers can be selected. 
    /// </param>
    /// <param name="ignoreLayerVisibility">
    /// If true, then objects on hidden layers can be selectable.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Select(Guid objectId, bool select, bool syncHighlight, bool persistentSelect, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility)
    {
      ObjRef objref = new ObjRef(Document, objectId);
      return Select(objref, select, syncHighlight, persistentSelect, ignoreGripsState, ignoreLayerLocking, ignoreLayerVisibility);
    }

    /// <summary>
    /// Selects a collection of objects.
    /// </summary>
    /// <param name="objectIds">Ids of objects to select.</param>
    /// <returns>Number of objects successfully selected.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curvesurfaceintersect.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvesurfaceintersect.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvesurfaceintersect.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int Select(IEnumerable<Guid> objectIds)
    {
      return Select(objectIds, true);
    }
    /// <summary>
    /// Selects or deselects a collection of objects.
    /// </summary>
    /// <param name="objectIds">Ids of objects to select or deselect.</param>
    /// <param name="select">
    /// If true, objects will be selected. 
    /// If false, objects will be deselected.
    /// </param>
    /// <returns>Number of objects successfully selected or deselected.</returns>
    /// <since>5.0</since>
    public int Select(IEnumerable<Guid> objectIds, bool select)
    {
      if (objectIds == null) { throw new ArgumentNullException("objectIds"); }
      int count = 0;
      foreach (Guid objectId in objectIds)
      {
        if (Select(objectId, select)) { count++; }
      }
      return count;
    }

    /// <summary>Unselect objects.</summary>
    /// <param name="ignorePersistentSelections">
    /// if true, then objects that are persistently selected will not be unselected.
    /// </param>
    /// <returns>Number of object that were unselected.</returns>
    /// <since>5.0</since>
    public int UnselectAll(bool ignorePersistentSelections)
    {
      return UnsafeNativeMethods.CRhinoDoc_UnselectAll(m_doc.RuntimeSerialNumber, ignorePersistentSelections);
    }
    /// <summary>Unselect objects.</summary>
    /// <returns>Number of object that were unselected.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_crvdeviation.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_crvdeviation.cs' lang='cs'/>
    /// <code source='examples\py\ex_crvdeviation.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int UnselectAll()
    {
      return UnselectAll(false);
    }
    #endregion

    #region Object replacement
    /// <summary>
    /// Modifies an object's attributes.  Cannot be used to change object id.
    /// </summary>
    /// <param name="objref">reference to object to modify.</param>
    /// <param name="newAttributes">new attributes.</param>
    /// <param name="quiet">if true, then warning message boxes are disabled.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool ModifyAttributes(ObjRef objref, ObjectAttributes newAttributes, bool quiet)
    {
      if (null == objref || null == newAttributes)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      IntPtr pAttrs = newAttributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_ModifyObjectAttributes(m_doc.RuntimeSerialNumber, pObjRef, pAttrs, quiet);
    }
    /// <summary>
    /// Modifies an object's attributes.  Cannot be used to change object id.
    /// </summary>
    /// <param name="obj">object to modify.</param>
    /// <param name="newAttributes">new attributes.</param>
    /// <param name="quiet">if true, then warning message boxes are disabled.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool ModifyAttributes(RhinoObject obj, ObjectAttributes newAttributes, bool quiet)
    {
      if (null == obj || null == newAttributes)
        return false;
      return ModifyAttributes(obj.Id, newAttributes, quiet);
    }
    /// <summary>
    /// Modifies an object's attributes.  Cannot be used to change object id.
    /// </summary>
    /// <param name="objectId">Id of object to modify.</param>
    /// <param name="newAttributes">new attributes.</param>
    /// <param name="quiet">if true, then warning message boxes are disabled.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool ModifyAttributes(Guid objectId, ObjectAttributes newAttributes, bool quiet)
    {
      if (Guid.Empty == objectId || null == newAttributes)
        return false;
      var objref = new ObjRef(Document, objectId);
      IntPtr pObjRef = objref.ConstPointer();
      IntPtr pAttrs = newAttributes.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ModifyObjectAttributes(m_doc.RuntimeSerialNumber, pObjRef, pAttrs, quiet);
      objref.Dispose();
      return rc;
    }
    /// <summary>
    /// Modifies an object's render material assignment, this will set the
    /// objects material source to ObjectMaterialSource.MaterialFromObject.
    /// </summary>
    /// <param name="obj">Object to modify.</param>
    /// <param name="material">
    /// Material to assign to this object.
    /// </param>
    /// <returns>
    /// Returns true on success otherwise returns false.
    /// </returns>
    /// <since>5.7</since>
    public bool ModifyRenderMaterial(RhinoObject obj, RenderMaterial material)
    {
      if (null == obj) return false;
      var material_id = (material == null ? Guid.Empty : material.Id);
      var pointer = obj.ConstPointer();
      var success = UnsafeNativeMethods.Rdk_RenderContent_SetObjectMaterialInstanceid(pointer, material_id);
      return (success != 0);
    }
    /// <summary>
    /// Modifies an object's render material assignment, this will set the
    /// objects material source to ObjectMaterialSource.MaterialFromObject.
    /// </summary>
    /// <param name="objRef">Object to modify.</param>
    /// <param name="material">
    /// Material to assign to this object.
    /// </param>
    /// <returns>
    /// Returns true on success otherwise returns false.
    /// </returns>
    /// <since>5.7</since>
    public bool ModifyRenderMaterial(ObjRef objRef, RenderMaterial material)
    {
      if (null == objRef) return false;
      var obj = objRef.Object();
      return ModifyRenderMaterial(obj, material);
    }
    /// <summary>
    /// Modifies an object's render material assignment, this will set the
    /// objects material source to ObjectMaterialSource.MaterialFromObject.
    /// </summary>
    /// <param name="objectId">Id of object to modify.</param>
    /// <param name="material">
    /// Material to assign to this object.
    /// </param>
    /// <returns>
    /// Returns true on success otherwise returns false.
    /// </returns>
    /// <since>5.7</since>
    public bool ModifyRenderMaterial(Guid objectId, RenderMaterial material)
    {
      var objref = new ObjRef(Document, objectId);
      var obj = objref.Object();
      return ModifyRenderMaterial(obj, material);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objRef"></param>
    /// <param name="channel"></param>
    /// <param name="mapping"></param>
    /// <returns></returns>
    /// <since>5.7</since>
    public bool ModifyTextureMapping(ObjRef objRef, int channel, TextureMapping mapping)
    {
      var obj = (objRef == null ? null : objRef.Object());
      return ModifyTextureMapping(obj, channel, mapping);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objId"></param>
    /// <param name="channel"></param>
    /// <param name="mapping"></param>
    /// <returns></returns>
    /// <since>5.7</since>
    public bool ModifyTextureMapping(Guid objId, int channel, TextureMapping mapping)
    {
      var obj_ref = new ObjRef(Document, objId);
      return ModifyTextureMapping(obj_ref, channel, mapping);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="channel"></param>
    /// <param name="mapping"></param>
    /// <returns></returns>
    /// <since>5.7</since>
    public bool ModifyTextureMapping(RhinoObject obj, int channel, TextureMapping mapping)
    {
      if (null == obj) return false;
      var pointer = obj.ConstPointer();
      var mapping_pointer = (null == mapping ? IntPtr.Zero : mapping.ConstPointer());
      var success = UnsafeNativeMethods.ON_TextureMapping_SetObjectMapping(pointer, channel, mapping_pointer);
      return (success != 0);
    }
    /// <summary>
    /// Replaces one object with another. Conceptually, this function is the same as calling
    /// Setting new_object attributes = old_object attributes
    /// DeleteObject(old_object);
    /// AddObject(old_object);
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="newObject">new replacement object - must not be in document.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, RhinoObject newObject)
    {
      if (null == objref || null == newObject || newObject.Document != null)
        return false;

      // Once the deprecated functions are removed, we should switch to checking for custom subclasses
      bool is_proper_subclass = newObject is Custom.CustomBrepObject ||
                                newObject is Custom.CustomCurveObject ||
                                newObject is Custom.CustomMeshObject ||
                                newObject is Custom.CustomPointObject;
      if (!is_proper_subclass)
        throw new NotImplementedException();

      Type t = newObject.GetType();
      if (t.GetConstructor(Type.EmptyTypes) == null)
        throw new NotImplementedException("class must have a public parameterless constructor");


      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_rhino_object = newObject.NonConstPointer();

      // 3 June 2014 (RH-27446)
      // Add the object to our tracking list before calling the native replace object
      // code. The native replace object code will attempt to wire up a new instance
      // of the custom object if it isn't already in our tracking list
      uint serial_number = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr_rhino_object);
      if (serial_number > 0)
        newObject.m_rhinoobject_serial_number = serial_number;
      newObject.m_pRhinoObject = IntPtr.Zero;
      GC.SuppressFinalize(newObject);
      AddCustomObjectForTracking(serial_number, newObject, ptr_rhino_object);

      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplaceObject1(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_rhino_object);
      if (!rc)
      {
        // let the object get garbage collected
        m_custom_objects.Remove(serial_number);
        newObject.m_pRhinoObject = ptr_rhino_object;
        GC.ReRegisterForFinalize(newObject);
      }
      return rc;
    }

    /// <summary>
    /// Replaces the geometry in one object.
    /// </summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="geometry"></param>
    /// <param name="ignoreModes"></param>
    /// <returns>true if successful.</returns>
    /// <since>7.9</since>
    public bool Replace(Guid objectId, GeometryBase geometry, bool ignoreModes)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, geometry, ignoreModes);
      }
    }

    /// <summary>
    /// Replaces the geometry in one object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="geometry"></param>
    /// <param name="ignoreModes"></param>
    /// <returns>true if successful.</returns>
    /// <since>7.9</since>
    public bool Replace(ObjRef objref, GeometryBase geometry, bool ignoreModes)
    {
      if (null == objref || null == geometry)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_geometry = geometry.ConstPointer();
      IntPtr ptr_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObjectEx(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_geometry, ignoreModes);
      return IntPtr.Zero != ptr_object;
    }

    /// <summary>Replaces one object with new point object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="point">new point to be added.  The point is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Point3d point)
    {
      // 7 Jan. 2009 S. Baer
      // I looked at every call to ReplaceObject in core Rhino and we only use the return
      // object about 4 times out of ~353 calls. Those 4 calls just use the return object
      // to look at it's attributes. If we need this extra information, we can add other Replace
      // functions in the future
      if (null == objref)
        return false;
      IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject2(m_doc.RuntimeSerialNumber, objref.ConstPointer(), point);
      return IntPtr.Zero != ptr_rhino_object;
    }

    /// <summary>Replaces one object with new point object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="point">new point to be added.  The point is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Point3d point)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, point);
      }
    }

    /// <summary>Replaces one object with new point object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="point">new point to be added.  The point is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(ObjRef objref, Point point)
    {
      if (null == objref || null == point)
        return false;
      IntPtr ptr_const_point = point.ConstPointer();
      IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject9(m_doc.RuntimeSerialNumber, objref.ConstPointer(), ptr_const_point);
      return IntPtr.Zero != ptr_rhino_object;
    }

    /// <summary>Replaces one object with new point object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="point">new point to be added.  The point is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(Guid objectId, Point point)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, point);
      }
    }

    /// <summary>Replaces one object with new text object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="text">new text to be added.  The text is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, TextEntity text)
    {
      if(null == objref || null == text)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_text = text.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplaceONText(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_text);
      return rc;
    }

    /// <summary>Replaces one object with new text object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="text">new text to be added.  The text is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, TextEntity text)
    {
      using(var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, text);
      }
    }

    /// <summary>Replaces one object with new text object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="leader">new leader to be added.  The leader is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(ObjRef objref, Leader leader)
    {
      if(null == objref || null == leader)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_leader = leader.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplaceONLeader(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_leader);
      return rc;
    }

    /// <summary>Replaces one object with new text object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="leader">new leader to be added.  The leader is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(Guid objectId, Leader leader)
    {
      using(var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, leader);
      }
    }

    /// <summary>Replaces one object with new text dot object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="dot">new text dot to be added.  The text dot is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, TextDot dot)
    {
      if (null == objref || null == dot)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_dot = dot.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplaceTextDot(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_dot);
      return rc;
    }

    /// <summary>Replaces one object with new text dot object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="dot">new text dot to be added.  The text dot is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, TextDot dot)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, dot);
      }
    }

    /// <summary>Replaces one object with new hatch object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="hatch">new hatch to be added. The hatch is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.1</since>
    public bool Replace(ObjRef objref, Hatch hatch)
    {
      if (null == objref || null == hatch)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_hatch = hatch.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplaceHatch(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_hatch);
      return rc;
    }

    /// <summary>Replaces one object with new hatch object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="hatch">new hatch to be added. The hatch is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.1</since>
    public bool Replace(Guid objectId, Hatch hatch)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, hatch);
      }
    }

    /// <summary>Replaces one object with new line curve object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="line">new line to be added.  The line is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Line line)
    {
      return Replace(objref, new LineCurve(line));
    }

    /// <summary>Replaces one object with new line curve object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="line">new line to be added.  The line is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Line line)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, line);
      }
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="circle">new circle to be added.  The circle is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Circle circle)
    {
      return Replace(objref, new ArcCurve(circle));
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="circle">new circle to be added.  The circle is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Circle circle)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, circle);
      }
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="arc">new arc to be added.  The arc is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Arc arc)
    {
      return Replace(objref, new ArcCurve(arc));
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="arc">new arc to be added.  The arc is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Arc arc)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, arc);
      }
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objref">
    /// Reference to old object to be replaced. The object objref.Object() will be deleted.
    /// </param>
    /// <param name="polyline">new polyline to be added.  The polyline is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Polyline polyline)
    {
      return Replace(objref, new PolylineCurve(polyline));
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="polyline">new polyline to be added.  The polyline is copied.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Polyline polyline)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, polyline);
      }
    }

    /// <summary>
    /// Replaces one object with new curve object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="curve">
    /// New curve to be added. A duplicate of the curve is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_insertknot.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_insertknot.cs' lang='cs'/>
    /// <code source='examples\py\ex_insertknot.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Curve curve)
    {
      if (null == objref || null == curve)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_curve = curve.ConstPointer();
      IntPtr ptr_curve_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject3(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_curve);
      return (IntPtr.Zero != ptr_curve_object);
    }

    /// <summary>Replaces one object with new curve object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="curve">
    /// New curve to be added. A duplicate of the curve is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Curve curve)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, curve);
      }
    }

    /// <summary>
    /// Replaces one object with new surface object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="surface">
    /// new surface to be added
    /// A duplicate of the surface is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Surface surface)
    {
      if (null == objref || null == surface)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_surface = surface.ConstPointer();
      IntPtr ptr_surface_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject4(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_surface);
      return (IntPtr.Zero != ptr_surface_object);
    }

    /// <summary>Replaces one object with new surface object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="surface">
    /// new surface to be added
    /// A duplicate of the surface is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Surface surface)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, surface);
      }
    }

    /// <summary>
    /// Replaces one object with new brep object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="brep">
    /// new brep to be added
    /// A duplicate of the brep is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Brep brep)
    {
      return Replace(objref, brep, false);
    }


    /// <since>6.1</since>
    public bool Replace(ObjRef objref, Brep brep, bool splitKinkySurfaces)
    {
      if (null == objref || null == brep)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr pre_const_brep = brep.ConstPointer();
      IntPtr pre_brep_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject5(m_doc.RuntimeSerialNumber, ptr_const_objref, pre_const_brep, splitKinkySurfaces);
      return (IntPtr.Zero != pre_brep_object);
    }

    /// <summary>Replaces one object with new brep object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="brep">
    /// new surface to be added
    /// A duplicate of the brep is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Brep brep)
    {
      return Replace(objectId, brep, false);
    }

    /// <since>6.1</since>
    public bool Replace(Guid objectId, Brep brep, bool splitKinkySurfaces)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, brep, splitKinkySurfaces);
      }
    }

    /// <summary>Replaces one object with new extrusion object.</summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="extrusion">New extrusion to be added. A duplicate of the extrusion is added to the Rhino model.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(ObjRef objref, Extrusion extrusion)
    {
      if (null == objref || null == extrusion)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr pre_const_extrusion = extrusion.ConstPointer();
      IntPtr pre_extrusion_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject8(m_doc.RuntimeSerialNumber, ptr_const_objref, pre_const_extrusion);
      return (IntPtr.Zero != pre_extrusion_object);
    }

    /// <summary>Replaces one object with new extrusion object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="extrusion">New extrusion to be added. A duplicate of the extrusion is added to the Rhino model.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.0</since>
    public bool Replace(Guid objectId, Extrusion extrusion)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, extrusion);
      }
    }

    /// <summary>
    /// Replaces one object with new mesh object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="mesh">
    /// new mesh to be added
    /// A duplicate of the mesh is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, Mesh mesh)
    {
      if (null == objref || null == mesh)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_mesh = mesh.ConstPointer();
      IntPtr ptr_mesh_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject6(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_mesh);
      return (IntPtr.Zero != ptr_mesh_object);
    }

    /// <summary>
    /// Replaces one object with a new SubD object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="subD">
    /// new SubD to be added
    /// A duplicate of the SubD is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>7.0</since>
    public bool Replace(ObjRef objref, SubD subD)
    {
      if (null == objref || null == subD)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_subdref = subD.SubDRefPointer();
      IntPtr ptr_subd_object = UnsafeNativeMethods.CRhinoDoc_ReplaceObject7(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_subdref);
      if (ptr_subd_object != IntPtr.Zero)
        subD.ChangeToConstObject(null);
      return (IntPtr.Zero != ptr_subd_object);
    }

    /// <summary>Replaces one object with new mesh object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="mesh">
    /// new mesh to be added
    /// A duplicate of the mesh is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, Mesh mesh)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, mesh);
      }
    }

    /// <summary>Replaces one object with new subd object.</summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="subD">
    /// new mesh to be added
    /// A duplicate of the mesh is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>7.0</since>
    public bool Replace(Guid objectId, SubD subD)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, subD);
      }
    }

    /// <summary>
    /// Replaces one object with new point cloud object.
    /// </summary>
    /// <param name="objref">reference to old object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="pointcloud">
    /// new point cloud to be added
    /// A duplicate of the point cloud is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(ObjRef objref, PointCloud pointcloud)
    {
      if (null == objref || null == pointcloud)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_const_pointcloud = pointcloud.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_ReplacePointCloud(m_doc.RuntimeSerialNumber, ptr_const_objref, ptr_const_pointcloud);
      return rc;
    }

    /// <summary>
    /// Replaces one object with new point cloud object.
    /// </summary>
    /// <param name="objectId">Id of object to be replaced.</param>
    /// <param name="pointcloud">
    /// new point cloud to be added
    /// A duplicate of the point cloud is added to the Rhino model.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Replace(Guid objectId, PointCloud pointcloud)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return Replace(objref, pointcloud);
      }
    }

    /// <summary>
    /// Replaces the underlying instance definition of an instance object.
    /// </summary>
    /// <param name="objectId">Id of the instance object to be replaced.</param>
    /// <param name="instanceDefinitionIndex">The index of the new instance definition to use.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.8</since>
    public bool ReplaceInstanceObject(Guid objectId, int instanceDefinitionIndex)
    {
      using (var objref = new ObjRef(Document, objectId))
      {
        return ReplaceInstanceObject(objref, instanceDefinitionIndex);
      }

    }

    /// <summary>
    /// Replaces the underlying instance definition of an instance object.
    /// </summary>
    /// <param name="objref">Reference to the instance object to be replaced. The objref.Object() will be deleted.</param>
    /// <param name="instanceDefinitionIndex">The index of the new instance definition to use.</param>
    /// <returns>true if successful.</returns>
    /// <since>6.8</since>
    public bool ReplaceInstanceObject(ObjRef objref, int instanceDefinitionIndex)
    {
      if (null == objref)
        return false;
      IntPtr ptr_const_objref = objref.ConstPointer();
      IntPtr ptr_iref_object = UnsafeNativeMethods.CRhinoDoc_ReplaceInstanceObject(m_doc.RuntimeSerialNumber, ptr_const_objref, instanceDefinitionIndex);
      return (IntPtr.Zero != ptr_iref_object);
    }

    #endregion

    #region Find geometry
    /// <summary>
    /// Gets the most recently added object that is still in the Document.
    /// </summary>
    /// <returns>The most recent (non-deleted) object in the document, or null if no such object exists.</returns>
    /// <since>5.0</since>
    public RhinoObject MostRecentObject()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoDoc_MostRecentObject(m_doc.RuntimeSerialNumber);
      return RhinoObject.CreateRhinoObjectHelper(ptr);
    }
    /// <summary>
    /// Gets all the objects that have been added to the document since a given runtime serial number. 
    /// </summary>
    /// <param name="runtimeSerialNumber">Runtime serial number of the last object not to include in the list.</param>
    /// <returns>An array of objects or null if no objects were added since the given runtime serial number.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public RhinoObject[] AllObjectsSince(uint runtimeSerialNumber)
    {
      using (var rhobjs = new Runtime.InternalRhinoObjectArray())
      {
        IntPtr pArray = rhobjs.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_AllObjectsSince(m_doc.RuntimeSerialNumber, runtimeSerialNumber, pArray);
        return rhobjs.ToArray();
      }
    }

    // 27 Jan 2010 S. Baer
    // I think it is useful to have "quick" finders, but I'm not exactly sure if this is the right
    // approach. I couldn't find any code in Grasshopper that calls this function and want to take
    // some time to figure out how the interface should be structured. If we want a quick point finder,
    // the internal implementation would look different than what is written below.
    //
    ///// <summary>
    ///// Searches the document for the point which matches the given ID.
    ///// </summary>
    ///// <param name="objectID">Object ID to search for.</param>
    ///// <returns>The sought after Point coordinates, or Point3d.Unset if the point could not be found.</returns>
    //public Point3d FindPoint(Guid objectID)
    //{
    //  DocObjects.RhinoObject obj = this.Find(objectID);
    //  if (obj == null) { return Point3d.Unset; }
    //  if (obj.ObjectType != Rhino.DocObjects.ObjectType.Point) { return Point3d.Unset; }
    //  //return (DocObjects.PointObject)(obj).Point; //Doesn't work yet.
    //  return Point3d.Unset;
    //}
    //// TODO: write these functions for all other Object types too.
    #endregion

    #region Object state changes (lock, hide, etc.)
    const int idxHideObject = 0;
    const int idxShowObject = 1;
    const int idxLockObject = 2;
    const int idxUnlockObject = 3;

    /// <summary>
    /// If objref.Object().IsNormal() is true, then the object will be hidden.
    /// </summary>
    /// <param name="objref">reference to object to hide.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be hidden even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully hidden.</returns>
    /// <since>5.0</since>
    public bool Hide(ObjRef objref, bool ignoreLayerMode)
    {
      if (null == objref)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxHideObject);
    }
    /// <summary>
    /// If obj.IsNormal() is true, then the object will be hidden.
    /// </summary>
    /// <param name="obj">object to hide.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be hidden even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully hidden.</returns>
    /// <since>5.0</since>
    public bool Hide(RhinoObject obj, bool ignoreLayerMode)
    {
      if (null == obj)
        return false;
      return Hide(obj.Id, ignoreLayerMode);
    }
    /// <summary>
    /// If Object().IsNormal() is true, then the object will be hidden.
    /// </summary>
    /// <param name="objectId">Id of object to hide.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be hidden even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully hidden.</returns>
    /// <since>5.0</since>
    public bool Hide(Guid objectId, bool ignoreLayerMode)
    {
      if (Guid.Empty == objectId)
        return false;
      var objref = new ObjRef(Document, objectId);
      IntPtr pObjRef = objref.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxHideObject);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// If objref.Object().IsHidden() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="objref">reference to normal object to show.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be shown even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully shown.</returns>
    /// <since>5.0</since>
    public bool Show(ObjRef objref, bool ignoreLayerMode)
    {
      if (null == objref)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxShowObject);
    }
    /// <summary>
    /// If obj.IsHidden() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="obj">the normal object to show.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be shown even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully shown.</returns>
    /// <since>5.0</since>
    public bool Show(RhinoObject obj, bool ignoreLayerMode)
    {
      if (null == obj)
        return false;
      return Show(obj.Id, ignoreLayerMode);
    }
    /// <summary>
    /// If Object().IsHidden() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="objectId">Id of the normal object to show.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be shown even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully shown.</returns>
    /// <since>5.0</since>
    public bool Show(Guid objectId, bool ignoreLayerMode)
    {
      if (Guid.Empty == objectId)
        return false;
      var objref = new ObjRef(Document, objectId);
      IntPtr pObjRef = objref.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxShowObject);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// If objref.Object().IsNormal() is true, then the object will be locked.
    /// </summary>
    /// <param name="objref">reference to normal object to lock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be locked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully locked.</returns>
    /// <since>5.0</since>
    public bool Lock(ObjRef objref, bool ignoreLayerMode)
    {
      if (null == objref)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxLockObject);
    }
    /// <summary>
    /// If obj.IsNormal() is true, then the object will be locked.
    /// </summary>
    /// <param name="obj">normal object to lock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be locked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully locked.</returns>
    /// <since>5.0</since>
    public bool Lock(RhinoObject obj, bool ignoreLayerMode)
    {
      if (null == obj)
        return false;
      return Lock(obj.Id, ignoreLayerMode);
    }
    /// <summary>
    /// If objref.Object().IsNormal() is true, then the object will be locked.
    /// </summary>
    /// <param name="objectId">Id of normal object to lock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be locked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully locked.</returns>
    /// <since>5.0</since>
    public bool Lock(Guid objectId, bool ignoreLayerMode)
    {
      if (Guid.Empty == objectId)
        return false;
      var objref = new ObjRef(Document, objectId);
      IntPtr pObjRef = objref.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxLockObject);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// If objref.Object().IsLocked() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="objref">reference to locked object to unlock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be unlocked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully unlocked.</returns>
    /// <since>5.0</since>
    public bool Unlock(ObjRef objref, bool ignoreLayerMode)
    {
      if (null == objref)
        return false;
      IntPtr pObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxUnlockObject);
    }
    /// <summary>
    /// If obj.IsLocked() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="obj">locked object to unlock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be unlocked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully unlocked.</returns>
    /// <since>5.0</since>
    public bool Unlock(RhinoObject obj, bool ignoreLayerMode)
    {
      if (null == obj)
        return false;
      return Unlock(obj.Id, ignoreLayerMode);
    }
    /// <summary>
    /// If Object().IsLocked() is true, then the object will be returned to normal (visible and selectable) mode.
    /// </summary>
    /// <param name="objectId">Id of locked object to unlock.</param>
    /// <param name="ignoreLayerMode">
    /// if true, the object will be unlocked even if it is on a layer that is locked or off.
    /// </param>
    /// <returns>true if the object was successfully unlocked.</returns>
    /// <since>5.0</since>
    public bool Unlock(Guid objectId, bool ignoreLayerMode)
    {
      if (Guid.Empty == objectId)
        return false;
      var objref = new ObjRef(Document, objectId);
      IntPtr pObjRef = objref.ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoDoc_SetObjectState(m_doc.RuntimeSerialNumber, pObjRef, ignoreLayerMode, idxUnlockObject);
      objref.Dispose();
      return rc;
    }
    #endregion

    #region Object transforms
    /// <summary>
    /// Gets the bounding box for all objects (normal, locked and hidden) in this
    /// document that exist in "model" space. This bounding box does not include
    /// objects that exist in layout space.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get
      {
        Point3d min = Point3d.Unset;
        Point3d max = Point3d.Unset;

        if (UnsafeNativeMethods.CRhinoDoc_BoundingBox(m_doc.RuntimeSerialNumber, ref min, ref max, false))
          return new BoundingBox(min, max);

        return BoundingBox.Empty;
      }
    }
    /// <summary>
    /// Gets the bounding box for all visible objects (normal and locked) in this
    /// document that exist in "model" space. This bounding box does not include
    /// hidden objects or any objects that exist in layout space.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox BoundingBoxVisible
    {
      get
      {
        Point3d min = Point3d.Unset;
        Point3d max = Point3d.Unset;

        if (UnsafeNativeMethods.CRhinoDoc_BoundingBox(m_doc.RuntimeSerialNumber, ref min, ref max, true))
          return new BoundingBox(min, max);

        return BoundingBox.Empty;
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.ModelGeometry;
      }
    }

    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and deletes the existing object if deleteOriginal is true.
    /// </summary>
    /// <param name="objref">
    /// reference to object to transform. The objref.Object() will be deleted if deleteOriginal is true.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <param name="deleteOriginal">
    /// if true, the original object is deleted
    /// if false, the original object is not deleted.
    /// </param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid Transform(ObjRef objref, Transform xform, bool deleteOriginal)
    {
      if (null == objref)
        return Guid.Empty;
      IntPtr pObjRef = objref.NonConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_TransformObject(m_doc.RuntimeSerialNumber, pObjRef, ref xform, deleteOriginal, false, false);
    }
    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and deletes the existing object if deleteOriginal is true.
    /// </summary>
    /// <param name="obj">
    /// Rhino object to transform. This object will be deleted if deleteOriginal is true.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <param name="deleteOriginal">
    /// if true, the original object is deleted
    /// if false, the original object is not deleted.
    /// </param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <since>5.0</since>
    public Guid Transform(RhinoObject obj, Transform xform, bool deleteOriginal)
    {
      if (null == obj)
        return Guid.Empty;
      var objref = new ObjRef(obj);
      Guid rc = Transform(objref, xform, deleteOriginal);
      objref.Dispose();
      return rc;
    }
    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and deletes the existing object if deleteOriginal is true.
    /// </summary>
    /// <param name="objectId">
    /// Id of rhino object to transform. This object will be deleted if deleteOriginal is true.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <param name="deleteOriginal">
    /// if true, the original object is deleted
    /// if false, the original object is not deleted.
    /// </param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <since>5.0</since>
    public Guid Transform(Guid objectId, Transform xform, bool deleteOriginal)
    {
      var objref = new ObjRef(Document, objectId);
      Guid rc = Transform(objref, xform, deleteOriginal);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and records history of the transformation if history recording is turned on.
    /// If history recording is not enabled, this function will act the same as
    /// Transform(objref, xform, false)
    /// </summary>
    /// <param name="objref">
    /// reference to object to transform.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <since>5.0</since>
    public Guid TransformWithHistory(ObjRef objref, Transform xform)
    {
      if (null == objref)
        return Guid.Empty;
      IntPtr pObjRef = objref.NonConstPointer();
      return UnsafeNativeMethods.CRhinoDoc_TransformObject(m_doc.RuntimeSerialNumber, pObjRef, ref xform, false, false, true);
    }
    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and records history of the transformation if history recording is turned on.
    /// If history recording is not enabled, this function will act the same as
    /// Transform(obj, xform, false)
    /// </summary>
    /// <param name="obj">
    /// Rhino object to transform.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <since>5.0</since>
    public Guid TransformWithHistory(RhinoObject obj, Transform xform)
    {
      if (null == obj)
        return Guid.Empty;
      var objref = new ObjRef(obj);
      Guid rc = TransformWithHistory(objref, xform);
      objref.Dispose();
      return rc;
    }
    /// <summary>
    /// Constructs a new object that is the transformation of the existing object
    /// and records history of the transformation if history recording is turned on.
    /// If history recording is not enabled, this function will act the same as
    /// Transform(objectId, xform, false)
    /// </summary>
    /// <param name="objectId">
    /// Id of rhino object to transform.
    /// </param>
    /// <param name="xform">transformation to apply.</param>
    /// <returns>
    /// Id of the new object that is the transformation of the existing_object.
    /// The new object has identical attributes.
    /// </returns>
    /// <remarks>
    /// If the object is locked or on a locked layer, then it cannot be transformed.
    /// </remarks>
    /// <since>5.0</since>
    public Guid TransformWithHistory(Guid objectId, Transform xform)
    {
      var objref = new ObjRef(Document, objectId);
      Guid rc = TransformWithHistory(objref, xform);
      objref.Dispose();
      return rc;
    }

    /// <summary>
    /// Duplicates the object that is referenced by objref.
    /// <para>Same as Transform(objref, <see cref="Geometry.Transform.Identity">Transform.Identity</see>, false)</para>
    /// </summary>
    /// <param name="objref">A Rhino object reference to follow for object duplication.</param>
    /// <returns>The new object ID.</returns>
    /// <since>5.0</since>
    public Guid Duplicate(ObjRef objref)
    {
      return Transform(objref, Geometry.Transform.Identity, false);
    }
    /// <summary>
    /// Duplicates the object that is referenced by obj.
    /// <para>Same as TransformObject(obj, <see cref="Geometry.Transform.Identity">Transform.Identity</see>y, false)</para>
    /// </summary>
    /// <param name="obj">A Rhino object to duplicate.</param>
    /// <returns>The new object ID.</returns>
    /// <since>5.0</since>
    public Guid Duplicate(RhinoObject obj)
    {
      return Transform(obj, Geometry.Transform.Identity, false);
    }
    /// <summary>
    /// Same as TransformObject(objref, ON_Xform.Identity, false)
    /// </summary>
    /// <param name="objectId">An ID to an object in the document that needs to be duplicated.</param>
    /// <returns>The new object ID.</returns>
    /// <since>5.0</since>
    public Guid Duplicate(Guid objectId)
    {
      return Transform(objectId, Geometry.Transform.Identity, false);
    }

    //  Description:
    //    Creates a new object that is the transformation of the
    //    existing object and deletes the existing object if 
    //    bDeleteOriginal is true.
    //  Parameters:
    //    old_object - [in] reference to object to morph.  The object
    //        objref.Object() will be deleted if bDeleteOriginal is true.
    //    xform - [in] transformation to apply
    //    bAddNewObjectToDoc - [in] 
    //        if true, the new object is added to the document.
    //        if false, the new object is not added to the document.
    //    bDeleteOriginal - [in] 
    //        if true, the original object is deleted
    //        if false, the original object is not deleted
    //    bAddTransformHistory - [in]
    //        If true and history recording is turned on, then
    //        transformation history is recorded.  This will be
    //        adequate for simple transformation commands like
    //        rotate, move, scale, and so on that do not have
    //        auxiliary input objects.  For fancier commands,
    //        that have an auxiliary object, like the spine
    //        curve in ArrayAlongCrv, set bAddTransformHistory
    //        to false. 
    //  Returns:
    //    New object that is the morph of the existing_object.
    //    The new object has identical attributes.
    //  Remarks:
    //    If the object is locked or on a locked layer, then it cannot
    //    be transformed.
    //  CRhinoObject* TransformObject( 
    //    const CRhinoObject* old_object,
    //    const ON_Xform& xform,
    //    bool bAddNewObjectToDoc,
    //    bool bDeleteOriginal,
    //    bool bAddTransformHistory
    //    );


    //  /*
    //  Description:
    //    Transforms every object in a list.
    //  Parameters:
    //    it - [in] iterates through list of objects to transform
    //    xform - [in] transformation to apply
    //    bDeleteOriginal = [in] 
    //         if true, the original objects are deleted
    //         if false, the original objects are not deleted
    //    bIgnoreModes - [in] If true, locked and hidden objects
    //        are transformed.  If false objects that are locked,
    //        hidden, or on locked or hidden layers are not 
    //        transformed.
    //    bAddTransformHistory - [in]
    //        If true and history recording is turned on, then
    //        transformation history is recorded.  This will be
    //        adequate for simple transformation commands like
    //        rotate, move, scale, and so on that do not have
    //        auxiliary input objects.  For fancier commands,
    //        that have an auxiliary object, like the spine
    //        curve in ArrayAlongCrv, set bAddTransformHistory
    //        to false. 
    //  Returns:
    //    Number of objects that were transformed.
    //  Remarks:
    //    This is similar to calling TransformObject() for each object
    //    int the list except that this function will modify locked and
    //    hidden objects.  It is used for things like scaling the entire
    //    model when a unit system is changed.
    //  */
    //  int TransformObjects(
    //    CRhinoObjectIterator& it,
    //    const ON_Xform& xform,
    //    bool bDeleteOriginal = true,
    //    bool bIgnoreModes = false,
    //    bool bAddTransformHistory = false
    //    );

    //public Guid MorphObject(DocObjects.RhinoObject oldObject, ON_SpaceMorph morph, bool deleteOriginal)
    //{
    //  if (null == oldObject || null == morph)
    //    return Guid.Empty;

    //  Guid rc = morph.PerformMorph(m_runtime_serial_number, oldObject, deleteOriginal);
    //  return rc;
    //}
    //  CRhinoObject* MorphObject( const CRhinoObject* old_object, const ON_SpaceMorph& morph, bool bAddNewObjectToDoc, bool bDeleteOriginal );
    //[skipping]
    //  BOOL SnapTo( const CRhinoSnapContext& snap_context, CRhinoSnapEvent& snap_event, const ON_SimpleArray<ON_3dPoint>* construction_points = 0,

    /// <summary>
    /// Pick one or more objects based on a given pick context
    /// </summary>
    /// <param name="pickContext">settings used to define what is picked</param>
    /// <returns>zero or more objects</returns>
    /// <since>6.0</since>
    public ObjRef[] PickObjects(Input.Custom.PickContext pickContext)
    {
      IntPtr const_ptr_pickcontext = pickContext.ConstPointer();
      using (var objref_array = new Runtime.InternalRhinoObjRefArray())
      {
        IntPtr ptr_array = objref_array.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_PickObjects(m_doc.RuntimeSerialNumber, const_ptr_pickcontext, ptr_array);
        return objref_array.ToArray();
      }
    }

    /// <summary>
    /// Altered grip positions on a RhinoObject are used to calculate an updated object
    /// that is added to the document.
    /// </summary>
    /// <param name="obj">object with modified grips to update.</param>
    /// <param name="deleteOriginal">if true, obj is deleted from the document.</param>
    /// <returns>new RhinoObject on success; otherwise null.</returns>
    /// <since>5.0</since>
    public RhinoObject GripUpdate(RhinoObject obj, bool deleteOriginal)
    {
      if (null == obj)
        return null;

      IntPtr pRhinoObject = obj.ConstPointer();
      IntPtr pNewObject = UnsafeNativeMethods.RHC_RhinoUpdateGripOwner(pRhinoObject, deleteOriginal);
      return RhinoObject.CreateRhinoObjectHelper(pNewObject);
    }

    // [skipping]
    //  void ClearMarks( CRhinoObjectIterator&, int=0 );
    //  void SetRedrawDisplayHint( unsigned int display_hint, ON::display_mode dm = ON::default_display ) const;
    #endregion

    #region Object enumerator

    private IEnumerator<RhinoObject> GetEnumerator(ObjectEnumeratorSettings settings)
    {
      ObjectIterator it = new ObjectIterator(m_doc, settings);
      return it;
    }

    private class EnumeratorWrapper : IEnumerable<RhinoObject>
    {
      readonly IEnumerator<RhinoObject> m_enumerator;
      public EnumeratorWrapper(IEnumerator<RhinoObject> enumerator)
      {
        m_enumerator = enumerator;
      }

      public IEnumerator<RhinoObject> GetEnumerator()
      {
        return m_enumerator;
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return m_enumerator;
      }
    }

    private class EnumeratorWrapperOfType<T> : IEnumerable<T> where T : RhinoObject
    {
      readonly IEnumerator<T> m_enumerator;
      public EnumeratorWrapperOfType(IEnumerator<T> enumerator)
      {
        m_enumerator = enumerator;
      }
      public IEnumerator<T> GetEnumerator()
      {
        return m_enumerator;
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return m_enumerator;
      }
    }

    /// <summary>
    /// Returns the number objects that pass a filter.
    /// </summary>
    /// <param name="filter">The <see cref="Rhino.DocObjects.ObjectEnumeratorSettings"/> filter.</param>
    /// <returns>The number of objects that pass the filter.</returns>
    public int ObjectCount(ObjectEnumeratorSettings filter)
    {
      ObjectIterator it = new ObjectIterator(m_doc, filter);
      IntPtr ptr_iterator = it.NonConstPointer();
      string name_filter = filter.m_name_filter;
      int count = UnsafeNativeMethods.CRhinoObjectIterator_Count(ptr_iterator, name_filter);
      it.Dispose();
      return count;
    }

    /// <summary>
    /// Returns an enumerable based on a <see cref="Rhino.DocObjects.ObjectEnumeratorSettings"/> filter.
    /// </summary>
    /// <param name="settings">The <see cref="Rhino.DocObjects.ObjectEnumeratorSettings"/> settings.</param>
    /// <returns>The enumerable.</returns>
    public IEnumerable<RhinoObject> GetObjectList(ObjectEnumeratorSettings settings)
    {
      IEnumerator<RhinoObject> e = GetEnumerator(settings);
      return new EnumeratorWrapper(e);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_dimstyle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dimstyle.cs' lang='cs'/>
    /// <code source='examples\py\ex_dimstyle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public IEnumerable<RhinoObject> GetObjectList(Type typeFilter)
    {
      var settings = new ObjectEnumeratorSettings { ClassTypeFilter = typeFilter };
      var it = new ObjectIterator(m_doc, settings);
      return new EnumeratorWrapper(it);
    }

    /// <summary>
    /// Returns Rhino object by type.
    /// </summary>
    /// <returns>The enumerable.</returns>
    /// <since>6.0</since>
    public IEnumerable<T> GetObjectsByType<T>() where T : RhinoObject
    {
      var settings = new ObjectEnumeratorSettings { ClassTypeFilter = typeof(T) };
      return GetObjectsByType<T>(settings);
    }

    /// <summary>
    /// Returns Rhino object by type.
    /// </summary>
    /// <returns>The enumerable.</returns>
    /// <since>6.0</since>
    public IEnumerable<T> GetObjectsByType<T>(ObjectEnumeratorSettings settings) where T : RhinoObject
    {
      settings.ClassTypeFilter = typeof(T);
      var it = new ObjectIteratorOfType<T>(m_doc, settings);
      return new EnumeratorWrapperOfType<T>(it);
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public IEnumerable<RhinoObject> GetObjectList(ObjectType typeFilter)
    {
      var settings = new ObjectEnumeratorSettings { ObjectTypeFilter = typeFilter };
      return GetObjectList(settings);
    }

    /// <since>5.0</since>
    public IEnumerable<RhinoObject> GetSelectedObjects(bool includeLights, bool includeGrips)
    {
      var s = new ObjectEnumeratorSettings
      {
        IncludeLights = includeLights,
        IncludeGrips = includeGrips,
        IncludePhantoms = true,
        SelectedObjectsFilter = true
      };
      return GetObjectList(s);
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public ObjectType GetSelectedObjectTypes()
    {
      return (ObjectType)UnsafeNativeMethods.CRhinoDoc_SelectedObjectTypes(m_doc.RuntimeSerialNumber);
    }

    /// <since>5.0</since>
    public override IEnumerator<RhinoObject> GetEnumerator()
    {
      var s = new ObjectEnumeratorSettings();
      return GetEnumerator(s);
    }

    #endregion
  }

  internal class RhinoDocManifestTable : ManifestTable
  {
    private readonly RhinoDoc m_doc;
    internal RhinoDocManifestTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>
    /// Gets the document that owns this object table.
    /// </summary>
    public override object Parent
    {
      get { return m_doc; }
    }

    public override ModelComponent FindId(Guid id)
    {
      return FindId(id, ModelComponentType.Unset);
    }

    public override ModelComponent FindId(Guid id, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentNullException(nameof(type));

      int index = RhinoMath.UnsetIntIndex; 
      IntPtr ptr_comp = UnsafeNativeMethods.CRhinoDoc_LookupDocumentObject(m_doc.RuntimeSerialNumber, id, ref type, ref index);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_doc, type, index, ptr_comp);
    }

    public override ModelComponent FindIndex(int index, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentNullException(nameof(type));
      if(ModelComponentType.Unset == type)
        throw new ArgumentNullException(nameof(type), "Type must be set.");

      Guid id = default(Guid);
      IntPtr ptr_comp = UnsafeNativeMethods.CRhinoDoc_LookupModelComponentByIndex(m_doc.RuntimeSerialNumber, type, index, ref id);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_doc, type, index, ptr_comp);
    }

    public override ModelComponent FindName(string name, ModelComponentType type, Guid parent)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentNullException(nameof(type));

      if (!ModelComponent.ModelComponentTypeRequiresUniqueName(type))
        throw new ArgumentOutOfRangeException(nameof(type), "Cannot use a non-unique name for the search. This type does not require uniqueness.");

      int index = default(int);
      Guid id = default(Guid);
      IntPtr ptr_comp = UnsafeNativeMethods.CRhinoDoc_LookupModelComponentByName(m_doc.RuntimeSerialNumber, type, parent, name, ref index, ref id);

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_doc, type, index, ptr_comp);
    }

    public override ModelComponent FindNameHash(NameHash nameHash, ModelComponentType type)
    {
      if (!Enum.IsDefined(typeof(ModelComponentType), type))
        throw new ArgumentNullException(nameof(type));

      if (!ModelComponent.ModelComponentTypeRequiresUniqueName(type))
        throw new ArgumentOutOfRangeException(nameof(type), "Cannot use a non-unique name for the search. This type does not require uniqueness.");


      if (nameHash == null) throw new ArgumentNullException(nameof(nameHash));

      int index = default(int);
      Guid id = default(Guid);
      IntPtr ptr_comp;
      using (var handle = nameHash.GetDisposableHandle())
      {
        ptr_comp = UnsafeNativeMethods.CRhinoDoc_LookupModelComponentByNameHash(m_doc.RuntimeSerialNumber, type, handle, ref index, ref id);
      }

      if (ptr_comp == IntPtr.Zero) return null;

      return Instantiate(m_doc, type, index, ptr_comp);
    }

    internal static ModelComponent Instantiate(RhinoDoc doc, ModelComponentType type, int index, IntPtr ptr)
    {
      switch (type)
      {
        case ModelComponentType.Unset:
          throw new NotImplementedException("This should never happen. CRhinoDoc_LookupModelComponent* should change Unset to the correct type.");

        case ModelComponentType.Image:
          return new BitmapEntry(index, doc);

        case ModelComponentType.TextureMapping:
          goto default; //not yet ON_ModelComponent derived

        case ModelComponentType.RenderMaterial:
          return new Material(index, doc);

        case ModelComponentType.LinePattern:
          return new Linetype(index, doc);

        case ModelComponentType.Layer:
          return new Layer(index, doc);

        case ModelComponentType.Group:
          return new Group(index, doc); //not yet ON_ModelComponent derived but I hope will be fast

        //case ModelComponentType.TextStyle:
        //  return new TextStyle(index, doc); //needs green light from Lowell;

        case ModelComponentType.DimStyle:
          return new DimensionStyle(index, doc);

        case ModelComponentType.RenderLight:
          return doc.Lights[index]; // Get the light from the table index

        case ModelComponentType.HatchPattern:
          return new HatchPattern(index, doc);

        case ModelComponentType.InstanceDefinition:
          return new InstanceDefinition(index, doc);

        case ModelComponentType.ModelGeometry:
          return RhinoObject.CreateRhinoObjectHelper(ptr);

        case ModelComponentType.HistoryRecord:
          goto default; //not yet ON_ModelComponent derived

        default:
          throw new NotImplementedException(
            string.Format(
              "Tell giulio@mcneel.com if you need access to ModelComponentType.{0}.",
              type.ToString()
              )
            );
      }
    }

    internal override IntPtr GetConstOnComponentManifestPtr()
    {
      return UnsafeNativeMethods.CRhinoDoc_GetConstOnComponentManifestPtr(m_doc.RuntimeSerialNumber);
    }

    public override IEnumerator<ModelComponent> GetEnumerator(ModelComponentType type)
    {
      Guid current_id = Guid.Empty;
      int current_index = RhinoMath.UnsetIntIndex;

      IntPtr current_ptr = UnsafeNativeMethods.CRhinoManifestIterator_FirstNextItem(m_doc.RuntimeSerialNumber, type, ref current_id, ref current_index);

      while (current_ptr != IntPtr.Zero)
      {
        var instance = Instantiate(m_doc, type, current_index, current_ptr);
        yield return instance;
        current_ptr = UnsafeNativeMethods.CRhinoManifestIterator_FirstNextItem(m_doc.RuntimeSerialNumber, type, ref current_id, ref current_index);
      }
    }
  }

  /// <summary>
  /// Collection of document user data strings
  /// </summary>
  public sealed class StringTable
  {
    internal StringTable(RhinoDoc doc)
    {
      Document = doc;
    }

    /// <summary>Document that owns this object table.</summary>
    /// <since>5.0</since>
    public RhinoDoc Document { get; }

    /// <summary>
    /// The number of user data strings in the current document.
    /// </summary>
    /// <since>5.0</since>
    public int Count => UnsafeNativeMethods.CRhinoDoc_GetDocTextCount(Document.RuntimeSerialNumber);

    /// <since>6.0</since>
    public int DocumentDataCount
    {
      get
      {
        int cnt = 0;
        for (int i = 0; i < Count; i++)
          if ((GetKey(i)??"").Contains("\\")) cnt++;
        return cnt;
      }
    }
    /// <since>6.0</since>
    public int DocumentUserTextCount
    {
      get
      {
        int cnt = 0;
        for (int i = 0; i < Count; i++)
          if (!(GetKey(i)??"").Contains("\\")) cnt++;
        return cnt;
      }
    }

    /// <since>5.0</since>
    public string GetKey(int i)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_GetDocTextString(Document.RuntimeSerialNumber, i, true, ptr_string);
        return sh.ToString();
      }
    }

    /// <since>5.0</since>
    public string GetValue(int i)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_GetDocTextString(Document.RuntimeSerialNumber, i, false, ptr_string);
        return sh.ToString();
      }
    }
    /// <since>5.0</since>
    public string GetValue(string key)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoDoc_GetDocTextString2(Document.RuntimeSerialNumber, key, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets a user data string from the document.
    /// </summary>
    /// <param name="section">The section at which to get the value.</param>
    /// <param name="entry">The entry to search for.</param>
    /// <returns>The user data.</returns>
    /// <since>5.0</since>
    public string GetValue(string section, string entry)
    {
      if (String.IsNullOrEmpty(section) || String.IsNullOrEmpty(entry))
        return String.Empty;
      string key = section + "\\" + entry;
      return GetValue(key);
    }

    /// <summary>
    /// Returns a list of all the section names for user data strings in the document.
    /// <para>By default a section name is a key that is prefixed with a string separated by a backslash.</para>
    /// </summary>
    /// <returns>An array of section names. This can be empty, but not null.</returns>
    /// <since>5.0</since>
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
    /// Return list of all entry names for a given section of user data strings in the document.
    /// </summary>
    /// <param name="section">The section from which to retrieve section names.</param>
    /// <returns>An array of section names. This can be empty, but not null.</returns>
    /// <since>5.0</since>
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
    /// Adds or sets a user data string to the document.
    /// </summary>
    /// <param name="section">The section.</param>
    /// <param name="entry">The entry name.</param>
    /// <param name="value">The entry value.</param>
    /// <returns>
    /// The previous value if successful and a previous value existed.
    /// </returns>
    /// <since>5.0</since>
    public string SetString(string section, string entry, string value)
    {
      string key = section;
      if ( !string.IsNullOrEmpty(entry) )
        key = section + "\\" + entry;
      var rc = GetValue(key);
      UnsafeNativeMethods.CRhinoDoc_SetDocTextString(Document.RuntimeSerialNumber, key, value);
      return rc;
    }

    /// <summary>
    /// Adds or sets a user data string to the document.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The entry value.</param>
    /// <returns>
    /// The previous value if successful and a previous value existed.
    /// </returns>
    /// <since>5.0</since>
    public string SetString(string key, string value)
    {
      // 10-Mar-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-38337
      //if (key != null && key.Contains("\\"))
      // back slash is reserved as a separator for "section\entry" in 
      // rhinoscriptsyntax.SetDocumentData which uses the SetString(str, str, str) overload
      //throw new ArgumentException("key cannot contain '\\' (back slashes)");

      string rc = GetValue(key);
      UnsafeNativeMethods.CRhinoDoc_SetDocTextString(Document.RuntimeSerialNumber, key, value);
      return rc;
    }

    /// <summary>
    /// Removes user data strings from the document.
    /// </summary>
    /// <param name="section">name of section to delete. If null, all sections will be deleted.</param>
    /// <param name="entry">name of entry to delete. If null, all entries will be deleted for a given section.</param>
    /// <since>5.0</since>
    public void Delete(string section, string entry)
    {
      if (null == section && null != entry)
        throw new ArgumentNullException(nameof(section), "'section' cannot be null if an 'entry' is passed");

      if (null == section)
      {
        //UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(m_doc.RuntimeSerialNumber, null);
        foreach (var sectionName in GetSectionNames())
          foreach(var entryName in GetEntryNames(sectionName))
            UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(Document.RuntimeSerialNumber, sectionName +"\\" + entryName);
        return;
      }

      if (null == entry)
      {
        string[] entries = GetEntryNames(section);
        for (int i = 0; i < entries.Length; i++)
        {
          string key = section + "\\" + entries[i];
          UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(Document.RuntimeSerialNumber, key);
        }
        UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(Document.RuntimeSerialNumber, section);
      }
      else
      {
        string key = section + "\\" + entry;
        UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(Document.RuntimeSerialNumber, key);
      }
    }

    /// <since>5.0</since>
    public void Delete(string key)
    {
      UnsafeNativeMethods.CRhinoDoc_DeleteDocTextString(Document.RuntimeSerialNumber, key);
    }
  }
}

namespace Rhino.DocObjects
{
#region private helper enums
  [FlagsAttribute]
  enum object_state
  {
    None = 0,
    normal_objects = 1, // (not locked and not hidden)
    locked_objects = 2, // (locked objects or on locked layers)
    hidden_objects = 4, // (hidden objects or on hidden layers)
    idef_objects = 8, // objects in instance definitions (not the instance references)
    deleted_objects = 16,
    normal_or_locked_objects = 3, // normal or locked
    undeleted_objects = 7       // normal, locked, or hidden
    //all_objects = 0xFFFFFFFF      // normal, locked, hidden, or deleted
  }
  [FlagsAttribute]
  enum object_category
  {
    None = 0,
    active_objects = 1,    // objects that are part of current model and saved in file
    reference_objects = 2, // objects that are for reference and not saved in file
    active_and_reference_objects = 3
  }
  #endregion


  /// <summary>
  /// Settings used for getting an enumerator of objects in a document.
  /// See <see cref="ObjectTable.FindByFilter(ObjectEnumeratorSettings)"/>, 
  /// <see cref="ObjectTable.GetObjectsByType{T}(ObjectEnumeratorSettings)"/>, 
  /// and <see cref="ObjectTable.GetEnumerator(ObjectEnumeratorSettings)"/>.
  /// </summary>
  /// <example>
  /// <code source='examples\vbnet\ex_moveobjectstocurrentlayer.vb' lang='vbnet'/>
  /// <code source='examples\cs\ex_moveobjectstocurrentlayer.cs' lang='cs'/>
  /// <code source='examples\py\ex_moveobjectstocurrentlayer.py' lang='py'/>
  /// </example>
  public class ObjectEnumeratorSettings
  {
    // all variables are set to use same defaults as defined in CRhinoObjectIterator::Init
    internal object_category m_object_category = object_category.active_objects;
    internal object_state m_object_state = object_state.normal_or_locked_objects;
    internal ObjectType m_objectfilter = ObjectType.None;
    int m_layerindex_filter = -1;

    /// <summary>
    /// Constructs object enumerator settings that will iterate the document looking for
    /// normal object and locked object that are active, or part of current model and saved in file.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_findobjectsbyname.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_findobjectsbyname.cs' lang='cs'/>
    /// <code source='examples\py\ex_findobjectsbyname.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public ObjectEnumeratorSettings()
    {
    }

    #region object state
    /// <summary>
    /// When true, normal objects (e.g. not locked and not hidden) are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool NormalObjects
    {
      get
      {
        return (m_object_state & object_state.normal_objects) == object_state.normal_objects;
      }
      set
      {
        if (value)
          m_object_state |= object_state.normal_objects;
        else
          m_object_state &= ~object_state.normal_objects;
      }
    }

    /// <summary>
    /// When true, locked objects or objects on locked layers are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool LockedObjects
    {
      get
      {
        return (m_object_state & object_state.locked_objects) == object_state.locked_objects;
      }
      set
      {
        if (value)
          m_object_state |= object_state.locked_objects;
        else
          m_object_state &= ~object_state.locked_objects;
      }
    }

    /// <summary>
    /// When true, hidden objects or objects on hidden layers are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool HiddenObjects
    {
      get
      {
        return (m_object_state & object_state.hidden_objects) == object_state.hidden_objects;
      }
      set
      {
        if (value)
          m_object_state |= object_state.hidden_objects;
        else
          m_object_state &= ~object_state.hidden_objects;
      }
    }

    /// <summary>
    /// When true, objects in instance definitions (not the instance references) are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool IdefObjects
    {
      get
      {
        return (m_object_state & object_state.idef_objects) == object_state.idef_objects;
      }
      set
      {
        if (value)
          m_object_state |= object_state.idef_objects;
        else
          m_object_state &= ~object_state.idef_objects;
      }
    }

    /// <summary>
    /// When true, deleted objects are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool DeletedObjects
    {
      get
      {
        return (m_object_state & object_state.deleted_objects) == object_state.deleted_objects;
      }
      set
      {
        if (value)
          m_object_state |= object_state.deleted_objects;
        else
          m_object_state &= ~object_state.deleted_objects;
      }
    }
    #endregion

    #region object category

    /// <summary>
    /// If true then objects which only have a sub object selected
    /// will be included. This is false by default.
    /// </summary>
    /// <since>7.3</since>
    public bool SubObjectSelected { get; set; }

    /// <summary>
    /// When true, objects that are part of current model and saved in file are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool ActiveObjects
    {
      get
      {
        return (m_object_category & object_category.active_objects) == object_category.active_objects;
      }
      set
      {
        if (value)
          m_object_category |= object_category.active_objects;
        else
          m_object_category &= ~object_category.active_objects;
      }
    }

    /// <summary>
    /// When true, objects that are for reference and not saved in file are returned.
    /// </summary>
    /// <since>5.0</since>
    public bool ReferenceObjects
    {
      get
      {
        return (m_object_category & object_category.reference_objects) == object_category.reference_objects;
      }
      set
      {
        if (value)
          m_object_category |= object_category.reference_objects;
        else
          m_object_category &= ~object_category.reference_objects;
      }
    }
    #endregion

    /// <summary>
    /// The default object enumerator settings will not iterate through render light objects.
    /// If you want the iterator to include lights, then set this property to true.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_objectiterator.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectiterator.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectiterator.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IncludeLights { get; set; }

    /// <summary>
    /// The default object enumerator settings will not iterate through grip objects.
    /// If you want the iterator to include grips, then set this property to true.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_objectiterator.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectiterator.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectiterator.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IncludeGrips { get; set; }

    /// <summary>
    /// The default object enumerator settings will not iterate through phantom objects.
    /// If you want the iterator to include phantom objects, then set this property to true.
    /// </summary>
    /// <since>5.0</since>
    public bool IncludePhantoms { get; set; }

    /// <summary>
    /// The default object enumerator settings ignore the selected state of objects.
    /// If you want the iterator to limit itself to selected objects, then set this property to true.
    /// </summary>
    /// <since>5.0</since>
    public bool SelectedObjectsFilter { get; set; }

    /// <summary>
    /// The default object enumerator settings ignore the visibility state of objects.
    /// If you want the iterator to limit itself to visible objects, then set this property to true.
    /// </summary>
    /// <since>5.0</since>
    public bool VisibleFilter { get; set; }

    /// <summary>
    /// The object type filter property can be used to limit the iteration to specific types of geometry.
    /// The default is to iterate all objects types.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public ObjectType ObjectTypeFilter
    {
      get { return m_objectfilter; }
      set { m_objectfilter = value; }
    }

    /// <since>5.0</since>
    public Type ClassTypeFilter { get; set; }

    /// <summary>
    /// The layer filter property can be used to limit the iteration to objects on a specific layer.
    /// The default is to iterate through all layers.
    /// </summary>
    /// <since>5.0</since>
    public int LayerIndexFilter
    {
      get { return m_layerindex_filter; }
      set { m_layerindex_filter = value; }
    }

    internal string m_name_filter; // = null; initialized to null by runtime
    /// <summary>
    /// The name filter property can be used to limit the iteration to objects with a specific name.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_findobjectsbyname.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_findobjectsbyname.cs' lang='cs'/>
    /// <code source='examples\py\ex_findobjectsbyname.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public string NameFilter
    {
      get
      {
        if (string.IsNullOrEmpty(m_name_filter))
          return "*";
        return m_name_filter;
      }
      set
      {
        if (string.IsNullOrEmpty(value))
          m_name_filter = null;
        else
        {
          value = value.Trim();
          m_name_filter = value == "*" ? null : value;
        }
      }
    }

    /// <summary>
    /// The viewport filter property can be used to limit the iteration to objects that are active in a specific viewport.
    /// </summary>
    /// <since>5.6</since>
    public RhinoViewport ViewportFilter { get; set; }
  }

  // ObjectIterator is not public. We only want to give the user an enumerator
  class ObjectIterator : IEnumerator<RhinoObject>
  {
#region IEnumerator Members
    bool m_first = true;
    RhinoObject m_current;

    object System.Collections.IEnumerator.Current
    {
      get
      {
        return m_current;
      }
    }

    public bool MoveNext()
    {
      while (MoveNextHelper())
      {
        if (m_settings != null && m_settings.ClassTypeFilter != null && m_current != null)
        {
          if (!m_settings.ClassTypeFilter.IsInstanceOfType(m_current))
          {
            m_current = null;
            continue;
          }
        }
        return true;
      }
      return false;
    }

    bool MoveNextHelper()
    {
      bool first = m_first;
      m_first = false;
      IntPtr ptr = NonConstPointer();
      string name_filter = null;
      if (null != m_settings)
        name_filter = m_settings.m_name_filter;
      IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoObjectIterator_FirstNext(ptr, first, name_filter);
      if (IntPtr.Zero == ptr_rhino_object)
        return false;
      m_current = RhinoObject.CreateRhinoObjectHelper(ptr_rhino_object);
      return true;
    }

    public void Reset()
    {
      m_first = true;
      m_current = null;
    }

    #endregion

#region IEnumerator<RhinoObject> Members

    public RhinoObject Current
    {
      get { return m_current; }
    }

    #endregion

    // This class is always constructed inside .NET and is
    // therefore never const
    IntPtr m_ptr; // CRhinoObjectIterator*
    internal IntPtr NonConstPointer() { return m_ptr; }
    readonly ObjectEnumeratorSettings m_settings;

    public ObjectIterator(RhinoDoc doc, ObjectEnumeratorSettings s)
    {
      m_settings = s;
      uint doc_id = 0;
      if (doc != null)
        doc_id = doc.RuntimeSerialNumber;

      IntPtr const_ptr_viewport = IntPtr.Zero;
      if (s.ViewportFilter != null)
        const_ptr_viewport = s.ViewportFilter.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoObjectIterator_New(doc_id, (int)s.m_object_state, (int)s.m_object_category);
      UnsafeNativeMethods.CRhinoObjectIterator_Initialize(m_ptr,
        s.IncludeLights,
        s.IncludeGrips,
        s.IncludePhantoms,
        s.SelectedObjectsFilter,
        s.SubObjectSelected,
        s.VisibleFilter,
        (uint)s.m_objectfilter,
        s.LayerIndexFilter,
        const_ptr_viewport);
    }

    ~ObjectIterator()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CRhinoObjectIterator_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  class ObjectIteratorOfType<T> : IEnumerator<T> where T : RhinoObject
  {
    readonly ObjectIterator m_iterator;
    
    public ObjectIteratorOfType(RhinoDoc doc, ObjectEnumeratorSettings s)
    {
      m_iterator = new ObjectIterator(doc, s);
    }

    public T Current
    {
      get { return m_iterator.Current as T; }
    }

    public void Dispose()
    {
      m_iterator.Dispose();
    }

    object System.Collections.IEnumerator.Current
    {
      get { return m_iterator.Current; }
    }

    public bool MoveNext()
    {
      return m_iterator.MoveNext();
    }

    public void Reset()
    {
      m_iterator.Reset();
    }
  }

}
#endif
