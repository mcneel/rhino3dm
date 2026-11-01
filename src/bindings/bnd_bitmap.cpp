#include "bnd_bitmap.h"
#include "bnd_file3dm_gen.h"

static const ON_Bitmap* RH3DM_FileImage(const ONX_Model* model, ON_UUID id)
{
  if (nullptr == model)
    return nullptr;
  ON_ModelComponentReference ref = model->ImageFromId(id);
  return ON_Bitmap::Cast(ref.ModelComponent());
}

ON_Bitmap* BND_Bitmap::RH3DM_Const() const
{
  if (m_ptr)
    return m_ptr;
  const ON_Bitmap* p = RH3DM_FileImage(m_model, m_id);
  if (nullptr == p)
    throw std::runtime_error("Bitmap: the image is no longer in the File3dm");
  return const_cast<ON_Bitmap*>(p);
}

std::wstring BND_Bitmap::FileFullPath() const
{
  return std::wstring(static_cast<const wchar_t*>(RH3DM_Const()->FileReference().FullPath()));
}

void BND_Bitmap::SetFileFullPath(std::wstring path)
{
  // the file keys its images by path (ON_NameHash::CreateFilePathHash): renaming one in
  // place would leave the manifest stale -- set the path before Add
  if (nullptr == m_ptr)
    throw std::runtime_error("Bitmap: the path of an image in a File3dm cannot be changed");
  m_ptr->SetFileFullPath(path.c_str(), true);
}

int BND_File3dmBitmapTable::Count() const
{
  return (int)m_model->ActiveComponentCount(ON_ModelComponent::Type::Image);
}

BND_Bitmap* BND_File3dmBitmapTable::Add(const BND_Bitmap& bitmap)
{
  // the file's copy (a view), or null when rejected: ONX_Model adds a copy
  // (Duplicate) keyed by the bitmap's file path
  ON_ModelComponentReference ref = m_model->AddModelComponent(*bitmap.RH3DM_Const());
  const ON_Bitmap* added = ON_Bitmap::Cast(ref.ModelComponent());
  return added ? new BND_Bitmap(m_model, added->Id()) : nullptr;
}

bool BND_File3dmBitmapTable::Delete(BND_UUID id)
{
  ON_ModelComponentReference ref = m_model->RemoveModelComponent(ON_ModelComponent::Type::Image, Binding_to_ON_UUID(id));
  return !ref.IsEmpty();
}

BND_Bitmap* BND_File3dmBitmapTable::FindIndex(int index) const
{
  // images carry no model index (ON_ModelComponent::IndexRequired(Image) is false, so
  // ImageFromIndex never finds one): the index-th live image, in manifest order
  if (index < 0)
    return nullptr;
  ONX_ModelComponentIterator it(*m_model, ON_ModelComponent::Type::Image);
  int i = 0;
  for (ON_ModelComponentReference ref = it.FirstComponentReference(); !ref.IsEmpty(); ref = it.NextComponentReference())
  {
    const ON_Bitmap* p = ON_Bitmap::Cast(ref.ModelComponent());
    if (p && !p->IsDeleted() && i++ == index)
      return new BND_Bitmap(m_model, p->Id());
  }
  return nullptr;
}

BND_Bitmap* BND_File3dmBitmapTable::FindId(BND_UUID id) const
{
  ON_UUID uuid = Binding_to_ON_UUID(id);
  return RH3DM_FileImage(m_model, uuid) ? new BND_Bitmap(m_model, uuid) : nullptr;
}

BND_Bitmap* BND_File3dmBitmapTable::GetItem(int index) const
{
  BND_Bitmap* p = FindIndex(index);
  if (nullptr == p)
    throw std::out_of_range("index");
  return p;
}

BND_File3dmBitmapTableIter* BND_File3dmBitmapTable::Iter() const
{
  return new BND_File3dmBitmapTableIter(m_model);
}

BND_Bitmap* BND_File3dmBitmapTableIter::Next()
{
  ON_ModelComponentReference ref = m_started ? m_it.NextComponentReference() : m_it.FirstComponentReference();
  m_started = true;
  for (; !ref.IsEmpty(); ref = m_it.NextComponentReference())
  {
    const ON_Bitmap* p = ON_Bitmap::Cast(ref.ModelComponent());
    if (p && !p->IsDeleted())
      return new BND_Bitmap(m_model, p->Id());
  }
  return nullptr;
}

BND_File3dmBitmapTable BND_File3dm_Bitmaps(const BND_File3dm& file)
{
  return BND_File3dmBitmapTable(file.m_ptr);
}

#if defined(ON_PYTHON_COMPILE)

void initBitmapBindings(rh3dmpymodule& m)
{
  py::class_<BND_Bitmap, BND_ModelComponent>(m, "Bitmap")
    .def(py::init<>())
    // user strings, as the old Bitmap had them (D62): RhinoCommon's internal CommonObject members
    .def("SetUserString", &BND_Bitmap::Gen__SetUserString, py::arg("key"), py::arg("value"))
    .def("GetUserString", &BND_Bitmap::Gen__GetUserString, py::arg("key"))
    .def_property_readonly("UserStringCount", &BND_Bitmap::Gen__UserStringCount)
    .def("GetUserStrings", [](const BND_Bitmap& s) { return RH3DM_UserStrings(s.RH3DM_ObjPtr()); })
    .def_property_readonly("Width", &BND_Bitmap::Width)
    .def_property_readonly("Height", &BND_Bitmap::Height)
    .def_property_readonly("BitsPerPixel", &BND_Bitmap::BitsPerPixel)
    .def_property_readonly("SizeOfScan", &BND_Bitmap::SizeOfScan)
    .def_property_readonly("SizeOfImage", &BND_Bitmap::SizeOfImage)
    .def_property("FileFullPath", &BND_Bitmap::FileFullPath, &BND_Bitmap::SetFileFullPath)
    ;

  py::class_<BND_File3dmBitmapTable>(m, "File3dmBitmapTable")
    .def("__len__", &BND_File3dmBitmapTable::Count)
    .def_property_readonly("Count", &BND_File3dmBitmapTable::Count)
    .def("__getitem__", [](const BND_File3dmBitmapTable& t, int index) {
        try { return t.GetItem(index); }
        catch (const std::out_of_range&) { throw py::index_error(); }
      }, py::keep_alive<0, 1>(), py::arg("index"))
    .def("__iter__", &BND_File3dmBitmapTable::Iter, py::keep_alive<0, 1>())
    .def("Add", &BND_File3dmBitmapTable::Add, py::keep_alive<0, 1>(), py::arg("bitmap"))
    .def("Delete", &BND_File3dmBitmapTable::Delete, py::arg("id"))
    .def("FindIndex", &BND_File3dmBitmapTable::FindIndex, py::keep_alive<0, 1>(), py::arg("index"))
    .def("FindId", &BND_File3dmBitmapTable::FindId, py::keep_alive<0, 1>(), py::arg("id"))
    ;

  py::class_<BND_File3dmBitmapTableIter>(m, "File3dmBitmapTableIter")
    .def("__iter__", [](py::object self) { return self; })
    .def("__next__", [](BND_File3dmBitmapTableIter& it) {
        BND_Bitmap* p = it.Next();
        if (!p) throw py::stop_iteration();
        return p;
      }, py::keep_alive<0, 1>())
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBitmapBindings(void)
{
  class_<BND_Bitmap, base<BND_ModelComponent>>("Bitmap")
    .constructor<>()
    .function("setUserString", &BND_Bitmap::Gen__SetUserString)
    .function("getUserString", &BND_Bitmap::Gen__GetUserString)
    .property("userStringCount", &BND_Bitmap::Gen__UserStringCount)
    .function("getUserStrings", optional_override([](const BND_Bitmap& s) { return RH3DM_UserStrings(s.RH3DM_ObjPtr()); }))
    .property("width", &BND_Bitmap::Width)
    .property("height", &BND_Bitmap::Height)
    .property("bitsPerPixel", &BND_Bitmap::BitsPerPixel)
    .property("sizeOfScan", &BND_Bitmap::SizeOfScan)
    .property("sizeOfImage", &BND_Bitmap::SizeOfImage)
    .property("fileFullPath", &BND_Bitmap::FileFullPath, &BND_Bitmap::SetFileFullPath)
    ;

  class_<BND_File3dmBitmapTable>("File3dmBitmapTable")
    .property("count", &BND_File3dmBitmapTable::Count)
    .function("add", &BND_File3dmBitmapTable::Add, allow_raw_pointers())
    .function("delete", &BND_File3dmBitmapTable::Delete)
    .function("get", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("findIndex", &BND_File3dmBitmapTable::FindIndex, allow_raw_pointers())
    .function("findId", &BND_File3dmBitmapTable::FindId, allow_raw_pointers())
    .function("iterator", &BND_File3dmBitmapTable::Iter, allow_raw_pointers())
    ;

  class_<BND_File3dmBitmapTableIter>("File3dmBitmapTableIter")
    .function("next", optional_override([](BND_File3dmBitmapTableIter& it) -> emscripten::val {
        emscripten::val r = emscripten::val::object();
        BND_Bitmap* p = it.Next();
        r.set("done", p == nullptr);
        if (p) r.set("value", emscripten::val(p, emscripten::allow_raw_pointers()));
        return r;
      }), allow_raw_pointers())
    ;
}

#endif
