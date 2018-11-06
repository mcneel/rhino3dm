#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initBitmapBindings(pybind11::module& m);
#else
void initBitmapBindings(void* m);
#endif

class BND_Bitmap : public BND_CommonObject
{
public:
  ON_Bitmap* m_bitmap = nullptr;
public:
  BND_Bitmap();
  BND_Bitmap(ON_Bitmap* bitmap, const ON_ModelComponentReference* compref);

  int Width() const { return m_bitmap->Width(); }
  int Height() const { return m_bitmap->Height(); }
  int BitsPerPixel() const { return m_bitmap->BitsPerPixel(); }
  size_t SizeOfScan() const { return m_bitmap->SizeofScan(); }
  size_t SizeOfImage() const { return m_bitmap->SizeofImage(); }
  //unsigned char* Bits(int scan_line_index);
  //const unsigned char* Bits(int scan_line_index) const;
  //const ON_FileReference& FileReference() const;
  //void SetFileReference(const ON_FileReference& file_reference);
  void SetFileFullPath(std::wstring path) { m_bitmap->SetFileFullPath(path.c_str(), true); }

protected:
  void SetTrackedPointer(ON_Bitmap* bitmap, const ON_ModelComponentReference* compref);
};