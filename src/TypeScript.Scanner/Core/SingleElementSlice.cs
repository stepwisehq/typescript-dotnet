// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/core/core.go:727 SingleElementSlice; actual pointer admission.
namespace TypeScript.Scanner;
internal static partial class Core
{
    internal static Slice<T> SingleElementSlice<T>(T? value)
        where T : class => value == null ? default : Slice<T>.FromArray([value]);
}
