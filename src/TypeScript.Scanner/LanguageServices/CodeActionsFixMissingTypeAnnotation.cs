// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/codeactions_fixmissingtypeannotation.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Change;
using TypeScript.Lsp.Protocol;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;
using LocaleValue = TypeScript.Locale.Locale;
using BuilderFlags = TypeScript.NodeBuilding.Flags;
using Type = TypeScript.Checking.Type;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private static readonly Slice<int> isolatedDeclarationsFixErrorCodes = Slice<int>.FromArray([Diagnostics.Function_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.Method_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.At_least_one_accessor_must_have_an_explicit_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.Variable_must_have_an_explicit_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.Parameter_must_have_an_explicit_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.Property_must_have_an_explicit_type_annotation_with_isolatedDeclarations.Code(), Diagnostics.Expression_type_can_t_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Binding_elements_with_initializers_can_t_be_exported_directly_with_isolatedDeclarations.Code(), Diagnostics.Computed_property_names_on_class_or_object_literals_cannot_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Computed_properties_must_be_number_or_string_literals_variables_or_dotted_expressions_with_isolatedDeclarations.Code(), Diagnostics.Enum_member_initializers_must_be_computable_without_references_to_external_symbols_with_isolatedDeclarations.Code(), Diagnostics.Extends_clause_can_t_contain_an_expression_with_isolatedDeclarations.Code(), Diagnostics.Objects_that_contain_shorthand_properties_can_t_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Objects_that_contain_spread_assignments_can_t_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Arrays_with_spread_elements_can_t_inferred_with_isolatedDeclarations.Code(), Diagnostics.Default_exports_can_t_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Only_const_arrays_can_be_inferred_with_isolatedDeclarations.Code(), Diagnostics.Assigning_properties_to_functions_without_declaring_them_is_not_supported_with_isolatedDeclarations_Add_an_explicit_declaration_for_the_properties_assigned_to_this_function.Code(), Diagnostics.Declaration_emit_for_this_parameter_requires_implicitly_adding_undefined_to_its_type_This_is_not_supported_with_isolatedDeclarations.Code(), Diagnostics.Type_containing_private_name_0_can_t_be_used_with_isolatedDeclarations.Code(), Diagnostics.Add_satisfies_and_a_type_assertion_to_this_expression_satisfies_T_as_T_to_make_the_type_explicit.Code(), ]);
    private static readonly ByteStringValue fixMissingTypeAnnotationOnExportsFixID = ByteStringValue.FromString("fixMissingTypeAnnotationOnExports");
    private static readonly CodeFixProvider IsolatedDeclarationsFixProvider = new()
    {
        ErrorCodes = isolatedDeclarationsFixErrorCodes,
        GetCodeActions = getIsolatedDeclarationsCodeActions,
        FixIds = Slice<ByteStringValue>.FromArray([fixMissingTypeAnnotationOnExportsFixID]),
        GetAllCodeActions = getAllIsolatedDeclarationsCodeActions,
    };
    private static readonly Dictionary<Kind, bool> canHaveTypeAnnotationKinds = new()
    {
        [Kind.GetAccessor] = true,
        [Kind.MethodDeclaration] = true,
        [Kind.PropertyDeclaration] = true,
        [Kind.FunctionDeclaration] = true,
        [Kind.FunctionExpression] = true,
        [Kind.ArrowFunction] = true,
        [Kind.VariableDeclaration] = true,
        [Kind.Parameter] = true,
        [Kind.ExportAssignment] = true,
        [Kind.ClassDeclaration] = true,
        [Kind.ObjectBindingPattern] = true,
        [Kind.ArrayBindingPattern] = true,
    };
    private const BuilderFlags declarationEmitNodeBuilderFlags = BuilderFlags.MultilineObjectLiterals | BuilderFlags.WriteClassExpressionAsTypeLiteral | BuilderFlags.UseTypeOfFunction | BuilderFlags.UseStructuralFallback | BuilderFlags.AllowEmptyTuple | BuilderFlags.GenerateNamesForShadowedTypeParams | BuilderFlags.NoTruncation;
    private enum TypePrintMode
    {
        Full,
        Relative,
        Widened
    }

    private static (Slice<CodeAction> Actions, Exception? Error) getIsolatedDeclarationsCodeActions(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        var(checker, done) = fixContext.Program.GetTypeCheckerForFile(context, fixContext.SourceFile);
        try
        {
            Slice<CodeAction> actions = default;
            void Add(CodeAction? action)
            {
                if (action is not null)
                    actions = actions.Append(action);
            }

            ReadOnlySpan<TypePrintMode> modes = [TypePrintMode.Full, TypePrintMode.Relative, TypePrintMode.Widened];
            foreach (var mode in modes)
                Add(tryCodeAction(context, fixContext, checker, fixer =>
                {
                    fixer.PrintMode = mode;
                    return fixer.addTypeAnnotation(fixContext.Span);
                }));
            foreach (var mode in modes)
                Add(tryCodeAction(context, fixContext, checker, fixer =>
                {
                    fixer.PrintMode = mode;
                    return fixer.addInlineAssertion(fixContext.Span);
                }));
            Add(tryCodeAction(context, fixContext, checker, fixer =>
            {
                fixer.PrintMode = TypePrintMode.Full;
                return fixer.extractAsVariable(fixContext.Span);
            }));
            return (actions, null);
        }
        finally
        {
            done();
        }
    }

    private static (CombinedCodeActions? Actions, Exception? Error) getAllIsolatedDeclarationsCodeActions(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        var diagnostics = getAllDiagnostics(context, fixContext.Program, fixContext.SourceFile);
        var(checker, done) = fixContext.Program.GetTypeCheckerForFile(context, fixContext.SourceFile);
        try
        {
            var tracker = Tracker.NewTracker(context, fixContext.Program.Options(), fixContext.LS.FormatOptions(), fixContext.LS.converters);
            var fixer = new IsolatedDeclarationsFixer
            {
                SourceFile = fixContext.SourceFile,
                Program = fixContext.Program,
                Checker = checker,
                ChangeTracker = tracker,
                Locale = context.Locale,
                FixedNodes = new(),
                PrintMode = TypePrintMode.Full
            };
            foreach (var diagnostic in diagnostics)
                if (isFixableDiagnostic(diagnostic, isolatedDeclarationsFixErrorCodes))
                    fixer.addTypeAnnotation(new TextRange(diagnostic.Loc().Pos(), diagnostic.Loc().End()));
            foreach (var symbol in fixer.SymbolsToImport)
                fixer.addSymbolToExistingImport(symbol);
            var(changes, _) = tracker.GetChanges();
            var edits = changes.GetValueOrDefault(fixContext.SourceFile.OriginalFileName());
            if (edits.Length == 0)
                return (null, null);
            return (new CombinedCodeActions { Description = ByteStringValue.FromString(Diagnostics.Add_all_missing_type_annotations.Localize(context.Locale)), Changes = edits }, null);
        }
        finally
        {
            done();
        }
    }

    private static CodeAction? tryCodeAction(ProtocolRequestContext context, CodeFixContext fixContext, Checker checker, Func<IsolatedDeclarationsFixer, ByteStringValue> action)
    {
        var tracker = Tracker.NewTracker(context, fixContext.Program.Options(), fixContext.LS.FormatOptions(), fixContext.LS.converters);
        IImportAdder? adder = null;
        var fixer = new IsolatedDeclarationsFixer
        {
            SourceFile = fixContext.SourceFile,
            Program = fixContext.Program,
            Checker = checker,
            ChangeTracker = tracker,
            ImportAdder = adder,
            Locale = context.Locale,
            FixedNodes = new()
        };
        var description = action(fixer);
        if (description.Length == 0)
            return null;
        foreach (var symbol in fixer.SymbolsToImport)
            fixer.addSymbolToExistingImport(symbol);
        var(changes, _) = tracker.GetChanges();
        var edits = changes.GetValueOrDefault(fixContext.SourceFile.OriginalFileName());
        if (adder is not null && adder.HasFixes())
            edits = edits.Append(adder.Edits());
        if (edits.Length == 0)
            return null;
        return new CodeAction
        {
            Description = description,
            Changes = edits,
            FixID = fixMissingTypeAnnotationOnExportsFixID,
            FixAllDescription = ByteStringValue.FromString(Diagnostics.Add_all_missing_type_annotations.Localize(context.Locale))
        };
    }

    private sealed class IsolatedDeclarationsFixer
    {
        internal SourceFile SourceFile = null!;
        internal CompilerProgram Program = null!;
        internal Checker Checker = null!;
        internal Tracker ChangeTracker = null!;
        internal IImportAdder? ImportAdder;
        internal LocaleValue Locale;
        internal Dictionary<Node, bool> FixedNodes = null!;
        internal TypePrintMode PrintMode;
        internal Slice<Symbol> SymbolsToImport;
        internal bool MutatedTarget;
        internal ByteStringValue addTypeAnnotation(TextRange span)
        {
            var node = AstNav.GetTokenAtPosition(SourceFile, span.Pos());
            var expando = findExpandoFunction(Checker, node);
            if (expando is not null)
                return Ast.IsFunctionDeclaration(expando) ? createNamespaceForExpandoProperties(expando) : fixIsolatedDeclarationError(expando);
            var missing = findAncestorWithMissingType(node);
            return missing is not null ? fixIsolatedDeclarationError(missing) : default;
        }

        private ByteStringValue createNamespaceForExpandoProperties(Node expando)
        {
            var declaration = expando.AsFunctionDeclaration();
            if (declaration.Name()is null)
                return default;
            var type = Checker.GetTypeAtLocation(expando);
            var elements = Checker.GetPropertiesOfType(type);
            if (elements.Length == 0)
                return default;
            var factory = ChangeTracker.NodeFactory;
            Slice<Node> properties = default;
            foreach (var symbol in elements)
            {
                if (!ScannerApi.IsIdentifierTextByteString(symbol.NameValue, LanguageVariant.Standard))
                    continue;
                if (symbol.ValueDeclaration is not null && Ast.IsVariableDeclaration(symbol.ValueDeclaration))
                    continue;
                var symbolType = Checker.GetTypeOfSymbol(symbol);
                var typeNode = typeToMinimizedReferenceType(symbolType, expando, declarationEmitNodeBuilderFlags);
                if (typeNode is null)
                    continue;
                var variable = factory.NewVariableDeclaration(factory.NewIdentifierWithTextValue(symbol.NameValue), null, typeNode, null);
                var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.None);
                properties = properties.Append(factory.NewVariableStatement(factory.NewModifierList(Slice<Node>.FromArray([factory.NewToken(Kind.ExportKeyword)])), list));
            }

            if (properties.Length == 0)
                return default;
            Slice<Node> modifiers = default;
            if (Ast.HasSyntacticModifier(expando, ModifierFlags.Export))
                modifiers = modifiers.Append(factory.NewToken(Kind.ExportKeyword));
            modifiers = modifiers.Append(factory.NewToken(Kind.DeclareKeyword));
            var module = factory.NewModuleDeclaration(factory.NewModifierList(modifiers), Kind.NamespaceKeyword, factory.NewIdentifierWithTextValue(declaration.Name().TextValue()), null, factory.NewModuleBlock(factory.NewNodeList(properties)));
            module.Flags = NodeFlags.Ambient | NodeFlags.ExportContext | NodeFlags.ContextFlags;
            ChangeTracker.InsertNodeAfter(SourceFile, expando, module);
            return ByteStringValue.FromString(Diagnostics.Annotate_types_of_properties_expando_function_in_a_namespace.Localize(Locale));
        }

        internal ByteStringValue addInlineAssertion(TextRange span)
        {
            var node = AstNav.GetTokenAtPosition(SourceFile, span.Pos());
            if (findExpandoFunction(Checker, node)is not null)
                return default;
            var target = findBestFittingNode(node, span);
            if (target is null || isValueSignatureDeclaration(target) || isValueSignatureDeclaration(target.Parent))
                return default;
            bool expression = Ast.IsExpression(target), shorthand = Ast.IsShorthandPropertyAssignment(target);
            if (!shorthand && isNamedDeclarationKind(target))
                return default;
            if (Ast.FindAncestor(target, Ast.IsBindingPattern)is not null || Ast.FindAncestor(target, Ast.IsEnumMember)is not null)
                return default;
            if (expression && (Ast.FindAncestorKind(target, Kind.HeritageClause)is not null || Ast.FindAncestor(target, Ast.IsTypeNode)is not null))
                return default;
            if (Ast.IsSpreadElement(target))
                return default;
            var variable = Ast.FindAncestorKind(target, Kind.VariableDeclaration);
            var variableType = variable is not null ? Checker.GetTypeAtLocation(variable) : null;
            if (variableType is not null && (variableType.Flags() & TypeFlags.UniqueESSymbol) != 0)
                return default;
            if (!expression && !shorthand)
                return default;
            var typeNode = inferType(target, variableType);
            if (typeNode is null || MutatedTarget)
                return default;
            var factory = ChangeTracker.NodeFactory;
            if (shorthand)
            {
                var name = factory.DeepCloneNode(target.AsShorthandPropertyAssignment().Name());
                ChangeTracker.InsertNodeAt(SourceFile, target.End(), createAsExpression(factory, name, typeNode), new NodeOptions { Prefix = ByteStringValue.FromString(": ") });
            }
            else if (expression)
            {
                var cloned = factory.DeepCloneNode(target);
                if (needsParenthesizedExpressionForAssertion(target))
                    cloned = factory.NewParenthesizedExpression(cloned);
                var clonedType = factory.DeepCloneNode(typeNode);
                ChangeTracker.ReplaceNode(SourceFile, target, factory.NewAsExpression(factory.NewSatisfiesExpression(cloned, clonedType), typeNode), null);
            }
            else
                return default;
            return ByteStringValue.FromString(Diagnostics.Add_satisfies_and_an_inline_type_assertion_with_0.Localize(Locale, typeToStringForDiag(typeNode, SourceFile, ChangeTracker)));
        }

        internal ByteStringValue extractAsVariable(TextRange span)
        {
            var node = AstNav.GetTokenAtPosition(SourceFile, span.Pos());
            var target = findBestFittingNode(node, span);
            if (target is null || isValueSignatureDeclaration(target) || isValueSignatureDeclaration(target.Parent))
                return default;
            if (!Ast.IsExpression(target))
                return default;
            var factory = ChangeTracker.NodeFactory;
            if (Ast.IsArrayLiteralExpression(target))
            {
                var constType = factory.NewTypeReferenceNode(factory.NewIdentifier("const"), null);
                ChangeTracker.ReplaceNode(SourceFile, target, createAsExpression(factory, factory.DeepCloneNode(target), constType), null);
                return ByteStringValue.FromString(Diagnostics.Mark_array_literal_as_const.Localize(Locale));
            }

            var property = Ast.FindAncestorKind(target, Kind.PropertyAssignment);
            if (property is not null)
            {
                if (property == target.Parent && Ast.IsEntityNameExpression(target))
                    return default;
                var temporary = ChangeTracker.EmitContext.Factory.NewUniqueNameEx(getIdentifierNameForNode(target), new TypeScript.DiagnosticPrinting.AutoGenerateOptions { Flags = TypeScript.DiagnosticPrinting.GeneratedIdentifierFlags.Optimistic });
                var replacement = target;
                var initialization = target;
                if (Ast.IsSpreadElement(replacement))
                {
                    replacement = Ast.WalkUpParenthesizedExpressions(replacement.Parent);
                    if (isConstAssertion(replacement.Parent))
                        replacement = initialization = replacement.Parent;
                    else
                        initialization = createAsExpression(factory, factory.DeepCloneNode(replacement), factory.NewTypeReferenceNode(factory.NewIdentifier("const"), null));
                }

                if (Ast.IsEntityNameExpression(replacement))
                    return default;
                var variable = factory.NewVariableDeclaration(temporary, null, null, factory.DeepCloneNode(initialization));
                var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
                var statement = Ast.FindAncestor(target, Ast.IsStatement);
                if (statement is null)
                    return default;
                ChangeTracker.InsertNodeBefore(SourceFile, statement, factory.NewVariableStatement(null, list), false, LeadingTriviaOption.None);
                ChangeTracker.ReplaceNode(SourceFile, replacement, factory.NewAsExpression(temporary, factory.NewTypeQueryNode(temporary, null)), null);
                return ByteStringValue.FromString(Diagnostics.Extract_to_variable_and_replace_with_0_as_typeof_0.Localize(Locale, typeToStringForDiag(temporary, SourceFile, ChangeTracker)));
            }

            return default;
        }

        private ByteStringValue fixIsolatedDeclarationError(Node node)
        {
            if (FixedNodes.GetValueOrDefault(node))
                return default;
            FixedNodes[node] = true;
            return node.Kind switch
            {
                Kind.Parameter or Kind.PropertyDeclaration or Kind.VariableDeclaration => addTypeToVariableLike(node),
                Kind.ArrowFunction or Kind.FunctionExpression or Kind.FunctionDeclaration or Kind.MethodDeclaration or Kind.GetAccessor => addTypeToSignatureDeclaration(node),
                Kind.ExportAssignment => transformExportAssignment(node),
                Kind.ClassDeclaration => transformExtendsClauseWithExpression(node),
                Kind.ObjectBindingPattern or Kind.ArrayBindingPattern => transformDestructuringPatterns(node),
                _ => default,
            };
        }

        private ByteStringValue addTypeToSignatureDeclaration(Node function)
        {
            if (function.Type()is not null)
                return default;
            var type = inferType(function, null);
            if (type is null)
                return default;
            ChangeTracker.TryInsertTypeAnnotation(SourceFile, function, type);
            return ByteStringValue.FromString(Diagnostics.Add_return_type_0.Localize(Locale, typeToStringForDiag(type, SourceFile, ChangeTracker)));
        }

        private ByteStringValue transformExportAssignment(Node export)
        {
            var assignment = export.AsExportAssignment();
            if (assignment.IsExportEquals)
                return default;
            var expression = assignment.Expression;
            var type = inferType(expression, null);
            if (type is null)
                return default;
            var factory = ChangeTracker.NodeFactory;
            var name = ChangeTracker.EmitContext.Factory.NewUniqueName("_default");
            var variable = factory.NewVariableDeclaration(name, null, type, factory.DeepCloneNode(expression));
            var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
            var statement = factory.NewVariableStatement(null, list);
            var newExport = factory.UpdateExportAssignment(assignment, export.Modifiers(), false, null, name);
            ChangeTracker.ReplaceNodeWithNodes(SourceFile, export, Slice<Node>.FromArray([statement, newExport]), null);
            return ByteStringValue.FromString(Diagnostics.Extract_default_export_to_variable.Localize(Locale));
        }

        private ByteStringValue transformExtendsClauseWithExpression(Node classNode)
        {
            var declaration = classNode.AsClassDeclaration();
            Node? extendsClause = null;
            if (declaration.HeritageClauses is not null)
                foreach (var clause in declaration.HeritageClauses.Nodes)
                    if (clause.AsHeritageClause().Token == Kind.ExtendsKeyword)
                    {
                        extendsClause = clause;
                        break;
                    }

            if (extendsClause is null)
                return default;
            var types = extendsClause.AsHeritageClause().Types;
            if (types is null || types.Nodes.Length == 0)
                return default;
            var heritage = types.Nodes[0];
            var expression = heritage.AsExpressionWithTypeArguments().Expression;
            var type = inferType(expression, null);
            if (type is null)
                return default;
            var factory = ChangeTracker.NodeFactory;
            var baseName = declaration.Name()is { } className ? ByteStringValue.Concat(className.TextValue(), ByteStringValue.FromString("Base")) : ByteStringValue.FromString("Anonymous");
            var name = ChangeTracker.EmitContext.Factory.NewUniqueNameEx(baseName, new TypeScript.DiagnosticPrinting.AutoGenerateOptions { Flags = TypeScript.DiagnosticPrinting.GeneratedIdentifierFlags.Optimistic });
            var variable = factory.NewVariableDeclaration(name, null, type, factory.DeepCloneNode(expression));
            var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
            ChangeTracker.InsertNodeBefore(SourceFile, classNode, factory.NewVariableStatement(null, list), false, LeadingTriviaOption.None);
            ChangeTracker.ReplaceNode(SourceFile, heritage, factory.NewExpressionWithTypeArguments(name, null), null);
            return ByteStringValue.FromString(Diagnostics.Extract_base_class_to_variable.Localize(Locale));
        }

        private ByteStringValue transformDestructuringPatterns(Node pattern)
        {
            var variable = pattern.Parent;
            if (!Ast.IsVariableDeclaration(variable))
                return default;
            var statement = variable.Parent.Parent;
            if (!Ast.IsVariableStatement(statement))
                return default;
            var initializer = variable.Initializer();
            if (initializer is null)
                return default;
            var factory = ChangeTracker.NodeFactory;
            Slice<Node> nodes = default;
            Node baseExpression;
            if (!Ast.IsIdentifier(initializer))
            {
                var name = ChangeTracker.EmitContext.Factory.NewUniqueNameEx("dest", new TypeScript.DiagnosticPrinting.AutoGenerateOptions { Flags = TypeScript.DiagnosticPrinting.GeneratedIdentifierFlags.Optimistic });
                var declaration = factory.NewVariableDeclaration(name, null, null, factory.DeepCloneNode(initializer));
                var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([declaration])), NodeFlags.Const);
                nodes = nodes.Append(factory.NewVariableStatement(null, list));
                baseExpression = name;
            }
            else
                baseExpression = factory.NewIdentifierWithTextValue(initializer.TextValue());
            extractBindingElements(pattern, baseExpression, ref nodes, statement);
            if (nodes.Length == 0)
                return default;
            var oldList = statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList();
            if (oldList.Declarations.Nodes.Length > 1)
            {
                Slice<Node> remaining = default;
                foreach (var declaration in oldList.Declarations.Nodes)
                    if (declaration != variable)
                        remaining = remaining.Append(declaration);
                if (remaining.Length > 0)
                    nodes = nodes.Append(factory.UpdateVariableStatement(statement.AsVariableStatement(), statement.Modifiers(), factory.UpdateVariableDeclarationList(oldList, factory.NewNodeList(remaining), oldList.Flags)));
            }

            ChangeTracker.ReplaceNodeWithNodes(SourceFile, statement, nodes, null);
            return ByteStringValue.FromString(Diagnostics.Extract_binding_expressions_to_variable.Localize(Locale));
        }

        private void extractBindingElements(Node pattern, Node baseExpression, ref Slice<Node> nodes, Node statement)
        {
            var factory = ChangeTracker.NodeFactory;
            if (Ast.IsObjectBindingPattern(pattern))
            {
                foreach (var element in pattern.AsBindingPattern().Elements.Nodes)
                {
                    if (Ast.IsOmittedExpression(element))
                        continue;
                    var binding = element.AsBindingElement();
                    var name = binding.Name();
                    if (name is null)
                        continue;
                    Node access;
                    if (binding.PropertyName is not null && Ast.IsComputedPropertyName(binding.PropertyName))
                    {
                        var expression = binding.PropertyName.AsComputedPropertyName().Expression;
                        var identifier = ChangeTracker.EmitContext.Factory.NewGeneratedNameForNode(expression);
                        var variable = factory.NewVariableDeclaration(identifier, null, null, expression);
                        var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
                        nodes = nodes.Append(factory.NewVariableStatement(null, list));
                        access = factory.NewElementAccessExpression(baseExpression, null, identifier, NodeFlags.None);
                    }
                    else if (binding.PropertyName is not null)
                        access = factory.NewPropertyAccessExpression(baseExpression, null, factory.NewIdentifierWithTextValue(binding.PropertyName.TextValue()), NodeFlags.None);
                    else if (Ast.IsIdentifier(name))
                        access = factory.NewPropertyAccessExpression(baseExpression, null, factory.NewIdentifierWithTextValue(name.TextValue()), NodeFlags.None);
                    else
                        continue;
                    if (Ast.IsBindingPattern(name))
                        extractBindingElements(name, access, ref nodes, statement);
                    else
                        emitBindingElementVariable(factory, name, binding, access, ref nodes, statement);
                }
            }
            else if (Ast.IsArrayBindingPattern(pattern))
            {
                var elements = pattern.AsBindingPattern().Elements.Nodes;
                for (int i = 0; i < elements.Length; i++)
                {
                    var element = elements[i];
                    if (Ast.IsOmittedExpression(element))
                        continue;
                    var binding = element.AsBindingElement();
                    var name = binding.Name();
                    if (name is null)
                        continue;
                    var access = factory.NewElementAccessExpression(baseExpression, null, factory.NewNumericLiteral(i.ToString(System.Globalization.CultureInfo.InvariantCulture), TokenFlags.None), NodeFlags.None);
                    if (Ast.IsBindingPattern(name))
                        extractBindingElements(name, access, ref nodes, statement);
                    else
                        emitBindingElementVariable(factory, name, binding, access, ref nodes, statement);
                }
            }
        }

        private void emitBindingElementVariable(NodeFactory factory, Node name, BindingElement binding, Node access, ref Slice<Node> nodes, Node statement)
        {
            var type = inferType(name, null);
            var initializer = access;
            if (binding.Initializer is not null)
            {
                var property = binding.PropertyName;
                var baseName = property is not null && Ast.IsIdentifier(property) ? property.TextValue() : ByteStringValue.FromString("temp");
                var temporary = ChangeTracker.EmitContext.Factory.NewUniqueNameEx(baseName, new TypeScript.DiagnosticPrinting.AutoGenerateOptions { Flags = TypeScript.DiagnosticPrinting.GeneratedIdentifierFlags.Optimistic });
                var temporaryDeclaration = factory.NewVariableDeclaration(temporary, null, null, initializer);
                var temporaryList = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([temporaryDeclaration])), NodeFlags.Const);
                nodes = nodes.Append(factory.NewVariableStatement(null, temporaryList));
                initializer = factory.NewConditionalExpression(factory.NewBinaryExpression(null, temporary, null, factory.NewToken(Kind.EqualsEqualsEqualsToken), factory.NewIdentifier("undefined")), factory.NewToken(Kind.QuestionToken), binding.Initializer, factory.NewToken(Kind.ColonToken), initializer);
            }

            var variable = factory.NewVariableDeclaration(factory.NewIdentifierWithTextValue(name.TextValue()), null, type, initializer);
            var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
            nodes = nodes.Append(factory.NewVariableStatement(getExportModifier(statement), list));
        }

        private ModifierList? getExportModifier(Node statement)
        {
            if (Ast.HasSyntacticModifier(statement, ModifierFlags.Export))
                return ChangeTracker.NodeFactory.NewModifierList(Slice<Node>.FromArray([ChangeTracker.NodeFactory.NewToken(Kind.ExportKeyword)]));
            return null;
        }

        private Node? inferType(Node node, Type? variableType)
        {
            MutatedTarget = false;
            if (PrintMode == TypePrintMode.Relative)
                return relativeType(node);
            Type? type = null;
            if (isValueSignatureDeclaration(node))
            {
                var signature = Checker.GetSignatureFromDeclaration(node);
                if (signature is not null)
                {
                    var predicate = Checker.GetTypePredicateOfSignature(signature);
                    if (predicate is not null)
                    {
                        if (predicate.Type()is null)
                            return null;
                        var enclosing = Ast.FindAncestor(node, Ast.IsDeclaration) ?? SourceFile;
                        var flags = declarationEmitNodeBuilderFlags;
                        if ((predicate.Type()!.Flags() & TypeFlags.UniqueESSymbol) != 0)
                            flags |= BuilderFlags.AllowUniqueESSymbolType;
                        return Checker.TypePredicateToTypePredicateNode(predicate, enclosing, flags, null);
                    }

                    type = Checker.GetReturnTypeOfSignature(signature);
                }
            }
            else
                type = Checker.GetTypeAtLocation(node);
            if (type is null)
                return null;
            if (PrintMode == TypePrintMode.Widened)
            {
                if (variableType is not null)
                    type = variableType;
                var widened = Checker.GetWidenedLiteralType(type);
                if (Checker.IsTypeAssignableTo(widened, type))
                    return null;
                type = widened;
            }

            var declaration = Ast.FindAncestor(node, Ast.IsDeclaration) ?? SourceFile;
            var builderFlags = declarationEmitNodeBuilderFlags | getExtraFlags(node, type);
            if (Ast.IsParameterDeclaration(node) && Checker.RequiresAddingImplicitUndefined(node))
                type = Checker.GetUnionTypeEx(Slice<Type>.FromArray([Checker.GetUndefinedType(), type]), Checker.UnionReduction.None);
            return typeToMinimizedReferenceType(type, declaration, builderFlags);
        }

        private BuilderFlags getExtraFlags(Node node, Type type)
        {
            if ((Ast.IsVariableDeclaration(node) || Ast.IsPropertyDeclaration(node) && Ast.HasSyntacticModifier(node, ModifierFlags.Static | ModifierFlags.Readonly)) && (type.Flags() & TypeFlags.UniqueESSymbol) != 0)
                return BuilderFlags.AllowUniqueESSymbolType;
            return BuilderFlags.None;
        }

        private Node createTypeOfFromEntityNameExpression(Node node) => ChangeTracker.NodeFactory.NewTypeQueryNode(ChangeTracker.NodeFactory.DeepCloneNode(node), null);
        private Node? typeFromArraySpreadElements(ArrayLiteralExpression node, ByteStringValue name)
        {
            bool isConst = Ast.FindAncestor(node, isConstAssertion)is not null;
            if (!isConst)
                return null;
            if (name.Length == 0)
                name = ByteStringValue.FromString("temp");
            var factory = ChangeTracker.NodeFactory;
            return typeFromSpreads(node, name, isConst, static item => item.AsArrayLiteralExpression().Elements.Nodes, Ast.IsSpreadElement, factory.NewSpreadElement, elements => factory.NewArrayLiteralExpression(factory.NewNodeList(elements), true), types =>
            {
                var rests = Slice<Node>.Make(types.Length);
                for (int i = 0; i < types.Length; i++)
                    rests[i] = factory.NewRestTypeNode(types[i]);
                return factory.NewTupleTypeNode(factory.NewNodeList(rests));
            });
        }

        private Node? typeFromObjectSpreadAssignment(ObjectLiteralExpression node, ByteStringValue name)
        {
            bool isConst = Ast.FindAncestor(node, isConstAssertion)is not null;
            if (name.Length == 0)
                name = ByteStringValue.FromString("temp");
            var factory = ChangeTracker.NodeFactory;
            return typeFromSpreads(node, name, isConst, static item => item.AsObjectLiteralExpression().Properties?.Nodes ?? default, Ast.IsSpreadAssignment, factory.NewSpreadAssignment, elements => factory.NewObjectLiteralExpression(factory.NewNodeList(elements), true), types => factory.NewIntersectionTypeNode(factory.NewNodeList(types)));
        }

        private Node? typeFromSpreads(Node node, ByteStringValue name, bool isConst, Func<Node, Slice<Node>> getChildren, Func<Node, bool> isSpread, Func<Node, Node> createSpread, Func<Slice<Node>, Node> makeNode, Func<Slice<Node>, Node> finalType)
        {
            var factory = ChangeTracker.NodeFactory;
            Slice<Node> types = default, spreads = default, properties = default;
            var statement = Ast.FindAncestor(node, Ast.IsStatement);
            foreach (var property in getChildren(node))
            {
                if (isSpread(property))
                {
                    finalizesVariablePart(factory, name, isConst, statement, makeNode, createSpread, ref properties, ref types, ref spreads);
                    if (Ast.IsEntityNameExpression(property.Expression()))
                    {
                        types = types.Append(createTypeOfFromEntityNameExpression(property.Expression()));
                        spreads = spreads.Append(property);
                    }
                    else
                        makeSpreadVariable(factory, name, isConst, statement, createSpread, property.Expression(), ref types, ref spreads);
                }
                else
                    properties = properties.Append(property);
            }

            if (spreads.Length == 0)
                return null;
            finalizesVariablePart(factory, name, isConst, statement, makeNode, createSpread, ref properties, ref types, ref spreads);
            ChangeTracker.ReplaceNode(SourceFile, node, makeNode(spreads), null);
            MutatedTarget = true;
            return finalType(types);
        }

        private void makeSpreadVariable(NodeFactory factory, ByteStringValue name, bool isConst, Node? statement, Func<Node, Node> createSpread, Node expression, ref Slice<Node> types, ref Slice<Node> spreads)
        {
            var suffix = ByteStringValue.FromString((spreads.Length + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            var temporary = ChangeTracker.EmitContext.Factory.NewUniqueNameEx(ByteStringValue.Concat(name, ByteStringValue.FromString("_Part"), suffix), new TypeScript.DiagnosticPrinting.AutoGenerateOptions { Flags = TypeScript.DiagnosticPrinting.GeneratedIdentifierFlags.Optimistic });
            var initializer = !isConst ? factory.DeepCloneNode(expression) : factory.NewAsExpression(factory.DeepCloneNode(expression), factory.NewTypeReferenceNode(factory.NewIdentifier("const"), null));
            var variable = factory.NewVariableDeclaration(temporary, null, null, initializer);
            var list = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([variable])), NodeFlags.Const);
            if (statement is not null)
                ChangeTracker.InsertNodeBefore(SourceFile, statement, factory.NewVariableStatement(null, list), false, LeadingTriviaOption.None);
            types = types.Append(createTypeOfFromEntityNameExpression(temporary));
            spreads = spreads.Append(createSpread(temporary));
        }

        private void finalizesVariablePart(NodeFactory factory, ByteStringValue name, bool isConst, Node? statement, Func<Slice<Node>, Node> makeNode, Func<Node, Node> createSpread, ref Slice<Node> properties, ref Slice<Node> types, ref Slice<Node> spreads)
        {
            if (properties.Length > 0)
            {
                makeSpreadVariable(factory, name, isConst, statement, createSpread, makeNode(properties), ref types, ref spreads);
                properties = default;
            }
        }

        private Node? relativeType(Node node)
        {
            if (Ast.IsParameterDeclaration(node))
                return null;
            if (Ast.IsShorthandPropertyAssignment(node))
                return createTypeOfFromEntityNameExpression(node.AsShorthandPropertyAssignment().Name());
            if (Ast.IsEntityNameExpression(node))
                return createTypeOfFromEntityNameExpression(node);
            if (isConstAssertion(node))
                return relativeType(node.Expression());
            if (Ast.IsArrayLiteralExpression(node))
            {
                var variable = Ast.FindAncestorKind(node, Kind.VariableDeclaration);
                var name = variable is not null && Ast.IsIdentifier(variable.Name()) ? variable.Name().TextValue() : default;
                return typeFromArraySpreadElements(node.AsArrayLiteralExpression(), name);
            }

            if (Ast.IsObjectLiteralExpression(node))
            {
                var variable = Ast.FindAncestorKind(node, Kind.VariableDeclaration);
                var name = variable is not null && Ast.IsIdentifier(variable.Name()) ? variable.Name().TextValue() : default;
                return typeFromObjectSpreadAssignment(node.AsObjectLiteralExpression(), name);
            }

            if (Ast.IsVariableDeclaration(node) && node.Initializer()is not null)
                return relativeType(node.Initializer());
            if (Ast.IsConditionalExpression(node))
            {
                var conditional = node.AsConditionalExpression();
                var whenTrue = relativeType(conditional.WhenTrue);
                if (whenTrue is null)
                    return null;
                bool trueMutated = MutatedTarget;
                var whenFalse = relativeType(conditional.WhenFalse);
                if (whenFalse is null)
                    return null;
                MutatedTarget = trueMutated || MutatedTarget;
                return ChangeTracker.NodeFactory.NewUnionTypeNode(ChangeTracker.NodeFactory.NewNodeList(Slice<Node>.FromArray([whenTrue, whenFalse])));
            }

            return null;
        }

        private Node? typeToMinimizedReferenceType(Type type, Node declaration, BuilderFlags flags)
        {
            var symbols = new Dictionary<Node, Symbol>();
            var node = Checker.TypeToTypeNodeEx(type, declaration, flags, TypeScript.NodeBuilding.InternalFlags.WriteComputedProps, symbols);
            if (node is null)
                return null;
            if (Ast.IsTypeReferenceNode(node) && (type.ObjectFlags() & ObjectFlags.Reference) != 0)
            {
                var arguments = Checker.GetTypeArguments(type);
                var nodeArguments = node.TypeArguments();
                if (arguments.Length > 0 && nodeArguments.Length > 0)
                {
                    int cutoff = endOfRequiredTypeParameters(Checker, type);
                    if (cutoff < nodeArguments.Length)
                    {
                        var trimmed = ChangeTracker.NodeFactory.NewNodeList(nodeArguments.Slice2(0, cutoff));
                        node = ChangeTracker.NodeFactory.UpdateTypeReferenceNode(node.AsTypeReferenceNode(), node.AsTypeReferenceNode().TypeName, trimmed);
                    }
                }
            }

            var(reference, importable) = TypeScript.LanguageServices.AutoImport.AutoImport.TryGetAutoImportableReferenceFromTypeNode(node, symbols);
            if (reference is not null)
            {
                node = reference;
                SymbolsToImport = SymbolsToImport.Append(importable);
            }

            return node;
        }

        private ByteStringValue addTypeToVariableLike(Node declaration)
        {
            var type = inferType(declaration, null);
            if (type is null)
                return default;
            if (declaration.Type()is not null)
                ChangeTracker.ReplaceNode(SourceFile, declaration.Type(), type, null);
            else
            {
                ChangeTracker.TryInsertTypeAnnotation(SourceFile, declaration, type);
                if (Ast.IsParameterDeclaration(declaration) && declaration.Parent is not null && Ast.IsArrowFunction(declaration.Parent))
                    ChangeTracker.ParenthesizeArrowParameters(SourceFile, declaration.Parent);
            }

            return ByteStringValue.FromString(Diagnostics.Add_annotation_of_type_0.Localize(Locale, typeToStringForDiag(type, SourceFile, ChangeTracker)));
        }

        internal void addSymbolToExistingImport(Symbol? symbol)
        {
            if (symbol?.Parent is null)
                return;
            var module = symbol.Parent;
            var name = symbol.NameValue;
            foreach (var statement in SourceFile.Statements.Nodes)
            {
                if (!Ast.IsImportDeclaration(statement))
                    continue;
                var declaration = statement.AsImportDeclaration();
                if (declaration.ImportClause is null)
                    continue;
                var importSymbol = Checker.GetSymbolAtLocation(declaration.ModuleSpecifier);
                if (importSymbol is null || Checker.GetMergedSymbol(importSymbol) != Checker.GetMergedSymbol(module))
                    continue;
                var clause = declaration.ImportClause.AsImportClause();
                if (clause.NamedBindings is not null && Ast.IsNamedImports(clause.NamedBindings))
                {
                    var elements = clause.NamedBindings.AsNamedImports().Elements.Nodes;
                    var factory = ChangeTracker.NodeFactory;
                    var newSpecifier = factory.NewImportSpecifier(false, null, factory.NewIdentifierWithTextValue(name));
                    var bindings = factory.NewNamedImports(factory.NewNodeList(elements.Append(newSpecifier)));
                    var newClause = factory.UpdateImportClause(clause, clause.PhaseModifier, clause.Name(), bindings);
                    ChangeTracker.ReplaceNode(SourceFile, statement, factory.UpdateImportDeclaration(declaration, declaration.Modifiers(), newClause, declaration.ModuleSpecifier, declaration.Attributes), null);
                }

                return;
            }
        }
    }

    private static bool needsParenthesizedExpressionForAssertion(Node node) => !Ast.IsEntityNameExpression(node) && !Ast.IsCallExpression(node) && !Ast.IsObjectLiteralExpression(node) && !Ast.IsArrayLiteralExpression(node);
    private static Node createAsExpression(NodeFactory factory, Node node, Node type)
    {
        if (needsParenthesizedExpressionForAssertion(node))
            node = factory.NewParenthesizedExpression(node);
        return factory.NewAsExpression(node, type);
    }

    private static bool isExpandoPropertyDeclarationForFix(Node? node) => node is not null && (Ast.IsPropertyAccessExpression(node) || Ast.IsElementAccessExpression(node) || Ast.IsBinaryExpression(node));
    private static Node? findExpandoFunction(Checker checker, Node node)
    {
        var expando = Ast.FindAncestorOrQuit(node, static item => Ast.IsStatement(item) ? FindAncestorResult.Quit : isExpandoPropertyDeclarationForFix(item) ? FindAncestorResult.True : FindAncestorResult.False);
        if (expando is null || !isExpandoPropertyDeclarationForFix(expando))
            return null;
        var target = expando;
        if (Ast.IsBinaryExpression(target))
        {
            target = target.AsBinaryExpression().Left;
            if (!isExpandoPropertyDeclarationForFix(target))
                return null;
        }

        Node expression;
        if (Ast.IsPropertyAccessExpression(target))
            expression = target.AsPropertyAccessExpression().Expression;
        else if (Ast.IsElementAccessExpression(target))
            expression = target.AsElementAccessExpression().Expression;
        else
            return null;
        var type = checker.GetTypeAtLocation(expression);
        if (type is null)
            return null;
        bool found = false;
        foreach (var property in checker.GetPropertiesOfType(type))
            if (property.ValueDeclaration == expando || property.ValueDeclaration == expando.Parent)
            {
                found = true;
                break;
            }

        if (!found)
            return null;
        var symbol = type.Symbol();
        if (symbol?.ValueDeclaration is null)
            return null;
        var function = symbol.ValueDeclaration;
        if ((Ast.IsFunctionExpression(function) || Ast.IsArrowFunction(function)) && Ast.IsVariableDeclaration(function.Parent))
            return function.Parent;
        return Ast.IsFunctionDeclaration(function) ? function : null;
    }

    private static bool isConstAssertion(Node node) => Ast.IsAssertionExpression(node) && Ast.IsConstTypeReference(node.Type());
    private static int endOfRequiredTypeParameters(Checker checker, Type type)
    {
        var arguments = checker.GetTypeArguments(type);
        if (arguments.Length == 0)
            return 0;
        var target = type.Target();
        if (target is null || target.AsInterfaceType()is null)
            return arguments.Length;
        var parameters = target.AsInterfaceType().TypeParameters();
        var localParameters = target.AsInterfaceType().LocalTypeParameters();
        int outerCount = parameters.Length - localParameters.Length;
        for (int cutoff = 0; cutoff < arguments.Length; cutoff++)
        {
            int localIndex = cutoff - outerCount;
            if (localIndex < 0 || localIndex >= localParameters.Length || !typeParamHasDefault(localParameters[localIndex]))
                continue;
            var filled = checker.FillMissingTypeArguments(arguments.Slice2(0, cutoff), parameters, cutoff, false);
            bool allMatch = true;
            for (int i = 0; i < filled.Length; i++)
                if (filled[i] != arguments[i])
                {
                    allMatch = false;
                    break;
                }

            if (allMatch)
                return cutoff;
        }

        return arguments.Length;
    }

    private static bool typeParamHasDefault(Type parameter)
    {
        var symbol = parameter.Symbol();
        if (symbol is null)
            return false;
        foreach (var declaration in symbol.Declarations)
            if (Ast.IsTypeParameterDeclaration(declaration) && declaration.AsTypeParameterDeclaration().DefaultType is not null)
                return true;
        return false;
    }

    private static ByteStringValue typeToStringForDiag(Node type, SourceFile file, Tracker tracker)
    {
        var saved = tracker.EmitContext.EmitFlags(type);
        tracker.EmitContext.SetEmitFlags(type, saved | TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
        var printer = TypeScript.DiagnosticPrinting.Printer.NewPrinter(new TypeScript.DiagnosticPrinting.PrinterOptions { NewLine = NewLineKind.LF }, default, tracker.EmitContext);
        var(writer, release) = TypeScript.DiagnosticPrinting.singleLineStringWriter.GetSingleLineStringWriter();
        try
        {
            printer.Write(type, file, writer, null);
            tracker.EmitContext.SetEmitFlags(type, saved);
            var result = writer.TextValue();
            return result.Length > 160 ? ByteStringValue.Concat(result.Slice(0, 157), ByteStringValue.FromString("...")) : result;
        }
        finally
        {
            release();
        }
    }

    private static Node? findAncestorWithMissingType(Node node) => Ast.FindAncestor(node, static item => canHaveTypeAnnotationKinds.GetValueOrDefault(item.Kind) && (!(Ast.IsObjectBindingPattern(item) || Ast.IsArrayBindingPattern(item)) || Ast.IsVariableDeclaration(item.Parent)));
    private static Node? findBestFittingNode(Node? node, TextRange span)
    {
        if (node is null)
            return null;
        while (node is not null && node.End() < span.Pos() + span.Len())
            node = node.Parent;
        while (node!.Parent is not null && node!.Parent.Pos() == node.Pos() && node!.Parent.End() == node.End())
            node = node.Parent;
        if (Ast.IsIdentifier(node) && Ast.HasInitializer(node.Parent) && node.Parent!.Initializer()is not null)
            return node.Parent.Initializer();
        if (Ast.IsIdentifier(node) && Ast.IsShorthandPropertyAssignment(node.Parent))
            return node.Parent;
        return node;
    }

    private static bool isNamedDeclarationKind(Node node) => node.Kind is Kind.ArrowFunction or Kind.BindingElement or Kind.ClassDeclaration or Kind.ClassExpression or Kind.ClassStaticBlockDeclaration or Kind.Constructor or Kind.EnumDeclaration or Kind.EnumMember or Kind.ExportSpecifier or Kind.FunctionDeclaration or Kind.FunctionExpression or Kind.GetAccessor or Kind.ImportClause or Kind.ImportEqualsDeclaration or Kind.ImportSpecifier or Kind.InterfaceDeclaration or Kind.JsxAttribute or Kind.MethodDeclaration or Kind.MethodSignature or Kind.ModuleDeclaration or Kind.NamespaceExportDeclaration or Kind.NamespaceImport or Kind.NamespaceExport or Kind.Parameter or Kind.PropertyAssignment or Kind.PropertyDeclaration or Kind.PropertySignature or Kind.SetAccessor or Kind.ShorthandPropertyAssignment or Kind.TypeAliasDeclaration or Kind.TypeParameter or Kind.VariableDeclaration or Kind.JSDocTypedefTag or Kind.JSDocCallbackTag or Kind.JSDocPropertyTag or Kind.NamedTupleMember;
    private static bool isValueSignatureDeclaration(Node node) => Ast.IsFunctionExpression(node) || Ast.IsArrowFunction(node) || Ast.IsMethodDeclaration(node) || Ast.IsAccessor(node) || Ast.IsFunctionDeclaration(node) || Ast.IsConstructorDeclaration(node);
    private static ByteStringValue getIdentifierNameForNode(Node node)
    {
        if (Ast.IsPropertyAccessExpression(node))
        {
            var name = node.AsPropertyAccessExpression().Name();
            if (Ast.IsIdentifier(name) && !Ast.IsPrivateIdentifier(name) && ScannerApi.IdentifierToKeywordKind(name.AsIdentifier()) == Kind.Unknown)
                return name.TextValue();
        }

        return ByteStringValue.FromString("newLocal");
    }
}
