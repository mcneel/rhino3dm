#include "bindings.h"

class BND_DocObjects {};

class BND_MeshTypeNamespace {};

enum class LoftType : int
{
  Normal = 0,
  Loose = 1,
  Tight = 2,
  Straight = 3,
  Uniform = 5
};

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDefines(pybind11::module& m)
{
  py::enum_<ON_COMPONENT_INDEX::TYPE>(m, "ComponentIndexType")
    .value("InvalidType", ON_COMPONENT_INDEX::TYPE::invalid_type)
    .value("BrepVertex", ON_COMPONENT_INDEX::TYPE::brep_vertex)
    .value("BrepEdge", ON_COMPONENT_INDEX::TYPE::brep_edge)
    .value("BrepFace", ON_COMPONENT_INDEX::TYPE::brep_face)
    .value("BrepTrim", ON_COMPONENT_INDEX::TYPE::brep_trim)
    .value("BrepLoop", ON_COMPONENT_INDEX::TYPE::brep_loop)
    .value("MeshVertex", ON_COMPONENT_INDEX::TYPE::mesh_vertex)
    .value("MeshTopologyVertex", ON_COMPONENT_INDEX::TYPE::meshtop_vertex)
    .value("MeshTopologyEdge", ON_COMPONENT_INDEX::TYPE::meshtop_edge)
    .value("MeshFace", ON_COMPONENT_INDEX::TYPE::mesh_face)
    .value("MeshNgon", ON_COMPONENT_INDEX::TYPE::mesh_ngon)
    .value("InstanceDefinitionPart", ON_COMPONENT_INDEX::TYPE::idef_part)
    .value("PolycurveSegment", ON_COMPONENT_INDEX::TYPE::polycurve_segment)
    .value("PointCloudPoint", ON_COMPONENT_INDEX::TYPE::pointcloud_point)
    .value("GroupMember", ON_COMPONENT_INDEX::TYPE::group_member)
    .value("ExtrusionBottomProfile", ON_COMPONENT_INDEX::TYPE::extrusion_bottom_profile)
    .value("ExtrusionTopProfile", ON_COMPONENT_INDEX::TYPE::extrusion_top_profile)
    .value("ExtrusionWallEdge", ON_COMPONENT_INDEX::TYPE::extrusion_wall_edge)
    .value("ExtrusionWallSurface", ON_COMPONENT_INDEX::TYPE::extrusion_wall_surface)
    .value("ExtrusionCapSurface", ON_COMPONENT_INDEX::TYPE::extrusion_cap_surface)
    .value("ExtrusionPath", ON_COMPONENT_INDEX::TYPE::extrusion_path)
    .value("SubdVertex", ON_COMPONENT_INDEX::TYPE::subd_vertex)
    .value("SubdEdge", ON_COMPONENT_INDEX::TYPE::subd_edge)
    .value("SubdFace", ON_COMPONENT_INDEX::TYPE::subd_face)
    .value("DimLinearPoint", ON_COMPONENT_INDEX::TYPE::dim_linear_point)
    .value("DimRadialPoint", ON_COMPONENT_INDEX::TYPE::dim_radial_point)
    .value("DimAngularPoint", ON_COMPONENT_INDEX::TYPE::dim_angular_point)
    .value("DimOrdinatePoint", ON_COMPONENT_INDEX::TYPE::dim_ordinate_point)
    .value("DimTextPoint", ON_COMPONENT_INDEX::TYPE::dim_text_point)
    .value("NoType", ON_COMPONENT_INDEX::TYPE::no_type)
    ;

  py::class_<ON_COMPONENT_INDEX>(m, "ComponentIndex")
    .def(py::init<ON_COMPONENT_INDEX::TYPE, int>(), py::arg("type"), py::arg("index"))
    .def_readonly("ComponentIndexType", &ON_COMPONENT_INDEX::m_type)
    .def_readonly("Index", &ON_COMPONENT_INDEX::m_index)
    ;

  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>(), py::arg("start"), py::arg("end"))
    .def_readwrite("From", &ON_Line::from)
    .def_readwrite("To", &ON_Line::to)
    .def_property_readonly("IsValid", &ON_Line::IsValid)
    .def_property_readonly("Length", &ON_Line::Length)
    .def_property_readonly("Direction", &ON_Line::Direction)
    .def_property_readonly("UnitTangent", &ON_Line::Tangent)
    .def("PointAt", &ON_Line::PointAt, py::arg("t"))
    ;

  py::enum_<LoftType>(m, "LoftType")
    .value("Normal", LoftType::Normal)
    .value("Loose", LoftType::Loose)
    .value("Tight", LoftType::Tight)
    .value("Straight", LoftType::Straight)
    .value("Uniform", LoftType::Uniform)
    ;

  py::enum_<ON::object_mode>(m, "ObjectMode")
    .value("Normal", ON::object_mode::normal_object)
    .value("Hidden", ON::object_mode::hidden_object)
    .value("Locked", ON::object_mode::locked_object)
    .value("InstanceDefinitionObject", ON::object_mode::idef_object)
    ;

  py::enum_<ON::object_color_source>(m, "ObjectColorSource")
    .value("ColorFromLayer", ON::object_color_source::color_from_layer)
    .value("ColorFromObject", ON::object_color_source::color_from_object)
    .value("ColorFromMaterial", ON::object_color_source::color_from_material)
    .value("ColorFromParent", ON::object_color_source::color_from_parent)
    ;

  py::enum_<ON::plot_color_source>(m, "ObjectPlotColorSource")
    .value("PlotColorFromLayer", ON::plot_color_from_layer)
    .value("PlotColorFromObject", ON::plot_color_from_object)
    .value("PlotColorFromDisplay", ON::plot_color_from_display)
    .value("PlotColorFromParent", ON::plot_color_from_parent)
    ;

  py::enum_<ON::plot_weight_source>(m, "ObjectPlotWeightSource")
    .value("PlotWeightFromLayer", ON::plot_weight_from_layer)
    .value("PlotWeightFromObject", ON::plot_weight_from_object)
    .value("PlotWeightFromParent", ON::plot_weight_from_parent)
    ;

  py::enum_<ON::object_linetype_source>(m, "ObjectLinetypeSource")
    .value("LinetypeFromLayer", ON::object_linetype_source::linetype_from_layer)
    .value("LinetypeFromObject", ON::linetype_from_object)
    .value("LinetypeFromParent", ON::linetype_from_parent)
    ;

  py::enum_<ON::object_material_source>(m, "ObjectMaterialSource")
    .value("MaterialFromLayer", ON::material_from_layer)
    .value("MaterialFromObject", ON::material_from_object)
    .value("MaterialFromParent", ON::material_from_parent)
    ;

  py::enum_<ON::object_type>(m, "ObjectType")
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

  py::enum_<ON::coordinate_system>(m, "CoordinateSystem")
    .value("World", ON::coordinate_system::world_cs)
    .value("Camera", ON::coordinate_system::camera_cs)
    .value("Clip", ON::coordinate_system::clip_cs)
    .value("Screen", ON::coordinate_system::screen_cs)
    ;

  py::enum_<ON::mesh_type>(m, "MeshType")
    .value("Default", ON::default_mesh)
    .value("Render", ON::render_mesh)
    .value("Analysis", ON::analysis_mesh)
    .value("Preview", ON::preview_mesh)
    .value("Any", ON::any_mesh)
    ;

  py::enum_<ON::object_decoration>(m, "ObjectDecoration")
    .value("None", ON::no_object_decoration)
    .value("StartArrowhead", ON::start_arrowhead)
    .value("EndArrowhead", ON::end_arrowhead)
    .value("BothArrowhead", ON::both_arrowhead)
    ;

  py::enum_<ON::active_space>(m, "ActiveSpace")
    .value("None", ON::active_space::no_space)
    .value("ModelSpace", ON::active_space::model_space)
    .value("PageSpace", ON::active_space::page_space)
    ;

  py::enum_<ON::LengthUnitSystem>(m, "UnitSystem")
    .value("None", ON::LengthUnitSystem::None)
    .value("Angstroms", ON::LengthUnitSystem::Angstroms)
    .value("Nanometers", ON::LengthUnitSystem::Nanometers)
    .value("Microns", ON::LengthUnitSystem::Microns)
    .value("Millimeters", ON::LengthUnitSystem::Millimeters)
    .value("Centimeters", ON::LengthUnitSystem::Centimeters)
    .value("Decimeters", ON::LengthUnitSystem::Decimeters)
    .value("Meters", ON::LengthUnitSystem::Meters)
    .value("Dekameters", ON::LengthUnitSystem::Dekameters)
    .value("Hectomeers", ON::LengthUnitSystem::Hectometers)
    .value("Kilometers", ON::LengthUnitSystem::Kilometers)
    .value("Megameters", ON::LengthUnitSystem::Megameters)
    .value("Gigameters", ON::LengthUnitSystem::Gigameters)
    .value("Microinches", ON::LengthUnitSystem::Microinches)
    .value("Mils", ON::LengthUnitSystem::Mils)
    .value("Inches", ON::LengthUnitSystem::Inches)
    .value("Feet", ON::LengthUnitSystem::Feet)
    .value("Yards", ON::LengthUnitSystem::Yards)
    .value("Miles", ON::LengthUnitSystem::Miles)
    .value("PrinterPoints", ON::LengthUnitSystem::PrinterPoints)
    .value("PrinterPicas", ON::LengthUnitSystem::PrinterPicas)
    .value("NauticalMiles", ON::LengthUnitSystem::NauticalMiles)
    .value("AstronomicalUnits", ON::LengthUnitSystem::AstronomicalUnits)
    .value("LightYears", ON::LengthUnitSystem::LightYears)
    .value("Parsecs", ON::LengthUnitSystem::Parsecs)
    .value("CustomUnits", ON::LengthUnitSystem::CustomUnits)
    .value("Unset", ON::LengthUnitSystem::Unset)
    .def_static("UnitScale", [](ON::LengthUnitSystem a, ON::LengthUnitSystem b) { return ON::UnitScale(a, b);})
    ;

  py::enum_<ON::light_style>(m, "LightStyle")
    .value("None", ON::light_style::unknown_light_style)
    .value("CameraDirectional", ON::light_style::camera_directional_light)
    .value("CameraPoint", ON::light_style::camera_point_light)
    .value("CameraSpot", ON::light_style::camera_spot_light)
    .value("WorldDirectional", ON::light_style::world_directional_light)
    .value("WorldPoint", ON::light_style::world_point_light)
    .value("WorldSpot", ON::light_style::world_spot_light)
    .value("Ambient", ON::light_style::ambient_light)
    .value("WorldLinear", ON::light_style::world_linear_light)
    .value("WorldRectangular", ON::light_style::world_rectangular_light)
    ;
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
  enum_<ON_COMPONENT_INDEX::TYPE>("ComponentIndexType")
    .value("InvalidType", ON_COMPONENT_INDEX::TYPE::invalid_type)
    .value("BrepVertex", ON_COMPONENT_INDEX::TYPE::brep_vertex)
    .value("BrepEdge", ON_COMPONENT_INDEX::TYPE::brep_edge)
    .value("BrepFace", ON_COMPONENT_INDEX::TYPE::brep_face)
    .value("BrepTrim", ON_COMPONENT_INDEX::TYPE::brep_trim)
    .value("BrepLoop", ON_COMPONENT_INDEX::TYPE::brep_loop)
    .value("MeshVertex", ON_COMPONENT_INDEX::TYPE::mesh_vertex)
    .value("MeshTopologyVertex", ON_COMPONENT_INDEX::TYPE::meshtop_vertex)
    .value("MeshTopologyEdge", ON_COMPONENT_INDEX::TYPE::meshtop_edge)
    .value("MeshFace", ON_COMPONENT_INDEX::TYPE::mesh_face)
    .value("MeshNgon", ON_COMPONENT_INDEX::TYPE::mesh_ngon)
    .value("InstanceDefinitionPart", ON_COMPONENT_INDEX::TYPE::idef_part)
    .value("PolycurveSegment", ON_COMPONENT_INDEX::TYPE::polycurve_segment)
    .value("PointCloudPoint", ON_COMPONENT_INDEX::TYPE::pointcloud_point)
    .value("GroupMember", ON_COMPONENT_INDEX::TYPE::group_member)
    .value("ExtrusionBottomProfile", ON_COMPONENT_INDEX::TYPE::extrusion_bottom_profile)
    .value("ExtrusionTopProfile", ON_COMPONENT_INDEX::TYPE::extrusion_top_profile)
    .value("ExtrusionWallEdge", ON_COMPONENT_INDEX::TYPE::extrusion_wall_edge)
    .value("ExtrusionWallSurface", ON_COMPONENT_INDEX::TYPE::extrusion_wall_surface)
    .value("ExtrusionCapSurface", ON_COMPONENT_INDEX::TYPE::extrusion_cap_surface)
    .value("ExtrusionPath", ON_COMPONENT_INDEX::TYPE::extrusion_path)
    .value("SubdVertex", ON_COMPONENT_INDEX::TYPE::subd_vertex)
    .value("SubdEdge", ON_COMPONENT_INDEX::TYPE::subd_edge)
    .value("SubdFace", ON_COMPONENT_INDEX::TYPE::subd_face)
    .value("DimLinearPoint", ON_COMPONENT_INDEX::TYPE::dim_linear_point)
    .value("DimRadialPoint", ON_COMPONENT_INDEX::TYPE::dim_radial_point)
    .value("DimAngularPoint", ON_COMPONENT_INDEX::TYPE::dim_angular_point)
    .value("DimOrdinatePoint", ON_COMPONENT_INDEX::TYPE::dim_ordinate_point)
    .value("DimTextPoint", ON_COMPONENT_INDEX::TYPE::dim_text_point)
    .value("NoType", ON_COMPONENT_INDEX::TYPE::no_type)
    ;

  class_<ON_COMPONENT_INDEX>("ComponentIndex")
    .constructor<ON_COMPONENT_INDEX::TYPE, int>()
    .property("componentIndexType", &ON_COMPONENT_INDEX::m_type)
    .property("index", &ON_COMPONENT_INDEX::m_index)
    ;

  class_<ON_Line>("Line")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .property("from", &ON_Line::from)
    .property("to", &ON_Line::to)
    .property("length", &ON_Line::Length);


  enum_<ON::object_mode>("ObjectMode")
    .value("Normal", ON::object_mode::normal_object)
    .value("Hidden", ON::object_mode::hidden_object)
    .value("Locked", ON::object_mode::locked_object)
    .value("InstanceDefinitionObject", ON::object_mode::idef_object)
    ;

  enum_<ON::object_color_source>("ObjectColorSource")
    .value("ColorFromLayer", ON::object_color_source::color_from_layer)
    .value("ColorFromObject", ON::object_color_source::color_from_object)
    .value("ColorFromMaterial", ON::object_color_source::color_from_material)
    .value("ColorFromParent", ON::object_color_source::color_from_parent)
    ;

  enum_<ON::plot_color_source>("ObjectPlotColorSource")
    .value("PlotColorFromLayer", ON::plot_color_from_layer)
    .value("PlotColorFromObject", ON::plot_color_from_object)
    .value("PlotColorFromDisplay", ON::plot_color_from_display)
    .value("PlotColorFromParent", ON::plot_color_from_parent)
    ;

  enum_<ON::plot_weight_source>("ObjectPlotWeightSource")
    .value("PlotWeightFromLayer", ON::plot_weight_from_layer)
    .value("PlotWeightFromObject", ON::plot_weight_from_object)
    .value("PlotWeightFromParent", ON::plot_weight_from_parent)
    ;

  enum_<ON::object_linetype_source>("ObjectLinetypeSource")
    .value("LinetypeFromLayer", ON::object_linetype_source::linetype_from_layer)
    .value("LinetypeFromObject", ON::linetype_from_object)
    .value("LinetypeFromParent", ON::linetype_from_parent)
    ;

  enum_<ON::object_material_source>("ObjectMaterialSource")
    .value("MaterialFromLayer", ON::material_from_layer)
    .value("MaterialFromObject", ON::material_from_object)
    .value("MaterialFromParent", ON::material_from_parent)
    ;

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

  enum_<ON::object_decoration>("ObjectDecoration")
    .value("None", ON::no_object_decoration)
    .value("StartArrowhead", ON::start_arrowhead)
    .value("EndArrowhead", ON::end_arrowhead)
    .value("BothArrowhead", ON::both_arrowhead)
    ;

  enum_<ON::active_space>("ActiveSpace")
    .value("None", ON::active_space::no_space)
    .value("ModelSpace", ON::active_space::model_space)
    .value("PageSpace", ON::active_space::page_space)
    ;

  enum_<ON::LengthUnitSystem>("UnitSystem")
    .value("None", ON::LengthUnitSystem::None)
    .value("Angstroms", ON::LengthUnitSystem::Angstroms)
    .value("Nanometers", ON::LengthUnitSystem::Nanometers)
    .value("Microns", ON::LengthUnitSystem::Microns)
    .value("Millimeters", ON::LengthUnitSystem::Millimeters)
    .value("Centimeters", ON::LengthUnitSystem::Centimeters)
    .value("Decimeters", ON::LengthUnitSystem::Decimeters)
    .value("Meters", ON::LengthUnitSystem::Meters)
    .value("Dekameters", ON::LengthUnitSystem::Dekameters)
    .value("Hectomeers", ON::LengthUnitSystem::Hectometers)
    .value("Kilometers", ON::LengthUnitSystem::Kilometers)
    .value("Megameters", ON::LengthUnitSystem::Megameters)
    .value("Gigameters", ON::LengthUnitSystem::Gigameters)
    .value("Microinches", ON::LengthUnitSystem::Microinches)
    .value("Mils", ON::LengthUnitSystem::Mils)
    .value("Inches", ON::LengthUnitSystem::Inches)
    .value("Feet", ON::LengthUnitSystem::Feet)
    .value("Yards", ON::LengthUnitSystem::Yards)
    .value("Miles", ON::LengthUnitSystem::Miles)
    .value("PrinterPoints", ON::LengthUnitSystem::PrinterPoints)
    .value("PrinterPicas", ON::LengthUnitSystem::PrinterPicas)
    .value("NauticalMiles", ON::LengthUnitSystem::NauticalMiles)
    .value("AstronomicalUnits", ON::LengthUnitSystem::AstronomicalUnits)
    .value("LightYears", ON::LengthUnitSystem::LightYears)
    .value("Parsecs", ON::LengthUnitSystem::Parsecs)
    .value("CustomUnits", ON::LengthUnitSystem::CustomUnits)
    .value("Unset", ON::LengthUnitSystem::Unset)
    ;

  enum_<ON::light_style>("LightStyle")
    .value("None", ON::light_style::unknown_light_style)
    .value("CameraDirectional", ON::light_style::camera_directional_light)
    .value("CameraPoint", ON::light_style::camera_point_light)
    .value("CameraSpot", ON::light_style::camera_spot_light)
    .value("WorldDirectional", ON::light_style::world_directional_light)
    .value("WorldPoint", ON::light_style::world_point_light)
    .value("WorldSpot", ON::light_style::world_spot_light)
    .value("Ambient", ON::light_style::ambient_light)
    .value("WorldLinear", ON::light_style::world_linear_light)
    .value("WorldRectangular", ON::light_style::world_rectangular_light)
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
