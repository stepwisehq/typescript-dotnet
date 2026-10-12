# Snapshot/API/refcount source review

The complete new packet is source-clear against pinned project originals. The
[manifest](./lsp-snapshot-assertions-promotion.json) binds twenty current paths and
three original functions: TestSnapshot (ten groups), TestProjectIDNarrowing and
TestRefCountingCaches (eleven leaf groups). The detailed hash-bound review remains
under `artifacts/language-services/source-review/snapshot-assertions.json`.

Review checked the actual mutable fixture, source/program/cache identity and
reference owners, failed API rejection with canonical host-change adoption,
unchanged-program reuse, duplicate parse-cache lifetimes, extended-config owner
locking and release, and real Session Close. Local API workers are required state
dependencies, not an advertised external embedding endpoint. Cache and pending
queue access changes expose original same-package observations only.

Two fixture findings were corrected: explicit nonnil synthetic-project assertions
and opened-snapshot cleanup after the remaining assertions, preserving the original
LIFO lifetime. The initial class-name collision was corrected, and existing
accepted SnapshotFS checks/wrappers remain byte-identical. Final API joins preserve
all original path/version/range values and initialized program owners.

Original parallel scheduling is adapted to sequential independent sibling groups;
nonfatal cache observations aggregate failures. Fixture cleanup may additionally
close the Session after assertions. No fabricated state, expectation changes or
behavioral/Native acceptance is claimed by this source review.
