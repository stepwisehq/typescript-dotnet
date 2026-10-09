// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/packagejson/packagejson.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Static typed decoding uses the
// shared raw JSON adapter; no arbitrary reflection-based struct codec is exposed.
using TypeScript.Collections;
using TypeScript.Json;

namespace TypeScript.PackageJson;
public struct HeaderFields
{
    public Expected<string> Name;
    public Expected<string> Version;
    public Expected<string> Type;
}

public struct PathFields
{
    public Expected<string> TSConfig;
    public Expected<string> Main;
    public Expected<string> Types;
    public Expected<string> Typings;
    public JSONValue TypesVersions;
    public ExportsOrImports Imports;
    public ExportsOrImports Exports;
}

public struct DependencyFields
{
    public Expected<Dictionary<string, string>?> Dependencies;
    public Expected<Dictionary<string, string>?> DevDependencies;
    public Expected<Dictionary<string, string>?> PeerDependencies;
    public Expected<Dictionary<string, string>?> OptionalDependencies;
    public readonly bool HasDependency(string name)
    {
        foreach (var field in new[]
        {
            Dependencies,
            DevDependencies,
            PeerDependencies,
            OptionalDependencies
        }

        )
            if (field.Valid && field.Value?.ContainsKey(name) == true)
                return true;
        return false;
    }

    public readonly void RangeDependencies(Func<string, string, string, bool> callback)
    {
        foreach (var(field, name)in new[]
        {
            (Dependencies, "dependencies"),
            (DevDependencies, "devDependencies"),
            (PeerDependencies, "peerDependencies"),
            (OptionalDependencies, "optionalDependencies")
        }

        )
            if (field.Valid && field.Value != null)
                foreach (var(key, version)in field.Value)
                    if (!callback(key, version, name))
                        return;
    }

    public readonly HashSet<string> GetRuntimeDependencyNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in new[]
        {
            Dependencies,
            PeerDependencies,
            OptionalDependencies
        }

        )
            if (field.Value != null)
                foreach (string name in field.Value.Keys)
                    names.Add(name);
        return names;
    }
}

public struct ContentMapperFields
{
    public Expected<List<string>?> Exec;
    public Expected<List<string>?> CompilerOptions;
    public Expected<bool> DynamicConfig;
}

public struct Fields
{
    public HeaderFields HeaderFields;
    public PathFields PathFields;
    public DependencyFields DependencyFields;
    public Expected<ContentMapperFields> ContentMapper;
    public Expected<string> Name { readonly get => HeaderFields.Name; set => HeaderFields.Name = value; }
    public Expected<string> Version { readonly get => HeaderFields.Version; set => HeaderFields.Version = value; }
    public Expected<string> Type { readonly get => HeaderFields.Type; set => HeaderFields.Type = value; }
    public Expected<string> TSConfig { readonly get => PathFields.TSConfig; set => PathFields.TSConfig = value; }
    public Expected<string> Main { readonly get => PathFields.Main; set => PathFields.Main = value; }
    public Expected<string> Types { readonly get => PathFields.Types; set => PathFields.Types = value; }
    public Expected<string> Typings { readonly get => PathFields.Typings; set => PathFields.Typings = value; }
    public JSONValue TypesVersions { readonly get => PathFields.TypesVersions; set => PathFields.TypesVersions = value; }
    public ExportsOrImports Imports { readonly get => PathFields.Imports; set => PathFields.Imports = value; }
    public ExportsOrImports Exports { readonly get => PathFields.Exports; set => PathFields.Exports = value; }
    public Expected<Dictionary<string, string>?> Dependencies { readonly get => DependencyFields.Dependencies; set => DependencyFields.Dependencies = value; }
    public Expected<Dictionary<string, string>?> DevDependencies { readonly get => DependencyFields.DevDependencies; set => DependencyFields.DevDependencies = value; }
    public Expected<Dictionary<string, string>?> PeerDependencies { readonly get => DependencyFields.PeerDependencies; set => DependencyFields.PeerDependencies = value; }
    public Expected<Dictionary<string, string>?> OptionalDependencies { readonly get => DependencyFields.OptionalDependencies; set => DependencyFields.OptionalDependencies = value; }

    public readonly bool HasDependency(string name) => DependencyFields.HasDependency(name);
    public readonly void RangeDependencies(Func<string, string, string, bool> callback) => DependencyFields.RangeDependencies(callback);
    public readonly HashSet<string> GetRuntimeDependencyNames() => DependencyFields.GetRuntimeDependencyNames();
}

public static class PackageJsonParser
{
    private struct TypeScriptFields
    {
        internal Expected<ContentMapperFields> ContentMapper;
    }

    public static (Fields Fields, Exception? Error) ParseFields(byte[] bytes)
    {
        var(entries, isNull, error) = JsonCodec.ParseObjectFields(bytes);
        if (entries == null)
            return (default, error);
        Fields fields = default;
        Expected<TypeScriptFields> typeScript = default;
        foreach (var(key, token)in entries!)
        {
            // The outer decoder calls ReadValue before Expected.UnmarshalJSON.
            // Its duplicate policy applies to this lexical read; the callback
            // subsequently creates a fresh decoder with the default policy.
            if (key is "name" or "version" or "type" or "tsconfig" or "main" or
                "types" or "typings" or "dependencies" or "devDependencies" or
                "peerDependencies" or "optionalDependencies" or "typescript")
            {
                var readError = JsonCodec.ValidateValue(token, allowDuplicateNames: true);
                if (readError != null)
                    return (default, readError);
            }

            switch (key)
            {
                case "name":
                    decodeString(ref fields.HeaderFields.Name, token);
                    break;
                case "version":
                    decodeString(ref fields.HeaderFields.Version, token);
                    break;
                case "type":
                    decodeString(ref fields.HeaderFields.Type, token);
                    break;
                case "tsconfig":
                    decodeString(ref fields.PathFields.TSConfig, token);
                    break;
                case "main":
                    decodeString(ref fields.PathFields.Main, token);
                    break;
                case "types":
                    decodeString(ref fields.PathFields.Types, token);
                    break;
                case "typings":
                    decodeString(ref fields.PathFields.Typings, token);
                    break;
                case "dependencies":
                    decodeDependencies(ref fields.DependencyFields.Dependencies, token);
                    break;
                case "devDependencies":
                    decodeDependencies(ref fields.DependencyFields.DevDependencies, token);
                    break;
                case "peerDependencies":
                    decodeDependencies(ref fields.DependencyFields.PeerDependencies, token);
                    break;
                case "optionalDependencies":
                    decodeDependencies(ref fields.DependencyFields.OptionalDependencies, token);
                    break;
                case "typescript":
                    decodeTypeScript(ref typeScript, token);
                    break;
                case "typesVersions":
                case "imports":
                case "exports":
                    var(raw, rawError) = JsonCodec.Parse(token, allowDuplicateNames: true);
                    if (rawError != null)
                        return (default, rawError);
                    if (key == "typesVersions")
                        fields.TypesVersions = JSONValue.FromRaw(raw);
                    else if (key == "imports")
                        fields.Imports = ExportsOrImports.FromRaw(raw);
                    else
                        fields.Exports = ExportsOrImports.FromRaw(raw);
                    break;
            // The typed outer decoder skips unknown values after lexical
            // validation. In particular, it never converts their numbers.
            }
        }

        // Completed fields are decoded before a later lexical failure, just as
        // the streaming source decoder stops at the earliest reached error.
        if (error != null)
            return (default, error);
        fields.ContentMapper = typeScript.Value.ContentMapper;
        return (fields, null);
    }

    // Cache construction is a separate source boundary from packagejson.Parse.
    // Retained for the established Projects metadata host API.
    public static PackageJson Parse(byte[] bytes)
    {
        var(fields, error) = ParseFields(bytes);
        return new PackageJson
        {
            Fields = fields,
            Parseable = error == null
        };
    }

    private static string jsonType(JsonValue token) => token.Bytes![0] switch
    {
        (byte)'n' => "null",
        (byte)'"' => "string",
        (byte)'t' or (byte)'f' => "boolean",
        (byte)'[' => "array",
        (byte)'{' => "object",
        _ => "number"
    };
    private static void decodeString(ref Expected<string> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        string value = field.Value;
        bool valid = false;
        if (type == "string")
        {
            var(raw, error) = JsonCodec.Parse(token);
            if (error == null)
            {
                value = (string)raw!;
                valid = true;
            }
        }

        field.ApplyDecodedValue(type, value, valid);
    }

    private static void decodeBool(ref Expected<bool> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        bool valid = type == "boolean";
        field.ApplyDecodedValue(type, valid ? token.Bytes![0] == (byte)'t' : field.Value, valid);
    }

    private static void decodeDependencies(ref Expected<Dictionary<string, string>?> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        var value = field.Value;
        bool valid = false;
        if (type == "object")
        {
            var(entries, _, error) = JsonCodec.ParseObjectFields(token.Bytes!, token.SourceOffset, token.Pointer);
            value ??= new Dictionary<string, string>(StringComparer.Ordinal);
            valid = error == null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var(key, entry)in entries!)
            {
                // Expected.UnmarshalJSON invokes a fresh default decoder, whose
                // duplicate-name option is not inherited from the outer parser.
                if (!names.Add(key))
                {
                    valid = false;
                    break;
                }

                string entryType = jsonType(entry);
                if (entryType == "string")
                {
                    var(raw, rawError) = JsonCodec.Parse(entry);
                    if (rawError != null)
                    {
                        value[key] = "";
                        valid = false;
                        break;
                    }

                    value[key] = (string)raw!;
                }
                else
                {
                    // Typed map decoding publishes the zero value first, even
                    // when decoding that value fails and Expected swallows it.
                    value[key] = "";
                    if (entryType != "null")
                    {
                        valid = false;
                        break;
                    }
                }
            }
        }

        field.ApplyDecodedValue(type, value, valid);
    }

    private static void decodeStrings(ref Expected<List<string>?> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        var value = field.Value;
        bool valid = false;
        if (type == "array")
        {
            var(entries, _, error) = JsonCodec.ParseArrayValues(token.Bytes!, token.SourceOffset, token.Pointer);
            value ??= new List<string>();
            value.Clear();
            valid = error == null;
            foreach (var entry in entries!)
            {
                string entryType = jsonType(entry);
                if (entryType == "string")
                {
                    var(raw, rawError) = JsonCodec.Parse(entry);
                    value.Add(rawError == null ? (string)raw! : "");
                    if (rawError != null)
                    {
                        valid = false;
                        break;
                    }
                }
                else
                {
                    value.Add("");
                    if (entryType != "null")
                    {
                        valid = false;
                        break;
                    }
                }
            }
        }

        field.ApplyDecodedValue(type, value, valid);
    }

    private static void decodeContentMapper(ref Expected<ContentMapperFields> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        var value = field.Value;
        bool valid = false;
        if (type == "object")
        {
            var(entries, _, error) = JsonCodec.ParseObjectFields(token.Bytes!, token.SourceOffset, token.Pointer);
            valid = error == null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var(key, entry)in entries!)
            {
                if (!names.Add(key))
                {
                    valid = false;
                    break;
                }

                // The parent decoder reads a whole custom-unmarshal value
                // under its own duplicate policy before invoking Expected.
                if (JsonCodec.ValidateValue(entry) != null)
                {
                    valid = false;
                    break;
                }

                switch (key)
                {
                    case "exec":
                        decodeStrings(ref value.Exec, entry);
                        break;
                    case "compilerOptions":
                        decodeStrings(ref value.CompilerOptions, entry);
                        break;
                    case "dynamicConfig":
                        decodeBool(ref value.DynamicConfig, entry);
                        break;
                }
            }
        }

        field.ApplyDecodedValue(type, value, valid);
    }

    private static void decodeTypeScript(ref Expected<TypeScriptFields> field, JsonValue token)
    {
        string type = jsonType(token);
        if (type == "null")
        {
            field.ApplyNull();
            return;
        }

        var value = field.Value;
        bool valid = false;
        if (type == "object")
        {
            var(entries, _, error) = JsonCodec.ParseObjectFields(token.Bytes!, token.SourceOffset, token.Pointer);
            valid = error == null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var(key, entry)in entries!)
            {
                if (!names.Add(key))
                {
                    valid = false;
                    break;
                }

                if (JsonCodec.ValidateValue(entry) != null)
                {
                    valid = false;
                    break;
                }

                if (key == "contentMapper")
                    decodeContentMapper(ref value.ContentMapper, entry);
            }
        }

        field.ApplyDecodedValue(type, value, valid);
    }
}
