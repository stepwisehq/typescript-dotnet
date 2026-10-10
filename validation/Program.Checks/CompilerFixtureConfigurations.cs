// Authored assertion-harness counterpart of harnessutil.GetFileBasedTestConfigurations,
// splitOptionValues and compilerVaryBy at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Actual decoded original settings/options; no semantic flags or libraries rewritten.
using TypeScript.Scanner;
using TypeScript.TsOptions;
using T = TypeScript.TsOptions.TsOptions;
namespace Program.Checks;
public static class CompilerFixtureConfigurations
{
    public sealed record Named(string Name, Dictionary<string,string>? Config);
    private static readonly HashSet<string> VaryBy = new(StringComparer.Ordinal)
    {
        "all",
        "allowarbitraryextensions",
        "allowimportingtsextensions",
        "allowjs",
        "allowsyntheticdefaultimports",
        "allowumdglobalaccess",
        "allowunreachablecode",
        "allowunusedlabels",
        "alwaysstrict",
        "assumechangesonlyaffectdirectdependencies",
        "checkjs",
        "composite",
        "declaration",
        "declarationmap",
        "deduplicatepackages",
        "diagnostics",
        "disablereferencedprojectload",
        "disablesizelimit",
        "disablesolutionsearching",
        "disablesourceofprojectreferenceredirect",
        "downleveliteration",
        "emitbom",
        "emitdeclarationonly",
        "emitdecoratormetadata",
        "erasablesyntaxonly",
        "esmoduleinterop",
        "exactoptionalpropertytypes",
        "experimentaldecorators",
        "explainfiles",
        "extendeddiagnostics",
        "forceconsistentcasinginfilenames",
        "importhelpers",
        "incremental",
        "init",
        "inlinesourcemap",
        "inlinesources",
        "isolateddeclarations",
        "isolatedmodules",
        "jsx",
        "libreplacement",
        "listemittedfiles",
        "listfiles",
        "module",
        "moduledetection",
        "moduleresolution",
        "newline",
        "nocheck",
        "noemit",
        "noemithelpers",
        "noemitonerror",
        "noerrortruncation",
        "nofallthroughcasesinswitch",
        "noimplicitany",
        "noimplicitoverride",
        "noimplicitreturns",
        "noimplicitthis",
        "nolib",
        "nopropertyaccessfromindexsignature",
        "noresolve",
        "nouncheckedindexedaccess",
        "nouncheckedsideeffectimports",
        "nounusedlocals",
        "nounusedparameters",
        "preserveconstenums",
        "preservesymlinks",
        "preservewatchoutput",
        "pretty",
        "quiet",
        "removecomments",
        "resolvejsonmodule",
        "resolvepackagejsonexports",
        "resolvepackagejsonimports",
        "rewriterelativeimportextensions",
        "singlethreaded",
        "skipdefaultlibcheck",
        "skiplibcheck",
        "sourcemap",
        "stabletypeordering",
        "strict",
        "strictbindcallapply",
        "strictbuiltiniteratorreturn",
        "strictfunctiontypes",
        "strictnullchecks",
        "strictpropertyinitialization",
        "stripinternal",
        "target",
        "traceresolution",
        "usedefineforclassfields",
        "useunknownincatchvariables",
        "verbatimmodulesyntax",
        "version",
    };
    private static readonly CommandLineOption[] Extra = [
        new(){Name="allowNonTsExtensions",Kind=CommandLineOptionKind.Boolean},
        new(){Name="noErrorTruncation",Kind=CommandLineOptionKind.Boolean},
        new(){Name="suppressOutputPathCheck",Kind=CommandLineOptionKind.Boolean},
        new(){Name="noCheck",Kind=CommandLineOptionKind.Boolean}];
    private static string Trim(string value) => GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(value,GoUnicode.IsSpace),GoUnicode.IsSpace);
    public static List<Named> Expand(Dictionary<string,string> settings)
    {
        List<(string Name,List<string> Values)> entries=[];
        Dictionary<string,string> fixedOptions=new(StringComparer.Ordinal);
        int count=1;
        foreach(var (key,value) in settings)
        {
            if(!VaryBy.Contains(key)){fixedOptions[key]=value;continue;}
            var values=SplitValues(key,value);
            if(values.Count>1){count*=values.Count;if(count>25)throw new InvalidOperationException("Provided test options exceeded the maximum number of variations");entries.Add((key,values));}
            else if(values.Count==1)fixedOptions[key]=values[0];
        }
        List<Named> result=[];
        if(entries.Count==0){result.Add(new("",fixedOptions.Count==0?null:fixedOptions));return result;}
        void Visit(int index,Dictionary<string,string> current)
        {
            if(index==entries.Count)
            {
                string name=string.Join(",",current.OrderBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>pair.Key+"="+GoStrings.ToLower(pair.Value)));
                var config=new Dictionary<string,string>(current,StringComparer.Ordinal);foreach(var pair in fixedOptions)config[pair.Key]=pair.Value;
                result.Add(new(name,config));return;
            }
            var entry=entries[index];foreach(string value in entry.Values){current[entry.Name]=value;Visit(index+1,current);}
        }
        Visit(0,new(StringComparer.Ordinal));result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Name,b.Name));return result;
    }
    private static CommandLineOption? Declaration(string key) => T.OptionsDeclarations.Concat(Extra).FirstOrDefault(o=>string.Equals(o.Name,key,StringComparison.OrdinalIgnoreCase));
    private static (object? Value,bool Ok) Value(string key,string text)
    {
        var option=Declaration(key);if(option is null)return(null,false);
        string lower=GoStrings.ToLower(text);
        if(option.Kind==CommandLineOptionKind.Enum)return option.EnumMap()!.Get(lower);
        if(option.Kind==CommandLineOptionKind.Boolean)return lower switch{"true"=>(true,true),"false"=>(false,true),_=>(null,false)};
        return(text,true);
    }
    private static List<string> SplitValues(string key,string value)
    {
        if(value.Length==0)return [];
        bool star=false;List<string> includes=[],excludes=[];
        foreach(string part in value.Split(','))
        {
            string text=Trim(part);if(text.Length==0)continue;
            if(text=="*")star=true;else if(text[0] is '-' or '!')excludes.Add(text[1..]);else includes.Add(text);
        }
        if(includes.Count==0&&!star&&excludes.Count==0)return [];
        Dictionary<object,string> variations=[];
        void Include(string text){var parsed=Value(key,text);if(!parsed.Ok)throw new InvalidOperationException("Unknown value '"+text+"' for option '"+key+"'");variations.TryAdd(parsed.Value!,text);}
        foreach(string text in includes)Include(text);
        var declaration=Declaration(key);
        if(star&&declaration is not null)
        {
            if(declaration.Kind==CommandLineOptionKind.Enum)foreach(string text in declaration.EnumMap()!.Keys())Include(text);
            else if(declaration.Kind==CommandLineOptionKind.Boolean){Include("true");Include("false");}
        }
        foreach(string text in excludes){var parsed=Value(key,text);if(parsed.Ok)variations.Remove(parsed.Value!);}
        if(variations.Count==0)throw new InvalidOperationException("Variations in test option '@"+key+"' resulted in an empty set.");
        return variations.Values.ToList();
    }
}
