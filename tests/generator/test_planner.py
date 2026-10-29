"""Unit tests for the binding generator's planner (tools/generate/planner.py).

The parity tests check generated code end to end against .NET; these pin
the PLANNER's decisions on synthetic manifest records, so a change to the
order-sensitive shape selection fails here in a second instead of as a
silently missing member after a 30-minute build. Each test names the
generator-log entry it guards.

    python3 -m pytest tests/generator
"""

import os
import sys

import pytest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'tools', 'generate'))

import gen_registry as R          # noqa: E402
import planner as P               # noqa: E402

VT = {
    'types': [
        {'Cs': 'Point3d', 'Cpp': 'BND_Point3dX', 'IsStaticClass': False, 'LayoutSize': 24,
         'Fields': [{'Name': 'm_x', 'Type': 'double'}, {'Name': 'm_y', 'Type': 'double'},
                    {'Name': 'm_z', 'Type': 'double'}]},
        {'Cs': 'Interval', 'Cpp': 'BND_IntervalX', 'IsStaticClass': False, 'LayoutSize': 16,
         'Fields': [{'Name': 'm_t0', 'Type': 'double'}, {'Name': 'm_t1', 'Type': 'double'}]},
        {'Cs': 'Plane', 'Cpp': 'BND_PlaneX', 'IsStaticClass': False, 'LayoutSize': 128,
         'Fields': [{'Name': 'm_origin', 'Type': 'BND_Point3dX'}]},
    ],
    'members': [
        {'Type': 'Interval', 'Name': 'Unset', 'Kind': 'property', 'Static': True, 'Ok': True},
        {'Type': 'Interval', 'Name': 'Min', 'Kind': 'property', 'Static': False, 'Ok': True,
         'Ret': 'double', 'Def': ''},
        {'Type': 'Plane', 'Name': 'WorldXY', 'Kind': 'property', 'Static': True, 'Ok': True},
    ],
    'enums': [],
}
R.register_value_types(VT)

CURVE = R.BY_CS['Curve']


def member(sig_ret, params=(), calls=None, **kw):
    m = {'Type': CURVE['type'], 'Kind': 'method', 'Name': 'M', 'Variant': 'portable',
         'Body': 'trivial', 'Returns': sig_ret,
         'Params': [{'Name': n, 'Type': t, 'Mode': mode} for mode, t, n in params],
         'Calls': [{'Name': 'F', 'Args': calls or []}]}
    m.update(kw)
    return m


def cfn(ret, *ptypes):
    return {'name': 'F', 'returns': ret, 'params': [{'type': t, 'name': 'p%d' % i}
                                                    for i, t in enumerate(ptypes)]}


SELF = {'Kind': 'expr', 'Text': 'ptr', 'Value': None}


def test_guid_return_is_converted_not_shadowed_by_out_branch():
    # G19/G20 era: the Guid out-branch once shadowed the direct ON_UUID return
    p, why = P.plan(member('Guid', calls=[SELF]), cfn('ON_UUID', 'const ON_Curve*'), CURVE)
    assert why is None and p['shape'][0] == 'conv' and 'ON_UUID_to_Binding' in p['shape'][2]


def test_bool_from_int_uses_the_recorded_comparison():
    # G19: `return rc != 0` vs `== 1` -- never assumed
    for cmp in ('!= 0', '== 1'):
        p, why = P.plan(member('bool', calls=[SELF], Compare=cmp), cfn('int', 'const ON_Curve*'), CURVE)
        assert why is None and p['shape'][2].endswith(cmp)
    p, why = P.plan(member('bool', calls=[SELF]), cfn('int', 'const ON_Curve*'), CURVE)
    assert p is None                                # no recorded comparison: refuse


def test_direct_return_wins_over_scratch_out_local():
    # IsPlanar(tol): `Plane plane; return F(ptr, ref plane, tol)` -- bool C return,
    # a scratch VT out local; the direct-return precedence regressed once
    calls = [SELF, {'Kind': 'out', 'Text': 'plane', 'Value': None}, {'Kind': 'param', 'Text': 'tol'}]
    p, why = P.plan(member('bool', [('', 'double', 'tol')], calls),
                    cfn('bool', 'const ON_Curve*', 'ON_PLANE_STRUCT*', 'double'), CURVE)
    assert why is None and p['shape'] == ('direct', 'bool')
    assert 'BND_PlaneX plane;' in p['locals'] and 'RH3DM_Ptr(&plane)' in p['call_exprs']


def test_out_local_initializer_is_reproduced_or_refused():
    # G18: `Interval rc = Interval.Unset; F(ptr, ref rc)` keeps its initializer
    calls = [SELF, {'Kind': 'out', 'Text': 'rc', 'Value': 'Interval.Unset'}]
    p, why = P.plan(member('Interval', calls=calls), cfn('bool', 'const ON_Curve*', 'ON_Interval*'), CURVE)
    assert why is None and 'BND_IntervalX rc = BND_IntervalX::Unset();' in p['locals']
    calls[1]['Value'] = 'SomethingUnknown.Value'     # cannot reproduce: refuse, never default
    p, why = P.plan(member('Interval', calls=calls), cfn('bool', 'const ON_Curve*', 'ON_Interval*'), CURVE)
    assert p is None and 'initialized' in why


def test_ref_of_by_value_value_type_passes_the_parameter_itself():
    # a by-value VT parameter IS the method's copy: no local (ctors cannot have one)
    calls = [SELF, {'Kind': 'out', 'Text': 'plane'}]
    p, why = P.plan(member('bool', [('', 'Plane', 'plane')], calls),
                    cfn('bool', 'const ON_Curve*', 'const ON_PLANE_STRUCT*'), CURVE)
    assert why is None and p['locals'] == [] and 'RH3DM_Ptr(&plane)' in p['call_exprs']
    assert ('BND_PlaneX', 'plane') in p['bind_args']


def test_null_literal_to_c_pointer_is_nullptr():
    # G21
    calls = [SELF, {'Kind': 'literal', 'Text': 'null'}]
    p, why = P.plan(member('bool', calls=calls), cfn('bool', 'const ON_Curve*', 'const ON_Curve*'), CURVE)
    assert why is None and 'nullptr' in p['call_exprs']


def test_forward_refuses_null_and_passes_out_params_through():
    target_m = member('bool', [('out', 'Plane', 'plane'), ('', 'double', 'tolerance')])
    target_m['Name'] = 'TryGetPlane'
    target = {'m': target_m, 'shape': ('tuple', True, []), 'static': False, 'bind_args': [('double', 'tolerance')]}
    by = {('TryGetPlane', 2): [target]}
    fwd = member('bool', [('out', 'Plane', 'plane')], Body='delegates', Delegates='TryGetPlane',
                 DelegateArgs=[{'Kind': 'param', 'Text': 'plane'},
                               {'Kind': 'expr', 'Text': 'RhinoMath.ZeroTolerance'}])
    fwd['Name'] = 'TryGetPlane'
    p, why = P.plan_forward(fwd, CURVE, by)
    assert why is None and p['bind_args'] == [] \
        and p['forward_args'] == ['RH3DM_RhinoMath::ZeroTolerance']
    fwd['DelegateArgs'][1] = {'Kind': 'literal', 'Text': 'null'}
    p, why = P.plan_forward(fwd, CURVE, by)
    assert p is None and why == 'null literal'


@pytest.mark.parametrize('t,expect', [('Rhino.Geometry.Plane', 'Plane'),
                                      ('Geometry.Plane', 'Plane'),
                                      ('DimensionStyle.ArrowFit', 'DimensionStyle.ArrowFit')])
def test_unqualify_keeps_nested_names(t, expect):
    assert R.cs_unqualify(t) == expect


def test_color_uses_the_recorded_conversion_or_refuses():
    # G24: the int encoding is the C# call site's choice (FromArgb vs ColorFromWin32)
    for conv in ('FromArgb', 'FromWin32'):
        p, why = P.plan(member('System.Drawing.Color', calls=[SELF], ResultConv=conv),
                        cfn('int', 'const ON_Curve*'), CURVE)
        assert why is None and p['shape'] == ('conv', 'BND_Color', 'RH3DM_Color%s({c})' % conv)
    p, why = P.plan(member('System.Drawing.Color', calls=[SELF]), cfn('int', 'const ON_Curve*'), CURVE)
    assert p is None                                  # no recorded conversion: refuse
    setter = [SELF, {'Kind': 'paramconv', 'Text': 'value', 'Value': 'ToArgb'}]
    p, why = P.plan(member('void', [('', 'Color', 'value')], setter), cfn('void', 'ON_Curve*', 'int'), CURVE)
    assert why is None and 'RH3DM_ColorToArgb(value)' in p['call_exprs']
    plain = [SELF, {'Kind': 'param', 'Text': 'value'}]
    p, why = P.plan(member('void', [('', 'Color', 'value')], plain), cfn('void', 'ON_Curve*', 'int'), CURVE)
    assert p is None and 'conversion' in why


def test_static_holder_members_are_emitted_static():
    # G26: the holder shapes (string / array / out-string tuple) dropped `static`
    import emit_classes as E
    calls = [{'Kind': 'param', 'Text': 's'}, {'Kind': 'expr', 'Text': 'ptr_out'}]
    m = member('string', [('', 'string', 's')], calls, Static=True, Holder='StringWrapper', HolderArg=1)
    p, why = P.plan(m, cfn('void', 'const RHMONO_STRING*', 'ON_wString*'), CURVE)
    assert why is None and p['shape'][0] == 'holder'
    p['cpp'] = 'Gen_M'
    decl, _ = E.emit_decl_def(p, CURVE)
    assert decl.strip().startswith('static std::wstring Gen_M(')


def test_failure_fallback_literal_and_value_type():
    # G28: `if (!F(ptr, ref d)) d = 0; return d;` and `if (!F(...)) rc = Point3d.Unset;`
    # are the Success idiom; a literal fallback suits a plain C value return only
    out_d = {'Kind': 'out', 'Text': 'd', 'Value': '0.0'}
    p, why = P.plan(member('double', calls=[SELF, out_d],
                           Success={'Out': 'd', 'Fallback': '0', 'Form': 'assign'}),
                    cfn('bool', 'const ON_Curve*', 'double*'), CURVE)
    assert why is None and p['shape'][0] == 'fromout' and p['success']['Fallback'] == '0'
    out_rc = {'Kind': 'out', 'Text': 'rc', 'Value': None}
    p, why = P.plan(member('Point3d', calls=[SELF, out_rc],
                           Success={'Out': 'rc', 'Fallback': 'Interval.Unset', 'Form': 'assign'}),
                    cfn('bool', 'const ON_Curve*', 'ON_3dPoint*'), CURVE)
    assert p is None and why.startswith('fallback')       # a static of ANOTHER type: refuse


def test_early_return_guards_need_a_matching_value():
    # G28: `if (COND) return X;` -- X must be expressible in the member's own shape
    guard = {'Cond': 'Count == 0', 'Ids': ['Count'], 'Exception': None, 'Return': 'false'}
    p, why = P.plan(member('bool', calls=[SELF], Guards=[guard]),
                    cfn('bool', 'const ON_Curve*'), CURVE)
    assert why is None and p['guards'][0]['ret_cpp'] == 'false'
    p, why = P.plan(member('int', calls=[SELF], Guards=[dict(guard, Return='false')]),
                    cfn('int', 'const ON_Curve*'), CURVE)
    assert p is None and why.startswith('early return')    # bool literal for an int: refuse
    p, why = P.plan(member('void', calls=[SELF], Guards=[dict(guard, Return='')]),
                    cfn('void', 'const ON_Curve*'), CURVE)
    assert why is None and p['guards'][0]['ret_cpp'] == ''


def test_early_return_on_null_class_argument_makes_it_optional():
    # G28: `if (null == curve) return false;` -- reachable only through an optional argument
    guard = {'Cond': 'null == curve', 'Ids': ['curve'], 'Exception': None, 'Return': 'false',
             'NullCheckOf': 'curve'}
    arg = {'Kind': 'paramptr', 'Text': 'curve', 'Value': None}
    p, why = P.plan(member('bool', params=[('', 'Curve', 'curve')], calls=[SELF, arg], Guards=[guard]),
                    cfn('bool', 'ON_Curve*', 'const ON_Curve*'), CURVE)
    assert why is None and p['bind_args'] == [('const BND_CurveX*', 'curve')]
    assert 'curve ?' in p['call_exprs'][1]
    guard2 = dict(guard, Cond='null == name', Ids=['name'], NullCheckOf='name')
    p, why = P.plan(member('bool', params=[('', 'string', 'name')],
                           calls=[SELF, {'Kind': 'param', 'Text': 'name'}], Guards=[guard2]),
                    cfn('bool', 'ON_Curve*', 'const RHMONO_STRING*'), CURVE)
    assert p is None and 'non-class' in why               # a string cannot carry null: refuse


def test_self_null_guard_tests_the_self_pointer():
    # G28: `if (IntPtr.Zero == pConstThis) return String.Empty;` -- a model-backed
    # proxy's pointer can be null; a value-mode member's address cannot
    guard = {'Cond': 'IntPtr.Zero == pConstThis', 'Ids': [], 'Exception': None,
             'Return': 'String.Empty', 'SelfNull': True}
    holder = {'Kind': 'expr', 'Text': 'pString', 'Value': None}
    p, why = P.plan(member('string', calls=[SELF, holder], Guards=[guard], Body='pattern',
                           Holder='StringHolder', HolderArg=1),
                    cfn('void', 'const ON_Curve*', 'CRhCmnStringHolder*'), CURVE)
    assert why is None and p['guards'][0]['cond_cpp'] == '!(m_ptr)'
    assert p['guards'][0]['ret_cpp'] == 'std::wstring()'


def test_guard_dotted_ids_enum_constant_and_value_type_property():
    # G28: `iso == IsoStatus.None` (valued by specgen) and `domain.Min` (a translated
    # property); a plain sibling `Count` must not rewrite the tail of a dotted id
    cnt = dict(member('int', calls=[SELF]), Name='Count', Kind='property', Signature='int Count',
               Calls=[{'Name': 'G', 'Args': [SELF]}])
    g = {'Cond': 'iso == IsoStatus.None || domain.Min > Count', 'Exception': None, 'Return': 'false',
         'Ids': ['Count', 'IsoStatus.None', 'domain.Min', 'iso'], 'IdValues': {'IsoStatus.None': '0'}}
    m = dict(member('bool', params=[('', 'IsoStatus', 'iso'), ('', 'Interval', 'domain')],
                    calls=[SELF, {'Kind': 'param', 'Text': 'iso'}]),
             Signature='bool M(IsoStatus iso, Interval domain)', Guards=[g])
    fns = {'F': cfn('bool', 'const ON_Curve*', 'int'), 'G': dict(cfn('int', 'const ON_Curve*'), name='G')}
    plans = P.plan_class(CURVE, {'members': [cnt, m]}, fns, {})
    pm = [p for p in plans if p['m']['Name'] == 'M'][0]
    assert pm['guards'][0]['cond_cpp'] == \
        'iso == (RH3DM_IsoStatus)0 || domain.Min() > Gen_Count()'


# ---- File3dm tables (G29): generic bases instantiated per concrete table
LAYERS = R.BY_CS['File3dmLayerTable']
LAYER = R.BY_CS['Layer']


def _tables_manifest(extra=()):
    T = 'Rhino.FileIO.'
    recs = [
        {'Type': T + 'CommonComponentTable', 'Kind': 'type', 'Name': 'CommonComponentTable',
         'Bases': ['IList'], 'BaseArgs': [['T']], 'TypeParams': ['T'], 'Variant': 'portable'},
        {'Type': T + 'File3dmCommonComponentTable', 'Kind': 'type', 'Name': 'File3dmCommonComponentTable',
         'Bases': ['CommonComponentTable', 'ICollection'], 'BaseArgs': [['T'], ['T']], 'TypeParams': ['T'],
         'Variant': 'portable'},
        {'Type': T + 'File3dmLayerTable', 'Kind': 'type', 'Name': 'File3dmLayerTable',
         'Bases': ['File3dmCommonComponentTable', 'IList'], 'BaseArgs': [['Layer'], ['Layer']], 'Variant': 'portable'},
        {'Type': 'Rhino.DocObjects.Layer', 'Kind': 'type', 'Name': 'Layer', 'Bases': ['ModelComponent'],
         'Variant': 'portable', 'FileProxy': {'Resolver': 'ONX_Model_GetModelComponentPointer', 'NonConst': 'resolve'}},
        {'Type': T + 'File3dmLayerTable', 'Kind': 'property', 'Name': 'ComponentType', 'Variant': 'portable',
         'Const': 'ModelComponentType.Layer', 'ConstValue': '5', 'Returns': 'ModelComponentType'},
        {'Type': 'Rhino.DocObjects.ModelComponentType', 'Kind': 'enum', 'Name': 'ModelComponentType',
         'Variant': 'portable', 'Values': ['Unset = 0', 'Material = 3', 'RenderMaterial = Material', 'Layer = 5']},
    ]
    return {'members': recs + list(extra)}


@pytest.fixture
def tables():
    m = _tables_manifest()
    R.index_types(m)
    yield m
    R.index_types({'members': []})       # leave no 'proxy' marks behind


def test_generic_base_members_are_instantiated_per_concrete_table(tables):
    dump = {'Type': 'Rhino.FileIO.File3dmCommonComponentTable', 'Kind': 'method', 'Name': 'Delete',
            'Variant': 'portable', 'Returns': 'bool', 'Signature': 'bool Delete(T item)',
            'Params': [{'Name': 'item', 'Type': 'T', 'Mode': ''}]}
    over = dict(dump, Type=LAYERS['type'], Signature='bool Delete(Layer item)',
                Params=[{'Name': 'item', 'Type': 'Layer', 'Mode': ''}], Body='own')
    got = P.class_members(LAYERS, {'members': tables['members'] + [dump]})
    inst = [x for x in got if x['Name'] == 'Delete']
    assert len(inst) == 1 and inst[0]['Params'][0]['Type'] == 'Layer' and inst[0]['Type'] == LAYERS['type']
    got = P.class_members(LAYERS, {'members': tables['members'] + [dump, over]})
    assert [x.get('Body') for x in got if x['Name'] == 'Delete'] == ['own']    # the override wins
    assert R.ENUM_VALUES['ModelComponentType'] == [0, 3, 5]                     # alias resolved


def test_class_constant_argument_and_layer_proxy(tables):
    assert LAYER.get('proxy') and not R.BY_CS['Curve'].get('proxy')
    ct = {'Kind': 'expr', 'Text': 'ComponentType', 'Value': None}
    selfm = {'Kind': 'expr', 'Text': 'pParent', 'Value': None}
    m = dict(member('int', calls=[selfm, ct]), Type=LAYERS['type'])
    p, why = P.plan(m, cfn('int', 'ONX_Model*', 'ON_ModelComponent::Type'), LAYERS)
    assert why is None and p['call_exprs'][1] == '(ON_ModelComponent::Type)5 /*Layer*/'
    nc = {'Kind': 'expr', 'Text': 'ptr', 'Value': 'NonConstPointer'}
    p, why = P.plan(dict(member('void', calls=[nc]), Type=LAYER['type']), cfn('void', 'ON_Layer*'), LAYER)
    assert p['call_exprs'][0] == 'RH3DM_NonConst()'


def test_nested_native_call_and_result_cast(tables):
    R.C_BY_NAME['G'] = {'name': 'G', 'returns': 'const ON_ComponentManifest*',
                        'params': [{'type': 'const ONX_Model*', 'name': 'm'}]}
    nested = {'Kind': 'nativecall', 'Text': 'GetPtr', 'Call': {'Name': 'G', 'Args': [{'Kind': 'expr', 'Text': 'p'}]}}
    m = dict(member('int', calls=[nested], ResultCast='int'), Type=LAYERS['type'])
    p, why = P.plan(m, cfn('unsigned int', 'const ON_ComponentManifest*'), LAYERS)
    assert why is None and p['call_exprs'] == ['G(m_ptr)'] and p['shape'][2] == '(int)({c})'
    p, why = P.plan(dict(m, ResultCast=None), cfn('unsigned int', 'const ON_ComponentManifest*'), LAYERS)
    assert p is None                                   # uint -> int without the recorded cast: refuse


def test_factory_builds_the_tables_own_proxy_or_refuses(tables):
    fac = {'Name': 'Instantiate', 'SwitchArg': 1,
           'Args': [{'Kind': 'expr', 'Text': 'm_parent'}, {'Kind': 'expr', 'Text': 'ComponentType'},
                    {'Kind': 'expr', 'Text': 'id'}, {'Kind': 'param', 'Text': 'index'}],
           'Cases': {'5': {'New': 'Layer', 'Args': ['id', 'm_parent']}, '3': None}}
    calls = [{'Kind': 'expr', 'Text': 'file3dm_ptr'}, {'Kind': 'param', 'Text': 'index'},
             {'Kind': 'out', 'Text': 'id', 'Value': 'default(Guid)'}]
    m = dict(member('Layer', params=[('', 'int', 'index')], calls=calls, Factory=fac, Body='pattern'),
             Type=LAYERS['type'])
    fn = cfn('const ON_ModelComponent*', 'const ONX_Model*', 'int', 'ON_UUID*')
    p, why = P.plan(m, fn, LAYERS)
    assert why is None and p['shape'][0] == 'proxywrap' and p['locals'] == ['ON_UUID id = ON_nil_uuid;']
    p, why = P.plan(dict(m, Returns='Material'), fn, LAYERS)
    assert p is None and why.startswith('factory constructs')     # the case is Layer, not T


def test_fail_throw_needs_a_void_member_over_a_bool_call():
    ft = {'Exception': 'NotSupportedException', 'Message': 'Addition failed.'}
    p, why = P.plan(member('void', calls=[SELF], FailThrow=ft, Body='pattern'),
                    cfn('bool', 'ON_Curve*'), CURVE)
    assert why is None and p['shape'][0] == 'failthrow'
    p, why = P.plan(member('void', calls=[SELF], FailThrow=ft, Body='pattern'),
                    cfn('int', 'ON_Curve*'), CURVE)
    assert p is None


def test_child_view_requires_new_T_of_this(tables):
    base = {'Kind': 'property', 'Child': 'File3dmLayerTable', 'ChildArgs': ['this']}
    assert P.child_view(dict(base, Returns='IList<Layer>')) is LAYERS          # an interface it implements
    assert P.child_view(dict(base, Returns='IList<Material>')) is None
    assert P.child_view(dict(base, Returns='File3dmLayerTable', ChildArgs=['this', 'true'])) is None


def test_nested_enum_resolves_in_the_members_own_type():
    # G35: `ArrowType` inside DimensionStyle is DimensionStyle.ArrowType
    R.CS_ENUMS['DimensionStyle.ArrowType'] = {'cpp': 'RH3DM_DimensionStyle_ArrowType', 'gen': True,
                                              'owner': 'DimensionStyle', 'js': 'DimensionStyleArrowType'}
    try:
        ds = {'Type': 'Rhino.DocObjects.DimensionStyle', 'Returns': 'ArrowType',
              'Params': [{'Name': 'a', 'Type': 'ArrowType', 'Mode': ''}]}
        assert R.cs_signature(ds) == ('DimensionStyle.ArrowType', [('', 'DimensionStyle.ArrowType', 'a')])
        other = dict(ds, Type='Rhino.Geometry.Curve')
        assert R.cs_signature(other)[0] == 'ArrowType'          # not nested there
    finally:
        del R.CS_ENUMS['DimensionStyle.ArrowType']


def test_uint_cast_of_an_int_parameter_is_kept():
    # G35: `(uint)count` into an unsigned int
    m = member('void', params=[('', 'int', 'count')], calls=[SELF, {'Kind': 'expr', 'Text': '(uint)count'}])
    p, why = P.plan(m, cfn('void', 'ON_Curve*', 'unsigned int'), CURVE)
    assert why is None and p['call_exprs'][1] == '(unsigned int)count'


def test_fail_throw_then_return_the_out_value():
    # G36: `if (!F(ptr, i, ref rc)) { if (i >= Count) throw ...; } return rc;`
    ft = {'Exception': 'IndexOutOfRangeException', 'Message': None, 'Cond': 'i < 0', 'Ids': ['i']}
    out = {'Kind': 'out', 'Text': 'rc', 'Value': None}
    m = member('Point3d', params=[('', 'int', 'i')], calls=[SELF, {'Kind': 'param', 'Text': 'i'}, out],
               FailThrow=ft, Body='pattern')
    p, why = P.plan(m, cfn('bool', 'const ON_Curve*', 'int', 'ON_3dPoint*'), CURVE)
    assert why is None and p['shape'][0] == 'fromout' and p['failthrow']['Exception'] == ft['Exception']


def test_guard_helper_call_becomes_its_native_call(tables):
    # G37: `!ModelComponent.ModelComponentTypeRequiresUniqueName(ModelComponentType.Layer)`
    R.C_BY_NAME['ON_ModelComponent_RequiresUniqueName'] = {
        'name': 'ON_ModelComponent_RequiresUniqueName', 'returns': 'bool',
        'params': [{'type': 'ON_ModelComponent::Type', 'name': 't'}]}
    g = {'Calls': [{'Name': 'ModelComponent.ModelComponentTypeRequiresUniqueName',
                    'Args': [{'Kind': 'expr', 'Text': 'ModelComponentType.Layer'}]}],
         'Native': [{'Name': 'ON_ModelComponent_RequiresUniqueName',
                     'Args': [{'Kind': 'const', 'Text': 'ModelComponentType.Layer', 'Value': '5'}]}]}
    p = {}
    cond = P.guard_calls_cpp(g, '!ModelComponent.ModelComponentTypeRequiresUniqueName(ModelComponentType.Layer)',
                             LAYERS, set(), {}, p)
    assert cond == '!ON_ModelComponent_RequiresUniqueName((ON_ModelComponent::Type)5)'
    assert P.guard_calls_cpp(dict(g, Native=None), cond, LAYERS, set(), {}, {}) is None   # unresolved: refuse


def test_enum_isdefined_folds_named_members_and_refuses_others(tables):
    R.CS_ENUMS.setdefault('ModelComponentType', {'cpp': 'RH3DM_ModelComponentType', 'gen': True})
    c = '!Enum.IsDefined(typeof(ModelComponentType), ModelComponentType.Layer)'
    assert P.enum_isdefined_cpp(c, LAYERS, {}) == '!true'
    assert P.enum_isdefined_cpp('!Enum.IsDefined(typeof(ModelComponentType), x)', LAYERS, {}) is None


def test_inherited_guards_are_resolved_per_table():
    # G37 review: an inherited member's guard dicts are shared by every table;
    # each plan must resolve its own copy (the constant differs per table)
    T = 'Rhino.FileIO.'
    m = _tables_manifest([
        {'Type': T + 'File3dmMaterialTable', 'Kind': 'type', 'Name': 'File3dmMaterialTable',
         'Bases': ['File3dmCommonComponentTable'], 'BaseArgs': [['Material']], 'Variant': 'portable'},
        {'Type': T + 'File3dmMaterialTable', 'Kind': 'property', 'Name': 'ComponentType', 'Variant': 'portable',
         'Const': 'ModelComponentType.RenderMaterial', 'ConstValue': '3', 'Returns': 'ModelComponentType'},
        {'Type': T + 'File3dmCommonComponentTable', 'Kind': 'method', 'Name': 'Check', 'Variant': 'portable',
         'Body': 'pattern', 'Returns': 'int', 'Signature': 'int Check()', 'Params': [],
         'Calls': [{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'p'}]}],
         'Guards': [{'Cond': 'ModelComponentType.Unset == ComponentType', 'Exception': 'ArgumentException',
                     'Ids': ['ComponentType', 'ModelComponentType.Unset'], 'Message': 'x',
                     'IdValues': {'ModelComponentType.Unset': '0'}}]}])
    R.index_types(m)
    R.CS_ENUMS.setdefault('ModelComponentType', {'cpp': 'RH3DM_ModelComponentType', 'gen': True})
    try:
        fns = {'F': dict(cfn('int', 'ONX_Model*'), name='F')}
        # plan BOTH, then read -- emission happens after every class is planned
        planned = {cs: P.plan_class(R.BY_CS[cs], {'members': P.class_members(R.BY_CS[cs], m)}, fns, {})
                   for cs in ('File3dmLayerTable', 'File3dmMaterialTable')}
        conds = {cs: [p for p in plans if p['m']['Name'] == 'Check'][0]['guards'][0]['cond_cpp']
                 for cs, plans in planned.items()}
        assert conds['File3dmLayerTable'].endswith('(RH3DM_ModelComponentType)5')
        assert conds['File3dmMaterialTable'].endswith('(RH3DM_ModelComponentType)3')
    finally:
        R.index_types({'members': []})


def test_table_protocol_requires_rhinocommons_own_pieces(tables):
    # G39: len/[i]/iteration are granted only when Count, a proxy FindIndex and the
    # base's `foreach` (m_manifest.GetEnumerator<T>) are all there
    T = 'Rhino.FileIO.'
    enum = {'Type': T + 'CommonComponentTable', 'Kind': 'method', 'Name': 'GetEnumerator', 'Variant': 'portable',
            'Body': 'delegates', 'Delegates': '@m_manifest.GetEnumerator<T>', 'Returns': 'IEnumerator<T>'}
    m = {'members': tables['members'] + [enum]}
    for fn in ('ONX_ModelComponentIterator_New', 'ONX_ModelComponentIterator_GetNext', 'ONX_ModelComponentIterator_Delete'):
        R.C_BY_NAME[fn] = {'name': fn, 'returns': 'void', 'params': []}
    count = {'m': {'Name': 'Count', 'Kind': 'property'}, 'bind_args': [], 'cpp': 'Gen_Count', 'shape': ('direct', 'int')}
    find = {'m': {'Name': 'FindIndex', 'Kind': 'method'}, 'bind_args': [('int', 'index')], 'cpp': 'Gen_FindIndex',
            'shape': ('proxywrap', LAYER, 'id')}
    tp = P.table_protocol(LAYERS, m, [count, find])
    assert tp and tp['item'] is LAYER and tp['type_value'] == '5' and tp['find'] == 'Gen_FindIndex'
    assert P.table_protocol(LAYERS, m, [count])['find'] is None             # no FindIndex: iteration and len, no [i]
    assert P.table_protocol(LAYERS, {'members': tables['members']}, [count, find]) is None   # no foreach in the spec


# ---- views (G40): copy-on-write geometry, write-through attributes, chains, multi-type tables

def _views_manifest():
    T = 'Rhino.FileIO.'
    recs = _tables_manifest()['members'] + [
        {'Type': T + 'File3dmObject', 'Kind': 'type', 'Name': 'File3dmObject', 'Bases': ['ModelComponent'],
         'Variant': 'portable', 'Duplicate': 'ON_Object_Duplicate',
         'FileProxy': {'Resolver': 'ONX_Model_ModelObjectGeometryConstPtrFromId', 'NonConst': 'base',
                       'IdField': 'm_id', 'FileField': 'm_parent'}},
        {'Type': 'Rhino.Geometry.GeometryBase', 'Kind': 'type', 'Name': 'GeometryBase', 'Variant': 'portable',
         'Duplicate': 'ON_Object_Duplicate',
         'ParentViews': [{'ParentType': 'File3dmObject', 'Const': 'GetGeometryConstPointer', 'NonConst': None,
                          'ConstNative': {'Name': 'ONX_Model_ModelObjectGeometry',
                                          'Args': [{'Kind': 'fieldptr', 'Text': 'm_parent', 'Value': 'ConstPointer'},
                                                   {'Kind': 'expr', 'Text': 'm_id'}]}}]},
        {'Type': 'Rhino.Geometry.Curve', 'Kind': 'type', 'Name': 'Curve', 'Bases': ['GeometryBase'],
         'Variant': 'portable', 'Duplicate': 'ON_Curve_DuplicateCurve',
         'ParentViews': [{'ParentType': 'CurveHolder', 'Const': 'ConstCurvePointer', 'NonConst': None}]},
        {'Type': 'Rhino.DocObjects.ObjectAttributes', 'Kind': 'type', 'Name': 'ObjectAttributes', 'Variant': 'portable',
         'Duplicate': 'ON_Object_Duplicate',
         'ParentViews': [{'ParentType': 'File3dmObject', 'Const': 'GetAttributesConstPointer',
                          'NonConst': 'GetAttributesNonConstPointer',
                          'ConstNative': {'Name': 'ONX_Model_ModelObjectAttributes',
                                          'Args': [{'Kind': 'fieldptr', 'Text': 'm_parent', 'Value': 'ConstPointer'},
                                                   {'Kind': 'expr', 'Text': 'm_id'}]},
                          'NonConstNative': {'Name': 'ONX_Model_ModelObjectAttributes',
                                             'Args': [{'Kind': 'fieldptr', 'Text': 'm_parent', 'Value': 'NonConstPointer'},
                                                      {'Kind': 'expr', 'Text': 'm_id'}]}}]},
    ]
    return {'members': recs}


@pytest.fixture
def views():
    m = _views_manifest()
    R.index_types(m)
    yield m
    R.index_types({'members': []})


def test_views_are_derived_from_the_recorded_idioms(views):
    # the geometry of a File3dmObject: copy on write through the class's own duplicate;
    # its attributes: write through; a holder parent (CurveHolder) grants nothing by itself
    assert R.BY_CS['Curve']['proxy'] == {'Resolver': 'ONX_Model_ModelObjectGeometry', 'NonConst': 'cow',
                                         'Dup': 'ON_Curve_DuplicateCurve', 'Origin': 'object', 'Parent': 'File3dmObject'}
    assert R.BY_CS['ObjectAttributes']['proxy']['NonConst'] == 'resolve'
    assert R.BY_CS['File3dmObject']['proxy'] == {'Resolver': 'ONX_Model_ModelObjectGeometryConstPtrFromId',
                                                 'NonConst': 'cow', 'Dup': 'ON_Object_Duplicate', 'Origin': 'file'}
    assert not R.BY_CS['PolyCurve'].get('proxy')          # shares CurveX's storage: forwards, no view of its own
    assert P.self_access(R.BY_CS['PolyCurve'], 'static_cast<ON_PolyCurve*>(m_ptr)', 'NonConstPointer') \
        == 'static_cast<ON_PolyCurve*>(RH3DM_NonConst())'   # ... and reaches it through the base's pair
    assert not R.BY_CS['Brep'].get('proxy')               # Brep has no type record here: no view


def test_constview_shape_requires_matching_resolver(views):
    OBJ = R.BY_CS['File3dmObject']
    native = {'Name': 'ONX_Model_ModelObjectGeometry',
              'Args': [{'Kind': 'fieldptr', 'Text': 'm_parent', 'Value': 'ConstPointer'}, {'Kind': 'expr', 'Text': 'm_id'}]}
    sh = P.constview_shape({'Helper': 'GetGeometryConstPointer', 'Native': native, 'New': None}, OBJ, 'GeometryBase')
    assert sh[0] == 'constview' and sh[1] is R.BY_CS['GeometryBase'] and sh[3] is True
    sh = P.constview_shape({'Helper': 'GetAttributesConstPointer', 'New': 'ObjectAttributes',
                            'Native': dict(native, Name='ONX_Model_ModelObjectAttributes')}, OBJ, 'ObjectAttributes')
    assert sh[0] == 'constview' and sh[3] is False
    bad = P.constview_shape({'Helper': 'X', 'New': 'ObjectAttributes', 'Native': native}, OBJ, 'ObjectAttributes')
    assert isinstance(bad, str)                            # the view's resolver is not that helper's


def test_multi_type_table_sums_counts_and_chains_types(views):
    T = 'Rhino.FileIO.'
    m = _views_manifest()
    m['members'] += [
        {'Type': T + 'File3dmObjectTable', 'Kind': 'type', 'Name': 'File3dmObjectTable',
         'Bases': ['File3dmCommonComponentTable'], 'BaseArgs': [['File3dmObject']], 'Variant': 'portable',
         'ExtraComponentTypes': ['ModelComponentType.RenderLight'], 'ExtraComponentTypeValues': ['9']},
        {'Type': T + 'File3dmObjectTable', 'Kind': 'property', 'Name': 'ComponentType', 'Variant': 'portable',
         'Const': 'ModelComponentType.ModelGeometry', 'ConstValue': '12', 'Returns': 'ModelComponentType'},
        {'Type': T + 'CommonComponentTable', 'Kind': 'method', 'Name': 'GetEnumerator', 'Variant': 'portable',
         'Body': 'delegates', 'Delegates': '@m_manifest.GetEnumerator<T>', 'Returns': 'IEnumerator<T>'},
    ]
    R.index_types(m)
    for fn in ('ONX_ModelComponentIterator_New', 'ONX_ModelComponentIterator_GetNext', 'ONX_ModelComponentIterator_Delete'):
        R.C_BY_NAME[fn] = {'name': fn, 'returns': 'void', 'params': []}
    OT = R.BY_CS['File3dmObjectTable']
    count = {'m': {'Name': 'Count', 'Kind': 'property'}, 'bind_args': [], 'cpp': 'Gen_Count', 'locals': [], 'guards': [],
             'fn': {'name': 'F'}, 'call_exprs': ['m', '(ON_ModelComponent::Type)12 /*ModelGeometry*/'],
             'shape': ('conv', 'int', '(int)({c})'), 'typearg_pos': [1]}
    tp = P.table_protocol(OT, m, [count])
    assert tp and tp['types'] == ['12', '9'] and tp['find'] is None
    assert count['shape'] == ('sum', 'int', ['(int)(F(m, (ON_ModelComponent::Type)12))', '(int)(F(m, (ON_ModelComponent::Type)9))'])


def test_commit_marker_needs_a_recorded_commit_and_a_void_shape(views):
    # G41: `SetBool(...); Commit();` -- the write-back is the class's recorded Commit
    RS = dict(CURVE, type='Rhino.Render.RenderSettings', cs='RenderSettings', on='ON_3dmRenderSettings',
              base=None, proxy={'Resolver': 'R', 'NonConst': 'cow', 'Dup': 'D', 'Origin': 'file', 'NoId': True,
                                'Commit': 'ON_3dmRenderSettings_ONX_Model_Commit'})
    m = dict(member('void', calls=[SELF, {'Kind': 'param', 'Text': 'b'}], Commits=True, Body='pattern'),
             Type=RS['type'], Params=[{'Name': 'b', 'Type': 'bool', 'Mode': ''}])
    p, why = P.plan(m, cfn('void', 'ON_3dmRenderSettings*', 'bool'), RS)
    assert why is None and p['commits'] is True
    p, why = P.plan(m, cfn('void', 'ON_Curve*', 'bool'), CURVE)
    assert p is None and 'Commit' in why                       # Curve has no commit: refuse
    p, why = P.plan(dict(m, Returns='bool'), cfn('bool', 'ON_3dmRenderSettings*', 'bool'), RS)
    assert p is None and 'non-void' in why                     # a value after the commit: refuse


# ---- G42: the RDK settings -- free-floating views and the Variant idiom

@pytest.fixture
def rdk():
    T = 'Rhino.Render.'
    m = {'members': [
        {'Type': T + 'Variant', 'Kind': 'type', 'Name': 'Variant', 'Variant': 'portable', 'Internal': True,
         'Wrapper': {'New': 'ON_XMLVariant_New', 'Delete': 'ON_XMLVariant_Delete',
                     'Set': {'bool': 'ON_XMLVariant_SetBoolValue', 'Vector2d': 'ON_XMLVariant_Set2dVectorValue',
                             'Color': 'ON_XMLVariant_SetOnColorValue'},
                     'SetConv': {'Color': 'ToArgb'},
                     'Get': {'ToBool': {'Native': 'ON_XMLVariant_GetBoolValue', 'Returns': 'bool', 'Form': 'ne0'},
                             'ToVector2d': {'Native': 'ON_XMLVariant_Get2dVectorValue', 'Returns': 'Vector2d', 'Form': 'ref'}}}},
        {'Type': T + 'GroundPlane', 'Kind': 'type', 'Name': 'GroundPlane', 'Variant': 'portable',
         'FreeFloating': {'New': 'ON_GroundPlane_New', 'Delete': 'ON_GroundPlane_Delete',
                          'FromFile': 'ON_GroundPlane_FromONX_Model', 'GetValueNative': 'ON_GroundPlane_GetValue',
                          'SetValueNative': 'ON_GroundPlane_SetValue', 'EqualNative': 'ON_XMLVariant_IsEqual'}},
    ]}
    R.index_types(m)
    R.C_BY_NAME['ON_GroundPlane_GetValue'] = {'name': 'ON_GroundPlane_GetValue', 'returns': 'void',
        'params': [{'type': 'const ON_GroundPlane*'}, {'type': 'GroundPlaneSetting'}, {'type': 'ON_XMLVariant*'}]}
    yield m
    R.index_types({'members': []})


def test_free_floating_class_is_a_write_through_view_with_its_own_delete(rdk):
    GP = R.BY_CS['GroundPlane']
    assert GP['ff']['Delete'] == 'ON_GroundPlane_Delete'
    assert GP['proxy'] == {'Resolver': 'ON_GroundPlane_FromONX_Model', 'NonConst': 'resolve', 'Dup': None,
                           'Origin': 'file', 'NoId': True}


def test_variant_property_plans_from_the_wrapper_and_helper_natives(rdk):
    GP = R.BY_CS['GroundPlane']
    x = {'Type': GP['type'], 'Kind': 'property', 'Name': 'Enabled', 'Returns': 'bool', 'Variant': 'portable',
         'VariantGet': {'Which': 'UnsafeNativeMethods.GroundPlaneSetting.Enabled', 'WhichValue': '0', 'Conv': 'ToBool'},
         'VariantSet': {'Which': 'UnsafeNativeMethods.GroundPlaneSetting.Enabled', 'WhichValue': '0'}}
    g, why = P.plan_variant(x, GP)
    assert why is None and g['shape'][0] == 'vget' and g['shape'][1]['conv']['Form'] == 'ne0'
    s, why = P.plan_variant(x, GP, setter=True)
    assert why is None and s['shape'][1]['set'] == 'ON_XMLVariant_SetBoolValue' and s['bind_args'] == [('bool', 'value')]
    assert P.plan_variant(dict(x, Returns='Color'), GP, setter=True)[0] is None   # a converted argument: refused
    assert P.plan_variant(dict(x, Returns='float'), GP)[0] is None                 # no binding type for it
    assert P.plan_variant(dict(x, VariantGet=dict(x['VariantGet'], WhichValue=None)), GP)[0] is None


# ---- G43: components -- (owner, key), resolved through the owner on every access

G = 'Rhino.Geometry.'
COMP_FNS = {
    'ON_Brep_BrepTrimPointer': ('const ON_BrepTrim*', ['const ON_Brep*', 'int']),
    'ON_SubD_VertexFromId': ('const ON_SubDVertex*', ['const ON_SubD*', 'unsigned int']),
    'ON_SubD_FirstVertex': ('const ON_SubDVertex*', ['const ON_SubD*', 'unsigned int*']),
    'ON_SubDVertex_GetNext': ('const ON_SubDVertex*', ['const ON_SubDVertex*', 'unsigned int*']),
}


def _comps_manifest():
    recs = _views_manifest()['members'] + [
        {'Type': G + 'CurveProxy', 'Kind': 'type', 'Name': 'CurveProxy', 'Bases': ['Curve'], 'Variant': 'portable'},
        {'Type': G + 'BrepTrim', 'Kind': 'type', 'Name': 'BrepTrim', 'Bases': ['CurveProxy'], 'Variant': 'portable',
         'Component': {'Owner': 'Brep', 'OwnerNames': ['Brep'], 'KeyType': 'int', 'KeyNames': ['m_index', 'TrimIndex'],
                       'Ctor': ['key', 'owner'], 'Const': 'ON_Brep_BrepTrimPointer', 'NonConst': None}},
        {'Type': G + 'SubDVertex', 'Kind': 'type', 'Name': 'SubDVertex', 'Variant': 'portable',
         'Component': {'Owner': 'SubD', 'OwnerNames': ['ParentSubD'], 'KeyType': 'uint', 'KeyNames': ['Id'],
                       'Ctor': ['owner', 'ptr', 'key'], 'Const': 'ON_SubD_VertexFromId', 'NonConst': 'ON_SubD_VertexFromId'}},
        {'Type': G + 'Collections.SubDVertexList', 'Kind': 'type', 'Name': 'SubDVertexList', 'Variant': 'portable',
         'ParentFields': ['m_subd'],
         'Enumerator': {'First': 'ON_SubD_FirstVertex', 'Next': 'ON_SubDVertex_GetNext', 'Element': 'SubDVertex',
                        'OwnerName': 'm_subd'}},
        {'Type': G + 'PointCloudItem', 'Kind': 'type', 'Name': 'PointCloudItem', 'Variant': 'portable',
         'Component': {'Owner': 'PointCloud', 'OwnerNames': ['m_parent'], 'KeyType': 'int', 'KeyNames': ['m_index', 'Index'],
                       'Ctor': ['owner', 'key'], 'Const': None, 'NonConst': None}},
    ]
    return {'members': recs}


@pytest.fixture
def comps():
    for n, (ret, ps) in COMP_FNS.items():
        R.C_BY_NAME[n] = {'name': n, 'returns': ret, 'params': [{'type': t, 'name': 'p%d' % i} for i, t in enumerate(ps)]}
    m = _comps_manifest()
    R.index_types(m)
    yield m
    R.index_types({'members': []})
    for n in COMP_FNS:
        R.C_BY_NAME.pop(n, None)


def test_components_are_derived_from_the_recorded_idiom(comps):
    trim, vert, item = R.BY_CS['BrepTrim'], R.BY_CS['SubDVertex'], R.BY_CS['PointCloudItem']
    # BrepTrim : CurveProxy : Curve -- Curve's (view-capable) storage hosts the component state
    assert trim['comp']['Owner'] is R.BY_CS['Brep'] and trim['comp']['NonConst'] is None
    assert R.BY_CS['Curve'].get('comp_host') is True
    # SubDVertex: its own storage, never a File3dm view
    assert vert['comp']['Key'] == 'unsigned int' and not vert.get('proxy') and not vert.get('comp_host')
    # a pointer-less component of a child's parent: a child with a key
    assert item['key']['Type'] == 'int' and not item.get('comp')
    # no resolver in the C surface: nothing granted
    R.C_BY_NAME.pop('ON_SubD_VertexFromId')
    R.index_types(comps)
    assert not R.BY_CS['SubDVertex'].get('comp')


def test_component_members_reach_owner_key_and_self(comps):
    trim = R.BY_CS['BrepTrim']
    m = dict(member('int', calls=[{'Kind': 'fieldptr', 'Text': 'Brep', 'Value': 'ConstPointer'},
                                  {'Kind': 'expr', 'Text': 'm_index'}]), Type=trim['type'])
    p, why = P.plan(m, cfn('int', 'const ON_Brep*', 'int'), trim)
    assert why is None and p['call_exprs'] == ['RH3DM_Owner()', '(int)RH3DM_Key()']
    # self of a component deriving a host: the host's resolution, downcast
    m = dict(member('bool', calls=[{'Kind': 'expr', 'Text': 'p', 'Value': 'ConstPointer'}]), Type=trim['type'])
    p, why = P.plan(m, cfn('bool', 'const ON_BrepTrim*'), trim)
    assert why is None and p['call_exprs'] == ['static_cast<ON_BrepTrim*>(RH3DM_Const())']
    # a field that is not the owner stays refused
    m = dict(member('int', calls=[{'Kind': 'fieldptr', 'Text': 'm_other', 'Value': 'ConstPointer'}]), Type=trim['type'])
    assert P.plan(m, cfn('int', 'const ON_Brep*'), trim)[0] is None


def test_construct_maps_the_constructor_roles_and_needs_the_null_check(comps):
    vert = R.BY_CS['SubDVertex']
    # `IntPtr p = UNM.Next(ConstPointer(), true, ref id); if (p != IntPtr.Zero) return new SubDVertex(ParentSubD, p, id);`
    m = dict(member('SubDVertex', calls=[{'Kind': 'expr', 'Text': 'c', 'Value': 'ConstPointer'},
                                         {'Kind': 'literal', 'Text': 'true'},
                                         {'Kind': 'out', 'Text': 'id', 'Value': '0'}]),
             Type=vert['type'], Body='construct', Child='SubDVertex', ChildArgs=['ParentSubD', 'p', 'id'],
             ChildArgKinds=['field', 'result', 'out'], ChildCheck='null')
    f = cfn('const ON_SubDVertex*', 'const ON_SubDVertex*', 'bool', 'unsigned int*')
    p, why = P.plan(m, f, vert)
    assert why is None and p['shape'] == ('construct', vert, 'RH3DM_Owner()', '(unsigned int)id', 'ptr', 'null')
    # without the C# null check the pointer is not proven: refuse
    assert P.plan(dict(m, ChildCheck=None), f, vert)[0] is None
    # a component is never wrapped as an owned object
    m2 = dict(member('SubDVertex', calls=[{'Kind': 'expr', 'Text': 'c', 'Value': 'ConstPointer'}]), Type=vert['type'])
    assert P.plan(m2, cfn('ON_SubDVertex*', 'const ON_SubDVertex*'), vert)[0] is None


def test_keyed_child_and_indexer_construct_without_a_call(comps):
    pc, item = R.BY_CS['PointCloud'], R.BY_CS['PointCloudItem']
    x = {'Type': pc['type'], 'Kind': 'indexer', 'Name': 'this', 'Variant': 'portable', 'Body': 'construct',
         'Returns': 'PointCloudItem', 'Params': [{'Name': 'index', 'Type': 'int', 'Mode': ''}], 'Signature': 's',
         'Child': 'PointCloudItem', 'ChildArgs': ['this', 'index'], 'ChildArgKinds': ['this', 'param'],
         'Guards': [{'Cond': 'index < 0', 'Ids': ['index'], 'Exception': 'IndexOutOfRangeException'}]}
    p, why = P.plan_construct_nocall(x, pc)
    assert why is None and p['shape'][0] == 'construct' and p['shape'][1] is item and p['shape'][3] == '(int)index'
    assert p['bind_args'] == [('int', 'index')] and len(p['guards']) == 1
    # an owner that is not this class: refuse
    assert P.plan_construct_nocall(x, R.BY_CS['Mesh'])[0] is None


def test_linked_iter_needs_a_component_element_of_the_parent(comps):
    lst = dict(mode='child', parent='SubDX', type=G + 'Collections.SubDVertexList', on='ON_SubD', reg='SubDVertexListX')
    li = P.linked_iter(lst, [])
    assert li and li['item'] is R.BY_CS['SubDVertex'] and li['first']['name'] == 'ON_SubD_FirstVertex'
    assert P.linked_iter(dict(lst, parent='BrepX'), []) is None      # the element's owner is not the parent


def test_conditional_child_is_an_early_return_on_the_sibling(tables):
    x = {'Type': R.BY_CS['Material']['type'], 'Kind': 'property', 'Name': 'PhysicallyBased', 'Variant': 'portable',
         'Returns': 'PhysicallyBasedMaterial', 'Child': 'PhysicallyBasedMaterial', 'ChildArgs': ['this'],
         'ChildArgKinds': ['this'], 'ChildIf': 'IsPhysicallyBased', 'Body': 'pure'}
    plans = P.plan_class(R.BY_CS['Material'], {'members': [x]}, {}, {})
    # the sibling getter is not planned here, so the guard cannot resolve: refused, never unguarded
    assert not [p for p in plans if p['m']['Name'] == 'PhysicallyBased']


def test_forward_resolves_the_target_overload_by_argument_types():
    # G44: `SetVertex(int i, Point3f v) => SetVertex(i, v.X, v.Y, v.Z)` calls the FLOAT
    # overload in C#; with only the double one generated, the forward must refuse
    T = R.BY_CS['Curve']['type']
    def m(name, ptypes, **kw):
        d = {'Type': T, 'Kind': 'method', 'Name': name, 'Variant': 'portable', 'Returns': 'bool',
             'Params': [{'Name': 'p%d' % i, 'Type': t, 'Mode': ''} for i, t in enumerate(ptypes)],
             'Signature': '%s(%s)' % (name, ','.join(ptypes))}
        d.update(kw)
        return d
    dbl, flt = m('Set', ['int', 'double', 'double', 'double']), m('Set', ['int', 'float', 'float', 'float'])
    target = {'m': dbl, 'shape': ('direct', 'bool'), 'static': False,
              'bind_args': [('int', 'p0'), ('double', 'p1'), ('double', 'p2'), ('double', 'p3')]}
    fwd = m('Set', ['int', 'Point3f'], Body='delegates', Delegates='Set',
            DelegateArgs=[{'Kind': 'param', 'Text': 'p0'}] + [{'Kind': 'parammember', 'Text': 'p1.%s' % c} for c in 'XYZ'])
    R.VT_PROP_RET.update({('Point3f', c): 'float' for c in 'XYZ'})
    R.VT_PROP_RET.update({('Point3d', c): 'double' for c in 'XYZ'})
    added = {(t, c) for t in ('Point3f', 'Point3d') for c in 'XYZ'} - R.VT_PROPS
    R.VT_PROPS.update(added)
    vt_added = [t for t in ('Point3f', 'Point3d') if t not in R.VT_CPP]
    for t in vt_added:
        R.VT_CPP[t] = 'BND_%sX' % t
    try:
        assert P.plan_forward(fwd, R.BY_CS['Curve'], {('Set', 4): [target]}, [dbl, flt, fwd])[0] is None
        fwd_d = dict(fwd, Params=[{'Name': 'p0', 'Type': 'int', 'Mode': ''}, {'Name': 'p1', 'Type': 'Point3d', 'Mode': ''}],
                     Signature='Set(int,Point3d)')
        p, why = P.plan_forward(fwd_d, R.BY_CS['Curve'], {('Set', 4): [target]}, [dbl, flt, fwd_d])
        assert why is None and p['shape'][2] is target
    finally:
        for t in vt_added:
            R.VT_CPP.pop(t, None)
        R.VT_PROPS.difference_update(added)
        for t in ('Point3f', 'Point3d'):
            for c in 'XYZ':
                R.VT_PROP_RET.pop((t, c), None)


# ---- G46: native-backed classes

def test_rhino_only_native_is_refused():
    # DracoCompression's natives live in the rhino repo's draco.cpp, not in rhino3dm
    f = dict(cfn('bool', 'const ON_Curve*'), variant='rhino-only')
    p, why = P.plan(member('bool', calls=[SELF]), f, CURVE)
    assert p is None and 'not in rhino3dm' in why


def test_newobj_needs_the_returned_local_and_a_registered_class():
    TEX = R.BY_CS['Texture']
    base = dict(member('Texture', calls=[SELF, {'Kind': 'newobj', 'Text': 'tex', 'Value': 'Texture'}]))
    f = cfn('void', 'const ON_Curve*', 'ON_Texture*')
    p, why = P.plan(dict(base, ReturnLocal='tex'), f, CURVE)
    assert why is None and p['shape'] == ('newobj', TEX, 'tex', None) and p['locals'] == ['BND_TextureX* tex = new BND_TextureX();']
    assert P.plan(dict(base, ReturnLocal='other'), f, CURVE)[0] is None          # the filled object is not the result
    bad = member('Texture', calls=[SELF, {'Kind': 'newobj', 'Text': 'h', 'Value': 'SimpleArrayGuid'}], ReturnLocal='h')
    assert P.plan(bad, f, CURVE)[0] is None                                       # not a generated class


# ---- G49: category N idioms

def test_newobj_failure_drops_the_object_only_on_the_recorded_status():
    # TextureMapping.Create*Mapping: `if (!CALL) { rc.Dispose(); rc = null; } return rc;`
    TEX = R.BY_CS['Texture']
    base = member('Texture', calls=[SELF, {'Kind': 'newobj', 'Text': 'rc', 'Value': 'Texture'}], ReturnLocal='rc')
    p, why = P.plan(dict(base, NewObjFail='rc'), cfn('bool', 'const ON_Curve*', 'ON_Texture*'), CURVE)
    assert why is None and p['shape'] == ('newobj', TEX, 'rc', 'null')
    # a void call has no status to decide by; a different local is not the dropped object
    assert P.plan(dict(base, NewObjFail='rc'), cfn('void', 'const ON_Curve*', 'ON_Texture*'), CURVE)[0] is None
    assert P.plan(dict(base, NewObjFail='other'), cfn('bool', 'const ON_Curve*', 'ON_Texture*'), CURVE)[0] is None
    # without the fact the status is discarded: a trivial body never branched on it
    p, why = P.plan(base, cfn('bool', 'const ON_Curve*', 'ON_Texture*'), CURVE)
    assert why is None and p['shape'][3] is None


def test_result_in_a_set_of_literals():
    # Extrusion.IsCappedAtBottom: `return (1 == rc || 3 == rc);` recorded "in 1,3"
    p, why = P.plan(member('bool', calls=[SELF], Compare='in 1,3'), cfn('int', 'const ON_Curve*'), CURVE)
    assert why is None and p['shape'] == ('conv', 'bool', 'RH3DM_In(({c}), 1, 3)')
    assert P.plan(member('bool', calls=[SELF], Compare='in 1,x'), cfn('int', 'const ON_Curve*'), CURVE)[0] is None


def test_conditional_bool_argument():
    # NurbsCurveKnotList.SuperfluousKnot: `F(p, start ? 0 : 1)`
    arg = {'Kind': 'paramcond', 'Text': 'start', 'Value': '0:1'}
    p, why = P.plan(member('double', params=[('', 'bool', 'start')], calls=[SELF, arg]),
                    cfn('double', 'const ON_Curve*', 'int'), CURVE)
    assert why is None and '(start ? 0 : 1)' in p['call_exprs']
    # only on a bool parameter
    p, why = P.plan(member('double', params=[('', 'int', 'start')], calls=[SELF, arg]),
                    cfn('double', 'const ON_Curve*', 'int'), CURVE)
    assert p is None


def test_parameter_normalization_guard():
    # Surface.Domain: `if (direction != 0) direction = 1;` before the call
    g = {'Cond': 'direction != 0', 'Ids': ['direction'], 'Assign': 'direction', 'AssignValue': '1'}
    m = member('double', params=[('', 'int', 'direction')], calls=[SELF, {'Kind': 'param', 'Text': 'direction'}], Guards=[g])
    p, why = P.plan(m, cfn('double', 'const ON_Curve*', 'int'), CURVE)
    assert why is None and p['guards'][0]['assign_cpp'] == 'direction = 1'
    # a computed value, or a parameter that is not a by-value scalar, is refused
    assert P.plan(dict(m, Guards=[dict(g, AssignValue='other')]), cfn('double', 'const ON_Curve*', 'int'), CURVE)[0] is None
    m2 = member('double', params=[('ref', 'int', 'direction')], calls=[SELF, {'Kind': 'param', 'Text': 'direction'}], Guards=[g])
    assert P.plan(m2, cfn('double', 'const ON_Curve*', 'int'), CURVE)[0] is None


def test_fail_throw_with_further_conditional_throws_is_emitted_in_order():
    # PointCloud.PointAt: `if (!F(...)) { if (C1) throw ...; if (C2) throw ...; }`
    import emit_classes as E
    ft = {'Exception': 'IndexOutOfRangeException', 'Message': 'a', 'Cond': 'i < 0', 'cond_cpp': 'i < 0',
          'More': [{'Exception': 'IndexOutOfRangeException', 'Message': 'b', 'Cond': 'i >= Count', 'cond_cpp': 'i >= Gen_Count()'}]}
    line = E.failthrow_line(ft, 'F()', {'Name': 'PointAt'})
    assert line == ('    if (!(F())) { if (i < 0) throw std::out_of_range("a"); '
                    'if (i >= Gen_Count()) throw std::out_of_range("b"); }')


def test_handle_class_comes_from_the_recorded_delete():
    T = 'Rhino.FileIO.FileReference'
    m = {'members': [{'Type': T, 'Kind': 'type', 'Name': 'FileReference', 'Variant': 'portable',
                      'Handle': {'Field': 'm_const_ptr', 'Delete': 'ON_FileReference_Delete'}}]}
    R.C_BY_NAME['ON_FileReference_Delete'] = {'name': 'ON_FileReference_Delete', 'returns': 'void',
                                             'params': [{'type': 'ON_FileReference*'}], 'variant': 'portable'}
    try:
        R.index_types(m)
        assert R.BY_CS['FileReference']['handle'] == {'Delete': 'ON_FileReference_Delete', 'Field': 'm_const_ptr'}
        R.C_BY_NAME['ON_FileReference_Delete']['variant'] = 'rhino-only'      # not shipped: no handle
        R.index_types(m)
        assert not R.BY_CS['FileReference'].get('handle')
    finally:
        R.C_BY_NAME.pop('ON_FileReference_Delete', None)
        R.index_types({'members': []})


def test_child_construction_after_a_bool_check(tables):
    MM = R.BY_CS['File3dmMeshModifiers']
    x = dict(member('File3dmDisplacement', calls=[{'Kind': 'fieldptr', 'Text': 'm_attr', 'Value': 'ConstPointer'}]),
             Type=MM['type'], Body='construct', Child='File3dmDisplacement', ChildArgs=['m_attr'],
             ChildArgKinds=['field'], ChildCheck='true')
    R.TYPE_RECS[MM['type']] = {'ParentFields': ['m_attr']}
    try:
        p, why = P.plan(x, cfn('bool', 'const ON_3dmObjectAttributes*'), MM)
        assert why is None and p['shape'][0] == 'construct' and p['shape'][4] == 'check' and p['shape'][2] == 'm_ptr'
        # G121: an unchecked result (`F(..); return m ?? (m = new X(m_attr));`: File3dmMeshModifiers.Create*)
        # calls the native for its effect, then makes the child -- the extractor records `construct`
        # only for bodies whose every statement it recognized, so no condition went unseen
        q, why = P.plan(dict(x, ChildCheck=None), cfn('bool', 'const ON_3dmObjectAttributes*'), MM)
        assert why is None and q['shape'][0] == 'construct' and q['shape'][4] == 'call'
        assert P.plan(dict(x, ChildArgs=['m_other']), cfn('bool', 'const ON_3dmObjectAttributes*'), MM)[0] is None
    finally:
        R.TYPE_RECS.pop(MM['type'], None)


# ---- G47: constructor specialization

def test_members_use_the_analysis_for_the_constructor_the_parent_uses():
    LL = R.BY_CS['BrepLoopList']
    T = LL['type']
    count = {'Type': T, 'Kind': 'property', 'Name': 'Count', 'Variant': 'portable', 'Body': 'branches', 'Calls': None,
             'Specialized': {'(Brep)': {'Body': 'trivial', 'Calls': [{'Name': 'ON_Brep_GetInt', 'Args': []}]}}}
    m = {'members': [{'Type': T, 'Kind': 'type', 'Name': 'BrepLoopList', 'Variant': 'portable',
                      'NullingCtors': [{'Key': '(Brep)', 'Params': ['Brep'], 'Nulls': ['m_brepface']}]},
                     {'Type': R.BY_CS['Brep']['type'], 'Kind': 'property', 'Name': 'Loops', 'Variant': 'portable',
                      'Child': 'Collections.BrepLoopList', 'ChildArgs': ['this'], 'ChildArgKinds': ['this']},
                     count]}
    R.index_types(m)
    try:
        assert LL['ctor_key'] == '(Brep)'
        got = [x for x in P.class_members(LL, m) if x['Name'] == 'Count'][0]
        assert got['Body'] == 'trivial' and got['Calls'][0]['Name'] == 'ON_Brep_GetInt'
        # reached some other way (no accessor through a nulling ctor): the unspecialized body
        m['members'][1] = dict(m['members'][1], ChildArgs=['this', 'x'])
        R.index_types(m)
        assert not LL.get('ctor_key')
        assert [x for x in P.class_members(LL, m) if x['Name'] == 'Count'][0]['Body'] == 'branches'
    finally:
        R.index_types({'members': []})


def test_base_member_overridden_with_ungenerated_logic_is_refused():
    # GeometryBase.GetBoundingBox(Transform) vs AnnotationBase's managed override:
    # C++ inheritance would hand the derived objects the base logic (G49)
    import bnd_gen as G
    base, derived = R.BY_REG['CurveX'], R.BY_REG['NurbsCurveX']
    mb = {'Type': base['type'], 'Kind': 'method', 'Name': 'M', 'Variant': 'portable', 'Params': [{'Type': 'double'}]}
    for body, kept in (('pure', False), ('base', True)):
        md = dict(mb, Type=derived['type'], Body=body)
        p = {'m': mb, 'shape': ('direct', 'double')}
        fwd = {'m': dict(mb, Params=[]), 'shape': ('forward', None, p)}
        per_class = [(base, [p, fwd]), (derived, [])]
        skipped = {}
        G.refuse_overridden({'members': [mb, md]}, per_class, skipped)
        assert (per_class[0][1] == [p, fwd]) is kept
        assert bool(skipped) is not kept          # the forward went with its target


def test_one_argument_wrap_needs_an_adopting_constructor():
    # `return new T(p);` adopts p only if T(IntPtr) does: TextureMapping's copies
    # (NewFromPointer), Font's keeps a pointer it never deletes (G49)
    TM = R.BY_CS['TextureMapping']
    m = member('TextureMapping', calls=[SELF], Body='pattern', WrapArity=1)
    f = cfn('ON_TextureMapping*', 'const ON_Curve*')
    saved = R.TYPE_RECS.get(TM['type'])
    try:
        for pc, ok in (('own', True), ('other', False), ('field:m_managed_font', False), (None, False)):
            R.TYPE_RECS[TM['type']] = {'PtrCtor': pc} if pc else {}
            assert (P.plan(m, f, CURVE)[0] is not None) is ok, pc
        # a constructor that throws on null matches only after the member's own null check
        R.TYPE_RECS[TM['type']] = {'PtrCtor': 'own+nullthrow'}
        assert P.plan(m, f, CURVE)[0] is None
        assert P.plan(dict(m, WrapChecked=True), f, CURVE)[0] is not None
        # `new T(p, null)` is CommonObject's own constructor: no check
        R.TYPE_RECS[TM['type']] = {'PtrCtor': 'other'}
        assert P.plan(dict(m, WrapArity=2), f, CURVE)[0] is not None
    finally:
        if saved is None:
            R.TYPE_RECS.pop(TM['type'], None)
        else:
            R.TYPE_RECS[TM['type']] = saved


def test_value_class_adopts_a_native_result_through_its_handle_delete():
    # MeshingParameters.DefaultAnalysisMesh: copy in, release with ON_MeshParameters_Delete
    MP = R.BY_CS['MeshingParameters']
    m = member('MeshingParameters', calls=[], Body='pattern', WrapArity=1, Static=True)
    f = {'name': 'F', 'returns': 'ON_MeshParameters*', 'params': []}
    rec = {'PtrCtor': 'field:m_ptr', 'Handle': {'Field': 'm_ptr', 'Delete': 'ON_MeshParameters_Delete'}}
    saved = R.TYPE_RECS.get(MP['type'])
    R.C_BY_NAME['ON_MeshParameters_Delete'] = {'name': 'ON_MeshParameters_Delete', 'returns': 'void',
                                              'params': [{'type': 'ON_MeshParameters*'}]}
    try:
        R.TYPE_RECS[MP['type']] = rec
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['shape'] == ('valadopt', MP, 'ON_MeshParameters_Delete', False)
        p, why = P.plan(dict(m, WrapChecked=True), f, CURVE)
        assert p['shape'][3] is True                      # null -> None, as C#'s `return null;`
        R.TYPE_RECS[MP['type']] = dict(rec, PtrCtor='other')
        assert P.plan(m, f, CURVE)[0] is None
    finally:
        R.C_BY_NAME.pop('ON_MeshParameters_Delete', None)
        if saved is None:
            R.TYPE_RECS.pop(MP['type'], None)
        else:
            R.TYPE_RECS[MP['type']] = saved


def test_color_converted_from_an_out_local():
    # PointCloudItem.Color: `int argb = 0; F(p, ref argb); return Color.FromArgb(argb);`
    out = {'Kind': 'out', 'Text': 'argb', 'Value': '0'}
    m = member('Color', calls=[SELF, out], ResultConv='FromArgb', ConvOut='argb')
    p, why = P.plan(m, cfn('void', 'const ON_Curve*', 'int*'), CURVE)
    assert why is None and p['shape'] == ('fromoutconv', 'BND_Color', 'RH3DM_ColorFromArgb({n})', 'argb')
    assert p['locals'] == ['int argb = 0;']
    # SubDFace.PerFaceColor: `if (!F(p, ref argb)) return Color.Empty;` -- Color.Empty only
    ok = dict(m, Success={'Out': 'argb', 'Fallback': 'System.Drawing.Color.Empty', 'Form': 'failreturn'})
    p, why = P.plan(ok, cfn('bool', 'const ON_Curve*', 'int*'), CURVE)
    assert why is None and p['success']['fb_cpp'] == 'RH3DM_ColorFromArgb(0)'
    bad = dict(m, Success={'Out': 'argb', 'Fallback': 'Color.Red', 'Form': 'failreturn'})
    assert P.plan(bad, cfn('bool', 'const ON_Curve*', 'int*'), CURVE)[0] is None


def test_opennurbs_enum_result_is_cast_like_the_pinvoke():
    # AnnotationBase.AnnotationType: the C function returns ON::AnnotationType
    R.CS_ENUMS['AnnotationTypeT'] = {'cpp': 'RH3DM_AnnotationTypeT', 'gen': True}
    try:
        p, why = P.plan(member('AnnotationTypeT', calls=[SELF]), cfn('ON::AnnotationType', 'const ON_Curve*'), CURVE)
        assert why is None and p['shape'] == ('enumret', 'AnnotationTypeT')
        assert P.plan(member('AnnotationTypeT', calls=[SELF]), cfn('ON_Curve*', 'const ON_Curve*'), CURVE)[0] is None
    finally:
        R.CS_ENUMS.pop('AnnotationTypeT', None)


def test_array_fill_plans_both_native_calls_and_checks_the_element_type():
    # PointCloud.GetPoints: `int count = GetInt(p, idx); if (count < 1) return new Point3d[0];
    # Point3d[] rc = new Point3d[count]; GetPoints(p, count, rc); return rc;`
    af = {'Local': 'rc', 'Elem': 'Point3d', 'Count': 'count', 'CountFrom': 'call',
          'EmptyCond': 'count < 1', 'Empty': 'empty', 'FillForm': 'stmt'}
    size = {'Name': 'S', 'Args': [SELF, {'Kind': 'literal', 'Text': '0'}]}
    fill = {'Name': 'G', 'Args': [SELF, {'Kind': 'expr', 'Text': 'count'}, {'Kind': 'expr', 'Text': 'rc'}]}
    x = dict(member('Point3d[]'), Body='arrayfill', ArrayFill=af, Calls=[size, fill])
    fns = {'S': {'name': 'S', 'returns': 'int', 'params': [{'type': 'const ON_Curve*'}, {'type': 'int'}]},
           'G': {'name': 'G', 'returns': 'void', 'params': [{'type': 'const ON_Curve*'}, {'type': 'int'}, {'type': 'ON_3dPoint*'}]}}
    p, why = P.plan_arrayfill(x, CURVE, fns)
    assert why is None and p['shape'][0] == 'arrayfill'
    assert p['call_exprs'][1:] == ['count', 'RH3DM_Ptr(rc.data())'] and p['shape'][1]['count_expr'].startswith('S(')
    # an element the C side does not lay out the same way is refused
    fns['G'] = dict(fns['G'], params=[{'type': 'const ON_Curve*'}, {'type': 'int'}, {'type': 'ON_3dVector*'}])
    assert P.plan_arrayfill(x, CURVE, fns)[0] is None
    # and an emptiness test that is not a plain comparison of the count
    fns['G'] = dict(fns['G'], params=[{'type': 'const ON_Curve*'}, {'type': 'int'}, {'type': 'ON_3dPoint*'}])
    assert P.plan_arrayfill(dict(x, ArrayFill=dict(af, EmptyCond='count < Other')), CURVE, fns)[0] is None


def test_native_calls_in_sequence():
    # MeshVertexList.Clear: `ClearList(p, ClearVertices); RepairHiddenArray(p);`
    a = {'Name': 'A', 'Args': [SELF, {'Kind': 'literal', 'Text': '0'}]}
    b = {'Name': 'B', 'Args': [SELF]}
    x = dict(member('void'), Calls=[a, b], Sequence=True)
    fns = {'A': {'name': 'A', 'returns': 'void', 'params': [{'type': 'const ON_Curve*'}, {'type': 'int'}]},
           'B': {'name': 'B', 'returns': 'bool', 'params': [{'type': 'const ON_Curve*'}]}}
    p, why = P.plan_sequence(x, CURVE, fns)
    assert why is None and p['shape'][0] == 'seq' and [c.split('(')[0] for c in p['shape'][1]] == ['A', 'B']
    assert {f['name'] for f in p['extra_fns']} == {'A', 'B'}
    # only void members: a C# result would come from somewhere
    assert P.plan_sequence(dict(x, Returns='bool'), CURVE, fns)[0] is None


def test_array_fill_count_sibling_overridden_in_a_derived_class_is_refused():
    import bnd_gen as G
    base, derived = R.BY_REG['CurveX'], R.BY_REG['NurbsCurveX']
    fill = {'m': {'Name': 'GetSpans'}, 'shape': ('arrayfill', {'count_method': ('SpanCount', ['d'])})}
    for dplans, kept in (([], True), ([{'m': {'Name': 'SpanCount'}, 'shape': ('direct', 'int')}], False)):
        per_class = [(base, [fill]), (derived, list(dplans))]
        G.refuse_overridden_count(per_class, {})
        assert (per_class[0][1] == [fill]) is kept


def test_result_select_maps_the_native_int_onto_enum_members():
    # Texture.WrapU: `value == (int)TextureUvwWrapping.Clamp ? TextureUvwWrapping.Clamp : TextureUvwWrapping.Repeat`
    R.CS_ENUMS['WrapT'] = {'cpp': 'RH3DM_WrapT', 'gen': True}
    R.ENUM_NAMED['WrapT'] = {'Repeat': 0, 'Clamp': 1}
    try:
        rs = {'Key': '(int)WrapT.Clamp', 'True': 'WrapT.Clamp', 'False': 'WrapT.Repeat'}
        p, why = P.plan(member('WrapT', calls=[SELF], ResultSelect=rs), cfn('int', 'const ON_Curve*'), CURVE)
        assert why is None and p['shape'] == ('conv', 'RH3DM_WrapT', '((int)({c}) == 1 ? (RH3DM_WrapT)1 : (RH3DM_WrapT)0)')
        # a constant of another enum is refused
        bad = dict(rs, **{'True': 'OtherEnum.Clamp'})
        assert P.plan(member('WrapT', calls=[SELF], ResultSelect=bad), cfn('int', 'const ON_Curve*'), CURVE)[0] is None
    finally:
        R.CS_ENUMS.pop('WrapT', None)
        R.ENUM_NAMED.pop('WrapT', None)


def test_native_unless_needs_a_statically_false_property():
    # TextureMapping.Id: `IsDocumentControlled ? m_id : F(...)` -- only where the
    # storage answers IsDocumentControlled with a constant false (G57)
    import planner as PL
    saved = PL.no_owned_pointer_cpp
    try:
        for answer, ok in (('false', True), ('(m_ptr == nullptr)', False), (None, False)):
            PL.no_owned_pointer_cpp = lambda cls, ident, a=answer: a
            p, why = P.plan(member('double', calls=[SELF], NativeUnless='IsDocumentControlled'),
                            cfn('double', 'const ON_Curve*'), CURVE)
            assert (p is not None) is ok
    finally:
        PL.no_owned_pointer_cpp = saved


def test_result_clamp_and_tuple_guard_returns():
    # SphereSphere: `if (rc <= 0 || rc > 3) return E.None; return (E)rc;` (outs: a circle)
    R.CS_ENUMS['SphereT'] = {'cpp': 'RH3DM_SphereT', 'gen': True}
    R.ENUM_NAMED['SphereT'] = {'None': 0, 'Point': 1, 'Circle': 2, 'Overlap': 3}
    try:
        rg = {'Cond': 'rc <= 0 || rc > 3', 'Ids': ['rc'], 'Return': 'SphereT.None'}
        m = member('SphereT', params=[('out', 'double', 'd')], calls=[SELF, {'Kind': 'out', 'Text': 'd'}], ResultGuard=rg)
        p, why = P.plan(m, cfn('int', 'const ON_Curve*', 'double*'), CURVE)
        assert why is None and '(rh3dm_r <= 0 || rh3dm_r > 3) ? (RH3DM_SphereT)0 : (RH3DM_SphereT)rh3dm_r' in p['shape'][1][2]
        # a condition naming anything but the result is refused, and so is the clamp on another shape
        assert P.plan(dict(m, ResultGuard=dict(rg, Cond='rc > Count')), cfn('int', 'const ON_Curve*', 'double*'), CURVE)[0] is None
        assert P.plan(member('SphereT', calls=[SELF], ResultGuard=rg), cfn('int', 'const ON_Curve*'), CURVE)[0] is None
        # LineCircle: an early `return E.None;` in a member whose result leads a tuple
        g = {'Cond': 'd < 0', 'Ids': [], 'Return': 'SphereT.None', 'ReturnValue': '0'}
        p, why = P.plan(dict(m, ResultGuard=None, Guards=[g]), cfn('int', 'const ON_Curve*', 'double*'), CURVE)
        assert why is None and p['guards'][0]['ret_cpp'] == 'TUPLE:(RH3DM_SphereT)0'
    finally:
        R.CS_ENUMS.pop('SphereT', None)
        R.ENUM_NAMED.pop('SphereT', None)


def test_sub_view_is_never_an_owned_wrap():
    # PolyCurve.SegmentCurve / BrepFace.GetMesh (G58): a view inside this object,
    # only when specgen verified RhinoCommon re-resolves it by this very call
    CV = R.BY_CS['Curve']
    saved = (CV.get('comp_host'), CV.get('proxy'))
    CV['comp_host'] = True
    CV['proxy'] = {'Origin': 'object', 'NonConst': 'cow', 'Dup': 'ON_Curve_DuplicateCurve', 'Resolver': 'R'}
    try:
        sv = {'Form': 'holder', 'Holder': 'H', 'Args': ['this', 'k'], 'Verified': True, 'Write': 'copy'}
        m = member('Curve', params=[('', 'int', 'k')], calls=[SELF, {'Kind': 'param', 'Text': 'k'}], Body='pattern', SubView=sv)
        f = cfn('const ON_Curve*', 'const ON_Curve*', 'int')
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['shape'][0] == 'subview' and p['shape'][2] == 'k' and p['shape'][4] == 'copy'
        # unverified, or two keys: refused -- never the owning wrap
        assert P.plan(dict(m, SubView=dict(sv, Verified=False)), f, CURVE)[0] is None
        m2 = member('Curve', params=[('', 'int', 'k'), ('', 'int', 'j')],
                    calls=[SELF, {'Kind': 'param', 'Text': 'k'}], Body='pattern', SubView=dict(sv, Args=['this', 'k', 'j']))
        assert P.plan(m2, f, CURVE)[0] is None
    finally:
        for k, v in zip(('comp_host', 'proxy'), saved):
            if v is None:
                CV.pop(k, None)
            else:
                CV[k] = v


def test_adopted_argument_needs_a_copy_on_write_view_host():
    # BrepFace.SetMesh (G59): `mesh.EnsurePrivateCopy(); F(this, mesh.NonConstPointer(), t);
    # mesh.SetParent(new MeshHolder(this, t));` -- only for a class that can become such a view
    MX = R.BY_CS['Mesh']
    saved = (MX.get('comp_host'), MX.get('proxy'))
    try:
        MX['comp_host'] = True
        MX['proxy'] = {'Origin': 'object', 'NonConst': 'cow', 'Dup': 'ON_Object_Duplicate', 'Resolver': 'R'}
        ad = {'Param': 'mesh', 'Private': True, 'Holder': 'MeshHolder', 'HolderArgs': ['this', 't']}
        m = member('bool', params=[('', 'int', 't'), ('', 'Mesh', 'mesh')],
                   calls=[SELF, {'Kind': 'paramptr', 'Text': 'mesh', 'Value': 'NonConstPointer'}, {'Kind': 'param', 'Text': 't'}],
                   Adopt=ad)
        f = cfn('bool', 'const ON_Curve*', 'ON_Mesh*', 'int')
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['adopt']['param'] == 'mesh'
        assert 'mesh.RH3DM_NonConst()' in p['call_exprs'][1]          # EnsurePrivateCopy: copy a view first
        MX['proxy'] = dict(MX['proxy'], NonConst='resolve')            # a write-through view cannot be adopted
        assert P.plan(m, f, CURVE)[0] is None
    finally:
        for k, v in zip(('comp_host', 'proxy'), saved):
            if v is None:
                MX.pop(k, None)
            else:
                MX[k] = v


def test_internal_exception_throws_as_its_base_with_its_fixed_message():
    # G60: `throw new DecalReadOnlyException()` -- an internal type whose
    # parameterless constructor is `: base("Can't modify read-only decal collection")`
    guard = {'Cond': 'm_read_only', 'Exception': 'DecalReadOnlyException', 'Ids': ['m_read_only'], 'Message': None}
    m = {'members': [
        {'Type': 'Rhino.Render.DecalReadOnlyException', 'Kind': 'type', 'Name': 'DecalReadOnlyException',
         'Variant': 'portable', 'Internal': True,
         'Exception': {'Base': 'InvalidOperationException', 'Message': "Can't modify"}},
        {'Type': 'Rhino.Render.OddException', 'Kind': 'type', 'Name': 'OddException', 'Variant': 'portable',
         'Exception': {'Base': 'SomethingUnmappedException', 'Message': 'm'}},
        {'Type': 'Rhino.Render.Decals', 'Kind': 'method', 'Name': 'RemoveAllDecals', 'Variant': 'portable',
         'Guards': [guard]}]}
    try:
        R.index_types(m)
        assert R.EXMAP['DecalReadOnlyException'] == R.EXMAP['InvalidOperationException']
        assert guard['Message'] == "Can't modify"
        assert 'OddException' not in R.EXMAP                 # an unmapped base maps nothing
    finally:
        R.index_types({'members': []})
    assert 'DecalReadOnlyException' not in R.EXMAP           # indexed per manifest


def test_child_view_with_constructor_state_takes_literals_only():
    # G60: `uint doc_sn = 0; m_decals = new Decals(this, false, doc_sn);` -> (false, 0)
    child = {'state': [('bool', 'm_read_only'), ('unsigned int', 'm_sn')]}
    x = {'ChildArgs': ['this', 'false', '0'], 'ChildArgKinds': ['this', 'const', 'const']}
    assert P.child_state_values(x, child) == ['false', '0']
    assert P.child_state_values(dict(x, ChildArgs=['this', '0', '0']), child) is None          # 0 is no bool
    assert P.child_state_values(dict(x, ChildArgKinds=['this', 'const', 'other']), child) is None
    assert P.child_state_values(dict(x, ChildArgs=['this', 'false'], ChildArgKinds=['this', 'const']), child) is None
    assert P.child_state_values(dict(x, ChildArgs=['m_x', 'false', '0']), child) is None


def test_child_state_needs_a_store_only_constructor_with_the_parent_first():
    # G60: Decals(ObjectAttributes parent, bool read_only, uint rhino_doc_sn) stores
    # all three; the parent is the view's pointer, the rest its state
    cs = [{'Param': 'parent', 'Type': 'ObjectAttributes', 'Field': 'm_parent'},
          {'Param': 'ro', 'Type': 'bool', 'Field': 'm_read_only'},
          {'Param': 'sn', 'Type': 'uint', 'Field': 'm_sn'}]
    rec = {'Type': 'Rhino.Render.Decals', 'Kind': 'type', 'Name': 'Decals', 'Variant': 'portable',
           'ParentFields': ['m_parent'], 'CtorStore': cs}
    try:
        R.index_types({'members': [rec]})
        assert R.BY_REG['DecalsX']['state'] == [('bool', 'm_read_only'), ('unsigned int', 'm_sn')]
        R.index_types({'members': [dict(rec, CtorStore=cs[:2] + [dict(cs[2], Field=None)])]})   # not stored
        assert 'state' not in R.BY_REG['DecalsX']
        R.index_types({'members': [dict(rec, CtorStore=cs[:2] + [dict(cs[2], Type='string')])]})
        assert 'state' not in R.BY_REG['DecalsX']
        R.index_types({'members': [dict(rec, ParentFields=None)]})                              # parent not first
        assert 'state' not in R.BY_REG['DecalsX']
    finally:
        R.index_types({'members': []})


def test_snapshot_enumerator_needs_a_copying_item_constructor():
    # G60: Decals.GetEnumerator -> DecalEnumerator: a snapshot of shared_ptr handles,
    # each item its own Copy (`new Decal(sp, this, sn)` copies), !IsVisible passed over
    T = 'Rhino.Render.'
    sp = 'std::shared_ptr<ON_Decal>*'
    fns = {'Raw': {'name': 'Raw', 'returns': 'ON_Decal*', 'params': [{'type': sp, 'name': 's'}]},
           'Del': {'name': 'Del', 'returns': 'void', 'params': [{'type': sp, 'name': 's'}]},
           'Cp': {'name': 'Cp', 'returns': sp, 'params': [{'type': sp, 'name': 's'}]},
           'Fill': {'name': 'Fill', 'returns': 'void',
                    'params': [{'type': 'const ON_3dmObjectAttributes*', 'name': 'a'},
                               {'type': 'ON_SimpleArray<%s>*' % sp, 'name': 'b'}]}}
    sh = {'Field': 'm_sp', 'Raw': 'Raw', 'Delete': 'Del', 'Copy': 'Cp', 'CopyArities': [3]}
    sn = {'Item': 'Decal', 'Fill': 'Fill', 'OwnerParam': 0, 'ItemArity': 3, 'Skip': 'IsVisible', 'ElemDelete': 'Del'}

    def manifest(sh=sh, sn=sn):
        return {'members': [
            {'Type': T + 'Decal', 'Kind': 'type', 'Name': 'Decal', 'Variant': 'portable', 'SharedHandle': sh},
            {'Type': T + 'Decals', 'Kind': 'type', 'Name': 'Decals', 'Variant': 'portable', 'ParentFields': ['m_parent']},
            {'Type': T + 'DecalEnumerator', 'Kind': 'type', 'Name': 'DecalEnumerator', 'Variant': 'portable',
             'Internal': True, 'Snapshot': sn},
            {'Type': T + 'Decals', 'Kind': 'method', 'Name': 'GetEnumerator', 'Variant': 'portable', 'Body': 'construct',
             'Child': 'DecalEnumerator', 'ChildArgs': ['m_parent', 'm_sn']}]}
    DX, DS = R.BY_REG['DecalX'], R.BY_REG['DecalsX']
    vis = {'m': {'Name': 'IsVisible', 'Kind': 'property', 'Returns': 'bool'}, 'bind_args': [], 'static': False,
           'cpp': 'Gen_IsVisible'}

    def run(m, planned=(vis,)):
        R.index_types(m)
        R.PLANNED['DecalX'] = list(planned)
        return P.snapshot_iter(DS, [], m)
    saved = dict(R.C_BY_NAME)
    try:
        R.C_BY_NAME.update(fns)
        li = run(manifest())
        assert li and li['item'] is DX and li['snapshot']['skip'] == 'Gen_IsVisible'
        assert li['snapshot']['copy']['name'] == 'Cp' and li['snapshot']['fill']['name'] == 'Fill'
        assert run(manifest(sh=dict(sh, CopyArities=[1]))) is None      # the item adopts the element: refused
        assert run(manifest(), planned=()) is None                       # the skip property is not generated
        assert run(manifest(sn=dict(sn, ElemDelete='Other'))) is None   # freed with another function
        assert run(manifest(sn=dict(sn, OwnerParam=1))) is None         # the owner is not the parent field
    finally:
        R.C_BY_NAME.clear()
        R.C_BY_NAME.update(saved)
        R.index_types({'members': []})


def test_nested_enum_values_are_keyed_by_their_declaring_type():
    # RenderChannels.Modes vs DataSources.Modes: the short name is ambiguous,
    # the declaring-type key is not (RenderChannels.Mode's result select)
    m = {'members': [
        {'Type': 'Rhino.Render.RenderChannels', 'Kind': 'enum', 'Name': 'Modes', 'Variant': 'portable',
         'Values': ['Automatic', 'Custom']},
        {'Type': 'Rhino.Render.DataSources', 'Kind': 'enum', 'Name': 'Modes', 'Variant': 'portable',
         'Values': ['Unset', 'Grid', 'List', 'Tree']}]}
    try:
        R.index_types(m)
        assert 'Modes' not in R.ENUM_NAMED
        assert R.ENUM_NAMED['RenderChannels.Modes'] == {'Automatic': 0, 'Custom': 1}
        assert R.ENUM_NAMED['DataSources.Modes']['Tree'] == 3
    finally:
        R.index_types({'members': []})


def test_abstract_base_of_an_abstract_base_forwards_its_getters():
    # G62: GeometryBase : CommonObject -- both abstract; the derived abstract
    # class answers the root's getters with its own
    import emit_classes as E
    txt = E.emit_class(R.BY_REG['GeometryBaseX'], [], False, [])
    assert 'class BND_GeometryBaseX : public BND_CommonObjectX' in txt
    assert 'ON_Object* RH3DM_ObjPtr() const override { return RH3DM_GeomPtr(); }' in txt
    assert 'ON_Object* RH3DM_ObjPtrNC() const override { return RH3DM_GeomPtrNC(); }' in txt
    assert [a['reg'] for a in E.abstract_chain(R.BY_REG['GeometryBaseX'])] == ['GeometryBaseX', 'CommonObjectX']
    py, js = E.emit_registrations(R.BY_REG['GeometryBaseX'], [], set())
    assert 'py::class_<BND_GeometryBaseX, BND_CommonObjectX>' in py      # isinstance(brep, CommonObjectX)
    assert 'class_<BND_GeometryBaseX, base<BND_CommonObjectX>>' in js
    # a class whose native type is no ON_Object stays off the root (pilot-findings #31)
    assert R.BY_REG['CachedTextureCoordinatesX'].get('base') is None


def test_translated_markers_resolve_only_to_generated_value_members():
    # G63: `return LightStyle == LightStyle.WorldLinear;` translated with markers;
    # the getter must be a generated, value-returning, non-overridden member
    L = R.BY_REG['LightX']
    m = {'members': [
        {'Type': 'Rhino.DocObjects.LightStyle', 'Kind': 'enum', 'Name': 'LightStyle', 'Variant': 'portable',
         'Values': ['None = 0', 'WorldLinear = 11']},
        {'Type': L['type'], 'Kind': 'type', 'Name': 'Light', 'Variant': 'portable', 'Bases': ['GeometryBase']},
        {'Type': L['type'], 'Kind': 'property', 'Name': 'LightStyle', 'Variant': 'portable', 'Returns': 'LightStyle'}]}
    try:
        R.index_types(m)
        R.CS_ENUMS.setdefault('LightStyle', {'cpp': 'LightStyle', 'gen': False})
        getter = {'m': m['members'][2], 'bind_args': [], 'static': False, 'cpp': 'Gen_LightStyle'}
        text = 'return $$get:LightStyle$$() == $$enum:LightStyle:WorldLinear$$;'
        out, deps, writes = P.resolve_markers(text, L, {'LightX': [getter]}, m)
        assert out == 'return Gen_LightStyle() == (LightStyle)11;' and deps == [getter] and not writes
        # not generated -> refused; a class-returning getter -> refused
        assert P.resolve_markers(text, L, {'LightX': []}, m)[0] is None
        objget = dict(getter, m=dict(m['members'][2], Returns='Mesh'))
        assert P.resolve_markers(text, L, {'LightX': [objget]}, m)[0] is None
        assert P.resolve_markers('$$enum:LightStyle:Nope$$', L, {}, m)[0] is None
        assert P.resolve_markers('$$set:LightStyle$$(v)', L, {'LightX': [getter]}, m)[0] is None
    finally:
        R.index_types({'members': []})


def test_translated_call_is_refused_when_a_derived_type_overrides_the_callee():
    # a non-virtual C++ call from the base would miss RhinoCommon's override
    G, C = R.BY_REG['GeometryBaseX'], R.BY_REG['CurveX']
    m = {'members': [
        {'Type': G['type'], 'Kind': 'type', 'Name': 'GeometryBase', 'Variant': 'portable'},
        {'Type': C['type'], 'Kind': 'type', 'Name': 'Curve', 'Variant': 'portable', 'Bases': ['GeometryBase']},
        {'Type': G['type'], 'Kind': 'property', 'Name': 'Thing', 'Variant': 'portable', 'Returns': 'double'},
        {'Type': C['type'], 'Kind': 'property', 'Name': 'Thing', 'Variant': 'portable', 'Returns': 'double'}]}
    try:
        R.index_types(m)
        getter = {'m': m['members'][2], 'bind_args': [], 'static': False, 'cpp': 'Gen_Thing'}
        out, why, _ = P.resolve_markers('return $$get:Thing$$();', G, {'GeometryBaseX': [getter]}, m)
        assert out is None and 'overridden' in why
    finally:
        R.index_types({'members': []})


def test_data_class_arguments_bind_by_pointer_only_when_optional():
    # G64: File3dm.Write(path, options) with `options = options ?? new File3dmWriteOptions();`
    # -> `const BND_File3dmWriteOptionsX* options` read through a defaulted local;
    # ViewInfo.SetPageSettings(pageSettings) null-throws -> by reference
    saved = (dict(R.DATA_CPP), dict(R.DATA_INFO))
    try:
        R.DATA_CPP['File3dmWriteOptions'] = 'BND_File3dmWriteOptionsX'
        R.DATA_INFO['File3dmWriteOptions'] = {'fields': {'RenderMeshFlags'}, 'props': {'SaveUserData': True, 'Version': True}, 'methods': set()}
        FX = R.BY_REG['File3dmX']
        m = {'Type': FX['type'], 'Kind': 'method', 'Name': 'Write', 'Variant': 'portable', 'Body': 'trivial',
             'Returns': 'bool', 'ParamDefaults': {'options': 'File3dmWriteOptions'},
             'Params': [{'Name': 'path', 'Type': 'string', 'Mode': ''}, {'Name': 'options', 'Type': 'File3dmWriteOptions', 'Mode': ''}],
             'Calls': [{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'p', 'Value': 'NonConstPointer'}, {'Kind': 'param', 'Text': 'path'},
                                              {'Kind': 'parammember', 'Text': 'options.RenderMeshFlags', 'Value': 'uint'},
                                              {'Kind': 'parammember', 'Text': 'options.SaveUserData', 'Value': None}]}]}
        f = cfn('bool', 'ONX_Model*', 'const RHMONO_STRING*', 'unsigned int', 'bool')
        p, why = P.plan(m, f, FX)
        assert why is None and p['bind_args'][1] == ('const BND_File3dmWriteOptionsX*', 'options')
        assert p['locals'] == ['const BND_File3dmWriteOptionsX& rh3dm_options = options ? *options : BND_File3dmWriteOptionsX();']
        assert p['call_exprs'][2:] == ['(unsigned int)(rh3dm_options.RenderMeshFlags)', 'rh3dm_options.SaveUserData()']
        # no default and no null check: by reference, the member read through `.`
        m2 = dict(m, ParamDefaults=None)
        p2, why2 = P.plan(m2, f, FX)
        assert why2 is None and p2['bind_args'][1] == ('const BND_File3dmWriteOptionsX&', 'options')
        assert p2['call_exprs'][3] == 'options.SaveUserData()'
        # a member the data class did not translate: refused
        m3 = dict(m, Calls=[{'Name': 'F', 'Args': m['Calls'][0]['Args'][:3] + [{'Kind': 'parammember', 'Text': 'options.Nope', 'Value': None}]}])
        assert P.plan(m3, f, FX)[0] is None
    finally:
        R.DATA_CPP.clear(); R.DATA_CPP.update(saved[0]); R.DATA_INFO.clear(); R.DATA_INFO.update(saved[1])


def test_data_class_made_by_its_static_bridge():
    # G64: `ConstructionPlane rc = ConstructionPlane.FromIntPtr(p); return rc;` -> datafrom
    saved = (dict(R.DATA_CPP), dict(R.DATA_INFO))
    try:
        R.DATA_CPP['ConstructionPlane'] = 'BND_ConstructionPlaneX'
        R.DATA_INFO['ConstructionPlane'] = {'fields': set(), 'props': {}, 'methods': {'FromIntPtr'}}
        m = member('ConstructionPlane', params=[('', 'int', 'index')], calls=[SELF, {'Kind': 'param', 'Text': 'index'}],
                   Body='pattern', Wrap='ConstructionPlane', WrapStatic='FromIntPtr', WrapArity=1)
        f = cfn('const ON_3dmConstructionPlane*', 'const ON_Curve*', 'int')
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['shape'] == ('datafrom', 'ConstructionPlane', 'FromIntPtr')
        # `new ConstructionPlane(p)` is not the bridge, and a bridge the class did not
        # translate is none either: refused (no registry class to wrap with)
        assert P.plan(dict(m, WrapStatic=None), f, CURVE)[0] is None
        assert P.plan(dict(m, WrapStatic='FromOther'), f, CURVE)[0] is None
    finally:
        R.DATA_CPP.clear(); R.DATA_CPP.update(saved[0]); R.DATA_INFO.clear(); R.DATA_INFO.update(saved[1])


def _with_data(cs, cpp, fields=(), props=None, methods=()):
    R.DATA_CPP[cs] = cpp
    R.DATA_INFO[cs] = {'fields': set(fields), 'props': dict(props or {}), 'methods': set(methods)}


def test_data_object_assembled_from_the_calls_out_locals():
    # G65: ViewInfo.GetPageSettings -- `var rc = new PageSettings(); F(this, ref a, ..., sh);
    # rc.A = a; ... rc.PrinterName = sh.ToString(); return rc;`
    saved = (dict(R.DATA_CPP), dict(R.DATA_INFO))
    try:
        _with_data('PageSettings', 'BND_PageSettingsX', props={'PageNumber': True, 'PrinterName': True})
        m = member('PageSettings', calls=[SELF, {'Kind': 'out', 'Text': 'n', 'Value': '0'}, {'Kind': 'expr', 'Text': 'sh.NonConstPointer()'}],
                   Body='pattern', Holder='StringHolder', HolderArg=2,
                   DataFill={'Local': 'rc', 'Type': 'PageSettings', 'Assigns': [['PageNumber', 'n'], ['PrinterName', '@holder']]})
        f = cfn('void', 'const ON_Curve*', 'int*', 'CRhCmnStringHolder*')
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['shape'] == ('datafill', 'PageSettings', [('PageNumber', 'n'), ('PrinterName', 'RH3DM_W(holder_out)')])
        # a member without a setter, or an out local the object does not take: refused
        m2 = dict(m, DataFill={'Local': 'rc', 'Type': 'PageSettings', 'Assigns': [['Nope', 'n'], ['PrinterName', '@holder']]})
        assert P.plan(m2, f, CURVE)[0] is None
        m3 = dict(m, DataFill={'Local': 'rc', 'Type': 'PageSettings', 'Assigns': [['PrinterName', '@holder']]})
        assert P.plan(m3, f, CURVE)[0] is None
    finally:
        R.DATA_CPP.clear(); R.DATA_CPP.update(saved[0]); R.DATA_INFO.clear(); R.DATA_INFO.update(saved[1])


def test_native_result_handed_to_a_data_arguments_bridge():
    # G65: File3dmSettings.GridDefaults setter -- `p = F(this); if (IntPtr.Zero != p) value.SetupNativePointer(p);`
    saved = (dict(R.DATA_CPP), dict(R.DATA_INFO))
    try:
        _with_data('Defaults', 'BND_DefaultsX', methods=['SetupNativePointer'])
        m = member('void', params=[('', 'Defaults', 'value')], calls=[SELF], Body='pattern',
                   DataApply={'Param': 'value', 'Method': 'SetupNativePointer'},
                   Guards=[{'Cond': 'value == null', 'Exception': 'ArgumentNullException', 'Ids': ['value'], 'NullCheckOf': 'value'}])
        f = cfn('ON_X*', 'const ON_Curve*')
        p, why = P.plan(m, f, CURVE)
        assert why is None and p['shape'] == ('dataapply', 'const_cast<BND_DefaultsX&>(value).SetupNativePointer')
        assert p['bind_args'] == [('const BND_DefaultsX&', 'value')]
        assert P.plan(dict(m, DataApply={'Param': 'value', 'Method': 'Other'}), f, CURVE)[0] is None
    finally:
        R.DATA_CPP.clear(); R.DATA_CPP.update(saved[0]); R.DATA_INFO.clear(); R.DATA_INFO.update(saved[1])


def test_temporary_native_from_a_data_argument_guards_the_sequence():
    # G65: File3dmNamedConstructionPlanes.Add -- `IntPtr L = cplane.CopyToNative();
    # if (L != IntPtr.Zero) { UNM.Add2(parent, L); UNM.Delete(L); }`
    saved = (dict(R.DATA_CPP), dict(R.DATA_INFO))
    try:
        _with_data('ConstructionPlane', 'BND_ConstructionPlaneX', methods=['CopyToNative'])
        tn = {'Local': 'L', 'Param': 'cplane', 'Method': 'CopyToNative', 'Guarded': True}
        m = member('void', params=[('', 'ConstructionPlane', 'cplane')], Body='pattern', Sequence=True, TempNative=tn,
                   Guards=[{'Cond': 'null == cplane', 'Exception': 'ArgumentNullException', 'Ids': ['cplane'], 'NullCheckOf': 'cplane'}],
                   Calls=[{'Name': 'A', 'Args': [SELF, {'Kind': 'tempnative', 'Text': 'cplane', 'Value': 'CopyToNative'}]},
                          {'Name': 'D', 'Args': [{'Kind': 'tempnative', 'Text': 'cplane', 'Value': 'CopyToNative'}]}])
        fns = {'A': dict(cfn('void', 'const ON_Curve*', 'const ON_X*'), name='A'), 'D': dict(cfn('void', 'ON_X*'), name='D')}
        p, why = P.plan_sequence(m, CURVE, fns)
        assert why is None
        assert p['locals'] == ['void* rh3dm_tmp_cplane = const_cast<BND_ConstructionPlaneX&>(cplane).CopyToNative();']
        assert p['shape'] == ('seq', ['A(m_ptr, (const ON_X*)rh3dm_tmp_cplane)', 'D((ON_X*)rh3dm_tmp_cplane)'], 'rh3dm_tmp_cplane')
        assert P.plan_sequence(dict(m, TempNative=None), CURVE, fns)[0] is None     # the local without its declaration
    finally:
        R.DATA_CPP.clear(); R.DATA_CPP.update(saved[0]); R.DATA_INFO.clear(); R.DATA_INFO.update(saved[1])


def test_factory_result_filtered_by_the_targets_bool_getter():
    # G66: CommonComponentTable.FindId -- `if (m_manifest.FindId(id, ComponentType) is T item
    # && !item.IsDeleted) return item; return null;` -- inlined to the manifest's factory;
    # the proxy is kept only when the generated IsDeleted getter says so
    fac = {'Name': 'Instantiate', 'SwitchArg': 1,
           'Args': [{'Kind': 'expr', 'Text': 'm_parent'}, {'Kind': 'expr', 'Text': 'type'}, {'Kind': 'param', 'Text': 'id'}, {'Kind': 'expr', 'Text': 'index'}],
           'Cases': {'5': {'Args': ['id', 'm_parent'], 'New': 'Layer', 'Value': '5'}}}
    LT = R.BY_REG['File3dmLayerTableX']
    m = {'Type': LT['type'], 'Kind': 'method', 'Name': 'FindId', 'Variant': 'portable', 'Body': 'pattern',
         'Returns': 'Layer', 'Params': [{'Name': 'id', 'Type': 'Guid', 'Mode': ''}], 'Factory': fac, 'ResultFilter': 'IsDeleted',
         'Calls': [{'Name': 'F', 'Args': [{'Kind': 'fieldptr', 'Text': 'm_parent', 'Value': 'ConstPointer'},
                                          {'Kind': 'out', 'Text': 'type', 'Value': 'ComponentType'},
                                          {'Kind': 'param', 'Text': 'id'},
                                          {'Kind': 'out', 'Text': 'index', 'Value': '-1'}]}]}
    f = cfn('const ON_ModelComponent*', 'const ONX_Model*', 'ON_ModelComponent::Type*', 'ON_UUID', 'int*')
    saved = dict(R.CONST_PROPS)
    LY = R.BY_CS['Layer']
    had_proxy = LY.get('proxy')
    try:
        R.CONST_PROPS[(LT['type'], 'ComponentType')] = ('ModelComponentType.Layer', '5')
        LY['proxy'] = had_proxy or {'Resolver': 'R', 'NonConst': 'resolve', 'Dup': None, 'Origin': 'file'}
        p, why = P.plan(m, f, LT)
        assert why is None and p['shape'] == ('proxywrap', R.BY_CS['Layer'], 'Binding_to_ON_UUID(id)')
        assert 'ON_ModelComponent::Type type = (ON_ModelComponent::Type)5;' in p['locals']
        assert p['call_exprs'][1] == '&type'
        # the filter resolves after planning, against the target's generated bool getter
        assert p['m']['ResultFilter'] == 'IsDeleted'
    finally:
        R.CONST_PROPS.clear(); R.CONST_PROPS.update(saved)
        if had_proxy is None:
            LY.pop('proxy', None)


def test_chain_through_a_value_type_sibling():
    # G67: PointCloudItem.X -- `return Location.X;` and `Point3d pt = Location; pt.X = value;
    # Location = pt;` -- the sibling returns a translated value type
    PI = R.BY_REG['PointCloudItemX']
    loc_get = {'m': {'Kind': 'property', 'Name': 'Location', 'Returns': 'Point3d'}, 'bind_args': [], 'static': False,
               'shape': ('fromout', 'Point3d', 'rc'), 'cpp': 'Gen_Location'}
    loc_set = {'m': {'Kind': 'method', 'Name': 'Location'}, 'bind_args': [('BND_Point3dX', 'value')], 'static': False,
               'shape': ('direct', 'void'), 'cpp': 'Gen_set_Location', 'setter_of': loc_get}
    loc_get['setter'] = loc_set
    x = {'Type': PI['type'], 'Kind': 'property', 'Name': 'X', 'Variant': 'portable', 'Body': 'chain',
         'Returns': 'double', 'Delegates': 'Location.X'}
    saved = (set(R.VT_PROPS), set(R.VT_PROP_SET), dict(R.VT_PROP_RET))
    try:
        R.VT_PROPS.add(('Point3d', 'X')); R.VT_PROP_SET.add(('Point3d', 'X')); R.VT_PROP_RET[('Point3d', 'X')] = 'double'
        p, why = P.plan_chain(x, PI, [loc_get, loc_set], 'Location.X', False)
        assert why is None and p['shape'] == ('chainvt', loc_get, 'X', False)
        sp, why = P.plan_chain(x, PI, [loc_get, loc_set], 'Location.X', True)
        assert why is None and sp['shape'] == ('chainvt', loc_get, 'X', True) and sp['bind_args'] == [('double', 'value')]
        R.VT_PROP_SET.discard(('Point3d', 'X'))
        assert P.plan_chain(x, PI, [loc_get, loc_set], 'Location.X', True)[0] is None      # no translated setter
        assert P.plan_chain(x, PI, [loc_get, loc_set], 'Location.Nope', False)[0] is None
    finally:
        R.VT_PROPS.clear(); R.VT_PROPS.update(saved[0]); R.VT_PROP_SET.clear(); R.VT_PROP_SET.update(saved[1])
        R.VT_PROP_RET.clear(); R.VT_PROP_RET.update(saved[2])


def test_datetime_result_from_the_out_locals():
    # G68: File3dm.Created -- `if (F(..., ref second, ..., ref year)) return new DateTime(year,
    # month+1, day, hour, minute, second); return DateTime.MinValue;`
    outs = [{'Kind': 'out', 'Text': n, 'Value': '0'} for n in ('second', 'minute', 'hour', 'day', 'month', 'year')]
    m = member('DateTime', calls=[SELF, {'Kind': 'literal', 'Text': 'true'}] + outs, Body='pattern',
               DateTime=['year', 'month+1', 'day', 'hour', 'minute', 'second'],
               Success={'Out': '@datetime', 'Form': 'return', 'Fallback': 'DateTime.MinValue'})
    f = cfn('bool', 'const ON_Curve*', 'bool', *(['int*'] * 6))
    p, why = P.plan(m, f, CURVE)
    assert why is None and p['shape'] == ('datetime', ['year', 'month+1', 'day', 'hour', 'minute', 'second'])
    assert p['success'] is None and 'int year = 0;' in p['locals']
    assert P.plan(dict(m, DateTime=['year', 'month+2', 'day', 'hour', 'minute', 'second']), f, CURVE)[0] is None
    assert P.plan(dict(m, Success={'Out': '@datetime', 'Form': 'return', 'Fallback': 'DateTime.Now'}), f, CURVE)[0] is None


def test_native_call_guard_takes_the_self_pointer():
    # G68: MeshVertexList.Hide -- `if (UnsafeNativeMethods.ON_Mesh_GetHiddenValue(const_ptr_mesh, i)) return;`
    g = {'Cond': 'UnsafeNativeMethods.F(const_ptr_mesh, i)', 'Ids': ['const_ptr_mesh', 'i'], 'Return': '',
         'Calls': [{'Name': 'UnsafeNativeMethods.F', 'Args': [{'Kind': 'expr', 'Text': 'const_ptr_mesh'}, {'Kind': 'expr', 'Text': 'i'}]}],
         'Native': [{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'const_ptr_mesh'}, {'Kind': 'expr', 'Text': 'i'}]}]}
    fns = {'F': dict(cfn('bool', 'const ON_Curve*', 'int'), name='F')}
    saved = dict(R.C_BY_NAME)
    try:
        R.C_BY_NAME.update(fns)
        p = {'m': {'Name': 'M'}, 'guards': [], 'static': False}
        cond = P.guard_calls_cpp(dict(g), g['Cond'], CURVE, {'i'}, {'i': 'int'}, p)
        assert cond == 'F(m_ptr, i)'
        # the pointer must be the first argument of the self type
        g2 = dict(g, Calls=[{'Name': 'UnsafeNativeMethods.F', 'Args': [{'Kind': 'expr', 'Text': 'i'}, {'Kind': 'expr', 'Text': 'const_ptr_mesh'}]}],
                  Native=[{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'i'}, {'Kind': 'expr', 'Text': 'const_ptr_mesh'}]}])
        assert P.guard_calls_cpp(g2, g2['Cond'], CURVE, {'i'}, {'i': 'int'}, p) is None
    finally:
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved)


def test_override_returning_the_base_wraps_as_the_class_it_constructs():
    # G69: `public override GeometryBase Duplicate() { p = ON_Brep_New(this); return new Brep(p, null); }`
    BR = R.BY_CS['Brep']
    m = {'Type': BR['type'], 'Kind': 'method', 'Name': 'Duplicate', 'Variant': 'portable', 'Body': 'pattern',
         'Returns': 'GeometryBase', 'Params': [], 'Wrap': 'Brep', 'WrapArity': 2,
         'Calls': [{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'p', 'Value': 'ConstPointer'}]}]}
    p, why = P.plan(m, cfn('ON_Brep*', 'const ON_Brep*'), BR)
    assert why is None and p['shape'] == ('wrap', 'Brep')
    # the base's own: `return CreateGeometryHelper(p, null);` -- an owned wrap of the root,
    # dispatched on the runtime type; the C side returns ON_Object*, cast by the root
    GB = R.BY_CS['GeometryBase']
    m2 = dict(m, Type=GB['type'], Wrap='@return', WrapArity=0)
    p2, why2 = P.plan(m2, cfn('ON_Object*', 'const ON_Object*'), GB)
    assert why2 is None and p2['shape'] == ('wrap', 'GeometryBase') and p2['wrap_cast'] is True
    # a class that does not derive the declared one is no wrap of it
    assert P.plan(dict(m, Wrap='Layer', WrapArity=2), cfn('ON_Layer*', 'const ON_Brep*'), BR)[0] is None


def test_handle_from_a_const_pointer_the_c_side_allocated():
    # G69: Texture.FileReference -- `IntPtr p = UNM.ON_Texture_GetFileReference(this);
    # return FileReference.ConstructAndOwnFromConstPtr(p);` (a new ON_FileReference, const)
    TX = R.BY_CS['Texture']
    m = {'Type': TX['type'], 'Kind': 'property', 'Name': 'FileReference', 'Variant': 'portable', 'Body': 'pattern',
         'Returns': 'FileReference', 'Wrap': 'FileReference', 'WrapArity': 1, 'WrapChecked': True,
         'Calls': [{'Name': 'F', 'Args': [{'Kind': 'expr', 'Text': 'p', 'Value': 'ConstPointer'}]}]}
    FR = R.BY_CS['FileReference']
    saved = (FR.get('handle'), dict(R.TYPE_RECS.get(FR['type']) or {}))
    try:
        FR['handle'] = {'Delete': 'ON_FileReference_Delete', 'Field': 'm_const_ptr'}
        R.TYPE_RECS[FR['type']] = dict(R.TYPE_RECS.get(FR['type']) or {}, PtrCtor='field:m_const_ptr+nullthrow')
        p, why = P.plan(m, cfn('const ON_FileReference*', 'const ON_Texture*'), TX)
        assert why is None and p['shape'] == ('wrap', 'FileReference')
        assert P.plan(dict(m, WrapArity=0), cfn('const ON_FileReference*', 'const ON_Texture*'), TX)[0] is None
    finally:
        if saved[0] is None:
            FR.pop('handle', None)
        else:
            FR['handle'] = saved[0]
        R.TYPE_RECS[FR['type']] = saved[1]


def test_new_self_through_the_internal_copy_constructor():
    # G69: PolyCurve.DuplicatePolyCurve -- `return new PolyCurve(this);` where the
    # internal `PolyCurve(PolyCurve other)` is `ON_PolyCurve_New(other.ConstPointer())`
    PC = R.BY_CS['PolyCurve']
    x = {'Type': PC['type'], 'Kind': 'method', 'Name': 'DuplicatePolyCurve', 'Variant': 'portable', 'Body': 'construct',
         'Returns': 'PolyCurve', 'Params': [], 'Child': 'PolyCurve', 'ChildArgs': ['this'], 'ChildArgKinds': ['this']}
    saved = (dict(R.TYPE_RECS.get(PC['type']) or {}), dict(R.C_BY_NAME))
    try:
        R.TYPE_RECS[PC['type']] = dict(saved[0], InternalCopy='ON_PolyCurve_New')
        R.C_BY_NAME['ON_PolyCurve_New'] = dict(cfn('ON_PolyCurve*', 'ON_PolyCurve*'), name='ON_PolyCurve_New')
        p, why = P.plan_construct_nocall(x, PC)
        assert why is None and p['shape'][0] == 'selfcopy' and p['shape'][1]['name'] == 'ON_PolyCurve_New'
        assert p['shape'][2].startswith('(ON_PolyCurve*)')
        # a native of another shape is not the copy
        R.C_BY_NAME['ON_PolyCurve_New'] = dict(cfn('ON_PolyCurve*', 'ON_PolyCurve*', 'int'), name='ON_PolyCurve_New')
        assert P.plan_construct_nocall(x, PC)[0] is None
    finally:
        R.TYPE_RECS[PC['type']] = saved[0]
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved[1])


def test_js_array_shape_for_vector_parameters():
    # G70: a std::vector parameter arrives in JS as an array (emscripten::val)
    import emit_classes as E
    key, check, conv = E.js_class_shape('emscripten::val')
    # G83: or a point list (Point3dListX / PolylineX: anything with toArray())
    assert key == 'array' and check.format(v='v0') == 'RH3DM_JsIsSeq(v0)' and conv.format(v='v0') == 'v0'


def test_table_iteration_through_an_internal_manifest_enumerator():
    # G71: File3dmEmbeddedFiles.GetEnumerator => new File3dmEmbeddedFileEnumerator(_file3dm),
    # whose MoveNext walks _manifest.GetEnumerator(ModelComponentType.EmbeddedFile)
    EF, EFS = R.BY_REG['File3dmEmbeddedFileX'], R.BY_REG['File3dmEmbeddedFilesX']
    m = {'members': [
        {'Type': 'Rhino.DocObjects.ModelComponentType', 'Kind': 'enum', 'Name': 'ModelComponentType', 'Variant': 'portable',
         'Values': ['Unset = 0', 'EmbeddedFile = 15']},
        {'Type': EFS['type'], 'Kind': 'type', 'Name': 'File3dmEmbeddedFiles', 'Variant': 'portable', 'ParentFields': ['_file3dm']},
        {'Type': 'Rhino.FileIO.File3dmEmbeddedFileEnumerator', 'Kind': 'type', 'Name': 'File3dmEmbeddedFileEnumerator', 'Variant': 'portable',
         'Internal': True, 'ManifestEnumerator': {'Item': 'File3dmEmbeddedFile', 'Type': 'ModelComponentType.EmbeddedFile'}},
        {'Type': EFS['type'], 'Kind': 'method', 'Name': 'GetEnumerator', 'Variant': 'portable', 'Body': 'construct',
         'Child': 'File3dmEmbeddedFileEnumerator', 'ChildArgs': ['_file3dm'], 'ChildArgKinds': ['field']}]}
    saved = (EF.get('proxy'), dict(R.C_BY_NAME))
    try:
        R.index_types(m)
        EF['proxy'] = {'Resolver': 'ONX_Model_GetModelComponentPointer', 'NonConst': 'cow', 'Dup': 'D', 'Origin': 'file'}
        for n in ('ONX_ModelComponentIterator_New', 'ONX_ModelComponentIterator_GetNext', 'ONX_ModelComponentIterator_Delete'):
            R.C_BY_NAME[n] = {'name': n, 'returns': 'void', 'params': []}
        tp = P.table_protocol(EFS, m, [])
        assert tp and tp['item'] is EF and tp['type_value'] == '15' and tp['count'] is None and tp['find'] is None
        # a component type the enum does not name: no grant
        m['members'][2]['ManifestEnumerator'] = {'Item': 'File3dmEmbeddedFile', 'Type': 'ModelComponentType.Nope'}
        R.index_types(m)
        assert P.table_protocol(EFS, m, []) is None
    finally:
        if saved[0] is None:
            EF.pop('proxy', None)
        else:
            EF['proxy'] = saved[0]
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved[1])
        R.index_types({'members': []})


def test_element_loop_plans_one_native_call_per_element():
    # G72: MeshVertexList.ToPoint3dArray: `int count = Count; var rc = new Point3d[count];
    # for (i < count) { var pt = new Point3d(); F(ptr, i, ref pt); rc[i] = pt; } return rc;`
    af = {'Local': 'rc', 'Elem': 'Point3d', 'Count': 'count', 'CountFrom': 'prop:Count',
          'FillForm': 'loop', 'Index': 'i', 'ElemLocal': 'pt'}
    calls = [SELF, {'Kind': 'local', 'Text': 'i', 'Value': '0'}, {'Kind': 'out', 'Text': 'pt', 'Value': None}]
    x = dict(member('Point3d[]', calls=calls), Body='arrayfill', ArrayFill=af, ResultUnused=True)
    fns = {'F': cfn('bool', 'const ON_Curve*', 'int', 'ON_3dPoint*')}
    p, why = P.plan_arrayfill(x, CURVE, fns)
    assert why is None and p['shape'][0] == 'arrayfill' and p['shape'][1]['form'] == 'loop'
    assert p['shape'][1]['count_prop'] == 'Count' and p['shape'][1]['item'] == '{n}'
    assert p['call_exprs'][1:] == ['rh3dm_i', 'RH3DM_Ptr(&pt)'] and p['locals'] == ['BND_Point3dX pt;']
    # ToPoint3fArray: `rc[i] = new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z)` -- the
    # narrower value type's constructor over the element's fields
    R.VT_CPP['Point3f'] = 'BND_Point3fX'
    try:
        p, why = P.plan_arrayfill(dict(x, Returns='Point3f[]', ArrayFill=dict(af, ElemConv='narrow:Point3f')), CURVE, fns)
        assert why is None and p['shape'][1]['item'] == 'BND_Point3fX((float){n}.m_x, (float){n}.m_y, (float){n}.m_z)'
    finally:
        R.VT_CPP.pop('Point3f', None)
    # the index must be an int parameter of the native; the count a sibling property
    assert P.plan_arrayfill(x, CURVE, {'F': cfn('bool', 'const ON_Curve*', 'double', 'ON_3dPoint*')})[0] is None
    assert P.plan_arrayfill(dict(x, ArrayFill=dict(af, CountFrom='call')), CURVE, fns)[0] is None
    assert P.plan_arrayfill(dict(x, Returns='Point3f[]'), CURVE, fns)[0] is None


def test_wrap_tuple_out_pointers_become_a_nullable_tuple():
    # G72: Curve.Split(double): `IntPtr a = IntPtr.Zero; ... bool rc = F(ptr, t, ref a, ref b);
    # Curve[] o = new Curve[2]; if (a != IntPtr.Zero) o[0] = CreateGeometryHelper(a, null) as Curve;
    # ... return rc ? o : null;`
    wt = {'Form': 'outptr', 'Call': 'F', 'Locals': ['leftptr', 'rightptr'], 'Wrap': 'Curve', 'Success': 'rc', 'Checked': True}
    calls = [SELF, {'Kind': 'param', 'Text': 't'}, {'Kind': 'out', 'Text': 'leftptr', 'Value': 'IntPtr.Zero'},
             {'Kind': 'out', 'Text': 'rightptr', 'Value': 'IntPtr.Zero'}]
    x = dict(member('Curve[]', params=[('', 'double', 't')], calls=calls), Body='wraptuple', WrapTuple=wt)
    fns = {'F': cfn('bool', 'const ON_Curve*', 'double', 'ON_Curve**', 'ON_Curve**')}
    p, why = P.plan_wraptuple(x, CURVE, fns)
    assert why is None and p['shape'][0] == 'wraptuple' and p['shape'][1]['form'] == 'outptr'
    assert p['locals'] == ['ON_Curve* leftptr = nullptr;', 'ON_Curve* rightptr = nullptr;']
    assert p['call_exprs'][2:] == ['&leftptr', '&rightptr']
    assert p['shape'][1]['fail'] == ['delete leftptr;', 'delete rightptr;']
    assert p['shape'][1]['item'] == '{p} ? RH3DM_Wrap_CurveX({p}) : (BND_CurveX*)nullptr'
    # a single pointer is not an out pointer; a non-array result or a void status is refused
    assert P.plan_wraptuple(x, CURVE, {'F': cfn('bool', 'const ON_Curve*', 'double', 'ON_Curve*', 'ON_Curve*')})[0] is None
    assert P.plan_wraptuple(dict(x, Returns='Curve'), CURVE, fns)[0] is None
    assert P.plan_wraptuple(x, CURVE, {'F': cfn('void', 'const ON_Curve*', 'double', 'ON_Curve**', 'ON_Curve**')})[0] is None


def test_wrap_tuple_new_delete_pair_on_a_value_class_fills_in_place():
    # G72: BezierCurve.Split(t, out left, out right): `IntPtr a = New(); IntPtr b = New();
    # bool rc = F(ptr, t, a, b); if (rc) { left = new BezierCurve(a); ... } else { Delete(a); ... } return rc;`
    # -- the generated class holds the value, so the natives fill locals; (success, left, right)
    BEZ = R.BY_CS['BezierCurve']
    wt = {'Form': 'newptr', 'Call': 'F', 'Locals': ['ptr_left', 'ptr_right'], 'Outs': ['left', 'right'],
          'Wrap': 'BezierCurve', 'Success': 'rc', 'New': 'N', 'Delete': 'D'}
    calls = [SELF, {'Kind': 'param', 'Text': 't'}, {'Kind': 'expr', 'Text': 'ptr_left'}, {'Kind': 'expr', 'Text': 'ptr_right'}]
    x = dict(member('bool', params=[('', 'double', 't'), ('out', 'BezierCurve', 'left'), ('out', 'BezierCurve', 'right')], calls=calls),
             Type=BEZ['type'], Body='wraptuple', WrapTuple=wt)
    fns = {'F': cfn('bool', 'const ON_BezierCurve*', 'double', 'ON_BezierCurve*', 'ON_BezierCurve*'),
           'N': {'name': 'N', 'returns': 'ON_BezierCurve*', 'params': []},
           'D': {'name': 'D', 'returns': 'void', 'params': [{'type': 'ON_BezierCurve*'}]}}
    p, why = P.plan_wraptuple(x, BEZ, fns)
    assert why is None and p['shape'][1]['form'] == 'newptr' and p['shape'][1]['fail'] == []
    assert p['locals'] == ['ON_BezierCurve ptr_left;', 'ON_BezierCurve ptr_right;']
    assert p['call_exprs'][2:] == ['&ptr_left', '&ptr_right']
    assert p['shape'][1]['item'] == 'rh3dm_ok ? new BND_BezierCurveX({p}) : (BND_BezierCurveX*)nullptr'
    assert p['bind_args'] == [('double', 't')]
    # an out parameter typed differently from the wrap is refused
    bad = dict(x, Params=[{'Name': 't', 'Type': 'double', 'Mode': ''}, {'Name': 'left', 'Type': 'Curve', 'Mode': 'out'},
                          {'Name': 'right', 'Type': 'BezierCurve', 'Mode': 'out'}])
    assert P.plan_wraptuple(bad, BEZ, fns)[0] is None


def test_wrap_tuple_pointer_holder_loop_reads_the_elements_back():
    # G72: PolyCurve.Explode: `var curves = new SimpleArrayCurvePointer(); Fill(ptr, curves.NonConstPointer());
    # int n = Cnt(p); for (i < n) { IntPtr q = Get(p, i); if (IntPtr.Zero == q) continue;
    # IntPtr d = Dup(q); rc[i] = CreateGeometryHelper(d, null) as Curve; } curves.Dispose(); return rc;`
    POLY = R.BY_CS['PolyCurve']
    wt = {'Form': 'ptrloop', 'Call': 'Fill', 'Holder': 'SimpleArrayCurvePointer', 'Count': 'Cnt', 'Get': 'Get',
          'Dup': 'Dup', 'Wrap': 'Curve', 'Checked': True}
    calls = [SELF, {'Kind': 'newobj', 'Text': 'curves', 'Value': 'SimpleArrayCurvePointer'}]
    x = dict(member('Curve[]', calls=calls), Type=POLY['type'], Body='wraptuple', WrapTuple=wt, ResultUnused=True)
    x['Calls'][0]['Name'] = 'Fill'
    fns = {'Fill': {'name': 'Fill', 'returns': 'void', 'params': [{'type': 'const ON_PolyCurve*'}, {'type': 'ON_SimpleArray<ON_Curve*>*'}]},
           'Cnt': {'name': 'Cnt', 'returns': 'int', 'params': [{'type': 'const ON_SimpleArray<ON_Curve*>*'}]},
           'Get': {'name': 'Get', 'returns': 'ON_Curve*', 'params': [{'type': 'ON_SimpleArray<ON_Curve*>*'}, {'type': 'int'}]},
           'Dup': {'name': 'Dup', 'returns': 'ON_Curve*', 'params': [{'type': 'ON_Curve*'}]}}
    p, why = P.plan_wraptuple(x, POLY, fns)
    assert why is None and p['shape'][1]['form'] == 'ptrloop'
    assert p['shape'][1]['count'] == 'Cnt' and p['shape'][1]['get'] == 'Get' and p['shape'][1]['dup'] == 'Dup'
    assert 'ON_SimpleArray<ON_Curve*> holder_out;' in p['locals'] and p['call_exprs'][1] == '&holder_out'
    assert {f['name'] for f in p['extra_fns']} == {'Cnt', 'Get', 'Dup'}
    # Get returning a const pointer cannot be owned; the plain holder shape never takes a holder of pointers
    assert P.plan_wraptuple(x, POLY, dict(fns, Get=dict(fns['Get'], returns='const ON_Curve*')))[0] is None
    plain = dict(member('Curve[]', calls=[SELF, {'Kind': 'expr', 'Text': 'curves'}]), Holder='SimpleArrayCurvePointer', HolderArg=1)
    assert P.plan(plain, fns['Fill'], POLY)[0] is None


def test_holder_array_empty_when_the_result_says_so():
    # G73: BrepVertex.EdgeIndices: `int rc = F(brep, index, ei.m_ptr); return rc == 0 ? new int[0] : ei.ToArray();`
    calls = [SELF, {'Kind': 'expr', 'Text': 'ei.m_ptr'}]
    x = dict(member('int[]', calls=calls), Holder='SimpleArrayInt', HolderArg=1, HolderEmpty='== 0', Compare='== 0')
    p, why = P.plan(x, cfn('int', 'const ON_Curve*', 'ON_SimpleArray<int>*'), CURVE)
    assert why is None and p['shape'] == ('holder', 'SimpleArrayInt', '== 0')
    # the comparison must be a plain one on a numeric result; a void result has none
    assert P.plan(dict(x, HolderEmpty='== Other'), cfn('int', 'const ON_Curve*', 'ON_SimpleArray<int>*'), CURVE)[0] is None
    assert P.plan(x, cfn('void', 'const ON_Curve*', 'ON_SimpleArray<int>*'), CURVE)[0] is None


def test_out_params_with_a_wrapped_result_lead_the_tuple_with_the_object():
    # G73: Surface.ToNurbsSurface(tol, out accuracy): `IntPtr rc = F(ptr, tol, ref accuracy);
    # return rc == Zero ? null : new NurbsSurface(rc, null);` -> (NurbsSurfaceX | None, accuracy)
    calls = [SELF, {'Kind': 'param', 'Text': 'tolerance'}, {'Kind': 'out', 'Text': 'accuracy', 'Value': '0'}]
    x = dict(member('NurbsCurve', params=[('', 'double', 'tolerance'), ('out', 'int', 'accuracy')], calls=calls),
             Wrap='NurbsCurve', WrapArity=2, WrapChecked=True)
    p, why = P.plan(x, cfn('ON_NurbsCurve*', 'const ON_Curve*', 'double', 'int*'), CURVE)
    assert why is None and p['shape'] == ('tuple', ('result', 'BND_NurbsCurveX*', 'RH3DM_Wrap_NurbsCurveX({c})'), [('accuracy', 'int*')])
    # a C type that is not the wrapped class (or a derived one) is refused
    assert P.plan(x, cfn('ON_Surface*', 'const ON_Curve*', 'double', 'int*'), CURVE)[0] is None


def test_discarded_out_forward_replans_the_targets_native_call():
    # G73: Surface.ToNurbsSurface(): `int accuracy; return ToNurbsSurface(0.0, out accuracy);`
    calls = [SELF, {'Kind': 'param', 'Text': 'tolerance'}, {'Kind': 'out', 'Text': 'accuracy', 'Value': '0'}]
    tm = dict(member('NurbsCurve', params=[('', 'double', 'tolerance'), ('out', 'int', 'accuracy')], calls=calls),
              Wrap='NurbsCurve', WrapArity=2, WrapChecked=True, Signature='NurbsCurve M(double tolerance, out int accuracy)')
    fn = cfn('ON_NurbsCurve*', 'const ON_Curve*', 'double', 'int*')
    target, why = P.plan(tm, fn, CURVE)
    assert why is None
    x = dict(member('NurbsCurve'), Body='delegates', Delegates='M', Signature='NurbsCurve M()',
             DelegateArgs=[{'Kind': 'literal', 'Text': '0.0'}, {'Kind': 'discard', 'Text': 'accuracy'}])
    p, why = P.plan_forward(x, CURVE, {('M', 2): [target]})
    assert why is None and p['shape'] == ('wrap', 'NurbsCurve') and p['bind_args'] == []
    assert p['call_exprs'][1:] == ['0.0', '&accuracy'] and p['locals'] == ['int accuracy = 0;']
    # a forwarded argument the target's call does not take as a parameter is refused
    bad = dict(x, DelegateArgs=[{'Kind': 'expr', 'Text': 'Tolerance'}, {'Kind': 'discard', 'Text': 'accuracy'}])
    assert P.plan_forward(bad, CURVE, {('M', 2): [target]})[0] is None


def test_self_identity_guard_compares_native_pointers():
    # G73: Mesh.Append(Mesh other): `if (null == other || other.ConstPointer() == ConstPointer()) return;`
    g = {'Cond': 'null == other || other.ConstPointer() == ConstPointer()', 'Ids': ['other'], 'Exception': None, 'Return': '',
         'Calls': [{'Name': 'other.ConstPointer', 'Args': []}, {'Name': 'ConstPointer', 'Args': []}]}
    cond = P.self_identity_cpp(dict(g), 'nullptr == other || other.ConstPointer() == ConstPointer()', CURVE, {'other'}, {'other': 'Curve'})
    assert cond == 'nullptr == other || (other->m_ptr == m_ptr)'
    # not a parameter, or another call in the guard: refused
    assert P.self_identity_cpp(dict(g), 'other.ConstPointer() == ConstPointer()', CURVE, set(), {'other': 'Curve'}) is None
    g2 = dict(g, Calls=g['Calls'] + [{'Name': 'IsValid', 'Args': []}])
    assert P.self_identity_cpp(g2, 'other.ConstPointer() == ConstPointer() && IsValid()', CURVE, {'other'}, {'other': 'Curve'}) is None


def test_class_typed_values_resolve_against_the_other_class(monkeypatch):
    # G74: `$$class:T$$` / `$$new:T:$$` / `(x)$$acc:T$$$$oget:T:N$$()` / `$$as:T$$(..)` against T's plans
    C, N = R.BY_CS['Curve'], R.BY_CS['NurbsCurve']
    m = {'members': [{'Type': N['type'], 'Kind': 'type', 'Name': 'NurbsCurve', 'Variant': 'portable', 'Bases': ['Curve']},
                     {'Type': N['type'], 'Kind': 'property', 'Name': 'Degree', 'Variant': 'portable', 'Returns': 'int'},
                     {'Type': C['type'], 'Kind': 'method', 'Name': 'Duplicate', 'Variant': 'portable', 'Returns': 'GeometryBase'}]}
    degree = {'m': m['members'][1], 'bind_args': [], 'static': False, 'cpp': 'Gen_Degree'}
    dup = {'m': m['members'][2], 'bind_args': [], 'static': False, 'cpp': 'Gen_Duplicate'}
    per_reg = {'NurbsCurveX': [degree], 'CurveX': [dup], 'GeometryBaseX': []}
    monkeypatch.setattr(P, 'CTORS', {'NurbsCurveX': True})
    monkeypatch.setattr(P, 'ARGCTORS', {})
    try:
        R.index_types(m)
        out, deps, writes = P.resolve_markers('$$class:NurbsCurve$$ nc = $$new:NurbsCurve:$$(); return (nc)$$acc:NurbsCurve$$$$oget:NurbsCurve:Degree$$();', C, per_reg, m)
        assert out == 'BND_NurbsCurveX* nc = new BND_NurbsCurveX(); return (nc)->Gen_Degree();' and deps == [degree]
        out, _, _ = P.resolve_markers('return $$as:NurbsCurve$$($$call:Duplicate:$$());', C, per_reg, m)
        assert out == 'return RH3DM_As<BND_NurbsCurveX>(Gen_Duplicate());'
        # a parameter is a const reference; a class without a generated default ctor refuses `new`
        assert P.resolve_markers('$$classarg:NurbsCurve$$', C, per_reg, m)[0] == 'const BND_NurbsCurveX&'
        monkeypatch.setattr(P, 'CTORS', {})
        assert P.resolve_markers('$$new:NurbsCurve:$$()', C, per_reg, m)[0] is None
        # a class the generator did not produce, or a nonconst call through a const parameter
        assert P.resolve_markers('$$class:Nope$$', C, per_reg, m)[0] is None
        nc = dict(degree, m=dict(m['members'][1], Kind='method', Params=[]), nonconst=True, cpp='Gen_Reverse')
        assert P.resolve_markers('(x).$$ocallc:NurbsCurve:Degree:$$()', C, {'NurbsCurveX': [nc]}, m)[0] is None
    finally:
        R.index_types({'members': []})


def test_value_mode_class_values_are_held_by_value(monkeypatch):
    # G74: MeshingParameters is a value-mode class: `.` accessors, construction by value, no null
    MP = R.BY_CS['MeshingParameters']
    m = {'members': [{'Type': MP['type'], 'Kind': 'type', 'Name': 'MeshingParameters', 'Variant': 'portable'},
                     {'Type': MP['type'], 'Kind': 'property', 'Name': 'GridAngle', 'Variant': 'portable', 'Returns': 'double'}]}
    getter = {'m': m['members'][1], 'bind_args': [], 'static': False, 'cpp': 'Gen_GridAngle'}
    setter = {'m': dict(m['members'][1], Kind='method'), 'bind_args': [('double', 'value')], 'static': False, 'cpp': 'Gen_set_GridAngle', 'setter_of': getter}
    monkeypatch.setattr(P, 'CTORS', {'MeshingParametersX': True})
    try:
        R.index_types(m)
        text = 'auto o = $$new:MeshingParameters:$$(); (o)$$acc:MeshingParameters$$$$oset:MeshingParameters:GridAngle$$(1.0); return o;'
        out, deps, writes = P.resolve_markers(text, MP, {'MeshingParametersX': [getter, setter]}, m)
        assert out == 'auto o = BND_MeshingParametersX(); (o).Gen_set_GridAngle(1.0); return o;' and deps == [setter]
        assert P.resolve_markers('$$class:MeshingParameters$$', MP, {'MeshingParametersX': [getter]}, m)[0] == 'BND_MeshingParametersX'
        assert P.resolve_markers('$$class:MeshingParameters$$ x = nullptr;', MP, {'MeshingParametersX': [getter]}, m)[0] is None
        assert P.resolve_markers('$$as:MeshingParameters$$(x)', MP, {'MeshingParametersX': [getter]}, m)[0] is None
    finally:
        R.index_types({'members': []})


def test_string_param_defaulted_to_empty_is_the_param():
    # G76: `F(ptr, text ?? string.Empty)` -- the binding's std::wstring is never null
    m = member('void', params=[('', 'string', 'text')],
               calls=[SELF, {'Kind': 'expr', 'Text': 'text ?? string.Empty'}], ResultUnused=True)
    p, why = P.plan(m, cfn('void', 'const ON_Curve*', 'const RHMONO_STRING*'), CURVE)
    assert why is None and p['call_exprs'][1] == 'RH3DM_S(text).data()'
    # on a non-string parameter it stays opaque
    bad = member('void', params=[('', 'int', 'n')], calls=[SELF, {'Kind': 'expr', 'Text': 'n ?? string.Empty'}], ResultUnused=True)
    assert P.plan(bad, cfn('void', 'const ON_Curve*', 'int'), CURVE)[0] is None


def test_nested_native_takes_an_inlined_zero_pointer():
    # G75: `SetTextString(p, value, ON_Annotation_DimensionStyle(this, IntPtr.Zero))`
    inner = {'Name': 'D', 'Args': [{'Kind': 'expr', 'Text': 'c', 'Value': 'ConstPointer'}, {'Kind': 'expr', 'Text': 'IntPtr.Zero'}]}
    m = member('void', calls=[SELF, {'Kind': 'nativecall', 'Text': 'rc', 'Call': inner}], ResultUnused=True)
    saved = dict(R.C_BY_NAME)
    try:
        R.C_BY_NAME['D'] = {'name': 'D', 'returns': 'const ON_DimStyle*', 'params': [{'type': 'const ON_Curve*'}, {'type': 'const ON_DimStyle*'}]}
        p, why = P.plan(m, cfn('void', 'ON_Curve*', 'const ON_DimStyle*'), CURVE)
        assert why is None and p['call_exprs'][1] == 'D(m_ptr, nullptr)'
        # IntPtr.Zero into a non-pointer parameter is refused
        R.C_BY_NAME['D'] = dict(R.C_BY_NAME['D'], params=[{'type': 'const ON_Curve*'}, {'type': 'int'}])
        assert P.plan(m, cfn('void', 'ON_Curve*', 'const ON_DimStyle*'), CURVE)[0] is None
    finally:
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved)


def test_managed_null_analyses_apply_only_while_no_writer_is_generated(monkeypatch):
    # G75: AnnotationBase.m_parent_dimstyle is written only by ParentDimensionStyle.set;
    # the class re-plans with its members' (managed-null) analyses unless a writer is planned
    import bnd_gen as B
    cls = dict(CURVE)
    calls = []
    def fake_plan_class(c, manifest, c_by_name, skipped):
        calls.append(c.get('null_key'))
        return [{'m': {'Type': c['type'], 'Name': 'TextHeight'}}]
    monkeypatch.setattr(B, 'plan_class', fake_plan_class)
    monkeypatch.setattr(B, 'class_members', lambda c, m: [{'Specialized': {'(managed-null)': {'Body': 'trivial'}}}])
    monkeypatch.setitem(B.TYPE_RECS, cls['type'], {'ManagedFields': {'m_f': [cls['type'] + '.Writer']}})
    per_class = [(cls, [{'m': {'Type': cls['type'], 'Name': 'Other'}}])]
    B.managed_null_pass(per_class, {}, {}, {})
    assert calls == ['(managed-null)'] and per_class[0][0]['null_key'] == '(managed-null)'
    # a planned writer: no re-plan
    cls2, calls[:] = dict(CURVE), []
    per_class = [(cls2, [{'m': {'Type': cls['type'], 'Name': 'Writer'}}])]
    skipped = {}
    B.managed_null_pass(per_class, {}, {}, skipped)
    assert calls == [] and 'null_key' not in cls2 and skipped
    # a writer that appears AFTER the re-plan is unsound: refused loudly
    def bad_plan_class(c, manifest, c_by_name, skipped):
        return [{'m': {'Type': c['type'], 'Name': 'Writer'}}]
    monkeypatch.setattr(B, 'plan_class', bad_plan_class)
    with pytest.raises(SystemExit):
        B.managed_null_pass([(dict(CURVE), [])], {}, {}, {})


def test_scoped_view_resolves_the_scope_field_and_maps_the_element_key(monkeypatch):
    # G77: BrepLoopList(BrepFace): `m_brepface.ConstPointer()` is the face resolved from
    # (brep, m_scope); the indexer's element key is LoopIndex(face, index) while it resolves
    LL, BF = R.BY_REG['BrepLoopListX'], R.BY_REG['BrepFaceX']
    fns = {'ON_Brep_BrepFacePointer': {'name': 'ON_Brep_BrepFacePointer', 'returns': 'const ON_BrepFace*', 'params': [{'type': 'const ON_Brep*'}, {'type': 'int'}]},
           'ON_BrepFace_LoopIndex': {'name': 'ON_BrepFace_LoopIndex', 'returns': 'int', 'params': [{'type': 'const ON_BrepFace*'}, {'type': 'int'}]}}
    for k, v in fns.items():
        monkeypatch.setitem(R.C_BY_NAME, k, v)
    comp = dict(BF, comp={'Const': 'ON_Brep_BrepFacePointer', 'Owner': R.BY_REG['BrepX'], 'Ctor': ['key', 'owner']})
    scls = dict(LL, scope={'key': '(BrepFace)', 'field': 'm_brepface', 'comp': comp}, scoped=True)
    m = dict(member('int', calls=[{'Kind': 'fieldptr', 'Text': 'm_brepface', 'Value': 'ConstPointer'}]), Type=LL['type'])
    p, why = P.plan(m, cfn('int', 'const ON_BrepFace*'), scls)
    assert why is None and p['call_exprs'] == ['(const ON_BrepFace*)ON_Brep_BrepFacePointer(m_ptr, m_scope)']
    # unscoped, the field pointer stays refused
    assert P.plan(m, cfn('int', 'const ON_BrepFace*'), dict(LL))[0] is None
    BL = R.BY_REG['BrepLoopX']
    monkeypatch.setitem(BL, 'comp', {'Ctor': ['key', 'owner'], 'Owner': R.BY_REG['BrepX'], 'Key': 'int', 'Const': 'X', 'OwnerNames': ['Brep']})
    ix = dict(member('BrepLoop', params=[('', 'int', 'index')]), Type=LL['type'], Kind='indexer', Body='construct', Child='BrepLoop',
              ChildArgs=['__keymap__', 'm_brep'], ChildArgKinds=['keymap', 'field'],
              KeyMap={'Native': 'ON_BrepFace_LoopIndex', 'Field': 'm_brepface', 'Param': 'index'})
    monkeypatch.setattr(P, 'construct_owner', lambda *a: 'm_ptr')
    shape = P.construct_shape(ix, scls, 'm_ptr', {'index'}, None, {})
    assert shape[0] == 'construct' and 'ON_BrepFace_LoopIndex(rh3dm_sp, index) : index' in shape[3]
    # the key map is a scoped view's alone
    assert isinstance(P.construct_shape(ix, dict(LL), 'm_ptr', {'index'}, None, {}), str)


def test_scoped_member_dispatches_on_m_scope():
    # G77: the emitted member runs the scope's variant when m_scope >= 0
    import emit_classes as E
    LL = R.BY_REG['BrepLoopListX']
    base = {'m': {'Name': 'Count', 'Kind': 'property'}, 'fn': {'name': 'A', 'params': []}, 'cls': LL, 'static': False,
            'call_exprs': [], 'locals': [], 'bind_args': [], 'shape': ('direct', 'int'), 'guards': [], 'cpp': 'Gen_Count'}
    sp = dict(base, fn={'name': 'B', 'params': []})
    text = E.emit(dict(base, scope_plan=sp))
    assert text.split('\n')[1:4] == ['    if (m_scope >= 0) {', '      return B();', '    }'] and 'return A();' in text
    refused = E.emit(dict(base, scope_plan=False, cls=dict(LL, scope={'comp': {'cs': 'BrepFace'}})))
    assert 'if (m_scope >= 0) throw std::runtime_error' in refused


def test_hand_enum_reused_only_with_rhinocommons_numbering():
    # G78: ON_Dithering::Methods numbers SimpleNoise 0, RhinoCommon 2 -- the old bindings'
    # enum is not reused for Dithering.Methods; the generated one carries RhinoCommon's values
    assert R.cpp_enum_values('ON_Dithering::Methods') == {'SimpleNoise': 0, 'FloydSteinberg': 1}
    m = {'members': [
        {'Type': 'Rhino.Render.Dithering', 'Kind': 'type', 'Name': 'Dithering', 'Variant': 'portable'},
        {'Type': 'Rhino.Render.Dithering', 'Kind': 'enum', 'Name': 'Methods', 'Variant': 'portable',
         'Values': ['None', 'FloydSteinberg', 'SimpleNoise'], 'EnumInts': [0, 1, 2]}]}
    saved = dict(R.CS_ENUMS)
    try:
        R.CS_ENUMS.pop('Dithering.Methods', None)
        R.extend_enum_table(m)
        e = R.CS_ENUMS['Dithering.Methods']
        assert e['gen'] and e['cpp'] == 'RH3DM_Dithering_Methods' and e['js'] == 'DitheringXMethods'
        assert R.RENUMBERED_ENUMS['DitheringMethods'][1]['SimpleNoise'] == (2, 0)
    finally:
        R.CS_ENUMS.clear(); R.CS_ENUMS.update(saved)


def test_variant_enum_map_reads_and_writes_as_rhinocommon_compares():
    # G78: Dithering.Method: `(v.ToInt() == 0) ? Methods.SimpleNoise : Methods.FloydSteinberg`,
    # set `new Variant((Methods.SimpleNoise == value) ? 0 : 1)`
    D = R.BY_REG['DitheringX']
    saved = (dict(R.CS_ENUMS), dict(R.ENUM_NAMED), dict(R.WRAPPER), D.get('ff'))
    try:
        R.CS_ENUMS['Dithering.Methods'] = {'cpp': 'RH3DM_Dithering_Methods', 'gen': True, 'owner': 'Dithering'}
        R.ENUM_NAMED['Dithering.Methods'] = {'None': '0', 'FloydSteinberg': '1', 'SimpleNoise': '2'}
        R.WRAPPER.update({'New': 'N', 'Delete': 'Del', 'Get': {'ToInt': {'Form': 'return', 'Native': 'GI', 'Returns': 'int'}},
                          'Set': {'int': 'SI'}})
        D['ff'] = {'GetValueNative': 'G', 'SetValueNative': 'S', 'EqualNative': 'E'}
        for n, ps in (('G', ['const ON_Dithering*', 'DitheringSetting', 'ON_XMLVariant*']),):
            R.C_BY_NAME[n] = {'name': n, 'returns': 'void', 'params': [{'type': t} for t in ps]}
        x = {'Type': D['type'], 'Kind': 'property', 'Name': 'Method', 'Returns': 'Methods',
             'VariantGet': {'Which': 'S.Method', 'WhichValue': '1', 'Conv': 'ToInt', 'MapEq': '0', 'MapA': 'SimpleNoise', 'MapB': 'FloydSteinberg'},
             'VariantSet': {'Which': 'S.Method', 'WhichValue': '1', 'MapA': 'SimpleNoise', 'MapK1': '0', 'MapK2': '1'}}
        g, why = P.plan_variant(x, D)
        assert why is None and g['shape'][1]['map'] == {'cpp': 'RH3DM_Dithering_Methods', 'a': '2', 'b': '1', 'eq': '0', 'k1': None, 'k2': None}
        s, why = P.plan_variant(x, D, setter=True)
        assert why is None and s['bind_args'] == [('RH3DM_Dithering_Methods', 'value')] and s['shape'][1]['map']['k1'] == '0'
        # a member name the enum does not have is refused
        bad = dict(x, VariantGet=dict(x['VariantGet'], MapA='Nope'))
        assert P.plan_variant(bad, D)[0] is None
    finally:
        R.CS_ENUMS.clear(); R.CS_ENUMS.update(saved[0]); R.ENUM_NAMED.clear(); R.ENUM_NAMED.update(saved[1])
        R.WRAPPER.clear(); R.WRAPPER.update(saved[2]); D['ff'] = saved[3]
        R.C_BY_NAME.pop('G', None)


def test_sequence_parameter_where_csharp_passed_getconstarray():
    # G82: `Point3d[] a = GetConstArray(points, out count); F(ptr, count, a, null)`,
    # read by specgen as `F(ptr, points.Count, points, null)`: the vector's data and size
    seq = ('', 'IEnumerable<Point3d>', 'points')
    calls = [SELF, {'Kind': 'parammember', 'Text': 'points.Count'}, {'Kind': 'param', 'Text': 'points'},
             {'Kind': 'literal', 'Text': 'null'}]
    p, why = P.plan(member('void', params=[seq], calls=calls),
                    cfn('void', 'ON_Curve*', 'int', 'const ON_3dPoint*', 'const ON_3dVector*'), CURVE)
    assert why is None
    assert p['bind_args'] == [('const std::vector<BND_Point3dX>&', 'points')]
    assert p['call_exprs'][1:] == ['(int)points.size()', 'RH3DM_Ptr(points.data())', 'nullptr']
    # a C element type that is not the sequence's is refused, never reinterpreted
    _, why = P.plan(member('void', params=[seq], calls=calls),
                    cfn('void', 'ON_Curve*', 'int', 'const ON_Interval*', 'const ON_3dVector*'), CURVE)
    assert why.startswith('sequence points')
    # as is a C side that could write through the pointer
    _, why = P.plan(member('void', params=[seq], calls=calls),
                    cfn('void', 'ON_Curve*', 'int', 'ON_3dPoint*', 'const ON_3dVector*'), CURVE)
    assert why.startswith('sequence points')
    assert R.seq_elem('System.Collections.Generic.IEnumerable<Point3d>') == 'Point3d'
    assert R.seq_elem('IEnumerable<Color>') is None


def test_guid_empty_early_return_and_js_sequence_shape():
    # G82: `if (points.Count < 1) return Guid.Empty;` (File3dmObjectTable.AddPolyline)
    g = {'Return': 'Guid.Empty'}
    assert P.early_return_cpp(g, ('conv', 'BND_UUID', 'ON_UUID_to_Binding({c})'), 'Guid') == 'ON_UUID_to_Binding(ON_nil_uuid)'
    assert P.early_return_cpp(g, ('direct', 'int'), 'int') is None
    import emit_classes as E
    key, check, conv = E.js_class_shape('const std::vector<BND_Point3dX>&')
    assert key == 'seq:BND_Point3dX' and check.format(v='v1') == 'RH3DM_JsIsSeq(v1)'
    assert conv.format(v='v1') == 'RH3DM_JsSeq<BND_Point3dX>(v1)'


def test_members_calling_an_unsafe_native_are_refused():
    # G82: a member calling a native known to overrun the heap is refused. The first entry,
    # ON_PointCloud_InsertPoints (pilot-findings #34), was fixed upstream (U11): a stand-in here
    seq = ('', 'IEnumerable<Point3d>', 'points')
    calls = [SELF, {'Kind': 'param', 'Text': 'index'}, {'Kind': 'parammember', 'Text': 'points.Count'},
             {'Kind': 'param', 'Text': 'points'}]
    x = member('void', params=[('', 'int', 'index'), seq], calls=calls)
    x['Calls'][0]['Name'] = 'RH3DM_TestUnsafe_InsertPoints'
    R.UNSAFE_NATIVES['RH3DM_TestUnsafe_InsertPoints'] = 'a test finding'
    try:
        _, why = P.plan(x, cfn('void', 'ON_Curve*', 'int', 'int', 'const ON_3dPoint*'), CURVE)
    finally:
        del R.UNSAFE_NATIVES['RH3DM_TestUnsafe_InsertPoints']
    assert why.startswith('unsafe native RH3DM_TestUnsafe_InsertPoints')


def test_constructor_choosing_its_creation_call_is_one_ternary():
    # G82: PolylineCurve(points): `if (points.Count < 1) ptr = New(IntPtr.Zero); else ptr = New2(..)`
    PC = R.BY_CS['PolylineCurve']
    saved = dict(R.C_BY_NAME)
    try:
        R.C_BY_NAME['N1'] = {'name': 'N1', 'returns': 'ON_PolylineCurve*', 'params': [{'type': 'ON_PolylineCurve*', 'name': 'o'}]}
        R.C_BY_NAME['N2'] = {'name': 'N2', 'returns': 'ON_PolylineCurve*', 'params': [{'type': 'int', 'name': 'n'}, {'type': 'const ON_3dPoint*', 'name': 'p'}]}
        x = {'Type': PC['type'], 'Kind': 'constructor', 'Name': 'PolylineCurve', 'Variant': 'portable', 'Body': 'trivial',
             'Signature': 'PolylineCurve(System.Collections.Generic.IEnumerable<Point3d> points)',
             'Params': [{'Name': 'points', 'Type': 'System.Collections.Generic.IEnumerable<Point3d>', 'Mode': ''}],
             'CtorCond': {'Cond': 'points.Count < 1', 'Ids': ['points.Count']},
             'Calls': [{'Name': 'N1', 'Args': [{'Kind': 'expr', 'Text': 'IntPtr.Zero'}]},
                       {'Name': 'N2', 'Args': [{'Kind': 'parammember', 'Text': 'points.Count'}, {'Kind': 'param', 'Text': 'points'}]}]}
        p, why = P.cond_ctor_plan(x, PC, R.C_BY_NAME)
        assert why is None and p['bind_args'] == [('const std::vector<BND_Point3dX>&', 'points')]
        assert p['ctor_call'] == '(((int)points.size()) < 1) ? N1(nullptr) : N2((int)points.size(), RH3DM_Ptr(points.data()))'
        assert [f['name'] for f in p['extra_fns']][-1] == 'N2'
        # a condition over anything but the parameters is refused
        bad = dict(x, CtorCond={'Cond': 'Count < 1', 'Ids': ['Count']})
        assert P.cond_ctor_plan(bad, PC, R.C_BY_NAME)[1] == 'cond ctor id Count'
    finally:
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved)


def test_data_member_markers_resolve_or_refuse_with_dependents():
    # G82: Polyline.ToPolylineCurve: `$$new:PolylineCurve:IEnumerable<Point3d>$$((*this).ToArray())`
    import bnd_gen as B
    PC = R.BY_CS['PolylineCurve']
    saved = dict(P.ARGCTORS)
    def vt():
        return {'types': [{'Cs': 'Polyline', 'Cpp': 'BND_PolylineX', 'IsData': True, 'Fields': []}],
                'members': [{'Type': 'Polyline', 'Name': 'ToPolylineCurve', 'Kind': 'method', 'Ok': True,
                             'Keys': ['Polyline.ToPolylineCurve/0'], 'Ret': '$$class:PolylineCurve$$', 'Params': [],
                             'Decl': '$$class:PolylineCurve$$ ToPolylineCurve() const;',
                             'Def': 'return $$new:PolylineCurve:IEnumerable<Point3d>$$((*this).ToArray());'},
                            {'Type': 'Polyline', 'Name': 'ToNurbs', 'Kind': 'method', 'Ok': True, 'Keys': ['Polyline.ToNurbs/0'],
                             'Refs': ['Polyline.ToPolylineCurve/0'], 'Ret': 'int', 'Params': [], 'Decl': 'int ToNurbs();', 'Def': ''}]}
    try:
        ctor = {'m': {'Params': [{'Type': 'System.Collections.Generic.IEnumerable<Point3d>'}]},
                'bind_args': [('const std::vector<BND_Point3dX>&', 'points')]}
        P.ARGCTORS[PC['reg']] = [ctor]
        v = vt()
        B.data_marker_pass(v, [(PC, [])], {'members': []}, {})
        m = v['members'][0]
        assert m['Ok'] and m['Ret'] == 'BND_PolylineCurveX*' and m['ClassDeps'] == ['PolylineCurveX']
        assert m['Def'] == 'return new BND_PolylineCurveX((*this).ToArray());'
        # no such constructor generated: refused, and so is what depends on it
        P.ARGCTORS[PC['reg']] = []
        v = vt()
        B.data_marker_pass(v, [(PC, [])], {'members': []}, {})
        assert [x['Ok'] for x in v['members']] == [False, False]
    finally:
        P.ARGCTORS.clear(); P.ARGCTORS.update(saved)


def test_class_body_markers_self_fieldptr_deref():
    # G84/G85: the class's own pointer, a child view's parent pointer, a class local by reference
    per_reg = {c['reg']: [] for c in R.CLASSES}
    out, deps, writes = P.resolve_markers('F($$self:Const$$)', CURVE, per_reg, {'members': []})
    assert out.startswith('F(((void*)(') and not writes
    out, deps, writes = P.resolve_markers('F($$self:NonConst$$)', CURVE, per_reg, {'members': []})
    assert writes                                         # a non-const pointer: a non-const member
    # a field's pointer is the view's own only on a child view
    assert P.resolve_markers('$$fieldptr:m_mesh:NonConst$$', CURVE, per_reg, {'members': []})[0] is None
    child = next(c for c in R.CLASSES if c['mode'] == 'child')
    assert P.resolve_markers('$$fieldptr:m_x:Const$$', child, per_reg, {'members': []})[0] is not None
    assert P.resolve_markers('G($$deref:Curve$$(c))', CURVE, per_reg, {'members': []})[0] == 'G(*(c))'


def test_derived_class_arguments_fit_and_native_usability():
    # G85: `Create(PolylineCurve pc, ..)` into `Create(Curve, ..)`; G84: by-value C types
    assert P._args_fit(['Curve', 'double'], ['PolylineCurve', 'double'])
    assert not P._args_fit(['PolylineCurve'], ['Curve'])          # never the other way
    saved = dict(R.C_BY_NAME)
    try:
        R.C_BY_NAME['ok'] = {'name': 'ok', 'returns': 'bool', 'params': [{'type': 'const ON_Curve*'}, {'type': 'double'}]}
        # G96: a C-layer enum (defined in on_material.cpp) is usable -- RH3DM_R converts the result,
        # RH3DM_EnumArg the argument; any other by-value C type is still refused
        R.C_BY_NAME['enum'] = {'name': 'enum', 'returns': 'TextureProjectionModes', 'params': [{'type': 'const ON_Texture*'}]}
        R.C_BY_NAME['cenum'] = {'name': 'cenum', 'returns': 'bool', 'params': [{'type': 'const ON_TextRun*'}, {'type': 'enum TextRunBool'}]}
        R.C_BY_NAME['byval'] = {'name': 'byval', 'returns': 'ON_SomethingByValue', 'params': [{'type': 'const ON_Texture*'}]}
        assert 'TextureProjectionModes' in P.c_layer_enums() and 'TextRunTypeConsts' in P.c_layer_enums()
        assert P.native_usable('ok') and P.native_usable('enum') and P.native_usable('cenum')
        assert not P.native_usable('byval') and not P.native_usable('missing')
        R.C_BY_NAME['unsafe'] = dict(R.C_BY_NAME['ok'], name='unsafe')
        R.UNSAFE_NATIVES['unsafe'] = 'a test finding'
        assert not P.native_usable('unsafe')               # usable but for the finding
    finally:
        R.C_BY_NAME.clear(); R.C_BY_NAME.update(saved)
        R.UNSAFE_NATIVES.pop('unsafe', None)


def test_enum_map_with_a_default_return():
    # G83: Curve.ClosedCurveOrientation: `switch (r) { case 1: .. case -1: .. }`, else Undefined
    saved = dict(R.CS_ENUMS)
    try:
        R.CS_ENUMS['CurveOrientation'] = {'cpp': 'CurveOrientation'}
        x = member('CurveOrientation', calls=[SELF],
                   EnumMap={'Values': [[1, 1], [-1, -1]], 'Default': 'CurveOrientation.Undefined', 'DefaultValue': 0})
        p, why = P.plan(x, cfn('int', 'const ON_Curve*'), CURVE)
        assert why is None and p['shape'][0] == 'enummap' and p['shape'][3] is None and p['shape'][4] == 0
    finally:
        R.CS_ENUMS.clear(); R.CS_ENUMS.update(saved)


def test_internal_handle_adopt_linq_sources_and_native_enums():
    # G96: AnnotationBase.PlainText / TextHasRtfFormatting -- LINQ over GetTextRuns()'s TextRun
    # objects (an INTERNAL handle class, adopted with `new TextRun(p)`)
    TR = R.BY_CS['TextRun']
    assert TR.get('internal') and TR['mode'] == 'pointer'
    import emit_classes as E
    assert E.emit_registrations(TR, [], set()) == ('', '')           # C++ only, never bound
    per_reg = {c['reg']: [] for c in R.CLASSES}
    saved = TR.get('handle')
    try:
        TR['handle'] = {'Delete': 'ON_TextRun_ReturnManagedTextRun', 'Field': 'm_pManagedTextRun'}
        out = P.resolve_markers('$$adopt:TextRun$$(RH3DM_IP(p))', CURVE, per_reg, {'members': []})[0]
        assert out == 'RH3DM_Adopt<BND_TextRunX, ON_TextRun>(RH3DM_IP(p))'
        TR.pop('handle')
        # not a handle: the constructor's ownership is unknown -- refused
        assert P.resolve_markers('$$adopt:TextRun$$(RH3DM_IP(p))', CURVE, per_reg, {'members': []})[0] is None
        assert P.resolve_markers('$$adopt:Curve$$(RH3DM_IP(p))', CURVE, per_reg, {'members': []})[0] is None
    finally:
        if saved is not None:
            TR['handle'] = saved
    # a class sequence's element (a tagged call returning one is a LINQ source)
    assert P.cs_array_elem('TextRun[]') == 'TextRun'
    assert P.cs_array_elem('IEnumerable<Rhino.Geometry.Curve>') == 'Curve'
    assert P.cs_array_elem('List<File3dmObject>') == 'File3dmObject'
    assert P.cs_array_elem('int') is None and P.cs_array_elem(None) is None


def test_enum_values_from_resolved_ints():
    # G96: `Text = UnsafeNativeMethods.TextRunTypeConsts.Text` -- not a literal; the extractor's
    # resolved EnumInts value the members (in declaration order), literal enums are unchanged
    m = {'members': [
        {'Kind': 'enum', 'Type': 'Rhino.Geometry', 'Name': 'TextRunTypeX', 'Variant': 'portable',
         'Values': ['None = UnsafeNativeMethods.C.None', 'Text = UnsafeNativeMethods.C.Text', 'Paragraph = UnsafeNativeMethods.C.Paragraph'],
         'EnumInts': [0, 1, 4]},
        {'Kind': 'enum', 'Type': 'Rhino.Geometry', 'Name': 'LiteralX', 'Variant': 'portable',
         'Values': ['A = 2', 'B'], 'EnumInts': [7, 7]},
        {'Kind': 'enum', 'Type': 'Rhino.Geometry', 'Name': 'BadX', 'Variant': 'portable',
         'Values': ['A = Foo.Bar', 'B'], 'EnumInts': [1]},              # count mismatch: unvalued
    ]}
    try:
        R.index_types(m)
        assert R.ENUM_NAMED['TextRunTypeX'] == {'None': 0, 'Text': 1, 'Paragraph': 4}
        assert R.ENUM_NAMED['LiteralX'] == {'A': 2, 'B': 3}
        assert 'BadX' not in R.ENUM_NAMED
    finally:
        R.index_types({'members': []})


def test_free_floating_cpp_pointer_and_system_enums():
    # G97: DocumentOrFreeFloatingBase.CppPointer in a translated body is the class's own accessor;
    # .NET's DateTimeKind joins the manifest's enums (as specgen declares it)
    per_reg = {c['reg']: [] for c in R.CLASSES}
    SUN = R.BY_CS['Sun']
    saved = SUN.get('ff')
    try:
        SUN['ff'] = saved or {'New': 'ON_Sun_New'}
        assert P.resolve_markers('F($$get:CppPointer$$())', SUN, per_reg, {'members': []})[0] == 'F((void*)RH3DM_Const())'
    finally:
        if saved is None:
            SUN.pop('ff')
    assert P.resolve_markers('F($$get:CppPointer$$())', CURVE, per_reg, {'members': []})[0] is None   # not free-floating
    m = R.with_system_enums({'members': []})
    assert [x['Name'] for x in m['members']] == ['DateTimeKind']
    assert len(R.with_system_enums(m)['members']) == 1                     # once
    try:
        R.index_types(m)
        assert R.ENUM_NAMED['DateTimeKind'] == {'Unspecified': 0, 'Utc': 1, 'Local': 2}
    finally:
        R.index_types({'members': []})


def test_js_dispatcher_shadowing_is_refused():
    # Luis 2026-10-16: PointCloud.add(point, color) checked the color as `true`, so add(point,
    # normal) was unreachable in JS -- the generator now refuses any shadowed branch
    import emit_classes as E
    assert E.js_shadowed([['object:Z', 'any:BND_Color'], ['object:Z', 'object:Z']]) == [(1, 0)]
    assert E.js_shadowed([['object:Z', 'color'], ['object:Z', 'object:Z']]) == []
    assert E.js_shadowed([['number'], ['string']]) == []
    assert E.js_shadowed([['class:CurveX'], ['class:NurbsCurveX']]) == [(1, 0)]     # a base check takes derived
    assert E.js_shadowed([['class:NurbsCurveX'], ['class:CurveX']]) == []
    color = E.js_class_shape('BND_Color')
    assert color[0] == 'color' and 'hasOwnProperty("r")' in color[1]
    assert E.js_class_shape('std::wstring')[0] == E.js_class_shape('BND_UUID')[0] == 'string'


def test_js_value_objects_are_checked_on_every_field():
    # G100: ArcX {Plane, Radius, AngleDomain} and CircleX {Plane, Radius} share their first field;
    # keyed on it, ArcCurveX(circle) reached the Arc conversion ("Missing field: AngleDomain")
    import emit_classes as E
    assert E.js_shadowed([['object:Plane,Radius'], ['object:AngleDomain,Plane,Radius']]) == [(1, 0)]
    assert E.js_shadowed([['object:AngleDomain,Plane,Radius'], ['object:Plane,Radius']]) == []


def test_out_companion_returns_an_unconverted_csharp_result():
    # G109: `uint SharpEdgeCount() { var r = default(SubDEdgeSharpness); return SharpEdgeCount(out r); }`
    # calls a tuple sibling led by its C# result, not a bool status; the companion returns that
    # result. A converted result (an enum cast, a wrapped object) is still not a plain return.
    assert P.companion_result(('result', 'unsigned int', '{c}')) == 'unsigned int'
    assert P.companion_result(('result', 'RH3DM_Foo', '(RH3DM_Foo){c}')) is None
    assert P.companion_result(True) is None and P.companion_result(' != 0') is None


def test_guid_guard_operands_compare_values():
    # G114: Python's BND_UUID is a py::object; `DimensionStyleId != value` compared object
    # identity, so the guarded setter always ran SetDimstyleId and dropped the overrides
    cond = P.guid_operands('Gen_DimensionStyleId() != value', {'value': 'Guid'}, {'Gen_DimensionStyleId'})
    assert cond == 'RH3DM_ToUuid(Gen_DimensionStyleId()) != RH3DM_ToUuid(value)'
    assert P.guid_operands('count != value', {'value': 'int'}, set()) == 'count != value'
    assert P.guid_operands('x.Gen_Id() == id', {'id': 'System.Guid'}, {'Gen_Id'}) == 'x.Gen_Id() == RH3DM_ToUuid(id)'
