
using System;
using Rhino.Runtime.InteropWrappers;
using Rhino.DocObjects;

namespace Rhino.FileIO
{
  /// <summary/>
  public class File3dmPostEffect : ModelComponent
  {
    #region members
    readonly Guid m_id = Guid.Empty;
    #endregion

    internal File3dmPostEffect(IntPtr p)
    {
      ConstructNonConstObject(p);
    }

    /// <summary>
    /// </summary>
    internal File3dmPostEffect(File3dm parent, Guid id)
    {
      m_id = id;
      m__parent = parent;
    }

    /// <summary>The types of a post effect.</summary>
    public enum Types
    {
      /// <summary>Unset type</summary>
      None,
      /// <summary>Early; executed first.</summary>
      Early,
      /// <summary>Tone Mapping; executed second.</summary>
      ToneMapping,
      /// <summary>Late; executed last.</summary>
      Late
    };

    /// <summary>
    /// <return>the type of the post effect.</return>
    /// </summary>
    public Types Type
    {
      get => (Types)UnsafeNativeMethods.ON_PostEffect_Type(ConstPointer());
    }

    /// <summary>
    /// <return>The localized name of the post effect.</return>
    /// </summary>
    public string LocalName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          UnsafeNativeMethods.ON_PostEffect_LocalName(ConstPointer(), sw.NonConstPointer);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// <return>true if the post effect is visible to the user. For early and late post effects,
    /// this is equivalent to 'shown'. For tone-mapping post effects, this is equivalent
    /// to 'selected'.</return>
    /// </summary>
    public bool Visible => UnsafeNativeMethods.ON_PostEffect_Visible(ConstPointer());

    /// <summary>
    /// <return>true if the post effect is visible to the user. For early and late post effects,
    /// this is equivalent to 'shown' and 'on'. For tone-mapping post effects, this is equivalent
    /// to 'selected'.</return>
    /// </summary>
    public bool Active => UnsafeNativeMethods.ON_PostEffect_Active(ConstPointer());

#if RHINO_SDK
    /// <summary>
    /// Get a named parameter.
    /// <return>The parameter value or null if not found.</return>
    /// </summary>
    [CLSCompliant(false)]
    public IConvertible GetParameter(string param)
    {
      var v = new Rhino.Render.Variant();
      if (!UnsafeNativeMethods.ON_PostEffect_GetParameter(ConstPointer(), param, v.NonConstPointer()))
        return null;

      return v;
    }

    /// <summary>
    /// Sets a named parameter.
    /// <return>True if the parameter was set, else false.</return>
    /// </summary>
    public bool SetParameter(string param, object value)
    {
      using (var v = new Rhino.Render.Variant(value))
      {
        return UnsafeNativeMethods.ON_PostEffect_SetParameter(ConstPointer(), param, v.ConstPointer());
      }
    }
#endif

    /// <summary>
    /// <return>The parent File3dm of the entire hierarchy.</return>
    /// </summary>
    public File3dm File3dmParent
    {
      get
      {
        if (m__parent is File3dm parent)
        {
          return parent;
        }

        return null;
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.PostEffect"/>.
    /// </summary>
    public override ModelComponentType ComponentType
    {
      get { return ModelComponentType.PostEffect; }
    }

    /// <summary/>
    internal override IntPtr _InternalGetConstPointer()
    {
      var file3dm = File3dmParent;
      if (file3dm == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ONX_Model_FindPostEffectFromId(file3dm.NonConstPointer(), m_id);
    }
  }

}
