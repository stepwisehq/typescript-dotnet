#!/usr/bin/env python3
"""Reproduce TsOptions declaration data from the locked Go generated source.

A finite parser translates only the source's literal declaration expressions; unknown
syntax fails. It preserves references, ordered enum entries and initialization dependencies.
"""
import argparse
import hashlib
from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go'
TARGET = ROOT / 'src/TypeScript.Scanner/TsOptions/DeclarationsGenerated.cs'
LOCKED_SHA = '13d6ef2de232b16ff818c8ec9e5d5075e62ae511e5254fb4286293b49fa2cc09'


def ident(value):
    if value.startswith('diagnostics.'): return 'Diagnostics.' + value[len('diagnostics.'):]
    for name in ['ModuleDetectionKind','ModuleResolutionKind','ModuleKind','ScriptTarget','NewLineKind','JsxEmit']:
        if value.startswith('core.' + name): return name + '.' + value[len('core.'+name):]
    return {'core.TSUnknown':'Tristate.Unknown', 'extraValidationLocale':'ExtraValidation.Locale'}.get(value, value.replace('CommandLineOptionType','CommandLineOptionKind.').replace('CommandLineOptionPathKind', 'CommandLineOptionPathKind.'))


def block(prefix, fields):
    return prefix + '\n{\n' + '\n'.join('    ' + field.replace('\n','\n    ') + ',' for field in fields) + '\n}'


class Parser:
    def __init__(self, source):
        self.tokens = [m.group() for m in re.finditer(r'"(?:\\.|[^"\\])*"|//[^\n]*|\w+|[^\s]', source) if not m.group().startswith('//')]
        self.i = 0
    def peek(self): return self.tokens[self.i] if self.i < len(self.tokens) else ''
    def pop(self, expected=None):
        token=self.peek(); self.i += 1
        if expected is not None and token != expected: raise ValueError((expected,token))
        return token
    def expression(self, expected='CommandLineOption'):
        token=self.pop()
        if token == '&': return self.expression(expected)
        if token == '{':
            fields=[]
            while self.peek() != '}':
                field=self.pop(); self.pop(':')
                fields.append(field + ' = ' + self.expression())
                if self.peek()==',':self.pop()
            self.pop('}')
            return block('new ' + expected, fields)
        if token == '[':
            self.pop(']')
            header=''
            while self.peek()!='{': header+=self.pop()
            self.pop('{')
            elem='CommandLineOption' if header=='*CommandLineOption' else 'MapEntry<string, object?>'
            fields=[]
            while self.peek()!='}':
                fields.append(self.expression(elem))
                if self.peek()==',':self.pop()
            self.pop('}')
            return block('new List<' + elem + '>', fields)
        if token == 'map':
            self.pop('['); key=self.pop(); self.pop(']'); header=''
            while self.peek()!='{':header+=self.pop()
            self.pop('{')
            cstype={'*CommandLineOption':'CommandLineOption','*collections.OrderedMap[string,any]':'OrderedMap<string, object?>', '*collections.Set[string]':'HashSet<string>', 'string':'string'}[header]
            if key=='core': raise ValueError('split key token unexpectedly')
            keytype={'string':'string','core.ScriptTarget':'ScriptTarget','ScriptTarget':'ScriptTarget'}[key]
            fields=[]
            while self.peek()!='}':
                k=self.expression();self.pop(':');v=self.expression(cstype)
                fields.append('[' + k + '] = ' + v)
                if self.peek()==',':self.pop()
            self.pop('}')
            return block('new Dictionary<' + keytype + ', ' + cstype + '>', fields)
        if token.startswith('"') or token in ('true','false') or token.isdecimal(): return token
        while self.peek()=='.': self.pop();token+='.'+self.pop()
        if self.peek()=='[':
            header='';depth=0
            while True:
                t=self.pop();header+=t
                if t=='[':depth+=1
                if t==']':
                    depth-=1
                    if depth==0:break
            token+=header
        if self.peek()=='{':
            # Rewind the opening object and let the implicit-literal branch parse its fields.
            return self.expression(token)
        if self.peek()=='(':
            self.pop();args=[]
            while self.peek()!=')':
                args.append(self.expression())
                if self.peek()==',':self.pop()
            self.pop(')')
            if token=='slices.Concat': return args[0]+'.Concat('+args[1]+').ToList()'
            if token=='collections.NewOrderedMapFromList':
                # Ordered map list elements use object initializers; MapEntry exposes init properties.
                return 'OrderedMaps.NewOrderedMapFromList<string, object?>(' + args[0] + ')'
            if token=='collections.NewSetFromItems': return 'new HashSet<string> { ' + ', '.join(args) + ' }'
            return ident(token)+'('+', '.join(args)+')'
        return ident(token)


def generate():
    raw=SOURCE.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=LOCKED_SHA: raise SystemExit('Locked declarations source drift')
    text=raw.decode()
    matches=list(re.finditer(r'^var (\w+) = ', text, re.M))
    definitions={};deps={}
    for i,m in enumerate(matches):
        name=m.group(1);source=text[m.end():matches[i+1].start() if i+1<len(matches) else len(text)]
        # Map key header contains a qualified enum type: consume as one token for the finite parser.
        source=source.replace('map[core.ScriptTarget]', 'map[ScriptTarget]')
        p=Parser(source);definitions[name]=p.expression()
        if p.peek():raise ValueError('unconsumed '+name+': '+p.peek())
        deps[name]=set(re.findall(r'\b\w+\b',source))
    definitions['CommandLineCompilerOptionsMap']='commandLineOptionsToMap(OptionsDeclarations)'
    deps['CommandLineCompilerOptionsMap']={'OptionsDeclarations'}
    order=[];active=set()
    def visit(name):
        if name in order:return
        if name in active:raise ValueError('cyclic initialization '+name)
        active.add(name)
        for dependency in sorted(deps[name].intersection(definitions)):visit(dependency)
        active.remove(name);order.append(name)
    for name in definitions:visit(name)
    def typ(name):
        if name in ['OptionsDeclarations','BuildOpts','commonOptionsWithBuild','optionsForCompiler','typeAcquisitionDecls','OptionsForBuild']:return 'List<CommandLineOption>'
        if name in ['commandLineOptionElements']:return 'Dictionary<string, CommandLineOption>'
        if name=='commandLineOptionEnumMap':return 'Dictionary<string, OrderedMap<string, object?>>'
        if name=='commandLineOptionDeprecated':return 'Dictionary<string, HashSet<string>>'
        if name=='targetToLibMap':return 'Dictionary<ScriptTarget, string>'
        if name=='CommandLineCompilerOptionsMap':return 'CommandLineOptionNameMap'
        if name.endswith('Map') and name not in ['tsconfigRootOptionsMap']:return 'OrderedMap<string, object?>'
        return 'CommandLineOption'
    public={'OptionsDeclarations','BuildOpts','TscBuildOption','OptionsForBuild','LibMap','CommandLineCompilerOptionsMap'}
    lines=['// Copyright (c) Microsoft Corporation. All rights reserved.',
           '// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.',
           '// Modified C# port of tsc/internal/tsoptions/declarations_generated.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.',
           '// Generated by tools/configuration-generate-declarations.py. DO NOT EDIT.',
           '// Source SHA256: '+LOCKED_SHA, 'using TypeScript.Collections;', 'using TypeScript.Scanner;',
           'namespace TypeScript.TsOptions;', '', 'public static partial class TsOptions', '{']
    for name in definitions:lines.append(f'    {"public" if name in public else "internal"} static readonly {typ(name)} {name};')
    lines+=['    public static readonly NameMap CompilerNameMap;', '    public static readonly NameMap BuildNameMap;',
            '    public static readonly List<string> Libs;', '    public static readonly HashSet<string> LibFilesSet;', '',
            '    // Go initialization follows dependency order rather than textual field order.',
            '    static TsOptions()', '    {']
    for name in order:lines.append('        '+name+' = '+definitions[name].replace('\n','\n        ')+';')
    lines += ['        CompilerNameMap = GetNameMapFromList(OptionsDeclarations);', '        BuildNameMap = GetNameMapFromList(BuildOpts);',
              '        Libs = LibMap.Keys().ToList();', '        LibFilesSet = LibMap.Values().Select(value => (string)value!).ToHashSet(StringComparer.Ordinal);', '    }', '}', '']
    return '\n'.join(lines)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--check',action='store_true');args=parser.parse_args()
    output=generate()
    if args.check:
        if not TARGET.exists() or TARGET.read_text()!=output:raise SystemExit('DeclarationsGenerated.cs differs')
    else:TARGET.write_text(output)
