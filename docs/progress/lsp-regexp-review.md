# Selected regexp independent review

Review in progress against the pinned Go1.27.1 source closure recorded in
[lsp-service-standard-library-mapping.json](./lsp-service-standard-library-mapping.json).
Scope is actual Compile/MatchString: parser, simplifier, instruction compiler,
NFA/one-pass/backtracking engines, their selected syntax/Unicode dependencies and
the new ModuleSpecifiers exclusion/cache consumer. Other public regexp APIs are
explicitly excluded. This is source/assertion review, not an execution claim.

Completed first read: GoRegexp compile/admission/prefix/capture-size/engine-selection,
NFA pool and queue ownership, input rune/context decoding, lazy empty-width flags,
and the actual exclusion-pattern wrapper/double-checked cache admission. Required
prefix copy, cached pattern clone, nil successful zero-capture sentinel, mutable
instruction-arena Memory references and ref queue swaps match these originals.
No substantive finding in this first portion. Remaining: full parser/syntax,
one-pass/backtracking bodies and original assertion tables/provenance. Source owner
retains edits and runtime-failure fixes; final hashes bind only a stable final packet.

Instruction compiler and simplifier bodies are also compared through all selected
original functions: patch-list arena mutation, alternate/nullable repetition priority,
folded rune specialization, unchanged-child identities, copy-on-first-change and
embedded-array/header distinction are clear. The authored inline-header support
creates new embedded owners on copied nodes while retained Sub/Rune descriptors
continue pointing to their original owners, matching the Go value-copy boundary.

All three matching engine bodies are now compared: NFA, one-pass analysis/copy/
rune-merge/cleanup/execution and bounded backtracking with pooled bit-state reset.
Original anchoring, alternate ambiguity, priority, visited-state reuse across starts,
required arena/rune copies and capture-free successful sentinel behavior are clear.
The MinInputLen runtime failure was an assertion extraction defect: the generator
previously read5 inside `{3,5}` rather than6 after the string literal. The corrected
extractor restricts numeric parsing to the suffix after the token; the unchanged
Go expectation and production worker were independently checked. Whole parser and
final original-table provenance review continue before a final packet ACK.

## Final selected-source review

The complete selected Compile/MatchString closure is independently reviewed clear:
parser admission, error remainder bytes, rune/class folding and factoring, repeat
size/height limits, syntax snapshots, simplifier, compiler, all three engines and
the actual module-specifier exclusion/cache consumer. There is no outstanding
algorithm or ownership finding in this packet. Explicit inline owners retain the
original embedded-array versus shared-header distinction; required prefix and
pattern copies remain. This does not claim the excluded replacement/find-all,
reader, capture-result or byte-Match public APIs.

The generated data was independently reconciled with pinned Go source: all219
new Unicode range owners have exact ranges, strides and Latin offsets; Perl/POSIX
range payloads also agree. Generator map aliases preserve shared table identity,
including reused accepted Mn/Lu owners. Original parser dumps, flags and admission
tables retain their original assertions, including malformed raw bytes. The
MinInputLen extractor correction restores6 after the literal `(aa){3,5}`; it does
not change the production worker or the pinned expectation. Go byte literals enter
the shared checks through raw-byte compatible strings, with Unicode explicitly
encoded once at that boundary.

The build16 managed16-group pass is coordinator execution evidence, not a Native
claim. Those groups distinguish fifteen whole original functions from the selected
TestMatch.MatchString channel. The later complete TestMergeRuneSet13-row assertion
and its visibility-only worker join are reviewed clear. SwitchBacktrack through
MatchString is explicitly an API adaptation, not whole-function credit. Remaining
original applicability/dispositions and actual Native execution belong to the
coordinator's next gate. No workloads or builds were run by this reviewer.

### Bound snapshot

The hashes below bind the reviewed production, consumer, generators and shared
assertions. Later changes require a proportional delta review.

| File | SHA256 |
| --- | --- |
| [Backtrack.cs](../../src/TypeScript.Scanner/RegularExpressions/Backtrack.cs) | `36dc951ffe5d6429f354ddc974a99d4b8a5253034731545ec06d0053805a0576` |
| [Exec.cs](../../src/TypeScript.Scanner/RegularExpressions/Exec.cs) | `eb5f384b4c31e532dc3bf87a3e5f7506e5e0bf1bdd4c6346baef1afcc69a0b49` |
| [LazyFlag.cs](../../src/TypeScript.Scanner/RegularExpressions/LazyFlag.cs) | `c7259c679fe571c76c1290103b54129d0f7fb773e0b52a239fde42e18805a9da` |
| [OnePass.cs](../../src/TypeScript.Scanner/RegularExpressions/OnePass.cs) | `f1b6a43ea39f04427e190b85e7903b136608b48f9b7819fbaa769a8e24bb1d26` |
| [Regexp.cs](../../src/TypeScript.Scanner/RegularExpressions/Regexp.cs) | `2be7cf33cdd76df7ebdaf22cb3e1888347812bef885416bd002ba786a9932488` |
| [Compile.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/Compile.cs) | `00b6cf0e5028edea63d3d2dae5995e2afe5f5352109ff4c57bc498cb564d42ed` |
| [InlineSlice.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/InlineSlice.cs) | `09390eb9739e2bb1d15019132bdb41170073df11738ae580524851af26501479` |
| [Parse.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/Parse.cs) | `eadcc0444bd74955ae8ced40170eaf3b5dcb4198f02fa76aae27f47ccd49eb11` |
| [PerlGroups.Generated.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/PerlGroups.Generated.cs) | `b7c16bcb329d8808e59165c94b2e84db01e6078b1cb56eb8c4765dd4408974f2` |
| [Prog.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/Prog.cs) | `73505d4d5e8574d3efd803245f2c25fbe81d6dac16d1af77d89a57558f08806e` |
| [Regexp.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/Regexp.cs) | `edd316c9c89888534218a28013b374136facd121da1502149ec3e7cf0e2b145e` |
| [Simplify.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/Simplify.cs) | `19b234aa504eb8b1c2f3cc9c6c89de8f49999bcbde531bb1d36eb7b76191bb42` |
| [UnicodeTables.Generated.cs](../../src/TypeScript.Scanner/RegularExpressions/Syntax/UnicodeTables.Generated.cs) | `63f02df14165e59d23e941ba0f30a6a03a1f9eb043a13d6aac16765ad9d6a44d` |
| [Util.cs](../../src/TypeScript.Scanner/ModuleSpecifiers/Util.cs) | `23af404f45110d5a6875cba1be36af677352204f485326e5e13cb7c6f24829d1` |
| [generate-regex-checks.py](../../tools/generate-regex-checks.py) | `9292a77a7ba71f771a42dc6b8fe6c7947d7052550ca216dd67ffdfc0eabed905` |
| [generate-regex-groups.py](../../tools/generate-regex-groups.py) | `8651997d422a42af05ae0cfb8af1ff6783a52c9487eb6210cae5c9d76cd60688` |
| [generate-regex-syntax-checks.py](../../tools/generate-regex-syntax-checks.py) | `38415ae446080c60672a6beaaf6064da772e9ada6c1b70f6ab110e2123265eaa` |
| [generate-regex-unicode.py](../../tools/generate-regex-unicode.py) | `9bd43d4e4ec903a70b5102d5c70e564bad59cc34a7af4da3283a088ee666cd3c` |
| [RegexChecks.cs](../../validation/LanguageServices.Checks/RegexChecks.cs) | `d8a6082c07ffbe44e98dc83110a50da4bd168a4e8d1aba5c7919b73aa41e134e` |
| [RegexMergeChecks.cs](../../validation/LanguageServices.Checks/RegexMergeChecks.cs) | `3ec2c21d5cf314b5556b4d7f6f0bd73f6732b0be9a17a9502d440315f6d4e063` |
| [RegexSyntaxChecks.cs](../../validation/LanguageServices.Checks/RegexSyntaxChecks.cs) | `0aae67321572493f3c635041bceec3ee6be6f4cd313c7f4ff547deb55cd80c51` |
