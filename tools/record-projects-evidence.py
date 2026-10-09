#!/usr/bin/env python3
"""Record current successful bounded-project evidence, preserving prior accepted proofs."""
import datetime
import importlib.util
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main():
    spec = importlib.util.spec_from_file_location("projects_gate", ROOT / "tools/run-projects-gate.py")
    gate_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate_module)
    gate = gate_module.read("artifacts/projects-validation/final-gate.json")
    if gate["exitCode"] or any(gate[name] for name in ("sourceDrift", "assetDrift", "historicalAssetDrift", "upstreamDrift")):
        raise RuntimeError("Project gate failed or drifted")
    if gate_module.sources() != gate["sourceSha256"] or gate_module.fingerprint(gate_module.assets()) != gate["verificationAssetsSha256"]:
        raise RuntimeError("Current source/assets differ from successful project gate")
    if gate_module.historical() != gate["protectedHistoricalAssetFiles"]:
        raise RuntimeError("Protected prior fixtures/proofs differ from successful gate")
    execution = gate_module.check_execution()
    binaries = gate_module.native_snapshot()
    for name, row in binaries.items():
        row["file"] = subprocess.check_output(["file", str(ROOT / name)], text=True).strip()
        if "ELF 64-bit" not in row["file"] or "x86-64" not in row["file"]:
            raise RuntimeError("Expected actual Linux x64 native executable: " + name)
        dependencies = subprocess.run(["ldd", str(ROOT / name)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        row["dependencies"] = dependencies.stdout.strip()
        if "libcoreclr" in row["dependencies"]:
            raise RuntimeError("Unexpected managed runtime dependency: " + name)
    # Link current regression execution; never run or overwrite historical recorders.
    retained_path = "artifacts/collections-validation/native-after-collections.json"
    result = {
        "stage": "bounded-relative-es-module-projects", "baseline": gate_module.BASELINE,
        "upstreamRevision": gate_module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        "sourceDrift": False, "assetDrift": False, "historicalAssetDrift": False, "upstreamDrift": False,
        "log": gate["log"], "logSha256": gate_module.sha(gate["log"]),
        "nativeBinaries": binaries,
        "nativeFingerprintScope": "Nine freshly published native binaries: one retained collection/regression cascade, project validation and ordinary disk CLI; captured before project execution and checked afterward.",
        "xUnit": execution["xUnit"], "xUnitCases": sum(row["counts"]["total"] for row in execution["xUnit"].values()),
        "projectNative": execution["native"], "originalGoProjects": execution["originalGo"],
        "projectOrderControls": execution["order"], "ordinaryNativeCLI": execution["cli"],
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "originalHelperProjections": execution["originalHelpers"],
        "originalDirectTests": execution["originalDirectTests"],
        "originalGoToolchain": {"executable": str(gate_module.GO_TOOL.relative_to(ROOT)), "sha256": gate_module.sha(gate_module.GO_TOOL),
                                "version": subprocess.check_output([str(gate_module.GO_TOOL), "version"], text=True).strip()},
        "projectSharedChecks": {"count": gate_module.manifest_groups()["sharedChecks"]["count"], "output": "artifacts/projects-validation/native-controls.txt", "outputSha256": gate_module.sha("artifacts/projects-validation/native-controls.txt")},
        "retainedCollectionExecution": {"binaryGuard": retained_path, "sha256": gate_module.sha(retained_path), "result": gate_module.retained.check_execution()},
        "fixedLibraryManifest": gate_module.retained.validate_libraries(), "fixedLibraryManifestSha256": gate_module.sha(gate_module.retained.LIBRARIES),
        "corpusManifest": gate_module.MANIFEST, "corpusManifestSha256": gate_module.sha(gate_module.MANIFEST),
        "contract": "docs/PROJECTS-CONTRACT.md",
        "limits": [
            "Bounded Linux case-sensitive relative .ts ES-module projects under authored strict tscheck.json; not general tsconfig/Program/package/project-reference/emit/service parity.",
            "One shared checker over discovered users plus three authentic declaration roots; demanded library declarations resolve normally, skipLibCheck skips library bodies.",
            "Admitted imports/exports and accepted collection language compose across files; prior unsupported language and broader-library boundaries remain explicit.",
            "Original Program/default resolver differentials, source-derived helper projections and shared authored/API controls retain their individual provenance.",
            "Malformed source reports parse failures without claiming complete semantic checking. Ordinary CLI execution requires neither Go nor an upstream checkout.",
        ],
    }
    destination = ROOT / "validation/results/projects-verified.json"
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "sourceSha256": result["sourceSha256"], "xUnitCases": result["xUnitCases"], "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
