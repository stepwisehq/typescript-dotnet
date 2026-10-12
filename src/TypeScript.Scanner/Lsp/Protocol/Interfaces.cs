// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/lsproto/lsp.go:151-165.
using TypeScript.Scanner;
namespace TypeScript.Lsp.Protocol;

internal interface IHasTextDocumentURI
{
    DocumentUri TextDocumentURI();
}
internal interface IHasTextDocumentPosition : IHasTextDocumentURI
{
    Position TextDocumentPosition();
}
internal interface IHasLocations
{
    Slice<Location>? GetLocations();
}
internal interface IHasLocation
{
    Location GetLocation();
}
