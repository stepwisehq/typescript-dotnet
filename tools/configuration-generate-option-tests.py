"""Independent source-derived static field/domain table for original reflection tests."""
from pathlib import Path
import re,json,hashlib,sys
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'upstream/TypeScript/tsc/internal/core/options_generated.go'
OUT=ROOT/'validation/Configuration.Checks/CompilerOptionFields.Generated.cs'
s=SOURCE.read_text().split('type CompilerOptions struct {',1)[1].split('\n}',1)[0]
tags=re.findall(r'^\s*([A-Z]\w*)\s+(.+?)\s+`json:"([^"]+)"`',s,re.M)
for name,_,tag in tags:
 if tag != tag.split(',',1)[0]+',omitzero':raise ValueError('Original full JSON tag suffix changed: '+name+' '+tag)
fields=[(name,type_,tag.split(',',1)[0]) for name,type_,tag in tags]
if len(fields)!=132:raise ValueError('Full exported field count '+str(len(fields)))
def path(t):return t.removeprefix('tspath.')
def mapping(t):
 if t.startswith('tspath.'):return path(t)
 return {'*int':'StrongBox<long>?','[]string':'List<string>?','[]PluginImport':'List<PluginImport>?','[]tspath.RootedDirectoryPath':'List<RootedDirectoryPath>?','*collections.OrderedMap[string, []string]':'OrderedMap<string, List<string>?>?'}.get(t,t)
def model(t):
 if t=='Tristate':return 'true','Tristate.True','false'
 if t=='string':return '"/value"','"/value"','false'
 if t.startswith('tspath.'):
  return '"/value"',path(t)+'.FromGoString("/value")','false'
 if t=='*int':return '2.0','new StrongBox<long>(2)','false'
 if t=='[]string':return 'new List<object?> { "/first", "/second" }','new List<string> { "/first", "/second" }','false'
 if t=='[]tspath.RootedDirectoryPath':return 'new List<object?> { "/first", "/second" }','new List<RootedDirectoryPath> { RootedDirectoryPath.FromGoString("/first"), RootedDirectoryPath.FromGoString("/second") }','false'
 if t=='[]PluginImport':return 'pluginInput()','new List<PluginImport> { new() { Name = "plugin" } }','false'
 if t=='*collections.OrderedMap[string, []string]':return 'pathsInput()','pathsExpected()','false'
 if t not in {'JsxEmit','ModuleKind','ModuleResolutionKind','ModuleDetectionKind','NewLineKind','ScriptTarget'}:raise ValueError('Unhandled original test type '+t)
 return '('+t+')1','('+t+')1','true'
def values(t):
 if t in {'Tristate','JsxEmit','ModuleKind','ModuleResolutionKind','ModuleDetectionKind','NewLineKind','ScriptTarget'}:return '[('+t+')0, ('+t+')1, ('+t+')2]'
 if t=='string':return '["", "b", "c"]'
 if t.startswith('tspath.'):return '[default('+path(t)+'), '+path(t)+'.FromGoString("b"), '+path(t)+'.FromGoString("c")]'
 if t=='*int':return '[null, new StrongBox<long>(0), new StrongBox<long>(0), new StrongBox<long>(1), new StrongBox<long>(1), new StrongBox<long>(2)]'
 if t=='[]string':return '[null, new List<string>(), new List<string> { "" }, new List<string> { "a", "b" }, new List<string> { "a", "b" }, new List<string> { "b", "a" }]'
 if t=='[]PluginImport':return '[null, new List<PluginImport>(), new List<PluginImport> { default }, new List<PluginImport> { new() { Name = "a" } }, new List<PluginImport> { new() { Name = "a" } }, new List<PluginImport> { new() { Name = "b" } }]'
 if t=='[]tspath.RootedDirectoryPath':return '[null, new List<RootedDirectoryPath>(), new List<RootedDirectoryPath> { default }]'
 if t=='*collections.OrderedMap[string, []string]':return '[null, new OrderedMap<string, List<string>?>(), new OrderedMap<string, List<string>?>()]'
 raise ValueError(t)
text='''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Independent static translation of the full reflection domain in pinned
// compileroptions_test.go; generated from ORIGINAL field declarations only.
#nullable enable
using System.Runtime.CompilerServices;
using TypeScript.Collections;
using TypeScript.Scanner;

namespace Configuration.Checks;

public static partial class CompilerOptionChecks
{
    private sealed record TestField(string Name, string JsonName, string GoType,
        Func<CompilerOptions, object?> Read, Action<CompilerOptions, object?> Write,
        Func<object?> ParseInput, Func<object?> ParseExpected, Func<object?[]> EqualityValues,
        bool NumericEnum, bool ShallowReference);

    private static readonly IReadOnlyList<TestField> originalFields =
    [
'''
for name,t,jsonname in fields:
 inp,expected,numeric=model(t)
 shallow='true' if t.startswith('*') or t.startswith('[]') else 'false'
 text+=f'        new("{name}", "{jsonname}", "{t}", o => o.{name}, (o, v) => o.{name} = ({mapping(t)})v!, () => {inp}, () => {expected}, () => {values(t)}, {numeric}, {shallow}),\n'
text+='    ];\n}\n'
prov={'source':str(SOURCE.relative_to(ROOT)),'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'originalTest':'upstream/TypeScript/tsc/internal/tsoptions/compileroptions_test.go','originalTestSha256':hashlib.sha256((ROOT/'upstream/TypeScript/tsc/internal/tsoptions/compileroptions_test.go').read_bytes()).hexdigest(),'fields':[{ 'name':n,'goType':t,'jsonName':j,'originalJsonTag':j+',omitzero'} for n,t,j in fields],'declarationTagAdaptation':'Static source guard retains every full name,omitzero tag assertion; C# has no Go struct-tag metadata. Actual zero omission is independently exercised by the complete showconfig and static codec assertion domains.', 'domain':'Every exported field, original parse input/expected value, enum-number branch, shallow reference requirement and Cartesian equality values; extra path-map matrix is in explicit shared assertion body. No production field descriptors or runtime values determine expected domains.'}
P=OUT.with_name('compiler-option-test-provenance.json')
encoded=json.dumps(prov,indent=2)+'\n'
if '--check' in sys.argv:
 if OUT.read_text()!=text or P.read_text()!=encoded:raise SystemExit('Static original option test assets stale')
else:OUT.write_text(text);P.write_text(encoded)
print('All',len(fields),'original fields and domains generated')
