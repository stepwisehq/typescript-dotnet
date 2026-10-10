# Reproducing synchronous expression and flow verification

Current acceptance status is authoritative in the [assessment](EXPRESSIONS-ASSESSMENT.md)
and eventual [proof](../validation/results/expressions-verified.json). This guide fixes
reproducible inputs and commands; it does not itself claim a completed runtime gate.

From the repository root, with the pinned Go toolchain and .NET 10/Linux x64 native tools:

```sh
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-expressions-gate.py --preflight
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-expressions-gate.py
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/record-expressions-evidence.py
```

The [manifest](../validation/expressions-corpus/manifest.json) currently binds 85 complete
Program records: 59 authored source-family requests, two disk-example inputs and 24
compiler-input variants. The 24 variants reuse 20 unchanged upstream fixtures; four
other candidate fixtures are explicitly external. Compiler-input reuse is not a full
compiler assertion-body or emit/type-baseline pass. Six shared groups reuse the two
original checker bodies, three restored mapper semantic-channel bodies, and one
existing authored JS/JSX/decorator external-consumer control. No new original test-body
port is claimed. The new xUnit project has 91 cases. Counts remain manifest-derived.

The runner compares genuine Program configuration/library/semantic outputs, complete
owned diagnostics including byte positions/chains/related information and public inferred
and narrowed identifier-occurrence types. It uses actual options and unchanged bundled
libraries; default `skipLibCheck` is not changed. Existing public property display and
reviewed current checking-control adaptations are inherited explicitly. No diagnostic
or type-display normalization, hidden globals or private-state census is introduced.

Cheap new managed records, original assertions and affected Binder/Checker/Scanner plus
configuration integration tests precede native publication. Preflight validates all
actual fixed `.csproj` paths. Project controls already live in Checker.Tests and the
retained native Projects gate; there is no separate Projects.Tests project. After the
new actual Linux x64 Native AOT corpus/shared/order/ordinary CLI stages, exactly one
retained declaration cascade runs. It preserves the reviewed `DECLARATIONS_CURRENT_CHECKING=1`
context and historical proof/corpus bytes. The selected order set runs 12 cases in two
sequences with a repeated first case, 26 executions per runtime. Native xUnit publication
is unnecessary; the same shared bodies execute in the native validation binary.

After the gate publishes the actual checking executable, ordinary user-facing checking is:

```sh
artifacts/expressions-validation/cli-native/tscheck --config examples/expressions/order-fulfillment/tsconfig.json --json
artifacts/expressions-validation/cli-native/tscheck --config examples/expressions/order-fulfillment-invalid/tsconfig.json --json
```

The valid project must exit 0 with no diagnostics. The invalid project must exit 1 with
all five pinned-Go diagnostics and related information. Both retain actual strict
ES2022/ESNext/Bundler options and unchanged ES2022 libraries. No separate publish is
required after the verification gate.

[The source/test catalog](expressions-tests.json) separates original-body reuse, authored
observations, compiler inputs and unexercised/external responsibilities. Async/yield,
for-await/disposal/dynamic import, checked JS/JSDoc/JSX/decorators, enabled tracing and
operational advanced type completion remain explicit contract boundaries. The original
mapper generator global/incremental semantic debt is not closed by synchronous flow.

The final report counts selected suite TRX case executions once per selected suite;
it is not a distinct-test inventory or the sum of every repeated invocation. Cheap
affected suites and the retained cascade repeat some checks and are recorded separately.
Failures and source/asset/historical/upstream/obligation drift stop acceptance. The
recorder runs once after the complete frozen gate succeeds, preserving previous proofs.
