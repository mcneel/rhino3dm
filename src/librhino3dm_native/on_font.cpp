#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_font.h")

RH_C_FUNCTION const ON_Font* ON_Font_GetManagedFont(const RHMONO_STRING* facename, ON_Font::Weight weight, ON_Font::Style style, ON_Font::Stretch stretch, bool underlined, bool strikeout)
{
  INPUTSTRINGCOERCE(_facename, facename);
  return ON_Font::GetManagedFont(
    _facename, 
    weight, 
    style,
    stretch, 
    underlined, 
    strikeout, 
    ON_FontMetrics::DefaultLineFeedRatio,
    ON_Font::Default.LogfontCharSet()
  );
}


// Font.FamilyName ->         ON_Font_FamilyName ->         ON_Font::FamilyName
// Font.LogfontName ->        ON_Font_WindowsLogfontName -> ON_Font::WindowsLogfontName
// Font.FaceName ->           ON_Font_FaceName ->           ON_Font::FaceName
// Font.PostScriptName ->     ON_Font_PostScriptName ->     ON_Font::PostScriptName
// Font.RichTextFontName ->   ON_Font_RichTextFontName ->   ON_Font::RichTextFontName
// Font.Description ->        ON_Font_Description ->        ON_Font::Description
// Font.IsSymbolFont ->       ON_Font_IsSymbolFont ->       ON_Font::IsSymbolFont
// Font.IsSingleStrokeFont -> ON_Font_IsSingleStrokeFont -> ON_Font::IsSingleStrokeFont
// Font.IsSimulated ->        ON_Font_IsSimulated ->        ON_Font::IsSimulated

RH_C_FUNCTION void ON_Font_FamilyName(const ON_Font* constFont, ON_wString* pString, bool localize)
{
  if (constFont && pString)
    *pString = constFont->FamilyName(localize ? ON_Font::NameLocale::LocalizedFirst : ON_Font::NameLocale::English);
}

RH_C_FUNCTION void ON_Font_FaceName(const ON_Font* constFont, ON_wString* pString, bool localize)
{
  if (constFont && pString)
    //  *pString = constFont->WindowsLogfontName();
    *pString = constFont->FaceName(localize ? ON_Font::NameLocale::LocalizedFirst : ON_Font::NameLocale::English);
}

RH_C_FUNCTION void ON_Font_WindowsLogfontName(const ON_Font* constFont, ON_wString* pString)
{
  if (constFont && pString)
    *pString = constFont->WindowsLogfontName();
}

RH_C_FUNCTION void ON_Font_PostScriptName(const ON_Font* constFont, ON_wString* pString)
{
  if (constFont && pString)
    *pString = constFont->PostScriptName();
}

RH_C_FUNCTION void ON_Font_RichTextFontName(const ON_Font* constFont, ON_wString* pString)
{
  if (constFont && pString)
    *pString = ON_Font::RichTextFontName(constFont, true);
}

RH_C_FUNCTION void ON_Font_Description(const ON_Font* constFont, ON_wString* pString)
{
  if (constFont && pString)
    *pString = constFont->Description();
}

RH_C_FUNCTION bool ON_Font_IsEngravingFont(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsEngravingFont();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsSymbolFont(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsSymbolFont();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsSingleStrokeFont(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsSingleStrokeFont();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsSimulated(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsSimulated();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsUnderlined(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsUnderlined();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsStrikeout(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsStrikethrough();
  return false;
}

RH_C_FUNCTION ON_Font::Style ON_Font_Style(const ON_Font* constFont)
{
  if (constFont)
    return constFont->FontStyle();
  return ON_Font::Style::Unset;
}

RH_C_FUNCTION ON_Font::Stretch ON_Font_Stretch(const ON_Font* constFont)
{
  if (constFont)
    return constFont->FontStretch();
  return ON_Font::Stretch::Unset;
}

RH_C_FUNCTION ON_Font::Weight ON_Font_Weight(const ON_Font* constFont)
{
  if (constFont)
    return constFont->FontWeight();
  return ON_Font::Weight::Unset;
}

RH_C_FUNCTION bool ON_Font_IsBold(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsBoldInQuartet();
  return false;
}

RH_C_FUNCTION bool ON_Font_IsItalic(const ON_Font* constFont)
{
  if (constFont)
    return constFont->IsItalicInQuartet();
  return false;
}

RH_C_FUNCTION double ON_Font_PointSize(const ON_Font* constFont)
{
  if (constFont)
    return constFont->PointSize();

  return 10.0;
}

RH_C_FUNCTION int ON_Font_GetFontNames(ON_ClassArray<ON_wString>* pStrings)
{
    // This function is flawed 
  int rc = 0;
  if (pStrings)
  {
    ON_SimpleArray<const ON_Font*> fonts;
    ON_Font::GetInstalledFontList(fonts);
    rc = fonts.Count();
    pStrings->Reserve(rc);
    for (int i = 0; i < rc; i++)
    {
      const ON_Font* font = fonts[i];
      if (font)
      {
        ON_wString& str = pStrings->AppendNew();
        // There is no string that uniquely idendifies an insalled font on Windows 10
        // Under NO circumstances use WindowsLogfontNamee() - it is the worst possible choice.
        // PostScriptName() is not unique (Bahnxchrift has 12 or so faces witht eh same PostScriptName()
        // CounteryBlueprint has 4, ...
        //
        str = font->WindowsLogfontName();
//        str = font->FamilyName() + L" " + font->FaceName();
      }
    }
    rc = pStrings->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_Font_GetFamilyPlusFaceNames(ON_ClassArray<ON_wString>* pStrings, const RHMONO_STRING* _sep) {
  INPUTSTRINGCOERCE(sep, _sep);
  int rc = 0;
  if (pStrings)
  {
    ON_SimpleArray<const ON_Font*> fonts;
    ON_Font::GetInstalledFontList(fonts);
    rc = fonts.Count();
    pStrings->Reserve(rc);
    for (int i = 0; i < rc; i++)
    {
      const ON_Font* font = fonts[i];
      if (font)
      {
        ON_wString& str = pStrings->AppendNew();
        // There is no string that uniquely idendifies an insalled font on Windows 10
        // Under NO circumstances use WindowsLogfontNamee() - it is the worst possible choice.
        // PostScriptName() is not unique (Bahnxchrift has 12 or so faces witht eh same PostScriptName()
        // CounteryBlueprint has 4, ...
        //
        //str = font->WindowsLogfontName();
        str = font->FamilyName(ON_Font::NameLocale::English) + sep + font->FaceName(ON_Font::NameLocale::English)
          + sep + font->FamilyName(ON_Font::NameLocale::LocalizedFirst) + sep + font->FaceName(ON_Font::NameLocale::LocalizedFirst);
      }
    }
    rc = pStrings->Count();
  }
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<const ON_Font*>* ON_Font_GetInstalledFontList(const RHMONO_STRING* familyName)
{
  ON_SimpleArray<const ON_Font*>* fonts = new ON_SimpleArray<const ON_Font*>();
  if (familyName)
  {
    INPUTSTRINGCOERCE(_familyName, familyName);
    ON_Font::GetInstalledFontFamily(_familyName, *fonts);
  }
  else
    ON_Font::GetInstalledFontList(*fonts);

  return fonts;
}

RH_C_FUNCTION void ON_SimpleArray_ON_Font_Delete(ON_SimpleArray<const ON_Font*>* fontArray)
{
  if (fontArray)
    delete fontArray;
}

RH_C_FUNCTION int ON_SimpleArray_ON_Font_Count(const ON_SimpleArray<const ON_Font*>* fontArray)
{
  return fontArray ? fontArray->Count() : 0;
}

RH_C_FUNCTION const ON_Font* ON_SimpleArray_ON_Font_At(const ON_SimpleArray<const ON_Font*>* fontArray, int index)
{
  const ON_Font* rc = nullptr;
  if (fontArray && index >= 0 && index < fontArray->Count())
  {
    rc = (*fontArray)[index];
    if (rc)
      rc = rc->ManagedFont();
  }
  return rc;
}

RH_C_FUNCTION int ON_FontList_QuartetCount()
{
  return ON_Font::InstalledFontList().QuartetList().Count();
}

RH_C_FUNCTION void ON_FontList_GetQuartet(int i, ON_wString* name,
  bool* hasRegularFace, bool* hasBoldFace, bool* hasItalicFace, bool* hasBoldItalicFace)
{
  if (name && hasRegularFace && hasBoldFace && hasItalicFace && hasBoldItalicFace && i>=0)
  {
    const ON_FontList& fontList = ON_Font::InstalledFontList();

    const ON_ClassArray<ON_FontFaceQuartet>& list = fontList.QuartetList();
    if (i < list.Count())
    {
      const ON_FontFaceQuartet& quartet = list[i];
      *name = quartet.QuartetName();
      *hasRegularFace = quartet.HasRegularFace();
      *hasBoldFace = quartet.HasBoldFace();
      *hasItalicFace = quartet.HasItalicFace();
      *hasBoldItalicFace = quartet.HasBoldItalicFace();
    }
  }
}

RH_C_FUNCTION void ON_Font_QuartetName(const ON_Font* font, ON_wString* name)
{
  if (font && name)
    *name = font->QuartetName();
}

RH_C_FUNCTION void ON_Font_EnglishQuartetName(const ON_Font* font, ON_wString* name)
{
  if (font && name)
    *name = font->QuartetName(ON_Font::NameLocale::English);
}

RH_C_FUNCTION const ON_Font* ON_Font_FromQuartetProperties(const RHMONO_STRING* quartetName, bool bold, bool italic)
{
  const ON_Font* rc = nullptr;
  if (quartetName)
  {
    const ON_FontList& fontList = ON_Font::InstalledFontList();
    INPUTSTRINGCOERCE(_quartetName, quartetName);
    rc = fontList.FontFromQuartetProperties(_quartetName, bold, italic);
  }
  if (rc)
    return rc->ManagedFont();
  return nullptr;
}

RH_C_FUNCTION const ON_Font* ON_Font_FromBuffer(int archive_3dm_version, unsigned int archive_on_version, int length, /*ARRAY*/const unsigned char* buffer)
{
  // Eliminate potential bogus file versions written
  if (archive_3dm_version > 5 && archive_3dm_version < 50)
    return nullptr;
  
  const ON_Font* rc = nullptr;
  if (length > 0 && buffer)
  {
    ON_Read3dmBufferArchive archive(length, buffer, false, archive_3dm_version, archive_on_version);
    ON_Font font_characteristics;
    if (font_characteristics.Read(archive))
      rc = font_characteristics.ManagedFont();
  }
  return rc;
}

RH_C_FUNCTION ON_Write3dmBufferArchive* ON_Font_WriteBufferArchive_NewWriter(const ON_Font* pConstObject, int* rhinoversion, unsigned int* length)
{
  ON_Write3dmBufferArchive* rc = nullptr;

  if (pConstObject && length && nullptr != rhinoversion)
  {
    *length = 0;
    size_t sz = 2 * sizeof(ON_Font);

    // 23 Oct 2019 - If .NET user passes a huge value, just clamp it and continue.
    const int current_3dm_version = ON_BinaryArchive::CurrentArchiveVersion();
    if (*rhinoversion > current_3dm_version)
      *rhinoversion = current_3dm_version;

    if (*rhinoversion < 60)
    {
      // ON_Font objects require at least a version 6 3dm archive.
      *rhinoversion = 60;
    }

    // figure out the appropriate version number
    unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(*rhinoversion, ON::Version());

    rc = new ON_Write3dmBufferArchive(sz, 0, *rhinoversion, on_version__to_write);
    if (pConstObject->Write(*rc))
    {
      *length = (unsigned int)rc->SizeOfArchive();
    }
    else
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Font_IsInstalledFont(const ON_Font* font)
{
  return (font && font->IsInstalledFont());
}

RH_C_FUNCTION const ON_Font* ON_Font_GetSubstituteFont(const ON_Font* font)
{
  if (font)
    return font->SubstituteFont();
  return nullptr;
}
