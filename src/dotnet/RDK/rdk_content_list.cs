#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhino.Render
{
  interface IRenderContentTable<TContentType>
  {
    bool Add(TContentType content);
    bool Remove(TContentType content);
  }

  public sealed class RenderMaterialTable : IRenderContentTable<RenderMaterial>, IEnumerable<RenderMaterial>, Collections.IRhinoTable<RenderMaterial>
  {
    internal RenderMaterialTable (RhinoDoc doc)
    {
      m_impl = new RenderContentTableImpl<RenderMaterial>(doc);
    }

    public bool Add(RenderMaterial c)
    {
      return m_impl.InternalContentList.Add(c);
    }

    public bool Remove(RenderMaterial c)
    {
      return m_impl.InternalContentList.Remove(c);
    }

    readonly RenderContentTableImpl<RenderMaterial> m_impl;

    #region IEnumerable required
    public IEnumerator<RenderMaterial> GetEnumerator()
    {
      return new ContentList<RenderMaterial>(m_impl.Document);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion IEnumerable required

    #region IRhinoTable required

    public int Count { get { return m_impl.InternalContentList.Count; } }

    public RenderMaterial this[int index] { get { return m_impl.InternalContentList[index]; } }

    #endregion IRhinoTable required
  }

  public sealed class RenderEnvironmentTable : IRenderContentTable<RenderEnvironment>, IEnumerable<RenderEnvironment>, Collections.IRhinoTable<RenderEnvironment>
  {
    internal RenderEnvironmentTable(RhinoDoc doc)
    {
      m_impl = new RenderContentTableImpl<RenderEnvironment>(doc);
    }

    public bool Add(RenderEnvironment c)
    {
      return m_impl.InternalContentList.Add(c);
    }

    public bool Remove(RenderEnvironment c)
    {
      return m_impl.InternalContentList.Remove(c);
    }

    readonly RenderContentTableImpl<RenderEnvironment> m_impl;

    #region IEnumerable required
    public IEnumerator<RenderEnvironment> GetEnumerator()
    {
      return new ContentList<RenderEnvironment>(m_impl.Document);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion IEnumerable required

    #region IRhinoTable required

    public int Count { get { return m_impl.InternalContentList.Count; } }

    public RenderEnvironment this[int index] { get { return m_impl.InternalContentList[index]; } }

    #endregion IRhinoTable required
  }

  public sealed class RenderTextureTable : IRenderContentTable<RenderTexture>, IEnumerable<RenderTexture>, Collections.IRhinoTable<RenderTexture>
  {
    internal RenderTextureTable(RhinoDoc doc)
    {
      m_impl = new RenderContentTableImpl<RenderTexture>(doc);
    }

    public bool Add(RenderTexture c)
    {
      return m_impl.InternalContentList.Add(c);
    }

    public bool Remove(RenderTexture c)
    {
      return m_impl.InternalContentList.Remove(c);
    }

    readonly RenderContentTableImpl<RenderTexture> m_impl;

    #region IEnumerable required
    public IEnumerator<RenderTexture> GetEnumerator()
    {
      return new ContentList<RenderTexture>(m_impl.Document);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion IEnumerable required

    #region IRhinoTable required

    public int Count { get { return m_impl.InternalContentList.Count; } }

    public RenderTexture this[int index] { get { return m_impl.InternalContentList[index]; } }

    #endregion IRhinoTable required
  }

  
  internal class RenderContentTableImpl<T> where T : RenderContent
  {
    internal RenderContentTableImpl(RhinoDoc rhinoDoc)
    {
      m_rhino_doc = rhinoDoc;
    }

    private readonly RhinoDoc m_rhino_doc;
    internal RhinoDoc Document { get { return m_rhino_doc; } }

    private ContentList<T> m_internal_content_list;
    internal ContentList<T> InternalContentList
    {
      get { return (m_internal_content_list ?? (m_internal_content_list = new ContentList<T>(Document))); }
    }
  }

  internal static class RenderContentTableEventForwarder
  {
    #region events

    internal delegate void ContentListClearingCallback(int kind, uint docSerialNumber);
    internal delegate void ContentListClearedCallback(int kind, uint docSerialNumber);
    internal delegate void ContentListLoadedCallback(int kind, uint docSerialNumber);
    internal delegate void MaterialAssigmentChangedCallback(uint docSerialNumber, Guid objectOrLayerId, Guid newMaterialId, Guid oldMaterialId);

    public class RenderContentTableEventArgs : EventArgs
    {
      internal RenderContentTableEventArgs(RhinoDoc document, RenderContentKind kind)
      {
        m_rhino_doc = document;
        m_content_kind = kind;
      }

      public RhinoDoc Document { get { return m_rhino_doc; } }
      public RenderContentKind ContentKind { get { return m_content_kind; } }

      private readonly RhinoDoc m_rhino_doc;
      private readonly RenderContentKind m_content_kind;
    }

    #endregion
  }

  /// <summary>
  /// Base class that provides access to the document lists of RenderContent instances
  /// ie - the Material, Environment and Texture tables.
  /// </summary>
  internal class ContentList<TContentType> : IRenderContentTable<TContentType>, IEnumerator<TContentType>, IEnumerable<TContentType> where TContentType : RenderContent
  {
    readonly RenderContentKind m_kind;
    readonly RhinoDoc m_doc;

    internal ContentList(RhinoDoc doc)
    {
      if (typeof(TContentType) == typeof(RenderMaterial))
      {
        m_kind = RenderContentKind.Material;
      }
      else if (typeof(TContentType) == typeof(RenderEnvironment))
      {
        m_kind = RenderContentKind.Environment;
      }
      else if (typeof(TContentType) == typeof(RenderTexture))
      {
        m_kind = RenderContentKind.Texture;
      }
      else
      {
        Debug.Assert(false, "Type unknown");
      }

      m_doc = doc;
    }
    /// <summary>
    /// The number of top level content objects in this list.
    /// </summary>
    public int Count
    {
      get
      {
        return UnsafeNativeMethods.Rdk_ContentList_Count(ConstPointer());
      }
    }

    public TContentType this[int index]
    {
      get
      {
        var i = 0;
        foreach (var content in this)
        {
          if (i == index) return content;
          i++;
        }
        return null;
      }
    }

    public bool Add(TContentType content)
    {
      Debug.Assert(content.AutoDelete);
      bool bRet = UnsafeNativeMethods.Rdk_Doc_AttachContent(m_doc.RuntimeSerialNumber, content.NonConstPointer());
      if (bRet)
      {
        content.AutoDelete = false;

        var native = content as INativeContent;
        if (native != null)
        {
          native.Document = UnsafeNativeMethods.Rdk_Doc_FromRhinoDocSerial(m_doc.RuntimeSerialNumber);
        }
      }
      return bRet;
    }

    public bool Remove(TContentType content)
    {
      Debug.Assert(false == content.AutoDelete);
      Debug.Assert(content.Document.RuntimeSerialNumber == m_doc.RuntimeSerialNumber);

      bool bRet = UnsafeNativeMethods.Rdk_Doc_DetachContent(m_doc.RuntimeSerialNumber, content.NonConstPointer());
      if (bRet)
      {
        content.AutoDelete = true;
        var native = content as INativeContent;
        if (native != null)
        {
          native.Document = Guid.Empty;
        }
      }

      return bRet;
    }

    /// <summary>
    /// The unique identifier for this list.
    /// </summary>
    public Guid Id
    {
      get
      {
        return UnsafeNativeMethods.Rdk_ContentList_Uuid(ConstPointer());
      }
    }

    /// <summary>
    /// Finds a content by its instance id.
    /// </summary>
    /// <param name="instanceId">Instance id of the content to find.</param>
    /// <param name="includeChildren">Specifies if children should be searched as well as top-level content.</param>
    /// <returns>The found content or null.</returns>
    public TContentType FindInstance(Guid instanceId, bool includeChildren)
    {
      var content_pointer = UnsafeNativeMethods.Rdk_ContentList_FindInstance(ConstPointer(), instanceId, includeChildren);
      if (IntPtr.Zero == content_pointer)
        return null;
      var content = RenderContent.FromPointer(content_pointer);
      return content as TContentType;
    }

    #region IEnumerator implemenation
    private IntPtr m_pIterator = IntPtr.Zero;
    private RenderContent m_content;
    public void Reset()
    {
        UnsafeNativeMethods.Rdk_ContentLists_DeleteIterator(m_pIterator);
        m_pIterator = UnsafeNativeMethods.Rdk_ContentLists_NewIterator(ConstPointer());
    }
    public TContentType Current
    {
      get
      {
        return m_content as TContentType;
      }
    }
    object IEnumerator.Current { get { return Current; } }
    public bool MoveNext()
    {
      if (m_pIterator == IntPtr.Zero)
      {
        m_pIterator = UnsafeNativeMethods.Rdk_ContentLists_NewIterator(ConstPointer());
      }

      IntPtr pContent = UnsafeNativeMethods.Rdk_ContentLists_Next(m_pIterator);
      if (IntPtr.Zero == pContent)
      {
        m_content = null;
        return false;
      }
      m_content = RenderContent.FromPointer(pContent);
      return true;
    }
    #endregion

    #region internals
    internal IntPtr ConstPointer()
    {
      return UnsafeNativeMethods.Rdk_ContentList_ListFromKind(RenderContent.KindString(m_kind), m_doc.RuntimeSerialNumber);
    }
    #endregion

    #region IDisposable

    public IEnumerator<TContentType> GetEnumerator()
    {
      return this;
    }

    ~ContentList()
    {
      Dispose(false);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
      UnsafeNativeMethods.Rdk_ContentLists_DeleteIterator(m_pIterator);
    }
    #endregion

  }
}

#endif

