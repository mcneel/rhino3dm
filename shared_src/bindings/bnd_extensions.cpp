#include "bindings.h"

BND_ONXModel::BND_ONXModel()
{
  m_model.reset(new ONX_Model());
}

BND_ONXModel::BND_ONXModel(ONX_Model* m)
{
  m_model.reset(m);
}

BND_ONXModel* BND_ONXModel::Read(const char* path)
{
  ONX_Model* m = new ONX_Model();
  if (!m->Read(path))
  {
    delete m;
    return nullptr;
  }
  return new BND_ONXModel(m);
}

std::string BND_ONXModel::ReadNotes(const char* path)
{
  std::string str;
  FILE* fp = ON::OpenFile(path, "rb");
  if (fp)
  {
    ON_BinaryFile file(ON::archive_mode::read3dm, fp);
    int version = 0;
    ON_String comments;
    bool rc = file.Read3dmStartSection(&version, comments);
    if (rc)
    {
      ON_3dmProperties prop;
      file.Read3dmProperties(prop);
      if (prop.m_Notes.IsValid())
      {
        ON_String s = prop.m_Notes.m_notes;
        str = s;
      }
    }
    ON::CloseFile(fp);
  }
  return str;
}

int BND_ONXModel::ReadArchiveVersion(const char* path)
{
  FILE* fp = ON::OpenFile(path, "rb");
  if (fp)
  {
    ON_BinaryFile file(ON::archive_mode::read3dm, fp);
    int version = 0;
    ON_String comment_block;
    bool rc = file.Read3dmStartSection(&version, comment_block);
    if (rc)
    {
      ON::CloseFile(fp);
      return version;
    }
    ON::CloseFile(fp);
  }
  return 0;
}

bool BND_ONXModel::Write(const char* path, int version)
{
  return m_model->Write(path, version);
}

std::wstring BND_ONXModel::GetStartSectionComments() const
{
  ON_wString comments = m_model->m_sStartSectionComments;
  return std::wstring(comments);
}
void BND_ONXModel::SetStartSectionComments(std::wstring comments)
{
  ON_wString wcomments = comments.c_str();
  m_model->m_sStartSectionComments = wcomments;
}

const int idxApplicationName = 0;
const int idxApplicationUrl = 1;
const int idxApplicationDetails = 2;
const int idxCreatedBy = 3;
const int idxLastCreatedBy = 4;

RH_C_FUNCTION void ONX_Model_GetString(const ONX_Model* pConstModel, int which, ON_String* pString)
{
  if (pConstModel && pString)
  {
    switch (which)
    {
    case idxApplicationName:
      *pString = (pConstModel->m_properties.m_Application.m_application_name);
      break;
    case idxApplicationUrl:
      *pString = (pConstModel->m_properties.m_Application.m_application_URL);
      break;
    case idxApplicationDetails:
      *pString = (pConstModel->m_properties.m_Application.m_application_details);
      break;
    case idxCreatedBy:
      *pString = (pConstModel->m_properties.m_RevisionHistory.m_sCreatedBy);
      break;
    case idxLastCreatedBy:
      *pString = (pConstModel->m_properties.m_RevisionHistory.m_sLastEditedBy);
      break;
    }
  }
}

RH_C_FUNCTION void ONX_Model_SetString(ONX_Model* pModel, int which, const char* str)
{
  ON_wString _str = str;

  if (pModel)
  {
    switch (which)
    {
    case idxApplicationName:
      pModel->m_properties.m_Application.m_application_name = _str;
      break;
    case idxApplicationUrl:
      pModel->m_properties.m_Application.m_application_URL = _str;
      break;
    case idxApplicationDetails:
      pModel->m_properties.m_Application.m_application_details = _str;
      break;
    case idxCreatedBy:
      pModel->m_properties.m_RevisionHistory.m_sCreatedBy = _str;
      break;
    case idxLastCreatedBy:
      pModel->m_properties.m_RevisionHistory.m_sLastEditedBy = _str;
      break;
    }
  }
}

std::string BND_ONXModel::GetApplicationName() const
{
  ON_String s;
  ONX_Model_GetString(m_model.get(), idxApplicationName, &s);
  return std::string(s);
}
void BND_ONXModel::SetApplicationName(const char* comments)
{
  ONX_Model_SetString(m_model.get(), idxApplicationName, comments);
}
std::string BND_ONXModel::GetApplicationUrl() const
{
  ON_String s;
  ONX_Model_GetString(m_model.get(), idxApplicationUrl, &s);
  return std::string(s);
}
void BND_ONXModel::SetApplicationUrl(const char* s)
{
  ONX_Model_SetString(m_model.get(), idxApplicationUrl, s);
}
std::string BND_ONXModel::GetApplicationDetails() const
{
  ON_String s;
  ONX_Model_GetString(m_model.get(), idxApplicationDetails, &s);
  return std::string(s);
}
void BND_ONXModel::SetApplicationDetails(const char* s)
{
  ONX_Model_SetString(m_model.get(), idxApplicationDetails, s);
}
std::string BND_ONXModel::GetCreatedBy() const
{
  ON_String s;
  ONX_Model_GetString(m_model.get(), idxCreatedBy, &s);
  return std::string(s);
}
std::string BND_ONXModel::GetLastEditedBy() const
{
  ON_String s;
  ONX_Model_GetString(m_model.get(), idxLastCreatedBy, &s);
  return std::string(s);
}

RH_C_FUNCTION int ONX_Model_GetRevision(const ONX_Model* pConstModel)
{
  int rc = 0;
  if (pConstModel)
    rc = pConstModel->m_properties.m_RevisionHistory.m_revision_count;
  return rc;
}

RH_C_FUNCTION void ONX_Model_SetRevision(ONX_Model* pModel, int rev)
{
  if (pModel)
    pModel->m_properties.m_RevisionHistory.m_revision_count = rev;
}

int BND_ONXModel::GetRevision() const
{
  return ONX_Model_GetRevision(m_model.get());
}
void BND_ONXModel::SetRevision(int r)
{
  ONX_Model_SetRevision(m_model.get(), r);
}

BND_ONXModel_ObjectTable BND_ONXModel::Objects()
{
  return BND_ONXModel_ObjectTable(m_model);
}

BND_ONXModel_ObjectTable::BND_ONXModel_ObjectTable(std::shared_ptr<ONX_Model> m)
{
  m_model = m;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initExtensionsBindings(pybind11::module& m)
{
  py::class_<BND_ONXModel>(m, "File3dm")
    .def(py::init<>())
    .def_static("Read", &BND_ONXModel::Read)
    .def_static("ReadNotes", &BND_ONXModel::ReadNotes)
    .def_static("ReadArchiveVersion", &BND_ONXModel::ReadArchiveVersion)
    .def("Write", &BND_ONXModel::Write)
    .def_property("StartSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments)
    .def_property("ApplicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    .def_property("ApplicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    .def_property("ApplicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    .def_property_readonly("CreatedBy", &BND_ONXModel::GetCreatedBy)
    .def_property_readonly("LastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .def_property("Revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .def_property_readonly("Objects", &BND_ONXModel::Objects)
    ;

  py::class_<BND_ONXModel_ObjectTable>(m, "File3dmObjectTable");
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtensionsBindings()
{
  class_<BND_ONXModel>("File3dm")
    .constructor<>()
    .property("startSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments);
    //.property("applicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    //.property("applicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    //.property("applicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    //.property("areatedBy", &BND_ONXModel::GetCreatedBy)
    //.property("lastEditedBy", &BND_ONXModel::GetLastEditedBy)
    //.property("revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    //.property("objects", &BND_ONXModel::Objects)
    //;
}
#endif
