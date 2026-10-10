# Checking against the saved original references

These commands run the current C# checker against the committed pinned-Go
diagnostic, type, symbol and original-invariant references. They do not regenerate
Go output or require ignored origin-generation reports. Use the repository's
.NET 10 SDK, the pinned upstream checkout, and Linux x64 NativeAOT prerequisites.
The [catalog](checker-noemit-tests.json) explains the original preparation and
environment dispositions. Acceptance status belongs in
[checker status](CHECKER-COMPLETION-STATUS.md) and the
[proof](../validation/results/checker-noemit-verified.json).

## Prepare this checkout

Run from the repository root. Initialize or verify the locked upstream checkout
with the existing bootstrap script. Registry preparation verifies
the original fixture hashes and relocates only authored absolute source-path
metadata. It preserves every source, option, configuration and expected record.
The metadata changes are local test preparation; they do not rewrite a historical
proof or claim that proof qualifies this checkout's subsequent execution.

```bash
bash tools/bootstrap-upstream.sh
python3 tools/prepare-checker-noemit-registry.py
mkdir -p artifacts/checker-noemit-cached
export CHECKER_NOEMIT_ROOT="$PWD"
export CHECKER_NOEMIT_REQUESTS="$PWD/validation/checker-noemit-corpus/requests.jsonl"
export CHECKER_NOEMIT_REFERENCE="$PWD/validation/checker-noemit-corpus/expected.jsonl.gz"
export CHECKER_NOEMIT_PREPARATION_DISPOSITIONS="$PWD/validation/checker-noemit-corpus/preparation-dispositions.json"
export CHECKER_NOEMIT_ACTUAL="$PWD/artifacts/checker-noemit-cached/managed-actual.jsonl"
```

## Managed xUnit

The project builds its actual C# dependencies. Each checking row compares the
complete saved record and executes the original union-ordering and parent-pointer
assertions. Preparation failures are separate assertions, not semantic passes.
The fresh output file matters because the tests append their actual records.

```bash
: > "$CHECKER_NOEMIT_ACTUAL"
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release \
  --logger 'trx;LogFileName=checker-noemit.trx' \
  --results-directory artifacts/checker-noemit-cached
```

The full registry has 15,340 xUnit executions: 15,304 checking records, 33 exact
original preparation dispositions and three authored harness controls. A quick
command smoke can select one existing checking row with
`--filter 'DisplayName~compiler/2dArrays.ts'`; this is a smoke, not whole-corpus
qualification.

The project now also contains three Go-backed `DiscriminantNeverTests` regressions
and eleven `ProjectDiagnosticPolicyTests` cases from the subsequent VS Code repairs,
plus 23 authored shared slice-semantics cases from the subsequent inference-stack
migration, and three complete pinned-Go regex byte-argument regressions from the
shared-text-view work, and eleven authored filename-role checks, plus four complete pinned-Go arrow-rune
diagnostic comparisons from the owned-byte source work. An unfiltered
current project run has 15,395 cases. The historical
acceptance totals above remain unchanged. The focused
repair and actual Native AOT retry are recorded in [the VS Code findings](VSCODE-CHECK-STATUS.md).

The documented Release project build and filter were smoke-tested with exactly
one executed, passing row. The documented native project published successfully;
the same selected native record matched the committed reference with process
exit 0. This command smoke does not add another whole-corpus acceptance run.

## Linux x64 NativeAOT

Build the existing whole-checker consumer. It uses actual production source and
the same preparation, diagnostic and full public type/symbol walker as xUnit.
Pass the native compiler/linker property shown below if the repository's cached
compiler shim is required by your environment.

```bash
dotnet publish validation/Checker.NoEmit.Validation/Checker.NoEmit.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -o artifacts/checker-noemit-cached/native
# If clang is unavailable and tools/.cache/native/bin/cc is installed, add:
# -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc"

artifacts/checker-noemit-cached/native/Program.Checks \
  "$PWD" "$CHECKER_NOEMIT_REQUESTS" \
  "$PWD/artifacts/checker-noemit-cached/native-actual.jsonl"
```

The process must exit 0. Compare every complete record, with duplicate/missing
identities rejected:

```bash
python3 - <<'PY'
import gzip, json, pathlib
root = pathlib.Path.cwd()
with gzip.open(root / 'validation/checker-noemit-corpus/expected.jsonl.gz', 'rt') as stream:
    original = [json.loads(line) for line in stream if line.strip()]
actual = [json.loads(line) for line in
          (root / 'artifacts/checker-noemit-cached/native-actual.jsonl').read_text().splitlines()]
expected = {row['name']: row for row in original}
assert len(expected) == len(original) == len(actual) == 15304
assert len({row['name'] for row in actual}) == len(actual)
assert all(expected.get(row['name']) == row for row in actual)
print('15,304 complete native checking records match the saved original reference.')
PY
```

A single long-running native process shares upstream-style fixture caches; the
acceptance run uses bounded chunks to preserve crash/timeout evidence. Neither
command claims incremental builder/cache scheduling or emit-output parity.

## Optional original-reference audit

The [frozen verification guide](CHECKER-NOEMIT-VERIFY.md) documents the complete
acceptance/original-origin audit, including the retained cascade, source and asset
drift checks, Linux ELF evidence and immutable proof recording. That audit also
requires its archived origin-generation reports and executable. Its historical
build8 snapshot and ignored provenance artifacts are not prerequisites for the
ordinary cached-reference commands above. Regenerating Go output is a separate
ground-truth audit, not a routine C# test step.
