// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored closed decoder transport for lsproto/structcodec.go and json/v2.
// Closed typed consumers retain original JSON streaming behavior.
using System.Buffers;
using System.Text;
using System.Text.Json;
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal ref struct ProtocolDecoder
{
    private static readonly UTF8Encoding strictUtf8 = new(false, true);
    private readonly ByteStringValue inputOwner;
    private readonly ReadOnlySpan<byte> input;
    private Utf8JsonReader reader;
    private bool current;
    private struct Frame(bool isObject)
    {
        internal readonly bool Object = isObject;
        internal readonly HashSet<ByteStringKey>? Names = isObject ? new() : null;
        internal int Count;
        internal bool AwaitingValue;
        internal ByteStringValue Name;
    }

    private Slice<Frame> frames;
    private ref Frame CurrentFrame => ref frames.AsSpan()[frames.Count - 1];

    private int rootCount;
    private int previousStart;
    private int previousEnd;
    internal ProtocolDecoder(byte[] input) : this(ByteStringValue.FromView(ByteStringView.FromOwnedBytes(input)))
    {
    }

    internal ProtocolDecoder(ByteStringValue input)
    {
        // Byte-backed transport inputs remain borrowed. A legacy raw Go-string
        // caller owns one byte array at the actual decoder ingress boundary.
        inputOwner = input.IsBytes ? input : ByteStringValue.FromView(ByteStringView.FromOwnedBytes(GoString.Bytes(input.Materialize() ?? "")));
        this.input = inputOwner.ByteSpan;
        reader = new(this.input, new JsonReaderOptions { MaxDepth = 10000 });
        current = false;
        frames = default;
        rootCount = previousStart = previousEnd = 0;
    }

    // jsontext/decode.go PeekKind only examines the next byte and delimiter;
    // it does not read/validate the full token. This preserves the original
    // null-rejection and union-kind error order on malformed following values.
    internal byte PeekKind()
    {
        if (current)
            return TokenKind(reader.TokenType);
        int pos = checked((int)reader.BytesConsumed);
        while (pos < input.Length && input[pos] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            pos++;
        byte delimiter = 0;
        if (pos < input.Length && input[pos] is (byte)',' or (byte)':')
        {
            delimiter = input[pos++];
            while (pos < input.Length && input[pos] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                pos++;
        }

        if (pos == input.Length)
            return 0;
        byte kind = input[pos] is >= (byte)'0' and <= (byte)'9' or (byte)'-' ? (byte)'0' : input[pos];
        if (kind is not ((byte)'{') and not ((byte)'}') and not ((byte)'[') and not ((byte)']') and not ((byte)'"') and not ((byte)'0') and not ((byte)'t') and not ((byte)'f') and not ((byte)'n'))
            kind = 0;
        byte expectedDelimiter = 0;
        if (frames.Count != 0)
        {
            ref var frame = ref CurrentFrame;
            bool closing = kind == (frame.Object ? '}' : ']');
            if (!frame.AwaitingValue && frame.Count != 0 && !closing)
                expectedDelimiter = (byte)',';
        }

        return delimiter == expectedDelimiter ? kind : (byte)0;
    }

    private static byte TokenKind(JsonTokenType type) => type switch
    {
        JsonTokenType.StartObject => (byte)'{',
        JsonTokenType.EndObject => (byte)'}',
        JsonTokenType.StartArray => (byte)'[',
        JsonTokenType.EndArray => (byte)']',
        JsonTokenType.String or JsonTokenType.PropertyName => (byte)'"',
        JsonTokenType.Number => (byte)'0',
        JsonTokenType.True => (byte)'t',
        JsonTokenType.False => (byte)'f',
        JsonTokenType.Null => (byte)'n',
        _ => 0
    };
    private void EnsureCurrent()
    {
        if (current)
            return;
        try
        {
            if (!reader.Read())
                throw new JsonException("unexpected EOF");
        }
        catch (JsonException)
        {
            var error = JsonCodec.ValidateValue(new JsonValue(input.ToArray()));
            if (error is not null)
                throw error;
            throw;
        }

        current = true;
    }

    private void RecordToken()
    {
        previousStart = checked((int)reader.TokenStartIndex);
        previousEnd = checked((int)reader.BytesConsumed);
    }

    private void CommitValue()
    {
        RecordToken();
        if (frames.Count != 0)
        {
            ref var frame = ref CurrentFrame;
            frame.Count++;
            frame.AwaitingValue = false;
        }
        else
            rootCount++;
        current = false;
    }

    internal void ReadToken(byte expected)
    {
        if (PeekKind() != expected)
            throw new JsonException("Unexpected JSON token");
        EnsureCurrent();
        if (expected is (byte)'{' or (byte)'[')
        {
            CommitValue();
            frames = frames.Append(new Frame(expected == '{'));
        }
        else if (expected is (byte)'}' or (byte)']')
        {
            RecordToken();
            frames[frames.Count - 1] = default;
            frames = frames.Slice2(0, frames.Count - 1);
            current = false;
        }
        else
            CommitValue();
    }

    internal ByteStringValue ReadPropertyName() => ReadPropertyNameCore(false);
    internal ByteStringValue ReadDecodedPropertyName() => ReadPropertyNameCore(true);
    private ByteStringValue ReadPropertyNameCore(bool decodeName)
    {
        EnsureCurrent();
        if (reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException("Expected object member name");
        var raw = inputOwner.Slice(checked((int)reader.TokenStartIndex + 1), checked((int)reader.TokenStartIndex + 1 + reader.ValueSpan.Length));
        ByteStringValue decoded;
        if (!reader.ValueIsEscaped)
        {
            ValidateStringToken();
            decoded = raw;
        }
        else
            decoded = DecodeStringToken();
        if (!CurrentFrame.Names!.Add(ByteStringKey.FromValue(decoded)))
            throw JsonCodec.ValidateValue(new JsonValue(input.ToArray())) ?? new JsonException("duplicate object member name");
        RecordToken();
        current = false;
        CurrentFrame.AwaitingValue = true;
        CurrentFrame.Name = decoded;
        // structcodec.go's dispatch deliberately sees raw quoted-token bytes,
        // while json/v2's duplicate namespace compares decoded property names.
        return decodeName ? decoded : raw;
    }

    private ByteStringValue DecodeStringToken()
    {
        try
        {
            _ = strictUtf8.GetCharCount(reader.ValueSpan);
            if (!reader.ValueIsEscaped)
                return ByteStringValue.FromView(ByteStringView.CopyFrom(reader.ValueSpan));
            var bytes = new byte[reader.ValueSpan.Length];
            int length = reader.CopyString(bytes);
            return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes)).Slice(0, length);
        }
        catch (Exception error)when (error is DecoderFallbackException or InvalidOperationException)
        {
            // The current token is the first lexical failure. Validate the
            // complete owner to retain its original offset and JSON pointer.
            throw JsonCodec.ValidateValue(new JsonValue(input.ToArray())) ?? error;
        }
    }

    private void ValidateStringToken()
    {
        byte[]? rented = null;
        try
        {
            _ = strictUtf8.GetCharCount(reader.ValueSpan);
            if (!reader.ValueIsEscaped)
                return;
            // SkipValue retains no decoded string. jsontext validates escapes
            // while scanning; CopyString is the standard .NET validation join.
            Span<byte> scratch = reader.ValueSpan.Length <= 256 ? stackalloc byte[256] : (rented = ArrayPool<byte>.Shared.Rent(reader.ValueSpan.Length));
            _ = reader.CopyString(scratch);
        }
        catch (Exception error)when (error is DecoderFallbackException or InvalidOperationException)
        {
            // The current token is the first lexical failure. Validate the
            // complete owner to retain its original offset and JSON pointer.
            throw JsonCodec.ValidateValue(new JsonValue(input.ToArray())) ?? error;
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    internal ProtocolSemanticError InvalidValueFor(string goType)
    {
        _ = ReadValue();
        return ErrorAfter(goType);
    }

    internal void ReadArrayStart(string goType)
    {
        if (PeekKind() != '[')
            throw InvalidValueFor(goType);
        ReadToken((byte)'[');
    }

    internal void ReadObjectStart(string goType)
    {
        if (PeekKind() != '{')
            throw InvalidValueFor(goType);
        ReadToken((byte)'{');
    }

    internal ByteStringValue ReadString(string goType = "string")
    {
        if (PeekKind() == 'n')
        {
            ReadToken((byte)'n');
            return default;
        }

        EnsureCurrent();
        if (TokenKind(reader.TokenType) != '"' || reader.TokenType == JsonTokenType.PropertyName)
        {
            // makeStringArshaler reads a complete value, including composites,
            // before returning its semantic error.
            _ = ReadValue();
            throw ErrorAfter(goType);
        }

        var result = DecodeStringToken();
        CommitValue();
        return result;
    }

    internal bool ReadBoolean(string goType = "bool")
    {
        byte kind = PeekKind();
        if (kind == 'n')
        {
            ReadToken(kind);
            return false;
        }

        if (kind is not ((byte)'t') and not ((byte)'f'))
        {
            // makeBoolArshaler reads one token, not a complete composite value.
            EnsureCurrent();
            ReadToken(TokenKind(reader.TokenType));
            throw ErrorAfter(goType);
        }

        bool value = kind == 't';
        ReadToken(kind);
        return value;
    }

    // jsonwire.ParseUint, decode.go:593: retain unsigned accumulation and the
    // original syntax/range split rather than accepting exponent-form integers.
    private static (ulong Value, bool Valid) ParseUint(ReadOnlySpan<byte> text)
    {
        ulong value = 0;
        int n = 0;
        for (; n < text.Length && text[n] is >= (byte)'0' and <= (byte)'9'; n++)
            value = unchecked(10 * value + (ulong)(text[n] - (byte)'0'));
        if (n == 0 || n != text.Length || (text[0] == '0' && text.Length != 1))
            return (0, false);
        if (n >= 20 && (text[0] != '1' || value < 10000000000000000000UL || n > 20))
            return (ulong.MaxValue, false);
        return (value, true);
    }

    internal int ReadInt32(string goType = "int32")
    {
        // Default numeric arshalers consume a whole value before reporting its
        // semantic type error; that includes wrong-kind object/array values.
        var raw = ReadValue().TextValue;
        if (raw.EqualsBytes("null"u8))
            return 0;
        var text = raw.ByteSpan;
        if (text.Length == 0 || text[0] is not ((byte)'-') && text[0] is not (>= (byte)'0' and <= (byte)'9'))
            throw ErrorAfter(goType);
        bool negative = text[0] == '-';
        var(number, valid) = ParseUint(negative ? text[1..] : text);
        if (!valid && number != ulong.MaxValue)
            throw ErrorAfter(goType, "invalid syntax", true);
        if (!valid || number > (negative ? 2147483648UL : 2147483647UL))
            throw ErrorAfter(goType, "value out of range", true);
        return negative ? unchecked(-(int)number) : (int)number;
    }

    internal uint ReadUint32(string goType = "uint32")
    {
        var raw = ReadValue().TextValue;
        if (raw.EqualsBytes("null"u8))
            return 0;
        var text = raw.ByteSpan;
        if (text.Length == 0 || text[0] is not ((byte)'-') && text[0] is not (>= (byte)'0' and <= (byte)'9'))
            throw ErrorAfter(goType);
        var(number, valid) = ParseUint(text);
        if (!valid && number != ulong.MaxValue)
            throw ErrorAfter(goType, "invalid syntax", true);
        if (!valid || number > uint.MaxValue)
            throw ErrorAfter(goType, "value out of range", true);
        return (uint)number;
    }

    internal double ReadFloat64()
    {
        double value = 0;
        ReadFloat64(ref value);
        return value;
    }

    internal void ReadFloat64(ref double value, string goType = "float64")
    {
        var raw = ReadValue().TextValue;
        if (raw.EqualsBytes("null"u8))
        {
            value = 0;
            return;
        }

        var text = raw.ByteSpan;
        if (text.Length == 0 || text[0] is not ((byte)'-') && text[0] is not (>= (byte)'0' and <= (byte)'9'))
            throw ErrorAfter(goType);
        if (!System.Buffers.Text.Utf8Parser.TryParse(text, out double parsed, out int consumed) || consumed != text.Length)
            throw ErrorAfter(goType, "invalid syntax", true);
        // strconv.ParseFloat writes its result before returning ErrRange. The
        // caller's finally writes this partial value back to its retained field.
        value = parsed;
        if (!double.IsFinite(parsed))
            throw ErrorAfter(goType, "value out of range", true);
    }

    // jsontext/decode.go:417: composite skips consume tokens until the
    // starting depth is restored. Do not recurse or retain decoded value text.
    internal void SkipValue()
    {
        byte kind = PeekKind();
        if (kind is (byte)'{' or (byte)'[')
        {
            int depth = frames.Count;
            do
            {
                ReadSkippedToken();
            }
            while (frames.Count > depth);
            return;
        }

        if (kind is (byte)'}' or (byte)']')
        {
            EnsureCurrent();
            throw new JsonException("unexpected JSON closing delimiter while reading value");
        }

        ReadSkippedToken();
    }

    private void ReadSkippedToken()
    {
        byte kind = PeekKind();
        if (frames.Count != 0 && CurrentFrame.Object && !CurrentFrame.AwaitingValue && kind == '"')
        {
            ReadPropertyName();
            return;
        }

        switch (kind)
        {
            case (byte)'{':
            case (byte)'[':
            case (byte)'}':
            case (byte)']':
                ReadToken(kind);
                break;
            case (byte)'"':
                EnsureCurrent();
                ValidateStringToken();
                CommitValue();
                break;
            case (byte)'0':
            case (byte)'t':
            case (byte)'f':
            case (byte)'n':
                EnsureCurrent();
                CommitValue();
                break;
            default:
                // EnsureCurrent routes malformed lexical input through the
                // accepted Go syntactic error producer before EOF semantics.
                EnsureCurrent();
                throw new JsonException("unexpected EOF");
        }
    }

    // json/v2 arshal_default.go:1894 preserves an existing any's dynamic
    // type; arshal_any.go constructs its default type only when the any is nil.
    internal object? ReadAny()
    {
        object? value = null;
        ReadAny(ref value);
        return value;
    }

    internal void ReadAny(ref object? value)
    {
        byte kind = PeekKind();
        if (kind == 'n')
        {
            ReadToken((byte)'n');
            value = null;
            return;
        }

        if (value is null)
        {
            // The optimized nil-any path returns nil on a lexical failure.
            // Do not install an empty string/bool before validating its value.
            if (kind == '"')
            {
                value = ReadString().Materialize();
                return;
            }

            if (kind is (byte)'t' or (byte)'f')
            {
                value = ReadBoolean();
                return;
            }

            EnsureCurrent();
            value = kind switch
            {
                (byte)'t' or (byte)'f' => false,
                (byte)'"' => "",
                (byte)'0' => 0d,
                (byte)'{' => new Dictionary<string, object?>(StringComparer.Ordinal),
                (byte)'[' => new List<object?>(),
                _ => null
            };
        }

        switch (value)
        {
            case bool:
                value = ReadBoolean();
                return;
            case string:
                value = ReadString().Materialize();
                return;
            case double number:
                try
                {
                    ReadFloat64(ref number);
                }
                finally
                {
                    value = number;
                }

                return;
            case TypeScript.JsonRpc.RawJsonValue:
                value = ReadValue().Clone();
                return;
            case Dictionary<string, object?> map:
                ReadObjectStart("map[string]interface {}");
                while (PeekKind() != '}')
                {
                    var key = ReadDecodedPropertyName().Materialize()!;
                    object? element = null;
                    try
                    {
                        ReadAny(ref element);
                    }
                    finally
                    {
                        map[key] = element;
                    }
                }

                ReadToken((byte)'}');
                return;
            case List<object?> array:
                ReadArrayStart("[]interface {}");
                int count = 0;
                try
                {
                    while (PeekKind() != ']')
                    {
                        object? element = null;
                        int index = count++;
                        if (index < array.Count)
                            array[index] = null;
                        else
                            array.Add(null);
                        try
                        {
                            ReadAny(ref element);
                        }
                        finally
                        {
                            array[index] = element;
                        }
                    }

                    ReadToken((byte)']');
                }
                finally
                {
                    if (array.Count > count)
                        array.RemoveRange(count, array.Count - count);
                }

                return;
            default:
                // Closed typed payloads enter through their actual descriptor;
                // source-decoded any values above retain the default Go owners.
                if (value is IProtocolReadable protocol)
                {
                    protocol.ReadProtocolJSON(ref this);
                    return;
                }

                if (ProtocolCodec.TryReadIntrinsicProtocolValue(ref this, ref value))
                    return;
                EnsureCurrent();
                throw new InvalidOperationException("unregistered dynamic protocol JSON owner");
        }
    }

    internal readonly record struct Mark(int Depth, int Length);
    private int CurrentLength => frames.Count == 0 ? rootCount : CurrentFrame.Object ? CurrentFrame.Count * 2 + (CurrentFrame.AwaitingValue ? 1 : 0) : CurrentFrame.Count;

    internal Mark Capture() => new(frames.Count + 1, CurrentLength);
    private int NextOffset()
    {
        int pos = checked((int)reader.BytesConsumed);
        while (pos < input.Length && input[pos] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            pos++;
        if (pos < input.Length && input[pos] is (byte)',' or (byte)':')
        {
            pos++;
            while (pos < input.Length && input[pos] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                pos++;
        }

        return pos;
    }

    private ByteStringValue StackPointer(int where)
    {
        var result = new GoBuilder();
        // Go state.appendStackPointer excludes its virtual root array and walks
        // the concrete value-frame slice from outer to inner.
        for (int i = 0; i < frames.Count; i++)
        {
            var entry = frames[i];
            int length = entry.Object ? entry.Count * 2 + (entry.AwaitingValue ? 1 : 0) : entry.Count;
            bool needsName = entry.Object && !entry.AwaitingValue;
            if (i == frames.Count - 1 && ((where < 0 && length == 0) || (where == 0 && !entry.AwaitingValue) || (where > 0 && needsName)))
                break;
            result.WriteByte((byte)'/');
            if (entry.Object)
            {
                foreach (byte value in entry.Name.ByteSpan)
                {
                    if (value == '~')
                        result.WriteString("~0");
                    else if (value == '/')
                        result.WriteString("~1");
                    else
                        result.WriteByte(value);
                }
            }
            else
            {
                int delta = i == frames.Count - 1 && where > 0 ? 0 : -1;
                result.WriteString((entry.Count + delta).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return result.Value();
    }

    // encoding/json/v2 newSemanticErrorWithPosition. Syntactic failures are
    // already returned as the accepted JsonTextErrors FormatException boundary.
    internal Exception Enrich(string goType, Mark previous, Exception error)
    {
        if (error is not JsonException)
            return error;
        var semantic = error as ProtocolSemanticError ?? new ProtocolSemanticError
        {
            Cause = GoString.ToUtf8(error.Message)
        };
        int depth = frames.Count + 1;
        int length = CurrentLength;
        if (semantic.ByteOffset == 0)
            semantic.ByteOffset = (previous.Depth == depth && previous.Length == length) || previousEnd == 0 ? NextOffset() : previousStart;
        if (semantic.JsonPointer.Length == 0)
        {
            int where = previous.Depth == depth && previous.Length == length ? 1 : previous.Depth == depth && previous.Length + 1 == length ? -1 : 0;
            semantic.JsonPointer = StackPointer(where);
        }

        if (semantic.GoType.Length == 0)
            semantic.GoType = goType;
        return semantic;
    }

    internal ProtocolSemanticError ErrorAfter(string goType, string? cause = null, bool includeValue = false)
    {
        byte kind = input[previousStart] is >= (byte)'0' and <= (byte)'9' or (byte)'-' ? (byte)'0' : input[previousStart];
        return new ProtocolSemanticError
        {
            ByteOffset = previousStart,
            JsonPointer = StackPointer(-1),
            JsonKind = kind,
            GoType = goType,
            Cause = cause,
            JsonValue = includeValue && (kind is (byte)'0' or (byte)'"') ? ByteStringValue.FromView(ByteStringView.CopyFrom(input.Slice(previousStart, previousEnd - previousStart))) : default
        };
    }

    internal void Finish()
    {
        if (frames.Count != 0)
            throw new JsonException("unexpected end of JSON input");
        // Read validates trailing bytes even when PeekKind cannot recognize them.
        // Preserve jsontext's syntactic error producer on this final boundary,
        // just as EnsureCurrent does during the actual typed decode.
        try
        {
            if (reader.Read())
                throw new JsonException("unexpected data after top-level JSON value");
        }
        catch (JsonException)
        {
            var error = JsonCodec.ValidateValue(new JsonValue(input.ToArray()));
            if (error is not null)
                throw error;
            throw;
        }
    }

    internal ProtocolRawValue ReadValue()
    {
        EnsureCurrent();
        int start = checked((int)reader.TokenStartIndex);
        SkipValue();
        previousStart = start;
        previousEnd = checked((int)reader.BytesConsumed);
        return new ProtocolRawValue(inputOwner.Slice(start, checked((int)reader.BytesConsumed)));
    }
}

// jsontext.Decoder.ReadValue borrows the decoder input. Only explicitly retained
// deferred/discriminator fields call Clone, as their original Go owners do.
internal readonly struct ProtocolRawValue(ByteStringValue text)
{
    internal ByteStringValue TextValue => text;

    internal TypeScript.JsonRpc.RawJsonValue Clone() => new(text.ByteSpan);
}
