
#include "bindings.h"

BND_File3dmEmbeddedFile::BND_File3dmEmbeddedFile()
{
  SetTrackedPointer(new ON_EmbeddedFile, nullptr);
}

BND_File3dmEmbeddedFile::BND_File3dmEmbeddedFile(const BND_File3dmEmbeddedFile& ef)
{
  SetTrackedPointer(new ON_EmbeddedFile(*ef._ef), nullptr);
}

BND_File3dmEmbeddedFile::BND_File3dmEmbeddedFile(ON_EmbeddedFile* ef, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(ef, compref);
}

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile::Read(const std::wstring& f) // Static.
{
  auto* ef = new ON_EmbeddedFile;
  if (!ef->LoadFromFile(f.c_str()))
  {
    delete ef;
    return nullptr;
  }

  return new BND_File3dmEmbeddedFile(ef, nullptr);
}

void BND_File3dmEmbeddedFile::SetTrackedPointer(ON_EmbeddedFile* ef, const ON_ModelComponentReference* compref)
{
  _ef = ef;

  BND_ModelComponent::SetTrackedPointer(ef, compref);
}

std::wstring BND_File3dmEmbeddedFile::GetFilename(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_ef->Filename()));
}

size_t BND_File3dmEmbeddedFile::GetLength(void) const
{
  return _ef->Length();
}

bool BND_File3dmEmbeddedFile::Write(const std::wstring& f) const
{
  return _ef->SaveToFile(f.c_str());
}

bool BND_File3dmEmbeddedFile::Clear(void) const
{
  return _ef->Clear();
}
void BND_File3dmEmbeddedFileTable::Add(const BND_File3dmEmbeddedFile& ef)
{
  if (nullptr != ef._ef)
  {
    m_model->AddModelComponent(*ef._ef);
  }
}

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFileTable::FindIndex(int index)
{
  ON_ModelComponentReference compref = m_model->ComponentFromIndex(ON_ModelComponent::Type::EmbeddedFile, index);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_EmbeddedFile* model_ef = const_cast<ON_EmbeddedFile*>(ON_EmbeddedFile::Cast(model_component));
  if (nullptr != model_ef)
    return new BND_File3dmEmbeddedFile(model_ef, &compref);

  return nullptr;
}

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFileTable::IterIndex(int index)
{
  return FindIndex(index);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initEmbeddedFileBindings(pybind11::module& m)
{
  py::class_<BND_File3dmEmbeddedFile>(m, "EmbeddedFile")
    .def(py::init<>())
    .def(py::init<const BND_File3dmEmbeddedFile&>(), py::arg("other"))
    .def_static("Read", &BND_File3dmEmbeddedFile::Read, py::arg("filename"))
    .def_property_readonly("Length", &BND_File3dmEmbeddedFile::GetLength)
    .def_property_readonly("Filename", &BND_File3dmEmbeddedFile::GetFilename)
    .def("Write", &BND_File3dmEmbeddedFile::Write, py::arg("filename"))
    .def("Clear", &BND_File3dmEmbeddedFile::Clear)
    ;
}
#endif

#if defined(ON_WASM_COMPILE__TEMP)
using namespace emscripten;

void initEmbeddedFileBindings(void*)
{
  class_<BND_File3dmEmbeddedFile>("EmbeddedFile")
    .constructor<>()
    .constructor<const BND_File3dmEmbeddedFile&>()
    .class_function("read", &BND_File3dmEmbeddedFile::Read, allow_raw_pointers())
    .property("length", &BND_File3dmEmbeddedFile::GetLength)
    .property("filename", &BND_File3dmEmbeddedFile::GetFilename, &BND_File3dmEmbeddedFile::SetFilename)
    .function("write", &BND_File3dmEmbeddedFile::SaveToFile, allow_raw_pointers())
    .function("clear", &BND_File3dmEmbeddedFile::Clear)
    ;
}
#endif
