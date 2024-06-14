#pragma once

// no need to export RH_C_FUNCTION in these libraries
#define RH_C_FUNCTION

#if defined(__EMSCRIPTEN__)
#define ON_WASM_COMPILE
#else
#define ON_PYTHON_COMPILE
#endif

#include "../lib/opennurbs/opennurbs.h"

#if defined(ON_PYTHON_COMPILE)
#include <pybind11/pybind11.h>
#include <pybind11/stl.h>
#include "datetime.h"
#pragma comment(lib, "rpcrt4.lib")
#pragma comment(lib, "shlwapi.lib")
#endif


#if defined(ON_WASM_COMPILE)
#include <emscripten/bind.h>
#endif

#if defined(ON_PYTHON_COMPILE)
typedef pybind11::dict BND_DICT;
typedef pybind11::tuple BND_Color;
typedef pybind11::tuple BND_Color4f;
typedef pybind11::tuple BND_TUPLE;
typedef pybind11::handle BND_DateTime;
#endif
#if defined(ON_WASM_COMPILE)
typedef emscripten::val BND_DICT;
typedef emscripten::val BND_Color;
typedef emscripten::val BND_Color4f;
typedef emscripten::val BND_TUPLE;
typedef emscripten::val BND_DateTime;

template<typename T>
std::vector<T> Tuple_To_Vector(BND_TUPLE data);

#endif

BND_TUPLE CreateTuple(int count);
BND_TUPLE NullTuple();
template<typename T>
void SetTuple(BND_TUPLE& tuple, int index, const T& value)
{
#if defined(ON_PYTHON_COMPILE)
  tuple[index] = value;
#else
  tuple.set(index, value);
#endif
}

BND_DateTime CreateDateTime(struct tm t);

#include "bnd_color.h"
#include "bnd_file_utilities.h"
#include "bnd_uuid.h"
#include "bnd_defines.h"
#include "bnd_intersect.h"
#include "bnd_boundingbox.h"
#include "bnd_box.h"
#include "bnd_point.h"
#include "bnd_object.h"
#include "bnd_geometry.h"
#include "bnd_curve.h"
#include "bnd_linecurve.h"
#include "bnd_polyline.h"
#include "bnd_plane.h"
#include "bnd_xform.h"
#include "bnd_circle.h"
#include "bnd_cone.h"
#include "bnd_cylinder.h"
#include "bnd_ellipse.h"
#include "bnd_font.h"
#include "bnd_object.h"
#include "bnd_model_component.h"
#include "bnd_geometry.h"
#include "bnd_light.h"
#include "bnd_material.h"
#include "bnd_embedded_file.h"
#include "bnd_skylight.h"
#include "bnd_ground_plane.h"
#include "bnd_safe_frame.h"
#include "bnd_dithering.h"
#include "bnd_linear_workflow.h"
#include "bnd_texture.h"
#include "bnd_render_channels.h"
#include "bnd_render_content.h"
#include "bnd_render_environments.h"
#include "bnd_post_effects.h"
#include "bnd_sun.h"
#include "bnd_decals.h"
#include "bnd_3dm_settings.h"
#include "bnd_bitmap.h"
#include "bnd_dimensionstyle.h"
#include "bnd_layer.h"
#include "bnd_texture_mapping.h"
#include "bnd_annotationbase.h"
#include "bnd_instance.h"
#include "bnd_hatch.h"
#include "bnd_pointcloud.h"
#include "bnd_pointgeometry.h"
#include "bnd_pointgrid.h"
#include "bnd_arc.h"
#include "bnd_curveproxy.h"
#include "bnd_arccurve.h"
#include "bnd_bezier.h"
#include "bnd_polycurve.h"
#include "bnd_polylinecurve.h"
#include "bnd_nurbscurve.h"
#include "bnd_mesh.h"
#include "bnd_surface.h"
#include "bnd_revsurface.h"
#include "bnd_subd.h"
#include "bnd_surfaceproxy.h"
#include "bnd_planesurface.h"
#include "bnd_brep.h"
#include "bnd_beam.h"
#include "bnd_nurbssurface.h"
#include "bnd_sphere.h"
#include "bnd_viewport.h"
#include "bnd_group.h"
#include "bnd_mesh_modifiers.h"
#include "bnd_extensions.h"
#include "bnd_3dm_attributes.h"
#include "bnd_draco.h"
#include "bnd_rtree.h"
#include "bnd_linetype.h"
