// Complete optional original replay_test.go. Missing optional input retains source skip;
// supplied input executes the actual framed Server and every original replay assertion.
using System.Text;
using TypeScript.JsonRpc;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;

namespace LanguageServices.Checks;
public static class ServerReplayChecks
{
    private sealed record RawMessage(ByteStringValue Kind, ByteStringValue Method, RawJsonValue? Params);
    private static ByteStringValue B(string value) => ByteStringValue.FromString(value);
    private static ByteStringValue OS(string value) => B(GoString.ToUtf8(value));
    private static Dictionary<ByteStringKey, object?> ReadFields(ByteStringValue input)
    {
        var decoder = new ProtocolDecoder(input);
        var fields = new Dictionary<ByteStringKey, object?>();
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            decoder.Finish();
            return fields;
        }

        decoder.ReadObjectStart("lsp_test.rawMessage");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadDecodedPropertyName();
            if (name.EqualsBytes("params"u8))
                fields[ByteStringKey.FromValue(name)] = decoder.ReadValue().Clone();
            else if (name.EqualsBytes("kind"u8) || name.EqualsBytes("method"u8) || name.EqualsBytes("rootDirPlaceholder"u8) || name.EqualsBytes("rootDirUriPlaceholder"u8))
                fields[ByteStringKey.FromValue(name)] = decoder.ReadString();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
        decoder.Finish();
        return fields;
    }

    private static ByteStringValue Field(Dictionary<ByteStringKey, object?> fields, string name) => fields.TryGetValue(ByteStringKey.FromValue(B(name)), out var value) && value is ByteStringValue text ? text : default;
    private static ByteStringValue? ReadBoundedLine(Stream stream)
    {
        using var line = new MemoryStream();
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0 || value == '\n')
            {
                if (value < 0 && line.Length == 0)
                    return null;
                byte[] bytes = line.ToArray();
                int length = bytes.Length;
                if (length != 0 && bytes[length - 1] == '\r')
                    length--;
                return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes).Slice(0, length));
            }

            line.WriteByte((byte)value);
            if (line.Length >= 10 * 1024 * 1024)
                throw new InvalidOperationException("error scanning replay file: token too long");
        }
    }

    private static bool Initialization(RawMessage message) => message.Method.EqualsBytes("initialize"u8) || message.Method.EqualsBytes("initialized"u8);
    private static bool Exit(RawMessage message) => message.Method.EqualsBytes("exit"u8) || message.Method.EqualsBytes("shutdown"u8);
    // Explicit arguments replace Go's optional test-binary flags; no fixture is invented.
    // Return false means the exact original optional-input skip, never pass credit.
    public static bool TestReplay(string? replay = null, string? testDir = null, bool simple = false, bool superSimple = false)
    {
        if (string.IsNullOrEmpty(replay))
        {
            Console.WriteLine("ORIGINAL_SKIP TestReplay: no replay file specified");
            return false;
        }

        if (string.IsNullOrEmpty(testDir))
            throw new InvalidOperationException("testDir must be specified");
        var root = TsPath.RootedFilePathFromAbsolute(OS(testDir));
        var rootURI = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(root);
        var cwd = TsPath.RootedDirectoryPathFromAbsolute(OS(Environment.CurrentDirectory));
        string? cache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (string.IsNullOrEmpty(cache))
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            cache = string.IsNullOrEmpty(home) ? Path.GetTempPath() : Path.Combine(home, ".cache");
        }

        using var client = new LspTestClient(new ServerOptions { Err = Console.OpenStandardError(), Cwd = cwd, FS = TypeScript.Bundled.Bundled.WrapFS(OsVfs.FS()), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = TsPath.ToRootedDirectoryPath(OS(Path.Combine(cache, "typescript", Core.VersionMajorMinor())), cwd), // Same genuine CLI exec adapter; test-only same-package accessibility join pending.
 NpmInstall = LspCommand.NpmInstall });
        using var stream = File.OpenRead(replay);
        using var reader = new BufferedStream(stream, 64 * 1024);
        var firstLine = ReadBoundedLine(reader);
        if (firstLine is null)
            throw new InvalidOperationException("replay file is empty");
        var initial = ReadFields(firstLine.Value);
        var directoryPlaceholder = Field(initial, "rootDirPlaceholder");
        if (directoryPlaceholder.Length == 0)
            directoryPlaceholder = B("@PROJECT_ROOT@");
        var uriPlaceholder = Field(initial, "rootDirUriPlaceholder");
        if (uriPlaceholder.Length == 0)
            uriPlaceholder = B("@PROJECT_ROOT_URI@");
        // Both mixed-width keys use the original generic replacer, not chained Replace.
        var replacer = GoGenericReplacer.Make(Slice<ByteStringValue>.FromArray([directoryPlaceholder, OS(testDir), uriPlaceholder, rootURI.Value]));
        List<RawMessage> messages = new();
        for (ByteStringValue? line; (line = ReadBoundedLine(reader))is not null;)
        {
            var fields = ReadFields(replacer.Replace(line.Value));
            fields.TryGetValue(ByteStringKey.FromValue(B("params")), out var parameters);
            messages.Add(new(Field(fields, "kind"), Field(fields, "method"), parameters as RawJsonValue));
        }

        if (simple)
        {
            List<RawMessage> filtered = new();
            int i = 0;
            for (; i < messages.Count && Initialization(messages[i]); i++)
                filtered.Add(messages[i]);
            int j = messages.Count - 1;
            for (; j >= 0 && Exit(messages[j]); j--)
            {
            }

            for (int k = i; k <= j; k++)
            {
                var method = messages[k].Method;
                if (method.EqualsBytes("textDocument/didOpen"u8) || method.EqualsBytes("textDocument/didChange"u8) || method.EqualsBytes("textDocument/didClose"u8))
                    filtered.Add(messages[k]);
            }

            for (int k = Math.Max(i, j); k < messages.Count; k++)
                filtered.Add(messages[k]);
            messages = filtered;
        }
        else if (superSimple)
        {
            List<RawMessage> filtered = new();
            int i = 0;
            for (; i < messages.Count && Initialization(messages[i]); i++)
                filtered.Add(messages[i]);
            int j = messages.Count - 1;
            for (; j >= 0 && Exit(messages[j]); j--)
            {
            }

            int openIndex = j;
            for (; openIndex >= i; openIndex--)
                if (messages[openIndex].Method.EqualsBytes("textDocument/didOpen"u8))
                {
                    filtered.Add(messages[openIndex]);
                    break;
                }

            for (int k = Math.Max(openIndex + 1, j); k < messages.Count; k++)
                filtered.Add(messages[k]);
            messages = filtered;
        }

        foreach (var raw in messages)
        {
            bool request = raw.Kind.EqualsBytes("request"u8);
            if (!request && !raw.Kind.EqualsBytes("notification"u8))
                throw new InvalidOperationException("unknown message kind: " + GoString.ToString(raw.Kind.Materialize()!));
            ID? id = request ? ID.NewIDInt(client.NextID()) : null;
            var encoder = new ProtocolEncoder();
            encoder.BeginObject();
            encoder.WriteProperty("jsonrpc"u8);
            encoder.WriteString(B("2.0"));
            encoder.WriteProperty("id"u8);
            ProtocolRpcCodec.WriteID(encoder, id);
            encoder.WriteProperty("method"u8);
            encoder.WriteString(raw.Method);
            encoder.WriteProperty("params"u8);
            encoder.WriteAny(raw.Params);
            encoder.EndObject();
            var message = new ProtocolMessage();
            message.UnmarshalJSON(encoder.Finish());
            if (request)
            {
                var(response, ok) = client.SendRequestWorker(message.AsRequest(), id!);
                if (!ok)
                    throw new InvalidOperationException("failed to send request for method " + raw.Method.Materialize());
                if (response!.Error is { } error)
                    throw new InvalidOperationException("server returned error for method " + raw.Method.Materialize() + ": " + error.Message);
            }
            else
                client.WriteMessage(message);
        }

        return true;
    }
}
