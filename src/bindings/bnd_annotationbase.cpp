#include "bindings.h"


BND_AnnotationBase::BND_AnnotationBase(ON_Annotation* annotation, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(annotation, compref);
}

void BND_AnnotationBase::SetTrackedPointer(ON_Annotation* annotation, const ON_ModelComponentReference* compref)
{
  m_annotation = annotation;
  BND_GeometryBase::SetTrackedPointer(annotation, compref);
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


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initAnnotationBaseBindings(pybind11::module& m)
{
  py::class_<BND_AnnotationBase, BND_GeometryBase>(m, "AnnotationBase")
    .def_property_readonly("RichText", &BND_AnnotationBase::RichText)
    .def_property_readonly("PlainText", &BND_AnnotationBase::PlainText)
    .def_property_readonly("PlainTextWithFields", &BND_AnnotationBase::PlainTextWithFields)
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
    .property("richText", &BND_AnnotationBase::RichText)
    .property("plainText", &BND_AnnotationBase::PlainText)
    //.property("plainTextWithFields", &BND_AnnotationBase::PlainTextWithFields)
    ;

  class_<BND_TextDot, base<BND_GeometryBase>>("TextDot")
    .constructor<const std::wstring&, ON_3dPoint>()
    .property("point", &BND_TextDot::GetLocation, &BND_TextDot::SetLocation)
    .property("text", &BND_TextDot::GetText, &BND_TextDot::SetText)
    .property("secondaryText", &BND_TextDot::GetSecondaryText, &BND_TextDot::SetSecondaryText)
    .property("fontHeight", &BND_TextDot::GetFontHeight, &BND_TextDot::SetFontHeight)
    .property("fontFace", &BND_TextDot::GetFontFace, &BND_TextDot::SetFontFace)
    ;
}
#endif
