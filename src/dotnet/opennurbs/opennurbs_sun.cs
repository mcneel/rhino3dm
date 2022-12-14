using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the sun in this file.
  /// </summary>
  public class File3dmSun
  {
    readonly File3dm _parent;

    internal File3dmSun(File3dm parent)
    {
      _parent = parent;
    }

    ///<summary>
    /// <return>The minimum allowed year for sun methods.</return>
    /// </summary>
    public static int MinYear => UnsafeNativeMethods.ON_Sun_GetMinYear();

    ///<summary>
    /// <return>The maximum allowed year for sun methods.</return>
    /// </summary>
    public static int MaxYear => UnsafeNativeMethods.ON_Sun_GetMaxYear();

    /// <summary>
    /// Returns true if all the sun parameters are valid.
    /// </summary>
    public bool IsValid
    {
      get => UnsafeNativeMethods.ON_Sun_GetIsValid(_parent.ConstPointer());
    }

    /// <summary>
    /// If enabling/disabling the sun is allowed.
    /// </summary>
    public bool EnableAllowed
    {
      get => UnsafeNativeMethods.ON_Sun_GetEnableAllowed(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetEnableAllowed(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Sun enabled state.
    /// </summary>
    public bool EnableOn
    {
      get => UnsafeNativeMethods.ON_Sun_GetEnableOn(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetEnableOn(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// If manual control of the sun position is allowed.
    /// </summary>
    public bool ManualControlAllowed
    {
      get => UnsafeNativeMethods.ON_Sun_GetManualControlAllowed(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetManualControlAllowed(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// If manual control of the sun position is in effect.
    /// </summary>
    public bool ManualControlOn
    {
      get => UnsafeNativeMethods.ON_Sun_GetManualControlOn(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetManualControlOn(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// World angle corresponding to North in degrees.
    /// This angle is zero along the x-axis and increases anticlockwise.
    /// </summary>
    public double North
    {
      get => UnsafeNativeMethods.ON_Sun_GetNorth(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetNorth(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// The sun's direction vector in world space, taking into account the sun's azimuth
    /// and altitude, and the direction of north. See Azimuth, Altitude, North.
    /// Note that this does not actually calculate the azimuth or altitude from the place and time;
    /// it merely returns the values that were stored in the model.
    /// </summary>
    public Geometry.Vector3d Vector
    {
      get
      {
        var vec = new Geometry.Vector3d();
        UnsafeNativeMethods.ON_Sun_GetVector(_parent.ConstPointer(), ref vec);
        return vec;
      }

      set { UnsafeNativeMethods.ON_Sun_SetVector(_parent.NonConstPointer(), ref value); }
    }

    /// <summary>
    /// Azimuth of the sun in degrees. The value increases Eastwards with North as zero.
    /// Note: This value is not affected by the direction of north. See North.
    /// </summary>
    public double Azimuth
    {
      get => UnsafeNativeMethods.ON_Sun_GetAzimuth(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetAzimuth(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Altitude of the sun in degrees.
    /// </summary>
    public double Altitude
    {
      get => UnsafeNativeMethods.ON_Sun_GetAltitude(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetAltitude(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Latitude of the observer.
    /// </summary>
    public double Latitude
    {
      get => UnsafeNativeMethods.ON_Sun_GetLatitude(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetLatitude(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// LLongitude of the observer.
    /// </summary>
    public double Longitude
    {
      get => UnsafeNativeMethods.ON_Sun_GetLongitude(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetLongitude(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Time zone of the observer in decimal hours.
    /// </summary>
    public double TimeZone
    {
      get => UnsafeNativeMethods.ON_Sun_GetTimeZone(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetTimeZone(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Daylight saving state.
    /// </summary>
    public bool DaylightSavingOn
    {
      get => UnsafeNativeMethods.ON_Sun_GetDaylightSavingOn(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetDaylightSavingOn(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Daylight saving offset of the observer in minutes.
    /// </summary>
    public int DaylightSavingMinutes
    {
      get => UnsafeNativeMethods.ON_Sun_GetDaylightSavingMinutes(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetDaylightSavingMinutes(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Gets the local date and time of the observer.
    /// <param name="year">Accepts the year in the range MinYear to MaxYear.</param>
    /// <param name="month">Accepts the month in the range 1 to 12.</param>
    /// <param name="day">Accepts the day in the range 1 to 31.</param>
    /// <param name="hours">Accepts the time expressed as decimal hours in the range 0 to 24.</param>
    /// </summary>
    public bool LocalDateTime(out int year, out int month, out int day, out double hours)
    {
      year = month = day = 0;
      hours = 0.0;
      return UnsafeNativeMethods.ON_Sun_GetLocalDateTime(_parent.ConstPointer(), ref year, ref month, ref day, ref hours);
    }

    /// <summary>
    /// Sets the local date and time of the observer.
    /// <param name="year">The year in the range MinYear to MaxYear.</param>
    /// <param name="month">The month in the range 1 to 12.</param>
    /// <param name="day">The day in the range 1 to 31.</param>
    /// <param name="hours">The time expressed as decimal hours in the range 0 to 24.</param>
    /// </summary>
    public void SetLocalDateTime(int year, int month, int day, double hours)
    {
      UnsafeNativeMethods.ON_Sun_SetLocalDateTime(_parent.NonConstPointer(), year, month, day, hours);
    }

    /// <summary>
    /// Intensity to be used for the sun. This is 1.0 by default.
    /// </summary>
    public double Intensity
    {
      get => UnsafeNativeMethods.ON_Sun_GetIntensity(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetIntensity(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Shadow intensity to be used for the sun. 
    /// This is 1.0 by default. 0.0 turns off all shadows.
    /// </summary>
    public double ShadowIntensity
    {
      get => UnsafeNativeMethods.ON_Sun_GetShadowIntensity(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Sun_SetShadowIntensity(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Returns a CRC calculated from the information that defines the object.
    /// This CRC can be used as a quick way to see if two objects are not identical.
    /// </summary>
    /// <param name="currentRemainder">The current remainder value.</param>
    /// <returns>CRC of the information the defines the object.</returns>
    [CLSCompliant(false)]
    public uint DataCRC(uint currentRemainder)
    {
      return UnsafeNativeMethods.ON_Sun_GetDataCRC(_parent.ConstPointer(), currentRemainder);
    }

    /// <summary>
    /// Get a light object which represents the sun. Note that this does not actually calculate the sun's
    /// azimuth or altitude from the place and time; it merely uses the values that were stored in the model.
    /// </summary>
    public Geometry.Light Light()
    {
      var light = new Geometry.Light();
      UnsafeNativeMethods.ON_Sun_GetLight(_parent.ConstPointer(), light.NonConstPointer());
      return light;
    }

    /// <summary>
    /// Get a color for rendering a sun light when the sun is at a particular altitude in the sky.
    /// </summary>
    public static System.Drawing.Color SunColorFromAltitude(double altitude)
    {
      var col = UnsafeNativeMethods.ON_Sun_GetSunColorFromAltitude(altitude);
      return Runtime.Interop.ColorFromWin32(col);
    }
  }
}
