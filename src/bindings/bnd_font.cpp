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
namespace py = pybind11;
void initFontBindings(pybind11::module& m)
{
  py::class_<BND_Font>(m, "Font")
    .def(py::init<std::wstring>())
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

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initFontBindings(void*)
{
}
#endif
