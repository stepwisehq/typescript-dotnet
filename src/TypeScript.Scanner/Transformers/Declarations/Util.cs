// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete declaration counterpart, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using NodeBuilderInternalFlags = TypeScript.NodeBuilding.InternalFlags;
using static TypeScript.Transformers.Declarations.DeclarationTransforms;

namespace TypeScript.Transformers.Declarations;
internal static partial class DeclarationTransforms
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:9:1.
    internal static bool NeedsScopeMarker(Node result)
    {
        return (((!Ast.IsAnyImportOrReExport(result) && !Ast.IsExportAssignment(result)) && !Ast.HasSyntacticModifier(result, ModifierFlags.Export)) && !Ast.IsAmbientModule(result));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:13:1.
    internal static bool CanHaveLiteralInitializer(IEmitResolver resolver, Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            {
                return (resolver.GetEffectiveDeclarationFlags(node, ModifierFlags.Private) == 0);
            }

            case Kind.Parameter:
            case Kind.VariableDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:25:1.
    internal static bool CanProduceDiagnostics(Node node)
    {
        return Ast.IsVariableDeclaration(node) || Ast.IsPropertyDeclaration(node) || Ast.IsPropertySignatureDeclaration(node) || Ast.IsBindingElement(node) || Ast.IsSetAccessorDeclaration(node) || Ast.IsGetAccessorDeclaration(node) || Ast.IsConstructSignatureDeclaration(node) || Ast.IsCallSignatureDeclaration(node) || Ast.IsMethodDeclaration(node) || Ast.IsMethodSignatureDeclaration(node) || Ast.IsFunctionDeclaration(node) || Ast.IsParameterDeclaration(node) || Ast.IsTypeParameterDeclaration(node) || Ast.IsExpressionWithTypeArguments(node) || Ast.IsImportEqualsDeclaration(node) || Ast.IsTypeAliasDeclaration(node) || Ast.IsJSTypeAliasDeclaration(node) || Ast.IsConstructorDeclaration(node) || Ast.IsIndexSignatureDeclaration(node) || Ast.IsPropertyAccessExpression(node) || Ast.IsElementAccessExpression(node) || Ast.IsBinaryExpression(node) || Ast.IsCallExpression(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:52:1.
    internal static bool CanReuseModifierNodes(Slice<Node> nodes)
    {
        {
            foreach (var node in GoRuntime.RangeValues(nodes))
            {
                if ((Ast.IsModifier(node) && ((node.Flags & NodeFlags.Reparsed) != 0)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:61:1.
    internal static bool IsDeclarationAndNotVisible(EmitContext emitContext, IEmitResolver resolver, Node node)
    {
        node = emitContext.ParseNode(node);
        switch (node.Kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.ClassDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.EnumDeclaration:
            {
                return !resolver.IsDeclarationVisible(node);
            }

            case Kind.VariableDeclaration:
            {
                return !GetBindingNameVisible(resolver, node);
            }

            case Kind.ImportEqualsDeclaration:
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            case Kind.ExportDeclaration:
            case Kind.ExportAssignment:
            {
                return false;
            }

            case Kind.ClassStaticBlockDeclaration:
            {
                return true;
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:87:1.
    internal static bool GetBindingNameVisible(IEmitResolver resolver, Node elem)
    {
        if (Ast.IsOmittedExpression(elem))
        {
            return false;
        }

        if ((elem.Name() == null))
        {
            return false;
        }

        if (Ast.IsBindingPattern(elem.Name()))
        {
            {
                foreach (var elem2 in GoRuntime.RangeValues(elem.Name().Elements()))
                {
                    if (GetBindingNameVisible(resolver, elem2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        else
        {
            return resolver.IsDeclarationVisible(elem);
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:109:1.
    internal static bool IsEnclosingDeclaration(Node node)
    {
        return (((((((((Ast.IsSourceFile(node) || Ast.IsTypeAliasDeclaration(node)) || Ast.IsJSTypeAliasDeclaration(node)) || Ast.IsModuleDeclaration(node)) || Ast.IsClassDeclaration(node)) || Ast.IsInterfaceDeclaration(node)) || Ast.IsFunctionLike(node)) || Ast.IsIndexSignatureDeclaration(node)) || Ast.IsMappedTypeNode(node)) || Ast.IsVariableDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:122:1.
    internal static bool IsAlwaysType(Node node)
    {
        if ((node.Kind == Kind.InterfaceDeclaration))
        {
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:129:1.
    internal static ModifierFlags MaskModifierFlags(Node node, ModifierFlags modifierMask, ModifierFlags modifierAdditions)
    {
        var flags = (((Ast.GetCombinedModifierFlags(node) & modifierMask)) | modifierAdditions);
        if ((((flags & ModifierFlags.Default) != 0) && (((flags & ModifierFlags.Export) == 0))))
        {
            flags ^= ModifierFlags.Export;
        }

        if ((((flags & ModifierFlags.Default) != 0) && ((flags & ModifierFlags.Ambient) != 0)))
        {
            flags ^= ModifierFlags.Ambient;
        }

        return flags;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:142:1.
    internal static Node UnwrapParenthesizedExpression(Node o)
    {
        while ((o.Kind == Kind.ParenthesizedExpression))
        {
            o = o.Expression();
        }

        return o;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:149:1.
    internal static bool IsPrivateMethodTypeParameter(IEmitResolver resolver, TypeParameterDeclaration node)
    {
        return ((node.AsNode().Parent.Kind == Kind.MethodDeclaration) && (resolver.GetEffectiveDeclarationFlags(node.AsNode().Parent, ModifierFlags.Private) != 0));
    }

    // Returns true if expando properties should be emitted for this function.
    // Properties are emitted if any overload in the symbol has a body (implementation).
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:155:1.
    internal static bool ShouldEmitFunctionProperties(FunctionDeclaration input)
    {
        if ((input.Body != null))
        {
            return true;
        }

        return !Core.Every(input.Symbol.Declarations, static (decl) =>
        {
            return (!Ast.IsFunctionDeclaration(decl) || (decl.AsFunctionDeclaration().Body == null));
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:164:1.
    internal static Node GetEffectiveBaseTypeNode(Node node)
    {
        // Pinned util.go:166–173 retains the unimplemented JSDoc @augments fallback.
        var baseType = Ast.GetClassExtendsHeritageElement(node);
        return baseType;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:177:1.
    internal static bool IsScopeMarker(Node node)
    {
        return (Ast.IsExportAssignment(node) || Ast.IsExportDeclaration(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/util.go:181:1.
    internal static bool HasScopeMarker(NodeList statements)
    {
        if ((statements == null))
        {
            return false;
        }

        return Core.Some(statements.Nodes, IsScopeMarker);
    }
}
