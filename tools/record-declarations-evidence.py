#!/usr/bin/env python3
"""Record one successful frozen declaration gate; preserve accepted historical evidence."""
import datetime
import importlib.util
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    spec = importlib.util.spec_from_file_location("declarations_gate", ROOT / "tools/run-declarations-gate.py")
    gate_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate_module)
    gate = gate_module.read("artifacts/declarations-validation/final-gate.json")
    drift = ("sourceDrift", "assetDrift", "historicalAssetDrift", "upstreamDrift", "obligationDrift")
    if gate["exitCode"] or any(gate[name] for name in drift):
        raise RuntimeError("Declaration gate failed or drifted")
    if gate_module.sources() != gate["sourceSha256"] or gate_module.fingerprint(gate_module.assets()) != gate["verificationAssetsSha256"]:
        raise RuntimeError("Current source/assets differ from the successful declaration gate")
    if gate_module.historical() != gate["protectedHistoricalAssetFiles"] or gate_module.obligations() != gate["reviewedAssertionObligations"]:
        raise RuntimeError("Historical evidence or reviewed declaration assertions changed")
    execution = gate_module.check_execution()
    manifest = gate_module.manifest_groups()
    binaries = gate_module.native_snapshot()
    for name, row in binaries.items():
        row["file"] = subprocess.check_output(["file", str(ROOT / name)], text=True).strip()
        if "ELF 64-bit" not in row["file"] or "x86-64" not in row["file"]:
            raise RuntimeError("Expected actual Linux x64 native executable: " + name)
        result = subprocess.run(["ldd", str(ROOT / name)], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        row["dependencies"] = result.stdout.strip()
        if "libcoreclr" in row["dependencies"].lower():
            raise RuntimeError("Unexpected managed runtime dependency")
    program = execution["retainedExecution"]
    resolution = program["retainedExecution"]
    configuration = resolution["retainedExecution"]
    projects = configuration["retainedExecution"]
    retained_tests = projects["xUnit"]
    cases = (execution["declarationsXUnit"]["counts"]["total"] + program["programXUnit"]["counts"]["total"]
             + resolution["resolutionXUnit"]["counts"]["total"] + configuration["configurationXUnit"]["counts"]["total"]
             + sum(row["counts"]["total"] for row in retained_tests.values()))
    result = {
        "stage": "checker-initialization-declaration-object-model", "baseline": gate_module.BASELINE,
        "upstreamRevision": gate_module.read("upstream.lock.json")["revision"],
        "recordedAtUTC": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "command": gate["command"], "exitCode": 0, "elapsedSeconds": gate["elapsedSeconds"],
        "sourceSha256": gate["sourceSha256"], "verificationAssetsSha256": gate["verificationAssetsSha256"],
        **{name: False for name in drift}, "reviewedAssertionObligations": gate["reviewedAssertionObligations"],
        "log": gate["log"], "logSha256": gate_module.sha(gate["log"]),
        "nativeBinaries": binaries, "managedBinaries": execution["managedBinaries"],
        "nativeFingerprintScope": "New declaration validation/CLI binaries captured before execution; one retained Program cascade independently captures its targets. Each actual native path is inspected and all stage hashes checked.",
        "declarationsManaged": execution["managed"], "declarationsNative": execution["native"],
        "declarationsXUnit": execution["declarationsXUnit"], "sharedChecks": manifest["sharedChecks"],
        "originalDirectTests": execution["original"]["directTests"], "originalGoCorpus": execution["original"]["corpus"],
        "focusedRetainedConsumers": execution["retainedConsumers"], "retainedProgramExecution": program,
        "xUnitCases": cases, "currentCheckingEnvironment": manifest["currentCheckingEnvironment"],
        "currentCheckingReplacement": manifest["currentCheckingReplacement"],
        "originalOracleBinaries": execution["originalOracleBinaries"],
        "originalGoToolchain": {"executable": str(gate_module.GO_TOOL.relative_to(ROOT)), "sha256": gate_module.sha(gate_module.GO_TOOL),
                                "version": subprocess.check_output([str(gate_module.GO_TOOL), "version"], text=True).strip()},
        "bundledLibraryManifest": "src/TypeScript.Scanner/Bundled/manifest.json",
        "bundledLibraryManifestSha256": gate_module.sha("src/TypeScript.Scanner/Bundled/manifest.json"),
        "corpusManifest": gate_module.MANIFEST, "corpusManifestSha256": gate_module.sha(gate_module.MANIFEST),
        "semanticObservation": manifest["semanticObservation"],
        "authoredControlRetirement": manifest["authoredControlRetirement"],
        "contract": "docs/DECLARATIONS-CONTRACT.md",
        "limits": [
            "Genuine NewChecker/Program options, unchanged bundled libraries and declaration responsibilities selected by D1-D6; no profile or hidden global/library substitution on this path.",
            "Compiler inputs and authored family requests are behavioral source comparisons, distinct from two complete original checker bodies and three restored mapper original semantic channels.",
            "Selected public semantic records retain complete diagnostic filenames, byte ranges, chains/related information, per-file/bulk/incremental/repeated publication and finite symbol/type/property/base/construct-signature queries; no private-state census.",
            "Property displayNameBase64 is actual public SymbolToString on the same ordered returned property, with its type association retained. It replaces incidental escaped allocation keys, whose prior mismatches remain archived; display names do not establish arbitrary property identity.",
            "External checked JS/JSX/decorator and other contract-external semantic consumers explicitly fail; source noCheck/@ts-nocheck/checkJs:false policy controls remain genuine Go comparisons. Unsupported failure is never source semantic success.",
            "The generator/yield mapper bulk/incremental semantic debt remains external; no fake global or modified original generator expectation.",
            "Retained current checking uses exactly DECLARATIONS_CURRENT_CHECKING=1. Reviewed authored hidden-library/profile controls are explicitly replaced; historical requests/expected/proofs and canonical interpretation/resolution observations remain unchanged. Two external JSON checking controls are authored boundaries, not Go parity.",
            "Retained xUnit aggregate counts each suite once; cheap affected managed checks intentionally run before AOT and are recorded separately from the full retained cascade.",
            "The authored historical globalThis-read rejection is explicitly retired because source initialization/identifier checking now supports it. Historical admission JSON/proofs are preserved; same source is checked in a genuine noLib ES2022 Program with complete missing-global and public semantic observations, alongside the reviewed named legacy adapter assertions.",
            "Existing read-side OS/scalar host and source-qualified typed JSON observation adaptations remain explicit; compiler/user diagnostic text is not normalized.",
        ],
    }
    destination = ROOT / "validation/results/declarations-verified.json"
    if destination.exists():
        raise RuntimeError("Declaration proof already exists; preserve it")
    destination.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"evidence": str(destination.relative_to(ROOT)), "xUnitCases": cases, "elapsedSeconds": result["elapsedSeconds"]}))


if __name__ == "__main__":
    main()
