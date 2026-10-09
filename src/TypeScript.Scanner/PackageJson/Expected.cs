// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/packagejson/expected.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.PackageJson;
public struct Expected<T> : TypeValidatedField
{
    private string? actualJSONType;
    private T? value;
    public bool Null;
    public bool Valid;
    public T Value { readonly get => value is null && typeof(T) == typeof(string) ? (T)(object)"" : value!; set => this.value = value; }

    public readonly bool IsPresent() => actualJSONType is { Length: > 0 };
    public readonly (T Value, bool Ok) GetValue() => (Value, Valid);
    public readonly bool IsValid() => Valid;
    public readonly string ActualJSONType() => actualJSONType ?? "";
    public readonly string ExpectedJSONType()
    {
        Type type = typeof(T);
        if (type == typeof(string))
            return "string";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            return "number";
        if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return "array";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<, >))
            return "object";
        return "unknown";
    }

    public static Expected<T> Of(T value) => new()
    {
        Value = value,
        Valid = true,
        actualJSONType = default(Expected<T>).ExpectedJSONType()
    };
    // The static typed decoder is supplied by the package fields adapter;
    // it retains partial values on wrong-type decode, like json.Unmarshal.
    public static Expected<T> FromRaw(object? raw, Func<object, (T Value, bool Valid)> decode)
    {
        if (raw == null)
            return new()
            {
                Null = true,
                actualJSONType = "null"
            };
        var(value, valid) = decode(raw);
        return new()
        {
            Value = value,
            Valid = valid,
            actualJSONType = RawJSONType(raw)
        };
    }

    internal static string RawJSONType(object raw) => raw switch
    {
        string => "string",
        bool => "boolean",
        List<object?> => "array",
        TypeScript.Collections.OrderedMap<string, object?> => "object",
        _ => "number"
    };
    // UnmarshalJSON decodes into the existing Value and only sets Valid on
    // success. A subsequent non-null mismatch does not reset Null or Valid.
    internal void ApplyDecodedValue(string actualType, T decodedValue, bool valid)
    {
        Value = decodedValue;
        if (valid)
            Valid = true;
        actualJSONType = actualType;
    }

    internal void ApplyNull() => this = new()
    {
        Null = true,
        actualJSONType = "null"
    };
}
