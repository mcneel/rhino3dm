#include "bindings.h"

#pragma once

BND_Color ON_Color_to_Binding(const ON_Color& color);
ON_Color Binding_to_ON_Color(const BND_Color& color);

BND_Color4f ON_4fColor_to_Binding(const ON_4fColor& color);
ON_4fColor Binding_to_ON_4fColor(const BND_Color4f& color);
