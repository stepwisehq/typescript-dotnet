// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;

namespace TypeScript.Transformers.ModuleTransforms;
internal sealed class ESModuleTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    // Pinned embedded owner is assigned but never read by upstream.
#pragma warning disable CS0414
    private IReferenceResolver resolver;
#pragma warning restore CS0414
    private Func<HasFileName, ModuleKind> getEmitModuleFormatOfFile;
    private SourceFile currentSourceFile;
    private ImportRequireStatements importRequireStatements;
    // Pinned field is retained although currently unused by upstream algorithms.
#pragma warning disable CS0169
    private Dictionary<ByteStringKey, Node> helperNameSubstitutions;
#pragma warning restore CS0169
    private sealed class ImportRequireStatements
    {
        internal Slice<Node> statements;
        internal Node requireHelperName;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:28:1.
    public static Transformer NewESModuleTransformer(TransformOptions opts)
    {
        var compilerOptions = opts.CompilerOptions;
        var tx = new ESModuleTransformer
        {
            compilerOptions = compilerOptions,
            resolver = opts.Resolver,
            getEmitModuleFormatOfFile = opts.GetEmitModuleFormatOfFile
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }

    // Visits source elements that are not top-level or top-level nested statements.
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:35:1.
    private Node visit(Node node)
    {
        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                node = this.visitSourceFile(node.AsSourceFile());
                break;
            }

            case Kind.ImportDeclaration:
            {
                node = this.visitImportDeclaration(node.AsImportDeclaration());
                break;
            }

            case Kind.ImportEqualsDeclaration:
            {
                node = this.visitImportEqualsDeclaration(node.AsImportEqualsDeclaration());
                break;
            }

            case Kind.ExportAssignment:
            {
                node = this.visitExportAssignment(node.AsExportAssignment());
                break;
            }

            case Kind.ExportDeclaration:
            {
                node = this.visitExportDeclaration(node.AsExportDeclaration());
                break;
            }

            case Kind.CallExpression:
            {
                node = this.visitCallExpression(node.AsCallExpression());
                break;
            }

            default:
            {
                node = this.Visitor().VisitEachChild(node);
                break;
            }
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:55:1.
    private Node visitSourceFile(SourceFile node)
    {
        if ((node.IsDeclarationFile || !((Ast.IsExternalModule(node) || this.compilerOptions.GetIsolatedModules()))))
        {
            return node.AsNode();
        }

        this.currentSourceFile = node;
        this.importRequireStatements = null;
        var result = this.Visitor().VisitEachChild(node.AsNode()).AsSourceFile();
        this.EmitContext().AddEmitHelper(result.AsNode(), this.EmitContext().ReadEmitHelpers());
        var externalHelpersImportDeclaration = ModuleTransforms.CreateExternalHelpersImportDeclarationIfNeeded(this.EmitContext(), result, this.compilerOptions, this.getEmitModuleFormatOfFile(node), false, false, false);
        if (((externalHelpersImportDeclaration != null) || (this.importRequireStatements != null)))
        {
            var tuple1 = this.Factory().SplitStandardPrologue(result.Statements.Nodes);
            var prologue = tuple1.Item1;
            var rest = tuple1.Item2;
            var tuple2 = this.Factory().SplitCustomPrologue(rest);
            var custom = tuple2.Item1;
            rest = tuple2.Item2;
            var statements = GoSlices.Clone(prologue);
            statements = statements.Append(custom);
            if ((externalHelpersImportDeclaration != null))
            {
                statements = statements.Append(this.Visitor().VisitNode(externalHelpersImportDeclaration));
            }

            if ((this.importRequireStatements != null))
            {
                statements = statements.Append(this.importRequireStatements.statements);
            }

            statements = statements.Append(rest);
            var statementList = this.Factory().NewNodeList(statements);
            statementList.Loc = result.Statements.Loc;
            result = this.Factory().UpdateSourceFile(result, statementList, node.EndOfFileToken).AsSourceFile();
        }

        if (((Ast.IsExternalModule(result) && (this.compilerOptions.GetEmitModuleKind() != ModuleKind.Preserve)) && !Core.Some(result.Statements.Nodes, Ast.IsExternalModuleIndicator)))
        {
            var statements2 = GoSlices.Clone(result.Statements.Nodes);
            statements2 = statements2.Append(ModuleTransforms.CreateEmptyImports(this.Factory()));
            var statementList2 = this.Factory().NewNodeList(statements2);
            statementList2.Loc = result.Statements.Loc;
            result = this.Factory().UpdateSourceFile(result, statementList2, node.EndOfFileToken).AsSourceFile();
        }

        this.importRequireStatements = null;
        this.currentSourceFile = null;
        return result.AsNode();
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:103:1.
    private Node visitImportDeclaration(ImportDeclaration node)
    {
        if (!(this.compilerOptions.RewriteRelativeImportExtensions == Tristate.True))
        {
            return node.AsNode();
        }

        var updatedModuleSpecifier = ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), node.ModuleSpecifier, this.compilerOptions);
        return this.Factory().UpdateImportDeclaration(node, null, this.Visitor().VisitNode(node.ImportClause), updatedModuleSpecifier, this.Visitor().VisitNode(node.Attributes));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:117:1.
    private Node visitImportEqualsDeclaration(ImportEqualsDeclaration node)
    {
        if ((this.compilerOptions.GetEmitModuleKind() < ModuleKind.Node16))
        {
            return null;
        }

        if (!Ast.IsExternalModuleImportEqualsDeclaration(node.AsNode()))
        {
            throw new InvalidOperationException("import= for internal module references should be handled in an earlier transformer.");
        }

        var varStatement = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(node.Name().Clone(this.Factory()), null, null, this.createRequireCall(node.AsNode()))])), NodeFlags.Const));
        this.EmitContext().SetOriginal(varStatement, node.AsNode());
        this.EmitContext().AssignCommentAndSourceMapRanges(varStatement, node.AsNode());
        Slice<Node> statements = default;
        statements = statements.Append(varStatement);
        statements = this.appendExportsOfImportEqualsDeclaration(statements, node);
        return Transformers.SingleOrMany(statements, this.Factory());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:152:1.
    private Slice<Node> appendExportsOfImportEqualsDeclaration(Slice<Node> statements, ImportEqualsDeclaration node)
    {
        if (Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export))
        {
            statements = statements.Append(this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, null, node.Name().Clone(this.Factory()))]))), null, null));
        }

        return statements;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:173:1.
    private Node visitExportAssignment(ExportAssignment node)
    {
        if (!node.IsExportEquals)
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        if ((this.compilerOptions.GetEmitModuleKind() != ModuleKind.Preserve))
        {
            return null;
        }

        var statement = this.Factory().NewExpressionStatement(this.Factory().NewAssignmentExpression(this.Factory().NewPropertyAccessExpression(this.Factory().NewIdentifier("module"), null, this.Factory().NewIdentifier("exports"), NodeFlags.None), this.Visitor().VisitNode(node.Expression)));
        this.EmitContext().SetOriginal(statement, node.AsNode());
        return statement;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:196:1.
    private Node visitExportDeclaration(ExportDeclaration node)
    {
        if ((node.ModuleSpecifier == null))
        {
            return node.AsNode();
        }

        var updatedModuleSpecifier = ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), node.ModuleSpecifier, this.compilerOptions);
        if ((((this.compilerOptions.Module > ModuleKind.ES2015) || (node.ExportClause == null)) || !Ast.IsNamespaceExport(node.ExportClause)))
        {
            return this.Factory().UpdateExportDeclaration(node, null, false, node.ExportClause, updatedModuleSpecifier, this.Visitor().VisitNode(node.Attributes));
        }

        var oldIdentifier = node.ExportClause.Name();
        var synthName = this.Factory().NewGeneratedNameForNode(oldIdentifier);
        var importDecl = this.Factory().NewImportDeclaration(null, this.Factory().NewImportClause(Kind.Unknown, null, this.Factory().NewNamespaceImport(synthName)), updatedModuleSpecifier, this.Visitor().VisitNode(node.Attributes));
        this.EmitContext().SetOriginal(importDecl, node.ExportClause);
        Node exportDecl = default;
        if (Ast.IsExportNamespaceAsDefaultDeclaration(node.AsNode()))
        {
            exportDecl = this.Factory().NewExportAssignment(null, false, null, synthName);
        }
        else
        {
            exportDecl = this.Factory().NewExportDeclaration(null, false, this.Factory().NewNamedExports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewExportSpecifier(false, synthName, oldIdentifier)]))), null, null);
        }

        this.EmitContext().SetOriginal(exportDecl, node.AsNode());
        return Transformers.SingleOrMany(Slice<Node>.FromArray([importDecl, exportDecl]), this.Factory());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:248:1.
    private Node visitCallExpression(CallExpression node)
    {
        if ((this.compilerOptions.RewriteRelativeImportExtensions == Tristate.True))
        {
            if (((Ast.IsImportCall(node.AsNode()) && (GoRuntime.Len(node.Arguments.Nodes) > 0)) || (Ast.IsInJSFile(node.AsNode()) && Ast.IsRequireCall(node.AsNode(), false))))
            {
                return this.visitImportOrRequireCall(node);
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:258:1.
    private Node visitImportOrRequireCall(CallExpression node)
    {
        if ((GoRuntime.Len(node.Arguments.Nodes) == 0))
        {
            return this.Visitor().VisitEachChild(node.AsNode());
        }

        var expression = this.Visitor().VisitNode(node.Expression);
        Node argument = default;
        if (Ast.IsStringLiteralLike(node.Arguments.Nodes[0]))
        {
            argument = ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), node.Arguments.Nodes[0], this.compilerOptions);
        }
        else
        {
            argument = this.Factory().NewRewriteRelativeImportExtensionsHelper(node.Arguments.Nodes[0], (this.compilerOptions.Jsx == JsxEmit.Preserve));
        }

        Slice<Node> arguments = default;
        arguments = arguments.Append(argument);
        var rest = this.Visitor().VisitSlice(node.Arguments.Nodes.Slice2(1, node.Arguments.Nodes.Length)).Item1;
        arguments = arguments.Append(rest);
        var argumentList = this.Factory().NewNodeList(arguments);
        argumentList.Loc = node.Arguments.Loc;
        return this.Factory().UpdateCallExpression(node, expression, node.QuestionDotToken, null, argumentList, node.Flags);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go:290:1.
    private Node createRequireCall(Node node)
    {
        var moduleName = ModuleTransforms.GetExternalModuleNameLiteral(this.Factory(), node, this.currentSourceFile, null, null, this.compilerOptions);
        Slice<Node> args = default;
        if ((moduleName != null))
        {
            args = args.Append(ModuleTransforms.RewriteModuleSpecifier(this.EmitContext(), moduleName, this.compilerOptions));
        }

        if ((this.compilerOptions.GetEmitModuleKind() == ModuleKind.Preserve))
        {
            return this.Factory().NewCallExpression(this.Factory().NewIdentifier("require"), null, null, this.Factory().NewNodeList(args), NodeFlags.None);
        }

        if ((this.importRequireStatements == null))
        {
            var createRequireName = this.Factory().NewUniqueNameEx("_createRequire", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
            var importStatement = this.Factory().NewImportDeclaration(null, this.Factory().NewImportClause(Kind.Unknown, null, this.Factory().NewNamedImports(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewImportSpecifier(false, this.Factory().NewIdentifier("createRequire"), createRequireName)])))), this.Factory().NewStringLiteral("module", TokenFlags.None), null);
            this.EmitContext().AddEmitFlags(importStatement, EmitFlags.CustomPrologue);
            var requireHelperName = this.Factory().NewUniqueNameEx("__require", new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel) });
            var requireStatement = this.Factory().NewVariableStatement(null, this.Factory().NewVariableDeclarationList(this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewVariableDeclaration(requireHelperName, null, null, this.Factory().NewCallExpression(createRequireName.Clone(this.Factory()), null, null, this.Factory().NewNodeList(Slice<Node>.FromArray([this.Factory().NewPropertyAccessExpression(this.Factory().NewMetaProperty(Kind.ImportKeyword, this.Factory().NewIdentifier("meta")), null, this.Factory().NewIdentifier("url"), NodeFlags.None)])), NodeFlags.None))])), NodeFlags.Const));
            this.EmitContext().AddEmitFlags(requireStatement, EmitFlags.CustomPrologue);
            this.importRequireStatements = new ImportRequireStatements
            {
                statements = Slice<Node>.FromArray([importStatement, requireStatement]),
                requireHelperName = requireHelperName
            };
        }

        return this.Factory().NewCallExpression(this.importRequireStatements.requireHelperName.Clone(this.Factory()), null, null, this.Factory().NewNodeList(args), NodeFlags.None);
    }
}
