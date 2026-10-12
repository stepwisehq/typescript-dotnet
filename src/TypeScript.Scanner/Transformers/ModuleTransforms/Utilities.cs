// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;

namespace TypeScript.Transformers.ModuleTransforms;
internal static partial class ModuleTransforms
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:12:1.
    internal static bool IsDeclarationNameOfEnumOrNamespace(EmitContext emitContext, Node node)
    {
        {
            var original = emitContext.MostOriginal(node);
            if (((original != null) && (original.Parent != null)))
            {
                switch (original.Parent.Kind)
                {
                    case Kind.EnumDeclaration:
                    case Kind.ModuleDeclaration:
                    {
                        return (original == original.Parent.Name());
                    }
                }
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:22:1.
    internal static Node RewriteModuleSpecifier(EmitContext emitContext, Node node, CompilerOptions compilerOptions)
    {
        if ((((node == null) || !Ast.IsStringLiteral(node)) || !Core.ShouldRewriteModuleSpecifier(node.TextValue(), compilerOptions)))
        {
            return node;
        }

        var updatedText = TypeScript.Scanner.TsPath.ChangeExtension(node.TextValue(), TypeScript.OutputPaths.OutputPaths.GetOutputExtension(node.TextValue(), compilerOptions.Jsx));
        if ((!updatedText.EqualsValue(node.TextValue())))
        {
            var updated = emitContext.Factory.NewStringLiteralWithTextValue(updatedText, node.AsStringLiteral().TokenFlags);
            emitContext.SetOriginal(updated, node);
            emitContext.AssignCommentAndSourceMapRanges(updated, node);
            return updated;
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:36:1.
    internal static Node CreateEmptyImports(NodeFactory factory)
    {
        return factory.NewExportDeclaration(null, false, factory.NewNamedExports(factory.NewNodeList(default(Slice<Node>))), null, null);
    }

    // Get the name of a target module from an import/export declaration as should be written in the emitted output.
    // The emitted output name can be different from the input if:
    //  1. The module has a /// <amd-module name="<new name>" />
    //  2. --out or --outFile is used, making the name relative to the rootDir
    //     3- The containing SourceFile has an entry in renamedDependencies for the import as requested by some module loaders (e.g. System).
    //
    // Otherwise, a new StringLiteral node representing the module name will be returned.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:53:1.
    internal static Node GetExternalModuleNameLiteral(NodeFactory factory, Node importNode, SourceFile sourceFile, object host, IEmitResolver resolver, CompilerOptions compilerOptions)
    {
        var moduleName = Ast.GetExternalModuleName(importNode);
        if (((moduleName != null) && Ast.IsStringLiteral(moduleName)))
        {
            var name = TryGetModuleNameFromDeclaration(importNode, host, factory, resolver, compilerOptions);
            if ((name == null))
            {
                name = TryRenameExternalModule(factory, moduleName, sourceFile);
            }

            if ((name == null))
            {
                name = factory.NewStringLiteralWithTextValue(moduleName.TextValue(), TokenFlags.None);
            }

            return name;
        }

        return null;
    }

    // Get the name of a module as should be written in the emitted output.
    // The emitted output name can be different from the input if:
    //  1. The module has a /// <amd-module name="<new name>" />
    //  2. --out or --outFile is used, making the name relative to the rootDir
    //
    // Otherwise, a new StringLiteral node representing the module name will be returned.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:74:1.
    internal static Node TryGetModuleNameFromFile(NodeFactory factory, SourceFile file, object host, CompilerOptions options)
    {
        if ((file == null))
        {
            return null;
        }

        // Pinned upstream !!! leaf intentionally returns nil; moduleName support is not implemented upstream.
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:85:1.
    internal static Node TryGetModuleNameFromDeclaration(Node declaration, object host, NodeFactory factory, IEmitResolver resolver, CompilerOptions compilerOptions)
    {
        if ((resolver == null))
        {
            return null;
        }

        return TryGetModuleNameFromFile(factory, resolver.GetExternalModuleFileFromDeclaration(declaration), host, compilerOptions);
    }

    // Resolves a local path to a path which is absolute to the base of the emit
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:93:1.
    internal static string GetExternalModuleNameFromPath(object host, string fileName, string referencePath)
    {
        // Pinned upstream !!! leaf intentionally returns empty.
        return "";
    }

    // Some bundlers (SystemJS builder) sometimes want to rename dependencies.
    // Here we check if alternative name was provided for a given moduleName and return it if possible.
    // Pinned upstream !!! dependency-renaming leaf intentionally returns nil.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:100:1.
    internal static Node TryRenameExternalModule(NodeFactory factory, Node moduleName, SourceFile sourceFile)
    {
        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:105:1.
    internal static bool IsFileLevelReservedGeneratedIdentifier(EmitContext emitContext, Node name)
    {
        var info = emitContext.GetAutoGenerateInfo(name);
        return ((((info != null) && info.Flags.IsFileLevel()) && info.Flags.IsOptimistic()) && info.Flags.IsReservedInNestedScopes());
    }

    // A simple inlinable expression is an expression which can be copied into multiple locations
    // without risk of repeating any sideeffects and whose value could not possibly change between
    // any such locations
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go:116:1.
    internal static bool IsSimpleInlineableExpression(Node expression)
    {
        return (!Ast.IsIdentifier(expression) && Transformers.IsSimpleCopiableExpression(expression));
    }
}
