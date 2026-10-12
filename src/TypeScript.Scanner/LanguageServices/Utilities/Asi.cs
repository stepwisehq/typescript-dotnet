// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:9:1.
    internal static bool PositionIsASICandidate(int pos, Node context, SourceFile file)
    {
        var contextAncestor = Ast.FindAncestorOrQuit(context, (ancestor) =>
        {
            if ((ancestor.End() != pos))
            {
                return FindAncestorResult.Quit;
            }

            return Ast.ToFindAncestorResult(SyntaxMayBeASICandidate(ancestor.Kind));
        });
        return ((contextAncestor != null) && NodeIsASICandidate(contextAncestor, file));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:21:1.
    internal static bool SyntaxMayBeASICandidate(Kind kind)
    {
        return (((SyntaxRequiresTrailingCommaOrSemicolonOrASI(kind) || SyntaxRequiresTrailingFunctionBlockOrSemicolonOrASI(kind)) || SyntaxRequiresTrailingModuleBlockOrSemicolonOrASI(kind)) || SyntaxRequiresTrailingSemicolonOrASI(kind));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:28:1.
    internal static bool SyntaxRequiresTrailingCommaOrSemicolonOrASI(Kind kind)
    {
        return (((((kind == Kind.CallSignature) || (kind == Kind.ConstructSignature)) || (kind == Kind.IndexSignature)) || (kind == Kind.PropertySignature)) || (kind == Kind.MethodSignature));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:36:1.
    internal static bool SyntaxRequiresTrailingFunctionBlockOrSemicolonOrASI(Kind kind)
    {
        return (((((kind == Kind.FunctionDeclaration) || (kind == Kind.Constructor)) || (kind == Kind.MethodDeclaration)) || (kind == Kind.GetAccessor)) || (kind == Kind.SetAccessor));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:44:1.
    internal static bool SyntaxRequiresTrailingModuleBlockOrSemicolonOrASI(Kind kind)
    {
        return (kind == Kind.ModuleDeclaration);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:48:1.
    internal static bool SyntaxRequiresTrailingSemicolonOrASI(Kind kind)
    {
        return (((((((((((((((kind == Kind.VariableStatement) || (kind == Kind.ExpressionStatement)) || (kind == Kind.DoStatement)) || (kind == Kind.ContinueStatement)) || (kind == Kind.BreakStatement)) || (kind == Kind.ReturnStatement)) || (kind == Kind.ThrowStatement)) || (kind == Kind.DebuggerStatement)) || (kind == Kind.PropertyDeclaration)) || (kind == Kind.TypeAliasDeclaration)) || (kind == Kind.ImportDeclaration)) || (kind == Kind.ImportEqualsDeclaration)) || (kind == Kind.ExportDeclaration)) || (kind == Kind.NamespaceExportDeclaration)) || (kind == Kind.ExportAssignment));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/asi.go:66:1.
    internal static bool NodeIsASICandidate(Node node, SourceFile file)
    {
        var lastToken = GetLastToken(node, file);
        if (((lastToken != null) && (lastToken.Kind == Kind.SemicolonToken)))
        {
            return false;
        }

        if (SyntaxRequiresTrailingCommaOrSemicolonOrASI(node.Kind))
        {
            if (((lastToken != null) && (lastToken.Kind == Kind.CommaToken)))
            {
                return false;
            }
        }
        else
        {
            if (SyntaxRequiresTrailingModuleBlockOrSemicolonOrASI(node.Kind))
            {
                var lastChild = GetLastChild(node, file);
                if (((lastChild != null) && Ast.IsModuleBlock(lastChild)))
                {
                    return false;
                }
            }
            else
            {
                if (SyntaxRequiresTrailingFunctionBlockOrSemicolonOrASI(node.Kind))
                {
                    var lastChild2 = GetLastChild(node, file);
                    if (((lastChild2 != null) && Ast.IsFunctionBlock(lastChild2)))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!SyntaxRequiresTrailingSemicolonOrASI(node.Kind))
                    {
                        return false;
                    }
                }
            }
        }

        if ((node.Kind == Kind.DoStatement))
        {
            return true;
        }

        var topNode = Ast.FindAncestor(node, (ancestor) =>
        {
            return (ancestor.Parent == null);
        });
        var nextToken = AstNav.FindNextToken(node, topNode, file);
        if (((nextToken == null) || (nextToken.Kind == Kind.CloseBraceToken)))
        {
            return true;
        }

        var startLine = ScannerApi.GetECMALineOfPosition(file, node.End());
        var endLine = ScannerApi.GetECMALineOfPosition(file, AstNav.GetStartOfNode(nextToken, file, false));
        return (startLine != endLine);
    }
}
