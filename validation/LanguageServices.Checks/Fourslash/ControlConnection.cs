// Authored bidirectional host-control channel; independent of framed LSP stdin/stdout.
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

namespace LanguageServices.Checks.Fourslash;
internal sealed class ControlConnection : IDisposable
{
    private readonly Stream stream;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;
    private readonly object writeGate = new();
    private readonly ConcurrentDictionary<long, TaskCompletionSource<ControlMessage>> pending = new();
    private readonly CancellationTokenSource shutdown = new();
    private readonly Func<string, JsonObject, JsonNode?> dispatch;
    private readonly Thread readThread;
    private long nextId;
    private Exception? terminalError;
    internal ControlConnection(Stream stream, Func<string, JsonObject, JsonNode?> dispatch)
    {
        this.stream = stream;
        this.dispatch = dispatch;
        reader = new StreamReader(stream, new UTF8Encoding(false, true), false, 4096, true);
        writer = new StreamWriter(stream, new UTF8Encoding(false, true), 4096, true);
        readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "Fourslash host-control reader"
        };
        readThread.Start();
    }

    internal JsonNode? Call(string operation, JsonObject arguments)
    {
        long id = Interlocked.Increment(ref nextId);
        var completion = new TaskCompletionSource<ControlMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!pending.TryAdd(id, completion))
            throw new InvalidOperationException("Duplicate control request ID.");
        try
        {
            if (Volatile.Read(ref terminalError)is { } failure)
                throw failure;
            Send(new ControlMessage { Id = id, Operation = operation, Arguments = arguments });
            // Reader remains independent; dispatching a request can itself make host calls.
            var response = completion.Task.GetAwaiter().GetResult();
            if (response.ErrorBase64 is { } error)
                throw new RemoteHostException(RawTransport.DecodeValue(System.Text.Json.Nodes.JsonValue.Create(error)!));
            return response.Result;
        }
        finally
        {
            pending.TryRemove(id, out _);
        }
    }

    private void Send(ControlMessage message)
    {
        lock (writeGate)
        {
            if (terminalError is { } failure)
                throw failure;
            writer.WriteLine(message.ToJson().ToJsonString());
            writer.Flush();
        }
    }

    private void ReadLoop()
    {
        try
        {
            while (!shutdown.IsCancellationRequested)
            {
                string? line = reader.ReadLine();
                if (line is null)
                    throw new EndOfStreamException("Candidate host-control connection closed.");
                var message = ControlMessage.FromJson(JsonNode.Parse(line)!.AsObject());
                if (message.IsResponse)
                {
                    if (!pending.TryGetValue(message.Id, out var completion))
                        throw new InvalidDataException("Unknown control response ID.");
                    completion.TrySetResult(message);
                }
                else
                {
                    // Request work must not occupy this reader: FS and spawner calls
                    // can synchronously cross the same connection in the other direction.
                    _ = Task.Run(() => Handle(message));
                }
            }
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private void Handle(ControlMessage message)
    {
        ControlMessage response;
        try
        {
            response = new ControlMessage
            {
                Id = message.Id,
                IsResponse = true,
                Result = dispatch(message.Operation!, message.Arguments!)
            };
        }
        catch (Exception error)
        {
            response = new ControlMessage
            {
                Id = message.Id,
                IsResponse = true,
                ErrorBase64 = error is RemoteHostException remote ? RawTransport.Encode(remote.Raw) : Convert.ToBase64String(Encoding.UTF8.GetBytes(error.Message))
            };
        }

        try
        {
            Send(response);
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private void Fail(Exception error)
    {
        if (Interlocked.CompareExchange(ref terminalError, error, null)is not null)
            return;
        CandidateHost.ReportOutcome("host-control terminal", error);
        foreach (var completion in pending.Values)
            completion.TrySetException(error);
    }

    public void Dispose()
    {
        Fail(new ObjectDisposedException(nameof(ControlConnection)));
        shutdown.Cancel();
        stream.Dispose();
        try
        {
            readThread.Join();
        }
        finally
        {
            reader.Dispose();
            writer.Dispose();
            shutdown.Dispose();
        }
    }
}
