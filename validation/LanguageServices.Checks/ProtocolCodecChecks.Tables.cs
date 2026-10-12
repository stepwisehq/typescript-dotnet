// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original lsp_json_test.go tables; inputs/error substrings are unchanged.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestUnmarshalRejectsNullForOptionalNonNullableFields()
    {
        {
            // InlayHint kind null
            var target = new InlayHint();
            var input = Bytes("{\"position\": {\"line\": 0, \"character\": 0}, \"label\": \"foo\", \"kind\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"kind\"", StringComparison.Ordinal), "InlayHint kind null" + ": " + error?.Message);
        }

        {
            // InlayHint textEdits null
            var target = new InlayHint();
            var input = Bytes("{\"position\": {\"line\": 0, \"character\": 0}, \"label\": \"foo\", \"textEdits\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"textEdits\"", StringComparison.Ordinal), "InlayHint textEdits null" + ": " + error?.Message);
        }

        {
            // InlayHint paddingLeft null
            var target = new InlayHint();
            var input = Bytes("{\"position\": {\"line\": 0, \"character\": 0}, \"label\": \"foo\", \"paddingLeft\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"paddingLeft\"", StringComparison.Ordinal), "InlayHint paddingLeft null" + ": " + error?.Message);
        }

        {
            // FoldingRange kind null
            var target = new FoldingRange();
            var input = Bytes("{\"startLine\": 0, \"endLine\": 10, \"kind\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_66fbdf0fd1929f3b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"kind\"", StringComparison.Ordinal), "FoldingRange kind null" + ": " + error?.Message);
        }

        {
            // FoldingRange startCharacter null
            var target = new FoldingRange();
            var input = Bytes("{\"startLine\": 0, \"endLine\": 10, \"startCharacter\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_66fbdf0fd1929f3b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"startCharacter\"", StringComparison.Ordinal), "FoldingRange startCharacter null" + ": " + error?.Message);
        }

        {
            // CompletionItem insertTextFormat null
            var target = new CompletionItem();
            var input = Bytes("{\"label\": \"test\", \"insertTextFormat\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d2a93ce39bb367de(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"insertTextFormat\"", StringComparison.Ordinal), "CompletionItem insertTextFormat null" + ": " + error?.Message);
        }

        {
            // Hover range null
            var target = new Hover();
            var input = Bytes("{\"contents\": {\"kind\": \"plaintext\", \"value\": \"hi\"}, \"range\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_c76ae86ad3014c85(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"range\"", StringComparison.Ordinal), "Hover range null" + ": " + error?.Message);
        }

        {
            // WorkDoneProgressOptions workDoneProgress null
            var target = new WorkDoneProgressOptions();
            var input = Bytes("{\"workDoneProgress\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_4c899c625b152feb(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"workDoneProgress\"", StringComparison.Ordinal), "WorkDoneProgressOptions workDoneProgress null" + ": " + error?.Message);
        }

        {
            // CallHierarchyIncomingCallsParams item null
            var target = new CallHierarchyIncomingCallsParams();
            var input = Bytes("{\"item\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_3a8cdb686bf71f77(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"item\"", StringComparison.Ordinal), "CallHierarchyIncomingCallsParams item null" + ": " + error?.Message);
        }

        {
            // CallHierarchyIncomingCall from null
            var target = new CallHierarchyIncomingCall();
            var input = Bytes("{\"from\": null, \"fromRanges\": []}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_e184a9b2f92e1a06(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"from\"", StringComparison.Ordinal), "CallHierarchyIncomingCall from null" + ": " + error?.Message);
        }

        {
            // InitializeParams capabilities null
            var target = new InitializeParams();
            var input = Bytes("{\"processId\": null, \"rootUri\": null, \"capabilities\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_6cabbf134c6b5195(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"capabilities\"", StringComparison.Ordinal), "InitializeParams capabilities null" + ": " + error?.Message);
        }

        {
            // InitializeResult capabilities null
            var target = new InitializeResult();
            var input = Bytes("{\"capabilities\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_ff2c54bb0124e1e5(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"capabilities\"", StringComparison.Ordinal), "InitializeResult capabilities null" + ": " + error?.Message);
        }

        {
            // SemanticTokens data null (required slice)
            var target = new SemanticTokens();
            var input = Bytes("{\"data\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_448a954da33d9419(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"data\"", StringComparison.Ordinal), "SemanticTokens data null (required slice)" + ": " + error?.Message);
        }

        {
            // TextDocumentEdit edits null (required slice)
            var target = new TextDocumentEdit();
            var input = Bytes("{\"textDocument\": {\"uri\": \"file:///a.ts\", \"version\": 1}, \"edits\": null}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_e35cf3d963f0d31d(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("null value is not allowed for field \"edits\"", StringComparison.Ordinal), "TextDocumentEdit edits null (required slice)" + ": " + error?.Message);
        }
    }

    private static void TestUnmarshalAcceptsNullForNullableFields()
    {
        {
            // InitializeParams rootUri null
            var target = new InitializeParams();
            var input = Bytes("{\"processId\": null, \"rootUri\": null, \"capabilities\": {}}");
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_6cabbf134c6b5195(), input, ref target);
        }

        {
            // InitializeParams workspaceFolders null
            var target = new InitializeParams();
            var input = Bytes("{\"processId\": null, \"rootUri\": null, \"capabilities\": {}, \"workspaceFolders\": null}");
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_6cabbf134c6b5195(), input, ref target);
        }

        {
            // InitializeParams processId null
            var target = new InitializeParams();
            var input = Bytes("{\"processId\": null, \"rootUri\": null, \"capabilities\": {}}");
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_6cabbf134c6b5195(), input, ref target);
        }

        {
            // InitializationOptions userPreferences null
            var target = new InitializationOptions();
            var input = Bytes("{\"userPreferences\": null}");
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d6291485da709fea(), input, ref target);
        }

        {
            // InitializeParams initializationOptions null
            var target = new InitializeParams();
            var input = Bytes("{\"processId\": null, \"rootUri\": null, \"capabilities\": {}, \"initializationOptions\": null}");
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_6cabbf134c6b5195(), input, ref target);
        }
    }

    private static void TestUnmarshalRejectsIncompleteObjects()
    {
        {
            // InlayHint missing position
            var target = new InlayHint();
            var input = Bytes("{\"label\": \"test\"}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("missing required properties: position", StringComparison.Ordinal), "InlayHint missing position" + ": " + error?.Message);
        }

        {
            // InlayHint missing label
            var target = new InlayHint();
            var input = Bytes("{\"position\": {\"line\": 0, \"character\": 0}}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_551be06dac20fdd0(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("missing required properties: label", StringComparison.Ordinal), "InlayHint missing label" + ": " + error?.Message);
        }

        {
            // Location missing uri
            var target = new Location();
            var input = Bytes("{\"range\": {\"start\": {\"line\": 0, \"character\": 0}, \"end\": {\"line\": 0, \"character\": 0}}}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("missing required properties: uri", StringComparison.Ordinal), "Location missing uri" + ": " + error?.Message);
        }

        {
            // Location empty object
            var target = new Location();
            var input = Bytes("{}");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null && error.Message.Contains("missing required properties: uri, range", StringComparison.Ordinal), "Location empty object" + ": " + error?.Message);
        }
    }

    private static void TestUnmarshalRejectsWrongTypes()
    {
        {
            // Location receives array
            var target = new Location();
            var input = Bytes("[]");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null, "Location receives array");
        }

        {
            // Location receives string
            var target = new Location();
            var input = Bytes("\"not an object\"");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null, "Location receives string");
        }

        {
            // Location receives number
            var target = new Location();
            var input = Bytes("42");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null, "Location receives number");
        }

        {
            // Location receives null
            var target = new Location();
            var input = Bytes("null");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_15b61974b2707a7b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null, "Location receives null");
        }

        {
            // FoldingRange receives boolean
            var target = new FoldingRange();
            var input = Bytes("true");
            Exception? error = null;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_66fbdf0fd1929f3b(), input, ref target);
            }
            catch (Exception failure)when (failure is System.Text.Json.JsonException or FormatException)
            {
                error = failure;
            }

            Require(error is not null, "FoldingRange receives boolean");
        }
    }
}
