// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/grammarchecks.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private TypeScript.Scanner.Scanner regExpScanner;
    // grammarchecks.go:69. Reuse the source scanner and retain byte-span diagnostics.
    private bool checkGrammarRegularExpressionLiteral(RegularExpressionLiteral node)
    {
        var sourceFile = Ast.GetSourceFileOfNode(node.AsNode());
        if (hasParseDiagnostics(sourceFile))
            return false;
        Diagnostic lastError = null;
        regExpScanner ??= TypeScript.Scanner.Scanner.NewScanner();
        regExpScanner.SetScriptTarget(languageVersion);
        regExpScanner.SetLanguageVariant(sourceFile.LanguageVariant);
        // This checker consumer stores Go-byte diagnostic arguments. The public
        // scanner callback converts them to Unicode for .NET callers instead.
        regExpScanner.SetOnErrorByteStrings((message, start, length, args) =>
        {
            if (message.Category() == DiagnosticCategory.Message && lastError != null && start == lastError.Pos() && length == lastError.Len())
                lastError.AddRelatedInfo(Ast.NewDiagnostic(null, new(start, start + length), message, args));
            else if (lastError == null || start != lastError.Pos())
                lastError = addDiagnostic(Ast.NewDiagnostic(sourceFile, new(start, start + length), message, args));
        });
        // grammarchecks.go:88. Share the existing byte-string source, as Go does;
        // the public byte[] setter would copy the complete file twice per regex.
        regExpScanner.SetByteTextInternal(sourceFile.TextView());
        regExpScanner.ResetTokenState(node.AsNode().Pos());
        regExpScanner.Scan();
        var tokenIsRegularExpressionLiteral = regExpScanner.ReScanSlashToken(true) == Kind.RegularExpressionLiteral;
        regExpScanner.SetText("");
        regExpScanner.SetOnErrorByteStrings(null);
        if (!tokenIsRegularExpressionLiteral)
            throw new InvalidOperationException("Regular expression scanner did not return a regular expression literal");
        return lastError != null;
    }

    private bool grammarErrorOnNode(Node node, Message message, params object[] args)
    {
        if (Ast.GetSourceFileOfNode(node).Diagnostics().Length != 0)
            return false;
        error(node, message, args);
        return true;
    }

    private bool grammarErrorAtPos(Node node, int start, int length, Message message, params object[] args)
    {
        var file = Ast.GetSourceFileOfNode(node);
        if (file.Diagnostics().Length != 0)
            return false;
        diagnostics.Add(Ast.NewDiagnostic(file, new(start, start + length), message, args));
        return true;
    }

    private bool grammarErrorOnNodeSkippedOnNoEmit(Node node, Message message, params object[] args)
    {
        if (hasParseDiagnostics(Ast.GetSourceFileOfNode(node)))
            return false;
        var diagnostic = NewDiagnosticForNode(node, message, args);
        diagnostic.SetSkippedOnNoEmit();
        addDiagnostic(diagnostic);
        return true;
    }

    // grammarchecks.go:859/1813/2087. Literal grammar is distinct from execution.
    private bool checkGrammarTaggedTemplateChain(TaggedTemplateExpression node)
    {
        if (node.QuestionDotToken != null || (node.Flags & NodeFlags.OptionalChain) != 0)
            return grammarErrorOnNode(node.Template, Diagnostics.Tagged_template_expressions_are_not_permitted_in_an_optional_chain);
        return false;
    }

    private bool checkGrammarMetaProperty(MetaProperty node)
    {
        var name = node.Name();
        var text = name.Text();
        switch (node.KeywordToken)
        {
            case Kind.NewKeyword:
                if (text != "target")
                    return grammarErrorOnNode(name, Diagnostics.X_0_is_not_a_valid_meta_property_for_keyword_1_Did_you_mean_2,
                        text, TypeScript.Scanner.Scanner.TokenToString(node.KeywordToken), "target");
                break;
            case Kind.ImportKeyword:
                if (text != "meta")
                {
                    var isCallee = Ast.IsCallExpression(node.Parent) && node.Parent.Expression() == node.AsNode();
                    if (Ast.IsImportPhaseMetaProperty(node.AsNode()))
                    {
                        if (!isCallee)
                            return grammarErrorAtPos(node.AsNode(), node.AsNode().End(), 0, Diagnostics.X_0_expected, "(");
                    }
                    else
                    {
                        if (isCallee)
                            return grammarErrorOnNode(name, Diagnostics.X_0_is_not_a_valid_meta_property_for_keyword_import_Did_you_mean_meta_defer_or_source, text);
                        return grammarErrorOnNode(name, Diagnostics.X_0_is_not_a_valid_meta_property_for_keyword_1_Did_you_mean_2,
                            text, TypeScript.Scanner.Scanner.TokenToString(node.KeywordToken), "meta");
                    }
                }
                break;
        }
        return false;
    }

    private bool checkGrammarBigIntLiteral(BigIntLiteral node)
    {
        var literalType = Ast.IsLiteralTypeNode(node.Parent) ||
            Ast.IsPrefixUnaryExpression(node.Parent) && Ast.IsLiteralTypeNode(node.Parent.Parent);
        if (!literalType && (node.Flags & NodeFlags.Ambient) == 0 && languageVersion < ScriptTarget.ES2020)
            return grammarErrorOnNode(node.AsNode(), Diagnostics.BigInt_literals_are_not_available_when_targeting_lower_than_ES2020);
        return false;
    }

    private bool checkGrammarModifiers(Node node)
    {
        if ((node.Modifiers() == null))
        {
            return false;
        }

        if ((this.reportObviousDecoratorErrors(node) || this.reportObviousModifierErrors(node)))
        {
            return true;
        }

        if (Ast.IsThisParameter(node))
        {
            return this.grammarErrorOnFirstToken(node, Diagnostics.Neither_decorators_nor_modifiers_may_be_applied_to_this_parameters);
        }

        var blockScopeKind = NodeFlags.None;
        if (Ast.IsVariableStatement(node))
        {
            blockScopeKind = (node.AsVariableStatement().DeclarationList.Flags & NodeFlags.BlockScoped);
        }

        Node lastStatic = default;
        Node lastDeclare = default;
        Node lastAsync = default;
        Node lastOverride = default;
        Node firstDecorator = default;
        var flags = ModifierFlags.None;
        var sawExportBeforeDecorators = false;
        var hasLeadingDecorators = false;
        var modifiers = node.ModifierNodes();
        {
            foreach (var modifier in GoRuntime.RangeValues(modifiers))
            {
                if (Ast.IsDecorator(modifier))
                {
                    if (!Ast.NodeCanBeDecorated(this.legacyDecorators, node, node.Parent, node.Parent.Parent))
                    {
                        if (((node.Kind == Kind.MethodDeclaration) && !Ast.NodeIsPresent(node.Body())))
                        {
                            return this.grammarErrorOnFirstToken(node, Diagnostics.A_decorator_can_only_decorate_a_method_implementation_not_an_overload);
                        }
                        else
                        {
                            return this.grammarErrorOnFirstToken(node, Diagnostics.Decorators_are_not_valid_here);
                        }
                    }
                    else
                    {
                        if ((this.legacyDecorators && (((node.Kind == Kind.GetAccessor) || (node.Kind == Kind.SetAccessor)))))
                        {
                            var accessors = Ast.GetAllAccessorDeclarationsForDeclaration(node, this.getSymbolOfDeclaration(node).Declarations);
                            if ((Ast.HasDecorators(accessors.FirstAccessor) && (node == accessors.SecondAccessor)))
                            {
                                return this.grammarErrorOnFirstToken(node, Diagnostics.Decorators_cannot_be_applied_to_multiple_get_Slashset_accessors_of_the_same_name);
                            }
                        }
                    }

                    if (((flags & ~((ModifierFlags.ExportDefault | ModifierFlags.Decorator))) != 0))
                    {
                        return this.grammarErrorOnNode(modifier, Diagnostics.Decorators_are_not_valid_here);
                    }

                    if ((hasLeadingDecorators && ((flags & ModifierFlags.Modifier) != 0)))
                    {
                        if ((firstDecorator == null))
                        {
                            throw new InvalidOperationException("Expected firstDecorator to be set");
                        }

                        var sourceFile = Ast.GetSourceFileOfNode(modifier);
                        if (!this.hasParseDiagnostics(sourceFile))
                        {
                            var err = this.error(modifier, Diagnostics.Decorators_may_not_appear_after_export_or_export_default_if_they_also_appear_before_export);
                            err.AddRelatedInfo(createDiagnosticForNode(firstDecorator, Diagnostics.Decorator_used_before_export_here));
                            return true;
                        }

                        return false;
                    }

                    flags |= ModifierFlags.Decorator;
                    if (((flags & ModifierFlags.Modifier) == 0))
                    {
                        hasLeadingDecorators = true;
                    }
                    else
                    {
                        if (((flags & ModifierFlags.Export) != 0))
                        {
                            sawExportBeforeDecorators = true;
                        }
                    }

                    if ((firstDecorator == null))
                    {
                        firstDecorator = modifier;
                    }
                }
                else
                {
                    if ((modifier.Kind != Kind.ReadonlyKeyword))
                    {
                        if (((node.Kind == Kind.PropertySignature) || (node.Kind == Kind.MethodSignature)))
                        {
                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_type_member, TypeScript.Scanner.Scanner.TokenToString(modifier.Kind));
                        }

                        if (((node.Kind == Kind.IndexSignature) && (((modifier.Kind != Kind.StaticKeyword) || !Ast.IsClassLike(node.Parent)))))
                        {
                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_an_index_signature, TypeScript.Scanner.Scanner.TokenToString(modifier.Kind));
                        }
                    }

                    if ((((modifier.Kind != Kind.InKeyword) && (modifier.Kind != Kind.OutKeyword)) && (modifier.Kind != Kind.ConstKeyword)))
                    {
                        if ((node.Kind == Kind.TypeParameter))
                        {
                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_type_parameter, TypeScript.Scanner.Scanner.TokenToString(modifier.Kind));
                        }
                    }

                    switch (modifier.Kind)
                    {
                        case Kind.ConstKeyword:
                        {
                            if (((node.Kind != Kind.EnumDeclaration) && (node.Kind != Kind.TypeParameter)))
                            {
                                return this.grammarErrorOnNode(node, Diagnostics.A_class_member_cannot_have_the_0_keyword, TypeScript.Scanner.Scanner.TokenToString(Kind.ConstKeyword));
                            }

                            var parent = node.Parent;
                            if ((node.Kind == Kind.TypeParameter))
                            {
                                if (!(((((((Ast.IsFunctionLikeDeclaration(parent) || Ast.IsClassLike(parent)) || Ast.IsFunctionTypeNode(parent)) || Ast.IsConstructorTypeNode(parent)) || Ast.IsCallSignatureDeclaration(parent)) || Ast.IsConstructSignatureDeclaration(parent)) || Ast.IsMethodSignatureDeclaration(parent))))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_can_only_appear_on_a_type_parameter_of_a_function_method_or_class, TypeScript.Scanner.Scanner.TokenToString(modifier.Kind));
                                }
                            }

                            break;
                        }

                        case Kind.OverrideKeyword:
                        {
                            if (((flags & ModifierFlags.Override) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "override");
                            }
                            else
                            {
                                if (((flags & ModifierFlags.Ambient) != 0))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "override", "declare");
                                }
                                else
                                {
                                    if ((((flags & ModifierFlags.Readonly) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "override", "readonly");
                                    }
                                    else
                                    {
                                        if ((((flags & ModifierFlags.Accessor) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "override", "accessor");
                                        }
                                        else
                                        {
                                            if ((((flags & ModifierFlags.Async) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                            {
                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "override", "async");
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Override;
                            lastOverride = modifier;
                            break;
                        }

                        case Kind.PublicKeyword:
                        case Kind.ProtectedKeyword:
                        case Kind.PrivateKeyword:
                        {
                            var text = visibilityToString(Ast.ModifierToFlag(modifier.Kind));
                            if (((flags & ModifierFlags.AccessibilityModifier) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.Accessibility_modifier_already_seen);
                            }
                            else
                            {
                                if ((((flags & ModifierFlags.Override) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "override");
                                }
                                else
                                {
                                    if ((((flags & ModifierFlags.Static) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "static");
                                    }
                                    else
                                    {
                                        if ((((flags & ModifierFlags.Accessor) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "accessor");
                                        }
                                        else
                                        {
                                            if ((((flags & ModifierFlags.Readonly) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                            {
                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "readonly");
                                            }
                                            else
                                            {
                                                if ((((flags & ModifierFlags.Async) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                                {
                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "async");
                                                }
                                                else
                                                {
                                                    if (((node.Parent.Kind == Kind.ModuleBlock) || (node.Parent.Kind == Kind.SourceFile)))
                                                    {
                                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_module_or_namespace_element, text);
                                                    }
                                                    else
                                                    {
                                                        if (((flags & ModifierFlags.Abstract) != 0))
                                                        {
                                                            if ((modifier.Kind == Kind.PrivateKeyword))
                                                            {
                                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, text, "abstract");
                                                            }
                                                            else
                                                            {
                                                                if (((modifier.Flags & NodeFlags.Reparsed) == 0))
                                                                {
                                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, text, "abstract");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (Ast.IsPrivateIdentifierClassElementDeclaration(node))
                                                            {
                                                                return this.grammarErrorOnNode(modifier, Diagnostics.An_accessibility_modifier_cannot_be_used_with_a_private_identifier);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= Ast.ModifierToFlag(modifier.Kind);
                            break;
                        }

                        case Kind.StaticKeyword:
                        {
                            if (((flags & ModifierFlags.Static) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "static");
                            }
                            else
                            {
                                if ((((flags & ModifierFlags.Readonly) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "static", "readonly");
                                }
                                else
                                {
                                    if ((((flags & ModifierFlags.Async) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "static", "async");
                                    }
                                    else
                                    {
                                        if ((((flags & ModifierFlags.Accessor) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "static", "accessor");
                                        }
                                        else
                                        {
                                            if (((node.Parent.Kind == Kind.ModuleBlock) || (node.Parent.Kind == Kind.SourceFile)))
                                            {
                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_module_or_namespace_element, "static");
                                            }
                                            else
                                            {
                                                if ((node.Kind == Kind.Parameter))
                                                {
                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_parameter, "static");
                                                }
                                                else
                                                {
                                                    if (((flags & ModifierFlags.Abstract) != 0))
                                                    {
                                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "static", "abstract");
                                                    }
                                                    else
                                                    {
                                                        if ((((flags & ModifierFlags.Override) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                                        {
                                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "static", "override");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Static;
                            lastStatic = modifier;
                            break;
                        }

                        case Kind.AccessorKeyword:
                        {
                            if (((flags & ModifierFlags.Accessor) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "accessor");
                            }
                            else
                            {
                                if (((flags & ModifierFlags.Readonly) != 0))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "accessor", "readonly");
                                }
                                else
                                {
                                    if (((flags & ModifierFlags.Ambient) != 0))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "accessor", "declare");
                                    }
                                    else
                                    {
                                        if ((node.Kind != Kind.PropertyDeclaration))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_accessor_modifier_can_only_appear_on_a_property_declaration);
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Accessor;
                            break;
                        }

                        case Kind.ReadonlyKeyword:
                        {
                            if (((flags & ModifierFlags.Readonly) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "readonly");
                            }
                            else
                            {
                                if (((((node.Kind != Kind.PropertyDeclaration) && (node.Kind != Kind.PropertySignature)) && (node.Kind != Kind.IndexSignature)) && (node.Kind != Kind.Parameter)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_readonly_modifier_can_only_appear_on_a_property_declaration_or_index_signature);
                                }
                                else
                                {
                                    if (((flags & ModifierFlags.Accessor) != 0))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "readonly", "accessor");
                                    }
                                }
                            }

                            flags |= ModifierFlags.Readonly;
                            break;
                        }

                        case Kind.ExportKeyword:
                        {
                            // grammarchecks.go:403. Top-level CommonJS value exports
                            // retain the original verbatimModuleSyntax diagnostic.
                            if (compilerOptions.VerbatimModuleSyntax == Tristate.True && (node.Flags & NodeFlags.Ambient) == 0 &&
                                node.Kind is not (Kind.TypeAliasDeclaration or Kind.InterfaceDeclaration or Kind.ModuleDeclaration) &&
                                node.Parent.Kind == Kind.SourceFile &&
                                (program?.GetEmitModuleFormatOfFile(Ast.GetSourceFileOfNode(node)) ?? moduleKind) == ModuleKind.CommonJS)
                                return grammarErrorOnNode(modifier, Diagnostics.A_top_level_export_modifier_cannot_be_used_on_value_declarations_in_a_CommonJS_module_when_verbatimModuleSyntax_is_enabled);
                            if (((flags & ModifierFlags.Export) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "export");
                            }
                            else
                            {
                                if ((((flags & ModifierFlags.Ambient) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "export", "declare");
                                }
                                else
                                {
                                    if ((((flags & ModifierFlags.Abstract) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "export", "abstract");
                                    }
                                    else
                                    {
                                        if ((((flags & ModifierFlags.Async) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "export", "async");
                                        }
                                        else
                                        {
                                            if ((Ast.IsClassLike(node.Parent) && !Ast.IsJSTypeAliasDeclaration(node)))
                                            {
                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_class_elements_of_this_kind, "export");
                                            }
                                            else
                                            {
                                                if ((node.Kind == Kind.Parameter))
                                                {
                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_parameter, "export");
                                                }
                                                else
                                                {
                                                    if ((blockScopeKind == NodeFlags.Using))
                                                    {
                                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_using_declaration, "export");
                                                    }
                                                    else
                                                    {
                                                        if ((blockScopeKind == NodeFlags.AwaitUsing))
                                                        {
                                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_an_await_using_declaration, "export");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Export;
                            break;
                        }

                        case Kind.DefaultKeyword:
                        {
                            Node container = default;
                            if ((node.Parent.Kind == Kind.SourceFile))
                            {
                                container = node.Parent;
                            }
                            else
                            {
                                container = node.Parent.Parent;
                            }

                            if (((container.Kind == Kind.ModuleDeclaration) && !Ast.IsAmbientModule(container)))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.A_default_export_can_only_be_used_in_an_ECMAScript_style_module);
                            }
                            else
                            {
                                if ((blockScopeKind == NodeFlags.Using))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_using_declaration, "default");
                                }
                                else
                                {
                                    if ((blockScopeKind == NodeFlags.AwaitUsing))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_an_await_using_declaration, "default");
                                    }
                                    else
                                    {
                                        if ((((flags & ModifierFlags.Export) == 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "export", "default");
                                        }
                                        else
                                        {
                                            if (sawExportBeforeDecorators)
                                            {
                                                return this.grammarErrorOnNode(firstDecorator, Diagnostics.Decorators_are_not_valid_here);
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Default;
                            break;
                        }

                        case Kind.DeclareKeyword:
                        {
                            if (((flags & ModifierFlags.Ambient) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "declare");
                            }
                            else
                            {
                                if (((flags & ModifierFlags.Async) != 0))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_in_an_ambient_context, "async");
                                }
                                else
                                {
                                    if (((flags & ModifierFlags.Override) != 0))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_in_an_ambient_context, "override");
                                    }
                                    else
                                    {
                                        if ((Ast.IsClassLike(node.Parent) && !Ast.IsPropertyDeclaration(node)))
                                        {
                                            return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_class_elements_of_this_kind, "declare");
                                        }
                                        else
                                        {
                                            if ((node.Kind == Kind.Parameter))
                                            {
                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_parameter, "declare");
                                            }
                                            else
                                            {
                                                if ((blockScopeKind == NodeFlags.Using))
                                                {
                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_using_declaration, "declare");
                                                }
                                                else
                                                {
                                                    if ((blockScopeKind == NodeFlags.AwaitUsing))
                                                    {
                                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_an_await_using_declaration, "declare");
                                                    }
                                                    else
                                                    {
                                                        if (((((node.Parent.Flags & NodeFlags.Ambient) != 0)) && (node.Parent.Kind == Kind.ModuleBlock)))
                                                        {
                                                            return this.grammarErrorOnNode(modifier, Diagnostics.A_declare_modifier_cannot_be_used_in_an_already_ambient_context);
                                                        }
                                                        else
                                                        {
                                                            if (Ast.IsPrivateIdentifierClassElementDeclaration(node))
                                                            {
                                                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_a_private_identifier, "declare");
                                                            }
                                                            else
                                                            {
                                                                if (((flags & ModifierFlags.Accessor) != 0))
                                                                {
                                                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "declare", "accessor");
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            flags |= ModifierFlags.Ambient;
                            lastDeclare = modifier;
                            break;
                        }

                        case Kind.AbstractKeyword:
                        {
                            if (((flags & ModifierFlags.Abstract) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "abstract");
                            }

                            if (((node.Kind != Kind.ClassDeclaration) && (node.Kind != Kind.ConstructorType)))
                            {
                                if (((((node.Kind != Kind.MethodDeclaration) && (node.Kind != Kind.PropertyDeclaration)) && (node.Kind != Kind.GetAccessor)) && (node.Kind != Kind.SetAccessor)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_abstract_modifier_can_only_appear_on_a_class_method_or_property_declaration);
                                }

                                if (!(((node.Parent.Kind == Kind.ClassDeclaration) && Ast.HasSyntacticModifier(node.Parent, ModifierFlags.Abstract))))
                                {
                                    Message message = default;
                                    if ((node.Kind == Kind.PropertyDeclaration))
                                    {
                                        message = Diagnostics.Abstract_properties_can_only_appear_within_an_abstract_class;
                                    }
                                    else
                                    {
                                        message = Diagnostics.Abstract_methods_can_only_appear_within_an_abstract_class;
                                    }

                                    return this.grammarErrorOnNode(modifier, message);
                                }

                                if (((flags & ModifierFlags.Static) != 0))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "static", "abstract");
                                }

                                if (((flags & ModifierFlags.Private) != 0))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "private", "abstract");
                                }

                                if ((((flags & ModifierFlags.Async) != 0) && (lastAsync != null)))
                                {
                                    return this.grammarErrorOnNode(lastAsync, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "async", "abstract");
                                }

                                if ((((flags & ModifierFlags.Override) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "abstract", "override");
                                }

                                if ((((flags & ModifierFlags.Accessor) != 0) && ((modifier.Flags & NodeFlags.Reparsed) == 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "abstract", "accessor");
                                }
                            }

                            {
                                var name = node.Name();
                                if (((name != null) && (name.Kind == Kind.PrivateIdentifier)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_a_private_identifier, "abstract");
                                }
                            }

                            flags |= ModifierFlags.Abstract;
                            break;
                        }

                        case Kind.AsyncKeyword:
                        {
                            if (((flags & ModifierFlags.Async) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "async");
                            }
                            else
                            {
                                if ((((flags & ModifierFlags.Ambient) != 0) || ((node.Parent.Flags & NodeFlags.Ambient) != 0)))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_in_an_ambient_context, "async");
                                }
                                else
                                {
                                    if ((node.Kind == Kind.Parameter))
                                    {
                                        return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_appear_on_a_parameter, "async");
                                    }
                                }
                            }

                            if (((flags & ModifierFlags.Abstract) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_cannot_be_used_with_1_modifier, "async", "abstract");
                            }

                            flags |= ModifierFlags.Async;
                            lastAsync = modifier;
                            break;
                        }

                        case Kind.InKeyword:
                        case Kind.OutKeyword:
                        {
                            ModifierFlags inOutFlag = default;
                            if ((modifier.Kind == Kind.InKeyword))
                            {
                                inOutFlag = ModifierFlags.In;
                            }
                            else
                            {
                                inOutFlag = ModifierFlags.Out;
                            }

                            string inOutText = "";
                            if ((modifier.Kind == Kind.InKeyword))
                            {
                                inOutText = "in";
                            }
                            else
                            {
                                inOutText = "out";
                            }

                            var parent2 = node.Parent;
                            if (((node.Kind != Kind.TypeParameter) || ((parent2 != null) && !(((Ast.IsInterfaceDeclaration(parent2) || Ast.IsClassLike(parent2)) || Ast.IsTypeOrJSTypeAliasDeclaration(parent2))))))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_can_only_appear_on_a_type_parameter_of_a_class_interface_or_type_alias, inOutText);
                            }

                            if (((flags & inOutFlag) != 0))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, inOutText);
                            }

                            if ((((inOutFlag & ModifierFlags.In) != 0) && ((flags & ModifierFlags.Out) != 0)))
                            {
                                return this.grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_must_precede_1_modifier, "in", "out");
                            }

                            flags |= inOutFlag;
                            break;
                        }
                    }
                }
            }
        }

        if ((node.Kind == Kind.Constructor))
        {
            if (((flags & ModifierFlags.Static) != 0))
            {
                return this.grammarErrorOnNode(lastStatic, Diagnostics.X_0_modifier_cannot_appear_on_a_constructor_declaration, "static");
            }

            if (((flags & ModifierFlags.Override) != 0))
            {
                return this.grammarErrorOnNode(lastOverride, Diagnostics.X_0_modifier_cannot_appear_on_a_constructor_declaration, "override");
            }

            if (((flags & ModifierFlags.Async) != 0))
            {
                return this.grammarErrorOnNode(lastAsync, Diagnostics.X_0_modifier_cannot_appear_on_a_constructor_declaration, "async");
            }

            return false;
        }
        else
        {
            if ((((((node.Kind == Kind.ImportDeclaration) || (node.Kind == Kind.JSImportDeclaration)) || (node.Kind == Kind.ImportEqualsDeclaration))) && ((flags & ModifierFlags.Ambient) != 0)))
            {
                return this.grammarErrorOnNode(lastDeclare, Diagnostics.A_0_modifier_cannot_be_used_with_an_import_declaration, "declare");
            }
            else
            {
                if ((((node.Kind == Kind.Parameter) && (((flags & ModifierFlags.ParameterPropertyModifier) != 0))) && Ast.IsBindingPattern(node.Name())))
                {
                    return this.grammarErrorOnNode(node, Diagnostics.A_parameter_property_may_not_be_declared_using_a_binding_pattern);
                }
                else
                {
                    if ((((node.Kind == Kind.Parameter) && (((flags & ModifierFlags.ParameterPropertyModifier) != 0))) && (node.AsParameterDeclaration().DotDotDotToken != null)))
                    {
                        return this.grammarErrorOnNode(node, Diagnostics.A_parameter_property_cannot_be_declared_using_a_rest_parameter);
                    }
                }
            }
        }

        if (((flags & ModifierFlags.Async) != 0))
        {
            return checkGrammarAsyncModifier(node, lastAsync);
        }

        return false;
    }

    // grammarchecks.go:659. The enclosing modifier walk owns modifier order.
    private bool checkGrammarAsyncModifier(Node node, Node asyncModifier)
    {
        switch (node.Kind)
        {
            case Kind.MethodDeclaration:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
                return false;
        }
        return grammarErrorOnNode(asyncModifier, Diagnostics.X_0_modifier_cannot_be_used_here, "async");
    }

    // grammarchecks.go:1658. Parse-error policy differs deliberately for static
    // blocks/parameter initializers and contextual/top-level diagnostics.
    private bool checkGrammarAwaitOrAwaitUsing(Node node)
    {
        bool hasError = false;
        var container = getContainingFunctionOrClassStaticBlock(node);
        if (container != null && Ast.IsClassStaticBlockDeclaration(container))
        {
            error(node, Ast.IsAwaitExpression(node)
                ? Diagnostics.X_await_expression_cannot_be_used_inside_a_class_static_block
                : Diagnostics.X_await_using_statements_cannot_be_used_inside_a_class_static_block);
            hasError = true;
        }
        else if ((node.Flags & NodeFlags.AwaitContext) == 0)
        {
            var sourceFile = Ast.GetSourceFileOfNode(node);
            if (Ast.IsInTopLevelContext(node))
            {
                if (!hasParseDiagnostics(sourceFile))
                {
                    TextRange span = default;
                    bool spanCalculated = false;
                    if (!Ast.IsEffectiveExternalModule(sourceFile, compilerOptions))
                    {
                        span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(sourceFile, node.Pos());
                        spanCalculated = true;
                        addDiagnostic(Ast.NewDiagnostic(sourceFile, span, Ast.IsAwaitExpression(node)
                            ? Diagnostics.X_await_expressions_are_only_allowed_at_the_top_level_of_a_file_when_that_file_is_a_module_but_this_file_has_no_imports_or_exports_Consider_adding_an_empty_export_to_make_this_file_a_module
                            : Diagnostics.X_await_using_statements_are_only_allowed_at_the_top_level_of_a_file_when_that_file_is_a_module_but_this_file_has_no_imports_or_exports_Consider_adding_an_empty_export_to_make_this_file_a_module));
                        hasError = true;
                    }
                    switch (moduleKind)
                    {
                        case ModuleKind.Node16:
                        case ModuleKind.Node18:
                        case ModuleKind.Node20:
                        case ModuleKind.NodeNext:
                            if (program.GetSourceFileMetaData(PathKey.FromValue(sourceFile.PathKeyValue())).ImpliedNodeFormat == ResolutionMode.CommonJS)
                            {
                                if (!spanCalculated) span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(sourceFile, node.Pos());
                                addDiagnostic(Ast.NewDiagnostic(sourceFile, span, Diagnostics.The_current_file_is_a_CommonJS_module_and_cannot_use_await_at_the_top_level));
                                hasError = true;
                                break;
                            }
                            goto case ModuleKind.ES2022;
                        case ModuleKind.ES2022:
                        case ModuleKind.ESNext:
                        case ModuleKind.Preserve:
                        case ModuleKind.System:
                            if (languageVersion >= ScriptTarget.ES2017) break;
                            goto default;
                        default:
                            if (!spanCalculated) span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(sourceFile, node.Pos());
                            addDiagnostic(Ast.NewDiagnostic(sourceFile, span, Ast.IsAwaitExpression(node)
                                ? Diagnostics.Top_level_await_expressions_are_only_allowed_when_the_module_option_is_set_to_es2022_esnext_system_node16_node18_node20_nodenext_or_preserve_and_the_target_option_is_set_to_es2017_or_higher
                                : Diagnostics.Top_level_await_using_statements_are_only_allowed_when_the_module_option_is_set_to_es2022_esnext_system_node16_node18_node20_nodenext_or_preserve_and_the_target_option_is_set_to_es2017_or_higher));
                            hasError = true;
                            break;
                    }
                }
            }
            else if (!hasParseDiagnostics(sourceFile))
            {
                var span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(sourceFile, node.Pos());
                var diagnostic = Ast.NewDiagnostic(sourceFile, span, Ast.IsAwaitExpression(node)
                    ? Diagnostics.X_await_expressions_are_only_allowed_within_async_functions_and_at_the_top_levels_of_modules
                    : Diagnostics.X_await_using_statements_are_only_allowed_within_async_functions_and_at_the_top_levels_of_modules);
                if (container != null && container.Kind != Kind.Constructor && !hasAsyncModifier(container))
                    diagnostic.AddRelatedInfo(createDiagnosticForNode(container, Diagnostics.Did_you_mean_to_mark_this_function_as_async));
                addDiagnostic(diagnostic);
                hasError = true;
            }
        }
        if (Ast.IsAwaitExpression(node) && isInParameterInitializerBeforeContainingFunction(node))
        {
            error(node, Diagnostics.X_await_expressions_cannot_be_used_in_a_parameter_initializer);
            hasError = true;
        }
        return hasError;
    }

    // grammarchecks.go:1759. Context and parameter-initializer errors both publish.
    private bool checkGrammarYieldExpression(Node node)
    {
        bool hasError = false;
        if ((node.Flags & NodeFlags.YieldContext) == 0)
        {
            grammarErrorOnFirstToken(node, Diagnostics.A_yield_expression_is_only_allowed_in_a_generator_body);
            hasError = true;
        }
        if (isInParameterInitializerBeforeContainingFunction(node))
        {
            error(node, Diagnostics.X_yield_expressions_cannot_be_used_in_a_parameter_initializer);
            hasError = true;
        }
        return hasError;
    }

    private bool checkGrammarSourceFile(SourceFile node)
    {
        return (((node.Flags & NodeFlags.Ambient) != 0) && this.checkGrammarTopLevelElementsForRequiredDeclareModifier(node));
    }

    private bool checkGrammarStatementInAmbientContext(Node node)
    {
        if (((node.Flags & NodeFlags.Ambient) != 0))
        {
            ref var links = ref this.nodeLinks.Get(node);
            if ((!links.hasReportedStatementInAmbientContext && ((Ast.IsFunctionLike(node.Parent) || Ast.IsAccessor(node.Parent)))))
            {
                links.hasReportedStatementInAmbientContext = this.grammarErrorOnFirstToken(node, Diagnostics.An_implementation_cannot_be_declared_in_ambient_contexts);
                return links.hasReportedStatementInAmbientContext;
            }

            if ((((node.Parent.Kind == Kind.Block) || (node.Parent.Kind == Kind.ModuleBlock)) || (node.Parent.Kind == Kind.SourceFile)))
            {
                ref var links2 = ref this.nodeLinks.Get(node.Parent);
                if (!links2.hasReportedStatementInAmbientContext)
                {
                    links2.hasReportedStatementInAmbientContext = this.grammarErrorOnFirstToken(node, Diagnostics.Statements_are_not_allowed_in_ambient_contexts);
                    return links2.hasReportedStatementInAmbientContext;
                }
            }
            else
            {
            }
        }

        return false;
    }

    private bool checkGrammarForDisallowedTrailingComma(NodeList list, Message message)
    {
        if (list != null && list.HasTrailingComma())
            return grammarErrorAtPos(list.Nodes[0], list.End() - 1, 1, message);
        return false;
    }

    private bool checkGrammarVariableDeclarationList(Node node)
    {
        var declarations = node.AsVariableDeclarationList().Declarations;
        if (checkGrammarForDisallowedTrailingComma(declarations, Diagnostics.Trailing_comma_not_allowed))
            return true;
        if ((declarations.Nodes.Count) == 0)
            return grammarErrorAtPos(node, declarations.Pos(), declarations.End() - declarations.Pos(), Diagnostics.Variable_declaration_list_cannot_be_empty);
        var flags = node.Flags & NodeFlags.BlockScoped;
        if (flags is NodeFlags.Using or NodeFlags.AwaitUsing)
        {
            if (Ast.IsForInStatement(node.Parent))
                return grammarErrorOnNode(node, flags == NodeFlags.Using
                    ? Diagnostics.The_left_hand_side_of_a_for_in_statement_cannot_be_a_using_declaration
                    : Diagnostics.The_left_hand_side_of_a_for_in_statement_cannot_be_an_await_using_declaration);
            if ((node.Flags & NodeFlags.Ambient) != 0)
                return grammarErrorOnNode(node, flags == NodeFlags.Using
                    ? Diagnostics.X_using_declarations_are_not_allowed_in_ambient_contexts
                    : Diagnostics.X_await_using_declarations_are_not_allowed_in_ambient_contexts);
            if (Ast.IsVariableStatement(node.Parent) && (Ast.IsCaseClause(node.Parent.Parent) || Ast.IsDefaultClause(node.Parent.Parent)))
                return grammarErrorOnNode(node, flags == NodeFlags.Using
                    ? Diagnostics.X_using_declarations_are_not_allowed_in_case_or_default_clauses_unless_contained_within_a_block
                    : Diagnostics.X_await_using_declarations_are_not_allowed_in_case_or_default_clauses_unless_contained_within_a_block);
        }
        return flags == NodeFlags.AwaitUsing && checkGrammarAwaitOrAwaitUsing(node);
    }

    // grammarchecks.go:1518. Rest binding syntax also applies to ambient signatures.
    private bool checkGrammarBindingElement(BindingElement node)
    {
        var declaration = node.AsNode();
        if (node.DotDotDotToken != null)
        {
            var elements = declaration.Parent.ElementList();
            if (declaration != Core.LastOrNil(elements.Nodes))
                return grammarErrorOnNode(declaration, Diagnostics.A_rest_element_must_be_last_in_a_destructuring_pattern);
            checkGrammarForDisallowedTrailingComma(elements, Diagnostics.A_rest_parameter_or_binding_pattern_may_not_have_a_trailing_comma);
            if (node.PropertyName != null)
                return grammarErrorOnNode(node.Name(), Diagnostics.A_rest_element_cannot_have_a_property_name);
        }
        if (node.DotDotDotToken != null && node.Initializer != null)
            return grammarErrorAtPos(declaration, node.Initializer.Pos() - 1, 1, Diagnostics.A_rest_element_cannot_have_an_initializer);
        return false;
    }

    private bool checkGrammarVariableDeclaration(Node node)
    {
        var flags = getCombinedNodeFlagsCached(node);
        var scope = flags & NodeFlags.BlockScoped;
        if (Ast.IsBindingPattern(node.Name()))
        {
            if (scope == NodeFlags.AwaitUsing)
                return grammarErrorOnNode(node, Diagnostics.X_0_declarations_may_not_have_binding_patterns, "await using");
            if (scope == NodeFlags.Using)
                return grammarErrorOnNode(node, Diagnostics.X_0_declarations_may_not_have_binding_patterns, "using");
        }
        if (node.Parent.Parent.Kind != Kind.ForInStatement && node.Parent.Parent.Kind != Kind.ForOfStatement)
        {
            if ((flags & NodeFlags.Ambient) != 0)
                checkAmbientInitializer(node);
            else if (node.Initializer() == null)
            {
                if (Ast.IsBindingPattern(node.Name()) && !Ast.IsBindingPattern(node.Parent))
                    return grammarErrorOnNode(node, Diagnostics.A_destructuring_declaration_must_have_an_initializer);
                if (scope == NodeFlags.AwaitUsing || scope == NodeFlags.Using || scope == NodeFlags.Const)
                    return grammarErrorOnNode(node, Diagnostics.X_0_declarations_must_be_initialized,
                        scope == NodeFlags.AwaitUsing ? "await using" : scope == NodeFlags.Using ? "using" : "const");
            }
        }

        var exclamation = node.AsVariableDeclaration().ExclamationToken;
        if (exclamation != null && (node.Parent.Parent.Kind != Kind.VariableStatement || node.Type() == null || node.Initializer() != null || (flags & NodeFlags.Ambient) != 0))
        {
            var message = node.Initializer() != null ? Diagnostics.Declarations_with_initializers_cannot_also_have_definite_assignment_assertions : node.Type() == null ? Diagnostics.Declarations_with_definite_assignment_assertions_must_also_have_type_annotations : Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context;
            return grammarErrorOnNode(exclamation, message);
        }

        // Named hostless test adapters retain their captured module kind; genuine Programs supply the source host.
        if ((program?.GetEmitModuleFormatOfFile(Ast.GetSourceFileOfNode(node)) ?? moduleKind) < ModuleKind.System &&
            (node.Parent.Parent.Flags & NodeFlags.Ambient) == 0 && Ast.HasSyntacticModifier(node.Parent.Parent, ModifierFlags.Export))
            checkGrammarForEsModuleMarkerInBindingName(node.Name());
        return scope != 0 && checkGrammarNameInLetOrConstDeclarations(node.Name());
    }

    private bool checkGrammarForEsModuleMarkerInBindingName(Node name)
    {
        if (Ast.IsIdentifier(name))
        {
            if (name.TextEquals("__esModule"u8))
                return grammarErrorOnNodeSkippedOnNoEmit(name, Diagnostics.Identifier_expected_esModule_is_reserved_as_an_exported_marker_when_transforming_ECMAScript_modules);
        }
        else
        {
            foreach (var element in name.Elements())
                if (element.Name() != null)
                    return checkGrammarForEsModuleMarkerInBindingName(element.Name());
        }
        return false;
    }

    private bool checkGrammarNameInLetOrConstDeclarations(Node name)
    {
        if (name.Kind == Kind.Identifier)
        {
            if (name.TextEquals("let"u8))
                return grammarErrorOnNode(name, Diagnostics.X_let_is_not_allowed_to_be_used_as_a_name_in_let_or_const_declarations);
        }
        else
            foreach (var element in name.Elements())
                if (element.AsBindingElement().Name() != null)
                    checkGrammarNameInLetOrConstDeclarations(element.Name());
        return false;
    }

    private bool checkGrammarForDisallowedBlockScopedVariableStatement(Node node)
    {
        if (!containerAllowsBlockScopedVariable(node.Parent))
        {
            var blockScopeKind = getCombinedNodeFlagsCached(node.AsVariableStatement().DeclarationList) & NodeFlags.BlockScoped;
            if (blockScopeKind != 0)
            {
                var keyword = blockScopeKind switch
                {
                    NodeFlags.Let => "let",
                    NodeFlags.Const => "const",
                    NodeFlags.Using => "using",
                    NodeFlags.AwaitUsing => "await using",
                    _ => throw new InvalidOperationException("Unknown BlockScope flag")};
                error(node, Diagnostics.X_0_declarations_can_only_be_declared_inside_a_block, keyword);
            }
        }

        return false;
    }

    // grammarchecks.go:1796. Labels inherit their containing statement's
    // block-scoped declaration policy instead of always rejecting declarations.
    private static bool containerAllowsBlockScopedVariable(Node parent) => parent.Kind switch
    {
        Kind.IfStatement or Kind.DoStatement or Kind.WhileStatement or Kind.WithStatement or
            Kind.ForStatement or Kind.ForInStatement or Kind.ForOfStatement => false,
        Kind.LabeledStatement => containerAllowsBlockScopedVariable(parent.Parent),
        _ => true
    };
    private void checkGrammarNumericLiteral(Node node)
    {
        var nodeText = TypeScript.Scanner.Scanner.GetTextOfNodeValue(node);
        bool fractional = nodeText.Contains("."u8);
        bool scientific = (node.AsNumericLiteral().TokenFlags & TokenFlags.Scientific) != 0;
        if (fractional || scientific)
            return;
        var value = JsNum.FromString(node.TextValue());
        if (value.Value <= 9007199254740991)
            return;
        // This upstream path produces a suggestion, not a file semantic error.
        suggestionDiagnostics.Add(NewDiagnosticForNode(node, Diagnostics.Numeric_literals_with_absolute_values_equal_to_2_53_or_greater_are_too_large_to_be_represented_accurately_as_integers));
    }

    private readonly DiagnosticsCollection suggestionDiagnostics = new();
}

public sealed partial class Checker
{
    private bool checkGrammarObjectLiteralExpression(Node node, bool inDestructuring = false)
    {
        var seen = new Dictionary<ByteStringKey, DeclarationMeaning>(ByteStringKey.Comparer);
        Slice<Node> properties = default;
        if ((node.PropertyList() != null))
        {
            properties = node.PropertyList().Nodes;
        }

        {
            foreach (var prop in GoRuntime.RangeValues(properties))
            {
                if ((prop.Kind == Kind.SpreadAssignment))
                {
                    var spreadAssignment = prop.AsSpreadAssignment();
                    if (inDestructuring)
                    {
                        var expression = Ast.SkipParentheses(spreadAssignment.Expression);
                        if ((Ast.IsArrayLiteralExpression(expression) || Ast.IsObjectLiteralExpression(expression)))
                        {
                            return this.grammarErrorOnNode(spreadAssignment.Expression, Diagnostics.A_rest_element_cannot_contain_a_binding_pattern);
                        }
                    }

                    continue;
                }

                var name = prop.Name();
                if ((name.Kind == Kind.ComputedPropertyName))
                {
                    this.checkGrammarComputedPropertyName(name);
                }

                if (((prop.Kind == Kind.ShorthandPropertyAssignment) && !inDestructuring))
                {
                    var shorthandProp = prop.AsShorthandPropertyAssignment();
                    if ((shorthandProp.ObjectAssignmentInitializer != null))
                    {
                        Node lastNodeBeforeInitializer = default;
                        shorthandProp.ForEachChild((child) =>
                        {
                            if ((child != shorthandProp.ObjectAssignmentInitializer))
                            {
                                lastNodeBeforeInitializer = child;
                                return false;
                            }

                            return true;
                        });
                        this.grammarErrorOnFirstToken(lastNodeBeforeInitializer, Diagnostics.Did_you_mean_to_use_a_Colon_An_can_only_follow_a_property_name_when_the_containing_object_literal_is_part_of_a_destructuring_pattern);
                    }
                }

                if ((name.Kind == Kind.PrivateIdentifier))
                {
                    this.grammarErrorOnNode(name, Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
                }

                {
                    var modifiers = prop.ModifierNodes();
                    if ((GoRuntime.Len(modifiers) != 0))
                    {
                        if (Ast.CanHaveModifiers(prop))
                        {
                            {
                                foreach (var range2 in GoRuntime.Range(modifiers))
                                {
                                    var _2 = range2.Key;
                                    var mod = range2.Value;
                                    if ((Ast.IsModifier(mod) && (((mod.Kind != Kind.AsyncKeyword) || (prop.Kind != Kind.MethodDeclaration)))))
                                    {
                                        this.grammarErrorOnNode(mod, Diagnostics.X_0_modifier_cannot_be_used_here, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(mod));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Ast.CanHaveIllegalModifiers(prop))
                            {
                                {
                                    foreach (var range3 in GoRuntime.Range(modifiers))
                                    {
                                        var _3 = range3.Key;
                                        var mod2 = range3.Value;
                                        if (Ast.IsModifier(mod2))
                                        {
                                            this.grammarErrorOnNode(mod2, Diagnostics.X_0_modifier_cannot_be_used_here, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(mod2));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                DeclarationMeaning currentKind = default;
                switch (prop.Kind)
                {
                    case Kind.ShorthandPropertyAssignment:
                    case Kind.PropertyAssignment:
                    {
                        NamedMemberBase commonProp = default;
                        if ((prop.Kind == Kind.ShorthandPropertyAssignment))
                        {
                            prop.ClassLikeData();
                            commonProp = prop.AsShorthandPropertyAssignment();
                        }
                        else
                        {
                            commonProp = prop.AsPropertyAssignment();
                        }

                        this.checkGrammarForInvalidExclamationToken(commonProp.PostfixToken, Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context);
                        this.checkGrammarForInvalidQuestionMark(commonProp.PostfixToken, Diagnostics.An_object_member_cannot_be_declared_optional);
                        if ((name.Kind == Kind.NumericLiteral))
                        {
                            this.checkGrammarNumericLiteral(name);
                        }

                        if ((name.Kind == Kind.BigIntLiteral))
                        {
                            this.addErrorOrSuggestion(true, createDiagnosticForNode(name, Diagnostics.A_bigint_literal_cannot_be_used_as_a_property_name));
                        }

                        currentKind = DeclarationMeaning.PropertyAssignment;
                        break;
                    }

                    case Kind.MethodDeclaration:
                    {
                        currentKind = DeclarationMeaning.Method;
                        break;
                    }

                    case Kind.GetAccessor:
                    {
                        currentKind = DeclarationMeaning.GetAccessor;
                        break;
                    }

                    case Kind.SetAccessor:
                    {
                        currentKind = DeclarationMeaning.SetAccessor;
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException("Unexpected node kind " + prop.Kind);
                    }
                }

                if (!inDestructuring)
                {
                    var tuple4 = this.getEffectivePropertyNameForPropertyNameNode(name);
                    var effectiveName = tuple4.Item1;
                    var ok = tuple4.Item2;
                    if (!ok)
                    {
                        continue;
                    }

                    effectiveName.TryGetValue(seen, out var existingKind);
                    if ((existingKind == 0))
                    {
                        seen[ByteStringKey.FromValue(effectiveName)] = currentKind;
                    }
                    else
                    {
                        if (((((currentKind & DeclarationMeaning.Method) != 0)) && (((existingKind & DeclarationMeaning.Method) != 0))))
                        {
                            this.grammarErrorOnNode(name, Diagnostics.Duplicate_identifier_0, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(name));
                        }
                        else
                        {
                            if (((((currentKind & DeclarationMeaning.PropertyAssignment) != 0)) && (((existingKind & DeclarationMeaning.PropertyAssignment) != 0))))
                            {
                                this.grammarErrorOnNode(name, Diagnostics.An_object_literal_cannot_have_multiple_properties_with_the_same_name, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(name));
                            }
                            else
                            {
                                if (((((currentKind & DeclarationMeaning.GetOrSetAccessor) != 0)) && (((existingKind & DeclarationMeaning.GetOrSetAccessor) != 0))))
                                {
                                    if (((existingKind != DeclarationMeaning.GetOrSetAccessor) && (currentKind != existingKind)))
                                    {
                                        seen[ByteStringKey.FromValue(effectiveName)] = (currentKind | existingKind);
                                    }
                                    else
                                    {
                                        return this.grammarErrorOnNode(name, Diagnostics.An_object_literal_cannot_have_multiple_get_Slashset_accessors_with_the_same_name);
                                    }
                                }
                                else
                                {
                                    return this.grammarErrorOnNode(name, Diagnostics.An_object_literal_cannot_have_property_and_accessor_with_the_same_name);
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool checkGrammarProperty(Node node)
    {
        var propertyName = node.Name();
        if (((Ast.IsComputedPropertyName(propertyName) && Ast.IsBinaryExpression(propertyName.Expression())) && (propertyName.Expression().AsBinaryExpression().OperatorToken.Kind == Kind.InKeyword)))
        {
            return this.grammarErrorOnNode(node.Parent.Members()[0], Diagnostics.A_mapped_type_may_not_declare_properties_or_methods);
        }
        if (Ast.IsClassLike(node.Parent))
        {
            if ((Ast.IsStringLiteral(propertyName) && (propertyName.TextEquals("constructor"u8))))
            {
                return this.grammarErrorOnNode(propertyName, Diagnostics.Classes_may_not_have_a_field_named_constructor);
            }
            if (this.checkGrammarForInvalidDynamicName(propertyName, Diagnostics.A_computed_property_name_in_a_class_property_declaration_must_have_a_simple_literal_type_or_a_unique_symbol_type))
            {
                return true;
            }
            if ((Ast.IsAutoAccessorPropertyDeclaration(node) && this.checkGrammarForInvalidQuestionMark(node.PostfixToken(), Diagnostics.An_accessor_property_cannot_be_declared_optional)))
            {
                return true;
            }
        }
        else
        {
            if (Ast.IsInterfaceDeclaration(node.Parent))
            {
                if (this.checkGrammarForInvalidDynamicName(propertyName, Diagnostics.A_computed_property_name_in_an_interface_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type))
                {
                    return true;
                }
                if (!Ast.IsPropertySignatureDeclaration(node))
                {
                    throw new InvalidOperationException("Unexpected node kind: " + node.Kind.String());
                }
                {
                    var initializer = node.Initializer();
                    if ((initializer != null))
                    {
                        return this.grammarErrorOnNode(initializer, Diagnostics.An_interface_property_cannot_have_an_initializer);
                    }
                }
            }
            else
            {
                if (Ast.IsTypeLiteralNode(node.Parent))
                {
                    if (this.checkGrammarForInvalidDynamicName(node.Name(), Diagnostics.A_computed_property_name_in_a_type_literal_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type))
                    {
                        return true;
                    }
                    if (!Ast.IsPropertySignatureDeclaration(node))
                    {
                        throw new InvalidOperationException("Unexpected node kind: " + node.Kind.String());
                    }
                    {
                        var initializer2 = node.Initializer();
                        if ((initializer2 != null))
                        {
                            return this.grammarErrorOnNode(initializer2, Diagnostics.A_type_literal_property_cannot_have_an_initializer);
                        }
                    }
                }
            }
        }
        if (((node.Flags & NodeFlags.Ambient) != 0))
        {
            this.checkAmbientInitializer(node);
        }
        if (Ast.IsPropertyDeclaration(node))
        {
            var propDecl = node.AsPropertyDeclaration();
            var postfixToken = propDecl.PostfixToken;
            if (((postfixToken != null) && (postfixToken.Kind == Kind.ExclamationToken)))
            {
                switch (true)
                {
                    case true when (propDecl.Initializer != null):
                    {
                        return this.grammarErrorOnNode(postfixToken, Diagnostics.Declarations_with_initializers_cannot_also_have_definite_assignment_assertions);
                    }
                    case true when (propDecl.Type == null):
                    {
                        return this.grammarErrorOnNode(postfixToken, Diagnostics.Declarations_with_definite_assignment_assertions_must_also_have_type_annotations);
                    }
                    case true when (((!Ast.IsClassLike(node.Parent) || ((node.Flags & NodeFlags.Ambient) != 0)) || Ast.IsStatic(node)) || Ast.HasAbstractModifier(node)):
                    {
                        return this.grammarErrorOnNode(postfixToken, Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context);
                    }
                }
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    // grammarchecks.go:935. Source parameter/signature restrictions are admission-owned.
    private bool checkGrammarInterfaceDeclaration(InterfaceDeclaration node)
    {
        bool seenExtends = false;
        foreach (var clauseNode in node.HeritageClauses?.Nodes ?? default)
        {
            var clause = clauseNode.AsHeritageClause();
            if (clause.Token == Kind.ExtendsKeyword)
            {
                if (seenExtends)
                    return grammarErrorOnFirstToken(clauseNode, Diagnostics.X_extends_clause_already_seen);
                seenExtends = true;
            }
            else if (clause.Token == Kind.ImplementsKeyword)
                return grammarErrorOnFirstToken(clauseNode, Diagnostics.Interface_declaration_cannot_have_implements_clause);
            else
                throw new InvalidOperationException("Unexpected heritage token");
            checkGrammarHeritageClause(clause);
        }

        return false;
    }

    private bool checkGrammarHeritageClause(HeritageClause node)
    {
        var types = node.Types;
        if (checkGrammarForDisallowedTrailingComma(types, Diagnostics.Trailing_comma_not_allowed))
            return true;
        if (types != null && (types.Nodes.Count) == 0)
            return grammarErrorAtPos(node.AsNode(), types.Pos(), 0, Diagnostics.X_0_list_cannot_be_empty, TypeScript.Scanner.Scanner.TokenToString(node.Token));
        foreach (var element in types?.Nodes ?? default)
            if (checkGrammarExpressionWithTypeArguments(element))
                return true;
        return false;
    }

    private bool checkGrammarExpressionWithTypeArguments(Node node)
    {
        if (node.Kind == Kind.ExpressionWithTypeArguments && node.Expression().Kind == Kind.ImportKeyword && node.TypeArgumentList() != null)
            return grammarErrorOnNode(node, Diagnostics.This_use_of_import_is_invalid_import_calls_can_be_written_but_they_must_have_parentheses_and_cannot_have_type_arguments);
        return checkGrammarTypeArguments(node, node.TypeArgumentList());
    }

    // grammarchecks.go:845. Highlight the actual empty <...> delimiters.
    private bool checkGrammarForAtLeastOneTypeArgument(Node node, NodeList arguments)
    {
        if (arguments != null && (arguments.Nodes.Count) == 0)
        {
            var sourceFile = Ast.GetSourceFileOfNode(node);
            int start = arguments.Pos() - 1;
            int end = TypeScript.Scanner.Scanner.SkipTrivia(sourceFile.TextView(), arguments.End()) + 1;
            return grammarErrorAtPos(sourceFile.AsNode(), start, end - start, Diagnostics.Type_argument_list_cannot_be_empty);
        }
        return false;
    }

    private bool checkGrammarTypeArguments(Node node, NodeList arguments) =>
        checkGrammarForDisallowedTrailingComma(arguments, Diagnostics.Trailing_comma_not_allowed) ||
        checkGrammarForAtLeastOneTypeArgument(node, arguments);
}

public sealed partial class Checker
{
    private bool grammarErrorOnFirstToken(Node node, Message message, params object[] args)
    {
        var file = Ast.GetSourceFileOfNode(node);
        if (file.Diagnostics().Length != 0)
            return false;
        var span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(file, node.Pos());
        addDiagnostic(Ast.NewDiagnostic(file, span, message, args));
        return true;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarTypeParameterList(NodeList typeParameters, SourceFile file)
    {
        if (((typeParameters != null) && (GoRuntime.Len(typeParameters.Nodes) == 0)))
        {
            var start = (typeParameters.Pos() - GoRuntime.Len("<"));
            var end = (TypeScript.Scanner.Scanner.SkipTrivia(file.TextView(), typeParameters.End()) + GoRuntime.Len(">"));
            return this.grammarErrorAtPos(file.AsNode(), start, (end - start), Diagnostics.Type_parameter_list_cannot_be_empty);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarParameterList(NodeList parameters)
    {
        var seenOptionalParameter = false;
        var parameterCount = GoRuntime.Len(parameters.Nodes);
        {
            foreach (var i in GoRuntime.RangeKeys(parameterCount))
            {
                var parameter = parameters.Nodes[i].AsParameterDeclaration();
                if ((parameter.DotDotDotToken != null))
                {
                    if ((i != (parameterCount - 1)))
                    {
                        return this.grammarErrorOnNode(parameter.DotDotDotToken, Diagnostics.A_rest_parameter_must_be_last_in_a_parameter_list);
                    }

                    if (((parameter.Flags & NodeFlags.Ambient) == 0))
                    {
                        this.checkGrammarForDisallowedTrailingComma(parameters, Diagnostics.A_rest_parameter_or_binding_pattern_may_not_have_a_trailing_comma);
                    }

                    if ((parameter.QuestionToken != null))
                    {
                        return this.grammarErrorOnNode(parameter.QuestionToken, Diagnostics.A_rest_parameter_cannot_be_optional);
                    }

                    if ((parameter.Initializer != null))
                    {
                        return this.grammarErrorOnNode(parameter.Name(), Diagnostics.A_rest_parameter_cannot_have_an_initializer);
                    }
                }
                else
                {
                    if (isOptionalDeclaration(parameter.AsNode()))
                    {
                        seenOptionalParameter = true;
                        if ((((parameter.QuestionToken != null) && ((parameter.QuestionToken.Flags & NodeFlags.Reparsed) == 0)) && (parameter.Initializer != null)))
                        {
                            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.Parameter_cannot_have_question_mark_and_initializer);
                        }
                    }
                    else
                    {
                        if ((seenOptionalParameter && (parameter.Initializer == null)))
                        {
                            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.A_required_parameter_cannot_follow_an_optional_parameter);
                        }
                    }
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForUseStrictSimpleParameterList(Node node)
    {
        if ((this.compilerOptions.Target >= ScriptTarget.ES2016))
        {
            var body = node.Body();
            Node useStrictDirective = default;
            if (((body != null) && Ast.IsBlock(body)))
            {
                useStrictDirective = Binder.FindUseStrictPrologue(Ast.GetSourceFileOfNode(node), body.Statements());
            }

            if ((useStrictDirective != null))
            {
                var nonSimpleParameters = Core.Filter(node.Parameters(), (n) =>
                {
                    var parameter = n.AsParameterDeclaration();
                    return (((parameter.Initializer != null) || Ast.IsBindingPattern(parameter.Name())) || isRestParameter(parameter.AsNode()));
                });
                if ((GoRuntime.Len(nonSimpleParameters) != 0))
                {
                    {
                        foreach (var parameter2 in GoRuntime.RangeValues(nonSimpleParameters))
                        {
                            var err = this.error(parameter2, Diagnostics.This_parameter_is_not_allowed_with_use_strict_directive);
                            err.AddRelatedInfo(createDiagnosticForNode(useStrictDirective, Diagnostics.X_use_strict_directive_used_here));
                        }
                    }

                    var err2 = this.error(useStrictDirective, Diagnostics.X_use_strict_directive_cannot_be_used_with_non_simple_parameter_list);
                    {
                        foreach (var range4 in GoRuntime.Range(nonSimpleParameters))
                        {
                            var index = range4.Key;
                            var parameter3 = range4.Value;
                            Message relatedMessage = default;
                            if ((index == 0))
                            {
                                relatedMessage = Diagnostics.Non_simple_parameter_declared_here;
                            }
                            else
                            {
                                relatedMessage = Diagnostics.X_and_here;
                            }

                            err2.AddRelatedInfo(createDiagnosticForNode(parameter3, relatedMessage));
                        }
                    }

                    return true;
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarFunctionLikeDeclaration(Node node)
    {
        var file = Ast.GetSourceFileOfNode(node);
        var funcData = node.FunctionLikeData();
        return ((((this.checkGrammarModifiers(node) || this.checkGrammarTypeParameterList(funcData.TypeParameters, file)) || this.checkGrammarParameterList(funcData.Parameters)) || this.checkGrammarArrowFunction(node, file)) || ((Ast.IsFunctionLikeDeclaration(node) && this.checkGrammarForUseStrictSimpleParameterList(node))));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarArrowFunction(Node node, SourceFile file)
    {
        if (!Ast.IsArrowFunction(node))
        {
            return false;
        }

        var arrowFunc = node.AsArrowFunction();
        var typeParameters = arrowFunc.TypeParameters;
        if ((typeParameters != null))
        {
            var typeParamNodes = typeParameters.Nodes;
            var hasConstraint = ((GoRuntime.Len(typeParamNodes) > 0) && (typeParamNodes[0].AsTypeParameterDeclaration().Constraint != null));
            if (!((((GoRuntime.Len(typeParamNodes) > 1) || typeParameters.HasTrailingComma()) || hasConstraint)))
            {
                if ((file.FileName().EndsWith(".mts", StringComparison.OrdinalIgnoreCase) || file.FileName().EndsWith(".cts", StringComparison.OrdinalIgnoreCase)))
                {
                    this.grammarErrorOnNode(typeParameters.Nodes[0], Diagnostics.This_syntax_is_reserved_in_files_with_the_mts_or_cts_extension_Add_a_trailing_comma_or_explicit_constraint);
                }
            }
        }

        var equalsGreaterThanToken = arrowFunc.EqualsGreaterThanToken;
        var arrowFullText = GoRuntime.Slice(file.TextView(), equalsGreaterThanToken.Pos(), equalsGreaterThanToken.End());
        return (GoStrings.ContainsFunc(arrowFullText.AsSpan(), StringUtil.IsLineBreak) && this.grammarErrorOnNode(equalsGreaterThanToken, Diagnostics.Line_terminator_not_permitted_before_arrow));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForGenerator(Node node)
    {
        {
            var bodyData = node.BodyData();
            if (((bodyData != null) && (bodyData.AsteriskToken != null)))
            {
                if ((((node.Kind != Kind.FunctionDeclaration) && (node.Kind != Kind.FunctionExpression)) && (node.Kind != Kind.MethodDeclaration)))
                {
                    throw new InvalidOperationException("Unexpected node kind " + node.Kind);
                }

                if (((node.Flags & NodeFlags.Ambient) != 0))
                {
                    return this.grammarErrorOnNode(bodyData.AsteriskToken, Diagnostics.Generators_are_not_allowed_in_an_ambient_context);
                }

                if ((bodyData.Body == null))
                {
                    return this.grammarErrorOnNode(bodyData.AsteriskToken, Diagnostics.An_overload_signature_cannot_be_declared_as_a_generator);
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForInvalidQuestionMark(Node postfixToken, Message message)
    {
        return (((postfixToken != null) && (postfixToken.Kind == Kind.QuestionToken)) && this.grammarErrorOnNode(postfixToken, message));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForInvalidExclamationToken(Node postfixToken, Message message)
    {
        return (((postfixToken != null) && (postfixToken.Kind == Kind.ExclamationToken)) && this.grammarErrorOnNode(postfixToken, message));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForInvalidDynamicName(Node node, Message message)
    {
        if (!this.isNonBindableDynamicName(node))
        {
            return false;
        }
        Node expression = default;
        if (Ast.IsElementAccessExpression(node))
        {
            expression = Ast.SkipParentheses(node.AsElementAccessExpression().ArgumentExpression);
        }
        else
        {
            expression = node.Expression();
        }
        if (!Ast.IsEntityNameExpression(expression))
        {
            return this.grammarErrorOnNode(node, message);
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarMethod(Node node)
    {
        if (this.checkGrammarFunctionLikeDeclaration(node))
        {
            return true;
        }

        if ((node.Kind == Kind.MethodDeclaration))
        {
            if ((node.Parent.Kind == Kind.ObjectLiteralExpression))
            {
                {
                    var modifiers = node.Modifiers();
                    if (((modifiers != null) && !(((GoRuntime.Len(modifiers.Nodes) == 1) && (modifiers.Nodes[0].Kind == Kind.AsyncKeyword)))))
                    {
                        return this.grammarErrorOnFirstToken(node, Diagnostics.Modifiers_cannot_appear_here);
                    }
                }

                var methodDecl = node.AsMethodDeclaration();
                if (this.checkGrammarForInvalidQuestionMark(methodDecl.PostfixToken, Diagnostics.An_object_member_cannot_be_declared_optional))
                {
                    return true;
                }

                if (this.checkGrammarForInvalidExclamationToken(methodDecl.PostfixToken, Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context))
                {
                    return true;
                }

                if ((node.Body() == null))
                {
                    return this.grammarErrorAtPos(node, (node.End() - 1), GoRuntime.Len(";"), Diagnostics.X_0_expected, "{");
                }
            }

            if (this.checkGrammarForGenerator(node))
            {
                return true;
            }
        }

        if (Ast.IsClassLike(node.Parent))
        {
            if (((node.Flags & NodeFlags.Ambient) != 0))
            {
                return this.checkGrammarForInvalidDynamicName(node.Name(), Diagnostics.A_computed_property_name_in_an_ambient_context_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type);
            }
            else
            {
                if (((node.Kind == Kind.MethodDeclaration) && (node.Body() == null)))
                {
                    return this.checkGrammarForInvalidDynamicName(node.Name(), Diagnostics.A_computed_property_name_in_a_method_overload_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type);
                }
            }
        }
        else
        {
            if ((node.Parent.Kind == Kind.InterfaceDeclaration))
            {
                return this.checkGrammarForInvalidDynamicName(node.Name(), Diagnostics.A_computed_property_name_in_an_interface_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type);
            }
            else
            {
                if ((node.Parent.Kind == Kind.TypeLiteral))
                {
                    return this.checkGrammarForInvalidDynamicName(node.Name(), Diagnostics.A_computed_property_name_in_a_type_literal_must_refer_to_an_expression_whose_type_is_a_literal_type_or_a_unique_symbol_type);
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool reportObviousModifierErrors(Node node)
    {
        var modifier = this.findFirstIllegalModifier(node);
        if ((modifier == null))
        {
            return false;
        }

        return this.grammarErrorOnFirstToken(modifier, Diagnostics.Modifiers_cannot_appear_here);
    }
}

public sealed partial class Checker
{
    private Node findFirstModifierExcept(Node node, Kind allowedModifier)
    {
        var modifier = Core.Find(node.ModifierNodes(), Ast.IsModifier);
        if (((modifier != null) && (modifier.Kind != allowedModifier)))
        {
            return modifier;
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Node findFirstIllegalModifier(Node node)
    {
        switch (node.Kind)
        {
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.Constructor:
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.IndexSignature:
            case Kind.ModuleDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ImportEqualsDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExportAssignment:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.Parameter:
            case Kind.TypeParameter:
            case Kind.JSTypeAliasDeclaration:
            {
                return null;
            }

            case Kind.ClassStaticBlockDeclaration:
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
            case Kind.NamespaceExportDeclaration:
            case Kind.MissingDeclaration:
            {
                return Core.Find(node.ModifierNodes(), Ast.IsModifier);
            }

            default:
            {
                if (((node.Parent.Kind == Kind.ModuleBlock) || (node.Parent.Kind == Kind.SourceFile)))
                {
                    return null;
                }

                switch (node.Kind)
                {
                    case Kind.FunctionDeclaration:
                    {
                        return this.findFirstModifierExcept(node, Kind.AsyncKeyword);
                    }

                    case Kind.ClassDeclaration:
                    case Kind.ConstructorType:
                    {
                        return this.findFirstModifierExcept(node, Kind.AbstractKeyword);
                    }

                    case Kind.ClassExpression:
                    case Kind.InterfaceDeclaration:
                    case Kind.TypeAliasDeclaration:
                    {
                        return Core.Find(node.ModifierNodes(), Ast.IsModifier);
                    }

                    case Kind.VariableStatement:
                    {
                        if (((node.AsVariableStatement().DeclarationList.Flags & NodeFlags.Using) != 0))
                        {
                            return this.findFirstModifierExcept(node, Kind.AwaitKeyword);
                        }

                        return Core.Find(node.ModifierNodes(), Ast.IsModifier);
                    }

                    case Kind.EnumDeclaration:
                    {
                        return this.findFirstModifierExcept(node, Kind.ConstKeyword);
                    }

                    default:
                    {
                        throw new InvalidOperationException("Unhandled case in findFirstIllegalModifier.");
                    }
                }
            }
        }
    }
}

public sealed partial class Checker
{
    private bool reportObviousDecoratorErrors(Node node)
    {
        var decorator = this.findFirstIllegalDecorator(node);
        if ((decorator == null))
        {
            return false;
        }

        return this.grammarErrorOnFirstToken(decorator, Diagnostics.Decorators_are_not_valid_here);
    }
}

public sealed partial class Checker
{
    private Node findFirstIllegalDecorator(Node node)
    {
        if (Ast.CanHaveIllegalDecorators(node))
        {
            var decorator = Core.Find(node.ModifierNodes(), Ast.IsDecorator);
            return decorator;
        }
        else
        {
            return null;
        }
    }
}

public sealed partial class Checker
{
    private bool checkGrammarComputedPropertyName(Node node)
    {
        if ((node.Kind != Kind.ComputedPropertyName))
        {
            return false;
        }

        var computedPropertyName = node.AsComputedPropertyName();
        if (((computedPropertyName.Expression.Kind == Kind.BinaryExpression) && (computedPropertyName.Expression.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)))
        {
            return this.grammarErrorOnNode(computedPropertyName.Expression, Diagnostics.A_comma_expression_is_not_allowed_in_a_computed_property_name);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkAmbientInitializer(Node node)
    {
        Node initializer = default;
        Node typeNode = default;
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            {
                var varDecl = node.AsVariableDeclaration();
                initializer = varDecl.Initializer;
                typeNode = varDecl.Type;
                break;
            }

            case Kind.PropertyDeclaration:
            {
                var propDecl = node.AsPropertyDeclaration();
                initializer = propDecl.Initializer;
                typeNode = propDecl.Type;
                break;
            }

            case Kind.PropertySignature:
            {
                var propSig = node.AsPropertySignatureDeclaration();
                initializer = propSig.Initializer;
                typeNode = propSig.Type;
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unexpected node kind " + node.Kind);
            }
        }

        if ((initializer != null))
        {
            var isInvalidInitializer = !(((((isInitializerStringOrNumberLiteralExpression(initializer) || this.isInitializerSimpleLiteralEnumReference(initializer)) || (initializer.Kind == Kind.TrueKeyword)) || (initializer.Kind == Kind.FalseKeyword)) || isInitializerBigIntLiteralExpression(initializer)));
            var isConstOrReadonly = (isDeclarationReadonly(node) || (Ast.IsVariableDeclaration(node) && this.isVarConstLike(node)));
            if ((isConstOrReadonly && ((typeNode == null))))
            {
                if (isInvalidInitializer)
                {
                    return this.grammarErrorOnNode(initializer, Diagnostics.A_const_initializer_in_an_ambient_context_must_be_a_string_or_numeric_literal_or_literal_enum_reference);
                }
            }
            else
            {
                return this.grammarErrorOnNode(initializer, Diagnostics.Initializers_are_not_allowed_in_ambient_contexts);
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private static bool isInitializerStringOrNumberLiteralExpression(Node expr)
    {
        return (Ast.IsStringOrNumericLiteralLike(expr) || (((expr.Kind == Kind.PrefixUnaryExpression) && (expr.AsPrefixUnaryExpression().Operator == Kind.MinusToken)) && (expr.AsPrefixUnaryExpression().Operand.Kind == Kind.NumericLiteral)));
    }
}

public sealed partial class Checker
{
    private static bool isInitializerBigIntLiteralExpression(Node expr)
    {
        if ((expr.Kind == Kind.BigIntLiteral))
        {
            return true;
        }

        if ((expr.Kind == Kind.PrefixUnaryExpression))
        {
            var unaryExpr = expr.AsPrefixUnaryExpression();
            return ((unaryExpr.Operator == Kind.MinusToken) && (unaryExpr.Operand.Kind == Kind.BigIntLiteral));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isInitializerSimpleLiteralEnumReference(Node expr)
    {
        if (Ast.IsPropertyAccessExpression(expr))
        {
            return ((this.checkExpressionCached(expr).flags & TypeFlags.EnumLike) != 0);
        }
        if (Ast.IsElementAccessExpression(expr))
        {
            var elementAccess = expr.AsElementAccessExpression();
            return ((isInitializerStringOrNumberLiteralExpression(elementAccess.ArgumentExpression) && Ast.IsEntityNameExpression(elementAccess.Expression)) && ((this.checkExpressionCached(expr).flags & TypeFlags.EnumLike) != 0));
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarIndexSignatureParameters(IndexSignatureDeclaration node)
    {
        var paramNodes = node.Parameters.Nodes;
        if ((GoRuntime.Len(paramNodes) == 0))
        {
            return this.grammarErrorOnNode(node.AsNode(), Diagnostics.An_index_signature_must_have_exactly_one_parameter);
        }

        var parameter = paramNodes[0].AsParameterDeclaration();
        if ((GoRuntime.Len(paramNodes) != 1))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_must_have_exactly_one_parameter);
        }

        this.checkGrammarForDisallowedTrailingComma(node.Parameters, Diagnostics.An_index_signature_cannot_have_a_trailing_comma);
        if ((parameter.DotDotDotToken != null))
        {
            return this.grammarErrorOnNode(parameter.DotDotDotToken, Diagnostics.An_index_signature_cannot_have_a_rest_parameter);
        }

        if ((parameter.Modifiers() != null))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_parameter_cannot_have_an_accessibility_modifier);
        }

        if ((parameter.QuestionToken != null))
        {
            return this.grammarErrorOnNode(parameter.QuestionToken, Diagnostics.An_index_signature_parameter_cannot_have_a_question_mark);
        }

        if ((parameter.Initializer != null))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_parameter_cannot_have_an_initializer);
        }

        var typeNode = parameter.Type;
        if ((typeNode == null))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_parameter_must_have_a_type_annotation);
        }

        var t = this.getTypeFromTypeNode(typeNode);
        if ((someType(t, (t2) =>
        {
            return ((t2.flags & TypeFlags.StringOrNumberLiteralOrUnique) != 0);
        }) || this.isGenericType(t)))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_parameter_type_cannot_be_a_literal_type_or_generic_type_Consider_using_a_mapped_object_type_instead);
        }

        if (!everyType(t, this.isValidIndexKeyType))
        {
            return this.grammarErrorOnNode(parameter.Name(), Diagnostics.An_index_signature_parameter_type_must_be_string_number_symbol_or_a_template_literal_type);
        }

        if ((node.Type == null))
        {
            return this.grammarErrorOnNode(node.AsNode(), Diagnostics.An_index_signature_must_have_a_type_annotation);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarIndexSignature(IndexSignatureDeclaration node)
    {
        return (this.checkGrammarModifiers(node.AsNode()) || this.checkGrammarIndexSignatureParameters(node));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarForInOrForOfStatement(ForInOrOfStatement forInOrOfStatement)
    {
        var asNode = forInOrOfStatement.AsNode();
        if (this.checkGrammarStatementInAmbientContext(asNode))
        {
            return true;
        }

        if (forInOrOfStatement.Kind == Kind.ForOfStatement && forInOrOfStatement.AwaitModifier != null)
        {
            if ((forInOrOfStatement.Flags & NodeFlags.AwaitContext) == 0)
            {
                var sourceFile = Ast.GetSourceFileOfNode(asNode);
                if (Ast.IsInTopLevelContext(asNode))
                {
                    if (!hasParseDiagnostics(sourceFile))
                    {
                        if (!Ast.IsEffectiveExternalModule(sourceFile, compilerOptions))
                            addDiagnostic(createDiagnosticForNode(forInOrOfStatement.AwaitModifier, Diagnostics.X_for_await_loops_are_only_allowed_at_the_top_level_of_a_file_when_that_file_is_a_module_but_this_file_has_no_imports_or_exports_Consider_adding_an_empty_export_to_make_this_file_a_module));
                        switch (moduleKind)
                        {
                            case ModuleKind.Node16:
                            case ModuleKind.Node18:
                            case ModuleKind.Node20:
                            case ModuleKind.NodeNext:
                                if (program.GetSourceFileMetaData(PathKey.FromValue(sourceFile.PathKeyValue())).ImpliedNodeFormat == ResolutionMode.CommonJS)
                                {
                                    addDiagnostic(createDiagnosticForNode(forInOrOfStatement.AwaitModifier, Diagnostics.The_current_file_is_a_CommonJS_module_and_cannot_use_await_at_the_top_level));
                                    break;
                                }
                                goto case ModuleKind.ES2022;
                            case ModuleKind.ES2022:
                            case ModuleKind.ESNext:
                            case ModuleKind.Preserve:
                            case ModuleKind.System:
                                if (languageVersion >= ScriptTarget.ES2017) break;
                                goto default;
                            default:
                                addDiagnostic(createDiagnosticForNode(forInOrOfStatement.AwaitModifier, Diagnostics.Top_level_for_await_loops_are_only_allowed_when_the_module_option_is_set_to_es2022_esnext_system_node16_node18_node20_nodenext_or_preserve_and_the_target_option_is_set_to_es2017_or_higher));
                                break;
                        }
                    }
                }
                else if (!hasParseDiagnostics(sourceFile))
                {
                    var diagnostic = createDiagnosticForNode(forInOrOfStatement.AwaitModifier, Diagnostics.X_for_await_loops_are_only_allowed_within_async_functions_and_at_the_top_levels_of_modules);
                    var containingFunc = Ast.GetContainingFunction(asNode);
                    if (containingFunc != null && containingFunc.Kind != Kind.Constructor)
                    {
                        if ((Ast.GetFunctionFlags(containingFunc) & FunctionFlags.Async) != 0)
                            throw new InvalidOperationException("Enclosing function should never be an async function.");
                        diagnostic.AddRelatedInfo(createDiagnosticForNode(containingFunc, Diagnostics.Did_you_mean_to_mark_this_function_as_async));
                    }
                    addDiagnostic(diagnostic);
                    return true;
                }
            }
        }
        if ((((Ast.IsForOfStatement(asNode) && ((forInOrOfStatement.Flags & NodeFlags.AwaitContext) == 0)) && Ast.IsIdentifier(forInOrOfStatement.Initializer)) && (forInOrOfStatement.Initializer.TextEquals("async"u8))))
        {
            this.grammarErrorOnNode(forInOrOfStatement.Initializer, Diagnostics.The_left_hand_side_of_a_for_of_statement_may_not_be_async);
            return false;
        }

        if ((forInOrOfStatement.Initializer.Kind == Kind.VariableDeclarationList))
        {
            var variableList = forInOrOfStatement.Initializer.AsVariableDeclarationList();
            if (!this.checkGrammarVariableDeclarationList(variableList.AsNode()))
            {
                var declarations = variableList.Declarations;
                if ((GoRuntime.Len(declarations.Nodes) == 0))
                {
                    return false;
                }

                if ((GoRuntime.Len(declarations.Nodes) > 1))
                {
                    Message diagnostic2 = default;
                    if ((forInOrOfStatement.Kind == Kind.ForInStatement))
                    {
                        diagnostic2 = Diagnostics.Only_a_single_variable_declaration_is_allowed_in_a_for_in_statement;
                    }
                    else
                    {
                        diagnostic2 = Diagnostics.Only_a_single_variable_declaration_is_allowed_in_a_for_of_statement;
                    }

                    return this.grammarErrorOnFirstToken(declarations.Nodes[1], diagnostic2);
                }

                var firstVariableDeclaration = declarations.Nodes[0].AsVariableDeclaration();
                if ((firstVariableDeclaration.Initializer != null))
                {
                    Message diagnostic3 = default;
                    if ((forInOrOfStatement.Kind == Kind.ForInStatement))
                    {
                        diagnostic3 = Diagnostics.The_variable_declaration_of_a_for_in_statement_cannot_have_an_initializer;
                    }
                    else
                    {
                        diagnostic3 = Diagnostics.The_variable_declaration_of_a_for_of_statement_cannot_have_an_initializer;
                    }

                    return this.grammarErrorOnNode(firstVariableDeclaration.Name(), diagnostic3);
                }

                if ((firstVariableDeclaration.Type != null))
                {
                    Message diagnostic4 = default;
                    if ((forInOrOfStatement.Kind == Kind.ForInStatement))
                    {
                        diagnostic4 = Diagnostics.The_left_hand_side_of_a_for_in_statement_cannot_use_a_type_annotation;
                    }
                    else
                    {
                        diagnostic4 = Diagnostics.The_left_hand_side_of_a_for_of_statement_cannot_use_a_type_annotation;
                    }

                    return this.grammarErrorOnNode(firstVariableDeclaration.AsNode(), diagnostic4);
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarBreakOrContinueStatement(Node node)
    {
        var targetLabel = node.Label();
        Node current = node;
        while ((current != null))
        {
            if (Ast.IsFunctionLikeOrClassStaticBlockDeclaration(current))
            {
                return this.grammarErrorOnNode(node, Diagnostics.Jump_target_cannot_cross_function_boundary);
            }

            switch (current.Kind)
            {
                case Kind.LabeledStatement:
                    if (targetLabel != null && current.Label().TextValue().EqualsValue(targetLabel.TextValue()))
                    {
                        if (node.Kind == Kind.ContinueStatement && !Ast.IsIterationStatement(current.Statement(), true))
                            return grammarErrorOnNode(node, Diagnostics.A_continue_statement_can_only_jump_to_a_label_of_an_enclosing_iteration_statement);
                        return false;
                    }
                    break;
                case Kind.SwitchStatement:
                    if (node.Kind == Kind.BreakStatement && targetLabel == null)
                        return false;
                    break;
                default:
                {
                    if ((Ast.IsIterationStatement(current, false) && (targetLabel == null)))
                    {
                        return false;
                    }

                    break;
                }
            }

            current = current.Parent;
        }

        if (targetLabel != null)
            return grammarErrorOnNode(node, node.Kind == Kind.BreakStatement
                ? Diagnostics.A_break_statement_can_only_jump_to_a_label_of_an_enclosing_statement
                : Diagnostics.A_continue_statement_can_only_jump_to_a_label_of_an_enclosing_iteration_statement);
        Message message2 = default;
        if ((node.Kind == Kind.BreakStatement))
        {
            message2 = Diagnostics.A_break_statement_can_only_be_used_within_an_enclosing_iteration_or_switch_statement;
        }
        else
        {
            message2 = Diagnostics.A_continue_statement_can_only_be_used_within_an_enclosing_iteration_statement;
        }

        return this.grammarErrorOnNode(node, message2);
    }
}

public sealed partial class Checker
{
    // grammarchecks.go:120.
    private bool checkGrammarMappedType(MappedTypeNode node)
    {
        if (GoRuntime.Len(node.Members?.Nodes ?? default) > 0)
            return grammarErrorOnNode(node.Members.Nodes[0], Diagnostics.A_mapped_type_may_not_declare_properties_or_methods);
        return false;
    }

    // grammarchecks.go:1351. Unique-symbol placement and declaration modifier checks.
    private bool checkGrammarTypeOperatorNode(TypeOperatorNode node)
    {
        if ((node.Operator == Kind.UniqueKeyword))
        {
            var innerType = node.Type;
            if ((innerType.Kind != Kind.SymbolKeyword))
            {
                return this.grammarErrorOnNode(innerType, Diagnostics.X_0_expected, TypeScript.Scanner.Scanner.TokenToString(Kind.SymbolKeyword));
            }
            var parent = Ast.WalkUpParenthesizedTypes(node.Parent);
            switch (parent.Kind)
            {
                case Kind.VariableDeclaration:
                {
                    var decl = parent.AsVariableDeclaration();
                    if ((decl.Name().Kind != Kind.Identifier))
                    {
                        return this.grammarErrorOnNode(node.AsNode(), Diagnostics.X_unique_symbol_types_may_not_be_used_on_a_variable_declaration_with_a_binding_name);
                    }
                    if (!isVariableDeclarationInVariableStatement(decl.AsNode()))
                    {
                        return this.grammarErrorOnNode(node.AsNode(), Diagnostics.X_unique_symbol_types_are_only_allowed_on_variables_in_a_variable_statement);
                    }
                    if (((decl.Parent.Flags & NodeFlags.Const) == 0))
                    {
                        return this.grammarErrorOnNode(parent.AsVariableDeclaration().Name(), Diagnostics.A_variable_whose_type_is_a_unique_symbol_type_must_be_const);
                    }
                    break;
                }
                case Kind.PropertyDeclaration:
                {
                    if ((!Ast.IsStatic(parent) || !hasReadonlyModifier(parent)))
                    {
                        return this.grammarErrorOnNode(parent.AsPropertyDeclaration().Name(), Diagnostics.A_property_of_a_class_whose_type_is_a_unique_symbol_type_must_be_both_static_and_readonly);
                    }
                    break;
                }
                case Kind.PropertySignature:
                {
                    if (!Ast.HasSyntacticModifier(parent, ModifierFlags.Readonly))
                    {
                        return this.grammarErrorOnNode(parent.AsPropertySignatureDeclaration().Name(), Diagnostics.A_property_of_an_interface_or_type_literal_whose_type_is_a_unique_symbol_type_must_be_readonly);
                    }
                    break;
                }
                default:
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.X_unique_symbol_types_are_not_allowed_here);
                }
            }
        }
        else
        {
            if ((node.Operator == Kind.ReadonlyKeyword))
            {
                var innerType2 = node.Type;
                if (((innerType2.Kind != Kind.ArrayType) && (innerType2.Kind != Kind.TupleType)))
                {
                    return this.grammarErrorOnFirstToken(node.AsNode(), Diagnostics.X_readonly_type_modifier_is_only_permitted_on_array_and_tuple_literal_types, TypeScript.Scanner.Scanner.TokenToString(Kind.SymbolKeyword));
                }
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarExportDeclaration(ExportDeclaration node)
    {
        if (((node.IsTypeOnly && (node.ExportClause != null)) && (node.ExportClause.Kind == Kind.NamedExports)))
        {
            return this.checkGrammarTypeOnlyNamedImportsOrExports(node.ExportClause);
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarModuleElementContext(Node node, Message errorMessage)
    {
        var isInAppropriateContext = (((node.Parent.Kind == Kind.SourceFile) || (node.Parent.Kind == Kind.ModuleBlock)) || (node.Parent.Kind == Kind.ModuleDeclaration));
        if (!isInAppropriateContext)
        {
            this.grammarErrorOnFirstToken(node, errorMessage);
        }

        return !isInAppropriateContext;
    }
}

public sealed partial class Checker
{
}

public sealed partial class Checker
{
}

public sealed partial class Checker
{
    private bool checkGrammarPrivateIdentifierExpression(PrivateIdentifier privId)
    {
        var privIdAsNode = privId.AsNode();
        if ((Ast.GetContainingClass(privId.AsNode()) == null))
        {
            return this.grammarErrorOnNode(privId.AsNode(), Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
        }
        if (!Ast.IsForInStatement(privId.Parent))
        {
            if (!Ast.IsExpressionNode(privIdAsNode))
            {
                return this.grammarErrorOnNode(privIdAsNode, Diagnostics.Private_identifiers_are_only_allowed_in_class_bodies_and_may_only_be_used_as_part_of_a_class_member_declaration_property_access_or_on_the_left_hand_side_of_an_in_expression);
            }
            var isInOperation = (Ast.IsBinaryExpression(privId.Parent) && (privId.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.InKeyword));
            if (((this.getSymbolForPrivateIdentifierExpression(privIdAsNode) == null) && !isInOperation))
            {
                return this.grammarErrorOnNode(privIdAsNode, Diagnostics.Cannot_find_name_0, privId.Text);
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarClassLikeDeclaration(Node node)
    {
        var file = Ast.GetSourceFileOfNode(node);
        return (this.checkGrammarClassDeclarationHeritageClauses(node, file) || this.checkGrammarTypeParameterList(node.TypeParameterList(), file));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarClassDeclarationHeritageClauses(Node node, SourceFile file)
    {
        var seenExtendsClause = false;
        var seenImplementsClause = false;
        var classLikeData = node.ClassLikeData();
        if ((!this.checkGrammarModifiers(node) && (classLikeData.HeritageClauses != null)))
        {
            {
                foreach (var heritageClauseNode in GoRuntime.RangeValues(classLikeData.HeritageClauses.Nodes))
                {
                    var heritageClause = heritageClauseNode.AsHeritageClause();
                    if ((heritageClause.Token == Kind.ExtendsKeyword))
                    {
                        if (seenExtendsClause)
                        {
                            return this.grammarErrorOnFirstToken(heritageClauseNode, Diagnostics.X_extends_clause_already_seen);
                        }
                        if (seenImplementsClause)
                        {
                            return this.grammarErrorOnFirstToken(heritageClauseNode, Diagnostics.X_extends_clause_must_precede_implements_clause);
                        }
                        var typeNodes = heritageClause.Types.Nodes;
                        if ((GoRuntime.Len(typeNodes) > 1))
                        {
                            return this.grammarErrorOnFirstToken(typeNodes[1], Diagnostics.Classes_can_only_extend_a_single_class);
                        }
                        seenExtendsClause = true;
                    }
                    else
                    {
                        if ((heritageClause.Token != Kind.ImplementsKeyword))
                        {
                            throw new InvalidOperationException("Unexpected heritage token: " + heritageClause.Token.String());
                        }
                        if (seenImplementsClause)
                        {
                            return this.grammarErrorOnFirstToken(heritageClauseNode, Diagnostics.X_implements_clause_already_seen);
                        }
                        seenImplementsClause = true;
                    }
                    this.checkGrammarHeritageClause(heritageClause);
                }
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarAccessor(Node accessor)
    {
        var body = accessor.Body();
        if (((((accessor.Flags & NodeFlags.Ambient) == 0) && ((accessor.Parent.Kind != Kind.TypeLiteral))) && ((accessor.Parent.Kind != Kind.InterfaceDeclaration))))
        {
            if (((body == null) && !Ast.HasSyntacticModifier(accessor, ModifierFlags.Abstract)))
            {
                return this.grammarErrorAtPos(accessor, (accessor.End() - 1), GoRuntime.Len(";"), Diagnostics.X_0_expected, "{");
            }
        }
        if ((body != null))
        {
            if (Ast.HasSyntacticModifier(accessor, ModifierFlags.Abstract))
            {
                return this.grammarErrorOnNode(accessor, Diagnostics.An_abstract_accessor_cannot_have_an_implementation);
            }
            if (((accessor.Parent.Kind == Kind.TypeLiteral) || (accessor.Parent.Kind == Kind.InterfaceDeclaration)))
            {
                return this.grammarErrorOnNode(body, Diagnostics.An_implementation_cannot_be_declared_in_ambient_contexts);
            }
        }
        var funcData = accessor.FunctionLikeData();
        NodeList typeParameters = default;
        if ((funcData != null))
        {
            typeParameters = funcData.TypeParameters;
        }
        if ((typeParameters != null))
        {
            return this.grammarErrorOnNode(accessor.Name(), Diagnostics.An_accessor_cannot_have_type_parameters);
        }
        if (!this.doesAccessorHaveCorrectParameterCount(accessor))
        {
            return this.grammarErrorOnNode(accessor.Name(), Core.IfElse((accessor.Kind == Kind.GetAccessor), Diagnostics.A_get_accessor_cannot_have_parameters, Diagnostics.A_set_accessor_must_have_exactly_one_parameter));
        }
        if ((accessor.Kind == Kind.SetAccessor))
        {
            if ((funcData.Type != null))
            {
                return this.grammarErrorOnNode(accessor.Name(), Diagnostics.A_set_accessor_cannot_have_a_return_type_annotation);
            }
            var parameterNode = GetSetAccessorValueParameter(accessor);
            if ((parameterNode == null))
            {
                throw new InvalidOperationException("Return value does not match parameter count assertion.");
            }
            var parameter = parameterNode.AsParameterDeclaration();
            if ((parameter.DotDotDotToken != null))
            {
                return this.grammarErrorOnNode(parameter.DotDotDotToken, Diagnostics.A_set_accessor_cannot_have_rest_parameter);
            }
            if ((parameter.QuestionToken != null))
            {
                return this.grammarErrorOnNode(parameter.QuestionToken, Diagnostics.A_set_accessor_cannot_have_an_optional_parameter);
            }
            if ((parameter.Initializer != null))
            {
                return this.grammarErrorOnNode(accessor.Name(), Diagnostics.A_set_accessor_parameter_cannot_have_an_initializer);
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool doesAccessorHaveCorrectParameterCount(Node accessor)
    {
        return ((this.getAccessorThisParameter(accessor) != null) || (GoRuntime.Len(accessor.Parameters()) == Core.IfElse((accessor.Kind == Kind.GetAccessor), 0, 1)));
    }
}

public sealed partial class Checker
{
    private bool isNonBindableDynamicName(Node node)
    {
        return (Ast.IsDynamicName(node) && !this.isLateBindableName(node));
    }
}

public sealed partial class Checker
{
    private bool checkGrammarConstructorTypeParameters(ConstructorDeclaration node)
    {
        var range_ = node.TypeParameters;
        if ((range_ != null))
        {
            int pos = default;
            if ((range_.Pos() == range_.End()))
            {
                pos = range_.Pos();
            }
            else
            {
                pos = TypeScript.Scanner.Scanner.SkipTrivia(Ast.GetSourceFileOfNode(node.AsNode()).TextView(), range_.Pos());
            }
            return this.grammarErrorAtPos(node.AsNode(), pos, (range_.End() - pos), Diagnostics.Type_parameters_cannot_appear_on_a_constructor_declaration);
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarConstructorTypeAnnotation(ConstructorDeclaration node)
    {
        var t = node.Type;
        if ((t != null))
        {
            return this.grammarErrorOnNode(t, Diagnostics.Type_annotation_cannot_appear_on_a_constructor_declaration);
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarTopLevelElementForRequiredDeclareModifier(Node node)
    {
        if ((((((((((node.Kind == Kind.InterfaceDeclaration) || (node.Kind == Kind.TypeAliasDeclaration)) || (node.Kind == Kind.ImportDeclaration)) || (node.Kind == Kind.JSImportDeclaration)) || (node.Kind == Kind.ImportEqualsDeclaration)) || (node.Kind == Kind.ExportDeclaration)) || (node.Kind == Kind.ExportAssignment)) || (node.Kind == Kind.NamespaceExportDeclaration)) || Ast.HasSyntacticModifier(node, ((ModifierFlags.Ambient | ModifierFlags.Export) | ModifierFlags.Default))))
        {
            return false;
        }
        return this.grammarErrorOnFirstToken(node, Diagnostics.Top_level_declarations_in_d_ts_files_must_start_with_either_a_declare_or_export_modifier);
    }
}

public sealed partial class Checker
{
    private bool checkGrammarTopLevelElementsForRequiredDeclareModifier(SourceFile file)
    {
        {
            foreach (var decl in GoRuntime.RangeValues(file.Statements.Nodes))
            {
                if ((Ast.IsDeclarationNode(decl) || (decl.Kind == Kind.VariableStatement)))
                {
                    if (this.checkGrammarTopLevelElementForRequiredDeclareModifier(decl))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

public sealed partial class Checker
{
    private bool checkGrammarImportAttributesType(TypeLiteralNode attributes)
    {
        var members = attributes.Members;
        if ((members == null))
        {
            return false;
        }
        {
            foreach (var member in GoRuntime.RangeValues(members.Nodes))
            {
                if ((member.Kind != Kind.PropertySignature))
                {
                    return this.grammarErrorOnNode(member, Diagnostics.An_import_attributes_type_may_only_contain_property_signatures);
                }
                var propertySignature = member.AsPropertySignatureDeclaration();
                {
                    var modifiers = propertySignature.Modifiers();
                    if ((modifiers != null))
                    {
                        {
                            foreach (var range2 in GoRuntime.Range(modifiers.Nodes))
                            {
                                var _2 = range2.Key;
                                var modifier = range2.Value;
                                if ((modifier.Kind == Kind.ReadonlyKeyword))
                                {
                                    return this.grammarErrorOnNode(modifier, Diagnostics.An_import_attributes_property_cannot_have_a_readonly_modifier);
                                }
                            }
                        }
                    }
                }
                if ((propertySignature.Type == null))
                {
                    return this.grammarErrorOnNode(member, Diagnostics.An_import_attributes_property_must_have_a_type_annotation);
                }
                if ((propertySignature.PostfixToken != null))
                {
                    return this.grammarErrorOnNode(member, Diagnostics.An_import_attributes_property_cannot_be_optional);
                }
                var name = propertySignature.Name();
                if (!((Ast.IsStringLiteralLike(name) || Ast.IsIdentifier(name))))
                {
                    return this.grammarErrorOnNode(name, Diagnostics.An_import_attributes_property_must_have_a_string_literal_or_identifier_name);
                }
                if ((name.TextEquals("resolution-mode"u8)))
                {
                    return this.grammarErrorOnNode(name, Diagnostics.X_0_is_not_a_valid_key_for_an_import_attributes_type, name.Text());
                }
                var typeNode = propertySignature.Type;
                if ((!Ast.IsLiteralTypeNode(typeNode) || !Ast.IsStringLiteral(typeNode.AsLiteralTypeNode().Literal)))
                {
                    return this.grammarErrorOnNode(typeNode, Diagnostics.An_import_attributes_property_must_have_a_string_literal_type_annotation);
                }
            }
        }
        return false;
    }

    // grammarchecks.go:1138–1179. JSX name, attribute and expression grammar.
    private bool checkGrammarJsxElement(Node node)
    {
        checkGrammarJsxName(node.TagName());
        checkGrammarTypeArguments(node, node.TypeArgumentList());
        var seen = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        foreach (var attrNode in node.Attributes().Properties())
        {
            if (attrNode.Kind == Kind.JsxSpreadAttribute)
                continue;
            var attr = attrNode.AsJsxAttribute();
            var name = attr.Name();
            var initializer = attr.Initializer;
            if (!seen.Add(ByteStringKey.FromValue(name.TextValue())))
                return grammarErrorOnNode(name, Diagnostics.JSX_elements_cannot_have_multiple_attributes_with_the_same_name);
            if (initializer != null && initializer.Kind == Kind.JsxExpression && initializer.Expression() == null)
                return grammarErrorOnNode(initializer, Diagnostics.JSX_attributes_must_only_be_assigned_a_non_empty_expression);
        }

        return false;
    }

    private bool checkGrammarJsxName(Node node)
    {
        if (Ast.IsPropertyAccessExpression(node) && Ast.IsJsxNamespacedName(node.Expression()))
            return grammarErrorOnNode(node.Expression(), Diagnostics.JSX_property_access_expressions_cannot_include_JSX_namespace_names);
        if (Ast.IsJsxNamespacedName(node) && compilerOptions.GetJSXTransformEnabled() && !TypeScript.Scanner.Scanner.IsIntrinsicJsxName(node.AsJsxNamespacedName().Namespace.TextValue()))
            return grammarErrorOnNode(node, Diagnostics.React_components_cannot_include_JSX_namespace_names);
        return false;
    }

    private bool checkGrammarJsxExpression(JsxExpression node)
    {
        if (node.Expression != null && Ast.IsCommaSequence(node.Expression))
            return grammarErrorOnNode(node.Expression, Diagnostics.JSX_expressions_may_not_use_the_comma_operator_Did_you_mean_to_write_an_array);
        return false;
    }

    // Upstream tsc/internal/checker/grammarchecks.go:127:1.
    private bool checkGrammarDecorator(Decorator decorator)
    {
        var sourceFile = Ast.GetSourceFileOfNode(decorator.AsNode());
        if (!this.hasParseDiagnostics(sourceFile))
        {
            var node = decorator.Expression;
            if (Ast.IsParenthesizedExpression(node))
            {
                return false;
            }

            var canHaveCallExpression = true;
            Node errorNode = default;
            while (true)
            {
                if ((Ast.IsExpressionWithTypeArguments(node) || Ast.IsNonNullExpression(node)))
                {
                    node = node.Expression();
                    continue;
                }

                if (Ast.IsCallExpression(node))
                {
                    var callExpr = node.AsCallExpression();
                    if (!canHaveCallExpression)
                    {
                        errorNode = node;
                    }

                    if ((callExpr.QuestionDotToken != null))
                    {
                        errorNode = callExpr.QuestionDotToken;
                    }

                    node = callExpr.Expression;
                    canHaveCallExpression = false;
                    continue;
                }

                if (Ast.IsPropertyAccessExpression(node))
                {
                    var propertyAccessExpr = node.AsPropertyAccessExpression();
                    if ((propertyAccessExpr.QuestionDotToken != null))
                    {
                        errorNode = propertyAccessExpr.QuestionDotToken;
                    }

                    node = propertyAccessExpr.Expression;
                    canHaveCallExpression = false;
                    continue;
                }

                if (!Ast.IsIdentifier(node))
                {
                    errorNode = node;
                }

                break;
            }

            if ((errorNode != null))
            {
                var err = this.error(decorator.Expression, Diagnostics.Expression_must_be_enclosed_in_parentheses_to_be_used_as_a_decorator);
                err.AddRelatedInfo(createDiagnosticForNode(errorNode, Diagnostics.Invalid_syntax_in_decorator));
                return true;
            }
        }

        return false;
    }

    // Upstream tsc/internal/checker/grammarchecks.go:2100:1.
    private bool checkGrammarImportClause(ImportClause node)
    {
        switch (node.PhaseModifier)
        {
            case Kind.TypeKeyword:
            {
                if (((((node.Flags & NodeFlags.JSDoc) == 0) && (node.Name() != null)) && (node.NamedBindings != null)))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.A_type_only_import_can_specify_a_default_import_or_named_bindings_but_not_both);
                }

                if (((node.NamedBindings != null) && (node.NamedBindings.Kind == Kind.NamedImports)))
                {
                    return this.checkGrammarTypeOnlyNamedImportsOrExports(node.NamedBindings);
                }

                break;
            }

            case Kind.DeferKeyword:
            {
                if ((node.Name() != null))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Default_imports_are_not_allowed_in_a_deferred_import);
                }

                if ((node.NamedBindings == null))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.A_deferred_import_must_specify_a_namespace_binding);
                }

                if ((node.NamedBindings.Kind == Kind.NamedImports))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Named_imports_are_not_allowed_in_a_deferred_import);
                }

                if (this.moduleKind.SupportsDeferredImports())
                {
                    break;
                }

                return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Deferred_imports_are_only_supported_when_the_module_flag_is_set_to_esnext_or_preserve);
            }

            case Kind.SourceKeyword:
            {
                if ((node.NamedBindings != null))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Named_and_namespace_imports_are_not_allowed_in_a_source_phase_import);
                }

                if ((node.Name() == null))
                {
                    return this.grammarErrorOnNode(node.AsNode(), Diagnostics.A_source_phase_import_must_specify_a_local_binding);
                }

                if (this.moduleKind.SupportsSourcePhaseImports())
                {
                    var moduleSpecifier = getModuleSpecifierFromNode(node.Parent);
                    if ((this.getEmitSyntaxForModuleSpecifierExpression(moduleSpecifier) == ResolutionMode.CommonJS))
                    {
                        return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Source_phase_imports_are_not_allowed_on_statements_that_compile_to_CommonJS_require_calls);
                    }

                    break;
                }

                return this.grammarErrorOnNode(node.AsNode(), Diagnostics.Source_phase_imports_are_only_supported_when_the_module_option_is_set_to_esnext_nodenext_or_preserve);
            }
        }

        return false;
    }

    // Upstream tsc/internal/checker/grammarchecks.go:2142:1.
    private bool checkGrammarImportAttributeValues(ImportAttributes node)
    {
        var hasError = false;
        {
            foreach (var attribute in GoRuntime.RangeValues(node.Attributes.Nodes))
            {
                var value = attribute.AsImportAttribute().Value;
                if (Ast.IsStringLiteral(value))
                {
                    continue;
                }

                hasError = true;
                this.error(value, Diagnostics.Import_attribute_values_must_be_string_literal_expressions);
            }
        }

        return hasError;
    }

    // Upstream tsc/internal/checker/grammarchecks.go:2155:1.
    private bool checkGrammarTypeOnlyNamedImportsOrExports(Node namedBindings)
    {
        var nodeList = namedBindings.ElementList();
        {
            foreach (var specifier in GoRuntime.RangeValues(nodeList.Nodes))
            {
                bool specifierIsTypeOnly = default;
                Message message = default;
                if ((specifier.Kind == Kind.ImportSpecifier))
                {
                    specifierIsTypeOnly = specifier.IsTypeOnly();
                    message = Diagnostics.The_type_modifier_cannot_be_used_on_a_named_import_when_import_type_is_used_on_its_import_statement;
                }
                else
                {
                    specifierIsTypeOnly = specifier.IsTypeOnly();
                    message = Diagnostics.The_type_modifier_cannot_be_used_on_a_named_export_when_export_type_is_used_on_its_export_statement;
                }

                if (specifierIsTypeOnly)
                {
                    return this.grammarErrorOnFirstToken(specifier, message);
                }
            }
        }

        return false;
    }

    // Upstream tsc/internal/checker/grammarchecks.go:2176:1.
    private bool checkGrammarImportCallExpression(Node node)
    {
        if (((this.compilerOptions.VerbatimModuleSyntax == Tristate.True) && (this.moduleKind == ModuleKind.CommonJS)))
        {
            return this.grammarErrorOnNode(node, getVerbatimModuleSyntaxErrorMessage(node));
        }

        switch (true)
        {
            case true when Ast.IsImportSourceMetaProperty(node.Expression()):
            {
                if (this.moduleKind.SupportsSourcePhaseImports())
                {
                    break;
                }

                return this.grammarErrorOnNode(node, Diagnostics.Source_phase_imports_are_only_supported_when_the_module_option_is_set_to_esnext_nodenext_or_preserve);
            }

            case true when Ast.IsImportDeferMetaProperty(node.Expression()):
            {
                if (this.moduleKind.SupportsDeferredImports())
                {
                    break;
                }

                return this.grammarErrorOnNode(node, Diagnostics.Deferred_imports_are_only_supported_when_the_module_flag_is_set_to_esnext_or_preserve);
            }

            case true when (this.moduleKind == ModuleKind.ES2015):
            {
                return this.grammarErrorOnNode(node, Diagnostics.Dynamic_imports_are_only_supported_when_the_module_flag_is_set_to_es2020_es2022_esnext_commonjs_amd_system_umd_node16_node18_node20_or_nodenext);
            }
        }

        var nodeAsCall = node.AsCallExpression();
        if ((Ast.IsSourcePhaseImportCall(node) && (nodeAsCall.QuestionDotToken != null)))
        {
            return this.grammarErrorOnNode(nodeAsCall.QuestionDotToken, Diagnostics.Optional_chaining_cannot_be_used_with_import_source);
        }

        if ((nodeAsCall.TypeArguments != null))
        {
            return this.grammarErrorOnNode(node, Diagnostics.This_use_of_import_is_invalid_import_calls_can_be_written_but_they_must_have_parentheses_and_cannot_have_type_arguments);
        }

        var nodeArguments = nodeAsCall.Arguments;
        var argumentNodes = nodeArguments.Nodes;
        if (((!(((ModuleKind.Node16 <= this.moduleKind) && (this.moduleKind <= ModuleKind.NodeNext))) && (this.moduleKind != ModuleKind.ESNext)) && (this.moduleKind != ModuleKind.Preserve)))
        {
            this.checkGrammarForDisallowedTrailingComma(nodeArguments, Diagnostics.Trailing_comma_not_allowed);
            if ((GoRuntime.Len(argumentNodes) > 1))
            {
                var importAttributesArgument = argumentNodes[1];
                return this.grammarErrorOnNode(importAttributesArgument, Diagnostics.Dynamic_imports_only_support_a_second_argument_when_the_module_option_is_set_to_esnext_node16_node18_node20_nodenext_or_preserve);
            }
        }

        if (((GoRuntime.Len(argumentNodes) == 0) || (GoRuntime.Len(argumentNodes) > 2)))
        {
            return this.grammarErrorOnNode(node, Diagnostics.Dynamic_imports_can_only_accept_a_module_specifier_and_an_optional_set_of_attributes_as_arguments);
        }

        var spreadElement = Core.Find(argumentNodes, Ast.IsSpreadElement);
        if ((spreadElement != null))
        {
            return this.grammarErrorOnNode(spreadElement, Diagnostics.Argument_of_dynamic_import_cannot_be_spread_element);
        }

        return false;
    }

    // grammarchecks.go:58. The final identifier of a source entity-name expression.
    private static Node getIdentifierFromEntityNameExpression(Node node) => node.Kind switch
    {
        Kind.Identifier => node,
        Kind.PropertyAccessExpression => node.AsPropertyAccessExpression().Name(),
        _ => null
    };
}
