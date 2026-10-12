using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static readonly Input[] DroppedReferenceFiles = [new("/user/username/projects/myproject/main/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"composite\": true\n\t\t\t\t},\n\t\t\t\t\"references\": [{ \"path\": \"../dependency\" }]\n\t\t\t}"), new("/user/username/projects/myproject/main/main.ts", "\n\t\t\t\timport { fn1 } from '../dependency/fns'\n\t\t\t\tfn1();\n\t\t\t"), new("/user/username/projects/myproject/dependency/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"composite\": true\n\t\t\t\t}\n\t\t\t}"), new("/user/username/projects/myproject/dependency/fns.ts", "\n\t\t\t\texport function fn1() { }\n\t\t\t"), new("/user/username/projects/myproject/other/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"composite\": true\n\t\t\t\t}\n\t\t\t}"), new("/user/username/projects/myproject/other/other.ts", "export const y = 1;"), ];
}
