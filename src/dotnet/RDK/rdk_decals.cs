
#pragma warning disable 1591

using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Rhino.Render
{
  internal class DecalReadOnlyException : InvalidOperationException
  {
    internal DecalReadOnlyException() : base("Can't modify read-only decal collection") { }
  };

  internal class DecalDocumentException : InvalidOperationException
  {
    internal DecalDocumentException() : base("This method requires the decal to be associated with a document") { }
  };

  /// <since>5.10</since>
  public enum DecalMapping : int
  {
    /// <summary>
    /// Planar mapping. Uses projection, origin, up and across vectors (not unitized).
    /// </summary>
    Planar = (int)UnsafeNativeMethods.ON_DecalMapping.Planar,
    /// <summary>
    /// Cylindrical mapping. Uses origin, up, across, height, radius, latitude start and stop.
    /// </summary>
    Cylindrical = (int)UnsafeNativeMethods.ON_DecalMapping.Cylindrical,
    /// <summary>
    /// Spherical mapping. Uses origin, up, across, radius, latitude/longitude start and stop.
    /// </summary>
    Spherical = (int)UnsafeNativeMethods.ON_DecalMapping.Spherical,
    /// <summary>
    /// UV mapping.
    /// </summary>
    UV = (int)UnsafeNativeMethods.ON_DecalMapping.UV
  }

  /// <since>5.10</since>
  public enum DecalProjection : int
  {
    /// <summary>No projection</summary>
    None = (int)UnsafeNativeMethods.ON_DecalProjection.None,
    /// <summary>Project forward</summary>
    Forward = (int)UnsafeNativeMethods.ON_DecalProjection.Forward,
    /// <summary>Project backward</summary>
    Backward = (int)UnsafeNativeMethods.ON_DecalProjection.Backward,
    /// <summary>Project forward and backward</summary>
    Both = (int)UnsafeNativeMethods.ON_DecalProjection.Both
  }

  /// <summary>
  /// Represents a decal, or a picture that can be moved on an object.
  /// </summary>
  public class Decal : IDisposable
  {
    private IntPtr m_decal_sp = IntPtr.Zero;      // Pointer to shared_ptr to ON_Decal.
    private readonly uint m_rhino_doc_serial = 0; // Serial number of document (if applicable).
    private IntPtr m_object_attr = IntPtr.Zero;   // Attributes for holding decal user data / XML.

    // Forces a reference to the decal enumerator to stick around until this object is GCd.
    private readonly DecalEnumerator m_decals;

    internal Decal(IntPtr decal_sp)
    {
      m_decal_sp = decal_sp;
    }

    internal Decal(IntPtr attr, uint decal_crc)
    {
      m_object_attr = attr;
      m_decal_sp = UnsafeNativeMethods.Rdk_Decal_NewDecalSharedPtrFromObjectAttributes(attr, decal_crc);
    }

    internal Decal(IntPtr decal_sp, DecalEnumerator decals, uint doc_sn)
    {
      // The document is only needed for rendering, specifically for calling Decal.TextureRenderCRC.
      m_rhino_doc_serial = doc_sn;

      m_decal_sp = UnsafeNativeMethods.SharedPtr_ON_Decal_Copy(decal_sp);
      m_decals = decals;
    }

    ~Decal() { Dispose(false); }

    /// <since>5.10</since>
    public void Dispose()
    {
      Dispose(true);
    }

    /// <since>5.10</since>
    public void Dispose(bool isDisposing)
    {
      if (m_object_attr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_Delete(m_object_attr);
        m_object_attr = IntPtr.Zero;
      }

      UnsafeNativeMethods.SharedPtr_ON_Decal_Delete(m_decal_sp);
      GC.KeepAlive(this);

      m_decal_sp = IntPtr.Zero;
    }

    /// <since>5.10</since>
    static public Decal Create(DecalCreateParams createParams)
    {
      var create_params = UnsafeNativeMethods.Rdk_DecalCreateParams_New();
      if (create_params == IntPtr.Zero)
        return null;

      var cp = createParams;
      var origin = cp.Origin;
      var up     = cp.VectorUp;
      var across = cp.VectorAcross;
      UnsafeNativeMethods.Rdk_DecalCreateParams_Set(create_params,
                   cp.TextureInstanceId, (int)cp.DecalMapping, (int)cp.DecalProjection,
                   cp.MapToInside, cp.Transparency, ref origin, ref up, ref across, cp.Height, cp.Radius,
                   cp.StartLatitude, cp.EndLatitude, cp.StartLongitude, cp.EndLongitude,
                   cp.MinU, cp.MinV, cp.MaxU, cp.MaxV);

      var attr = UnsafeNativeMethods.ON_3dmObjectAttributes_New(IntPtr.Zero);

      // create_params is actually an ON_Decal.
      var decal_crc = UnsafeNativeMethods.Rdk_Decal_AddDecal(create_params, attr);

      UnsafeNativeMethods.Rdk_DecalCreateParams_Delete(create_params);

      GC.KeepAlive(createParams);

      if (0 == decal_crc)
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_Delete(attr);
        return null;
      }

      return new Decal(attr, decal_crc);
    }

    /// <summary>
    /// The decal CRC identifies a decal by its state. Multiple decals which would be
    /// exactly the same would have the same CRC and are culled from the system.
    /// If you store this value with the intention of using it to find the decal again
    /// later, you must update your stored value whenever the decal state changes.
    /// You can detect when a decal changes by watching for the OnUserDataTransformed event.
    /// </summary>
    /// <since>6.0</since>
    public int CRC
    {
      get
      {
        var ret = (int)UnsafeNativeMethods.ON_Decal_DecalCRC(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the texture ID for this decal.
    /// </summary>
    /// <since>5.10</since>
    public Guid TextureInstanceId
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_TextureInstanceId(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// This method is deprecated in favor of TextureRenderHash below.
    /// </summary>
    /// <since>7.0</since>
    /// <deprecated>8.0</deprecated>
    [CLSCompliant(false)]
    [Obsolete("Do not use")]
    public uint TextureRenderCRC(TextureRenderHashFlags rh)
    {
      var ret = UnsafeNativeMethods.Rdk_ON_Decal_TextureRenderCRC(
                                 m_rhino_doc_serial, ConstPointer(), (ulong)rh, IntPtr.Zero);
      GC.KeepAlive(this);
      return ret;
    }

    /// <summary>
    /// This method is deprecated in favor of TextureRenderHash below.
    /// </summary>
    /// <since>7.0</since>
    /// <deprecated>8.0</deprecated>
    [CLSCompliant(false)]
    [Obsolete("Do not use")]
    public uint TextureRenderCRC(TextureRenderHashFlags rh, LinearWorkflow lw)
    {
      var ret = UnsafeNativeMethods.Rdk_ON_Decal_TextureRenderCRC(
                                 m_rhino_doc_serial, ConstPointer(), (ulong)rh, lw.CppPointer);
      GC.KeepAlive(this);
      GC.KeepAlive(lw);
      return ret;
    }

    /// <summary>
    /// Get the texture render hash for the referenced texture using the specified CrcRenderHashFlags.
    /// </summary>
    /// <since>8.0</since>
    /// <deprecated>8.5</deprecated>
    [CLSCompliant(false)]
    [Obsolete("Do not use")]
    public uint TextureRenderHash(CrcRenderHashFlags flags)
    {
      if (0 == m_rhino_doc_serial)
        throw new DecalDocumentException();

      var ret = UnsafeNativeMethods.Rdk_ON_Decal_TextureRenderCRC(
                                 m_rhino_doc_serial, ConstPointer(), (ulong)flags, IntPtr.Zero);

      GC.KeepAlive(this);
      return ret;
    }

    /// <summary>
    /// Get the texture render hash for the referenced texture using the specified CrcRenderHashFlags
    /// and linear workflow.
    /// </summary>
    /// <since>8.0</since>
    /// <deprecated>8.5</deprecated>
    [CLSCompliant(false)]
    [Obsolete("Do not use")]
    public uint TextureRenderHash(CrcRenderHashFlags flags, LinearWorkflow lw)
    {
      if (0 == m_rhino_doc_serial)
        throw new DecalDocumentException();

      var ret = UnsafeNativeMethods.Rdk_ON_Decal_TextureRenderCRC(
                                 m_rhino_doc_serial, ConstPointer(), (ulong)flags, lw.CppPointer);

      GC.KeepAlive(this);
      return ret;
    }
#endif

    /// <summary>
    /// Gets the decal's mapping.
    /// </summary>
    /// <since>5.10</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete ("Use Mapping instead")]
    public DecalMapping DecalMapping { get => Mapping; }

    /// <summary>
    /// Gets the decal's mapping.
    /// </summary>
    /// <since>8.0</since>
    public DecalMapping Mapping
    {
      get
      {
        var s = (UnsafeNativeMethods.ON_DecalMapping)UnsafeNativeMethods.ON_Decal_Mapping(ConstPointer());
        GC.KeepAlive(this);

        switch (s)
        {
          case UnsafeNativeMethods.ON_DecalMapping.Planar:      return DecalMapping.Planar;
          case UnsafeNativeMethods.ON_DecalMapping.Cylindrical: return DecalMapping.Cylindrical;
          case UnsafeNativeMethods.ON_DecalMapping.Spherical:   return DecalMapping.Spherical;
          case UnsafeNativeMethods.ON_DecalMapping.UV:          return DecalMapping.UV;
        }

        throw new Exception("Unknown DecalMapping type");
      }
    }

    /// <summary>
    /// Gets the decal's projection. Used only when mapping is planar.
    /// </summary>
    /// <since>5.10</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete ("Use Projection instead")]
    public DecalProjection DecalProjection { get => Projection; }

    /// <summary>
    /// Gets the decal's projection. Used only when mapping is planar.
    /// </summary>
    /// <since>8.0</since>
    public DecalProjection Projection
    {
      get
      {
        var s = (UnsafeNativeMethods.ON_DecalProjection)UnsafeNativeMethods.ON_Decal_Projection(ConstPointer());
        GC.KeepAlive(this);

        switch (s)
        {
          case UnsafeNativeMethods.ON_DecalProjection.Forward:  return DecalProjection.Forward;
          case UnsafeNativeMethods.ON_DecalProjection.Backward: return DecalProjection.Backward;
          case UnsafeNativeMethods.ON_DecalProjection.Both:     return DecalProjection.Both;
          case UnsafeNativeMethods.ON_DecalProjection.None:     return DecalProjection.None;
        }

        throw new Exception("Unknown DecalProjection type");
      }
    }

    /// <summary>
    /// Used only when mapping is cylindrical or spherical.
    /// </summary>
    /// <value>true if texture is mapped to inside of sphere or cylinder, else \e false.</value>
    /// <since>5.10</since>
    public bool MapToInside
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_MapToInside(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the decal's transparency in the range 0 to 1.
    /// </summary>
    /// <since>5.10</since>
    public double Transparency
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_Transparency(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the decal's visibility.
    /// </summary>
    /// <since>8.18</since>
    public bool IsVisible
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_IsVisible(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the origin of the decal in world space.
    /// </summary>
    /// <since>5.10</since>
    public Rhino.Geometry.Point3d Origin
    {
      get
      {
        var v = new Rhino.Geometry.Point3d();
        UnsafeNativeMethods.ON_Decal_Origin(ConstPointer(), ref v);
        GC.KeepAlive(this);
        return v;
      }
    }

    /// <summary>
    /// For cylindrical and spherical mapping, the vector is unitized.
    /// </summary>
    /// <returns>The 'up' vector of the decal. For planar mapping the length of the vector is relevant.</returns>
    /// <since>5.10</since>
    public Rhino.Geometry.Vector3d VectorUp
    {
      get
      {
        var v = new Rhino.Geometry.Vector3d();
        UnsafeNativeMethods.ON_Decal_VectorUp(ConstPointer(), ref v);
        GC.KeepAlive(this);
        return v;
      }
    }

    /// <summary>
    /// Gets the vector across. For cylindrical and spherical mapping, the vector is unitized.
    /// </summary>
    /// <value>The 'across' vector of the decal. For planar mapping the length of the vector is relevant.</value>
    /// <since>5.10</since>
    public Rhino.Geometry.Vector3d VectorAcross
    {
      get
      {
        var v = new Rhino.Geometry.Vector3d();
        UnsafeNativeMethods.ON_Decal_VectorAcross(ConstPointer(), ref v);
        GC.KeepAlive(this);
        return v;
      }
    }

    /// <summary>
    /// Gets the height of the decal. Only used when mapping is cylindrical.
    /// </summary>
    /// <since>5.10</since>
    public double Height
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_Height(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the radius of the decal. Only used when mapping is cylindrical or spherical.
    /// </summary>
    /// <since>5.10</since>
    public double Radius
    {
      get
      {
        var ret = UnsafeNativeMethods.ON_Decal_Radius(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    /// <summary>
    /// Gets the start angle of the decal's arc of latitude or 'horizontal sweep'. This is actually a LONGITUDINAL angle. Only used when mapping is cylindrical or spherical.
    /// This is deprecated in favor of HorzSweep().
    /// </summary>
    /// <since>5.10</since>
    public double StartLatitude { get { HorzSweep(out double sta, out _); return sta; } }

    /// <summary>
    /// Gets the end angle of the decal's arc of latitude or 'horizontal sweep'. This is actually a LONGITUDINAL angle. Only used when mapping is cylindrical or spherical.
    /// This is deprecated in favor of HorzSweep().
    /// </summary>
    /// <since>5.10</since>
    public double EndLatitude { get { HorzSweep(out _, out double end); return end; } }

    /// <summary>
    /// Gets the start angle of the decal's arc of longitude or 'vertical sweep'. This is actually a LATITUDINAL angle. Only used when mapping is spherical.
    /// This is deprecated in favor of VertSweep().
    /// </summary>
    /// <since>5.10</since>
    public double StartLongitude { get { VertSweep(out double sta, out _); return sta; } }

    /// <summary>
    /// Gets the end angle of the decal's arc of longitude or 'vertical sweep'. This is actually a LATITUDINAL angle. Only used when mapping is spherical.
    /// This is deprecated in favor of VertSweep().
    /// </summary>
    /// <since>5.10</since>
    public double EndLongitude { get { VertSweep(out _, out double end); return end; } }

    /// <summary>
    /// Gets the angles of the decal's arc of 'horizontal sweep'. Replaces StartLatitude and EndLatitude.
    /// </summary>
    /// <since>8.0</since>
    public void HorzSweep(out double sta, out double end)
    {
      double s = 0.0, e = 0.0;
      UnsafeNativeMethods.ON_Decal_GetHorzSweep(ConstPointer(), ref s, ref e);
      GC.KeepAlive(this);
      sta = s; end = e;
    }

    /// <summary>
    /// Gets the angles of the decal's arc of 'vertical sweep'. Replaces StartLongitude and EndLongitude.
    /// </summary>
    /// <since>8.0</since>
    public void VertSweep(out double sta, out double end)
    {
      double s = 0.0, e = 0.0;
      UnsafeNativeMethods.ON_Decal_GetVertSweep(ConstPointer(), ref s, ref e);
      GC.KeepAlive(this);
      sta = s; end = e;
    }

    /// <summary>
    /// The UV bounds of the decal. Only used when mapping is UV.
    /// </summary>
    /// <since>5.10</since>
 //   [Obsolete("Use GetUVBounds instead")]
    public void UVBounds(ref double minUOut, ref double minVOut, ref double maxUOut, ref double maxVOut)
    {
      UnsafeNativeMethods.ON_Decal_UVBounds(ConstPointer(), ref minUOut, ref minVOut, ref maxUOut, ref maxVOut);
    }

    /// <summary>
    /// The UV bounds of the decal. Only used when mapping is UV.
    /// </summary>
    /// <since>8.18</since>
    public void GetUVBounds(out double min_u, out double min_v, out double max_u, out double max_v)
    {
      double a = 0.0, b = 0.0, c = 0.0, d = 0.0;
      UnsafeNativeMethods.ON_Decal_UVBounds(ConstPointer(), ref a, ref b, ref c, ref d);
      GC.KeepAlive(this);
      min_u = a; min_v = b; max_u = c; max_v = d;
    }

    /// <summary>
    /// The TextureMapping of the decal.
    /// </summary>
    /// <since>7.0</since>
    public TextureMapping GetTextureMapping()
    {
      var tm = new TextureMapping();
      UnsafeNativeMethods.ON_Decal_TextureMapping(ConstPointer(), tm.NonConstPointer());
      GC.KeepAlive(this);
      return tm;
    }

    internal static List<NamedValue> ConvertToNamedValueList(IntPtr parms)
    {
      var list = new List<NamedValue>();

      var pIterator = UnsafeNativeMethods.ON_XMLParameters_GetIterator(parms);
      if (pIterator != IntPtr.Zero)
      {
        using (var sh = new StringHolder())
        {
          using (var variant = new Variant())
          {
            while (UnsafeNativeMethods.ON_XMLParameters_NextParam(parms, pIterator, sh.ConstPointer(), variant.ConstPointer()))
            {
              list.Add(new NamedValue(sh.ToString(), variant.AsObject()));
            }
          }
        }

        UnsafeNativeMethods.ON_XMLParameters_DeleteIterator(pIterator);
      }

      return list;
    }

    /// <summary>
    /// Gets decal custom data for a specified renderer. See Rhino.Plugins.RenderPlugIn.ShowDecalProperties.
    /// </summary>
    /// <returns>A list of name-value pairs for the custom data properties. If there is no
    /// custom data on the decal for the specified renderer, the list will be empty.</returns>
    /// <since>8.0</since>
    public List<Rhino.Render.NamedValue> CustomData(Guid renderer)
    {
      var param_block = UnsafeNativeMethods.ON_XMLParameters_NewParamBlock();
      UnsafeNativeMethods.ON_Decal_CustomData(ConstPointer(), param_block, ref renderer);
      var list = ConvertToNamedValueList(param_block);
      UnsafeNativeMethods.ON_XMLParameters_Delete(param_block);

      GC.KeepAlive(this);

      return list;
    }

#if RHINO_SDK
    /// <summary>
    /// Gets decal custom data for the current renderer. See Rhino.Plugins.RenderPlugIn.ShowDecalProperties.
    /// </summary>
    /// <returns>A list of name-value pairs for the custom data properties. If there is no
    /// custom data on the decal for the current renderer, the list will be empty.</returns>
    /// <since>6.0</since>
    public List<Rhino.Render.NamedValue> CustomData()
    {
      var param_block = UnsafeNativeMethods.ON_XMLParameters_NewParamBlock();
      UnsafeNativeMethods.Rdk_ON_Decal_CustomData(ConstPointer(), param_block);
      var list = ConvertToNamedValueList(param_block);
      UnsafeNativeMethods.ON_XMLParameters_Delete(param_block);

      GC.KeepAlive(this);

      return list;
    }

    /// <summary>
    /// Blend a color with the decal color at a given point. Please note that this function is very slow
    /// so you should only use it if you're not overly concerned about performance.
    /// Also please note that this function is actually a 'blend' operation, not a true 'get' operation
    /// and is sensitive to the value passed into the colInOut parameter. The alpha value of colInOut
    /// is used as the blend amount where 0 means 'all decal RGB' and 1 means 'all colInOut RGB'.
    /// If you want to simply GET the decal color, you must pass 0 for colInOut alpha.
    /// </summary>
    /// <param name="point">The point in space or, if the decal is uv-mapped, the uv-coordinate of that point.</param>
    /// <param name="normal">The face normal of the given point.</param>
    /// <param name="colInOut">Accepts the color to blend the decal color to and returns the blended color.
    ///                        See the notes above for details.</param>
    /// <param name="uvOut">Returns the (u,v) on the texture that the color was read from.</param>
    /// <returns>true if the given point hits the decal, else false.</returns>
    /// <since>5.10</since>
    public bool TryGetColor(    Rhino.Geometry.Point3d point,       Rhino.Geometry.Vector3d normal,
                            ref Rhino.Display.Color4f colInOut, ref Rhino.Geometry.Point2d uvOut)
    {
      if (0 == m_rhino_doc_serial)
        throw new DecalDocumentException();

      var ret = UnsafeNativeMethods.Rdk_ON_Decal_GetColor(m_rhino_doc_serial, ConstPointer(), ref point, ref normal, ref colInOut, ref uvOut);
      GC.KeepAlive(this);
      return ret;
    }
#else
#endif

    #region internals

    internal IntPtr DecalIntPtr
    {
      get
      {
        var decal_ptr = UnsafeNativeMethods.SharedPtr_ON_Decal_RawPtr(m_decal_sp);
        GC.KeepAlive(this);
        return decal_ptr;
      }
    }

    /// <since>5.10</since>
    /// Returns the raw const ON_Decal pointer.
    public IntPtr ConstPointer() { return DecalIntPtr; }

    /// <since>5.10</since>
    /// Returns the raw non-const ON_Decal pointer.
    public IntPtr NonConstPointer() { return DecalIntPtr; }

    #endregion
  }

  // TODO: Modify this to be derived from IList<Decal> instead when possible.
  /// <summary>Represents all the decals of an object.</summary>
  public class Decals : IEnumerable<Decal>
  {
    private readonly ObjectAttributes m_parent_attributes;
    private readonly uint m_rhino_doc_serial = 0;
    private readonly bool m_read_only = false;

    internal Decals(ObjectAttributes parent, bool read_only, uint rhino_doc_sn)
    {
      m_read_only = read_only;
      m_parent_attributes = parent;
      m_rhino_doc_serial = rhino_doc_sn;
    }

    /// <summary>
    /// Add a new Decal to the decals list, use Decal.Create to create
    /// a new decal instance to add.
    /// </summary>
    /// <param name="decal"></param>
    /// <returns></returns>
    /// <since>5.10</since>
    [CLSCompliant(false)]
    public uint Add(Decal decal)
    {
      if (m_read_only)
        throw new DecalReadOnlyException();

      if (decal == null)
        throw new ArgumentNullException(nameof(decal));

      // Adding a decal to the collection requires the decal data to be copied onto the collection's
      // parent object attributes. We can't just add the decal to some list of decals because the
      // decal actually has its own object attributes to hold the user data / XML blob as well as
      // a cached decal object.

      var create_params = UnsafeNativeMethods.Rdk_DecalCreateParams_New();

      var origin = decal.Origin;
      var up     = decal.VectorUp;
      var across = decal.VectorAcross;
      decal.HorzSweep(out var horz_sta, out var horz_end);
      decal.VertSweep(out var vert_sta, out var vert_end);
      decal.GetUVBounds(out var min_u, out var min_v, out var max_u, out var max_v);
      UnsafeNativeMethods.Rdk_DecalCreateParams_Set(create_params,
                    decal.TextureInstanceId, (int)decal.Mapping, (int)decal.Projection, decal.MapToInside,
                    decal.Transparency, ref origin, ref up, ref across, decal.Height, decal.Radius,
                    horz_sta, horz_end, vert_sta, vert_end, min_u, min_v, max_u, max_v);

      // create_params is actually an ON_Decal.
      var attr_ptr = m_parent_attributes.NonConstPointer();
      var decal_crc = UnsafeNativeMethods.Rdk_Decal_AddDecal(create_params, attr_ptr);

      UnsafeNativeMethods.Rdk_DecalCreateParams_Delete(create_params);

      GC.KeepAlive(decal);

      // TODO: Eventually this class will be derived from IList<Decal> which requires
      // a "int Add(T)" method so return the index of the new decal instead of the Id.

      return decal_crc;
    }

    /// <since>5.10</since>
    /// <deprecated>8.0</deprecated>
    [Obsolete("Use RemoveAllDecals")]
    public void Clear() { RemoveAllDecals(); }

    /// <summary>
    /// Remove all the decals from the collection.
    /// </summary>
    /// <since>8.0</since>
    public void RemoveAllDecals()
    {
      if (m_read_only)
        throw new DecalReadOnlyException();

      var non_const_pointer = m_parent_attributes.NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_RemoveAllDecals(non_const_pointer);

      GC.KeepAlive(this);
    }

    /// <summary>
    /// Remove a single decal from the collection.
    /// </summary>
    /// <since>5.10</since>
    public bool Remove(Decal decal)
    {
      if (m_read_only)
        throw new DecalReadOnlyException();

      if (decal == null)
        return false;

      var non_const_pointer = m_parent_attributes.NonConstPointer();
      var ret = UnsafeNativeMethods.ON_3dmObjectAttributes_RemoveDecal(non_const_pointer, decal.NonConstPointer());
      GC.KeepAlive(this);
      GC.KeepAlive(decal);
      return ret;
    }

    /// <since>5.10</since>
    public IEnumerator<Decal> GetEnumerator()
    {
      return new DecalEnumerator(m_parent_attributes, m_rhino_doc_serial);
    }

    /// <since>5.10</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }


  /// <summary>Represents all the decals of an object.</summary>
  internal class DecalEnumerator : IEnumerator<Decal>
  {
    private int m_index = 0;
    private readonly uint m_rhino_doc_serial = 0;
    private readonly ObjectAttributes m_attr;
    private readonly SimpleArrayIntPtr m_ptrs_to_shared_ptrs_to_decals;

    internal DecalEnumerator(ObjectAttributes attr, uint rhino_doc_sn)
    {
      m_attr = attr;
      m_rhino_doc_serial = rhino_doc_sn;
      m_ptrs_to_shared_ptrs_to_decals = new SimpleArrayIntPtr();

      var attr_ptr = m_attr.ConstPointer();
      var array_ptr = m_ptrs_to_shared_ptrs_to_decals.NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GetDecalArray(attr_ptr, array_ptr);

      GC.KeepAlive(this);
    }

    ~DecalEnumerator() { Dispose(false); }

    #region IEnumerator Members

    public Decal Current { get; private set; }

    object IEnumerator.Current { get { return Current; } }

    internal Decal NextDecal()
    {
      if (m_index >= m_ptrs_to_shared_ptrs_to_decals.Count)
        return null;

      var decal_sp_ptr = m_ptrs_to_shared_ptrs_to_decals.ToArray()[m_index++];

      return new Decal(decal_sp_ptr, this, m_rhino_doc_serial);
    }

    public bool MoveNext()
    {
      Decal decal = NextDecal();

      while ((decal != null) && !decal.IsVisible)
      {
        decal = NextDecal();
      }

      Current = decal;

      return decal != null;
    }

    public void Reset()
    {
      m_index = 0;
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      Dispose(true);
    }

    protected void Dispose(bool isDisposing)
    {
      var decal_array = m_ptrs_to_shared_ptrs_to_decals.ToArray();

      foreach (var psp in decal_array)
      {
        UnsafeNativeMethods.SharedPtr_ON_Decal_Delete(psp);
      }

      m_ptrs_to_shared_ptrs_to_decals.Dispose();
    }

    #endregion
  }

  /// <summary>
  /// Used by RhinoObject.AddDecal() to create and add a decal
  /// </summary>
  public class DecalCreateParams
  {
    /// <since>6.0</since>
    public Guid TextureInstanceId { get; set; }
    /// <since>6.0</since>
    public DecalMapping DecalMapping { get; set; }
    /// <since>6.0</since>
    public DecalProjection DecalProjection { get; set; }
    /// <since>6.0</since>
    public bool MapToInside { get; set; }
    /// <since>6.0</since>
    public double Transparency { get; set; }
    /// <since>6.0</since>
    public Geometry.Point3d Origin { get; set; }
    /// <since>6.0</since>
    public Geometry.Vector3d VectorUp { get; set; }
    /// <since>6.0</since>
    public Geometry.Vector3d VectorAcross { get; set; }
    /// <since>6.0</since>
    public double Height { get; set; }
    /// <since>6.0</since>
    public double Radius { get; set; }
    /// <since>6.0</since>
    public double StartLatitude { get; set; }
    /// <since>6.0</since>
    public double EndLatitude { get; set; }
    /// <since>6.0</since>
    public double StartLongitude { get; set; }
    /// <since>6.0</since>
    public double EndLongitude { get; set; }
    /// <since>6.0</since>
    public double MinU { get; set; }
    /// <since>6.0</since>
    public double MinV { get; set; }
    /// <since>6.0</since>
    public double MaxU { get; set; }
    /// <since>6.0</since>
    public double MaxV { get; set; }
  }
}
