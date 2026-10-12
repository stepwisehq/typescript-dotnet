// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/selectionranges_test.go TestSelectionRangeDepthIsLimited.
using TypeScript.Scanner;
using TypeScript.LanguageServices;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.JsonRpc;

namespace LanguageServices.Checks;
public static class SelectionRangeChecks
{
    public static int RunAll()
    {
        TestSelectionRangeDepthIsLimited();
        return 1;
    }

    public static void TestSelectionRangeDepthIsLimited()
    {
        const int nestingDepth = 12000;
        string text = "const x = " + new string ('(', nestingDepth) + "1" + new string (')', nestingDepth) + ";";
        var sourceFile = Parser.ParseSourceFile(new SourceFileParseOptions("/index.ts", "/index.ts"), text, ScriptKind.TS);
        var lineMap = Lsconv.ComputeLSPLineStarts(ByteStringValue.FromString(text));
        var converters = Converters.NewConverters(PositionEncodingKind.UTF16, _ => lineMap);
        // The original partial Go owner supplies only its converter; no semantic backing.
        var service = new LanguageService(null!, converters);
        var result = LanguageService.getSmartSelectionRange(service, sourceFile, "const x = ".Length + nestingDepth);
        int depth = 0;
        SelectionRange? outermost = null;
        for (var current = result; current is not null; current = current.Parent)
        {
            depth++;
            outermost = current;
        }

        Require(depth == 1000, "selection range depth differs from original maximum");
        var(innerRange, _) = converters.ToLSPRange(sourceFile, new TextRange("const x = ".Length + nestingDepth, "const x = ".Length + nestingDepth + 1));
        Require(result is not null && ProtocolUtilities.CompareRanges(result.Range, innerRange) == 0, "innermost selection range differs");
        var(fullRange, _) = converters.ToLSPRange(sourceFile, new TextRange(sourceFile.Pos(), sourceFile.End()));
        Require(outermost is not null && ProtocolUtilities.CompareRanges(outermost.Range, fullRange) == 0, "outermost selection range differs from full file range");
        var response = new SelectionRangesOrNull
        {
            SelectionRanges = Slice<SelectionRange?>.FromArray([result])
        };
        var message = new ResponseMessage
        {
            ID = ID.NewIDString(ByteStringValue.FromString("selectionRange")),
            Result = new ProtocolPayload<SelectionRangesOrNull>(Method.TextDocumentSelectionRangeInfo.Result, response)
        }.Message();
        _ = message.MarshalJSON();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
