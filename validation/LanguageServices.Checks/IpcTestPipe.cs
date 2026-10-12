// Authored .NET duplex fixture adaptation for original ipc net.Pipe assertions.
// Writes rendezvous with complete consumption, and either Close wakes both sides.
using System.Threading.Channels;

namespace LanguageServices.Checks;
internal sealed class IpcTestPipe : Stream
{
    private sealed class WriteBlock(byte[] bytes)
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly TaskCompletionSource Consumed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Owner
    {
        internal readonly Channel<WriteBlock>[] Directions = [Channel.CreateUnbounded<WriteBlock>(), Channel.CreateUnbounded<WriteBlock>()];
        internal readonly CancellationTokenSource Closed = new();
        internal void Close()
        {
            Closed.Cancel();
            foreach (var direction in Directions)
                direction.Writer.TryComplete();
        }
    }

    private readonly Owner owner;
    private readonly int side;
    private WriteBlock? current;
    private int offset;
    private IpcTestPipe(Owner owner, int side)
    {
        this.owner = owner;
        this.side = side;
    }

    internal static (IpcTestPipe Client, IpcTestPipe Server) Pair()
    {
        var owner = new Owner();
        return (new(owner, 0), new(owner, 1));
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length == 0)
            return 0;
        if (current is null)
        {
            try
            {
                current = owner.Directions[1 - side].Reader.ReadAsync(owner.Closed.Token).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (ChannelClosedException)
            {
                return 0;
            }
        }

        int count = Math.Min(buffer.Length, current.Bytes.Length - offset);
        current.Bytes.AsSpan(offset, count).CopyTo(buffer);
        offset += count;
        if (offset == current.Bytes.Length)
        {
            current.Consumed.TrySetResult();
            current = null;
            offset = 0;
        }

        return count;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
            return;
        var block = new WriteBlock(buffer.ToArray());
        if (owner.Closed.IsCancellationRequested || !owner.Directions[side].Writer.TryWrite(block))
            throw new IOException("io: read/write on closed pipe");
        try
        {
            block.Consumed.Task.WaitAsync(owner.Closed.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // net/pipe.go:193–197 receives the read acknowledgement without a
            // cancellation select after handing the write to the peer. WaitAsync
            // can cancel its proxy before the asynchronous completion continuation
            // runs, even though Read already acknowledged every byte.
            if (!block.Consumed.Task.IsCompletedSuccessfully)
                throw new IOException("io: read/write on closed pipe");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            owner.Close();
        base.Dispose(disposing);
    }

    public override void Flush()
    {
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
