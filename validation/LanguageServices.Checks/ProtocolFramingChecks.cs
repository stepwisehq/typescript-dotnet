// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/lsp/lsproto/baseproto_test.go assertions, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Text;
using TypeScript.Lsp.Protocol;
namespace LanguageServices.Checks;
public static class ProtocolFramingChecks
{
    public static readonly string[] Names = ["TestBaseReader", "TestBaseReaderMultipleReads", "TestBaseWriter", "TestBaseWriterWriteError"];
    public static int RunAll()
    {
        foreach (var name in Names) Run(name);
        return Names.Length;
    }
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestBaseReader": TestBaseReader(); break;
            case "TestBaseReaderMultipleReads": TestBaseReaderMultipleReads(); break;
            case "TestBaseWriter": TestBaseWriter(); break;
            case "TestBaseWriterWriteError": TestBaseWriterWriteError(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original framing test");
        }
    }
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
    private static void Equal(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
    {
        if (!actual.SequenceEqual(expected)) throw new InvalidOperationException("Original framing bytes differ");
    }
    private static void Error(Action action, string expected)
    {
        try { action(); }
        catch (IOException error)
        {
            if (error.Message != expected) throw new InvalidOperationException($"Expected {expected}, got {error.Message}", error);
            return;
        }
        throw new InvalidOperationException("Expected framing error: " + expected);
    }
    private static void TestBaseReader()
    {
        (string Input, string? Value, string? Error)[] tests =
        [
            ("Content-Length: 0\r\n\r\n", null, "jsonrpc: no content length"),
            ("oops", null, "EOF"),
            ("Content-Length: -1\r\n\r\n", null, "jsonrpc: invalid content length: negative value -1"),
            ("Content-Length: 1\r\n\r\n{", "{", null),
            ("Content-Length: 2\r\n\r\n{}", "{}", null),
            ("Content-Length: 2\r\nExtra: 1\r\n\r\n{}", "{}", null),
            ("Content-Length: 100\r\n\r\n{}", null, "jsonrpc: read content: unexpected EOF"),
            ("Content-Length: \r\n\r\n{}", null, "jsonrpc: invalid content length: parse error: strconv.ParseInt: parsing \"\": invalid syntax"),
            ("Nope\r\n\r\n{}", null, "jsonrpc: invalid header: \"Nope\\r\\n\"")
        ];
        foreach (var test in tests)
        {
            var reader = BaseReader.NewBaseReader(new MemoryStream(Bytes(test.Input), writable: false));
            if (test.Error is not null) Error(() => reader.Read(), test.Error);
            else Equal(reader.Read(), Bytes(test.Value!));
        }
    }
    private static void TestBaseReaderMultipleReads()
    {
        var reader = BaseReader.NewBaseReader(new MemoryStream(Bytes("Content-Length: 4\r\n\r\n1234Content-Length: 2\r\n\r\n{}"), writable: false));
        Equal(reader.Read(), "1234"u8);
        Equal(reader.Read(), "{}"u8);
        Error(() => reader.Read(), "EOF");
    }
    private static void TestBaseWriter()
    {
        (string Value, string Input)[] tests =
        [
            ("{}", "Content-Length: 2\r\n\r\n{}"),
            ("{\"key\":\"value\"}", "Content-Length: 15\r\n\r\n{\"key\":\"value\"}")
        ];
        foreach (var test in tests)
        {
            using var buffer = new MemoryStream();
            var writer = BaseWriter.NewBaseWriter(buffer);
            writer.Write(Bytes(test.Value));
            Equal(buffer.ToArray(), Bytes(test.Input));
        }
    }
    private static void TestBaseWriterWriteError()
    {
        var writer = BaseWriter.NewBaseWriter(new ErrorWriter());
        Error(() => writer.Write("{}"u8), "test error");
    }
    private sealed class ErrorWriter : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("test error");
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("test error");
    }
}
