
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initEmbeddedFileBindings(pybind11::module& m);
#else
void initEmbeddedFileBindings(void* m);
#endif

class BND_File3dmEmbeddedFile : public BND_ModelComponent
{
public:
  ON_EmbeddedFile* m_embedded_file = nullptr;

protected:
  void SetTrackedPointer(ON_EmbeddedFile* ef, const ON_ModelComponentReference* compref);

public:
  BND_File3dmEmbeddedFile();
  BND_File3dmEmbeddedFile(const BND_File3dmEmbeddedFile& other);
  BND_File3dmEmbeddedFile(ON_EmbeddedFile* ef, const ON_ModelComponentReference* compref);

  static BND_File3dmEmbeddedFile* Read(const std::wstring& f);

  std::wstring GetFilename(void) const { return std::wstring(static_cast<const wchar_t*>(m_embedded_file->Filename())); }
  size_t GetLength(void) const { return m_embedded_file->Length(); }
  bool Write(const std::wstring& f) const { return m_embedded_file->SaveToFile(f.c_str()); }
  bool Clear(void) const { return m_embedded_file->Clear(); }
};

class BND_File3dmEmbeddedFileTable
{
private:
  std::shared_ptr<ONX_Model> m_model;

public:
  BND_File3dmEmbeddedFileTable(std::shared_ptr<ONX_Model> m) { m_model = m; }

  int Count() const { return m_model.get()->ActiveComponentCount(ON_ModelComponent::Type::EmbeddedFile); }
  void Add(const BND_File3dmEmbeddedFile& ef);
  BND_File3dmEmbeddedFile* FindIndex(int index);
  BND_File3dmEmbeddedFile* IterIndex(int index); // helper function for iterator
  BND_File3dmEmbeddedFile* FindId(BND_UUID id);
};
