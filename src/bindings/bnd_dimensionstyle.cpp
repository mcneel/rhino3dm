#include "bindings.h"

BND_DimensionStyle::BND_DimensionStyle()
{
  SetTrackedPointer(new ON_DimStyle(), nullptr);
}

BND_DimensionStyle::BND_DimensionStyle(ON_DimStyle* dimstyle, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(dimstyle, compref);
}

void BND_DimensionStyle::SetTrackedPointer(ON_DimStyle* dimstyle, const ON_ModelComponentReference* compref)
{
  m_dimstyle = dimstyle;
  BND_CommonObject::SetTrackedPointer(dimstyle, compref);
}

BND_Font* BND_DimensionStyle::GetFont() const
{
  const ON_Font& font = m_dimstyle->Font();
  return new BND_Font(font);
}

void BND_DimensionStyle::SetFont(const BND_Font* font)
{
  if (font)
    m_dimstyle->SetFont(*font->m_managed_font);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDimensionStyleBindings(pybind11::module& m)
{
  py::class_<BND_DimensionStyle, BND_CommonObject>(m, "DimensionStyle")
    .def(py::init<>())
    .def_property("Name", &BND_DimensionStyle::GetName, &BND_DimensionStyle::SetName)
    .def_property("Font", &BND_DimensionStyle::GetFont, &BND_DimensionStyle::SetFont)
    .def("ScaleLengthValues", &BND_DimensionStyle::ScaleLengthValues)
    .def_property("ArrowBlockId1", &BND_DimensionStyle::GetArrowBlockId1, &BND_DimensionStyle::SetArrowBlockId1)
    .def_property("ArrowBlockId2", &BND_DimensionStyle::GetArrowBlockId2, &BND_DimensionStyle::SetArrowBlockId2)
    .def_property("LeaderArrowBlockId", &BND_DimensionStyle::GetLeaderArrowBlockId, &BND_DimensionStyle::SetLeaderArrowBlockId)
    .def_property("SuppressExtension1", &BND_DimensionStyle::GetSuppressExtension1, &BND_DimensionStyle::SetSuppressExtension1)
    .def_property("SuppressExtension2", &BND_DimensionStyle::GetSuppressExtension2, &BND_DimensionStyle::SetSuppressExtension2)
    .def_property("SuppressArrow1", &BND_DimensionStyle::GetSuppressArrow1, &BND_DimensionStyle::SetSuppressArrow1)
    .def_property("SuppressArrow2", &BND_DimensionStyle::GetSuppressArrow2, &BND_DimensionStyle::SetSuppressArrow2)
    .def_property("AlternateBelowLine", &BND_DimensionStyle::GetAlternateBelowLine, &BND_DimensionStyle::SetAlternateBelowLine)
    .def_property("DrawTextMask", &BND_DimensionStyle::GetDrawTextMask, &BND_DimensionStyle::SetDrawTextMask)
    .def_property("LeaderHasLanding", &BND_DimensionStyle::GetLeaderHasLanding, &BND_DimensionStyle::SetLeaderHasLanding)
    .def_property("DrawForward", &BND_DimensionStyle::GetDrawForward, &BND_DimensionStyle::SetDrawForward)
    .def_property("TextUnderlined", &BND_DimensionStyle::GetTextUnderlined, &BND_DimensionStyle::SetTextUnderlined)
    .def_property("ArrowLength", &BND_DimensionStyle::GetArrowSize, &BND_DimensionStyle::SetArrowSize)
    .def_property("LeaderArrowLength", &BND_DimensionStyle::GetLeaderArrowSize, &BND_DimensionStyle::SetLeaderArrowSize)
    .def_property("CentermarkSize", &BND_DimensionStyle::GetCenterMark, &BND_DimensionStyle::SetCenterMark)
    .def_property("TextGap", &BND_DimensionStyle::GetTextGap, &BND_DimensionStyle::SetTextGap)
    .def_property("TextHEight", &BND_DimensionStyle::GetTextHeight, &BND_DimensionStyle::SetTextHeight)
    .def_property("LengthFactor", &BND_DimensionStyle::GetLengthFactor, &BND_DimensionStyle::SetLengthFactor)
    .def_property("AlternateLengthFactor", &BND_DimensionStyle::GetAlternateLengthFactor, &BND_DimensionStyle::SetAlternateLengthFactor)
    .def_property("ToleranceUpperValue", &BND_DimensionStyle::GetToleranceUpperValue, &BND_DimensionStyle::SetToleranceUpperValue)
    .def_property("ToleranceLowerValue", &BND_DimensionStyle::GetToleranceLowerValue, &BND_DimensionStyle::SetToleranceLowerValue)
    .def_property("ToleranceHeightScale", &BND_DimensionStyle::GetToleranceHeightScale, &BND_DimensionStyle::SetToleranceHeightScale)
    .def_property("BaselineSpacing", &BND_DimensionStyle::GetBaselineSpacing, &BND_DimensionStyle::SetBaselineSpacing)
    .def_property("TextRotation", &BND_DimensionStyle::GetTextRotation, &BND_DimensionStyle::SetTextRotation)
    .def_property("StackHeightScale", &BND_DimensionStyle::GetStackHeightScale, &BND_DimensionStyle::SetStackHeightScale)
    .def_property("LeaderLandingLength", &BND_DimensionStyle::GetLeaderLandingLength, &BND_DimensionStyle::SetLeaderLandingLength)
    .def("ClearAllFieldOverrides", &BND_DimensionStyle::ClearAllFieldOverrides)
    .def_property_readonly("HasFieldOverrides", &BND_DimensionStyle::HasFieldOverrides)
    .def_property_readonly("IsChild", &BND_DimensionStyle::IsChild)
    .def("IsChildOf", &BND_DimensionStyle::IsChildOf)
    .def_property("ParentId", &BND_DimensionStyle::GetParentId, &BND_DimensionStyle::SetParentId)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDimensionStyleBindings(void*)
{
  class_<BND_DimensionStyle, base<BND_CommonObject>>("DimensionStyle")
    .constructor<>()
    .property("name", &BND_DimensionStyle::GetName, &BND_DimensionStyle::SetName)
    .function("getFont", &BND_DimensionStyle::GetFont, allow_raw_pointers())
    .function("setFont", &BND_DimensionStyle::SetFont, allow_raw_pointers())
    .function("scaleLengthValues", &BND_DimensionStyle::ScaleLengthValues)
    .property("arrowBlockId1", &BND_DimensionStyle::GetArrowBlockId1, &BND_DimensionStyle::SetArrowBlockId1)
    .property("arrowBlockId2", &BND_DimensionStyle::GetArrowBlockId2, &BND_DimensionStyle::SetArrowBlockId2)
    .property("leaderArrowBlockId", &BND_DimensionStyle::GetLeaderArrowBlockId, &BND_DimensionStyle::SetLeaderArrowBlockId)
    .property("suppressExtension1", &BND_DimensionStyle::GetSuppressExtension1, &BND_DimensionStyle::SetSuppressExtension1)
    .property("suppressExtension2", &BND_DimensionStyle::GetSuppressExtension2, &BND_DimensionStyle::SetSuppressExtension2)
    .property("suppressArrow1", &BND_DimensionStyle::GetSuppressArrow1, &BND_DimensionStyle::SetSuppressArrow1)
    .property("suppressArrow2", &BND_DimensionStyle::GetSuppressArrow2, &BND_DimensionStyle::SetSuppressArrow2)
    .property("alternateBelowLine", &BND_DimensionStyle::GetAlternateBelowLine, &BND_DimensionStyle::SetAlternateBelowLine)
    .property("drawTextMask", &BND_DimensionStyle::GetDrawTextMask, &BND_DimensionStyle::SetDrawTextMask)
    .property("leaderHasLanding", &BND_DimensionStyle::GetLeaderHasLanding, &BND_DimensionStyle::SetLeaderHasLanding)
    .property("drawForward", &BND_DimensionStyle::GetDrawForward, &BND_DimensionStyle::SetDrawForward)
    .property("textUnderlined", &BND_DimensionStyle::GetTextUnderlined, &BND_DimensionStyle::SetTextUnderlined)
    .property("arrowLength", &BND_DimensionStyle::GetArrowSize, &BND_DimensionStyle::SetArrowSize)
    .property("leaderArrowLength", &BND_DimensionStyle::GetLeaderArrowSize, &BND_DimensionStyle::SetLeaderArrowSize)
    .property("centermarkSize", &BND_DimensionStyle::GetCenterMark, &BND_DimensionStyle::SetCenterMark)
    .property("textGap", &BND_DimensionStyle::GetTextGap, &BND_DimensionStyle::SetTextGap)
    .property("textHEight", &BND_DimensionStyle::GetTextHeight, &BND_DimensionStyle::SetTextHeight)
    .property("lengthFactor", &BND_DimensionStyle::GetLengthFactor, &BND_DimensionStyle::SetLengthFactor)
    .property("alternateLengthFactor", &BND_DimensionStyle::GetAlternateLengthFactor, &BND_DimensionStyle::SetAlternateLengthFactor)
    .property("toleranceUpperValue", &BND_DimensionStyle::GetToleranceUpperValue, &BND_DimensionStyle::SetToleranceUpperValue)
    .property("toleranceLowerValue", &BND_DimensionStyle::GetToleranceLowerValue, &BND_DimensionStyle::SetToleranceLowerValue)
    .property("toleranceHeightScale", &BND_DimensionStyle::GetToleranceHeightScale, &BND_DimensionStyle::SetToleranceHeightScale)
    .property("baselineSpacing", &BND_DimensionStyle::GetBaselineSpacing, &BND_DimensionStyle::SetBaselineSpacing)
    .property("textRotation", &BND_DimensionStyle::GetTextRotation, &BND_DimensionStyle::SetTextRotation)
    .property("stackHeightScale", &BND_DimensionStyle::GetStackHeightScale, &BND_DimensionStyle::SetStackHeightScale)
    .property("leaderLandingLength", &BND_DimensionStyle::GetLeaderLandingLength, &BND_DimensionStyle::SetLeaderLandingLength)
    .function("clearAllFieldOverrides", &BND_DimensionStyle::ClearAllFieldOverrides)
    .property("hasFieldOverrides", &BND_DimensionStyle::HasFieldOverrides)
    .property("isChild", &BND_DimensionStyle::IsChild)
    .function("isChildOf", &BND_DimensionStyle::IsChildOf)
    .property("parentId", &BND_DimensionStyle::GetParentId, &BND_DimensionStyle::SetParentId)
    ;
}
#endif
