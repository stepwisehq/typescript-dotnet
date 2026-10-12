using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectBulkCacheChecks
{
    private const string UpdatedConfig = "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"strict\": true,\n\t\t\t\t\"target\": \"esnext\",\n\t\t\t\t\"types\": [\"node\"]\n\t\t\t},\n\t\t\t\"include\": [\"src/**/*\"]\n\t\t}";
    private const string DiscoveryConfig = "{\n\t\t\"compilerOptions\": {\n\t\t\t\"strict\": true\n\t\t},\n\t\t\"include\": [\"src/**/*\"]\n\t}";
    private const string DistConfig = "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"strict\": true\n\t\t\t},\n\t\t\t\"include\": [\"src/**/*\"]\n\t\t}";
    private const string NestedConfig = "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"strict\": false,\n\t\t\t\t\"target\": \"esnext\"\n\t\t\t}\n\t\t}";
    private const string AppConfig = "{\n\t\t\t\"compilerOptions\": { \"composite\": true, \"target\": \"esnext\" },\n\t\t\t\"include\": [\"**/*\"]\n\t\t}";
    private const string SolutionConfig = "{\n\t\t\t\t\"compilerOptions\": { \"composite\": true },\n\t\t\t\t\"files\": [],\n\t\t\t\t\"references\": [{ \"path\": \"./app\" }]\n\t\t\t}";
    private const string IndexText = "import { helper } from \"./helper\"; console.log(helper);";
    private static Input[] BaseFiles() => [new("/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"strict\": true,\n\t\t\t\t\"target\": \"es2015\",\n\t\t\t\t\"types\": [\"node\"]\n\t\t\t},\n\t\t\t\"include\": [\"src/**/*\"]\n\t\t}"), new("/project/src/index.ts", "import { helper } from \"./helper\"; console.log(helper);"), new("/project/src/helper.ts", "export const helper = \"test\";"), new("/project/src/utils/lib.ts", "export function util() { return \"util\"; }"), new("/project/node_modules/@types/node/index.d.ts", "import \"./fs\"; import \"./console\";"), new("/project/node_modules/@types/node/fs.d.ts", ""), new("/project/node_modules/@types/node/console.d.ts", ""), ];
}
