// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed protocol encoder storage adapter; Go JSON field/union writers remain
// source-defined; standard .NET byte storage retains the Go output owner.
using System.Globalization;
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal interface IProtocolReadable
{
    void ReadProtocolJSON(ref ProtocolDecoder decoder);
}

internal interface IProtocolWritable
{
    void WriteProtocolJSON(ProtocolEncoder encoder);
}

internal sealed class ProtocolEncoder
{
    private struct Frame(bool isObject, bool checkNames = false)
    {
        internal readonly bool Object = isObject;
        internal HashSet<ByteStringKey>? Names = checkNames ? new(ByteStringKey.Comparer) : null;
        internal int Count;
        internal bool AwaitingValue;
        internal int NameStart;
        internal int NameEnd;
    }

    private readonly GoBuilder output = new();
    private readonly JsonIndentation indentation;
    internal ProtocolEncoder(JsonIndentation indentation = default) => this.indentation = indentation;
    internal ByteStringValue PartialOutput() => output.Value();
    private readonly List<Frame> frames = new();
    private bool rootWritten;
    // json/v2 arshal_default.go starts pointer-cycle tracking only beyond
    // 1000 JSON token frames. Keep the map lazy and retain owners until leave.
    private readonly struct CycleKey(object owner, int offset, int length, string goType) : IEquatable<CycleKey>
    {
        private readonly object owner = owner;
        private readonly int offset = offset;
        private readonly int length = length;
        private readonly string goType = goType;
        public bool Equals(CycleKey other) => ReferenceEquals(owner, other.owner) && offset == other.offset && length == other.length && goType == other.goType;
        public override bool Equals(object? value) => value is CycleKey key && Equals(key);
        public override int GetHashCode() => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner), offset, length, goType);
    }

    private HashSet<CycleKey>? seenPointers;
    internal bool VisitPointer(object owner, string goType, int offset = 0, int length = 0)
    {
        if (frames.Count <= 1000)
            return false;
        seenPointers ??= new HashSet<CycleKey>();
        if (!seenPointers.Add(new(owner, offset, length, goType)))
            throw TypeScript.Json.Internal.JsonTextErrors.AsException(TypeScript.Json.Internal.JsonTextErrors.SemanticError("marshal", "", "", goType, StackPointer().Materialize() ?? "", 0, "encountered a cycle"));
        return true;
    }

    internal void LeavePointer(object owner, bool visited, string goType, int offset = 0, int length = 0)
    {
        if (visited)
            seenPointers!.Remove(new(owner, offset, length, goType));
    }

    internal bool VisitSlice<T>(Slice<T> value, string goType)
    {
        if (frames.Count <= 1000 || value.IsNil)
            return false;
        if (!System.Runtime.InteropServices.MemoryMarshal.TryGetArray(value.AsMemory(), out var array))
            throw new InvalidOperationException("Protocol slice requires an immutable array owner");
        return VisitPointer(array.Array!, goType, array.Offset, value.Length);
    }

    internal void LeaveSlice<T>(Slice<T> value, bool visited, string goType)
    {
        if (!visited)
            return;
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(value.AsMemory(), out var array);
        LeavePointer(array.Array!, true, goType, array.Offset, value.Length);
    }

    private ByteStringValue StackPointer(bool omitCurrentObjectName = false)
    {
        var pointer = new GoBuilder();
        var text = output.Value();
        for (int index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (omitCurrentObjectName && index == frames.Count - 1 && frame.Object)
                continue;
            pointer.WriteByte('/');
            if (!frame.Object)
            {
                int item = index == frames.Count - 1 ? frame.Count : frame.Count - 1;
                pointer.WriteString(item.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            var name = TypeScript.JsonRpc.RpcJson.ReadString(text.Slice(frame.NameStart, frame.NameEnd).ByteSpan);
            foreach (byte element in name.ByteSpan)
            {
                if (element == '~')
                    pointer.WriteString("~0");
                else if (element == '/')
                    pointer.WriteString("~1");
                else
                    pointer.WriteByte(element);
            }
        }

        return pointer.Value();
    }

    internal void BeforeGenericValue() => BeforeValue();
    private void BeforeValue()
    {
        if (frames.Count == 0)
        {
            if (rootWritten)
                throw new InvalidOperationException("multiple JSON values in protocol marshal");
            rootWritten = true;
            return;
        }

        int index = frames.Count - 1;
        var frame = frames[index];
        if (frame.Object)
        {
            if (!frame.AwaitingValue)
                throw new InvalidOperationException("object value without property name");
            output.WriteByte(':');
            if (indentation.Multiline)
                output.WriteByte(' ');
            frame.AwaitingValue = false;
        }
        else
        {
            if (frame.Count != 0)
                output.WriteByte((byte)',');
            indentation.AppendIndent(output, frames.Count + 1);
        }

        frame.Count++;
        frames[index] = frame;
    }

    // jsontext/state.go pushObject/pushArray: reject the next container at
    // 10,000 open frames. WriteToken computes its prospective token position,
    // but commits neither the implicit delimiter nor the failed token.
    private void CheckContainerDepth()
    {
        if (frames.Count != 10000)
            return;
        var frame = frames[^1];
        int delimiterLength = frame.Object ? 1 + (indentation.Multiline ? 1 : 0)
            : (frame.Count == 0 ? 0 : 1) + indentation.Length(frames.Count + 1);
        throw TypeScript.Json.Internal.JsonTextErrors.AsException(
            TypeScript.Json.Internal.JsonTextErrors.SyntacticError("exceeded max depth", StackPointer().Materialize()!, output.Len() + delimiterLength));
    }

    internal void BeginObject(bool checkNames = false)
    {
        CheckContainerDepth();
        BeforeValue();
        output.WriteByte((byte)'{');
        frames.Add(new(true, checkNames));
    }

    internal void BeginArray()
    {
        CheckContainerDepth();
        BeforeValue();
        output.WriteByte((byte)'[');
        frames.Add(new(false));
    }

    internal void EndObject()
    {
        var frame = frames[^1];
        if (!frame.Object || frame.AwaitingValue)
            throw new InvalidOperationException("invalid object close");
        frames.RemoveAt(frames.Count - 1);
        if (frame.Count != 0)
            indentation.AppendIndent(output, frames.Count + 1);
        output.WriteByte((byte)'}');
    }

    internal void EndArray()
    {
        var frame = frames[^1];
        if (frame.Object)
            throw new InvalidOperationException("invalid array close");
        frames.RemoveAt(frames.Count - 1);
        if (frame.Count != 0)
            indentation.AppendIndent(output, frames.Count + 1);
        output.WriteByte((byte)']');
    }

    // Generated field names are source literals containing only ASCII letters,
    // digits and underscore. This overload writes that already-proven name
    // directly, while dynamic map names use the full quote worker below.
    internal void WriteProperty(ReadOnlySpan<byte> name)
    {
        int index = frames.Count - 1;
        var frame = frames[index];
        if (!frame.Object || frame.AwaitingValue)
            throw new InvalidOperationException("invalid object property position");
        if (frame.Count != 0)
            output.WriteByte((byte)',');
        indentation.AppendIndent(output, frames.Count + 1);
        frame.NameStart = output.Len();
        output.WriteByte((byte)'"');
        output.WriteString(name);
        output.WriteByte((byte)'"');
        frame.NameEnd = output.Len();
        // jsontext emits the implicit colon only when the next value token
        // succeeds; a semantic failure retains the member name alone.
        frame.AwaitingValue = true;
        frames[index] = frame;
    }

    internal void WriteProperty(ByteStringValue name)
    {
        int index = frames.Count - 1;
        var frame = frames[index];
        if (!frame.Object || frame.AwaitingValue)
            throw new InvalidOperationException("invalid object property position");
        if (frame.Count != 0)
            output.WriteByte((byte)',');
        indentation.AppendIndent(output, frames.Count + 1);
        frame.NameStart = output.Len();
        JsonCodec.QuoteToBuilder(output, name);
        if (frame.Names is not null)
        {
            // json/v2 keeps map namespaces when invalid UTF8 replacement could
            // make distinct string keys encode the same JSON object name.
            // Valid keys retain their existing borrowed header; only invalid
            // keys need the required replacement-name owner.
            var normalized = name;
            for (int position = 0; position < name.Length;)
            {
                var(rune, size) = name.IsBytes ? Utf8.DecodeRuneInString(name.ByteSpan.Slice(position)) : Utf8.DecodeRuneInString(name.CharSpan.Slice(position));
                if (rune == 0xfffd && size == 1)
                {
                    normalized = TypeScript.JsonRpc.RpcJson.ReadString(output.Value().Slice(frame.NameStart, output.Len()).ByteSpan);
                    break;
                }

                position += size;
            }

            if (!frame.Names.Add(ByteStringKey.FromValue(normalized)))
                throw TypeScript.Json.Internal.JsonTextErrors.AsException(TypeScript.Json.Internal.JsonTextErrors.SyntacticError("duplicate object member name", StackPointer(omitCurrentObjectName: true).Materialize() ?? "", 0, normalized.Materialize()));
        }

        frame.NameEnd = output.Len();
        // jsontext emits the implicit colon only when the next value token
        // succeeds; a semantic failure retains the member name alone.
        frame.AwaitingValue = true;
        frames[index] = frame;
    }

    internal void WriteString(ByteStringValue value)
    {
        BeforeValue();
        JsonCodec.QuoteToBuilder(output, value);
    }

    internal void WriteNull()
    {
        BeforeValue();
        output.WriteString("null");
    }

    internal void WriteBoolean(bool value)
    {
        BeforeValue();
        output.WriteString(value ? "true" : "false");
    }

    internal void WriteNumber(int value)
    {
        BeforeValue();
        output.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }

    internal void WriteNumber(uint value)
    {
        BeforeValue();
        output.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }

    internal void WriteNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            // json/v2 rejects the scalar before emitting the implicit delimiter.
            string label = double.IsNaN(value) ? "NaN" : value > 0 ? "+Inf" : "-Inf";
            throw TypeScript.Json.Internal.JsonTextErrors.AsException(TypeScript.Json.Internal.JsonTextErrors.SemanticError("marshal", "", "", "float64", StackPointer().Materialize() ?? "", 0, "unsupported value: " + label));
        }

        BeforeValue();
        output.WriteString(value == 0 && BitConverter.DoubleToInt64Bits(value) < 0 ? "-0" : new JsNum.Number(value).String());
    }

    internal void WriteRaw(ReadOnlySpan<byte> value)
    {
        BeforeValue();
        JsonCodec.WriteIndentedRaw(output, value, indentation, frames.Count);
    }

    internal void WriteAny(object? value)
    {
        // Preserve real dynamic scalar roles without an intermediate output
        // buffer; errors retain the enclosing encoder's original JSON pointer.
        if (value is double number)
        {
            WriteNumber(number);
            return;
        }

        if (value is ByteStringValue bytesValue)
        {
            WriteString(bytesValue);
            return;
        }

        if (value is string text)
        {
            WriteString(ByteStringValue.FromString(text));
            return;
        }

        if (value is IProtocolWritable protocol)
        {
            protocol.WriteProtocolJSON(this);
            return;
        }

        if (value is not null && ProtocolCodec.TryWriteIntrinsicProtocolValue(this, value))
            return;
        if (value is Dictionary<string, object?> map)
        {
            bool visited = VisitPointer(map, "map[string]interface {}");
            try
            {
                BeginObject(checkNames: true);
                foreach (var entry in map)
                {
                    WriteProperty(ByteStringValue.FromString(entry.Key));
                    WriteAny(entry.Value);
                }

                EndObject();
            }
            finally
            {
                LeavePointer(map, visited, "map[string]interface {}");
            }

            return;
        }

        if (value is List<object?> array)
        {
            bool visited = VisitPointer(array, "[]interface {}", length: array.Count);
            try
            {
                BeginArray();
                foreach (var element in array)
                    WriteAny(element);
                EndArray();
            }
            finally
            {
                LeavePointer(array, visited, "[]interface {}", length: array.Count);
            }

            return;
        }

        // json.Value's encoder compacts and validates raw retained payloads;
        // it does not emit their original whitespace directly.
        if (value is TypeScript.JsonRpc.RawJsonValue raw)
        {
            if (raw.IsNil)
            {
                WriteNull();
                return;
            }

            value = new JsonValue(raw.BorrowOwnedBytes());
        }

        JsonCodec.WriteGenericProtocolValue(output, value, indentation, frames.Count, StackPointer(), this);
    }

    internal ByteStringValue Finish()
    {
        if (!rootWritten || frames.Count != 0)
            throw new InvalidOperationException("incomplete protocol JSON value");
        return output.Value();
    }
}
