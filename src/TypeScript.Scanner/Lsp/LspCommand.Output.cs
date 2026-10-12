// Copyright 2009 The Go Authors. All rights reserved.
// Modified C# port of Go1.27.1 os/exec/exec.go:1138–1207.
// Licensed under the BSD license in licenses/Go-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.Lsp;
public static partial class LspCommand
{
    // One actual stderr writer owns the same bounded prefix and suffix ring as Cmd.Output.
    private sealed class PrefixSuffixSaver : Stream
    {
        private const int N = 32 << 10;
        private Slice<byte> prefix, suffix;
        private int suffixOffset;
        private long skipped;
        public override void Write(ReadOnlySpan<byte> bytes)
        {
            bytes = Fill(ref prefix, bytes);
            int overage = bytes.Length - N;
            if (overage > 0)
            {
                bytes = bytes[overage..];
                skipped += overage;
            }

            bytes = Fill(ref suffix, bytes);
            while (!bytes.IsEmpty)
            {
                int count = Math.Min(suffix.Length - suffixOffset, bytes.Length);
                bytes[..count].CopyTo(suffix.AsSpan()[suffixOffset..]);
                bytes = bytes[count..];
                skipped += count;
                suffixOffset += count;
                if (suffixOffset == N)
                    suffixOffset = 0;
            }
        }

        private static ReadOnlySpan<byte> Fill(ref Slice<byte> destination, ReadOnlySpan<byte> bytes)
        {
            int remaining = N - destination.Length;
            if (remaining > 0)
            {
                int add = Math.Min(bytes.Length, remaining);
                destination = destination.Append(bytes[..add]);
                bytes = bytes[add..];
            }

            return bytes;
        }

        internal Slice<byte> Bytes()
        {
            if (suffix.IsNil)
                return prefix;
            if (skipped == 0)
                return prefix.Append(suffix);
            using var result = new MemoryStream(prefix.Length + suffix.Length + 50);
            result.Write(prefix.AsSpan());
            result.Write("\n... omitting "u8);
            result.Write(System.Text.Encoding.ASCII.GetBytes(skipped.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            result.Write(" bytes ...\n"u8);
            result.Write(suffix.AsSpan()[suffixOffset..]);
            result.Write(suffix.AsSpan()[..suffixOffset]);
            return Slice<byte>.FromArray(result.ToArray());
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
