# Invariant globalization experiment

Tested configuration: Linux x64 Native AOT, .NET SDK 10.0.401/runtime 10.0.12,
with the accepted Speed, AVX2, Server GC and compiler settings retained.
Production project settings and compiler source are unchanged. This is an
experiment, not adoption of a new default.

## Size

| Native CLI | Executable bytes |
| --- | ---: |
| InvariantGlobalization=false | 31,805,480 |
| InvariantGlobalization=true | 31,628,136 |
| Reduction | 177,344 (0.56%) |

Both publications used identical hashes for all 178 managed inputs. Archived
Native AOT response files confirm the globalization feature/runtime flags are
the only changed runtime options. The earlier measured baseline has a different
assembly version stamp; the freshly published false control removes that
confound. Debug symbols are separate and excluded from these executable sizes.

The normal Linux binary loads ICU dynamically from the system. ICU's large data
files therefore were not embedded in this executable to begin with. The compiler
also has its own upstream-derived Unicode, normalization and diagnostic-language
tables; invariant .NET globalization does not remove those.

## Behavior

The invariant executable compiled the existing VS Code checkout and matched the
pinned Go compiler on all 10,031 emitted files (150,466,543 bytes), byte for byte.
Both reported the same ten complete comparable CLI diagnostics and exited with
status 2. This establishes agreement on that workload, not an error-free VS Code
check. Comparison includes diagnostic text, category, code, file, line and UTF-16
column; Go's text output does not expose end spans or related-information DTOs.
The ordinary comparison run is not a controlled performance measurement.

All 3,330 selected xUnit cases passed with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`
(109 configuration, 67 language services, 604 parser, 2,550 scanner; no failures or
skips). Actual invariant Native AOT validation also passed:

- 3,153 original foundation cases plus 35 byte/text controls.
- 109 selected configuration, semver, locale and path checks.
- The existing language-services runner: 406 original functions, 80 authored
  controls and its separately reported Unicode, normalization, locale and regex
  assertion channels.

Existing upstream dispositions remain visible in the logs: the ATA skip, RE2
byte-escape/UTF-8 boundary exclusions and Fowler unsupported/control rows. One
watcher test passed on its existing second attempt with increased timeout scaling;
no assertions or expectations were changed. This experiment did not rerun the full
compiler corpus or LSP transport acceptance. Independent source review found compiler-owned
implementations for the main Unicode/locale operations, with BCL invariant casing
and ordinal path comparisons remaining relevant boundaries.

## Reproduction and evidence

Experiment artifacts are under `artifacts/vscode-invariant-globalization/`:

- `publish-cli.py` and `build-1/`: invariant publication, archived actual ILC
  inputs and response file. The initial script rejected the changed assembly
  version stamp after a successful publish; `binding-transition.json` records
  that issue and the reviewed final binding preserves it.
- `publish-false-control.py` and `control-false-1/`: matching-input non-invariant
  control and `size-comparison.json`.
- `vscode/comparison.json`: emitted-file hashes, complete comparable diagnostics,
  binary hashes, options and execution results.
- `validation-plan.json`: existing checks selected for globalization risk.
- `managed-results.json`, `managed-environment.json`, `managed/`: all four xUnit
  invocations, invariant environment and TRX records.
- `run-validation.py`, `validation-2/`: three actual invariant Native AOT
  publications and their passing selected checks. `validation-1/` preserves a
  rejected managed-only output caused by restoring without PublishAot; the
  corrected restore/publication admitted genuine native executables.

The durable [result summary](../validation/results/invariant-globalization.json)
records counts, hashes, limitations and artifact locations. Native checks can be
reproduced with:

```sh
python3 artifacts/vscode-invariant-globalization/run-validation.py \
  --output artifacts/vscode-invariant-globalization/validation-fresh
```

The output directory must be new. Managed commands and exact filters are recorded
in `validation-plan.json`; run them with
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`.

The VS Code comparison command was:

```sh
python3 tools/compare-vscode-emit.py artifacts/vscode-invariant-globalization/vscode \
  --native artifacts/vscode-invariant-globalization/build-1/native-avx2/tscheck \
  --go artifacts/emit/command-reference/tsgo
```

Upstream revision: `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Source HEAD at publication: `fa23726ece50645372a635b3e41b87edbcac6902`.
No speed improvement or additional platform coverage is claimed.

## Assessment

No globalization-related discrepancy was found in these checks. Invariant mode is
plausible for the tested compiler configuration and removes its .NET ICU dependency,
but a 0.56% executable saving alone is a weak reason to change the default. Keep the
current default until deployment requirements justify that tradeoff; arbitrary
culture-dependent .NET behavior is intentionally unavailable in invariant mode.
Independent GPT-6.1 Sol review found no outstanding issues in the evidence or
qualified assessment. No production change or commit was made by this experiment.
