"""Pilot binding generator v3: registry-driven, multi-class.

v2 proved the pipeline on Curve but was Curve-shaped throughout. v3 moves
everything class-specific into CLASSES, the wrapper-type registry:

    C# type <-> ON_* type <-> hand BND class (copy source) <-> generated class

One generated file defines every class (in registry order, so cross-class
returns like Surface.IsoCurve -> CurveX see a complete type) and registers
them all through a single initGeneratedBindings entry point.

Everything else is unchanged from v2: C# signatures shape the API, Calls[]
maps parameters onto the C call, guards/success patterns reproduce the C#
contract, and anything unrecognized is skipped with a counted reason.
"""

import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# ---------------------------------------------------------------- registry
CLASSES = [
    # order matters: a class whose methods return another generated class must
    # come AFTER it (inline bodies need the complete type), and abstract bases
    # come before every class deriving them.
    # mode 'pointer': heap-owned ON_X* self; 'value': ON_X held by value
    # (RhinoCommon structs like Transform, whose members pass `ref this`);
    # 'abstract': a generated BASE class -- no storage, no constructor, its
    # members reach self through a pure-virtual getter each derived class
    # overrides with its typed pointer. 'base' on a derived entry names the
    # abstract class it extends, mirroring the RHINOCOMMON hierarchy (the
    # spec), not opennurbs's (ON_Viewport is ON_Geometry there, but
    # ViewportInfo is not a GeometryBase in .NET).
    # 'copy' is the expression constructing this class's storage from the
    # hand-written BND source object.
    {'type': 'Rhino.Geometry.GeometryBase', 'cs': 'GeometryBase',
     'on': 'ON_Geometry',
     'mode': 'abstract', 'getter': 'RH3DM_GeomPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_GeometryBaseX', 'reg': 'GeometryBaseX'},
    {'type': 'Rhino.DocObjects.ModelComponent', 'cs': 'ModelComponent',
     'on': 'ON_ModelComponent',
     'mode': 'abstract', 'getter': 'RH3DM_McPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_ModelComponentX', 'reg': 'ModelComponentX'},
    {'type': 'Rhino.Geometry.Curve', 'cs': 'Curve', 'on': 'ON_Curve',
     'mode': 'pointer', 'hand': 'BND_Curve', 'hand_header': 'bnd_curve.h',
     'copy': 'source.m_curve ? source.m_curve->DuplicateCurve() : nullptr',
     'gen': 'BND_CurveX', 'reg': 'CurveX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.Surface', 'cs': 'Surface', 'on': 'ON_Surface',
     'mode': 'pointer', 'hand': 'BND_Surface', 'hand_header': 'bnd_surface.h',
     'copy': 'source.m_surface ? source.m_surface->DuplicateSurface() : nullptr',
     'gen': 'BND_SurfaceX', 'reg': 'SurfaceX', 'base': 'GeometryBaseX'},
    # m_brep is private on BND_Brep; copy via the public GeometryPointer()
    {'type': 'Rhino.Geometry.Brep', 'cs': 'Brep', 'on': 'ON_Brep',
     'mode': 'pointer', 'hand': 'BND_Brep', 'hand_header': 'bnd_brep.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Brep(*ON_Brep::Cast(source.GeometryPointer())) : nullptr'),
     'gen': 'BND_BrepX', 'reg': 'BrepX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.Transform', 'cs': 'Transform', 'on': 'ON_Xform',
     'mode': 'value', 'hand': 'BND_Transform', 'hand_header': 'bnd_xform.h',
     'copy': 'source.m_xform',
     'gen': 'BND_TransformX', 'reg': 'TransformX'},
    {'type': 'Rhino.Geometry.Mesh', 'cs': 'Mesh', 'on': 'ON_Mesh',
     'mode': 'pointer', 'hand': 'BND_Mesh', 'hand_header': 'bnd_mesh.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Mesh(*ON_Mesh::Cast(source.GeometryPointer())) : nullptr'),
     'gen': 'BND_MeshX', 'reg': 'MeshX', 'base': 'GeometryBaseX'},
    # the manifest's richest class (46 candidates): camera/frustum math is
    # all doubles, bools, points and vectors -- exactly the marshalled set
    {'type': 'Rhino.DocObjects.ViewportInfo', 'cs': 'ViewportInfo',
     'on': 'ON_Viewport',
     'mode': 'pointer', 'hand': 'BND_Viewport', 'hand_header': 'bnd_viewport.h',
     'copy': 'source.m_viewport ? new ON_Viewport(*source.m_viewport) : nullptr',
     'gen': 'BND_ViewportX', 'reg': 'ViewportX', 'bridge': False},
    # hand binding file is bnd_beam.h / C layer on_beam.cpp (opennurbs's
    # historical name for ON_Extrusion)
    {'type': 'Rhino.Geometry.Extrusion', 'cs': 'Extrusion', 'on': 'ON_Extrusion',
     'mode': 'pointer', 'hand': 'BND_Extrusion', 'hand_header': 'bnd_beam.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Extrusion(*ON_Extrusion::Cast(source.GeometryPointer()))'
              ' : nullptr'),
     'gen': 'BND_ExtrusionX', 'reg': 'ExtrusionX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.PointCloud', 'cs': 'PointCloud', 'on': 'ON_PointCloud',
     'mode': 'pointer', 'hand': 'BND_PointCloud', 'hand_header': 'bnd_pointcloud.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_PointCloud(*ON_PointCloud::Cast(source.GeometryPointer()))'
              ' : nullptr'),
     'gen': 'BND_PointCloudX', 'reg': 'PointCloudX', 'base': 'GeometryBaseX',
     # bulk tier: Python buffer protocol over the contiguous ON_3dPoint storage
     # (3 doubles each) -> numpy.asarray(cloud) is an (N, 3) zero-copy view
     'buffer': 'm_ptr->m_P'},
    {'type': 'Rhino.DocObjects.Font', 'cs': 'Font', 'on': 'ON_Font',
     'mode': 'pointer', 'hand': 'BND_Font', 'hand_header': 'bnd_font.h',
     'copy': ('source.m_managed_font'
              ' ? new ON_Font(*source.m_managed_font) : nullptr'),
     'gen': 'BND_FontX', 'reg': 'FontX'},
    # hand class holds ON_BezierCurve BY VALUE (private; public ONBezier()
    # accessor added for this) -- second value-mode class
    {'type': 'Rhino.Geometry.BezierCurve', 'cs': 'BezierCurve',
     'on': 'ON_BezierCurve',
     'mode': 'value', 'hand': 'BND_BezierCurve', 'hand_header': 'bnd_bezier.h',
     'copy': 'source.ONBezier()',
     'gen': 'BND_BezierCurveX', 'reg': 'BezierCurveX'},
    # m_polycurve is private; the BND_Curve base's m_curve is public and
    # aliases the same object (the Brep approach)
    {'type': 'Rhino.Geometry.PolyCurve', 'cs': 'PolyCurve', 'on': 'ON_PolyCurve',
     'mode': 'pointer', 'hand': 'BND_PolyCurve', 'hand_header': 'bnd_polycurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_PolyCurve(*ON_PolyCurve::Cast(source.m_curve))'
              ' : nullptr'),
     'gen': 'BND_PolyCurveX', 'reg': 'PolyCurveX', 'base': 'CurveX'},
    # concrete subtypes: the hand classes keep their specific pointer
    # private, so copy through the public base pointer (Brep/PolyCurve idiom)
    {'type': 'Rhino.Geometry.LineCurve', 'cs': 'LineCurve', 'on': 'ON_LineCurve',
     'mode': 'pointer', 'hand': 'BND_LineCurve', 'hand_header': 'bnd_linecurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_LineCurve(*ON_LineCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_LineCurveX', 'reg': 'LineCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.ArcCurve', 'cs': 'ArcCurve', 'on': 'ON_ArcCurve',
     'mode': 'pointer', 'hand': 'BND_ArcCurve', 'hand_header': 'bnd_arccurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_ArcCurve(*ON_ArcCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_ArcCurveX', 'reg': 'ArcCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.NurbsCurve', 'cs': 'NurbsCurve', 'on': 'ON_NurbsCurve',
     'mode': 'pointer', 'hand': 'BND_NurbsCurve', 'hand_header': 'bnd_nurbscurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_NurbsCurve(*ON_NurbsCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_NurbsCurveX', 'reg': 'NurbsCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.PolylineCurve', 'cs': 'PolylineCurve', 'on': 'ON_PolylineCurve',
     'mode': 'pointer', 'hand': 'BND_PolylineCurve', 'hand_header': 'bnd_polylinecurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_PolylineCurve(*ON_PolylineCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_PolylineCurveX', 'reg': 'PolylineCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.NurbsSurface', 'cs': 'NurbsSurface', 'on': 'ON_NurbsSurface',
     'mode': 'pointer', 'hand': 'BND_NurbsSurface', 'hand_header': 'bnd_nurbssurface.h',
     'copy': ('source.m_surface'
              ' ? new ON_NurbsSurface(*ON_NurbsSurface::Cast(source.m_surface)) : nullptr'),
     'gen': 'BND_NurbsSurfaceX', 'reg': 'NurbsSurfaceX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.PlaneSurface', 'cs': 'PlaneSurface', 'on': 'ON_PlaneSurface',
     'mode': 'pointer', 'hand': 'BND_PlaneSurface', 'hand_header': 'bnd_planesurface.h',
     'copy': ('source.m_surface'
              ' ? new ON_PlaneSurface(*ON_PlaneSurface::Cast(source.m_surface)) : nullptr'),
     'gen': 'BND_PlaneSurfaceX', 'reg': 'PlaneSurfaceX', 'base': 'SurfaceX'},
    {'type': 'Rhino.DocObjects.Linetype', 'cs': 'Linetype', 'on': 'ON_Linetype',
     'mode': 'pointer', 'hand': 'BND_Linetype', 'hand_header': 'bnd_linetype.h',
     'copy': ('source.m_linetype'
              ' ? new ON_Linetype(*source.m_linetype) : nullptr'),
     'gen': 'BND_LinetypeX', 'reg': 'LinetypeX', 'bridge': False, 'base': 'ModelComponentX'},
    {'type': 'Rhino.DocObjects.DimensionStyle', 'cs': 'DimensionStyle',
     'on': 'ON_DimStyle',
     'mode': 'pointer', 'hand': 'BND_DimensionStyle',
     'hand_header': 'bnd_dimensionstyle.h',
     'copy': ('source.m_dimstyle'
              ' ? new ON_DimStyle(*source.m_dimstyle) : nullptr'),
     'gen': 'BND_DimensionStyleX', 'reg': 'DimensionStyleX', 'bridge': False, 'base': 'ModelComponentX'},
]
BY_CS = {c['cs']: c for c in CLASSES}
BY_TYPE = {c['type']: c for c in CLASSES}
BY_REG = {c['reg']: c for c in CLASSES}

CVALUE = {'double', 'int', 'bool', 'unsigned int'}
OUTLOCAL = {
    'double*':          ('double {n} = 0;', '{n}'),
    'int*':             ('int {n} = 0;', '{n}'),
    'bool*':            ('bool {n} = false;', '{n}'),
    # value types reach the binding as the GENERATED value types (translated
    # RhinoCommon bodies), converted from the C layer's opennurbs locals
    'ON_3dPoint*':      ('ON_3dPoint {n};', 'RH3DM_P({n})'),
    'ON_3dVector*':     ('ON_3dVector {n};', 'RH3DM_V({n})'),
    'ON_Interval*':     ('ON_Interval {n};', 'RH3DM_I({n})'),
    'ON_PLANE_STRUCT*': ('ON_PLANE_STRUCT {n};',
                         'BND_Plane::FromOnPlane(FromPlaneStruct({n}))'),
    'ON_Xform*':        ('ON_Xform {n};', 'BND_Transform({n})'),
}
CS_ARG = {
    'double':   ('double', '{n}'),
    'int':      ('int', '{n}'),
    'bool':     ('bool', '{n}'),
    'Point3d':  ('BND_Point3dX', 'ON_3DPOINT_STRUCT{{{{{n}.m_x, {n}.m_y, {n}.m_z}}}}'),
    'Vector3d': ('BND_Vector3dX', 'ON_3DVECTOR_STRUCT{{{{{n}.m_x, {n}.m_y, {n}.m_z}}}}'),
    'Interval': ('BND_IntervalX', 'ON_INTERVAL_STRUCT{{{{{n}.m_t0, {n}.m_t1}}}}'),
    'string':   ('std::wstring', 'RH3DM_S({n}).data()'),
}
CS_ARG_CTYPES = {
    'double': {'double'}, 'int': {'int'}, 'bool': {'bool'},
    'Point3d': {'ON_3DPOINT_STRUCT'}, 'Vector3d': {'ON_3DVECTOR_STRUCT'},
    'Interval': {'ON_INTERVAL_STRUCT'},
    'string': {'const RHMONO_STRING*'},
}
CS_RET_DIRECT = {'bool': 'bool', 'int': 'int', 'double': 'double'}
CS_RET_OUT = {'Point3d': 'ON_3dPoint*', 'Vector3d': 'ON_3dVector*',
              'Interval': 'ON_Interval*', 'Plane': 'ON_PLANE_STRUCT*',
              'Transform': 'ON_Xform*'}
CS_RET_EXPR = {'Point3d': 'RH3DM_P({n})', 'Vector3d': 'RH3DM_V({n})', 'Interval': 'RH3DM_I({n})',
               'Plane': 'BND_Plane::FromOnPlane(FromPlaneStruct({n}))',
               'Transform': 'BND_Transform({n})'}
CS_RET_CPP = {'Point3d': 'BND_Point3dX', 'Vector3d': 'BND_Vector3dX',
              'Interval': 'BND_IntervalX', 'Plane': 'BND_Plane',
              'Transform': 'BND_Transform'}
# interop-holder wrappers (C# Rhino.Runtime.InteropWrappers) -> C-side type,
# generated local, and return treatment
HOLDERS = {
    'StringHolder':      {'ctype': 'CRhCmnStringHolder*',
                          'local': 'CRhCmnStringHolder {n};',
                          'cs': 'string', 'conv': 'RH3DM_W({n})'},
    'SimpleArrayDouble': {'ctype': 'ON_SimpleArray<double>*',
                          'local': 'ON_SimpleArray<double> {n};',
                          'cs': 'double[]', 'elem': '{n}[i]'},
    'SimpleArrayInt':    {'ctype': 'ON_SimpleArray<int>*',
                          'local': 'ON_SimpleArray<int> {n};',
                          'cs': 'int[]', 'elem': '{n}[i]'},
    'SimpleArrayPoint3d':{'ctype': 'ON_3dPointArray*',
                          'local': 'ON_3dPointArray {n};',
                          'cs': 'Point3d[]', 'elem': 'RH3DM_P({n}[i])'},
}

EXMAP = {
    'IndexOutOfRangeException': 'std::out_of_range',
    'ArgumentOutOfRangeException': 'std::out_of_range',
    'ArgumentNullException': 'std::invalid_argument',
    'ArgumentException': 'std::invalid_argument',
    'InvalidOperationException': 'std::runtime_error',
    'NotSupportedException': 'std::runtime_error',
}
CS_CONST = {
    'Interval.Unset': ('Interval', 'BND_IntervalX::Unset()'),
    'Point3d.Unset': ('Point3d', 'BND_Point3dX::Unset()'),
    'Vector3d.Unset': ('Vector3d', 'BND_Vector3dX::Unset()'),
}
# C# enum parameter/return types. The C side always takes/returns int (the C#
# call site casts, `(int)continuityType`). 'cpp' is the binding-side spelling:
# an enum the HAND bindings already register (reused -- registering the same
# python name twice would throw), or an RH3DM_-prefixed enum class this
# generator defines and registers from the manifest's Values.
CS_ENUMS = {
    'CoordinateSystem': {'cpp': 'ON::coordinate_system', 'gen': False},
    'TransformSimilarityType': {'cpp': 'TransformSimilarityType', 'gen': False},
    'Continuity':       {'cpp': 'RH3DM_Continuity', 'gen': True},
    'IsoStatus':        {'cpp': 'RH3DM_IsoStatus', 'gen': True},
}


def camel(n):
    return n[0].lower() + n[1:]


def parse_cs_signature(sig, kind):
    if kind == 'property':
        return sig.split()[0], []
    m = re.match(r'^(\S+)\s+\w+\((.*)\)$', sig)
    if not m:
        return None, None
    ret, raw = m.group(1), m.group(2).strip()
    params = []
    if raw:
        for part in raw.split(','):
            part = part.split('=')[0].strip()
            words = part.split()
            mode = words[0] if words[0] in ('out', 'ref') else ''
            if mode:
                words = words[1:]
            if len(words) < 2:
                return None, None
            params.append((mode, ' '.join(words[:-1]), words[-1]))
    return ret, params


def plan(member, cfn, cls):
    if member.get('Body') not in ('trivial', 'pattern'):
        return None, 'body: %s' % (member.get('Body') or 'unknown')
    for g in member.get('Guards') or []:
        if g['Exception'] not in EXMAP:
            return None, 'guard exception %s' % g['Exception']
    sig_ret, sig_params = parse_cs_signature(member['Signature'], member['Kind'])
    if sig_ret is None:
        return None, 'unparsed C# signature'
    call = member['Calls'][0]
    if len(call['Args']) != len(cfn['params']):
        return None, 'arity mismatch manifest vs call'

    holder = HOLDERS.get(member.get('Holder') or '')
    holder_arg = member.get('HolderArg', -1)
    if member.get('Holder') and holder is None:
        return None, 'holder %s' % member['Holder']

    selfs = {'const %s*' % cls['on'], '%s*' % cls['on']}
    # an abstract base's members may call C fns taking the deeper base
    # (ON_Object*); the derived->base pointer conversion is implicit
    for extra in cls.get('self_extra', ()):
        selfs |= {'const %s*' % extra, '%s*' % extra}
    # value-mode members are const (embind's .property requires const
    # getters); the C layer takes a mutable pointer because C# cannot say
    # const, so cast -- legal, m_val's storage is never actually const, and
    # mutating members (ref this) keep working through it. Abstract bases
    # reach self through their pure-virtual getter; a class deriving a
    # CONCRETE base (PolyCurveX : CurveX) reuses the base's m_ptr, whose
    # static type is the base's ON type, so its own members downcast.
    basecls = BY_REG.get(cls.get('base', ''))
    concrete_base = basecls if basecls and basecls['mode'] != 'abstract' else None
    if cls['mode'] == 'abstract':
        self_expr = '%s()' % cls['getter']
    elif cls['mode'] == 'pointer':
        self_expr = ('static_cast<%s*>(m_ptr)' % cls['on']
                     if concrete_base else 'm_ptr')
    else:
        self_expr = 'const_cast<%s*>(&m_val)' % cls['on']
    cs_param_modes = {n: (mode, t) for mode, t, n in sig_params}
    call_exprs, locals_, out_by_name = [], [], {}
    for pos, (carg, cparam) in enumerate(zip(call['Args'], cfn['params'])):
        k, ctype = carg['Kind'], cparam['type']
        # self arrives as kind 'expr' (ptr locals) on classes and as kind
        # 'out' with text `this` on value structs (`ref this`); position 0
        # plus the class's own pointer type is the discriminator. STATICS have
        # no self: their pos-0 out is the RESULT (the factory idiom), handled
        # by the ordinary out machinery below.
        if (not member.get('Static') and pos == 0
                and ctype in selfs and k in ('expr', 'out')):
            call_exprs.append(self_expr)
            continue
        if holder and pos == holder_arg:
            if ctype != holder['ctype']:
                return None, 'holder ctype %s vs %s' % (ctype, holder['ctype'])
            locals_.append(holder['local'].format(n='holder_out'))
            call_exprs.append('&holder_out')
            continue
        if k == 'expr':
            # a widening/enum cast of a declared parameter is the parameter:
            # RhinoCommon spells enum args `(int)continuityType` at the call site
            mm = re.match(r'^\((?:int|uint|double)\)\s*(\w+)$', carg['Text'])
            if mm and mm.group(1) in cs_param_modes:
                k, carg = 'param', {'Kind': 'param', 'Text': mm.group(1)}
            else:
                return None, 'opaque expr arg: %s' % carg['Text']
        if k == 'const':
            if ctype.startswith('enum '):
                call_exprs.append('(%s)%s' % (ctype[5:], carg['Value']))
            else:
                call_exprs.append(carg['Value'])
        elif k == 'literal':
            call_exprs.append(carg['Text'])
        elif k == 'paramptr':
            # a generated class passed as an argument: hand the C function the
            # wrapped object's native pointer (abstract bases via their getter)
            name = carg['Text']
            cstype = cs_param_modes.get(name, ('', ''))[1]
            argcls = BY_CS.get(cstype)
            if argcls is None:
                return None, 'class arg %s: %s not in registry' % (name, cstype)
            if argcls['mode'] == 'abstract':
                ptr = '%s.%s()' % (name, argcls['getter'])
            elif argcls['mode'] == 'pointer':
                ptr = '%s.m_ptr' % name
            else:
                ptr = '&%s.m_val' % name
            call_exprs.append('(%s)(%s)' % (ctype, ptr))
        elif k == 'param':
            name = carg['Text']
            if name not in cs_param_modes:
                return None, 'param %s: not a C# parameter' % name
            cstype = cs_param_modes[name][1]
            if cstype in CS_ENUMS:
                if ctype not in ('int', 'unsigned int') and not ctype.startswith('enum '):
                    return None, 'enum param %s -> %s unsupported' % (name, ctype)
                call_exprs.append('(%s)%s' % (ctype[5:] if ctype.startswith('enum ')
                                              else 'int', name))
            elif cstype not in CS_ARG or ctype not in CS_ARG_CTYPES[cstype]:
                return None, 'param %s: %s -> %s unsupported' % (name, cstype, ctype)
            else:
                call_exprs.append(CS_ARG[cstype][1].format(n=name))
        elif k == 'out' and carg['Text'] in cs_param_modes \
                and cs_param_modes[carg['Text']][0] not in ('out', 'ref'):
            # `ref x` where x is a BY-VALUE C# parameter (RhinoCommon passes
            # structs like Transform by ref for speed): C# refers to the
            # method's own copy, so pass the address of a local copy of the
            # argument -- never a fresh default-constructed local
            name = carg['Text']
            argcls = BY_CS.get(cs_param_modes[name][1])
            if argcls is None or argcls['mode'] != 'value' or ctype.rstrip('*').replace('const ', '') != argcls['on']:
                return None, 'ref of by-value param %s (%s)' % (name, ctype)
            locals_.append('%s %s_copy = %s.m_val;' % (argcls['on'], name, name))
            call_exprs.append('&%s_copy' % name)
        elif k == 'out':
            name = carg['Text']
            if ctype not in OUTLOCAL:
                return None, 'out %s: unsupported (%s)' % (name, ctype)
            locals_.append(OUTLOCAL[ctype][0].format(n=name))
            call_exprs.append('&' + name)
            out_by_name[name] = ctype
        else:
            return None, 'arg kind ' + k

    bind_args, ret_outs = [], []
    holder_is_sigout = False
    for mode, cstype, name in sig_params:
        if mode in ('out', 'ref'):
            if (holder and not holder_is_sigout
                    and cstype == holder['cs'].rstrip('[]')
                    and name not in out_by_name):
                holder_is_sigout = True       # e.g. bool IsValidTopology(out string log)
                continue
            if name not in out_by_name:
                return None, 'C# out %s not matched in call' % name
            ret_outs.append((name, out_by_name[name]))
        else:
            if cstype in CS_ENUMS:
                bind_args.append((CS_ENUMS[cstype]['cpp'], name))
            elif cstype in BY_CS:
                bind_args.append(('const %s&' % BY_CS[cstype]['gen'], name))
            elif cstype not in CS_ARG:
                return None, 'C# arg type %s' % cstype
            else:
                bind_args.append((CS_ARG[cstype][0], name))

    cret = cfn['returns']
    if holder:
        if ret_outs:
            return None, 'holder plus other outs'
        if holder_is_sigout:
            if sig_ret != 'bool' or cret != 'bool':
                return None, 'holder sig-out needs bool/bool'
            shape = ('holdertuple', member['Holder'])
        elif sig_ret == holder['cs'] and cret in ('void', 'bool'):
            shape = ('holder', member['Holder'])
        else:
            return None, 'holder return %s vs %s' % (sig_ret, holder['cs'])
    elif ret_outs:
        shape = ('tuple', cret == 'bool', ret_outs)
        if sig_ret != 'bool' and cret == 'bool':
            return None, 'out-params with non-bool C# return'
    elif sig_ret in CS_RET_DIRECT:
        if cret != CS_RET_DIRECT[sig_ret]:
            return None, 'return mismatch %s vs %s' % (sig_ret, cret)
        shape = ('direct', cret)
    elif sig_ret in CS_RET_OUT:
        want = CS_RET_OUT[sig_ret]
        outs = [n for n, t in out_by_name.items() if t == want]
        if cret not in ('void', 'bool') or len(outs) != 1:
            return None, '%s return without single %s out' % (sig_ret, want)
        shape = ('fromout', sig_ret, outs[0])
    elif sig_ret in CS_ENUMS:
        if cret != 'int' and not cret.startswith('enum '):
            return None, 'enum return %s vs C %s' % (sig_ret, cret)
        shape = ('enumret', sig_ret)
    elif (sig_ret in BY_CS and BY_CS[sig_ret]['mode'] == 'pointer'
          and cret == '%s*' % BY_CS[sig_ret]['on']):
        shape = ('wrap', sig_ret)
    elif sig_ret == 'void' and cret == 'void':
        shape = ('direct', 'void')
    else:
        return None, 'return type %s (C: %s)' % (sig_ret, cret)

    success = member.get('Success')
    if success:
        if shape[0] != 'fromout' or success['Out'] != shape[2]:
            return None, 'success form vs shape'
        fb = success['Fallback']
        if fb != success['Out']:
            known = CS_CONST.get(fb)
            if known is None or known[0] != sig_ret:
                return None, 'fallback %s' % fb
    return {'m': member, 'fn': cfn, 'cls': cls,
            'static': bool(member.get('Static')),
            'call_exprs': call_exprs, 'locals': locals_,
            'bind_args': bind_args, 'shape': shape,
            'guards': member.get('Guards') or [], 'success': success}, None


def emit(p):
    m = p['m']
    name = p['cpp']
    sig = ', '.join('%s %s' % (t, n) for t, n in p['bind_args'])
    call = '%s(%s)' % (p['fn']['name'], ', '.join(p['call_exprs']))
    shape = p['shape']
    cq = ' const' if not p['static'] else ''
    st = 'static ' if p['static'] else ''
    L = []
    guard_lines = [
        '    if (%s) throw %s("%s");'
        % (g['cond_cpp'], EXMAP[g['Exception']],
           (g['Message'] or m['Name']).replace('"', "'"))
        for g in p['guards']]
    if shape[0] == 'direct':
        ret = shape[1]
        L.append('  %s%s %s(%s)%s {' % (st, ret, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s%s;' % ('' if ret == 'void' else 'return ', call))
    elif shape[0] == 'fromout':
        cs, out = shape[1], shape[2]
        L.append('  %s%s %s(%s)%s {' % (st, CS_RET_CPP[cs], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        if p['success']:
            L.append('    if (%s) return %s;' % (call, CS_RET_EXPR[cs].format(n=out)))
            fb = p['success']['Fallback']
            fb_cpp = (CS_RET_EXPR[cs].format(n=out) if fb == out
                      else CS_CONST[fb][1])
            L.append('    return %s;' % fb_cpp)
        else:
            L.append('    %s;' % call)
            L.append('    return %s;' % CS_RET_EXPR[cs].format(n=out))
    elif shape[0] == 'enumret':
        cpp = CS_ENUMS[shape[1]]['cpp']
        L.append('  %s%s %s(%s)%s {' % (st, cpp, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    return (%s)%s;' % (cpp, call))
    elif shape[0] == 'wrap':
        target = BY_CS[shape[1]]
        L.append('  %sclass %s* %s(%s)%s {' % (st, target['gen'], name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s* rc = %s;' % (target['on'], call))
        # most-derived generated class (an ON_Curve* may be a NURBS curve);
        # pybind11/embind then downcast the polymorphic result automatically
        L.append('    return rc ? RH3DM_Wrap_%s(rc) : nullptr;' % target['reg'])
    elif shape[0] == 'holder':
        hp = HOLDERS[shape[1]]
        if 'conv' in hp:                      # string
            L.append('  std::wstring %s(%s)%s {' % (name, sig, cq))
            L += guard_lines
            L += ['    %s' % x for x in p['locals']]
            L.append('    %s;' % call)
            L.append('    return %s;' % hp['conv'].format(n='holder_out'))
        else:                                 # array -> tuple of elements
            L.append('  BND_TUPLE %s(%s)%s {' % (name, sig, cq))
            L += guard_lines
            L += ['    %s' % x for x in p['locals']]
            L.append('    %s;' % call)
            L.append('    BND_TUPLE rc = CreateTuple(holder_out.Count());')
            L.append('    for (int i = 0; i < holder_out.Count(); i++)')
            L.append('      SetTuple(rc, i, %s);' % hp['elem'].format(n='holder_out'))
            L.append('    return rc;')
    elif shape[0] == 'holdertuple':           # bool X(out string log)
        hp = HOLDERS[shape[1]]
        L.append('  BND_TUPLE %s(%s)%s {' % (name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    bool success = %s;' % call)
        L.append('    BND_TUPLE rc = CreateTuple(2);')
        L.append('    SetTuple(rc, 0, success);')
        L.append('    SetTuple(rc, 1, %s);' % hp['conv'].format(n='holder_out'))
        L.append('    return rc;')
    else:  # tuple
        _, has_success, outs = shape
        L.append('  %sBND_TUPLE %s(%s)%s {' % (st, name, sig, cq))
        L += guard_lines
        L += ['    %s' % x for x in p['locals']]
        L.append('    %s%s;' % ('bool success = ' if has_success else '', call))
        L.append('    BND_TUPLE rc = CreateTuple(%d);' % (len(outs) + (1 if has_success else 0)))
        i = 0
        if has_success:
            L.append('    SetTuple(rc, 0, success);')
            i = 1
        for j, (n, t) in enumerate(outs):
            L.append('    SetTuple(rc, %d, %s);' % (i + j, OUTLOCAL[t][1].format(n=n)))
        L.append('    return rc;')
    L.append('  }')
    return '\n'.join(L)


def default_ctor_call(cls, manifest, c_by_name):
    """RhinoCommon's standard default ctor (`ConstructNonConstObject(
    UnsafeNativeMethods.ON_X_New(IntPtr.Zero))`) becomes a real generated
    constructor: returns the C creation expression, or None."""
    if cls['mode'] != 'pointer':
        return None
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Kind'] == 'constructor'
                and x['Variant'] in ('portable', 'rhino3dm-only')
                and x.get('Calls') and len(x['Calls']) == 1
                and x['Body'] in ('trivial', 'pattern')):
            call = x['Calls'][0]
            fn = c_by_name.get(call['Name'])
            if fn is None or fn['returns'] != '%s*' % cls['on']:
                continue
            if all(a['Kind'] == 'expr' and a['Text'] == 'IntPtr.Zero'
                   for a in call['Args']):
                expr = '%s(%s)' % (call['Name'],
                                   ', '.join(['nullptr'] * len(call['Args'])))
                return expr, fn
    return None


def arg_ctor_plans(cls, manifest, c_by_name, skipped):
    """RhinoCommon constructors with arguments (`LineCurve(Point3d from,
    Point3d to) { ConstructNonConstObject(UNM.ON_LineCurve_New2(from, to)); }`)
    planned exactly like a static factory returning the class: same argument
    marshalling, same refusal rules. Guarded ctors are skipped, not
    guard-dropped."""
    if cls['mode'] != 'pointer':
        return []
    out = []
    def skip(why):
        key = 'ctor: ' + why.split(':')[0]
        skipped[key] = skipped.get(key, 0) + 1
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Kind'] == 'constructor'
                and x['Variant'] in ('portable', 'rhino3dm-only')
                and x.get('Calls') and len(x['Calls']) == 1):
            continue
        call = x['Calls'][0]
        if all(a['Kind'] == 'expr' and a['Text'] == 'IntPtr.Zero' for a in call['Args']):
            continue                                    # the default ctor tier
        if x['Body'] not in ('trivial', 'pattern'):
            skip('body: %s' % x['Body']); continue
        if x.get('Guards'):
            skip('guarded'); continue
        fn = c_by_name.get(call['Name'])
        if fn is None or fn['returns'] != '%s*' % cls['on']:
            skip('creation fn'); continue
        sig = x['Signature']
        m2 = dict(x, Kind='method', Static=True, Name='__ctor',
                  Signature='%s __ctor%s' % (cls['cs'], sig[sig.index('('):]))
        p, why = plan(m2, fn, cls)
        if p is None:
            skip(why); continue
        if p['locals'] or p['shape'][0] != 'wrap':
            skip('shape'); continue
        p['ctor_call'] = '%s(%s)' % (fn['name'], ', '.join(p['call_exprs']))
        out.append(p)
    return out


def plan_class(cls, manifest, c_by_name, skipped):
    cands = [x for x in manifest['members']
             if x['Type'] == cls['type'] and x['Variant'] == 'portable'
             and x['Kind'] in ('method', 'property')
             and x.get('Calls') and len(x['Calls']) == 1]
    plans = []
    for x in sorted(cands, key=lambda x: (x['Name'], x['Signature'])):
        fn = c_by_name.get(x['Calls'][0]['Name'])
        if fn is None:
            skipped['C fn not in manifest'] = skipped.get('C fn not in manifest', 0) + 1
            continue
        p, why = plan(x, fn, cls)
        if p:
            plans.append(p)
        elif why:
            skipped[why.split(':')[0]] = skipped.get(why.split(':')[0], 0) + 1

    # overloads: every plan gets a unique C++ method name; registration puts
    # them all under the one RhinoCommon name (pybind dispatches by type, the
    # JS side keeps one per arity -- embind dispatches on argument count).
    # An overload whose binding-side argument types duplicate an earlier one
    # (all-out members like DecomposeAffine collapse to zero args) is
    # unreachable in BOTH languages: dropped, counted.
    seen_sigs, dedup = set(), []
    for p in plans:
        key = (p['m']['Name'], tuple(t for t, _ in p['bind_args']))
        if key in seen_sigs:
            skipped['unreachable overload (same binding signature)'] = \
                skipped.get('unreachable overload (same binding signature)', 0) + 1
            continue
        seen_sigs.add(key)
        dedup.append(p)
    plans = dedup
    counts = {}
    for p in plans:
        n = p['m']['Name']
        counts[n] = counts.get(n, 0) + 1
        p['cpp'] = 'Gen_' + n + ('' if counts[n] == 1 else '_%d' % counts[n])

    # guard identifiers: own parameters pass, zero-arg siblings become calls
    zero_arg = {}
    for p in plans:
        if not p['bind_args'] and p['m']['Name'] not in zero_arg:
            zero_arg[p['m']['Name']] = p['cpp']
    resolved = []
    for p in plans:
        argnames = {n for _, n in p['bind_args']}
        ok = True
        kept = []
        for g in p['guards']:
            # a pure null-check on a by-value marshaled param is enforced by
            # the binding boundary (pybind/embind reject None before the body)
            if g.get('NullCheckOf') and g['NullCheckOf'] in argnames:
                continue
            kept.append(g)
            cond = g['Cond']
            for ident in g['Ids']:
                if ident in argnames:
                    continue
                if ident in zero_arg:
                    cond = re.sub(r'\b%s\b' % re.escape(ident),
                                  '%s()' % zero_arg[ident], cond)
                else:
                    ok = False
                    break
            if not ok:
                break
            g['cond_cpp'] = cond
        if ok:
            p['guards'] = kept
            resolved.append(p)
        else:
            skipped['guard id'] = skipped.get('guard id', 0) + 1
    return resolved


def emit_decl_def(p, cls):
    """One member as (header declaration, cpp definition). emit() produces
    the inline form; the first line carries the whole signature, so the
    declaration is that line with `;` and the definition is the same text
    with the name qualified, `static` dropped, and the indent removed."""
    lines = emit(p).split('\n')
    first = lines[0]
    decl = first[:first.rfind('{')].rstrip() + ';'
    qualified = first.replace(' %s(' % p['cpp'],
                              ' %s::%s(' % (cls['gen'], p['cpp']), 1)
    body = [qualified] + lines[1:]
    body = [l[2:] if l.startswith('  ') else l for l in body]
    if body[0].startswith('static '):
        body[0] = body[0][len('static '):]
    return decl, '\n'.join(body)


def emit_class(cls, plans, has_default_ctor=False, arg_ctors=()):
    methods = '\n'.join(emit_decl_def(p, cls)[0] for p in plans)
    dctor = '\n  %s();' % cls['gen'] if has_default_ctor else ''
    for p in arg_ctors:
        dctor += '\n  %s(%s);' % (cls['gen'], ', '.join('%s %s' % a for a in p['bind_args']))
    # RhinoCommon's own copy ctor (`X(X other)` -> ON_X_New(other)) replaces
    # the deleted C++ copy: deep copy through the C layer, as in .NET
    self_copy = any(len(p['bind_args']) == 1 and p['bind_args'][0][0] == 'const %s&' % cls['gen']
                    for p in arg_ctors)
    # the bridge ctor copies from the hand BND class; 'bridge': False drops
    # it (and with it every dependency on the hand header) for classes the
    # generated surface can construct and populate on its own
    bridge = ('\n  %s(const class %s& source);' % (cls['gen'], cls['hand'])
              if cls.get('bridge', True) and cls['mode'] != 'abstract' else '')
    if cls['mode'] == 'abstract':
        storage = '''  virtual ~{gen}() = default;
  virtual {on}* {getter}() const = 0;'''
        return ('class {gen}\n{{\npublic:\n' + storage
                + '\n\n{methods}\n}};\n').format(
            gen=cls['gen'], on=cls['on'], getter=cls['getter'],
            methods=methods)
    base = BY_REG.get(cls.get('base', ''))
    inherit = ' : public %s' % base['gen'] if base else ''
    if base and base['mode'] != 'abstract':
        # concrete base owns the pointer (and the abstract getter override);
        # this class only adds members and a forwarding constructor
        storage = '''  explicit {gen}({on}* owned) : {basegen}(owned) {{}}{bridge}{dctor}'''
        return ('class {gen}{inherit}\n{{\npublic:\n' + storage
                + '\n\n{methods}\n}};\n').format(
            gen=cls['gen'], on=cls['on'], hand=cls['hand'],
            basegen=base['gen'], methods=methods, inherit=inherit,
            dctor=dctor, bridge=bridge)
    override = ('\n  %s* %s() const override { return m_ptr; }'
                % (base['on'], base['getter'])) if base else ''
    if cls['mode'] == 'pointer':
        storage = '''  {on}* m_ptr = nullptr;
  explicit {gen}({on}* owned) : m_ptr(owned) {{}}{bridge}{dctor}
  ~{gen}() {{ delete m_ptr; }}{nocopy}{override}'''
    else:
        storage = '''  {on} m_val;{bridge}{override}'''
    return ('class {gen}{inherit}\n{{\npublic:\n' + storage
            + '\n\n{methods}\n}};\n').format(
        gen=cls['gen'], on=cls['on'], hand=cls['hand'], methods=methods,
        inherit=inherit, override=override, dctor=dctor, bridge=bridge,
        nocopy='' if self_copy else '\n  %s(const %s&) = delete;' % (cls['gen'], cls['gen']))


def emit_registrations(cls, plans, js_pruned, has_default_ctor=False, arg_ctors=()):
    py, js = [], []
    js_arities = {}            # camel name -> arities already registered
    for p in plans:
        n, g = p['m']['Name'], p['cpp']
        pyargs = ''.join(', py::arg("%s")' % a for _, a in p['bind_args'])
        js_ok = True
        if p['m']['Kind'] != 'property':
            ar = len(p['bind_args'])
            taken = js_arities.setdefault(camel(n), set())
            if ar in taken:    # embind dispatches on arity; same-arity extras
                js_ok = False  # are pruned, first (signature-sorted) wins
                js_pruned.append('%s.%s/%d' % (cls['reg'], camel(n), ar))
            else:
                taken.add(ar)
        # embind refuses to bind a raw-pointer return implicitly ('wrap'
        # shape returns BND_*X*); allow_raw_pointers() matches the hand code
        rawp = ', allow_raw_pointers()' if p['shape'][0] == 'wrap' else ''
        if p['static']:
            py.append('      .def_static("%s", &%s::%s%s)' % (n, cls['gen'], g, pyargs))
            if js_ok:
                js.append('      .class_function("%s", &%s::%s%s)'
                          % (camel(n), cls['gen'], g, rawp))
        elif p['m']['Kind'] == 'property':
            py.append('      .def_property_readonly("%s", &%s::%s)' % (n, cls['gen'], g))
            js.append('      .property("%s", &%s::%s)' % (camel(n), cls['gen'], g))
        else:
            py.append('      .def("%s", &%s::%s%s)' % (n, cls['gen'], g, pyargs))
            if js_ok:
                js.append('      .function("%s", &%s::%s%s)'
                          % (camel(n), cls['gen'], g, rawp))
    base = BY_REG.get(cls.get('base', ''))
    if cls['mode'] == 'abstract':
        # no constructor: instances only exist as derived objects
        py_block = ('  py::class_<{gen}>(m, "{reg}")\n{defs};').format(
            gen=cls['gen'], reg=cls['reg'], defs='\n'.join(py))
        js_block = ('  class_<{gen}>("{reg}")\n{defs};').format(
            gen=cls['gen'], reg=cls['reg'], defs='\n'.join(js))
        return py_block, js_block
    py_base = ', %s' % base['gen'] if base else ''
    js_base = '<%s, base<%s>>' % (cls['gen'], base['gen']) if base \
        else '<%s>' % cls['gen']
    py_dctor = '      .def(py::init<>())\n' if has_default_ctor else ''
    js_dctor = '      .constructor<>()\n' if has_default_ctor else ''
    # embind constructors dispatch on arity only: default (0) and bridge (1)
    # keep their slots; a same-arity argument ctor is pruned in JS, counted
    js_arity = ({0} if has_default_ctor else set()) | ({1} if cls.get('bridge', True) else set())
    for p in arg_ctors:
        types = ', '.join(t for t, _ in p['bind_args'])
        py_dctor += '      .def(py::init<%s>()%s)\n' % (
            types, ''.join(', py::arg("%s")' % n for _, n in p['bind_args']))
        if len(p['bind_args']) in js_arity:
            js_pruned.append('%s.constructor/%d' % (cls['reg'], len(p['bind_args'])))
        else:
            js_arity.add(len(p['bind_args']))
            js_dctor += '      .constructor<%s>()\n' % types
    has_bridge = cls.get('bridge', True)
    if not has_bridge and not has_default_ctor:
        raise SystemExit('%s: bridge-free class needs a generated constructor'
                         % cls['reg'])
    py_bridge = ('      .def(py::init<const %s&>(), py::arg("source"))\n'
                 % cls['hand']) if has_bridge else ''
    js_bridge = ('      .constructor<const %s&>()\n' % cls['hand']
                 ) if has_bridge else ''
    py_buf, py_bufdef = '', ''
    if cls.get('buffer'):
        py_buf = ', py::buffer_protocol()'
        py_bufdef = ('      .def_buffer([]({gen}& c) -> py::buffer_info {{\n'
                     '        ON_3dPointArray& a = c.{expr};\n'
                     '        return py::buffer_info(a.Array(), sizeof(double),\n'
                     '          py::format_descriptor<double>::format(), 2,\n'
                     '          {{(py::ssize_t)a.Count(), (py::ssize_t)3}},\n'
                     '          {{(py::ssize_t)sizeof(ON_3dPoint), (py::ssize_t)sizeof(double)}}); }})\n'
                     ).format(gen=cls['gen'], expr=cls['buffer'])
    py_block = ('  py::class_<{gen}{py_base}>(m, "{reg}"{py_buf})\n{py_dctor}{py_bridge}{py_bufdef}'
                '{defs};').format(gen=cls['gen'], py_base=py_base,
                                  reg=cls['reg'], hand=cls['hand'],
                                  py_dctor=py_dctor, py_bridge=py_bridge, py_buf=py_buf, py_bufdef=py_bufdef,
                                  defs='\n'.join(py))
    js_block = ('  class_{js_base}("{reg}")\n{js_dctor}{js_bridge}'
                '{defs};').format(js_base=js_base, reg=cls['reg'],
                                  hand=cls['hand'], js_dctor=js_dctor, js_bridge=js_bridge,
                                  defs='\n'.join(js))
    return py_block, js_block


VT_BANNER = ('// GENERATED by tools/generate/bnd_gen.py from api/valuetypes.json\n'
             '// (tools/specgen translate: RhinoCommon value-type BODIES translated\n'
             '// to C++). DO NOT EDIT. Plain C++: no opennurbs, no binding headers.\n')


def vt_file(t):
    return 'gen_%s' % ((t['Cs'] + 'X') if not t['IsStaticClass'] else t['Cs']).lower()


def emit_valuetypes(vt, blocks):
    """One header + one cpp per value type (gen_point3dx.h/.cpp, ...), plus
    gen_vt_common.h (helpers + forward declarations). Headers only DECLARE:
    member declarations may name the other structs by value while still
    incomplete, and fields are plain doubles, so the mutual references
    (Point3d - Point3d -> Vector3d) need no include cycle. Each cpp includes
    every value-type header, so all bodies see complete types. Struct fields
    mirror the C# fields; consts are dependency-ordered; operators are free
    functions declared with their C# declaring type."""
    types = vt['types']
    ok = [m for m in vt['members'] if m['Ok']]
    order = sorted(types, key=lambda t: (not t['IsStaticClass'], t['Cs']))
    files = {}
    files['gen_vt_common.h'] = '\n'.join([
        VT_BANNER, '#pragma once', '#include <cmath>', '#include <limits>',
        '#include <stdexcept>', '#include <type_traits>', '',
        '// C# semantics helpers: Math.Max/Min promote mixed arguments;',
        '// Math.Sign returns int',
        'template <class A, class B> inline typename std::common_type<A, B>::type',
        'RH3DM_Max(A a, B b) { typedef typename std::common_type<A, B>::type T; return T(a) > T(b) ? T(a) : T(b); }',
        'template <class A, class B> inline typename std::common_type<A, B>::type',
        'RH3DM_Min(A a, B b) { typedef typename std::common_type<A, B>::type T; return T(a) < T(b) ? T(a) : T(b); }',
        'template <class A> inline int RH3DM_Sign(A a) { return (a > A(0)) - (a < A(0)); }',
        'inline bool RH3DM_IsPosInf(double x) { return std::isinf(x) && x > 0; }',
        'inline bool RH3DM_IsNegInf(double x) { return std::isinf(x) && x < 0; }',
        ''] + ['struct %s;' % t['Cpp'] for t in order]) + '\n'
    math_hdr = [vt_file(t) + '.h' for t in order if t['IsStaticClass']]
    all_hdrs = ''.join('#include "%s.h"\n' % vt_file(t) for t in order)
    for t in order:
        mine = [m for m in ok if m['Type'] == t['Cs']]
        consts = [m for m in mine if m['Kind'] == 'field-const']
        names = {c['Name'] for c in consts}
        placed, ordered = set(), []
        while len(ordered) < len(consts):
            progress = False
            for c in consts:
                if c['Name'] in placed:
                    continue
                deps = set(re.findall(r'\b(\w+)\b', c['ConstInit'])) & names - {c['Name']}
                if deps <= placed:
                    ordered.append(c); placed.add(c['Name']); progress = True
            if not progress:
                raise SystemExit('const cycle in %s' % t['Cs'])
        h = [VT_BANNER, '#pragma once', '#include "gen_vt_common.h"']
        # default arguments may name RhinoMath constants: it must be complete
        h += ['#include "%s"' % x for x in math_hdr if x != vt_file(t) + '.h']
        h += ['', 'struct %s\n{' % t['Cpp']]
        for f in t['Fields']:
            h.append('  %s %s = 0;' % (f['Type'], f['Name']))
        if not t['IsStaticClass']:
            h.append('  %s() = default;' % t['Cpp'])
        h += ['  ' + c['Decl'] for c in ordered]
        h += ['  ' + m['Decl'] for m in mine
              if m['Kind'] not in ('field-const', 'field', 'operator') and m['Decl']]
        h.append('};\n')
        h += [m['Decl'] for m in mine if m['Kind'] == 'operator']
        files[vt_file(t) + '.h'] = '\n'.join(h) + '\n'

        cpp = [VT_BANNER, all_hdrs]
        cpp += [m['Def'] + '\n' for m in mine if m.get('Def')]
        if t['Cs'] in blocks:
            py_block, js_tag, js_block = blocks[t['Cs']]
            reg = t['Cs'] + 'X'
            cpp.append('''#include "gen_common.h"

#if defined(ON_PYTHON_COMPILE)
void initGen{reg}(rh3dmpymodule& m)
{{
{py}
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
{tag}
void initGen{reg}(void)
{{
{js}
}}
#endif
'''.format(reg=reg, py=py_block, tag=js_tag, js=js_block))
        files[vt_file(t) + '.cpp'] = '\n'.join(cpp)
    return files


PY_OPS = {('+', 2): 'add', ('-', 2): 'sub', ('*', 2): 'mul', ('/', 2): 'truediv',
          ('%', 2): 'mod', ('==', 2): 'eq', ('!=', 2): 'ne', ('<', 2): 'lt',
          ('>', 2): 'gt', ('<=', 2): 'le', ('>=', 2): 'ge',
          ('-', 1): 'neg', ('+', 1): 'pos'}
CS_OP_NAMES = {('+', 2): 'op_Addition', ('-', 2): 'op_Subtraction',
               ('*', 2): 'op_Multiply', ('/', 2): 'op_Division', ('%', 2): 'op_Modulus',
               ('==', 2): 'op_Equality', ('!=', 2): 'op_Inequality',
               ('<', 2): 'op_LessThan', ('>', 2): 'op_GreaterThan',
               ('<=', 2): 'op_LessThanOrEqual', ('>=', 2): 'op_GreaterThanOrEqual',
               ('-', 1): 'op_UnaryNegation', ('+', 1): 'op_UnaryPlus'}


def emit_valuetype_bindings(vt, skipped):
    """Python: real classes (pybind objects are garbage-collected), RhinoCommon
    members only, plus language protocols -- operators from the C# operators,
    repr, a sequence constructor and implicit conversion from tuple/list.
    JS: embind value_object, so a value is a plain garbage-collected
    {X, Y, Z} object (no wasm-heap instance, no .delete()); behavior is
    exposed as functions, rhino.Point3dX.DistanceTo(p, q), and mutating
    members return the new value since a JS value is a copy."""
    blocks = {}   # C# type -> (py block, js tag decl, js block)
    js_key = {t['Cpp']: t['Fields'][0]['Name'][2:].upper()
              for t in vt['types'] if not t['IsStaticClass'] and t['Fields']}
    for t in vt['types']:
        if t['IsStaticClass']:
            continue
        T, cs = t['Cpp'], t['Cs']
        reg = cs + 'X'
        fields = [f['Name'] for f in t['Fields']]
        mine = [m for m in vt['members'] if m['Type'] == cs and m['Ok'] and m['Public']]
        props = {m['Name'] for m in mine if m['Kind'] == 'property'}
        # a property is field-backed when its field is m_<lowercased name>
        field_of = {('m_' + p.lower()): p for p in props if ('m_' + p.lower()) in fields}
        pyl = ['  py::class_<%s>(m, "%s")' % (T, reg), '      .def(py::init<>())']
        jsl = []
        js_seen = set()

        def sig(m):
            return ', '.join('%s %s' % (p['Type'], p['Name']) for p in m['Params'] or [])

        def names(m):
            return ', '.join(p['Name'] for p in m['Params'] or [])

        def pyargs(m):
            return ''.join(', py::arg("%s")' % p['Name'] for p in m['Params'] or [])

        # JS functions are collected per (name, arity) and emitted after the
        # loop: embind keeps ONE function per name+arity, so same-arity
        # overloads (Point3d*double vs double*Point3d, Vector3d*Vector3d dot)
        # get a generated C++ dispatcher on argument shape instead of the
        # first one silently winning (generator-log G5). Bodies use a0..aN.
        js_entries = {}

        def js_fn(name, types, body):
            js_entries.setdefault((name, len(types)), []).append((types, body))

        for m in mine:
            if any(p['Mode'] for p in m['Params'] or []):
                skipped['value-type ref/out param'] = skipped.get('value-type ref/out param', 0) + 1
                continue
            k, n = m['Kind'], m['Name']
            if k == 'ctor':
                if m['Decl']:
                    pyl.append('      .def(py::init<%s>()%s)'
                               % (', '.join(p['Type'] for p in m['Params']), pyargs(m)))
                else:
                    pyl.append('      .def(py::init<const %s&>())' % T)
            elif k == 'method' and m['Static']:
                pyl.append('      .def_static("%s", [](%s) { return %s::%s(%s); }%s)'
                           % (n, sig(m), T, n, names(m), pyargs(m)))
                js_fn(n, [p['Type'] for p in m['Params']],
                      'return %s::%s(%s);' % (T, n, ', '.join('a%d' % i for i in range(len(m['Params'])))))
            elif k == 'method':
                self_sig = ', '.join(['%s& self' % T] + ([sig(m)] if sig(m) else []))
                pyl.append('      .def("%s", [](%s) { return self.%s(%s); }%s)'
                           % (n, self_sig, n, names(m), pyargs(m)))
                types = [T] + [p['Type'] for p in m['Params']]
                call = 'a0.%s(%s)' % (n, ', '.join('a%d' % (i + 1) for i in range(len(m['Params']))))
                if m['Ret'] == 'void':     # mutator: a JS value is a copy -> return it
                    js_fn(n, types, '%s; return a0;' % call)
                elif m.get('Mutates'):     # mutates AND returns: both (generator-log G6)
                    js_fn(n, types, 'auto r = %s; emscripten::val o = emscripten::val::object(); '
                                    'o.set("self", a0); o.set("returns", r); return o;' % call)
                else:
                    js_fn(n, types, 'return %s;' % call)
            elif k in ('property', 'static-field') and m['Static']:
                pyl.append('      .def_property_readonly_static("%s", [](py::object) { return %s::%s(); })'
                           % (n, T, m['Getter']))
                js_fn(n, [], 'return %s::%s();' % (T, m['Getter']))
            elif k == 'property':
                get = '[](%s& s) { return s.%s(); }' % (T, m['Getter'])
                if m.get('Setter'):
                    pyl.append('      .def_property("%s", %s, [](%s& s, %s v) { s.%s(v); })'
                               % (n, get, T, m['Ret'], m['Setter']))
                else:
                    pyl.append('      .def_property_readonly("%s", %s)' % (n, get))
                if n not in field_of.values():          # field-backed: a plain JS field
                    js_fn(n, [T], 'return a0.%s();' % m['Getter'])
            elif k == 'indexer':
                pyl.append('      .def("__getitem__", [](%s& s, %s) { return s.get_Item(%s); })'
                           % (T, sig(m), names(m)))
                if m.get('Setter'):
                    pyl.append('      .def("__setitem__", [](%s& s, %s, %s value) { s.set_Item(%s, value); })'
                               % (T, sig(m), m['Ret'], names(m)))
            elif k == 'operator':
                ps = m['Params']
                key = (m['Op'], len(ps))
                if key not in PY_OPS:
                    skipped['value-type operator %s' % m['Op']] = 1
                    continue
                dunder = PY_OPS[key]
                if len(ps) == 1:
                    pyl.append('      .def("__%s__", [](%s a) { return %sa; }, py::is_operator())'
                               % (dunder, ps[0]['Type'], m['Op']))
                elif ps[0]['Type'] == T:
                    pyl.append('      .def("__%s__", [](%s a, %s b) { return a %s b; }, py::is_operator())'
                               % (dunder, ps[0]['Type'], ps[1]['Type'], m['Op']))
                else:                                   # other type on the left: reflected
                    pyl.append('      .def("__r%s__", [](%s b, %s a) { return a %s b; }, py::is_operator())'
                               % (dunder, ps[1]['Type'], ps[0]['Type'], m['Op']))
                js_fn(CS_OP_NAMES[key], [p['Type'] for p in ps],
                      ('return %sa0;' % m['Op']) if len(ps) == 1 else ('return a0 %s a1;' % m['Op']))

        def js_check(v, t):
            if t in ('double', 'float', 'int', 'unsigned int', 'long long'):
                return '%s.isNumber()' % v
            if t == 'bool':
                return '(%s.typeOf().as<std::string>() == "boolean")' % v
            if t in js_key:
                return '(%s.typeOf().as<std::string>() == "object" && %s.hasOwnProperty("%s"))' % (v, v, js_key[t])
            return 'true'

        for (fname, arity), ents in js_entries.items():
            if len(ents) == 1:
                types, body = ents[0]
                params = ', '.join('%s a%d' % (t, i) for i, t in enumerate(types))
                jsl.append('      .class_function("%s", optional_override([](%s) { %s }))'
                           % (fname, params, body))
                continue
            # dispatcher: first overload whose argument shapes match. Shapes
            # that JS cannot tell apart (Point3d vs Vector3d are both {X,Y,Z})
            # resolve to the first -- same numbers for every such pair here.
            vparams = ', '.join('emscripten::val v%d' % i for i in range(arity))
            branches = []
            for types, body in ents:
                cond = ' && '.join(js_check('v%d' % i, t) for i, t in enumerate(types)) or 'true'
                conv = ' '.join('%s a%d = v%d.as<%s>();' % (t, i, i, t) for i, t in enumerate(types))
                branches.append('if (%s) { %s return emscripten::val([&]() { %s }()); }'
                                % (cond, conv, body))
            jsl.append('      .class_function("%s", optional_override([](%s) -> emscripten::val {\n'
                       '        %s\n        return emscripten::val::undefined(); }))'
                       % (fname, vparams, '\n        '.join(branches)))
            skipped['js value-type dispatcher'] = skipped.get('js value-type dispatcher', 0) + 1

        # protocols: sequence construction (+ implicit tuple/list), repr
        nf = len(fields)
        pyl.append('      .def(py::init([](py::sequence s) {\n'
                   '        if (py::len(s) != %d) throw py::value_error("%s needs %d values");\n'
                   '        %s v;\n%s        return v; }))'
                   % (nf, reg, nf, T, ''.join('        v.%s = s[%d].cast<double>();\n' % (f, i)
                                              for i, f in enumerate(fields))))
        pyl.append('      .def("__repr__", [](const %s& v) { return py::str("%s(%s)").format(%s); });'
                   % (T, reg, ', '.join(['{}'] * nf), ', '.join('v.%s' % f for f in fields)))
        pyl.append('  py::implicitly_convertible<py::tuple, %s>();' % T)
        pyl.append('  py::implicitly_convertible<py::list, %s>();' % T)
        py_block = '\n'.join(pyl)

        vo = ('  value_object<%s>("%sValue")\n%s;'
              % (T, reg, '\n'.join('      .field("%s", &%s::%s)' % (field_of.get(f, f), T, f)
                                   for f in fields)))
        tag = 'RH3DM_%sFns' % reg
        blocks[cs] = (py_block, 'struct %s {};' % tag,
                      vo + '\n  class_<%s>("%s")\n%s;' % (tag, reg, '\n'.join(jsl)))

    return blocks


def main():
    manifest = json.load(open(os.path.join(ROOT, 'api', 'manifest.json')))
    c_by_name = {f['name']: f for f in manifest['c_surface']}
    # rhcommon_c.h declares some exports with opaque handle types
    # (ON_LineCurveImpl*) while the definitions use the real ones
    # (ON_LineCurve*); same C symbol. Generate against the real types.
    unimpl = lambda t: re.sub(r'\b(ON_\w+?)Impl\*', r'\1*', t or '')
    for f in c_by_name.values():
        f['returns'] = unimpl(f['returns'])
        for q in f['params']:
            q['type'] = unimpl(q['type'])

    skipped = {}
    per_class = [(cls, plan_class(cls, manifest, c_by_name, skipped))
                 for cls in CLASSES]

    all_plans = [p for _, plans in per_class for p in plans]
    # opaque declarations for C enums appearing in planned signatures
    enum_decls = sorted({t[5:] for p in all_plans for t in
                         ([p['fn']['returns']] + [q['type'] for q in p['fn']['params']])
                         if t.startswith('enum ')})

    # C# enums the plans use that no hand binding registers: define an
    # RH3DM_-prefixed enum class from the manifest's Values and register it
    # under the RhinoCommon name (verified free of hand collisions).
    cpp_to_enum = {v['cpp']: k for k, v in CS_ENUMS.items() if v['gen']}
    used_enums = set()
    for p in all_plans:
        for t, _ in p['bind_args']:
            if t in cpp_to_enum:
                used_enums.add(cpp_to_enum[t])
        if p['shape'][0] == 'enumret' and CS_ENUMS[p['shape'][1]]['gen']:
            used_enums.add(p['shape'][1])
    enum_values = {}
    for x in manifest['members']:
        if (x['Kind'] == 'enum' and x['Name'] in used_enums
                and x['Name'] not in enum_values and x.get('Values')):
            enum_values[x['Name']] = x['Values']
    missing = used_enums - set(enum_values)
    if missing:
        raise SystemExit('no manifest Values for enums: %s' % sorted(missing))
    gen_enum_defs, gen_enum_py, gen_enum_js = [], [], []
    for ename in sorted(used_enums):
        cpp = CS_ENUMS[ename]['cpp']
        pairs = [(v.split('=')[0].strip(), v.split('=')[1].strip())
                 for v in enum_values[ename]]
        gen_enum_defs.append('enum class %s : int { %s };'
                             % (cpp, ', '.join('%s = %s' % pr for pr in pairs)))
        vals = '\n'.join('      .value("%s", %s::%s)' % (n, cpp, n)
                         for n, _ in pairs)
        gen_enum_py.append('  py::enum_<%s>(m, "%s")\n%s;' % (cpp, ename, vals))
        gen_enum_js.append('  enum_<%s>("%s")\n%s;' % (cpp, ename, vals))
    decls = {'extern "C" %s %s(%s);' % (
        p['fn']['returns'], p['fn']['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in p['fn']['params']))
        for p in all_plans}

    ctors = {cls['reg']: default_ctor_call(cls, manifest, c_by_name)
             for cls in CLASSES}
    argctors = {cls['reg']: arg_ctor_plans(cls, manifest, c_by_name, skipped)
                for cls in CLASSES}
    decls |= {'extern "C" %s %s(%s);' % (
        p['fn']['returns'], p['fn']['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in p['fn']['params']))
        for ps in argctors.values() for p in ps}
    decls |= {'extern "C" %s %s(%s);' % (
        fn['returns'], fn['name'],
        ', '.join('%s %s' % (q['type'], q['name'] or 'a') for q in fn['params']))
        for expr_fn in ctors.values() if expr_fn
        for fn in [expr_fn[1]]}

    js_pruned = []
    reg_blocks = {cls['reg']: emit_registrations(cls, plans, js_pruned,
                                                 bool(ctors[cls['reg']]),
                                                 argctors[cls['reg']])
                  for cls, plans in per_class}

    BANNER = ('// GENERATED by tools/generate/bnd_gen.py (v3) -- DO NOT EDIT.\n'
              '// Shaped by RhinoCommon signatures, calling the flat C layer with\n'
              '// constants and guards baked from the C# call sites '
              '(api/manifest.json).\n')
    files = {}   # filename -> content

    vt_path = os.path.join(ROOT, 'api', 'valuetypes.json')
    if os.path.exists(vt_path):
        vt = json.load(open(vt_path))
        vt_regs = [t['Cs'] + 'X' for t in vt['types'] if not t['IsStaticClass']]
        files.update(emit_valuetypes(vt, emit_valuetype_bindings(vt, skipped)))
    else:
        vt_regs = []

    files['gen_common.h'] = BANNER + '''#pragma once
#include "bindings.h"
#include <stdexcept>

// The real C-layer contract header: blittable structs, RHMONO_STRING, and
// CRhCmnStringHolder (whose layout is platform-conditional, so a local copy
// would be an ABI trap). It has no includes of its own; bindings.h already
// provided opennurbs. Include under RHINO3DM_BUILD, scoped: that is the
// branch the C layer itself compiles this header with (the other branch
// declares Win32 types like HBITMAP), so CRhCmnStringHolder gets the
// identical declaration here.
#if !defined(RHINO3DM_BUILD)
#define RHINO3DM_BUILD
#define RH3DM_GEN_TEMP_DEF
#endif
#include "../librhino3dm_native/rhcommon_c/rhcommon_c_api.h"
#if defined(RH3DM_GEN_TEMP_DEF)
#undef RHINO3DM_BUILD
#undef RH3DM_GEN_TEMP_DEF
#endif
ON_Plane FromPlaneStruct(const ON_PLANE_STRUCT& ps);

// generated value types (translated RhinoCommon bodies) and the conversions
// from the C layer's opennurbs values; field-wise, so independent of which
// C# constructors translated
#include "gen_rhinomath.h"
#include "gen_intervalx.h"
#include "gen_point3dx.h"
#include "gen_vector3dx.h"
inline BND_Point3dX RH3DM_P(const ON_3dPoint& p) { BND_Point3dX r; r.m_x = p.x; r.m_y = p.y; r.m_z = p.z; return r; }
inline BND_Vector3dX RH3DM_V(const ON_3dVector& v) { BND_Vector3dX r; r.m_x = v.x; r.m_y = v.y; r.m_z = v.z; return r; }
inline BND_IntervalX RH3DM_I(const ON_Interval& i) { BND_IntervalX r; r.m_t0 = i.m_t[0]; r.m_t1 = i.m_t[1]; return r; }

// CRhCmnStringHolder::Array() yields RHMONO_STRING: wchar_t on Windows,
// UTF-16 (ON__UINT16) on Linux/Android/WASM -- convert accordingly.
inline std::wstring RH3DM_W(const CRhCmnStringHolder& h)
{
  const RHMONO_STRING* a = h.Array();
  if (!a) return std::wstring();
#if defined(ON_RUNTIME_LINUX) || defined(ON_RUNTIME_ANDROID) || defined(ON_RUNTIME_WASM)
  int n = 0;
  while (a[n]) n++;
  std::wstring out;
  out.resize((size_t)n + 8);
  unsigned int err = 0;
  int wn = ON_ConvertUTF16ToWideChar(false, (const ON__UINT16*)a, n,
                                     &out[0], (int)out.size(), &err, 0xFFFFFFFF,
                                     0xFFFD, nullptr);
  out.resize(wn > 0 ? (size_t)wn : 0);
  return out;
#else
  return std::wstring((const wchar_t*)a);
#endif
}

// The inverse: a binding-side std::wstring becomes the C layer's
// RHMONO_STRING* (wchar_t on Windows; UTF-16 elsewhere). Returned as a
// vector (not basic_string: char_traits for non-char types is gone from
// newer standard libraries); the temporary lives to the end of the full
// expression, which is exactly the C call.
// NOT via ON_ConvertWideCharToUTF16: its opennurbs definition takes char*
// where the extern "C" declaration says ON__UINT16*, so the declared symbol
// exists in no build (pilot-findings.md #13). wchar_t is UTF-32 on every
// non-Windows target, so call the branch it would have dispatched to.
inline std::vector<RHMONO_STRING> RH3DM_S(const std::wstring& s)
{
  std::vector<RHMONO_STRING> out(s.size() * 2 + 2, 0);
#if defined(ON_RUNTIME_LINUX) || defined(ON_RUNTIME_ANDROID) || defined(ON_RUNTIME_WASM) || defined(__APPLE__)
  unsigned int err = 0;
  ON_ConvertUTF32ToUTF16(false, (const ON__UINT32*)s.c_str(), (int)s.size(),
                         (ON__UINT16*)out.data(), (int)out.size() - 1,
                         &err, 0xFFFFFFFF, 0xFFFD, nullptr);
#else
  for (size_t i = 0; i < s.size(); i++) out[i] = (RHMONO_STRING)s[i];
#endif
  return out;
}

''' + '\n'.join(['enum %s : int;' % e for e in enum_decls] + gen_enum_defs
                + ['']) \
        + '\n'.join(sorted(decls)) + '\n\n' \
        + '\n'.join('class %s;' % cls['gen'] for cls in CLASSES) + '\n\n' \
        + '// subclass-aware wrapping: the most-derived generated class (gen_wrap.cpp)\n' \
        + '\n'.join('%s* RH3DM_Wrap_%s(%s* p);' % (c['gen'], c['reg'], c['on'])
                    for c in CLASSES if c['mode'] == 'pointer') + '\n'

    def fname(cls):
        return 'gen_%s' % cls['reg'].lower()

    for cls, plans in per_class:
        # the header carries declarations only: it needs just gen_common.h
        # (forward decls cover pointer returns) and the base class, whose
        # complete type inheritance requires
        base_inc = ('#include "gen_%s.h"\n' % cls['base'].lower()
                    if cls.get('base') else '')
        files[fname(cls) + '.h'] = (
            BANNER + '#pragma once\n#include "gen_common.h"\n' + base_inc
            + '\n' + emit_class(cls, plans, bool(ctors[cls['reg']]), argctors[cls['reg']]))

        # cross-class returns need the target's complete type where the
        # BODIES live -- the cpp
        arg_classes = {c['reg'] for p in plans + argctors[cls['reg']] for t, _ in p['bind_args']
                       for c in CLASSES if t == 'const %s&' % c['gen']}
        deps = sorted(({BY_CS[p['shape'][1]]['reg'] for p in plans
                        if p['shape'][0] == 'wrap'} | arg_classes) - {cls['reg']})
        dep_includes = ''.join('#include "gen_%s.h"\n' % d.lower() for d in deps)
        defs = '\n\n'.join(emit_decl_def(p, cls)[1] for p in plans)

        py_block, js_block = reg_blocks[cls['reg']]
        if cls['mode'] == 'abstract':
            hand_inc, ctor = '', ''
        else:
            bridged = cls.get('bridge', True)
            hand_inc = ('#include "%s"\n' % cls['hand_header']) if bridged else ''
            basecls = BY_REG.get(cls.get('base', ''))
            if basecls and basecls['mode'] != 'abstract':
                member = basecls['gen']       # forward to the owning base
            else:
                member = 'm_ptr' if cls['mode'] == 'pointer' else 'm_val'
            ctor = ('\n{gen}::{gen}(const {hand}& source)\n  : {member}({copy}) {{}}\n'.format(
                gen=cls['gen'], hand=cls['hand'], member=member,
                copy=cls['copy']) if bridged else '')
            if ctors[cls['reg']]:
                ctor += '\n{gen}::{gen}()\n  : {member}({expr}) {{}}\n'.format(
                    gen=cls['gen'], member=member, expr=ctors[cls['reg']][0])
            for p in argctors[cls['reg']]:
                ctor += '\n{gen}::{gen}({sig})\n  : {member}({expr}) {{}}\n'.format(
                    gen=cls['gen'], member=member, expr=p['ctor_call'],
                    sig=', '.join('%s %s' % a for a in p['bind_args']))
        files[fname(cls) + '.cpp'] = BANNER + '''#include "{hdr}.h"
{dep_includes}{hand_inc}{ctor}
{defs}

#if defined(ON_PYTHON_COMPILE)
void initGen{reg}(rh3dmpymodule& m)
{{
{py_block}
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
void initGen{reg}(void)
{{
{js_block}
}}
#endif
'''.format(hdr=fname(cls), dep_includes=dep_includes, hand_inc=hand_inc,
           ctor=ctor, defs=defs, reg=cls['reg'],
           py_block=py_block, js_block=js_block)

    def depth(c):
        n, b = 0, c.get('base')
        while b:
            n, b = n + 1, BY_REG[b].get('base')
        return n

    def derives(d, c):
        b = d.get('base')
        while b:
            if b == c['reg']:
                return True
            b = BY_REG[b].get('base')
        return False

    wrap = []
    for c in [c for c in CLASSES if c['mode'] == 'pointer']:
        body = ['%s* RH3DM_Wrap_%s(%s* p)\n{' % (c['gen'], c['reg'], c['on'])]
        for d in sorted((d for d in CLASSES if d['mode'] == 'pointer' and derives(d, c)),
                        key=lambda d: (-depth(d), d['reg'])):
            body.append('  if (%s* d = %s::Cast(p)) return new %s(d);'
                        % (d['on'], d['on'], d['gen']))
        body.append('  return new %s(p);\n}' % c['gen'])
        wrap.append('\n'.join(body))
    files['gen_wrap.cpp'] = (BANNER + ''.join('#include "gen_%s.h"\n' % c['reg'].lower()
                                             for c in CLASSES) + '\n' + '\n\n'.join(wrap) + '\n')

    inits_py = '\n'.join('  initGen%s(m);' % cls['reg'] for cls in CLASSES)
    inits_js = '\n'.join('  initGen%s();' % cls['reg'] for cls in CLASSES)
    files['gen_init.cpp'] = BANNER + '''#include "gen_common.h"

#if defined(ON_PYTHON_COMPILE)
{py_decls}
{vt_py_decls}
void initGeneratedBindings(rh3dmpymodule& m)
{{
{enums_py}
{vt_py}{inits_py}
}}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;
{js_decls}
{vt_js_decls}
void initGeneratedBindings(void)
{{
{enums_js}
{vt_js}{inits_js}
}}
#endif
'''.format(
        py_decls='\n'.join('void initGen%s(rh3dmpymodule&);' % cls['reg']
                           for cls in CLASSES),
        js_decls='\n'.join('void initGen%s(void);' % cls['reg']
                           for cls in CLASSES),
        enums_py='\n'.join(gen_enum_py), enums_js='\n'.join(gen_enum_js),
        inits_py=inits_py, inits_js=inits_js,
        vt_py_decls=''.join('void initGen%s(rh3dmpymodule&);\n' % r for r in vt_regs),
        vt_js_decls=''.join('void initGen%s(void);\n' % r for r in vt_regs),
        vt_py=''.join('  initGen%s(m);\n' % r for r in vt_regs),
        vt_js=''.join('  initGen%s();\n' % r for r in vt_regs))

    # the C-layer translation units the generated code calls into, from the
    # manifest's defining file per function -- replaces discovering the link
    # closure one linker error at a time
    used = {re.search(r'\b(\w+)\(', d.split('"C"')[1]).group(1) for d in decls}
    native = os.path.join(ROOT, 'src', 'librhino3dm_native')
    # 55 exports are declared RH_C_FUNCTION in rhcommon_c.h but DEFINED in a
    # .cpp without the macro (the manifest records the header): find the
    # definition -- a line opening with the return type, not a call
    defined_in = {}
    for f in sorted(os.listdir(native)):
        if f.endswith('.cpp'):
            for line in open(os.path.join(native, f), errors='replace'):
                mm = re.match(r'^(?:RH_C_FUNCTION\s+)?[A-Za-z_][\w:<> ]*[\s*&]+(\w+)\s*\(', line)
                if mm and not line.rstrip().endswith(';'):
                    defined_in.setdefault(mm.group(1), f)
    tus = set()
    for n in used:
        f = c_by_name[n]['file'] if n in c_by_name else ''
        if not f.endswith('.cpp') or not os.path.exists(os.path.join(native, f)):
            f = defined_in.get(n, '')
        if f:
            tus.add(f)
        else:
            raise SystemExit('no defining C-layer file for %s' % n)
    # transitive closure over free-function helpers the C layer calls across
    # files (FromCircleStruct -> on_circle.cpp, ...): each listed file's
    # calls to functions DEFINED in another C-layer file pull that file in
    texts = {}
    def text(f):
        if f not in texts:
            texts[f] = open(os.path.join(native, f), errors='replace').read()
        return texts[f]
    frontier = set(tus)
    while frontier:
        nxt = set()
        for f in frontier:
            for ident in set(re.findall(r'\b([A-Za-z_]\w+)\s*\(', text(f))):
                g = defined_in.get(ident)
                if g and g not in tus and not re.search(r'^[A-Za-z_][\w:<> ]*[\s*&]+%s\s*\(' % ident,
                                                        text(f), re.M):
                    nxt.add(g)
        tus |= nxt
        frontier = nxt
    tus = sorted(tus)
    files['clayer_sources.cmake'] = (
        '# GENERATED by tools/generate/bnd_gen.py -- DO NOT EDIT.\n'
        '# C-layer sources defining every flat C function the generated bindings\n'
        '# call (%d functions, %d files). Helper TUs those reach indirectly are\n'
        '# listed by hand in src/CMakeLists.txt.\n'
        'set(generated_clayer_SRC\n%s)\n'
        % (len(used), len(tus), ''.join('  "${CMAKE_CURRENT_SOURCE_DIR}/librhino3dm_native/%s"\n' % t
                                        for t in tus)))

    # RH3DM_GEN_OUT: dry-run elsewhere (e.g. while a build reads src/generated)
    out_dir = os.environ.get('RH3DM_GEN_OUT') or os.path.join(ROOT, 'src', 'generated')
    os.makedirs(out_dir, exist_ok=True)
    wrote, kept = 0, 0
    for name, content in files.items():
        path = os.path.join(out_dir, name)
        if os.path.exists(path) and open(path, encoding='utf-8').read() == content:
            kept += 1          # unchanged: keep mtime, incremental build skips it
            continue
        with open(path, 'w', encoding='utf-8', newline='\n') as f:
            f.write(content)
        wrote += 1
    # output hygiene: a generated file this run did not produce is STALE and
    # would compile silently -- remove anything unexpected (old layouts too)
    removed = []
    for name in sorted(os.listdir(out_dir)):
        if name.endswith(('.cpp', '.h', '.cmake')) and name not in files:
            os.remove(os.path.join(out_dir, name))
            removed.append(name)

    for cls, plans in per_class:
        props = sum(1 for p in plans if p['m']['Kind'] == 'property')
        print('  %-10s %d members (%d properties) -> %s.{h,cpp}'
              % (cls['reg'], len(plans), props, fname(cls)))
    print('src/generated/: %d members, %d files (%d written, %d unchanged%s)'
          % (len(all_plans), len(files), wrote, kept,
             ', removed stale: ' + ', '.join(removed) if removed else ''))
    if js_pruned:
        print('  js-only same-arity prunes (%d): %s'
              % (len(js_pruned), ', '.join(js_pruned)))
    for why, n in sorted(skipped.items(), key=lambda kv: -kv[1]):
        print('  skipped %-38s %d' % (why, n))
    return 0


if __name__ == '__main__':
    sys.exit(main())
