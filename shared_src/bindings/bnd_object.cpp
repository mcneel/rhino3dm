#include "bindings.h"
#include "base64.h"

BND_Object::BND_Object()
{
}

BND_Object::~BND_Object()
{
  if (m_object && m_component_ref.IsEmpty())
    delete m_object;
}

BND_Object::BND_Object(ON_Object* obj, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(obj, compref);
}

void BND_Object::SetTrackedPointer(ON_Object* obj, const ON_ModelComponentReference* compref)
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

BND_Object* BND_Object::CreateWrapper(ON_Object* obj, const ON_ModelComponentReference* compref)
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
      return new BND_Curve(curve, compref);
    }

    ON_Viewport* viewport = ON_Viewport::Cast(obj);
    if( viewport )
      return new BND_Viewport(viewport, compref);

    return new BND_Geometry(geometry, compref);
  }
  return new BND_Object(obj, compref);
}

BND_Object* BND_Object::CreateWrapper(const ON_ModelComponentReference& compref)
{
  ON_Object* obj = const_cast<ON_ModelComponent*>(compref.ModelComponent());
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
emscripten::val BND_Object::Encode() const
{
  emscripten::val v(emscripten::val::object());
  v.set("version", emscripten::val(10000));
  const int rhinoversion = 60;
  v.set("archive3dm", emscripten::val(rhinoversion));
  unsigned int on_version__to_write = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(rhinoversion, ON::Version());
  v.set("opennurbs", emscripten::val((int)on_version__to_write));

  unsigned int length=0;
  ON_Write3dmBufferArchive* archive = ON_WriteBufferArchive_NewWriter(m_object.get(), 60, true, &length);
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
#endif
#if defined(ON_PYTHON_COMPILE)

pybind11::dict BND_Object::Encode() const
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
BND_Object* BND_Object::Decode(emscripten::val jsonObject)
{
  std::string buffer = jsonObject["data"].as<std::string>();
  std::string decoded = base64_decode(buffer);
  int rhinoversion = jsonObject["archive3dm"].as<int>();
  int opennurbsversion = jsonObject["opennurbs"].as<int>();
  int length = decoded.length();
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  ON_Object* obj = ON_ReadBufferArchive(rhinoversion, opennurbsversion, length, c);
  return CreateWrapper(obj);
}
#endif

#if defined(ON_PYTHON_COMPILE)

BND_Object* BND_Object::Decode(pybind11::dict jsonObject)
{
  std::string buffer = pybind11::str(jsonObject["data"]);
  std::string decoded = base64_decode(buffer);
  int rhinoversion = jsonObject["archive3dm"].cast<int>();
  int opennurbsversion = jsonObject["opennurbs"].cast<int>();
  int length = decoded.length();
  const unsigned char* c = (const unsigned char*)&decoded.at(0);
  ON_Object* obj = ON_ReadBufferArchive(rhinoversion, opennurbsversion, length, c);
  return CreateWrapper(obj, nullptr);
}
#endif

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initObjectBindings(pybind11::module& m)
{
  py::class_<BND_Object>(m, "CommonObject")
    .def("Encode", &BND_Object::Encode)
    .def_static("Decode", &BND_Object::Decode);
}
#endif
