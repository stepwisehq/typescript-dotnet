// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of tstransforms/importelision.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class ImportElisionTransformer : Transformer
{
private CompilerOptions compilerOptions;
private SourceFile currentSourceFile;
private IEmitResolver emitResolver;
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:17:1.
public static Transformer NewImportElisionTransformer(TransformOptions opt)
{
    var compilerOptions = opt.CompilerOptions;
    var emitContext = opt.Context;
    if ((compilerOptions.VerbatimModuleSyntax == Tristate.True))
    {
        throw new InvalidOperationException("ImportElisionTransformer should not be used with VerbatimModuleSyntax");
    }
    var tx = new ImportElisionTransformer { compilerOptions = compilerOptions, emitResolver = opt.EmitResolver };
    return tx.NewTransformer(tx.visit, emitContext);
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:27:1.
private Node visit(Node node)
{
    if ((Ast.IsSourceFile(node) && (this.emitResolver != null)))
    {
        this.emitResolver.MarkLinkedReferencesRecursively(this.EmitContext().MostOriginal(node).AsSourceFile());
    }
    switch (node.Kind)
    {
        case Kind.ImportEqualsDeclaration:
        {
            if (Ast.IsExternalModuleImportEqualsDeclaration(node))
            {
                if (!this.shouldEmitAliasDeclaration(node))
                {
                    return null;
                }
            }
            else
            {
                if (!this.shouldEmitImportEqualsDeclaration(node.AsImportEqualsDeclaration()))
                {
                    return null;
                }
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.ImportDeclaration:
        {
            var n = node.AsImportDeclaration();
            if ((n.ImportClause != null))
            {
                var importClause = this.Visitor().VisitNode(n.ImportClause);
                if ((importClause == null))
                {
                    return null;
                }
                return this.Factory().UpdateImportDeclaration(n, n.Modifiers(), importClause, n.ModuleSpecifier, this.Visitor().VisitNode(n.Attributes));
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.ImportClause:
        {
            var n2 = node.AsImportClause();
            var name = this.shouldEmitAliasDeclaration(node) ? n2.Name() : null;
            var namedBindings = this.Visitor().VisitNode(n2.NamedBindings);
            if (((name == null) && (namedBindings == null)))
            {
                return null;
            }
            return this.Factory().UpdateImportClause(n2, n2.PhaseModifier, name, namedBindings);
        }
        case Kind.NamespaceImport:
        {
            if (!this.shouldEmitAliasDeclaration(node))
            {
                return null;
            }
            return node;
        }
        case Kind.NamedImports:
        {
            var n3 = node.AsNamedImports();
            var elements = this.Visitor().VisitNodes(n3.Elements);
            if ((GoRuntime.Len(elements.Nodes) == 0))
            {
                return null;
            }
            return this.Factory().UpdateNamedImports(n3, elements);
        }
        case Kind.ImportSpecifier:
        {
            if (!this.shouldEmitAliasDeclaration(node))
            {
                return null;
            }
            return node;
        }
        case Kind.ExportAssignment:
        {
            if ((!(this.compilerOptions.VerbatimModuleSyntax == Tristate.True) && !this.isValueAliasDeclaration(node)))
            {
                return null;
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.ExportDeclaration:
        {
            var n4 = node.AsExportDeclaration();
            Node exportClause = default;
            if ((n4.ExportClause != null))
            {
                exportClause = this.Visitor().VisitNode(n4.ExportClause);
                if ((exportClause == null))
                {
                    return null;
                }
            }
            return this.Factory().UpdateExportDeclaration(n4, null, false, exportClause, this.Visitor().VisitNode(n4.ModuleSpecifier), this.Visitor().VisitNode(n4.Attributes));
        }
        case Kind.NamedExports:
        {
            var n5 = node.AsNamedExports();
            var elements2 = this.Visitor().VisitNodes(n5.Elements);
            if ((GoRuntime.Len(elements2.Nodes) == 0))
            {
                return null;
            }
            return this.Factory().UpdateNamedExports(n5, elements2);
        }
        case Kind.ExportSpecifier:
        {
            if (!this.isValueAliasDeclaration(node))
            {
                return null;
            }
            return node;
        }
        case Kind.SourceFile:
        {
            var savedCurrentSourceFile = this.currentSourceFile;
            this.currentSourceFile = node.AsSourceFile();
            node = this.Visitor().VisitEachChild(node);
            this.currentSourceFile = savedCurrentSourceFile;
            return node;
        }
        case Kind.ModuleDeclaration:
        case Kind.ModuleBlock:
        {
            return this.Visitor().VisitEachChild(node);
        }
        default:
        {
            return node;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:129:1.
private bool shouldEmitAliasDeclaration(Node node)
{
    return (Ast.IsInJSFile(node) || this.isReferencedAliasDeclaration(node));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:133:1.
private bool shouldEmitImportEqualsDeclaration(ImportEqualsDeclaration node)
{
    return (this.shouldEmitAliasDeclaration(node.AsNode()) || ((!Ast.IsExternalModule(this.currentSourceFile) && this.isTopLevelValueImportEqualsWithEntityName(node.AsNode()))));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:140:1.
private bool isReferencedAliasDeclaration(Node node)
{
    node = this.EmitContext().ParseNode(node);
    return ((node == null) || this.emitResolver.IsReferencedAliasDeclaration(node));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:145:1.
private bool isValueAliasDeclaration(Node node)
{
    node = this.EmitContext().ParseNode(node);
    return ((node == null) || this.emitResolver.IsValueAliasDeclaration(node));
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go:150:1.
private bool isTopLevelValueImportEqualsWithEntityName(Node node)
{
    node = this.EmitContext().ParseNode(node);
    return ((node != null) && this.emitResolver.IsTopLevelValueImportEqualsWithEntityName(node));
}

}
