// Authored test harness counterpart of the regular compiler-test branch of
// tsc/internal/testrunner/test_case_parser.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Source/unit/path contents retain exact Go bytes. Fourslash is a separate harness.
using System.Text;
using System.Text.RegularExpressions;
using TypeScript.Scanner;

namespace Program.Checks;

public static class CompilerFixtureParser
{
    public sealed record Unit(string Name, string Content, Dictionary<string, string> FileOptions);
    public sealed record Payload(List<Unit> Units, Dictionary<string, string> Symlinks, string CurrentDirectory, Dictionary<string, string> GlobalOptions);
    private static readonly Regex Lines = new("\\r?\\n", RegexOptions.CultureInvariant);
    // Go RE2's ASCII \\w and \\s classes, with the same greedy capture groups.
    private static readonly Regex Option = new("^//[\\t\\n\\f\\r ]*@([A-Za-z0-9_]+)[\\t\\n\\f\\r ]*:[\\t\\n\\f\\r ]*([^\\r\\n]*)", RegexOptions.CultureInvariant);
    private static readonly Regex Link = new("^//[\\t\\n\\f\\r ]*@link[\\t\\n\\f\\r ]*:[\\t\\n\\f\\r ]*([^\\r\\n]*)[\\t\\n\\f\\r ]*->[\\t\\n\\f\\r ]*([^\\r\\n]*)", RegexOptions.CultureInvariant);
    private static string Trim(string text) => GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(text, GoUnicode.IsSpace), GoUnicode.IsSpace);

    public static Payload Parse(string goBytes, string fileName)
    {
        List<Unit> units = [];
        Dictionary<string, string> symlinks = new(StringComparer.Ordinal), global = new(StringComparer.Ordinal), fileOptions = new(StringComparer.Ordinal);
        var content = new StringBuilder();
        string currentName = "", directory = "";
        foreach (string line in Lines.Split(goBytes))
        {
            Match link = Link.Match(line);
            if (link.Success) { symlinks[Trim(link.Groups[2].Value)] = Trim(link.Groups[1].Value); continue; }
            Match option = Option.Match(line);
            if (option.Success)
            {
                string name = option.Groups[1].Value.ToLowerInvariant(), value = Trim(option.Groups[2].Value);
                if (name == "currentdirectory") directory = value;
                if (name != "filename")
                {
                    if (name == "symlink" && currentName != "")
                    {
                        foreach (string item in value.Split(',')) { string path = Trim(item); if (path != "") symlinks[path] = currentName; }
                    }
                    else if (name is "emitthisfile" or "noopen") fileOptions[name] = value;
                    else global[name] = value; // Original differing duplicates overwrite rather than fail.
                    continue;
                }
                if (currentName != "")
                {
                    units.Add(new(currentName, content.ToString(), fileOptions));
                    content.Clear(); currentName = value; fileOptions = new(StringComparer.Ordinal);
                }
                else
                {
                    string prefix = content.ToString();
                    if (prefix.Length != 0 && Scanner.SkipTrivia(GoString.Bytes(prefix), 0) != prefix.Length)
                        throw new InvalidOperationException("Non-comment test content appears before the first '// @Filename' directive");
                    content.Clear(); currentName = Trim(option.Groups[2].Value); fileOptions = new(StringComparer.Ordinal);
                }
            }
            else
            {
                // Exact regular harness behavior drops leading empty lines.
                if (content.Length != 0) content.Append('\n');
                content.Append(line);
            }
        }
        if (units.Count == 0 && currentName == "") currentName = TsPath.GetBaseFileName(fileName);
        units.Add(new(currentName, content.ToString(), fileOptions));
        return new(units, symlinks, directory, global);
    }

    public static Dictionary<string, string> ExtractCompilerSettings(string bytes)
    {
        Dictionary<string, string> settings = new(StringComparer.Ordinal);
        foreach (string line in Lines.Split(bytes))
        {
            Match option = Option.Match(line);
            if (!option.Success) continue;
            string value = Trim(option.Groups[2].Value);
            if (value.EndsWith(';')) value = value[..^1];
            settings[option.Groups[1].Value.ToLowerInvariant()] = value;
        }
        return settings;
    }
}
