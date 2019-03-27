#include "bindings.h"

BND_FileReference BND_FileReference::CreateFromFullPath(std::wstring path)
{
  BND_FileReference rc;
  rc.m_fileref.SetFullPath(path.c_str(), true);
  return rc;
}

BND_FileReference BND_FileReference::CreateFromFullAndRelativePaths(std::wstring fullPath, std::wstring relativePath)
{
  BND_FileReference rc;
  rc.m_fileref.SetFullPath(fullPath.c_str(), true);
  rc.m_fileref.SetRelativePath(relativePath.c_str());
  return rc;
}



//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initFileUtilitiesBindings(pybind11::module& m)
{
  py::class_<BND_FileReference>(m, "FileReference")
    .def_static("CreateFromFullPath", &BND_FileReference::CreateFromFullPath, py::arg("path"))
    .def_static("CreateFromFullAndRelativePaths", &BND_FileReference::CreateFromFullAndRelativePaths, py::arg("fullPath"), py::arg("relativePath"))
    .def_property_readonly("FullPath", &BND_FileReference::GetFullPath)
    .def_property_readonly("RelativePath", &BND_FileReference::GetRelativePath)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initFileUtilitiesBindings(void*)
{
  class_<BND_FileReference>("FileReference")
    .class_function("createFromFullPath", &BND_FileReference::CreateFromFullPath)
    .class_function("createFromFullAndRelativePaths", &BND_FileReference::CreateFromFullAndRelativePaths)
    .property("fullPath", &BND_FileReference::GetFullPath)
    .property("relativePath", &BND_FileReference::GetRelativePath)
    ;
}
#endif
