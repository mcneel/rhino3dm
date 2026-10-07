using Rhino.Runtime.InteropWrappers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Rhino
{
  /// <summary>
  /// Represents a length unit.
  /// </summary>
  /// <since>9.0</since>
  [DebuggerDisplay("{DebuggerDisplay, nq}")]
  public readonly struct LengthUnit : IEquatable<LengthUnit>, IComparable<LengthUnit>
  {
    #region Static
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private static readonly double[] MetersPerUnitSystem = BuildMetersPerUnitSystem();
    private static double[] BuildMetersPerUnitSystem()
    {
      var metersPerUnitSystem = new double[byte.MaxValue + 1];
      for (int i = 1; i < metersPerUnitSystem.Length; i++)
        metersPerUnitSystem[i] = RhinoMath.MetersPerUnit((UnitSystem)i);

      return metersPerUnitSystem;
    }
    #endregion

    #region Fields
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private readonly UnitSystem _UnitSystem;

    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private readonly string _Name;

    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private readonly double _MetersPerUnit;

    /// <summary>The size in meters of this unit for scale purposes.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private double Magnitude => _UnitSystem == UnitSystem.None ? 1.0 : _MetersPerUnit;
    #endregion

    #region Debugger
    [DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    private string DebuggerDisplay => _UnitSystem != UnitSystem.CustomUnits ? Name : $"{Name} ({_MetersPerUnit} m)";
    #endregion

    #region System.Object
    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => _MetersPerUnit.GetHashCode();

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>true if the current object is equal to the <paramref name="other"/> object otherwise false.</returns>
    public override bool Equals(object other) => other is LengthUnit unit && Equals(unit);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>true if the current object is equal to the <paramref name="other"/> object otherwise false.</returns>
    /// <remarks>Name is ignored on the comparison (Meters and Metres are considered the same)</remarks>
    /// <since>9.0</since>
    public bool Equals(LengthUnit other)
    {
      if (_UnitSystem != other._UnitSystem) return false;
      if (_UnitSystem != UnitSystem.CustomUnits) return true;

      return _MetersPerUnit == other._MetersPerUnit;
    }

    /// <summary>
    /// Compares the current instance with another object of the same type and returns
    /// an integer that indicates whether the current instance precedes, follows, or
    /// occurs in the same position in the sort order as the other object.
    /// </summary>
    /// <param name="other">A <see cref="LengthUnit"/> to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the objects being compared.</returns>
    /// <remarks>Name is ignored on the comparison (Meters and Metres are considered the same)</remarks>
    /// <since>9.0</since>
    public int CompareTo(LengthUnit other) => _MetersPerUnit.CompareTo(other._MetersPerUnit);

    /// <summary>Equality operator.</summary>
    /// <since>9.0</since>
    public static bool operator ==(in LengthUnit left, in LengthUnit right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    /// <since>9.0</since>
    public static bool operator !=(in LengthUnit left, in LengthUnit right) => !left.Equals(right);

    /// <summary>Less than operator.</summary>
    /// <since>9.0</since>
    public static bool operator <(in LengthUnit left, in LengthUnit right) => left.CompareTo(right) < 0;
    
    /// <summary>Less than or equal operator.</summary>
    /// <since>9.0</since>
    public static bool operator <=(in LengthUnit left, in LengthUnit right) => left.CompareTo(right) <= 0;

    /// <summary>Greater than operator.</summary>
    /// <since>9.0</since>
    public static bool operator >(in LengthUnit left, in LengthUnit right) => left.CompareTo(right) > 0;

    /// <summary>Greater than or equal operator.</summary>
    /// <since>9.0</since>
    public static bool operator >=(in LengthUnit left, in LengthUnit right) => left.CompareTo(right) >= 0;
    #endregion

    #region Constructors
    private LengthUnit(UnitSystem unitSystem, string name = null, double metersPerUnit = double.NaN)
    {
      _UnitSystem = unitSystem;
      _Name = name;
      _MetersPerUnit = double.IsNaN(metersPerUnit) ? MetersPerUnitSystem[(int) _UnitSystem] : Math.Abs(metersPerUnit);
    }
    #endregion

    #region Known Units
    private static readonly ISet<UnitSystem> KnownUnitSystems = new SortedSet<UnitSystem>
    (
      ((UnitSystem[])Enum.GetValues(typeof(UnitSystem))).
      Where(x => x != UnitSystem.Unset && x != UnitSystem.CustomUnits).
      OrderBy(x => MetersPerUnitSystem[(int)x])
    );

    /// <summary>Unset unit</summary>
    public static readonly LengthUnit Unset = new LengthUnit(UnitSystem.Unset);
    /// <summary>No unit</summary>
    public static readonly LengthUnit None = new LengthUnit(UnitSystem.None);

    /// <summary>1 millimeter = 1.0e-3 meters</summary>
    public static readonly LengthUnit Millimeters = new LengthUnit(UnitSystem.Millimeters);
    /// <summary>1 millimeter = 1.0e-2 meters</summary>
    public static readonly LengthUnit Centimeters = new LengthUnit(UnitSystem.Centimeters);
    /// <summary>SI meter length unit</summary>
    public static readonly LengthUnit Meters = new LengthUnit(UnitSystem.Meters);
    /// <summary>1 kilometer = 1.0e+3 meters</summary>
    public static readonly LengthUnit Kilometers = new LengthUnit(UnitSystem.Kilometers);

    /// <summary>1 inch = 0.0254 meters = 1/12 foot</summary>
    public static readonly LengthUnit Inches = new LengthUnit(UnitSystem.Inches);
    /// <summary>1 foot = 0.3048  meters = 12 inches</summary>
    public static readonly LengthUnit Feet = new LengthUnit(UnitSystem.Feet);
    /// <summary>1 yard = 0.9144 meters = 3 feet</summary>
    public static readonly LengthUnit Yards = new LengthUnit(UnitSystem.Yards);
    /// <summary>1 yard = 1609.344 meters = 5280 feet</summary>
    public static readonly LengthUnit Miles = new LengthUnit(UnitSystem.Miles);
    #endregion

    #region Properties
    /// <summary>Non localized lower case plural unit name.</summary>
    /// <since>9.0</since>
    public string Name
    {
      get
      {
        if (_Name != null)
          return _Name;

        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.ON_LengthUnitSystem_Name(_UnitSystem, pString);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
    }

    /// <summary>Non localized unit symbol string.</summary>
    /// <since>9.0</since>
    public string Symbol
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(_Name))
          return _Name.ToLowerInvariant().Replace(" ", "-");

        switch (_UnitSystem)
        {
          case UnitSystem.None: return string.Empty;
          case UnitSystem.CustomUnits: return "cu";

          case UnitSystem.Angstroms: return "Å";
          case UnitSystem.Nanometers: return "nm";
          case UnitSystem.Microns: return "μm";
          case UnitSystem.Millimeters: return "mm";
          case UnitSystem.Centimeters: return "cm";
          case UnitSystem.Decimeters: return "dm";
          case UnitSystem.Meters: return "m";
          case UnitSystem.Dekameters: return "dam";
          case UnitSystem.Hectometers: return "hm";
          case UnitSystem.Kilometers: return "km";
          case UnitSystem.Megameters: return "Mm";
          case UnitSystem.Gigameters: return "Gm";
          case UnitSystem.NauticalMiles: return "nmi";

          case UnitSystem.Microinches: return "μin";
          case UnitSystem.Mils: return "mil";
          case UnitSystem.Inches: return "in";
          case UnitSystem.Feet: return "ft";
          case UnitSystem.Yards: return "yd";
          case UnitSystem.Miles: return "mi";
          case UnitSystem.PrinterPoints: return "points";
          case UnitSystem.PrinterPicas: return "picas";

          case UnitSystem.AstronomicalUnits: return "AU";
          case UnitSystem.LightYears: return "ly";
          case UnitSystem.Parsecs: return "pc";
        }

        return string.Empty;
      }
    }

#if RHINO_SDK
    /// <summary>Upper case plural localized unit name.</summary>
    /// <since>9.0</since>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string DisplayName
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(_Name))
        {
          var displayName = $"{_Name.Substring(0, 1).ToUpperInvariant()}{_Name.Substring(1)}";
          if (displayName.Length > 20) displayName = displayName.Substring(0, 19) + "…";
          return displayName;
        }

        return Rhino.UI.Localization.UnitSystemName(_UnitSystem, capitalize: true, singular: false, abbreviate: false);
      }
    }

    /// <summary> Lower case singular localized unit symbol.</summary>
    /// <since>9.0</since>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string DisplaySymbol
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(_Name))
          return _Name.ToLowerInvariant().Replace(" ", "-");

        return Rhino.UI.Localization.UnitSystemName(_UnitSystem, capitalize: false, singular: true, abbreviate: true);
      }
    }
#endif
    #endregion

    #region Methods
    /// <summary>
    /// Determines whether the specified value is None.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    /// <since>9.0</since>
    public static bool IsNone(in LengthUnit unit) => unit._UnitSystem == UnitSystem.None;

    /// <summary>
    /// Determines whether the specified value is Unset.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    /// <since>9.0</since>
    public static bool IsUnset(in LengthUnit unit) => unit._UnitSystem == UnitSystem.Unset;

    /// <summary>
    /// Determines whether the specified value is a custom unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    /// <since>9.0</since>
    public static bool IsCustom(in LengthUnit unit) => unit._UnitSystem == UnitSystem.CustomUnits;

    /// <summary>
    /// Determines whether the instance value is a known unit system.
    /// </summary>
    /// <remarks>
    /// Not <see cref="UnitSystem.Unset"/> nor <see cref="UnitSystem.CustomUnits"/>
    /// </remarks>
    /// <param name="unitSystem"></param>
    /// <returns>True when this instance value is a valid known <see cref="UnitSystem"/>, False otherwise.</returns>
    /// <since>9.0</since>
    public bool IsKnownUnitSystem(out UnitSystem unitSystem)
    {
      unitSystem = _UnitSystem;
      return unitSystem != UnitSystem.Unset && unitSystem != UnitSystem.CustomUnits && typeof(UnitSystem).IsEnumDefined(unitSystem);
    }

    /// <summary>
    /// Determines whether the provided value is a known unit system.
    /// </summary>
    /// <remarks>
    /// Not <see cref="UnitSystem.Unset"/> nor <see cref="UnitSystem.CustomUnits"/>
    /// </remarks>
    /// <param name="unitSystem"></param>
    /// <param name="lengthUnit"></param>
    /// <returns>True when provided <paramref name="lengthUnit"/> value is a valid known <see cref="UnitSystem"/>, False otherwise.</returns>
    /// <since>9.0</since>
    public static bool IsKnownLengthUnit(UnitSystem unitSystem, out LengthUnit lengthUnit)
    {
      lengthUnit = Unset;

      if (unitSystem == UnitSystem.Unset || unitSystem == UnitSystem.CustomUnits)
        return false;

      if ((unitSystem < UnitSystem.None || unitSystem > UnitSystem.Parsecs) && !typeof(UnitSystem).IsEnumDefined(unitSystem))
        return false;

      lengthUnit = new LengthUnit(unitSystem);
      return true;
    }

    /// <summary>
    /// Creates a custom <see cref="LengthUnit"/> based on its size expressed on a known <see cref="UnitSystem"/>.
    /// </summary>
    /// <param name="name">Name of the custom unit</param>
    /// <param name="customUnitSize">Meters per custom unit in case <paramref name="knownUnitSystem"/> is omitted</param>
    /// <param name="knownUnitSystem">A known unit system. Meters when omitted</param>
    /// <exception cref="ArgumentNullException">When <paramref name="name"/> is null</exception>
    /// <exception cref="ArgumentException">When <paramref name="customUnitSize"/> is not a positive finite number</exception>
    /// <since>9.0</since>
    public static LengthUnit FromCustomUnitSystem(string name, double customUnitSize, UnitSystem knownUnitSystem = UnitSystem.Meters)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      const double MinSize = double.MaxValue * double.Epsilon / 4.0;
      if (!(customUnitSize >= MinSize)) // Only positive finite numbers that are not denormalized here.
        throw new ArgumentException("Unit size should be a positive non denormalized value", nameof(customUnitSize));

      const double MaxSize = -RhinoMath.UnsetValue;
      if (!(customUnitSize < MaxSize))
        throw new ArgumentException("Unit size should be a positive finite value", nameof(customUnitSize));

      if (knownUnitSystem == UnitSystem.None || knownUnitSystem == UnitSystem.Unset || knownUnitSystem == UnitSystem.CustomUnits)
        throw new ArgumentException("Unit size should be expressed using a known unit system", nameof(knownUnitSystem));

      return new LengthUnit(UnitSystem.CustomUnits, name, customUnitSize * (knownUnitSystem == UnitSystem.Meters ? 1.0 : RhinoMath.UnitScale(knownUnitSystem, UnitSystem.Meters)));
    }

    /// <summary>
    /// Constructs a <see cref="LengthUnit"/> from a <see cref="UnitSystem"/>.
    /// </summary>
    /// <param name="knownUnitSystem">A known unit system.</param>
    /// <since>9.0</since>
    public static LengthUnit FromKnownUnitSystem(UnitSystem knownUnitSystem)
    {
      if (knownUnitSystem == UnitSystem.Unset) return Unset;
      if (knownUnitSystem == UnitSystem.CustomUnits) return None;

      if ((knownUnitSystem < UnitSystem.None || knownUnitSystem > UnitSystem.Parsecs) && !typeof(UnitSystem).IsEnumDefined(knownUnitSystem))
        return Unset;

      return new LengthUnit(knownUnitSystem);
    }

    /// <summary>
    /// Returns a <see cref="UnitSystem"/> equivalent to this instance and its size expressed on meters.
    /// </summary>
    /// <param name="metersPerUnit"> Unit size expressed in meters.</param>
    /// <returns>A <see cref="UnitSystem"/> value.</returns>
    /// <since>9.0</since>
    public UnitSystem ToUnitSystem(out double metersPerUnit)
    {
      metersPerUnit = _MetersPerUnit;
      return _UnitSystem;
    }

    /// <summary>
    /// Returns the most similar known <see cref="UnitSystem"/> to this instance.
    /// </summary>
    /// <param name="factor"></param>
    /// <returns></returns>
    internal UnitSystem? ToKnownUnitSystem(out double factor)
    {
      return ToUnitSystem(out factor, KnownUnitSystems);
    }

    /// <summary>
    /// Returns the most similar <see cref="UnitSystem"/> to this instance available on <paramref name="unitSystems"/> set.
    /// </summary>
    /// <param name="factor"></param>
    /// <param name="unitSystems"></param>
    /// <returns>A <see cref="UnitSystem"/> in the provided set of available <paramref name="unitSystems"/>.</returns>
    /// <since>9.0</since>
    public UnitSystem? ToUnitSystem(out double factor, ISet<UnitSystem> unitSystems)
    {
      if (unitSystems.Contains(_UnitSystem))
      {
        factor = 1.0;
        return _UnitSystem;
      }

      var candidates = unitSystems?.Select(FromKnownUnitSystem).Where(x => !IsUnset(x) && !IsNone(x)).ToArray();
      if (!(candidates?.Length > 0))
      {
        factor = double.NaN;
        return default;
      }

      if (!(unitSystems is SortedSet<UnitSystem>))
        Array.Sort(candidates);

      var index = Array.BinarySearch(candidates, this);
      if (index < 0)
      {
        index = ~index;
        if
        (
          index != 0 &&
          (index >= candidates.Length || Math.Abs(candidates[index - 1]._MetersPerUnit - _MetersPerUnit) < Math.Abs(candidates[index]._MetersPerUnit - _MetersPerUnit))
        )
          index--;

        factor = RhinoMath.UnitScale(_UnitSystem, _MetersPerUnit, candidates[index]._UnitSystem, 1.0);
        return candidates[index]._UnitSystem;
      }

      factor = 1.0;
      return _UnitSystem;
    }
    #endregion

    #region Size & Scale
    /// <summary>
    /// Returns the size of this unit expressed on a different length unit.
    /// </summary>
    /// <param name="unit">Size unit.</param>
    /// <returns>The size of this length unit expressed on the specified length <paramref name="unit"/>.</returns>
    /// <remarks>
    /// Size of None is always 1.0 expressed on any other length unit.<br/>
    /// Size of Unset is always NaN expressed on any other length unit.
    /// </remarks>
    /// <since>9.0</since>
    public double Size(in LengthUnit unit) => RhinoMath.UnitScale(_UnitSystem, _MetersPerUnit, unit._UnitSystem, unit._MetersPerUnit);

    /// <summary> 
    /// Computes the scale factor for changing a measurement on different units.
    /// </summary>
    /// <param name="from">The units to convert from.</param>
    /// <param name="to">The units to convert measurements into.</param>
    /// <returns>A unit scale factor.</returns>
    /// <since>9.0</since>
    public static double Scale(in LengthUnit from, in LengthUnit to) => RhinoMath.UnitScale(from._UnitSystem, from._MetersPerUnit, to._UnitSystem, to._MetersPerUnit);
    #endregion
  }
}
