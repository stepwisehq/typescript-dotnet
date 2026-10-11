// Independently authored public-boundary assertions; Go accepts broader syntax.
using TypeScript.Scanner;

namespace TypeScript.Checking.Validation;
public static class SelfTests
{
    public static List<UtilityObservation> Run()
    {
        var result = new List<UtilityObservation>();
        void Check(string name, bool success)
        {
            if (!success)
                throw new InvalidOperationException("Checker public API regression: " + name);
            result.Add(new() { Name = name, BoolValue = success });
        }

        SourceFile Parse(string text) => Parser.ParseSourceFile(new("/api.ts", "/api.ts"), text, ScriptKind.TS);
        Node Name(SourceFile file) => file.Statements.Nodes[0].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0].AsVariableDeclaration().Name();
        var file = Parse("const x=1;const y=1;");
        var other = Parse("const x=1;");
        var checker = Checker.Create(file);
        checker.GetDiagnostics();
        bool Rejected(Action action, string feature)
        {
            try
            {
                action();
                return false;
            }
            catch (UnsupportedSemanticFeatureException error)
            {
                return error.Feature == feature;
            }
        }

        Check("api.foreign-type-node", Rejected(() => checker.GetTypeAtLocation(Name(other)), "foreignNode"));
        Check("api.foreign-symbol-node", Rejected(() => checker.GetSymbolAtLocation(Name(other)), "foreignNode"));
        var orphan = new NodeFactory().NewIdentifier("x");
        Check("api.orphan-node", Rejected(() => checker.GetTypeAtLocation(orphan), "foreignNode"));
        orphan.Parent = orphan;
        Check("api.cyclic-foreign-parent", Rejected(() => checker.GetSymbolAtLocation(orphan), "foreignNode"));
        Check("api.global-table-alias", ReferenceEquals(checker.Globals, checker.GlobalThisSymbol.Exports));
        Check("api.global-readonly", (checker.GlobalThisSymbol.CheckFlags & CheckFlags.Readonly) != 0);
        var globals = checker.GetGlobalDiagnostics();
        Check("api.no-library-errors-visible", globals.Count == 8 && GoSlices.Values(globals).All(d => d.Code() == 2318));
        Check("api.repeat-diagnostics", checker.GetDiagnostics().IsNil && checker.GetDiagnostics().IsNil);
        Check("api.repeat-type-identity", ReferenceEquals(checker.GetTypeAtLocation(Name(file)), checker.GetTypeAtLocation(Name(file))));
        var objectFile = Parse("const x=globalThis;");
        var objectChecker = Checker.Create(objectFile);
        Check("api.builtin-object-read-admitted", objectChecker.GetDiagnostics().IsNil);
        var objectGlobals = objectChecker.GetGlobalDiagnostics();
        Check("api.builtin-object-missing-globals-visible", objectGlobals.Count == 8 && GoSlices.Values(objectGlobals).All(d => d.Code() == 2318));
        var objectDeclaration = objectFile.Statements.Nodes[0].AsVariableStatement().DeclarationList
            .AsVariableDeclarationList().Declarations.Nodes[0].AsVariableDeclaration();
        Check("api.builtin-object-symbol", ReferenceEquals(objectChecker.GetSymbolAtLocation(objectDeclaration.Initializer), objectChecker.GlobalThisSymbol));
        var objectType = objectChecker.GetTypeAtLocation(objectDeclaration.Initializer);
        Check("api.builtin-object-type", ReferenceEquals(objectType, objectChecker.GetTypeAtLocation(objectDeclaration.Name())));
        Check("api.builtin-object-type-identity", ReferenceEquals(objectType, objectChecker.GetTypeAtLocation(objectDeclaration.Initializer)));
        return result;
    }
}
