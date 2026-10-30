"""Hand members (G123: gen_registry.HAND_MEMBERS, tools/generate/emit_hand.py): plain C++
free functions over a generated class or value type, registered in every binding from one
description, and kept binding-free.

    python3 -m pytest tests/generator
"""

import os
import sys

import pytest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'tools', 'generate'))

import gen_registry as R          # noqa: E402
import emit_hand as H             # noqa: E402


@pytest.fixture
def members(monkeypatch):
    table = {}
    monkeypatch.setattr(R, 'HAND_MEMBERS', table)
    monkeypatch.setattr(H, 'HAND_MEMBERS', table)
    return table


def test_every_kind_in_both_bindings(members):
    members['FooX'] = [
        {'name': 'Encode', 'kind': 'method', 'fn': 'F_Encode', 'header': 'hand/x.h'},
        {'name': 'Decode', 'kind': 'static', 'fn': 'F_Decode', 'header': 'hand/x.h', 'args': ['jsonObject']},
        {'name': 'Items', 'kind': 'property', 'fn': 'F_Items', 'header': 'hand/x.h', 'keep_alive': True},
        {'name': 'Size', 'kind': 'property', 'fn': 'F_Size', 'set': 'F_SetSize', 'header': 'hand/x.h'},
    ]
    py, js = H.hand_registrations('FooX')
    assert py == ['      .def("Encode", &F_Encode)',
                  '      .def_static("Decode", &F_Decode, py::arg("jsonObject"))',
                  '      .def_property_readonly("Items", py::cpp_function(&F_Items, py::keep_alive<0, 1>()))',
                  '      .def_property("Size", py::cpp_function(&F_Size), &F_SetSize)']
    assert js == ['      .function("encode", &F_Encode, allow_raw_pointers())',
                  '      .class_function("decode", &F_Decode, allow_raw_pointers())',
                  '      .property("items", &F_Items, allow_raw_pointers())',
                  '      .property("size", &F_Size, &F_SetSize)']


def test_value_type_members_are_js_class_functions(members):
    # JS values are plain objects: a method takes the value first, as generated ones do (G38)
    members['BoxX'] = [{'name': 'Encode', 'kind': 'method', 'fn': 'B_Encode', 'header': 'hand/x.h'}]
    py, js = H.hand_registrations('BoxX', value=True)
    assert py == ['      .def("Encode", &B_Encode)']
    assert js == ['      .class_function("encode", &B_Encode, allow_raw_pointers())']
    members['BoxX'] = [{'name': 'Size', 'kind': 'property', 'fn': 'B_Size', 'header': 'hand/x.h'}]
    with pytest.raises(SystemExit, match='no JS properties'):
        H.hand_registrations('BoxX', value=True)


def test_one_language_only(members):
    members['FooX'] = [{'name': 'Read', 'kind': 'method', 'fn': 'F_Read', 'header': 'hand/x.h', 'js': None},
                       {'name': 'Destroy', 'kind': 'method', 'fn': 'F_Destroy', 'header': 'hand/x.h', 'py': False},
                       {'name': 'Rdk', 'kind': 'method', 'fn': 'F_Rdk', 'header': 'hand/x.h', 'js': 'rdkXml'}]
    py, js = H.hand_registrations('FooX')
    assert py == ['      .def("Read", &F_Read)', '      .def("Rdk", &F_Rdk)']
    assert js == ['      .function("destroy", &F_Destroy, allow_raw_pointers())',
                  '      .function("rdkXml", &F_Rdk, allow_raw_pointers())']
    assert H.hand_names('FooX') == ({'Read', 'Rdk'}, {'destroy', 'rdkXml'})


def _src(tmp_path, files):
    for rel, text in files.items():
        p = tmp_path / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text)
    return str(tmp_path)


def test_check_accepts_a_well_formed_entry(members, tmp_path):
    members['FooX'] = [{'name': 'Encode', 'kind': 'method', 'fn': 'F_Encode', 'header': 'hand/x.h'}]
    root = _src(tmp_path, {'hand/x.h': 'BND_DICT F_Encode(const BND_FooX& f);\n',
                           'hand/x.cpp': '// py:: in a comment is fine\nBND_DICT F_Encode(const BND_FooX& f) { return {}; }\n',
                           'hand/hand_common.h': '#if defined(ON_PYTHON_COMPILE)\n  py::dict d;\n#endif\n'})
    R.check_hand_members({'FooX'}, root)


@pytest.mark.parametrize('entry, regs, files, msg', [
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h'}, set(),
     {'hand/x.h': 'int F_E();'}, 'not a generated class'),
    ({'name': 'E', 'kind': 'getter', 'fn': 'F_E', 'header': 'hand/x.h'}, {'FooX'},
     {'hand/x.h': 'int F_E();'}, "kind 'getter'"),
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h', 'keepalive': True}, {'FooX'},
     {'hand/x.h': 'int F_E();'}, 'unknown keys'),
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h'}, {'FooX'},
     {'hand/x.h': 'int F_Other();'}, 'F_E is not declared'),
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/y.h'}, {'FooX'},
     {'hand/x.h': 'int F_E();'}, 'no header hand/y.h'),
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h', 'set': 'F_S'}, {'FooX'},
     {'hand/x.h': 'int F_E(); void F_S();'}, 'a setter on a method'),
    # binding-free: only the shim may name a binding
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h'}, {'FooX'},
     {'hand/x.h': 'int F_E();', 'hand/x.cpp': 'int F_E() { py::dict d; return 0; }'}, 'names a binding'),
    ({'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h'}, {'FooX'},
     {'hand/x.h': '#if defined(ON_WASM_COMPILE)\nint F_E();\n#endif'}, 'names a binding'),
])
def test_check_refuses(members, tmp_path, entry, regs, files, msg):
    members['FooX'] = [entry]
    with pytest.raises(SystemExit, match=msg):
        R.check_hand_members(regs, _src(tmp_path, files))


def test_the_tree_passes():
    # the real registry against the real src/: what bnd_gen enforces
    root = os.path.join(HERE, '..', '..', 'src')
    regs = set(R.HAND_MEMBERS)          # registration of each reg is checked by bnd_gen itself
    R.check_hand_members(regs, root)


def test_hand_class_is_a_holder_of_statics(members, monkeypatch):
    # a hand class with no generated counterpart (ArchivableDictionaryX): an empty holder for its statics
    monkeypatch.setattr(H, 'HAND_CLASSES', ['DictX'])
    members['DictX'] = [{'name': 'EncodeDict', 'kind': 'static', 'fn': 'D_Enc', 'header': 'hand/x.h', 'args': ['dict']}]
    inc, py, js = H.hand_class_registrations()
    assert inc == '#include "../hand/x.h"\nstruct RH3DM_Hand_DictXHolder {};\n'
    assert py == '  py::class_<RH3DM_Hand_DictXHolder>(m, "DictX")\n      .def_static("EncodeDict", &D_Enc, py::arg("dict"));\n'
    assert js == '  class_<RH3DM_Hand_DictXHolder>("DictX")\n      .class_function("encodeDict", &D_Enc, allow_raw_pointers());\n'
    members['DictX'].append({'name': 'Size', 'kind': 'method', 'fn': 'D_Size', 'header': 'hand/x.h'})
    with pytest.raises(SystemExit, match='statics only'):
        H.hand_class_registrations()


def test_alias_is_a_js_post_script_entry(members, monkeypatch, tmp_path):
    # G124: a JS-only name calling another method (File3dmX.destroy -> embind's delete): no C++
    members['FooX'] = [{'name': 'destroy', 'kind': 'alias', 'of': 'delete', 'py': False}]
    assert H.hand_registrations('FooX') == ([], [])
    assert H.hand_headers('FooX') == []
    assert H.hand_js_aliases() == [('FooX', 'destroy', 'delete')]
    assert H.hand_names('FooX') == (set(), {'destroy'})
    R.check_hand_members({'FooX'}, str(tmp_path))
    members['FooX'] = [{'name': 'destroy', 'kind': 'alias', 'of': 'delete'}]
    with pytest.raises(SystemExit, match='JS only'):
        R.check_hand_members({'FooX'}, str(tmp_path))
    members['FooX'] = [{'name': 'E', 'kind': 'method', 'fn': 'F_E', 'header': 'hand/x.h', 'of': 'x'}]
    with pytest.raises(SystemExit, match="'of' belongs to an alias"):
        R.check_hand_members({'FooX'}, _src(tmp_path, {'hand/x.h': 'int F_E();'}))


def test_overload_is_python_after_the_generated_and_js_by_its_own_name(members, tmp_path):
    # G126: an overload RhinoCommon lacks (PointCloud.Add(point, value)): Python joins the generated
    # member's overloads AFTER them (pybind11 tries in order; a py::object Guid accepts anything);
    # JS needs its own name (embind keeps one function per arity)
    members['FooX'] = [{'name': 'Add', 'kind': 'method', 'fn': 'F_AddV', 'header': 'hand/x.h',
                        'args': ['point', 'value'], 'overload': True, 'js': 'addPointValue'}]
    assert H.hand_registrations('FooX') == ([], ['      .function("addPointValue", &F_AddV, allow_raw_pointers())'])
    assert H.hand_registrations('FooX', overloads=True) == (
        ['      .def("Add", &F_AddV, py::arg("point"), py::arg("value"))'], [])
    assert H.hand_names('FooX', strict=True) == (set(), {'addPointValue'})
    assert H.hand_names('FooX')[0] == {'Add'}
    R.check_hand_members({'FooX'}, _src(tmp_path, {'hand/x.h': 'void F_AddV();'}))
    members['FooX'][0].pop('js')
    with pytest.raises(SystemExit, match='its own JS name'):
        R.check_hand_members({'FooX'}, str(tmp_path))


def test_hand_type_registers_its_instance_members(members, monkeypatch):
    # G129: a hand TYPE -- a C++ class in src/hand/ with instance members (the SubD neighbour cursors);
    # members marked keep_alive hold their object in JS too; `enumerable` gives JS for...of
    monkeypatch.setattr(H, 'HAND_CLASSES', [{'name': 'CurX', 'cpp': 'RH3DM_Cur', 'header': 'hand/c.h', 'enumerable': True}])
    members['CurX'] = [{'name': 'Count', 'kind': 'property', 'fn': 'C_Count', 'header': 'hand/c.h'},
                       {'name': '__getitem__', 'kind': 'method', 'fn': 'C_Item', 'header': 'hand/c.h', 'args': ['index'],
                        'keep_alive': True, 'js': 'get'}]
    inc, py, js = H.hand_class_registrations()
    assert inc == '#include "../hand/c.h"\n'
    assert py.startswith('  py::class_<RH3DM_Cur>(m, "CurX")\n') and '.def("__getitem__", &C_Item, py::keep_alive<0, 1>(), py::arg("index"))' in py
    assert js.startswith('  class_<RH3DM_Cur>("CurX")\n') and '.function("get", &C_Item, allow_raw_pointers())' in js
    monkeypatch.setattr(R, 'HAND_CLASSES', H.HAND_CLASSES)
    assert H.hand_js_post() == ([('CurX', 'get')], ['CurX'])
