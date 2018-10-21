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
#include "bnd_uuid.h"
#include "bnd_defines.h"
#include "bnd_boundingbox.h"
#include "bnd_point.h"
#include "bnd_polyline.h"
#include "bnd_plane.h"
#include "bnd_xform.h"
#include "bnd_circle.h"
#include "bnd_cone.h"
#include "bnd_cylinder.h"
#include "bnd_object.h"
#include "bnd_3dm_attributes.h"
#include "bnd_layer.h"
#include "bnd_geometry.h"
#include "bnd_hatch.h"
#include "bnd_arc.h"
#include "bnd_curve.h"
#include "bnd_arccurve.h"
#include "bnd_bezier.h"
#include "bnd_linecurve.h"
#include "bnd_polycurve.h"
#include "bnd_polylinecurve.h"
#include "bnd_nurbscurve.h"
#include "bnd_mesh.h"
#include "bnd_surface.h"
#include "bnd_surfaceproxy.h"
#include "bnd_brep.h"
#include "bnd_beam.h"
#include "bnd_nurbssurface.h"
#include "bnd_sphere.h"
#include "bnd_viewport.h"
#include "bnd_extensions.h"
