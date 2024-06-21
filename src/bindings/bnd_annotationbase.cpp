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
  throw pybind11::value_error("Failed to get DimLinear points");
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
  throw pybind11::value_error("Failed to get DimAngular points");
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
  throw pybind11::value_error("Failed to get DimRadial points");
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
  throw pybind11::value_error("Failed to get DimOrdinate points");
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
namespace py = pybind11;
void initAnnotationBaseBindings(pybind11::module& m)
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
