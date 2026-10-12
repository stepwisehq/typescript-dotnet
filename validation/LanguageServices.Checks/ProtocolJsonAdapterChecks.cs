// Authored controls for source-grounded JSON ownership and platform adaptations.
using System.Text;
using TypeScript.Json;
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class ProtocolJsonAdapterChecks
{
    public static readonly string[] Names = ["BorrowedQuote", "StrictDecode", "OwnedDecodedString", "OwnedDeferredValue", "DictionaryPartialOwner", "DeterministicMapOrder"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Protocol JSON adaptation assertion failed");
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "BorrowedQuote":
                var builder = new GoBuilder();
                JsonCodec.QuoteToBuilder(builder, ByteStringValue.FromView(ByteStringView.FromOwnedBytes([0xe2, 0x80, 0xa8, 0x80, (byte)'\n', (byte)'"'])));
                Assert(builder.Value().ByteSpan.SequenceEqual(new byte[] { (byte)'"', 0xe2, 0x80, 0xa8, 0xef, 0xbf, 0xbd, (byte)'\\', (byte)'n', (byte)'\\', (byte)'"', (byte)'"' }));
                break;
            case "StrictDecode":
                Assert(JsonCodec.ParseDictionary("{\"x\":1,\"x\":2}").Error is not null);
                Assert(JsonCodec.ParseDictionary("[]").Error?.Message == "json: cannot unmarshal JSON array into Go map[string]interface {}");
                Assert(JsonCodec.ParseDictionary("null") is { Value: null, Error: null });
                Assert(JsonCodec.ParseDictionary("{\"x\":\"\u0080\"}").Error is not null);
                Assert(JsonCodec.ParseDictionary("{\"x\":\"\\ud800\"}").Error is not null);
                break;
            case "OwnedDecodedString":
                var stringBytes = Encoding.UTF8.GetBytes("\"retained\"");
                var decoded = RpcJson.ReadString(stringBytes);
                stringBytes.AsSpan().Fill((byte)' ');
                Assert(decoded.EqualsBytes("retained"u8));
                var escaped = RpcJson.ReadString("\"caf\\u00e9\""u8);
                Assert(escaped.EqualsBytes("café"u8));
                break;
            case "OwnedDeferredValue":
                var valueBytes = Encoding.UTF8.GetBytes("{\"nested\":[1,true]}");
                var retained = new RawJsonValue(valueBytes);
                valueBytes.AsSpan().Fill((byte)' ');
                Assert(retained.Bytes.SequenceEqual("{\"nested\":[1,true]}"u8));
                break;
            case "DictionaryPartialOwner":
                var parsed = JsonCodec.ParseDictionary("{\"items\":[{\"value\":1}],\"tail\":");
                Assert(parsed.Error is not null);
                var root = (Dictionary<string, object?>)parsed.Value!;
                var items = (List<object?>)root["items"]!;
                Assert(items[0] is Dictionary<string, object?> child && (double)child["value"]! == 1);
                break;
            case "DeterministicMapOrder":
                var map = new Dictionary<string, object?>
                {
                    ["z"] = 1,
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["y"] = true,
                        ["b"] = false
                    }
                };
                Assert(JsonCodec.MarshalStringDeterministic(map) == "{\"a\":{\"b\":false,\"y\":true},\"z\":1}");
                Assert(JsonCodec.MarshalString(map) == "{\"z\":1,\"a\":{\"y\":true,\"b\":false}}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}
