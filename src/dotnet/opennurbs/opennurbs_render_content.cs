
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.FileIO
{
  internal class File3dmRenderContentEnumerator<T> : IEnumerator<T>
  {
    private readonly ManifestTable _manifest;
    protected IEnumerator<ModelComponent> _manifest_enumerator;

    public T Current { get; private set; }
    object IEnumerator.Current { get => Current; }

    public File3dmRenderContentEnumerator(File3dm f)
    {
      _manifest = f.Manifest;
      Reset();
    }

    public void Reset()
    {
      _manifest_enumerator = _manifest.GetEnumerator(ModelComponentType.RenderContent);
    }

    public bool MoveNext()
    {
      while (_manifest_enumerator.MoveNext())
      {
        if (_manifest_enumerator.Current is T curr)
        {
          Current = curr;
          return true;
        }
      }

      return false;
    }

    public void Dispose() { Dispose(true); }
    protected void Dispose(bool b) { }
  }

  /// <summary></summary>
  public class File3dmRenderMaterials : IEnumerable<File3dmRenderMaterial>
  {
    private readonly File3dm _file3dm;

    /// <summary></summary>
    /// <since>8.0</since>
    public File3dmRenderMaterials(File3dm f) { _file3dm = f; }

    /// <summary></summary>
    /// <since>8.0</since>
    public IEnumerator<File3dmRenderMaterial> GetEnumerator()
    {
      return new File3dmRenderContentEnumerator<File3dmRenderMaterial>(_file3dm);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  /// <summary></summary>
  public class File3dmRenderEnvironments : IEnumerable<File3dmRenderEnvironment>
  {
    private readonly File3dm _file3dm;

    /// <summary></summary>
    /// <since>8.0</since>
    public File3dmRenderEnvironments(File3dm f) { _file3dm = f; }

    /// <summary></summary>
    /// <since>8.0</since>
    public IEnumerator<File3dmRenderEnvironment> GetEnumerator()
    {
      return new File3dmRenderContentEnumerator<File3dmRenderEnvironment>(_file3dm);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  /// <summary></summary>
  public class File3dmRenderTextures : IEnumerable<File3dmRenderTexture>
  {
    private readonly File3dm _file3dm;

    /// <summary></summary>
    /// <since>8.0</since>
    public File3dmRenderTextures(File3dm f) { _file3dm = f; }

    /// <summary></summary>
    /// <since>8.0</since>
    public IEnumerator<File3dmRenderTexture> GetEnumerator()
    {
      return new File3dmRenderContentEnumerator<File3dmRenderTexture>(_file3dm);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  /// <summary/>
  public abstract class File3dmRenderContent : ModelComponent
  {
    #region members
    readonly Guid m_id = Guid.Empty;
    #endregion

    internal File3dmRenderContent(IntPtr p)
    {
      ConstructNonConstObject(p);
    }

    /// <summary>
    /// Constructor for when the object is top-level.
    /// </summary>
    internal File3dmRenderContent(Guid id, File3dm parent)
    {
      m_id = id;
      m__parent = parent;
    }

    /// <summary>
    /// Constructor for when the object is a child.
    /// </summary>
    internal File3dmRenderContent(Guid id, File3dmRenderContent parent)
    {
      m_id = id;
      m__parent = parent;
    }

    /// <summary>
    /// <return>the kind of render content as a string.</return>
    /// </summary>
    /// <since>8.0</since>
    public string Kind
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_RenderContent_Kind(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>The internal name of the content type.</return>
    /// </summary>
    /// <since>8.0</since>
    public string TypeName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_RenderContent_TypeName(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>The unique id of the content type.</return>
    /// </summary>
    /// <since>8.0</since>
    public Guid TypeId => UnsafeNativeMethods.ON_RenderContent_TypeId(ConstPointer());

    /// <summary>
    /// <return>The content's render-engine id.</return>
    /// </summary>
    /// <since>8.0</since>
    public Guid RenderEngineId => UnsafeNativeMethods.ON_RenderContent_RenderEngineId(ConstPointer());

    /// <summary>
    /// <return>The content's plug-in id.</return>
    /// </summary>
    /// <since>8.0</since>
    public Guid PlugInId => UnsafeNativeMethods.ON_RenderContent_PlugInId(ConstPointer());

    /// <summary>
    /// <return>The content's group id.</return>
    /// </summary>
    /// <since>8.0</since>
    public Guid GroupId => UnsafeNativeMethods.ON_RenderContent_GroupId(ConstPointer());

    /// <summary>
    /// <return>The content's notes.</return>
    /// </summary>
    /// <since>8.0</since>
    public string Notes
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_RenderContent_Notes(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>The content's tags.</return>
    /// </summary>
    /// <since>8.0</since>
    public string Tags
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_RenderContent_Tags(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>True if the content is hidden.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool Hidden => UnsafeNativeMethods.ON_RenderContent_Hidden(ConstPointer());

    /// <summary>
    /// <return>True if the content is a reference content.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool Reference => UnsafeNativeMethods.ON_RenderContent_Reference(ConstPointer());

    /// <summary>
    /// <return>True if the content is automatically deleted when not in use.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool AutoDelete => UnsafeNativeMethods.ON_RenderContent_AutoDelete(ConstPointer());

    /// <summary>
    /// Gets a named parameter.
    /// <return>The parameter value or null if not found.</return>
    /// </summary>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public IConvertible GetParameter(string param)
    {
      var v = new Rhino.Render.Variant();
      if (!UnsafeNativeMethods.ON_RenderContent_GetParameter(ConstPointer(), param, v.NonConstPointer()))
        return null;

      return v;
    }

    /// <summary>
    /// Sets a named parameter.
    /// <return>True if the parameter was set, else false.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool SetParameter(string param, object value)
    {
      using (var v = new Rhino.Render.Variant(value))
      {
        // I have to use ConstPointer here. It's a hack to prevent a copy being made. If a copy is
        // made, the changes get written to the copy and subsequently lost.
        return UnsafeNativeMethods.ON_RenderContent_SetParameter(ConstPointer(), param, v.ConstPointer());
      }
    }

    /// <summary>
    /// <return>The parent File3dm of the entire hierarchy.</return>
    /// </summary>
    /// <since>8.0</since>
    public File3dm File3dmParent
    {
      get
      {
        if (m__parent is File3dm parent)
        {
          return parent;
        }

        if (Parent != null)
        {
          return Parent.File3dmParent;
        }

        return null;
      }
    }

    /// <summary>
    /// <return>The parent content or null if this is the top level object.</return>
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderContent Parent
    {
      get
      {
        if (m__parent is File3dmRenderContent parent)
        {
          return parent;
        }

        return null;
      }
    }

    /// <summary>
    /// <return>The top-level parent content. Returns this if this is the top-level item.</return>
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderContent TopLevel
    {
      get
      {
        if (m__parent is File3dmRenderContent parent)
        {
          return parent;
        }

        return null;
      }
    }

    internal static File3dmRenderContent NewFile3dmRenderContent(File3dmRenderContent parent, Guid child_id)
    {
      var file3dm = parent.File3dmParent;
      if (file3dm != null)
      {
        IntPtr model = file3dm.ConstPointer();

        switch (UnsafeNativeMethods.ONX_Model_GetFile3dmRenderContentKind(model, child_id))
        {
        case 0: return new File3dmRenderMaterial(child_id, parent);
        case 1: return new File3dmRenderEnvironment(child_id, parent);
        case 2: return new File3dmRenderTexture(child_id, parent);
        }
      }

      return null;
    }

    /// <summary/>
    /// <since>8.0</since>
    public IEnumerable<File3dmRenderContent> Children
    {
      get
      {
        using (var children = new SimpleArrayGuid())
        {
          UnsafeNativeMethods.ON_RenderContent_Children(ConstPointer(), children.NonConstPointer());

          for (int i = 0; i < children.Count; i++)
          {
            var child = NewFile3dmRenderContent(this, children[i]);
            if (child != null)
            {
              yield return child;
            }
          }
        }
      }
    }

    /// <summary>
    /// <return>True if this is a top-level render content (i.e., has no parent; is not a child).</return>
    /// </summary>
    /// <since>8.0</since>
    public bool IsTopLevel => UnsafeNativeMethods.ON_RenderContent_IsTopLevel(ConstPointer());

    /// <summary>
    /// <return>True if this is a child of another render content (i.e., has a parent; is not top-level).</return>
    /// </summary>
    /// <since>8.0</since>
    public bool IsChild => UnsafeNativeMethods.ON_RenderContent_IsChild(ConstPointer());

    /// <summary>
    /// <return>The render content's child slot name.</return>
    /// </summary>
    /// <since>8.0</since>
    public string ChildSlotName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_RenderContent_ChildSlotName(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>True if a particular child slot is 'on'.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool ChildSlotOn(string child_slot_name)
    {
      return UnsafeNativeMethods.ON_RenderContent_ChildSlotOn(ConstPointer(), child_slot_name);
    }

    /// <summary>
    /// Deletes any existing child with the specified child slot name.
    /// <return>True if successful, else false.</return>
    /// </summary>
    /// <since>8.0</since>
    public bool DeleteChild(string child_slot_name)
    {
      return UnsafeNativeMethods.ON_RenderContent_DeleteChild(ConstPointer(), child_slot_name);
    }

    /// <summary>
    /// <return>The child with the specified child slot name, or null if no such child exists.</return>
    /// </summary>
    /// <since>8.0</since>
    public File3dmRenderContent FindChild(string child_slot_name)
    {
     var child_id = UnsafeNativeMethods.ON_RenderContent_FindChild(ConstPointer(), child_slot_name);
     return NewFile3dmRenderContent(this, child_id);
    }

    /// <summary>
    /// Gets the render content's state as an XML string.
    /// </summary>
    /// <since>8.0</since>
    public string XML(bool recursive)
    {
      using (var sw = new StringWrapper())
      {
        UnsafeNativeMethods.ON_RenderContent_XML(ConstPointer(), recursive, sw.NonConstPointer);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.RenderContent"/>.
    /// </summary>
    /// <since>8.0</since>
    public override ModelComponentType ComponentType
    {
      get { return ModelComponentType.RenderContent; }
    }

    /// <summary/>
    internal override IntPtr _InternalGetConstPointer()
    {
      var file3dm = File3dmParent;
      if (file3dm == null)
        return IntPtr.Zero;

      var model = file3dm.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_FindRenderContentFromId(model, m_id);
    }
  }

  /// <summary/>
  public class File3dmRenderMaterial : File3dmRenderContent
  {
    ///// <summary/>
    //public File3dmRenderMaterial() : base(UnsafeNativeMethods.ON_RenderMaterial3dm_New())
    //{ // This is for adding new ones -- maybe
    //}

    /// <summary>
    /// Constructor for when the object is top-level.
    /// </summary>
    internal File3dmRenderMaterial(Guid id, File3dm parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Constructor for when the object is a child.
    /// </summary>
    internal File3dmRenderMaterial(Guid id, File3dmRenderContent parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Get a simulated material that approximates this material's appearance.
    /// </summary>
    /// <since>8.0</since>
    public Material ToMaterial()
    {
      var m = new Material();
      UnsafeNativeMethods.ON_RenderMaterial_To_ON_Material(ConstPointer(), m.NonConstPointer());
      return m;
    }
  }

  /// <summary/>
  public class File3dmRenderEnvironment: File3dmRenderContent
  {
    /// <summary>
    /// Constructor for when the object is top-level.
    /// </summary>
    internal File3dmRenderEnvironment(Guid id, File3dm parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Constructor for when the object is a child.
    /// </summary>
    internal File3dmRenderEnvironment(Guid id, File3dmRenderContent parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Get a simulated environment that approximates this environment's appearance.
    /// </summary>
    /// <since>8.0</since>
    public Rhino.DocObjects.Environment ToEnvironment()
    {
      var e = new Rhino.DocObjects.Environment();
      UnsafeNativeMethods.ON_RenderEnvironment_To_ON_Environment(ConstPointer(), e.NonConstPointer());
      return e;
    }
  }

  /// <summary/>
  public class File3dmRenderTexture : File3dmRenderContent
  {
    /// <summary>
    /// Constructor for when the object is top-level.
    /// </summary>
    internal File3dmRenderTexture(Guid id, File3dm parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Constructor for when the object is a child.
    /// </summary>
    internal File3dmRenderTexture(Guid id, File3dmRenderContent parent) : base(id, parent)
    {
    }

    /// <summary>
    /// Get a simulated texture that approximates this texture's appearance.
    /// </summary>
    /// <since>8.0</since>
    public Texture ToTexture()
    {
      var t = new Texture();
      UnsafeNativeMethods.ON_RenderTexture_To_ON_Texture(ConstPointer(), t.NonConstPointer());
      return t;
    }
  }

}
