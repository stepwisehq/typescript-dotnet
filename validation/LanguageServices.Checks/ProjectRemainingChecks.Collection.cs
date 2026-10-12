// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole internal/project/projectcollectionbuilder_test.go:19; thirteen original sibling groups.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private const string CollectionRoot = "/user/username/projects/myproject/";
    private static Input[] SolutionFiles(string[] references, string options, string[]? ownFiles = null)
    {
        string optionsText = options.Length == 0 ? "" : filesForSolutionConfigFileRaw0.Replace("%s", options, StringComparison.Ordinal);
        string referencesText = string.Join(",", references.Select(reference => filesForSolutionConfigFileRaw2.Replace("%s", reference, StringComparison.Ordinal)));
        string config = filesForSolutionConfigFileRaw1;
        foreach (var value in new[]
        {
            optionsText,
            string.Join(",", ownFiles ?? []),
            referencesText
        }

        )
        {
            int position = config.IndexOf("%s", StringComparison.Ordinal);
            config = config[..position] + value + config[(position + 2)..];
        }

        return[new Input(CollectionRoot + "tsconfig.json", config), new Input(CollectionRoot + "tsconfig-src.json", filesForSolutionConfigFileRaw3), new Input(CollectionRoot + "src/main.ts", filesForSolutionConfigFileRaw4), new Input(CollectionRoot + "src/helpers/functions.ts", filesForSolutionConfigFileRaw5)];
    }

    private static Input[] IndirectFiles(int index, string options) => [new Input(CollectionRoot + $"tsconfig-indirect{index}.json", filesForIndirectProjectRaw0.Replace("%s", options, StringComparison.Ordinal).Replace("%d", index.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)), new Input(CollectionRoot + $"indirect{index}/main.ts", filesForIndirectProjectRaw1)];
    private static void ConfigPresence(Snapshot snapshot, string path, bool expected) => Check((snapshot.ConfigFileRegistry.GetConfig(PathKey.FromGoString(path))is not null) == expected);
    private static Snapshot MoveToDummy(ProjectSessionFixture fixture, string path)
    {
        fixture.Session.DidCloseFile(ProtocolRequestContext.Default, Uri(path));
        Open(fixture, "/user/username/workspaces/dummy/dummy.ts", "const x = 1;");
        var snapshot = fixture.Session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == 1);
        Check(snapshot.ProjectCollection.InferredProject()is not null);
        return snapshot;
    }

    private static void SolutionCase(int kind)
    {
        bool indirect = kind is 1 or 3 or 4;
        string[] references = indirect ? kind == 3 ? ["./tsconfig-indirect1.json"] : ["./tsconfig-indirect1.json", "./tsconfig-indirect2.json"] : ["./tsconfig-src.json"];
        var files = new List<Input>(SolutionFiles(references, kind == 2 ? "\"disableReferencedProjectLoad\": true" : "", kind == 5 ? ["\"./own/main.ts\""] : null));
        if (indirect)
        {
            files.AddRange(IndirectFiles(1, kind is 3 or 4 ? "\"disableReferencedProjectLoad\": true" : ""));
            if (kind != 3)
                files.AddRange(IndirectFiles(2, ""));
        }

        if (kind == 5)
            files.Add(new Input(CollectionRoot + "own/main.ts", "\n\t\t\timport { foo } from '../src/main';\n\t\t\tfoo;\n\t\t\texport function bar() {}\n\t\t"));
        using var fixture = new ProjectSessionFixture(files);
        const string path = CollectionRoot + "src/main.ts";
        Open(fixture, path, filesForSolutionConfigFileRaw4);
        var snapshot = fixture.Session.Snapshot();
        Check(snapshot.ProjectCollection.Projects().Length == (kind == 5 ? 2 : 1));
        var sourceProject = snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString(CollectionRoot + "tsconfig-src.json"));
        Check((sourceProject is not null) == (kind is not (2 or 3)));
        if (kind == 0)
        {
            _ = Service(fixture, path);
            Check(ReferenceEquals(fixture.Session.Snapshot(), snapshot));
        }

        if (kind is 1 or 4 or 5)
            Check(ReferenceEquals(snapshot.GetDefaultProject(Uri(path)), sourceProject));
        if (kind is 2 or 3)
        {
            var defaultProject = snapshot.GetDefaultProject(Uri(path));
            Check(defaultProject is not null);
            Check(defaultProject!.Kind == ProjectKind.Inferred);
        }

        if (kind == 5)
            Check(snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString(CollectionRoot + "tsconfig.json"))is not null);
        ConfigPresence(snapshot, CollectionRoot + "tsconfig.json", true);
        ConfigPresence(snapshot, CollectionRoot + "tsconfig-src.json", kind is not (2 or 3));
        if (indirect)
            ConfigPresence(snapshot, CollectionRoot + "tsconfig-indirect1.json", true);
        if (kind == 4)
            ConfigPresence(snapshot, CollectionRoot + "tsconfig-indirect2.json", true);
        snapshot = MoveToDummy(fixture, path);
        ConfigPresence(snapshot, CollectionRoot + "tsconfig.json", false);
        ConfigPresence(snapshot, CollectionRoot + "tsconfig-src.json", false);
        if (indirect)
            ConfigPresence(snapshot, CollectionRoot + "tsconfig-indirect1.json", false);
        if (kind is 1 or 4)
            ConfigPresence(snapshot, CollectionRoot + "tsconfig-indirect2.json", false);
    }

    internal static void TestProjectCollectionBuilder()
    {
        Groups(("when project found is solution referencing default project directly", () => SolutionCase(0)), ("when project found is solution referencing default project indirectly", () => SolutionCase(1)), ("when project found is solution with disableReferencedProjectLoad referencing default project directly", () => SolutionCase(2)), ("when project found is solution referencing default project indirectly through disableReferencedProjectLoad", () => SolutionCase(3)), ("when project found is solution referencing default project indirectly through disableReferencedProjectLoad in one but without it in another", () => SolutionCase(4)), ("when project found is project with own files referencing the file from referenced project", () => SolutionCase(5)), ("when file is not part of first config tree found, looks into ancestor folder and its references to find default project", () =>
        {
            using var fixture = new ProjectSessionFixture(AncestorCollectionFiles);
            const string root = "/home/src/projects/project/";
            const string path = root + "app/Component-demos.ts";
            Open(fixture, path, AncestorCollectionFiles[0].Contents);
            var snapshot = fixture.Session.Snapshot();
            Check(snapshot.ProjectCollection.Projects().Length == 2);
            var demos = snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString(root + "demos/tsconfig.json"));
            Check(demos is not null);
            Check(snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString(root + "tsconfig.json"))is not null);
            Check(ReferenceEquals(snapshot.GetDefaultProject(Uri(path)), demos));
            foreach (var config in new[]
            {
                "app/tsconfig.json",
                "demos/tsconfig.json",
                "tsconfig.json"
            }

            )
                ConfigPresence(snapshot, root + config, true);
            snapshot = MoveToDummy(fixture, path);
            foreach (var config in new[]
            {
                "app/tsconfig.json",
                "demos/tsconfig.json",
                "tsconfig.json"
            }

            )
                ConfigPresence(snapshot, root + config, false);
        }), ("when dts file is next to ts file and included as root in referenced project", () =>
        {
            using var fixture = new ProjectSessionFixture(DtsCollectionFiles);
            const string root = "/home/src/projects/project/";
            const string path = root + "src/index.d.ts";
            Open(fixture, path, DtsCollectionFiles[0].Contents);
            var snapshot = fixture.Session.Snapshot();
            Check(snapshot.ProjectCollection.Projects().Length == 2);
            Check(snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString(root + "tsconfig.json"))is not null);
            var project = snapshot.GetDefaultProject(Uri(path));
            Check(project is not null);
            Check(project!.Kind == ProjectKind.Inferred);
            ConfigPresence(snapshot, root + "tsconfig.json", true);
            ConfigPresence(snapshot, root + "tsconfig.node.json", true);
            snapshot = MoveToDummy(fixture, path);
            ConfigPresence(snapshot, root + "tsconfig.json", false);
            ConfigPresence(snapshot, root + "tsconfig.node.json", false);
        }), ("#1630", () =>
        {
            using var fixture = new ProjectSessionFixture(Issue1630CollectionFiles);
            Open(fixture, "/project/lib/b.ts", Issue1630CollectionFiles[2].Contents);
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("untitled:Untitled-1")), 1, default, LanguageKind.TypeScript);
            Open(fixture, "/project/index.ts", "");
        }), ("inferred project root files are in stable order", () =>
        {
            using var fixture = new ProjectSessionFixture(StableRootsCollectionFiles);
            Open(fixture, "/project/b.ts", StableRootsCollectionFiles[1].Contents);
            Open(fixture, "/project/c.ts", StableRootsCollectionFiles[2].Contents);
            Open(fixture, "/project/a.ts", StableRootsCollectionFiles[0].Contents);
            var snapshot = fixture.Session.Snapshot();
            Check(snapshot.ProjectCollection.Projects().Length == 1);
            var project = snapshot.ProjectCollection.InferredProject();
            Check(project is not null);
            var files = project!.Program!.CommandLine().FileNames();
            Check(files is not null && files.Count == 3);
            for (int index = 0; index < 3; index++)
                Check(files![index] == RootedFilePath.FromGoString("/project/" + (char)('a' + index) + ".ts"));
        }), ("project lookup terminates", () =>
        {
            using var fixture = new ProjectSessionFixture(CycleCollectionFiles);
            Open(fixture, "/script.ts", CycleCollectionFiles[3].Contents);
        }), ("file moves to inferred project after import is deleted", () =>
        {
            using var fixture = new ProjectSessionFixture(MovedDependencyCollectionFiles);
            const string rootPath = "/project/index.ts";
            const string depPath = "/project/node_modules/dep/index.d.ts";
            Open(fixture, rootPath, MovedDependencyCollectionFiles[1].Contents);
            _ = Service(fixture, rootPath);
            Open(fixture, depPath, MovedDependencyCollectionFiles[2].Contents);
            var snapshot = fixture.Session.Snapshot();
            var configured = snapshot.ProjectCollection.ConfiguredProject(PathKey.FromGoString("/project/tsconfig.json"));
            Check(configured is not null);
            Check(ReferenceEquals(snapshot.GetDefaultProject(Uri(depPath)), configured));
            fixture.Session.DidChangeFile(ProtocolRequestContext.Default, Uri(rootPath), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("// import removed") } }]));
            var service = Service(fixture, depPath);
            Check(service is not null);
            var project = fixture.Session.Snapshot().GetDefaultProject(Uri(depPath));
            Check(project is not null);
            Check(project!.Kind == ProjectKind.Inferred);
        }), ("should update project on package.json change", () =>
        {
            using var fixture = new ProjectSessionFixture(PackageCollectionFiles);
            const string path = "/home/projects/myproject/src/index.ts";
            Open(fixture, path, PackageCollectionFiles[2].Contents);
            var program = Service(fixture, path).GetProgram();
            Check(program.GetSemanticDiagnostics(default(CancellationToken), null).Length == 0);
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/projects/myproject/package.json"), "{\n\t\t\t\"name\": \"myproject\",\n\t\t\t\"type\": \"module\",\n\t\t\t\"imports\": {\n\t\t\t\t\"#utils\": \"./src/nonexistent.ts\"\n\t\t\t}\n\t\t}")is null);
            WatchChange(fixture, "/home/projects/myproject/package.json", FileChangeType.Changed);
            var updated = Service(fixture, path).GetProgram();
            Check(updated.GetSemanticDiagnostics(default(CancellationToken), null).Length == 1);
        }));
    }
}
