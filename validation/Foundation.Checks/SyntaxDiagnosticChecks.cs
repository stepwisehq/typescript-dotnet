using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class SyntaxServiceChecks
{
    private static object DiagnosticSnapshot(Diagnostic d) => Map(("code", d.Code()), ("category", (int)d.Category()), ("pos", d.Pos()), ("end", d.End()), ("key", d.MessageKey()), ("source", Bytes(d.Source())), ("messageText", Bytes(d.MessageText())), ("text", d.String()), ("args", (d.MessageArgs() ?? []).Select(a => (object)Bytes(a)).ToArray()), ("argsNull", d.MessageArgs()is null), ("chain", GoSlices.Values(d.MessageChain()).Select(DiagnosticSnapshot).ToArray()), ("chainNull", d.MessageChain().IsNil), ("related", GoSlices.Values(d.RelatedInformation()).Select(DiagnosticSnapshot).ToArray()), ("relatedNull", d.RelatedInformation().IsNil), ("unnecessary", d.ReportsUnnecessary()), ("deprecated", d.ReportsDeprecated()), ("skipped", d.SkippedOnNoEmit()));
    private static object DiagnosticData()
    {
        var file = Parse("const x=1");
        var chain = Ast.NewDiagnosticFromText(null, new(0, 0), 999, DiagnosticCategory.Error, "chain é", null, null, false, false);
        var related = Ast.NewDiagnosticFromText(file, new(1, 2), 998, DiagnosticCategory.Warning, "related", null, null, false, false);
        var d = Ast.NewDiagnosticFromSerialized(file, new(3, 7), 2575, DiagnosticCategory.Error, "No_overload_expects_0_arguments_but_overloads_do_exist_that_expect_either_1_or_2_arguments_2575", ["7", "2", "3"], [chain], [related], true, true, true);
        var clone = d.Clone();
        var first = DiagnosticSnapshot(d);
        d.SetExternalData("mapped", Raw("localized é"));
        var second = DiagnosticSnapshot(d);
        var unknown = Ast.NewDiagnosticFromSerialized(null, new(0, 0), 123, DiagnosticCategory.Error, "nonexistent-key", null, null, null, false, false, false);
        var collection = new DiagnosticsCollection();
        bool emptyGlobal = collection.GetGlobalDiagnostics().IsNil, emptyAll = collection.GetDiagnostics().IsNil;
        var canonical = collection.Add(clone);
        var duplicate = collection.Add(clone.Clone());
        return Map(("serialized", first), ("externalOverride", second), ("unknownKeyThrows", Caught(() => unknown.String())), ("cloneDistinct", !ReferenceEquals(clone, d)), ("cloneSharesArgs", ReferenceEquals(clone.MessageArgs(), d.MessageArgs())), ("cloneSharesChain", Core.Same(clone.MessageChain(), d.MessageChain())), ("canonicalIdentity", ReferenceEquals(canonical, clone)), ("duplicateIdentity", ReferenceEquals(duplicate, clone)), ("globalInitiallyNil", emptyGlobal), ("allInitiallyNil", emptyAll), ("equivalentFileCount", collection.GetDiagnosticsForFile(Parse("")).Count));
    }
}
