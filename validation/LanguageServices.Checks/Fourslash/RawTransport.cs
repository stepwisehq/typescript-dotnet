// Raw Go strings are carried as base64, preserving malformed UTF8 and byte paths.
using System.Text.Json.Nodes;
using TypeScript.Scanner;

namespace LanguageServices.Checks.Fourslash;
internal static class RawTransport
{
    internal static string Encode(ByteStringValue value)
    {
        if (value.IsBytes)
            return Convert.ToBase64String(value.ByteSpan);
        // String-backed Go strings contain one character per byte, not CLR UTF8 text.
        byte[] bytes = new byte[value.Length];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = checked((byte)value[i]);
        return Convert.ToBase64String(bytes);
    }

    internal static string Encode(string rawGo) => Encode(ByteStringValue.FromString(rawGo));
    internal static ByteStringView DecodeView(JsonNode value) => ByteStringView.FromOwnedBytes(Convert.FromBase64String(value.GetValue<string>()));
    internal static ByteStringValue DecodeValue(JsonNode value) => ByteStringValue.FromView(DecodeView(value));
    internal static string Decode(JsonNode value) => DecodeValue(value).Materialize()!;
    internal static JsonObject Path(ByteStringValue value) => new()
    {
        ["path"] = JsonValue.Create(Encode(value))
    };
}
