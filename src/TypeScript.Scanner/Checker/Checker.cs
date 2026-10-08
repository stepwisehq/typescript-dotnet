// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/checker.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected scalar declaration branches; omitted branches throw explicitly.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal SourceFile sourceFile;
    internal CompilerOptions compilerOptions;
    internal readonly SymbolTable globals = new();
    internal Symbol undefinedSymbol, argumentsSymbol, requireSymbol, unknownSymbol, globalThisSymbol;
    internal NameResolver nameResolver, suggestionNameResolver;
    internal Type anyType, autoType, wildcardType, errorType, unresolvedType, nonInferrableAnyType, intrinsicMarkerType, unknownType, undefinedType, undefinedWideningType, missingType, undefinedOrMissingType, optionalType, nullType, nullWideningType, stringType, numberType, bigintType, regularFalseType, falseType, regularTrueType, trueType, booleanType, esSymbolType, voidType, neverType, silentNeverType, implicitNeverType, unreachableNeverType, nonPrimitiveType, emptyObjectType, emptyGenericType, emptyTypeLiteralType, globalObjectType, globalStringType, globalNumberType, globalBooleanType;
    internal bool strictNullChecks = true, noImplicitAny, exactOptionalPropertyTypes;
    public SymbolTable Globals => globals;
    public Symbol GlobalThisSymbol => globalThisSymbol;
    public int TypeCount { get; private set; }
    public int SymbolCount { get; private set; }

    private readonly Dictionary<string, Type> stringLiteralTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<JsNum.Number, Type> numberLiteralTypes = new();
    private readonly Dictionary<string, Type> unionTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<(uint, uint), Type> unionOfUnionTypes = new();
    // checker.go:118/147. Only this cache kind is selected; other users remain excluded.
    private enum CachedTypeKind
    {
        LiteralUnionBaseType = 0,
        Widened = 13,
        RegularObjectLiteral = 14,
        PromisedTypeOfPromise = 15
    }

    private readonly record struct CachedTypeKey(CachedTypeKind kind, uint typeId);
    private readonly Dictionary<CachedTypeKey, Type> cachedTypes = new();
    private Type nanType;
    private Node currentNode, lastGetCombinedNodeFlagsNode;
    private NodeFlags lastGetCombinedNodeFlagsResult;
    private readonly DiagnosticsCollection diagnostics = new();
    private DiagnosticsCollection globalDiagnostics => diagnostics;

    // Scoped adaptation of NewChecker (913): fixed options, one script, no tracer/host.
    // Omitted caches, signatures, iteration resolvers and module state are unreachable
    // after admission; their semantic branches throw rather than use default values.
    internal static Checker CreateCore(SourceFile file)
    {
        Binder.BindSourceFile(file);
        var c = new Checker
        {
            sourceFile = file,
            compilerOptions = new CompilerOptions()
        };
        c.initializeScalarOptions();
        c.undefinedSymbol = c.newSymbol(SymbolFlags.Property, "undefined");
        c.argumentsSymbol = c.newSymbol(SymbolFlags.Property, "arguments");
        c.requireSymbol = c.newSymbol(SymbolFlags.Property, "require");
        c.unknownSymbol = c.newSymbol(SymbolFlags.Property, "unknown");
        c.globalThisSymbol = c.newSymbolEx(SymbolFlags.Module, "globalThis", CheckFlags.Readonly);
        c.globalThisSymbol.Exports = c.globals;
        c.globals[c.globalThisSymbol.Name] = c.globalThisSymbol;
        c.nameResolver = c.createNameResolver();
        c.suggestionNameResolver = c.createNameResolverForSuggestion();
        c.anyType = c.newIntrinsicType(TypeFlags.Any, "any");
        c.autoType = c.newIntrinsicTypeEx(TypeFlags.Any, "any", ObjectFlags.NonInferrableType);
        c.wildcardType = c.newIntrinsicType(TypeFlags.Any, "any");
        c.errorType = c.newIntrinsicType(TypeFlags.Any, "error");
        c.unresolvedType = c.newIntrinsicType(TypeFlags.Any, "unresolved");
        c.nonInferrableAnyType = c.newIntrinsicTypeEx(TypeFlags.Any, "any", ObjectFlags.ContainsWideningType);
        c.intrinsicMarkerType = c.newIntrinsicType(TypeFlags.Any, "intrinsic");
        c.unknownType = c.newIntrinsicType(TypeFlags.Unknown, "unknown");
        c.undefinedType = c.newIntrinsicType(TypeFlags.Undefined, "undefined");
        c.undefinedWideningType = c.createWideningType(c.undefinedType);
        c.missingType = c.newIntrinsicType(TypeFlags.Undefined, "undefined");
        c.undefinedOrMissingType = c.undefinedType;
        c.optionalType = c.newIntrinsicType(TypeFlags.Undefined, "undefined");
        c.nullType = c.newIntrinsicType(TypeFlags.Null, "null");
        c.nullWideningType = c.createWideningType(c.nullType);
        c.stringType = c.newIntrinsicType(TypeFlags.String, "string");
        c.numberType = c.newIntrinsicType(TypeFlags.Number, "number");
        c.bigintType = c.newIntrinsicType(TypeFlags.BigInt, "bigint");
        c.regularFalseType = c.newLiteralType(TypeFlags.BooleanLiteral, false, null);
        c.falseType = c.newLiteralType(TypeFlags.BooleanLiteral, false, c.regularFalseType);
        c.regularFalseType.AsLiteralType().freshType = c.falseType;
        c.falseType.AsLiteralType().freshType = c.falseType;
        c.regularTrueType = c.newLiteralType(TypeFlags.BooleanLiteral, true, null);
        c.trueType = c.newLiteralType(TypeFlags.BooleanLiteral, true, c.regularTrueType);
        c.regularTrueType.AsLiteralType().freshType = c.trueType;
        c.trueType.AsLiteralType().freshType = c.trueType;
        c.booleanType = c.getUnionType([c.regularFalseType, c.regularTrueType]);
        c.esSymbolType = c.newIntrinsicType(TypeFlags.ESSymbol, "symbol");
        c.voidType = c.newIntrinsicType(TypeFlags.Void, "void");
        c.neverType = c.newIntrinsicType(TypeFlags.Never, "never");
        c.silentNeverType = c.newIntrinsicTypeEx(TypeFlags.Never, "never", ObjectFlags.NonInferrableType);
        c.implicitNeverType = c.newIntrinsicType(TypeFlags.Never, "never");
        c.unreachableNeverType = c.newIntrinsicType(TypeFlags.Never, "never");
        c.nonPrimitiveType = c.newIntrinsicType(TypeFlags.NonPrimitive, "object");
        c.emptyObjectType = c.newAnonymousType(null, null);
        c.emptyGenericType = c.newAnonymousType(null, null);
        c.emptyTypeLiteralType = c.newAnonymousType(c.newSymbol(SymbolFlags.TypeLiteral, Ast.InternalSymbolNameType), null);
        c.initializeChecker();
        return c;
    }

    // initializeChecker (1309), one global script, no augmentations or modules.
    private void initializeChecker()
    {
        foreach (var(name, symbol)in sourceFile.Locals ?? new SymbolTable())
        {
            if (name == globalThisSymbol.Name)
                foreach (var declaration in symbol.Declarations)
                    error(declaration, Diagnostics.Declaration_name_conflicts_with_built_in_global_identifier_0, name);
            else if (globals.ContainsKey(name))
                throw unsupported("initializeChecker: global symbol merging");
            else
                globals[name] = symbol;
        }

        addUndefinedToGlobalsOrErrorOnRedeclaration();
        valueSymbolLinks.Get(undefinedSymbol).resolvedType = undefinedType;
        valueSymbolLinks.Get(argumentsSymbol).resolvedType = getGlobalType("IArguments", 0, true);
        valueSymbolLinks.Get(unknownSymbol).resolvedType = errorType;
        valueSymbolLinks.Get(globalThisSymbol).resolvedType = newAnonymousType(globalThisSymbol, globals);
        getGlobalType("Array", 1, true);
        globalObjectType = getGlobalType("Object", 0, true);
        globalFunctionType = getGlobalType("Function", 0, true);
        globalStringType = getGlobalType("String", 0, true);
        globalNumberType = getGlobalType("Number", 0, true);
        globalBooleanType = getGlobalType("Boolean", 0, true);
        getGlobalType("RegExp", 0, true);
        getGlobalType("ReadonlyArray", 1, false);
    }

    // getGlobalType (1210), missing built-ins in the fixed noLib environment.
    private Type getGlobalType(string name, int arity, bool reportErrors)
    {
        var symbol = getGlobalSymbol(name, SymbolFlags.Type, reportErrors ? Diagnostics.Cannot_find_global_type_0 : null);
        if (symbol != null)
            throw unsupported("getGlobalType: declared library types");
        return arity != 0 ? emptyGenericType : emptyObjectType;
    }

    private Symbol getGlobalSymbol(string name, SymbolFlags meaning, Message diagnostic) => nameResolver.Resolve(null, name, meaning, diagnostic, false, false);
    // addUndefinedToGlobalsOrErrorOnRedeclaration (1496).
    private void addUndefinedToGlobalsOrErrorOnRedeclaration()
    {
        var target = globals[undefinedSymbol.Name];
        if (target != null)
            foreach (var declaration in target.Declarations)
                if (!Ast.IsTypeDeclaration(declaration))
                    error(declaration, Diagnostics.Declaration_name_conflicts_with_built_in_global_identifier_0, undefinedSymbol.Name);
        if (target == null)
            globals[undefinedSymbol.Name] = undefinedSymbol;
    }

    // newSymbol/newSymbolEx retain checker-owned transient symbols.
    private Symbol newSymbol(SymbolFlags flags, string name) => newSymbolEx(flags, name, CheckFlags.None);
    private Symbol newSymbolEx(SymbolFlags flags, string name, CheckFlags checkFlags)
    {
        SymbolCount++;
        return new Symbol
        {
            Flags = flags | SymbolFlags.Transient,
            Name = name,
            CheckFlags = checkFlags
        };
    }

    internal static UnsupportedSemanticFeatureException unsupported(string feature) => new(feature);
    public List<Diagnostic> GetDiagnostics()
    {
        checkSourceFile();
        return diagnostics.GetDiagnosticsForFile(sourceFile);
    }

    public List<Diagnostic> GetGlobalDiagnostics()
    {
        produceDeferredDiagnostics();
        return diagnostics.GetGlobalDiagnostics();
    }

    // checkSourceFile (2240): script-only; no deferred/unused nodes admitted.
    private void checkSourceFile()
    {
        var links = sourceFileLinks.Get(sourceFile.AsNode());
        if (links.typeChecked)
            return;
        checkGrammarSourceFile();
        checkSourceElements(sourceFile.Statements.Nodes);
        checkDeferredNodes(sourceFile);
        produceDeferredDiagnostics();
        links.typeChecked = true;
    }

    private void checkSourceElements(List<Node> nodes)
    {
        if (nodes != null)
            foreach (var node in nodes)
                checkSourceElement(node);
    }

    private void checkSourceElement(Node node)
    {
        if (node == null)
            return;
        currentNode = node;
        checkSourceElementWorker(node);
    }

    // checkSourceElementWorker (2301), retained scalar syntax dispatch.
    private void checkSourceElementWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Block:
                checkBlock(node);
                break;
            case Kind.VariableStatement:
                checkVariableStatement(node);
                break;
            case Kind.VariableDeclarationList:
                checkVariableDeclarationList(node);
                break;
            case Kind.VariableDeclaration:
                checkVariableDeclaration(node);
                break;
            case Kind.TypeLiteral:
                checkTypeLiteral(node);
                break;
            case Kind.PropertySignature:
                checkPropertySignature(node);
                break;
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.StringKeyword:
            case Kind.NumberKeyword:
            case Kind.BooleanKeyword:
            case Kind.BigIntKeyword:
            case Kind.SymbolKeyword:
            case Kind.VoidKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NeverKeyword:
            case Kind.LiteralType:
            case Kind.ParenthesizedType:
                getTypeFromTypeNode(node);
                break;
            default:
                throw unsupported("checkSourceElementWorker: " + node.Kind);
        }
    }

    private void checkBlock(Node node)
    {
        if (node.Kind == Kind.Block)
            checkGrammarStatementInAmbientContext(node);
        if (Ast.IsFunctionOrModuleBlock(node))
            throw unsupported("checkBlock: function/module");
        checkSourceElements(node.Statements());
    // registerForUnusedIdentifiersCheck only adds this block to a worklist;
    // noUnusedLocals/noUnusedParameters/suggestion checks excluded by fixed API.
    }

    private void checkVariableStatement(Node node)
    {
        var list = node.AsVariableStatement().DeclarationList;
        if (!checkGrammarModifiers(node) && !checkGrammarVariableDeclarationList(list))
            checkGrammarForDisallowedBlockScopedVariableStatement(node);
        checkVariableDeclarationList(list);
    }

    private void checkVariableDeclarationList(Node node)
    {
        if ((Ast.GetCombinedNodeFlags(node) & NodeFlags.BlockScoped)is NodeFlags.Using or NodeFlags.AwaitUsing)
            throw unsupported("checkVariableDeclarationList: using");
        checkSourceElements(node.AsVariableDeclarationList().Declarations.Nodes);
    }

    private void checkVariableDeclaration(Node node)
    {
        checkGrammarVariableDeclaration(node);
        checkVariableLikeDeclaration(node);
    }

    // checkVariableLikeDeclaration (5977): primary/secondary scalar declarations.
    private void checkVariableLikeDeclaration(Node node)
    {
        if (node.Kind is not (Kind.VariableDeclaration or Kind.PropertySignature) || node.Kind == Kind.VariableDeclaration && !Ast.IsIdentifier(node.Name()))
            throw unsupported("checkVariableLikeDeclaration: nonscalar declaration");
        var name = node.Name();
        var initializer = node.Initializer();
        checkSourceElement(node.Type());
        var symbol = getSymbolOfDeclaration(node);
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("checkVariableLikeDeclaration: alias");
        var t = convertAutoToAny(getTypeOfSymbol(symbol));
        if (node == symbol.ValueDeclaration)
        {
            if (initializer != null)
                checkTypeAssignableToAndOptionallyElaborate(checkExpressionCached(initializer), t, node, initializer);
            if ((symbol.Declarations?.Count ?? 0) > 1 && symbol.Declarations.Any(d => d != node && !areDeclarationFlagsIdentical(d, node)))
                error(name, Diagnostics.All_declarations_of_0_must_have_identical_modifiers, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name));
        }
        else
        {
            var declarationType = convertAutoToAny(getWidenedTypeForVariableLikeDeclaration(node, false));
            if (!isErrorType(t) && !isErrorType(declarationType) && !isTypeIdenticalTo(t, declarationType) && (symbol.Flags & SymbolFlags.Assignment) == 0)
                errorNextVariableOrPropertyDeclarationMustHaveSameType(symbol.ValueDeclaration, t, node, declarationType);
            if (initializer != null)
                checkTypeAssignableToAndOptionallyElaborate(checkExpressionCached(initializer), declarationType, node, initializer);
            if (symbol.ValueDeclaration != null && !areDeclarationFlagsIdentical(node, symbol.ValueDeclaration))
                error(name, Diagnostics.All_declarations_of_0_must_have_identical_modifiers, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name));
        }

        if (node.Kind != Kind.PropertySignature)
        {
            checkExportsOnMergedDeclarations(node);
            checkVarDeclaredNamesNotShadowed(node);
            checkCollisionsForDeclarationName(node, name);
        }
    }

    private void errorNextVariableOrPropertyDeclarationMustHaveSameType(Node first, Type firstType, Node next, Type nextType)
    {
        var name = next.Name();
        var text = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name);
        var message = Ast.IsPropertyDeclaration(next) || Ast.IsPropertySignatureDeclaration(next) ? Diagnostics.Subsequent_property_declarations_must_have_the_same_type_Property_0_must_be_of_type_1_but_here_has_type_2 : Diagnostics.Subsequent_variable_declarations_must_have_the_same_type_Variable_0_must_be_of_type_1_but_here_has_type_2;
        var d = error(name, message, text, typeToStringByteString(firstType), typeToStringByteString(nextType));
        if (first != null)
            d.AddRelatedInfo(NewDiagnosticForNode(first, Diagnostics.X_0_was_also_declared_here, text));
    }

    // areDeclarationFlagsIdentical (7067): selected variable declarations have no optionality.
    private bool areDeclarationFlagsIdentical(Node left, Node right)
    {
        if (Ast.IsParameterDeclaration(left) && Ast.IsVariableDeclaration(right) || Ast.IsVariableDeclaration(left) && Ast.IsParameterDeclaration(right))
            return true;
        if (Ast.HasQuestionToken(left) != Ast.HasQuestionToken(right))
            return false;
        var interestingFlags = ModifierFlags.Private | ModifierFlags.Protected | ModifierFlags.Async | ModifierFlags.Abstract | ModifierFlags.Readonly | ModifierFlags.Static;
        return (Ast.GetCombinedModifierFlags(left) & interestingFlags) == (Ast.GetCombinedModifierFlags(right) & interestingFlags);
    }

    // getCombinedNodeFlagsCached (19095).
    private NodeFlags getCombinedNodeFlagsCached(Node node)
    {
        if (node == lastGetCombinedNodeFlagsNode)
            return lastGetCombinedNodeFlagsResult;
        lastGetCombinedNodeFlagsNode = node;
        return lastGetCombinedNodeFlagsResult = Ast.GetCombinedNodeFlags(node);
    }

    // getSymbolOfDeclaration/getMergedSymbol/getLateBoundSymbol: ordinary unmerged variable arm.
    internal Symbol getSymbolOfDeclaration(Node node)
    {
        var symbol = node.Symbol();
        if (symbol == null)
            return null;
        if ((symbol.Flags & SymbolFlags.ClassMember) != 0 && symbol.Name == Ast.InternalSymbolNameComputed)
            throw unsupported("getSymbolOfDeclaration: late binding");
        return getMergedSymbol(symbol);
    }

    private Symbol getMergedSymbol(Symbol symbol)
    {
        if (symbol != null && symbol.Declarations?.Any(d => d.Kind is not (Kind.VariableDeclaration or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.PropertySignature or Kind.TypeLiteral or Kind.ObjectLiteralExpression)) == true && (symbol.Flags & SymbolFlags.Transient) == 0)
            throw unsupported("getMergedSymbol: non-variable symbols");
        return symbol;
    }

    internal Symbol getSymbol(SymbolTable symbols, string name, SymbolFlags meaning)
    {
        if ((meaning & SymbolFlags.All) != 0)
        {
            var symbol = getMergedSymbol(symbols?[name]);
            if (symbol != null && (symbol.Flags & meaning) != 0)
                return symbol;
            if (symbol != null && (symbol.Flags & SymbolFlags.Alias) != 0)
                throw unsupported("getSymbol: alias resolution");
        }

        return null;
    }

    private Diagnostic error(Node node, Message message, params object[] args)
    {
        var d = NewDiagnosticForNode(node, message, args);
        return addDiagnostic(d);
    }
}

public sealed partial class Checker
{
    // newType/newIntrinsicType/newIntrinsicTypeEx (25534..25562).
    private Type newType(TypeFlags flags, ObjectFlags objectFlags, TypeData data)
    {
        TypeCount++;
        var t = data.AsType();
        t.flags = flags;
        t.objectFlags = objectFlags & ~(ObjectFlags.CouldContainTypeVariablesComputed | ObjectFlags.CouldContainTypeVariables | ObjectFlags.MembersResolved);
        t.id = (uint)TypeCount;
        t.checker = this;
        t.data = data;
        return t;
    }

    private Type newIntrinsicType(TypeFlags flags, string name) => newIntrinsicTypeEx(flags, name, ObjectFlags.None);
    private Type newIntrinsicTypeEx(TypeFlags flags, string name, ObjectFlags objectFlags) => newType(flags, objectFlags, new IntrinsicType { intrinsicName = name });
    private Type createWideningType(Type t)
    {
        if (strictNullChecks)
            return t;
        var result = newIntrinsicType(t.flags, t.AsIntrinsicType().intrinsicName);
        result.objectFlags |= ObjectFlags.ContainsWideningType;
        return result;
    }

    private Type newLiteralType(TypeFlags flags, object value, Type regularType)
    {
        var data = new LiteralType
        {
            value = value
        };
        var t = newType(flags, ObjectFlags.None, data);
        data.regularType = regularType ?? t;
        return t;
    }

    // newObjectType/newAnonymousType/setStructuredTypeMembers: initialization markers only.
    private Type newAnonymousType(Symbol symbol, SymbolTable members, List<Signature> callSignatures = null, List<Signature> constructSignatures = null, List<IndexInfo> indexInfos = null)
    {
        var t = newObjectType(ObjectFlags.Anonymous, symbol);
        setStructuredTypeMembers(t, members, callSignatures, constructSignatures, indexInfos);
        return t;
    }

    private Type newUnionType(ObjectFlags objectFlags, List<Type> types) => newType(TypeFlags.Union, objectFlags, new UnionType { types = types });
    private ObjectFlags getPropagatingFlagsOfTypes(List<Type> types, TypeFlags excludeKinds)
    {
        ObjectFlags result = 0;
        foreach (var t in types)
            if ((t.flags & excludeKinds) == 0)
                result |= t.objectFlags;
        return result & ObjectFlags.PropagatingFlags;
    }

    internal Type getRegularTypeOfLiteralType(Type t)
    {
        if ((t.flags & TypeFlags.Freshable) != 0)
            return t.AsLiteralType().regularType;
        if ((t.flags & TypeFlags.Union) != 0)
        {
            var u = t.AsUnionType();
            return u.regularType ??= mapType(t, getRegularTypeOfLiteralType);
        }

        return t;
    }

    private Type getFreshTypeOfLiteralType(Type t)
    {
        if ((t.flags & TypeFlags.Freshable) != 0)
        {
            var d = t.AsLiteralType();
            if (d.freshType == null)
            {
                var f = newLiteralType(t.flags, d.value, t);
                f.symbol = t.symbol;
                f.AsLiteralType().freshType = f;
                d.freshType = f;
            }

            return d.freshType;
        }

        return t;
    }

    internal static bool isFreshLiteralType(Type t) => (t.flags & TypeFlags.Freshable) != 0 && t.AsLiteralType().freshType == t;
    private Type getStringLiteralType(string value)
    {
        if (!stringLiteralTypes.TryGetValue(value, out var t))
            stringLiteralTypes[value] = t = newLiteralType(TypeFlags.StringLiteral, value, null);
        return t;
    }

    private Type getNumberLiteralType(JsNum.Number value)
    {
        if (double.IsNaN(value.Value))
            return nanType ??= newLiteralType(TypeFlags.NumberLiteral, value, null);
        if (!numberLiteralTypes.TryGetValue(value, out var t))
            numberLiteralTypes[value] = t = newLiteralType(TypeFlags.NumberLiteral, value, null);
        return t;
    }

    private Type getBaseTypeOfLiteralType(Type t)
    {
        if ((t.flags & TypeFlags.EnumLike) != 0)
            throw unsupported("getBaseTypeOfLiteralType: enum");
        if ((t.flags & (TypeFlags.StringLiteral | TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0)
            return stringType;
        if ((t.flags & TypeFlags.NumberLiteral) != 0)
            return numberType;
        if ((t.flags & TypeFlags.BigIntLiteral) != 0)
            return bigintType;
        if ((t.flags & TypeFlags.BooleanLiteral) != 0)
            return booleanType;
        if ((t.flags & TypeFlags.Union) != 0)
            return getBaseTypeOfLiteralTypeUnion(t);
        return t;
    }

    private Type getBaseTypeOfLiteralTypeUnion(Type t)
    {
        var key = new CachedTypeKey(CachedTypeKind.LiteralUnionBaseType, t.id);
        if (cachedTypes.TryGetValue(key, out var result))
            return result;
        result = mapType(t, getBaseTypeOfLiteralType);
        cachedTypes[key] = result;
        return result;
    }

    internal Type getWidenedLiteralType(Type t)
    {
        if ((t.flags & TypeFlags.EnumLike) != 0 && isFreshLiteralType(t))
            throw unsupported("getWidenedLiteralType: enum");
        if ((t.flags & TypeFlags.StringLiteral) != 0 && isFreshLiteralType(t))
            return stringType;
        if ((t.flags & TypeFlags.NumberLiteral) != 0 && isFreshLiteralType(t))
            return numberType;
        if ((t.flags & TypeFlags.BigIntLiteral) != 0 && isFreshLiteralType(t))
            return bigintType;
        if ((t.flags & TypeFlags.BooleanLiteral) != 0 && isFreshLiteralType(t))
            return booleanType;
        if ((t.flags & TypeFlags.Union) != 0)
            return mapType(t, getWidenedLiteralType);
        return t;
    }

    private Type mapType(Type t, Func<Type, Type> f) => mapTypeEx(t, f, false);
    private Type mapTypeEx(Type t, Func<Type, Type> f, bool noReductions)
    {
        if ((t.flags & TypeFlags.Never) != 0)
            return t;
        if ((t.flags & TypeFlags.Union) == 0)
            return f(t);
        var u = t.AsUnionType();
        var types = u.origin != null && (u.origin.flags & TypeFlags.Union) != 0 ? u.origin.Types() : u.types;
        var mappedTypes = new List<Type>(16);
        bool changed = false;
        foreach (var s in types)
        {
            var mapped = (s.flags & TypeFlags.Union) != 0 ? mapTypeEx(s, f, noReductions) : f(s);
            if (mapped != s)
                changed = true;
            if (mapped != null)
                mappedTypes.Add(mapped);
        }

        if (!changed)
            return t;
        if (mappedTypes.Count == 0)
            return null;
        return getUnionTypeEx(mappedTypes, noReductions ? UnionReduction.None : UnionReduction.Literal, null, null);
    }

    private enum UnionReduction
    {
        None,
        Literal,
        Subtype
    }

    internal Type getUnionType(List<Type> types) => getUnionTypeEx(types, UnionReduction.Literal, null, null);
    // getUnionTypeEx/getUnionTypeWorker (26166): scalar unions; aliases/subtypes reject.
    private Type getUnionTypeEx(List<Type> types, UnionReduction reduction, TypeAlias alias, Type origin)
    {
        if (alias != null || origin != null || reduction == UnionReduction.Subtype)
            throw unsupported("getUnionTypeEx: alias/origin/subtype reduction");
        if (types.Count == 0)
            return neverType;
        if (types.Count == 1)
            return types[0];
        if (types.Count == 2 && ((types[0].flags | types[1].flags) & TypeFlags.Union) != 0)
        {
            uint id1 = types[0].id, id2 = types[1].id;
            if (id1 > id2)
                (id1, id2) = (id2, id1);
            // No reductions other than Literal used by this admitted slice.
            if (reduction == UnionReduction.None)
                return getUnionTypeWorker(types, reduction);
            if (!unionOfUnionTypes.TryGetValue((id1, id2), out var t))
                unionOfUnionTypes[(id1, id2)] = t = getUnionTypeWorker(types, reduction);
            return t;
        }

        return getUnionTypeWorker(types, reduction);
    }

    private Type getUnionTypeWorker(List<Type> types, UnionReduction reduction)
    {
        var(typeSet, includes) = addTypesToUnion(types);
        if (reduction != UnionReduction.None)
        {
            if ((includes & TypeFlags.AnyOrUnknown) != 0)
            {
                if ((includes & TypeFlags.Any) != 0)
                {
                    if ((includes & TypeFlags.IncludesWildcard) != 0)
                        return wildcardType;
                    if ((includes & TypeFlags.IncludesError) != 0)
                        return errorType;
                    return anyType;
                }

                return unknownType;
            }

            if ((includes & TypeFlags.Undefined) != 0 && typeSet.Count >= 2 && typeSet[0] == undefinedType && typeSet[1] == missingType)
                typeSet.RemoveAt(1);
            if ((includes & (TypeFlags.Enum | TypeFlags.Literal | TypeFlags.UniqueESSymbol | TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0 || (includes & TypeFlags.Void) != 0 && (includes & TypeFlags.Undefined) != 0)
                typeSet = removeRedundantLiteralTypes(typeSet, includes, false);
            if ((includes & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping | TypeFlags.IncludesConstrainedTypeVariable)) != 0)
                throw unsupported("getUnionTypeWorker: template/constrained types");
            if (typeSet.Count == 0)
            {
                if ((includes & TypeFlags.Null) != 0)
                    return nullType;
                if ((includes & TypeFlags.Undefined) != 0)
                    return undefinedType;
                return neverType;
            }
        }

        if ((includes & TypeFlags.Union) != 0)
            throw unsupported("getUnionTypeWorker: named union origins");
        var objectFlags = (includes & TypeFlags.NotPrimitiveUnion) != 0 ? ObjectFlags.None : ObjectFlags.PrimitiveUnion;
        if ((includes & TypeFlags.Intersection) != 0)
            objectFlags |= ObjectFlags.ContainsIntersections;
        return getUnionTypeFromSortedList(typeSet, objectFlags);
    }

    private (List<Type>, TypeFlags) addTypesToUnion(List<Type> sourceTypes)
    {
        var types = new List<Type>(sourceTypes.Count);
        TypeFlags includes = 0;
        void addType(Type t)
        {
            var flags = t.flags;
            if ((flags & TypeFlags.Never) != 0)
                return;
            if ((flags & (TypeFlags.Instantiable | TypeFlags.Intersection | TypeFlags.EnumLike | TypeFlags.UniqueESSymbol)) != 0)
                throw unsupported("addTypesToUnion: nonscalar constituent");
            includes |= flags & TypeFlags.IncludesMask;
            if (t == wildcardType)
                includes |= TypeFlags.IncludesWildcard;
            if (isErrorType(t))
                includes |= TypeFlags.IncludesError;
            types.Add(t);
        }

        Type lastType = null;
        foreach (var t in sourceTypes)
        {
            if (t != lastType)
            {
                if ((t.flags & TypeFlags.Union) != 0)
                {
                    var u = t.AsUnionType();
                    if (t.alias != null || u.origin != null)
                        includes |= TypeFlags.Union;
                    foreach (var s in u.types)
                        addType(s);
                }
                else
                    addType(t);
                lastType = t;
            }
        }

        if (types.Count >= 2)
        {
            types.Sort(CompareTypes);
            int unique = 1;
            foreach (var t in types.Skip(1).ToArray())
                if (t != types[unique - 1])
                    types[unique++] = t;
            types.RemoveRange(unique, types.Count - unique);
        }

        return (types, includes);
    }

    private List<Type> removeRedundantLiteralTypes(List<Type> types, TypeFlags includes, bool reduceVoidUndefined)
    {
        for (int i = types.Count - 1; i >= 0; i--)
        {
            var t = types[i];
            var flags = t.flags;
            bool remove = (flags & (TypeFlags.StringLiteral | TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0 && (includes & TypeFlags.String) != 0 || (flags & TypeFlags.NumberLiteral) != 0 && (includes & TypeFlags.Number) != 0 || (flags & TypeFlags.BigIntLiteral) != 0 && (includes & TypeFlags.BigInt) != 0 || (flags & TypeFlags.UniqueESSymbol) != 0 && (includes & TypeFlags.ESSymbol) != 0 || reduceVoidUndefined && (flags & TypeFlags.Undefined) != 0 && (includes & TypeFlags.Void) != 0 || isFreshLiteralType(t) && types.Contains(t.AsLiteralType().regularType);
            if (remove)
                types.RemoveAt(i);
        }

        return types;
    }

    private Type getUnionTypeFromSortedList(List<Type> types, ObjectFlags precomputedObjectFlags)
    {
        if (types.Count == 0)
            return neverType;
        if (types.Count == 1)
            return types[0];
        // Exact constituent identity tuple replaces opaque xxh3 cache key encoding.
        var key = string.Join(",", types.Select(t => t.id));
        if (!unionTypes.TryGetValue(key, out var t))
        {
            t = newUnionType(precomputedObjectFlags | getPropagatingFlagsOfTypes(types, TypeFlags.Nullable), types);
            if (types.Count == 2 && (types[0].flags & TypeFlags.BooleanLiteral) != 0 && (types[1].flags & TypeFlags.BooleanLiteral) != 0)
                t.flags |= TypeFlags.Boolean;
            unionTypes[key] = t;
        }

        return t;
    }

    private Type getOptionalType(Type t, bool isProperty)
    {
        var missingOrUndefined = isProperty ? undefinedOrMissingType : undefinedType;
        if (t == missingOrUndefined || (t.flags & TypeFlags.Union) != 0 && t.Types()[0] == missingOrUndefined)
            return t;
        return getUnionType([t, missingOrUndefined]);
    }

    private Type convertAutoToAny(Type t) => t == autoType ? anyType : t;
    internal bool isErrorType(Type t) => t == errorType || t == unresolvedType;
    private bool containsUndefinedType(Type t)
    {
        if ((t.flags & TypeFlags.Union) != 0)
            t = t.Types()[0];
        return (t.flags & TypeFlags.Undefined) != 0;
    }
}

public sealed partial class Checker
{
    internal readonly nodeLinkStore<NodeLinks> nodeLinks = new();
    internal readonly nodeLinkStore<SymbolNodeLinks> symbolNodeLinks = new();
    internal readonly nodeLinkStore<TypeNodeLinks> typeNodeLinks = new();
    internal readonly symbolArenaLinkStore<ValueSymbolLinks> valueSymbolLinks = new();
    internal readonly symbolArenaLinkStore<SymbolReferenceLinks> symbolReferenceLinks = new();
    private readonly List<(Symbol target, bool result)> typeResolutions = new();
    private void initializeScalarOptions()
    {
        compilerOptions.Target = ScriptTarget.ESNext;
        compilerOptions.NoLib = Tristate.True;
        compilerOptions.Strict = Tristate.False;
        compilerOptions.StrictNullChecks = Tristate.True;
        compilerOptions.NoImplicitAny = Tristate.False;
        compilerOptions.StrictBindCallApply = Tristate.False;
    }

    // createNameResolver (1510), actual callbacks retained; omitted feature hooks throw.
    private NameResolver createNameResolver() => new()
    {
        CompilerOptions = compilerOptions,
        Globals = globals,
        GetSymbolOfDeclaration = getSymbolOfDeclaration,
        Error = error,
        ArgumentsSymbol = argumentsSymbol,
        RequireSymbol = requireSymbol,
        Lookup = getSymbol,
        SymbolReferenced = symbolReferenced,
        SetRequiresScopeChangeCache = setRequiresScopeChangeCache,
        GetRequiresScopeChangeCache = getRequiresScopeChangeCache,
        OnPropertyWithInvalidInitializer = checkAndReportErrorForInvalidInitializer,
        OnFailedToResolveSymbol = onFailedToResolveSymbol,
        OnSuccessfullyResolvedSymbol = onSuccessfullyResolvedSymbol,
    };
    private NameResolver createNameResolverForSuggestion() => new()
    {
        CompilerOptions = compilerOptions,
        Globals = globals,
        GetSymbolOfDeclaration = getSymbolOfDeclaration,
        Error = error,
        ArgumentsSymbol = argumentsSymbol,
        RequireSymbol = requireSymbol,
        Lookup = getSuggestionForSymbolNameLookup,
        SymbolReferenced = symbolReferenced,
        SetRequiresScopeChangeCache = setRequiresScopeChangeCache,
        GetRequiresScopeChangeCache = getRequiresScopeChangeCache,
    };
    private void symbolReferenced(Symbol symbol, SymbolFlags meaning) => symbolReferenceLinks.Get(symbol).referenceKinds |= meaning;
    private Tristate getRequiresScopeChangeCache(Node node) => nodeLinks.Get(node).declarationRequiresScopeChange;
    private void setRequiresScopeChangeCache(Node node, Tristate value) => nodeLinks.Get(node).declarationRequiresScopeChange = value;
    private bool checkAndReportErrorForInvalidInitializer(Node location, string name, Node declaration, Symbol result) => throw unsupported("checkAndReportErrorForInvalidInitializer: class property");
    // getResolvedSymbol (14161).
    private Symbol getResolvedSymbol(Node node)
    {
        var links = symbolNodeLinks.Get(node);
        if (links.resolvedSymbol == null)
        {
            Symbol symbol = null;
            if (!Ast.NodeIsMissing(node))
                symbol = nameResolver.Resolve(node, node.Text(), SymbolFlags.Value | SymbolFlags.ExportValue, getCannotFindNameDiagnosticForName(node), true, false);
            links.resolvedSymbol = symbol ?? unknownSymbol;
        }

        return links.resolvedSymbol;
    }

    // getCannotFindNameDiagnosticForName (14186): source-admitted identifier uses.
    private Message getCannotFindNameDiagnosticForName(Node node)
    {
        switch (node.Text())
        {
            case "document":
            case "console":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_include_dom;
            case "$":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_jQuery_Try_npm_i_save_dev_types_Slashjquery_and_then_add_jquery_to_the_types_field_in_your_tsconfig;
            case "beforeEach":
            case "describe":
            case "suite":
            case "it":
            case "test":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_a_test_runner_Try_npm_i_save_dev_types_Slashjest_or_npm_i_save_dev_types_Slashmocha_and_then_add_jest_or_mocha_to_the_types_field_in_your_tsconfig;
            case "process":
            case "require":
            case "Buffer":
            case "module":
            case "NodeJS":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_node_Try_npm_i_save_dev_types_Slashnode_and_then_add_node_to_the_types_field_in_your_tsconfig;
            case "Bun":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_install_type_definitions_for_Bun_Try_npm_i_save_dev_types_Slashbun_and_then_add_bun_to_the_types_field_in_your_tsconfig;
            case "Map":
            case "Set":
            case "Promise":
            case "ast.Symbol":
            case "WeakMap":
            case "WeakSet":
            case "Iterator":
            case "AsyncIterator":
            case "SharedArrayBuffer":
            case "Atomics":
            case "AsyncIterable":
            case "AsyncIterableIterator":
            case "AsyncGenerator":
            case "AsyncGeneratorFunction":
            case "BigInt":
            case "Reflect":
            case "BigInt64Array":
            case "BigUint64Array":
                return Diagnostics.Cannot_find_name_0_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_1_or_later;
            default:
                if (node.Parent.Kind == Kind.ShorthandPropertyAssignment)
                    return Diagnostics.No_value_exists_in_scope_for_the_shorthand_property_0_Either_declare_one_or_provide_an_initializer;
                return Diagnostics.Cannot_find_name_0;
        }
    }

    // onFailedToResolveSymbol (1608): real primitive-name, lib and spelling paths.
    private void onFailedToResolveSymbol(Node location, string name, SymbolFlags meaning, Message message)
    {
        if (location != null)
        {
            if (!Ast.IsIdentifier(location) || location.Parent.Kind is not (Kind.VariableDeclaration or Kind.ParenthesizedExpression or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment))
                throw unsupported("onFailedToResolveSymbol: noninitializer name context");
            if ((meaning & SymbolFlags.Value) != 0 && isPrimitiveTypeName(name))
            {
                error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here, name);
                return;
            }

            var typeSymbol = nameResolver.Resolve(location, name, SymbolFlags.Type & ~SymbolFlags.Value, null, false, false);
            if (typeSymbol != null)
                throw unsupported("onFailedToResolveSymbol: type-as-value");
        }

        string declarationName = name;
        if (location != null && Ast.IsIdentifier(location) && location.Text() == name)
            declarationName = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(location);
        var suggestedLib = getSuggestedLibForNonExistentName(name);
        if (suggestedLib != "")
        {
            error(location, message, declarationName, suggestedLib);
            return;
        }

        var suggestion = suggestionNameResolver.Resolve(location, name, meaning, null, false, false);
        if (suggestion != null)
        {
            var suggestionName = symbolToString(suggestion);
            var diagnostic = NewDiagnosticForNode(location, meaning == SymbolFlags.Namespace ? Diagnostics.Cannot_find_namespace_0_Did_you_mean_1 : Diagnostics.Cannot_find_name_0_Did_you_mean_1, declarationName, suggestionName);
            if (suggestion.ValueDeclaration != null)
                diagnostic.AddRelatedInfo(NewDiagnosticForNode(suggestion.ValueDeclaration, Diagnostics.X_0_is_declared_here, suggestionName));
            (location == null ? globalDiagnostics : diagnostics).Add(diagnostic);
            return;
        }

        error(location, message, declarationName);
    }

    private static bool isPrimitiveTypeName(string s) => s is "any" or "string" or "number" or "boolean" or "never" or "unknown";
    private Symbol getSuggestionForSymbolNameLookup(SymbolTable symbols, string name, SymbolFlags meaning)
    {
        var symbol = getSymbol(symbols, name, meaning);
        if (symbol != null)
            return symbol;
        IEnumerable<Symbol> extras = Enumerable.Empty<Symbol>();
        if ((meaning & SymbolFlags.GlobalLookup) != 0)
            extras = getPrimitiveTypeAliasSuggestions(symbols);
        return getSpellingSuggestionForName(name, (symbols?.Values ?? Enumerable.Empty<Symbol>()).Concat(extras), meaning);
    }

    private Symbol getSpellingSuggestionForName(string name, IEnumerable<Symbol> symbols, SymbolFlags meaning) => Core.GetSpellingSuggestion(name, symbols, candidate =>
    {
        var candidateName = Ast.SymbolName(candidate);
        if (candidateName.Length == 0 || candidateName[0] is '"' or '\u00FE')
            return "";
        if ((candidate.Flags & meaning) != 0)
            return candidateName;
        if ((candidate.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("getSpellingSuggestionForName: alias");
        return "";
    }, compareSymbolsWorker);
    private void onSuccessfullyResolvedSymbol(Node location, Symbol result, SymbolFlags meaning, Node lastLocation, Node associatedDeclaration, bool withinDeferredContext)
    {
        if (location != null && ((meaning & SymbolFlags.BlockScopedVariable) != 0 || (meaning & (SymbolFlags.Class | SymbolFlags.Enum)) != 0 && (meaning & SymbolFlags.Value) == SymbolFlags.Value))
        {
            var local = getExportSymbolOfValueSymbolIfExported(result);
            if ((local.Flags & (SymbolFlags.BlockScopedVariable | SymbolFlags.Class | SymbolFlags.Enum)) != 0)
                checkResolvedBlockScopedVariable(local, location);
        }

        if (associatedDeclaration != null)
            throw unsupported("onSuccessfullyResolvedSymbol: parameter initializer");
        if ((result.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("onSuccessfullyResolvedSymbol: alias");
    }

    private void checkResolvedBlockScopedVariable(Symbol result, Node location)
    {
        if ((result.Flags & (SymbolFlags.Class | SymbolFlags.Enum)) != 0)
            throw unsupported("checkResolvedBlockScopedVariable: class/enum");
        var declaration = result.Declarations.Find(Ast.IsBlockOrCatchScoped);
        if (declaration == null)
            throw new InvalidOperationException("checkResolvedBlockScopedVariable could not find block-scoped declaration");
        if ((declaration.Flags & NodeFlags.Ambient) == 0 && !isBlockScopedNameDeclaredBeforeUse(declaration, location))
        {
            var name = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(declaration.Name());
            error(location, Diagnostics.Block_scoped_variable_0_used_before_its_declaration, name).AddRelatedInfo(NewDiagnosticForNode(declaration, Diagnostics.X_0_is_declared_here, name));
        }
    }

    private bool isBlockScopedNameDeclaredBeforeUse(Node declaration, Node usage)
    {
        if (Ast.GetSourceFileOfNode(declaration) != Ast.GetSourceFileOfNode(usage))
            return true;
        if (declaration.Kind != Kind.VariableDeclaration)
            throw unsupported("isBlockScopedNameDeclaredBeforeUse: nonvariable");
        var container = Ast.GetEnclosingBlockScopeContainer(declaration);
        if (declaration.Pos() <= usage.Pos())
            return !isImmediatelyUsedInInitializerOfBlockScopedVariable(declaration, usage, container);
        // No deferred use contexts are source-admitted in this contract.
        return false;
    }

    private static bool isImmediatelyUsedInInitializerOfBlockScopedVariable(Node declaration, Node usage, Node container)
    {
        if (declaration.Parent.Parent.Kind == Kind.VariableStatement && isSameScopeDescendentOf(usage, declaration, container))
            return true;
        if (declaration.Parent.Parent.Kind != Kind.VariableStatement)
            throw unsupported("isImmediatelyUsedInInitializerOfBlockScopedVariable: loop declaration");
        return false;
    }

    private static bool isSameScopeDescendentOf(Node initial, Node parent, Node stopAt)
    {
        if (parent == null)
            return false;
        for (var n = initial; n != null; n = n.Parent)
        {
            if (n == parent)
                return true;
            if (n == stopAt)
                return false;
            if (Ast.IsFunctionLike(n))
                throw unsupported("isSameScopeDescendentOf: deferred function use");
        }

        return false;
    }

    private Symbol getExportSymbolOfValueSymbolIfExported(Symbol symbol)
    {
        if ((symbol.Flags & SymbolFlags.ExportValue) != 0)
            return symbol.ExportSymbol;
        return symbol;
    }

    // getTypeOfSymbol (16879): primitive variable/property cache arm.
    internal Type getTypeOfSymbol(Symbol symbol)
    {
        if ((symbol.CheckFlags & (CheckFlags.DeferredType | CheckFlags.Instantiated | CheckFlags.Mapped | CheckFlags.ReverseMapped)) != 0)
            throw unsupported("getTypeOfSymbol: transformed symbol");
        if ((symbol.Flags & (SymbolFlags.Variable | SymbolFlags.Property)) != 0)
            return getTypeOfVariableOrParameterOrProperty(symbol);
        if (valueSymbolLinks.Get(symbol).resolvedType is { } cached)
            return cached;
        if ((symbol.Flags & (SymbolFlags.Accessor | SymbolFlags.Function | SymbolFlags.Method | SymbolFlags.Class | SymbolFlags.Enum | SymbolFlags.ValueModule | SymbolFlags.EnumMember | SymbolFlags.Alias)) != 0)
            throw unsupported("getTypeOfSymbol: accessor/function/class/enum/module/alias");
        return errorType;
    }

    private Type getTypeOfVariableOrParameterOrProperty(Symbol symbol)
    {
        var links = valueSymbolLinks.Get(symbol);
        if (links.resolvedType == null)
        {
            var t = getTypeOfVariableOrParameterOrPropertyWorker(symbol);
            if (t == null)
                throw new InvalidOperationException("Unexpected nil type");
            links.resolvedType ??= t;
            return t;
        }

        return links.resolvedType;
    }

    private Type getTypeOfVariableOrParameterOrPropertyWorker(Symbol symbol)
    {
        if (symbol == requireSymbol)
            return anyType;
        var declaration = symbol.ValueDeclaration;
        if (declaration == null || declaration.Kind is not (Kind.VariableDeclaration or Kind.PropertySignature or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment))
            throw unsupported("getTypeOfVariableOrParameterOrPropertyWorker: declaration kind");
        if (!pushTypeResolution(symbol))
            return reportCircularityError(symbol);
        var result = declaration.Kind switch
        {
            Kind.PropertyAssignment => checkPropertyAssignment(declaration),
            Kind.ShorthandPropertyAssignment => checkShorthandPropertyAssignment(declaration, true),
            _ => getWidenedTypeForVariableLikeDeclaration(declaration, true)};
        if (!popTypeResolution())
            return reportCircularityError(symbol);
        return result;
    }

    private bool pushTypeResolution(Symbol target)
    {
        for (int i = typeResolutions.Count - 1; i >= 0; i--)
        {
            if (valueSymbolLinks.Get(typeResolutions[i].target).resolvedType != null)
                break;
            if (typeResolutions[i].target == target)
            {
                for (int j = i; j < typeResolutions.Count; j++)
                    typeResolutions[j] = (typeResolutions[j].target, false);
                return false;
            }
        }

        typeResolutions.Add((target, true));
        return true;
    }

    private bool popTypeResolution()
    {
        int last = typeResolutions.Count - 1;
        bool result = typeResolutions[last].result;
        typeResolutions.RemoveAt(last);
        return result;
    }

    private Type reportCircularityError(Symbol symbol)
    {
        var declaration = symbol.ValueDeclaration;
        if (declaration?.Type() != null)
        {
            error(declaration, Diagnostics.X_0_is_referenced_directly_or_indirectly_in_its_own_type_annotation, symbolToString(symbol));
            return errorType;
        }

        if (noImplicitAny && declaration != null)
            error(declaration, Diagnostics.X_0_implicitly_has_type_any_because_it_does_not_have_a_type_annotation_and_is_referenced_directly_or_indirectly_in_its_own_initializer, symbolToString(symbol));
        return anyType;
    }

    private Type getWidenedTypeForVariableLikeDeclaration(Node declaration, bool reportErrors) => widenTypeForVariableLikeDeclaration(getTypeForVariableLikeDeclaration(declaration, true), declaration, reportErrors);
    private Type getTypeForVariableLikeDeclaration(Node declaration, bool includeOptionality)
    {
        if (declaration.Kind != Kind.PropertySignature && (declaration.Kind != Kind.VariableDeclaration || declaration.Parent.Parent.Kind != Kind.VariableStatement))
            throw unsupported("getTypeForVariableLikeDeclaration: nonscalar declaration");
        var declaredType = tryGetTypeFromTypeNode(declaration);
        if (declaredType != null)
            return strictNullChecks && includeOptionality && declaration.QuestionToken() != null ? getOptionalType(declaredType, declaration.Kind == Kind.PropertySignature) : declaredType;
        if (noImplicitAny)
            throw unsupported("getTypeForVariableLikeDeclaration: auto variable inference option");
        if (declaration.Initializer() != null)
            return widenTypeInferredFromInitializer(declaration, checkDeclarationInitializer(declaration));
        return null;
    }

    private Type tryGetTypeFromTypeNode(Node node) => node.Type() != null ? getTypeFromTypeNode(node.Type()) : null;
    private Type checkDeclarationInitializer(Node declaration)
    {
        var initializer = declaration.Initializer();
        var t = getQuickTypeOfExpression(initializer);
        return t ?? checkExpressionCached(initializer);
    }

    private Type getQuickTypeOfExpression(Node node)
    {
        var expr = Ast.SkipParentheses(node);
        if (Ast.IsLiteralExpression(node) || Ast.IsBooleanLiteral(node))
            return checkExpression(node);
        if (expr.Kind is not (Kind.Identifier or Kind.StringLiteral or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.ObjectLiteralExpression or Kind.PropertyAccessExpression))
            throw unsupported("getQuickTypeOfExpression: complex expression");
        return null;
    }

    private Type widenTypeInferredFromInitializer(Node declaration, Type t) => getWidenedLiteralTypeForInitializer(declaration, t);
    private Type getWidenedLiteralTypeForInitializer(Node declaration, Type t)
    {
        if ((getCombinedNodeFlagsCached(declaration) & NodeFlags.Constant) != 0 || (Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Readonly) != 0)
            return t;
        return getWidenedLiteralType(t);
    }

    private Type widenTypeForVariableLikeDeclaration(Type t, Node declaration, bool reportErrors)
    {
        if (t != null)
        {
            if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
                throw unsupported("widenTypeForVariableLikeDeclaration: unique symbol");
            return getWidenedType(t);
        }

        if (declaration.Kind is not (Kind.VariableDeclaration or Kind.PropertySignature))
            throw unsupported("widenTypeForVariableLikeDeclaration: parameter");
        if (reportErrors && noImplicitAny)
            throw unsupported("widenTypeForVariableLikeDeclaration: implicit any option");
        return anyType;
    }

    internal Type getWidenedType(Type t) => getWidenedTypeWithContext(t, null);
    private Type checkExpressionCached(Node node)
    {
        var links = typeNodeLinks.Get(node);
        // No loops are admitted: flowLoopStack/flowTypeCache are always nil.
        if (links.resolvedType == null)
        {
            var savedFlowTypeCache = flowTypeCache;
            flowTypeCache = null;
            links.resolvedType = checkExpression(node);
            flowTypeCache = savedFlowTypeCache;
        }

        return links.resolvedType;
    }

    private Type checkExpression(Node node) => checkExpressionEx(node, CheckMode.Normal);
    private Type checkExpressionEx(Node node, CheckMode checkMode)
    {
        if ((checkMode & ~(CheckMode.Contextual | CheckMode.SkipContextSensitive)) != 0)
            throw unsupported("checkExpressionEx: inference/signature/tuple check mode");
        var saved = currentNode;
        currentNode = node;
        var t = checkExpressionWorker(node, checkMode);
        currentNode = saved;
        return t;
    }

    private Type checkExpressionWorker(Node node, CheckMode checkMode)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                return checkIdentifier(node);
            case Kind.NullKeyword:
                return nullWideningType;
            case Kind.StringLiteral:
                return getFreshTypeOfLiteralType(getStringLiteralType(node.Text()));
            case Kind.NumericLiteral:
                checkGrammarNumericLiteral(node);
                return getFreshTypeOfLiteralType(getNumberLiteralType(JsNum.FromString(node.Text())));
            case Kind.TrueKeyword:
                return trueType;
            case Kind.FalseKeyword:
                return falseType;
            case Kind.ParenthesizedExpression:
                return checkExpressionEx(node.Expression(), checkMode);
            case Kind.ObjectLiteralExpression:
                return checkObjectLiteral(node, checkMode);
            case Kind.PropertyAccessExpression:
                return checkPropertyAccessExpression(node);
            default:
                throw unsupported("checkExpressionWorker: " + node.Kind);
        }
    }

    internal Type getTypeFromTypeNode(Node node)
    {
        // Conditional type flow is unreachable: no conditional/type-parameter contexts.
        return getTypeFromTypeNodeWorker(node);
    }

    private Type getTypeFromTypeNodeWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.AnyKeyword:
                return anyType;
            case Kind.UnknownKeyword:
                return unknownType;
            case Kind.StringKeyword:
                return stringType;
            case Kind.NumberKeyword:
                return numberType;
            case Kind.BigIntKeyword:
                return bigintType;
            case Kind.BooleanKeyword:
                return booleanType;
            case Kind.SymbolKeyword:
                return esSymbolType;
            case Kind.VoidKeyword:
                return voidType;
            case Kind.UndefinedKeyword:
                return undefinedType;
            case Kind.NullKeyword:
                return nullType;
            case Kind.NeverKeyword:
                return neverType;
            case Kind.ParenthesizedType:
                return getTypeFromTypeNode(node.Type());
            case Kind.TypeLiteral:
                return getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode(node);
            case Kind.LiteralType:
                return getTypeFromLiteralTypeNode(node);
            default:
                throw unsupported("getTypeFromTypeNodeWorker: " + node.Kind);
        }
    }

    private Type getTypeFromLiteralTypeNode(Node node)
    {
        var literal = node.AsLiteralTypeNode().Literal;
        if (literal.Kind == Kind.NullKeyword)
            return nullType;
        return typeNodeLinks.Get(node).resolvedType ??= getRegularTypeOfLiteralType(checkExpression(literal));
    }

    // GetTypeAtLocation (32712), same reparsed-node normalization.
    public Type GetTypeAtLocation(Node node)
    {
        SemanticAdmission.ValidateOwnedNode(sourceFile, node);
        return getTypeOfNode(Ast.GetReparsedNodeForNode(node));
    }

    // getTypeOfNode (32483): ordered admitted scalar branches.
    private Type getTypeOfNode(Node node)
    {
        if (Ast.IsSourceFile(node) && !Ast.IsExternalOrCommonJSModule(node.AsSourceFile()))
            return errorType;
        if ((node.Flags & NodeFlags.InWithStatement) != 0)
            return errorType;
        if (Ast.IsPartOfTypeNode(node))
            return getTypeFromTypeNode(node);
        if (Ast.IsExpressionNode(node))
            return getRegularTypeOfExpression(node);
        if (Ast.IsVariableDeclaration(node) || node.Kind == Kind.PropertySignature)
        {
            var symbol = getSymbolOfDeclaration(node);
            return symbol != null ? getTypeOfSymbol(symbol) : errorType;
        }

        if (Ast.IsDeclarationNameOrImportPropertyName(node))
        {
            var symbol = getSymbolAtLocation(node, false);
            return symbol != null ? getTypeOfSymbol(symbol) : errorType;
        }

        // Literal-type children (numeric/string), statements and other admitted
        // nonsemantic locations reach the genuine upstream final errorType arm.
        // Heritage/type declarations/binding/import/meta clauses are excluded by admission.
        return errorType;
    }

    // GetSymbolAtLocation/getSymbolAtLocation (32134/32147): script/ordinary name branches.
    public Symbol GetSymbolAtLocation(Node node)
    {
        SemanticAdmission.ValidateOwnedNode(sourceFile, node);
        return getSymbolAtLocation(Ast.GetReparsedNodeForNode(node), true);
    }

    private Symbol getSymbolAtLocation(Node node, bool ignoreErrors)
    {
        if (Ast.IsSourceFile(node))
        {
            if (Ast.IsExternalOrCommonJSModule(node.AsSourceFile()))
                throw unsupported("getSymbolAtLocation: module");
            return null;
        }

        if ((node.Flags & NodeFlags.InWithStatement) != 0)
            return null;
        if (Ast.IsDeclarationNameOrImportPropertyName(node))
            return getSymbolOfDeclaration(node.Parent);
        if (Ast.IsIdentifier(node) || node.Kind == Kind.PropertyAccessExpression)
            return getSymbolOfNameOrPropertyAccessExpression(node);
        if (node.Kind is Kind.StringLiteral or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.ParenthesizedExpression)
            return null;
        // The omitted import/meta/member clauses cannot occur in admitted input.
        // Keywords/type nodes and other non-name locations retain Go's final nil arm.
        return null;
    }

    // checker.go:32333. Selected ordinary expression identifier branch; aliases,
    // heritage/import/JSDoc-qualified/member names are excluded by admission.
    private Symbol getSymbolOfNameOrPropertyAccessExpression(Node name)
    {
        if (Ast.IsDeclarationName(name))
            return getSymbolOfDeclaration(name.Parent);
        while (name.Parent?.Kind == Kind.PropertyAccessExpression && name.Parent.Name() == name)
            name = name.Parent;
        if (name.Kind == Kind.PropertyAccessExpression)
        {
            var links = symbolNodeLinks.Get(name);
            if (links.resolvedSymbol != null)
                return links.resolvedSymbol;
            checkPropertyAccessExpression(name);
            return links.resolvedSymbol;
        }

        if (name.Kind != Kind.Identifier)
            throw unsupported("getSymbolOfNameOrPropertyAccessExpression: qualified name");
        if (Ast.IsExpressionNode(name))
        {
            if (Ast.NodeIsMissing(name))
                return null;
            return resolveEntityName(name, SymbolFlags.Value, true, true, null);
        }

        return null;
    }

    private Symbol resolveEntityName(Node name, SymbolFlags meaning, bool ignoreErrors, bool dontResolveAlias, Node location)
    {
        if (Ast.NodeIsMissing(name))
            return null;
        if (name.Kind != Kind.Identifier || meaning == SymbolFlags.Namespace)
            throw unsupported("resolveEntityName: qualified/namespace name");
        Message message = null;
        if (!ignoreErrors)
            message = getCannotFindNameDiagnosticForName(name);
        var symbol = getMergedSymbol(nameResolver.Resolve(location ?? name, name.Text(), meaning, message, true, false));
        if (symbol != null && symbol != unknownSymbol)
        {
            if ((symbol.Flags & SymbolFlags.Alias) != 0 && !dontResolveAlias)
                throw unsupported("resolveEntityName: alias");
            return symbol;
        }

        return null;
    }
}

public sealed partial class Checker
{
    // checkIdentifier (11278): ordinary scalar variable reads, exact initialization/flow path.
    private Type checkIdentifier(Node node)
    {
        var symbol = getResolvedSymbol(node);
        if (symbol == unknownSymbol)
            return errorType;
        if (symbol == argumentsSymbol)
            return getTypeOfSymbol(symbol);
        var localOrExportSymbol = getExportSymbolOfValueSymbolIfExported(symbol);
        if ((localOrExportSymbol.Flags & (SymbolFlags.Alias | SymbolFlags.Class | SymbolFlags.Enum | SymbolFlags.Function)) != 0)
            throw unsupported("checkIdentifier: alias/class/enum/function");
        var declaration = localOrExportSymbol.ValueDeclaration;
        var immediateDeclaration = declaration;
        var t = getNarrowedTypeOfSymbol(localOrExportSymbol, node);
        if ((t.flags & TypeFlags.Object) != 0 && symbol == globalThisSymbol)
            throw unsupported("checkIdentifier: object value");
        if ((localOrExportSymbol.Flags & SymbolFlags.Variable) == 0)
            return t;
        if (declaration == null)
            return t;
        t = getNarrowableTypeForReference(t, node);
        var declarationContainer = getControlFlowContainer(declaration);
        var flowContainer = getControlFlowContainer(node);
        bool isOuterVariable = flowContainer != declarationContainer;
        bool typeIsAutomatic = t == autoType;
        bool isNeverInitialized = immediateDeclaration != null && Ast.IsVariableDeclaration(immediateDeclaration) && immediateDeclaration.Initializer() == null && immediateDeclaration.AsVariableDeclaration().ExclamationToken == null && isMutableLocalVariableDeclaration(immediateDeclaration) && !isSymbolAssignedDefinitely(symbol);
        bool assumeInitialized = isOuterVariable && !isNeverInitialized || t != autoType && (!strictNullChecks || (t.flags & (TypeFlags.AnyOrUnknown | TypeFlags.Void)) != 0) || declaration.AsVariableDeclaration().ExclamationToken != null || (declaration.Flags & NodeFlags.Ambient) != 0;
        var initialType = assumeInitialized ? t : typeIsAutomatic ? undefinedType : getOptionalType(t, false);
        var flowType = getFlowTypeOfReferenceEx(node, t, initialType, flowContainer, null);
        if (typeIsAutomatic)
        {
            if (flowType == autoType)
            {
                if (noImplicitAny)
                    throw unsupported("checkIdentifier: implicit automatic any errors");
                return convertAutoToAny(flowType);
            }
        }
        else if (!assumeInitialized && !containsUndefinedType(t) && containsUndefinedType(flowType))
        {
            error(node, Diagnostics.Variable_0_is_used_before_being_assigned, symbolToString(symbol));
            return t;
        }

        return flowType;
    }

    private Type getNarrowedTypeOfSymbol(Symbol symbol, Node location)
    {
        var t = getTypeOfSymbol(symbol);
        if (symbol.ValueDeclaration != null && symbol.ValueDeclaration.Kind != Kind.VariableDeclaration)
            throw unsupported("getNarrowedTypeOfSymbol: binding/parameter");
        return t;
    }

    private Type getNarrowableTypeForReference(Type t, Node reference)
    {
        if ((t.flags & TypeFlags.Instantiable) != 0 || (t.flags & TypeFlags.Union) != 0 && t.Types().Any(s => (s.flags & TypeFlags.Instantiable) != 0))
            throw unsupported("getNarrowableTypeForReference: constrained generic");
        return t;
    }

    private Node getControlFlowContainer(Node node) => Ast.FindAncestor(node.Parent, n => Ast.IsFunctionLike(n) || Ast.IsModuleBlock(n) || Ast.IsSourceFile(n) || Ast.IsPropertyDeclaration(n));
    private Dictionary<Node, Type> flowTypeCache;
    internal Type getTypeOfExpression(Node node)
    {
        var quickType = getQuickTypeOfExpression(node);
        if (quickType != null)
            return quickType;
        if (flowTypeCache != null && flowTypeCache.TryGetValue(node, out var cached))
            return cached;
        int invocation = flowInvocationCount;
        var t = checkExpression(node);
        if (flowInvocationCount != invocation)
            (flowTypeCache ??= new())[node] = t;
        return t;
    }
}

public sealed partial class Checker
{
    private void checkExportsOnMergedDeclarations(Node node)
    {
        var symbol = node.LocalSymbol();
        if (symbol == null)
        {
            symbol = getSymbolOfDeclaration(node);
            if (symbol.ExportSymbol == null)
                return;
        }

        throw unsupported("checkExportsOnMergedDeclarations: export merge");
    }

    private void checkVarDeclaredNamesNotShadowed(Node node)
    {
        if ((getCombinedNodeFlagsCached(node) & NodeFlags.BlockScoped) != 0 || Ast.IsPartOfParameterDeclaration(node))
            return;
        var symbol = getSymbolOfDeclaration(node);
        var name = node.Name();
        if ((symbol.Flags & SymbolFlags.FunctionScopedVariable) != 0)
        {
            var local = nameResolver.Resolve(node, name.Text(), SymbolFlags.Variable, null, false, false);
            if (local != null && local != symbol && (local.Flags & SymbolFlags.BlockScopedVariable) != 0 && (getCombinedNodeFlagsCached(local.ValueDeclaration) & NodeFlags.BlockScoped) != 0)
            {
                var list = Ast.FindAncestorKind(local.ValueDeclaration, Kind.VariableDeclarationList);
                Node container = list.Parent.Kind == Kind.VariableStatement ? list.Parent.Parent : null;
                bool shareScope = container != null && (Ast.IsSourceFile(container) || Ast.IsModuleBlock(container) || Ast.IsModuleDeclaration(container) || Ast.IsBlock(container) && Ast.IsFunctionLike(container.Parent));
                if (!shareScope)
                {
                    var text = symbolToString(local);
                    error(node, Diagnostics.Cannot_initialize_outer_scoped_variable_0_in_the_same_scope_as_block_scoped_declaration_1, text, text);
                }
            }
        }
    }

    private void checkCollisionsForDeclarationName(Node node, Node name)
    {
        if (name == null)
            return;
        // Every selected collision helper first excludes this ESNext script:
        // require/exports: module>=ES2015; Object: !external module;
        // Promise: target>=ES2017; WeakMap/Reflect: target>ES2021.
        if (compilerOptions.GetEmitScriptTarget() != ScriptTarget.ESNext || compilerOptions.GetEmitModuleKind() < ModuleKind.ES2015 || sourceFile.ExternalModuleIndicator != null || sourceFile.CommonJSModuleIndicator != null || node.Kind != Kind.VariableDeclaration)
            throw unsupported("checkCollisionsForDeclarationName: emit collision context");
    }
}

public sealed partial class Checker
{
    private static readonly Lazy<Dictionary<string, Symbol>> primitiveTypeAliasSuggestions = new(() =>
    {
        var result = new Dictionary<string, Symbol>(StringComparer.Ordinal);
        foreach (var(primitive, builtin)in new (string, string)[]
        {
            ("string", "String"),
            ("number", "Number"),
            ("boolean", "Boolean"),
            ("object", "Object"),
            ("bigint", "BigInt"),
            ("symbol", "Symbol")
        }

        )
            result[builtin] = new Symbol
            {
                Flags = SymbolFlags.TypeAlias | SymbolFlags.Transient,
                Name = primitive
            };
        return result;
    });
    private static IEnumerable<Symbol> getPrimitiveTypeAliasSuggestions(SymbolTable symbols)
    {
        foreach (var(name, suggestion)in primitiveTypeAliasSuggestions.Value)
            if (symbols?.ContainsKey(name) == true)
                yield return suggestion;
    }
}

public sealed partial class Checker
{
    // checker.go:32668. A queried dot-name denotes its containing access.
    internal Type getRegularTypeOfExpression(Node node)
    {
        if (Ast.IsRightSideOfQualifiedNameOrPropertyAccess(node))
            node = node.Parent;
        return getRegularTypeOfLiteralType(getTypeOfExpression(node));
    }
}

public sealed partial class Checker
{
    private Type newObjectType(ObjectFlags objectFlags, Symbol symbol)
    {
        if ((objectFlags & ObjectFlags.ObjectTypeKindMask) != ObjectFlags.Anonymous)
            throw unsupported("newObjectType: general object payload");
        var t = newType(TypeFlags.Object, objectFlags, new ObjectType());
        t.symbol = symbol;
        return t;
    }

    private void setStructuredTypeMembers(Type t, SymbolTable members, List<Signature> callSignatures = null, List<Signature> constructSignatures = null, List<IndexInfo> indexInfos = null)
    {
        t.objectFlags |= ObjectFlags.MembersResolved;
        var data = t.AsStructuredType();
        data.members = members;
        data.properties = getNamedMembers(members, t.symbol);
        if ((callSignatures?.Count ?? 0) != 0 || (constructSignatures?.Count ?? 0) != 0 || (indexInfos?.Count ?? 0) != 0)
            throw unsupported("setStructuredTypeMembers: signatures/index signatures");
        data.signatures = callSignatures;
        data.callSignatureCount = callSignatures?.Count ?? 0;
        data.indexInfos = indexInfos;
    }

    private List<Symbol> getNamedMembers(SymbolTable members, Symbol container)
    {
        if ((members?.Count ?? 0) == 0)
            return null;
        if (container != null && (container.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
            throw unsupported("getNamedMembers: class/interface inherited order");
        var result = new List<Symbol>(members.Count);
        foreach (var(id, symbol)in members)
            if (isNamedMember(symbol, id))
                result.Add(symbol);
        sortSymbols(result);
        return result;
    }

    private bool isNamedMember(Symbol symbol, string id) => !isReservedMemberName(id) && symbolIsValue(symbol);
    private bool symbolIsValue(Symbol symbol) => symbolIsValueEx(symbol, false);
    private bool symbolIsValueEx(Symbol symbol, bool includeTypeOnlyMembers)
    {
        if ((symbol.Flags & SymbolFlags.Value) != 0)
            return true;
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("symbolIsValueEx: alias");
        return false;
    }
}

public sealed partial class Checker
{
    private static bool isUnitType(Type t) => (t.flags & TypeFlags.Unit) != 0;
    private static bool isLiteralType(Type t)
    {
        if ((t.flags & TypeFlags.Boolean) != 0)
            return true;
        if ((t.flags & TypeFlags.Union) != 0)
        {
            if ((t.flags & TypeFlags.EnumLiteral) != 0)
                return true;
            return t.Types().All(isUnitType);
        }

        return isUnitType(t);
    }
}

public sealed partial class Checker
{
    private Type filterType(Type t, Func<Type, bool> predicate)
    {
        if ((t.flags & TypeFlags.Union) != 0)
        {
            var types = t.Types();
            var filtered = types.Where(predicate).ToList();
            if (filtered.Count == types.Count)
                return t;
            if (t.AsUnionType().origin != null)
                throw unsupported("filterType: named union origin");
            return getUnionTypeFromSortedList(filtered, t.objectFlags & ObjectFlags.PrimitiveUnion);
        }

        return predicate(t) ? t : neverType;
    }
}

public sealed partial class Checker
{
    private Type getNormalizedType(Type t, bool writing)
    {
        while (true)
        {
            Type n;
            if (isFreshLiteralType(t))
                n = t.AsLiteralType().regularType;
            else if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            {
                if ((t.flags & TypeFlags.Intersection) != 0)
                    throw unsupported("getNormalizedType: intersection");
                n = t; // getReducedType/normalizedUnion: scalar union has no intersections to reduce.
            }
            else if ((t.flags & TypeFlags.Instantiable) != 0 || (t.objectFlags & ObjectFlags.Reference) != 0)
                throw unsupported("getNormalizedType: instantiable/reference");
            else
                return t;
            if (n == t)
                return n;
            t = n;
        }
    }
}

public sealed partial class Checker
{
    private bool isUnknownLikeUnionType(Type t)
    {
        if (strictNullChecks && (t.flags & TypeFlags.Union) != 0)
        {
            if ((t.objectFlags & ObjectFlags.IsUnknownLikeUnionComputed) == 0)
            {
                t.objectFlags |= ObjectFlags.IsUnknownLikeUnionComputed;
                var types = t.Types();
                if (types.Count >= 3 && (types[0].flags & TypeFlags.Undefined) != 0 && (types[1].flags & TypeFlags.Null) != 0 && types.Any(IsEmptyAnonymousObjectType))
                    t.objectFlags |= ObjectFlags.IsUnknownLikeUnion;
            }

            return (t.objectFlags & ObjectFlags.IsUnknownLikeUnion) != 0;
        }

        return false;
    }

    private bool IsEmptyAnonymousObjectType(Type t)
    {
        return (t.objectFlags & ObjectFlags.Anonymous) != 0 && ((t.objectFlags & ObjectFlags.MembersResolved) != 0 && isEmptyResolvedType(t.AsStructuredType()) || t.symbol != null && (t.symbol.Flags & SymbolFlags.TypeLiteral) != 0 && (getMembersOfSymbol(t.symbol)?.Count ?? 0) == 0);
    }
}

public sealed partial class Checker
{
    private string getSuggestedLibForNonExistentName(string name) => getFeatureMap().TryGetValue(name, out var features) ? features[0].lib : "";
}

public sealed partial class Checker
{
    private readonly symbolArenaLinkStore<MembersAndExportsLinks> membersAndExportsLinks = new();
    // checker.go:16510. Anonymous type/object literals are late-binding containers
    // even when every admitted name is static.
    internal SymbolTable getMembersOfSymbol(Symbol symbol)
    {
        if ((symbol.Flags & SymbolFlags.LateBindingContainer) != 0)
            return getResolvedMembersOrExportsOfSymbol(symbol, MembersOrExportsResolutionKind.ResolvedMembers);
        return symbol.Members;
    }

    private SymbolTable getResolvedMembersOrExportsOfSymbol(Symbol symbol, MembersOrExportsResolutionKind resolutionKind)
    {
        var links = membersAndExportsLinks.Get(symbol);
        if (links[resolutionKind] == null)
        {
            bool isStatic = resolutionKind == MembersOrExportsResolutionKind.ResolvedExports;
            var earlySymbols = isStatic ? symbol.Exports : symbol.Members;
            if (isStatic && (symbol.Flags & SymbolFlags.Module) != 0)
                throw unsupported("getResolvedMembersOrExportsOfSymbol: module exports");
            links[resolutionKind] = earlySymbols;
            SymbolTable lateSymbols = null;
            foreach (var declaration in symbol.Declarations ?? [])
                foreach (var member in getMembersOfDeclaration(declaration) ?? [])
                    if (isStatic == Ast.HasStaticModifier(member))
                    {
                        if (member.Name()?.Kind == Kind.ComputedPropertyName || member.Kind == Kind.IndexSignature)
                            throw unsupported("getResolvedMembersOrExportsOfSymbol: late-bound/index member");
                    }

            if (isStatic && symbol.Exports?[Ast.InternalSymbolNameAssignmentDeclaration] != null)
                throw unsupported("getResolvedMembersOrExportsOfSymbol: assignment exports");
            links[resolutionKind] = combineSymbolTables(earlySymbols, lateSymbols);
        }

        return links[resolutionKind];
    }

    private SymbolTable combineSymbolTables(SymbolTable first, SymbolTable second)
    {
        if ((first?.Count ?? 0) == 0)
            return second;
        if ((second?.Count ?? 0) == 0)
            return first;
        throw unsupported("combineSymbolTables: late-bound symbol merging");
    }

    internal StructuredType resolveStructuredTypeMembers(Type t)
    {
        if ((t.objectFlags & ObjectFlags.MembersResolved) == 0)
        {
            if ((t.flags & TypeFlags.Object) != 0)
            {
                if ((t.objectFlags & ObjectFlags.ObjectTypeKindMask) != ObjectFlags.Anonymous)
                    throw unsupported("resolveStructuredTypeMembers: nonanonymous object");
                resolveAnonymousTypeMembers(t);
            }
            else if ((t.flags & TypeFlags.Union) != 0)
                resolveUnionTypeMembers(t);
            else
                throw unsupported("resolveStructuredTypeMembers: intersection members");
        }

        return t.AsStructuredType();
    }

    private void resolveAnonymousTypeMembers(Type t)
    {
        if (t.AsObjectType().target != null)
            throw unsupported("resolveAnonymousTypeMembers: instantiated target");
        var symbol = getMergedSymbol(t.symbol);
        if (symbol != null && (symbol.Flags & SymbolFlags.TypeLiteral) != 0)
        {
            setStructuredTypeMembers(t, null);
            var members = getMembersOfSymbol(symbol);
            if (members?[Ast.InternalSymbolNameCall] != null || members?[Ast.InternalSymbolNameNew] != null || members?[Ast.InternalSymbolNameIndex] != null)
                throw unsupported("resolveAnonymousTypeMembers: signatures/index members");
            setStructuredTypeMembers(t, members);
            return;
        }

        throw unsupported("resolveAnonymousTypeMembers: class/function/module members");
    }

    private Type getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode(Node node)
    {
        if (node.Kind != Kind.TypeLiteral)
            throw unsupported("getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode: function/constructor type");
        var links = typeNodeLinks.Get(node);
        if (links.resolvedType == null)
        {
            var symbol = node.Symbol();
            if (symbol == null || (getMembersOfSymbol(symbol)?.Count ?? 0) == 0)
                links.resolvedType = emptyTypeLiteralType;
            else
                links.resolvedType = newObjectType(ObjectFlags.Anonymous, symbol);
        }

        return links.resolvedType;
    }

    private Type getReducedApparentType(Type t) => getReducedType(getApparentType(getReducedType(t)));
    private Type getReducedType(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0 || (t.objectFlags & ObjectFlags.ContainsIntersections) != 0)
            throw unsupported("getReducedType: intersection reduction");
        return t;
    }

    private Type getApparentType(Type t)
    {
        if ((t.flags & TypeFlags.Instantiable) != 0 || (t.objectFlags & (ObjectFlags.Mapped | ObjectFlags.Reference)) != 0 || (t.flags & TypeFlags.Intersection) != 0)
            throw unsupported("getApparentType: instantiable/mapped/reference/intersection type");
        if ((t.flags & TypeFlags.StringLike) != 0)
            return globalStringType;
        if ((t.flags & TypeFlags.NumberLike) != 0)
            return globalNumberType;
        if ((t.flags & TypeFlags.BigIntLike) != 0)
            return getGlobalBigIntType();
        if ((t.flags & TypeFlags.BooleanLike) != 0)
            return globalBooleanType;
        if ((t.flags & TypeFlags.ESSymbolLike) != 0)
            return getGlobalESSymbolType();
        if ((t.flags & TypeFlags.NonPrimitive) != 0)
            return emptyObjectType;
        if ((t.flags & TypeFlags.Index) != 0)
            throw unsupported("getApparentType: index type");
        if ((t.flags & TypeFlags.Unknown) != 0 && !strictNullChecks)
            return emptyObjectType;
        return t;
    }

    private Type globalBigIntType, globalESSymbolType;
    private Type getGlobalBigIntType() => globalBigIntType ??= getGlobalType("BigInt", 0, false);
    private Type getGlobalESSymbolType() => globalESSymbolType ??= getGlobalType("Symbol", 0, false);
    internal List<Symbol> getPropertiesOfObjectType(Type t) => (t.flags & TypeFlags.Object) != 0 ? resolveStructuredTypeMembers(t).properties : null;
    internal List<Symbol> getPropertiesOfType(Type t)
    {
        t = getReducedApparentType(t);
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return getPropertiesOfUnionOrIntersectionType(t);
        return getPropertiesOfObjectType(t);
    }

    private Symbol getPropertyOfObjectType(Type t, string name)
    {
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var symbol = resolveStructuredTypeMembers(t).members?[name];
            if (symbol != null && symbolIsValue(symbol))
                return symbol;
        }

        return null;
    }

    internal Symbol getPropertyOfType(Type t, string name) => getPropertyOfTypeEx(t, name, false, false);
    private Symbol getPropertyOfTypeEx(Type t, string name, bool skipObjectFunctionPropertyAugment, bool includeTypeOnlyMembers)
    {
        t = getReducedApparentType(t);
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var resolved = resolveStructuredTypeMembers(t);
            var symbol = resolved.members?[name];
            if (symbol != null && symbolIsValueEx(symbol, includeTypeOnlyMembers))
                return symbol;
            if (skipObjectFunctionPropertyAugment)
                return null;
            if ((resolved.signatures?.Count ?? 0) != 0)
                throw unsupported("getPropertyOfTypeEx: function augmentation");
            return getPropertyOfObjectType(globalObjectType, name);
        }

        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return getPropertyOfUnionOrIntersectionType(t, name, skipObjectFunctionPropertyAugment);
        return null;
    }

    private Type getTypeOfPropertyOfType(Type t, string name)
    {
        var property = getPropertyOfType(t, name);
        return property != null ? getTypeOfSymbol(property) : null;
    }

    internal Type getNonMissingTypeOfSymbol(Symbol symbol) => removeMissingType(getTypeOfSymbol(symbol), (symbol.Flags & SymbolFlags.Optional) != 0);
    internal bool isReadonlySymbol(Symbol symbol)
    {
        if ((symbol.Flags & (SymbolFlags.Accessor | SymbolFlags.EnumMember)) != 0)
            throw unsupported("isReadonlySymbol: accessor/enum");
        return (symbol.CheckFlags & CheckFlags.Readonly) != 0 || (symbol.Flags & SymbolFlags.Property) != 0 && (getDeclarationModifierFlagsFromSymbol(symbol) & ModifierFlags.Readonly) != 0 || (symbol.Flags & SymbolFlags.Variable) != 0 && (getDeclarationNodeFlagsFromSymbol(symbol) & NodeFlags.Constant) != 0;
    }

    private Symbol createSymbolWithType(Symbol source, Type t)
    {
        var symbol = newSymbolEx(source.Flags, source.Name, source.CheckFlags & CheckFlags.Readonly);
        symbol.Declarations = source.Declarations;
        symbol.Parent = source.Parent;
        symbol.ValueDeclaration = source.ValueDeclaration;
        var links = valueSymbolLinks.Get(symbol);
        links.resolvedType = t;
        links.target = source;
        links.nameType = valueSymbolLinks.Get(source).nameType;
        return symbol;
    }

    private Type getRegularTypeOfObjectLiteral(Type t)
    {
        if (!isObjectLiteralType(t) || (t.objectFlags & ObjectFlags.FreshLiteral) == 0)
            return t;
        var key = new CachedTypeKey(CachedTypeKind.RegularObjectLiteral, t.id);
        if (cachedTypes.TryGetValue(key, out var cached))
            return cached;
        var resolved = resolveStructuredTypeMembers(t);
        var members = transformTypeOfMembers(t, getRegularTypeOfObjectLiteral);
        var regular = newAnonymousType(t.symbol, members, resolved.CallSignatures(), resolved.ConstructSignatures(), resolved.indexInfos);
        regular.flags = resolved.AsType().flags;
        regular.objectFlags |= resolved.AsType().objectFlags & ~ObjectFlags.FreshLiteral;
        cachedTypes[key] = regular;
        return regular;
    }

    private SymbolTable transformTypeOfMembers(Type t, Func<Type, Type> f)
    {
        var members = new SymbolTable();
        foreach (var property in getPropertiesOfObjectType(t) ?? [])
        {
            var original = getTypeOfSymbol(property);
            var updated = f(original);
            members[property.Name] = updated != original ? createSymbolWithType(property, updated) : property;
        }

        return members;
    }

    private static bool isObjectLiteralType(Type t) => (t.objectFlags & ObjectFlags.ObjectLiteral) != 0;
}

public sealed partial class Checker
{
    // checker.go:contextual type stack. Selected callers use ContextFlagsNone.
    private readonly record struct ContextualInfo(Node node, Type t, bool isCache);
    private readonly List<ContextualInfo> contextualInfos = new();
    private int findContextualNode(Node node, bool includeCaches)
    {
        for (int i = 0; i < contextualInfos.Count; i++)
            if (node == contextualInfos[i].node && (includeCaches || !contextualInfos[i].isCache))
                return i;
        return -1;
    }

    private void pushCachedContextualType(Node node) => pushContextualType(node, getContextualType(node), true);
    private void pushContextualType(Node node, Type t, bool isCache) => contextualInfos.Add(new(node, t, isCache));
    private void popContextualType()
    {
        int index = contextualInfos.Count - 1;
        contextualInfos[index] = default;
        contextualInfos.RemoveAt(index);
    }

    private Type getContextualType(Node node)
    {
        if ((node.Flags & NodeFlags.InWithStatement) != 0)
            return null;
        int index = findContextualNode(node, true);
        if (index >= 0)
            return contextualInfos[index].t;
        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.PropertySignature:
                return getContextualTypeForInitializerExpression(node);
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
                return getContextualTypeForObjectLiteralElement(parent);
            case Kind.ParenthesizedExpression:
                return getContextualType(parent);
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.BindingElement:
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.AsExpression:
            case Kind.TypeAssertionExpression:
            case Kind.SatisfiesExpression:
                throw unsupported("getContextualType: unselected contextual parent");
        }

        return null;
    }

    private Type getContextualTypeForInitializerExpression(Node node)
    {
        var declaration = node.Parent;
        if (node == declaration.Initializer())
        {
            var result = getContextualTypeForVariableLikeDeclaration(declaration);
            if (result != null)
                return result;
            if (Ast.IsBindingPattern(declaration.Name()))
                throw unsupported("getContextualTypeForInitializerExpression: binding pattern");
        }

        return null;
    }

    private Type getContextualTypeForVariableLikeDeclaration(Node declaration)
    {
        if (declaration.Type() != null)
            return getTypeFromTypeNode(declaration.Type());
        if (declaration.Kind is Kind.Parameter or Kind.BindingElement or Kind.PropertyDeclaration)
            throw unsupported("getContextualTypeForVariableLikeDeclaration: parameter/binding/class context");
        return null;
    }

    private Type getContextualTypeForObjectLiteralElement(Node element)
    {
        if (element.Type() != null)
            return getTypeFromTypeNode(element.Type());
        var t = getApparentTypeOfContextualType(element.Parent);
        if (t != null)
        {
            if (element.Name()?.Kind == Kind.ComputedPropertyName)
                throw unsupported("getContextualTypeForObjectLiteralElement: computed name");
            var symbol = getSymbolOfDeclaration(element);
            return getTypeOfPropertyOfContextualTypeEx(t, symbol.Name, valueSymbolLinks.Get(symbol).nameType);
        }

        return null;
    }

    private Type getTypeOfPropertyOfContextualTypeEx(Type t, string name, Type nameType) => mapTypeEx(t, constituent =>
    {
        if ((constituent.flags & TypeFlags.Intersection) != 0 || (constituent.objectFlags & ObjectFlags.Mapped) != 0)
            throw unsupported("getTypeOfPropertyOfContextualTypeEx: intersection/mapped property");
        if ((constituent.flags & TypeFlags.Object) == 0)
            return null;
        var result = getTypeOfConcretePropertyOfContextualType(constituent, name);
        if (result != null)
            return result;
        var infos = resolveStructuredTypeMembers(constituent).indexInfos;
        if ((infos?.Count ?? 0) != 0)
            throw unsupported("getTypeOfPropertyOfContextualTypeEx: index infos");
        return null;
    }, true);
    private Type getTypeOfConcretePropertyOfContextualType(Type t, string name)
    {
        var property = getPropertyOfType(t, name);
        if (property == null)
            return null;
        return removeMissingType(getTypeOfSymbol(property), (property.Flags & SymbolFlags.Optional) != 0);
    }

    private Type instantiateContextualType(Type t, Node node)
    {
        if (t != null && maybeTypeOfKind(t, TypeFlags.Instantiable))
            throw unsupported("instantiateContextualType: inference mapper");
        return t;
    }

    private Type getApparentTypeOfContextualType(Node node)
    {
        var instantiatedType = instantiateContextualType(getContextualType(node), node);
        if (instantiatedType != null)
        {
            var apparentType = mapTypeEx(instantiatedType, getApparentType, true);
            if ((apparentType.flags & TypeFlags.Union) != 0 && node.Kind == Kind.ObjectLiteralExpression)
                return discriminateContextualTypeByObjectMembers(node, apparentType);
            return apparentType;
        }

        return null;
    }

    private Type getWidenedLiteralLikeTypeForContextualType(Type t, Type contextualType)
    {
        if (!isLiteralOfContextualType(t, contextualType))
        {
            if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
                throw unsupported("getWidenedLiteralLikeTypeForContextualType: unique symbol");
            t = getWidenedLiteralType(t);
        }

        return getRegularTypeOfLiteralType(t);
    }

    private bool isLiteralOfContextualType(Type candidateType, Type contextualType)
    {
        if (contextualType != null)
        {
            if ((contextualType.flags & TypeFlags.Union) != 0)
                return contextualType.Types().Any(t => isLiteralOfContextualType(candidateType, t));
            if ((contextualType.flags & (TypeFlags.Intersection | TypeFlags.InstantiableNonPrimitive)) != 0)
                throw unsupported("isLiteralOfContextualType: intersection/constrained context");
            return (contextualType.flags & (TypeFlags.StringLiteral | TypeFlags.Index | TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0 && maybeTypeOfKind(candidateType, TypeFlags.StringLiteral) || (contextualType.flags & TypeFlags.NumberLiteral) != 0 && maybeTypeOfKind(candidateType, TypeFlags.NumberLiteral) || (contextualType.flags & TypeFlags.BigIntLiteral) != 0 && maybeTypeOfKind(candidateType, TypeFlags.BigIntLiteral) || (contextualType.flags & TypeFlags.BooleanLiteral) != 0 && maybeTypeOfKind(candidateType, TypeFlags.BooleanLiteral) || (contextualType.flags & TypeFlags.UniqueESSymbol) != 0 && maybeTypeOfKind(candidateType, TypeFlags.UniqueESSymbol);
        }

        return false;
    }

    private Type checkExpressionForMutableLocation(Node node, CheckMode checkMode = CheckMode.Normal)
    {
        if (node.Kind is Kind.AsExpression or Kind.TypeAssertionExpression)
            throw unsupported("checkExpressionForMutableLocation: assertion/const context");
        var t = checkExpressionEx(node, checkMode);
        return getWidenedLiteralLikeTypeForContextualType(t, instantiateContextualType(getContextualType(node), node));
    }

    private Type checkPropertyAssignment(Node node, CheckMode checkMode = CheckMode.Normal)
    {
        if (node.Name().Kind == Kind.ComputedPropertyName)
            throw unsupported("checkPropertyAssignment: computed name");
        var initializerType = checkExpressionForMutableLocation(node.Initializer(), checkMode);
        if (node.Type() != null)
        {
            var t = getTypeFromTypeNode(node.Type());
            checkTypeAssignableToAndOptionallyElaborate(initializerType, t, node, node.Initializer());
            return t;
        }

        return initializerType;
    }

    private Type checkShorthandPropertyAssignment(Node node, bool inDestructuringPattern, CheckMode checkMode = CheckMode.Normal)
    {
        var expr = inDestructuringPattern ? null : node.AsShorthandPropertyAssignment().ObjectAssignmentInitializer;
        expr ??= node.Name();
        var expressionType = checkExpressionForMutableLocation(expr, checkMode);
        if (node.Type() != null)
        {
            var t = getTypeFromTypeNode(node.Type());
            checkTypeAssignableToAndOptionallyElaborate(expressionType, t, node, expr);
            return t;
        }

        return expressionType;
    }

    private Type checkObjectLiteral(Node node, CheckMode checkMode)
    {
        if ((node.Properties()?.Count ?? 0) == 0 && node.Symbol() != null && (node.Symbol().Exports?.Count ?? 0) != 0)
            return newAnonymousType(node.Symbol(), node.Symbol().Exports);
        checkNodeDeferred(node);
        if (Ast.IsAssignmentTarget(node))
            throw unsupported("checkObjectLiteral: destructuring pattern");
        checkGrammarObjectLiteralExpression(node);
        var allPropertiesTable = strictNullChecks ? new SymbolTable() : null;
        var propertiesTable = new SymbolTable();
        List<Symbol> propertiesArray = null;
        pushCachedContextualType(node);
        var contextualType = getApparentTypeOfContextualType(node);
        ObjectFlags objectFlags = ObjectFlags.FreshLiteral;
        foreach (var element in node.Properties() ?? [])
            if (element.Name()?.Kind == Kind.ComputedPropertyName)
                throw unsupported("checkObjectLiteral: computed property");
        foreach (var memberDeclaration in node.Properties() ?? [])
        {
            var member = getSymbolOfDeclaration(memberDeclaration);
            Type t;
            switch (memberDeclaration.Kind)
            {
                case Kind.PropertyAssignment:
                    t = checkPropertyAssignment(memberDeclaration, checkMode);
                    break;
                case Kind.ShorthandPropertyAssignment:
                    t = checkShorthandPropertyAssignment(memberDeclaration, false, checkMode);
                    break;
                default:
                    throw unsupported("checkObjectLiteral: spread/method/accessor");
            }

            objectFlags |= t.objectFlags & ObjectFlags.PropagatingFlags;
            var prop = newSymbolEx(SymbolFlags.Property | member.Flags, member.Name, CheckFlags.None);
            prop.Declarations = member.Declarations;
            prop.Parent = member.Parent;
            prop.ValueDeclaration = member.ValueDeclaration;
            var links = valueSymbolLinks.Get(prop);
            links.resolvedType = t;
            links.target = member;
            if (allPropertiesTable != null)
                allPropertiesTable[prop.Name] = prop;
            propertiesTable[prop.Name] = prop;
            (propertiesArray ??= []).Add(prop);
        }

        popContextualType();
        var result = newAnonymousType(node.Symbol(), propertiesTable);
        result.objectFlags |= objectFlags | ObjectFlags.ObjectLiteral | ObjectFlags.ContainsObjectOrArrayLiteral;
        return result;
    }

    internal Type removeMissingType(Type t, bool isOptional)
    {
        if (exactOptionalPropertyTypes && isOptional)
            throw unsupported("removeMissingType: exact optional option");
        return t;
    }

    private NodeFlags getDeclarationNodeFlagsFromSymbol(Symbol symbol) => symbol.ValueDeclaration != null ? getCombinedNodeFlagsCached(symbol.ValueDeclaration) : NodeFlags.None;
}

public sealed partial class Checker
{
    private readonly nodeLinkStore<SourceFileLinks> sourceFileLinks = new();
    private List<Action> deferredDiagnosticCallbacks;
    private void checkNodeDeferred(Node node)
    {
        var links = sourceFileLinks.Get(Ast.GetSourceFileOfNode(node).AsNode());
        if (!links.typeChecked)
            links.deferredNodes.Add(node);
    }

    private void checkDeferredNodes(SourceFile file)
    {
        var links = sourceFileLinks.Get(file.AsNode());
        foreach (var node in links.deferredNodes.Values())
            checkDeferredNode(node);
        links.deferredNodes = new();
    }

    private void checkDeferredNode(Node node)
    {
        var saved = currentNode;
        currentNode = node;
        switch (node.Kind)
        {
            case Kind.ObjectLiteralExpression:
                checkContextualDeprecations(node);
                break;
            default:
                throw unsupported("checkDeferredNode: unselected deferred syntax");
        }

        currentNode = saved;
    }

    private void addDeferredDiagnostic(Action callback) => (deferredDiagnosticCallbacks ??= []).Add(callback);
    private void produceDeferredDiagnostics()
    {
        int count = deferredDiagnosticCallbacks?.Count ?? 0;
        for (int i = 0; i < count; i++)
            deferredDiagnosticCallbacks[i]();
        deferredDiagnosticCallbacks = null;
    }

    private void checkContextualDeprecations(Node node)
    {
        var contextualType = getApparentTypeOfContextualType(node);
        foreach (var property in node.Properties() ?? [])
            if (property.Name() != null && property.Name().Kind != Kind.ComputedPropertyName)
                checkDeprecatedProperty(property.Name(), contextualType);
    }

    private void checkDeprecatedProperty(Node name, Type contextualType)
    {
        if (contextualType == null)
            return;
        var property = getPropertyOfType(contextualType, name.Text());
        if (property == null || (property.Declarations?.Count ?? 0) == 0)
            return;
        if (isDeprecatedSymbol(property))
            throw unsupported("checkDeprecatedProperty: deprecated tags");
    }

    private bool isDeprecatedSymbol(Symbol symbol)
    {
        bool isDeprecated(Node declaration) => Ast.IsDeprecatedDeclarationWithCachedFlags(declaration, getCombinedNodeFlagsCached(declaration));
        var parentSymbol = getParentOfSymbol(symbol);
        if (parentSymbol != null && (symbol.Declarations?.Count ?? 0) > 1)
            return (parentSymbol.Flags & SymbolFlags.Interface) != 0 ? symbol.Declarations.Any(isDeprecated) : symbol.Declarations.All(isDeprecated);
        return symbol.ValueDeclaration != null && isDeprecated(symbol.ValueDeclaration) || (symbol.Declarations?.Count ?? 0) != 0 && symbol.Declarations.All(isDeprecated);
    }
}

public sealed partial class Checker
{
    // checker.go:17764/18016. The value record retains the entire selected
    // XXH3 input preimage; opaque hash representation is a managed adaptation.
    private readonly record struct CacheHashKey(uint sourceId, uint targetId, IntersectionState intersectionState);
    private static (CacheHashKey key, bool constrained) getRelationKey(Type source, Type target, IntersectionState intersectionState, bool isIdentity, bool ignoreConstraints)
    {
        if (isIdentity && source.id > target.id)
            (source, target) = (target, source);
        if ((source.objectFlags & ObjectFlags.Reference) != 0 || (target.objectFlags & ObjectFlags.Reference) != 0)
            throw unsupported("getRelationKey: generic/reference type arguments");
        return (new(source.id, target.id, intersectionState), false);
    }

    // checker.go:538. Widening contexts and their caches retain child identity.
    private sealed class WideningContext
    {
        internal WideningContext parent;
        internal string propertyName;
        internal List<Type> siblings;
        internal List<Symbol> resolvedProperties;
        internal Dictionary<string, WideningContext> childContexts;
        internal Dictionary<Type, Type> widenedTypes;
        internal WideningContext getChildContext(string name)
        {
            if (childContexts != null && childContexts.TryGetValue(name, out var cached))
                return cached;
            var result = new WideningContext
            {
                parent = this,
                propertyName = name
            };
            (childContexts ??= new(StringComparer.Ordinal))[name] = result;
            return result;
        }
    }

    private readonly Dictionary<string, Symbol> undefinedProperties = new(StringComparer.Ordinal);
    private Type getWidenedTypeWithContext(Type t, WideningContext context)
    {
        if ((t.objectFlags & ObjectFlags.RequiresWidening) != 0)
        {
            var key = new CachedTypeKey(CachedTypeKind.Widened, t.id);
            if (context == null && cachedTypes.TryGetValue(key, out var cached))
                return cached;
            Type result = null;
            if ((t.flags & (TypeFlags.Any | TypeFlags.Nullable)) != 0)
                result = anyType;
            else if (isObjectLiteralType(t))
                result = getWidenedTypeOfObjectLiteral(t, context);
            else if ((t.flags & TypeFlags.Union) != 0)
            {
                var unionContext = context ?? new WideningContext
                {
                    siblings = t.Types()
                };
                var widenedTypes = t.Types().Select(s => (s.flags & TypeFlags.Nullable) != 0 ? s : getWidenedTypeWithContext(s, unionContext)).ToList();
                result = getUnionTypeEx(widenedTypes, widenedTypes.Any(isEmptyObjectType) ? UnionReduction.Subtype : UnionReduction.Literal, null, null);
            }
            else if ((t.flags & TypeFlags.Intersection) != 0 || (t.objectFlags & (ObjectFlags.Reference | ObjectFlags.Tuple)) != 0)
                throw unsupported("getWidenedTypeWithContext: intersection/array/tuple");
            if (result != null && context == null)
                cachedTypes[key] = result;
            return result ?? t;
        }

        return t;
    }

    private Type getWidenedTypeOfObjectLiteral(Type t, WideningContext context)
    {
        if (context?.widenedTypes != null && context.widenedTypes.TryGetValue(t, out var cached))
            return cached;
        var members = new SymbolTable();
        foreach (var property in getPropertiesOfObjectType(t) ?? [])
            members[property.Name] = getWidenedProperty(property, context);
        if (context != null)
            foreach (var property in getPropertiesOfContext(context))
                if (!members.ContainsKey(property.Name))
                    members[property.Name] = getUndefinedProperty(property);
        var resolved = resolveStructuredTypeMembers(t);
        if ((resolved.indexInfos?.Count ?? 0) != 0)
            throw unsupported("getWidenedTypeOfObjectLiteral: index infos");
        var result = newAnonymousType(t.symbol, members);
        result.objectFlags |= t.objectFlags & (ObjectFlags.JSLiteral | ObjectFlags.NonInferrableType);
        if (context?.parent != null)
            (context.widenedTypes ??= new())[t] = result;
        return result;
    }

    private Symbol getWidenedProperty(Symbol property, WideningContext context)
    {
        if ((property.Flags & SymbolFlags.Property) == 0)
            return property;
        var original = getTypeOfSymbol(property);
        var propertyContext = context?.getChildContext(property.Name);
        var widened = getWidenedTypeWithContext(original, propertyContext);
        return widened == original ? property : createSymbolWithType(property, widened);
    }

    private List<Symbol> getPropertiesOfContext(WideningContext context)
    {
        if (context.resolvedProperties == null)
        {
            // Managed insertion-ordered map preserves first insertion position
            // and replacement of a repeated key, like collections.OrderedMap.
            var names = new System.Collections.Generic.OrderedDictionary<string, Symbol>(StringComparer.Ordinal);
            foreach (var t in getSiblingsOfContext(context))
                if (isObjectLiteralType(t) && (t.objectFlags & ObjectFlags.ContainsSpread) == 0)
                    foreach (var property in getPropertiesOfType(t) ?? [])
                        names[property.Name] = property;
            context.resolvedProperties = names.Values.ToList();
        }

        return context.resolvedProperties;
    }

    private List<Type> getSiblingsOfContext(WideningContext context)
    {
        if (context.siblings == null)
        {
            var siblings = new List<Type>();
            foreach (var t in getSiblingsOfContext(context.parent))
                if (isObjectLiteralType(t))
                {
                    var property = getPropertyOfObjectType(t, context.propertyName);
                    if (property != null)
                        siblings.AddRange(getTypeOfSymbol(property).Distributed() ?? []);
                }

            context.siblings = siblings;
        }

        return context.siblings;
    }

    private Symbol getUndefinedProperty(Symbol property)
    {
        if (undefinedProperties.TryGetValue(property.Name, out var cached))
            return cached;
        var result = createSymbolWithType(property, undefinedOrMissingType);
        result.Flags |= SymbolFlags.Optional;
        undefinedProperties[property.Name] = result;
        return result;
    }

    private bool isEmptyResolvedType(StructuredType t) => (t.properties?.Count ?? 0) == 0 && (t.signatures?.Count ?? 0) == 0 && (t.indexInfos?.Count ?? 0) == 0;
    private bool isEmptyObjectType(Type t)
    {
        if ((t.flags & TypeFlags.Object) != 0)
        {
            if ((t.objectFlags & ObjectFlags.Mapped) != 0)
                throw unsupported("isEmptyObjectType: mapped type");
            return isEmptyResolvedType(resolveStructuredTypeMembers(t));
        }

        if ((t.flags & TypeFlags.NonPrimitive) != 0)
            return true;
        if ((t.flags & TypeFlags.Union) != 0)
            return t.Types().Any(isEmptyObjectType);
        if ((t.flags & TypeFlags.Intersection) != 0)
            throw unsupported("isEmptyObjectType: intersection");
        return false;
    }
}

public sealed partial class Checker
{
    private RelationComparisonResult reliabilityFlags;
    private Relater freeRelater;
    private string getSuggestionForNonexistentProperty(string name, Type containingType) => getSpellingSuggestionForName(name, getPropertiesOfType(containingType) ?? [], SymbolFlags.Value)?.Name ?? "";
    private Symbol getParentOfSymbol(Symbol symbol) => symbol.Parent != null ? getMergedSymbol(getLateBoundSymbol(symbol.Parent)) : null;
    private Symbol getLateBoundSymbol(Symbol symbol)
    {
        foreach (var declaration in symbol.Declarations ?? [])
            if (declaration.Name()?.Kind == Kind.ComputedPropertyName)
                throw unsupported("getLateBoundSymbol: dynamic declaration");
        return symbol;
    }

    private void checkTypeLiteral(Node node)
    {
        checkSourceElements(node.Members());
        var t = getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode(node);
        checkIndexConstraints(t, t.symbol, false);
        checkTypeForDuplicateIndexSignatures(node);
        checkObjectTypeForDuplicateDeclarations(node, false);
    }

    private void checkPropertySignature(Node node)
    {
        if (node.Name().Kind == Kind.PrivateIdentifier)
            error(node, Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
        checkPropertyDeclaration(node);
    }

    private void checkPropertyDeclaration(Node node)
    {
        if (node.Kind != Kind.PropertySignature)
            throw unsupported("checkPropertyDeclaration: class property");
        if (!checkGrammarModifiers(node) && !checkGrammarProperty(node) && node.Name().Kind == Kind.ComputedPropertyName)
            throw unsupported("checkPropertyDeclaration: computed name");
        checkVariableLikeDeclaration(node);
    }

    private void checkIndexConstraints(Type t, Symbol symbol, bool isStaticIndex)
    {
        if ((resolveStructuredTypeMembers(t).indexInfos?.Count ?? 0) == 0)
            return;
        throw unsupported("checkIndexConstraints: nonempty index signatures");
    }

    private void checkTypeForDuplicateIndexSignatures(Node node)
    {
        var indexSymbol = getMembersOfSymbol(getSymbolOfDeclaration(node))?[Ast.InternalSymbolNameIndex];
        if (indexSymbol == null || (indexSymbol.Declarations?.Count ?? 0) <= 1)
            return;
        throw unsupported("checkTypeForDuplicateIndexSignatures: index signatures");
    }

    private void checkObjectTypeForDuplicateDeclarations(Node node, bool checkPrivateNames)
    {
        Dictionary<string, int> instanceNames = null;
        foreach (var member in node.Members() ?? [])
        {
            if (member.Kind != Kind.PropertySignature)
                throw unsupported("checkObjectTypeForDuplicateDeclarations: nondata member");
            var symbol = getSymbolOfDeclaration(member);
            if ((symbol.Declarations?.Count ?? 0) > 1)
            {
                instanceNames ??= new(StringComparer.Ordinal);
                instanceNames.TryGetValue(symbol.Name, out int state);
                if (state == 0)
                    instanceNames[symbol.Name] = 1;
                else if (state == 1)
                {
                    reportDuplicateMemberErrors(node, symbol.Name, true, false, Diagnostics.Duplicate_identifier_0);
                    instanceNames[symbol.Name] = 3;
                }
            }
        }
    }

    private void reportDuplicateMemberErrors(Node node, string name, bool checkStatic, bool isStatic, Message message)
    {
        foreach (var member in node.Members() ?? [])
        {
            if (member.Kind == Kind.Constructor)
                throw unsupported("reportDuplicateMemberErrors: constructor");
            var symbol = getSymbolOfDeclaration(member);
            if (symbol != null && symbol.Name == name && (!checkStatic || isStatic == Ast.HasStaticModifier(member)))
                error(member.Name(), message, symbolToString(symbol));
        }
    }
}

public sealed partial class Checker
{
    private (string, bool) getEffectivePropertyNameForPropertyNameNode(Node node)
    {
        var name = Ast.GetPropertyNameForPropertyNameNode(node);
        if (name != Ast.InternalSymbolNameMissing)
            return (name, true);
        if (node.Kind == Kind.ComputedPropertyName)
            throw unsupported("getEffectivePropertyNameForPropertyNameNode: computed name");
        return ("", false);
    }
}

public sealed partial class Checker
{
    // checker.go:399. Exact TypeFacts constants.
    [Flags]
    internal enum TypeFacts : uint
    {
        None = 0,
        TypeofEQString = 1 << 0,
        TypeofEQNumber = 1 << 1,
        TypeofEQBigInt = 1 << 2,
        TypeofEQBoolean = 1 << 3,
        TypeofEQSymbol = 1 << 4,
        TypeofEQObject = 1 << 5,
        TypeofEQFunction = 1 << 6,
        TypeofEQHostObject = 1 << 7,
        TypeofNEString = 1 << 8,
        TypeofNENumber = 1 << 9,
        TypeofNEBigInt = 1 << 10,
        TypeofNEBoolean = 1 << 11,
        TypeofNESymbol = 1 << 12,
        TypeofNEObject = 1 << 13,
        TypeofNEFunction = 1 << 14,
        TypeofNEHostObject = 1 << 15,
        EQUndefined = 1 << 16,
        EQNull = 1 << 17,
        EQUndefinedOrNull = 1 << 18,
        NEUndefined = 1 << 19,
        NENull = 1 << 20,
        NEUndefinedOrNull = 1 << 21,
        Truthy = 1 << 22,
        Falsy = 1 << 23,
        IsUndefined = 1 << 24,
        IsNull = 1 << 25,
        IsUndefinedOrNull = IsUndefined | IsNull,
        All = (1 << 27) - 1,
        BaseStringStrictFacts = TypeofEQString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | NEUndefined | NENull | NEUndefinedOrNull,
        BaseStringFacts = BaseStringStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        StringStrictFacts = BaseStringStrictFacts | Truthy | Falsy,
        StringFacts = BaseStringFacts | Truthy,
        EmptyStringStrictFacts = BaseStringStrictFacts | Falsy,
        EmptyStringFacts = BaseStringFacts,
        NonEmptyStringStrictFacts = BaseStringStrictFacts | Truthy,
        NonEmptyStringFacts = BaseStringFacts | Truthy,
        BaseNumberStrictFacts = TypeofEQNumber | TypeofNEString | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | NEUndefined | NENull | NEUndefinedOrNull,
        BaseNumberFacts = BaseNumberStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        NumberStrictFacts = BaseNumberStrictFacts | Truthy | Falsy,
        NumberFacts = BaseNumberFacts | Truthy,
        ZeroNumberStrictFacts = BaseNumberStrictFacts | Falsy,
        ZeroNumberFacts = BaseNumberFacts,
        NonZeroNumberStrictFacts = BaseNumberStrictFacts | Truthy,
        NonZeroNumberFacts = BaseNumberFacts | Truthy,
        BaseBigIntStrictFacts = TypeofEQBigInt | TypeofNEString | TypeofNENumber | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | NEUndefined | NENull | NEUndefinedOrNull,
        BaseBigIntFacts = BaseBigIntStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        BigIntStrictFacts = BaseBigIntStrictFacts | Truthy | Falsy,
        BigIntFacts = BaseBigIntFacts | Truthy,
        ZeroBigIntStrictFacts = BaseBigIntStrictFacts | Falsy,
        ZeroBigIntFacts = BaseBigIntFacts,
        NonZeroBigIntStrictFacts = BaseBigIntStrictFacts | Truthy,
        NonZeroBigIntFacts = BaseBigIntFacts | Truthy,
        BaseBooleanStrictFacts = TypeofEQBoolean | TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | NEUndefined | NENull | NEUndefinedOrNull,
        BaseBooleanFacts = BaseBooleanStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        BooleanStrictFacts = BaseBooleanStrictFacts | Truthy | Falsy,
        BooleanFacts = BaseBooleanFacts | Truthy,
        FalseStrictFacts = BaseBooleanStrictFacts | Falsy,
        FalseFacts = BaseBooleanFacts,
        TrueStrictFacts = BaseBooleanStrictFacts | Truthy,
        TrueFacts = BaseBooleanFacts | Truthy,
        SymbolStrictFacts = TypeofEQSymbol | TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | NEUndefined | NENull | NEUndefinedOrNull | Truthy,
        SymbolFacts = SymbolStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        ObjectStrictFacts = TypeofEQObject | TypeofEQHostObject | TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEFunction | NEUndefined | NENull | NEUndefinedOrNull | Truthy,
        ObjectFacts = ObjectStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        FunctionStrictFacts = TypeofEQFunction | TypeofEQHostObject | TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | NEUndefined | NENull | NEUndefinedOrNull | Truthy,
        FunctionFacts = FunctionStrictFacts | EQUndefined | EQNull | EQUndefinedOrNull | Falsy,
        VoidFacts = TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | EQUndefined | EQUndefinedOrNull | NENull | Falsy,
        UndefinedFacts = TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | TypeofNEHostObject | EQUndefined | EQUndefinedOrNull | NENull | Falsy | IsUndefined,
        NullFacts = TypeofEQObject | TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEFunction | TypeofNEHostObject | EQNull | EQUndefinedOrNull | NEUndefined | Falsy | IsNull,
        EmptyObjectStrictFacts = All & ~(EQUndefined | EQNull | EQUndefinedOrNull | IsUndefinedOrNull),
        EmptyObjectFacts = All & ~IsUndefinedOrNull,
        UnknownFacts = All & ~IsUndefinedOrNull,
        AllTypeofNE = TypeofNEString | TypeofNENumber | TypeofNEBigInt | TypeofNEBoolean | TypeofNESymbol | TypeofNEObject | TypeofNEFunction | NEUndefined,
        OrFactsMask = TypeofEQFunction | TypeofNEObject,
        AndFactsMask = All & ~OrFactsMask,
    }
}

public sealed partial class Checker
{
    private TypeFacts getTypeFacts(Type t, TypeFacts mask) => getTypeFactsWorker(t, mask) & mask;
    private bool hasTypeFacts(Type t, TypeFacts mask) => getTypeFacts(t, mask) != 0;
    private TypeFacts getTypeFactsWorker(Type t, TypeFacts callerOnlyNeeds)
    {
        if ((t.flags & (TypeFlags.Intersection | TypeFlags.Instantiable)) != 0)
            throw unsupported("getTypeFactsWorker: constrained/intersection facts");
        var flags = t.flags;
        if ((flags & (TypeFlags.String | TypeFlags.StringMapping)) != 0)
            return strictNullChecks ? TypeFacts.StringStrictFacts : TypeFacts.StringFacts;
        if ((flags & (TypeFlags.StringLiteral | TypeFlags.TemplateLiteral)) != 0)
        {
            bool empty = (flags & TypeFlags.StringLiteral) != 0 && (string)t.AsLiteralType().value == "";
            return strictNullChecks ? empty ? TypeFacts.EmptyStringStrictFacts : TypeFacts.NonEmptyStringStrictFacts : empty ? TypeFacts.EmptyStringFacts : TypeFacts.NonEmptyStringFacts;
        }

        if ((flags & (TypeFlags.Number | TypeFlags.Enum)) != 0)
            return strictNullChecks ? TypeFacts.NumberStrictFacts : TypeFacts.NumberFacts;
        if ((flags & TypeFlags.NumberLiteral) != 0)
        {
            bool zero = ((JsNum.Number)t.AsLiteralType().value).Value == 0;
            return strictNullChecks ? zero ? TypeFacts.ZeroNumberStrictFacts : TypeFacts.NonZeroNumberStrictFacts : zero ? TypeFacts.ZeroNumberFacts : TypeFacts.NonZeroNumberFacts;
        }

        if ((flags & TypeFlags.BigInt) != 0)
            return strictNullChecks ? TypeFacts.BigIntStrictFacts : TypeFacts.BigIntFacts;
        if ((flags & TypeFlags.BigIntLiteral) != 0)
            throw unsupported("getTypeFactsWorker: bigint literal facts");
        if ((flags & TypeFlags.Boolean) != 0)
            return strictNullChecks ? TypeFacts.BooleanStrictFacts : TypeFacts.BooleanFacts;
        if ((flags & TypeFlags.BooleanLike) != 0)
        {
            bool isFalse = t == falseType || t == regularFalseType;
            return strictNullChecks ? isFalse ? TypeFacts.FalseStrictFacts : TypeFacts.TrueStrictFacts : isFalse ? TypeFacts.FalseFacts : TypeFacts.TrueFacts;
        }

        if ((flags & TypeFlags.Object) != 0)
        {
            var possibleFacts = strictNullChecks ? TypeFacts.EmptyObjectStrictFacts | TypeFacts.FunctionStrictFacts | TypeFacts.ObjectStrictFacts : TypeFacts.EmptyObjectFacts | TypeFacts.FunctionFacts | TypeFacts.ObjectFacts;
            if ((callerOnlyNeeds & possibleFacts) == 0)
                return TypeFacts.None;
            if ((t.objectFlags & ObjectFlags.Anonymous) != 0 && isEmptyObjectType(t))
                return strictNullChecks ? TypeFacts.EmptyObjectStrictFacts : TypeFacts.EmptyObjectFacts;
            if (isFunctionObjectType(t))
                return strictNullChecks ? TypeFacts.FunctionStrictFacts : TypeFacts.FunctionFacts;
            return strictNullChecks ? TypeFacts.ObjectStrictFacts : TypeFacts.ObjectFacts;
        }

        if ((flags & TypeFlags.Void) != 0)
            return TypeFacts.VoidFacts;
        if ((flags & TypeFlags.Undefined) != 0)
            return TypeFacts.UndefinedFacts;
        if ((flags & TypeFlags.Null) != 0)
            return TypeFacts.NullFacts;
        if ((flags & TypeFlags.ESSymbolLike) != 0)
            return strictNullChecks ? TypeFacts.SymbolStrictFacts : TypeFacts.SymbolFacts;
        if ((flags & TypeFlags.NonPrimitive) != 0)
            return strictNullChecks ? TypeFacts.ObjectStrictFacts : TypeFacts.ObjectFacts;
        if ((flags & TypeFlags.Never) != 0)
            return TypeFacts.None;
        if ((flags & TypeFlags.Union) != 0)
        {
            TypeFacts facts = TypeFacts.None;
            foreach (var constituent in t.Types())
                facts |= getTypeFactsWorker(constituent, callerOnlyNeeds);
            return facts;
        }

        return TypeFacts.UnknownFacts;
    }

    private bool isFunctionObjectType(Type t)
    {
        if ((t.objectFlags & ObjectFlags.EvolvingArray) != 0)
            return false;
        var resolved = resolveStructuredTypeMembers(t);
        return (resolved.signatures?.Count ?? 0) != 0 || resolved.members?["bind"] != null && isTypeSubtypeOf(t, globalFunctionType);
    }

    private Type globalFunctionType;
    internal Type getTypeWithFacts(Type t, TypeFacts include) => filterType(t, s => hasTypeFacts(s, include));
    private Type GetNonNullableType(Type t) => strictNullChecks ? getAdjustedTypeWithFacts(t, TypeFacts.NEUndefinedOrNull) : t;
    private Type getAdjustedTypeWithFacts(Type t, TypeFacts facts)
    {
        if (strictNullChecks && (t.flags & TypeFlags.Unknown) != 0)
            throw unsupported("getAdjustedTypeWithFacts: unknown-union/intersection adjustment");
        var reduced = getTypeWithFacts(t, facts);
        if (strictNullChecks)
        {
            switch (facts)
            {
                case TypeFacts.NEUndefinedOrNull:
                case TypeFacts.Truthy:
                    return mapType(reduced, s => hasTypeFacts(s, TypeFacts.EQUndefinedOrNull) ? getGlobalNonNullableTypeInstantiation(s) : s);
                case TypeFacts.NEUndefined:
                case TypeFacts.NENull:
                    throw unsupported("getAdjustedTypeWithFacts: single-nullable intersection");
            }
        }

        return reduced;
    }

    private Type getGlobalNonNullableTypeInstantiation(Type t)
    {
        // Fixed noLib options: getGlobalNonNullableTypeAlias() returns nil.
        return getIntersectionType([t, emptyObjectType]);
    }

    private Type checkNonNullExpression(Node node) => checkNonNullType(checkExpression(node), node);
    private Type checkNonNullType(Type t, Node node) => checkNonNullTypeWithReporter(t, node, reportObjectPossiblyNullOrUndefinedError);
    private Type checkNonNullTypeWithReporter(Type t, Node node, Action<Node, TypeFacts> reportError)
    {
        if (strictNullChecks && (t.flags & TypeFlags.Unknown) != 0)
        {
            if (Ast.IsEntityNameExpression(node))
            {
                var text = entityNameToString(node);
                if (text.Length < 100)
                {
                    error(node, Diagnostics.X_0_is_of_type_unknown, text);
                    return errorType;
                }
            }

            error(node, Diagnostics.Object_is_of_type_unknown);
            return errorType;
        }

        var facts = getTypeFacts(t, TypeFacts.IsUndefinedOrNull);
        if ((facts & TypeFacts.IsUndefinedOrNull) != 0)
        {
            reportError(node, facts);
            var nonNullable = GetNonNullableType(t);
            return (nonNullable.flags & (TypeFlags.Nullable | TypeFlags.Never)) != 0 ? errorType : nonNullable;
        }

        return t;
    }

    private void reportObjectPossiblyNullOrUndefinedError(Node node, TypeFacts facts)
    {
        var text = Ast.IsEntityNameExpression(node) ? entityNameToString(node) : "";
        if (node.Kind == Kind.NullKeyword)
        {
            error(node, Diagnostics.The_value_0_cannot_be_used_here, "null");
            return;
        }

        if (text != "" && text.Length < 100)
        {
            if (node.Kind == Kind.Identifier && text == "undefined")
            {
                error(node, Diagnostics.The_value_0_cannot_be_used_here, "undefined");
                return;
            }

            error(node, (facts & TypeFacts.IsUndefined) != 0 ? (facts & TypeFacts.IsNull) != 0 ? Diagnostics.X_0_is_possibly_null_or_undefined : Diagnostics.X_0_is_possibly_undefined : Diagnostics.X_0_is_possibly_null, text);
        }
        else
            error(node, (facts & TypeFacts.IsUndefined) != 0 ? (facts & TypeFacts.IsNull) != 0 ? Diagnostics.Object_is_possibly_null_or_undefined : Diagnostics.Object_is_possibly_undefined : Diagnostics.Object_is_possibly_null);
    }
}

public sealed partial class Checker
{
    private bool maybeTypeOfKind(Type t, TypeFlags kind)
    {
        if ((t.flags & kind) != 0)
            return true;
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            foreach (var constituent in t.Types())
                if (maybeTypeOfKind(constituent, kind))
                    return true;
        return false;
    }

    private Ternary compareProperties(Symbol sourceProperty, Symbol targetProperty, Func<Type, Type, Ternary> compareTypes)
    {
        if (sourceProperty == targetProperty)
            return Ternary.True;
        var sourceAccessibility = getDeclarationModifierFlagsFromSymbol(sourceProperty) & ModifierFlags.NonPublicAccessibilityModifier;
        var targetAccessibility = getDeclarationModifierFlagsFromSymbol(targetProperty) & ModifierFlags.NonPublicAccessibilityModifier;
        if (sourceAccessibility != targetAccessibility)
            return Ternary.False;
        if (sourceAccessibility != ModifierFlags.None)
        {
            if (getTargetSymbol(sourceProperty) != getTargetSymbol(targetProperty))
                return Ternary.False;
        }
        else if ((sourceProperty.Flags & SymbolFlags.Optional) != (targetProperty.Flags & SymbolFlags.Optional))
            return Ternary.False;
        if (isReadonlySymbol(sourceProperty) != isReadonlySymbol(targetProperty))
            return Ternary.False;
        return compareTypes(getNonMissingTypeOfSymbol(sourceProperty), getNonMissingTypeOfSymbol(targetProperty));
    }

    private Symbol getTargetSymbol(Symbol symbol)
    {
        if ((symbol.CheckFlags & CheckFlags.Instantiated) != 0)
            return valueSymbolLinks.Get(symbol).target;
        return symbol;
    }
}

public sealed partial class Checker
{
    private Type checkPropertyAccessExpression(Node node)
    {
        if ((node.Flags & NodeFlags.OptionalChain) != 0)
            throw unsupported("checkPropertyAccessExpression: optional chain");
        var expression = node.Expression();
        return checkPropertyAccessExpressionOrQualifiedName(node, expression, checkNonNullExpression(expression), node.Name());
    }

    private Type checkPropertyAccessExpressionOrQualifiedName(Node node, Node left, Type leftType, Node right)
    {
        var parentSymbol = getResolvedSymbolOrNil(left);
        if (Ast.IsAssignmentTarget(node))
            throw unsupported("checkPropertyAccessExpressionOrQualifiedName: write");
        var widenedType = leftType;
        var apparentType = getApparentType(widenedType);
        bool isAnyLike = (apparentType.flags & TypeFlags.Any) != 0 || apparentType == silentNeverType;
        if (right.Kind == Kind.PrivateIdentifier)
            throw unsupported("checkPropertyAccessExpressionOrQualifiedName: private identifier");
        if (isAnyLike)
            return isErrorType(apparentType) ? errorType : apparentType;
        var property = getPropertyOfTypeEx(apparentType, right.Text(), false, false);
        Type propertyType;
        if (property == null)
        {
            if ((getIndexInfosOfType(apparentType)?.Count ?? 0) != 0)
                throw unsupported("checkPropertyAccessExpressionOrQualifiedName: index signature");
            if (leftType.symbol == globalThisSymbol)
                throw unsupported("checkPropertyAccessExpressionOrQualifiedName: builtin globalThis");
            if (right.Text() != "")
                addDeferredDiagnostic(() => reportNonexistentProperty(right, leftType, false));
            return errorType;
        }

        var targetProperty = resolveAliasWithDeprecationCheck(property, right);
        if (isDeprecatedSymbol(targetProperty))
            throw unsupported("checkPropertyAccessExpressionOrQualifiedName: deprecated property");
        checkPropertyNotUsedBeforeDeclaration(property, node, right);
        markPropertyAsReferenced(property, node, false);
        symbolNodeLinks.Get(node).resolvedSymbol = property;
        checkPropertyAccessibility(node, false, false, apparentType, property);
        propertyType = getTypeOfSymbol(property);
        return getFlowTypeOfAccessExpression(node, property, propertyType, right);
    }

    private Symbol getResolvedSymbolOrNil(Node node) => symbolNodeLinks.TryGet(node)?.resolvedSymbol;
    private Symbol resolveAliasWithDeprecationCheck(Symbol symbol, Node location)
    {
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("resolveAliasWithDeprecationCheck: alias");
        return symbol;
    }

    private void checkPropertyNotUsedBeforeDeclaration(Symbol property, Node node, Node right)
    {
        if (property.ValueDeclaration?.Kind != Kind.PropertyDeclaration)
            return;
        throw unsupported("checkPropertyNotUsedBeforeDeclaration: class property initialization");
    }

    private void markPropertyAsReferenced(Symbol property, Node nodeForCheckWriteOnly, bool isSelfTypeAccess)
    {
        if ((property.Flags & SymbolFlags.ClassMember) == 0 || property.ValueDeclaration == null)
            return;
        bool privateModifier = (Ast.GetCombinedModifierFlags(property.ValueDeclaration) & ModifierFlags.Private) != 0;
        bool privateIdentifier = property.ValueDeclaration.Name()?.Kind == Kind.PrivateIdentifier;
        if (!privateModifier && !privateIdentifier)
            return;
        throw unsupported("markPropertyAsReferenced: private/self/write-only property");
    }

    private bool checkPropertyAccessibility(Node node, bool isSuper, bool writing, Type t, Symbol prop) => checkPropertyAccessibilityEx(node, isSuper, writing, t, prop, true);
    private bool checkPropertyAccessibilityEx(Node node, bool isSuper, bool writing, Type t, Symbol prop, bool reportError)
    {
        Node errorNode = null;
        if (reportError)
        {
            switch (node.Kind)
            {
                case Kind.PropertyAccessExpression:
                    errorNode = node.Name();
                    break;
                case Kind.QualifiedName:
                    errorNode = node.AsQualifiedName().Right;
                    break;
                case Kind.ImportType:
                    errorNode = node;
                    break;
                case Kind.BindingElement:
                    throw unsupported("checkPropertyAccessibilityEx: binding element");
                default:
                    errorNode = node.Name();
                    break;
            }
        }

        return checkPropertyAccessibilityAtLocation(node, isSuper, writing, t, prop, errorNode);
    }

    private bool checkPropertyAccessibilityAtLocation(Node location, bool isSuper, bool writing, Type containingType, Symbol prop, Node errorNode)
    {
        var flags = getDeclarationModifierFlagsFromSymbolEx(prop, writing);
        if (isSuper)
            throw unsupported("checkPropertyAccessibilityAtLocation: super/class field");
        if ((flags & ModifierFlags.Abstract) != 0)
            throw unsupported("checkPropertyAccessibilityAtLocation: abstract initialization");
        if ((flags & ModifierFlags.NonPublicAccessibilityModifier) == 0)
            return true;
        throw unsupported("checkPropertyAccessibilityAtLocation: private/protected declaring class");
    }

    private Type getFlowTypeOfAccessExpression(Node node, Symbol property, Type propertyType, Node errorNode)
    {
        if (Ast.IsAssignmentTarget(node))
            throw unsupported("getFlowTypeOfAccessExpression: assignment");
        if (property != null && (property.Flags & (SymbolFlags.Variable | SymbolFlags.Property | SymbolFlags.Accessor)) == 0 && !((property.Flags & SymbolFlags.Method) != 0 && (propertyType.flags & TypeFlags.Union) != 0))
            return propertyType;
        if (propertyType == autoType)
            throw unsupported("getFlowTypeOfAccessExpression: auto property flow");
        propertyType = getNarrowableTypeForReference(propertyType, node);
        if (property?.ValueDeclaration is { } declaration && (declaration.Kind == Kind.BinaryExpression || node.Expression()?.Kind == Kind.ThisKeyword))
            throw unsupported("getFlowTypeOfAccessExpression: constructor/assignment initialization");
        return getFlowTypeOfReferenceEx(node, propertyType, propertyType, null, null);
    }

    // checker.go:19366/19370. Resolving the structured union itself is observable
    // even when the retained signature slices are nil.
    private List<Signature> getSignaturesOfType(Type t, SignatureKind kind) => getSignaturesOfStructuredType(getReducedApparentType(t), kind);
    private List<Signature> getSignaturesOfStructuredType(Type t, SignatureKind kind)
    {
        if ((t.flags & TypeFlags.StructuredType) == 0)
            return null;
        var resolved = resolveStructuredTypeMembers(t);
        int start = kind == SignatureKind.Call ? 0 : resolved.callSignatureCount;
        int end = kind == SignatureKind.Call ? resolved.callSignatureCount : resolved.signatures?.Count ?? 0;
        if (end != start)
            throw unsupported("getSignaturesOfStructuredType: nonempty signatures");
        return GoRuntime.Slice(resolved.signatures, start, end);
    }

    private List<IndexInfo> getIndexInfosOfType(Type t) => getIndexInfosOfStructuredType(getReducedApparentType(t));
    private List<IndexInfo> getIndexInfosOfStructuredType(Type t) => (t.flags & TypeFlags.StructuredType) != 0 ? resolveStructuredTypeMembers(t).indexInfos : null;
    private readonly record struct NonExistentPropertyKey(Node node, Type type, bool uncheckedJS);
    private readonly HashSet<NonExistentPropertyKey> nonExistentProperties = new();
    private void reportNonexistentProperty(Node propertyNode, Type containingType, bool isUncheckedJS)
    {
        if (!nonExistentProperties.Add(new(propertyNode, containingType, isUncheckedJS)))
            return;
        var links = nodeLinks.Get(propertyNode);
        if ((links.flags & NodeCheckFlags.TypeChecked) != 0)
            return;
        links.flags |= NodeCheckFlags.TypeChecked;
        Diagnostic diagnostic = null;
        if (containingType.flags.HasFlag(TypeFlags.Union) && (containingType.flags & TypeFlags.Primitive) == 0)
            foreach (var subtype in containingType.Types())
                if (getPropertyOfType(subtype, propertyNode.Text()) == null && getApplicableIndexInfoForName(subtype, propertyNode.Text()) == null)
                {
                    diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(propertyNode), typeToStringByteString(subtype));
                    break;
                }

        if (typeHasStaticProperty(propertyNode.Text(), containingType))
            throw unsupported("reportNonexistentProperty: static member suggestion");
        var promisedType = GetPromisedTypeOfPromise(containingType);
        if (promisedType != null)
            throw unsupported("reportNonexistentProperty: promised property");
        var missingProperty = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(propertyNode);
        var container = typeToStringByteString(containingType);
        var libSuggestion = getSuggestedLibForNonExistentProperty(missingProperty, containingType);
        if (libSuggestion != "")
            diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_2_or_later, missingProperty, container, libSuggestion);
        else
        {
            var suggestion = getSuggestedSymbolForNonexistentProperty(propertyNode, containingType);
            if (suggestion != null)
            {
                var suggestedName = Ast.SymbolName(suggestion);
                diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1_Did_you_mean_2, missingProperty, container, suggestedName);
                if (suggestion.ValueDeclaration != null)
                    diagnostic.AddRelatedInfo(NewDiagnosticForNode(suggestion.ValueDeclaration, Diagnostics.X_0_is_declared_here, suggestedName));
            }
            else
            {
                if ((containingType.flags & TypeFlags.Intersection) != 0)
                    throw unsupported("reportNonexistentProperty: never intersection");
                if (containerSeemsToBeEmptyDomElement(containingType))
                    throw unsupported("reportNonexistentProperty: DOM library suggestion");
                diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1, missingProperty, container);
            }
        }

        diagnostics.Add(diagnostic);
    }

    private string getSuggestedLibForNonExistentProperty(string name, Type containingType)
    {
        var container = getApparentType(containingType).symbol;
        if (container != null && getFeatureMap().TryGetValue(container.Name, out var entries))
            foreach (var entry in entries)
                if (entry.props.Contains(name))
                    return entry.lib;
        return "";
    }

    private Symbol getSuggestedSymbolForNonexistentProperty(Node name, Type containingType)
    {
        var properties = getPropertiesOfType(containingType);
        if (name.Parent.Kind == Kind.PropertyAccessExpression)
            properties = properties?.Where(property => isValidPropertyAccessForCompletions(name.Parent, containingType, property)).ToList();
        return getSpellingSuggestionForName(name.Text(), properties ?? [], SymbolFlags.Value);
    }

    private bool isValidPropertyAccessForCompletions(Node node, Type t, Symbol property) => isPropertyAccessible(node, false, false, t, property);
    private bool isPropertyAccessible(Node node, bool isSuper, bool isWrite, Type containingType, Symbol property)
    {
        if ((containingType.flags & TypeFlags.Any) != 0)
            return true;
        if (property.ValueDeclaration?.Name()?.Kind == Kind.PrivateIdentifier)
            throw unsupported("isPropertyAccessible: private identifier class element");
        return checkPropertyAccessibilityAtLocation(node, isSuper, isWrite, containingType, property, null);
    }

    private bool typeHasStaticProperty(string name, Type containingType)
    {
        if (containingType.symbol != null)
        {
            var property = getPropertyOfType(getTypeOfSymbol(containingType.symbol), name);
            return property != null && property.ValueDeclaration != null && Ast.IsStatic(property.ValueDeclaration);
        }

        return false;
    }

    private bool containerSeemsToBeEmptyDomElement(Type t)
    {
        bool commonDom(Type type) => type.symbol != null && (type.symbol.Name is "EventTarget" or "Node" or "Element" || type.symbol.Name.StartsWith("HTML", StringComparison.Ordinal) && type.symbol.Name.EndsWith("Element", StringComparison.Ordinal));
        return (t.flags & TypeFlags.Union) != 0 ? t.Types().All(commonDom) && isEmptyObjectType(t) : commonDom(t) && isEmptyObjectType(t);
    }

    private IndexInfo getApplicableIndexInfoForName(Type t, string name)
    {
        if ((getIndexInfosOfType(t)?.Count ?? 0) != 0)
            throw unsupported("getApplicableIndexInfoForName: index signatures");
        return null;
    }

    private Type globalPromiseType;
    private Type getGlobalPromiseType() => globalPromiseType ??= getGlobalType("Promise", 1, false);
    private Type GetPromisedTypeOfPromise(Type t) => getPromisedTypeOfPromiseEx(t, null, null);
    private Type getPromisedTypeOfPromiseEx(Type t, Node errorNode, GoRef<Type> thisTypeForErrorOut)
    {
        if ((t.flags & TypeFlags.Any) != 0)
            return null;
        var key = new CachedTypeKey(CachedTypeKind.PromisedTypeOfPromise, t.id);
        if (cachedTypes.TryGetValue(key, out var cached))
            return cached;
        getGlobalPromiseType();
        if ((t.objectFlags & ObjectFlags.Reference) != 0)
            throw unsupported("getPromisedTypeOfPromiseEx: promise reference");
        if ((t.flags & TypeFlags.Instantiable) != 0)
            throw unsupported("getPromisedTypeOfPromiseEx: base constraint");
        if (allTypesAssignableToKind(t, TypeFlags.Primitive | TypeFlags.Never))
            return null;
        var thenFunction = getTypeOfPropertyOfType(t, "then");
        if (thenFunction != null && (thenFunction.flags & TypeFlags.Any) != 0)
            return null;
        var thenSignatures = thenFunction != null && (getReducedApparentType(thenFunction).flags & TypeFlags.StructuredType) != 0 ? resolveStructuredTypeMembers(getReducedApparentType(thenFunction)).CallSignatures() : null;
        if ((thenSignatures?.Count ?? 0) == 0)
        {
            if (errorNode != null)
                throw unsupported("getPromisedTypeOfPromiseEx: promise diagnostic/call profile");
            return null;
        }

        throw unsupported("getPromisedTypeOfPromiseEx: callable then/signature candidates");
    }
}

public sealed partial class Checker
{
    [Flags]
    private enum CheckMode : uint
    {
        Normal = 0,
        Contextual = 1,
        Inferential = 2,
        SkipContextSensitive = 4,
        SkipGenericFunctions = 8,
        IsForSignatureHelp = 16,
        RestBindingElement = 32,
        TypeOnly = 64,
        ForceTuple = 128
    }

    private readonly Dictionary<Node, Type> contextFreeTypes = new(ReferenceEqualityComparer.Instance);
    private Type getContextFreeTypeOfExpression(Node node)
    {
        if (contextFreeTypes.TryGetValue(node, out var cached))
            return cached;
        pushContextualType(node, anyType, false);
        var t = checkExpressionEx(node, CheckMode.SkipContextSensitive);
        contextFreeTypes[node] = t;
        popContextualType();
        return t;
    }

    private void resolveUnionTypeMembers(Type t)
    {
        // checker.go:21456. The selected constituents have no signatures/indexes;
        // reject an actual nonempty slot before the excluded combination algorithms.
        foreach (var constituent in t.Types())
        {
            if (typeHasCallOrConstructSignatures(constituent) || (getIndexInfosOfType(constituent)?.Count ?? 0) != 0)
                throw unsupported("resolveUnionTypeMembers: call/construct/index combination");
        }

        setStructuredTypeMembers(t, null, null, null, null);
    }

    private bool typeHasCallOrConstructSignatures(Type t) => (t.flags & TypeFlags.StructuredType) != 0 && (resolveStructuredTypeMembers(t).signatures?.Count ?? 0) != 0;
    private List<Symbol> getPropertiesOfUnionOrIntersectionType(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0)
            throw unsupported("getPropertiesOfUnionOrIntersectionType: intersection");
        var d = t.AsUnionOrIntersectionType();
        if (d.resolvedProperties == null)
        {
            var checkedNames = new HashSet<string>(StringComparer.Ordinal);
            var props = new List<Symbol>();
            foreach (var current in d.types)
            {
                foreach (var prop in getPropertiesOfType(current) ?? [])
                    if (checkedNames.Add(prop.Name))
                    {
                        var combined = getPropertyOfUnionOrIntersectionType(t, prop.Name, false);
                        if (combined != null)
                            props.Add(combined);
                    }

                if ((t.flags & TypeFlags.Union) != 0 && (getIndexInfosOfType(current)?.Count ?? 0) == 0)
                    break;
            }

            d.resolvedProperties = props;
        }

        return d.resolvedProperties;
    }

    private Symbol getPropertyOfUnionOrIntersectionType(Type t, string name, bool skipObjectFunctionPropertyAugment)
    {
        var prop = getUnionOrIntersectionProperty(t, name, skipObjectFunctionPropertyAugment);
        return prop != null && (prop.CheckFlags & CheckFlags.ReadPartial) == 0 ? prop : null;
    }

    private Symbol getUnionOrIntersectionProperty(Type t, string name, bool skipObjectFunctionPropertyAugment)
    {
        var d = t.AsUnionOrIntersectionType();
        var cache = skipObjectFunctionPropertyAugment ? d.propertyCacheWithoutFunctionPropertyAugment ??= new SymbolTable() : d.propertyCache ??= new SymbolTable();
        if (cache[name] != null)
            return cache[name];
        var prop = createUnionOrIntersectionProperty(t, name, skipObjectFunctionPropertyAugment);
        if (prop != null)
        {
            cache[name] = prop;
            if (skipObjectFunctionPropertyAugment && (prop.CheckFlags & CheckFlags.Partial) == 0)
            {
                var augmented = d.propertyCache ??= new SymbolTable();
                if (augmented[name] == null)
                    augmented[name] = prop;
            }
        }

        return prop;
    }

    private Symbol createUnionOrIntersectionProperty(Type containingType, string name, bool skipObjectFunctionPropertyAugment)
    {
        if ((containingType.flags & TypeFlags.Intersection) != 0)
            throw unsupported("createUnionOrIntersectionProperty: intersection");
        SymbolFlags propFlags = 0, optionalFlag = 0;
        Symbol singleProp = null;
        var propSet = new List<Symbol>();
        List<Type> indexTypes = null;
        CheckFlags checkFlags = 0;
        var syntheticFlag = CheckFlags.SyntheticMethod;
        foreach (var current in containingType.Types())
        {
            var t = getApparentType(current);
            if (isErrorType(t) || (t.flags & TypeFlags.Never) != 0)
                continue;
            var prop = getPropertyOfTypeEx(t, name, skipObjectFunctionPropertyAugment, false);
            if (prop != null)
            {
                var modifiers = getDeclarationModifierFlagsFromSymbol(prop);
                var writeModifiers = getDeclarationModifierFlagsFromSymbolEx(prop, true);
                if ((modifiers & ModifierFlags.NonPublicAccessibilityModifier) != 0 || (writeModifiers & ModifierFlags.NonPublicAccessibilityModifier) != 0)
                    throw unsupported("createUnionOrIntersectionProperty: nonpublic property");
                if ((prop.Flags & SymbolFlags.ClassMember) != 0)
                    optionalFlag |= prop.Flags & SymbolFlags.Optional;
                if (singleProp == null)
                {
                    singleProp = prop;
                    propFlags = (prop.Flags & SymbolFlags.Accessor) != 0 ? prop.Flags & SymbolFlags.Accessor : SymbolFlags.Property;
                }
                else if (prop != singleProp)
                {
                    if (getTargetSymbol(prop) == getTargetSymbol(singleProp) && compareProperties(singleProp, prop, (a, b) => a == b ? Ternary.True : Ternary.False) == Ternary.True)
                        throw unsupported("createUnionOrIntersectionProperty: equivalent generic instantiations");
                    if (propSet.Count == 0)
                        propSet.Add(singleProp);
                    if (!propSet.Contains(prop))
                        propSet.Add(prop);
                    if ((propFlags & SymbolFlags.Accessor) != 0 && (prop.Flags & SymbolFlags.Accessor) != (propFlags & SymbolFlags.Accessor))
                        propFlags = propFlags & ~SymbolFlags.Accessor | SymbolFlags.Property;
                }

                if (isReadonlySymbol(prop))
                    checkFlags |= CheckFlags.Readonly;
                checkFlags |= CheckFlags.ContainsPublic | CheckFlags.ContainsWritePublic;
                if ((modifiers & ModifierFlags.Static) != 0)
                    checkFlags |= CheckFlags.ContainsStatic;
                if ((prop.Flags & SymbolFlags.Prototype) == 0)
                    syntheticFlag = CheckFlags.SyntheticProperty;
            }
            else
            {
                if (!isLateBoundName(name) && getApplicableIndexInfoForName(t, name) != null)
                    throw unsupported("createUnionOrIntersectionProperty: index/tuple property");
                if ((t.objectFlags & ObjectFlags.ObjectLiteral) != 0 && (t.objectFlags & ObjectFlags.ContainsSpread) == 0)
                {
                    checkFlags |= CheckFlags.WritePartial;
                    (indexTypes ??= []).Add(undefinedType);
                }
                else
                    checkFlags |= CheckFlags.ReadPartial;
            }
        }

        if (singleProp == null)
            return null;
        if (propSet.Count == 0 && (checkFlags & CheckFlags.ReadPartial) == 0 && (indexTypes?.Count ?? 0) == 0)
            return singleProp;
        if (propSet.Count == 0)
            propSet.Add(singleProp);
        List<Node> declarations = null;
        Type firstType = null, nameType = null;
        var propTypes = new List<Type>();
        Node firstValueDeclaration = null;
        bool hasNonUniformValueDeclaration = false;
        foreach (var prop in propSet)
        {
            if (firstValueDeclaration == null)
                firstValueDeclaration = prop.ValueDeclaration;
            else if (prop.ValueDeclaration != null && prop.ValueDeclaration != firstValueDeclaration)
                hasNonUniformValueDeclaration = true;
            foreach (var declaration in prop.Declarations ?? [])
                if (!(declarations?.Contains(declaration) ?? false))
                    (declarations ??= []).Add(declaration);
            var t = getTypeOfSymbol(prop);
            if (firstType == null)
            {
                firstType = t;
                nameType = valueSymbolLinks.Get(prop).nameType;
            }

            if ((prop.Flags & SymbolFlags.Accessor) != 0)
                throw unsupported("createUnionOrIntersectionProperty: accessor write type");
            if (t != firstType)
                checkFlags |= CheckFlags.HasNonUniformType;
            if (isLiteralType(t))
                checkFlags |= CheckFlags.HasLiteralType;
            if ((t.flags & TypeFlags.Never) != 0)
                checkFlags |= CheckFlags.HasNeverType;
            propTypes.Add(t);
        }

        if (indexTypes != null)
            propTypes.AddRange(indexTypes);
        var result = newSymbolEx(propFlags | optionalFlag, name, checkFlags | syntheticFlag);
        result.Declarations = declarations;
        if (!hasNonUniformValueDeclaration && firstValueDeclaration != null)
        {
            result.ValueDeclaration = firstValueDeclaration;
            result.Parent = firstValueDeclaration.Symbol().Parent;
        }

        var links = valueSymbolLinks.Get(result);
        links.containingType = containingType;
        links.nameType = nameType;
        if (propTypes.Count > 2)
            throw unsupported("createUnionOrIntersectionProperty: deferred multiobject constituents");
        links.resolvedType = getUnionType(propTypes);
        return result;
    }

    private Type getTypeOfPropertyOrIndexSignatureOfType(Type t, string name)
    {
        var propertyType = getTypeOfPropertyOfType(t, name);
        if (propertyType != null)
            return propertyType;
        var indexInfo = getApplicableIndexInfoForName(t, name);
        if (indexInfo != null)
            throw unsupported("getTypeOfPropertyOrIndexSignatureOfType: index signature");
        return null;
    }

    private Type extractTypesOfKind(Type t, TypeFlags kind) => filterType(t, constituent => (constituent.flags & kind) != 0);
    private string getPropertyNameFromIndex(Type indexType, Node accessNode)
    {
        if (isTypeUsableAsPropertyName(indexType))
            return getPropertyNameFromType(indexType);
        if (accessNode != null && Ast.IsPropertyName(accessNode))
            return Ast.GetPropertyNameForPropertyNameNode(accessNode);
        return Ast.InternalSymbolNameMissing;
    }

    private Type getLiteralTypeFromProperty(Symbol prop, TypeFlags include, bool includeNonPublic)
    {
        if (includeNonPublic || (getDeclarationModifierFlagsFromSymbol(prop) & ModifierFlags.NonPublicAccessibilityModifier) == 0)
        {
            var t = valueSymbolLinks.Get(getLateBoundSymbol(prop)).nameType;
            if (t == null)
            {
                if (prop.Name == Ast.InternalSymbolNameDefault)
                    t = getStringLiteralType("default");
                else
                {
                    var name = Ast.GetNameOfDeclaration(prop.ValueDeclaration);
                    if (name != null)
                        t = getLiteralTypeFromPropertyName(name);
                    if (t == null && !IsKnownSymbol(prop))
                        t = getStringLiteralType(Ast.SymbolName(prop));
                }
            }

            if (t != null && (t.flags & include) != 0)
                return t;
        }

        return neverType;
    }

    private Type getLiteralTypeFromPropertyName(Node name)
    {
        if (name.Kind == Kind.PrivateIdentifier)
            return neverType;
        if (name.Kind == Kind.NumericLiteral)
            return getRegularTypeOfLiteralType(checkExpression(name));
        if (name.Kind == Kind.ComputedPropertyName)
            throw unsupported("getLiteralTypeFromPropertyName: computed name");
        var propertyName = Ast.GetPropertyNameForPropertyNameNode(name);
        if (propertyName != Ast.InternalSymbolNameMissing)
            return getStringLiteralType(propertyName);
        if (Ast.IsExpression(name))
            return getRegularTypeOfLiteralType(checkExpression(name));
        return neverType;
    }

    private Type getSuggestedTypeForNonexistentStringLiteralType(Type source, Type target) => Core.GetSpellingSuggestionWithMaxCandidateCount((string)source.AsLiteralType().value, target.Types().Where(t => (t.flags & TypeFlags.StringLiteral) != 0), t => (string)t.AsLiteralType().value, CompareTypes, 1000);
}

public sealed partial class Checker
{
    private readonly record struct DiscriminatedContextualTypeKey(ulong nodeId, uint typeId);
    private readonly Dictionary<DiscriminatedContextualTypeKey, Type> discriminatedContextualTypes = new();
    private sealed class ObjectLiteralDiscriminator(Checker c, List<Node> props, List<Symbol> members) : Discriminator
    {
        public int len() => props.Count + members.Count;
        public string name(int index) => index < props.Count ? props[index].Symbol().Name : members[index - props.Count].Name;
        public bool matches(int index, Type t)
        {
            Type propType;
            if (index < props.Count)
            {
                var prop = props[index];
                if (prop.Kind == Kind.PropertyAssignment)
                    propType = prop.Initializer() != null ? c.getContextFreeTypeOfExpression(prop.Initializer()) : c.trueType;
                else
                    propType = c.getContextFreeTypeOfExpression(prop.Name());
            }
            else
                propType = c.undefinedType;
            foreach (var s in propType.Distributed() ?? [])
                if (c.isTypeAssignableTo(s, t))
                    return true;
            return false;
        }
    }

    private Type discriminateContextualTypeByObjectMembers(Node node, Type contextualType)
    {
        var key = new DiscriminatedContextualTypeKey(Ast.GetNodeId(node), contextualType.id);
        if (discriminatedContextualTypes.TryGetValue(key, out var cached))
            return cached;
        var discriminated = getMatchingUnionConstituentForObjectLiteral(contextualType, node);
        if (discriminated == null)
        {
            var properties = (node.Properties() ?? []).Where(p => p.Symbol() != null && (p.Kind == Kind.PropertyAssignment ? isPossiblyDiscriminantValue(p.Initializer()) && isDiscriminantProperty(contextualType, p.Symbol().Name) : p.Kind == Kind.ShorthandPropertyAssignment && isDiscriminantProperty(contextualType, p.Symbol().Name))).ToList();
            var members = (getPropertiesOfType(contextualType) ?? []).Where(s => (s.Flags & SymbolFlags.Optional) != 0 && node.Symbol().Members[s.Name] == null && isDiscriminantProperty(contextualType, s.Name)).ToList();
            discriminated = discriminateTypeByDiscriminableItems(contextualType, new ObjectLiteralDiscriminator(this, properties, members));
        }

        discriminatedContextualTypes[key] = discriminated;
        return discriminated;
    }

    private Type getMatchingUnionConstituentForObjectLiteral(Type unionType, Node node)
    {
        var keyPropertyName = getKeyPropertyName(unionType);
        if (keyPropertyName != "")
        {
            var property = (node.Properties() ?? []).Find(p => p.Symbol() != null && p.Kind == Kind.PropertyAssignment && p.Symbol().Name == keyPropertyName && isPossiblyDiscriminantValue(p.Initializer()));
            if (property != null)
                return getConstituentTypeForKeyType(unionType, getContextFreeTypeOfExpression(property.Initializer()));
        }

        return null;
    }

    private bool isPossiblyDiscriminantValue(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateExpression:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
            case Kind.Identifier:
            case Kind.UndefinedKeyword:
                return true;
            case Kind.PropertyAccessExpression:
            case Kind.ParenthesizedExpression:
                return isPossiblyDiscriminantValue(node.Expression());
            case Kind.JsxExpression:
                return node.Expression() == null || isPossiblyDiscriminantValue(node.Expression());
            default:
                return false;
        }
    }
}

public sealed partial class Checker
{
    private enum IntersectionFlags : uint
    {
        None = 0,
        NoSupertypeReduction = 1,
        NoConstraintReduction = 2
    }

    // The selected complete cache preimage is ordered type IDs + flags. Aliases
    // and retained intersection payloads are excluded explicitly below.
    private readonly Dictionary<string, Type> intersectionTypes = new(StringComparer.Ordinal);
    private Type getIntersectionType(List<Type> types) => getIntersectionTypeEx(types, IntersectionFlags.None, null);
    private Type getIntersectionTypeEx(List<Type> types, IntersectionFlags flags, TypeAlias alias)
    {
        if (alias != null)
            throw unsupported("getIntersectionTypeEx: alias");
        var orderedTypes = new List<Type>(types.Count);
        var includes = addTypesToIntersection(orderedTypes, 0, types);
        var typeSet = orderedTypes;
        if ((includes & TypeFlags.Never) != 0)
            return typeSet.Contains(silentNeverType) ? silentNeverType : neverType;
        if (strictNullChecks && (includes & TypeFlags.Nullable) != 0 && (includes & (TypeFlags.Object | TypeFlags.NonPrimitive | TypeFlags.IncludesEmptyObject)) != 0 || (includes & TypeFlags.NonPrimitive) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.NonPrimitive)) != 0 || (includes & TypeFlags.StringLike) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.StringLike)) != 0 || (includes & TypeFlags.NumberLike) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.NumberLike)) != 0 || (includes & TypeFlags.BigIntLike) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.BigIntLike)) != 0 || (includes & TypeFlags.ESSymbolLike) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.ESSymbolLike)) != 0 || (includes & TypeFlags.VoidLike) != 0 && (includes & (TypeFlags.DisjointDomains & ~TypeFlags.VoidLike)) != 0)
            return neverType;
        if ((includes & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0)
            throw unsupported("getIntersectionTypeEx: template/string mapping");
        if ((includes & TypeFlags.Any) != 0)
            return (includes & TypeFlags.IncludesWildcard) != 0 ? wildcardType : (includes & TypeFlags.IncludesError) != 0 ? errorType : anyType;
        if (!strictNullChecks && (includes & TypeFlags.Nullable) != 0)
            return (includes & TypeFlags.IncludesEmptyObject) != 0 ? neverType : (includes & TypeFlags.Undefined) != 0 ? undefinedType : nullType;
        if ((includes & TypeFlags.String) != 0 && (includes & TypeFlags.StringLiteral) != 0 || (includes & TypeFlags.Number) != 0 && (includes & TypeFlags.NumberLiteral) != 0 || (includes & TypeFlags.BigInt) != 0 && (includes & TypeFlags.BigIntLiteral) != 0 || (includes & TypeFlags.ESSymbol) != 0 && (includes & TypeFlags.UniqueESSymbol) != 0 || (includes & TypeFlags.Void) != 0 && (includes & TypeFlags.Undefined) != 0 || (includes & TypeFlags.IncludesEmptyObject) != 0 && (includes & TypeFlags.DefinitelyNonNullable) != 0)
            if ((flags & IntersectionFlags.NoSupertypeReduction) == 0)
                typeSet = removeRedundantSupertypes(typeSet, includes);
        if ((includes & TypeFlags.IncludesMissingType) != 0)
            typeSet[typeSet.IndexOf(undefinedType)] = missingType;
        if (typeSet.Count == 0)
            return unknownType;
        if (typeSet.Count == 1)
            return typeSet[0];
        if ((includes & TypeFlags.Instantiable) != 0)
            throw unsupported("getIntersectionTypeEx: type variable constraint reduction");
        var key = ((uint)flags).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + string.Join(",", typeSet.Select(t => t.id));
        if (intersectionTypes.TryGetValue(key, out var result))
            return result;
        if ((includes & TypeFlags.Union) != 0)
        {
            var(reducedSet, reduced) = intersectUnionsOfPrimitiveTypes(typeSet);
            typeSet = reducedSet;
            if (reduced)
                result = getIntersectionTypeEx(typeSet, flags, null);
            else if (typeSet.All(isUnionWithUndefined))
            {
                var containedUndefined = typeSet.Any(containsMissingType) ? missingType : undefinedType;
                filterTypes(typeSet, t => (t.flags & TypeFlags.Undefined) == 0);
                result = getUnionTypeEx([getIntersectionTypeEx(typeSet, flags, null), containedUndefined], UnionReduction.Literal, null, null);
            }
            else if (typeSet.All(isUnionWithNull))
            {
                filterTypes(typeSet, t => (t.flags & TypeFlags.Null) == 0);
                result = getUnionTypeEx([getIntersectionTypeEx(typeSet, flags, null), nullType], UnionReduction.Literal, null, null);
            }
            else if (typeSet.Count >= 3 && types.Count > 2)
            {
                int middle = typeSet.Count / 2;
                result = getIntersectionTypeEx([getIntersectionTypeEx(typeSet.GetRange(0, middle), flags, null), getIntersectionTypeEx(typeSet.GetRange(middle, typeSet.Count - middle), flags, null)], flags, null);
            }
            else
            {
                if (!checkCrossProductUnion(typeSet))
                    return errorType;
                result = getUnionTypeEx(getCrossProductIntersections(typeSet, flags), UnionReduction.Literal, null, null);
            }
        }
        else
            throw unsupported("getIntersectionTypeEx: retained structural intersection payload");
        intersectionTypes[key] = result;
        return result;
    }

    private TypeFlags addTypesToIntersection(List<Type> typeSet, TypeFlags includes, List<Type> types)
    {
        foreach (var t in types)
            includes = addTypeToIntersection(typeSet, includes, getRegularTypeOfLiteralType(t));
        return includes;
    }

    private TypeFlags addTypeToIntersection(List<Type> typeSet, TypeFlags includes, Type t)
    {
        var flags = t.flags;
        if ((flags & TypeFlags.Intersection) != 0)
            throw unsupported("addTypeToIntersection: retained intersection");
        if (IsEmptyAnonymousObjectType(t))
        {
            if ((includes & TypeFlags.IncludesEmptyObject) == 0)
            {
                includes |= TypeFlags.IncludesEmptyObject;
                typeSet.Add(t);
            }
        }
        else
        {
            if ((flags & TypeFlags.AnyOrUnknown) != 0)
            {
                if (t == wildcardType)
                    includes |= TypeFlags.IncludesWildcard;
                if (isErrorType(t))
                    includes |= TypeFlags.IncludesError;
            }
            else if (strictNullChecks || (flags & TypeFlags.Nullable) == 0)
            {
                if (t == missingType)
                {
                    includes |= TypeFlags.IncludesMissingType;
                    t = undefinedType;
                }

                if (!typeSet.Contains(t))
                {
                    if ((t.flags & TypeFlags.Unit) != 0 && (includes & TypeFlags.Unit) != 0)
                        includes |= TypeFlags.NonPrimitive;
                    typeSet.Add(t);
                }
            }

            includes |= flags & TypeFlags.IncludesMask;
        }

        return includes;
    }

    private List<Type> removeRedundantSupertypes(List<Type> types, TypeFlags includes)
    {
        for (int i = types.Count - 1; i >= 0; i--)
        {
            var t = types[i];
            if ((t.flags & TypeFlags.String) != 0 && (includes & (TypeFlags.StringLiteral | TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0 || (t.flags & TypeFlags.Number) != 0 && (includes & TypeFlags.NumberLiteral) != 0 || (t.flags & TypeFlags.BigInt) != 0 && (includes & TypeFlags.BigIntLiteral) != 0 || (t.flags & TypeFlags.ESSymbol) != 0 && (includes & TypeFlags.UniqueESSymbol) != 0 || (t.flags & TypeFlags.Void) != 0 && (includes & TypeFlags.Undefined) != 0 || IsEmptyAnonymousObjectType(t) && (includes & TypeFlags.DefinitelyNonNullable) != 0)
                types.RemoveAt(i);
        }

        return types;
    }

    private (List<Type>, bool) intersectUnionsOfPrimitiveTypes(List<Type> types)
    {
        int index = types.FindIndex(t => (t.objectFlags & ObjectFlags.PrimitiveUnion) != 0);
        if (index < 0)
            return (types, false);
        var unionTypes = new List<Type>
        {
            types[index]
        };
        for (int i = index + 1; i < types.Count;)
            if ((types[i].objectFlags & ObjectFlags.PrimitiveUnion) != 0)
            {
                unionTypes.Add(types[i]);
                types.RemoveAt(i);
            }
            else
                i++;
        if (unionTypes.Count == 1)
            return (types, false);
        var checkedTypes = new List<Type>();
        var result = new List<Type>();
        foreach (var u in unionTypes)
            foreach (var t in u.Types())
                if (!checkedTypes.Contains(t))
                {
                    checkedTypes.Add(t);
                    if (eachUnionContains(unionTypes, t))
                    {
                        if (t == undefinedType && result.Count != 0 && result[0] == missingType)
                            continue;
                        if (t == missingType && result.Count != 0 && result[0] == undefinedType)
                        {
                            result[0] = missingType;
                            continue;
                        }

                        if (!result.Contains(t))
                        {
                            result.Add(t);
                            result.Sort((a, b) => a.id.CompareTo(b.id));
                        }
                    }
                }

        types[index] = getUnionTypeFromSortedList(result, ObjectFlags.PrimitiveUnion);
        return (types, true);
    }

    private bool eachUnionContains(List<Type> unions, Type t) => unions.All(u => unionContainsType(u, t, true));
    private bool unionContainsType(Type union, Type t, bool matchSymbol)
    {
        var types = union.Types();
        if (types.Contains(t))
            return true;
        if (t == missingType)
            return types.Contains(undefinedType);
        if (t == undefinedType)
            return types.Contains(missingType);
        Type primitive = (t.flags & TypeFlags.StringLiteral) != 0 ? stringType : (t.flags & (TypeFlags.Enum | TypeFlags.NumberLiteral)) != 0 ? numberType : (t.flags & TypeFlags.BigIntLiteral) != 0 ? bigintType : (t.flags & TypeFlags.UniqueESSymbol) != 0 && matchSymbol ? esSymbolType : null;
        return primitive != null && types.Contains(primitive);
    }

    private static bool isUnionWithUndefined(Type t) => (t.flags & TypeFlags.Union) != 0 && (t.Types()[0].flags & TypeFlags.Undefined) != 0;
    private static bool isUnionWithNull(Type t) => (t.flags & TypeFlags.Union) != 0 && ((t.Types()[0].flags & TypeFlags.Null) != 0 || (t.Types()[1].flags & TypeFlags.Null) != 0);
    private void filterTypes(List<Type> types, Func<Type, bool> predicate)
    {
        for (int i = 0; i < types.Count; i++)
            types[i] = filterType(types[i], predicate);
    }

    private int getCrossProductUnionSize(List<Type> types)
    {
        int size = 1;
        foreach (var t in types)
            if ((t.flags & TypeFlags.Union) != 0)
            {
                int n = t.Types().Count;
                if (n > 0 && size > int.MaxValue / n)
                    return int.MaxValue;
                size *= n;
            }
            else if ((t.flags & TypeFlags.Never) != 0)
                return 0;
        return size;
    }

    private bool checkCrossProductUnion(List<Type> types)
    {
        if (getCrossProductUnionSize(types) >= 100000)
        {
            error(currentNode, Diagnostics.Expression_produces_a_union_type_that_is_too_complex_to_represent);
            return false;
        }

        return true;
    }

    private List<Type> getCrossProductIntersections(List<Type> types, IntersectionFlags flags)
    {
        int count = getCrossProductUnionSize(types);
        List<Type> intersections = null;
        for (int i = 0; i < count; i++)
        {
            var constituents = new List<Type>(types);
            int n = i;
            for (int j = types.Count - 1; j >= 0; j--)
                if ((types[j].flags & TypeFlags.Union) != 0)
                {
                    var sourceTypes = types[j].Types();
                    constituents[j] = sourceTypes[n % sourceTypes.Count];
                    n /= sourceTypes.Count;
                }

            var t = getIntersectionTypeEx(constituents, flags, null);
            if ((t.flags & TypeFlags.Never) == 0)
                (intersections ??= []).Add(t);
        }

        return intersections ?? [];
    }
}

public sealed partial class Checker
{
    private Type stringNumberSymbolType;
    private readonly record struct PropertiesTypesKey(uint typeId, TypeFlags include, bool includeOrigin);
    private readonly Dictionary<PropertiesTypesKey, Type> propertiesTypes = new();
    private Type getIndexType(Type t) => getIndexTypeEx(t, IndexFlags.None);
    private Type getIndexTypeEx(Type t, IndexFlags indexFlags)
    {
        t = getReducedType(t);
        if ((t.flags & TypeFlags.Instantiable) != 0 || (t.objectFlags & (ObjectFlags.Reference | ObjectFlags.Mapped)) != 0)
            throw unsupported("getIndexTypeEx: generic/no-infer/mapped index");
        if ((t.flags & TypeFlags.Union) != 0)
            return getIntersectionType(t.Types().Select(part => getIndexTypeEx(part, indexFlags)).ToList());
        if ((t.flags & TypeFlags.Intersection) != 0)
            throw unsupported("getIndexTypeEx: intersection payload");
        if (t == wildcardType)
            return wildcardType;
        if ((t.flags & TypeFlags.Unknown) != 0)
            return neverType;
        if ((t.flags & (TypeFlags.Any | TypeFlags.Never)) != 0)
            return stringNumberSymbolType ??= getUnionType([stringType, numberType, esSymbolType]);
        var include = ((indexFlags & IndexFlags.NoIndexSignatures) != 0 ? TypeFlags.StringLiteral : TypeFlags.StringLike) | ((indexFlags & IndexFlags.StringsOnly) != 0 ? TypeFlags.None : TypeFlags.NumberLike | TypeFlags.ESSymbolLike);
        return getLiteralTypeFromProperties(t, include, indexFlags == IndexFlags.None);
    }

    private Type getLiteralTypeFromProperties(Type t, TypeFlags include, bool includeOrigin)
    {
        var key = new PropertiesTypesKey(t.id, include, includeOrigin);
        if (propertiesTypes.TryGetValue(key, out var cached))
            return cached;
        if (includeOrigin && (t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference)) != 0 || t.alias != null)
            throw unsupported("getLiteralTypeFromProperties: named index origin");
        var props = getPropertiesOfType(t);
        var indexInfos = getIndexInfosOfType(t);
        var types = new List<Type>((props?.Count ?? 0) + (indexInfos?.Count ?? 0));
        foreach (var prop in props ?? [])
            types.Add(getLiteralTypeFromProperty(prop, include, false));
        if ((indexInfos?.Count ?? 0) != 0)
            throw unsupported("getLiteralTypeFromProperties: index signatures");
        var result = getUnionTypeEx(types, UnionReduction.Literal, null, null);
        propertiesTypes[key] = result;
        return result;
    }

    private Type getIndexedAccessTypeOrUndefined(Type objectType, Type indexType, AccessFlags accessFlags, Node accessNode, TypeAlias alias)
    {
        if (alias != null)
            throw unsupported("getIndexedAccessTypeOrUndefined: alias");
        if (objectType == wildcardType || indexType == wildcardType)
            return wildcardType;
        objectType = getReducedType(objectType);
        if ((getIndexInfosOfType(objectType)?.Count ?? 0) != 0)
            throw unsupported("getIndexedAccessTypeOrUndefined: index signatures");
        if ((objectType.flags & TypeFlags.Instantiable) != 0 || (indexType.flags & TypeFlags.Instantiable) != 0 || (objectType.objectFlags & (ObjectFlags.Reference | ObjectFlags.Mapped)) != 0)
            throw unsupported("getIndexedAccessTypeOrUndefined: deferred generic/tuple index");
        var apparentObjectType = getReducedApparentType(objectType);
        if ((indexType.flags & TypeFlags.Union) != 0 && (indexType.flags & TypeFlags.Boolean) == 0)
        {
            List<Type> propTypes = null;
            bool wasMissing = false;
            foreach (var t in indexType.Types())
            {
                var propType = getPropertyTypeForIndexType(objectType, apparentObjectType, t, indexType, accessNode, accessFlags | (wasMissing ? AccessFlags.SuppressNoImplicitAnyError : 0));
                if (propType != null)
                    (propTypes ??= []).Add(propType);
                else if (accessNode == null)
                    return null;
                else
                    wasMissing = true;
            }

            if (wasMissing)
                return null;
            if ((accessFlags & AccessFlags.Writing) != 0)
                throw unsupported("getIndexedAccessTypeOrUndefined: write position");
            return getUnionTypeEx(propTypes ?? [], UnionReduction.Literal, null, null);
        }

        return getPropertyTypeForIndexType(objectType, apparentObjectType, indexType, indexType, accessNode, accessFlags | AccessFlags.CacheSymbol | AccessFlags.ReportDeprecated);
    }

    private Type getPropertyTypeForIndexType(Type originalObjectType, Type objectType, Type indexType, Type fullIndexType, Node accessNode, AccessFlags accessFlags)
    {
        if (accessNode?.Kind is Kind.ElementAccessExpression or Kind.IndexedAccessType or Kind.PrivateIdentifier)
            throw unsupported("getPropertyTypeForIndexType: source indexed/private access");
        var propName = getPropertyNameFromIndex(indexType, accessNode);
        if (propName != Ast.InternalSymbolNameMissing)
        {
            if ((accessFlags & AccessFlags.Contextual) != 0)
                return getTypeOfPropertyOfContextualTypeEx(objectType, propName, null) ?? anyType;
            var prop = getPropertyOfType(objectType, propName);
            if (prop != null)
            {
                if ((accessFlags & AccessFlags.ReportDeprecated) != 0 && accessNode != null && (prop.Declarations?.Count ?? 0) != 0 && isDeprecatedSymbol(prop))
                    throw unsupported("getPropertyTypeForIndexType: deprecated tag");
                if ((accessFlags & AccessFlags.Writing) != 0)
                    throw unsupported("getPropertyTypeForIndexType: write position");
                return getTypeOfSymbol(prop);
            }
        }

        if ((getIndexInfosOfType(objectType)?.Count ?? 0) != 0)
            throw unsupported("getPropertyTypeForIndexType: index signature fallback");
        if ((indexType.flags & TypeFlags.Nullable) == 0 && isTypeAssignableToKind(indexType, TypeFlags.StringLike | TypeFlags.NumberLike | TypeFlags.ESSymbolLike))
        {
            if ((objectType.flags & (TypeFlags.Any | TypeFlags.Never)) != 0)
                return objectType;
            if (accessNode != null)
                throw unsupported("getPropertyTypeForIndexType: source missing indexed property diagnostic");
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Diagnostic addDiagnostic(Diagnostic diagnostic) => serializationLevel < 2 ? diagnostics.Add(diagnostic) : diagnostic;
    private bool containsMissingType(Type t) => t == missingType || (t.flags & TypeFlags.Union) != 0 && t.Types()[0] == missingType;
    private Type addOptionality(Type t) => addOptionalityEx(t, false, true);
    private Type addOptionalityEx(Type t, bool isProperty, bool isOptional) => strictNullChecks && isOptional ? getOptionalType(t, isProperty) : t;
    private bool isTypeAssignableToKind(Type source, TypeFlags kind) => isTypeAssignableToKindEx(source, kind, false);
    private bool isTypeAssignableToKindEx(Type source, TypeFlags kind, bool strict)
    {
        if ((source.flags & kind) != 0)
            return true;
        if (strict && (source.flags & (TypeFlags.AnyOrUnknown | TypeFlags.Void | TypeFlags.Undefined | TypeFlags.Null)) != 0)
            return false;
        return (kind & TypeFlags.NumberLike) != 0 && isTypeAssignableTo(source, numberType) || (kind & TypeFlags.BigIntLike) != 0 && isTypeAssignableTo(source, bigintType) || (kind & TypeFlags.StringLike) != 0 && isTypeAssignableTo(source, stringType) || (kind & TypeFlags.BooleanLike) != 0 && isTypeAssignableTo(source, booleanType) || (kind & TypeFlags.Void) != 0 && isTypeAssignableTo(source, voidType) || (kind & TypeFlags.Never) != 0 && isTypeAssignableTo(source, neverType) || (kind & TypeFlags.Null) != 0 && isTypeAssignableTo(source, nullType) || (kind & TypeFlags.Undefined) != 0 && isTypeAssignableTo(source, undefinedType) || (kind & TypeFlags.ESSymbol) != 0 && isTypeAssignableTo(source, esSymbolType) || (kind & TypeFlags.NonPrimitive) != 0 && isTypeAssignableTo(source, nonPrimitiveType);
    }

    private Type getWriteTypeOfSymbol(Symbol symbol)
    {
        if ((symbol.CheckFlags & CheckFlags.SyntheticProperty) != 0)
        {
            if ((symbol.CheckFlags & CheckFlags.DeferredType) != 0)
                throw unsupported("getWriteTypeOfSymbol: deferred property");
            var links = valueSymbolLinks.Get(symbol);
            return links.writeType ?? links.resolvedType;
        }

        if ((symbol.Flags & SymbolFlags.Property) != 0)
            return removeMissingType(getTypeOfSymbol(symbol), (symbol.Flags & SymbolFlags.Optional) != 0);
        if ((symbol.Flags & SymbolFlags.Accessor) != 0)
            throw unsupported("getWriteTypeOfSymbol: accessor");
        return getTypeOfSymbol(symbol);
    }
}

public sealed partial class Checker
{
    internal bool isContextSensitive(Node node)
    {
        switch (node.Kind)
        {
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            case Kind.FunctionDeclaration:
                throw unsupported("isContextSensitive: function/method contextual inference");
            case Kind.ObjectLiteralExpression:
                return (node.Properties() ?? []).Any(isContextSensitive);
            case Kind.ArrayLiteralExpression:
                throw unsupported("isContextSensitive: array");
            case Kind.ConditionalExpression:
            case Kind.BinaryExpression:
                throw unsupported("isContextSensitive: conditional/binary expression");
            case Kind.PropertyAssignment:
                return isContextSensitive(node.Initializer());
            case Kind.ParenthesizedExpression:
                return isContextSensitive(node.Expression());
            case Kind.JsxAttributes:
            case Kind.JsxAttribute:
            case Kind.JsxExpression:
            case Kind.YieldExpression:
                throw unsupported("isContextSensitive: JSX/yield");
            default:
                return false;
        }
    }

    internal bool isConstContext(Node node)
    {
        var parent = node.Parent;
        if (parent?.Kind is Kind.AsExpression or Kind.TypeAssertionExpression)
            throw unsupported("isConstContext: const assertion");
        return isInlineImportAttributes(node) || isValidConstAssertionArgument(node) && isConstTypeVariable(getContextualType(node), 0) || parent?.Kind is Kind.ParenthesizedExpression or Kind.ArrayLiteralExpression or Kind.SpreadElement && isConstContext(parent) || parent?.Kind is Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.TemplateSpan && isConstContext(parent.Parent);
    }

    private bool isInlineImportAttributes(Node node)
    {
        if (node.Kind != Kind.ObjectLiteralExpression || node.Parent?.Kind != Kind.PropertyAssignment || node.Parent.Initializer() != node)
            return false;
        var property = node.Parent;
        if (property.Name().Kind is not (Kind.Identifier or Kind.StringLiteral) || property.Name().Text() != "with")
            return false;
        var options = property.Parent;
        if (options.Kind != Kind.ObjectLiteralExpression)
            return false;
        var importCall = Ast.FindAncestor(options, Ast.IsImportCall);
        if (importCall != null)
            throw unsupported("isInlineImportAttributes: dynamic import");
        return false;
    }

    private bool isValidConstAssertionArgument(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.ArrayLiteralExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.TemplateExpression:
                return true;
            case Kind.ParenthesizedExpression:
                return isValidConstAssertionArgument(node.Expression());
            case Kind.PrefixUnaryExpression:
                throw unsupported("isValidConstAssertionArgument: unary expression");
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                var expression = Ast.SkipParentheses(node.Expression());
                Symbol symbol = null;
                if (Ast.IsEntityNameExpression(expression))
                {
                    if (expression.Kind != Kind.Identifier)
                        throw unsupported("isValidConstAssertionArgument: qualified enum entity");
                    symbol = nameResolver.Resolve(expression, expression.Text(), SymbolFlags.Value, null, false, false);
                }

                return symbol != null && (symbol.Flags & SymbolFlags.Enum) != 0;
            }

            default:
                return false;
        }
    }

    private bool isConstTypeVariable(Type t, int depth)
    {
        if (depth >= 5 || t == null)
            return false;
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return t.Types().Any(s => isConstTypeVariable(s, depth));
        if ((t.flags & TypeFlags.Instantiable) != 0 || (t.objectFlags & (ObjectFlags.Reference | ObjectFlags.Mapped)) != 0)
            throw unsupported("isConstTypeVariable: generic parameter/mapped/reference");
        return false;
    }
}

public sealed partial class Checker
{
    private bool allTypesAssignableToKind(Type source, TypeFlags kind) => allTypesAssignableToKindEx(source, kind, false);
    private bool allTypesAssignableToKindEx(Type source, TypeFlags kind, bool strict) => (source.flags & TypeFlags.Union) != 0 ? source.Types().All(t => allTypesAssignableToKindEx(t, kind, strict)) : isTypeAssignableToKindEx(source, kind, strict);
}
