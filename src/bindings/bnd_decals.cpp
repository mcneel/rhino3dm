
#include "bindings.h"

BND_File3dmDecal::BND_File3dmDecal()
{
  SetTrackedPointer(new ON_Decal);
}

BND_File3dmDecal::BND_File3dmDecal(const BND_File3dmDecal& decal)
{
  SetTrackedPointer(new ON_Decal(*decal.m_decal));
}

BND_File3dmDecal::BND_File3dmDecal(ON_Decal* decal)
{
  SetTrackedPointer(decal);
}

void BND_File3dmDecal::SetTrackedPointer(ON_Decal* decal)
{
  m_decal = decal;
}

double BND_File3dmDecal::HorzSweepStart(void) const
{
  double sta = 0.0, end = 0.0;
  m_decal->GetHorzSweep(sta, end);
  return sta;
}

double BND_File3dmDecal::HorzSweepEnd(void) const
{
  double sta = 0.0, end = 0.0;
  m_decal->GetHorzSweep(sta, end);
  return end;
}

double BND_File3dmDecal::VertSweepStart(void) const
{
  double sta = 0.0, end = 0.0;
  m_decal->GetVertSweep(sta, end);
  return sta;
}

double BND_File3dmDecal::VertSweepEnd(void) const
{
  double sta = 0.0, end = 0.0;
  m_decal->GetVertSweep(sta, end);
  return end;
}

double BND_File3dmDecal::BoundsMinU(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  return min_u;
}

double BND_File3dmDecal::BoundsMinV(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  return min_v;
}

double BND_File3dmDecal::BoundsMaxU(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  return max_u;
}

double BND_File3dmDecal::BoundsMaxV(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  return max_v;
}

void BND_File3dmDecal::SetHorzSweepStart(double v) const
{
  m_decal->SetHorzSweep(v, HorzSweepEnd());
}

void BND_File3dmDecal::SetHorzSweepEnd(double v) const
{
  m_decal->SetHorzSweep(HorzSweepStart(), v);
}

void BND_File3dmDecal::SetVertSweepStart(double v) const
{
  m_decal->SetVertSweep(v, VertSweepEnd());
}

void BND_File3dmDecal::SetVertSweepEnd(double v) const
{
  m_decal->SetVertSweep(VertSweepStart(), v);
}

void BND_File3dmDecal::SetBoundsMinU(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  m_decal->SetUVBounds(v, min_v, max_u, max_v);
}

void BND_File3dmDecal::SetBoundsMinV(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  m_decal->SetUVBounds(min_u, v, max_u, max_v);
}

void BND_File3dmDecal::SetBoundsMaxU(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  m_decal->SetUVBounds(min_u, min_v, v, max_v);
}

void BND_File3dmDecal::SetBoundsMaxV(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  m_decal->UVBounds(min_u, min_v, max_u, max_v);
  m_decal->SetUVBounds(min_u, min_v, max_u, v);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDecalBindings(pybind11::module& m)
{
  py::class_<BND_File3dmDecal>(m, "Decal")
    .def(py::init<>())
    .def(py::init<const BND_File3dmDecal&>(), py::arg("other"))
    .def_property("TextureInstanceId", &BND_File3dmDecal::TextureInstanceId, &BND_File3dmDecal::SetTextureInstanceId)
    .def_property("Mapping", &BND_File3dmDecal::Mapping, &BND_File3dmDecal::SetMapping)
    .def_property("Projection", &BND_File3dmDecal::Projection, &BND_File3dmDecal::SetProjection)
    .def_property("MapToInside", &BND_File3dmDecal::MapToInside, &BND_File3dmDecal::SetMapToInside)
    .def_property("Transparency", &BND_File3dmDecal::Transparency, &BND_File3dmDecal::SetTransparency)
    .def_property("Origin", &BND_File3dmDecal::Origin, &BND_File3dmDecal::SetOrigin)
    .def_property("VectorUp", &BND_File3dmDecal::VectorUp, &BND_File3dmDecal::SetVectorUp)
    .def_property("VectorAcross", &BND_File3dmDecal::VectorAcross, &BND_File3dmDecal::SetVectorAcross)
    .def_property("Height", &BND_File3dmDecal::Height, &BND_File3dmDecal::SetHeight)
    .def_property("Radius", &BND_File3dmDecal::Radius, &BND_File3dmDecal::SetRadius)
    .def_property("HorzSweepStart", &BND_File3dmDecal::HorzSweepStart, &BND_File3dmDecal::SetHorzSweepStart)
    .def_property("HorzSweepEnd", &BND_File3dmDecal::HorzSweepEnd, &BND_File3dmDecal::SetHorzSweepEnd)
    .def_property("VertSweepStart", &BND_File3dmDecal::VertSweepStart, &BND_File3dmDecal::SetVertSweepStart)
    .def_property("VertSweepEnd", &BND_File3dmDecal::VertSweepEnd, &BND_File3dmDecal::SetVertSweepEnd)
    .def_property("BoundsMinU", &BND_File3dmDecal::BoundsMinU, &BND_File3dmDecal::SetBoundsMinU)
    .def_property("BoundsMinV", &BND_File3dmDecal::BoundsMinV, &BND_File3dmDecal::SetBoundsMinV)
    .def_property("BoundsMaxU", &BND_File3dmDecal::BoundsMaxU, &BND_File3dmDecal::SetBoundsMaxU)
    .def_property("BoundsMaxV", &BND_File3dmDecal::BoundsMaxV, &BND_File3dmDecal::SetBoundsMaxV)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDecalBindings(void*)
{
  class_<BND_File3dmDecal>("Decal")
    .constructor<>()
    .constructor<const BND_File3dmDecal&>()
    .property("TextureInstanceId", &BND_File3dmDecal::TextureInstanceId, &BND_File3dmDecal::SetTextureInstanceId)
    .property("Mapping", &BND_File3dmDecal::Mapping, &BND_File3dmDecal::SetMapping)
    .property("Projection", &BND_File3dmDecal::Projection, &BND_File3dmDecal::SetProjection)
    .property("MapToInside", &BND_File3dmDecal::MapToInside, &BND_File3dmDecal::SetMapToInside)
    .property("Transparency", &BND_File3dmDecal::Transparency, &BND_File3dmDecal::SetTransparency)
    .property("Origin", &BND_File3dmDecal::Origin, &BND_File3dmDecal::SetOrigin)
    .property("VectorUp", &BND_File3dmDecal::VectorUp, &BND_File3dmDecal::SetVectorUp)
    .property("VectorAcross", &BND_File3dmDecal::VectorAcross, &BND_File3dmDecal::SetVectorAcross)
    .property("Height", &BND_File3dmDecal::Height, &BND_File3dmDecal::SetHeight)
    .property("Radius", &BND_File3dmDecal::Radius, &BND_File3dmDecal::SetRadius)
    .property("HorzSweepStart", &BND_File3dmDecal::HorzSweepStart, &BND_File3dmDecal::SetHorzSweepStart)
    .property("HorzSweepEnd", &BND_File3dmDecal::HorzSweepEnd, &BND_File3dmDecal::SetHorzSweepEnd)
    .property("VertSweepStart", &BND_File3dmDecal::VertSweepStart, &BND_File3dmDecal::SetVertSweepStart)
    .property("VertSweepEnd", &BND_File3dmDecal::VertSweepEnd, &BND_File3dmDecal::SetVertSweepEnd)
    .property("BoundsMinU", &BND_File3dmDecal::BoundsMinU, &BND_File3dmDecal::SetBoundsMinU)
    .property("BoundsMinV", &BND_File3dmDecal::BoundsMinV, &BND_File3dmDecal::SetBoundsMinV)
    .property("BoundsMaxU", &BND_File3dmDecal::BoundsMaxU, &BND_File3dmDecal::SetBoundsMaxU)
    .property("BoundsMaxV", &BND_File3dmDecal::BoundsMaxV, &BND_File3dmDecal::SetBoundsMaxV)
    ;
}
#endif
