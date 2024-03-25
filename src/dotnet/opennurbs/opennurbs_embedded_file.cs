
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.FileIO
{
  internal class File3dmEmbeddedFileEnumerator : IEnumerator<File3dmEmbeddedFile>
  {
    private readonly ManifestTable _manifest;
    private IEnumerator<ModelComponent> _manifest_enumerator;

    public File3dmEmbeddedFile Current { get; private set; }
    object IEnumerator.Current { get => Current; }

    public File3dmEmbeddedFileEnumerator(File3dm f)
    {
      _manifest = f.Manifest;
      Reset();
    }

    public void Reset()
    {
      _manifest_enumerator = _manifest.GetEnumerator(ModelComponentType.EmbeddedFile);
    }

    public bool MoveNext()
    {
      if (!_manifest_enumerator.MoveNext())
        return false;

      Current = _manifest_enumerator.Current as File3dmEmbeddedFile;

      return true;
    }

    public void Dispose() { Dispose(true); }
    protected void Dispose(bool b) { }
  }

  /// <summary></summary>
  public class File3dmEmbeddedFiles : IEnumerable<File3dmEmbeddedFile>
  {
    private readonly File3dm _file3dm;

    /// <summary>
    /// Add a new embedded file and load it from a local file.
    /// </summary>
    /// <since>8.0</since>
    public bool Add(string filename)
    {
      return UnsafeNativeMethods.ONX_Model_AddEmbeddedFile(_file3dm.NonConstPointer(), filename);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    public File3dmEmbeddedFiles(File3dm f) { _file3dm = f; }

    /// <summary></summary>
    /// <since>8.0</since>
    public IEnumerator<File3dmEmbeddedFile> GetEnumerator()
    {
      return new File3dmEmbeddedFileEnumerator(_file3dm);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  /// <since>8.0</since>
  public sealed class File3dmEmbeddedFile : ModelComponent
  {
    readonly Guid m_id = Guid.Empty;

    /// <summary/>
    /// <since>8.0</since>
    public File3dmEmbeddedFile()
    {
      IntPtr pEF = UnsafeNativeMethods.ON_EmbeddedFile_New();
      ConstructNonConstObject(pEF);
    }

    internal File3dmEmbeddedFile(IntPtr ef)
    {
      ConstructNonConstObject(ef);
    }

    internal File3dmEmbeddedFile(Guid id, File3dm parent)
    {
      m_id = id;
      m__parent = parent;
    }

    /// <summary>
    /// <return>the fully-qualified filename of the embedded file. This filename may or may not refer to a
    /// local file depending on the way the embedded file was loaded. For example, if it was loaded from an
    /// archive, the filename could be that of a file on a different computer.</return>
    /// </summary>
    /// <since>8.0</since>
    public string Filename
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_EmbeddedFile_GetFilename(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// Saves the contents of the embedded file to a local file.
    /// Returns true if successful, else false.
    /// </summary>
    /// <since>8.0</since>
    public bool SaveToFile(string filename)
    {
      if (!UnsafeNativeMethods.ON_EmbeddedFile_SaveToFile(NonConstPointer(), filename))
        return false;

      return true;
    }

    /// <summary>
    /// Creates an <see cref="ModelComponentType.EmbeddedFile"/> from reading a file from disk.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static public File3dmEmbeddedFile Read(string path) 
    {
      IntPtr ptr = UnsafeNativeMethods.ON_EmbeddedFile_Read(path);
      return new File3dmEmbeddedFile(ptr);
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.EmbeddedFile"/>.
    /// </summary>
    /// <since>8.0</since>
    public override ModelComponentType ComponentType
    {
      get { return ModelComponentType.EmbeddedFile; }
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      if (m__parent is File3dm parent)
      {
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(parent.NonConstPointer(), m_id);
      }

      return IntPtr.Zero;
    }
  }
}
