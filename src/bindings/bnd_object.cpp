#include "bindings.h"
#include "base64.h"

BND_CommonObject::BND_CommonObject()
{
}

BND_CommonObject::~BND_CommonObject()
{
  // m_component_ref should almost always track the lifetime of the ON_Object
  if (m_object && m_component_ref.IsEmpty())
    delete m_object;
}

BND_CommonObject::BND_CommonObject(ON_Object* obj, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(obj, compref);
}

void BND_CommonObject::SetTrackedPointer(ON_Object* obj, const ON_ModelComponentReference* compref)
{
  if( compref )
  {
    m_component_ref = *compref;
  }
  else
  {
    ON_ModelComponent* model_component = ON_ModelComponent::Cast(obj);
    if (model_component)
      m_component_ref = ON_ModelComponentReference::CreateForExperts(model_component, true);
  }
  m_object = obj;
}

BND_CommonObject* BND_CommonObject::CreateWrapper(ON_Object* obj, const ON_ModelComponentReference* compref)
{
  if( nullptr == obj )
    return nullptr;

  ON_Geometry* geometry = ON_Geometry::Cast(obj);
  if( geometry )
  {
    ON_Mesh* mesh = ON_Mesh::Cast(obj);
    if( mesh )
      return new BND_Mesh(mesh, compref);
    ON_Brep* brep = ON_Brep::Cast(obj);
    if( brep )
      return new BND_Brep(brep, compref);
    ON_Curve* curve = ON_Curve::Cast(obj);
    if(curve)
    {
      ON_NurbsCurve* nc = ON_NurbsCurve::Cast(obj);
      if( nc )
        return new BND_NurbsCurve(nc, compref);
      ON_LineCurve* lc = ON_LineCurve::Cast(obj);
      if( lc )
        return new BND_LineCurve(lc, compref);
      ON_PolylineCurve* plc = ON_PolylineCurve::Cast(obj);
      if( plc )
        return new BND_PolylineCurve(plc, compref);
      ON_PolyCurve* pc = ON_PolyCurve::Cast(obj);
      if( pc )
        return new BND_PolyCurve(pc, compref);
      ON_ArcCurve* ac = ON_ArcCurve::Cast(obj);
      if (ac)
        return new BND_ArcCurve(ac, compref);
      ON_CurveProxy* proxy = ON_CurveProxy::Cast(obj);
      if (proxy)
        return new BND_CurveProxy(proxy, compref);
      return new BND_Curve(curve, compref);
    }

    ON_Surface* surface = ON_Surface::Cast(obj);
    if (surface)
    {
      ON_NurbsSurface* ns = ON_NurbsSurface::Cast(obj);
      if (ns)
        return new BND_NurbsSurface(ns, compref);
      ON_Extrusion* extr = ON_Extrusion::Cast(obj);
      if (extr)
        return new BND_Extrusion(extr, compref);
      ON_SurfaceProxy* proxy = ON_SurfaceProxy::Cast(obj);
      if (proxy)
      {
        ON_BrepFace* brepface = ON_BrepFace::Cast(obj);
        if (brepface)
          return new BND_BrepFace(brepface, compref);
        return new BND_SurfaceProxy(proxy, compref);
      }
      ON_RevSurface* revsrf = ON_RevSurface::Cast(obj);
      if (revsrf)
        return new BND_RevSurface(revsrf, compref);
      return new BND_Surface(surface, compref);
    }

    ON_Point* point = ON_Point::Cast(obj);
    if (point)
      return new BND_Point(point, compref);

    ON_PointCloud* pointcloud = ON_PointCloud::Cast(obj);
    if (pointcloud)
      return new BND_PointCloud(pointcloud, compref);

    ON_PointGrid* pointgrid = ON_PointGrid::Cast(obj);
    if (pointgrid)
      return new BND_PointGrid(pointgrid, compref);

    ON_Viewport* viewport = ON_Viewport::Cast(obj);
    if( viewport )
      return new BND_Viewport(viewport, compref);

    ON_Hatch* hatch = ON_Hatch::Cast(obj);
    if (hatch)
      return new BND_Hatch(hatch, compref);

    return new BND_GeometryBase(geometry, compref);
  }

  ON_Material* material = ON_Material::Cast(obj);
  if (material)
    return new BND_Material(material, compref);

  ON_Layer* layer = ON_Layer::Cast(obj);
  if (layer)
    return new BND_Layer(layer, compref);

  return new BND_CommonObject(obj, compref);
}

BND_CommonObject* BND_CommonObject::CreateWrapper(const ON_ModelComponentReference& compref)
{
  const ON_ModelComponent* model_component = compref.ModelComponent();
  const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
  if (nullptr == geometryComponent)
    return nullptr;

  ON_Object* obj = const_cast<ON_Geometry*>(geometryComponent->Geometry(nullptr));
  return CreateWrapper(obj, &compref);
}


RH_C_FUNCTION ON_Write3dmBufferArchive* ON_WriteBufferArchive_NewWriter(const ON_Object* pConstObject, int rhinoversion, bool writeuserdata, unsigned int* length)
{
  ON_Write3dmBufferArchive* rc = nullptr;

  if( pConstObject && length )
  {
    ON_UserDataHolder holder;
    if( !writeuserdata )
      holder.MoveUserDataFrom(*pConstObject);
    *length = 0;
    size_t sz = pConstObject->SizeOf() + 512; // 256 was too small on x86 builds to account for extra data written

    // figure out the appropriate version number
    unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(rhinoversion, ON::Version());

    rc = new ON_Write3dmBufferArchive(sz, 0, rhinoversion, on_version__to_write);
    if( rc->WriteObject(pConstObject) )
    {
      *length = (unsigned int)rc->SizeOfArchive();
    }
    else
    {
      delete rc;
      rc = nullptr;
    }
    if( !writeuserdata )
      holder.MoveUserDataTo(*pConstObject, false);
  }
  return rc;
}

#if defined(__EMSCRIPTEN__)
emscripten::val BND_CommonObject::Encode() const
{
  emscripten::val v(emscripten::val::object());
  v.set("version", emscripten::val(10000));
  const int rhinoversion = 60;
  v.set("archive3dm", emscripten::val(rhinoversion));
  unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(rhinoversion, ON::Version());
  v.set("opennurbs", emscripten::val((int)on_version__to_write));

  unsigned int length=0;
  ON_Write3dmBufferArchive* archive = ON_WriteBufferArchive_NewWriter(m_object, 60, true, &length);
  std::string data = "";
  if( length>0 && archive )
  {
    unsigned char* buffer = (unsigned char*)archive->Buffer();
    data = base64_encode(buffer, length);
  }
  if( archive )
    delete archive;

  v.set("data", emscripten::val(data));
  return v;
}

emscripten::val BND_CommonObject::toJSON(emscripten::val key)
{
  return Encode();
}

#endif
#if defined(ON_PYTHON_COMPILE)

pybind11::dict BND_CommonObject::Encode() const
{
  pybind11::dict d;
  d["version"] = 10000;
  const int rhinoversion = 60;
  d["archive3dm"] = rhinoversion;
  unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(rhinoversion, ON::Version());
  d["opennurbs"] = (int)(on_version__to_write);
  unsigned int length=0;
  ON_Write3dmBufferArchive* archive = ON_WriteBufferArchive_NewWriter(m_object, 60, true, &length);
  std::string data = "";
  if( length>0 && archive )
  {
    unsigned char* buffer = (unsigned char*)archive->Buffer();
    data = base64_encode(buffer, length);
  }
  if( archive )
    delete archive;

  d["data"] = data;
  return d;
}
#endif

RH_C_FUNCTION ON_Object* ON_ReadBufferArchive(int archive_3dm_version, unsigned int archive_on_version, int length, /*ARRAY*/const unsigned char* buffer)
{
  // Eliminate potential bogus file versions written
  if (archive_3dm_version > 5 && archive_3dm_version < 50)
    return nullptr;
  ON_Object* rc = nullptr;
  if( length>0 && buffer )
  {
    ON_Read3dmBufferArchive archive(length, buffer, false, archive_3dm_version, archive_on_version);
    archive.ReadObject( &rc );
  }
  return rc;
}

#if defined(__EMSCRIPTEN__)
BND_CommonObject* BND_CommonObject::Decode(emscripten::val jsonObject)
{
  std::string buffer = jsonObject["data"].as<std::string>();
  std::string decoded = base64_decode(buffer);
  int rhinoversion = jsonObject["archive3dm"].as<int>();
  int opennurbsversion = jsonObject["opennurbs"].as<int>();
  int length = decoded.length();
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  ON_Object* obj = ON_ReadBufferArchive(rhinoversion, opennurbsversion, length, c);
  return CreateWrapper(obj, nullptr);
}
#endif


#if defined(ON_PYTHON_COMPILE)

BND_CommonObject* BND_CommonObject::Decode(pybind11::dict jsonObject)
{
  std::string buffer = pybind11::str(jsonObject["data"]);
  std::string decoded = base64_decode(buffer);
  int rhinoversion = jsonObject["archive3dm"].cast<int>();
  int opennurbsversion = jsonObject["opennurbs"].cast<int>();
  int length = static_cast<int>(decoded.length());
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  ON_Object* obj = ON_ReadBufferArchive(rhinoversion, opennurbsversion, length, c);
  return CreateWrapper(obj, nullptr);
}
#endif


bool BND_CommonObject::SetUserString(std::wstring key, std::wstring value)
{
  return m_object->SetUserString(key.c_str(), value.c_str());
}

std::wstring BND_CommonObject::GetUserString(std::wstring key)
{
  ON_wString value;
  if (m_object->GetUserString(key.c_str(), value))
  {
    return std::wstring(value);
  }
  return std::wstring(L"");
}

#if defined(ON_PYTHON_COMPILE)
pybind11::tuple BND_CommonObject::GetUserStrings() const
{
  ON_ClassArray<ON_wString> keys;
  m_object->GetUserStringKeys(keys);
  pybind11::tuple rc(keys.Count());
  for (int i = 0; i < keys.Count(); i++)
  {
    ON_wString sval;
    m_object->GetUserString(keys[i].Array(), sval);
    pybind11::tuple keyval(2);
    keyval[0] = std::wstring(keys[i].Array());
    keyval[1] = std::wstring(sval.Array());
    rc[i] = keyval;
  }
  return rc;
}

std::wstring BND_CommonObject::RdkXml() const
{
  std::wstring rc;
  ON_wString xmlstring;
  if (ONX_Model::GetRDKObjectInformation(*m_object, xmlstring))
  {
    rc = xmlstring.Array();
    return rc;
  }
  return rc;
}

#endif




#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initObjectBindings(pybind11::module& m)
{
  py::class_<BND_CommonObject>(m, "CommonObject")
    .def("Encode", &BND_CommonObject::Encode)
    .def_static("Decode", &BND_CommonObject::Decode)
    .def("SetUserString", &BND_CommonObject::SetUserString)
    .def("GetUserString", &BND_CommonObject::GetUserString)
    .def_property_readonly("UserStringCount", &BND_CommonObject::UserStringCount)
    .def("GetUserStrings", &BND_CommonObject::GetUserStrings)
    .def("RdkXml", &BND_CommonObject::RdkXml)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initObjectBindings(void*)
{
  class_<BND_CommonObject>("CommonObject")
    .function("encode", &BND_CommonObject::Encode)
    .function("toJSON", &BND_CommonObject::toJSON)
    .class_function("decode", &BND_CommonObject::Decode, allow_raw_pointers())
    .function("setUserString", &BND_CommonObject::SetUserString)
    .function("getUserString", &BND_CommonObject::GetUserString)
    .property("userStringCount", &BND_CommonObject::UserStringCount)
    ;
}
#endif
