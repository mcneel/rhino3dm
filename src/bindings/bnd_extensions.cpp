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


BND_UUID BND_ONXModel_ObjectTable::AddPoint1(double x, double y, double z)
{
  ON_Point point_geometry(x,y,z);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &point_geometry, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddLine1(const ON_3dPoint& from, const ON_3dPoint& to)
{
  ON_LineCurve lc(from, to);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &lc, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddCircle1(const BND_Circle& circle)
{
  ON_NurbsCurve nc;
  if (circle.ToONCircle().GetNurbForm(nc) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &nc, nullptr);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}
BND_UUID BND_ONXModel_ObjectTable::AddSphere1(const BND_Sphere& sphere)
{
  ON_NurbsSurface ns;
  if (sphere.GetNurbForm(ns) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &ns, nullptr);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}

BND_UUID BND_ONXModel_ObjectTable::AddCurve1(const BND_Curve* curve)
{
  const ON_Geometry* g = curve ? curve->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddTextDot1(std::wstring text, const ON_3dPoint& location)
{
  ON_TextDot dot(location, text.c_str(), nullptr);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &dot, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddMesh1(const BND_Mesh* mesh)
{
  const ON_Geometry* g = mesh ? mesh->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddBrep1(const BND_Brep* brep)
{
  const ON_Geometry* g = brep ? brep->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, nullptr);
  return ON_UUID_to_Binding(rc);
}

int BND_ONXModel_ObjectTable::Count() const
{
  ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
  iterator.FirstComponentReference();
  return iterator.ActiveComponentCount();

}

BND_FileObject* BND_ONXModel_ObjectTable::ModelObjectAt(int index)
{
  BND_GeometryBase* geometry = dynamic_cast<BND_GeometryBase*>(ObjectAt(index));
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


BND_CommonObject* BND_ONXModel_ObjectTable::ObjectAt(int index)
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
  return BND_CommonObject::CreateWrapper(compref);
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

void BND_File3dmLayerTable::Add(const BND_Layer& layer)
{
  const ON_Layer* l = layer.m_layer;
  int index = m_model->AddLayer(l->NameAsPointer(), l->Color());
  ON_ModelComponentReference compref = m_model->LayerFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
  {
    *modellayer = *l;
    modellayer->SetIndex(index);
  }
}

BND_Layer* BND_File3dmLayerTable::FindName(std::wstring name, BND_UUID parentId)
{
  ON_UUID id = Binding_to_ON_UUID(parentId);
  ON_ModelComponentReference compref  = m_model->LayerFromName(id, name.c_str());
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
    return new BND_Layer(modellayer, &compref);
  return nullptr;
}

BND_Layer* BND_File3dmLayerTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->LayerFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
    return new BND_Layer(modellayer, &compref);
  return nullptr;
}

BND_Layer* BND_File3dmLayerTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->LayerFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
    return new BND_Layer(modellayer, &compref);
  return nullptr;
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

bool BND_ONXModel::ReadTest(std::wstring path)
{
  ONX_ModelTest modeltest;
  bool rc = modeltest.ReadTest(path.c_str(), ONX_ModelTest::Type::Read, false, nullptr, nullptr);
  return rc;
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
    .def("__len__", &BND_ONXModel_ObjectTable::Count)
    .def("__getitem__", &BND_ONXModel_ObjectTable::ModelObjectAt)
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint1)
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint2)
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint4)
    .def("AddLine", &BND_ONXModel_ObjectTable::AddLine1)
    .def("AddCircle", &BND_ONXModel_ObjectTable::AddCircle1)
    .def("AddSphere", &BND_ONXModel_ObjectTable::AddSphere1)
    .def("AddCurve", &BND_ONXModel_ObjectTable::AddCurve1)
    .def("AddTextDot", &BND_ONXModel_ObjectTable::AddTextDot1)
    .def("AddMesh", &BND_ONXModel_ObjectTable::AddMesh1)
    .def("AddBrep", &BND_ONXModel_ObjectTable::AddBrep1)
    .def("GetBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    ;

  py::class_<BND_File3dmLayerTable>(m, "File3dmLayerTable")
    .def("__len__", &BND_File3dmLayerTable::Count)
    .def("__getitem__", &BND_File3dmLayerTable::FindIndex)
    .def("Add", &BND_File3dmLayerTable::Add)
    .def("FindName", &BND_File3dmLayerTable::FindName)
    .def("FindIndex", &BND_File3dmLayerTable::FindIndex)
    .def("FindId", &BND_File3dmLayerTable::FindId)
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
    .def_property_readonly("Layers", &BND_ONXModel::Layers)
    .def_static("_TestRead", &BND_ONXModel::ReadTest)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtensionsBindings(void*)
{
  class_<BND_FileObject>("File3dmObject")
    .function("attributes", &BND_FileObject::GetAttributes, allow_raw_pointers())
    .function("geometry", &BND_FileObject::GetGeometry, allow_raw_pointers())
    ;

  class_<BND_ONXModel_ObjectTable>("File3dmObjectTable")
    .property("count", &BND_ONXModel_ObjectTable::Count)
    .function("get", &BND_ONXModel_ObjectTable::ModelObjectAt, allow_raw_pointers())
    .function("getBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    .function("addPoint", &BND_ONXModel_ObjectTable::AddPoint1)
    .function("addLine", &BND_ONXModel_ObjectTable::AddLine1)
    .function("addCircle", &BND_ONXModel_ObjectTable::AddCircle1)
    .function("addSphere", &BND_ONXModel_ObjectTable::AddSphere1)
    .function("addCurve", &BND_ONXModel_ObjectTable::AddCurve1, allow_raw_pointers())
    .function("addTextDot", &BND_ONXModel_ObjectTable::AddTextDot1)
    .function("addMesh", &BND_ONXModel_ObjectTable::AddMesh1, allow_raw_pointers())
    .function("addBrep", &BND_ONXModel_ObjectTable::AddBrep1, allow_raw_pointers())
    ;

  class_<BND_File3dmLayerTable>("File3dmLayerTable")
    .function("count", &BND_File3dmLayerTable::Count)
    .function("get", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmLayerTable::Add)
    .function("findName", &BND_File3dmLayerTable::FindName, allow_raw_pointers())
    .function("findIndex", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmLayerTable::FindId, allow_raw_pointers())
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
    .function("layers", &BND_ONXModel::Layers)
    ;
}
#endif
