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
        if (node.Kind != Kind.PropertySignature)
            throw unsupported("checkGrammarModifiers: modifiers/decorators");
        ModifierFlags flags = ModifierFlags.None;
        foreach (var modifier in node.ModifierNodes())
        {
            if (modifier.Kind != Kind.ReadonlyKeyword)
                throw unsupported("checkGrammarModifiers: nonreadonly modifier");
            if ((flags & ModifierFlags.Readonly) != 0)
                return grammarErrorOnNode(modifier, Diagnostics.X_0_modifier_already_seen, "readonly");
            flags |= ModifierFlags.Readonly;
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

public sealed partial class Checker
{
    private bool checkGrammarObjectLiteralExpression(Node node)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var property in node.Properties() ?? [])
        {
            if (property.Kind is not (Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment))
                throw unsupported("checkGrammarObjectLiteralExpression: spread/method/accessor");
            var name = property.Name();
            if (name.Kind == Kind.ComputedPropertyName)
                throw unsupported("checkGrammarObjectLiteralExpression: computed name");
            if (property.Kind == Kind.ShorthandPropertyAssignment && property.AsShorthandPropertyAssignment().ObjectAssignmentInitializer != null)
                throw unsupported("checkGrammarObjectLiteralExpression: assignment pattern initializer");
            if (name.Kind == Kind.PrivateIdentifier)
                grammarErrorOnNode(name, Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies);
            if ((property.ModifierNodes()?.Count ?? 0) != 0)
                throw unsupported("checkGrammarObjectLiteralExpression: property modifiers");
            var postfix = property.PostfixToken();
            if (postfix?.Kind == Kind.ExclamationToken)
                grammarErrorOnNode(postfix, Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context);
            if (postfix?.Kind == Kind.QuestionToken)
                grammarErrorOnNode(postfix, Diagnostics.An_object_member_cannot_be_declared_optional);
            if (name.Kind == Kind.NumericLiteral)
                checkGrammarNumericLiteral(name);
            if (name.Kind == Kind.BigIntLiteral)
                throw unsupported("checkGrammarObjectLiteralExpression: bigint key");
            var(effectiveName, hasName) = getEffectivePropertyNameForPropertyNameNode(name);
            if (hasName)
            {
                seen.TryGetValue(effectiveName, out int existingKind);
                if (existingKind == 0)
                    seen[effectiveName] = 1;
                else
                    grammarErrorOnNode(name, Diagnostics.An_object_literal_cannot_have_multiple_properties_with_the_same_name, TypeScript.Scanner.Scanner.GetTextOfNodeByteString(name));
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
            throw unsupported("checkGrammarProperty: ambient initializer");
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
