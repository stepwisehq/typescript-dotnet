# Language services and LSP assessment

The pinned upstream language-services and LSP contract is accepted for Linux x64
Native AOT after independent final claim review. Delivery commit and clean-worktree
confirmation remain coordinator actions. The [completion contract](./LANGUAGE-SERVICES-CONTRACT.md) defines the
scope at revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`; historical checkpoints
retain their original scope, while the current evidence binds the final source.

## Implementation and ownership

The [source map](../source-map.json) and linked owner manifests account for protocol,
JSON-RPC/framing, project/session/snapshot lifetime, watches and overlays, formatting
and change tracking, autoimports, semantic/edit services, registered server requests,
and public CLI dispatch. The39 language-service root counterparts/1038 mapped workers
and100 contracted Server workers have been implemented and independently reviewed.
Source coverage is not a proof of every behavior: subsequent logging, hover,
null-list and depth findings were real supporting-owner gaps and are preserved with
their corrections and failed execution records.

Required Go copies, retained byte/slice headers, checker leases, immutable snapshots
and cancellation carriers remain explicit. Locale selection uses pinned x/text
workers and13 genuine catalogs. Linux watcher support uses the pinned shared and
Linux algorithms/native ABI. Runtime adaptations include synchronous parser stack
continuation and dedicated IPC reader/handler threads plus synchronous private
response delivery. These preserve the selected blocking/error ownership paths;
OS-thread/default-stack cost is not Go goroutine resource or performance parity.

## Current execution

| Gate | Current evidence | Remaining acceptance |
| --- | --- | --- |
| Shared managed/direct assertions | Integration98 corrected packet executed; original mapper and watch-timeout failures corrected | Accepted |
| Actual shared Native98 |406 whole original functions and80 authored controls passed; adapted watcher/external/partial channels separate; all11 isolated/ordinary gates independently reviewed | Accepted |
| Full original editor corpus | Native98 completed4152 passes,415 original skips and4 same pinned-Go failures across4571 identities; independently reviewed | Accepted disposition/proof review |
| Public disk/stdio | Fresh final CLI both paired drivers passed:100 queries/100 unsaved edits, exact two shutdown and four signal/watchdog roles | Accepted delivery evidence |
| Retained compiler/emit | Final retained managed1,730 plus Emit496 passed; full Parser18,799 passed; six actual Native publications/direct gates passed; full15,330-entry emit corpus completed with documented dispositions | Command gates and15,304-entry Native no-emit exact comparison passed; scoped performance cohort completed and independently reviewed |

[Native98 process outcomes](../artifacts/language-services/integration-7/native-gates-98.json)
and [actual ILC input hashes](../artifacts/language-services/integration-7/native-inputs-98.json)
retain the executed binary identity. Independent [direct Native98 review](../artifacts/language-services/source-review/native-direct-98-evidence.json)
and [full corpus review](../artifacts/language-services/source-review/native-corpus-98-evidence.json)
bind968 current source hashes,179 actual ILC inputs and the executed ELF without drift.
Three missing-golden full observed outputs match270/373/891 bytes exactly; they remain
reference-failure dispositions with zero added original-pass credit. The fourth
`importType_js` reference failure retains the same two diagnostics: its original
VerifyNoErrors precondition fails, blocking the later intended reference assertions.
Those reference assertions are unexecuted in both implementations, with no original
pass credit or claim that their semantic results were observed. The original32-file parallel mapper now passes
following complete request/response/duplex delivery correction; Native94 and managed96
initialization failures are not relabeled. The original watch-timeout harness keeps
its real two-second sleeps and assertions, adding actual background-batch completion
where Go uses synctest quiescence. Optional Replay remains an original input-dependent
skip, not a passing assertion.

The [test inventory](./language-services-upstream-tests.json) is the authoritative
per-function disposition packet accounts for all 5,006 identities: 406 direct passes,
4,152 editor passes, 415 original editor skips, one optional Replay skip, four known
pinned-reference failures, one Go generator disposition, 26 excluded platform
backend tests and one adapted watcher observation. Historical totals must not be
substituted for current evidence. Original functions, selected channels,
the adapted watcher thread observation, authored controls, table rows and package
lifecycle functions remain separate. Full corpus uses genuine Go test bodies through
a candidate-backed C# server adapter; validation-only FS/process/state joins are
explicit, with no shadow Go Session. Public launch evidence uses a separate real
disk and stdio driver.

The [final retained managed marker](../artifacts/language-services/final-retained94/managed-complete.json),
[Native direct marker](../artifacts/language-services/final-retained94/native-complete.json)
and [emit corpus marker](../artifacts/language-services/final-retained94/corpus-complete.json)
record completed frozen-source stages. The directory's historical94 name does not
identify its source: the run binds the final98 source freeze and preserved narrow
retained test/counter transitions. Managed Emit496 includes three authored mutex
controls beyond the historical493; remaining1,730 retained cases are unchanged.
The15,330-entry emit corpus retains15,285 exact public outputs,13,571 original
assertion outcomes,39 preparation skips and six known external metadata differences.
These overlapping channels are not summed as test identities. Command/no-emit and fresh public drivers have now passed. Performance cohort and final claim review are complete.

## Public operation and limits

[Verification commands](./LANGUAGE-SERVICES-VERIFY.md) and
[public reproduction/editor setup](./progress/lsp-public-launch-readiness.md)
explain `tscheck --lsp --stdio`, bundled libraries, actual client configuration and
both lifecycle roles. Long-lived LSP bypasses the one-shot no-GC region. Shutdown
then exit with stdin retained inherits pinned exit1/`context canceled`; shutdown
then EOF gives exit0/empty stderr. Signal/watchdog cancellation has exact recorded
terminals. No arbitrary nonzero status is accepted.

Public91's short local run retains separate cold/query/edit timings and growing
RSS/thread samples. It establishes responsiveness for that workload, not a memory
plateau, leak-freedom or universal/server performance threshold. Fresh final public [measurements and independent proof](./progress/lsp-public-launch-readiness.md)
are recorded. The observer-corrected [performance cohort](../artifacts/language-services/final-performance/cohort-2/result.json)
and [independent review](../artifacts/language-services/source-review/retained-performance-cohort2-evidence.json)
retain all 75 observations. Best-of-five subprocess wall ratios against the accepted
Native baseline are 1.033, 1.050, 1.030, 0.977 and 1.061 for 1,000/5,000/10,000 lines,
ManyFiles and multiline lines respectively. Thus four workloads are modestly slower
by about 2–6%, while ManyFiles is about 2.3% faster. Emit-phase ratios are separately
1.061, 1.056, 1.034, 0.976 and 0.998; measured Native allocations are lower on all
five workloads. This is scoped protection evidence, not equal speed or a universal
20% Native/Go guarantee. Native whole-runtime bytes and Go B/op are different units.

The preserved [first cohort](../artifacts/language-services/final-performance/cohort-1/result.json)
used a timed-wait polling wall observer. Its short-workload wall ratio1.780 disappeared
with blocking waitpid plus a separate safety timer; Emit timing and production
binaries were unchanged. Both cohorts remain visible, and no claim attributes every
original timing difference to the observer. Fixture/Program construction is excluded
from the Emit-phase timer and remains included in subprocess wall observation.

External projects and actual editor UI/package execution were not required or
performed. Windows/macOS/BSD native watcher backends, public embedding APIs, unused
schema methods and upstream-unregistered features retain explicit contract boundaries.
The upstream code-action, reference/symlink and semantic-token-delta limitations are
preserved rather than advertised as newly implemented features.

All contract evidence and dispositions have independent final review. The coordinator
accepted this bounded contract; delivery commit and clean-worktree confirmation are
still coordinator actions. Acceptance retains every implementation/platform boundary
and evidence limitation described above.
