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
