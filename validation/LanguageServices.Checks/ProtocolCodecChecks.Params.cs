// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/lsp/lsproto/lsp_json_test.go TestUnmarshalParamsRequiresParams.
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static readonly ProtocolType<object?> AnyDescriptor = new("interface {}", static (ref ProtocolDecoder decoder, ref object? value) => decoder.ReadAny(ref value), static (ProtocolEncoder encoder, object? value) => encoder.WriteAny(value));
    private static ByteStringValue Bytes(string text) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(GoString.Bytes(GoString.ToUtf8(text))));
    private static RawJsonValue Raw(string text) => new(GoString.Bytes(GoString.ToUtf8(text)));
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void TestUnmarshalParamsRequiresParams()
    {
        (string Name, object? Params, bool WantError)[] noParamsTests = [("absent", null, false), ("empty", Raw(""), false), ("null", Raw("null"), true), ("object", Raw("{}"), true), ];
        foreach (var test in noParamsTests)
        {
            var request = new RequestMessage
            {
                Params = test.Params
            };
            var(_, error) = request.UnmarshalParams(Method.WorkspaceWorkspaceFoldersInfo.Parameters);
            if (test.WantError)
                Require(error is ProtocolErrorException { Code: ErrorCode.InvalidParams }, "NoParams/" + test.Name);
            else
                Require(error is null, "NoParams/" + test.Name + ": " + error?.Message);
        }

        (string Name, object? Params, bool WantError)[] typedTests = [("absent", null, true), ("empty", Raw(""), true), ("null", Raw("null"), true), ("number", Raw("5"), true), ("string", Raw("\"x\""), true), ("object", Raw("{\"settings\":{\"x\":1}}"), false), ];
        foreach (var test in typedTests)
        {
            var request = new RequestMessage
            {
                Params = test.Params
            };
            var(got, error) = request.UnmarshalParams(Method.WorkspaceDidChangeConfigurationInfo.Parameters);
            if (test.WantError)
                Require(error is ProtocolErrorException { Code: ErrorCode.InvalidParams }, "typed/" + test.Name);
            else
            {
                Require(error is null, "typed/" + test.Name + ": " + error?.Message);
                Require(got is not null && got.Settings is not null, "typed/" + test.Name);
            }
        }
    }
}
