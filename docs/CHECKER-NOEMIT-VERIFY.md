# Complete original checker no-emit verification

The [language contract](LANGUAGE-CONTRACT.md) defines source responsibilities. Current
acceptance status belongs in [checker status](CHECKER-COMPLETION-STATUS.md) and the
[durable proof](../validation/results/checker-noemit-verified.json); this document
specifies reproducible inputs and commands, not a pending/completed status.

The pin is `fed0bf24149fb1ed36039212648bafdafc1ea10e`; the accepted predecessor is
`c5e96e1`. Use the repository's .NET 10 SDK, pinned Go toolchain and Linux x64
NativeAOT prerequisites. All original source/configuration bytes, options,
libraries and applicable diagnostic/query assertions are retained.

## Final run

The central build produces an immutable library and source snapshot before the
verifier starts. For the current candidate that is
[build8/snapshot.json](../artifacts/language-validation/build8/snapshot.json).
The gate checks every source hash and the copied DLL hash before compiling its
isolated test consumer; it does not rebuild production during that stage.

```bash
DECLARATIONS_CURRENT_CHECKING=1 CHECKER_LANGUAGE_CURRENT=1 \
  python3 tools/run-checker-noemit-gate.py \
  --snapshot artifacts/language-validation/build8/snapshot.json --preflight
DECLARATIONS_CURRENT_CHECKING=1 CHECKER_LANGUAGE_CURRENT=1 \
  python3 tools/run-checker-noemit-gate.py \
  --snapshot artifacts/language-validation/build8/snapshot.json
DECLARATIONS_CURRENT_CHECKING=1 CHECKER_LANGUAGE_CURRENT=1 \
  python3 tools/record-checker-noemit-evidence.py \
  --snapshot artifacts/language-validation/build8/snapshot.json
```

For a fresh checkout, first build the actual library with
`dotnet build src/TypeScript.Scanner/TypeScript.Scanner.csproj -c Release` and
capture its DLL/PDB/deps plus current source hashes using
[the snapshot command](../tools/snapshot-checker-library.py). Pass that snapshot
path instead. The recorded fixture requests identify this checkout's original
source paths. [The registry preparation command](../tools/prepare-checker-noemit-registry.py)
can relocate only those source paths to a new checkout, preserving original
fixture hashes/options/configurations and reference records; the gate binds the
resulting registry bytes. It never substitutes fixture contents or diagnostics.

The stages are:

1. Validate the complete original input/reference/configuration registry,
   source-qualified dispositions, fixed authored capability migrations and all
   retained source guards. Validate every invoked project path.
2. Build only the isolated whole-checker consumer and xUnit assembly. Execute
   15,304 complete checking records, 33 original preparation-failure assertions
   and three authored harness controls in-process. Persist every actual checking
   record and verify the exact executed name set. OS `time -v` records peak RSS.
3. Run exactly one retained protocols cascade, including all earlier accepted
   original tests, native comparisons and ordinary CLI checks. Its earlier
   repeated test executions remain visible; no extra comprehensive suite is added.
4. Publish the new whole-checker consumer as actual Linux x64 NativeAOT and compare
   the same 15,304 complete checking records in bounded process chunks. Missing,
   crashed, timed-out or failed records are failures. Verify original invariants
   inside each runtime rather than comparing private storage.
5. Check source, verification-asset, historical-evidence, upstream and obligation
   drift. Record proof only after every stage succeeds; never overwrite old proof.

[The final report](../artifacts/checker-noemit-validation/final/final-gate.json),
managed TRX/complete records, native chunk records and retained logs are under
[the final artifact directory](../artifacts/checker-noemit-validation/final/).
The proof records actual ELF/hash/dependency evidence. Aggregate selected TRX
counts are executed cases, not a distinct upstream-test count.

## Original inventory and assertion domains

[The catalog](checker-noemit-tests.json) reconciles all 12,824 original compiler
and conformance files. The historical raw extraction remains untouched; actual
original disk decoding expands 15,330 configurations, including 15,291 standard
checking configurations. BOM handling adds 101 net standard variants compared
with the old raw registry. This is input correction, not fixture modification.

The 42 original pre-configuration skipped files are classified individually.
Ten built-API fixtures execute genuine checking using authentic locked public
TypeScript 6.0.3 declarations. The absent two internal declaration roles in
`APILibCheck.ts` remain an explicit unavailable historical environment; its
unchanged input still executes fresh checking/missing-import diagnostics and
original invariants. Of the 32 removed-option fixtures, three have executable
checking obligations, 22 expand to 26 exact unknown-option preparation failures,
and seven retain original `module: none` expansion failures. Consequently the
checking registry has 15,304 rows and the failure-disposition registry has 33.
No original skip or preparation failure is silently counted as semantic success.

Every checking row compares complete separate pre-checking/checking
configuration, Program, syntactic, semantic, global and suggestion diagnostics,
including owners, byte spans, chains, related information, order and nil shape.
The original full type/symbol walker retains its traversal, Any-only exceptions,
flags, formatting, default-library declaration markers, headers, CRLF, file order
and source fixups. `noTypesAndSymbols` disables only the original walker;
union-ordering and parent-pointer assertions still execute afterward in original
order. Enabled `traceResolution` is captured after actual checking.

Both functions in the pinned original
[checker test file](../upstream/TypeScript/tsc/internal/checker/checker_test.go)
remain complete assertion-preserving ports through the retained cascade.
`BenchmarkNewChecker` is a performance operation. The other original checker
[test file](../upstream/TypeScript/tsc/internal/checker/tracer_test.go) tests enabled
trace instrumentation and is explicitly external. The historical authored oracle
utility file is not reclassified as an original test. All four restored mapper
bodies, including both generator policies and bulk/incremental globals, remain
retained unchanged.

This is an assertion-preserving **no-emit adaptation**, not a claim that complete
historical compiler emit/output tests passed. JavaScript/declaration output,
declaration-output producer diagnostics, Emit-induced diagnostics and the
pre/post-Emit count invariant are separate external operations. Actual Incremental
options/buildinfo inputs remain; managed checking recomputes full diagnostics
rather than claiming builder/cache/update parity. In-process contentmapper
callbacks preserve original fixture behavior without claiming subprocess IPC.

## Reference reproduction and ordinary use

[The Go bridge](../tools/checker-noemit-oracle/) adds source-qualified helper
files through an additive overlay; it replaces no original compiler source.
[The oracle build command](../tools/build-checker-noemit-oracle.sh) compiles the
original preparation/walker/assertions. Complete references can be reproduced:

```bash
bash tools/build-checker-noemit-oracle.sh
python3 tools/run-checker-noemit-original.py \
  --requests validation/checker-noemit-corpus/requests.jsonl \
  --original tools/.cache/checker-noemit-original \
  --output artifacts/checker-noemit-validation/reproduced-original
```

The saved reference provenance distinguishes reuse of complete pinned-Go records
from new process execution. No fresh execution is claimed when references are
reused for targeted source repairs. API inputs bind exact package integrity,
licenses/member hashes and the explicit built-environment adaptation; historical
baseline/version differences are not hidden.

For ordinary no-emit checking, use the actual native CLI produced by the single
retained cascade, without observer flags:

```bash
artifacts/protocols-validation/cli-native/tscheck \
  --config examples/protocols/job-stream/tsconfig.json --json
artifacts/protocols-validation/cli-native/tscheck \
  --config examples/protocols/job-stream-invalid/tsconfig.json --json
```

The valid project exits 0; the invalid project exits 1 with its complete six
pinned-Go diagnostic records. [The examples guide](../examples/protocols/README.md)
provides the project paths and source environment. The two former Resolution
JSON/JS rejection controls and former authored JS/JSX/decorator boundary bans are
explicitly promoted to genuine same-input checking observations in the final
[migration provenance](../validation/checker-noemit-corpus/authored-capability-migration.json).
Historical proofs, requests and expectations remain unchanged.
