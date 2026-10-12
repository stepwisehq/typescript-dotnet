// Authored Go fmt boundary for actual pinned project/LSP logging consumers.
// Does not alter the accepted compiler/scanner formatter. Structured owners supply
// their explicit Go projection; reflection and unknown-format fallbacks are absent.
using System.Collections;
using System.Globalization;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Logging;
internal interface IGoLogStringer
{
    string GoLogString();
}

internal readonly record struct GoLogDuration(long Nanoseconds) : IGoLogStringer
{
    // Go time/format.go Duration.String. TimeSpan callers have 100ns resolution;
    // explicit nanoseconds retain the original precision where the host provides it.
    public string GoLogString()
    {
        if (Nanoseconds == 0)
            return "0s";
        bool negative = Nanoseconds < 0;
        ulong value = negative ? (ulong)(-(Nanoseconds + 1)) + 1 : (ulong)Nanoseconds;
        string result;
        if (value < 1_000_000_000)
        {
            if (value < 1_000)
                result = value.ToString(CultureInfo.InvariantCulture) + "ns";
            else if (value < 1_000_000)
                result = Fraction(value, 1_000, 3) + GoString.ToUtf8("µs");
            else
                result = Fraction(value, 1_000_000, 6) + "ms";
        }
        else
        {
            ulong seconds = value / 1_000_000_000;
            ulong minutes = seconds / 60;
            ulong hours = minutes / 60;
            result = (hours == 0 ? "" : hours.ToString(CultureInfo.InvariantCulture) + "h") + (minutes == 0 ? "" : (minutes % 60).ToString(CultureInfo.InvariantCulture) + "m") + Fraction(value % 60_000_000_000, 1_000_000_000, 9) + "s";
        }

        return negative ? "-" + result : result;
    }

    private static string Fraction(ulong value, ulong divisor, int digits)
    {
        ulong remainder = value % divisor;
        string integer = (value / divisor).ToString(CultureInfo.InvariantCulture);
        return remainder == 0 ? integer : integer + "." + remainder.ToString("D" + digits, CultureInfo.InvariantCulture).TrimEnd('0');
    }
}

internal static class LogFormat
{
    public static string Sprint(params object? [] args)
    {
        var builder = new GoBuilder();
        for (int i = 0; i < args.Length; i++)
        {
            if (i != 0 && !IsStringKind(args[i - 1]) && !IsStringKind(args[i]))
                builder.WriteString(" ");
            builder.WriteString(Value(args[i]));
        }

        return builder.String();
    }

    private static bool IsStringKind(object? value) => value is string or ByteStringValue or ByteStringKey or RootedFilePath or RootedDirectoryPath or RootedPath or PathKey;
    public static string Sprintf(string format, params object? [] args)
    {
        var builder = new GoBuilder();
        int next = 0;
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '%')
            {
                builder.WriteByte((byte)format[i]);
                continue;
            }

            if (++i == format.Length)
                throw new FormatException("missing log format verb");
            if (format[i] == '%')
            {
                builder.WriteString("%");
                continue;
            }

            bool left = format[i] == '-';
            if (left)
                i++;
            bool zero = i < format.Length && format[i] == '0';
            int width = 0;
            while (i < format.Length && char.IsAsciiDigit(format[i]))
                width = checked(width * 10 + format[i++] - '0');
            if (i == format.Length || next == args.Length)
                throw new FormatException("invalid log format arguments");
            object? arg = args[next++];
            string value = format[i] switch
            {
                's' => String(arg),
                'q' => GoQuote.Quote(String(arg)),
                'v' => Value(arg),
                't' => arg is bool flag ? flag ? "true" : "false" : throw new FormatException("non-bool log %t"),
                'd' => Integer(arg, false),
                'x' => Integer(arg, true),
                _ => throw new FormatException("unsupported project log format verb %" + format[i])};
            int pad = Math.Max(0, width - GoString.CountRunes(value));
            if (!left)
                builder.WriteRepeatedByte((byte)(zero ? '0' : ' '), pad);
            builder.WriteString(value);
            if (left)
                builder.WriteRepeatedByte((byte)' ', pad);
        }

        if (next != args.Length)
            throw new FormatException("unused log format arguments");
        return builder.String();
    }

    private static string String(object? value) => value switch
    {
        string text => text,
        TypeScript.Lsp.Protocol.DocumentUri uri => uri.Value.Materialize() ?? "",
        ByteStringValue text => text.Materialize() ?? "",
        ByteStringKey text => text.Value.Materialize() ?? "",
        RootedFilePath path => path.AsString(),
        RootedDirectoryPath path => path.AsString(),
        RootedPath path => path.AsString(),
        PathKey path => path.AsString(),
        IGoLogStringer stringer => stringer.GoLogString(),
        _ => throw new FormatException("non-string project log argument")};
    private static string Integer(object? value, bool hex)
    {
        string format = hex ? "x" : "D";
        return value switch
        {
            byte n => n.ToString(format, CultureInfo.InvariantCulture),
            sbyte n => n.ToString(format, CultureInfo.InvariantCulture),
            short n => n.ToString(format, CultureInfo.InvariantCulture),
            ushort n => n.ToString(format, CultureInfo.InvariantCulture),
            int n => n.ToString(format, CultureInfo.InvariantCulture),
            uint n => n.ToString(format, CultureInfo.InvariantCulture),
            long n => n.ToString(format, CultureInfo.InvariantCulture),
            ulong n => n.ToString(format, CultureInfo.InvariantCulture),
            _ => throw new FormatException("non-integer project log argument")};
    }

    private static string Value(object? value)
    {
        switch (value)
        {
            case null:
                return "<nil>";
            case bool boolean:
                return boolean ? "true" : "false";
            case TypeScript.Lsp.Protocol.DocumentUri or string or ByteStringValue or ByteStringKey or RootedFilePath or RootedDirectoryPath or RootedPath or PathKey or IGoLogStringer:
                return String(value);
            case Exception error:
                return GoString.ToUtf8(error.Message);
            case TimeSpan duration:
                return new GoLogDuration(checked(duration.Ticks * 100)).GoLogString();
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return Integer(value, false);
            // Slice is a value header with a pattern enumerator, not IEnumerable.
            // These are the actual ATA/project logging element roles. Formatting
            // owns only its output; no list/array bridge or generic ToString fallback.
            case Slice<TypeScript.Lsp.Protocol.DocumentUri> items:
                return SliceValue(items);
            case Slice<Exception> items:
                return SliceValue(items);
            case Slice<ByteStringValue> items:
                return SliceValue(items);
            case Slice<ByteStringKey> items:
                return SliceValue(items);
            case Slice<string> items:
                return SliceValue(items);
            case Slice<RootedFilePath> items:
                return SliceValue(items);
            case Slice<RootedDirectoryPath> items:
                return SliceValue(items);
            case Slice<RootedPath> items:
                return SliceValue(items);
            case Slice<PathKey> items:
                return SliceValue(items);
            case Slice<TypeScript.ProjectSystem.ID> items:
                return SliceValue(items);
            case Slice<TypeScript.ProjectSystem.ConfiguredProjectID> items:
                return SliceValue(items);
            case IDictionary dictionary:
            {
                // Go fmt sorts map keys. Actual logging maps have string/path keys;
                // their raw byte ordinal order is retained without display decoding.
                var entries = new List<(string Key, object? Value)>(dictionary.Count);
                foreach (DictionaryEntry item in dictionary)
                    entries.Add((Value(item.Key), item.Value));
                entries.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
                var builder = new GoBuilder();
                builder.WriteString("map[");
                for (int i = 0; i < entries.Count; i++)
                {
                    if (i != 0)
                        builder.WriteString(" ");
                    builder.WriteString(entries[i].Key);
                    builder.WriteString(":");
                    builder.WriteString(Value(entries[i].Value));
                }

                builder.WriteString("]");
                return builder.String();
            }

            case IEnumerable sequence:
            {
                var builder = new GoBuilder();
                builder.WriteString("[");
                bool first = true;
                foreach (object? item in sequence)
                {
                    if (!first)
                        builder.WriteString(" ");
                    first = false;
                    builder.WriteString(Value(item));
                }

                builder.WriteString("]");
                return builder.String();
            }

            default:
                throw new FormatException("project log value requires an explicit Go projection");
        }
    }

    private static string SliceValue<T>(Slice<T> items)
    {
        var builder = new GoBuilder();
        builder.WriteString("[");
        var first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.WriteString(" ");
            first = false;
            builder.WriteString(Value(item));
        }

        builder.WriteString("]");
        return builder.String();
    }
}
