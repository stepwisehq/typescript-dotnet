#!/usr/bin/env python3
"""Record only a successful current resolution gate; preserve prior proofs."""
import datetime
import importlib.util
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main():
    spec = importlib.util.spec_from_file_location("resolution_gate", ROOT / "tools/run-resolution-gate.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    gate = module.read("artifacts/resolution-validation/final-gate.json")
    if gate["exitCode"] or any(gate[name] for name in ("sourceDrift", "assetDrift", "historicalAssetDrift", "upstreamDrift", "obligationDrift")):
        raise RuntimeError("Resolution gate failed/drifted")
    if module.sources() != gate["sourceSha256"] or module.fingerprint(module.assets()) != gate["verificationAssetsSha256"] or module.historical() != gate["protectedHistoricalAssetFiles"]:
        raise RuntimeError("Current source/assets/historical proofs differ from successful gate")
    if module.obligations() != gate["reviewedAssertionObligations"]:
        raise RuntimeError("Reviewed assertion obligations changed after gate")
    execution = module.check_execution()
    binaries = module.native_snapshot()
    for name, row in binaries.items():
        row["file"] = subprocess.check_output(["file", str(ROOT / name)], text=True).strip()
        if "ELF 64-bit" not in row["file"] or "x86-64" not in row["file"]:
            raise RuntimeError("Expected actual Linux x64 native executable: " + name)
        dependencies = subprocess.run(["ldd", str(ROOT / name)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        row["dependencies"] = dependencies.stdout.strip()
        if "libcoreclr" in row["dependencies"].lower():
            raise RuntimeError("Unexpected managed runtime dependency")
    retained_tests = execution["retainedExecution"]["retainedExecution"]["xUnit"]
    result = {
        "stage": "complete-source-import-module-resolution", "baseline": module.BASELINE,
        "upstreamRevision": module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        "sourceDrift": False, "assetDrift": False, "historicalAssetDrift": False, "upstreamDrift": False, "obligationDrift": False,
        "reviewedAssertionObligations": gate["reviewedAssertionObligations"],
        "log": gate["log"], "logSha256": module.sha(gate["log"]),
        "nativeBinaries": binaries,
        "nativeFingerprintScope": "Resolution validation and ordinary CLI captured before new execution; retained configuration and project cascades capture their own native/oracle binaries before their execution. Every physical path is inspected and stage pre/post hashes are verified.",
        "resolutionNative": execution["native"], "originalGoResolution": execution["originalGo"],
        "resolutionSharedChecks": module.manifest_groups()["sharedChecks"],
        "resolutionXUnit": execution["resolutionXUnit"], "retainedXUnit": retained_tests,
        "xUnitCases": execution["resolutionXUnit"]["counts"]["total"] + execution["retainedExecution"]["configurationXUnit"]["counts"]["total"] + sum(row["counts"]["total"] for row in retained_tests.values()),
        "resolutionOrderControls": execution["order"], "ordinaryNativeCLI": execution["cli"],
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "pristineParity": execution["pristineParity"], "graphOracle": execution["graphOracle"], "originalDirectTests": execution["originalDirectTests"],
        "originalGoToolchain": {"executable": str(module.GO_TOOL.relative_to(ROOT)), "sha256": module.sha(module.GO_TOOL), "version": subprocess.check_output([str(module.GO_TOOL), "version"], text=True).strip()},
        "retainedConfigurationExecution": execution["retainedExecution"],
        "fixedLibraryManifest": module.retained.retained.retained.validate_libraries(),
        "corpusManifest": module.MANIFEST, "corpusManifestSha256": module.sha(module.MANIFEST),
        "contract": "docs/RESOLUTION-CONTRACT.md",
        "limits": [
            "Complete five-file source-import Module resolution, caches and source entry APIs; general Program/checker/emitter execution remains external.",
            "Graph observes selected original loading, scheduling, references, metadata and diagnostic publication; the approved original observer overlay skips default-lib-reference scheduling, separately recorded from ordinary API parity.",
            "A pristine original companion rejects only its unavailable authored graph observer entry. All ordinary canonical response bytes match the overlaid oracle and all 38 original Module bodies replay without an overlay.",
            "The original vfstest-backed host and public result/trace/package-cache observations do not expose every private lazy cache or compiler lifecycle; exact identity and gated concurrency assertions are separate.",
            "Checking a graph retains explicit checker syntax/type capabilities and named legacy host policy. Resolution and graph inspection do not turn unsupported semantic forms into accepted checker execution.",
            "Virtual host paths preserve Go-byte identity; ordinary disk inspection retains the explicit scalar-UTF8 filesystem boundary and previously recorded read-side OS limits.",
        ],
    }
    destination = ROOT / "validation/results/resolution-verified.json"
    if destination.exists():
        raise RuntimeError("Resolution evidence already exists; preserve prior proof")
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "sourceSha256": result["sourceSha256"], "xUnitCases": result["xUnitCases"], "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
