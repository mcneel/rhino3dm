#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initVersionBindings(rh3dmpymodule& m);
#else
void initVersionBindings(void* m);
#endif