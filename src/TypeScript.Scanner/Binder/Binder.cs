// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/binder/binder.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Binder
{
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:94:1.
    public static void BindSourceFile(SourceFile file)
    {
        if (!file.IsBound())
        {
            bindSourceFile(file);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:119:1.
    private static void bindSourceFile(SourceFile file)
    {
        file.BindOnce(() =>
        {
            var b = getBinder();
            // Go binder-pool return omitted: this port creates an isolated managed binder.
            b.file = file;
            b.unreachableFlow = b.newFlowNode(FlowFlags.Unreachable);
            b.bind(file.AsNode());
            b.bindDeferredExpandoAssignments();
            file.SymbolCount = b.symbolCount;
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:131:1.
    private Symbol newSymbol(SymbolFlags flags, string name)
    {
        this.symbolCount++;
        var result = this.symbolArena.New();
        result.Flags = flags;
        result.Name = name;
        return result;
    }

    // *
    // * Declares a Symbol for the node and adds it to symbols. Reports errors for conflicting identifier names.
    // * @param symbolTable - The symbol table which node will be added to.
    // * @param parent - node's parent declaration.
    // * @param node - The declaration to be added to the symbol table
    // * @param includes - The SymbolFlags that node has in addition to its declaration type (eg: export, ambient, etc.)
    // * @param excludes - The flags which node cannot be declared alongside in a symbol table. Used to report forbidden declarations.
    // 
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:147:1.
    private Symbol declareSymbol(SymbolTable symbolTable, Symbol parent, Node node, SymbolFlags includes, SymbolFlags excludes)
    {
        return this.declareSymbolEx(symbolTable, parent, node, includes, excludes, false, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:151:1.
    private Symbol declareSymbolEx(SymbolTable symbolTable, Symbol parent, Node node, SymbolFlags includes, SymbolFlags excludes, bool isReplaceableByMethod, bool isComputedName)
    {
        Debug.Assert((isComputedName || !Ast.HasDynamicName(node)));
        var isDefaultExport = (Ast.HasSyntacticModifier(node, ModifierFlags.Default) || (Ast.IsExportSpecifier(node) && Ast.ModuleExportNameIsDefault(node.AsExportSpecifier().Name())));
        string name = "";
        switch (true)
        {
            case true when isComputedName:
            {
                name = Ast.InternalSymbolNameComputed;
                break;
            }

            case true when (isDefaultExport && (parent != null)):
            {
                name = Ast.InternalSymbolNameDefault;
                break;
            }

            default:
            {
                name = this.getDeclarationName(node);
                break;
            }
        }

        Symbol symbol = default;
        if ((name == Ast.InternalSymbolNameMissing))
        {
            symbol = this.newSymbol(SymbolFlags.None, Ast.InternalSymbolNameMissing);
        }
        else
        {
            symbol = symbolTable[name];
            if ((symbol == null))
            {
                symbol = this.newSymbol(SymbolFlags.None, name);
                symbolTable[name] = symbol;
                if (isReplaceableByMethod)
                {
                    symbol.Flags |= SymbolFlags.ReplaceableByMethod;
                }
            }
            else
            {
                if ((isReplaceableByMethod && ((symbol.Flags & SymbolFlags.ReplaceableByMethod) == 0)))
                {
                    return symbol;
                }
                else
                {
                    if (((symbol.Flags & excludes) != 0))
                    {
                        if (((symbol.Flags & SymbolFlags.ReplaceableByMethod) != 0))
                        {
                            symbol = this.newSymbol(SymbolFlags.None, name);
                            symbolTable[name] = symbol;
                        }
                        else
                        {
                            if (!(((((includes & SymbolFlags.Variable) != 0) && ((symbol.Flags & SymbolFlags.Assignment) != 0)) || (((includes & SymbolFlags.Assignment) != 0) && ((symbol.Flags & SymbolFlags.Variable) != 0)))))
                            {
                                Message message = default;
                                if (((symbol.Flags & SymbolFlags.BlockScopedVariable) != 0))
                                {
                                    message = Diagnostics.Cannot_redeclare_block_scoped_variable_0;
                                }
                                else
                                {
                                    message = Diagnostics.Duplicate_identifier_0;
                                }

                                var messageNeedsName = true;
                                if ((((symbol.Flags & SymbolFlags.Enum) != 0) || ((includes & SymbolFlags.Enum) != 0)))
                                {
                                    message = Diagnostics.Enum_declarations_can_only_merge_with_namespace_or_other_enum_declarations;
                                    messageNeedsName = false;
                                }

                                var multipleDefaultExports = false;
                                if ((GoRuntime.Len(symbol.Declarations) != 0))
                                {
                                    if (isDefaultExport)
                                    {
                                        message = Diagnostics.A_module_cannot_have_multiple_default_exports;
                                        messageNeedsName = false;
                                        multipleDefaultExports = true;
                                    }
                                    else
                                    {
                                        if ((((GoRuntime.Len(symbol.Declarations) != 0) && Ast.IsExportAssignment(node)) && !node.AsExportAssignment().IsExportEquals))
                                        {
                                            message = Diagnostics.A_module_cannot_have_multiple_default_exports;
                                            messageNeedsName = false;
                                            multipleDefaultExports = true;
                                        }
                                    }
                                }

                                Node declarationName = Ast.GetNameOfDeclaration(node);
                                if ((declarationName == null))
                                {
                                    declarationName = node;
                                }

                                Diagnostic diag = default;
                                if (messageNeedsName)
                                {
                                    diag = this.createDiagnosticForNode(declarationName, message, this.getDisplayName(node));
                                }
                                else
                                {
                                    diag = this.createDiagnosticForNode(declarationName, message);
                                }

                                if ((((Ast.IsTypeAliasDeclaration(node) && Ast.NodeIsMissing(node.Type())) && Ast.HasSyntacticModifier(node, ModifierFlags.Export)) && ((symbol.Flags & (((SymbolFlags.Alias | SymbolFlags.Type) | SymbolFlags.Namespace))) != 0)))
                                {
                                    diag.AddRelatedInfo(this.createDiagnosticForNode(node, Diagnostics.Did_you_mean_0, (("export type { " + node.AsTypeAliasDeclaration().Name().Text()) + " }")));
                                }

                                {
                                    foreach (var range1 in GoRuntime.Range(symbol.Declarations))
                                    {
                                        var index = range1.Key;
                                        var declaration = range1.Value;
                                        Node decl = Ast.GetNameOfDeclaration(declaration);
                                        if ((decl == null))
                                        {
                                            decl = declaration;
                                        }

                                        Diagnostic d = default;
                                        if (messageNeedsName)
                                        {
                                            d = this.createDiagnosticForNode(decl, message, this.getDisplayName(declaration));
                                        }
                                        else
                                        {
                                            d = this.createDiagnosticForNode(decl, message);
                                        }

                                        if (multipleDefaultExports)
                                        {
                                            d.AddRelatedInfo(this.createDiagnosticForNode(declarationName, Core.IfElse((index == 0), Diagnostics.Another_export_default_is_here, Diagnostics.X_and_here)));
                                        }

                                        this.addDiagnostic(d);
                                        if (multipleDefaultExports)
                                        {
                                            diag.AddRelatedInfo(this.createDiagnosticForNode(decl, Diagnostics.The_first_export_default_is_here));
                                        }
                                    }
                                }

                                this.addDiagnostic(diag);
                                if ((((symbol.Flags & SymbolFlags.Accessor) != 0) && ((symbol.Flags & SymbolFlags.Accessor) != (includes & SymbolFlags.Accessor))))
                                {
                                    symbol.Flags |= SymbolFlags.Accessor;
                                }

                                symbol = this.newSymbol(SymbolFlags.None, name);
                            }
                        }
                    }
                }
            }
        }

        this.addDeclarationToSymbol(symbol, node, includes);
        if ((symbol.Parent == null))
        {
            symbol.Parent = parent;
        }
        else
        {
            if ((symbol.Parent != parent))
            {
                throw new InvalidOperationException("Existing symbol parent should match new one");
            }
        }

        return symbol;
    }

    // Should not be called on a declaration with a computed property name,
    // unless it is a well known Symbol.
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:300:1.
    private string getDeclarationName(Node node)
    {
        if (Ast.IsExportAssignment(node))
        {
            return Core.IfElse(node.AsExportAssignment().IsExportEquals, Ast.InternalSymbolNameExportEquals, Ast.InternalSymbolNameDefault);
        }

        var name = Ast.GetNameOfDeclaration(node);
        if ((name != null))
        {
            if (Ast.IsAmbientModule(node))
            {
                var moduleName = name.Text();
                if (Ast.IsGlobalScopeAugmentation(node))
                {
                    return Ast.InternalSymbolNameGlobal;
                }

                {
                    var pattern = Core.TryParsePattern(moduleName);
                    if ((pattern.IsValid() && (pattern.StarIndex >= 0)))
                    {
                        {
                            var attributes = node.AsModuleDeclaration().Attributes;
                            if ((attributes != null))
                            {
                                return ((((Ast.InternalSymbolNamePrefix + "\"") + moduleName) + "\"pattern@") + Strconv.FormatUint((ulong)(Ast.GetNodeId(attributes)), 10));
                            }
                        }
                    }
                }

                return (("\"" + moduleName) + "\"");
            }

            if (Ast.IsPrivateIdentifier(name))
            {
                var containingClass = Ast.GetContainingClass(node);
                if ((containingClass == null))
                {
                    return Ast.InternalSymbolNameMissing;
                }

                return GetSymbolNameForPrivateIdentifier(containingClass.Symbol(), name.Text());
            }

            if ((Ast.IsPropertyNameLiteral(name) || Ast.IsJsxNamespacedName(name)))
            {
                return name.Text();
            }

            if (Ast.IsComputedPropertyName(name))
            {
                var nameExpression = name.Expression();
                if (Ast.IsStringOrNumericLiteralLike(nameExpression))
                {
                    return nameExpression.Text();
                }

                if (Ast.IsSignedNumericLiteral(nameExpression))
                {
                    var unaryExpression = nameExpression.AsPrefixUnaryExpression();
                    return (Scanner.TokenToString(unaryExpression.Operator) + unaryExpression.Operand.Text());
                }

                throw new InvalidOperationException("Only computed properties with literal names have declaration names");
            }

            return Ast.InternalSymbolNameMissing;
        }

        switch (node.Kind)
        {
            case Kind.Constructor:
            {
                return Ast.InternalSymbolNameConstructor;
            }

            case Kind.FunctionType:
            case Kind.CallSignature:
            {
                return Ast.InternalSymbolNameCall;
            }

            case Kind.ConstructorType:
            case Kind.ConstructSignature:
            {
                return Ast.InternalSymbolNameNew;
            }

            case Kind.IndexSignature:
            {
                return Ast.InternalSymbolNameIndex;
            }

            case Kind.ExportDeclaration:
            {
                return Ast.InternalSymbolNameExportStar;
            }

            case Kind.SourceFile:
            case Kind.BinaryExpression:
            {
                return Ast.InternalSymbolNameExportEquals;
            }
        }

        return Ast.InternalSymbolNameMissing;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:361:1.
    private string getDisplayName(Node node)
    {
        var nameNode = node.Name();
        if ((nameNode != null))
        {
            return Scanner.DeclarationNameToStringByteString(nameNode);
        }

        var name = this.getDeclarationName(node);
        if ((name != Ast.InternalSymbolNameMissing))
        {
            return name;
        }

        return "(Missing)";
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:373:1.
    public static string GetSymbolNameForPrivateIdentifier(Symbol containingClassSymbol, string description)
    {
        return ((((Ast.InternalSymbolNamePrefix + "#") + Strconv.Itoa(unchecked((long)Ast.GetSymbolId(containingClassSymbol)))) + "@") + description);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:377:1.
    private Symbol declareModuleMember(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        var container = this.container;
        var hasExportModifier = (((Ast.GetCombinedModifierFlags(node) & ModifierFlags.Export) != 0) || Ast.IsImplicitlyExportedJSDocDeclaration(node));
        if (((symbolFlags & SymbolFlags.Alias) != 0))
        {
            if (((node.Kind == Kind.ExportSpecifier) || (((node.Kind == Kind.ImportEqualsDeclaration) && hasExportModifier))))
            {
                return this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, symbolFlags, symbolExcludes);
            }

            return this.declareSymbol(Ast.GetLocals(container), null, node, symbolFlags, symbolExcludes);
        }

        if ((!Ast.IsAmbientModule(node) && ((hasExportModifier || ((container.Flags & NodeFlags.ExportContext) != 0)))))
        {
            if ((!Ast.IsLocalsContainer(container) || ((Ast.HasSyntacticModifier(node, ModifierFlags.Default) && (this.getDeclarationName(node) == Ast.InternalSymbolNameMissing)))))
            {
                return this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, symbolFlags, symbolExcludes);
            }

            var exportKind = SymbolFlags.None;
            if (((symbolFlags & SymbolFlags.Value) != 0))
            {
                exportKind = SymbolFlags.ExportValue;
            }

            var local = this.declareSymbol(Ast.GetLocals(container), null, node, exportKind, symbolExcludes);
            local.ExportSymbol = this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, symbolFlags, symbolExcludes);
            node.ExportableData().LocalSymbol = local;
            return local;
        }

        return this.declareSymbol(Ast.GetLocals(container), null, node, symbolFlags, symbolExcludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:418:1.
    private Symbol declareClassMember(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        if (Ast.IsStatic(node))
        {
            return this.declareSymbol(Ast.GetExports(this.container.Symbol()), this.container.Symbol(), node, symbolFlags, symbolExcludes);
        }

        return this.declareSymbol(Ast.GetMembers(this.container.Symbol()), this.container.Symbol(), node, symbolFlags, symbolExcludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:425:1.
    private Symbol declareSourceFileMember(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        if (Ast.IsExternalModule(this.file))
        {
            return this.declareModuleMember(node, symbolFlags, symbolExcludes);
        }

        return this.declareSymbol(Ast.GetLocals(this.file.AsNode()), null, node, symbolFlags, symbolExcludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:432:1.
    private Symbol declareSymbolAndAddToSymbolTable(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        switch (this.container.Kind)
        {
            case Kind.ModuleDeclaration:
            {
                return this.declareModuleMember(node, symbolFlags, symbolExcludes);
            }

            case Kind.SourceFile:
            {
                return this.declareSourceFileMember(node, symbolFlags, symbolExcludes);
            }

            case Kind.ClassExpression:
            case Kind.ClassDeclaration:
            {
                return this.declareClassMember(node, symbolFlags, symbolExcludes);
            }

            case Kind.EnumDeclaration:
            {
                return this.declareSymbol(Ast.GetExports(this.container.Symbol()), this.container.Symbol(), node, symbolFlags, symbolExcludes);
            }

            case Kind.TypeLiteral:
            case Kind.ObjectLiteralExpression:
            case Kind.InterfaceDeclaration:
            case Kind.JsxAttributes:
            {
                return this.declareSymbol(Ast.GetMembers(this.container.Symbol()), this.container.Symbol(), node, symbolFlags, symbolExcludes);
            }

            case Kind.FunctionType:
            case Kind.ConstructorType:
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.IndexSignature:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.ClassStaticBlockDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.MappedType:
            {
                return this.declareSymbol(Ast.GetLocals(this.container), null, node, symbolFlags, symbolExcludes);
            }
        }

        throw new InvalidOperationException("Unhandled case in declareSymbolAndAddToSymbolTable");
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:453:1.
    private FlowNode newFlowNode(FlowFlags flags)
    {
        var result = this.flowNodeArena.New();
        result.Flags = flags;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:459:1.
    private FlowNode newFlowNodeEx(FlowFlags flags, Node node, FlowNode antecedent)
    {
        var result = this.newFlowNode(flags);
        result.Node = node;
        result.Antecedent = antecedent;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:466:1.
    private FlowNode createLoopLabel()
    {
        return this.newFlowNode(FlowFlags.LoopLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:470:1.
    private FlowNode createBranchLabel()
    {
        return this.newFlowNode(FlowFlags.BranchLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:474:1.
    private FlowNode createReduceLabel(FlowNode target, FlowList antecedents, FlowNode antecedent)
    {
        return this.newFlowNodeEx(FlowFlags.ReduceLabel, Ast.NewFlowReduceLabelData(target, antecedents), antecedent);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:478:1.
    private FlowNode createFlowCondition(FlowFlags flags, FlowNode antecedent, Node expression)
    {
        if (((antecedent.Flags & FlowFlags.Unreachable) != 0))
        {
            return antecedent;
        }

        if ((expression == null))
        {
            if (((flags & FlowFlags.TrueCondition) != 0))
            {
                return antecedent;
            }

            return this.unreachableFlow;
        }

        if (((((((expression.Kind == Kind.TrueKeyword) && ((flags & FlowFlags.FalseCondition) != 0)) || ((expression.Kind == Kind.FalseKeyword) && ((flags & FlowFlags.TrueCondition) != 0)))) && !Ast.IsExpressionOfOptionalChainRoot(expression)) && !Ast.IsNullishCoalesce(expression.Parent)))
        {
            return this.unreachableFlow;
        }

        if (!isNarrowingExpression(expression))
        {
            return antecedent;
        }

        setFlowNodeReferenced(antecedent);
        return this.newFlowNodeEx(flags, expression, antecedent);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:498:1.
    private FlowNode createFlowMutation(FlowFlags flags, FlowNode antecedent, Node node)
    {
        setFlowNodeReferenced(antecedent);
        this.hasFlowEffects = true;
        var result = this.newFlowNodeEx(flags, node, antecedent);
        if ((this.currentExceptionTarget != null))
        {
            this.addAntecedent(this.currentExceptionTarget, result);
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:508:1.
    private FlowNode createFlowSwitchClause(FlowNode antecedent, Node switchStatement, int clauseStart, int clauseEnd)
    {
        setFlowNodeReferenced(antecedent);
        return this.newFlowNodeEx(FlowFlags.SwitchClause, Ast.NewFlowSwitchClauseData(switchStatement, clauseStart, clauseEnd), antecedent);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:513:1.
    private FlowNode createFlowCall(FlowNode antecedent, Node node)
    {
        setFlowNodeReferenced(antecedent);
        this.hasFlowEffects = true;
        return this.newFlowNodeEx(FlowFlags.Call, node, antecedent);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:519:1.
    private FlowList newFlowList(FlowNode head, FlowList tail)
    {
        var result = this.flowListArena.New();
        result.Flow = head;
        result.Next = tail;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:526:1.
    private FlowList combineFlowLists(FlowList head, FlowList tail)
    {
        if ((head == null))
        {
            return tail;
        }

        return this.newFlowList(head.Flow, this.combineFlowLists(head.Next, tail));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:533:1.
    private List<Node> newSingleDeclaration(Node declaration)
    {
        return this.singleDeclarationsArena.NewSlice1(declaration);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:537:1.
    private static void setFlowNodeReferenced(FlowNode flow)
    {
        if (((flow.Flags & FlowFlags.Referenced) == 0))
        {
            flow.Flags |= FlowFlags.Referenced;
        }
        else
        {
            flow.Flags |= FlowFlags.Shared;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:546:1.
    private void addAntecedent(FlowNode label, FlowNode antecedent)
    {
        if (((antecedent.Flags & FlowFlags.Unreachable) != 0))
        {
            return;
        }

        FlowList last = default;
        {
            var list = label.Antecedents;
            for (; (list != null); list = list.Next)
            {
                if ((list.Flow == antecedent))
                {
                    return;
                }

                last = list;
            }
        }

        if ((last == null))
        {
            label.Antecedents = this.newFlowList(antecedent, null);
        }
        else
        {
            last.Next = this.newFlowList(antecedent, null);
        }

        setFlowNodeReferenced(antecedent);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:566:1.
    private FlowNode finishFlowLabel(FlowNode label)
    {
        if ((label.Antecedents == null))
        {
            return this.unreachableFlow;
        }

        if ((label.Antecedents.Next == null))
        {
            return label.Antecedents.Flow;
        }

        return label;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:576:1.
    private bool bind(Node node)
    {
        if ((node == null))
        {
            return false;
        }

        switch (node.Kind)
        {
            case Kind.Identifier:
            {
                node.AsIdentifier().FlowNode = this.currentFlow;
                this.checkContextualIdentifier(node);
                break;
            }

            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            {
                if ((node.Kind == Kind.ThisKeyword))
                {
                    this.seenThisKeyword = true;
                }

                node.AsKeywordExpression().FlowNode = this.currentFlow;
                break;
            }

            case Kind.QualifiedName:
            {
                if (((this.currentFlow != null) && Ast.IsPartOfTypeQuery(node)))
                {
                    node.AsQualifiedName().FlowNode = this.currentFlow;
                }

                break;
            }

            case Kind.MetaProperty:
            {
                node.AsMetaProperty().FlowNode = this.currentFlow;
                break;
            }

            case Kind.PrivateIdentifier:
            {
                this.checkPrivateIdentifier(node);
                break;
            }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                if (((this.currentFlow != null) && isNarrowableReference(node)))
                {
                    setFlowNode(node, this.currentFlow);
                }

                break;
            }

            case Kind.BinaryExpression:
            {
                switch (Ast.GetAssignmentDeclarationKind(node))
                {
                    case JSDeclarationKind.ModuleExports:
                    {
                        this.bindModuleExportsAssignment(node);
                        break;
                    }

                    case JSDeclarationKind.ExportsProperty:
                    {
                        this.bindExportsOrObjectDefineProperty(node);
                        break;
                    }

                    case JSDeclarationKind.Property:
                    {
                        this.bindExpandoPropertyAssignment(node);
                        break;
                    }

                    case JSDeclarationKind.ThisProperty:
                    {
                        this.bindThisPropertyAssignment(node);
                        break;
                    }
                }

                this.checkStrictModeBinaryExpression(node);
                break;
            }

            case Kind.CatchClause:
            {
                this.checkStrictModeCatchClause(node);
                break;
            }

            case Kind.DeleteExpression:
            {
                this.checkStrictModeDeleteExpression(node);
                break;
            }

            case Kind.PostfixUnaryExpression:
            {
                this.checkStrictModePostfixUnaryExpression(node);
                break;
            }

            case Kind.PrefixUnaryExpression:
            {
                this.checkStrictModePrefixUnaryExpression(node);
                break;
            }

            case Kind.WithStatement:
            {
                this.checkStrictModeWithStatement(node);
                break;
            }

            case Kind.LabeledStatement:
            {
                this.checkStrictModeLabeledStatement(node);
                break;
            }

            case Kind.ThisType:
            {
                this.seenThisKeyword = true;
                break;
            }

            case Kind.TypeParameter:
            {
                this.bindTypeParameter(node);
                break;
            }

            case Kind.Parameter:
            {
                this.bindParameter(node);
                break;
            }

            case Kind.VariableDeclaration:
            {
                this.bindVariableDeclarationOrBindingElement(node);
                break;
            }

            case Kind.BindingElement:
            {
                node.AsBindingElement().FlowNode = this.currentFlow;
                this.bindVariableDeclarationOrBindingElement(node);
                break;
            }

            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            {
                this.bindPropertyWorker(node);
                break;
            }

            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            {
                this.bindPropertyOrMethodOrAccessor(node, SymbolFlags.Property, SymbolFlags.PropertyExcludes);
                break;
            }

            case Kind.EnumMember:
            {
                this.bindPropertyOrMethodOrAccessor(node, SymbolFlags.EnumMember, SymbolFlags.EnumMemberExcludes);
                break;
            }

            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.IndexSignature:
            {
                this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.Signature, SymbolFlags.None);
                break;
            }

            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            {
                this.bindPropertyOrMethodOrAccessor(node, (SymbolFlags.Method | getOptionalSymbolFlagForNode(node)), Core.IfElse(Ast.IsObjectLiteralMethod(node), SymbolFlags.Value, SymbolFlags.MethodExcludes));
                break;
            }

            case Kind.FunctionDeclaration:
            {
                this.bindFunctionDeclaration(node);
                break;
            }

            case Kind.Constructor:
            {
                this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.Constructor, SymbolFlags.None);
                break;
            }

            case Kind.GetAccessor:
            {
                this.bindPropertyOrMethodOrAccessor(node, SymbolFlags.GetAccessor, SymbolFlags.GetAccessorExcludes);
                break;
            }

            case Kind.SetAccessor:
            {
                this.bindPropertyOrMethodOrAccessor(node, SymbolFlags.SetAccessor, SymbolFlags.SetAccessorExcludes);
                break;
            }

            case Kind.FunctionType:
            case Kind.ConstructorType:
            {
                this.bindFunctionOrConstructorType(node);
                break;
            }

            case Kind.TypeLiteral:
            case Kind.MappedType:
            {
                this.bindAnonymousDeclaration(node, SymbolFlags.TypeLiteral, Ast.InternalSymbolNameType);
                break;
            }

            case Kind.ObjectLiteralExpression:
            {
                this.bindAnonymousDeclaration(node, SymbolFlags.ObjectLiteral, Ast.InternalSymbolNameObject);
                break;
            }

            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            {
                this.bindFunctionExpression(node);
                break;
            }

            case Kind.ClassExpression:
            case Kind.ClassDeclaration:
            {
                this.bindClassLikeDeclaration(node);
                break;
            }

            case Kind.InterfaceDeclaration:
            {
                this.bindBlockScopedDeclaration(node, SymbolFlags.Interface, SymbolFlags.InterfaceExcludes);
                break;
            }

            case Kind.CallExpression:
            {
                switch (Ast.GetAssignmentDeclarationKind(node))
                {
                    case JSDeclarationKind.ObjectDefinePropertyValue:
                    {
                        this.bindExpandoPropertyAssignment(node);
                        break;
                    }

                    case JSDeclarationKind.ObjectDefinePropertyExports:
                    {
                        this.bindExportsOrObjectDefineProperty(node);
                        break;
                    }
                }

                if (Ast.IsInJSFile(node))
                {
                    this.bindCallExpression(node);
                }

                break;
            }

            case Kind.TypeAliasDeclaration:
            {
                this.bindBlockScopedDeclaration(node, SymbolFlags.TypeAlias, SymbolFlags.TypeAliasExcludes);
                break;
            }

            case Kind.JSTypeAliasDeclaration:
            {
                if (!Ast.IsSourceFile(this.blockScopeContainer))
                {
                    this.bindBlockScopedDeclaration(node, SymbolFlags.TypeAlias, SymbolFlags.TypeAliasExcludes);
                }

                break;
            }

            case Kind.EnumDeclaration:
            {
                this.bindEnumDeclaration(node);
                break;
            }

            case Kind.ModuleDeclaration:
            {
                this.bindModuleDeclaration(node);
                break;
            }

            case Kind.ImportEqualsDeclaration:
            case Kind.NamespaceImport:
            case Kind.ImportSpecifier:
            case Kind.ExportSpecifier:
            {
                this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.Alias, SymbolFlags.AliasExcludes);
                break;
            }

            case Kind.NamespaceExportDeclaration:
            {
                this.bindNamespaceExportDeclaration(node);
                break;
            }

            case Kind.ImportClause:
            {
                this.bindImportClause(node);
                break;
            }

            case Kind.ExportDeclaration:
            {
                this.bindExportDeclaration(node);
                break;
            }

            case Kind.ExportAssignment:
            {
                this.bindExportAssignment(node);
                break;
            }

            case Kind.SourceFile:
            {
                this.bindSourceFileIfExternalModule();
                break;
            }

            case Kind.JsxAttributes:
            {
                this.bindJsxAttributes(node);
                break;
            }

            case Kind.JsxAttribute:
            {
                this.bindJsxAttribute(node, SymbolFlags.Property, SymbolFlags.PropertyExcludes);
                break;
            }
        }

        var thisNodeOrAnySubnodesHasError = ((node.Flags & NodeFlags.ThisNodeHasError) != 0);
        if ((node.Kind > Kind.LastToken))
        {
            var saveSeenParseError = this.seenParseError;
            this.seenParseError = false;
            var containerFlags = GetContainerFlags(node);
            if ((containerFlags == ContainerFlags.None))
            {
                this.bindChildren(node);
            }
            else
            {
                this.bindContainer(node, containerFlags);
            }

            if (this.seenParseError)
            {
                thisNodeOrAnySubnodesHasError = true;
            }

            this.seenParseError = saveSeenParseError;
        }

        if (thisNodeOrAnySubnodesHasError)
        {
            node.Flags |= NodeFlags.ThisNodeOrAnySubNodesHasError;
            this.seenParseError = true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:751:1.
    private void bindPropertyWorker(Node node)
    {
        var isAutoAccessor = Ast.IsAutoAccessorPropertyDeclaration(node);
        var includes = Core.IfElse(isAutoAccessor, SymbolFlags.Accessor, SymbolFlags.Property);
        var excludes = Core.IfElse(isAutoAccessor, SymbolFlags.AccessorExcludes, SymbolFlags.PropertyExcludes);
        this.bindPropertyOrMethodOrAccessor(node, (includes | getOptionalSymbolFlagForNode(node)), excludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:758:1.
    private void bindSourceFileIfExternalModule()
    {
        this.setExportContextFlag(this.file.AsNode());
        if (Ast.IsExternalOrCommonJSModule(this.file))
        {
            this.bindSourceFileAsExternalModule();
        }
        else
        {
            if (Ast.IsJsonSourceFile(this.file))
            {
                this.bindSourceFileAsExternalModule();
                var originalSymbol = this.file.Symbol;
                this.declareSymbol(Ast.GetSymbolTable(new GoRef<SymbolTable>(() => this.file.Symbol.Exports, value => this.file.Symbol.Exports = value)), this.file.Symbol, this.file.AsNode(), SymbolFlags.Property, SymbolFlags.All);
                this.file.Symbol = originalSymbol;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:771:1.
    private void bindSourceFileAsExternalModule()
    {
        this.bindAnonymousDeclaration(this.file.AsNode(), SymbolFlags.ValueModule, (("\"" + TsPath.RemoveFileExtension(GoString.ToUtf8(this.file.FileName() ?? ""))) + "\""));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:775:1.
    private void bindModuleDeclaration(Node node)
    {
        this.setExportContextFlag(node);
        if (Ast.IsAmbientModule(node))
        {
            if (Ast.HasSyntacticModifier(node, ModifierFlags.Export))
            {
                this.errorOnFirstToken(node, Diagnostics.X_export_modifier_cannot_be_applied_to_ambient_modules_and_module_augmentations_since_they_are_always_visible);
            }

            if (Ast.IsModuleAugmentationExternal(node))
            {
                this.declareModuleSymbol(node);
            }
            else
            {
                var name = node.AsModuleDeclaration().Name();
                var symbol = this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.ValueModule, SymbolFlags.ValueModuleExcludes);
                if (Ast.IsStringLiteral(name))
                {
                    var attributes = node.AsModuleDeclaration().Attributes;
                    var pattern = Core.TryParsePattern(name.Text());
                    if (!pattern.IsValid())
                    {
                        this.errorOnFirstToken(name, Diagnostics.Pattern_0_can_have_at_most_one_Asterisk_character, name.Text());
                    }
                    else
                    {
                        if ((pattern.StarIndex >= 0))
                        {
                            this.file.PatternAmbientModules = GoRuntime.Append(this.file.PatternAmbientModules, new PatternAmbientModule { Pattern = pattern, Symbol = symbol });
                        }
                        else
                        {
                            if ((attributes != null))
                            {
                                this.errorOnNode(name, Diagnostics.An_ambient_module_declaration_with_import_attributes_must_use_a_pattern_name_with_an_Asterisk_character);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            var state = this.declareModuleSymbol(node);
            if ((state != ModuleInstanceState.NonInstantiated))
            {
                var symbol2 = node.Symbol();
                var constEnumOnlyModule = (((((symbol2.Flags & (((SymbolFlags.Function | SymbolFlags.Class) | SymbolFlags.RegularEnum))) == 0)) && (state == ModuleInstanceState.ConstEnumOnly)) && !this.notConstEnumOnlyModules.Contains(symbol2));
                if (constEnumOnlyModule)
                {
                    symbol2.Flags |= SymbolFlags.ConstEnumOnlyModule;
                }
                else
                {
                    symbol2.Flags &= ~(SymbolFlags.ConstEnumOnlyModule);
                    this.notConstEnumOnlyModules.Add(symbol2);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:820:1.
    private ModuleInstanceState declareModuleSymbol(Node node)
    {
        var state = Ast.GetModuleInstanceState(node);
        var instantiated = (state != ModuleInstanceState.NonInstantiated);
        this.declareSymbolAndAddToSymbolTable(node, Core.IfElse(instantiated, SymbolFlags.ValueModule, SymbolFlags.NamespaceModule), Core.IfElse(instantiated, SymbolFlags.ValueModuleExcludes, SymbolFlags.NamespaceModuleExcludes));
        return state;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:827:1.
    private void bindNamespaceExportDeclaration(Node node)
    {
        if ((node.Modifiers() != null))
        {
            this.errorOnNode(node, Diagnostics.Modifiers_cannot_appear_here);
        }

        switch (true)
        {
            case true when !Ast.IsSourceFile(node.Parent):
            {
                this.errorOnNode(node, Diagnostics.Global_module_exports_may_only_appear_at_top_level);
                break;
            }

            case true when !Ast.IsExternalModule(node.Parent.AsSourceFile()):
            {
                this.errorOnNode(node, Diagnostics.Global_module_exports_may_only_appear_in_module_files);
                break;
            }

            case true when !node.Parent.AsSourceFile().IsDeclarationFile:
            {
                this.errorOnNode(node, Diagnostics.Global_module_exports_may_only_appear_in_declaration_files);
                break;
            }

            default:
            {
                this.declareSymbol(Ast.GetSymbolTable(new GoRef<SymbolTable>(() => this.file.GlobalExports, value => this.file.GlobalExports = value)), this.file.Symbol, node, SymbolFlags.Alias, SymbolFlags.AliasExcludes);
                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:843:1.
    private void bindImportClause(Node node)
    {
        if ((node.AsImportClause().Name() != null))
        {
            this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.Alias, SymbolFlags.AliasExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:849:1.
    private void bindExportDeclaration(Node node)
    {
        var decl = node.AsExportDeclaration();
        if ((this.container.Symbol() == null))
        {
            this.bindAnonymousDeclaration(node, SymbolFlags.ExportStar, this.getDeclarationName(node));
        }
        else
        {
            if ((decl.ExportClause == null))
            {
                this.declareSymbol(Ast.GetExports(this.container.Symbol()), this.container.Symbol(), node, SymbolFlags.ExportStar, SymbolFlags.None);
            }
            else
            {
                if (Ast.IsNamespaceExport(decl.ExportClause))
                {
                    this.declareSymbol(Ast.GetExports(this.container.Symbol()), this.container.Symbol(), decl.ExportClause, SymbolFlags.Alias, SymbolFlags.AliasExcludes);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:862:1.
    private void bindExportAssignment(Node node)
    {
        var container = this.container;
        if (((container.Symbol() == null) && Ast.IsExportAssignment(node)))
        {
            this.bindAnonymousDeclaration(node, SymbolFlags.Value, this.getDeclarationName(node));
        }
        else
        {
            var flags = Core.IfElse(Ast.ExpressionIsAlias(node.Expression()), SymbolFlags.Alias, SymbolFlags.Property);
            var symbol = this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, flags, SymbolFlags.All);
            if (node.AsExportAssignment().IsExportEquals)
            {
                SetValueDeclaration(symbol, node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:879:1.
    private void bindJsxAttributes(Node node)
    {
        this.bindAnonymousDeclaration(node, SymbolFlags.ObjectLiteral, Ast.InternalSymbolNameJSXAttributes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:883:1.
    private void bindJsxAttribute(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        this.declareSymbolAndAddToSymbolTable(node, symbolFlags, symbolExcludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:887:1.
    private void setExportContextFlag(Node node)
    {
        if ((((node.Flags & NodeFlags.Ambient) != 0) && !this.hasExportDeclarations(node)))
        {
            node.Flags |= NodeFlags.ExportContext;
        }
        else
        {
            node.Flags &= ~(NodeFlags.ExportContext);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:897:1.
    private bool hasExportDeclarations(Node node)
    {
        List<Node> statements = default;
        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                statements = node.Statements();
                break;
            }

            case Kind.ModuleDeclaration:
            {
                var body = node.Body();
                if (((body != null) && Ast.IsModuleBlock(body)))
                {
                    statements = body.Statements();
                }

                break;
            }
        }

        return Core.Some(statements, (s) =>
        {
            return (Ast.IsExportDeclaration(s) || Ast.IsExportAssignment(s));
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:913:1.
    private void bindFunctionExpression(Node node)
    {
        if (((!this.file.IsDeclarationFile && ((node.Flags & NodeFlags.Ambient) == 0)) && Ast.IsAsyncFunction(node)))
        {
            this.emitFlags |= NodeFlags.HasAsyncFunctions;
        }

        setFlowNode(node, this.currentFlow);
        var bindingName = Ast.InternalSymbolNameFunction;
        if ((Ast.IsFunctionExpression(node) && (node.AsFunctionExpression().Name() != null)))
        {
            this.checkStrictModeFunctionName(node);
            bindingName = node.AsFunctionExpression().Name().Text();
        }

        this.bindAnonymousDeclaration(node, SymbolFlags.Function, bindingName);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:926:1.
    private void bindCallExpression(Node node)
    {
        if (((this.file.CommonJSModuleIndicator == null) && Ast.IsRequireCall(node, false)))
        {
            this.setCommonJSModuleIndicator(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:934:1.
    private bool setCommonJSModuleIndicator(Node node)
    {
        if (((this.file.ExternalModuleIndicator != null) && (this.file.ExternalModuleIndicator != this.file.AsNode())))
        {
            return false;
        }

        if ((this.file.CommonJSModuleIndicator == null))
        {
            this.file.CommonJSModuleIndicator = node;
            if ((this.file.ExternalModuleIndicator == null))
            {
                this.bindSourceFileAsExternalModule();
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:947:1.
    private void bindClassLikeDeclaration(Node node)
    {
        var name = node.Name();
        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
            {
                this.bindBlockScopedDeclaration(node, SymbolFlags.Class, SymbolFlags.ClassExcludes);
                break;
            }

            case Kind.ClassExpression:
            {
                var nameText = Ast.InternalSymbolNameClass;
                if ((name != null))
                {
                    nameText = name.Text();
                }

                this.bindAnonymousDeclaration(node, SymbolFlags.Class, nameText);
                break;
            }
        }

        var symbol = node.Symbol();
        var prototypeSymbol = this.newSymbol((SymbolFlags.Property | SymbolFlags.Prototype), "prototype");
        var symbolExport = Ast.GetExports(symbol)[prototypeSymbol.Name];
        if ((symbolExport != null))
        {
            this.errorOnNode(symbolExport.Declarations[0], Diagnostics.Duplicate_identifier_0, Ast.SymbolName(prototypeSymbol));
        }

        Ast.GetExports(symbol)[prototypeSymbol.Name] = prototypeSymbol;
        prototypeSymbol.Parent = symbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:978:1.
    private void bindPropertyOrMethodOrAccessor(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        if (((!this.file.IsDeclarationFile && ((node.Flags & NodeFlags.Ambient) == 0)) && Ast.IsAsyncFunction(node)))
        {
            this.emitFlags |= NodeFlags.HasAsyncFunctions;
        }

        if (((this.currentFlow != null) && Ast.IsObjectLiteralOrClassExpressionMethodOrAccessor(node)))
        {
            setFlowNode(node, this.currentFlow);
        }

        if (Ast.HasDynamicName(node))
        {
            this.bindAnonymousDeclaration(node, symbolFlags, Ast.InternalSymbolNameComputed);
        }
        else
        {
            this.declareSymbolAndAddToSymbolTable(node, symbolFlags, symbolExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:992:1.
    private void bindFunctionOrConstructorType(Node node)
    {
        var symbol = this.newSymbol(SymbolFlags.Signature, this.getDeclarationName(node));
        this.addDeclarationToSymbol(symbol, node, SymbolFlags.Signature);
        var typeLiteralSymbol = this.newSymbol(SymbolFlags.TypeLiteral, Ast.InternalSymbolNameType);
        this.addDeclarationToSymbol(typeLiteralSymbol, node, SymbolFlags.TypeLiteral);
        typeLiteralSymbol.Members = new SymbolTable();
        typeLiteralSymbol.Members[symbol.Name] = symbol;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1007:1.
    private void addLateBoundAssignmentDeclarationToSymbol(Node node, Symbol symbol)
    {
        var exports = Ast.GetExports(symbol);
        var assignmentSymbol = exports[Ast.InternalSymbolNameAssignmentDeclaration];
        if ((assignmentSymbol == null))
        {
            assignmentSymbol = this.newSymbol(SymbolFlags.None, Ast.InternalSymbolNameAssignmentDeclaration);
            exports[Ast.InternalSymbolNameAssignmentDeclaration] = assignmentSymbol;
        }

        assignmentSymbol.Declarations = GoRuntime.Append(assignmentSymbol.Declarations, node);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1017:1.
    private void bindModuleExportsAssignment(Node node)
    {
        if (this.setCommonJSModuleIndicator(node))
        {
            var container = this.file.AsNode();
            var flags = Core.IfElse(Ast.ExpressionIsAlias(node.AsBinaryExpression().Right), SymbolFlags.Alias, SymbolFlags.Property);
            var symbol = this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, flags, 0);
            SetValueDeclaration(symbol, node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1026:1.
    private void bindExpandoPropertyAssignment(Node node)
    {
        this.expandoAssignments = GoRuntime.Append(this.expandoAssignments, new ExpandoAssignmentInfo { node = node, container = this.container, blockScopeContainer = this.blockScopeContainer });
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1034:1.
    private void bindDeferredExpandoAssignments()
    {
        {
            foreach (var info in GoRuntime.RangeValues(this.expandoAssignments))
            {
                this.container = info.container;
                this.blockScopeContainer = info.blockScopeContainer;
                this.bindDeferredExpandoAssignment(info.node);
            }
        }
    }

    // If the given module symbol has an export= symbol, promote exports with a type or namespace meaning
    // from the module symbol onto the export= symbol and, if any such exports exist, mark the export=
    // symbol as a namespace module.
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1045:1.
    private void bindCommonJSTypeExports(Symbol moduleSymbol)
    {
        var moduleExports = moduleSymbol.Exports;
        {
            var exportEquals = moduleExports?[Ast.InternalSymbolNameExportEquals];
            if ((exportEquals != null))
            {
                {
                    foreach (var symbol in GoRuntime.RangeValues(moduleExports))
                    {
                        if (((symbol.Name != Ast.InternalSymbolNameExportEquals) && ((symbol.Flags & ((SymbolFlags.Type | SymbolFlags.Namespace))) != 0)))
                        {
                            Ast.GetExports(exportEquals)[symbol.Name] = symbol;
                            exportEquals.Flags |= SymbolFlags.NamespaceModule;
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1057:1.
    private void bindDeferredExpandoAssignment(Node node)
    {
        var parent = getParentOfPropertyAssignment(node);
        var symbol = this.lookupEntity(parent, this.blockScopeContainer);
        if ((symbol == null))
        {
            symbol = this.lookupEntity(parent, this.container);
        }

        {
            symbol = getInitializerSymbol(symbol);
            if ((symbol != null))
            {
                if (Ast.HasDynamicName(node))
                {
                    this.bindAnonymousDeclaration(node, (SymbolFlags.Property | SymbolFlags.Assignment), Ast.InternalSymbolNameComputed);
                    this.addLateBoundAssignmentDeclarationToSymbol(node, symbol);
                }
                else
                {
                    var exports = Ast.GetExports(symbol);
                    {
                        var existing = exports[this.getDeclarationName(node)];
                        if (((existing == null) || ((existing.Flags & SymbolFlags.Assignment) != 0)))
                        {
                            this.declareSymbol(exports, symbol, node, (SymbolFlags.Property | SymbolFlags.Assignment), SymbolFlags.PropertyExcludes);
                        }
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1077:1.
    private static Node getParentOfPropertyAssignment(Node node)
    {
        switch (node.Kind)
        {
            case Kind.BinaryExpression:
            {
                return node.AsBinaryExpression().Left.Expression();
            }

            case Kind.CallExpression:
            {
                return node.Arguments()[0];
            }
        }

        throw new InvalidOperationException("Unhandled case in getParentOfPropertyAssignment");
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1087:1.
    private void bindExportsOrObjectDefineProperty(Node node)
    {
        if (this.setCommonJSModuleIndicator(node))
        {
            var container = this.file.AsNode();
            var flags = Core.IfElse((Ast.IsBinaryExpression(node) && Ast.ExpressionIsAlias(node.AsBinaryExpression().Right)), SymbolFlags.Alias, SymbolFlags.FunctionScopedVariable);
            this.declareSymbol(Ast.GetExports(container.Symbol()), container.Symbol(), node, flags, SymbolFlags.FunctionScopedVariableExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1095:1.
    private static Symbol getInitializerSymbol(Symbol symbol)
    {
        if (((symbol == null) || (symbol.ValueDeclaration == null)))
        {
            return null;
        }

        var declaration = symbol.ValueDeclaration;
        switch (true)
        {
            case true when (Ast.IsFunctionDeclaration(declaration) || (Ast.IsInJSFile(declaration) && Ast.IsClassDeclaration(declaration))):
            {
                return symbol;
            }

            case true when (Ast.IsVariableDeclaration(declaration) && ((((declaration.Parent.Flags & NodeFlags.Const) != 0) || Ast.IsInJSFile(declaration)))):
            {
                var initializer = declaration.Initializer();
                if (Ast.IsExpandoInitializer(declaration, initializer))
                {
                    return initializer.Symbol();
                }

                break;
            }

            case true when (Ast.IsBinaryExpression(declaration) && Ast.IsInJSFile(declaration)):
            {
                var initializer2 = declaration.AsBinaryExpression().Right;
                if (Ast.IsExpandoInitializer(declaration, initializer2))
                {
                    return initializer2.Symbol();
                }

                break;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1122:1.
    private void bindThisPropertyAssignment(Node node)
    {
        if (!Ast.IsInJSFile(node))
        {
            return;
        }

        var bin = node.AsBinaryExpression();
        if (((Ast.IsPropertyAccessExpression(bin.Left) && Ast.IsPrivateIdentifier(bin.Left.AsPropertyAccessExpression().Name())) || (this.thisContainer == null)))
        {
            return;
        }

        {
            var tuple4 = this.getThisClassAndSymbolTable();
            var classSymbol = tuple4.Item1;
            var symbolTable = tuple4.Item2;
            if ((symbolTable != null))
            {
                if (Ast.HasDynamicName(node))
                {
                    this.declareSymbolEx(symbolTable, classSymbol, node, SymbolFlags.Property, SymbolFlags.None, true, true);
                    this.addLateBoundAssignmentDeclarationToSymbol(node, classSymbol);
                }
                else
                {
                    this.declareSymbolEx(symbolTable, classSymbol, node, (SymbolFlags.Property | SymbolFlags.Assignment), SymbolFlags.None, true, false);
                }
            }
            else
            {
                if (((this.thisContainer.Kind != Kind.FunctionDeclaration) && (this.thisContainer.Kind != Kind.FunctionExpression)))
                {
                    throw new InvalidOperationException(("Unhandled case in bindThisPropertyAssignment: " + this.thisContainer.Kind.String()));
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1144:1.
    private (Symbol, SymbolTable) getThisClassAndSymbolTable()
    {
        Symbol classSymbol = default;
        SymbolTable symbolTable = default;
        if ((this.thisContainer == null))
        {
            return (null, null);
        }

        switch (this.thisContainer.Kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            {
                break;
            }

            case Kind.Constructor:
            case Kind.PropertyDeclaration:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.ClassStaticBlockDeclaration:
            {
                classSymbol = this.thisContainer.Parent.Symbol();
                if (Ast.IsStatic(this.thisContainer))
                {
                    symbolTable = Ast.GetExports(classSymbol);
                }
                else
                {
                    symbolTable = Ast.GetMembers(classSymbol);
                }

                break;
            }
        }

        return (classSymbol, symbolTable);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1163:1.
    private void bindEnumDeclaration(Node node)
    {
        if (Ast.IsEnumConst(node))
        {
            this.bindBlockScopedDeclaration(node, SymbolFlags.ConstEnum, SymbolFlags.ConstEnumExcludes);
        }
        else
        {
            this.bindBlockScopedDeclaration(node, SymbolFlags.RegularEnum, SymbolFlags.RegularEnumExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1171:1.
    private void bindVariableDeclarationOrBindingElement(Node node)
    {
        this.checkStrictModeEvalOrArguments(node, node.Name());
        {
            var name = node.Name();
            if (((name != null) && !Ast.IsBindingPattern(name)))
            {
                switch (true)
                {
                    case true when Ast.IsVariableDeclarationInitializedToRequire(node):
                    {
                        this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.Alias, SymbolFlags.AliasExcludes);
                        break;
                    }

                    case true when Ast.IsBlockOrCatchScoped(node):
                    {
                        this.bindBlockScopedDeclaration(node, SymbolFlags.BlockScopedVariable, SymbolFlags.BlockScopedVariableExcludes);
                        break;
                    }

                    case true when Ast.IsPartOfParameterDeclaration(node):
                    {
                        this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.FunctionScopedVariable, SymbolFlags.ParameterExcludes);
                        break;
                    }

                    default:
                    {
                        this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.FunctionScopedVariable, SymbolFlags.FunctionScopedVariableExcludes);
                        break;
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1196:1.
    private void bindParameter(Node node)
    {
        var decl = node.AsParameterDeclaration();
        if (((node.Flags & NodeFlags.Ambient) == 0))
        {
            this.checkStrictModeEvalOrArguments(node, decl.Name());
        }

        if (Ast.IsBindingPattern(decl.Name()))
        {
            var index = GoSlices.Index(node.Parent.Parameters(), node);
            this.bindAnonymousDeclaration(node, SymbolFlags.FunctionScopedVariable, ("__" + Strconv.Itoa(index)));
        }
        else
        {
            this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.FunctionScopedVariable, SymbolFlags.ParameterExcludes);
        }

        if (Ast.IsParameterPropertyDeclaration(node, node.Parent))
        {
            var classDeclaration = node.Parent.Parent;
            var flags = (SymbolFlags.Property | Core.IfElse((decl.QuestionToken != null), SymbolFlags.Optional, SymbolFlags.None));
            this.declareSymbol(Ast.GetMembers(classDeclaration.Symbol()), classDeclaration.Symbol(), node, flags, SymbolFlags.PropertyExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1218:1.
    private void bindFunctionDeclaration(Node node)
    {
        if (((!this.file.IsDeclarationFile && ((node.Flags & NodeFlags.Ambient) == 0)) && Ast.IsAsyncFunction(node)))
        {
            this.emitFlags |= NodeFlags.HasAsyncFunctions;
        }

        this.checkStrictModeFunctionName(node);
        this.bindBlockScopedDeclaration(node, SymbolFlags.Function, SymbolFlags.FunctionExcludes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1226:1.
    private Node getInferTypeContainer(Node node)
    {
        var extendsType = Ast.FindAncestor(node, (n) =>
        {
            var parent = n.Parent;
            return (((parent != null) && Ast.IsConditionalTypeNode(parent)) && (parent.AsConditionalTypeNode().ExtendsType == n));
        });
        if ((extendsType != null))
        {
            return extendsType.Parent;
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1237:1.
    private void bindAnonymousDeclaration(Node node, SymbolFlags symbolFlags, string name)
    {
        var symbol = this.newSymbol(symbolFlags, name);
        if (((symbolFlags & ((SymbolFlags.EnumMember | SymbolFlags.ClassMember))) != 0))
        {
            symbol.Parent = this.container.Symbol();
        }

        this.addDeclarationToSymbol(symbol, node, symbolFlags);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1245:1.
    private void bindBlockScopedDeclaration(Node node, SymbolFlags symbolFlags, SymbolFlags symbolExcludes)
    {
        switch (this.blockScopeContainer.Kind)
        {
            case Kind.ModuleDeclaration:
            {
                this.declareModuleMember(node, symbolFlags, symbolExcludes);
                break;
            }

            case Kind.SourceFile:
            {
                if (Ast.IsExternalOrCommonJSModule(this.container.AsSourceFile()))
                {
                    this.declareModuleMember(node, symbolFlags, symbolExcludes);
                    break;
                }

                goto default;
            }

            default:
            {
                this.declareSymbol(Ast.GetLocals(this.blockScopeContainer), null, node, symbolFlags, symbolExcludes);
                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1260:1.
    private void bindTypeParameter(Node node)
    {
        if ((node.Parent.Kind == Kind.InferType))
        {
            var container = this.getInferTypeContainer(node.Parent);
            if ((container != null))
            {
                this.declareSymbol(Ast.GetLocals(container), null, node, SymbolFlags.TypeParameter, SymbolFlags.TypeParameterExcludes);
            }
            else
            {
                this.bindAnonymousDeclaration(node, SymbolFlags.TypeParameter, this.getDeclarationName(node));
            }
        }
        else
        {
            this.declareSymbolAndAddToSymbolTable(node, SymbolFlags.TypeParameter, SymbolFlags.TypeParameterExcludes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1273:1.
    private Symbol lookupEntity(Node node, Node container)
    {
        if (Ast.IsIdentifier(node))
        {
            return this.lookupName(node.Text(), container);
        }

        if ((node.Expression().Kind == Kind.ThisKeyword))
        {
            {
                var tuple5 = this.getThisClassAndSymbolTable();
                var symbolTable = tuple5.Item2;
                if ((symbolTable != null))
                {
                    {
                        var name = Ast.GetElementOrPropertyAccessName(node);
                        if ((name != null))
                        {
                            return symbolTable[name.Text()];
                        }
                    }
                }
            }

            return null;
        }

        {
            var symbol = getInitializerSymbol(this.lookupEntity(node.Expression(), container));
            if (((symbol != null) && (symbol.Exports != null)))
            {
                {
                    var name2 = Ast.GetElementOrPropertyAccessName(node);
                    if ((name2 != null))
                    {
                        return symbol.Exports[name2.Text()];
                    }
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1293:1.
    private Symbol lookupName(string name, Node container)
    {
        {
            var localsContainer = container.LocalsContainerData();
            if ((localsContainer != null))
            {
                {
                    var local = localsContainer.Locals?[name];
                    if ((local != null))
                    {
                        return Core.OrElse(local.ExportSymbol, local);
                    }
                }
            }
        }

        {
            var declaration = container.DeclarationData();
            if (((declaration != null) && (declaration.Symbol != null)))
            {
                return declaration.Symbol.Exports?[name];
            }
        }

        return null;
    }

    // The binder visits every node in the syntax tree so it is a convenient place to perform a single localized
    // check for reserved words used as identifiers in strict mode code, as well as `yield` or `await` in
    // [Yield] or [Await] contexts, respectively.
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1308:1.
    private void checkContextualIdentifier(Node node)
    {
        if (((((GoRuntime.Len(this.file.Diagnostics()) == 0) && ((node.Flags & NodeFlags.Ambient) == 0)) && ((node.Flags & NodeFlags.JSDoc) == 0)) && !Ast.IsIdentifierName(node)))
        {
            var originalKeywordKind = Scanner.GetIdentifierToken(node.Text());
            if ((originalKeywordKind == Kind.Identifier))
            {
                return;
            }

            if (((originalKeywordKind >= Kind.FirstFutureReservedWord) && (originalKeywordKind <= Kind.LastFutureReservedWord)))
            {
                this.errorOnNode(node, this.getStrictModeIdentifierMessage(node), Scanner.DeclarationNameToStringByteString(node));
            }
            else
            {
                if ((originalKeywordKind == Kind.AwaitKeyword))
                {
                    if ((Ast.IsExternalModule(this.file) && Ast.IsInTopLevelContext(node)))
                    {
                        this.errorOnNode(node, Diagnostics.Identifier_expected_0_is_a_reserved_word_at_the_top_level_of_a_module, Scanner.DeclarationNameToStringByteString(node));
                    }
                    else
                    {
                        if (((node.Flags & NodeFlags.AwaitContext) != 0))
                        {
                            this.errorOnNode(node, Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here, Scanner.DeclarationNameToStringByteString(node));
                        }
                    }
                }
                else
                {
                    if (((originalKeywordKind == Kind.YieldKeyword) && ((node.Flags & NodeFlags.YieldContext) != 0)))
                    {
                        this.errorOnNode(node, Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here, Scanner.DeclarationNameToStringByteString(node));
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1330:1.
    private void checkPrivateIdentifier(Node node)
    {
        if ((node.Text() == "#constructor"))
        {
            if ((GoRuntime.Len(this.file.Diagnostics()) == 0))
            {
                this.errorOnNode(node, Diagnostics.X_constructor_is_a_reserved_word, Scanner.DeclarationNameToStringByteString(node));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1339:1.
    private Message getStrictModeIdentifierMessage(Node node)
    {
        if ((Ast.GetContainingClass(node) != null))
        {
            return Diagnostics.Identifier_expected_0_is_a_reserved_word_in_strict_mode_Class_definitions_are_automatically_in_strict_mode;
        }

        if ((this.file.ExternalModuleIndicator != null))
        {
            return Diagnostics.Identifier_expected_0_is_a_reserved_word_in_strict_mode_Modules_are_automatically_in_strict_mode;
        }

        return Diagnostics.Identifier_expected_0_is_a_reserved_word_in_strict_mode;
    }

    // Should be called only on prologue directives (ast.IsPrologueDirective(node) should be true)
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1352:1.
    private static bool isUseStrictPrologueDirective(SourceFile sourceFile, Node node)
    {
        var nodeText = Scanner.GetSourceTextOfNodeFromSourceFileByteString(sourceFile, node.Expression(), false);
        return ((nodeText == "\"use strict\"") || (nodeText == "'use strict'"));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1359:1.
    public static Node FindUseStrictPrologue(SourceFile sourceFile, List<Node> statements)
    {
        {
            foreach (var statement in GoRuntime.RangeValues(statements))
            {
                if (Ast.IsPrologueDirective(statement))
                {
                    if (isUseStrictPrologueDirective(sourceFile, statement))
                    {
                        return statement;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1373:1.
    private void checkStrictModeFunctionName(Node node)
    {
        if (((node.Flags & NodeFlags.Ambient) == 0))
        {
            this.checkStrictModeEvalOrArguments(node, node.Name());
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1380:1.
    private Message getStrictModeBlockScopeFunctionDeclarationMessage(Node node)
    {
        if ((Ast.GetContainingClass(node) != null))
        {
            return Diagnostics.Function_declarations_are_not_allowed_inside_blocks_in_strict_mode_when_targeting_ES5_Class_definitions_are_automatically_in_strict_mode;
        }

        if ((this.file.ExternalModuleIndicator != null))
        {
            return Diagnostics.Function_declarations_are_not_allowed_inside_blocks_in_strict_mode_when_targeting_ES5_Modules_are_automatically_in_strict_mode;
        }

        return Diagnostics.Function_declarations_are_not_allowed_inside_blocks_in_strict_mode_when_targeting_ES5;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1391:1.
    private void checkStrictModeBinaryExpression(Node node)
    {
        var expr = node.AsBinaryExpression();
        if ((Ast.IsLeftHandSideExpression(expr.Left) && Ast.IsAssignmentOperator(expr.OperatorToken.Kind)))
        {
            this.checkStrictModeEvalOrArguments(node, expr.Left);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1400:1.
    private void checkStrictModeCatchClause(Node node)
    {
        var clause = node.AsCatchClause();
        if ((clause.VariableDeclaration != null))
        {
            this.checkStrictModeEvalOrArguments(node, clause.VariableDeclaration.AsVariableDeclaration().Name());
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1409:1.
    private void checkStrictModeDeleteExpression(Node node)
    {
        var expr = node.AsDeleteExpression();
        if ((expr.Expression.Kind == Kind.Identifier))
        {
            this.errorOnNode(expr.Expression, Diagnostics.X_delete_cannot_be_called_on_an_identifier_in_strict_mode);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1419:1.
    private void checkStrictModePostfixUnaryExpression(Node node)
    {
        this.checkStrictModeEvalOrArguments(node, node.AsPostfixUnaryExpression().Operand);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1427:1.
    private void checkStrictModePrefixUnaryExpression(Node node)
    {
        var expr = node.AsPrefixUnaryExpression();
        if (((expr.Operator == Kind.PlusPlusToken) || (expr.Operator == Kind.MinusMinusToken)))
        {
            this.checkStrictModeEvalOrArguments(node, expr.Operand);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1435:1.
    private void checkStrictModeWithStatement(Node node)
    {
        this.errorOnFirstToken(node, Diagnostics.X_with_statements_are_not_allowed_in_strict_mode);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1440:1.
    private void checkStrictModeLabeledStatement(Node node)
    {
        var data = node.AsLabeledStatement();
        if ((Ast.IsDeclarationStatement(data.Statement) || Ast.IsVariableStatement(data.Statement)))
        {
            this.errorOnFirstToken(data.Label, Diagnostics.A_label_is_not_allowed_here);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1448:1.
    private static bool isEvalOrArgumentsIdentifier(Node node)
    {
        if (Ast.IsIdentifier(node))
        {
            var text = node.Text();
            return ((text == "eval") || (text == "arguments"));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1456:1.
    private void checkStrictModeEvalOrArguments(Node contextNode, Node name)
    {
        if (((name != null) && isEvalOrArgumentsIdentifier(name)))
        {
            this.errorOnNode(name, this.getStrictModeEvalOrArgumentsMessage(contextNode), name.Text());
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1464:1.
    private Message getStrictModeEvalOrArgumentsMessage(Node node)
    {
        if ((Ast.GetContainingClass(node) != null))
        {
            return Diagnostics.Code_contained_in_a_class_is_evaluated_in_JavaScript_s_strict_mode_which_does_not_allow_this_use_of_0_For_more_information_see_https_Colon_Slash_Slashdeveloper_mozilla_org_Slashen_US_Slashdocs_SlashWeb_SlashJavaScript_SlashReference_SlashStrict_mode;
        }

        if ((this.file.ExternalModuleIndicator != null))
        {
            return Diagnostics.Invalid_use_of_0_Modules_are_automatically_in_strict_mode;
        }

        return Diagnostics.Invalid_use_of_0_in_strict_mode;
    }

    // All container nodes are kept on a linked list in declaration order. This list is used by
    // the getLocalNameOfContainer function in the type checker to validate that the local name
    // used for a container is unique.
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1478:1.
    private void bindContainer(Node node, ContainerFlags containerFlags)
    {
        var saveContainer = this.container;
        var saveThisContainer = this.thisContainer;
        var savedBlockScopeContainer = this.blockScopeContainer;
        if (((containerFlags & ContainerFlags.IsContainer) != 0))
        {
            this.container = node;
            this.blockScopeContainer = node;
            if (((containerFlags & ContainerFlags.HasLocals) != 0))
            {
                this.addToContainerChain(node);
            }
        }
        else
        {
            if (((containerFlags & ContainerFlags.IsBlockScopedContainer) != 0))
            {
                this.blockScopeContainer = node;
                this.addToContainerChain(node);
            }
        }

        if (((containerFlags & ContainerFlags.IsThisContainer) != 0))
        {
            this.thisContainer = node;
        }

        if (((containerFlags & ContainerFlags.IsControlFlowContainer) != 0))
        {
            var saveCurrentFlow = this.currentFlow;
            var saveBreakTarget = this.currentBreakTarget;
            var saveContinueTarget = this.currentContinueTarget;
            var saveReturnTarget = this.currentReturnTarget;
            var saveExceptionTarget = this.currentExceptionTarget;
            var saveActiveLabelList = this.activeLabelList;
            var saveHasExplicitReturn = this.hasExplicitReturn;
            var saveSeenThisKeyword = this.seenThisKeyword;
            var isImmediatelyInvoked = (((((((containerFlags & ContainerFlags.IsFunctionExpression) != 0) && !Ast.HasSyntacticModifier(node, ModifierFlags.Async)) && !isGeneratorFunctionExpression(node)) && (Ast.GetImmediatelyInvokedFunctionExpression(node) != null))) || (node.Kind == Kind.ClassStaticBlockDeclaration));
            if (!isImmediatelyInvoked)
            {
                var flowStart = this.newFlowNode(FlowFlags.Start);
                this.currentFlow = flowStart;
                if (((containerFlags & ((ContainerFlags.IsFunctionExpression | ContainerFlags.IsObjectLiteralOrClassExpressionMethodOrAccessor))) != 0))
                {
                    flowStart.Node = node;
                }
            }

            if ((isImmediatelyInvoked || (node.Kind == Kind.Constructor)))
            {
                this.currentReturnTarget = this.newFlowNode(FlowFlags.BranchLabel);
            }
            else
            {
                this.currentReturnTarget = null;
            }

            this.currentExceptionTarget = null;
            this.currentBreakTarget = null;
            this.currentContinueTarget = null;
            this.activeLabelList = null;
            this.hasExplicitReturn = false;
            this.seenThisKeyword = false;
            this.bindChildren(node);
            node.Flags &= ~((NodeFlags.ReachabilityAndEmitFlags | NodeFlags.ContainsThis));
            if ((((this.currentFlow.Flags & FlowFlags.Unreachable) == 0) && ((containerFlags & ContainerFlags.IsFunctionLike) != 0)))
            {
                var bodyData = node.BodyData();
                if (((bodyData != null) && Ast.NodeIsPresent(bodyData.Body)))
                {
                    node.Flags |= NodeFlags.HasImplicitReturn;
                    if (this.hasExplicitReturn)
                    {
                        node.Flags |= NodeFlags.HasExplicitReturn;
                    }

                    bodyData.EndFlowNode = this.currentFlow;
                }
            }

            if (this.seenThisKeyword)
            {
                node.Flags |= NodeFlags.ContainsThis;
            }

            if ((node.Kind == Kind.SourceFile))
            {
                node.Flags |= this.emitFlags;
            }

            if ((this.currentReturnTarget != null))
            {
                this.addAntecedent(this.currentReturnTarget, this.currentFlow);
                this.currentFlow = this.finishFlowLabel(this.currentReturnTarget);
                if (((node.Kind == Kind.Constructor) || (node.Kind == Kind.ClassStaticBlockDeclaration)))
                {
                    setReturnFlowNode(node, this.currentFlow);
                }
            }

            if (!isImmediatelyInvoked)
            {
                this.currentFlow = saveCurrentFlow;
            }

            this.currentBreakTarget = saveBreakTarget;
            this.currentContinueTarget = saveContinueTarget;
            this.currentReturnTarget = saveReturnTarget;
            this.currentExceptionTarget = saveExceptionTarget;
            this.activeLabelList = saveActiveLabelList;
            this.hasExplicitReturn = saveHasExplicitReturn;
            if (((containerFlags & ContainerFlags.PropagatesThisKeyword) != 0))
            {
                this.seenThisKeyword = (saveSeenThisKeyword || this.seenThisKeyword);
            }
            else
            {
                this.seenThisKeyword = saveSeenThisKeyword;
            }
        }
        else
        {
            if (((containerFlags & ContainerFlags.IsInterface) != 0))
            {
                var saveSeenThisKeyword2 = this.seenThisKeyword;
                this.seenThisKeyword = false;
                this.bindChildren(node);
                if (this.seenThisKeyword)
                {
                    node.Flags |= NodeFlags.ContainsThis;
                }
                else
                {
                    node.Flags &= ~(NodeFlags.ContainsThis);
                }

                this.seenThisKeyword = saveSeenThisKeyword2;
            }
            else
            {
                this.bindChildren(node);
            }
        }

        if ((Ast.IsSourceFile(node) && Ast.IsInJSFile(node)))
        {
            {
                foreach (var statement in GoRuntime.RangeValues(node.Statements()))
                {
                    if (Ast.IsJSTypeAliasDeclaration(statement))
                    {
                        this.bindBlockScopedDeclaration(statement, SymbolFlags.TypeAlias, SymbolFlags.TypeAliasExcludes);
                    }
                }
            }

            if ((this.file.CommonJSModuleIndicator != null))
            {
                this.declareCommonJSVariable("module");
                this.declareCommonJSVariable("exports");
            }
        }

        if (((Ast.IsSourceFile(node) && Ast.IsExternalOrCommonJSModule(node.AsSourceFile())) || Ast.IsAmbientModule(node)))
        {
            this.bindCommonJSTypeExports(node.Symbol());
        }

        this.container = saveContainer;
        this.thisContainer = saveThisContainer;
        this.blockScopeContainer = savedBlockScopeContainer;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1627:1.
    private void declareCommonJSVariable(string name)
    {
        var locals = Ast.GetLocals(this.file.AsNode());
        if ((locals[name] == null))
        {
            var symbol = this.newSymbol((SymbolFlags.FunctionScopedVariable | SymbolFlags.ModuleExports), name);
            symbol.Declarations = this.newSingleDeclaration(this.file.AsNode());
            symbol.ValueDeclaration = symbol.Declarations[0];
            if ((name == "module"))
            {
                var exportsProperty = this.newSymbol((SymbolFlags.ModuleExports | SymbolFlags.Property), "exports");
                exportsProperty.Declarations = symbol.Declarations;
                exportsProperty.ValueDeclaration = symbol.ValueDeclaration;
                exportsProperty.Parent = symbol;
                symbol.Members = new SymbolTable();
                symbol.Members["exports"] = exportsProperty;
            }

            locals[name] = symbol;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1645:1.
    private void bindChildren(Node node)
    {
        var saveInAssignmentPattern = this.inAssignmentPattern;
        this.inAssignmentPattern = false;
        if ((this.currentFlow == this.unreachableFlow))
        {
            {
                var flowNodeData = node.FlowNodeData();
                if ((flowNodeData != null))
                {
                    flowNodeData.FlowNode = null;
                }
            }

            if (Ast.IsPotentiallyExecutableNode(node))
            {
                node.Flags |= NodeFlags.Unreachable;
            }

            this.bindEachChild(node);
            this.inAssignmentPattern = saveInAssignmentPattern;
            return;
        }

        if (((Kind.FirstStatement <= node.Kind) && (node.Kind <= Kind.LastStatement)))
        {
            {
                var flowNodeData2 = node.FlowNodeData();
                if ((flowNodeData2 != null))
                {
                    flowNodeData2.FlowNode = this.currentFlow;
                }
            }
        }

        switch (node.Kind)
        {
            case Kind.WhileStatement:
            {
                this.bindWhileStatement(node);
                break;
            }

            case Kind.DoStatement:
            {
                this.bindDoStatement(node);
                break;
            }

            case Kind.ForStatement:
            {
                this.bindForStatement(node);
                break;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                this.bindForInOrForOfStatement(node);
                break;
            }

            case Kind.IfStatement:
            {
                this.bindIfStatement(node);
                break;
            }

            case Kind.ReturnStatement:
            {
                this.bindReturnStatement(node);
                break;
            }

            case Kind.ThrowStatement:
            {
                this.bindThrowStatement(node);
                break;
            }

            case Kind.BreakStatement:
            {
                this.bindBreakStatement(node);
                break;
            }

            case Kind.ContinueStatement:
            {
                this.bindContinueStatement(node);
                break;
            }

            case Kind.TryStatement:
            {
                this.bindTryStatement(node);
                break;
            }

            case Kind.SwitchStatement:
            {
                this.bindSwitchStatement(node);
                break;
            }

            case Kind.CaseBlock:
            {
                this.bindCaseBlock(node);
                break;
            }

            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                this.bindCaseOrDefaultClause(node);
                break;
            }

            case Kind.ExpressionStatement:
            {
                this.bindExpressionStatement(node);
                break;
            }

            case Kind.LabeledStatement:
            {
                this.bindLabeledStatement(node);
                break;
            }

            case Kind.PrefixUnaryExpression:
            {
                this.bindPrefixUnaryExpressionFlow(node);
                break;
            }

            case Kind.PostfixUnaryExpression:
            {
                this.bindPostfixUnaryExpressionFlow(node);
                break;
            }

            case Kind.BinaryExpression:
            {
                if (Ast.IsDestructuringAssignment(node))
                {
                    this.inAssignmentPattern = saveInAssignmentPattern;
                    this.bindDestructuringAssignmentFlow(node);
                    return;
                }

                this.bindBinaryExpressionFlow(node);
                break;
            }

            case Kind.DeleteExpression:
            {
                this.bindDeleteExpressionFlow(node);
                break;
            }

            case Kind.ConditionalExpression:
            {
                this.bindConditionalExpressionFlow(node);
                break;
            }

            case Kind.VariableDeclaration:
            {
                this.bindVariableDeclarationFlow(node);
                break;
            }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                this.bindAccessExpressionFlow(node);
                break;
            }

            case Kind.CallExpression:
            {
                this.bindCallExpressionFlow(node);
                break;
            }

            case Kind.NonNullExpression:
            {
                this.bindNonNullExpressionFlow(node);
                break;
            }

            case Kind.SourceFile:
            {
                var sourceFile = node.AsSourceFile();
                this.bindEachStatementFunctionsFirst(sourceFile.Statements);
                this.bind(sourceFile.EndOfFileToken);
                break;
            }

            case Kind.Block:
            case Kind.ModuleBlock:
            {
                this.bindEachStatementFunctionsFirst(node.StatementList());
                break;
            }

            case Kind.BindingElement:
            {
                this.bindBindingElementFlow(node);
                break;
            }

            case Kind.Parameter:
            {
                this.bindParameterFlow(node);
                break;
            }

            case Kind.ObjectLiteralExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.PropertyAssignment:
            case Kind.SpreadElement:
            {
                this.inAssignmentPattern = saveInAssignmentPattern;
                this.bindEachChild(node);
                break;
            }

            default:
            {
                this.bindEachChild(node);
                break;
            }
        }

        this.inAssignmentPattern = saveInAssignmentPattern;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1744:1.
    private void bindEachChild(Node node)
    {
        node.ForEachChild(this.bindFunc);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1748:1.
    private void bindEach(List<Node> nodes)
    {
        {
            foreach (var node in GoRuntime.RangeValues(nodes))
            {
                this.bind(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1754:1.
    private void bindNodeList(NodeList nodeList)
    {
        if ((nodeList != null))
        {
            this.bindEach(nodeList.Nodes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1760:1.
    private void bindModifiers(ModifierList modifiers)
    {
        if ((modifiers != null))
        {
            this.bindEach(modifiers.Nodes);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1766:1.
    private void bindEachStatementFunctionsFirst(NodeList statements)
    {
        {
            foreach (var node in GoRuntime.RangeValues(statements.Nodes))
            {
                if ((node.Kind == Kind.FunctionDeclaration))
                {
                    this.bind(node);
                }
            }
        }

        {
            foreach (var range10 in GoRuntime.Range(statements.Nodes))
            {
                var _2 = range10.Key;
                var node2 = range10.Value;
                if ((node2.Kind != Kind.FunctionDeclaration))
                {
                    this.bind(node2);
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1779:1.
    private FlowNode setContinueTarget(Node node, FlowNode target)
    {
        var label = this.activeLabelList;
        while (((label != null) && (node.Parent.Kind == Kind.LabeledStatement)))
        {
            label.continueTarget = target;
            label = label.next;
            node = node.Parent;
        }

        return target;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1789:1.
    private void doWithConditionalBranches(Func<Binder, Node, bool> action, Node value2, FlowNode trueTarget, FlowNode falseTarget)
    {
        var savedTrueTarget = this.currentTrueTarget;
        var savedFalseTarget = this.currentFalseTarget;
        this.currentTrueTarget = trueTarget;
        this.currentFalseTarget = falseTarget;
        action(this, value2);
        this.currentTrueTarget = savedTrueTarget;
        this.currentFalseTarget = savedFalseTarget;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1799:1.
    private void bindCondition(Node node, FlowNode trueTarget, FlowNode falseTarget)
    {
        this.doWithConditionalBranches((binderCallback, argnode) => binderCallback.bind(argnode), node, trueTarget, falseTarget);
        if (((node == null) || ((!isLogicalAssignmentExpression(node) && !Ast.IsLogicalExpression(node)) && !((Ast.IsOptionalChain(node) && Ast.IsOutermostOptionalChain(node))))))
        {
            this.addAntecedent(trueTarget, this.createFlowCondition(FlowFlags.TrueCondition, this.currentFlow, node));
            this.addAntecedent(falseTarget, this.createFlowCondition(FlowFlags.FalseCondition, this.currentFlow, node));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1807:1.
    private void bindIterativeStatement(Node node, FlowNode breakTarget, FlowNode continueTarget)
    {
        var saveBreakTarget = this.currentBreakTarget;
        var saveContinueTarget = this.currentContinueTarget;
        this.currentBreakTarget = breakTarget;
        this.currentContinueTarget = continueTarget;
        this.bind(node);
        this.currentBreakTarget = saveBreakTarget;
        this.currentContinueTarget = saveContinueTarget;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1817:1.
    private static bool isLogicalAssignmentExpression(Node node)
    {
        return Ast.IsLogicalOrCoalescingAssignmentExpression(Ast.SkipParentheses(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1821:1.
    private void bindAssignmentTargetFlow(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ArrayLiteralExpression:
            {
                {
                    foreach (var e in GoRuntime.RangeValues(node.Elements()))
                    {
                        if ((e.Kind == Kind.SpreadElement))
                        {
                            this.bindAssignmentTargetFlow(e.Expression());
                        }
                        else
                        {
                            this.bindDestructuringTargetFlow(e);
                        }
                    }
                }

                break;
            }

            case Kind.ObjectLiteralExpression:
            {
                {
                    foreach (var range12 in GoRuntime.Range(node.Properties()))
                    {
                        var _2 = range12.Key;
                        var p = range12.Value;
                        switch (p.Kind)
                        {
                            case Kind.PropertyAssignment:
                            {
                                this.bindDestructuringTargetFlow(p.Initializer());
                                break;
                            }

                            case Kind.ShorthandPropertyAssignment:
                            {
                                this.bindAssignmentTargetFlow(p.AsShorthandPropertyAssignment().Name());
                                break;
                            }

                            case Kind.SpreadAssignment:
                            {
                                this.bindAssignmentTargetFlow(p.Expression());
                                break;
                            }
                        }
                    }
                }

                break;
            }

            default:
            {
                if (isNarrowableReference(node))
                {
                    this.currentFlow = this.createFlowMutation(FlowFlags.Assignment, this.currentFlow, node);
                }

                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1849:1.
    private void bindDestructuringTargetFlow(Node node)
    {
        if ((Ast.IsBinaryExpression(node) && (node.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken)))
        {
            this.bindAssignmentTargetFlow(node.AsBinaryExpression().Left);
        }
        else
        {
            this.bindAssignmentTargetFlow(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1857:1.
    private void bindWhileStatement(Node node)
    {
        var stmt = node.AsWhileStatement();
        var preWhileLabel = this.setContinueTarget(node, this.createLoopLabel());
        var preBodyLabel = this.createBranchLabel();
        var postWhileLabel = this.createBranchLabel();
        this.addAntecedent(preWhileLabel, this.currentFlow);
        this.currentFlow = preWhileLabel;
        this.bindCondition(stmt.Expression, preBodyLabel, postWhileLabel);
        this.currentFlow = this.finishFlowLabel(preBodyLabel);
        this.bindIterativeStatement(stmt.Statement, postWhileLabel, preWhileLabel);
        this.addAntecedent(preWhileLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(postWhileLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1871:1.
    private void bindDoStatement(Node node)
    {
        var stmt = node.AsDoStatement();
        var preDoLabel = this.createLoopLabel();
        var preConditionLabel = this.setContinueTarget(node, this.createBranchLabel());
        var postDoLabel = this.createBranchLabel();
        this.addAntecedent(preDoLabel, this.currentFlow);
        this.currentFlow = preDoLabel;
        this.bindIterativeStatement(stmt.Statement, postDoLabel, preConditionLabel);
        this.addAntecedent(preConditionLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(preConditionLabel);
        this.bindCondition(stmt.Expression, preDoLabel, postDoLabel);
        this.currentFlow = this.finishFlowLabel(postDoLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1885:1.
    private void bindForStatement(Node node)
    {
        var stmt = node.AsForStatement();
        this.bind(stmt.Initializer);
        if ((this.currentFlow == this.unreachableFlow))
        {
            this.bind(stmt.Condition);
            this.bind(stmt.Statement);
            this.bind(stmt.Incrementor);
            return;
        }

        var preLoopLabel = this.setContinueTarget(node, this.createLoopLabel());
        var preBodyLabel = this.createBranchLabel();
        var preIncrementorLabel = this.createBranchLabel();
        var postLoopLabel = this.createBranchLabel();
        this.addAntecedent(preLoopLabel, this.currentFlow);
        this.currentFlow = preLoopLabel;
        this.bindCondition(stmt.Condition, preBodyLabel, postLoopLabel);
        this.currentFlow = this.finishFlowLabel(preBodyLabel);
        this.bindIterativeStatement(stmt.Statement, postLoopLabel, preIncrementorLabel);
        this.addAntecedent(preIncrementorLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(preIncrementorLabel);
        this.bind(stmt.Incrementor);
        this.addAntecedent(preLoopLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(postLoopLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1915:1.
    private void bindForInOrForOfStatement(Node node)
    {
        var stmt = node.AsForInOrOfStatement();
        this.bind(stmt.Expression);
        if ((this.currentFlow == this.unreachableFlow))
        {
            this.bind(stmt.Initializer);
            this.bind(stmt.Statement);
            return;
        }

        var preLoopLabel = this.setContinueTarget(node, this.createLoopLabel());
        var postLoopLabel = this.createBranchLabel();
        this.addAntecedent(preLoopLabel, this.currentFlow);
        this.currentFlow = preLoopLabel;
        if ((node.Kind == Kind.ForOfStatement))
        {
            this.bind(stmt.AwaitModifier);
        }

        this.addAntecedent(postLoopLabel, this.currentFlow);
        this.bind(stmt.Initializer);
        if ((stmt.Initializer.Kind != Kind.VariableDeclarationList))
        {
            this.bindAssignmentTargetFlow(stmt.Initializer);
        }

        this.bindIterativeStatement(stmt.Statement, postLoopLabel, preLoopLabel);
        this.addAntecedent(preLoopLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(postLoopLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1945:1.
    private void bindIfStatement(Node node)
    {
        var stmt = node.AsIfStatement();
        var thenLabel = this.createBranchLabel();
        var elseLabel = this.createBranchLabel();
        var postIfLabel = this.createBranchLabel();
        this.bindCondition(stmt.Expression, thenLabel, elseLabel);
        this.currentFlow = this.finishFlowLabel(thenLabel);
        this.bind(stmt.ThenStatement);
        this.addAntecedent(postIfLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(elseLabel);
        this.bind(stmt.ElseStatement);
        this.addAntecedent(postIfLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(postIfLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1960:1.
    private void bindReturnStatement(Node node)
    {
        this.bind(node.Expression());
        if ((this.currentReturnTarget != null))
        {
            this.addAntecedent(this.currentReturnTarget, this.currentFlow);
        }

        this.currentFlow = this.unreachableFlow;
        this.hasExplicitReturn = true;
        this.hasFlowEffects = true;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1970:1.
    private void bindThrowStatement(Node node)
    {
        this.bind(node.Expression());
        this.currentFlow = this.unreachableFlow;
        this.hasFlowEffects = true;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1976:1.
    private void bindBreakStatement(Node node)
    {
        this.bindBreakOrContinueStatement(node.Label(), this.currentBreakTarget, (binderCallback) => binderCallback.BreakTarget());
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1980:1.
    private void bindContinueStatement(Node node)
    {
        this.bindBreakOrContinueStatement(node.Label(), this.currentContinueTarget, (binderCallback) => binderCallback.ContinueTarget());
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1984:1.
    private void bindBreakOrContinueStatement(Node label, FlowNode currentTarget, Func<ActiveLabel, FlowNode> getTarget)
    {
        this.bind(label);
        if ((label != null))
        {
            var activeLabel = this.findActiveLabel(label.Text());
            if ((activeLabel != null))
            {
                activeLabel.referenced = true;
                this.bindBreakOrContinueFlow(getTarget(activeLabel));
            }
        }
        else
        {
            this.bindBreakOrContinueFlow(currentTarget);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:1997:1.
    private ActiveLabel findActiveLabel(string name)
    {
        {
            var label = this.activeLabelList;
            for (; (label != null); label = label.next)
            {
                if ((label.name == name))
                {
                    return label;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2006:1.
    private void bindBreakOrContinueFlow(FlowNode flowLabel)
    {
        if ((flowLabel != null))
        {
            this.addAntecedent(flowLabel, this.currentFlow);
            this.currentFlow = this.unreachableFlow;
            this.hasFlowEffects = true;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2014:1.
    private void bindTryStatement(Node node)
    {
        var stmt = node.AsTryStatement();
        var saveReturnTarget = this.currentReturnTarget;
        var saveExceptionTarget = this.currentExceptionTarget;
        var normalExitLabel = this.createBranchLabel();
        var returnLabel = this.createBranchLabel();
        var exceptionLabel = this.createBranchLabel();
        if ((stmt.FinallyBlock != null))
        {
            this.currentReturnTarget = returnLabel;
        }

        this.addAntecedent(exceptionLabel, this.currentFlow);
        this.currentExceptionTarget = exceptionLabel;
        this.bind(stmt.TryBlock);
        this.addAntecedent(normalExitLabel, this.currentFlow);
        if ((stmt.CatchClause != null))
        {
            this.currentFlow = this.finishFlowLabel(exceptionLabel);
            exceptionLabel = this.createBranchLabel();
            this.addAntecedent(exceptionLabel, this.currentFlow);
            this.currentExceptionTarget = exceptionLabel;
            this.bind(stmt.CatchClause);
            this.addAntecedent(normalExitLabel, this.currentFlow);
        }

        this.currentReturnTarget = saveReturnTarget;
        this.currentExceptionTarget = saveExceptionTarget;
        if ((stmt.FinallyBlock != null))
        {
            var finallyLabel = this.createBranchLabel();
            finallyLabel.Antecedents = this.combineFlowLists(normalExitLabel.Antecedents, this.combineFlowLists(exceptionLabel.Antecedents, returnLabel.Antecedents));
            this.currentFlow = finallyLabel;
            this.bind(stmt.FinallyBlock);
            if (((this.currentFlow.Flags & FlowFlags.Unreachable) != 0))
            {
                this.currentFlow = this.unreachableFlow;
            }
            else
            {
                if (((this.currentReturnTarget != null) && (returnLabel.Antecedents != null)))
                {
                    this.addAntecedent(this.currentReturnTarget, this.createReduceLabel(finallyLabel, returnLabel.Antecedents, this.currentFlow));
                }

                if (((this.currentExceptionTarget != null) && (exceptionLabel.Antecedents != null)))
                {
                    this.addAntecedent(this.currentExceptionTarget, this.createReduceLabel(finallyLabel, exceptionLabel.Antecedents, this.currentFlow));
                }

                if ((normalExitLabel.Antecedents != null))
                {
                    this.currentFlow = this.createReduceLabel(finallyLabel, normalExitLabel.Antecedents, this.currentFlow);
                }
                else
                {
                    this.currentFlow = this.unreachableFlow;
                }
            }
        }
        else
        {
            this.currentFlow = this.finishFlowLabel(normalExitLabel);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2095:1.
    private void bindSwitchStatement(Node node)
    {
        var stmt = node.AsSwitchStatement();
        var postSwitchLabel = this.createBranchLabel();
        this.bind(stmt.Expression);
        var saveBreakTarget = this.currentBreakTarget;
        var savePreSwitchCaseFlow = this.preSwitchCaseFlow;
        this.currentBreakTarget = postSwitchLabel;
        this.preSwitchCaseFlow = this.currentFlow;
        this.bind(stmt.CaseBlock);
        this.addAntecedent(postSwitchLabel, this.currentFlow);
        var hasDefault = Core.Some(stmt.CaseBlock.AsCaseBlock().Clauses.Nodes, (c) =>
        {
            return (c.Kind == Kind.DefaultClause);
        });
        if (!hasDefault)
        {
            this.addAntecedent(postSwitchLabel, this.createFlowSwitchClause(this.preSwitchCaseFlow, node, 0, 0));
        }

        this.currentBreakTarget = saveBreakTarget;
        this.preSwitchCaseFlow = savePreSwitchCaseFlow;
        this.currentFlow = this.finishFlowLabel(postSwitchLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2116:1.
    private void bindCaseBlock(Node node)
    {
        var switchStatement = node.Parent;
        var clauses = node.AsCaseBlock().Clauses.Nodes;
        var isNarrowingSwitch = ((switchStatement.Expression().Kind == Kind.TrueKeyword) || isNarrowingExpression(switchStatement.Expression()));
        FlowNode fallthroughFlow = this.unreachableFlow;
        {
            var i = 0;
            for (; (i < GoRuntime.Len(clauses)); i++)
            {
                var clauseStart = i;
                while (((GoRuntime.Len(clauses[i].Statements()) == 0) && ((i + 1) < GoRuntime.Len(clauses))))
                {
                    if ((fallthroughFlow == this.unreachableFlow))
                    {
                        this.currentFlow = this.preSwitchCaseFlow;
                    }

                    this.bind(clauses[i]);
                    i++;
                }

                var preCaseLabel = this.createBranchLabel();
                var preCaseFlow = this.preSwitchCaseFlow;
                if (isNarrowingSwitch)
                {
                    preCaseFlow = this.createFlowSwitchClause(this.preSwitchCaseFlow, switchStatement, clauseStart, (i + 1));
                }

                this.addAntecedent(preCaseLabel, preCaseFlow);
                this.addAntecedent(preCaseLabel, fallthroughFlow);
                this.currentFlow = this.finishFlowLabel(preCaseLabel);
                var clause = clauses[i];
                this.bind(clause);
                fallthroughFlow = this.currentFlow;
                if ((((this.currentFlow.Flags & FlowFlags.Unreachable) == 0) && (i != (GoRuntime.Len(clauses) - 1))))
                {
                    clause.AsCaseOrDefaultClause().FallthroughFlowNode = this.currentFlow;
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2147:1.
    private void bindCaseOrDefaultClause(Node node)
    {
        var clause = node.AsCaseOrDefaultClause();
        if ((clause.Expression != null))
        {
            var saveCurrentFlow = this.currentFlow;
            this.currentFlow = this.preSwitchCaseFlow;
            this.bind(clause.Expression);
            this.currentFlow = saveCurrentFlow;
        }

        this.bindEach(clause.Statements.Nodes);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2158:1.
    private void bindExpressionStatement(Node node)
    {
        var stmt = node.AsExpressionStatement();
        this.bind(stmt.Expression);
        this.maybeBindExpressionFlowIfCall(stmt.Expression);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2164:1.
    private void maybeBindExpressionFlowIfCall(Node node)
    {
        if (Ast.IsCallExpression(node))
        {
            if (((node.Expression().Kind != Kind.SuperKeyword) && Ast.IsDottedName(node.Expression())))
            {
                this.currentFlow = this.createFlowCall(this.currentFlow, node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2174:1.
    private void bindLabeledStatement(Node node)
    {
        var stmt = node.AsLabeledStatement();
        var postStatementLabel = this.createBranchLabel();
        this.activeLabelList = new ActiveLabel
        {
            next = this.activeLabelList,
            name = stmt.Label.Text(),
            breakTarget = postStatementLabel,
            continueTarget = null,
            referenced = false
        };
        this.bind(stmt.Label);
        this.bind(stmt.Statement);
        if (!this.activeLabelList.referenced)
        {
            stmt.Label.Flags |= NodeFlags.Unreachable;
        }

        this.activeLabelList = this.activeLabelList.next;
        this.addAntecedent(postStatementLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(postStatementLabel);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2195:1.
    private void bindPrefixUnaryExpressionFlow(Node node)
    {
        var expr = node.AsPrefixUnaryExpression();
        if ((expr.Operator == Kind.ExclamationToken))
        {
            var saveTrueTarget = this.currentTrueTarget;
            this.currentTrueTarget = this.currentFalseTarget;
            this.currentFalseTarget = saveTrueTarget;
            this.bindEachChild(node);
            this.currentFalseTarget = this.currentTrueTarget;
            this.currentTrueTarget = saveTrueTarget;
        }
        else
        {
            this.bindEachChild(node);
            if (((expr.Operator == Kind.PlusPlusToken) || (expr.Operator == Kind.MinusMinusToken)))
            {
                this.bindAssignmentTargetFlow(expr.Operand);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2212:1.
    private void bindPostfixUnaryExpressionFlow(Node node)
    {
        var expr = node.AsPostfixUnaryExpression();
        this.bindEachChild(node);
        if (((expr.Operator == Kind.PlusPlusToken) || (expr.Operator == Kind.MinusMinusToken)))
        {
            this.bindAssignmentTargetFlow(expr.Operand);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2220:1.
    private void bindDestructuringAssignmentFlow(Node node)
    {
        var expr = node.AsBinaryExpression();
        if (this.inAssignmentPattern)
        {
            this.inAssignmentPattern = false;
            this.bind(expr.OperatorToken);
            this.bind(expr.Right);
            this.inAssignmentPattern = true;
            this.bind(expr.Left);
            this.bind(expr.Type);
        }
        else
        {
            this.inAssignmentPattern = true;
            this.bind(expr.Left);
            this.bind(expr.Type);
            this.inAssignmentPattern = false;
            this.bind(expr.OperatorToken);
            this.bind(expr.Right);
        }

        this.bindAssignmentTargetFlow(expr.Left);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2240:1.
    private void bindBinaryExpressionFlow(Node node)
    {
        var expr = node.AsBinaryExpression();
        var @operator = expr.OperatorToken.Kind;
        if ((Ast.IsLogicalOrCoalescingBinaryOperator(@operator) || Ast.IsLogicalOrCoalescingAssignmentOperator(@operator)))
        {
            if (isTopLevelLogicalExpression(node))
            {
                var postExpressionLabel = this.createBranchLabel();
                var saveCurrentFlow = this.currentFlow;
                var saveHasFlowEffects = this.hasFlowEffects;
                this.hasFlowEffects = false;
                this.bindLogicalLikeExpression(node, postExpressionLabel, postExpressionLabel);
                if (this.hasFlowEffects)
                {
                    this.currentFlow = this.finishFlowLabel(postExpressionLabel);
                }
                else
                {
                    this.currentFlow = saveCurrentFlow;
                }

                this.hasFlowEffects = (this.hasFlowEffects || saveHasFlowEffects);
            }
            else
            {
                this.bindLogicalLikeExpression(node, this.currentTrueTarget, this.currentFalseTarget);
            }
        }
        else
        {
            this.bind(expr.Left);
            this.bind(expr.Type);
            if ((@operator == Kind.CommaToken))
            {
                this.maybeBindExpressionFlowIfCall(expr.Left);
            }

            this.bind(expr.OperatorToken);
            this.bind(expr.Right);
            if ((@operator == Kind.CommaToken))
            {
                this.maybeBindExpressionFlowIfCall(expr.Right);
            }

            if ((Ast.IsAssignmentOperator(@operator) && !Ast.IsAssignmentTarget(node)))
            {
                this.bindAssignmentTargetFlow(expr.Left);
                if (((@operator == Kind.EqualsToken) && (expr.Left.Kind == Kind.ElementAccessExpression)))
                {
                    var elementAccess = expr.Left.AsElementAccessExpression();
                    if (isNarrowableOperand(elementAccess.Expression))
                    {
                        this.currentFlow = this.createFlowMutation(FlowFlags.ArrayMutation, this.currentFlow, node);
                    }
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2282:1.
    private void bindLogicalLikeExpression(Node node, FlowNode trueTarget, FlowNode falseTarget)
    {
        var expr = node.AsBinaryExpression();
        var preRightLabel = this.createBranchLabel();
        if (((expr.OperatorToken.Kind == Kind.AmpersandAmpersandToken) || (expr.OperatorToken.Kind == Kind.AmpersandAmpersandEqualsToken)))
        {
            this.bindCondition(expr.Left, preRightLabel, falseTarget);
        }
        else
        {
            this.bindCondition(expr.Left, trueTarget, preRightLabel);
        }

        this.currentFlow = this.finishFlowLabel(preRightLabel);
        this.bind(expr.OperatorToken);
        if (Ast.IsLogicalOrCoalescingAssignmentOperator(expr.OperatorToken.Kind))
        {
            this.doWithConditionalBranches((binderCallback, argnode) => binderCallback.bind(argnode), expr.Right, trueTarget, falseTarget);
            this.bindAssignmentTargetFlow(expr.Left);
            this.addAntecedent(trueTarget, this.createFlowCondition(FlowFlags.TrueCondition, this.currentFlow, node));
            this.addAntecedent(falseTarget, this.createFlowCondition(FlowFlags.FalseCondition, this.currentFlow, node));
        }
        else
        {
            this.bindCondition(expr.Right, trueTarget, falseTarget);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2302:1.
    private void bindDeleteExpressionFlow(Node node)
    {
        var expr = node.AsDeleteExpression();
        this.bindEachChild(node);
        if ((expr.Expression.Kind == Kind.PropertyAccessExpression))
        {
            this.bindAssignmentTargetFlow(expr.Expression);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2310:1.
    private void bindConditionalExpressionFlow(Node node)
    {
        var expr = node.AsConditionalExpression();
        var trueLabel = this.createBranchLabel();
        var falseLabel = this.createBranchLabel();
        var postExpressionLabel = this.createBranchLabel();
        var saveCurrentFlow = this.currentFlow;
        var saveHasFlowEffects = this.hasFlowEffects;
        this.hasFlowEffects = false;
        this.bindCondition(expr.Condition, trueLabel, falseLabel);
        this.currentFlow = this.finishFlowLabel(trueLabel);
        this.bind(expr.QuestionToken);
        this.bind(expr.WhenTrue);
        this.addAntecedent(postExpressionLabel, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(falseLabel);
        this.bind(expr.ColonToken);
        this.bind(expr.WhenFalse);
        this.addAntecedent(postExpressionLabel, this.currentFlow);
        if (this.hasFlowEffects)
        {
            this.currentFlow = this.finishFlowLabel(postExpressionLabel);
        }
        else
        {
            this.currentFlow = saveCurrentFlow;
        }

        this.hasFlowEffects = (this.hasFlowEffects || saveHasFlowEffects);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2335:1.
    private void bindVariableDeclarationFlow(Node node)
    {
        this.bindEachChild(node);
        if (((node.Initializer() != null) || Ast.IsForInOrOfStatement(node.Parent.Parent)))
        {
            this.bindInitializedVariableFlow(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2342:1.
    private void bindInitializedVariableFlow(Node node)
    {
        Node name = default;
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            {
                name = node.AsVariableDeclaration().Name();
                break;
            }

            case Kind.BindingElement:
            {
                name = node.AsBindingElement().Name();
                break;
            }
        }

        if (((name != null) && Ast.IsBindingPattern(name)))
        {
            {
                foreach (var child in GoRuntime.RangeValues(name.Elements()))
                {
                    this.bindInitializedVariableFlow(child);
                }
            }
        }
        else
        {
            this.currentFlow = this.createFlowMutation(FlowFlags.Assignment, this.currentFlow, node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2359:1.
    private void bindAccessExpressionFlow(Node node)
    {
        if (Ast.IsOptionalChain(node))
        {
            this.bindOptionalChainFlow(node);
        }
        else
        {
            this.bindEachChild(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2367:1.
    private void bindOptionalChainFlow(Node node)
    {
        if (isTopLevelLogicalExpression(node))
        {
            var postExpressionLabel = this.createBranchLabel();
            var saveCurrentFlow = this.currentFlow;
            var saveHasFlowEffects = this.hasFlowEffects;
            this.bindOptionalChain(node, postExpressionLabel, postExpressionLabel);
            if (this.hasFlowEffects)
            {
                this.currentFlow = this.finishFlowLabel(postExpressionLabel);
            }
            else
            {
                this.currentFlow = saveCurrentFlow;
            }

            this.hasFlowEffects = (this.hasFlowEffects || saveHasFlowEffects);
        }
        else
        {
            this.bindOptionalChain(node, this.currentTrueTarget, this.currentFalseTarget);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2384:1.
    private void bindOptionalChain(Node node, FlowNode trueTarget, FlowNode falseTarget)
    {
        FlowNode preChainLabel = default;
        if (Ast.IsOptionalChainRoot(node))
        {
            preChainLabel = this.createBranchLabel();
        }

        this.bindOptionalExpression(node.Expression(), Core.IfElse((preChainLabel != null), preChainLabel, trueTarget), falseTarget);
        if ((preChainLabel != null))
        {
            this.currentFlow = this.finishFlowLabel(preChainLabel);
        }

        this.doWithConditionalBranches((binderCallback, argnode) => binderCallback.bindOptionalChainRest(argnode), node, trueTarget, falseTarget);
        if (Ast.IsOutermostOptionalChain(node))
        {
            this.addAntecedent(trueTarget, this.createFlowCondition(FlowFlags.TrueCondition, this.currentFlow, node));
            this.addAntecedent(falseTarget, this.createFlowCondition(FlowFlags.FalseCondition, this.currentFlow, node));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2411:1.
    private void bindOptionalExpression(Node node, FlowNode trueTarget, FlowNode falseTarget)
    {
        this.doWithConditionalBranches((binderCallback, argnode) => binderCallback.bind(argnode), node, trueTarget, falseTarget);
        if ((!Ast.IsOptionalChain(node) || Ast.IsOutermostOptionalChain(node)))
        {
            this.addAntecedent(trueTarget, this.createFlowCondition(FlowFlags.TrueCondition, this.currentFlow, node));
            this.addAntecedent(falseTarget, this.createFlowCondition(FlowFlags.FalseCondition, this.currentFlow, node));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2419:1.
    private bool bindOptionalChainRest(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                this.bind(node.QuestionDotToken());
                this.bind(node.Name());
                break;
            }

            case Kind.ElementAccessExpression:
            {
                this.bind(node.QuestionDotToken());
                this.bind(node.AsElementAccessExpression().ArgumentExpression);
                break;
            }

            case Kind.CallExpression:
            {
                this.bind(node.QuestionDotToken());
                this.bindNodeList(node.TypeArgumentList());
                this.bindEach(node.Arguments());
                break;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2435:1.
    private void bindCallExpressionFlow(Node node)
    {
        var call = node.AsCallExpression();
        if (Ast.IsOptionalChain(node))
        {
            this.bindOptionalChainFlow(node);
        }
        else
        {
            var expr = Ast.SkipParentheses(call.Expression);
            if (((expr.Kind == Kind.FunctionExpression) || (expr.Kind == Kind.ArrowFunction)))
            {
                this.bindNodeList(call.TypeArguments);
                this.bindEach(call.Arguments.Nodes);
                this.bind(call.Expression);
            }
            else
            {
                this.bindEachChild(node);
                if ((call.Expression.Kind == Kind.SuperKeyword))
                {
                    this.currentFlow = this.createFlowCall(this.currentFlow, node);
                }
            }
        }

        if (Ast.IsPropertyAccessExpression(call.Expression))
        {
            var access = call.Expression.AsPropertyAccessExpression();
            if (((Ast.IsIdentifier(access.Name()) && isNarrowableOperand(access.Expression)) && Ast.IsPushOrUnshiftIdentifier(access.Name())))
            {
                this.currentFlow = this.createFlowMutation(FlowFlags.ArrayMutation, this.currentFlow, node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2463:1.
    private void bindNonNullExpressionFlow(Node node)
    {
        if (Ast.IsOptionalChain(node))
        {
            this.bindOptionalChainFlow(node);
        }
        else
        {
            this.bindEachChild(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2471:1.
    private void bindBindingElementFlow(Node node)
    {
        var elem = node.AsBindingElement();
        this.bind(elem.DotDotDotToken);
        this.bind(elem.PropertyName);
        this.bindInitializer(elem.Initializer);
        this.bind(elem.Name());
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2484:1.
    private void bindParameterFlow(Node node)
    {
        var param = node.AsParameterDeclaration();
        this.bindModifiers(param.Modifiers());
        this.bind(param.DotDotDotToken);
        this.bind(param.QuestionToken);
        this.bind(param.Type);
        this.bindInitializer(param.Initializer);
        this.bind(param.Name());
    }

    // a BindingElement/Parameter does not have side effects if initializers are not evaluated and used. (see GH#49759)
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2495:1.
    private void bindInitializer(Node node)
    {
        if ((node == null))
        {
            return;
        }

        var entryFlow = this.currentFlow;
        this.bind(node);
        if (((entryFlow == this.unreachableFlow) || (entryFlow == this.currentFlow)))
        {
            return;
        }

        var exitFlow = this.createBranchLabel();
        this.addAntecedent(exitFlow, entryFlow);
        this.addAntecedent(exitFlow, this.currentFlow);
        this.currentFlow = this.finishFlowLabel(exitFlow);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2510:1.
    private static void setFlowNode(Node node, FlowNode flowNode)
    {
        var data = node.FlowNodeData();
        if ((data != null))
        {
            data.FlowNode = flowNode;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2517:1.
    private static void setReturnFlowNode(Node node, FlowNode returnFlowNode)
    {
        switch (node.Kind)
        {
            case Kind.Constructor:
            {
                node.AsConstructorDeclaration().ReturnFlowNode = returnFlowNode;
                break;
            }

            case Kind.FunctionDeclaration:
            {
                node.AsFunctionDeclaration().ReturnFlowNode = returnFlowNode;
                break;
            }

            case Kind.FunctionExpression:
            {
                node.AsFunctionExpression().ReturnFlowNode = returnFlowNode;
                break;
            }

            case Kind.ClassStaticBlockDeclaration:
            {
                node.AsClassStaticBlockDeclaration().ReturnFlowNode = returnFlowNode;
                break;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2530:1.
    private static bool isGeneratorFunctionExpression(Node node)
    {
        return (Ast.IsFunctionExpression(node) && (node.AsFunctionExpression().AsteriskToken != null));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2534:1.
    private void addToContainerChain(Node next)
    {
        if ((this.lastContainer != null))
        {
            this.lastContainer.LocalsContainerData().NextContainer = next;
        }

        this.lastContainer = next;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2541:1.
    private void addDeclarationToSymbol(Symbol symbol, Node node, SymbolFlags symbolFlags)
    {
        symbol.Flags |= symbolFlags;
        node.DeclarationData().Symbol = symbol;
        if ((symbol.Declarations == null))
        {
            symbol.Declarations = this.newSingleDeclaration(node);
        }
        else
        {
            symbol.Declarations = Core.AppendIfUnique(symbol.Declarations, node);
        }

        if ((((symbol.Flags & SymbolFlags.ConstEnumOnlyModule) != 0) && ((symbol.Flags & (((SymbolFlags.Function | SymbolFlags.Class) | SymbolFlags.RegularEnum))) != 0)))
        {
            symbol.Flags &= ~(SymbolFlags.ConstEnumOnlyModule);
            this.notConstEnumOnlyModules.Add(symbol);
        }

        if (((symbolFlags & SymbolFlags.Value) != 0))
        {
            SetValueDeclaration(symbol, node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2559:1.
    public static void SetValueDeclaration(Symbol symbol, Node node)
    {
        var valueDeclaration = symbol.ValueDeclaration;
        if ((((valueDeclaration == null) || (isAssignmentDeclaration(valueDeclaration) && !isAssignmentDeclaration(node))) || ((valueDeclaration.Kind != node.Kind) && isEffectiveModuleDeclaration(valueDeclaration))))
        {
            symbol.ValueDeclaration = node;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2579:1.
    public static ContainerFlags GetContainerFlags(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ClassExpression:
            case Kind.ClassDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ObjectLiteralExpression:
            case Kind.TypeLiteral:
            case Kind.JsxAttributes:
            {
                return ContainerFlags.IsContainer;
            }

            case Kind.InterfaceDeclaration:
            {
                return (ContainerFlags.IsContainer | ContainerFlags.IsInterface);
            }

            case Kind.ModuleDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.MappedType:
            case Kind.IndexSignature:
            {
                return (ContainerFlags.IsContainer | ContainerFlags.HasLocals);
            }

            case Kind.SourceFile:
            {
                return ((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals);
            }

            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
            {
                if (Ast.IsObjectLiteralOrClassExpressionMethodOrAccessor(node))
                {
                    return (((((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals) | ContainerFlags.IsFunctionLike) | ContainerFlags.IsObjectLiteralOrClassExpressionMethodOrAccessor) | ContainerFlags.IsThisContainer);
                }

                goto case Kind.Constructor;
            }

            case Kind.Constructor:
            case Kind.FunctionDeclaration:
            case Kind.ClassStaticBlockDeclaration:
            {
                return ((((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals) | ContainerFlags.IsFunctionLike) | ContainerFlags.IsThisContainer);
            }

            case Kind.MethodSignature:
            case Kind.CallSignature:
            case Kind.FunctionType:
            case Kind.ConstructSignature:
            case Kind.ConstructorType:
            {
                return ((((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals) | ContainerFlags.IsFunctionLike) | ContainerFlags.PropagatesThisKeyword);
            }

            case Kind.FunctionExpression:
            {
                return (((((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals) | ContainerFlags.IsFunctionLike) | ContainerFlags.IsFunctionExpression) | ContainerFlags.IsThisContainer);
            }

            case Kind.ArrowFunction:
            {
                return (((((ContainerFlags.IsContainer | ContainerFlags.IsControlFlowContainer) | ContainerFlags.HasLocals) | ContainerFlags.IsFunctionLike) | ContainerFlags.IsFunctionExpression) | ContainerFlags.PropagatesThisKeyword);
            }

            case Kind.ModuleBlock:
            {
                return ContainerFlags.IsControlFlowContainer;
            }

            case Kind.PropertyDeclaration:
            {
                if ((node.Initializer() != null))
                {
                    return (ContainerFlags.IsControlFlowContainer | ContainerFlags.IsThisContainer);
                }
                else
                {
                    return ContainerFlags.None;
                }
            }

            case Kind.CatchClause:
            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.CaseBlock:
            {
                return (ContainerFlags.IsBlockScopedContainer | ContainerFlags.HasLocals);
            }

            case Kind.Block:
            {
                if ((Ast.IsFunctionLike(node.Parent) || Ast.IsClassStaticBlockDeclaration(node.Parent)))
                {
                    return ContainerFlags.None;
                }
                else
                {
                    return (ContainerFlags.IsBlockScopedContainer | ContainerFlags.HasLocals);
                }
            }
        }

        return ContainerFlags.None;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2623:1.
    private static bool isNarrowingExpression(Node expr)
    {
        switch (expr.Kind)
        {
            case Kind.Identifier:
            case Kind.ThisKeyword:
            {
                return true;
            }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            {
                return containsNarrowableReference(expr);
            }

            case Kind.CallExpression:
            {
                return hasNarrowableArgument(expr);
            }

            case Kind.ParenthesizedExpression:
            case Kind.NonNullExpression:
            case Kind.TypeOfExpression:
            {
                return isNarrowingExpression(expr.Expression());
            }

            case Kind.BinaryExpression:
            {
                return isNarrowingBinaryExpression(expr.AsBinaryExpression());
            }

            case Kind.PrefixUnaryExpression:
            {
                return ((expr.AsPrefixUnaryExpression().Operator == Kind.ExclamationToken) && isNarrowingExpression(expr.AsPrefixUnaryExpression().Operand));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2641:1.
    private static bool containsNarrowableReference(Node expr)
    {
        if (isNarrowableReference(expr))
        {
            return true;
        }

        if (((expr.Flags & NodeFlags.OptionalChain) != 0))
        {
            switch (expr.Kind)
            {
                case Kind.PropertyAccessExpression:
                case Kind.ElementAccessExpression:
                case Kind.CallExpression:
                case Kind.NonNullExpression:
                {
                    return containsNarrowableReference(expr.Expression());
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2654:1.
    private static bool isNarrowableReference(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.MetaProperty:
            {
                return true;
            }

            case Kind.PropertyAccessExpression:
            case Kind.ParenthesizedExpression:
            case Kind.NonNullExpression:
            {
                return isNarrowableReference(node.Expression());
            }

            case Kind.ElementAccessExpression:
            {
                var expr = node.AsElementAccessExpression();
                return (Ast.IsStringOrNumericLiteralLike(expr.ArgumentExpression) || (Ast.IsEntityNameExpression(expr.ArgumentExpression) && isNarrowableReference(expr.Expression)));
            }

            case Kind.BinaryExpression:
            {
                var expr2 = node.AsBinaryExpression();
                return (((expr2.OperatorToken.Kind == Kind.CommaToken) && isNarrowableReference(expr2.Right)) || (Ast.IsAssignmentOperator(expr2.OperatorToken.Kind) && Ast.IsLeftHandSideExpression(expr2.Left)));
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2672:1.
    private static bool hasNarrowableArgument(Node expr)
    {
        var call = expr.AsCallExpression();
        {
            foreach (var argument in GoRuntime.RangeValues(call.Arguments.Nodes))
            {
                if (containsNarrowableReference(argument))
                {
                    return true;
                }
            }
        }

        if (Ast.IsPropertyAccessExpression(call.Expression))
        {
            if (containsNarrowableReference(call.Expression.Expression()))
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2687:1.
    private static bool isNarrowingBinaryExpression(BinaryExpression expr)
    {
        switch (expr.OperatorToken.Kind)
        {
            case Kind.EqualsToken:
            case Kind.BarBarEqualsToken:
            case Kind.AmpersandAmpersandEqualsToken:
            case Kind.QuestionQuestionEqualsToken:
            {
                return containsNarrowableReference(expr.Left);
            }

            case Kind.EqualsEqualsToken:
            case Kind.ExclamationEqualsToken:
            case Kind.EqualsEqualsEqualsToken:
            case Kind.ExclamationEqualsEqualsToken:
            {
                var left = Ast.SkipParentheses(expr.Left);
                var right = Ast.SkipParentheses(expr.Right);
                return ((((isNarrowableOperand(left) || isNarrowableOperand(right)) || isNarrowingTypeOfOperands(right, left)) || isNarrowingTypeOfOperands(left, right)) || (((Ast.IsBooleanLiteral(right) && isNarrowingExpression(left)) || (Ast.IsBooleanLiteral(left) && isNarrowingExpression(right)))));
            }

            case Kind.InstanceOfKeyword:
            {
                return isNarrowableOperand(expr.Left);
            }

            case Kind.InKeyword:
            {
                return isNarrowingExpression(expr.Right);
            }

            case Kind.CommaToken:
            {
                return isNarrowingExpression(expr.Right);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2707:1.
    private static bool isNarrowableOperand(Node expr)
    {
        switch (expr.Kind)
        {
            case Kind.ParenthesizedExpression:
            {
                return isNarrowableOperand(expr.Expression());
            }

            case Kind.BinaryExpression:
            {
                var binary = expr.AsBinaryExpression();
                switch (binary.OperatorToken.Kind)
                {
                    case Kind.EqualsToken:
                    {
                        return isNarrowableOperand(binary.Left);
                    }

                    case Kind.CommaToken:
                    {
                        return isNarrowableOperand(binary.Right);
                    }
                }

                break;
            }
        }

        return containsNarrowableReference(expr);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2723:1.
    private static bool isNarrowingTypeOfOperands(Node expr1, Node expr2)
    {
        return ((Ast.IsTypeOfExpression(expr1) && isNarrowableOperand(expr1.Expression())) && Ast.IsStringLiteralLike(expr2));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2727:1.
    private void errorOnNode(Node node, Message message, params object[] args)
    {
        this.addDiagnostic(this.createDiagnosticForNode(node, message, args));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2731:1.
    private void errorOnFirstToken(Node node, Message message, params object[] args)
    {
        var span = Scanner.GetRangeOfTokenAtPosition(this.file, node.Pos());
        this.addDiagnostic(Ast.NewDiagnostic(this.file, span, message, args));
    }

    // Inside the binder, we may create a diagnostic for an as-yet unbound node (with potentially no parent pointers, implying no accessible source file)
    // If so, the node _must_ be in the current file (as that's the only way anything could have traversed to it to yield it as the error node)
    // This version of `createDiagnosticForNode` uses the binder's context to account for this, and always yields correct diagnostics even in these situations.
    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2739:1.
    private Diagnostic createDiagnosticForNode(Node node, Message message, params object[] args)
    {
        return Ast.NewDiagnostic(this.file, Scanner.GetErrorRangeForNode(this.file, node), message, args);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2743:1.
    private void addDiagnostic(Diagnostic diagnostic)
    {
        this.file.SetBindDiagnostics(GoRuntime.Append(this.file.BindDiagnostics(), diagnostic));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2747:1.
    private static bool isSignedNumericLiteral(Node node)
    {
        if ((node.Kind == Kind.PrefixUnaryExpression))
        {
            var node2 = node.AsPrefixUnaryExpression();
            return ((((node2.Operator == Kind.PlusToken) || (node2.Operator == Kind.MinusToken))) && Ast.IsNumericLiteral(node2.Operand));
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2755:1.
    private static SymbolFlags getOptionalSymbolFlagForNode(Node node)
    {
        var postfixToken = node.PostfixToken();
        return Core.IfElse(((postfixToken != null) && (postfixToken.Kind == Kind.QuestionToken)), SymbolFlags.Optional, SymbolFlags.None);
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2760:1.
    private static bool isFunctionSymbol(Symbol symbol)
    {
        var d = symbol.ValueDeclaration;
        if ((d != null))
        {
            if (Ast.IsFunctionDeclaration(d))
            {
                return true;
            }

            if (Ast.IsVariableDeclaration(d))
            {
                var varDecl = d.AsVariableDeclaration();
                if ((varDecl.Initializer != null))
                {
                    return Ast.IsFunctionLike(varDecl.Initializer);
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2776:1.
    private static bool isStatementCondition(Node node)
    {
        switch (node.Parent.Kind)
        {
            case Kind.IfStatement:
            case Kind.WhileStatement:
            case Kind.DoStatement:
            {
                return (node.Parent.Expression() == node);
            }

            case Kind.ForStatement:
            {
                return (node.Parent.AsForStatement().Condition == node);
            }

            case Kind.ConditionalExpression:
            {
                return (node.Parent.AsConditionalExpression().Condition == node);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2788:1.
    private static bool isTopLevelLogicalExpression(Node node)
    {
        while ((Ast.IsParenthesizedExpression(node.Parent) || (Ast.IsPrefixUnaryExpression(node.Parent) && (node.Parent.AsPrefixUnaryExpression().Operator == Kind.ExclamationToken))))
        {
            node = node.Parent;
        }

        return ((!isStatementCondition(node) && !Ast.IsLogicalExpression(node.Parent)) && !((Ast.IsOptionalChain(node.Parent) && (node.Parent.Expression() == node))));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2795:1.
    private static bool isAssignmentDeclaration(Node decl)
    {
        return (((Ast.IsBinaryExpression(decl) || Ast.IsAccessExpression(decl)) || Ast.IsIdentifier(decl)) || Ast.IsCallExpression(decl));
    }

    // Upstream upstream/TypeScript/tsc/internal/binder/binder.go:2799:1.
    private static bool isEffectiveModuleDeclaration(Node node)
    {
        return (Ast.IsModuleDeclaration(node) || Ast.IsIdentifier(node));
    }
}

// Hand-adapted declarations from binder.go; managed pool/arena boundaries only.
[Flags]
public enum ContainerFlags
{
    None = 0,
    IsContainer = 1 << 0,
    IsBlockScopedContainer = 1 << 1,
    IsControlFlowContainer = 1 << 2,
    IsFunctionLike = 1 << 3,
    IsFunctionExpression = 1 << 4,
    HasLocals = 1 << 5,
    IsInterface = 1 << 6,
    IsObjectLiteralOrClassExpressionMethodOrAccessor = 1 << 7,
    IsThisContainer = 1 << 8,
    PropagatesThisKeyword = 1 << 9,
}

public partial class Binder
{
    private sealed class ActiveLabel
    {
        public ActiveLabel next;
        public FlowNode breakTarget, continueTarget;
        public string name;
        public bool referenced;
        public FlowNode BreakTarget() => breakTarget;
        public FlowNode ContinueTarget() => continueTarget;
    }

    private struct ExpandoAssignmentInfo
    {
        public Node node, container, blockScopeContainer;
    }

    private SourceFile file;
    private readonly Visitor bindFunc;
    private FlowNode unreachableFlow;
    private Node container, thisContainer, blockScopeContainer, lastContainer;
    private FlowNode currentFlow, currentBreakTarget, currentContinueTarget, currentReturnTarget;
    private FlowNode currentTrueTarget, currentFalseTarget, currentExceptionTarget, preSwitchCaseFlow;
    private ActiveLabel activeLabelList;
    private NodeFlags emitFlags;
    private bool seenThisKeyword, hasExplicitReturn, hasFlowEffects, inAssignmentPattern, seenParseError;
    private int symbolCount;
    private readonly HashSet<Symbol> notConstEnumOnlyModules = new();
    private readonly GoObjectArena<Symbol> symbolArena = new();
    private readonly GoObjectArena<FlowNode> flowNodeArena = new();
    private readonly GoObjectArena<FlowList> flowListArena = new();
    private readonly GoArena<Node> singleDeclarationsArena = new();
    private List<ExpandoAssignmentInfo> expandoAssignments;
    private Binder() => bindFunc = bind;
    // getBinder/putBinder adaptation: preserve isolated binding state and arena-owned identity,
    // replacing sync.Pool with one fresh managed Binder per file. No checker state is imported.
    private static Binder getBinder() => new();
}
