#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtensionsBindings(pybind11::module& m);
#else
void initExtensionsBindings();
#endif


class BND_ONXModel_ObjectTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel_ObjectTable(std::shared_ptr<ONX_Model> m);
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
  bool Write(const char* path, int version);

  std::wstring GetStartSectionComments() const;
  void SetStartSectionComments(std::wstring comments);

  std::string GetApplicationName() const;
  void SetApplicationName(const char* comments);
  std::string GetApplicationUrl() const;
  void SetApplicationUrl(const char* comments);
  std::string GetApplicationDetails() const;
  void SetApplicationDetails(const char* comments);
  std::string GetCreatedBy() const;
  std::string GetLastEditedBy() const;
  int GetRevision() const;
  void SetRevision(int r);

  BND_ONXModel_ObjectTable Objects();
};
