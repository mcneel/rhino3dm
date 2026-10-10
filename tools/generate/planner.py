"""Planning: one RhinoCommon member -> a plan (C call, argument
marshalling, return shape) or a refusal reason."""

from gen_registry import *


def plan(member, cfn, cls):
    if member.get('Body') not in ('trivial', 'pattern'):
        return None, 'body: %s' % (member.get('Body') or 'unknown')
    for g in member.get('Guards') or []:
        if g['Exception'] not in EXMAP:
            return None, 'guard exception %s' % g['Exception']
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
    nullable_params = set()
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
            nullable = carg.get('Value') == 'nullable'     # `x == null ? IntPtr.Zero : x.ConstPointer()`
            acc = '->' if nullable else '.'
            if argcls['mode'] == 'abstract':
                ptr = '%s%s%s()' % (name, acc, argcls['getter'])
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
                if carg.get('Value') and ctype in ('double*', 'int*', 'bool*'):
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
        elif sig_ret == holder['cs'] and cret in ('void', 'bool'):
            shape = ('holder', member['Holder'])
        else:
            return None, 'holder return %s vs %s' % (sig_ret, holder['cs'])
    elif ret_outs:
        shape = ('tuple', cret == 'bool', ret_outs)
        if sig_ret != 'bool' and cret == 'bool':
            return None, 'out-params with non-bool C# return'
    elif sig_ret in CS_RET_DIRECT and cret == CS_RET_DIRECT[sig_ret]:
        shape = ('direct', cret)
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

    success = member.get('Success')
    if success:
        if shape[0] != 'fromout' or success['Out'] != shape[2]:
            return None, 'success form vs shape'
        fb = success['Fallback']
        if fb != success['Out']:
            known = local_value(fb)                # Interval.Unset -> BND_IntervalX::Unset()
            if known is None or known[1] != sig_ret:
                return None, 'fallback %s' % fb
    return {'m': member, 'fn': cfn, 'cls': cls,
            'static': bool(member.get('Static')),
            'call_exprs': call_exprs, 'locals': locals_,
            'bind_args': bind_args, 'shape': shape,
            'guards': member.get('Guards') or [], 'success': success}, None


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
            call_args.append(a['Value'])
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
    have = {p['m']['Name'] for p in plans if p['m']['Kind'] == 'property'}
    for x in manifest['members']:
        if not (x['Type'] == cls['type'] and x['Variant'] == 'portable'
                and x['Kind'] == 'property' and x['Name'] not in have):
            continue
        child = BY_CS.get(cs_unqualify(x.get('Returns') or ''))
        if child and child['mode'] == 'child' and child.get('parent') == cls['reg'] \
                and cls['mode'] in ('pointer', 'child'):
            plans.append({'m': x, 'fn': None, 'cls': cls, 'static': False,
                          'call_exprs': [], 'locals': [], 'bind_args': [],
                          'shape': ('child', child), 'guards': [], 'success': None,
                          # a child may view a PART of the parent (File3dmSettings:
                          # &model->m_settings), per its registry 'derive'
                          'self_ptr': child.get('derive', '{p}').format(p=self_ptr)})
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
        sm = {'Type': g['m']['Type'], 'Variant': g['m']['Variant'],
              'Kind': 'method', 'Name': g['m']['Name'],
              'Static': g['m'].get('Static'), 'Body': st.get('Body'),
              'Guards': st.get('Guards'), 'Calls': st['Calls'],
              'Holder': st.get('Holder'), 'HolderArg': st.get('HolderArg', -1),
              'Params': (list(g['m'].get('Params') or []) if g['m']['Kind'] == 'indexer' else [])
                        + [{'Name': 'value', 'Type': g['m']['Returns'], 'Mode': ''}],
              'Returns': 'void'}
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
        for g in p['guards']:
            # a pure null-check on a by-value marshaled param is enforced by
            # the binding boundary (pybind/embind reject None before the body)
            if g.get('NullCheckOf') and g['NullCheckOf'] in argnames:
                continue
            kept.append(g)
            # File.Exists(path), the one call a guard may contain (specgen)
            cond = re.sub(r'\bFile\.Exists\s*\(', 'RH3DM_FileExists(', g['Cond'])
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
