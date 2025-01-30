#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
RH3DM_PYTHON_BINDING(_rhino3dm, m) {
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
EMSCRIPTEN_BINDINGS(rhino3dm) {
  void* m = nullptr;
#endif

  ON::Begin();
  initFileUtilitiesBindings(m);
  initDefines(m);
  initIntersectBindings(m);
  initPolylineBindings(m);
  initPlaneBindings(m);
  initObjectBindings(m);
  initModelComponentBindings(m);
  init3dmSettingsBindings(m);
  init3dmAttributesBindings(m);
  initBitmapBindings(m);
  initDimensionStyleBindings(m);
  initLayerBindings(m);
  initMaterialBindings(m);
  initEmbeddedFileBindings(m);
  initSkylightBindings(m);
  initGroundPlaneBindings(m);
  initSafeFrameBindings(m);
  initDitheringBindings(m);
  initLinearWorkflowBindings(m);
  initRenderEnvironmentsBindings(m);
  initRenderChannelsBindings(m);
  initRenderContentBindings(m);
  initSunBindings(m);
  initPostEffectBindings(m);
  initDecalBindings(m);
  initMeshModifierBindings(m);
  initEnvironmentBindings(m);
  initTextureBindings(m);
  initTextureMappingBindings(m);
  initPointBindings(m);
  initXformBindings(m);
  initArcBindings(m);
  initBoundingBoxBindings(m);
  initBoxBindings(m);
  initGeometryBindings(m);
  initAnnotationBaseBindings(m);
  initInstanceBindings(m);
  initHatchBindings(m);
  initPointCloudBindings(m);
  initPointGeometryBindings(m);
  initPointGridBindings(m);
  initCurveBindings(m);
  initCurveProxyBindings(m);
  initNurbsCurveBindings(m);
  initMeshBindings(m);
  initCircleBindings(m);
  initConeBindings(m);
  initCylinderBindings(m);
  initEllipseBindings(m);
  initFontBindings(m);
  initArcCurveBindings(m);
  initBezierBindings(m);
  initLineCurveBindings(m);
  initPolyCurveBindings(m);
  initPolylineCurveBindings(m);
  initSurfaceBindings(m);
  initRevSurfaceBindings(m);
  initSubDBindings(m);
  initSurfaceProxyBindings(m);
  initPlaneSurfaceBindings(m);
  initBrepBindings(m);
  initExtrusionBindings(m);
  initNurbsSurfaceBindings(m);
  initLightBindings(m);
  initSphereBindings(m);
  initViewportBindings(m);
  initGroupBindings(m);
  initExtensionsBindings(m);
  initDracoBindings(m);
  initRTreeBindings(m);
  initLinetypeBindings(m);
}

#if defined(ON_PYTHON_COMPILE)
std::string ToStdString(const py::str& str)
{
  std::string rc = py::cast<std::string>(str);
  return rc;
}
#endif
  
BND_TUPLE CreateTuple(int count)
{
#if defined(ON_PYTHON_COMPILE)

#if defined(NANOBIND)
  BND_TUPLE rc = py::tuple();
#else
  BND_TUPLE rc = py::tuple(count);
#endif
#else
  emscripten::val rc(emscripten::val::array());
#endif
  return rc;
}

BND_TUPLE NullTuple()
{
#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
  UNIMPLEMENTED_EXCEPTION;
#else
  return py::none();
#endif
#else
  return emscripten::val::null();
#endif
}

BND_LIST CreateList()
{
#if defined(ON_PYTHON_COMPILE)
  BND_LIST rc = py::list();
#else
  emscripten::val rc(emscripten::val::array());
#endif
  return rc;
}

BND_LIST NullList()
{
#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
  return py::list();
#else
  return py::none();
#endif
#else
  return emscripten::val::null();
#endif
}

BND_DateTime CreateDateTime(struct tm t)
{
#if defined(ON_PYTHON_COMPILE)
  if (!PyDateTimeAPI) {
    PyDateTime_IMPORT;
  }
  return PyDateTime_FromDateAndTime(t.tm_year + 1900,
                                    t.tm_mon + 1,
                                    t.tm_mday,
                                    t.tm_hour,
                                    t.tm_min,
                                    t.tm_sec,
                                    0);
#else
  emscripten::val Date = emscripten::val::global("Date");
  return Date.new_(t.tm_year + 1900,
                   t.tm_mon,
                   t.tm_mday,
                   t.tm_hour,
                   t.tm_min,
                   t.tm_sec,
                   0);
#endif
}
