#include "bindings.h"

class BND_DocObjects {};

class BND_MeshTypeNamespace {};

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDefines(pybind11::module& m)
{
  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def_property_readonly("Length", &ON_Line::Length);

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

  py::class_<BND_DocObjects> docobjects(m, "DocObjects");

  py::enum_<ON::object_type>(docobjects, "ObjectType")
    .value("None", ON::unknown_object_type)
    .value("Point", ON::point_object)
    .value("PointSet", ON::pointset_object)
    .value("Curve", ON::curve_object)
    .value("Surface", ON::surface_object)
    .value("Brep", ON::brep_object)
    .value("Mesh", ON::mesh_object)
    .value("Light", ON::light_object)
    .value("Annotation", ON::annotation_object)
    .value("InstanceDefinition", ON::instance_definition)
    .value("InstanceReference", ON::instance_reference)
    .value("TextDot", ON::text_dot)
    .value("Grip", ON::grip_object)
    .value("Detail", ON::detail_object)
    .value("Hatch", ON::hatch_object)
    .value("MorphControl", ON::morph_control_object)
    .value("SubD", ON::subd_object)
    .value("BrepLoop", ON::loop_object)
    .value("PolysrfFilter", ON::polysrf_filter)
    .value("EdgeFilter", ON::edge_filter)
    .value("PolyedgeFilter", ON::polyedge_filter)
    .value("MeshVertex", ON::meshvertex_filter)
    .value("MeshEdge", ON::meshedge_filter)
    .value("MeshFace", ON::meshface_filter)
    .value("Cage", ON::cage_object)
    .value("Phantom", ON::phantom_object)
    .value("ClipPlane", ON::clipplane_object)
    .value("Extrusion", ON::extrusion_object)
    .value("AnyObject", ON::any_object)
    .export_values();

  py::class_<BND_MeshTypeNamespace> geometry(m, "MeshType");
  py::enum_<ON::mesh_type>(geometry, "MeshTypeEnum")
    .value("Default", ON::default_mesh)
    .value("Render", ON::render_mesh)
    .value("Analysis", ON::analysis_mesh)
    .value("Preview", ON::preview_mesh)
    .value("Any", ON::any_mesh)
    .export_values();
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDefines(void*)
{
  class_<ON_UUID>("Guid");

  class_<ON_Line>("Line")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .property("from", &ON_Line::from)
    .property("to", &ON_Line::to)
    .property("length", &ON_Line::Length);

  class_<BND_Xform>("Transform");

  enum_<ON::object_type>("ObjectType")
    .value("None", ON::unknown_object_type)
    .value("Point", ON::point_object)
    .value("PointSet", ON::pointset_object)
    .value("Curve", ON::curve_object)
    .value("Surface", ON::surface_object)
    .value("Brep", ON::brep_object)
    .value("Mesh", ON::mesh_object)
    .value("Light", ON::light_object)
    .value("Annotation", ON::annotation_object)
    .value("InstanceDefinition", ON::instance_definition)
    .value("InstanceReference", ON::instance_reference)
    .value("TextDot", ON::text_dot)
    .value("Grip", ON::grip_object)
    .value("Detail", ON::detail_object)
    .value("Hatch", ON::hatch_object)
    .value("MorphControl", ON::morph_control_object)
    .value("SubD", ON::subd_object)
    .value("BrepLoop", ON::loop_object)
    .value("PolysrfFilter", ON::polysrf_filter)
    .value("EdgeFilter", ON::edge_filter)
    .value("PolyedgeFilter", ON::polyedge_filter)
    .value("MeshVertex", ON::meshvertex_filter)
    .value("MeshEdge", ON::meshedge_filter)
    .value("MeshFace", ON::meshface_filter)
    .value("Cage", ON::cage_object)
    .value("Phantom", ON::phantom_object)
    .value("ClipPlane", ON::clipplane_object)
    .value("Extrusion", ON::extrusion_object)
    .value("AnyObject", ON::any_object)
    ;

  enum_<ON::coordinate_system>("CoordinateSystem")
    .value("WORLD", ON::coordinate_system::world_cs)
    .value("CAMERA", ON::coordinate_system::camera_cs)
    .value("CLIP", ON::coordinate_system::clip_cs)
    .value("SCREEN", ON::coordinate_system::screen_cs)
    ;
}
#endif
