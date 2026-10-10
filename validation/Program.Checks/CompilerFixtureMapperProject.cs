// Authored in-process adapter of the original contentmappertest fixture handlers
// and hostimpl decode/identity boundaries at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Actual callback algorithms, options and UTF-8 span semantics; no captured checker
// responses, fixture globals, process implementation or successful fallback.
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using TypeScript.ContentMapper;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using Map = TypeScript.SpanMap.SpanMap;
using Kind = TypeScript.SpanMap.Kind;
namespace Program.Checks;
public sealed class CompilerFixtureMapperProject : IProject
{
    private const string Preamble="const __VERSION = \"1.0.0\";\n";
    private readonly List<Mapper> mappers;
    private readonly CompilerOptions options;
    private readonly Dictionary<Mapper,string> identities=new(ReferenceEqualityComparer.Instance);
    private readonly JsonDocument compilerOptions;
    public CompilerFixtureMapperProject(List<Mapper> mappers,CompilerOptions options)
    {
        this.mappers=mappers;this.options=options;compilerOptions=JsonDocument.Parse(JsonCodec.Marshal(options));
        foreach(var mapper in mappers)identities[mapper]=CombinedIdentity(mapper,options);
    }
    public Exception? Refresh()=>null;
    public (List<string>?,Exception?) Identities()=>(mappers.Select(m=>identities[m]).ToList(),null);
    public (string,Exception?) Identity(Mapper mapper)=>identities.TryGetValue(mapper,out var value)?(value,null):("",new Exception("content mapper project is closed"));
    public (List<RootedFilePath>?,Exception?) WatchedFiles()=>(null,null);
    public Exception? Close(){compilerOptions.Dispose();return null;}
    private static string DiagnosticSource(Mapper mapper)=>mapper.Exec?.FirstOrDefault() switch{"compiler-test-mapper"=>"box","lisp-mapper"=>"lisp",_=>"mapper"};
    public List<OptionDiagnostic>? Diagnostics()
    {
        List<OptionDiagnostic>? result=null;
        foreach(var mapper in mappers)
            if(mapper.Options?.Bytes is {} raw&&GoString.FromBytes(raw)=="{\"plugins\":[{\"name\":1}]}")
                (result??=[]).Add(new(){Mapper=mapper,Source=DiagnosticSource(mapper),MessageText="Option 'name' requires a string.",Code=123,Path=[new(){Property="plugins"},new(){IsIndex=true,Index=0},new(){Property="name"}]});
        return result;
    }
    private static string CombinedIdentity(Mapper mapper,CompilerOptions options)
    {
        List<byte> bytes=[];bytes.AddRange(GoString.Bytes(mapper.Identity()));bytes.Add(0);bytes.AddRange(mapper.Options?.Bytes??[]);bytes.Add(0);bytes.Add(0); // static configIdentity is the empty source string.
        UInt128 identity=mapper.TransformIdentity(options);byte[] hash=new byte[16];BinaryPrimitives.WriteUInt64BigEndian(hash,(ulong)(identity>>64));BinaryPrimitives.WriteUInt64BigEndian(hash.AsSpan(8),(ulong)identity);bytes.AddRange(hash);
        return mapper.Identity()+":"+XxHash128.HashToUInt128(bytes.ToArray()).ToString("x32",System.Globalization.CultureInfo.InvariantCulture);
    }
    private sealed record RawDirective(int OriginalStart=0,int OriginalLength=0,int VirtualStart=0,int VirtualEnd=0,int Policy=0,int? UnusedIndex=null);
    private sealed record Unused(int Code=0,string Message="");
    private sealed record RawDirectives(List<RawDirective> Directives,List<Unused> Unused);
    private sealed record Output(string Text,string Extension,Map? Mappings=null,RawDirectives? Directives=null);
    private sealed record RawDiagnostic(int Start,int Length,int Code,string Message);
    public (Result Result,Exception? Error) Transform(Mapper mapper,Request request)
    {
        string name=mapper.Exec?.FirstOrDefault()??"";
        Output output;List<Output>? supplemental=null;List<RawDiagnostic>? diagnostics=null;
        try
        {
            switch(name)
            {
                case "compiler-test-mapper":(output,diagnostics)=TransformBox(request.Content);break;
                case "failing-mapper":throw new Exception("content mapper failed to transform the file");
                case "lisp-mapper":
                    string trimmed=request.Content.EndsWith('\n')?request.Content[..^1]:request.Content;
                    if(trimmed!="(+ 1 2 \"oops\")")throw new Exception("contentmappertest: unsupported Lisp expression "+GoQuote.Quote(request.Content));
                    output=new("add(1, 2, \"oops\");",".ts",SpanMaps.New([new(0,3,1,2,Kind.Alias,Feature.All),new(4,5,3,4,Kind.Verbatim,Feature.All),new(7,8,5,6,Kind.Verbatim,Feature.All),new(10,16,7,13,Kind.Verbatim,Feature.All)]));break;
                case "supplemental-mapper":output=new("export {};",".ts");supplemental=[IdentityOutput(request.Content)];break;
                case "supplemental-diagnostics-mapper":
                    const string prefix="missingSupplementalGlobal;\n";
                    output=new("export {};",".ts");supplemental=[new(prefix+request.Content,".ts",SpanMaps.New([new(prefix.Length,prefix.Length+request.Content.Length,0,request.Content.Length,Kind.Verbatim,Feature.All)]))];break;
                case "supplemental-globals-mapper":
                    string file=request.FileName.AsString();string text=file.EndsWith("/a.vue",StringComparison.Ordinal)?"/// <reference path=\"./extra.d.ts\" />\ninterface Shared extends Extra { value: string }":file.EndsWith("/b.vue",StringComparison.Ordinal)?"declare const shared: Shared;":throw new Exception("contentmappertest: unexpected supplemental global input "+GoQuote.Quote(file));
                    output=new("export default shared.value;",".ts");supplemental=[new(text,".ts")];break;
                case "supplemental-module-mapper":output=new("export default 1;",".ts");supplemental=[new("export const privateValue: number = \"wrong\";",".ts")];break;
                default:throw new Exception("contentmappertest: unknown mapper command "+name);
            }
        }
        catch(Exception error){return(default,TransformError.NewTransformError(TransformErrorKind.Request,new Exception("[-32603]: "+error.Message)));}
        try
        {
            string source=DiagnosticSource(mapper);var main=Decode(output,request.Content,source);
            Result result=new(){Text=main.Text,VirtualExtension=main.VirtualExtension,Mappings=main.Mappings,DiagnosticDirectives=main.DiagnosticDirectives};
            if(supplemental is not null)
            {
                result.Supplemental=[];for(int i=0;i<supplemental.Count;i++)
                {try{result.Supplemental.Add(Decode(supplemental[i],request.Content,source));}catch(DiagnosticDirectiveError error){error.SupplementalIndex=i;throw;}}
            }
            if(diagnostics is not null)
            {
                result.Diagnostics=[];foreach(var d in diagnostics)
                {
                    if(d.Start<0||d.Length<0||(long)d.Start+d.Length>long.MaxValue)throw new Exception($"invalid content mapper diagnostic range [{d.Start}, {d.Start+d.Length})");
                    int start,end;try{start=Normalize(request.Content,d.Start);}catch(Exception error){throw new Exception("invalid content mapper diagnostic start: "+error.Message);}
                    try{end=Normalize(request.Content,d.Start+d.Length);}catch(Exception error){throw new Exception("invalid content mapper diagnostic end: "+error.Message);}
                    result.Diagnostics.Add(Ast.NewExternalDiagnostic(null!,new TextRange(start,end),source,DiagnosticCategory.Error,d.Code,d.Message));
                }
            }
            return(result,null);
        }
        catch(Exception error){return(default,TransformError.NewTransformError(TransformErrorKind.Response,error));}
    }
    private static Output IdentityOutput(string text)=>new(text,".ts",SpanMaps.New([new(0,text.Length,0,text.Length,Kind.Verbatim,Feature.All)]));
    private (Output,List<RawDiagnostic>?) TransformBox(string content)
    {
        StringBuilder text=new(Preamble);List<Segment> segments=[];List<RawDiagnostic>? diagnostics=null;
        void Verbatim(int from,int to){if(to<=from)return;int start=text.Length;text.Append(content[from..to]);segments.Add(new(start,text.Length,from,to,Kind.Verbatim,Feature.All));}
        void Atom(string value,int from,int to){int start=text.Length;text.Append(value);segments.Add(new(start,text.Length,from,to,Kind.Atom,Feature.All));}
        int pos=0;
        while(pos<content.Length)
        {
            int relative=content[pos..].IndexOf("#{",StringComparison.Ordinal);if(relative<0){Verbatim(pos,content.Length);break;}
            int tokenStart=pos+relative,lineEnd=content.IndexOf('\n',tokenStart);if(lineEnd<tokenStart)lineEnd=content.Length;
            int close=content[tokenStart..lineEnd].IndexOf('}');
            if(close<0){Verbatim(pos,tokenStart);Atom("undefined",tokenStart,lineEnd);(diagnostics??=[]).Add(new(tokenStart,lineEnd-tokenStart,1000,"Unclosed interpolation."));pos=lineEnd;continue;}
            int tokenEnd=tokenStart+close+1;string name=content[(tokenStart+2)..(tokenEnd-1)];
            string value=compilerOptions.RootElement.TryGetProperty(name,out var option)?GoString.ToUtf8(option.GetRawText()):"undefined";
            Verbatim(pos,tokenStart);Atom(value,tokenStart,tokenEnd);pos=tokenEnd;
        }
        Map map=SpanMaps.New(segments);string extension=".ts";int newline=content.IndexOf('\n');const string extensionPrefix="// @box-extension:";
        if(newline>=0&&content.StartsWith(extensionPrefix,StringComparison.Ordinal))extension=Trim(content[extensionPrefix.Length..newline]);
        return(new(text.ToString(),extension,map,BoxDirectives(content,map)),diagnostics);
    }
    private static string Trim(string value)=>GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(value,GoUnicode.IsSpace),GoUnicode.IsSpace);
    private static RawDirectives? BoxDirectives(string content,Map map)
    {
        const string invalid="// @box-invalid-directive:";
        if(content.StartsWith(invalid,StringComparison.Ordinal))
        {
            int line=content.IndexOf('\n');string tag=Trim(content[invalid.Length..(line<0?content.Length:line)]);
            RawDirectives Wrap(List<RawDirective> ds,params Unused[] unused)=>new(ds,unused.ToList());
            var broken=tag switch
            {
                "invalid-range"=>Wrap([new(VirtualStart:-1)]),
                "original-range-out-of-bounds"=>Wrap([new(OriginalStart:content.Length+1)]),
                "virtual-range-out-of-bounds"=>Wrap([new(VirtualStart:1<<20,VirtualEnd:1<<20)]),
                "invalid-policy"=>Wrap([new(Policy:2)]),
                "ignore-with-unused-diagnostic"=>Wrap([new()],new Unused()),
                "expect-without-unused-diagnostic"=>Wrap([new(Policy:1)]),
                "invalid-unused-diagnostic-index"=>Wrap([new(Policy:1,UnusedIndex:1)],new Unused()),
                "overlap"=>Wrap([new(VirtualEnd:2),new(VirtualStart:1,VirtualEnd:3)]),
                _=>null
            };if(broken is not null)return broken;
        }
        List<RawDirective> directives=[];List<Unused> unusedDiagnostics=[];const string expect="// @box-expect-error";
        for(int start=0;start<content.Length;)
        {
            int end=content.IndexOf('\n',start);if(end<0)end=content.Length;string line=Trim(content[start..end]);int policy=-1,unused=-1;
            if(line=="// @box-ignore")policy=0;
            else if(line.StartsWith(expect+":",StringComparison.Ordinal)){policy=1;unused=unusedDiagnostics.Count;unusedDiagnostics.Add(new(2578,Trim(line[(expect.Length+1)..])));}
            if(policy>=0&&end<content.Length)
            {
                int affectedStart=end+1,affectedEnd=content.IndexOf('\n',affectedStart);if(affectedEnd<0)affectedEnd=content.Length;
                var spans=map.OriginalToVirtualSpans(new TextRange(affectedStart,affectedEnd),Feature.All);
                if(spans?.Count==1)directives.Add(new(start,end-start,spans[0].Span.Pos(),spans[0].Span.End(),policy,unused>=0?unused:null));
            }
            if(end==content.Length)break;start=end+1;
        }
        if(unusedDiagnostics.Count==1)for(int i=0;i<directives.Count;i++)directives[i]=directives[i] with{UnusedIndex=null};
        return directives.Count==0&&unusedDiagnostics.Count==0?null:new(directives,unusedDiagnostics);
    }
    private static int Normalize(string text,int position)
    {
        if(position<0)throw new Exception($"position {position} is negative");
        if(position>text.Length)throw new Exception($"position {position} exceeds utf-8 length {text.Length}");
        if(position<text.Length&&(text[position]&0xC0)==0x80)throw new Exception($"position {position} splits a Unicode code point");return position;
    }
    private static MappedResult Decode(Output output,string original,string source)
    {
        if(!ContentMappers.IsSupportedVirtualExtension(output.Extension))throw new InvalidVirtualExtensionError{Extension=output.Extension};
        var segments=output.Mappings.Segments();
        for(int i=0;i<(segments?.Count??0);i++)
        {
            var s=segments![i];
            int Position(string text,int pos,string role){try{return Normalize(text,pos);}catch(Exception error){throw new Exception($"invalid content mapper mapping {i} {role}: "+error.Message);}}
            segments[i]=new(Position(output.Text,s.VirtualStart,"virtual start"),Position(output.Text,s.VirtualEnd,"virtual end"),Position(original,s.OriginalStart,"original start"),Position(original,s.OriginalEnd,"original end"),s.Kind,s.Features);
        }
        return new(){Text=output.Text,VirtualExtension=output.Extension,Mappings=SpanMaps.New(segments),DiagnosticDirectives=NormalizeDirectives(output.Directives,output.Text,original,source)};
    }
    private static List<MappedDiagnosticDirective>? NormalizeDirectives(RawDirectives? raw,string text,string original,string source)
    {
        if(raw is null)return null;List<MappedDiagnosticDirective> result=[];
        for(int i=0;i<raw.Directives.Count;i++)
        {
            var d=raw.Directives[i];DiagnosticDirectiveError Error(DiagnosticDirectiveErrorKind kind)=>new(){Kind=kind,Index=i,SupplementalIndex=-1};
            MappedDiagnosticDirective converted=new(){Source=source};
            if(d.Policy==0)converted.Policy=MappedDiagnosticDirectivePolicy.Ignore;
            else if(d.Policy==1)
            {
                int index=d.UnusedIndex??0;if(d.UnusedIndex is null&&raw.Unused.Count!=1)throw Error(DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic);
                if(index<0||index>=raw.Unused.Count)throw Error(DiagnosticDirectiveErrorKind.InvalidUnusedDiagnosticIndex);
                converted.Policy=MappedDiagnosticDirectivePolicy.Expect;converted.UnusedCode=raw.Unused[index].Code;converted.UnusedMessageText=raw.Unused[index].Message;
            }
            else {var error=Error(DiagnosticDirectiveErrorKind.InvalidPolicy);error.Policy=(DiagnosticDirectivePolicy)d.Policy;throw error;}
            try{if(d.VirtualStart<0||d.VirtualEnd<d.VirtualStart)throw new Exception();converted.VirtualRange=new(Normalize(text,d.VirtualStart),Normalize(text,d.VirtualEnd));}catch{throw Error(DiagnosticDirectiveErrorKind.InvalidRange);}
            bool valid=d.OriginalStart>=0&&d.OriginalLength>=0&&(long)d.OriginalStart+d.OriginalLength<=long.MaxValue;
            if(valid){try{converted.OriginalRange=new(Normalize(original,d.OriginalStart),Normalize(original,d.OriginalStart+d.OriginalLength));}catch{valid=false;}}
            if(converted.Policy==MappedDiagnosticDirectivePolicy.Expect&&!valid)throw Error(DiagnosticDirectiveErrorKind.InvalidRange);
            result.Add(converted);
        }
        var sorted=result.Select((d,i)=>(Directive:d,Index:i)).OrderBy(x=>x.Directive.VirtualRange.Pos()).ToList();
        for(int i=1;i<sorted.Count;i++)if(sorted[i].Directive.VirtualRange.Pos()<sorted[i-1].Directive.VirtualRange.End())throw new DiagnosticDirectiveError{Kind=DiagnosticDirectiveErrorKind.Overlap,Index=sorted[i].Index,SupplementalIndex=-1};
        return result;
    }
}
