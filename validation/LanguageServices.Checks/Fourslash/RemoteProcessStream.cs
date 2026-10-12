// Owned duplex transport for the actual Go test-injected content-mapper process.
using System.Text.Json.Nodes;

namespace LanguageServices.Checks.Fourslash;
internal sealed class RemoteProcessStream(ControlConnection connection, long handle, Action onClosed) : Stream
{
    private int closed;
    public override bool CanRead => closed == 0;
    public override bool CanWrite => closed == 0;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    private JsonObject Args() => new()
    {
        ["handle"] = JsonValue.Create(handle)
    };
    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(closed != 0, this);
        ValidateBufferArguments(buffer, offset, count);
        if (count == 0)
            return 0;
        var args = Args();
        args["count"] = JsonValue.Create(count);
        var response = connection.Call("process.read", args)!.AsObject();
        var bytes = Convert.FromBase64String(response["bytes"]!.GetValue<string>());
        if (bytes.Length > count)
            throw new InvalidDataException("Remote process read exceeded requested length.");
        bytes.CopyTo(buffer, offset);
        // Go permits bytes plus an error. Deliver bytes first; the host retains
        // that pending error for the next read, matching Stream's separate EOF.
        if (bytes.Length == 0 && response["error"] is { } error)
            throw new RemoteHostException(RawTransport.DecodeValue(error));
        return bytes.Length;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(closed != 0, this);
        ValidateBufferArguments(buffer, offset, count);
        var args = Args();
        args["bytes"] = JsonValue.Create(Convert.ToBase64String(buffer, offset, count));
        var result = connection.Call("process.write", args)!.AsObject();
        if (result["error"] is { } error)
            throw new RemoteHostException(RawTransport.DecodeValue(error));
        if (result["count"]!.GetValue<int>() != count)
            throw new IOException("short write");
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref closed, 1) == 0)
            try
            {
                connection.Call("process.close", Args());
            }
            finally
            {
                onClosed();
            }

        base.Dispose(disposing);
    }
}
