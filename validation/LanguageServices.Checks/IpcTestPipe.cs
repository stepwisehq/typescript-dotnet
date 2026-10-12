// Authored .NET duplex fixture adaptation for original ipc net.Pipe assertions.
// Writes rendezvous with complete consumption, and either Close wakes both sides.
using System.Collections.Concurrent;

namespace LanguageServices.Checks;
internal sealed class IpcTestPipe : Stream
{
    private sealed class WriteBlock(byte[] bytes)
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly TaskCompletionSource Consumed = new();
    }

    private sealed class Owner
    {
        // Synchronous io/net.Pipe admission must not enqueue a ValueTask-to-Task
        // read completion onto the same pool occupied by compiler callers.
        // Default BlockingCollection storage is the retained FIFO queue.
        internal readonly BlockingCollection<WriteBlock>[] Directions = [new(), new()];
        internal readonly CancellationTokenSource Closed = new();
        internal void Close()
        {
            Closed.Cancel();
            foreach (var direction in Directions)
                direction.CompleteAdding();
            // Do not Dispose queues still borrowed by peer Read/Write. No wait
            // handles are exposed; these managed owners become collectible once
            // both stream endpoints and any active operation release them.
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
                current = owner.Directions[1 - side].Take(owner.Closed.Token);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (InvalidOperationException) when (owner.Directions[1 - side].IsCompleted)
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
        if (owner.Closed.IsCancellationRequested)
            throw new IOException("io: read/write on closed pipe");
        try
        {
            if (!owner.Directions[side].TryAdd(block))
                throw new IOException("io: read/write on closed pipe");
        }
        catch (InvalidOperationException) when (owner.Directions[side].IsAddingCompleted)
        {
            throw new IOException("io: read/write on closed pipe");
        }
        try
        {
            // The sole consumer is this synchronous writer; Task.Wait's internal
            // completion event wakes directly, without an asynchronous proxy.
            block.Consumed.Task.Wait(owner.Closed.Token);
        }
        catch (OperationCanceledException)
        {
            // net/pipe.go:193–197 receives the read acknowledgement without a
            // cancellation select after handing the write to the peer. Close may
            // race the completed acknowledgement; retain success when every byte
            // was already acknowledged by Read.
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
