// Original pinned tstransforms/importelision_test.go TestImportElision20rows; shared xUnit/Native.
// Genuine Program fixture replaces original controlled fakeProgram; table expectations unchanged.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
using TypeScript.Transformers.TsTransforms;
namespace Emit.Checks;
public static class ImportElisionChecks
{
    public static int RunAll() { TestImportElision(); return 1; }
    // Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision_test.go:185.
    public static void TestImportElision()
    {
        (string Title, string Input, string Output, string Other, bool Jsx)[] rows =
        [
            ("ImportEquals#1", "import x = require(\"other\"); x;", "import x = require(\"other\");\nx;", "", false),
            ("ImportEquals#2", "import x = require(\"other\");", "", "", false),
            ("ImportDeclaration#1", "import \"m\";", "import \"m\";", "", false),
            ("ImportDeclaration#2", "import * as x from \"other\"; x;", "import * as x from \"other\";\nx;", "", false),
            ("ImportDeclaration#3", "import x from \"other\"; x;", "import x from \"other\";\nx;", "", false),
            ("ImportDeclaration#4", "import { x } from \"other\"; x;", "import { x } from \"other\";\nx;", "", false),
            ("ImportDeclaration#5", "import * as x from \"other\";", "", "", false),
            ("ImportDeclaration#6", "import x from \"other\";", "", "", false),
            ("ImportDeclaration#7", "import { x } from \"other\";", "", "", false),
            ("ExportDeclaration#1", "export * from \"other\";", "export * from \"other\";", "export let x;", false),
            ("ExportDeclaration#2", "export * as x from \"other\";", "export * as x from \"other\";", "export let x;", false),
            ("ExportDeclaration#3", "export * from \"other\";", "export * from \"other\";", "export let x;", false),
            ("ExportDeclaration#4", "export * as x from \"other\";", "export * as x from \"other\";", "export let x;", false),
            ("ExportDeclaration#5", "export { x } from \"other\";", "export { x } from \"other\";", "export let x;", false),
            ("ExportDeclaration#6", "export { x } from \"other\";", "", "export type x = any;", false),
            ("ExportDeclaration#7", "export { x }; let x;", "export { x };\nlet x;", "", false),
            ("ExportDeclaration#8", "export { x }; type x = any;", "", "", false),
            ("ExportDeclaration#9", "import { x } from \"other\"; export { x };", "", "export type x = any;", false),
            ("ExportAssignment#1", "let x; export default x;", "let x;\nexport default x;", "", false),
            ("ExportAssignment#2", "type x = any; export default x;", "", "", false),
        ];
        foreach (var row in rows)
        {
            // Same original TypeEraser -> ImportElision order, one real context/resolver.
            using var fixture = new TsTransformFixture(row.Input, row.Other, row.Jsx,
                new CompilerOptions { Module = ModuleKind.ESNext, NoLib = Tristate.True });
            PrinterChecks.CheckDiagnostics(fixture.File);
            if (fixture.Other != null) PrinterChecks.CheckDiagnostics(fixture.Other);
            var file = TypeEraserTransformer.NewTypeEraserTransformer(fixture.Options).TransformSourceFile(fixture.File);
            file = ImportElisionTransformer.NewImportElisionTransformer(fixture.Options).TransformSourceFile(file);
            PrinterChecks.CheckEmit(null, file, row.Output);
        }
    }
}
