#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initAnnotationBaseBindings(rh3dmpymodule& m);
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

  ON::AnnotationType AnnotationType() const;
  BND_UUID DimensionStyleId() const;
  BND_Plane Plane() const;

  // --- Effective dimension style (parent style + per-object overrides) ---
  // The parent dimension style comes from File3dm.DimStyles.FindId(annotation.DimensionStyleId).
  BND_DimensionStyle* GetDimensionStyle(const BND_DimensionStyle& parentDimStyle) const;

  // --- Per-object dimension style overrides ---
  bool HasPropertyOverrides() const;
  bool IsPropertyOverridden(ON_DimStyle::field field) const;
  void ClearPropertyOverrides();
  bool SetOverrideDimStyle(const BND_DimensionStyle& overrideStyle);

  // --- Text ---
  std::wstring RichText() const;
  std::wstring PlainText() const;
  std::wstring PlainTextWithFields() const;
  void SetRichText(const std::wstring& rtfText, const BND_DimensionStyle& dimstyle);
  static std::wstring PlainTextToRtf(const std::wstring& str);
  bool TextHasRtfFormatting() const;
  bool RunReplace(const BND_DimensionStyle& dimstyle, const std::wstring& str, int startRunIndex, int startRunPosition, int endRunIndex, int endRunPosition);

  bool TextIsWrapped() const;
  void SetTextIsWrapped(bool wrapped);
  void WrapText(double wrapWidth);

  double TextRotationRadians() const;
  void SetTextRotationRadians(double rotation);
  double TextRotationDegrees() const;
  void SetTextRotationDegrees(double rotation);

  // --- Properties that originate from the dimension style and can be overridden
  //     per annotation object. Each takes the parent dimension style. ---
  double TextHeight(const BND_DimensionStyle& parentDimStyle) const;
  void SetTextHeight(const BND_DimensionStyle& parentDimStyle, double height);
  double DimensionScale(const BND_DimensionStyle& parentDimStyle) const;
  void SetDimensionScale(const BND_DimensionStyle& parentDimStyle, double scale);

  bool MaskEnabled(const BND_DimensionStyle& parentDimStyle) const;
  void SetMaskEnabled(const BND_DimensionStyle& parentDimStyle, bool on);
  ON_TextMask::MaskType MaskColorSource(const BND_DimensionStyle& parentDimStyle) const;
  void SetMaskColorSource(const BND_DimensionStyle& parentDimStyle, ON_TextMask::MaskType source);
  ON_TextMask::MaskFrame MaskFrame(const BND_DimensionStyle& parentDimStyle) const;
  void SetMaskFrame(const BND_DimensionStyle& parentDimStyle, ON_TextMask::MaskFrame frame);
  BND_Color MaskColor(const BND_DimensionStyle& parentDimStyle) const;
  void SetMaskColor(const BND_DimensionStyle& parentDimStyle, BND_Color color);
  double MaskOffset(const BND_DimensionStyle& parentDimStyle) const;
  void SetMaskOffset(const BND_DimensionStyle& parentDimStyle, double offset);

  ON_DimStyle::LengthDisplay DimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle) const;
  void SetDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle, ON_DimStyle::LengthDisplay display);
  ON_DimStyle::LengthDisplay AlternateDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle) const;
  void SetAlternateDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle, ON_DimStyle::LengthDisplay display);

  class BND_Font* GetFont(const BND_DimensionStyle& parentDimStyle) const;
  void SetFont(const BND_DimensionStyle& parentDimStyle, const class BND_Font* font);

  // A valid bounding box for annotation geometry requires the parent dimension
  // style (the generic GeometryBase.GetBoundingBox returns an invalid box).
  BND_BoundingBox GetBoundingBox(const BND_DimensionStyle& parentDimStyle) const;

  // Not exposed: GetPlainTextWithRunMap has no direct opennurbs public method
  // (it is implemented only in the rhinocommon_c shim layer).
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

class BND_Text : public BND_AnnotationBase
{
  ON_Text* m_text = nullptr;
protected:
  void SetTrackedPointer(ON_Text* text, const ON_ModelComponentReference* compref);
public:
  BND_Text(ON_Text* text, const ON_ModelComponentReference* compref);
};

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

