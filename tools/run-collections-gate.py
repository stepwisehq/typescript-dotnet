#!/usr/bin/env python3
"""Run one frozen collection/regression gate without rewriting expected outputs."""
import argparse
import base64
import collections
import datetime
import gzip
import hashlib
import importlib.util
import json
import pathlib
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "artifacts/collections-validation"
BASELINE = "cfa6d2f"
MANIFEST = "validation/collections-corpus/manifest.json"
LIBRARIES = "src/TypeScript.Scanner/Checker/Libraries/manifest.json"
EXAMPLES = "examples/collections/manifest.json"
GO = ROOT / "tools/.cache/collections-oracle"
GO_UNINSTRUMENTED = ROOT / "tools/.cache/collections-oracle-uninstrumented"
NATIVE = ROOT / "artifacts/collections-validation/native/Collections.Validation"

spec = importlib.util.spec_from_file_location("retained_callable_gate", ROOT / "tools/run-callables-gate.py")
retained = importlib.util.module_from_spec(spec)
spec.loader.exec_module(retained)
NATIVE_PATHS = [*retained.NATIVE_PATHS, str(NATIVE.relative_to(ROOT))]
REQUIRED_CASES = {
    "G5-grouped-records", "G5-grouped-records-invalid",
    "G3-array-instance-apis", "G3-readonly-array-instance-apis",
    "G3-array-api-invalid-interactions",
}
DEMO_CASES = {
    "G5-grouped-records": "grouped-records.ts",
    "G5-grouped-records-invalid": "grouped-records-invalid.ts",
}


def read(path):
    return json.loads((ROOT / path).read_text())


def sha(path):
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def sources():
    return retained.sources()


def assets():
    # Prior asset scope plus authentic embedded declarations, which are not C#.
    library_files = (ROOT / LIBRARIES).parent.rglob("*")
    example_files = (ROOT / EXAMPLES).parent.rglob("*")
    return sorted(set(retained.assets()) | {p for p in library_files if p.is_file()} |
                  {p for p in example_files if p.is_file()})


def fingerprint(files):
    return retained.fingerprint(files)


def historical():
    names = subprocess.check_output(
        ["git", "ls-tree", "-r", "--name-only", BASELINE, "validation"],
        cwd=ROOT, text=True,
    ).splitlines()
    protected = {}
    for name in names:
        folder = pathlib.PurePosixPath(name).parts[1]
        if folder not in ("corpus", "results") and not folder.endswith("-corpus"):
            continue
        expected = subprocess.check_output(["git", "show", BASELINE + ":" + name], cwd=ROOT)
        if (ROOT / name).read_bytes() != expected:
            raise RuntimeError("Historical fixture/proof differs from baseline: " + name)
        protected[name] = hashlib.sha256(expected).hexdigest()
    return protected


def native_snapshot():
    return {name: {"sha256": sha(name), "sizeBytes": (ROOT / name).stat().st_size}
            for name in NATIVE_PATHS}


def oracle_snapshot():
    return {str(path.relative_to(ROOT)): {"sha256": sha(path), "sizeBytes": path.stat().st_size}
            for path in (GO_UNINSTRUMENTED, GO)}


def validate_libraries():
    manifest = read(LIBRARIES)
    if manifest["upstreamRevision"] != read("upstream.lock.json")["revision"]:
        raise RuntimeError("Fixed libraries have a different upstream revision")
    names = ["lib.es5.d.ts", "lib.decorators.d.ts", "lib.decorators.legacy.d.ts"]
    if [row["fileName"] for row in manifest["files"]] != ["/lib/" + name for name in names]:
        raise RuntimeError("Fixed library roots/order differ from the contract")
    for row, name in zip(manifest["files"], names):
        embedded = (ROOT / LIBRARIES).parent / name
        original = ROOT / "upstream/TypeScript/tsc/internal/bundled/libs" / name
        value = embedded.read_bytes()
        if value != original.read_bytes() or len(value) != row["bytes"] or hashlib.sha256(value).hexdigest() != row["sha256"]:
            raise RuntimeError("Fixed library bytes/digest differ: " + name)
    return manifest


def validate_examples():
    manifest = read(EXAMPLES)
    if manifest["upstreamRevision"] != read("upstream.lock.json")["revision"] or manifest["checkingOnly"] is not True:
        raise RuntimeError("Example revision/checking scope differs from contract")
    files = manifest["files"]
    if len(files) != len(DEMO_CASES) or {row["file"] for row in files} != set(DEMO_CASES.values()):
        raise RuntimeError("Missing/duplicate shipped collection demo inventory")
    values = {}
    for row in files:
        path = (ROOT / EXAMPLES).parent / row["file"]
        value = path.read_bytes()
        if hashlib.sha256(value).hexdigest() != row["sha256"]:
            raise RuntimeError("Collection demo digest mismatch: " + row["file"])
        values[row["file"]] = value
    return values


def validate_demo_diagnostics(name, expected):
    if name not in DEMO_CASES:
        return
    if expected["globalDiagnostics"] or expected["configurationDiagnostics"]:
        raise RuntimeError("Collection demo has global/configuration diagnostics: " + name)
    files = expected["files"]
    if len(files) != 4 or [row["index"] for row in files] != list(range(4)):
        raise RuntimeError("Collection demo lacks the actual four-file environment")
    for row in files:
        if row["parseDiagnostics"] or row["bindDiagnostics"]:
            raise RuntimeError("Collection demo must parse/bind without diagnostics: " + name)
        if row["index"] < 3 and row["programSemanticDiagnostics"]:
            raise RuntimeError("Collection demo has library Program diagnostics: " + name)
    diagnostics = files[3]["programSemanticDiagnostics"]
    if name == "G5-grouped-records" and diagnostics:
        raise RuntimeError("Valid collection demo has Program diagnostics")
    if name == "G5-grouped-records-invalid" and not diagnostics:
        raise RuntimeError("Invalid collection demo lacks user Program diagnostics")


def validate_support_provenance(manifest):
    support = manifest["supportProjections"]
    for key, digest_key in [("expected", "expectedSha256"), ("provenance", "provenanceSha256")]:
        path = pathlib.PurePosixPath(support[key])
        if path.is_absolute() or ".." in path.parts or path.parts[:2] != ("validation", "collections-corpus") or sha(support[key]) != support[digest_key]:
            raise RuntimeError("Invalid durable original support provenance asset")
    provenance = read(support["provenance"])
    if provenance["upstream"] != read("upstream.lock.json")["revision"] or provenance["sourceSha256"] != sha("tools/collections-oracle/support_projections.go") or provenance["outputSha256"] != support["expectedSha256"]:
        raise RuntimeError("Original support operation source/pin/output provenance differs")
    if type(support["groups"]) is not int or support["groups"] <= 0 or len(read(support["expected"])) != support["groups"] or type(support["sharedCases"]) is not int or support["sharedCases"] <= 0:
        raise RuntimeError("Incomplete original support projection inventory")
    if manifest["sharedChecks"]["xUnitClassCounts"].get("CollectionsSupportTests") != support["sharedCases"]:
        raise RuntimeError("Support projection/shared xUnit body counts differ")
    return support


def manifest_groups():
    manifest = read(MANIFEST)
    examples = validate_examples()
    groups = manifest["groups"]
    if not groups or len({g["group"] for g in groups}) != len(groups):
        raise RuntimeError("Missing/duplicate collection groups")
    seen = set()
    rich = 0
    total = 0
    for group in groups:
        name = group["group"]
        if not name or any(ch not in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_" for ch in name):
            raise RuntimeError("Unsafe collection group artifact name")
        if type(group["cases"]) is not int or group["cases"] <= 0 or type(group["storedObservations"]) is not bool:
            raise RuntimeError("Invalid collection group count/mode")
        for key, digest_key in [("requests", "requestsSha256"), ("expected", "expectedSha256")]:
            path = pathlib.PurePosixPath(group[key])
            if path.is_absolute() or ".." in path.parts or path.parts[:2] != ("validation", "collections-corpus"):
                raise RuntimeError("Collection assets must be durable corpus paths")
            if sha(group[key]) != group[digest_key]:
                raise RuntimeError("Collection manifest digest mismatch: " + group[key])
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            for row in rows:
                line = stream.readline()
                if not line.strip():
                    raise RuntimeError("Missing collection expected row: " + name)
                if row["name"] in DEMO_CASES:
                    validate_demo_diagnostics(row["name"], json.loads(line))
            if stream.readline():
                raise RuntimeError("Extra collection expected rows: " + name)
        if len(rows) != group["cases"]:
            raise RuntimeError("Collection manifest row count mismatch: " + name)
        for row in rows:
            # A source may deliberately appear in both behavior and stored groups.
            identity = (row["name"], group["storedObservations"])
            if identity in seen or row.get("profile") != "Collections" or row.get("storedObservations", False) != group["storedObservations"]:
                raise RuntimeError("Duplicate collection case or profile/stored-mode mismatch")
            if row["name"] in DEMO_CASES:
                raw = row.get("textBytesBase64")
                text = base64.b64decode(raw, validate=True) if raw is not None else row["text"].encode("utf-8")
                if text != examples[DEMO_CASES[row["name"]]]:
                    raise RuntimeError("Tested collection demo bytes differ from shipped example: " + row["name"])
            seen.add(identity)
        total += group["cases"]
        rich += group["cases"] if group["storedObservations"] else 0
    if total != manifest["cases"] or not rich or not REQUIRED_CASES <= {name for name, _ in seen}:
        raise RuntimeError("Incomplete collection total/rich/native demo/API inventory")
    checks = manifest["sharedChecks"]
    if type(checks["count"]) is not int or checks["count"] <= 0 or not checks["xUnitClassCounts"]:
        raise RuntimeError("Missing shared/xUnit collection assertions")
    corpus_classes = manifest["corpusXUnitClassCounts"]
    for class_counts, count in [(checks["xUnitClassCounts"], checks["count"]), (corpus_classes, total)]:
        if not class_counts or any(type(value) is not int or value <= 0 for value in class_counts.values()) or sum(class_counts.values()) != count:
            raise RuntimeError("Shared/corpus xUnit registries do not match their native counts")
    if checks["xUnitClassCounts"].keys() & corpus_classes.keys():
        raise RuntimeError("Corpus/shared xUnit classes overlap")
    order = manifest["orderControls"]
    parity = manifest["instrumentationParity"]
    for count in (order["cases"], order["sequencesPerRuntime"], order["executionsPerRuntime"], parity["cases"]):
        if type(count) is not int or count <= 0:
            raise RuntimeError("Missing original/order/provenance assertions")
    if order["sequencesPerRuntime"] < 2 or order["executionsPerRuntime"] < order["cases"] * order["sequencesPerRuntime"]:
        raise RuntimeError("Collection order controls must cover multiple complete sequences")
    for digest in (order["requestsSha256"], parity["requestsSha256"]):
        if not isinstance(digest, str) or len(digest) != 64 or any(ch not in "0123456789abcdef" for ch in digest):
            raise RuntimeError("Missing original/order/provenance request fingerprints")
    validate_support_provenance(manifest)
    return manifest


def compare_group(group, executable, output):
    subprocess.run([
        sys.executable, "tools/compare-collections.py", "--requests", group["requests"],
        "--expected", group["expected"], "--executable", str(executable), "--output", str(output),
    ], cwd=ROOT, check=True)


def reproduce_original():
    manifest = manifest_groups()
    (OUT / "oracle-before-collections.json").write_text(json.dumps(oracle_snapshot(), indent=2) + "\n")
    for group in manifest["groups"]:
        oracle = GO if group["storedObservations"] else GO_UNINSTRUMENTED
        compare_group(group, oracle, OUT / "original-go" / group["group"])
    subprocess.run([
        sys.executable, "tools/check-collections-provenance.py", "--manifest", MANIFEST,
        "--uninstrumented", str(GO_UNINSTRUMENTED), "--instrumented", str(GO),
        "--output", str(OUT / "original-go-provenance"),
    ], cwd=ROOT, check=True)
    support = manifest["supportProjections"]
    result = subprocess.run([str(GO_UNINSTRUMENTED), "--support-projections"], cwd=ROOT, text=True, capture_output=True)
    (OUT / "original-go-support.json").write_text(result.stdout)
    (OUT / "original-go-support.stderr").write_text(result.stderr)
    if result.returncode or canonical_json(json.loads(result.stdout)) != canonical_json(read(support["expected"])):
        raise RuntimeError("Original support operation reproduction differs")


def canonical_json(value):
    # Unlike Python ==, canonical JSON does not equate booleans with numbers.
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True)


def execute_native(executable):
    manifest = manifest_groups()
    for group in manifest["groups"]:
        compare_group(group, executable, OUT / "native" / group["group"])
    result = subprocess.run([str(executable), "--controls"], cwd=ROOT, text=True, capture_output=True)
    (OUT / "native-controls.txt").write_text(result.stdout)
    (OUT / "native-controls.stderr").write_text(result.stderr)
    if result.returncode or result.stdout.strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Failed/incomplete native collection controls")
    subprocess.run([
        sys.executable, "tools/check-collections-order.py", "--manifest", MANIFEST,
        "--go", str(GO), "--target", str(executable), "--output", str(OUT / "native-order"),
    ], cwd=ROOT, check=True)


def validate_comparisons(manifest, stage):
    reports = {}
    for group in manifest["groups"]:
        path = "artifacts/collections-validation/" + stage + "/" + group["group"] + "/report.json"
        report = read(path)
        if report["total"] != group["cases"] or report["exact"] != group["cases"] or report["failures"] or any(
            report[key] != group[key] for key in ("requestsSha256", "expectedSha256")
        ):
            raise RuntimeError("Failed/incomplete " + stage + " collection comparison: " + group["group"])
        reports[group["group"]] = {"report": path, "reportSha256": sha(path), **report}
    return reports


def xunit_results():
    namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    result = {}
    for name, path in [
        ("scanner", "artifacts/validation/scanner.trx"),
        ("parser", "artifacts/parser-validation/parser.trx"),
        ("binder", "artifacts/binder-validation/binder.trx"),
        ("checker", "artifacts/data-types-validation/checker.trx"),
    ]:
        tree = ET.parse(ROOT / path)
        counts = {key: int(value) for key, value in tree.find(".//" + namespace + "Counters").attrib.items()}
        if counts["failed"] or counts["passed"] != counts["total"]:
            raise RuntimeError("Failed/skipped xUnit: " + name)
        classes = collections.Counter()
        for row in tree.findall(".//" + namespace + "UnitTest"):
            method = row.find(namespace + "TestMethod")
            if method is not None:
                classes[method.attrib["className"].split(".")[-1]] += 1
        result[name] = {"counts": counts, "classCaseCounts": dict(classes), "trx": path, "trxSha256": sha(path)}
    return result


def check_execution():
    manifest = manifest_groups()
    validate_libraries()
    # Existing cascade assertions remain current executions, not rewritten proofs.
    retained.check_execution()
    native = validate_comparisons(manifest, "native")
    original = validate_comparisons(manifest, "original-go")
    support = validate_support_provenance(manifest)
    if canonical_json(read("artifacts/collections-validation/original-go-support.json")) != canonical_json(read(support["expected"])):
        raise RuntimeError("Original support reproduction is incomplete or differs")
    if (OUT / "native-controls.txt").read_text().strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Native shared registry differs from manifest")
    tests = xunit_results()
    classes = {**manifest["sharedChecks"]["xUnitClassCounts"], **manifest["corpusXUnitClassCounts"]}
    for name, count in classes.items():
        if type(count) is not int or count <= 0 or tests["checker"]["classCaseCounts"].get(name) != count:
            raise RuntimeError("Incomplete collection xUnit registry: " + name)
    order = read("artifacts/collections-validation/native-order/report.json")
    expected = manifest["orderControls"]
    if order["cases"] != expected["cases"] or order["sequencesPerRuntime"] != expected["sequencesPerRuntime"] or order["originalGoExact"] != expected["executionsPerRuntime"] or order["targetExact"] != expected["executionsPerRuntime"] or order["requestsSha256"] != expected["requestsSha256"] or order["failures"]:
        raise RuntimeError("Incomplete collection process/order controls")
    parity = read("artifacts/collections-validation/original-go-provenance/report.json")
    expected = manifest["instrumentationParity"]
    if parity["cases"] != expected["cases"] or parity["exact"] != expected["cases"] or parity["requestsSha256"] != expected["requestsSha256"] or parity["failures"]:
        raise RuntimeError("Incomplete original instrumentation semantic parity")
    snapshot = native_snapshot()
    if read("artifacts/collections-validation/native-before-collections.json") != snapshot:
        raise RuntimeError("Native binary changed during collection execution")
    oracle = oracle_snapshot()
    if read("artifacts/collections-validation/oracle-before-collections.json") != oracle:
        raise RuntimeError("Original oracle binary changed during collection verification")
    (OUT / "native-after-collections.json").write_text(json.dumps(snapshot, indent=2) + "\n")
    (OUT / "oracle-after-collections.json").write_text(json.dumps(oracle, indent=2) + "\n")
    return {"native": native, "originalGo": original, "xUnit": tests, "order": order,
            "instrumentationParity": parity, "originalOracleBinaries": oracle,
            "originalSupport": {**support, "current": "artifacts/collections-validation/original-go-support.json",
                                "currentSha256": sha("artifacts/collections-validation/original-go-support.json")}}


def run_gate():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_groups()
    validate_libraries()
    source = sources()
    files = assets()
    asset = fingerprint(files)
    protected = historical()
    command = ["bash", "tools/verify-collections.sh"]
    start = time.monotonic()
    utc = datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Final collections gate log: " + str(OUT / "final-gate.log"), flush=True)
    with (OUT / "final-gate.log").open("w") as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    current_files = assets()
    asset_drift = files != current_files or asset != fingerprint(current_files)
    historical_drift = any(not (ROOT / name).is_file() or sha(name) != digest for name, digest in protected.items())
    status = result.returncode or int(source_drift or asset_drift or historical_drift)
    report = {
        "command": command, "baseline": BASELINE, "startedAtUTC": utc,
        "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": status,
        "sourceSha256": source, "verificationAssetsSha256": asset,
        "sourceDrift": source_drift, "assetDrift": asset_drift, "historicalAssetDrift": historical_drift,
        "verificationAssetFiles": [p.relative_to(ROOT).as_posix() for p in files],
        "protectedHistoricalAssetFiles": protected,
        "log": "artifacts/collections-validation/final-gate.log",
    }
    (OUT / "final-gate.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key not in ("verificationAssetFiles", "protectedHistoricalAssetFiles")}), flush=True)
    raise SystemExit(status)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--preflight", action="store_true")
    modes.add_argument("--reproduce-original", action="store_true")
    modes.add_argument("--capture-native")
    modes.add_argument("--execute-native", action="store_true")
    modes.add_argument("--check-execution", action="store_true")
    parser.add_argument("--executable", type=pathlib.Path, default=NATIVE)
    args = parser.parse_args()
    if args.preflight:
        manifest_groups()
        validate_libraries()
        historical()
    elif args.reproduce_original:
        reproduce_original()
    elif args.capture_native:
        (ROOT / args.capture_native).write_text(json.dumps(native_snapshot(), indent=2) + "\n")
    elif args.execute_native:
        execute_native(args.executable)
    elif args.check_execution:
        check_execution()
    else:
        run_gate()


if __name__ == "__main__":
    main()
