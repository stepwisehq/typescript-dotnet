// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored observations from protocol_json_boundaries_test.go, executed against pinned Go.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Message = TypeScript.Lsp.Protocol.Message;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static ByteStringValue OwnedBytes(byte[] bytes) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
    public static int RunJSONBoundaryControls()
    {
        var invalid = new List<byte>("{\"label\":\""u8.ToArray());
        invalid.Add(0xff);
        invalid.AddRange("\"}"u8.ToArray());
        CompletionItem value = new();
        bool rejected = false;
        try
        {
            ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d2a93ce39bb367de(), OwnedBytes(invalid.ToArray()), ref value);
        }
        catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
        {
            rejected = true;
        }

        Require(rejected, "invalid UTF8 admitted");
        var encoded = ProtocolCodec.Marshal(ProtocolCodec.GetType_d2a93ce39bb367de(), new CompletionItem { Label = OwnedBytes(new byte[] { 0xff }) });
        Require(encoded.EqualsValue(Bytes("{\"label\":\"�\"}")), "invalid UTF8 encoding differs");
        foreach (string input in new[]
        {
            "{\"label\":\"a\",\"label\":\"b\"}",
            "{\"label\":\"a\",\"l\\u0061bel\":\"b\"}"
        }

        )
        {
            value = new();
            rejected = false;
            try
            {
                ProtocolCodec.Unmarshal(ProtocolCodec.GetType_d2a93ce39bb367de(), Bytes(input), ref value);
            }
            catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
            {
                rejected = true;
            }

            Require(rejected, "duplicate label admitted");
        }

        var ownedInput = GoString.Bytes(GoString.ToUtf8("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"text\":\"café\"}}"));
        var message = new Message();
        message.UnmarshalJSON(OwnedBytes(ownedInput));
        Array.Fill(ownedInput, (byte)'x');
        Require(message.AsResponse().Result is RawJsonValue retained && retained.TextValue.EqualsValue(Bytes("{\"text\":\"café\"}")), "deferred result lost owned bytes");
        var request = new RequestMessage();
        request.UnmarshalJSON(Bytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"first\",\"params\":{\"x\":1}}"));
        request.UnmarshalJSON(Bytes("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"second\"}"));
        Require(request.Params is RawJsonValue parameters && parameters.TextValue.EqualsValue(Bytes("{\"x\":1}")) && request.Method.Value.EqualsValue(Bytes("second")) && request.ID!.MustInt() == 2, "reused request absent parameters changed");
        message.UnmarshalJSON(Bytes("{\"jsonrpc\":\"2.0\",\"id\":1}"));
        Require(message.AsResponse().Result is RawJsonValue { IsNil: true }, "nil raw result type lost");
        Require(message.MarshalJSON().EqualsValue(Bytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":null}")), "nil raw result omitted");
        return 7;
    }
}
