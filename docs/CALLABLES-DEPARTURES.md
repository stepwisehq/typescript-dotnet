# Callable campaign adaptations

This records implementation decisions; acceptance is tracked separately. The fixed
[contract](./CALLABLES-CONTRACT.md) and [status](./CALLABLES-STATUS.md) control scope.
Earlier accepted language adaptations continue to apply.

- **Explicit authored entry profiles.** Legacy retains accepted admission/options;
  Callable selects the same upstream checker with the documented fixed options and
  a larger syntax boundary. Admission distinguishes unsupported syntax from checked
  semantic errors; it is not a replacement checker.
- **Public API ownership.** Per-checker managed identity registries track allocated
  transient symbols and signatures solely for authored public guards. Returned
  synthetic parameters may have no declaration. Foreign checker-created objects
  must still reject even if their declaration belongs to the same source file.
  Binder-owned symbols retain source-file ownership checks. The extra guard state
  does not replace upstream symbols, caches or algorithms.
- **Diagnostic name scopes.** Private managed fake-name scopes clone on entry
  instead of Go's copy-on-first-write. Parameter/declaration identities and scope
  restoration are preserved; the difference is allocation timing in diagnostic
  formatting, not a new name-resolution algorithm.
- **Numeric implementation.** CLR binary64, bit-conversion facilities and
  System.Numerics.BigInteger replace the corresponding Go primitives. Integer
  exponentiation retains the original exact-integer/256-bit intermediate rounding
  algorithm before binary64 conversion; ordinary Math.Pow alone is insufficient.
  Original numeric literal and live Node assertions are shared by xUnit and the
  native executable, including the original exponentiation Node comparison tolerance.
  Default PseudoBigInt backing-string null follows Go's empty-string value semantics
  for equality/hash; observation retains the actual raw empty value.
- **Test execution.** The ten newly applicable numeric test functions contain 152
  table rows, each with an original literal assertion and an independent Node
  assertion (304 checks). Node operations are batched into one subprocess using
  lossless IEEE-bit input/output. Assertions are preserved; test-process organization
  is adapted. Benchmarks are excluded. Historical foundation inventories/proofs are
  left intact and new applicability is recorded in the callable provenance.
- **Diagnostic support is bounded.** Original signature formatting, declaration
  visibility, trailing-semicolon writing, constant evaluation and promise-like
  diagnostic inspection are required by this contract. They do not imply general
  emit, async syntax, standard-library loading or source generic support. Missing
  globals and real fallback types remain observable.
- **Internal unique symbols.** Caller-written ambient Symbol/Symbol.for declarations
  trigger the original internal unique type inference even without libraries. The
  actual symbol-keyed cache and raw unsigned-ID name are retained; written unique-
  symbol syntax remains outside admission. Observation validates that complete name
  preimage and maps only the allocation ID to the already-observed symbol identity.
- **Observations are authored.** Go and C# observers record actual fields and
  relationships with logical canonical identities and explicit query order. Physical
  addresses/allocator values are not equivalence claims. Null, allocated empty and
  shared sentinel states are distinct; unknown payloads fail rather than disappear.

Every port retains its original Go counterpart in the source map. New harness,
admission and observation files have no fabricated upstream origin. Selected source
branches remain partial, with exclusions and remaining guards reviewed against the
contract before completion.
