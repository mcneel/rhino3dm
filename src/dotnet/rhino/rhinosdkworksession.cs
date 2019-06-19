#if RHINO_SDK
using System;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
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
    public RhinoDoc Document => m_doc;

    /// <summary>
    /// Unique serial number for the worksession while the application is running.
    /// This is not a persistent value.
    /// </summary>
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
    /// Returns the path to the open worksession, or .rws, file. 
    /// If there is no worksession file open, or the active worksession
    /// has not yet been saved, then null is returned.
    /// </summary>
    /// <param name="runtimeSerialNumber"></param>
    /// <returns></returns>
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
  }
}

#endif