// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned declaration support, fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Transformers.Declarations;
internal static partial class DeclarationTransforms
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:18:1.
    internal static GetSymbolAccessibilityDiagnostic WrapSimpleDiagnosticSelector(Node node, Func<Node, SymbolAccessibilityResult, Message> selector)
    {
        return (symbolAccessibilityResult2) =>
        {
            var diagnosticMessage = selector(node, symbolAccessibilityResult2);
            if ((diagnosticMessage == null))
            {
                return null;
            }

            return new SymbolAccessibilityDiagnostic(node, diagnosticMessage, Ast.GetNameOfDeclaration(node));
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:32:1.
    internal static GetSymbolAccessibilityDiagnostic WrapNamedDiagnosticSelector(Node node, Func<Node, SymbolAccessibilityResult, Message> selector)
    {
        return (symbolAccessibilityResult2) =>
        {
            var diagnosticMessage = selector(node, symbolAccessibilityResult2);
            if ((diagnosticMessage == null))
            {
                return null;
            }

            var name = Ast.GetNameOfDeclaration(node);
            return new SymbolAccessibilityDiagnostic(name, diagnosticMessage, name);
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:47:1.
    internal static GetSymbolAccessibilityDiagnostic WrapFallbackErrorDiagnosticSelector(Node node, Func<Node, SymbolAccessibilityResult, Message> selector)
    {
        return (symbolAccessibilityResult2) =>
        {
            var diagnosticMessage = selector(node, symbolAccessibilityResult2);
            if ((diagnosticMessage == null))
            {
                return null;
            }

            var errorNode = Ast.GetNameOfDeclaration(node);
            if ((errorNode == null))
            {
                errorNode = node;
            }

            return new SymbolAccessibilityDiagnostic(errorNode, diagnosticMessage, null);
        };
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:64:1.
    internal static Message SelectDiagnosticBasedOnModuleName(SymbolAccessibilityResult symbolAccessibilityResult, Message moduleNotNameable, Message privateModule, Message nonModule)
    {
        if ((GoRuntime.Len(symbolAccessibilityResult.ErrorModuleName) > 0))
        {
            if ((symbolAccessibilityResult.Accessibility == SymbolAccessibility.CannotBeNamed))
            {
                return moduleNotNameable;
            }

            return privateModule;
        }

        return nonModule;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:74:1.
    internal static Message SelectDiagnosticBasedOnModuleNameNoNameCheck(SymbolAccessibilityResult symbolAccessibilityResult, Message privateModule, Message nonModule)
    {
        if ((GoRuntime.Len(symbolAccessibilityResult.ErrorModuleName) > 0))
        {
            return privateModule;
        }

        return nonModule;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:81:1.
    internal static GetSymbolAccessibilityDiagnostic CreateGetSymbolAccessibilityDiagnosticForNodeName(Node node)
    {
        if ((Ast.IsSetAccessorDeclaration(node) || Ast.IsGetAccessorDeclaration(node)))
        {
            return WrapSimpleDiagnosticSelector(node, GetAccessorNameVisibilityDiagnosticMessage);
        }
        else if ((Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node)))
        {
            return WrapSimpleDiagnosticSelector(node, GetMethodNameVisibilityDiagnosticMessage);
        }
        else
        {
            return CreateGetSymbolAccessibilityDiagnosticForNode(node);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:91:1.
    internal static Message GetAccessorNameVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        if (Ast.IsStatic(node))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_private_name_1);
        }
        else if ((node.Parent.Kind == Kind.ClassDeclaration))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_private_name_1);
        }
        else
        {
            return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Property_0_of_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Property_0_of_exported_interface_has_or_is_using_private_name_1);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:115:1.
    internal static Message GetMethodNameVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        if (Ast.IsStatic(node))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_static_method_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_static_method_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_static_method_0_of_exported_class_has_or_is_using_private_name_1);
        }
        else if ((node.Parent.Kind == Kind.ClassDeclaration))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_method_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_method_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_method_0_of_exported_class_has_or_is_using_private_name_1);
        }
        else
        {
            return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Method_0_of_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Method_0_of_exported_interface_has_or_is_using_private_name_1);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:139:1.
    internal static GetSymbolAccessibilityDiagnostic CreateGetSymbolAccessibilityDiagnosticForNode(Node node)
    {
        if ((((((((Ast.IsVariableDeclaration(node) || Ast.IsPropertyDeclaration(node)) || Ast.IsPropertySignatureDeclaration(node)) || Ast.IsPropertyAccessExpression(node)) || Ast.IsElementAccessExpression(node)) || Ast.IsBinaryExpression(node)) || Ast.IsBindingElement(node)) || Ast.IsConstructorDeclaration(node)))
        {
            return WrapSimpleDiagnosticSelector(node, GetVariableDeclarationTypeVisibilityDiagnosticMessage);
        }
        else if ((Ast.IsSetAccessorDeclaration(node) || Ast.IsGetAccessorDeclaration(node)))
        {
            return WrapNamedDiagnosticSelector(node, GetAccessorDeclarationTypeVisibilityDiagnosticMessage);
        }
        else if ((((((Ast.IsConstructSignatureDeclaration(node) || Ast.IsCallSignatureDeclaration(node)) || Ast.IsMethodDeclaration(node)) || Ast.IsMethodSignatureDeclaration(node)) || Ast.IsFunctionDeclaration(node)) || Ast.IsIndexSignatureDeclaration(node)))
        {
            return WrapFallbackErrorDiagnosticSelector(node, GetReturnTypeVisibilityDiagnosticMessage);
        }
        else if (Ast.IsParameterDeclaration(node))
        {
            if ((Ast.IsParameterPropertyDeclaration(node, node.Parent) && Ast.HasSyntacticModifier(node.Parent, ModifierFlags.Private)))
            {
                return WrapSimpleDiagnosticSelector(node, GetVariableDeclarationTypeVisibilityDiagnosticMessage);
            }

            return WrapSimpleDiagnosticSelector(node, GetParameterDeclarationTypeVisibilityDiagnosticMessage);
        }
        else if (Ast.IsTypeParameterDeclaration(node))
        {
            return WrapSimpleDiagnosticSelector(node, GetTypeParameterConstraintVisibilityDiagnosticMessage);
        }
        else if (Ast.IsExpressionWithTypeArguments(node))
        {
            return (symbolAccessibilityResult) =>
            {
                Message diagnosticMessage = default;
                if (Ast.IsClassDeclaration(node.Parent.Parent))
                {
                    if ((Ast.IsHeritageClause(node.Parent) && (node.Parent.AsHeritageClause().Token == Kind.ImplementsKeyword)))
                    {
                        diagnosticMessage = Diagnostics.Implements_clause_of_exported_class_0_has_or_is_using_private_name_1;
                    }
                    else if ((node.Parent.Parent.Name() != null))
                    {
                        diagnosticMessage = Diagnostics.X_extends_clause_of_exported_class_0_has_or_is_using_private_name_1;
                    }
                    else
                    {
                        diagnosticMessage = Diagnostics.X_extends_clause_of_exported_class_has_or_is_using_private_name_0;
                    }
                }
                else
                {
                    diagnosticMessage = Diagnostics.X_extends_clause_of_exported_interface_0_has_or_is_using_private_name_1;
                }

                return new SymbolAccessibilityDiagnostic(node, diagnosticMessage, Ast.GetNameOfDeclaration(node.Parent.Parent));
            };
        }
        else if (Ast.IsImportEqualsDeclaration(node))
        {
            return WrapSimpleDiagnosticSelector(node, (_, _2) =>
            {
                return Diagnostics.Import_declaration_0_is_using_private_name_1;
            });
        }
        else if ((Ast.IsTypeAliasDeclaration(node) || Ast.IsJSTypeAliasDeclaration(node)))
        {
            return (symbolAccessibilityResult2) =>
            {
                var diagnosticMessage2 = SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult2, Diagnostics.Exported_type_alias_0_has_or_is_using_private_name_1_from_module_2, Diagnostics.Exported_type_alias_0_has_or_is_using_private_name_1);
                var errorNode = node.Type();
                var typeName = node.Name();
                return new SymbolAccessibilityDiagnostic(errorNode, diagnosticMessage2, typeName);
            };
        }
        else if (Ast.IsCallExpression(node))
        {
            return (symbolAccessibilityResult3) =>
            {
                var diagnosticMessage3 = SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult3, Diagnostics.Exported_variable_0_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Exported_variable_0_has_or_is_using_name_1_from_private_module_2, Diagnostics.Exported_variable_0_has_or_is_using_private_name_1);
                var errorNode2 = node.Arguments()[1];
                var typeName2 = node.Arguments()[1];
                return new SymbolAccessibilityDiagnostic(errorNode2, diagnosticMessage3, typeName2);
            };
        }
        else
        {
            throw new InvalidOperationException(("Attempted to set a declaration diagnostic context for unhandled node kind: " + node.Kind.String()));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:223:1.
    internal static Message GetVariableDeclarationTypeVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        if (((node.Kind == Kind.VariableDeclaration) || (node.Kind == Kind.BindingElement)))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Exported_variable_0_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Exported_variable_0_has_or_is_using_name_1_from_private_module_2, Diagnostics.Exported_variable_0_has_or_is_using_private_name_1);
        }
        else if (((((((node.Kind == Kind.PropertyDeclaration) || (node.Kind == Kind.PropertyAccessExpression)) || (node.Kind == Kind.ElementAccessExpression)) || (node.Kind == Kind.BinaryExpression)) || (node.Kind == Kind.PropertySignature)) || (((node.Kind == Kind.Parameter) && Ast.HasSyntacticModifier(node.Parent, ModifierFlags.Private)))))
        {
            if (Ast.IsStatic(node))
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_static_property_0_of_exported_class_has_or_is_using_private_name_1);
            }
            else if (((node.Parent.Kind == Kind.ClassDeclaration) || (node.Kind == Kind.Parameter)))
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Public_property_0_of_exported_class_has_or_is_using_private_name_1);
            }
            else
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Property_0_of_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Property_0_of_exported_interface_has_or_is_using_private_name_1);
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:263:1.
    internal static Message GetAccessorDeclarationTypeVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        if ((node.Kind == Kind.SetAccessor))
        {
            if (Ast.IsStatic(node))
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_type_of_public_static_setter_0_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_type_of_public_static_setter_0_from_exported_class_has_or_is_using_private_name_1);
            }
            else
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_type_of_public_setter_0_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_type_of_public_setter_0_from_exported_class_has_or_is_using_private_name_1);
            }
        }
        else if (Ast.IsStatic(node))
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Return_type_of_public_static_getter_0_from_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Return_type_of_public_static_getter_0_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Return_type_of_public_static_getter_0_from_exported_class_has_or_is_using_private_name_1);
        }
        else
        {
            return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Return_type_of_public_getter_0_from_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Return_type_of_public_getter_0_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Return_type_of_public_getter_0_from_exported_class_has_or_is_using_private_name_1);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:299:1.
    internal static Message GetReturnTypeVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        switch (node.Kind)
        {
            case Kind.ConstructSignature:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Return_type_of_constructor_signature_from_exported_interface_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_constructor_signature_from_exported_interface_has_or_is_using_private_name_0);
            }

            case Kind.CallSignature:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Return_type_of_call_signature_from_exported_interface_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_call_signature_from_exported_interface_has_or_is_using_private_name_0);
            }

            case Kind.IndexSignature:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Return_type_of_index_signature_from_exported_interface_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_index_signature_from_exported_interface_has_or_is_using_private_name_0);
            }

            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            {
                if (Ast.IsStatic(node))
                {
                    return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Return_type_of_public_static_method_from_exported_class_has_or_is_using_name_0_from_external_module_1_but_cannot_be_named, Diagnostics.Return_type_of_public_static_method_from_exported_class_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_public_static_method_from_exported_class_has_or_is_using_private_name_0);
                }
                else if ((node.Parent.Kind == Kind.ClassDeclaration))
                {
                    return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Return_type_of_public_method_from_exported_class_has_or_is_using_name_0_from_external_module_1_but_cannot_be_named, Diagnostics.Return_type_of_public_method_from_exported_class_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_public_method_from_exported_class_has_or_is_using_private_name_0);
                }
                else
                {
                    return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Return_type_of_method_from_exported_interface_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_method_from_exported_interface_has_or_is_using_private_name_0);
                }
            }

            case Kind.FunctionDeclaration:
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Return_type_of_exported_function_has_or_is_using_name_0_from_external_module_1_but_cannot_be_named, Diagnostics.Return_type_of_exported_function_has_or_is_using_name_0_from_private_module_1, Diagnostics.Return_type_of_exported_function_has_or_is_using_private_name_0);
            }

            default:
            {
                throw new InvalidOperationException(("This is unknown kind for signature: " + node.Kind.String()));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:358:1.
    internal static Message GetParameterDeclarationTypeVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        switch (node.Parent.Kind)
        {
            case Kind.Constructor:
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Parameter_0_of_constructor_from_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Parameter_0_of_constructor_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_constructor_from_exported_class_has_or_is_using_private_name_1);
            }

            case Kind.ConstructSignature:
            case Kind.ConstructorType:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_0_of_constructor_signature_from_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_constructor_signature_from_exported_interface_has_or_is_using_private_name_1);
            }

            case Kind.CallSignature:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_0_of_call_signature_from_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_call_signature_from_exported_interface_has_or_is_using_private_name_1);
            }

            case Kind.IndexSignature:
            {
                return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_0_of_index_signature_from_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_index_signature_from_exported_interface_has_or_is_using_private_name_1);
            }

            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            {
                if (Ast.IsStatic(node.Parent))
                {
                    return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Parameter_0_of_public_static_method_from_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Parameter_0_of_public_static_method_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_public_static_method_from_exported_class_has_or_is_using_private_name_1);
                }
                else if ((node.Parent.Parent.Kind == Kind.ClassDeclaration))
                {
                    return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Parameter_0_of_public_method_from_exported_class_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Parameter_0_of_public_method_from_exported_class_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_public_method_from_exported_class_has_or_is_using_private_name_1);
                }
                else
                {
                    return SelectDiagnosticBasedOnModuleNameNoNameCheck(symbolAccessibilityResult, Diagnostics.Parameter_0_of_method_from_exported_interface_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_method_from_exported_interface_has_or_is_using_private_name_1);
                }
            }

            case Kind.FunctionDeclaration:
            case Kind.FunctionType:
            case Kind.ArrowFunction:
            case Kind.FunctionExpression:
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Parameter_0_of_exported_function_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Parameter_0_of_exported_function_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_exported_function_has_or_is_using_private_name_1);
            }

            case Kind.SetAccessor:
            case Kind.GetAccessor:
            {
                return SelectDiagnosticBasedOnModuleName(symbolAccessibilityResult, Diagnostics.Parameter_0_of_accessor_has_or_is_using_name_1_from_external_module_2_but_cannot_be_named, Diagnostics.Parameter_0_of_accessor_has_or_is_using_name_1_from_private_module_2, Diagnostics.Parameter_0_of_accessor_has_or_is_using_private_name_1);
            }

            default:
            {
                throw new InvalidOperationException(("Unknown parent for parameter: " + node.Parent.Kind.String()));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:436:1.
    internal static Message GetTypeParameterConstraintVisibilityDiagnosticMessage(Node node, SymbolAccessibilityResult symbolAccessibilityResult)
    {
        switch (node.Parent.Kind)
        {
            case Kind.ClassDeclaration:
            {
                return Diagnostics.Type_parameter_0_of_exported_class_has_or_is_using_private_name_1;
            }

            case Kind.InterfaceDeclaration:
            {
                return Diagnostics.Type_parameter_0_of_exported_interface_has_or_is_using_private_name_1;
            }

            case Kind.MappedType:
            {
                return Diagnostics.Type_parameter_0_of_exported_mapped_object_type_is_using_private_name_1;
            }

            case Kind.ConstructorType:
            case Kind.ConstructSignature:
            {
                return Diagnostics.Type_parameter_0_of_constructor_signature_from_exported_interface_has_or_is_using_private_name_1;
            }

            case Kind.CallSignature:
            {
                return Diagnostics.Type_parameter_0_of_call_signature_from_exported_interface_has_or_is_using_private_name_1;
            }

            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            {
                if (Ast.IsStatic(node.Parent))
                {
                    return Diagnostics.Type_parameter_0_of_public_static_method_from_exported_class_has_or_is_using_private_name_1;
                }
                else if ((node.Parent.Parent.Kind == Kind.ClassDeclaration))
                {
                    return Diagnostics.Type_parameter_0_of_public_method_from_exported_class_has_or_is_using_private_name_1;
                }
                else
                {
                    return Diagnostics.Type_parameter_0_of_method_from_exported_interface_has_or_is_using_private_name_1;
                }
            }

            case Kind.FunctionType:
            case Kind.FunctionDeclaration:
            {
                return Diagnostics.Type_parameter_0_of_exported_function_has_or_is_using_private_name_1;
            }

            case Kind.InferType:
            {
                return Diagnostics.Extends_clause_for_inferred_type_0_has_or_is_using_private_name_1;
            }

            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                return Diagnostics.Type_parameter_0_of_exported_type_alias_has_or_is_using_private_name_1;
            }

            default:
            {
                throw new InvalidOperationException(("This is unknown parent for type parameter: " + node.Parent.Kind.String()));
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:471:1.
    internal static Message GetRelatedSuggestionByDeclarationKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.ArrowFunction:
            {
                return Diagnostics.Add_a_return_type_to_the_function_expression;
            }

            case Kind.FunctionExpression:
            {
                return Diagnostics.Add_a_return_type_to_the_function_expression;
            }

            case Kind.MethodDeclaration:
            {
                return Diagnostics.Add_a_return_type_to_the_method;
            }

            case Kind.GetAccessor:
            {
                return Diagnostics.Add_a_return_type_to_the_get_accessor_declaration;
            }

            case Kind.SetAccessor:
            {
                return Diagnostics.Add_a_type_to_parameter_of_the_set_accessor_declaration;
            }

            case Kind.FunctionDeclaration:
            {
                return Diagnostics.Add_a_return_type_to_the_function_declaration;
            }

            case Kind.ConstructSignature:
            {
                return Diagnostics.Add_a_return_type_to_the_function_declaration;
            }

            case Kind.Parameter:
            {
                return Diagnostics.Add_a_type_annotation_to_the_parameter_0;
            }

            case Kind.VariableDeclaration:
            {
                return Diagnostics.Add_a_type_annotation_to_the_variable_0;
            }

            case Kind.PropertyDeclaration:
            {
                return Diagnostics.Add_a_type_annotation_to_the_property_0;
            }

            case Kind.PropertySignature:
            {
                return Diagnostics.Add_a_type_annotation_to_the_property_0;
            }

            case Kind.ExportAssignment:
            {
                return Diagnostics.Move_the_expression_in_default_export_to_a_variable_and_add_a_type_annotation_to_it;
            }

            default:
            {
                return null;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:502:1.
    internal static Message GetErrorByDeclarationKind(Kind kind)
    {
        switch (kind)
        {
            case Kind.FunctionExpression:
            {
                return Diagnostics.Function_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations;
            }

            case Kind.FunctionDeclaration:
            {
                return Diagnostics.Function_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations;
            }

            case Kind.ArrowFunction:
            {
                return Diagnostics.Function_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations;
            }

            case Kind.MethodDeclaration:
            {
                return Diagnostics.Method_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations;
            }

            case Kind.ConstructSignature:
            {
                return Diagnostics.Method_must_have_an_explicit_return_type_annotation_with_isolatedDeclarations;
            }

            case Kind.GetAccessor:
            {
                return Diagnostics.At_least_one_accessor_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.SetAccessor:
            {
                return Diagnostics.At_least_one_accessor_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.Parameter:
            {
                return Diagnostics.Parameter_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.VariableDeclaration:
            {
                return Diagnostics.Variable_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.PropertyDeclaration:
            {
                return Diagnostics.Property_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.PropertySignature:
            {
                return Diagnostics.Property_must_have_an_explicit_type_annotation_with_isolatedDeclarations;
            }

            case Kind.ComputedPropertyName:
            {
                return Diagnostics.Computed_property_names_on_class_or_object_literals_cannot_be_inferred_with_isolatedDeclarations;
            }

            case Kind.SpreadAssignment:
            {
                return Diagnostics.Objects_that_contain_spread_assignments_can_t_be_inferred_with_isolatedDeclarations;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                return Diagnostics.Objects_that_contain_shorthand_properties_can_t_be_inferred_with_isolatedDeclarations;
            }

            case Kind.ArrayLiteralExpression:
            {
                return Diagnostics.Only_const_arrays_can_be_inferred_with_isolatedDeclarations;
            }

            case Kind.ExportAssignment:
            {
                return Diagnostics.Default_exports_can_t_be_inferred_with_isolatedDeclarations;
            }

            case Kind.SpreadElement:
            {
                return Diagnostics.Arrays_with_spread_elements_can_t_inferred_with_isolatedDeclarations;
            }

            default:
            {
                return null;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:543:1.
    internal static bool IsDeclarationEnoughForErrors(Node node)
    {
        return ((((Ast.IsExportAssignment(node) || Ast.IsStatement(node)) || Ast.IsVariableDeclaration(node)) || Ast.IsPropertyDeclaration(node)) || Ast.IsParameterDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:547:1.
    internal static bool IsFunctionLikeAndNotConstructor(Node node)
    {
        return (Ast.IsFunctionLikeDeclaration(node) && !Ast.IsConstructorDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:551:1.
    internal static Node FindNearestDeclaration(Node node)
    {
        var result = Ast.FindAncestor(node, IsDeclarationEnoughForErrors);
        if ((result == null))
        {
            return null;
        }

        if (Ast.IsExportAssignment(result))
        {
            return result;
        }

        if (Ast.IsReturnStatement(result))
        {
            return Ast.FindAncestor(result, IsFunctionLikeAndNotConstructor);
        }

        if (Ast.IsStatement(result))
        {
            return null;
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:568:1.
    internal static Diagnostic CreateEntityInTypeNodeError(Node node)
    {
        var diag = CreateDiagnosticForNode(node, Diagnostics.Type_containing_private_name_0_can_t_be_used_with_isolatedDeclarations, ScannerApi.GetTextOfNodeValue(node));
        AddParentDeclarationRelatedInfo(node, diag);
        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:574:1.
    internal static void AddParentDeclarationRelatedInfo(Node node, Diagnostic diag)
    {
        var parentDeclaration = FindNearestDeclaration(node);
        if ((parentDeclaration == null))
        {
            return;
        }

        ByteStringValue targetStr = ByteStringValue.FromString("");
        if ((!Ast.IsExportAssignment(parentDeclaration) && (parentDeclaration.Name() != null)))
        {
            targetStr = ScannerApi.GetTextOfNodeValue(parentDeclaration.Name());
        }

        diag.AddRelatedInfo(CreateDiagnosticForNode(parentDeclaration, GetRelatedSuggestionByDeclarationKind(parentDeclaration.Kind), targetStr));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:586:1.
    internal static Diagnostic CreateAccessorTypeError(Node node)
    {
        var allDeclarations = Ast.GetAllAccessorDeclarationsForDeclaration(node, node.Symbol().Declarations);
        var getAccessor = allDeclarations.GetAccessor;
        var setAccessor = allDeclarations.SetAccessor;
        var targetNode = node;
        if ((Ast.IsSetAccessorDeclaration(node) && (GoRuntime.Len(node.Parameters()) > 0)))
        {
            targetNode = node.Parameters()[0];
        }

        var diag = CreateDiagnosticForNode(targetNode, GetErrorByDeclarationKind(node.Kind));
        if ((setAccessor != null))
        {
            diag.AddRelatedInfo(CreateDiagnosticForNode(setAccessor.AsNode(), GetRelatedSuggestionByDeclarationKind(setAccessor.Kind)));
        }

        if ((getAccessor != null))
        {
            diag.AddRelatedInfo(CreateDiagnosticForNode(getAccessor.AsNode(), GetRelatedSuggestionByDeclarationKind(getAccessor.Kind)));
        }

        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:604:1.
    internal static Diagnostic CreateObjectLiteralError(Node node)
    {
        var diag = CreateDiagnosticForNode(node, GetErrorByDeclarationKind(node.Kind));
        AddParentDeclarationRelatedInfo(node, diag);
        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:610:1.
    internal static Diagnostic CreateArrayLiteralError(Node node)
    {
        var diag = CreateDiagnosticForNode(node, GetErrorByDeclarationKind(node.Kind));
        AddParentDeclarationRelatedInfo(node, diag);
        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:616:1.
    internal static Diagnostic CreateReturnTypeError(Node node)
    {
        var diag = CreateDiagnosticForNode(node, GetErrorByDeclarationKind(node.Kind));
        AddParentDeclarationRelatedInfo(node, diag);
        diag.AddRelatedInfo(CreateDiagnosticForNode(node, GetRelatedSuggestionByDeclarationKind(node.Kind)));
        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:623:1.
    internal static Diagnostic CreateBindingElementError(Node node)
    {
        return CreateDiagnosticForNode(node, Diagnostics.Binding_elements_with_initializers_can_t_be_exported_directly_with_isolatedDeclarations);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:627:1.
    internal static Diagnostic CreateVariableOrPropertyError(Node node)
    {
        var diag = CreateDiagnosticForNode(node, GetErrorByDeclarationKind(node.Kind));
        diag.AddRelatedInfo(CreateDiagnosticForNode(node, GetRelatedSuggestionByDeclarationKind(node.Kind), ScannerApi.GetTextOfNodeValue(node.Name())));
        return diag;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:633:1.
    internal static Diagnostic CreateExpressionError(Node node)
    {
        return CreateExpressionErrorEx(node, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:637:1.
    internal static Diagnostic CreateClassExpressionError(Node node)
    {
        return CreateExpressionErrorEx(node, Diagnostics.Inference_from_class_expressions_is_not_supported_with_isolatedDeclarations);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:641:1.
    internal static FindAncestorResult IsParentForIDDIagnostic(Node node)
    {
        if (Ast.IsExportAssignment(node))
        {
            return FindAncestorResult.True;
        }

        if (Ast.IsStatement(node))
        {
            return FindAncestorResult.Quit;
        }

        return Ast.ToFindAncestorResult((!Ast.IsParenthesizedExpression(node) && !Ast.IsAssertionExpression(node)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:651:1.
    internal static Diagnostic CreateExpressionErrorEx(Node node, Message diagnosticMessage)
    {
        var parentDeclaration = FindNearestDeclaration(node);
        if ((parentDeclaration == null))
        {
            if ((diagnosticMessage == null))
            {
                diagnosticMessage = Diagnostics.Expression_type_can_t_be_inferred_with_isolatedDeclarations;
            }

            return CreateDiagnosticForNode(node, diagnosticMessage);
        }

        ByteStringValue targetStr = ByteStringValue.FromString("");
        if ((!Ast.IsExportAssignment(parentDeclaration) && (parentDeclaration.Name() != null)))
        {
            targetStr = ScannerApi.GetTextOfNodeValue(parentDeclaration.Name());
        }

        var parent = Ast.FindAncestorOrQuit(node.Parent, IsParentForIDDIagnostic);
        if ((parentDeclaration == parent))
        {
            if ((diagnosticMessage == null))
            {
                diagnosticMessage = GetErrorByDeclarationKind(parentDeclaration.Kind);
            }

            var diag = CreateDiagnosticForNode(node, diagnosticMessage);
            diag.AddRelatedInfo(CreateDiagnosticForNode(parentDeclaration, GetRelatedSuggestionByDeclarationKind(parentDeclaration.Kind), targetStr));
            return diag;
        }

        if ((diagnosticMessage == null))
        {
            diagnosticMessage = Diagnostics.Expression_type_can_t_be_inferred_with_isolatedDeclarations;
        }

        var diag2 = CreateDiagnosticForNode(node, diagnosticMessage);
        diag2.AddRelatedInfo(CreateDiagnosticForNode(parentDeclaration, GetRelatedSuggestionByDeclarationKind(parentDeclaration.Kind), targetStr));
        diag2.AddRelatedInfo(CreateDiagnosticForNode(node, Diagnostics.Add_satisfies_and_a_type_assertion_to_this_expression_satisfies_T_as_T_to_make_the_type_explicit));
        return diag2;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go:683:1.
    internal static Func<Node, Diagnostic> CreateGetIsolatedDeclarationErrors(IEmitResolver resolver)
    {
        Func<Node, Diagnostic> createParameterError = (node2) =>
        {
            if (Ast.IsSetAccessorDeclaration(node2.Parent))
            {
                return CreateAccessorTypeError(node2.Parent);
            }

            var addUndefined = resolver.RequiresAddingImplicitUndefinedUnsafe(node2, null, null);
            if ((!addUndefined && (node2.Initializer() != null)))
            {
                return CreateExpressionError(node2.Initializer());
            }

            var message = GetErrorByDeclarationKind(node2.Kind);
            if (addUndefined)
            {
                message = Diagnostics.Declaration_emit_for_this_parameter_requires_implicitly_adding_undefined_to_its_type_This_is_not_supported_with_isolatedDeclarations;
            }

            var diag = CreateDiagnosticForNode(node2, message);
            var targetStr = ScannerApi.GetTextOfNodeValue(node2.Name());
            diag.AddRelatedInfo(CreateDiagnosticForNode(node2, GetRelatedSuggestionByDeclarationKind(node2.Kind), targetStr));
            return diag;
        };
        return (node3) =>
        {
            var heritageClause = Ast.FindAncestor(node3, Ast.IsHeritageClause);
            if ((heritageClause != null))
            {
                return CreateDiagnosticForNode(node3, Diagnostics.Extends_clause_can_t_contain_an_expression_with_isolatedDeclarations);
            }

            if ((Ast.IsPartOfTypeNode(node3) || Ast.IsTypeQueryNode(node3)))
            {
                return CreateEntityInTypeNodeError(node3);
            }

            if ((Ast.IsEntityName(node3) || Ast.IsEntityNameExpression(node3)))
            {
                return CreateEntityInTypeNodeError(node3);
            }

            switch (node3.Kind)
            {
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                {
                    return CreateAccessorTypeError(node3);
                }

                case Kind.ComputedPropertyName:
                case Kind.ShorthandPropertyAssignment:
                case Kind.SpreadAssignment:
                {
                    return CreateObjectLiteralError(node3);
                }

                case Kind.ArrayLiteralExpression:
                case Kind.SpreadElement:
                {
                    return CreateArrayLiteralError(node3);
                }

                case Kind.MethodDeclaration:
                case Kind.ConstructSignature:
                case Kind.FunctionExpression:
                case Kind.ArrowFunction:
                case Kind.FunctionDeclaration:
                {
                    return CreateReturnTypeError(node3);
                }

                case Kind.BindingElement:
                {
                    return CreateBindingElementError(node3);
                }

                case Kind.PropertyDeclaration:
                case Kind.VariableDeclaration:
                {
                    return CreateVariableOrPropertyError(node3);
                }

                case Kind.Parameter:
                {
                    return createParameterError(node3);
                }

                case Kind.PropertyAssignment:
                {
                    return CreateExpressionError(node3.Initializer());
                }

                case Kind.ClassExpression:
                {
                    return CreateClassExpressionError(node3);
                }

                default:
                {
                    return CreateExpressionError(node3);
                }
            }
        };
    }
}
