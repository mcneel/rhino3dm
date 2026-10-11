"""Planning: one RhinoCommon member -> a plan (C call, argument
marshalling, return shape) or a refusal reason."""

from gen_registry import *


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
    if owner is not cls and owner.get('proxy'):
        return 'static_cast<%s*>(%s)' % (cls['on'], 'RH3DM_NonConst()' if nonconst else 'RH3DM_Const()')
    if cls['mode'] == 'abstract':
        return '%s%s()' % (cls['getter'], 'NC' if nonconst else '')
    return self_expr


def plan(member, cfn, cls):
    if member.get('Body') not in ('trivial', 'pattern'):
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
                ptr = '%s%sRH3DM_Const()' % (name, acc)      # item.ConstPointer(): a proxy resolves
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
                if carg.get('Value') and ctype in ('double*', 'int*', 'bool*', 'ON_UUID*'):
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
    elif ret_outs:
        shape = ('tuple', cret == 'bool', ret_outs)
        if sig_ret != 'bool' and cret == 'bool':
            return None, 'out-params with non-bool C# return'
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
        # `return rc != 0;` -- the comparison RhinoCommon wrote, recorded by specgen
        shape = ('conv', 'bool', '({c}) ' + member['Compare'])
    elif (sig_ret in CS_RET_DIRECT and cret in ('void', 'bool')
          and [t for t in out_by_name.values()] == [CS_RET_DIRECT[sig_ret] + '*']):
        # `double d = 0; F(ptr, ref d); return d;` (the status is ignored, as in C#)
        shape = ('fromout', sig_ret, next(iter(out_by_name)))
    elif sig_ret in CS_RET_DIRECT:
        return None, 'return mismatch %s vs %s' % (sig_ret, cret)
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
    elif sig_ret in CS_ENUMS:
        if cret not in ('int', 'unsigned int') and not cret.startswith('enum ') and cret != CS_ENUMS[sig_ret]['cpp']:
            return None, 'enum return %s vs C %s' % (sig_ret, cret)
        shape = ('enumret', sig_ret)
    elif (sig_ret in BY_CS and BY_CS[sig_ret]['mode'] == 'pointer'
          and cret == '%s*' % BY_CS[sig_ret]['on']):
        shape = ('wrap', sig_ret)
    elif sig_ret == 'void' and (cret == 'void' or cret in CVALUE):
        shape = ('direct', 'void')         # a C status C# ignores is ignored here too
    else:
        return None, 'return type %s (C: %s)' % (sig_ret, cret)

    if retnull - nullable_params:
        return None, 'early return on null of non-class %s' % sorted(retnull - nullable_params)[0]
    guards = []
    for g in member.get('Guards') or []:
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
        if shape == ('direct', 'void'):
            shape = ('failthrow', failthrow)
        elif shape[0] == 'fromout' and not member.get('Success'):
            ft_attached = failthrow
        else:
            return None, 'fail-throw shape'

    if commits and not (shape == ('direct', 'void') or shape[0] == 'failthrow'):
        return None, 'Commit() after a non-void shape'
    success = member.get('Success')
    if success:
        if shape[0] != 'fromout' or success['Out'] != shape[2]:
            return None, 'success form vs shape'
        fb = success['Fallback']
        if fb != success['Out']:
            known = local_value(fb)                # Interval.Unset -> BND_IntervalX::Unset()
            # `if (!F(ptr, ref d)) d = 0;`: a literal suits a plain C value return
            literal_ok = known is not None and known[1] is None and sig_ret in CS_RET_DIRECT
            if known is None or (known[1] != sig_ret and not literal_ok):
                return None, 'fallback %s' % fb
    return {'m': member, 'fn': cfn, 'cls': cls,
            'static': bool(member.get('Static')),
            'call_exprs': call_exprs, 'locals': locals_,
            'bind_args': bind_args, 'shape': shape, 'extra_fns': extra_fns,
            'guards': guards, 'success': success, 'failthrow': ft_attached,
            'typearg_pos': typearg_pos, 'commits': commits}, None


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


def plan_forward(x, cls, by_name_arity):
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
    if child is None or x.get('ChildArgs') != ['this']:
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
                          'shape': ('child', child), 'guards': [], 'success': None,
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
        f, why = plan_forward(x, cls, by_name_arity)
        if f:
            plans.append(f)
        else:
            skipped['forward: ' + why] = skipped.get('forward: ' + why, 0) + 1

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
            cond = re.sub(r'\bnull\b', 'nullptr', ft['Cond'])
            for ident in ft.get('Ids') or []:
                if ident in argnames:
                    continue
                if ident in zero_arg:
                    cond = re.sub(r'(?<![\w.])%s\b' % re.escape(ident), '%s()' % zero_arg[ident], cond)
                else:
                    ok = False
                    break
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
