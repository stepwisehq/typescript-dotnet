// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // utilities.go:912.
    private static bool isNodeDescendantOf(Node node, Node ancestor)
    {
        for (var current = node; current != null; current = current.Parent)
            if (current == ancestor)
                return true;
        return false;
    }

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
        int c = getSortOrderFlags(t1) - getSortOrderFlags(t2);
        if (c != 0)
            return c;
        c = compareTypeNames(t1, t2);
        if (c != 0)
            return c;
        if ((t1.flags & TypeFlags.Singleton) != 0)
        {
        }
        else if ((t1.flags & TypeFlags.Object) != 0)
        {
            if ((t1.objectFlags & ObjectFlags.InstantiationExpressionType) != 0 && (t2.objectFlags & ObjectFlags.InstantiationExpressionType) != 0)
                throw unsupported("CompareTypes: source instantiation expression");
            c = t1.checker.compareSymbols(t1.symbol, t2.symbol);
            if (c != 0)
                return c;
            if ((t1.objectFlags & ObjectFlags.Reference) != 0 && (t2.objectFlags & ObjectFlags.Reference) != 0)
            {
                var r1 = t1.AsTypeReference();
                var r2 = t2.AsTypeReference();
                if ((r1.target.objectFlags & r2.target.objectFlags & ObjectFlags.Tuple) != 0)
                    throw unsupported("CompareTypes: source tuple reference");
                if (r1.node == null && r2.node == null)
                    c = compareTypeLists(r1.resolvedTypeArguments, r2.resolvedTypeArguments);
                else
                {
                    c = t1.checker.compareNodes(r1.node, r2.node);
                    if (c == 0)
                        c = compareTypeMappers(t1.AsObjectType().mapper, t2.AsObjectType().mapper);
                }

                if (c != 0)
                    return c;
            }
            else if ((t1.objectFlags & ObjectFlags.Reference) != 0)
                return -1;
            else if ((t2.objectFlags & ObjectFlags.Reference) != 0)
                return 1;
            else
            {
                c = (int)(t1.objectFlags & ObjectFlags.ObjectTypeKindMask) - (int)(t2.objectFlags & ObjectFlags.ObjectTypeKindMask);
                if (c != 0)
                    return c;
                if ((t1.objectFlags & (ObjectFlags.ReverseMapped | ObjectFlags.Mapped)) != 0)
                    throw unsupported("CompareTypes: source mapped object");
                c = compareTypeMappers(t1.AsObjectType().mapper, t2.AsObjectType().mapper);
                if (c != 0)
                    return c;
            }
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
        else if ((t1.flags & TypeFlags.Intersection) != 0)
        {
            c = compareTypeLists(t1.Types(), t2.Types());
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.TypeParameter) != 0)
        {
            c = t1.checker.compareSymbols(t1.symbol, t2.symbol);
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.Index) != 0)
        {
            c = CompareTypes(t1.AsIndexType().target, t2.AsIndexType().target);
            if (c != 0)
                return c;
            c = (int)t1.AsIndexType().indexFlags - (int)t2.AsIndexType().indexFlags;
            if (c != 0)
                return c;
        }
        else if ((t1.flags & TypeFlags.UniqueESSymbol) != 0)
        {
            c = t1.checker.compareSymbols(t1.symbol, t2.symbol);
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

    // utilities.go:716.
    private static int compareTypeMappers(TypeMapper m1, TypeMapper m2)
    {
        if (m1 == m2)
            return 0;
        if (m1 == null)
            return 1;
        if (m2 == null)
            return -1;
        var kind1 = m1.Kind();
        var kind2 = m2.Kind();
        if (kind1 != kind2)
            return (int)kind1 - (int)kind2;
        int c;
        switch (kind1)
        {
            case TypeMapperKind.Simple:
                var s1 = (SimpleTypeMapper)m1.data;
                var s2 = (SimpleTypeMapper)m2.data;
                c = CompareTypes(s1.source, s2.source);
                return c != 0 ? c : CompareTypes(s1.target, s2.target);
            case TypeMapperKind.Array:
                var a1 = (ArrayTypeMapper)m1.data;
                var a2 = (ArrayTypeMapper)m2.data;
                c = compareTypeLists(a1.sources, a2.sources);
                return c != 0 ? c : compareTypeLists(a1.targets, a2.targets);
            case TypeMapperKind.Merged:
                var g1 = (MergedTypeMapper)m1.data;
                var g2 = (MergedTypeMapper)m2.data;
                c = compareTypeMappers(g1.m1, g2.m1);
                return c != 0 ? c : compareTypeMappers(g1.m2, g2.m2);
        }

        return 0;
    }

    private static int compareTypeLists(List<Type> left, List<Type> right)
    {
        if ((left?.Count ?? 0) != (right?.Count ?? 0))
            return (left?.Count ?? 0) - (right?.Count ?? 0);
        for (int i = 0; i < (left?.Count ?? 0); i++)
        {
            int result = CompareTypes(left[i], right[i]);
            if (result != 0)
                return result;
        }

        return 0;
    }

    private static int getSortOrderFlags(Type t) => (t.flags & (TypeFlags.EnumLiteral | TypeFlags.Enum)) != 0 && (t.flags & TypeFlags.Union) == 0 ? (int)TypeFlags.Enum : (int)t.flags;
    private static int compareTypeNames(Type t1, Type t2)
    {
        var s1 = getTypeNameSymbol(t1);
        var s2 = getTypeNameSymbol(t2);
        if (s1 == s2)
            return compareTypeLists(t1.alias?.TypeArguments(), t2.alias?.TypeArguments());
        if (s1 == null)
            return 1;
        if (s2 == null)
            return -1;
        var result = string.CompareOrdinal(s1.Name, s2.Name);
        return result != 0 ? result : t1.checker.compareSymbolsWorker(s1, s2);
    }

    private static Symbol getTypeNameSymbol(Type t)
    {
        if (t.alias != null)
            return t.alias.symbol;
        return (t.flags & (TypeFlags.TypeParameter | TypeFlags.StringMapping)) != 0 || (t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference)) != 0 ? t.symbol : null;
    }
// isMutableLocalVariableDeclaration (1096).
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

public sealed partial class Checker
{
    // utilities.go:1140. Retained syntax-only declaration member dispatch.
    private static List<Node> getMembersOfDeclaration(Node node) => node.Kind switch
    {
        Kind.InterfaceDeclaration or Kind.ClassDeclaration or Kind.ClassExpression or Kind.TypeLiteral => node.Members(),
        Kind.ObjectLiteralExpression => node.Properties(),
        _ => null
    };
}

public sealed partial class Checker
{
    private static ModifierFlags getDeclarationModifierFlagsFromSymbol(Symbol symbol) => getDeclarationModifierFlagsFromSymbolEx(symbol, false);
    private static ModifierFlags getDeclarationModifierFlagsFromSymbolEx(Symbol symbol, bool isWrite)
    {
        if ((symbol.CheckFlags & CheckFlags.Synthetic) != 0)
        {
            var access = (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsPublic) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWritePublic) != 0) ? ModifierFlags.Public : (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsProtected) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWriteProtected) != 0) ? ModifierFlags.Protected : (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsPrivate) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWritePrivate) != 0) ? ModifierFlags.Private : ModifierFlags.None;
            return (symbol.CheckFlags & CheckFlags.ContainsStatic) != 0 ? access | ModifierFlags.Static : access;
        }

        if (symbol.ValueDeclaration != null)
        {
            if ((symbol.Flags & SymbolFlags.Accessor) != 0)
                throw unsupported("getDeclarationModifierFlagsFromSymbolEx: accessor");
            var flags = Ast.GetCombinedModifierFlags(symbol.ValueDeclaration);
            return symbol.Parent != null && (symbol.Parent.Flags & SymbolFlags.Class) != 0 ? flags : flags & ~ModifierFlags.AccessibilityModifier;
        }

        return (symbol.Flags & SymbolFlags.Prototype) != 0 ? ModifierFlags.Public | ModifierFlags.Static : ModifierFlags.None;
    }
}

public sealed partial class Checker
{
    private static string entityNameToString(Node node) => Ast.EntityNameToString(node, TypeScript.Scanner.Scanner.GetTextOfNodeByteString);
}

public sealed partial class Checker
{
    private static Diagnostic NewDiagnosticChainForNode(Diagnostic chain, Node node, Message message, params object[] args) => chain != null ? Ast.NewDiagnosticChain(chain, message, args) : NewDiagnosticForNode(node, message, args);
}

public sealed partial class Checker
{
    // checker/utilities.go:922/929, selected static property-name types.
    private static bool isTypeUsableAsPropertyName(Type t) => (t.flags & TypeFlags.StringOrNumberLiteralOrUnique) != 0;
    private static string getPropertyNameFromType(Type t)
    {
        if ((t.flags & TypeFlags.StringLiteral) != 0)
            return (string)t.AsLiteralType().value;
        if ((t.flags & TypeFlags.NumberLiteral) != 0)
            return ((JsNum.Number)t.AsLiteralType().value).String();
        if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
            return t.AsUniqueESSymbolType().name;
        throw new InvalidOperationException("Unhandled case in getPropertyNameFromType");
    }
}

public sealed partial class Checker
{
    // checker/utilities.go:298.
    internal static bool isOptionalDeclaration(Node declaration) => Ast.HasQuestionToken(declaration);
}

public sealed partial class Checker
{
    private enum AssignmentKind
    {
        None,
        Definite,
        Compound
    }

    // checker/utilities.go:89. Pure syntax classification is complete; semantic
    // write checking remains outside admission and explicitly guards when reached.
    private static AssignmentKind getAssignmentTargetKind(Node node)
    {
        var target = Ast.GetAssignmentTarget(node);
        if (target == null)
            return AssignmentKind.None;
        switch (target.Kind)
        {
            case Kind.BinaryExpression:
                var binaryOperator = target.AsBinaryExpression().OperatorToken.Kind;
                return binaryOperator == Kind.EqualsToken || Ast.IsLogicalOrCoalescingAssignmentOperator(binaryOperator) ? AssignmentKind.Definite : AssignmentKind.Compound;
            case Kind.PrefixUnaryExpression:
            case Kind.PostfixUnaryExpression:
                return AssignmentKind.Compound;
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
                return AssignmentKind.Definite;
            default:
                throw new InvalidOperationException("Unhandled case in getAssignmentTargetKind");
        }
    }
}

public sealed partial class Checker
{
    // checker/utilities.go:1018/1029.
    private static bool IsKnownSymbol(Symbol symbol) => isLateBoundName(symbol.Name);
    private static bool isLateBoundName(string name) => name.Length >= 2 && name[0] == '\u00FE' && name[1] == '@';
}

public sealed partial class Checker
{
    // utilities.go:71/183.
    private static bool isStaticPrivateIdentifierProperty(Symbol symbol) => symbol.ValueDeclaration != null && Ast.IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration) && Ast.IsStatic(symbol.ValueDeclaration);
    private static bool canHaveLocals(Node node) => node.Kind is Kind.ArrowFunction or Kind.Block or Kind.CallSignature or Kind.CaseBlock or Kind.CatchClause or Kind.ClassStaticBlockDeclaration or Kind.ConditionalType or Kind.Constructor or Kind.ConstructorType or Kind.ConstructSignature or Kind.ForStatement or Kind.ForInStatement or Kind.ForOfStatement or Kind.FunctionDeclaration or Kind.FunctionExpression or Kind.FunctionType or Kind.GetAccessor or Kind.IndexSignature or Kind.JSDocSignature or Kind.MappedType or Kind.MethodDeclaration or Kind.MethodSignature or Kind.ModuleDeclaration or Kind.SetAccessor or Kind.SourceFile or Kind.TypeAliasDeclaration or Kind.JSTypeAliasDeclaration;
}

public sealed partial class Checker
{
    // utilities.go:941. ECMAScript numeric property-name round trip.
    private static bool isNumericLiteralName(string name) => JsNum.FromString(name).String() == name;
}

public sealed partial class Checker
{
    // utilities.go:1188.
    private static bool isInNameOfExpressionWithTypeArgumentsOrHeritageTypeReference(Node node)
    {
        while (node.Parent.Kind is Kind.PropertyAccessExpression or Kind.QualifiedName)
            node = node.Parent;
        return node.Parent.Kind == Kind.ExpressionWithTypeArguments || Ast.IsNameOfHeritageClauseTypeReference(node);
    }
}

public sealed partial class Checker
{
    // utilities.go:161.
    private static bool isTypeReferenceIdentifier(Node node)
    {
        while (node.Parent.Kind == Kind.QualifiedName)
            node = node.Parent;
        return Ast.IsTypeReferenceNode(node.Parent);
    }
}

public sealed partial class Checker
{
    // utilities.go:221.
    private static Node getContainingQualifiedNameNode(Node node)
    {
        while (Ast.IsQualifiedName(node.Parent))
            node = node.Parent;
        return node;
    }
}

public sealed partial class Checker
{
    private static bool hasDotDotDotToken(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
            {
                return (node.AsParameterDeclaration().DotDotDotToken != null);
            }

            case Kind.BindingElement:
            {
                return (node.AsBindingElement().DotDotDotToken != null);
            }

            case Kind.NamedTupleMember:
            {
                return (node.AsNamedTupleMember().DotDotDotToken != null);
            }

            case Kind.JsxExpression:
            {
                return (node.AsJsxExpression().DotDotDotToken != null);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private static bool declarationBelongsToPrivateAmbientMember(Node declaration)
    {
        var root = Ast.GetRootDeclaration(declaration);
        var memberDeclaration = root;
        if ((root.Kind == Kind.Parameter))
        {
            memberDeclaration = root.Parent;
        }

        return isPrivateWithinAmbient(memberDeclaration);
    }
}

public sealed partial class Checker
{
    private static bool isPrivateWithinAmbient(Node node)
    {
        return (((Ast.HasModifier(node, ModifierFlags.Private) || Ast.IsPrivateIdentifierClassElementDeclaration(node))) && ((node.Flags & NodeFlags.Ambient) != 0));
    }
}

public sealed partial class Checker
{
    private static Node getContainingFunctionOrClassStaticBlock(Node node)
    {
        return Ast.FindAncestor(node.Parent, Ast.IsFunctionLikeOrClassStaticBlockDeclaration);
    }
}

public sealed partial class Checker
{
    internal bool isOptionalParameter(Node node)
    {
        if ((Ast.IsParameterDeclaration(node) && (node.QuestionToken() != null)))
        {
            return true;
        }

        if (!Ast.IsParameterDeclaration(node))
        {
            return false;
        }

        if ((node.Initializer() != null))
        {
            var signature = this.getSignatureFromDeclaration(node.Parent);
            var parameterIndex = Core.FindIndex(node.Parent.Parameters(), (p) =>
            {
                return (p == node);
            });
            Debug.Assert((parameterIndex >= 0));
            return (parameterIndex >= this.getMinArgumentCountEx(signature, (MinArgumentCountFlags.StrongArityForUntypedJS | MinArgumentCountFlags.VoidIsNonOptional)));
        }

        var iife = Ast.GetImmediatelyInvokedFunctionExpression(node.Parent);
        if ((iife != null))
        {
            var parameterIndex2 = Core.FindIndex(node.Parent.Parameters(), (p2) =>
            {
                return (p2 == node);
            });
            return (((node.Type() == null) && (node.AsParameterDeclaration().DotDotDotToken == null)) && (parameterIndex2 >= GoRuntime.Len(this.getEffectiveCallArguments(iife))));
        }

        return false;
    }
}

public sealed partial class Checker
{
    internal static bool containsNonMissingUndefinedType(Checker c, Type t)
    {
        Type candidate = default;
        if (((t.flags & TypeFlags.Union) != 0))
        {
            candidate = t.AsUnionType().types[0];
        }
        else
        {
            candidate = t;
        }

        return (((candidate.flags & TypeFlags.Undefined) != 0) && (candidate != c.missingType));
    }
}

public sealed partial class Checker
{
    private bool isConstantVariable(Symbol symbol)
    {
        return (((symbol.Flags & SymbolFlags.Variable) != 0) && (((this.getDeclarationNodeFlagsFromSymbol(symbol) & NodeFlags.Constant)) != 0));
    }
}

public sealed partial class Checker
{
    private bool isParameterOrMutableLocalVariable(Symbol symbol)
    {
        if ((symbol.ValueDeclaration != null))
        {
            var declaration = Ast.GetRootDeclaration(symbol.ValueDeclaration);
            return ((declaration != null) && ((Ast.IsParameterDeclaration(declaration) || (Ast.IsVariableDeclaration(declaration) && ((Ast.IsCatchClause(declaration.Parent) || this.isMutableLocalVariableDeclaration(declaration)))))));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isMutableLocalVariableDeclaration(Node declaration)
    {
        return (((declaration.Parent.Flags & NodeFlags.Let) != 0) && !((((Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Export) != 0) || ((declaration.Parent.Parent.Kind == Kind.VariableStatement) && Ast.IsGlobalSourceFile(declaration.Parent.Parent.Parent)))));
    }
}

public sealed partial class Checker
{
    private static Node getEnclosingContainer(Node node)
    {
        return Ast.FindAncestor(node.Parent, (n) =>
        {
            return ((Binder.GetContainerFlags(n) & ContainerFlags.IsContainer) != 0);
        });
    }
}

public sealed partial class Checker
{
    private static bool isInCompoundLikeAssignment(Node node)
    {
        var target = Ast.GetAssignmentTarget(node);
        return (((target != null) && Ast.IsAssignmentExpression(target, true)) && isCompoundLikeAssignment(target));
    }
}

public sealed partial class Checker
{
    private static bool isCompoundLikeAssignment(Node assignment)
    {
        var right = Ast.SkipParentheses(assignment.AsBinaryExpression().Right);
        return ((right.Kind == Kind.BinaryExpression) && isShiftOperatorOrHigher(right.AsBinaryExpression().OperatorToken.Kind));
    }
}

public sealed partial class Checker
{
    private static bool isNonNullAccess(Node node)
    {
        return (Ast.IsAccessExpression(node) && Ast.IsNonNullExpression(node.Expression()));
    }
}

public sealed partial class Checker
{
    private static bool isCallChain(Node node)
    {
        return (Ast.IsCallExpression(node) && ((node.Flags & NodeFlags.OptionalChain) != 0));
    }
}

public sealed partial class Checker
{
    private static bool isShiftOperatorOrHigher(Kind kind)
    {
        return (isShiftOperator(kind) || isAdditiveOperatorOrHigher(kind));
    }
}

public sealed partial class Checker
{
    private bool callLikeExpressionMayHaveTypeArguments(Node node)
    {
        return ((Ast.IsCallOrNewExpression(node) || Ast.IsTaggedTemplateExpression(node)) || Ast.IsJsxOpeningLikeElement(node));
    }
}

public sealed partial class Checker
{
    private static bool isSuperCall(Node n)
    {
        return (Ast.IsCallExpression(n) && (n.Expression().Kind == Kind.SuperKeyword));
    }
}

public sealed partial class Checker
{
    private static int getNonRestParameterCount(Signature sig)
    {
        return (GoRuntime.Len(sig.parameters) - Core.IfElse(signatureHasRestParameter(sig), 1, 0));
    }
}

public sealed partial class Checker
{
    public static bool IsTypeAny(Type t)
    {
        return ((t != null) && ((t.flags & TypeFlags.Any) != 0));
    }
}

public sealed partial class Checker
{
    private static bool isExponentiationOperator(Kind kind)
    {
        return (kind == Kind.AsteriskAsteriskToken);
    }
}

public sealed partial class Checker
{
    private static bool isMultiplicativeOperatorOrHigher(Kind kind)
    {
        return (isExponentiationOperator(kind) || isMultiplicativeOperator(kind));
    }
}

public sealed partial class Checker
{
    private static bool isAdditiveOperatorOrHigher(Kind kind)
    {
        return (isAdditiveOperator(kind) || isMultiplicativeOperatorOrHigher(kind));
    }
}

public sealed partial class Checker
{
    private static bool isShiftOperator(Kind kind)
    {
        return (((kind == Kind.LessThanLessThanToken) || (kind == Kind.GreaterThanGreaterThanToken)) || (kind == Kind.GreaterThanGreaterThanGreaterThanToken));
    }
}

public sealed partial class Checker
{
    private static bool isMultiplicativeOperator(Kind kind)
    {
        return (((kind == Kind.AsteriskToken) || (kind == Kind.SlashToken)) || (kind == Kind.PercentToken));
    }
}

public sealed partial class Checker
{
    private static bool isAdditiveOperator(Kind kind)
    {
        return ((kind == Kind.PlusToken) || (kind == Kind.MinusToken));
    }
}

public sealed partial class Checker
{
    private static Node getContainingObjectLiteral(Node f)
    {
        if ((((((f.Kind == Kind.MethodDeclaration) || (f.Kind == Kind.GetAccessor)) || (f.Kind == Kind.SetAccessor))) && (f.Parent.Kind == Kind.ObjectLiteralExpression)))
        {
            return f.Parent;
        }
        else
        {
            if (((f.Kind == Kind.FunctionExpression) && (f.Parent.Kind == Kind.PropertyAssignment)))
            {
                return f.Parent.Parent;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private static List<Node> getAllJSDocTags(Node node)
    {
        if (((node.Flags & NodeFlags.JSDoc) == 0))
        {
            {
                var current = node;
                for (; (current != null); current = Ast.GetNextJSDocCommentLocation(current))
                {
                    var jsdocs = current.JSDoc(null);
                    if ((GoRuntime.Len(jsdocs) == 0))
                    {
                        continue;
                    }

                    var lastJSDoc = jsdocs[(GoRuntime.Len(jsdocs) - 1)].AsJSDoc();
                    if ((lastJSDoc.Tags != null))
                    {
                        return lastJSDoc.Tags.Nodes;
                    }
                }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private static (int, int) minAndMax<T>(List<T> slice, Func<T, int> getValue)
    {
        int minValue = default;
        int maxValue = default;
        {
            foreach (var range1 in GoRuntime.Range(slice))
            {
                var i = range1.Key;
                var element = range1.Value;
                var value2 = getValue(element);
                if ((i == 0))
                {
                    minValue = value2;
                    maxValue = value2;
                }
                else
                {
                    minValue = Math.Min(minValue, value2);
                    maxValue = Math.Max(maxValue, value2);
                }
            }
        }

        return (minValue, maxValue);
    }
}

public sealed partial class Checker
{
    public static bool IsInTypeQuery(Node node)
    {
        return (Ast.FindAncestorOrQuit(node, (n) =>
        {
            switch (n.Kind)
            {
                case Kind.TypeQuery:
                {
                    return FindAncestorResult.True;
                }

                case Kind.Identifier:
                case Kind.QualifiedName:
                {
                    return FindAncestorResult.False;
                }
            }

            return FindAncestorResult.Quit;
        }) != null);
    }
}

public sealed partial class Checker
{
}

public sealed partial class Checker
{
    private static bool isLiteralExpressionOfObject(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ObjectLiteralExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.RegularExpressionLiteral:
            case Kind.FunctionExpression:
            case Kind.ClassExpression:
            {
                return true;
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private static bool isDeleteTarget(Node node)
    {
        if (!Ast.IsAccessExpression(node))
        {
            return false;
        }

        node = Ast.WalkUpParenthesizedExpressions(node.Parent);
        return ((node != null) && (node.Kind == Kind.DeleteExpression));
    }
}

public sealed partial class Checker
{
    private static bool isThisProperty(Node node)
    {
        return (((Ast.IsPropertyAccessExpression(node) || Ast.IsElementAccessExpression(node))) && (node.Expression().Kind == Kind.ThisKeyword));
    }
}

public sealed partial class Checker
{
    private bool isJSLiteralType(Type t)
    {
        if (this.noImplicitAny)
        {
            return false;
        }

        if (((t.objectFlags & ObjectFlags.JSLiteral) != 0))
        {
            return true;
        }

        if (((t.flags & TypeFlags.Union) != 0))
        {
            return Core.Every(t.AsUnionType().types, this.isJSLiteralType);
        }

        if (((t.flags & TypeFlags.Intersection) != 0))
        {
            return Core.Some(t.AsIntersectionType().types, this.isJSLiteralType);
        }

        if (((t.flags & TypeFlags.Instantiable) != 0))
        {
            var constraint = this.getResolvedBaseConstraint(t, null);
            return ((constraint != t) && this.isJSLiteralType(constraint));
        }

        return false;
    }
}

public sealed partial class Checker
{
    // utilities.go:871.
    private static bool isDeclarationReadonly(Node declaration) => (Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Readonly) != 0 && !Ast.IsParameterPropertyDeclaration(declaration, declaration.Parent);
}

public sealed partial class Checker
{
    // utilities.go:1228.
    internal static string pseudoBigIntToString(JsNum.PseudoBigInt value) => value.String();
}

public sealed partial class Checker
{
    // utilities.go:294.
    private static bool isExclamationToken(Node node) => node != null && node.Kind == Kind.ExclamationToken;
}

public sealed partial class Checker
{
    // utilities.go:1004.
    private static bool isValidESSymbolDeclaration(Node node)
    {
        if (Ast.IsVariableDeclaration(node))
            return Ast.IsVarConst(node) && Ast.IsIdentifier(node.AsVariableDeclaration().Name()) && isVariableDeclarationInVariableStatement(node);
        if (Ast.IsPropertyDeclaration(node))
            return hasReadonlyModifier(node) && Ast.HasStaticModifier(node);
        return Ast.IsPropertySignatureDeclaration(node) && hasReadonlyModifier(node);
    }

    // utilities.go:1014.
    private static bool isVariableDeclarationInVariableStatement(Node node) => Ast.IsVariableDeclarationList(node.Parent) && Ast.IsVariableStatement(node.Parent.Parent);
    // utilities.go:67.
    private static bool hasReadonlyModifier(Node node) => Ast.HasModifier(node, ModifierFlags.Readonly);
}
