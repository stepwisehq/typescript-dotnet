// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/parseoptions.go; standalone parsing slice.
#nullable disable
namespace TypeScript.Scanner;
public readonly record struct ExternalModuleIndicatorOptions(bool JSX = false, bool Force = false);
public readonly record struct SourceFileParseOptions(string FileName = "", string PathKey = "", ExternalModuleIndicatorOptions ExternalModuleIndicatorOptions = default);
public static partial class Ast
{
    public static void SetExternalModuleIndicator(SourceFile file, ExternalModuleIndicatorOptions opts) => file.ExternalModuleIndicator = getExternalModuleIndicator(file, opts);
    private static Node getExternalModuleIndicator(SourceFile file, ExternalModuleIndicatorOptions opts)
    {
        if (file.ScriptKind == ScriptKind.JSON)
            return null;
        var node = isFileProbablyExternalModule(file);
        if (node is not null)
            return node;
        if (file.IsDeclarationFile)
            return null;
        if (opts.JSX)
        {
            node = isFileModuleFromUsingJSXTag(file);
            if (node is not null)
                return node;
        }

        return opts.Force ? file.AsNode() : null;
    }

    private static Node isFileProbablyExternalModule(SourceFile file)
    {
        if (file.Statements?.Nodes is not null)
            foreach (var statement in file.Statements.Nodes)
                if (isAnExternalModuleIndicatorNode(statement))
                    return statement;
        return getImportMetaIfNecessary(file);
    }

    private static bool isAnExternalModuleIndicatorNode(Node node) => HasSyntacticModifier(node, ModifierFlags.Export) || IsImportEqualsDeclaration(node) && IsExternalModuleReference(node.AsImportEqualsDeclaration().ModuleReference) || IsImportDeclaration(node) || IsExportAssignment(node) || IsExportDeclaration(node);
    private static Node getImportMetaIfNecessary(SourceFile file) => (file.Flags & NodeFlags.PossiblyContainsImportMeta) != 0 ? findChildNode(file.AsNode(), IsImportMeta) : null;
    private static Node findChildNode(Node root, Func<Node, bool> check)
    {
        Node result = null;
        Visitor visit = null;
        visit = node =>
        {
            if (check(node))
            {
                result = node;
                return true;
            }

            return node.ForEachChild(visit);
        };
        visit(root);
        return result;
    }

    private static Node isFileModuleFromUsingJSXTag(SourceFile file) => walkTreeForJSXTags(file.AsNode());
    private static Node walkTreeForJSXTags(Node root)
    {
        Node found = null;
        Visitor visitor = null;
        visitor = node =>
        {
            if (found is not null)
                return true;
            if ((node.SubtreeFactsValue() & SubtreeFactValues.ContainsJsx) == 0)
                return false;
            if (IsJsxOpeningLikeElement(node) || IsJsxFragment(node))
            {
                found = node;
                return true;
            }

            return node.ForEachChild(visitor);
        };
        visitor(root);
        return found;
    }
}
