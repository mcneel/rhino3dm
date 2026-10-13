"""Planning: one RhinoCommon member -> a plan (C call, argument
marshalling, return shape) or a refusal reason."""

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
    if cls.get('proxy'):
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
    sig_ret, sig_params = cs_signature(member)
    if sig_ret is None:
        return None, 'unparsed C# signature'
    call = member['Calls'][0]
    if len(call['Args']) != len(cfn['params']):
        return None, 'arity mismatch manifest vs call'
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
        self_expr = 'const_cast<%s*>(&m_val)' % cls['on']
    cs_param_modes = {n: (mode, t) for mode, t, n in sig_params}
    # a class argument null-checked by an early return is optional (None/null)
    nullable_params = {n for n in retnull if cs_param_modes.get(n, ('', ''))[1] in BY_CS}
    call_exprs, locals_, out_by_name, extra_fns, typearg_pos = [], [], {}, [], []
    newobj = None
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
        if k == 'fieldptr':
            return None, 'field pointer %s' % carg['Text']
        if holder and pos == holder_arg:
            if ctype != holder['ctype']:
                return None, 'holder ctype %s vs %s' % (ctype, holder['ctype'])
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
                else:
                    return None, 'nested call arg %s' % ia['Text']
            call_exprs.append('%s(%s)' % (inner['Name'], ', '.join(iargs)))
            extra_fns.append(ifn)
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
        if k == 'parammember':
            # box.Min: a translated property of a value-type parameter
            pn, _, mem = carg['Text'].partition('.')
            pcs = cs_param_modes.get(pn, ('', ''))[1]
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
            elif argcls.get('proxy'):
                # item.ConstPointer(): a proxy resolves; item.NonConstPointer() on a
                # copy-on-write view copies first (BrepFace.SetMesh's mesh; G59)
                cow_nc = carg.get('Value') == 'NonConstPointer' and argcls['proxy'].get('NonConst') == 'cow'
                ptr = '%s%s%s' % (name, acc, 'RH3DM_NonConst()' if cow_nc else 'RH3DM_Const()')
            elif argcls['mode'] in ('pointer', 'child'):
                ptr = '%s%sm_ptr' % (name, acc)
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
            if cstype in CS_ENUMS:
                if ctype == CS_ENUMS[cstype]['cpp']:
                    call_exprs.append(name)            # the C layer takes the public type itself
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
            locals_.append('%s %s_copy = %s.m_val;' % (argcls['on'], name, name))
            call_exprs.append('&%s_copy' % name)
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
            elif cstype in VT_CPP and cstype not in CS_ARG:
                bind_args.append((VT_CPP[cstype], name))
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
        shape = factory_shape(member['Factory'], cls, sig_ret, cret, out_by_name)
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
    elif sig_ret in CS_ENUMS and member.get('ResultSelect'):
        # `return value == (int)E.Clamp ? E.Clamp : E.Repeat;` (G55): every constant
        # must be a member of the returned enum (or a plain integer key)
        rs = member['ResultSelect']
        named = ENUM_NAMED.get(sig_ret.split('.')[-1]) or {}
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
    elif (sig_ret in BY_CS and BY_CS[sig_ret]['mode'] == 'pointer' and not BY_CS[sig_ret].get('comp')
          and (cret == '%s*' % BY_CS[sig_ret]['on']
               or BY_CS[sig_ret].get('borrowed') and cret == 'const %s*' % BY_CS[sig_ret]['on'])):
        if member.get('WrapArity') == 1:
            # `new T(p)`: only T's (IntPtr) constructor says whether p is ADOPTED;
            # TextureMapping's copies (NewFromPointer), Font's keeps a managed
            # pointer it never deletes (G49)
            target = BY_CS[sig_ret]
            pc = (TYPE_RECS.get(target['type']) or {}).get('PtrCtor') or ''
            if pc.endswith('+nullthrow'):
                # the constructor throws on a null p; the binding returns None
                # instead -- the same only where the member checked p first
                if not member.get('WrapChecked'):
                    return None, 'wrap through %s(IntPtr): throws on null' % sig_ret
                pc = pc[:-len('+nullthrow')]
            handle_field = (target.get('handle') or {}).get('Field')
            if not (pc == 'own' or (handle_field and pc == 'field:' + handle_field)
                    or (pc == 'borrowed' and target.get('borrowed'))):
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
    if success and shape[0] == 'fromoutconv':
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
            'guards': guards, 'success': success, 'failthrow': ft_attached, 'adopt': adopt,
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
        return 'construct child after a call'
    pv = (target or {}).get('proxy') or {}
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
    if kkind == 'param' and ktext in argnames:
        key = '(%s)%s' % (ktype, ktext)
    elif kkind == 'const' and re.match(r'^\d+$', ktext):
        key = ktext
    elif kkind == 'out' and out_by_name.get(ktext) in ('unsigned int*', 'int*'):
        key = '(%s)%s' % (ktype, ktext)
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


def factory_shape(fac, cls, sig_ret, cret, out_by_name):
    """`if (p == IntPtr.Zero) return null; return Instantiate(m_parent, type,
    id, index);` -- the switch factory's case for this class's constant
    (`type` is ComponentType), which must construct the member's own return
    type as a File3dm proxy from the Guid the native call wrote and the
    table's File3dm. Returns the shape, or a refusal reason (str)."""
    sw = fac['Args'][fac['SwitchArg']]
    value = None
    if sw['Kind'] == 'expr':
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
    if out_by_name.get(idl) != 'ON_UUID*':
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
        for a, q in zip(nat['Args'], fn['params']):
            ctype = q['type']
            if a['Kind'] in ('param', 'expr') and a['Text'] in argnames:
                cs = ptypes.get(a['Text'])
                args.append('(%s)%s' % (ctype, a['Text']) if cs in CS_ENUMS else a['Text'])
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
    named = ENUM_NAMED.get(enum.split('.')[-1]) or {}
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
        m2 = dict(x, Kind='method', Static=True, Name='__ctor', Returns=cls['cs'])
        p, why = plan(m2, fn, cls)
        if p is None:
            skip(why); continue
        if p['locals'] or p['shape'][0] != 'wrap':
            skip('shape'); continue
        p['ctor_call'] = '%s(%s)' % (fn['name'], ', '.join(p['call_exprs']))
        out.append(p)
    return out


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


def plan_forward(x, cls, by_name_arity, members=()):
    """a RhinoCommon member whose body only calls a sibling overload"""
    args = x.get('DelegateArgs') or []
    targets = by_name_arity.get((x['Delegates'], len(args)), [])
    if len(targets) != 1:
        return None, 'target %s' % ('ambiguous' if targets else 'not generated')
    target = targets[0]
    if target['shape'][0] == 'forward':
        return None, 'chained'
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
    call_args, sib_refs = [], []
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
            'call_exprs': [], 'locals': [], 'bind_args': bind_args,
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
    if x.get('ChildArgs') != ['this']:
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
        if t not in ('int', 'uint'):
            return None, 'construct param %s' % t
        bind_args.append((CS_ARG[t][0], n))
    basecls = BY_REG.get(cls.get('base', ''))
    self_expr = ('static_cast<%s*>(m_ptr)' % cls['on']
                 if basecls and basecls['mode'] not in ('abstract',) else 'm_ptr')
    shape = construct_shape(x, cls, self_expr, {n for _, n in bind_args}, None, {})
    if isinstance(shape, str):
        return None, shape
    if any(g.get('Assign') for g in x.get('Guards') or []):
        return None, 'parameter normalization in a construction'
    return {'m': x, 'fn': None, 'cls': cls, 'static': False, 'call_exprs': [], 'locals': [],
            'bind_args': bind_args, 'shape': shape, 'extra_fns': [],
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
    first['shape'] = ('seq', ['%s(%s)' % (q['fn']['name'], ', '.join(q['call_exprs'])) for q in subs])
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
                and x['Kind'] == 'method' and x.get('Body') == 'trivial'):
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
        if child and child['mode'] == 'child' and child.get('parent') == cls['reg'] \
                and cls['mode'] in ('pointer', 'child'):
            plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                          'call_exprs': [], 'locals': [], 'bind_args': [],
                          'shape': ('child', child), 'success': None,
                          'child_key': child_key(x, child) if child.get('key') else None,
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

    # constructions with no native call (G43): a list's `this[int]`, `new PointCloudItem(this, index)`
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

    # guard identifiers: own parameters pass, zero-arg siblings become calls
    zero_arg = {}
    for p in plans:
        if not p['bind_args'] and p['m']['Name'] not in zero_arg:
            zero_arg[p['m']['Name']] = p['cpp']
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
            if g.get('Calls'):
                # `!ModelComponent.ModelComponentTypeRequiresUniqueName(type)`: each
                # helper call becomes its resolved native call (specgen Native), or refuse
                cond = guard_calls_cpp(g, cond, cls, argnames, ptypes, p)
                if cond is None:
                    ok = False
                    break
            # dotted ids first: `Count` must not rewrite the tail of `curve.Count`
            for ident in sorted(g['Ids'], key=lambda i: '.' not in i):
                if ident in argnames:
                    continue
                if '.' in ident:
                    a, b = ident.split('.', 1)
                    vals = g.get('IdValues') or {}
                    if a in CS_ENUMS and ident in vals:          # IsoStatus.None
                        rep = '(%s)%s' % (CS_ENUMS[a]['cpp'], vals[ident])
                        GUARD_ENUMS.add(a)
                    elif a in argnames and (ptypes.get(a), b) in VT_PROPS:
                        rep = '%s.%s()' % (a, b)                  # xform.IsIdentity
                    else:
                        ok = False
                        break
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), rep, cond)
                elif ident in zero_arg:
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident),
                                  '%s()' % zero_arg[ident], cond)
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
            g['cond_cpp'] = cond
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
                'call_exprs': [], 'locals': [], 'bind_args': [(bt, 'value')],
                'shape': ('vset', {'which': '(%s)%s' % (which_t, va['WhichValue']), 'set': setn, 'cs': cs,
                                   'vt': vt, 'get': ff['GetValueNative'], 'put': ff['SetValueNative'],
                                   'eq': ff['EqualNative']}),
                'guards': [], 'success': None,
                'extra_fns': [C_BY_NAME[n] for n in (wr['New'], wr['Delete'], setn, ff['SetValueNative'], ff['EqualNative']) if n in C_BY_NAME]}, None
    get = (wr.get('Get') or {}).get(va['Conv'])
    if get is None or cs_unqualify(get['Returns']) != cs:
        return None, 'Variant.%s' % va['Conv']
    return {'m': x, 'fn': getfn, 'cls': cls, 'static': False,
            'call_exprs': [], 'locals': [], 'bind_args': [],
            'shape': ('vget', {'which': '(%s)%s' % (which_t, va['WhichValue']), 'conv': get, 'cs': cs, 'vt': vt,
                               'get': ff['GetValueNative']}),
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
    if item is None or not item.get('proxy') or not enumer:
        return None
    cv = class_const(cls['type'], 'ComponentType')
    if cv is None or cv[1] is None:
        return None
    for fn in ('ONX_ModelComponentIterator_New', 'ONX_ModelComponentIterator_GetNext',
               'ONX_ModelComponentIterator_Delete'):
        if fn not in C_BY_NAME:
            return None
    count = [p for p in plans if p['m']['Name'] == 'Count' and p['m']['Kind'] == 'property' and not p['bind_args']]
    find = [p for p in plans if p['m']['Name'] == 'FindIndex' and p['shape'][0] == 'proxywrap'
            and [t for t, _ in p['bind_args']] == ['int']]
    if len(count) != 1 or len(find) > 1 or (find and find[0]['shape'][1] is not item):
        return None
    types = [cv[1]]
    extras = (TYPE_RECS.get(cls['type']) or {}).get('ExtraComponentTypeValues') or []
    if extras:
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
    return {'item': item, 'type_value': cv[1], 'types': types, 'count': count[0]['cpp'], 'find': find[0]['cpp'] if find else None,
            'fns': [C_BY_NAME[n] for n in ('ONX_ModelComponentIterator_New',
                                           'ONX_ModelComponentIterator_GetNext',
                                           'ONX_ModelComponentIterator_Delete')]}


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
