// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/symbol.go; binder-owned state. Checker-only CheckFlags excluded.
#nullable disable
namespace TypeScript.Scanner;
public sealed class Symbol
{
    public SymbolFlags Flags;
    public string Name = "";
    public List<Node> Declarations;
    public Node ValueDeclaration;
    public SymbolTable Members, Exports;
    internal long id;
    public Symbol Parent, ExportSymbol;
    public bool IsExternalModule() => (Flags & SymbolFlags.Module) != 0 && Ast.IsAmbientModuleSymbolName(Name);
    public bool IsStatic() => ValueDeclaration is not null && (ValueDeclaration.ModifierFlags() & ModifierFlags.Static) != 0;
    public SymbolFlags CombinedLocalAndExportSymbolFlags() => ExportSymbol is null ? Flags : Flags | ExportSymbol.Flags;
}

public sealed class SymbolTable : Dictionary<string, Symbol>
{
    public SymbolTable() : base(StringComparer.Ordinal)
    {
    }

    public SymbolTable(int capacity) : base(capacity, StringComparer.Ordinal)
    {
    }

    public new Symbol this[string key] { get => TryGetValue(key, out var symbol) ? symbol : null; set => base[key] = value; }
}

public static partial class Ast
{
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
        if ((symbol.Declarations?.Count ?? 0) == 0)
        {
            Debug.Assert((symbol.Flags & SymbolFlags.Prototype) != 0, "File-bound symbol has no declarations");
            Debug.Assert(symbol.Parent is not null && (symbol.Parent.Flags & SymbolFlags.Class) != 0, "Prototype has no declaring class");
            symbol = symbol.Parent;
            Debug.Assert((symbol.Flags & SymbolFlags.Transient) == 0, "Prototype parent is not file-bound");
            Debug.Assert((symbol.Declarations?.Count ?? 0) != 0, "Prototype parent has no declarations");
        }

        var file = GetSourceFileOfNode(symbol.Declarations[0]);
        Debug.Assert(file is not null, "File-bound declaration has no source file");
        return file;
    }

    public static string SymbolName(Symbol symbol) => symbol.ValueDeclaration is not null && IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration) ? symbol.ValueDeclaration.Name().Text() : symbol.Name;
    public static string EscapeAllInternalSymbolNames(string name) => name.Replace(InternalSymbolNamePrefix, "__", StringComparison.Ordinal);
    public static string EscapeInternalSymbolName(string name) => name.StartsWith(InternalSymbolNamePrefix, StringComparison.Ordinal) ? "__" + name.Substring(1) : name;
    public static string EscapeSymbolName(string name) => name.StartsWith(InternalSymbolNamePrefix, StringComparison.Ordinal) ? "__" + name.Substring(1) : name.StartsWith("__", StringComparison.Ordinal) ? "_" + name : name;
}
