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
  initDefines(m);
  initObjectBindings(m);
  init3dmAttributesBindings(m);
  initLayerBindings(m);
  initPointBindings(m);
  initArcBindings(m);
  initBoundingBoxBindings(m);
  initGeometryBindings(m);
  initCurveBindings(m);
  initNurbsCurveBindings(m);
  initMeshBindings(m);
  initCircleBindings(m);
  initArcCurveBindings(m);
  initLineCurveBindings(m);
  initPolyCurveBindings(m);
  initPolylineCurveBindings(m);
  initSurfaceBindings(m);
  initSurfaceProxyBindings(m);
  initBrepBindings(m);
  initExtrusionBindings(m);
  initNurbsSurfaceBindings(m);
  initSphereBindings(m);
  initViewportBindings(m);
  initExtensionsBindings(m);
}
