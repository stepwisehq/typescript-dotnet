// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/grammarchecks.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private bool grammarErrorOnNode(Node node, Message message, params object[] args)
    {
        if ((Ast.GetSourceFileOfNode(node).Diagnostics()?.Count ?? 0) != 0)
            return false;
        error(node, message, args);
        return true;
    }

    private bool grammarErrorAtPos(Node node, int start, int length, Message message, params object[] args)
    {
        var file = Ast.GetSourceFileOfNode(node);
        if ((file.Diagnostics()?.Count ?? 0) != 0)
            return false;
        diagnostics.Add(Ast.NewDiagnostic(file, new(start, start + length), message, args));
        return true;
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
                            if ((flags & ModifierFlags.Export) != 0 && compilerOptions.VerbatimModuleSyntax == Tristate.True)
                            {
                                throw unsupported("checkGrammarModifiers: source exports");
                            }

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
            throw unsupported("checkGrammarModifiers: async modifier");
        }

        return false;
    }

    private bool checkGrammarSourceFile()
    {
        if ((sourceFile.Flags & NodeFlags.Ambient) != 0)
            throw unsupported("checkGrammarSourceFile: ambient script");
        return false;
    }

    private bool checkGrammarStatementInAmbientContext(Node node)
    {
        if (((node.Flags & NodeFlags.Ambient) != 0))
        {
            var links = this.nodeLinks.Get(node);
            if ((!links.hasReportedStatementInAmbientContext && ((Ast.IsFunctionLike(node.Parent) || Ast.IsAccessor(node.Parent)))))
            {
                links.hasReportedStatementInAmbientContext = this.grammarErrorOnFirstToken(node, Diagnostics.An_implementation_cannot_be_declared_in_ambient_contexts);
                return links.hasReportedStatementInAmbientContext;
            }

            if ((((node.Parent.Kind == Kind.Block) || (node.Parent.Kind == Kind.ModuleBlock)) || (node.Parent.Kind == Kind.SourceFile)))
            {
                var links2 = this.nodeLinks.Get(node.Parent);
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
        if ((declarations.Nodes?.Count ?? 0) == 0)
            return grammarErrorAtPos(node, declarations.Pos(), declarations.End() - declarations.Pos(), Diagnostics.Variable_declaration_list_cannot_be_empty);
        var flags = node.Flags & NodeFlags.BlockScoped;
        if (flags is NodeFlags.Using or NodeFlags.AwaitUsing)
            throw unsupported("checkGrammarVariableDeclarationList: using");
        return false;
    }

    private bool checkGrammarVariableDeclaration(Node node)
    {
        var flags = getCombinedNodeFlagsCached(node);
        var scope = flags & NodeFlags.BlockScoped;
        if (Ast.IsBindingPattern(node.Name()))
            throw unsupported("checkGrammarVariableDeclaration: binding pattern");
        if (node.Parent.Parent.Kind != Kind.VariableStatement)
            throw unsupported("checkGrammarVariableDeclaration: loop");
        if ((flags & NodeFlags.Ambient) != 0)
            checkAmbientInitializer(node);
        else if (node.Initializer() == null && scope == NodeFlags.Const)
            return grammarErrorOnNode(node, Diagnostics.X_0_declarations_must_be_initialized, "const");
        var exclamation = node.AsVariableDeclaration().ExclamationToken;
        if (exclamation != null && (node.Parent.Parent.Kind != Kind.VariableStatement || node.Type() == null || node.Initializer() != null || (flags & NodeFlags.Ambient) != 0))
        {
            var message = node.Initializer() != null ? Diagnostics.Declarations_with_initializers_cannot_also_have_definite_assignment_assertions : node.Type() == null ? Diagnostics.Declarations_with_definite_assignment_assertions_must_also_have_type_annotations : Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context;
            return grammarErrorOnNode(exclamation, message);
        }

        return scope != 0 && checkGrammarNameInLetOrConstDeclarations(node.Name());
    }

    private bool checkGrammarNameInLetOrConstDeclarations(Node name)
    {
        if (name.Kind != Kind.Identifier)
            throw unsupported("checkGrammarNameInLetOrConstDeclarations: binding pattern");
        if (name.Text() == "let")
            return grammarErrorOnNode(name, Diagnostics.X_let_is_not_allowed_to_be_used_as_a_name_in_let_or_const_declarations);
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

    private static bool containerAllowsBlockScopedVariable(Node parent) => parent.Kind is not (Kind.IfStatement or Kind.DoStatement or Kind.WhileStatement or Kind.WithStatement or Kind.ForStatement or Kind.ForInStatement or Kind.ForOfStatement or Kind.LabeledStatement);
    private void checkGrammarNumericLiteral(Node node)
    {
        var nodeText = TypeScript.Scanner.Scanner.GetTextOfNodeByteString(node);
        bool fractional = nodeText.Contains('.');
        bool scientific = (node.AsNumericLiteral().TokenFlags & TokenFlags.Scientific) != 0;
        if (fractional || scientific)
            return;
        var value = JsNum.FromString(node.Text());
        if (value.Value <= 9007199254740991)
            return;
        // This upstream path produces a suggestion, not a file semantic error.
        suggestionDiagnostics.Add(NewDiagnosticForNode(node, Diagnostics.Numeric_literals_with_absolute_values_equal_to_2_53_or_greater_are_too_large_to_be_represented_accurately_as_integers));
    }

    private readonly List<Diagnostic> suggestionDiagnostics = new();
}

public sealed partial class Checker
{
    private bool checkGrammarObjectLiteralExpression(Node node, bool inDestructuring = false)
    {
        var seen = new Dictionary<string, DeclarationMeaning>();
        List<Node> properties = default;
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

                    var existingKind = seen.GetValueOrDefault(effectiveName);
                    if ((existingKind == 0))
                    {
                        seen[effectiveName] = currentKind;
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
                                        seen[effectiveName] = (currentKind | existingKind);
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
        if (node.Parent.Kind is not (Kind.TypeLiteral or Kind.InterfaceDeclaration) || node.Kind != Kind.PropertySignature)
            throw unsupported("checkGrammarProperty: source class property");
        if (node.Name().Kind == Kind.ComputedPropertyName)
            throw unsupported("checkGrammarProperty: computed name");
        if (node.Initializer() != null)
            return grammarErrorOnNode(node.Initializer(), node.Parent.Kind == Kind.InterfaceDeclaration ? Diagnostics.An_interface_property_cannot_have_an_initializer : Diagnostics.A_type_literal_property_cannot_have_an_initializer);
        if ((node.Flags & NodeFlags.Ambient) != 0)
            checkAmbientInitializer(node);
        return false;
    }
}

public sealed partial class Checker
{
    // grammarchecks.go:935. Source parameter/signature restrictions are admission-owned.
    private bool checkGrammarInterfaceDeclaration(InterfaceDeclaration node)
    {
        bool seenExtends = false;
        foreach (var clauseNode in node.HeritageClauses?.Nodes ?? [])
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
        if (types != null && (types.Nodes?.Count ?? 0) == 0)
            return grammarErrorAtPos(node.AsNode(), types.Pos(), 0, Diagnostics.X_0_list_cannot_be_empty, TypeScript.Scanner.Scanner.TokenToString(node.Token));
        foreach (var element in types?.Nodes ?? [])
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

    private bool checkGrammarTypeArguments(Node node, NodeList arguments)
    {
        if (checkGrammarForDisallowedTrailingComma(arguments, Diagnostics.Trailing_comma_not_allowed))
            return true;
        if (arguments != null && (arguments.Nodes?.Count ?? 0) == 0)
            return grammarErrorOnNode(node, Diagnostics.Type_argument_list_cannot_be_empty);
        return false;
    }
}

public sealed partial class Checker
{
    private bool grammarErrorOnFirstToken(Node node, Message message, params object[] args)
    {
        var file = Ast.GetSourceFileOfNode(node);
        if ((file.Diagnostics()?.Count ?? 0) != 0)
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
            var end = (TypeScript.Scanner.Scanner.SkipTrivia(file.Text(), typeParameters.End()) + GoRuntime.Len(">"));
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
        var arrowFullText = GoRuntime.Slice(file.Text(), equalsGreaterThanToken.Pos(), equalsGreaterThanToken.End());
        return (arrowFullText.Any(ch => StringUtil.IsLineBreak(ch)) && this.grammarErrorOnNode(equalsGreaterThanToken, Diagnostics.Line_terminator_not_permitted_before_arrow));
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
        if (!Ast.HasDynamicName(node.Parent))
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
            throw unsupported("isInitializerSimpleLiteralEnumReference: element access");
        }

        return false;
    }
}
