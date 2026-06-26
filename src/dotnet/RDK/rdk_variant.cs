
#pragma warning disable 1591

using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using Rhino.FileIO;

namespace Rhino.Render
{
  public sealed class NamedValue
  {
    /// <since>5.1</since>
    public NamedValue(string name, object value)
    {
      m_name = name;
      m_value = new Rhino.Render.Variant(value);
    }

    private string m_name = string.Empty;
    private readonly Variant m_value = new Variant();

    /// <since>5.1</since>
    public string Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    /// <since>5.1</since>
    public object Value
    {
      get
      {
        return m_value.AsObject();
      }
      set
      {
        m_value.SetValue(value);
      }
    }
  }

#if RHINO_SDK
  internal class XMLSectionUtilities
  {
    //Push the List<Rhino.Render.NamedValue> values into the IRhRdk_XMLSection
    public static void SetFromNamedValueList(IntPtr pXmlSection, List<Rhino.Render.NamedValue> list)
    {
      if ((pXmlSection == IntPtr.Zero) || (null == list))
        return;

      foreach (var item in list) // [MARKER]
      {
        var variant = new Variant(item.Value);
        UnsafeNativeMethods.Rdk_XmlSection_SetParam(pXmlSection, item.Name, variant.ConstPointer());
        GC.KeepAlive(variant);

      }
    }

    public static List<NamedValue> ConvertToNamedValueList(IntPtr pXmlSection)
    {
      var list = new List<NamedValue>();

      var pIterator = UnsafeNativeMethods.Rdk_XmlSection_GetIterator(pXmlSection);
      if (pIterator != IntPtr.Zero)
      {
        using (var sh = new StringHolder())
        {
          using (var variant = new Variant())
          {
            while (UnsafeNativeMethods.Rdk_XmlSection_NextParam(pXmlSection, pIterator, sh.ConstPointer(), variant.ConstPointer()))
            {
              list.Add(new NamedValue(sh.ToString(), variant.AsObject()));
            }
          }
        }

        UnsafeNativeMethods.Rdk_XmlSection_DeleteIterator(pIterator);
      }

      return list;
    }
  }

  /// <summary>
  /// Extension methods for IConvertible that work when
  /// an object is a Variant.
  /// </summary>
  public static class ConvertibleExtensions
  {
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static Color4f ToColor4f(this IConvertible variant)
    {
      if (variant is Variant v)
      {
        return v.ToColor4f();
      }
      return Color4f.Empty;
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static Vector2d ToVector2d(this IConvertible variant)
    {
      if (variant is Variant v)
      {
        return v.ToVector2d();
      }
      return new Vector2d();
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static Vector3d ToVector3d(this IConvertible variant)
    {
      if (variant is Variant v)
      {
        return v.ToVector3d();
      }
      return new Vector3d();
    }
  }
#endif

  internal sealed class Variant : IDisposable, IConvertible
  {
    #region internals
    private IntPtr m_native_ptr = IntPtr.Zero; // ON_XMLVariant
    private bool m_auto_delete = true;

    internal IntPtr ConstPointer()    { return m_native_ptr; }
    internal IntPtr NonConstPointer() { return m_native_ptr; }
    #endregion

    public enum Types : int // Same order as enum in ON_XMLVariant.
    {
      Null, Bool, Integer, Float, Double, String, Vector2d, Vector3d,
      Point4d, Color, Xform, Uuid, Time, Buffer,
    }

    internal static Variant CopyFromPointer(IntPtr pVariant)
    {
      if (pVariant == IntPtr.Zero)
        return null;

      var v = new Variant();
      UnsafeNativeMethods.ON_XMLVariant_Copy(pVariant, v.NonConstPointer());
      return v;
    }

    internal void CopyToPointer(IntPtr pOtherVarient)
    {
      IntPtr ptr_const_this = ConstPointer();
      UnsafeNativeMethods.ON_XMLVariant_Copy(ptr_const_this, pOtherVarient);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Constructs as Null type.
    /// </summary>
    public Variant()
    {
      m_native_ptr = UnsafeNativeMethods.ON_XMLVariant_New();
    }

    public Variant(IntPtr native)
    {
      m_native_ptr = native;
      m_auto_delete = false;
    }

    #region constructors
    public Variant(int v)       : this() { SetValue(v); }
    public Variant(bool v)      : this() { SetValue(v); }
    public Variant(float v)     : this() { SetValue(v); }
    public Variant(double v)    : this() { SetValue(v); }
    public Variant(string v)    : this() { SetValue(v); }
    public Variant(Color v)     : this() { SetValue(v); }
    public Variant(Color4f v)   : this() { SetValue(v); }
    public Variant(Vector2d v)  : this() { SetValue(v); }
    public Variant(Vector3d v)  : this() { SetValue(v); }
    public Variant(Point4d v)   : this() { SetValue(v); }
    public Variant(Guid v)      : this() { SetValue(v); }
    public Variant(Transform v) : this() { SetValue(v); }
    public Variant(DateTime v)  : this() { SetValue(v); }
    public Variant(byte[] v)    : this() { SetValue(v); }
    public Variant(object v)    : this() { SetValue(v); }
    #endregion

    /// <summary>
    /// Units associated with numeric values, see AsModelFloat etc.
    /// </summary>
    public Rhino.UnitSystem Units
    {
      get
      {
        var ret = (Rhino.UnitSystem)UnsafeNativeMethods.ON_XMLVariant_GetUnits(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
      set
      {
        UnsafeNativeMethods.ON_XMLVariant_SetUnits(NonConstPointer(), (int)value);
        GC.KeepAlive(this);
      }
    }

    public bool IsNull
    {
      get
      {
        var ret = (1 == UnsafeNativeMethods.ON_XMLVariant_IsNull(ConstPointer()));
        GC.KeepAlive(this);
        return ret;
      }
      set
      {
        UnsafeNativeMethods.ON_XMLVariant_SetNull(NonConstPointer());
        GC.KeepAlive(this);
      }
    }

    public bool Varies
    {
      get
      {
        var ret = ( 1 == UnsafeNativeMethods.ON_XMLVariant_Varies(ConstPointer()));
        GC.KeepAlive(this);
        return ret;
      }
      set
      {
        UnsafeNativeMethods.ON_XMLVariant_SetVaries(NonConstPointer());
        GC.KeepAlive(this);
      }
    }

    public Types Type
    {
      get
      {
        var ret = (Types)UnsafeNativeMethods.ON_XMLVariant_Type(ConstPointer());
        GC.KeepAlive(this);
        return ret;
      }
    }

    #region value setters
    public void SetValue(int v)
    { UnsafeNativeMethods.ON_XMLVariant_SetIntValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(bool v)
    { UnsafeNativeMethods.ON_XMLVariant_SetBoolValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(double v)
    { UnsafeNativeMethods.ON_XMLVariant_SetDoubleValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(float v)
    { UnsafeNativeMethods.ON_XMLVariant_SetFloatValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(string v)
    { UnsafeNativeMethods.ON_XMLVariant_SetStringValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Color v)
    { UnsafeNativeMethods.ON_XMLVariant_SetOnColorValue(NonConstPointer(), v.ToArgb()); GC.KeepAlive(this); }

    public void SetValue(Color4f v)
    { UnsafeNativeMethods.ON_XMLVariant_SetColor4fValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Vector2d v)
    { UnsafeNativeMethods.ON_XMLVariant_Set2dVectorValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Vector3d v)
    { UnsafeNativeMethods.ON_XMLVariant_Set3dVectorValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Point2d p)
    {
      var value = new Vector2d(p.X, p.Y);
      UnsafeNativeMethods.ON_XMLVariant_Set2dVectorValue(NonConstPointer(), value);
      GC.KeepAlive(this);
    }

    public void SetValue(Point3d p)
    {
      var value = new Vector3d(p.X, p.Y, p.Z);
      UnsafeNativeMethods.ON_XMLVariant_Set3dVectorValue(NonConstPointer(), value);
      GC.KeepAlive(this);
    }

    public void SetValue(Point4d v)
    { UnsafeNativeMethods.ON_XMLVariant_Set4dPointValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Guid v)
    { UnsafeNativeMethods.ON_XMLVariant_SetUuidValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(Transform v)
    { UnsafeNativeMethods.ON_XMLVariant_SetXformValue(NonConstPointer(), v); GC.KeepAlive(this); }

    public void SetValue(byte[] v)
    { UnsafeNativeMethods.ON_XMLVariant_SetByteArrayValue(NonConstPointer(), v, null == v ? 0 : v.Length); GC.KeepAlive(this); }

    public void SetValue(DateTime v)
    {
      // 22nd March 2023 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-73674
      // This variant is used by the automatic UI to interoperate with the C++ RDK code.
      // In C++ we specify time_t as UTC and CTime is used extensively. CTime accepts time_t as UTC
      // and always converts it to the local time the computer is in if you call out any individual
      // item like day or hour. Therefore, we must always assume any time_t value is expressed in UTC.
      // The commented out code below doesn't work properly because it specifies that the start date
      // is Local, not UTC. In any case, there's no need to do this calculation here because there is
      // already a utility function TimeHelpers.ToUnixEpoch().
      // See also https://en.cppreference.com/w/c/chrono/time_t
      //DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
      //UInt32 time_t = Convert.ToUInt32(Math.Abs((v - startTime).TotalSeconds));

      var time_t = (UInt32)TimeHelpers.ToUnixEpoch(v);
      UnsafeNativeMethods.ON_XMLVariant_SetTimeValue(NonConstPointer(), time_t);
      GC.KeepAlive(this);
    }

    public void SetValue(object v)
    {
#if RHINO_SDK
      if (v is Fields.Field fd) SetValue(fd.ValueAsObject()); else
#endif
           if (v is Variant vt)      SetValue(vt.AsObject());
      else if (v is bool   b)        SetValue(b);
      else if (v is int    i)        SetValue(i);
      else if (v is float  f)        SetValue(f);
      else if (v is double d)        SetValue(d);
      else if (v is string s)        SetValue(s);
      else if (v is Color c)         SetValue(c);
      else if (v is Color4f c4)      SetValue(c4);
      else if (v is Vector2d v2)     SetValue(v2);
      else if (v is Vector3d v3)     SetValue(v3);
      else if (v is Point2d p2)      SetValue(p2);
      else if (v is Point3d p3)      SetValue(p3);
      else if (v is Point4d p4)      SetValue(p4);
      else if (v is Guid g)          SetValue(g);
      else if (v is Transform t)     SetValue(t);
      else if (v is DateTime dt)     SetValue(dt);
      else if (v is byte[] ba)       SetValue(ba);
      else if (v == null) IsNull = true;
      else throw new InvalidOperationException("Type not supported for Rhino.Render.Variant");

      GC.KeepAlive(v);
    }

    #endregion

    #region value getters
    public object AsObject()
    {
      switch (Type)
      {
        case Types.Null:     return null;
        case Types.Bool:     return ToBoolean(null);
        case Types.Integer:  return ToInt32(null);
        case Types.Float:    return ToSingle(null);
        case Types.Double:   return ToDouble(null);
        case Types.Color:    return ToColor4f();
        case Types.Vector2d: return ToVector2d();
        case Types.Vector3d: return ToVector3d();
        case Types.Point4d:  return ToPoint4d();
        case Types.String:   return ToString(null);
        case Types.Uuid:     return ToGuid();
        case Types.Xform:    return ToTransform();
        case Types.Time:     return ToDateTime(null);
        default: throw new InvalidOperationException("Type not supported by Rhino.Render.Variant");
      }
    }

    public Color ToSystemColor()
    {
      var ret = Color.FromArgb(UnsafeNativeMethods.ON_XMLVariant_GetOnColorValue(ConstPointer()));
      GC.KeepAlive(this);
      return ret;
    }

    public Color4f ToColor4f()
    {
      var v = new Color4f();
      UnsafeNativeMethods.ON_XMLVariant_GetColor4fValue(ConstPointer(), ref v);
      GC.KeepAlive(this);
      return v;
    }

    public Vector2d ToVector2d()
    {
      var v = new Vector2d();
      UnsafeNativeMethods.ON_XMLVariant_Get2dVectorValue(ConstPointer(), ref v);
      GC.KeepAlive(this);
      return v;
    }

    public Vector3d ToVector3d()
    {
      var v = new Vector3d();
      UnsafeNativeMethods.ON_XMLVariant_Get3dVectorValue(ConstPointer(), ref v);
      GC.KeepAlive(this);
      return v;
    }

    public Point4d ToPoint4d()
    {
      var v = new Point4d();
      UnsafeNativeMethods.ON_XMLVariant_Get4dPointValue(ConstPointer(), ref v);
      GC.KeepAlive(this);
      return v;
    }

    public Guid ToGuid()
    { 
      var ret = UnsafeNativeMethods.ON_XMLVariant_GetUuidValue(ConstPointer());
      GC.KeepAlive(this);
      return ret;
    }

    public Transform ToTransform()
    {
      var v = new Transform();
      UnsafeNativeMethods.ON_XMLVariant_GetXformValue(ConstPointer(), ref v);
      GC.KeepAlive(this);
      return v;
    }
    #endregion

    #region IDisposable pattern implementation
    ~Variant()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (m_auto_delete && (m_native_ptr != IntPtr.Zero))
      {
        UnsafeNativeMethods.ON_XMLVariant_Delete(m_native_ptr);
        GC.KeepAlive(this);
        m_native_ptr = IntPtr.Zero;
        m_auto_delete = false;
      }
    }
    #endregion

    #region IConvertible implementation
    public TypeCode GetTypeCode()
    {
      return TypeCode.Object;
    }

    public bool ToBool()
    {
      var ret = UnsafeNativeMethods.ON_XMLVariant_GetBoolValue(ConstPointer()) != 0;
      GC.KeepAlive(this);
      return ret;
    }

    public int ToInt()
    {
      var ret = UnsafeNativeMethods.ON_XMLVariant_GetIntValue(ConstPointer());
      GC.KeepAlive(this);
      return ret;
    }

    public float ToFloat()
    {
      var ret = UnsafeNativeMethods.ON_XMLVariant_GetFloatValue(ConstPointer());
      GC.KeepAlive(this);
      return ret;
    }

    public double ToDouble()
    {
      var ret = UnsafeNativeMethods.ON_XMLVariant_GetDoubleValue(ConstPointer());
      GC.KeepAlive(this);
      return ret;
    }

    public byte ToByte(IFormatProvider provider)
    {
      return (byte)ToInt32(provider);
    }

    public sbyte ToSByte(IFormatProvider provider)
    {
      return (sbyte)ToInt32(provider);
    }

    public char ToChar(IFormatProvider provider)
    {
      return (char)ToInt32(provider);
    }

    public short ToInt16(IFormatProvider provider)
    {
      return (short)ToInt32(provider);
    }

    public long ToInt64(IFormatProvider provider)
    {
      // TODO: support longs
      return (long)ToInt32(provider);
    }

    public decimal ToDecimal(IFormatProvider provider)
    {
      return (decimal)ToInt32(provider);
    }

    public string ToString(IFormatProvider provider)
    {
      return ToString();
    }

    public bool ToBoolean(IFormatProvider provider)
    {
      return ToBool();
    }

    public float ToSingle(IFormatProvider provider)
    {
      return ToFloat();
    }

    public double ToDouble(IFormatProvider provider)
    {
      return ToDouble();
    }

    public int ToInt32(IFormatProvider provider)
    {
      return ToInt();
    }

    public ushort ToUInt16(IFormatProvider provider)
    {
      return (ushort)ToInt16(provider);
    }

    public uint ToUInt32(IFormatProvider provider)
    {
      //TODO: support unsigned ints
      return (uint)ToInt32(provider);
    }

    public ulong ToUInt64(IFormatProvider provider)
    {
      //TODO: support unsigned long
      return (ulong)ToInt64(provider);
    }

    public new string ToString()
    {
      using (var sw = new StringWrapper())
      {
        UnsafeNativeMethods.ON_XMLVariant_GetStringValue(ConstPointer(), sw.NonConstPointer);
        GC.KeepAlive(this);
        return sw.ToString();
      }
    }

    public DateTime ToDateTime()
    {
      // 22nd March 2023 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-73674
      // This variant is used by the automatic UI to interoperate with the C++ RDK code.
      // In C++ we specify time_t as UTC and CTime is used extensively. CTime accepts time_t as UTC
      // and always converts it to the local time the computer is in if you call out any individual
      // item like day or hour. Therefore, we must always assume any time_t value is expressed in UTC.
      // The commented out code below doesn't work properly because it doesn't specify that the start
      // date is UTC. In any case, there's no need to do this calculation here because there is already
      // a utility function TimeHelpers.FromUnixEpoch().
      // See also https://en.cppreference.com/w/c/chrono/time_t
      //DateTime dt = new DateTime(1970, 1, 1);
      //dt = dt.AddSeconds(UnsafeNativeMethods.ON_XMLVariant_GetTimeValue(const_ptr_this));
      //return dt;

      var time_t = (ulong)UnsafeNativeMethods.ON_XMLVariant_GetTimeValue(ConstPointer());
      GC.KeepAlive(this);
      return TimeHelpers.FromUnixEpoch(time_t);
    }

    public DateTime ToDateTime(IFormatProvider provider)
    {
      return ToDateTime();
    }

    public object ToType(Type type, IFormatProvider provider)
    {
           if (type == typeof(bool))      return ToBoolean(provider);
      else if (type == typeof(int))       return ToInt32(provider);
      else if (type == typeof(float))     return ToSingle(provider);
      else if (type == typeof(double))    return ToDouble(provider);
      else if (type == typeof(Color4f))   return ToColor4f();
      else if (type == typeof(Vector2d))  return ToVector2d();
      else if (type == typeof(Vector3d))  return ToVector3d();
      else if (type == typeof(Point4d))   return ToPoint4d();
      else if (type == typeof(string))    return ToString(provider);
      else if (type == typeof(Transform)) return ToTransform();
      else if (type == typeof(DateTime))  return ToDateTime(provider);

      throw new NotImplementedException();
    }

    #endregion
  }
}
