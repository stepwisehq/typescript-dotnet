// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/binder/nameresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
namespace TypeScript.Scanner;
public partial class NameResolver
{
    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:25:1.
    public Symbol Resolve(Node location, string name, SymbolFlags meaning, Message nameNotFoundMessage, bool isUse, bool excludeGlobals) =>
        Resolve(location, ByteStringValue.FromString(name), meaning, nameNotFoundMessage, isUse, excludeGlobals);

    // nameresolver.go:25. Internal names retain their Go string header through
    // lookup; only diagnostic and public callback boundaries project a string.
    internal Symbol Resolve(Node location, ByteStringValue name, SymbolFlags meaning, Message nameNotFoundMessage, bool isUse, bool excludeGlobals)
    {
        Symbol result = default;
        Node lastLocation = default;
        Node lastSelfReferenceLocation = default;
        Node propertyWithInvalidInitializer = default;
        Node associatedDeclarationForContainingInitializerOrBindingName = default;
        bool withinDeferredContext = default;
        Node grandparent = default;
        var originalLocation = location;
        var nameIsConst = (name.EqualsBytes("const"u8));
        while ((location != null))
        {
            if ((nameIsConst && Ast.IsConstAssertion(location)))
            {
                return null;
            }

            if (((Ast.IsModuleOrEnumDeclaration(location) && (lastLocation != null)) && (location.Name() == lastLocation)))
            {
                lastLocation = location;
                location = location.Parent;
            }

            var isModuleAttributes = ((Ast.IsModuleDeclaration(location) && (location.AsModuleDeclaration().Attributes != null)) && (lastLocation == location.AsModuleDeclaration().Attributes));
            var locals = location.Locals();
            if (((locals != null) && !Ast.IsGlobalSourceFile(location)))
            {
                result = this.lookup(locals, name, meaning);
                if ((result != null))
                {
                    var useResult = true;
                    if (isModuleAttributes)
                    {
                        useResult = false;
                    }
                    else
                    {
                        if (((Ast.IsFunctionLike(location) && (lastLocation != null)) && (lastLocation != location.Body())))
                        {
                            if (((((meaning & result.Flags) & SymbolFlags.Type) != 0) && (lastLocation.Kind != Kind.JSDoc)))
                            {
                                useResult = (((result.Flags & SymbolFlags.TypeParameter) != 0) && ((((((((lastLocation.Flags & NodeFlags.Synthesized) != 0) || (lastLocation == location.Type())) || (lastLocation.Kind == Kind.Parameter)) || (lastLocation.Kind == Kind.JSDocParameterTag)) || (lastLocation.Kind == Kind.JSDocReturnTag)) || (lastLocation.Kind == Kind.TypeParameter))));
                            }

                            if ((((meaning & result.Flags) & SymbolFlags.Variable) != 0))
                            {
                                if (this.useOuterVariableScopeInParameter(result, location, lastLocation))
                                {
                                    useResult = false;
                                }
                                else
                                {
                                    if (((result.Flags & SymbolFlags.FunctionScopedVariable) != 0))
                                    {
                                        useResult = (((lastLocation.Kind == Kind.Parameter) || ((lastLocation.Flags & NodeFlags.Synthesized) != 0)) || ((lastLocation == location.Type()) && (Ast.FindAncestor(result.ValueDeclaration, Ast.IsParameterDeclaration) != null)));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if ((location.Kind == Kind.ConditionalType))
                            {
                                useResult = (lastLocation == location.AsConditionalTypeNode().TrueType);
                            }
                        }
                    }

                    if (useResult)
                    {
                        goto loopEnd;
                    }

                    result = null;
                }
            }

            withinDeferredContext = (withinDeferredContext || getIsDeferredContext(location, lastLocation));
            switch (location.Kind)
            {
                case Kind.SourceFile:
                {
                    if (!Ast.IsExternalOrCommonJSModule(location.AsSourceFile()))
                    {
                        break;
                    }

                    goto case Kind.ModuleDeclaration;
                }

                case Kind.ModuleDeclaration:
                {
                    if (isModuleAttributes)
                    {
                        break;
                    }

                    var moduleSymbol = this.getSymbolOfDeclaration(location);
                    if ((moduleSymbol == null))
                    {
                        break;
                    }

                    var moduleExports = moduleSymbol.Exports;
                    if ((Ast.IsSourceFile(location) || (((Ast.IsModuleDeclaration(location) && ((location.Flags & NodeFlags.Ambient) != 0)) && !Ast.IsGlobalScopeAugmentation(location)))))
                    {
                        result = moduleExports?[Ast.InternalSymbolNameDefault];
                        if ((result != null))
                        {
                            var localSymbol = GetLocalSymbolForExportDefault(result);
                            if ((((localSymbol != null) && ((result.Flags & meaning) != 0)) && (name.EqualsValue(localSymbol.NameValue))))
                            {
                                goto loopEnd;
                            }

                            result = null;
                        }

                        var moduleExport = moduleExports?[name];
                        if ((((moduleExport != null) && (moduleExport.Flags == SymbolFlags.Alias)) && (((Ast.GetDeclarationOfKind(moduleExport, Kind.ExportSpecifier) != null) || (Ast.GetDeclarationOfKind(moduleExport, Kind.NamespaceExport) != null)))))
                        {
                            break;
                        }
                    }

                    if ((!name.EqualsString(Ast.InternalSymbolNameDefault)))
                    {
                        {
                            result = this.lookup(moduleExports, name, (meaning & SymbolFlags.ModuleMember));
                            if ((result != null))
                            {
                                if (((Ast.IsSourceFile(location) && (location.AsSourceFile().CommonJSModuleIndicator != null)) && ((result.Flags & SymbolFlags.Type) == 0)))
                                {
                                    result = null;
                                }
                                else
                                {
                                    goto loopEnd;
                                }
                            }
                        }
                    }

                    break;
                }

                case Kind.EnumDeclaration:
                {
                    var enumSymbol = this.getSymbolOfDeclaration(location);
                    if ((enumSymbol == null))
                    {
                        break;
                    }

                    result = this.lookup(enumSymbol.Exports, name, (meaning & SymbolFlags.EnumMember));
                    if ((result != null))
                    {
                        if (((((nameNotFoundMessage != null) && this.CompilerOptions.GetIsolatedModules()) && ((location.Flags & NodeFlags.Ambient) == 0)) && (Ast.GetSourceFileOfNode(location) != Ast.GetSourceFileOfNode(result.ValueDeclaration))))
                        {
                            var isolatedModulesLikeFlagName = Core.IfElse((this.CompilerOptions.VerbatimModuleSyntax == Tristate.True), "verbatimModuleSyntax", "isolatedModules");
                            this.error(originalLocation, Diagnostics.Cannot_access_0_from_another_file_without_qualification_when_1_is_enabled_Use_2_instead, name.Materialize(), isolatedModulesLikeFlagName, ((enumSymbol.Name + ".") + name.Materialize()));
                        }

                        goto loopEnd;
                    }

                    break;
                }

                case Kind.PropertyDeclaration:
                {
                    if (!Ast.IsStatic(location))
                    {
                        var ctor = Ast.FindConstructorDeclaration(location.Parent);
                        if (((ctor != null) && (ctor.Locals() != null)))
                        {
                            if ((this.lookup(ctor.Locals(), name, (meaning & SymbolFlags.Value)) != null))
                            {
                                propertyWithInvalidInitializer = location;
                            }
                        }
                    }

                    break;
                }

                case Kind.ClassDeclaration:
                case Kind.ClassExpression:
                case Kind.InterfaceDeclaration:
                {
                    result = this.lookup(this.getSymbolOfDeclaration(location).Members, name, (meaning & SymbolFlags.Type));
                    if ((result != null))
                    {
                        if (!isTypeParameterSymbolDeclaredInContainer(result, location))
                        {
                            result = null;
                            break;
                        }

                        if (((lastLocation != null) && Ast.IsStatic(lastLocation)))
                        {
                            if ((nameNotFoundMessage != null))
                            {
                                this.error(originalLocation, Diagnostics.Static_members_cannot_reference_class_type_parameters);
                            }

                            return null;
                        }

                        goto loopEnd;
                    }

                    if ((Ast.IsClassExpression(location) && ((meaning & SymbolFlags.Class) != 0)))
                    {
                        var className = location.Name();
                        if (((className != null) && (name.EqualsValue(className.TextValue()))))
                        {
                            result = location.Symbol();
                            goto loopEnd;
                        }
                    }

                    break;
                }

                case Kind.ExpressionWithTypeArguments:
                {
                    if ((((lastLocation == location.Expression()) && Ast.IsHeritageClause(location.Parent)) && (location.Parent.AsHeritageClause().Token == Kind.ExtendsKeyword)))
                    {
                        var container = location.Parent.Parent;
                        if (Ast.IsClassLike(container))
                        {
                            result = this.lookup(this.getSymbolOfDeclaration(container).Members, name, (meaning & SymbolFlags.Type));
                            if ((result != null))
                            {
                                if ((nameNotFoundMessage != null))
                                {
                                    this.error(originalLocation, Diagnostics.Base_class_expressions_cannot_reference_class_type_parameters);
                                }

                                return null;
                            }
                        }
                    }

                    break;
                }

                case Kind.ComputedPropertyName:
                {
                    grandparent = location.Parent.Parent;
                    if ((Ast.IsClassLike(grandparent) || Ast.IsInterfaceDeclaration(grandparent)))
                    {
                        result = this.lookup(this.getSymbolOfDeclaration(grandparent).Members, name, (meaning & SymbolFlags.Type));
                        if ((result != null))
                        {
                            if ((nameNotFoundMessage != null))
                            {
                                this.error(originalLocation, Diagnostics.A_computed_property_name_cannot_reference_a_type_parameter_from_its_containing_type);
                            }

                            return null;
                        }
                    }

                    break;
                }

                case Kind.MethodDeclaration:
                case Kind.Constructor:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.FunctionDeclaration:
                {
                    if ((((meaning & SymbolFlags.Variable) != 0) && (name.EqualsBytes("arguments"u8))))
                    {
                        result = this.argumentsSymbol();
                        goto loopEnd;
                    }

                    break;
                }

                case Kind.FunctionExpression:
                {
                    if ((((meaning & SymbolFlags.Variable) != 0) && (name.EqualsBytes("arguments"u8))))
                    {
                        result = this.argumentsSymbol();
                        goto loopEnd;
                    }

                    if (((meaning & SymbolFlags.Function) != 0))
                    {
                        var functionName = location.AsFunctionExpression().Name();
                        if (((functionName != null) && (name.EqualsValue(functionName.TextValue()))))
                        {
                            result = location.Symbol();
                            goto loopEnd;
                        }
                    }

                    break;
                }

                case Kind.Decorator:
                {
                    if (((location.Parent != null) && (location.Parent.Kind == Kind.Parameter)))
                    {
                        location = location.Parent;
                    }

                    if (((location.Parent != null) && ((Ast.IsClassElement(location.Parent) || (location.Parent.Kind == Kind.ClassDeclaration)))))
                    {
                        location = location.Parent;
                    }

                    break;
                }

                case Kind.Parameter:
                {
                    var parameterDeclaration = location.AsParameterDeclaration();
                    if (((lastLocation != null) && (((lastLocation == parameterDeclaration.Initializer) || ((lastLocation == parameterDeclaration.Name()) && Ast.IsBindingPattern(lastLocation))))))
                    {
                        if ((associatedDeclarationForContainingInitializerOrBindingName == null))
                        {
                            associatedDeclarationForContainingInitializerOrBindingName = location;
                        }
                    }

                    break;
                }

                case Kind.BindingElement:
                {
                    var bindingElement = location.AsBindingElement();
                    if (((lastLocation != null) && (((lastLocation == bindingElement.Initializer) || ((lastLocation == bindingElement.Name()) && Ast.IsBindingPattern(lastLocation))))))
                    {
                        if ((Ast.IsPartOfParameterDeclaration(location) && (associatedDeclarationForContainingInitializerOrBindingName == null)))
                        {
                            associatedDeclarationForContainingInitializerOrBindingName = location;
                        }
                    }

                    break;
                }

                case Kind.InferType:
                {
                    if (((meaning & SymbolFlags.TypeParameter) != 0))
                    {
                        var parameterName = location.AsInferTypeNode().TypeParameter.AsTypeParameterDeclaration().Name();
                        if (((parameterName != null) && (name.EqualsValue(parameterName.TextValue()))))
                        {
                            result = location.AsInferTypeNode().TypeParameter.Symbol();
                            goto loopEnd;
                        }
                    }

                    break;
                }

                case Kind.ExportSpecifier:
                {
                    var exportSpecifier = location.AsExportSpecifier();
                    if ((((lastLocation != null) && (lastLocation == exportSpecifier.PropertyName)) && (location.Parent.Parent.ModuleSpecifier() != null)))
                    {
                        location = location.Parent.Parent.Parent;
                    }

                    break;
                }
            }

            if (isSelfReferenceLocation(location, lastLocation))
            {
                lastSelfReferenceLocation = location;
            }

            lastLocation = location;
            location = location.Parent;
        }

        loopEnd:
            ;
        if (((isUse && (result != null)) && (((lastSelfReferenceLocation == null) || (result != lastSelfReferenceLocation.Symbol())))))
        {
            if ((this.SymbolReferenced != null))
            {
                this.SymbolReferenced(result, meaning);
            }
        }

        if (((result == null) && !excludeGlobals))
        {
            result = this.lookup(this.Globals, name, (meaning | SymbolFlags.GlobalLookup));
        }

        if ((result == null))
        {
            if ((((originalLocation != null) && Ast.IsInJSFile(originalLocation)) && (originalLocation.Parent != null)))
            {
                if (Ast.IsRequireCall(originalLocation.Parent, false))
                {
                    return this.RequireSymbol;
                }
            }
        }

        if ((nameNotFoundMessage != null))
        {
            if ((((propertyWithInvalidInitializer != null) && (this.OnPropertyWithInvalidInitializer != null)) && this.OnPropertyWithInvalidInitializer(originalLocation, name.Materialize(), propertyWithInvalidInitializer, result)))
            {
                return null;
            }

            if ((result == null))
            {
                if ((this.OnFailedToResolveSymbol != null))
                {
                    this.OnFailedToResolveSymbol(originalLocation, name.Materialize(), meaning, nameNotFoundMessage);
                }
            }
            else
            {
                if ((this.OnSuccessfullyResolvedSymbol != null))
                {
                    this.OnSuccessfullyResolvedSymbol(originalLocation, result, meaning, lastLocation, associatedDeclarationForContainingInitializerOrBindingName, withinDeferredContext);
                }
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:354:1.
    private bool useOuterVariableScopeInParameter(Symbol result, Node location, Node lastLocation)
    {
        if (Ast.IsParameterDeclaration(lastLocation))
        {
            var body = location.Body();
            if (((((body != null) && (result.ValueDeclaration != null)) && (result.ValueDeclaration.Pos() >= body.Pos())) && (result.ValueDeclaration.End() <= body.End())))
            {
                var functionLocation = location;
                var declarationRequiresScopeChange = Tristate.Unknown;
                if ((this.GetRequiresScopeChangeCache != null))
                {
                    declarationRequiresScopeChange = this.GetRequiresScopeChangeCache(functionLocation);
                }

                if ((declarationRequiresScopeChange == Tristate.Unknown))
                {
                    declarationRequiresScopeChange = Core.IfElse(Core.Some(functionLocation.Parameters(), this.requiresScopeChange), Tristate.True, Tristate.False);
                    if ((this.SetRequiresScopeChangeCache != null))
                    {
                        this.SetRequiresScopeChangeCache(functionLocation, declarationRequiresScopeChange);
                    }
                }

                return (declarationRequiresScopeChange != Tristate.True);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:380:1.
    private bool requiresScopeChange(Node node)
    {
        var d = node.AsParameterDeclaration();
        return (this.requiresScopeChangeWorker(d.Name()) || ((d.Initializer != null) && this.requiresScopeChangeWorker(d.Initializer)));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:385:1.
    private bool requiresScopeChangeWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ArrowFunction:
            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.Constructor:
            {
                return false;
            }

            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.PropertyAssignment:
            {
                return this.requiresScopeChangeWorker(node.Name());
            }

            case Kind.PropertyDeclaration:
            {
                if (Ast.HasStaticModifier(node))
                {
                    return !this.CompilerOptions.GetEmitStandardClassFields();
                }

                return this.requiresScopeChangeWorker(node.AsPropertyDeclaration().Name());
            }

            default:
            {
                if ((Ast.IsNullishCoalesce(node) || Ast.IsOptionalChain(node)))
                {
                    return (this.CompilerOptions.GetEmitScriptTarget() < ScriptTarget.ES2020);
                }

                if (((Ast.IsBindingElement(node) && (node.AsBindingElement().DotDotDotToken != null)) && Ast.IsObjectBindingPattern(node.Parent)))
                {
                    return (this.CompilerOptions.GetEmitScriptTarget() < ScriptTarget.ES2017);
                }

                if (Ast.IsTypeNode(node))
                {
                    return false;
                }

                return node.ForEachChild(this.requiresScopeChangeWorker);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:410:1.
    private void error(Node location, Message message, params object[] args)
    {
        if ((this.Error != null))
        {
            this.Error(location, message, args);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:417:1.
    private Symbol getSymbolOfDeclaration(Node node)
    {
        if ((this.GetSymbolOfDeclaration != null))
        {
            return this.GetSymbolOfDeclaration(node);
        }

        return node.Symbol();
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:426:1.
    private Symbol lookup(SymbolTable symbols, ByteStringValue name, SymbolFlags meaning)
    {
        if (this.LookupTextValue != null)
            return this.LookupTextValue(symbols, name, meaning);
        if ((this.Lookup != null))
        {
            return this.Lookup(symbols, name.Materialize(), meaning);
        }

        if ((meaning != 0))
        {
            var symbol = symbols?[name];
            if ((symbol != null))
            {
                if (((symbol.Flags & meaning) != 0))
                {
                    return symbol;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:442:1.
    private Symbol argumentsSymbol()
    {
        if ((this.ArgumentsSymbol == null))
        {
            this.ArgumentsSymbol = new Symbol
            {
                Name = "arguments",
                Flags = (SymbolFlags.Property | SymbolFlags.Transient)
            };
        }

        return this.ArgumentsSymbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:450:1.
    public static Symbol GetLocalSymbolForExportDefault(Symbol symbol)
    {
        if ((!isExportDefaultSymbol(symbol) || (GoRuntime.Len(symbol.Declarations) == 0)))
        {
            return null;
        }

        {
            foreach (var decl in GoRuntime.RangeValues(symbol.Declarations))
            {
                var localSymbol = decl.LocalSymbol();
                if ((localSymbol != null))
                {
                    return localSymbol;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:463:1.
    private static bool isExportDefaultSymbol(Symbol symbol)
    {
        return (((symbol != null) && (GoRuntime.Len(symbol.Declarations) > 0)) && Ast.HasSyntacticModifier(symbol.Declarations[0], ModifierFlags.Default));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:467:1.
    private static bool getIsDeferredContext(Node location, Node lastLocation)
    {
        if (((location.Kind != Kind.ArrowFunction) && (location.Kind != Kind.FunctionExpression)))
        {
            return (Ast.IsTypeQueryNode(location) || (((Ast.IsFunctionLikeDeclaration(location) || ((location.Kind == Kind.PropertyDeclaration) && !Ast.IsStatic(location)))) && (((lastLocation == null) || (lastLocation != location.Name())))));
        }

        if (((lastLocation != null) && (lastLocation == location.Name())))
        {
            return false;
        }

        if (((location.BodyData().AsteriskToken != null) || Ast.HasSyntacticModifier(location, ModifierFlags.Async)))
        {
            return true;
        }

        return (Ast.GetImmediatelyInvokedFunctionExpression(location) == null);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:485:1.
    private static bool isTypeParameterSymbolDeclaredInContainer(Symbol symbol, Node container)
    {
        {
            foreach (var decl in GoRuntime.RangeValues(symbol.Declarations))
            {
                if ((decl.Kind == Kind.TypeParameter))
                {
                    var parent = decl.Parent;
                    if ((parent == container))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/nameresolver.go:497:1.
    private static bool isSelfReferenceLocation(Node node, Node lastLocation)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
            {
                return ((lastLocation != null) && (lastLocation == node.Name()));
            }

            case Kind.FunctionDeclaration:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.ModuleDeclaration:
            {
                return true;
            }
        }

        return false;
    }
}

// Declaration counterpart from binder/nameresolver.go; params delegates keep raw args.
public delegate Diagnostic NameResolverError(Node location, Message message, params object[] args);
public partial class NameResolver
{
    public CompilerOptions CompilerOptions;
    public Func<Node, Symbol> GetSymbolOfDeclaration;
    public NameResolverError Error;
    public SymbolTable Globals;
    public Symbol ArgumentsSymbol, RequireSymbol;
    public Func<SymbolTable, string, SymbolFlags, Symbol> Lookup;
    // Private checker hook for borrowed text; public resolver hooks keep their
    // existing string contract when this internal counterpart is not supplied.
    internal Func<SymbolTable, ByteStringValue, SymbolFlags, Symbol> LookupTextValue;
    public Action<Symbol, SymbolFlags> SymbolReferenced;
    public Action<Node, Tristate> SetRequiresScopeChangeCache;
    public Func<Node, Tristate> GetRequiresScopeChangeCache;
    public Func<Node, string, Node, Symbol, bool> OnPropertyWithInvalidInitializer;
    public Action<Node, string, SymbolFlags, Message> OnFailedToResolveSymbol;
    public Action<Node, Symbol, SymbolFlags, Node, Node, bool> OnSuccessfullyResolvedSymbol;
}
