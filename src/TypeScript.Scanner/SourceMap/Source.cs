// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/source.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
namespace TypeScript.SourceMap;
internal interface Source
{
    ByteStringView Text();
    RootedFilePath FileName();
    Slice<int> ECMALineMap();
}
