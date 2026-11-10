"""The documented API (M5): RhinoCommon's doc comments (api/docs.json, from src/dotnet by
`specgen docs`) joined to the manifest's member records, for the typings and the reference.

A generated registration is found here by its C# type, member name and parameter names -- the
generator binds C# parameter names, so the Python stub's names select the overload."""

import json
import os
import tomllib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'tools', 'generate'))


def _load_json(path):
    with open(path) as f:
        return json.load(f)


def _short(cref):
    """`M:Rhino.Geometry.Curve.PointAt(System.Double)` -> `Curve.PointAt`; `T:Rhino.Geometry.Point3d` -> `Point3d`"""
    cref = re.sub(r'^\w:', '', cref)
    cref = re.sub(r'\(.*$', '', cref)
    cref = re.sub(r'`\d+', '', cref)
    parts = cref.split('.')
    # keep Type.Member for members, Type for types
    if len(parts) >= 2 and parts[-1][:1].isupper() and parts[-2][:1].isupper() and not cref.startswith('System.'):
        return '.'.join(parts[-2:])
    return parts[-1]


LANGWORDS = {
    'py': {'null': 'None', 'true': 'True', 'false': 'False'},
    'js': {'null': 'null', 'true': 'true', 'false': 'false'},
}


def _text(el, lang):
    """the element's content as plain text; inline references in backticks"""
    out = [el.text or '']
    for c in el:
        tag = c.tag
        if tag in ('see', 'seealso'):
            if c.get('langword'):
                out.append('`%s`' % LANGWORDS[lang].get(c.get('langword'), c.get('langword')))
            elif c.get('cref'):
                out.append('`%s`' % _short(c.get('cref')))
            elif c.get('href'):
                out.append(c.get('href'))
            out.append(_text(c, lang) if (c.text or len(c)) else '')
        elif tag in ('paramref', 'typeparamref'):
            out.append('`%s`' % c.get('name', ''))
        elif tag in ('c', 'code'):
            if c.get('source'):
                pass                                    # an example file elsewhere: not shipped
            else:
                out.append('`%s`' % ''.join(c.itertext()).strip())
        elif tag == 'para':
            out.append('\n\n' + _text(c, lang).strip() + '\n\n')
        elif tag == 'br':
            out.append('\n')
        elif tag == 'list':
            items = [' '.join(_text(i, lang).split()) for i in c.findall('item')]
            out.append('\n\n' + '\n'.join('- ' + i for i in items) + '\n\n')
        else:
            out.append(_text(c, lang))
        out.append(c.tail or '')
    return ''.join(out)


def _clean(s):
    """whitespace: paragraphs kept, lines re-flowed"""
    paras = [' '.join(p.split()) for p in re.split(r'\n\s*\n', s)]
    return '\n\n'.join(p for p in paras if p)


def parse_doc(xml, lang='py'):
    """XML doc comment -> {summary, params{name: text}, returns, remarks, since, deprecated, exceptions[(type, text)]}"""
    root = ET.fromstring('<r>' + xml + '</r>')
    d = {'summary': '', 'params': {}, 'returns': '', 'remarks': '', 'since': None, 'deprecated': None,
         'exceptions': [], 'value': '', 'inherit': root.find('inheritdoc') is not None}
    for el in root:
        t = _clean(_text(el, lang))
        if el.tag == 'summary':
            d['summary'] = t
        elif el.tag == 'param':
            d['params'][el.get('name')] = t
        elif el.tag in ('returns', 'return'):
            d['returns'] = t
        elif el.tag == 'remarks':
            d['remarks'] = t
        elif el.tag == 'value':
            d['value'] = t
        elif el.tag == 'since':
            d['since'] = t
        elif el.tag == 'deprecated':
            d['deprecated'] = t
        elif el.tag == 'exception':
            d['exceptions'].append((_short(el.get('cref', '')), t))
    return d


class Api:
    def __init__(self, manifest=None, docs=None):
        self.manifest = _load_json(manifest or os.path.join(ROOT, 'api', 'manifest.json'))['members']
        rows = _load_json(docs or os.path.join(ROOT, 'api', 'docs.json'))['docs']
        self.docs = {(r['Type'], r['Kind'], r['Name'], r['Signature']): r['Xml'] for r in rows}
        self.type_docs = {r['Type']: r['Xml'] for r in rows if r['Kind'] == 'type'}
        # C# base lists (`RhinoList<Point3d>, ICloneable`): generic bases the registry does not chain
        self.cs_bases = {}
        for r in rows:
            if r['Kind'] == 'type' and r['Signature'].strip():
                names = [re.sub(r'<.*', '', b.strip()) for b in re.split(r',(?![^<]*>)', ' '.join(r['Signature'].split()))]
                self.cs_bases[r['Type']] = [n for n in names if n and not re.match(r'I[A-Z]', n.split('.')[-1])]
        self.enum_docs = {}
        for r in rows:
            if r['Kind'] == 'enum':
                self.enum_docs.setdefault(r['Name'], r['Xml'])
            elif r['Kind'] == 'enumvalue':
                self.enum_docs.setdefault(r['Type'].split('.')[-1].split('+')[-1] + '.' + r['Name'], r['Xml'])
        with open(os.path.join(ROOT, 'api', 'hand-docs.toml'), 'rb') as f:
            self.hand = tomllib.load(f)
        # value types: field names in order (the sequence constructor's order)
        vt = _load_json(os.path.join(ROOT, 'api', 'valuetypes.json'))
        self.vt_raw = vt['types']
        self.vt_fields = {t['Cs']: [f['Name'] for f in t['Fields']] for t in vt['types'] if t.get('Fields')}
        self.by_type = {}
        for m in self.manifest:
            if m.get('Variant') == 'rhino-only':
                continue
            self.by_type.setdefault(m['Type'], []).append(m)
        # C# type for a registered class name: the registry's, else a unique short-name match
        from gen_registry import CLASSES
        self.cs_of = {c['reg']: c['type'] for c in CLASSES}
        shorts = {}
        for t in self.by_type:
            shorts.setdefault(t.split('.')[-1].split('+')[-1], set()).add(t)
        for t in self.type_docs:
            shorts.setdefault(t.split('.')[-1].split('+')[-1], set()).add(t)
        for s, ts in shorts.items():
            if s not in self.cs_of:
                geo = [t for t in ts if t.startswith('Rhino.Geometry.')]
                self.cs_of[s] = sorted(ts)[0] if len(ts) == 1 else (geo[0] if len(geo) == 1 else None)

        # G143: overloads bound under a name of their own (GetPoint4d) -> RhinoCommon's (type, name, signature)
        from gen_registry import OVERLOAD_NAMES, VT_OVERLOAD_NAMES
        self.renamed = {(t, new): sig for (t, sig), new in OVERLOAD_NAMES.items()}
        for key, new in VT_OVERLOAD_NAMES.items():
            short, rest = key.split('.', 1)
            name, types = rest[:-1].split('(')
            t = self.cs_of.get(short)
            for m in self.by_type.get(t, []):
                if m['Name'] == name and [p['Type'].split('.')[-1] for p in m.get('Params') or []] == types.split(','):
                    self.renamed[(t, new)] = m['Signature']

    def _resolve(self, name, near):
        """a base-list name (`RhinoList`, `Runtime.CommonObject`) -> a full C# type"""
        ns = near.rsplit('.', 1)[0]
        for cand in (ns + '.' + name, 'Rhino.' + name, name):
            if cand in self.by_type or cand in self.type_docs:
                return cand
        short = name.split('.')[-1]
        hits = [t for t in list(self.by_type) + list(self.type_docs) if t.split('.')[-1] == short]
        return hits[0] if len(set(hits)) == 1 else None

    def base_types(self, cs):
        """the C# type and its bases (registry `base` chain, then the C# base lists), for inherited members"""
        from gen_registry import BY_TYPE, BY_REG
        out, c = [cs], BY_TYPE.get(cs)
        while c is not None and c.get('base'):
            b = BY_REG.get(c['base'])
            if b is None:
                break
            out.append(b['type'])
            c = b
        i = 0
        while i < len(out):
            for b in self.cs_bases.get(out[i], []):
                t = self._resolve(b, out[i])
                if t and t not in out:
                    out.append(t)
            i += 1
        return out

    def member(self, reg, name, kind, params=None, static=None):
        """the manifest record of a registration, or None. `params`: the bound parameter names in
        order (overload selection); kind: method | property | constructor"""
        if params is not None:
            params = [p[:-3] if p.endswith('_cs') else p for p in params]   # pilot #85, older builds
        cs = self.cs_of.get(reg)
        if not cs:
            return None
        for t in self.base_types(cs):
            sig = self.renamed.get((t, name))
            if sig:
                return next(m for m in self.by_type[t] if m['Signature'] == sig)
            taken = {v for (t2, _), v in self.renamed.items() if t2 == t}
            kinds = ('property', 'field', 'const') if kind == 'property' else (kind,)
            cands = [m for m in self.by_type.get(t, []) if m['Kind'] in kinds and
                     (m['Name'] == name or (kind == 'constructor')) and m['Signature'] not in taken]
            if kind == 'constructor' and t != cs:
                break                                   # constructors are not inherited
            if static is not None and kind == 'method':
                cands = [m for m in cands if bool(m.get('Static')) == static] or cands
            if params is not None and kind in ('method', 'constructor'):
                # `out` parameters are results in the bindings (a tuple), not arguments
                def ins(m):
                    return [p for p in (m.get('Params') or []) if p.get('Mode') not in ('out', 'ref')]
                exact = [m for m in cands if [p['Name'] for p in ins(m)] == list(params)]
                if exact:
                    return exact[0]
                # a C# default parameter the binding leaves out
                pre = [m for m in cands if [p['Name'] for p in ins(m)][:len(params)] == list(params)
                       and all(p.get('Default') is not None for p in ins(m)[len(params):])]
                if pre:
                    return pre[0]
                # a parameter the binding names differently: the one overload of that arity
                arity = [m for m in cands if len(ins(m)) == len(params)]
                if len(arity) == 1 and len(cands) <= 3:
                    return arity[0]
                continue
            if cands:
                return cands[0]
        return None

    def doc(self, m, lang='py'):
        """parsed docs of a manifest record ({} when undocumented)"""
        if m is None:
            return {}
        xml = self.docs.get((m['Type'], m['Kind'], m['Name'], m['Signature']))
        return parse_doc(xml, lang) if xml else {}

    def type_doc(self, reg, lang='py'):
        cs = self.cs_of.get(reg)
        xml = self.type_docs.get(cs) if cs else None
        if xml is None:
            xml = self.enum_docs.get(reg)
        return parse_doc(xml, lang) if xml else {}

    def enum_value_doc(self, enum, value, lang='py'):
        xml = self.enum_docs.get(enum + '.' + value)
        return parse_doc(xml, lang) if xml else {}

    def hand_doc(self, reg, name, params=None):
        """docs of a member rhino3dm adds (api/hand-docs.toml): `Class.Member(args)`, `Class.Member`,
        then `*.Member`; `name` None: the class itself"""
        keys = []
        if name is None:
            keys = [reg]
        else:
            if params is not None:
                keys.append('%s.%s(%s)' % (reg, name, ', '.join(params)))
            keys += ['%s.%s' % (reg, name), '*.%s' % name]
        for k in keys:
            e = self.hand.get(k)
            if e:
                return {'summary': e.get('summary', ''), 'params': dict(e.get('params', {})),
                        'returns': e.get('returns', ''), 'remarks': e.get('remarks', ''),
                        'since': e.get('since'), 'deprecated': e.get('deprecated'), 'exceptions': [], 'value': '',
                        'ts': e.get('ts')}
        return {}
