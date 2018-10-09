#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtensionsBindings(pybind11::module& m);
#else
void initExtensionsBindings();
#endif

class BND_FileObject
{
public:
  BND_Geometry* m_geometry = nullptr;
  BND_3dmAttributes* m_attributes = nullptr;

  BND_Geometry* GetGeometry() { return m_geometry; };
  BND_3dmAttributes* GetAttributes() { return m_attributes; }
};

class BND_ONXModel_ObjectTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel_ObjectTable(std::shared_ptr<ONX_Model> m);
  ON_UUID AddPoint(double x, double y, double z);

  int Count() const;
  BND_FileObject* ModelObjectAt(int index);
  BND_Object* ObjectAt(int index);
  BND_3dmAttributes* AttributesAt(int index);
  BND_BoundingBox GetBoundingBox() const;
};

class BND_ONXModel
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel();
  BND_ONXModel(ONX_Model* m);
  static BND_ONXModel* Read(const char* path);
  static std::string ReadNotes(const char* path);
  static int ReadArchiveVersion(const char* path);

  #if defined(ON_WASM_COMPILE)
  // from https://sean.voisen.org/blog/2018/03/rendering-images-emscripten-wasm/
  static BND_ONXModel* WasmFromByteArray(std::string buffer);
  #endif

  static BND_ONXModel* FromByteArray(int length, const void* buffer);

  bool Write(const char* path, int version);

  std::wstring GetStartSectionComments() const;
  void SetStartSectionComments(std::wstring comments);

  std::wstring GetApplicationName() const;
  void SetApplicationName(std::wstring name);
  std::wstring GetApplicationUrl() const;
  void SetApplicationUrl(std::wstring url);
  std::wstring GetApplicationDetails() const;
  void SetApplicationDetails(std::wstring details);
  std::wstring GetCreatedBy() const;
  std::wstring GetLastEditedBy() const;
  int GetRevision() const;
  void SetRevision(int r);

  BND_ONXModel_ObjectTable Objects();
};
