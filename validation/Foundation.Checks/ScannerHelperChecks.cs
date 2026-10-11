// Authored additional differential assertions; expected values are unchanged pinned Go outputs.
// This supplements, and is counted separately from, original upstream assertions.
using System.Globalization;
using System.Text.Json;
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;
public static partial class ScannerChecks
{
    private sealed class ByteSource(string text, Slice<int> lines) : SourceFileLike
    {
        public string Text() => text;
        public Slice<int> ECMALineMap() => lines;
    }

    private static readonly Lazy<JsonDocument> helperFixtures = new(() => LoadFixture("TypeScript.Foundation.ScannerHelperFixtures.json"));
    private static readonly Lazy<JsonDocument> numberPatterns = new(() => LoadFixture("TypeScript.Foundation.ScannerNumberPatterns.json"));
    private static JsonDocument LoadFixture(string name)
    {
        using Stream source = typeof(ScannerChecks).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing generated foundation fixture: " + name);
        return JsonDocument.Parse(source);
    }

    private static void AddHelperChecks(Dictionary<string, Action> cases)
    {
        cases.Add("AdditionalHelper/filterCallbackSliceLength", CheckFilterCallbacks);
        cases.Add("AdditionalHelper/coordinates", CheckCoordinateHelpers);
        cases.Add("AdditionalHelper/identifiers", CheckIdentifierHelpers);
        cases.Add("AdditionalHelper/tokenStringBounds", () =>
        {
            foreach (var row in helperFixtures.Value.RootElement.GetProperty("TokenStrings").EnumerateArray())
            {
                var kind = (Kind)row.GetProperty("Kind").GetInt32();
                if (row.GetProperty("Panics").GetBoolean())
                {
                    bool threw = false;
                    try
                    {
                        ScannerApi.TokenToString(kind);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        threw = true;
                    }

                    Equal(true, threw, "Go token-array bounds failure " + kind);
                }
                else
                    Equal(row.GetProperty("Text").GetString(), ScannerApi.TokenToString(kind), "Token text " + kind);
            }
        });
        cases.Add("AdditionalHelper/JSDocComment", CheckJSDocCommentHelper);
        cases.Add("AdditionalHelper/keywordSuggestions", () =>
        {
            string[] expected = helperFixtures.Value.RootElement.GetProperty("Keywords").EnumerateArray().Select(row => row.GetString()!).ToArray();
            string[] actual = ScannerApi.GetViableKeywordSuggestions().Order(StringComparer.Ordinal).ToArray();
            Equal(string.Join("\n", expected), string.Join("\n", actual), "Viable keyword membership (Go map order unspecified)");
        });
        cases.Add("AdditionalHelper/commentFactoryAndRanges", CheckCommentFactory);
        cases.Add("AdditionalHelper/unterminatedBackslashAnd1513Boundary", () =>
        {
            var scanner = new ScannerApi();
            var codes = new List<int>();
            scanner.SetOnError((message, _, _, _) => codes.Add(message.Code()));
            scanner.SetText("/\\");
            Equal(Kind.SlashToken, scanner.Scan(), "Slash scan");
            Equal(Kind.RegularExpressionLiteral, scanner.ReScanSlashToken(true), "Unterminated regex recovery kind");
            Equal(true, (scanner.TokenFlags() & TokenFlags.Unterminated) != 0, "Unterminated flag");
            Equal("1161", string.Join(",", codes), "Only unterminated-literal error; terminated-body parser never sees a trailing escape");
        });
        foreach (string family in new[]
        {
            "binary-powers-neighbors",
            "presentation-boundaries",
            "deterministic-bits"
        }

        )
        {
            string selectedFamily = family;
            cases.Add("AdditionalNumeric/" + family, () => CheckNumberPatterns(selectedFamily));
        }
    }

    private static void CheckFilterCallbacks()
    {
        foreach (var row in helperFixtures.Value.RootElement.GetProperty("Filters").EnumerateArray())
        {
            string name = row.GetProperty("Name").GetString()!;
            List<int>? input = name == "nil" ? null : name == "empty" ? [] : new List<int>(8)
            {
                1,
                2,
                3
            };
            var visited = new List<int>();
            List<int>? result = Core.Filter(input, value =>
            {
                visited.Add(value);
                if (name is "append-all" or "append-first-failure")
                    input!.Add(9);
                return !(name is "first-failure" or "append-first-failure" && value == 2);
            });
            string Expected(string key) => row.GetProperty(key).ValueKind == JsonValueKind.Null ? "" : string.Join(",", row.GetProperty(key).EnumerateArray().Select(v => v.GetInt32()));
            Equal(Expected("Input"), string.Join(",", input ?? []), "Filter caller slice " + name);
            Equal(Expected("Visited"), string.Join(",", visited), "Filter captured iteration length " + name);
            Equal(Expected("Result"), string.Join(",", result ?? []), "Filter result slice " + name);
            Equal(row.GetProperty("ResultNil").GetBoolean(), result == null, "Filter nil result " + name);
            bool same = (result?.Count ?? 0) == (input?.Count ?? 0) && ((result?.Count ?? 0) == 0 || ReferenceEquals(result, input));
            Equal(row.GetProperty("Same").GetBoolean(), same, "Filter result/current caller slice identity " + name);
        }
    }

    private static void CheckCoordinateHelpers()
    {
        foreach (var row in helperFixtures.Value.RootElement.GetProperty("Coordinates").EnumerateArray())
        {
            byte[] bytes = Convert.FromBase64String(row.GetProperty("TextBase64").GetString()!);
            string text = GoString.FromBytes(bytes);
            List<int> lines = row.GetProperty("Lines").EnumerateArray().Select(value => value.GetInt32()).ToList();
            var source = new ByteSource(text, Core.ComputeECMALineStarts(text));
            Equal(string.Join(",", lines), string.Join(",", GoSlices.ToListOrNull(source.ECMALineMap())!), "ECMAScript line starts");
            Equal(row.GetProperty("UTF16Length").GetInt32(), Core.UTF16Len(text), "UTF16Len including malformed byte sequences");
            foreach (var forward in row.GetProperty("Forward").EnumerateArray())
            {
                int pos = forward.GetProperty("Pos").GetInt32();
                int line = forward.GetProperty("Line").GetInt32();
                int units = forward.GetProperty("UTF16").GetInt32();
                int offset = forward.GetProperty("ByteOffset").GetInt32();
                Equal((line, units), ScannerApi.GetECMALineAndUTF16CharacterOfPosition(source, pos), "Forward UTF16 at byte " + pos);
                Equal((line, offset), ScannerApi.GetECMALineAndByteOffsetOfPosition(source, pos), "Forward byte offset at byte " + pos);
                Equal((line, offset), Core.PositionToLineAndByteOffset(pos, lines), "Core line/byte lookup");
            }

            foreach (var inverse in row.GetProperty("Inverse").EnumerateArray())
            {
                int line = inverse.GetProperty("Line").GetInt32();
                int offset = inverse.GetProperty("Offset").GetInt32();
                Equal(inverse.GetProperty("UTF16Position").GetInt32(), ScannerApi.GetECMAPositionOfLineAndUTF16Character(source, line, offset), "Inverse UTF16");
                Equal(inverse.GetProperty("BytePosition").GetInt32(), ScannerApi.GetECMAPositionOfLineAndByteOffset(source, line, offset), "Inverse byte offset");
            }
        }

        Equal(new TextRange(-1, -1), Core.UndefinedTextRange(), "Undefined text range");
        Equal(false, Core.UndefinedTextRange().IsValid(), "Undefined range validity");
    }

    private static void CheckIdentifierHelpers()
    {
        var factory = new NodeFactory();
        foreach (var row in helperFixtures.Value.RootElement.GetProperty("Identifiers").EnumerateArray())
        {
            byte[] bytes = Convert.FromBase64String(row.GetProperty("NameBase64").GetString()!);
            string raw = GoString.FromBytes(bytes);
            var variant = (LanguageVariant)row.GetProperty("Variant").GetInt32();
            Equal(row.GetProperty("Valid").GetBoolean(), ScannerApi.IsIdentifierText(bytes, variant), "Identifier text " + row.GetProperty("NameBase64"));
            Equal((Kind)row.GetProperty("Keyword").GetInt32(), ScannerApi.IdentifierToKeywordKind(factory.NewIdentifier(raw).AsIdentifier()), "Identifier keyword");
            Equal(row.GetProperty("Intrinsic").GetBoolean(), ScannerApi.IsIntrinsicJsxName(raw), "Intrinsic JSX name");
        }
    }

    private static void CheckJSDocCommentHelper()
    {
        Equal("", ScannerApi.GetTextOfJSDocComment(null), "Nil JSDoc comment list");
        foreach (var row in helperFixtures.Value.RootElement.GetProperty("JSDoc").EnumerateArray())
        {
            string source = row.GetProperty("Source").GetString()!;
            SourceFile file = Parser.ParseSourceFile(new("/input.ts", "/input.ts"), source, ScriptKind.TS);
            Node doc = file.Statements.Nodes[0].JSDoc(file)[0];
            string actual = Convert.ToBase64String(ScannerApi.GetTextOfJSDocCommentBytes(doc.AsJSDoc().Comment));
            Equal(row.GetProperty("TextBase64").GetString(), actual, "JSDoc text/link source and Unicode whitespace");
        }
    }

    private static void CheckCommentFactory()
    {
        var factory = new NodeFactory();
        var range = factory.NewCommentRange(Kind.MultiLineCommentTrivia, 2, 5, true);
        Equal(new TextRange(2, 5), range.Loc, "CommentRange embedded range");
        Equal(3, range.Len(), "CommentRange length");
        Equal(true, range.Contains(2), "CommentRange contains start");
        Equal(false, range.Contains(5), "CommentRange excludes end");
        const string text = "// α\r\n/*b*/ value";
        Equal(true, ScannerApi.GetLeadingCommentRanges(text, 0).SequenceEqual(ScannerApi.GetLeadingCommentRanges(factory, text, 0)), "Factory-leading iterator matches convenience API");
        Equal(true, ScannerApi.GetTrailingCommentRanges(text, 0).SequenceEqual(ScannerApi.GetTrailingCommentRanges(factory, text, 0)), "Factory-trailing iterator matches convenience API");
    }

    private static void CheckNumberPatterns(string family)
    {
        int samples = 0;
        var selectedRows = numberPatterns.Value.RootElement.EnumerateArray().Where(row => row.GetProperty("Family").GetString() == family).ToArray();
        var nodeInputs = selectedRows.Select(row => new NumberFixtures.Row(ulong.Parse(row.GetProperty("Bits").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture), row.GetProperty("Text").GetString()!)).ToArray();
        JsResult[] nodeResults = EvaluateWithNode(nodeInputs);
        foreach (var row in selectedRows)
        {
            ulong inputBits = ulong.Parse(row.GetProperty("Bits").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            ulong parsedBits = ulong.Parse(row.GetProperty("ParsedBits").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            string expected = row.GetProperty("Text").GetString()!;
            double number = BitConverter.UInt64BitsToDouble(inputBits);
            Equal(expected, new JsNum.Number(number).String(), "Go formatting IEEE " + inputBits.ToString("x16"));
            double actual = JsNum.FromString(expected).Value;
            JsResult node = nodeResults[samples];
            Equal(expected, node.Text, "Go/Node formatting IEEE " + inputBits.ToString("x16"));
            // ECMAScript NaN does not preserve payload/sign; original tests use
            // NaN-aware numerical equality. Every non-NaN parse checks exact bits.
            if (double.IsNaN(BitConverter.UInt64BitsToDouble(parsedBits)))
            {
                Equal(true, double.IsNaN(actual), "Parsed NaN classification");
                Equal(true, double.IsNaN(node.Number), "Node parsed NaN classification");
            }
            else
            {
                Equal(parsedBits, BitConverter.DoubleToUInt64Bits(actual), "Go parsing IEEE " + inputBits.ToString("x16"));
                Equal(parsedBits, BitConverter.DoubleToUInt64Bits(node.Number), "Go/Node parsing IEEE " + inputBits.ToString("x16"));
            }

            samples++;
        }

        Equal(true, samples > 0, "Selected numeric family has actual samples");
    }
}
