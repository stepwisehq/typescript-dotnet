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
        if (node.Modifiers() == null)
            return false;
        throw unsupported("checkGrammarModifiers: modifiers/decorators");
    }

    private bool checkGrammarSourceFile()
    {
        if ((sourceFile.Flags & NodeFlags.Ambient) != 0)
            throw unsupported("checkGrammarSourceFile: ambient script");
        return false;
    }

    private bool checkGrammarStatementInAmbientContext(Node node)
    {
        if ((node.Flags & NodeFlags.Ambient) != 0)
            throw unsupported("checkGrammarStatementInAmbientContext: ambient");
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
            throw unsupported("checkGrammarVariableDeclaration: ambient");
        if (node.Initializer() == null && scope == NodeFlags.Const)
            return grammarErrorOnNode(node, Diagnostics.X_0_declarations_must_be_initialized, "const");
        if (node.AsVariableDeclaration().ExclamationToken != null)
            throw unsupported("checkGrammarVariableDeclaration: definite assignment");
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
            if ((getCombinedNodeFlagsCached(node.AsVariableStatement().DeclarationList) & NodeFlags.BlockScoped) != 0)
                throw unsupported("checkGrammarForDisallowedBlockScopedVariableStatement: nonscript/block container");
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
