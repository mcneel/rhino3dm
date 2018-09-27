#pragma once

#include "../opennurbs/opennurbs.h"

#define RH_C_FUNCTION extern "C"

#if defined(__EMSCRIPTEN__)
#define ON_WASM_COMPILE
#else
#define ON_PYTHON_COMPILE
#endif

#if defined(ON_PYTHON_COMPILE)

#if defined(ON_RUNTIME_WIN)
#define  BOOST_PYTHON_STATIC_LIB
#pragma message( " --- statically linking opennurbs." )
#pragma comment(lib, "\"" "C:/dev/github/mcneel/rhino-geometry.py/opennurbs/bin/x64/Release/opennurbs_public_staticlib.lib" "\"")
#pragma comment(lib, "\"" "C:/dev/github/mcneel/rhino-geometry.py/opennurbs/bin/x64/Release/zlib.lib" "\"")
#pragma comment(lib, "\"" "C:/dev/github/mcneel/rhino-geometry.py/opennurbs/bin/x64/Release/freetype263_staticlib.lib" "\"")
#pragma comment(lib, "rpcrt4.lib")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "C:/Python27/libs/python27.lib")
#endif

#include <boost/python.hpp>
#endif

#include "bnd_boundingbox.h"
#include "bnd_point.h"
#include "bnd_plane.h"
#include "bnd_xform.h"
#include "bnd_circle.h"
#include "bnd_object.h"
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
