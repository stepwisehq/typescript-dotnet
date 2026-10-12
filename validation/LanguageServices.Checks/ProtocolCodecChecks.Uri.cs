// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/lsp/lsproto/lsp_test.go TestTryDynamicFileNameToDocumentUri.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    private static void TestTryDynamicFileNameToDocumentUri()
    {
        var uri = new DocumentUri(ByteStringValue.FromString("untitled://wsl+ubuntu/home/user/file.ts?version=1"));
        var path = uri.Path();
        var(roundTrip, ok) = ProtocolUris.TryDynamicFileNameToDocumentUri(path);
        Require(ok, "dynamic URI round trip was rejected");
        Require(roundTrip == uri, "dynamic URI round trip changed URI");
        string[] malformed = ["^/invalid", "^/~ts-uri~//authority/path", "^/~ts-uri~/scheme/~ts-uri-escape~zz~/path", "^/~ts-uri~/scheme/authority/~ts-uri-escape~zz~", "^/~ts-uri~/scheme/authority/~ts-uri-no-path~zz~", ];
        foreach (var fileName in malformed)
        {
            var(_, valid) = ProtocolUris.TryDynamicFileNameToDocumentUri(RootedPath.FromGoString(fileName));
            Require(!valid, "expected malformed path to be rejected: " + fileName);
        }
    }
}
