#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initFileUtilitiesBindings(py::module_& m);
#else
namespace py = pybind11;
void initFileUtilitiesBindings(py::module& m);
#endif

#else
void initFileUtilitiesBindings(void* m);
#endif

class BND_FileReference
{
  ON_FileReference m_fileref;
public:
  BND_FileReference() = default;
  BND_FileReference(const ON_FileReference& fileref) : m_fileref(fileref) {}
  //public FileReference(string fullPath, string relativePath, ContentHash hash, FileReferenceStatus status);
  static BND_FileReference CreateFromFullPath(std::wstring fullPath);
  static BND_FileReference CreateFromFullAndRelativePaths(std::wstring fullPath, std::wstring relativePath);
  std::wstring GetFullPath() const { return std::wstring(m_fileref.FullPathAsPointer()); }
  std::wstring GetRelativePath() const { return std::wstring(m_fileref.RelativePathAsPointer()); }
  //public ContentHash ContentHash
  //public FileReferenceStatus FullPathStatus
  //public bool IsSet
};