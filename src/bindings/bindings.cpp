#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
PYBIND11_MODULE(_rhino3dm, m){
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
  init3dmSettingsBindings(m);
  initPolylineBindings(m);
  initPlaneBindings(m);
  initObjectBindings(m);
  init3dmAttributesBindings(m);
  initBitmapBindings(m);
  initDimensionStyleBindings(m);
  initLayerBindings(m);
  initMaterialBindings(m);
  initTextureBindings(m);
  initTextureMappingBindings(m);
  initPointBindings(m);
  initXformBindings(m);
  initArcBindings(m);
  initBoundingBoxBindings(m);
  initBoxBindings(m);
  initGeometryBindings(m);
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
  initSurfaceProxyBindings(m);
  initPlaneSurfaceBindings(m);
  initBrepBindings(m);
  initExtrusionBindings(m);
  initNurbsSurfaceBindings(m);
  initSphereBindings(m);
  initViewportBindings(m);
  initExtensionsBindings(m);
}

