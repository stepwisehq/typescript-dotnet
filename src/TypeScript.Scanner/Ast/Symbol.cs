// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/symbol.go; binding and selected checker state.
#nullable disable
namespace TypeScript.Scanner;
public sealed class Symbol
{
    public SymbolFlags Flags;
    public CheckFlags CheckFlags;
    private ByteStringValue nameValue;
    // symbol.go Name is a Go string header. Public raw-string access projects
    // only at the requested .NET boundary; setters replace this symbol's header.
    public string Name { get => nameValue.Project(); set => nameValue = ByteStringValue.FromString(value); }
    internal ByteStringValue NameValue { get => nameValue.Snapshot(); set => nameValue = value.Snapshot(); }
    public Slice<Node> Declarations;
    public Node ValueDeclaration;
    public SymbolTable Members, Exports;
    internal long id;
    // Authored public ownership guard only; an opaque token never references the checker.
    internal object authoredOwnerToken;
    public Symbol Parent, ExportSymbol;
    public bool IsExternalModule() => (Flags & SymbolFlags.Module) != 0 && Ast.IsAmbientModuleSymbolName(NameValue);
    public bool IsStatic() => ValueDeclaration is not null && (ValueDeclaration.ModifierFlags() & ModifierFlags.Static) != 0;
    public SymbolFlags CombinedLocalAndExportSymbolFlags() => ExportSymbol is null ? Flags : Flags | ExportSymbol.Flags;
}

// Public key storage now exposes immutable Go-byte headers. Explicit string
// ingress/projection adapters preserve ordinary caller spelling and null-key
// errors; internal producers and consumers never materialize a key to insert it.
public sealed class SymbolTable : Dictionary<ByteStringKey, Symbol>
{
    public SymbolTable() : base(ByteStringKey.Comparer) { }
    public SymbolTable(int capacity) : base(capacity, ByteStringKey.Comparer) { }
    private static ByteStringKey StringKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return ByteStringKey.FromString(key);
    }
    public new Symbol this[ByteStringKey key]
    {
        get => TryGetValue(key, out var symbol) ? symbol : null;
        set => base[key] = value;
    }
    public Symbol this[string key]
    {
        get => TryGetValue(key, out var symbol) ? symbol : null;
        set => base[StringKey(key)] = value;
    }
    public new bool TryGetValue(ByteStringKey key, out Symbol symbol) => base.TryGetValue(key, out symbol);
    public new bool ContainsKey(ByteStringKey key) => base.ContainsKey(key);
    public new bool Remove(ByteStringKey key) => base.Remove(key);
    public new void Add(ByteStringKey key, Symbol symbol) => base.Add(key, symbol);
    public Symbol GetValueOrDefault(ByteStringKey key) => this[key];
    public Symbol GetValueOrDefault(string key) => this[key];
    internal Symbol GetValueOrDefault(ByteStringValue key) => this[key];
    public void Add(string key, Symbol symbol) => base.Add(StringKey(key), symbol);
    public bool Remove(string key) => base.Remove(StringKey(key));
    public bool ContainsKey(string key) => base.ContainsKey(StringKey(key));
    public bool TryGetValue(string key, out Symbol symbol) => base.TryGetValue(StringKey(key), out symbol);
    internal Symbol this[ByteStringValue key]
    {
        get => TryGetValue(key, out var symbol) ? symbol : null;
        set => base[ByteStringKey.FromValue(key)] = value;
    }
    internal bool TryGetValue(ByteStringValue key, out Symbol symbol) => key.TryGetValue(this, out symbol);
    internal bool TryGetValue(ByteStringValue key, out ByteStringValue actualKey, out Symbol symbol) =>
        key.TryGetValue(this, out actualKey, out symbol);
    internal bool ContainsKey(ByteStringValue key) => TryGetValue(key, out _);
    internal bool Remove(ByteStringValue key) => base.Remove(ByteStringKey.FromValue(key));
    internal void Add(ByteStringValue key, Symbol symbol) => base.Add(ByteStringKey.FromValue(key), symbol);
    public IEnumerable<KeyValuePair<string, Symbol>> GetStringEntries()
    {
        foreach (var entry in this)
            yield return new(entry.Key.ToString(), entry.Value);
    }
}

public static partial class Ast
{
    internal static bool IsAmbientModuleSymbolName(ByteStringValue name) =>
        TryGetAmbientModuleNameValueFromSymbolName(name).Item2;

    public const string InternalSymbolNamePrefix = "\u00FE";
    public const string InternalSymbolNameCall = InternalSymbolNamePrefix + "call";
    public const string InternalSymbolNameConstructor = InternalSymbolNamePrefix + "constructor";
    public const string InternalSymbolNameNew = InternalSymbolNamePrefix + "new";
    public const string InternalSymbolNameIndex = InternalSymbolNamePrefix + "index";
    public const string InternalSymbolNameExportStar = InternalSymbolNamePrefix + "export";
    public const string InternalSymbolNameGlobal = InternalSymbolNamePrefix + "global";
    public const string InternalSymbolNameMissing = InternalSymbolNamePrefix + "missing";
    public const string InternalSymbolNameType = InternalSymbolNamePrefix + "type";
    public const string InternalSymbolNameObject = InternalSymbolNamePrefix + "object";
    public const string InternalSymbolNameJSXAttributes = InternalSymbolNamePrefix + "jsxAttributes";
    public const string InternalSymbolNameClass = InternalSymbolNamePrefix + "class";
    public const string InternalSymbolNameFunction = InternalSymbolNamePrefix + "function";
    public const string InternalSymbolNameComputed = InternalSymbolNamePrefix + "computed";
    public const string InternalSymbolNameAssignmentDeclaration = InternalSymbolNamePrefix + "assignment";
    public const string InternalSymbolNameInstantiationExpression = InternalSymbolNamePrefix + "instantiationExpression";
    public const string InternalSymbolNameImportAttributes = InternalSymbolNamePrefix + "importAttributes";
    public const string InternalSymbolNameExportEquals = "export=";
    public const string InternalSymbolNameDefault = "default";
    public const string InternalSymbolNameThis = "this";
    public const string InternalSymbolNameModuleExports = "module.exports";
    public static SourceFile GetSourceFileOfSymbol(Symbol symbol)
    {
        Debug.Assert(symbol is not null, "Expected a symbol");
        if ((symbol.Flags & SymbolFlags.Transient) != 0)
            return null;
        if (symbol.Declarations.Length == 0)
        {
            Debug.Assert((symbol.Flags & SymbolFlags.Prototype) != 0, "File-bound symbol has no declarations");
            Debug.Assert(symbol.Parent is not null && (symbol.Parent.Flags & SymbolFlags.Class) != 0, "Prototype has no declaring class");
            symbol = symbol.Parent;
            Debug.Assert((symbol.Flags & SymbolFlags.Transient) == 0, "Prototype parent is not file-bound");
            Debug.Assert(symbol.Declarations.Length != 0, "Prototype parent has no declarations");
        }

        var file = GetSourceFileOfNode(symbol.Declarations[0]);
        Debug.Assert(file is not null, "File-bound declaration has no source file");
        return file;
    }

    internal static ByteStringValue SymbolNameValue(Symbol symbol) =>
        symbol.ValueDeclaration is not null && IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration)
            ? symbol.ValueDeclaration.Name().TextValue() : symbol.NameValue;
    public static string SymbolName(Symbol symbol) => symbol.ValueDeclaration is not null && IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration) ? symbol.ValueDeclaration.Name().Text() : symbol.Name;
    public static string EscapeAllInternalSymbolNames(string name) => name.Replace(InternalSymbolNamePrefix, "__", StringComparison.Ordinal);
    public static string EscapeInternalSymbolName(string name) => name.StartsWith(InternalSymbolNamePrefix, StringComparison.Ordinal) ? "__" + name.Substring(1) : name;
    public static string EscapeSymbolName(string name) => name.StartsWith(InternalSymbolNamePrefix, StringComparison.Ordinal) ? "__" + name.Substring(1) : name.StartsWith("__", StringComparison.Ordinal) ? "_" + name : name;
}
