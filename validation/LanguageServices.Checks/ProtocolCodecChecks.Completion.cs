// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestUnmarshalCompletionItem complete expected struct.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Range = TypeScript.Lsp.Protocol.Range;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalCompletionItem()
    {
        var result = Decode(ProtocolCodec.GetType_d2a93ce39bb367de(), "{\n    \"label\": \"pageXOffset\",\n    \"insertTextFormat\": 1,\n    \"textEdit\": {\n        \"newText\": \"pageXOffset\",\n        \"insert\": {\n            \"start\": {\n                \"line\": 4,\n                \"character\": 0\n            },\n            \"end\": {\n                \"line\": 4,\n                \"character\": 4\n            }\n        },\n        \"replace\": {\n            \"start\": {\n                \"line\": 4,\n                \"character\": 0\n            },\n            \"end\": {\n                \"line\": 4,\n                \"character\": 4\n            }\n        }\n    },\n    \"kind\": 6,\n    \"sortText\": \"15\",\n    \"commitCharacters\": [\n        \".\",\n        \",\",\n        \";\"\n    ]\n}");
        var range = new Range
        {
            Start = new Position
            {
                Line = 4,
                Character = 0
            },
            End = new Position
            {
                Line = 4,
                Character = 4
            }
        };
        var expected = new CompletionItem
        {
            Label = Text("pageXOffset"),
            InsertTextFormat = InsertTextFormat.PlainText,
            TextEdit = new TextEditOrInsertReplaceEdit
            {
                InsertReplaceEdit = new InsertReplaceEdit
                {
                    NewText = Text("pageXOffset"),
                    Insert = range,
                    Replace = range
                },
            },
            Kind = CompletionItemKind.Variable,
            SortText = Text("15"),
            CommitCharacters = Slice<ByteStringValue>.FromArray([Text("."), Text(","), Text(";")]),
        };
        Require(EqualShape_d2a93ce39bb367de(result, expected), "CompletionItem structural value");
    }
}
