#include "bindings.h"
#include "base64.h"


BND_ONXModel::BND_ONXModel()
{
  m_model.reset(new ONX_Model());
}

BND_ONXModel::BND_ONXModel(ONX_Model* m)
{
  m_model.reset(m);
}

BND_ONXModel* BND_ONXModel::Read(std::wstring path)
{
  ONX_Model* m = new ONX_Model();
  if (!m->Read(path.c_str()))
  {
    delete m;
    return nullptr;
  }
  return new BND_ONXModel(m);
}

std::string BND_ONXModel::ReadNotes(std::wstring path)
{
  std::string str;
  FILE* fp = ON::OpenFile(path.c_str(), L"rb");
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

int BND_ONXModel::ReadArchiveVersion(std::wstring path)
{
  FILE* fp = ON::OpenFile(path.c_str(), L"rb");
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

bool BND_ONXModel::Write(std::wstring path, int version)
{
  return m_model->Write(path.c_str(), version);
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
  const BND_3dmObjectAttributes* attributes
)
{
  if (nullptr == model)
    return ON_nil_uuid;
  if (nullptr == geometry)
    return ON_nil_uuid;
  const ON_3dmObjectAttributes* attr = attributes ? attributes->m_attributes : nullptr;

  ON_ModelComponentReference model_component_reference = model->AddModelGeometryComponent(geometry, attr);
  return ON_ModelGeometryComponent::FromModelComponentRef(model_component_reference, &ON_ModelGeometryComponent::Unset)->Id();
}


BND_UUID BND_ONXModel_ObjectTable::AddPoint1(double x, double y, double z)
{
  ON_Point point_geometry(x,y,z);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &point_geometry, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddPointCloud(const BND_PointCloud& cloud, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = cloud.GeometryPointer();
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddLine1(const ON_3dPoint& from, const ON_3dPoint& to)
{
  ON_LineCurve lc(from, to);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &lc, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddPolyline(const BND_Point3dList& points, const BND_3dmObjectAttributes* attributes)
{
  ON_PolylineCurve plc(points.m_polyline);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &plc, nullptr);
  return ON_UUID_to_Binding(rc);
}

#if defined(ON_PYTHON_COMPILE)
BND_UUID BND_ONXModel_ObjectTable::AddPolyline2(pybind11::object points, const class BND_3dmObjectAttributes* attributes)
{
  BND_Point3dList list;
  for (auto item : points)
  {
    ON_3dPoint point = item.cast<ON_3dPoint>();
    list.Add(point.x, point.y, point.z);
  }
  return AddPolyline(list, attributes);
}
#endif

BND_UUID BND_ONXModel_ObjectTable::AddArc(const BND_Arc& arc, const BND_3dmObjectAttributes* attributes)
{
  ON_NurbsCurve nc;
  if (arc.m_arc.GetNurbForm(nc) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &nc, attributes);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}

BND_UUID BND_ONXModel_ObjectTable::AddCircle(const BND_Circle& circle, const BND_3dmObjectAttributes* attributes)
{
  ON_NurbsCurve nc;
  if (circle.m_circle.GetNurbForm(nc) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &nc, attributes);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}

BND_UUID BND_ONXModel_ObjectTable::AddEllipse(const BND_Ellipse& ellipse, const BND_3dmObjectAttributes* attributes)
{
  ON_NurbsCurve nc;
  if (ellipse.m_ellipse.GetNurbForm(nc) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &nc, nullptr);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}

BND_UUID BND_ONXModel_ObjectTable::AddSphere(const BND_Sphere& sphere, const BND_3dmObjectAttributes* attributes)
{
  ON_NurbsSurface ns;
  if (sphere.m_sphere.GetNurbForm(ns) != 0)
  {
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &ns, attributes);
    return ON_UUID_to_Binding(rc);
  }
  return ON_UUID_to_Binding(ON_nil_uuid);
}

BND_UUID BND_ONXModel_ObjectTable::AddCurve(const BND_Curve* curve, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = curve ? curve->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddTextDot(std::wstring text, const ON_3dPoint& location, const BND_3dmObjectAttributes* attributes)
{
  ON_TextDot dot(location, text.c_str(), nullptr);
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &dot, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddSurface(const BND_Surface* surface, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = surface ? surface->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddExtrusion(const BND_Extrusion* extrusion, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = extrusion ? extrusion->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddMesh(const BND_Mesh* mesh, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = mesh ? mesh->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddBrep(const BND_Brep* brep, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = brep ? brep->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddHatch(const BND_Hatch* hatch, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = hatch ? hatch->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::Add(const BND_GeometryBase* geometry, const BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = geometry ? geometry->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

void BND_ONXModel_ObjectTable::Delete(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  m_model->RemoveModelComponent(ON_ModelComponent::Type::ModelGeometry, _id);
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
  BND_3dmObjectAttributes* attrs = AttributesAt(index);
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

// helper function for iterator
BND_FileObject* BND_ONXModel_ObjectTable::IterIndex(int index)
{
  return ModelObjectAt(index);
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

BND_3dmObjectAttributes* BND_ONXModel_ObjectTable::AttributesAt(int index)
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
  return new BND_3dmObjectAttributes(attrs, &compref);
}

BND_BoundingBox BND_ONXModel_ObjectTable::GetBoundingBox() const
{
  return BND_BoundingBox(m_model->ModelGeometryBoundingBox());
}

void BND_File3dmMaterialTable::Add(const BND_Material& material)
{
  const ON_Material* m = material.m_material;
  m_model->AddModelComponent(*m);
}

BND_Material* BND_File3dmMaterialTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_Material* BND_File3dmMaterialTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->RenderMaterialFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Material* modelmaterial = const_cast<ON_Material*>(ON_Material::Cast(model_component));
  if (modelmaterial)
    return new BND_Material(modelmaterial, &compref);
  return nullptr;
}

BND_Material* BND_File3dmMaterialTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->RenderMaterialFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Material* modelmaterial = const_cast<ON_Material*>(ON_Material::Cast(model_component));
  if (modelmaterial)
    return new BND_Material(modelmaterial, &compref);
  return nullptr;
}

void BND_File3dmBitmapTable::Add(const BND_Bitmap& bitmap)
{
  const ON_Bitmap* b = bitmap.m_bitmap;
  m_model->AddModelComponent(*b);
}

BND_Bitmap* BND_File3dmBitmapTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->RenderMaterialFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Bitmap* modelbitmap = const_cast<ON_Bitmap*>(ON_Bitmap::Cast(model_component));
  if (modelbitmap)
    return new BND_Bitmap(modelbitmap, &compref);
  return nullptr;
}

BND_Bitmap* BND_File3dmBitmapTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_Bitmap* BND_File3dmBitmapTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->RenderMaterialFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Bitmap* modelbitmap = const_cast<ON_Bitmap*>(ON_Bitmap::Cast(model_component));
  if (modelbitmap)
    return new BND_Bitmap(modelbitmap, &compref);
  return nullptr;
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

BND_Layer* BND_File3dmLayerTable::IterIndex(int index)
{
  return FindIndex(index);
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

int BND_File3dmViewTable::Count() const
{
  return m_named_views ? m_model->m_settings.m_named_views.Count() : m_model->m_settings.m_views.Count();
}

void BND_File3dmViewTable::Add(const BND_ViewInfo& view)
{
  if (m_named_views)
    m_model->m_settings.m_named_views.Append(view.m_view);
  else
    m_model->m_settings.m_views.Append(view.m_view);
}

BND_ViewInfo* BND_File3dmViewTable::GetItem(int index) const
{
  int count = m_named_views ? m_model->m_settings.m_named_views.Count()
    : m_model->m_settings.m_views.Count();
  if (index < 0 || index >= count)
    return nullptr;
  BND_ViewInfo* rc = new BND_ViewInfo();
  if (m_named_views)
    rc->m_view = m_model->m_settings.m_named_views[index];
  else
    rc->m_view = m_model->m_settings.m_views[index];
  return rc;
}

BND_ViewInfo* BND_File3dmViewTable::IterIndex(int index) const
{
  return GetItem(index);
}

void BND_File3dmViewTable::SetItem(int index, const BND_ViewInfo& view)
{
  int count = m_named_views ? m_model->m_settings.m_named_views.Count()
    : m_model->m_settings.m_views.Count();
  if (index < 0 || index >= count)
    return;

  if (m_named_views)
    m_model->m_settings.m_named_views[index] = view.m_view;
  else
    m_model->m_settings.m_views[index] = view.m_view;
}

void BND_File3dmDimStyleTable::Add(const BND_DimensionStyle& dimstyle)
{
  const ON_DimStyle* ds = dimstyle.m_dimstyle;
  m_model->AddModelComponent(*ds);
}

BND_DimensionStyle* BND_File3dmDimStyleTable::FindIndex(int index) const
{
  ON_ModelComponentReference compref = m_model->DimensionStyleFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_DimStyle* modeldimstyle = const_cast<ON_DimStyle*>(ON_DimStyle::Cast(model_component));
  if (modeldimstyle)
    return new BND_DimensionStyle(modeldimstyle, &compref);
  return nullptr;
}

BND_DimensionStyle* BND_File3dmDimStyleTable::IterIndex(int index) const
{
  return FindIndex(index);
}

BND_DimensionStyle* BND_File3dmDimStyleTable::FindId(BND_UUID id) const
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->DimensionStyleFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_DimStyle* modeldimstyle = const_cast<ON_DimStyle*>(ON_DimStyle::Cast(model_component));
  if (modeldimstyle)
    return new BND_DimensionStyle(modeldimstyle, &compref);
  return nullptr;
}

void BND_File3dmInstanceDefinitionTable::Add(const BND_InstanceDefinitionGeometry& idef)
{
  const ON_InstanceDefinition* _idef = idef.m_idef;
  m_model->AddModelComponent(*_idef);
}
BND_InstanceDefinitionGeometry* BND_File3dmInstanceDefinitionTable::FindIndex(int index) const
{
  ON_ModelComponentReference compref = m_model->ComponentFromIndex(ON_ModelComponent::Type::InstanceDefinition, index);
  if (compref.IsEmpty())
    return nullptr;

  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_InstanceDefinition* modelidef = const_cast<ON_InstanceDefinition*>(ON_InstanceDefinition::Cast(model_component));
  if (modelidef)
    return new BND_InstanceDefinitionGeometry(modelidef, &compref);
  return nullptr;
}
BND_InstanceDefinitionGeometry* BND_File3dmInstanceDefinitionTable::IterIndex(int index) const
{
  return FindIndex(index);
}
BND_InstanceDefinitionGeometry* BND_File3dmInstanceDefinitionTable::FindId(BND_UUID id) const
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->ComponentFromId(ON_ModelComponent::Type::InstanceDefinition, _id);
  if (compref.IsEmpty())
    return nullptr;

  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_InstanceDefinition* modelidef = const_cast<ON_InstanceDefinition*>(ON_InstanceDefinition::Cast(model_component));
  if (modelidef)
    return new BND_InstanceDefinitionGeometry(modelidef, &compref);
  return nullptr;
}


std::wstring BND_RDKPlugInData::RdkDocumentData() const
{
  std::wstring rc;
  if (m_index >= 0 && m_index < m_model->m_userdata_table.Count())
  {
    ONX_Model_UserData* ud = m_model->m_userdata_table[m_index];
    if (ud)
    {
      ON_wString docdata;
      if( ONX_Model::GetRDKDocumentInformation(*ud, docdata) )
        rc = docdata.Array();
    }
  }
  return rc;
}


BND_File3dmPlugInData* BND_File3dmPlugInDataTable::GetPlugInData(int index)
{
  if (index < 0 || index >= m_model->m_userdata_table.Count())
    return nullptr;
  ONX_Model_UserData* ud = m_model->m_userdata_table[index];
  if (nullptr == ud)
    return nullptr;
  if (ONX_Model::IsRDKDocumentInformation(*ud))
    return new BND_RDKPlugInData(m_model, index);
  return new BND_File3dmPlugInData(m_model, index);
}

int BND_File3dmStringTable::Count() const
{
  ON_ClassArray<ON_UserString> str;
  return m_model->GetDocumentUserStrings(str);
}

int BND_File3dmStringTable::DocumentUserTextCount() const
{
  ON_ClassArray<ON_UserString> strings;
  m_model->GetDocumentUserStrings(strings);
  int cnt = 0;
  for (int i = 0; i < strings.Count(); i++)
    if (strings[i].m_key.Find(L"\\")>=0) cnt++;
  return cnt;
}

std::wstring BND_File3dmStringTable::GetKey(int i) const
{
  ON_ClassArray<ON_UserString> strings;
  m_model->GetDocumentUserStrings(strings);
  const ON_UserString& us = strings[i];
  return std::wstring(us.m_key.Array());
}

std::wstring BND_File3dmStringTable::GetValue(int i) const
{
  ON_ClassArray<ON_UserString> strings;
  m_model->GetDocumentUserStrings(strings);
  const ON_UserString& us = strings[i];
  return std::wstring(us.m_string_value.Array());
}

BND_TUPLE BND_File3dmStringTable::GetKeyValue(int i) const
{
  ON_ClassArray<ON_UserString> strings;
  m_model->GetDocumentUserStrings(strings);
  const ON_UserString& us = strings[i];
  std::wstring key(us.m_key.Array());
  std::wstring sval(us.m_string_value.Array());
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple rc(2);
  rc[0] = key;
  rc[1] = sval;
#else
  emscripten::val rc(emscripten::val::array());
  rc.set(0, key);
  rc.set(1, sval);
#endif
  return rc;
}

std::wstring BND_File3dmStringTable::GetValueFromKey(std::wstring key) const
{
  ON_ClassArray<ON_UserString> strings;
  m_model->GetDocumentUserStrings(strings);
  ON_wString _key(key.c_str());
  for (int i = 0; i < strings.Count(); i++)
  {
    if (strings[i].m_key.EqualOrdinal(_key, false))
      return std::wstring(strings[i].m_string_value.Array());
  }
  return std::wstring(L"");
}

void BND_File3dmStringTable::SetString(std::wstring key, std::wstring value)
{
  m_model->SetDocumentUserString(key.c_str(), value.c_str());
}

void BND_File3dmStringTable::Delete(std::wstring key)
{
  m_model->SetDocumentUserString(key.c_str(), nullptr);
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
std::string BND_ONXModel::Encode()
{
  ON_Write3dmBufferArchive archive(0, 0, 0, 0);// on_version__to_write);
  m_model->Write(archive);
  const unsigned char* buffer = (const unsigned char*)archive.Buffer();
  size_t length = archive.SizeOfArchive();

  std::string rc = base64_encode(buffer, (unsigned int)length);
  return rc;
}

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

BND_ONXModel* BND_ONXModel::Decode(std::string buffer)
{
  std::string decoded = base64_decode(buffer);
  int length = (int)decoded.length();
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  return FromByteArray(length, c);
}


bool BND_ONXModel::ReadTest(std::wstring path)
{
  ONX_ModelTest modeltest;
  bool rc = modeltest.ReadTest(path.c_str(), ONX_ModelTest::Type::Read, false, nullptr, nullptr);
  return rc;
}


// --------------------- Iterator helpers ------- //
#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;

template <typename IT, typename ET>
struct PyBNDIterator {
  PyBNDIterator(const IT table, py::object ref)
    : seq(table), ref(ref) {}

  ET next() {
    if(index>=seq.Count()) throw py::stop_iteration();
    return const_cast<IT>(seq).IterIndex(index++);
  }

  const IT seq;
  py::object ref;
  int index = 0;
};

#endif


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initExtensionsBindings(pybind11::module& m)
{
  py::class_<BND_File3dmPlugInData>(m, "File3dmPlugInData")
    ;

  py::class_<BND_RDKPlugInData, BND_File3dmPlugInData>(m, "File3dmRdkDocumentData")
    .def("RdkXml", &BND_RDKPlugInData::RdkDocumentData)
    ;

  py::class_<BND_File3dmPlugInDataTable>(m, "File3dmPlugInDataTable")
    .def("__len__", &BND_File3dmPlugInDataTable::Count)
    .def("__getitem__", &BND_File3dmPlugInDataTable::GetPlugInData)
    ;

  py::class_<BND_FileObject>(m, "File3dmObject")
    .def_property_readonly("Attributes", &BND_FileObject::GetAttributes)
    .def_property_readonly("Geometry", &BND_FileObject::GetGeometry)
    ;

  py::class_<PyBNDIterator<BND_ONXModel_ObjectTable&, BND_FileObject*> >(m, "__ObjectIterator")
    .def("__iter__", [](PyBNDIterator<BND_ONXModel_ObjectTable&, BND_FileObject*> &it) -> PyBNDIterator<BND_ONXModel_ObjectTable&, BND_FileObject*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_ONXModel_ObjectTable&, BND_FileObject*>::next)
    ;

  py::class_<BND_ONXModel_ObjectTable>(m, "File3dmObjectTable")
    .def("__len__", &BND_ONXModel_ObjectTable::Count)
    .def("__getitem__", &BND_ONXModel_ObjectTable::ModelObjectAt)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_ONXModel_ObjectTable&, BND_FileObject*>(s.cast<BND_ONXModel_ObjectTable &>(), s); })
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint1, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint2, py::arg("point"))
    .def("AddPoint", &BND_ONXModel_ObjectTable::AddPoint4, py::arg("point"))
    .def("AddPointCloud", &BND_ONXModel_ObjectTable::AddPointCloud, py::arg("cloud"), py::arg("attributes")=nullptr)
    .def("AddLine", &BND_ONXModel_ObjectTable::AddLine1, py::arg("from"), py::arg("to"))
    .def("AddPolyline", &BND_ONXModel_ObjectTable::AddPolyline2, py::arg("polyline"), py::arg("attributes")=nullptr)
    .def("AddArc", &BND_ONXModel_ObjectTable::AddArc, py::arg("arc"), py::arg("attributes")=nullptr)
    .def("AddCircle", &BND_ONXModel_ObjectTable::AddCircle, py::arg("circle"), py::arg("attributes") = nullptr)
    .def("AddEllipse", &BND_ONXModel_ObjectTable::AddEllipse, py::arg("ellipse"), py::arg("attributes") = nullptr)
    .def("AddSphere", &BND_ONXModel_ObjectTable::AddSphere, py::arg("sphere"), py::arg("attributes") = nullptr)
    .def("AddCurve", &BND_ONXModel_ObjectTable::AddCurve, py::arg("curve"), py::arg("attributes")=nullptr)
    .def("AddTextDot", &BND_ONXModel_ObjectTable::AddTextDot, py::arg("text"), py::arg("location"), py::arg("attributes")=nullptr)
    .def("AddSurface", &BND_ONXModel_ObjectTable::AddSphere, py::arg("surface"), py::arg("attributes")=nullptr)
    .def("AddExtrusion", &BND_ONXModel_ObjectTable::AddExtrusion, py::arg("extrusion"), py::arg("attributes")=nullptr)
    .def("AddMesh", &BND_ONXModel_ObjectTable::AddMesh, py::arg("mesh"), py::arg("attributes")=nullptr)
    .def("AddBrep", &BND_ONXModel_ObjectTable::AddBrep, py::arg("brep"), py::arg("attributes")=nullptr)
    .def("AddHatch", &BND_ONXModel_ObjectTable::AddHatch, py::arg("hatch"), py::arg("attributes")=nullptr)
    .def("Add", &BND_ONXModel_ObjectTable::Add, py::arg("geometry"), py::arg("attributes")=nullptr)
    .def("GetBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    .def("Delete", &BND_ONXModel_ObjectTable::Delete, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmMaterialTable&, BND_Material*> >(m, "__MaterialIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmMaterialTable&, BND_Material*>  &it) -> PyBNDIterator<BND_File3dmMaterialTable&, BND_Material*> & { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmMaterialTable&, BND_Material*> ::next)
    ;

  py::class_<BND_File3dmMaterialTable>(m, "File3dmMaterialTable")
    .def("__len__", &BND_File3dmMaterialTable::Count)
    .def("__getitem__", &BND_File3dmMaterialTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmMaterialTable&, BND_Material*>(s.cast<BND_File3dmMaterialTable &>(), s); })
    .def("Add", &BND_File3dmMaterialTable::Add, py::arg("material"))
    .def("FindIndex", &BND_File3dmMaterialTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmMaterialTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmBitmapTable&, BND_Bitmap*> >(m, "__ImageIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmBitmapTable&, BND_Bitmap*>  &it) -> PyBNDIterator<BND_File3dmBitmapTable&, BND_Bitmap*> & { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmBitmapTable&, BND_Bitmap*> ::next)
    ;

  py::class_<BND_File3dmBitmapTable>(m, "File3dmBitmapTable")
    .def("__len__", &BND_File3dmBitmapTable::Count)
    .def("__getitem__", &BND_File3dmBitmapTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmBitmapTable&, BND_Bitmap*>(s.cast<BND_File3dmBitmapTable &>(), s); })
    .def("Add", &BND_File3dmBitmapTable::Add, py::arg("bitmap"))
    .def("FindIndex", &BND_File3dmBitmapTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmBitmapTable::FindId, py::arg("id"))
    ;


  py::class_<PyBNDIterator<BND_File3dmLayerTable&, BND_Layer*> >(m, "__LayerIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmLayerTable&, BND_Layer*> &it) -> PyBNDIterator<BND_File3dmLayerTable&, BND_Layer*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmLayerTable&, BND_Layer*>::next)
    ;

  py::class_<BND_File3dmLayerTable>(m, "File3dmLayerTable")
    .def("__len__", &BND_File3dmLayerTable::Count)
    .def("__getitem__", &BND_File3dmLayerTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmLayerTable&, BND_Layer*>(s.cast<BND_File3dmLayerTable &>(), s); })
    .def("Add", &BND_File3dmLayerTable::Add, py::arg("layer"))
    .def("FindName", &BND_File3dmLayerTable::FindName, py::arg("name"), py::arg("parentId"))
    .def("FindIndex", &BND_File3dmLayerTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmLayerTable::FindId, py::arg("id"))
    ;


  py::class_<PyBNDIterator<BND_File3dmDimStyleTable&, BND_DimensionStyle*> >(m, "__DimStyleIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmDimStyleTable&, BND_DimensionStyle*> &it) -> PyBNDIterator<BND_File3dmDimStyleTable&, BND_DimensionStyle*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmDimStyleTable&, BND_DimensionStyle*>::next)
    ;

  py::class_<BND_File3dmDimStyleTable>(m, "File3dmDimStyleTable")
    .def("__len__", &BND_File3dmDimStyleTable::Count)
    .def("__getitem__", &BND_File3dmDimStyleTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmDimStyleTable&, BND_DimensionStyle*>(s.cast<BND_File3dmDimStyleTable &>(), s); })
    .def("Add", &BND_File3dmDimStyleTable::Add, py::arg("dimstyle"))
    .def("FindIndex", &BND_File3dmDimStyleTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmDimStyleTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmInstanceDefinitionTable&, BND_InstanceDefinitionGeometry*> >(m, "__IdefIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmInstanceDefinitionTable&, BND_InstanceDefinitionGeometry*> &it) -> PyBNDIterator<BND_File3dmInstanceDefinitionTable&, BND_InstanceDefinitionGeometry*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmInstanceDefinitionTable&, BND_InstanceDefinitionGeometry*>::next)
    ;

  py::class_<BND_File3dmInstanceDefinitionTable>(m, "File3dmInstanceDefinitionTable")
    .def("__len__", &BND_File3dmInstanceDefinitionTable::Count)
    .def("__getitem__", &BND_File3dmInstanceDefinitionTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmInstanceDefinitionTable&, BND_InstanceDefinitionGeometry*>(s.cast<BND_File3dmInstanceDefinitionTable &>(), s); })
    .def("Add", &BND_File3dmInstanceDefinitionTable::Add, py::arg("idef"))
    .def("FindIndex", &BND_File3dmInstanceDefinitionTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmInstanceDefinitionTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmViewTable&, BND_ViewInfo*> >(m, "__ViewIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmViewTable&, BND_ViewInfo*> &it) -> PyBNDIterator<BND_File3dmViewTable&, BND_ViewInfo*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmViewTable&, BND_ViewInfo*>::next)
    ;

  py::class_<BND_File3dmViewTable>(m, "File3dmViewTable")
    .def("__len__", &BND_File3dmViewTable::Count)
    .def("__getitem__", &BND_File3dmViewTable::GetItem)
    .def("__setitem__", &BND_File3dmViewTable::SetItem)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmViewTable&, BND_ViewInfo*>(s.cast<BND_File3dmViewTable &>(), s); })
    .def("Add", &BND_File3dmViewTable::Add, py::arg("view"))
    ;

  py::class_<BND_File3dmStringTable>(m, "File3dmStringTable")
    .def("__len__", &BND_File3dmStringTable::Count)
    .def("__getitem__", &BND_File3dmStringTable::GetKeyValue)
    .def("__getitem__", &BND_File3dmStringTable::GetValueFromKey)
    .def("__setitem__", &BND_File3dmStringTable::SetString)
    .def("DocumentUserTextCount", &BND_File3dmStringTable::DocumentUserTextCount)
    .def("Delete", &BND_File3dmStringTable::Delete, py::arg("key"))
    ;

  py::class_<BND_ONXModel>(m, "File3dm")
    .def(py::init<>())
    .def_static("Read", &BND_ONXModel::Read, py::arg("path"))
    .def_static("ReadNotes", &BND_ONXModel::ReadNotes, py::arg("path"))
    .def_static("ReadArchiveVersion", &BND_ONXModel::ReadArchiveVersion, py::arg("path"))
    .def_static("FromByteArray", [](py::buffer b) {
      py::buffer_info info = b.request();
      return BND_ONXModel::FromByteArray(static_cast<int>(info.size), info.ptr);
    })
    .def("Write", &BND_ONXModel::Write, py::arg("path"), py::arg("version")=0)
    .def_property("StartSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments)
    .def_property("ApplicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    .def_property("ApplicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    .def_property("ApplicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    .def_property_readonly("CreatedBy", &BND_ONXModel::GetCreatedBy)
    .def_property_readonly("LastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .def_property("Revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .def_property_readonly("Settings", &BND_ONXModel::Settings)
    .def_property_readonly("Objects", &BND_ONXModel::Objects)
    .def_property_readonly("Materials", &BND_ONXModel::Materials)
    .def_property_readonly("Bitmaps", &BND_ONXModel::Bitmaps)
    .def_property_readonly("Layers", &BND_ONXModel::Layers)
    .def_property_readonly("DimStyles", &BND_ONXModel::DimStyles)
    .def_property_readonly("InstanceDefinitions", &BND_ONXModel::InstanceDefinitions)
    .def_property_readonly("Views", &BND_ONXModel::Views)
    .def_property_readonly("NamedViews", &BND_ONXModel::NamedViews)
    .def_property_readonly("PlugInData", &BND_ONXModel::PlugInData)
    .def_property_readonly("Strings", &BND_ONXModel::Strings)
    .def("Encode", &BND_ONXModel::Encode)
    .def("Decode", &BND_ONXModel::Decode)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtensionsBindings(void*)
{
  class_<BND_File3dmPlugInData>("File3dmPlugInData")
    ;

  class_<BND_RDKPlugInData, base<BND_File3dmPlugInData>>("File3dmRdkDocumentData")
    .function("rdkXml", &BND_RDKPlugInData::RdkDocumentData)
    ;

  class_<BND_File3dmPlugInDataTable>("File3dmPlugInDataTable")
    .function("count", &BND_File3dmPlugInDataTable::Count)
    .function("get", &BND_File3dmPlugInDataTable::GetPlugInData, allow_raw_pointers())
    ;

  class_<BND_FileObject>("File3dmObject")
    .function("attributes", &BND_FileObject::GetAttributes, allow_raw_pointers())
    .function("geometry", &BND_FileObject::GetGeometry, allow_raw_pointers())
    ;

  class_<BND_ONXModel_ObjectTable>("File3dmObjectTable")
    .property("count", &BND_ONXModel_ObjectTable::Count)
    .function("get", &BND_ONXModel_ObjectTable::ModelObjectAt, allow_raw_pointers())
    .function("addPoint", &BND_ONXModel_ObjectTable::AddPoint1)
    .function("addPoint", &BND_ONXModel_ObjectTable::AddPoint2)
    .function("addPointCloud", &BND_ONXModel_ObjectTable::AddPointCloud, allow_raw_pointers())
    .function("addLine", &BND_ONXModel_ObjectTable::AddLine1)
    //.function("addPolyline", &BND_ONXModel_ObjectTable::AddPolyline2, allow_raw_pointers())
    .function("addArc", &BND_ONXModel_ObjectTable::AddArc, allow_raw_pointers())
    .function("addCircle", &BND_ONXModel_ObjectTable::AddCircle, allow_raw_pointers())
    .function("addEllipse", &BND_ONXModel_ObjectTable::AddEllipse, allow_raw_pointers())
    .function("addSphere", &BND_ONXModel_ObjectTable::AddSphere, allow_raw_pointers())
    .function("addCurve", &BND_ONXModel_ObjectTable::AddCurve, allow_raw_pointers())
    .function("addTextDot", &BND_ONXModel_ObjectTable::AddTextDot, allow_raw_pointers())
    .function("addSurface", &BND_ONXModel_ObjectTable::AddSphere, allow_raw_pointers())
    .function("addExtrusion", &BND_ONXModel_ObjectTable::AddExtrusion, allow_raw_pointers())
    .function("addMesh", &BND_ONXModel_ObjectTable::AddMesh, allow_raw_pointers())
    .function("addBrep", &BND_ONXModel_ObjectTable::AddBrep, allow_raw_pointers())
    .function("add", &BND_ONXModel_ObjectTable::Add, allow_raw_pointers())
    .function("getBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    .function("deleteItem", &BND_ONXModel_ObjectTable::Delete)
    ;

  class_<BND_File3dmMaterialTable>("File3dmMaterialTable")
    .function("count", &BND_File3dmMaterialTable::Count)
    .function("get", &BND_File3dmMaterialTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmMaterialTable::Add)
    .function("findIndex", &BND_File3dmMaterialTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmMaterialTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmBitmapTable>("File3dmBitmapTable")
    .function("count", &BND_File3dmBitmapTable::Count)
    .function("get", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmBitmapTable::Add)
    .function("findIndex", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmBitmapTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmLayerTable>("File3dmLayerTable")
    .function("count", &BND_File3dmLayerTable::Count)
    .function("get", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmLayerTable::Add)
    .function("findName", &BND_File3dmLayerTable::FindName, allow_raw_pointers())
    .function("findIndex", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmLayerTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmDimStyleTable>("File3dmDimStyleTable")
    .function("count", &BND_File3dmDimStyleTable::Count)
    .function("get", &BND_File3dmDimStyleTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmDimStyleTable::Add)
    .function("findIndex", &BND_File3dmDimStyleTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmDimStyleTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmInstanceDefinitionTable>("File3dmInstanceDefinitionTable")
    .function("count", &BND_File3dmInstanceDefinitionTable::Count)
    .function("get", &BND_File3dmInstanceDefinitionTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmInstanceDefinitionTable::Add)
    .function("findIndex", &BND_File3dmInstanceDefinitionTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmInstanceDefinitionTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmViewTable>("File3dmViewTable")
    .function("count", &BND_File3dmViewTable::Count)
    .function("get", &BND_File3dmViewTable::GetItem, allow_raw_pointers())
    .function("set", &BND_File3dmViewTable::SetItem)
    .function("add", &BND_File3dmViewTable::Add)
    ;

  class_<BND_File3dmStringTable>("File3dmStringTable")
    .function("count", &BND_File3dmStringTable::Count)
    .function("get", &BND_File3dmStringTable::GetKeyValue)
    .function("getvalue", &BND_File3dmStringTable::GetValueFromKey)
    .function("set", &BND_File3dmStringTable::SetString)
    .function("count", &BND_File3dmStringTable::DocumentUserTextCount)
    .function("delete", &BND_File3dmStringTable::Delete)
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
    .function("settings", &BND_ONXModel::Settings)
    .function("objects", &BND_ONXModel::Objects)
    .function("materials", &BND_ONXModel::Materials)
    .function("bitmaps", &BND_ONXModel::Bitmaps)
    .function("layers", &BND_ONXModel::Layers)
    .function("dimstyles", &BND_ONXModel::DimStyles)
    .function("instanceDefinitions", &BND_ONXModel::InstanceDefinitions)
    .function("views", &BND_ONXModel::Views)
    .function("namedViews", &BND_ONXModel::NamedViews)
    .function("plugInData", &BND_ONXModel::PlugInData)
    .function("strings", &BND_ONXModel::Strings)
    .function("encode", &BND_ONXModel::Encode)
    .class_function("decode", &BND_ONXModel::Decode, allow_raw_pointers())
    ;
}
#endif
