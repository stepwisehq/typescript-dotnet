// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/lsproto/baseproto.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Lsp.Protocol;
internal sealed class BaseReader
{
    private readonly TypeScript.JsonRpc.Reader reader;
    private BaseReader(Stream stream) => reader = TypeScript.JsonRpc.Reader.NewReader(stream);
    internal static BaseReader NewBaseReader(Stream stream) => new(stream);
    internal byte[] Read() => reader.Read();
}
internal sealed class BaseWriter
{
    private readonly TypeScript.JsonRpc.Writer writer;
    private BaseWriter(Stream stream) => writer = TypeScript.JsonRpc.Writer.NewWriter(stream);
    internal static BaseWriter NewBaseWriter(Stream stream) => new(stream);
    internal void Write(ReadOnlySpan<byte> data) => writer.Write(data);
}
