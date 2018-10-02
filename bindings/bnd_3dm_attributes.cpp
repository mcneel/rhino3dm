#include "bindings.h"

BND_3dmAttributes::BND_3dmAttributes()
{
  m_attributes.reset(new ON_3dmObjectAttributes());
  m_object = m_attributes;
}

BND_3dmAttributes::BND_3dmAttributes(ON_3dmObjectAttributes* attrs)
{
  m_attributes.reset(attrs);
  m_object = m_attributes;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void init3dmAttributesBindings(pybind11::module& m)
{
  py::class_<BND_3dmAttributes, BND_Object>(m, "ObjectAttributes")
    .def(py::init<>());
}
#endif
