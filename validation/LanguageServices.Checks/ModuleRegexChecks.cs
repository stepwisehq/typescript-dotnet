// Authored public module-specifier exclusion controls; no original upstream test claim.
using TypeScript.Scanner;
using ModuleFunctions = TypeScript.ModuleSpecifiers.ModuleSpecifiers;

namespace LanguageServices.Checks;
public static class ModuleRegexChecks
{
    public static int RunAll()
    {
        Check("Foo", ["/^foo$/i"], true);
        Check("foo", ["/foo/z"], true);
        Check("FOO/BAR", ["/foo\\/bar/i"], true);
        Check("FOO/BAR", ["/foo/bar/i"], false);
        Check("/foo/bar/i", ["/foo/bar/i"], true);
        Check("abc", ["[", "^abc$"], true);
        Check("abc", ["["], false);
        Check("abc", ["["], false); // Cached invalid admission retains nil.
        Check("abc", [], false);
        return 1;
    }
    private static void Check(string name, string[] patterns, bool expected)
    {
        bool actual = ModuleFunctions.IsExcludedByRegex(name, Slice<string>.FromArray(patterns));
        if (actual != expected) throw new InvalidOperationException("wrong module exclusion: " + name);
    }
}
