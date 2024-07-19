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

ON_Arrowhead::arrow_type BND_DimensionStyle::ArrowType1() const
{
  return m_dimstyle->ArrowType1();
}

ON_Arrowhead::arrow_type BND_DimensionStyle::ArrowType2() const
{
  return m_dimstyle->ArrowType2();
}

ON_Arrowhead::arrow_type BND_DimensionStyle::LeaderArrowType() const
{
  return m_dimstyle->LeaderArrowType();
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDimensionStyleBindings(pybind11::module& m)
{
  py::class_<BND_DimensionStyle, BND_CommonObject> pyDimStyle(m, "DimensionStyle");

  pyDimStyle.def(py::init<>())
    .def_property("Name", &BND_DimensionStyle::GetName, &BND_DimensionStyle::SetName)
    .def_property("Font", &BND_DimensionStyle::GetFont, &BND_DimensionStyle::SetFont)
    .def("ScaleLengthValues", &BND_DimensionStyle::ScaleLengthValues, py::arg("scale"))
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
    .def_property_readonly("ArrowType1", &BND_DimensionStyle::ArrowType1)
    .def_property_readonly("ArrowType2", &BND_DimensionStyle::ArrowType2)
    .def_property_readonly("LeaderArrowType", &BND_DimensionStyle::LeaderArrowType)
    .def_property("CentermarkSize", &BND_DimensionStyle::GetCenterMark, &BND_DimensionStyle::SetCenterMark)
    .def_property("TextGap", &BND_DimensionStyle::GetTextGap, &BND_DimensionStyle::SetTextGap)
    .def_property("TextHEight", &BND_DimensionStyle::GetTextHeight, &BND_DimensionStyle::SetTextHeight)
    .def_property("TextHeight", &BND_DimensionStyle::GetTextHeight, &BND_DimensionStyle::SetTextHeight)
    .def_property("LengthFactor", &BND_DimensionStyle::GetLengthFactor, &BND_DimensionStyle::SetLengthFactor)
    .def_property("AlternateLengthFactor", &BND_DimensionStyle::GetAlternateLengthFactor, &BND_DimensionStyle::SetAlternateLengthFactor)
    .def_property("ToleranceUpperValue", &BND_DimensionStyle::GetToleranceUpperValue, &BND_DimensionStyle::SetToleranceUpperValue)
    .def_property("ToleranceLowerValue", &BND_DimensionStyle::GetToleranceLowerValue, &BND_DimensionStyle::SetToleranceLowerValue)
    .def_property("ToleranceHeightScale", &BND_DimensionStyle::GetToleranceHeightScale, &BND_DimensionStyle::SetToleranceHeightScale)
    .def_property("BaselineSpacing", &BND_DimensionStyle::GetBaselineSpacing, &BND_DimensionStyle::SetBaselineSpacing)
    .def_property("TextRotation", &BND_DimensionStyle::GetTextRotation, &BND_DimensionStyle::SetTextRotation)
    .def_property("StackHeightScale", &BND_DimensionStyle::GetStackHeightScale, &BND_DimensionStyle::SetStackHeightScale)
    .def_property("LeaderLandingLength", &BND_DimensionStyle::GetLeaderLandingLength, &BND_DimensionStyle::SetLeaderLandingLength)
    .def_property("ExtensionLineExtension", &BND_DimensionStyle::GetExtExtension, &BND_DimensionStyle::SetExtExtension)
    .def_property("ExtensionLineOffset", &BND_DimensionStyle::GetExtOffset, &BND_DimensionStyle::SetExtOffset)
    .def_property("DimensionLineExtension", &BND_DimensionStyle::GetDimExtension, &BND_DimensionStyle::SetDimExtension)
    .def_property("FixedExtensionLength", &BND_DimensionStyle::GetFixedExtensionLen, &BND_DimensionStyle::SetFixedExtensionLen)
    .def_property("FixedExtensionLengthOn", &BND_DimensionStyle::GetFixedExtensionLenOn, &BND_DimensionStyle::SetFixedExtensionLenOn)
    .def("IsFieldOverridden", &BND_DimensionStyle::IsFieldOverriden, py::arg("field"))
    .def("SetFieldOverride", &BND_DimensionStyle::SetFieldOverride, py::arg("field"))
    .def("ClearFieldOverride", &BND_DimensionStyle::ClearFieldOverride, py::arg("field"))
    .def("ClearAllFieldOverrides", &BND_DimensionStyle::ClearAllFieldOverrides)
    .def_property_readonly("HasFieldOverrides", &BND_DimensionStyle::HasFieldOverrides)
    .def_property_readonly("IsChild", &BND_DimensionStyle::IsChild)
    .def("IsChildOf", &BND_DimensionStyle::IsChildOf, py::arg("id"))
    .def_property("ParentId", &BND_DimensionStyle::GetParentId, &BND_DimensionStyle::SetParentId)
    ;

  py::enum_<ON_DimStyle::field>(pyDimStyle, "Field")
    .value("Unset", ON_DimStyle::field::Unset)
    .value("Name", ON_DimStyle::field::Name)
    .value("Index", ON_DimStyle::field::Index)
    .value("ExtensionLineExtension", ON_DimStyle::field::ExtensionLineExtension)
    .value("ExtensionLineOffset", ON_DimStyle::field::ExtensionLineOffset)
    .value("ArrowSize", ON_DimStyle::field::Arrowsize)
    .value("LeaderArrowSize", ON_DimStyle::field::LeaderArrowsize)
    .value("Centermark", ON_DimStyle::field::Centermark)
    .value("TextGap", ON_DimStyle::field::TextGap)
    .value("TextHeight", ON_DimStyle::field::TextHeight)
    .value("DimTextLocation", ON_DimStyle::field::DimTextLocation)
    .value("LengthResolution", ON_DimStyle::field::LengthResolution)
    .value("AngleFormat", ON_DimStyle::field::AngleFormat)
    .value("AngleResolution", ON_DimStyle::field::AngleResolution)
    .value("Font", ON_DimStyle::field::Font)
    .value("LengthFactor", ON_DimStyle::field::LengthFactor)
    .value("Alternate", ON_DimStyle::field::Alternate)
    .value("AlternateLengthFactor", ON_DimStyle::field::AlternateLengthFactor)
    .value("AlternateLengthResolution", ON_DimStyle::field::AlternateLengthResolution)
    .value("Prefix", ON_DimStyle::field::Prefix)
    .value("Suffix", ON_DimStyle::field::Suffix)
    .value("AlternatePrefix", ON_DimStyle::field::AlternatePrefix)
    .value("AlternateSuffix", ON_DimStyle::field::AlternateSuffix)
    .value("DimensionLineExtension", ON_DimStyle::field::DimensionLineExtension)
    .value("SuppressExtension1", ON_DimStyle::field::SuppressExtension1)
    .value("SuppressExtension2", ON_DimStyle::field::SuppressExtension2)
    .value("ExtLineColorSource", ON_DimStyle::field::ExtLineColorSource)
    .value("DimLineColorSource", ON_DimStyle::field::DimLineColorSource)
    .value("ArrowColorSource", ON_DimStyle::field::ArrowColorSource)
    .value("TextColorSource", ON_DimStyle::field::TextColorSource)
    .value("ExtLineColor", ON_DimStyle::field::ExtLineColor)
    .value("DimLineColor", ON_DimStyle::field::DimLineColor)
    .value("ArrowColor", ON_DimStyle::field::ArrowColor)
    .value("TextColor", ON_DimStyle::field::TextColor)
    .value("ExtLinePlotColorSource", ON_DimStyle::field::ExtLinePlotColorSource)
    .value("DimLinePlotColorSource", ON_DimStyle::field::DimLinePlotColorSource)
    .value("ArrowPlotColorSource", ON_DimStyle::field::ArrowPlotColorSource)
    .value("TextPlotColorSource", ON_DimStyle::field::TextPlotColorSource)
    .value("ExtLinePlotColor", ON_DimStyle::field::ExtLinePlotColor)
    .value("DimLinePlotColor", ON_DimStyle::field::DimLinePlotColor)
    .value("ArrowPlotColor", ON_DimStyle::field::ArrowPlotColor)
    .value("TextPlotColor", ON_DimStyle::field::TextPlotColor)
    .value("ExtLinePlotWeightSource", ON_DimStyle::field::ExtLinePlotWeightSource)
    .value("DimLinePlotWeightSource", ON_DimStyle::field::DimLinePlotWeightSource)
    .value("ExtLinePlotWeight_mm", ON_DimStyle::field::ExtLinePlotWeight_mm)
    .value("DimLinePlotWeight_mm", ON_DimStyle::field::DimLinePlotWeight_mm)
    .value("ToleranceFormat", ON_DimStyle::field::ToleranceFormat)
    .value("ToleranceResolution", ON_DimStyle::field::ToleranceResolution)
    .value("ToleranceUpperValue", ON_DimStyle::field::ToleranceUpperValue)
    .value("ToleranceLowerValue", ON_DimStyle::field::ToleranceLowerValue)
    .value("AltToleranceResolution", ON_DimStyle::field::AltToleranceResolution)
    .value("ToleranceHeightScale", ON_DimStyle::field::ToleranceHeightScale)
    .value("BaselineSpacing", ON_DimStyle::field::BaselineSpacing)
    .value("DrawMask", ON_DimStyle::field::DrawMask)
    .value("MaskColorSource", ON_DimStyle::field::MaskColorSource)
    .value("MaskColor", ON_DimStyle::field::MaskColor)
    .value("MaskBorder", ON_DimStyle::field::MaskBorder)
    .value("DimensionScale", ON_DimStyle::field::DimensionScale)
    .value("DimscaleSource", ON_DimStyle::field::DimscaleSource)
    .value("FixedExtensionLength", ON_DimStyle::field::FixedExtensionLength)
    .value("FixedExtensionOn", ON_DimStyle::field::FixedExtensionOn)
    .value("TextRotation", ON_DimStyle::field::TextRotation)
    .value("SuppressArrow1", ON_DimStyle::field::SuppressArrow1)
    .value("SuppressArrow2", ON_DimStyle::field::SuppressArrow2)
    .value("TextmoveLeader", ON_DimStyle::field::TextmoveLeader)
    .value("ArclengthSymbol", ON_DimStyle::field::ArclengthSymbol)
    .value("StackTextheightScale", ON_DimStyle::field::StackTextheightScale)
    .value("StackFormat", ON_DimStyle::field::StackFormat)
    .value("AltRound", ON_DimStyle::field::AltRound)
    .value("Round", ON_DimStyle::field::Round)
    .value("AngularRound", ON_DimStyle::field::AngularRound)
    .value("AltZeroSuppress", ON_DimStyle::field::AltZeroSuppress)
    .value("AngleZeroSuppress", ON_DimStyle::field::AngleZeroSuppress)
    .value("AltBelow", ON_DimStyle::field::AltBelow)
    .value("ArrowType1", ON_DimStyle::field::ArrowType1)
    .value("ArrowType2", ON_DimStyle::field::ArrowType2)
    .value("LeaderArrowType", ON_DimStyle::field::LeaderArrowType)
    .value("ArrowBlockId1", ON_DimStyle::field::ArrowBlockId1)
    .value("ArrowBlockId2", ON_DimStyle::field::ArrowBlockId2)
    .value("LeaderArrowBlock", ON_DimStyle::field::LeaderArrowBlock)
    .value("DimRadialTextLocation", ON_DimStyle::field::DimRadialTextLocation)
    .value("TextVerticalAlignment", ON_DimStyle::field::TextVerticalAlignment)
    .value("LeaderTextVerticalAlignment", ON_DimStyle::field::LeaderTextVerticalAlignment)
    .value("LeaderContentAngleStyle", ON_DimStyle::field::LeaderContentAngleStyle)
    .value("LeaderCurveType", ON_DimStyle::field::LeaderCurveType)
    .value("LeaderContentAngle", ON_DimStyle::field::LeaderContentAngle)
    .value("LeaderHasLanding", ON_DimStyle::field::LeaderHasLanding)
    .value("LeaderLandingLength", ON_DimStyle::field::LeaderLandingLength)
    .value("MaskFlags", ON_DimStyle::field::MaskFlags)
    .value("CentermarkStyle", ON_DimStyle::field::CentermarkStyle)
    .value("TextHorizontalAlignment", ON_DimStyle::field::TextHorizontalAlignment)
    .value("LeaderTextHorizontalAlignment", ON_DimStyle::field::LeaderTextHorizontalAlignment)
    .value("DrawForward", ON_DimStyle::field::DrawForward)
    .value("SignedOrdinate", ON_DimStyle::field::SignedOrdinate)
    .value("UnitSystem", ON_DimStyle::field::UnitSystem)
    .value("TextMask", ON_DimStyle::field::TextMask)
    .value("TextOrientation", ON_DimStyle::field::TextOrientation)
    .value("LeaderTextOrientation", ON_DimStyle::field::LeaderTextOrientation)
    .value("DimTextOrientation", ON_DimStyle::field::DimTextOrientation)
    .value("DimRadialTextOrientation", ON_DimStyle::field::DimRadialTextOrientation)
    .value("DimTextAngleStyle", ON_DimStyle::field::DimTextAngleStyle)
    .value("DimRadialTextAngleStyle", ON_DimStyle::field::DimRadialTextAngleStyle)
    .value("TextUnderlined", ON_DimStyle::field::TextUnderlined)
    .value("DimensionLengthDisplay", ON_DimStyle::field::DimensionLengthDisplay)
    .value("AlternateDimensionLengthDisplay", ON_DimStyle::field::AlternateDimensionLengthDisplay)
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
    .property("arrowType1", &BND_DimensionStyle::ArrowType1)
    .property("arrowType2", &BND_DimensionStyle::ArrowType2)
    .property("leaderArrowType", &BND_DimensionStyle::LeaderArrowType)
    .property("centermarkSize", &BND_DimensionStyle::GetCenterMark, &BND_DimensionStyle::SetCenterMark)
    .property("textGap", &BND_DimensionStyle::GetTextGap, &BND_DimensionStyle::SetTextGap)
    .property("textHeight", &BND_DimensionStyle::GetTextHeight, &BND_DimensionStyle::SetTextHeight)
    .property("lengthFactor", &BND_DimensionStyle::GetLengthFactor, &BND_DimensionStyle::SetLengthFactor)
    .property("alternateLengthFactor", &BND_DimensionStyle::GetAlternateLengthFactor, &BND_DimensionStyle::SetAlternateLengthFactor)
    .property("toleranceUpperValue", &BND_DimensionStyle::GetToleranceUpperValue, &BND_DimensionStyle::SetToleranceUpperValue)
    .property("toleranceLowerValue", &BND_DimensionStyle::GetToleranceLowerValue, &BND_DimensionStyle::SetToleranceLowerValue)
    .property("toleranceHeightScale", &BND_DimensionStyle::GetToleranceHeightScale, &BND_DimensionStyle::SetToleranceHeightScale)
    .property("baselineSpacing", &BND_DimensionStyle::GetBaselineSpacing, &BND_DimensionStyle::SetBaselineSpacing)
    .property("textRotation", &BND_DimensionStyle::GetTextRotation, &BND_DimensionStyle::SetTextRotation)
    .property("stackHeightScale", &BND_DimensionStyle::GetStackHeightScale, &BND_DimensionStyle::SetStackHeightScale)
    .property("leaderLandingLength", &BND_DimensionStyle::GetLeaderLandingLength, &BND_DimensionStyle::SetLeaderLandingLength)
    .property("extensionLineExtension", &BND_DimensionStyle::GetExtExtension, &BND_DimensionStyle::SetExtExtension)
    .property("extensionLineOffset", &BND_DimensionStyle::GetExtOffset, &BND_DimensionStyle::SetExtOffset)
    .property("dimensionLineExtension", &BND_DimensionStyle::GetDimExtension, &BND_DimensionStyle::SetDimExtension)
    .property("fixedExtensionLength", &BND_DimensionStyle::GetFixedExtensionLen, &BND_DimensionStyle::SetFixedExtensionLen)
    .property("fixedExtensionLengthOn", &BND_DimensionStyle::GetFixedExtensionLenOn, &BND_DimensionStyle::SetFixedExtensionLenOn)
    .function("clearAllFieldOverrides", &BND_DimensionStyle::ClearAllFieldOverrides)
    .property("hasFieldOverrides", &BND_DimensionStyle::HasFieldOverrides)
    .property("isChild", &BND_DimensionStyle::IsChild)
    .function("isChildOf", &BND_DimensionStyle::IsChildOf)
    .property("parentId", &BND_DimensionStyle::GetParentId, &BND_DimensionStyle::SetParentId)
    ;
}
#endif
