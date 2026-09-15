#include "bindings.h"

BND_AnnotationBase::BND_AnnotationBase()
{
}

BND_AnnotationBase::BND_AnnotationBase(ON_Annotation* annotation, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(annotation, compref);
}

void BND_AnnotationBase::SetTrackedPointer(ON_Annotation* annotation, const ON_ModelComponentReference* compref)
{
  m_annotation = annotation;
  BND_GeometryBase::SetTrackedPointer(annotation, compref);
}

ON::AnnotationType BND_AnnotationBase::AnnotationType() const
{
  return m_annotation->Type();
}

/*
double BND_AnnotationBase::TextHeight() const
{
  return m_annotation->TextHeight();
}
*/

BND_Plane BND_AnnotationBase::Plane() const
{
  return BND_Plane::FromOnPlane(m_annotation->Plane());
}

std::wstring BND_AnnotationBase::RichText() const
{
  std::wstring rc;
  const ON_TextContent* text_content = m_annotation->Text();
  if (text_content)
    rc = text_content->PlatformRichTextFromRuns();
  if (rc.empty())
    // opennurbs_font.cpp ON::RichTextStyleFromCurrentPlatform() does hot have a Rich Text Style for Linux
    rc = m_annotation->RichText().Array();
  return rc;
}

std::wstring BND_AnnotationBase::PlainText() const
{
  std::wstring rc(m_annotation->PlainText());
  return rc;
}

std::wstring BND_AnnotationBase::PlainTextWithFields() const
{
  std::wstring rc(m_annotation->PlainTextWithFields());
  return rc;
}

bool BND_AnnotationBase::TextIsWrapped() const
{
  const ON_TextContent* text = m_annotation->Text();
  if(nullptr != text)
  {
    return text->TextIsWrapped();
  }
  return false;
}

void BND_AnnotationBase::SetTextIsWrapped(bool wrapped)
{
  ON_TextContent* text = m_annotation->Text();
  if(nullptr != text)
  {
    text->SetTextIsWrapped(wrapped);
  }
}

void BND_AnnotationBase::WrapText(double wrapwidth)
{
  ON_TextContent* text = m_annotation->Text();
  if(nullptr != text)
  {
    text->WrapText(wrapwidth);
  }
}

/*
BND_DimensionStyle BND_AnnotationBase::DimensionStyle()
{
  ON_UUID dimstyleid = m_annotation->DimensionStyleId();

  //const ON_DimStyle &ds = m_annotation->DimensionStyle();
  ON_ModelComponentReference compref = m_model->DimensionStyleFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_DimStyle* modeldimstyle = const_cast<ON_DimStyle*>(ON_DimStyle::Cast(model_component));
  if (modeldimstyle)
    return new BND_DimensionStyle(modeldimstyle, &compref);

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
}
*/

BND_UUID BND_AnnotationBase::DimensionStyleId() const
{
  ON_UUID dimstyleid = m_annotation->DimensionStyleId();
  return ON_UUID_to_Binding(dimstyleid);
}

BND_DimensionStyle* BND_AnnotationBase::GetDimensionStyle(const BND_DimensionStyle& parentDimStyle) const
{
  // DimensionStyle() returns a reference to either the parent style or the internal
  // override style, so copy it into a new heap object owned by the returned wrapper.
  const ON_DimStyle& effective = m_annotation->DimensionStyle(*parentDimStyle.m_dimstyle);
  return new BND_DimensionStyle(new ON_DimStyle(effective), nullptr);
}

bool BND_AnnotationBase::HasPropertyOverrides() const
{
  return m_annotation->HasDimensionStyleOverrides();
}

bool BND_AnnotationBase::IsPropertyOverridden(ON_DimStyle::field field) const
{
  return m_annotation->FieldIsOverridden(field);
}

void BND_AnnotationBase::ClearPropertyOverrides()
{
  m_annotation->ClearOverrideDimensionStyle();
}

bool BND_AnnotationBase::SetOverrideDimStyle(const BND_DimensionStyle& overrideStyle)
{
  return m_annotation->SetOverrideDimensionStyle(overrideStyle.m_dimstyle, false);
}

void BND_AnnotationBase::SetRichText(const std::wstring& rtfText, const BND_DimensionStyle& dimstyle)
{
  m_annotation->ReplaceTextString(rtfText.c_str(), dimstyle.m_dimstyle);
}

std::wstring BND_AnnotationBase::PlainTextToRtf(const std::wstring& str)
{
  std::wstring rc = L"{\\rtf1{\\ltrch ";
  for (wchar_t c : str)
  {
    switch (c)
    {
    case L'\\': rc += L"\\\\"; break;
    case L'{':  rc += L"\\{";  break;
    case L'}':  rc += L"\\}";  break;
    case L'\n': rc += L"}\\par {"; break;
    case L'\r': break;
    default:    rc += c; break;
    }
  }
  rc += L"}}";
  return rc;
}

bool BND_AnnotationBase::TextHasRtfFormatting() const
{
  std::wstring rt = RichText();
  return rt.rfind(L"{\\rtf", 0) == 0 || rt.rfind(L"{\\\\rtf", 0) == 0;
}

bool BND_AnnotationBase::RunReplace(const BND_DimensionStyle& dimstyle, const std::wstring& str, int startRunIndex, int startRunPosition, int endRunIndex, int endRunPosition)
{
  return m_annotation->RunReplaceString(dimstyle.m_dimstyle, str.c_str(), startRunIndex, startRunPosition, endRunIndex, endRunPosition);
}

double BND_AnnotationBase::TextRotationRadians() const { return m_annotation->TextRotationRadians(); }
void BND_AnnotationBase::SetTextRotationRadians(double rotation) { m_annotation->SetTextRotationRadians(rotation); }
double BND_AnnotationBase::TextRotationDegrees() const { return m_annotation->TextRotationDegrees(); }
void BND_AnnotationBase::SetTextRotationDegrees(double rotation) { m_annotation->SetTextRotationDegrees(rotation); }

double BND_AnnotationBase::TextHeight(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->TextHeight(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetTextHeight(const BND_DimensionStyle& parentDimStyle, double height) { m_annotation->SetTextHeight(parentDimStyle.m_dimstyle, height); }
double BND_AnnotationBase::DimensionScale(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->DimScale(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetDimensionScale(const BND_DimensionStyle& parentDimStyle, double scale) { m_annotation->SetDimScale(parentDimStyle.m_dimstyle, scale); }

bool BND_AnnotationBase::MaskEnabled(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->DrawTextMask(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetMaskEnabled(const BND_DimensionStyle& parentDimStyle, bool on) { m_annotation->SetDrawTextMask(parentDimStyle.m_dimstyle, on); }
ON_TextMask::MaskType BND_AnnotationBase::MaskColorSource(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->MaskFillType(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetMaskColorSource(const BND_DimensionStyle& parentDimStyle, ON_TextMask::MaskType source) { m_annotation->SetMaskFillType(parentDimStyle.m_dimstyle, source); }
ON_TextMask::MaskFrame BND_AnnotationBase::MaskFrame(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->MaskFrameType(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetMaskFrame(const BND_DimensionStyle& parentDimStyle, ON_TextMask::MaskFrame frame) { m_annotation->SetMaskFrameType(parentDimStyle.m_dimstyle, frame); }
BND_Color BND_AnnotationBase::MaskColor(const BND_DimensionStyle& parentDimStyle) const { return ON_Color_to_Binding(m_annotation->MaskColor(parentDimStyle.m_dimstyle)); }
void BND_AnnotationBase::SetMaskColor(const BND_DimensionStyle& parentDimStyle, BND_Color color) { m_annotation->SetMaskColor(parentDimStyle.m_dimstyle, Binding_to_ON_Color(color)); }
double BND_AnnotationBase::MaskOffset(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->MaskBorder(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetMaskOffset(const BND_DimensionStyle& parentDimStyle, double offset) { m_annotation->SetMaskBorder(parentDimStyle.m_dimstyle, offset); }

ON_DimStyle::LengthDisplay BND_AnnotationBase::DimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->DimensionLengthDisplay(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle, ON_DimStyle::LengthDisplay display) { m_annotation->SetDimensionLengthDisplay(parentDimStyle.m_dimstyle, display); }
ON_DimStyle::LengthDisplay BND_AnnotationBase::AlternateDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle) const { return m_annotation->AlternateDimensionLengthDisplay(parentDimStyle.m_dimstyle); }
void BND_AnnotationBase::SetAlternateDimensionLengthDisplay(const BND_DimensionStyle& parentDimStyle, ON_DimStyle::LengthDisplay display) { m_annotation->SetAlternateDimensionLengthDisplay(parentDimStyle.m_dimstyle, display); }

BND_Font* BND_AnnotationBase::GetFont(const BND_DimensionStyle& parentDimStyle) const
{
  return new BND_Font(m_annotation->Font(parentDimStyle.m_dimstyle));
}

void BND_AnnotationBase::SetFont(const BND_DimensionStyle& parentDimStyle, const BND_Font* font)
{
  if (font)
    m_annotation->SetFont(parentDimStyle.m_dimstyle, *font->m_managed_font);
}

BND_BoundingBox BND_AnnotationBase::GetBoundingBox(const BND_DimensionStyle& parentDimStyle) const
{
  const ON_DimStyle& effective = m_annotation->DimensionStyle(*parentDimStyle.m_dimstyle);
  double dimscale = effective.DimScale();
  double boxmin[3] = { 0.0, 0.0, 0.0 };
  double boxmax[3] = { 0.0, 0.0, 0.0 };
  if (m_annotation->GetAnnotationBoundingBox(nullptr, &effective, dimscale, boxmin, boxmax, false))
  {
    ON_BoundingBox bbox(ON_3dPoint(boxmin[0], boxmin[1], boxmin[2]), ON_3dPoint(boxmax[0], boxmax[1], boxmax[2]));
    if (bbox.IsNotEmpty())
      return BND_BoundingBox(bbox);
  }

  // Glyph metrics may be unavailable in a headless context, so GetAnnotationBoundingBox
  // can fail to measure the text. Fall back to the stored text-run rectangle transformed
  // into world coordinates using the same transform the glyph path would use.
  const ON_TextContent* tc = m_annotation->Text();
  if (nullptr != tc)
  {
    ON_2dPoint corners2d[4];
    ON_Xform txf;
    if (tc->Get2dCorners(corners2d) && m_annotation->GetTextXform(nullptr, &effective, dimscale, txf))
    {
      ON_BoundingBox bbox = ON_BoundingBox::EmptyBoundingBox;
      for (int i = 0; i < 4; i++)
      {
        ON_3dPoint p(corners2d[i].x, corners2d[i].y, 0.0);
        p.Transform(txf);
        bbox.Set(p, true);
      }
      if (bbox.IsNotEmpty())
        return BND_BoundingBox(bbox);
    }
  }

  return BND_BoundingBox(ON_BoundingBox::EmptyBoundingBox);
}

/*********/

BND_TextDot::BND_TextDot(ON_TextDot* dot, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dot, compref);
}

BND_TextDot::BND_TextDot(const std::wstring& text, ON_3dPoint location)
{
  SetTrackedPointer(new ON_TextDot(location, text.c_str(), nullptr), nullptr);
}

void BND_TextDot::SetTrackedPointer(ON_TextDot* dot, const ON_ModelComponentReference* compref)
{
  m_dot = dot;
  BND_GeometryBase::SetTrackedPointer(dot, compref);
}

/*********/

BND_Text::BND_Text(ON_Text* text, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(text, compref);
}


void BND_Text::SetTrackedPointer(ON_Text* text, const ON_ModelComponentReference* compref)
{
  m_text = text;
  BND_AnnotationBase::SetTrackedPointer(text, compref);
}

/*********/

BND_Leader::BND_Leader(ON_Leader* leader, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(leader, compref);
}


void BND_Leader::SetTrackedPointer(ON_Leader* leader, const ON_ModelComponentReference* compref)
{
  m_leader= leader;
  BND_AnnotationBase::SetTrackedPointer(leader, compref);
}

#if defined(ON_PYTHON_COMPILE)
std::vector<ON_3dPoint> BND_Leader::GetPoints() const
#else
emscripten::val BND_Leader::GetPoints() const
#endif
{
#if defined(ON_PYTHON_COMPILE)
  std::vector<ON_3dPoint> rc;
#else
  emscripten::val rc = emscripten::val::array();
#endif
  const ON_2dPointArray& points = m_leader->Points2d();
  for (int i = 0; i < points.Count(); i++)
  {
    ON_3dPoint pt;
    if(m_leader->Point3d(i, pt)) {
#if defined(ON_PYTHON_COMPILE)
      rc.push_back(pt);
#else
      rc.call<void>("push", PointToDict(pt));
#endif
    }
  }
  return rc;
}

ON_2dPoint BND_Leader::GetTextPoint2d(const BND_DimensionStyle& dimstyle, double leaderscale) const
{
  ON_2dPoint pt;
  if(m_leader->GetTextPoint2d(dimstyle.m_dimstyle, leaderscale, pt))
    return pt;
  return ON_2dPoint::UnsetPoint;
}

/*********/

BND_Dimension::BND_Dimension()
{
}

BND_Dimension::BND_Dimension(ON_Dimension* dimension, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimension, compref);
}

void BND_Dimension::SetTrackedPointer(ON_Dimension* dimension, const ON_ModelComponentReference* compref)
{
  m_dimension = dimension;
  BND_AnnotationBase::SetTrackedPointer(dimension, compref);
}
/*********/

BND_DimLinear::BND_DimLinear(ON_DimLinear* dimLinear, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimLinear, compref);
}

void BND_DimLinear::SetTrackedPointer(ON_DimLinear* dimLinear, const ON_ModelComponentReference* compref)
{
  m_dimLinear = dimLinear;
  BND_Dimension::SetTrackedPointer(dimLinear, compref);
}

/*

#if defined(ON_PYTHON_COMPILE)
  BND_DICT d;
#else
  emscripten::val d(emscripten::val::object());
#endif
#if defined(ON_PYTHON_COMPILE)
  d["radius"] = Radius();
  d["plane"] = PlaneToDict(m_circle.plane);
#else
  d.set("radius", emscripten::val(Radius()));
  d.set("plane", emscripten::val(PlaneToDict(m_circle.plane)));
#endif
*/

BND_DICT BND_DimLinear::GetPoints() const
{
  ON_3dPoint defpt1;
  ON_3dPoint defpt2;
  ON_3dPoint arrowpt1;
  ON_3dPoint arrowpt2;
  ON_3dPoint dimline;
  ON_3dPoint textpt;
  if(m_dimLinear->Get3dPoints(&defpt1, &defpt2, &arrowpt1, &arrowpt2, &dimline, &textpt))
  {
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif
#if defined(ON_PYTHON_COMPILE)
    d["defpt1"] = defpt1;
    d["defpt2"] = defpt2;
    d["arrowpt1"] = arrowpt1;
    d["arrowpt2"] = arrowpt2;
    d["dimline"] = dimline;
    d["textpt"] = textpt;
#else
    d.set("defpt1", PointToDict(defpt1));
    d.set("defpt2", PointToDict(defpt2));
    d.set("arrowpt1", PointToDict(arrowpt1));
    d.set("arrowpt2", PointToDict(arrowpt2));
    d.set("dimline", PointToDict(dimline));
    d.set("textpt", PointToDict(textpt));
#endif
    return d;
  }
#if defined(ON_PYTHON_COMPILE)
  throw py::value_error("Failed to get DimLinear points");
#else
  return emscripten::val::null();
#endif
}

BND_DICT BND_DimLinear::GetDisplayLines(const BND_DimensionStyle& dimstyle)
{
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif
  std::vector<ON_Line> rc;
  std::vector<ON_3dPoint> text_points;
  ON_3dPoint text_rect[4];
  ON_Line lines[4];
  bool isline[4];
  if (m_dimLinear->GetDisplayLines(nullptr, dimstyle.m_dimstyle, 1.0, text_rect, lines, isline, 4))
  {
    for(int i = 0; i < 4; i++)
    {
      if(isline[i])
        rc.push_back(lines[i]);
    }
    for(int i = 0; i < 4; i++)
    {
        text_points.push_back(text_rect[i]);
    }

#if defined(ON_PYTHON_COMPILE)
    d["lines"] = rc;
    d["text_rect"] = text_points;
#else
    d.set("lines", emscripten::val(rc));
    d.set("text_rect", emscripten::val(text_points));
#endif
  }

  return d;
}
/*********/

BND_DimAngular::BND_DimAngular(ON_DimAngular* dimAngular, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimAngular, compref);
}

void BND_DimAngular::SetTrackedPointer(ON_DimAngular* dimAngular, const ON_ModelComponentReference* compref)
{
  m_dimAngular = dimAngular;
  BND_Dimension::SetTrackedPointer(dimAngular, compref);
}

double BND_DimAngular::Radius() const
{
  return m_dimAngular->Radius();
}

double BND_DimAngular::Measurement() const
{
  return m_dimAngular->Measurement();
}

BND_DICT BND_DimAngular::GetPoints() const
{
  ON_3dPoint centerpt;
  ON_3dPoint defpt1;
  ON_3dPoint defpt2;
  ON_3dPoint arrowpt1;
  ON_3dPoint arrowpt2;
  ON_3dPoint dimlinept;
  ON_3dPoint textpt;
  if(m_dimAngular->Get3dPoints(&centerpt, &defpt1, &defpt2, &arrowpt1, &arrowpt2, &dimlinept, &textpt))
  {
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif

#if defined(ON_PYTHON_COMPILE)
    d["centerpt"] = centerpt;
    d["defpt1"] = defpt1;
    d["defpt2"] = defpt2;
    d["arrowpt1"] = arrowpt1;
    d["arrowpt2"] = arrowpt2;
    d["dimlinept"] = dimlinept;
    d["textpt"] = textpt;
#else
    d.set("centerpt", PointToDict(centerpt));
    d.set("defpt1", PointToDict(defpt1));
    d.set("defpt2", PointToDict(defpt2));
    d.set("arrowpt1", PointToDict(arrowpt1));
    d.set("arrowpt2", PointToDict(arrowpt2));
    d.set("dimlinept", PointToDict(dimlinept));
    d.set("textpt", PointToDict(textpt));
#endif
    return d;
  }
#if defined(ON_PYTHON_COMPILE)
  throw py::value_error("Failed to get DimAngular points");
#else
  return emscripten::val::null();
#endif
}

BND_DICT BND_DimAngular::GetDisplayLines(const BND_DimensionStyle& dimstyle)
{
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif
  std::vector<ON_Line> linevec;
  std::vector<BND_Arc> arcvec;
  std::vector<ON_3dPoint> text_points;
  ON_3dPoint text_rect[4];
  ON_Line lines[2];
  bool isline[2];
  ON_Arc arcs[2];
  bool isarc[2];
  if (m_dimAngular->GetDisplayLines(nullptr, dimstyle.m_dimstyle, 1.0, text_rect, lines, isline, arcs, isarc, 2, 2))
  {
    for(int i = 0; i < 2; i++)
    {
      if(isline[i])
        linevec.push_back(lines[i]);
    }
    for(int i = 0; i < 2; i++)
    {
      if(isarc[i])
        arcvec.push_back(BND_Arc(arcs[i]));
    }
    for(int i = 0; i < 4; i++)
    {
        text_points.push_back(text_rect[i]);
    }

#if defined(ON_PYTHON_COMPILE)
    d["lines"] = linevec;
    d["arcs"] = arcvec;
    d["text_rect"] = text_points;
#else
    d.set("lines", emscripten::val(linevec));
    d.set("arcs", emscripten::val(arcvec));
    d.set("text_rect", emscripten::val(text_points));
#endif
  }

  return d;
}

/*********/

BND_DimRadial::BND_DimRadial(ON_DimRadial* dimRadial, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimRadial, compref);
}

void BND_DimRadial::SetTrackedPointer(ON_DimRadial* dimRadial, const ON_ModelComponentReference* compref)
{
  m_dimRadial = dimRadial;
  BND_Dimension::SetTrackedPointer(dimRadial, compref);
}

BND_DICT BND_DimRadial::GetPoints() const
{
  ON_3dPoint centerpt;
  ON_3dPoint radiuspt;
  ON_3dPoint dimlinept;
  ON_3dPoint kneept;
  if(m_dimRadial->Get3dPoints(&centerpt, &radiuspt, &dimlinept, &kneept))
  {
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif

#if defined(ON_PYTHON_COMPILE)
    d["centerpt"] = centerpt;
    d["radiuspt"] = radiuspt;
    d["dimlinept"] = dimlinept;
    d["kneept"] = kneept;
#else
    d.set("centerpt", PointToDict(centerpt));
    d.set("radiuspt", PointToDict(radiuspt));
    d.set("dimlinept", PointToDict(dimlinept));
    d.set("kneept", PointToDict(kneept));
#endif

    return d;
  }
#if defined(ON_PYTHON_COMPILE)
  throw py::value_error("Failed to get DimRadial points");
#else
  return emscripten::val::null();
#endif
}

BND_DICT BND_DimRadial::GetDisplayLines(const BND_DimensionStyle& dimstyle)
{
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif
  std::vector<ON_Line> rc;
  std::vector<ON_3dPoint> text_points;
  ON_3dPoint text_rect[4];
  ON_Line lines[9];
  bool isline[9];
  if (m_dimRadial->GetDisplayLines(dimstyle.m_dimstyle, 1.0, text_rect, lines, isline, 9))
  {
    for(int i = 0; i < 9; i++)
    {
      if(isline[i])
        rc.push_back(lines[i]);
    }
    for(int i = 0; i < 4; i++)
    {
        text_points.push_back(text_rect[i]);
    }

#if defined(ON_PYTHON_COMPILE)
    d["lines"] = rc;
    d["text_rect"] = text_points;
#else
    d.set("lines", emscripten::val(rc));
    d.set("text_rect", emscripten::val(text_points));
#endif
  }

  return d;
}

/*********/

BND_DimOrdinate::BND_DimOrdinate(ON_DimOrdinate* dimOrdinate, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimOrdinate, compref);
}

void BND_DimOrdinate::SetTrackedPointer(ON_DimOrdinate* dimOrdinate, const ON_ModelComponentReference* compref)
{
  m_dimOrdinate = dimOrdinate;
  BND_Dimension::SetTrackedPointer(dimOrdinate, compref);
}

BND_DICT BND_DimOrdinate::GetPoints() const
{
  ON_3dPoint basept;
  ON_3dPoint defpt;
  ON_3dPoint leaderpt;
  ON_3dPoint kinkpt1;
  ON_3dPoint kinkpt2;
  if(m_dimOrdinate->Get3dPoints(&basept, &defpt, &leaderpt, &kinkpt1, &kinkpt2))
  {
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif

#if defined(ON_PYTHON_COMPILE)
    d["basept"] = basept;
    d["defpt"] = defpt;
    d["leaderpt"] = leaderpt;
    d["kinkpt1"] = kinkpt1;
    d["kinkpt2"] = kinkpt2;
#else
    d.set("basept", PointToDict(basept));
    d.set("defpt", PointToDict(defpt));
    d.set("leaderpt", PointToDict(leaderpt));
    d.set("kinkpt1", PointToDict(kinkpt1));
    d.set("kinkpt2", PointToDict(kinkpt2));
#endif

    return d;
  }
#if defined(ON_PYTHON_COMPILE)
  throw py::value_error("Failed to get DimOrdinate points");
#else
  return emscripten::val::null();
#endif
}

BND_DICT BND_DimOrdinate::GetDisplayLines(const BND_DimensionStyle& dimstyle)
{
#if defined(ON_PYTHON_COMPILE)
    BND_DICT d;
#else
    emscripten::val d(emscripten::val::object());
#endif
  std::vector<ON_Line> rc;
  std::vector<ON_3dPoint> text_points;
  ON_3dPoint text_rect[4];
  ON_Line lines[3];
  bool isline[3];
  if (m_dimOrdinate->GetDisplayLines(dimstyle.m_dimstyle, 1.0, text_rect, lines, isline, 3))
  {
    for(int i = 0; i < 3; i++)
    {
      if(isline[i])
        rc.push_back(lines[i]);
    }
    for(int i = 0; i < 4; i++)
    {
        text_points.push_back(text_rect[i]);
    }

#if defined(ON_PYTHON_COMPILE)
    d["lines"] = rc;
    d["text_rect"] = text_points;
#else
    d.set("lines", emscripten::val(rc));
    d.set("text_rect", emscripten::val(text_points));
#endif
  }

  return d;
}

/*********/

BND_Centermark::BND_Centermark(ON_Centermark* centermark, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(centermark, compref);
}

void BND_Centermark::SetTrackedPointer(ON_Centermark* centermark, const ON_ModelComponentReference* compref)
{
  m_centermark = centermark;
  BND_Dimension::SetTrackedPointer(centermark, compref);
}

std::vector<ON_Line> BND_Centermark::GetDisplayLines(const BND_DimensionStyle& dimstyle)
{
  std::vector<ON_Line> rc;
  ON_Line lines[6];
  bool isline[6];
  if (m_centermark->GetDisplayLines(dimstyle.m_dimstyle, 1.0, lines, isline, 6))
  {
    for(int i = 0; i < 6; i++)
    {
      if(isline[i])
        rc.push_back(lines[i]);
    }
  }

  return rc;
}

#if defined(ON_PYTHON_COMPILE)

void initAnnotationBaseBindings(rh3dmpymodule& m)
{
  py::class_<BND_AnnotationBase, BND_GeometryBase>(m, "AnnotationBase")
    .def_property_readonly("DimensionStyleId", &BND_AnnotationBase::DimensionStyleId)
    .def_property_readonly("RichText", &BND_AnnotationBase::RichText)
    .def_property_readonly("PlainText", &BND_AnnotationBase::PlainText)
    .def_property_readonly("PlainTextWithFields", &BND_AnnotationBase::PlainTextWithFields)
    .def_property_readonly("AnnotationType", &BND_AnnotationBase::AnnotationType)
    .def("WrapText", &BND_AnnotationBase::WrapText, py::arg("wrapwidth"))
    .def_property("TextIsWrapped", &BND_AnnotationBase::TextIsWrapped, &BND_AnnotationBase::SetTextIsWrapped)
    .def_property_readonly("Plane", &BND_AnnotationBase::Plane)
    .def("GetDimensionStyle", &BND_AnnotationBase::GetDimensionStyle, py::arg("parentDimStyle"))
    .def_property_readonly("HasPropertyOverrides", &BND_AnnotationBase::HasPropertyOverrides)
    .def("IsPropertyOverridden", &BND_AnnotationBase::IsPropertyOverridden, py::arg("field"))
    .def("ClearPropertyOverrides", &BND_AnnotationBase::ClearPropertyOverrides)
    .def("SetOverrideDimStyle", &BND_AnnotationBase::SetOverrideDimStyle, py::arg("overrideStyle"))
    .def("SetRichText", &BND_AnnotationBase::SetRichText, py::arg("rtfText"), py::arg("dimstyle"))
    .def_static("PlainTextToRtf", &BND_AnnotationBase::PlainTextToRtf, py::arg("str"))
    .def_property_readonly("TextHasRtfFormatting", &BND_AnnotationBase::TextHasRtfFormatting)
    .def("RunReplace", &BND_AnnotationBase::RunReplace, py::arg("dimstyle"), py::arg("str"), py::arg("startRunIndex"), py::arg("startRunPosition"), py::arg("endRunIndex"), py::arg("endRunPosition"))
    .def_property("TextRotationRadians", &BND_AnnotationBase::TextRotationRadians, &BND_AnnotationBase::SetTextRotationRadians)
    .def_property("TextRotationDegrees", &BND_AnnotationBase::TextRotationDegrees, &BND_AnnotationBase::SetTextRotationDegrees)
    .def("GetTextHeight", &BND_AnnotationBase::TextHeight, py::arg("parentDimStyle"))
    .def("SetTextHeight", &BND_AnnotationBase::SetTextHeight, py::arg("parentDimStyle"), py::arg("height"))
    .def("GetDimensionScale", &BND_AnnotationBase::DimensionScale, py::arg("parentDimStyle"))
    .def("SetDimensionScale", &BND_AnnotationBase::SetDimensionScale, py::arg("parentDimStyle"), py::arg("scale"))
    .def("GetMaskEnabled", &BND_AnnotationBase::MaskEnabled, py::arg("parentDimStyle"))
    .def("SetMaskEnabled", &BND_AnnotationBase::SetMaskEnabled, py::arg("parentDimStyle"), py::arg("on"))
    .def("GetMaskColorSource", &BND_AnnotationBase::MaskColorSource, py::arg("parentDimStyle"))
    .def("SetMaskColorSource", &BND_AnnotationBase::SetMaskColorSource, py::arg("parentDimStyle"), py::arg("source"))
    .def("GetMaskFrame", &BND_AnnotationBase::MaskFrame, py::arg("parentDimStyle"))
    .def("SetMaskFrame", &BND_AnnotationBase::SetMaskFrame, py::arg("parentDimStyle"), py::arg("frame"))
    .def("GetMaskColor", &BND_AnnotationBase::MaskColor, py::arg("parentDimStyle"))
    .def("SetMaskColor", &BND_AnnotationBase::SetMaskColor, py::arg("parentDimStyle"), py::arg("color"))
    .def("GetMaskOffset", &BND_AnnotationBase::MaskOffset, py::arg("parentDimStyle"))
    .def("SetMaskOffset", &BND_AnnotationBase::SetMaskOffset, py::arg("parentDimStyle"), py::arg("offset"))
    .def("GetDimensionLengthDisplay", &BND_AnnotationBase::DimensionLengthDisplay, py::arg("parentDimStyle"))
    .def("SetDimensionLengthDisplay", &BND_AnnotationBase::SetDimensionLengthDisplay, py::arg("parentDimStyle"), py::arg("display"))
    .def("GetAlternateDimensionLengthDisplay", &BND_AnnotationBase::AlternateDimensionLengthDisplay, py::arg("parentDimStyle"))
    .def("SetAlternateDimensionLengthDisplay", &BND_AnnotationBase::SetAlternateDimensionLengthDisplay, py::arg("parentDimStyle"), py::arg("display"))
    .def("GetFont", &BND_AnnotationBase::GetFont, py::arg("parentDimStyle"))
    .def("SetFont", &BND_AnnotationBase::SetFont, py::arg("parentDimStyle"), py::arg("font"))
    .def("GetBoundingBox", &BND_AnnotationBase::GetBoundingBox, py::arg("parentDimStyle"))
    ;

  py::class_<BND_Text, BND_AnnotationBase>(m, "Text")
    ;

  py::class_<BND_Leader, BND_AnnotationBase>(m, "Leader")
    .def_property_readonly("Points", &BND_Leader::GetPoints)
    .def("GetTextPoint2d", &BND_Leader::GetTextPoint2d, py::arg("dimstyle"), py::arg("leaderscale"))
    ;

  py::class_<BND_Dimension, BND_AnnotationBase>(m, "Dimension")
    ;

  py::class_<BND_DimLinear, BND_Dimension>(m, "DimLinear")
    .def_property_readonly("Points", &BND_DimLinear::GetPoints)
    .def("GetDisplayLines", &BND_DimLinear::GetDisplayLines, py::arg("dimstyle"))
    ;

  py::class_<BND_DimAngular, BND_Dimension>(m, "DimAngular")
    .def_property_readonly("Points", &BND_DimAngular::GetPoints)
    .def_property_readonly("Radius", &BND_DimAngular::Radius)
    .def_property_readonly("Angle", &BND_DimAngular::Measurement)
    .def("GetDisplayLines", &BND_DimAngular::GetDisplayLines, py::arg("dimstyle"))
    ;
  py::class_<BND_DimRadial, BND_Dimension>(m, "DimRadial")
    .def_property_readonly("Points", &BND_DimRadial::GetPoints)
    .def("GetDisplayLines", &BND_DimRadial::GetDisplayLines, py::arg("dimstyle"))
    ;
  py::class_<BND_DimOrdinate, BND_Dimension>(m, "DimOrdinate")
    .def_property_readonly("Points", &BND_DimOrdinate::GetPoints)
    .def("GetDisplayLines", &BND_DimOrdinate::GetDisplayLines, py::arg("dimstyle"))
    ;
  py::class_<BND_Centermark, BND_Dimension>(m, "Centermark")
    .def("GetDisplayLines", &BND_Centermark::GetDisplayLines, py::arg("dimstyle"))
    ;

  py::class_<BND_TextDot, BND_GeometryBase>(m, "TextDot")
    .def(py::init<const std::wstring&, ON_3dPoint>(), py::arg("text"), py::arg("location"))
    .def_property("Point", &BND_TextDot::GetLocation, &BND_TextDot::SetLocation)
    .def_property("Text", &BND_TextDot::GetText, &BND_TextDot::SetText)
    .def_property("SecondaryText", &BND_TextDot::GetSecondaryText, &BND_TextDot::SetSecondaryText)
    .def_property("FontHeight", &BND_TextDot::GetFontHeight, &BND_TextDot::SetFontHeight)
    .def_property("FontFace", &BND_TextDot::GetFontFace, &BND_TextDot::SetFontFace)
    ;

}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initAnnotationBaseBindings(void*)
{
  class_<BND_AnnotationBase, base<BND_GeometryBase>>("AnnotationBase")
    .property("annotationType", &BND_AnnotationBase::AnnotationType)
    .property("dimensionStyleId", &BND_AnnotationBase::DimensionStyleId)
    .property("richText", &BND_AnnotationBase::RichText)
    .property("plainText", &BND_AnnotationBase::PlainText)
    .property("plainTextWithFields", &BND_AnnotationBase::PlainTextWithFields)
    .property("plane", &BND_AnnotationBase::Plane)
    .property("textIsWrapped", &BND_AnnotationBase::TextIsWrapped, &BND_AnnotationBase::SetTextIsWrapped)
    .property("hasPropertyOverrides", &BND_AnnotationBase::HasPropertyOverrides)
    .property("textHasRtfFormatting", &BND_AnnotationBase::TextHasRtfFormatting)
    .property("textRotationRadians", &BND_AnnotationBase::TextRotationRadians, &BND_AnnotationBase::SetTextRotationRadians)
    .property("textRotationDegrees", &BND_AnnotationBase::TextRotationDegrees, &BND_AnnotationBase::SetTextRotationDegrees)
    .function("wrapText", &BND_AnnotationBase::WrapText)
    .function("getDimensionStyle", &BND_AnnotationBase::GetDimensionStyle, allow_raw_pointers())
    .function("isPropertyOverridden", &BND_AnnotationBase::IsPropertyOverridden)
    .function("clearPropertyOverrides", &BND_AnnotationBase::ClearPropertyOverrides)
    .function("setOverrideDimStyle", &BND_AnnotationBase::SetOverrideDimStyle)
    .function("setRichText", &BND_AnnotationBase::SetRichText)
    .class_function("plainTextToRtf", &BND_AnnotationBase::PlainTextToRtf)
    .function("runReplace", &BND_AnnotationBase::RunReplace)
    .function("getTextHeight", &BND_AnnotationBase::TextHeight)
    .function("setTextHeight", &BND_AnnotationBase::SetTextHeight)
    .function("getDimensionScale", &BND_AnnotationBase::DimensionScale)
    .function("setDimensionScale", &BND_AnnotationBase::SetDimensionScale)
    .function("getMaskEnabled", &BND_AnnotationBase::MaskEnabled)
    .function("setMaskEnabled", &BND_AnnotationBase::SetMaskEnabled)
    .function("getMaskColorSource", &BND_AnnotationBase::MaskColorSource)
    .function("setMaskColorSource", &BND_AnnotationBase::SetMaskColorSource)
    .function("getMaskFrame", &BND_AnnotationBase::MaskFrame)
    .function("setMaskFrame", &BND_AnnotationBase::SetMaskFrame)
    .function("getMaskColor", &BND_AnnotationBase::MaskColor)
    .function("setMaskColor", &BND_AnnotationBase::SetMaskColor)
    .function("getMaskOffset", &BND_AnnotationBase::MaskOffset)
    .function("setMaskOffset", &BND_AnnotationBase::SetMaskOffset)
    .function("getDimensionLengthDisplay", &BND_AnnotationBase::DimensionLengthDisplay)
    .function("setDimensionLengthDisplay", &BND_AnnotationBase::SetDimensionLengthDisplay)
    .function("getAlternateDimensionLengthDisplay", &BND_AnnotationBase::AlternateDimensionLengthDisplay)
    .function("setAlternateDimensionLengthDisplay", &BND_AnnotationBase::SetAlternateDimensionLengthDisplay)
    .function("getFont", &BND_AnnotationBase::GetFont, allow_raw_pointers())
    .function("setFont", &BND_AnnotationBase::SetFont, allow_raw_pointers())
    .function("getBoundingBox", &BND_AnnotationBase::GetBoundingBox)
    ;

  class_<BND_TextDot, base<BND_GeometryBase>>("TextDot")
    .constructor<const std::wstring&, ON_3dPoint>()
    .property("point", &BND_TextDot::GetLocation, &BND_TextDot::SetLocation)
    .property("text", &BND_TextDot::GetText, &BND_TextDot::SetText)
    .property("secondaryText", &BND_TextDot::GetSecondaryText, &BND_TextDot::SetSecondaryText)
    .property("fontHeight", &BND_TextDot::GetFontHeight, &BND_TextDot::SetFontHeight)
    .property("fontFace", &BND_TextDot::GetFontFace, &BND_TextDot::SetFontFace)
    ;

  class_<BND_Leader, base<BND_AnnotationBase>>("Leader")
    .property("points", &BND_Leader::GetPoints)
    .function("getTextPoint2d", &BND_Leader::GetTextPoint2d, allow_raw_pointers())
    ;

  class_<BND_Text, base<BND_AnnotationBase>>("Text")
    ;

  class_<BND_Dimension, base<BND_AnnotationBase>>("Dimension")
    ;

  class_<BND_DimLinear, base<BND_Dimension>>("DimLinear")
    .property("points", &BND_DimLinear::GetPoints)
    .function("getDisplayLines", &BND_DimLinear::GetDisplayLines, allow_raw_pointers())
    ;

  class_<BND_DimAngular, base<BND_Dimension>>("DimAngular")
    .property("points", &BND_DimAngular::GetPoints)
    .property("radius", &BND_DimAngular::Radius)
    .property("angle", &BND_DimAngular::Measurement)
    .function("getDisplayLines", &BND_DimAngular::GetDisplayLines, allow_raw_pointers())
    ;

  class_<BND_DimRadial, base<BND_Dimension>>("DimRadial")
    .property("points", &BND_DimRadial::GetPoints)
    .function("getDisplayLines", &BND_DimRadial::GetDisplayLines, allow_raw_pointers())
    ;

  class_<BND_DimOrdinate, base<BND_Dimension>>("DimOrdinate")
    .property("points", &BND_DimOrdinate::GetPoints)
    .function("getDisplayLines", &BND_DimOrdinate::GetDisplayLines, allow_raw_pointers())
    ;

  class_<BND_Centermark, base<BND_Dimension>>("Centermark")
    .function("getDisplayLines", &BND_Centermark::GetDisplayLines, allow_raw_pointers())
    ;

  register_vector<ON_Line>("vector<ON_Line>");
  register_vector<ON_3dPoint>("vector<ON_3dPoint>");
  register_vector<BND_Arc>("vector<BND_Arc>");

}
#endif
