using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectConfigChangeChecks
{
    private const string IndexText = "console.log(\"Hello, world!\");";
    private const string FooText = "export const foo = \"bar\";";
    private static Input[] Files() => [new("/tsconfig.more-base.json", "{}"), new("/tsconfig.base.json", "{\"extends\": \"../tsconfig.more-base.json\", \"compilerOptions\": {\"strict\": true}}"), new("/src/tsconfig.json", "{\"extends\": \"../tsconfig.base.json\", \"compilerOptions\": {\"target\": \"es6\"}, \"references\": [{\"path\": \"../utils\"}]}"), new("/src/index.ts", "console.log(\"Hello, world!\");"), new("/src/subfolder/foo.ts", "export const foo = \"bar\";"), new("/utils/tsconfig.json", "{\"compilerOptions\": {\"composite\": true}}"), new("/utils/index.ts", "console.log(\"Hello, test!\");"), ];
}
