// Authored differential observations for the actual consumed logging boundary.
// Go oracle: tools/language-services-oracle/log_format_controls_test.go (13 rows).
using TypeScript.ProjectSystem.Logging;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class ConsumedLogFormatChecks
{
    private static string Raw(string display) => GoString.ToUtf8(display);
    private static void Equal(string name, string format, string expected, params object? [] arguments)
    {
        string actual = LogFormat.Sprintf(format, arguments);
        if (actual != Raw(expected))
            throw new InvalidOperationException(name + ": consumed Go log formatting bytes differ: " + GoString.ToString(actual));
    }

    public static int RunAll()
    {
        Equal("nil", "%T\n%+v", "<nil>\n<nil>", null, null);
        Equal("bool", "%T\n%+v", "bool\ntrue", true, true);
        Equal("string", "%T\n%+v", "string\ncafé", Raw("café"), Raw("café"));
        Equal("number", "%T\n%+v", "float64\n1.25", 1.25, 1.25);
        var map = new Dictionary<string, object?>
        {
            ["z"] = 2d,
            ["a"] = new List<object?>
            {
                true,
                null,
                Raw("café")
            }
        };
        Equal("object", "%T\n%+v", "map[string]interface {}\nmap[a:[true <nil> café] z:2]", map, map);
        var array = new List<object?>
        {
            1d,
            false,
            "x"
        };
        Equal("array", "%T\n%+v", "[]interface {}\n[1 false x]", array, array);
        var emptyMap = new Dictionary<string, object?>();
        Equal("emptyObject", "%T\n%+v", "map[string]interface {}\nmap[]", emptyMap, emptyMap);
        var emptyArray = new List<object?>();
        Equal("emptyArray", "%T\n%+v", "[]interface {}\n[]", emptyArray, emptyArray);
        // The object[] is one actual []any argument, not params argument expansion.
        Equal("numbers", "%v", "[-0 0.0001 1e-05 999999 1e+06 1e+23 5e-324 1.7976931348623157e+308]", (object)new object? [] { -0d, 0.0001, 0.00001, 999999d, 1000000d, 1e23, double.Epsilon, double.MaxValue });
        var error = new InvalidOperationException("café failure");
        Equal("exceptionString", "%s\n%v\n%q", "café failure\ncafé failure\n\"café failure\"", error, error, error);
        var bytes = new GoBuilder();
        bytes.WriteString(Raw("café\n"));
        bytes.WriteByte(0xff);
        Equal("rawQuoted", "%q", "\"café\\n\\xff\"", bytes.Value());
        Equal("countWidth", "%6d/%d", "     7/-12", 7, -12L);
        Equal("duration", "%v", "1h1m0.123456789s", new GoLogDuration(3_660_123_456_789));
        // Authored admission control: decoder allows deep generic arrays. The
        // formatter preserves their source recursive spelling without CLR recursion.
        object? deep = null;
        for (int i = 0; i < 10000; i++)
            deep = new List<object?>
            {
                deep
            };
        string expectedDeep = new string ('[', 10000) + "<nil>" + new string (']', 10000);
        if (LogFormat.Sprintf("%+v", deep) != expectedDeep)
            throw new InvalidOperationException("deep decoded logging value differs");
        return 14;
    }
}
