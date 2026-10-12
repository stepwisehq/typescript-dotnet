// Original assertions from pinned printer/namegenerator_test.go; C# test adaptation.
// Go t.Parallel is omitted; original identities, source parsing/binding and expectations remain.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace Emit.Checks;
public static class NameGeneratorChecks
{
    public static readonly string[] Names = [
        "TestTempVariable1",
        "TestTempVariable2",
        "TestTempVariable3",
        "TestTempVariableScoped",
        "TestTempVariableScopedReserved",
        "TestLoopVariable1",
        "TestLoopVariable2",
        "TestLoopVariable3",
        "TestLoopVariableScoped",
        "TestUniqueName1",
        "TestUniqueName2",
        "TestUniqueNameScoped",
        "TestUniquePrivateName1",
        "TestUniquePrivateName2",
        "TestUniquePrivateNameScoped",
        "TestGeneratedNameForIdentifier1",
        "TestGeneratedNameForIdentifier2",
        "TestGeneratedNameForIdentifier3",
        "TestGeneratedNameForNamespace1",
        "TestGeneratedNameForNamespace2",
        "TestGeneratedNameForNamespace3",
        "TestGeneratedNameForNamespace4",
        "TestGeneratedNameForNodeCached",
        "TestGeneratedNameForImport",
        "TestGeneratedNameForExport",
        "TestGeneratedNameForFunctionDeclaration1",
        "TestGeneratedNameForFunctionDeclaration2",
        "TestGeneratedNameForClassDeclaration1",
        "TestGeneratedNameForClassDeclaration2",
        "TestGeneratedNameForExportAssignment",
        "TestGeneratedNameForClassExpression",
        "TestGeneratedNameForMethod1",
        "TestGeneratedNameForMethod2",
        "TestGeneratedPrivateNameForMethod",
        "TestGeneratedNameForComputedPropertyName",
        "TestGeneratedNameForOther",
    ];
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestTempVariable1": TestTempVariable1(); break;
            case "TestTempVariable2": TestTempVariable2(); break;
            case "TestTempVariable3": TestTempVariable3(); break;
            case "TestTempVariableScoped": TestTempVariableScoped(); break;
            case "TestTempVariableScopedReserved": TestTempVariableScopedReserved(); break;
            case "TestLoopVariable1": TestLoopVariable1(); break;
            case "TestLoopVariable2": TestLoopVariable2(); break;
            case "TestLoopVariable3": TestLoopVariable3(); break;
            case "TestLoopVariableScoped": TestLoopVariableScoped(); break;
            case "TestUniqueName1": TestUniqueName1(); break;
            case "TestUniqueName2": TestUniqueName2(); break;
            case "TestUniqueNameScoped": TestUniqueNameScoped(); break;
            case "TestUniquePrivateName1": TestUniquePrivateName1(); break;
            case "TestUniquePrivateName2": TestUniquePrivateName2(); break;
            case "TestUniquePrivateNameScoped": TestUniquePrivateNameScoped(); break;
            case "TestGeneratedNameForIdentifier1": TestGeneratedNameForIdentifier1(); break;
            case "TestGeneratedNameForIdentifier2": TestGeneratedNameForIdentifier2(); break;
            case "TestGeneratedNameForIdentifier3": TestGeneratedNameForIdentifier3(); break;
            case "TestGeneratedNameForNamespace1": TestGeneratedNameForNamespace1(); break;
            case "TestGeneratedNameForNamespace2": TestGeneratedNameForNamespace2(); break;
            case "TestGeneratedNameForNamespace3": TestGeneratedNameForNamespace3(); break;
            case "TestGeneratedNameForNamespace4": TestGeneratedNameForNamespace4(); break;
            case "TestGeneratedNameForNodeCached": TestGeneratedNameForNodeCached(); break;
            case "TestGeneratedNameForImport": TestGeneratedNameForImport(); break;
            case "TestGeneratedNameForExport": TestGeneratedNameForExport(); break;
            case "TestGeneratedNameForFunctionDeclaration1": TestGeneratedNameForFunctionDeclaration1(); break;
            case "TestGeneratedNameForFunctionDeclaration2": TestGeneratedNameForFunctionDeclaration2(); break;
            case "TestGeneratedNameForClassDeclaration1": TestGeneratedNameForClassDeclaration1(); break;
            case "TestGeneratedNameForClassDeclaration2": TestGeneratedNameForClassDeclaration2(); break;
            case "TestGeneratedNameForExportAssignment": TestGeneratedNameForExportAssignment(); break;
            case "TestGeneratedNameForClassExpression": TestGeneratedNameForClassExpression(); break;
            case "TestGeneratedNameForMethod1": TestGeneratedNameForMethod1(); break;
            case "TestGeneratedNameForMethod2": TestGeneratedNameForMethod2(); break;
            case "TestGeneratedPrivateNameForMethod": TestGeneratedPrivateNameForMethod(); break;
            case "TestGeneratedNameForComputedPropertyName": TestGeneratedNameForComputedPropertyName(); break;
            case "TestGeneratedNameForOther": TestGeneratedNameForOther(); break;
            default: throw new ArgumentException("Unknown original name test: " + name);
        }
    }
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    private static void Equal(string expected, string actual)
    {
        if (expected != actual) throw new InvalidOperationException($"Original generated-name assertion: expected {expected}, actual {actual}");
    }
    private static SourceFile ParseTypeScript(string text, bool jsx) => Parser.ParseSourceFile(
        new SourceFileParseOptions(jsx ? "/main.tsx" : "/main.ts", jsx ? "/main.tsx" : "/main.ts"),
        text, jsx ? ScriptKind.TSX : ScriptKind.TS);
private static void TestTempVariable1() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewTempVariable();
	var name2 = ec.Factory.NewTempVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("_a", text1);
	Equal("_b", text2);
}

private static void TestTempVariable2() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewTempVariableEx(new AutoGenerateOptions {
		Prefix = "A",
		Suffix = "B",
	});
	var name2 = ec.Factory.NewTempVariableEx(new AutoGenerateOptions {
		Prefix = "A",
		Suffix = "B",
	});

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("A_aB", text1);
	Equal("A_bB", text2);
}

private static void TestTempVariable3() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewTempVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name1);

	Equal("_a", text1);
	Equal("_a", text2);
}

private static void TestTempVariableScoped() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewTempVariable();
	var name2 = ec.Factory.NewTempVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	g.PushScope(false);
	var text2 = g.GenerateName(name2);
	g.PopScope(false);

	Equal("_a", text1);
	Equal("_a", text2);
}

private static void TestTempVariableScopedReserved() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewTempVariableEx(new AutoGenerateOptions {Flags = GeneratedIdentifierFlags.ReservedInNestedScopes});
	var name2 = ec.Factory.NewTempVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	g.PushScope(false);
	var text2 = g.GenerateName(name2);
	g.PopScope(false);

	Equal("_a", text1);
	Equal("_b", text2);
}

private static void TestLoopVariable1() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewLoopVariable();
	var name2 = ec.Factory.NewLoopVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("_i", text1);
	Equal("_a", text2);
}

private static void TestLoopVariable2() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewLoopVariableEx(new AutoGenerateOptions {
		Prefix = "A",
		Suffix = "B",
	});
	var name2 = ec.Factory.NewLoopVariableEx(new AutoGenerateOptions {
		Prefix = "A",
		Suffix = "B",
	});

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("A_iB", text1);
	Equal("A_aB", text2);
}

private static void TestLoopVariable3() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewLoopVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name1);

	Equal("_i", text1);
	Equal("_i", text2);
}

private static void TestLoopVariableScoped() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewLoopVariable();
	var name2 = ec.Factory.NewLoopVariable();

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	g.PushScope(false);
	var text2 = g.GenerateName(name2);
	g.PopScope(false);

	Equal("_i", text1);
	Equal("_i", text2);
}

private static void TestUniqueName1() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniqueName("foo");
	var name2 = ec.Factory.NewUniqueName("foo");

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("foo_1", text1);
	Equal("foo_2", text2);
}

private static void TestUniqueName2() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniqueName("foo");

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name1);

	Equal("foo_1", text1);
	// Expected to be same because GenerateName goes off object identity
	Equal("foo_1", text2);
}

private static void TestUniqueNameScoped() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniqueName("foo");
	var name2 = ec.Factory.NewUniqueName("foo");

	var g = new NameGenerator {Context = ec};
	Equal("foo_1", g.GenerateName(name1));

	g.PushScope(false);
	Equal("foo_2", g.GenerateName(name2)); // Matches Strada, but is incorrect
	// Equal("foo_1", g.GenerateName(name2)) // TODO: Fix after Strada port is complete.
	g.PopScope(false);
}

private static void TestUniquePrivateName1() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniquePrivateName("#foo");
	var name2 = ec.Factory.NewUniquePrivateName("#foo");

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("#foo_1", text1);
	Equal("#foo_2", text2);
}

private static void TestUniquePrivateName2() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniquePrivateName("#foo");

	var g = new NameGenerator {Context = ec};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name1);

	Equal("#foo_1", text1);
	Equal("#foo_1", text2);
}

private static void TestUniquePrivateNameScoped() {

	var ec = new EmitContext();
	var name1 = ec.Factory.NewUniquePrivateName("#foo");
	var name2 = ec.Factory.NewUniquePrivateName("#foo");

	var g = new NameGenerator {Context = ec};
	Equal("#foo_1", g.GenerateName(name1));

	g.PushScope(false); // private names are always reserved in nested scopes
	Equal("#foo_2", g.GenerateName(name2));
	g.PopScope(false);
}

private static void TestGeneratedNameForIdentifier1() {

	var ec = new EmitContext();

	var file = ParseTypeScript("function f() {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Name();
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("f_1", text1);
}

private static void TestGeneratedNameForIdentifier2() {

	var ec = new EmitContext();

	var file = ParseTypeScript("function f() {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Name();
	var name1 = ec.Factory.NewGeneratedNameForNodeEx(n, new AutoGenerateOptions {
		Prefix = "a",
		Suffix = "b",
	});

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("afb", text1);
}

private static void TestGeneratedNameForIdentifier3() {

	var ec = new EmitContext();

	var file = ParseTypeScript("function f() {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Name();
	var name1 = ec.Factory.NewGeneratedNameForNodeEx(n, new AutoGenerateOptions {
		Prefix = "a",
		Suffix = "b",
	});
	var name2 = ec.Factory.NewGeneratedNameForNode(name1);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name2);

	Equal("afb_1", text1);
}

// namespace reuses name if it does not collide with locals
private static void TestGeneratedNameForNamespace1() {

	var ec = new EmitContext();

	var file = ParseTypeScript("namespace foo { }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var ns1 = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(ns1);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("foo", text1);
}

// namespace uses generated name if it collides with locals
private static void TestGeneratedNameForNamespace2() {

	var ec = new EmitContext();

	var file = ParseTypeScript("namespace foo { var foo; }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var ns1 = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(ns1);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("foo_1", text1);
}

// avoids collisions when unscoped
private static void TestGeneratedNameForNamespace3() {

	var ec = new EmitContext();

	var file = ParseTypeScript("namespace ns1 { namespace foo { var foo; } } namespace ns2 { namespace foo { var foo; } }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var ns1 = file.Statements.Nodes[0].Body().Statements()[0];
	var ns2 = file.Statements.Nodes[1].Body().Statements()[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(ns1);
	var name2 = ec.Factory.NewGeneratedNameForNode(ns2);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("foo_1", text1);
	Equal("foo_2", text2);
}

// reuse name when scoped
private static void TestGeneratedNameForNamespace4() {

	var ec = new EmitContext();

	var file = ParseTypeScript("namespace ns1 { namespace foo { var foo; } } namespace ns2 { namespace foo { var foo; } }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var ns1 = file.Statements.Nodes[0].Body().Statements()[0];
	var ns2 = file.Statements.Nodes[1].Body().Statements()[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(ns1);
	var name2 = ec.Factory.NewGeneratedNameForNode(ns2);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	g.PushScope(false);
	var text1 = g.GenerateName(name1);
	g.PopScope(false);

	g.PushScope(false);
	var text2 = g.GenerateName(name2);
	g.PopScope(false);

	Equal("foo_1", text1);
	Equal("foo_2", text2); // Matches Strada, but is incorrect
	// Equal("foo_1", text2) // TODO: Fix after Strada port is complete.
}

private static void TestGeneratedNameForNodeCached() {

	var ec = new EmitContext();

	var file = ParseTypeScript("namespace foo { var foo; }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var ns1 = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(ns1);
	var name2 = ec.Factory.NewGeneratedNameForNode(ns1);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);
	var text2 = g.GenerateName(name2);

	Equal("foo_1", text1);
	Equal("foo_1", text2);
}

private static void TestGeneratedNameForImport() {

	var ec = new EmitContext();

	var file = ParseTypeScript("import * as foo from 'foo'", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("foo_1", text1);
}

private static void TestGeneratedNameForExport() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export * as foo from 'foo'", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("foo_1", text1);
}

private static void TestGeneratedNameForFunctionDeclaration1() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export function f() {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("f_1", text1);
}

private static void TestGeneratedNameForFunctionDeclaration2() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export default function () {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("default_1", text1);
}

private static void TestGeneratedNameForClassDeclaration1() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export class C {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("C_1", text1);
}

private static void TestGeneratedNameForClassDeclaration2() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export default class {}", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("default_1", text1);
}

private static void TestGeneratedNameForExportAssignment() {

	var ec = new EmitContext();

	var file = ParseTypeScript("export default 0", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("default_1", text1);
}

private static void TestGeneratedNameForClassExpression() {

	var ec = new EmitContext();

	var file = ParseTypeScript("(class {})", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Expression().Expression();
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("class_1", text1);
}

private static void TestGeneratedNameForMethod1() {

	var ec = new EmitContext();

	var file = ParseTypeScript("class C { m() {} }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Members()[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("m_1", text1);
}

private static void TestGeneratedNameForMethod2() {

	var ec = new EmitContext();

	var file = ParseTypeScript("class C { 0() {} }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Members()[0];
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("_a", text1);
}

private static void TestGeneratedPrivateNameForMethod() {

	var ec = new EmitContext();

	var file = ParseTypeScript("class C { m() {} }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Members()[0];
	var name1 = ec.Factory.NewGeneratedPrivateNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("#m_1", text1);
}

private static void TestGeneratedNameForComputedPropertyName() {

	var ec = new EmitContext();

	var file = ParseTypeScript("class C { [x] }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = file.Statements.Nodes[0].Members()[0].Name();
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("_a", text1);
}

private static void TestGeneratedNameForOther() {

	var ec = new EmitContext();

	var file = ParseTypeScript("class C { [x] }", false /*jsx*/);
	Binder.BindSourceFile(file);

	var n = ec.Factory.NewObjectLiteralExpression(
		ec.Factory.NewNodeList(Slice<Node>.Make(0)),
		false /*multiLine*/
	);
	var name1 = ec.Factory.NewGeneratedNameForNode(n);

	var g = new NameGenerator {Context = ec, GetTextOfNode = node => node.Text()};
	var text1 = g.GenerateName(name1);

	Equal("_a", text1);
}
}
