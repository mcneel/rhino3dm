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
        assert P.plan(dict(x, ChildCheck=None), cfn('bool', 'const ON_3dmObjectAttributes*'), MM)[0] is None
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
