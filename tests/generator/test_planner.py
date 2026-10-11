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
