// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored map-name observations backed by protocol_map_boundaries_test.go.
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    public static int RunMapNameBoundaryControls()
    {
        object? dynamicMap = new Dictionary<string, object?>
        {
            ["\u00ff"] = 1,
            ["\u00fe"] = 2
        };
        bool rejected = false;
        try
        {
            ProtocolCodec.Marshal(AnyDescriptor, dynamicMap);
        }
        catch (FormatException error)
        {
            rejected = error.Message.Contains("duplicate object member name \"�\"", StringComparison.Ordinal);
        }

        Require(rejected, "invalid UTF8 dynamic map keys collided without rejection");
        var changes = ProtocolMap<DocumentUri, Slice<TextEdit?>>.Make();
        changes[new DocumentUri(OwnedBytes(new byte[] { 0xff }))] = Slice<TextEdit?>.Make(0);
        changes[new DocumentUri(OwnedBytes(new byte[] { 0xfe }))] = Slice<TextEdit?>.Make(0);
        rejected = false;
        try
        {
            ProtocolCodec.Marshal(ProtocolCodec.GetType_f594be26f67ebc84(), new WorkspaceEdit { Changes = changes });
        }
        catch (FormatException error)
        {
            rejected = error.Message.Contains("duplicate object member name \"�\"", StringComparison.Ordinal);
        }

        Require(rejected, "invalid UTF8 typed map keys collided without rejection");
        dynamicMap = new Dictionary<string, object?>
        {
            [GoString.ToUtf8("é")] = 1,
            [GoString.ToUtf8("é")] = 2
        };
        ProtocolCodec.Marshal(AnyDescriptor, dynamicMap);
        return 3;
    }
}
