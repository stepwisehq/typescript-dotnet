// Whole TestProjectReferencesProgram, all fourteen independent original siblings.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Project = TypeScript.ProjectSystem.Project;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private const string MainReference = "/user/username/projects/myproject/main/main.ts";
    private static Project OnlyConfigured(ProjectSessionFixture fixture)
    {
        var projects = fixture.Session.Snapshot().ProjectCollection.Projects();
        Check(projects.Length == 1);
        Check(projects[0].Kind == ProjectKind.Configured);
        return projects[0];
    }

    internal static void TestProjectReferencesProgram()
    {
        List<(string Name, Action Run)> groups = new();
        void Redirect(bool disabled)
        {
            var files = ReferencedFiles(disabled);
            if (disabled)
                files["/user/username/projects/myproject/decls/fns.d.ts"] = new("/user/username/projects/myproject/decls/fns.d.ts", "\n\t\t\texport declare function fn1(): void;\n\t\t\texport declare function fn2(): void;\n\t\t\texport declare function fn3(): void;\n\t\t\texport declare function fn4(): void;\n\t\t\texport declare function fn5(): void;\n\t\t");
            using var fixture = new ProjectSessionFixture(files.Values);
            Check(fixture.Session.Snapshot().ProjectCollection.Projects().Length == 0);
            Open(fixture, MainReference, files[MainReference].Contents);
            var project = OnlyConfigured(fixture);
            Check((project.Program!.GetSourceFileByPath(PathKey.FromGoString("/user/username/projects/myproject/dependency/fns.ts"))is null) == disabled);
            Check((project.Program.GetSourceFileByPath(PathKey.FromGoString("/user/username/projects/myproject/decls/fns.d.ts"))is not null) == disabled);
        }

        groups.Add(("program for referenced project", () => Redirect(false)));
        groups.Add(("program with disableSourceOfProjectReferenceRedirect", () => Redirect(true)));
        void Symlink(bool preserve, string scope, bool subfolder)
        {
            var(files, entry, foo, bar) = SymlinkReferenceFiles(preserve, scope, subfolder);
            using var fixture = new ProjectSessionFixture(files.Values);
            Check(fixture.Session.Snapshot().ProjectCollection.Projects().Length == 0);
            Open(fixture, entry, files[entry].Contents);
            var project = OnlyConfigured(fixture);
            Check(project.Program!.GetSourceFile(RootedFilePath.FromGoString(foo))is not null);
            Check(project.Program.GetSourceFile(RootedFilePath.FromGoString(bar))is not null);
        }

        groups.Add(("references through symlink with index and typings", () => Symlink(false, "", false)));
        groups.Add(("references through symlink with index and typings with preserveSymlinks", () => Symlink(true, "", false)));
        groups.Add(("references through symlink with index and typings scoped package", () => Symlink(false, "@issue/", false)));
        groups.Add(("references through symlink with index and typings with scoped package preserveSymlinks", () => Symlink(true, "@issue/", false)));
        groups.Add(("references through symlink referencing from subFolder", () => Symlink(false, "", true)));
        groups.Add(("references through symlink referencing from subFolder with preserveSymlinks", () => Symlink(true, "", true)));
        groups.Add(("references through symlink referencing from subFolder scoped package", () => Symlink(false, "@issue/", true)));
        groups.Add(("references through symlink referencing from subFolder with scoped package preserveSymlinks", () => Symlink(true, "@issue/", true)));
        void DirectorySubpath(string scope)
        {
            var(files, entry, file) = DirectorySubpathFiles(scope);
            using var fixture = new ProjectSessionFixture(files.Values);
            Open(fixture, entry, files[entry].Contents);
            var project = OnlyConfigured(fixture);
            Check(project.Program!.GetSourceFile(RootedFilePath.FromGoString(file))is not null);
            var request = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0"));
            Check(project.Program.GetSemanticDiagnostics(request, project.Program.GetSourceFile(RootedFilePath.FromGoString(entry))).Length == 0);
        }

        groups.Add(("references through symlink with directory index subpath (issue 4373)", () => DirectorySubpath("")));
        groups.Add(("references through symlink with directory index subpath scoped package", () => DirectorySubpath("@issue/")));
        groups.Add(("when new file is added to referenced project", () =>
        {
            var files = ReferencedFiles(false);
            using var fixture = new ProjectSessionFixture(files.Values);
            Open(fixture, MainReference, files[MainReference].Contents);
            var before = OnlyConfigured(fixture).Program;
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/user/username/projects/myproject/dependency/fns2.ts"), "export const x = 2;")is null);
            WatchChange(fixture, "/user/username/projects/myproject/dependency/fns2.ts", FileChangeType.Created);
            _ = Service(fixture, MainReference);
            Check(!ReferenceEquals(OnlyConfigured(fixture).Program, before));
        }));
        groups.Add(("dropped project reference does not crash on later change to the dropped config", () =>
        {
            using var fixture = new ProjectSessionFixture(DroppedReferenceFiles);
            const string main = "/user/username/projects/myproject/main/main.ts";
            const string other = "/user/username/projects/myproject/other/other.ts";
            Open(fixture, main, DroppedReferenceFiles.First(file => file.Path == main).Contents);
            fixture.Session.WaitForBackgroundTasks();
            Check(fixture.Session.Snapshot().ProjectCollection.Projects().Length == 1);
            Check(fixture.Session.Snapshot().ConfigFileRegistry.GetConfig(PathKey.FromGoString("/user/username/projects/myproject/dependency/tsconfig.json"))is not null);
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/user/username/projects/myproject/main/tsconfig.json"), "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true\n\t\t\t}\n\t\t}")is null);
            WatchChange(fixture, "/user/username/projects/myproject/main/tsconfig.json", FileChangeType.Changed);
            _ = Service(fixture, main);
            fixture.Session.DidCloseFile(ProtocolRequestContext.Default, Uri(main));
            Open(fixture, other, DroppedReferenceFiles.First(file => file.Path == other).Contents);
            fixture.Session.WaitForBackgroundTasks();
            Check(fixture.Session.Snapshot().ProjectCollection.ConfiguredProject(PathKey.FromGoString("/user/username/projects/myproject/main/tsconfig.json"))is null);
            Check(fixture.Session.Snapshot().ConfigFileRegistry.GetConfig(PathKey.FromGoString("/user/username/projects/myproject/dependency/tsconfig.json"))is null);
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/user/username/projects/myproject/dependency/tsconfig.json"), "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"composite\": true,\n\t\t\t\t\"strict\": true\n\t\t\t}\n\t\t}")is null);
            WatchChange(fixture, "/user/username/projects/myproject/dependency/tsconfig.json", FileChangeType.Changed);
            _ = Service(fixture, other);
            fixture.Session.WaitForBackgroundTasks();
        }));
        Groups(groups.ToArray());
    }
}
