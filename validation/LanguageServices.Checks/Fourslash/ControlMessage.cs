// Authored candidate-host bridge. Original fourslash assertions remain in Go.
// This channel is independent of the original Content-Length LSP transport.
using System.Text.Json.Nodes;

namespace LanguageServices.Checks.Fourslash;
internal sealed class ControlMessage
{
    public long Id { get; init; }
    public string? Operation { get; init; }
    public JsonObject? Arguments { get; init; }
    public JsonNode? Result { get; init; }
    public string? ErrorBase64 { get; init; }
    public bool IsResponse { get; init; }

    public JsonObject ToJson() => new()
    {
        ["id"] = JsonValue.Create(Id),
        ["operation"] = JsonValue.Create(Operation),
        ["arguments"] = Arguments?.DeepClone(),
        ["result"] = Result?.DeepClone(),
        ["errorBase64"] = JsonValue.Create(ErrorBase64),
        ["response"] = JsonValue.Create(IsResponse)
    };
    public static ControlMessage FromJson(JsonObject value) => new()
    {
        Id = value["id"]!.GetValue<long>(),
        Operation = value["operation"]?.GetValue<string>(),
        Arguments = value["arguments"] as JsonObject,
        Result = value["result"],
        ErrorBase64 = value["errorBase64"]?.GetValue<string>(),
        IsResponse = value["response"]!.GetValue<bool>()
    };
}
