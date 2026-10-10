// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored AOT-safe .NET adapter for the raw ordered-value uses of internal/json/json.go.
// JSONC syntax/diagnostics are parsed by the actual TypeScript parser, not this adapter.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using TypeScript.Collections;
using TypeScript.Json.Internal;
using TypeScript.Scanner;

namespace TypeScript.Json;
/// <summary>Already-encoded raw JSON, corresponding to jsontext.Value.</summary>
public sealed record JsonValue(byte[]? Bytes, int SourceOffset = 0, string Pointer = "");
public static class JsonCodec
{
    private static readonly UTF8Encoding strictUtf8 = new(false, true);
    public static (object? Value, Exception? Error) Parse(string goBytes, bool allowDuplicateNames = false) => parse(goBytes, allowDuplicateNames, 0, "");
    public static (object? Value, Exception? Error) Parse(JsonValue value, bool allowDuplicateNames = false) => parse(GoString.FromBytes(value.Bytes), allowDuplicateNames, value.SourceOffset, value.Pointer);
    private sealed class ReadFrame(object value)
    {
        internal readonly object Value = value;
        internal string Name = "";
    }

    private static (object? Value, Exception? Error) parse(string goBytes, bool allowDuplicateNames, int sourceOffset, string pointer)
    {
        object? root = null;
        var frames = new Stack<ReadFrame>();
        JsonTokenType last = JsonTokenType.None;
        void Attach(object? value)
        {
            if (frames.Count == 0)
                root = value;
            else if (frames.Peek().Value is List<object?> items)
                items.Add(value);
            else
                ((OrderedMap<string, object?>)frames.Peek().Value).Set(frames.Peek().Name, value);
        }

        try
        {
            if (goBytes.Any(ch => ch > 255))
                throw new ArgumentException("Raw JSON input must be a Go byte string.", nameof(goBytes));
            var reader = new Utf8JsonReader(GoString.Bytes(goBytes), new JsonReaderOptions { MaxDepth = 10000 });
            while (reader.Read())
            {
                last = reader.TokenType;
                switch (last)
                {
                    case JsonTokenType.StartObject:
                        var fields = new OrderedMap<string, object?>();
                        Attach(fields);
                        frames.Push(new ReadFrame(fields));
                        break;
                    case JsonTokenType.StartArray:
                        var values = new List<object?>();
                        Attach(values);
                        frames.Push(new ReadFrame(values));
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        frames.Pop();
                        break;
                    case JsonTokenType.PropertyName:
                        string name = readString(ref reader);
                        var frame = frames.Peek();
                        var map = (OrderedMap<string, object?>)frame.Value;
                        if (!allowDuplicateNames && map.Has(name))
                            throw new JsonException("duplicate object member name");
                        frame.Name = name;
                        map.Set(name, null);
                        break;
                    case JsonTokenType.String:
                        Attach(null);
                        string text = readString(ref reader);
                        if (frames.Count != 0 && frames.Peek().Value is List<object?> list)
                            list[^1] = text;
                        else if (frames.Count != 0)
                            ((OrderedMap<string, object?>)frames.Peek().Value).Set(frames.Peek().Name, text);
                        else
                            root = text;
                        break;
                    case JsonTokenType.Number:
                        Attach(null);
                        double number = reader.GetDouble();
                        if (!double.IsFinite(number))
                            throw new OverflowException("JSON number is outside the float64 range.");
                        if (frames.Count != 0 && frames.Peek().Value is List<object?> numbers)
                            numbers[^1] = number;
                        else if (frames.Count != 0)
                            ((OrderedMap<string, object?>)frames.Peek().Value).Set(frames.Peek().Name, number);
                        else
                            root = number;
                        break;
                    case JsonTokenType.True:
                        Attach(true);
                        break;
                    case JsonTokenType.False:
                        Attach(false);
                        break;
                    case JsonTokenType.Null:
                        Attach(null);
                        break;
                    default:
                        throw new JsonException("Unexpected JSON token.");
                }
            }

            return (root, last == JsonTokenType.None ? JsonDiagnosticReader.Analyze(goBytes, allowDuplicateNames, sourcePointer: pointer, sourceOffset: sourceOffset) : null);
        }
        catch (Exception error)when (error is JsonException or DecoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            // Reconstruct only the partial value on a failed .NET read. The
            // selected source token grammar commits completed tokens before a
            // following delimiter error and retains original partial containers.
            var partial = JsonDiagnosticReader.Decode(goBytes, allowDuplicateNames, sourcePointer: pointer, sourceOffset: sourceOffset);
            return (partial.Value, partial.Error ?? error);
        }
    }

    public static (object? Value, Exception? Error) Parse(byte[] bytes, bool allowDuplicateNames = false) => Parse(GoString.FromBytes(bytes), allowDuplicateNames);
    // Selected typed-struct adapter: original ordered occurrences and raw value
    // tokens are retained. Unknown fields are skipped lexically without decoding
    // numbers to float64. Completed fields survive later input errors.
    public static (List<(string Name, JsonValue Value)>? Fields, bool IsNull, Exception? Error) ParseObjectFields(byte[] bytes, int sourceOffset = 0, string pointer = "")
    {
        string input = GoString.FromBytes(bytes);
        var(handled, tokenError) = JsonDiagnosticReader.ReadNonObjectToken(input, null, "struct");
        if (handled)
            return (null, false, tokenError);
        Exception? syntaxError = JsonDiagnosticReader.Analyze(input, true, validateNumbers: false, sourcePointer: pointer, sourceOffset: sourceOffset);
        var fields = new List<(string Name, JsonValue Value)>();
        string? pendingName = null;
        int pendingStart = 0;
        void RetainPending()
        {
            if (pendingName is null)
                return;
            int start = pendingStart + JsonWire.ConsumeWhitespace(input.AsSpan(pendingStart));
            if (start == bytes.Length)
                return;
            int length = JsonDiagnosticReader.ReadPrimitivePrefix(input.AsSpan(start));
            // Stream-based custom field decoders may run before a containing
            // close is available. Retain the unread container suffix too, so
            // its earlier semantic error can precede the later lexical error.
            byte[] raw = length > 0 ? bytes[start..(start + length)] : bytes[start..];
            fields.Add((pendingName, new JsonValue(raw, sourceOffset + start, JsonTextErrors.AppendPointer(pointer, pendingName))));
            pendingName = null;
        }

        try
        {
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 10000 });
            if (!reader.Read())
                return (fields, false, syntaxError);
            if (reader.TokenType == JsonTokenType.Null)
                return (null, true, syntaxError);
            if (reader.TokenType != JsonTokenType.StartObject)
                return (null, false, syntaxError);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return (fields, false, syntaxError);
                string name = readString(ref reader);
                pendingName = name;
                pendingStart = checked((int)reader.BytesConsumed);
                if (!reader.Read())
                    return (fields, false, syntaxError);
                int start = checked((int)reader.TokenStartIndex);
                pendingStart = start;
                using var document = JsonDocument.ParseValue(ref reader);
                int end = checked((int)reader.BytesConsumed);
                byte[] raw = bytes[start..end];
                // JsonDocument preserves string tokens without decoding every
                // nested escape; the source lexer validates skipped strings too.
                if (JsonDiagnosticReader.Analyze(GoString.FromBytes(raw), true, validateNumbers: false)is not null)
                    return (fields, false, syntaxError);
                fields.Add((name, new JsonValue(raw, sourceOffset + start, JsonTextErrors.AppendPointer(pointer, name))));
                pendingName = null;
            }

            return (fields, false, syntaxError);
        }
        catch (Exception error)when (error is JsonException or DecoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            RetainPending();
            return (fields, false, syntaxError ?? error);
        }
    }

    public static (List<JsonValue>? Values, bool IsNull, Exception? Error) ParseArrayValues(byte[] bytes, int sourceOffset = 0, string pointer = "")
    {
        string input = GoString.FromBytes(bytes);
        Exception? syntaxError = JsonDiagnosticReader.Analyze(input, true, validateNumbers: false, sourcePointer: pointer, sourceOffset: sourceOffset);
        var values = new List<JsonValue>();
        try
        {
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 10000 });
            if (!reader.Read())
                return (values, false, syntaxError);
            if (reader.TokenType == JsonTokenType.Null)
                return (null, true, syntaxError);
            if (reader.TokenType != JsonTokenType.StartArray)
                return (null, false, new ArgumentException("ParseArrayValues requires a JSON array or null."));
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return (values, false, syntaxError);
                int start = checked((int)reader.TokenStartIndex);
                using var document = JsonDocument.ParseValue(ref reader);
                int end = checked((int)reader.BytesConsumed);
                byte[] raw = bytes[start..end];
                if (JsonDiagnosticReader.Analyze(GoString.FromBytes(raw), true, validateNumbers: false)is not null)
                    return (values, false, syntaxError);
                values.Add(new JsonValue(raw, sourceOffset + start, JsonTextErrors.AppendPointer(pointer, values.Count.ToString(CultureInfo.InvariantCulture))));
            }

            return (values, false, syntaxError);
        }
        catch (Exception error)when (error is JsonException or DecoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            return (values, false, syntaxError ?? error);
        }
    }

    // Selected json/v2 makeSliceArshaler + makeIntArshaler default decoding.
    // This fixed typed destination supports SpanMap.Unmarshal; it introduces no
    // reflection, configurable decoder or general typed-marshaling framework.
    public static (List<List<int>?>? Value, Exception? Error) UnmarshalInt32Matrix(byte[] bytes)
    {
        string input = GoString.FromBytes(bytes);
        List<List<int>?>? result = null;
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 10000 });
        Exception? Semantic(ref Utf8JsonReader token, string type, string pointer, bool integer)
        {
            int start = checked((int)token.TokenStartIndex);
            int end;
            try { using var document = JsonDocument.ParseValue(ref token); end = checked((int)token.BytesConsumed); }
            catch (JsonException) { return JsonDiagnosticReader.AnalyzeRawValue(input[start..], sourcePointer: pointer, sourceOffset: start); }
            string raw = input[start..end];
            var lexical = JsonDiagnosticReader.AnalyzeRawValue(raw, sourcePointer: pointer, sourceOffset: start);
            if (lexical is not null) return lexical;
            string kind = raw[0] == '"' ? "string" : raw[0] == '[' ? "array" : raw[0] == '{' ? "object" : raw[0] is 'f' or 't' ? "boolean" : raw[0] == 'n' ? "null" : "number";
            string? error = null;
            string value = "";
            if (integer && kind == "number")
            {
                value = raw;
                bool negative = raw[0] == '-';
                string digits = negative ? raw[1..] : raw;
                bool allDigits = digits.All(ch => ch is >= '0' and <= '9');
                bool parsed = ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out ulong number);
                error = !allDigits ? "invalid syntax" : !parsed || number > (negative ? 2147483648UL : 2147483647UL) ? "value out of range" : null;
                if (error is null) return null;
            }
            return JsonTextErrors.AsException(JsonTextErrors.SemanticError("unmarshal", kind, value, type, pointer, start, error));
        }
        bool Read(ref Utf8JsonReader token) => token.Read();
        try
        {
            if (!Read(ref reader)) return (null, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false));
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Read(ref reader)) return (null, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false));
                return (null, null);
            }
            if (reader.TokenType != JsonTokenType.StartArray) return (null, Semantic(ref reader, "[][]int32", "", false));
            result = [];
            while (true)
            {
                bool read;
                try { read = Read(ref reader); }
                catch (JsonException) { result.Add(null); throw; }
                if (!read) { result.Add(null); break; }
                if (reader.TokenType == JsonTokenType.EndArray) break;
                int rowIndex = result.Count;
                string pointer = "/" + rowIndex.ToString(CultureInfo.InvariantCulture);
                result.Add(null);
                if (reader.TokenType == JsonTokenType.Null) continue;
                if (reader.TokenType != JsonTokenType.StartArray) return (result, Semantic(ref reader, "[]int32", pointer, false));
                var row = new List<int>(); result[rowIndex] = row;
                while (true)
                {
                    try { read = Read(ref reader); }
                    catch (JsonException) { row.Add(0); throw; }
                    if (!read) { row.Add(0); return (result, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false)); }
                    if (reader.TokenType == JsonTokenType.EndArray) break;
                    int column = row.Count; row.Add(0);
                    string elementPointer = pointer + "/" + column.ToString(CultureInfo.InvariantCulture);
                    if (reader.TokenType == JsonTokenType.Null) continue;
                    bool number = reader.TokenType == JsonTokenType.Number;
                    int start = checked((int)reader.TokenStartIndex);
                    string raw = number ? input[start..checked((int)reader.BytesConsumed)] : "";
                    var error = Semantic(ref reader, "int32", elementPointer, true);
                    if (error is not null) return (result, error);
                    if (number) row[column] = int.Parse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                }
            }
            if (Read(ref reader)) return (result, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false));
            return (result, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false));
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            return (result, JsonDiagnosticReader.Analyze(input, allowDuplicateNames: false, validateNumbers: false) ?? error);
        }
    }

    public static Exception? ValidateValue(JsonValue value, bool allowDuplicateNames = false) => JsonDiagnosticReader.AnalyzeRawValue(GoString.FromBytes(value.Bytes), allowDuplicateNames, sourcePointer: value.Pointer, sourceOffset: value.SourceOffset);
    // Stream adapter used by RawCompilerOptions.UnmarshalJSONFrom. Completed fields
    // remain published if a later token fails, matching the original decoder loop.
    public static Exception? UnmarshalObject(string goBytes, OrderedMap<string, object?> target, string nonObjectError, bool allowDuplicateNames = false)
    {
        var(handled, tokenError) = JsonDiagnosticReader.ReadNonObjectToken(goBytes, nonObjectError);
        if (handled)
            return tokenError;
        try
        {
            var reader = new Utf8JsonReader(GoString.Bytes(goBytes), new JsonReaderOptions { MaxDepth = 10000 });
            if (!reader.Read())
                throw new JsonException("unexpected end of JSON input");
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (reader.Read())
                    throw new JsonException("unexpected token after JSON value");
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                return JsonTextErrors.AsException(JsonTextErrors.SemanticError("unmarshal", "", "", "tsoptions.RawCompilerOptions", "", 0, nonObjectError));
            readObject(ref reader, target, allowDuplicateNames);
            if (reader.Read())
                throw new JsonException("unexpected token after JSON value");
            return null;
        }
        catch (Exception error)when (error is JsonException or DecoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            return JsonDiagnosticReader.Analyze(goBytes, allowDuplicateNames) ?? error;
        }
    }

    private static object? readToken(ref Utf8JsonReader reader, bool allowDuplicateNames)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.String:
                return readString(ref reader);
            case JsonTokenType.Number:
                double number = reader.GetDouble();
                if (!double.IsFinite(number))
                    throw new OverflowException("JSON number is outside the float64 range.");
                return number;
            case JsonTokenType.StartArray:
                var values = new List<object?>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return values;
                    values.Add(readToken(ref reader, allowDuplicateNames));
                }

                throw new JsonException("unexpected end of JSON array");
            case JsonTokenType.StartObject:
                var fields = new OrderedMap<string, object?>();
                readObject(ref reader, fields, allowDuplicateNames);
                return fields;
            default:
                throw new JsonException("unexpected JSON token");
        }
    }

    private static void readObject(ref Utf8JsonReader reader, OrderedMap<string, object?> fields, bool allowDuplicateNames)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("expected JSON object member name");
            string key = readString(ref reader);
            if (!names.Add(key) && !allowDuplicateNames)
                throw new JsonException("duplicate object member name " + GoString.ToString(key));
            if (!reader.Read())
                throw new JsonException("unexpected end of JSON object");
            fields.Set(key, readToken(ref reader, allowDuplicateNames));
        }

        throw new JsonException("unexpected end of JSON object");
    }

    private static string readString(ref Utf8JsonReader reader)
    {
        // GetString validates UTF16 escapes; the byte validation prevents replacement
        // of invalid unescaped UTF8, including invalid virtual names.
        _ = strictUtf8.GetString(reader.ValueSpan);
        return GoString.ToUtf8(reader.GetString()!);
    }

    public static (byte[]? Bytes, Exception? Error) TryMarshal(object? value)
    {
        var writer = new GoBuilder();
        try
        {
            writeValue(writer, value);
            return (GoString.Bytes(writer.String()), null);
        }
        catch (Exception error)when (error is InvalidOperationException or NotSupportedException or JsonException or FormatException)
        {
            return (GoString.Bytes(writer.String()), error);
        }
    }

    public static byte[] Marshal(object? value) => GoString.Bytes(MarshalString(value));
    public static string MarshalString(object? value)
    {
        var writer = new GoBuilder();
        writeValue(writer, value);
        return writer.String();
    }

    private readonly record struct WriteJob(object? Value, int Depth, string Pointer, int Kind = 0, string Prefix = "");
    private static void writeValue(GoBuilder writer, object? root)
    {
        // Go's stack grows; managed stacks do not. Explicit work preserves the
        // same ordered container traversal for the complete legal JSON depth.
        var pending = new Stack<WriteJob>();
        var active = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push(new WriteJob(root, 0, ""));
        while (pending.Count != 0)
        {
            WriteJob job = pending.Pop();
            if (job.Kind is 3 or 4)
            {
                if (job.Depth >= 1000)
                    active.Remove(job.Value!);
                writer.WriteString(job.Kind == 3 ? "}" : "]");
                continue;
            }

            if (job.Kind == 1)
            {
                writer.WriteString((string)job.Value!);
                continue;
            }

            if (job.Kind == 2)
            {
                quote(writer, (string)job.Value!);
                continue;
            }

            if (job.Kind == 5)
            {
                // Definition.Options has v2 omitempty: successful empty raw
                // values are unwritten, but failures retain the member name.
                var rawOptions = (JsonValue)job.Value!;
                const string memberName = "\"options\"";
                var result = rawOptions.Bytes is null ? (Bytes: GoString.Bytes("null"), Error: (Exception? )null) : FormatRawValue(rawOptions, allowInvalidUtf8: true, depth: job.Depth, pointer: job.Pointer, offset: writer.Len() + job.Prefix.Length + memberName.Length + 1);
                if (result.Error != null)
                {
                    writer.WriteString(job.Prefix + memberName);
                    string cause = GoString.ToUtf8(result.Error.Message)["jsontext: ".Length..];
                    throw JsonTextErrors.AsException(JsonTextErrors.SemanticError("marshal", "", "", "jsontext.Value", "", 0, cause));
                }

                string encoded = GoString.FromBytes(result.Bytes!);
                if (encoded is not ("null" or "\"\"" or "[]" or "{}"))
                    writer.WriteString(job.Prefix + memberName + ":" + encoded);
                continue;
            }

            object? value = job.Value;
            // The original cycle detector runs before the encoder emits the
            // next delimiter; its visit map starts beyond depth 1000.
            if (job.Depth >= 1000 && value is not null && active.Contains(value))
            {
                string goType = value is OrderedMap<string, object?> or Dictionary<string, object?> ? "map[string]interface {}" : "[]interface {}";
                throw JsonTextErrors.AsException(JsonTextErrors.SemanticError("marshal", "", "", goType, job.Pointer, 0, "encountered a cycle"));
            }

            if (value is double nonfinite && !double.IsFinite(nonfinite))
            {
                string label = double.IsNaN(nonfinite) ? "NaN" : nonfinite > 0 ? "+Inf" : "-Inf";
                throw JsonTextErrors.AsException(JsonTextErrors.SemanticError("marshal", "", "", "float64", job.Pointer, 0, "unsupported value: " + label));
            }

            bool prefixWritten = false;
            void WritePrefix()
            {
                if (!prefixWritten)
                {
                    writer.WriteString(job.Prefix);
                    prefixWritten = true;
                }
            }

            bool container = value is TypeScript.TsOptions.TSConfig or OrderedMap<string, object?> or OrderedMap<string, List<string>?> or OrderedMap<string, JsonValue> or Dictionary<string, object?> or CompilerOptions or TypeAcquisition or BuildOptions or TypeScript.ContentMapper.Definition or PluginImport or ProjectReference || value is System.Collections.IEnumerable && value is not (string or byte[]);
            if (!container && value is not JsonValue)
                WritePrefix();
            void PushValue(object? item) => pending.Push(new WriteJob(item, job.Depth, job.Pointer));
            void Begin(object owner)
            {
                if (job.Depth == 10000)
                    throw JsonTextErrors.AsException(JsonTextErrors.SyntacticError("exceeded max depth", job.Pointer, writer.Len() + job.Prefix.Length));
                if (job.Depth >= 1000)
                    active.Add(owner);
            }

            void PushObject(IEnumerable<(string Key, object? Value)> entries)
            {
                var fields = entries.ToList();
                if (fields.Count == 0)
                {
                    WritePrefix();
                    writer.WriteString("{}");
                    return;
                }

                Begin(value!);
                WritePrefix();
                writer.WriteString("{");
                pending.Push(new WriteJob(value, job.Depth, job.Pointer, 3));
                for (int i = fields.Count - 1; i >= 0; i--)
                {
                    var field = fields[i];
                    if (value is TypeScript.ContentMapper.Definition && field.Key == "options" && field.Value is JsonValue)
                    {
                        pending.Push(new WriteJob(field.Value, job.Depth + 1, JsonTextErrors.AppendPointer(job.Pointer, field.Key), 5, i > 0 ? "," : ""));
                        continue;
                    }

                    pending.Push(new WriteJob(field.Value, job.Depth + 1, JsonTextErrors.AppendPointer(job.Pointer, field.Key), Prefix: ":"));
                    pending.Push(new WriteJob(field.Key, 0, "", 2));
                    if (i > 0)
                        pending.Push(new WriteJob(",", 0, "", 1));
                }
            }

            switch (value)
            {
                case TypeScript.TsOptions.EmptyConfigurationObject:
                    writer.WriteString("{}");
                    continue;
                case TypeScript.TsOptions.TSConfig config:
                    var configFields = new OrderedMap<string, object?>();
                    configFields.Set("compilerOptions", config.CompilerOptions);
                    if (config.References != null)
                        configFields.Set("references", config.References);
                    if (config.Files != null)
                        configFields.Set("files", config.Files);
                    if (config.Include != null)
                        configFields.Set("include", config.Include);
                    if (config.Exclude != null)
                        configFields.Set("exclude", config.Exclude);
                    if (config.CompileOnSave != null)
                        configFields.Set("compileOnSave", config.CompileOnSave.Value);
                    PushObject(configFields.Entries());
                    continue;
                case null:
                    writer.WriteString("null");
                    continue;
                case bool flag:
                    writer.WriteString(flag ? "true" : "false");
                    continue;
                case Tristate state:
                    writer.WriteString(state == Tristate.True ? "true" : state == Tristate.False ? "false" : "null");
                    continue;
                case string text:
                    quote(writer, text);
                    continue;
                case StrongBox<long> pointer:
                    PushValue(pointer.Value);
                    continue;
                case StrongBox<bool> pointer:
                    PushValue(pointer.Value);
                    continue;
                case long number:
                    writer.WriteString(number.ToString(CultureInfo.InvariantCulture));
                    continue;
                case int number:
                    writer.WriteString(number.ToString(CultureInfo.InvariantCulture));
                    continue;
                case uint number:
                    writer.WriteString(number.ToString(CultureInfo.InvariantCulture));
                    continue;
                case ulong number:
                    writer.WriteString(number.ToString(CultureInfo.InvariantCulture));
                    continue;
                case double number:
                    writer.WriteString(number == 0 && BitConverter.DoubleToInt64Bits(number) < 0 ? "-0" : new JsNum.Number(number).String());
                    continue;
                case ScriptTarget item:
                    PushValue((int)item);
                    continue;
                case ModuleKind item:
                    PushValue((int)item);
                    continue;
                case ModuleResolutionKind item:
                    PushValue((int)item);
                    continue;
                case ModuleDetectionKind item:
                    PushValue((int)item);
                    continue;
                case NewLineKind item:
                    PushValue((int)item);
                    continue;
                case JsxEmit item:
                    PushValue((int)item);
                    continue;
                case RootedPath path:
                    quote(writer, path.AsString());
                    continue;
                case RootedFilePath path:
                    quote(writer, path.AsString());
                    continue;
                case RootedDirectoryPath path:
                    quote(writer, path.AsString());
                    continue;
                case SourceMapLocation path:
                    quote(writer, path.AsString());
                    continue;
                case PathKey path:
                    quote(writer, path.AsString());
                    continue;
                case FileSpec path:
                    quote(writer, path.AsString());
                    continue;
                case PathPattern path:
                    quote(writer, path.AsString());
                    continue;
                case JsonValue raw:
                    if (raw.Bytes is null)
                    {
                        WritePrefix();
                        writer.WriteString("null");
                        continue;
                    }

                    var formatted = FormatRawValue(raw, allowInvalidUtf8: true, depth: job.Depth, pointer: job.Pointer, offset: writer.Len() + job.Prefix.Length);
                    if (formatted.Error != null)
                    {
                        string cause = GoString.ToUtf8(formatted.Error.Message)["jsontext: ".Length..];
                        throw JsonTextErrors.AsException(JsonTextErrors.SemanticError("marshal", "", "", "jsontext.Value", "", 0, cause));
                    }

                    WritePrefix();
                    writer.WriteString(GoString.FromBytes(formatted.Bytes!));
                    continue;
                case byte[] bytes:
                    quote(writer, Convert.ToBase64String(bytes));
                    continue;
                case OrderedMap<string, object?> fields:
                    PushObject(fields.Entries());
                    continue;
                case Dictionary<string, object?> fields:
                    PushObject(fields.Select(entry => (entry.Key, entry.Value)));
                    continue;
                case OrderedMap<string, JsonValue> fields:
                    PushObject(fields.Entries().Select(entry => (entry.Key, (object?)entry.Value)));
                    continue;
                case OrderedMap<string, List<string>?> fields:
                    PushObject(fields.Entries().Select(entry => (entry.Key, (object? )entry.Value)));
                    continue;
                case CompilerOptions options:
                    PushObject(optionFields(options, OptionFields.CompilerOptions));
                    continue;
                case TypeAcquisition options:
                    PushObject(optionFields(options, OptionFields.TypeAcquisition));
                    continue;
                case BuildOptions options:
                    PushObject(optionFields(options, OptionFields.BuildOptions));
                    continue;
                case TypeScript.ContentMapper.Mapper mapper:
                    PushObject(mapperFields(mapper));
                    continue;
                case TypeScript.ContentMapper.Definition definition:
                    PushObject(mapperFields(definition));
                    continue;
                case PluginImport plugin:
                    PushObject(new[] { ("name", (object? )plugin.Name) });
                    continue;
                case ProjectReference reference:
                    var referenceFields = new OrderedMap<string, object?>();
                    referenceFields.Set("path", reference.Path);
                    if (reference.OriginalPath != "")
                        referenceFields.Set("originalPath", reference.OriginalPath);
                    // JSONv2 omitempty preserves false: only null, empty string,
                    // array and object are JSON-empty values.
                    referenceFields.Set("circular", reference.Circular);
                    PushObject(referenceFields.Entries());
                    continue;
                case System.Collections.IEnumerable values:
                    var items = values.Cast<object?>().ToList();
                    if (items.Count == 0)
                    {
                        WritePrefix();
                        writer.WriteString("[]");
                        continue;
                    }

                    Begin(values);
                    WritePrefix();
                    writer.WriteString("[");
                    pending.Push(new WriteJob(values, job.Depth, job.Pointer, 4));
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        pending.Push(new WriteJob(items[i], job.Depth + 1, JsonTextErrors.AppendPointer(job.Pointer, i.ToString(CultureInfo.InvariantCulture)), Prefix: i > 0 ? "," : ""));
                    }

                    continue;
                default:
                    throw new NotSupportedException("Raw JSON adapter has no static mapping for " + value.GetType().Name);
            }
        }
    }

    private static IEnumerable<(string Key, object? Value)> mapperFields(TypeScript.ContentMapper.Definition definition)
    {
        var fields = new OrderedMap<string, object?>();
        fields.Set("package", definition.Package);
        fields.Set("extensions", definition.Extensions);
        if (definition.Options != null)
            fields.Set("options", definition.Options);
        return fields.Entries();
    }

    private static IEnumerable<(string Key, object? Value)> optionFields<T>(T options, IReadOnlyList<OptionFieldDescriptor<T>> descriptors)
    {
        var fields = new OrderedMap<string, object?>();
        foreach (var descriptor in descriptors)
        {
            object? value = descriptor.Read(options);
            if (!isZero(value))
                fields.Set(descriptor.JsonName, value);
        }

        return fields.Entries();
    }

    internal static bool IsZeroValue(object? value) => isZero(value);
    private static bool isZero(object? value) => value switch
    {
        null => true,
        string text => text == "",
        Tristate flag => flag == Tristate.Unknown,
        ScriptTarget flag => flag == 0,
        ModuleKind flag => flag == 0,
        ModuleResolutionKind flag => flag == 0,
        ModuleDetectionKind flag => flag == 0,
        NewLineKind flag => flag == 0,
        JsxEmit flag => flag == 0,
        RootedPath path => path == default,
        RootedFilePath path => path == default,
        RootedDirectoryPath path => path == default,
        SourceMapLocation path => path == default,
        // Nonnull pointer-to-int, slices and ordered-map pointers are nonzero even at value/count zero.
        _ => false,
    };
    // Selected whole-buffer Encoder.WriteValue/reformatValue counterpart. The
    // temporary output is committed only after all syntax succeeds; no stream API.
    internal static (byte[]? Bytes, Exception? Error) FormatRawValue(JsonValue value, bool allowInvalidUtf8 = false, int depth = 0, string pointer = "", int offset = 0)
    {
        string input = GoString.FromBytes(value.Bytes);
        var error = JsonDiagnosticReader.AnalyzeRawValue(input, writing: true, allowInvalidUtf8: allowInvalidUtf8, sourcePointer: pointer, sourceOffset: offset, depth: depth);
        if (error != null)
            return (null, error);
        var output = new GoBuilder();
        for (int i = 0; i < input.Length;)
        {
            if (input[i] is ' ' or '\t' or '\n' or '\r')
            {
                i++;
                continue;
            }

            if (input[i] == '"')
            {
                int length = JsonWire.ConsumeString(input.AsSpan(i), !allowInvalidUtf8).Consumed;
                quote(output, JsonWire.Unquote(input.Substring(i, length)));
                i += length;
            }
            else
                output.WriteByte((byte)input[i++]);
        }

        return (GoString.Bytes(output.String()), null);
    }

    private static void quote(GoBuilder writer, string bytes)
    {
        writer.WriteString("\"");
        for (int offset = 0; offset < bytes.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(bytes.AsSpan(offset));
            offset += size;
            switch (rune)
            {
                case '"':
                    writer.WriteString("\\\"");
                    break;
                case '\\':
                    writer.WriteString("\\\\");
                    break;
                case '\b':
                    writer.WriteString("\\b");
                    break;
                case '\f':
                    writer.WriteString("\\f");
                    break;
                case '\n':
                    writer.WriteString("\\n");
                    break;
                case '\r':
                    writer.WriteString("\\r");
                    break;
                case '\t':
                    writer.WriteString("\\t");
                    break;
                default:
                    if (rune < 0x20)
                        writer.WriteString("\\u" + rune.ToString("x4", CultureInfo.InvariantCulture));
                    else
                        writer.WriteRune(rune);
                    break;
            }
        }

        writer.WriteString("\"");
    }
}
