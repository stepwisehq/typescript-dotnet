# Project campaign adaptations and exclusions

Status: bounded P1–P5 accepted; see the [assessment](./PROJECTS-ASSESSMENT.md) and
[actual native evidence](../validation/results/projects-verified.json).
The [contract](./PROJECTS-CONTRACT.md) is authoritative. Per-file implemented scope
belongs in [source-map.json](../source-map.json), updated after source integration.

A late negative control exposed a genuine false success: a scanner-preserved lone
surrogate in an import specifier was replacement-decoded before filesystem lookup,
so the port could load a different, real replacement-character filename. The first
acceptance run was stopped without recording acceptance. The disk adapter now
strictly decodes representable UTF8 paths and rejects this unsupported encoding
explicitly, without substituting a filename. The repaired-source full gate passed. Scanner/string-literal CESU8
semantics stay intact. The original Go returns missing-module 2307 for these test
inputs; the managed host rejects the unrepresentable path with status 2. These are
explicit boundary controls, not diagnostic-equivalence successes. Genuine Unicode
supplementary pairs and real U+FFFD filenames remain supported. Arbitrary raw-byte
Unix filenames are outside the bounded .NET disk host.

| Area | Adaptation or bounded selection | Reason and observable obligation |
| --- | --- | --- |
| Configuration entry | Authored strict JSON subset, explicit files and fixed recognized options | A full CLI/tsconfig parser is outside scope. Reject unsupported properties/options instead of accepting then ignoring them. Go oracle uses equivalent original Program configuration. |
| Disk host | .NET filesystem and stream facilities, Linux case-sensitive canonical identity | Preserve importing-file-relative resolution, normalized file reuse and failures; exclude symlinks and outside-project paths explicitly. This is host support, not a replacement checker. |
| Module resolver | Selected original Bundler relative search, no package/Node ecosystem | Keep original function/control-flow and candidate precedence. Encountered unsupported candidate/metadata fails; it must not be skipped to obtain a supported answer. |
| Program execution | Mapped FileLoader/FilesParser tasks and one shared checker, original single-threaded scheduling | No incremental/project-reference/service infrastructure or parallel scheduler claim. Preserve task deduplication, source/alias identity, cyclic loading, separate final ordering, full applicable diagnostic channels and repeated query state. |
| Libraries | Existing exact embedded declarations with skipLibCheck | No upstream checkout or Go process needed for ordinary native use. Whole-library body checking and all APIs remain unclaimed; demanded declaration semantics stay real. |
| Options | Fixed supported options and explicit disabled automatic types/JS/JSON | Bundler is viable at this pin; Classic/Node10 and explicit downlevelIteration config are removed. Effective option parity must be recorded, including config diagnostic ownership. |
| Unsupported syntax | Existing collection admission plus selected module forms | An unsupported source or dependency is a distinct failure, never a successful check with placeholders. Internal representations demanded by supported semantics remain required. |
| Semantic observations | Authored static readers/projection, selected new module/alias state | Native AOT forbids relying on arbitrary runtime reflection. Compare identity/relationships, not only text/counts; omissions and instrumentation effects must be documented. |

Project-root containment applies to loaded source dependencies. Original diagnostic
specifier generation also needs genuine read-only ancestor package lookup and a
narrow index-shadow probe: formatting `<root>/index.ts` can consult `<root>.ts` and
the corresponding original extension candidates to decide whether `..` is ambiguous.
Permit those exact metadata/existence queries without loading the outside file;
retain the explicit symlink policy. Test existence-dependent spelling and verify
that outside files never enter the Program source set.

No change to accepted library bytes, expected historical fixture behavior or upstream
revision is authorized. Any additional departure discovered during implementation
or review must be explained here or in the relevant per-owner checkpoint and mapping.

Direct tests retain the established foundation test-file mapping exception: selected
original functions share AOT-callable assertion helpers used by xUnit and the native
dispatcher. Newly relevant path assertions are in a generated project helper;
already accepted path assertions stay in the foundation helper and regression gate.
The [test catalog](./projects-support-tests.json) maps each original function to its
actual destination and labels new versus retained execution, adapted assertions,
and authored controls. This does not split the production Go-to-C# counterparts or
count the retained functions as newly ported tests.

The original file loader may record an ancestor PackageJsonDirectory even for Bundler
`.ts` input outside node_modules. Initial original-Go controls under ancestor
package.json `type: module` and `type: commonjs` produced identical selected outputs;
they do not establish general package metadata independence. A later demanded
formatter path (GetModuleSpecifiers / ProjectRelative for a target not already
directly imported) can consult ancestor imports maps under Bundler defaults.
Implement genuine required ancestor lookup/read and preserve required state. If that
path requires package mapping, report explicit unsupported within the existing package
resolution exclusion; do not fabricate absent metadata or change default options to
avoid the original branch. Ordinary package files without applicable mappings remain
supported. Exact observed/omitted metadata fields must be reviewed before acceptance.
