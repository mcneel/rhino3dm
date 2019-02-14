#pragma once

// we don't need to export RH_C_FUNCTION in these libraries
#define RH_C_FUNCTION

#if defined(__EMSCRIPTEN__)
#define ON_WASM_COMPILE
#else
#define ON_PYTHON_COMPILE
#endif

#if defined(ON_PYTHON_COMPILE)
#include <pybind11/pybind11.h>
#pragma comment(lib, "rpcrt4.lib")
#pragma comment(lib, "shlwapi.lib")
#endif

#include "../lib/opennurbs/opennurbs.h"


#if defined(ON_WASM_COMPILE)
#include <emscripten/bind.h>
#endif

#include "bnd_color.h"
#include "bnd_file_utilities.h"
#include "bnd_uuid.h"
#include "bnd_defines.h"
#include "bnd_intersect.h"
#include "bnd_boundingbox.h"
#include "bnd_box.h"
#include "bnd_point.h"
#include "bnd_polyline.h"
#include "bnd_plane.h"
#include "bnd_3dm_settings.h"
#include "bnd_xform.h"
#include "bnd_circle.h"
#include "bnd_cone.h"
#include "bnd_cylinder.h"
#include "bnd_ellipse.h"
#include "bnd_font.h"
#include "bnd_object.h"
#include "bnd_model_component.h"
#include "bnd_3dm_attributes.h"
#include "bnd_bitmap.h"
#include "bnd_dimensionstyle.h"
#include "bnd_layer.h"
#include "bnd_material.h"
#include "bnd_texture.h"
#include "bnd_texture_mapping.h"
#include "bnd_geometry.h"
#include "bnd_instance.h"
#include "bnd_hatch.h"
#include "bnd_pointcloud.h"
#include "bnd_pointgeometry.h"
#include "bnd_pointgrid.h"
#include "bnd_arc.h"
#include "bnd_curve.h"
#include "bnd_curveproxy.h"
#include "bnd_arccurve.h"
#include "bnd_bezier.h"
#include "bnd_linecurve.h"
#include "bnd_polycurve.h"
#include "bnd_polylinecurve.h"
#include "bnd_nurbscurve.h"
#include "bnd_mesh.h"
#include "bnd_surface.h"
#include "bnd_revsurface.h"
#include "bnd_surfaceproxy.h"
#include "bnd_planesurface.h"
#include "bnd_brep.h"
#include "bnd_beam.h"
#include "bnd_nurbssurface.h"
#include "bnd_sphere.h"
#include "bnd_viewport.h"
#include "bnd_extensions.h"

#if defined(ON_PYTHON_COMPILE)
std::string StringFromDict(pybind11::dict& d, const char* key);
int IntFromDict(pybind11::dict& d, const char* key);

template <class T>
void SetDictValue(pybind11::dict& d, const char* key, T& value);

#else

std::string StringFromDict(emscripten::val& d, const char* key);
int IntFromDict(emscripten::val& d, const char* key);

template <class T>
void SetDictValue(emscripten::val& d, const char* key, T& value);
#endif
