// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored root-value versus root-pointer observations from pinned Go.
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    public static int RunZeroValueBoundaryControls()
    {
        bool rejected = false;
        try
        {
            ProtocolCodec.Marshal(ProtocolCodec.GetType_baa50df5ae26800a(), default!);
        }
        catch (InvalidOperationException error)
        {
            rejected = error.Message == "exactly one element of BooleanOrHoverOptions should be set";
        }

        Require(rejected, "zero strict union lost its original panic");
        rejected = false;
        try
        {
            ProtocolCodec.Marshal(ProtocolCodec.GetType_c793e0d9a1cc6421(), default!);
        }
        catch (InvalidOperationException error)
        {
            rejected = error.Message == "RegisterOptions must be set";
        }

        Require(rejected, "zero registration lost its original panic");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_eed3cf3342b20f8c(), default!).EqualsValue(Bytes("null")), "zero nullable union changed");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_d2a93ce39bb367de(), default!).EqualsValue(Bytes("{\"label\":\"\"}")), "zero struct value became pointer null");
        Require(ProtocolCodec.Marshal(ProtocolCodec.GetType_cf50404157ead35b(), default).EqualsValue(Bytes("null")), "nil struct pointer became zero struct value");
        return 5;
    }
}
