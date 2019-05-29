using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

#pragma warning disable 1591

// TODO: Add #if def for quick removal if we don't want this class in SDK
// TODO: Remove V6 suffix once we have this class 'done'

namespace Rhino.DocObjects
{

  //public class DimstyleTableV6
  //{
  //  private DimstyleTableV6()
  //  {}

  //  static
  //  public int DimstyleCount(RhinoDoc doc)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_Count(doc.RuntimeSerialNumber);
  //  }

  //  static
  //  public List<string> GetStyleNames(RhinoDoc doc)
  //  {
  //    int count = DimstyleCount(doc);
  //    List<string> names = new List<string>();
  //    for (int i = 0; i < count; i++)
  //    {
  //      using (var sh = new StringHolder())
  //      {
  //        IntPtr pString = sh.NonConstPointer();
  //        if (UnsafeNativeMethods.DimstyleTableV6_GetDimstyleName(doc.RuntimeSerialNumber, pString, i))
  //          names.Add(sh.ToString());
  //      }
  //    }
  //    return names;
  //  }

  //  static
  //  public AnnotationStyle GetAt(RhinoDoc doc, int i)
  //  {
  //    IntPtr ptr = UnsafeNativeMethods.DimstyleTableV6_GetAt(doc.RuntimeSerialNumber, i);
  //    return new AnnotationStyle(ptr);
  //  }

  //  static
  //  public AnnotationStyle GetCurrentStyle(RhinoDoc doc)
  //  {
  //    IntPtr ptr = UnsafeNativeMethods.DimstyleTableV6_GetCurrentStyle(doc.RuntimeSerialNumber);
  //    return new AnnotationStyle(ptr);
  //  }

  //  static
  //  public int GetCurrentStyleIndex(RhinoDoc doc)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_GetCurrentStyleIndex(doc.RuntimeSerialNumber);
  //  }

  //  static
  //  public void SetCurrentStyleIndex(RhinoDoc doc, int i)
  //  {
  //    UnsafeNativeMethods.DimstyleTableV6_SetCurrentStyleIndex(doc.RuntimeSerialNumber, i);
  //  }

  //  static
  //  public int FindStyle(RhinoDoc doc, string name)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_FindStyleByName(doc.RuntimeSerialNumber, name);
  //  }

  //  static
  //  public int FindStyle(RhinoDoc doc, Guid id)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_FindStyleById(doc.RuntimeSerialNumber, id);
  //  }

  //  static
  //  public bool ReplaceStyle(RhinoDoc doc, int si, AnnotationStyle style)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_ReplaceStyle(doc.RuntimeSerialNumber, si, style.ConstPointer());
  //  }

  //  static
  //  public int AppendStyle(RhinoDoc doc, AnnotationStyle style)
  //  {
  //    return UnsafeNativeMethods.DimstyleTableV6_AppendStyle(doc.RuntimeSerialNumber, style.ConstPointer());
  //  }
  //}


  //public partial class AnnotationStyle : IDisposable
  //{

  //  public ManagedFont Font
  //  {
  //    get
  //    {
  //      uint font_sn = UnsafeNativeMethods.ON_V6_Dimstyle_GetFontSn(ConstPointer());
  //      return new ManagedFont(font_sn);
  //    }
  //    set { UnsafeNativeMethods.ON_V6_Dimstyle_SetFontSn(NonConstPointer(), value.SerialNumber); }
  //  }

  //  [CLSCompliant(false)]
  //  public Bitmap GetPreviewBitmap(uint doc_sn, int width, int height)
  //  {
  //    Bitmap bitmap = null;
  //    IntPtr dibptr = UnsafeNativeMethods.ON_V6_Dimstyle_GetPreview_Bitmap(ConstPointer(), doc_sn, width, height);
  //    IntPtr hbitmap = UnsafeNativeMethods.CRhinoDib_Bitmap(dibptr);
  //    if( IntPtr.Zero != hbitmap)
  //      bitmap = System.Drawing.Image.FromHbitmap(hbitmap);
  //    UnsafeNativeMethods.CRhinoDib_Delete(dibptr);
  //    return  bitmap;
  //  }

  //  #region guid properties

  //  public Guid ArrowBlockId1
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetArrowBlockId1(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArrowBlockId1(NonConstPointer(), value); }
  //  }

  //  public Guid DimArrowBlock2
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetArrowBlockId2(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArrowBlockId2(NonConstPointer(), value); }
  //  }

  //  public Guid LeaderArrowBlock
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetLeaderArrowBlockId(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLeaderArrowBlockId(NonConstPointer(), value); }
  //  }

  //  #endregion guid properties

  //  #region bool properties

  //  public bool SuppressExtension1
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetSuppressExtension1(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetSuppressExtension1(NonConstPointer(), value); }
  //  }

  //  public bool SuppressExtension2
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetSuppressExtension2(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetSuppressExtension2(NonConstPointer(), value); }
  //  }

  //  public bool SuppressArrow1
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetSuppressArrow1(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetSuppressArrow1(NonConstPointer(), value); }
  //  }

  //  public bool SuppressArrow2
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetSuppressArrow2(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetSuppressArrow2(NonConstPointer(), value); }
  //  }

  //  public bool AlternateUnitsDisplay
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAlternateDisplay(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlternateDisplay(NonConstPointer(), value); }
  //  }

  //  public bool DrawTextMask
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetDrawTextMask(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetDrawTextMask(NonConstPointer(), value); }
  //  }

  //  public bool FixedExtensionOn
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetFixedExtensionOn(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetFixedExtensionOn(NonConstPointer(), value); }
  //  }

  //  #endregion

  //  #region double properties

  //  public double ExtensionLineExtension
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetExtensionLineExtension(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetExtensionLineExtension(NonConstPointer(), value); }
  //  }

  //  public double ExtensionLineOffset
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetExtensionLineOffset(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetExtensionLineOffset(NonConstPointer(), value); }
  //  }

  //  public double DimensionLineExtension
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetExtensionLineExtension(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetExtensionLineExtension(NonConstPointer(), value); }
  //  }

  //  public double ArrowLength
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetArrowSize(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArrowSize(NonConstPointer(), value); }
  //  }

  //  public double LeaderArrowLength
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetLeaderArrowSize(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLeaderArrowSize(NonConstPointer(), value); }
  //  }

  //  public double CentermarkSize
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetCenterMark(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetCenterMark(NonConstPointer(), value); }
  //  }

  //  public double TextGap
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetTextGap(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetTextGap(NonConstPointer(), value); }
  //  }

  //  public double TextHeight
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetTextHeight(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetTextHeight(NonConstPointer(), value); }
  //  }

  //  public double LengthFactor
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetLengthFactor(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLengthFactor(NonConstPointer(), value); }
  //  }

  //  public double AlternateLengthFactor
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAlternateLengthFactor(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlternateLengthFactor(NonConstPointer(), value); }
  //  }

  //  public double ToleranceUpperValue
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetToleranceUpperValue(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceUpperValue(NonConstPointer(), value); }
  //  }

  //  public double ToleranceLowerValue
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetToleranceLowerValue(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceLowerValue(NonConstPointer(), value); }
  //  }

  //  public double ToleranceHeightScale
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetToleranceHeightScale(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceHeightScale(NonConstPointer(), value); }
  //  }

  //  public double BaselineSpacing
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetBaselineSpacing(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetBaselineSpacing(NonConstPointer(), value); }
  //  }

  //  public double DimensionScale
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetDimensionScale(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetDimensionScale(NonConstPointer(), value); }
  //  }

  //  public double FixedExtensionLength
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetFixedExtensionLength(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetFixedExtensionLength(NonConstPointer(), value); }
  //  }

  //  public double TextRotation
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetTextRotation(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetTextRotation(NonConstPointer(), value); }
  //  }

  //  public double StackHeightScale
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetStackHeightScale(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetStackHeightScale(NonConstPointer(), value); }
  //  }

  //  public double Roundoff
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetRound(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetRound(NonConstPointer(), value); }
  //  }

  //  public double AlternateRoundoff
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAltRound(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAltRound(NonConstPointer(), value); }
  //  }

  //  public double AngularRoundoff
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAngularRound(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAngularRound(NonConstPointer(), value); }
  //  }

  //  #endregion

  //  #region enum properties

  //  /// <summary> </summary>
  //  public TextDisplayAlignment TextAlignment
  //  {
  //    get { return (TextDisplayAlignment) UnsafeNativeMethods.ON_Dimstyle_GetTextAlignment(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetTextAlignment(NonConstPointer(), (int)value); }
  //  }

  //  /// <summary> </summary>
  //  public LengthDisplayFormat LengthFormat
  //  {
  //    get { return (LengthDisplayFormat) UnsafeNativeMethods.ON_Dimstyle_GetLengthFormat(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLengthFormat(NonConstPointer(), (uint) value); }
  //  }

  //  /// <summary> </summary>
  //  public LengthDisplayFormat AlternateLengthFormat
  //  {
  //    get { return (LengthDisplayFormat)UnsafeNativeMethods.ON_Dimstyle_GetAlternateLengthFormat(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlternateLengthFormat(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public AngleDisplayFormat AngleFormat
  //  {
  //    get { return (AngleDisplayFormat)UnsafeNativeMethods.ON_Dimstyle_GetAlternateAngleFormat(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAngleFormat(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ToleranceDisplayFormat ToleranceFormat
  //  {
  //    get { return (ToleranceDisplayFormat)UnsafeNativeMethods.ON_Dimstyle_GetToleranceFormat(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceFormat(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary>
  //  /// Should be ON::object_color_source
  //  ///  </summary>
  //  public int MaskColorSource
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetMaskColorSource(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetMaskColorSource(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public StackDisplayFormat StackFractionFormat
  //  {
  //    get { return (StackDisplayFormat)UnsafeNativeMethods.ON_Dimstyle_GetStackFormat(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetStackFormat(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ZeroSuppression ZeroSuppress
  //  {
  //    get { return (ZeroSuppression)UnsafeNativeMethods.ON_Dimstyle_GetZeroSuppress(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetZeroSuppress(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ZeroSuppression AlternateZeroSuppress
  //  {
  //    get { return (ZeroSuppression)UnsafeNativeMethods.ON_Dimstyle_GetAlternateZeroSuppress(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlternateZeroSuppress(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ZeroSuppression ToleranceZeroSuppress
  //  {
  //    get { return (ZeroSuppression)UnsafeNativeMethods.ON_Dimstyle_GetToleranceZeroSuppress(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceZeroSuppress(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ZeroSuppression AngleZeroSuppress
  //  {
  //    get { return (ZeroSuppression)UnsafeNativeMethods.ON_Dimstyle_GetAngleZeroSuppress(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAngleZeroSuppress(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ArrowType ArrowType1
  //  {
  //    get { return (ArrowType)UnsafeNativeMethods.ON_Dimstyle_GetArrowType1(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArrowType1(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public ArrowType ArrowType2
  //  {
  //    get { return (ArrowType)UnsafeNativeMethods.ON_Dimstyle_GetArrowType2(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArrowType2(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public AnnotationStyle.ArrowType LeaderArrowType
  //  {
  //    get { return (ArrowType)UnsafeNativeMethods.ON_Dimstyle_GetLeaderArrowType(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLeaderArrowType(NonConstPointer(), (uint)value); }
  //  }

  //  /// <summary> </summary>
  //  public int TextMoveLeader
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetTextMoveLeader(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetTextMoveLeader(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public int ArcLengthSymbol
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetArcLengthSymbol(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetArcLengthSymbol(NonConstPointer(), value); }
  //  }


  //  #endregion

  //  #region int properties

  //  /// <summary> </summary>
  //  public int LengthResolution
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetLengthResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLengthResolution(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public int AlternateLengthResolution
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAlternateLengthResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlternateLengthResolution(NonConstPointer(), value); }
  //  }

  //  public int AngleResolution
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAngleResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAngleResolution(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public int ToleranceResolution
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetToleranceResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetToleranceResolution(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public int AlternateToleranceResolution
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetAlternateToleranceResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetAlterateToleranceResolution(NonConstPointer(), value); }
  //  }

  //  /// <summary> </summary>
  //  public int MaskColor
  //  {
  //    get { return UnsafeNativeMethods.ON_Dimstyle_GetLengthResolution(ConstPointer()); }
  //    set { UnsafeNativeMethods.ON_Dimstyle_SetLengthResolution(NonConstPointer(), value); }
  //  }


  //  #endregion

  //  #region string properties

  //  public string Name
  //  {
  //    get
  //    {
  //      using(var sh = new StringHolder())
  //      {
  //        UnsafeNativeMethods.ON_DimStyle_GetName(ConstPointer(), sh.NonConstPointer());
  //        return sh.ToString();
  //      }
  //    }
  //    set { UnsafeNativeMethods.ON_DimStyle_SetName(NonConstPointer(), value); }
  //  }

  //  public string Prefix
  //  {
  //    get
  //    {
  //      using(var sh = new StringHolder())
  //      {
  //        UnsafeNativeMethods.ON_DimStyle_GetPrefix(ConstPointer(), sh.NonConstPointer());
  //        return sh.ToString();
  //      }
  //    }
  //    set { UnsafeNativeMethods.ON_DimStyle_SetPrefix(NonConstPointer(), value); }
  //  }

  //  public string Suffix
  //  {
  //    get
  //    {
  //      using(var sh = new StringHolder())
  //      {
  //        UnsafeNativeMethods.ON_DimStyle_GetSuffix(ConstPointer(), sh.NonConstPointer());
  //        return sh.ToString();
  //      }
  //    }
  //    set { UnsafeNativeMethods.ON_DimStyle_SetSuffix(NonConstPointer(), value); }
  //  }

  //  public string AlternatePrefix
  //  {
  //    get
  //    {
  //      using(var sh = new StringHolder())
  //      {
  //        UnsafeNativeMethods.ON_DimStyle_GetAlternatePrefix(ConstPointer(), sh.NonConstPointer());
  //        return sh.ToString();
  //      }
  //    }
  //    set { UnsafeNativeMethods.ON_DimStyle_SetAlternatePrefix(NonConstPointer(), value); }
  //  }

  //  public string AlternateSuffix
  //  {
  //    get
  //    {
  //      using(var sh = new StringHolder())
  //      {
  //        UnsafeNativeMethods.ON_DimStyle_GetAlternateSuffix(ConstPointer(), sh.NonConstPointer());
  //        return sh.ToString();
  //      }
  //    }
  //    set { UnsafeNativeMethods.ON_DimStyle_SetAlternateSuffix(NonConstPointer(), value); }
  //  }

  //  #endregion string properties

  //}
}