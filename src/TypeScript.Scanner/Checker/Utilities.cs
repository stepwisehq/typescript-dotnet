// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // utilities.go:127/133. Const assertion names do not resolve as ordinary names.
    private static bool isConstTypeReference(Node node) => Ast.IsTypeReferenceNode(node) &&
        GoRuntime.Len(node.TypeArguments()) == 0 && Ast.IsIdentifier(node.AsTypeReferenceNode().TypeName) &&
        node.AsTypeReferenceNode().TypeName.TextEquals("const"u8);

    private static bool isConstTypeReferenceName(Node node) => node != null && Ast.IsIdentifier(node) &&
        node.Parent != null && isConstTypeReference(node.Parent) &&
        node.Parent.Parent != null && Ast.IsAssertionExpression(node.Parent.Parent);

    // utilities.go:240.
    private static bool isRightSideOfAccessExpression(Node node) => node.Parent != null &&
        (Ast.IsPropertyAccessExpression(node.Parent) && node.Parent.Name() == node ||
         Ast.IsElementAccessExpression(node.Parent) && node.Parent.AsElementAccessExpression().ArgumentExpression == node);

    // utilities.go:1174. Return the owning import type, retaining qualifier identity.
    private static Node isImportTypeQualifierPart(Node node)
    {
        var parent = node.Parent;
        while (Ast.IsQualifiedName(parent))
        {
            node = parent;
            parent = parent.Parent;
        }
        return parent != null && parent.Kind == Kind.ImportType && parent.AsImportTypeNode().Qualifier == node ? parent : null;
    }

    // utilities.go:1708.
    internal static bool IsExternalModuleSymbol(Symbol moduleSymbol) => moduleSymbol.IsExternalModule();

    // utilities.go:1203. The source caller supplies a valid parent pointer.
    private static bool expressionResultIsUnused(Node node)
    {
        while (true)
        {
            var parent = node.Parent;
            if (Ast.IsParenthesizedExpression(parent))
            {
                node = parent;
                continue;
            }
            if (Ast.IsExpressionStatement(parent) || Ast.IsVoidExpression(parent) ||
                Ast.IsForStatement(parent) && (parent.Initializer() == node || parent.AsForStatement().Incrementor == node))
                return true;
            if (Ast.IsBinaryExpression(parent) && parent.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)
            {
                if (node == parent.AsBinaryExpression().Left)
                    return true;
                node = parent;
                continue;
            }
            return false;
        }
    }

    // utilities.go:59 and 1264. Yield traversal excludes nested function bodies,
    // while retaining computed names evaluated in the containing generator.
    private static bool hasAsyncModifier(Node node) => Ast.HasSyntacticModifier(node, ModifierFlags.Async);
    private static bool forEachYieldExpression(Node body, Func<Node, bool> visitor) =>
        forEachYieldExpression(body, visitor, static (expression, callback) => callback(expression));

    private static bool forEachYieldExpression<TState>(Node body, TState state, Func<Node, TState, bool> visitor)
    {
        // utilities.go:1264. One recursive visitor belongs to this traversal;
        // the virtual node visitor boundary retains the source callback lifetime.
        Visitor traverse = null;
        traverse = node =>
        {
            switch (node.Kind)
            {
                case Kind.YieldExpression:
                    if (visitor(node, state)) return true;
                    var operand = node.Expression();
                    return operand != null && traverse(operand);
                case Kind.EnumDeclaration:
                case Kind.InterfaceDeclaration:
                case Kind.ModuleDeclaration:
                case Kind.TypeAliasDeclaration:
                    break;
                default:
                    if (Ast.IsFunctionLike(node))
                    {
                        if (node.Name() != null && Ast.IsComputedPropertyName(node.Name()))
                            return traverse(node.Name().Expression());
                    }
                    else if (!Ast.IsPartOfTypeNode(node))
                        return node.ForEachChild(traverse);
                    break;
            }
            return false;
        };
        return traverse(body);
    }

    // utilities.go:1150. Entity-name callers supply a parent, as in the source precondition.
    private static bool isInRightSideOfImportOrExportAssignment(Node node)
    {
        while (node.Parent.Kind == Kind.QualifiedName)
            node = node.Parent;
        return node.Parent.Kind == Kind.ImportEqualsDeclaration && node.Parent.AsImportEqualsDeclaration().ModuleReference == node ||
            node.Parent.Kind == Kind.ExportAssignment && node.Parent.Expression() == node;
    }

    // utilities.go:875-908. The source instantiation is orderedSet[*Type].
    // Pointer identity and the small-set scan precede the original thresholded
    // membership map; append order is independent of hash iteration order.
    private struct orderedSet<T> where T : class
    {
        private const int orderedSetMapThreshold = 16;
        private HashSet<T> valuesByKey;
        internal Slice<T> values;

        internal bool contains(T value)
        {
            if (valuesByKey != null)
                return valuesByKey.Contains(value);
            foreach (var existing in values)
                if (ReferenceEquals(existing, value))
                    return true;
            return false;
        }

        internal void add(T value)
        {
            values = values.Append(value);
            if (valuesByKey == null)
            {
                if (values.Length <= orderedSetMapThreshold)
                    return;
                valuesByKey = new HashSet<T>(values.Length, ReferenceEqualityComparer.Instance);
                foreach (var previous in values.Slice2(0, values.Length - 1))
                    valuesByKey.Add(previous);
            }
            valuesByKey.Add(value);
        }
    }

    // utilities.go:350. Empty input retains a nil table; duplicate names retain the last symbol.
    private static SymbolTable createSymbolTable(Slice<Symbol> symbols)
    {
        if (GoRuntime.Len(symbols) == 0)
            return null;
        var result = new SymbolTable();
        foreach (var symbol in symbols)
            result[symbol.NameValue] = symbol;
        return result;
    }

    // utilities.go:1777. Suggestions in unchecked JS retain the source file/declaration exclusions.
    private bool isUncheckedJSSuggestion(Node node, Symbol suggestion, bool excludeClasses)
    {
        var file = Ast.GetSourceFileOfNode(node);
        if (file != null && compilerOptions.CheckJs == Tristate.Unknown &&
            file.CheckJsDirective == null && (file.ScriptKind == ScriptKind.JS || file.ScriptKind == ScriptKind.JSX))
        {
            SourceFile declarationFile = null;
            if (suggestion != null)
            {
                var firstDeclaration = Core.FirstOrNil(suggestion.Declarations);
                if (firstDeclaration != null)
                    declarationFile = Ast.GetSourceFileOfNode(firstDeclaration);
            }

            var suggestionHasNoExtendsOrDecorators = suggestion == null ||
                suggestion.ValueDeclaration == null ||
                !Ast.IsClassLike(suggestion.ValueDeclaration) ||
                GoRuntime.Len(Ast.GetExtendsHeritageClauseElements(suggestion.ValueDeclaration)) != 0 ||
                Ast.ClassOrConstructorParameterIsDecorated(false, suggestion.ValueDeclaration);
            return !(file != declarationFile && declarationFile != null && Ast.IsGlobalSourceFile(declarationFile.AsNode())) &&
                !(excludeClasses && suggestion != null && (suggestion.Flags & SymbolFlags.Class) != 0 && suggestionHasNoExtendsOrDecorators) &&
                !(node != null && excludeClasses && Ast.IsPropertyAccessExpression(node) && node.Expression().Kind == Kind.ThisKeyword && suggestionHasNoExtendsOrDecorators);
        }
        return false;
    }

    // utilities.go:38. Nil maps have no entries; an unmatched value is Go's zero value.
    private static TValue findInMap<TKey, TValue>(Dictionary<TKey, TValue> map, Func<TValue, bool> predicate) where TKey : notnull
    {
        if (map != null)
            foreach (var value in map.Values)
                if (predicate(value))
                    return value;
        return default;
    }

    // utilities.go:143. Let the export-assignment checker decide value-usage errors.
    private static bool isExportAssignmentExpressionName(Node node)
    {
        if (node == null)
            return false;
        var current = node;
        while (current.Parent != null && Ast.IsPropertyAccessOrQualifiedName(current.Parent))
            current = current.Parent;
        return current.Parent != null && Ast.IsExportAssignment(current.Parent) && current.Parent.Expression() == current;
    }

    // utilities.go:55/1923.
    private static bool hasOverrideModifier(Node node) => Ast.HasSyntacticModifier(node, ModifierFlags.Override);
    private static string quotedAndCommaSeparated(List<string> items) => string.Join(", ", items.Select(item => "'" + item + "'"));
    // utilities.go:1906.
    private static Node walkUpOuterExpressions(Node node)
    {
        var parent = node.Parent;
        while (parent != null && Ast.IsOuterExpression(parent, OuterExpressionKinds.All))
            parent = parent.Parent;
        return parent;
    }

    // utilities.go:63.
    private static ModifierFlags getSelectedModifierFlags(Node node, ModifierFlags flags) => node.ModifierFlags() & flags;
    // utilities.go:1232. Computed names and decorators are evaluated outside their member body.
    private static Node getSuperContainer(Node node, bool stopOnFunctions)
    {
        for (;;)
        {
            node = node.Parent;
            if (node == null)
                return null;
            switch (node.Kind)
            {
                case Kind.ComputedPropertyName:
                    node = node.Parent;
                    break;
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.ArrowFunction:
                    if (!stopOnFunctions)
                        continue;
                    return node;
                case Kind.PropertyDeclaration:
                case Kind.PropertySignature:
                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                case Kind.Constructor:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.ClassStaticBlockDeclaration:
                    return node;
                case Kind.Decorator:
                    if (Ast.IsParameterDeclaration(node.Parent) && Ast.IsClassElement(node.Parent.Parent))
                        node = node.Parent.Parent;
                    else if (Ast.IsClassElement(node.Parent))
                        node = node.Parent;
                    break;
            }
        }
    }

    // checker.go:8927. Same-package predicate dependency, placed beside other checker utility leaves.
    private static bool someSignature(Slice<Signature> signatures, Func<Signature, bool> predicate)
    {
        foreach (var signature in signatures)
            if (signature.composite != null && signature.composite.isUnion && Core.Some(signature.composite.signatures, predicate) ||
                signature.composite == null && predicate(signature))
                return true;
        return false;
    }

    // utilities.go:912.
    private static bool isNodeDescendantOf(Node node, Node ancestor)
    {
        for (var current = node; current != null; current = current.Parent)
            if (current == ancestor)
                return true;
        return false;
    }

    // CompareTypes (414): original selected scalar/generic/object ordering families.
    public static int CompareTypes(Type t1, Type t2)
    {
        if ((t1 == t2))
        {
            return 0;
        }

        if ((t1 == null))
        {
            return -1;
        }

        if ((t2 == null))
        {
            return 1;
        }

        if ((t1.checker != t2.checker))
        {
            throw new InvalidOperationException("Cannot compare types from different checkers");
        }

        {
            var c = (getSortOrderFlags(t1) - getSortOrderFlags(t2));
            if ((c != 0))
            {
                return c;
            }
        }

        {
            var c2 = compareTypeNames(t1, t2);
            if ((c2 != 0))
            {
                return c2;
            }
        }

        switch (true)
        {
            case true when ((t1.flags & ((((((((((((TypeFlags.Any | TypeFlags.Unknown) | TypeFlags.String) | TypeFlags.Number) | TypeFlags.Boolean) | TypeFlags.BigInt) | TypeFlags.ESSymbol) | TypeFlags.Void) | TypeFlags.Undefined) | TypeFlags.Null) | TypeFlags.Never) | TypeFlags.NonPrimitive))) != 0):
            {
                break;
            }

            case true when ((t1.flags & TypeFlags.Object) != 0):
            {
                if ((((t1.objectFlags & ObjectFlags.InstantiationExpressionType) != 0) && ((t2.objectFlags & ObjectFlags.InstantiationExpressionType) != 0)))
                {
                    Node declaration1 = null;
                    Node declaration2 = null;
                    if (t1.symbol?.Declarations is { Count: > 0 })
                        declaration1 = t1.symbol.Declarations[0];
                    if (t2.symbol?.Declarations is { Count: > 0 })
                        declaration2 = t2.symbol.Declarations[0];
                    var declarationOrder = t1.checker.compareNodes(declaration1, declaration2);
                    if (declarationOrder != 0)
                        return declarationOrder;
                    var expressionOrder = t1.checker.compareNodes(t1.AsInstantiationExpressionType().node, t2.AsInstantiationExpressionType().node);
                    if (expressionOrder != 0)
                        return expressionOrder;
                }
                else
                {
                    {
                        var c5 = t1.checker.compareSymbols(t1.symbol, t2.symbol);
                        if ((c5 != 0))
                        {
                            return c5;
                        }
                    }
                }

                if ((((t1.objectFlags & ObjectFlags.Reference) != 0) && ((t2.objectFlags & ObjectFlags.Reference) != 0)))
                {
                    var r1 = t1.AsTypeReference();
                    var r2 = t2.AsTypeReference();
                    if ((((r1.target.objectFlags & ObjectFlags.Tuple) != 0) && ((r2.target.objectFlags & ObjectFlags.Tuple) != 0)))
                    {
                        {
                            var c6 = compareTupleTypes(r1.target.AsTupleType(), r2.target.AsTupleType());
                            if ((c6 != 0))
                            {
                                return c6;
                            }
                        }
                    }

                    if (((r1.node == null) && (r2.node == null)))
                    {
                        {
                            var c7 = compareTypeLists(t1.AsTypeReference().resolvedTypeArguments, t2.AsTypeReference().resolvedTypeArguments);
                            if ((c7 != 0))
                            {
                                return c7;
                            }
                        }
                    }
                    else
                    {
                        {
                            var c8 = t1.checker.compareNodes(r1.node, r2.node);
                            if ((c8 != 0))
                            {
                                return c8;
                            }
                        }

                        {
                            var c9 = compareTypeMappers(t1.AsObjectType().mapper, t2.AsObjectType().mapper);
                            if ((c9 != 0))
                            {
                                return c9;
                            }
                        }
                    }
                }
                else
                {
                    if (((t1.objectFlags & ObjectFlags.Reference) != 0))
                    {
                        return -1;
                    }
                    else
                    {
                        if (((t2.objectFlags & ObjectFlags.Reference) != 0))
                        {
                            return 1;
                        }
                        else
                        {
                            {
                                var c10 = ((int)((t1.objectFlags & ObjectFlags.ObjectTypeKindMask)) - (int)((t2.objectFlags & ObjectFlags.ObjectTypeKindMask)));
                                if ((c10 != 0))
                                {
                                    return c10;
                                }
                            }

                            if (((t1.objectFlags & ObjectFlags.ReverseMapped) != 0))
                            {
                                var r12 = t1.AsReverseMappedType();
                                var r22 = t2.AsReverseMappedType();
                                {
                                    var c11 = CompareTypes(r12.source, r22.source);
                                    if ((c11 != 0))
                                    {
                                        return c11;
                                    }
                                }

                                {
                                    var c12 = CompareTypes(r12.mappedType, r22.mappedType);
                                    if ((c12 != 0))
                                    {
                                        return c12;
                                    }
                                }

                                {
                                    var c13 = CompareTypes(r12.constraintType, r22.constraintType);
                                    if ((c13 != 0))
                                    {
                                        return c13;
                                    }
                                }
                            }

                            var m1 = t1.AsObjectType().mapper;
                            var m2 = t2.AsObjectType().mapper;
                            if (((t1.objectFlags & ObjectFlags.Mapped) != 0))
                            {
                                if ((m1 != null))
                                {
                                    m1 = ((CompositeTypeMapper)m1.data).m2;
                                }

                                if ((m2 != null))
                                {
                                    m2 = ((CompositeTypeMapper)m2.data).m2;
                                }
                            }

                            {
                                var c14 = compareTypeMappers(m1, m2);
                                if ((c14 != 0))
                                {
                                    return c14;
                                }
                            }
                        }
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.Union) != 0):
            {
                var o1 = t1.AsUnionType().origin;
                var o2 = t2.AsUnionType().origin;
                if (((o1 == null) && (o2 == null)))
                {
                    {
                        var c15 = compareTypeLists(t1.Types(), t2.Types());
                        if ((c15 != 0))
                        {
                            return c15;
                        }
                    }
                }
                else
                {
                    if ((o1 == null))
                    {
                        return 1;
                    }
                    else
                    {
                        if ((o2 == null))
                        {
                            return -1;
                        }
                        else
                        {
                            {
                                var c16 = CompareTypes(o1, o2);
                                if ((c16 != 0))
                                {
                                    return c16;
                                }
                            }
                        }
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.Intersection) != 0):
            {
                {
                    var c17 = compareTypeLists(t1.Types(), t2.Types());
                    if ((c17 != 0))
                    {
                        return c17;
                    }
                }

                break;
            }

            case true when ((t1.flags & (((TypeFlags.Enum | TypeFlags.EnumLiteral) | TypeFlags.UniqueESSymbol))) != 0):
            {
                {
                    var c18 = t1.checker.compareSymbols(t1.symbol, t2.symbol);
                    if ((c18 != 0))
                    {
                        return c18;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.StringLiteral) != 0):
            {
                {
                    var c19 = ByteStringKey.FromValue(t1.AsLiteralType().StringValue()).CompareTo(ByteStringKey.FromValue(t2.AsLiteralType().StringValue()));
                    if ((c19 != 0))
                    {
                        return c19;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.NumberLiteral) != 0):
            {
                {
                    var c20 = ((JsNum.Number)t1.AsLiteralType().value).Value.CompareTo(((JsNum.Number)t2.AsLiteralType().value).Value);
                    if ((c20 != 0))
                    {
                        return c20;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.BigIntLiteral) != 0):
            {
                {
                    var c21 = getBigIntLiteralValue(t1).Compare(getBigIntLiteralValue(t2));
                    if ((c21 != 0))
                    {
                        return c21;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.BooleanLiteral) != 0):
            {
                var b1 = ((bool)t1.AsLiteralType().value);
                var b2 = ((bool)t2.AsLiteralType().value);
                if ((b1 != b2))
                {
                    if (b1)
                    {
                        return 1;
                    }

                    return -1;
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.TypeParameter) != 0):
            {
                {
                    var c22 = t1.checker.compareSymbols(t1.symbol, t2.symbol);
                    if ((c22 != 0))
                    {
                        return c22;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.Index) != 0):
            {
                {
                    var c23 = CompareTypes(t1.AsIndexType().target, t2.AsIndexType().target);
                    if ((c23 != 0))
                    {
                        return c23;
                    }
                }

                {
                    var c24 = ((int)(t1.AsIndexType().indexFlags) - (int)(t2.AsIndexType().indexFlags));
                    if ((c24 != 0))
                    {
                        return c24;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.IndexedAccess) != 0):
            {
                {
                    var c25 = CompareTypes(t1.AsIndexedAccessType().objectType, t2.AsIndexedAccessType().objectType);
                    if ((c25 != 0))
                    {
                        return c25;
                    }
                }

                {
                    var c26 = CompareTypes(t1.AsIndexedAccessType().indexType, t2.AsIndexedAccessType().indexType);
                    if ((c26 != 0))
                    {
                        return c26;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.Conditional) != 0):
            {
                {
                    var c27 = t1.checker.compareNodes(t1.AsConditionalType().root.node.AsNode(), t2.AsConditionalType().root.node.AsNode());
                    if ((c27 != 0))
                    {
                        return c27;
                    }
                }

                {
                    var c28 = compareTypeMappers(t1.AsConditionalType().mapper, t2.AsConditionalType().mapper);
                    if ((c28 != 0))
                    {
                        return c28;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.Substitution) != 0):
            {
                {
                    var c29 = CompareTypes(t1.AsSubstitutionType().baseType, t2.AsSubstitutionType().baseType);
                    if ((c29 != 0))
                    {
                        return c29;
                    }
                }

                {
                    var c30 = CompareTypes(t1.AsSubstitutionType().constraint, t2.AsSubstitutionType().constraint);
                    if ((c30 != 0))
                    {
                        return c30;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.TemplateLiteral) != 0):
            {
                {
                    var c31 = compareTemplateTexts(t1.AsTemplateLiteralType().texts, t2.AsTemplateLiteralType().texts);
                    if ((c31 != 0))
                    {
                        return c31;
                    }
                }

                {
                    var c32 = compareTypeLists(t1.AsTemplateLiteralType().types, t2.AsTemplateLiteralType().types);
                    if ((c32 != 0))
                    {
                        return c32;
                    }
                }

                break;
            }

            case true when ((t1.flags & TypeFlags.StringMapping) != 0):
            {
                var targetOrder = CompareTypes(t1.AsStringMappingType().target, t2.AsStringMappingType().target);
                if (targetOrder != 0)
                    return targetOrder;
                break;
            }
        }

        return ((int)(t1.id) - (int)(t2.id));
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

    private static int compareTypeLists(Slice<Type> left, Slice<Type> right) =>
        compareTypeLists(left.AsSpan(), right.AsSpan());
    private static int compareTypeLists(List<Type> left, List<Type> right) =>
        compareTypeLists(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(left), System.Runtime.InteropServices.CollectionsMarshal.AsSpan(right));
    private static int compareTypeLists(ReadOnlySpan<Type> left, ReadOnlySpan<Type> right)
    {
        if ((left.Length) != (right.Length))
            return (left.Length) - (right.Length);
        for (int i = 0; i < (left.Length); i++)
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
            return compareTypeLists(t1.alias?.TypeArguments() ?? default, t2.alias?.TypeArguments() ?? default);
        if (s1 == null)
            return 1;
        if (s2 == null)
            return -1;
        var result = compareSymbolNames(s1.NameValue, s2.NameValue);
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
        int leftCount = left.Declarations.Count, rightCount = right.Declarations.Count;
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
        var leftName = left.NameValue;
        var rightName = right.NameValue;
        int result = compareSymbolNames(leftName, rightName);
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
        var leftFile = Ast.GetSourceFileOfNode(left);
        var rightFile = Ast.GetSourceFileOfNode(right);
        if (leftFile != rightFile)
            return fileIndexMap.GetValueOrDefault(leftFile) - fileIndexMap.GetValueOrDefault(rightFile);
        return left.Pos() - right.Pos();
    }
}

public sealed partial class Checker
{
    // utilities.go:260.
    private static bool isTypeAlias(Node node) => node.Kind is Kind.TypeAliasDeclaration or Kind.JSTypeAliasDeclaration;
}

public sealed partial class Checker
{
    internal void sortSymbols(List<Symbol> symbols) => symbols.Sort(compareSymbolsForSort);
    internal void sortSymbols(Slice<Symbol> symbols) => GoSlices.SortFunc(symbols, compareSymbolsForSort);
    internal static bool isReservedMemberName(string name) => name.Length >= 2 && name[0] == '\u00FE' && name[1] != '@' && name[1] != '#';
    internal static bool isReservedMemberName(ByteStringKey name) => isReservedMemberName(name.Value);
    internal static bool isReservedMemberName(ByteStringValue name)
    {
        if (name.IsNull) throw new NullReferenceException();
        if (name.Length < 2) return false;
        return name.IsBytes
            ? name.ByteSpan[0] == 0xFE && name.ByteSpan[1] != (byte)'@' && name.ByteSpan[1] != (byte)'#'
            : name.CharSpan[0] == '\u00FE' && name.CharSpan[1] != '@' && name.CharSpan[1] != '#';
    }
}

public sealed partial class Checker
{
    internal static Diagnostic NewDiagnosticForNode(Node node, Message message, params object[] args)
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
    private static Slice<Node> getMembersOfDeclaration(Node node) => node.Kind switch
    {
        Kind.InterfaceDeclaration or Kind.ClassDeclaration or Kind.ClassExpression or Kind.TypeLiteral => node.Members(),
        Kind.ObjectLiteralExpression => node.Properties(),
        _ => default
    };
}

public sealed partial class Checker
{
    internal static ModifierFlags getDeclarationModifierFlagsFromSymbol(Symbol symbol) => getDeclarationModifierFlagsFromSymbolEx(symbol, false);
    private static ModifierFlags getDeclarationModifierFlagsFromSymbolEx(Symbol symbol, bool isWrite)
    {
        if ((symbol.CheckFlags & CheckFlags.Synthetic) != 0)
        {
            var access = (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsPublic) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWritePublic) != 0) ? ModifierFlags.Public : (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsProtected) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWriteProtected) != 0) ? ModifierFlags.Protected : (!isWrite && (symbol.CheckFlags & CheckFlags.ContainsPrivate) != 0 || isWrite && (symbol.CheckFlags & CheckFlags.ContainsWritePrivate) != 0) ? ModifierFlags.Private : ModifierFlags.None;
            return (symbol.CheckFlags & CheckFlags.ContainsStatic) != 0 ? access | ModifierFlags.Static : access;
        }

        if (symbol.ValueDeclaration != null)
        {
            Node declaration = null;
            if (isWrite)
                declaration = Core.Find(symbol.Declarations, Ast.IsSetAccessorDeclaration);
            if (declaration == null && (symbol.Flags & SymbolFlags.GetAccessor) != 0)
                declaration = Core.Find(symbol.Declarations, Ast.IsGetAccessorDeclaration);
            declaration ??= symbol.ValueDeclaration;
            var flags = Ast.GetCombinedModifierFlags(declaration);
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
    private static string getPropertyNameFromType(Type t) => getPropertyNameValueFromType(t).Materialize();
    internal static ByteStringValue getPropertyNameValueFromType(Type t)
    {
        if ((t.flags & TypeFlags.StringLiteral) != 0)
            return t.AsLiteralType().StringValue();
        if ((t.flags & TypeFlags.NumberLiteral) != 0)
            return ByteStringValue.FromString(((JsNum.Number)t.AsLiteralType().value).String());
        if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
            return ByteStringValue.FromString(t.AsUniqueESSymbolType().name);
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
    private static int compareSymbolNames(ByteStringValue left, ByteStringValue right) =>
        left.IsNull ? (right.IsNull ? 0 : -1) : right.IsNull ? 1 :
        ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right));

    // utilities.go:1022. Internal prefix contains a raw 0xFE byte.
    internal static bool IsPrivateIdentifierSymbol(Symbol symbol) => symbol != null && symbol.NameValue.StartsWith([0xFE, (byte)'#']);
    private static bool IsKnownSymbol(Symbol symbol) => isLateBoundName(symbol.NameValue);
    internal static bool isLateBoundName(string name) => name.Length >= 2 && name[0] == '\u00FE' && name[1] == '@';
    // utilities.go:1029. Internal symbol names are raw bytes, including 0xFE.
    internal static bool isLateBoundName(ByteStringValue name)
    {
        if (name.IsNull) throw new NullReferenceException();
        return name.Length >= 2 && (name.IsBytes
            ? name.ByteSpan[0] == 0xFE && name.ByteSpan[1] == (byte)'@'
            : name.CharSpan[0] == '\u00FE' && name.CharSpan[1] == '@');
    }
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
    internal static bool isNumericLiteralName(string name) => JsNum.FromString(name).String() == name;
    internal static bool isNumericLiteralName(ByteStringValue name) => name.EqualsString(JsNum.FromString(name).String());
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
    private static Slice<Node> getAllJSDocTags(Node node)
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

        return default;
    }
}

public sealed partial class Checker
{
    // utilities.go:1316: range captures each value before invoking getValue.
    private static (int, int) minAndMax<T>(Slice<T> slice, Func<T, int> getValue)
    {
        int minValue = default;
        int maxValue = default;
        foreach (var (i, element) in GoRuntime.Range(slice))
        {
            var value = getValue(element);
            if (i == 0)
                minValue = maxValue = value;
            else
            {
                minValue = Math.Min(minValue, value);
                maxValue = Math.Max(maxValue, value);
            }
        }
        return (minValue, maxValue);
    }

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
            var constraint = this.getResolvedBaseConstraint(t, default);
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

public sealed partial class Checker
{
    // utilities.go:1033. Array literals carry the same inference-candidate marker
    // semantics as object literals.
    private static bool isObjectOrArrayLiteralType(Type t) => (t.objectFlags & (ObjectFlags.ObjectLiteral | ObjectFlags.ArrayLiteral)) != 0;
}

public sealed partial class Checker
{
    // upstream checker.go:27061. Preserve the union-only constituent callback boundary.
    private static void forEachType(Type t, Action<Type> callback)
    {
        if ((t.flags & TypeFlags.Union) != 0)
        {
            foreach (var constituent in GoRuntime.RangeValues(t.Types()))
                callback(constituent);
        }
        else
            callback(t);
    }

    // upstream checker.go:25054. Concrete unary tuple wrapper used by implied constraints.
    private static bool isUnaryTupleTypeNode(Node node) => Ast.IsTupleTypeNode(node) && (node.Elements().Count) == 1;
}

public sealed partial class Checker
{
    // utilities.go:970. Numeric placeholder validation retains the actual JS-number coercion.
    private static bool isValidNumberString(string text, bool roundTripOnly) =>
        isValidNumberString(ByteStringValue.FromString(text), roundTripOnly);
    private static bool isValidNumberString(ByteStringValue text, bool roundTripOnly)
    {
        if (!text.IsNull && text.Length == 0)
            return false;
        var number = JsNum.FromString(text);
        return !number.IsNaN() && !number.IsInf() && (!roundTripOnly || text.EqualsString(number.String()));
    }

    // utilities.go:978. Validate full raw byte-string bigint spelling through the original scanner.
    private static bool isValidBigIntString(string text, bool roundTripOnly) =>
        isValidBigIntString(ByteStringValue.FromString(text), roundTripOnly);
    private static bool isValidBigIntString(ByteStringValue text, bool roundTripOnly)
    {
        if (!text.IsNull && text.Length == 0)
            return false;
        var scanner = new TypeScript.Scanner.Scanner();
        scanner.SetSkipTrivia(false);
        bool success = true;
        scanner.SetOnError((message, start, length, args) => success = false);
        // utilities.go:978 owns text+"n". Build that required byte buffer directly;
        // SetByteTextInternal transfers it without another public-ingress copy.
        var bytes = new byte[text.Length + 1];
        if (text.IsBytes) text.ByteSpan.CopyTo(bytes);
        else System.Text.Encoding.Latin1.GetBytes(text.CharSpan, bytes);
        bytes[^1] = (byte)'n';
        scanner.SetByteTextInternal(ByteStringView.FromOwnedBytes(bytes));
        var result = scanner.Scan();
        bool negative = result == Kind.MinusToken;
        if (negative)
            result = scanner.Scan();
        var flags = scanner.TokenFlags();
        return success && result == Kind.BigIntLiteral && scanner.TokenEnd() == text.Length + 1 && (flags & TokenFlags.ContainsSeparator) == 0 && (!roundTripOnly || text.EqualsString(pseudoBigIntToString(JsNum.NewPseudoBigInt(JsNum.ParsePseudoBigInt(scanner.TokenValueTextValue()), negative))));
    }
}

public sealed partial class Checker
{
    private static string tryGetPropertyAccessOrIdentifierToString(Node expr)
    {
        switch (true)
        {
            case true when Ast.IsPropertyAccessExpression(expr):
            {
                var baseStr = tryGetPropertyAccessOrIdentifierToString(expr.Expression());
                if ((baseStr != ""))
                {
                    return ((baseStr + ".") + entityNameToString(expr.Name()));
                }

                break;
            }

            case true when Ast.IsElementAccessExpression(expr):
            {
                var baseStr2 = tryGetPropertyAccessOrIdentifierToString(expr.Expression());
                if (((baseStr2 != "") && Ast.IsPropertyName(expr.AsElementAccessExpression().ArgumentExpression)))
                {
                    return ((baseStr2 + ".") + Ast.GetPropertyNameForPropertyNameNode(expr.AsElementAccessExpression().ArgumentExpression));
                }

                break;
            }

            case true when Ast.IsIdentifier(expr):
            {
                return expr.Text();
            }

            case true when Ast.IsJsxNamespacedName(expr):
            {
                return entityNameToString(expr);
            }
        }

        return "";
    }
}

public sealed partial class Checker
{
    // utilities.go:264. Binding-element cases are demanded by original library declarations.
    private static bool hasOnlyExpressionInitializer(Node node) => node.Kind is Kind.VariableDeclaration or Kind.Parameter or Kind.BindingElement or Kind.PropertyDeclaration or Kind.PropertyAssignment or Kind.EnumMember;
}

public sealed partial class Checker
{
    // utilities.go:1304.
    private static Slice<Node> getDeclarationsOfKind(Symbol symbol, Kind kind) => Core.Filter(symbol.Declarations, declaration => declaration.Kind == kind);
}

public sealed partial class Checker
{
    private static int compareTupleTypes(TupleType t1, TupleType t2)
    {
        if ((t1 == t2))
        {
            return 0;
        }

        if ((t1.@readonly != t2.@readonly))
        {
            return Core.IfElse(t1.@readonly, 1, -1);
        }

        if ((GoRuntime.Len(t1.elementInfos) != GoRuntime.Len(t2.elementInfos)))
        {
            return (GoRuntime.Len(t1.elementInfos) - GoRuntime.Len(t2.elementInfos));
        }

        {
            foreach (var i in GoRuntime.RangeKeys(t1.elementInfos))
            {
                {
                    var c = ((int)(t1.elementInfos[i].flags) - (int)(t2.elementInfos[i].flags));
                    if ((c != 0))
                    {
                        return c;
                    }
                }
            }
        }

        {
            foreach (var i2 in GoRuntime.RangeKeys(t1.elementInfos))
            {
                {
                    var c2 = compareElementLabels(t1.elementInfos[i2].labeledDeclaration, t2.elementInfos[i2].labeledDeclaration);
                    if ((c2 != 0))
                    {
                        return c2;
                    }
                }
            }
        }

        return 0;
    }
}

public sealed partial class Checker
{
    private static int compareElementLabels(Node n1, Node n2)
    {
        if ((n1 == n2))
        {
            return 0;
        }

        if ((n1 == null))
        {
            return -1;
        }

        if ((n2 == null))
        {
            return 1;
        }

        return string.CompareOrdinal(n1.Name().Text(), n2.Name().Text());
    }
}

public sealed partial class Checker
{
    // slices.Compare adaptation: lexicographic raw-string comparison.
    private static int compareTemplateTexts(Slice<ByteStringValue> left, Slice<ByteStringValue> right)
    {
        int count = Math.Min(GoRuntime.Len(left), GoRuntime.Len(right));
        for (int i = 0; i < count; i++)
        {
            int result = compareSymbolNames(left[i], right[i]);
            if (result != 0)
                return result;
        }

        return GoRuntime.Len(left) - GoRuntime.Len(right);
    }
}

public sealed partial class Checker
{
    private static bool isShorthandAmbientModuleSymbol(Symbol moduleSymbol)
    {
        return isShorthandAmbientModule(moduleSymbol.ValueDeclaration);
    }
}

public sealed partial class Checker
{
    private static bool isSyntacticDefault(Node node)
    {
        return (((((Ast.IsExportAssignment(node) && !node.AsExportAssignment().IsExportEquals)) || Ast.HasSyntacticModifier(node, ModifierFlags.Default)) || Ast.IsExportSpecifier(node)) || Ast.IsNamespaceExport(node));
    }
}

public sealed partial class Checker
{
    private static bool hasExportAssignmentSymbol(Symbol moduleSymbol)
    {
        return (moduleSymbol.Exports?[Ast.InternalSymbolNameExportEquals] != null);
    }
}

public sealed partial class Checker
{
    // utilities.go:201.
    private static bool isShorthandAmbientModule(Node node) => node != null && node.Kind == Kind.ModuleDeclaration && node.Body() == null;
    // utilities.go:206.
    private static Node getAliasDeclarationFromName(Node node)
    {
        switch (node.Parent.Kind)
        {
            case Kind.ImportClause:
            case Kind.ImportSpecifier:
            case Kind.NamespaceImport:
            case Kind.ExportSpecifier:
            case Kind.ExportAssignment:
            case Kind.ImportEqualsDeclaration:
            case Kind.NamespaceExport:
                return node.Parent;
            case Kind.QualifiedName:
                return getAliasDeclarationFromName(node.Parent);
        }

        return null;
    }

    // utilities.go:230.
    private static bool isSideEffectImport(Node node)
    {
        var ancestor = Ast.FindAncestor(node, Ast.IsImportDeclaration);
        return ancestor != null && ancestor.ImportClause() == null;
    }

    // utilities.go:235. CommonJS source is outside the fixed project boundary.
    private static Node getExternalModuleRequireArgument(Node node)
    {
        if (Ast.IsVariableDeclarationInitializedToRequire(node))
            return node.Initializer().Arguments()[0];
        return null;
    }
}

public sealed partial class Checker
{
    private static bool isTopLevelInExternalModuleAugmentation(Node node)
    {
        return ((((node != null) && (node.Parent != null)) && Ast.IsModuleBlock(node.Parent)) && Ast.IsExternalModuleAugmentation(node.Parent.Parent));
    }
}

public sealed partial class Checker
{
    // Source slices.Collect(table.Values()) retains every entry, including reserved members.
    private static Slice<Symbol> collectSymbolValues(SymbolTable symbols)
    {
        Slice<Symbol> result = default;
        if (symbols != null)
            foreach (var symbol in symbols.Values)
                result = result.Append(symbol);
        return result;
    }

    // utilities.go:1690. Go map range + append preserve absent empty result.
    private static Slice<Symbol> symbolsToArray(SymbolTable symbols)
    {
        Slice<Symbol> result = default;
        if (symbols != null)
            foreach (var pair in symbols)
                if (!isReservedMemberName(pair.Key))
                    result = GoRuntime.Append(result, pair.Value);
        return result;
    }
}

public sealed partial class Checker
{
    // utilities.go:1749; printer.go:249 forwards to this same value formatter.
    internal static string ValueToString(object value) => value switch
    {
        ByteStringValue text => ValueToString(text.Materialize()),
        string text => "\"" + TypeScript.DiagnosticPrinting.Printer.EscapeString(text, '\"') + "\"",
        JsNum.Number number => number.String(),
        bool boolean => boolean ? "true" : "false",
        JsNum.PseudoBigInt bigint => bigint.String() + "n",
        _ => throw new InvalidOperationException("unhandled value type in valueToString")};
}

public sealed partial class Checker
{
    // utilities.go:1657. Exact containing import statement selection.
    private static Node getAnyImportSyntax(Node node) => node.Kind switch
    {
        Kind.ImportEqualsDeclaration => node,
        Kind.ImportClause => node.Parent,
        Kind.NamespaceImport => node.Parent.Parent,
        Kind.ImportSpecifier => node.Parent.Parent.Parent,
        _ => null
    };
}

public sealed partial class Checker
{
    // utilities.go:1609. Type-parameter list positions use source byte offsets.
    private static TextRange rangeOfTypeParameters(SourceFile sourceFile, NodeList typeParameters) =>
        new(typeParameters.Pos() - 1, Math.Min(sourceFile.TextView().Length,
            TypeScript.Scanner.Scanner.SkipTrivia(sourceFile.TextView(), typeParameters.End()) + 1));
    // utilities.go:1633. Declaration identity, rather than equivalent filenames.
    private static bool allDeclarationsInSameSourceFile(Symbol symbol)
    {
        if ((symbol.Declarations.Count) > 1)
        {
            var file = Ast.GetSourceFileOfNode(symbol.Declarations[0]);
            for (int i = 1; i < symbol.Declarations.Count; i++)
                if (Ast.GetSourceFileOfNode(symbol.Declarations[i]) != file)
                    return false;
        }
        return true;
    }

    // utilities.go:1914. A setter's optional this parameter precedes its value.
    private static Node GetSetAccessorValueParameter(Node accessor)
    {
        var parameters = accessor.Parameters();
        if ((parameters.Count) > 0)
            return parameters[parameters.Count == 2 && Ast.IsThisParameter(parameters[0]) ? 1 : 0];
        return null;
    }
}

public sealed partial class Checker
{
    // utilities.go:1037. Class decorators are evaluated outside their class scope.
    private static Node getContainingClassExcludingClassDecorators(Node node)
    {
        var decorator = Ast.FindAncestorOrQuit(node.Parent, ancestor =>
            Ast.IsClassLike(ancestor) ? FindAncestorResult.Quit :
            Ast.IsDecorator(ancestor) ? FindAncestorResult.True : FindAncestorResult.False);
        if (decorator != null && Ast.IsClassLike(decorator.Parent))
            return Ast.GetContainingClass(decorator.Parent);
        if (decorator != null)
            return Ast.GetContainingClass(decorator);
        return Ast.GetContainingClass(node);
    }
}

public sealed partial class Checker
{
    // utilities.go:1830–1905. Shared source details for initial module diagnostics;
    // incremental builder replay remains outside the declaration campaign.
    private readonly record struct DiagnosticDetails(Message Message, object[] Args);
    private static DiagnosticDetails CreateModuleNotFoundChain(TypeScript.Compiler.ICheckerProgramHost program,
        SourceFile file, string moduleReference, ResolutionMode mode, string packageName)
    {
        var resolved = program.GetResolvedModule(file, moduleReference, mode);
        if (resolved != null && resolved.AlternateResult != default)
        {
            if (resolved.AlternateResult.ContainsLowercaseDirectorySequence("/node_modules/@types/"))
                packageName = "@types/" + TypeScript.Modules.ModuleUtilities.MangleScopedPackageName(packageName);
            return new(Diagnostics.There_are_types_at_0_but_this_result_could_not_be_resolved_when_respecting_package_json_exports_The_1_library_may_need_to_update_its_package_json_or_typings,
                new object[] { resolved.AlternateResult, packageName });
        }
        var packages = program.GetPackagesMap();
        if (packages != null && packages.ContainsKey(TypeScript.Modules.ModuleUtilities.GetTypesPackageName(packageName)))
            return new(Diagnostics.If_the_0_package_actually_exposes_this_module_consider_sending_a_pull_request_to_amend_https_Colon_Slash_Slashgithub_com_SlashDefinitelyTyped_SlashDefinitelyTyped_Slashtree_Slashmaster_Slashtypes_Slash_1,
                new object[] { packageName, TypeScript.Modules.ModuleUtilities.MangleScopedPackageName(packageName) });
        if (packages != null && packages.GetValueOrDefault(packageName))
            return new(Diagnostics.If_the_0_package_actually_exposes_this_module_try_adding_a_new_declaration_d_ts_file_containing_declare_module_1,
                new object[] { packageName, moduleReference });
        return new(Diagnostics.Try_npm_i_save_dev_types_Slash_1_if_it_exists_or_add_a_new_declaration_d_ts_file_containing_declare_module_0,
            new object[] { moduleReference, TypeScript.Modules.ModuleUtilities.MangleScopedPackageName(packageName) });
    }

    private static DiagnosticDetails CreateModeMismatchDetails(TypeScript.Compiler.ICheckerProgramHost program, SourceFile file)
    {
        var extension = RootedFilePath.FromValue(file.FileNameGoValue()).Extension();
        var target = extension == TsPath.ExtensionTs ? TsPath.ExtensionMts : extension == TsPath.ExtensionJs ? TsPath.ExtensionMjs : "";
        var metadata = program.GetSourceFileMetaData(PathKey.FromValue(file.PathKeyValue()));
        if (metadata.PackageJsonDirectory != default && metadata.PackageJsonType == "")
        {
            string package = metadata.PackageJsonDirectory.ResolveFile("package.json").AsString();
            return target != ""
                ? new(Diagnostics.To_convert_this_file_to_an_ECMAScript_module_change_its_file_extension_to_0_or_add_the_field_type_Colon_module_to_1, new object[] { target, package })
                : new(Diagnostics.To_convert_this_file_to_an_ECMAScript_module_add_the_field_type_Colon_module_to_0, new object[] { package });
        }
        return target != ""
            ? new(Diagnostics.To_convert_this_file_to_an_ECMAScript_module_change_its_file_extension_to_0_or_create_a_local_package_json_file_with_type_Colon_module, new object[] { target })
            : new(Diagnostics.To_convert_this_file_to_an_ECMAScript_module_create_a_local_package_json_file_with_type_Colon_module, null);
    }
}

public sealed partial class Checker
{
    private static bool isClassInstanceProperty(Node node)
    {
        if ((Ast.IsInJSFile(node) && Ast.IsExpandoPropertyDeclaration(node)))
        {
            var left = node.AsBinaryExpression().Left;
            return (((!Ast.IsBindableStaticAccessExpression(left, false) || !Ast.IsPrototypeAccess(left.Expression()))) && !Ast.IsBindableStaticNameExpression(left, true));
        }
        return ((((node.Parent != null) && Ast.IsClassLike(node.Parent)) && Ast.IsPropertyDeclaration(node)) && !Ast.HasAccessorModifier(node));
    }
}

public sealed partial class Checker
{
    private static bool isThisInitializedObjectBindingExpression(Node node)
    {
        return (((((node != null) && ((Ast.IsShorthandPropertyAssignment(node) || Ast.IsPropertyAssignment(node)))) && Ast.IsBinaryExpression(node.Parent.Parent)) && (node.Parent.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken)) && (node.Parent.Parent.AsBinaryExpression().Right.Kind == Kind.ThisKeyword));
    }
}

public sealed partial class Checker
{
    private static bool isThisInitializedDeclaration(Node node)
    {
        return ((((node != null) && Ast.IsVariableDeclaration(node)) && (node.Initializer() != null)) && (node.Initializer().Kind == Kind.ThisKeyword));
    }
}

public sealed partial class Checker
{
    private static Node getBindingElementPropertyName(Node node)
    {
        return node.PropertyNameOrName();
    }
}

public sealed partial class Checker
{
    private static Node getThisParameterFromNodeContext(Node node)
    {
        var thisContainer = Ast.GetThisContainer(node, false, false);
        if (((thisContainer != null) && Ast.IsFunctionLike(thisContainer)))
        {
            return Ast.GetThisParameter(thisContainer);
        }
        return null;
    }
}

public sealed partial class Checker
{
    private static bool nodeStartsNewLexicalEnvironment(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Constructor:
            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.ModuleDeclaration:
            case Kind.SourceFile:
            {
                return true;
            }
        }
        return false;
    }

    // utilities.go:1159. Namespaced JSX names are intrinsic, irrespective of prefix case.
    private static bool isJsxIntrinsicTagName(Node tagName)
    {
        if (Ast.IsIdentifier(tagName))
        {
            var text = tagName.TextValue();
            if (text.IsNull) throw new NullReferenceException();
            if (text.Length != 0 && (text.IsBytes
                ? text.ByteSpan[0] is >= (byte)'a' and <= (byte)'z' || text.ByteSpan.Contains((byte)'-')
                : text.CharSpan[0] is >= 'a' and <= 'z' || text.CharSpan.Contains('-')))
                return true;
        }
        return Ast.IsJsxNamespacedName(tagName);
    }
}
