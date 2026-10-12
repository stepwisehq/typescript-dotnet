// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Remaining pinned exports.go leaves. Existing accepted public ownership wrappers retained.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:8:1.
    internal Type GetStringType()
    {
        return this.stringType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:12:1.
    internal Type GetNumberType()
    {
        return this.numberType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:16:1.
    internal Type GetBooleanType()
    {
        return this.booleanType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:20:1.
    internal Type GetVoidType()
    {
        return this.voidType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:24:1.
    internal Type GetUndefinedType()
    {
        return this.undefinedType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:28:1.
    internal Type GetNullType()
    {
        return this.nullType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:32:1.
    internal Type GetAnyType()
    {
        return this.anyType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:36:1.
    internal Type GetErrorType()
    {
        return this.errorType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:40:1.
    internal Type GetNeverType()
    {
        return this.neverType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:44:1.
    internal Type GetUnknownType()
    {
        return this.unknownType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:48:1.
    internal Type GetBigIntType()
    {
        return this.bigintType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:52:1.
    internal Type GetESSymbolType()
    {
        return this.esSymbolType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:56:1.
    internal Type GetNonPrimitiveType()
    {
        return this.nonPrimitiveType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:60:1.
    internal Type GetBaseTypeOfLiteralType(Type t)
    {
        return this.getBaseTypeOfLiteralType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:64:1.
    internal Symbol GetUnknownSymbol()
    {
        return this.unknownSymbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:68:1.
    internal Symbol GetUndefinedSymbol()
    {
        return this.undefinedSymbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:72:1.
    internal Symbol GetArgumentsSymbol()
    {
        return this.argumentsSymbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:76:1.
    internal Signature GetUnknownSignature()
    {
        return this.unknownSignature;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:80:1.
    internal Type GetUnionType(Slice<Type> types)
    {
        return this.getUnionType(types);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:84:1.
    internal Type GetNameTypeOfSymbol(Symbol symbol)
    {
        {
            var links = this.valueSymbolLinks.TryGet(symbol);
            if ((!links.IsEmpty))
            {
                return links.Span[0].nameType;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:91:1.
    internal static bool IsTypeUsableAsPropertyName(Type t)
    {
        return isTypeUsableAsPropertyName(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:95:1.
    internal static string GetPropertyNameFromType(Type t)
    {
        return getPropertyNameFromType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:99:1.
    internal Symbol GetGlobalSymbol(string name, SymbolFlags meaning, Message diagnostic)
    {
        return this.getGlobalSymbol(name, meaning, diagnostic);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:103:1.
    internal Symbol GetMergedSymbol(Symbol symbol)
    {
        return this.getMergedSymbol(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:107:1.
    internal Symbol GetSymbolOfNode(Node node)
    {
        return this.getSymbolOfNode(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:111:1.
    internal Symbol GetSymbolOfDeclaration(Node node)
    {
        return this.getSymbolOfDeclaration(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:115:1.
    internal Symbol GetParentOfSymbol(Symbol symbol)
    {
        return this.getParentOfSymbol(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:119:1.
    internal Symbol TryFindAmbientModule(string moduleName) => TryFindAmbientModule(ByteStringValue.FromString(moduleName));
    internal Symbol TryFindAmbientModule(ByteStringValue moduleName) => this.tryFindAmbientModule(moduleName, true);
    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:135:1.
    internal Symbol ResolveExternalModuleName(Node moduleSpecifier, Type importAttributesType)
    {
        return this.resolveExternalModuleName(moduleSpecifier, moduleSpecifier, true, importAttributesType);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:139:1.
    internal Symbol ResolveExternalModuleSymbol(Symbol moduleSymbol)
    {
        return this.resolveExternalModuleSymbol(moduleSymbol, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:143:1.
    internal Type GetTypeFromTypeNode(Node node)
    {
        return this.getTypeFromTypeNode(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:147:1.
    internal bool IsArrayLikeType(Type t)
    {
        return this.isArrayLikeType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:159:1.
    internal bool TypeHasCallOrConstructSignatures(Type t)
    {
        return this.typeHasCallOrConstructSignatures(t);
    }

    // Checks if a property can be accessed in a location.
    // The location is given by the `node` parameter.
    // The node does not need to be a property access.
    // @param node location where to check property accessibility
    // @param isSuper whether to consider this a `super` property access, e.g. `super.foo`.
    // @param isWrite whether this is a write access, e.g. `++foo.x`.
    // @param containingType type where the property comes from.
    // @param property property symbol.
    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:171:1.
    internal bool IsPropertyAccessible(Node node, bool isSuper, bool isWrite, Type containingType, Symbol property)
    {
        return this.isPropertyAccessible(node, isSuper, isWrite, containingType, property);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:175:1.
    internal Type GetTypeOfPropertyOfContextualType(Type t, string name)
    {
        return this.getTypeOfPropertyOfContextualType(t, name);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:199:1.
    internal Type GetNonMissingTypeOfSymbol(Symbol symbol)
    {
        return this.getNonMissingTypeOfSymbol(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:203:1.
    internal Type GetConstraintOfTypeParameter(Type typeParameter)
    {
        return this.getConstraintOfTypeParameter(typeParameter);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:207:1.
    internal Type GetTrueTypeOfConditionalType(Type t)
    {
        return this.getTrueTypeFromConditionalType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:211:1.
    internal Type GetFalseTypeOfConditionalType(Type t)
    {
        return this.getFalseTypeFromConditionalType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:215:1.
    internal Type GetDefaultFromTypeParameter(Type typeParameter)
    {
        return this.getDefaultFromTypeParameter(typeParameter);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:223:1.
    internal Type GetBaseConstraintOfType(Type t)
    {
        return this.getBaseConstraintOfType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:235:1.
    internal static bool IsTupleTypeTarget(Type t)
    {
        return (isTupleType(t) && (t.Target() == t));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:239:1.
    internal bool IsArrayType(Type t)
    {
        return this.isArrayType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:243:1.
    internal bool IsReadonlySymbol(Symbol symbol)
    {
        return this.isReadonlySymbol(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:251:1.
    internal bool HasEffectiveRestParameter(Signature signature)
    {
        return this.hasEffectiveRestParameter(signature);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:255:1.
    internal Slice<Type> GetLocalTypeParametersOfClassOrInterfaceOrTypeAlias(Symbol symbol)
    {
        return this.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:259:1.
    internal Type GetContextualTypeForObjectLiteralElement(Node element, ContextFlags contextFlags)
    {
        return this.getContextualTypeForObjectLiteralElement(element, contextFlags);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:263:1.
    internal string TypePredicateToString(TypePredicate t)
    {
        return this.typePredicateToString(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:267:1.
    internal Slice<Slice<Symbol>> GetExpandedParameters(Signature signature, bool skipUnionExpanding)
    {
        return this.getExpandedParameters(signature, skipUnionExpanding);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:276:1.
    internal Type GetTypeOfPropertyOfType(Type t, string name)
    {
        return this.getTypeOfPropertyOfType(t, name);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:280:1.
    internal Type GetContextualTypeForArgumentAtIndex(Node node, int argIndex)
    {
        return this.getContextualTypeForArgumentAtIndex(node, argIndex);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:284:1.
    internal Type GetAwaitedType(Type t)
    {
        return this.getAwaitedType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:288:1.
    internal Slice<Node> GetIndexSignaturesAtLocation(Node node)
    {
        return this.getIndexSignaturesAtLocation(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:292:1.
    internal Symbol GetResolvedSymbol(Node node)
    {
        return this.getResolvedSymbol(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:308:1.
    internal Symbol ResolveName(string name, Node location, SymbolFlags meaning, bool excludeGlobals)
    {
        return this.resolveName(location, name, meaning, null, true, excludeGlobals);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:312:1.
    internal SymbolFlags GetSymbolFlags(Symbol symbol)
    {
        return this.getSymbolFlags(symbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:320:1.
    internal Type GetApparentType(Type t)
    {
        return this.getApparentType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:324:1.
    internal Type GetReducedType(Type t)
    {
        return this.getReducedType(t);
    }

    // GetFullyQualifiedName returns the fully qualified name of a symbol, walking up
    // its parent chain (e.g. `"/path/to/module".Namespace.Name`).
    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:330:1.
    internal string GetFullyQualifiedName(Symbol symbol)
    {
        return this.getFullyQualifiedName(symbol, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:334:1.
    internal Type GetBaseConstructorTypeOfClass(Type t)
    {
        return this.getBaseConstructorTypeOfClass(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:338:1.
    internal MemberOverrideStatus GetMemberOverrideModifierStatus(Node node, Node member, Symbol memberSymbol)
    {
        return this.getMemberOverrideModifierStatus(node, member, memberSymbol);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:342:1.
    internal Type GetRestTypeOfSignature(Signature sig)
    {
        return this.getRestTypeOfSignature(sig);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:346:1.
    internal Slice<Type> GetTypeArguments(Type t)
    {
        return this.getTypeArguments(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:350:1.
    internal IndexInfo GetIndexInfoOfType(Type t, Type keyType)
    {
        return this.getIndexInfoOfType(t, keyType);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:354:1.
    internal Type GetIndexTypeOfType(Type t, Type keyType)
    {
        return this.getIndexTypeOfType(t, keyType);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:358:1.
    internal Slice<IndexInfo> GetIndexInfosOfType(Type t)
    {
        return this.getIndexInfosOfType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:362:1.
    internal bool IsContextSensitive(Node node)
    {
        return this.isContextSensitive(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:366:1.
    internal Slice<Type> FillMissingTypeArguments(Slice<Type> typeArguments, Slice<Type> typeParameters, int minTypeArgumentCount, bool isJavaScriptImplicitAny)
    {
        return this.fillMissingTypeArguments(typeArguments, typeParameters, minTypeArgumentCount, isJavaScriptImplicitAny);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:370:1.
    internal int GetMinTypeArgumentCount(Slice<Type> typeParameters)
    {
        return this.getMinTypeArgumentCount(typeParameters);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:374:1.
    internal Type GetWidenedLiteralType(Type t)
    {
        return this.getWidenedLiteralType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:382:1.
    internal Type GetUnionTypeEx(Slice<Type> types, UnionReduction unionReduction)
    {
        return this.getUnionTypeEx(types, unionReduction, null, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:398:1.
    internal Type RemoveMissingOrUndefinedType(Type t)
    {
        return this.removeMissingOrUndefinedType(t);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/exports.go:406:1.
    internal int CompareSymbols(Symbol s1, Symbol s2)
    {
        return this.compareSymbols(s1, s2);
    }
}
