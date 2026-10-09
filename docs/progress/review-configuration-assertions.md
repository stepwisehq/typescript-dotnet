# Independent configuration assertion review

Reviewer: core owner (`/root/scanner`), 2026-10-09. This is a read-only comparison of verifier/support test bodies against the pinned original assertions; it is not a compiler or acceptance run. Original TypeScript pin: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Locale dependency: `golang.org/x/text v0.42.0`.

The later [recorded comprehensive gate](../../validation/results/configuration-verified.json)
passed all 217 shared groups in xUnit and actual NativeAOT execution, including
the repaired M49 assertions. Core-owned metadata assertions have the separate
[verifier verdict](review-configuration-metadata-tests.json); matcher/walker
assertions have the support verdict in [the core review](review-configuration-core.md).
Runtime evidence is kept separate from the source comparison below.

## Reviewed verifier families

The following 50 TsOptions original functions were compared with their shared C# bodies. Core-owned metadata11 and matcher/walker28 tests require the verifier's independent review; they are not self-approved here.

| Family | Original functions | Assertion preservation reviewed |
| --- | ---: | --- |
| Compiler options | 6 | All 132 exported original fields; original typed parse values and numeric enum branch; distinct complete clone and shallow pointer/list identity; mutable numeric-box sharing; complete Cartesian equality domains and 28-by-28 paths matrix; six exact panic strings; unknown option leaves all fields unchanged; configDir copy-on-write and unchanged slice/map identities. |
| Parsing helpers, declarations, raw options | 6 | Nil/nil merge and unchanged target; explicit raw null versus zero values; shallow merge/source nonmutation; declaration/name completeness; all original path classifications; finalize paths, insertion order, lookup presence and case-sensitive unknown raw key. Full Go JSON tag suffix obligation requires the source-domain guard noted below. |
| CLI/build parsing | 8 | All 63 scenario rows and both old TypeScript typed option/project comparisons and current pinned Tsc diagnostic baselines; five extra build scenarios retain the original omission of the old TS comparison. Seven removed-watch baseline files unchanged. Response-file cyclic/final-token/missing-file checks, relative TypeRoots and customConditions explicit-null override retained. |
| Showconfig | 2 | All 132-field original zero/1/2/b/c/allocated-zero-pointer/nil-empty-one-zero-slice domains, all enum map values, invalid enums/tristate255, relative/rooted paths, mixed list values, final all-options/paths/plugins and first-alias enum expectations. |
| Config baseline APIs | 4 | All 33 config rows across JSON-value/source APIs, seven text groups and eight acquisition rows across both APIs; all 89 original baseline files unchanged. Actual filesystem dump, options, acquisition, file list, colored diagnostics, LF/CRLF and formatting retained. |
| Config direct/source fixture | 12 | All four raw JSON representations; unknown raw custom key and normalized key order; allocated compile-on-save true/default false; null array diagnostics; recovered extended-file token arguments, positions and owner; empty base; exact unquoted/question property locations; null enums through both APIs; cache-hit errors; configDir paths not reused across projects; actual compiler fixture empty Types, options, 79 files and four required members. |
| Parsed command line | 1 | Complete 23-path matching domain; excludes do not remove literals; duplicate removal and missing-files reload; ordinary/mapped/case-insensitive extension behavior; shared AST specs owned by each parsed line; literal/include canonical identity; WithFileNames config identity and exact list. |
| Mapper metadata APIs | 4 | Longest-extension actual mapper identity; host case behavior; exact mapper-owned output list; manifest exec/options/version/package-directory and each original failure code. |
| Mapper configuration parsing | 7 | Both value/source APIs where original does so; dynamicConfig/options/exec/registered extension/file discovery; inherited mapper; flag and unresolved cases; all nine validation rows, source diagnostic ownership/nonempty range and postvalidation mapper contents; all four host case rows. |

The [sequence assertion helper](../../validation/Configuration.Checks/Check.cs) distinguishes nil and allocated empty lists before comparing values. The option expected domains come from original declarations, not production descriptors/Equals. The showconfig reference translates the original reflection serializer to static stored reads; its use of the shared source path serializer mirrors the original helper. Observable ordered-map associations are compared; private Go backing topology is explicitly not asserted.

Read-only generator checks completed successfully: [config baselines](../../tools/configuration-generate-config-baselines.py), [option fields/domains](../../tools/configuration-generate-option-tests.py), [CLI scenarios](../../tools/configuration-generate-cli-scenarios.py), [watch baselines](../../tools/configuration-generate-command-baselines.py) and [Locale literal tables](../../tools/configuration-generate-locale-tests.py), each with `--check`. These checks extract original inputs/read original baseline bytes; they do not regenerate compiler expectations. Config extraction rejects a newly nonnil existingOptions fixture rather than silently dropping it.

## Locale selected/adapted review

Compared the current 27 shared groups in [LocaleChecks](../../validation/Configuration.Checks/LocaleChecks.cs), the literal extractor and [provenance](../../validation/Configuration.Checks/locale-test-provenance.json) against actual x/text bodies. The eight retained helper bodies preserve scanner25/minimum sizes1–8, parse100 and original skip/rewrite domains, extension/variant/raw-field assertions, error priority/value-subtag and syntax sentinel checks, index12 and fix-case7. Public String retains its one literal through the selected Default Parse/String call.

The adapted groups are intentionally separate from whole original body counts:

- Internal equality checks every retained Tag field; compact/public roundtrip groups use the selected comparable canonical Locale value. Private compact numeric topology and the public Compose assertion remain omitted.
- Grandfathered32 preserves all raw Tag fields; MakeString7 preserves both output checks after original three-field setup. The Go allocation budget is omitted.
- Script-ID11 calls the selected existing FindIndex helper using the exact original custom table. It does not claim the unselected custom-registry wrapper API.
- Lang-ID27 preserves lookup/error, ISO2/ISO3 input, normalization and BCP47 output assertions; ISO3 output is omitted. Region-ID15 preserves lookup/string/ISO2 decisions. M49-from25 is retained; inverse M49 output is omitted.
- Region deprecation22/canonicalization6 and base9/script7/region9 preserve selected lookup/raw-field outputs with the documented wrapper adaptation.
- Canonicalization retains 14 compatible Legacy/Deprecated rows through Default and ten Raw rows. Every excluded flag row is listed by the generator; SuppressScript/Macro/CLDR/All transformations and Supported/Compose idempotence are not claimed.
- Public Raw part-checks103, eight error expectations and the seven-line error example retain original literal expected fields/output through selected raw/default parser paths. No inferred expected results or substitution of Default for Raw.

## Findings and disposition

1. **Lost M49 zero-return assertions, repaired in test source.** Original TestRegionM49 continues only when error-presence comparison fails. The managed subset continued on every nonnil error, omitting returned-ID equality for inputs `0`, `-1`, `1000`, `10000`, `899`. Support removed that continue; all 25 rows now compare actual ID with original expected lookup, including those five zero results. Inputs/expected table and production algorithms unchanged. Runtime replay of this repaired test body remains part of the final shared gate.
2. **Go tag suffix accounting repaired.** Original TestCompilerOptionsDeclaration asserts the complete `name,omitzero` struct tag; the managed body has no Go struct tag and checks only mapped JsonName. Verification added a fail-fast check of all 132 complete original source tags to its independent domain generator, and recorded the C# metadata adaptation in provenance. Runtime zero omission is independently exercised by showconfig/static codec tests. Generator `--check` passed after the repair. This was a test/provenance obligation, not a demonstrated compiler defect.

Both findings are closed in reviewed source. No other lost assertion was found in the compared families; final native/xUnit and complete corpus evidence are separate requirements. No comprehensive gate or additional compiler invocation was launched for this review. The Locale extractor now derives its pinned module directory from the repository Go toolchain cache, with an explicit source override, replacing its machine-specific absolute path without changing generated expectations.

## Reviewed source snapshot

Selected reviewed body SHA-256 values (updates after this checkpoint require a narrow delta review):

| Body | SHA-256 |
| --- | --- |
| [CompilerOptionGeneratedChecks](../../validation/Configuration.Checks/CompilerOptionGeneratedChecks.cs) | `3a87274c965e5d4c6837a63529974d5bd886f45063ce2ccc9bedd78d1983e6f5` |
| [ConfigDirectChecks](../../validation/Configuration.Checks/ConfigDirectChecks.cs) including compiler fixture | `23620d341c1bb06b97cde25397df2041f10d80339975e7a2debe7822a3997ae8` |
| [ConfigBaselineChecks](../../validation/Configuration.Checks/ConfigBaselineChecks.cs) | `1ae0c9d4cb0418d4070e025c34f553cf5075a91651c40f117ebe2152442294df` |
| [ShowConfigChecks](../../validation/Configuration.Checks/ShowConfigChecks.cs) | `bb0ce8fb04e183c2a109139a983e271aa63fc26b97aaa6189405f74358e23743` |
| [CommandLineScenarioChecks](../../validation/Configuration.Checks/CommandLineScenarioChecks.cs) | `ba1d066cf41e8c53b418d3b01ec60f96119f6fde3ffb9dd7f6252eae93b9effb` |
| [ConfigMapperChecks](../../validation/Configuration.Checks/ConfigMapperChecks.cs) | `073af4bff9238e16ba1145cb065a6ada39ec66400b001b4a1e6b495543f33b85` |
| [LocaleChecks](../../validation/Configuration.Checks/LocaleChecks.cs) after M49 repair | `d14a328b175640d9e753cd5c260b9bdf5ea8dc706d36570a58078b9f958ff14c` |
