#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)

void initVersionBindings(rh3dmpymodule& m)
{
  
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initVersionBindings(void*)
{
    value_object("Version")
        .field("major", OPENNURBS_VERSION_MAJOR)
        .field("minor", OPENNURBS_VERSION_MINOR)
        .field("full", VERSION_WITH_PERIODS)
    ;
}
#endif