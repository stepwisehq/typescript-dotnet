// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original decode subtests; exact inputs and every assertion are preserved.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalStringOrArrayUnion()
    {
        {
            // StringOrInlayHintLabelParts with string
            var v = new StringOrInlayHintLabelParts();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_be643dd009a1ea80(), Bytes("\"hello\""), ref v);
            Require(v.String != null, "StringOrInlayHintLabelParts with string");
            Require(v.String.Value.EqualsValue(Text("hello")), "StringOrInlayHintLabelParts with string");
            Require(v.InlayHintLabelParts == null, "StringOrInlayHintLabelParts with string");
        }

        {
            // StringOrInlayHintLabelParts with array
            var v = new StringOrInlayHintLabelParts();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_be643dd009a1ea80(), Bytes("[{\"value\": \"param\"}, {\"value\": \": \"}, {\"value\": \"string\"}]"), ref v);
            Require(v.String == null, "StringOrInlayHintLabelParts with array");
            Require(v.InlayHintLabelParts != null, "StringOrInlayHintLabelParts with array");
            Require(v.InlayHintLabelParts.Value.Length == 3, "StringOrInlayHintLabelParts with array");
            Require(v.InlayHintLabelParts.Value[0].Value.EqualsValue(Text("param")), "StringOrInlayHintLabelParts with array");
        }
    }

    private static void TestUnmarshalDocumentEditUnion()
    {
        {
            // TextDocumentEdit without kind
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\n\t\t\t\"textDocument\": {\"uri\": \"file:///a.ts\", \"version\": 1},\n\t\t\t\"edits\": [{\"range\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 0}}, \"newText\": \"x\"}]\n\t\t}"), ref v);
            Require(v.TextDocumentEdit != null, "TextDocumentEdit without kind");
            Require(v.CreateFile == null, "TextDocumentEdit without kind");
            Require(v.RenameFile == null, "TextDocumentEdit without kind");
            Require(v.DeleteFile == null, "TextDocumentEdit without kind");
        }

        {
            // TextDocumentEdit with non-string kind
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\n\t\t\t\"kind\": null,\n\t\t\t\"textDocument\": {\"uri\": \"file:///a.ts\", \"version\": 1},\n\t\t\t\"edits\": []\n\t\t}"), ref v);
            Require(v.TextDocumentEdit != null, "TextDocumentEdit with non-string kind");
            Require(v.CreateFile == null, "TextDocumentEdit with non-string kind");
            Require(v.RenameFile == null, "TextDocumentEdit with non-string kind");
            Require(v.DeleteFile == null, "TextDocumentEdit with non-string kind");
        }

        {
            // CreateFile with kind create
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\"kind\": \"create\", \"uri\": \"file:///new.ts\"}"), ref v);
            Require(v.TextDocumentEdit == null, "CreateFile with kind create");
            Require(v.CreateFile != null, "CreateFile with kind create");
            Require(v.CreateFile.Uri.Equals(new DocumentUri(Text("file:///new.ts"))), "CreateFile with kind create");
        }

        {
            // CreateFile with kind after fields
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\"uri\": \"file:///new.ts\", \"kind\": \"create\"}"), ref v);
            Require(v.CreateFile != null, "CreateFile with kind after fields");
            Require(v.CreateFile.Uri.Equals(new DocumentUri(Text("file:///new.ts"))), "CreateFile with kind after fields");
        }

        {
            // RenameFile with kind rename
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\"kind\": \"rename\", \"oldUri\": \"file:///old.ts\", \"newUri\": \"file:///new.ts\"}"), ref v);
            Require(v.RenameFile != null, "RenameFile with kind rename");
            Require(v.RenameFile.OldUri.Equals(new DocumentUri(Text("file:///old.ts"))), "RenameFile with kind rename");
        }

        {
            // DeleteFile with kind delete
            var v = new TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d30cdf3f6150538e(), Bytes("{\"kind\": \"delete\", \"uri\": \"file:///gone.ts\"}"), ref v);
            Require(v.DeleteFile != null, "DeleteFile with kind delete");
            Require(v.DeleteFile.Uri.Equals(new DocumentUri(Text("file:///gone.ts"))), "DeleteFile with kind delete");
        }
    }

    private static void TestUnmarshalFieldOrdering()
    {
        {
            // Location with reversed field order
            var loc = new Location();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), Bytes("{\n\t\t\t\"range\": {\"start\": {\"line\": 1, \"character\": 2}, \"end\": {\"line\": 3, \"character\": 4}},\n\t\t\t\"uri\": \"file:///test.ts\"\n\t\t}"), ref loc);
            Require(loc.Uri.Equals(new DocumentUri(Text("file:///test.ts"))), "Location with reversed field order");
            Require(loc.Range.Start.Line == 1u, "Location with reversed field order");
        }

        {
            // InlayHint with kind before label
            var hint = new InlayHint();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), Bytes("{\n\t\t\t\"kind\": 1,\n\t\t\t\"label\": \"x\",\n\t\t\t\"position\": {\"line\": 0, \"character\": 0}\n\t\t}"), ref hint);
            Require(hint.Kind != null, "InlayHint with kind before label");
            Require(hint.Kind.Value == InlayHintKind.Type, "InlayHint with kind before label");
        }
    }

    private static void TestUnmarshalPresenceDiscriminatorUnion()
    {
        {
            // TextEdit via range field
            var v = new TextEditOrInsertReplaceEdit();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_b3a71121264d6d6a(), Bytes("{\n\t\t\t\"range\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 1}},\n\t\t\t\"newText\": \"x\"\n\t\t}"), ref v);
            Require(v.TextEdit != null, "TextEdit via range field");
            Require(v.InsertReplaceEdit == null, "TextEdit via range field");
            Require(v.TextEdit.NewText.EqualsValue(Text("x")), "TextEdit via range field");
        }

        {
            // InsertReplaceEdit via insert field
            var v = new TextEditOrInsertReplaceEdit();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_b3a71121264d6d6a(), Bytes("{\n\t\t\t\"insert\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 1}},\n\t\t\t\"replace\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 2}},\n\t\t\t\"newText\": \"y\"\n\t\t}"), ref v);
            Require(v.TextEdit == null, "InsertReplaceEdit via insert field");
            Require(v.InsertReplaceEdit != null, "InsertReplaceEdit via insert field");
            Require(v.InsertReplaceEdit.NewText.EqualsValue(Text("y")), "InsertReplaceEdit via insert field");
        }
    }

    private static void TestUnmarshalDiscriminatorUnion()
    {
        {
            // WorkDoneProgressBegin
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_fd6812dd893b247d(), Bytes("{\"kind\": \"begin\", \"title\": \"Indexing\"}"), ref v);
            Require(v.Begin != null, "WorkDoneProgressBegin");
            Require(v.Report == null, "WorkDoneProgressBegin");
            Require(v.End == null, "WorkDoneProgressBegin");
            Require(v.Begin.Title.EqualsValue(Text("Indexing")), "WorkDoneProgressBegin");
        }

        {
            // WorkDoneProgressReport
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_fd6812dd893b247d(), Bytes("{\"kind\": \"report\", \"message\": \"50%\"}"), ref v);
            Require(v.Begin == null, "WorkDoneProgressReport");
            Require(v.Report != null, "WorkDoneProgressReport");
            Require(v.End == null, "WorkDoneProgressReport");
            Require(v.Report.Message != null, "WorkDoneProgressReport");
            Require(v.Report.Message.Value.EqualsValue(Text("50%")), "WorkDoneProgressReport");
        }

        {
            // WorkDoneProgressEnd
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_fd6812dd893b247d(), Bytes("{\"kind\": \"end\"}"), ref v);
            Require(v.Begin == null, "WorkDoneProgressEnd");
            Require(v.Report == null, "WorkDoneProgressEnd");
            Require(v.End != null, "WorkDoneProgressEnd");
        }

        {
            // discriminator after variant fields
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_fd6812dd893b247d(), Bytes("{\"title\": \"Indexing\", \"percentage\": 25, \"kind\": \"begin\"}"), ref v);
            Require(v.Begin != null, "discriminator after variant fields");
            Require(v.Begin.Title.EqualsValue(Text("Indexing")), "discriminator after variant fields");
            Require(v.Begin.Percentage != null, "discriminator after variant fields");
            Require(v.Begin.Percentage.Value == 25u, "discriminator after variant fields");
        }

        {
            // optional discriminator is preserved
            CheckOriginalOptionalDiscriminatorArm();
        }

        {
            // invalid discriminator
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            var error = DecodeError(ProtocolCodec.GetType_fd6812dd893b247d(), "{\"kind\": \"invalid\"}", ref v);
            Require(error is not null, "invalid discriminator");
        }

        {
            // non-string discriminator
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            var error = DecodeError(ProtocolCodec.GetType_fd6812dd893b247d(), "{\"kind\": null}", ref v);
            Require(error is not null, "non-string discriminator");
        }

        {
            // missing discriminator
            var v = new WorkDoneProgressBeginOrReportOrEnd();
            var error = DecodeError(ProtocolCodec.GetType_fd6812dd893b247d(), "{\"message\": \"missing kind\"}", ref v);
            Require(error is not null && error.Message.Contains("missing discriminator \"kind\"", StringComparison.Ordinal), "missing discriminator");
        }
    }
}
