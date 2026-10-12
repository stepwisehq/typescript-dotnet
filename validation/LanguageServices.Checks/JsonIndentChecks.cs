// Authored public JSON observations, captured from pinned Go MarshalIndent.
// These are not additional original Go test functions.
using TypeScript.Json;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class JsonIndentChecks
{
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(object? value, string prefix, string indent, string expected, string error = "")
    {
        var result = JsonCodec.MarshalIndentValue(value, Text(prefix), Text(indent));
        if (!result.Output.EqualsValue(Text(expected)))
            throw new InvalidOperationException("MarshalIndent output bytes differ from pinned Go.");
        if ((result.Error?.Message ?? "") != error)
            throw new InvalidOperationException("MarshalIndent error differs from pinned Go: " + result.Error?.Message);
    }

    private sealed class AnonymousStruct : IProtocolWritable
    {
        // The reference uses struct{ Good int; Bad float64 }, whose fields are
        // closed here explicitly instead of reflection-based CLR serialization.
        public void WriteProtocolJSON(ProtocolEncoder encoder)
        {
            encoder.BeginObject();
            encoder.WriteProperty("Good"u8);
            encoder.WriteNumber(1);
            encoder.WriteProperty("Bad"u8);
            encoder.WriteNumber(double.NaN);
            encoder.EndObject();
        }
    }

    public static int RunAll()
    {
        Check(Text("café\n"), "", "", "\"café\\n\"");
        Check(new List<object?> { new List<object?> { 1.0, Text("café") }, new List<object?>(), null }, " \t", "\t", "[\n \t\t[\n \t\t\t1,\n \t\t\t\"café\"\n \t\t],\n \t\t[],\n \t\tnull\n \t]");
        Check(new ProtocolPayload<Position>(ProtocolCodec.GetType_6d031af10da7a25e(), new Position { Line = 1, Character = 2 }), "", "\t", "{\n\t\"line\": 1,\n\t\"character\": 2\n}");
        Check(new JsonValue(System.Text.Encoding.UTF8.GetBytes("{\"a\":[1,{},[\"café\"]]}")), " ", "  ", "{\n   \"a\": [\n     1,\n     {},\n     [\n       \"café\"\n     ]\n   ]\n }");
        Check(new List<object?> { 1.0, double.NaN }, "", "\t", "[\n\t1", "json: cannot marshal from Go float64 within \"/1\": unsupported value: NaN");
        Check(new List<object?> { new double[] { 1, double.NaN } }, "", "\t", "[\n\t[\n\t\t1", "json: cannot marshal from Go float64 within \"/0/1\": unsupported value: NaN");
        Check(new AnonymousStruct(), "", "\t", "{\n\t\"Good\": 1,\n\t\"Bad\"", "json: cannot marshal from Go float64 within \"/Bad\": unsupported value: NaN");
        Check(new int[] { 1, 2 }, " \t", "", "[\n \t1,\n \t2\n \t]");
        CheckPanic(default, Text("x"), "json: invalid character 'x' in indent");
        CheckPanic(ByteStringValue.FromView(ByteStringView.FromOwnedBytes([0xff])), Text(" "), "json: invalid character '\\xff' in indent prefix");
        return 10;
    }

    private static void CheckPanic(ByteStringValue prefix, ByteStringValue indent, string expected)
    {
        try
        {
            _ = JsonCodec.MarshalIndentValue(null, prefix, indent);
        }
        catch (InvalidOperationException error)
        {
            if (error.Message == expected)
                return;
            throw;
        }

        throw new InvalidOperationException("Invalid JSON indentation did not panic.");
    }
}
