#!/usr/bin/env python3
"""Translate unchanged original protocol tables into closed shared assertions."""
from pathlib import Path
import hashlib,json,re
HERE=Path(__file__).parent
ROOT=HERE.parents[2]
source=(ROOT/'upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_json_test.go').read_text()
strings=r'("(?:[^"\\]|\\.)*"|`[^`]*`)'
row=re.compile(r'name:\s*'+strings+r',\s*input:\s*'+strings+r',\s*target:\s*new\((\w+)\),(?:\s*errText:\s*'+strings+r',)?\s*}',re.S)
def unquote(value): return value[1:-1] if value.startswith('`') else json.loads(value)
names=['TestUnmarshalRejectsNullForOptionalNonNullableFields','TestUnmarshalAcceptsNullForNullableFields','TestUnmarshalRejectsIncompleteObjects','TestUnmarshalRejectsWrongTypes']
lines=['// Copyright (c) Microsoft Corporation. All rights reserved.',
'// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.',
'// Original lsp_json_test.go tables; inputs/error substrings are unchanged.',
'using TypeScript.Lsp.Protocol;', 'using TypeScript.Scanner;',
'namespace LanguageServices.Checks;', 'public static partial class ProtocolCodecChecks','{']
counts={}
for name in names:
    body=source.split('func '+name+'(',1)[1].split('\nfunc ',1)[0]
    rows=list(row.finditer(body))
    if len(rows)!=len(re.findall(r'\btarget:\s*new\(',body)): raise ValueError('unrecognized original row '+name)
    counts[name]=len(rows)
    lines += [f'private static void {name}()', '{']
    for item in rows:
        title,input_,typ,error=item.groups()
        title=unquote(title);input_=unquote(input_)
        descriptor='ProtocolCodec.GetType_'+hashlib.sha256(typ.encode()).hexdigest()[:16]+'()'
        lines+=['{', '// '+title, f'var target = new {typ}();',
                f'var input = Bytes({json.dumps(input_,ensure_ascii=False)});']
        if error:
            lines += ['Exception? error = null;', 'try', '{',
                      f'ProtocolCodec.Unmarshal({descriptor}, input, ref target);', '}',
                      'catch (Exception failure) when (failure is System.Text.Json.JsonException or FormatException) { error = failure; }',
                      f'Require(error is not null && error.Message.Contains({json.dumps(unquote(error))}, StringComparison.Ordinal), {json.dumps(title)} + ": " + error?.Message);']
        elif name == 'TestUnmarshalRejectsWrongTypes':
            lines += ['Exception? error = null;', 'try', '{',
                      f'ProtocolCodec.Unmarshal({descriptor}, input, ref target);', '}',
                      'catch (Exception failure) when (failure is System.Text.Json.JsonException or FormatException) { error = failure; }',
                      f'Require(error is not null, {json.dumps(title)});']
        else:lines += [f'ProtocolCodec.Unmarshal({descriptor}, input, ref target);']
        lines+=['}']
    lines+=['}']
lines+=['}']
(HERE/'ProtocolCodecChecks.Tables.cs').write_text('\n'.join(lines)+'\n')
(HERE/'protocol-test-materialization.json').write_text(json.dumps({'originalFunctions':counts,'sourceSha256':hashlib.sha256(source.encode()).hexdigest(),'draftOnly':True},indent=2)+'\n')
print(counts)
