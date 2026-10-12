// Exact literal file maps from project_test.go TestProjectProgramUpdateKind.
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static readonly Input[] InitialFiles = [new("/src/tsconfig.json", "{}"), new("/src/index.ts", "export const x = 1;"), ];
    private static readonly Input[] SingleChangeFiles = [new("/src/tsconfig.json", "{}"), new("/src/index.ts", "console.log('Hello');"), ];
    private static readonly Input[] ResolutionFiles = [new("/src/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\":{\"module\":\"preserve\",\"moduleResolution\":\"bundler\",\"noEmit\":true},\n\t\t\t\t\"files\":[\"index.ts\"]\n\t\t\t}"), new("/src/index.ts", "import type { Value } from \"pkg\" with { \"resolution-mode\": \"require\" };\nconst value: Value = { mode: \"require\" };"), new("/src/node_modules/pkg/package.json", "{\n\t\t\t\t\"name\": \"pkg\",\n\t\t\t\t\"version\": \"1.0.0\",\n\t\t\t\t\"exports\": {\n\t\t\t\t\t\".\": {\n\t\t\t\t\t\t\"import\": \"./index.mjs\",\n\t\t\t\t\t\t\"require\": \"./index.js\"\n\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t}"), new("/src/node_modules/pkg/index.d.mts", "export interface Value { mode: \"import\" }"), new("/src/node_modules/pkg/index.d.ts", "export interface Value { mode: \"require\" }"), ];
    private static readonly Input[] ConfigChangeFiles = [new("/src/tsconfig.json", "{\"compilerOptions\": {\"strict\": true}}"), new("/src/index.ts", "export const x = 1;"), ];
    private static readonly Input[] RootAdditionFiles = [new("/src/tsconfig.json", "{}"), new("/src/index.ts", "export {}"), ];
    private static readonly Input[] MissingImportFiles = [new("/src/tsconfig.json", "{}"), new("/src/index.ts", "export const x = 1;"), new("/src/other.ts", "export const z = 3;"), ];
}
