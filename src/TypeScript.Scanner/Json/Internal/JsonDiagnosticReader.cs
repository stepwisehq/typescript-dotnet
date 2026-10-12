// Authored whole-input diagnostic adapter over selected pinned JSON grammar.
// Source-based token/state/error producers replace .NET exception wording only.
// No streaming I/O, reflection, general custom marshalers or decoder options.
using System.Globalization;
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.Collections;

namespace TypeScript.Json.Internal;
internal static class JsonDiagnosticReader
{
    private sealed class Frame(bool isObject, string pointer)
    {
        internal readonly bool Object = isObject;
        internal readonly string Pointer = pointer;
        internal object? Data;
        internal readonly HashSet<string> Names = new(StringComparer.Ordinal);
        internal string Name = "";
        internal int Index;
        internal int DelimiterPosition;
        internal int Stage; // 0 first/name/value, 1 colon, 2 object value, 3 after value, 4 after comma.
    }

    internal static (bool Handled, Exception? Error) ReadNonObjectToken(string input, string? nonObjectError, string type = "tsoptions.RawCompilerOptions")
    {
        int start = JsonWire.ConsumeWhitespace(input.AsSpan());
        if (start == input.Length || input[start] is '{' or 'n')
            return (false, null);
        ReadOnlySpan<char> token = input.AsSpan(start);
        WireResult consumed;
        switch (token[0])
        {
            case '[':
                consumed = new(1);
                break;
            case '\"':
                consumed = JsonWire.ConsumeString(token);
                break;
            case 'f':
                consumed = JsonWire.ConsumeLiteral(token, "false");
                break;
            case 't':
                consumed = JsonWire.ConsumeLiteral(token, "true");
                break;
            default:
                if (token[0] != '-' && token[0] is not (>= '0' and <= '9'))
                    return (false, null);
                consumed = JsonWire.ConsumeNumber(token);
                break;
        }

        if (consumed.Error is not null || consumed.UnexpectedEof)
            return (false, null);
        // Original custom UnmarshalJSONFrom rejects after just ReadToken; it
        // does not validate the remainder or convert number tokens to float64.
        string kind = (type == "struct" || type == "map[string]interface {}") ? token[0] == '[' ? "array" : token[0] == '"' ? "string" : token[0] is 'f' or 't' ? "boolean" : "number" : "";
        return (true, JsonTextErrors.AsException(JsonTextErrors.SemanticError("unmarshal", kind, "", type, "", start, nonObjectError)));
    }

    internal static int ReadPrimitivePrefix(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return 0;
        WireResult result;
        switch (input[0])
        {
            case '"':
                result = JsonWire.ConsumeString(input);
                break;
            case 'n':
                result = JsonWire.ConsumeLiteral(input, "null");
                break;
            case 'f':
                result = JsonWire.ConsumeLiteral(input, "false");
                break;
            case 't':
                result = JsonWire.ConsumeLiteral(input, "true");
                break;
            default:
                if (input[0] != '-' && input[0] is not (>= '0' and <= '9'))
                    return 0;
                result = JsonWire.ConsumeNumber(input);
                break;
        }

        return result.Error is null && !result.UnexpectedEof ? result.Consumed : 0;
    }

    internal static Exception? Analyze(string input, bool allowDuplicateNames, bool validateNumbers = true, string sourcePointer = "", int sourceOffset = 0) => Decode(input, allowDuplicateNames, validateNumbers, sourcePointer, sourceOffset, assemble: false).Error;
    internal static Exception? AnalyzeRawValue(string input, bool allowDuplicateNames = false, bool writing = false, bool allowInvalidUtf8 = false, string sourcePointer = "", int sourceOffset = 0, int depth = 0) => Decode(input, allowDuplicateNames, false, sourcePointer, sourceOffset, assemble: false, rawValue: true, writing, allowInvalidUtf8, depth).Error;
    internal static (object? Value, Exception? Error) Decode(string input, bool allowDuplicateNames, bool validateNumbers = true, string sourcePointer = "", int sourceOffset = 0, bool assemble = true, bool rawValue = false, bool writing = false, bool allowInvalidUtf8 = false, int depth = 0, bool dictionaryObjects = false)
    {
        // Explicit frames preserve source depth semantics without consuming the
        // managed call stack on legal 10,000-level inputs or rejected deeper ones.
        var stack = new List<Frame>();
        object? root = null;
        bool discard = false;
        void SetObjectField(object owner, string name, object? value)
        {
            if (owner is Dictionary<string, object?> dictionary)
                dictionary[name] = value;
            else
                ((OrderedMap<string, object?>)owner).Set(name, value);
        }

        void Set(object? value)
        {
            if (!assemble || discard)
                return;
            if (stack.Count == 0)
                root = value;
            else if (stack[^1].Data is List<object?> values)
                values[^1] = value;
            else
                SetObjectField(stack[^1].Data!, stack[^1].Name, value);
        }

        void Slot()
        {
            if (!assemble || discard)
                return;
            if (stack.Count == 0)
                root = null;
            else if (stack[^1].Data is List<object?> values)
                values.Add(null);
            else
                SetObjectField(stack[^1].Data!, stack[^1].Name, null);
        }

        int position = 0;
        bool complete = false;
        string? failure = null;
        void Whitespace() => position += JsonWire.ConsumeWhitespace(input.AsSpan(position));
        void Syntax(string error, string pointer, int offset, string? name = null) => failure = JsonTextErrors.SyntacticError(error, pointer, sourceOffset + offset, name);
        void Unexpected(string pointer) => Syntax("unexpected EOF", pointer, position);
        bool Consume(WireResult result, string pointer, bool number = false)
        {
            int start = position;
            if (result.UnexpectedEof)
            {
                // consumeNumber's terminal fetch reports its original resume
                // point; literal/string fetches report consumed prefix bytes.
                position = number && !writing ? start : start + result.Consumed;
                Unexpected(pointer);
                return false;
            }

            position += result.Consumed;
            if (result.Error is not null)
            {
                Syntax(result.Error, pointer, position);
                return false;
            }

            return true;
        }

        bool Value(string pointer)
        {
            Slot();
            Whitespace();
            if (position == input.Length)
            {
                Unexpected(pointer);
                return false;
            }

            char kind = input[position];
            switch (kind)
            {
                case '{':
                case '[':
                    object? data = !assemble ? null : kind != '{' ? new List<object?>() : dictionaryObjects ? new Dictionary<string, object?>(StringComparer.Ordinal) : new OrderedMap<string, object?>();
                    Set(data);
                    if (stack.Count + depth == 10000)
                    {
                        Syntax("exceeded max depth", pointer, position);
                        return false;
                    }

                    position++;
                    stack.Add(new Frame(kind == '{', pointer) { Data = data });
                    return true;
                case '"':
                    int stringStart = position;
                    if (!Consume(JsonWire.ConsumeString(input.AsSpan(position), !allowInvalidUtf8), pointer))
                        return false;
                    if (assemble)
                    {
                        using var valueDoc = JsonDocument.Parse(GoString.Bytes(input[stringStart..position]));
                        Set(GoString.ToUtf8(valueDoc.RootElement.GetString()!));
                    }

                    return true;
                case 'n':
                    return Consume(JsonWire.ConsumeLiteral(input.AsSpan(position), "null"), pointer);
                case 'f':
                    if (!Consume(JsonWire.ConsumeLiteral(input.AsSpan(position), "false"), pointer))
                        return false;
                    Set(false);
                    return true;
                case 't':
                    if (!Consume(JsonWire.ConsumeLiteral(input.AsSpan(position), "true"), pointer))
                        return false;
                    Set(true);
                    return true;
                default:
                    if (kind == '-' || kind is >= '0' and <= '9')
                    {
                        int start = position;
                        if (!Consume(JsonWire.ConsumeNumber(input.AsSpan(position)), pointer, number: true))
                            return false;
                        string rawNumber = input[start..position];
                        double.TryParse(rawNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double number);
                        Set(number);
                        if (validateNumbers && !discard && !double.IsFinite(number))
                        {
                            failure = JsonTextErrors.SemanticError("unmarshal", "number", rawNumber, "float64", pointer, sourceOffset + start, "value out of range");
                            return false;
                        }

                        return true;
                    }

                    Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(position), "at start of value"), pointer, position);
                    return false;
            }
        }

        if (!Value(sourcePointer))
            return (root, JsonTextErrors.AsException(failure!));
        if (stack.Count == 0)
            complete = true;
        while (!complete && failure is null)
        {
            var frame = stack[^1];
            Whitespace();
            string current = frame.Object ? JsonTextErrors.AppendPointer(frame.Pointer, frame.Name) : JsonTextErrors.AppendPointer(frame.Pointer, frame.Index.ToString(CultureInfo.InvariantCulture));
            if (position == input.Length)
            {
                if (!frame.Object)
                    Slot();
                Unexpected(frame.Stage is 1 or 2 ? current : frame.Pointer);
                break;
            }

            char next = input[position];
            if (frame.Stage is 0 or 4)
            {
                if (!rawValue && frame.Stage == 4 && next is '}' or ']')
                {
                    if (!frame.Object)
                        Slot();
                    Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(frame.DelimiterPosition), "at start of value"), frame.Pointer, frame.DelimiterPosition);
                    break;
                }

                char end = frame.Object ? '}' : ']';
                if (next == end && frame.Stage == 0)
                {
                    position++;
                    stack.RemoveAt(stack.Count - 1);
                    if (stack.Count == 0)
                        complete = true;
                    continue;
                }

                if (frame.Object)
                {
                    int start = position;
                    if (next != '"' && !rawValue)
                    {
                        discard = true;
                        if (Value(frame.Pointer))
                            Syntax("object member name must be a string", frame.Pointer, start);
                        break;
                    }

                    if (!Consume(JsonWire.ConsumeString(input.AsSpan(position), !allowInvalidUtf8), frame.Pointer))
                        break;
                    using var document = allowInvalidUtf8 ? null : JsonDocument.Parse(GoString.Bytes(input[start..position]));
                    frame.Name = allowInvalidUtf8 ? JsonWire.Unquote(input[start..position]) : GoString.ToUtf8(document!.RootElement.GetString()!);
                    if (!frame.Names.Add(frame.Name) && !allowDuplicateNames)
                    {
                        Syntax("duplicate object member name", frame.Pointer, start, frame.Name);
                        break;
                    }

                    if (assemble)
                        SetObjectField(frame.Data!, frame.Name, null);
                    frame.Stage = 1;
                }
                else
                {
                    frame.Stage = 3;
                    if (!Value(current))
                        break;
                }

                continue;
            }

            if (frame.Stage == 1)
            {
                if (next != ':')
                {
                    Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(position), "after object name (expecting ':')"), current, position);
                    break;
                }

                position++;
                frame.Stage = 2;
                continue;
            }

            if (frame.Stage == 2)
            {
                frame.Stage = 3;
                if (!Value(current))
                    break;
                continue;
            }

            if (next == (frame.Object ? '}' : ']'))
            {
                position++;
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0)
                    complete = true;
                continue;
            }

            if (next == ',')
            {
                frame.DelimiterPosition = position;
                position++;
                frame.Stage = 4;
                if (!frame.Object)
                    frame.Index++;
                continue;
            }

            if (!frame.Object)
                Slot();
            if (next == ':' && !rawValue)
            {
                int delimiter = position;
                position++;
                Whitespace();
                string where = position < input.Length && input[position] is '}' or ']' ? "at start of value" : frame.Object ? "after object value (expecting ',' or '}')" : "after array element (expecting ',' or ']')";
                Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(delimiter), where), frame.Pointer, delimiter);
                break;
            }

            Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(position), frame.Object ? "after object value (expecting ',' or '}')" : writing ? "after array value (expecting ',' or ']')" : "after array element (expecting ',' or ']')"), frame.Pointer, position);
        }

        if (failure is null)
        {
            Whitespace();
            if (position < input.Length)
                Syntax(JsonWire.NewInvalidCharacterError(input.AsSpan(position), "after top-level value"), sourcePointer, position);
        }

        return (root, failure is null ? null : JsonTextErrors.AsException(failure));
    }
}
