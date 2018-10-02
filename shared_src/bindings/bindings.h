#pragma once

#define RH_C_FUNCTION extern "C"

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

#include "../opennurbs/opennurbs.h"


#if defined(ON_WASM_COMPILE)
#include <emscripten/bind.h>
#endif

#include "bnd_boundingbox.h"
#include "bnd_point.h"
#include "bnd_plane.h"
#include "bnd_xform.h"
#include "bnd_circle.h"
#include "bnd_object.h"
#include "bnd_3dm_attributes.h"
#include "bnd_geometry.h"
#include "bnd_arc.h"
#include "bnd_brep.h"
#include "bnd_curve.h"
#include "bnd_linecurve.h"
#include "bnd_polycurve.h"
#include "bnd_polylinecurve.h"
#include "bnd_nurbscurve.h"
#include "bnd_mesh.h"
#include "bnd_sphere.h"
#include "bnd_viewport.h"
#include "bnd_extensions.h"
