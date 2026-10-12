# Language-services verification

The pinned upstream language-services/LSP contract is accepted on Linux x64 after
independent review of all current runtime gates. Delivery commit and clean-worktree
confirmation remain coordinator actions. See the [assessment](./LANGUAGE-SERVICES-ASSESSMENT.md),
[contract](./LANGUAGE-SERVICES-CONTRACT.md), [source map](../source-map.json) and
[per-function dispositions](./language-services-upstream-tests.json).

Native98 executes 406 whole original functions and 80 authored controls; the adapted
watcher observation and external/partial channels are separate. The complete editor
corpus retains 4,152 passes, 415 original skips and four same pinned-Go failures.
Those failures receive no original-pass credit: three missing-golden outputs match
fully as adapted evidence, while `importType_js` fails its original VerifyNoErrors
precondition before its intended reference assertions. Independent
[direct](../artifacts/language-services/source-review/native-direct-98-evidence.json)
and [corpus](../artifacts/language-services/source-review/native-corpus-98-evidence.json)
reviews bind actual source, ILC inputs and executed binaries.

## Shared managed and actual Native checks

Run from the repository root with the pinned .NET SDK and Node. The xUnit wrappers
and Native runner invoke the same assertion bodies. Publishing alone is insufficient.

```sh
dotnet test tests/LanguageServices.Tests/LanguageServices.Tests.csproj -c Release
dotnet publish validation/LanguageServices.Validation/LanguageServices.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true \
  -o artifacts/language-services/native
artifacts/language-services/native/LanguageServices.Validation
artifacts/language-services/native/LanguageServices.Validation --selection-depth
```

The last command preserves the original 12,000-parenthesis parse/range/serialization
assertion. Run each authored decoder-depth probe in its own process:

```sh
for depth in 16 256 2048 4998; do
  artifacts/language-services/native/LanguageServices.Validation --protocol-depth model "$depth" || exit
done
for depth in 16 256 2048 10000; do
  artifacts/language-services/native/LanguageServices.Validation --protocol-depth any "$depth" || exit
done
```

Run the fatal notification control with core dumps disabled and a deadlock safety
bound. It must terminate nonzero with the exact `authored fatal IPC notification`
marker, without timing out; the current gate returned -6. For example:

```sh
(ulimit -c 0; timeout 30s artifacts/language-services/native/LanguageServices.Validation \
  --ipc-fatal-notification > artifacts/language-services/fatal.log 2>&1)
```

Check the exit status and marker; exit 124 is a failure. This isolated control is
expected to terminate and is not an ordinary successful command.

## Complete original editor corpus

The tracked [candidate backend](../tools/language-services-oracle/fourslash-backend/)
retains genuine Go test bodies, assertions, baselines and authoritative fixture FS.
Only client construction and explicit non-wire state/process joins adapt to the
actual C# Server; no shadow Go Session supplies candidate observations. The
[verification design](./progress/lsp-verification-design.md) documents those joins.

```sh
python3 tools/language-services-oracle/fourslash-backend/prepare-overlay.py
export TSDOTNET_FOURSLASH_SERVER="$PWD/artifacts/language-services/native/LanguageServices.Validation"
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go test \
  -overlay ../../../artifacts/language-services/fourslash-backend-overlay/overlay.json \
  ./internal/fourslash/tests -count=1 -json -timeout 30m) \
  > artifacts/language-services/candidate-corpus.jsonl
unset TSDOTNET_FOURSLASH_SERVER
```

The four pinned-reference failures mean a nonzero package result is expected;
inspect every terminal identity against the recorded dispositions. Do not normalize
arbitrary failures or drop array/message order. The overlay also admits the thirteen
unchanged JS feature files excluded by Go's `_js_test.go` filename rule. For a genuine
Go reference, prepare `--js-only` and use its generated overlay with the candidate
variable absent. Original skips remain skips.

## Public disk/stdio and lifecycle

The fresh public CLI passed full payload comparisons across changing documents,
configuration/dependencies, 100 unchanged queries and 100 unsaved edits. Separate
SIGINT, SIGTERM and both declared-parent watchdog roles also passed. The
[stdio](../artifacts/language-services/source-review/public-stdio-final-evidence.json)
and [lifecycle](../artifacts/language-services/source-review/public-lifecycle-final-evidence.json)
reviews bind the current ELF and actual ILC inputs. Reproduce with tracked tools:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true \
  -o artifacts/language-services/public-native
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go build \
  -o ../../../artifacts/language-services/pinned-tsgo ./cmd/tsc)
python3 tools/language-services-oracle/compare-public-lsp.py \
  --go-command '["artifacts/language-services/pinned-tsgo","--lsp","--stdio"]' \
  --native-command '["artifacts/language-services/public-native/tscheck","--lsp","--stdio"]' \
  --output artifacts/language-services/public-stdio-reproduction --repeats 100
python3 tools/language-services-oracle/compare-public-lsp-lifecycle.py \
  --go-command '["artifacts/language-services/pinned-tsgo","--lsp","--stdio"]' \
  --native-command '["artifacts/language-services/public-native/tscheck","--lsp","--stdio"]' \
  --output artifacts/language-services/public-lifecycle-reproduction
```

Use new output directories. [Editor connection instructions and measurements](./progress/lsp-public-launch-readiness.md)
explain the actual pinned client settings and exact inherited shutdown outcomes.
Public runs bypass one-shot no-GC. The candidate corpus's forced-cleanup ownership
cannot substitute for public shutdown evidence. Short-run RSS growth does not prove
steady-state memory, leak-freedom or universal performance.

## Retained compiler, emit and performance protection

Final retained checks pass 1,730 managed cases plus 496 Emit cases, full Parser 18,799,
six Native publications/direct gates, the qualified 15,330-entry emit corpus and
15,304 exact Native no-emit records. Follow the tracked comprehensive recipes in
[EMIT-VERIFY](./EMIT-VERIFY.md); the [current assessment](./LANGUAGE-SERVICES-ASSESSMENT.md)
links final evidence and distinguishes overlapping channels, preparation skips and
six known external metadata differences. The independently reviewed [corrected performance cohort](../artifacts/language-services/source-review/retained-performance-cohort2-evidence.json)
retains all 75 observations, with modest wall slowdowns on four workloads and faster
ManyFiles. Emit timing and subprocess wall are separate; no new threshold or universal
Native/Go/external-project claim is made.

Historical [dependency74](./progress/lsp-dependencies-74.md) and
[public91](./progress/lsp-public-stdio-driver-promotion.json) evidence remains valid
for those exact snapshots. Their counts and pending-work wording are historical;
they do not replace current-source acceptance or reproduce the final ELF identity.
