// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/relater.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Scalar/literal/boolean and undefined-union relationships only.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private sealed class Relation
    {
    }

    private readonly Relation assignableRelation = new(), identityRelation = new();
    private bool isTypeAssignableTo(Type source, Type target) => isTypeRelatedTo(source, target, assignableRelation);
    private bool isTypeIdenticalTo(Type source, Type target) => isTypeRelatedTo(source, target, identityRelation);
    // isTypeRelatedTo (169): freshness, simple comparison, structured scalar union path.
    private bool isTypeRelatedTo(Type source, Type target, Relation relation)
    {
        if (isFreshLiteralType(source))
            source = source.AsLiteralType().regularType;
        if (isFreshLiteralType(target))
            target = target.AsLiteralType().regularType;
        if (source == target)
            return true;
        if (relation != identityRelation)
        {
            if (isSimpleTypeRelatedTo(source, target, relation))
                return true;
        }
        else if (((source.flags | target.flags) & (TypeFlags.UnionOrIntersection | TypeFlags.IndexedAccess | TypeFlags.Conditional | TypeFlags.Substitution)) == 0)
        {
            if (source.flags != target.flags)
                return false;
            if ((source.flags & TypeFlags.Singleton) != 0)
                return true;
        }

        if ((source.flags & TypeFlags.Object) != 0 || (target.flags & TypeFlags.Object) != 0)
            throw unsupported("isTypeRelatedTo: object types");
        if (((source.flags | target.flags) & TypeFlags.StructuredOrInstantiable) != 0)
            return checkTypeRelatedToEx(source, target, relation, null);
        return false;
    }

    // isSimpleTypeRelatedTo (205): selected primitive branches, enum/object throw.
    private bool isSimpleTypeRelatedTo(Type source, Type target, Relation relation)
    {
        var s = source.flags;
        var t = target.flags;
        if ((t & TypeFlags.Any) != 0 || (s & TypeFlags.Never) != 0 || source == wildcardType)
            return true;
        if ((t & TypeFlags.Unknown) != 0)
            return true;
        if ((t & TypeFlags.Never) != 0)
            return false;
        if ((s & TypeFlags.StringLike) != 0 && (t & TypeFlags.String) != 0)
            return true;
        if (((s | t) & TypeFlags.EnumLike) != 0)
            throw unsupported("isSimpleTypeRelatedTo: enum");
        if ((s & TypeFlags.NumberLike) != 0 && (t & TypeFlags.Number) != 0)
            return true;
        if ((s & TypeFlags.BigIntLike) != 0 && (t & TypeFlags.BigInt) != 0)
            return true;
        if ((s & TypeFlags.BooleanLike) != 0 && (t & TypeFlags.Boolean) != 0)
            return true;
        if ((s & TypeFlags.ESSymbolLike) != 0 && (t & TypeFlags.ESSymbol) != 0)
            return true;
        if ((s & TypeFlags.Undefined) != 0 && (!strictNullChecks && (t & TypeFlags.UnionOrIntersection) == 0 || (t & (TypeFlags.Undefined | TypeFlags.Void)) != 0))
            return true;
        if ((s & TypeFlags.Null) != 0 && (!strictNullChecks && (t & TypeFlags.UnionOrIntersection) == 0 || (t & TypeFlags.Null) != 0))
            return true;
        if ((s & TypeFlags.Object) != 0 && (t & TypeFlags.NonPrimitive) != 0)
            throw unsupported("isSimpleTypeRelatedTo: object/nonprimitive");
        if (relation == assignableRelation)
        {
            if ((s & TypeFlags.Any) != 0)
                return true;
            if (isUnknownLikeUnionType(target))
                return true;
        }

        return false;
    }

    private bool checkTypeAssignableToAndOptionallyElaborate(Type source, Type target, Node errorNode, Node expr)
    {
        if (isTypeRelatedTo(source, target, assignableRelation))
            return true;
        if (errorNode != null && !elaborateError(expr, source, target))
            return checkTypeRelatedToEx(source, target, assignableRelation, errorNode);
        return false;
    }

    private bool elaborateError(Node node, Type source, Type target)
    {
        if (node == null)
            return false;
        if (((source.flags | target.flags) & (TypeFlags.Object | TypeFlags.Conditional | TypeFlags.Instantiable)) != 0)
            throw unsupported("elaborateError: structured/callable/conditional types");
        // elaborateDidYouMeanToCallOrConstruct has no signatures for scalar types.
        var expr = Ast.SkipParentheses(node);
        switch (expr.Kind)
        {
            case Kind.Identifier:
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                return false;
            default:
                throw unsupported("elaborateError: expression elaboration");
        }
    }

    // checkTypeRelatedToEx (358): no overflow/alias help on scalar relations.
    private bool checkTypeRelatedToEx(Type source, Type target, Relation relation, Node errorNode)
    {
        var r = new Relater(this)
        {
            relation = relation,
            errorNode = errorNode
        };
        bool result = r.isRelatedToEx(source, target, errorNode != null);
        if (r.errorChain != null)
            diagnostics.Add(createDiagnosticChainFromErrorChain(r.errorChain, errorNode));
        return result;
    }

    private sealed class ErrorChain
    {
        internal Message message;
        internal object[] args;
        internal ErrorChain next;
    }

    private static Diagnostic createDiagnosticChainFromErrorChain(ErrorChain chain, Node errorNode)
    {
        if (chain == null)
            return null;
        if (chain.next != null)
            throw unsupported("createDiagnosticChainFromErrorChain: nonprimitive chain");
        return NewDiagnosticForNode(errorNode, chain.message, chain.args);
    }

    private sealed class Relater
    {
        private readonly Checker c;
        internal Relation relation;
        internal Node errorNode;
        internal ErrorChain errorChain;
        internal Relater(Checker checker) => c = checker;
        // isRelatedToEx (2633), retained normalization/simple/small-union/error branches.
        internal bool isRelatedToEx(Type originalSource, Type originalTarget, bool reportErrors)
        {
            if (originalSource == originalTarget)
                return true;
            var source = c.getNormalizedType(originalSource, false);
            var target = c.getNormalizedType(originalTarget, true);
            if (source == target)
                return true;
            if (relation == c.identityRelation)
            {
                if (source.flags != target.flags)
                    return false;
                if ((source.flags & TypeFlags.Singleton) != 0)
                    return true;
                throw unsupported("Relater.isRelatedToEx: recursive identity relation");
            }

            if ((source.flags & TypeFlags.DefinitelyNonNullable) != 0 && (target.flags & TypeFlags.Union) != 0)
            {
                var types = target.Types();
                Type candidate = null;
                if (types.Count == 2 && (types[0].flags & TypeFlags.Nullable) != 0)
                    candidate = types[1];
                else if (types.Count == 3 && (types[0].flags & TypeFlags.Nullable) != 0 && (types[1].flags & TypeFlags.Nullable) != 0)
                    candidate = types[2];
                if (candidate != null && (candidate.flags & TypeFlags.Nullable) == 0)
                {
                    target = c.getNormalizedType(candidate, true);
                    if (source == target)
                        return true;
                }
            }

            if (c.isSimpleTypeRelatedTo(source, target, relation))
                return true;
            if (((source.flags | target.flags) & TypeFlags.StructuredOrInstantiable) != 0)
            {
                if (((source.flags | target.flags) & (TypeFlags.Object | TypeFlags.Intersection | TypeFlags.Instantiable)) != 0)
                    throw unsupported("Relater.isRelatedToEx: structured relationship");
                bool skipCaching = (source.flags & TypeFlags.Union) != 0 && source.Types().Count < 4 && (target.flags & TypeFlags.Union) == 0 || (target.flags & TypeFlags.Union) != 0 && target.Types().Count < 4 && (source.flags & TypeFlags.StructuredOrInstantiable) == 0;
                if (!skipCaching)
                    throw unsupported("Relater.isRelatedToEx: recursive relation cache");
                if (unionOrIntersectionRelatedTo(source, target, reportErrors))
                    return true;
            }

            if (reportErrors)
                reportErrorResults(originalSource, originalTarget, source, target);
            return false;
        }

        private bool unionOrIntersectionRelatedTo(Type source, Type target, bool reportErrors)
        {
            if ((source.flags & TypeFlags.Union) != 0)
                return eachTypeRelatedToType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0);
            if ((target.flags & TypeFlags.Union) != 0)
                return typeRelatedToSomeType(source, target, reportErrors && (source.flags & TypeFlags.Primitive) == 0 && (target.flags & TypeFlags.Primitive) == 0);
            throw unsupported("unionOrIntersectionRelatedTo: intersection");
        }

        private bool eachTypeRelatedToType(Type source, Type target, bool reportErrors)
        {
            // Admitted unions have at most three scalars; source/target union recursive
            // comparisons are guarded by isRelatedToEx before this path.
            foreach (var sourceType in source.Types())
                if (!isRelatedToEx(sourceType, target, reportErrors))
                    return false;
            return true;
        }

        private bool typeRelatedToSomeType(Type source, Type target, bool reportErrors)
        {
            var targetTypes = target.Types();
            if (targetTypes.Contains(source))
                return true;
            if ((target.objectFlags & ObjectFlags.PrimitiveUnion) != 0 && (source.flags & TypeFlags.EnumLiteral) == 0 && (source.flags & (TypeFlags.StringLiteral | TypeFlags.BooleanLiteral | TypeFlags.BigIntLiteral)) != 0)
            {
                var literal = source.AsLiteralType();
                var alternate = source == literal.regularType ? literal.freshType : literal.regularType;
                Type primitive = null;
                if ((source.flags & TypeFlags.StringLiteral) != 0)
                    primitive = c.stringType;
                else if ((source.flags & TypeFlags.BigIntLiteral) != 0)
                    primitive = c.bigintType;
                if (primitive != null && targetTypes.Contains(primitive) || alternate != null && targetTypes.Contains(alternate))
                    return true;
                return false;
            }

            // getMatchingUnionConstituentForType only applies object discriminants.
            foreach (var t in targetTypes)
                if (isRelatedToEx(source, t, false))
                    return true;
            if (reportErrors)
                throw unsupported("typeRelatedToSomeType: best-match elaboration");
            return false;
        }

        private void reportErrorResults(Type originalSource, Type originalTarget, Type source, Type target)
        {
            if (originalSource.alias != null || originalTarget.alias != null || ((source.flags | target.flags) & (TypeFlags.Object | TypeFlags.Intersection | TypeFlags.TypeParameter)) != 0)
                throw unsupported("reportErrorResults: named/structured error elaboration");
            reportRelationError(source, target);
        }

        private void reportRelationError(Type source, Type target)
        {
            var(sourceType, targetType) = c.getTypeNamesForErrorDisplay(source, target);
            var generalizedSourceType = sourceType;
            if ((target.flags & TypeFlags.Never) == 0 && isLiteralType(source) && !c.typeCouldHaveTopLevelSingletonTypes(target))
                generalizedSourceType = c.typeToStringByteString(c.getBaseTypeOfLiteralType(source));
            Message message;
            if (sourceType == targetType)
                message = Diagnostics.Type_0_is_not_assignable_to_type_1_Two_different_types_with_this_name_exist_but_they_are_unrelated;
            else
                message = Diagnostics.Type_0_is_not_assignable_to_type_1;
            errorChain = new ErrorChain
            {
                message = message,
                args = [generalizedSourceType, targetType],
                next = errorChain
            };
        }
    }

    private (string, string) getTypeNamesForErrorDisplay(Type left, Type right)
    {
        if (left.symbol != null || right.symbol != null)
            throw unsupported("getTypeNamesForErrorDisplay: contextual named symbols");
        return (typeToStringByteString(left), typeToStringByteString(right));
    }

    private bool typeCouldHaveTopLevelSingletonTypes(Type t)
    {
        if ((t.flags & TypeFlags.Boolean) != 0)
            return false;
        if ((t.flags & TypeFlags.Union) != 0)
            return t.Types().Any(typeCouldHaveTopLevelSingletonTypes);
        if ((t.flags & (TypeFlags.Instantiable | TypeFlags.Intersection)) != 0)
            throw unsupported("typeCouldHaveTopLevelSingletonTypes: constrained types");
        return isUnitType(t) || (t.flags & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0;
    }
}
