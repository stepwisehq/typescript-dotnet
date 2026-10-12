// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/astnav/tokens_test.go:506, unchanged two input/assertion rows.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using Parser = TypeScript.Scanner.Parser;

namespace LanguageServices.Checks;
public static class AstNavChecks
{
    public static readonly string[] Names = ["TestUnitFindPrecedingToken"];
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentException(name);
        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "import {\n    CharacterCodes,\n    compareStringsCaseInsensitive,\n    compareStringsCaseSensitive,\n    compareValues,\n    Comparison,\n    Debug,\n    endsWith,\n    equateStringsCaseInsensitive,\n    equateStringsCaseSensitive,\n    GetCanonicalFileName,\n    getDeclarationFileExtension,\n    getStringComparer,\n    identity,\n    lastOrUndefined,\n    Path,\n    some,\n    startsWith,\n} from \"./_namespaces/ts.js\";\n\n/**\n * Internally, we represent paths as strings with '/' as the directory separator.\n * When we make system calls (eg: LanguageServiceHost.getDirectory()),\n * we expect the host to correctly handle paths in our specified format.\n *\n * @internal\n */\nexport const directorySeparator = \"/\";\n/** @internal */\nexport const altDirectorySeparator = \"\\\\\";\nconst urlSchemeSeparator = \"://\";\nconst backslashRegExp = /\\\\/g;\n\n\nbackslashRegExp.\n\n//Path Tests\n\n/**\n * Determines whether a charCode corresponds to '/' or '\\'.\n *\n * @internal\n */\nexport function isAnyDirectorySeparator(charCode: number): boolean {\n    return charCode === CharacterCodes.slash || charCode === CharacterCodes.backslash;\n}", ScriptKind.TS);
            var token = AstNav.FindPrecedingToken(file, 839);
            if (token?.Kind != Kind.DotToken)
                throw new InvalidOperationException("Original AstNav: after dot in jsdoc");
        }

        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "takesCb((n, s, ))", ScriptKind.TS);
            var token = AstNav.FindPrecedingToken(file, 15);
            if (token?.Kind != Kind.CommaToken)
                throw new InvalidOperationException("Original AstNav: after comma in parameter list");
        }
    }

    // Original TestGetTokenAtPosition: JSDoc and stable token-identity subtests.
    // Baseline/differential channels are separately tracked; this is not the whole function.
    public static int RunOriginalTokenSubtests()
    {
        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/test.js", "/test.js"), "function foo(x) {\n    const s = /**@type {string}*/(x)\n}", ScriptKind.JS);
            var token = AstNav.GetTouchingPropertyName(file, 52);
            if (token == null)
                throw new InvalidOperationException("Original JSDoc assertion: missing token");
            if (token.Kind != Kind.Identifier && token.Kind != Kind.ParenthesizedExpression)
                throw new InvalidOperationException("Original JSDoc assertion: unexpected token kind");
        }

        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/test.js", "/test.js"), "function foo(x) {\n    const s = /**@type {string}*/(x)  // Go-to-definition on x causes panic\n}", ScriptKind.JS);
            var token = AstNav.GetTouchingPropertyName(file, 52);
            if (token == null)
                throw new InvalidOperationException("Original JSDoc assertion: missing token");
        }

        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "\n\t\t\tfunction foo() {\n\t\t\t\treturn 0;\n\t\t\t}\n\t\t", ScriptKind.TS);
            if (!ReferenceEquals(AstNav.GetTokenAtPosition(file, 0), AstNav.GetTokenAtPosition(file, 0)))
                throw new InvalidOperationException("Original token pointer equality assertion");
        }

        return 3;
    }
}
