"""The surface lock reads the generated nanobind registrations (M2), and the generator's nanobind
stage (tools/generate/nanobind_pass.py) keeps the lock lines of what it rewrites."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'tools', 'generate'))
import surface_lock  # noqa: E402

NANOBIND = '''
  nb::class_<BND_Curve, BND_GeometryBase>(m, "Curve")
      .def(nb::init<const std::vector<BND_Point3d>&>(), nb::arg("points"))
      .def("PointAt", &BND_Curve::Gen_PointAt, nb::arg("t"))
      .def_static("CreateX", &BND_Curve::Gen_CreateX, nb::arg("a"), nb::arg("b"))
      .def_prop_rw("Domain", &BND_Curve::Gen_Domain, &BND_Curve::Gen_SetDomain)
      .def_prop_ro("IsValid", &BND_Curve::Gen_IsValid)
      .def_prop_ro_static("Unset", [](nb::handle) { return 1; })
      .def_rw("X", &BND_Curve::x);
'''
# what the emitters write before the nanobind stage: wrapped getters, a factory constructor, an enum
EMITTED = '''
  py::enum_<ON::LengthUnitSystem>(m, "UnitSystem")
      .value("None", ON::LengthUnitSystem::None);
  py::class_<BND_Curve, BND_GeometryBase>(m, "Curve")
      .def(py::init([](double t) { return BND_Curve(t); }), py::arg("t"))
      .def("PointAt", &BND_Curve::Gen_PointAt, py::arg("t"))
      .def_prop_rw("Domain", py::cpp_function(&BND_Curve::Gen_Domain, py::rv_policy::take_ownership), py::cpp_function(&BND_Curve::Gen_SetDomain, py::keep_alive<1, 2>()))
      .def_prop_ro("Owner", py::cpp_function(&BND_Curve::Gen_Owner, py::rv_policy::reference_internal));
'''
JS = '''
  class_<BND_Curve, base<BND_GeometryBase>>("Curve")
      .function("pointAt", &BND_Curve::Gen_PointAt);
'''


def test_nanobind_lines():
    a = surface_lock.surface({'bnd_curve_gen.cpp': NANOBIND + JS})
    b = surface_lock.surface({'bnd_curve_gen.cpp': NANOBIND.replace('nb::', 'py::') + JS})
    assert a == b
    assert 'py  Curve.Domain [property rw]' in a and 'py  Curve.Unset [property ro static]' in a
    assert 'py  Curve.X [property rw]' in a and 'py  Curve.IsValid [property ro]' in a
    assert 'py  Curve.CreateX(a, b) [static]' in a and 'js  Curve.pointAt [function]' in a


def test_nanobind_pass_keeps_the_lock_lines():
    import nanobind_pass
    out = nanobind_pass.transform(EMITTED)
    assert 'py::cpp_function' not in out
    assert ('.def_prop_rw("Domain", &BND_Curve::Gen_Domain, &BND_Curve::Gen_SetDomain, '
            'py::for_getter(py::rv_policy::take_ownership), py::for_setter(py::keep_alive<1, 2>()))') in out
    assert '.def_prop_ro("Owner", &BND_Curve::Gen_Owner, py::rv_policy::reference_internal)' in out
    assert '.def("__init__", [](BND_Curve* rh3dm_self, double t) { new (rh3dm_self) BND_Curve(' in out
    assert 'm, "UnitSystem", py::is_arithmetic())' in out
    # the factory constructor is unlisted either way; every other line survives
    assert surface_lock.surface({'a.cpp': out + JS}) == surface_lock.surface({'a.cpp': EMITTED + JS})
