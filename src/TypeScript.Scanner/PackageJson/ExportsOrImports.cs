// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/packagejson/exportsorimports.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;

namespace TypeScript.PackageJson;
public struct ExportsOrImports
{
    private enum ObjectKind : sbyte
    {
        Unknown,
        Subpaths,
        Conditions,
        Imports,
        Invalid
    }

    public JSONValueType Type;
    public object? Value;
    private ObjectKind objectKind;
    public readonly bool IsPresent() => Type != JSONValueType.NotPresent;
    public readonly bool IsFalsy() => new JSONValue
    {
        Type = Type,
        Value = Value
    }.IsFalsy();
    public readonly OrderedMap<string, ExportsOrImports> AsObject() => Type == JSONValueType.Object ? (OrderedMap<string, ExportsOrImports>)Value! : throw new InvalidOperationException("expected object");
    public readonly List<ExportsOrImports>? AsArray() => Type == JSONValueType.Array ? (List<ExportsOrImports>? )Value : throw new InvalidOperationException("expected array");
    public readonly bool IsSubpaths()
    {
        var copy = this;
        copy.initObjectKind();
        return copy.objectKind == ObjectKind.Subpaths;
    }

    public readonly bool IsImports()
    {
        var copy = this;
        copy.initObjectKind();
        return copy.objectKind == ObjectKind.Imports;
    }

    public readonly bool IsConditions()
    {
        var copy = this;
        copy.initObjectKind();
        return copy.objectKind == ObjectKind.Conditions;
    }

    private void initObjectKind()
    {
        if (objectKind != ObjectKind.Unknown || Type != JSONValueType.Object)
            return;
        var map = AsObject();
        if (map.Size() > 0)
        {
            bool dot = false, hash = false, other = false;
            foreach (string key in map.Keys())
            {
                if (key.Length == 0)
                    continue;
                dot |= key[0] == '.';
                hash |= key[0] == '#';
                other |= key[0] != '.' && key[0] != '#';
                if (other && (dot || hash))
                {
                    objectKind = ObjectKind.Invalid;
                    return;
                }
            }

            if (dot)
            {
                objectKind = ObjectKind.Subpaths;
                return;
            }

            if (hash)
            {
                objectKind = ObjectKind.Imports;
                return;
            }
        }

        objectKind = ObjectKind.Conditions;
    }

    public static ExportsOrImports FromRaw(object? raw)
    {
        if (raw is OrderedMap<string, object?> map)
        {
            var result = new OrderedMap<string, ExportsOrImports>();
            foreach (var(key, item)in map.Entries())
                result.Set(key, FromRaw(item));
            return new()
            {
                Type = JSONValueType.Object,
                Value = result
            };
        }

        if (raw is List<object?> array)
            return new()
            {
                Type = JSONValueType.Array,
                Value = array.Count == 0 ? null : array.Select(FromRaw).ToList()
            };
        var value = JSONValue.FromRaw(raw);
        return new()
        {
            Type = value.Type,
            Value = value.Value
        };
    }
}
