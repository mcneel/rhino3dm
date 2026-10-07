#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
  /// <summary>
  /// An immutable snapshot of one model in a worksession.
  /// <para>
  /// The values are copied when the snapshot is taken, so an instance stays usable
  /// (though it may go out of date) after the worksession changes. It holds no
  /// pointer into the document.
  /// </para>
  /// </summary>
  /// <since>9.0</since>
  public sealed class WorksessionModel
  {
    internal WorksessionModel(WorksessionModelInfo info)
    {
      Path = info.FileName();
      Alias = info.Alias();
      SerialNumber = info.ModelSn();
      IsAttached = info.Attached();
      AttachFailed = info.AttachedFailed();
      UpdateAvailable = info.UpdateAvailable();
      FileTypeId = info.FileTypeId();
    }

    /// <summary>
    /// The full path to the model's file. This is empty for an active model that
    /// has never been saved.
    /// </summary>
    /// <since>9.0</since>
    public string Path { get; }

    /// <summary>
    /// The alias used to decorate names - layers, materials, and so on - that come
    /// from this model.
    /// </summary>
    /// <since>9.0</since>
    public string Alias { get; }

    /// <summary>
    /// The reference model's serial number, which identifies this model in calls
    /// like <see cref="Worksession.Detach(uint)"/>. Zero means this is the
    /// worksession's active model.
    /// <para>
    /// Reference model serial numbers are unique across the application, not just
    /// within one document, so a serial number from another document's worksession
    /// will simply not be found rather than matching the wrong model.
    /// </para>
    /// </summary>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public uint SerialNumber { get; }

    /// <summary>
    /// True if this is the worksession's active model - the document you are
    /// editing - rather than a reference model.
    /// </summary>
    /// <since>9.0</since>
    public bool IsActiveModel => 0 == SerialNumber;

    /// <summary>
    /// True if the model's contents are currently attached to the document.
    /// </summary>
    /// <since>9.0</since>
    public bool IsAttached { get; }

    /// <summary>
    /// True if the most recent attempt to attach this model failed, which usually
    /// means the file is missing.
    /// </summary>
    /// <since>9.0</since>
    public bool AttachFailed { get; }

    /// <summary>
    /// True if the file on disk has changed since it was attached, so
    /// <see cref="Worksession.Update(uint)"/> has something to do.
    /// <para>
    /// This is only filled in when the snapshot was taken with the file check
    /// enabled. <see cref="Worksession.Models"/> enables it; pass false to
    /// <see cref="Worksession.GetModels(bool)"/> to skip it, and this is then
    /// always false.
    /// </para>
    /// </summary>
    /// <since>9.0</since>
    public bool UpdateAvailable { get; }

    /// <summary>
    /// The id of the plug-in used to read the model's file.
    /// </summary>
    /// <since>9.0</since>
    public Guid FileTypeId { get; }

    /// <summary>
    /// Returns the model's alias, or its path when it has no alias.
    /// </summary>
    public override string ToString()
    {
      return string.IsNullOrEmpty(Alias) ? Path : Alias;
    }
  }

  /// <summary>
  /// Manages a list of models that are being used as reference geometry.
  /// </summary>
  public sealed class Worksession
  {
    private readonly RhinoDoc m_doc;

    internal Worksession(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>
    /// Gets the document that owns this worksession.
    /// </summary>
    /// <since>6.0</since>
    public RhinoDoc Document => m_doc;

    /// <summary>
    /// Unique serial number for the worksession while the application is running.
    /// This is not a persistent value.
    /// <para>
    /// Since Rhino 9 worksessions are per-document and no longer have serial numbers
    /// of their own, so this returns <see cref="Document"/>'s
    /// <see cref="RhinoDoc.RuntimeSerialNumber"/>. Prefer that property in new code.
    /// </para>
    /// </summary>
    /// <since>6.3</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber
    {
      get
      {
        return UnsafeNativeMethods.CRhinoWorkSession_SerialNumber(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Returns the path to the open worksession, or .rws, file. 
    /// If there is no worksession file open, or the active worksession
    /// has not yet been saved, then null is returned.
    /// </summary>
    /// <since>6.0</since>
    public string FileName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          bool rc = UnsafeNativeMethods.CRhinoWorkSession_FileName(m_doc.RuntimeSerialNumber, ptr_string);
          if (rc)
            return sh.ToString();
        }
        return null;
      }
    }

    /// <summary>
    /// Gets the name of the worksession - the worksession (.rws) file name
    /// without its path or extension. Returns an empty string if there is no
    /// worksession file open, or the active worksession has not yet been saved;
    /// it is up to the caller to decide what to display in that case.
    /// </summary>
    /// <since>9.0</since>
    public string Name
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          if (UnsafeNativeMethods.CRhinoWorkSession_Name(m_doc.RuntimeSerialNumber, ptr_string))
            return sh.ToString();
        }
        return string.Empty;
      }
    }

    /// <summary>
    /// Returns the path to the open worksession, or .rws, file.
    /// If there is no worksession file open, or the active worksession
    /// has not yet been saved, then null is returned.
    /// </summary>
    /// <param name="runtimeSerialNumber">
    /// Since Rhino 9 worksessions are per-document and no longer have serial numbers
    /// of their own, so this is a document's
    /// <see cref="RhinoDoc.RuntimeSerialNumber"/>. In new code, prefer getting the
    /// document and using its <see cref="RhinoDoc.Worksession"/>
    /// <see cref="FileName"/>.
    /// </param>
    /// <returns>The path to the .rws file, or null.</returns>
    /// <since>6.3</since>
    [CLSCompliant(false)]
    public static string FileNameFromRuntimeSerialNumber(uint runtimeSerialNumber)
    {
      using (var sh = new StringWrapper())
      {
        IntPtr ptr_string = sh.NonConstPointer;
        bool rc = UnsafeNativeMethods.CRhinoWorkSession_FilenameFromSN(runtimeSerialNumber, ptr_string);
        if (rc)
          return sh.ToString();
      }
      return null;
    }

    /// <summary>
    /// Returns the number of models in the worksession. The active model will included
    /// in this count whether or not it has been saved.
    /// </summary>
    /// <since>6.0</since>
    public int ModelCount
    {
      get
      {
        int count = 0;
        bool rc = UnsafeNativeMethods.CRhinoWorkSession_ModelCount(m_doc.RuntimeSerialNumber, ref count);
        return rc ? count : 0;
      }
    }

    /// <summary>
    /// Returns the paths to the models used by the worksession. If the active model has
    /// not been saved, then it will not be included in the output array.
    /// </summary>
    /// <since>6.0</since>
    public string[] ModelPaths
    {
      get
      {
        using (var strings = new ClassArrayString())
        {
          IntPtr ptr_strings = strings.NonConstPointer();
          int rc = UnsafeNativeMethods.CRhinoWorkSession_ModelNames(m_doc.RuntimeSerialNumber, ptr_strings);
          return rc > 0 ? strings.ToArray() : new string[0];
        }
      }
    }

    /// <summary>
    /// Returns the path to a model, used by the worksession, given a reference model serial number
    /// </summary>
    /// <param name="modelSerialNumber">The reference model serial number.</param>
    /// <returns>The path to the model if successful, null otherwise.</returns>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public string ModelPathFromSerialNumber(uint modelSerialNumber)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        bool result = UnsafeNativeMethods.CRhinoWorksession_ModelPathFromSerialNumber(m_doc.RuntimeSerialNumber, modelSerialNumber, ptr_string);
        if (result)
          return sh.ToString();
      }
      return null;
    }

    internal string[] ModelAliases
    {
      get
      {
        using (var strings = new ClassArrayString())
        {
          IntPtr ptr_strings = strings.NonConstPointer();
          int rc = UnsafeNativeMethods.CRhinoWorkSession_ModelAliases(m_doc.RuntimeSerialNumber, ptr_strings);
          return rc > 0 ? strings.ToArray() : new string[0];
        }
      }
    }

    /// <summary>
    /// The models in this worksession, including the active model.
    /// <para>
    /// <see cref="WorksessionModel.UpdateAvailable"/> is filled in, so this
    /// reports the same state the Worksessions panel shows. Use
    /// <see cref="GetModels(bool)"/> with false to skip that check.
    /// </para>
    /// </summary>
    /// <since>9.0</since>
    public WorksessionModel[] Models => GetModels(true);

    /// <summary>
    /// The models in this worksession, including the active model.
    /// </summary>
    /// <param name="checkForUpdates">
    /// True to fill in <see cref="WorksessionModel.UpdateAvailable"/> for each
    /// model. This compares each referenced file's size and last modified time
    /// against the values stored when the file was attached, which opens and
    /// stats each file once - no file contents are read, so the cost does not
    /// grow with the size of the models, but it is still one round trip per
    /// model. Pass false to skip it, for example when reading this repeatedly
    /// or when the models live on a slow network share.
    /// </param>
    /// <returns>The models in the worksession, or an empty array if there are none.</returns>
    /// <since>9.0</since>
    public WorksessionModel[] GetModels(bool checkForUpdates)
    {
      var rc = new List<WorksessionModel>();
      using (var list = new WorksessionModelInfoList(m_doc))
      {
        if (checkForUpdates)
          list.ComputeUpdateAvailable();

        int count = list.Count();
        for (int i = 0; i < count; i++)
        {
          // WorksessionModel copies the values it needs, so the info wrapper - which
          // only points into the snapshot owned by list - can go straight away.
          using (var info = list.At(i))
          {
            if (null != info)
              rc.Add(new WorksessionModel(info));
          }
        }
      }
      return rc.ToArray();
    }

    /// <summary>
    /// Attaches a model to this worksession as a reference model.
    /// </summary>
    /// <param name="path">The full path to the .3dm file to attach.</param>
    /// <returns>True if the model was attached, false otherwise.</returns>
    /// <remarks>
    /// The model is attached to this worksession's document, which need not be the
    /// active document. No user interface is displayed.
    /// </remarks>
    /// <since>9.0</since>
    public bool Attach(string path)
    {
      return UnsafeNativeMethods.CRhinoWorksession_AttachModel(m_doc.RuntimeSerialNumber, path);
    }

    /// <summary>
    /// Attaches several models to this worksession as reference models.
    /// </summary>
    /// <param name="paths">The full paths to the .3dm files to attach.</param>
    /// <returns>The number of models that were attached.</returns>
    /// <since>9.0</since>
    public int Attach(IEnumerable<string> paths)
    {
      if (null == paths)
        throw new ArgumentNullException(nameof(paths));

      int rc = 0;
      foreach (var path in paths)
      {
        if (Attach(path))
          rc++;
      }
      return rc;
    }

    /// <summary>
    /// Detaches a reference model from this worksession.
    /// </summary>
    /// <param name="modelSerialNumber">
    /// The reference model's serial number, from
    /// <see cref="WorksessionModel.SerialNumber"/>.
    /// </param>
    /// <returns>True if the model was detached, false otherwise.</returns>
    /// <remarks>
    /// The active model - serial number 0 - cannot be detached.
    /// </remarks>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public bool Detach(uint modelSerialNumber)
    {
      return Detach(new[] { modelSerialNumber });
    }

    /// <summary>
    /// Detaches several reference models from this worksession.
    /// </summary>
    /// <param name="modelSerialNumbers">
    /// The reference models' serial numbers, from
    /// <see cref="WorksessionModel.SerialNumber"/>.
    /// </param>
    /// <returns>True if at least one model was detached, false otherwise.</returns>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public bool Detach(IEnumerable<uint> modelSerialNumbers)
    {
      if (null == modelSerialNumbers)
        throw new ArgumentNullException(nameof(modelSerialNumbers));

      using (var array = new SimpleArrayUint(modelSerialNumbers))
      {
        return UnsafeNativeMethods.CRhinoWorksession_DetachModels(m_doc.RuntimeSerialNumber, array.ConstPointer());
      }
    }

    /// <summary>
    /// Re-reads a reference model whose file has changed on disk.
    /// </summary>
    /// <param name="modelSerialNumber">
    /// The reference model's serial number, from
    /// <see cref="WorksessionModel.SerialNumber"/>. Pass 0 to update every
    /// out-of-date reference model in this worksession.
    /// </param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public bool Update(uint modelSerialNumber)
    {
      return UnsafeNativeMethods.CRhinoWorksession_UpdateModel(m_doc.RuntimeSerialNumber, modelSerialNumber);
    }

    /// <summary>
    /// Re-reads every reference model in this worksession whose file has changed
    /// on disk.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>9.0</since>
    public bool UpdateAll()
    {
      return Update(0);
    }

    /// <summary>
    /// Makes one of this worksession's reference models the active model.
    /// </summary>
    /// <param name="modelSerialNumber">
    /// The reference model's serial number, from
    /// <see cref="WorksessionModel.SerialNumber"/>.
    /// </param>
    /// <returns>
    /// The document the worksession is now using, or null on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Changing the active model loads the requested model into a different
    /// document, so carry on with the returned document: treat
    /// <see cref="Document"/> and this <see cref="Worksession"/> instance as
    /// unusable once this returns.
    /// </para>
    /// <para>
    /// The document this was called on is closed as part of the switch, but on Mac
    /// that close is asynchronous and may not have finished when this returns. Do
    /// not rely on the old document either still being open or already being gone.
    /// </para>
    /// <para>
    /// This is not fully silent: if the outgoing document has unsaved changes the
    /// user is prompted to save it, and cancelling that prompt makes this return
    /// null. Save the document first if you need it to run unattended.
    /// </para>
    /// <para>
    /// Do not call this from a script that is itself running in the document being
    /// replaced. Scripts run in a document context, and closing that document from
    /// underneath the running script leaves Rhino's command machinery working with
    /// a document that no longer exists. Drive this from a command, a panel, or an
    /// idle handler instead.
    /// </para>
    /// </remarks>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public RhinoDoc SetActiveModel(uint modelSerialNumber)
    {
      uint doc_serial_number = UnsafeNativeMethods.CRhinoWorksession_SetActiveModel(m_doc.RuntimeSerialNumber, modelSerialNumber);
      return 0 == doc_serial_number ? null : RhinoDoc.FromRuntimeSerialNumber(doc_serial_number);
    }

    /// <summary>
    /// Saves this worksession to its existing .rws file.
    /// </summary>
    /// <returns>
    /// True if successful. False if the worksession has never been saved, in which
    /// case use <see cref="SaveAs(string)"/> to supply a path.
    /// </returns>
    /// <since>9.0</since>
    public bool Save()
    {
      return UnsafeNativeMethods.CRhinoWorksession_SaveFile(m_doc.RuntimeSerialNumber, null);
    }

    /// <summary>
    /// Saves this worksession to a .rws file.
    /// </summary>
    /// <param name="path">The full path to write to.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>9.0</since>
    public bool SaveAs(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return false;
      return UnsafeNativeMethods.CRhinoWorksession_SaveFile(m_doc.RuntimeSerialNumber, path);
    }

    /// <summary>
    /// Opens a worksession (.rws) file.
    /// </summary>
    /// <param name="path">The full path to the .rws file to open.</param>
    /// <returns>
    /// The document the worksession was read into, or null on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Opening a worksession is a document-level operation rather than a change to
    /// an existing worksession, which is why this is static. Always use the
    /// returned document: it may be the current document, reused, or a newly
    /// created one, depending on the platform and on whether the current document
    /// can be reused. On Windows, other than headless, the current document is
    /// closed and replaced.
    /// </para>
    /// <para>
    /// As with <see cref="SetActiveModel(uint)"/>, do not call this from a script
    /// running in a document that this might close.
    /// </para>
    /// </remarks>
    /// <since>9.0</since>
    public static RhinoDoc Open(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return null;

      var doc = RhinoDoc.ActiveDoc;
      uint doc_serial_number = UnsafeNativeMethods.CRhinoWorksession_OpenFile(null == doc ? 0 : doc.RuntimeSerialNumber, path);
      return 0 == doc_serial_number ? null : RhinoDoc.FromRuntimeSerialNumber(doc_serial_number);
    }

    /// <summary>
    /// Describes whether a worksession model's file is locked, and by whom.
    /// </summary>
    /// <param name="modelPath">The full path to the model's file.</param>
    /// <returns>
    /// A localized description of the file's lock state, or an empty string if it
    /// could not be determined.
    /// </returns>
    /// <remarks>
    /// This reads the file's lock file and does not depend on any document, which
    /// is why it is static.
    /// </remarks>
    /// <since>9.0</since>
    public static string GetLockInformation(string modelPath)
    {
      if (string.IsNullOrWhiteSpace(modelPath))
        return string.Empty;

      using (var sw = new StringWrapper(modelPath))
      using (var sh = new StringHolder())
      {
        if (UnsafeNativeMethods.CRhinoWorksession_GetLockInfo(sw.ConstPointer, sh.NonConstPointer()))
          return sh.ToString();
      }
      return string.Empty;
    }
  }

  internal class WorksessionModelInfoList : IDisposable
  {
    private readonly bool m_owner;

    public IntPtr CppPointer { get; private set; }

    public WorksessionModelInfoList(Rhino.RhinoDoc doc)
    {
      m_owner = true;
      // Builds a detached snapshot of the worksession models. This reads the
      // document (so it must run on the main thread) but does NOT perform the
      // slow per-file checksum comparison. Call ComputeUpdateAvailable - which
      // is thread safe - to fill in the UpdateAvailable flags. See RH-94507.
      CppPointer = UnsafeNativeMethods.CRhinoWorksessionModelList_New(doc.RuntimeSerialNumber);
    }

    /// <summary>
    /// Performs the (potentially slow) per-file checksum comparison that fills in
    /// each model's UpdateAvailable flag. Touches only this detached snapshot and
    /// the file system - no document access - so it is safe to call from a worker
    /// thread (e.g. Task.Run). See RH-94507.
    /// </summary>
    public void ComputeUpdateAvailable()
    {
      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoWorksessionModelList_ComputeUpdateAvailable(CppPointer);
      }
    }

    public WorksessionModelInfoList(IntPtr pArray)
    {
      m_owner = false;
      CppPointer = pArray;
    }

    ~WorksessionModelInfoList()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    void Dispose(bool bDisposing)
    {
      //if (bDisposing)
      {
        if (CppPointer != IntPtr.Zero)
        {
          if (m_owner)
          {
            UnsafeNativeMethods.CRhinoWorksessionModelList_Delete(CppPointer);
          }
          CppPointer = IntPtr.Zero;
        }
      }
    }

    public int Count()
    {
      if (CppPointer != IntPtr.Zero)
      {
        return UnsafeNativeMethods.CRhinoWorksessionModelList_Count(CppPointer);
      }

      return 0;
    }

    public WorksessionModelInfo At(int index)
    {

      if (CppPointer != IntPtr.Zero)
      {
        IntPtr p = UnsafeNativeMethods.CRhinoWorksessionModelList_At(CppPointer, index);

        return new WorksessionModelInfo(p);
      }

      return null;
    }
  }

  internal class WorksessionModelInfo : IDisposable
  {
    private readonly bool m_owner;

    public IntPtr CppPointer { get; private set; }

    public WorksessionModelInfo(IntPtr pArray)
    {
      m_owner = false;
      CppPointer = pArray;
    }

    ~WorksessionModelInfo()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    void Dispose(bool bDisposing)
    {
      {
        if (CppPointer != IntPtr.Zero)
        {
          if (m_owner)
          {
            
          }
          CppPointer = IntPtr.Zero;
        }
      }
    }

    public Guid FileTypeId()
    {
      if (CppPointer != IntPtr.Zero)
      {
        Guid id = UnsafeNativeMethods.CRhinoWorksessionModel_FileTypeId(CppPointer);

        return id;
      }

      return Guid.Empty;
    }

    public string FileName()
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoWorksessionModel_FileName(CppPointer, ptr_string);
        return sh.ToString();
      }
    }

    public string Alias()
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoWorksessionModel_Alias(CppPointer, ptr_string);
        return sh.ToString();
      }
    }

    public uint ModelSn()
    {
      return UnsafeNativeMethods.CRhinoWorksessionModel_ModelSn(CppPointer);
    }

    public bool Attached()
    {
      return UnsafeNativeMethods.CRhinoWorksessionModel_Attached(CppPointer);
    }

    public bool AttachedFailed()
    {
      return UnsafeNativeMethods.CRhinoWorksessionModel_AttachedFailed(CppPointer);
    }

    public bool UpdateAvailable()
    {
      return UnsafeNativeMethods.CRhinoWorksessionModel_UpdateAvailable(CppPointer);
    }
  }
}

#endif
