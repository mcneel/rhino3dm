#include "bnd_environment.h"

void BND_Environment_SetBackgroundImage(BND_Environment& environment, const BND_Texture* value)
{
  if (nullptr == environment.m_ptr)
    throw std::runtime_error("Rhino.Render.Environment: no environment");
  environment.m_ptr->SetBackgroundImage(value && value->RH3DM_Const() ? *value->RH3DM_Const() : ON_Texture());
}
