// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original simple decode bodies, retaining exact JSON strings and field assertions.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalIgnoresUnknownFields()
    {
        {
            var loc = new Location();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), Bytes("{\n\t\t\t\"uri\": \"file:///test.ts\",\n\t\t\t\"range\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 5}},\n\t\t\t\"someUnknownField\": 42,\n\t\t\t\"anotherUnknown\": {\"nested\": true}\n\t\t}"), ref loc);
            Require(loc.Uri.Value.EqualsBytes("file:///test.ts"u8), "Location URI");
        }

        {
            var hint = new InlayHint();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), Bytes("{\n\t\t\t\"position\": {\"line\": 0, \"character\": 0},\n\t\t\t\"label\": \"x\",\n\t\t\t\"futureField\": [1, 2, 3]\n\t\t}"), ref hint);
        }
    }

    private static void TestUnmarshalCompletionItemDataFileName()
    {
        {
            var data = new CompletionItemData();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_ec574f9f7f49a56a(), Bytes("{\"fileName\":\"/src/index.ts\",\"position\":1,\"name\":\"value\"}"), ref data);
            Require(data.FileName.EqualsBytes("/src/index.ts"u8), "CompletionItemData FileName");
        }
    }

    private static void TestUnmarshalEmptyObject()
    {
        {
            var v = new WorkDoneProgressOptions();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_4c899c625b152feb(), Bytes("{}"), ref v);
            Require(v.WorkDoneProgress is null, "WorkDoneProgressOptions optional progress");
        }

        {
            var v = new InitializationOptions();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d6291485da709fea(), Bytes("{}"), ref v);
        }

        {
            var v = new ClientCapabilities();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_ae5beebabf5633ee(), Bytes("{}"), ref v);
        }

        {
            var v = new ServerCapabilities();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_628c62fa6a34c245(), Bytes("{}"), ref v);
        }
    }
}
