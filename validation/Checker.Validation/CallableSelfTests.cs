// Authored profile/API assertions; semantic results are independently compared to Go.
using TypeScript.Scanner;

namespace TypeScript.Checking.Validation;
public static class CallableSelfTests
{
    public static List<UtilityObservation> Run()
    {
        List<UtilityObservation> result = [];
        void Check(string name, bool value)
        {
            if (!value)
                throw new InvalidOperationException("Callable API control failed: " + name);
            result.Add(new() { Name = name, BoolValue = true });
        }

        static SourceFile Parse(string text) => Parser.ParseSourceFile(new("/api.ts", "/api.ts"), text, ScriptKind.TS);
        var file = Parse("function f(x:number):number{return x;}const value=f(1);");
        try
        {
            Checker.Create(file);
            throw new InvalidOperationException("Legacy silently admitted function syntax");
        }
        catch (UnsupportedSemanticFeatureException error)
        {
            Check("callable.default-legacy-boundary", error.Feature == "statement");
        }

        try
        {
            Checker.Create(file, (CheckerProfile)int.MaxValue);
            throw new InvalidOperationException("Invalid profile silently admitted");
        }
        catch (ArgumentOutOfRangeException)
        {
            Check("callable.invalid-profile-boundary", true);
        }

        var checker = Checker.Create(file, CheckerProfile.Callable);
        Check("callable.diagnostics-repeated", checker.GetDiagnostics().IsNil && checker.GetDiagnostics().IsNil);
        var name = file.Statements.Nodes[0].Name();
        Check("callable.type-query-identity", ReferenceEquals(checker.GetTypeAtLocation(name), checker.GetTypeAtLocation(name)));
        Check("callable.symbol-query-identity", ReferenceEquals(checker.GetSymbolAtLocation(name), checker.GetSymbolAtLocation(name)));
        var foreign = Parse("function f(x:number){return x;}").Statements.Nodes[0].Name();
        foreach (var action in new Action[]
        {
            () => checker.GetTypeAtLocation(foreign),
            () => checker.GetSymbolAtLocation(foreign)
        }

        )
        {
            try
            {
                action();
                throw new InvalidOperationException("Foreign callable node silently admitted");
            }
            catch (UnsupportedSemanticFeatureException error)
            {
                Check("callable.foreign-query-" + result.Count, error.Feature == "foreignNode");
            }
        }

        Check("callable.global-table-identity", ReferenceEquals(checker.Globals, checker.GlobalThisSymbol.Exports));
        const string unionSource = "declare let f:((left:number)=>string)|((right:string)=>number);const value=f(1);";
        (Checker Checker, Symbol Parameter) UnionParameter()
        {
            var unionFile = Parse(unionSource);
            var unionChecker = Checker.Create(unionFile, CheckerProfile.Callable);
            unionChecker.GetDiagnostics();
            var call = unionFile.Statements.Nodes[1].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0].Initializer();
            return (unionChecker, unionChecker.GetResolvedSignature(call).Parameters()[0]);
        }

        var ownUnion = UnionParameter();
        Check("callable.owned-synthetic-parameter", ownUnion.Parameter.Declarations.IsNil && (ownUnion.Checker.GetTypeOfSymbol(ownUnion.Parameter).Flags() & TypeFlags.Never) != 0);
        var foreignUnion = UnionParameter();
        try
        {
            ownUnion.Checker.GetTypeOfSymbol(foreignUnion.Parameter);
            throw new InvalidOperationException("Foreign synthetic symbol silently admitted");
        }
        catch (ArgumentException)
        {
            Check("callable.foreign-synthetic-parameter", true);
        }

        return result;
    }
}
