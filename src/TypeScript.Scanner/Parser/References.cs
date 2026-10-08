// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/parser/references.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Parser
{
    // Upstream upstream/TypeScript/tsc/internal/parser/references.go:11:1.
    private static void collectExternalModuleReferences(SourceFile file)
    {
        {
            foreach (var node in GoRuntime.RangeValues(file.Statements.Nodes))
            {
                collectModuleReferences(file, node, false);
            }
        }

        if ((((file.Flags & NodeFlags.PossiblyContainsDynamicImport) != 0) || Ast.IsInJSFile(file.AsNode())))
        {
            Ast.ForEachDynamicImportOrRequireCall(file, true, true, (node2, moduleSpecifier) =>
            {
                Ast.SetImportsOfSourceFile(file, GoRuntime.Append(file.Imports(), moduleSpecifier));
                return false;
            });
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/references.go:24:1.
    private static void collectModuleReferences(SourceFile file, Node node, bool inAmbientModule)
    {
        if (Ast.IsAnyImportOrReExport(node))
        {
            var moduleNameExpr = Ast.GetExternalModuleName(node);
            if (((moduleNameExpr != null) && Ast.IsStringLiteral(moduleNameExpr)))
            {
                var moduleName = moduleNameExpr.Text();
                if (((moduleName != "") && ((!inAmbientModule || !TsPath.IsExternalModuleNameRelative(moduleName)))))
                {
                    Ast.SetImportsOfSourceFile(file, GoRuntime.Append(file.Imports(), moduleNameExpr));
                    if (((file.UsesUriStyleNodeCoreModules != Tristate.True) && !file.IsDeclarationFile))
                    {
                        if ((GoStrings.HasPrefix(moduleName, "node:") && !Core.ExclusivelyPrefixedNodeCoreModules.Contains(moduleName)))
                        {
                            file.UsesUriStyleNodeCoreModules = Tristate.True;
                        }
                        else
                        {
                            if (((file.UsesUriStyleNodeCoreModules == Tristate.Unknown) && Core.UnprefixedNodeCoreModules.Contains(moduleName)))
                            {
                                file.UsesUriStyleNodeCoreModules = Tristate.False;
                            }
                        }
                    }
                }
            }

            return;
        }

        if (((Ast.IsModuleDeclaration(node) && Ast.IsAmbientModule(node)) && (((inAmbientModule || Ast.HasSyntacticModifier(node, ModifierFlags.Ambient)) || file.IsDeclarationFile))))
        {
            var nameText = node.AsModuleDeclaration().Name().Text();
            if ((Ast.IsExternalModule(file) || ((inAmbientModule && !TsPath.IsExternalModuleNameRelative(nameText)))))
            {
                file.ModuleAugmentations = GoRuntime.Append(file.ModuleAugmentations, node.AsModuleDeclaration().Name());
            }
            else
            {
                if (!inAmbientModule)
                {
                    file.AmbientModuleNames = GoRuntime.Append(file.AmbientModuleNames, nameText);
                    if ((node.Body() != null))
                    {
                        {
                            foreach (var statement in GoRuntime.RangeValues(node.Body().Statements()))
                            {
                                collectModuleReferences(file, statement, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
