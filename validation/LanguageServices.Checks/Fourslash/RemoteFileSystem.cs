// Authored forwarding host: the original Go fixture filesystem remains authoritative.
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.Vfs;
using VfsFileInfo = TypeScript.Vfs.FileInfo;

namespace LanguageServices.Checks.Fourslash;
internal sealed class RemoteFileSystem(ControlConnection connection, CaseSensitivity sensitivity) : IFS, IMutableFileSystem, ISourceTextFileSystem, IOutputTextFileSystem
{
    public CaseSensitivity CaseSensitivity() => sensitivity;
    private JsonNode? Call(string operation, ByteStringValue path) => connection.Call(operation, RawTransport.Path(path));
    public bool FileExists(RootedFilePath path) => Call("fs.fileExists", path.AsValue())!.GetValue<bool>();
    public bool DirectoryExists(RootedDirectoryPath path) => Call("fs.directoryExists", path.AsValue())!.GetValue<bool>();
    public (string Contents, bool Ok) ReadFile(RootedFilePath path)
    {
        var(contents, ok) = ReadSourceText(path);
        return (ByteStringValue.FromView(contents).Materialize()!, ok);
    }

    public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
    {
        var result = Call("fs.readFile", path.AsValue())!.AsObject();
        return (RawTransport.DecodeView(result["contents"]!), result["ok"]!.GetValue<bool>());
    }

    public Exception? WriteFile(RootedFilePath path, string data) => WriteOutputText(path, ByteStringValue.FromString(data));
    public Exception? WriteOutputText(RootedFilePath path, ByteStringValue data)
    {
        var args = RawTransport.Path(path.AsValue());
        args["contents"] = JsonValue.Create(RawTransport.Encode(data));
        var error = connection.Call("fs.writeFile", args);
        return error is null ? null : new RemoteHostException(RawTransport.DecodeValue(error));
    }

    public Exception? AppendFile(RootedFilePath path, ByteStringValue data)
    {
        var args = RawTransport.Path(path.AsValue());
        args["contents"] = JsonValue.Create(RawTransport.Encode(data));
        var error = connection.Call("fs.appendFile", args);
        return error is null ? null : new RemoteHostException(RawTransport.DecodeValue(error));
    }

    public Exception? Remove(RootedPath path)
    {
        var error = Call("fs.remove", path.AsValue());
        return error is null ? null : new RemoteHostException(RawTransport.DecodeValue(error));
    }

    public Exception? Chtimes(RootedPath path, DateTimeOffset accessTime, DateTimeOffset modificationTime)
    {
        var args = RawTransport.Path(path.AsValue());
        args["accessTicks"] = JsonValue.Create(accessTime.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks);
        args["modificationTicks"] = JsonValue.Create(modificationTime.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks);
        var error = connection.Call("fs.chtimes", args);
        return error is null ? null : new RemoteHostException(RawTransport.DecodeValue(error));
    }

    public RootedPath Realpath(RootedPath path) => RootedPath.FromValue(RawTransport.DecodeValue(Call("fs.realpath", path.AsValue())!));
    public Entries GetAccessibleEntries(RootedDirectoryPath path)
    {
        var result = Call("fs.entries", path.AsValue())!.AsObject();
        static Slice<ByteStringValue> Names(JsonNode? input)
        {
            if (input is null)
                return default;
            var array = input.AsArray();
            var names = Slice<ByteStringValue>.Make(array.Count);
            for (int i = 0; i < array.Count; i++)
                names[i] = RawTransport.DecodeValue(array[i]!);
            return names;
        }

        HashSet<ByteStringKey>? links = null;
        if (result["symlinks"] is JsonArray symlinks)
        {
            links = new();
            foreach (var item in symlinks)
                links.Add(ByteStringKey.FromValue(RawTransport.DecodeValue(item!)));
        }

        return new Entries
        {
            Files = Names(result["files"]),
            Directories = Names(result["directories"]),
            Symlinks = links
        };
    }

    public VfsFileInfo? Stat(RootedPath path)
    {
        var value = Call("fs.stat", path.AsValue());
        return value is null ? null : new RemoteFileInfo(value.AsObject());
    }

    private sealed class RemoteFileInfo(JsonObject value) : VfsFileInfo
    {
        public string Name() => RawTransport.Decode(value["name"]!);
        public long Size() => value["size"]!.GetValue<long>();
        public TypeScript.Vfs.FileMode Mode() => (TypeScript.Vfs.FileMode)value["mode"]!.GetValue<uint>();
        public DateTimeOffset ModTime() => DateTimeOffset.FromUnixTimeSeconds(value["seconds"]!.GetValue<long>()).AddTicks(value["nanoseconds"]!.GetValue<long>() / 100);
        public bool IsDir() => value["isDir"]!.GetValue<bool>();
        // Sys has no compiler/project caller; admission fails explicitly if that changes.
        public object? Sys() => throw new NotSupportedException("Opaque Go FileInfo.Sys is not a candidate host observation.");
    }
}
