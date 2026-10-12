// Exact raw templates from projectreferencesprogram_test.go fixture workers.
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static Dictionary<string, Input> ReferencedFiles(bool disable)
    {
        var result = new Dictionary<string, Input>();
        result.Add("/user/username/projects/myproject/main/tsconfig.json", new("/user/username/projects/myproject/main/tsconfig.json", "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true%s\n\t\t\t},\n\t\t\t\"references\": [{ \"path\": \"../dependency\" }]\n\t\t}".Replace("%s", disable ? ", \"disableSourceOfProjectReferenceRedirect\": true" : "", StringComparison.Ordinal)));
        result.Add("/user/username/projects/myproject/main/main.ts", new("/user/username/projects/myproject/main/main.ts", "\n\t\t\timport {\n\t\t\t\tfn1,\n\t\t\t\tfn2,\n\t\t\t\tfn3,\n\t\t\t\tfn4,\n\t\t\t\tfn5\n\t\t\t} from '../decls/fns'\n\t\t\tfn1();\n\t\t\tfn2();\n\t\t\tfn3();\n\t\t\tfn4();\n\t\t\tfn5();\n\t\t"));
        result.Add("/user/username/projects/myproject/dependency/tsconfig.json", new("/user/username/projects/myproject/dependency/tsconfig.json", "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"declarationDir\": \"../decls\"\n\t\t\t},\n\t\t}"));
        result.Add("/user/username/projects/myproject/dependency/fns.ts", new("/user/username/projects/myproject/dependency/fns.ts", "\n\t\t\texport function fn1() { }\n\t\t\texport function fn2() { }\n\t\t\texport function fn3() { }\n\t\t\texport function fn4() { }\n\t\t\texport function fn5() { }\n\t\t"));
        return result;
    }

    private const string filesForSymlinkReferencesRaw0 = "{\n\t\t\t\"main\": \"lib/index.js\",\n\t\t\t\"types\": \"lib/index.d.ts\"\n\t\t}";
    private const string filesForSymlinkReferencesRaw1 = "\n\t\t\timport { foo } from '%sb';\n\t\t\timport { bar } from '%sb/lib/bar';\n\t\t\tfoo();\n\t\t\tbar();\n\t\t";
    private const string filesForSymlinkReferencesRaw2 = "export function foo() { }";
    private const string filesForSymlinkReferencesRaw3 = "export function bar() { }";
    private const string filesForSymlinkReferencesRaw4 = "/user/username/projects/myproject/node_modules/%sb";
    private const string filesForSymlinkReferencesInSubfolderRaw0 = "{}";
    private const string filesForSymlinkReferencesInSubfolderRaw1 = "\n\t\t\timport { foo } from '%sb/lib/foo';\n\t\t\timport { bar } from '%sb/lib/bar/foo';\n\t\t\tfoo();\n\t\t\tbar();\n\t\t";
    private const string filesForSymlinkReferencesInSubfolderRaw2 = "export function foo() { }";
    private const string filesForSymlinkReferencesInSubfolderRaw3 = "export function bar() { }";
    private const string filesForSymlinkReferencesInSubfolderRaw4 = "/user/username/projects/myproject/node_modules/%sb";
    private const string filesForDirectorySubpathSymlinkReferencesRaw0 = "{\n\t\t\t\"main\": \"lib/index.js\",\n\t\t\t\"types\": \"lib/index.d.ts\"\n\t\t}";
    private const string filesForDirectorySubpathSymlinkReferencesRaw1 = "\n\t\t\timport { helper } from \"%sb/lib/File\";\n\t\t\texport const result: number = helper();\n\t\t";
    private const string filesForDirectorySubpathSymlinkReferencesRaw2 = "export function helper(): number { return 1; }";
}
