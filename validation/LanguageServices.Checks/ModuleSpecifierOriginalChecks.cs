// Whole original modulespecifiers/specifiers_test.go assertions, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Symlinks;
using TypeScript.TsOptions;
using TypeScript.ModuleSpecifiers;
using Functions = TypeScript.ModuleSpecifiers.ModuleSpecifiers;

namespace LanguageServices.Checks;
public static class ModuleSpecifierOriginalChecks
{
    private static void Require([DoesNotReturnIf(false)] bool condition, string message = "original module-specifier assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static RootedFilePath File(string value) => RootedFilePath.FromGoString(value);
    private static RootedDirectoryPath Directory(string value) => RootedDirectoryPath.FromGoString(value);
    private sealed class Host : IModuleSpecifierGenerationHost
    {
        internal RootedDirectoryPath CurrentDirectory = default;
        internal CaseSensitivity Sensitivity = default;
        internal KnownSymlinks? Symlinks = null;
        internal Dictionary<RootedFilePath, bool>? ExistingFiles = null;
        internal List<RootedFilePath> FileExistsCalls = new();
        public RootedDirectoryPath BaseDirectory() => CurrentDirectory;
        public CaseSensitivity CaseSensitivity() => Sensitivity;
        public KnownSymlinks GetSymlinkCache() => Symlinks!;
        public RootedDirectoryPath GetGlobalTypingsCacheLocation() => default;
        public RootedDirectoryPath CommonSourceDirectory() => CurrentDirectory;
        public List<string>? ContentMapperExtensions() => null;
        public SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => null;
        public Slice<RootedFilePath> GetRedirectTargets(PathKey path) => default;
        public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file) => RootedFilePath.FromGoString(file.FileName());
        public bool FileExists(RootedFilePath path)
        {
            FileExistsCalls.Add(path);
            return ExistingFiles is null || ExistingFiles.TryGetValue(path, out var exists) && exists;
        }

        public RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory) => default;
        public InfoCacheEntry? GetPackageJsonInfo(RootedFilePath path) => null;
        public ResolutionMode GetDefaultResolutionModeForFile(HasFileName file) => ResolutionMode.None;
        public ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node specifier) => null;
        public ResolutionMode GetModeForUsageLocation(HasFileName file, Node specifier) => ResolutionMode.None;
    }

    public static void TestGetNodeModulePathParts()
    {
        (string Path, string SearchRoot, string NodeModules, string PackageRoot, string Package, string Relative, bool Nested)[] rows = [("/workspace/node_modules/pkg/lib/index.d.ts", "/workspace", "/workspace/node_modules", "/workspace/node_modules/pkg", "pkg", "lib/index.d.ts", false), ("/node_modules/@scope/pkg/index.d.ts", "/", "/node_modules", "/node_modules/@scope/pkg", "@scope/pkg", "index.d.ts", false), ("c:/node_modules/pkg/index.d.ts", "c:/", "c:/node_modules", "c:/node_modules/pkg", "pkg", "index.d.ts", false), ("/workspace/node_modules/pkg/node_modules/@scope/dep/index.d.ts", "/workspace", "/workspace/node_modules", "/workspace/node_modules/pkg/node_modules/@scope/dep", "@scope/dep", "index.d.ts", true)];
        var actions = new List<Action>();
        foreach (var row in rows)
            actions.Add(() =>
            {
                var parts = Functions.GetNodeModulePathParts(File(row.Path));
                Require(parts is not null);
                Require(parts.TopLevelNodeModulesSearchRoot == Directory(row.SearchRoot) && parts.TopLevelNodeModulesDirectory == Directory(row.NodeModules) && parts.PackageRootDirectory == Directory(row.PackageRoot) && parts.PackageName.EqualsString(row.Package) && parts.PackageRelativePath.AsValue().EqualsString(row.Relative) && parts.HasNestedNodeModules == row.Nested && !parts.IsDirectNodeModulesFile);
            });
        actions.Add(() => Require(Functions.GetNodeModulePathParts(File("/workspace/src/index.ts"))is null));
        actions.Add(() =>
        {
            var direct = Functions.GetNodeModulePathParts(File("/workspace/node_modules/pkg"));
            Require(direct is not null && direct.IsDirectNodeModulesFile && direct.PackageName.EqualsString("pkg"));
        });
        actions.Add(() =>
        {
            var scoped = Functions.GetNodeModulePathParts(File("/workspace/node_modules/@scope"));
            Require(scoped is not null && scoped.IsDirectNodeModulesFile && scoped.PackageName.EqualsString("@scope"));
        });
        RunRows(actions, action => action());
    }

    public static void TestGetEachFileNameOfModule()
    {
        (string Name, string Imported, bool Prefer, string[]? Expected)[] rows = [("basic file path", "/project/lib/utils.ts", false, ["/project/lib/utils.ts"]), ("symlink preference false", "/project/lib/utils.ts", false, null), ("symlink preference true", "/project/lib/utils.ts", true, null), ("ignored path with no alternatives", "/project/node_modules/.pnpm/file.ts", false, null)];
        RunRows(rows, row =>
        {
            var host = new Host
            {
                CurrentDirectory = Directory("/project"),
                Sensitivity = CaseSensitivity.CaseSensitive,
                Symlinks = KnownSymlinks.NewKnownSymlinks(CaseSensitivity.CaseSensitive)
            };
            var result = Functions.GetEachFileNameOfModule(File("/project/src/main.ts"), File(row.Imported), host, row.Prefer);
            Require(result.Length == 1, row.Name);
            if (row.Expected is not null)
                for (int i = 0; i < row.Expected.Length; i++)
                    Require(i < result.Length && result[i].FileName.AsValue().EqualsString(row.Expected[i]), row.Name);
            foreach (var path in result)
                Require(path.FileName.AsValue().Length != 0, row.Name);
        });
    }

    public static void TestGetEachFileNameOfModuleWithSymlinks()
    {
        var host = new Host
        {
            CurrentDirectory = Directory("/project"),
            Sensitivity = CaseSensitivity.CaseSensitive,
            Symlinks = KnownSymlinks.NewKnownSymlinks(CaseSensitivity.CaseSensitive)
        };
        var link = Directory("/project/symlink");
        var real = Directory("/real/path");
        host.Symlinks!.SetDirectory(link, host.Sensitivity.PathKey(link.AsPath()), new KnownDirectoryLink { Real = real, RealPath = host.Sensitivity.PathKey(real.AsPath()) });
        var result = Functions.GetEachFileNameOfModule(File("/project/src/main.ts"), File("/real/path/file.ts"), host, true);
        bool found = false;
        foreach (var path in result)
            if (path.FileName.AsValue().EqualsString("/project/symlink/file.ts"))
            {
                found = true;
                break;
            }

        Require(found);
    }

    public static void TestModuleSpecifierContainsNodeModules()
    {
        (string Path, bool Expected)[] rows = [("/project/node_modules/lodash/index.js", true), ("/project/src/utils.ts", false), ("/project/packages/node_modules/pkg/file.js", true), ("", false)];
        RunRows(rows, row => Require(Functions.moduleSpecifierContainsNodeModules(TsPath.ToModuleSpecifier(row.Path)) == row.Expected));
    }

    public static void TestIsPackageMainFilePreservesDirectoryIntent()
    {
        var root = Directory("/project/node_modules/pkg");
        Require(!Functions.isPackageMainFile(File("/project/node_modules/pkg/types.d.ts"), root, "./types/", "", CaseSensitivity.CaseSensitive));
        Require(Functions.isPackageMainFile(File("/project/node_modules/pkg/types/index.d.ts"), root, "./types/", "", CaseSensitivity.CaseSensitive));
        Require(Functions.isPackageMainFile(File("/project/node_modules/pkg/types.d.ts"), root, "./types", "", CaseSensitivity.CaseSensitive));
    }

    public static void TestContainsIgnoredPath()
    {
        RunRows(new (string Path, bool Expected)[] { ("/project/node_modules/.pnpm/file.ts", true), ("/project/src/file.ts", false) }, row => Require(Functions.containsIgnoredPath(File(row.Path)) == row.Expected));
    }

    public static void TestTryGetRealFileNameForNonJSDeclarationFileName()
    {
        (string Input, string Expected)[] rows = [("/project/foo.d.json.ts", "/project/foo.json"), ("/project/foo.module.d.css.ts", "/project/foo.module.css"), ("/project/foo.d.ts", "")];
        RunRows(rows, row => Require(Functions.TryGetRealFileNameForNonJSDeclarationFileName(row.Input) == row.Expected));
    }

    public static void TestProcessEndingChecksRootedFilePath()
    {
        var host = new Host
        {
            CurrentDirectory = Directory("/wrong"),
            Sensitivity = CaseSensitivity.CaseSensitive,
            ExistingFiles = new()
            {
                [File("/project/src/lib.ts")] = true
            }
        };
        var result = Functions.processEnding(TsPath.ToModuleSpecifier("./lib/index.ts"), File("/project/src/lib/index.ts"), Slice<ModuleSpecifierEnding>.FromArray([ModuleSpecifierEnding.Minimal]), new CompilerOptions(), host);
        Require(result.AsValue().EqualsString("./lib/index"));
        Require(host.FileExistsCalls.Contains(File("/project/src/lib.ts")));
    }

    public static void TestIsPathRelativeToParent()
    {
        (string Input, bool Expected)[] rows = [("..", true), ("../sibling.ts", true), ("..foo.ts", false), ("child/..foo.ts", false)];
        RunRows(rows, row => Require(Functions.isPathRelativeToParent(RelativePath.FromGoString(row.Input)) == row.Expected));
    }

    public static void TestTryGetModuleNameFromExportsOrImports()
    {
        RunRows(new Action[] { () =>
        {
            var exports = new ExportsOrImports
            {
                Type = JSONValueType.String,
                Value = "./dist/internal/"
            };
            var host = new Host
            {
                Sensitivity = CaseSensitivity.CaseSensitive
            };
            Require(Functions.tryGetModuleNameFromExportsOrImports(new CompilerOptions(), host, File("/pkg/dist/internal/file.ts"), Directory("/pkg"), ByteStringValue.FromString("#internal/"), exports, default, MatchingMode.Directory, true, false).Length != 0);
            Require(Functions.tryGetModuleNameFromExportsOrImports(new CompilerOptions(), host, File("/pkg/dist/internal/file.ts"), Directory("/pkg"), ByteStringValue.FromString("#internal"), exports, default, MatchingMode.Exact, true, false).Length == 0);
        }, () =>
        {
            (string Path, string Expected)[] rows = [("/pkg/src/things/thing1/index.ts", "./src/things/thing1"), ("/pkg/src/things/index.ts", "")];
            RunRows(rows, row => Require(Functions.tryGetModuleNameFromExportsOrImports(new CompilerOptions(), new Host(), File(row.Path), Directory("/pkg"), ByteStringValue.FromString("./src/things/*"), new ExportsOrImports { Type = JSONValueType.String, Value = "./src/things/*/index.js" }, Slice<string>.Make(0), MatchingMode.Pattern, false, false).EqualsString(row.Expected)));
        } }, action => action());
    }

    private static void RunRows<T>(IEnumerable<T> rows, Action<T> run)
    {
        List<Exception>? failures = null;
        foreach (var row in rows)
            try
            {
                run(row);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }

        if (failures is not null)
            throw new AggregateException(failures);
    }

    public static int RunAll()
    {
        (string Name, Action Run)[] tests = [(nameof(TestGetNodeModulePathParts), TestGetNodeModulePathParts), (nameof(TestGetEachFileNameOfModule), TestGetEachFileNameOfModule), (nameof(TestGetEachFileNameOfModuleWithSymlinks), TestGetEachFileNameOfModuleWithSymlinks), (nameof(TestModuleSpecifierContainsNodeModules), TestModuleSpecifierContainsNodeModules), (nameof(TestIsPackageMainFilePreservesDirectoryIntent), TestIsPackageMainFilePreservesDirectoryIntent), (nameof(TestContainsIgnoredPath), TestContainsIgnoredPath), (nameof(TestTryGetRealFileNameForNonJSDeclarationFileName), TestTryGetRealFileNameForNonJSDeclarationFileName), (nameof(TestProcessEndingChecksRootedFilePath), TestProcessEndingChecksRootedFilePath), (nameof(TestIsPathRelativeToParent), TestIsPathRelativeToParent), (nameof(TestTryGetModuleNameFromExportsOrImports), TestTryGetModuleNameFromExportsOrImports), ];
        List<Exception>? failures = null;
        foreach (var test in tests)
            try
            {
                test.Run();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(test.Name, error));
            }

        if (failures is not null)
            throw new AggregateException(failures);
        return tests.Length;
    }

    public static void RunOriginal(int index)
    {
        switch (index)
        {
            case 0:
                TestGetNodeModulePathParts();
                break;
            case 1:
                TestGetEachFileNameOfModule();
                break;
            case 2:
                TestGetEachFileNameOfModuleWithSymlinks();
                break;
            case 3:
                TestModuleSpecifierContainsNodeModules();
                break;
            case 4:
                TestIsPackageMainFilePreservesDirectoryIntent();
                break;
            case 5:
                TestContainsIgnoredPath();
                break;
            case 6:
                TestTryGetRealFileNameForNonJSDeclarationFileName();
                break;
            case 7:
                TestProcessEndingChecksRootedFilePath();
                break;
            case 8:
                TestIsPathRelativeToParent();
                break;
            case 9:
                TestTryGetModuleNameFromExportsOrImports();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
