#!/usr/bin/env python3
"""Record successful current-source collection evidence; preserve earlier proofs."""
import datetime
import importlib.util
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main():
    spec = importlib.util.spec_from_file_location("collections_gate", ROOT / "tools/run-collections-gate.py")
    gate_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate_module)
    gate = gate_module.read("artifacts/collections-validation/final-gate.json")
    if gate["exitCode"] or any(gate[key] for key in ("sourceDrift", "assetDrift", "historicalAssetDrift")):
        raise RuntimeError("Collection gate failed or drifted")
    if gate["sourceSha256"] != gate_module.sources() or gate["verificationAssetsSha256"] != gate_module.fingerprint(gate_module.assets()):
        raise RuntimeError("Current source/assets differ from successful collection gate")
    if gate_module.historical() != gate["protectedHistoricalAssetFiles"]:
        raise RuntimeError("Historical fixture/proof set differs from successful gate")
    execution = gate_module.check_execution()
    libraries = gate_module.validate_libraries()
    binaries = gate_module.native_snapshot()
    for path, data in binaries.items():
        data["file"] = subprocess.check_output(["file", str(ROOT / path)], text=True).strip()
        if "ELF 64-bit" not in data["file"] or "x86-64" not in data["file"]:
            raise RuntimeError("Expected actual Linux x64 native executable: " + path)
        dependencies = subprocess.run(["ldd", str(ROOT / path)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        data["dependencies"] = dependencies.stdout.strip()
        if "libcoreclr" in data["dependencies"]:
            raise RuntimeError("Unexpected managed runtime dependency: " + path)
    retained_paths = {
        "callableNative": "artifacts/callables-validation/native/report.json",
        "callableControls": "artifacts/callables-validation/native-boundary.json",
        "callableOrder": "artifacts/callables-validation/native-order/report.json",
        "callableNumeric": "artifacts/callables-validation/native-numeric.json",
        "bindingTargeted": "artifacts/binding-completion-validation/targeted-native/report.json",
        "bindingBroad": "artifacts/binding-completion-validation/broad-native/report.json",
        "bindingSharedCases": "artifacts/binding-completion-validation/native-counts.json",
        "originalGoNumericFinite": "artifacts/binding-completion-validation/original-go-numeric-finite.json",
        "foundationDirect": "artifacts/foundation-validation/native-direct.json",
        "foundationFiniteFuzz": "artifacts/foundation-validation/native-fuzz.json",
        "foundationBroadSyntax": "artifacts/foundation-validation/broad-parser-native/report.json",
        "foundationBroadScanner": "artifacts/foundation-validation/broad-scanner-native/report.json",
        "historicalBinder": "artifacts/binder-validation/native-result.json",
        "dataTypes": "artifacts/data-types-validation/native-result.json",
        "objects": "artifacts/data-types-validation/object-native-result.json",
        "scalars": "artifacts/data-types-validation/scalar-native-result.json",
        "legacyControls": "artifacts/data-types-validation/native-boundary.json",
    }
    regressions = {name: {"path": path, "sha256": gate_module.sha(path), "result": gate_module.read(path)}
                   for name, path in retained_paths.items()}
    manifest = gate_module.manifest_groups()
    result = {
        "stage": "generic-collections", "baseline": gate_module.BASELINE,
        "upstreamRevision": gate_module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        "sourceDrift": False, "assetDrift": False, "historicalAssetDrift": False,
        "log": gate["log"], "logSha256": gate_module.sha(gate["log"]),
        "nativeBinaries": binaries,
        "nativeFingerprintScope": "Seven freshly published native binaries captured after the retained cascade and Collections publication, before Collections execution, and checked afterward. The one retained cascade also keeps its original execution guards.",
        "xUnit": execution["xUnit"],
        "xUnitCases": sum(row["counts"]["total"] for row in execution["xUnit"].values()),
        "collectionNative": execution["native"], "originalGoCollections": execution["originalGo"],
        "collectionSharedChecks": {"count": manifest["sharedChecks"]["count"], "output": "artifacts/collections-validation/native-controls.txt", "outputSha256": gate_module.sha("artifacts/collections-validation/native-controls.txt")},
        "collectionOrderControls": execution["order"], "originalInstrumentationParity": execution["instrumentationParity"],
        "originalSupportOperations": execution["originalSupport"],
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "fixedLibraryManifest": libraries, "fixedLibraryManifestSha256": gate_module.sha(gate_module.LIBRARIES),
        "exampleManifest": gate_module.read(gate_module.EXAMPLES), "exampleManifestSha256": gate_module.sha(gate_module.EXAMPLES),
        "regressions": regressions,
        "corpusManifest": gate_module.MANIFEST, "corpusManifestSha256": gate_module.sha(gate_module.MANIFEST),
        "contract": "docs/COLLECTIONS-CONTRACT.md",
        "limits": [
            "Single user TS script with fresh complete pinned ES5/decorators/decorators.legacy roots, ESNext target and skipLibCheck; demanded library types are resolved, library bodies are not comprehensively checked.",
            "Selected generic/collection capability under the fixed Collections option profile; not whole-checker or all-loaded-library API parity.",
            "Authored independent-Go differentials and shared API/support assertions retain their stated provenance; adapted direct-test subsets do not claim full upstream assertion coverage.",
            "Source classes/new, namespaces/modules, async/custom iterators, generic variadic tuples, template/key-remapped types and intrinsic string transforms remain explicit contract exclusions; required internal library/inference dependencies are distinct.",
        ],
    }
    destination = ROOT / "validation/results/collections-verified.json"
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "sourceSha256": result["sourceSha256"], "xUnitCases": result["xUnitCases"], "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
