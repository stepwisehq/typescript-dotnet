// Whole original projectcollectiondefaultproject_test.go function.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class ProjectDefaultProjectChecks
{
    public static int RunAll()
    {
        TestProjectCollectionDefaultProject();
        return 1;
    }

    public static void TestProjectCollectionDefaultProject()
    {
        Input[] files = [new("/project1/tsconfig.json", "{\n\t\t\t\"extends\": \"../tsconfig.json\",\n\t\t\t\"files\": [],\n\t\t\t\"include\": [\"src/**/*\"],\n\t\t\t\"references\": [\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"../project2\"\n\t\t\t\t}\n\t\t\t],\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"outDir\": \"./dist\",\n\t\t\t\t\"rootDir\": \"./src\",\n\t\t\t}\n\t\t}"), new("/project1/src/index.ts", "export const foo = 42;\n\t\texport type Bar = { a: string };"), new("/project1/dist/index.d.ts", "export declare const foo = 42;\n\t\t\texport type Bar = {\n\t\t\t\ta: string;\n\t\t\t};"), new("/project2/tsconfig.json", "{\n\t\t\t\"extends\": \"../tsconfig.json\",\n\t\t\t\"files\": [],\n\t\t\t\"include\": [\"src/**/*\"],\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"outDir\": \"./dist\",\n\t\t\t\t\"rootDir\": \"./src\"\n\t\t\t}\n\t\t}"), new("/project3/tsconfig.json", "{\n\t\t\t\"extends\": \"../tsconfig.json\",\n\t\t\t\"files\": [],\n\t\t\t\"include\": [\"src/**/*\"],\n\t\t\t\"references\": [\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"../project1\"\n\t\t\t\t}\n\t\t\t],\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"outDir\": \"./dist\",\n\t\t\t\t\"rootDir\": \"./src\",\n\t\t\t}\n\t\t}"), new("/project3/src/index.ts", "import { Bar } from \"../../project1/dist/index.js\";\n\t\t\tdeclare const b: Bar;\n\t\t\tconst x: string = b.a;"), new("/project4/tsconfig.json", "{\n\t\t\t\"extends\": \"../tsconfig.json\",\n\t\t\t\"files\": [],\n\t\t\t\"include\": [\"src/**/*\"],\n\t\t\t\"references\": [\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"../project1\"\n\t\t\t\t}\n\t\t\t],\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"outDir\": \"./dist\",\n\t\t\t\t\"rootDir\": \"./src\",\n\t\t\t}\n\t\t}"), new("/project4/src/index.ts", "import { Bar } from \"../../project1/dist/index.js\";\ndeclare const b: Bar;\nconst x: string = b.a;"), new("/tsconfig.json", "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"disableReferencedProjectLoad\": true,\n\t\t\t\t\"disableSolutionSearching\": true,\n\t\t\t\t\"disableSourceOfProjectReferenceRedirect\": true\n\t\t\t},\n\t\t\t\"files\": [],\n\t\t\t\"references\": [\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"./project1\"\n\t\t\t\t},\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"./project2\"\n\t\t\t\t},\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"./project3\"\n\t\t\t\t},\n\t\t\t\t{\n\t\t\t\t\t\"path\": \"./project4\"\n\t\t\t\t}\n\t\t\t]\n\t\t}"), ];
        using var fixture = new ProjectSessionFixture(files);
        string[] paths = ["/project1/dist/index.d.ts", "/project1/src/index.ts", "/project3/src/index.ts", "/project4/src/index.ts"];
        foreach (var path in paths)
        {
            var content = files.Single(file => file.Path == path).Contents;
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file://" + path)), 1, ByteStringValue.FromString(content), LanguageKind.TypeScript);
        }
    // Original assertion: opening these files must not crash; no added result baseline.
    }
}
