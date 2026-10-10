#!/usr/bin/env python3
"""Record only a successful current Program gate; preserve every prior proof."""
import datetime
import importlib.util
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main():
    spec = importlib.util.spec_from_file_location("program_gate", ROOT / "tools/run-program-gate.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    gate = module.read("artifacts/program-validation/final-gate.json")
    drift = ("sourceDrift", "assetDrift", "historicalAssetDrift", "upstreamDrift", "obligationDrift")
    if gate["exitCode"] or any(gate[name] for name in drift):
        raise RuntimeError("Program gate failed/drifted")
    if module.sources() != gate["sourceSha256"] or module.fingerprint(module.assets()) != gate["verificationAssetsSha256"] or module.historical() != gate["protectedHistoricalAssetFiles"]:
        raise RuntimeError("Current source/assets/historical proofs differ from successful gate")
    if module.obligations() != gate["reviewedAssertionObligations"]:
        raise RuntimeError("Reviewed Program responsibility/assertion obligations changed after gate")
    execution = module.check_execution()
    manifest = module.manifest_groups()
    binaries = module.native_snapshot()
    for name, row in binaries.items():
        row["file"] = subprocess.check_output(["file", str(ROOT / name)], text=True).strip()
        if "ELF 64-bit" not in row["file"] or "x86-64" not in row["file"]:
            raise RuntimeError("Expected actual Linux x64 native executable: " + name)
        dependencies = subprocess.run(["ldd", str(ROOT / name)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        row["dependencies"] = dependencies.stdout.strip()
        if "libcoreclr" in row["dependencies"].lower():
            raise RuntimeError("Unexpected managed runtime dependency")
    resolution = execution["retainedExecution"]
    configuration = resolution["retainedExecution"]
    retained_tests = configuration["retainedExecution"]["xUnit"]
    cases = execution["programXUnit"]["counts"]["total"] + resolution["resolutionXUnit"]["counts"]["total"] + configuration["configurationXUnit"]["counts"]["total"] + sum(row["counts"]["total"] for row in retained_tests.values())
    result = {
        "stage": "program-construction-environment-assembly", "baseline": module.BASELINE,
        "upstreamRevision": module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        **{name: False for name in drift}, "reviewedAssertionObligations": gate["reviewedAssertionObligations"],
        "log": gate["log"], "logSha256": module.sha(gate["log"]),
        "nativeBinaries": binaries, "managedBinaries": execution["managedBinaries"],
        "nativeFingerprintScope": "Program validation and ordinary CLI captured before native execution; each retained cascade captures its own targets before execution. All physical paths are inspected and stage pre/post hashes verified.",
        "programNative": execution["native"], "programManaged": execution["managed"],
        "originalGoProgram": execution["original"]["corpus"], "originalDirectTests": execution["original"]["directTests"],
        "typedJSONObservationControls": execution["original"]["jsonErrorProjection"],
        "programSharedChecks": manifest["sharedChecks"], "programXUnit": execution["programXUnit"],
        "focusedRetainedConsumers": execution["retainedConsumers"], "retainedXUnit": retained_tests, "xUnitCases": cases,
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "originalGoToolchain": {"executable": str(module.GO_TOOL.relative_to(ROOT)), "sha256": module.sha(module.GO_TOOL), "version": subprocess.check_output([str(module.GO_TOOL), "version"], text=True).strip()},
        "retainedResolutionExecution": resolution,
        "fixedLibraryManifest": module.retained.retained.retained.retained.validate_libraries(),
        "bundledLibraryManifest": "src/TypeScript.Scanner/Bundled/manifest.json",
        "bundledLibraryManifestSha256": module.sha("src/TypeScript.Scanner/Bundled/manifest.json"),
        "corpusManifest": module.MANIFEST, "corpusManifestSha256": module.sha(module.MANIFEST),
        "contract": "docs/PROGRAM-CONTRACT.md",
        "limits": [
            "Fresh Program construction, local Update/Reuse, source lazy CheckerPool policies and genuine source/library/type/reference environments; no emitter, incremental builder, watch or services execution.",
            "Construction and inspection do not acquire a semantic checker. Actual semantic acquisition retains loaded options/files and explicitly admits only the accepted checker option/declaration profile; unsupported environments reject.",
            "In-process injected content mapper callback/results, transform validation, SpanMap and Program consumers are selected. Subprocess/IPC host execution and the generator/yield semantic mapper body remain external; adapted assertion channels are recorded separately.",
            "Authentic115 bundled library bytes and read-side caches/symlinks are selected; writable cache/bundled forwarding, nonembedded build variant and ThrottleGroup remain external.",
            "Enabled performance tracing/type tracing remain explicitly unsupported; disabled tracing guards and actual host diagnostic Trace callbacks are retained.",
            "Only an actual original json/v2.SemanticError own modal verb is canonically observed as source-sanctioned cannot/unable rendering. Raw original typed error stderr is retained; user/compiler/CLI diagnostic text is not normalized. Original-only typed offset/pointer/cause context is not fabricated as managed decoder metadata.",
            "Exact public records, original identity/lifetime/policy assertions and concrete scheduling controls establish their stated behavior; no exhaustive private representation/storage equivalence or per-function execution claim.",
            "Virtual paths preserve Go-byte identity. Ordinary disk inspection retains explicit scalar-UTF8 host boundaries and previously recorded read-side OS limitations.",
        ],
    }
    destination = ROOT / "validation/results/program-verified.json"
    if destination.exists():
        raise RuntimeError("Program evidence already exists; preserve prior proof")
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "sourceSha256": result["sourceSha256"], "xUnitCases": cases, "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
