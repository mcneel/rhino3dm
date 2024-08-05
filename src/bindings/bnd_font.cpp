#include "bindings.h"

BND_Font::BND_Font(const ON_Font& font)
{
  m_managed_font = ON_Font::GetManagedFont(font, true);
}

BND_Font::BND_Font(std::wstring familyName)
{
  m_managed_font = ON_Font::GetManagedFont(familyName.c_str(),
      ON_Font::Weight::Normal,
      ON_Font::Style::Upright,
      ON_Font::Stretch::Medium,
      false,
      false,
      ON_FontMetrics::DefaultLineFeedRatio,
      ON_Font::Default.LogfontCharSet()
    );
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initFontBindings(py::module_& m){}
#else
namespace py = pybind11;
void initFontBindings(py::module& m)
{
  py::class_<BND_Font>(m, "Font")
    .def(py::init<std::wstring>(), py::arg("familyName"))
    .def_property_readonly("QuartetName", &BND_Font::QuartetName)
    .def_property_readonly("FaceName", &BND_Font::FaceName)
    .def_property_readonly("PostScriptName", &BND_Font::PostScriptName)
    .def_property_readonly("RichTextFontName", &BND_Font::RichTextFontName)
    .def_property_readonly("Bold", &BND_Font::Bold)
    .def_property_readonly("Italic", &BND_Font::Italic)
    .def_property_readonly("Underlined", &BND_Font::Underlined)
    .def_property_readonly("StrikeOut", &BND_Font::StrikeOut)
    .def_property_readonly("IsEngravingFont", &BND_Font::IsEngravingFont)
    .def_property_readonly("IsSymbolFont", &BND_Font::IsSymbolFont)
    .def_property_readonly("IsSingleStrokeFont", &BND_Font::IsSingleStrokeFont)
    .def_property_readonly("IsSimulated", &BND_Font::IsSimulated)
    .def_property_readonly("PointSize", &BND_Font::PointSize)
    .def_property_readonly("FamilyName", &BND_Font::FamilyName)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initFontBindings(void*)
{
  class_<BND_Font>("Font")
    .constructor<std::wstring>()
    .property("quartetName", &BND_Font::QuartetName)
    .property("faceName", &BND_Font::FaceName)
    .property("postScriptName", &BND_Font::PostScriptName)
    .property("richTextFontName", &BND_Font::RichTextFontName)
    .property("bold", &BND_Font::Bold)
    .property("italic", &BND_Font::Italic)
    .property("underlined", &BND_Font::Underlined)
    .property("strikeOut", &BND_Font::StrikeOut)
    .property("isEngravingFont", &BND_Font::IsEngravingFont)
    .property("isSymbolFont", &BND_Font::IsSymbolFont)
    .property("isSingleStrokeFont", &BND_Font::IsSingleStrokeFont)
    .property("isSimulated", &BND_Font::IsSimulated)
    .property("pointSize", &BND_Font::PointSize)
    .property("familyName", &BND_Font::FamilyName)
    ;
}
#endif
