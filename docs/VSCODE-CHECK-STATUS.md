# VS Code checking: diagnostic repairs and performance evidence

The post-acceptance investigation fixed two demonstrated port defects: a null
iteration crash and the ordinary CLI's publication of raw binding diagnostics.
The corrected Linux x64 Native AOT executable completes the main VS Code project
check. Its remaining ten diagnostics also occur in the exact pinned Go compiler;
no relation algorithm was changed to match the different installed compiler.
This follow-up checkpoint records the fixes, focused tests, and comparison findings.

The historical whole-checker acceptance at `17e8c36` and its corpus/proof remain
unchanged. This focused investigation does not repeat that complete acceptance gate
or imply that finite acceptance established exhaustive large-project correctness.

## Workload and compiler identity

Target: `/home/greg/projects/vscode`, commit
`4038aefd2c4aa51457420ef764c4c2358ae0ddfa`, main `src/tsconfig.json` project.
The configuration sets `skipLibCheck: true`. Build tooling and extension projects
are not included. No VS Code files or dependencies were modified.

The requested `./node_modules/.bin/tsc` resolves to `@typescript/native/bin/tsc`,
version **7.0.2**, and runs Go. The separately installed JavaScript TypeScript
6.0.2 compiler was **not run**. The port's exact upstream revision remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, reporting **7.1.0-dev**.
No conclusions about revision chronology are implied by these version labels.

| Run | Result | Wall | CPU (user + system) | Peak RSS |
| --- | --- | ---: | ---: | ---: |
| Accepted original native CLI | Aborted before diagnostics | 65.17s | 152.23s | 12.722 GiB |
| Native with nil-distribution repair | Exit 1; 12 diagnostics | 68.07s | 149.97s | 12.593 GiB |
| Installed Go 7.0.2 | Exit 0; no diagnostics | 11.72s | 58.68s | 8.073 GiB |
| Exact pinned Go | Exit 2; 10 diagnostics | 9.61s | 46.86s | 7.374 GiB |
| Corrected Native AOT CLI, matching noEmit | Exit 1; 10 diagnostics | 62.37s | 147.17s | 13.490 GiB |

The last two runs use the original configuration with `noEmit:true` and
`skipLibCheck:true`. Earlier .NET runs performed no output but retained configured
`NoEmit=false`; the narrow new override removes that option mismatch without moving
the config or changing type-root discovery. The pinned Go executable uses original
tracked compiler source; build-info `vcs.modified=true` reflects existing untracked
oracle/access helpers, not changes to tracked compiler algorithms.

## Repairs and verification

[TypeDiscriminator.matches](../src/TypeScript.Scanner/Checker/Relater.cs) now uses
the loop from pinned `checker/relater.go:1044`. The upstream distribution of `never`
is nil; Go's range visits zero elements, while the former C# `.Any()` threw on null.
The producer remains unchanged, and the consumer returns false for no members.
[Three regressions](../tests/Checker.NoEmit.Tests/DiscriminantNeverTests.cs) compare
complete genuine pinned-Go Program observations with real ES2022 libraries. The old
accepted DLL reproduces two crashes and passes the ordinary control; the repaired
build passes all three. The first repaired native executable also passes the three
disk cases, including exact TS2353 message/span and exit statuses 0/1/0.

[Project.Check](../src/TypeScript.Scanner/Projects/Project.cs) now follows the full
relevant upstream diagnostic aggregation order and gates. It invokes binding but
discards the raw result, allowing semantic policy to apply library skipping and
preceding directives. This removes the two extra Playwright TS2300 reports. It does
not suppress a diagnostic category. Reported-error controls remain errors.
The [ordinary CLI](../src/TypeScript.Check/Program.cs) accepts `--noEmit` for its
existing `--config` route through genuine existing-options config parsing.

The coherent Release build has zero warnings/errors. **14/14 focused xUnit cases
pass**: three discriminator cases, ten complete policy records, and one override
file-discovery assertion. The corrected native executable matches nine applicable
complete diagnostic records and passes flag-order/rejection controls. The remaining
ListFilesOnly record is API-only and tested in xUnit. Independent Sol review covered
the changed upstream functions, adjacent nil-distribution consumers, option join,
reference provenance, and assertions. See [support evidence](progress/vscode-diagnostics-support.md).

The full corrected native check has empty stderr and exactly the pinned Go's ten
TS2345/TS2322/TS2416 diagnostics concerning conditional generic returns. File owners,
codes, line/column locations, and complete rendered diagnostic chains agree.
Go's text CLI does not expose end offsets or structured related-information JSON;
those fields are not claimed equivalent in this full-workload comparison. The
focused policy tests compare complete structured records. Exit 1 for our CLI versus
2 for Go is an existing failure-status adaptation; both communicate checking failure.
The installed 7.0.2 compiler accepts the same project, so this is **not a clean VS
Code check against the pinned implementation**. Changing the pin is separate work.

## Bounded performance assessment

With aligned options and revision, this single native run takes **6.49 times** the
pinned Go wall time and **1.83 times** its peak RSS. It consumes **3.14 times** the
CPU work and averages **2.36 CPU cores**, versus Go's **4.88**. Thus both more CPU
work and lower average utilization contribute to the wall-time gap. This is an
arithmetic decomposition, not attribution to particular code or runtime phases.

Concrete areas for later investigation:

- Allocation and representation costs: Go byte strings use .NET UTF-16 characters
  with conversion allocations; link-store pages contain separately allocated
  managed cells rather than Go value-array entries. Their measured cost is unknown.
- GC and scheduling: the release Native AOT build uses optimized code and default
  workstation GC configuration. Both implementations default to four checker
  instances. Lower utilization is observed; GC cost, serial work, and ThreadPool
  scheduling contributions are not yet separated.
- Authored project-wrapper work: eager file/path/import-edge projections and
  repeated cached diagnostic collection exist before/around checking. No evidence
  establishes that this work dominates.

No optimization was attempted. The ordinary CLI does not run the full semantic
observation harness. A one-second OS sampler was attached after startup during the
already-required native check; it observes CPU/RSS/thread state, not checker phases
or GC pause/allocation totals. Single sequential runs have cache/scheduling noise,
and the .NET run has this small extra observation overhead. The next useful step
would be a targeted native CPU/allocation/GC profile to test these hypotheses,
not speculative algorithm changes. No performance tools were installed.

## Reproduction and records

```bash
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~DiscriminantNeverTests|FullyQualifiedName~ProjectDiagnosticPolicyTests'
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release \
  -r linux-x64 -p:PublishAot=true -o artifacts/vscode-diagnostic-fix/native
# If the repository's native compiler shim is needed, add:
# -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc"
artifacts/vscode-diagnostic-fix/native/tscheck \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json

# Build the exact pinned compiler using the existing local Go toolchain:
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go build \
  -o ../../../artifacts/vscode-diagnostic-fix/pinned-cli/tsc ./cmd/tsc)
artifacts/vscode-diagnostic-fix/pinned-cli/tsc \
  --project /home/greg/projects/vscode/src/tsconfig.json \
  --noEmit --skipLibCheck --pretty false
```

Local evidence: [final result](../artifacts/vscode-diagnostic-fix/native-run/result.json),
[final comparison](../artifacts/vscode-diagnostic-fix/native-run/comparison.json),
[performance observations](../artifacts/vscode-diagnostic-fix/performance-observations.json),
[pinned CLI reference](../artifacts/vscode-diagnostic-fix/pinned-vscode/comparison.json),
[policy native controls](../artifacts/vscode-first-check/cli-diagnostic-policy/native-report.json),
[reduced discriminator](../artifacts/vscode-first-check/reduced-discriminator/), and
[first repaired comparison](../artifacts/vscode-never-fix/comparison.json).

For the diagnostic-policy follow-up, one coordinated Release build took 10.57s and
one actual Native AOT publish took 19.51s, followed by the focused controls and one
full native retry. Three existing Sol agents were reused for implementation,
reference investigation and cross-review. The relation reduction avoided an
unjustified checker change; the pinned run removed revision ambiguity. No token or
monetary measurements are available. Full historical gates were not redundantly
rerun for these narrow changes.
