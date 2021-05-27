#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initAnnotationBaseBindings(pybind11::module& m);
#else
void initAnnotationBaseBindings(void* m);
#endif

class BND_AnnotationBase : public BND_GeometryBase
{
  ON_Annotation* m_annotation = nullptr;
protected:
  void SetTrackedPointer(ON_Annotation* annotation, const ON_ModelComponentReference* compref);

public:
  BND_AnnotationBase(ON_Annotation* annotation, const ON_ModelComponentReference* compref);
  //public Guid DimensionStyleId {get;set;}
  //public bool HasPropertyOverrides {get;}
  //public bool IsPropertyOverridden(DimensionStyle.Field field)
  //public bool ClearPropertyOverrides()
  //public DimensionStyle GetDimensionStyle(DimensionStyle parentDimStyle)
  //public DimensionStyle DimensionStyle {get;}
  //public bool SetOverrideDimStyle(DimensionStyle OverrideStyle)
  //public DimensionStyle ParentDimensionStyle {get; set;}
  //public double TextHeight {get; set;}
  //public bool MaskEnabled { get;set;}
  //public bool MaskUsesViewportColor{ get; set; }
  //public DimensionStyle.MaskType MaskColorSource{ get; set; }
  //public bool DrawTextFrame{ get; set; }
  //public DimensionStyle.MaskFrame MaskFrame{ get; set; }
  //public Color MaskColor{ get; set; }
  //public double MaskOffset{ get; set; }
  //public double DimensionScale{ get; set; }
  //public bool DrawForward{ get; set; }
  //public DocObjects.Font Font{ get; set; }
  //public DimensionStyle.LengthDisplay DimensionLengthDisplay{ get; set; }
  //public DimensionStyle.LengthDisplay AlternateDimensionLengthDisplay{ get; set; }
  //public char DecimalSeparator{ get; set; }
  //public Plane Plane{ get; set; }
  //public string GetPlainTextWithRunMap(ref int[] map)
  std::wstring RichText() const;
  //void SetRichText(const std::wstring& rtf);
  std::wstring PlainText() const;
  //void SetPlainText(const std::wstring& text);
  //public string RichText{ get; set; }
  //public string PlainText{ get; set; }
  //public string PlainTextWithFields{ get; }
  //public static string PlainTextToRtf(string str) = >
  //public void SetRichText(string rtfText, DimensionStyle dimstyle)
  //public bool TextHasRtfFormatting{ get; }
  //static public string FormatRtfString(string rtf_in,
  //static public bool FirstCharProperties(string rtf_str, ref bool bold, ref bool italic, ref bool underline, ref string facename)
  //public Rhino.DocObjects.Font FirstCharFont
  //public bool IsAllBold()
  //public bool IsAllItalic()
  //public bool IsAllUnderlined()
  //public double TextModelWidth{ get; }
  //public double FormatWidth{ get; set; }
  //public bool TextIsWrapped{ get; set; }
  //public void WrapText()
  //public double TextRotationRadians{ get; set; }
  //public double TextRotationDegrees{ get; set; }
  //public virtual bool SetBold(bool set_on)
  //public virtual bool SetItalic(bool set_on)
  //public virtual bool SetUnderline(bool set_on)
  //public virtual bool SetFacename(bool set_on, string facename)
  //public bool RunReplace(
};


class BND_TextDot : public BND_GeometryBase
{
  ON_TextDot* m_dot = nullptr;
protected:
  void SetTrackedPointer(ON_TextDot* dot, const ON_ModelComponentReference* compref);

public:
  BND_TextDot(ON_TextDot* dot, const ON_ModelComponentReference* compref);
  BND_TextDot(const std::wstring& text, ON_3dPoint location);

  ON_3dPoint GetLocation() const { return m_dot->CenterPoint(); }
  void SetLocation(ON_3dPoint loc) { m_dot->SetCenterPoint(loc); }
  std::wstring GetText() const { return std::wstring(m_dot->PrimaryText()); }
  void SetText(const std::wstring& text) { m_dot->SetPrimaryText(text.c_str()); }
  std::wstring GetSecondaryText() const { return std::wstring(m_dot->SecondaryText()); }
  void SetSecondaryText(const std::wstring& text) { m_dot->SetSecondaryText(text.c_str()); }
  int GetFontHeight() const { return m_dot->HeightInPoints(); }
  void SetFontHeight(int height) { m_dot->SetHeightInPoints(height); }
  std::wstring GetFontFace() const { return std::wstring(m_dot->FontFace()); }
  void SetFontFace(const std::wstring& face) { m_dot->SetFontFace(face.c_str()); }
};