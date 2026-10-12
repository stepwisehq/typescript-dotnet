// Original pinned tstransforms/typeeraser_test.go TestTypeEraser table; shared xUnit/Native.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
using TypeScript.Transformers.TsTransforms;
namespace Emit.Checks;
public static class TypeEraserChecks
{
    public static int RunAll() { TestTypeEraser(); return 1; }
    // Upstream typeeraser_test.go:15; original duplicate title retained by row.
    public static void TestTypeEraser()
    {
        (string Title, string Input, string Output, bool Jsx, bool Vms)[] rows =
        [
            ("Modifiers", "class C { public x; private y }", "class C {\n    x;\n    y;\n}", false, false),
            ("InterfaceDeclaration", "interface I { }", "", false, false),
            ("TypeAliasDeclaration", "type T = U;", "", false, false),
            ("NamespaceExportDeclaration", "export as namespace N;", "", false, false),
            ("UninstantiatedNamespace1", "namespace N {}", "", false, false),
            ("UninstantiatedNamespace2", "namespace N { export interface I {} }", "", false, false),
            ("UninstantiatedNamespace3", "namespace N { export type T = U; }", "", false, false),
            ("ExpressionWithTypeArguments", "F<T>", "F;", false, false),
            ("PropertyDeclaration1", "class C { declare x; }", "class C {\n}", false, false),
            ("PropertyDeclaration2", "class C { public x: number; }", "class C {\n    x;\n}", false, false),
            ("PropertyDeclaration3", "class C { public static x: number; }", "class C {\n    static x;\n}", false, false),
            ("ConstructorDeclaration1", "class C { constructor(); }", "class C {\n}", false, false),
            ("ConstructorDeclaration2", "class C { public constructor() {} }", "class C {\n    constructor() { }\n}", false, false),
            ("MethodDeclaration1", "class C { m(); }", "class C {\n}", false, false),
            ("MethodDeclaration2", "class C { public m<T>(): U {} }", "class C {\n    m() { }\n}", false, false),
            ("MethodDeclaration3", "class C { public static m<T>(): U {} }", "class C {\n    static m() { }\n}", false, false),
            ("GetAccessorDeclaration1", "class C { get m(); }", "class C {\n    get m() { }\n}", false, false),
            ("GetAccessorDeclaration2", "class C { public get m<T>(): U {} }", "class C {\n    get m() { }\n}", false, false),
            ("GetAccessorDeclaration3", "class C { public static get m<T>(): U {} }", "class C {\n    static get m() { }\n}", false, false),
            ("SetAccessorDeclaration1", "class C { set m(v); }", "class C {\n    set m(v) { }\n}", false, false),
            ("SetAccessorDeclaration2", "class C { public set m<T>(v): U {} }", "class C {\n    set m(v) { }\n}", false, false),
            ("SetAccessorDeclaration3", "class C { public static set m<T>(v): U {} }", "class C {\n    static set m(v) { }\n}", false, false),
            ("IndexSignature", "class C { [key: string]: number; }", "class C {\n}", false, false),
            ("VariableDeclaration1", "declare var a;", "", false, false),
            ("VariableDeclaration2", "var a: number", "var a;", false, false),
            ("HeritageClause", "class C implements I {}", "class C {\n}", false, false),
            ("ClassDeclaration1", "declare class C {}", "", false, false),
            ("ClassDeclaration2", "class C<T> {}", "class C {\n}", false, false),
            ("ClassExpression", "(class C<T> {})", "(class C {\n});", false, false),
            ("FunctionDeclaration1", "declare function f() {}", "", false, false),
            ("FunctionDeclaration2", "function f();", "", false, false),
            ("FunctionDeclaration3", "function f<T>(): U {}", "function f() { }", false, false),
            ("FunctionExpression", "(function f<T>(): U {})", "(function f() { });", false, false),
            ("ArrowFunction", "(<T>(): U => {})", "(() => { });", false, false),
            ("ParameterDeclaration", "function f(this: x, a: number, b?: boolean) {}", "function f(a, b) { }", false, false),
            ("CallExpression", "f<T>()", "f();", false, false),
            ("NewExpression1", "new f<T>()", "new f();", false, false),
            ("NewExpression2", "new f<T>", "new f;", false, false),
            ("TaggedTemplateExpression", "f<T>``", "f ``;", false, false),
            ("NonNullExpression", "x!", "x;", false, false),
            ("TypeAssertionExpression#1", "<T>x", "x;", false, false),
            ("TypeAssertionExpression#2", "(<T>x).c", "x.c;", false, false),
            ("AsExpression#1", "x as T", "x;", false, false),
            ("AsExpression#2", "(x as T).c", "x.c;", false, false),
            ("SatisfiesExpression#1", "x satisfies T", "x;", false, false),
            ("SatisfiesExpression#2", "(x satisfies T).c", "x.c;", false, false),
            ("JsxSelfClosingElement", "<x<T> />", "<x />;", true, false),
            ("JsxOpeningElement", "<x<T>></x>", "<x></x>;", true, false),
            ("ImportEqualsDeclaration#1", "import x = require(\"m\");", "import x = require(\"m\");", false, false),
            ("ImportEqualsDeclaration#2", "import type x = require(\"m\");", "", false, false),
            ("ImportEqualsDeclaration#3", "import x = y;", "import x = y;", false, false),
            ("ImportEqualsDeclaration#4", "import type x = y;", "", false, false),
            ("ImportDeclaration#1", "import \"m\";", "import \"m\";", false, false),
            ("ImportDeclaration#2", "import * as x from \"m\"; x;", "import * as x from \"m\";\nx;", false, false),
            ("ImportDeclaration#3", "import x from \"m\"; x;", "import x from \"m\";\nx;", false, false),
            ("ImportDeclaration#4", "import { x } from \"m\"; x;", "import { x } from \"m\";\nx;", false, false),
            ("ImportDeclaration#5", "import type * as x from \"m\";", "", false, false),
            ("ImportDeclaration#6", "import type x from \"m\";", "", false, false),
            ("ImportDeclaration#7", "import type { x } from \"m\";", "", false, false),
            ("ImportDeclaration#8", "import { type x } from \"m\";", "", false, false),
            ("ImportDeclaration#9", "import { type x } from \"m\";", "import {} from \"m\";", false, true),
            ("ExportDeclaration#1", "export * from \"m\";", "export * from \"m\";", false, false),
            ("ExportDeclaration#2", "export * as x from \"m\";", "export * as x from \"m\";", false, false),
            ("ExportDeclaration#3", "export { x } from \"m\";", "export { x } from \"m\";", false, false),
            ("ExportDeclaration#4", "export type * from \"m\";", "", false, false),
            ("ExportDeclaration#5", "export type * as x from \"m\";", "", false, false),
            ("ExportDeclaration#6", "export type { x } from \"m\";", "", false, false),
            ("ExportDeclaration#7", "export { type x } from \"m\";", "", false, false),
            ("ExportDeclaration#7", "export { type x } from \"m\";", "export {} from \"m\";", false, true),
        ];
        foreach (var row in rows)
        {
            var file = PrinterChecks.ParseTypeScript(row.Input, row.Jsx);
            PrinterChecks.CheckDiagnostics(file);
            var options = new CompilerOptions();
            if (row.Vms) options.VerbatimModuleSyntax = Tristate.True;
            var transformed = TypeEraserTransformer.NewTypeEraserTransformer(new TransformOptions { CompilerOptions = options, Context = new EmitContext() }).TransformSourceFile(file);
            PrinterChecks.CheckEmit(null, transformed, row.Output);
        }
    }
}
