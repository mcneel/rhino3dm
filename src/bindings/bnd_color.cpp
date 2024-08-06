#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)

BND_Color ON_Color_to_Binding(const ON_Color& color)
{
  return py::make_tuple(color.Red(), color.Green(), color.Blue(), 255 - color.Alpha());
}

ON_Color Binding_to_ON_Color(const BND_Color& color)
{
  int r = py::cast<int>(color[0]);
  int g = py::cast<int>(color[1]);
  int b = py::cast<int>(color[2]);
  int a = py::cast<int>(color[3]);
  return ON_Color(r, g, b, 255-a);
}

BND_Color4f ON_4fColor_to_Binding(const ON_4fColor& color)
{
  return py::make_tuple(color.Red(), color.Green(), color.Blue(), color.Alpha());
}


ON_4fColor Binding_to_ON_4fColor(const BND_Color4f& color)
{
  float r = py::cast<float>(color[0]);
  float g = py::cast<float>(color[1]);
  float b = py::cast<float>(color[2]);
  float a = py::cast<float>(color[3]);
  return ON_4fColor(r, g, b, a);
}

#endif

#if defined(ON_WASM_COMPILE)

BND_Color ON_Color_to_Binding(const ON_Color& color)
{
  emscripten::val v(emscripten::val::object());
  v.set("r", emscripten::val(color.Red()));
  v.set("g", emscripten::val(color.Green()));
  v.set("b", emscripten::val(color.Blue()));
  v.set("a", emscripten::val(255 - color.Alpha()));
  return v;
}

ON_Color Binding_to_ON_Color(const BND_Color& color)
{
  int r = color["r"].as<int>();
  int g = color["g"].as<int>();
  int b = color["b"].as<int>();
  int a = color["a"].as<int>();
  return ON_Color(r,g,b,255-a);
}

BND_Color4f ON_4fColor_to_Binding(const ON_4fColor& color) 
{

  emscripten::val v(emscripten::val::object());
  v.set("r", emscripten::val(color.Red()));
  v.set("g", emscripten::val(color.Green()));
  v.set("b", emscripten::val(color.Blue()));
  v.set("a", emscripten::val(color.Alpha()));
  return v;

}

ON_4fColor Binding_to_ON_4fColor(const BND_Color4f& color)
{

  float r = color["r"].as<float>();
  float g = color["g"].as<float>();
  float b = color["b"].as<float>();
  float a = color["a"].as<float>();
  return ON_4fColor(r,g,b,a);

}



#endif
