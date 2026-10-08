// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // CompareTypes (414): intrinsic, scalar literal and unnamed union branches.
    public static int CompareTypes(Type t1, Type t2)
    {
        if (t1 == t2)
            return 0;
        if (t1 == null)
            return -1;
        if (t2 == null)
            return 1;
        if (t1.checker != t2.checker)
            throw new InvalidOperationException("Cannot compare types from different checkers");
        int c = unchecked((int)t1.flags - (int)t2.flags);
        if (c != 0)
            return c;
        if (t1.alias != null || t2.alias != null || t1.symbol != null || t2.symbol != null)
            throw unsupported("CompareTypes: named types");
        if ((t1.flags & TypeFlags.Singleton) != 0)
        {
        }
        else if ((t1.flags & TypeFlags.Union) != 0)
        {
            var o1 = t1.AsUnionType().origin;
            var o2 = t2.AsUnionType().origin;
            if (o1 == null && o2 == null)
                c = compareTypeLists(t1.Types(), t2.Types());
            else if (o1 == null)
                c = 1;
            else if (o2 == null)
                c = -1;
            else
                c = CompareTypes(o1, o2);
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.StringLiteral) != 0)
        {
            c = string.CompareOrdinal((string)t1.AsLiteralType().value, (string)t2.AsLiteralType().value);
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.NumberLiteral) != 0)
        {
            c = ((JsNum.Number)t1.AsLiteralType().value).Value.CompareTo(((JsNum.Number)t2.AsLiteralType().value).Value);
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.BooleanLiteral) != 0)
        {
            bool b1 = (bool)t1.AsLiteralType().value, b2 = (bool)t2.AsLiteralType().value;
            if (b1 != b2)
                return b1 ? 1 : -1;
        }
        else
            throw unsupported("CompareTypes: nonscalar payload");
        return unchecked((int)t1.id - (int)t2.id);
    }

    private static int compareTypeLists(List<Type> left, List<Type> right)
    {
        if (left.Count != right.Count)
            return left.Count - right.Count;
        for (int i = 0; i < left.Count; i++)
        {
            int result = CompareTypes(left[i], right[i]);
            if (result != 0)
                return result;
        }

        return 0;
    }

    // isMutableLocalVariableDeclaration (1096).
    private bool isMutableLocalVariableDeclaration(Node declaration) => (declaration.Parent.Flags & NodeFlags.Let) != 0 && !((Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Export) != 0 || declaration.Parent.Parent.Kind == Kind.VariableStatement && Ast.IsGlobalSourceFile(declaration.Parent.Parent.Parent));
}

public sealed partial class Checker
{
    // Generated from checker/utilities.go:1331-1608 by tools/generate-checker-feature-map.py.
    private readonly record struct FeatureMapEntry(string lib, List<string> props);
    private static readonly Lazy<Dictionary<string, List<FeatureMapEntry>>> featureMap = new(() => new(StringComparer.Ordinal) { ["Array"] = [new("es2015", ["find", "findIndex", "fill", "copyWithin", "entries", "keys", "values"]), new("es2016", ["includes"]), new("es2019", ["flat", "flatMap"]), new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Iterator"] = [new("es2015", []), ], ["IteratorConstructor"] = [new("es2026", ["concat"]), ], ["RawJSON"] = [new("es2026", []), ], ["JSON"] = [new("es2026", ["isRawJSON", "rawJSON"]), ], ["AsyncIterator"] = [new("es2015", []), ], ["ArrayBuffer"] = [new("es2024", ["maxByteLength", "resizable", "resize", "detached", "transfer", "transferToFixedLength"]), ], ["Atomics"] = [new("es2017", ["add", "and", "compareExchange", "exchange", "isLockFree", "load", "or", "store", "sub", "wait", "notify", "xor"]), new("es2024", ["waitAsync"]), ], ["SharedArrayBuffer"] = [new("es2017", ["byteLength", "slice"]), new("es2024", ["growable", "maxByteLength", "grow"]), ], ["AsyncIterable"] = [new("es2018", []), ], ["AsyncIterableIterator"] = [new("es2018", []), ], ["AsyncGenerator"] = [new("es2018", []), ], ["AsyncGeneratorFunction"] = [new("es2018", []), ], ["RegExp"] = [new("es2015", ["flags", "sticky", "unicode"]), new("es2018", ["dotAll"]), new("es2024", ["unicodeSets"]), ], ["RegExpConstructor"] = [new("es2025", ["escape"]), ], ["Reflect"] = [new("es2015", ["apply", "construct", "defineProperty", "deleteProperty", "get", "getOwnPropertyDescriptor", "getPrototypeOf", "has", "isExtensible", "ownKeys", "preventExtensions", "set", "setPrototypeOf"]), ], ["ArrayConstructor"] = [new("es2015", ["from", "of"]), new("es2026", ["fromAsync"]), ], ["ObjectConstructor"] = [new("es2015", ["assign", "getOwnPropertySymbols", "keys", "is", "setPrototypeOf"]), new("es2017", ["values", "entries", "getOwnPropertyDescriptors"]), new("es2019", ["fromEntries"]), new("es2022", ["hasOwn"]), new("es2024", ["groupBy"]), ], ["NumberConstructor"] = [new("es2015", ["isFinite", "isInteger", "isNaN", "isSafeInteger", "parseFloat", "parseInt"]), ], ["Math"] = [new("es2015", ["clz32", "imul", "sign", "log10", "log2", "log1p", "expm1", "cosh", "sinh", "tanh", "acosh", "asinh", "atanh", "hypot", "trunc", "fround", "cbrt"]), new("es2025", ["f16round"]), new("es2026", ["sumPrecise"]), ], ["Map"] = [new("es2015", ["entries", "keys", "values"]), new("es2026", ["getOrInsert", "getOrInsertComputed"]), ], ["MapConstructor"] = [new("es2024", ["groupBy"]), ], ["Set"] = [new("es2015", ["entries", "keys", "values"]), new("es2025", ["union", "intersection", "difference", "symmetricDifference", "isSubsetOf", "isSupersetOf", "isDisjointFrom"]), ], ["PromiseConstructor"] = [new("es2015", ["all", "race", "reject", "resolve"]), new("es2020", ["allSettled"]), new("es2021", ["any"]), new("es2024", ["withResolvers"]), new("es2025", ["try"]), ], ["Symbol"] = [new("es2015", ["for", "keyFor"]), new("es2019", ["description"]), ], ["WeakMap"] = [new("es2015", []), new("es2026", ["getOrInsert", "getOrInsertComputed"]), ], ["WeakSet"] = [new("es2015", []), ], ["String"] = [new("es2015", ["codePointAt", "includes", "endsWith", "normalize", "repeat", "startsWith", "anchor", "big", "blink", "bold", "fixed", "fontcolor", "fontsize", "italics", "link", "small", "strike", "sub", "sup"]), new("es2017", ["padStart", "padEnd"]), new("es2019", ["trimStart", "trimEnd", "trimLeft", "trimRight"]), new("es2020", ["matchAll"]), new("es2021", ["replaceAll"]), new("es2022", ["at"]), new("es2024", ["isWellFormed", "toWellFormed"]), ], ["StringConstructor"] = [new("es2015", ["fromCodePoint", "raw"]), ], ["DateTimeFormat"] = [new("es2017", ["formatToParts"]), ], ["Promise"] = [new("es2015", []), new("es2018", ["finally"]), ], ["RegExpMatchArray"] = [new("es2018", ["groups"]), ], ["RegExpExecArray"] = [new("es2018", ["groups"]), ], ["Intl"] = [new("es2018", ["PluralRules"]), new("es2020", ["RelativeTimeFormat", "Locale", "DisplayNames"]), new("es2021", ["ListFormat", "DateTimeFormat"]), new("es2022", ["Segmenter"]), new("es2025", ["DurationFormat"]), ], ["NumberFormat"] = [new("es2018", ["formatToParts"]), ], ["SymbolConstructor"] = [new("es2020", ["matchAll"]), new("esnext", ["metadata", "dispose", "asyncDispose"]), ], ["DataView"] = [new("es2020", ["setBigInt64", "setBigUint64", "getBigInt64", "getBigUint64"]), new("es2025", ["setFloat16", "getFloat16"]), ], ["BigInt"] = [new("es2020", []), ], ["RelativeTimeFormat"] = [new("es2020", ["format", "formatToParts", "resolvedOptions"]), ], ["Int8Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Uint8Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), new("es2026", ["toBase64", "setFromBase64", "toHex", "setFromHex"]), ], ["Uint8ClampedArray"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Int16Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Uint16Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Int32Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Uint32Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Float16Array"] = [new("es2025", []), ], ["Float32Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Float64Array"] = [new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["BigInt64Array"] = [new("es2020", []), new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["BigUint64Array"] = [new("es2020", []), new("es2022", ["at"]), new("es2023", ["findLastIndex", "findLast", "toReversed", "toSorted", "toSpliced", "with"]), ], ["Error"] = [new("es2022", ["cause"]), ], ["ErrorConstructor"] = [new("es2026", ["isError"]), ], ["Uint8ArrayConstructor"] = [new("es2026", ["fromBase64", "fromHex"]), ], ["DisposableStack"] = [new("esnext", []), ], ["AsyncDisposableStack"] = [new("esnext", []), ], ["Date"] = [new("esnext", ["toTemporalInstant"]), ], });
    private static Dictionary<string, List<FeatureMapEntry>> getFeatureMap() => featureMap.Value;
}

public sealed partial class Checker
{
    private int compareSymbolsWorker(Symbol left, Symbol right)
    {
        if (left == right)
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;
        int leftCount = left.Declarations?.Count ?? 0, rightCount = right.Declarations?.Count ?? 0;
        if (leftCount != 0 && rightCount != 0)
        {
            int c = compareNodes(left.Declarations[0], right.Declarations[0]);
            if (c != 0)
                return c;
        }
        else if (leftCount != 0)
            return -1;
        else if (rightCount != 0)
            return 1;
        int result = string.CompareOrdinal(left.Name, right.Name);
        if (result != 0)
            return result;
        return unchecked((int)(Ast.GetSymbolId(left) - Ast.GetSymbolId(right)));
    }

    private int compareNodes(Node left, Node right)
    {
        if (left == right)
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;
        if (Ast.GetSourceFileOfNode(left) != Ast.GetSourceFileOfNode(right))
            throw unsupported("compareNodes: multi-file checker");
        return left.Pos() - right.Pos();
    }
}

public sealed partial class Checker
{
    private void sortSymbols(List<Symbol> symbols) => symbols.Sort(compareSymbolsWorker);
    private static bool isReservedMemberName(string name) => name.Length >= 2 && name[0] == '\u00FE' && name[1] != '@' && name[1] != '#';
}

public sealed partial class Checker
{
    private static Diagnostic NewDiagnosticForNode(Node node, Message message, params object[] args)
    {
        if (node == null)
            return Ast.NewDiagnostic(null, new TextRange(0, 0), message, args);
        var range = TypeScript.Scanner.Scanner.GetErrorRangeForNode(Ast.GetSourceFileOfNode(node), node);
        return Ast.NewDiagnostic(Ast.GetSourceFileOfNode(node), range, message, args);
    }
}
