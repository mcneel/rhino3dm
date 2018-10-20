#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)

BND_Color ON_Color_to_Binding(const ON_Color& color)
{
  return pybind11::make_tuple(color.Red(), color.Green(), color.Blue(), 255 - color.Alpha());
}

ON_Color Binding_to_ON_Color(const BND_Color& color)
{
  int r = color[0].cast<int>();
  int g = color[1].cast<int>();
  int b = color[2].cast<int>();
  int a = color[3].cast<int>();
  return ON_Color(r, g, b, a);
}

#endif

#if defined(ON_WASM_COMPILE)
//BND_UUID ON_UUID_to_Binding(const ON_UUID& id)
//{
//  char s[37];
//  memset(s, 0, sizeof(s));
//
//  char* suuid = ON_UuidToString(id, s);
//  std::string rc(suuid);
//  return rc;
//}

#endif
