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
    internal Type anyType, autoType, wildcardType, errorType, unresolvedType, nonInferrableAnyType, intrinsicMarkerType, unknownType, undefinedType, undefinedWideningType, missingType, undefinedOrMissingType, optionalType, nullType, nullWideningType, stringType, numberType, bigintType, regularFalseType, falseType, regularTrueType, trueType, booleanType, esSymbolType, voidType, neverType, silentNeverType, implicitNeverType, unreachableNeverType, nonPrimitiveType, emptyObjectType, emptyGenericType;
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
        LiteralUnionBaseType = 0
    }

    private readonly record struct CachedTypeKey(CachedTypeKind kind, uint typeId);
    private readonly Dictionary<CachedTypeKey, Type> cachedTypes = new();
    private Type nanType;
    private Node currentNode, lastGetCombinedNodeFlagsNode;
    private NodeFlags lastGetCombinedNodeFlagsResult;
    private bool typeChecked;
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
        getGlobalType("Object", 0, true);
        getGlobalType("Function", 0, true);
        getGlobalType("String", 0, true);
        getGlobalType("Number", 0, true);
        getGlobalType("Boolean", 0, true);
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

    public List<Diagnostic> GetGlobalDiagnostics() => diagnostics.GetGlobalDiagnostics();
    // checkSourceFile (2240): script-only; no deferred/unused nodes admitted.
    private void checkSourceFile()
    {
        if (typeChecked)
            return;
        checkGrammarSourceFile();
        checkSourceElements(sourceFile.Statements.Nodes);
        typeChecked = true;
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
        if (node.Kind != Kind.VariableDeclaration || !Ast.IsIdentifier(node.Name()))
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

        checkExportsOnMergedDeclarations(node);
        checkVarDeclaredNamesNotShadowed(node);
        checkCollisionsForDeclarationName(node, name);
    }

    private void errorNextVariableOrPropertyDeclarationMustHaveSameType(Node first, Type firstType, Node next, Type nextType)
    {
        var name = next.Name();
        var text = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name);
        var d = error(name, Diagnostics.Subsequent_variable_declarations_must_have_the_same_type_Variable_0_must_be_of_type_1_but_here_has_type_2, text, typeToStringByteString(firstType), typeToStringByteString(nextType));
        if (first != null)
            d.AddRelatedInfo(NewDiagnosticForNode(first, Diagnostics.X_0_was_also_declared_here, text));
    }

    // areDeclarationFlagsIdentical (7067): selected variable declarations have no optionality.
    private bool areDeclarationFlagsIdentical(Node left, Node right)
    {
        if (Ast.IsParameterDeclaration(left) && Ast.IsVariableDeclaration(right) || Ast.IsVariableDeclaration(left) && Ast.IsParameterDeclaration(right))
            return true;
        if (left.Kind != Kind.VariableDeclaration || right.Kind != Kind.VariableDeclaration)
            throw unsupported("areDeclarationFlagsIdentical: optional declarations");
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
        if (symbol != null && symbol.Declarations?.Any(d => d.Kind != Kind.VariableDeclaration) == true && (symbol.Flags & SymbolFlags.Transient) == 0)
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
        return diagnostics.Add(d);
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
    private Type newAnonymousType(Symbol symbol, SymbolTable members)
    {
        var t = newObjectType(ObjectFlags.Anonymous, symbol);
        setStructuredTypeMembers(t, members);
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

    private Type getRegularTypeOfLiteralType(Type t)
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

    private Type getWidenedLiteralType(Type t)
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

    private Type getUnionType(List<Type> types) => getUnionTypeEx(types, UnionReduction.Literal, null, null);
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
            if ((flags & (TypeFlags.Instantiable | TypeFlags.Intersection | TypeFlags.Object | TypeFlags.EnumLike | TypeFlags.UniqueESSymbol)) != 0)
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
    private bool isErrorType(Type t) => t == errorType || t == unresolvedType;
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
                return Diagnostics.Cannot_find_name_0;
        }
    }

    // onFailedToResolveSymbol (1608): real primitive-name, lib and spelling paths.
    private void onFailedToResolveSymbol(Node location, string name, SymbolFlags meaning, Message message)
    {
        if (location != null)
        {
            if (!Ast.IsIdentifier(location) || location.Parent.Kind is not (Kind.VariableDeclaration or Kind.ParenthesizedExpression))
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
    private Type getTypeOfSymbol(Symbol symbol)
    {
        if (symbol.CheckFlags != CheckFlags.None && symbol != globalThisSymbol)
            throw unsupported("getTypeOfSymbol: transformed symbol");
        if ((symbol.Flags & (SymbolFlags.Variable | SymbolFlags.Property)) != 0)
            return getTypeOfVariableOrParameterOrProperty(symbol);
        if (valueSymbolLinks.Get(symbol).resolvedType is { } cached)
            return cached;
        throw unsupported("getTypeOfSymbol: nonvariable");
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
        if (declaration == null || declaration.Kind != Kind.VariableDeclaration)
            throw unsupported("getTypeOfVariableOrParameterOrPropertyWorker: declaration kind");
        if (!pushTypeResolution(symbol))
            return reportCircularityError(symbol);
        var result = getWidenedTypeForVariableLikeDeclaration(declaration, true);
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
        if (declaration.Kind != Kind.VariableDeclaration || declaration.Parent.Parent.Kind != Kind.VariableStatement)
            throw unsupported("getTypeForVariableLikeDeclaration: nonscalar declaration");
        var declaredType = tryGetTypeFromTypeNode(declaration);
        if (declaredType != null)
            return declaredType;
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
        if (expr.Kind is not (Kind.Identifier or Kind.StringLiteral or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword))
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

        if (declaration.Kind != Kind.VariableDeclaration)
            throw unsupported("widenTypeForVariableLikeDeclaration: parameter");
        if (reportErrors && noImplicitAny)
            throw unsupported("widenTypeForVariableLikeDeclaration: implicit any option");
        return anyType;
    }

    private Type getWidenedType(Type t)
    {
        if ((t.objectFlags & ObjectFlags.RequiresWidening) != 0)
        {
            if ((t.flags & (TypeFlags.Any | TypeFlags.Nullable)) != 0)
                return anyType;
            if ((t.flags & TypeFlags.Union) != 0)
                return getUnionType(t.Types().Select(s => (s.flags & TypeFlags.Nullable) != 0 ? s : getWidenedType(s)).ToList());
            throw unsupported("getWidenedType: object/intersection/array widening");
        }

        return t;
    }

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

    private Type checkExpression(Node node)
    {
        var saved = currentNode;
        currentNode = node;
        var t = checkExpressionWorker(node);
        currentNode = saved;
        return t;
    }

    private Type checkExpressionWorker(Node node)
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
                return checkExpression(node.Expression());
            default:
                throw unsupported("checkExpressionWorker: " + node.Kind);
        }
    }

    private Type getTypeFromTypeNode(Node node)
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
        if (Ast.IsVariableDeclaration(node))
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
        if (Ast.IsIdentifier(node))
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
        if (name.Kind != Kind.Identifier)
            throw unsupported("getSymbolOfNameOrPropertyAccessExpression: member/qualified name");
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
        if ((t.flags & TypeFlags.Object) != 0)
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
    private Type getTypeOfExpression(Node node)
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
    // getRegularTypeOfExpression (32668): no dotted reference forms admitted.
    private Type getRegularTypeOfExpression(Node node) => getRegularTypeOfLiteralType(getTypeOfExpression(node));
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

    private void setStructuredTypeMembers(Type t, SymbolTable members)
    {
        t.objectFlags |= ObjectFlags.MembersResolved;
        var data = t.AsStructuredType();
        data.members = members;
        data.properties = getNamedMembers(members, t.symbol);
    // Signature/index inputs of admitted initialization calls are all nil;
    // their payload fields remain explicitly excluded in the partial model.
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
        if ((t.flags & TypeFlags.Object) == 0)
            return false;
        throw unsupported("IsEmptyAnonymousObjectType: object structure");
    }
}

public sealed partial class Checker
{
    private string getSuggestedLibForNonExistentName(string name) => getFeatureMap().TryGetValue(name, out var features) ? features[0].lib : "";
}
