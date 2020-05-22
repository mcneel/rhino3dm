using System;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;

// ReSharper disable once CheckNamespace
namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a light that shines in the modeling space.
  /// </summary>
  [Serializable]
  public class Light : GeometryBase
  {
 #if RHINO_SDK
    /// <summary>
    /// Constructs a light that represents the Sun.
    /// </summary>
    /// <param name="northAngleDegrees">The angle of North in degrees. North is the angle between positive World Y axis and model North, as measured on World XY plane.</param>
    /// <param name="azimuthDegrees">The Azimuth angle value in degrees. Azimuth is the compass angle from North.</param>
    /// <param name="altitudeDegrees">The Altitude angle in degrees. Altitude is the angle above the ground plane.</param>
    /// <returns>A new sun light.</returns>
    /// <exception cref="Rhino.Runtime.RdkNotLoadedException">If the RDK is not loaded.</exception>
    /// <since>5.0</since>
    public static Light CreateSunLight(double northAngleDegrees, double azimuthDegrees, double altitudeDegrees)
    {
      Runtime.HostUtils.CheckForRdk(true, true);

      var sun = new Rhino.Render.Sun();

      sun.North = northAngleDegrees;
      sun.SetPosition(azimuthDegrees, altitudeDegrees);

      return sun.Light;
    }

    /// <summary>
    /// Constructs a light which simulates the Sun based on a given time and location on Earth.
    /// </summary>
    /// <param name="northAngleDegrees">The angle of North in degrees. North is the angle between positive World Y axis and model North, as measured on World XY plane.</param>
    /// <param name="when">The time of the measurement. The Kind property of DateTime specifies whether this is in local or universal time.
    /// <para>Local and Undefined <see cref="DateTimeKind">daytime kinds</see> in this argument are considered local.</para></param>
    /// <param name="latitudeDegrees">The latitude, in degrees, of the location on Earth.</param>
    /// <param name="longitudeDegrees">The longitude, in degrees, of the location on Earth.</param>
    /// <returns>A newly constructed light object.</returns>
    /// <exception cref="Rhino.Runtime.RdkNotLoadedException">If the RDK is not loaded.</exception>
    /// <since>5.0</since>
    public static Light CreateSunLight(double northAngleDegrees, DateTime when, double latitudeDegrees, double longitudeDegrees)
    {
      Runtime.HostUtils.CheckForRdk(true, true);

      var sun = new Rhino.Render.Sun();

      sun.North = northAngleDegrees;
      sun.SetPosition(when, latitudeDegrees, longitudeDegrees);

      return sun.Light;
    }

    /// <summary>
    /// Constructs a light which simulates a <see cref="Rhino.Render.Sun"/>.
    /// </summary>
    /// <param name="sun">A Sun object from the Rhino.Render namespace.</param>
    /// <returns>A light.</returns>
    /// <since>5.0</since>
    public static Light CreateSunLight(Render.Sun sun)
    {
      Runtime.HostUtils.CheckForRdk(true, true);

      return sun.Light;
    }
#endif

    internal Light(IntPtr nativePtr, object parent)
      : base(nativePtr, parent, -1)
    { }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Light(IntPtr.Zero, null);
    }

    /// <summary>
    /// Initializes a new light.
    /// </summary>
    /// <since>5.0</since>
    public Light()
    {
      IntPtr pLight = UnsafeNativeMethods.ON_Light_New();
      ConstructNonConstObject(pLight);
    }

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Light(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

#if RHINO_SDK
    internal override IntPtr _InternalGetConstPointer()
    {
      Rhino.DocObjects.Tables.LightTableEventArgs lte = m__parent as Rhino.DocObjects.Tables.LightTableEventArgs;
      if (lte != null)
        return lte.ConstLightPointer();

      var pipeline = m__parent as Display.DisplayPipeline;
      if( pipeline!=null )
      {
        IntPtr pipeline_ptr = pipeline.NonConstPointer();
        return UnsafeNativeMethods.CRhinoDisplayPipeline_GetLight(pipeline_ptr, m_subobject_index);
      }
      return base._InternalGetConstPointer();
    }

    internal Light(Display.DisplayPipeline parentPipeline, int index) : base(IntPtr.Zero, parentPipeline, index)
    {

    }
#endif

    /// <summary>
    /// Gets or sets a value that defines if the light is turned on (true) or off (false).
    /// </summary>
    /// <since>5.0</since>
    public bool IsEnabled
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_Light_IsEnabled(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_Light_SetEnabled(pThis, value);
      }
    }

    const int idxLightStyle = 0;
    const int idxCoordinateSystem = 1;
    int GetInt(int which)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.ON_Light_GetInt(pConstThis, which);
    }
    void SetInt(int which, int val)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_Light_SetInt(pThis, which, val);
    }

    /// <summary>
    /// Gets or sets a light style on this camera.
    /// </summary>
    /// <since>5.0</since>
    public LightStyle LightStyle
    {
      get { return (LightStyle)GetInt(idxLightStyle); }
      set { SetInt(idxLightStyle, (int)value); }
    }

    /// <summary>
    /// Gets a value indicating whether the light style
    /// is <see cref="LightStyle"/> CameraPoint or WorldPoint.
    /// </summary>
    /// <since>5.0</since>
    public bool IsPointLight
    {
      get
      {
        LightStyle ls = LightStyle;
        return ls == LightStyle.CameraPoint || ls == LightStyle.WorldPoint;
      }
    }

    /// <summary>
    /// Gets a value indicating whether the light style
    /// is <see cref="LightStyle"/> CameraDirectional or WorldDirectional.
    /// </summary>
    /// <since>5.0</since>
    public bool IsDirectionalLight
    {
      get
      {
        LightStyle ls = LightStyle;
        return ls == LightStyle.CameraDirectional || ls == LightStyle.WorldDirectional;
      }
    }

    /// <summary>
    /// Gets a value indicating whether the light style
    /// is <see cref="LightStyle"/> CameraSpot or WorldSpot.
    /// </summary>
    /// <since>5.0</since>
    public bool IsSpotLight
    {
      get
      {
        LightStyle ls = LightStyle;
        return ls == LightStyle.CameraSpot || ls == LightStyle.WorldSpot;
      }
    }

    /// <summary>
    /// Gets a value indicating whether the light style
    /// is <see cref="LightStyle"/> WorldLinear.
    /// </summary>
    /// <since>5.0</since>
    public bool IsLinearLight
    {
      get { return LightStyle == LightStyle.WorldLinear; }
    }

    /// <summary>
    /// Gets a value indicating whether the light style
    /// is <see cref="LightStyle"/> WorldRectangular.
    /// </summary>
    /// <since>5.0</since>
    public bool IsRectangularLight
    {
      get { return LightStyle == LightStyle.WorldRectangular; }
    }

#if RHINO_SDK
    /// <summary>
    /// Gets a value indicating whether this object is a Sun light.
    /// </summary>
    /// <since>5.0</since>
    public bool IsSunLight
    {
      get 
      {
        Runtime.HostUtils.CheckForRdk(true, true);
        var pointer = ConstPointer();
        return (UnsafeNativeMethods.RdkRhCmn_RhRdkIsSunLight(pointer) != 0);
      }
    }
#endif

    /// <summary>
    /// Gets a value, determined by LightStyle, that explains whether
    /// the camera directions are relative to World or Camera spaces.
    /// </summary>
    /// <since>5.0</since>
    public DocObjects.CoordinateSystem CoordinateSystem
    {
      get { return (DocObjects.CoordinateSystem)GetInt(idxCoordinateSystem);}
    }

    /// <summary>
    /// Gets or sets the light or 3D position or location.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Location
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        Point3d rc = new Point3d();
        UnsafeNativeMethods.ON_Light_GetLocation(pConstThis, ref rc);
        return rc;
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_Light_SetLocation(pThis, value);
      }
    }

    const int idxDirection = 0;
    const int idxPerpendicularDirection = 1;
    const int idxLength = 2;
    const int idxWidth = 3;
    Vector3d GetVector(int which)
    {
      IntPtr pConstThis = ConstPointer();
      Vector3d rc = new Vector3d();
      UnsafeNativeMethods.ON_Light_GetVector(pConstThis, ref rc, which);
      return rc;
    }
    void SetVector(int which, Vector3d v)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_Light_SetVector(pThis, v, which);
    }

    /// <summary>
    /// Gets or sets the vector direction of the camera.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Direction
    {
      get{ return GetVector(idxDirection); }
      set { SetVector(idxDirection, value); }
    }

    /// <summary>
    /// Gets a perpendicular vector to the camera direction.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d PerpendicularDirection
    {
      get { return GetVector(idxPerpendicularDirection); }
    }

    const int idxIntensity = 0;
    const int idxPowerWatts = 1;
    const int idxPowerLumens = 2;
    const int idxPowerCandela = 3;
    const int idxSpotAngleRadians = 4;
    const int idxSpotExponent = 5;
    const int idxHotSpot = 6;
    const int idxShadowIntensity = 7;
    double GetDouble(int which)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.ON_Light_GetDouble(pConstThis, which);
    }
    void SetDouble(int which, double val)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_Light_SetDouble(pThis, which, val);
    }

    /// <summary>
    /// Gets or sets the light intensity.
    /// </summary>
    /// <since>5.0</since>
    public double Intensity
    {
      get { return GetDouble(idxIntensity); }
      set { SetDouble(idxIntensity, value); }
    }

    /// <summary>
    /// Gets or sets the light power in watts (W).
    /// </summary>
    /// <since>5.0</since>
    public double PowerWatts
    {
      get { return GetDouble(idxPowerWatts); }
      set { SetDouble(idxPowerWatts, value); }
    }

    /// <summary>
    /// Gets or sets the light power in lumens (lm).
    /// </summary>
    /// <since>5.0</since>
    public double PowerLumens
    {
      get { return GetDouble(idxPowerLumens); }
      set { SetDouble(idxPowerLumens, value); }
    }

    /// <summary>
    /// Gets or sets the light power in candelas (cd).
    /// </summary>
    /// <since>5.0</since>
    public double PowerCandela
    {
      get { return GetDouble(idxPowerCandela); }
      set { SetDouble(idxPowerCandela, value); }
    }

    const int idxAmbient = 0;
    const int idxDiffuse = 1;
    const int idxSpecular = 2;
    System.Drawing.Color GetColor(int which)
    {
      IntPtr pConstThis = ConstPointer();
      int argb = UnsafeNativeMethods.ON_Light_GetColor(pConstThis, which);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(int which, System.Drawing.Color c)
    {
      IntPtr pThis = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_Light_SetColor(pThis, which, argb);
    }

    /// <summary>
    /// Gets or sets the ambient color.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color Ambient
    {
      get { return GetColor(idxAmbient); }
      set { SetColor(idxAmbient, value); }
    }

    /// <summary>
    /// Gets or sets the diffuse color.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_modifylightcolor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_modifylightcolor.cs' lang='cs'/>
    /// <code source='examples\py\ex_modifylightcolor.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public System.Drawing.Color Diffuse
    {
      get { return GetColor(idxDiffuse); }
      set { SetColor(idxDiffuse, value); }
    }

    /// <summary>
    /// Gets or sets the specular color.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color Specular
    {
      get { return GetColor(idxSpecular); }
      set { SetColor(idxSpecular, value); }
    }

    /// <summary>
    /// Sets the attenuation settings (ignored for "directional" and "ambient" lights).
    /// <para>attenuation = 1/(a0 + d*a1 + d^2*a2) where d = distance to light.</para>
    /// </summary>
    /// <param name="a0">The new constant attenuation divisor term.</param>
    /// <param name="a1">The new reverse linear attenuation divisor term.</param>
    /// <param name="a2">The new reverse quadratic attenuation divisor term.</param>
    /// <since>5.0</since>
    public void SetAttenuation(double a0, double a1, double a2)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_Light_SetAttenuation(pThis, a0, a1, a2);
    }

    /// <summary>
    /// Gets or Sets the attenuation vector.
    /// </summary>
    /// <since>5.7</since>
    public Vector3d AttenuationVector
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        Vector3d rc = new Vector3d();
        UnsafeNativeMethods.ON_Light_GetAttenuationVector(ptr_const_this, ref rc);
        return rc;
      }

      set
      {
        SetAttenuation(value.X, value.Y, value.Z);
      }
    }

    /// <summary>
    /// Gets the attenuation settings (ignored for "directional" and "ambient" lights).
    /// <para>attenuation = 1/(a0 + d*a1 + d^2*a2) where d = distance to light.</para>
    /// </summary>
    /// <param name="d">The distance to evaluate.</param>
    /// <returns>0 if a0 + d*a1 + d^2*a2 &lt;= 0.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double GetAttenuation(double d)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.ON_Light_GetAttenuation(pConstThis, d);
    }

    /// <summary>
    /// Gets or sets the spot angle in radians.
    /// <para>Ignored for non-spot lights.</para>
    /// <para>angle = 0 to pi/2  (0 to 90 degrees).</para>
    /// </summary>
    /// <since>5.0</since>
    public double SpotAngleRadians
    {
      get{ return GetDouble(idxSpotAngleRadians); }
      set{ SetDouble(idxSpotAngleRadians, value); }
    }

    /// <summary>
    /// The spot exponent varies from 0.0 to 128.0 and provides
    /// an exponential interface for controlling the focus or 
    /// concentration of a spotlight (like the 
    /// OpenGL GL_SPOT_EXPONENT parameter).  The spot exponent
    /// and hot spot parameters are linked; changing one will
    /// change the other.
    /// A hot spot setting of 0.0 corresponds to a spot exponent of 128.
    /// A hot spot setting of 1.0 corresponds to a spot exponent of 0.0.
    /// </summary>
    /// <since>5.0</since>
    public double SpotExponent
    {
      get { return GetDouble(idxSpotExponent); }
      set { SetDouble(idxSpotExponent, value); }
    }

    /// <summary>
    /// The hot spot setting runs from 0.0 to 1.0 and is used to
    /// provides a linear interface for controlling the focus or 
    /// concentration of a spotlight.
    /// A hot spot setting of 0.0 corresponds to a spot exponent of 128.
    /// A hot spot setting of 1.0 corresponds to a spot exponent of 0.0.
    /// </summary>
    /// <since>5.0</since>
    public double HotSpot
    {
      get { return GetDouble(idxHotSpot); }
      set { SetDouble(idxHotSpot, value); }
    }

    /// <summary>
    /// Gets the spot light radii.
    /// </summary>
    /// <param name="innerRadius">The inner radius. This out parameter is assigned during this call.</param>
    /// <param name="outerRadius">The outer radius. This out parameter is assigned during this call.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool GetSpotLightRadii(out double innerRadius, out double outerRadius)
    {
      innerRadius = 0;
      outerRadius = 0;
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.ON_Light_GetSpotLightRadii(pConstThis, ref innerRadius, ref outerRadius);
    }

    /// <summary>
    /// Gets or sets the height in linear and rectangular lights.
    /// <para>(ignored for non-linear/rectangular lights.)</para>
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Length
    {
      get { return GetVector(idxLength); }
      set { SetVector(idxLength, value); }
    }

    /// <summary>
    /// Gets or sets the width in linear and rectangular lights.
    /// <para>(ignored for non-linear/rectangular lights.)</para>
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Width
    {
      get { return GetVector(idxWidth); }
      set { SetVector(idxWidth, value); }
    }

    /// <summary>
    /// Gets or sets the spot light shadow intensity.
    /// <para>(ignored for non-spot lights.)</para>
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use ShadowIntensity")]
    public double SpotLightShadowIntensity
    {
      get { return GetDouble(idxShadowIntensity); }
      set { SetDouble(idxShadowIntensity, value); }
    }

    /// <summary>
    /// Gets or sets the shadow intensity for the light.
    /// </summary>
    /// <since>6.0</since>
    public double ShadowIntensity
    {
        get { return GetDouble(idxShadowIntensity); }
        set { SetDouble(idxShadowIntensity, value); }
    }

    /// <summary>
    /// Gets or sets the spot light name.
    /// </summary>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.ON_Light_GetName(pConstThis, pString);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_Light_SetName(pThis, value);
      }
    }

    /// <summary>Gets the ID of this light.</summary>
    /// <since>6.0</since>
    public Guid Id
    {
      get
      {
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Light_ModelObjectId(ptr_const_this);
      }

      set 
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_Light_SetModelObjectId(pThis, value);
      }
    }

    #region Attenuation helper functions and enum
    /// <summary>
    /// Vector denoting a constant light attenuation.
    /// </summary>
    public static readonly Vector3d ConstantAttenuationVector = new Vector3d(1, 0, 0);
    /// <summary>
    /// Vector denoting a linear light attenuation.
    /// </summary>
    public static readonly Vector3d LinearAttenuationVector = new Vector3d(0, 1, 0);
    /// <summary>
    /// Vector denoting an inverse squared light attenuation.
    /// </summary>
    public static readonly Vector3d InverseSquaredAttenuationVector = new Vector3d(0, 0, 1);

    /// <summary>
    /// Types of light attenuation available.
    /// </summary>
    public enum Attenuation {
      /// <summary>
      /// Constant light attenuation, meaning no light energy fall-off.
      /// </summary>
      Constant,
      /// <summary>
      /// Linear light attenuation, meaning linear light energy fall-off.
      /// </summary>
      Linear,
      /// <summary>
      /// Inverse squared light attenuation, meaning light energy falls off in spherical order.
      /// </summary>
      InverseSquared,
    }

    /// <summary>
    /// Get the type of attenuation for this light.
    /// </summary>
    /// <since>7.0</since>
    public Attenuation AttenuationType {
      get {
        var att = Attenuation.Constant;
        var vec = AttenuationVector;
        if (vec.Equals(ConstantAttenuationVector))
        {
          att = Attenuation.Constant;
        }
        else if (vec.Equals(LinearAttenuationVector))
        {
          att = Attenuation.Linear;
        }
        else if (vec.Equals(InverseSquaredAttenuationVector))
        {
          att = Attenuation.InverseSquared;
        }
        return att;
      }
      set {
        switch(value) {
          case Attenuation.Linear:
            AttenuationVector = LinearAttenuationVector;
            break;
          case Attenuation.InverseSquared:
            AttenuationVector = InverseSquaredAttenuationVector;
            break;
          default: // defaulting to constant since that is what pre-rhino7 used to support only anyway
            AttenuationVector = ConstantAttenuationVector;
            break;
        }
      }
    }
    #endregion
  }
}
