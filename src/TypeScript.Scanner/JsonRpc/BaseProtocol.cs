// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/jsonrpc/baseproto.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
internal static class BaseProtocol
{
    internal static readonly IOException ErrInvalidHeader = new("jsonrpc: invalid header");
    internal static readonly IOException ErrInvalidContentLength = new("jsonrpc: invalid content length");
    internal static readonly IOException ErrNoContentLength = new("jsonrpc: no content length");
}

internal sealed class Reader
{
    private readonly Stream reader;
    private Reader(Stream stream) => reader = new BufferedStream(stream);
    internal static Reader NewReader(Stream stream) => new(stream);

    // baseproto.go:34. Header matching is byte/case sensitive; the blank line is CRLF.
    internal byte[] Read()
    {
        long contentLength = 0;
        while (true)
        {
            var line = new GoBuilder();
            while (true)
            {
                int value;
                try { value = reader.ReadByte(); }
                catch (IOException error) { throw new IOException("jsonrpc: read header: " + error.Message, error); }
                if (value < 0)
                    throw new EndOfStreamException("EOF");
                line.WriteByte(value);
                if (value == '\n')
                    break;
            }
            var bytes = line.Value().ByteSpan;
            if (bytes.SequenceEqual("\r\n"u8))
                break;
            int colon = bytes.IndexOf((byte)':');
            if (colon < 0)
                throw new IOException($"jsonrpc: invalid header: {quoteHeader(bytes)}", BaseProtocol.ErrInvalidHeader);
            if (bytes[..colon].SequenceEqual("Content-Length"u8))
            {
                var value = GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(line.Value().ByteView.Slice(colon + 1), GoUnicode.IsSpace), GoUnicode.IsSpace);
                var text = value.Materialize();
                if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out contentLength))
                {
                    int start = text.StartsWith('+') || text.StartsWith('-') ? 1 : 0;
                    bool validDigits = start < text.Length;
                    for (int index = start; index < text.Length; index++)
                        validDigits &= text[index] is >= '0' and <= '9';
                    string reason = validDigits ? "value out of range" : "invalid syntax";
                    throw new IOException("jsonrpc: invalid content length: parse error: strconv.ParseInt: parsing " + GoQuote.Quote(text) + ": " + reason, BaseProtocol.ErrInvalidContentLength);
                }
                if (contentLength < 0)
                    throw new IOException($"jsonrpc: invalid content length: negative value {contentLength}", BaseProtocol.ErrInvalidContentLength);
            }
        }
        if (contentLength <= 0)
            throw BaseProtocol.ErrNoContentLength;
        var data = new byte[checked((int)contentLength)];
        int offset = 0;
        while (offset < data.Length)
        {
            int count;
            try { count = reader.Read(data.AsSpan(offset)); }
            catch (IOException error) { throw new IOException("jsonrpc: read content: " + error.Message, error); }
            if (count == 0)
                throw new IOException("jsonrpc: read content: " + (offset == 0 ? "EOF" : "unexpected EOF"));
            offset += count;
        }
        return data;
    }
    private static string quoteHeader(ReadOnlySpan<byte> bytes) => GoQuote.Quote(GoString.FromBytes(bytes));
}

internal sealed class Writer
{
    private readonly Stream writer;
    private Writer(Stream stream) => writer = new BufferedStream(stream);
    internal static Writer NewWriter(Stream stream) => new(stream);
    // baseproto.go:95. Length counts payload bytes, and each complete frame flushes.
    internal void Write(ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[64];
        "Content-Length: "u8.CopyTo(header);
        if (!System.Buffers.Text.Utf8Formatter.TryFormat(data.Length, header[16..], out int length))
            throw new InvalidOperationException("Content length formatting failed");
        "\r\n\r\n"u8.CopyTo(header[(16 + length)..]);
        writer.Write(header[..(20 + length)]);
        writer.Write(data);
        writer.Flush();
    }
}
