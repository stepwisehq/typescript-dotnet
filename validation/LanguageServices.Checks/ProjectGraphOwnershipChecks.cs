// Authored controls for projectcollectionbuilder.go's concrete diagnostic change predicate.
// Original Session/cache integration assertions remain separate and require Session.
using TypeScript.ProjectSystem;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class ProjectGraphOwnershipChecks
{
    public static int RunAuthoredControls()
    {
        Diagnostic Make(string message, int end = 2) => Ast.NewExternalDiagnostic(null!, new(1, end), "api", DiagnosticCategory.Error, 7006, message);
        var first = Make("parameter needs a type");
        var equal = Make("parameter needs a type");
        Check(ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([equal])), "equivalent reconstructed nil-file API diagnostic");
        Check(!ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([Make("changed message")])), "full message change");
        Check(!ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([Make("parameter needs a type", 3)])), "location change");
        Check(!ProjectDiagnosticEquality.Equal(default, Slice<Diagnostic>.Make(0)), "nil versus allocated-empty errors");
        first.relatedInformation = Slice<Diagnostic>.FromArray([Make("related")]);
        equal.relatedInformation = Slice<Diagnostic>.FromArray([Make("related")]);
        Check(ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([equal])), "full related information comparison");
        equal.relatedInformation[0].messageText = "changed related message";
        Check(!ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([equal])), "related message change");
        Check(ProjectDiagnosticEquality.Equal(Slice<Diagnostic>.FromArray([first]), Slice<Diagnostic>.FromArray([first])), "same retained diagnostic owner");
        return 1;
    }

    private static void Check(bool condition, string reason)
    {
        if (!condition)
            throw new InvalidOperationException(reason);
    }
}
