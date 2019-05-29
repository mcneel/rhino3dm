#if RHINO_SDK
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public sealed class NamedValue
  {
    public NamedValue(string name, object value)
    {
      m_name = name;
      m_value = new Rhino.Render.Variant(value);
    }

    private string m_name = String.Empty;
    private readonly Variant m_value = new Variant();

    public string Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

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

  internal class XMLSectionUtilities
  {
    //Push the List<Rhino.Render.NamedValue> values into the IRhRdk_XMLSection
    public static void SetFromNamedValueList(IntPtr pXmlSection, List<Rhino.Render.NamedValue> list)
    {
      if (pXmlSection == IntPtr.Zero)
        return;

      if (null == list)
        return;

      foreach (Rhino.Render.NamedValue nv in list)
      {
        Rhino.Render.Variant variant = new Rhino.Render.Variant(nv.Value);
        UnsafeNativeMethods.Rdk_XmlSection_SetParam(pXmlSection, nv.Name, variant.ConstPointer());
      }
    }
          
    public static List<NamedValue> ConvertToNamedValueList(IntPtr pXmlSection)
    {
      if (IntPtr.Zero == pXmlSection)
        return null;

      List<Rhino.Render.NamedValue> list = new List<NamedValue>();

      IntPtr pIterator = UnsafeNativeMethods.Rdk_XmlSection_GetIterator(pXmlSection);

      //Fill the property list from the XML section
      if (pIterator != IntPtr.Zero)
      {
        while (true)
        {
          using (var sh = new StringHolder())
          {
            Variant variant = new Variant();

            if (1 == UnsafeNativeMethods.Rdk_XmlSection_NextParam(pXmlSection, pIterator, sh.ConstPointer(), variant.ConstPointer()))
            {
              NamedValue nv = new NamedValue(sh.ToString(), variant.AsObject());
              list.Add(nv);
            }
            else
              break;
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
    [CLSCompliant(false)]
    public static Color4f ToColor4f(this IConvertible variant)
    {
      var v = variant as Variant;
      if (v != null)
      {
        return v.ToColor4f();
      }
      return Color4f.Empty;
    }

    [CLSCompliant(false)]
    public static Vector2d ToVector2d(this IConvertible variant)
    {
      var v = variant as Variant;
      if (v != null)
      {
        return v.ToVector2d();
      }
      return new Vector2d();
    }

    [CLSCompliant(false)]
    public static Vector3d ToVector3d(this IConvertible variant)
    {
      var v = variant as Variant;
      if (v != null)
      {
        return v.ToVector3d();
      }
      return new Vector3d();
    }
  }

  /*public*/ sealed class Variant : IDisposable, IConvertible
  {
    public enum VariantTypes : int
    {
      Null = 0,
      Bool = 1,
      Integer = 2,
      Float = 3,
      Double = 4,
      Color = 5,
      Vector2d = 6,
      Vector3d = 7,
      String = 8,
      Pointer = 9,
      Uuid = 10,
      Matrix = 11,
      Time = 12,
      Buffer = 13,
      Point4d = 14,
    }

    internal static Variant CopyFromPointer(IntPtr pVariant)
    {
      if (pVariant == IntPtr.Zero)
        return null;
      Variant v = new Variant();
      UnsafeNativeMethods.Rdk_Variant_Copy(pVariant, v.NonConstPointer());
      return v;
    }

    internal void CopyToPointer(IntPtr pOtherVarient)
    {
      IntPtr ptr_const_this = ConstPointer();
      UnsafeNativeMethods.Rdk_Variant_Copy(ptr_const_this, pOtherVarient);
    }


    /// <summary>
    /// Constructs as VariantTypes.Null.
    /// </summary>
    public Variant()
    {
      m_ptr_variant = UnsafeNativeMethods.Rdk_Variant_New(IntPtr.Zero);
      m_auto_delete = true;
    }

    internal Variant(IntPtr pVariant)
    {
      m_ptr_variant = pVariant;
      m_auto_delete = false;
    }

    #region constructors
    public Variant(int v) : this() { SetValue(v); }
    public Variant(bool v) : this() { SetValue(v); }
    public Variant(float v) : this() { SetValue(v); }
    public Variant(double v) : this() { SetValue(v); }
    public Variant(string v) : this() { SetValue(v); }
    public Variant(System.Drawing.Color v) : this() { SetValue(v); }
    public Variant(Rhino.Display.Color4f v) : this() { SetValue(v); }
    public Variant(Rhino.Geometry.Vector2d v) : this() { SetValue(v); }
    public Variant(Rhino.Geometry.Vector3d v) : this() { SetValue(v); }
    public Variant(Rhino.Geometry.Point4d v) : this() { SetValue(v); }
    //public Variant(IntPtr v)                  : this()  { SetValue(v); }
    public Variant(Guid v) : this() { SetValue(v); }
    public Variant(Rhino.Geometry.Transform v) : this() { SetValue(v); }
    public Variant(byte[] v) : this() { SetValue(v); }
    public Variant(object v) : this() { SetValue(v); }
    public Variant(DateTime v) : this() { SetValue(v); }
    #endregion

    /// <summary>
    /// Units associated with numeric values, see AsModelFloat etc.
    /// </summary>
    public Rhino.UnitSystem Units
    {
      get
      {
        return (Rhino.UnitSystem)UnsafeNativeMethods.Rdk_Variant_GetUnits(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_Variant_SetUnits(NonConstPointer(), (int)value);
      }
    }

    public bool IsNull
    {
      get
      {
        return 1 == UnsafeNativeMethods.Rdk_Variant_IsNull(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_Variant_SetNull(NonConstPointer());
      }
    }

    public bool Varies
    {
      get
      {
        return 1 == UnsafeNativeMethods.Rdk_Variant_Varies(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_Variant_SetVaries(NonConstPointer());
      }
    }

    public VariantTypes Type
    {
      get
      {
        return (VariantTypes)UnsafeNativeMethods.Rdk_Variant_Type(ConstPointer());
      }
    }

    #region value setters
    public void SetValue(int v)
    { UnsafeNativeMethods.Rdk_Variant_SetIntValue(NonConstPointer(), v); }

    public void SetValue(bool v)
    { UnsafeNativeMethods.Rdk_Variant_SetBoolValue(NonConstPointer(), v); }

    public void SetValue(double v)
    { UnsafeNativeMethods.Rdk_Variant_SetDoubleValue(NonConstPointer(), v); }

    public void SetValue(float v)
    { UnsafeNativeMethods.Rdk_Variant_SetFloatValue(NonConstPointer(), v); }

    public void SetValue(string v)
    { UnsafeNativeMethods.Rdk_Variant_SetStringValue(NonConstPointer(), v); }

    public void SetValue(System.Drawing.Color v)
    { UnsafeNativeMethods.Rdk_Variant_SetOnColorValue(NonConstPointer(), v.ToArgb()); }

    public void SetValue(Rhino.Display.Color4f v)
    { UnsafeNativeMethods.Rdk_Variant_SetRdkColorValue(NonConstPointer(), v); }

    public void SetValue(Rhino.Geometry.Vector2d v)
    { UnsafeNativeMethods.Rdk_Variant_Set2dVectorValue(NonConstPointer(), v); }

    public void SetValue(Rhino.Geometry.Vector3d v)
    { UnsafeNativeMethods.Rdk_Variant_Set3dVectorValue(NonConstPointer(), v); }

    public void SetValue(Rhino.Geometry.Point2d p)
    {
      var value = new Geometry.Vector2d(p.X, p.Y);
      UnsafeNativeMethods.Rdk_Variant_Set2dVectorValue(NonConstPointer(), value);
    }

    public void SetValue(Rhino.Geometry.Point3d p)
    {
      var value = new Geometry.Vector3d(p.X, p.Y, p.Z);
      UnsafeNativeMethods.Rdk_Variant_Set3dVectorValue(NonConstPointer(), value);
    }

    public void SetValue(Rhino.Geometry.Point4d v)
    { UnsafeNativeMethods.Rdk_Variant_Set4dPointValue(NonConstPointer(), v); }

    //public void SetValue(IntPtr v)
    //{ UnsafeNativeMethods.Rdk_Variant_SetPointerValue(NonConstPointer(), v); }

    public void SetValue(Guid v)
    { UnsafeNativeMethods.Rdk_Variant_SetUuidValue(NonConstPointer(), v); }

    public void SetValue(Rhino.Geometry.Transform v)
    { UnsafeNativeMethods.Rdk_Variant_SetXformValue(NonConstPointer(), v); }

    public void SetValue(byte[] v)
    { UnsafeNativeMethods.Rdk_Variant_SetByteArrayValue(NonConstPointer(), v, null == v ? 0 : v.Length); }

    public void SetValue(DateTime v)
    {
      DateTime startTime = new DateTime(1970, 1, 1);
      UInt32 time_t = Convert.ToUInt32(Math.Abs((DateTime.Now - startTime).TotalSeconds));
      UnsafeNativeMethods.Rdk_Variant_SetTimeValue(NonConstPointer(), time_t);
    }

    public void SetValue(object v)
    {
      Fields.Field field = v as Fields.Field;
      if (field != null)
      {
        SetValue(field.ValueAsObject());
        return;
      }

      if (v is Rhino.Render.Variant)
      {
        SetValue((v as Variant).AsObject());
      }
      else if (v is bool) SetValue((bool)v);
      else if (v is int) SetValue((int)v);
      else if (v is float) SetValue((float)v);
      else if (v is double) SetValue((double)v);
      else if (v is string) SetValue((string)v);
      else if (v is System.Drawing.Color) SetValue((System.Drawing.Color)v);
      else if (v is Rhino.Display.Color4f) SetValue((Rhino.Display.Color4f)v);
      else if (v is Rhino.Geometry.Vector2d) SetValue((Rhino.Geometry.Vector2d)v);
      else if (v is Rhino.Geometry.Vector3d) SetValue((Rhino.Geometry.Vector3d)v);
      else if (v is Rhino.Geometry.Point2d) SetValue((Geometry.Point2d)v);
      else if (v is Rhino.Geometry.Point3d) SetValue((Geometry.Point3d)v);
      else if (v is Rhino.Geometry.Point4d) SetValue((Rhino.Geometry.Point4d)v);
      else if (v is Guid) SetValue((Guid)v);
      else if (v is Rhino.Geometry.Transform) SetValue((Rhino.Geometry.Transform)v);
      else if (v is DateTime) SetValue((DateTime)v);
      else if (v is byte[]) SetValue((byte[])v);
      else if (v == null) IsNull = true;
      else
        throw new InvalidOperationException("Type not supported for Rhino.Rhino.Variant");
    }


    #endregion

    #region value getters
    public object AsObject()
    {
      VariantTypes vt = Type;

      switch (vt)
      {
        case VariantTypes.Null:
          return null;
        case VariantTypes.Bool:
          return ToBoolean(null);
        case VariantTypes.Integer:
          return ToInt32(null);
        case VariantTypes.Float:
          return ToSingle(null);
        case VariantTypes.Double:
          return ToDouble(null);
        case VariantTypes.Color:
          return ToColor4f();
        case VariantTypes.Vector2d:
          return ToVector2d();
        case VariantTypes.Vector3d:
          return ToVector3d();
        case VariantTypes.Point4d:
          return ToPoint4d();
        case VariantTypes.String:
          return ToString(null);
        case VariantTypes.Uuid:
          return ToGuid();
        case VariantTypes.Matrix:
          return ToTransform();
        case VariantTypes.Time:
          return ToDateTime(null);
      }
      throw new InvalidOperationException("Type not supported by Rhino.Render.Variant");
    }

    public System.Drawing.Color ToSystemColor()
    {
      return System.Drawing.Color.FromArgb(UnsafeNativeMethods.Rdk_Variant_GetOnColorValue(ConstPointer()));
    }

    public Rhino.Display.Color4f ToColor4f()
    {
      Rhino.Display.Color4f v = new Rhino.Display.Color4f();
      UnsafeNativeMethods.Rdk_Variant_GetRdkColorValue(ConstPointer(), ref v);
      return v;
    }

    public Rhino.Geometry.Vector2d ToVector2d()
    {
      Rhino.Geometry.Vector2d v = new Rhino.Geometry.Vector2d();
      UnsafeNativeMethods.Rdk_Variant_Get2dVectorValue(ConstPointer(), ref v);
      return v;
    }

    public Rhino.Geometry.Vector3d ToVector3d()
    {
      Rhino.Geometry.Vector3d v = new Rhino.Geometry.Vector3d();
      UnsafeNativeMethods.Rdk_Variant_Get3dVectorValue(ConstPointer(), ref v);
      return v;
    }

    public Rhino.Geometry.Point4d ToPoint4d()
    {
      Rhino.Geometry.Point4d v = new Rhino.Geometry.Point4d();
      UnsafeNativeMethods.Rdk_Variant_Get4dPointValue(ConstPointer(), ref v);
      return v;
    }

    //public IntPtr AsPointer()
    //{ return UnsafeNativeMethods.Rdk_Variant_GetPointerValue(ConstPointer()); }

    public Guid ToGuid()
    { return UnsafeNativeMethods.Rdk_Variant_GetUuidValue(ConstPointer()); }

    public Rhino.Geometry.Transform ToTransform()
    {
      Rhino.Geometry.Transform v = new Rhino.Geometry.Transform();
      UnsafeNativeMethods.Rdk_Variant_GetXformValue(ConstPointer(), ref v);
      return v;
    }

    #endregion

    #region units support
    /// <summary>
    /// Retrieves the value as a float in model units. Null or varying returns 0.0.
    /// The value will be converted from the variant's units to model units if necessary.
    /// \see Units(). \see SetUnits().
    /// </summary>
    /// <param name="document">A Rhino document.</param>
    /// <returns>The value in model units.
    /// <para>This is a single-precision value.</para>
    /// </returns>
    public float AsModelFloat(Rhino.RhinoDoc document)
    {
      return UnsafeNativeMethods.Rdk_Variant_AsModelFloat(ConstPointer(), document.RuntimeSerialNumber);
    }

    /// <summary>
    /// Retrieves the value as a double in model units. Null or varying returns 0.0.
    /// The value will be converted from the variant's units to model units if necessary.
    /// \see Units(). \see SetUnits().
    /// </summary>
    /// <param name="document">A Rhino document.</param>
    /// <returns>The value in model units.
    /// <para>This is a double-precision value.</para></returns>
    public double AsModelDouble(Rhino.RhinoDoc document)
    {
      return UnsafeNativeMethods.Rdk_Variant_AsModelDouble(ConstPointer(), document.RuntimeSerialNumber);
    }

    /// <summary>
    /// Sets the value to a float in model units.
    /// The value will be converted from model units to the variant's units if necessary. 
    /// </summary>
    /// <param name="f">The value in model units.</param>
    /// <param name="document">A Rhino document.</param>
    public void SetAsModelFloat(float f, Rhino.RhinoDoc document)
    {
      UnsafeNativeMethods.Rdk_Variant_SetAsModelFloat(NonConstPointer(), f, document.RuntimeSerialNumber);
    }

    /// <summary>
    /// Sets the value to a double in model units.
    /// The value will be converted from model units to the variant's units if necessary. 
    /// </summary>
    /// <param name="d">The value in model units.</param>
    /// <param name="document">A Rhino document.</param>
    public void SetAsModelDouble(double d, Rhino.RhinoDoc document)
    {
      UnsafeNativeMethods.Rdk_Variant_SetAsModelDouble(NonConstPointer(), d, document.RuntimeSerialNumber);
    }
    #endregion

    #region internals
    private IntPtr m_ptr_variant = IntPtr.Zero;
    private bool m_auto_delete = true;
    internal IntPtr ConstPointer()
    {
      return m_ptr_variant;
    }
    internal IntPtr NonConstPointer()
    {
      return m_ptr_variant;
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
      if (m_auto_delete && m_ptr_variant!=IntPtr.Zero)
      {
        UnsafeNativeMethods.Rdk_Variant_Delete(m_ptr_variant);
        m_ptr_variant = IntPtr.Zero;
        m_auto_delete = false;
      }
    }
    #endregion

    #region IConvertible implementation
    public TypeCode GetTypeCode()
    {
      return TypeCode.Object;
    }

    public bool ToBoolean(IFormatProvider provider)
    {
      IntPtr const_ptr_this = ConstPointer();
      return 1 == UnsafeNativeMethods.Rdk_Variant_GetBoolValue(const_ptr_this);
    }

    public byte ToByte(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public char ToChar(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public DateTime ToDateTime(IFormatProvider provider)
    {
      DateTime dt = new DateTime(1970, 1, 1);
      IntPtr const_ptr_this = ConstPointer();
      dt = dt.AddSeconds(UnsafeNativeMethods.Rdk_Variant_GetTimeValue(const_ptr_this));
      return dt;
    }

    public decimal ToDecimal(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public double ToDouble(IFormatProvider provider)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.Rdk_Variant_GetDoubleValue(const_ptr_this);
    }

    public short ToInt16(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public int ToInt32(IFormatProvider provider)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.Rdk_Variant_GetIntValue(const_ptr_this);
    }

    public long ToInt64(IFormatProvider provider)
    {
      // TODO: support longs
      return (long)ToInt32(provider);
    }

    public sbyte ToSByte(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public float ToSingle(IFormatProvider provider)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.Rdk_Variant_GetFloatValue(const_ptr_this);
    }

    public string ToString(IFormatProvider provider)
    {
      using (var sh = new StringHolder())
      {
        UnsafeNativeMethods.Rdk_Variant_GetStringValue(ConstPointer(), sh.NonConstPointer());
        return sh.ToString();
      }
    }

    public object ToType(Type conversionType, IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public ushort ToUInt16(IFormatProvider provider)
    {
      throw new NotImplementedException();
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
    #endregion
  }

}
#endif