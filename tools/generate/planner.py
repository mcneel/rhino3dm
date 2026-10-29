"""Planning: one RhinoCommon member -> a plan (C call, argument
marshalling, return shape) or a refusal reason."""

import os
import sys
from gen_registry import *


def _derives_from(d, base):
    b = BY_REG.get(d.get('base', ''))
    while b is not None:
        if b is base:
            return True
        b = BY_REG.get(b.get('base', ''))
    return False


def storage_owner(cls):
    """the class holding m_ptr: itself, or the concrete base it extends"""
    while True:
        b = BY_REG.get(cls.get('base', ''))
        if b is None or b['mode'] == 'abstract':
            return cls
        cls = b


def self_access(cls, self_expr, accessor):
    """the this-pointer as C# obtained it. A view (File3dm proxy, or the
    geometry/attributes of a File3dmObject) has two: ConstPointer (resolve, or
    throw DocumentCollectedException) and NonConstPointer (resolve, or copy on
    write). A class sharing a view-capable base's storage downcasts the base's
    pair. Abstract bases forward to the derived class's pair. Anything else:
    its one pointer."""
    nonconst = accessor == 'NonConstPointer'
    if cls.get('proxy') or cls.get('shared'):
        return 'RH3DM_NonConst()' if nonconst else 'RH3DM_Const()'
    owner = storage_owner(cls)
    if cls.get('comp') and owner is cls:
        # a component (BrepLoop, SubDFace): resolved through its owner (G43)
        return 'RH3DM_NonConst()' if nonconst else 'RH3DM_Const()'
    if owner is not cls and owner.get('proxy'):
        return 'static_cast<%s*>(%s)' % (cls['on'], 'RH3DM_NonConst()' if nonconst else 'RH3DM_Const()')
    if cls['mode'] == 'abstract':
        return '%s%s()' % (cls['getter'], 'NC' if nonconst else '')
    return self_expr


def plan(member, cfn, cls):
    if member.get('Body') not in ('trivial', 'pattern', 'construct'):
        return None, 'body: %s' % (member.get('Body') or 'unknown')
    if member.get('Wrap') == '@return' and not member.get('FactoryArgs'):
        # `return CreateGeometryHelper(p, null);` (GeometryBase.Duplicate; G69): the
        # most-derived generated class owning the new object -- the wrap, dispatched
        # by RH3DM_Wrap_<root> on the runtime type
        member = dict(member, Wrap=cs_unqualify(member.get('Returns') or ''), WrapArity=0)
    if (member.get('Wrap') or '').startswith('@') or member.get('FactoryArgs'):
        return None, 'unresolved factory'           # specgen could not resolve it: never guess
    for g in member.get('Guards') or []:
        if g.get('Exception') is None:
            continue                     # an early return: checked once the shape is known
        if g['Exception'] not in EXMAP:
            return None, 'guard exception %s' % g['Exception']
    # `if (null == curve) return false;`: the argument is optional (None /
    # null) so the early return is reachable, exactly as in C#
    retnull = {g['NullCheckOf'] for g in member.get('Guards') or []
               if g.get('Exception') is None and g.get('NullCheckOf')}
    # `if (null == other || other.ConstPointer() == ConstPointer()) return;` (Mesh.Append;
    # G73): the null arm of a compound early return makes the argument optional too
    for g in member.get('Guards') or []:
        nm = re.match(r'^\s*\(?\s*(?:null\s*==\s*(\w+)|(\w+)\s*==\s*null)\s*\)?\s*\|\|', g.get('Cond') or '')
        if g.get('Exception') is None and nm:
            retnull.add(nm.group(1) or nm.group(2))
    sig_ret, sig_params = cs_signature(member)
    if sig_ret is None:
        return None, 'unparsed C# signature'
    call = member['Calls'][0]
    if len(call['Args']) != len(cfn['params']):
        return None, 'arity mismatch manifest vs call'
    for c in member.get('Calls') or []:
        if c['Name'] in UNSAFE_NATIVES:
            return None, 'unsafe native %s (%s)' % (c['Name'], UNSAFE_NATIVES[c['Name']])
    if cfn.get('variant') == 'rhino-only':
        # the C function lives in the rhino repo only (DracoCompression -> draco.cpp)
        return None, 'native not in rhino3dm'
    if member.get('NativeUnless'):
        # `rc = IsDocumentControlled ? m_id : F(...)`: the native arm only where the
        # property is statically false for this class (G57)
        if no_owned_pointer_cpp(cls, member['NativeUnless']) != 'false':
            return None, 'native unless %s' % member['NativeUnless']

    holder = HOLDERS.get(member.get('Holder') or '')
    holder_arg = member.get('HolderArg', -1)
    if member.get('Holder') and holder is None:
        return None, 'holder %s' % member['Holder']
    if holder and holder.get('ptr') and not member.get('_wraptuple'):
        return None, 'holder of pointers %s' % member['Holder']      # plan_wraptuple only (G72)

    selfs = {'const %s*' % cls['on'], '%s*' % cls['on']}
    # an abstract base's members may call C fns taking the deeper base
    # (ON_Object*); the derived->base pointer conversion is implicit
    for extra in cls.get('self_extra', ()):
        selfs |= {'const %s*' % extra, '%s*' % extra}
    b = BY_REG.get(cls.get('base', ''))
    while b is not None:                 # ON_Geometry_Dimension(const ON_Geometry*) on a curve
        selfs |= {'const %s*' % b['on'], '%s*' % b['on']}
        b = BY_REG.get(b.get('base', ''))
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
    elif cls['mode'] in ('pointer', 'child'):
        self_expr = ('static_cast<%s*>(m_ptr)' % cls['on']
                     if concrete_base else 'm_ptr')
    else:
        self_expr = value_self(cls)
    cs_param_modes = {n: (mode, t) for mode, t, n in sig_params}
    # a class argument null-checked by an early return is optional (None/null)
    nullable_params = {n for n in retnull if cs_param_modes.get(n, ('', ''))[1] in BY_CS or cs_param_modes.get(n, ('', ''))[1] in DATA_CPP}
    call_exprs, locals_, out_by_name, extra_fns, typearg_pos = [], [], {}, [], []
    ref_inputs = set()                 # C# `ref T` value parameters: bound and returned (G120)
    out_const = {}        # out locals initialized to a class constant (G66)
    newobj = None
    # `options = options ?? new File3dmWriteOptions();` (G64): a data-class argument
    # defaulted to a fresh instance -- optional at the binding, read through a local
    data_acc = {}
    for pn, pt in (member.get('ParamDefaults') or {}).items():
        pcs = cs_param_modes.get(pn, ('', ''))[1]
        if pcs != pt or pcs not in DATA_CPP:
            return None, 'param default %s = new %s' % (pn, pt)
        nullable_params.add(pn)
        locals_.append('const %s& rh3dm_%s = %s ? *%s : %s();' % (DATA_CPP[pcs], pn, pn, pn, DATA_CPP[pcs]))
        data_acc[pn] = 'rh3dm_%s.' % pn
    for pos, (carg, cparam) in enumerate(zip(call['Args'], cfn['params'])):
        k, ctype = carg['Kind'], cparam['type']
        fp = (TYPE_RECS.get(cls['type']) or {}).get('FileProxy') or {}
        if k == 'fieldptr' and cls.get('proxy') and carg['Text'] == fp.get('FileField'):
            # a File3dm proxy's own file (`m_parent.ConstPointer()`)
            if ctype.replace('const ', '') != 'ONX_Model*':
                return None, 'field pointer %s -> %s' % (carg['Text'], ctype)
            call_exprs.append('m_model')
            continue
        if k == 'expr' and cls.get('proxy') and carg['Text'] == fp.get('IdField'):
            if ctype != 'ON_UUID':
                return None, 'id field %s -> %s' % (carg['Text'], ctype)
            call_exprs.append('m_id')
            continue
        # self arrives as kind 'expr' (ptr locals) on classes, as kind 'out'
        # with text `this` on value structs (`ref this`), and as 'fieldptr' on
        # a child VIEW (`m_mesh.ConstPointer()`: the parent object the view is
        # of); position 0 plus the class's own pointer type is the
        # discriminator. STATICS have no self: their pos-0 out is the RESULT
        # (the factory idiom), handled by the ordinary out machinery below.
        if (not member.get('Static') and pos == 0 and ctype in selfs
                and (k in ('expr', 'out') or (k == 'fieldptr' and cls['mode'] == 'child'))):
            call_exprs.append(self_access(cls, self_expr, carg.get('Value')))
            continue
        comp = cls.get('comp')
        if (k == 'fieldptr' and comp and carg['Text'] in comp['OwnerNames']
                and ctype.replace('const ', '').rstrip('*').strip() == comp['Owner']['on']):
            # `Brep.ConstPointer()` in a component: its owner's pointer (G43)
            call_exprs.append('RH3DM_Owner()')
            continue
        if k == 'newobj':
            # `var tex = new Texture(); F(..., tex.NonConstPointer()); return tex;`:
            # the generated class's own default constructor, filled by the call
            target = BY_CS.get(carg.get('Value') or '')
            if (target is None or target['mode'] != 'pointer' or newobj is not None
                    or member.get('ReturnLocal') != carg['Text'] or not ctype.endswith('*')):
                return None, 'newobj %s' % carg['Text']
            acc = 'RH3DM_NonConst()' if (storage_owner(target).get('proxy') or target.get('comp')) else 'm_ptr'
            locals_.append('%s* %s = new %s();' % (target['gen'], carg['Text'], target['gen']))
            call_exprs.append('(%s)%s->%s' % (ctype, carg['Text'], acc))
            newobj = (target, carg['Text'])
            continue
        keyinfo = comp or cls.get('key')
        if (k == 'expr' and keyinfo and carg['Text'] in (keyinfo.get('KeyNames') or keyinfo.get('Names') or ())
                and ctype in ('int', 'unsigned int')):
            # `FaceIndex` / `m_direction`: the component's key (G43)
            call_exprs.append('(%s)%s' % (ctype, 'RH3DM_Key()' if comp else 'm_key'))
            continue
        st = dict((f, t) for t, f in cls.get('state') or [])
        if k == 'expr' and carg['Text'] in st:
            # `m_named_views`: a field the view keeps (registry 'state'; G60)
            if st[carg['Text']] != ctype:
                return None, 'state field %s -> %s' % (carg['Text'], ctype)
            call_exprs.append(carg['Text'])
            continue
        sc = cls.get('scope') if cls.get('scoped') else None
        if k == 'fieldptr' and sc and carg['Text'] == sc['field']:
            # `m_brepface.ConstPointer()` in a scoped view (G77): the component's
            # pointer resolved from (parent, m_scope), as BrepFace.ConstPointer does
            res = C_BY_NAME[sc['comp']['comp']['Const']]
            if ctype.replace('const ', '').rstrip('*').strip() != sc['comp']['on']:
                return None, 'scope pointer %s -> %s' % (carg['Text'], ctype)
            call_exprs.append('(%s)%s(m_ptr, m_scope)' % (ctype, res['name']))
            extra_fns.append(res)
            continue
        if k == 'fieldptr':
            return None, 'field pointer %s' % carg['Text']
        if holder and pos == holder_arg:
            if ctype != holder['ctype']:
                return None, 'holder ctype %s vs %s' % (ctype, holder['ctype'])
            if member.get('HolderStatic') and member['Holder'] == 'SimpleArrayPoint3d':
                # handed to a translated static afterwards: the points holder the
                # translated bodies take (RH3DM_Points wraps the ON_3dPointArray; G82)
                locals_.append('RH3DM_Points holder_out;')
                call_exprs.append('&holder_out.a')
                continue
            locals_.append(holder['local'].format(n='holder_out'))
            call_exprs.append('&holder_out')
            continue
        if k == 'nativecall':
            # `IntPtr p = GetConstOnComponentManifestPtr();` resolved by specgen to
            # one native call: emitted nested, its result typed as this parameter
            inner = carg['Call']
            ifn = C_BY_NAME.get(inner['Name'])
            if ifn is None or ifn['returns'].replace('const ', '') != ctype.replace('const ', '') \
                    or len(ifn['params']) != len(inner['Args']):
                return None, 'nested call %s' % inner['Name']
            iargs = []
            for ipos, (ia, iq) in enumerate(zip(inner['Args'], ifn['params'])):
                if (ipos == 0 and iq['type'] in selfs and not member.get('Static')
                        and (ia['Kind'] == 'expr' or (ia['Kind'] == 'fieldptr' and cls['mode'] == 'child'))):
                    iargs.append(self_access(cls, self_expr, ia.get('Value')))
                elif ia['Kind'] == 'fieldptr' and cls.get('proxy') and ia['Text'] == fp.get('FileField'):
                    iargs.append('m_model')
                elif ia['Kind'] == 'literal':
                    iargs.append('nullptr' if ia['Text'] == 'null' else ia['Text'])
                elif ia['Kind'] == 'expr' and ia['Text'] == 'IntPtr.Zero' and iq['type'].endswith('*'):
                    iargs.append('nullptr')                  # an inlined helper's IntPtr.Zero (G75)
                elif ia['Kind'] == 'helpercall' and modeled_ptr(cls, ia['Text']) and iq['type'].endswith('*'):
                    iargs.append('(%s)RH3DM_MfPtr_%s()' % (iq['type'], modeled_ptr(cls, ia['Text'])))   # G88
                else:
                    return None, 'nested call arg %s' % ia['Text']
            call_exprs.append('%s(%s)' % (inner['Name'], ', '.join(iargs)))
            extra_fns.append(ifn)
            continue
        if k == 'helpercall' and modeled_ptr(cls, carg['Text']) and ctype.endswith('*'):
            # `ConstParentDimStylePointer()` (G88): the modeled field's pointer, or null
            call_exprs.append('(%s)RH3DM_MfPtr_%s()' % (ctype, modeled_ptr(cls, carg['Text'])))
            continue
        if k == 'fillptr':                    # the object the fill idiom created
            call_exprs.append('fill_ptr')
            continue
        if k == 'expr' and carg['Text'] == 'IntPtr.Zero' and ctype.endswith('*'):
            call_exprs.append('nullptr')
            continue
        if k == 'expr' and carg['Text'] == 'Guid.Empty' and ctype == 'ON_UUID':
            call_exprs.append('ON_nil_uuid')
            continue
        if k == 'paramconv':
            # value.ToArgb(): the parameter, converted exactly as the C# call site does
            name = carg['Text']
            cstype = cs_param_modes.get(name, ('', ''))[1]
            if cstype != 'Color' or carg.get('Value') != 'ToArgb' or ctype not in ('int', 'unsigned int'):
                return None, 'paramconv %s.%s' % (name, carg.get('Value'))
            call_exprs.append('RH3DM_ColorToArgb(%s)' % name)
            continue
        if k == 'tempnative':
            # `IntPtr L = cplane.CopyToNative();`: the temporary native a data-class
            # argument builds, declared once by plan_sequence (G65)
            pcs = cs_param_modes.get(carg['Text'], ('', ''))[1]
            tn = member.get('TempNative') or {}
            if (pcs not in DATA_CPP or carg.get('Value') not in DATA_INFO[pcs]['methods']
                    or tn.get('Param') != carg['Text'] or not ctype.endswith('*')):
                return None, 'tempnative %s' % carg['Text']
            call_exprs.append('(%s)rh3dm_tmp_%s' % (ctype, carg['Text']))
            continue
        if k == 'parammember':
            # box.Min: a translated property of a value-type parameter
            pn, _, mem = carg['Text'].partition('.')
            pcs = cs_param_modes.get(pn, ('', ''))[1]
            if pcs == 'byte[]' and mem == 'Length':
                if ctype not in ('int', 'unsigned int'):
                    return None, 'bytes length -> %s' % ctype
                call_exprs.append('(%s)%s.size()' % (ctype, pn))         # G86
                continue
            if seq_elem(pcs) and mem == 'Count':
                if ctype not in ('int', 'unsigned int'):
                    return None, 'sequence count -> %s' % ctype
                call_exprs.append('(%s)%s.size()' % (ctype, pn))         # G82
                continue
            if pcs in DATA_CPP and '.' not in mem:
                # options.Version / options.RenderMeshFlags: a DATA class argument's
                # translated property or field, cast as the C# call site wrote it (G64)
                info = DATA_INFO[pcs]
                acc = data_acc.get(pn) or (pn + ('->' if pn in nullable_params else '.'))
                if mem in info['fields']:
                    expr = acc + mem
                elif mem in info['props']:
                    expr = acc + mem + '()'
                else:
                    return None, 'parammember %s' % carg['Text']
                if carg.get('Value'):
                    expr = '(%s)(%s)' % (ctype, expr)
                elif ctype == 'const RHMONO_STRING*':
                    expr = 'RH3DM_S(%s).data()' % expr
                call_exprs.append(expr)
                continue
            ret = VT_PROP_RET.get((pcs, mem))
            if ret is None and pcs in BY_CS and '.' not in mem:
                # item.Id: a generated CLASS's property, read through its own
                # generated getter (the same RhinoCommon member)
                g = generated_getter(BY_CS[pcs], mem)
                conv = g and C_FROM_BINDING.get((plan_cpp_ret(g), ctype))
                if conv is None:
                    return None, 'parammember %s' % carg['Text']
                acc = '->' if pn in nullable_params else '.'
                call_exprs.append(conv.format('%s%s%s()' % (pn, acc, g['cpp'])))
                continue
            if ret is None or '.' in mem:
                return None, 'parammember %s' % carg['Text']
            expr = '%s.%s()' % (pn, mem)
            if ret in VT_CPP.values() and ctype.endswith('*'):
                locals_.append('%s %s_%s = %s;' % (ret, pn, mem, expr))
                call_exprs.append('RH3DM_Ptr(&%s_%s)' % (pn, mem))
            elif ret in VT_CPP.values():
                call_exprs.append('RH3DM_Val(%s)' % expr)
            else:
                call_exprs.append(expr)
            continue
        if k == 'local':
            v = local_value(carg['Value'])
            if v is None:
                return None, 'local %s = %s' % (carg['Text'], carg['Value'])
            expr, vcs = v
            if vcs in VT_CPP and ctype.endswith('*'):
                locals_.append('%s %s = %s;' % (VT_CPP[vcs], carg['Text'], expr))
                call_exprs.append('RH3DM_Ptr(&%s)' % carg['Text'])
            elif vcs in VT_CPP:
                call_exprs.append('RH3DM_Val(%s)' % expr)
            else:
                call_exprs.append(expr)
            continue
        if k == 'expr' and re.match(r'^\w+$', carg['Text']) and carg['Text'] not in cs_param_modes:
            # `ComponentType`: a property of this class whose getter is a
            # constant (File3dmLayerTable overrides it: ModelComponentType.Layer)
            cv = class_const(cls['type'], carg['Text'])
            if cv is not None and cv[1] is not None:
                k, carg = 'const', {'Kind': 'const', 'Text': cv[0], 'Value': cv[1]}
                if carg['Text'].startswith('ModelComponentType.'):
                    typearg_pos.append(len(call_exprs))      # a multi-type table sums over it
        em = member.get('EnumMap')
        if k == 'expr' and em and em.get('Local') and carg['Text'] in ('(int)%s' % em['Local'], em['Local']):
            # Decal.Mapping's setter (G60): `switch (value) { case M.A: m = N.A; ... }`
            # then `F(ptr, (int)m)` -- the same switch, member by member
            if not em.get('Values') or em.get('Exception') not in EXMAP or em['Switched'] not in cs_param_modes:
                return None, 'enum map %s' % em.get('Switched')
            cases = ' '.join('case %d: rh3dm_m = %d; break;' % (a, b) for a, b in em['Values'])
            locals_.append('int rh3dm_m = 0;')
            locals_.append('switch ((int)%s) { %s default: throw %s("%s"); }'
                           % (em['Switched'], cases, EXMAP[em['Exception']], net_message(em['Exception'], em.get('Message'))))
            call_exprs.append('rh3dm_m' if ctype in ('int', 'unsigned int') else '(%s)rh3dm_m' % ctype)
            continue
        if k == 'expr' and (sm := re.match(r'^(\w+)\s*\?\?\s*(?:string\.Empty|String\.Empty|"")$', carg['Text'])) \
                and cs_param_modes.get(sm.group(1)) == ('', 'string'):
            # `rtfText ?? string.Empty`: the binding's string is never null (G76)
            k, carg = 'param', {'Kind': 'param', 'Text': sm.group(1)}
        if k == 'expr':
            # a widening/enum cast of a declared parameter is the parameter:
            # RhinoCommon spells enum args `(int)continuityType` at the call site
            mm = re.match(r'^\((int|uint|double)\)\s*(\w+)$', carg['Text'])
            if mm and mm.group(2) in cs_param_modes:
                # `(uint)count` into an unsigned int: the same cast, kept
                if (mm.group(1) == 'uint' and ctype == 'unsigned int'
                        and cs_param_modes[mm.group(2)] == ('', 'int')):
                    call_exprs.append('(unsigned int)%s' % mm.group(2))
                    continue
                k, carg = 'param', {'Kind': 'param', 'Text': mm.group(2)}
            else:
                return None, 'opaque expr arg: %s' % carg['Text']
        if k == 'const':
            if ctype.startswith('enum '):
                call_exprs.append('(%s)%s' % (ctype[5:], carg['Value']))
            elif ctype not in CVALUE and not ctype.endswith('*') and re.match(r'^[\w:]+$', ctype):
                # a C enum spelled as a type (ON_DimStyle::field): cast the baked value
                call_exprs.append('(%s)%s /*%s*/' % (ctype, carg['Value'],
                                                      carg['Text'].split('.')[-1]))
            else:
                call_exprs.append(carg['Value'])
        elif k == 'literal':
            call_exprs.append('nullptr' if carg['Text'] == 'null' else carg['Text'])
        elif k == 'arrcount':
            # an array fill's element count (plan_arrayfill)
            if ctype not in ('int', 'unsigned int'):
                return None, 'array count -> %s' % ctype
            call_exprs.append(carg['Text'] if ctype == 'int' else '(unsigned int)%s' % carg['Text'])
        elif k == 'arrindex':
            # the index of the element loop (plan_arrayfill, form loop; G72)
            if ctype != 'int':
                return None, 'array index -> %s' % ctype
            call_exprs.append('rh3dm_i')
        elif k == 'ptrlocal':
            # a pointer (or object) local the wrap-tuple shapes declare themselves
            # (plan_wraptuple; G72): Value the declaration, Text the argument
            locals_.append(carg['Value'])
            call_exprs.append(carg['Text'])
        elif k == 'arrayout':
            # the caller-allocated array the native fills (plan_arrayfill): its C
            # element type must be the C# element's (layout-equal value types)
            elem = carg.get('Value')
            scalar = {'int': 'int*', 'double': 'double*', 'float': 'float*', 'uint': 'unsigned int*'}
            ok = (scalar.get(elem) == ctype or (elem in VT_CPP and ctype.endswith('*')
                                                 and C_VT.get(ctype.replace('const ', '').strip()) == elem))
            if not ok:
                return None, 'array %s[] -> %s' % (elem, ctype)
            # value types through RH3DM_Ptr: the element size is static_assert'ed
            call_exprs.append(('%s.data()' if elem in scalar else 'RH3DM_Ptr(%s.data())') % carg['Text'])
        elif k == 'paramcond':
            # `start ? 0 : 1` on a bool parameter (G49)
            name = carg['Text']
            arms = (carg.get('Value') or '').split(':')
            if cs_param_modes.get(name) != ('', 'bool') or len(arms) != 2:
                return None, 'conditional arg on %s' % name
            call_exprs.append('(%s ? %s : %s)' % (name, arms[0], arms[1]))
        elif k == 'paramptr':
            # a generated class passed as an argument: hand the C function the
            # wrapped object's native pointer (abstract bases via their getter)
            name = carg['Text']
            cstype = cs_param_modes.get(name, ('', ''))[1]
            argcls = BY_CS.get(cstype)
            if argcls is None:
                return None, 'class arg %s: %s not in registry' % (name, cstype)
            # `x == null ? IntPtr.Zero : x.ConstPointer()`, or null-checked by an early return
            nullable = carg.get('Value') == 'nullable' or name in retnull
            acc = '->' if nullable else '.'
            if argcls['mode'] == 'abstract':
                ptr = '%s%s%s()' % (name, acc, argcls['getter'])
            elif storage_owner(argcls).get('proxy'):
                # item.ConstPointer(): a proxy resolves; item.NonConstPointer() on a
                # copy-on-write view copies first (BrepFace.SetMesh's mesh; G59). A derived
                # class's proxy is its base's (G118: AddExtrusion(file.Objects[0].Geometry)
                # passed the view's m_ptr -- null -- and added nothing)
                cow_nc = carg.get('Value') == 'NonConstPointer' and storage_owner(argcls)['proxy'].get('NonConst') == 'cow'
                ptr = '%s%s%s' % (name, acc, 'RH3DM_NonConst()' if cow_nc else 'RH3DM_Const()')
            elif argcls.get('shared'):
                # a shared_ptr handle (Decal; G60): its object is Raw(sp), no m_ptr
                ptr = '%s%s%s' % (name, acc, 'RH3DM_NonConst()' if carg.get('Value') == 'NonConstPointer' else 'RH3DM_Const()')
            elif argcls['mode'] in ('pointer', 'child'):
                ptr = '%s%sm_ptr' % (name, acc)
            elif argcls.get('fileview'):
                ptr = '%s%sRH3DM_Val()' % (name, acc)          # a file view resolves (G104)
            else:
                ptr = '&%s%sm_val' % (name, acc)
            if nullable:
                nullable_params.add(name)
                call_exprs.append('(%s ? (%s)(%s) : nullptr)' % (name, ctype, ptr))
            else:
                call_exprs.append('(%s)(%s)' % (ctype, ptr))
        elif k == 'param':
            name = carg['Text']
            if name not in cs_param_modes:
                return None, 'param %s: not a C# parameter' % name
            cstype = cs_param_modes[name][1]
            if cstype == 'byte[]':
                # a byte buffer (File3dm.FromByteArray; G86): its bytes, as the C side reads them
                if ctype.replace(' ', '') not in ('constunsignedchar*', 'constvoid*', 'unsignedchar*'):
                    return None, 'bytes %s -> %s' % (name, ctype)
                call_exprs.append('(%s)%s.data()' % (ctype, name))
                continue
            if seq_elem(cstype):
                # a sequence parameter where C# passed GetConstArray's array (G82):
                # the vector's contiguous elements, layout-equal to the C struct
                e = seq_elem(cstype)
                base = ctype.replace('const ', '').strip()
                if not ctype.startswith('const ') or not base.endswith('*') or \
                        (C_VT.get(base) != e if e in VT_CPP else base != e + '*'):
                    return None, 'sequence %s<%s> -> %s' % (name, e, ctype)
                call_exprs.append(('RH3DM_Ptr(%s.data())' if e in VT_CPP else '%s.data()') % name)
                continue
            if cstype in CS_ENUMS:
                if ctype == CS_ENUMS[cstype]['cpp']:
                    call_exprs.append(name)            # the C layer takes the public type itself
                    continue
                if re.match(r'^ON(?:_\w+)?::\w+$', ctype):
                    # an opennurbs scoped enum (G99: Font(.., FontWeight weight, ..) -> ON_Font::Weight):
                    # P/Invoke passes the value -- the same cast (the G91 rule for translated bodies)
                    call_exprs.append('(%s)(int)(%s)' % (ctype, name))
                    continue
                if ctype not in ('int', 'unsigned int') and not ctype.startswith('enum '):
                    return None, 'enum param %s -> %s unsupported' % (name, ctype)
                call_exprs.append('(%s)%s' % (ctype[5:] if ctype.startswith('enum ')
                                              else 'int', name))
            elif cstype in VT_CPP and cstype not in CS_ARG and not ctype.endswith('*'):
                call_exprs.append('RH3DM_Val(%s)' % name)
            elif cstype not in CS_ARG or ctype not in CS_ARG_CTYPES[cstype]:
                return None, 'param %s: %s -> %s unsupported' % (name, cstype, ctype)
            elif CS_ARG[cstype][1] is None:
                return None, 'param %s: %s needs a recorded conversion' % (name, cstype)
            else:
                call_exprs.append(CS_ARG[cstype][1].format(n=name))
        elif k == 'out' and carg['Text'] in cs_param_modes \
                and cs_param_modes[carg['Text']][0] not in ('out', 'ref'):
            # `ref x` where x is a BY-VALUE C# parameter (RhinoCommon passes
            # structs like Transform by ref for speed): C# refers to the
            # method's own copy, so pass the address of a local copy of the
            # argument -- never a fresh default-constructed local
            name = carg['Text']
            vcs = cs_param_modes[name][1]
            if vcs in VT_CPP and ctype.endswith('*'):
                # the binding takes value types BY VALUE: the parameter is
                # already the method's own copy, exactly C#'s `ref` on a
                # by-value struct parameter -- no local (ctors cannot have one)
                call_exprs.append('RH3DM_Ptr(&%s)' % name)
                continue
            argcls = BY_CS.get(cs_param_modes[name][1])
            if argcls is None or argcls['mode'] != 'value' or ctype.rstrip('*').replace('const ', '') != argcls['on']:
                return None, 'ref of by-value param %s (%s)' % (name, ctype)
            locals_.append(('%s %s_copy = *%s.RH3DM_Val();' if argcls.get('fileview') else '%s %s_copy = %s.m_val;') % (argcls['on'], name, name))
            call_exprs.append('&%s_copy' % name)
        elif k == 'out' and carg['Text'] in cs_param_modes and cs_param_modes[carg['Text']][0] == 'ref' \
                and cs_param_modes[carg['Text']][1] in VT_CPP and ctype.endswith('*') \
                and C_VT.get(ctype.replace('const ', '').strip()) == cs_param_modes[carg['Text']][1]:
            # a C# `ref T` parameter of a value type (G120: Mesh.SetCachedTextureCoordinates(tm, ref xf)):
            # in AND out -- the binding takes the caller's value (its own copy), the native reads and may
            # write it there, and the updated value is returned; before, a default-constructed local was
            # passed (the zero transform) and the caller's never reached the native (G4's case, one level up)
            name = carg['Text']
            call_exprs.append('RH3DM_Ptr(&%s)' % name)
            out_by_name[name] = 'VT:' + cs_param_modes[name][1]
            ref_inputs.add(name)
        elif k == 'out':
            name = carg['Text']
            # the C# type of the out local: a declared out-param, else (a
            # getter's `Plane plane; F(ptr, ref plane); return plane;`) the
            # member's own return type
            vcs = (cs_param_modes[name][1] if name in cs_param_modes
                   else sig_ret if sig_ret in VT_CPP
                   else C_VT.get(ctype.replace('const ', '')))
            vt_init = (carg.get('Value') and vcs in VT_CPP
                       and (local_value(carg['Value']) or (None, None))[1] == vcs)
            if ctype in OUTLOCAL and not (vcs in VT_CPP and vcs not in CS_ARG) and not vt_init:
                decl = OUTLOCAL[ctype][0].format(n=name)
                if carg.get('Value') and ctype in ('double*', 'int*', 'bool*', 'ON_UUID*', 'unsigned int*'):
                    # `double t = RhinoMath.UnsetValue; F(ptr, ref t)`: the C
                    # side may READ it (ref), so reproduce the initializer
                    init = local_value(carg['Value'])
                    if init is None or init[1] is not None:
                        return None, 'out %s initialized to %s' % (name, carg['Value'])
                    decl = '%s %s = %s;' % (ctype[:-1], name, init[0])
                elif carg.get('Value'):
                    return None, 'out %s initialized to %s' % (name, carg['Value'])
                locals_.append(decl)
                call_exprs.append('&' + name)
                out_by_name[name] = ctype
            elif ctype.endswith('*') and re.match(r'^ON(_\w+)?::\w+$', ctype[:-1]) and carg.get('Value') \
                    and class_const(cls['type'], carg['Value']) and class_const(cls['type'], carg['Value'])[1] is not None:
                # `ref type` fed this class's constant (`ComponentType`; G66): an enum
                # local the C side may read or write
                locals_.append('%s %s = (%s)%s;' % (ctype[:-1], name, ctype[:-1], class_const(cls['type'], carg['Value'])[1]))
                call_exprs.append('&' + name)
                out_by_name[name] = ctype
                out_const[name] = carg['Value']
            elif vcs in VT_CPP and ctype.endswith('*'):
                init = local_value(carg.get('Value')) if carg.get('Value') else None
                if carg.get('Value') and (init is None or init[1] != vcs):
                    return None, 'out %s initialized to %s' % (name, carg['Value'])
                locals_.append('%s %s = %s;' % (VT_CPP[vcs], name, init[0]) if init
                               else OUTLOCAL['VT:' + vcs][0].format(n=name))
                call_exprs.append('RH3DM_Ptr(&%s)' % name)
                out_by_name[name] = 'VT:' + vcs
            else:
                return None, 'out %s: unsupported (%s)' % (name, ctype)
        else:
            return None, 'arg kind ' + k

    bind_args, ret_outs = [], []
    holder_is_sigout = False
    for mode, cstype, name in sig_params:
        if mode == 'ref' and name in ref_inputs:
            bind_args.append((VT_CPP[cstype], name))       # in (G120) ...
            ret_outs.append((name, out_by_name[name]))     # ... and out
            continue
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
            elif cstype in BY_CS and name in nullable_params:
                bind_args.append(('const %s*' % BY_CS[cstype]['gen'], name))   # optional: None / null
            elif cstype in BY_CS:
                bind_args.append(('const %s&' % BY_CS[cstype]['gen'], name))
            elif cstype in DATA_CPP:
                # a data class (G64): by pointer when optional, else by reference
                bind_args.append(('const %s%s' % (DATA_CPP[cstype], '*' if name in nullable_params else '&'), name))
            elif cstype in VT_CPP and cstype not in CS_ARG:
                bind_args.append((VT_CPP[cstype], name))
            elif seq_cpp(cstype):
                bind_args.append(('const std::vector<%s>&' % seq_cpp(cstype), name))    # G82
            elif cstype == 'byte[]':
                bind_args.append(('const RH3DM_Bytes&', name))                         # G86
            elif cstype not in CS_ARG:
                return None, 'C# arg type %s' % cstype
            else:
                bind_args.append((CS_ARG[cstype][0], name))

    cret = cfn['returns']
    # a data class assembled from the out locals and the holder (G65):
    # `var rc = new PageSettings(); CALL(..., ref a, ..., sh); rc.A = a; ... return rc;`
    # `if (CALL) return new DateTime(year, month+1, ...); return DateTime.MinValue;` (G68):
    # the out locals make the date; the binding's own date type carries it
    wrap_cast = False
    # `public override GeometryBase Duplicate() { ... return new Brep(p, null); }` (G69):
    # declared as the base, constructed as this class -- the wrap is the class it
    # constructs, a derived generated class of the declared one
    wrap_cs = sig_ret
    if (member.get('Wrap') in BY_CS and sig_ret in BY_CS and member['Wrap'] != sig_ret
            and BY_CS[member['Wrap']]['mode'] == 'pointer' and derives_from(BY_CS[member['Wrap']], BY_CS[sig_ret])):
        wrap_cs = member['Wrap']
    datetime_shape = None
    if member.get('DateTime'):
        su = member.get('Success') or {}
        if sig_ret != 'DateTime' or cret != 'bool' or su.get('Out') != '@datetime' or su.get('Fallback') != 'DateTime.MinValue':
            return None, 'datetime form'
        for e in member['DateTime']:
            mm = re.match(r'^(\w+)(\+1)?$', e)
            if not mm or out_by_name.get(mm.group(1)) != 'int*':
                return None, 'datetime arg %s' % e
        datetime_shape = ('datetime', list(member['DateTime']))
    datafill = None
    if member.get('DataFill'):
        df = member['DataFill']
        dcs = cs_unqualify(df['Type'])
        if dcs not in DATA_CPP or sig_ret != dcs:
            return None, 'datafill %s' % df['Type']
        steps = []
        for mem, src in df['Assigns']:
            if not DATA_INFO[dcs]['props'].get(mem):
                return None, 'datafill member %s' % mem
            if src == '@holder':
                if not holder or holder['cs'] != 'string':
                    return None, 'datafill holder'
                steps.append((mem, 'RH3DM_W(holder_out)'))
            elif src in out_by_name:
                steps.append((mem, OUTLOCAL[out_by_name[src]][1].format(n=src)))
            else:
                return None, 'datafill source %s' % src
        if {src for _, src in df['Assigns']} - {'@holder'} != set(out_by_name) - {n for n, _ in ret_outs}:
            return None, 'datafill outs'          # every out local of the call lands in the object
        ret_outs = []
        if cret != 'void' and not member.get('ResultUnused'):
            return None, 'datafill result %s' % cret
        datafill = ('datafill', dcs, steps)
    if datetime_shape:
        shape = datetime_shape
    elif datafill:
        shape = datafill
    elif member.get('DataApply'):
        # `IntPtr p = CALL; if (IntPtr.Zero != p) value.SetupNativePointer(p);` (G65)
        da = member['DataApply']
        pcs = cs_param_modes.get(da['Param'], ('', ''))[1]
        if sig_ret != 'void' or pcs not in DATA_CPP or da['Method'] not in DATA_INFO[pcs]['methods'] or not cret.endswith('*'):
            return None, 'dataapply %s' % da['Param']
        acc = (('const_cast<%s*>(%s)->' if da['Param'] in nullable_params else 'const_cast<%s&>(%s).')
               % (DATA_CPP[pcs], da['Param']))
        shape = ('dataapply', acc + da['Method'])
    elif holder and member.get('HolderStatic'):
        # `using (var h = new SimpleArrayPoint3d()) { F(ptr, h.NonConstPointer());
        # return Polyline.PolyLineFromNativeArray(h); }` (G82): the data class's own
        # translated static builds the result from the filled holder
        t, _, mth = member['HolderStatic'].partition('.')
        if (ret_outs or sig_ret != t or t not in DATA_CPP or mth not in DATA_INFO[t]['methods']
                or member['Holder'] != 'SimpleArrayPoint3d'
                or (cret not in ('void', 'bool') and not member.get('ResultUnused'))):
            return None, 'holder static %s' % member['HolderStatic']
        shape = ('holderdata', t, mth)
    elif holder:
        if ret_outs:
            return None, 'holder plus other outs'
        if holder_is_sigout:
            if sig_ret != 'bool' or cret != 'bool':
                return None, 'holder sig-out needs bool/bool'
            shape = ('holdertuple', member['Holder'])
        elif member.get('HolderEmpty'):
            # `int rc = F(...); return rc == 0 ? new int[0] : h.ToArray();` (BrepVertex.EdgeIndices;
            # G73): the comparison on the C result decides emptiness, reproduced as written
            if sig_ret != holder['cs'] or cret not in ('int', 'unsigned int', 'bool') \
                    or not re.match(r'^(==|!=|<|>|<=|>=) -?\d+$', member['HolderEmpty']):
                return None, 'holder empty on %s (%s)' % (member['HolderEmpty'], cret)
            shape = ('holder', member['Holder'], member['HolderEmpty'])
        elif sig_ret == holder['cs'] and (cret in ('void', 'bool') or member.get('ResultUnused')):
            # a C status (or any value) the C# call site discards is discarded here too
            shape = ('holder', member['Holder'])
        else:
            return None, 'holder return %s vs %s' % (sig_ret, holder['cs'])
    elif newobj is not None:
        if sig_ret != newobj[0]['cs'] or (cret not in ('void', 'bool') and not member.get('ResultUnused')):
            return None, 'newobj return %s' % sig_ret
        fail = member.get('NewObjFail')
        if fail is not None:
            # `if (!CALL) { rc.Dispose(); rc = null; }`: the C status decides (G49)
            if fail != newobj[1] or cret != 'bool':
                return None, 'newobj failure %s on %s' % (fail, cret)
            fail = 'null'
        shape = ('newobj', newobj[0], newobj[1], fail)
    elif member.get('Body') == 'construct':
        shape = construct_shape(member, cls, self_expr, {n for _, n in bind_args}, cret, out_by_name)
        if isinstance(shape, str):
            return None, shape
    elif ret_outs:
        shape = ('tuple', cret == 'bool', ret_outs)
        if sig_ret != 'bool' and cret == 'bool':
            return None, 'out-params with non-bool C# return'
        if sig_ret == 'bool' and cret not in ('bool', 'int', 'unsigned int'):
            return None, 'out-params: C# bool from C %s' % cret
        if sig_ret not in ('void', 'bool'):
            # the C# result leads the tuple, as a bool status does -- never
            # dropped (G49: SegmentIndexes lost its count, the Intersection
            # methods their intersection type, ToNurbsSurface its surface)
            if sig_ret in CS_RET_DIRECT and cret == CS_RET_DIRECT[sig_ret]:
                shape = ('tuple', ('result', cret, '{c}'), ret_outs)
            elif sig_ret in CS_ENUMS and (cret in ('int', 'unsigned int') or cret.startswith('enum ')
                                          or cret == CS_ENUMS[sig_ret]['cpp']):
                cpp = CS_ENUMS[sig_ret]['cpp']
                expr = '(%s)({c})' % cpp
                if member.get('ResultGuard'):
                    expr = result_guard_expr(member, sig_ret)
                    if expr is None:
                        return None, 'result guard %s' % member['ResultGuard'].get('Cond')
                shape = ('tuple', ('result', cpp, expr), ret_outs)
            elif (sig_ret in BY_CS and member.get('Wrap') == sig_ret and BY_CS[sig_ret]['mode'] in ('pointer', 'abstract')
                  and not any(BY_CS[sig_ret].get(f) for f in ('comp', 'handle', 'borrowed', 'shared'))
                  and cret.endswith('*') and not cret.startswith('const ')
                  and (cret[:-1] == BY_CS[sig_ret]['on'] or any(c['on'] == cret[:-1] and derives_from(c, BY_CS[sig_ret]) for c in CLASSES))):
                # `IntPtr rc = F(ptr, tol, ref accuracy); return rc == Zero ? null : new T(rc, null);`
                # (Surface.ToNurbsSurface; G73): the owned object (None when null) leads the tuple
                W = BY_CS[sig_ret]
                shape = ('tuple', ('result', '%s*' % W['gen'], 'RH3DM_Wrap_%s({c})' % W['reg']), ret_outs)
            else:
                return None, 'out-params: C# result %s (C: %s)' % (sig_ret, cret)
        if member.get('ResultGuard') and not (isinstance(shape[1], tuple) and 'rh3dm_r' in shape[1][2]):
            return None, 'result guard on this shape'
        if sig_ret == 'bool' and cret in ('int', 'unsigned int'):
            # `return (success > 0);`: the comparison RhinoCommon wrote decides
            if not member.get('Compare'):
                return None, 'out-params: int status without a recorded comparison'
            if member['Compare'].startswith('in '):
                return None, 'out-params: compare %s' % member['Compare']
            shape = ('tuple', member['Compare'], ret_outs)
    elif member.get('Factory'):
        shape = factory_shape(member['Factory'], cls, sig_ret, cret, out_by_name, out_const,
                              {n for mode, t, n in sig_params if t == 'Guid' and not mode})
        if isinstance(shape, str):
            return None, shape
    elif member.get('ConstView'):
        shape = constview_shape(member['ConstView'], cls, sig_ret)
        if isinstance(shape, str):
            return None, shape
    elif sig_ret in CS_RET_DIRECT and cret == CS_RET_DIRECT[sig_ret]:
        shape = ('direct', cret)
    elif (sig_ret in CS_RET_DIRECT and member.get('ResultCast') == sig_ret and not out_by_name
          and cret in ('int', 'unsigned int', 'double')):
        # `return (int)UNM.F(...)` over an unsigned C result: the same cast
        shape = ('conv', CS_RET_DIRECT[sig_ret], '(%s)({c})' % CS_RET_DIRECT[sig_ret])
    elif (sig_ret == 'bool' and cret in ('int', 'unsigned int') and not out_by_name
          and member.get('Compare')):
        # `return rc != 0;` -- the comparison RhinoCommon wrote, recorded by specgen;
        # `return (1 == rc || 3 == rc);` arrives as "in 1,3" (G49)
        cmp = member['Compare']
        if cmp.startswith('in '):
            vals = cmp[3:].split(',')
            if not all(v.isdigit() for v in vals):
                return None, 'compare %s' % cmp
            shape = ('conv', 'bool', 'RH3DM_In(({c}), %s)' % ', '.join(vals))
        else:
            shape = ('conv', 'bool', '({c}) ' + cmp)
    elif (sig_ret in CS_RET_DIRECT and cret in ('void', 'bool')
          and [t for t in out_by_name.values()] == [CS_RET_DIRECT[sig_ret] + '*']):
        # `double d = 0; F(ptr, ref d); return d;` (the status is ignored, as in C#)
        shape = ('fromout', sig_ret, next(iter(out_by_name)))
    elif sig_ret in CS_RET_DIRECT:
        return None, 'return mismatch %s vs %s' % (sig_ret, cret)
    elif (sig_ret == 'Color' and member.get('ConvOut') and member.get('ResultConv') in ('FromArgb', 'FromWin32')
          and list(out_by_name) == [member['ConvOut']] and out_by_name[member['ConvOut']] in ('int*', 'unsigned int*')
          and cret in ('void', 'bool')):
        # `int argb = 0; F(..., ref argb); return Color.FromArgb(argb);` (G53)
        shape = ('fromoutconv', 'BND_Color', 'RH3DM_Color%s({n})' % member['ResultConv'], member['ConvOut'])
    elif (sig_ret == 'Color' and cret in ('int', 'unsigned int') and not out_by_name
          and member.get('ResultConv') in ('FromArgb', 'FromWin32')):
        # Color.FromArgb(rc) / Interop.ColorFromWin32(abgr): the recorded conversion
        shape = ('conv', 'BND_Color', 'RH3DM_Color%s({c})' % member['ResultConv'])
    elif sig_ret == 'Guid' and cret == 'ON_UUID' and not out_by_name:
        shape = ('conv', 'BND_UUID', 'ON_UUID_to_Binding({c})')
    elif sig_ret in VT_CPP and (sig_ret not in CS_RET_OUT
                                or 'VT:' + sig_ret in out_by_name.values()):
        outs = [n for n, t in out_by_name.items() if t == 'VT:' + sig_ret]
        if cret not in ('void', 'bool') or len(outs) != 1:
            return None, '%s return without single out' % sig_ret
        shape = ('fromout', 'VT:' + sig_ret if sig_ret in CS_RET_OUT else sig_ret, outs[0])
    elif sig_ret in CS_RET_OUT:
        want = CS_RET_OUT[sig_ret]
        outs = [n for n, t in out_by_name.items() if t == want]
        if cret not in ('void', 'bool') or len(outs) != 1:
            return None, '%s return without single %s out' % (sig_ret, want)
        shape = ('fromout', sig_ret, outs[0])
    elif sig_ret in CS_ENUMS and (member.get('EnumMap') or {}).get('Values') and not member['EnumMap'].get('Local'):
        # Decal.Mapping's getter (G60): `switch ((N)F(...)) { case N.A: return M.A; ... }`
        em = member['EnumMap']
        if em.get('Default') is not None and em.get('Exception') is None:
            # `default: return E.Undefined;` (Curve.ClosedCurveOrientation; G83)
            if cret not in ('int', 'unsigned int') or em.get('DefaultValue') is None:
                return None, 'enum map default on %s' % cret
            shape = ('enummap', CS_ENUMS[sig_ret]['cpp'], em['Values'], None, em['DefaultValue'])
        elif cret not in ('int', 'unsigned int') or em.get('Exception') not in EXMAP:
            return None, 'enum map on %s' % cret
        else:
            shape = ('enummap', CS_ENUMS[sig_ret]['cpp'], em['Values'], EXMAP[em['Exception']],
                     net_message(em['Exception'], em.get('Message')))
    elif sig_ret in CS_ENUMS and member.get('ResultSelect'):
        # `return value == (int)E.Clamp ? E.Clamp : E.Repeat;` (G55): every constant
        # must be a member of the returned enum (or a plain integer key)
        rs = member['ResultSelect']
        named = ENUM_NAMED.get(sig_ret) or ENUM_NAMED.get(sig_ret.split('.')[-1]) or {}
        def enum_val(t, key=False):
            t = re.sub(r'^\((int|uint)\)', '', t)
            if key and re.match(r'^-?\d+$', t):
                return t
            mm = re.match(r'^(\w+)\.(\w+)$', t)
            if mm and mm.group(1) == sig_ret.split('.')[-1] and mm.group(2) in named:
                return str(named[mm.group(2)])
            return None
        k, a, b = enum_val(rs['Key'], True), enum_val(rs['True']), enum_val(rs['False'])
        if None in (k, a, b) or cret not in ('int', 'unsigned int'):
            return None, 'result select %s' % rs
        cpp = CS_ENUMS[sig_ret]['cpp']
        shape = ('conv', cpp, '((int)({c}) == %s ? (%s)%s : (%s)%s)' % (k, cpp, a, cpp, b))
    elif sig_ret in CS_ENUMS:
        # an opennurbs enum (`ON::AnnotationType`): RhinoCommon's P/Invoke receives
        # its integer as the C# enum, so the explicit cast is the same (G53)
        if (cret not in ('int', 'unsigned int') and not cret.startswith('enum ') and cret != CS_ENUMS[sig_ret]['cpp']
                and not re.match(r'^ON(_\w+)?::\w+$', cret)):
            return None, 'enum return %s vs C %s' % (sig_ret, cret)
        shape = ('enumret', sig_ret)
    elif (member.get('SubView') and sig_ret in BY_CS and BY_CS[sig_ret]['mode'] in ('pointer', 'abstract')
          and cret.replace('const ', '').strip() == '%s*' % BY_CS[sig_ret]['on']):
        shape = ('wrap', sig_ret)          # a view, never owned: a const pointer is fine (G58)
    elif (sig_ret in DATA_CPP and member.get('Wrap') == sig_ret
          and member.get('WrapStatic') in DATA_INFO[sig_ret]['methods'] and cret.replace('const ', '').endswith('*')):
        # `T rc = T.FromIntPtr(p); return rc;` / `p == 0 ? null : T.FromConstPointer(p)`:
        # a DATA class made from the native pointer by its own translated static
        # bridge; null -> None either way (G64, G65)
        shape = ('datafrom', sig_ret, member['WrapStatic'])
    elif (sig_ret in BY_CS and BY_CS[sig_ret]['mode'] == 'abstract' and member.get('Wrap') == sig_ret
          and member.get('WrapArity') == 0 and cret.endswith('*') and not cret.startswith('const ')
          and (cret[:-1] == BY_CS[sig_ret]['on'] or any(c['on'] == cret[:-1] and derives_from(c, BY_CS[sig_ret]) for c in CLASSES)
               or cret[:-1] == 'ON_Object')):
        # an abstract root (GeometryBase) made by CreateGeometryHelper(p, null) (G69):
        # RH3DM_Wrap_<root> picks the generated class by the runtime type, or null
        shape = ('wrap', sig_ret)
        wrap_cast = cret[:-1] != BY_CS[sig_ret]['on']
    elif (wrap_cs in BY_CS and BY_CS[wrap_cs]['mode'] == 'pointer' and not BY_CS[wrap_cs].get('comp')
          and (cret == '%s*' % BY_CS[wrap_cs]['on']
               or BY_CS[wrap_cs].get('borrowed') and cret == 'const %s*' % BY_CS[wrap_cs]['on']
               # a handle made from a const pointer the C side allocated for it
               # (ON_Texture_GetFileReference: `new ON_FileReference(...)`; G69)
               or BY_CS[wrap_cs].get('handle') and cret == 'const %s*' % BY_CS[wrap_cs]['on'] and member.get('WrapArity') == 1)):
        sig_ret = wrap_cs
        if member.get('WrapStatic') == '(IntPtr, true)':
            # `new T(native(), true)`: T's (IntPtr, bool) constructor adopts p when the flag is
            # set (specgen PtrCtor "|own-if-true"; G122) -- the binding owns the result likewise
            pc = (TYPE_RECS.get(BY_CS[sig_ret]['type']) or {}).get('PtrCtor') or ''
            if not pc.endswith('|own-if-true'):
                return None, 'wrap through %s(IntPtr, true): %s' % (sig_ret, pc or 'unknown')
        elif member.get('WrapArity') == 1:
            # `new T(p)`: only T's (IntPtr) constructor says whether p is ADOPTED;
            # TextureMapping's copies (NewFromPointer), Font's keeps a managed
            # pointer it never deletes (G49)
            target = BY_CS[sig_ret]
            pc = ((TYPE_RECS.get(target['type']) or {}).get('PtrCtor') or '').split('|')[0]
            if pc.endswith('+nullthrow'):
                # the constructor throws on a null p; the binding returns None
                # instead -- the same only where the member checked p first
                if not member.get('WrapChecked'):
                    return None, 'wrap through %s(IntPtr): throws on null' % sig_ret
                pc = pc[:-len('+nullthrow')]
            handle_field = (target.get('handle') or {}).get('Field')
            # a MANAGED target (G99: Font): its (IntPtr) constructor keeps the shared pointer in its
            # field, never deleting it -- what a managed (borrowed) generated class does too
            if not (pc == 'own' or (handle_field and pc == 'field:' + handle_field)
                    or (pc == 'borrowed' and target.get('borrowed'))
                    or (target.get('managed') and pc.startswith('field:'))):
                return None, 'wrap through %s(IntPtr): %s' % (sig_ret, pc or 'unknown')
        shape = ('wrap', sig_ret)
    elif (sig_ret in BY_CS and BY_CS[sig_ret]['mode'] == 'value' and cret == '%s*' % BY_CS[sig_ret]['on']
          and member.get('WrapArity') == 1):
        # `new MeshingParameters(p)` adopting a native result, on a class stored
        # by value: only when T(IntPtr) stores p in its handle field and the
        # handle's Delete is in the C layer (copy in, then release) (G49)
        target = BY_CS[sig_ret]
        rec = TYPE_RECS.get(target['type']) or {}
        h = rec.get('Handle') or {}
        if not h or rec.get('PtrCtor') != 'field:%s' % h.get('Field') or h.get('Delete') not in C_BY_NAME:
            return None, 'adopt %s into a value: %s' % (sig_ret, rec.get('PtrCtor'))
        extra_fns.append(C_BY_NAME[h['Delete']])
        shape = ('valadopt', target, h['Delete'], bool(member.get('WrapChecked')))
    elif sig_ret == 'void' and (cret == 'void' or cret in CVALUE):
        shape = ('direct', 'void')         # a C status C# ignores is ignored here too
    else:
        return None, 'return type %s (C: %s)' % (sig_ret, cret)

    if member.get('ResultGuard') and not (shape[0] == 'tuple' and isinstance(shape[1], tuple)
                                          and 'rh3dm_r' in shape[1][2]):
        return None, 'result guard on shape %s' % shape[0]     # never dropped (G57)
    if retnull - nullable_params:
        return None, 'early return on null of non-class %s' % sorted(retnull - nullable_params)[0]
    # `if (p != IntPtr.Zero) return new T(...); if (COND) throw ...; throw ...;` (G43):
    # the conditional throws resolve like guards, emitted in the null branch
    fail_final, fail_guards = None, []
    if shape[0] == 'construct' and member.get('ChildFail'):
        cf = member['ChildFail']
        if shape[4] != 'ptr' or cf[-1].get('Cond') or any(g.get('Exception') not in EXMAP for g in cf):
            return None, 'construct failure tail'
        fail_final = cf[-1]
        fail_guards = [dict(g, fail=True) for g in cf[:-1]]
    guards = []
    for g in member.get('Guards') or []:
        if g.get('Assign'):
            # `if (direction != 0) direction = 1;`: a by-value scalar parameter,
            # normalized in place before the call, as C# does (G49)
            if (cs_param_modes.get(g['Assign'], ('', ''))[1] not in CS_RET_DIRECT
                    or cs_param_modes[g['Assign']][0] != ''
                    or not re.match(r'^-?(\d+\.?\d*|true|false)$', g.get('AssignValue') or '')):
                return None, 'parameter normalization %s = %s' % (g['Assign'], g.get('AssignValue'))
            guards.append(dict(g, assign_cpp='%s = %s' % (g['Assign'], g['AssignValue'])))
            continue
        if g.get('Exception') is not None:
            guards.append(g)
            continue
        rv = early_return_cpp(g, shape, sig_ret)
        if rv is None:
            return None, 'early return %s' % (g.get('Return') or 'void')
        g = dict(g, ret_cpp=rv)
        if g.get('SelfNull'):
            # `if (IntPtr.Zero == pConstThis) return ...`: a value member's
            # address is never null; a pointer can be (a model-backed proxy)
            if cls['mode'] == 'value':
                continue
            g['cond_cpp'] = '!(%s)' % self_access(cls, self_expr, 'ConstPointer')
        guards.append(g)

    guards += fail_guards
    commits = bool(member.get('Commits'))
    if commits and not (cls.get('proxy') or {}).get('Commit'):
        return None, 'Commit() on a class without a recorded commit'
    failthrow = member.get('FailThrow')
    ft_attached = None
    if failthrow:
        # `if (!CALL) throw new E(msg);` (optionally `if (COND) throw`): the C
        # call's bool decides. A void member is just that; a member returning
        # its out value (`... ref rc)) {...} return rc;`) throws, then returns it
        if cfn['returns'] != 'bool' or failthrow['Exception'] not in EXMAP:
            return None, 'fail-throw shape'
        if any(g.get('Exception') not in EXMAP or not g.get('Cond') for g in failthrow.get('More') or []):
            return None, 'fail-throw: further throws'
        if shape == ('direct', 'void'):
            shape = ('failthrow', failthrow)
        elif shape[0] == 'fromout' and not member.get('Success'):
            ft_attached = failthrow
        elif shape[0] == 'holder' and not failthrow.get('Cond'):
            ft_attached = failthrow          # `if (!F(..., holder)) throw E; return holder.ToString();`
        else:
            return None, 'fail-throw shape'

    if commits and not (shape == ('direct', 'void') or shape[0] == 'failthrow'):
        return None, 'Commit() after a non-void shape'
    success = member.get('Success')
    if success and shape[0] == 'datetime':
        success = None                        # folded into the shape (G68)
    elif success and shape[0] == 'fromoutconv':
        # `if (!F(..., ref argb)) return Color.Empty;` -- Color.Empty is (0,0,0,0),
        # the binding's FromArgb(0) (G53)
        if success['Out'] != shape[3] or cret != 'bool' or success.get('Fallback') not in (
                'Color.Empty', 'System.Drawing.Color.Empty'):
            return None, 'success form vs shape'
        success = dict(success, fb_cpp='RH3DM_ColorFromArgb(0)')
    elif success:
        if shape[0] != 'fromout' or success['Out'] != shape[2]:
            return None, 'success form vs shape'
        fb = success['Fallback']
        if fb != success['Out']:
            known = local_value(fb)                # Interval.Unset -> BND_IntervalX::Unset()
            # `if (!F(ptr, ref d)) d = 0;`: a literal suits a plain C value return
            literal_ok = known is not None and known[1] is None and sig_ret in CS_RET_DIRECT
            if known is None or (known[1] != sig_ret and not literal_ok):
                return None, 'fallback %s' % fb
    adopt = None
    if member.get('Adopt'):
        # BrepFace.SetMesh (G59): the argument's private copy goes to the native,
        # then the argument becomes a view of it through the holder
        ad = member['Adopt']
        pcls = BY_CS.get(cs_param_modes.get(ad['Param'], ('', ''))[1])
        host = storage_owner(pcls) if pcls else None
        if (shape != ('direct', 'bool') or pcls is None or not host.get('comp_host')
                or (host.get('proxy') or {}).get('NonConst') != 'cow' or ad['Param'] in nullable_params
                or any(a != 'this' and a not in cs_param_modes for a in ad['HolderArgs'])):
            return None, 'adopt %s' % ad['Param']
        adopt = {'param': ad['Param'], 'holder': ad['Holder'], 'args': ad['HolderArgs']}
    if member.get('SubView'):
        # a VIEW inside this object (G58): never wrapped as an owned pointer
        sv = member['SubView']
        target = BY_CS.get(sig_ret)
        keys = [a for a in sv['Args'] if a != 'this']
        if (not sv.get('Verified') or shape[0] != 'wrap' or target is None or target['mode'] not in ('pointer', 'abstract')
                or len(keys) > 1 or locals_ or member.get('Static')):
            return None, 'sub-view %s' % sv.get('Form')
        hosts = [c for c in CLASSES if c['mode'] == 'pointer' and (c is target or _derives_from(c, target))
                 and storage_owner(c).get('comp_host') and not c.get('comp')]
        if not hosts:
            return None, 'sub-view: no generated class hosts views of %s' % sig_ret
        if sv['Write'] == 'copy' and any(not (storage_owner(c).get('proxy') or {}).get('Dup') for c in hosts):
            return None, 'sub-view: copy-on-write without a recorded duplicate'
        ktype = None
        if keys:
            kb = [t for t, n in bind_args if n == keys[0]]
            if not kb or not (kb[0] in ('int', 'unsigned int') or kb[0] in VT_ENUMS.values()
                              or any(kb[0] == e['cpp'] for e in CS_ENUMS.values())):
                return None, 'sub-view key %s' % keys[0]
            ktype = kb[0]
        nc_exprs = None
        if sv['Write'] == 'through':
            # NonConstPointer: the same native on the owner's writable pointer
            ncm = dict(member, SubView=None, Calls=[dict(member['Calls'][0], Args=[
                dict(a, Value='NonConstPointer') if i == 0 and a.get('Value') == 'ConstPointer' else a
                for i, a in enumerate(member['Calls'][0]['Args'])])])
            ncp, why = plan(ncm, cfn, cls)
            if ncp is None or ncp['call_exprs'] == call_exprs:
                return None, 'sub-view write-through: %s' % (why or 'no writable self')
            nc_exprs = ncp['call_exprs']
        shape = ('subview', target, keys[0] if keys else None, ktype, sv['Write'], nc_exprs)
    return {'m': member, 'fn': cfn, 'cls': cls,
            'static': bool(member.get('Static')),
            'call_exprs': call_exprs, 'locals': locals_,
            'bind_args': bind_args, 'shape': shape, 'extra_fns': extra_fns,
            'guards': guards, 'success': success, 'failthrow': ft_attached, 'adopt': adopt, 'wrap_cast': wrap_cast,
            'typearg_pos': typearg_pos, 'commits': commits, 'fail_final': fail_final}, None


def proxy_id_field(cls):
    fp = (TYPE_RECS.get(cls['type']) or {}).get('FileProxy') or {}
    return fp.get('IdField')


def constview_shape(cv, cls, sig_ret):
    """`p = GetGeometryConstPointer(); if (cache stale) cache = CreateGeometryHelper(p, this);
    return cache;` on a File3dm proxy: the result is a VIEW of (this file, this
    id) whose class has that very resolver. CreateGeometryHelper picks the
    most-derived geometry class at run time (RH3DM_View_<root>); `new T(this)`
    names the class. The cache is not reproduced: a view resolves on every
    access, which is what the cache guaranteed."""
    if not cls.get('proxy') or not cv.get('Native'):
        return 'constview on a non-proxy'
    fp = (TYPE_RECS.get(cls['type']) or {}).get('FileProxy') or {}
    a = cv['Native']['Args']
    if not (len(a) == 2 and a[0]['Kind'] == 'fieldptr' and a[0]['Text'] == fp.get('FileField')
            and a[1]['Kind'] == 'expr' and a[1]['Text'] == fp.get('IdField')):
        return 'constview resolver args'
    target = BY_CS.get(cv['New'] or sig_ret)
    if target is None:
        return 'constview target %s' % (cv['New'] or sig_ret)
    if cv['New'] is None:
        # CreateGeometryHelper: an abstract root whose derived classes carry the view
        if target['mode'] != 'abstract':
            return 'constview root %s' % target['cs']
        views = [c for c in CLASSES if c.get('proxy') and c['proxy'].get('Parent') == cls['cs']
                 and derives_from(c, target)]
        if not views or any(c['proxy']['Resolver'] != cv['Native']['Name'] for c in views):
            return 'constview resolver mismatch'
        return ('constview', target, cv['Native']['Name'], True)
    if (target.get('proxy') or {}).get('Resolver') != cv['Native']['Name'] or target['proxy'].get('Parent') != cls['cs']:
        return 'constview resolver mismatch'
    return ('constview', target, cv['Native']['Name'], False)


def construct_owner(cls, text, kind, owner_cls, self_expr):
    """the owner pointer a construction passes, in this class's terms, or None:
    `this` when this class IS the owner, or a field that holds the owner -- a
    list's parent (`m_brep`), a keyed child's owner, a component's owner
    (`Brep`, `ParentSubD`: its siblings share it)"""
    if kind == 'this':
        if cls is not owner_cls or cls['mode'] != 'pointer':
            return None
        # as a child view takes its parent: writes through the view land in the owner
        return self_access(cls, self_expr, 'NonConstPointer')
    if kind != 'field':
        return None
    rec = TYPE_RECS.get(cls['type']) or {}
    if cls['mode'] == 'child' and BY_REG.get(cls.get('parent', '')) is owner_cls \
            and (text in (rec.get('ParentFields') or []) or text in ((cls.get('key') or {}).get('OwnerNames') or [])):
        return 'm_ptr'
    if cls.get('comp') and cls['comp']['Owner'] is owner_cls and text in cls['comp']['OwnerNames']:
        return 'RH3DM_Owner()'
    return None


def construct_shape(member, cls, self_expr, argnames, cret, out_by_name):
    """Body "construct" (specgen): the member returns a COMPONENT it makes from
    (owner, key) -- `new BrepFace(index, m_brep)`, `new SubDVertex(ParentSubD,
    p, id)` after `if (p != IntPtr.Zero)`, `new PointCloudItem(this, index)` --
    or takes one from a list's indexer (`return Brep.Faces[index];`). Returns
    ('construct', target, owner, key, mode, check) / ('via', list, owner, check),
    or a refusal (str). mode: 'ptr' (the native returned the component's
    pointer: only its non-null-ness is used, the key re-resolves it), 'key'
    (the native returned the key), None (no native call)."""
    kinds, args = member.get('ChildArgKinds') or [], member.get('ChildArgs') or []
    check = member.get('ChildCheck')
    # `IntPtr p = F(..); if (IntPtr.Zero != p) return new Mesh(p, null); return null;`
    # (Mesh.CreateFromSubDControlNet; G86): not a component -- the new object, wrapped
    tgt = BY_CS.get(member.get('Child') or '')
    if (tgt is not None and tgt['mode'] == 'pointer' and not tgt.get('comp') and (tgt.get('proxy') or {}).get('Origin') != 'parent'
            and kinds == ['result', 'const'] and args[1:] == ['null'] and check == 'null'
            and cret == '%s*' % tgt['on'] and cs_unqualify(member.get('Returns') or '') == member['Child']):
        return ('wrap', member['Child'])
    if member.get('ChildVia'):
        if len(args) != 2 or kinds[1] != 'result' or check != 'neg' or cret not in ('int', 'unsigned int'):
            return 'via form'
        via = member['ChildVia']
        if via == 'this':
            lst, owner = cls, 'this'
            if cls['mode'] != 'child':
                return 'via this on a non-list'
        else:
            # `Brep.Faces[index]`: the owner's list property, a generated child
            ocls = None
            if kinds[0] == 'field' and cls.get('comp') and args[0] in cls['comp']['OwnerNames']:
                ocls, owner = cls['comp']['Owner'], 'RH3DM_Owner()'
            if ocls is None and kinds[0] == 'this' and cls.get('comp'):
                # `return Loops[index];` on a component: its own scoped list (G77)
                prop = [x for x in PROPS_BY_TYPE.get(cls['type'], []) if x['Name'] == via]
                lst = child_view(prop[0]) if prop else None
                if lst and (lst.get('scope') or {}).get('comp') is cls:
                    return ('via', lst, 'RH3DM_Owner(), (int)RH3DM_Key()', check)
                return 'via list %s' % via
            if ocls is None:
                return 'via owner %s' % args[0]
            prop = [x for x in PROPS_BY_TYPE.get(ocls['type'], []) if x['Name'] == via]
            lst = child_view(prop[0]) if prop else None
            if lst is None or lst.get('parent') != ocls['reg']:
                return 'via list %s' % via
        return ('via', lst, owner, check)
    target = BY_CS.get(cs_unqualify(member.get('Child') or ''))
    if target and target['mode'] == 'child' and not target.get('key') and target.get('parent') == cls['reg']:
        # a plain child this class makes (`new File3dmDisplacement(m_attr)`): from this
        # object itself, or from the owner field it shares the pointer with
        rec = TYPE_RECS.get(cls['type']) or {}
        if len(args) != 1 or target['on'] != (cls['on'] if cls['mode'] in ('pointer', 'child') else None):
            return 'construct child args'
        if kinds[0] == 'this':
            owner = self_access(cls, self_expr, 'NonConstPointer')
        elif kinds[0] == 'field' and cls['mode'] == 'child' and args[0] in (rec.get('ParentFields') or []):
            owner = 'm_ptr'
        else:
            return 'construct child owner %s' % args[0]
        if cret is None:
            return ('construct', target, owner, None, None, check)
        if check == 'true' and cret == 'bool':
            return ('construct', target, owner, None, 'check', check)   # `if (!F(...)) return null;`
        if check is None:
            # `F(...); return m_x ?? (m_x = new X(m_attr));` (G121: File3dmMeshModifiers.CreateShutLining):
            # the native's result unused, as C# leaves it -- call, then the child
            return ('construct', target, owner, None, 'call', check)
        return 'construct child after a call'
    if (target and target.get('managed') and kinds == ['result'] and check == 'null' and cret
            and cret.replace('const ', '').strip() == '%s*' % target['on']
            and ((TYPE_RECS.get(target['type']) or {}).get('PtrCtor') or '').startswith('field:')):
        # `p = F(..); if (p != IntPtr.Zero) return new Font(p); return null;` (G99): the shared
        # managed object, wrapped (never owned); null -> None
        return ('wrap', target['cs'])
    pv = (target or {}).get('proxy') or {}
    if pv.get('Origin') == 'parent' and pv.get('NoIndex'):
        # `m_viewport ?? (m_viewport = new ViewportInfo(this))` (ViewInfo.Viewport; G89): the
        # view of this object's sub-object, made once and held (the C# cache field)
        if pv['Owner'] != cls['reg'] or kinds != ['this'] or cret is not None or not member.get('ChildCache'):
            return 'construct view of %s' % pv['Owner']
        return ('cachedview', target, member['ChildCache'])
    if pv.get('Origin') == 'parent':
        # `if (index >= 0) return new Texture(index, this); return null;` (Material.GetTexture):
        # a view of (this, index), copy-on-write on its first edit (G57)
        if pv['Owner'] != cls['reg'] or sorted(kinds) != ['result', 'this'] or check != 'neg' \
                or cret not in ('int', 'unsigned int'):
            return 'construct view of %s' % pv['Owner']
        return ('construct', target, 'this', None, 'key', check)
    info = target and (target.get('comp') or target.get('key'))
    if not info:
        return 'construct target %s' % member.get('Child')
    roles = info['Ctor']
    if len(roles) != len(args):
        return 'construct arity'
    by = {r: (a, k) for r, a, k in zip(roles, args, kinds)}
    owner_cls = target['comp']['Owner'] if target.get('comp') else BY_REG[target['parent']]
    owner = construct_owner(cls, by['owner'][0], by['owner'][1], owner_cls, self_expr)
    if owner is None:
        return 'construct owner %s (%s)' % by['owner']
    ktype = info.get('Key') or info.get('Type')
    ktext, kkind = by['key']
    mode = None
    if 'ptr' in by:
        # the native returned the component pointer, null-checked first
        if by['ptr'][1] != 'result' or check != 'null' or not cret.endswith('*'):
            return 'construct pointer'
        mode = 'ptr'
    if kkind == 'param' and ktext in argnames and ktype == 'ON_UUID':
        key = 'Binding_to_ON_UUID(%s)' % ktext            # a public Guid parameter (G98)
    elif kkind == 'param' and ktext in argnames:
        key = '(%s)%s' % (ktype, ktext)
    elif kkind == 'const' and re.match(r'^\d+$', ktext):
        key = ktext
    elif kkind == 'out' and out_by_name.get(ktext) in ('unsigned int*', 'int*'):
        key = '(%s)%s' % (ktype, ktext)
    elif kkind == 'keymap' and cls.get('scoped') and member.get('KeyMap'):
        # the scoped indexer's element (G77): F(scope pointer, index) while the
        # component resolves, the index itself otherwise -- as RhinoCommon's loop
        km, sc = member['KeyMap'], cls['scope']
        fn, res = C_BY_NAME.get(km['Native']), C_BY_NAME[sc['comp']['comp']['Const']]
        if (fn is None or km['Field'] != sc['field'] or km['Param'] not in argnames or fn['returns'] != 'int'
                or [q['type'].replace('const ', '') for q in fn['params']] != ['%s*' % sc['comp']['on'], 'int']):
            return 'construct key map %s' % km.get('Native')
        key = '(%s)([&]() { auto rh3dm_sp = %s(m_ptr, m_scope); return rh3dm_sp ? %s(rh3dm_sp, %s) : %s; }())' % (
            ktype, res['name'], fn['name'], km['Param'], km['Param'])
    elif kkind == 'result' and mode is None and cret in ('int', 'unsigned int'):
        key, mode = None, 'key'                       # the call itself
    else:
        return 'construct key %s (%s)' % (ktext, kkind)
    if mode is None and cret is not None:
        return 'construct after an unrelated call'
    return ('construct', target, owner, key, mode, check)


def derives_from(c, base):
    b = BY_REG.get(c.get('base', ''))
    while b is not None:
        if b is base:
            return True
        b = BY_REG.get(b.get('base', ''))
    return False


def factory_shape(fac, cls, sig_ret, cret, out_by_name, out_const=None, guid_params=()):
    """`if (p == IntPtr.Zero) return null; return Instantiate(m_parent, type,
    id, index);` -- the switch factory's case for this class's constant
    (`type` is ComponentType), which must construct the member's own return
    type as a File3dm proxy from the Guid the native call wrote and the
    table's File3dm. Returns the shape, or a refusal reason (str)."""
    sw = fac['Args'][fac['SwitchArg']]
    value = None
    if sw['Kind'] == 'expr' and sw['Text'] in (out_const or {}):
        # the switch is on an out local the native may write (FindId's `ref type`):
        # a table of ONE type gets back that type or nothing, so its constant decides (G66)
        cv = class_const(cls['type'], out_const[sw['Text']])
        value = cv[1] if cv else None
    elif sw['Kind'] == 'expr':
        cv = class_const(cls['type'], sw['Text'])
        value = cv[1] if cv else None
    elif sw['Kind'] == 'const':
        value = sw.get('Value')               # FindName<Layer>: the type map's constant
    if value is None:
        return 'factory switch %s' % sw['Text']
    case = (fac.get('Cases') or {}).get(value)
    if case is None:
        return 'factory case %s' % value
    target = BY_CS.get(case['New'])
    if target is None or case['New'] != sig_ret or not target.get('proxy'):
        return 'factory constructs %s' % case['New']
    if not cret.endswith('*') or len(case['Args']) != 2:
        return 'factory result'
    idl, parent = case['Args']
    if out_by_name.get(idl) == 'ON_UUID*':
        pass                                   # the id the native wrote (FindIndex)
    elif idl in guid_params:
        idl = 'Binding_to_ON_UUID(%s)' % idl   # the id asked for (FindId; G66)
    else:
        return 'factory id %s' % idl
    # the File3dm the proxy refers to: the table's own (its pointer is the model)
    if parent != 'm_parent' or cls.get('parent') != 'File3dmX' or cls['on'] != 'ONX_Model':
        return 'factory parent %s' % parent
    return ('proxywrap', target, idl)


# a generated getter's C++ return -> the C parameter type it feeds: how to convert
C_FROM_BINDING = {('BND_UUID', 'ON_UUID'): 'Binding_to_ON_UUID({})',
                  ('double', 'double'): '{}', ('int', 'int'): '{}', ('bool', 'bool'): '{}',
                  ('unsigned int', 'unsigned int'): '{}'}


def plan_cpp_ret(p):
    """the C++ type a planned zero-argument getter returns (the shapes a
    getter feeding another call can have), or None"""
    sh = p['shape']
    if sh[0] in ('direct', 'conv'):
        return sh[1]
    if sh[0] == 'fromout':
        return CS_RET_CPP.get(sh[1])
    return None


def generated_getter(cls, name):
    """the plan of class `cls`'s (or a registry base's) generated zero-argument
    property `name`; classes are planned in CLASSES order, so only an earlier
    class can be asked"""
    c = cls
    while c is not None:
        for p in PLANNED.get(c['reg'], []):
            if (p['m']['Kind'] == 'property' and p['m']['Name'] == name and not p['bind_args']
                    and not p.get('setter_of')):
                return p
        c = BY_REG.get(c.get('base', ''))
    return None


def no_owned_pointer_cpp(cls, ident):
    """`IsDocumentControlled` where the nearest PORTABLE declaration in the
    RhinoCommon chain is CommonObject's `IntPtr.Zero == m_ptr` (specgen
    Const "@NoOwnedPointer"): the generated storage answers it exactly -- a
    proxy owns nothing, an owned object always has its pointer, a value
    never is one. Abstract bases ask the derived class (RH3DM_NoOwnedPtr)."""
    for t in [cls['type']] + [b for b, _ in base_chain(cls['type'])]:
        decl = [x for x in PROPS_BY_TYPE.get(t, []) if x['Name'] == ident]
        if decl:
            if decl[0].get('Const') != '@NoOwnedPointer':
                return None
            if cls['mode'] == 'abstract':
                NO_OWNED_ABSTRACT.add(cls['reg'])
                return 'RH3DM_NoOwnedPtr()'
            # an owned object never is document-controlled in rhino3dm: a pointer
            # class with no proxy/view state, no component, not borrowed or a handle
            # (decided with Luis 2026-10-13, G57)
            if (cls['mode'] == 'pointer' and not storage_owner(cls).get('proxy') and not cls.get('comp')
                    and not cls.get('borrowed') and not cls.get('handle')):
                return 'false'
            if cls['mode'] in ('pointer', 'child'):
                return '(m_ptr == nullptr)'
            return 'false'
    return None


def _optional_idiom(name):
    """G27's optional class argument as the translator writes it for a reference parameter:
    `attributes == null ? IntPtr.Zero : attributes.ConstPointer()` -> `((false) ? nullptr : LAMBDA(&name))`"""
    return re.compile(r'\(\(false\) \? nullptr : (\(\[\]\(const BND_\w+\* o\) \{[^{}]*\}\))\(&%s\)\)' % re.escape(name))


def hand_fallback(cls, member, body):
    """G119: a registry HAND_FALLBACKS entry for this member replaces the body's terminal
    `throw` of the named exception with the hand call; the throw must be there exactly once
    (a body that changed upstream stops generation rather than losing the hand path)"""
    for fb in HAND_FALLBACKS.get(cls['reg'], []):
        if fb['member'] != member['Name'] or [cs_unqualify(q['Type']) for q in member.get('Params') or []] != fb['params']:
            continue
        thr = re.findall(r'throw std::\w+\("%s"\);' % re.escape(fb['throws']), body)
        if len(thr) != 1:
            raise SystemExit('HAND_FALLBACKS %s.%s: expected one `throw %s` in the body, found %d'
                             % (cls['reg'], fb['member'], fb['throws'], len(thr)))
        cls.setdefault('hand_headers', set()).add(fb['header'])
        return body.replace(thr[0], 'return %s;' % fb['call'])
    return body


def nullable_class_param(tp, name, body, arg_uses):
    """(type, body): `const BND_X& name` -> `const BND_X* name` when every use of the name in the body
    is `RH3DM_ClassArg(name)` passed to a pointer parameter, or G27's optional idiom, whose null
    test is then restored (G117: File3dmObjectTable.Add(item, attributes) / AddLeader -- C# passes
    null on; G27 made the pattern-planned Add* take None the same way)"""
    if tp is None or not re.match(r'^const BND_\w+\s*&$', tp.strip()):
        return tp, body
    uses = len(re.findall(r'(?<![\w.>])%s\b' % re.escape(name), body))
    fwd = 'RH3DM_ClassArg(%s)' % name
    ptr = sum(1 for a, isptr in arg_uses if a == fwd and isptr)
    idiom = len(_optional_idiom(name).findall(body))
    if uses == 0 or uses != ptr + idiom or any(a == fwd and not isptr for a, isptr in arg_uses):
        return tp, body
    body = _optional_idiom(name).sub(lambda m: '((%s == nullptr) ? nullptr : %s(%s))' % (name, m.group(1), name), body)
    return tp.strip()[:-1].strip() + '*', body


def namespace_enum_named(key):
    """ENUM_NAMED for a namespace-level enum the manifest records under its namespace
    (G117: `ObjectType` is `DocObjects.ObjectType` there); a type-nested enum of the same
    short name (`FileFbxWriteOptions.ObjectType`) never matches. None unless exactly one."""
    if '.' in key:
        return None
    hits = [k for k in ENUM_NAMED if k.endswith('.' + key)
            and k.split('.')[-2] not in SIMPLE_TYPES and k.split('.')[-2] not in BY_CS]
    return ENUM_NAMED[hits[0]] if len(hits) == 1 else None


def guid_operands(cond, ptypes, guid_calls):
    """a guard's Guid operands read through RH3DM_ToUuid (G114): Python's BND_UUID is a
    py::object, whose == / != compare object identity, so `DimensionStyleId != value`
    was always true. ptypes: parameter name -> C# type; guid_calls: the zero-argument
    Guid getters the condition calls (`Gen_DimensionStyleId`)."""
    for c in sorted(guid_calls, key=len, reverse=True):
        cond = re.sub(r'(?<![\w.])%s\(\)' % re.escape(c), 'RH3DM_ToUuid(%s())' % c, cond)
    for n, t in ptypes.items():
        if (t or '').split('.')[-1] == 'Guid':
            cond = re.sub(r'(?<![\w.])%s\b(?!\s*\()' % re.escape(n), 'RH3DM_ToUuid(%s)' % n, cond)
    return cond


def guard_calls_cpp(g, cond, cls, argnames, ptypes, p):
    """replace each helper call of a guard condition with its native call;
    arguments: the member's own parameters, or constants (a class constant,
    an enum member) cast to the C parameter type. None: refuse."""
    native = g.get('Native') or []
    if len(native) != len(g['Calls']):
        return None
    for call, nat in zip(g['Calls'], native):
        fn = C_BY_NAME.get(nat['Name'])
        if fn is None or len(fn['params']) != len(nat['Args']):
            return None
        args = []
        selfs = self_ptr_types(cls)
        for pos, (a, q) in enumerate(zip(nat['Args'], fn['params'])):
            ctype = q['type']
            if a['Kind'] in ('param', 'expr') and a['Text'] in argnames:
                cs = ptypes.get(a['Text'])
                args.append('(%s)%s' % (ctype, a['Text']) if cs in CS_ENUMS else a['Text'])
                continue
            if pos == 0 and ctype in selfs and a['Kind'] == 'expr' and not p.get('static'):
                # a native guard's own self pointer (`const_ptr_mesh`; G68)
                args.append(self_access(cls, self_expr_of(cls), 'ConstPointer'))
                g.setdefault('consumed', set()).add(a['Text'])
                continue
            cv = class_const(cls['type'], a['Text']) if a['Kind'] == 'expr' else None
            val = cv[1] if cv else (a.get('Value') if a['Kind'] == 'const' else None)
            if val is None or ctype.endswith('*'):
                return None
            args.append('(%s)%s' % (ctype, val))
        text = '%s(%s)' % (call['Name'], ', '.join(x['Text'] for x in call['Args']))
        if text not in re.sub(r'\s+', ' ', cond).replace('( ', '(').replace(' )', ')'):
            return None
        cond = cond.replace(text, '%s(%s)' % (nat['Name'], ', '.join(args)))
        p.setdefault('extra_fns', []).append(fn)
    return cond


def self_identity_cpp(g, cond, cls, argnames, ptypes):
    """`X.ConstPointer() == ConstPointer()` (either order, either pointer method) for
    a class-typed parameter X: the argument's native pointer against self's. Every
    call the guard holds must be one of the two; the rest is refused (None)."""
    m = (re.search(r'(\w+)\.(?:Const|NonConst)Pointer\(\)\s*==\s*(?:Const|NonConst)Pointer\(\)', cond)
         or re.search(r'(?:Const|NonConst)Pointer\(\)\s*==\s*(\w+)\.(?:Const|NonConst)Pointer\(\)', cond))
    if m is None or m.group(1) not in argnames:
        return None
    name = m.group(1)
    argcls = BY_CS.get(ptypes.get(name, ''))
    if argcls is None or any(argcls.get(f) for f in ('comp', 'handle', 'borrowed', 'shared')):
        return None
    # the argument's native pointer, as plan() hands it to a C function (paramptr)
    if argcls['mode'] == 'abstract':
        ptr = '%s->%s()' % (name, argcls['getter'])
    elif storage_owner(argcls).get('proxy'):
        ptr = '%s->RH3DM_Const()' % name                       # a derived class's proxy is its base's (G118)
    elif argcls['mode'] in ('pointer', 'child'):
        ptr = '%s->m_ptr' % name
    else:
        return None
    if not all(c['Name'] in ('%s.ConstPointer' % name, '%s.NonConstPointer' % name, 'ConstPointer', 'NonConstPointer') and not c['Args']
               for c in g['Calls']):
        return None
    g['Calls'] = []
    return cond[:m.start()] + '(%s == %s)' % (ptr, self_access(cls, self_expr_of(cls), 'ConstPointer')) + cond[m.end():]


def self_ptr_types(cls):
    """the C pointer types that are this class's self (plan()'s `selfs`)"""
    selfs = {'const %s*' % cls['on'], '%s*' % cls['on']}
    for extra in cls.get('self_extra', ()):
        selfs |= {'const %s*' % extra, '%s*' % extra}
    b = BY_REG.get(cls.get('base', ''))
    while b is not None:
        selfs |= {'const %s*' % b['on'], '%s*' % b['on']}
        b = BY_REG.get(b.get('base', ''))
    return selfs


def self_expr_of(cls):
    """plan()'s self_expr for a class"""
    basecls = BY_REG.get(cls.get('base', ''))
    concrete_base = basecls if basecls and basecls['mode'] != 'abstract' else None
    if cls['mode'] == 'abstract':
        return '%s()' % cls['getter']
    if cls['mode'] in ('pointer', 'child'):
        return 'static_cast<%s*>(m_ptr)' % cls['on'] if concrete_base else 'm_ptr'
    return value_self(cls)


def class_const_cpp(cls, ident):
    """`ComponentType` on File3dmLayerTable: the constant, typed as the enum
    the bindings register (`(RH3DM_ModelComponentType)5`), or None"""
    cv = class_const(cls['type'], ident)
    if cv is None or cv[1] is None:
        return None
    enum = cv[0].split('.')[0]
    if enum not in CS_ENUMS:
        return None
    GUARD_ENUMS.add(enum)
    return '(%s)%s' % (CS_ENUMS[enum]['cpp'], cv[1])


def enum_isdefined_cpp(cond, cls, ptypes):
    """`Enum.IsDefined(typeof(E), x)` exactly: true for a class constant of E
    (a named member by construction), a membership test over E's declared
    values for an E-typed parameter; anything else None (refuse)"""
    def rep(mm):
        e, x = mm.group(1), mm.group(2)
        if x.startswith(e + '.') and x.count('.') == 1:
            # a named member of E (ModelComponentType.Layer): defined by construction
            if ENUM_VALUES.get(e) is not None and e in CS_ENUMS:
                return 'true'
            raise KeyError(x)
        cv = class_const(cls['type'], x)
        if cv is not None and cv[0].startswith(e + '.') and cv[1] is not None:
            return 'true'
        if ptypes.get(x) == e and e in CS_ENUMS and ENUM_VALUES.get(e):
            return '(%s)' % ' || '.join('(int)%s == %s' % (x, v) for v in ENUM_VALUES[e])
        raise KeyError(x)
    try:
        return re.sub(r'\bEnum\.IsDefined\s*\(\s*typeof\s*\(\s*(\w+)\s*\)\s*,\s*(\w+(?:\.\w+)?)\s*\)', rep, cond)
    except KeyError:
        return None


def enum_const_value(t, enum):
    """`E.Member` (or a bare integer) as the integer C# gives it, for enum E"""
    if re.match(r'^-?\d+$', t):
        return t
    mm = re.match(r'^(\w+)\.(\w+)$', t)
    named = ENUM_NAMED.get(enum) or ENUM_NAMED.get(enum.split('.')[-1]) or {}
    if mm and mm.group(1) == enum.split('.')[-1] and mm.group(2) in named:
        return str(named[mm.group(2)])
    return None


def result_guard_expr(member, sig_ret):
    """`int rc = F(...); if (rc <= 0 || rc > 3) return E.None; return (E)rc;`
    as one C++ expression over the call (a str.format template on {c}), or
    None (refuse). The condition reads only the result local and literals (G57)."""
    rg = member['ResultGuard']
    local = rg['Ids'][0]
    cpp = CS_ENUMS[sig_ret]['cpp']
    v = enum_const_value(rg.get('Return') or '', sig_ret)
    if v is None or not re.match(r'^[\w\s<>=!|&()-]+$', rg['Cond']):
        return None
    if set(re.findall(r'[A-Za-z_]\w*', rg['Cond'])) - {local}:
        return None
    cond = re.sub(r'\b%s\b' % re.escape(local), 'rh3dm_r', rg['Cond'])
    return '[&]{{ int rh3dm_r = (int)({c}); return (%s) ? (%s)%s : (%s)rh3dm_r; }}()' % (cond, cpp, v, cpp)


def early_return_cpp(g, shape, sig_ret):
    """the C++ value of an early `return X;` for this member's return shape,
    or None (refuse): void, literals of the member's own type, String.Empty,
    null for a wrapped class, an enum constant (by its recorded value), a
    translated value-type static (Point3d.Unset)"""
    r = g.get('Return')
    kind = shape[0]
    if r == '':
        return '' if kind == 'direct' and shape[1] == 'void' else None
    if r in ('true', 'false'):
        return r if sig_ret == 'bool' and kind in ('direct', 'conv', 'fromout') else None
    if re.match(r'^-?\d+(\.\d*)?$', r):
        return r if sig_ret in ('int', 'double', 'uint') and kind in ('direct', 'fromout') else None
    if r == 'null':
        return 'nullptr' if kind in ('wrap', 'fill') else None
    if r == 'Guid.Empty':
        # `if (points.Count < 1) return Guid.Empty;` (File3dmObjectTable.AddPolyline; G82)
        return 'ON_UUID_to_Binding(ON_nil_uuid)' if sig_ret == 'Guid' and shape[:2] == ('conv', 'BND_UUID') else None
    if r in ('String.Empty', 'string.Empty', '""'):
        return 'std::wstring()' if sig_ret == 'string' and kind == 'holder' else None
    mm = re.match(r'^(\w+)\.(\w+)$', r)
    if mm and mm.group(1) == sig_ret and sig_ret in CS_ENUMS and g.get('ReturnValue') is not None \
            and kind == 'enumret':
        return '(%s)%s' % (CS_ENUMS[sig_ret]['cpp'], g['ReturnValue'])
    # `return LineCircleIntersection.None;` from a member whose result leads a
    # tuple: the tuple of that value and the outs as initialized (G57)
    if (mm and mm.group(1) == sig_ret and sig_ret in CS_ENUMS and g.get('ReturnValue') is not None
            and kind == 'tuple' and isinstance(shape[1], tuple)):
        return 'TUPLE:(%s)%s' % (CS_ENUMS[sig_ret]['cpp'], g['ReturnValue'])
    known = local_value(r)
    if known is not None and known[1] == sig_ret and kind == 'fromout':
        return known[0]
    return None


def default_ctor_call(cls, manifest, c_by_name):
    """RhinoCommon's standard default ctor (`ConstructNonConstObject(
    UnsafeNativeMethods.ON_X_New(IntPtr.Zero))`) becomes a real generated
    constructor: returns the C creation expression, or None. A value-mode class
    (MeshingParameters) copies the created object in and frees it (G74)."""
    if cls['mode'] not in ('pointer', 'value'):
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
    if cls['mode'] not in ('pointer', 'value'):
        return []
    out = []
    def skip(why):
        key = 'ctor: ' + why.split(':')[0]
        skipped[key] = skipped.get(key, 0) + 1
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Kind'] == 'constructor' and x.get('CtorCond')
                and x['Variant'] in ('portable', 'rhino3dm-only') and len(x.get('Calls') or []) == 2):
            p, why = cond_ctor_plan(x, cls, c_by_name)
            if p is None:
                skip(why)
            else:
                out.append(p)
            continue
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
        # a MANAGED class (G99: Font) is made from the shared object its finder returns, const
        managed_ctor = bool(cls.get('managed') and fn and fn['returns'] == 'const %s*' % cls['on'])
        if fn is None or (fn['returns'] != '%s*' % cls['on'] and not managed_ctor):
            skip('creation fn'); continue
        m2 = dict(x, Kind='method', Static=True, Name='__ctor', Returns=cls['cs'])
        p, why = plan(m2, fn, cls)
        if p is None:
            skip(why); continue
        if p['locals'] or p['shape'][0] not in ('wrap', 'valadopt'):
            skip('shape'); continue
        p['ctor_call'] = '%s(%s)' % (fn['name'], ', '.join(p['call_exprs']))
        if managed_ctor:
            p['ctor_call'] = 'const_cast<%s*>(%s)' % (cls['on'], p['ctor_call'])   # shared, never written through
        out.append(p)
    return out


def cond_ctor_plan(x, cls, c_by_name):
    """`IntPtr p; if (COND) p = F1(..); else p = F2(..); ConstructNonConstObject(p);`
    (PolylineCurve(points); G82): each arm planned as an ordinary constructor,
    the same binding arguments both ways, the condition translated like a
    guard over the parameters only -> one plan whose creation is a ternary"""
    if x['Body'] not in ('trivial', 'pattern') or x.get('Guards'):
        return None, 'cond ctor body'
    arms = []
    for call in x['Calls']:
        fn = c_by_name.get(call['Name'])
        if fn is None or fn['returns'] != '%s*' % cls['on']:
            return None, 'cond ctor creation fn'
        m2 = dict(x, Kind='method', Static=True, Name='__ctor', Returns=cls['cs'], Calls=[call], CtorCond=None)
        p, why = plan(m2, fn, cls)
        if p is None:
            return None, 'cond ctor arm: %s' % why
        if p['locals'] or p['shape'][0] != 'wrap':
            return None, 'cond ctor shape'
        arms.append((p, fn))
    if arms[0][0]['bind_args'] != arms[1][0]['bind_args']:
        return None, 'cond ctor arms bind differently'
    p = arms[0][0]
    ptypes = {n: t for _, t, n in (cs_signature(x)[1] or [])}
    argnames = {n for _, n in p['bind_args']}
    cond = re.sub(r'\bnull\b', 'nullptr', x['CtorCond']['Cond'])
    for ident in sorted(x['CtorCond'].get('Ids') or [], key=lambda i: '.' not in i):
        a, _, b = ident.partition('.')
        if not b and ident in argnames:
            continue
        if b == 'Count' and a in argnames and seq_elem(ptypes.get(a)):
            cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), '((int)%s.size())' % a, cond)
            continue
        return None, 'cond ctor id %s' % ident
    p = dict(p, extra_fns=list(p.get('extra_fns') or []) + [arms[1][1]] + list(arms[1][0].get('extra_fns') or []))
    p['ctor_call'] = '(%s) ? %s(%s) : %s(%s)' % (cond, arms[0][1]['name'], ', '.join(arms[0][0]['call_exprs']),
                                                 arms[1][1]['name'], ', '.join(arms[1][0]['call_exprs']))
    return p, None


_C_BODIES = None


def c_body(fn):
    """the text of a C-layer function's body (librhino3dm_native/*.cpp)"""
    global _C_BODIES
    if _C_BODIES is None:
        _C_BODIES = {}
        native = os.path.join(ROOT, 'src', 'librhino3dm_native')
        head = re.compile(r'^(?:RH_C_FUNCTION\s+)?[A-Za-z_][\w:<> ]*[\s*&]+(\w+)\s*\([^;{]*\)\s*\{', re.M)
        for f in sorted(os.listdir(native)):
            if not f.endswith('.cpp'):
                continue
            text = open(os.path.join(native, f), errors='replace').read()
            for mm in head.finditer(text):
                depth, i = 0, mm.end() - 1
                for j in range(i, len(text)):
                    if text[j] == '{':
                        depth += 1
                    elif text[j] == '}':
                        depth -= 1
                        if depth == 0:
                            _C_BODIES.setdefault(mm.group(1), text[i:j + 1])
                            break
    return _C_BODIES.get(fn)


_NON_CONST = {}


def non_const_methods(cls_name):
    """methods of an opennurbs class declared WITHOUT a trailing const"""
    if cls_name not in _NON_CONST:
        names = set()
        hdr_dir = os.path.join(ROOT, 'src', 'lib', 'opennurbs')
        start_re = re.compile(r'class\s+ON_CLASS\s+%s\b[^;{]*\{' % re.escape(cls_name))
        meth_re = re.compile(r'\b(\w+)\s*\(([^;{}]*)\)\s*(const)?\s*[;{]')
        for h in sorted(os.listdir(hdr_dir)):
            if not h.endswith('.h'):
                continue
            text = open(os.path.join(hdr_dir, h), errors='replace').read()
            mm = start_re.search(text)
            if not mm:
                continue
            depth, start, decl = 0, mm.end() - 1, ''
            for j in range(start, len(text)):
                if text[j] == '{':
                    depth += 1
                elif text[j] == '}':
                    depth -= 1
                    if depth == 0:
                        decl = text[start:j]
                        break
            for d in meth_re.finditer(decl):
                if not d.group(3):
                    names.add(d.group(1))
            break
        _NON_CONST[cls_name] = names
    return _NON_CONST[cls_name]


def plan_fill(x, cls, c_by_name):
    """create, fill, delete on failure (NurbsCurve.CreateFromArc):
    the first call creates the object, the second fills it; specgen recorded
    the pointer local, the failure test and the wrapped type"""
    new_call, fill_call = x['Calls']
    fn_new, fn_fill = c_by_name.get(new_call['Name']), c_by_name.get(fill_call['Name'])
    target = BY_CS.get(x.get('Wrap') or '')
    if not (fn_new and fn_fill and target and target['mode'] == 'pointer'):
        return None, 'fill: functions or wrapped type'
    if fn_new['returns'] != '%s*' % target['on']:
        return None, 'fill: creation returns %s' % fn_new['returns']
    new_args = []
    for a in new_call['Args']:
        if a['Kind'] == 'expr' and a['Text'] == 'IntPtr.Zero':
            new_args.append('nullptr')
        else:
            return None, 'fill: creation arg %s' % a['Text']
    ptr = x['FillPtr']
    # the fill call as an ordinary member returning its C status; the created
    # pointer is bound by name
    m2 = dict(x, Body='trivial', Returns=fn_fill['returns'] if fn_fill['returns'] in CVALUE else 'int',
              Calls=[dict(fill_call, Args=[dict(a, Kind='fillptr') if a['Text'] == ptr else a
                                           for a in fill_call['Args']])], Wrap=None)
    if fn_fill['returns'] not in ('int', 'bool'):
        return None, 'fill: status %s' % fn_fill['returns']
    m2['Returns'] = {'int': 'int', 'bool': 'bool'}[fn_fill['returns']]
    p, why = plan(m2, fn_fill, cls)
    if p is None:
        return None, 'fill: ' + why
    p['m'] = x
    p['fn'] = fn_fill
    p['static'] = bool(x.get('Static'))
    p['shape'] = ('fill', target, '%s(%s)' % (fn_new['name'], ', '.join(new_args)), x['FillFail'])
    return p, None


def plan_discard_forward(x, target, args, cls):
    """`int accuracy; return ToNurbsSurface(0.0, out accuracy);` (specgen 'discard';
    G73): the target's own native call, planned under this member's signature
    with the discarded out as a local nothing reads -- never a call of the
    generated target, whose tuple owns the object it leads with."""
    tm, fn = target['m'], target.get('fn')
    if fn is None or len(tm.get('Calls') or []) != 1:
        return None, 'discard: target shape'
    tret, tparams = cs_signature(tm)
    sret, sparams = cs_signature(x)
    if tparams is None or sparams is None or len(tparams) != len(args) or tret != sret:
        return None, 'discard: signature'
    own = {n for _, _, n in sparams}
    tnames = {n for _, _, n in tparams}
    amap = {tparams[i][2]: a for i, a in enumerate(args)}
    def sub(a):
        root = (a.get('Text') or '').split('.')[0]
        ca = amap.get(root)
        if a['Kind'] in ('param', 'paramptr', 'parammember') and ca is not None:
            if ca['Kind'] == 'param' and ca['Text'] in own:
                return dict(a, Text=ca['Text'] + a['Text'][len(root):])
            if ca['Kind'] == 'literal' and a['Kind'] == 'param':
                return {'Kind': 'literal', 'Text': ca['Text']}
            return None
        if a['Kind'] == 'out' and ca is not None:
            if ca['Kind'] == 'discard':
                return a                                   # the target's own local, never read
            if ca['Kind'] == 'param' and ca['Text'] in own:
                return dict(a, Text=ca['Text'])            # an out handed through
            return None
        if root in tnames and a['Kind'] != 'expr':
            return None                                    # a target parameter this member lacks
        return a
    nargs = [sub(a) for a in tm['Calls'][0]['Args']]
    if any(a is None for a in nargs):
        return None, 'discard: argument'
    m = dict(tm, Calls=[dict(tm['Calls'][0], Args=nargs)], Params=x['Params'], Signature=x['Signature'],
             Returns=x['Returns'], Name=x['Name'], Kind=x['Kind'], Static=x.get('Static'))
    p, why = plan(m, fn, cls)
    if p is None:
        return None, 'discard: ' + why
    p['m'] = x
    return p, None


def plan_forward(x, cls, by_name_arity, members=()):
    """a RhinoCommon member whose body only calls a sibling overload"""
    args = x.get('DelegateArgs') or []
    targets = by_name_arity.get((x['Delegates'], len(args)), [])
    if len(targets) != 1:
        return None, 'target %s' % ('ambiguous' if targets else 'not generated')
    target = targets[0]
    if target['shape'][0] == 'forward':
        return None, 'chained'
    if any(a['Kind'] == 'discard' for a in args):
        return plan_discard_forward(x, target, args, cls)
    sig_ret, sig_params = cs_signature(x)
    if sig_ret is None:
        return None, 'unparsed signature'
    # C# picks the overload by argument TYPES: among every RhinoCommon overload
    # of that name and arity, the ones whose parameters take the forwarded
    # parameters' types -- it must be exactly one, and the generated target
    # (`Add(BrepVertex, ...)` forwards to Add(BrepVertex, ...), not to the
    # generated Add(int, ...))
    own = {n: t for _, t, n in sig_params}
    NUMERIC = ('int', 'uint', 'long', 'ulong', 'short', 'ushort', 'byte', 'float', 'double')
    def arg_fits(a, pt):
        k, t = a['Kind'], a['Text']
        if k == 'param' and t in own:
            return pt == own[t]
        if k == 'parammember' and t.count('.') == 1:
            pn, fld = t.split('.')
            rt = VT_PROP_RET.get((own.get(pn), fld))
            cs_rt = {v: k for k, v in VT_CPP.items()}.get(rt) or {'unsigned int': 'uint'}.get(rt, rt)
            return rt is None or pt == cs_rt
        if k == 'literal':
            if t in ('true', 'false'):
                return pt == 'bool'
            if t == 'null':
                return pt not in NUMERIC and pt != 'bool'
            if re.match(r'^-?\d+$', t):                      # an int literal converts to any numeric
                return pt in NUMERIC
            if re.match(r'^-?[\d.]+([eE][-+]?\d+)?[dD]?$', t):
                return pt == 'double'
        return True                                         # untyped here: does not discriminate
    def takes(m):
        _, ps = cs_signature(m)
        return ps is not None and len(ps) == len(args) and all(arg_fits(a, ps[i][1]) for i, a in enumerate(args))
    overloads = [m for m in members if m['Name'] == x['Delegates'] and m['Kind'] == target['m']['Kind']
                 and m.get('Variant') == 'portable' and m is not x and takes(m)]
    if members and (len(overloads) != 1 or overloads[0].get('Signature') != target['m'].get('Signature')):
        return None, 'target overload by type'
    # an `out` param handed straight to an `out` slot of the target is
    # dropped from both sides: the generated target already returns it in
    # its tuple (TryGetPlane(out plane) -> TryGetPlane(out plane, tol))
    _, tparams = cs_signature(target['m'])
    passthru = set()
    for i, a in enumerate(args):
        if (a['Kind'] == 'param' and tparams and i < len(tparams)
                and tparams[i][0] == 'out'
                and any(m == 'out' and n == a['Text'] for m, _, n in sig_params)):
            passthru.add(a['Text'])
    bind_args, ptypes = [], {}
    for mode, cstype, name in sig_params:
        if name in passthru:
            continue
        if mode:
            return None, 'out/ref param'
        if cstype in CS_ENUMS:
            bind_args.append((CS_ENUMS[cstype]['cpp'], name))
        elif cstype in BY_CS:
            bind_args.append(('const %s&' % BY_CS[cstype]['gen'], name))
        elif cstype in CS_ARG:
            bind_args.append((CS_ARG[cstype][0], name))
        elif cstype in VT_CPP:
            bind_args.append((VT_CPP[cstype], name))
        else:
            return None, 'param type %s' % cstype
        ptypes[name] = cstype
    call_args, sib_refs, init_locals = [], [], []
    for a in args:
        k, t = a['Kind'], a['Text']
        if k == 'param' and t in passthru:
            continue
        if k == 'param' and t in ptypes:
            call_args.append(t)
        elif k == 'literal' and t == 'null':
            # SetUserString(key, null) means "delete" in C; a by-value
            # std::wstring target parameter cannot carry null -- refuse
            return None, 'null literal'
        elif k == 'literal':
            call_args.append({'true': 'true', 'false': 'false'}.get(t, t.rstrip('dDfF') or t))
        elif k == 'const' and a.get('Value') is not None:
            # an enum constant into an enum-typed target parameter keeps its type
            tt = target['bind_args'][len(call_args)][0] if len(call_args) < len(target['bind_args']) else None
            enum_cpp = {v['cpp'] for v in CS_ENUMS.values()}
            call_args.append('(%s)%s' % (tt, a['Value']) if tt in enum_cpp else a['Value'])
        elif k == 'expr' and re.match(r'^RhinoMath\.\w+$', t):
            call_args.append('RH3DM_RhinoMath::' + t.split('.')[1])
        elif k == 'expr' and re.match(r'^(?:\w+\.)*(\w+)\.(\w+)$', t) and \
                (t.split('.')[-2], t.split('.')[-1]) in VT_STATICS:
            # Transform.Identity / Geometry.Transform.Identity: a translated static
            vt_t, vt_m = t.split('.')[-2:]
            call_args.append('%s::%s()' % (VT_CPP[vt_t], vt_m))
        elif k == 'expr' and re.match(r'^(\w+)\.(\w+)$', t):
            # Domain.Min: a sibling property (generated, zero arguments, a value
            # type) then a translated property of that value type. The sibling's
            # C++ name is assigned after planning: resolved at emit time.
            sib, mem = t.split('.')
            sibs = [q for q in by_name_arity.get((sib, 0), []) if q['m']['Kind'] == 'property']
            vt_ret = sibs[0]['m'].get('Returns') if len(sibs) == 1 else None
            if vt_ret not in VT_CPP or (vt_ret, mem) not in VT_PROPS:
                return None, 'arg expr %s' % t
            sib_refs.append(sibs[0])
            call_args.append('@@%d@@.%s()' % (len(sib_refs) - 1, mem))
        elif k == 'parammember' and t.count('.') == 1:
            pn, fld = t.split('.')
            if (ptypes.get(pn), fld) not in VT_PROPS:
                return None, 'member arg %s' % t
            call_args.append('%s.%s()' % (pn, fld))         # the translated getter
        elif k == 'expr' and (mm := re.match(r'^new\s+([\w.]+)\s*\{\s*(.+?)\s*\}$', t)) and cs_unqualify(mm.group(1)) in DATA_CPP:
            # `new File3dmWriteOptions { Version = version }`: a data object built
            # from the forwarded parameters, handed to the target (G65)
            dcs = cs_unqualify(mm.group(1))
            steps = []
            for asg in mm.group(2).split(','):
                pm = re.match(r'^\s*(\w+)\s*=\s*(\w+)\s*$', asg)
                if not pm or not DATA_INFO[dcs]['props'].get(pm.group(1)) or pm.group(2) not in ptypes:
                    return None, 'arg initializer %s' % t
                steps.append('rh3dm_init.set_%s(%s);' % (pm.group(1), pm.group(2)))
            init_locals += ['%s rh3dm_init;' % DATA_CPP[dcs]] + steps
            tt = target['bind_args'][len(call_args)][0] if len(call_args) < len(target['bind_args']) else ''
            if tt not in ('const %s*' % DATA_CPP[dcs], 'const %s&' % DATA_CPP[dcs]):
                return None, 'arg initializer slot %s' % tt
            call_args.append('&rh3dm_init' if tt.endswith('*') else 'rh3dm_init')
        else:
            return None, 'arg %s' % k
    # a class argument forwarded into an OPTIONAL slot of the target (a
    # nullable pointer: AddLine(line, attributes) -> AddLine(from, to,
    # attributes)) is optional here too -- C# lets it be null in both
    tbind = target['bind_args']
    for i, a in enumerate(call_args):
        if i < len(tbind) and tbind[i][0].endswith('*') and ptypes.get(a) in BY_CS:
            bind_args = [('const %s*' % BY_CS[ptypes[a]]['gen'], n) if n == a else (t, n)
                         for t, n in bind_args]
    return {'m': x, 'fn': None, 'cls': cls, 'static': target['static'],
            'call_exprs': [], 'locals': init_locals, 'bind_args': bind_args,
            'shape': ('forward', None, target), 'guards': [], 'success': None,
            'forward_args': call_args, 'sib_refs': sib_refs}, None


def _subst(t, binding):
    """T -> Layer in a C# type text (`T`, `IEnumerable<T>`, `T[]`)"""
    if not t or not binding:
        return t
    return re.sub(r'\b(%s)\b' % '|'.join(map(re.escape, binding)), lambda mm: binding[mm.group(1)], t)


def class_members(cls, manifest):
    """the members a registry class plans: its own, plus those its GENERIC
    RhinoCommon bases declare (File3dmCommonComponentTable<Layer>.Delete(T)
    planned as File3dmLayerTable.Delete(Layer)), with the type parameters
    bound. A generic base is never a C++ class of its own (a C++ method
    cannot return T per subclass), so its members are instantiated per
    concrete class -- as C# itself does. A member the class (or a nearer
    base) redeclares with the same signature is the override: the base's is
    skipped. Registry bases are left to C++ inheritance, as before."""
    own = [x for x in manifest['members'] if x['Type'] == cls['type']]
    if cls.get('ctor_key'):
        # reached through one constructor that nulls fields: each body as analyzed
        # with those fields null (G47) -- the record's analysis keys replaced
        own = [dict(x, **x['Specialized'][cls['ctor_key']])
               if (x.get('Specialized') or {}).get(cls['ctor_key']) else x for x in own]
    if cls.get('mf_active'):
        # G88: the id property's setter is its (managed-null) analysis plus the
        # retarget of the modeled field the C# body does after it (MfRetarget)
        for f, v in (cls.get('modeled') or {}).items():
            for i, x in enumerate(own):
                sp = (x.get('Specialized') or {}).get('(managed-null):set')
                if x['Name'] == v['id_prop'] and x['Kind'] == 'property' and sp:
                    own[i] = dict(x, Setter=sp, MfRetarget=f)
    if cls.get('null_key'):
        # the type's managed fields are null on every generated object (no writer is
        # generated; bnd_gen checks): bodies as analyzed with them null (G75)
        def mnull(x):
            sp = x.get('Specialized') or {}
            if not sp.get(cls['null_key']) or sp.get(cls.get('ctor_key') or ''):
                return x
            y = dict(x, **sp[cls['null_key']])
            if sp.get(cls['null_key'] + ':set'):
                y['Setter'] = sp[cls['null_key'] + ':set']
            return y
        own = [mnull(x) for x in own]
    if not TYPE_RECS:
        return own
    if (TYPE_RECS.get(cls['type']) or {}).get('ExtraComponentTypeValues'):
        # a multi-type table (File3dmObjectTable): its Count and GetEnumerator
        # overrides are the base's over several types -- planned from the base
        own = [x for x in own if not (x['Kind'] in ('method', 'property') and x['Name'] in ('Count', 'GetEnumerator'))]
    def key(x):
        _, ps = cs_signature(x)
        return (x['Kind'], x['Name'], tuple(t for _, t, _ in (ps or [])))
    seen = {key(x) for x in own}
    by_type = {}
    for x in manifest['members']:
        by_type.setdefault(x['Type'], []).append(x)
    out = list(own)
    for bt, binding in base_chain(cls['type']):
        if bt in BY_TYPE or not TYPE_RECS[bt].get('TypeParams'):
            continue
        for x in by_type.get(bt, []):
            if x['Kind'] in ('type', 'constructor'):
                continue
            y = dict(x, Type=cls['type'], InheritedFrom=bt,
                     Returns=_subst(x.get('Returns'), binding),
                     Params=[dict(q, Type=_subst(q['Type'], binding)) for q in x.get('Params') or []],
                     Signature=_subst(x.get('Signature'), binding))
            if key(y) in seen:
                continue
            seen.add(key(y))
            out.append(y)
    return out


def child_view(x):
    """the registry child class a property hands out, or None: the getter must
    construct exactly `new T(this)` (specgen Child/ChildArgs) -- the view
    holds nothing but its parent (`new File3dmViewTable(this, true)` carries
    state a pointer view cannot, so it is refused) -- and the declared type is
    T itself or an interface T implements with the same arguments
    (`IList<Layer> Layers` is a File3dmLayerTable)"""
    child = BY_CS.get(cs_unqualify(x.get('Child') or ''))
    if child is None:
        return None
    if child.get('key') and child_key(x, child) is not None:
        return child
    if child.get('state'):
        # a view with constructor state (G60: `new Decals(this, false, doc_sn)`)
        if child_state_values(x, child) is None:
            return None
    elif x.get('ChildArgs') != ['this']:
        return None
    ret = cs_unqualify(x.get('Returns') or '')
    if ret == child['cs']:
        return child
    mm = re.match(r'^([\w.]+)<(.+)>$', ret)
    rec = TYPE_RECS.get(child['type']) or {}
    for i, b in enumerate(rec.get('Bases') or []):
        args = (rec.get('BaseArgs') or [None] * len(rec['Bases']))[i] or []
        if mm and cs_unqualify(b) == cs_unqualify(mm.group(1)) \
                and [cs_unqualify(a) for a in args] == [cs_unqualify(a.strip()) for a in mm.group(2).split(',')]:
            return child
    return None


def child_state_values(x, child):
    """`new Decals(this, false, 0)`: the view's state (registry 'state') as C++
    literals, in constructor order -- every argument after `this` a literal of
    its parameter's type -- or None"""
    args, kinds = x.get('ChildArgs') or [], x.get('ChildArgKinds') or []
    st = child['state']
    if args[:1] != ['this'] or len(args) != 1 + len(st) or any(k != 'const' for k in kinds[1:]):
        return None
    vals = []
    for (ctype, _), a in zip(st, args[1:]):
        if ctype == 'bool' and a in ('true', 'false'):
            vals.append(a)
        elif ctype in ('int', 'unsigned int') and re.match(r'^\d+[uU]?$', a):
            vals.append(a.rstrip('uU'))
        else:
            return None
    return vals


def child_key(x, child):
    """`new NurbsSurfaceKnotList(this, 0)`: the constant key a keyed child is
    made with (its constructor roles: owner, key), or None"""
    roles = child['key']['Ctor']
    args, kinds = x.get('ChildArgs') or [], x.get('ChildArgKinds') or []
    if len(args) != len(roles) or len(roles) != 2:
        return None
    by = dict(zip(roles, zip(args, kinds)))
    if by['owner'] != ('this', 'this') or not re.match(r'^\d+$', by['key'][0]):
        return None
    return by['key'][0]


def plan_construct_nocall(x, cls):
    """a construction with no native call (`this[int]` of a list after its
    guards: `new BrepFace(index, m_brep)`; `new PointCloudItem(this, index)`)"""
    sig_ret, sig_params = cs_signature(x)
    if sig_params is None or any(m for m, _, _ in sig_params):
        return None, 'construct params'
    bind_args = []
    for _, t, n in sig_params:
        # a Guid key (G98: `new PostEffectData(this, id)`): the binding's uuid, converted for the key
        if t not in ('int', 'uint') and not (t == 'Guid' and cs_unqualify(x.get('Child') or '') in BY_CS
                                             and (BY_CS[cs_unqualify(x['Child'])].get('comp') or {}).get('Key') == 'ON_UUID'):
            return None, 'construct param %s' % t
        bind_args.append((CS_ARG[t][0], n))
    basecls = BY_REG.get(cls.get('base', ''))
    self_expr = ('static_cast<%s*>(m_ptr)' % cls['on']
                 if basecls and basecls['mode'] not in ('abstract',) else 'm_ptr')
    ic = (TYPE_RECS.get(cls['type']) or {}).get('InternalCopy')
    if cs_unqualify(x.get('Child') or '') == cls['cs'] and x.get('ChildArgs') == ['this'] and ic:
        # `return new PolyCurve(this);` (DuplicatePolyCurve; G69): the internal copy
        # constructor's native on this object's const pointer, owned by the new object
        fn = C_BY_NAME.get(ic)
        if (not fn or cls['mode'] != 'pointer' or fn.get('variant') == 'rhino-only' or fn['returns'] != '%s*' % cls['on']
                or [q['type'] for q in fn['params']] not in (['const %s*' % cls['on']], ['%s*' % cls['on']]) or bind_args):
            return None, 'construct self copy %s' % ic
        # C# hands ConstPointer() to the C parameter whatever its constness: cast as the marshaller does
        return {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
                'bind_args': [], 'shape': ('selfcopy', fn, '(%s)%s' % (fn['params'][0]['type'], self_access(cls, self_expr, 'ConstPointer'))), 'extra_fns': [fn],
                'guards': [g for g in x.get('Guards') or []], 'success': None, 'failthrow': None,
                'typearg_pos': [], 'commits': False}, None
    shape = construct_shape(x, cls, self_expr, {n for _, n in bind_args}, None, {})
    if isinstance(shape, str):
        return None, shape
    if any(g.get('Assign') for g in x.get('Guards') or []):
        return None, 'parameter normalization in a construction'
    extra = []
    if x.get('KeyMap') and cls.get('scoped'):
        extra = [C_BY_NAME[x['KeyMap']['Native']], C_BY_NAME[cls['scope']['comp']['comp']['Const']]]
    return {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
            'bind_args': bind_args, 'shape': shape, 'extra_fns': extra,
            'guards': [g for g in x.get('Guards') or []], 'success': None, 'failthrow': None,
            'typearg_pos': [], 'commits': False}, None


def plan_arrayfill(x, cls, c_by_name):
    """`int count = <native | property>; if (count < 1) return <new T[0] | null>;
    T[] a = new T[count]; UNM.Fill(..., count, a); return a;` (specgen ArrayFill):
    a std::vector the native fills, returned as a tuple (G54). Each native call
    is planned by plan() itself, so arguments obey the ordinary rules."""
    af = x['ArrayFill']
    sig_ret, _ = cs_signature(x)
    elem = af['Elem']
    conv = None
    if af['FillForm'] == 'loop':
        return plan_elemloop(x, cls, c_by_name)
    if af.get('ElemConv'):
        # `res[i] = Color.FromArgb(rc[i])`: the ints converted as the C# loop does
        how, to = af['ElemConv'].split(':')
        if elem != 'int' or cs_unqualify(to) != 'Color' or how not in ('FromArgb', 'FromWin32'):
            return None, 'arrayfill: conversion %s' % af['ElemConv']
        conv = 'RH3DM_Color%s(%%s)' % how
        if sig_ret != 'Color[]':
            return None, 'arrayfill: returns %s' % sig_ret
    elif sig_ret != elem + '[]':
        return None, 'arrayfill: returns %s' % sig_ret
    if elem in VT_CPP:
        elem_cpp = VT_CPP[elem]
    elif elem in ('int', 'double', 'float'):
        elem_cpp = elem
    elif elem == 'uint':
        elem_cpp = 'unsigned int'
    else:
        return None, 'arrayfill: element %s' % elem
    def sub(call, ret):
        args = [{'Kind': 'arrcount', 'Text': a['Text']} if a['Text'] == af['Count'] and a['Kind'] in ('expr', 'local')
                else {'Kind': 'arrayout', 'Text': a['Text'], 'Value': elem} if a['Text'] == af['Local']
                else a for a in call['Args']]
        m = dict(x, Calls=[dict(call, Args=args)], Returns=ret, Body='trivial', ArrayFill=None,
                 Success=None, FailThrow=None, Wrap=None, WrapArity=0, Compare=None, ResultConv=None,
                 ConvOut=None, ResultCast=None, ResultUnused=(ret == 'void'), Holder=None, NewObjFail=None)
        fn = c_by_name.get(call['Name'])
        if fn is None:
            return None, 'arrayfill: C fn %s' % call['Name']
        return plan(m, fn, cls)
    fill, why = sub(x['Calls'][-1], 'bool' if af['FillForm'] == 'success' else 'void')
    if fill is None:
        return None, 'arrayfill: fill: ' + why
    if af['FillForm'] == 'success' and fill['fn']['returns'] != 'bool':
        return None, 'arrayfill: fill status %s' % fill['fn']['returns']
    info = {'elem': elem_cpp, 'local': af['Local'], 'count': af['Count'], 'empty': af.get('Empty'),
            'empty_cond': af.get('EmptyCond'), 'form': af['FillForm'], 'conv': conv}
    cm = re.match(r'^(call|prop:(\w+)|method:(\w+)\(([\w,]*)\))(?:\+(\d+))?$', af['CountFrom'])
    if not cm:
        return None, 'arrayfill: count from %s' % af['CountFrom']
    info['count_add'] = cm.group(5)                     # `SpanCount(direction) + 1`
    if cm.group(1) == 'call':
        size, why = sub(x['Calls'][0], 'int')
        if size is None or size['locals'] or size['shape'] != ('direct', 'int'):
            return None, 'arrayfill: size: %s' % (why or 'shape')
        info['count_expr'] = '%s(%s)' % (size['fn']['name'], ', '.join(size['call_exprs']))
        fill['extra_fns'] = list(fill.get('extra_fns') or []) + [size['fn']]
    elif cm.group(2):
        info['count_prop'] = cm.group(2)               # a zero-arg sibling, resolved with the guards
    else:
        # a sibling method on this member's own parameters, resolved with the guards
        names = [a for a in cm.group(4).split(',') if a]
        if any(a not in {n for _, n in fill['bind_args']} for a in names):
            return None, 'arrayfill: count method args'
        info['count_method'] = (cm.group(3), names)
    if info['count_add'] and 'count_expr' in info:
        info['count_expr'] += ' + ' + info['count_add']
    if info['empty_cond'] is not None and not re.match(r'^(%s\s*(<|<=|==)\s*-?\d+|-?\d+\s*(>|>=|==)\s*%s)$'
                                                       % (re.escape(af['Count']), re.escape(af['Count'])), info['empty_cond']):
        return None, 'arrayfill: empty condition %s' % info['empty_cond']
    fill['m'] = x
    fill['shape'] = ('arrayfill', info)
    return fill, None


def plan_elemloop(x, cls, c_by_name):
    """`int count = Count; var rc = new T[count]; for (i < count) { var e = new E();
    UNM.F(..., i, ref e); rc[i] = e | new T((float)e.X, ...); } return rc;` (specgen
    ArrayFill, FillForm loop; G72): one native call per element, the elements
    as a tuple. The call is planned by plan() with the index as `rh3dm_i`."""
    af = x['ArrayFill']
    sig_ret, _ = cs_signature(x)
    elem = af['Elem']
    if elem not in VT_CPP:
        return None, 'arrayfill: element %s' % elem
    item = '{n}'
    if af.get('ElemConv'):
        how, to = af['ElemConv'].split(':')
        # `new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z)`: the narrower value
        # type's own constructor over the element's three fields
        if how != 'narrow' or to not in VT_CPP or len(VT_FIELDS.get(elem) or []) != 3 or sig_ret != to + '[]':
            return None, 'arrayfill: conversion %s' % af['ElemConv']
        item = '%s(%s)' % (VT_CPP[to], ', '.join('(float){n}.%s' % f for f in VT_FIELDS[elem]))
    elif sig_ret != elem + '[]':
        return None, 'arrayfill: returns %s' % sig_ret
    call = x['Calls'][0]
    args = [{'Kind': 'arrindex', 'Text': a['Text']} if a['Text'] == af['Index'] and a['Kind'] in ('expr', 'local')
            else a for a in call['Args']]
    if not any(a['Kind'] == 'arrindex' for a in args) or not any(a['Kind'] == 'out' and a['Text'] == af['ElemLocal'] for a in args):
        return None, 'arrayfill: loop arguments'
    fn = c_by_name.get(call['Name'])
    if fn is None:
        return None, 'arrayfill: C fn %s' % call['Name']
    m = dict(x, Calls=[dict(call, Args=args)], Returns='void', Body='trivial', ArrayFill=None,
             Success=None, FailThrow=None, Wrap=None, WrapArity=0, Compare=None, ResultConv=None,
             ConvOut=None, ResultCast=None, ResultUnused=True, Holder=None, NewObjFail=None)
    p, why = plan(m, fn, cls)
    if p is None:
        return None, 'arrayfill: loop call: ' + why
    if not any(l.startswith(VT_CPP[elem] + ' ' + af['ElemLocal']) for l in p['locals']):
        return None, 'arrayfill: element local %s' % af['ElemLocal']
    cm = re.match(r'^prop:(\w+)$', af['CountFrom'])
    if not cm:
        return None, 'arrayfill: count from %s' % af['CountFrom']
    p['m'] = x
    p['shape'] = ('arrayfill', {'form': 'loop', 'elem': VT_CPP[elem], 'local': af['Local'], 'count': af['Count'],
                                'count_prop': cm.group(1), 'count_add': None, 'item': item, 'elem_local': af['ElemLocal'],
                                'empty': None, 'empty_cond': None, 'conv': None})
    return p, None


def plan_wraptuple(x, cls, c_by_name):
    """A tuple of geometry made from native pointers (specgen WrapTuple; G72):
    `bool rc = F(..., ref a, ref b); ... return rc ? [wrap a, wrap b] : null`
    (outptr), `a = New(); rc = F(..., a); if (rc) left = new T(a) else Delete(a)`
    (newptr: the tuple leads with the status, as every out-param member does),
    or a holder of pointers read back element by element (ptrloop). The call
    is planned by plan(); the pointer locals are its 'ptrlocal' arguments."""
    wt = x['WrapTuple']
    sig_ret, sig_params = cs_signature(x)
    W = BY_CS.get(wt['Wrap'])
    if W is None:
        return None, 'wraptuple: wrap %s' % wt['Wrap']
    call = next((c for c in x['Calls'] if c['Name'] == wt['Call']), None)
    fn = c_by_name.get(wt['Call'])
    if call is None or fn is None:
        return None, 'wraptuple: C fn %s' % wt['Call']
    if len(call['Args']) != len(fn['params']):
        return None, 'wraptuple: arity'
    form = wt['Form']
    plain = W['mode'] in ('pointer', 'abstract') and not (W.get('comp') or W.get('handle') or W.get('borrowed') or W.get('shared'))

    def pointee_ok(t):          # W's own ON type, or one deriving from it
        t = t.replace('const ', '').strip()
        return t == W['on'] or any(c['on'] == t and derives_from(c, W) for c in CLASSES)
    args = list(call['Args'])
    info = {'form': form, 'gen': W['gen'], 'on': W['on'], 'items': list(wt.get('Locals') or []), 'fail': []}
    if form in ('outptr', 'newptr'):
        if form == 'outptr' and sig_ret != wt['Wrap'] + '[]':
            return None, 'wraptuple: returns %s' % sig_ret
        if form == 'newptr' and (sig_ret != 'bool' or [(m, t) for m, t, n in sig_params if n in wt['Outs']] != [('out', wt['Wrap'])] * len(wt['Outs'])):
            return None, 'wraptuple: out params'
        for name in wt['Locals']:
            pos = [i for i, a in enumerate(args) if a['Text'] == name]
            if len(pos) != 1:
                return None, 'wraptuple: local %s' % name
            ctype = fn['params'][pos[0]]['type']
            if form == 'outptr':
                if not plain or not ctype.endswith('**') or not pointee_ok(ctype[:-2]):
                    return None, 'wraptuple: %s -> %s' % (name, ctype)
                args[pos[0]] = {'Kind': 'ptrlocal', 'Text': '&' + name, 'Value': '%s %s = nullptr;' % (ctype[:-1], name)}
                info['fail'].append('delete %s;' % name)
            elif W['mode'] == 'value' and ctype == W['on'] + '*':
                # the generated class holds the value: filled in place, no New/Delete
                args[pos[0]] = {'Kind': 'ptrlocal', 'Text': '&' + name, 'Value': '%s %s;' % (W['on'], name)}
            elif plain and ctype.endswith('*') and pointee_ok(ctype[:-1]):
                nf, df = c_by_name.get(wt['New']), c_by_name.get(wt['Delete'])
                if (nf is None or df is None or nf['params'] or nf['returns'] != ctype
                        or [q['type'] for q in df['params']] != [ctype]):
                    return None, 'wraptuple: new/delete %s' % name
                args[pos[0]] = {'Kind': 'ptrlocal', 'Text': name, 'Value': '%s %s = %s();' % (ctype, name, nf['name'])}
                info['fail'].append('%s(%s);' % (df['name'], name))
                info.setdefault('extra', []).extend([nf, df])
            else:
                return None, 'wraptuple: %s -> %s' % (name, ctype)
        if form == 'outptr':
            info['item'] = '{p} ? RH3DM_Wrap_%s({p}) : (%s*)nullptr' % (W['reg'], W['gen'])
        elif W['mode'] == 'value':
            info['item'] = 'rh3dm_ok ? new %s({p}) : (%s*)nullptr' % (W['gen'], W['gen'])
        else:
            info['item'] = 'rh3dm_ok ? RH3DM_Wrap_%s({p}) : (%s*)nullptr' % (W['reg'], W['gen'])
        params = [q for q in x['Params'] if q['Name'] not in (wt.get('Outs') or [])]
        m = dict(x, Calls=[dict(call, Args=args)], Params=params, Returns='bool', Body='trivial', WrapTuple=None,
                 Success=None, FailThrow=None, Wrap=None, WrapArity=0, Compare=None, ResultConv=None,
                 ConvOut=None, ResultCast=None, ResultUnused=False, Holder=None, NewObjFail=None)
        p, why = plan(m, fn, cls)
        if p is None:
            return None, 'wraptuple: call: ' + why
        if fn['returns'] != 'bool':
            return None, 'wraptuple: status %s' % fn['returns']
    else:                                                   # ptrloop
        if sig_ret != wt['Wrap'] + '[]' or not plain:
            return None, 'wraptuple: returns %s' % sig_ret
        hp = HOLDERS.get(wt['Holder'])
        if hp is None or hp.get('ptr') != W['on']:
            return None, 'wraptuple: holder %s' % wt['Holder']
        pos = [i for i, a in enumerate(args) if a['Kind'] == 'newobj' and a['Text'] and HOLDERS.get(a.get('Value') or '') is hp]
        if len(pos) != 1:
            return None, 'wraptuple: holder argument'
        args[pos[0]] = dict(args[pos[0]], Kind='expr')
        cf, gf, df = c_by_name.get(wt['Count']), c_by_name.get(wt['Get']), c_by_name.get(wt['Dup']) if wt.get('Dup') else None
        if (cf is None or gf is None or cf['returns'] != 'int' or [q['type'].replace('const ', '') for q in cf['params']] != [hp['ctype']]
                or [q['type'].replace('const ', '') for q in gf['params']] != [hp['ctype'], 'int'] or not gf['returns'].endswith('*')
                or not pointee_ok(gf['returns'][:-1]) or gf['returns'].startswith('const ')):
            return None, 'wraptuple: count/get %s %s' % (wt['Count'], wt['Get'])
        if wt.get('Dup') and (df is None or [q['type'] for q in df['params']] != [gf['returns']] or df['returns'] != gf['returns']):
            return None, 'wraptuple: dup %s' % wt['Dup']
        m = dict(x, Calls=[dict(call, Args=args)], Returns=hp['cs'], Body='trivial', WrapTuple=None, _wraptuple=True,
                 Holder=wt['Holder'], HolderArg=pos[0], Success=None, FailThrow=None, Wrap=None, WrapArity=0, Compare=None,
                 ResultConv=None, ConvOut=None, ResultCast=None, ResultUnused=True, NewObjFail=None)
        p, why = plan(m, fn, cls)
        if p is None:
            return None, 'wraptuple: call: ' + why
        info.update(count=cf['name'], get=gf['name'], dup=df and df['name'], ptr=gf['returns'][:-1],
                    item='{p} ? RH3DM_Wrap_%s({p}) : (%s*)nullptr' % (W['reg'], W['gen']))
        info['extra'] = [cf, gf] + ([df] if df else [])
    p['m'] = x
    p['shape'] = ('wraptuple', info)
    p['extra_fns'] = list(p.get('extra_fns') or []) + list(info.pop('extra', []))
    return p, None


def plan_sequence(x, cls, c_by_name):
    """`UNM.A(p, ...); UNM.B(p);` (specgen Sequence): a void member whose native
    calls are bare statements, in order (MeshVertexList.Clear). Each call is
    planned by plan(); the member's guards run once, first (G54)."""
    sig_ret, _ = cs_signature(x)
    if sig_ret != 'void':
        return None, 'sequence: returns %s' % sig_ret
    subs = []
    for i, call in enumerate(x['Calls']):
        fn = c_by_name.get(call['Name'])
        if fn is None:
            return None, 'sequence: C fn %s' % call['Name']
        m = dict(x, Calls=[call], ResultUnused=True, Sequence=False,
                 Guards=x.get('Guards') if i == 0 else None)
        p, why = plan(m, fn, cls)
        if p is None:
            return None, 'sequence: call %d: %s' % (i, why)
        if p['locals'] or p['shape'] != ('direct', 'void') or (i and p['guards']):
            return None, 'sequence: call %d shape' % i
        subs.append(p)
    first = dict(subs[0])
    guard = None
    tn = x.get('TempNative')
    if tn:
        # the temporary native the data argument builds, once; the calls run only
        # when it was made (`if (L != IntPtr.Zero) { ... }`) (G65)
        bt = dict((n, t) for t, n in first['bind_args']).get(tn['Param'])
        pcs = {v: k for k, v in DATA_CPP.items()}.get((bt or '').replace('const ', '').rstrip('*&'))
        if pcs is None or tn['Method'] not in DATA_INFO[pcs]['methods']:
            return None, 'sequence: tempnative %s' % tn['Param']
        acc = ('const_cast<%s*>(%s)->' if bt.endswith('*') else 'const_cast<%s&>(%s).') % (DATA_CPP[pcs], tn['Param'])
        first['locals'] = ['void* rh3dm_tmp_%s = %s%s();' % (tn['Param'], acc, tn['Method'])]
        guard = 'rh3dm_tmp_%s' % tn['Param'] if tn.get('Guarded') else None
    elif any('tempnative' == a['Kind'] for c in x['Calls'] for a in c['Args']):
        return None, 'sequence: tempnative without its local'
    first['shape'] = ('seq', ['%s(%s)' % (q['fn']['name'], ', '.join(q['call_exprs'])) for q in subs], guard)
    first['extra_fns'] = [f for q in subs for f in [q['fn']] + list(q.get('extra_fns') or [])]
    return first, None


def plan_class(cls, manifest, c_by_name, skipped):
    cands = [x for x in manifest['members']
             if x['Type'] == cls['type'] and x['Variant'] == 'portable'
             and x['Kind'] in ('method', 'property', 'indexer')
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

    # create-fill-or-delete factories (NurbsCurve.CreateFromArc)
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x.get('Body') == 'fill'
                and x['Kind'] == 'method' and len(x.get('Calls') or []) == 2):
            p, why = plan_fill(x, cls, c_by_name)
            if p:
                plans.append(p)
            else:
                skipped[why.split(':')[0] + ': ' + why.split(':')[1].split()[0]] = \
                    skipped.get(why.split(':')[0] + ': ' + why.split(':')[1].split()[0], 0) + 1

    # native calls in sequence (MeshVertexList.Clear) (G54)
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x.get('Sequence')
                and x['Kind'] == 'method' and x.get('Body') in ('trivial', 'pattern')):
            p, why = plan_sequence(x, cls, c_by_name)
            if p:
                plans.append(p)
            else:
                skipped[why.split(':')[0]] = skipped.get(why.split(':')[0], 0) + 1

    # caller-allocated arrays the native fills (PointCloud.GetPoints) (G54)
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x.get('Body') == 'arrayfill'
                and x['Kind'] == 'method' and x.get('ArrayFill')):
            p, why = plan_arrayfill(x, cls, c_by_name)
            if p:
                plans.append(p)
            else:
                skipped[why.split(':')[0] + ':' + why.split(':')[1]] = skipped.get(why.split(':')[0] + ':' + why.split(':')[1], 0) + 1

    # tuples of geometry made from native pointers (Curve.Split, PolyCurve.Explode) (G72)
    for x in manifest['members']:
        if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x.get('Body') == 'wraptuple'
                and x['Kind'] == 'method' and x.get('WrapTuple')):
            p, why = plan_wraptuple(x, cls, c_by_name)
            if p:
                plans.append(p)
            else:
                skipped[why.split(':')[0] + ':' + why.split(':')[1]] = skipped.get(why.split(':')[0] + ':' + why.split(':')[1], 0) + 1

    # collection accessors (Mesh.Vertices => new MeshVertexList(this)): a
    # property whose type is a registered CHILD of this class returns a
    # view bound to this object's pointer
    basecls = BY_REG.get(cls.get('base', ''))
    self_ptr = ('static_cast<%s*>(m_ptr)' % cls['on']
                if basecls and basecls['mode'] not in ('abstract',) else 'm_ptr')
    # a child view of a VIEW parent (curve.Points on a File3dmObject's geometry)
    # gets the writable pointer: the parent copies on write right there, so
    # writes through the child land in the private copy, as in .NET
    self_ptr = self_access(cls, self_ptr, 'NonConstPointer')
    have = {p['m']['Name'] for p in plans if p['m']['Kind'] == 'property'}
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x['Name'] not in have):
            continue
        child = child_view(x)
        if (child and child['mode'] == 'child' and (child.get('scope') or {}).get('comp') is cls
                and x.get('ChildArgs') == ['this'] and cls.get('comp')):
            # `BrepFace.Loops => new BrepLoopList(this)` (G77): the parent's list scoped
            # to this component -- the owner's pointer and this component's key
            plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                          'call_exprs': [], 'locals': [], 'bind_args': [],
                          'shape': ('child', child), 'success': None, 'guards': [],
                          'child_scope': True, 'self_ptr': 'RH3DM_Owner()'})
            have.add(x['Name'])
            continue
        if child and child['mode'] == 'child' and child.get('parent') == cls['reg'] \
                and cls['mode'] in ('pointer', 'child'):
            plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                          'call_exprs': [], 'locals': [], 'bind_args': [],
                          'shape': ('child', child), 'success': None,
                          'child_key': child_key(x, child) if child.get('key') else None,
                          'child_state': child_state_values(x, child) if child.get('state') else None,
                          # `IsPhysicallyBased ? new T(this) : null`: an early return on
                          # the sibling property, resolved like any guard (zero-arg sibling)
                          'guards': ([{'Cond': '!%s' % x['ChildIf'], 'Ids': [x['ChildIf']], 'Exception': None,
                                       'Return': 'null', 'ret_cpp': 'nullptr'}] if x.get('ChildIf') else []),
                          # a child that views a PART of the parent (File3dmSettings:
                          # &model->m_settings, registry 'derive') derives it itself
                          # and keeps the parent too (its own file views need it)
                          'self_ptr': self_ptr})
            have.add(x['Name'])

    # file views (File3dmSettings.RenderSettings => new RenderSettings(m_parent)):
    # the property hands out a view of the FILE; the owner must carry the model
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x['Name'] not in have and x.get('Child')):
            continue
        target = BY_CS.get(cs_unqualify(x['Child']))
        if not (target and (target.get('proxy') or {}).get('NoId') and x.get('ChildArgKinds') == ['file']
                and cs_unqualify(x.get('Returns') or '') == target['cs']):
            continue
        if not (cls['mode'] == 'child' and cls.get('parent') == 'File3dmX'):
            skipped['fileview: owner has no model'] = skipped.get('fileview: owner has no model', 0) + 1
            continue
        plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                      'call_exprs': [], 'locals': [], 'bind_args': [],
                      'shape': ('fileview', target), 'guards': [], 'success': None})
        have.add(x['Name'])

    # Variant properties (the RDK settings): `using (var v = GetValue(E.M)) return v.ToX();`
    # and `using (var v = new Variant(value)) SetValue(E.M, v);` -- the natives
    # of the class's helpers and of the Variant wrapper, in that exact order
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x.get('VariantGet') and x['Name'] not in have):
            continue
        g, why = plan_variant(x, cls)
        if g is None:
            skipped['variant: ' + why] = skipped.get('variant: ' + why, 0) + 1
            continue
        plans.append(g)
        have.add(x['Name'])
        if x.get('VariantSet'):
            sp, swhy = plan_variant(x, cls, setter=True)
            if sp is None:
                skipped['variant setter: ' + swhy] = skipped.get('variant setter: ' + swhy, 0) + 1
            else:
                sp['setter_of'] = g
                g['setter'] = sp
                plans.append(sp)

    # settings views (RenderSettings.GroundPlane): a file view when the render
    # settings are the file's, else a pointer into these render settings
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x.get('SettingsView') and x['Name'] not in have):
            continue
        target = BY_CS.get(x['SettingsView']['New'])
        fn = C_BY_NAME.get(x['SettingsView']['Native'])
        if not (target and target.get('ff') and target.get('proxy') and fn and (cls.get('proxy') or {}).get('NoId')
                and fn['returns'].replace('const ', '') == target['on'] + '*'
                and cs_unqualify(x.get('Returns') or '') == target['cs']):
            skipped['settingsview: %s' % x['SettingsView']['New']] = skipped.get('settingsview: %s' % x['SettingsView']['New'], 0) + 1
            continue
        plans.append({'m': x, 'fn': fn, 'cls': cls, 'static': False,
                      'call_exprs': [], 'locals': [], 'bind_args': [],
                      'shape': ('settingsview', target, fn['name']), 'guards': [], 'success': None})
        have.add(x['Name'])

    # const views (File3dmObject.Geometry / .Attributes): no native call of their
    # own -- the result is a view of (this file, this id)
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x.get('ConstView') and x['Name'] not in have):
            continue
        sh = constview_shape(x['ConstView'], cls, cs_unqualify(x.get('Returns') or ''))
        if isinstance(sh, str):
            skipped['constview: ' + sh] = skipped.get('constview: ' + sh, 0) + 1
            continue
        plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                      'call_exprs': [], 'locals': [], 'bind_args': [],
                      'shape': sh, 'guards': [], 'success': None})
        have.add(x['Name'])

    # property setters: the manifest's Setter is the `set` accessor analyzed
    # like a method body, planned as `void set(T value)` and registered with
    # its getter as one read-write property (no getter, no setter: a
    # write-only property has no binding shape)
    for g in [p for p in plans if p['m']['Kind'] in ('property', 'indexer')]:
        st = g['m'].get('Setter')
        if not st:
            continue
        if not st.get('Calls') or len(st['Calls']) != 1:
            why = 'setter: body %s' % (st.get('Body') or 'unknown')
            skipped[why] = skipped.get(why, 0) + 1
            continue
        fn = c_by_name.get(st['Calls'][0]['Name'])
        if fn is None:
            skipped['setter: C fn not in manifest'] = skipped.get('setter: C fn not in manifest', 0) + 1
            continue
        # EVERYTHING the setter's analysis recorded travels (FailThrow, Success...):
        # a field left behind is logic silently dropped (G28)
        sm = dict(st)
        sm.update({'Type': g['m']['Type'], 'Variant': g['m']['Variant'],
                   'Kind': 'method', 'Name': g['m']['Name'],
                   'Static': g['m'].get('Static'),
                   'HolderArg': st.get('HolderArg', -1),
                   'Params': (list(g['m'].get('Params') or []) if g['m']['Kind'] == 'indexer' else [])
                             + [{'Name': 'value', 'Type': g['m']['Returns'], 'Mode': ''}],
                   'Returns': 'void'})
        sp, why = plan(sm, fn, cls)
        if sp is None:
            why = 'setter: ' + why.split(':')[0]
            skipped[why] = skipped.get(why, 0) + 1
            continue
        sp['setter_of'] = g
        g['setter'] = sp
        plans.append(sp)

    # constructions with no native call (G43; before the forwards: `Duplicate => DuplicatePolyCurve` targets one; G69): a list's `this[int]`, `new PointCloudItem(this, index)`
    have = {(p['m']['Kind'], p['m'].get('Signature')) for p in plans}
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x.get('Body') == 'construct'
                and not x.get('Calls') and x['Kind'] in ('method', 'property', 'indexer')
                and (x['Kind'], x['Signature']) not in have):
            continue
        c, why = plan_construct_nocall(x, cls)
        if c is None:
            skipped['construct: ' + why.split(' ')[0] + ' ' + (why.split(' ')[1] if ' ' in why else '')] = \
                skipped.get('construct: ' + why.split(' ')[0] + ' ' + (why.split(' ')[1] if ' ' in why else ''), 0) + 1
            continue
        plans.append(c)

    # delegating overloads (`IsPlanar() => IsPlanar(RhinoMath.ZeroTolerance)`,
    # `TrimInterval(Interval d) => TrimInterval(d.T0, d.T1)`) forward to the
    # generated target overload with RhinoCommon's own arguments
    # keyed by RhinoCommon arity: the delegating call passes every C# argument,
    # out-params included, though the binding drops those
    by_name_arity = {}
    for p in plans:
        cs_params = cs_signature(p['m'])[1]
        arity = len(cs_params) if cs_params is not None else len(p['bind_args'])
        by_name_arity.setdefault((p['m']['Name'], arity), []).append(p)
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] in ('method', 'property') and x.get('Body') == 'delegates'):
            continue
        f, why = plan_forward(x, cls, by_name_arity,
                              [m for m in manifest['members'] if m['Type'] == cls['type']])
        if f:
            plans.append(f)
        else:
            skipped['forward: ' + why] = skipped.get('forward: ' + why, 0) + 1

    # a component's key (`public int FaceIndex { get; }`, `Index => m_index`): the key itself.
    # On an abstract base of components (SubDComponent.Id) every derived component must
    # name it as its key, with one key type: the base asks the derived storage (virtual)
    keyinfo = cls.get('comp') or cls.get('key')
    cls.pop('abstract_key', None)
    if cls['mode'] == 'abstract':
        kids = [d for d in CLASSES if d.get('base') == cls['reg'] and d.get('comp')]
        if kids and len({d['comp']['Key'] for d in kids}) == 1:
            common = set.intersection(*(set(d['comp']['KeyNames']) for d in kids))
            if common:
                keyinfo = {'KeyNames': sorted(common), 'Key': kids[0]['comp']['Key'], 'abstract': True}
    if keyinfo:
        names = keyinfo.get('KeyNames') or keyinfo.get('Names')
        ktype = keyinfo.get('Key') or keyinfo.get('Type')
        for x in manifest['members']:
            if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x['Kind'] == 'property'
                    and x['Name'] in names and not x.get('Calls') and not x.get('Setter')
                    and x.get('Body') in ('pure', 'alias') and CS_ARG.get(cs_unqualify(x.get('Returns') or ''), ('',))[0] == ktype
                    and not any(p['m']['Name'] == x['Name'] for p in plans)):
                plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
                              'bind_args': [], 'shape': ('keyprop', ktype), 'extra_fns': [], 'guards': [],
                              'success': None, 'failthrow': None, 'typearg_pos': [], 'commits': False})
                if keyinfo.get('abstract'):
                    cls['abstract_key'] = ktype

    # literal constant properties (`IsReadOnly => true`; G48): the value itself
    for x in manifest['members']:
        c = x.get('Const') or ''
        if (x['Type'] == cls['type'] and x['Variant'] == 'portable' and x['Kind'] == 'property'
                and c.startswith('@literal:') and not x.get('Setter') and cls['mode'] != 'static'
                and not any(p['m']['Name'] == x['Name'] for p in plans)):
            ret = cs_unqualify(x.get('Returns') or '')
            lit = c[len('@literal:'):]
            if ret in ('bool', 'int', 'double', 'uint') and (lit in ('true', 'false')) == (ret == 'bool'):
                plans.append({'m': x, 'fn': None, 'cls': cls, 'static': bool(x.get('Static')), 'call_exprs': [], 'locals': [],
                              'bind_args': [], 'shape': ('constprop', CS_ARG[ret][0], lit.rstrip('dDfFuU')), 'extra_fns': [],
                              'guards': [], 'success': None, 'failthrow': None, 'typearg_pos': [], 'commits': False})

    # property chains (`File3dmObject.Name => Attributes.Name`): a property of
    # the object a sibling property returns, both generated -- the sibling here
    # (a view or child), the member on its class (planned earlier, PLANNED)
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x.get('Body') == 'chain'):
            continue
        c, why = plan_chain(x, cls, plans, x['Delegates'], setter=False)
        if c is None:
            skipped['chain: ' + why] = skipped.get('chain: ' + why, 0) + 1
            continue
        st = x.get('Setter') or {}
        if st.get('Body') == 'chain' and st.get('Delegates') == x['Delegates'] \
                and [a['Kind'] for a in st.get('DelegateArgs') or []] == ['param']:
            sp, swhy = plan_chain(x, cls, plans, x['Delegates'], setter=True)
            if sp is not None:
                sp['setter_of'] = c
                c['setter'] = sp
                plans.append(sp)
            else:
                skipped['chain setter: ' + swhy] = skipped.get('chain setter: ' + swhy, 0) + 1
        plans.append(c)

    # overloads: every plan gets a unique C++ method name; registration puts
    # them all under the one RhinoCommon name (pybind dispatches by type, the
    # JS side keeps one per arity -- embind dispatches on argument count).
    # An overload whose binding-side argument types duplicate an earlier one
    # (all-out members like DecomposeAffine collapse to zero args) is
    # unreachable in BOTH languages: dropped, counted.
    seen_sigs, dedup = set(), []
    for p in plans:
        key = (('set ' if p.get('setter_of') else '') + p['m']['Name'],
               tuple(t for t, _ in p['bind_args']))
        if key in seen_sigs:
            skipped['unreachable overload (same binding signature)'] = \
                skipped.get('unreachable overload (same binding signature)', 0) + 1
            continue
        seen_sigs.add(key)
        dedup.append(p)
    plans = dedup
    counts = {}
    for p in plans:
        n = ('set_' if p.get('setter_of') else '') + p['m']['Name']
        counts[n] = counts.get(n, 0) + 1
        p['cpp'] = 'Gen_' + n + ('' if counts[n] == 1 else '_%d' % counts[n])

    # `is T item && !item.IsDeleted` (G66): the proxy the factory made is kept only
    # when the target's generated bool getter says so
    for p in list(plans):
        rf = p['m'].get('ResultFilter')
        if not rf:
            continue
        g = generated_getter(p['shape'][1], rf) if p['shape'][0] == 'proxywrap' else None
        if g is None or plan_cpp_ret(g) != 'bool':
            plans.remove(p)
            skipped['result filter %s' % rf] = skipped.get('result filter %s' % rf, 0) + 1
            continue
        p['result_filter'] = g['cpp']

    # guard identifiers: own parameters pass, zero-arg siblings become calls
    zero_arg = {}
    for p in plans:
        if not p['bind_args'] and p['m']['Name'] not in zero_arg:
            zero_arg[p['m']['Name']] = p['cpp']
    # Guid operands of a guard (G114): Python's BND_UUID is a py::object, whose == / !=
    # compare object identity -- every Guid reads through RH3DM_ToUuid (either form)
    guid_calls = {c for n, c in zero_arg.items()
                  if any(q['cpp'] == c and (cs_signature(q['m'])[0] or '').split('.')[-1] == 'Guid' for q in plans)}
    resolved = []
    for p in plans:
        if p.get('adopt'):
            # the getter handing out the same holder (BrepFace.GetMesh) gives its resolver
            ad = p['adopt']
            ptypes = dict((n, t) for t, n in p['bind_args'])
            sib = [q for q in plans if q['shape'][0] == 'subview' and (q['m'].get('SubView') or {}).get('Holder') == ad['holder']
                   and len(q['m']['SubView']['Args']) == len(ad['args'])
                   and [ptypes.get(a) for a in ad['args'] if a != 'this'] == ([q['shape'][3]] if q['shape'][2] else [])]
            if len(sib) != 1:
                skipped['adopt: sibling view'] = skipped.get('adopt: sibling view', 0) + 1
                continue
            key = [a for a in ad['args'] if a != 'this']
            p['adopt'] = dict(ad, tramp='RH3DM_SubT_%s' % sib[0]['cpp'], key=key[0] if key else None,
                              cow=sib[0]['shape'][4] == 'copy')
        if p['shape'][0] == 'arrayfill' and (p['shape'][1].get('count_prop') or p['shape'][1].get('count_method')):
            # `int count = GroupCount;` / `SpanCount(direction) + 1`: the generated
            # int sibling of this class, taking exactly these parameters' types
            af = p['shape'][1]
            ptypes = dict((n, t) for t, n in p['bind_args'])
            if af.get('count_prop'):
                src = [q for q in plans if q['m']['Name'] == af['count_prop'] and q['m']['Kind'] == 'property'
                       and not q.get('setter_of') and not q['bind_args'] and q['shape'] == ('direct', 'int')]
                expr = src and '%s()' % src[0]['cpp']
            else:
                mname, margs = af['count_method']
                src = [q for q in plans if q['m']['Name'] == mname and q['m']['Kind'] == 'method'
                       and q['shape'] == ('direct', 'int') and not q.get('static')
                       and [t for t, _ in q['bind_args']] == [ptypes[a] for a in margs]]
                expr = len(src) == 1 and '%s(%s)' % (src[0]['cpp'], ', '.join(margs))
            if not src or not expr:
                skipped['arrayfill: count sibling'] = skipped.get('arrayfill: count sibling', 0) + 1
                continue
            if af.get('count_add'):
                expr += ' + ' + af['count_add']
            p['shape'] = ('arrayfill', dict(af, count_expr=expr))
        argnames = {n for _, n in p['bind_args']}
        ok = True
        kept = []
        # each plan resolves its OWN copy: an inherited generic member's guards
        # are shared by every concrete table, and their translation can depend
        # on the table (`ModelComponentType.Unset == ComponentType`)
        p['guards'] = [dict(g) for g in p['guards']]
        for g in p['guards']:
            # a pure null-check on a by-value marshaled param is enforced by
            # the binding boundary (pybind/embind reject None before the body)
            if g.get('NullCheckOf') and g['NullCheckOf'] in argnames and g.get('Exception'):
                continue
            kept.append(g)
            if 'cond_cpp' in g and g.get('SelfNull'):
                continue                      # resolved by plan(): the self pointer
            # File.Exists(path), the one call a guard may contain (specgen)
            cond = re.sub(r'\bFile\.Exists\s*\(', 'RH3DM_FileExists(', g['Cond'])
            cond = re.sub(r'\bnull\b', 'nullptr', cond)     # `null == curve`, curve now a pointer
            ptypes = {n: t for _, t, n in (cs_signature(p['m'])[1] or [])}
            cond = enum_isdefined_cpp(cond, cls, ptypes)
            if cond is None:
                ok = False
                break
            if g.get('Calls') and not g.get('Native'):
                # `null == other || other.ConstPointer() == ConstPointer()` (Mesh.Append; G73):
                # the argument IS this object -- its pointer against self's
                cond = self_identity_cpp(g, cond, cls, argnames, ptypes)
                if cond is None:
                    ok = False
                    break
            if g.get('Calls'):
                # `!ModelComponent.ModelComponentTypeRequiresUniqueName(type)`: each
                # helper call becomes its resolved native call (specgen Native), or refuse
                cond = guard_calls_cpp(g, cond, cls, argnames, ptypes, p)
                if cond is None:
                    ok = False
                    break
            # dotted ids first: `Count` must not rewrite the tail of `curve.Count`
            for ident in sorted(g['Ids'], key=lambda i: '.' not in i):
                if ident in argnames or ident in g.get('consumed', ()):
                    continue
                if '.' in ident:
                    a, b = ident.split('.', 1)
                    vals = g.get('IdValues') or {}
                    if a in CS_ENUMS and ident in vals:          # IsoStatus.None
                        rep = '(%s)%s' % (CS_ENUMS[a]['cpp'], vals[ident])
                        GUARD_ENUMS.add(a)
                    elif a in argnames and (ptypes.get(a), b) in VT_PROPS:
                        rep = '%s.%s()' % (a, b)                  # xform.IsIdentity
                    elif a in argnames and seq_elem(ptypes.get(a)) and b == 'Count':
                        rep = '((int)%s.size())' % a              # points.Count (G82)
                    else:
                        ok = False
                        break
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), rep, cond)
                elif ident in zero_arg:
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident),
                                  '%s()' % zero_arg[ident], cond)
                elif ident in {f for _, f in cls.get('state') or []}:
                    pass                    # `m_read_only`: a field the view keeps, same name (G60)
                elif no_owned_pointer_cpp(cls, ident) is not None:
                    # `IsDocumentControlled`: CommonObject's `IntPtr.Zero == m_ptr`
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), no_owned_pointer_cpp(cls, ident), cond)
                elif class_const_cpp(cls, ident) is not None:
                    # `ComponentType` -> (ON_ModelComponent::Type)5: this class's constant
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), class_const_cpp(cls, ident), cond)
                else:
                    ok = False
                    break
            if not ok:
                break
            g['cond_cpp'] = guid_operands(cond, dict(ptypes, **{n: t for t, n in p['bind_args'] if n not in ptypes}), guid_calls)
        fts = p['shape'][1] if p['shape'][0] == 'failthrow' else p.get('failthrow')
        if ok and fts and fts.get('Cond'):
            # `if (!CALL) { if (index < 0 || index >= Count) throw ...; }`:
            # the inner condition resolves exactly like a guard's
            ft = dict(fts)
            def ft_cond(c, ids):
                c = re.sub(r'\bnull\b', 'nullptr', c)
                for ident in ids or []:
                    if ident in argnames:
                        continue
                    if ident in zero_arg:
                        c = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), '%s()' % zero_arg[ident], c)
                    else:
                        return None
                return c
            cond = ft_cond(ft['Cond'], ft.get('Ids'))
            ok = cond is not None
            # PointCloud.PointAt: `if (!CALL) { if (C1) throw ...; if (C2) throw ...; }` (G49)
            more = []
            for g in ft.get('More') or []:
                mc = ft_cond(g['Cond'], g.get('Ids'))
                if mc is None:
                    ok = False
                    break
                more.append(dict(g, cond_cpp=mc))
            ft['More'] = more
            ft['cond_cpp'] = cond
            if p['shape'][0] == 'failthrow':
                p['shape'] = ('failthrow', ft)
            else:
                p['failthrow'] = ft
        if ok:
            p['guards'] = kept
            resolved.append(p)
        else:
            skipped['guard id'] = skipped.get('guard id', 0) + 1
    return resolved


def plan_chain(x, cls, plans, chain, setter):
    """`Attributes.Name`: sibling property `Attributes` (a generated view/child
    returning class T*), then T's generated property `Name` (its getter, or its
    setter for the chain's setter). Refuses when either half is not generated."""
    sib_name, _, mem = chain.partition('.')
    # `Location.X` (PointCloudItem; G67): the sibling returns a translated VALUE
    # type; the chain reads its translated property, or sets it on a copy and
    # writes the copy back through the sibling's setter -- RhinoCommon's own steps
    vsibs = [p for p in plans if p['m']['Kind'] == 'property' and p['m']['Name'] == sib_name
             and not p.get('setter_of') and plan_cpp_ret(p) in VT_CPP.values()]
    if len(vsibs) == 1:
        sib = vsibs[0]
        vt = {v: k for k, v in VT_CPP.items()}[plan_cpp_ret(sib)]
        if (vt, mem) not in VT_PROPS:
            return None, 'member %s.%s' % (vt, mem)
        if setter:
            if (vt, mem) not in VT_PROP_SET or sib.get('setter') is None:
                return None, 'setter %s.%s' % (vt, mem)
            return {'m': dict(x, Kind='method', Returns='void'), 'fn': None, 'cls': cls, 'static': False,
                    'call_exprs': [], 'locals': [], 'bind_args': [(VT_PROP_RET[(vt, mem)], 'value')],
                    'shape': ('chainvt', sib, mem, True), 'guards': [], 'success': None}, None
        return {'m': x, 'fn': None, 'cls': cls, 'static': False,
                'call_exprs': [], 'locals': [], 'bind_args': [],
                'shape': ('chainvt', sib, mem, False), 'guards': [], 'success': None}, None
    sibs = [p for p in plans if p['m']['Kind'] == 'property' and p['m']['Name'] == sib_name
            and not p.get('setter_of') and p['shape'][0] in ('constview', 'child')]
    if len(sibs) != 1:
        return None, 'sibling %s' % sib_name
    sib = sibs[0]
    target = sib['shape'][1]
    if target['mode'] == 'abstract':
        return None, 'sibling %s is an abstract view' % sib_name
    getter = generated_getter(target, mem)
    if getter is None:
        return None, 'member %s.%s' % (target['cs'], mem)
    if setter:
        sp = getter.get('setter')
        if sp is None:
            return None, 'setter %s.%s' % (target['cs'], mem)
        decl = None
        return {'m': dict(x, Kind='method', Returns='void'), 'fn': None, 'cls': cls, 'static': False,
                'call_exprs': [], 'locals': [], 'bind_args': list(sp['bind_args']),
                'shape': ('chain', sib, sp, True), 'guards': [], 'success': None}, None
    return {'m': x, 'fn': None, 'cls': cls, 'static': False,
            'call_exprs': [], 'locals': [], 'bind_args': [],
            'shape': ('chain', sib, getter, False), 'guards': [], 'success': None}, None


# the Variant's C# value types the binding carries, and how
VARIANT_TYPES = {
    'bool':     ('bool', None),           # (binding type, value-type C# name or None)
    'double':   ('double', None),
    'int':      ('int', None),
    'float':    ('float', None),          # LinearWorkflow.PreProcessGamma (G78)
    'Guid':     ('BND_UUID', None),
    'Vector2d': ('BND_Vector2dX', 'Vector2d'),
    'Vector3d': ('BND_Vector3dX', 'Vector3d'),
}


def plan_variant(x, cls, setter=False):
    ff = cls.get('ff') or {}
    wr = WRAPPER
    if not (ff.get('GetValueNative') and wr.get('New') and wr.get('Delete')):
        return None, 'no helpers'
    va = x['VariantSet' if setter else 'VariantGet']
    if va.get('WhichValue') is None:
        return None, 'which %s' % va['Which']
    cs = cs_unqualify(x.get('Returns') or '')
    emap = None
    if va.get('MapA'):
        # an enum mapped through an int (Dithering.Method; G78): the C# comparison as written
        key = next((k for k in ('%s.%s' % (cls['cs'], cs), cs) if k in CS_ENUMS), None)
        named = (ENUM_NAMED.get(key) or ENUM_NAMED.get(cs) or {}) if key else {}
        if key is None or va['MapA'] not in named or (not setter and va.get('MapB') not in named):
            return None, 'enum map %s' % cs
        GUARD_ENUMS.add(key)
        emap = {'cpp': CS_ENUMS[key]['cpp'], 'a': named[va['MapA']], 'b': named.get(va.get('MapB')),
                'eq': va.get('MapEq'), 'k1': va.get('MapK1'), 'k2': va.get('MapK2')}
        if any(not re.match(r'^-?\d+$', str(emap[k])) for k in (('eq', 'b') if not setter else ('k1', 'k2'))):
            return None, 'enum map values'
        cs = 'int'
    if cs not in VARIANT_TYPES:
        return None, 'type %s' % cs
    getfn = C_BY_NAME.get(ff['GetValueNative'])
    if not getfn or len(getfn['params']) != 3:
        return None, 'GetValue signature'
    which_t = getfn['params'][1]['type']
    bt, vt = VARIANT_TYPES[cs]
    if setter:
        if not (ff.get('SetValueNative') and ff.get('EqualNative')):
            return None, 'no SetValue/IsValueEqual'
        setn = wr['Set'].get(cs)
        if setn is None or (wr.get('SetConv') or {}).get(cs):
            return None, 'Variant.SetValue(%s)' % cs
        return {'m': dict(x, Kind='method', Returns='void'), 'fn': getfn, 'cls': cls, 'static': False,
                'call_exprs': [], 'locals': [], 'bind_args': [(emap['cpp'] if emap else bt, 'value')],
                'shape': ('vset', {'which': '(%s)%s' % (which_t, va['WhichValue']), 'set': setn, 'cs': cs,
                                   'vt': vt, 'get': ff['GetValueNative'], 'put': ff['SetValueNative'],
                                   'eq': ff['EqualNative'], 'map': emap}),
                'guards': [], 'success': None,
                'extra_fns': [C_BY_NAME[n] for n in (wr['New'], wr['Delete'], setn, ff['SetValueNative'], ff['EqualNative']) if n in C_BY_NAME]}, None
    get = (wr.get('Get') or {}).get(va['Conv'])
    if get is None or cs_unqualify(get['Returns']) != cs:
        return None, 'Variant.%s' % va['Conv']
    return {'m': x, 'fn': getfn, 'cls': cls, 'static': False,
            'call_exprs': [], 'locals': [], 'bind_args': [],
            'shape': ('vget', {'which': '(%s)%s' % (which_t, va['WhichValue']), 'conv': get, 'cs': cs, 'vt': vt,
                               'get': ff['GetValueNative'], 'map': emap}),
            'guards': [], 'success': None,
            'extra_fns': [C_BY_NAME[n] for n in (wr['New'], wr['Delete'], get['Native']) if n in C_BY_NAME]}, None


def table_protocol(cls, manifest, plans):
    """The language protocols of a File3dm component table (a concession,
    like value-type operators: not generated from a member). Granted only
    when the table has exactly what RhinoCommon's own protocols use:
      - `foreach`: the generic base's `GetEnumerator` delegates to
        `m_manifest.GetEnumerator` (File3dmManifestTable.GetEnumerator(type):
        ONX_ModelComponentIterator_New / GetNext / Delete, `Instantiate`)
      - `this[int]` (explicit IList<T>): the generated FindIndex
      - `Count`: the generated Count
    and the item type T is a proxy-capable registry class, so the iterator
    can construct `new T(id, file)` exactly as Instantiate does.
    Returns {item, type_value, count, find} or None."""
    filt = False                      # an `is T` enumerator: yields only the item type (G87)
    find_id = None                    # `Find(Guid g)` through the enumerator's find-by-id (G89)
    if cls['mode'] != 'child' or cls.get('parent') != 'File3dmX' or cls['on'] != 'ONX_Model':
        return None
    item = None
    enumer = False
    for bt, binding in base_chain(cls['type']):
        if TYPE_RECS[bt].get('TypeParams') and binding.get('T') and item is None:
            item = BY_CS.get(binding['T'])
        for x in manifest['members']:
            if (x['Type'] == bt and x['Name'] == 'GetEnumerator' and x['Kind'] == 'method'
                    and x.get('Body') == 'delegates'
                    and re.sub(r'<.*>$', '', x.get('Delegates') or '') == '@m_manifest.GetEnumerator'):
                enumer = True
    cv = class_const(cls['type'], 'ComponentType')
    if item is None and not enumer:
        # `GetEnumerator() => new File3dmEmbeddedFileEnumerator(_file3dm)` (G71): an internal
        # enumerator over the manifest for ONE component type (specgen ManifestEnumerator)
        ge = [x for x in manifest['members'] if x['Type'] == cls['type'] and x['Name'] == 'GetEnumerator'
              and x['Kind'] == 'method' and x['Variant'] == 'portable' and x.get('Body') == 'construct' and x.get('Child')]
        rec = TYPE_RECS.get(cls['type']) or {}
        if len(ge) == 1 and ge[0].get('ChildArgs') and ge[0]['ChildArgs'][0] in (rec.get('ParentFields') or []):
            # `new File3dmRenderContentEnumerator<File3dmRenderMaterial>(_file3dm)` (G87): generic, T bound here
            gm = re.match(r'^([\w.]+)<(\w+)>$', ge[0]['Child'])
            child = gm.group(1) if gm else ge[0]['Child']
            ens = [TYPE_RECS[t] for t in SIMPLE_TYPES.get(cs_unqualify(child), []) if (TYPE_RECS.get(t) or {}).get('ManifestEnumerator')]
            if len(ens) == 1:
                me = ens[0]['ManifestEnumerator']
                if me.get('FindById'):
                    finds = [x for x in manifest['members'] if x['Type'] == cls['type'] and x['Name'] == 'Find'
                             and x['Kind'] == 'method' and x['Variant'] == 'portable'
                             and [q['Type'] for q in x.get('Params') or []] in (['Guid'], ['System.Guid'])]
                    if len(finds) == 1:
                        find_id = finds[0]
                if me.get('TypeParam'):
                    item = BY_CS.get(gm.group(2)) if gm else None
                else:
                    item = BY_CS.get(cs_unqualify(me['Item']))
                filt = bool(me.get('Filter'))
                mm = re.match(r'^ModelComponentType\.(\w+)$', me['Type'])
                named = ENUM_NAMED.get('ModelComponentType') or {}
                if item is not None and mm and mm.group(1) in named:
                    cv = ('ModelComponentType.' + mm.group(1), str(named[mm.group(1)]))
                    enumer = True
    # the item's (file, id) view may be its proxy base's (File3dmRenderMaterial; G87)
    if item is None or not (item.get('proxy') or (BY_REG.get(item.get('base', '')) or {}).get('proxy')) or not enumer:
        return None
    if cv is None or cv[1] is None:
        return None
    for fn in ('ONX_ModelComponentIterator_New', 'ONX_ModelComponentIterator_GetNext',
               'ONX_ModelComponentIterator_Delete'):
        if fn not in C_BY_NAME:
            return None
    count = [p for p in plans if p['m']['Name'] == 'Count' and p['m']['Kind'] == 'property' and not p['bind_args']]
    find = [p for p in plans if p['m']['Name'] == 'FindIndex' and p['shape'][0] == 'proxywrap'
            and [t for t, _ in p['bind_args']] == ['int']]
    if len(count) > 1 or len(find) > 1 or (find and find[0]['shape'][1] is not item):
        return None
    if not count and not (TYPE_RECS.get(cls['type']) or {}).get('ParentFields'):
        return None                            # a table without Count: only the enumerator form (G71)
    types = [cv[1]]
    extras = (TYPE_RECS.get(cls['type']) or {}).get('ExtraComponentTypeValues') or []
    if extras and count:
        # `Count => base.Count + m_manifest.ActiveObjectCount(RenderLight)`: the
        # base's Count call once per type, summed; the iterator chains the types
        c = count[0]
        if c['shape'][0] != 'conv' or len(c.get('typearg_pos') or []) != 1 or c['locals']:
            return None
        pos = c['typearg_pos'][0]
        calls = []
        for v in types + extras:
            args = list(c['call_exprs'])
            args[pos] = '(ON_ModelComponent::Type)%s' % v
            calls.append(c['shape'][2].format(c='%s(%s)' % (c['fn']['name'], ', '.join(args))))
        c['shape'] = ('sum', 'int', calls)
        types = types + extras
    # no FindIndex (the object table): len and iteration, no [i]
    return {'item': item, 'type_value': cv[1], 'types': types, 'count': count[0]['cpp'] if count else None, 'find': find[0]['cpp'] if find else None,
            'filter': filt,
            # `Find(Guid g)` (G89): the walk's first item whose Id is g -- only when it returns the item
            'find_id': find_id['Name'] if find_id and cs_unqualify(find_id.get('Returns') or '') == item['cs'] else None,
            'fns': [C_BY_NAME[n] for n in ('ONX_ModelComponentIterator_New',
                                           'ONX_ModelComponentIterator_GetNext',
                                           'ONX_ModelComponentIterator_Delete')]}


def snapshot_iter(cls, plans, manifest):
    """RhinoCommon's SNAPSHOT enumerator (specgen Snapshot; G60: Decals.GetEnumerator
    -> `new DecalEnumerator(m_parent_attributes, m_rhino_doc_serial)`): the
    constructor's fill call copies the owner's items into an array of
    shared_ptr handles; each item yielded is a generated shared-handle class
    owning its own Copy of an element (the item constructor of that arity
    copies: specgen CopyArities); items whose bool property Skip is false are
    passed over; the snapshot's elements are freed with the item's Delete.
    The list is a generated child whose parent field is the owner argument,
    and Skip a generated zero-argument property of the item. Returns {item,
    snapshot: {fill, copy, delete, sp, skip}, count: None, find: None} or None."""
    if cls['mode'] != 'child' or cls.get('key'):
        return None
    rec = TYPE_RECS.get(cls['type']) or {}
    ges = [x for x in manifest['members'] if x['Type'] == cls['type'] and x['Name'] == 'GetEnumerator'
           and x['Kind'] == 'method' and x['Variant'] == 'portable' and x.get('Body') == 'construct' and x.get('Child')]
    if len(ges) != 1:
        return None
    ge = ges[0]
    ens = [TYPE_RECS[t] for t in SIMPLE_TYPES.get(cs_unqualify(ge['Child']), []) if (TYPE_RECS.get(t) or {}).get('Snapshot')]
    if len(ens) != 1:
        return None
    en = ens[0]['Snapshot']
    args = ge.get('ChildArgs') or []
    if en['OwnerParam'] >= len(args) or args[en['OwnerParam']] not in (rec.get('ParentFields') or []):
        return None
    item = BY_CS.get(cs_unqualify(en['Item']))
    sh = (item or {}).get('shared')
    ish = (TYPE_RECS.get((item or {}).get('type', '')) or {}).get('SharedHandle') or {}
    if not sh or not sh.get('Copy') or en['ItemArity'] not in (ish.get('CopyArities') or []) \
            or en['ElemDelete'] != sh['Delete'] or sh['Copy'] not in C_BY_NAME:
        return None
    fill = C_BY_NAME.get(en['Fill'])
    if not fill or fill.get('variant') == 'rhino-only' or fill['returns'] != 'void' \
            or [q['type'] for q in fill['params']] != ['const %s*' % cls['on'], 'ON_SimpleArray<%s>*' % sh['sp']]:
        return None
    skip = None
    if en.get('Skip'):
        sp = [q for q in PLANNED.get(item['reg'], []) if q['m']['Name'] == en['Skip'] and q['m']['Kind'] == 'property'
              and not q['bind_args'] and not q.get('static') and not q.get('setter_of') and q['m'].get('Returns') == 'bool']
        if len(sp) != 1:
            return None
        skip = sp[0]['cpp']
    return {'item': item, 'count': None, 'find': None,
            'snapshot': {'fill': fill, 'copy': C_BY_NAME[sh['Copy']], 'delete': C_BY_NAME[sh['Delete']],
                         'sp': sh['sp'], 'skip': skip}}


def index_iter(cls, plans, manifest):
    """RhinoCommon's index enumerator (G98: PostEffectCollection.GetEnumerator -> the internal
    PostEffectDataEnumerator: `p = At(owner, i++)` until zero, `Key(p, ref id)`, `new Item(owner,
    id)`): the owner is this generated class, the item a generated component of it keyed by a
    Guid, both natives in the C layer. Returns {item, index: {at, key}, count: None, find: None}."""
    if cls['mode'] != 'pointer':
        return None
    ges = [x for x in manifest['members'] if x['Type'] == cls['type'] and x['Name'] == 'GetEnumerator' and not x.get('Params')
           and x['Kind'] == 'method' and x['Variant'] == 'portable' and x.get('Body') == 'construct' and x.get('Child')
           and x.get('ChildArgKinds') == ['this']]
    if len(ges) != 1:
        return None
    ens = [TYPE_RECS[t] for t in SIMPLE_TYPES.get(cs_unqualify(ges[0]['Child']), []) if (TYPE_RECS.get(t) or {}).get('IndexEnum')]
    if len(ens) != 1:
        return None
    en = ens[0]['IndexEnum']
    item = BY_CS.get(cs_unqualify(en['Item']))
    if not item or not item.get('comp') or item['comp']['Owner'] is not cls or item['comp']['Key'] != 'ON_UUID':
        return None
    at, key = C_BY_NAME.get(en['At']), C_BY_NAME.get(en['Key'])
    if (not at or not key or at.get('variant') == 'rhino-only' or key.get('variant') == 'rhino-only'
            or [q['type'].replace('const ', '') for q in at['params']] != ['%s*' % cls['on'], 'int'] or not at['returns'].endswith('*')
            or [q['type'] for q in key['params']] != ['const %s*' % item['on'], 'ON_UUID*']):
        return None
    return {'item': item, 'index': {'at': at, 'key': key}, 'count': None, 'find': None}


def linked_iter(cls, plans):
    """RhinoCommon's linked enumerator over an owner's components (specgen
    Enumerator: SubDFaceList.GetEnumerator -> `p = First(subd, ref id); while
    (p != 0) { yield new SubDFace(subd, p, id); p = Next(p, ref id); }`): the
    list is a generated child of the owner, the element a generated component
    of that owner keyed by the id, and both natives exist. Returns {item,
    first, next, count, find: None} or None."""
    if cls['mode'] != 'child' or cls.get('key'):
        return None
    rec = TYPE_RECS.get(cls['type']) or {}
    en = rec.get('Enumerator')
    if not en or en.get('OwnerName') not in (rec.get('ParentFields') or []):
        return None
    item = BY_CS.get(cs_unqualify(en['Element']))
    if not item or not item.get('comp') or item['comp']['Owner']['reg'] != cls.get('parent') \
            or item['comp']['Key'] != 'unsigned int':
        return None
    first, nxt = C_BY_NAME.get(en['First']), C_BY_NAME.get(en['Next'])
    if not first or not nxt or [q['type'] for q in first['params']] != ['const %s*' % cls['on'], 'unsigned int*'] \
            or [q['type'] for q in nxt['params']] != [first['returns'], 'unsigned int*']:
        return None
    count = [p for p in plans if p['m']['Name'] == 'Count' and p['m']['Kind'] == 'property' and not p['bind_args']]
    return {'item': item, 'first': first, 'next': nxt,
            'count': count[0]['cpp'] if len(count) == 1 else None, 'find': None}


# ------------------------------------------------------------------ G63
# PURE members translated from RhinoCommon's C# (specgen translate, class mode):
# bodies built from the class's own members, value types, RhinoMath and enums.
# Their references to the class's members are markers resolved here against
# what was GENERATED -- a member resolving anything less is refused.
MARKER = re.compile(r'\$\$(get|sget|set|call|scall|enum|enumtype|class|classarg|new|index|oget|ocall|ocallc|oset|oindex|osget|oscall|acc|as|self|fieldptr|deref|optr|wrapptr|adopt|ocast|childlink|proxyfile|pcast|isview|viewcopy|fnew|filefield|ftable|owned|bget|bset|bcall|iter|ptrsof|state|hfield):([^$]*)\$\$')
VALUE_RETS = {'bool', 'double', 'float', 'int', 'uint', 'long', 'short', 'byte', 'string', 'void'}


CTORS, ARGCTORS = {}, {}      # reg -> has a generated default ctor / its argument-ctor plans (bnd_gen; G74)


# by-value C types a translated body's native may use (G84): anything else by value
# (a C# enum name like `TextureProjectionModes`) has no declaration here
C_SCALARS = {'void', 'bool', 'int', 'unsigned int', 'double', 'float', 'char', 'unsigned char', 'short',
             'unsigned short', 'long', 'unsigned long', 'long long', 'unsigned long long', 'ON__UINT64',
             'ON__INT64', 'ON__UINT32', 'ON__INT32', 'ON__UINT16', 'ON__INT16', 'ON__UINT8', 'ON__INT8', 'ON_UUID'}


_C_ENUMS = None
def c_layer_enums():
    """the enums the C layer declares (`enum TextRunTypeConsts : int { .. }` in on_text.cpp;
    G96): a translated body passes them as RH3DM_EnumArg and casts a result to its own enum"""
    global _C_ENUMS
    if _C_ENUMS is None:
        _C_ENUMS = set()
        for f in glob.glob(os.path.join(ROOT, 'src', 'librhino3dm_native', '*.cpp')) \
                + glob.glob(os.path.join(ROOT, 'src', 'librhino3dm_native', '*.h')):
            _C_ENUMS |= set(re.findall(r'\benum\s+(?:class\s+)?(\w+)\s*:\s*[\w ]+?\s*\{', open(f, errors='replace').read()))
    return _C_ENUMS


def _ff_class(cls):
    """a free-floating class (DocumentOrFreeFloatingBase; specgen FreeFloating): itself or a base"""
    c = cls
    while c is not None:
        if c.get('ff'):
            return True
        c = BY_REG.get(c.get('base', ''))
    return False


def native_usable(n):
    """a C function a translated class body may call (G84)"""
    f = C_BY_NAME.get(n)
    if f is None or f.get('variant') == 'rhino-only' or n in UNSAFE_NATIVES:
        return False
    for i, t in enumerate([f['returns']] + [q['type'] for q in f['params']]):
        t = (t or '').replace('const ', '').strip()
        # an opennurbs scoped enum PARAMETER (`ON_ModelComponent::Type`; G91): the translator
        # passes every enum argument as RH3DM_EnumArg, which converts to it
        # ... and an opennurbs namespace enum (`ON::AnnotationType`; G111: LinearDimension.Create)
        if i > 0 and re.match(r'^ON(?:_\w+)?::\w+$', t):
            continue
        # a C-layer enum (`enum TextRunBool`, `TextRunTypeConsts`; G96): a parameter RH3DM_EnumArg
        # converts to; a result the translator casts to the C# enum it stands for
        if re.match(r'^enum \w+$', t) or t in c_layer_enums():
            continue
        if not (t.endswith('*') or t in C_SCALARS or (t + '*') in C_VT or t.endswith('_STRUCT')):
            return False
    return True


OWNED_SHAPES = {'wrap', 'fill', 'proxywrap', 'construct', 'via', 'newobj', 'valadopt', 'subview', 'datafrom',
                'datafill', 'holderdata', 'selfcopy', 'child', 'fileview', 'settingsview', 'constview'}
BORROWED_SHAPES = {'mfget', 'cachedview'}


def returns_new(q):
    """whether the class pointer a plan returns is a NEW object the caller owns (G92): True,
    False (borrowed: held by the receiver), or None (unknown -- the caller refuses)"""
    sh = q['shape'][0]
    if q.get('ret_borrowed') or sh in BORROWED_SHAPES:
        return False
    if sh in OWNED_SHAPES or sh == 'translated':
        return True
    return None


def child_link(cls):
    """a File3dm proxy's parent link (G89: File3dmRenderContent's `(Guid, File3dmRenderContent
    parent)` ctor), on cls or a base: {'Field', 'Type'}, else None"""
    c = cls
    while c is not None:
        ch = (c.get('proxy') or {}).get('Child')
        if ch:
            return ch
        c = BY_REG.get(c.get('base', ''))
    return None


def modeled_ptr(cls, helper):
    """the modeled field (G88) whose pointer helper this is, on cls or a base"""
    c = cls
    while c is not None:
        for f, v in (c.get('modeled') or {}).items():
            if v['ptr'] == helper:
                return f
        c = BY_REG.get(c.get('base', ''))
    return None


def modeled_plans(cls, plans, manifest):
    """the modeled field's own property (G88: AnnotationBase.ParentDimensionStyle):
    the getter returns the stored object; the setter stores it -- a File3dm view as a
    light copy (a new view of the same file and id), as RhinoCommon does -- and sets
    the id property through its generated setter"""
    out = []
    for f, v in (cls.get('modeled') or {}).items():
        tgt = BY_CS.get(v['cs'])
        x = next((x for x in manifest['members'] if x['Type'] == cls['type'] and x['Name'] == v['prop']
                  and x['Kind'] == 'property'), None)
        ids = [p for p in plans if p['m']['Name'] == v['id_prop'] and p.get('setter_of')]
        idg = [q for c, q in _chain_plans(tgt, {c['reg']: PLANNED.get(c['reg'], []) for c in CLASSES})
               if q['m']['Name'] == 'Id' and q['m']['Kind'] == 'property' and not q.get('setter_of')] if tgt else []
        if tgt is None or x is None or len(ids) != 1 or not idg or not tgt.get('proxy'):
            continue
        g = {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [], 'bind_args': [],
             'guards': [], 'success': None, 'shape': ('mfget', f, tgt), 'cpp': 'Gen_' + v['prop']}
        s = {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
             'bind_args': [('const %s*' % tgt['gen'], 'value')], 'guards': [], 'success': None, 'setter_of': g,
             'shape': ('mfset', f, tgt, ids[0]['cpp'], idg[0]['cpp']), 'cpp': 'Gen_set_' + v['prop'], 'nonconst': True}
        g['setter'] = s
        out += [g, s]
    return out


def out_companion(q):
    if q['shape'][0] == 'translatedout':
        return q['shape'][3]                 # a translated member with outs: its `_out` already
    return _out_companion(q)


def companion_result(has_success):
    """the C++ type an out companion returns for a tuple led by an unconverted C# result
    (G109), else None"""
    if isinstance(has_success, tuple) and len(has_success) == 3 and has_success[0] == 'result' \
            and has_success[2] == '{c}':
        return has_success[1]
    return None


def _out_companion(q):
    """the C++ signature parts of a tuple member's companion taking its out
    arguments by reference and returning its success (G83): [(type, name, is_out)]
    in C# order, or None when the tuple is not that simple"""
    if q['shape'][0] != 'tuple' or q.get('static'):
        return None
    _, has_success, outs = q['shape']
    # a bool status, or a C# result passed through unconverted (G109: `uint SharpEdgeCount(out
    # SubDEdgeSharpness)`, ('result', 'unsigned int', '{c}')): the companion returns it as is
    if not (has_success is True or isinstance(has_success, str) or companion_result(has_success)):
        return None
    if any(g.get('Exception') is None or g.get('assign_cpp') for g in q['guards'] if not g.get('fail')):
        return None                          # an early return would not be the success flag
    bind = {n: t for t, n in q['bind_args']}
    out_t = {}
    for n, t in outs:
        if OUTLOCAL.get(t, (None, None))[1] != '{n}':
            return None                      # converted on the way out: not a plain reference
        dm = re.match(r'^(\S+) %s\b' % re.escape(n), OUTLOCAL[t][0].format(n=n))
        if dm is None:
            return None
        out_t[n] = dm.group(1)
    parts = []
    for qq in q['m'].get('Params') or []:
        if qq.get('Mode') == 'out':
            if qq['Name'] not in out_t:
                return None
            parts.append((out_t[qq['Name']], qq['Name'], True))
        elif qq['Name'] in bind:
            parts.append((bind[qq['Name']], qq['Name'], False))
        else:
            return None
    return parts if len(parts) == len(q['bind_args']) + len(outs) else None


def _args_fit(ps, want):
    """C# parameter types against a call's argument types: equal, numerically
    alike, or a class argument deriving from the parameter's class (G85:
    `Create(PolylineCurve pc, ...)` into `Create(Curve, ...)`)"""
    if ps == want or [_num(t) for t in ps] == [_num(t) for t in want]:
        return True
    for p, a in zip(ps, want):
        if p == a or _num(p) == _num(a) or cs_unqualify(p) == cs_unqualify(a):   # `FileIO.File3dm` is File3dm (G92)
            continue
        if _seq_of(p) and _seq_of(p) == _seq_of(a):
            continue                    # `Point3d[]` into `IEnumerable<Point3d>` (G94: Brep.CreateFromBox(Box))
        if a == 'null' and (cs_unqualify(p) in BY_CS or cs_unqualify(p) in DATA_CPP):
            continue                    # `ToByteArray(null)` (G101): null into a class or data-class parameter
        pc, ac = BY_CS.get(cs_unqualify(p)), BY_CS.get(cs_unqualify(a))
        if pc is None or ac is None or not derives_from(ac, pc):
            return False
    return True


def _seq_of(t):
    """the element of a C# sequence type (`Point3d[]`, `IEnumerable<Point3d>`, `List<Point3d>`), else None"""
    t = (t or '').replace(' ', '')
    m = re.match(r'^(?:[\w.]*\.)?(?:IEnumerable|List|IList|ICollection|IReadOnlyList)<([\w.]+)>$', t) or re.match(r'^([\w.]+)\[\]$', t)
    return cs_unqualify(m.group(1)) if m else None


def _value_ret(t):
    t = cs_unqualify(t or '')
    return t in VALUE_RETS or t in VT_CPP or t in CS_ENUMS


def cs_array_elem(t):
    """`TextRun[]` / `IEnumerable<TextRun>` / `List<TextRun>` -> 'TextRun' (G96), else None"""
    m = re.match(r'^(?:([\w.]+)\[\]|(?:[\w.]*\.)?(?:IEnumerable|List|IList|IReadOnlyList)<([\w.]+)>)$', (t or '').replace(' ', ''))
    return cs_unqualify(m.group(1) or m.group(2)) if m else None


def _class_ret(t, per_reg):
    """a generated class, returned as a pointer the caller owns (G74)"""
    c = BY_CS.get(cs_unqualify(t or ''))
    return c is not None and c['reg'] in per_reg and c['mode'] in ('pointer', 'abstract', 'child', 'value')


def _chain_plans(cls, per_reg):
    c = cls
    while c is not None:
        for p in per_reg.get(c['reg'], []):
            yield c, p
        c = BY_REG.get(c.get('base', ''))


_DERIVED = {}


def _overridden_below(cls, kind, name, ptypes, manifest):
    """a RhinoCommon type deriving from cls's declares the same member: a
    non-virtual C++ call from cls would miss it (refused)"""
    if _DERIVED.get('__manifest') is not manifest:
        _DERIVED.clear()
        _DERIVED['__manifest'] = manifest
        for t in TYPE_RECS:
            for b, _ in base_chain(t):
                _DERIVED.setdefault(b, set()).add(t)
        for x in manifest['members']:
            if x['Variant'] == 'portable' and x['Kind'] in ('method', 'property'):
                _DERIVED.setdefault(('m', x['Type']), set()).add(
                    (x['Kind'], x['Name'], tuple(q['Type'] for q in x.get('Params') or [])))
    sig = (kind, name, tuple(ptypes))
    return any(sig in _DERIVED.get(('m', t), ()) for t in _DERIVED.get(cls['type'], ()) if t != cls['type'])


def _virtual_overrides(owner, q, ptypes, per_reg, manifest):
    """the generated overrides of q (G89: AnnotationBase.GetBoundingBox(Transform) over
    GeometryBase's) when EVERY RhinoCommon type below owner that declares q's member has a
    generated class with a plan of the same C++ name and argument types; else None"""
    _overridden_below(owner, q['m']['Kind'], q['m']['Name'], ptypes, manifest)      # fills _DERIVED
    sig = (q['m']['Kind'], q['m']['Name'], tuple(ptypes))
    out = []
    for t in _DERIVED.get(owner['type'], ()):
        if t == owner['type'] or sig not in _DERIVED.get(('m', t), ()):
            continue
        c = BY_TYPE.get(t)
        hits = [p for p in per_reg.get(c['reg'], []) if p['cpp'] == q['cpp'] and not p.get('setter_of')
                and [a for a, _ in p['bind_args']] == [a for a, _ in q['bind_args']]
                and (p['m']['Kind'], p['m']['Name'], tuple(x['Type'] for x in p['m'].get('Params') or [])) == sig] if c else []
        if len(hits) != 1:
            return None
        out.append(hits[0])
    return out


def _num(t):
    return 'num' if t in ('double', 'float', 'int', 'uint', 'long', 'short', 'byte') else t


# G112: the arguments the last resolve_markers() passed to a BORROWING setter (a modeled field's,
# shape mfget: the object stores the pointer), as their C++ text; translated_pass reads it at once
RESOLVE_BORROWS = []
# G115: C functions the last resolve_markers() needs declared beyond the body's own natives (a value
# adopted from a native object is released with its type's Delete); translated_pass reads it at once
RESOLVE_FNS = []
# G117: (argument text, callee takes a pointer) for each argument the last resolve_markers() passed to a
# planned call -- a class parameter only ever forwarded to pointer parameters is nullable itself
RESOLVE_ARGUSE = []


def _addr_args(res):
    """`NAME\\x00ADDR?(arg)` -> `NAME(RH3DM_AddrOf(arg))`: a pointer-taking class setter's argument (G112);
    borrowed arguments are recorded; None when the call has no plain argument list"""
    while '\x00ADDR' in res:
        i = res.index('\x00ADDR')
        flag, j = res[i + 5], i + 6
        if j >= len(res) or res[j] != '(':
            return None
        depth, k = 0, j
        while k < len(res):
            depth += {'(': 1, ')': -1}.get(res[k], 0)
            if depth == 0:
                break
            k += 1
        if k >= len(res):
            return None
        inner = res[j + 1:k]
        if flag == 'B':
            RESOLVE_BORROWS.append(inner.strip())
        res = res[:i] + '(RH3DM_AddrOf(' + inner + '))' + res[k + 1:]
    return res


def _split_args(s):
    """a C++ argument list's top-level arguments"""
    out, depth, cur = [], 0, ''
    for ch in s:
        if ch in '(<[{':
            depth += 1
        elif ch in ')>]}':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(cur.strip()); cur = ''
        else:
            cur += ch
    if cur.strip():
        out.append(cur.strip())
    return out


def _arg_uses(res):
    """`NAME\\x00ARGSPR\\x00(args)` -> `NAME(args)`, recording each argument with whether the
    callee's parameter at its position is a pointer (G117); the flags may be followed by a
    `\\x00KEEP..\\x00` marker, kept for _keep_args"""
    while '\x00ARGS' in res:
        i = res.index('\x00ARGS')
        e = res.index('\x00', i + 1)
        flags = res[i + 5:e]
        j = e + 1
        if res.startswith('\x00KEEP', j):
            j = res.index('\x00', j + 1) + 1
        if j < len(res) and res[j] == '(':
            depth, k = 0, j
            while k < len(res):
                depth += {'(': 1, ')': -1}.get(res[k], 0)
                if depth == 0:
                    break
                k += 1
            for a, f in zip(_split_args(res[j + 1:k]), flags):
                RESOLVE_ARGUSE.append((a, f == 'P'))
        res = res[:i] + res[e + 1:]
    return res


def _keep_args(res):
    """`NAME\\x00KEEP1,2\\x00(args)` -> `NAME(args)`, recording the arguments at those positions as
    borrowed (G116), unwrapped to the name the body passed (`RH3DM_ClassArg(dimstyle)` -> dimstyle);
    None when the call has no plain argument list"""
    while '\x00KEEP' in res:
        i = res.index('\x00KEEP')
        e = res.index('\x00', i + 1)
        pos = [int(x) for x in res[i + 5:e].split(',') if x]
        j = e + 1
        if j >= len(res) or res[j] != '(':
            return None
        depth, k = 0, j
        while k < len(res):
            depth += {'(': 1, ')': -1}.get(res[k], 0)
            if depth == 0:
                break
            k += 1
        if k >= len(res):
            return None
        args = _split_args(res[j + 1:k])
        for p in pos:
            if p >= len(args):
                return None
            a = args[p]
            m = re.match(r'^(?:RH3DM_ClassArg|RH3DM_AddrOf)\((.*)\)$', a)
            a = (m.group(1) if m else a).strip().lstrip('&*').strip('() ')
            RESOLVE_BORROWS.append(a)
        res = res[:i] + res[e + 1:]
    return res


def resolve_markers(text, cls, per_reg, manifest):
    """the C++ text with every marker resolved, the plans it calls, and whether
    it writes (a setter call); or (None, reason, None)"""
    deps, writes, byvalue = [], False, False
    del RESOLVE_FNS[:]
    tags = {}                       # G92: `#k` on a call marker -> the plan it resolved to
    out, pos = [], 0
    for mm in MARKER.finditer(text):
        out.append(text[pos:mm.start()])
        pos = mm.end()
        kind, arg = mm.group(1), mm.group(2)
        arg, _, tag = arg.partition('#')      # an owned temporary's callee (G92)
        if kind == 'owned':
            # `std::unique_ptr<T> x_owned($$owned:k$$ ? x : nullptr)` (G92): the temporary is the
            # body's to free when its callee returned a NEW object; a borrowed one is not
            out.append('\x00OWN%s\x00' % arg)      # decided after the loop: the tag may come later (G92)
            continue
        if kind == 'hfield':
            # a handle class's pointer field (G107: DracoCompression.m_ptrDraco): its own m_ptr
            if cls is None or (cls.get('handle') or {}).get('Field') != arg:
                return None, 'handle field %s' % arg, None
            out.append('((void*)(m_ptr))')
            continue
        if kind == 'state':
            # a field RhinoCommon's constructor stores (G103: File3dmViewTable.m_named_views): the
            # generated class keeps it when the registry lists it as `state` (G60); else refused
            st = {f for _, f in (storage_owner(cls).get('state') or [])} if cls else set()
            if arg not in st:
                return None, 'state field %s' % arg, None
            out.append(arg)
            continue
        if kind == 'filefield':
            # a File3dm field (G92): a table's file is its own pointer (a child of File3dmX); a
            # File3dm proxy's (Layer.m_onx_model) the model it resolves in
            fp = (TYPE_RECS.get(storage_owner(cls)['type']) or {}).get('FileProxy') or {} if cls else {}
            if cls is not None and cls['mode'] == 'child' and cls.get('parent') == 'File3dmX':
                out.append('((void*)(%s))' % self_access(cls, self_expr_of(cls), 'ConstPointer'))
            elif cls is not None and (storage_owner(cls).get('proxy') or {}).get('Origin') == 'file' and fp.get('FileField') == arg:
                out.append('m_model')
            else:
                return None, 'filefield %s' % arg, None
            continue
        if kind == 'iter':
            # `foreach (var obj in m_parent.Objects)` (G92): the table's generated enumerator,
            # yielding exactly the element class the body declared
            tn, _, en = arg.partition(':')
            c2 = BY_CS.get(tn)
            # probed on COPIES: table_protocol rewrites the object table's Count plan (conv -> sum)
            tp = table_protocol(c2, manifest, [dict(p) for p in per_reg.get(c2['reg'], [])]) if c2 is not None and c2['reg'] in per_reg else None
            if tp is None or tp['item']['cs'] != en:
                return None, 'iter %s' % arg, None
            out.append('%sIter' % c2['gen'])
            continue
        if kind == 'ftable':
            # `file.AllLayers` (G92): the File3dm property's table, made from the model
            f3 = BY_REG.get('File3dmX')
            hit = [p for p in per_reg.get('File3dmX', []) if p['m']['Name'] == arg and p['m']['Kind'] == 'property'
                   and p['shape'][0] == 'child'] if f3 else []
            tc = hit[0]['shape'][1] if hit else None
            if tc is None or tc.get('parent') != 'File3dmX' or tc['on'] != 'ONX_Model':
                return None, 'ftable %s' % arg, None
            out.append(tc['gen'])
            continue
        if kind == 'self':
            # the class's own pointer (G84), as RhinoCommon's ConstPointer/NonConstPointer
            if cls is None:
                return None, 'self outside a class', None
            nc = arg == 'NonConst'
            out.append('((void*)(%s))' % self_access(cls, self_expr_of(cls), 'NonConstPointer' if nc else 'ConstPointer'))
            writes = writes or nc
            continue
        if kind == 'childlink':
            # `if (m__parent is File3dmRenderContent parent)` (G89): a child proxy's parent link
            # (specgen FileProxy ChildField) -- the light copy of the parent it holds, or null
            field, _, tn = arg.partition(':')
            ch = child_link(cls)
            if ch is None or ch['Field'] != field or ch['Type'] != tn:
                return None, 'childlink %s' % arg, None
            out.append('m_child_parent.get()')
            continue
        if kind == 'fnew':
            # `new Layer(id, file)` (G91: ManifestTable.Instantiate): the File3dm proxy of
            # (model, id); `new BitmapEntry(index, file)`: the hand class standing in for it
            tn, _, a0 = arg.partition(':')
            st = HAND_STANDINS.get(tn)
            if st is not None and [a0] == st['args']:
                out.append('new %s' % st['gen'])
                if st.get('rebind'):
                    # `(parent, index)` -> `(parent, id)`: the argument list right after the marker
                    am = re.match(r'\(([^()$]*), %s\)' % re.escape(st['rebind'][0]), text[pos:])
                    if am is None:
                        return None, 'fnew %s rebind' % arg, None
                    out.append('(%s, %s)' % (am.group(1), st['rebind'][1]))
                    pos += am.end()
                continue
            c2 = BY_CS.get(tn)
            pr = (storage_owner(c2) if c2 else {}).get('proxy') or {}      # a derived kind's proxy is its base's
            if (c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract')
                    or pr.get('Origin') != 'file' or pr.get('NoId') or a0 not in ('Guid', 'System.Guid')):
                return None, 'fnew %s' % arg, None
            out.append('new %s' % c2['gen'])
            continue
        if kind == 'ocast':
            # `(T)manifest.FindIndex(i, type)` / `... as T` (G122): the owned result as the derived
            # class -- the wrong class is deleted, then (T) throws and `as T` gives null
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract'):
                return None, 'ocast %s' % arg, None
            out.append('RH3DM_OwnedCast<%s>' % c2['gen'])
            continue
        if kind in ('pcast', 'isview', 'viewcopy'):
            # TryConvertBrep (G89): `geometry as Brep` on a parameter (a non-owning cast),
            # `IsDocumentControlled` (a File3dm view, owning nothing), and the view handed back
            # as a NEW view of the same object (D-identity: never the caller's own wrapper)
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract'):
                return None, '%s %s' % (kind, arg), None
            if kind == 'pcast':
                out.append('dynamic_cast<const %s*>' % c2['gen'])
                continue
            if not view_copyable(c2):
                return None, '%s %s' % (kind, arg), None
            out.append('->RH3DM_IsView()' if kind == 'isview' else 'RH3DM_ViewCopyOf')
            continue
        if kind == 'proxyfile':
            # `parent.File3dmParent` (G89): the file a File3dm proxy resolves in (null when owned)
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg or (c2.get('proxy') or {}).get('Origin') != 'file' or (c2.get('proxy') or {}).get('NoId'):
                return None, 'proxyfile %s' % arg, None
            out.append('m_model')
            continue
        if kind == 'fieldptr':
            # `m_mesh.NonConstPointer()` in a child view (G85): the parent's pointer is the view's
            if cls is None or cls['mode'] != 'child':
                return None, 'fieldptr %s' % arg, None
            nc = arg.endswith(':NonConst')
            out.append('((void*)(%s))' % self_access(cls, self_expr_of(cls), 'NonConstPointer' if nc else 'ConstPointer'))
            writes = writes or nc
            continue
        if kind == 'enumtype':
            e = CS_ENUMS.get(arg)
            if e is None:
                return None, 'enum type %s' % arg, None
            GUARD_ENUMS.add(arg)
            out.append(e['cpp'])
            continue
        if kind == 'enum':
            key, member = arg.split(':', 1)
            e = CS_ENUMS.get(key)
            named = ENUM_NAMED.get(key) or ENUM_NAMED.get(key.split('.')[-1]) or namespace_enum_named(key) or {}
            if e is None or member not in named:
                return None, 'enum %s.%s' % (key, member), None
            GUARD_ENUMS.add(key)
            out.append('(%s)%s' % (e['cpp'], named[member]))
            continue
        # class-typed values (G74): another generated class as a type, its construction,
        # and its members on a class-typed parameter (`ocallc`: a const reference) or local
        if kind == 'wrapptr':
            # `new Mesh(ptr, null)` / `CreateGeometryHelper(ptr, null)` (G86): the native's new
            # object, owned, as the most-derived generated class (null stays null)
            c2 = BY_CS.get(arg)
            if (c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract') or c2.get('comp')
                    or c2.get('handle') or c2.get('borrowed') or c2.get('shared')):
                return None, 'wrapptr %s' % arg, None
            out.append('RH3DM_Wrap_%s' % c2['reg'])
            continue
        if kind == 'adopt':
            # `new TextRun(ptr)` (G96): a handle class adopting the native's object (its
            # constructor stores the pointer; its Delete releases it) -- null stays null
            c2 = BY_CS.get(arg)
            # `new Font(ptr)` (G115): a managed class wraps the native's object without owning it
            # (a managed font lives as long as the process: RhinoCommon's Font(IntPtr) contract)
            rec = TYPE_RECS.get(c2['type']) or {} if c2 is not None else {}
            h = rec.get('Handle') or {}
            if (c2 is not None and c2['reg'] in per_reg and c2['mode'] == 'value' and h.get('Delete') in C_BY_NAME
                    and rec.get('PtrCtor') == 'field:%s' % h.get('Field')):
                # `new EarthAnchorPoint(ptr)` on a class stored BY VALUE (G115, as G49's valadopt):
                # copy the native's new object in, then release it with the type's own Delete
                RESOLVE_FNS.append(C_BY_NAME[h['Delete']])
                out.append('RH3DM_ValAdopt<%s, %s, %s>' % (c2['gen'], c2['on'], h['Delete']))
                byvalue = True
                continue
            if (c2 is not None and c2['reg'] in per_reg and c2['mode'] == 'pointer'
                    and not (c2.get('handle') or c2.get('managed') or c2.get('comp') or c2.get('borrowed') or c2.get('shared'))
                    and (rec.get('PtrCtor') or '').split('|')[0] == 'own'):
                # `new Material(ptr)` (G122): T(IntPtr) takes ownership (ConstructNonConstObject) --
                # the generated class owns the native's new object the same way
                out.append('RH3DM_Wrap_%s' % c2['reg'])
                continue
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] != 'pointer' or not (c2.get('handle') or c2.get('managed')):
                return None, 'adopt %s' % arg, None
            out.append('RH3DM_Adopt<%s, %s>' % (c2['gen'], c2['on']))
            continue
        if kind == 'ptrsof':
            # `new SimpleArrayCurvePointer(curves)` (G95): each object's own const pointer, as its
            # class's self access spells it (a view resolves) -- borrowed for the call
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract'):
                return None, 'ptrsof %s' % arg, None
            sa = self_access(c2, 'o->m_ptr' if c2['mode'] == 'pointer' else None, 'ConstPointer')
            sa = re.sub(r'(?<![\w>.])(RH3DM_NonConst|RH3DM_Const|%s)\(' % re.escape(c2.get('getter') or 'RH3DM_none'), r'o->\1(', sa)
            out.append('RH3DM_PtrsOf<%s>([](const %s* o) { return (const void*)(%s); })' % (c2['on'], c2['gen'], sa))
            continue
        if kind == 'optr':
            # `nc.NonConstPointer()` on a class local/parameter (G86): its pointer, as its
            # class's own self access spells it (views resolve or copy on write)
            tn, _, acc = arg.partition(':')
            c2 = BY_CS.get(tn)
            if c2 is not None and c2['reg'] in per_reg and c2['mode'] == 'value' and acc == 'Const' and not c2.get('fileview'):
                # `value.ConstPointer()` on a class stored BY VALUE (G115: File3dm.EarthAnchorPoint's
                # setter): its stored object, read only -- a NonConstPointer() write into a by-value
                # copy would be lost, so that stays refused
                out.append('RH3DM_ValPtr')
                continue
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract'):
                return None, 'optr %s' % tn, None
            sa = self_access(c2, 'o->m_ptr' if c2['mode'] == 'pointer' else None, 'NonConstPointer' if acc == 'NonConst' else 'ConstPointer')
            sa = re.sub(r'(?<![\w>.])(RH3DM_NonConst|RH3DM_Const|%s)\(' % re.escape(c2.get('getter') or 'RH3DM_none'), r'o->\1(', sa)
            # a null object throws, as C#'s `x.ConstPointer()` does (G115: `font.ConstPointer()` in
            # AnnotationBase.Font's setter, which Python could pass None)
            nul = 'if (!o) throw std::runtime_error("null reference"); '
            out.append('([](const %s* o) { %sreturn (void*)(%s); })' % (c2['gen'], nul, sa) if acc == 'Const'
                       else '([](%s* o) { %sreturn (void*)(%s); })' % (c2['gen'], nul, sa))
            continue
        if kind == 'deref':
            # a class local passed by reference (G85)
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg:
                return None, 'class %s' % arg, None
            out.append('' if c2['mode'] == 'value' and not c2.get('fileview') else '*')
            continue
        if kind in ('class', 'classarg', 'acc', 'as'):
            c2 = BY_CS.get(arg)
            if c2 is None or c2['reg'] not in per_reg or c2['mode'] not in ('pointer', 'abstract', 'child', 'value'):
                return None, 'class %s' % arg, None
            # a value-mode class (MeshingParameters) is held by value: `.`, no null -- unless it can be
            # a file view (G104: ViewInfo), handed out as a new object like a pointer class
            val = c2['mode'] == 'value' and not c2.get('fileview')
            byvalue = byvalue or (val and kind != 'classarg')
            if kind == 'class':
                out.append(c2['gen'] if val else '%s*' % c2['gen'])
            elif kind == 'classarg':
                out.append('const %s&' % c2['gen'])
            elif kind == 'acc':
                out.append('.' if val else '->')
            else:
                if val:
                    return None, 'as %s (a value)' % arg, None
                out.append('RH3DM_As<%s>' % c2['gen'])
            continue
        ocls, tcls = cls, None
        base_q = kind in ('bget', 'bset', 'bcall')
        if base_q:
            # `base.Name` (G92): the base generated class's own member, called non-virtually
            ocls = BY_REG.get(cls.get('base', '')) if cls is not None else None
            if ocls is None:
                return None, '%s %s (no generated base)' % (kind, arg), None
            kind = {'bget': 'get', 'bset': 'set', 'bcall': 'call'}[kind]
        if kind in ('new', 'oget', 'ocall', 'ocallc', 'oset', 'oindex', 'osget', 'oscall'):
            tname, _, arg = arg.partition(':')
            tcls = BY_CS.get(tname)
            if tcls is None or tcls['reg'] not in per_reg:
                return None, 'class %s' % tname, None
            ocls = tcls
        if kind == 'new':
            # `new T(args)` (G74): T's generated default constructor, or an argument
            # constructor taking exactly these C# types (bnd_gen stashes both)
            want = [t for t in arg.split(',') if t]
            if not want:
                if not CTORS.get(tcls['reg']):
                    return None, 'new %s' % tname, None
            else:
                # a sequence is its element, however the namespace is spelled (G82)
                norm = lambda t: ('seq:' + seq_elem(t)) if seq_elem(t) else t
                hits = [q for q in ARGCTORS.get(tcls['reg']) or []
                        if [norm(qq['Type']) for qq in q['m'].get('Params') or []] == [norm(t) for t in want]
                        and len(q['bind_args']) == len(want)]
                ch = child_link(tcls)
                if not hits and ch is not None and len(want) == 2 and want[0] in ('Guid', 'System.Guid') and want[1] == ch['Type']:
                    hits = ['child']      # `new File3dmRenderMaterial(child_id, parent)` (G89): the child proxy ctor
                if (not hits and tcls['mode'] == 'child' and tcls.get('state') and BY_REG.get(tcls.get('parent', ''))
                        and len(want) == 1 + len(tcls['state']) and cs_unqualify(want[0]) == BY_REG[tcls['parent']]['cs']
                        and [('ON_UUID' if t in ('Guid', 'System.Guid') else CS_ARG.get(t, (None,))[0]) for t in want[1:]]
                            == [t for t, _ in tcls['state']]):
                    # `new File3dmPlugInData(m_parent, id)` (G104): a child's (parent, state...) constructor
                    out.append('new %s' % tcls['gen'])
                    continue
                if not hits and tcls.get('fileview') and want == tcls['fileview']['ctor']:
                    # `new ViewInfo(file, id, ptr, named)` (G104): the file view constructor
                    out.append('new %s' % tcls['gen'])
                    continue
                if len(hits) != 1:
                    return None, 'new %s(%s)' % (tname, arg), None
            tval = tcls['mode'] == 'value' and not tcls.get('fileview')
            out.append(('%s' if tval else 'new %s') % tcls['gen'])
            byvalue = byvalue or tval
            continue
        name, _, types = arg.partition(':') if kind not in ('index', 'oindex') else ('', '', arg)
        if kind == 'get' and name == 'CppPointer' and cls is not None and (_ff_class(cls) or cls.get('comp')):
            # DocumentOrFreeFloatingBase.CppPointer (G97: Sun.GetValue): the free-floating object or
            # the file's, as the class's own accessor resolves it each time (the `()` that follows calls it)
            out.append('(void*)RH3DM_Const')
            continue
        if kind == 'call' and name == 'Commit' and not types and cls is not None and (storage_owner(cls).get('proxy') or {}).get('Commit'):
            # `Commit()` in a translated body (G116: RenderSettings.SetRenderEnvironmentId): the private
            # copy written back into the file, as the pattern planner's `commits` does
            out.append('RH3DM_Commit')
            writes = True
            continue
        if kind == 'call' and not types and cls is not None and modeled_ptr(cls, name):
            # `ConstParentDimStylePointer()` in a translated body (G88/G89): the modeled field's
            # pointer, as an IntPtr (the `()` that follows calls the helper)
            out.append('(void*)RH3DM_MfPtr_%s' % modeled_ptr(cls, name))
            continue
        if kind in ('index', 'oindex'):
            name = 'this[]'
        want = [t for t in types.split(',') if t] if kind in ('call', 'scall', 'ocall', 'ocallc', 'oscall', 'index', 'oindex') else []
        found = None
        for owner, q in _chain_plans(ocls, per_reg):
            m = q['m']
            static = kind in ('sget', 'scall', 'osget', 'oscall')
            if kind not in ('index', 'oindex') and (m['Name'] != name or q.get('static', False) != static):
                continue
            if kind in ('get', 'sget', 'oget', 'osget'):
                ok = m['Kind'] == 'property' and not q.get('setter_of') and not q['bind_args']
            elif kind in ('set', 'oset'):
                # a setter plan carries its accessor as a `method` record (plan's setter path)
                ok = bool(q.get('setter_of')) and q['setter_of']['m']['Kind'] == 'property'
            else:
                ps = [qq['Type'] for qq in m.get('Params') or []]
                kinds = 'indexer' if kind in ('index', 'oindex') else 'method'
                ok = (m['Kind'] == kinds and len(ps) == len(want) == len(q['bind_args'])
                      and _args_fit(ps, want))
                # `TryGetArc(out arc, tolerance)` (G83): a tuple-returning sibling called with
                # its out arguments -- through its C++ companion taking them by reference
                if (not ok and kind == 'call' and m['Kind'] == 'method' and len(ps) == len(want)
                        and _args_fit(ps, want)
                        and out_companion(q) is not None):
                    ok = 'out'
            # a null argument (G101) fits only a parameter bound as a pointer: a reference parameter's
            # `p == null` was folded to false (G74), so its callee has no null arm to reach
            if ok and any(w == 'null' and not str(ba[0]).rstrip().endswith('*')
                          for w, ba in zip(want, q['bind_args'])):
                ok = False
            if ok and kind == 'ocallc' and (q.get('nonconst') or q.get('setter_of')):
                return None, 'ocallc %s on a const parameter' % name, None
            # a hidden helper's IntPtr (G91: GetConstOnComponentManifestPtr) is a void* here; a
            # Guid result is read through RH3DM_ToUuid (G91)
            # an array a native fills (G92: GetGroupList), read as a C++ vector companion
            if (ok is True and kind in ('call', 'ocall', 'ocallc') and (q.get('shape') or ('',))[0] == 'arrayfill'
                    and q['shape'][1]['form'] != 'loop' and not q['shape'][1].get('conv')):
                ok = 'vec'
            if ok and (ok == 'vec' or kind in ('set', 'oset') or _value_ret(m.get('Returns')) or _class_ret(m.get('Returns'), per_reg)
                       # a class sequence (G96: `GetTextRuns()` as a LINQ source): only on a tagged
                       # call -- the caller frees its elements by the plan's ownership ($$owned$$)
                       or (tag and _class_ret((cs_array_elem(m.get('Returns')) or ''), per_reg))
                       or (q.get('hidden') and cs_unqualify(m.get('Returns') or '') == 'IntPtr')
                       # a C-layer enum (an int; G96) or a Variant (RH3DM_Variant, by value) from a
                       # hidden helper (G97: Sun.SunSettingFromDateTimeKind / GetValue)
                       or (q.get('hidden') and re.match(r'^UnsafeNativeMethods\.\w+$', m.get('Returns') or ''))
                       or (q.get('hidden') and cs_unqualify(m.get('Returns') or '') == 'Variant')
                       or cs_unqualify(m.get('Returns') or '') == 'Guid'
                       # System.Drawing.Rectangle (G105: GetScreenPort() => GetScreenPort(out near, out far)):
                       # the (x, y, width, height) tuple, a plain value
                       or cs_unqualify(m.get('Returns') or '') == 'Rectangle'
                       # System.Drawing.Color (G113: DrawColor(file) => DrawColor(file, Guid.Empty)): BND_Color,
                       # passed through unopened (G79)
                       or cs_unqualify(m.get('Returns') or '') == 'Color'
                       # an array of a value type (G116: `var p2d = Points2D;` in Leader.Points3D): the
                       # member's std::vector, as the translated body reads a C# array
                       or (cs_array_elem(m.get('Returns')) and _value_ret(cs_array_elem(m.get('Returns'))))):
                found = (owner, q)
                break
        if found is None:
            return None, '%s %s' % (kind, name), None
        owner, q = found
        if tag:
            tags[tag] = q
        if ok == 'out':
            q['out_companion'] = True
        if ok == 'vec':
            q['vec_companion'] = True
        ptypes = [qq['Type'] for qq in q['m'].get('Params') or []]
        # overrides that matter are those below the RECEIVER's static type (G92: `layer.Name` on a
        # Layer): an override between the declaring class and the receiver is reached by the C++
        # call through the receiver's own class (name hiding)
        recv = ocls if (ocls is not None and ocls is not owner and _derives_from(ocls, owner)) else owner
        if base_q:
            pass                            # `base.X` is the base's own body, never an override
        elif recv is not owner:
            # ... provided every class on the way that declares it has it generated
            mname = name if name != 'this[]' else q['m']['Name']
            c = recv
            while c is not None and c is not owner:
                declared = any(x['Type'] == c['type'] and x['Name'] == mname and x['Kind'] == q['m']['Kind']
                               for x in manifest['members'])
                if declared and not any(pp['m']['Name'] == mname and pp['m']['Kind'] == q['m']['Kind'] for pp in per_reg.get(c['reg'], [])):
                    return None, '%s %s is overridden in %s (not generated)' % (kind, name, c['cs']), None
                c = BY_REG.get(c.get('base', ''))
        if not base_q and _overridden_below(recv, q['m']['Kind'], name if name != 'this[]' else q['m']['Name'], ptypes, manifest):
            ov = _virtual_overrides(owner, q, ptypes, per_reg, manifest)
            if ov is None:
                return None, '%s %s is overridden below %s' % (kind, name, owner['cs']), None
            # every RhinoCommon override is generated under the same C++ name (G89):
            # a virtual call reaches it, as C#'s does
            q['virtual'] = True
            for o in ov:
                o['override_of'] = q
        deps.append(q)
        writes = writes or kind == 'set' or (kind in ('call', 'index') and bool(q.get('nonconst') or q.get('setter_of')))
        if kind in ('sget', 'scall', 'osget', 'oscall') or base_q:
            out.append('%s::%s' % (owner['gen'], q['cpp']))
        else:
            out.append(q['cpp'] + ('_out' if ok == 'out' else '_vec' if ok == 'vec' else ''))
        # a setter taking a class POINTER (a nullable class property; G112): the translator wrote the
        # argument as C# has it (a const& parameter, a local, null) -- pass its address; a modeled
        # field's setter (mfget) also keeps the pointer, so its argument is recorded
        if kind in ('set', 'oset') and q['bind_args'] and re.match(r'^const BND_\w+\s*\*$', q['bind_args'][0][0].strip()):
            borrows = (q.get('setter_of') or {}).get('shape', ('',))[0] == 'mfget'
            out.append('\x00ADDR' + ('B' if borrows else 'N'))
        # a callee that borrows arguments (G116: Leader.Create => CreateWithRichText, whose
        # ParentDimensionStyle keeps the style): the caller borrows what it passes there too
        elif kind in ('call', 'scall', 'ocall', 'ocallc', 'oscall') and q.get('keep_args'):
            out.append('\x00KEEP%s\x00' % ','.join(str(i) for i in q['keep_args']))
        if kind in ('call', 'scall', 'ocall', 'ocallc', 'oscall') and q.get('bind_args'):
            out.append('\x00ARGS%s\x00' % ''.join('P' if t.strip().endswith('*') else 'R' for t, _ in q['bind_args']))
    out.append(text[pos:])
    res = ''.join(out)
    del RESOLVE_BORROWS[:]
    del RESOLVE_ARGUSE[:]
    res = _arg_uses(res)
    res = _keep_args(res) if res is not None else None
    res = _addr_args(res) if res is not None else None
    if res is None:
        return None, 'pointer setter call', None
    for k in re.findall(r'\x00OWN(\d+)\x00', res):
        q = tags.get(k)
        own = returns_new(q) if q is not None else None
        if own is None:
            return None, 'owned temporary: ownership of %s unknown' % (q['m']['Name'] if q else '#' + k), None
        res = res.replace('\x00OWN%s\x00' % k, 'true' if own else 'false')
    if '$$' in res:
        return None, 'unresolved marker', None
    if byvalue and re.search(r'\bnullptr\b', res):
        return None, 'null on a value class', None
    return res, deps, writes


def _ttrace(x, why):
    # RH3DM_TRACE_TRANSLATED=1: name the member each translated-pass refusal is for (G122)
    if os.environ.get('RH3DM_TRACE_TRANSLATED'):
        print('translated refused: %s.%s: %s' % (x['Type'], x['Name'], why), file=sys.stderr)


def translated_pass(per_class, manifest, skipped):
    """add the translated pure members of every generated class (fixpoint:
    one may call another)"""
    per_reg = {cls['reg']: plans for cls, plans in per_class}
    def sig(m):
        return (m['Kind'], m['Name'], tuple(q['Type'] for q in m.get('Params') or []))
    changed = True
    while changed:
        changed = False
        for cls, plans in per_class:
            if cls['mode'] not in ('pointer', 'abstract', 'child', 'value', 'static'):    # value: MeshingParameters (G74)
                continue
            have = {sig(p['m']) for p in plans if not p.get('setter_of')}
            names = {p['cpp'] for p in plans}
            simple = cls['type'].replace('+', '.').split('.')[-1]
            # non-public helpers the bodies call (G85): unregistered plans, resolvable
            hidden = [{'Type': cls['type'], 'Kind': r['Kind'], 'Name': r['Name'], 'Variant': 'portable', 'Body': 'hidden',
                       'Static': bool(r.get('Static')), 'Returns': r.get('RetCs'), 'Hidden': True,
                       'Params': [{'Type': cs, 'Name': q['Name'], 'Mode': q.get('Mode') or ''}
                                  for cs, q in zip(r.get('ParamsCs') or [], r.get('Params') or [])]}
                      for k, r in CLASS_BODIES.items() if (r.get('Hidden') or cls.get('internal')) and k[0] in (simple, cls.get('impl'))]
            # an internal class (G96) has no manifest records: its every body is such a helper
            # hidden helpers first (G92): a setter is tried once, with its getter, and may call one
            for x in hidden + [x for x in manifest['members'] if x['Type'] == cls['type']]:
                # a delegation the forwarding planner could not express (an argument like
                # `Transform.Identity`; G83) is its translated body, as a pure member is
                # G84: and any other body the planner's shapes did not take (branches, switches,
                # loops around native calls), now that class bodies may call natives
                if (x['Type'] != cls['type'] or x['Variant'] != 'portable'
                        or x['Kind'] not in ('method', 'property', 'indexer')
                        or (cls['mode'] == 'static' and not x.get('Static'))):    # static mode: statics only (G122: Arrowhead)
                    continue
                bkey = (x['Kind'], x['Name'], tuple(q['Type'] for q in x.get('Params') or []))
                # an abstract member's body is its internal implementation's override (G91)
                r = CLASS_BODIES.get((simple,) + bkey) or (CLASS_BODIES.get((cls['impl'],) + bkey) if cls.get('impl') else None)
                if sig(x) in have:
                    # a getter another shape planned, its setter refused there (G115: MeshVertexList.Count's
                    # guarded two-call setter): the translated setter, tried below like any other
                    if (x['Kind'] == 'property' and r is not None and r.get('SetBody') and not x.get('Static')
                            and not x.get('Hidden') and not _overridden_below(cls, 'property', x['Name'], [], manifest)):
                        g = [q for q in plans if q['m']['Kind'] == 'property' and q['m']['Name'] == x['Name']
                             and not q.get('setter_of') and not q.get('hidden')]
                        if len(g) == 1 and not g[0].get('setter') and not g[0].get('pending_set') and not g[0]['bind_args']:
                            gret = resolve_markers(r['Ret'], cls, per_reg, manifest)[0]
                            if gret is not None:
                                g[0]['pending_set'] = (r, gret, [])
                    continue
                if r is None or bool(r.get('Static')) != bool(x.get('Static')):
                    continue
                natives = r.get('Native') or []
                bad = next((n for n in natives if not native_usable(n)), None)
                if bad is not None:
                    key = 'translated: native %s' % ('unsafe' if bad in UNSAFE_NATIVES else 'not portable')
                    _ttrace(x, key)
                    skipped[key] = skipped.get(key, 0) + 1
                    continue
                body, deps, writes = resolve_markers(r['Body'], cls, per_reg, manifest)
                borrowed = list(RESOLVE_BORROWS)
                marker_fns = list(RESOLVE_FNS)
                arg_uses = list(RESOLVE_ARGUSE)
                ret, _, _ = resolve_markers(r['Ret'], cls, per_reg, manifest)
                params = [(resolve_markers(q['Type'], cls, per_reg, manifest)[0], q['Name']) for q in r.get('Params') or []]
                # a hidden helper's `ref` parameter (G92: `ref string fullName`): by reference
                if x.get('Hidden'):
                    params = [((t + '&') if t and q.get('Mode') == 'ref' else t, n)
                              for (t, n), q in zip(params, r.get('Params') or [])]
                outs = [q.get('Mode') == 'out' for q in r.get('Params') or []]
                if any(outs) and (ret is None or ret.endswith('*') or x['Kind'] != 'method' or x.get('Static')
                                  or any(t not in VT_CPP.values() and t[:-1] not in DATA_CPP.values() and not t.startswith('std::vector<')
                                         and t not in ('int', 'double', 'bool', 'float', 'unsigned int')
                                         for (t, _), o in zip(params, outs) if o)):
                    skipped['translated: out parameter'] = skipped.get('translated: out parameter', 0) + 1
                    _ttrace(x, 'translated: out parameter')
                    continue
                # G112: a parameter stored by a borrowing setter must outlive the result -- the binding
                # keeps it alive (keep_args); any other borrowed argument's lifetime cannot be said
                pnames = [n for _, n in params]
                keep_args = sorted({pnames.index(b) for b in borrowed if b in pnames})
                if body is not None and any(b not in pnames for b in borrowed):
                    skipped['translated: borrowed argument'] = skipped.get('translated: borrowed argument', 0) + 1
                    _ttrace(x, 'translated: borrowed argument')
                    continue
                if body is not None and params:
                    # `Add(item, attributes)` => `AddBrep(brep, attributes)` (G117): a class parameter whose
                    # every use is an argument to a callee's POINTER parameter takes None, as C# passes null on
                    np_ = []
                    for tp, n in params:
                        tp, body = nullable_class_param(tp, n, body, arg_uses)
                        np_.append((tp, n))
                    params = np_
                if body is not None:
                    body = hand_fallback(cls, x, body)
                if body is None or ret is None or any(t is None for t, _ in params):
                    key = 'translated: ' + (deps if body is None else 'signature').split(' ')[0]
                    _ttrace(x, deps if body is None else 'signature: ret=%s params=%s' % (ret, params))
                    skipped[key] = skipped.get(key, 0) + 1
                    continue
                if _overridden_below(cls, x['Kind'], x['Name'], [q['Type'] for q in x.get('Params') or []], manifest):
                    skipped['translated: overridden below'] = skipped.get('translated: overridden below', 0) + 1
                    _ttrace(x, 'translated: overridden below')
                    continue
                def uniq(n):
                    k, c = n, 1
                    while k in names:
                        c += 1
                        k = '%s_%d' % (n, c)
                    names.add(k)
                    return k
                p = {'m': x, 'fn': None, 'cls': cls, 'static': bool(x.get('Static')), 'call_exprs': [], 'locals': [],
                     'bind_args': params, 'guards': [], 'success': None, 'translated': True, 'hidden': bool(x.get('Hidden')),
                     'ret_borrowed': bool(r.get('RetBorrowed')),
                     'shape': ('translated', ret, body), 'deps': deps, 'nonconst': writes,
                     'extra_fns': [C_BY_NAME[n] for n in natives] + marker_fns,
                     'cpp': uniq('Gen_' + x['Name'])}
                if keep_args:
                    p['keep_args'] = keep_args
                if any(outs):
                    # `bool TryGetCircle(out Circle circle, double tol)` (G83): the body as
                    # `_out` (outs by reference), the binding a tuple (success, outs...)
                    p['bind_args'] = [a for a, o in zip(params, outs) if not o]
                    p['shape'] = ('translatedout', ret, body, [(t, n, o) for (t, n), o in zip(params, outs)])
                plans.append(p)
                if x['Kind'] in ('property', 'indexer') and r.get('SetBody'):
                    p['pending_set'] = (r, ret, params)       # tried now, and again each round (G105)
                have.add(sig(x))
                changed = True
            # a translated property's setter (G105: ScreenPort => SetScreenPort(value)): resolved once the
            # members it calls are planned -- possibly in a later round of this fixpoint than its getter
            for p in [q for q in plans if q.get('pending_set') and not q.get('setter')]:
                r, ret, params = p['pending_set']
                x = p['m']
                sbody, sdeps, _ = resolve_markers(r['SetBody'], cls, per_reg, manifest)
                snat = r.get('SetNative') or []
                if sbody is None or not all(native_usable(n) for n in snat):
                    continue
                sp = {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
                      # an indexer's setter takes its index first (G101): set(index, value)
                      'bind_args': (params if x['Kind'] == 'indexer' else []) + [(ret, 'value')],
                      'guards': [], 'success': None, 'translated': True,
                      'shape': ('translated', 'void', sbody), 'deps': sdeps, 'setter_of': p,
                      'extra_fns': [C_BY_NAME[n] for n in snat],
                      'cpp': uniq('Gen_set_' + x['Name'])}
                p['setter'] = sp
                plans.append(sp)
                changed = True


def hide_shadowed_out_overloads(per_class, skipped):
    """two overloads with one bound signature where one returns its out parameters as a tuple (G83)
    and the other is RhinoCommon's plain form (G105: GetScreenPort() vs GetScreenPort(out near, out
    far), both `GetScreenPort()` in Python/JS): the plain one is registered, the out one kept as an
    unregistered helper -- the plain one usually calls it"""
    def is_out(q):
        return q['shape'][0] == 'translatedout' or any((pp.get('Mode') == 'out') for pp in q['m'].get('Params') or [])
    for cls, plans in per_class:
        groups = {}
        for q in plans:
            if q.get('setter_of') or q.get('hidden') or q['m']['Kind'] != 'method':
                continue
            key = (q['m']['Name'], bool(q.get('static')), tuple(t for t, _ in q['bind_args']))
            groups.setdefault(key, []).append(q)
        for qs in groups.values():
            plain = [q for q in qs if not is_out(q)]
            if len(qs) > 1 and plain:
                for q in qs:
                    if is_out(q):
                        q['hidden'] = True
                        skipped['hidden: out overload shadowed by its plain form'] = skipped.get('hidden: out overload shadowed by its plain form', 0) + 1


def drop_setterless_overrides(per_class, skipped):
    """a derived class's property with no setter where a generated base has the same property
    WITH one (G92: Layer.Name overrides ModelComponent.Name): registering it would hide the base's
    setter in Python and JS -- the override is dropped, the base's read/write property stays"""
    per_reg = {cls['reg']: plans for cls, plans in per_class}
    for cls, plans in per_class:
        for p in list(plans):
            if p['m']['Kind'] != 'property' or p.get('setter_of') or p.get('setter') or p['static'] or p.get('hidden'):
                continue
            b = BY_REG.get(cls.get('base', ''))
            while b is not None:
                if any(q['m']['Kind'] == 'property' and q['m']['Name'] == p['m']['Name'] and q.get('setter')
                       for q in per_reg.get(b['reg'], [])):
                    plans.remove(p)
                    skipped['override without the base setter'] = skipped.get('override without the base setter', 0) + 1
                    break
                b = BY_REG.get(b.get('base', ''))


def drop_orphaned_translations(per_class, skipped):
    """after the override refusals: a translated member whose callee is gone goes too"""
    changed = True
    while changed:
        changed = False
        live = {id(p) for _, plans in per_class for p in plans}
        for _, plans in per_class:
            for p in list(plans):
                if p.get('translated') and any(id(d) not in live for d in p['deps']):
                    plans.remove(p)
                    if p.get('setter_of') and p['setter_of'].get('setter') is p:
                        p['setter_of'].pop('setter', None)
                    skipped['translated: callee refused'] = skipped.get('translated: callee refused', 0) + 1
                    changed = True
