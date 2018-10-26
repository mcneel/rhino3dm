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

pybind11::dict PointToDict(const ON_3dPoint& point)
{
  pybind11::dict rc;
  rc["X"] = point.x;
  rc["Y"] = point.y;
  rc["Z"] = point.z;
  return rc;
}
pybind11::dict VectorToDict(const ON_3dVector& vector)
{
  pybind11::dict rc;
  rc["X"] = vector.x;
  rc["Y"] = vector.y;
  rc["Z"] = vector.z;
  return rc;
}
pybind11::dict PlaneToDict(const ON_Plane& plane)
{
  pybind11::dict rc;
  rc["Origin"] = PointToDict(plane.origin);
  rc["XAxis"] = VectorToDict(plane.xaxis);
  rc["YAxis"] = VectorToDict(plane.yaxis);
  rc["ZAxis"] = VectorToDict(plane.zaxis);
  return rc;
}

ON_3dPoint PointFromDict(pybind11::dict& dict)
{
  ON_3dVector rc;
  rc.x = dict["X"].cast<double>();
  rc.y = dict["Y"].cast<double>();
  rc.z = dict["Z"].cast<double>();
  return rc;
}
ON_3dVector VectorFromDict(pybind11::dict& dict)
{
  ON_3dPoint pt = PointFromDict(dict);
  return ON_3dVector(pt.x, pt.y, pt.z);
}
ON_Plane PlaneFromDict(pybind11::dict& dict)
{
  ON_Plane plane;
  pybind11::dict d = dict["Origin"].cast<pybind11::dict>();
  plane.origin = PointFromDict(d);
  d = dict["XAxis"].cast<pybind11::dict>();
  plane.xaxis = VectorFromDict(d);
  d = dict["YAxis"].cast<pybind11::dict>();
  plane.yaxis = VectorFromDict(d);
  d = dict["ZAxis"].cast<pybind11::dict>();
  plane.zaxis = VectorFromDict(d);
  plane.UpdateEquation();
  return plane;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDefines(void*)
{
  class_<ON_Line>("Line")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .property("from", &ON_Line::from)
    .property("to", &ON_Line::to)
    .property("length", &ON_Line::Length);

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
    .value("World", ON::coordinate_system::world_cs)
    .value("Camera", ON::coordinate_system::camera_cs)
    .value("Clip", ON::coordinate_system::clip_cs)
    .value("Screen", ON::coordinate_system::screen_cs)
    ;

  enum_<ON::mesh_type>("MeshType")
    .value("Default", ON::default_mesh)
    .value("Render", ON::render_mesh)
    .value("Analysis", ON::analysis_mesh)
    .value("Preview", ON::preview_mesh)
    .value("Any", ON::any_mesh)
    ;
}


emscripten::val PointToDict(const ON_3dPoint& point)
{
  emscripten::val p(emscripten::val::object());
  p.set("X", emscripten::val(point.x));
  p.set("Y", emscripten::val(point.y));
  p.set("Z", emscripten::val(point.z));
  return p;
}
emscripten::val VectorToDict(const ON_3dVector& vector)
{
  emscripten::val p(emscripten::val::object());
  p.set("X", emscripten::val(vector.x));
  p.set("Y", emscripten::val(vector.y));
  p.set("Z", emscripten::val(vector.z));
  return p;
}

emscripten::val PlaneToDict(const ON_Plane& plane)
{
  emscripten::val p(emscripten::val::object());
  p.set("Origin", PointToDict(plane.origin));
  p.set("XAxis", VectorToDict(plane.xaxis));
  p.set("YAxis", VectorToDict(plane.yaxis));
  p.set("ZAxis", VectorToDict(plane.zaxis));
  return p;
}

#endif
