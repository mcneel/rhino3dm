#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initFontBindings(pybind11::module& m);
#else
void initFontBindings(void* m);
#endif

class BND_Font
{
public:
  const ON_Font* m_managed_font = nullptr;

public:
  BND_Font(const ON_Font& font);

  BND_Font(std::wstring familyName);
  //BND_Font(std::wstring familyName, FontWeight weight, FontStyle style, bool underlined, bool strikethrough);
  //static BND_Font* FromQuartetProperties(std::wstring quartetName, bool bold, bool italic);

  std::wstring QuartetName() const { return std::wstring(m_managed_font->QuartetName()); }
  std::wstring FaceName() const { return std::wstring(m_managed_font->FaceName()); }
  //std::wstring EnglishFaceName() const { return std::wstring(m_managed_font->EnglishFaceName()); }
  //public string LogfontName | get;
  //public string FamilyPlusFaceName = > FamilyName + (string.IsNullOrEmpty(FaceName) ? "" : " " + FaceName);
  std::wstring PostScriptName() const { return std::wstring(m_managed_font->PostScriptName()); }
  std::wstring RichTextFontName() const { return std::wstring(m_managed_font->RichTextFontName()); }
  //public string Description | get;
  bool Bold() const { return m_managed_font->IsBold(); }
  bool Italic() const { return m_managed_font->IsItalic(); }
  bool Underlined() const { return m_managed_font->IsUnderlined(); }
  bool StrikeOut() const { return m_managed_font->IsStrikethrough(); }
  bool IsEngravingFont() const { return m_managed_font->IsEngravingFont(); }
  bool IsSymbolFont() const { return m_managed_font->IsSymbolFont(); }
  bool IsSingleStrokeFont() const { return m_managed_font->IsSingleStrokeFont(); }
  bool IsSimulated() const { return m_managed_font->IsSimulated(); }
  //public FontStyle Style = > UnsafeNativeMethods.ON_Font_Style(m_managed_font);
  //public FontWeight Weight = > UnsafeNativeMethods.ON_Font_Weight(m_managed_font);
  double PointSize() const { return m_managed_font->PointSize(); }
  std::wstring FamilyName() const { return std::wstring(m_managed_font->FamilyName()); }
  //public string EnglishFamilyName | get;
};
