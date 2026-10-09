"""Checked function-level test provenance inventory, separate from implementation."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
BASE = ROOT / 'upstream/TypeScript/tsc/internal'
COMPLETE = ['tsoptions', 'vfs/vfsmatch', 'packagejson', 'semver', 'outputpaths']
REVIEW = ['tspath', 'vfs', 'vfs/vfstest', 'vfs/osvfs', 'vfs/vfsmock', 'vfs/iovfs', 'vfs/cachedvfs', 'fswatch', 'glob', 'locale', 'diagnosticwriter']
rows = []
for package in COMPLETE + REVIEW:
    directory = BASE / package
    if not directory.exists():
        continue
    for file in sorted(directory.glob('*_test.go')):
        source = file.read_text()
        digest = hashlib.sha256(file.read_bytes()).hexdigest()
        for match in re.finditer(r'^func (Test\w+|Fuzz\w+|Benchmark\w+)\(', source, re.M):
            name = match.group(1)
            if name == 'TestMain':
                continue
            kind = 'benchmark' if name.startswith('Benchmark') else 'fuzz' if name.startswith('Fuzz') else 'unit'
            selected = package in COMPLETE or (package == 'vfs' and file.name == 'walkdir_test.go')
            disposition = 'performance-excluded' if kind == 'benchmark' else 'fuzz-domain-review-pending' if kind == 'fuzz' else 'preserve-all-original-assertions-planned' if selected else 'operation-and-retained-assertion-review-pending'
            row = {'package': './internal/' + package, 'name': name, 'kind': kind, 'source': str(file.relative_to(ROOT)), 'sourceSha256': digest, 'line': source[:match.start()].count('\n') + 1, 'disposition': disposition, 'managed': 'not-run', 'native': 'not-run'}
            if selected and kind == 'unit':
                row['originalGo'] = 'passed-original-only-replay' if package != 'outputpaths' else 'not-run'
            if kind == 'unit' and (package in ['semver', 'packagejson'] or package == 'tsoptions' and file.name == 'compileroptions_test.go'):
                family = 'semver' if package == 'semver' else 'package' if package == 'packagejson' else 'option'
                row['disposition'] = 'assertion-preserving-port'
                row['shared'] = 'validation/Configuration.Checks/' + {'semver': 'SemverChecks.Generated.cs', 'package': 'PackageJsonChecks.cs', 'option': 'CompilerOptionChecks.cs'}[family]
                row['mechanics'] = {'semver': 'All literal rows and helper assertions retained; sequential shared xUnit/native dispatch replaces parallel subtests.', 'package': 'All assertions retained; synthetic anonymous Go JSON structs use explicit static field decoding over the real raw/typed wrapper APIs.', 'option': 'Full 132 original-field reflection domain independently generated as static AOT fields, with all parse/clone/equality and configDir assertions.'}[family]
                row['managed'] = 'passed-isolated-central3-shared-dispatch'
                row['managedEvidence'] = 'artifacts/configuration-validation/build3/package-checks-final.json' if family == 'package' else 'artifacts/configuration-validation/build3/shared-report.json'
            if kind == 'unit' and (package == 'tsoptions' and name in ['TestRemovedWatchOptions', 'TestResponseFileDoesNotPanic', 'TestResponseFileParsing', 'TestParseCommandLineTypeRootsRelativePath'] or package == 'vfs/vfsmatch' or package == 'vfs' and file.name == 'walkdir_test.go'):
                family = 'command' if package == 'tsoptions' else 'matcher' if package == 'vfs/vfsmatch' else 'walker'
                row['disposition'] = 'assertion-preserving-port'
                row['shared'] = 'validation/Configuration.Checks/' + {'command': 'CommandLineChecks.cs', 'matcher': 'MatcherChecks.cs', 'walker': 'WalkDirChecks.cs'}[family]
                row['mechanics'] = {'command': 'Complete original bodies and seven unchanged diagnostic baseline streams; actual source wrappers and finite read fixture supply original setup.', 'matcher': 'All 110 source literal rows and eight explicit bodies retain every assertion; dynamic case sensitivity and wrapvfs use a finite actual read-side host adapter.', 'walker': 'All eight original bodies retain paths, modes, link metadata, realpath calls, sentinels and error assertions; sentinel identity is the selected ErrorIs adaptation.'}[family]
                row['managed'] = 'passed-isolated-central4-shared-dispatch' if family == 'command' else 'passed-isolated-central5b-shared-dispatch'
                row['managedEvidence'] = 'artifacts/configuration-validation/build4/command-checks.json' if family == 'command' else 'artifacts/configuration-validation/build5b/matcher-walker-dispatcher.json'
            executed_family = None
            if kind == 'unit' and package == 'tsoptions':
                if file.name == 'commandlineparser_test.go': executed_family = ('command', 'CommandLineChecks.cs', 8)
                elif file.name in ['parsinghelpers_test.go', 'decls_test.go', 'rawcompileroptions_test.go']: executed_family = ('parsing-helper', 'ParsingHelperChecks.cs', 6)
                elif file.name == 'contentmappers_test.go': executed_family = ('content-mapper', 'ContentMapperChecks.cs', 4)
                elif file.name == 'showconfig_test.go': executed_family = ('show-config', 'ShowConfigChecks.cs', 2)
                elif file.name == 'parsedoptions_test.go': executed_family = ('parsed-option', 'ParsedOptionChecks.cs', 3)
                elif file.name == 'wildcarddirectories_test.go': executed_family = ('wildcard-directory', 'WildcardDirectoryChecks.cs', 3)
                elif name in ['TestContentMappers', 'TestContentMapperOptionDiagnosticLocation', 'TestContentMappersAreInheritedFromExtendedConfig', 'TestContentMappersRequireFlag', 'TestUnresolvedContentMapperDoesNotRegisterExtensions', 'TestContentMappersValidation', 'TestContentMapperExtensionValidationUsesHostCaseSensitivity']: executed_family = ('config-mapper', 'ConfigMapperChecks.cs', 7)
                elif file.name == 'parsedcommandline_test.go': executed_family = ('parsed-command', 'ParsedCommandLineChecks.cs', 1)
                elif name in ['TestParseConfigFileTextToJson', 'TestParseJsonConfigFileContent', 'TestParseJsonSourceFileConfigFileContent', 'TestParseTypeAcquisition']: executed_family = ('config-baseline', 'ConfigBaselineChecks.cs', 4)
                elif name in ['TestParseJsonConfigFileContentAcceptsJsonRepresentations', 'TestParseJsonConfigFileContentPreservesRaw', 'TestParseJsonConfigFileContentHandlesNullArrayElements', 'TestParseJsonConfigFileContentDefaultsCompileOnSaveToFalse', 'TestParseJsonSourceFileConfigFileContentReportsInvalidExtendedConfig', 'TestParseJsonSourceFileConfigFileContentWithEmptyExtendedConfig', 'TestParseJsonSourceFileConfigFileContentDoesNotDuplicateUnquotedKeyDiagnostics', 'TestParseJsonSourceFileConfigFileContentReportsQuestionTokenDiagnostics', 'TestParseNullEnumCompilerOptions', 'TestExtendedConfigErrorsAppearOnCacheHit', 'TestExtendedConfigConfigDirPathsAreNotCached', 'TestParseSrcCompiler']: executed_family = ('config-direct', 'ConfigDirectChecks.cs', 12)
            if kind == 'unit' and package == 'outputpaths': executed_family = ('output-path', 'OutputPathChecks.cs', 5)
            if kind == 'unit' and package == 'vfs/osvfs' and name in ['TestOS', 'TestSymlinkRealpath', 'TestGetAccessibleEntries']:
                executed_family = ('os', 'OsFileSystemChecks.cs', 3)
            if executed_family:
                family, body, count = executed_family
                row['disposition'] = 'assertion-preserving-port'
                row['shared'] = 'validation/Configuration.Checks/' + body
                row['managed'] = 'passed-isolated-shared-dispatch'
                row['managedEvidence'] = 'artifacts/configuration-validation/build5b/parsing-helper-checks/report.json' if family == 'parsing-helper' else 'artifacts/configuration-validation/' + ('build9/' if family in ['config-baseline', 'config-direct', 'show-config', 'config-mapper', 'parsed-option', 'output-path', 'wildcard-directory'] else 'build8/' if family == 'parsed-command' else 'build7/') + family + '-shared/report.json'
                row['mechanics'] = {
                    'command': 'All eight original bodies, 63 scenario rows and unchanged original paired baseline bytes; static typed decoding replaces Go test reflection. Sequential scheduling and finite source fixture host only.',
                    'parsing-helper': 'All merge, explicit-null/self-merge, every 132-field parse/declaration and original raw finalized-path assertions retained; static source-generated field domain replaces reflection.',
                    'content-mapper': 'All actual receiver-identity/output-list and seven source manifest branch assertions retained; no external process execution.',
                    'show-config': 'All132 original fields/types/enum/default/path domains and source-full-tag guards, with actual serializer output tested against independent static expectations.',
                    'parsed-option': 'All original complete field mutation/equality and nil/empty identity distinctions retained.',
                    'output-path': 'All original output metadata assertions over typed rooted input lists retained.',
                    'wildcard-directory': 'All original dot/drive/Unicode wildcard metadata rows retained.',
                    'config-mapper': 'All seven original mapper parsing bodies, both JSON/source APIs,9 validation rows/4 host-case rows, exact range slice, inheritance/manifest/discovery and malformed registration assertions retained.',
                    'config-baseline': 'All 33 source config scenarios,7 text groups,8 acquisition rows and89 unchanged pinned baseline files across complete text/JSON/source API bodies; full raw bytes compared.',
                    'config-direct': 'All original JSON representation/null/CompileOnSave, inherited token arguments/positions/owners, UTF16 positions and both extended-cache error/path isolation assertion setups retained.',
                    'parsed-command': 'Complete original literal/include/reload/canonical keys, content mapper matching and WithFileNames config identity assertions retained.',
                    'os': 'All original Linux read-side assertions over real OS fixtures; Windows mklink/junction fixture setup not executed, no mapped write API claim.'
                }[family]
            if kind == 'unit' and package in ['outputpaths', 'vfs/osvfs', 'tspath', 'vfs/vfstest']:
                row['originalGo'] = 'passed-original-only-dependency-replay'
            if package == 'fswatch':
                row['disposition'] = 'watch-execution-excluded'
                row['reason'] = 'Separate OS watcher/walker implementation is not the selected VFS WalkDir API or read-side FS.'
            if package == 'vfs/cachedvfs':
                row['disposition'] = 'cache-mutation-framework-excluded'
                row['reason'] = 'Selected extends cache is TsOptions metadata; cachedvfs write/invalidation/watch framework is outside the read-side host.'
            rows.append(row)
# Close dependency applicability without claiming retained or partial bodies as new main110.
retained = json.loads((ROOT / 'docs/projects-support-tests.json').read_text())
retained_paths = {r['name']: r for file in retained['files'] if 'tspath/path_test.go' in file['path'] for r in file['testFunctions'] if r['disposition'] == 'assertion-preserving-port'}
typed = {r['name']: r for r in json.loads((ROOT / 'validation/Configuration.Checks/typed-path-test-provenance.json').read_text())['entries']}
for row in rows:
    name = row['name']
    if row['package'] == './internal/tspath' and row['kind'] != 'benchmark':
        if name in retained_paths:
            row.update(disposition='retained-assertion-preserving-port', executionScope='retained-projects-foundation-cascade', dependencyInventory='docs/projects-support-tests.json', reason='Complete unchanged scalar assertion body is retained; no new typed-wrapper proof inferred.', managed='prior-accepted-source; current-source-retained-cascade-pending', native='current-source-retained-cascade-pending')
        elif name in typed:
            row.update(disposition='dependency-' + typed[name]['disposition'], dependencyInventory='docs/progress/configuration-path-tests.json', shared='validation/Configuration.Checks/TypedPathChecks.cs', managed='passed-copied11b-full33', managedEvidence='artifacts/configuration-validation/build11b/typed-path-shared/report.json', reason=typed[name]['assertions'])
        elif name in ['TestGetLongestExtensionFromPath', 'TestRemoveAnyFileExtension', 'TestCaseSensitivityTrimPrefix']:
            row.update(disposition='dependency-assertion-preserving-port', shared='validation/Configuration.Checks/PathHelperChecks.cs', managed='passed-copied10-full3', managedEvidence='artifacts/configuration-validation/build10/path-helper-shared/report.json', reason='Every source assertion preserved over actual selected helper; RemoveAnyFileExtension directly checks implemented complete Extension API.', dependencyInventory='validation/Configuration.Checks/path-helper-test-provenance.json')
        elif name in ['FuzzHasRelativePathSegment', 'FuzzToFileNameLowerCase']:
            row.update(disposition='adapted-original-finite-seed-domain', shared='validation/Configuration.Checks/PathFuzzSeedChecks.cs', managed='passed-copied11b-full2', managedEvidence='artifacts/configuration-validation/build11b/path-fuzz-seed-shared/report.json', reason='All original9/8 seed inputs and reference predicate assertions preserved with finite ASCII regex/lowercase scheduling adaptation; no mutation/arbitrary generated-domain claim.', dependencyInventory='validation/Configuration.Checks/path-fuzz-seed-test-provenance.json')
        elif name == 'FuzzGetNormalizedAbsolutePath':
            row.update(disposition='seed-input-domain-covered-by-retained-original-unit', dependencyInventory='docs/projects-support-tests.json', reason='All source seed pairs are the same literal input domain preserved by retained TestGetNormalizedAbsolutePath with independent original expected values. The old-algorithm equality fuzz callback and mutation campaign are not claimed executed.')
        else:
            reason = 'Ignored-path watcher/service predicates are neither implemented nor consumed by selected config discovery; matcher dot/node_modules exclusions do not imply this separate service.' if 'IgnoredPath' in name else 'Free-string StartsWithDirectory accepts relative/empty/mixed raw strings unlike selected rooted typed API. No invocation substitution: all11 selected rooted predicate assertions remain in TestContainsFilePath.' if 'StartsWithDirectory' in name else 'Free scalar GetRelativePathToDirectoryOrUrl/GetCommonParents entry is outside selected rooted config/read APIs; no whole path.go completion claim.'
            row.update(disposition='excluded-operation', reason=reason)
    if row['package'].startswith('./internal/vfs/vfstest'):
        row.update(disposition='test-host-framework-excluded', reason='Upstream vfstest io/fs wrappers, constructors and writable/concurrent fixture framework are original-oracle setup, not a mapped production API. Managed ConfigurationFileSystem is an explicitly authored read-only host, with raw-byte ReadFile/UNC differences disclosed; no full vfstest assertion port or arbitrary host equality claim.')
    if row['package'] in ['./internal/vfs/vfsmock', './internal/vfs/iovfs']:
        row.update(disposition='external-host-adapter-excluded', reason='Go reflect-based wrapper function injection / io/fs interoperation API is unselected. Actual IFS/OSVFS read APIs and matcher/walker source assertions are selected; no .NET counterpart to this host adapter is claimed.')
report = {
    'upstreamRevision': PIN,
    'status': 'original-test-inventory-and-first-shared-packet-in-progress',
    'contract': 'docs/CONFIGURATION-CONTRACT.md',
    'implementationInventory': 'docs/configuration-functions.json',
    'separateInventoryRule': 'Function implementation dispositions and test/assertion execution are independent; totals never imply completion.',
    'originalOnlyReplay': {'report': 'artifacts/configuration-validation/original-tests/report.json', 'topLevelFunctions': 102, 'managedPortEvidence': False},
    'originalDependencyReplay': {'report': 'artifacts/configuration-validation/original-dependency-tests/report.json', 'topLevelFunctions': 87, 'managedPortEvidence': False, 'scope': 'Includes original writable filesystem assertions for provenance only; managed read-side applicability remains explicit per operation.'},
    'currentManagedShared': {'originalFunctions': 110, 'configBaselines': 4, 'configDirect': 12, 'configMapper': 7, 'showConfig': 2, 'parsedOptions': 3, 'outputPaths': 5, 'wildcardDirectories': 3, 'parsedCommandLine': 1, 'compilerOptions': 6, 'semver': 11, 'packageJson': 7, 'commandLine': 8, 'matcher': 20, 'walker': 8, 'parsingDeclarationRawHelpers': 6, 'contentMapper': 4, 'osRead': 3, 'snapshot': 'central3/5b/7/8/9', 'native': 'pending', 'xUnit': 'pending'},
    'fullAssertionPreservation': 'All 56 TsOptions,20 matcher,8 VFS WalkDir,7 packageJSON,11 semver and5 outputpaths original unit bodies are selected. Reflection domains use independently source-derived static fields under AOT. Baselines retain exact original serialization and formatting bytes.',
    'retainedReusePolicy': 'Review actual callable APIs/assertions before reusing accepted Foundation/Projects path bodies. Native OS adaptation is not a substitute for raw-byte virtual path assertions.',
    'noDirectUnitFiles': ['internal/glob', 'internal/locale', 'internal/diagnosticwriter'],
    'additionalDependencyInventories': ['docs/progress/configuration-path-tests.json', 'docs/progress/configuration-module-tests.json', 'docs/progress/configuration-locale-tests.json', 'docs/configuration-json-tests.json', 'validation/Configuration.Checks/ordered-map-test-provenance.json', 'validation/Configuration.Checks/dependency-metadata-test-provenance.json'],
    'dependencyEvidence': 'Diagnosticwriter has no direct unit suite at this pin; preserve its exact formatted baseline assertions through TsOptions and add bounded generic formatter controls. Locale Parse requires locked x/text grammar/registry coverage, not invented CultureInfo expectations. OSVFS/BOM/realpath, typed paths, selected scalar helpers and inherited path bodies have explicit separate dispositions; no implicit all-VFS/path completion claim.',
    'entries': rows,
}
(ROOT / 'docs/configuration-tests.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({'entries': len(rows), 'fullSelectedOriginalUnits': sum(r.get('originalGo') == 'passed-original-only-replay' for r in rows), 'authoredSharedFunctions': sum('shared' in r for r in rows)}))
