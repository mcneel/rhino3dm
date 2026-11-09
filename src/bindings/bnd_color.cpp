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
  // a colour is {r, g, b, a} (a optional): anything else used to read as black
  bool ok = color.typeOf().as<std::string>() == "object" && !color.isNull()
    && !emscripten::val::global("Array").call<bool>("isArray", color);
  for (const char* k : { "r", "g", "b" })
    ok = ok && color[k].typeOf().as<std::string>() == "number";
  ok = ok && (color["a"].isUndefined() || color["a"].typeOf().as<std::string>() == "number");
  if (!ok)
    emscripten::val::global("TypeError").new_(emscripten::val("expected a color {r, g, b, a}")).throw_();
  int r = color["r"].as<int>();
  int g = color["g"].as<int>();
  int b = color["b"].as<int>();
  int a = color["a"].isUndefined() ? 255 : color["a"].as<int>();  // no a: opaque
  return ON_Color(r,g,b,255-a);
}





#endif
