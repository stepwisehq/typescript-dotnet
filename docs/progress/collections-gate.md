# Collection acceptance gate

Status: accepted for the bounded [contract](../COLLECTIONS-CONTRACT.md). The
[recorded proof](../../validation/results/collections-verified.json) reports exit 0
in 658.892 seconds with no source, asset or historical-asset drift: 44/44 exact
Linux x64 Native AOT collection cases (11.789 seconds), 12 shared checks, 18 exact
order replies each for original Go and native, full 44-case original-overlay parity,
and all 23,467 xUnit cases passing with no skips. All retained regressions passed.
The [manifest](../../validation/collections-corpus/manifest.json) contains those
44 rich cases, eight order inputs, 44 parity inputs and seven support projection
groups covering eight shared bodies.

Frozen source SHA-256:
`4b72bcbcae5b18e9322d2d45ecee624bc0d4b2f4d4b0ca109209fcf78f3fc9c3`.
Verification assets SHA-256:
`5bfa508a7115d8b175af41cea303ba10c792656d7c025b53353b865a6655dda6`.
Seven native and two original-Go executable fingerprints are recorded in the proof.

The coordinator's final command is:

```bash
python3 tools/run-collections-gate.py
```

After successful exit, review closure and unchanged frozen source/assets, record the
new proof separately:

```bash
python3 tools/record-collections-evidence.py
```

The [shell gate](../../tools/verify-collections.sh) composes the accepted callable
cascade exactly once. That runs all four xUnit projects and the six retained native
layers with their existing original-source and execution checks. It then builds the
two collection Go oracles, explicitly reproduces all final collection expectations,
publishes a seventh Collections.Validation Native AOT executable, and runs the final
collection groups, shared controls and process/order checks. Ordinary collection
native stdin/demo execution has no original-Go dependency. Reproduction is the explicit
`--reproduce-original` verification mode.

The [runner](../../tools/run-collections-gate.py) protects baseline `cfa6d2f` corpus
and accepted proof bytes, guards source and verification-asset fingerprints, and adds
the authentic three embedded declaration files/manifest to the earlier asset scope.
It compares their exact bytes and order against pinned upstream files. Seven freshly
published native binaries and both collection original-Go binaries are fingerprinted
before/after their collection execution. Group/proof/native/xUnit counts and digests
come from the frozen manifest. The valid/invalid grouped demo and Array/ReadonlyArray
API specimens must be present; a nonempty stored-observation family is mandatory.
Unknown or incomplete evidence cannot become a success by reporting zero cases.
Shipped examples and their manifest are also fingerprinted; each valid/invalid demo
request must contain those exact source bytes, including raw-base64 requests. The
valid original expectation must have no configuration/global/parse/bind/Program
diagnostics, while the invalid expectation must retain user Program diagnostics.
Shared and corpus xUnit registries are counted separately against their native totals.

The [recorder](../../tools/record-collections-evidence.py) rechecks successful exit,
no drift, current fingerprints, prior proof protection and execution inventories before
writing only `validation/results/collections-verified.json`. Its regression evidence
describes this campaign's executions; it does not rewrite accepted historical proofs.

Unavoidable overlap remains only inside the retained cascade: its foundation original
number fuzz smoke calls precede the binding campaign's full finite baseline calls, as
already specified by the accepted gates. No additional collection-wide original support
test command is inserted: OrderedSet is an explicitly adapted assertion prefix and
remaining selected helper observations have supplementary original-Go provenance.
All xUnit projects and each retained native regression layer execute once, not in an
additional collection cascade. Collection native execution and original reproduction
are distinct required runtimes, not duplicated acceptance runs.

Preparation validation: shell/Python syntax and help mode succeed; exact three library
roots pass; an isolated temporary corpus accepts valid inventory/reports and rejects
nineteen invalid count/digest/mode/rich/shared/order/parity/demo/support/completion controls. The artifact
`artifacts/collections-validation/gate-tooling-checks.json` records tool fingerprints
and explicitly excludes comprehensive/native success. Independent script review was
completed by verifier and support. Strict manifest assembly and gate preflight pass
for the published registry; this does not execute the comprehensive gate.

## Follow-up recommendation

Representative small multi-file checking is presently blocked by program-host and
module work: this checker owns three fixed library roots plus one admitted user script,
has noResolve set, merges script globals, and has no promised import/export resolution,
external-module alias resolution, per-user-file public query ownership, or whole-program
user diagnostic traversal. Already supporting four parsed/bound files does not supply
those missing project semantics. A bounded multi-file ES-module campaign with relative
imports, named/default exports and re-exports, cyclic dependencies, cross-file generic
calls and precise file-owned diagnostics would be the most informative next step.
It can retain these genuine ES5 libraries without simultaneously adding package/Node
resolution or further standard-library environments.

General-language/library gaps are separate: source classes/new, destructuring, optional
chains, async/generators and custom iterators remain excluded; ES2015 Map/Set/iteration
declarations are absent; loaded Intl/intrinsic-string families are not promised merely
because the complete ES5 file is present. Those are subsequent type/language capability
campaigns, not problems solved by a file host. This collection campaign's selected
generic algorithms and identity/cache evidence do not establish whole generic-checker
or all-loaded-library parity.
