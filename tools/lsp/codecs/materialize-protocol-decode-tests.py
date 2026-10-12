#!/usr/bin/env python3
"""Closed original decode assertions; fail if any source assertion is unrecognized."""
from pathlib import Path
import hashlib,json,re
HERE=Path(__file__).parent;ROOT=HERE.parents[2]
source=(ROOT/'upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_json_test.go').read_text()
def desc(n):return 'ProtocolCodec.GetType_'+hashlib.sha256(n.encode()).hexdigest()[:16]+'()'
def expression(s):
 s=s.strip()
 s=re.sub(r'len\(\*(\w+(?:\.\w+)*)\)',r'\1.Value.Length',s)
 s=re.sub(r'\(\*(\w+(?:\.\w+)*)\)',r'\1.Value',s)
 if s.startswith('*'):s=s[1:]+'.Value'
 s=re.sub(r'uint32\((\d+)\)',r'\1u',s)
 s=re.sub(r'int32\((\d+)\)',r'\1',s)
 s=s.replace('nil','null')
 # Source enum constants retain their actual named scalar owner.
 s=re.sub(r'InlayHintKind(Type|Parameter)',r'InlayHintKind.\1',s)
 return s
names=['TestUnmarshalStringOrArrayUnion','TestUnmarshalDocumentEditUnion','TestUnmarshalFieldOrdering','TestUnmarshalPresenceDiscriminatorUnion','TestUnmarshalDiscriminatorUnion']
lines=['// Copyright (c) Microsoft Corporation. All rights reserved.','// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.','// Original decode subtests; exact inputs and every assertion are preserved.','using TypeScript.Lsp.Protocol;','namespace LanguageServices.Checks;','public static partial class ProtocolCodecChecks','{']
counts={}
for name in names:
 body=source.split('func '+name+'(',1)[1].split('\nfunc ',1)[0]
 blocks=re.findall(r't.Run\("([^"]+)", func\(t \*testing.T\) \{(.*?)\n\t\}\)',body,re.S)
 if len(blocks)!=body.count('t.Run('):raise ValueError('unknown subtest shape '+name)
 counts[name]=len(blocks);lines += [f'private static void {name}()', '{']
 for title,block in blocks:
  lines+=['{','// '+title]
  if title=='optional discriminator is preserved':lines+=['CheckOriginalOptionalDiscriminatorArm();','}'];continue
  match=re.search(r'var (\w+) (\w+)\s*err := json.Unmarshal\(\[\]byte\(`([^`]*)`\), &(\w+)\)',block,re.S)
  if not match:raise ValueError('unknown decode '+name+'/'+title)
  variable,typ,input_,arg=match.groups();assert variable==arg
  errors='assert.Assert(t, err != nil)' in block or 'assert.ErrorContains(t, err,' in block
  lines += [f'var {variable}=new {typ}();']
  if errors:lines += [f'var error=DecodeError({desc(typ)},{json.dumps(input_,ensure_ascii=False)},ref {variable});']
  else:lines += [f'ProtocolCodec.Unmarshal({desc(typ)},Bytes({json.dumps(input_,ensure_ascii=False)}),ref {variable});']
  assertions=re.findall(r'assert\.(\w+)\(t, (.*?)\)\s*$',block,re.M)
  if len(assertions)!=block.count('assert.'):raise ValueError('unknown multiline assertion '+title)
  for kind,args in assertions:
   label=json.dumps(title)
   if kind=='NilError':assert args=='err';continue
   if kind=='Assert':
    if args=='err != nil':lines += [f'Require(error is not null,{label});'];continue
    if re.fullmatch(r'\w+(?:\.\w+)* (?:==|!=) nil',args):lines += [f'Require({expression(args)},{label});'];continue
    raise ValueError('unknown boolean '+args)
   if kind=='ErrorContains':
    raw=args.split(',',1)[1].strip();text=raw[1:-1] if raw.startswith('`') else json.loads(raw)
    lines += [f'Require(error is not null && error.Message.Contains({json.dumps(text)},StringComparison.Ordinal),{label});'];continue
   if kind=='Equal':
    lhs,rhs=args.split(',',1);lhs=expression(lhs);rhs=rhs.strip()
    if rhs.startswith('DocumentUri('):
     text=json.loads(rhs[len('DocumentUri('):-1]);lines += [f'Require({lhs}.Equals(new DocumentUri(Text({json.dumps(text)}))),{label});']
    elif rhs.startswith('"'):
     lines += [f'Require({lhs}.EqualsValue(Text({rhs})),{label});']
    else:lines += [f'Require({lhs}=={expression(rhs)},{label});']
    continue
   raise ValueError('unrecognized assertion '+kind)
  lines+=['}']
 lines+=['}']
lines+=['}'];(HERE/'ProtocolCodecChecks.Decode.Generated.cs').write_text('\n'.join(lines)+'\n')
(HERE/'protocol-decode-test-materialization.json').write_text(json.dumps({'originalFunctions':counts,'sourceSha256':hashlib.sha256(source.encode()).hexdigest(),'draftOnly':True},indent=2)+'\n')
print(counts)
