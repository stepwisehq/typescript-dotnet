using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.Vfs;
using Program.Checks;
namespace WholeConsumer;
internal static class Entry {public static int Main(string[] args) {
if(args.Length==3 && args[0]=="--reconcile-configurations")return Reconcile(Path.GetFullPath(args[1]),args[2]);
if(args.Length!=3)throw new ArgumentException("root requests output required");
string root=Path.GetFullPath(args[0]);
Dictionary<string,JsonObject> inventory=[];
using(var source=new StreamReader(new GZipStream(File.OpenRead(root+"/validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz"),CompressionMode.Decompress)))
while(source.ReadLine() is {} line){var row=JsonNode.Parse(line)!.AsObject();inventory.Add(Path.GetFullPath(root+"/"+row["source"]!.GetValue<string>()),row);}
using var output=new StreamWriter(args[2]);int failures=0;
foreach(string line in File.ReadLines(args[1])){
 var request=JsonNode.Parse(line)!.AsObject();string name=request["Name"]!.GetValue<string>();
 try{
 foreach(var key in request.Select(pair=>pair.Key))if(key is not("Name" or "Source" or "SourceSha256" or "Configuration"))throw new ArgumentException("Unknown request key");
 string source=Path.GetFullPath(request["Source"]!.GetValue<string>()),configuration=request["Configuration"]!.GetValue<string>();
 var row=inventory[source];byte[] bytes=File.ReadAllBytes(source);string hash=Convert.ToHexStringLower(SHA256.HashData(bytes));
 if(hash!=request["SourceSha256"]!.GetValue<string>()||hash!=row["sourceSha256"]!.GetValue<string>())throw new Exception("Fixture hash changed");
 string decoded=VfsInternal.DecodeBytes(GoString.FromBytes(bytes)).Contents;
 var expanded=CompilerFixtureConfigurations.Expand(CompilerFixtureParser.ExtractCompilerSettings(decoded));
 var variant=expanded.Single(item=>item.Name==configuration);
 var options=variant.Config??new Dictionary<string,string>(StringComparer.Ordinal);
 var result=Program.Checks.ProgramRunner.ObserveCompilerNoEmit(name,GoString.ToUtf8(source),hash,configuration,decoded,options,root+"/upstream/TypeScript/tsc/testdata",row["originalPreConfigurationSkip"]!.GetValue<bool>());
 output.WriteLine(result.ToJsonString());output.Flush();
 }catch(Exception error){failures++;output.WriteLine(new JsonObject{["name"]=name,["status"]="failed-managed-runtime",["errorBase64"]=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(error.ToString()))}.ToJsonString());output.Flush();Console.Error.WriteLine(name+": "+error);}
}
return failures==0?0:1;}
private static int Reconcile(string root,string destination)
{
 int cases=0,exact=0,failed=0,originalFailures=0;
 using var input=new StreamReader(new GZipStream(File.OpenRead(root+"/validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz"),CompressionMode.Decompress));
 using var output=new StreamWriter(destination);
 while(input.ReadLine() is {} line)
 {
  var row=JsonNode.Parse(line)!.AsObject();string source=root+"/"+row["source"]!.GetValue<string>();cases++;
  var result=new JsonObject{["source"]=row["source"]!.DeepClone()};
  bool originalFailed=row["status"]!.GetValue<string>()!="decoded-configuration-enumerated";
  try
  {
   byte[] raw=File.ReadAllBytes(source);if(Convert.ToHexStringLower(SHA256.HashData(raw))!=row["sourceSha256"]!.GetValue<string>())throw new Exception("Original source hash mismatch");
   string decoded=VfsInternal.DecodeBytes(GoString.FromBytes(raw)).Contents;
   var settings=CompilerFixtureParser.ExtractCompilerSettings(decoded);
   var configs=CompilerFixtureConfigurations.Expand(settings);
   if(originalFailed){result["exact"]=false;result["error"]="Managed expansion succeeded where original failed";failed++;}
   else
   {
    var expected=row["configurations"]!;
    var actualSettings=DisplayObject(settings);
    // Config values are original Go strings in the JSON producer; convert byte role once.
    var actual=new JsonArray();
    foreach(var configuration in configs)actual.Add((JsonNode)new JsonObject{["Name"]=configuration.Name,["Config"]=configuration.Config is null?null:DisplayObject(configuration.Config)});
    var payload=CompilerFixtureParser.Parse(decoded,GoString.ToUtf8(source));
    var units=new JsonArray();
    foreach(var unit in payload.Units)units.Add((JsonNode)new JsonObject{["name"]=GoString.ToString(unit.Name),["textSha256"]=Convert.ToHexStringLower(SHA256.HashData(GoString.Bytes(unit.Content))),["textBytes"]=unit.Content.Length,["fileOptions"]=DisplayObject(unit.FileOptions)});
    var links=new JsonObject();
    foreach(var link in payload.Symlinks)links.Add(GoString.ToString(link.Key),GoString.ToString(link.Value));
    var globals=DisplayObject(payload.GlobalOptions);
    bool same=JsonNode.DeepEquals(row["symlinks"],links)&&JsonNode.DeepEquals(row["globalOptions"],globals)&&GoString.ToString(payload.CurrentDirectory)==row["currentDirectory"]!.GetValue<string>()&&JsonNode.DeepEquals(expected,actual)&&JsonNode.DeepEquals(row["rawSettings"],actualSettings)&&JsonNode.DeepEquals(row["units"],units)&&Convert.ToHexStringLower(SHA256.HashData(GoString.Bytes(decoded)))==row["decodedTextSha256"]!.GetValue<string>();
    result["exact"]=same;if(same)exact++;else {failed++;result["expectedConfigurations"]=expected.DeepClone();result["actualConfigurations"]=actual;result["settingsExact"]=JsonNode.DeepEquals(row["rawSettings"],actualSettings);result["unitsExact"]=JsonNode.DeepEquals(row["units"],units);}
   }
  }
  catch(Exception error){if(originalFailed&&error is InvalidOperationException&&error.Message==row["originalExpansionFailureText"]?.GetValue<string>()){originalFailures++;result["exact"]=true;result["classification"]="original-and-managed-expansion-failure-not-semantic-pass";}else {failed++;result["exact"]=false;}result["error"]=error.Message;}
  output.WriteLine(result.ToJsonString());
 }
 Console.WriteLine(new JsonObject{["cases"]=cases,["exact"]=exact,["originalFailures"]=originalFailures,["failed"]=failed}.ToJsonString());return failed==0?0:1;
}
private static JsonObject DisplayObject(IEnumerable<KeyValuePair<string,string>> values)
{
 var result=new JsonObject();
 foreach(var item in values)result.Add(item.Key,GoString.ToString(item.Value));
 return result;
}
}
