// Authored validation launch of the actual production Server, with forwarded Go host inputs.
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks.Fourslash;
internal static class CandidateHost
{
    // Validation boundary evidence only; framing stdout remains untouched.
    internal static void ReportOutcome(string operation, Exception? error)
    {
        string text = error?.ToString() ?? "success";
        if (text.Length > 4096)
            text = text[..4096] + " [truncated]";
        try
        {
            Console.Error.WriteLine("candidate " + operation + " outcome: " + text);
        }
        catch (Exception failure) when (failure is IOException or ObjectDisposedException)
        {
            // Observation failure must not replace the actual lifecycle outcome.
        }
    }

    internal static Exception? Run(string hostname, int port, CancellationToken cancellation)
    {
        using var socket = new TcpClient();
        socket.Connect(hostname, port);
        var ready = new TaskCompletionSource<Server>(TaskCreationOptions.RunContinuationsAsynchronously);
        var projection = new StateProjection();
        CaseSensitivity sensitivity = default;
        var stderrStreams = new ConcurrentDictionary<long, Stream>();
        long nextStderr = 0;
        using var connection = new ControlConnection(socket.GetStream(), (operation, args) =>
        {
            Server? server = operation.StartsWith("server.", StringComparison.Ordinal) ? ready.Task.GetAwaiter().GetResult() : null;
            switch (operation)
            {
                case "process.stderr":
                    var bytes = Convert.FromBase64String(args["bytes"]!.GetValue<string>());
                    var stream = stderrStreams[args["handle"]!.GetValue<long>()];
                    lock (stream)
                    {
                        stream.Write(bytes);
                        stream.Flush();
                    }

                    return System.Text.Json.Nodes.JsonValue.Create(bytes.Length);
                case "server.inferredOptions":
                    var(raw, error) = JsonCodec.ParseDictionary(RawTransport.Decode(args["options"]!));
                    if (error is not null)
                        throw error;
                    if (raw is not Dictionary<string, object?> fields)
                        throw new InvalidDataException("Compiler option snapshot must be an object.");
                    var options = new CompilerOptions();
                    foreach (var pair in fields)
                        if (!TsOptions.parseCompilerOptions(pair.Key, pair.Value, options))
                            throw new InvalidDataException("Unrecognized finalized compiler option: " + pair.Key);
                    server!.SetCompilerOptionsForInferredProjects(ProtocolRequestContext.Default, options);
                    return null;
                case "server.initComplete":
                    server!.InitComplete().WaitAsync(cancellation).GetAwaiter().GetResult();
                    return null;
                case "server.stateDiff":
                    var files = args["openFiles"]!.AsArray().Select(value => RawTransport.Decode(value!)).ToArray();
                    return System.Text.Json.Nodes.JsonValue.Create(RawTransport.Encode(projection.Observe(server!.Session(), args["initialized"]!.GetValue<bool>(), files, sensitivity)));
                default:
                    throw new InvalidDataException("Unknown candidate server operation: " + operation);
            }
        });
        try
        {
            var host = connection.Call("host.info", new JsonObject())!.AsObject();
            sensitivity = (CaseSensitivity)host["caseSensitivity"]!.GetValue<int>();
            var fs = new RemoteFileSystem(connection, sensitivity);
            var options = new ServerOptions();
            options.In = LspTransport.ToReader(Console.OpenStandardInput());
            options.Out = LspTransport.ToWriter(Console.OpenStandardOutput());
            options.Err = Console.OpenStandardError();
            options.FS = fs;
            options.ParseCache = TypeScript.ProjectSystem.ParseCaches.NewParseCache(new TypeScript.ProjectSystem.RefCountCacheOptions(DisableDeletion: true));
            options.ProgressDelay = TimeSpan.FromTicks(host["progressTicks"]!.GetValue<long>());
            options.Cwd = RootedDirectoryPath.FromValue(RawTransport.DecodeValue(host["cwd"]!));
            options.DefaultLibraryPath = RootedDirectoryPath.FromValue(RawTransport.DecodeValue(host["defaultLibraryPath"]!));
            options.TypingsLocation = RootedDirectoryPath.FromValue(RawTransport.DecodeValue(host["typingsLocation"]!));
            options.SetParentProcessID = host["hasParentProcessCallback"]!.GetValue<bool>() ? pid => connection.Call("host.parentProcess", new JsonObject { ["pid"] = System.Text.Json.Nodes.JsonValue.Create(pid) }) : null;
            options.NpmInstall = host["hasNpmInstaller"]!.GetValue<bool>() ? (ctx, directory, arguments) =>
            {
                var items = new JsonArray();
                foreach (var argument in arguments)
                    items.Add((JsonNode?)System.Text.Json.Nodes.JsonValue.Create(RawTransport.Encode(argument)));
                var result = connection.Call("host.npmInstall", new JsonObject { ["directory"] = System.Text.Json.Nodes.JsonValue.Create(RawTransport.Encode(directory)), ["arguments"] = items })!.AsObject();
                return (Convert.FromBase64String(result["output"]!.GetValue<string>()), result["error"] is { } error ? new RemoteHostException(RawTransport.DecodeValue(error)) : null);
            } : null;
            options.Spawn = host["hasSpawner"]!.GetValue<bool>() ? (command, directory, stderr) =>
            {
                long stderrHandle = Interlocked.Increment(ref nextStderr);
                stderrStreams[stderrHandle] = stderr;
                var args = new JsonObject
                {
                    ["directory"] = System.Text.Json.Nodes.JsonValue.Create(RawTransport.Encode(directory)),
                    ["stderrHandle"] = System.Text.Json.Nodes.JsonValue.Create(stderrHandle)
                };
                var commands = new JsonArray();
                foreach (var part in command)
                    commands.Add((JsonNode?)System.Text.Json.Nodes.JsonValue.Create(RawTransport.Encode(part)));
                args["command"] = commands;
                try
                {
                    var result = connection.Call("process.spawn", args)!;
                    return (new RemoteProcessStream(connection, result.GetValue<long>(), () => stderrStreams.TryRemove(stderrHandle, out _)), null);
                }
                catch (Exception error)
                {
                    stderrStreams.TryRemove(stderrHandle, out _);
                    return (null, error);
                }
            } : null;
            var server = Server.NewServer(options);
            ready.SetResult(server);
            var outcome = server.Run(cancellation);
            ReportOutcome("Server.Run", outcome);
            return outcome;
        }
        catch (Exception error)
        {
            ready.TrySetException(error);
            return error;
        }
    }
}
