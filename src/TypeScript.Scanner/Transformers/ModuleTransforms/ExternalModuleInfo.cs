// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Collections;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;

namespace TypeScript.Transformers.ModuleTransforms;
internal sealed class ExternalModuleInfo
{
    internal Slice<Node> externalImports;
    internal readonly MultiMap<ByteStringKey, Node> exportSpecifiers = new();
    internal readonly MultiMap<Node, Node> exportedBindings = new();
    internal Slice<Node> exportedNames;
    internal readonly OrderedSet<Node> exportedFunctions = new();
    internal ExportAssignment exportEquals;
    internal bool hasExportStarsToExportValues;
}

internal sealed class ExternalModuleInfoCollector
{
    internal SourceFile sourceFile;
    internal CompilerOptions compilerOptions;
    internal EmitContext emitContext;
    internal IReferenceResolver resolver;
    private readonly HashSet<ByteStringKey> uniqueExports = new();
    private bool hasExportDefault;
    internal ExternalModuleInfo output;
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:46:1.
    internal ExternalModuleInfo collect()
    {
        var hasImportStar = false;
        var hasImportDefault = false;
        {
            foreach (var node in GoRuntime.RangeValues(this.sourceFile.Statements.Nodes))
            {
                if (Ast.IsNotEmittedStatement(node))
                {
                    var original = this.emitContext.MostOriginal(node);
                    if (((original != null) && Ast.IsExportAssignment(original)))
                    {
                        var n = original.AsExportAssignment();
                        if ((n.IsExportEquals && (this.output.exportEquals == null)))
                        {
                            this.output.exportEquals = n;
                        }
                    }

                    continue;
                }

                switch (node.Kind)
                {
                    case Kind.ImportDeclaration:
                    {
                        var n2 = node.AsImportDeclaration();
                        this.addExternalImport(node);
                        if ((!hasImportStar && ModuleTransforms.GetImportNeedsImportStarHelper(n2)))
                        {
                            hasImportStar = true;
                        }

                        if ((!hasImportDefault && ModuleTransforms.GetImportNeedsImportDefaultHelper(n2)))
                        {
                            hasImportDefault = true;
                        }

                        break;
                    }

                    case Kind.ImportEqualsDeclaration:
                    {
                        var n3 = node.AsImportEqualsDeclaration();
                        if (Ast.IsExternalModuleReference(n3.ModuleReference))
                        {
                            this.addExternalImport(node);
                        }

                        break;
                    }

                    case Kind.ExportDeclaration:
                    {
                        var n4 = node.AsExportDeclaration();
                        if ((n4.ModuleSpecifier != null))
                        {
                            this.addExternalImport(node);
                            if ((n4.ExportClause == null))
                            {
                                this.output.hasExportStarsToExportValues = true;
                            }
                            else
                            {
                                if (Ast.IsNamedExports(n4.ExportClause))
                                {
                                    this.addExportedNamesForExportDeclaration(n4);
                                    if (!hasImportDefault)
                                    {
                                        hasImportDefault = ModuleTransforms.ContainsDefaultReference(n4.ExportClause);
                                    }
                                }
                                else
                                {
                                    var name = n4.ExportClause.AsNamespaceExport().Name();
                                    var nameText = name.TextValue();
                                    if (this.addUniqueExport(nameText))
                                    {
                                        this.addExportedBinding(node, name);
                                        this.addExportedName(name);
                                    }

                                    hasImportStar = true;
                                }
                            }
                        }
                        else
                        {
                            this.addExportedNamesForExportDeclaration(node.AsExportDeclaration());
                        }

                        break;
                    }

                    case Kind.ExportAssignment:
                    {
                        var n5 = node.AsExportAssignment();
                        if ((n5.IsExportEquals && (this.output.exportEquals == null)))
                        {
                            this.output.exportEquals = n5;
                        }

                        break;
                    }

                    case Kind.VariableStatement:
                    {
                        var n6 = node.AsVariableStatement();
                        if (Ast.HasSyntacticModifier(node, ModifierFlags.Export))
                        {
                            {
                                foreach (var decl in n6.DeclarationList.AsVariableDeclarationList().Declarations.Nodes)
                                {
                                    this.collectExportedVariableInfo(decl);
                                }
                            }
                        }

                        break;
                    }

                    case Kind.FunctionDeclaration:
                    {
                        var n7 = node.AsFunctionDeclaration();
                        if (Ast.HasSyntacticModifier(node, ModifierFlags.Export))
                        {
                            this.addExportedFunctionDeclaration(n7, null, Ast.HasSyntacticModifier(node, ModifierFlags.Default));
                        }

                        break;
                    }

                    case Kind.ClassDeclaration:
                    {
                        var n8 = node.AsClassDeclaration();
                        if (Ast.HasSyntacticModifier(node, ModifierFlags.Export))
                        {
                            if (Ast.HasSyntacticModifier(node, ModifierFlags.Default))
                            {
                                if (!this.hasExportDefault)
                                {
                                    var name2 = n8.Name();
                                    if ((name2 == null))
                                    {
                                        name2 = this.emitContext.Factory.NewGeneratedNameForNode(node);
                                    }

                                    this.addExportedBinding(node, name2);
                                    this.hasExportDefault = true;
                                }
                            }
                            else
                            {
                                var name3 = n8.Name();
                                if ((name3 != null))
                                {
                                    if (this.addUniqueExport(name3.TextValue()))
                                    {
                                        this.addExportedBinding(node, name3);
                                        this.addExportedName(name3);
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        return this.output;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:167:1.
    private bool addUniqueExport(ByteStringValue name)
    {
        if (!this.uniqueExports.Contains(ByteStringKey.FromValue(name)))
        {
            this.uniqueExports.Add(ByteStringKey.FromValue(name));
            return true;
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:175:1.
    private void addExportedBinding(Node decl, Node name)
    {
        this.output.exportedBindings.Add(this.emitContext.MostOriginal(decl), name);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:179:1.
    private void addExternalImport(Node node)
    {
        this.output.externalImports = this.output.externalImports.Append(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:183:1.
    private void addExportedName(Node name)
    {
        this.output.exportedNames = this.output.exportedNames.Append(name);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:187:1.
    private void addExportedNamesForExportDeclaration(ExportDeclaration node)
    {
        {
            foreach (var specifier in GoRuntime.RangeValues(node.ExportClause.Elements()))
            {
                var specifierNameText = specifier.Name().TextValue();
                if (this.addUniqueExport(specifierNameText))
                {
                    var name = specifier.PropertyNameOrName();
                    if ((name.Kind != Kind.StringLiteral))
                    {
                        if ((node.ModuleSpecifier == null))
                        {
                            this.output.exportSpecifiers.Add(ByteStringKey.FromValue(name.TextValue()), specifier.AsExportSpecifier());
                        }

                        var decl = this.resolver.GetReferencedImportDeclaration(this.emitContext.MostOriginal(name));
                        if ((decl == null))
                        {
                            decl = this.resolver.GetReferencedValueDeclaration(this.emitContext.MostOriginal(name));
                        }

                        if ((decl != null))
                        {
                            if ((decl.Kind == Kind.FunctionDeclaration))
                            {
                                this.uniqueExports.Remove(ByteStringKey.FromValue(specifierNameText));
                                this.addExportedFunctionDeclaration(decl.AsFunctionDeclaration(), specifier.Name(), Ast.ModuleExportNameIsDefault(specifier.Name()));
                                continue;
                            }

                            this.addExportedBinding(decl, specifier.Name());
                        }
                    }

                    this.addExportedName(specifier.Name());
                }
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:216:1.
    private void addExportedFunctionDeclaration(FunctionDeclaration node, Node name, bool isDefault)
    {
        this.output.exportedFunctions.Add(this.emitContext.MostOriginal(node.AsNode()));
        if (isDefault)
        {
            if (!this.hasExportDefault)
            {
                if ((name == null))
                {
                    name = this.emitContext.Factory.NewGeneratedNameForNode(node.AsNode());
                }

                this.addExportedBinding(node.AsNode(), name);
                this.hasExportDefault = true;
            }
        }
        else
        {
            if ((name == null))
            {
                name = node.Name();
            }

            var nameText = name.TextValue();
            if (this.addUniqueExport(nameText))
            {
                this.addExportedBinding(node.AsNode(), name);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:241:1.
    private void collectExportedVariableInfo(Node decl)
    {
        if (Ast.IsBindingPattern(decl.Name()))
        {
            {
                foreach (var element in GoRuntime.RangeValues(decl.Name().Elements()))
                {
                    var e = element.AsBindingElement();
                    if ((e.Name() != null))
                    {
                        this.collectExportedVariableInfo(element);
                    }
                }
            }
        }
        else
        {
            if (!this.emitContext.HasAutoGenerateInfo(decl.Name()))
            {
                var text = decl.Name().TextValue();
                if (this.addUniqueExport(text))
                {
                    this.addExportedName(decl.Name());
                    if (Transformers.IsLocalName(this.emitContext, decl.Name()))
                    {
                        this.addExportedBinding(decl, decl.Name());
                    }
                }
            }
        }
    }
}

internal static partial class ModuleTransforms
{
    private const string externalHelpersModuleNameText = "tslib";
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:35:1.
    internal static ExternalModuleInfo CollectExternalModuleInfo(SourceFile sourceFile, CompilerOptions compilerOptions, EmitContext emitContext, IReferenceResolver resolver)
    {
        var c = new ExternalModuleInfoCollector
        {
            sourceFile = sourceFile,
            compilerOptions = compilerOptions,
            emitContext = emitContext,
            resolver = resolver,
            output = new ExternalModuleInfo
            {
            }
        };
        return c.collect();
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:262:1.
    internal static Node CreateExternalHelpersImportDeclarationIfNeeded(EmitContext emitContext, SourceFile sourceFile, CompilerOptions compilerOptions, ModuleKind fileModuleKind, bool hasExportStarsToExportValues, bool hasImportStar, bool hasImportDefault)
    {
        if (((compilerOptions.ImportHelpers == Tristate.True) && Ast.IsEffectiveExternalModule(sourceFile, compilerOptions)))
        {
            var moduleKind = compilerOptions.GetEmitModuleKind();
            var helpers = GetImportedHelpers(emitContext, sourceFile);
            if (((fileModuleKind == ModuleKind.CommonJS) || ((fileModuleKind == ModuleKind.None) && (moduleKind == ModuleKind.CommonJS))))
            {
                var externalHelpersModuleName = GetOrCreateExternalHelpersModuleNameIfNeeded(emitContext, sourceFile, compilerOptions, helpers, hasExportStarsToExportValues, (hasImportStar || hasImportDefault), fileModuleKind);
                if ((externalHelpersModuleName != null))
                {
                    var externalHelpersImportDeclaration = emitContext.Factory.NewImportEqualsDeclaration(null, false, externalHelpersModuleName, emitContext.Factory.NewExternalModuleReference(emitContext.Factory.NewStringLiteral(externalHelpersModuleNameText, TokenFlags.None)));
                    emitContext.AddEmitFlags(externalHelpersImportDeclaration, EmitFlags.CustomPrologue);
                    return externalHelpersImportDeclaration;
                }
            }
            else
            {
                Slice<string> helperNames = default;
                {
                    foreach (var helper in GoRuntime.RangeValues(helpers))
                    {
                        var importName = helper.ImportName;
                        if ((GoRuntime.Len(importName) > 0))
                        {
                            helperNames = Core.AppendIfUnique(helperNames, importName);
                        }
                    }
                }

                if ((GoRuntime.Len(helperNames) > 0))
                {
                    GoSlices.SortFunc(helperNames, string.CompareOrdinal);
                    // core.Map preserves one exact-length destination owner; avoid a captured callback.
                    var importSpecifiers = Slice<Node>.Make(helperNames.Length);
                    for (int i = 0; i < helperNames.Length; i++)
                    {
                        string name = helperNames[i];
                        importSpecifiers[i] = emitContext.IsFileLevelUniqueName(sourceFile, name, null) ? emitContext.Factory.NewImportSpecifier(false, null, emitContext.Factory.NewIdentifier(name)) : emitContext.Factory.NewImportSpecifier(false, emitContext.Factory.NewIdentifier(name), emitContext.Factory.NewUnscopedHelperName(name));
                    }

                    var namedBindings = emitContext.Factory.NewNamedImports(emitContext.Factory.NewNodeList(importSpecifiers));
                    var parseNode = emitContext.MostOriginal(sourceFile.AsNode());
                    emitContext.AddEmitFlags(parseNode, EmitFlags.ExternalHelpers);
                    var externalHelpersImportDeclaration2 = emitContext.Factory.NewImportDeclaration(null, emitContext.Factory.NewImportClause(Kind.Unknown, null, namedBindings), emitContext.Factory.NewStringLiteral(externalHelpersModuleNameText, TokenFlags.None), null);
                    emitContext.AddEmitFlags(externalHelpersImportDeclaration2, EmitFlags.CustomPrologue);
                    return externalHelpersImportDeclaration2;
                }
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:320:1.
    internal static Slice<EmitHelper> GetImportedHelpers(EmitContext emitContext, SourceFile sourceFile)
    {
        Slice<EmitHelper> helpers = default;
        {
            foreach (var helper in GoRuntime.RangeValues(emitContext.GetEmitHelpers(sourceFile.AsNode())))
            {
                if (!helper.Scoped)
                {
                    helpers = helpers.Append(helper);
                }
            }
        }

        return helpers;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:330:1.
    internal static Node GetOrCreateExternalHelpersModuleNameIfNeeded(EmitContext emitContext, SourceFile node, CompilerOptions compilerOptions, Slice<EmitHelper> helpers, bool hasExportStarsToExportValues, bool hasImportStarOrImportDefault, ModuleKind fileModuleKind)
    {
        var externalHelpersModuleName = emitContext.GetExternalHelpersModuleName(node);
        if ((externalHelpersModuleName != null))
        {
            return externalHelpersModuleName;
        }

        var create = ((GoRuntime.Len(helpers) > 0) || (((hasExportStarsToExportValues || hasImportStarOrImportDefault)) && (fileModuleKind < ModuleKind.System)));
        if (create)
        {
            externalHelpersModuleName = emitContext.Factory.NewUniqueName(externalHelpersModuleNameText);
            emitContext.SetExternalHelpersModuleName(node, externalHelpersModuleName);
        }

        return externalHelpersModuleName;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:348:1.
    internal static bool IsNamedDefaultReference(Node e)
    {
        return Ast.ModuleExportNameIsDefault(e.PropertyNameOrName());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:352:1.
    internal static bool ContainsDefaultReference(Node node)
    {
        return (((node != null) && ((Ast.IsNamedImports(node) || Ast.IsNamedExports(node)))) && Core.Some(node.Elements(), IsNamedDefaultReference));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:356:1.
    internal static bool GetExportNeedsImportStarHelper(ExportDeclaration node)
    {
        return (Ast.GetNamespaceDeclarationNode(node.AsNode()) != null);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:360:1.
    internal static bool GetImportNeedsImportStarHelper(ImportDeclaration node)
    {
        if ((Ast.GetNamespaceDeclarationNode(node.AsNode()) != null))
        {
            return true;
        }

        if ((node.ImportClause == null))
        {
            return false;
        }

        var bindings = node.ImportClause.AsImportClause().NamedBindings;
        if ((bindings == null))
        {
            return false;
        }

        if (!Ast.IsNamedImports(bindings))
        {
            return false;
        }

        var namedImports = bindings.AsNamedImports();
        var defaultRefCount = 0;
        {
            foreach (var binding in GoRuntime.RangeValues(namedImports.Elements.Nodes))
            {
                if (IsNamedDefaultReference(binding))
                {
                    defaultRefCount++;
                }
            }
        }

        return ((((defaultRefCount > 0) && (defaultRefCount != GoRuntime.Len(namedImports.Elements.Nodes)))) || (((((GoRuntime.Len(namedImports.Elements.Nodes) - defaultRefCount)) != 0) && Ast.IsDefaultImport(node.AsNode()))));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go:385:1.
    internal static bool GetImportNeedsImportDefaultHelper(ImportDeclaration node)
    {
        return (!GetImportNeedsImportStarHelper(node) && ((Ast.IsDefaultImport(node.AsNode()) || (((node.ImportClause != null) && ContainsDefaultReference(node.ImportClause.AsImportClause().NamedBindings))))));
    }
}
