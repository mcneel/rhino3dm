

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

//#if defined(ON_WASM_COMPILE)
BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile::WasmFromByteArray(std::string sbuffer)
{
  int length = sbuffer.length();
  const void* buffer = sbuffer.c_str();

  ON_Buffer* b = new ON_Buffer;
  b->Write(length, buffer);

  ON_EmbeddedFile* ef = new ON_EmbeddedFile();
  ef->LoadFromBuffer(*b);

  return new BND_File3dmEmbeddedFile(ef, nullptr);
}
//#endif

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

/*
BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFileTable::FindId(BND_UUID id)
{
  const ON_UUID _id = Binding_to_ON_UUID(id);
  ON_ModelComponentReference compref = m_model->ComponentFromId(ON_ModelComponent::Type::EmbeddedFile, _id);
  const ON_ModelComponent* model_component = compref.ModelComponent();
  ON_EmbeddedFile* model_ef = const_cast<ON_EmbeddedFile*>(ON_EmbeddedFile::Cast(model_component));
  if (nullptr != model_ef)
    return new BND_File3dmEmbeddedFile(model_ef, &compref);

  return nullptr;
}
*/

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initEmbeddedFileBindings(pybind11::module& m)
{
  py::class_<BND_File3dmEmbeddedFile>(m, "EmbeddedFile")
    .def(py::init<>())
    .def(py::init<const BND_File3dmEmbeddedFile&>(), py::arg("other"))
    .def_static("Read", &BND_File3dmEmbeddedFile::Read, py::arg("fileName"))
    .def_property_readonly("Length", &BND_File3dmEmbeddedFile::GetLength)
    .def_property_readonly("FileName", &BND_File3dmEmbeddedFile::GetFilename)
    .def("Write", &BND_File3dmEmbeddedFile::Write, py::arg("fileName"))
    .def("Clear", &BND_File3dmEmbeddedFile::Clear)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initEmbeddedFileBindings(void*)
{
  class_<BND_File3dmEmbeddedFile>("EmbeddedFile")
    //commented ctors, read and write 
    //we don't yet have the add method on the embedded file table.
    //reading an embedded file thus makes little sense.
    //writing seems to have issues

    .constructor<>()
    .constructor<const BND_File3dmEmbeddedFile&>()
    .class_function("read", &BND_File3dmEmbeddedFile::Read, allow_raw_pointers())
    .class_function("fromByteArray", &BND_File3dmEmbeddedFile::WasmFromByteArray, allow_raw_pointers())
    .property("length", &BND_File3dmEmbeddedFile::GetLength)
    .property("fileName", &BND_File3dmEmbeddedFile::GetFilename)
    //.function("setFileName", &BND_File3dmEmbeddedFile::SetFilename) //TODO
    .function("write", &BND_File3dmEmbeddedFile::Write, allow_raw_pointers()) //should return some sort of buffer that can be saved with the FileAPI
    .function("clear", &BND_File3dmEmbeddedFile::Clear)
    ;
}
#endif
