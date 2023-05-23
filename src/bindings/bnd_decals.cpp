
#include "bindings.h"

double BND_File3dmDecal::HorzSweepStart(void) const
{
  double sta = 0.0, end = 0.0;
  _decal->GetHorzSweep(sta, end);
  return sta;
}

double BND_File3dmDecal::HorzSweepEnd(void) const
{
  double sta = 0.0, end = 0.0;
  _decal->GetHorzSweep(sta, end);
  return end;
}

double BND_File3dmDecal::VertSweepStart(void) const
{
  double sta = 0.0, end = 0.0;
  _decal->GetVertSweep(sta, end);
  return sta;
}

double BND_File3dmDecal::VertSweepEnd(void) const
{
  double sta = 0.0, end = 0.0;
  _decal->GetVertSweep(sta, end);
  return end;
}

double BND_File3dmDecal::BoundsMinU(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  return min_u;
}

double BND_File3dmDecal::BoundsMinV(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  return min_v;
}

double BND_File3dmDecal::BoundsMaxU(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  return max_u;
}

double BND_File3dmDecal::BoundsMaxV(void) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  return max_v;
}

void BND_File3dmDecal::SetHorzSweepStart(double v) const
{
  _decal->SetHorzSweep(v, HorzSweepEnd());
}

void BND_File3dmDecal::SetHorzSweepEnd(double v) const
{
  _decal->SetHorzSweep(HorzSweepStart(), v);
}

void BND_File3dmDecal::SetVertSweepStart(double v) const
{
  _decal->SetVertSweep(v, VertSweepEnd());
}

void BND_File3dmDecal::SetVertSweepEnd(double v) const
{
  _decal->SetVertSweep(VertSweepStart(), v);
}

void BND_File3dmDecal::SetBoundsMinU(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  _decal->SetUVBounds(v, min_v, max_u, max_v);
}

void BND_File3dmDecal::SetBoundsMinV(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  _decal->SetUVBounds(min_u, v, max_u, max_v);
}

void BND_File3dmDecal::SetBoundsMaxU(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  _decal->SetUVBounds(min_u, min_v, v, max_v);
}

void BND_File3dmDecal::SetBoundsMaxV(double v) const
{
  double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
  _decal->GetUVBounds(min_u, min_v, max_u, max_v);
  _decal->SetUVBounds(min_u, min_v, max_u, v);
}

BND_File3dmDecalTable::BND_File3dmDecalTable(ON_3dmObjectAttributes* a)
{
  _attr = a;
}

int BND_File3dmDecalTable::Count() const
{
  if (nullptr == _attr)
    return 0;

  return _attr->GetDecalArray().Count();
}

BND_File3dmDecal* BND_File3dmDecalTable::FindIndex(int index)
{
  if (nullptr == _attr)
    return nullptr;

  const auto& decals = _attr->GetDecalArray();

  if ((index < 0) || (index >= decals.Count()))
    return nullptr;

  return new BND_File3dmDecal(decals[index]); // I don't understand the ownership around this object.
}

BND_File3dmDecal* BND_File3dmDecalTable::IterIndex(int index)
{
  return FindIndex(index);
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

#if defined(ON_WASM_COMPILE____TEMP)
using namespace emscripten;

void initDecalBindings(void*)
{
  class_<BND_File3dmDecal>("Decal")
    .constructor<>()
    .constructor<const BND_File3dmDecal&>()
    .property("textureInstanceId", &BND_File3dmDecal::TextureInstanceId, &BND_File3dmDecal::SetTextureInstanceId)
    .property("mapping", &BND_File3dmDecal::Mapping, &BND_File3dmDecal::SetMapping)
    .property("projection", &BND_File3dmDecal::Projection, &BND_File3dmDecal::SetProjection)
    .property("mapToInside", &BND_File3dmDecal::MapToInside, &BND_File3dmDecal::SetMapToInside)
    .property("transparency", &BND_File3dmDecal::Transparency, &BND_File3dmDecal::SetTransparency)
    .property("origin", &BND_File3dmDecal::Origin, &BND_File3dmDecal::SetOrigin)
    .property("vectorUp", &BND_File3dmDecal::VectorUp, &BND_File3dmDecal::SetVectorUp)
    .property("vectorAcross", &BND_File3dmDecal::VectorAcross, &BND_File3dmDecal::SetVectorAcross)
    .property("height", &BND_File3dmDecal::Height, &BND_File3dmDecal::SetHeight)
    .property("radius", &BND_File3dmDecal::Radius, &BND_File3dmDecal::SetRadius)
    .property("horzSweepStart", &BND_File3dmDecal::HorzSweepStart, &BND_File3dmDecal::SetHorzSweepStart)
    .property("horzSweepEnd", &BND_File3dmDecal::HorzSweepEnd, &BND_File3dmDecal::SetHorzSweepEnd)
    .property("vertSweepStart", &BND_File3dmDecal::VertSweepStart, &BND_File3dmDecal::SetVertSweepStart)
    .property("vertSweepEnd", &BND_File3dmDecal::VertSweepEnd, &BND_File3dmDecal::SetVertSweepEnd)
    .property("boundsMinU", &BND_File3dmDecal::BoundsMinU, &BND_File3dmDecal::SetBoundsMinU)
    .property("boundsMinV", &BND_File3dmDecal::BoundsMinV, &BND_File3dmDecal::SetBoundsMinV)
    .property("boundsMaxU", &BND_File3dmDecal::BoundsMaxU, &BND_File3dmDecal::SetBoundsMaxU)
    .property("boundsMaxV", &BND_File3dmDecal::BoundsMaxV, &BND_File3dmDecal::SetBoundsMaxV)
    ;
}
#endif
