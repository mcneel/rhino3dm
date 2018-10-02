#include "bindings.h"

namespace py = pybind11;

PYBIND11_MODULE(_rhino3dm, m)
{
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";

  init3dmAttributesBindings(m);

  initPointBindings(m);

  initArcBindings(m);

  initBoundingBoxBindings(m);

  initObjectBindings(m);

  initGeometryBindings(m);

  initBrepBindings(m);

  initCurveBindings(m);

  initNurbsCurveBindings(m);

  initMeshBindings(m);

  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def_property_readonly("Length", &ON_Line::Length);

  initCircleBindings(m);

  initLineCurveBindings(m);

  initPolyCurveBindings(m);

  initPolylineCurveBindings(m);

  initSphereBindings(m);

  initViewportBindings(m);

  py::class_<ON_Xform>(m, "Transform")
    .def(py::init<>())
    .def(py::init<double>())
    .def_static("Translation", py::overload_cast<double, double, double>(&ON_Xform::TranslationTransformation))
    .def_static("Scale", py::overload_cast<const ON_3dPoint&, double>(&ON_Xform::ScaleTransformation))
    .def_static("Rotation", [](double angle, ON_3dVector rotationAxis, ON_3dPoint rotationCenter) {
      ON_Xform rc(1);
      rc.Rotation(angle, rotationAxis, rotationCenter);
      return rc;
    })
    ;

  initExtensionsBindings(m);
}


