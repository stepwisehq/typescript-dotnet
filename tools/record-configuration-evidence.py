#!/usr/bin/env python3
"""Record only a successful current configuration gate; preserve prior proofs."""
import datetime
import importlib.util
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main():
    spec = importlib.util.spec_from_file_location("configuration_gate", ROOT / "tools/run-configuration-gate.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    gate = module.read("artifacts/configuration-validation/final-gate.json")
    if gate["exitCode"] or any(gate[name] for name in ("sourceDrift", "assetDrift", "historicalAssetDrift", "upstreamDrift", "obligationDrift")):
        raise RuntimeError("Configuration gate failed/drifted")
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
    retained_tests = execution["retainedExecution"]["xUnit"]
    result = {
        "stage": "complete-configuration-interpretation-discovery", "baseline": module.BASELINE,
        "upstreamRevision": module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        "sourceDrift": False, "assetDrift": False, "historicalAssetDrift": False, "upstreamDrift": False, "obligationDrift": False,
        "reviewedAssertionObligations": gate["reviewedAssertionObligations"],
        "log": gate["log"], "logSha256": module.sha(gate["log"]),
        "nativeBinaries": binaries,
        "nativeFingerprintScope": "Configuration validation and ordinary CLI captured before new execution; nine retained native paths captured by the single Projects cascade. Eleven physical paths represent ten unique targets because unchanged retained tooling publishes tscheck again. Each stage checks its own pre/post fingerprints.",
        "configurationNative": execution["native"], "originalGoConfiguration": execution["originalGo"],
        "configurationSharedChecks": module.manifest_groups()["sharedChecks"],
        "configurationXUnit": execution["configurationXUnit"], "retainedXUnit": retained_tests,
        "xUnitCases": execution["configurationXUnit"]["counts"]["total"] + sum(row["counts"]["total"] for row in retained_tests.values()),
        "configurationOrderControls": execution["order"], "ordinaryNativeCLI": execution["cli"],
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "originalHelperProjections": execution["originalHelpers"], "originalDirectTests": execution["originalDirectTests"],
        "originalGoToolchain": {"executable": str(module.GO_TOOL.relative_to(ROOT)), "sha256": module.sha(module.GO_TOOL), "version": subprocess.check_output([str(module.GO_TOOL), "version"], text=True).strip()},
        "retainedProjectExecution": execution["retainedExecution"],
        "fixedLibraryManifest": module.retained.retained.validate_libraries(),
        "corpusManifest": module.MANIFEST, "corpusManifestSha256": module.sha(module.MANIFEST),
        "contract": "docs/CONFIGURATION-CONTRACT.md",
        "limits": [
            "Complete configuration interpretation/discovery and option metadata; actual emit/build/watch/content-mapper processes remain external.",
            "Selected JSON grammar/error/raw-container and locale language parser dependencies have individually documented assertion subsets; no general JSON streaming/reflection or x/text matcher/collator claim.",
            "Original observation canonicalizes only the own randomized cannot/unable modal of direct typed json/v2.SemanticError; original raw stderr is retained and hashed. User/plain/CLI diagnostic text is unchanged, with six current-original controls required.",
            "Raw partial-output assertions use the source OrderedMap API for deterministic multi-key traversal and ordinary maps with one key. Ordinary multi-key Go map traversal order and its correlated error offsets/partial bytes are not asserted as deterministic.",
            "Read-side Linux x64 filesystem adapter has explicit Unicode-scalar disk boundary, stat time precision and opaque Sys exclusions; virtual paths preserve raw byte identity.",
            "Checking authentic interpreted options remains subject to explicit downstream checker capabilities; inspection does not construct a checker.",
        ],
    }
    destination = ROOT / "validation/results/configuration-verified.json"
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "sourceSha256": result["sourceSha256"], "xUnitCases": result["xUnitCases"], "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
