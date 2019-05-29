#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using System.Collections;
using Rhino.DocObjects;


namespace Rhino.Render
{
  public enum DecalMapping : int
  {
    /// <summary>
    /// Planar mapping. Uses projection, origin, up and across vectors (not unitized).
    /// </summary>
    Planar = (int)UnsafeNativeMethods.RhRdkDecalMapping.Planar,
    /// <summary>
    /// Cylindrical mapping. Uses origin, up, across, height, radius, latitude start and stop.
    /// </summary>
    Cylindrical = (int)UnsafeNativeMethods.RhRdkDecalMapping.Cylindrical,
    /// <summary>
    /// Spherical mapping. Uses origin, up, across, radius, latitude/longitude start and stop.
    /// </summary>
    Spherical = (int)UnsafeNativeMethods.RhRdkDecalMapping.Spherical,
    /// <summary>
    /// UV mapping.
    /// </summary>
    UV = (int)UnsafeNativeMethods.RhRdkDecalMapping.UV
  }

  public enum DecalProjection : int
  {
    /// <summary>Project forward</summary>
    Forward = (int)UnsafeNativeMethods.RhRdkDecalProjection.Forward,
    /// <summary>Project backward</summary>
    Backward = (int)UnsafeNativeMethods.RhRdkDecalProjection.Backward,
    /// <summary>Project forward and backward</summary>
    Both = (int)UnsafeNativeMethods.RhRdkDecalProjection.Both
  }

  /// <summary>
  /// Represents a decal, or a picture that can be moved on an object.
  /// </summary>
  public class Decal : IDisposable
  {
    IntPtr m_pDecal = IntPtr.Zero;
    //Forces a reference to the decal iterator to stick around until this object is GCd.
    DecalEnumerator m_decals;
    // Used by Decal.Create() to hold a temporary object attributes
    // pointer which the decal will get attached to
    private IntPtr m_object_attributes_pointer = IntPtr.Zero;
    private bool m_delete_decal_pointer;

    internal Decal(IntPtr pDecal)
    {
      m_pDecal = pDecal;
    }

    internal Decal(IntPtr pDecal, DecalEnumerator decals)
    {
      m_pDecal = pDecal;
      m_decals = decals;
    }

    internal Decal(IntPtr objectAttributesPointer, uint decalId)
    {
      m_object_attributes_pointer = objectAttributesPointer;
      m_delete_decal_pointer = true;
      m_pDecal = UnsafeNativeMethods.Rdk_Decals_FindDecalOnObjectAttributes(m_object_attributes_pointer, decalId);
    }

    ~Decal()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    public void Dispose(bool isDisposing)
    {
      if (m_object_attributes_pointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_Delete(m_object_attributes_pointer);
        m_object_attributes_pointer = IntPtr.Zero;
      }
      if (!m_delete_decal_pointer || m_pDecal == IntPtr.Zero) return;
      UnsafeNativeMethods.IRhRdkDecal_Delete(m_pDecal);
      m_pDecal = IntPtr.Zero;
    }

    static public Decal Create(DecalCreateParams createParams)
    {
      var pointer = UnsafeNativeMethods.Rdk_DecalCreateParams_New();
      if (pointer == IntPtr.Zero) return null;
      var origin = createParams.Origin;
      var up_vector = createParams.VectorUp;
      var across_vector = createParams.VectorAcross;
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetFrame(pointer, ref origin, ref up_vector, ref across_vector);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetMap(pointer, createParams.TextureInstanceId, (int)createParams.DecalMapping, (int)createParams.DecalProjection, createParams.MapToInside, createParams.Transparency);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetCylindricalAndSpherical(pointer, createParams.Height, createParams.Radius, createParams.StartLatitude, createParams.EndLatitude, createParams.StartLongitude, createParams.EndLongitude);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetUV(pointer, createParams.MinU, createParams.MinV, createParams.MaxU, createParams.MaxV);

      var attributes_pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_New(IntPtr.Zero);
      var id = UnsafeNativeMethods.Rdk_Decals_AddDecal(pointer, attributes_pointer);

      UnsafeNativeMethods.Rdk_DecalCreateParams_Delete(pointer);
      pointer = IntPtr.Zero;

      if (id < 1)
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_Delete(attributes_pointer);
        return null;
      }

      var new_decal = new Decal(attributes_pointer, id);
      return new_decal;
    }

    /// <summary>
    /// The decal CRC identifies a decal by its state. Multiple decals which would be
    /// exactly the same would have the same CRC and are culled from the system.
    /// If you store this value with the intention of using it to find the decal again
    /// later, you must update your stored value whenever the decal state changes.
    /// You can detect when a decal changes by watching for the OnUserDataTransformed event.
    /// </summary>
    public int CRC
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.Rdk_Decal_CRC(const_ptr_this);
      }
    }

    /// <summary>
    /// Gets the texture ID for this decal.
    /// </summary>
    public Guid TextureInstanceId
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.Rdk_Decal_TextureInstanceId(pConstThis);
      }
    }

    /// <summary>
    /// Gets the mapping of the decal.
    /// </summary>
    public DecalMapping DecalMapping
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        var mapping = UnsafeNativeMethods.Rdk_Decal_Mapping(pConstThis);
        switch (mapping)
        {
          case UnsafeNativeMethods.RhRdkDecalMapping.Planar:
            return DecalMapping.Planar;
          case UnsafeNativeMethods.RhRdkDecalMapping.Cylindrical:
            return DecalMapping.Cylindrical;
          case UnsafeNativeMethods.RhRdkDecalMapping.Spherical:
            return DecalMapping.Spherical;
          case UnsafeNativeMethods.RhRdkDecalMapping.UV:
            return DecalMapping.UV;
        }
        throw new Exception("Unknown DecalMapping type");
      }
    }

    /// <summary>
    /// Gets the decal's projection. Used only when mapping is planar.
    /// </summary>
    public DecalProjection DecalProjection
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        var projection = UnsafeNativeMethods.Rdk_Decal_Projection(pConstThis);
        switch (projection)
        {
          case UnsafeNativeMethods.RhRdkDecalProjection.Forward:
            return DecalProjection.Forward;
          case UnsafeNativeMethods.RhRdkDecalProjection.Backward:
            return DecalProjection.Backward;
          case UnsafeNativeMethods.RhRdkDecalProjection.Both:
            return DecalProjection.Both;
        }
        throw new Exception("Unknown DecalProjection type");
      }
    }

    /// <summary>
    /// Used only when mapping is cylindrical or spherical.
    /// </summary>
    /// <value>true if texture is mapped to inside of sphere or cylinder, else \e false.</value>
    public bool MapToInside
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return 1 == UnsafeNativeMethods.Rdk_Decal_MapToInside(pConstThis);
      }
    }

    /// <summary>
    /// Gets the decal's transparency in the range 0 to 1.
    /// </summary>
    public double Transparency
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.Rdk_Decal_Transparency(pConstThis);
      }
    }

    /// <summary>
    /// Gets the origin of the decal in world space.
    /// </summary>
    public Rhino.Geometry.Point3d Origin
    {
      get
      {
        Rhino.Geometry.Point3d v = new Rhino.Geometry.Point3d();
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.Rdk_Decal_Origin(pConstThis, ref v);
        return v;
      }
    }

    /// <summary>
    /// For cylindrical and spherical mapping, the vector is unitized.
    /// </summary>
    /// <returns>The 'up' vector of the decal. For planar mapping the length of the vector is relevant.</returns>
    public Rhino.Geometry.Vector3d VectorUp
    {
      get
      {
        Rhino.Geometry.Vector3d v = new Rhino.Geometry.Vector3d();
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.Rdk_Decal_VectorUp(pConstThis, ref v);
        return v;
      }
    }

    /// <summary>
    /// Gets the vector across. For cylindrical and spherical mapping, the vector is unitized.
    /// </summary>
    /// <value>The 'across' vector of the decal. For planar mapping the length of the vector is relevant.</value>
    public Rhino.Geometry.Vector3d VectorAcross
    {
      get
      {

        Rhino.Geometry.Vector3d v = new Rhino.Geometry.Vector3d();
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.Rdk_Decal_VectorAcross(pConstThis, ref v);
        return v;
      }
    }

    /// <summary>
    /// Gets the height of the decal. Only used when mapping is cylindrical.
    /// </summary>
    public double Height
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.Rdk_Decal_Height(pConstThis);
      }
    }

    /// <summary>
    /// Gets the radius of the decal. Only used when mapping is cylindrical or spherical.
    /// </summary>
    public double Radius
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.Rdk_Decal_Radius(pConstThis);
      }
    }

    /// <summary>
    /// Gets the start angle of the decal's arc of latitude or 'horizontal sweep'. This is actually a LONGITUDINAL angle. Only used when mapping is cylindrical or spherical.
    /// </summary>
    public double StartLatitude
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        double start = 0.0, end = 0.0;
        UnsafeNativeMethods.Rdk_Decal_HorzSweep(pConstThis, ref start, ref end);
        return start;
      }
    }

    /// <summary>
    /// Gets the end angle of the decal's arc of latitude or 'horizontal sweep'. This is actually a LONGITUDINAL angle. Only used when mapping is cylindrical or spherical.
    /// </summary>
    public double EndLatitude
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        double start = 0.0, end = 0.0;
        UnsafeNativeMethods.Rdk_Decal_HorzSweep(pConstThis, ref start, ref end);
        return end;
      }
    }

    /// <summary>
    /// Gets the start angle of the decal's arc of longitude or 'vertical sweep'. This is actually a LATITUDINAL angle. Only used when mapping is spherical.
    /// </summary>
    public double StartLongitude
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        double start = 0.0, end = 0.0;
        UnsafeNativeMethods.Rdk_Decal_VertSweep(pConstThis, ref start, ref end);
        return start;
      }
    }

    /// <summary>
    /// Gets the end angle of the decal's arc of longitude or 'vertical sweep'. This is actually a LATITUDINAL angle. Only used when mapping is spherical.
    /// </summary>
    public double EndLongitude
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        double start = 0.0, end = 0.0;
        UnsafeNativeMethods.Rdk_Decal_VertSweep(pConstThis, ref start, ref end);
        return end;
      }
    }

    /// <summary>
    /// The UV bounds of the decal. Only used when mapping is UV.
    /// </summary>
    public void UVBounds(ref double minUOut, ref double minVOut, ref double maxUOut, ref double maxVOut)
    {
      IntPtr pConstThis = ConstPointer();
      UnsafeNativeMethods.Rdk_Decal_UVBounds(pConstThis, ref minUOut, ref minVOut, ref maxUOut, ref maxVOut);
    }

    /// <summary>
    /// Gets custom data associated with this decal - see Rhino.Plugins.RenderPlugIn.ShowDecalProperties.
    /// </summary>
    /// <returns>The return value can be null if there is no data associated with this decal.</returns>
    public List<Rhino.Render.NamedValue> CustomData()
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pXmlSection = UnsafeNativeMethods.Rdk_Decal_CustomData(pConstThis);
      if (IntPtr.Zero == pXmlSection)
        return null;

      return Rhino.Render.XMLSectionUtilities.ConvertToNamedValueList(pXmlSection);
    }

    /// <summary>
    /// Blend color with the decal color at a given point.
    /// </summary>
    /// <param name="point">The point in space or, if the decal is uv-mapped, the uv-coordinate of that point.</param>
    /// <param name="normal">The face normal of the given point.</param>
    /// <param name="colInOut">The color to blend the decal color to.</param>
    /// <param name="uvOut">the UV on the texture that the color point was read from.</param>
    /// <returns>true if the given point hits the decal, else false.</returns>
    public bool TryGetColor(Rhino.Geometry.Point3d point, Rhino.Geometry.Vector3d normal, ref Rhino.Display.Color4f colInOut, ref Rhino.Geometry.Point2d uvOut)
    {
      return 1 == UnsafeNativeMethods.Rdk_Decal_Color(ConstPointer(), point, normal, ref colInOut, ref uvOut);
    }

    #region internals
    public IntPtr ConstPointer()
    {
      return m_pDecal;
    }

    public IntPtr NonConstPointer()
    {
      return m_pDecal;
    }
    #endregion
  }



  // TODO
  // Modify this to be derived from IList<Decal> instead when possible
  /// <summary>Represents all the decals of an object.</summary>
  public class Decals : IEnumerable<Decal>
  {
    private readonly ObjectAttributes m_parent_attributes;

    internal Decals(ObjectAttributes parent)
    {
      m_parent_attributes = parent;
    }

    /// <summary>
    /// Add a new Decal to the decals list, use Decal.Create to create
    /// a new decal instance to add.
    /// </summary>
    /// <param name="decal"></param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public uint Add(Decal decal)
    {
      if (decal == null) throw new ArgumentNullException("decal");
      var pointer = UnsafeNativeMethods.Rdk_DecalCreateParams_New();
      var origin = decal.Origin;
      var up_vector = decal.VectorUp;
      var across_vector = decal.VectorAcross;
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetFrame(pointer, ref origin, ref up_vector, ref across_vector);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetMap(pointer, decal.TextureInstanceId, (int)decal.DecalMapping, (int)decal.DecalProjection, decal.MapToInside, decal.Transparency);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetCylindricalAndSpherical(pointer, decal.Height, decal.Radius, decal.StartLatitude, decal.EndLatitude, decal.StartLongitude, decal.EndLongitude);
      double minu = 0.0, maxu = 0.0, minv = 0.0, maxv = 0.0;
      decal.UVBounds(ref minu, ref minv, ref maxu, ref maxv);
      UnsafeNativeMethods.Rdk_DecalCreateParams_SetUV(pointer, minu, minv, maxu, maxv);
      var non_const_pointer = m_parent_attributes.NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_Decals_AddDecal(pointer, non_const_pointer);
      UnsafeNativeMethods.Rdk_DecalCreateParams_Delete(pointer);
      // TODO
      // Eventually this class will be derived from IList<Decal> which requires
      // a "int Add(T)" method so return the index of the new decal instead of
      // the Id.
      return success;
    }

    public void Clear()
    {
      var non_const_pointer = m_parent_attributes.NonConstPointer();
      UnsafeNativeMethods.Rdk_Decals_RemoveAllDecalsFromObjectAttributes(non_const_pointer);
    }

    public bool Remove(Decal decal)
    {
      if (decal == null) return false;
      var non_const_pointer = m_parent_attributes.NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_Decals_RemoveDecalFromObjectAttributes(non_const_pointer, (uint)decal.CRC);
      return success;
    }

    public IEnumerator<Decal> GetEnumerator()
    {
      return new DecalEnumerator(m_parent_attributes);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }

  /// <summary>Represents all the decals of an object.</summary>
  internal class DecalEnumerator : IEnumerator<Decal>
  {
    private IntPtr m_decal_iterator;
    internal DecalEnumerator(ObjectAttributes attributes)
    {
      var pointer = attributes.NonConstPointer();
      m_decal_iterator = UnsafeNativeMethods.Rdk_Decals_NewDecalIteratorForObjectAttributes(pointer);
    }

    ~DecalEnumerator()
    {
      Dispose(false);
    }

    #region IEnumerator Members

    public Decal Current { get; private set; }

    object IEnumerator.Current { get { return Current; } }

    public bool MoveNext()
    {
      var decal = UnsafeNativeMethods.Rdk_Decals_Next(NonConstPointer());

      if (decal == IntPtr.Zero)
      {
        Current = null;
        return false;
      }

      Current = new Decal(decal, this);
      return true;
    }

    public void Reset()
    {
      UnsafeNativeMethods.Rdk_Decals_ResetIterator(NonConstPointer());
    }

    #endregion

    #region internals
    private IntPtr NonConstPointer()
    {
      return m_decal_iterator;
    }
    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      Dispose(true);
    }

    protected void Dispose(bool isDisposiing)
    {
      if (m_decal_iterator != IntPtr.Zero)
        UnsafeNativeMethods.Rdk_Decals_DeleteDecalIterator(m_decal_iterator);
      m_decal_iterator = IntPtr.Zero;
    }

    #endregion
  }


  /// <summary>
  /// Used by RhinoObject.AddDecal() to create and add a decal
  /// </summary>
  public class DecalCreateParams
  {
    public Guid TextureInstanceId { get; set; }
    public DecalMapping DecalMapping { get; set; }
    public DecalProjection DecalProjection { get; set; }
    public bool MapToInside { get; set; }
    public double Transparency { get; set; }
    public Geometry.Point3d Origin { get; set; }
    public Geometry.Vector3d VectorUp { get; set; }
    public Geometry.Vector3d VectorAcross { get; set; }
    public double Height { get; set; }
    public double Radius { get; set; }
    public double StartLatitude { get; set; }
    public double EndLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLongitude { get; set; }
    public double MinU { get; set; }
    public double MinV { get; set; }
    public double MaxU { get; set; }
    public double MaxV { get; set; }
  }
}

#endif
