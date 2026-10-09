// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/packagejson/jsonvalue.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;

namespace TypeScript.PackageJson;
public enum JSONValueType : sbyte
{
    NotPresent,
    Null,
    String,
    Number,
    Boolean,
    Array,
    Object
}

public static class JSONValueTypeOperations
{
    public static string String(this JSONValueType type) => type switch
    {
        JSONValueType.Null => "null",
        JSONValueType.String => "string",
        JSONValueType.Number => "number",
        JSONValueType.Boolean => "boolean",
        JSONValueType.Array => "array",
        JSONValueType.Object => "object",
        _ => "unknown(" + (sbyte)type + ")"
    };
}

public struct JSONValue
{
    public JSONValueType Type;
    public object? Value;
    public readonly bool IsPresent() => Type != JSONValueType.NotPresent;
    public readonly bool IsFalsy() => Type switch
    {
        JSONValueType.NotPresent or JSONValueType.Null => true,
        JSONValueType.String => (string)Value! == "",
        // The original compares an interface value with the Go int constant 0.
        // JSON decoding stores float64, whose dynamic type differs even at +/-0.
        JSONValueType.Number => Value is int integer && integer == 0,
        JSONValueType.Boolean => !(bool)Value!,
        _ => false
    };
    public readonly OrderedMap<string, JSONValue> AsObject() => Type == JSONValueType.Object ? (OrderedMap<string, JSONValue>)Value! : throw new InvalidOperationException("expected object, got " + Type.String());
    public readonly List<JSONValue>? AsArray() => Type == JSONValueType.Array ? (List<JSONValue>? )Value : throw new InvalidOperationException("expected array, got " + Type.String());
    public readonly string AsString() => Type == JSONValueType.String ? (string)Value! : throw new InvalidOperationException("expected string, got " + Type.String());
    public static JSONValue FromRaw(object? raw)
    {
        if (raw == null)
            return new()
            {
                Type = JSONValueType.Null
            };
        if (raw is string text)
            return new()
            {
                Type = JSONValueType.String,
                Value = text
            };
        if (raw is bool boolean)
            return new()
            {
                Type = JSONValueType.Boolean,
                Value = boolean
            };
        if (raw is List<object?> array)
            return new()
            {
                Type = JSONValueType.Array,
                Value = array.Count == 0 ? null : array.Select(FromRaw).ToList()
            };
        if (raw is OrderedMap<string, object?> map)
        {
            var result = new OrderedMap<string, JSONValue>();
            foreach (var(key, value)in map.Entries())
                result.Set(key, FromRaw(value));
            return new()
            {
                Type = JSONValueType.Object,
                Value = result
            };
        }

        if (raw is double number)
            return new()
            {
                Type = JSONValueType.Number,
                Value = number
            };
        throw new InvalidOperationException("Unknown raw JSON value.");
    }
}
