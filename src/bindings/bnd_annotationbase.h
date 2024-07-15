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
  BND_AnnotationBase();
  void SetTrackedPointer(ON_Annotation* annotation, const ON_ModelComponentReference* compref);

public:
  BND_AnnotationBase(ON_Annotation *annotation, const ON_ModelComponentReference *compref);
  //public Guid DimensionStyleId {get;set;}
  ON::AnnotationType AnnotationType() const;
  BND_UUID DimensionStyleId() const;
  // public bool HasPropertyOverrides {get;}
  // public bool IsPropertyOverridden(DimensionStyle.Field field)
  // public bool ClearPropertyOverrides()
  // public DimensionStyle GetDimensionStyle(DimensionStyle parentDimStyle)
  // public DimensionStyle DimensionStyle {get;}
  //BND_DimensionStyle DimensionStyle();
  // public bool SetOverrideDimStyle(DimensionStyle OverrideStyle)
  // public DimensionStyle ParentDimensionStyle {get; set;}
  // public double TextHeight {get; set;}
  //  --- double TextHeight() const;
  //  public bool MaskEnabled { get;set;}
  //  public bool MaskUsesViewportColor{ get; set; }
  //  public DimensionStyle.MaskType MaskColorSource{ get; set; }
  //  public bool DrawTextFrame{ get; set; }
  //  public DimensionStyle.MaskFrame MaskFrame{ get; set; }
  //  public Color MaskColor{ get; set; }
  //  public double MaskOffset{ get; set; }
  //  public double DimensionScale{ get; set; }
  //  public bool DrawForward{ get; set; }
  //  public DocObjects.Font Font{ get; set; }
  //  public DimensionStyle.LengthDisplay DimensionLengthDisplay{ get; set; }
  //  public DimensionStyle.LengthDisplay AlternateDimensionLengthDisplay{ get; set; }
  //  public char DecimalSeparator{ get; set; }
  //  public Plane Plane{ get; set; }
  BND_Plane Plane() const;
  // public string GetPlainTextWithRunMap(ref int[] map)
  std::wstring RichText() const;
  //void SetRichText(const std::wstring& rtf);
  std::wstring PlainText() const;
  //void SetPlainText(const std::wstring& text);
  //public string RichText{ get; set; }
  //public string PlainText{ get; set; }
  std::wstring PlainTextWithFields() const;
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
  bool TextIsWrapped() const;
  void SetTextIsWrapped(bool wrapped);
  //public void WrapText()
  void WrapText(double wrapWidth);
  // public double TextRotationRadians{ get; set; }
  // public double TextRotationDegrees{ get; set; }
  // public virtual bool SetBold(bool set_on)
  // public virtual bool SetItalic(bool set_on)
  // public virtual bool SetUnderline(bool set_on)
  // public virtual bool SetFacename(bool set_on, string facename)
  // public bool RunReplace(
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

/*
class BND_Text : public BND_AnnotationBase
{
  ON_Text* m_text = nullptr;
protected:
  void SetTrackedPointer(ON_Text* text, const ON_ModelComponentReference* compref);
public:
  BND_Text(ON_Text* text, const ON_ModelComponentReference* compref);
};

*/

class BND_Leader : public BND_AnnotationBase
{
  ON_Leader* m_leader = nullptr;
protected:
  void SetTrackedPointer(ON_Leader* leader, const ON_ModelComponentReference* compref);
public:
  BND_Leader(ON_Leader* leader, const ON_ModelComponentReference* compref);

#if defined(ON_PYTHON_COMPILE)
  std::vector<ON_3dPoint> GetPoints() const;
#else
  emscripten::val GetPoints() const;
#endif
  ON_2dPoint GetTextPoint2d(const BND_DimensionStyle& dimstyle, double leaderscale) const;
};

class BND_Dimension : public BND_AnnotationBase
{
  ON_Dimension* m_dimension = nullptr;
protected:
  BND_Dimension();
  void SetTrackedPointer(ON_Dimension* dimension, const ON_ModelComponentReference* compref);

public:
  BND_Dimension(ON_Dimension* dimension, const ON_ModelComponentReference* compref);
};

class BND_DimLinear : public BND_Dimension
{
  ON_DimLinear* m_dimLinear= nullptr;
protected:
  void SetTrackedPointer(ON_DimLinear* dimLinear, const ON_ModelComponentReference* compref);

public:
  BND_DimLinear(ON_DimLinear* dimLinear, const ON_ModelComponentReference* compref);
  BND_DICT GetPoints() const;
  BND_DICT GetDisplayLines(const BND_DimensionStyle& dimStyle);
};

class BND_DimAngular : public BND_Dimension
{
  ON_DimAngular* m_dimAngular= nullptr;
protected:
  void SetTrackedPointer(ON_DimAngular *dimAngular, const ON_ModelComponentReference *compref);

public:
  BND_DimAngular(ON_DimAngular* dimAngular, const ON_ModelComponentReference* compref);

  BND_DICT GetPoints() const;
  BND_DICT GetDisplayLines(const BND_DimensionStyle& dimStyle);
  double Radius() const;
  double Measurement() const;
};

class BND_DimRadial : public BND_Dimension
{
  ON_DimRadial* m_dimRadial= nullptr;
protected:
  void SetTrackedPointer(ON_DimRadial* dimRadial, const ON_ModelComponentReference* compref);

public:
  BND_DimRadial(ON_DimRadial* dimRadial, const ON_ModelComponentReference* compref);
  BND_DICT GetPoints() const;
  BND_DICT GetDisplayLines(const BND_DimensionStyle& dimStyle);

};

class BND_DimOrdinate : public BND_Dimension
{
  ON_DimOrdinate* m_dimOrdinate= nullptr;
protected:
  void SetTrackedPointer(ON_DimOrdinate* dimOrdinate, const ON_ModelComponentReference* compref);

public:
  BND_DimOrdinate(ON_DimOrdinate* dimOrdinate, const ON_ModelComponentReference* compref);
  BND_DICT GetPoints() const;
  BND_DICT GetDisplayLines(const BND_DimensionStyle& dimStyle);
};

class BND_Centermark : public BND_Dimension
{
  ON_Centermark* m_centermark= nullptr;
protected:
  void SetTrackedPointer(ON_Centermark* centermark, const ON_ModelComponentReference* compref);

public:
  BND_Centermark(ON_Centermark* centermark, const ON_ModelComponentReference* compref);
  std::vector<ON_Line> GetDisplayLines(const BND_DimensionStyle& dimStyle);
};

