// Authored differential observations from pinned Go context-emit-go-2.log.
// Four actual Session/diagnostics/emit rows; no original-function credit.
using System.Text.Json;
namespace LanguageServices.Checks;
public static partial class SessionDiagnosticsEmitChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Valid-NoEmitOnError-false", "Invalid-NoEmitOnError-false", "Valid-NoEmitOnError-true", "Invalid-NoEmitOnError-true"];
    public static int RunAll() { foreach (var name in Cases) Run(name); return Cases.Count; }
    public static void Run(string name)
    {
        int index = -1;
        for (int i = 0; i < Cases.Count; i++) if (Cases[i] == name) { index = i; break; }
        if (index < 0) throw new ArgumentException("Unknown context emit case", nameof(name));
        using var document = JsonDocument.Parse(Observations[index]);
        var row = document.RootElement;
        string source = row.GetProperty("Name").GetString() == "Valid" ? "export const value: number = 1;\n" : "export function echo(value) { return value; }\nexport const value: number = \"bad\";\n";
        var expected = row.GetProperty("Outputs").EnumerateArray().Select(value => new ExpectedOutput(value.GetProperty("Name").GetString()!, value.GetProperty("Text").GetString()!)).ToArray();
        Require(row.GetProperty("Before").GetString() == row.GetProperty("After").GetString(), "reference capture contains a diagnostic flake");
        RunCase(row.GetProperty("Name").GetString()!, source, row.GetProperty("NoEmitOnError").GetBoolean(), row.GetProperty("Before").GetString()!, row.GetProperty("Diagnostics").GetRawText(), row.GetProperty("EmitSkipped").GetBoolean(), expected);
    }
    private static readonly string[] Observations = [
        """
        {"Name":"Valid","NoEmitOnError":false,"Before":"{\"kind\":\"full\",\"items\":[]}","After":"{\"kind\":\"full\",\"items\":[]}","EmitSkipped":false,"Diagnostics":[],"Outputs":[{"Name":"/fixture/input.d.ts","Text":"export declare const value: number;\n"},{"Name":"/fixture/input.js","Text":"export const value = 1;\n"}]}
        """,
        """
        {"Name":"Invalid","NoEmitOnError":false,"Before":"{\"kind\":\"full\",\"items\":[{\"range\":{\"start\":{\"line\":0,\"character\":21},\"end\":{\"line\":0,\"character\":26}},\"severity\":1,\"code\":7006,\"source\":\"ts\",\"message\":\"Parameter 'value' implicitly has an 'any' type.\"},{\"range\":{\"start\":{\"line\":1,\"character\":13},\"end\":{\"line\":1,\"character\":18}},\"severity\":1,\"code\":2322,\"source\":\"ts\",\"message\":\"Type 'string' is not assignable to type 'number'.\"}]}","After":"{\"kind\":\"full\",\"items\":[{\"range\":{\"start\":{\"line\":0,\"character\":21},\"end\":{\"line\":0,\"character\":26}},\"severity\":1,\"code\":7006,\"source\":\"ts\",\"message\":\"Parameter 'value' implicitly has an 'any' type.\"},{\"range\":{\"start\":{\"line\":1,\"character\":13},\"end\":{\"line\":1,\"character\":18}},\"severity\":1,\"code\":2322,\"source\":\"ts\",\"message\":\"Type 'string' is not assignable to type 'number'.\"}]}","EmitSkipped":false,"Diagnostics":[],"Outputs":[{"Name":"/fixture/input.d.ts","Text":"export declare function echo(value: any): any;\nexport declare const value: number;\n"},{"Name":"/fixture/input.js","Text":"export function echo(value) { return value; }\nexport const value = \"bad\";\n"}]}
        """,
        """
        {"Name":"Valid","NoEmitOnError":true,"Before":"{\"kind\":\"full\",\"items\":[]}","After":"{\"kind\":\"full\",\"items\":[]}","EmitSkipped":false,"Diagnostics":[],"Outputs":[{"Name":"/fixture/input.d.ts","Text":"export declare const value: number;\n"},{"Name":"/fixture/input.js","Text":"export const value = 1;\n"}]}
        """,
        """
        {"Name":"Invalid","NoEmitOnError":true,"Before":"{\"kind\":\"full\",\"items\":[{\"range\":{\"start\":{\"line\":0,\"character\":21},\"end\":{\"line\":0,\"character\":26}},\"severity\":1,\"code\":7006,\"source\":\"ts\",\"message\":\"Parameter 'value' implicitly has an 'any' type.\"},{\"range\":{\"start\":{\"line\":1,\"character\":13},\"end\":{\"line\":1,\"character\":18}},\"severity\":1,\"code\":2322,\"source\":\"ts\",\"message\":\"Type 'string' is not assignable to type 'number'.\"}]}","After":"{\"kind\":\"full\",\"items\":[{\"range\":{\"start\":{\"line\":0,\"character\":21},\"end\":{\"line\":0,\"character\":26}},\"severity\":1,\"code\":7006,\"source\":\"ts\",\"message\":\"Parameter 'value' implicitly has an 'any' type.\"},{\"range\":{\"start\":{\"line\":1,\"character\":13},\"end\":{\"line\":1,\"character\":18}},\"severity\":1,\"code\":2322,\"source\":\"ts\",\"message\":\"Type 'string' is not assignable to type 'number'.\"}]}","EmitSkipped":true,"Diagnostics":[{"File":"/fixture/input.ts","Start":21,"Length":5,"End":26,"Code":7006,"Category":1,"Key":"Parameter_0_implicitly_has_an_1_type_7006","Message":"Parameter 'value' implicitly has an 'any' type.","Args":["value","any"],"Source":"","ReportsUnnecessary":false,"ReportsDeprecated":false,"SkippedOnNoEmit":false,"Chain":[],"Related":[],"Repopulate":null},{"File":"/fixture/input.ts","Start":59,"Length":5,"End":64,"Code":2322,"Category":1,"Key":"Type_0_is_not_assignable_to_type_1_2322","Message":"Type 'string' is not assignable to type 'number'.","Args":["string","number"],"Source":"","ReportsUnnecessary":false,"ReportsDeprecated":false,"SkippedOnNoEmit":false,"Chain":[],"Related":[],"Repopulate":null}],"Outputs":[]}
        """,
    ];
}
