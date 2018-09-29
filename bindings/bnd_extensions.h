#include "bindings.h"

#pragma once

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

  std::string GetStartSectionComments() const;
  void SetStartSectionComments(const char* comments);

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
