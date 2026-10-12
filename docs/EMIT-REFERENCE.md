# Reproduce the original emit reference

Generated reference/registry files under `artifacts/` are evidence, not checked-in
expectations. These instructions regenerate them from the pinned original source and
checked-in decoded configuration inventory. They were prepared without executing a new
capture. Run from the repository root in a fresh checkout; preserve prior evidence by
using a fresh artifact directory. Do not run during a frozen correctness gate.

Prerequisites: the checkout specified by [upstream.lock.json](../upstream.lock.json)
at revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`, its original test data/baselines,
and Go1.27.1. The checked-in
[decoded inventory](../validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz)
retains original source hashes, configuration identities and expansion dispositions.
The registry producer rejects source drift rather than replacing expectations.
Use the existing toolchain/shared build cache or an equivalent Go1.27.1 installation.

## Prepare and compile once

```sh
python3 tools/prepare-emit-registry.py artifacts/emit/reference-infrastructure/registry
python3 tools/prepare-emit-overlay.py artifacts/emit/reference-infrastructure/overlay.json
TASK_EMIT_ROOT="$PWD"
cd upstream/TypeScript/tsc
GOCACHE="$HOME/.cache/go-build" "$TASK_EMIT_ROOT/tools/.cache/go/bin/go" test \
  -overlay="$TASK_EMIT_ROOT/artifacts/emit/reference-infrastructure/overlay.json" \
  -c -o "$TASK_EMIT_ROOT/tools/.cache/emit-original" ./internal/testrunner
cd "$TASK_EMIT_ROOT"
```

The overlay adds observation code and leaves original assertion bodies unchanged.
The request registry must have15,330 unique names. Its separate fixture dispositions
retain seven original expansion failures; no fallback configuration is invented.

## Capture bounded batches and the record supplement

Save/run the following ordinary Python recipe from the repository root. It compiles
nothing, launches the same original binary for100 requests per process, validates
identity/dispositions, closes each gzip member before deleting raw output, and retains
per-batch logs/results. It deliberately rejects an existing reference destination.

```python
import collections, gzip, hashlib, json, os, subprocess
from pathlib import Path
root = Path.cwd()
registry = root / "artifacts/emit/reference-infrastructure/registry/requests.jsonl"
binary = root / "tools/.cache/emit-original"
destination = root / "artifacts/emit/whole-reference"
destination.mkdir(parents=True, exist_ok=False)
requests = [json.loads(s) for s in registry.read_text().splitlines()]
assert len(requests) == 15330 and len({x["Name"] for x in requests}) == 15330
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
initial = {str(p): sha(p) for p in [registry, binary]}
(destination / "frozen-inputs.json").write_text(json.dumps(initial, indent=2))
command = [str(binary), "-test.run=^TestAuthoredWholeEmit$", "-test.parallel=1",
           "-test.count=1", "-test.v"]
statuses, observed, records_missing = collections.Counter(), 0, []
def capture(rows, folder):
    folder.mkdir(parents=True, exist_ok=False)
    request_file, output = folder / "requests.jsonl", folder / "original.jsonl"
    request_file.write_text("".join(json.dumps(x) + "\n" for x in rows))
    tracking = folder / "tracking"
    tracking.mkdir()
    env = os.environ.copy()
    env.update(EMIT_REQUESTS=str(request_file), EMIT_OUTPUT=str(output),
               TSGO_BASELINE_TRACKING_DIR=str(tracking))
    with (folder / "original.log").open("w") as log:
        exit_code = subprocess.run(command, cwd=root / "upstream/TypeScript/tsc",
                                   env=env, stdout=log, stderr=subprocess.STDOUT).returncode
    actual = [json.loads(s) for s in output.read_text().splitlines()]
    expected = {x["Name"]: x for x in rows}
    assert exit_code == 0 and len(actual) == len(rows)
    assert {x["name"] for x in actual} == set(expected)
    for x in actual:
        q = expected[x["name"]]
        assert x["sourceSha256"] == q["SourceSha256"]
        assert x["configuration"] == q["Configuration"]
        assert x["status"] in {"passed-original-full-harness",
            "original-option-policy-disposition", "skipped-original-before-configuration"}
        for channel in x.get("channels", {}).values():
            assert channel["status"] in {"passed-original-assertions", "skipped-original-channel"}
    assert all(sha(Path(p)) == digest for p, digest in initial.items())
    (folder / "result.json").write_text(json.dumps({"command": command,
        "exitCode": exit_code, "requested": len(rows), "observed": len(actual),
        "outputSha256": sha(output)}, indent=2))
    return actual, output
for offset in range(0, len(requests), 100):
    rows = requests[offset:offset + 100]
    actual, output = capture(rows, destination / f"batch-{offset // 100:04d}")
    for x in actual:
        statuses[x["status"]] += 1
        if x["status"] == "passed-original-full-harness" and not x["channels"]["sourceMapRecord"]["baselines"]:
            records_missing.append(x["name"])
    with gzip.open(destination / "original.jsonl.gz", "ab") as aggregate:
        aggregate.write(output.read_bytes())
    output.unlink()
    observed += len(actual)
    (destination / "progress.json").write_text(json.dumps({"observed": observed, "target": 15330}))
assert statuses == {"passed-original-full-harness": 13571,
                   "original-option-policy-disposition": 1720,
                   "skipped-original-before-configuration": 39}
(destination / "result.json").write_text(json.dumps({"observedTotal": observed,
    "statuses": dict(statuses), "frozenInputs": initial}, indent=2))
# The observer compares global recorded-baseline names before/after a channel.
# Earlier .d.ts/.ts pairs can reuse a .sourcemap.txt name within a process.
# Recover missing records in a fresh original process; do not edit the main reference.
supplement_rows = [x for x in requests if x["Name"] in set(records_missing)]
assert len(supplement_rows) == 11
actual, output = capture(supplement_rows,
    root / "artifacts/emit/whole-reference-record-supplement")
assert all(x["status"] == "passed-original-full-harness" and
           x["channels"]["sourceMapRecord"]["baselines"] for x in actual)
```

The supplement contains11 original records, not revised expected content. Consumers
use it only when the main record lacks that channel baseline and verify exact
name/source/configuration identity. The original output-channel skips, option-policy
skips and preparation skips remain separate. Any new original failure is a failure,
not permission to regenerate a different expectation.

## Reproduce the122 selected original xUnit inputs

The following frozen IDs preserve the actual final selection independently of ignored
development artifacts. They include applicable repaired cases and representatives of
source maps, declarations, module formats, JSX, async/classes/decorators/using/operators,
output settings, content mapping, JS/JSON, Unicode and diagnostics. All must have
`passed-original-full-harness` disposition in the regenerated original reference.
Six incremental-owned build-info metadata cases remain excluded and visibly blocked in
the full standalone corpus, never counted as original xUnit passes.

<!-- emit-selected-identities -->
```json
[
  "ClassDeclaration10.ts", "ClassDeclaration11.ts", "ClassDeclaration13.ts", "allowJsClassThisTypeCrash.ts",
  "allowJsCrossMonorepoPackage.ts", "allowJscheckJsTypeParameterNoCrash.ts", "ambientRequireFunction.ts module=commonjs", "argumentsSpreadRestIterables.tsx target=esnext",
  "asyncArrowInClassES5.ts target=es2015", "asyncArrowStaticFieldThis.ts", "asyncAwaitWithCapturedBlockScopeVar.ts target=es2015", "augmentedTypesModules.ts",
  "augmentedTypesModules2.ts", "bom-utf8.ts", "booleanLiteralsContextuallyTypedFromUnion.tsx", "callsOnComplexSignatures.tsx",
  "classFieldsAssignmentNamedEvaluation.ts", "classFieldsBrokenConstructorEmitNoCrash1.ts", "classFieldsNamedEvaluationDestructuringAssignment.ts", "collisionCodeGenModuleWithUnicodeNames.ts",
  "constructorWithIncompleteTypeAnnotation.ts", "contentMapperAliasDiagnostic.ts", "contentMapperDeclarationEmit.ts", "contentMapperDeclarationEmitFailure.ts",
  "contentMapperDiagnosticDirectives.ts", "contentMapperDiagnostics.ts", "contentMapperSupplementalDiagnostics.ts", "contentMapperSupplementalModule.ts",
  "contentMapperSynthesizedUnusedDiagnostics.ts", "contextualOverloadListFromUnionWithPrimitiveNoImplicitAny.ts", "emitBOM.ts", "enumUnicodeEscapeMemberName.ts",
  "errorMessageOnIntersectionsWithDiscriminants01.ts", "errorMessageOnObjectLiteralType.ts", "errorMessagesIntersectionTypes01.ts", "exportAssignmentMerging5.ts",
  "exportAssignmentMerging6.ts", "inlineSourceMap.ts target=es2015", "inlineSourceMap2.ts target=es2015", "isolatedModules_resolveJsonModule_strict_outDir_commonJs.ts",
  "jsDeclarationEmitDoesNotReuseUnrelatedJSDocImport.ts", "jsdocModifiersInObjectLiteral.ts checkjs=false", "jsdocModifiersInObjectLiteral.ts checkjs=true", "modulePreserve4.ts",
  "multipleModuleExportsAssignments.ts", "noCheckDoesNotReportError.ts", "noCheckNoEmit.ts", "noCheckRequiresEmitDeclarationOnly.ts",
  "noFlakyDiagnosticsUntypedModule1.ts", "nullishCoalescingAlwaysNullFalsePositive.ts", "objectRestBindingContextualInference.ts", "objectRestSpread.ts",
  "signatureOverloadsWithComments.ts", "sourceMap-Comment1.ts target=es2015", "symlinkedPackageReachedRelativelyIsNotExternalLibrary.ts", "symlinkedWorkspaceDependenciesNoDirectLinkGeneratesDeepNonrelativeName.ts",
  "symlinkedWorkspaceDependenciesNoDirectLinkGeneratesNonrelativeName.ts", "usingDeclarationWithNewline.ts", "usingDeclarationsHoistedExportOrder.ts", "usingDeclarationsNestedRest.ts",
  "exportNamespace_js.ts", "checkJsdocTypeTag2.ts", "jsDeclarationsExportForms.ts target=es2015", "jsDeclarationsExportFormsErr.ts target=es2015",
  "jsDeclarationsFunctionClassesCjsExportAssignment.ts target=es2015", "jsDeclarationsFunctionLikeClasses2.ts target=es2015", "jsDeclarationsImportNamespacedType.ts", "jsDeclarationsReferenceToClassInstanceCrossFile.ts",
  "moduleExportsElementAccessAssignment.ts", "tsxReactEmitNesting.tsx", "nodeAllowJsPackageSelfName.ts module=node16", "nodeAllowJsPackageSelfName.ts module=node18",
  "nodeAllowJsPackageSelfName.ts module=node20", "nodeAllowJsPackageSelfName.ts module=nodenext", "nodeAllowJsPackageSelfName2.ts", "nodeModulesAllowJsConditionalPackageExports.ts module=node16",
  "nodeModulesAllowJsConditionalPackageExports.ts module=node18", "nodeModulesAllowJsConditionalPackageExports.ts module=node20", "nodeModulesAllowJsConditionalPackageExports.ts module=nodenext", "nodeModulesAllowJsExportAssignment.ts module=node16",
  "nodeModulesAllowJsExportAssignment.ts module=node18", "nodeModulesAllowJsExportAssignment.ts module=node20", "nodeModulesAllowJsExportAssignment.ts module=nodenext", "nodeModulesAllowJsImportHelpersCollisions1.ts module=node16",
  "nodeModulesAllowJsImportHelpersCollisions1.ts module=node18", "nodeModulesAllowJsImportHelpersCollisions1.ts module=node20", "nodeModulesAllowJsImportHelpersCollisions1.ts module=nodenext", "nodeModulesAllowJsPackageExports.ts module=node16",
  "nodeModulesAllowJsPackageExports.ts module=node18", "nodeModulesAllowJsPackageExports.ts module=node20", "nodeModulesAllowJsPackageExports.ts module=nodenext", "nodeModulesAllowJsPackageImports.ts module=node16",
  "nodeModulesAllowJsPackageImports.ts module=node18", "nodeModulesAllowJsPackageImports.ts module=node20", "nodeModulesAllowJsPackageImports.ts module=nodenext", "nodeModulesAllowJsPackagePatternExports.ts module=node16",
  "nodeModulesAllowJsPackagePatternExports.ts module=node18", "nodeModulesAllowJsPackagePatternExports.ts module=node20", "nodeModulesAllowJsPackagePatternExports.ts module=nodenext", "nodeModulesAllowJsPackagePatternExportsExclude.ts module=node16",
  "nodeModulesAllowJsPackagePatternExportsExclude.ts module=node18", "nodeModulesAllowJsPackagePatternExportsExclude.ts module=node20", "nodeModulesAllowJsPackagePatternExportsExclude.ts module=nodenext", "nodeModulesAllowJsPackagePatternExportsTrailers.ts module=node16",
  "nodeModulesAllowJsPackagePatternExportsTrailers.ts module=node18", "nodeModulesAllowJsPackagePatternExportsTrailers.ts module=node20", "nodeModulesAllowJsPackagePatternExportsTrailers.ts module=nodenext", "assignmentToVoidZero2.ts",
  "commonJSAliasedExport.ts", "commonJSImportClassTypeReference.ts", "commonJSImportExportedClassExpression.ts", "commonJSImportNestedClassTypeReference.ts",
  "expandoOnAlias.ts", "moduleExportDuplicateAlias.ts", "moduleExportDuplicateAlias2.ts", "moduleExportDuplicateAlias3.ts",
  "namespaceAssignmentToRequireAlias.ts", "nestedDestructuringOfRequire.ts", "requireOfESWithPropertyAccess.ts", "typeFromPrototypeAssignment4.ts",
  "contextuallyTypedClassExpressionMethodDeclaration02.ts", "contextualTypeWithUnionTypeCallSignatures.ts"
]
```
<!-- /emit-selected-identities -->

This short extraction preserves registry order, source paths/hashes and configuration:

```python
import gzip, json
from pathlib import Path
root = Path.cwd()
text = (root / "docs/EMIT-REFERENCE.md").read_text()
block = text.split("<!-- emit-selected-identities -->")[1].split("<!-- /emit-selected-identities -->")[0]
names = set(json.loads(block.split("```json\n")[1].split("```")[0]))
with gzip.open(root / "artifacts/emit/whole-reference/original.jsonl.gz", "rt") as f:
    dispositions = {x["name"]: x["status"] for x in map(json.loads, f)}
assert len(names) == 122 and all(dispositions[n] == "passed-original-full-harness" for n in names)
registry = root / "artifacts/emit/reference-infrastructure/registry/requests.jsonl"
rows = [json.loads(s) for s in registry.read_text().splitlines()]
selected = [x for x in rows if x["Name"] in names]
assert len(selected) == 122
out = root / "artifacts/emit/final-managed"
out.mkdir(parents=True, exist_ok=True)
(out / "requests.jsonl").write_text("".join(json.dumps(x, separators=(",", ":")) + "\n" for x in selected))
```

The full standalone comparison still observes every15,330 original configuration and
all nine channels. It retains six explicit external incremental metadata discrepancies;
selection does not hide or weaken those results. Run the existing
[bounded consumer launcher](../tools/run-emit-corpus.py) with fresh results, and set
`EMIT_CORPUS_ROOT`, `EMIT_CORPUS_REQUESTS`, `EMIT_CORPUS_REFERENCE` and
`EMIT_CORPUS_ACTUAL` for [original xUnit assertions](../tests/Emit.Tests/OriginalProgramEmitTests.cs).
See [verification instructions](EMIT-VERIFY.md) for final coordinated managed/Native gates.
