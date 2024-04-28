#include "bindings.h"
#include "base64.h"

// TODO: Move some of this functionality into core opennurbs
static bool SeekPastCompressedBuffer(ON_BinaryArchive& archive)
{
  if (!archive.ReadMode())
    return false;

  bool rc = false;
  char method = 0;
  unsigned int buffer_crc0 = 0;

  size_t sizeof__outbuffer;
  if (!archive.ReadCompressedBufferSize(&sizeof__outbuffer))
    return false;
  
  if (0 == sizeof__outbuffer)
    return true;

  if (!archive.ReadInt(&buffer_crc0)) // 32 bit crc of uncompressed buffer
    return false;

  if (!archive.ReadChar(&method))
    return false;

  if (method != 0 && method != 1)
    return false;

  switch (method)
  {
  case 0: // uncompressed
    rc = archive.SeekForward(sizeof__outbuffer);
    break;
  case 1: // compressed
    {
    ON__UINT32 tcode = 0;
    ON__INT64  big_value = 0;
    rc = archive.BeginRead3dmBigChunk(&tcode, &big_value);
    if (rc)
      rc = archive.EndRead3dmChunk();
    }
    break;
  }

  return rc;
}


static bool GetRDKEmbeddedFileHelper(ONX_Model_UserData& docud, ON_ClassArray<ON_wString>& paths,
  const wchar_t* specificPath, ON_SimpleArray<unsigned char>* pathBuffer, bool strictSearch)
{
  if (!ONX_Model::IsRDKDocumentInformation(docud))
    return false;

  ON_Read3dmBufferArchive a(docud.m_goo.m_value, docud.m_goo.m_goo, false, docud.m_usertable_3dm_version, docud.m_usertable_opennurbs_version);

  int version = 0;
  if (!a.ReadInt(&version))
    return false;

  if (4 != version)
    return false;

  //Read out the document data, and throw it away.
  {
    int slen = 0;
    if (!a.ReadInt(&slen))
      return 0;
    if (slen <= 0)
      return 0;
    if (slen + 4 > docud.m_goo.m_value)
      return 0;
    ON_String s;
    s.SetLength(slen);
    if (!a.ReadChar(slen, s.Array()))
      return 0;
  }

  unsigned int iCount = 0;
  if (!a.ReadInt(&iCount))
    return false;

  ON_SimpleArray<unsigned char> buffer;
  ON_wString searchFilename;
  if (specificPath && pathBuffer)
    ON_FileSystemPath::SplitPath(specificPath, nullptr, nullptr, &searchFilename);

  int path_count = 0;
  for (unsigned int i = 0; i < iCount; i++)
  {
    ON_wString sPath;
    if (!a.ReadString(sPath))
      return false;

    if (specificPath && pathBuffer)
    {
      bool pathMatch = sPath.EqualOrdinal(specificPath, !strictSearch);
      if (!pathMatch && !strictSearch)
      {
        ON_wString filename;
        ON_FileSystemPath::SplitPath(sPath, nullptr, nullptr, &filename);
        pathMatch = filename.EqualOrdinal(searchFilename, true);
      }
      if (pathMatch)
      {
        size_t size;
        if (!a.ReadCompressedBufferSize(&size))
          return false;

        bool bFailedCRC = false;
        pathBuffer->Reserve(size);
        if (!a.ReadCompressedBuffer(size, pathBuffer->Array(), &bFailedCRC))
          return false;

        pathBuffer->SetCount((int)size);
        return true;
      }
    }

    SeekPastCompressedBuffer(a);

    paths.Append(sPath);
    path_count++;
  }

  return path_count > 0;
}

std::string BND_ONXModel::GetEmbeddedFileAsBase64(std::wstring path)
{
  return GetEmbeddedFileAsBase64Strict(path, false);
}

std::string BND_ONXModel::GetEmbeddedFileAsBase64Strict(std::wstring path, bool strict)
{
  ON_ClassArray<ON_wString> paths;
  ON_SimpleArray<ONX_Model_UserData*>& userdata_table = m_model->m_userdata_table;
  ON_SimpleArray<unsigned char> buffer;
  for (int i = 0; i < userdata_table.Count(); i++)
  {
    ONX_Model_UserData* ud = userdata_table[i];
    if (ud && GetRDKEmbeddedFileHelper(*ud, paths, path.c_str(), &buffer, strict))
      break;
  }

  std::string rc;
  if (buffer.Count() > 0)
  {
    rc = base64_encode(buffer.Array(), buffer.UnsignedCount());
  }
  return rc;
}

BND_TUPLE BND_ONXModel::GetEmbeddedFilePaths()
{
  ON_ClassArray<ON_wString> paths;
  ON_SimpleArray<ONX_Model_UserData*>& userdata_table = m_model->m_userdata_table;
  for (int i = 0; i < userdata_table.Count(); i++)
  {
    ONX_Model_UserData* ud = userdata_table[i];
    if (ud && m_model->GetRDKEmbeddedFilePaths(*ud, paths))
    //if (ud && GetRDKEmbeddedFileHelper(*ud, paths, nullptr, nullptr, false))
      break;
  }
  int count = paths.Count();

  BND_TUPLE rc = CreateTuple(count);
  for (int i = 0; i < count; i++)
  {
    std::wstring path(paths[i].Array());
    SetTuple(rc, i, path);
  }
  return rc;
}

///////////////////////////////////////////////

BND_ONXModel::BND_ONXModel()
{
  m_model.reset(new ONX_Model());
}

BND_ONXModel::BND_ONXModel(ONX_Model* m)
{
  m_model.reset(m);
}

void BND_ONXModel::Destroy()
{
  ONX_Model* model = m_model.get();
  if(model)
    delete model;
  model = nullptr;
  m_model.reset(model);
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
int BND_ONXModel::GetArchiveVersion() const
{
  return m_model->m_3dm_file_version;
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
BND_DateTime BND_ONXModel::GetCreated() const
{
  return CreateDateTime(m_model->m_properties.m_RevisionHistory.m_create_time);
}
BND_DateTime BND_ONXModel::GetLastEdited() const
{
  return CreateDateTime(m_model->m_properties.m_RevisionHistory.m_last_edit_time);
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
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &plc, attributes);
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
    ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &nc, attributes);
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

BND_UUID BND_ONXModel_ObjectTable::AddObject(const class BND_FileObject* object)
{
  const ON_Geometry* g = object ? object->m_geometry->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, object->m_attributes);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddInstanceObject1(const class BND_InstanceReferenceGeometry* instanceReference)
{
  const ON_Geometry* g = instanceReference ? instanceReference->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, nullptr);
  return ON_UUID_to_Binding(rc);
}

BND_UUID BND_ONXModel_ObjectTable::AddInstanceObject2(const class BND_InstanceReferenceGeometry* instanceReference, const class BND_3dmObjectAttributes* attributes)
{
  const ON_Geometry* g = instanceReference ? instanceReference->GeometryPointer() : nullptr;
  ON_UUID rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), g, attributes);
  return ON_UUID_to_Binding(rc);
}

/*
BND_UUID BND_ONXModel_ObjectTable::AddInstanceObject3(int idefIndex, const class BND_Transform& transform)
{
  ON_UUID rc = ON_nil_uuid;
  const ON_ModelComponentReference& idef_component_ref = m_model->ComponentFromIndex(ON_ModelComponent::Type::InstanceDefinition, idefIndex);

  if (!idef_component_ref.IsEmpty())
    {
      const ON_InstanceDefinition* idef = ON_InstanceDefinition::Cast(idef_component_ref.ModelComponent());
      if (nullptr != idef)
      {
        ON_InstanceRef iref;
        iref.m_instance_definition_uuid = idef->Id();
        const ON_Xform* xform = transform ? &(transform->m_xform) : nullptr;
        iref.m_xform = *xform;
        // Internal_ONX_Model_AddModelGeometry makes a copy
        rc = Internal_ONX_Model_AddModelGeometry(m_model.get(), &iref, nullptr);
      }
    }
  }

  return rc;

}

BND_UUID BND_ONXModel_ObjectTable::AddInstanceObject4(int idefIndex, const class BND_Transform& transform, const class BND_3dmObjectAttributes* attributes)
{
  return ON_nil_uuid;
}
*/

void BND_ONXModel_ObjectTable::Delete(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  m_model->RemoveModelComponent(ON_ModelComponent::Type::ModelGeometry, _id);
}

int BND_ONXModel_ObjectTable::Count() const
{
  int count = m_model->ActiveComponentCount(ON_ModelComponent::Type::ModelGeometry) +
    m_model->ActiveAndDeletedComponentCount(ON_ModelComponent::Type::RenderLight);
  return count;
}

static BND_FileObject* FileObjectFromCompRef(ON_ModelComponentReference& compref)
{
  BND_GeometryBase* geometry = dynamic_cast<BND_GeometryBase*>(BND_CommonObject::CreateWrapper(compref));
  if (nullptr == geometry)
    return nullptr;

  const ON_ModelComponent* model_component = compref.ModelComponent();
  const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
  if (nullptr == geometryComponent)
  {
    delete geometry;
    return nullptr;
  }

  ON_3dmObjectAttributes* attrs = const_cast<ON_3dmObjectAttributes*>(geometryComponent->Attributes(nullptr));
  if (nullptr == attrs)
  {
    delete geometry;
    return nullptr;
  }
  BND_FileObject* rc = new BND_FileObject();
  rc->m_attributes = new BND_3dmObjectAttributes(attrs, &compref);
  rc->m_geometry = geometry;
  return rc;
}

BND_FileObject* BND_ONXModel_ObjectTable::ModelObjectAt(int index)
{
#if defined(ON_PYTHON_COMPILE)
  if (index < 0)
    throw pybind11::index_error();
#else
  if (index < 0)
    return nullptr;
#endif

  if (0 == index)
    m_compref_cache.Empty(); // clear cache every time we restart counting

  if (m_compref_cache.Count() == 0)
  {
    m_compref_cache.Reserve(Count());
    ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
    ON_ModelComponentReference compref = iterator.FirstComponentReference();
    while (!compref.IsEmpty())
    {
      m_compref_cache.Append(compref);
      compref = iterator.NextComponentReference();
    }

    ONX_ModelComponentIterator iterator2(*m_model.get(), ON_ModelComponent::Type::RenderLight);
    compref = iterator2.FirstComponentReference();
    while (!compref.IsEmpty())
    {
      m_compref_cache.Append(compref);
      compref = iterator2.NextComponentReference();
    }
  }

  if (index < m_compref_cache.Count())
  {
    return FileObjectFromCompRef(m_compref_cache[index]);
  }

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
}

// helper function for iterator
BND_FileObject* BND_ONXModel_ObjectTable::IterIndex(int index)
{
  return ModelObjectAt(index);
}

BND_BoundingBox BND_ONXModel_ObjectTable::GetBoundingBox() const
{
  return BND_BoundingBox(m_model->ModelGeometryBoundingBox());
}

BND_FileObject* BND_ONXModel_ObjectTable::FindId(BND_UUID id) const
{
	ON_UUID _id = Binding_to_ON_UUID(id);
	ON_ModelComponentReference compref = m_model->ComponentFromId(ON_ModelComponent::Type::ModelGeometry, _id);
	if (compref.IsEmpty())
		return nullptr;

	const ON_ModelComponent* model_component = compref.ModelComponent();
	const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
	if (nullptr == geometryComponent)
		return nullptr;

	BND_GeometryBase* geometry = dynamic_cast<BND_GeometryBase*>(BND_CommonObject::CreateWrapper(compref));
	if (nullptr == geometry)
		return nullptr;
	ON_3dmObjectAttributes* attrs = const_cast<ON_3dmObjectAttributes*>(geometryComponent->Attributes(nullptr));
	if (nullptr == attrs)
		return nullptr;

	BND_FileObject* rc = new BND_FileObject();
	rc->m_attributes = new BND_3dmObjectAttributes(attrs, &compref);
	rc->m_geometry = geometry;
	return rc;
}

int BND_File3dmMaterialTable::Add(const BND_Material& material)
{
  const ON_Material* m = material.m_material;
  ON_ModelComponentReference mr = m_model->AddModelComponent(*m);
  const ON_Material* managed_material = ON_Material::FromModelComponentRef(mr, nullptr);
  int material_index = (nullptr != managed_material) ? managed_material->Index() : ON_UNSET_INT_INDEX;
  if ( material_index < 0 )
  {
    ON_ERROR("failed to add material.");
  }
  return material_index;
}

BND_Material* BND_File3dmMaterialTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_Material* BND_File3dmMaterialTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->MaterialFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Material* modelmaterial = const_cast<ON_Material*>(ON_Material::Cast(model_component));
  if (modelmaterial)
    return new BND_Material(modelmaterial, &compref);

#if defined(ON_PYTHON_COMPILE)
    throw pybind11::index_error();
#else
  return nullptr;
#endif
}

BND_Material* BND_File3dmMaterialTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->MaterialFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Material* modelmaterial = const_cast<ON_Material*>(ON_Material::Cast(model_component));
  if (modelmaterial)
    return new BND_Material(modelmaterial, &compref);
  return nullptr;
}

BND_Material* BND_File3dmMaterialTable::FromAttributes(const BND_3dmObjectAttributes* attributes)
{
  if (nullptr == attributes)
    return nullptr;
  ON_ModelComponentReference compref = m_model->MaterialFromAttributes(*attributes->m_attributes);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Material* modelmaterial = const_cast<ON_Material*>(ON_Material::Cast(model_component));
  if (modelmaterial)
    return new BND_Material(modelmaterial, &compref);
  return nullptr;
}


void BND_File3dmLinetypeTable::Add(const BND_Linetype& linetype)
{
  const ON_Linetype* l = linetype.m_linetype;
  m_model->AddModelComponent(*l);
}

void BND_File3dmLinetypeTable::Delete(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  m_model->RemoveModelComponent(ON_ModelComponent::Type::LinePattern, _id);
}

BND_Linetype* BND_File3dmLinetypeTable::IterIndex(int index)
{
  return FindIndex(index);
}

template<typename ON_Type, typename BND_Type>
BND_Type* bindingFromCompRef(ON_ModelComponentReference compref)
{
  const ON_ModelComponent* model_component = compref.ModelComponent();
  if (compref.IsEmpty())
    return nullptr;
  ON_Type* model_object = const_cast<ON_Type*>(ON_Type::Cast(model_component));
  if (model_object)
    return new BND_Type(model_object, &compref);
  return nullptr;
}

BND_Linetype* BND_File3dmLinetypeTable::FindIndex(int index)
{
  if (index >= 0)
  {
    ON_ModelComponentReference compref = m_model->ComponentFromIndex(ON_ModelComponent::Type::LinePattern, index); //no specific method in ON Extensions, therefore getting component here
    return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
  }
  if (index == ON_Linetype::Continuous.Index()) // -1
    return new BND_Linetype(ON_Linetype::Continuous);
  if (index == ON_Linetype::ByLayer.Index()) // -2
    return new BND_Linetype(ON_Linetype::ByLayer);
  if (index == ON_Linetype::ByParent.Index()) // -3
    return new BND_Linetype(ON_Linetype::ByParent);
  if (index == ON_Linetype::Hidden.Index()) // -4
    return new BND_Linetype(ON_Linetype::Hidden);
  if (index == ON_Linetype::Dashed.Index()) // -5
    return new BND_Linetype(ON_Linetype::Dashed);
  if (index == ON_Linetype::DashDot.Index()) // -6
    return new BND_Linetype(ON_Linetype::DashDot);
  if (index == ON_Linetype::Center.Index()) // -7
    return new BND_Linetype(ON_Linetype::Center);
  if (index == ON_Linetype::Border.Index()) // -8
    return new BND_Linetype(ON_Linetype::Border);
  if (index == ON_Linetype::Dots.Index()) // -9
    return new BND_Linetype(ON_Linetype::Dots);
  return nullptr;
}

BND_Linetype* BND_File3dmLinetypeTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->LinePatternFromId(_id);
  return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
}

BND_Linetype* BND_File3dmLinetypeTable::FindName(std::wstring name)
{
  ON_ModelComponentReference compref = m_model->LinePatternFromName(name.c_str());
  return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
}

BND_Linetype* BND_File3dmLinetypeTable::FromAttributes(const BND_3dmObjectAttributes* attributes)
{
  ON_ModelComponentReference compref = m_model->LinePatternFromAttributes(*attributes->m_attributes);
  return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
}

BND_Linetype* BND_File3dmLinetypeTable::FromLayerIndex(int index)
{
  ON_ModelComponentReference compref = m_model->LinePatternFromLayerIndex(index);
  return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
}

BND_Linetype* BND_File3dmLinetypeTable::GetCurrent()
{
  // V6 and later stores the ID of the current linetype
  ON_UUID _id = m_model->m_settings.CurrentLinePatternId();
  if (_id != ON_nil_uuid)
  {
    ON_ModelComponentReference compref = m_model->LinePatternFromId(_id);
    return bindingFromCompRef<ON_Linetype, BND_Linetype>(compref);
  }
  // V5 and earlier store the index of the current linetype
  int _index = m_model->m_settings.CurrentLinePatternIndex();
  if (_index != ON_UNSET_INT_INDEX)
  {
    return FindIndex(_index);
  }
  return nullptr;
}

void BND_File3dmLinetypeTable::SetCurrent(BND_Linetype* linetype)
{
  if (linetype->IsValid() && linetype->GetIndex() >= 0)
  {
    ON_UUID _id = linetype->m_linetype->Id();
    m_model->m_settings.SetCurrentLinePatternId(_id);
  }
}


void BND_File3dmBitmapTable::Add(const BND_Bitmap& bitmap)
{
  const ON_Bitmap* b = bitmap.m_bitmap;
  m_model->AddModelComponent(*b);
}

BND_Bitmap* BND_File3dmBitmapTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->MaterialFromIndex(index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Bitmap* modelbitmap = const_cast<ON_Bitmap*>(ON_Bitmap::Cast(model_component));
  if (modelbitmap)
    return new BND_Bitmap(modelbitmap, &compref);

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
}

BND_Bitmap* BND_File3dmBitmapTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_Bitmap* BND_File3dmBitmapTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->MaterialFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Bitmap* modelbitmap = const_cast<ON_Bitmap*>(ON_Bitmap::Cast(model_component));
  if (modelbitmap)
    return new BND_Bitmap(modelbitmap, &compref);
  return nullptr;
}


int BND_File3dmLayerTable::Add(const BND_Layer& layer)
{
  const ON_Layer* l = layer.m_layer;
  ON_ModelComponentReference mr = m_model->AddModelComponent(*l);
  const ON_Layer* managed_layer = ON_Layer::FromModelComponentRef(mr, nullptr);
  int layer_index = (nullptr != managed_layer) ? managed_layer->Index() : ON_UNSET_INT_INDEX;
  return layer_index;
}

int BND_File3dmLayerTable::AddLayer(std::wstring name, BND_Color color)
{
  ON_Color c = Binding_to_ON_Color(color);
  int rc = m_model->AddLayer(name.c_str(), c);
  return rc;
}

BND_Layer* BND_File3dmLayerTable::FindName(std::wstring name, BND_UUID parentId)
{
  ON_UUID id = Binding_to_ON_UUID(parentId);
  ON_ModelComponentReference compref  = m_model->LayerFromName(id, name.c_str());
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
    return new BND_Layer(modellayer, &compref, m_model);
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
    return new BND_Layer(modellayer, &compref, m_model);

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
}

BND_Layer* BND_File3dmLayerTable::FindId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->LayerFromId(_id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Layer* modellayer = const_cast<ON_Layer*>(ON_Layer::Cast(model_component));
  if (modellayer)
    return new BND_Layer(modellayer, &compref, m_model);
  return nullptr;
}

void BND_File3dmGroupTable::Add(const BND_Group& group)
{
  const ON_Group* l = group.m_group;
  m_model->AddModelComponent(*l);
}

bool BND_File3dmGroupTable::Delete(const BND_Group& group)
{
  ON_UUID _id = Binding_to_ON_UUID(group.GetId());
  ON_ModelComponentReference compref = m_model->RemoveModelComponent(ON_ModelComponent::Type::Group, _id);
  return !compref.IsEmpty();
}

bool BND_File3dmGroupTable::DeleteId(BND_UUID id)
{
  ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->RemoveModelComponent(ON_ModelComponent::Type::Group, _id);
  return !compref.IsEmpty();
}

bool BND_File3dmGroupTable::DeleteIndex(int index)
{
  ON_ComponentManifestItem item = m_model->Manifest().ItemFromIndex(ON_ModelComponent::Type::Group, index);
  if (!item.IsValid()) return false;
  ON_ModelComponentReference compref = m_model->RemoveModelComponent(ON_ModelComponent::Type::Group, item.Id());
  return !compref.IsEmpty();
}

BND_Group* BND_File3dmGroupTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_Group* BND_File3dmGroupTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->ComponentFromIndex(ON_ModelComponent::Type::Group, index); //no specific method in ON Extensions, therefore getting component here
  const ON_ModelComponent* model_component = compref.ModelComponent();
  if (!compref.IsEmpty())
  {
    ON_Group* modelgroup = const_cast<ON_Group*>(ON_Group::Cast(model_component));
    if (modelgroup)
      return new BND_Group(modelgroup, &compref);
  }

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
}

BND_Group* BND_File3dmGroupTable::FindName(std::wstring name)
{
  ON_ModelComponentReference compref = m_model->ComponentFromName(ON_ModelComponent::Type::Group, ON_nil_uuid, name.c_str());
  if (compref.IsEmpty())
    return nullptr;
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_Group* modelgroup = const_cast<ON_Group*>(ON_Group::Cast(model_component));
  if (modelgroup)
    return new BND_Group(modelgroup, &compref);
  return nullptr;
}

BND_TUPLE BND_File3dmGroupTable::GroupMembers(int groupIndex)
{
  ON_SimpleArray<BND_FileObject*> fileObjects;
  ONX_ModelComponentIterator iterator(*m_model.get(), ON_ModelComponent::Type::ModelGeometry);
  ON_ModelComponentReference compref = iterator.FirstComponentReference();
  while (!compref.IsEmpty())
  {
    const ON_ModelComponent* model_component = compref.ModelComponent();
    const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
    if (geometryComponent)
    {
      const ON_3dmObjectAttributes* attrs = geometryComponent->Attributes(nullptr);
      if (attrs && attrs->IsInGroup(groupIndex))
      {
        BND_GeometryBase* geometry = dynamic_cast<BND_GeometryBase*>(BND_CommonObject::CreateWrapper(compref));
        if (geometry)
        {
          BND_FileObject* rc = FileObjectFromCompRef(compref);
          if (rc)
            fileObjects.Append(rc);
        }
      }
    }
    compref = iterator.NextComponentReference();
  }

  BND_TUPLE rc = CreateTuple(fileObjects.Count());
  for (int i = 0; i < fileObjects.Count(); i++)
  {
    SetTuple<BND_FileObject*>(rc, i, fileObjects[i]);
  }
  return rc;
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

#if defined(ON_PYTHON_COMPILE)
  if (index < 0 || index >= count)
    throw pybind11::index_error();
#else
  if (index < 0 || index >= count)
    return nullptr;
#endif

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

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
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


void BND_File3dmInstanceDefinitionTable::AddInstanceDefinition(const BND_InstanceDefinitionGeometry& idef)
{
  const ON_InstanceDefinition* _idef = idef.m_idef;
  m_model->AddModelComponent(*_idef);
}

int BND_File3dmInstanceDefinitionTable::Add(std::wstring name, std::wstring description, std::wstring url, std::wstring url_tag, ON_3dPoint basePoint, BND_TUPLE geometry, BND_TUPLE attributes)
{

  int index = -1;
#if defined(ON_PYTHON_COMPILE)
  const int count_g = geometry.size();
  const int count_a = attributes.size();
#else
  const int count_g = geometry["length"].as<int>();
  const int count_a = attributes["length"].as<int>();
#endif

  if(m_model && count_g > 0) 
  {
    // Determine if we need to transform geometry to world origin
    ON_Xform xf;
    ON_Xform* pXform = nullptr;
    if (basePoint.IsValid() && basePoint != ON_3dPoint::Origin)
    {
      xf = ON_Xform::TranslationTransformation(ON_3dPoint::Origin - basePoint);
      pXform = &xf;
    }

    ON_SimpleArray<ON_UUID> object_uuids;

    for ( int i = 0; i < count_g; i ++ ) 
    {

#if defined(ON_PYTHON_COMPILE)
      BND_GeometryBase g = geometry[i].cast<BND_GeometryBase>();  
      BND_3dmObjectAttributes oa = attributes[i].cast<BND_3dmObjectAttributes>();
#else
      BND_GeometryBase g = geometry[i].as<BND_GeometryBase>();  
      BND_3dmObjectAttributes oa = attributes[i].as<BND_3dmObjectAttributes>();
#endif
      
      const ON_Geometry* pConstGeom = g.GeometryPointer();
      const ON_3dmObjectAttributes* pConstAtts = i < count_a ? oa.m_attributes : &ON_3dmObjectAttributes::DefaultAttributes;

      if (pConstGeom && pConstAtts)
      {
        ON_Geometry* pGeom = pConstGeom->Duplicate(); // Copy so we can transform

        if (pGeom)
        {

          // Make certain that proper flags are set for instance definiton geometry
          ON_3dmObjectAttributes atts(*pConstAtts);
          atts.m_uuid = ON_nil_uuid;
          atts.SetMode(ON::object_mode::idef_object);
          atts.RemoveFromAllGroups();
          atts.m_space = ON::model_space;
          atts.m_viewport_id = ON_nil_uuid;

          // Transform if needed
          if (pXform)
          {
            atts.Transform(pGeom, *pXform);
            pGeom->Transform(*pXform);
          }

          //have to pass in BND_3dmObjectAttributes to Internal_ONX_Model_AddModelGeometry
          BND_3dmObjectAttributes _atts;
          _atts.m_attributes = &atts;
          ON_UUID uuid = Internal_ONX_Model_AddModelGeometry(m_model.get(), pGeom, &_atts);
          if (ON_UuidIsNotNil(uuid))
            object_uuids.Append(uuid);

          delete pGeom; // Don't leak...

        }

      }

    }

    if (object_uuids.Count())
    {
      ON_InstanceDefinition* idef = new ON_InstanceDefinition();
      if (nullptr != idef)
      {
        idef->SetInstanceGeometryIdList(object_uuids);
        idef->SetInstanceDefinitionType(ON_InstanceDefinition::IDEF_UPDATE_TYPE::Static);
        idef->SetName(name.c_str());
        idef->SetDescription(description.c_str());
        idef->SetURL(url.c_str());
        idef->SetURL_Tag(url_tag.c_str());
        ON_ModelComponentReference model_component_reference = m_model->AddManagedModelComponent(idef, true);
        if (!model_component_reference.IsEmpty())
        {
          const ON_ModelComponent* model_component = model_component_reference.ModelComponent();
          if (nullptr != model_component)
            index = model_component->Index();
        }
      }
    }
  }
  
  return index;
}

BND_InstanceDefinitionGeometry* BND_File3dmInstanceDefinitionTable::FindIndex(int index) const
{
  ON_ModelComponentReference compref = m_model->ComponentFromIndex(ON_ModelComponent::Type::InstanceDefinition, index);
  if (!compref.IsEmpty())
  {
    const ON_ModelComponent* model_component = compref.ModelComponent();
    ON_InstanceDefinition* modelidef = const_cast<ON_InstanceDefinition*>(ON_InstanceDefinition::Cast(model_component));
    if (modelidef)
      return new BND_InstanceDefinitionGeometry(modelidef, &compref);
  }

#if defined(ON_PYTHON_COMPILE)
  throw pybind11::index_error();
#else
  return nullptr;
#endif
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
#if defined(ON_PYTHON_COMPILE)
  if (index < 0 || index >= m_model->m_userdata_table.Count())
    throw pybind11::index_error();
#else
  if (index < 0 || index >= m_model->m_userdata_table.Count())
    return nullptr;
#endif
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

#if defined(ON_PYTHON_COMPILE)
  if (i < 0 || i >= strings.Count())
    throw pybind11::index_error();
#endif

  const ON_UserString& us = strings[i];
  std::wstring key(us.m_key.Array());
  std::wstring sval(us.m_string_value.Array());
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, key);
  SetTuple(rc, 1, sval);
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
  return Encode2(nullptr);
}

std::string BND_ONXModel::Encode2(const BND_File3dmWriteOptions* options)
{
  BND_File3dmWriteOptions defaults;
  if (nullptr == options)
    options = &defaults;

  ON_Write3dmBufferArchive archive(0, 0, options->VersionForWriting(), ON::Version());
  archive.SetShouldSerializeUserDataDefault(options->SaveUserData());

  m_model->Write(archive, options->VersionForWriting());
  const unsigned char* buffer = (const unsigned char*)archive.Buffer();
  size_t length = archive.SizeOfArchive();

  std::string rc = base64_encode(buffer, (unsigned int)length);
  return rc;
}


#if defined(ON_WASM_COMPILE)
emscripten::val BND_ONXModel::ToByteArray() const
{
  return ToByteArray2(nullptr);
}

emscripten::val BND_ONXModel::ToByteArray2(const BND_File3dmWriteOptions* options) const
{
  BND_File3dmWriteOptions defaults;
  if (nullptr == options)
    options = &defaults;

  ON_Write3dmBufferArchive archive(0, 0, options->VersionForWriting(), ON::Version());
  archive.SetShouldSerializeUserDataDefault(options->SaveUserData());

  m_model->Write(archive, options->VersionForWriting());
  const unsigned char* buffer = (const unsigned char*)archive.Buffer();
  size_t length = archive.SizeOfArchive();

  emscripten::val Uint8Array = emscripten::val::global("Uint8Array");
  emscripten::val rc = Uint8Array.new_(emscripten::typed_memory_view(length, buffer));
  return rc;
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

BND_ONXModel* BND_ONXModel::Decode(std::string buffer)
{
  std::string decoded = base64_decode(buffer);
  int length = (int)decoded.length();
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  return FromByteArray(length, c);
}

std::wstring BND_ONXModel::RdkXml() const
{
  std::wstring rc;
  ON_wString s;
  int count = m_model->m_userdata_table.Count();
  for (int i = 0; i < count; i++)
  {
    ONX_Model_UserData* ud = m_model->m_userdata_table[i];
    if (ud && ONX_Model::GetRDKDocumentInformation(*ud, s))
    {
      rc = s.Array();
      break;
    }
  }
  return rc;
}

bool BND_ONXModel::ReadTest(std::wstring path)
{
  ONX_ModelTest modeltest;
  bool rc = modeltest.ReadTest(path.c_str(), ONX_ModelTest::Type::Read, false, nullptr, nullptr);
  return rc;
}

BND_File3dmWriteOptions::BND_File3dmWriteOptions()
{
  m_version = ON_BinaryArchive::CurrentArchiveVersion() / 10;
}

int BND_File3dmWriteOptions::VersionForWriting() const
{
  if (m_version < 5)
    return m_version;
  return m_version * 10;
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
    .def("AddSurface", &BND_ONXModel_ObjectTable::AddSurface, py::arg("surface"), py::arg("attributes")=nullptr)
    .def("AddExtrusion", &BND_ONXModel_ObjectTable::AddExtrusion, py::arg("extrusion"), py::arg("attributes")=nullptr)
    .def("AddMesh", &BND_ONXModel_ObjectTable::AddMesh, py::arg("mesh"), py::arg("attributes")=nullptr)
    .def("AddBrep", &BND_ONXModel_ObjectTable::AddBrep, py::arg("brep"), py::arg("attributes")=nullptr)
    .def("AddHatch", &BND_ONXModel_ObjectTable::AddHatch, py::arg("hatch"), py::arg("attributes")=nullptr)
    .def("Add", &BND_ONXModel_ObjectTable::Add, py::arg("geometry"), py::arg("attributes")=nullptr)
    .def("AddInstanceObject", &BND_ONXModel_ObjectTable::AddInstanceObject1, py::arg("idef") )
    .def("AddInstanceObject", &BND_ONXModel_ObjectTable::AddInstanceObject2, py::arg("idef"), py::arg("attributes")=nullptr)
    .def("AddObject", &BND_ONXModel_ObjectTable::AddObject, py::arg("object"))
    .def("GetBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    .def("Delete", &BND_ONXModel_ObjectTable::Delete, py::arg("id"))
    .def("FindId", &BND_ONXModel_ObjectTable::FindId, py::arg("id"))
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
    .def("FindFromAttributes", &BND_File3dmMaterialTable::FromAttributes)
    ;

  py::class_<PyBNDIterator<BND_File3dmLinetypeTable&, BND_Linetype*> >(m, "__LinetypeIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmLinetypeTable&, BND_Linetype*> &it) -> PyBNDIterator<BND_File3dmLinetypeTable&, BND_Linetype*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmLinetypeTable&, BND_Linetype*>::next)
    ;

  py::class_<BND_File3dmLinetypeTable>(m, "File3dmLinetypeTable")
    .def("__len__", &BND_File3dmLinetypeTable::Count)
    .def("__getitem__", static_cast<BND_Linetype* (BND_File3dmLinetypeTable::*)(int)>(&BND_File3dmLinetypeTable::FindIndex))
    .def("__getitem__", static_cast<BND_Linetype* (BND_File3dmLinetypeTable::*)(BND_UUID)>(&BND_File3dmLinetypeTable::FindId))
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmLinetypeTable&, BND_Linetype*>(s.cast<BND_File3dmLinetypeTable &>(), s); })
    .def("Add", &BND_File3dmLinetypeTable::Add, py::arg("linetype"))
    .def("Delete", &BND_File3dmLinetypeTable::Delete, py::arg("id"))
    .def("FindIndex", &BND_File3dmLinetypeTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmLinetypeTable::FindId, py::arg("id"))
    .def("FindName", &BND_File3dmLinetypeTable::FindName, py::arg("name"))
    .def("FromAttributes", &BND_File3dmLinetypeTable::FromAttributes, py::arg("attributes"))
    .def("FromLayerIndex", &BND_File3dmLinetypeTable::FromLayerIndex, py::arg("index"))
    .def_property("Current", &BND_File3dmLinetypeTable::GetCurrent, &BND_File3dmLinetypeTable::SetCurrent)
    .def_property("CurrentSource", &BND_File3dmLinetypeTable::GetCurrentSource, &BND_File3dmLinetypeTable::SetCurrentSource)
    .def_property("Scale", &BND_File3dmLinetypeTable::GetScale, &BND_File3dmLinetypeTable::SetScale)
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
    .def("AddLayer", &BND_File3dmLayerTable::AddLayer, py::arg("name"), py::arg("color"))
    .def("FindName", &BND_File3dmLayerTable::FindName, py::arg("name"), py::arg("parentId"))
    .def("FindIndex", &BND_File3dmLayerTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmLayerTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmGroupTable&, BND_Group*> >(m, "__GroupIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmGroupTable&, BND_Group*> &it) -> PyBNDIterator<BND_File3dmGroupTable&, BND_Group*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmGroupTable&, BND_Group*>::next)
    ;

  py::class_<BND_File3dmGroupTable>(m, "File3dmGroupTable")
    .def("__len__", &BND_File3dmGroupTable::Count)
    .def("__getitem__", &BND_File3dmGroupTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmGroupTable&, BND_Group*>(s.cast<BND_File3dmGroupTable &>(), s); })
    .def("Add", &BND_File3dmGroupTable::Add, py::arg("group"))
    .def("Delete", &BND_File3dmGroupTable::Delete, py::arg("group"))
    .def("Delete", &BND_File3dmGroupTable::DeleteIndex, py::arg("index"))
    .def("Delete", &BND_File3dmGroupTable::DeleteId, py::arg("id")) // This overload must come last because ON_UUID is a pybind11::object and accepts anything given to it
    .def("FindIndex", &BND_File3dmGroupTable::FindIndex, py::arg("index"))
    .def("FindName", &BND_File3dmGroupTable::FindName, py::arg("name"))
    .def("GroupMembers", &BND_File3dmGroupTable::GroupMembers, py::arg("groupIndex"))
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
    .def("AddInstanceDefinition", &BND_File3dmInstanceDefinitionTable::AddInstanceDefinition, py::arg("idef"))
    .def("Add", &BND_File3dmInstanceDefinitionTable::Add, py::arg("name"), py::arg("description"), py::arg("url"), py::arg("urlTag"), py::arg("basePoint"), py::arg("geometry"), py::arg("attributes"))
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

  py::class_<BND_File3dmWriteOptions>(m, "File3dmWriteOptions")
    .def(py::init<>())
    .def_property("Version", &BND_File3dmWriteOptions::GetVersion, &BND_File3dmWriteOptions::SetVersion)
    .def_property("SaveUserData", &BND_File3dmWriteOptions::SaveUserData, &BND_File3dmWriteOptions::SetSaveUserData)
    ;

  py::class_<PyBNDIterator<BND_File3dmEmbeddedFileTable&, BND_File3dmEmbeddedFile*> >(m, "__EmbeddedFileIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmEmbeddedFileTable&, BND_File3dmEmbeddedFile*> &it) -> PyBNDIterator<BND_File3dmEmbeddedFileTable&, BND_File3dmEmbeddedFile*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmEmbeddedFileTable&, BND_File3dmEmbeddedFile*>::next)
    ;

  py::class_<BND_File3dmEmbeddedFileTable>(m, "File3dmEmbeddedFileTable")
    .def("__len__", &BND_File3dmEmbeddedFileTable::Count)
    .def("__getitem__", &BND_File3dmEmbeddedFileTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmEmbeddedFileTable&, BND_File3dmEmbeddedFile*>(s.cast<BND_File3dmEmbeddedFileTable &>(), s); })
    .def("Add", &BND_File3dmEmbeddedFileTable::Add, py::arg("embedded_file"))
    .def("FindIndex", &BND_File3dmEmbeddedFileTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmEmbeddedFileTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmRenderContentTable&, BND_File3dmRenderContent*> >(m, "__RenderContentIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmRenderContentTable&, BND_File3dmRenderContent*> &it) -> PyBNDIterator<BND_File3dmRenderContentTable&, BND_File3dmRenderContent*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmRenderContentTable&, BND_File3dmRenderContent*>::next)
    ;
  py::class_<BND_File3dmRenderContentTable>(m, "File3dmRenderContentTable")
    .def("__len__", &BND_File3dmRenderContentTable::Count)
    .def("__getitem__", &BND_File3dmRenderContentTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmRenderContentTable&, BND_File3dmRenderContent*>(s.cast<BND_File3dmRenderContentTable &>(), s); })
    .def("Add", &BND_File3dmRenderContentTable::Add, py::arg("render_content"))
    .def("FindIndex", &BND_File3dmRenderContentTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmRenderContentTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmPostEffectTable&, BND_File3dmPostEffect*> >(m, "__PostEffectIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmPostEffectTable&, BND_File3dmPostEffect*> &it) -> PyBNDIterator<BND_File3dmPostEffectTable&, BND_File3dmPostEffect*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmPostEffectTable&, BND_File3dmPostEffect*>::next)
    ;

  py::class_<BND_File3dmPostEffectTable>(m, "File3dmPostEffectTable")
    .def(py::init<>())
    .def(py::init<const BND_File3dmPostEffectTable&>(), py::arg("other"))
    .def("__len__", &BND_File3dmPostEffectTable::Count)
    .def("__getitem__", &BND_File3dmPostEffectTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmPostEffectTable&, BND_File3dmPostEffect*>(s.cast<BND_File3dmPostEffectTable &>(), s); })
    //.def("Add", &BND_File3dmRenderContentTable::Add, py::arg("render_content"))
    .def("FindIndex", &BND_File3dmPostEffectTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmPostEffectTable::FindId, py::arg("id"))
    ;

  py::class_<PyBNDIterator<BND_File3dmDecalTable&, BND_File3dmDecal*> >(m, "__DecalIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmDecalTable&, BND_File3dmDecal*> &it) -> PyBNDIterator<BND_File3dmDecalTable&, BND_File3dmDecal*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmDecalTable&, BND_File3dmDecal*>::next)
    ;

  py::class_<BND_File3dmDecalTable>(m, "File3dmDecalTable")
    .def(py::init<>())
    .def(py::init<const BND_File3dmDecalTable&>(), py::arg("other"))
    .def("__len__", &BND_File3dmDecalTable::Count)
    .def("__getitem__", &BND_File3dmDecalTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmDecalTable&, BND_File3dmDecal*>(s.cast<BND_File3dmDecalTable&>(), s); })
    .def("FindIndex", &BND_File3dmDecalTable::FindIndex, py::arg("index"))
    ;

  py::class_<BND_File3dmMeshModifiers>(m, "File3dmMeshModifiers")
    .def_property_readonly("Displacement", &BND_File3dmMeshModifiers::Displacement)
    .def_property_readonly("EdgeSoftening", &BND_File3dmMeshModifiers::EdgeSoftening)
    .def_property_readonly("Thickening", &BND_File3dmMeshModifiers::Thickening)
    .def_property_readonly("CurvePiping", &BND_File3dmMeshModifiers::CurvePiping)
    .def_property_readonly("ShutLining", &BND_File3dmMeshModifiers::ShutLining)
    .def("CreateDisplacement", &BND_File3dmMeshModifiers::CreateDisplacement)
    .def("CreateEdgeSoftening", &BND_File3dmMeshModifiers::CreateEdgeSoftening)
    .def("CreateThickening ", &BND_File3dmMeshModifiers::CreateThickening)
    .def("CreateCurvePiping", &BND_File3dmMeshModifiers::CreateCurvePiping)
    .def("CreateShutLining ", &BND_File3dmMeshModifiers::CreateShutLining)
    ;

  py::class_<PyBNDIterator<BND_File3dmShutLiningCurveTable&, BND_File3dmShutLiningCurve*> >(m, "__ShutLiningCurveIterator")
    .def("__iter__", [](PyBNDIterator<BND_File3dmShutLiningCurveTable&, BND_File3dmShutLiningCurve*> &it) -> PyBNDIterator<BND_File3dmShutLiningCurveTable&, BND_File3dmShutLiningCurve*>& { return it; })
    .def("__next__", &PyBNDIterator<BND_File3dmShutLiningCurveTable&, BND_File3dmShutLiningCurve*>::next)
    ;

  py::class_<BND_File3dmShutLiningCurveTable>(m, "File3dmShutLiningCurveTable")
    .def("__len__", &BND_File3dmShutLiningCurveTable::Count)
    .def("__getitem__", &BND_File3dmShutLiningCurveTable::FindIndex)
    .def("__iter__", [](py::object s) { return PyBNDIterator<BND_File3dmShutLiningCurveTable&, BND_File3dmShutLiningCurve*>(s.cast<BND_File3dmShutLiningCurveTable&>(), s); })
    .def("Add", &BND_File3dmShutLiningCurveTable::Add, py::arg("id"))
    .def("FindIndex", &BND_File3dmShutLiningCurveTable::FindIndex, py::arg("index"))
    .def("FindId", &BND_File3dmShutLiningCurveTable::FindId, py::arg("id"))
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
    .def_property_readonly("ArchiveVersion", &BND_ONXModel::GetArchiveVersion)
    .def_property_readonly("Created", &BND_ONXModel::GetCreated)
    .def_property_readonly("CreatedBy", &BND_ONXModel::GetCreatedBy)
    .def_property_readonly("LastEdited", &BND_ONXModel::GetLastEdited)
    .def_property_readonly("LastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .def_property("Revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .def_property_readonly("Settings", &BND_ONXModel::Settings)
    .def_property_readonly("Objects", &BND_ONXModel::Objects)
    .def_property_readonly("Materials", &BND_ONXModel::Materials)
    .def_property_readonly("Linetypes", &BND_ONXModel::Linetypes)
    .def_property_readonly("Bitmaps", &BND_ONXModel::Bitmaps)
    .def_property_readonly("Layers", &BND_ONXModel::Layers)
    .def_property_readonly("Groups", &BND_ONXModel::AllGroups)
    .def_property_readonly("DimStyles", &BND_ONXModel::DimStyles)
    .def_property_readonly("InstanceDefinitions", &BND_ONXModel::InstanceDefinitions)
    .def_property_readonly("Views", &BND_ONXModel::Views)
    .def_property_readonly("NamedViews", &BND_ONXModel::NamedViews)
    .def_property_readonly("PlugInData", &BND_ONXModel::PlugInData)
    .def_property_readonly("Strings", &BND_ONXModel::Strings)
    .def_property_readonly("EmbeddedFiles", &BND_ONXModel::EmbeddedFiles)
    .def_property_readonly("RenderContent", &BND_ONXModel::RenderContent)
    .def("Encode", &BND_ONXModel::Encode)
    .def("Encode", &BND_ONXModel::Encode2)
    .def("Decode", &BND_ONXModel::Decode)
    .def("EmbeddedFilePaths", &BND_ONXModel::GetEmbeddedFilePaths)
    .def("GetEmbeddedFileAsBase64", &BND_ONXModel::GetEmbeddedFileAsBase64)
    .def("GetEmbeddedFileAsBase64", &BND_ONXModel::GetEmbeddedFileAsBase64Strict)
    .def("RdkXml", &BND_ONXModel::RdkXml)
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
    .property("count", &BND_File3dmPlugInDataTable::Count)
    .function("get", &BND_File3dmPlugInDataTable::GetPlugInData, allow_raw_pointers())
    ;

  class_<BND_FileObject>("File3dmObject")
    .function("attributes", &BND_FileObject::GetAttributes, allow_raw_pointers())
    .function("geometry", &BND_FileObject::GetGeometry, allow_raw_pointers())
    ;

  class_<BND_ONXModel_ObjectTable>("File3dmObjectTable")
    .property("count", &BND_ONXModel_ObjectTable::Count)
    .function("get", &BND_ONXModel_ObjectTable::ModelObjectAt, allow_raw_pointers())
    .function("addPointXYZ", &BND_ONXModel_ObjectTable::AddPoint1)
    .function("addPoint", &BND_ONXModel_ObjectTable::AddPoint2)
    .function("addPointCloud", &BND_ONXModel_ObjectTable::AddPointCloud, allow_raw_pointers())
    .function("addLine", &BND_ONXModel_ObjectTable::AddLine1)
    .function("addPolyline", &BND_ONXModel_ObjectTable::AddPolyline, allow_raw_pointers())
    .function("addArc", &BND_ONXModel_ObjectTable::AddArc, allow_raw_pointers())
    .function("addCircle", &BND_ONXModel_ObjectTable::AddCircle, allow_raw_pointers())
    .function("addEllipse", &BND_ONXModel_ObjectTable::AddEllipse, allow_raw_pointers())
    .function("addSphere", &BND_ONXModel_ObjectTable::AddSphere, allow_raw_pointers())
    .function("addCurve", &BND_ONXModel_ObjectTable::AddCurve, allow_raw_pointers())
    .function("addTextDot", &BND_ONXModel_ObjectTable::AddTextDot, allow_raw_pointers())
    //.function("addSurface", &BND_ONXModel_ObjectTable::AddSurface, allow_raw_pointers())
    .function("addExtrusion", &BND_ONXModel_ObjectTable::AddExtrusion, allow_raw_pointers())
    .function("addMesh", &BND_ONXModel_ObjectTable::AddMesh, allow_raw_pointers())
    .function("addBrep", &BND_ONXModel_ObjectTable::AddBrep, allow_raw_pointers())
    .function("add", &BND_ONXModel_ObjectTable::Add, allow_raw_pointers())
    .function("addObject", &BND_ONXModel_ObjectTable::AddObject, allow_raw_pointers())
    //.function("addInstanceObject", &BND_ONXModel_ObjectTable::AddInstanceObject2, allow_raw_pointers())
    .function("getBoundingBox", &BND_ONXModel_ObjectTable::GetBoundingBox)
    .function("deleteItem", &BND_ONXModel_ObjectTable::Delete)
    .function("findId", &BND_ONXModel_ObjectTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmMaterialTable>("File3dmMaterialTable")
    .property("count", &BND_File3dmMaterialTable::Count)
    .function("get", &BND_File3dmMaterialTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmMaterialTable::Add)
    .function("findIndex", &BND_File3dmMaterialTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmMaterialTable::FindId, allow_raw_pointers())
    .function("findFromAttributes", &BND_File3dmMaterialTable::FromAttributes, allow_raw_pointers())
    ;

  class_<BND_File3dmLinetypeTable>("File3dmLinetypeTable")
    .property("count", &BND_File3dmLinetypeTable::Count)
    .function("get", &BND_File3dmLinetypeTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmLinetypeTable::Add)
    .function("delete", &BND_File3dmLinetypeTable::Delete)
    .function("findIndex", &BND_File3dmLinetypeTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmLinetypeTable::FindId, allow_raw_pointers())
    .function("findName", &BND_File3dmLinetypeTable::FindName, allow_raw_pointers())
    .function("fromAttributes", &BND_File3dmLinetypeTable::FromAttributes, allow_raw_pointers())
    .function("fromLayerIndex", &BND_File3dmLinetypeTable::FromLayerIndex, allow_raw_pointers())
    ;

  class_<BND_File3dmBitmapTable>("File3dmBitmapTable")
    .property("count", &BND_File3dmBitmapTable::Count)
    .function("get", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmBitmapTable::Add)
    .function("findIndex", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmBitmapTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmLayerTable>("File3dmLayerTable")
    .property("count", &BND_File3dmLayerTable::Count)
    .function("get", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmLayerTable::Add)
    .function("addLayer", &BND_File3dmLayerTable::AddLayer)
    .function("findName", &BND_File3dmLayerTable::FindName, allow_raw_pointers())
    .function("findIndex", &BND_File3dmLayerTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmLayerTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmGroupTable>("File3dmGroupTable")
    .property("count", &BND_File3dmGroupTable::Count)
    .function("get", &BND_File3dmGroupTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmGroupTable::Add)
    .function("delete", &BND_File3dmGroupTable::Delete)
    .function("deleteIndex", &BND_File3dmGroupTable::DeleteIndex)
    .function("deleteId", &BND_File3dmGroupTable::DeleteId)
    .function("findIndex", &BND_File3dmGroupTable::FindIndex, allow_raw_pointers())
    .function("findName", &BND_File3dmGroupTable::FindName, allow_raw_pointers())
    ;

  class_<BND_File3dmDimStyleTable>("File3dmDimStyleTable")
    .property("count", &BND_File3dmDimStyleTable::Count)
    .function("get", &BND_File3dmDimStyleTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmDimStyleTable::Add)
    .function("findIndex", &BND_File3dmDimStyleTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmDimStyleTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmInstanceDefinitionTable>("File3dmInstanceDefinitionTable")
    .property("count", &BND_File3dmInstanceDefinitionTable::Count)
    .function("get", &BND_File3dmInstanceDefinitionTable::FindIndex, allow_raw_pointers())
    .function("addInstanceDefinition", &BND_File3dmInstanceDefinitionTable::AddInstanceDefinition)
    .function("add", &BND_File3dmInstanceDefinitionTable::Add, allow_raw_pointers())
    .function("findIndex", &BND_File3dmInstanceDefinitionTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmInstanceDefinitionTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmViewTable>("File3dmViewTable")
    .property("count", &BND_File3dmViewTable::Count)
    .function("get", &BND_File3dmViewTable::GetItem, allow_raw_pointers())
    .function("set", &BND_File3dmViewTable::SetItem)
    .function("add", &BND_File3dmViewTable::Add)
    ;

  class_<BND_File3dmStringTable>("File3dmStringTable")
    .property("count", &BND_File3dmStringTable::Count)
    .function("get", &BND_File3dmStringTable::GetKeyValue)
    .function("getvalue", &BND_File3dmStringTable::GetValueFromKey)
    .function("set", &BND_File3dmStringTable::SetString)
    .function("documentUserTextCount", &BND_File3dmStringTable::DocumentUserTextCount)
    .function("delete", &BND_File3dmStringTable::Delete)
    ;

  class_<BND_File3dmWriteOptions>("File3dmWriteOptions")
    .constructor<>()
    .property("version", &BND_File3dmWriteOptions::GetVersion, &BND_File3dmWriteOptions::SetVersion)
    .property("saveUserData", &BND_File3dmWriteOptions::SaveUserData, &BND_File3dmWriteOptions::SetSaveUserData)
    ;

  class_<BND_File3dmEmbeddedFileTable>("File3dmEmbeddedFileTable")
    .property("count", &BND_File3dmEmbeddedFileTable::Count)
    .function("get", &BND_File3dmEmbeddedFileTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmEmbeddedFileTable::Add)
    .function("findIndex", &BND_File3dmEmbeddedFileTable::FindIndex, allow_raw_pointers())
    //.function("findId", &BND_File3dmEmbeddedFileTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmPostEffectTable>("File3dmPostEffectTable")
    .property("count", &BND_File3dmPostEffectTable::Count)
    .function("get", &BND_File3dmPostEffectTable::FindIndex, allow_raw_pointers())
    //.function("add", &BND_File3dmPostEffectTable::Add, allow_raw_pointers())
    .function("findIndex", &BND_File3dmPostEffectTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmPostEffectTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmDecalTable>("File3dmDecalTable")
    .property("count", &BND_File3dmDecalTable::Count)
    .function("get", &BND_File3dmDecalTable::FindIndex, allow_raw_pointers())
    .function("findIndex", &BND_File3dmDecalTable::FindIndex, allow_raw_pointers())
    ;

  class_<BND_File3dmMeshModifiers>("File3dmMeshModifiers")
    .function("displacement", &BND_File3dmMeshModifiers::Displacement, allow_raw_pointers())
    .function("edgeSoftening", &BND_File3dmMeshModifiers::EdgeSoftening, allow_raw_pointers())
    .function("thickening", &BND_File3dmMeshModifiers::Thickening, allow_raw_pointers())
    .function("curvePiping", &BND_File3dmMeshModifiers::CurvePiping, allow_raw_pointers())
    .function("shutLining", &BND_File3dmMeshModifiers::ShutLining, allow_raw_pointers())
    .function("createDisplacement", &BND_File3dmMeshModifiers::CreateDisplacement)
    .function("createEdgeSoftening", &BND_File3dmMeshModifiers::CreateEdgeSoftening)
    .function("createThickening ", &BND_File3dmMeshModifiers::CreateThickening)
    .function("createCurvePiping", &BND_File3dmMeshModifiers::CreateCurvePiping)
    .function("createShutLining ", &BND_File3dmMeshModifiers::CreateShutLining)
    ;

  class_<BND_File3dmShutLiningCurveTable>("File3dmShutLiningCurveTable")
    .property("count", &BND_File3dmShutLiningCurveTable::Count)
    .function("get", &BND_File3dmShutLiningCurveTable::FindIndex, allow_raw_pointers())
    .function("add", &BND_File3dmShutLiningCurveTable::Add)
    .function("findIndex", &BND_File3dmShutLiningCurveTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmShutLiningCurveTable::FindId, allow_raw_pointers())
    ;

  class_<BND_File3dmRenderContentTable>("File3dmRenderContentTable")
    .property("count", &BND_File3dmRenderContentTable::Count)
    .function("add", &BND_File3dmRenderContentTable::Add)
    .function("get", &BND_File3dmRenderContentTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmRenderContentTable::FindId, allow_raw_pointers())
    ;

  class_<BND_ONXModel>("File3dm")
    .constructor<>()
    .function("destroy", &BND_ONXModel::Destroy)
    .class_function("fromByteArray", &BND_ONXModel::WasmFromByteArray, allow_raw_pointers())
    .property("startSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments)
    .property("applicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    .property("applicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    .property("applicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    .property("archiveVersion", &BND_ONXModel::GetArchiveVersion)
    .property("created", &BND_ONXModel::GetCreated)
    .property("createdBy", &BND_ONXModel::GetCreatedBy)
    .property("lastEdited", &BND_ONXModel::GetLastEdited)
    .property("lastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .property("revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .function("settings", &BND_ONXModel::Settings)
    .function("objects", &BND_ONXModel::Objects)
    .function("materials", &BND_ONXModel::Materials)
    .function("linetypes", &BND_ONXModel::Linetypes)
    .function("bitmaps", &BND_ONXModel::Bitmaps)
    .function("layers", &BND_ONXModel::Layers)
    .function("groups", &BND_ONXModel::AllGroups)
    .function("dimstyles", &BND_ONXModel::DimStyles)
    .function("instanceDefinitions", &BND_ONXModel::InstanceDefinitions)
    .function("views", &BND_ONXModel::Views)
    .function("namedViews", &BND_ONXModel::NamedViews)
    .function("plugInData", &BND_ONXModel::PlugInData)
    .function("strings", &BND_ONXModel::Strings)
    .function("embeddedFiles", &BND_ONXModel::EmbeddedFiles)
    .function("renderContent", &BND_ONXModel::RenderContent)
    .function("encode", &BND_ONXModel::Encode)
    .function("encodeOptions", &BND_ONXModel::Encode2, allow_raw_pointers())
    .function("toByteArray", &BND_ONXModel::ToByteArray)
    .function("toByteArrayOptions", &BND_ONXModel::ToByteArray2, allow_raw_pointers())
    .class_function("decode", &BND_ONXModel::Decode, allow_raw_pointers())
    .function("embeddedFilePaths", &BND_ONXModel::GetEmbeddedFilePaths)
    .function("getEmbeddedFileAsBase64", &BND_ONXModel::GetEmbeddedFileAsBase64)
    .function("getEmbeddedFileAsBase64Strict", &BND_ONXModel::GetEmbeddedFileAsBase64Strict)
    .function("rdkXml", &BND_ONXModel::RdkXml)
    ;
}
#endif
