// Copyright (c) Microsoft Corporation and The Go Authors. All rights reserved.
// TypeScript Apache license and Go BSD license in licenses/.
using TypeScript.Scanner;
using TypeScript.Json.Internal;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Json;
public static partial class JsonCodec
{
    // internal/json/json.go MarshalIndent; prefix/indent are raw Go UTF8 strings.
    internal static (ByteStringValue Output, Exception? Error) MarshalIndentValue(object? value, ByteStringValue prefix, ByteStringValue indent)
    {
        // Original option constructors panic before Marshal starts.
        var formatting = new JsonIndentation(prefix, indent);
        var encoder = new ProtocolEncoder(formatting);
        try
        {
            if (value is IProtocolWritable protocol)
            {
                protocol.WriteProtocolJSON(encoder);
                return (encoder.Finish(), null);
            }

            if (value is Dictionary<string, object?> or List<object?>)
            {
                encoder.WriteAny(value);
                return (encoder.Finish(), null);
            }

            if (value is not null && ProtocolCodec.TryWriteIntrinsicProtocolValue(encoder, value))
                return (encoder.Finish(), null);
        }
        catch (Exception error)when (error is InvalidOperationException or NotSupportedException or System.Text.Json.JsonException or FormatException)
        {
            return (encoder.PartialOutput(), error);
        }

        var output = new GoBuilder();
        try
        {
            writeValue(output, value, indentation: formatting);
            return (output.Value(), null);
        }
        catch (Exception error)when (error is InvalidOperationException or NotSupportedException or System.Text.Json.JsonException or FormatException)
        {
            return (output.Value(), error);
        }
    }

    // Continue the already-supported generic owner in the same output buffer.
    // The enclosing delimiter is admitted only at its first successful token;
    // semantic failures retain nested partial bytes and absolute JSON pointers.
    internal static void WriteGenericProtocolValue(GoBuilder output, object? value, JsonIndentation formatting, int depth, ByteStringValue pointer, ProtocolEncoder encoder) => writeValue(output, value, indentation: formatting, initialDepth: depth, initialPointer: pointer.Materialize() ?? "", enclosingEncoder: encoder);
    // jsontext/encode.go reformatValue whitespace path. Input is an already
    // validated/quoted owned payload from the existing serializer, or its
    // retained successfully emitted prefix on a semantic failure. Token bytes
    // and names remain unchanged; no parse/object reconstruction occurs.
    internal static void WriteIndentedRaw(GoBuilder output, ReadOnlySpan<byte> input, JsonIndentation formatting, int depth)
    {
        if (!formatting.Multiline)
        {
            output.WriteString(input);
            return;
        }

        int previous = 0;
        for (int offset = 0; offset < input.Length;)
        {
            byte next = input[offset];
            if (next is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                offset++;
                continue;
            }

            bool end = next is (byte)'}' or (byte)']';
            if (end)
            {
                depth--;
                if (previous is not ('{' or '['))
                    formatting.AppendIndent(output, depth + 1);
            }
            else if (previous is '{' or '[')
                formatting.AppendIndent(output, depth + 1);
            if (next == '"')
            {
                int start = offset++;
                while (offset < input.Length)
                {
                    byte item = input[offset++];
                    if (item == '\\' && offset < input.Length)
                        offset++;
                    else if (item == '"')
                        break;
                }

                output.WriteString(input[start..offset]);
            }
            else
            {
                output.WriteByte(next);
                offset++;
                if (next is (byte)'{' or (byte)'[')
                    depth++;
                else if (next == ',')
                    formatting.AppendIndent(output, depth + 1);
                else if (next == ':')
                    output.WriteByte(' ');
            }

            previous = next;
        }
    }
}
