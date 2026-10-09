#!/usr/bin/env python3
"""Extract selected literal assertions from locked Go tests, never codec outputs."""
import argparse,base64,hashlib,json,pathlib,re,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[1]
BASE=ROOT/'tools/.cache/go/src/encoding/json/internal/jsonwire'
OUT=ROOT/'validation/Configuration.Checks/JsonOriginalChecks.Generated.cs'
PROV=ROOT/'validation/Configuration.Checks/json-original-provenance.json'
NAMES={'decode_test.go':['TestConsumeWhitespace','TestConsumeLiteral','TestConsumeString','TestConsumeNumber','TestParseHexUint16'], 'encode_test.go':['TestAppendQuote','TestAppendNumber'], 'wire_test.go':['TestQuoteRune','TestTruncatePointer']}

def block(text,name):
    start=text.index('func '+name+'(')
    end=text.find('\nfunc ',start+1)
    body=text[start:end if end>=0 else len(text)]
    table=body[body.index('\n\ttests :='):body.index('\n\tfor _, tt := range tests')]
    return table

def generate():
    funcs=[]
    emit={
      'TestConsumeWhitespace':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"want":tt.want}) }',
      'TestConsumeLiteral':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"literal":tt.literal,"want":tt.want,"error":errstr(tt.wantErr)}) }',
      'TestConsumeString':'for _,tt:=range tests { e:=tt.wantErrUTF8; if e==errPrev {e=tt.wantErr}; emit(map[string]any{"in":b64(tt.in),"want":tt.wantUTF8,"error":errstr(e)}) }',
      'TestConsumeNumber':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"want":tt.want,"error":errstr(tt.wantErr)}) }',
      'TestParseHexUint16':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"want":tt.want,"ok":tt.wantOk}) }',
      'TestAppendQuote':'for _,tt:=range tests { if tt.flags==0 {emit(map[string]any{"in":b64(tt.in),"want":b64(tt.want),"error":errstr(tt.wantErr)})} }',
      'TestAppendNumber':'for _,tt:=range tests { emit(map[string]any{"bits":fmt.Sprintf("%016x",math.Float64bits(tt.in)),"want":tt.want64}) }',
      'TestQuoteRune':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"want":b64(tt.want)}) }',
      'TestTruncatePointer':'for _,tt:=range tests { emit(map[string]any{"in":b64(tt.in),"want":b64(tt.want)}) }'}
    for file,names in NAMES.items():
      text=(BASE/file).read_text()
      for name in names:
        prefix='var errPrev = errors.New("same as previous error")\n' if name=='TestConsumeString' else ''
        funcs.append('func export'+name+'(emit func(map[string]any)) { '+prefix+block(text,name)+ '\n'+emit[name]+'\n}')
    helper='''//go:build goexperiment.jsonv2
package jsonwire
import("encoding/base64";"strconv";"errors";"fmt";"io";"math";"strings";"testing";"encoding/json/internal/jsonflags")
func b64(s string) string{return base64.StdEncoding.EncodeToString([]byte(s))}
func errstr(e error) any {if e==nil{return nil};return e.Error()}
'''+ '\n'.join(funcs)+'\nfunc TestConfigurationLiteralExport(t *testing.T){\n'
    for names in NAMES.values():
      for name in names:
        helper+='export'+name+'(func(row map[string]any){row["sourceTest"]="'+name+'";parts:=[]string{};for k,v:=range row { var value string;switch x:=v.(type){case string:value="s"+b64(x);case int:value="i"+strconv.Itoa(x);case uint16:value="i"+strconv.Itoa(int(x));case bool:value="b"+strconv.FormatBool(x);case nil:value="n";default:t.Fatalf("unknown literal type %T",v)};parts=append(parts,k+"="+value)};fmt.Println("CONFIGJSON:"+strings.Join(parts,"\t"))})\n'
    helper+='}\n'
    path=BASE/'configuration_literal_export_test.go'
    if path.exists(): raise RuntimeError('Temporary source exporter already exists')
    try:
      path.write_text(helper)
      run=subprocess.run([str(ROOT/'tools/.cache/go/bin/go'),'test','-v','-run','^TestConfigurationLiteralExport$','-count=1','encoding/json/internal/jsonwire'],cwd=ROOT,capture_output=True,text=True,env={**__import__('os').environ,'GOEXPERIMENT':'jsonv2'})
      if run.returncode: raise RuntimeError(run.stdout+run.stderr)
    finally: path.unlink(missing_ok=True)
    rows=[]
    for line in run.stdout.splitlines():
      if not line.startswith('CONFIGJSON:'):continue
      row={}
      for part in line[len('CONFIGJSON:'):].split('\t'):
        key,value=part.split('=',1)
        row[key]=base64.b64decode(value[1:]).decode() if value[0]=='s' else int(value[1:]) if value[0]=='i' else value[1:]=='true' if value[0]=='b' else None
      rows.append(row)
    if not rows: raise RuntimeError('No literal assertions')
    code='// Generated from locked Go literal EXPECTATIONS; no production codec evaluated.\n#nullable enable\nnamespace Configuration.Checks;\npublic static partial class JsonOriginalChecks\n{\n'
    def cs(s):return 'null' if s is None else json.dumps(s,ensure_ascii=True)
    for names in NAMES.values():
      for name in names:
        selected=[r for r in rows if r['sourceTest']==name]
        code+='    private static readonly Row[] '+name+'Rows = [\n'
        for row in selected:
          code+='        new('+cs(row.get('in',''))+', '+str(row.get('want',0) if isinstance(row.get('want',0),int) else 0)+', '+cs(row.get('error'))+', '+cs(row.get('want') if isinstance(row.get('want'),str) else None)+', '+cs(row.get('literal'))+', '+cs(row.get('bits'))+', '+str(row.get('ok',False)).lower()+'),\n'
        code+='    ];\n'
    code+='}\n'
    prov={'generator':str(pathlib.Path(__file__).relative_to(ROOT)),'goVersion':subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'version'],text=True).strip(),'sourceFiles':{str((BASE/f).relative_to(ROOT)):hashlib.sha256((BASE/f).read_bytes()).hexdigest() for f in NAMES},'sourceTests':{n:len([r for r in rows if r['sourceTest']==n]) for names in NAMES.values() for n in names},'expectedSelection':'Literal table fields only; strict consumeString UTF8 offsets/errors, flags=0 AppendQuote AllowInvalidUTF8 output, float64 AppendFloat expectations. No codec result generation. Specialized fast consumers, ValueFlags, nonstrict unquote and float32/custom flags excluded.','rows':len(rows),'generatedSha256':hashlib.sha256(code.encode()).hexdigest()}
    return code,json.dumps(prov,indent=2)+'\n'


def generate_jsontext():
    base=BASE.parent.parent/'jsontext'
    state=(base/'state_test.go').read_text()
    pointer=block(state,'TestPointer')
    start=state.index('func TestObjectNamespace(')
    ops=state[state.index('\n\ttype operation any',start):state.index('\n\t// Execute the sequence',start)]
    # The emitter selects expectations, not results from the port/oracle.
    helper=r"""//go:build goexperiment.jsonv2
package jsontext
import("encoding/base64";"fmt";"io";"strconv";"strings";"testing";"unicode/utf8")
func cfgB64(s string) string{return base64.StdEncoding.EncodeToString([]byte(s))}
func cfgEmit(test,input,want string,ok bool){fmt.Println("CONFIGTEXT:"+test+"\t"+cfgB64(input)+"\t"+cfgB64(want)+"\t"+strconv.FormatBool(ok))}
func TestConfigurationLiteralExport(t *testing.T){
for _,td:=range coderTestdata {cfgEmit("TestDecoder",td.in,"",true);if len(td.tokens)==1 && (td.tokens[0].Kind()=='n' || td.tokens[0].Kind()=='t' || td.tokens[0].Kind()=='f' || td.tokens[0].Kind()=='"') {cfgEmit("TestEncoder",td.in,td.outCompacted,true)}}
for _,td:=range decoderErrorTestdata {if len(td.opts)==0 && len(td.calls)>0 {if _,ok:=td.calls[0].wantOut.(Value);ok && td.calls[0].wantErr!=nil && td.calls[0].wantErr!=io.EOF {cfgEmit("TestDecoderErrors",td.in,td.calls[0].wantErr.Error(),false)}}}
for _,td:=range encoderErrorTestdata {if len(td.opts)==0 && len(td.calls)>0 {if value,ok:=td.calls[0].in.(Value);ok && td.calls[0].wantErr!=nil {cfgEmit("TestEncoderErrors",string(value),td.calls[0].wantErr.Error(),false)}}}
for _,td:=range valueTestdata {cfgEmit("TestValueMethods",td.in,"",td.wantValid)}
arrays:=strings.Repeat("[",maxNestingDepth+1)+strings.Repeat("]",maxNestingDepth+1)
objects:=strings.Repeat(`{"":`,maxNestingDepth+1)+`""`+strings.Repeat("}",maxNestingDepth+1)
cfgEmit("TestCoderMaxDepth",arrays[1:len(arrays)-1],"",true)
cfgEmit("TestCoderMaxDepth",objects[len(`{"":`):len(objects)-1],"",true)
cfgEmit("TestCoderMaxDepth",arrays,(&SyntacticError{ByteOffset:maxNestingDepth,JSONPointer:Pointer(strings.Repeat("/0",maxNestingDepth)),Err:errMaxDepth}).Error(),false)
cfgEmit("TestCoderMaxDepth",objects,(&SyntacticError{ByteOffset:maxNestingDepth*int64(len(`{"":`)),JSONPointer:Pointer(strings.Repeat("/",maxNestingDepth)),Err:errMaxDepth}).Error(),false)
exportCfgPointer()
exportCfgNamespace()
}
"""
    helper+='func exportCfgPointer(){'+pointer+'\nfor _,tt:=range tests{if tt.wantValid && strings.HasPrefix(string(tt.in),"/"){cfgEmit("TestPointer",string(tt.wantParent)+"\\x00"+tt.wantLast,string(tt.in),true)}}\n}\n'
    helper+='func exportCfgNamespace(){'+ops+'\nfor _,op:=range ops{switch x:=op.(type){case insert:if utf8.ValidString(x.name){cfgEmit("TestObjectNamespace",x.name,"",x.wantInserted)};case removeLast:cfgEmit("TestObjectNamespace","","",false)}}\n}\n'
    path=base/'configuration_literal_export_test.go'
    if path.exists():raise RuntimeError('Temporary exporter exists')
    try:
      path.write_text(helper)
      run=subprocess.run([str(ROOT/'tools/.cache/go/bin/go'),'test','-v','-run','^TestConfigurationLiteralExport$','-count=1','encoding/json/jsontext'],cwd=ROOT,capture_output=True,text=True,env={**__import__('os').environ,'GOEXPERIMENT':'jsonv2'})
      if run.returncode:raise RuntimeError(run.stdout+run.stderr)
    finally:path.unlink(missing_ok=True)
    rows=[]
    for line in run.stdout.splitlines():
      if line.startswith('CONFIGTEXT:'):
        test,input,want,ok=line[len('CONFIGTEXT:'):].split('\t')
        rows.append((test,input,want,ok))
    code='// Generated only from locked source assertion literals/table fields.\n#nullable enable\nnamespace Configuration.Checks;\npublic static partial class JsonOriginalChecks\n{\n'
    names=['TestEncoder','TestDecoder','TestDecoderErrors','TestEncoderErrors','TestCoderMaxDepth','TestPointer','TestObjectNamespace','TestValueMethods']
    counts={}
    for test in names:
      selected=[r for r in rows if r[0]==test];counts[test]=len(selected)
      if not selected:raise RuntimeError('Empty selected domain '+test)
      code+='    private static readonly ExtraRow[] '+test+'Rows = [\n'
      for _,input,want,ok in selected:code+='        new('+json.dumps(input)+', '+json.dumps(want)+', '+ok+'),\n'
      code+='    ];\n'
    code+='}\n'
    files=['coder_test.go','decode_test.go','encode_test.go','state_test.go','value_test.go','fuzz_test.go']
    provenance={'sourceFiles':{str((base/f).relative_to(ROOT)):hashlib.sha256((base/f).read_bytes()).hexdigest() for f in files},'sourceTests':counts,'selection':'Decoder first whole-value/default grammar acceptance and first Value error only; no stream-state/offset/peek/read output claims. Encoder first whole-value/default WriteValue errors through actual strict FormatRawValue producer; no stream state claim. Decoder first raw ReadValue errors use lexical raw mode, not generic Unmarshal. Pointer valid Parent+AppendToken roundtrip selected; traversal/malformed validation omitted. Namespace source insertion/removal predicate sequence selected at duplicate validation API; topology/storage and invalid-UTF8 replacement row excluded. Maxdepth SingleValue validation only. TestValueMethods all33 IsValid literal predicates; Compact/Indent/Canonicalize omitted. Fuzz strict-validity seed inputs reuse these unit assertions without a fuzz body claim.','generatedSha256':hashlib.sha256(code.encode()).hexdigest()}
    return code,json.dumps(provenance,indent=2)+'\n'


def generate_arshal():
    base=BASE.parent.parent/'v2';source=base/'arshal_test.go';text=source.read_text()
    marshal=block(text,'TestMarshal');unmarshal=block(text,'TestUnmarshal')
    model=r"""
func cfgModel(v reflect.Value) (string,bool) {
 if !v.IsValid(){return "null",true};if v.Kind()==reflect.Interface {if v.IsNil(){return "null",true};return cfgModel(v.Elem())}
 if v.Type().PkgPath()!="" {return "",false}
 switch v.Kind(){
 case reflect.Bool:return strconv.FormatBool(v.Bool()),true
 case reflect.String:return `Raw("`+base64.StdEncoding.EncodeToString([]byte(v.String()))+`")`,true
 case reflect.Int,reflect.Int64,reflect.Int32,reflect.Int16,reflect.Int8:return strconv.FormatInt(v.Int(),10)+"L",true
 case reflect.Uint,reflect.Uint64,reflect.Uint32,reflect.Uint16,reflect.Uint8:return strconv.FormatUint(v.Uint(),10)+"UL",true
 case reflect.Float64:return `BitConverter.Int64BitsToDouble(unchecked((long)0x`+fmt.Sprintf("%016x",math.Float64bits(v.Float()))+`UL))`,true
 case reflect.Slice,reflect.Array:
   if v.Type().Elem()==reflect.TypeFor[byte](){var a []string;for i:=0;i<v.Len();i++{a=append(a,strconv.FormatUint(v.Index(i).Uint(),10))};return "new byte[] {"+strings.Join(a,",")+"}",true}
   a:=[]string{};for i:=0;i<v.Len();i++{x,ok:=cfgModel(v.Index(i));if !ok{return "",false};a=append(a,x)};return "new List<object?> {"+strings.Join(a,",")+"}",true
 case reflect.Map:
   if v.Type().Key()!=reflect.TypeFor[string](){return "",false};var a []string;keys:=v.MapKeys();slices.SortFunc(keys,func(a,b reflect.Value)int{return strings.Compare(a.String(),b.String())})
   for _,key:=range keys {k,_:=cfgModel(key);x,ok:=cfgModel(v.MapIndex(key));if !ok{return "",false};a=append(a,"["+k+"]="+x)};return "new Dictionary<string,object?> {"+strings.Join(a,",")+"}",true
 };return "",false
}
func cfgLine(test,name,model,input,want string,err error){e:="";if err!=nil{e=err.Error();if strings.HasPrefix(e,"json: unable to "){e="json: cannot "+strings.TrimPrefix(e,"json: unable to ")}};var fields []string;for _,s:=range []string{test,name,model,input,want,e}{fields=append(fields,base64.StdEncoding.EncodeToString([]byte(s)))};fmt.Println("CONFIGARSHAL:"+strings.Join(fields,"\t"))}
"""
    helper='//go:build goexperiment.jsonv2\npackage json\n'+model
    helper+='func cfgExportMarshal(){'+marshal+'\nfor _,tt:=range tests{if len(tt.opts)!=0 || tt.useWriter || tt.canonicalize {continue};v:=reflect.ValueOf(tt.in);if v.IsValid() && v.Kind()==reflect.Map && v.Len()>1 {continue};m,ok:=cfgModel(v);if ok{cfgLine("TestMarshal",tt.name.Name,m,"",tt.want,tt.wantErr)}}\n}\n'
    helper+='func cfgExportUnmarshal(){'+unmarshal+'\nfor _,tt:=range tests{if len(tt.opts)!=0 || tt.skip {continue};v:=reflect.ValueOf(tt.inVal);if !v.IsValid() || v.Type()!=reflect.TypeFor[*any]() || !v.Elem().IsNil() {continue};m,ok:=cfgModel(reflect.ValueOf(tt.want).Elem());if ok{cfgLine("TestUnmarshal",tt.name.Name,m,tt.inBuf,"",tt.wantErr)}}\n}\nfunc TestConfigurationArshalLiteralExport(t *testing.T){cfgExportMarshal();cfgExportUnmarshal()}\n'
    # Copy only imports actually referenced by extracted literal initializers;
    # original test declarations remain in their genuine package.
    packages=re.findall(r'^\s*"([^"]+)"',text[text.index('import ('):text.index('\n)',text.index('import ('))],re.M)
    packages+=['slices']
    imports=[]
    for package in sorted(set(packages)):
      alias=package.rsplit('/',1)[-1]
      if re.search(r'\b'+re.escape(alias)+r'\.',helper):imports.append('"'+package+'"')
    helper=helper.replace('package json\n','package json\nimport('+ ';'.join(imports)+')\n',1)
    path=base/'configuration_literal_export_test.go'
    if path.exists():raise RuntimeError('Temporary exporter exists')
    try:
      path.write_text(helper)
      run=subprocess.run([str(ROOT/'tools/.cache/go/bin/go'),'test','-v','-run','^TestConfigurationArshalLiteralExport$','-count=1','encoding/json/v2'],cwd=ROOT,capture_output=True,text=True,env={**__import__('os').environ,'GOEXPERIMENT':'jsonv2'})
      if run.returncode:raise RuntimeError(run.stdout+run.stderr)
    finally:path.unlink(missing_ok=True)
    rows=[]
    for line in run.stdout.splitlines():
      if line.startswith('CONFIGARSHAL:'):
        fields=line[len('CONFIGARSHAL:'):].split('\t')
        decoded=[base64.b64decode(f).decode() for f in fields[:3]]
        rows.append(decoded+fields[3:])
    code='// Generated from locked Go input/expected literals; no codec outputs.\n#nullable enable\nnamespace Configuration.Checks;\npublic static partial class JsonOriginalChecks\n{\n'
    counts={}
    for test in ['TestMarshal','TestUnmarshal']:
      selected=[r for r in rows if r[0]==test];counts[test]=len(selected)
      if not selected:raise RuntimeError('Empty selected '+test)
      code+='    private static readonly ArshalRow[] '+test+'Rows = [\n'
      for _,name,model,input,want,error in selected:code+='        new('+json.dumps(name)+', '+model+', '+json.dumps(input)+', '+json.dumps(want)+', '+json.dumps(error)+'),\n'
      code+='    ];\n'
    code+='}\n'
    provenance={'sourceFiles':{str(f.relative_to(ROOT)):hashlib.sha256(f.read_bytes()).hexdigest() for f in (source,base/'errors_test.go',base/'errors.go')},'sourceTests':counts,'selection':'Default (no option overrides), nonwriter, noncanonicalized standard bool/string/byte/list/map/int/uint/float64 value domains only. Map marshal >1 fields excluded for unspecified order. Go named types, arbitrary struct/method/reflection target encoders excluded. Typed nil slices adapted only at output-value equality. Unmarshal actual initially nil *any target rows only; prepopulated interfaces/merge and reflected typed destinations and target pointer identity excluded. Literal want/wantErr fields exported, not actual runtime codec results. Expected struct error rendering applies only original TestSemanticError cleanup of randomized errorModalVerb unable-to/cannot; no corpus diagnostic normalization.','generatedSha256':hashlib.sha256(code.encode()).hexdigest()}
    return code,json.dumps(provenance,indent=2)+'\n'

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--check',action='store_true');a=p.parse_args()
    code,prov=generate()
    for path,data in ((OUT,code),(PROV,prov)):
      if a.check:
        if path.read_text()!=data: raise RuntimeError('Generated asset differs: '+str(path))
      else:path.write_text(data)
    textcode,textprov=generate_jsontext()
    for path,data in ((OUT.with_name('JsonOriginalTextChecks.Generated.cs'),textcode),(PROV.with_name('json-original-text-provenance.json'),textprov)):
      if a.check:
        if path.read_text()!=data:raise RuntimeError('Generated asset differs: '+str(path))
      else:path.write_text(data)
    arshalcode,arshalprov=generate_arshal()
    for path,data in ((OUT.with_name('JsonOriginalArshalChecks.Generated.cs'),arshalcode),(PROV.with_name('json-original-arshal-provenance.json'),arshalprov)):
      if a.check:
        if path.read_text()!=data:raise RuntimeError('Generated asset differs: '+str(path))
      else:path.write_text(data)
    print('JSON literal assertions checked'  if a.check else 'JSON literal assertions generated')
