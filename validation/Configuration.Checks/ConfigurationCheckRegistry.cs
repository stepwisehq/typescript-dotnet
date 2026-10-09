// Authored static registry shared by managed, xUnit inventory and native gates.
// Each group calls the same preserved assertion bodies; no runtime reflection.
namespace Configuration.Checks;

public static class ConfigurationCheckRegistry
{
    public sealed record Group(string Flag, string ClassName, IReadOnlyList<string> Cases, Action<string> Run);
    public static IReadOnlyList<Group> Groups { get; } =
    [
        new("--semver-checks", "Configuration.Tests.SemverTests", SemverChecks.Cases, SemverChecks.Run),
        new("--option-checks", "Configuration.Tests.CompilerOptionTests", CompilerOptionChecks.Cases, CompilerOptionChecks.Run),
        new("--package-checks", "Configuration.Tests.PackageJsonTests", PackageJsonChecks.Cases, PackageJsonChecks.Run),
        new("--command-checks", "Configuration.Tests.CommandLineTests", CommandLineChecks.Cases, CommandLineChecks.Run),
        new("--matcher-checks", "Configuration.Tests.MatcherTests", MatcherChecks.Cases, MatcherChecks.Run),
        new("--walker-checks", "Configuration.Tests.WalkDirTests", WalkDirChecks.Cases, WalkDirChecks.Run),
        new("--quote-checks", "Configuration.Tests.QuoteTests", QuoteChecks.Cases, QuoteChecks.Run),
        new("--parsing-helper-checks", "Configuration.Tests.ParsingHelperTests", ParsingHelperChecks.Cases, ParsingHelperChecks.Run),
        new("--content-mapper-checks", "Configuration.Tests.ContentMapperTests", ContentMapperChecks.Cases, ContentMapperChecks.Run),
        new("--locale-checks", "Configuration.Tests.LocaleTests", LocaleChecks.Cases, LocaleChecks.Run),
        new("--os-checks", "Configuration.Tests.OsFileSystemTests", OsFileSystemChecks.Cases, OsFileSystemChecks.Run),
        new("--parsed-command-checks", "Configuration.Tests.ParsedCommandLineTests", ParsedCommandLineChecks.Cases, ParsedCommandLineChecks.Run),
        new("--config-baseline-checks", "Configuration.Tests.ConfigBaselineTests", ConfigBaselineChecks.Cases, ConfigBaselineChecks.Run),
        new("--config-direct-checks", "Configuration.Tests.ConfigDirectTests", ConfigDirectChecks.Cases, ConfigDirectChecks.Run),
        new("--show-config-checks", "Configuration.Tests.ShowConfigTests", ShowConfigChecks.Cases, ShowConfigChecks.Run),
        new("--integration-checks", "Configuration.Tests.IntegrationTests", IntegrationChecks.Cases, IntegrationChecks.Run),
        new("--parsed-option-checks", "Configuration.Tests.ParsedOptionTests", ParsedOptionChecks.Cases, ParsedOptionChecks.Run),
        new("--output-path-checks", "Configuration.Tests.OutputPathTests", OutputPathChecks.Cases, OutputPathChecks.Run),
        new("--wildcard-directory-checks", "Configuration.Tests.WildcardDirectoryTests", WildcardDirectoryChecks.Cases, WildcardDirectoryChecks.Run),
        new("--config-mapper-checks", "Configuration.Tests.ConfigMapperTests", ConfigMapperChecks.Cases, ConfigMapperChecks.Run),
        new("--ordered-map-checks", "Configuration.Tests.OrderedMapTests", OrderedMapChecks.Cases, OrderedMapChecks.Run),
        new("--dependency-metadata-checks", "Configuration.Tests.DependencyMetadataTests", DependencyMetadataChecks.Cases, DependencyMetadataChecks.Run),
        new("--module-package-root-checks", "Configuration.Tests.ModulePackageRootTests", ModulePackageRootChecks.Cases, ModulePackageRootChecks.Run),
        new("--path-helper-checks", "Configuration.Tests.PathHelperTests", PathHelperChecks.Cases, PathHelperChecks.Run),
        new("--module-candidate-checks", "Configuration.Tests.ModuleCandidateTests", ModuleCandidateChecks.Cases, ModuleCandidateChecks.Run),
        new("--json-original-checks", "Configuration.Tests.JsonOriginalTests", JsonOriginalChecks.Cases, JsonOriginalChecks.Run),
        new("--path-fuzz-seed-checks", "Configuration.Tests.PathFuzzSeedTests", PathFuzzSeedChecks.Cases, PathFuzzSeedChecks.Run),
        new("--typed-path-checks", "Configuration.Tests.TypedPathTests", TypedPathChecks.Cases, TypedPathChecks.Run),
        new("--native-path-checks", "Configuration.Tests.NativePathTests", NativePathChecks.Cases, NativePathChecks.Run),
    ];
}
