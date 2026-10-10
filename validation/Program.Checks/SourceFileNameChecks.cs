using System.Text;
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using TypeScript.Compiler;

namespace Program.Checks;

// Authored filename-role/cache regressions, not upstream test-body ports.
// Go ast.SourceFile keeps raw filename bytes; Unicode is the existing .NET boundary.
public static class SourceFileNameChecks
{
    public static readonly string[] Names = ["default-empty", "scalar-names", "raw-names", "filename-mutation", "metadata-mutation", "role-transition", "promotion", "malformed-promotion", "clone-update", "content-mapping", "disk-project"];
    private static readonly (string Scalar, string Raw)[] NamesByRole =
    [
        ("/ascii.ts", "/ascii.ts"),
        ("/café.ts", "/caf\u00c3\u00a9.ts"),
        ("/😀.ts", "/\u00f0\u009f\u0098\u0080.ts")
    ];
    private static SourceFile Parse(string name, bool raw = false) => Parser.ParseSourceFile(
        new(name, "/stable-path", FileNameIsGoBytes: raw), "export const value = 1;", ScriptKind.TS);
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Equal(string? actual, string? expected) => Require(actual == expected, "Filename value differs.");
    private static string Cached(SourceFile file, string expected)
    {
        string bytes = file.FileNameGoBytes();
        Equal(bytes, expected);
        Require(ReferenceEquals(bytes, file.FileNameGoBytes()), "Repeated filename conversion allocated a different string.");
        return bytes;
    }
    public static void Run(string name)
    {
        switch (name)
        {
            case "default-empty":
                var empty = new NodeFactory().NewSourceFile(default, "", null!, null!).AsSourceFile();
                Require(empty.FileName() is null, "Managed default filename changed.");
                Cached(empty, "");
                foreach (bool raw in new[] { false, true }) Cached(Parse("", raw), "");
                break;
            case "scalar-names":
                foreach (var pair in NamesByRole)
                {
                    var file = Parse(pair.Scalar);
                    Cached(file, pair.Raw);
                    Equal(file.FileName(), pair.Scalar);
                    Require(!file.ParseOptions().FileNameIsGoBytes, "Scalar role changed.");
                }
                break;
            case "raw-names":
                foreach (var pair in NamesByRole.Append((Scalar: "/invalid\u00ff.ts", Raw: "/invalid\u00ff.ts")))
                {
                    var file = Parse(pair.Raw, true);
                    Require(ReferenceEquals(pair.Raw, Cached(file, pair.Raw)), "Raw string was copied.");
                    Equal(file.FileName(), pair.Raw);
                }
                break;
            case "filename-mutation":
                var renamed = Parse(NamesByRole[1].Scalar);
                string old = Cached(renamed, NamesByRole[1].Raw);
                renamed.parseOptions = renamed.ParseOptions() with { FileName = NamesByRole[2].Scalar };
                Cached(renamed, NamesByRole[2].Raw);
                string equalValue = new(NamesByRole[2].Scalar.ToCharArray());
                renamed.parseOptions = renamed.ParseOptions() with { FileName = equalValue };
                Cached(renamed, NamesByRole[2].Raw);
                Equal(old, NamesByRole[1].Raw);
                break;
            case "metadata-mutation":
                var metadata = Parse(NamesByRole[1].Scalar);
                string before = Cached(metadata, NamesByRole[1].Raw);
                metadata.parseOptions = metadata.ParseOptions() with { PathKey = "/changed-key", ExternalModuleIndicatorOptions = new(Force: true) };
                Require(ReferenceEquals(before, Cached(metadata, NamesByRole[1].Raw)), "Unrelated metadata invalidated filename bytes.");
                Equal(metadata.PathKey(), "/changed-key");
                Require(metadata.ParseOptions().ExternalModuleIndicatorOptions.Force, "Metadata mutation lost.");
                break;
            case "role-transition":
                var role = Parse(NamesByRole[1].Scalar);
                Cached(role, NamesByRole[1].Raw);
                role.parseOptions = role.ParseOptions() with { FileNameIsGoBytes = true };
                Require(ReferenceEquals(role.FileName(), Cached(role, NamesByRole[1].Scalar)), "Raw role reused encoded scalar bytes.");
                role.parseOptions = role.ParseOptions() with { FileNameIsGoBytes = false };
                Cached(role, NamesByRole[1].Raw);
                break;
            case "promotion":
                foreach (var pair in NamesByRole)
                {
                    var file = Parse(pair.Raw, true);
                    file.SetFileNameToUnicode();
                    Equal(file.FileName(), pair.Scalar);
                    Equal(file.ParseOptions().FileName, pair.Scalar);
                    Require(!file.ParseOptions().FileNameIsGoBytes, "Promotion left raw public role.");
                    Require(ReferenceEquals(pair.Raw, Cached(file, pair.Raw)), "Promotion lost original byte string.");
                    Equal(file.PathKey(), "/stable-path");
                    file.SetFileNameToUnicode();
                    Require(ReferenceEquals(pair.Raw, Cached(file, pair.Raw)), "Repeated promotion lost original bytes.");
                }
                break;
            case "malformed-promotion":
                foreach (string raw in new[] { "/x\u00ff.ts", "/x\u00ed\u00a0\u0080.ts" })
                {
                    var file = Parse(raw, true);
                    var original = file.ParseOptions();
                    try { file.SetFileNameToUnicode(); throw new InvalidOperationException("Malformed filename promotion succeeded."); }
                    catch (DecoderFallbackException) { }
                    Require(file.ParseOptions() == original, "Failed promotion mutated options.");
                    Require(ReferenceEquals(raw, Cached(file, raw)), "Failed promotion changed raw bytes.");
                    var clone = file.AsNode().Clone(new NodeFactory()).AsSourceFile();
                    Require(ReferenceEquals(raw, Cached(clone, raw)), "Raw clone lost malformed bytes.");
                }
                break;
            case "clone-update":
                var parent = Parse(NamesByRole[1].Raw, true);
                parent.SetFileNameToUnicode();
                string originalBytes = Cached(parent, NamesByRole[1].Raw);
                var factory = new NodeFactory();
                var cloned = parent.AsNode().Clone(factory).AsSourceFile();
                Cached(cloned, NamesByRole[1].Raw);
                var updated = factory.UpdateSourceFile(parent, factory.NewNodeList(new(parent.Statements.Nodes)), parent.EndOfFileToken).AsSourceFile();
                Require(!ReferenceEquals(updated, parent), "Update did not create a source file.");
                Cached(updated, NamesByRole[1].Raw);
                cloned.parseOptions = cloned.ParseOptions() with { FileName = NamesByRole[2].Scalar };
                Cached(cloned, NamesByRole[2].Raw);
                Require(ReferenceEquals(originalBytes, Cached(parent, NamesByRole[1].Raw)), "Clone mutation changed parent cache.");
                Cached(updated, NamesByRole[1].Raw);
                break;
            case "content-mapping":
                string source = "/caf\u00c3\u00a9.astro";
                var (mapped, error) = ContentMappers.ParseResult(new(source, source, FileNameIsGoBytes: true), "", new() { Name = "fixture" }, "identity", new()
                {
                    Text = "export {};", VirtualExtension = ".ts", Mappings = SpanMaps.New(null!),
                    Supplemental = [new() { Text = "export {};", VirtualExtension = ".mts", Mappings = SpanMaps.New(null!) }]
                });
                Require(error is null && mapped.Canonical is not null && mapped.Supplemental?.Count == 1, "Mapper setup failed.");
                foreach (var file in new[] { mapped.Canonical!, mapped.Supplemental![0] })
                {
                    string raw = file.FileNameGoBytes(), path = file.PathKey(), virtualName = file.VirtualFileName().AsString();
                    file.SetFileNameToUnicode();
                    Require(ReferenceEquals(raw, Cached(file, raw)), "Mapped filename bytes changed.");
                    Equal(file.PathKey(), path);
                    Equal(file.VirtualFileName().AsString(), virtualName);
                    Equal(file.OriginalFileName().AsString(), source);
                }
                break;
            case "disk-project":
                DiskProject();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
    private static void DiskProject()
    {
        string directory = Path.Combine(Path.GetTempPath(), "filename-role-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string[] filenames = ["ascii.ts", "café.ts", "😀.ts"];
            foreach (string filename in filenames) File.WriteAllText(Path.Combine(directory, filename), "export const value: number = \"bad\";\n");
            string config = Path.Combine(directory, "tsconfig.json");
            File.WriteAllText(config, "{\"compilerOptions\":{\"strict\":true,\"noEmit\":true,\"lib\":[\"es5\"],\"types\":[]},\"files\":[\"ascii.ts\",\"café.ts\",\"😀.ts\"]}");
            var project = Project.LoadConfiguration(config);
            Require(project.UserFiles.Count == 3 && project.RootFiles.Count == 3, "Disk project roots differ.");
            foreach (string filename in filenames)
            {
                string scalar = Path.Combine(directory, filename);
                var file = project.UserFiles.Single(f => f.FileName() == scalar);
                Require(!file.ParseOptions().FileNameIsGoBytes, "Disk public filename role changed.");
                string raw = Cached(file, GoString.ToUtf8(scalar));
                Equal(file.ParseOptions().FileName, scalar);
                Require(ReferenceEquals(raw, file.FileNameGoBytes()), "Disk raw filename was not retained.");
            }
            var diagnostics = project.Check();
            Require(diagnostics.Count == 3 && diagnostics.All(d => d.Code() == 2322), "Disk semantic errors differ.");
            Require(diagnostics.Select(d => d.File().FileName()).ToHashSet(StringComparer.Ordinal).SetEquals(filenames.Select(n => Path.Combine(directory, n))), "Disk diagnostic owners differ.");
        }
        finally { Directory.Delete(directory, true); }
    }
    public static int RunAll()
    {
        foreach (string name in Names) Run(name);
        return Names.Length;
    }
}
