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
    internal bool strictNullChecks = true, noImplicitAny, noImplicitThis, strictFunctionTypes, exactOptionalPropertyTypes;
    public int SignatureCount { get; private set; }

    internal Type anyFunctionType;
    internal TypePredicate noTypePredicate;
    internal Signature anySignature, unknownSignature, resolvingSignature, silentNeverSignature;
    public SymbolTable Globals => globals;
    public Symbol GlobalThisSymbol => globalThisSymbol;
    public int TypeCount { get; private set; }
    public int SymbolCount { get; private set; }

    private readonly Dictionary<string, Type> stringLiteralTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<JsNum.Number, Type> numberLiteralTypes = new();
    private readonly Dictionary<CacheHashKey, Type> unionTypes = new();
    private readonly Dictionary<(uint, uint, UnionReduction, CacheHashKey), Type> unionOfUnionTypes = new();
    // checker.go:118/147. Only this cache kind is selected; other users remain excluded.
    private enum CachedTypeKind
    {
        LiteralUnionBaseType = 0,
        IndexType = 1,
        StringIndexType = 2,
        EquivalentBaseType = 3,
        ApparentType = 4,
        AwaitedType = 5,
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
    internal static Checker CreateCore(SourceFile file) => CreateCore(file, CheckerProfile.Legacy);
    internal static Checker CreateCore(SourceFile file, CheckerProfile profile)
    {
        Binder.BindSourceFile(file);
        var c = new Checker
        {
            sourceFile = file,
            compilerOptions = new CompilerOptions()
        };
        c.initializeScalarOptions(profile);
        c.emitStandardClassFields = c.compilerOptions.GetEmitStandardClassFields();
        c.undefinedSymbol = c.newSymbol(SymbolFlags.Property, "undefined");
        c.argumentsSymbol = c.newSymbol(SymbolFlags.Property, "arguments");
        c.requireSymbol = c.newSymbol(SymbolFlags.Property, "require");
        c.unknownSymbol = c.newSymbol(SymbolFlags.Property, "unknown");
        c.globalThisSymbol = c.newSymbolEx(SymbolFlags.Module, "globalThis", CheckFlags.Readonly);
        c.globalThisSymbol.Exports = c.globals;
        c.globals[c.globalThisSymbol.Name] = c.globalThisSymbol;
        c.evaluate = Evaluator.NewEvaluator(c.evaluateEntity, OuterExpressionKinds.Parentheses);
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
        c.numberOrBigIntType = c.getUnionType([c.numberType, c.bigintType]);
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
        c.uniqueLiteralType = c.newIntrinsicType(TypeFlags.Never, "never");
        c.uniqueLiteralMapper = newFunctionTypeMapper(c.getUniqueLiteralTypeForTypeParameter);
        c.permissiveMapper = newFunctionTypeMapper(c.permissiveMapperWorker);
        c.reportUnreliableMapper = newFunctionTypeMapper(c.reportUnreliableWorker);
        c.emptyObjectType = c.newAnonymousType(null, null);
        c.emptyGenericType = c.newAnonymousType(null, null);
        c.unknownEmptyObjectType = c.newAnonymousType(null, null);
        c.unknownUnionType = c.createUnknownUnionType();
        c.noConstraintType = c.newAnonymousType(null, null);
        c.circularConstraintType = c.newAnonymousType(null, null);
        c.emptyTypeLiteralType = c.newAnonymousType(c.newSymbol(SymbolFlags.TypeLiteral, Ast.InternalSymbolNameType), null);
        c.markerSuperType = c.newTypeParameter(null);
        c.markerSubType = c.newTypeParameter(null);
        c.markerSubType.AsTypeParameter().constraint = c.markerSuperType;
        c.markerOtherType = c.newTypeParameter(null);
        c.anyFunctionType = c.newAnonymousType(null, null);
        c.anyFunctionType.objectFlags |= ObjectFlags.NonInferrableType;
        c.noTypePredicate = new TypePredicate
        {
            kind = TypePredicateKind.Identifier,
            parameterIndex = 0,
            parameterName = "<<unresolved>>",
            t = c.anyType
        };
        c.anySignature = c.newSignature(SignatureFlags.None, null, null, null, null, c.anyType, null, 0);
        c.unknownSignature = c.newSignature(SignatureFlags.None, null, null, null, null, c.errorType, null, 0);
        c.resolvingSignature = c.newSignature(SignatureFlags.None, null, null, null, null, c.anyType, null, 0);
        c.silentNeverSignature = c.newSignature(SignatureFlags.None, null, null, null, null, c.silentNeverType, null, 0);
        c.emptyStringType = c.getStringLiteralType("");
        c.zeroType = c.getNumberLiteralType(new JsNum.Number(0));
        c.zeroBigIntType = c.getBigIntLiteralType(new JsNum.PseudoBigInt());
        c.typeofType = c.getUnionType(typeofNEFacts.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(c.getStringLiteralType).ToList());
        c.anyBaseTypeIndexInfo = c.newIndexInfo(c.stringType, c.anyType, false, null, null);
        c.getGlobalESSymbolConstructorSymbolOrNil = c.getGlobalValueSymbolResolver("Symbol", false);
        c.getGlobalESSymbolConstructorTypeSymbolOrNil = c.getGlobalTypeSymbolResolver("SymbolConstructor", false);
        c.getGlobalNaNSymbolOrNil = c.getGlobalValueSymbolResolver("NaN", false);
        c.getGlobalAwaitedSymbol = memoize(() => c.getGlobalTypeAliasSymbol("Awaited", 1, true));
        c.getGlobalAwaitedSymbolOrNil = memoize(() => c.getGlobalTypeAliasSymbol("Awaited", 1, false));
        c.getGlobalPromiseTypeChecked = memoize(() => c.getGlobalType("Promise", 1, true));
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
        valueSymbolLinks.Get(globalThisSymbol).resolvedType = newObjectType(ObjectFlags.Anonymous, globalThisSymbol);
        globalArrayType = getGlobalType("Array", 1, true);
        globalObjectType = getGlobalType("Object", 0, true);
        globalFunctionType = getGlobalType("Function", 0, true);
        globalCallableFunctionType = getGlobalStrictFunctionType("CallableFunction");
        globalNewableFunctionType = getGlobalStrictFunctionType("NewableFunction");
        globalThisType = getGlobalType("ThisType", 1, false);
        globalStringType = getGlobalType("String", 0, true);
        globalNumberType = getGlobalType("Number", 0, true);
        globalBooleanType = getGlobalType("Boolean", 0, true);
        getGlobalType("RegExp", 0, true);
        globalReadonlyArrayType = getGlobalType("ReadonlyArray", 1, false);
    }

    // getGlobalType (1210), missing built-ins in the fixed noLib environment.
    private Type getGlobalType(string name, int arity, bool reportErrors)
    {
        var symbol = getGlobalSymbol(name, SymbolFlags.Type, reportErrors ? Diagnostics.Cannot_find_global_type_0 : null);
        if (symbol != null)
        {
            if ((symbol.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
            {
                var t = getDeclaredTypeOfSymbol(symbol);
                if ((t.AsInterfaceType().TypeParameters()?.Count ?? 0) == arity)
                    return t;
                if (reportErrors)
                    error(getGlobalTypeDeclaration(symbol), Diagnostics.Global_type_0_must_have_1_type_parameter_s, Ast.SymbolName(symbol), arity);
            }
            else if (reportErrors)
                error(getGlobalTypeDeclaration(symbol), Diagnostics.Global_type_0_must_be_a_class_or_interface_type, Ast.SymbolName(symbol));
        }

        return arity != 0 ? emptyGenericType : emptyObjectType;
    }

    private static Node getGlobalTypeDeclaration(Symbol symbol) => symbol.Declarations?.Find(d => d.Kind is Kind.ClassDeclaration or Kind.InterfaceDeclaration or Kind.EnumDeclaration or Kind.TypeAliasDeclaration);
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
        var symbol = new Symbol
        {
            Flags = flags | SymbolFlags.Transient,
            Name = name,
            CheckFlags = checkFlags
        };
        authoredOwnedSymbols.Add(symbol);
        return symbol;
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
        var saveCurrentNode = currentNode;
        var saveWithinUnreachableCode = withinUnreachableCode;
        currentNode = node;
        instantiationCount = 0;
        checkSourceElementWorker(node);
        currentNode = saveCurrentNode;
        withinUnreachableCode = saveWithinUnreachableCode;
    }

    // checkSourceElementWorker (2301), retained scalar syntax dispatch.
    private void checkSourceElementWorker(Node node)
    {
        if (!withinUnreachableCode && compilerOptions.AllowUnreachableCode != Tristate.True && checkSourceElementUnreachable(node))
            withinUnreachableCode = true;
        switch (node.Kind)
        {
            case Kind.Parameter:
                checkParameter(node);
                break;
            case Kind.FunctionDeclaration:
                checkFunctionDeclaration(node);
                break;
            case Kind.FunctionType:
            case Kind.CallSignature:
                checkSignatureDeclaration(node);
                break;
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
                checkMethodDeclaration(node);
                break;
            case Kind.IfStatement:
                checkIfStatement(node);
                break;
            case Kind.ReturnStatement:
                checkReturnStatement(node);
                break;
            case Kind.ThrowStatement:
                checkThrowStatement(node);
                break;
            case Kind.ExpressionStatement:
                checkExpressionStatement(node);
                break;
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
            case Kind.TypeAliasDeclaration:
                checkTypeAliasDeclaration(node);
                break;
            case Kind.InterfaceDeclaration:
                checkInterfaceDeclaration(node);
                break;
            case Kind.TypeReference:
            case Kind.ExpressionWithTypeArguments:
                checkTypeReferenceNode(node);
                break;
            case Kind.UnionType:
            case Kind.IntersectionType:
                checkUnionOrIntersectionType(node);
                break;
            case Kind.EmptyStatement:
                checkGrammarStatementInAmbientContext(node);
                break;
            case Kind.TypeLiteral:
                checkTypeLiteral(node);
                break;
            case Kind.PropertySignature:
                checkPropertySignature(node);
                break;
            case Kind.ObjectKeyword:
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
                getTypeFromTypeNode(node);
                break;
            case Kind.ParenthesizedType:
                node.ForEachChild(child =>
                {
                    checkSourceElement(child);
                    return false;
                });
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
        {
            var saveFlowAnalysisDisabled = flowAnalysisDisabled;
            checkSourceElements(node.Statements());
            flowAnalysisDisabled = saveFlowAnalysisDisabled;
        }
        else
            checkSourceElements(node.Statements());
        if ((node.Locals()?.Count ?? 0) != 0)
            registerForUnusedIdentifiersCheck(node);
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
        if (node.Kind is not (Kind.VariableDeclaration or Kind.PropertySignature or Kind.Parameter) || node.Kind == Kind.VariableDeclaration && !Ast.IsIdentifier(node.Name()))
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

    private readonly Dictionary<Symbol, Symbol> mergedSymbols = new();
    internal Symbol getMergedSymbol(Symbol symbol) => symbol != null && mergedSymbols.TryGetValue(symbol, out var merged) ? merged : symbol;
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
        if (types.Count == 0)
            return neverType;
        if (types.Count == 1)
            return types[0];
        if (types.Count == 2 && origin == null && ((types[0].flags | types[1].flags) & TypeFlags.Union) != 0)
        {
            uint id1 = types[0].id, id2 = types[1].id;
            if (id1 > id2)
                (id1, id2) = (id2, id1);
            if (!unionOfUnionTypes.TryGetValue((id1, id2, reduction, getAliasKey(alias)), out var t))
                unionOfUnionTypes[(id1, id2, reduction, getAliasKey(alias))] = t = getUnionTypeWorker(types, reduction, alias, origin);
            return t;
        }

        return getUnionTypeWorker(types, reduction, alias, origin);
    }

    private Type getUnionTypeWorker(List<Type> types, UnionReduction reduction, TypeAlias alias, Type origin)
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
                typeSet = removeRedundantLiteralTypes(typeSet, includes, reduction == UnionReduction.Subtype);
            if ((includes & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping | TypeFlags.IncludesConstrainedTypeVariable)) != 0)
                throw unsupported("getUnionTypeWorker: template/constrained types");
            if (reduction == UnionReduction.Subtype)
            {
                typeSet = removeSubtypes(typeSet, (includes & TypeFlags.Object) != 0);
                if (typeSet == null)
                    return errorType;
            }

            if (typeSet.Count == 0)
            {
                if ((includes & TypeFlags.Null) != 0)
                    return nullType;
                if ((includes & TypeFlags.Undefined) != 0)
                    return undefinedType;
                return neverType;
            }
        }

        if (origin == null && (includes & TypeFlags.Union) != 0)
        {
            var named = addNamedUnions(null, types);
            List<Type> reduced = null;
            foreach (var t in typeSet)
                if (!(named?.Any(u => u.Types().Contains(t)) ?? false))
                    reduced = GoRuntime.Append(reduced, t);
            if (alias == null && named?.Count == 1 && (reduced?.Count ?? 0) == 0)
                return named[0];
            if ((named?.Sum(u => u.Types().Count) ?? 0) + (reduced?.Count ?? 0) == typeSet.Count)
            {
                foreach (var t in named ?? [])
                {
                    reduced ??= [];
                    if (!reduced.Contains(t))
                        reduced.Add(t);
                }

                reduced?.Sort(CompareTypes);
                origin = newUnionType(ObjectFlags.None, reduced);
            }
        }

        var objectFlags = (includes & TypeFlags.NotPrimitiveUnion) != 0 ? ObjectFlags.None : ObjectFlags.PrimitiveUnion;
        if ((includes & TypeFlags.Intersection) != 0)
            objectFlags |= ObjectFlags.ContainsIntersections;
        return getUnionTypeFromSortedList(typeSet, objectFlags, alias, origin);
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
            if ((flags & TypeFlags.EnumLike) != 0)
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

    private Type getUnionTypeFromSortedList(List<Type> types, ObjectFlags precomputedObjectFlags, TypeAlias alias = null, Type origin = null)
    {
        if (types.Count == 0)
            return neverType;
        if (types.Count == 1)
            return types[0];
        // Exact constituent identity tuple replaces opaque xxh3 cache key encoding.
        var key = getUnionKey(types, origin, alias);
        if (!unionTypes.TryGetValue(key, out var t))
        {
            t = newUnionType(precomputedObjectFlags | getPropagatingFlagsOfTypes(types, TypeFlags.Nullable), types);
            t.AsUnionType().origin = origin;
            t.alias = alias;
            if (types.Count == 2 && (types[0].flags & TypeFlags.BooleanLiteral) != 0 && (types[1].flags & TypeFlags.BooleanLiteral) != 0)
                t.flags |= TypeFlags.Boolean;
            unionTypes[key] = t;
        }

        return t;
    }

    internal Type getOptionalType(Type t, bool isProperty)
    {
        var missingOrUndefined = isProperty ? undefinedOrMissingType : undefinedType;
        if (t == missingOrUndefined || (t.flags & TypeFlags.Union) != 0 && t.Types()[0] == missingOrUndefined)
            return t;
        return getUnionType([t, missingOrUndefined]);
    }

    private Type convertAutoToAny(Type t) => t == autoType ? anyType : t;
    // checker.go:27172. Unresolved-name alias instances behave as error types.
    internal bool isErrorType(Type t) => t == errorType || (t.flags & TypeFlags.Any) != 0 && t.alias != null;
    private bool containsUndefinedType(Type t)
    {
        if (((t.flags & TypeFlags.Union) != 0))
        {
            t = t.Types()[0];
        }

        return ((t.flags & TypeFlags.Undefined) != 0);
    }
}

public sealed partial class Checker
{
    internal readonly LinkStore<Node, NodeLinks> nodeLinks = new();
    internal readonly LinkStore<Node, SignatureLinks> signatureLinks = new();
    internal readonly nodeLinkStore<SymbolNodeLinks> symbolNodeLinks = new();
    internal readonly symbolArenaLinkStore<MappedSymbolLinks> mappedSymbolLinks = new();
    internal readonly LinkStore<Node, TypeNodeLinks> typeNodeLinks = new();
    internal readonly symbolArenaLinkStore<ValueSymbolLinks> valueSymbolLinks = new();
    internal readonly symbolArenaLinkStore<SymbolReferenceLinks> symbolReferenceLinks = new();
    private enum TypeSystemPropertyName
    {
        Type,
        ResolvedBaseConstructorType,
        DeclaredType,
        ResolvedReturnType,
        ResolvedBaseConstraint,
        ResolvedTypeArguments,
        ResolvedBaseTypes,
        WriteType,
        InitializerIsUndefined,
        AliasTarget
    }

    private sealed class TypeResolution
    {
        internal object target;
        internal TypeSystemPropertyName propertyName;
        internal bool result;
    }

    private readonly List<TypeResolution> typeResolutions = new();
    private int resolutionStart;
    internal readonly symbolArenaLinkStore<TypeAliasLinks> typeAliasLinks = new();
    internal readonly symbolArenaLinkStore<DeclaredTypeLinks> declaredTypeLinks = new();
    internal readonly symbolArenaLinkStore<DeferredSymbolLinks> deferredSymbolLinks = new();
    private readonly Dictionary<string, Symbol> unresolvedSymbols = new(StringComparer.Ordinal);
    private readonly Dictionary<CacheHashKey, Type> errorTypes = new();
    private void initializeScalarOptions(CheckerProfile profile)
    {
        compilerOptions.Target = ScriptTarget.ESNext;
        compilerOptions.NoLib = Tristate.True;
        compilerOptions.Strict = Tristate.False;
        compilerOptions.StrictNullChecks = Tristate.True;
        noImplicitAny = noImplicitThis = strictFunctionTypes = profile == CheckerProfile.Callable;
        compilerOptions.NoImplicitAny = noImplicitAny ? Tristate.True : Tristate.False;
        compilerOptions.NoImplicitThis = noImplicitThis ? Tristate.True : Tristate.False;
        compilerOptions.StrictFunctionTypes = strictFunctionTypes ? Tristate.True : Tristate.False;
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
        if (location != null && (checkAndReportErrorForUsingTypeAsNamespace(location, name, meaning) || checkAndReportErrorForUsingNamespaceAsTypeOrValue(location, name, meaning) || checkAndReportErrorForUsingTypeAsValue(location, name, meaning) || checkAndReportErrorForUsingValueAsType(location, name, meaning)))
            return;
        // Missing-prefix/class-extends/export/JSDoc branches are outside admitted source syntax.
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
    private void onSuccessfullyResolvedSymbol(Node errorLocation, Symbol result, SymbolFlags meaning, Node lastLocation, Node associatedDeclarationForContainingInitializerOrBindingName, bool withinDeferredContext)
    {
        var name = result.Name;
        var isInExternalModule = (((lastLocation != null) && Ast.IsSourceFile(lastLocation)) && Ast.IsExternalOrCommonJSModule(lastLocation.AsSourceFile()));
        if (((errorLocation != null) && ((((meaning & SymbolFlags.BlockScopedVariable) != 0) || (((meaning & ((SymbolFlags.Class | SymbolFlags.Enum))) != 0) && ((meaning & SymbolFlags.Value) == SymbolFlags.Value))))))
        {
            var exportOrLocalSymbol = this.getExportSymbolOfValueSymbolIfExported(result);
            if (((exportOrLocalSymbol.Flags & (((SymbolFlags.BlockScopedVariable | SymbolFlags.Class) | SymbolFlags.Enum))) != 0))
            {
                this.checkResolvedBlockScopedVariable(exportOrLocalSymbol, errorLocation);
            }
        }

        if (((isInExternalModule && (((meaning & SymbolFlags.Value)) == SymbolFlags.Value)) && ((errorLocation.Flags & NodeFlags.JSDoc) == 0)))
        {
            throw unsupported("external module symbol");
        }

        if ((((associatedDeclarationForContainingInitializerOrBindingName != null) && !withinDeferredContext) && (((meaning & SymbolFlags.Value)) == SymbolFlags.Value)))
        {
            var candidate = this.getMergedSymbol(this.getLateBoundSymbol(result));
            var root = Ast.GetRootDeclaration(associatedDeclarationForContainingInitializerOrBindingName);
            if ((candidate == this.getSymbolOfDeclaration(associatedDeclarationForContainingInitializerOrBindingName)))
            {
                this.error(errorLocation, Diagnostics.Parameter_0_cannot_reference_itself, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(associatedDeclarationForContainingInitializerOrBindingName.Name()));
            }
            else
            {
                if (((((candidate.ValueDeclaration != null) && (candidate.ValueDeclaration.Pos() > associatedDeclarationForContainingInitializerOrBindingName.Pos())) && (root.Parent.Locals() != null)) && (this.getSymbol(root.Parent.Locals(), candidate.Name, meaning) == candidate)))
                {
                    this.error(errorLocation, Diagnostics.Parameter_0_cannot_reference_identifier_1_declared_after_it, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(associatedDeclarationForContainingInitializerOrBindingName.Name()), TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(errorLocation));
                }
            }
        }

        if ((((((errorLocation != null) && ((meaning & SymbolFlags.Value) != 0)) && ((result.Flags & SymbolFlags.Alias) != 0)) && ((result.Flags & SymbolFlags.Value) == 0)) && !Ast.IsValidTypeOnlyAliasUseSite(errorLocation)))
        {
            throw unsupported("type-only import alias");
        }

        if (((((this.compilerOptions.IsolatedModules == Tristate.True) && (result != null)) && isInExternalModule) && (((meaning & SymbolFlags.Value)) == SymbolFlags.Value)))
        {
            throw unsupported("isolated module import collision");
        }
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
        var declarationFile = Ast.GetSourceFileOfNode(declaration);
        var useFile = Ast.GetSourceFileOfNode(usage);
        var declContainer = Ast.GetEnclosingBlockScopeContainer(declaration);
        if ((declarationFile != useFile))
        {
            return true;
        }

        if (((((usage.Flags & NodeFlags.JSDoc) != 0) || IsInTypeQuery(usage)) || this.isInAmbientOrTypeNode(usage)))
        {
            return true;
        }

        if (((declaration.Pos() <= usage.Pos()) && !((((Ast.IsPropertyDeclaration(declaration) && isThisProperty(usage.Parent)) && (declaration.Initializer() == null)) && !isExclamationToken(declaration.PostfixToken())))))
        {
            switch (true)
            {
                case true when (declaration.Kind == Kind.BindingElement):
                {
                    throw unsupported("binding pattern declaration order");
                }

                case true when (declaration.Kind == Kind.VariableDeclaration):
                {
                    return !isImmediatelyUsedInInitializerOfBlockScopedVariable(declaration, usage, declContainer);
                }

                case true when Ast.IsClassLike(declaration):
                {
                    throw unsupported("class declaration order");
                }

                case true when Ast.IsPropertyDeclaration(declaration):
                {
                    throw unsupported("class property declaration order");
                }

                case true when Ast.IsParameterPropertyDeclaration(declaration, declaration.Parent):
                {
                    throw unsupported("class parameter property declaration order");
                }
            }

            return true;
        }

        if ((Ast.IsExportSpecifier(usage.Parent) || (Ast.IsExportAssignment(usage.Parent) && usage.Parent.AsExportAssignment().IsExportEquals)))
        {
            return true;
        }

        if ((Ast.IsExportAssignment(usage) && usage.AsExportAssignment().IsExportEquals))
        {
            return true;
        }

        if (this.isUsedInFunctionOrInstanceProperty(usage, declaration, declContainer))
        {
            if (((this.emitStandardClassFields && (Ast.GetContainingClass(declaration) != null)) && ((Ast.IsPropertyDeclaration(declaration) || Ast.IsParameterPropertyDeclaration(declaration, declaration.Parent)))))
            {
                throw unsupported("class field declaration order");
            }

            return true;
        }

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
        if ((parent == null))
        {
            return false;
        }

        {
            var n = initial;
            for (; (n != null); n = n.Parent)
            {
                if ((n == parent))
                {
                    return true;
                }

                if (((n == stopAt) || (Ast.IsFunctionLike(n) && (((Ast.GetImmediatelyInvokedFunctionExpression(n) == null) || (((Ast.GetFunctionFlags(n) & FunctionFlags.AsyncGenerator) != 0)))))))
                {
                    return false;
                }
            }
        }

        return false;
    }

    internal Symbol getExportSymbolOfValueSymbolIfExported(Symbol symbol)
    {
        if (symbol != null && (symbol.Flags & SymbolFlags.ExportValue) != 0 && symbol.ExportSymbol != null)
            symbol = symbol.ExportSymbol;
        return getMergedSymbol(symbol);
    }

    // getTypeOfSymbol (16879): primitive variable/property cache arm.
    internal Type getTypeOfSymbol(Symbol symbol)
    {
        if ((symbol.CheckFlags & CheckFlags.DeferredType) != 0)
            return getTypeOfSymbolWithDeferredType(symbol);
        if ((symbol.CheckFlags & CheckFlags.Instantiated) != 0)
            return getTypeOfInstantiatedSymbol(symbol);
        if ((symbol.CheckFlags & (CheckFlags.Mapped | CheckFlags.ReverseMapped)) != 0)
            throw unsupported("getTypeOfSymbol: transformed symbol");
        if ((symbol.Flags & SymbolFlags.Accessor) != 0)
            throw unsupported("getTypeOfSymbol: accessor");
        if ((symbol.Flags & (SymbolFlags.Variable | SymbolFlags.Property)) != 0)
            return getTypeOfVariableOrParameterOrProperty(symbol);
        if ((symbol.Flags & (SymbolFlags.Function | SymbolFlags.Method | SymbolFlags.Class | SymbolFlags.Enum | SymbolFlags.ValueModule)) != 0)
            return getTypeOfFuncClassEnumModule(symbol);
        if ((symbol.Flags & (SymbolFlags.EnumMember | SymbolFlags.Alias)) != 0)
            throw unsupported("getTypeOfSymbol: enum member/import alias");
        return errorType;
    }

    // checker.go:17291. The synthesized globalThis module is the selected value-module path.
    private Type getTypeOfFuncClassEnumModule(Symbol symbol)
    {
        var links = valueSymbolLinks.Get(symbol);
        if (links.resolvedType == null)
            links.resolvedType = getTypeOfFuncClassEnumModuleWorker(symbol);
        return links.resolvedType;
    }

    private Type getTypeOfFuncClassEnumModuleWorker(Symbol symbol)
    {
        if (symbol != globalThisSymbol && (symbol.Flags & (SymbolFlags.Function | SymbolFlags.Method)) == 0)
            throw unsupported("getTypeOfFuncClassEnumModuleWorker: source class/enum/module");
        var t = newObjectType(ObjectFlags.Anonymous, symbol);
        return strictNullChecks && (symbol.Flags & SymbolFlags.Optional) != 0 ? getOptionalType(t, true) : t;
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
        if (((symbol.Flags & SymbolFlags.Prototype) != 0))
        {
            throw unsupported("class prototype property");
        }

        if ((symbol == this.requireSymbol))
        {
            return this.anyType;
        }

        Debug.Assert((symbol.ValueDeclaration != null));
        var declaration = symbol.ValueDeclaration;
        if ((Ast.IsSourceFile(declaration) && Ast.IsJsonSourceFile(declaration.AsSourceFile())))
        {
            throw unsupported("JSON source variable type");
        }

        if (!this.pushTypeResolution(symbol, TypeSystemPropertyName.Type))
        {
            return this.reportCircularityError(symbol);
        }

        if (((symbol.Flags & SymbolFlags.ModuleExports) != 0))
        {
            throw unsupported("CommonJS exports variable type");
        }

        Type result = default;
        switch (declaration.Kind)
        {
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.VariableDeclaration:
            case Kind.BindingElement:
            {
                result = this.getWidenedTypeForVariableLikeDeclaration(declaration, !this.isParameterOfContextSensitiveSignature(symbol));
                break;
            }

            case Kind.PropertyAssignment:
            {
                result = this.checkPropertyAssignment(declaration, CheckMode.Normal);
                break;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                result = this.checkShorthandPropertyAssignment(declaration, true, CheckMode.Normal);
                break;
            }

            case Kind.MethodDeclaration:
            {
                result = this.checkObjectLiteralMethod(declaration, CheckMode.Normal);
                break;
            }

            case Kind.ExportAssignment:
            {
                throw unsupported("export assignment");
            }

            case Kind.BinaryExpression:
            case Kind.CallExpression:
            {
                result = this.getWidenedTypeForAssignmentDeclaration(symbol);
                break;
            }

            case Kind.JsxAttribute:
            {
                throw unsupported("JSX attribute");
            }

            case Kind.EnumMember:
            {
                throw unsupported("enum member");
            }

            default:
            {
                throw new InvalidOperationException(("Unhandled case in getTypeOfVariableOrParameterOrPropertyWorker: " + declaration.KindString()));
            }
        }

        if (!this.popTypeResolution())
        {
            return this.reportCircularityError(symbol);
        }

        return result;
    }

    // checker.go:19165. Symbols and semantic types share the original tagged stack.
    private bool pushTypeResolution(object target, TypeSystemPropertyName propertyName)
    {
        int cycleStart = findResolutionCycleStartIndex(target, propertyName);
        if (cycleStart >= 0)
        {
            for (int i = cycleStart; i < typeResolutions.Count; i++)
                typeResolutions[i].result = false;
            return false;
        }

        typeResolutions.Add(new() { target = target, propertyName = propertyName, result = true });
        return true;
    }

    private bool popTypeResolution()
    {
        int last = typeResolutions.Count - 1;
        bool result = typeResolutions[last].result;
        typeResolutions.RemoveAt(last);
        return result;
    }

    private int findResolutionCycleStartIndex(object target, TypeSystemPropertyName propertyName)
    {
        for (int i = typeResolutions.Count - 1; i >= resolutionStart; i--)
        {
            var r = typeResolutions[i];
            if (typeResolutionHasProperty(r))
                return -1;
            if (r.target == target && r.propertyName == propertyName)
                return i;
        }

        return -1;
    }

    private bool typeResolutionHasProperty(TypeResolution r)
    {
        return r.propertyName switch
        {
            TypeSystemPropertyName.InitializerIsUndefined => (nodeLinks.Get((Node)r.target).flags & NodeCheckFlags.InitializerIsUndefinedComputed) != 0,
            TypeSystemPropertyName.ResolvedReturnType => ((Signature)r.target).resolvedReturnType != null,
            TypeSystemPropertyName.Type => valueSymbolLinks.Get((Symbol)r.target).resolvedType != null,
            TypeSystemPropertyName.DeclaredType => typeAliasLinks.Get((Symbol)r.target).declaredType != null,
            TypeSystemPropertyName.ResolvedTypeArguments => ((Type)r.target).AsTypeReference().resolvedTypeArguments != null,
            TypeSystemPropertyName.ResolvedBaseTypes => ((Type)r.target).AsInterfaceType().baseTypesResolved,
            TypeSystemPropertyName.ResolvedBaseConstructorType => ((Type)r.target).AsInterfaceType().resolvedBaseConstructorType != null,
            TypeSystemPropertyName.ResolvedBaseConstraint => ((Type)r.target).AsConstrainedType().resolvedBaseConstraint != null,
            TypeSystemPropertyName.WriteType => valueSymbolLinks.Get((Symbol)r.target).writeType != null,
            _ => throw unsupported("typeResolutionHasProperty: callable/assignment/module resolution")};
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
    internal Type getTypeForVariableLikeDeclaration(Node declaration, bool includeOptionality, CheckMode checkMode = CheckMode.Normal)
    {
        if (Ast.IsVariableDeclaration(declaration))
        {
            if (declaration.Parent.Parent.Kind is Kind.ForInStatement or Kind.ForOfStatement)
                throw unsupported("getTypeForVariableLikeDeclaration: loop");
        }
        else if (Ast.IsBindingElement(declaration))
            throw unsupported("getTypeForVariableLikeDeclaration: binding element");
        bool isProperty = Ast.IsPropertySignatureDeclaration(declaration), isOptional = includeOptionality && isOptionalDeclaration(declaration);
        var declaredType = tryGetTypeFromTypeNode(declaration);
        if (Ast.IsCatchClauseVariableDeclarationOrBindingElement(declaration))
            throw unsupported("getTypeForVariableLikeDeclaration: catch");
        if (declaredType != null)
            return addOptionalityEx(declaredType, isProperty, isOptional);
        if (noImplicitAny && Ast.IsVariableDeclaration(declaration) && !Ast.IsBindingPattern(declaration.Name()) && (Ast.GetCombinedModifierFlags(declaration) & ModifierFlags.Export) == 0 && (declaration.Flags & NodeFlags.Ambient) == 0)
        {
            var initializer = declaration.Initializer();
            if ((getCombinedNodeFlagsCached(declaration) & NodeFlags.Constant) == 0 && (initializer == null || isNullOrUndefined(initializer)))
                return autoType;
            if (initializer?.Kind == Kind.ArrayLiteralExpression)
                throw unsupported("getTypeForVariableLikeDeclaration: auto array");
        }

        if (Ast.IsParameterDeclaration(declaration))
        {
            if (declaration.Symbol() == null)
                return null;
            var fn = declaration.Parent;
            if (Ast.IsSetAccessorDeclaration(fn))
                throw unsupported("getTypeForVariableLikeDeclaration: setter");
            var fullSignatureType = getParameterTypeOfFullSignature(fn, declaration);
            if (fullSignatureType != null)
                return fullSignatureType;
            var t = declaration.Symbol().Name == Ast.InternalSymbolNameThis ? getContextualThisParameterType(fn) : getContextuallyTypedParameterType(declaration);
            if (t != null)
                return addOptionalityEx(t, false, isOptional);
        }

        if (declaration.Initializer() != null)
            return addOptionalityEx(widenTypeInferredFromInitializer(declaration, checkDeclarationInitializer(declaration, checkMode, null)), isProperty, isOptional);
        if (Ast.IsPropertyDeclaration(declaration) || Ast.IsJsxAttribute(declaration) || Ast.IsBindingPattern(declaration.Name()))
            throw unsupported("getTypeForVariableLikeDeclaration: class/JSX/binding");
        return null;
    }

    private Type tryGetTypeFromTypeNode(Node node) => node.Type() != null ? getTypeFromTypeNode(node.Type()) : null;
    private Type checkDeclarationInitializer(Node declaration, CheckMode checkMode = CheckMode.Normal, Type contextualType = null)
    {
        var initializer = declaration.Initializer();
        var t = getQuickTypeOfExpression(initializer);
        if (t == null)
            t = contextualType != null ? checkExpressionWithContextualType(initializer, contextualType, null, checkMode) : checkExpressionCachedEx(initializer, checkMode);
        if (Ast.IsParameterDeclaration(Ast.GetRootDeclaration(declaration)) && Ast.IsBindingPattern(declaration.Name()))
            throw unsupported("checkDeclarationInitializer: binding pattern");
        return t;
    }

    private Type getQuickTypeOfExpression(Node node)
    {
        var expr = Ast.SkipParentheses(node);
        switch (true)
        {
            case true when Ast.IsAwaitExpression(expr):
            {
                throw unsupported("getQuickTypeOfExpression: source await");
            }

            case true when ((((Ast.IsCallExpression(expr) && (expr.Expression().Kind != Kind.SuperKeyword)) && !Ast.IsRequireCall(expr, true)) && !this.isSymbolOrSymbolForCall(expr)) && !Ast.IsImportCall(expr)):
            {
                if (isCallChain(expr))
                {
                    throw unsupported("getQuickTypeOfExpression: optional call");
                }

                return this.getReturnTypeOfSingleNonGenericSignature(this.checkNonNullExpression(expr.Expression()), SignatureKind.Call);
            }

            case true when Ast.IsNewExpression(expr):
            {
                throw unsupported("getQuickTypeOfExpression: source new");
            }

            case true when (Ast.IsAssertionExpression(expr) && !Ast.IsConstTypeReference(expr.Type())):
            {
                throw unsupported("getQuickTypeOfExpression: source assertion");
            }

            case true when (Ast.IsLiteralExpression(node) || Ast.IsBooleanLiteral(node)):
            {
                return this.checkExpression(node);
            }
        }

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
            if ((t.flags & TypeFlags.ESSymbol) != 0 && isGlobalSymbolConstructor(declaration.Parent))
                t = getESSymbolLikeTypeForNode(declaration);
            if (reportErrors)
                reportErrorsFromWidening(declaration, t, WideningKind.Normal);
            if ((t.flags & TypeFlags.UniqueESSymbol) != 0 && (Ast.IsBindingElement(declaration) || declaration.Type() == null) && t.symbol != getSymbolOfDeclaration(declaration))
                t = esSymbolType;
            return getWidenedType(t);
        }

        if (Ast.IsParameterDeclaration(declaration) && isRestParameter(declaration))
            throw unsupported("widenTypeForVariableLikeDeclaration: rest");
        t = anyType;
        if (reportErrors && !declarationBelongsToPrivateAmbientMember(declaration))
            reportImplicitAny(declaration, t, WideningKind.Normal);
        return t;
    }

    internal Type getWidenedType(Type t) => getWidenedTypeWithContext(t, null);
    private Type checkExpressionCached(Node node) => checkExpressionCachedEx(node, CheckMode.Normal);
    private Type checkExpressionCachedEx(Node node, CheckMode checkMode)
    {
        if (checkMode != CheckMode.Normal)
            return checkExpressionEx(node, checkMode);
        var links = typeNodeLinks.Get(node);
        // No loops are admitted: flowLoopStack/flowTypeCache are always nil.
        if (links.resolvedType == null)
        {
            var savedFlowTypeCache = flowTypeCache;
            flowTypeCache = null;
            links.resolvedType = checkExpressionEx(node, checkMode);
            flowTypeCache = savedFlowTypeCache;
        }

        return links.resolvedType;
    }

    private Type checkExpression(Node node) => checkExpressionEx(node, CheckMode.Normal);
    private Type checkExpressionEx(Node node, CheckMode checkMode)
    {
        if ((checkMode & ~(CheckMode.Contextual | CheckMode.Inferential | CheckMode.SkipContextSensitive | CheckMode.SkipGenericFunctions | CheckMode.TypeOnly)) != 0)
            throw unsupported("checkExpressionEx: inference/signature/tuple check mode");
        var saved = currentNode;
        currentNode = node;
        instantiationCount = 0;
        var t = checkExpressionWorker(node, checkMode);
        currentNode = saved;
        return t;
    }

    private Type checkExpressionWorker(Node node, CheckMode checkMode)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                return checkIdentifier(node, checkMode);
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
                return checkPropertyAccessExpression(node, checkMode);
            case Kind.CallExpression:
                return checkCallExpression(node, checkMode);
            case Kind.BinaryExpression:
                return checkBinaryExpression(node, checkMode);
            case Kind.PrefixUnaryExpression:
                return checkPrefixUnaryExpression(node);
            case Kind.ConditionalExpression:
                return checkConditionalExpression(node, checkMode);
            case Kind.TypeOfExpression:
                return checkTypeOfExpression(node);
            case Kind.VoidExpression:
                return checkVoidExpression(node);
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
                return checkFunctionExpressionOrObjectLiteralMethod(node, checkMode);
            case Kind.ThisKeyword:
                return checkThisExpression(node);
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
            case Kind.ObjectKeyword:
                return nonPrimitiveType;
            case Kind.TypeReference:
            case Kind.ExpressionWithTypeArguments:
                return getTypeFromTypeReference(node);
            case Kind.UnionType:
                return getTypeFromUnionTypeNode(node);
            case Kind.IntersectionType:
                return getTypeFromIntersectionTypeNode(node);
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
            case Kind.FunctionType:
                return getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode(node);
            case Kind.LiteralType:
                return getTypeFromLiteralTypeNode(node);
            case Kind.Identifier:
            case Kind.QualifiedName:
                return errorType; // Original worker final arm for owned name/query locations.
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
        if (Ast.IsTypeDeclaration(node))
            return getDeclaredTypeOfSymbol(getSymbolOfDeclaration(node));
        if (Ast.IsTypeDeclarationName(node))
        {
            var typeSymbol = getSymbolAtLocation(node, false);
            return typeSymbol != null ? getDeclaredTypeOfSymbol(typeSymbol) : errorType;
        }

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
        while (Ast.IsRightSideOfQualifiedNameOrPropertyAccess(name))
            name = name.Parent;
        if (isInNameOfExpressionWithTypeArgumentsOrHeritageTypeReference(name))
        {
            var meaning = name.Parent.Kind is Kind.ExpressionWithTypeArguments or Kind.TypeReference ? Ast.IsPartOfTypeNode(name) ? SymbolFlags.Type : SymbolFlags.Value : SymbolFlags.Namespace;
            meaning |= SymbolFlags.Alias;
            if (Ast.IsEntityNameExpression(name))
            {
                var entityNameSymbol = resolveEntityName(name, meaning, true, false, null);
                if (entityNameSymbol != null)
                    return entityNameSymbol;
            }
        }

        if (name.Kind == Kind.PropertyAccessExpression)
        {
            var links = symbolNodeLinks.Get(name);
            if (links.resolvedSymbol != null)
                return links.resolvedSymbol;
            checkPropertyAccessExpression(name);
            if (links.resolvedSymbol == null && !Ast.IsPrivateIdentifier(name.Name()))
                links.resolvedSymbol = getApplicableIndexSymbol(checkExpressionCached(name.Expression()), getLiteralTypeFromPropertyName(name.Name()));
            return links.resolvedSymbol;
        }

        if (Ast.IsExpressionNode(name))
        {
            if (Ast.NodeIsMissing(name))
                return null;
            return resolveEntityName(name, SymbolFlags.Value, true, true, null);
        }

        if (Ast.IsEntityName(name) && isTypeReferenceIdentifier(name))
        {
            var meaning = name.Parent.Kind == Kind.TypeReference ? SymbolFlags.Type : SymbolFlags.Namespace;
            var symbol = resolveEntityName(name, meaning, true, true, null);
            if (symbol != null && symbol != unknownSymbol)
                return symbol;
            if (Ast.IsNameOfHeritageClauseTypeReference(name))
                return null;
            return getUnresolvedSymbolForEntityName(name);
        }

        return null;
    }

    internal Symbol resolveEntityName(Node name, SymbolFlags meaning, bool ignoreErrors, bool dontResolveAlias, Node location)
    {
        if (Ast.NodeIsMissing(name))
            return null;
        Symbol symbol;
        switch (name.Kind)
        {
            case Kind.Identifier:
                Message message = null;
                if (!ignoreErrors)
                    message = meaning == SymbolFlags.Namespace || Ast.NodeIsSynthesized(name) ? Diagnostics.Cannot_find_namespace_0 : getCannotFindNameDiagnosticForName(Ast.GetFirstIdentifier(name));
                var resolveLocation = location ?? name;
                if (meaning == SymbolFlags.Namespace)
                {
                    symbol = getMergedSymbol(nameResolver.Resolve(resolveLocation, name.Text(), meaning, null, true, false));
                    if (symbol == null)
                    {
                        var alias = getMergedSymbol(nameResolver.Resolve(resolveLocation, name.Text(), SymbolFlags.Alias, null, true, false));
                        if (alias != null && alias.Name == Ast.InternalSymbolNameExportEquals)
                            symbol = alias.Parent;
                    }

                    if (symbol == null && message != null)
                        nameResolver.Resolve(resolveLocation, name.Text(), meaning, message, true, false);
                }
                else
                    symbol = getMergedSymbol(nameResolver.Resolve(resolveLocation, name.Text(), meaning, message, true, false));
                break;
            case Kind.QualifiedName:
                var qualified = name.AsQualifiedName();
                symbol = resolveQualifiedName(name, qualified.Left, qualified.Right, meaning, ignoreErrors, location);
                break;
            case Kind.PropertyAccessExpression:
                var access = name.AsPropertyAccessExpression();
                symbol = resolveQualifiedName(name, access.Expression, access.Name(), meaning, ignoreErrors, location);
                break;
            default:
                throw new InvalidOperationException("Unknown entity name kind");
        }

        if (symbol != null && symbol != unknownSymbol)
        {
            if ((symbol.Flags & SymbolFlags.Alias) != 0)
                throw unsupported("resolveEntityName: source module alias");
        }

        return symbol;
    }

    private Symbol resolveQualifiedName(Node name, Node left, Node right, SymbolFlags meaning, bool ignoreErrors, Node location)
    {
        var ns = resolveEntityName(left, SymbolFlags.Namespace, ignoreErrors, false, location);
        if (ns == null || Ast.NodeIsMissing(right))
            return null;
        if (ns == unknownSymbol)
            return ns;
        if (ns.ValueDeclaration != null && Ast.IsInJSFile(ns.ValueDeclaration))
            throw unsupported("resolveQualifiedName: CommonJS module");
        var symbol = getMergedSymbol(getSymbol(getExportsOfSymbol(ns), right.Text(), meaning));
        if (symbol == null && (ns.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("resolveQualifiedName: module alias");
        if (symbol == null && !ignoreErrors)
        {
            var namespaceName = getFullyQualifiedName(ns, null);
            var declarationName = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(right);
            var suggestion = getSuggestedSymbolForNonexistentModule(right, ns);
            if (suggestion != null)
            {
                error(right, Diagnostics.X_0_has_no_exported_member_named_1_Did_you_mean_2, namespaceName, declarationName, symbolToString(suggestion));
                return null;
            }

            var containingQualifiedName = Ast.IsQualifiedName(name) ? getContainingQualifiedNameNode(name) : null;
            bool canSuggestTypeof = globalObjectType != null && (meaning & SymbolFlags.Type) != 0 && containingQualifiedName != null && !Ast.IsTypeOfExpression(containingQualifiedName.Parent) && tryGetQualifiedNameAsValue(containingQualifiedName) != null;
            if (canSuggestTypeof)
            {
                error(containingQualifiedName, Diagnostics.X_0_refers_to_a_value_but_is_being_used_as_a_type_here_Did_you_mean_typeof_0, entityNameToString(containingQualifiedName));
                return null;
            }

            if ((meaning & SymbolFlags.Namespace) != 0 && Ast.IsQualifiedName(name.Parent))
            {
                var exportedTypeSymbol = getMergedSymbol(getSymbol(getExportsOfSymbol(ns), right.Text(), SymbolFlags.Type));
                if (exportedTypeSymbol != null)
                {
                    var qualified = name.Parent.AsQualifiedName();
                    error(qualified.Right, Diagnostics.Cannot_access_0_1_because_0_is_a_type_but_not_a_namespace_Did_you_mean_to_retrieve_the_type_of_the_property_1_in_0_with_0_1, symbolToString(exportedTypeSymbol), qualified.Right.Text());
                    return null;
                }
            }

            error(right, Diagnostics.Namespace_0_has_no_exported_member_1, namespaceName, declarationName);
        }

        return symbol;
    }

    private Symbol tryGetQualifiedNameAsValue(Node node)
    {
        var id = Ast.GetFirstIdentifier(node);
        var symbol = nameResolver.Resolve(id, id.Text(), SymbolFlags.Value, null, true, false);
        if (symbol == null)
            return null;
        var n = id;
        while (Ast.IsQualifiedName(n.Parent))
        {
            symbol = getPropertyOfType(getTypeOfSymbol(symbol), n.Parent.AsQualifiedName().Right.Text());
            if (symbol == null)
                return null;
            n = n.Parent;
        }

        return symbol;
    }

    private Symbol getSuggestedSymbolForNonexistentModule(Node name, Symbol targetModule) => getSpellingSuggestionForName(name.Text(), getExportsOfModule(targetModule).Values, SymbolFlags.ModuleMember);
    private string getFullyQualifiedName(Symbol symbol, Node containingLocation) => symbol.Parent != null ? getFullyQualifiedName(symbol.Parent, containingLocation) + "." + symbolToString(symbol) : symbolToStringWithoutSymbolChain(symbol, containingLocation);
}

public sealed partial class Checker
{
    // checkIdentifier (11278): ordinary scalar variable reads, exact initialization/flow path.
    private Type checkIdentifier(Node node, CheckMode checkMode = CheckMode.Normal)
    {
        if (Ast.IsThisInTypeQuery(node))
        {
            return this.checkThisExpression(node);
        }

        var symbol = this.getResolvedSymbol(node);
        if ((symbol == this.unknownSymbol))
        {
            return this.errorType;
        }

        if ((symbol == this.argumentsSymbol))
        {
            if (this.isInPropertyInitializerOrClassStaticBlock(node, true))
            {
                this.error(node, Diagnostics.X_arguments_cannot_be_referenced_in_property_initializers_or_class_static_initialization_blocks);
                return this.errorType;
            }

            return this.getTypeOfSymbol(symbol);
        }

        if (shouldMarkIdentifierAliasReferenced(node) && ((node.Flags & NodeFlags.Ambient) == 0))
        {
            this.markIdentifierAliasReferenced(node);
        }

        var localOrExportSymbol = this.getExportSymbolOfValueSymbolIfExported(symbol);
        var targetSymbol = this.resolveAliasWithDeprecationCheck(localOrExportSymbol, node);
        if ((((GoRuntime.Len(targetSymbol.Declarations) != 0) && this.isDeprecatedSymbol(targetSymbol)) && this.isUncalledFunctionReference(node, targetSymbol)))
        {
            this.addDeprecatedSuggestion(node, targetSymbol.Declarations, node.Text());
        }

        var declaration = localOrExportSymbol.ValueDeclaration;
        var immediateDeclaration = declaration;
        if (declaration != null && Ast.IsBindingElement(declaration))
            throw unsupported("checkIdentifier: binding element");
        var t = this.getNarrowedTypeOfSymbol(localOrExportSymbol, node);
        if (localOrExportSymbol == globalThisSymbol)
            throw unsupported("checkIdentifier: object value");
        var assignmentKind = getAssignmentTargetKind(node);
        if ((assignmentKind != AssignmentKind.None))
        {
            if ((((localOrExportSymbol.Flags & SymbolFlags.Variable) == 0) && !((Ast.IsInJSFile(node) && ((localOrExportSymbol.Flags & SymbolFlags.ValueModule) != 0)))))
            {
                Message assignmentError = default;
                switch (true)
                {
                    case true when ((localOrExportSymbol.Flags & SymbolFlags.Enum) != 0):
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_an_enum;
                        break;
                    }

                    case true when ((localOrExportSymbol.Flags & SymbolFlags.Class) != 0):
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_a_class;
                        break;
                    }

                    case true when ((localOrExportSymbol.Flags & SymbolFlags.Module) != 0):
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_a_namespace;
                        break;
                    }

                    case true when ((localOrExportSymbol.Flags & SymbolFlags.Function) != 0):
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_a_function;
                        break;
                    }

                    case true when ((localOrExportSymbol.Flags & SymbolFlags.Alias) != 0):
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_an_import;
                        break;
                    }

                    default:
                    {
                        assignmentError = Diagnostics.Cannot_assign_to_0_because_it_is_not_a_variable;
                        break;
                    }
                }

                this.error(node, assignmentError, this.symbolToString(symbol));
                return this.errorType;
            }

            if (this.isReadonlySymbol(localOrExportSymbol))
            {
                if (((localOrExportSymbol.Flags & SymbolFlags.Variable) != 0))
                {
                    this.error(node, Diagnostics.Cannot_assign_to_0_because_it_is_a_constant, this.symbolToString(symbol));
                }
                else
                {
                    this.error(node, Diagnostics.Cannot_assign_to_0_because_it_is_a_read_only_property, this.symbolToString(symbol));
                }

                return this.errorType;
            }
        }

        var isAlias = ((localOrExportSymbol.Flags & SymbolFlags.Alias) != 0);
        if (((localOrExportSymbol.Flags & SymbolFlags.Variable) != 0))
        {
            if ((assignmentKind == AssignmentKind.Definite))
            {
                if (isInCompoundLikeAssignment(node))
                {
                    return this.getBaseTypeOfLiteralType(t);
                }

                return t;
            }
        }
        else
        {
            if (isAlias)
            {
                declaration = this.getDeclarationOfAliasSymbol(symbol);
            }
            else
            {
                return t;
            }
        }

        if ((declaration == null))
        {
            return t;
        }

        t = this.getNarrowableTypeForReference(t, node, checkMode);
        var isParameter = (Ast.GetRootDeclaration(declaration).Kind == Kind.Parameter);
        var declarationContainer = this.getControlFlowContainer(declaration);
        var flowContainer = this.getControlFlowContainer(node);
        var isOuterVariable = (flowContainer != declarationContainer);
        var isSpreadDestructuringAssignmentTarget = ((((node.Parent != null) && (node.Parent.Parent != null)) && Ast.IsSpreadAssignment(node.Parent)) && throwUnsupportedDestructuring(node.Parent.Parent));
        var isModuleExports = ((symbol.Flags & SymbolFlags.ModuleExports) != 0);
        var typeIsAutomatic = ((t == this.autoType) || (t == this.autoType));
        var isAutomaticTypeInNonNull = (typeIsAutomatic && (node.Parent.Kind == Kind.NonNullExpression));
        while ((((flowContainer != declarationContainer) && ((Ast.IsFunctionExpressionOrArrowFunction(flowContainer) || Ast.IsObjectLiteralOrClassExpressionMethodOrAccessor(flowContainer)))) && (((this.isConstantVariable(localOrExportSymbol) && (t != this.autoType)) || (this.isParameterOrMutableLocalVariable(localOrExportSymbol) && this.isPastLastAssignment(localOrExportSymbol, node))))))
        {
            flowContainer = this.getControlFlowContainer(flowContainer);
        }

        var isNeverInitialized = (((((((immediateDeclaration != null) && Ast.IsVariableDeclaration(immediateDeclaration)) && !Ast.IsForInOrOfStatement(immediateDeclaration.Parent.Parent)) && (immediateDeclaration.Initializer() == null)) && (immediateDeclaration.AsVariableDeclaration().ExclamationToken == null)) && this.isMutableLocalVariableDeclaration(immediateDeclaration)) && !this.isSymbolAssignedDefinitely(symbol));
        var assumeInitialized = (((((((((isParameter || isAlias) || ((isOuterVariable && !isNeverInitialized))) || isSpreadDestructuringAssignmentTarget) || isModuleExports) || this.isSameScopedBindingElement(node, declaration)) || (((t != this.autoType) && (t != this.autoType)) && (((((!this.strictNullChecks || ((t.flags & ((TypeFlags.AnyOrUnknown | TypeFlags.Void))) != 0)) || IsInTypeQuery(node)) || this.isInAmbientOrTypeNode(node)) || (node.Parent.Kind == Kind.ExportSpecifier))))) || Ast.IsNonNullExpression(node.Parent)) || (Ast.IsVariableDeclaration(declaration) && (declaration.AsVariableDeclaration().ExclamationToken != null))) || ((declaration.Flags & NodeFlags.Ambient) != 0));
        Type initialType = default;
        switch (true)
        {
            case true when isAutomaticTypeInNonNull:
            {
                initialType = this.undefinedType;
                break;
            }

            case true when (assumeInitialized && isParameter):
            {
                initialType = this.removeOptionalityFromDeclaredType(t, declaration);
                break;
            }

            case true when assumeInitialized:
            {
                initialType = t;
                break;
            }

            case true when typeIsAutomatic:
            {
                initialType = this.undefinedType;
                break;
            }

            default:
            {
                initialType = this.getOptionalType(t, false);
                break;
            }
        }

        Type flowType = default;
        if (isAutomaticTypeInNonNull)
        {
            flowType = this.GetNonNullableType(this.getFlowTypeOfReferenceEx(node, t, initialType, flowContainer, null));
        }
        else
        {
            flowType = this.getFlowTypeOfReferenceEx(node, t, initialType, flowContainer, null);
        }

        if ((!this.isEvolvingArrayOperationTarget(node) && (((t == this.autoType) || (t == this.autoType)))))
        {
            if (((flowType == this.autoType) || (flowType == this.autoType)))
            {
                if (this.noImplicitAny)
                {
                    this.error(Ast.GetNameOfDeclaration(declaration), Diagnostics.Variable_0_implicitly_has_type_1_in_some_locations_where_its_type_cannot_be_determined, this.symbolToString(symbol), this.typeToStringByteString(flowType));
                    this.error(node, Diagnostics.Variable_0_implicitly_has_an_1_type, this.symbolToString(symbol), this.typeToStringByteString(flowType));
                }

                return this.convertAutoToAny(flowType);
            }
        }
        else
        {
            if (((!assumeInitialized && !this.containsUndefinedType(t)) && this.containsUndefinedType(flowType)))
            {
                this.error(node, Diagnostics.Variable_0_is_used_before_being_assigned, this.symbolToString(symbol));
                return t;
            }
        }

        if ((assignmentKind != AssignmentKind.None))
        {
            return this.getBaseTypeOfLiteralType(flowType);
        }

        return flowType;
    }

    private Type getNarrowedTypeOfSymbol(Symbol symbol, Node location)
    {
        var t = this.getTypeOfSymbol(symbol);
        var declaration = symbol.ValueDeclaration;
        if ((declaration != null))
        {
            switch (true)
            {
                case true when (((Ast.IsBindingElement(declaration) && (declaration.Initializer() == null)) && !hasDotDotDotToken(declaration)) && (GoRuntime.Len(declaration.Parent.Elements()) >= 2)):
                {
                    throw unsupported("getNarrowedTypeOfSymbol: destructuring");
                }

                case true when (((Ast.IsParameterDeclaration(declaration) && (declaration.Type() == null)) && (declaration.Initializer() == null)) && !hasDotDotDotToken(declaration)):
                {
                    var fn = declaration.Parent;
                    if (((GoRuntime.Len(fn.Parameters()) >= 2) && this.isContextSensitiveFunctionOrObjectLiteralMethod(fn)))
                    {
                        var contextualSignature = this.getContextualSignature(fn);
                        if ((((contextualSignature != null) && (GoRuntime.Len(contextualSignature.parameters) == 1)) && signatureHasRestParameter(contextualSignature)))
                        {
                            throw unsupported("getNarrowedTypeOfSymbol: rest-correlated tuple");
                        }
                    }

                    break;
                }
            }
        }

        return t;
    }

    private Type getNarrowableTypeForReference(Type t, Node reference, CheckMode checkMode = CheckMode.Normal)
    {
        if ((t.flags & TypeFlags.Substitution) != 0)
            throw unsupported("getNarrowableTypeForReference: source NoInfer substitution");
        bool substituteConstraints = (checkMode & CheckMode.Inferential) == 0 && (t.Distributed()?.Any(isGenericTypeWithUnionConstraint) ?? false) && (isConstraintPosition(t, reference) || hasContextualTypeWithNoGenericTypes(reference, checkMode));
        return substituteConstraints ? mapType(t, getBaseConstraintOrType) : t;
    }

    private bool isConstraintPosition(Type t, Node node)
    {
        var parent = node.Parent;
        if (Ast.IsElementAccessExpression(parent))
            throw unsupported("isConstraintPosition: source element access");
        return Ast.IsPropertyAccessExpression(parent) || Ast.IsQualifiedName(parent) || (Ast.IsCallExpression(parent) || Ast.IsNewExpression(parent)) && parent.Expression() == node;
    }

    private bool isGenericTypeWithUnionConstraint(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.AsIntersectionType().types.Any(isGenericTypeWithUnionConstraint);
        return (t.flags & TypeFlags.Instantiable) != 0 && (getBaseConstraintOrType(t).flags & (TypeFlags.Nullable | TypeFlags.Union)) != 0;
    }

    private bool isGenericTypeWithoutNullableConstraint(Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0)
            return t.AsIntersectionType().types.Any(isGenericTypeWithoutNullableConstraint);
        return (t.flags & TypeFlags.Instantiable) != 0 && !maybeTypeOfKind(getBaseConstraintOrType(t), TypeFlags.Nullable);
    }

    private bool hasContextualTypeWithNoGenericTypes(Node node, CheckMode checkMode)
    {
        if ((checkMode & CheckMode.RestBindingElement) != 0)
            throw unsupported("hasContextualTypeWithNoGenericTypes: source rest binding");
        if (Ast.IsIdentifier(node) || Ast.IsPropertyAccessExpression(node) || Ast.IsElementAccessExpression(node))
        {
            if (Ast.IsJsxOpeningElement(node.Parent) || Ast.IsJsxSelfClosingElement(node.Parent))
                throw unsupported("hasContextualTypeWithNoGenericTypes: source JSX");
            var contextualType = getContextualType(node);
            if (contextualType != null)
                return !isGenericType(contextualType);
        }

        return false;
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
        if (compilerOptions.GetEmitScriptTarget() != ScriptTarget.ESNext || compilerOptions.GetEmitModuleKind() < ModuleKind.ES2015 || sourceFile.ExternalModuleIndicator != null || sourceFile.CommonJSModuleIndicator != null)
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
    // checker.go:32639.
    private Symbol getApplicableIndexSymbol(Type t, Type keyType)
    {
        var info = getApplicableIndexInfo(t, keyType);
        if (info == null || info == anyBaseTypeIndexInfo)
            return null;
        if (info.indexSymbol == null)
        {
            List<Node> declarations = null;
            if (info.declaration != null)
                declarations = [info.declaration];
            else
                foreach (var candidate in getIndexInfosOfType(t) ?? [])
                    if (candidate.declaration != null && isApplicableIndexType(keyType, candidate.keyType))
                        declarations = GoRuntime.Append(declarations, candidate.declaration);
            if ((declarations?.Count ?? 0) != 0)
            {
                var symbol = newSymbol(SymbolFlags.Property, Ast.InternalSymbolNameIndex);
                symbol.CheckFlags |= CheckFlags.IndexSymbol;
                symbol.Declarations = declarations;
                symbol.ValueDeclaration = declarations[0];
                symbol.Parent = t.symbol;
                valueSymbolLinks.Get(symbol).resolvedType = info.valueType;
                info.indexSymbol = symbol;
            }
        }

        return info.indexSymbol;
    }

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
        TypeData data;
        if ((objectFlags & ObjectFlags.ClassOrInterface) != 0)
            data = new InterfaceType();
        else if ((objectFlags & ObjectFlags.Reference) != 0)
            data = new TypeReference();
        else if ((objectFlags & ObjectFlags.Anonymous) != 0)
            data = new ObjectType();
        else
            throw unsupported("newObjectType: tuple/mapped/reverse/evolving/callable payload");
        var t = newType(TypeFlags.Object, objectFlags, data);
        t.symbol = symbol;
        return t;
    }

    private void setStructuredTypeMembers(Type t, SymbolTable members, List<Signature> callSignatures = null, List<Signature> constructSignatures = null, List<IndexInfo> indexInfos = null)
    {
        t.objectFlags |= ObjectFlags.MembersResolved;
        var data = t.AsStructuredType();
        data.members = members;
        data.properties = getNamedMembers(members, t.symbol);
        data.signatures = GoRuntime.Append(callSignatures, constructSignatures ?? []);
        data.callSignatureCount = callSignatures?.Count ?? 0;
        data.indexInfos = indexInfos;
    }

    private List<Symbol> getNamedMembers(SymbolTable members, Symbol container)
    {
        if ((members?.Count ?? 0) == 0)
            return null;
        var result = new List<Symbol>(members.Count);
        int containedCount = 0;
        if (container != null && (container.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
        {
            foreach (var(id, symbol)in members)
                if (isNamedMember(symbol, id) && isDeclarationContainedBy(symbol, container))
                    result.Add(symbol);
            containedCount = result.Count;
        }

        foreach (var(id, symbol)in members)
            if (isNamedMember(symbol, id) && (container == null || (container.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) == 0 || !isDeclarationContainedBy(symbol, container)))
                result.Add(symbol);
        var contained = result.GetRange(0, containedCount);
        var inherited = result.GetRange(containedCount, result.Count - containedCount);
        sortSymbols(contained);
        sortSymbols(inherited);
        contained.AddRange(inherited);
        return contained;
    }

    private static bool isDeclarationContainedBy(Symbol symbol, Symbol container)
    {
        if (symbol.ValueDeclaration != null)
            foreach (var d in container.Declarations ?? [])
                if (symbol.ValueDeclaration.Pos() >= d.Pos() && symbol.ValueDeclaration.End() <= d.End())
                    return true;
        return false;
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
    internal Type filterType(Type t, Func<Type, bool> predicate)
    {
        if ((t.flags & TypeFlags.Union) != 0)
        {
            var types = t.Types();
            var filtered = types.Where(predicate).ToList();
            if (filtered.Count == types.Count)
                return t;
            var origin = t.AsUnionType().origin;
            Type newOrigin = null;
            if (origin != null && (origin.flags & TypeFlags.Union) != 0)
            {
                var originTypes = origin.Types();
                var originFiltered = originTypes.Where(u => (u.flags & TypeFlags.Union) != 0 || predicate(u)).ToList();
                if (originTypes.Count - originFiltered.Count == types.Count - filtered.Count)
                {
                    if (originFiltered.Count == 1)
                        return originFiltered[0];
                    newOrigin = newUnionType(ObjectFlags.None, originFiltered);
                }
            }

            return getUnionTypeFromSortedList(filtered, t.objectFlags & (ObjectFlags.PrimitiveUnion | ObjectFlags.ContainsIntersections), null, newOrigin);
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
            else if (isGenericTupleType(t))
                throw unsupported("getNormalizedType: source generic tuple");
            else if ((t.objectFlags & ObjectFlags.Reference) != 0)
                n = t.AsTypeReference().node != null ? createTypeReference(t.Target(), getTypeArguments(t)) : getSingleBaseForNonAugmentingSubtype(t) ?? t;
            else if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
                n = getNormalizedUnionOrIntersectionType(t, writing);
            else if ((t.flags & TypeFlags.Substitution) != 0)
                throw unsupported("getNormalizedType: source substitution");
            else if ((t.flags & TypeFlags.Simplifiable) != 0)
                n = getSimplifiedType(t, writing);
            else
                return t;
            if (n == t)
                return n;
            t = n;
        }
    }

    private Type getNormalizedUnionOrIntersectionType(Type t, bool writing)
    {
        var reduced = getReducedType(t);
        if (reduced != t)
            return reduced;
        if ((t.flags & TypeFlags.Intersection) != 0 && shouldNormalizeIntersection(t))
        {
            var types = t.Types();
            var normalized = instantiateList(types, null, (u, _) => getNormalizedType(u, writing));
            if (!ReferenceEquals(normalized, types))
                return getIntersectionType(normalized);
        }

        return t;
    }

    private bool shouldNormalizeIntersection(Type t)
    {
        bool hasInstantiable = false, hasNullableOrEmpty = false;
        foreach (var constituent in t.Types())
        {
            hasInstantiable |= (constituent.flags & TypeFlags.Instantiable) != 0;
            hasNullableOrEmpty |= (constituent.flags & TypeFlags.Nullable) != 0 || IsEmptyAnonymousObjectType(constituent);
            if (hasInstantiable && hasNullableOrEmpty)
                return true;
        }

        return false;
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
                earlySymbols = getExportsOfModuleWorker(symbol).exports;
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
                if ((t.objectFlags & ObjectFlags.Reference) != 0)
                    resolveTypeReferenceMembers(t);
                else if ((t.objectFlags & ObjectFlags.ClassOrInterface) != 0)
                    resolveClassOrInterfaceMembers(t);
                else if ((t.objectFlags & ObjectFlags.Anonymous) != 0)
                    resolveAnonymousTypeMembers(t);
                else
                    throw unsupported("resolveStructuredTypeMembers: mapped/reverse-mapped object");
            }
            else if ((t.flags & TypeFlags.Union) != 0)
                resolveUnionTypeMembers(t);
            else if ((t.flags & TypeFlags.Intersection) != 0)
                resolveIntersectionTypeMembers(t);
            else
                throw new InvalidOperationException("Unhandled case in resolveStructuredTypeMembers");
        }

        return t.AsStructuredType();
    }

    private void resolveAnonymousTypeMembers(Type t)
    {
        var d = t.AsObjectType();
        if ((d.target != null))
        {
            this.setStructuredTypeMembers(t, null, null, null, null);
            var members = this.createInstantiatedSymbolTable(this.getPropertiesOfObjectType(d.target), d.mapper);
            var callSignatures = this.instantiateSignatures(this.getSignaturesOfType(d.target, SignatureKind.Call), d.mapper);
            var constructSignatures = this.instantiateSignatures(this.getSignaturesOfType(d.target, SignatureKind.Construct), d.mapper);
            var indexInfos = this.instantiateIndexInfos(this.getIndexInfosOfType(d.target), d.mapper);
            this.setStructuredTypeMembers(t, members, callSignatures, constructSignatures, indexInfos);
            return;
        }

        var symbol = this.getMergedSymbol(t.symbol);
        if (((symbol.Flags & SymbolFlags.TypeLiteral) != 0))
        {
            this.setStructuredTypeMembers(t, null, null, null, null);
            var members2 = this.getMembersOfSymbol(symbol);
            var callSignatures2 = this.getSignaturesOfSymbol(members2?[Ast.InternalSymbolNameCall]);
            var constructSignatures2 = this.getSignaturesOfSymbol(members2?[Ast.InternalSymbolNameNew]);
            var indexInfos2 = this.getIndexInfosOfSymbol(symbol);
            this.setStructuredTypeMembers(t, members2, callSignatures2, constructSignatures2, indexInfos2);
            return;
        }

        var members3 = this.getExportsOfSymbol(symbol);
        List<IndexInfo> indexInfos3 = default;
        if ((symbol == this.globalThisSymbol))
        {
            var varsOnly = new SymbolTable();
            {
                foreach (var p in GoRuntime.RangeValues(members3))
                {
                    if ((((p.Flags & SymbolFlags.BlockScoped) == 0) && !(((((p.Flags & SymbolFlags.ValueModule) != 0) && (GoRuntime.Len(p.Declarations) != 0)) && Core.Every(p.Declarations, Ast.IsAmbientModule)))))
                    {
                        varsOnly[p.Name] = p;
                    }
                }
            }

            members3 = varsOnly;
        }

        IndexInfo baseConstructorIndexInfo = default;
        this.setStructuredTypeMembers(t, members3, null, null, null);
        if (((symbol.Flags & SymbolFlags.Class) != 0))
        {
            throw unsupported("resolveAnonymousTypeMembers: class construct signature");
        }

        var indexSymbol = members3?[Ast.InternalSymbolNameIndex];
        if ((indexSymbol != null))
        {
            throw unsupported("resolveAnonymousTypeMembers: written index signature");
        }
        else
        {
            if ((baseConstructorIndexInfo != null))
            {
                indexInfos3 = GoRuntime.Append(indexInfos3, baseConstructorIndexInfo);
            }

            if ((symbol.Flags & SymbolFlags.Enum) != 0)
            {
                throw unsupported("resolveAnonymousTypeMembers: enum");
            }
        }

        d.indexInfos = indexInfos3;
        if (((symbol.Flags & ((SymbolFlags.Function | SymbolFlags.Method))) != 0))
        {
            d.signatures = this.getSignaturesOfSymbol(symbol);
            d.callSignatureCount = GoRuntime.Len(d.signatures);
        }

        if (((symbol.Flags & SymbolFlags.Class) != 0))
        {
            throw unsupported("resolveAnonymousTypeMembers: class constructor");
        }
    }

    private SymbolTable createInstantiatedSymbolTable(List<Symbol> symbols, TypeMapper mapper)
    {
        if ((symbols?.Count ?? 0) == 0)
            return null;
        var table = new SymbolTable(symbols.Count);
        foreach (var symbol in symbols)
            table[symbol.Name] = instantiateSymbol(symbol, mapper);
        return table;
    }

    private SymbolTable getExportsOfSymbol(Symbol symbol)
    {
        if ((symbol.Flags & SymbolFlags.LateBindingContainer) != 0)
            return getResolvedMembersOrExportsOfSymbol(symbol, MembersOrExportsResolutionKind.ResolvedExports);
        if ((symbol.Flags & SymbolFlags.Module) != 0)
            return getExportsOfModule(symbol);
        return symbol.Exports;
    }

    private readonly symbolArenaLinkStore<ModuleSymbolLinks> moduleSymbolLinks = new();
    private SymbolTable getExportsOfModule(Symbol moduleSymbol)
    {
        var links = moduleSymbolLinks.Get(moduleSymbol);
        if (links.resolvedExports == null)
        {
            var(exports, typeOnlyExportStarMap) = getExportsOfModuleWorker(moduleSymbol);
            links.resolvedExports = exports;
            links.typeOnlyExportStarMap = typeOnlyExportStarMap;
        }

        return links.resolvedExports;
    }

    private (SymbolTable exports, Dictionary<string, Node> typeOnlyExportStarMap) getExportsOfModuleWorker(Symbol moduleSymbol)
    {
        // Source modules/re-exports are rejected by admission; the genuine
        // internal globalThis module still resolves and clones its exports.
        if (moduleSymbol != globalThisSymbol)
            throw unsupported("getExportsOfModuleWorker: source module");
        List<Symbol> visitedSymbols = null;
        var nonTypeOnlyNames = new HashSet<string>(StringComparer.Ordinal);
        SymbolTable visit(Symbol symbol, Node exportStar, bool isTypeOnly)
        {
            if (!isTypeOnly && symbol != null)
                foreach (var name in (IEnumerable<string>)symbol.Exports?.Keys ?? [])
                    nonTypeOnlyNames.Add(name);
            if (symbol == null || symbol.Exports == null || (visitedSymbols?.Contains(symbol) ?? false))
                return null;
            visitedSymbols = GoRuntime.Append(visitedSymbols, symbol);
            var symbols = new SymbolTable();
            foreach (var entry in symbol.Exports)
                symbols[entry.Key] = entry.Value;
            if (symbol.Exports[Ast.InternalSymbolNameExportStar] != null)
                throw unsupported("getExportsOfModuleWorker: source export star");
            return symbols;
        }

        if (moduleSymbol.Exports?[Ast.InternalSymbolNameExportEquals] != null)
            throw unsupported("getExportsOfModuleWorker: source export equals");
        var exports = visit(getMergedSymbol(moduleSymbol), null, false) ?? new SymbolTable();
        Dictionary<string, Node> typeOnlyExportStarMap = null;
        foreach (var name in nonTypeOnlyNames)
            typeOnlyExportStarMap?.Remove(name);
        return (exports, typeOnlyExportStarMap);
    }

    private Type getTypeFromTypeLiteralOrFunctionOrConstructorTypeNode(Node node)
    {
        var links = this.typeNodeLinks.Get(node);
        if ((links.resolvedType == null))
        {
            var alias = this.getAliasForTypeNode(node);
            {
                var sym = node.Symbol();
                if (((sym == null) || ((GoRuntime.Len(this.getMembersOfSymbol(sym)) == 0) && (alias == null))))
                {
                    links.resolvedType = this.emptyTypeLiteralType;
                }
                else
                {
                    var t = this.newObjectType(ObjectFlags.Anonymous, node.Symbol());
                    t.alias = alias;
                    links.resolvedType = t;
                }
            }
        }

        return links.resolvedType;
    }

    private Type getReducedApparentType(Type t) => getReducedType(getApparentType(getReducedType(t)));
    internal Type getReducedType(Type t)
    {
        if ((t.flags & TypeFlags.Union) != 0 && (t.objectFlags & ObjectFlags.ContainsIntersections) != 0)
            return t.AsUnionType().resolvedReducedType ??= getReducedUnionType(t);
        if ((t.flags & TypeFlags.Intersection) != 0)
        {
            if ((t.objectFlags & ObjectFlags.IsNeverIntersectionComputed) == 0)
            {
                t.objectFlags |= ObjectFlags.IsNeverIntersectionComputed;
                if (!isMappingOfSameObjectType(t.Types()) && somePropertyReducesToNever(t))
                    t.objectFlags |= ObjectFlags.IsNeverIntersection;
            }

            if ((t.objectFlags & ObjectFlags.IsNeverIntersection) != 0)
                return neverType;
        }

        return t;
    }

    private bool isMappingOfSameObjectType(List<Type> types)
    {
        if ((types?.Count ?? 0) != 0 && (types[0].objectFlags & ObjectFlags.Mapped) != 0)
            throw unsupported("isMappingOfSameObjectType: source mapped types");
        return false;
    }

    private bool somePropertyReducesToNever(Type t)
    {
        var counts = new System.Collections.Generic.OrderedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var constituent in t.Types())
            foreach (var prop in getPropertiesOfType(constituent) ?? [])
                counts[prop.Name] = counts.GetValueOrDefault(prop.Name) + 1;
        foreach (var(name, count)in counts)
            if (count > 1)
            {
                var prop = getPropertyOfUnionOrIntersectionType(t, name, true);
                if (prop != null && (isDiscriminantWithNeverType(prop) || isConflictingPrivateProperty(prop)))
                    return true;
            }

        return false;
    }

    private static bool isConflictingPrivateProperty(Symbol prop) => prop.ValueDeclaration == null && (prop.CheckFlags & CheckFlags.ContainsPrivate) != 0;
    private Type getReducedUnionType(Type t)
    {
        var types = t.Types();
        var reduced = instantiateList(types, null, (u, _) => getReducedType(u));
        if (ReferenceEquals(reduced, types))
            return t;
        var result = getUnionType(reduced);
        if ((result.flags & TypeFlags.Union) != 0)
            result.AsUnionType().resolvedReducedType = result;
        return result;
    }

    private Type getApparentType(Type t)
    {
        var original = t;
        if ((t.flags & TypeFlags.Instantiable) != 0)
            t = getBaseConstraintOfType(t) ?? unknownType;
        if ((t.objectFlags & ObjectFlags.Mapped) != 0)
            throw unsupported("getApparentType: source mapped type");
        if ((t.objectFlags & ObjectFlags.Reference) != 0 && t != original)
            return getTypeWithThisArgument(t, original, false);
        if ((t.flags & TypeFlags.Intersection) != 0)
            return getApparentTypeOfIntersectionType(t, original);
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
            return stringNumberSymbolType ??= getUnionType([stringType, numberType, esSymbolType]);
        if ((t.flags & TypeFlags.Unknown) != 0 && !strictNullChecks)
            return emptyObjectType;
        return t;
    }

    private Type getApparentTypeOfIntersectionType(Type t, Type thisArgument)
    {
        if (t == thisArgument)
            return t.AsIntersectionType().resolvedApparentType ??= getTypeWithThisArgument(t, thisArgument, true);
        var key = new CachedTypeKey(CachedTypeKind.ApparentType, thisArgument.id);
        if (!cachedTypes.TryGetValue(key, out var result))
            cachedTypes[key] = result = getTypeWithThisArgument(t, thisArgument, true);
        return result;
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
        t = this.getReducedApparentType(t);
        switch (true)
        {
            case true when ((t.flags & TypeFlags.Object) != 0):
            {
                var resolved = this.resolveStructuredTypeMembers(t);
                var symbol = resolved.members?[name];
                if ((symbol != null))
                {
                    if ((((!includeTypeOnlyMembers && (t.symbol != null)) && ((t.symbol.Flags & SymbolFlags.ValueModule) != 0)) && (this.moduleSymbolLinks.Get(t.symbol).typeOnlyExportStarMap?.GetValueOrDefault(name) != null)))
                    {
                        return null;
                    }

                    if (this.symbolIsValueEx(symbol, includeTypeOnlyMembers))
                    {
                        return symbol;
                    }
                }

                if (skipObjectFunctionPropertyAugment)
                {
                    return null;
                }

                Type functionType = default;
                switch (true)
                {
                    case true when (t == this.anyFunctionType):
                    {
                        functionType = this.globalFunctionType;
                        break;
                    }

                    case true when (GoRuntime.Len(resolved.CallSignatures()) != 0):
                    {
                        functionType = this.globalCallableFunctionType;
                        break;
                    }

                    case true when (GoRuntime.Len(resolved.ConstructSignatures()) != 0):
                    {
                        functionType = this.globalNewableFunctionType;
                        break;
                    }
                }

                if ((functionType != null))
                {
                    symbol = this.getPropertyOfObjectType(functionType, name);
                    if ((symbol != null))
                    {
                        return symbol;
                    }
                }

                return this.getPropertyOfObjectType(this.globalObjectType, name);
            }

            case true when ((t.flags & TypeFlags.Intersection) != 0):
            {
                var prop = this.getPropertyOfUnionOrIntersectionType(t, name, true);
                if ((prop != null))
                {
                    return prop;
                }

                if (!skipObjectFunctionPropertyAugment)
                {
                    return this.getPropertyOfUnionOrIntersectionType(t, name, skipObjectFunctionPropertyAugment);
                }

                return null;
            }

            case true when ((t.flags & TypeFlags.Union) != 0):
            {
                return this.getPropertyOfUnionOrIntersectionType(t, name, skipObjectFunctionPropertyAugment);
            }
        }

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

    private Type getContextualType(Node node, ContextFlags contextFlags = ContextFlags.None)
    {
        if (((node.Flags & NodeFlags.InWithStatement) != 0))
        {
            return null;
        }

        var index = this.findContextualNode(node, (contextFlags == ContextFlags.None));
        if ((index >= 0))
        {
            return this.contextualInfos[index].t;
        }

        var parent = node.Parent;
        switch (parent.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.Parameter:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.BindingElement:
            {
                return this.getContextualTypeForInitializerExpression(node);
            }

            case Kind.ArrowFunction:
            case Kind.ReturnStatement:
            {
                return this.getContextualTypeForReturnExpression(node, contextFlags);
            }

            case Kind.YieldExpression:
            {
                throw unsupported("getContextualType: source YieldExpression");
            }

            case Kind.AwaitExpression:
            {
                throw unsupported("getContextualType: source AwaitExpression");
            }

            case Kind.CallExpression:
            case Kind.NewExpression:
            {
                return this.getContextualTypeForArgument(parent, node);
            }

            case Kind.Decorator:
            {
                throw unsupported("getContextualType: source Decorator");
            }

            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
            {
                if (Ast.IsConstAssertion(parent))
                {
                    return this.getContextualType(parent, contextFlags);
                }

                return this.getTypeFromTypeNode(parent.Type());
            }

            case Kind.BinaryExpression:
            {
                return this.getContextualTypeForBinaryOperand(node, contextFlags);
            }

            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            {
                return this.getContextualTypeForObjectLiteralElement(parent);
            }

            case Kind.SpreadAssignment:
            {
                return this.getContextualType(parent.Parent, contextFlags);
            }

            case Kind.ArrayLiteralExpression:
            {
                throw unsupported("getContextualType: source ArrayLiteralExpression");
            }

            case Kind.ConditionalExpression:
            {
                return this.getContextualTypeForConditionalOperand(node, contextFlags);
            }

            case Kind.TemplateSpan:
            {
                throw unsupported("getContextualType: source TemplateSpan");
            }

            case Kind.ParenthesizedExpression:
            {
                return this.getContextualType(parent, contextFlags);
            }

            case Kind.NonNullExpression:
            {
                return this.getContextualType(parent, contextFlags);
            }

            case Kind.SatisfiesExpression:
            {
                return this.getTypeFromTypeNode(parent.Type());
            }

            case Kind.ExportAssignment:
            {
                return this.tryGetTypeFromTypeNode(parent);
            }

            case Kind.JsxExpression:
            {
                throw unsupported("getContextualType: source JsxExpression");
            }

            case Kind.JsxAttribute:
            case Kind.JsxSpreadAttribute:
            {
                throw unsupported("getContextualType: source JsxSpreadAttribute");
            }

            case Kind.JsxOpeningElement:
            case Kind.JsxSelfClosingElement:
            {
                throw unsupported("getContextualType: source JsxSelfClosingElement");
            }

            case Kind.ImportAttribute:
            {
                throw unsupported("getContextualType: source ImportAttribute");
            }
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

    private Type getContextualTypeForVariableLikeDeclaration(Node declaration, ContextFlags contextFlags = ContextFlags.None)
    {
        var typeNode = declaration.Type();
        if ((typeNode != null))
        {
            return this.getTypeFromTypeNode(typeNode);
        }

        switch (declaration.Kind)
        {
            case Kind.Parameter:
            {
                return this.getContextuallyTypedParameterType(declaration);
            }

            case Kind.BindingElement:
            {
                throw unsupported("contextual binding element");
            }

            case Kind.PropertyDeclaration:
            {
                throw unsupported("contextual class property");
            }
        }

        return null;
    }

    private Type getContextualTypeForObjectLiteralElement(Node element, ContextFlags contextFlags = ContextFlags.None)
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
        if ((constituent.flags & TypeFlags.Intersection) != 0)
        {
            List<Type> types = null, indexCandidates = null;
            bool ignoreIndexInfos = false;
            foreach (var memberType in constituent.Types())
            {
                if ((memberType.flags & TypeFlags.Object) == 0)
                    continue;
                if (isGenericMappedType(memberType))
                    throw unsupported("getTypeOfPropertyOfContextualTypeEx: source mapped substitution");
                var propertyType = getTypeOfConcretePropertyOfContextualType(memberType, name);
                if (propertyType == null)
                {
                    if (!ignoreIndexInfos)
                        indexCandidates = GoRuntime.Append(indexCandidates, memberType);
                    continue;
                }

                ignoreIndexInfos = true;
                indexCandidates = null;
                types = appendContextualPropertyTypeConstituent(types, propertyType);
            }

            foreach (var candidate in indexCandidates ?? [])
                types = appendContextualPropertyTypeConstituent(types, getTypeFromIndexInfosOfContextualType(candidate, name, nameType));
            if ((types?.Count ?? 0) == 0)
                return null;
            if (types.Count == 1)
                return types[0];
            return getIntersectionType(types);
        }

        if ((constituent.flags & TypeFlags.Object) == 0)
            return null;
        if (isGenericMappedType(constituent))
            throw unsupported("getTypeOfPropertyOfContextualTypeEx: source mapped substitution");
        return getTypeOfConcretePropertyOfContextualType(constituent, name) ?? getTypeFromIndexInfosOfContextualType(constituent, name, nameType);
    }, true);
    private Type getTypeOfConcretePropertyOfContextualType(Type t, string name)
    {
        var property = getPropertyOfType(t, name);
        if (property == null || isCircularMappedProperty(property))
            return null;
        return removeMissingType(getTypeOfSymbol(property), (property.Flags & SymbolFlags.Optional) != 0);
    }

    private bool isCircularMappedProperty(Symbol symbol)
    {
        if ((symbol.CheckFlags & CheckFlags.Mapped) != 0)
            return valueSymbolLinks.Get(symbol).resolvedType == null && findResolutionCycleStartIndex(symbol, TypeSystemPropertyName.Type) >= 0;
        return false;
    }

    private Type getTypeFromIndexInfosOfContextualType(Type t, string name, Type nameType)
    {
        if ((t.objectFlags & ObjectFlags.Tuple) != 0)
            throw unsupported("getTypeFromIndexInfosOfContextualType: source tuple");
        nameType ??= getStringLiteralType(name);
        return findApplicableIndexInfo(getIndexInfosOfStructuredType(t), nameType)?.valueType;
    }

    private List<Type> appendContextualPropertyTypeConstituent(List<Type> types, Type t) => t == null ? types : GoRuntime.Append(types, (t.flags & TypeFlags.Any) != 0 ? unknownType : t);
    private Type instantiateContextualType(Type contextualType, Node node, ContextFlags contextFlags = ContextFlags.None)
    {
        if (((contextualType != null) && this.maybeTypeOfKind(contextualType, TypeFlags.Instantiable)))
        {
            var inferenceContext = this.getInferenceContext(node);
            if ((inferenceContext != null))
            {
                throw unsupported("instantiateContextualType: source generic inference");
            }
        }

        return contextualType;
    }

    // checker.go:84/277/821/31514. Callable and source-generic inference contexts
    // are never allocated in the admitted data-type syntax. The actual empty
    // stack lookup remains selected for synthetic interface this-types.
    private sealed class InferenceContext
    {
        internal TypeMapper mapper = null;
        internal TypeMapper nonFixingMapper = null, returnMapper = null;
        internal List<IntraExpressionInferenceSite> intraExpressionInferenceSites = null;
    }

    private sealed record InferenceContextInfo(Node node, InferenceContext context);
    private List<InferenceContextInfo> inferenceContextInfos = null;
    private InferenceContext getInferenceContext(Node node)
    {
        for (int i = (inferenceContextInfos?.Count ?? 0) - 1; i >= 0; i--)
            if (isNodeDescendantOf(node, inferenceContextInfos[i].node))
                return inferenceContextInfos[i].context;
        return null;
    }

    private Type getApparentTypeOfContextualType(Node node, ContextFlags contextFlags = ContextFlags.None)
    {
        Type contextualType = default;
        if (Ast.IsObjectLiteralMethod(node))
        {
            contextualType = this.getContextualTypeForObjectLiteralMethod(node, contextFlags);
        }
        else
        {
            contextualType = this.getContextualType(node, contextFlags);
        }

        var instantiatedType = this.instantiateContextualType(contextualType, node, contextFlags);
        if (((instantiatedType != null) && !((((contextFlags & ContextFlags.NoConstraints) != 0) && ((instantiatedType.flags & TypeFlags.TypeVariable) != 0)))))
        {
            var apparentType = this.mapTypeEx(instantiatedType, (t) =>
            {
                if (((t.objectFlags & ObjectFlags.Mapped) != 0))
                {
                    return t;
                }

                return this.getApparentType(t);
            }, true);
            switch (true)
            {
                case true when (((apparentType.flags & TypeFlags.Union) != 0) && Ast.IsObjectLiteralExpression(node)):
                {
                    return this.discriminateContextualTypeByObjectMembers(node, apparentType);
                }

                case true when (((apparentType.flags & TypeFlags.Union) != 0) && Ast.IsJsxAttributes(node)):
                {
                    throw unsupported("getApparentTypeOfContextualType: JSX");
                }

                default:
                {
                    return apparentType;
                }
            }
        }

        return null;
    }

    private Type getWidenedLiteralLikeTypeForContextualType(Type t, Type contextualType)
    {
        if (!isLiteralOfContextualType(t, contextualType))
        {
            t = getWidenedUniqueESSymbolType(getWidenedLiteralType(t));
        }

        return getRegularTypeOfLiteralType(t);
    }

    private bool isLiteralOfContextualType(Type candidateType, Type contextualType)
    {
        if (contextualType != null)
        {
            if ((contextualType.flags & TypeFlags.UnionOrIntersection) != 0)
                return contextualType.Types().Any(t => isLiteralOfContextualType(candidateType, t));
            if ((contextualType.flags & TypeFlags.InstantiableNonPrimitive) != 0)
            {
                var constraint = getBaseConstraintOfType(contextualType) ?? unknownType;
                return maybeTypeOfKind(constraint, TypeFlags.String) && maybeTypeOfKind(candidateType, TypeFlags.StringLiteral) || maybeTypeOfKind(constraint, TypeFlags.Number) && maybeTypeOfKind(candidateType, TypeFlags.NumberLiteral) || maybeTypeOfKind(constraint, TypeFlags.BigInt) && maybeTypeOfKind(candidateType, TypeFlags.BigIntLiteral) || maybeTypeOfKind(constraint, TypeFlags.ESSymbol) && maybeTypeOfKind(candidateType, TypeFlags.UniqueESSymbol) || isLiteralOfContextualType(candidateType, constraint);
            }

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
                case Kind.MethodDeclaration:
                    t = checkObjectLiteralMethod(memberDeclaration, checkMode);
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
    internal readonly LinkStore<Node, SourceFileLinks> sourceFileLinks = new();
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
        instantiationCount = 0;
        switch (node.Kind)
        {
            case Kind.ObjectLiteralExpression:
                checkContextualDeprecations(node);
                break;
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
                checkFunctionExpressionOrObjectLiteralMethodDeferred(node);
                break;
            case Kind.VoidExpression:
                checkExpression(node.Expression());
                break;
            case Kind.CallExpression:
                resolveUntypedCall(node);
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
    internal readonly record struct CacheHashKey(string preimage);
    // checker.go:17767. Managed complete preimage replaces the opaque XXH3 hash;
    // write widths, little endian order, delimiters and presence tags are exact.
    private sealed class keyBuilder
    {
        private readonly List<byte> bytes = new();
        internal void writeByte(byte value) => bytes.Add(value);
        internal void writeUint32(uint value)
        {
            Span<byte> encoded = stackalloc byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(encoded, value);
            foreach (var b in encoded)
                bytes.Add(b);
        }

        internal void writeUint64(ulong value)
        {
            Span<byte> encoded = stackalloc byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(encoded, value);
            foreach (var b in encoded)
                bytes.Add(b);
        }

        internal void writeInt(int value) => writeUint64(unchecked((ulong)value));
        internal void writeSymbol(Symbol symbol) => writeUint64(Ast.GetSymbolId(symbol));
        internal void writeType(Type t) => writeUint32(t.id);
        internal void writeTypes(List<Type> types)
        {
            writeInt(types?.Count ?? 0);
            foreach (var t in types ?? [])
                writeType(t);
        }

        internal void writeAlias(TypeAlias alias)
        {
            if (alias != null)
            {
                writeByte(1);
                writeSymbol(alias.symbol);
                writeTypes(alias.typeArguments);
            }
            else
                writeByte(0);
        }

        internal void writeNodeId(ulong id) => writeUint64(id);
        internal void writeNode(Node node)
        {
            if (node != null)
                writeNodeId(Ast.GetNodeId(node));
        }

        internal void writeString(string text)
        {
            foreach (var value in text)
                bytes.Add((byte)value);
        }

        internal CacheHashKey hash() => new(Convert.ToBase64String(bytes.ToArray()));
        internal bool writeGenericTypeReferences(Type source, Type target, bool ignoreConstraints)
        {
            bool constrained = false;
            List<Type> typeParameters = new(8);
            void writeTypeReference(Type reference, int depth)
            {
                writeType(reference.Target());
                foreach (var t in reference.AsTypeReference().resolvedTypeArguments ?? [])
                {
                    if ((t.flags & TypeFlags.TypeParameter) != 0)
                    {
                        if (ignoreConstraints || t.checker.getConstraintOfTypeParameter(t) == null)
                        {
                            int index = typeParameters.IndexOf(t);
                            if (index < 0)
                            {
                                index = typeParameters.Count;
                                typeParameters.Add(t);
                            }

                            writeByte((byte)'=');
                            writeInt(index);
                            continue;
                        }

                        constrained = true;
                    }
                    else if (depth < 4 && isTypeReferenceWithGenericArguments(t))
                    {
                        writeByte((byte)'<');
                        writeTypeReference(t, depth + 1);
                        writeByte((byte)'>');
                        continue;
                    }

                    writeByte((byte)'-');
                    writeType(t);
                }
            }

            writeTypeReference(source, 0);
            writeByte((byte)',');
            writeTypeReference(target, 0);
            return constrained;
        }
    }

    private static CacheHashKey getTypeListKey(List<Type> types)
    {
        var b = new keyBuilder();
        b.writeTypes(types);
        return b.hash();
    }

    private static CacheHashKey getAliasKey(TypeAlias alias)
    {
        var b = new keyBuilder();
        b.writeAlias(alias);
        return b.hash();
    }

    private static CacheHashKey getUnionKey(List<Type> types, Type origin, TypeAlias alias)
    {
        var b = new keyBuilder();
        if (origin == null)
            b.writeTypes(types);
        else if ((origin.flags & TypeFlags.Union) != 0)
        {
            b.writeByte((byte)'|');
            b.writeTypes(origin.Types());
        }
        else if ((origin.flags & TypeFlags.Intersection) != 0)
        {
            b.writeByte((byte)'&');
            b.writeTypes(origin.Types());
        }
        else if ((origin.flags & TypeFlags.Index) != 0)
        {
            b.writeByte((byte)'#');
            b.writeType(origin);
            b.writeByte((byte)'|');
            b.writeTypes(types);
        }
        else
            throw new InvalidOperationException("Unhandled case in getUnionKey");
        b.writeAlias(alias);
        return b.hash();
    }

    private static CacheHashKey getIntersectionKey(List<Type> types, IntersectionFlags flags, TypeAlias alias)
    {
        var b = new keyBuilder();
        b.writeTypes(types);
        if ((flags & IntersectionFlags.NoConstraintReduction) == 0)
            b.writeAlias(alias);
        else
            b.writeByte((byte)'*');
        return b.hash();
    }

    private static CacheHashKey getTypeInstantiationKey(List<Type> types, TypeAlias alias, bool singleSignature)
    {
        var b = new keyBuilder();
        b.writeTypes(types);
        b.writeAlias(alias);
        if (singleSignature)
            b.writeByte((byte)'!');
        return b.hash();
    }

    private static CacheHashKey getTypeAliasInstantiationKey(List<Type> types, TypeAlias alias) => getTypeInstantiationKey(types, alias, false);
    private static (CacheHashKey key, bool constrained) getRelationKey(Type source, Type target, IntersectionState intersectionState, bool isIdentity, bool ignoreConstraints)
    {
        if (isIdentity && source.id > target.id)
            (source, target) = (target, source);
        var b = new keyBuilder();
        bool constrained = false;
        if (isTypeReferenceWithGenericArguments(source) && isTypeReferenceWithGenericArguments(target))
        {
            b.writeByte((byte)'g');
            constrained = b.writeGenericTypeReferences(source, target, ignoreConstraints);
        }
        else
        {
            b.writeByte((byte)'s');
            b.writeType(source);
            b.writeType(target);
        }

        b.writeUint32((uint)intersectionState);
        return (b.hash(), constrained);
    }

    private static bool isNonDeferredTypeReference(Type t) => (t.objectFlags & ObjectFlags.Reference) != 0 && t.AsTypeReference().node == null;
    private static bool isTypeReferenceWithGenericArguments(Type t) => isNonDeferredTypeReference(t) && (t.checker.getTypeArguments(t)?.Any(arg => (arg.flags & TypeFlags.TypeParameter) != 0 || isTypeReferenceWithGenericArguments(arg)) ?? false);
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
            else if ((t.flags & TypeFlags.Intersection) != 0)
                result = getIntersectionType(instantiateList(t.Types(), null, (u, _) => getWidenedType(u)));
            else if (isArrayOrTupleType(t))
                result = createTypeReference(t.Target(), instantiateList(getTypeArguments(t), null, (u, _) => getWidenedType(u)));
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
            return t.Types().All(isEmptyObjectType);
        return false;
    }
}

public sealed partial class Checker
{
    private RelationComparisonResult reliabilityFlags;
    private Relater freeRelater;
    private string getSuggestionForNonexistentProperty(string name, Type containingType) => getSpellingSuggestionForName(name, getPropertiesOfType(containingType) ?? [], SymbolFlags.Value)?.Name ?? "";
    internal Symbol getParentOfSymbol(Symbol symbol) => symbol.Parent != null ? getMergedSymbol(getLateBoundSymbol(symbol.Parent)) : null;
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
        var infos = getIndexInfosOfType(t);
        if ((infos?.Count ?? 0) == 0)
            return;
        foreach (var prop in getPropertiesOfObjectType(t) ?? [])
            if (!(isStaticIndex && (prop.Flags & SymbolFlags.Prototype) != 0))
                checkIndexConstraintForProperty(t, prop, getLiteralTypeFromProperty(prop, TypeFlags.StringOrNumberLiteralOrUnique, true), getNonMissingTypeOfSymbol(prop));
        if (symbol.ValueDeclaration != null && Ast.IsClassLike(symbol.ValueDeclaration))
            throw unsupported("checkIndexConstraints: source class dynamic member");
        if (infos.Count > 1)
            foreach (var info in infos)
                checkIndexConstraintForIndexSignature(t, info);
    }

    private List<IndexInfo> getApplicableIndexInfos(Type t, Type keyType) => getIndexInfosOfType(t)?.Where(info => isApplicableIndexType(keyType, info.keyType)).ToList();
    private void checkIndexConstraintForProperty(Type t, Symbol prop, Type propNameType, Type propType)
    {
        var declaration = prop.ValueDeclaration;
        var name = declaration?.Name();
        if (name != null && Ast.IsPrivateIdentifier(name))
            return;
        var infos = getApplicableIndexInfos(t, propNameType);
        if ((infos?.Count ?? 0) == 0)
            return;
        var interfaceDeclaration = (t.objectFlags & ObjectFlags.Interface) != 0 ? Ast.GetDeclarationOfKind(t.symbol, Kind.InterfaceDeclaration) : null;
        var propDeclaration = declaration != null && Ast.IsBinaryExpression(declaration) || name != null && Ast.IsComputedPropertyName(name) ? declaration : null;
        var localProp = getParentOfSymbol(prop) == t.symbol ? declaration : null;
        foreach (var info in infos)
        {
            var localIndex = info.declaration != null && getParentOfSymbol(getSymbolOfDeclaration(info.declaration)) == t.symbol ? info.declaration : null;
            var errorNode = localProp ?? localIndex;
            if (errorNode == null && interfaceDeclaration != null && !(getBaseTypes(t)?.Any(b => getPropertyOfObjectType(b, prop.Name) != null && getIndexTypeOfType(b, info.keyType) != null) ?? false))
                errorNode = interfaceDeclaration;
            if (errorNode != null && !isTypeAssignableTo(propType, info.valueType))
            {
                var diagnostic = NewDiagnosticForNode(errorNode, Diagnostics.Property_0_of_type_1_is_not_assignable_to_2_index_type_3, symbolToString(prop), typeToStringByteString(propType), typeToStringByteString(info.keyType), typeToStringByteString(info.valueType));
                if (propDeclaration != null && errorNode != propDeclaration)
                    diagnostic.AddRelatedInfo(NewDiagnosticForNode(propDeclaration, Diagnostics.X_0_is_declared_here, symbolToString(prop)));
                addDiagnostic(diagnostic);
            }
        }
    }

    private void checkIndexConstraintForIndexSignature(Type t, IndexInfo checkInfo)
    {
        var declaration = checkInfo.declaration;
        var infos = getApplicableIndexInfos(t, checkInfo.keyType);
        if ((infos?.Count ?? 0) == 0)
            return;
        var interfaceDeclaration = (t.objectFlags & ObjectFlags.Interface) != 0 ? Ast.GetDeclarationOfKind(t.symbol, Kind.InterfaceDeclaration) : null;
        var localCheck = declaration != null && getParentOfSymbol(getSymbolOfDeclaration(declaration)) == t.symbol ? declaration : null;
        foreach (var info in infos)
        {
            if (info == checkInfo)
                continue;
            var localIndex = info.declaration != null && getParentOfSymbol(getSymbolOfDeclaration(info.declaration)) == t.symbol ? info.declaration : null;
            var errorNode = localCheck ?? localIndex;
            if (errorNode == null && interfaceDeclaration != null && !(getBaseTypes(t)?.Any(b => getIndexInfoOfType(b, checkInfo.keyType) != null && getIndexTypeOfType(b, info.keyType) != null) ?? false))
                errorNode = interfaceDeclaration;
            if (errorNode != null && !isTypeAssignableTo(checkInfo.valueType, info.valueType))
                error(errorNode, Diagnostics.X_0_index_type_1_is_not_assignable_to_2_index_type_3, typeToStringByteString(checkInfo.keyType), typeToStringByteString(checkInfo.valueType), typeToStringByteString(info.keyType), typeToStringByteString(info.valueType));
        }
    }

    private void checkTypeForDuplicateIndexSignatures(Node node)
    {
        var indexSymbol = getIndexSymbol(getSymbolOfDeclaration(node));
        if (indexSymbol == null || (indexSymbol.Declarations?.Count ?? 0) <= 1)
            return;
        throw unsupported("checkTypeForDuplicateIndexSignatures: index signatures");
    }

    private void checkObjectTypeForDuplicateDeclarations(Node node, bool checkPrivateNames)
    {
        Dictionary<string, int> instanceNames = null;
        foreach (var member in node.Members() ?? [])
        {
            // checker.go:3238. Methods/call signatures do not run data-property duplicate checks.
            if (!Ast.IsPropertySignatureDeclaration(member))
            {
                if (Ast.IsMethodSignatureDeclaration(member) || Ast.IsCallSignatureDeclaration(member))
                    continue;
                throw unsupported("checkObjectTypeForDuplicateDeclarations: class/accessor member");
            }

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
            t = getBaseConstraintOfType(t) ?? unknownType;
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
        {
            bool zero = isZeroBigInt(t);
            return strictNullChecks ? zero ? TypeFacts.ZeroBigIntStrictFacts : TypeFacts.NonZeroBigIntStrictFacts : zero ? TypeFacts.ZeroBigIntFacts : TypeFacts.NonZeroBigIntFacts;
        }

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

        if ((flags & TypeFlags.Intersection) != 0)
            return getIntersectionTypeFacts(t, callerOnlyNeeds);
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
        var reduced = recombineUnknownType(getTypeWithFacts(strictNullChecks && (t.flags & TypeFlags.Unknown) != 0 ? unknownUnionType : t, facts));
        if (strictNullChecks)
        {
            switch (facts)
            {
                case TypeFacts.NEUndefined:
                    return removeNullableByIntersection(reduced, TypeFacts.EQUndefined, TypeFacts.EQNull, TypeFacts.IsNull, nullType);
                case TypeFacts.NENull:
                    return removeNullableByIntersection(reduced, TypeFacts.EQNull, TypeFacts.EQUndefined, TypeFacts.IsUndefined, undefinedType);
                case TypeFacts.NEUndefinedOrNull:
                case TypeFacts.Truthy:
                    return mapType(reduced, s => hasTypeFacts(s, TypeFacts.EQUndefinedOrNull) ? getGlobalNonNullableTypeInstantiation(s) : s);
            }
        }

        return reduced;
    }

    private Type removeNullableByIntersection(Type t, TypeFacts targetFacts, TypeFacts otherFacts, TypeFacts includesFacts, Type otherType)
    {
        var facts = getTypeFacts(t, TypeFacts.EQUndefined | TypeFacts.EQNull | TypeFacts.IsUndefined | TypeFacts.IsNull);
        if ((facts & targetFacts) == 0)
            return t;
        var emptyAndOther = getUnionType([emptyObjectType, otherType]);
        return mapType(t, u => hasTypeFacts(u, targetFacts) ? getIntersectionType([u, (facts & includesFacts) == 0 && hasTypeFacts(u, otherFacts) ? emptyAndOther : emptyObjectType]) : u);
    }

    private Type recombineUnknownType(Type t) => t == unknownUnionType ? unknownType : t;
    private Type getGlobalNonNullableTypeInstantiation(Type t)
    {
        var alias = getGlobalNonNullableTypeAliasOrNil();
        if (alias != null)
            throw unsupported("getGlobalNonNullableTypeInstantiation: source generic library alias");
        return getIntersectionType([t, emptyObjectType]);
    }

    private Symbol globalNonNullableTypeAlias;
    private bool globalNonNullableTypeAliasResolved;
    private Symbol getGlobalNonNullableTypeAliasOrNil()
    {
        if (!globalNonNullableTypeAliasResolved)
        {
            globalNonNullableTypeAlias = getGlobalTypeAliasSymbol("NonNullable", 1, false);
            globalNonNullableTypeAliasResolved = true;
        }

        return globalNonNullableTypeAlias;
    }

    private Symbol getGlobalTypeAliasSymbol(string name, int arity, bool reportErrors)
    {
        var symbol = getGlobalSymbol(name, SymbolFlags.TypeAlias, reportErrors ? Diagnostics.Cannot_find_global_type_0 : null);
        if (symbol == null)
            return null;
        getDeclaredTypeOfSymbol(symbol);
        if ((typeAliasLinks.Get(symbol).typeParameters?.Count ?? 0) != arity)
        {
            if (reportErrors)
                error(symbol.Declarations.Find(Ast.IsTypeAliasDeclaration), Diagnostics.Global_type_0_must_have_1_type_parameter_s, Ast.SymbolName(symbol), arity);
            return null;
        }

        return symbol;
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
    private Type checkPropertyAccessExpression(Node node, CheckMode checkMode = CheckMode.Normal, bool writeOnly = false)
    {
        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            throw unsupported("optional property access");
        }

        var expr = node.Expression();
        return this.checkPropertyAccessExpressionOrQualifiedName(node, expr, this.checkNonNullExpression(expr), node.AsPropertyAccessExpression().Name(), checkMode, writeOnly);
    }

    private Type checkPropertyAccessExpressionOrQualifiedName(Node node, Node left, Type leftType, Node right, CheckMode checkMode = CheckMode.Normal, bool writeOnly = false)
    {
        var parentSymbol = this.getResolvedSymbolOrNil(left);
        var assignmentKind = getAssignmentTargetKind(node);
        var widenedType = leftType;
        if (((assignmentKind != AssignmentKind.None) || this.isMethodAccessForCall(node)))
        {
            widenedType = this.getWidenedType(leftType);
        }

        var apparentType = this.getApparentType(widenedType);
        var isAnyLike = (IsTypeAny(apparentType) || (apparentType == this.silentNeverType));
        Symbol prop = default;
        if (Ast.IsPrivateIdentifier(right))
        {
            throw unsupported("private class member access");
        }
        else
        {
            if (isAnyLike)
            {
                if ((Ast.IsIdentifier(left) && (parentSymbol != null)))
                {
                    this.markPropertyAliasReferenced(node, null, leftType);
                }

                if (this.isErrorType(apparentType))
                {
                    return this.errorType;
                }

                return apparentType;
            }

            prop = this.getPropertyOfTypeEx(apparentType, right.Text(), isConstEnumObjectType(apparentType), (node.Kind == Kind.QualifiedName));
        }

        this.markPropertyAliasReferenced(node, prop, leftType);
        Type propType = default;
        if ((prop == null))
        {
            IndexInfo indexInfo = default;
            if ((!Ast.IsPrivateIdentifier(right) && ((((assignmentKind == AssignmentKind.None) || !this.isGenericObjectType(leftType)) || isThisTypeParameter(leftType)))))
            {
                indexInfo = this.getApplicableIndexInfoForName(apparentType, right.Text());
            }

            if ((indexInfo == null))
            {
                var isUncheckedJS = false; // TS-only fixed profile; JS suggestions excluded.
                if ((!isUncheckedJS && this.isJSLiteralType(leftType)))
                {
                    return this.anyType;
                }

                if (leftType.symbol == globalThisSymbol)
                {
                    var globalSymbol = globalThisSymbol.Exports[right.Text()];
                    if (globalSymbol != null && (globalSymbol.Flags & SymbolFlags.BlockScoped) != 0)
                        error(right, Diagnostics.Property_0_does_not_exist_on_type_1, right.Text(), typeToStringByteString(leftType));
                    else if (noImplicitAny)
                        error(right, Diagnostics.Element_implicitly_has_an_any_type_because_type_0_has_no_index_signature, typeToStringByteString(leftType));
                    return anyType;
                }

                if (((right.Text() != "") && !this.checkAndReportErrorForExtendingInterface(node)))
                {
                    this.addDeferredDiagnostic(() =>
                    {
                        this.reportNonexistentProperty(right, Core.IfElse(isThisTypeParameter(leftType), apparentType, leftType), isUncheckedJS);
                    });
                }

                return this.errorType;
            }

            if ((indexInfo.isReadonly && ((Ast.IsAssignmentTarget(node) || isDeleteTarget(node)))))
            {
                this.error(node, Diagnostics.Index_signature_in_type_0_only_permits_reading, this.typeToStringByteString(apparentType));
            }

            propType = indexInfo.valueType;
            if (((false) && (getAssignmentTargetKind(node) != AssignmentKind.Definite)))
            {
                propType = this.getUnionType(new List<Type> { propType, this.missingType });
            }

            if (((false) && Ast.IsPropertyAccessExpression(node)))
            {
                this.error(right, Diagnostics.Property_0_comes_from_an_index_signature_so_it_must_be_accessed_with_0, right.Text());
            }

            if (((indexInfo.declaration != null) && this.IsDeprecatedDeclaration(indexInfo.declaration)))
            {
                this.addDeprecatedSuggestion(right, new List<Node> { indexInfo.declaration }, right.Text());
            }
        }
        else
        {
            var targetPropSymbol = this.resolveAliasWithDeprecationCheck(prop, right);
            if (((this.isDeprecatedSymbol(targetPropSymbol) && this.isUncalledFunctionReference(node, targetPropSymbol)) && (targetPropSymbol.Declarations != null)))
            {
                this.addDeprecatedSuggestion(right, targetPropSymbol.Declarations, right.Text());
            }

            this.checkPropertyNotUsedBeforeDeclaration(prop, node, right);
            this.markPropertyAsReferenced(prop, node, this.isSelfTypeAccess(left, parentSymbol));
            this.symbolNodeLinks.Get(node).resolvedSymbol = prop;
            this.checkPropertyAccessibility(node, (left.Kind == Kind.SuperKeyword), Ast.IsWriteAccess(node), apparentType, prop);
            if (this.isAssignmentToReadonlyEntity(node, prop, assignmentKind))
            {
                this.error(right, Diagnostics.Cannot_assign_to_0_because_it_is_a_read_only_property, right.Text());
                return this.errorType;
            }

            switch (true)
            {
                case true when this.isThisPropertyAccessInConstructor(node, prop):
                {
                    propType = this.autoType;
                    break;
                }

                case true when (writeOnly || Ast.IsWriteOnlyAccess(node)):
                {
                    propType = this.getWriteTypeOfSymbol(prop);
                    break;
                }

                default:
                {
                    propType = this.getTypeOfSymbol(prop);
                    break;
                }
            }
        }

        return this.getFlowTypeOfAccessExpression(node, prop, propType, right, checkMode);
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

    private Type getFlowTypeOfAccessExpression(Node node, Symbol prop, Type propType, Node errorNode, CheckMode checkMode = CheckMode.Normal)
    {
        var assignmentKind = getAssignmentTargetKind(node);
        if ((assignmentKind == AssignmentKind.Definite))
        {
            return this.removeMissingType(propType, ((prop != null) && ((prop.Flags & SymbolFlags.Optional) != 0)));
        }

        if ((((prop != null) && ((prop.Flags & (((SymbolFlags.Variable | SymbolFlags.Property) | SymbolFlags.Accessor))) == 0)) && !((((prop.Flags & SymbolFlags.Method) != 0) && ((propType.flags & TypeFlags.Union) != 0)))))
        {
            return propType;
        }

        if ((propType == this.autoType))
        {
            return this.getFlowTypeOfProperty(node, prop);
        }

        propType = this.getNarrowableTypeForReference(propType, node, checkMode);
        var assumeUninitialized = false;
        if ((this.strictNullChecks && (prop != null)))
        {
            {
                var declaration = prop.ValueDeclaration;
                if ((declaration != null))
                {
                    if (((this.strictPropertyInitialization && Ast.IsAccessExpression(node)) && (node.Expression().Kind == Kind.ThisKeyword)))
                    {
                        throw unsupported("getFlowTypeOfAccessExpression: class property initialization");
                    }
                    else
                    {
                        if (((Ast.IsBinaryExpression(declaration) && Ast.IsPropertyAccessExpression(declaration.AsBinaryExpression().Left)) && (this.getControlFlowContainer(node) == this.getControlFlowContainer(declaration))))
                        {
                            assumeUninitialized = true;
                        }
                    }
                }
            }
        }

        var flowType = this.getFlowTypeOfReferenceEx(node, propType, this.addOptionalityEx(propType, false, assumeUninitialized), null, null);
        if (((assumeUninitialized && !this.containsUndefinedType(propType)) && this.containsUndefinedType(flowType)))
        {
            this.error(errorNode, Diagnostics.Property_0_is_used_before_being_assigned, this.symbolToString(prop));
            return propType;
        }

        if ((assignmentKind != AssignmentKind.None))
        {
            return this.getBaseTypeOfLiteralType(flowType);
        }

        return flowType;
    }

    // checker.go:19366/19370. Resolving the structured union itself is observable
    // even when the retained signature slices are nil.
    internal List<Signature> getSignaturesOfType(Type t, SignatureKind kind) => getSignaturesOfStructuredType(getReducedApparentType(t), kind);
    private List<Signature> getSignaturesOfStructuredType(Type t, SignatureKind kind)
    {
        if ((t.flags & TypeFlags.StructuredType) == 0)
            return null;
        var resolved = resolveStructuredTypeMembers(t);
        int start = kind == SignatureKind.Call ? 0 : resolved.callSignatureCount;
        int end = kind == SignatureKind.Call ? resolved.callSignatureCount : resolved.signatures?.Count ?? 0;
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
        if (promisedType != null && getPropertyOfType(promisedType, propertyNode.Text()) != null)
        {
            diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(propertyNode), typeToStringByteString(containingType));
            diagnostic.AddRelatedInfo(NewDiagnosticForNode(propertyNode, Diagnostics.Did_you_forget_to_use_await));
        }
        else
        {
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
                    diagnostic = elaborateNeverIntersection(diagnostic, propertyNode, containingType);
                    if (containerSeemsToBeEmptyDomElement(containingType))
                        throw unsupported("reportNonexistentProperty: DOM library suggestion");
                    diagnostic = NewDiagnosticChainForNode(diagnostic, propertyNode, Diagnostics.Property_0_does_not_exist_on_type_1, missingProperty, container);
                }
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

    private IndexInfo getApplicableIndexInfoForName(Type t, string name) => getApplicableIndexInfo(t, isLateBoundName(name) ? esSymbolType : getStringLiteralType(name));
    private Type globalPromiseType;
    private Type getGlobalPromiseType() => globalPromiseType ??= getGlobalType("Promise", 1, false);
    private Type GetPromisedTypeOfPromise(Type t) => getPromisedTypeOfPromiseEx(t, null, null);
    private Type getPromisedTypeOfPromiseEx(Type t, Node errorNode, GoRef<Type> thisTypeForErrorOut)
    {
        if (IsTypeAny(t))
        {
            return null;
        }

        var key = new CachedTypeKey(CachedTypeKind.PromisedTypeOfPromise, t.id);
        {
            var cached = this.cachedTypes.GetValueOrDefault(key);
            if ((cached != null))
            {
                return cached;
            }
        }

        if (this.isReferenceToType(t, this.getGlobalPromiseType()))
        {
            var result = this.getTypeArguments(t)[0];
            this.cachedTypes[key] = result;
            return result;
        }

        if (this.allTypesAssignableToKind(this.getBaseConstraintOrType(t), (TypeFlags.Primitive | TypeFlags.Never)))
        {
            return null;
        }

        var thenFunction = this.getTypeOfPropertyOfType(t, "then");
        if (IsTypeAny(thenFunction))
        {
            return null;
        }

        List<Signature> thenSignatures = default;
        if ((thenFunction != null))
        {
            thenSignatures = this.getSignaturesOfType(thenFunction, SignatureKind.Call);
        }

        if ((GoRuntime.Len(thenSignatures) == 0))
        {
            if ((errorNode != null))
            {
                this.error(errorNode, Diagnostics.A_promise_must_have_a_then_method);
            }

            return null;
        }

        Type thisTypeForError = default;
        List<Signature> candidates = default;
        {
            foreach (var thenSignature in GoRuntime.RangeValues(thenSignatures))
            {
                var thisType = this.getThisTypeOfSignature(thenSignature);
                if ((((thisType != null) && (thisType != this.voidType)) && !this.isTypeRelatedTo(t, thisType, this.subtypeRelation)))
                {
                    thisTypeForError = thisType;
                }
                else
                {
                    candidates = GoRuntime.Append(candidates, thenSignature);
                }
            }
        }

        if ((GoRuntime.Len(candidates) == 0))
        {
            Debug.Assert((thisTypeForError != null));
            if ((thisTypeForErrorOut != null))
            {
                thisTypeForErrorOut.Value = thisTypeForError;
            }

            if ((errorNode != null))
            {
                this.error(errorNode, Diagnostics.The_this_context_of_type_0_is_not_assignable_to_method_s_this_of_type_1, this.typeToStringByteString(t), this.typeToStringByteString(thisTypeForError));
            }

            return null;
        }

        var onfulfilledParameterType = this.getTypeWithFacts(this.getUnionType(Core.Map(candidates, this.getTypeOfFirstParameterOfSignature)), TypeFacts.NEUndefinedOrNull);
        if (IsTypeAny(onfulfilledParameterType))
        {
            return null;
        }

        var onfulfilledParameterSignatures = this.getSignaturesOfType(onfulfilledParameterType, SignatureKind.Call);
        if ((GoRuntime.Len(onfulfilledParameterSignatures) == 0))
        {
            if ((errorNode != null))
            {
                this.error(errorNode, Diagnostics.The_first_parameter_of_the_then_method_of_a_promise_must_be_a_callback);
            }

            return null;
        }

        var result2 = this.getUnionTypeEx(Core.Map(onfulfilledParameterSignatures, this.getTypeOfFirstParameterOfSignature), UnionReduction.Subtype, null, null);
        this.cachedTypes[key] = result2;
        return result2;
    }
}

public sealed partial class Checker
{
    [Flags]
    internal enum CheckMode : uint
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
        var calls = getUnionSignatures(t.Types().Select(u => getSignaturesOfType(u, SignatureKind.Call)).ToList());
        if ((calls?.Count ?? 0) == 0)
            calls = getArrayMemberCallSignatures(t);
        var constructs = getUnionSignatures(t.Types().Select(u => getSignaturesOfType(u, SignatureKind.Construct)).ToList());
        setStructuredTypeMembers(t, null, calls, constructs, getUnionIndexInfos(t.Types()));
    }

    private List<Signature> getUnionSignatures(List<List<Signature>> signatureLists)
    {
        List<Signature> result = default;
        int indexWithLengthOverOne = default;
        int countLengthOverOne = default;
        {
            foreach (var i in GoRuntime.RangeKeys(signatureLists))
            {
                if ((GoRuntime.Len(signatureLists[i]) == 0))
                {
                    return null;
                }

                if ((GoRuntime.Len(signatureLists[i]) > 1))
                {
                    indexWithLengthOverOne = i;
                    countLengthOverOne++;
                }

                {
                    foreach (var signature in GoRuntime.RangeValues(signatureLists[i]))
                    {
                        if (((result == null) || (this.findMatchingSignature(result, signature, false, false, true) == null)))
                        {
                            var unionSignatures = this.findMatchingSignatures(signatureLists, signature, i);
                            if ((unionSignatures != null))
                            {
                                var s = signature;
                                if ((GoRuntime.Len(unionSignatures) > 1))
                                {
                                    var thisParameter = signature.thisParameter;
                                    var firstThisParameterOfUnionSignatures = Core.FirstNonNil(unionSignatures, (sig) =>
                                    {
                                        return sig.thisParameter;
                                    });
                                    if ((firstThisParameterOfUnionSignatures != null))
                                    {
                                        var thisType = this.getIntersectionType(Core.MapNonNil(unionSignatures, (sig2) =>
                                        {
                                            if ((sig2.thisParameter != null))
                                            {
                                                return this.getTypeOfSymbol(sig2.thisParameter);
                                            }

                                            return null;
                                        }));
                                        thisParameter = this.createSymbolWithType(firstThisParameterOfUnionSignatures, thisType);
                                    }

                                    s = this.createUnionSignature(signature, unionSignatures);
                                    s.thisParameter = thisParameter;
                                }

                                result = GoRuntime.Append(result, s);
                            }
                        }
                    }
                }
            }
        }

        if (((GoRuntime.Len(result) == 0) && (countLengthOverOne <= 1)))
        {
            var masterList = signatureLists[indexWithLengthOverOne];
            List<Signature> results = GoSlices.Clone(masterList);
            {
                foreach (var range3 in GoRuntime.Range(signatureLists))
                {
                    var _2 = range3.Key;
                    var signatures = range3.Value;
                    if (!ReferenceEquals(signatures, masterList))
                    {
                        var signature2 = signatures[0];
                        Debug.Assert((signature2 != null), "getUnionSignatures bails early on empty signature lists and should not have empty lists on second pass");
                        if (((GoRuntime.Len(signature2.typeParameters) != 0) && Core.Some(results, (s2) =>
                        {
                            return ((GoRuntime.Len(s2.typeParameters) != 0) && !this.compareTypeParametersIdentical(signature2.typeParameters, s2.typeParameters));
                        })))
                        {
                            results = null;
                        }
                        else
                        {
                            results = Core.Map(results, (sig3) =>
                            {
                                return this.combineUnionOrIntersectionMemberSignatures(sig3, signature2, true);
                            });
                        }

                        if ((results == null))
                        {
                            break;
                        }
                    }
                }
            }

            result = results;
        }

        return result;
    }

    private List<Signature> getArrayMemberCallSignatures(Type t)
    {
        foreach (var u in t.Types())
            if ((u.objectFlags & ObjectFlags.Instantiated) == 0 || u.symbol == null || u.symbol.Parent == null || !isArrayOrTupleSymbol(u.symbol.Parent))
                return null;
        throw unsupported("getArrayMemberCallSignatures: source array member");
    }

    private bool isArrayOrTupleSymbol(Symbol symbol)
    {
        if (symbol == null || globalArrayType.symbol == null || globalReadonlyArrayType.symbol == null)
            return false;
        return getSymbolIfSameReference(symbol, globalArrayType.symbol) != null || getSymbolIfSameReference(symbol, globalReadonlyArrayType.symbol) != null;
    }

    private List<IndexInfo> getUnionIndexInfos(List<Type> types)
    {
        var sourceInfos = getIndexInfosOfType(types[0]);
        List<IndexInfo> result = null;
        foreach (var info in sourceInfos ?? [])
            if (types.All(t => getIndexInfoOfType(t, info.keyType) != null))
            {
                var valueType = getUnionType(types.Select(t => getIndexTypeOfType(t, info.keyType)).ToList());
                result = GoRuntime.Append(result, newIndexInfo(info.keyType, valueType, types.Any(t => getIndexInfoOfType(t, info.keyType).isReadonly), null, null));
            }

        return result;
    }

    private Type getIndexTypeOfType(Type t, Type keyType) => getIndexInfoOfType(t, keyType)?.valueType;
    private bool typeHasCallOrConstructSignatures(Type t) => (t.flags & TypeFlags.StructuredType) != 0 && (resolveStructuredTypeMembers(t).signatures?.Count ?? 0) != 0;
    private List<Symbol> getPropertiesOfUnionOrIntersectionType(Type t)
    {
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
                        var combined = getPropertyOfUnionOrIntersectionType(t, prop.Name, (t.flags & TypeFlags.Intersection) != 0);
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
        bool isUnion = (containingType.flags & TypeFlags.Union) != 0;
        bool mergedInstantiations = false;
        SymbolFlags propFlags = 0, optionalFlag = 0;
        Symbol singleProp = null;
        var propSet = new List<Symbol>();
        List<Type> indexTypes = null;
        CheckFlags checkFlags = isUnion ? 0 : CheckFlags.Readonly;
        optionalFlag = isUnion ? 0 : SymbolFlags.Optional;
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
                {
                    if (isUnion)
                        optionalFlag |= prop.Flags & SymbolFlags.Optional;
                    else
                        optionalFlag &= prop.Flags;
                }

                if (singleProp == null)
                {
                    singleProp = prop;
                    propFlags = (prop.Flags & SymbolFlags.Accessor) != 0 ? prop.Flags & SymbolFlags.Accessor : SymbolFlags.Property;
                }
                else if (prop != singleProp)
                {
                    if (getTargetSymbol(prop) == getTargetSymbol(singleProp) && compareProperties(singleProp, prop, (a, b) => a == b ? Ternary.True : Ternary.False) == Ternary.True)
                        mergedInstantiations = singleProp.Parent != null && (getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(singleProp.Parent)?.Count ?? 0) != 0;
                    else
                    {
                        if (propSet.Count == 0)
                            propSet.Add(singleProp);
                        if (!propSet.Contains(prop))
                            propSet.Add(prop);
                    }

                    if ((propFlags & SymbolFlags.Accessor) != 0 && (prop.Flags & SymbolFlags.Accessor) != (propFlags & SymbolFlags.Accessor))
                        propFlags = propFlags & ~SymbolFlags.Accessor | SymbolFlags.Property;
                }

                if (isUnion && isReadonlySymbol(prop))
                    checkFlags |= CheckFlags.Readonly;
                else if (!isUnion && !isReadonlySymbol(prop))
                    checkFlags &= ~CheckFlags.Readonly;
                checkFlags |= CheckFlags.ContainsPublic | CheckFlags.ContainsWritePublic;
                if ((modifiers & ModifierFlags.Static) != 0)
                    checkFlags |= CheckFlags.ContainsStatic;
                if ((prop.Flags & SymbolFlags.Prototype) == 0)
                    syntheticFlag = CheckFlags.SyntheticProperty;
            }
            else if (isUnion)
            {
                var indexInfo = !isLateBoundName(name) ? getApplicableIndexInfoForName(t, name) : null;
                if (indexInfo != null)
                {
                    propFlags = propFlags & ~SymbolFlags.Accessor | SymbolFlags.Property;
                    checkFlags |= CheckFlags.WritePartial | (indexInfo.isReadonly ? CheckFlags.Readonly : 0);
                    if ((t.objectFlags & ObjectFlags.Tuple) != 0)
                        throw unsupported("createUnionOrIntersectionProperty: source tuple");
                    (indexTypes ??= []).Add(indexInfo.valueType);
                }
                else if ((t.objectFlags & ObjectFlags.ObjectLiteral) != 0 && (t.objectFlags & ObjectFlags.ContainsSpread) == 0)
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
        {
            if (!mergedInstantiations)
                return singleProp;
            Type singleType = null;
            TypeMapper singleMapper = null;
            if ((singleProp.Flags & SymbolFlags.Transient) != 0)
            {
                var singleLinks = valueSymbolLinks.Get(singleProp);
                singleType = singleLinks.resolvedType;
                singleMapper = singleLinks.mapper;
            }

            var clone = createSymbolWithType(singleProp, singleType);
            if (singleProp.ValueDeclaration != null)
                clone.Parent = singleProp.ValueDeclaration.Symbol().Parent;
            var cloneLinks = valueSymbolLinks.Get(clone);
            cloneLinks.containingType = containingType;
            cloneLinks.mapper = singleMapper;
            cloneLinks.writeType = getWriteTypeOfSymbol(singleProp);
            return clone;
        }

        if (propSet.Count == 0)
            propSet.Add(singleProp);
        List<Node> declarations = null;
        Type firstType = null, nameType = null;
        var propTypes = new List<Type>();
        List<Type> writeTypes = null;
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

            var writeType = getWriteTypeOfSymbol(prop);
            if (writeTypes != null || writeType != t)
            {
                writeTypes ??= new List<Type>(propTypes);
                writeTypes.Add(writeType);
            }

            if (t != firstType)
                checkFlags |= CheckFlags.HasNonUniformType;
            if (isLiteralType(t))
                checkFlags |= CheckFlags.HasLiteralType;
            if ((t.flags & TypeFlags.Never) != 0 && t != uniqueLiteralType)
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
        {
            result.CheckFlags |= CheckFlags.DeferredType;
            var deferred = deferredSymbolLinks.Get(result);
            deferred.parent = containingType;
            deferred.constituents = propTypes;
            deferred.writeConstituents = writeTypes;
            return result;
        }

        links.resolvedType = isUnion ? getUnionType(propTypes) : getIntersectionType(propTypes);
        if (writeTypes != null)
            links.writeType = isUnion ? getUnionType(writeTypes) : getIntersectionType(writeTypes);
        return result;
    }

    private Type getTypeOfPropertyOrIndexSignatureOfType(Type t, string name)
    {
        var propertyType = getTypeOfPropertyOfType(t, name);
        if (propertyType != null)
            return propertyType;
        var indexInfo = getApplicableIndexInfoForName(t, name);
        if (indexInfo != null)
            return addOptionalityEx(indexInfo.valueType, true, true);
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
            var members = (getPropertiesOfType(contextualType) ?? []).Where(s => (s.Flags & SymbolFlags.Optional) != 0 && node.Symbol().Members?[s.Name] == null && isDiscriminantProperty(contextualType, s.Name)).ToList();
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
    private readonly Dictionary<CacheHashKey, Type> intersectionTypes = new();
    private Type getIntersectionType(List<Type> types) => getIntersectionTypeEx(types, IntersectionFlags.None, null);
    private Type getIntersectionTypeEx(List<Type> types, IntersectionFlags flags, TypeAlias alias)
    {
        var orderedTypes = new List<Type>(types.Count);
        var includes = addTypesToIntersection(orderedTypes, 0, types);
        var typeSet = orderedTypes;
        ObjectFlags objectFlags = ObjectFlags.None;
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
        if (typeSet.Count == 2 && (flags & IntersectionFlags.NoConstraintReduction) == 0)
        {
            int typeVarIndex = (typeSet[0].flags & TypeFlags.TypeVariable) != 0 ? 0 : 1;
            var typeVariable = typeSet[typeVarIndex];
            var primitiveType = typeSet[1 - typeVarIndex];
            if ((typeVariable.flags & TypeFlags.TypeVariable) != 0 && ((primitiveType.flags & (TypeFlags.Primitive | TypeFlags.NonPrimitive)) != 0 && !isGenericStringLikeType(primitiveType) || (includes & TypeFlags.IncludesEmptyObject) != 0))
            {
                var constraint = getBaseConstraintOfType(typeVariable);
                if (constraint != null && (constraint.Distributed()?.All(isPrimitiveOrObjectOrEmptyType) ?? true))
                {
                    if (isTypeStrictSubtypeOf(constraint, primitiveType))
                        return typeVariable;
                    if (!((constraint.flags & TypeFlags.Union) != 0 && constraint.Types().Any(n => isTypeStrictSubtypeOf(n, primitiveType))))
                        if (!isTypeStrictSubtypeOf(primitiveType, constraint))
                            return neverType;
                    objectFlags = ObjectFlags.IsConstrainedTypeVariable;
                }
            }
        }

        var key = getIntersectionKey(typeSet, flags, alias);
        if (intersectionTypes.TryGetValue(key, out var result))
            return result;
        if ((includes & TypeFlags.Union) != 0)
        {
            var(reducedSet, reduced) = intersectUnionsOfPrimitiveTypes(typeSet);
            typeSet = reducedSet;
            if (reduced)
                result = getIntersectionTypeEx(typeSet, flags, alias);
            else if (typeSet.All(isUnionWithUndefined))
            {
                var containedUndefined = typeSet.Any(containsMissingType) ? missingType : undefinedType;
                filterTypes(typeSet, t => (t.flags & TypeFlags.Undefined) == 0);
                result = getUnionTypeEx([getIntersectionTypeEx(typeSet, flags, null), containedUndefined], UnionReduction.Literal, alias, null);
            }
            else if (typeSet.All(isUnionWithNull))
            {
                filterTypes(typeSet, t => (t.flags & TypeFlags.Null) == 0);
                result = getUnionTypeEx([getIntersectionTypeEx(typeSet, flags, null), nullType], UnionReduction.Literal, alias, null);
            }
            else if (typeSet.Count >= 3 && types.Count > 2)
            {
                int middle = typeSet.Count / 2;
                result = getIntersectionTypeEx([getIntersectionTypeEx(typeSet.GetRange(0, middle), flags, null), getIntersectionTypeEx(typeSet.GetRange(middle, typeSet.Count - middle), flags, null)], flags, alias);
            }
            else
            {
                if (!checkCrossProductUnion(typeSet))
                    return errorType;
                var constituents = getCrossProductIntersections(typeSet, flags);
                Type origin = null;
                if (constituents.Any(t => (t.flags & TypeFlags.Intersection) != 0) && getConstituentCountOfTypes(constituents) > getConstituentCountOfTypes(typeSet))
                    origin = newIntersectionType(ObjectFlags.None, typeSet);
                result = getUnionTypeEx(constituents, UnionReduction.Literal, alias, origin);
            }
        }
        else
        {
            result = newIntersectionType(objectFlags | getPropagatingFlagsOfTypes(types, TypeFlags.Nullable), typeSet);
            result.alias = alias;
        }

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
            return addTypesToIntersection(typeSet, includes, t.Types());
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
    private Type uniqueLiteralType;
    private TypeMapper uniqueLiteralMapper;
    private Type stringNumberSymbolType;
    private readonly record struct PropertiesTypesKey(uint typeId, TypeFlags include, bool includeOrigin);
    private readonly Dictionary<PropertiesTypesKey, Type> propertiesTypes = new();
    private Type getIndexType(Type t) => getIndexTypeEx(t, IndexFlags.None);
    private Type getIndexTypeEx(Type t, IndexFlags indexFlags)
    {
        t = getReducedType(t);
        if ((t.flags & TypeFlags.Substitution) != 0)
            throw unsupported("getIndexTypeEx: source NoInfer substitution");
        if (shouldDeferIndexType(t, indexFlags))
            return getIndexTypeForGenericType(t, indexFlags);
        if ((t.objectFlags & ObjectFlags.Mapped) != 0)
            throw unsupported("getIndexTypeEx: source mapped index");
        if ((t.flags & TypeFlags.Union) != 0)
            return getIntersectionType(t.Types().Select(part => getIndexTypeEx(part, indexFlags)).ToList());
        if ((t.flags & TypeFlags.Intersection) != 0)
            return getUnionType(t.Types().Select(part => getIndexTypeEx(part, indexFlags)).ToList());
        if (t == wildcardType)
            return wildcardType;
        if ((t.flags & TypeFlags.Unknown) != 0)
            return neverType;
        if ((t.flags & (TypeFlags.Any | TypeFlags.Never)) != 0)
            return stringNumberSymbolType ??= getUnionType([stringType, numberType, esSymbolType]);
        var include = ((indexFlags & IndexFlags.NoIndexSignatures) != 0 ? TypeFlags.StringLiteral : TypeFlags.StringLike) | ((indexFlags & IndexFlags.StringsOnly) != 0 ? TypeFlags.None : TypeFlags.NumberLike | TypeFlags.ESSymbolLike);
        return getLiteralTypeFromProperties(t, include, indexFlags == IndexFlags.None);
    }

    private bool shouldDeferIndexType(Type t, IndexFlags indexFlags) => (t.flags & TypeFlags.InstantiableNonPrimitive) != 0 || isGenericTupleType(t) || isGenericMappedType(t) || (t.flags & TypeFlags.Union) != 0 && (indexFlags & IndexFlags.NoReducibleCheck) == 0 && isGenericReducibleType(t) || (t.flags & TypeFlags.Intersection) != 0 && maybeTypeOfKind(t, TypeFlags.Instantiable) && t.Types().Any(IsEmptyAnonymousObjectType);
    private Type getIndexTypeForGenericType(Type t, IndexFlags indexFlags)
    {
        var key = new CachedTypeKey((indexFlags & IndexFlags.StringsOnly) != 0 ? CachedTypeKind.StringIndexType : CachedTypeKind.IndexType, t.id);
        if (cachedTypes.TryGetValue(key, out var result))
            return result;
        cachedTypes[key] = result = newIndexType(t, indexFlags & IndexFlags.StringsOnly);
        return result;
    }

    private Type getLiteralTypeFromProperties(Type t, TypeFlags include, bool includeOrigin)
    {
        var key = new PropertiesTypesKey(t.id, include, includeOrigin);
        if (propertiesTypes.TryGetValue(key, out var cached))
            return cached;
        Type origin = null;
        if (includeOrigin && (t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference)) != 0 || t.alias != null)
            origin = newIndexType(t, IndexFlags.None);
        var props = getPropertiesOfType(t);
        var indexInfos = getIndexInfosOfType(t);
        var types = new List<Type>((props?.Count ?? 0) + (indexInfos?.Count ?? 0));
        foreach (var prop in props ?? [])
            types.Add(getLiteralTypeFromProperty(prop, include, false));
        foreach (var info in indexInfos ?? [])
            if (isKeyTypeIncluded(info.keyType, include))
                types.Add(info.keyType == stringType && (include & TypeFlags.Number) != 0 ? stringOrNumberType ??= getUnionType([stringType, numberType]) : info.keyType);
        var result = getUnionTypeEx(types, UnionReduction.Literal, null, origin);
        propertiesTypes[key] = result;
        return result;
    }

    private bool shouldDeferIndexedAccessType(Type objectType, Type indexType, Node accessNode)
    {
        if (isGenericIndexType(indexType))
            return true;
        if (accessNode != null && !Ast.IsIndexedAccessTypeNode(accessNode))
        {
            if (isGenericTupleType(objectType))
                throw unsupported("shouldDeferIndexedAccessType: source generic tuple");
            return false;
        }

        return isGenericObjectType(objectType) || isGenericReducibleType(objectType);
    }

    private bool isGenericIndexType(Type t) => (getGenericObjectFlags(t) & ObjectFlags.IsGenericIndexType) != 0;
    private bool isGenericReducibleType(Type t) => (t.flags & TypeFlags.Union) != 0 && (t.objectFlags & ObjectFlags.ContainsIntersections) != 0 && t.Types().Any(isGenericReducibleType) || (t.flags & TypeFlags.Intersection) != 0 && isReducibleIntersection(t);
    private bool isReducibleIntersection(Type t)
    {
        var d = t.AsIntersectionType();
        d.uniqueLiteralFilledInstantiation ??= instantiateType(t, uniqueLiteralMapper);
        return getReducedType(d.uniqueLiteralFilledInstantiation) != d.uniqueLiteralFilledInstantiation;
    }

    private Type getUniqueLiteralTypeForTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 ? uniqueLiteralType : t;
    private Type getIndexedAccessTypeOrUndefined(Type objectType, Type indexType, AccessFlags accessFlags, Node accessNode, TypeAlias alias)
    {
        if (alias != null)
            throw unsupported("getIndexedAccessTypeOrUndefined: alias");
        if (objectType == wildcardType || indexType == wildcardType)
            return wildcardType;
        objectType = getReducedType(objectType);
        if (shouldDeferIndexedAccessType(objectType, indexType, accessNode))
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

        if ((indexType.flags & TypeFlags.Nullable) == 0 && isTypeAssignableToKind(indexType, TypeFlags.StringLike | TypeFlags.NumberLike | TypeFlags.ESSymbolLike))
        {
            if ((objectType.flags & (TypeFlags.Any | TypeFlags.Never)) != 0)
                return objectType;
            var info = getApplicableIndexInfo(objectType, indexType) ?? getIndexInfoOfType(objectType, stringType);
            if (info != null)
            {
                if ((accessFlags & AccessFlags.NoIndexSignatures) != 0 && info.keyType != numberType)
                    return null;
                if (accessNode != null && info.keyType == stringType && !isTypeAssignableToKind(indexType, TypeFlags.String | TypeFlags.Number))
                    throw unsupported("getPropertyTypeForIndexType: source nonstatic index diagnostic");
                if ((accessFlags & AccessFlags.Writing) != 0 && info.isReadonly)
                    throw unsupported("getPropertyTypeForIndexType: source index write");
                return (accessFlags & AccessFlags.IncludeUndefined) != 0 ? getUnionType([info.valueType, missingType]) : info.valueType;
            }

            if ((indexType.flags & TypeFlags.Never) != 0)
                return neverType;
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
                return getWriteTypeOfSymbolWithDeferredType(symbol);
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
            {
                return this.isContextSensitiveFunctionLikeDeclaration(node);
            }

            case Kind.ObjectLiteralExpression:
            {
                return Core.Some(node.Properties(), this.isContextSensitive);
            }

            case Kind.ArrayLiteralExpression:
            {
                throw unsupported("isContextSensitive: source ArrayLiteralExpression");
            }

            case Kind.ConditionalExpression:
            {
                return (this.isContextSensitive(node.AsConditionalExpression().WhenTrue) || this.isContextSensitive(node.AsConditionalExpression().WhenFalse));
            }

            case Kind.BinaryExpression:
            {
                var binary = node.AsBinaryExpression();
                return (Ast.NodeKindIs(binary.OperatorToken, Kind.BarBarToken, Kind.QuestionQuestionToken) && ((this.isContextSensitive(binary.Left) || this.isContextSensitive(binary.Right))));
            }

            case Kind.PropertyAssignment:
            {
                return this.isContextSensitive(node.Initializer());
            }

            case Kind.ParenthesizedExpression:
            {
                return this.isContextSensitive(node.Expression());
            }

            case Kind.JsxAttributes:
            {
                throw unsupported("isContextSensitive: source JsxAttributes");
            }

            case Kind.JsxAttribute:
            {
                throw unsupported("isContextSensitive: source JsxAttribute");
            }

            case Kind.JsxExpression:
            {
                throw unsupported("isContextSensitive: source JsxExpression");
            }

            case Kind.YieldExpression:
            {
                throw unsupported("isContextSensitive: source YieldExpression");
            }
        }

        return false;
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
                var op = node.AsPrefixUnaryExpression().Operator;
                var arg = node.AsPrefixUnaryExpression().Operand;
                return op == Kind.MinusToken && arg.Kind is Kind.NumericLiteral or Kind.BigIntLiteral || op == Kind.PlusToken && arg.Kind == Kind.NumericLiteral;
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                var expression = Ast.SkipParentheses(node.Expression());
                Symbol symbol = null;
                if (Ast.IsEntityNameExpression(expression))
                {
                    symbol = resolveEntityName(expression, SymbolFlags.Value, true, false, null);
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
        if ((t.flags & TypeFlags.TypeParameter) != 0)
            return t.symbol != null && (t.symbol.Declarations?.Any(d => Ast.HasSyntacticModifier(d, ModifierFlags.Const)) ?? false);
        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
            return t.Types().Any(s => isConstTypeVariable(s, depth));
        if ((t.flags & (TypeFlags.IndexedAccess | TypeFlags.Conditional | TypeFlags.Substitution)) != 0 || (t.objectFlags & ObjectFlags.Mapped) != 0 || isGenericTupleType(t))
            throw unsupported("isConstTypeVariable: source generic type operator");
        return false;
    }
}

public sealed partial class Checker
{
    private bool allTypesAssignableToKind(Type source, TypeFlags kind) => allTypesAssignableToKindEx(source, kind, false);
    private bool allTypesAssignableToKindEx(Type source, TypeFlags kind, bool strict) => (source.flags & TypeFlags.Union) != 0 ? source.Types().All(t => allTypesAssignableToKindEx(t, kind, strict)) : isTypeAssignableToKindEx(source, kind, strict);
}

public sealed partial class Checker
{
    // checker.go:24194/24200. Enum/class/source generic declarations remain outside admission.
    internal Type getDeclaredTypeOfSymbol(Symbol symbol) => tryGetDeclaredTypeOfSymbol(symbol) ?? errorType;
    private Type tryGetDeclaredTypeOfSymbol(Symbol symbol)
    {
        if ((symbol.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
            return getDeclaredTypeOfClassOrInterface(symbol);
        if ((symbol.Flags & SymbolFlags.TypeAlias) != 0)
            return getDeclaredTypeOfTypeAlias(symbol);
        if ((symbol.Flags & (SymbolFlags.TypeParameter | SymbolFlags.Enum | SymbolFlags.EnumMember | SymbolFlags.Alias)) != 0)
            throw unsupported("tryGetDeclaredTypeOfSymbol: enum/source parameter/module alias");
        return null;
    }

    private Type getDeclaredTypeOfTypeAlias(Symbol symbol)
    {
        var links = typeAliasLinks.Get(symbol);
        if (links.declaredType == null)
        {
            if (!pushTypeResolution(symbol, TypeSystemPropertyName.DeclaredType))
                return errorType;
            var declaration = symbol.Declarations.Find(Ast.IsTypeAliasDeclaration);
            var t = getTypeFromTypeNode(declaration.Type());
            if (popTypeResolution())
            {
                var parameters = getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
                if ((parameters?.Count ?? 0) != 0)
                    throw unsupported("getDeclaredTypeOfTypeAlias: source generic alias");
                if (t == intrinsicMarkerType && symbol.Name == "BuiltinIteratorReturn")
                    throw unsupported("getDeclaredTypeOfTypeAlias: library intrinsic");
            }
            else
            {
                error(declaration.Name() ?? declaration, Diagnostics.Type_alias_0_circularly_references_itself, symbolToString(symbol));
                t = errorType;
            }

            links.declaredType ??= t;
        }

        return links.declaredType;
    }

    private List<Type> getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(Symbol symbol) => appendLocalTypeParametersOfClassOrInterfaceOrTypeAlias(null, symbol);
    private List<Type> appendLocalTypeParametersOfClassOrInterfaceOrTypeAlias(List<Type> types, Symbol symbol)
    {
        foreach (var node in symbol.Declarations ?? [])
            if (node.Kind is Kind.InterfaceDeclaration or Kind.ClassDeclaration or Kind.ClassExpression or Kind.TypeAliasDeclaration)
                types = appendTypeParameters(types, node.TypeParameters());
        return types;
    }

    private List<Type> appendTypeParameters(List<Type> types, List<Node> declarations)
    {
        foreach (var declaration in declarations ?? [])
        {
            var type = getDeclaredTypeOfTypeParameter(getSymbolOfDeclaration(declaration));
            if (!(types?.Contains(type) ?? false))
                types = GoRuntime.Append(types, type);
        }

        return types;
    }

    private Type getDeclaredTypeOfTypeParameter(Symbol symbol)
    {
        var links = declaredTypeLinks.Get(symbol);
        return links.declaredType ??= newTypeParameter(symbol);
    }

    private TypeAlias getAliasForTypeNode(Node node)
    {
        var symbol = getAliasSymbolForTypeNode(node);
        return symbol != null ? new()
        {
            symbol = symbol,
            typeArguments = getTypeArgumentsForAliasSymbol(symbol)
        }

        : null;
    }

    private Symbol getAliasSymbolForTypeNode(Node node)
    {
        var host = node.Parent;
        while (host?.Kind == Kind.ParenthesizedType || host?.Kind == Kind.TypeOperator && host.AsTypeOperatorNode().Operator == Kind.ReadonlyKeyword)
            host = host.Parent;
        return host?.Kind == Kind.TypeAliasDeclaration ? getSymbolOfDeclaration(host) : null;
    }

    private List<Type> getTypeArgumentsForAliasSymbol(Symbol symbol) => symbol != null ? getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol) : null;
    private Type getTypeFromTypeReference(Node node)
    {
        var links = typeNodeLinks.Get(node);
        if (links.resolvedType == null)
        {
            if ((node.Flags & NodeFlags.JSDoc) != 0)
                throw unsupported("getTypeFromTypeReference: JSDoc");
            links.resolvedType = getDistributedTypeParameter(node, getTypeReferenceType(node, getSymbolFromTypeReference(node)));
        }

        return links.resolvedType;
    }

    private Type getDistributedTypeParameter(Node node, Type t)
    {
        if ((t.flags & TypeFlags.TypeParameter) != 0 && !t.AsTypeParameter().isDistributed)
            throw unsupported("getDistributedTypeParameter: source conditional parameter");
        return t;
    }

    private Symbol getSymbolFromTypeReference(Node node)
    {
        var links = symbolNodeLinks.Get(node);
        links.resolvedSymbol ??= resolveTypeReferenceName(node, SymbolFlags.Type, false);
        return links.resolvedSymbol;
    }

    private Symbol resolveTypeReferenceName(Node reference, SymbolFlags meaning, bool ignoreErrors)
    {
        var name = getTypeReferenceName(reference);
        if (name == null)
            return unknownSymbol;
        var symbol = resolveEntityName(name, meaning, ignoreErrors, false, null);
        if (symbol != null && symbol != unknownSymbol)
            return symbol;
        return ignoreErrors ? unknownSymbol : getUnresolvedSymbolForEntityName(name);
    }

    private static Node getTypeReferenceName(Node node) => node.Kind == Kind.TypeReference ? node.AsTypeReferenceNode().TypeName : node.Kind == Kind.ExpressionWithTypeArguments ? node.Expression() : null;
    private Symbol getUnresolvedSymbolForEntityName(Node name)
    {
        var identifier = name.Kind == Kind.QualifiedName ? name.AsQualifiedName().Right : name.Kind == Kind.PropertyAccessExpression ? name.Name() : name;
        string text = identifier.Text();
        if (text != "")
        {
            Symbol parent = name.Kind == Kind.QualifiedName ? getUnresolvedSymbolForEntityName(name.AsQualifiedName().Left) : name.Kind == Kind.PropertyAccessExpression ? getUnresolvedSymbolForEntityName(name.Expression()) : null;
            string path = parent != null ? getSymbolPath(parent) + "." + text : text;
            if (!unresolvedSymbols.TryGetValue(path, out var result))
            {
                result = newSymbolEx(SymbolFlags.TypeAlias, text, CheckFlags.Unresolved);
                unresolvedSymbols[path] = result;
                result.Parent = parent;
                typeAliasLinks.Get(result).declaredType = unresolvedType;
            }

            return result;
        }

        return unknownSymbol;
    }

    private static string getSymbolPath(Symbol symbol) => symbol.Parent != null ? getSymbolPath(symbol.Parent) + "." + symbol.Name : symbol.Name;
    private Type getTypeReferenceType(Node node, Symbol symbol)
    {
        if (symbol == unknownSymbol)
            return errorType;
        if ((symbol.Flags & (SymbolFlags.Class | SymbolFlags.Interface)) != 0)
            return getTypeFromClassOrInterfaceReference(node, symbol);
        if ((symbol.Flags & SymbolFlags.TypeAlias) != 0)
            return getTypeFromTypeAliasReference(node, symbol);
        var t = tryGetDeclaredTypeOfSymbol(symbol);
        return t != null && checkNoTypeArguments(node, symbol) ? getRegularTypeOfLiteralType(t) : errorType;
    }

    private Type getTypeFromTypeAliasReference(Node node, Symbol symbol)
    {
        var arguments = node.TypeArguments();
        if ((symbol.CheckFlags & CheckFlags.Unresolved) != 0)
        {
            var alias = new TypeAlias
            {
                symbol = symbol,
                typeArguments = arguments?.Select(getTypeFromTypeNode).ToList()
            };
            var key = getAliasKey(alias);
            if (!errorTypes.TryGetValue(key, out var t))
            {
                t = newIntrinsicType(TypeFlags.Any, "error");
                t.alias = alias;
                errorTypes[key] = t;
            }

            return t;
        }

        var declared = getDeclaredTypeOfSymbol(symbol);
        if ((typeAliasLinks.Get(symbol).typeParameters?.Count ?? 0) != 0)
            throw unsupported("getTypeFromTypeAliasReference: source generic alias");
        return checkNoTypeArguments(node, symbol) ? declared : errorType;
    }

    private bool checkNoTypeArguments(Node node, Symbol symbol)
    {
        if ((node.TypeArguments()?.Count ?? 0) != 0)
        {
            error(node, Diagnostics.Type_0_is_not_generic, symbol != null ? symbolToString(symbol) : TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(node.AsTypeReferenceNode().TypeName));
            return false;
        }

        return true;
    }

    private void checkTypeAliasDeclaration(Node node)
    {
        checkGrammarModifiers(node);
        checkTypeNameIsReserved(node.Name(), Diagnostics.Type_alias_name_cannot_be_0);
        if (!containerAllowsBlockScopedVariable(node.Parent))
            grammarErrorOnNode(node, Diagnostics.X_0_declarations_can_only_be_declared_inside_a_block, "type");
        checkExportsOnMergedDeclarations(node);
        if ((node.TypeParameters()?.Count ?? 0) != 0)
            throw unsupported("checkTypeAliasDeclaration: source generic");
        if (node.Type()?.Kind == Kind.IntrinsicKeyword)
            throw unsupported("checkTypeAliasDeclaration: library intrinsic");
        checkSourceElement(node.Type());
        registerForUnusedIdentifiersCheck(node);
    }

    private void checkTypeNameIsReserved(Node name, Message message)
    {
        if (name.Text()is "any" or "unknown" or "never" or "number" or "bigint" or "boolean" or "string" or "symbol" or "void" or "object" or "undefined")
            error(name, message, name.Text());
    }

    private void checkTypeReferenceNode(Node node)
    {
        checkGrammarTypeArguments(node, node.TypeArgumentList());
        if (node.TypeArguments() != null)
            throw unsupported("checkTypeReferenceNode: written generic arguments");
        checkSourceElements(node.TypeArguments());
        checkTypeReferenceOrImport(node);
    }

    private void checkTypeReferenceOrImport(Node node)
    {
        var t = getTypeFromTypeNode(node);
        if (!isErrorType(t))
        {
            if ((node.TypeArguments()?.Count ?? 0) != 0)
                throw unsupported("checkTypeReferenceOrImport: written generic constraints");
            var symbol = getResolvedSymbolOrNil(node);
            if (symbol != null && (symbol.Declarations?.Any(IsDeprecatedDeclaration) ?? false))
                throw unsupported("checkTypeReferenceOrImport: tagged JSDoc declaration");
        }
    }
}

public sealed partial class Checker
{
    // checker.go:17706. Nongeneric interfaces may still have a synthetic this parameter.
    internal Type getDeclaredTypeOfClassOrInterface(Symbol symbol)
    {
        var links = declaredTypeLinks.Get(symbol);
        if (links.declaredType == null)
        {
            if ((symbol.Flags & SymbolFlags.Class) != 0)
                throw unsupported("getDeclaredTypeOfClassOrInterface: source class");
            var kind = ObjectFlags.Interface;
            var t = newObjectType(kind, symbol);
            links.declaredType = t;
            var outerParameters = getOuterTypeParametersOfClassOrInterface(symbol);
            var parameters = appendLocalTypeParametersOfClassOrInterfaceOrTypeAlias(outerParameters, symbol);
            if (parameters != null || !isThislessInterface(symbol))
            {
                t.objectFlags |= ObjectFlags.Reference;
                var d = t.AsInterfaceType();
                d.thisType = newTypeParameter(symbol);
                d.thisType.AsTypeParameter().isThisType = true;
                d.thisType.AsTypeParameter().constraint = t;
                d.allTypeParameters = GoRuntime.Append(parameters, d.thisType);
                d.outerTypeParameterCount = outerParameters?.Count ?? 0;
                d.resolvedTypeArguments = d.TypeParameters();
                d.instantiations = new();
                d.instantiations[getTypeListKey(d.resolvedTypeArguments)] = t;
                d.target = t;
            }
        }

        return links.declaredType;
    }

    private bool isThislessInterface(Symbol symbol)
    {
        foreach (var declaration in symbol.Declarations ?? [])
            if (Ast.IsInterfaceDeclaration(declaration))
            {
                if ((declaration.Flags & NodeFlags.ContainsThis) != 0)
                    return false;
                foreach (var node in Ast.GetExtendsHeritageClauseElements(declaration) ?? [])
                {
                    var name = Ast.GetHeritageClauseElementName(node);
                    if (Ast.IsEntityName(name) || Ast.IsEntityNameExpression(name))
                    {
                        var baseSymbol = resolveEntityName(name, SymbolFlags.Type, true, false, null);
                        if (baseSymbol == null || (baseSymbol.Flags & SymbolFlags.Interface) == 0 || getDeclaredTypeOfClassOrInterface(baseSymbol).AsInterfaceType().thisType != null)
                            return false;
                    }
                }
            }

        return true;
    }

    private List<Type> getOuterTypeParametersOfClassOrInterface(Symbol symbol)
    {
        var declaration = getClassOrInterfaceLikeDeclaration(symbol);
        Debug.Assert(declaration != null);
        return getOuterTypeParameters(declaration, false);
    }

    private Node getClassOrInterfaceLikeDeclaration(Symbol symbol)
    {
        if ((symbol.Flags & (SymbolFlags.Class | SymbolFlags.Function)) != 0)
            return symbol.ValueDeclaration;
        return Core.Find(symbol.Declarations, declaration =>
        {
            if (Ast.IsInterfaceDeclaration(declaration))
                return true;
            if (!Ast.IsVariableDeclaration(declaration))
                return false;
            var initializer = declaration.Initializer();
            return initializer != null && Ast.IsFunctionExpressionOrArrowFunction(initializer);
        });
    }

    private Type newTypeParameter(Symbol symbol)
    {
        var t = newType(TypeFlags.TypeParameter, ObjectFlags.None, new TypeParameter());
        t.symbol = symbol;
        return t;
    }

    private Type getTypeFromClassOrInterfaceReference(Node node, Symbol symbol)
    {
        var t = getDeclaredTypeOfClassOrInterface(getMergedSymbol(symbol));
        if ((t.AsInterfaceType().LocalTypeParameters()?.Count ?? 0) != 0)
            throw unsupported("getTypeFromClassOrInterfaceReference: source generic");
        return checkNoTypeArguments(node, symbol) ? t : errorType;
    }

    private Type createTypeReference(Type target, List<Type> arguments) => createTypeReferenceEx(target, arguments, ObjectFlags.None);
    private Type createTypeReferenceEx(Type target, List<Type> arguments, ObjectFlags objectFlags)
    {
        var key = getTypeListKey(arguments);
        var data = target.AsInterfaceType();
        if (data.instantiations.TryGetValue(key, out var cached))
            return cached;
        var t = newObjectType(ObjectFlags.Reference | objectFlags | getPropagatingFlagsOfTypes(arguments, TypeFlags.None), target.symbol);
        var d = t.AsTypeReference();
        d.target = target;
        d.resolvedTypeArguments = arguments;
        data.instantiations[key] = t;
        return t;
    }

    internal List<Type> getTypeArguments(Type t)
    {
        var d = t.AsTypeReference();
        if (d.resolvedTypeArguments == null)
        {
            var n = d.target.AsInterfaceType();
            if (!pushTypeResolution(t, TypeSystemPropertyName.ResolvedTypeArguments))
                return Enumerable.Repeat(errorType, n.TypeParameters()?.Count ?? 0).ToList();
            List<Type> arguments = null;
            if (d.node != null)
                throw unsupported("getTypeArguments: deferred explicit source generic/tuple/array");
            if (popTypeResolution())
                d.resolvedTypeArguments ??= instantiateTypes(arguments, d.mapper);
            else
            {
                d.resolvedTypeArguments ??= Enumerable.Repeat(errorType, n.TypeParameters()?.Count ?? 0).ToList();
                error(d.node ?? currentNode, Diagnostics.Type_arguments_for_0_circularly_reference_themselves, symbolToString(d.target.symbol));
            }
        }

        return d.resolvedTypeArguments;
    }

    private static Type getTargetType(Type t) => (t.objectFlags & ObjectFlags.Reference) != 0 ? t.Target() : t;
    internal Type getTypeWithThisArgument(Type t, Type thisArgument, bool needApparentType)
    {
        if ((t.objectFlags & ObjectFlags.Reference) != 0)
        {
            var target = t.Target();
            var arguments = getTypeArguments(t);
            if ((target.AsInterfaceType().TypeParameters()?.Count ?? 0) == (arguments?.Count ?? 0))
                return createTypeReference(target, GoRuntime.Append(arguments, thisArgument ?? target.AsInterfaceType().thisType));
            return t;
        }

        if ((t.flags & TypeFlags.Intersection) != 0)
        {
            var types = t.Types();
            var mapped = types.Select(x => getTypeWithThisArgument(x, thisArgument, needApparentType)).ToList();
            if (mapped.SequenceEqual(types))
                return t;
            return getIntersectionType(mapped);
        }

        return needApparentType ? getApparentType(t) : t;
    }

    internal List<Type> getBaseTypes(Type t)
    {
        if ((t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Tuple)) == 0)
            return null;
        var d = t.AsInterfaceType();
        if (!d.baseTypesResolved)
        {
            if (!pushTypeResolution(t, TypeSystemPropertyName.ResolvedBaseTypes))
                return d.resolvedBaseTypes;
            if ((t.objectFlags & ObjectFlags.Tuple) != 0 || (t.symbol.Flags & SymbolFlags.Class) != 0)
                throw unsupported("getBaseTypes: tuple/class");
            if ((t.symbol.Flags & SymbolFlags.Interface) != 0)
                resolveBaseTypesOfInterface(t);
            else
                throw new InvalidOperationException("Unhandled case in getBaseTypes");
            if (!popTypeResolution() && t.symbol.Declarations != null)
                foreach (var declaration in t.symbol.Declarations)
                    if (Ast.IsInterfaceDeclaration(declaration))
                        reportCircularBaseType(declaration, t);
            t.objectFlags &= ~ObjectFlags.MembersResolved;
            d.baseTypesResolved = true;
        }

        return d.resolvedBaseTypes;
    }

    private void resolveBaseTypesOfInterface(Type t)
    {
        var d = t.AsInterfaceType();
        foreach (var declaration in t.symbol.Declarations ?? [])
            if (Ast.IsInterfaceDeclaration(declaration))
                foreach (var node in Ast.GetExtendsHeritageClauseElements(declaration) ?? [])
                {
                    var @base = getReducedType(getTypeFromTypeNode(node));
                    if (!isErrorType(@base))
                    {
                        if (isValidBaseType(@base))
                        {
                            if (t != @base && !hasBaseType(@base, t))
                                d.resolvedBaseTypes = GoRuntime.Append(d.resolvedBaseTypes, @base);
                            else
                                reportCircularBaseType(declaration, t);
                        }
                        else
                            error(node, Diagnostics.An_interface_can_only_extend_an_object_type_or_intersection_of_object_types_with_statically_known_members);
                    }
                }
    }

    private bool hasBaseType(Type t, Type checkBase)
    {
        if ((t.objectFlags & (ObjectFlags.ClassOrInterface | ObjectFlags.Reference)) != 0)
        {
            var target = getTargetType(t);
            return target == checkBase || (getBaseTypes(target)?.Any(x => hasBaseType(x, checkBase)) ?? false);
        }

        return (t.flags & TypeFlags.Intersection) != 0 && t.Types().Any(x => hasBaseType(x, checkBase));
    }

    private void reportCircularBaseType(Node node, Type t) => error(node, Diagnostics.Type_0_recursively_references_itself_as_a_base_type, typeToStringWriteArrayAsGenericType(t));
    private bool isValidBaseType(Type t)
    {
        if ((t.flags & TypeFlags.TypeParameter) != 0)
        {
            var constraint = getBaseConstraintOfType(t);
            if (constraint != null)
                return isValidBaseType(constraint);
        }

        return (t.flags & (TypeFlags.Object | TypeFlags.NonPrimitive | TypeFlags.Any)) != 0 && (t.objectFlags & ObjectFlags.Mapped) == 0 || (t.flags & TypeFlags.Intersection) != 0 && t.Types().All(isValidBaseType);
    }

    private InterfaceType resolveDeclaredMembers(Type t)
    {
        var d = t.AsInterfaceType();
        if (!d.declaredMembersResolved)
        {
            var members = getMembersOfSymbol(t.symbol);
            d.declaredMembersResolved = true;
            d.declaredMembers = members;
            d.declaredCallSignatures = getSignaturesOfSymbol(d.declaredMembers?[Ast.InternalSymbolNameCall]);
            d.declaredConstructSignatures = getSignaturesOfSymbol(d.declaredMembers?[Ast.InternalSymbolNameNew]);
            d.declaredIndexInfos = getIndexInfosOfSymbol(t.symbol);
        }

        return d;
    }

    private List<IndexInfo> getIndexInfosOfSymbol(Symbol symbol)
    {
        if (getMembersOfSymbol(symbol)?[Ast.InternalSymbolNameIndex] != null)
            throw unsupported("getIndexInfosOfSymbol: source index signature");
        return null;
    }

    private void resolveClassOrInterfaceMembers(Type t) => resolveObjectTypeMembers(t, t, null, null);
    private void resolveTypeReferenceMembers(Type t)
    {
        var source = t.Target();
        var parameters = source.AsInterfaceType().allTypeParameters;
        var arguments = getTypeArguments(t);
        var padded = arguments;
        if ((arguments?.Count ?? 0) == (parameters?.Count ?? 0) - 1)
            padded = GoRuntime.Append(arguments, t);
        resolveObjectTypeMembers(t, source, parameters, padded);
    }

    private void resolveObjectTypeMembers(Type t, Type source, List<Type> parameters, List<Type> arguments)
    {
        TypeMapper mapper = null;
        SymbolTable members;
        List<Signature> call, construct;
        List<IndexInfo> indexes;
        bool instantiated = false;
        var d = resolveDeclaredMembers(source);
        if ((parameters ?? []).SequenceEqual(arguments ?? []))
        {
            members = d.declaredMembers;
            call = d.declaredCallSignatures;
            construct = d.declaredConstructSignatures;
            indexes = d.declaredIndexInfos;
        }
        else
        {
            instantiated = true;
            mapper = newTypeMapper(parameters, arguments);
            members = instantiateSymbolTable(d.declaredMembers, mapper);
            call = instantiateSignatures(d.declaredCallSignatures, mapper);
            construct = instantiateSignatures(d.declaredConstructSignatures, mapper);
            indexes = instantiateIndexInfos(d.declaredIndexInfos, mapper);
        }

        var bases = getBaseTypes(source);
        if ((bases?.Count ?? 0) > 0)
        {
            if (!instantiated)
                members = members != null ? cloneSymbolTable(members) : null;
            var thisArgument = arguments is { Count: > 0 } ? arguments[^1] : null;
            foreach (var @base in bases)
            {
                var inherited = thisArgument != null ? getTypeWithThisArgument(instantiateType(@base, mapper), thisArgument, false) : @base;
                members = addInheritedMembers(members, getPropertiesOfType(inherited));
                call = GoRuntime.Append(call, getSignaturesOfType(inherited, SignatureKind.Call) ?? []);
                construct = GoRuntime.Append(construct, getSignaturesOfType(inherited, SignatureKind.Construct) ?? []);
                foreach (var info in inherited != anyType ? getIndexInfosOfType(inherited) ?? [] : new List<IndexInfo>
                {
                    anyBaseTypeIndexInfo
                }

                )
                    if (!(indexes?.Any(x => x.keyType == info.keyType) ?? false))
                        indexes = GoRuntime.Append(indexes, info);
            }
        }

        setStructuredTypeMembers(t, members, call, construct, indexes);
    }

    private SymbolTable addInheritedMembers(SymbolTable symbols, List<Symbol> bases)
    {
        foreach (var @base in bases ?? [])
            if (!isStaticPrivateIdentifierProperty(@base) && (symbols?[@base.Name] == null || (symbols[@base.Name].Flags & SymbolFlags.Value) == 0))
            {
                symbols ??= new();
                symbols[@base.Name] = @base;
            }

        return symbols;
    }
}

public sealed partial class Checker
{
    private void checkInterfaceDeclaration(Node node)
    {
        if (!checkGrammarModifiers(node))
            checkGrammarInterfaceDeclaration(node.AsInterfaceDeclaration());
        if (!containerAllowsBlockScopedVariable(node.Parent))
            grammarErrorOnNode(node, Diagnostics.X_0_declarations_can_only_be_declared_inside_a_block, "interface");
        if ((node.TypeParameters()?.Count ?? 0) != 0)
            throw unsupported("checkInterfaceDeclaration: source parameters");
        checkTypeNameIsReserved(node.Name(), Diagnostics.Interface_name_cannot_be_0);
        checkExportsOnMergedDeclarations(node);
        var symbol = getSymbolOfDeclaration(node);
        checkTypeParameterListsIdentical(symbol);
        var links = declaredTypeLinks.Get(symbol);
        if (!links.interfaceChecked)
        {
            links.interfaceChecked = true;
            var first = Ast.GetDeclarationOfKind(symbol, Kind.InterfaceDeclaration);
            var t = getDeclaredTypeOfSymbol(symbol);
            var typeWithThis = getTypeWithThisArgument(t, null, false);
            if (checkInheritedPropertiesAreIdentical(t, first.Name()))
            {
                foreach (var @base in getBaseTypes(t) ?? [])
                    checkTypeAssignableTo(typeWithThis, getTypeWithThisArgument(@base, t.AsInterfaceType().thisType, false), first.Name(), Diagnostics.Interface_0_incorrectly_extends_interface_1);
                checkIndexConstraints(t, symbol, false);
            }
        }

        checkObjectTypeForDuplicateDeclarations(node, false);
        foreach (var element in Ast.GetExtendsHeritageClauseElements(node) ?? [])
        {
            if (element.Kind == Kind.ExpressionWithTypeArguments)
            {
                var expression = element.Expression();
                if (!Ast.IsEntityNameExpression(expression) || Ast.IsOptionalChain(expression))
                    error(expression, Diagnostics.An_interface_can_only_extend_an_identifier_Slashqualified_name_with_optional_type_arguments);
            }

            checkTypeReferenceNode(element);
        }

        checkSourceElements(node.Members());
        checkClassOrInterfaceForDuplicateIndexSignatures(node);
        registerForUnusedIdentifiersCheck(node);
    }

    private void checkTypeParameterListsIdentical(Symbol symbol)
    {
        if ((symbol.Declarations?.Count ?? 0) == 1)
            return;
        var links = declaredTypeLinks.Get(symbol);
        if (!links.typeParametersChecked)
        {
            links.typeParametersChecked = true;
            var declarations = symbol.Declarations.Where(Ast.IsInterfaceDeclaration).ToList();
            if (declarations.Count <= 1)
                return;
            var t = getDeclaredTypeOfSymbol(symbol);
            if (!areTypeParametersIdentical(declarations, t.AsInterfaceType().LocalTypeParameters()))
                foreach (var declaration in declarations)
                    error(declaration.Name(), Diagnostics.All_declarations_of_0_must_have_identical_type_parameters, symbolToString(symbol));
        }
    }

    private bool areTypeParametersIdentical(List<Node> declarations, List<Type> parameters)
    {
        int maximum = parameters?.Count ?? 0;
        if (maximum != 0)
            throw unsupported("areTypeParametersIdentical: source parameters");
        foreach (var declaration in declarations)
        {
            int length = declaration.TypeParameters()?.Count ?? 0;
            if (length < maximum || length > maximum)
                return false;
        }

        return true;
    }

    private sealed record InheritanceInfo(Symbol prop, Type containingType);
    private bool checkInheritedPropertiesAreIdentical(Type t, Node node)
    {
        var bases = getBaseTypes(t);
        if ((bases?.Count ?? 0) < 2)
            return true;
        var seen = new Dictionary<string, InheritanceInfo>(StringComparer.Ordinal);
        foreach (var(name, property)in resolveDeclaredMembers(t).declaredMembers ?? new SymbolTable())
            if (isNamedMember(property, name))
                seen[property.Name] = new(property, t);
        bool identical = true;
        foreach (var @base in bases)
            foreach (var property in getPropertiesOfType(getTypeWithThisArgument(@base, t.AsInterfaceType().thisType, false)) ?? [])
            {
                if (!seen.TryGetValue(property.Name, out var existing))
                    seen[property.Name] = new(property, @base);
                else if (existing.containingType != t && !isPropertyIdenticalTo(existing.prop, property))
                {
                    identical = false;
                    var name1 = typeToStringByteString(existing.containingType);
                    var name2 = typeToStringByteString(@base);
                    var info = NewDiagnosticForNode(node, Diagnostics.Named_property_0_of_types_1_and_2_are_not_identical, symbolToString(property), name1, name2);
                    addDiagnostic(Ast.NewDiagnosticChain(info, Diagnostics.Interface_0_cannot_simultaneously_extend_types_1_and_2, typeToStringByteString(t), name1, name2));
                }
            }

        return identical;
    }

    private bool isPropertyIdenticalTo(Symbol source, Symbol target) => compareProperties(source, target, compareTypesIdentical) != Ternary.False;
    private void checkClassOrInterfaceForDuplicateIndexSignatures(Node node)
    {
        var links = declaredTypeLinks.Get(getSymbolOfDeclaration(node));
        if (!links.indexSignaturesChecked)
        {
            links.indexSignaturesChecked = true;
            checkTypeForDuplicateIndexSignatures(node);
        }
    }

    private void checkUnionOrIntersectionType(Node node)
    {
        node.ForEachChild(child =>
        {
            checkSourceElement(child);
            return false;
        });
        getTypeFromTypeNode(node);
    }

    private Type getTypeFromUnionTypeNode(Node node)
    {
        var links = typeNodeLinks.Get(node);
        return links.resolvedType ??= getUnionTypeEx(node.AsUnionTypeNode().Types.Nodes.Select(getTypeFromTypeNode).ToList(), UnionReduction.Literal, getAliasForTypeNode(node), null);
    }

    private Type getTypeFromIntersectionTypeNode(Node node)
    {
        var links = typeNodeLinks.Get(node);
        if (links.resolvedType == null)
        {
            var types = node.AsIntersectionTypeNode().Types.Nodes.Select(getTypeFromTypeNode).ToList();
            bool noSupertypeReduction = types.Count == 2 && types.IndexOf(emptyTypeLiteralType) >= 0 && (types[1 - types.IndexOf(emptyTypeLiteralType)].flags & (TypeFlags.String | TypeFlags.Number | TypeFlags.BigInt)) != 0;
            links.resolvedType = getIntersectionTypeEx(types, noSupertypeReduction ? IntersectionFlags.NoSupertypeReduction : IntersectionFlags.None, getAliasForTypeNode(node));
        }

        return links.resolvedType;
    }
}

public sealed partial class Checker
{
    internal Type noConstraintType, circularConstraintType;
    private int instantiationCount;
    public int TotalInstantiationCount { get; private set; }

    private readonly List<Type> instantiationStack = new();
    private readonly List<TypeMapper> activeMappers = new();
    private readonly List<Dictionary<CacheHashKey, Type>> activeTypeMappersCaches = new();
    internal Type instantiateType(Type t, TypeMapper mapper) => instantiateTypeWithAlias(t, mapper, null);
    private Type instantiateTypeWithAlias(Type t, TypeMapper mapper, TypeAlias alias)
    {
        if (t == null || mapper == null || !(couldContainTypeVariables(t) || t.alias != null && (t.alias.typeArguments?.Any(couldContainTypeVariables) ?? false)))
            return t;
        if (instantiationStack.Count == 100 || instantiationCount >= 5_000_000)
        {
            var names = getCircularTypeNames();
            if (names.Count == 1)
                error(currentNode, Diagnostics.Instantiations_of_type_0_appear_infinitely_circular, names[0]);
            else if (names.Count > 1)
                error(currentNode, Diagnostics.Instantiations_of_the_following_types_appear_infinitely_circular_Colon_0, string.Join(", ", names.Select(n => "'" + n + "'")));
            else
                error(currentNode, Diagnostics.Type_instantiation_is_excessively_deep_and_possibly_infinite);
            return errorType;
        }

        int index = activeMappers.FindLastIndex(m => m == mapper);
        if (index == -1)
            pushActiveMapper(mapper);
        var b = new keyBuilder();
        b.writeType(t);
        b.writeAlias(alias);
        var key = b.hash();
        var cache = activeTypeMappersCaches[index != -1 ? index : activeTypeMappersCaches.Count - 1];
        if (cache.TryGetValue(key, out var cached))
            return cached;
        TotalInstantiationCount++;
        instantiationCount++;
        instantiationStack.Add(t);
        var result = instantiateTypeWorker(t, mapper, alias);
        if (index == -1)
            popActiveMapper();
        else
            cache[key] = result;
        instantiationStack.RemoveAt(instantiationStack.Count - 1);
        return result;
    }

    private List<string> getCircularTypeNames()
    {
        var counts = new Dictionary<Type, int>();
        List<string> names = new();
        foreach (var t in instantiationStack)
        {
            counts.TryGetValue(t, out var count);
            counts[t] = ++count;
            if (count == 3)
            {
                var symbol = t.alias?.symbol ?? t.symbol;
                if (symbol != null && symbol.Name.Length != 0 && symbol.Name[0] != '\xFE')
                {
                    var name = symbolToString(symbol);
                    if (!names.Contains(name))
                        names.Add(name);
                }
            }
        }

        return names;
    }

    private void pushActiveMapper(TypeMapper mapper)
    {
        activeMappers.Add(mapper);
        activeTypeMappersCaches.Add(new());
    }

    private void popActiveMapper()
    {
        activeMappers.RemoveAt(activeMappers.Count - 1);
        activeTypeMappersCaches[^1].Clear();
        activeTypeMappersCaches.RemoveAt(activeTypeMappersCaches.Count - 1);
    }

    private bool couldContainTypeVariables(Type t) => couldContainTypeVariablesWorker(t);
    private bool couldContainTypeVariablesWorker(Type t)
    {
        if ((t.flags & TypeFlags.StructuredOrInstantiable) == 0)
            return false;
        var flags = t.objectFlags;
        if ((flags & ObjectFlags.CouldContainTypeVariablesComputed) != 0)
            return (flags & ObjectFlags.CouldContainTypeVariables) != 0;
        bool result = (t.flags & TypeFlags.Instantiable) != 0 || (t.flags & TypeFlags.Object) != 0 && !isNonGenericTopLevelType(t) && ((flags & ObjectFlags.Reference) != 0 && (t.AsTypeReference().node != null || (getTypeArguments(t)?.Any(couldContainTypeVariables) ?? false)) || (flags & ObjectFlags.Anonymous) != 0 && t.symbol != null && (t.symbol.Flags & (SymbolFlags.Function | SymbolFlags.Method | SymbolFlags.Class | SymbolFlags.TypeLiteral | SymbolFlags.ObjectLiteral)) != 0 && t.symbol.Declarations != null || (flags & (ObjectFlags.Mapped | ObjectFlags.ReverseMapped | ObjectFlags.ObjectRestType | ObjectFlags.InstantiationExpressionType)) != 0) || (t.flags & TypeFlags.UnionOrIntersection) != 0 && (t.flags & TypeFlags.EnumLiteral) == 0 && !isNonGenericTopLevelType(t) && t.Types().Any(couldContainTypeVariables);
        t.objectFlags |= ObjectFlags.CouldContainTypeVariablesComputed | (result ? ObjectFlags.CouldContainTypeVariables : 0);
        return result;
    }

    private bool isNonGenericTopLevelType(Type t)
    {
        if (t.alias != null && (t.alias.typeArguments?.Count ?? 0) == 0)
        {
            var declaration = Ast.GetDeclarationOfKind(t.alias.symbol, Kind.TypeAliasDeclaration);
            return declaration?.Parent.Kind == Kind.SourceFile;
        }

        return false;
    }

    private Type instantiateTypeWorker(Type t, TypeMapper mapper, TypeAlias alias)
    {
        if ((t.flags & TypeFlags.TypeParameter) != 0)
            return getMappedType(t, mapper);
        if ((t.flags & TypeFlags.Object) != 0)
        {
            if ((t.objectFlags & (ObjectFlags.Reference | ObjectFlags.Anonymous | ObjectFlags.Mapped)) != 0)
            {
                if ((t.objectFlags & ObjectFlags.Reference) != 0 && t.AsTypeReference().node == null)
                {
                    var arguments = t.AsTypeReference().resolvedTypeArguments;
                    var mapped = instantiateTypes(arguments, mapper);
                    return mapped == arguments ? t : createTypeReference(t.Target(), mapped);
                }

                if ((t.objectFlags & ObjectFlags.ReverseMapped) != 0)
                    throw unsupported("instantiateTypeWorker: reverse mapped");
                return getObjectTypeInstantiation(t, mapper, alias);
            }

            return t;
        }

        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
        {
            var source = t;
            if ((t.flags & TypeFlags.Union) != 0 && t.AsUnionType().origin is { } origin && (origin.flags & TypeFlags.UnionOrIntersection) != 0)
                source = origin;
            var types = source.Types();
            var mapped = instantiateTypes(types, mapper);
            if (mapped == types && alias?.symbol == t.alias?.symbol)
                return t;
            alias ??= instantiateTypeAlias(t.alias, mapper);
            return (source.flags & TypeFlags.Intersection) != 0 ? getIntersectionTypeEx(mapped, IntersectionFlags.None, alias) : getUnionTypeEx(mapped, UnionReduction.Literal, alias, null);
        }

        if ((t.flags & TypeFlags.Instantiable) != 0)
            throw unsupported("instantiateTypeWorker: source indexed/template/conditional/substitution");
        return t;
    }

    private Type getObjectTypeInstantiation(Type t, TypeMapper m, TypeAlias alias)
    {
        Node declaration = default;
        Type target = default;
        List<Type> typeParameters = default;
        switch (true)
        {
            case true when ((t.objectFlags & ObjectFlags.Reference) != 0):
            {
                declaration = t.AsTypeReference().node;
                break;
            }

            case true when ((t.objectFlags & ObjectFlags.InstantiationExpressionType) != 0):
            {
                throw unsupported("getObjectTypeInstantiation: source instantiation expression");
            }

            default:
            {
                declaration = t.symbol.Declarations[0];
                break;
            }
        }

        var links = this.typeNodeLinks.Get(declaration);
        switch (true)
        {
            case true when ((t.objectFlags & ObjectFlags.Reference) != 0):
            {
                target = links.resolvedType;
                break;
            }

            case true when ((t.objectFlags & ObjectFlags.Instantiated) != 0):
            {
                target = t.Target();
                break;
            }

            default:
            {
                target = t;
                break;
            }
        }

        typeParameters = links.outerTypeParameters;
        if ((typeParameters == null))
        {
            typeParameters = this.getOuterTypeParameters(declaration, true);
            if ((GoRuntime.Len(target.alias?.TypeArguments()) == 0))
            {
                if (((t.objectFlags & ((ObjectFlags.Reference | ObjectFlags.InstantiationExpressionType))) != 0))
                {
                    typeParameters = Core.Filter(typeParameters, (tp) =>
                    {
                        return this.isTypeParameterPossiblyReferenced(tp, declaration);
                    });
                }
                else
                {
                    if (((target.symbol.Flags & ((SymbolFlags.Method | SymbolFlags.TypeLiteral))) != 0))
                    {
                        typeParameters = Core.Filter(typeParameters, (tp2) =>
                        {
                            return Core.Some(t.symbol.Declarations, (d) =>
                            {
                                return this.isTypeParameterPossiblyReferenced(tp2, d);
                            });
                        });
                    }
                }
            }

            if ((typeParameters == null))
            {
                typeParameters = new List<Type>
                {
                };
            }

            links.outerTypeParameters = typeParameters;
        }

        if ((GoRuntime.Len(typeParameters) == 0))
        {
            return t;
        }

        var typeArguments = GoRuntime.MakeSlice<Type>(GoRuntime.Len(typeParameters));
        {
            foreach (var range1 in GoRuntime.Range(typeParameters))
            {
                var i = range1.Key;
                var tp3 = range1.Value;
                typeArguments[i] = this.mapTypeWithCompositeMapper(tp3, t.Mapper(), m);
            }
        }

        var newAlias = alias;
        if ((newAlias == null))
        {
            newAlias = this.instantiateTypeAlias(t.alias, m);
        }

        var data = target.AsObjectType();
        var key = getTypeInstantiationKey(typeArguments, newAlias, ((t.objectFlags & ObjectFlags.SingleSignatureType) != 0));
        if ((data.instantiations == null))
        {
            data.instantiations = new Dictionary<CacheHashKey, Type>();
            data.instantiations[getTypeInstantiationKey(typeParameters, target.alias, false)] = target;
        }

        var result = data.instantiations.GetValueOrDefault(key);
        if ((result == null))
        {
            var newMapper = newTypeMapper(typeParameters, typeArguments);
            if ((((target.objectFlags & ObjectFlags.SingleSignatureType) != 0) && (m != null)))
            {
                newMapper = this.combineTypeMappers(newMapper, m);
            }

            switch (true)
            {
                case true when ((target.objectFlags & ObjectFlags.Reference) != 0):
                {
                    result = this.createDeferredTypeReference(t.Target(), t.AsTypeReference().node, newMapper, newAlias);
                    break;
                }

                case true when ((target.objectFlags & ObjectFlags.Mapped) != 0):
                {
                    throw unsupported("getObjectTypeInstantiation: mapped type");
                }

                default:
                {
                    result = this.instantiateAnonymousType(target, newMapper, newAlias);
                    break;
                }
            }

            data.instantiations[key] = result;
            if ((((result.flags & TypeFlags.ObjectFlagsType) != 0) && ((result.objectFlags & ObjectFlags.CouldContainTypeVariablesComputed) == 0)))
            {
                var resultCouldContainObjectFlags = Core.Some(typeArguments, this.couldContainTypeVariables);
                if (((result.objectFlags & ObjectFlags.CouldContainTypeVariablesComputed) == 0))
                {
                    if (((result.objectFlags & (((ObjectFlags.Mapped | ObjectFlags.Anonymous) | ObjectFlags.Reference))) != 0))
                    {
                        result.objectFlags |= (ObjectFlags.CouldContainTypeVariablesComputed | Core.IfElse(resultCouldContainObjectFlags, ObjectFlags.CouldContainTypeVariables, ObjectFlags.None));
                    }
                    else
                    {
                        result.objectFlags |= Core.IfElse(!resultCouldContainObjectFlags, ObjectFlags.CouldContainTypeVariablesComputed, ObjectFlags.None);
                    }
                }
            }
        }

        return result;
    }

    private bool isTypeParameterPossiblyReferenced(Type tp, Node node)
    {
        Func<Node, bool> containsReference = default;
        containsReference = (node2) =>
        {
            switch (node2.Kind)
            {
                case Kind.ThisType:
                {
                    return tp.AsTypeParameter().isThisType;
                }

                case Kind.TypeReference:
                {
                    if (((!tp.AsTypeParameter().isThisType && (GoRuntime.Len(node2.TypeArguments()) == 0)) && (this.getSymbolFromTypeReference(node2) == tp.symbol)))
                    {
                        return true;
                    }

                    break;
                }

                case Kind.TypeQuery:
                {
                    var entityName = node2.AsTypeQueryNode().ExprName;
                    var firstIdentifier = Ast.GetFirstIdentifier(entityName);
                    if (!Ast.IsThisIdentifier(firstIdentifier))
                    {
                        var firstIdentifierSymbol = this.getResolvedSymbol(firstIdentifier);
                        var tpDeclaration = tp.symbol.Declarations[0];
                        Node tpScope = default;
                        switch (true)
                        {
                            case true when Ast.IsTypeParameterDeclaration(tpDeclaration):
                            {
                                tpScope = tpDeclaration.Parent;
                                break;
                            }

                            case true when tp.AsTypeParameter().isThisType:
                            {
                                tpScope = tpDeclaration;
                                break;
                            }
                        }

                        if ((tpScope != null))
                        {
                            return (Core.Some(firstIdentifierSymbol.Declarations, (d) =>
                            {
                                return isNodeDescendantOf(d, tpScope);
                            }) || Core.Some(node2.TypeArguments(), containsReference));
                        }
                    }

                    return true;
                }

                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                {
                    var returnType = node2.Type();
                    return (((((returnType == null) && (node2.Body() != null)) || Core.Some(node2.TypeParameters(), containsReference)) || Core.Some(node2.Parameters(), containsReference)) || ((returnType != null) && containsReference(returnType)));
                }
            }

            return node2.ForEachChild(n => containsReference(n));
        };
        if (((tp.symbol != null) && (GoRuntime.Len(tp.symbol.Declarations) == 1)))
        {
            var container = tp.symbol.Declarations[0].Parent;
            {
                var n = node;
                for (; (n != container); n = n.Parent)
                {
                    if ((((n == null) || Ast.IsBlock(n)) || (Ast.IsConditionalTypeNode(n) && containsReference(n.AsConditionalTypeNode().ExtendsType))))
                    {
                        return true;
                    }
                }
            }

            return containsReference(node);
        }

        return true;
    }

    private TypeAlias instantiateTypeAlias(TypeAlias alias, TypeMapper mapper)
    {
        if (alias == null)
            return null;
        var arguments = instantiateTypes(alias.typeArguments, mapper);
        return arguments == alias.typeArguments ? alias : new()
        {
            symbol = alias.symbol,
            typeArguments = arguments
        };
    }

    private List<T> instantiateList<T>(List<T> values, TypeMapper mapper, Func<T, TypeMapper, T> instantiate)
        where T : class
    {
        for (int i = 0; i < (values?.Count ?? 0); i++)
        {
            var mapped = instantiate(values[i], mapper);
            if (mapped != values[i])
            {
                var result = new List<T>(values);
                result[i] = mapped;
                for (int j = i + 1; j < values.Count; j++)
                    result[j] = instantiate(values[j], mapper);
                return result;
            }
        }

        return values;
    }

    private List<Type> instantiateTypes(List<Type> values, TypeMapper mapper) => instantiateList(values, mapper, instantiateType);
    private List<Signature> instantiateSignatures(List<Signature> values, TypeMapper mapper) => instantiateList(values, mapper, instantiateSignature);
    private List<IndexInfo> instantiateIndexInfos(List<IndexInfo> values, TypeMapper mapper) => instantiateList(values, mapper, instantiateIndexInfo);
    private SymbolTable instantiateSymbolTable(SymbolTable symbols, TypeMapper mapper)
    {
        if ((symbols?.Count ?? 0) == 0)
            return null;
        var result = new SymbolTable(symbols.Count);
        foreach (var(name, symbol)in symbols)
            if (isNamedMember(symbol, name))
                result[name] = instantiateSymbol(symbol, mapper);
        return result;
    }

    private Symbol instantiateSymbol(Symbol symbol, TypeMapper mapper)
    {
        if (symbol == null)
            return null;
        var links = valueSymbolLinks.Get(symbol);
        if (mapper != null && mapper.MapsThisOnly() && isThisless(symbol))
            return symbol;
        if (links.resolvedType != null && !couldContainTypeVariables(links.resolvedType))
        {
            if ((symbol.Flags & SymbolFlags.SetAccessor) == 0 || links.writeType != null && !couldContainTypeVariables(links.writeType))
                return symbol;
        }

        if ((symbol.CheckFlags & CheckFlags.Instantiated) != 0)
        {
            symbol = links.target;
            mapper = combineTypeMappers(links.mapper, mapper);
        }

        var result = newSymbol(symbol.Flags, symbol.Name);
        result.CheckFlags = CheckFlags.Instantiated | (symbol.CheckFlags & (CheckFlags.Readonly | CheckFlags.Late | CheckFlags.OptionalParameter | CheckFlags.RestParameter));
        result.Declarations = symbol.Declarations;
        result.Parent = symbol.Parent;
        result.ValueDeclaration = symbol.ValueDeclaration;
        var resultLinks = valueSymbolLinks.Get(result);
        resultLinks.target = symbol;
        resultLinks.mapper = mapper;
        resultLinks.nameType = links.nameType;
        return result;
    }

    private static bool isThisless(Symbol symbol)
    {
        if ((GoRuntime.Len(symbol.Declarations) == 1))
        {
            var declaration = symbol.Declarations[0];
            if ((declaration != null))
            {
                switch (declaration.Kind)
                {
                    case Kind.Parameter:
                    {
                        return isThislessVariableLikeDeclaration(declaration);
                    }

                    case Kind.PropertyDeclaration:
                    case Kind.PropertySignature:
                    {
                        return isThislessVariableLikeDeclaration(declaration);
                    }

                    case Kind.MethodDeclaration:
                    case Kind.MethodSignature:
                    case Kind.Constructor:
                    case Kind.GetAccessor:
                    case Kind.SetAccessor:
                    {
                        return isThislessFunctionLikeDeclaration(declaration);
                    }
                }
            }
        }

        return false;
    }

    private Type getTypeOfInstantiatedSymbol(Symbol symbol)
    {
        var links = valueSymbolLinks.Get(symbol);
        return links.resolvedType ??= instantiateType(getTypeOfSymbol(links.target), links.mapper);
    }
}

public sealed partial class Checker
{
    private Type getConstraintOfType(Type t)
    {
        if ((t.flags & TypeFlags.TypeParameter) != 0)
            return getConstraintOfTypeParameter(t);
        if ((t.flags & (TypeFlags.IndexedAccess | TypeFlags.Conditional)) != 0)
            throw unsupported("getConstraintOfType: indexed/conditional");
        return getBaseConstraintOfType(t);
    }

    private Type getConstraintOfTypeParameter(Type t) => hasNonCircularBaseConstraint(t) ? getConstraintFromTypeParameter(t) : null;
    private bool hasNonCircularBaseConstraint(Type t) => getResolvedBaseConstraint(t, null) != circularConstraintType;
    private Type getConstraintFromTypeParameter(Type t)
    {
        if ((t.flags & TypeFlags.TypeParameter) == 0)
            return null;
        var d = t.AsTypeParameter();
        if (d.constraint == null)
        {
            if (d.target != null)
                d.constraint = instantiateType(getConstraintOfTypeParameter(d.target), d.mapper) ?? noConstraintType;
            else
                throw unsupported("getConstraintFromTypeParameter: source parameter/inference constraint");
        }

        return d.constraint == noConstraintType ? null : d.constraint;
    }

    private Type getBaseConstraintOfType(Type t)
    {
        if ((t.flags & (TypeFlags.InstantiableNonPrimitive | TypeFlags.UnionOrIntersection | TypeFlags.TemplateLiteral | TypeFlags.StringMapping | TypeFlags.Index)) != 0)
        {
            var constraint = getResolvedBaseConstraint(t, null);
            return constraint != noConstraintType && constraint != circularConstraintType ? constraint : null;
        }

        return null;
    }

    private Type getResolvedBaseConstraint(Type t, List<RecursionId> stack)
    {
        var d = t.AsConstrainedType();
        if (d == null)
            return t;
        if (d.resolvedBaseConstraint != null)
            return d.resolvedBaseConstraint;
        if (!pushTypeResolution(t, TypeSystemPropertyName.ResolvedBaseConstraint))
            return circularConstraintType;
        Type constraint = null;
        var identity = getRecursionIdentity(t);
        if ((stack?.Count ?? 0) < 10 || (stack?.Count ?? 0) < 50 && !(stack?.Contains(identity) ?? false))
            constraint = computeBaseConstraint(getSimplifiedType(t, false), GoRuntime.Append(stack, identity));
        if (!popTypeResolution())
        {
            if ((t.flags & TypeFlags.TypeParameter) != 0 && !t.AsTypeParameter().isThisType)
                throw unsupported("getResolvedBaseConstraint: source circular constraint");
            constraint = circularConstraintType;
        }

        constraint ??= noConstraintType;
        return d.resolvedBaseConstraint ??= constraint;
    }

    private Type computeBaseConstraint(Type t, List<RecursionId> stack)
    {
        if ((t.flags & TypeFlags.TypeParameter) != 0)
        {
            var constraint = getConstraintFromTypeParameter(t);
            return t.AsTypeParameter().isThisType ? constraint : getNextBaseConstraint(constraint, stack);
        }

        if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
        {
            var types = t.Types();
            List<Type> constraints = new(types.Count);
            bool different = false;
            foreach (var source in types)
            {
                var constraint = getNextBaseConstraint(source, stack);
                if (constraint != null)
                {
                    if (constraint != source)
                        different = true;
                    constraints.Add(constraint);
                }
                else
                    different = true;
            }

            if (!different)
                return t;
            if ((t.flags & TypeFlags.Union) != 0 && constraints.Count == types.Count)
                return getUnionType(constraints);
            if ((t.flags & TypeFlags.Intersection) != 0 && constraints.Count > 0)
                return getIntersectionType(constraints);
            return null;
        }

        if ((t.flags & TypeFlags.Index) != 0)
        {
            if (isGenericMappedType(t.AsIndexType().target))
                throw unsupported("computeBaseConstraint: source remapped index");
            return stringNumberSymbolType ??= getUnionType([stringType, numberType, esSymbolType]);
        }

        if ((t.flags & TypeFlags.Instantiable) != 0 || (t.objectFlags & ObjectFlags.Tuple) != 0)
            throw unsupported("computeBaseConstraint: source generic/index/tuple");
        return t;
    }

    private Type getNextBaseConstraint(Type t, List<RecursionId> stack)
    {
        if (t == null)
            return null;
        var constraint = getResolvedBaseConstraint(t, stack);
        return constraint == noConstraintType || constraint == circularConstraintType ? null : constraint;
    }

    private Type getSimplifiedType(Type t, bool writing)
    {
        if ((t.flags & (TypeFlags.IndexedAccess | TypeFlags.Conditional)) != 0)
            throw unsupported("getSimplifiedType: source indexed/conditional");
        return t;
    }

    private static SymbolTable cloneSymbolTable(SymbolTable source)
    {
        if (source == null)
            return null;
        var result = new SymbolTable(source.Count);
        foreach (var(name, symbol)in source)
            result[name] = symbol;
        return result;
    }

    private List<Type> addNamedUnions(List<Type> named, List<Type> types)
    {
        foreach (var t in types ?? [])
            if ((t.flags & TypeFlags.Union) != 0)
            {
                var origin = t.AsUnionType().origin;
                if (t.alias != null || origin != null && (origin.flags & TypeFlags.Union) == 0)
                {
                    if (!(named?.Contains(t) ?? false))
                        named = GoRuntime.Append(named, t);
                }
                else if (origin != null && (origin.flags & TypeFlags.Union) != 0)
                    named = addNamedUnions(named, origin.Types());
            }

        return named;
    }

    private Type getTypeOfSymbolWithDeferredType(Symbol symbol)
    {
        var links = valueSymbolLinks.Get(symbol);
        if (links.resolvedType == null)
        {
            var deferred = deferredSymbolLinks.Get(symbol);
            links.resolvedType = (deferred.parent.flags & TypeFlags.Union) != 0 ? getUnionType(deferred.constituents) : getIntersectionType(deferred.constituents);
        }

        return links.resolvedType;
    }
}

public sealed partial class Checker
{
    internal readonly HashSet<Type> markerTypes = new();
    private int compareSymbols(Symbol left, Symbol right) => compareSymbolsWorker(left, right);
    private Type globalArrayType, globalReadonlyArrayType;
    private Type globalIterableType, globalIterableIteratorType, globalAsyncIterableType, globalAsyncIterableIteratorType;
    internal Type getGlobalIterableType() => globalIterableType ??= getGlobalType("Iterable", 3, false);
    internal Type getGlobalIterableIteratorType() => globalIterableIteratorType ??= getGlobalType("IterableIterator", 3, false);
    internal Type getGlobalAsyncIterableType() => globalAsyncIterableType ??= getGlobalType("AsyncIterable", 3, false);
    internal Type getGlobalAsyncIterableIteratorType() => globalAsyncIterableIteratorType ??= getGlobalType("AsyncIterableIterator", 3, false);
    internal bool isReferenceToType(Type t, Type target) => t != null && (t.objectFlags & ObjectFlags.Reference) != 0 && t.Target() == target;
    private bool isArrayType(Type t) => (t.objectFlags & ObjectFlags.Reference) != 0 && (t.Target() == globalArrayType || t.Target() == globalReadonlyArrayType);
    private bool isArrayOrTupleType(Type t) => isArrayType(t) || (t.objectFlags & ObjectFlags.Tuple) != 0;
    private bool isNonGenericObjectType(Type t) => (t.flags & TypeFlags.Object) != 0 && !isGenericMappedType(t);
    private bool isGenericObjectType(Type t) => (getGenericObjectFlags(t) & ObjectFlags.IsGenericObjectType) != 0;
    private bool isGenericMappedType(Type t)
    {
        if ((t.objectFlags & ObjectFlags.Mapped) != 0)
            throw unsupported("isGenericMappedType: source mapped type");
        return false;
    }

    private bool isGenericTupleType(Type t)
    {
        if ((t.objectFlags & ObjectFlags.Tuple) != 0)
            throw unsupported("isGenericTupleType: source tuple");
        return false;
    }

    private ObjectFlags getGenericObjectFlags(Type t)
    {
        ObjectFlags combined = 0;
        if ((t.flags & (TypeFlags.UnionOrIntersection | TypeFlags.Substitution)) != 0)
        {
            if ((t.objectFlags & ObjectFlags.IsGenericTypeComputed) == 0)
            {
                if ((t.flags & TypeFlags.UnionOrIntersection) != 0)
                    foreach (var type in t.Types())
                        combined |= getGenericObjectFlags(type);
                else
                    throw unsupported("getGenericObjectFlags: source substitution");
                t.objectFlags |= ObjectFlags.IsGenericTypeComputed | combined;
            }

            return t.objectFlags & ObjectFlags.IsGenericType;
        }

        if ((t.flags & TypeFlags.InstantiableNonPrimitive) != 0 || isGenericMappedType(t) || isGenericTupleType(t))
            combined |= ObjectFlags.IsGenericObjectType;
        if ((t.flags & (TypeFlags.InstantiableNonPrimitive | TypeFlags.Index)) != 0)
            combined |= ObjectFlags.IsGenericIndexType;
        if ((t.flags & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0)
            throw unsupported("getGenericObjectFlags: source generic string type");
        return combined;
    }

    private IndexInfo anyBaseTypeIndexInfo;
    private IndexInfo newIndexInfo(Type keyType, Type valueType, bool isReadonly, Node declaration, List<Node> components) => new()
    {
        keyType = keyType,
        valueType = valueType,
        isReadonly = isReadonly,
        declaration = declaration,
        components = components
    };
    private IndexInfo instantiateIndexInfo(IndexInfo info, TypeMapper mapper)
    {
        var value = instantiateType(info.valueType, mapper);
        return value == info.valueType ? info : newIndexInfo(info.keyType, value, info.isReadonly, info.declaration, info.components);
    }

    private static IndexInfo findIndexInfo(List<IndexInfo> infos, Type keyType) => infos?.Find(info => info.keyType == keyType);
    private IndexInfo getIndexInfoOfType(Type t, Type keyType) => findIndexInfo(getIndexInfosOfType(t), keyType);
    private IndexInfo getApplicableIndexInfo(Type t, Type keyType) => findApplicableIndexInfo(getIndexInfosOfType(t), keyType);
    private IndexInfo findApplicableIndexInfo(List<IndexInfo> infos, Type keyType)
    {
        IndexInfo stringInfo = null;
        List<IndexInfo> applicable = new(8);
        foreach (var info in infos ?? [])
            if (info.keyType == stringType)
                stringInfo = info;
            else if (isApplicableIndexType(keyType, info.keyType))
                applicable.Add(info);
        if (applicable.Count == 0)
            return stringInfo != null && isApplicableIndexType(keyType, stringType) ? stringInfo : null;
        if (applicable.Count == 1)
            return applicable[0];
        return newIndexInfo(unknownType, getIntersectionType(applicable.Select(info => info.valueType).ToList()), applicable.All(info => info.isReadonly), null, null);
    }

    private bool isApplicableIndexType(Type source, Type target) => isTypeAssignableTo(source, target) || target == stringType && isTypeAssignableTo(source, numberType) || target == numberType && (source.flags & TypeFlags.StringLiteral) != 0 && isNumericLiteralName(getStringLiteralValue(source));
    private Type getSingleBaseForNonAugmentingSubtype(Type t)
    {
        if ((t.objectFlags & ObjectFlags.Reference) == 0 || (t.Target().objectFlags & ObjectFlags.ClassOrInterface) == 0)
            return null;
        var key = new CachedTypeKey(CachedTypeKind.EquivalentBaseType, t.id);
        if ((t.objectFlags & ObjectFlags.IdenticalBaseTypeCalculated) != 0)
            return cachedTypes.TryGetValue(key, out var cached) ? cached : null;
        t.objectFlags |= ObjectFlags.IdenticalBaseTypeCalculated;
        var target = t.Target();
        if ((target.objectFlags & ObjectFlags.Class) != 0)
            throw unsupported("getSingleBaseForNonAugmentingSubtype: source class");
        var bases = getBaseTypes(target);
        if ((bases?.Count ?? 0) != 1 || (getMembersOfSymbol(t.symbol)?.Count ?? 0) != 0)
            return null;
        var parameters = target.AsInterfaceType().TypeParameters();
        var instantiated = (parameters?.Count ?? 0) == 0 ? bases[0] : instantiateType(bases[0], newTypeMapper(parameters, getTypeArguments(t).GetRange(0, parameters.Count)));
        if ((getTypeArguments(t)?.Count ?? 0) > (parameters?.Count ?? 0))
            instantiated = getTypeWithThisArgument(instantiated, getTypeArguments(t)[^1], false);
        cachedTypes[key] = instantiated;
        return instantiated;
    }
}

public sealed partial class Checker
{
    // checker.go:25728, 26986. Full selected data-type allocation/count paths.
    private Type newIntersectionType(ObjectFlags flags, List<Type> types) => newType(TypeFlags.Intersection, flags, new IntersectionType { types = types });
    private static int getConstituentCount(Type t)
    {
        if ((t.flags & TypeFlags.UnionOrIntersection) == 0 || t.alias != null)
            return 1;
        if ((t.flags & TypeFlags.Union) != 0 && t.AsUnionType().origin != null)
            return getConstituentCount(t.AsUnionType().origin);
        return getConstituentCountOfTypes(t.Types());
    }

    private static int getConstituentCountOfTypes(List<Type> types)
    {
        int n = 0;
        foreach (var t in types ?? [])
            n += getConstituentCount(t);
        return n;
    }

    // checker.go:28330. Source index syntax is excluded; synthetic base infos have no declaration.
    internal static string getNameFromIndexInfo(IndexInfo info)
    {
        if (info.declaration != null)
            return TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(info.declaration.Parameters()[0].Name());
        return "x";
    }

    private readonly Dictionary<CacheHashKey, List<Type>> subtypeReductionCache = new();
    private Type unknownEmptyObjectType;
    // checker.go:26468. Class derivation and source type variables are excluded;
    // selected implicit-this constraints still use the original union constraint arm.
    private List<Type> removeSubtypes(List<Type> types, bool hasObjectTypes)
    {
        if (types.Count < 2)
            return types;
        var key = getTypeListKey(types);
        if (subtypeReductionCache.TryGetValue(key, out var cached))
            return cached;
        bool hasEmptyObject = hasObjectTypes && types.Any(t => (t.flags & TypeFlags.Object) != 0 && !isGenericMappedType(t) && isEmptyResolvedType(resolveStructuredTypeMembers(t)));
        int length = types.Count, i = length, count = 0;
        while (i > 0)
        {
            i--;
            var source = types[i];
            if (hasEmptyObject || (source.flags & TypeFlags.StructuredOrInstantiable) != 0)
            {
                if ((source.flags & TypeFlags.TypeParameter) != 0 && (getBaseConstraintOrType(source).flags & TypeFlags.Union) != 0)
                {
                    if (isTypeRelatedTo(source, getUnionType(types.Select(t => t == source ? neverType : t).ToList()), strictSubtypeRelation))
                        types.RemoveAt(i);
                    continue;
                }

                Symbol keyProperty = null;
                Type keyPropertyType = null;
                if ((source.flags & (TypeFlags.Object | TypeFlags.Intersection | TypeFlags.InstantiableNonPrimitive)) != 0)
                    keyProperty = (getPropertiesOfType(source) ?? []).Find(p => isUnitType(getTypeOfSymbol(p)));
                if (keyProperty != null)
                    keyPropertyType = getRegularTypeOfLiteralType(getTypeOfSymbol(keyProperty));
                foreach (var target in types)
                {
                    if (source == target)
                        continue;
                    if (count == 100000)
                    {
                        int estimatedCount = count / (length - i) * length;
                        if (estimatedCount > 1000000)
                        {
                            error(currentNode, Diagnostics.Expression_produces_a_union_type_that_is_too_complex_to_represent);
                            return null;
                        }
                    }

                    count++;
                    if (keyProperty != null && (target.flags & (TypeFlags.Object | TypeFlags.Intersection | TypeFlags.InstantiableNonPrimitive)) != 0)
                    {
                        var t = getTypeOfPropertyOfType(target, keyProperty.Name);
                        if (t != null && isUnitType(t) && getRegularTypeOfLiteralType(t) != keyPropertyType)
                            continue;
                    }

                    if ((source == emptyObjectType || source == unknownEmptyObjectType) && target.symbol != null && IsEmptyAnonymousObjectType(target))
                        continue;
                    if (isTypeRelatedTo(source, target, strictSubtypeRelation))
                    {
                        if ((getTargetType(source).objectFlags & ObjectFlags.Class) != 0 && (getTargetType(target).objectFlags & ObjectFlags.Class) != 0)
                            throw unsupported("removeSubtypes: class derivation");
                        types.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        subtypeReductionCache[key] = types;
        return types;
    }

    // checker.go:21706. Constructor/mixin behavior is guarded before selected data-only combination.
    private void resolveIntersectionTypeMembers(Type t)
    {
        List<Signature> callSignatures = null, constructSignatures = null;
        List<IndexInfo> indexInfos = null;
        var(mixinFlags, mixinCount) = findMixins(t.Types());
        for (int i = 0; i < t.Types().Count; i++)
        {
            var constituent = t.Types()[i];
            if (!mixinFlags[i])
            {
                var signatures = getSignaturesOfType(constituent, SignatureKind.Construct);
                if ((signatures?.Count ?? 0) != 0 && mixinCount > 0)
                    throw unsupported("resolveIntersectionTypeMembers: constructor mixins");
                constructSignatures = appendSignatures(constructSignatures, signatures);
            }

            callSignatures = appendSignatures(callSignatures, getSignaturesOfType(constituent, SignatureKind.Call));
            foreach (var info in getIndexInfosOfType(constituent) ?? [])
                indexInfos = appendIndexInfo(indexInfos, info, false);
        }

        setStructuredTypeMembers(t, null, callSignatures, constructSignatures, indexInfos);
    }

    private (List<bool>, int) findMixins(List<Type> types)
    {
        var flags = types.Select(isMixinConstructorType).ToList();
        int constructorTypeCount = 0, mixinCount = 0, firstMixinIndex = -1;
        for (int i = 0; i < types.Count; i++)
        {
            if ((getSignaturesOfType(types[i], SignatureKind.Construct)?.Count ?? 0) > 0)
                constructorTypeCount++;
            if (flags[i])
            {
                if (firstMixinIndex < 0)
                    firstMixinIndex = i;
                mixinCount++;
            }
        }

        if (constructorTypeCount > 0 && constructorTypeCount == mixinCount)
        {
            flags[firstMixinIndex] = false;
            mixinCount--;
        }

        return (flags, mixinCount);
    }

    private bool isMixinConstructorType(Type t)
    {
        var signatures = getSignaturesOfType(t, SignatureKind.Construct);
        if ((signatures?.Count ?? 0) != 1)
            return false;
        throw unsupported("isMixinConstructorType: source constructor signature");
    }

    private List<Signature> appendSignatures(List<Signature> signatures, List<Signature> newSignatures)
    {
        {
            foreach (var sig in GoRuntime.RangeValues(newSignatures))
            {
                if (((GoRuntime.Len(signatures) == 0) || Core.Every(signatures, (s) =>
                {
                    return (this.compareSignaturesIdentical(s, sig, false, false, false, this.compareTypesIdentical) == Ternary.False);
                })))
                {
                    signatures = GoRuntime.Append(signatures, sig);
                }
            }
        }

        return signatures;
    }

    private List<IndexInfo> appendIndexInfo(List<IndexInfo> infos, IndexInfo newInfo, bool union)
    {
        for (int i = 0; i < (infos?.Count ?? 0); i++)
        {
            var info = infos[i];
            if (info.keyType == newInfo.keyType)
            {
                var value = union ? getUnionType([info.valueType, newInfo.valueType]) : getIntersectionType([info.valueType, newInfo.valueType]);
                infos[i] = newIndexInfo(info.keyType, value, union ? info.isReadonly || newInfo.isReadonly : info.isReadonly && newInfo.isReadonly, null, null);
                return infos;
            }
        }

        return GoRuntime.Append(infos, newInfo);
    }

    private Diagnostic elaborateNeverIntersection(Diagnostic chain, Node node, Type t)
    {
        if ((t.flags & TypeFlags.Intersection) != 0 && (t.objectFlags & ObjectFlags.IsNeverIntersection) != 0)
        {
            var neverProp = getPropertiesOfUnionOrIntersectionType(t)?.Find(isDiscriminantWithNeverType);
            if (neverProp != null)
                return NewDiagnosticChainForNode(chain, node, Diagnostics.The_intersection_0_was_reduced_to_never_because_property_1_has_conflicting_types_in_some_constituents, typeToStringNoTypeReduction(t), symbolToString(neverProp));
            var privateProp = getPropertiesOfUnionOrIntersectionType(t)?.Find(isConflictingPrivateProperty);
            if (privateProp != null)
                return NewDiagnosticChainForNode(chain, node, Diagnostics.The_intersection_0_was_reduced_to_never_because_property_1_exists_in_multiple_constituents_and_is_private_in_some, typeToStringNoTypeReduction(t), symbolToString(privateProp));
        }

        return chain;
    }

    private bool isDiscriminantWithNeverType(Symbol prop) => (prop.Flags & SymbolFlags.Optional) == 0 && (prop.CheckFlags & (CheckFlags.NonUniformAndLiteral | CheckFlags.HasNeverType)) == CheckFlags.NonUniformAndLiteral && (getTypeOfSymbol(prop).flags & TypeFlags.Never) != 0;
}

public sealed partial class Checker
{
    private static string getStringLiteralValue(Type t) => (string)t.AsLiteralType().value;
    internal Type getBaseConstraintOrType(Type t) => getBaseConstraintOfType(t) ?? t;
    private Symbol resolveSymbol(Symbol symbol) => resolveSymbolEx(symbol, false);
    private Symbol resolveSymbolEx(Symbol symbol, bool dontResolveAlias)
    {
        if (!dontResolveAlias && Ast.IsNonLocalAlias(symbol, SymbolFlags.Value | SymbolFlags.Type | SymbolFlags.Namespace))
            throw unsupported("resolveSymbolEx: source import/export alias");
        return symbol;
    }

    private Symbol getResolvedTarget(Symbol symbol) => getMergedSymbol(resolveSymbol(getMergedSymbol(symbol)));
    internal Symbol getSymbolIfSameReference(Symbol first, Symbol second) => getResolvedTarget(first) == getResolvedTarget(second) ? first : null;
}

public sealed partial class Checker
{
    // checker.go:1651,1682,1706,1766. Actual selected meaning-space diagnostics.
    private bool checkAndReportErrorForUsingTypeAsNamespace(Node location, string name, SymbolFlags meaning)
    {
        if (meaning == SymbolFlags.Namespace)
        {
            var symbol = resolveSymbol(nameResolver.Resolve(location, name, SymbolFlags.Type & ~SymbolFlags.Namespace, null, false, false));
            if (symbol != null)
            {
                var parent = location.Parent;
                if (Ast.IsQualifiedName(parent))
                {
                    var propertyName = parent.AsQualifiedName().Right.Text();
                    if (getPropertyOfType(getDeclaredTypeOfSymbol(symbol), propertyName) != null)
                    {
                        error(parent, Diagnostics.Cannot_access_0_1_because_0_is_a_type_but_not_a_namespace_Did_you_mean_to_retrieve_the_type_of_the_property_1_in_0_with_0_1, name, propertyName);
                        return true;
                    }
                }

                error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_namespace_here, name);
                return true;
            }
        }

        return false;
    }

    private bool checkAndReportErrorForUsingNamespaceAsTypeOrValue(Node location, string name, SymbolFlags meaning)
    {
        if ((meaning & (SymbolFlags.Value & ~SymbolFlags.Type)) != 0)
        {
            var symbol = resolveSymbol(nameResolver.Resolve(location, name, SymbolFlags.NamespaceModule, null, false, false));
            if (symbol != null)
            {
                error(location, Diagnostics.Cannot_use_namespace_0_as_a_value, name);
                return true;
            }
        }
        else if ((meaning & (SymbolFlags.Type & ~SymbolFlags.Value)) != 0)
        {
            var symbol = resolveSymbol(nameResolver.Resolve(location, name, SymbolFlags.Module, null, false, false));
            if (symbol != null)
            {
                error(location, Diagnostics.Cannot_use_namespace_0_as_a_type, name);
                return true;
            }
        }

        return false;
    }

    private bool checkAndReportErrorForUsingTypeAsValue(Node location, string name, SymbolFlags meaning)
    {
        if ((meaning & SymbolFlags.Value) != 0)
        {
            if (isPrimitiveTypeName(name))
            {
                var grandparent = location.Parent.Parent;
                if (grandparent?.Parent != null && Ast.IsHeritageClause(grandparent))
                {
                    if (grandparent.Parent.Kind == Kind.InterfaceDeclaration && grandparent.AsHeritageClause().Token == Kind.ExtendsKeyword)
                        error(location, Diagnostics.An_interface_cannot_extend_a_primitive_type_like_0_It_can_only_extend_other_named_object_types, name);
                    else
                        throw unsupported("checkAndReportErrorForUsingTypeAsValue: class heritage");
                }
                else
                    error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here, name);
                return true;
            }

            var symbol = resolveSymbol(nameResolver.Resolve(location, name, SymbolFlags.Type & ~SymbolFlags.Value, null, false, false));
            if (symbol != null && (symbol.Flags & SymbolFlags.Value) == 0)
            {
                if (isES2015OrLaterConstructorName(name))
                    error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here_Do_you_need_to_change_your_target_library_Try_changing_the_lib_compiler_option_to_es2015_or_later, name);
                else if (maybeMappedType(location, symbol))
                    error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here_Did_you_mean_to_use_1_in_0, name, name == "K" ? "P" : "K");
                else
                    error(location, Diagnostics.X_0_only_refers_to_a_type_but_is_being_used_as_a_value_here, name);
                return true;
            }
        }

        return false;
    }

    private static bool isES2015OrLaterConstructorName(string name) => name is "Promise" or "Symbol" or "Map" or "WeakMap" or "Set" or "WeakSet";
    private bool maybeMappedType(Node node, Symbol symbol)
    {
        do
        {
            node = node.Parent;
        }
        while (Ast.IsComputedPropertyName(node) || Ast.IsPropertySignatureDeclaration(node));
        if (Ast.IsTypeLiteralNode(node) && node.Members()?.Count == 1)
        {
            var t = getDeclaredTypeOfSymbol(symbol);
            return (t.flags & TypeFlags.Union) != 0 && allTypesAssignableToKindEx(t, TypeFlags.StringOrNumberLiteral, true);
        }

        return false;
    }

    private bool checkAndReportErrorForUsingValueAsType(Node location, string name, SymbolFlags meaning)
    {
        if ((meaning & (SymbolFlags.Type & ~SymbolFlags.Namespace)) != 0)
        {
            var symbol = resolveSymbol(nameResolver.Resolve(location, name, ~SymbolFlags.Type & SymbolFlags.Value, null, false, false));
            if (symbol != null && (symbol.Flags & SymbolFlags.Namespace) == 0)
            {
                error(location, Diagnostics.X_0_refers_to_a_value_but_is_being_used_as_a_type_here_Did_you_mean_typeof_0, name);
                return true;
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isGenericType(Type t) => getGenericObjectFlags(t) != 0;
    private Type getWriteTypeOfSymbolWithDeferredType(Symbol symbol)
    {
        var links = valueSymbolLinks.Get(symbol);
        if (links.writeType == null)
        {
            var deferred = deferredSymbolLinks.Get(symbol);
            links.writeType = (deferred.writeConstituents?.Count ?? 0) != 0 ? (deferred.parent.flags & TypeFlags.Union) != 0 ? getUnionType(deferred.writeConstituents) : getIntersectionType(deferred.writeConstituents) : getTypeOfSymbolWithDeferredType(symbol);
        }

        return links.writeType;
    }
}

public sealed partial class Checker
{
    public bool IsDeprecatedDeclaration(Node declaration) => Ast.IsDeprecatedDeclarationWithCachedFlags(declaration, getCombinedNodeFlagsCached(declaration));
}

public sealed partial class Checker
{
    private Type unknownUnionType;
    private Type createUnknownUnionType() => getUnionType([undefinedType, nullType, unknownEmptyObjectType]);
    private TypeFacts getIntersectionTypeFacts(Type t, TypeFacts callerOnlyNeeds)
    {
        bool ignoreObjects = maybeTypeOfKind(t, TypeFlags.Primitive);
        TypeFacts ored = TypeFacts.None, anded = TypeFacts.All;
        foreach (var u in t.Types())
            if (!(ignoreObjects && (u.flags & TypeFlags.Object) != 0))
            {
                var facts = getTypeFactsWorker(u, callerOnlyNeeds);
                ored |= facts;
                anded &= facts;
            }

        return ored & TypeFacts.OrFactsMask | anded & TypeFacts.AndFactsMask;
    }
}

public sealed partial class Checker
{
    private Type stringOrNumberType;
    private Type newIndexType(Type target, IndexFlags flags) => newType(TypeFlags.Index, ObjectFlags.None, new IndexType { target = target, indexFlags = flags });
    private bool isKeyTypeIncluded(Type keyType, TypeFlags include) => (keyType.flags & include) != 0 || (keyType.flags & TypeFlags.Intersection) != 0 && keyType.Types().Any(t => isKeyTypeIncluded(t, include));
}

public sealed partial class Checker
{
    // checker.go:initializeClosures and27057; source template/string-mapping types are excluded.
    private bool isPrimitiveOrObjectOrEmptyType(Type t) => (t.flags & (TypeFlags.Primitive | TypeFlags.NonPrimitive)) != 0 || IsEmptyAnonymousObjectType(t);
    private bool isGenericStringLikeType(Type t)
    {
        if ((t.flags & (TypeFlags.TemplateLiteral | TypeFlags.StringMapping)) != 0)
            throw unsupported("isGenericStringLikeType: source template/string mapping");
        return false;
    }
}

public sealed partial class Checker
{
    // checker.go:25781. Managed allocation replaces only the upstream signature arena.
    private Signature newSignature(SignatureFlags flags, Node declaration, List<Type> typeParameters, Symbol thisParameter, List<Symbol> parameters, Type resolvedReturnType, TypePredicate resolvedTypePredicate, int minArgumentCount)
    {
        SignatureCount++;
        var sig = new Signature();
        authoredOwnedSignatures.Add(sig);
        sig.id = (uint)SignatureCount;
        sig.flags = flags;
        sig.declaration = declaration;
        sig.typeParameters = typeParameters;
        sig.parameters = parameters;
        sig.thisParameter = thisParameter;
        sig.resolvedReturnType = resolvedReturnType;
        sig.resolvedTypePredicate = resolvedTypePredicate;
        sig.minArgumentCount = minArgumentCount;
        sig.resolvedMinArgumentCount = -1;
        return sig;
    }
}

public sealed partial class Checker
{
    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20210:1.
    private List<Signature> getSignaturesOfSymbol(Symbol symbol)
    {
        if ((symbol == null))
        {
            return null;
        }

        List<Signature> result = default;
        {
            foreach (var range1 in GoRuntime.Range(symbol.Declarations))
            {
                var i = range1.Key;
                var decl = range1.Value;
                if (!Ast.IsFunctionLike(decl))
                {
                    continue;
                }

                if (((i > 0) && (decl.Body() != null)))
                {
                    var previous = symbol.Declarations[(i - 1)];
                    if ((((decl.Parent == previous.Parent) && (decl.Kind == previous.Kind)) && (((decl.Pos() == previous.End()) || ((previous.Flags & NodeFlags.Reparsed) != 0)))))
                    {
                        continue;
                    }
                }

                var sig = this.getSignatureOfFullSignatureType(decl);
                if ((sig == null))
                {
                    sig = this.getSignatureFromDeclaration(decl);
                }

                result = GoRuntime.Append(result, sig);
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20240:1.
    internal Signature getSignatureFromDeclaration(Node declaration)
    {
        var links = this.signatureLinks.Get(declaration);
        if ((links.resolvedSignature != null))
        {
            return links.resolvedSignature;
        }

        List<Symbol> parameters = default;
        SignatureFlags flags = default;
        Symbol thisParameter = default;
        var minArgumentCount = 0;
        var iife = Ast.GetImmediatelyInvokedFunctionExpression(declaration);
        var isUntypedSignatureInJSFile = (((((iife == null) && Ast.IsInJSFile(declaration)) && (((((Ast.IsFunctionExpression(declaration) || Ast.IsArrowFunction(declaration)) || Ast.IsMethodOrAccessor(declaration)) || Ast.IsFunctionDeclaration(declaration)) || Ast.IsConstructorDeclaration(declaration)))) && Core.Every(declaration.Parameters(), (param) =>
        {
            return (param.Type() == null);
        })) && (this.getContextualType(declaration) == null));
        if (isUntypedSignatureInJSFile)
        {
            flags |= SignatureFlags.IsUntypedSignatureInJSFile;
        }

        {
            foreach (var range2 in GoRuntime.Range(declaration.Parameters()))
            {
                var i = range2.Key;
                var param2 = range2.Value;
                var paramSymbol = param2.Symbol();
                var typeNode = param2.Type();
                if ((((paramSymbol != null) && ((paramSymbol.Flags & SymbolFlags.Property) != 0)) && !Ast.IsBindingPattern(param2.Name())))
                {
                    var resolvedSymbol = this.nameResolver.Resolve(param2, paramSymbol.Name, SymbolFlags.Value, null, false, false);
                    paramSymbol = resolvedSymbol;
                }

                if (((i == 0) && (paramSymbol.Name == Ast.InternalSymbolNameThis)))
                {
                    thisParameter = param2.Symbol();
                }
                else
                {
                    parameters = GoRuntime.Append(parameters, paramSymbol);
                }

                if (((typeNode != null) && (typeNode.Kind == Kind.LiteralType)))
                {
                    flags |= SignatureFlags.HasLiteralTypes;
                }

                var isOptionalParameter = (((isOptionalDeclaration(param2) || (param2.Initializer() != null)) || isRestParameter(param2)) || (((iife != null) && (GoRuntime.Len(parameters) > GoRuntime.Len(iife.Arguments()))) && (typeNode == null)));
                if (!isOptionalParameter)
                {
                    minArgumentCount = GoRuntime.Len(parameters);
                }
            }
        }

        if (Ast.IsGetAccessorDeclaration(declaration) || Ast.IsSetAccessorDeclaration(declaration) || Ast.IsConstructorDeclaration(declaration) || Ast.IsConstructorTypeNode(declaration) || Ast.IsConstructSignatureDeclaration(declaration))
            throw unsupported("getSignatureFromDeclaration: accessor/constructor");
        var typeParameters = this.getTypeParametersFromDeclaration(declaration);
        if (hasRestParameter(declaration))
            throw unsupported("getSignatureFromDeclaration: rest parameter");
        links.resolvedSignature = this.newSignature(flags, declaration, typeParameters, thisParameter, parameters, null, null, minArgumentCount);
        return links.resolvedSignature;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20316:1.
    private List<Type> getTypeParametersFromDeclaration(Node declaration)
    {
        {
            var sig = this.getSignatureOfFullSignatureType(declaration);
            if ((sig != null))
            {
                return sig.TypeParameters();
            }
        }

        List<Type> result = default;
        {
            foreach (var node in GoRuntime.RangeValues(declaration.TypeParameters()))
            {
                result = Core.AppendIfUnique(result, this.getDeclaredTypeOfTypeParameter(node.Symbol()));
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20405:1.
    internal Type getReturnTypeOfSignature(Signature sig)
    {
        if ((sig.resolvedReturnType != null))
        {
            return sig.resolvedReturnType;
        }

        if (!this.pushTypeResolution(sig, TypeSystemPropertyName.ResolvedReturnType))
        {
            return this.errorType;
        }

        Type t = default;
        switch (true)
        {
            case true when (sig.target != null):
            {
                t = this.instantiateType(this.getReturnTypeOfSignature(sig.target), sig.mapper);
                break;
            }

            case true when (sig.composite != null):
            {
                t = this.instantiateType(this.getUnionOrIntersectionType(Core.Map(sig.composite.signatures, this.getReturnTypeOfSignature), sig.composite.isUnion, UnionReduction.Subtype), sig.mapper);
                break;
            }

            default:
            {
                t = this.getReturnTypeFromAnnotation(sig.declaration);
                if ((t == null))
                {
                    if (!Ast.NodeIsMissing(sig.declaration.Body()))
                    {
                        t = this.getReturnTypeFromBody(sig.declaration, CheckMode.Normal);
                    }
                    else
                    {
                        t = this.anyType;
                    }
                }

                break;
            }
        }

        if (((sig.flags & SignatureFlags.IsInnerCallChain) != 0))
        {
            throw unsupported("getReturnTypeOfSignature: optional call chain");
        }
        else
        {
            if (((sig.flags & SignatureFlags.IsOuterCallChain) != 0))
            {
                t = this.getOptionalType(t, false);
            }
        }

        if (!this.popTypeResolution())
        {
            if ((sig.declaration != null))
            {
                var typeNode = sig.declaration.Type();
                if ((typeNode != null))
                {
                    this.error(typeNode, Diagnostics.Return_type_annotation_circularly_references_itself);
                }
                else
                {
                    if (this.noImplicitAny)
                    {
                        var name = Ast.GetNameOfDeclaration(sig.declaration);
                        if ((name != null))
                        {
                            this.error(name, Diagnostics.X_0_implicitly_has_return_type_any_because_it_does_not_have_a_return_type_annotation_and_is_referenced_directly_or_indirectly_in_one_of_its_return_expressions, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name));
                        }
                        else
                        {
                            this.error(sig.declaration, Diagnostics.Function_implicitly_has_return_type_any_because_it_does_not_have_a_return_type_annotation_and_is_referenced_directly_or_indirectly_in_one_of_its_return_expressions);
                        }
                    }
                }
            }

            t = this.anyType;
        }

        if ((sig.resolvedReturnType == null))
        {
            sig.resolvedReturnType = t;
        }

        return sig.resolvedReturnType;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20455:1.
    private Type getNonCircularReturnTypeOfSignature(Signature sig)
    {
        if (this.isResolvingReturnTypeOfSignature(sig))
        {
            return this.anyType;
        }

        return this.getReturnTypeOfSignature(sig);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20462:1.
    private Type getReturnTypeFromAnnotation(Node declaration)
    {
        if (Ast.IsConstructorDeclaration(declaration))
        {
            throw unsupported("getReturnTypeFromAnnotation: constructor");
        }

        var returnType = declaration.Type();
        if ((returnType != null))
        {
            return this.getTypeFromTypeNode(returnType);
        }

        if ((Ast.IsGetAccessorDeclaration(declaration) && !Ast.HasDynamicName(declaration)))
        {
            throw unsupported("getReturnTypeFromAnnotation: accessor");
        }

        return this.getReturnTypeOfFullSignature(declaration);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20476:1.
    private Signature getSignatureOfFullSignatureType(Node node)
    {
        if (((Ast.IsInJSFile(node) && (((Ast.IsFunctionDeclaration(node) || Ast.IsMethodDeclaration(node)) || Ast.IsFunctionExpressionOrArrowFunction(node)))) && (node.FunctionLikeData().FullSignature != null)))
        {
            return this.getSingleCallSignature(this.getTypeFromTypeNode(node.FunctionLikeData().FullSignature));
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20495:1.
    private Type getReturnTypeOfFullSignature(Node node)
    {
        {
            var signature = this.getSignatureOfFullSignatureType(node);
            if ((signature != null))
            {
                return this.getReturnTypeOfSignature(signature);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20711:1.
    private bool functionHasImplicitReturn(Node fn)
    {
        var endFlowNode = fn.BodyData().EndFlowNode;
        return ((endFlowNode != null) && this.isReachableFlowNode(endFlowNode));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20716:1.
    private static bool mayReturnNever(Node fn)
    {
        switch (fn.Kind)
        {
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            {
                return true;
            }

            case Kind.MethodDeclaration:
            {
                return Ast.IsObjectLiteralExpression(fn.Parent);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    // checker.go:20530. Async/generator branches are excluded by admission and guarded here.
    private Type getReturnTypeFromBody(Node fn, CheckMode checkMode)
    {
        var body = fn.Body();
        if (body == null)
            return errorType;
        var functionFlags = Ast.GetFunctionFlags(fn);
        if ((functionFlags & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            throw unsupported("getReturnTypeFromBody: async/generator");
        Type returnType;
        if (!Ast.IsBlock(body))
        {
            returnType = checkExpressionCachedEx(body, checkMode & ~CheckMode.SkipGenericFunctions);
            if (isConstContext(body))
                returnType = getRegularTypeOfLiteralType(returnType);
        }
        else
        {
            var(types, isNeverReturning) = checkAndAggregateReturnExpressionTypes(fn, checkMode);
            if (isNeverReturning)
                return neverType;
            if ((types?.Count ?? 0) == 0)
            {
                var contextualReturnType = getContextualReturnType(fn);
                return contextualReturnType != null && someType(contextualReturnType, t => (t.flags & TypeFlags.Undefined) != 0) ? undefinedType : voidType;
            }

            returnType = getUnionTypeEx(types, UnionReduction.Subtype, null, null);
        }

        if (returnType != null)
        {
            reportErrorsFromWidening(fn, returnType, WideningKind.FunctionReturn);
            if (isUnitType(returnType))
            {
                var contextualSignature = getContextualSignatureForFunctionLikeDeclaration(fn);
                Type contextualType = null;
                if (contextualSignature != null)
                    contextualType = contextualSignature == getSignatureFromDeclaration(fn) ? returnType : instantiateContextualType(getReturnTypeOfSignature(contextualSignature), fn);
                returnType = getWidenedLiteralLikeTypeForContextualReturnTypeIfNeeded(returnType, contextualType, false);
            }

            returnType = getWidenedType(returnType);
        }

        return returnType ?? voidType;
    }

    // checker.go:20663. The async unwrapping arm is rejected before aggregation.
    private (List<Type>, bool) checkAndAggregateReturnExpressionTypes(Node fn, CheckMode checkMode)
    {
        var functionFlags = Ast.GetFunctionFlags(fn);
        if ((functionFlags & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            throw unsupported("checkAndAggregateReturnExpressionTypes: async/generator");
        List<Type> aggregatedTypes = null;
        bool hasReturnWithNoExpression = functionHasImplicitReturn(fn), hasReturnOfTypeNever = false;
        Ast.ForEachReturnStatement(fn.Body(), returnStatement =>
        {
            var expr = returnStatement.Expression();
            if (expr == null)
            {
                hasReturnWithNoExpression = true;
                return false;
            }

            expr = Ast.SkipParentheses(expr);
            if (Ast.IsCallExpression(expr) && Ast.IsIdentifier(expr.Expression()) && checkExpressionCached(expr.Expression()).symbol == getMergedSymbol(fn.Symbol()) && (!Ast.IsFunctionExpressionOrArrowFunction(fn.Symbol().ValueDeclaration) || isConstantReference(expr.Expression())))
            {
                hasReturnOfTypeNever = true;
                return false;
            }

            var t = checkExpressionCachedEx(expr, checkMode & ~CheckMode.SkipGenericFunctions);
            if ((t.flags & TypeFlags.Never) != 0)
                hasReturnOfTypeNever = true;
            if (isConstContext(expr))
                t = getRegularTypeOfLiteralType(t);
            if (!(aggregatedTypes?.Contains(t) ?? false))
                aggregatedTypes = GoRuntime.Append(aggregatedTypes, t);
            return false;
        });
        if ((aggregatedTypes?.Count ?? 0) == 0 && !hasReturnWithNoExpression && (hasReturnOfTypeNever || mayReturnNever(fn)))
            return (null, true);
        if (strictNullChecks && (aggregatedTypes?.Count ?? 0) != 0 && hasReturnWithNoExpression && !aggregatedTypes.Contains(undefinedType))
            aggregatedTypes = GoRuntime.Append(aggregatedTypes, undefinedType);
        return (aggregatedTypes, false);
    }

    // checker.go:20811; ordinary returns retain literal contextual widening.
    private Type getWidenedLiteralLikeTypeForContextualReturnTypeIfNeeded(Type t, Type contextualSignatureReturnType, bool isAsync)
    {
        if (isAsync)
            throw unsupported("getWidenedLiteralLikeTypeForContextualReturnTypeIfNeeded: async");
        if (t != null && isUnitType(t))
            t = getWidenedLiteralLikeTypeForContextualType(t, contextualSignatureReturnType);
        return t;
    }

    // checker.go:30214/30260. No async/generator or annotated accessor/constructor admission.
    private Type getContextualReturnType(Node functionDecl)
    {
        var returnType = getReturnTypeFromAnnotation(functionDecl);
        if (returnType != null)
            return returnType;
        var signature = getContextualSignatureForFunctionLikeDeclaration(functionDecl);
        if (signature != null && !isResolvingReturnTypeOfSignature(signature))
        {
            if (Ast.GetFunctionFlags(functionDecl) != FunctionFlags.Normal)
                throw unsupported("getContextualReturnType: async/generator");
            return getReturnTypeOfSignature(signature);
        }

        var iife = Ast.GetImmediatelyInvokedFunctionExpression(functionDecl);
        return iife != null ? getContextualType(iife) : null;
    }

    private Signature getContextualSignatureForFunctionLikeDeclaration(Node node) => Ast.IsFunctionExpressionOrArrowFunction(node) || Ast.IsObjectLiteralMethod(node) ? getContextualSignature(node) : null;
    // checker.go:26144.
    private Type getUnionOrIntersectionType(List<Type> types, bool isUnion, UnionReduction unionReduction) => isUnion ? getUnionTypeEx(types, unionReduction, null, null) : getIntersectionType(types);
    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20939:1.
    private TypePredicate getTypePredicateFromBody(Node fn)
    {
        switch (fn.Kind)
        {
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            {
                return null;
            }
        }

        var functionFlags = Ast.GetFunctionFlags(fn);
        if ((functionFlags != FunctionFlags.Normal))
        {
            return null;
        }

        Node singleReturn = default;
        var body = fn.Body();
        if (((body != null) && !Ast.IsBlock(body)))
        {
            singleReturn = body;
        }
        else
        {
            var bailedEarly = Ast.ForEachReturnStatement(body, (returnStatement) =>
            {
                if (((singleReturn != null) || (returnStatement.Expression() == null)))
                {
                    return true;
                }

                singleReturn = returnStatement.Expression();
                return false;
            });
            if (((bailedEarly || (singleReturn == null)) || this.functionHasImplicitReturn(fn)))
            {
                return null;
            }
        }

        return this.checkIfExpressionRefinesAnyParameter(fn, singleReturn);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20969:1.
    private TypePredicate checkIfExpressionRefinesAnyParameter(Node fn, Node expr)
    {
        expr = Ast.SkipParentheses(expr);
        var returnType = this.checkExpressionCached(expr);
        if (((returnType.flags & TypeFlags.Boolean) == 0))
        {
            return null;
        }

        {
            foreach (var range4 in GoRuntime.Range(fn.Parameters()))
            {
                var i = range4.Key;
                var param = range4.Value;
                var initType = this.getTypeOfSymbol(param.Symbol());
                if ((((((initType == null) || ((initType.flags & TypeFlags.Boolean) != 0)) || !Ast.IsIdentifier(param.Name())) || this.isSymbolAssigned(param.Symbol())) || isRestParameter(param)))
                {
                    continue;
                }

                var trueType = this.checkIfExpressionRefinesParameter(fn, expr, param, initType);
                if ((trueType != null))
                {
                    return this.newTypePredicate(TypePredicateKind.Identifier, param.Name().Text(), (int)(i), trueType);
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:20989:1.
    private Type checkIfExpressionRefinesParameter(Node fn, Node expr, Node param, Type initType)
    {
        var antecedent = getFlowNodeOfNode(expr);
        if (((antecedent == null) && Ast.IsReturnStatement(expr.Parent)))
        {
            antecedent = getFlowNodeOfNode(expr.Parent);
        }

        if ((antecedent == null))
        {
            antecedent = new FlowNode
            {
                Flags = FlowFlags.Start
            };
        }

        var trueCondition = new FlowNode
        {
            Flags = FlowFlags.TrueCondition,
            Node = expr,
            Antecedent = antecedent
        };
        var trueType = this.getFlowTypeOfReferenceEx(param.Name(), initType, initType, fn, trueCondition);
        if ((trueType == initType))
        {
            return null;
        }

        var falseCondition = new FlowNode
        {
            Flags = FlowFlags.FalseCondition,
            Node = expr,
            Antecedent = antecedent
        };
        var falseSubtype = this.getReducedType(this.getFlowTypeOfReferenceEx(param.Name(), initType, trueType, fn, falseCondition));
        if (((falseSubtype.flags & TypeFlags.Never) != 0))
        {
            return trueType;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:28321:1.
    private static bool hasRestParameter(Node signature)
    {
        var last = Core.LastOrNil(signature.Parameters());
        return ((last != null) && isRestParameter(last));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/checker.go:28326:1.
    internal static bool isRestParameter(Node param)
    {
        return (param.AsParameterDeclaration().DotDotDotToken != null);
    }
}

public sealed partial class Checker
{
    private Signature getContextualSignature(Node node)
    {
        var t = this.getApparentTypeOfContextualType(node, ContextFlags.Signature);
        if ((t == null))
        {
            return null;
        }

        if (((t.flags & TypeFlags.Union) == 0))
        {
            return this.getContextualCallSignature(t, node);
        }

        List<Signature> signatureList = default;
        var types = t.Types();
        {
            foreach (var current in GoRuntime.RangeValues(types))
            {
                var signature = this.getContextualCallSignature(current, node);
                if ((signature != null))
                {
                    if (((GoRuntime.Len(signatureList) != 0) && (this.compareSignaturesIdentical(signatureList[0], signature, false, true, true, this.compareTypesIdentical) == Ternary.False)))
                    {
                        return null;
                    }

                    signatureList = GoRuntime.Append(signatureList, signature);
                }
            }
        }

        switch (GoRuntime.Len(signatureList))
        {
            case 0:
            {
                return null;
            }

            case 1:
            {
                return signatureList[0];
            }
        }

        return this.createUnionSignature(signatureList[0], signatureList);
    }
}

public sealed partial class Checker
{
    private Signature createUnionSignature(Signature sig, List<Signature> unionSignatures)
    {
        var result = this.cloneSignature(sig);
        result.composite = new CompositeSignature
        {
            isUnion = true,
            signatures = unionSignatures
        };
        result.target = null;
        result.mapper = null;
        return result;
    }
}

public sealed partial class Checker
{
    private Signature getContextualCallSignature(Type t, Node node)
    {
        var signatures = this.getSignaturesOfType(t, SignatureKind.Call);
        var applicableByArity = Core.Filter(signatures, (s) =>
        {
            return !this.isAritySmaller(s, node);
        });
        if ((GoRuntime.Len(applicableByArity) == 1))
        {
            return applicableByArity[0];
        }

        return this.getIntersectedSignatures(applicableByArity);
    }
}

public sealed partial class Checker
{
    private Signature getIntersectedSignatures(List<Signature> signatures)
    {
        if (!this.noImplicitAny)
        {
            return null;
        }

        Signature combined = default;
        {
            foreach (var sig in GoRuntime.RangeValues(signatures))
            {
                switch (true)
                {
                    case true when ((combined == sig) || (combined == null)):
                    {
                        combined = sig;
                        break;
                    }

                    case true when this.compareTypeParametersIdentical(combined.typeParameters, sig.typeParameters):
                    {
                        combined = this.combineUnionOrIntersectionMemberSignatures(combined, sig, false);
                        break;
                    }

                    default:
                    {
                        return null;
                    }
                }
            }
        }

        return combined;
    }
}

public sealed partial class Checker
{
    private bool isAritySmaller(Signature signature, Node target)
    {
        var parameters = target.Parameters();
        var targetParameterCount = 0;
        while ((targetParameterCount < GoRuntime.Len(parameters)))
        {
            var param = parameters[targetParameterCount];
            if ((((param.Initializer() != null) || (param.QuestionToken() != null)) || hasDotDotDotToken(param)))
            {
                break;
            }

            targetParameterCount++;
        }

        if (((GoRuntime.Len(parameters) != 0) && Ast.IsThisParameter(parameters[0])))
        {
            targetParameterCount--;
        }

        return (!this.hasEffectiveRestParameter(signature) && (this.getParameterCount(signature) < targetParameterCount));
    }
}

public sealed partial class Checker
{
    private void assignContextualParameterTypes(Signature sig, Signature context)
    {
        if ((GoRuntime.Len(context.typeParameters) != 0))
        {
            if ((GoRuntime.Len(sig.typeParameters) != 0))
            {
                return;
            }

            sig.typeParameters = context.typeParameters;
        }

        if ((context.thisParameter != null))
        {
            var parameter = sig.thisParameter;
            if (((parameter == null) || ((parameter.ValueDeclaration != null) && (parameter.ValueDeclaration.Type() == null))))
            {
                if ((parameter == null))
                {
                    sig.thisParameter = this.createSymbolWithType(context.thisParameter, null);
                }

                this.assignParameterType(sig.thisParameter, this.getTypeOfSymbol(context.thisParameter));
            }
        }

        var length = (GoRuntime.Len(sig.parameters) - Core.IfElse(signatureHasRestParameter(sig), 1, 0));
        {
            foreach (var i in GoRuntime.RangeKeys(length))
            {
                var parameter2 = sig.parameters[i];
                var declaration = parameter2.ValueDeclaration;
                if ((declaration.Type() == null))
                {
                    var t = this.tryGetTypeAtPosition(context, i);
                    if (((t != null) && (declaration.Initializer() != null)))
                    {
                        var initializerType = this.checkDeclarationInitializer(declaration, CheckMode.Normal, null);
                        if (!this.isTypeAssignableTo(initializerType, t))
                        {
                            initializerType = this.widenTypeInferredFromInitializer(declaration, initializerType);
                            if (this.isTypeAssignableTo(t, initializerType))
                            {
                                t = initializerType;
                            }
                        }
                    }

                    this.assignParameterType(parameter2, t);
                }
            }
        }

        if (signatureHasRestParameter(sig))
        {
            var parameter3 = Core.LastOrNil(sig.parameters);
            if ((((parameter3.ValueDeclaration != null) && (parameter3.ValueDeclaration.Type() == null)) || ((parameter3.ValueDeclaration == null) && ((parameter3.CheckFlags & CheckFlags.DeferredType) != 0))))
            {
                var contextualParameterType = this.getRestTypeAtPosition(context, length, false);
                this.assignParameterType(parameter3, contextualParameterType);
            }
        }
    }
}

public sealed partial class Checker
{
    private void assignNonContextualParameterTypes(Signature signature)
    {
        if ((signature.thisParameter != null))
        {
            this.assignParameterType(signature.thisParameter, null);
        }

        {
            foreach (var parameter in GoRuntime.RangeValues(signature.parameters))
            {
                this.assignParameterType(parameter, null);
            }
        }
    }
}

public sealed partial class Checker
{
    private void assignParameterType(Symbol parameter, Type contextualType)
    {
        var links = this.valueSymbolLinks.Get(parameter);
        if ((links.resolvedType != null))
        {
            return;
        }

        var declaration = parameter.ValueDeclaration;
        var t = contextualType;
        if ((t == null))
        {
            if ((declaration != null))
            {
                t = this.getWidenedTypeForVariableLikeDeclaration(declaration, true);
            }
            else
            {
                t = this.getTypeOfSymbol(parameter);
            }
        }

        links.resolvedType = this.addOptionalityEx(t, false, (((declaration != null) && (declaration.Initializer() == null)) && isOptionalDeclaration(declaration)));
        if (((declaration != null) && !Ast.IsIdentifier(declaration.Name())))
        {
            throw unsupported("assignParameterType: binding pattern");
        }
    }
}

public sealed partial class Checker
{
    internal Type getTypeOfParameter(Symbol symbol)
    {
        var declaration = symbol.ValueDeclaration;
        return this.addOptionalityEx(this.getTypeOfSymbol(symbol), false, ((declaration != null) && (((declaration.Initializer() != null) || isOptionalDeclaration(declaration)))));
    }
}

public sealed partial class Checker
{
    private void reportImplicitAny(Node declaration, Type t, WideningKind wideningKind)
    {
        if ((Ast.IsInJSFile(declaration) && true))
        {
            return;
        }

        var typeAsString = this.typeToStringByteString(this.getWidenedType(t));
        Message diagnostic = default;
        switch (declaration.Kind)
        {
            case Kind.BinaryExpression:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            {
                diagnostic = Core.IfElse(this.noImplicitAny, Diagnostics.Member_0_implicitly_has_an_1_type, Diagnostics.Member_0_implicitly_has_an_1_type_but_a_better_type_may_be_inferred_from_usage);
                break;
            }

            case Kind.Parameter:
            {
                var param = declaration.AsParameterDeclaration();
                if (Ast.IsIdentifier(param.Name()))
                {
                    var name = param.Name().AsIdentifier();
                    var originalKeywordKind = TypeScript.Scanner.Scanner.IdentifierToKeywordKind(name);
                    if ((((((Ast.IsCallSignatureDeclaration(declaration.Parent) || Ast.IsMethodSignatureDeclaration(declaration.Parent)) || Ast.IsFunctionTypeNode(declaration.Parent))) && (declaration.Parent.Parameters()?.Contains(declaration) == true)) && ((Ast.IsTypeNodeKind(originalKeywordKind) || (this.nameResolver.Resolve(declaration, name.Text, SymbolFlags.Type, null, true, false) != null)))))
                    {
                        var newName = "arg" + (declaration.Parent.Parameters()?.IndexOf(declaration) ?? -1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var typeName = (TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(param.Name()) + Core.IfElse((param.DotDotDotToken != null), "[]", ""));
                        this.errorOrSuggestion(this.noImplicitAny, declaration, Diagnostics.Parameter_has_a_name_but_no_type_Did_you_mean_0_Colon_1, newName, typeName);
                        return;
                    }
                }

                switch (true)
                {
                    case true when (param.DotDotDotToken != null):
                    {
                        if (this.noImplicitAny)
                        {
                            diagnostic = Diagnostics.Rest_parameter_0_implicitly_has_an_any_type;
                        }
                        else
                        {
                            diagnostic = Diagnostics.Rest_parameter_0_implicitly_has_an_any_type_but_a_better_type_may_be_inferred_from_usage;
                        }

                        break;
                    }

                    case true when this.noImplicitAny:
                    {
                        diagnostic = Diagnostics.Parameter_0_implicitly_has_an_1_type;
                        break;
                    }

                    default:
                    {
                        diagnostic = Diagnostics.Parameter_0_implicitly_has_an_1_type_but_a_better_type_may_be_inferred_from_usage;
                        break;
                    }
                }

                break;
            }

            case Kind.BindingElement:
            {
                diagnostic = Diagnostics.Binding_element_0_implicitly_has_an_1_type;
                if (!this.noImplicitAny)
                {
                    return;
                }

                break;
            }

            case Kind.FunctionDeclaration:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            {
                if ((this.noImplicitAny && (declaration.Name() == null)))
                {
                    if ((wideningKind == WideningKind.GeneratorYield))
                    {
                        this.error(declaration, Diagnostics.Generator_implicitly_has_yield_type_0_Consider_supplying_a_return_type_annotation, typeAsString);
                    }
                    else
                    {
                        this.error(declaration, Diagnostics.Function_expression_which_lacks_return_type_annotation_implicitly_has_an_0_return_type, typeAsString);
                    }

                    return;
                }

                switch (true)
                {
                    case true when !this.noImplicitAny:
                    {
                        diagnostic = Diagnostics.X_0_implicitly_has_an_1_return_type_but_a_better_type_may_be_inferred_from_usage;
                        break;
                    }

                    case true when ((declaration.Flags & NodeFlags.Reparsed) != 0):
                    {
                        {
                            var name2 = TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(Ast.GetNameOfDeclaration(declaration));
                            if ((name2 != ""))
                            {
                                this.error(declaration, Diagnostics.X_0_which_lacks_return_type_annotation_implicitly_has_an_1_return_type, name2, typeAsString);
                            }
                            else
                            {
                                this.error(declaration, Diagnostics.This_overload_implicitly_returns_the_type_0_because_it_lacks_a_return_type_annotation, typeAsString);
                            }
                        }

                        return;
                    }

                    case true when (wideningKind == WideningKind.GeneratorYield):
                    {
                        diagnostic = Diagnostics.X_0_which_lacks_return_type_annotation_implicitly_has_an_1_yield_type;
                        break;
                    }

                    default:
                    {
                        diagnostic = Diagnostics.X_0_which_lacks_return_type_annotation_implicitly_has_an_1_return_type;
                        break;
                    }
                }

                break;
            }

            case Kind.MappedType:
            {
                if (this.noImplicitAny)
                {
                    this.error(declaration, Diagnostics.Mapped_object_type_implicitly_has_an_any_template_type);
                }

                return;
            }

            default:
            {
                if (this.noImplicitAny)
                {
                    diagnostic = Diagnostics.Variable_0_implicitly_has_an_1_type;
                }
                else
                {
                    diagnostic = Diagnostics.Variable_0_implicitly_has_an_1_type_but_a_better_type_may_be_inferred_from_usage;
                }

                break;
            }
        }

        this.errorOrSuggestion(this.noImplicitAny, declaration, diagnostic, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(Ast.GetNameOfDeclaration(declaration)), typeAsString);
    }
}

public sealed partial class Checker
{
    private void reportErrorsFromWidening(Node declaration, Type t, WideningKind wideningKind)
    {
        if ((this.noImplicitAny && ((t.objectFlags & ObjectFlags.ContainsWideningType) != 0)))
        {
            if (((wideningKind == WideningKind.Normal) || (Ast.IsFunctionLikeDeclaration(declaration) && this.shouldReportErrorsFromWideningWithContextualSignature(declaration, wideningKind))))
            {
                if (!this.reportWideningErrorsInType(t))
                {
                    this.reportImplicitAny(declaration, t, wideningKind);
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private bool reportWideningErrorsInType(Type t)
    {
        var errorReported = false;
        if (((t.objectFlags & ObjectFlags.ContainsWideningType) != 0))
        {
            if (((t.flags & TypeFlags.Union) != 0))
            {
                if (Core.Some(t.Types(), this.isEmptyObjectType))
                {
                    errorReported = true;
                }
                else
                {
                    {
                        foreach (var s in GoRuntime.RangeValues(t.Types()))
                        {
                            errorReported = (errorReported || this.reportWideningErrorsInType(s));
                        }
                    }
                }
            }
            else
            {
                if (this.isArrayOrTupleType(t))
                {
                    {
                        foreach (var range6 in GoRuntime.Range(this.getTypeArguments(t)))
                        {
                            var _2 = range6.Key;
                            var s2 = range6.Value;
                            errorReported = (errorReported || this.reportWideningErrorsInType(s2));
                        }
                    }
                }
                else
                {
                    if (isObjectLiteralType(t))
                    {
                        {
                            foreach (var range7 in GoRuntime.Range(this.getPropertiesOfObjectType(t)))
                            {
                                var _3 = range7.Key;
                                var p = range7.Value;
                                var s3 = this.getTypeOfSymbol(p);
                                if (((s3.objectFlags & ObjectFlags.ContainsWideningType) != 0))
                                {
                                    errorReported = this.reportWideningErrorsInType(s3);
                                    if (!errorReported)
                                    {
                                        var valueDeclaration = Core.Find(p.Declarations, (d) =>
                                        {
                                            var valueDeclaration2 = d.Symbol().ValueDeclaration;
                                            return ((valueDeclaration2 != null) && (valueDeclaration2.Parent == t.symbol.ValueDeclaration));
                                        });
                                        if ((valueDeclaration != null))
                                        {
                                            this.error(valueDeclaration, Diagnostics.Object_literal_s_property_0_implicitly_has_an_1_type, this.symbolToString(p), this.typeToStringByteString(this.getWidenedType(s3)));
                                            errorReported = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return errorReported;
    }
}

public sealed partial class Checker
{
    private Signature instantiateSignature(Signature sig, TypeMapper m)
    {
        return this.instantiateSignatureEx(sig, m, (m == this.permissiveMapper));
    }
}

public sealed partial class Checker
{
    private Signature instantiateSignatureEx(Signature sig, TypeMapper m, bool eraseTypeParameters)
    {
        List<Type> freshTypeParameters = default;
        if (((GoRuntime.Len(sig.typeParameters) != 0) && !eraseTypeParameters))
        {
            freshTypeParameters = Core.Map(sig.typeParameters, this.cloneTypeParameter);
            m = this.combineTypeMappers(newTypeMapper(sig.typeParameters, freshTypeParameters), m);
            {
                foreach (var tp in GoRuntime.RangeValues(freshTypeParameters))
                {
                    tp.AsTypeParameter().mapper = m;
                }
            }
        }

        var result = this.newSignature((sig.flags & SignatureFlags.PropagatingFlags), sig.declaration, freshTypeParameters, this.instantiateSymbol(sig.thisParameter, m), this.instantiateSymbols(sig.parameters, m), null, null, (int)(sig.minArgumentCount));
        result.target = sig;
        result.mapper = m;
        return result;
    }
}

public sealed partial class Checker
{
    private Type getContextuallyTypedParameterType(Node parameter)
    {
        var fn = parameter.Parent;
        if (!this.isContextSensitiveFunctionOrObjectLiteralMethod(fn))
        {
            return null;
        }

        var iife = Ast.GetImmediatelyInvokedFunctionExpression(fn);
        if ((iife != null))
        {
            var args = this.getEffectiveCallArguments(iife);
            var indexOfParameter = GoSlices.Index(fn.Parameters(), parameter);
            if (hasDotDotDotToken(parameter))
            {
                throw unsupported("getContextuallyTypedParameterType: rest");
            }

            var links = this.signatureLinks.Get(iife);
            var cached = links.resolvedSignature;
            links.resolvedSignature = this.anySignature;
            Type t = default;
            switch (true)
            {
                case true when (indexOfParameter < GoRuntime.Len(args)):
                {
                    t = this.getWidenedLiteralType(this.checkExpression(args[indexOfParameter]));
                    break;
                }

                case true when (parameter.Initializer() != null):
                {
                    t = null;
                    break;
                }

                default:
                {
                    t = this.undefinedWideningType;
                    break;
                }
            }

            links.resolvedSignature = cached;
            return t;
        }

        var contextualSignature = this.getContextualSignature(fn);
        if ((contextualSignature != null))
        {
            var index = (GoSlices.Index(fn.Parameters(), parameter) - Core.IfElse((Ast.GetThisParameter(fn) != null), 1, 0));
            if ((hasDotDotDotToken(parameter) && (Core.LastOrNil(fn.Parameters()) == parameter)))
            {
                throw unsupported("getContextuallyTypedParameterType: rest");
            }

            return this.tryGetTypeAtPosition(contextualSignature, index);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool isContextSensitiveFunctionOrObjectLiteralMethod(Node fn)
    {
        return (((Ast.IsFunctionExpressionOrArrowFunction(fn) || Ast.IsObjectLiteralMethod(fn))) && this.isContextSensitiveFunctionLikeDeclaration(fn));
    }
}

public sealed partial class Checker
{
    private Type getParameterTypeOfFullSignature(Node node, Node parameter)
    {
        var signature = getSignatureOfFullSignatureType(node);
        if (signature != null)
        {
            int pos = node.Parameters().IndexOf(parameter);
            if (isRestParameter(parameter))
                throw unsupported("getParameterTypeOfFullSignature: rest");
            return getTypeAtPosition(signature, pos);
        }

        return null;
    }
}

public sealed partial class Checker
{
    // checker.go:93/172. Full-preimage managed cache keys preserve source equality.
    private enum WideningKind
    {
        Normal,
        FunctionReturn,
        GeneratorNext,
        GeneratorYield
    }

    internal readonly record struct CachedSignatureKey(Signature sig, CacheHashKey key);
    internal readonly Dictionary<CachedSignatureKey, Signature> cachedSignatures = new();
    internal static readonly CacheHashKey SignatureKeyErased = new("LQ=="), SignatureKeyCanonical = new("Kg=="), SignatureKeyBase = new("Iw=="), SignatureKeyInner = new("PA=="), SignatureKeyOuter = new("Pg==");
    internal TypeMapper permissiveMapper, reportUnreliableMapper;
    // checker.go:19800/19813: nongeneric signatures require no erasure/canonical cache.
    private Signature getErasedSignature(Signature signature)
    {
        if ((signature.typeParameters?.Count ?? 0) == 0)
            return signature;
        throw unsupported("getErasedSignature: source-generic signature");
    }

    private Signature getCanonicalSignature(Signature signature)
    {
        if ((signature.typeParameters?.Count ?? 0) == 0)
            return signature;
        throw unsupported("getCanonicalSignature: source-generic signature");
    }

    private Signature cloneSignature(Signature sig)
    {
        var result = newSignature(sig.flags & SignatureFlags.PropagatingFlags, sig.declaration, sig.typeParameters, sig.thisParameter, sig.parameters, null, null, sig.minArgumentCount);
        result.target = sig.target;
        result.mapper = sig.mapper;
        result.composite = sig.composite;
        return result;
    }

    internal Signature getSingleCallSignature(Type t) => getSingleSignature(t, SignatureKind.Call, false);
    internal Signature getSingleSignature(Type t, SignatureKind kind, bool allowMembers)
    {
        if ((t.flags & TypeFlags.Object) != 0)
        {
            var resolved = resolveStructuredTypeMembers(t);
            if (allowMembers || (resolved.properties?.Count ?? 0) == 0 && (resolved.indexInfos?.Count ?? 0) == 0)
            {
                if (kind == SignatureKind.Call && (resolved.CallSignatures()?.Count ?? 0) == 1 && (resolved.ConstructSignatures()?.Count ?? 0) == 0)
                    return resolved.CallSignatures()[0];
                if (kind == SignatureKind.Construct && (resolved.ConstructSignatures()?.Count ?? 0) == 1 && (resolved.CallSignatures()?.Count ?? 0) == 0)
                    return resolved.ConstructSignatures()[0];
            }
        }

        return null;
    }

    private bool shouldReportErrorsFromWideningWithContextualSignature(Node declaration, WideningKind wideningKind)
    {
        var signature = getContextualSignatureForFunctionLikeDeclaration(declaration);
        if (signature == null)
            return true;
        var returnType = getReturnTypeOfSignature(signature);
        var flags = Ast.GetFunctionFlags(declaration);
        if (wideningKind == WideningKind.FunctionReturn)
        {
            if (flags != FunctionFlags.Normal)
                throw unsupported("shouldReportErrorsFromWideningWithContextualSignature: async/generator");
            return isGenericType(returnType);
        }

        if (wideningKind is WideningKind.GeneratorNext or WideningKind.GeneratorYield)
            throw unsupported("shouldReportErrorsFromWideningWithContextualSignature: generator");
        return false;
    }

    // checker.go:14283/14300. Suggestions remain separate from semantic errors.
    private void errorOrSuggestion(bool isError, Node location, Message message, params object[] args) => addErrorOrSuggestion(isError, NewDiagnosticForNode(location, message, args));
    private void addErrorOrSuggestion(bool isError, Diagnostic diagnostic)
    {
        if (isError)
            addDiagnostic(diagnostic);
        else
        {
            diagnostic.SetCategory(DiagnosticCategory.Suggestion);
            suggestionDiagnostics.Add(diagnostic);
        }
    }
}

public sealed partial class Checker
{
    private void checkParameter(Node node)
    {
        this.checkGrammarModifiers(node);
        this.checkVariableLikeDeclaration(node);
        var fn = Ast.GetContainingFunction(node);
        string paramName = "";
        if (((node.Name() != null) && Ast.IsIdentifier(node.Name())))
        {
            paramName = node.Name().Text();
        }

        if (Ast.HasSyntacticModifier(node, ModifierFlags.ParameterPropertyModifier))
        {
            if (this.shouldCheckErasableSyntax(node))
            {
                this.error(node, Diagnostics.This_syntax_is_not_allowed_when_erasableSyntaxOnly_is_enabled);
            }

            if (!((Ast.IsConstructorDeclaration(fn) && Ast.NodeIsPresent(fn.Body()))))
            {
                this.error(node, Diagnostics.A_parameter_property_is_only_allowed_in_a_constructor_implementation);
            }

            if ((Ast.IsConstructorDeclaration(fn) && (paramName == "constructor")))
            {
                this.error(node.Name(), Diagnostics.X_constructor_cannot_be_used_as_a_parameter_property_name);
            }
        }

        if (((((node.Initializer() == null) && isOptionalDeclaration(node)) && Ast.IsBindingPattern(node.Name())) && (fn.Body() != null)))
        {
            this.error(node, Diagnostics.A_binding_pattern_parameter_cannot_be_optional_in_an_implementation_signature);
        }

        if (((paramName == "this") || (paramName == "new")))
        {
            if ((GoSlices.Index(fn.Parameters(), node) != 0))
            {
                this.error(node, Diagnostics.A_0_parameter_must_be_the_first_parameter, paramName);
            }

            if (((Ast.IsConstructorDeclaration(fn) || Ast.IsConstructSignatureDeclaration(fn)) || Ast.IsConstructorTypeNode(fn)))
            {
                this.error(node, Diagnostics.A_constructor_cannot_have_a_this_parameter);
            }

            if (Ast.IsArrowFunction(fn))
            {
                this.error(node, Diagnostics.An_arrow_function_cannot_have_a_this_parameter);
            }

            if (Ast.IsAccessor(fn))
            {
                this.error(node, Diagnostics.X_get_and_set_accessors_cannot_declare_this_parameters);
            }
        }

        if (hasDotDotDotToken(node))
            throw unsupported("checkParameter: rest");
    }
}

public sealed partial class Checker
{
    private void checkSignatureDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.IndexSignature:
            {
                throw unsupported("checkSignatureDeclaration: index signature");
            }

            case Kind.FunctionType:
            case Kind.FunctionDeclaration:
            case Kind.ConstructorType:
            case Kind.CallSignature:
            case Kind.Constructor:
            case Kind.ConstructSignature:
            {
                this.checkGrammarFunctionLikeDeclaration(node);
                break;
            }
        }

        var functionFlags = Ast.GetFunctionFlags(node);
        if ((functionFlags & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            throw unsupported("checkSignatureDeclaration: async/generator");
        this.checkTypeParameters(node.TypeParameters());
        this.checkUnmatchedJSDocParameters(node);
        this.checkSourceElements(node.Parameters());
        var returnTypeNode = node.Type();
        if ((returnTypeNode != null))
        {
            this.checkSourceElement(returnTypeNode);
        }

        if ((this.noImplicitAny && (returnTypeNode == null)))
        {
            switch (node.Kind)
            {
                case Kind.ConstructSignature:
                {
                    this.error(node, Diagnostics.Construct_signature_which_lacks_return_type_annotation_implicitly_has_an_any_return_type);
                    break;
                }

                case Kind.CallSignature:
                {
                    this.error(node, Diagnostics.Call_signature_which_lacks_return_type_annotation_implicitly_has_an_any_return_type);
                    break;
                }
            }
        }

        if (!Ast.IsIndexSignatureDeclaration(node))
        {
            this.registerForUnusedIdentifiersCheck(node);
        }
    }
}

public sealed partial class Checker
{
    private void checkFunctionDeclaration(Node node)
    {
        this.checkFunctionOrMethodDeclaration(node);
        if ((Ast.GetFunctionFlags(node) & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            throw unsupported("checkFunctionDeclaration: generator/async");
        this.checkCollisionsForDeclarationName(node, node.Name());
    }
}

public sealed partial class Checker
{
    private void checkFunctionOrMethodDeclaration(Node node)
    {
        if ((node.Decorators()?.Count ?? 0) != 0)
            throw unsupported("checkFunctionOrMethodDeclaration: decorators");
        this.checkSignatureDeclaration(node);
        var functionFlags = Ast.GetFunctionFlags(node);
        if (((node.Name() != null) && Ast.IsComputedPropertyName(node.Name())))
        {
            throw unsupported("checkFunctionOrMethodDeclaration: computed property");
        }

        if (!Ast.HasDynamicName(node))
        {
            var symbol = this.getSymbolOfDeclaration(node);
            var localSymbol = Core.OrElse(node.LocalSymbol(), symbol);
            if (((node.Flags & NodeFlags.JavaScriptFile) == 0))
            {
                this.checkFunctionOrConstructorSymbol(localSymbol);
            }

            if ((symbol.Parent != null))
            {
                this.checkFunctionOrConstructorSymbol(symbol);
            }
        }

        var body = node.Body();
        this.checkSourceElement(body);
        this.checkAllCodePathsInNonVoidFunctionReturnOrThrow(node, this.getReturnTypeFromAnnotation(node));
        if ((node.FunctionLikeData().FullSignature != null))
        {
            this.checkSourceElement(node.FunctionLikeData().FullSignature);
            if ((this.getContextualCallSignature(this.getTypeFromTypeNode(node.FunctionLikeData().FullSignature), node) == null))
            {
                this.error(node.FunctionLikeData().FullSignature, Diagnostics.A_JSDoc_type_tag_on_a_function_must_have_a_signature_with_the_correct_number_of_arguments);
            }
        }

        if ((node.Type() == null))
        {
            if ((Ast.NodeIsMissing(body) && !isPrivateWithinAmbient(node)))
            {
                this.reportImplicitAny(node, this.anyType, WideningKind.Normal);
            }

            if ((functionFlags & FunctionFlags.Generator) != 0)
                throw unsupported("checkFunctionOrMethodDeclaration: generator");
        }
    }
}

public sealed partial class Checker
{
    private void checkFunctionOrConstructorSymbol(Symbol symbol)
    {
        {
            var links = this.valueSymbolLinks.Get(symbol);
            if (!links.functionOrConstructorChecked)
            {
                links.functionOrConstructorChecked = true;
                this.checkFunctionOrConstructorSymbolWorker(symbol);
            }
        }
    }
}

public sealed partial class Checker
{
    private void checkFunctionOrConstructorSymbolWorker(Symbol symbol)
    {
        var flagsToCheck = ((((ModifierFlags.Export | ModifierFlags.Ambient) | ModifierFlags.Private) | ModifierFlags.Protected) | ModifierFlags.Abstract);
        var someNodeFlags = ModifierFlags.None;
        var allNodeFlags = flagsToCheck;
        var someHaveQuestionToken = false;
        var allHaveQuestionToken = true;
        var hasOverloads = false;
        Node bodyDeclaration = default;
        Node lastSeenNonAmbientDeclaration = default;
        Node previousDeclaration = default;
        var declarations = symbol.Declarations;
        var isConstructor = ((symbol.Flags & SymbolFlags.Constructor) != 0);
        var duplicateFunctionDeclaration = false;
        var multipleConstructorImplementation = false;
        var hasNonAmbientClass = false;
        List<Node> functionDeclarations = default;
        Func<List<Node>, Node, Node> getCanonicalOverload = (overloads, implementation) =>
        {
            var implementationSharesContainerWithFirstOverload = ((implementation != null) && (implementation.Parent == overloads[0].Parent));
            if (implementationSharesContainerWithFirstOverload)
            {
                return implementation;
            }

            return overloads[0];
        };
        Action<List<Node>, Node, ModifierFlags, ModifierFlags, ModifierFlags> checkFlagAgreementBetweenOverloads = (overloads2, implementation2, flagsToCheck2, someOverloadFlags, allOverloadFlags) =>
        {
            var someButNotAllOverloadFlags = (someOverloadFlags ^ allOverloadFlags);
            if ((someButNotAllOverloadFlags != 0))
            {
                var canonicalFlags = this.getEffectiveDeclarationFlags(getCanonicalOverload(overloads2, implementation2), flagsToCheck2);
                var groups = new Dictionary<SourceFile, List<Node>>();
                {
                    foreach (var overload in GoRuntime.RangeValues(overloads2))
                    {
                        var sourceFile = Ast.GetSourceFileOfNode(overload);
                        groups[sourceFile] = GoRuntime.Append(groups[sourceFile], overload);
                    }
                }

                {
                    foreach (var range2 in GoRuntime.Range(groups))
                    {
                        var _2 = range2.Key;
                        var overloadsInFile = range2.Value;
                        var canonicalFlagsForFile = this.getEffectiveDeclarationFlags(getCanonicalOverload(overloadsInFile, implementation2), flagsToCheck2);
                        {
                            foreach (var range3 in GoRuntime.Range(overloadsInFile))
                            {
                                var _3 = range3.Key;
                                var overload2 = range3.Value;
                                var deviation = (this.getEffectiveDeclarationFlags(overload2, flagsToCheck2) ^ canonicalFlags);
                                var deviationInFile = (this.getEffectiveDeclarationFlags(overload2, flagsToCheck2) ^ canonicalFlagsForFile);
                                switch (true)
                                {
                                    case true when ((deviationInFile & ModifierFlags.Export) != 0):
                                    {
                                        this.error(Ast.GetNameOfDeclaration(overload2), Diagnostics.Overload_signatures_must_all_be_exported_or_non_exported);
                                        break;
                                    }

                                    case true when ((deviationInFile & ModifierFlags.Ambient) != 0):
                                    {
                                        this.error(Ast.GetNameOfDeclaration(overload2), Diagnostics.Overload_signatures_must_all_be_ambient_or_non_ambient);
                                        break;
                                    }

                                    case true when ((deviation & ((ModifierFlags.Private | ModifierFlags.Protected))) != 0):
                                    {
                                        this.error(Core.OrElse(Ast.GetNameOfDeclaration(overload2), overload2), Diagnostics.Overload_signatures_must_all_be_public_private_or_protected);
                                        break;
                                    }

                                    case true when ((deviation & ModifierFlags.Abstract) != 0):
                                    {
                                        this.error(Ast.GetNameOfDeclaration(overload2), Diagnostics.Overload_signatures_must_all_be_abstract_or_non_abstract);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        Action<List<Node>, Node, bool, bool> checkQuestionTokenAgreementBetweenOverloads = (overloads3, implementation3, someHaveQuestionToken2, allHaveQuestionToken2) =>
        {
            if ((someHaveQuestionToken2 != allHaveQuestionToken2))
            {
                var canonicalHasQuestionToken = isOptionalDeclaration(getCanonicalOverload(overloads3, implementation3));
                {
                    foreach (var range4 in GoRuntime.Range(overloads3))
                    {
                        var _4 = range4.Key;
                        var o = range4.Value;
                        if ((isOptionalDeclaration(o) != canonicalHasQuestionToken))
                        {
                            this.error(Ast.GetNameOfDeclaration(o), Diagnostics.Overload_signatures_must_all_be_optional_or_required);
                        }
                    }
                }
            }
        };
        Action<Node> reportImplementationExpectedError = (node) =>
        {
            var name = node.Name();
            if (((name != null) && Ast.NodeIsMissing(name)))
            {
                return;
            }

            var seen = false;
            Node subsequentNode = default;
            node.Parent.ForEachChild((child) =>
            {
                if (seen)
                {
                    subsequentNode = child;
                    return true;
                }

                seen = (child == node);
                return false;
            });
            if (((subsequentNode != null) && (subsequentNode.Pos() == node.End())))
            {
                if ((subsequentNode.Kind == node.Kind))
                {
                    var subsequentName = subsequentNode.Name();
                    var errorNode = Core.OrElse(subsequentName, subsequentNode);
                    if ((((name != null) && (subsequentName != null)) && (((((Ast.IsPrivateIdentifier(name) && Ast.IsPrivateIdentifier(subsequentName)) && (name.Text() == subsequentName.Text())) || ((Ast.IsComputedPropertyName(name) && Ast.IsComputedPropertyName(subsequentName)) && this.isTypeIdenticalTo(this.checkComputedPropertyName(name), this.checkComputedPropertyName(subsequentName)))) || ((Ast.IsPropertyNameLiteral(name) && Ast.IsPropertyNameLiteral(subsequentName)) && (name.Text() == subsequentName.Text()))))))
                    {
                        var reportError = (((Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node))) && (Ast.IsStatic(node) != Ast.IsStatic(subsequentNode)));
                        if (reportError)
                        {
                            var diagnostic = Core.IfElse(Ast.IsStatic(node), Diagnostics.Function_overload_must_be_static, Diagnostics.Function_overload_must_not_be_static);
                            this.error(errorNode, diagnostic);
                        }

                        return;
                    }

                    if (Ast.NodeIsPresent(subsequentNode.Body()))
                    {
                        this.error(errorNode, Diagnostics.Function_implementation_name_must_be_0, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(name));
                        return;
                    }
                }
            }

            var errorNode2 = Core.OrElse(name, node);
            if (isConstructor)
            {
                this.error(errorNode2, Diagnostics.Constructor_implementation_is_missing);
            }
            else
            {
                if (Ast.HasSyntacticModifier(node, ModifierFlags.Abstract))
                {
                    this.error(errorNode2, Diagnostics.All_declarations_of_an_abstract_method_must_be_consecutive);
                }
                else
                {
                    this.error(errorNode2, Diagnostics.Function_implementation_is_missing_or_not_immediately_following_the_declaration);
                }
            }
        };
        {
            foreach (var range5 in GoRuntime.Range(declarations))
            {
                var _5 = range5.Key;
                var node2 = range5.Value;
                var inAmbientContext = ((node2.Flags & NodeFlags.Ambient) != 0);
                var inAmbientContextOrInterface = (inAmbientContext || ((node2.Parent != null) && ((Ast.IsInterfaceDeclaration(node2.Parent) || Ast.IsTypeLiteralNode(node2.Parent)))));
                if (inAmbientContextOrInterface)
                {
                    previousDeclaration = null;
                }

                if ((Ast.IsClassLike(node2) && !inAmbientContext))
                {
                    hasNonAmbientClass = true;
                }

                if ((((Ast.IsFunctionDeclaration(node2) || Ast.IsMethodDeclaration(node2)) || Ast.IsMethodSignatureDeclaration(node2)) || Ast.IsConstructorDeclaration(node2)))
                {
                    functionDeclarations = GoRuntime.Append(functionDeclarations, node2);
                    var currentNodeFlags = this.getEffectiveDeclarationFlags(node2, flagsToCheck);
                    someNodeFlags |= currentNodeFlags;
                    allNodeFlags &= currentNodeFlags;
                    someHaveQuestionToken = (someHaveQuestionToken || isOptionalDeclaration(node2));
                    allHaveQuestionToken = (allHaveQuestionToken && isOptionalDeclaration(node2));
                    var bodyIsPresent = Ast.NodeIsPresent(node2.Body());
                    if ((bodyIsPresent && (bodyDeclaration != null)))
                    {
                        if (isConstructor)
                        {
                            multipleConstructorImplementation = true;
                        }
                        else
                        {
                            duplicateFunctionDeclaration = true;
                        }
                    }
                    else
                    {
                        if (((((previousDeclaration != null) && (previousDeclaration.Parent == node2.Parent)) && (previousDeclaration.End() != node2.Pos())) && ((previousDeclaration.Flags & NodeFlags.Reparsed) == 0)))
                        {
                            reportImplementationExpectedError(previousDeclaration);
                        }
                    }

                    if (bodyIsPresent)
                    {
                        if ((bodyDeclaration == null))
                        {
                            bodyDeclaration = node2;
                        }
                    }
                    else
                    {
                        hasOverloads = true;
                    }

                    previousDeclaration = node2;
                    if (!inAmbientContextOrInterface)
                    {
                        lastSeenNonAmbientDeclaration = node2;
                    }
                }
            }
        }

        if (multipleConstructorImplementation)
        {
            {
                foreach (var range6 in GoRuntime.Range(functionDeclarations))
                {
                    var _6 = range6.Key;
                    var declaration = range6.Value;
                    this.error(declaration, Diagnostics.Multiple_constructor_implementations_are_not_allowed);
                }
            }
        }

        if (duplicateFunctionDeclaration)
        {
            {
                foreach (var range7 in GoRuntime.Range(functionDeclarations))
                {
                    var _7 = range7.Key;
                    var declaration2 = range7.Value;
                    this.error(Core.OrElse(Ast.GetNameOfDeclaration(declaration2), declaration2), Diagnostics.Duplicate_function_implementation);
                }
            }
        }

        if ((((hasNonAmbientClass && !isConstructor) && ((symbol.Flags & SymbolFlags.Function) != 0)) && (GoRuntime.Len(declarations) != 0)))
        {
            List<Diagnostic> relatedDiagnostics = default;
            {
                foreach (var range8 in GoRuntime.Range(declarations))
                {
                    var _8 = range8.Key;
                    var declaration3 = range8.Value;
                    if (Ast.IsClassDeclaration(declaration3))
                    {
                        relatedDiagnostics = GoRuntime.Append(relatedDiagnostics, createDiagnosticForNode(declaration3, Diagnostics.Consider_adding_a_declare_modifier_to_this_class));
                    }
                }
            }

            {
                foreach (var range9 in GoRuntime.Range(declarations))
                {
                    var _9 = range9.Key;
                    var declaration4 = range9.Value;
                    Message diagnostic2 = default;
                    switch (declaration4.Kind)
                    {
                        case Kind.ClassDeclaration:
                        {
                            diagnostic2 = Diagnostics.Class_declaration_cannot_implement_overload_list_for_0;
                            break;
                        }

                        case Kind.FunctionDeclaration:
                        {
                            diagnostic2 = Diagnostics.Function_with_bodies_can_only_merge_with_classes_that_are_ambient;
                            break;
                        }
                    }

                    if ((diagnostic2 != null))
                    {
                        this.error(Core.OrElse(Ast.GetNameOfDeclaration(declaration4), declaration4), diagnostic2, symbol.Name).SetRelatedInfo(relatedDiagnostics);
                    }
                }
            }
        }

        if (((((lastSeenNonAmbientDeclaration != null) && (lastSeenNonAmbientDeclaration.Body() == null)) && !Ast.HasSyntacticModifier(lastSeenNonAmbientDeclaration, ModifierFlags.Abstract)) && !isOptionalDeclaration(lastSeenNonAmbientDeclaration)))
        {
            reportImplementationExpectedError(lastSeenNonAmbientDeclaration);
        }

        if (hasOverloads)
        {
            checkFlagAgreementBetweenOverloads(declarations, bodyDeclaration, flagsToCheck, someNodeFlags, allNodeFlags);
            checkQuestionTokenAgreementBetweenOverloads(declarations, bodyDeclaration, someHaveQuestionToken, allHaveQuestionToken);
            if ((bodyDeclaration != null))
            {
                var signatures = this.getSignaturesOfSymbol(symbol);
                var bodySignature = this.getSignatureFromDeclaration(bodyDeclaration);
                {
                    foreach (var range10 in GoRuntime.Range(signatures))
                    {
                        var _10 = range10.Key;
                        var signature = range10.Value;
                        if (!this.isImplementationCompatibleWithOverload(bodySignature, signature))
                        {
                            var errorNode3 = signature.declaration;
                            this.error(errorNode3, Diagnostics.This_overload_signature_is_not_compatible_with_its_implementation_signature).AddRelatedInfo(createDiagnosticForNode(bodyDeclaration, Diagnostics.The_implementation_signature_is_declared_here));
                            break;
                        }
                    }
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private void checkAllCodePathsInNonVoidFunctionReturnOrThrow(Node fn, Type returnType)
    {
        var functionFlags = Ast.GetFunctionFlags(fn);
        Type t = default;
        if ((returnType != null))
        {
            t = this.unwrapReturnType(returnType, functionFlags);
        }

        if (((t != null) && ((this.maybeTypeOfKind(t, TypeFlags.Void) || ((t.flags & ((TypeFlags.Any | TypeFlags.Undefined))) != 0)))))
        {
            return;
        }

        if ((((Ast.IsMethodSignatureDeclaration(fn) || Ast.NodeIsMissing(fn.Body())) || !Ast.IsBlock(fn.Body())) || !this.functionHasImplicitReturn(fn)))
        {
            return;
        }

        var hasExplicitReturn = ((fn.Flags & NodeFlags.HasExplicitReturn) != 0);
        var errorNode = fn.Type();
        if ((errorNode == null))
        {
            {
                var data = fn.FunctionLikeData();
                if (((data != null) && (data.FullSignature != null)))
                {
                    errorNode = data.FullSignature;
                }
            }
        }

        if ((errorNode == null))
        {
            errorNode = fn;
        }

        switch (true)
        {
            case true when ((t != null) && ((t.flags & TypeFlags.Never) != 0)):
            {
                this.error(errorNode, Diagnostics.A_function_returning_never_cannot_have_a_reachable_end_point);
                break;
            }

            case true when ((t != null) && !hasExplicitReturn):
            {
                this.error(errorNode, Diagnostics.A_function_whose_declared_type_is_neither_undefined_void_nor_any_must_return_a_value);
                break;
            }

            case true when (((t != null) && this.strictNullChecks) && !this.isTypeAssignableTo(this.undefinedType, t)):
            {
                this.error(errorNode, Diagnostics.Function_lacks_ending_return_statement_and_return_type_does_not_include_undefined);
                break;
            }

            case true when (this.compilerOptions.NoImplicitReturns == Tristate.True):
            {
                if ((t == null))
                {
                    if (!hasExplicitReturn)
                    {
                        return;
                    }

                    var inferredReturnType = this.getReturnTypeOfSignature(this.getSignatureFromDeclaration(fn));
                    if (this.isUnwrappedReturnTypeUndefinedVoidOrAny(fn, inferredReturnType))
                    {
                        return;
                    }
                }

                this.error(errorNode, Diagnostics.Not_all_code_paths_return_a_value);
                break;
            }
        }
    }
}

public sealed partial class Checker
{
    private bool isUnwrappedReturnTypeUndefinedVoidOrAny(Node fn, Type returnType)
    {
        var t = this.unwrapReturnType(returnType, Ast.GetFunctionFlags(fn));
        return ((t != null) && ((this.maybeTypeOfKind(t, TypeFlags.Void) || ((t.flags & ((TypeFlags.Any | TypeFlags.Undefined))) != 0))));
    }
}

public sealed partial class Checker
{
    private void checkIfStatement(Node node)
    {
        this.checkGrammarStatementInAmbientContext(node);
        var t = this.checkTruthinessExpression(node.Expression(), CheckMode.Normal);
        var data = node.AsIfStatement();
        this.checkTestingKnownTruthyCallableOrAwaitableOrEnumMemberType(node.Expression(), t, data.ThenStatement);
        this.checkSourceElement(data.ThenStatement);
        if (Ast.IsEmptyStatement(data.ThenStatement))
        {
            this.error(data.ThenStatement, Diagnostics.The_body_of_an_if_statement_cannot_be_the_empty_statement);
        }

        this.checkSourceElement(data.ElseStatement);
    }
}

public sealed partial class Checker
{
    private void checkReturnStatement(Node node)
    {
        var exprNode = node.Expression();
        var exprType = this.undefinedType;
        if ((exprNode != null))
        {
            exprType = this.checkExpressionCached(exprNode);
        }

        if (this.checkGrammarStatementInAmbientContext(node))
        {
            return;
        }

        var container = getContainingFunctionOrClassStaticBlock(node);
        if (((container != null) && Ast.IsClassStaticBlockDeclaration(container)))
        {
            this.grammarErrorOnFirstToken(node, Diagnostics.A_return_statement_cannot_be_used_inside_a_class_static_block);
            return;
        }

        if ((container == null))
        {
            this.grammarErrorOnFirstToken(node, Diagnostics.A_return_statement_can_only_be_used_within_a_function_body);
            return;
        }

        var signature = this.getSignatureFromDeclaration(container);
        var returnType = this.getReturnTypeOfSignature(signature);
        var functionFlags = Ast.GetFunctionFlags(container);
        if (((this.strictNullChecks || (exprNode != null)) || ((returnType.flags & TypeFlags.Never) != 0)))
        {
            if (Ast.IsSetAccessorDeclaration(container))
            {
                if ((exprNode != null))
                {
                    this.error(node, Diagnostics.Setters_cannot_return_a_value);
                }
            }
            else
            {
                if (Ast.IsConstructorDeclaration(container))
                {
                    if (((exprNode != null) && !this.checkTypeAssignableToAndOptionallyElaborate(exprType, returnType, node, exprNode, null, null)))
                    {
                        this.error(node, Diagnostics.Return_type_of_constructor_signature_must_be_assignable_to_the_instance_type_of_the_class);
                    }
                }
                else
                {
                    if ((this.getReturnTypeFromAnnotation(container) != null))
                    {
                        var unwrappedReturnType = Core.OrElse(this.unwrapReturnType(returnType, functionFlags), returnType);
                        this.checkReturnExpression(container, unwrappedReturnType, node, node.Expression(), exprType, false);
                    }
                }
            }
        }
        else
        {
            if (((!Ast.IsConstructorDeclaration(container) && this.compilerOptions.NoImplicitReturns == Tristate.True) && !this.isUnwrappedReturnTypeUndefinedVoidOrAny(container, returnType)))
            {
                this.error(node, Diagnostics.Not_all_code_paths_return_a_value);
            }
        }
    }
}

public sealed partial class Checker
{
    private void checkReturnExpression(Node container, Type unwrappedReturnType, Node node, Node expr, Type exprType, bool inConditionalExpression)
    {
        var unwrappedExprType = exprType;
        var functionFlags = Ast.GetFunctionFlags(container);
        if ((expr != null))
        {
            var unwrappedExpr = Ast.SkipParentheses(expr);
            if (Ast.IsConditionalExpression(unwrappedExpr))
            {
                var whenTrue = unwrappedExpr.AsConditionalExpression().WhenTrue;
                var whenFalse = unwrappedExpr.AsConditionalExpression().WhenFalse;
                this.checkReturnExpression(container, unwrappedReturnType, node, whenTrue, this.checkExpression(whenTrue), true);
                this.checkReturnExpression(container, unwrappedReturnType, node, whenFalse, this.checkExpression(whenFalse), true);
                return;
            }
        }

        var inReturnStatement = (node.Kind == Kind.ReturnStatement);
        if (((functionFlags & FunctionFlags.Async) != 0))
        {
            throw unsupported("checkReturnExpression: async");
        }

        var effectiveExpr = expr;
        if ((expr != null))
        {
            effectiveExpr = this.getEffectiveCheckNode(expr);
        }

        var errorNode = Core.IfElse((inReturnStatement && !inConditionalExpression), node, effectiveExpr);
        this.checkTypeAssignableToAndOptionallyElaborate(unwrappedExprType, unwrappedReturnType, errorNode, effectiveExpr, null, null);
    }
}

public sealed partial class Checker
{
    private void checkThrowStatement(Node node)
    {
        var throwExpr = node.Expression();
        if (!this.checkGrammarStatementInAmbientContext(node))
        {
            if ((Ast.IsIdentifier(throwExpr) && (GoRuntime.Len(throwExpr.Text()) == 0)))
            {
                this.grammarErrorAtPos(node, throwExpr.Pos(), 0, Diagnostics.Line_break_not_permitted_here);
            }
        }

        this.checkExpression(throwExpr);
    }
}

public sealed partial class Checker
{
    private void checkExpressionStatement(Node node)
    {
        this.checkGrammarStatementInAmbientContext(node);
        this.checkExpression(node.Expression());
    }
}

public sealed partial class Checker
{
    private Type reportUnreliableWorker(Type t)
    {
        if ((((t == this.markerSuperType) || (t == this.markerSubType)) || (t == this.markerOtherType)))
        {
            this.reliabilityFlags |= RelationComparisonResult.ReportsUnreliable;
        }

        return t;
    }
}

public sealed partial class Checker
{
    private bool shouldCheckErasableSyntax(Node node)
    {
        return (this.erasableSyntaxOnly && !Ast.IsInJSFile(node));
    }
}

public sealed partial class Checker
{
    private ModifierFlags getEffectiveDeclarationFlags(Node n, ModifierFlags flagsToCheck)
    {
        var flags = this.getCombinedModifierFlagsCached(n);
        if ((((!Ast.IsInterfaceDeclaration(n.Parent) && !Ast.IsClassDeclaration(n.Parent)) && !Ast.IsClassExpression(n.Parent)) && ((n.Flags & NodeFlags.Ambient) != 0)))
        {
            var container = getEnclosingContainer(n);
            if (((((container != null) && ((container.Flags & NodeFlags.ExportContext) != 0)) && ((flags & ModifierFlags.Ambient) == 0)) && !((Ast.IsModuleBlock(n.Parent) && Ast.IsGlobalScopeAugmentation(n.Parent.Parent)))))
            {
                flags |= ModifierFlags.Export;
            }

            flags |= ModifierFlags.Ambient;
        }

        return (flags & flagsToCheck);
    }
}

public sealed partial class Checker
{
    private bool isImplementationCompatibleWithOverload(Signature implementation, Signature overload)
    {
        var erasedSource = this.getErasedSignature(implementation);
        var erasedTarget = this.getErasedSignature(overload);
        var sourceReturnType = this.getReturnTypeOfSignature(erasedSource);
        var targetReturnType = this.getReturnTypeOfSignature(erasedTarget);
        if ((((targetReturnType == this.voidType) || this.isTypeRelatedTo(targetReturnType, sourceReturnType, this.assignableRelation)) || this.isTypeRelatedTo(sourceReturnType, targetReturnType, this.assignableRelation)))
        {
            return this.isSignatureAssignableTo(erasedSource, erasedTarget, true);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private void checkTypeParameters(List<Node> declarations)
    {
        if (declarations != null && declarations.Count != 0)
            throw unsupported("checkTypeParameters: source generic parameters");
    }
}

public sealed partial class Checker
{
    private Node getEffectiveCheckNode(Node argument)
    {
        var flags = Core.IfElse(Ast.IsInJSFile(argument), ((OuterExpressionKinds.Parentheses | OuterExpressionKinds.Satisfies) | OuterExpressionKinds.ExcludeJSDocTypeAssertion), (OuterExpressionKinds.Parentheses | OuterExpressionKinds.Satisfies));
        return Ast.SkipOuterExpressions(argument, flags);
    }
}

public sealed partial class Checker
{
    private bool isNullOrUndefined(Node node)
    {
        var expr = Ast.SkipParentheses(node);
        switch (expr.Kind)
        {
            case Kind.NullKeyword:
            {
                return true;
            }

            case Kind.Identifier:
            {
                return (this.getResolvedSymbol(expr) == this.undefinedSymbol);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isGlobalSymbolConstructor(Node node)
    {
        var symbol = this.getSymbolOfNode(node);
        var globalSymbol = this.getGlobalESSymbolConstructorTypeSymbolOrNil();
        return ((globalSymbol != null) && (symbol == globalSymbol));
    }
}

public sealed partial class Checker
{
    private List<Symbol> instantiateSymbols(List<Symbol> symbols, TypeMapper m)
    {
        return this.instantiateList(symbols, m, instantiateSymbol);
    }
}

public sealed partial class Checker
{
    private Type permissiveMapperWorker(Type t)
    {
        if (((t.flags & TypeFlags.TypeParameter) != 0))
        {
            return this.wildcardType;
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Func<Symbol> getGlobalTypeSymbolResolver(string name, bool reportErrors)
    {
        return memoize(() =>
        {
            return this.getGlobalSymbol(name, SymbolFlags.Type, Core.IfElse(reportErrors, Diagnostics.Cannot_find_global_type_0, null));
        });
    }
}

public sealed partial class Checker
{
    private static Diagnostic createDiagnosticForNode(Node node, Message message, params object[] args)
    {
        return NewDiagnosticForNode(node, message, args);
    }
}

public sealed partial class Checker
{
    private static bool signatureHasRestParameter(Signature sig)
    {
        return ((sig.flags & SignatureFlags.HasRestParameter) != 0);
    }
}

public sealed partial class Checker
{
    private static bool isUnitType(Type t)
    {
        return ((t.flags & TypeFlags.Unit) != 0);
    }
}

public sealed partial class Checker
{
    private bool isUnitLikeType(Type t)
    {
        t = this.getBaseConstraintOrType(t);
        if (((t.flags & TypeFlags.Intersection) != 0))
        {
            return Core.Some(t.AsIntersectionType().types, isUnitType);
        }

        return isUnitType(t);
    }
}

public sealed partial class Checker
{
    private static bool someType(Type t, Func<Type, bool> f)
    {
        if (((t.flags & TypeFlags.Union) != 0))
        {
            return Core.Some(t.Types(), f);
        }

        return f(t);
    }
}

public sealed partial class Checker
{
    private Type removeType(Type t, Type targetType)
    {
        if (((t.flags & TypeFlags.Union) == 0))
        {
            if ((t == targetType))
            {
                return this.neverType;
            }

            return t;
        }

        {
            var origin = t.AsUnionType().origin;
            if ((((origin != null) && ((origin.flags & TypeFlags.Union) != 0)) && containsType(origin.Types(), targetType)))
            {
                return this.filterType(t, (t2) =>
                {
                    return (t2 != targetType);
                });
            }
        }

        var types = t.Types();
        {
            var tuple1 = GoSlices.BinarySearchFunc(types, targetType, CompareTypes);
            var i = tuple1.Item1;
            var ok = tuple1.Item2;
            if (ok)
            {
                if ((GoRuntime.Len(types) == 2))
                {
                    return types[(1 - i)];
                }

                var filtered = GoRuntime.Append(GoRuntime.Slice(types, 0, i), GoRuntime.Slice(types, (i + 1)));
                return this.getUnionTypeFromSortedList(filtered, (t.objectFlags & ((ObjectFlags.PrimitiveUnion | ObjectFlags.ContainsIntersections))), null, null);
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private bool isUniformUnionType(Type t)
    {
        if (((t.objectFlags & ObjectFlags.PrimitiveUnion) != 0))
        {
            if (((t.objectFlags & ObjectFlags.IsUniformEnumComputed) == 0))
            {
                t.objectFlags |= (ObjectFlags.IsUniformEnumComputed | Core.IfElse(this.computeIsUniformUnionType(t.Types()), ObjectFlags.IsUniformEnum, ObjectFlags.None));
            }

            return ((t.objectFlags & ObjectFlags.IsUniformEnum) != 0);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool computeIsUniformUnionType(List<Type> types)
    {
        Symbol enumSymbol = default;
        bool hasStringOrNumberLiteral = default;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                if (((t.flags & TypeFlags.EnumLike) != 0))
                {
                    if (hasStringOrNumberLiteral)
                    {
                        return false;
                    }

                    var parent = this.getParentOfSymbol(t.symbol);
                    if ((enumSymbol == null))
                    {
                        enumSymbol = parent;
                    }
                    else
                    {
                        if ((enumSymbol != parent))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (((t.flags & TypeFlags.StringOrNumberLiteral) != 0))
                    {
                        if ((enumSymbol != null))
                        {
                            return false;
                        }

                        hasStringOrNumberLiteral = true;
                    }
                }
            }
        }

        return true;
    }
}

public sealed partial class Checker
{
    private Type getNonUndefinedType(Type t)
    {
        var typeOrConstraint = t;
        if (someType(t, this.isGenericTypeWithUndefinedConstraint))
        {
            typeOrConstraint = this.mapType(t, (t2) =>
            {
                if (((t2.flags & TypeFlags.Instantiable) != 0))
                {
                    return this.getBaseConstraintOrType(t2);
                }

                return t2;
            });
        }

        return this.getTypeWithFacts(typeOrConstraint, TypeFacts.NEUndefined);
    }
}

public sealed partial class Checker
{
    private readonly bool erasableSyntaxOnly = false; // Fixed selected compiler profiles leave this option unset.
    private bool emitStandardClassFields;
    private readonly bool legacyDecorators = false; // Source decorators are rejected by admission.
    private Func<Symbol> getGlobalESSymbolConstructorTypeSymbolOrNil;
    private Func<Symbol> getGlobalESSymbolConstructorSymbolOrNil;
    private static Func<T> memoize<T>(Func<T> create)
    {
        T value = default;
        return () =>
        {
            if (create != null)
            {
                value = create();
                create = null;
            }

            return value;
        };
    }
}

public sealed partial class Checker
{
    private Type getReturnTypeOfSingleNonGenericSignature(Type funcType, SignatureKind kind)
    {
        var signature = this.getSingleSignature(funcType, kind, true);
        if (((signature != null) && (GoRuntime.Len(signature.typeParameters) == 0)))
        {
            return this.getReturnTypeOfSignature(signature);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type checkCallExpression(Node node, CheckMode checkMode)
    {
        this.checkGrammarTypeArguments(node, node.TypeArgumentList());
        var signature = this.getResolvedSignature(node, null, checkMode);
        if ((signature == this.resolvingSignature))
        {
            return this.silentNeverType;
        }

        this.checkDeprecatedSignature(signature, node);
        if ((node.Expression().Kind == Kind.SuperKeyword))
        {
            return this.voidType;
        }

        if (Ast.IsNewExpression(node))
        {
            throw unsupported("checkCallExpression: source new");
        }

        if ((Ast.IsInJSFile(node)))
        {
            throw unsupported("checkCallExpression: JS require");
        }

        var returnType = this.getReturnTypeOfSignature(signature);
        if ((((returnType.flags & TypeFlags.ESSymbolLike) != 0) && this.isSymbolOrSymbolForCall(node)))
        {
            return getESSymbolLikeTypeForNode(Ast.WalkUpParenthesizedExpressions(node.Parent));
        }

        if (((((Ast.IsCallExpression(node) && (node.QuestionDotToken() == null)) && Ast.IsExpressionStatement(node.Parent)) && ((returnType.flags & TypeFlags.Void) != 0)) && (this.getTypePredicateOfSignature(signature) != null)))
        {
            if (!Ast.IsDottedName(node.Expression()))
            {
                this.error(node.Expression(), Diagnostics.Assertions_require_the_call_target_to_be_an_identifier_or_qualified_name);
            }
            else
            {
                if ((this.getEffectsSignature(node) == null))
                {
                    var diagnostic = this.error(node.Expression(), Diagnostics.Assertions_require_every_name_in_the_call_target_to_be_declared_with_an_explicit_type_annotation);
                    this.getTypeOfDottedName(node.Expression(), diagnostic);
                }
            }
        }

        return returnType;
    }
}

public sealed partial class Checker
{
    private Signature getResolvedSignature(Node node, GoRef<List<Signature>> candidatesOutArray, CheckMode checkMode)
    {
        var links = this.signatureLinks.Get(node);
        var cached = links.resolvedSignature;
        if ((((cached != null) && (cached != this.resolvingSignature)) && (candidatesOutArray == null)))
        {
            return cached;
        }

        var saveResolutionStart = this.resolutionStart;
        if ((cached == null))
        {
            this.resolutionStart = GoRuntime.Len(this.typeResolutions);
        }

        links.resolvedSignature = this.resolvingSignature;
        var result = this.resolveSignature(node, candidatesOutArray, checkMode);
        this.resolutionStart = saveResolutionStart;
        if ((result != this.resolvingSignature))
        {
            if ((links.resolvedSignature != this.resolvingSignature))
            {
                result = links.resolvedSignature;
            }

            // Source loops are excluded; this is the original zero-flowLoopStack cache arm.
            links.resolvedSignature = result;
        }

        return result;
    }
}

public sealed partial class Checker
{
    private Signature resolveSignature(Node node, GoRef<List<Signature>> candidatesOutArray, CheckMode checkMode)
    {
        if (node.Kind == Kind.CallExpression)
            return resolveCallExpression(node, candidatesOutArray, checkMode);
        throw unsupported("resolveSignature: non-call syntax");
    }
}

public sealed partial class Checker
{
    private Signature resolveCallExpression(Node node, GoRef<List<Signature>> candidatesOutArray, CheckMode checkMode)
    {
        if ((node.Expression().Kind == Kind.SuperKeyword))
        {
            throw unsupported("resolveCallExpression: super");
        }

        if (Ast.IsImportCall(node))
        {
            throw unsupported("resolveCallExpression: import");
        }

        SignatureFlags callChainFlags = default;
        var funcType = this.checkExpression(node.Expression());
        if (isCallChain(node))
        {
            throw unsupported("resolveCallExpression: optional chain");
        }
        else
        {
            callChainFlags = SignatureFlags.None;
        }

        funcType = this.checkNonNullTypeWithReporter(funcType, node.Expression(), reportCannotInvokePossiblyNullOrUndefinedError);
        if ((funcType == this.silentNeverType))
        {
            return this.silentNeverSignature;
        }

        var apparentType = this.getApparentType(funcType);
        if (this.isErrorType(apparentType))
        {
            return this.resolveErrorCall(node);
        }

        var callSignatures = this.getSignaturesOfType(apparentType, SignatureKind.Call);
        var numConstructSignatures = GoRuntime.Len(this.getSignaturesOfType(apparentType, SignatureKind.Construct));
        if (this.isUntypedFunctionCall(funcType, apparentType, GoRuntime.Len(callSignatures), numConstructSignatures))
        {
            if ((!this.isErrorType(funcType) && (node.TypeArguments() != null)))
            {
                this.error(node, Diagnostics.Untyped_function_calls_may_not_accept_type_arguments);
            }

            return this.resolveUntypedCall(node);
        }

        if ((GoRuntime.Len(callSignatures) == 0))
        {
            if ((numConstructSignatures != 0))
            {
                this.error(node, Diagnostics.Value_of_type_0_is_not_callable_Did_you_mean_to_include_new, this.typeToStringByteString(funcType));
            }
            else
            {
                Diagnostic relatedInformation = default;
                if ((GoRuntime.Len(node.Arguments()) == 1))
                {
                    var text = Ast.GetSourceFileOfNode(node).Text();
                    var options = new SkipTriviaOptions
                    {
                        StopAfterLineBreak = true
                    };
                    if (StringUtil.IsLineBreak((int)(text[(TypeScript.Scanner.Scanner.SkipTriviaEx(text, node.Expression().End(), options) - 1)])))
                    {
                        relatedInformation = createDiagnosticForNode(node.Expression(), Diagnostics.Are_you_missing_a_semicolon);
                    }
                }

                this.invocationError(node.Expression(), apparentType, SignatureKind.Call, relatedInformation);
            }

            return this.resolveErrorCall(node);
        }

        if (((((checkMode & CheckMode.SkipGenericFunctions) != 0) && (GoRuntime.Len(node.TypeArguments()) == 0)) && Core.Some(callSignatures, s => GoRuntime.Len(s.typeParameters) != 0)))
        {
            throw unsupported("resolveCallExpression: source generic return");
        }

        return this.resolveCall(node, callSignatures, candidatesOutArray, checkMode, callChainFlags, null);
    }
}

public sealed partial class Checker
{
    private Signature resolveCall(Node node, List<Signature> signatures, GoRef<List<Signature>> candidatesOutArray, CheckMode checkMode, SignatureFlags callChainFlags, Message headMessage)
    {
        var isTaggedTemplate = (node.Kind == Kind.TaggedTemplateExpression);
        var isDecorator = (node.Kind == Kind.Decorator);
        var isJsxOpeningOrSelfClosingElement = Ast.IsJsxOpeningLikeElement(node);
        var isInstanceof = (node.Kind == Kind.BinaryExpression);
        var reportErrors = (!this.isInferencePartiallyBlocked && (candidatesOutArray == null));
        CallState s = new();
        s.node = node;
        if ((((!isDecorator && !isInstanceof) && node.Expression().Kind != Kind.SuperKeyword) && !Ast.IsJsxOpeningFragment(node)))
        {
            s.typeArguments = node.TypeArguments();
            if (((isTaggedTemplate || isJsxOpeningOrSelfClosingElement) || (node.Expression().Kind != Kind.SuperKeyword)))
            {
                this.checkSourceElements(s.typeArguments);
            }
        }

        s.candidates = this.reorderCandidates(signatures, callChainFlags);
        if ((candidatesOutArray != null))
        {
            candidatesOutArray.Value = s.candidates;
        }

        if ((GoRuntime.Len(s.candidates) == 0))
        {
            return this.unknownSignature;
        }

        s.args = this.getEffectiveCallArguments(node);
        s.isSingleNonGenericCandidate = ((GoRuntime.Len(s.candidates) == 1) && (GoRuntime.Len(s.candidates[0].typeParameters) == 0));
        if (((!isDecorator && !s.isSingleNonGenericCandidate) && Core.Some(s.args, this.isContextSensitive)))
        {
            s.argCheckMode = CheckMode.SkipContextSensitive;
        }
        else
        {
            s.argCheckMode = CheckMode.Normal;
        }

        s.signatureHelpTrailingComma = ((((checkMode & CheckMode.IsForSignatureHelp) != 0) && Ast.IsCallExpression(node)) && node.ArgumentList().HasTrailingComma());
        Signature result = default;
        s.recursiveResolution = (this.callResolutionStack?.Contains(s.node) == true);
        this.callResolutionStack = GoRuntime.Append(this.callResolutionStack, s.node);
        if ((GoRuntime.Len(s.candidates) > 1))
        {
            result = this.chooseOverload(s, this.subtypeRelation);
        }

        if ((result == null))
        {
            result = this.chooseOverload(s, this.assignableRelation);
        }

        this.callResolutionStack = GoRuntime.Slice(this.callResolutionStack, 0, (GoRuntime.Len(this.callResolutionStack) - 1));
        if ((result != null))
        {
            return result;
        }

        result = this.getCandidateForOverloadFailure(s.node, s.candidates, s.args, (candidatesOutArray != null), checkMode);
        this.signatureLinks.Get(node).resolvedSignature = result;
        if (reportErrors)
        {
            if (((headMessage == null) && isInstanceof))
            {
                headMessage = Diagnostics.The_left_hand_side_of_an_instanceof_expression_must_be_assignable_to_the_first_argument_of_the_right_hand_side_s_Symbol_hasInstance_method;
            }

            this.reportCallResolutionErrors(node, s, signatures, headMessage);
        }

        return result;
    }
}

public sealed partial class Checker
{
    private List<Signature> reorderCandidates(List<Signature> signatures, SignatureFlags callChainFlags)
    {
        Node lastParent = default;
        Symbol lastSymbol = default;
        int index = default;
        int cutoffIndex = default;
        int spliceIndex = default;
        var specializedIndex = -1;
        var result = GoRuntime.MakeSlice<Signature>(0);
        {
            foreach (var signature in GoRuntime.RangeValues(signatures))
            {
                Symbol symbol = default;
                Node parent = default;
                if ((signature.declaration != null))
                {
                    symbol = this.getSymbolOfDeclaration(signature.declaration);
                    parent = signature.declaration.Parent;
                }

                if (((lastSymbol == null) || (symbol == lastSymbol)))
                {
                    if (((lastParent != null) && (parent == lastParent)))
                    {
                        index = (index + 1);
                    }
                    else
                    {
                        lastParent = parent;
                        index = cutoffIndex;
                    }
                }
                else
                {
                    index = GoRuntime.Len(result);
                    cutoffIndex = GoRuntime.Len(result);
                    lastParent = parent;
                }

                lastSymbol = symbol;
                if (signatureHasLiteralTypes(signature))
                {
                    specializedIndex++;
                    spliceIndex = specializedIndex;
                    cutoffIndex++;
                }
                else
                {
                    spliceIndex = index;
                }

                if ((callChainFlags != 0))
                {
                    throw unsupported("reorderCandidates: optional chain");
                }

                result = GoRuntime.Append(GoRuntime.Append(GoRuntime.Slice(result, 0, spliceIndex), signature), GoRuntime.Slice(result, spliceIndex));
            }
        }

        return result;
    }
}

public sealed partial class Checker
{
    private Signature chooseOverload(CallState s, Relation relation)
    {
        s.candidatesForArgumentError = null;
        s.candidateForArgumentArityError = null;
        s.candidateForTypeArgumentError = null;
        if (s.isSingleNonGenericCandidate)
        {
            var candidate = s.candidates[0];
            if (((GoRuntime.Len(s.typeArguments) != 0) || !this.hasCorrectArity(s.node, s.args, candidate, s.signatureHelpTrailingComma)))
            {
                return null;
            }

            if (!this.isSignatureApplicable(s.node, s.args, candidate, relation, CheckMode.Normal, false, null))
            {
                s.candidatesForArgumentError = new List<Signature>
                {
                    candidate
                };
                return null;
            }

            return candidate;
        }

        {
            foreach (var range3 in GoRuntime.Range(s.candidates))
            {
                var candidateIndex = range3.Key;
                var candidate2 = range3.Value;
                if ((!this.hasCorrectTypeArgumentArity(candidate2, s.typeArguments) || !this.hasCorrectArity(s.node, s.args, candidate2, s.signatureHelpTrailingComma)))
                {
                    continue;
                }

                Signature checkCandidate = default;
                InferenceContext inferenceContext = default;
                if ((GoRuntime.Len(candidate2.typeParameters) != 0))
                {
                    throw unsupported("chooseOverload: source generic candidate");
                }
                else
                {
                    checkCandidate = candidate2;
                }

                if (!this.isSignatureApplicable(s.node, s.args, checkCandidate, relation, s.argCheckMode, false, null))
                {
                    s.candidatesForArgumentError = GoRuntime.Append(s.candidatesForArgumentError, checkCandidate);
                    continue;
                }

                if ((s.argCheckMode != 0))
                {
                    s.argCheckMode = CheckMode.Normal;
                    if ((inferenceContext != null))
                    {
                        throw unsupported("chooseOverload: generic inference");
                    }

                    if (!this.isSignatureApplicable(s.node, s.args, checkCandidate, relation, s.argCheckMode, false, null))
                    {
                        s.candidatesForArgumentError = GoRuntime.Append(s.candidatesForArgumentError, checkCandidate);
                        continue;
                    }
                }

                s.candidates[candidateIndex] = checkCandidate;
                return checkCandidate;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool hasCorrectArity(Node node, List<Node> args, Signature signature, bool signatureHelpTrailingComma)
    {
        if (Ast.IsJsxOpeningFragment(node))
        {
            return true;
        }

        int argCount = default;
        var callIsIncomplete = false;
        var effectiveParameterCount = this.getParameterCount(signature);
        var effectiveMinimumArguments = this.getMinArgumentCount(signature);
        switch (true)
        {
            case true when Ast.IsTaggedTemplateExpression(node):
            {
                throw unsupported("hasCorrectArity: excluded call syntax");
            }

            case true when Ast.IsDecorator(node):
            {
                throw unsupported("hasCorrectArity: excluded call syntax");
            }

            case true when Ast.IsBinaryExpression(node):
            {
                throw unsupported("hasCorrectArity: excluded call syntax");
            }

            case true when Ast.IsJsxOpeningLikeElement(node):
            {
                throw unsupported("hasCorrectArity: excluded call syntax");
            }

            case true when (Ast.IsNewExpression(node) && (node.ArgumentList() == null)):
            {
                throw unsupported("hasCorrectArity: excluded call syntax");
            }

            default:
            {
                if (signatureHelpTrailingComma)
                {
                    argCount = (GoRuntime.Len(args) + 1);
                }
                else
                {
                    argCount = GoRuntime.Len(args);
                }

                callIsIncomplete = (node.ArgumentList().End() == node.End());
                var spreadArgIndex = this.getSpreadArgumentIndex(args);
                if ((spreadArgIndex >= 0))
                {
                    throw unsupported("hasCorrectArity: spread");
                }

                break;
            }
        }

        if ((!this.hasEffectiveRestParameter(signature) && (argCount > effectiveParameterCount)))
        {
            return false;
        }

        if ((callIsIncomplete || (argCount >= effectiveMinimumArguments)))
        {
            return true;
        }

        {
            var i = argCount;
            for (; (i < effectiveMinimumArguments); i++)
            {
                var t = this.getTypeAtPosition(signature, i);
                if (((this.filterType(t, acceptsVoid).flags & TypeFlags.Never) != 0))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public sealed partial class Checker
{
    private bool isSignatureApplicable(Node node, List<Node> args, Signature signature, Relation relation, CheckMode checkMode, bool reportErrors, List<Diagnostic> diagnosticOutput)
    {
        if (Ast.IsJsxCallLike(node))
        {
            throw unsupported("isSignatureApplicable: JSX");
        }

        var thisType = this.getThisTypeOfSignature(signature);
        if ((((thisType != null) && (thisType != this.voidType)) && !((Ast.IsNewExpression(node) || (Ast.IsCallExpression(node) && Ast.IsSuperProperty(node.Expression()))))))
        {
            var thisArgumentNode = this.getThisArgumentOfCall(node);
            var thisArgumentType = this.getThisArgumentType(thisArgumentNode);
            Node errorNode = default;
            if (reportErrors)
            {
                errorNode = thisArgumentNode;
                if ((errorNode == null))
                {
                    errorNode = node;
                }
            }

            var headMessage = Diagnostics.The_this_context_of_type_0_is_not_assignable_to_method_s_this_of_type_1;
            if (!this.checkTypeRelatedToEx(thisArgumentType, thisType, relation, errorNode, headMessage, diagnosticOutput))
            {
                return false;
            }
        }

        var headMessage2 = Diagnostics.Argument_of_type_0_is_not_assignable_to_parameter_of_type_1;
        var restType = this.getNonArrayRestType(signature);
        int argCount = default;
        if ((restType != null))
        {
            argCount = Math.Min((this.getParameterCount(signature) - 1), GoRuntime.Len(args));
        }
        else
        {
            argCount = GoRuntime.Len(args);
        }

        {
            foreach (var i in GoRuntime.RangeKeys(argCount))
            {
                var arg = args[i];
                if (!Ast.IsOmittedExpression(arg))
                {
                    var paramType = this.getTypeAtPosition(signature, i);
                    var argType = this.checkExpressionWithContextualType(arg, paramType, null, checkMode);
                    Type checkArgType = default;
                    if (((checkMode & CheckMode.SkipContextSensitive) != 0))
                    {
                        checkArgType = this.getRegularTypeOfObjectLiteral(argType);
                    }
                    else
                    {
                        checkArgType = argType;
                    }

                    var effectiveCheckArgumentNode = this.getEffectiveCheckNode(arg);
                    if (!this.checkTypeRelatedToAndOptionallyElaborate(checkArgType, paramType, relation, Core.IfElse(reportErrors, effectiveCheckArgumentNode, null), effectiveCheckArgumentNode, headMessage2, diagnosticOutput))
                    {
                        this.maybeAddMissingAwaitInfo(arg, checkArgType, paramType, relation, reportErrors, diagnosticOutput);
                        return false;
                    }
                }
            }
        }

        if ((restType != null))
        {
            throw unsupported("isSignatureApplicable: non-array rest");
        }

        return true;
    }
}

public sealed partial class Checker
{
    private Signature getCandidateForOverloadFailure(Node node, List<Signature> candidates, List<Node> args, bool hasCandidatesOutArray, CheckMode checkMode)
    {
        this.checkNodeDeferred(node);
        if (((hasCandidatesOutArray || (GoRuntime.Len(candidates) == 1)) || Core.Some(candidates, (s) =>
        {
            return (GoRuntime.Len(s.typeParameters) != 0);
        })))
        {
            return this.pickLongestCandidateSignature(node, candidates, args, checkMode);
        }

        return this.createUnionOfSignaturesForOverloadFailure(candidates);
    }
}

public sealed partial class Checker
{
    private Signature pickLongestCandidateSignature(Node node, List<Signature> candidates, List<Node> args, CheckMode checkMode)
    {
        var argCount = GoRuntime.Len(args);
        if ((this.apparentArgumentCount != null))
        {
            argCount = this.apparentArgumentCount.Value;
        }

        var bestIndex = this.getLongestCandidateIndex(candidates, argCount);
        var candidate = candidates[bestIndex];
        var typeParameters = candidate.typeParameters;
        if ((GoRuntime.Len(typeParameters) == 0))
        {
            return candidate;
        }

        throw unsupported("pickLongestCandidateSignature: source generic candidate");
    }
}

public sealed partial class Checker
{
    private Signature createUnionOfSignaturesForOverloadFailure(List<Signature> candidates)
    {
        var thisParameters = Core.MapNonNil(candidates, (c) =>
        {
            return c.thisParameter;
        });
        Symbol thisParameter = default;
        if ((GoRuntime.Len(thisParameters) != 0))
        {
            thisParameter = this.createCombinedSymbolFromTypes(thisParameters, Core.Map(thisParameters, this.getTypeOfParameter));
        }

        var tuple5 = minAndMax(candidates, getNonRestParameterCount);
        var minArgumentCount = tuple5.Item1;
        var maxNonRestParam = tuple5.Item2;
        var parameters = GoRuntime.MakeSlice<Symbol>(0);
        {
            foreach (var i in GoRuntime.RangeKeys(maxNonRestParam))
            {
                var symbols = Core.MapNonNil(candidates, (s) =>
                {
                    if (signatureHasRestParameter(s))
                    {
                        if ((i < (GoRuntime.Len(s.parameters) - 1)))
                        {
                            return s.parameters[i];
                        }

                        return Core.LastOrNil(s.parameters);
                    }

                    if ((i < GoRuntime.Len(s.parameters)))
                    {
                        return s.parameters[i];
                    }

                    return null;
                });
                parameters = GoRuntime.Append(parameters, this.createCombinedSymbolFromTypes(symbols, Core.MapNonNil(candidates, (s2) =>
                {
                    return this.tryGetTypeAtPosition(s2, i);
                })));
            }
        }

        var restParameterSymbols = Core.MapNonNil(candidates, (s3) =>
        {
            if (signatureHasRestParameter(s3))
            {
                return Core.LastOrNil(s3.parameters);
            }

            return null;
        });
        var flags = SignatureFlags.IsSignatureCandidateForOverloadFailure;
        if ((GoRuntime.Len(restParameterSymbols) != 0))
        {
            throw unsupported("createUnionOfSignaturesForOverloadFailure: source rest");
        }

        if (Core.Some(candidates, signatureHasLiteralTypes))
        {
            flags |= SignatureFlags.HasLiteralTypes;
        }

        return this.newSignature(flags, candidates[0].declaration, null, thisParameter, parameters, this.getIntersectionType(Core.Map(candidates, this.getReturnTypeOfSignature)), null, minArgumentCount);
    }
}

public sealed partial class Checker
{
    private Signature resolveUntypedCall(Node node)
    {
        if (node.Kind != Kind.CallExpression)
            throw unsupported("resolveUntypedCall: non-call");
        checkSourceElements(node.TypeArguments());
        foreach (var argument in node.Arguments() ?? [])
            checkExpression(argument);
        return anySignature;
    }
}

public sealed partial class Checker
{
    private Signature resolveErrorCall(Node node)
    {
        this.resolveUntypedCall(node);
        return this.unknownSignature;
    }
}

public sealed partial class Checker
{
    private bool isUntypedFunctionCall(Type funcType, Type apparentFuncType, int numCallSignatures, int numConstructSignatures)
    {
        return ((IsTypeAny(funcType) || (IsTypeAny(apparentFuncType) && ((funcType.flags & TypeFlags.TypeParameter) != 0))) || (((((numCallSignatures == 0) && (numConstructSignatures == 0)) && ((apparentFuncType.flags & TypeFlags.Union) == 0)) && ((this.getReducedType(apparentFuncType).flags & TypeFlags.Never) == 0)) && this.isTypeAssignableTo(funcType, this.globalFunctionType)));
    }
}

public sealed partial class Checker
{
    private List<Node> getEffectiveCallArguments(Node node)
    {
        if (node.Kind != Kind.CallExpression)
            throw unsupported("getEffectiveCallArguments: non-call");
        var args = node.Arguments();
        if (getSpreadArgumentIndex(args) >= 0)
            throw unsupported("getEffectiveCallArguments: spread");
        return args;
    }
}

public sealed partial class Checker
{
    private Type checkExpressionWithContextualType(Node node, Type contextualType, InferenceContext inferenceContext, CheckMode checkMode)
    {
        var contextNode = this.getContextNode(node);
        this.pushContextualType(contextNode, contextualType, false);
        this.pushInferenceContext(contextNode, inferenceContext);
        var t = this.checkExpressionEx(node, ((checkMode | CheckMode.Contextual) | Core.IfElse((inferenceContext != null), CheckMode.Inferential, CheckMode.Normal)));
        if (((inferenceContext != null) && (inferenceContext.intraExpressionInferenceSites != null)))
        {
            inferenceContext.intraExpressionInferenceSites = null;
        }

        if ((this.maybeTypeOfKind(t, TypeFlags.Literal) && this.isLiteralOfContextualType(t, this.instantiateContextualType(contextualType, node))))
        {
            t = this.getRegularTypeOfLiteralType(t);
        }

        this.popInferenceContext();
        this.popContextualType();
        return t;
    }
}

public sealed partial class Checker
{
    private Node getContextNode(Node node)
    {
        if ((Ast.IsJsxAttributes(node) && !Ast.IsJsxSelfClosingElement(node.Parent)))
        {
            return node.Parent.Parent;
        }

        return node;
    }
}

public sealed partial class Checker
{
    private static bool signatureHasLiteralTypes(Signature s)
    {
        return ((s.flags & SignatureFlags.HasLiteralTypes) != 0);
    }
}

public sealed partial class Checker
{
    private bool hasParseDiagnostics(SourceFile sourceFile)
    {
        return (GoRuntime.Len(sourceFile.Diagnostics()) > 0);
    }
}

public sealed partial class Checker
{
    internal Type createPromiseType(Type promisedType)
    {
        var globalPromiseType = this.getGlobalPromiseTypeChecked();
        if ((globalPromiseType != this.emptyGenericType))
        {
            throw unsupported("createPromiseType: supplied generic Promise library type");
        }

        return this.unknownType;
    }
}

public sealed partial class Checker
{
    private Type unwrapReturnType(Type returnType, FunctionFlags functionFlags)
    {
        if ((functionFlags & (FunctionFlags.Async | FunctionFlags.Generator)) != 0)
            throw unsupported("unwrapReturnType: async/generator");
        return returnType;
    }
}

public sealed partial class Checker
{
    private static bool containsType(List<Type> types, Type t)
    {
        var tuple1 = GoSlices.BinarySearchFunc(types, t, CompareTypes);
        var ok = tuple1.Item2;
        return ok;
    }
}

public sealed partial class Checker
{
    private void pushInferenceContext(Node node, InferenceContext context)
    {
        this.inferenceContextInfos = GoRuntime.Append(this.inferenceContextInfos, new InferenceContextInfo(node, context));
    }
}

public sealed partial class Checker
{
    private void popInferenceContext()
    {
        var lastIndex = (GoRuntime.Len(this.inferenceContextInfos) - 1);
        this.inferenceContextInfos[lastIndex] = null;
        this.inferenceContextInfos = GoRuntime.Slice(this.inferenceContextInfos, 0, lastIndex);
    }
}

public sealed partial class Checker
{
    private bool isGenericTypeWithUndefinedConstraint(Type t)
    {
        if (((t.flags & TypeFlags.Instantiable) != 0))
        {
            var constraint = this.getBaseConstraintOfType(t);
            if ((constraint != null))
            {
                return this.maybeTypeOfKind(constraint, TypeFlags.Undefined);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private void checkDeprecatedSignature(Signature sig, Node node)
    {
        if (((sig.flags & SignatureFlags.IsSignatureCandidateForOverloadFailure) != 0))
        {
            return;
        }

        if (((sig.declaration != null) && this.IsDeprecatedDeclaration(sig.declaration)))
        {
            throw unsupported("checkDeprecatedSignature: JSDoc deprecated declaration");
        }
    }
}

public sealed partial class Checker
{
    private static bool acceptsVoid(Type t)
    {
        return ((t.flags & TypeFlags.Void) != 0);
    }
}

public sealed partial class Checker
{
    private bool hasCorrectTypeArgumentArity(Signature signature, List<Node> typeArguments)
    {
        var numTypeParameters = GoRuntime.Len(signature.typeParameters);
        var minTypeArgumentCount = GoRuntime.Len(signature.typeParameters);
        return ((GoRuntime.Len(typeArguments) == 0) || ((GoRuntime.Len(typeArguments) >= minTypeArgumentCount) && (GoRuntime.Len(typeArguments) <= numTypeParameters)));
    }
}

public sealed partial class Checker
{
    private void maybeAddMissingAwaitInfo(Node errorNode, Type source, Type target, Relation relation, bool reportErrors, List<Diagnostic> diagnosticOutput)
    {
        if (((((errorNode != null) && reportErrors) && (diagnosticOutput != null)) && (GoRuntime.Len(diagnosticOutput) != 0)))
        {
            if ((this.getAwaitedTypeOfPromise(target) != null))
            {
                return;
            }

            var awaitedTypeOfSource = this.getAwaitedTypeOfPromise(source);
            if (((awaitedTypeOfSource != null) && this.isTypeRelatedTo(awaitedTypeOfSource, target, relation)))
            {
                (diagnosticOutput)[0].AddRelatedInfo(NewDiagnosticForNode(errorNode, Diagnostics.Did_you_forget_to_use_await));
            }
        }
    }
}

public sealed partial class Checker
{
    private Node getThisArgumentOfCall(Node node)
    {
        if (Ast.IsBinaryExpression(node))
        {
            return node.AsBinaryExpression().Right;
        }

        Node expression = default;
        switch (true)
        {
            case true when Ast.IsCallExpression(node):
            {
                expression = node.Expression();
                break;
            }

            case true when Ast.IsTaggedTemplateExpression(node):
            {
                expression = node.AsTaggedTemplateExpression().Tag;
                break;
            }

            case true when (Ast.IsDecorator(node) && !this.legacyDecorators):
            {
                expression = node.Expression();
                break;
            }
        }

        if ((expression != null))
        {
            var callee = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
            if (Ast.IsAccessExpression(callee))
            {
                return callee.Expression();
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getThisArgumentType(Node node)
    {
        if ((node == null))
        {
            return this.voidType;
        }

        var thisArgumentType = this.checkExpression(node);
        if (Ast.IsOptionalChain(node.Parent))
            throw unsupported("getThisArgumentType: optional chain");
        return thisArgumentType;
    }
}

public sealed partial class Checker
{
    private int getLongestCandidateIndex(List<Signature> candidates, int argsCount)
    {
        var maxParamsIndex = -1;
        var maxParams = -1;
        {
            foreach (var range1 in GoRuntime.Range(candidates))
            {
                var i = range1.Key;
                var candidate = range1.Value;
                var paramCount = this.getParameterCount(candidate);
                if ((this.hasEffectiveRestParameter(candidate) || (paramCount >= argsCount)))
                {
                    return i;
                }

                if ((paramCount > maxParams))
                {
                    maxParams = paramCount;
                    maxParamsIndex = i;
                }
            }
        }

        return maxParamsIndex;
    }
}

public sealed partial class Checker
{
    private Symbol createCombinedSymbolFromTypes(List<Symbol> sources, List<Type> types)
    {
        return this.createCombinedSymbolForOverloadFailure(sources, this.getUnionTypeEx(types, UnionReduction.Subtype, null, null));
    }
}

public sealed partial class Checker
{
    private Symbol createCombinedSymbolForOverloadFailure(List<Symbol> sources, Type t)
    {
        return this.createSymbolWithType(Core.FirstOrNil(sources), t);
    }
}

public sealed partial class Checker
{
    private void reportCallResolutionErrors(Node node, CallState s, List<Signature> signatures, Message headMessage)
    {
        switch (true)
        {
            case true when (GoRuntime.Len(s.candidatesForArgumentError) != 0):
            {
                var last = s.candidatesForArgumentError[(GoRuntime.Len(s.candidatesForArgumentError) - 1)];
                List<Diagnostic> diags = new();
                this.isSignatureApplicable(s.node, s.args, last, this.assignableRelation, CheckMode.Normal, true, diags);
                {
                    foreach (var diagnosticValue in GoRuntime.RangeValues(diags))
                    {
                        var diagnostic = diagnosticValue;
                        if ((GoRuntime.Len(s.candidatesForArgumentError) > 1))
                        {
                            diagnostic = Ast.NewDiagnosticChain(diagnostic, Diagnostics.The_last_overload_gave_the_following_error);
                            diagnostic = Ast.NewDiagnosticChain(diagnostic, Diagnostics.No_overload_matches_this_call);
                        }

                        if ((headMessage != null))
                        {
                            diagnostic = Ast.NewDiagnosticChain(diagnostic, headMessage);
                        }

                        if (((last.declaration != null) && (GoRuntime.Len(s.candidatesForArgumentError) > 1)))
                        {
                            diagnostic.AddRelatedInfo(NewDiagnosticForNode(last.declaration, Diagnostics.The_last_overload_is_declared_here));
                        }

                        this.addImplementationSuccessElaboration(s, last, diagnostic);
                        this.addDiagnostic(diagnostic);
                    }
                }

                break;
            }

            case true when (s.candidateForArgumentArityError != null):
            {
                this.addDiagnostic(this.getArgumentArityError(s.node, new List<Signature> { s.candidateForArgumentArityError }, s.args, headMessage));
                break;
            }

            case true when (s.candidateForTypeArgumentError != null):
            {
                throw unsupported("reportCallResolutionErrors: source type arguments");
            }

            case true when !Ast.IsJsxOpeningFragment(node):
            {
                var signaturesWithCorrectTypeArgumentArity = Core.Filter(signatures, (sig) =>
                {
                    return this.hasCorrectTypeArgumentArity(sig, s.typeArguments);
                });
                if ((GoRuntime.Len(signaturesWithCorrectTypeArgumentArity) == 0))
                {
                    throw unsupported("reportCallResolutionErrors: source type argument arity");
                }
                else
                {
                    this.addDiagnostic(this.getArgumentArityError(s.node, signaturesWithCorrectTypeArgumentArity, s.args, headMessage));
                }

                break;
            }
        }
    }
}

public sealed partial class Checker
{
    private Diagnostic getArgumentArityError(Node node, List<Signature> signatures, List<Node> args, Message headMessage)
    {
        var spreadIndex = this.getSpreadArgumentIndex(args);
        if ((spreadIndex > -1))
        {
            return NewDiagnosticForNode(args[spreadIndex], Diagnostics.A_spread_argument_must_either_have_a_tuple_type_or_be_passed_to_a_rest_parameter);
        }

        var minCount = int.MaxValue;
        var maxCount = int.MinValue;
        var maxBelow = int.MinValue;
        var minAbove = int.MaxValue;
        Signature closestSignature = default;
        {
            foreach (var sig in GoRuntime.RangeValues(signatures))
            {
                var minParameter = this.getMinArgumentCount(sig);
                var maxParameter = this.getParameterCount(sig);
                if ((minParameter < minCount))
                {
                    minCount = minParameter;
                    closestSignature = sig;
                }

                maxCount = Math.Max(maxCount, maxParameter);
                if (((minParameter < GoRuntime.Len(args)) && (minParameter > maxBelow)))
                {
                    maxBelow = minParameter;
                }

                if (((GoRuntime.Len(args) < maxParameter) && (maxParameter < minAbove)))
                {
                    minAbove = maxParameter;
                }
            }
        }

        var hasRestParameter = Core.Some(signatures, this.hasEffectiveRestParameter);
        string parameterRange = "";
        switch (true)
        {
            case true when hasRestParameter:
            {
                parameterRange = Strconv.Itoa(minCount);
                break;
            }

            case true when (minCount < maxCount):
            {
                parameterRange = ((Strconv.Itoa(minCount) + "-") + Strconv.Itoa(maxCount));
                break;
            }

            default:
            {
                parameterRange = Strconv.Itoa(minCount);
                break;
            }
        }

        var isVoidPromiseError = (((!hasRestParameter && (parameterRange == "1")) && (GoRuntime.Len(args) == 0)) && isPromiseResolveArityError(node));
        var errorNode = getErrorNodeForCallNode(node);
        if ((isVoidPromiseError && Ast.IsInJSFile(node)))
        {
            return NewDiagnosticForNode(errorNode, Diagnostics.Expected_1_argument_but_got_0_new_Promise_needs_a_JSDoc_hint_to_produce_a_resolve_that_can_be_called_without_arguments);
        }

        Message message = default;
        switch (true)
        {
            case true when Ast.IsDecorator(node):
            {
                if (hasRestParameter)
                {
                    message = Diagnostics.The_runtime_will_invoke_the_decorator_with_1_arguments_but_the_decorator_expects_at_least_0;
                }
                else
                {
                    message = Diagnostics.The_runtime_will_invoke_the_decorator_with_1_arguments_but_the_decorator_expects_0;
                }

                break;
            }

            case true when hasRestParameter:
            {
                message = Diagnostics.Expected_at_least_0_arguments_but_got_1;
                break;
            }

            case true when isVoidPromiseError:
            {
                message = Diagnostics.Expected_0_arguments_but_got_1_Did_you_forget_to_include_void_in_your_type_argument_to_Promise;
                break;
            }

            default:
            {
                message = Diagnostics.Expected_0_arguments_but_got_1;
                break;
            }
        }

        switch (true)
        {
            case true when ((minCount < GoRuntime.Len(args)) && (GoRuntime.Len(args) < maxCount)):
            {
                var diagnostic = NewDiagnosticForNode(errorNode, Diagnostics.No_overload_expects_0_arguments_but_overloads_do_exist_that_expect_either_1_or_2_arguments, GoRuntime.Len(args), maxBelow, minAbove);
                if ((headMessage != null))
                {
                    diagnostic = Ast.NewDiagnosticChain(diagnostic, headMessage);
                }

                return diagnostic;
            }

            case true when (GoRuntime.Len(args) < minCount):
            {
                var diagnostic2 = NewDiagnosticForNode(errorNode, message, parameterRange, GoRuntime.Len(args));
                if ((headMessage != null))
                {
                    diagnostic2 = Ast.NewDiagnosticChain(diagnostic2, headMessage);
                }

                Node parameter = default;
                if (((closestSignature != null) && (closestSignature.declaration != null)))
                {
                    parameter = Core.ElementOrNil(closestSignature.declaration.Parameters(), (GoRuntime.Len(args) + Core.IfElse((closestSignature.thisParameter != null), 1, 0)));
                }

                if ((parameter != null))
                {
                    Diagnostic related = default;
                    switch (true)
                    {
                        case true when Ast.IsBindingPattern(parameter.Name()):
                        {
                            related = NewDiagnosticForNode(parameter, Diagnostics.An_argument_matching_this_binding_pattern_was_not_provided);
                            break;
                        }

                        case true when isRestParameter(parameter):
                        {
                            related = NewDiagnosticForNode(parameter, Diagnostics.Arguments_for_the_rest_parameter_0_were_not_provided, parameter.Name().Text());
                            break;
                        }

                        default:
                        {
                            related = NewDiagnosticForNode(parameter, Diagnostics.An_argument_for_0_was_not_provided, parameter.Name().Text());
                            break;
                        }
                    }

                    diagnostic2.AddRelatedInfo(related);
                }

                return diagnostic2;
            }

            default:
            {
                if ((maxCount >= GoRuntime.Len(args)))
                {
                    var diagnostic3 = NewDiagnosticForNode(errorNode, message, parameterRange, GoRuntime.Len(args));
                    if ((headMessage != null))
                    {
                        diagnostic3 = Ast.NewDiagnosticChain(diagnostic3, headMessage);
                    }

                    return diagnostic3;
                }

                var sourceFile = Ast.GetSourceFileOfNode(node);
                var pos = args[maxCount].Pos();
                var end = args[(GoRuntime.Len(args) - 1)].End();
                if ((end == pos))
                {
                    end++;
                }

                pos = TypeScript.Scanner.Scanner.SkipTrivia(sourceFile.Text(), pos);
                if ((end < pos))
                {
                    end = pos;
                }

                var diagnostic4 = Ast.NewDiagnostic(sourceFile, Core.NewTextRange(pos, end), message, parameterRange, GoRuntime.Len(args));
                if ((headMessage != null))
                {
                    diagnostic4 = Ast.NewDiagnosticChain(diagnostic4, headMessage);
                }

                return diagnostic4;
            }
        }
    }
}

public sealed partial class Checker
{
    private void reportCannotInvokePossiblyNullOrUndefinedError(Node node, TypeFacts facts)
    {
        this.error(node, Core.IfElse(((facts & TypeFacts.IsUndefined) != 0), Core.IfElse(((facts & TypeFacts.IsNull) != 0), Diagnostics.Cannot_invoke_an_object_which_is_possibly_null_or_undefined, Diagnostics.Cannot_invoke_an_object_which_is_possibly_undefined), Diagnostics.Cannot_invoke_an_object_which_is_possibly_null));
    }
}

public sealed partial class Checker
{
    private Diagnostic invocationErrorDetails(Node errorTarget, Type apparentType, SignatureKind kind)
    {
        Diagnostic diagnostic = default;
        var isCall = (kind == SignatureKind.Call);
        var awaitedType = this.getAwaitedType(apparentType);
        var maybeMissingAwait = ((awaitedType != null) && (GoRuntime.Len(this.getSignaturesOfType(awaitedType, kind)) > 0));
        var target = errorTarget;
        if ((Ast.IsPropertyAccessExpression(errorTarget) && Ast.IsCallExpression(errorTarget.Parent)))
        {
            target = errorTarget.Name();
        }

        if (((apparentType.flags & TypeFlags.Union) != 0))
        {
            var types = apparentType.Types();
            var hasSignatures = false;
            {
                foreach (var constituent in GoRuntime.RangeValues(types))
                {
                    var signatures = this.getSignaturesOfType(constituent, kind);
                    if ((GoRuntime.Len(signatures) != 0))
                    {
                        hasSignatures = true;
                        if ((diagnostic != null))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if ((diagnostic == null))
                        {
                            diagnostic = NewDiagnosticForNode(target, Core.IfElse(isCall, Diagnostics.Type_0_has_no_call_signatures, Diagnostics.Type_0_has_no_construct_signatures), this.typeToStringByteString(constituent));
                            diagnostic = NewDiagnosticChainForNode(diagnostic, target, Core.IfElse(isCall, Diagnostics.Not_all_constituents_of_type_0_are_callable, Diagnostics.Not_all_constituents_of_type_0_are_constructable), this.typeToStringByteString(apparentType));
                        }

                        if (hasSignatures)
                        {
                            break;
                        }
                    }
                }
            }

            if (!hasSignatures)
            {
                diagnostic = NewDiagnosticForNode(target, Core.IfElse(isCall, Diagnostics.No_constituent_of_type_0_is_callable, Diagnostics.No_constituent_of_type_0_is_constructable), this.typeToStringByteString(apparentType));
            }

            if ((diagnostic == null))
            {
                diagnostic = NewDiagnosticForNode(target, Core.IfElse(isCall, Diagnostics.Each_member_of_the_union_type_0_has_signatures_but_none_of_those_signatures_are_compatible_with_each_other, Diagnostics.Each_member_of_the_union_type_0_has_construct_signatures_but_none_of_those_signatures_are_compatible_with_each_other), this.typeToStringByteString(apparentType));
            }
        }
        else
        {
            diagnostic = NewDiagnosticChainForNode(diagnostic, target, Core.IfElse(isCall, Diagnostics.Type_0_has_no_call_signatures, Diagnostics.Type_0_has_no_construct_signatures), this.typeToStringByteString(apparentType));
        }

        var headMessage = Core.IfElse(isCall, Diagnostics.This_expression_is_not_callable, Diagnostics.This_expression_is_not_constructable);
        if ((Ast.IsCallExpression(errorTarget.Parent) && (GoRuntime.Len(errorTarget.Parent.Arguments()) == 0)))
        {
            var resolvedSymbol = this.getResolvedSymbolOrNil(errorTarget);
            if (((resolvedSymbol != null) && ((resolvedSymbol.Flags & SymbolFlags.GetAccessor) != 0)))
            {
                headMessage = Diagnostics.This_expression_is_not_callable_because_it_is_a_get_accessor_Did_you_mean_to_use_it_without;
            }
        }

        diagnostic = NewDiagnosticChainForNode(diagnostic, target, headMessage);
        if (maybeMissingAwait)
        {
            diagnostic.AddRelatedInfo(NewDiagnosticForNode(errorTarget, Diagnostics.Did_you_forget_to_use_await));
        }

        return diagnostic;
    }
}

public sealed partial class Checker
{
    private void invocationError(Node errorTarget, Type apparentType, SignatureKind kind, Diagnostic relatedInformation)
    {
        var diagnostic = this.invocationErrorDetails(errorTarget, apparentType, kind);
        if ((relatedInformation != null))
        {
            diagnostic.AddRelatedInfo(relatedInformation);
        }

        diagnostic = this.addDiagnostic(diagnostic);
        this.invocationErrorRecovery(apparentType, kind, diagnostic);
    }
}

public sealed partial class Checker
{
    private void invocationErrorRecovery(Type apparentType, SignatureKind kind, Diagnostic diagnostic)
    {
        if (apparentType.symbol == null)
            return;
        var links = exportTypeLinks.Get(apparentType.symbol);
        if (links.originatingImport != null && !Ast.IsImportCall(links.originatingImport))
            throw unsupported("invocationErrorRecovery: source import");
    }
}

public sealed partial class Checker
{
    private Type checkTruthinessExpression(Node node, CheckMode checkMode)
    {
        return this.checkTruthinessOfType(this.checkExpressionEx(node, checkMode), node);
    }
}

public sealed partial class Checker
{
    private int getSpreadArgumentIndex(List<Node> args)
    {
        return Core.FindIndex(args, isSpreadArgument);
    }
}

public sealed partial class Checker
{
    private bool hasContextSensitiveReturnExpression(Node node)
    {
        if (((node.TypeParameters() != null) || (node.Type() != null)))
        {
            return false;
        }

        var body = node.Body();
        if ((body == null))
        {
            return false;
        }

        if (!Ast.IsBlock(body))
        {
            return this.isContextSensitive(body);
        }

        return Ast.ForEachReturnStatement(body, (statement) =>
        {
            return ((statement.Expression() != null) && this.isContextSensitive(statement.Expression()));
        });
    }
}

public sealed partial class Checker
{
    // checker.go:9047. Go CallState is passed by pointer; copy uses MemberwiseClone for value-header state.
    private sealed class CallState
    {
        internal Node node;
        internal List<Node> typeArguments, args;
        internal List<Signature> candidates, candidatesForArgumentError;
        internal CheckMode argCheckMode;
        internal bool isSingleNonGenericCandidate, signatureHelpTrailingComma, recursiveResolution;
        internal Signature candidateForArgumentArityError, candidateForTypeArgumentError;
        internal CallState Copy() => (CallState)MemberwiseClone();
    }

    private List<Node> callResolutionStack;
    private bool isInferencePartiallyBlocked = false;
    private int? apparentArgumentCount = null;
    private Func<Type> getGlobalPromiseTypeChecked;
}

public sealed partial class Checker
{
    private sealed record IntraExpressionInferenceSite(Node node, Type type);
}

public sealed partial class Checker
{
    private void checkMethodDeclaration(Node node)
    {
        if (!this.checkGrammarMethod(node))
        {
            this.checkGrammarComputedPropertyName(node.Name());
            if ((((Ast.IsMethodDeclaration(node) && (node.AsMethodDeclaration().AsteriskToken != null)) && Ast.IsIdentifier(node.Name())) && (node.Name().Text() == "constructor")))
            {
                this.error(node.Name(), Diagnostics.Class_constructor_may_not_be_a_generator);
            }
        }

        this.checkFunctionOrMethodDeclaration(node);
        if (((Ast.HasSyntacticModifier(node, ModifierFlags.Abstract) && Ast.IsMethodDeclaration(node)) && (node.Body() != null)))
        {
            this.error(node, Diagnostics.Method_0_cannot_have_an_implementation_because_it_is_marked_abstract, TypeScript.Scanner.Scanner.DeclarationNameToStringByteString(node.Name()));
        }

        if ((Ast.IsPrivateIdentifier(node.Name()) && (Ast.GetContainingClass(node) == null)))
        {
            this.error(node, Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
        }

        if (Ast.IsPrivateIdentifier(node.Name()))
            throw unsupported("checkMethodDeclaration: private class name");
    }
}

public sealed partial class Checker
{
    private Type checkFunctionExpressionOrObjectLiteralMethod(Node node, CheckMode checkMode)
    {
        this.checkNodeDeferred(node);
        if ((node.FunctionLikeData().FullSignature != null))
        {
            this.checkSourceElement(node.FunctionLikeData().FullSignature);
        }

        if (Ast.IsFunctionExpression(node))
        {
            this.checkCollisionsForDeclarationName(node, node.Name());
        }

        if ((((checkMode & CheckMode.SkipContextSensitive) != 0) && this.isContextSensitive(node)))
        {
            if (((node.Type() == null) && !Ast.HasContextSensitiveParameters(node)))
            {
                var contextualSignature = this.getContextualSignature(node);
                if (((contextualSignature != null) && this.couldContainTypeVariables(this.getReturnTypeOfSignature(contextualSignature))))
                {
                    {
                        var ok = this.contextFreeTypes.TryGetValue(node, out var cached);
                        if (ok)
                        {
                            return cached;
                        }
                    }

                    var returnType = this.getReturnTypeFromBody(node, checkMode);
                    var returnOnlySignature = this.newSignature(SignatureFlags.IsNonInferrable, null, null, null, null, returnType, null, 0);
                    var returnOnlyType = this.newAnonymousType(node.Symbol(), null, new List<Signature> { returnOnlySignature }, null, null);
                    returnOnlyType.objectFlags |= ObjectFlags.NonInferrableType;
                    this.contextFreeTypes[node] = returnOnlyType;
                    return returnOnlyType;
                }
            }

            return this.anyFunctionType;
        }

        var hasGrammarError = this.checkGrammarFunctionLikeDeclaration(node);
        if ((!hasGrammarError && Ast.IsFunctionExpression(node)))
        {
            this.checkGrammarForGenerator(node);
        }

        if ((node.FunctionLikeData().FullSignature != null))
        {
            if ((this.getContextualCallSignature(this.getTypeFromTypeNode(node.FunctionLikeData().FullSignature), node) == null))
            {
                this.error(node.FunctionLikeData().FullSignature, Diagnostics.A_JSDoc_type_tag_on_a_function_must_have_a_signature_with_the_correct_number_of_arguments);
            }
        }

        this.contextuallyCheckFunctionExpressionOrObjectLiteralMethod(node, checkMode);
        return this.getTypeOfSymbol(this.getSymbolOfDeclaration(node));
    }
}

public sealed partial class Checker
{
    private void contextuallyCheckFunctionExpressionOrObjectLiteralMethod(Node node, CheckMode checkMode)
    {
        var links = this.nodeLinks.Get(node);
        if (((links.flags & NodeCheckFlags.ContextChecked) == 0))
        {
            var contextualSignature = this.getContextualSignature(node);
            if (((links.flags & NodeCheckFlags.ContextChecked) == 0))
            {
                links.flags |= NodeCheckFlags.ContextChecked;
                var signature = Core.FirstOrNil(this.getSignaturesOfType(this.getTypeOfSymbol(this.getSymbolOfDeclaration(node)), SignatureKind.Call));
                if ((signature == null))
                {
                    return;
                }

                if (this.isContextSensitive(node))
                {
                    if ((contextualSignature != null))
                    {
                        var inferenceContext = this.getInferenceContext(node);
                        Signature instantiatedContextualSignature = default;
                        if (((checkMode & CheckMode.Inferential) != 0))
                        {
                            throw unsupported("contextuallyCheckFunctionExpressionOrObjectLiteralMethod: generic inference");
                        }

                        if ((instantiatedContextualSignature == null))
                        {
                            if ((inferenceContext != null))
                            {
                                instantiatedContextualSignature = this.instantiateSignature(contextualSignature, inferenceContext.mapper);
                            }
                            else
                            {
                                instantiatedContextualSignature = contextualSignature;
                            }
                        }

                        this.assignContextualParameterTypes(signature, instantiatedContextualSignature);
                    }
                    else
                    {
                        this.assignNonContextualParameterTypes(signature);
                    }
                }
                else
                {
                    if ((((contextualSignature != null) && (node.TypeParameters() == null)) && (GoRuntime.Len(contextualSignature.parameters) > GoRuntime.Len(node.Parameters()))))
                    {
                        var inferenceContext2 = this.getInferenceContext(node);
                        if (((checkMode & CheckMode.Inferential) != 0))
                        {
                            throw unsupported("generic inference from annotated signature");
                        }
                    }
                }

                if ((((contextualSignature != null) && (this.getReturnTypeFromAnnotation(node) == null)) && (signature.resolvedReturnType == null)))
                {
                    var returnType = this.getReturnTypeFromBody(node, (checkMode & ~CheckMode.SkipContextSensitive));
                    if ((signature.resolvedReturnType == null))
                    {
                        signature.resolvedReturnType = returnType;
                    }
                }

                this.checkSignatureDeclaration(node);
            }
        }
    }
}

public sealed partial class Checker
{
    private void checkFunctionExpressionOrObjectLiteralMethodDeferred(Node node)
    {
        var functionFlags = Ast.GetFunctionFlags(node);
        var returnType = this.getReturnTypeFromAnnotation(node);
        this.checkAllCodePathsInNonVoidFunctionReturnOrThrow(node, returnType);
        var body = node.Body();
        if ((body != null))
        {
            if ((node.Type() == null))
            {
                this.getReturnTypeOfSignature(this.getSignatureFromDeclaration(node));
            }

            if (Ast.IsBlock(body))
            {
                this.checkSourceElement(body);
            }
            else
            {
                var exprType = this.checkExpression(body);
                if ((returnType != null))
                {
                    var returnOrPromisedType = this.unwrapReturnType(returnType, functionFlags);
                    if ((returnOrPromisedType != null))
                    {
                        this.checkReturnExpression(node, returnOrPromisedType, body, body, exprType, false);
                    }
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private Type getContextualThisParameterType(Node fn)
    {
        if (Ast.IsArrowFunction(fn))
        {
            return null;
        }

        if (this.isContextSensitiveFunctionOrObjectLiteralMethod(fn))
        {
            var contextualSignature = this.getContextualSignature(fn);
            if ((contextualSignature != null))
            {
                var thisParameter = contextualSignature.thisParameter;
                if ((thisParameter != null))
                {
                    return this.getTypeOfSymbol(thisParameter);
                }
            }
        }

        var inJs = Ast.IsInJSFile(fn);
        if ((this.noImplicitThis || inJs))
        {
            var containingLiteral = getContainingObjectLiteral(fn);
            if ((containingLiteral != null))
            {
                var contextualType = this.getApparentTypeOfContextualType(containingLiteral);
                var thisType = this.getThisTypeOfObjectLiteralFromContextualType(containingLiteral, contextualType);
                if ((thisType != null))
                {
                    return this.instantiateType(thisType, this.getMapperFromContext(this.getInferenceContext(containingLiteral)));
                }

                if ((contextualType != null))
                {
                    thisType = this.GetNonNullableType(contextualType);
                }
                else
                {
                    thisType = this.checkExpressionCached(containingLiteral);
                }

                return this.getWidenedType(thisType);
            }

            var parent = Ast.WalkUpParenthesizedExpressions(fn.Parent);
            if (Ast.IsAssignmentExpression(parent, false))
            {
                var target = parent.AsBinaryExpression().Left;
                if (Ast.IsAccessExpression(target))
                {
                    var expression = target.Expression();
                    if ((inJs && Ast.IsIdentifier(expression)))
                    {
                        var sourceFile = Ast.GetSourceFileOfNode(parent);
                        if (((sourceFile.CommonJSModuleIndicator != null) && ((this.getResolvedSymbol(expression).Flags & SymbolFlags.ModuleExports) != 0)))
                        {
                            return null;
                        }
                    }

                    return this.getWidenedType(this.checkExpressionCached(expression));
                }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type checkThisExpression(Node node)
    {
        var container = Ast.GetThisContainer(node, true, true);
        var capturedByArrowFunction = false;
        var thisInComputedPropertyName = false;
        if (Ast.IsConstructorDeclaration(container))
        {
            throw unsupported("checkThisExpression: class constructor");
        }

        while (true)
        {
            if (Ast.IsArrowFunction(container))
            {
                container = Ast.GetThisContainer(container, false, !thisInComputedPropertyName);
                capturedByArrowFunction = true;
            }

            if (Ast.IsComputedPropertyName(container))
            {
                container = Ast.GetThisContainer(container, !capturedByArrowFunction, false);
                thisInComputedPropertyName = true;
                continue;
            }

            break;
        }

        checkThisInStaticClassFieldInitializerInDecoratedClass(node, container);
        if (thisInComputedPropertyName)
        {
            this.error(node, Diagnostics.X_this_cannot_be_referenced_in_a_computed_property_name);
        }
        else
        {
            switch (container.Kind)
            {
                case Kind.ModuleDeclaration:
                {
                    this.error(node, Diagnostics.X_this_cannot_be_referenced_in_a_module_or_namespace_body);
                    break;
                }

                case Kind.EnumDeclaration:
                {
                    this.error(node, Diagnostics.X_this_cannot_be_referenced_in_current_location);
                    break;
                }
            }
        }

        var t = this.tryGetThisTypeAtEx(node, true, container);
        if (this.noImplicitThis)
        {
            var globalThisType = this.getTypeOfSymbol(this.globalThisSymbol);
            if (((t == globalThisType) && capturedByArrowFunction))
            {
                this.error(node, Diagnostics.The_containing_arrow_function_captures_the_global_value_of_this);
            }
            else
            {
                if ((t == null))
                {
                    var diag = this.error(node, Diagnostics.X_this_implicitly_has_type_any_because_it_does_not_have_a_type_annotation);
                    if (!Ast.IsSourceFile(container))
                    {
                        var outsideThis = this.tryGetThisTypeAt(container);
                        if (((outsideThis != null) && (outsideThis != globalThisType)))
                        {
                            diag.AddRelatedInfo(createDiagnosticForNode(container, Diagnostics.An_outer_value_of_this_is_shadowed_by_this_container));
                        }
                    }
                }
            }
        }

        if ((t == null))
        {
            return this.anyType;
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForArgumentAtIndex(Node callTarget, int argIndex)
    {
        if (Ast.IsImportCall(callTarget))
        {
            throw unsupported("getContextualTypeForArgumentAtIndex: import");
        }

        Signature signature = default;
        if ((this.signatureLinks.Get(callTarget).resolvedSignature == this.resolvingSignature))
        {
            signature = this.resolvingSignature;
        }
        else
        {
            signature = this.getResolvedSignature(callTarget, null, CheckMode.Normal);
        }

        if ((Ast.IsJsxOpeningLikeElement(callTarget) && (argIndex == 0)))
        {
            throw unsupported("getContextualTypeForArgumentAtIndex: JSX");
        }

        var restIndex = (GoRuntime.Len(signature.parameters) - 1);
        if ((signatureHasRestParameter(signature) && (argIndex >= restIndex)))
        {
            throw unsupported("getContextualTypeForArgumentAtIndex: rest");
        }

        return this.getTypeAtPosition(signature, argIndex);
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForObjectLiteralMethod(Node node, ContextFlags contextFlags)
    {
        if (((node.Flags & NodeFlags.InWithStatement) != 0))
        {
            return null;
        }

        return this.getContextualTypeForObjectLiteralElement(node);
    }
}

public sealed partial class Checker
{
    private bool isContextSensitiveFunctionLikeDeclaration(Node node)
    {
        return ((Ast.HasContextSensitiveParameters(node) || this.hasContextSensitiveReturnExpression(node)) || (Ast.GetFunctionFlags(node) & FunctionFlags.Generator) != 0);
    }
}

public sealed partial class Checker
{
    private void checkTestingKnownTruthyTypes(Node condExpr, Type condType, Node body)
    {
        condExpr = Ast.SkipParentheses(condExpr);
        this.checkTestingKnownTruthyType(condExpr, condType, body);
        while ((Ast.IsBinaryExpression(condExpr) && (((condExpr.AsBinaryExpression().OperatorToken.Kind == Kind.BarBarToken) || (condExpr.AsBinaryExpression().OperatorToken.Kind == Kind.QuestionQuestionToken)))))
        {
            condExpr = Ast.SkipParentheses(condExpr.AsBinaryExpression().Left);
            this.checkTestingKnownTruthyType(condExpr, condType, body);
        }
    }
}

public sealed partial class Checker
{
    private static Node getErrorNodeForCallNode(Node node)
    {
        if (Ast.IsCallExpression(node))
        {
            node = node.Expression();
            if (Ast.IsPropertyAccessExpression(node))
            {
                node = node.Name();
            }
        }

        return node;
    }
}

public sealed partial class Checker
{
    private Type tryGetThisTypeAt(Node node)
    {
        return this.tryGetThisTypeAtEx(node, true, null);
    }
}

public sealed partial class Checker
{
    private Type tryGetThisTypeAtEx(Node node, bool includeGlobalThis, Node container)
    {
        if ((container == null))
        {
            container = this.getThisContainer(node, false, false);
        }

        if ((Ast.IsFunctionLike(container) && ((!this.isInParameterInitializerBeforeContainingFunction(node) || (Ast.GetThisParameter(container) != null)))))
        {
            var sig = this.getSignatureOfFullSignatureType(container);
            if ((sig == null))
            {
                sig = this.getSignatureFromDeclaration(container);
            }

            var thisType = this.getThisTypeOfSignature(sig);
            if ((thisType == null))
            {
                thisType = this.getContextualThisParameterType(container);
            }

            if ((thisType != null))
            {
                return this.getFlowTypeOfReference(node, thisType);
            }
        }

        if (((container.Parent != null) && Ast.IsClassLike(container.Parent)))
        {
            throw unsupported("tryGetThisTypeAtEx: class");
        }

        if (Ast.IsSourceFile(container))
        {
            if ((container.AsSourceFile().ExternalModuleIndicator != null))
            {
                return this.undefinedType;
            }

            if (includeGlobalThis)
            {
                return this.getTypeOfSymbol(this.globalThisSymbol);
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private PredicateSemantics getSyntacticTruthySemantics(Node node)
    {
        node = Ast.SkipOuterExpressions(node, OuterExpressionKinds.All);
        switch (node.Kind)
        {
            case Kind.NumericLiteral:
            {
                if (((node.Text() == "0") || (node.Text() == "1")))
                {
                    return PredicateSemantics.Sometimes;
                }

                return PredicateSemantics.Always;
            }

            case Kind.ArrayLiteralExpression:
            case Kind.ArrowFunction:
            case Kind.BigIntLiteral:
            case Kind.ClassExpression:
            case Kind.FunctionExpression:
            case Kind.JsxElement:
            case Kind.JsxSelfClosingElement:
            case Kind.ObjectLiteralExpression:
            case Kind.RegularExpressionLiteral:
            {
                return PredicateSemantics.Always;
            }

            case Kind.VoidExpression:
            case Kind.NullKeyword:
            {
                return PredicateSemantics.Never;
            }

            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.StringLiteral:
            {
                if ((node.Text() != ""))
                {
                    return PredicateSemantics.Always;
                }

                return PredicateSemantics.Never;
            }

            case Kind.ConditionalExpression:
            {
                return (this.getSyntacticTruthySemantics(node.AsConditionalExpression().WhenTrue) | this.getSyntacticTruthySemantics(node.AsConditionalExpression().WhenFalse));
            }

            case Kind.Identifier:
            {
                if ((this.getResolvedSymbol(node) == this.undefinedSymbol))
                {
                    return PredicateSemantics.Never;
                }

                break;
            }
        }

        return PredicateSemantics.Sometimes;
    }
}

public sealed partial class Checker
{
    private bool isFunctionType(Type t)
    {
        return (((t.flags & TypeFlags.Object) != 0) && (GoRuntime.Len(this.getSignaturesOfType(t, SignatureKind.Call)) > 0));
    }
}

public sealed partial class Checker
{
    private bool isConstructorType(Type t)
    {
        if ((GoRuntime.Len(this.getSignaturesOfType(t, SignatureKind.Construct)) > 0))
        {
            return true;
        }

        if (((t.flags & TypeFlags.TypeVariable) != 0))
        {
            throw unsupported("isConstructorType: source mixin constraint");
        }

        return false;
    }
}

public sealed partial class Checker
{
    public bool IsNullableType(Type t)
    {
        return this.hasTypeFacts(t, TypeFacts.IsUndefinedOrNull);
    }
}

public sealed partial class Checker
{
    private static bool everyType(Type t, Func<Type, bool> f)
    {
        if (((t.flags & TypeFlags.Union) != 0))
        {
            return Core.Every(t.Types(), f);
        }

        return f(t);
    }
}

public sealed partial class Checker
{
    private static bool isSpreadArgument(Node arg)
    {
        if (Ast.IsSyntheticExpression(arg))
            throw unsupported("compiler-only synthetic expression");
        return Ast.IsSpreadElement(arg);
    }
}

public sealed partial class Checker
{
    private Type getThisTypeOfObjectLiteralFromContextualType(Node containingLiteral, Type contextualType)
    {
        var literal = containingLiteral;
        var t = contextualType;
        while ((t != null))
        {
            var thisType = this.getThisTypeFromContextualType(t);
            if ((thisType != null))
            {
                return thisType;
            }

            if ((literal.Parent.Kind != Kind.PropertyAssignment))
            {
                break;
            }

            literal = literal.Parent.Parent;
            t = this.getApparentTypeOfContextualType(literal);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private void checkTestingKnownTruthyCallableOrAwaitableOrEnumMemberType(Node condExpr, Type condType, Node body)
    {
        if (!this.strictNullChecks)
        {
            return;
        }

        this.checkTestingKnownTruthyTypes(condExpr, condType, body);
    }
}

public sealed partial class Checker
{
    private void addImplementationSuccessElaboration(CallState s, Signature failed, Diagnostic diagnostic)
    {
        if (((failed.declaration != null) && (failed.declaration.Symbol() != null)))
        {
            var declarations = failed.declaration.Symbol().Declarations;
            if ((GoRuntime.Len(declarations) > 1))
            {
                var implementation = Core.Find(declarations, (d) =>
                {
                    return (Ast.IsFunctionLikeDeclaration(d) && Ast.NodeIsPresent(d.Body()));
                });
                if ((implementation != null))
                {
                    var candidate = this.getSignatureFromDeclaration(implementation);
                    var localState = s.Copy();
                    localState.candidates = new List<Signature>
                    {
                        candidate
                    };
                    localState.isSingleNonGenericCandidate = (GoRuntime.Len(candidate.typeParameters) == 0);
                    if ((this.chooseOverload(localState, this.assignableRelation) != null))
                    {
                        diagnostic.AddRelatedInfo(NewDiagnosticForNode(implementation, Diagnostics.The_call_would_have_succeeded_against_this_implementation_but_implementation_signatures_of_overloads_are_not_externally_visible));
                    }
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private bool isPromiseResolveArityError(Node node)
    {
        if ((!Ast.IsCallExpression(node) || !Ast.IsIdentifier(node.Expression())))
        {
            return false;
        }

        var symbol = this.nameResolver.Resolve(node.Expression(), node.Expression().Text(), SymbolFlags.Value, null, false, false);
        if ((symbol == null))
        {
            return false;
        }

        var decl = symbol.ValueDeclaration;
        if ((((((decl == null) || !Ast.IsParameterDeclaration(decl)) || !Ast.IsFunctionExpressionOrArrowFunction(decl.Parent)) || !Ast.IsNewExpression(decl.Parent.Parent)) || !Ast.IsIdentifier(decl.Parent.Parent.Expression())))
        {
            return false;
        }

        throw unsupported("isPromiseResolveArityError: source new Promise");
    }
}

public sealed partial class Checker
{
    private Type checkTruthinessOfType(Type t, Node node)
    {
        if (((t.flags & TypeFlags.Void) != 0))
        {
            this.error(node, Diagnostics.An_expression_of_type_void_cannot_be_tested_for_truthiness);
            return t;
        }

        var semantics = this.getSyntacticTruthySemantics(node);
        if ((semantics != PredicateSemantics.Sometimes))
        {
            this.error(node, Core.IfElse((semantics == PredicateSemantics.Always), Diagnostics.This_kind_of_expression_is_always_truthy, Diagnostics.This_kind_of_expression_is_always_falsy));
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Symbol getSymbolOfNode(Node node)
    {
        var data = node.DeclarationData();
        if (((data != null) && (data.Symbol != null)))
        {
            return this.getMergedSymbol(this.getLateBoundSymbol(data.Symbol));
        }

        return null;
    }
}

public sealed partial class Checker
{
    private ModifierFlags getCombinedModifierFlagsCached(Node node)
    {
        if ((this.lastGetCombinedModifierFlagsNode == node))
        {
            return this.lastGetCombinedModifierFlagsResult;
        }

        this.lastGetCombinedModifierFlagsNode = node;
        this.lastGetCombinedModifierFlagsResult = Ast.GetCombinedModifierFlags(node);
        return this.lastGetCombinedModifierFlagsResult;
    }
}

public sealed partial class Checker
{
    private Type cloneTypeParameter(Type tp)
    {
        var result = this.newTypeParameter(tp.symbol);
        result.AsTypeParameter().target = tp;
        return result;
    }
}

public sealed partial class Checker
{
    private Node lastGetCombinedModifierFlagsNode;
    private ModifierFlags lastGetCombinedModifierFlagsResult;
    private Type markerSuperType, markerSubType, markerOtherType, globalThisType;
    private enum PredicateSemantics
    {
        None = 0,
        Always = 1,
        Never = 2,
        Sometimes = 3
    }
}

public sealed partial class Checker
{
    private void registerForUnusedIdentifiersCheck(Node node)
    {
        var sourceFile = Ast.GetSourceFileOfNode(node);
        var links = this.sourceFileLinks.Get(sourceFile.AsNode());
        links.identifierCheckNodes = GoRuntime.Append(links.identifierCheckNodes, node);
    }
}

public sealed partial class Checker
{
    private Type checkObjectLiteralMethod(Node node, CheckMode checkMode)
    {
        this.checkGrammarMethod(node);
        if (Ast.IsComputedPropertyName(node.Name()))
        {
            throw unsupported("checkObjectLiteralMethod: computed key");
        }

        var uninstantiatedType = this.checkFunctionExpressionOrObjectLiteralMethod(node, checkMode);
        if (Core.Some(getSignaturesOfType(uninstantiatedType, SignatureKind.Call), s => GoRuntime.Len(s.typeParameters) != 0))
            throw unsupported("checkObjectLiteralMethod: source generic signature");
        return uninstantiatedType;
    }
}

public sealed partial class Checker
{
    private bool isParameterOfContextSensitiveSignature(Symbol symbol)
    {
        var decl = symbol.ValueDeclaration;
        if ((decl == null))
        {
            return false;
        }

        if (Ast.IsBindingElement(decl))
        {
            decl = Ast.WalkUpBindingElementsAndPatterns(decl);
        }

        if (Ast.IsParameterDeclaration(decl))
        {
            return this.isContextSensitiveFunctionOrObjectLiteralMethod(decl.Parent);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForReturnExpression(Node node, ContextFlags contextFlags)
    {
        var fn = Ast.GetContainingFunction(node);
        if ((fn != null))
        {
            var contextualReturnType = this.getContextualReturnType(fn);
            if ((contextualReturnType != null))
            {
                var functionFlags = Ast.GetFunctionFlags(fn);
                if (((functionFlags & FunctionFlags.Generator) != 0))
                {
                    throw unsupported("getContextualTypeForReturnExpression: generator");
                }

                if (((functionFlags & FunctionFlags.Async) != 0))
                {
                    throw unsupported("getContextualTypeForReturnExpression: async");
                }

                return contextualReturnType;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForArgument(Node callTarget, Node arg)
    {
        var args = this.getEffectiveCallArguments(callTarget);
        var argIndex = GoSlices.Index(args, arg);
        if ((argIndex == -1))
        {
            return null;
        }

        return this.getContextualTypeForArgumentAtIndex(callTarget, argIndex);
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForBinaryOperand(Node node, ContextFlags contextFlags)
    {
        var binary = node.Parent.AsBinaryExpression();
        {
            var t = binary.Type;
            if ((t != null))
            {
                return this.getTypeFromTypeNode(t);
            }
        }

        switch (binary.OperatorToken.Kind)
        {
            case Kind.EqualsToken:
            case Kind.AmpersandAmpersandEqualsToken:
            case Kind.BarBarEqualsToken:
            case Kind.QuestionQuestionEqualsToken:
            {
                if ((node == binary.Right))
                {
                    var target = Ast.GetLeftmostExpression(binary.Left, false);
                    if (!((Ast.IsIdentifier(target) && ((this.getResolvedSymbol(target).Flags & SymbolFlags.ModuleExports) != 0))))
                    {
                        return this.getContextualTypeForAssignmentExpression(binary);
                    }
                }

                break;
            }

            case Kind.BarBarToken:
            case Kind.QuestionQuestionToken:
            {
                var t2 = this.getContextualType(binary.AsNode(), contextFlags);
                if (((node == binary.Right) && (((t2 == null) || (this.patternForType.GetValueOrDefault(t2) != null)))))
                {
                    return this.getTypeOfExpression(binary.Left);
                }

                return t2;
            }

            case Kind.AmpersandAmpersandToken:
            case Kind.CommaToken:
            {
                if ((node == binary.Right))
                {
                    return this.getContextualType(binary.AsNode(), contextFlags);
                }

                break;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForConditionalOperand(Node node, ContextFlags contextFlags)
    {
        var conditional = node.Parent.AsConditionalExpression();
        if (((node == conditional.WhenTrue) || (node == conditional.WhenFalse)))
        {
            return this.getContextualType(node.Parent, contextFlags);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Node getThisContainer(Node node, bool includeArrowFunctions, bool includeClassComputedPropertyName)
    {
        while (true)
        {
            node = node.Parent;
            if ((node == null))
            {
                throw new InvalidOperationException("No parent in getThisContainer");
            }

            switch (node.Kind)
            {
                case Kind.ComputedPropertyName:
                {
                    if ((includeClassComputedPropertyName && Ast.IsClassLike(node.Parent.Parent)))
                    {
                        return node;
                    }

                    node = node.Parent.Parent;
                    break;
                }

                case Kind.Decorator:
                {
                    if (((node.Parent.Kind == Kind.Parameter) && Ast.IsClassElement(node.Parent.Parent)))
                    {
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (Ast.IsClassElement(node.Parent))
                        {
                            node = node.Parent;
                        }
                    }

                    break;
                }

                case Kind.ArrowFunction:
                {
                    if (!includeArrowFunctions)
                    {
                        continue;
                    }

                    goto case Kind.FunctionDeclaration;
                }

                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.ModuleDeclaration:
                case Kind.ClassStaticBlockDeclaration:
                case Kind.PropertyDeclaration:
                case Kind.PropertySignature:
                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                case Kind.Constructor:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.CallSignature:
                case Kind.ConstructSignature:
                case Kind.IndexSignature:
                case Kind.EnumDeclaration:
                case Kind.SourceFile:
                {
                    return node;
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private bool isInParameterInitializerBeforeContainingFunction(Node node)
    {
        var inBindingInitializer = false;
        while (((node.Parent != null) && !Ast.IsFunctionLike(node.Parent)))
        {
            if (Ast.IsParameterDeclaration(node.Parent))
            {
                if ((inBindingInitializer || (node.Parent.Initializer() == node)))
                {
                    return true;
                }
            }

            if ((Ast.IsBindingElement(node.Parent) && (node.Parent.Initializer() == node)))
            {
                inBindingInitializer = true;
            }

            node = node.Parent;
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type getThisTypeFromContextualType(Type t)
    {
        return this.mapType(t, (t2) =>
        {
            if (((t2.flags & TypeFlags.Intersection) != 0))
            {
                {
                    foreach (var t3 in GoRuntime.RangeValues(t2.AsIntersectionType().types))
                    {
                        var typeArg = this.getThisTypeArgument(t3);
                        if ((typeArg != null))
                        {
                            return typeArg;
                        }
                    }
                }

                return null;
            }
            else
            {
                return this.getThisTypeArgument(t2);
            }
        });
    }
}

public sealed partial class Checker
{
    private Type getThisTypeArgument(Type t)
    {
        if ((((t.objectFlags & ObjectFlags.Reference) != 0) && (t.AsTypeReference().target == this.globalThisType)))
        {
            return this.getTypeArguments(t)[0];
        }

        return null;
    }
}

public sealed partial class Checker
{
    // inference.go:1426; selected contextual calls preserve nil inference contexts.
    private TypeMapper getMapperFromContext(InferenceContext n) => n?.mapper;
    // jsdoc.go:9; only ordinary documentation without semantic tags is admitted.
    private void checkUnmatchedJSDocParameters(Node node)
    {
        foreach (var tag in getAllJSDocTags(node) ?? [])
            if (tag.Kind == Kind.JSDocParameterTag)
                throw unsupported("checkUnmatchedJSDocParameters: semantic JSDoc tag");
    }
}

public sealed partial class Checker
{
    private Signature combineUnionOrIntersectionMemberSignatures(Signature left, Signature right, bool isUnion)
    {
        var typeParams = left.typeParameters;
        if ((GoRuntime.Len(typeParams) == 0))
        {
            typeParams = right.typeParameters;
        }

        TypeMapper paramMapper = default;
        if (((GoRuntime.Len(left.typeParameters) != 0) && (GoRuntime.Len(right.typeParameters) != 0)))
        {
            paramMapper = newTypeMapper(right.typeParameters, left.typeParameters);
        }

        var flags = (((left.flags | right.flags)) & ((SignatureFlags.PropagatingFlags & ~SignatureFlags.HasRestParameter)));
        var declaration = left.declaration;
        var @params = this.combineUnionOrIntersectionParameters(left, right, paramMapper, isUnion);
        var lastParam = Core.LastOrNil(@params);
        if (((lastParam != null) && ((lastParam.CheckFlags & CheckFlags.RestParameter) != 0)))
        {
            flags |= SignatureFlags.HasRestParameter;
        }

        var thisParam = this.combineUnionOrIntersectionThisParam(left.thisParameter, right.thisParameter, paramMapper, isUnion);
        var minArgCount = (int)(Math.Max(left.minArgumentCount, right.minArgumentCount));
        var result = this.newSignature(flags, declaration, typeParams, thisParam, @params, null, null, minArgCount);
        List<Signature> leftSignatures = default;
        if (((left.composite != null) && left.composite.isUnion))
        {
            leftSignatures = left.composite.signatures;
        }
        else
        {
            leftSignatures = new List<Signature>
            {
                left
            };
        }

        result.composite = new CompositeSignature
        {
            isUnion = isUnion,
            signatures = GoRuntime.Append(leftSignatures, right)
        };
        if ((paramMapper != null))
        {
            if ((((left.composite != null) && (left.composite.isUnion == isUnion)) && (left.mapper != null)))
            {
                result.mapper = this.combineTypeMappers(left.mapper, paramMapper);
            }
            else
            {
                result.mapper = paramMapper;
            }
        }
        else
        {
            if (((left.composite != null) && (left.composite.isUnion == isUnion)))
            {
                result.mapper = left.mapper;
            }
        }

        return result;
    }
}

public sealed partial class Checker
{
    private List<Symbol> combineUnionOrIntersectionParameters(Signature left, Signature right, TypeMapper mapper, bool isUnion)
    {
        var leftCount = this.getParameterCount(left);
        var rightCount = this.getParameterCount(right);
        int longestCount = default;
        Signature longest = default;
        Signature shorter = default;
        if ((leftCount >= rightCount))
        {
            (longestCount, longest, shorter) = (leftCount, left, right);
        }
        else
        {
            (longestCount, longest, shorter) = (rightCount, right, left);
        }

        var eitherHasEffectiveRest = (this.hasEffectiveRestParameter(left) || this.hasEffectiveRestParameter(right));
        var needsExtraRestElement = (eitherHasEffectiveRest && !this.hasEffectiveRestParameter(longest));
        var @params = GoRuntime.MakeSlice<Symbol>((longestCount + Core.IfElse(needsExtraRestElement, 1, 0)));
        {
            foreach (var i in GoRuntime.RangeKeys(longestCount))
            {
                var longestParamType = this.tryGetTypeAtPosition(longest, i);
                if ((longest == right))
                {
                    longestParamType = this.instantiateType(longestParamType, mapper);
                }

                var shorterParamType = Core.OrElse(this.tryGetTypeAtPosition(shorter, i), this.unknownType);
                if ((shorter == right))
                {
                    shorterParamType = this.instantiateType(shorterParamType, mapper);
                }

                var combinedParamType = this.getUnionOrIntersectionType(new List<Type> { longestParamType, shorterParamType }, !isUnion, UnionReduction.Literal);
                var isRestParam = ((eitherHasEffectiveRest && !needsExtraRestElement) && (i == ((longestCount - 1))));
                var isOptional = ((i >= this.getMinArgumentCount(longest)) && (i >= this.getMinArgumentCount(shorter)));
                string leftName = "";
                string rightName = "";
                if ((i < leftCount))
                {
                    leftName = this.getParameterNameAtPosition(left, i);
                }

                if ((i < rightCount))
                {
                    rightName = this.getParameterNameAtPosition(right, i);
                }

                string paramName = "";
                switch (true)
                {
                    case true when (leftName == rightName):
                    {
                        paramName = leftName;
                        break;
                    }

                    case true when (leftName == ""):
                    {
                        paramName = rightName;
                        break;
                    }

                    case true when (rightName == ""):
                    {
                        paramName = leftName;
                        break;
                    }
                }

                if ((paramName == ""))
                {
                    paramName = ("arg" + Strconv.Itoa(i));
                }

                var paramSymbol = this.newSymbolEx((SymbolFlags.FunctionScopedVariable | Core.IfElse((isOptional && !isRestParam), SymbolFlags.Optional, SymbolFlags.None)), paramName, Core.IfElse(isRestParam, CheckFlags.RestParameter, Core.IfElse(isOptional, CheckFlags.OptionalParameter, CheckFlags.None)));
                var links = this.valueSymbolLinks.Get(paramSymbol);
                if (isRestParam)
                {
                    throw unsupported("combineUnionOrIntersectionParameters: source rest");
                }
                else
                {
                    links.resolvedType = combinedParamType;
                }

                @params[i] = paramSymbol;
            }
        }

        if (needsExtraRestElement)
        {
            throw unsupported("combineUnionOrIntersectionParameters: source rest tuple");
        }

        return @params;
    }
}

public sealed partial class Checker
{
    private Symbol combineUnionOrIntersectionThisParam(Symbol left, Symbol right, TypeMapper mapper, bool isUnion)
    {
        if ((left == null))
        {
            return right;
        }

        if ((right == null))
        {
            return left;
        }

        var thisType = this.getUnionOrIntersectionType(new List<Type> { this.getTypeOfSymbol(left), this.instantiateType(this.getTypeOfSymbol(right), mapper) }, !isUnion, UnionReduction.Literal);
        return this.createSymbolWithType(left, thisType);
    }
}

public sealed partial class Checker
{
    // Partial checker.go export-origin links: imports remain outside script admission.
    private sealed class ExportTypeLinks
    {
        internal Node originatingImport = null;
    }

    private readonly LinkStore<Symbol, ExportTypeLinks> exportTypeLinks = new();
}

public sealed partial class Checker
{
    private Type getTypeOfFirstParameterOfSignature(Signature signature)
    {
        return this.getTypeOfFirstParameterOfSignatureWithFallback(signature, this.neverType);
    }
}

public sealed partial class Checker
{
    private Type getTypeOfFirstParameterOfSignatureWithFallback(Signature signature, Type fallbackType)
    {
        if ((GoRuntime.Len(signature.parameters) > 0))
        {
            return this.getTypeAtPosition(signature, 0);
        }

        return fallbackType;
    }
}

public sealed partial class Checker
{
    private Type getAwaitedType(Type t)
    {
        return this.getAwaitedTypeEx(t, null, null);
    }
}

public sealed partial class Checker
{
    private Type getAwaitedTypeEx(Type t, Node errorNode, Message diagnosticMessage, params object[] args)
    {
        var awaitedType = this.getAwaitedTypeNoAliasEx(t, errorNode, diagnosticMessage, args);
        if ((awaitedType != null))
        {
            return this.createAwaitedTypeIfNeeded(awaitedType);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getAwaitedTypeNoAlias(Type t)
    {
        return this.getAwaitedTypeNoAliasEx(t, null, null);
    }
}

public sealed partial class Checker
{
    private Type getAwaitedTypeNoAliasEx(Type t, Node errorNode, Message diagnosticMessage, params object[] args)
    {
        if (IsTypeAny(t))
        {
            return t;
        }

        if (this.isAwaitedTypeInstantiation(t))
        {
            return t;
        }

        var key = new CachedTypeKey(CachedTypeKind.AwaitedType, t.id);
        {
            var awaitedType = this.cachedTypes.GetValueOrDefault(key);
            if ((awaitedType != null))
            {
                return awaitedType;
            }
        }

        if (((t.flags & TypeFlags.Union) != 0))
        {
            if ((this.awaitedTypeStack?.Contains(t) == true))
            {
                if ((errorNode != null))
                {
                    this.error(errorNode, Diagnostics.Type_is_referenced_directly_or_indirectly_in_the_fulfillment_callback_of_its_own_then_method);
                }

                return null;
            }

            this.awaitedTypeStack = GoRuntime.Append(this.awaitedTypeStack, t);
            var mapped = this.mapType(t, (t2) =>
            {
                return this.getAwaitedTypeNoAliasEx(t2, errorNode, diagnosticMessage, args);
            });
            this.awaitedTypeStack = GoRuntime.Slice(this.awaitedTypeStack, 0, (GoRuntime.Len(this.awaitedTypeStack) - 1));
            this.cachedTypes[key] = mapped;
            return mapped;
        }

        if (this.isAwaitedTypeNeeded(t))
        {
            this.cachedTypes[key] = t;
            return t;
        }

        Type thisTypeForError = default;
        var promisedType = this.getPromisedTypeOfPromiseEx(t, null, new GoRef<Type>(() => thisTypeForError, v => thisTypeForError = v));
        if ((promisedType != null))
        {
            if (((t == promisedType) || (this.awaitedTypeStack?.Contains(promisedType) == true)))
            {
                if ((errorNode != null))
                {
                    this.error(errorNode, Diagnostics.Type_is_referenced_directly_or_indirectly_in_the_fulfillment_callback_of_its_own_then_method);
                }

                return null;
            }

            this.awaitedTypeStack = GoRuntime.Append(this.awaitedTypeStack, t);
            var awaitedType2 = this.getAwaitedTypeNoAliasEx(promisedType, errorNode, diagnosticMessage, args);
            this.awaitedTypeStack = GoRuntime.Slice(this.awaitedTypeStack, 0, (GoRuntime.Len(this.awaitedTypeStack) - 1));
            if ((awaitedType2 == null))
            {
                return null;
            }

            this.cachedTypes[key] = awaitedType2;
            return awaitedType2;
        }

        if (this.isThenableType(t))
        {
            if ((errorNode != null))
            {
                Diagnostic diagnostic = default;
                if ((thisTypeForError != null))
                {
                    diagnostic = NewDiagnosticForNode(errorNode, Diagnostics.The_this_context_of_type_0_is_not_assignable_to_method_s_this_of_type_1, this.typeToStringByteString(t), this.typeToStringByteString(thisTypeForError));
                }

                this.addDiagnostic(NewDiagnosticChainForNode(diagnostic, errorNode, diagnosticMessage, args));
            }

            return null;
        }

        this.cachedTypes[key] = t;
        return t;
    }
}

public sealed partial class Checker
{
    private bool isAwaitedTypeInstantiation(Type t)
    {
        if (((t.flags & TypeFlags.Conditional) != 0))
        {
            var awaitedSymbol = this.getGlobalAwaitedSymbolOrNil();
            return ((((awaitedSymbol != null) && (t.alias != null)) && (t.alias.symbol == awaitedSymbol)) && (GoRuntime.Len(t.alias.typeArguments) == 1));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isAwaitedTypeNeeded(Type t)
    {
        if ((IsTypeAny(t) || this.isAwaitedTypeInstantiation(t)))
        {
            return false;
        }

        if (this.isGenericObjectType(t))
        {
            var baseConstraint = this.getBaseConstraintOfType(t);
            if ((baseConstraint != null))
            {
                return ((((baseConstraint.flags & TypeFlags.AnyOrUnknown) != 0) || this.isEmptyObjectType(baseConstraint)) || someType(baseConstraint, this.isThenableType));
            }

            return this.maybeTypeOfKind(t, TypeFlags.TypeVariable);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type createAwaitedTypeIfNeeded(Type t)
    {
        if (this.isAwaitedTypeNeeded(t))
        {
            var awaitedType = this.tryCreateAwaitedType(t);
            if ((awaitedType != null))
            {
                return awaitedType;
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type unwrapAwaitedType(Type t)
    {
        switch (true)
        {
            case true when ((t.flags & TypeFlags.Union) != 0):
            {
                return this.mapType(t, this.unwrapAwaitedType);
            }

            case true when this.isAwaitedTypeInstantiation(t):
            {
                return t.alias.typeArguments[0];
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private bool isThenableType(Type t)
    {
        if (this.allTypesAssignableToKind(this.getBaseConstraintOrType(t), (TypeFlags.Primitive | TypeFlags.Never)))
        {
            return false;
        }

        var thenFunction = this.getTypeOfPropertyOfType(t, "then");
        return ((thenFunction != null) && (GoRuntime.Len(this.getSignaturesOfType(this.getTypeWithFacts(thenFunction, TypeFacts.NEUndefinedOrNull), SignatureKind.Call)) != 0));
    }
}

public sealed partial class Checker
{
    private Type getAwaitedTypeOfPromise(Type t)
    {
        return this.getAwaitedTypeOfPromiseEx(t, null, null);
    }
}

public sealed partial class Checker
{
    private Type getAwaitedTypeOfPromiseEx(Type t, Node errorNode, Message diagnosticMessage, params object[] args)
    {
        var promisedType = this.getPromisedTypeOfPromiseEx(t, errorNode, null);
        if ((promisedType != null))
        {
            return this.getAwaitedTypeEx(promisedType, errorNode, diagnosticMessage, args);
        }

        return null;
    }
}

public sealed partial class Checker
{
    // checker.go:842/843/31976. Diagnostic-only dependency; written async remains excluded.
    private Func<Symbol> getGlobalAwaitedSymbol, getGlobalAwaitedSymbolOrNil;
    private List<Type> awaitedTypeStack;
    private Type tryCreateAwaitedType(Type t)
    {
        var awaitedSymbol = getGlobalAwaitedSymbol();
        if (awaitedSymbol != null)
            return getTypeAliasInstantiation(awaitedSymbol, [unwrapAwaitedType(t)], null);
        return null;
    }
}

public sealed partial class Checker
{
    private void checkTestingKnownTruthyType(Node condExpr, Type condType, Node body)
    {
        var location = condExpr;
        if (Ast.IsLogicalOrCoalescingBinaryExpression(condExpr))
        {
            location = Ast.SkipParentheses(condExpr.AsBinaryExpression().Right);
        }

        if (Ast.IsModuleExportsAccessExpression(location))
        {
            return;
        }

        if (Ast.IsLogicalOrCoalescingBinaryExpression(location))
        {
            this.checkTestingKnownTruthyTypes(location, condType, body);
            return;
        }

        var t = condType;
        if ((location != condExpr))
        {
            t = this.checkExpression(location);
        }

        if (((((t.flags & TypeFlags.EnumLiteral) != 0) && Ast.IsPropertyAccessExpression(location)) && ((Core.OrElse(this.getResolvedSymbolOrNil(location.Expression()), this.unknownSymbol).Flags & SymbolFlags.Enum) != 0)))
        {
            throw unsupported("checkTestingKnownTruthyType: enum");
        }

        var isPropertyExpressionCast = (Ast.IsPropertyAccessExpression(location) && Ast.IsAssertionExpression(location.Expression()));
        if ((!this.hasTypeFacts(t, TypeFacts.Truthy) || isPropertyExpressionCast))
        {
            return;
        }

        var callSignatures = this.getSignaturesOfType(t, SignatureKind.Call);
        var isPromise = (this.getAwaitedTypeOfPromise(t) != null);
        if (((GoRuntime.Len(callSignatures) == 0) && !isPromise))
        {
            return;
        }

        Node testedNode = default;
        switch (true)
        {
            case true when Ast.IsIdentifier(location):
            {
                testedNode = location;
                break;
            }

            case true when Ast.IsPropertyAccessExpression(location):
            {
                testedNode = location.Name();
                break;
            }
        }

        Symbol testedSymbol = default;
        if ((testedNode != null))
        {
            testedSymbol = this.getSymbolAtLocation(testedNode, false);
        }

        if (((testedSymbol == null) && !isPromise))
        {
            return;
        }

        var isUsed = ((((testedSymbol != null) && Ast.IsBinaryExpression(condExpr.Parent)) && this.isSymbolUsedInBinaryExpressionChain(condExpr.Parent, testedSymbol)) || (((testedSymbol != null) && (body != null)) && this.isSymbolUsedInConditionBody(condExpr, body, testedNode, testedSymbol)));
        if (!isUsed)
        {
            if (isPromise)
            {
                this.errorAndMaybeSuggestAwait(location, true, Diagnostics.This_condition_will_always_return_true_since_this_0_is_always_defined, this.getTypeNameForErrorDisplay(t));
            }
            else
            {
                this.error(location, Diagnostics.This_condition_will_always_return_true_since_this_function_is_always_defined_Did_you_mean_to_call_it_instead);
            }
        }
    }
}

public sealed partial class Checker
{
    private bool isSymbolUsedInBinaryExpressionChain(Node node, Symbol testedSymbol)
    {
        Func<Node, bool> visit = default;
        visit = (child) =>
        {
            if (Ast.IsIdentifier(child))
            {
                var symbol = this.getSymbolAtLocation(child, false);
                if (((symbol != null) && (symbol == testedSymbol)))
                {
                    return true;
                }
            }

            return child.ForEachChild(n => visit(n));
        };
        while ((Ast.IsBinaryExpression(node) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.AmpersandAmpersandToken)))
        {
            var isUsed = node.AsBinaryExpression().Right.ForEachChild(n => visit(n));
            if (isUsed)
            {
                return true;
            }

            node = node.Parent;
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isSymbolUsedInConditionBody(Node expr, Node body, Node testedNode, Symbol testedSymbol)
    {
        Func<Node, bool> visit = default;
        visit = (childNode) =>
        {
            if (Ast.IsIdentifier(childNode))
            {
                var childSymbol = this.getSymbolAtLocation(childNode, false);
                if (((childSymbol != null) && (childSymbol == testedSymbol)))
                {
                    if ((Ast.IsIdentifier(expr) || (Ast.IsIdentifier(testedNode) && Ast.IsBinaryExpression(testedNode.Parent))))
                    {
                        return true;
                    }

                    var testedExpression = testedNode.Parent;
                    var childExpression = childNode.Parent;
                    while (((testedExpression != null) && (childExpression != null)))
                    {
                        if (((Ast.IsIdentifier(testedExpression) && Ast.IsIdentifier(childExpression)) || ((testedExpression.Kind == Kind.ThisKeyword) && (childExpression.Kind == Kind.ThisKeyword))))
                        {
                            return (this.getSymbolAtLocation(testedExpression, false) == this.getSymbolAtLocation(childExpression, false));
                        }
                        else
                        {
                            if ((Ast.IsPropertyAccessExpression(testedExpression) && Ast.IsPropertyAccessExpression(childExpression)))
                            {
                                if ((this.getSymbolAtLocation(testedExpression.Name(), false) != this.getSymbolAtLocation(childExpression.Name(), false)))
                                {
                                    return false;
                                }

                                childExpression = childExpression.Expression();
                                testedExpression = testedExpression.Expression();
                            }
                            else
                            {
                                if ((Ast.IsCallExpression(testedExpression) && Ast.IsCallExpression(childExpression)))
                                {
                                    childExpression = childExpression.Expression();
                                    testedExpression = testedExpression.Expression();
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return childNode.ForEachChild(n => visit(n));
        };
        return body.ForEachChild(n => visit(n));
    }
}

public sealed partial class Checker
{
    private Type getFlowTypeOfProperty(Node reference, Symbol prop)
    {
        var initialType = this.undefinedType;
        if ((((prop != null) && (prop.ValueDeclaration != null)) && ((!this.isAutoTypedProperty(prop) || ((prop.ValueDeclaration.ModifierFlags() & ModifierFlags.Ambient) != 0)))))
        {
            {
                var baseType = this.getTypeOfPropertyInBaseClass(prop);
                if ((baseType != null))
                {
                    initialType = baseType;
                }
            }
        }

        return this.getFlowTypeOfReferenceEx(reference, this.autoType, initialType, null, null);
    }
}

public sealed partial class Checker
{
    private Diagnostic errorAndMaybeSuggestAwait(Node location, bool maybeMissingAwait, Message message, params object[] args)
    {
        var diagnostic = this.error(location, message, args);
        if (maybeMissingAwait)
        {
            diagnostic.AddRelatedInfo(createDiagnosticForNode(location, Diagnostics.Did_you_forget_to_use_await));
        }

        return diagnostic;
    }
}

public sealed partial class Checker
{
    private Type getContextualTypeForAssignmentExpression(BinaryExpression binary)
    {
        var left = binary.Left;
        if (Ast.IsElementAccessExpression(left))
            throw unsupported("contextual assignment: element access");
        if (Ast.IsAccessExpression(left))
        {
            var expr = left.Expression();
            switch (expr.Kind)
            {
                case Kind.Identifier:
                {
                    var symbol = this.getExportSymbolOfValueSymbolIfExported(this.getResolvedSymbol(expr));
                    if (((symbol.Flags & SymbolFlags.ModuleExports) != 0))
                    {
                        return null;
                    }

                    if ((binary.Symbol != null))
                    {
                        if (((symbol.ValueDeclaration != null) && Ast.IsVariableDeclaration(symbol.ValueDeclaration)))
                        {
                            {
                                var typeNode = symbol.ValueDeclaration.Type();
                                if ((typeNode != null))
                                {
                                    if (Ast.IsPropertyAccessExpression(left))
                                    {
                                        return this.getTypeOfPropertyOfContextualType(this.getTypeFromTypeNode(typeNode), left.Name().Text());
                                    }

                                    var nameType = this.checkExpressionCached(left.AsElementAccessExpression().ArgumentExpression);
                                    if (isTypeUsableAsPropertyName(nameType))
                                    {
                                        return this.getTypeOfPropertyOfContextualTypeEx(this.getTypeFromTypeNode(typeNode), getPropertyNameFromType(nameType), nameType);
                                    }

                                    return this.getTypeOfExpression(left);
                                }
                            }
                        }

                        return null;
                    }

                    break;
                }

                case Kind.PropertyAccessExpression:
                case Kind.ElementAccessExpression:
                {
                    if ((binary.Symbol != null))
                    {
                        return null;
                    }

                    break;
                }

                case Kind.ThisKeyword:
                {
                    Symbol symbol2 = default;
                    var thisType = this.getTypeOfExpression(expr);
                    if (Ast.IsPropertyAccessExpression(left))
                    {
                        var name = left.Name();
                        if (Ast.IsPrivateIdentifier(name))
                        {
                            throw unsupported("contextual assignment: private class member");
                        }
                        else
                        {
                            symbol2 = this.getPropertyOfType(thisType, name.Text());
                        }
                    }
                    else
                    {
                        var propType = this.checkExpressionCached(left.AsElementAccessExpression().ArgumentExpression);
                        if (isTypeUsableAsPropertyName(propType))
                        {
                            symbol2 = this.getPropertyOfType(thisType, getPropertyNameFromType(propType));
                        }
                    }

                    if ((symbol2 != null))
                    {
                        {
                            var d = symbol2.ValueDeclaration;
                            if (((((d != null) && ((Ast.IsPropertyDeclaration(d) || Ast.IsPropertySignatureDeclaration(d)))) && (d.Type() == null)) && (d.Initializer() == null)))
                            {
                                return null;
                            }
                        }
                    }

                    if ((((binary.Symbol != null) && (binary.Symbol.ValueDeclaration != null)) && (binary.Symbol.ValueDeclaration.Type() == null)))
                    {
                        if (!Ast.IsObjectLiteralMethod(this.getThisContainer(expr, false, false)))
                        {
                            return null;
                        }

                        var name2 = Ast.GetElementOrPropertyAccessName(left);
                        if ((name2 == null))
                        {
                            return null;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    break;
                }
            }
        }

        return this.getTypeOfExpression(left);
    }
}

public sealed partial class Checker
{
    private bool isSameScopedBindingElement(Node node, Node declaration)
    {
        if (Ast.IsBindingElement(declaration))
        {
            var bindingElement = Ast.FindAncestor(node, Ast.IsBindingElement);
            return ((bindingElement != null) && (Ast.GetRootDeclaration(bindingElement) == Ast.GetRootDeclaration(declaration)));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type removeOptionalityFromDeclaredType(Type declaredType, Node declaration)
    {
        var removeUndefined = ((((this.strictNullChecks && Ast.IsParameterDeclaration(declaration)) && (declaration.Initializer() != null)) && this.hasTypeFacts(declaredType, TypeFacts.IsUndefined)) && !this.parameterInitializerContainsUndefined(declaration));
        if (removeUndefined)
        {
            return this.getTypeWithFacts(declaredType, TypeFacts.NEUndefined);
        }

        return declaredType;
    }
}

public sealed partial class Checker
{
    private bool parameterInitializerContainsUndefined(Node declaration)
    {
        var links = this.nodeLinks.Get(declaration);
        if (((links.flags & NodeCheckFlags.InitializerIsUndefinedComputed) == 0))
        {
            if (!this.pushTypeResolution(declaration, TypeSystemPropertyName.InitializerIsUndefined))
            {
                this.reportCircularityError(declaration.Symbol());
                return true;
            }

            var containsUndefined = this.hasTypeFacts(this.checkDeclarationInitializer(declaration, CheckMode.Normal, null), TypeFacts.IsUndefined);
            if (!this.popTypeResolution())
            {
                this.reportCircularityError(declaration.Symbol());
                return true;
            }

            if (((links.flags & NodeCheckFlags.InitializerIsUndefinedComputed) == 0))
            {
                links.flags |= (NodeCheckFlags.InitializerIsUndefinedComputed | Core.IfElse(containsUndefined, NodeCheckFlags.InitializerIsUndefined, NodeCheckFlags.None));
            }
        }

        return ((links.flags & NodeCheckFlags.InitializerIsUndefined) != 0);
    }
}

public sealed partial class Checker
{
    private bool isInAmbientOrTypeNode(Node node)
    {
        return (((node.Flags & NodeFlags.Ambient) != 0) || (Ast.FindAncestor(node, (n) =>
        {
            return (((Ast.IsInterfaceDeclaration(n) || Ast.IsTypeAliasDeclaration(n)) || Ast.IsJSTypeAliasDeclaration(n)) || Ast.IsTypeLiteralNode(n));
        }) != null));
    }
}

public sealed partial class Checker
{
    private Type getTypeOfPropertyInBaseClass(Symbol property)
    {
        var classType = this.getDeclaringClass(property);
        if ((classType != null))
        {
            var baseClassTypes = this.getBaseTypes(classType);
            if ((GoRuntime.Len(baseClassTypes) > 0))
            {
                return this.getTypeOfPropertyOfType(baseClassTypes[0], property.Name);
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getDeclaringClass(Symbol prop)
    {
        if (((prop.Parent != null) && ((prop.Parent.Flags & SymbolFlags.Class) != 0)))
        {
            return this.getDeclaredTypeOfSymbol(this.getParentOfSymbol(prop));
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool isInPropertyInitializerOrClassStaticBlock(Node node, bool ignoreArrowFunctions)
    {
        return (Ast.FindAncestorOrQuit(node, (node2) =>
        {
            switch (node2.Kind)
            {
                case Kind.PropertyDeclaration:
                case Kind.ClassStaticBlockDeclaration:
                {
                    return FindAncestorResult.True;
                }

                case Kind.TypeQuery:
                case Kind.JsxClosingElement:
                {
                    return FindAncestorResult.Quit;
                }

                case Kind.ArrowFunction:
                {
                    return Core.IfElse(ignoreArrowFunctions, FindAncestorResult.False, FindAncestorResult.Quit);
                }

                case Kind.Block:
                {
                    return Core.IfElse((Ast.IsFunctionLikeDeclaration(node2.Parent) && (node2.Parent.Kind != Kind.ArrowFunction)), FindAncestorResult.Quit, FindAncestorResult.False);
                }

                default:
                {
                    return FindAncestorResult.False;
                }
            }
        }) != null);
    }
}

public sealed partial class Checker
{
    private bool isAutoTypedProperty(Symbol symbol)
    {
        var declaration = symbol.ValueDeclaration;
        return (((((declaration != null) && Ast.IsPropertyDeclaration(declaration)) && (declaration.Type() == null)) && (declaration.Initializer() == null)) && this.noImplicitAny);
    }
}

public sealed partial class Checker
{
    private static bool shouldMarkIdentifierAliasReferenced(Node node)
    {
        var parent = node.Parent;
        if ((parent != null))
        {
            if ((Ast.IsPropertyAccessExpression(parent) && (parent.Expression() == node)))
            {
                return false;
            }

            if ((Ast.IsExportSpecifier(parent) && parent.IsTypeOnly()))
            {
                return false;
            }

            if ((parent.Parent != null))
            {
                var greatGrandparent = parent.Parent.Parent;
                if ((((greatGrandparent != null) && Ast.IsExportDeclaration(greatGrandparent)) && greatGrandparent.IsTypeOnly()))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public sealed partial class Checker
{
    private void markIdentifierAliasReferenced(Node location)
    {
        if (Ast.IsThisInTypeQuery(location))
        {
            return;
        }

        var symbol = this.getResolvedSymbol(location);
        if ((((symbol != null) && (symbol != this.argumentsSymbol)) && (symbol != this.unknownSymbol)))
        {
            this.markAliasReferenced(symbol, location);
        }
    }
}

public sealed partial class Checker
{
    private readonly Dictionary<Type, Node> patternForType = new();
    private static bool throwUnsupportedDestructuring(Node node) => throw unsupported("destructuring assignment");
    private Node getDeclarationOfAliasSymbol(Symbol symbol) => throw unsupported("import alias declaration");
    // checker.go:29331. Fixed VerbatimModuleSyntax=false; source imports are not admitted.
    private void markAliasReferenced(Symbol symbol, Node location)
    {
        if (Ast.IsNonLocalAlias(symbol, SymbolFlags.Value) && !IsInTypeQuery(location))
            throw unsupported("non-local import alias references");
    }
}

public sealed partial class Checker
{
    private Type checkComputedPropertyName(Node node) => throw unsupported("computed property name expression");
}

public sealed partial class Checker
{
    // The fixed profile does not enable strictPropertyInitialization; source classes excluded.
    private readonly bool strictPropertyInitialization = false;
    // checker.go:24165 selected diagnostic-library guard. A one-parameter Awaited alias
    // can only be supplied by excluded source generics/library environments.
    private Type getTypeAliasInstantiation(Symbol symbol, List<Type> typeArguments, TypeAlias alias)
    {
        var t = getDeclaredTypeOfSymbol(symbol);
        throw unsupported("type alias generic instantiation");
    }

    private Type getTypeOfPropertyOfContextualType(Type t, string name) => getTypeOfPropertyOfContextualTypeEx(t, name, null);
    private Diagnostic addDeprecatedSuggestion(Node node, List<Node> declarations, string name) => throw unsupported("deprecated JSDoc tag semantics");
}

public sealed partial class Checker
{
    private bool isUncalledFunctionReference(Node node, Symbol symbol)
    {
        if (((symbol.Flags & ((SymbolFlags.Function | SymbolFlags.Method))) != 0))
        {
            var parent = Ast.FindAncestor(node.Parent, (n) =>
            {
                return !Ast.IsAccessExpression(n);
            });
            if ((parent == null))
            {
                parent = node.Parent;
            }

            if (Ast.IsCallLikeExpression(parent))
            {
                return ((Ast.IsCallOrNewExpression(parent) && Ast.IsIdentifier(node)) && this.hasMatchingArgument(parent, node));
            }

            return Core.Every(symbol.Declarations, (d) =>
            {
                return (!Ast.IsFunctionLike(d) || this.IsDeprecatedDeclaration(d));
            });
        }

        return true;
    }
}

public sealed partial class Checker
{
    private Type checkTypeOfExpression(Node node)
    {
        this.checkExpression(node.Expression());
        return this.typeofType;
    }
}

public sealed partial class Checker
{
    private Type checkVoidExpression(Node node)
    {
        this.checkNodeDeferred(node);
        return this.undefinedWideningType;
    }
}

public sealed partial class Checker
{
    private Type checkPrefixUnaryExpression(Node node)
    {
        var expr = node.AsPrefixUnaryExpression();
        var operandType = this.checkExpression(expr.Operand);
        if ((operandType == this.silentNeverType))
        {
            return this.silentNeverType;
        }

        switch (expr.Operand.Kind)
        {
            case Kind.NumericLiteral:
            {
                switch (expr.Operator)
                {
                    case Kind.MinusToken:
                    {
                        return this.getFreshTypeOfLiteralType(this.getNumberLiteralType(-JsNum.FromString(expr.Operand.Text())));
                    }

                    case Kind.PlusToken:
                    {
                        return this.getFreshTypeOfLiteralType(this.getNumberLiteralType(+JsNum.FromString(expr.Operand.Text())));
                    }
                }

                break;
            }

            case Kind.BigIntLiteral:
            {
                throw unsupported("prefix unary: bigint literal source");
            }
        }

        switch (expr.Operator)
        {
            case Kind.PlusToken:
            case Kind.MinusToken:
            case Kind.TildeToken:
            {
                this.checkNonNullType(operandType, expr.Operand);
                if (this.maybeTypeOfKindConsideringBaseConstraint(operandType, TypeFlags.ESSymbolLike))
                {
                    this.error(expr.Operand, Diagnostics.The_0_operator_cannot_be_applied_to_type_symbol, TypeScript.Scanner.Scanner.TokenToString(expr.Operator));
                }

                if ((expr.Operator == Kind.PlusToken))
                {
                    if (this.maybeTypeOfKindConsideringBaseConstraint(operandType, TypeFlags.BigIntLike))
                    {
                        this.error(expr.Operand, Diagnostics.Operator_0_cannot_be_applied_to_type_1, TypeScript.Scanner.Scanner.TokenToString(expr.Operator), this.typeToStringByteString(this.getBaseTypeOfLiteralType(operandType)));
                    }

                    return this.numberType;
                }

                return this.getUnaryResultType(operandType);
            }

            case Kind.ExclamationToken:
            {
                this.checkTruthinessOfType(operandType, expr.Operand);
                var facts = this.getTypeFacts(operandType, (TypeFacts.Truthy | TypeFacts.Falsy));
                switch (true)
                {
                    case true when (facts == TypeFacts.Truthy):
                    {
                        return this.falseType;
                    }

                    case true when (facts == TypeFacts.Falsy):
                    {
                        return this.trueType;
                    }

                    default:
                    {
                        return this.booleanType;
                    }
                }
            }

            case Kind.PlusPlusToken:
            case Kind.MinusMinusToken:
            {
                throw unsupported("prefix unary: increment/decrement");
            }
        }

        return this.errorType;
    }
}

public sealed partial class Checker
{
    private Type getUnaryResultType(Type operandType)
    {
        if (this.maybeTypeOfKind(operandType, TypeFlags.BigIntLike))
        {
            if ((this.isTypeAssignableToKind(operandType, TypeFlags.AnyOrUnknown) || this.maybeTypeOfKind(operandType, TypeFlags.NumberLike)))
            {
                return this.numberOrBigIntType;
            }

            return this.bigintType;
        }

        return this.numberType;
    }
}

public sealed partial class Checker
{
    private Type checkConditionalExpression(Node node, CheckMode checkMode)
    {
        var cond = node.AsConditionalExpression();
        var t = this.checkTruthinessExpression(cond.Condition, checkMode);
        this.checkTestingKnownTruthyCallableOrAwaitableOrEnumMemberType(cond.Condition, t, cond.WhenTrue);
        var type1 = this.checkExpressionEx(cond.WhenTrue, checkMode);
        var type2 = this.checkExpressionEx(cond.WhenFalse, checkMode);
        return this.getUnionTypeEx(new List<Type> { type1, type2 }, UnionReduction.Subtype, null, null);
    }
}

public sealed partial class Checker
{
    private Type checkBinaryExpression(Node node, CheckMode checkMode)
    {
        var binary = node.AsBinaryExpression();
        return this.checkBinaryLikeExpression(binary.Left, binary.OperatorToken, binary.Right, checkMode, node);
    }
}

public sealed partial class Checker
{
    private Type checkBinaryLikeExpression(Node left, Node operatorToken, Node right, CheckMode checkMode, Node errorNode)
    {
        var @operator = operatorToken.Kind;
        if (((@operator == Kind.EqualsToken) && (((left.Kind == Kind.ObjectLiteralExpression) || (left.Kind == Kind.ArrayLiteralExpression)))))
        {
            throw unsupported("binary expression: destructuring assignment");
        }

        var leftType = this.checkExpressionEx(left, checkMode);
        var rightType = this.checkExpressionEx(right, checkMode);
        if (Ast.IsLogicalOrCoalescingBinaryOperator(@operator))
        {
            var parent = left.Parent.Parent;
            while ((Ast.IsParenthesizedExpression(parent) || Ast.IsLogicalOrCoalescingBinaryExpression(parent)))
            {
                parent = parent.Parent;
            }

            if (((@operator == Kind.AmpersandAmpersandToken) || Ast.IsIfStatement(parent)))
            {
                Node body = default;
                if (Ast.IsIfStatement(parent))
                {
                    body = parent.AsIfStatement().ThenStatement;
                }

                this.checkTestingKnownTruthyCallableOrAwaitableOrEnumMemberType(left, leftType, body);
            }

            if (Ast.IsLogicalBinaryOperator(@operator))
            {
                this.checkTruthinessOfType(leftType, left);
            }
        }

        switch (@operator)
        {
            case Kind.AsteriskToken:
            case Kind.AsteriskAsteriskToken:
            case Kind.AsteriskEqualsToken:
            case Kind.AsteriskAsteriskEqualsToken:
            case Kind.SlashToken:
            case Kind.SlashEqualsToken:
            case Kind.PercentToken:
            case Kind.PercentEqualsToken:
            case Kind.MinusToken:
            case Kind.MinusEqualsToken:
            case Kind.LessThanLessThanToken:
            case Kind.LessThanLessThanEqualsToken:
            case Kind.GreaterThanGreaterThanToken:
            case Kind.GreaterThanGreaterThanEqualsToken:
            case Kind.GreaterThanGreaterThanGreaterThanToken:
            case Kind.GreaterThanGreaterThanGreaterThanEqualsToken:
            case Kind.BarToken:
            case Kind.BarEqualsToken:
            case Kind.CaretToken:
            case Kind.CaretEqualsToken:
            case Kind.AmpersandToken:
            case Kind.AmpersandEqualsToken:
            {
                if (((leftType == this.silentNeverType) || (rightType == this.silentNeverType)))
                {
                    return this.silentNeverType;
                }

                leftType = this.checkNonNullType(leftType, left);
                rightType = this.checkNonNullType(rightType, right);
                if ((((leftType.flags & TypeFlags.BooleanLike) != 0) && ((rightType.flags & TypeFlags.BooleanLike) != 0)))
                {
                    var suggestedOperator = this.getSuggestedBooleanOperator(@operator);
                    if ((suggestedOperator != Kind.Unknown))
                    {
                        this.error(operatorToken, Diagnostics.The_0_operator_is_not_allowed_for_boolean_types_Consider_using_1_instead, TypeScript.Scanner.Scanner.TokenToString(operatorToken.Kind), TypeScript.Scanner.Scanner.TokenToString(suggestedOperator));
                        return this.numberType;
                    }
                }

                var leftOk = this.checkArithmeticOperandType(left, leftType, Diagnostics.The_left_hand_side_of_an_arithmetic_operation_must_be_of_type_any_number_bigint_or_an_enum_type, true);
                var rightOk = this.checkArithmeticOperandType(right, rightType, Diagnostics.The_right_hand_side_of_an_arithmetic_operation_must_be_of_type_any_number_bigint_or_an_enum_type, true);
                Type resultType = default;
                if (((this.isTypeAssignableToKind(leftType, TypeFlags.AnyOrUnknown) && this.isTypeAssignableToKind(rightType, TypeFlags.AnyOrUnknown)) || (!this.maybeTypeOfKind(leftType, TypeFlags.BigIntLike) && !this.maybeTypeOfKind(rightType, TypeFlags.BigIntLike))))
                {
                    resultType = this.numberType;
                }
                else
                {
                    if (this.bothAreBigIntLike(leftType, rightType))
                    {
                        switch (@operator)
                        {
                            case Kind.GreaterThanGreaterThanGreaterThanToken:
                            case Kind.GreaterThanGreaterThanGreaterThanEqualsToken:
                            {
                                this.reportOperatorError(leftType, @operator, rightType, errorNode, null);
                                break;
                            }

                            case Kind.AsteriskAsteriskToken:
                            case Kind.AsteriskAsteriskEqualsToken:
                            {
                                if ((this.compilerOptions.GetEmitScriptTarget() < ScriptTarget.ES2016))
                                {
                                    this.error(errorNode, Diagnostics.Exponentiation_cannot_be_performed_on_bigint_values_unless_the_target_option_is_set_to_es2016_or_later);
                                }

                                break;
                            }
                        }

                        resultType = this.bigintType;
                    }
                    else
                    {
                        this.reportOperatorError(leftType, @operator, rightType, errorNode, this.bothAreBigIntLike);
                        resultType = this.errorType;
                    }
                }

                if ((leftOk && rightOk))
                {
                    this.checkAssignmentOperator(left, @operator, right, leftType, resultType);
                    switch (@operator)
                    {
                        case Kind.LessThanLessThanToken:
                        case Kind.LessThanLessThanEqualsToken:
                        case Kind.GreaterThanGreaterThanToken:
                        case Kind.GreaterThanGreaterThanEqualsToken:
                        case Kind.GreaterThanGreaterThanGreaterThanToken:
                        case Kind.GreaterThanGreaterThanGreaterThanEqualsToken:
                        {
                            var rhsEval = this.evaluate(right, right);
                            {
                                if (rhsEval.Value is JsNum.Number numValue && numValue.Abs() >= 32)
                                {
                                    this.errorOrSuggestion(Ast.IsEnumMember(Ast.WalkUpParenthesizedExpressions(right.Parent.Parent)), errorNode, Diagnostics.This_operation_can_be_simplified_This_shift_is_identical_to_0_1_2, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(left), TypeScript.Scanner.Scanner.TokenToString(@operator), numValue.Remainder(32));
                                }
                            }

                            break;
                        }
                    }
                }

                return resultType;
            }

            case Kind.PlusToken:
            case Kind.PlusEqualsToken:
            {
                if (((leftType == this.silentNeverType) || (rightType == this.silentNeverType)))
                {
                    return this.silentNeverType;
                }

                if ((!this.isTypeAssignableToKind(leftType, TypeFlags.StringLike) && !this.isTypeAssignableToKind(rightType, TypeFlags.StringLike)))
                {
                    leftType = this.checkNonNullType(leftType, left);
                    rightType = this.checkNonNullType(rightType, right);
                }

                Type resultType2 = default;
                if ((this.isTypeAssignableToKindEx(leftType, TypeFlags.NumberLike, true) && this.isTypeAssignableToKindEx(rightType, TypeFlags.NumberLike, true)))
                {
                    resultType2 = this.numberType;
                }
                else
                {
                    if ((this.isTypeAssignableToKindEx(leftType, TypeFlags.BigIntLike, true) && this.isTypeAssignableToKindEx(rightType, TypeFlags.BigIntLike, true)))
                    {
                        resultType2 = this.bigintType;
                    }
                    else
                    {
                        if ((this.isTypeAssignableToKindEx(leftType, TypeFlags.StringLike, true) || this.isTypeAssignableToKindEx(rightType, TypeFlags.StringLike, true)))
                        {
                            resultType2 = this.stringType;
                        }
                        else
                        {
                            if ((IsTypeAny(leftType) || IsTypeAny(rightType)))
                            {
                                if ((this.isErrorType(leftType) || this.isErrorType(rightType)))
                                {
                                    resultType2 = this.errorType;
                                }
                                else
                                {
                                    resultType2 = this.anyType;
                                }
                            }
                        }
                    }
                }

                if (((resultType2 != null) && !this.checkForDisallowedESSymbolOperand(left, right, leftType, rightType, @operator)))
                {
                    return resultType2;
                }

                if ((resultType2 == null))
                {
                    var closeEnoughKind = (((TypeFlags.NumberLike | TypeFlags.BigIntLike) | TypeFlags.StringLike) | TypeFlags.AnyOrUnknown);
                    this.reportOperatorError(leftType, @operator, rightType, errorNode, (left2, right2) =>
                    {
                        return (this.isTypeAssignableToKind(left2, closeEnoughKind) && this.isTypeAssignableToKind(right2, closeEnoughKind));
                    });
                    return this.anyType;
                }

                if ((@operator == Kind.PlusEqualsToken))
                {
                    this.checkAssignmentOperator(left, @operator, right, leftType, resultType2);
                }

                return resultType2;
            }

            case Kind.LessThanToken:
            case Kind.GreaterThanToken:
            case Kind.LessThanEqualsToken:
            case Kind.GreaterThanEqualsToken:
            {
                if (this.checkForDisallowedESSymbolOperand(left, right, leftType, rightType, @operator))
                {
                    leftType = this.getBaseTypeOfLiteralTypeForComparison(this.checkNonNullType(leftType, left));
                    rightType = this.getBaseTypeOfLiteralTypeForComparison(this.checkNonNullType(rightType, right));
                    this.reportOperatorErrorUnless(leftType, @operator, rightType, errorNode, (left3, right3) =>
                    {
                        if ((IsTypeAny(left3) || IsTypeAny(right3)))
                        {
                            return true;
                        }

                        var leftAssignableToNumber = this.isTypeAssignableTo(left3, this.numberOrBigIntType);
                        var rightAssignableToNumber = this.isTypeAssignableTo(right3, this.numberOrBigIntType);
                        return ((leftAssignableToNumber && rightAssignableToNumber) || ((!leftAssignableToNumber && !rightAssignableToNumber) && this.areTypesComparable(left3, right3)));
                    });
                }

                return this.booleanType;
            }

            case Kind.EqualsEqualsToken:
            case Kind.ExclamationEqualsToken:
            case Kind.EqualsEqualsEqualsToken:
            case Kind.ExclamationEqualsEqualsToken:
            {
                if (((checkMode & CheckMode.TypeOnly) == 0))
                {
                    if ((((isLiteralExpressionOfObject(left) || isLiteralExpressionOfObject(right))) && ((!Ast.IsInJSFile(left) || (((@operator == Kind.EqualsEqualsEqualsToken) || (@operator == Kind.ExclamationEqualsEqualsToken)))))))
                    {
                        var eqType = ((@operator == Kind.EqualsEqualsToken) || (@operator == Kind.EqualsEqualsEqualsToken));
                        this.error(errorNode, Diagnostics.This_condition_will_always_return_0_since_JavaScript_compares_objects_by_reference_not_value, Core.IfElse(eqType, "false", "true"));
                    }

                    this.checkNaNEquality(errorNode, @operator, left, right);
                    this.reportOperatorErrorUnless(leftType, @operator, rightType, errorNode, (left4, right4) =>
                    {
                        return (this.isTypeEqualityComparableTo(left4, right4) || this.isTypeEqualityComparableTo(right4, left4));
                    });
                }

                return this.booleanType;
            }

            case Kind.InstanceOfKeyword:
            {
                throw unsupported("instanceof expression");
            }

            case Kind.InKeyword:
            {
                throw unsupported("in expression");
            }

            case Kind.AmpersandAmpersandToken:
            case Kind.AmpersandAmpersandEqualsToken:
            {
                var resultType3 = leftType;
                if (this.hasTypeFacts(leftType, TypeFacts.Truthy))
                {
                    var t = leftType;
                    if (!this.strictNullChecks)
                    {
                        t = this.getBaseTypeOfLiteralType(rightType);
                    }

                    resultType3 = this.getUnionType(new List<Type> { this.extractDefinitelyFalsyTypes(t), rightType });
                }

                if ((@operator == Kind.AmpersandAmpersandEqualsToken))
                {
                    this.checkAssignmentOperator(left, @operator, right, leftType, rightType);
                }

                return resultType3;
            }

            case Kind.BarBarToken:
            case Kind.BarBarEqualsToken:
            {
                var resultType4 = leftType;
                if (this.hasTypeFacts(leftType, TypeFacts.Falsy))
                {
                    resultType4 = this.getUnionTypeEx(new List<Type> { this.GetNonNullableType(this.removeDefinitelyFalsyTypes(leftType)), rightType }, UnionReduction.Subtype, null, null);
                }

                if ((@operator == Kind.BarBarEqualsToken))
                {
                    this.checkAssignmentOperator(left, @operator, right, leftType, rightType);
                }

                return resultType4;
            }

            case Kind.QuestionQuestionToken:
            case Kind.QuestionQuestionEqualsToken:
            {
                if ((@operator == Kind.QuestionQuestionToken))
                {
                    this.checkNullishCoalesceOperands(left, right);
                }

                var resultType5 = leftType;
                if (this.hasTypeFacts(leftType, TypeFacts.EQUndefinedOrNull))
                {
                    resultType5 = this.getUnionTypeEx(new List<Type> { this.GetNonNullableType(leftType), rightType }, UnionReduction.Subtype, null, null);
                }

                if ((@operator == Kind.QuestionQuestionEqualsToken))
                {
                    this.checkAssignmentOperator(left, @operator, right, leftType, rightType);
                }

                return resultType5;
            }

            case Kind.EqualsToken:
            {
                this.checkAssignmentOperator(left, @operator, right, leftType, rightType);
                return rightType;
            }

            case Kind.CommaToken:
            {
                throw unsupported("comma expression");
            }
        }

        throw new InvalidOperationException("Unhandled case in checkBinaryLikeExpression");
    }
}

public sealed partial class Checker
{
    private void reportOperatorError(Type leftType, Kind @operator, Type rightType, Node errorNode, Func<Type, Type, bool> isRelated)
    {
        var wouldWorkWithAwait = false;
        if ((isRelated != null))
        {
            var awaitedLeftType = this.getAwaitedTypeNoAlias(leftType);
            var awaitedRightType = this.getAwaitedTypeNoAlias(rightType);
            wouldWorkWithAwait = (((!(((awaitedLeftType == leftType) && (awaitedRightType == rightType))) && (awaitedLeftType != null)) && (awaitedRightType != null)) && isRelated(awaitedLeftType, awaitedRightType));
        }

        var effectiveLeft = leftType;
        var effectiveRight = rightType;
        if ((!wouldWorkWithAwait && (isRelated != null)))
        {
            (effectiveLeft, effectiveRight) = this.getBaseTypesIfUnrelated(leftType, rightType, isRelated);
        }

        var tuple1 = this.getTypeNamesForErrorDisplay(effectiveLeft, effectiveRight);
        var leftStr = tuple1.Item1;
        var rightStr = tuple1.Item2;
        switch (@operator)
        {
            case Kind.EqualsEqualsEqualsToken:
            case Kind.EqualsEqualsToken:
            case Kind.ExclamationEqualsEqualsToken:
            case Kind.ExclamationEqualsToken:
            {
                this.errorAndMaybeSuggestAwait(errorNode, wouldWorkWithAwait, Diagnostics.This_comparison_appears_to_be_unintentional_because_the_types_0_and_1_have_no_overlap, leftStr, rightStr);
                break;
            }

            default:
            {
                this.errorAndMaybeSuggestAwait(errorNode, wouldWorkWithAwait, Diagnostics.Operator_0_cannot_be_applied_to_types_1_and_2, TypeScript.Scanner.Scanner.TokenToString(@operator), leftStr, rightStr);
                break;
            }
        }
    }
}

public sealed partial class Checker
{
    private void reportOperatorErrorUnless(Type leftType, Kind @operator, Type rightType, Node errorNode, Func<Type, Type, bool> typesAreCompatible)
    {
        if (!typesAreCompatible(leftType, rightType))
        {
            this.reportOperatorError(leftType, @operator, rightType, errorNode, typesAreCompatible);
        }
    }
}

public sealed partial class Checker
{
    private (Type, Type) getBaseTypesIfUnrelated(Type leftType, Type rightType, Func<Type, Type, bool> isRelated)
    {
        var effectiveLeft = leftType;
        var effectiveRight = rightType;
        var leftBase = this.getBaseTypeOfLiteralType(leftType);
        var rightBase = this.getBaseTypeOfLiteralType(rightType);
        if (!isRelated(leftBase, rightBase))
        {
            effectiveLeft = leftBase;
            effectiveRight = rightBase;
        }

        return (effectiveLeft, effectiveRight);
    }
}

public sealed partial class Checker
{
    private void checkAssignmentOperator(Node left, Kind @operator, Node right, Type leftType, Type rightType)
    {
        if (Ast.IsAssignmentOperator(@operator))
        {
            if ((Ast.IsDeclarationNode(left.Parent) && (Ast.GetAssignmentDeclarationKind(left.Parent) == JSDeclarationKind.ExportsProperty)))
            {
                {
                    var symbol = this.symbolNodeLinks.Get(left).resolvedSymbol;
                    if ((((symbol != null) && (GoRuntime.Len(symbol.Declarations) > 1)) && ((rightType.flags & TypeFlags.Undefined) != 0)))
                    {
                        return;
                    }
                }
            }

            if ((Ast.IsCompoundAssignment(@operator) && Ast.IsPropertyAccessExpression(left)))
            {
                throw unsupported("compound assignment");
            }

            if (this.checkReferenceExpression(left, Diagnostics.The_left_hand_side_of_an_assignment_expression_must_be_a_variable_or_a_property_access, Diagnostics.The_left_hand_side_of_an_assignment_expression_may_not_be_an_optional_property_access))
            {
                Message headMessage = default;
                if (((this.exactOptionalPropertyTypes && Ast.IsPropertyAccessExpression(left)) && this.maybeTypeOfKind(rightType, TypeFlags.Undefined)))
                {
                    throw unsupported("exact optional assignment option");
                }

                this.checkTypeAssignableToAndOptionallyElaborate(rightType, leftType, left, right, headMessage, null);
            }
        }
    }
}

public sealed partial class Checker
{
    private bool bothAreBigIntLike(Type left, Type right)
    {
        return (this.isTypeAssignableToKind(left, TypeFlags.BigIntLike) && this.isTypeAssignableToKind(right, TypeFlags.BigIntLike));
    }
}

public sealed partial class Checker
{
    private Kind getSuggestedBooleanOperator(Kind @operator)
    {
        switch (@operator)
        {
            case Kind.BarToken:
            case Kind.BarEqualsToken:
            {
                return Kind.BarBarToken;
            }

            case Kind.CaretToken:
            case Kind.CaretEqualsToken:
            {
                return Kind.ExclamationEqualsEqualsToken;
            }

            case Kind.AmpersandToken:
            case Kind.AmpersandEqualsToken:
            {
                return Kind.AmpersandAmpersandToken;
            }
        }

        return Kind.Unknown;
    }
}

public sealed partial class Checker
{
    private bool checkArithmeticOperandType(Node operand, Type t, Message diagnostic, bool isAwaitValid)
    {
        if (!this.isTypeAssignableTo(t, this.numberOrBigIntType))
        {
            Type awaitedType = default;
            if (isAwaitValid)
            {
                awaitedType = this.getAwaitedTypeOfPromise(t);
            }

            this.errorAndMaybeSuggestAwait(operand, ((awaitedType != null) && this.isTypeAssignableTo(awaitedType, this.numberOrBigIntType)), diagnostic);
            return false;
        }

        return true;
    }
}

public sealed partial class Checker
{
    private bool checkForDisallowedESSymbolOperand(Node left, Node right, Type leftType, Type rightType, Kind @operator)
    {
        Node offendingSymbolOperand = default;
        switch (true)
        {
            case true when this.maybeTypeOfKindConsideringBaseConstraint(leftType, TypeFlags.ESSymbolLike):
            {
                offendingSymbolOperand = left;
                break;
            }

            case true when this.maybeTypeOfKindConsideringBaseConstraint(rightType, TypeFlags.ESSymbolLike):
            {
                offendingSymbolOperand = right;
                break;
            }
        }

        if ((offendingSymbolOperand != null))
        {
            this.error(offendingSymbolOperand, Diagnostics.The_0_operator_cannot_be_applied_to_type_symbol, TypeScript.Scanner.Scanner.TokenToString(@operator));
            return false;
        }

        return true;
    }
}

public sealed partial class Checker
{
    private void checkNaNEquality(Node errorNode, Kind @operator, Node left, Node right)
    {
        var isLeftNaN = this.isGlobalNaN(Ast.SkipParentheses(left));
        var isRightNaN = this.isGlobalNaN(Ast.SkipParentheses(right));
        if ((isLeftNaN || isRightNaN))
        {
            var err = this.error(errorNode, Diagnostics.This_condition_will_always_return_0, TypeScript.Scanner.Scanner.TokenToString(Core.IfElse(((@operator == Kind.EqualsEqualsEqualsToken) || (@operator == Kind.EqualsEqualsToken)), Kind.FalseKeyword, Kind.TrueKeyword)));
            if ((isLeftNaN && isRightNaN))
            {
                return;
            }

            string operatorString = "";
            if (((@operator == Kind.ExclamationEqualsEqualsToken) || (@operator == Kind.ExclamationEqualsToken)))
            {
                operatorString = TypeScript.Scanner.Scanner.TokenToString(Kind.ExclamationToken);
            }

            var location = left;
            if (isLeftNaN)
            {
                location = right;
            }

            var expression = Ast.SkipParentheses(location);
            var entityName = "...";
            if (Ast.IsEntityNameExpression(expression))
            {
                entityName = entityNameToString(expression);
            }

            var suggestion = (((operatorString + "Number.isNaN(") + entityName) + ")");
            err.AddRelatedInfo(createDiagnosticForNode(location, Diagnostics.Did_you_mean_0, suggestion));
        }
    }
}

public sealed partial class Checker
{
    private bool isGlobalNaN(Node expr)
    {
        if ((Ast.IsIdentifier(expr) && (expr.Text() == "NaN")))
        {
            var globalNaNSymbol = this.getGlobalNaNSymbolOrNil();
            return ((globalNaNSymbol != null) && (globalNaNSymbol == this.getResolvedSymbol(expr)));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isTypeEqualityComparableTo(Type source, Type target)
    {
        return ((((target.flags & TypeFlags.Nullable)) != 0) || this.isTypeComparableTo(source, target));
    }
}

public sealed partial class Checker
{
    private bool checkReferenceExpression(Node expr, Message invalidReferenceMessage, Message invalidOptionalChainMessage)
    {
        var node = Ast.SkipOuterExpressions(expr, (OuterExpressionKinds.Assertions | OuterExpressionKinds.Parentheses));
        if (((node.Kind != Kind.Identifier) && !Ast.IsAccessExpression(node)))
        {
            this.error(expr, invalidReferenceMessage);
            return false;
        }

        if (((node.Flags & NodeFlags.OptionalChain) != 0))
        {
            this.error(expr, invalidOptionalChainMessage);
            return false;
        }

        return true;
    }
}

public sealed partial class Checker
{
    private void checkNullishCoalesceOperands(Node left, Node right)
    {
        if (Ast.IsBinaryExpression(left.Parent.Parent))
        {
            var grandparentLeft = left.Parent.Parent.AsBinaryExpression().Left;
            var grandparentOperatorToken = left.Parent.Parent.AsBinaryExpression().OperatorToken;
            if ((Ast.IsBinaryExpression(grandparentLeft) && (grandparentOperatorToken.Kind == Kind.BarBarToken)))
            {
                this.grammarErrorOnNode(grandparentLeft, Diagnostics.X_0_and_1_operations_cannot_be_mixed_without_parentheses, TypeScript.Scanner.Scanner.TokenToString(Kind.QuestionQuestionToken), TypeScript.Scanner.Scanner.TokenToString(grandparentOperatorToken.Kind));
            }
        }
        else
        {
            if (Ast.IsBinaryExpression(left))
            {
                var operatorToken = left.AsBinaryExpression().OperatorToken;
                if (((operatorToken.Kind == Kind.BarBarToken) || (operatorToken.Kind == Kind.AmpersandAmpersandToken)))
                {
                    this.grammarErrorOnNode(left, Diagnostics.X_0_and_1_operations_cannot_be_mixed_without_parentheses, TypeScript.Scanner.Scanner.TokenToString(operatorToken.Kind), TypeScript.Scanner.Scanner.TokenToString(Kind.QuestionQuestionToken));
                }
            }
            else
            {
                if (Ast.IsBinaryExpression(right))
                {
                    var operatorToken2 = right.AsBinaryExpression().OperatorToken;
                    if ((operatorToken2.Kind == Kind.AmpersandAmpersandToken))
                    {
                        this.grammarErrorOnNode(right, Diagnostics.X_0_and_1_operations_cannot_be_mixed_without_parentheses, TypeScript.Scanner.Scanner.TokenToString(Kind.QuestionQuestionToken), TypeScript.Scanner.Scanner.TokenToString(operatorToken2.Kind));
                    }
                }
            }
        }

        this.checkNullishCoalesceOperandLeft(left);
    }
}

public sealed partial class Checker
{
    private void checkNullishCoalesceOperandLeft(Node left)
    {
        var leftTarget = Ast.SkipOuterExpressions(left, OuterExpressionKinds.All);
        var nullishSemantics = this.getSyntacticNullishnessSemantics(leftTarget);
        if ((nullishSemantics != PredicateSemantics.Sometimes))
        {
            if ((nullishSemantics == PredicateSemantics.Always))
            {
                this.error(leftTarget, Diagnostics.This_expression_is_always_nullish);
            }
            else
            {
                this.error(leftTarget, Diagnostics.Right_operand_of_is_unreachable_because_the_left_operand_is_never_nullish);
            }
        }
    }
}

public sealed partial class Checker
{
    private PredicateSemantics getSyntacticNullishnessSemantics(Node node)
    {
        node = Ast.SkipOuterExpressions(node, OuterExpressionKinds.All);
        switch (node.Kind)
        {
            case Kind.AwaitExpression:
            case Kind.CallExpression:
            case Kind.TaggedTemplateExpression:
            case Kind.ElementAccessExpression:
            case Kind.MetaProperty:
            case Kind.NewExpression:
            case Kind.PropertyAccessExpression:
            case Kind.YieldExpression:
            case Kind.ThisKeyword:
            {
                return PredicateSemantics.Sometimes;
            }

            case Kind.BinaryExpression:
            {
                switch (node.AsBinaryExpression().OperatorToken.Kind)
                {
                    case Kind.BarBarToken:
                    case Kind.BarBarEqualsToken:
                    case Kind.AmpersandAmpersandToken:
                    case Kind.AmpersandAmpersandEqualsToken:
                    {
                        return PredicateSemantics.Sometimes;
                    }

                    case Kind.CommaToken:
                    case Kind.EqualsToken:
                    {
                        return this.getSyntacticNullishnessSemantics(node.AsBinaryExpression().Right);
                    }

                    case Kind.QuestionQuestionToken:
                    case Kind.QuestionQuestionEqualsToken:
                    {
                        var leftSemantics = this.getSyntacticNullishnessSemantics(node.AsBinaryExpression().Left);
                        var result = (leftSemantics & PredicateSemantics.Never);
                        if (((leftSemantics & PredicateSemantics.Always) != 0))
                        {
                            result |= this.getSyntacticNullishnessSemantics(node.AsBinaryExpression().Right);
                        }

                        return result;
                    }
                }

                return PredicateSemantics.Never;
            }

            case Kind.ConditionalExpression:
            {
                return (this.getSyntacticNullishnessSemantics(node.AsConditionalExpression().WhenTrue) | this.getSyntacticNullishnessSemantics(node.AsConditionalExpression().WhenFalse));
            }

            case Kind.NullKeyword:
            {
                return PredicateSemantics.Always;
            }

            case Kind.Identifier:
            {
                if ((this.getResolvedSymbol(node) == this.undefinedSymbol))
                {
                    return PredicateSemantics.Always;
                }

                return PredicateSemantics.Sometimes;
            }
        }

        return PredicateSemantics.Never;
    }
}

public sealed partial class Checker
{
    private Type getBaseTypeOfLiteralTypeForComparison(Type t)
    {
        switch (true)
        {
            case true when ((t.flags & (((TypeFlags.StringLiteral | TypeFlags.TemplateLiteral) | TypeFlags.StringMapping))) != 0):
            {
                return this.stringType;
            }

            case true when ((t.flags & ((TypeFlags.NumberLiteral | TypeFlags.Enum))) != 0):
            {
                return this.numberType;
            }

            case true when ((t.flags & TypeFlags.BigIntLiteral) != 0):
            {
                return this.bigintType;
            }

            case true when ((t.flags & TypeFlags.BooleanLiteral) != 0):
            {
                return this.booleanType;
            }

            case true when ((t.flags & TypeFlags.Union) != 0):
            {
                return this.mapType(t, this.getBaseTypeOfLiteralTypeForComparison);
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type removeDefinitelyFalsyTypes(Type t)
    {
        return this.filterType(t, (t2) =>
        {
            return this.hasTypeFacts(t2, TypeFacts.Truthy);
        });
    }
}

public sealed partial class Checker
{
    private Type extractDefinitelyFalsyTypes(Type t)
    {
        return this.mapType(t, this.getDefinitelyFalsyPartOfType);
    }
}

public sealed partial class Checker
{
    private Func<Symbol> getGlobalValueSymbolResolver(string name, bool reportErrors)
    {
        return memoize(() =>
        {
            return this.getGlobalSymbol(name, SymbolFlags.Value, Core.IfElse(reportErrors, Diagnostics.Cannot_find_global_value_0, null));
        });
    }
}

public sealed partial class Checker
{
    private bool maybeTypeOfKindConsideringBaseConstraint(Type t, TypeFlags kind)
    {
        if (this.maybeTypeOfKind(t, kind))
        {
            return true;
        }

        var baseConstraint = this.getBaseConstraintOrType(t);
        return ((baseConstraint != null) && this.maybeTypeOfKind(baseConstraint, kind));
    }
}

public sealed partial class Checker
{
    private Type getDefinitelyFalsyPartOfType(Type t)
    {
        switch (true)
        {
            case true when ((t.flags & TypeFlags.String) != 0):
            {
                return this.emptyStringType;
            }

            case true when ((t.flags & TypeFlags.Number) != 0):
            {
                return this.zeroType;
            }

            case true when ((t.flags & TypeFlags.BigInt) != 0):
            {
                return this.zeroBigIntType;
            }

            case true when ((((((t == this.regularFalseType) || (t == this.falseType)) || ((t.flags & ((((TypeFlags.Void | TypeFlags.Undefined) | TypeFlags.Null) | TypeFlags.AnyOrUnknown))) != 0)) || (((t.flags & TypeFlags.StringLiteral) != 0) && (getStringLiteralValue(t) == ""))) || (((t.flags & TypeFlags.NumberLiteral) != 0) && (getNumberLiteralValue(t) == 0))) || (((t.flags & TypeFlags.BigIntLiteral) != 0) && isZeroBigInt(t))):
            {
                return t;
            }
        }

        return this.neverType;
    }
}

public sealed partial class Checker
{
    private Type numberOrBigIntType, typeofType, emptyStringType, zeroType, zeroBigIntType;
    private Func<Symbol> getGlobalNaNSymbolOrNil;
    // checker.go:327. Exact meaning bits for grammar object members.
    [Flags]
    private enum DeclarationMeaning
    {
        GetAccessor = 1,
        SetAccessor = 2,
        PropertyAssignment = 4,
        Method = 8,
        PrivateStatic = 16,
        GetOrSetAccessor = GetAccessor | SetAccessor,
        PropertyAssignmentOrMethod = PropertyAssignment | Method
    }
}

public sealed partial class Checker
{
    private static JsNum.Number getNumberLiteralValue(Type t)
    {
        return ((JsNum.Number)t.AsLiteralType().value);
    }
}

public sealed partial class Checker
{
    private static bool isZeroBigInt(Type t)
    {
        return (getBigIntLiteralValue(t) == new JsNum.PseudoBigInt());
    }
}

public sealed partial class Checker
{
    private bool isSelfTypeAccess(Node name, Symbol parent)
    {
        return ((name.Kind == Kind.ThisKeyword) || (((parent != null) && Ast.IsEntityNameExpression(name)) && (parent == this.getResolvedSymbol(Ast.GetFirstIdentifier(name)))));
    }
}

public sealed partial class Checker
{
    private bool isAssignmentToReadonlyEntity(Node expr, Symbol symbol, AssignmentKind assignmentKind)
    {
        if ((assignmentKind == AssignmentKind.None))
        {
            return false;
        }

        if (Ast.IsAccessExpression(expr))
        {
            var node = Ast.SkipParentheses(expr.Expression());
            if (Ast.IsIdentifier(node))
            {
                var expressionSymbol = this.getResolvedSymbol(node);
                if (((expressionSymbol.Flags & SymbolFlags.ModuleExports) != 0))
                {
                    return false;
                }
            }
        }

        if (this.isReadonlySymbol(symbol))
        {
            if (((((symbol.Flags & SymbolFlags.Property) != 0) && Ast.IsAccessExpression(expr)) && (expr.Expression().Kind == Kind.ThisKeyword)))
            {
                var ctor = this.getControlFlowContainer(expr);
                if (((ctor == null) || !Ast.IsConstructorDeclaration(ctor)))
                {
                    return true;
                }

                if ((symbol.ValueDeclaration != null))
                {
                    var isAssignmentDeclaration = Ast.IsBinaryExpression(symbol.ValueDeclaration);
                    var isLocalPropertyDeclaration = (ctor.Parent == symbol.ValueDeclaration.Parent);
                    var isLocalParameterProperty = (ctor == symbol.ValueDeclaration.Parent);
                    var isLocalThisPropertyAssignment = (isAssignmentDeclaration && (symbol.Parent.ValueDeclaration == ctor.Parent));
                    var isLocalThisPropertyAssignmentConstructorFunction = (isAssignmentDeclaration && (symbol.Parent.ValueDeclaration == ctor));
                    var isWriteableSymbol = (((isLocalPropertyDeclaration || isLocalParameterProperty) || isLocalThisPropertyAssignment) || isLocalThisPropertyAssignmentConstructorFunction);
                    return !isWriteableSymbol;
                }
            }

            return true;
        }

        if (Ast.IsAccessExpression(expr))
        {
            var node2 = Ast.SkipParentheses(expr.Expression());
            if (Ast.IsIdentifier(node2))
            {
                var expressionSymbol2 = this.getResolvedSymbol(node2);
                if (((expressionSymbol2.Flags & SymbolFlags.Alias) != 0))
                {
                    var declaration = this.getDeclarationOfAliasSymbol(expressionSymbol2);
                    return ((declaration != null) && Ast.IsNamespaceImport(declaration));
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isThisPropertyAccessInConstructor(Node node, Symbol prop)
    {
        Node constructor = default;
        {
            var tuple1 = this.isConstructorDeclaredThisProperty(prop);
            var kind = tuple1.Item1;
            var location = tuple1.Item2;
            if ((kind == ThisAssignmentDeclarationKind.Constructor))
            {
                constructor = location;
            }
            else
            {
                if ((isThisProperty(node) && this.isAutoTypedProperty(prop)))
                {
                    constructor = this.getDeclaringConstructor(prop);
                }
            }
        }

        return (Ast.GetThisContainer(node, true, false) == constructor);
    }
}

public sealed partial class Checker
{
    private bool isMethodAccessForCall(Node node)
    {
        while (Ast.IsParenthesizedExpression(node.Parent))
        {
            node = node.Parent;
        }

        return (Ast.IsCallOrNewExpression(node.Parent) && (node.Parent.Expression() == node));
    }
}

public sealed partial class Checker
{
    private bool checkAndReportErrorForExtendingInterface(Node errorLocation)
    {
        var expression = this.getEntityNameForExtendingInterface(errorLocation);
        if (((expression != null) && (this.resolveEntityName(expression, SymbolFlags.Interface, true, false, null) != null)))
        {
            this.error(errorLocation, Diagnostics.Cannot_extend_an_interface_0_Did_you_mean_implements, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(expression));
            return true;
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Node getEntityNameForExtendingInterface(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.QualifiedName:
            case Kind.PropertyAccessExpression:
            {
                if ((node.Parent != null))
                {
                    return this.getEntityNameForExtendingInterface(node.Parent);
                }

                break;
            }

            case Kind.TypeReference:
            {
                return node.AsTypeReferenceNode().TypeName;
            }

            case Kind.ExpressionWithTypeArguments:
            {
                if (Ast.IsEntityNameExpression(node.Expression()))
                {
                    return node.Expression();
                }

                break;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private void markPropertyAliasReferenced(Node location, Symbol propSymbol, Type parentType)
    {
        if (isPartOfImportEqualsModuleReference(location))
        {
            return;
        }

        Node left = default;
        if (Ast.IsPropertyAccessExpression(location))
        {
            left = location.Expression();
        }
        else
        {
            left = location.AsQualifiedName().Left;
        }

        if ((Ast.IsThisIdentifier(left) || !Ast.IsIdentifier(left)))
        {
            return;
        }

        var parentSymbol = this.getResolvedSymbol(left);
        if (((parentSymbol == null) || (parentSymbol == this.unknownSymbol)))
        {
            return;
        }

        if ((this.compilerOptions.GetIsolatedModules() || ((false /* fixed preserveConstEnums option */ && isExportOrExportExpression(location)))))
        {
            this.markAliasReferenced(parentSymbol, location);
            return;
        }

        var leftType = parentType;
        if ((leftType == null))
        {
            leftType = this.checkExpressionCached(left);
        }

        if ((IsTypeAny(leftType) || (leftType == this.silentNeverType)))
        {
            this.markAliasReferenced(parentSymbol, location);
            return;
        }

        var prop = propSymbol;
        if (((prop == null) && (parentType == null)))
        {
            Node right = default;
            if (Ast.IsPropertyAccessExpression(location))
            {
                right = location.AsPropertyAccessExpression().Name();
            }
            else
            {
                right = location.AsQualifiedName().Right;
            }

            Symbol lexicallyScopedSymbol = default;
            if (Ast.IsPrivateIdentifier(right))
            {
                throw unsupported("private class identifier lookup");
            }

            var assignmentKind = getAssignmentTargetKind(location);
            Type apparentType = default;
            if (((assignmentKind != AssignmentKind.None) || this.isMethodAccessForCall(location)))
            {
                apparentType = this.getApparentType(this.getWidenedType(leftType));
            }
            else
            {
                apparentType = this.getApparentType(leftType);
            }

            if (Ast.IsPrivateIdentifier(right))
            {
                if ((lexicallyScopedSymbol != null))
                {
                    throw unsupported("private class property type");
                }
            }
            else
            {
                prop = this.getPropertyOfType(apparentType, right.Text());
            }
        }

        if (!(((prop != null) && ((isConstEnumOrConstEnumOnlyModule(prop) || (((prop.Flags & SymbolFlags.EnumMember) != 0) && (location.Parent.Kind == Kind.EnumMember)))))))
        {
            this.markAliasReferenced(parentSymbol, location);
        }
    }
}

public sealed partial class Checker
{
    private Evaluator.Result evaluateEntity(Node expr, Node location)
    {
        switch (expr.Kind)
        {
            case Kind.Identifier:
            case Kind.PropertyAccessExpression:
            {
                var symbol = this.resolveEntityName(expr, SymbolFlags.Value, true, false, null);
                if ((symbol == null))
                {
                    return Evaluator.NewResult(null, false, false, false);
                }

                if ((expr.Kind == Kind.Identifier))
                {
                    if ((Ast.IsInfinityOrNaNString(expr.Text()) && ((symbol == this.getGlobalSymbol(expr.Text(), SymbolFlags.Value, null)))))
                    {
                        return Evaluator.NewResult(JsNum.FromString(expr.Text()), false, false, false);
                    }
                }

                if (((symbol.Flags & SymbolFlags.EnumMember) != 0))
                {
                    throw unsupported("constant evaluator: enum");
                }

                if (this.isConstantVariable(symbol))
                {
                    var declaration = symbol.ValueDeclaration;
                    if ((((((declaration != null) && Ast.IsVariableDeclaration(declaration)) && (declaration.Type() == null)) && (declaration.Initializer() != null)) && (((location == null) || ((declaration != location) && this.isBlockScopedNameDeclaredBeforeUse(declaration, location))))))
                    {
                        var result = this.evaluate(declaration.Initializer(), declaration);
                        if (((location != null) && (Ast.GetSourceFileOfNode(location) != Ast.GetSourceFileOfNode(declaration))))
                        {
                            return Evaluator.NewResult(result.Value, false, true, true);
                        }

                        return Evaluator.NewResult(result.Value, result.IsSyntacticallyString, result.ResolvedOtherFiles, true);
                    }
                }

                return Evaluator.NewResult(null, false, false, false);
            }

            case Kind.ElementAccessExpression:
            {
                throw unsupported("constant evaluator: element access");
            }
        }

        throw new InvalidOperationException("Unhandled case in evaluateEntity");
    }
}

public sealed partial class Checker
{
    private Evaluator.EvaluatorDelegate evaluate;
}

public sealed partial class Checker
{
    private (ThisAssignmentDeclarationKind, Node) isConstructorDeclaredThisProperty(Symbol symbol)
    {
        if (((symbol.ValueDeclaration == null) || !Ast.IsBinaryExpression(symbol.ValueDeclaration)))
        {
            return (ThisAssignmentDeclarationKind.None, null);
        }

        {
            var ok = this.thisExpandoKinds.TryGetValue(symbol, out var kind);
            if (ok)
            {
                var ok2 = this.thisExpandoLocations.TryGetValue(symbol, out var location);
                if (!ok2)
                {
                    throw new InvalidOperationException("location should be cached whenever this expando symbol is cached");
                }

                return (kind, location);
            }
        }

        var allThis = true;
        Node typeAnnotation = default;
        {
            foreach (var declaration in GoRuntime.RangeValues(symbol.Declarations))
            {
                if (!Ast.IsBinaryExpression(declaration))
                {
                    allThis = false;
                    break;
                }

                var bin = declaration.AsBinaryExpression();
                if (((Ast.GetAssignmentDeclarationKind(declaration) == JSDeclarationKind.ThisProperty) && (((bin.Left.Kind != Kind.ElementAccessExpression) || Ast.IsStringOrNumericLiteralLike(bin.Left.AsElementAccessExpression().ArgumentExpression)))))
                {
                    if ((bin.Type != null))
                    {
                        typeAnnotation = bin.Type;
                    }
                }
                else
                {
                    allThis = false;
                    break;
                }
            }
        }

        Node location2 = default;
        var kind2 = ThisAssignmentDeclarationKind.None;
        if (allThis)
        {
            if ((typeAnnotation != null))
            {
                location2 = typeAnnotation;
                kind2 = ThisAssignmentDeclarationKind.Typed;
            }
            else
            {
                location2 = this.getDeclaringConstructor(symbol);
                kind2 = Core.IfElse((location2 == null), ThisAssignmentDeclarationKind.Method, ThisAssignmentDeclarationKind.Constructor);
            }
        }

        this.thisExpandoKinds[symbol] = kind2;
        this.thisExpandoLocations[symbol] = location2;
        return (kind2, location2);
    }
}

public sealed partial class Checker
{
    private Node getDeclaringConstructor(Symbol symbol)
    {
        {
            foreach (var declaration in GoRuntime.RangeValues(symbol.Declarations))
            {
                var container = Ast.GetThisContainer(declaration, false, false);
                if (((container != null) && Ast.IsConstructorDeclaration(container)))
                {
                    return container;
                }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private enum ThisAssignmentDeclarationKind
    {
        None,
        Typed,
        Constructor,
        Method
    }

    private readonly Dictionary<Symbol, ThisAssignmentDeclarationKind> thisExpandoKinds = new();
    private readonly Dictionary<Symbol, Node> thisExpandoLocations = new();
}

public sealed partial class Checker
{
    private Type getBigIntLiteralType(JsNum.PseudoBigInt value)
    {
        var t = this.bigintLiteralTypes.GetValueOrDefault(value);
        if ((t == null))
        {
            t = this.newLiteralType(TypeFlags.BigIntLiteral, value, null);
            this.bigintLiteralTypes[value] = t;
        }

        return t;
    }
}

public sealed partial class Checker
{
    internal static JsNum.PseudoBigInt getBigIntLiteralValue(Type t)
    {
        return ((JsNum.PseudoBigInt)t.AsLiteralType().value);
    }
}

public sealed partial class Checker
{
    private static bool isConstEnumObjectType(Type t)
    {
        return ((((t.objectFlags & ObjectFlags.Anonymous) != 0) && (t.symbol != null)) && isConstEnumSymbol(t.symbol));
    }
}

public sealed partial class Checker
{
    private static bool isExportOrExportExpression(Node location)
    {
        return (Ast.FindAncestor(location, (n) =>
        {
            var parent = n.Parent;
            if ((parent != null))
            {
                if (Ast.IsAnyExportAssignment(parent))
                {
                    return ((parent.Expression() == n) && Ast.IsEntityNameExpression(n));
                }

                if (Ast.IsExportSpecifier(parent))
                {
                    return ((parent.AsExportSpecifier().Name() == n) || (parent.PropertyName() == n));
                }
            }

            return false;
        }) != null);
    }
}

public sealed partial class Checker
{
    private static bool isPartOfImportEqualsModuleReference(Node location)
    {
        var importEquals = Ast.FindAncestorKind(location, Kind.ImportEqualsDeclaration);
        if ((importEquals == null))
        {
            return false;
        }

        {
            var node = location;
            for (; ((node != null) && (node != importEquals)); node = node.Parent)
            {
                if ((node == importEquals.AsImportEqualsDeclaration().ModuleReference))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private readonly Dictionary<JsNum.PseudoBigInt, Type> bigintLiteralTypes = new();
    private static bool isConstEnumSymbol(Symbol s) => (s.Flags & SymbolFlags.ConstEnum) != 0;
}

public sealed partial class Checker
{
    private bool isUsedInFunctionOrInstanceProperty(Node usage, Node declaration, Node declContainer)
    {
        return (Ast.FindAncestorOrQuit(usage, (current) =>
        {
            if ((current == declContainer))
            {
                return FindAncestorResult.Quit;
            }

            if (Ast.IsFunctionLike(current))
            {
                return Ast.ToFindAncestorResult((Ast.GetImmediatelyInvokedFunctionExpression(current) == null));
            }

            if (Ast.IsClassStaticBlockDeclaration(current))
            {
                return Ast.ToFindAncestorResult((declaration.Pos() < usage.Pos()));
            }

            if (((current.Parent != null) && Ast.IsPropertyDeclaration(current.Parent)))
            {
                throw unsupported("class property use order");
            }

            if (((current.Parent != null) && Ast.IsDecorator(current.Parent)))
            {
                throw unsupported("decorator reference order");
            }

            return FindAncestorResult.False;
        }) != null);
    }
}

public sealed partial class Checker
{
    private Type getWidenedTypeForAssignmentDeclaration(Symbol symbol)
    {
        Type t = default;
        var tuple1 = this.isConstructorDeclaredThisProperty(symbol);
        var kind = tuple1.Item1;
        var location = tuple1.Item2;
        switch (kind)
        {
            case ThisAssignmentDeclarationKind.Typed:
            {
                if ((location == null))
                {
                    throw new InvalidOperationException("location should not be nil when this assignment has a type.");
                }

                t = this.getTypeFromTypeNode(location);
                break;
            }

            case ThisAssignmentDeclarationKind.Constructor:
            {
                throw unsupported("class constructor property flow");
            }

            case ThisAssignmentDeclarationKind.Method:
            {
                t = this.getTypeOfPropertyInBaseClass(symbol);
                break;
            }
        }

        if ((t == null))
        {
            List<Type> types = default;
            {
                foreach (var range2 in GoRuntime.Range(symbol.Declarations))
                {
                    var i = range2.Key;
                    var declaration = range2.Value;
                    if ((Ast.IsBinaryExpression(declaration) && (declaration.Type() != null)))
                    {
                        t = this.getTypeFromTypeNode(declaration.Type());
                        break;
                    }

                    {
                        var assignedType = this.getAssignmentDeclarationInitializerType(declaration);
                        if ((assignedType != null))
                        {
                            if (((((Ast.GetAssignmentDeclarationKind(declaration) != JSDeclarationKind.ExportsProperty) || (i != 0)) || (GoRuntime.Len(symbol.Declarations) == 1)) || ((assignedType.flags & TypeFlags.Undefined) == 0)))
                            {
                                types = Core.AppendIfUnique(types, assignedType);
                            }
                        }
                    }
                }
            }

            if (((kind == ThisAssignmentDeclarationKind.Method) && (GoRuntime.Len(types) > 0)))
            {
                if (this.strictNullChecks)
                {
                    types = Core.AppendIfUnique(types, this.undefinedOrMissingType);
                }
            }

            if ((t == null))
            {
                t = this.anyType;
                if ((GoRuntime.Len(types) != 0))
                {
                    t = this.getUnionType(types);
                }
            }
        }

        t = this.getWidenedType(t);
        if ((((symbol.ValueDeclaration != null) && Ast.IsInJSFile(symbol.ValueDeclaration)) && (this.filterType(t, (c) =>
        {
            return ((c.Flags() & ~TypeFlags.Nullable) != 0);
        }) == this.neverType)))
        {
            this.reportImplicitAny(symbol.ValueDeclaration, this.anyType, WideningKind.Normal);
            return this.anyType;
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type getGlobalStrictFunctionType(string name)
    {
        if (this.strictBindCallApply)
        {
            return this.getGlobalType(name, 0, true);
        }

        return this.globalFunctionType;
    }
}

public sealed partial class Checker
{
    private Type getAssignmentDeclarationInitializerType(Node node)
    {
        if (Ast.IsBinaryExpression(node))
        {
            Type t = default;
            switch (Ast.GetAssignmentDeclarationKind(node))
            {
                case JSDeclarationKind.ModuleExports:
                case JSDeclarationKind.ExportsProperty:
                {
                    t = this.getRegularTypeOfLiteralType(this.checkExpressionCached(Ast.GetRightMostAssignedExpression(node)));
                    break;
                }

                case JSDeclarationKind.ThisProperty:
                {
                    if (this.containsSameNamedThisProperty(node.AsBinaryExpression().Left, node.AsBinaryExpression().Right))
                    {
                        return null;
                    }

                    goto default;
                }

                default:
                {
                    t = this.checkExpressionForMutableLocation(node.AsBinaryExpression().Right, CheckMode.Normal);
                    break;
                }
            }

            if ((isArrayOrTupleType(t)))
            {
                throw unsupported("empty array assignment declaration");
            }

            return t;
        }

        if (Ast.IsCallExpression(node))
        {
            return this.getTypeFromPropertyDescriptor(node.Arguments()[2]);
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool containsSameNamedThisProperty(Node thisProperty, Node expression)
    {
        Func<Node, bool> visit = default;
        visit = (node2) =>
        {
            if (this.isMatchingReference(thisProperty, node2))
            {
                return true;
            }

            if (Ast.IsFunctionLike(node2))
            {
                return false;
            }

            return node2.ForEachChild(n => visit(n));
        };
        return visit(expression);
    }
}

public sealed partial class Checker
{
    private Type getTypeFromPropertyDescriptor(Node node)
    {
        var objectLiteralType = this.checkExpressionCached(node);
        {
            var valueType = this.getTypeOfPropertyOfType(objectLiteralType, "value");
            if ((valueType != null))
            {
                return valueType;
            }
        }

        {
            var getFunc = this.getTypeOfPropertyOfType(objectLiteralType, "get");
            if ((getFunc != null))
            {
                {
                    var getSig = this.getSingleCallSignature(getFunc);
                    if ((getSig != null))
                    {
                        return this.getReturnTypeOfSignature(getSig);
                    }
                }
            }
        }

        {
            var setFunc = this.getTypeOfPropertyOfType(objectLiteralType, "set");
            if ((setFunc != null))
            {
                {
                    var setSig = this.getSingleCallSignature(setFunc);
                    if ((setSig != null))
                    {
                        return this.getTypeOfFirstParameterOfSignature(setSig);
                    }
                }
            }
        }

        return this.anyType;
    }
}

public sealed partial class Checker
{
    private Type globalCallableFunctionType, globalNewableFunctionType;
    private readonly bool strictBindCallApply = false;
}

public sealed partial class Checker
{
    private void checkThisInStaticClassFieldInitializerInDecoratedClass(Node thisExpression, Node container)
    {
        if (((Ast.IsPropertyDeclaration(container) && Ast.HasStaticModifier(container)) && this.legacyDecorators))
        {
            throw unsupported("checkThisInStaticClassFieldInitializerInDecoratedClass: decorated class");
        }
    }
}

public sealed partial class Checker
{
    private bool isVarConstLike(Node node)
    {
        var blockScopeKind = (this.getCombinedNodeFlagsCached(node) & NodeFlags.BlockScoped);
        return (((blockScopeKind == NodeFlags.Const) || (blockScopeKind == NodeFlags.Using)) || (blockScopeKind == NodeFlags.AwaitUsing));
    }
}

public sealed partial class Checker
{
    private bool checkSourceElementUnreachable(Node node)
    {
        if (!Ast.IsPotentiallyExecutableNode(node))
        {
            return false;
        }

        if (this.reportedUnreachableNodes.Contains(node))
        {
            return true;
        }

        if (!this.isSourceElementUnreachable(node))
        {
            return false;
        }

        this.reportedUnreachableNodes.Add(node);
        var sourceFile = Ast.GetSourceFileOfNode(node);
        var startNode = node;
        var endNode = node;
        var parent = node.Parent;
        if (parent.CanHaveStatements())
        {
            var statements = parent.Statements();
            {
                var offset = statements?.IndexOf(node) ?? -1;
                if ((offset >= 0))
                {
                    var first = offset;
                    var last = offset;
                    {
                        var i = (offset + 1);
                        for (; (i < GoRuntime.Len(statements)); i++)
                        {
                            var nextNode = statements[i];
                            if ((!Ast.IsPotentiallyExecutableNode(nextNode) || !this.isSourceElementUnreachable(nextNode)))
                            {
                                break;
                            }

                            last = i;
                            this.reportedUnreachableNodes.Add(nextNode);
                        }
                    }

                    startNode = statements[first];
                    endNode = statements[last];
                }
            }
        }

        var start = TypeScript.Scanner.Scanner.GetTokenPosOfNode(startNode, sourceFile, false);
        var diagnostic = Ast.NewDiagnostic(sourceFile, new TextRange(start, endNode.End()), Diagnostics.Unreachable_code_detected);
        this.addErrorOrSuggestion((this.compilerOptions.AllowUnreachableCode == Tristate.False), diagnostic);
        return true;
    }
}

public sealed partial class Checker
{
    private bool isSourceElementUnreachable(Node node)
    {
        if (((node.Flags & NodeFlags.Unreachable) != 0))
        {
            switch (node.Kind)
            {
                case Kind.EnumDeclaration:
                {
                    throw unsupported("isSourceElementUnreachable: enum source");
                }

                case Kind.ModuleDeclaration:
                {
                    throw unsupported("isSourceElementUnreachable: module source");
                }

                default:
                {
                    return true;
                }
            }
        }
        else
        {
            {
                var flowNode = node.FlowNodeData().FlowNode;
                if ((flowNode != null))
                {
                    return !this.isReachableFlowNode(flowNode);
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    // checker.go:903 / reportedUnreachableNodes original identity set.
    private bool withinUnreachableCode;
    private readonly HashSet<Node> reportedUnreachableNodes = new(ReferenceEqualityComparer.Instance);
}

public sealed partial class Checker
{
    // Authored public-boundary ownership only. These sets do not participate in
    // upstream checking algorithms, caches or semantic graph serialization.
    private readonly HashSet<Symbol> authoredOwnedSymbols = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Signature> authoredOwnedSignatures = new(ReferenceEqualityComparer.Instance);
}

public sealed partial class Checker
{
    private bool isSymbolOrSymbolForCall(Node node)
    {
        if (!Ast.IsCallExpression(node))
        {
            return false;
        }

        var left = node.Expression();
        if ((Ast.IsPropertyAccessExpression(left) && (left.Name().Text() == "for")))
        {
            left = left.Expression();
        }

        if ((!Ast.IsIdentifier(left) || (left.Text() != "Symbol")))
        {
            return false;
        }

        var globalESSymbol = this.getGlobalESSymbolConstructorSymbolOrNil();
        if ((globalESSymbol == null))
        {
            return false;
        }

        return (globalESSymbol == this.nameResolver.Resolve(left, "Symbol", SymbolFlags.Value, null, false, false));
    }
}

public sealed partial class Checker
{
    private static bool isThislessVariableLikeDeclaration(Node node)
    {
        var typeNode = node.Type();
        if ((typeNode != null))
        {
            return isThislessType(typeNode);
        }

        return (node.Initializer() == null);
    }
}

public sealed partial class Checker
{
    private static bool isThislessType(Node node)
    {
        switch (node.Kind)
        {
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.StringKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.BooleanKeyword:
            case Kind.SymbolKeyword:
            case Kind.ObjectKeyword:
            case Kind.VoidKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NeverKeyword:
            case Kind.LiteralType:
            {
                return true;
            }

            case Kind.ArrayType:
            {
                return isThislessType(node.AsArrayTypeNode().ElementType);
            }

            case Kind.TypeReference:
            {
                return Core.Every(node.TypeArguments(), isThislessType);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private static bool isThislessFunctionLikeDeclaration(Node node)
    {
        var returnType = node.Type();
        return ((((Ast.IsConstructorDeclaration(node) || ((returnType != null) && isThislessType(returnType)))) && Core.Every(node.Parameters(), isThislessVariableLikeDeclaration)) && Core.Every(node.TypeParameters(), isThislessTypeParameter));
    }
}

public sealed partial class Checker
{
    private static bool isThislessTypeParameter(Node node)
    {
        var constraint = node.AsTypeParameterDeclaration().Constraint;
        return ((constraint == null) || isThislessType(constraint));
    }
}

public sealed partial class Checker
{
    private Type instantiateAnonymousType(Type t, TypeMapper m, TypeAlias alias)
    {
        var result = this.newObjectType(((t.objectFlags & ~((ObjectFlags.CouldContainTypeVariablesComputed | ObjectFlags.CouldContainTypeVariables))) | ObjectFlags.Instantiated), t.symbol);
        switch (true)
        {
            case true when ((t.objectFlags & ObjectFlags.Mapped) != 0):
            {
                throw unsupported("instantiateAnonymousType: mapped type");
            }

            case true when ((t.objectFlags & ObjectFlags.InstantiationExpressionType) != 0):
            {
                throw unsupported("instantiateAnonymousType: source instantiation expression");
            }
        }

        if ((alias == null))
        {
            alias = this.instantiateTypeAlias(t.alias, m);
        }

        result.alias = alias;
        if (((alias != null) && (GoRuntime.Len(alias.typeArguments) != 0)))
        {
            result.objectFlags |= this.getPropagatingFlagsOfTypes(result.alias.typeArguments, TypeFlags.None);
        }

        var d = result.AsObjectType();
        d.target = t;
        d.mapper = m;
        return result;
    }
}

public sealed partial class Checker
{
    private List<Type> getOuterTypeParameters(Node node, bool includeThisTypes)
    {
        while (true)
        {
            node = node.Parent;
            if ((node == null))
            {
                return null;
            }

            var kind = node.Kind;
            switch (kind)
            {
                case Kind.ClassDeclaration:
                case Kind.ClassExpression:
                case Kind.InterfaceDeclaration:
                case Kind.CallSignature:
                case Kind.ConstructSignature:
                case Kind.MethodSignature:
                case Kind.FunctionType:
                case Kind.ConstructorType:
                case Kind.FunctionDeclaration:
                case Kind.MethodDeclaration:
                case Kind.FunctionExpression:
                case Kind.ArrowFunction:
                case Kind.TypeAliasDeclaration:
                case Kind.JSTypeAliasDeclaration:
                case Kind.MappedType:
                case Kind.ConditionalType:
                {
                    var outerTypeParameters = this.getOuterTypeParameters(node, includeThisTypes);
                    if ((((((kind == Kind.FunctionExpression) || (kind == Kind.ArrowFunction)) || Ast.IsObjectLiteralMethod(node))) && this.isContextSensitive(node)))
                    {
                        var signature = Core.FirstOrNil(this.getSignaturesOfType(this.getTypeOfSymbol(this.getSymbolOfDeclaration(node)), SignatureKind.Call));
                        if (((signature != null) && (GoRuntime.Len(signature.typeParameters) != 0)))
                        {
                            throw unsupported("getOuterTypeParameters: contextual generic signature");
                        }
                    }

                    if ((kind == Kind.MappedType))
                    {
                        throw unsupported("getOuterTypeParameters: mapped type");
                    }

                    if ((kind == Kind.ConditionalType))
                    {
                        throw unsupported("getOuterTypeParameters: conditional inference");
                    }

                    var outerAndOwnTypeParameters = this.appendTypeParameters(outerTypeParameters, node.TypeParameters());
                    Type thisType = default;
                    if ((includeThisTypes && ((((kind == Kind.ClassDeclaration) || (kind == Kind.ClassExpression)) || (kind == Kind.InterfaceDeclaration)))))
                    {
                        thisType = this.getDeclaredTypeOfClassOrInterface(this.getSymbolOfDeclaration(node)).AsInterfaceType().thisType;
                    }

                    if ((thisType != null))
                    {
                        return GoRuntime.Append(outerAndOwnTypeParameters, thisType);
                    }

                    return outerAndOwnTypeParameters;
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private Type createDeferredTypeReference(Type target, Node node, TypeMapper mapper, TypeAlias alias)
    {
        if ((alias == null))
        {
            alias = this.getAliasForTypeNode(node);
            if (((alias != null) && (mapper != null)))
            {
                alias.typeArguments = this.instantiateTypes(alias.typeArguments, mapper);
            }
        }

        var t = this.newObjectType(ObjectFlags.Reference, target.symbol);
        t.alias = alias;
        var d = t.AsTypeReference();
        d.target = target;
        d.mapper = mapper;
        d.node = node;
        return t;
    }
}

public sealed partial class Checker
{
    // checker.go:633,951; the original symbol-identity unique type cache.
    private readonly Dictionary<Symbol, Type> uniqueESSymbolTypes = new(ReferenceEqualityComparer.Instance);
    // checker.go:23470. The name is a raw-byte symbolic spelling, including the
    // full original unsigned symbol ID. Observers canonicalize that identity;
    // checking algorithms retain it unchanged.
    private Type getESSymbolLikeTypeForNode(Node node)
    {
        if (isValidESSymbolDeclaration(node))
        {
            var symbol = getSymbolOfNode(node);
            if (symbol != null)
            {
                if (!uniqueESSymbolTypes.TryGetValue(symbol, out var uniqueType))
                {
                    var name = Ast.InternalSymbolNamePrefix + "@" + symbol.Name + "@" + Ast.GetSymbolId(symbol).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    uniqueType = newUniqueESSymbolType(symbol, name);
                    uniqueESSymbolTypes[symbol] = uniqueType;
                }

                return uniqueType;
            }
        }

        return esSymbolType;
    }

    // checker.go:25589.
    private Type newUniqueESSymbolType(Symbol symbol, string name)
    {
        var data = new UniqueESSymbolType
        {
            name = name
        };
        var t = newType(TypeFlags.UniqueESSymbol, ObjectFlags.None, data);
        t.symbol = symbol;
        return t;
    }

    // checker.go:26039.
    private Type getWidenedUniqueESSymbolType(Type t)
    {
        if ((t.flags & TypeFlags.UniqueESSymbol) != 0)
            return esSymbolType;
        if ((t.flags & TypeFlags.Union) != 0)
            return mapType(t, getWidenedUniqueESSymbolType);
        return t;
    }

    // checker.go:20206. Only the original index member's declarations enter
    // duplicate checking; merged value/function declarations are not members.
    private Symbol getIndexSymbol(Symbol symbol) => getMembersOfSymbol(symbol)?[Ast.InternalSymbolNameIndex];
}
