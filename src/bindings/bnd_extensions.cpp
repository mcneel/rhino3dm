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

RH_C_FUNCTION void ONX_Model_GetString(const ONX_Model* pConstModel, int which, ON_wString* pString)
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

RH_C_FUNCTION void ONX_Model_SetString(ONX_Model* pModel, int which, const wchar_t* str)
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

std::wstring BND_ONXModel::GetApplicationName() const
{
  ON_wString s;
  ONX_Model_GetString(m_model.get(), idxApplicationName, &s);
  return std::wstring(s);
}
void BND_ONXModel::SetApplicationName(std::wstring comments)
{
  ONX_Model_SetString(m_model.get(), idxApplicationName, comments.c_str());
}
std::wstring BND_ONXModel::GetApplicationUrl() const
{
  ON_wString s;
  ONX_Model_GetString(m_model.get(), idxApplicationUrl, &s);
  return std::wstring(s);
}
void BND_ONXModel::SetApplicationUrl(std::wstring s)
{
  ONX_Model_SetString(m_model.get(), idxApplicationUrl, s.c_str());
}
std::wstring BND_ONXModel::GetApplicationDetails() const
{
  ON_wString s;
  ONX_Model_GetString(m_model.get(), idxApplicationDetails, &s);
  return std::wstring(s);
}
void BND_ONXModel::SetApplicationDetails(std::wstring s)
{
  ONX_Model_SetString(m_model.get(), idxApplicationDetails, s.c_str());
}
std::wstring BND_ONXModel::GetCreatedBy() const
{
  ON_wString s;
  ONX_Model_GetString(m_model.get(), idxCreatedBy, &s);
  return std::wstring(s);
}
std::wstring BND_ONXModel::GetLastEditedBy() const
{
  ON_wString s;
  ONX_Model_GetString(m_model.get(), idxLastCreatedBy, &s);
  return std::wstring(s);
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

static ON_UUID Internal_ONX_Model_AddModelGeometry(
  ONX_Model* model,
  const ON_Geometry* geometry,
  const ON_3dmObjectAttributes* attributes
)
{
  if (nullptr == model)
    return ON_nil_uuid;
  if (nullptr == geometry)
    return ON_nil_uuid;
  ON_ModelComponentReference model_component_reference = model->AddModelGeometryComponent(geometry, attributes);
  return ON_ModelGeometryComponent::FromModelComponentRef(model_component_reference, &ON_ModelGeometryComponent::Unset)->Id();
}


ON_UUID BND_ONXModel_ObjectTable::AddPoint(double x, double y, double z)
{
  ON_Point point_geometry(x,y,z);
  return Internal_ONX_Model_AddModelGeometry(m_model.get(), &point_geometry, nullptr);
}


int BND_ONXModel_ObjectTable::Count() const
{
  ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
  iterator.FirstComponentReference();
  return iterator.ActiveComponentCount();

}

BND_FileObject* BND_ONXModel_ObjectTable::ModelObjectAt(int index)
{
  BND_Geometry* geometry = dynamic_cast<BND_Geometry*>(ObjectAt(index));
  if (nullptr == geometry)
    return nullptr;
  BND_3dmAttributes* attrs = AttributesAt(index);
  if (nullptr == attrs)
  {
    delete geometry;
    return nullptr;
  }
  BND_FileObject* rc = new BND_FileObject();
  rc->m_attributes = attrs;
  rc->m_geometry = geometry;
  return rc;
}


BND_Object* BND_ONXModel_ObjectTable::ObjectAt(int index)
{
  // I know this is dumb. I haven't figured out how to set up enumeration in
  // javascript yet, so this is just here to keep things moving along
  ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
  ON_ModelComponentReference compref = iterator.FirstComponentReference();
  int current = 0;
  while(current<index)
  {
    compref = iterator.NextComponentReference();
    current++;
  }
  return BND_Object::CreateWrapper(compref);
}

BND_3dmAttributes* BND_ONXModel_ObjectTable::AttributesAt(int index)
{
  // I know this is dumb. I haven't figured out how to set up enumeration in
  // javascript yet, so this is just here to keep things moving along
  ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
  ON_ModelComponentReference compref = iterator.FirstComponentReference();
  int current = 0;
  while (current < index)
  {
    compref = iterator.NextComponentReference();
    current++;
  }

  const ON_ModelComponent* model_component = compref.ModelComponent();
  const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
  if (nullptr == geometryComponent)
    return nullptr;

  ON_3dmObjectAttributes* attrs = const_cast<ON_3dmObjectAttributes*>(geometryComponent->Attributes(nullptr));
  if (nullptr == attrs)
    return nullptr;
  return new BND_3dmAttributes(attrs, &compref);
}

BND_BoundingBox BND_ONXModel_ObjectTable::GetBoundingBox() const
{
  return BND_BoundingBox(m_model->ModelGeometryBoundingBox());
}


#if defined(ON_WASM_COMPILE)
BND_ONXModel* BND_ONXModel::WasmFromByteArray(std::string sbuffer)
{
/*
old code used for debugging
const void* buffer = sbuffer.c_str();
ON_Read3dmBufferArchive archive(length, buffer, true, 0, 0);
ON_ErrorLog errorlog;
errorlog.EnableLogging();

ONX_Model* model = new ONX_Model();
ON_wString log;
ON_TextLog textlog(log);
if(!model->Read(archive)) {
  delete model;
  errorlog.Dump(textlog);
  return std::wstring(log);
}
return std::wstring(L"success");
*/

  int length = sbuffer.length();
  const void* buffer = sbuffer.c_str();
  return FromByteArray(length, buffer);
}
#endif
BND_ONXModel* BND_ONXModel::FromByteArray(int length, const void* buffer)
{
  ON_Read3dmBufferArchive archive(length, buffer, true, 0, 0);

  ONX_Model* model = new ONX_Model();
  if (!model->Read(archive)) {
    delete model;
    return nullptr;
  }
  return new BND_ONXModel(model);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initExtensionsBindings(pybind11::module& m)
{
  py::class_<BND_FileObject>(m, "File3dmObject")
    .def_property_readonly("Attributes", &BND_FileObject::GetAttributes)
    .def_property_readonly("Geometry", &BND_FileObject::GetGeometry)
    ;

  py::class_<BND_ONXModel_ObjectTable>(m, "File3dmObjectTable")
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint)
    .def("__len__", &BND_ONXModel_ObjectTable::Count)
    .def("__getitem__", &BND_ONXModel_ObjectTable::ModelObjectAt)
    .def("GetBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    ;

  py::class_<BND_ONXModel>(m, "File3dm")
    .def(py::init<>())
    .def_static("Read", &BND_ONXModel::Read)
    .def_static("ReadNotes", &BND_ONXModel::ReadNotes)
    .def_static("ReadArchiveVersion", &BND_ONXModel::ReadArchiveVersion)
    .def_static("FromByteArray", [](py::buffer b) {
      py::buffer_info info = b.request();
      return BND_ONXModel::FromByteArray(static_cast<int>(info.size), info.ptr);
    })
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
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtensionsBindings()
{
  class_<BND_FileObject>("File3dmObject")
    .function("attributes", &BND_FileObject::GetAttributes, allow_raw_pointers())
    .function("geometry", &BND_FileObject::GetGeometry, allow_raw_pointers())
    ;

  class_<BND_ONXModel_ObjectTable>("File3dmObjectTable")
    .function("addPoint", &BND_ONXModel_ObjectTable::AddPoint)
    .property("count", &BND_ONXModel_ObjectTable::Count)
    .function("get", &BND_ONXModel_ObjectTable::ModelObjectAt, allow_raw_pointers())
    .function("getBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    ;

  class_<BND_ONXModel>("File3dm")
    .constructor<>()
    .class_function("fromByteArray", &BND_ONXModel::WasmFromByteArray, allow_raw_pointers())
    .property("startSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments)
    .property("applicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    .property("applicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    .property("applicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    .property("createdBy", &BND_ONXModel::GetCreatedBy)
    .property("lastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .property("revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .function("objects", &BND_ONXModel::Objects)
    ;
}
#endif
