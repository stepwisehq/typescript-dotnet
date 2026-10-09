#!/usr/bin/env python3
"""Guard the frozen project gate; keep accepted collection/regression proofs immutable."""
import argparse
import datetime
import gzip
import importlib.util
import json
import pathlib
import re
import subprocess
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "artifacts/projects-validation"
BASELINE = "2598ceb"
MANIFEST = "validation/projects-corpus/manifest.json"
GO = ROOT / "tools/.cache/projects-oracle"
NATIVE = ROOT / "artifacts/projects-validation/native/Projects.Validation"
CLI = ROOT / "artifacts/projects-validation/cli-native/tscheck"
GO_TOOL = ROOT / "tools/.cache/go/bin/go"

spec = importlib.util.spec_from_file_location("retained_collections_gate", ROOT / "tools/run-collections-gate.py")
retained = importlib.util.module_from_spec(spec)
spec.loader.exec_module(retained)
NATIVE_PATHS = [*retained.NATIVE_PATHS, str(NATIVE.relative_to(ROOT)), str(CLI.relative_to(ROOT))]
read, sha, sources, fingerprint = retained.read, retained.sha, retained.sources, retained.fingerprint


def assets():
    return sorted(set(retained.assets()) | {p for p in (ROOT / "examples/projects").rglob("*") if p.is_file()})


def historical():
    names = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASELINE, "validation"], cwd=ROOT, text=True).splitlines()
    protected = {}
    for name in names:
        folder = pathlib.PurePosixPath(name).parts[1]
        if folder not in ("corpus", "results") and not folder.endswith("-corpus"):
            continue
        value = subprocess.check_output(["git", "show", BASELINE + ":" + name], cwd=ROOT)
        if (ROOT / name).read_bytes() != value:
            raise RuntimeError("Historical fixture/proof changed: " + name)
        protected[name] = sha(name)
    return protected


def native_snapshot():
    return {name: {"sha256": sha(name), "sizeBytes": (ROOT / name).stat().st_size} for name in NATIVE_PATHS}


def oracle_snapshot():
    return {**retained.oracle_snapshot(), **{str(path.relative_to(ROOT)): {"sha256": sha(path), "sizeBytes": path.stat().st_size} for path in (GO, GO_TOOL)}}


def validate_upstream():
    directory = ROOT / "upstream/TypeScript"
    revision = read("upstream.lock.json")["revision"]
    actual = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=directory, text=True).strip()
    if actual != revision:
        raise RuntimeError("Original project reference HEAD differs from locked pin")
    for command in (["git", "diff", "--quiet", revision, "--"], ["git", "diff", "--cached", "--quiet", revision, "--"]):
        if subprocess.run(command, cwd=directory).returncode:
            raise RuntimeError("Tracked original project algorithms/assertions differ from pin")
    return revision


def corpus_asset(path, digest):
    name = pathlib.PurePosixPath(path)
    if name.is_absolute() or ".." in name.parts or name.parts[:2] != ("validation", "projects-corpus") or sha(path) != digest:
        raise RuntimeError("Invalid project corpus asset/digest: " + path)


def validate_demo(request, expected, demo):
    folder = pathlib.PurePosixPath(demo["directory"])
    if folder.is_absolute() or ".." in folder.parts or folder.parts[:2] != ("examples", "projects"):
        raise RuntimeError("Invalid shipped project demonstration path")
    directory = ROOT / folder
    if request["config"].encode("utf-8") != (directory / "tscheck.json").read_bytes():
        raise RuntimeError("Tested configuration differs from shipped demonstration")
    files = {p.relative_to(directory).as_posix(): p.read_bytes() for p in directory.rglob("*.ts")}
    if {name: text.encode("utf-8") for name, text in request["files"].items()} != files:
        raise RuntimeError("Tested source files differ from shipped demonstration")
    status = demo["exitCode"]
    if type(status) is not int or status not in (0, 1) or expected["exitCode"] != status or expected["unsupportedFeature"] is not None:
        raise RuntimeError("Demonstration has wrong expected compiler status")
    if expected["globalDiagnostics"] or expected["configDiagnostics"]:
        raise RuntimeError("Demonstration has global/configuration errors")
    if any(file["parseDiagnostics"] or file["bindDiagnostics"] for file in expected["files"]):
        raise RuntimeError("Demonstration must parse/bind without errors")
    actual_diagnostics = expected["programDiagnostics"] or any(file["semanticDiagnostics"] for file in expected["files"])
    if bool(actual_diagnostics) != bool(status):
        raise RuntimeError("Demonstration Program diagnostic obligation failed")


def validate_helpers(manifest):
    for name in ("supportProjections", "specifierHelper"):
        helper = manifest[name]
        for field in ("expected", "provenance") + (("requests",) if name == "specifierHelper" else ()):
            corpus_asset(helper[field], helper[field + "Sha256"])
        provenance = read(helper["provenance"])
        if provenance["upstream"] != read("upstream.lock.json")["revision"]:
            raise RuntimeError("Project helper projection pin differs")
        for path, digest in provenance.get("sourceSha256", provenance.get("sha256", {})).items():
            parsed = pathlib.PurePosixPath(path)
            if parsed.is_absolute() or ".." in parsed.parts or sha(path) != digest:
                raise RuntimeError("Project helper projection source/output digest differs: " + path)
        count = helper["cases" if name == "specifierHelper" else "groups"]
        if type(count) is not int or count <= 0:
            raise RuntimeError("Project helper projection lacks assertion inventory")
        if name == "specifierHelper":
            rows = (ROOT / helper["requests"]).read_text().splitlines()
            values = (ROOT / helper["expected"]).read_text().splitlines()
            if len(rows) != count or len(values) != count:
                raise RuntimeError("Project specifier helper row inventory differs")
        elif len(read(helper["expected"])) != count:
            raise RuntimeError("Project support helper inventory differs")


def validate_original_tests(manifest):
    inventory = read("docs/projects-support-tests.json")
    selected = {}
    for file in inventory["files"]:
        for test in file["testFunctions"]:
            if test["disposition"] == "assertion-preserving-port" and test["executionScope"] == "project-shared":
                path = "upstream/TypeScript/" + file["path"]
                package = "./" + str(pathlib.PurePosixPath(file["path"]).parent.relative_to("tsc"))
                selected[(package, test["name"])] = path
    actual = {}
    if not selected:
        raise RuntimeError("Applicable original project tests are missing")
    for group in manifest["originalTests"]:
        if not group["functions"] or len(set(group["functions"])) != len(group["functions"]):
            raise RuntimeError("Invalid original test function registry")
        for name in group["functions"]:
            key = (group["package"], name)
            if key in actual or key not in selected or not re.fullmatch(r"Test[A-Za-z0-9_]+", name):
                raise RuntimeError("Unreviewed/duplicate original test")
            path = selected[key]
            if group["sourceFiles"].get(path) != sha(path):
                raise RuntimeError("Original assertion source digest differs")
            actual[key] = path
        if set(group["sourceFiles"]) != {actual[(group["package"], name)] for name in group["functions"]}:
            raise RuntimeError("Original test source inventory differs")
    if actual != selected:
        raise RuntimeError("Applicable original function registry is incomplete")


def manifest_groups():
    manifest = read(MANIFEST)
    if manifest["upstreamRevision"] != read("upstream.lock.json")["revision"]:
        raise RuntimeError("Project manifest upstream pin differs")
    groups = manifest["groups"]
    if not groups or len({g["group"] for g in groups}) != len(groups):
        raise RuntimeError("Missing/duplicate project groups")
    demonstrations = manifest["demonstrations"]
    if len(demonstrations) != 2 or {d["exitCode"] for d in demonstrations} != {0, 1}:
        raise RuntimeError("Missing valid/invalid shipped demonstrations")
    demos = {d["name"]: d for d in demonstrations}
    if len(demos) != 2:
        raise RuntimeError("Duplicate demonstration case")
    seen, requests = set(), {}
    for group in groups:
        if not group["group"] or any(c not in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_" for c in group["group"]):
            raise RuntimeError("Unsafe artifact group name")
        if type(group["cases"]) is not int or group["cases"] <= 0:
            raise RuntimeError("Invalid project group count")
        for path, digest in (("requests", "requestsSha256"), ("expected", "expectedSha256")):
            corpus_asset(group[path], group[digest])
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        if len(rows) != group["cases"]:
            raise RuntimeError("Project request count mismatch")
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            for row in rows:
                expected = json.loads(stream.readline())
                name = row["name"]
                if name in seen:
                    raise RuntimeError("Duplicate project case: " + name)
                seen.add(name)
                requests[name] = row
                if name in demos:
                    validate_demo(row, expected, demos[name])
            if stream.readline():
                raise RuntimeError("Extra project expected row")
    if len(seen) != manifest["cases"] or not demos.keys() <= seen:
        raise RuntimeError("Incomplete project/demo inventory")
    shared = manifest["sharedChecks"]
    classes = manifest["corpusXUnitClassCounts"]
    for counts, total in ((shared["xUnitClassCounts"], shared["count"]), (classes, manifest["cases"])):
        if type(total) is not int or total <= 0 or not counts or any(type(c) is not int or c <= 0 for c in counts.values()) or sum(counts.values()) != total:
            raise RuntimeError("Project native/xUnit class inventory differs")
    if classes.keys() & shared["xUnitClassCounts"].keys():
        raise RuntimeError("Shared/corpus xUnit classes overlap")
    order = manifest["orderControls"]
    corpus_asset(order["requests"], order["requestsSha256"])
    rows = [json.loads(line) for line in (ROOT / order["requests"]).read_text().splitlines()]
    if type(order["cases"]) is not int or order["cases"] <= 1 or len(rows) != order["cases"] or order["sequencesPerRuntime"] != 2 or order["executionsPerRuntime"] != 2 * (order["cases"] + 1):
        raise RuntimeError("Project process-order inventory differs")
    if len({r["name"] for r in rows}) != len(rows) or any(retained.canonical_json(r) != retained.canonical_json(requests.get(r["name"])) for r in rows):
        raise RuntimeError("Order requests differ from canonical cases")
    validate_helpers(manifest)
    validate_original_tests(manifest)
    return manifest


def compare_group(group, executable, output):
    subprocess.run([sys.executable, "tools/compare-projects.py", "--requests", group["requests"], "--expected", group["expected"],
                    "--executable", str(executable), "--output", str(output)], cwd=ROOT, check=True)


def reproduce_original():
    validate_upstream()
    manifest = manifest_groups()
    (OUT / "oracle-before-projects.json").write_text(json.dumps(oracle_snapshot(), indent=2) + "\n")
    for group in manifest["groups"]:
        compare_group(group, GO, OUT / "original-go" / group["group"])
    for name, flag in (("supportProjections", "--support-projections"), ("specifierHelper", "--specifier-projection")):
        helper = manifest[name]
        data = (ROOT / helper["requests"]).read_text() if name == "specifierHelper" else None
        result = subprocess.run([str(GO), flag], cwd=ROOT, input=data, text=True, capture_output=True)
        (OUT / ("original-go-" + name + ".json")).write_text(result.stdout)
        (OUT / ("original-go-" + name + ".stderr")).write_text(result.stderr)
        if result.returncode:
            raise RuntimeError("Original project helper reproduction failed")
        check_helper_output(name, helper, result.stdout)
    tests = []
    for index, group in enumerate(manifest["originalTests"]):
        command = [str(GO_TOOL), "test", "-json", group["package"], "-run", "^(" + "|".join(group["functions"]) + ")$", "-count=1"]
        stem = "original-tests-" + str(index)
        start = time.monotonic()
        result = subprocess.run(command, cwd=ROOT / "upstream/TypeScript/tsc", text=True, capture_output=True)
        (OUT / (stem + ".jsonl")).write_text(result.stdout)
        (OUT / (stem + ".stderr")).write_text(result.stderr)
        events = [json.loads(line) for line in result.stdout.splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if result.returncode or sorted(passed) != sorted(group["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original project direct assertions failed/skipped/incomplete")
        tests.append({"command": command, "functions": passed, "sourceFiles": group["sourceFiles"],
                      "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": result.returncode,
                      "log": "artifacts/projects-validation/" + stem + ".jsonl", "logSha256": sha(OUT / (stem + ".jsonl"))})
    (OUT / "original-tests.json").write_text(json.dumps(tests, indent=2) + "\n")
    validate_upstream()


def check_helper_output(name, helper, actual):
    expected = (ROOT / helper["expected"]).read_text()
    if name == "specifierHelper":
        want, got = [json.loads(line) for line in expected.splitlines()], [json.loads(line) for line in actual.splitlines()]
    else:
        want, got = json.loads(expected), json.loads(actual)
    if retained.canonical_json(want) != retained.canonical_json(got):
        raise RuntimeError("Original project helper reproduction differs: " + name)


def execute_native(executable):
    manifest = manifest_groups()
    for group in manifest["groups"]:
        compare_group(group, executable, OUT / "native" / group["group"])
    result = subprocess.run([str(executable), "--controls"], cwd=ROOT, text=True, capture_output=True)
    (OUT / "native-controls.txt").write_text(result.stdout)
    (OUT / "native-controls.stderr").write_text(result.stderr)
    if result.returncode or result.stdout.strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Native project shared checks failed/incomplete")
    subprocess.run([sys.executable, "tools/check-projects-order.py", "--manifest", MANIFEST, "--go", str(GO), "--target", str(executable), "--output", str(OUT / "native-order")], cwd=ROOT, check=True)
    subprocess.run([sys.executable, "tools/check-projects-cli.py", "--manifest", MANIFEST, "--executable", str(CLI), "--output", str(OUT / "native-cli")], cwd=ROOT, check=True)


def comparison_reports(manifest, stage):
    reports = {}
    for group in manifest["groups"]:
        path = "artifacts/projects-validation/" + stage + "/" + group["group"] + "/report.json"
        value = read(path)
        if value["total"] != group["cases"] or value["exact"] != group["cases"] or value["failures"] or any(value[k] != group[k] for k in ("requestsSha256", "expectedSha256")):
            raise RuntimeError("Failed/incomplete project comparison")
        if value["command"] != [str(NATIVE if stage == "native" else GO)]:
            raise RuntimeError("Project comparison executed a different binary")
        reports[group["group"]] = {"report": path, "reportSha256": sha(path), **value}
    return reports


def check_execution():
    validate_upstream()
    manifest = manifest_groups()
    retained.check_execution()
    native, original = comparison_reports(manifest, "native"), comparison_reports(manifest, "original-go")
    tests = retained.xunit_results()
    for name, count in {**manifest["sharedChecks"]["xUnitClassCounts"], **manifest["corpusXUnitClassCounts"]}.items():
        if tests["checker"]["classCaseCounts"].get(name) != count:
            raise RuntimeError("Incomplete project xUnit class: " + name)
    if (OUT / "native-controls.txt").read_text().strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Incomplete native project checks")
    order, expected = read("artifacts/projects-validation/native-order/report.json"), manifest["orderControls"]
    for key in ("cases", "sequencesPerRuntime", "requestsSha256"):
        if order[key] != expected[key]:
            raise RuntimeError("Project order report inventory differs")
    if order["failures"] or order["originalGoExact"] != expected["executionsPerRuntime"] or order["targetExact"] != expected["executionsPerRuntime"]:
        raise RuntimeError("Failed/incomplete project order checks")
    cli = read("artifacts/projects-validation/native-cli/report.json")
    if cli["cases"] != 10 or cli["exact"] != 10 or cli["failures"] or cli["lexicalProjectionControls"] != 14 or cli["managed"] is not False or cli["canonicalDemoDiagnostics"] is not True or cli["manifestSha256"] != sha(MANIFEST) or cli["executableSha256"] != sha(CLI):
        raise RuntimeError("Failed/incomplete ordinary CLI controls")
    helpers = {}
    for name in ("supportProjections", "specifierHelper"):
        path = "artifacts/projects-validation/original-go-" + name + ".json"
        check_helper_output(name, manifest[name], (ROOT / path).read_text())
        helpers[name] = {**manifest[name], "currentOutput": path, "currentOutputSha256": sha(path)}
    direct_tests = read("artifacts/projects-validation/original-tests.json")
    if len(direct_tests) != len(manifest["originalTests"]):
        raise RuntimeError("Original direct-test package inventory differs")
    for expected, actual in zip(manifest["originalTests"], direct_tests):
        command = [str(GO_TOOL), "test", "-json", expected["package"], "-run", "^(" + "|".join(expected["functions"]) + ")$", "-count=1"]
        if actual["command"] != command or actual["exitCode"] or sorted(actual["functions"]) != sorted(expected["functions"]) or actual["sourceFiles"] != expected["sourceFiles"] or sha(actual["log"]) != actual["logSha256"]:
            raise RuntimeError("Failed/incomplete original direct-test proof")
        events = [json.loads(line) for line in (ROOT / actual["log"]).read_text().splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if sorted(passed) != sorted(expected["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original direct-test events failed/skipped/incomplete")
    binaries, oracles = native_snapshot(), oracle_snapshot()
    if read("artifacts/projects-validation/native-before-projects.json") != binaries or read("artifacts/projects-validation/oracle-before-projects.json") != oracles:
        raise RuntimeError("Native/original executable changed during project execution")
    (OUT / "native-after-projects.json").write_text(json.dumps(binaries, indent=2) + "\n")
    (OUT / "oracle-after-projects.json").write_text(json.dumps(oracles, indent=2) + "\n")
    return {"native": native, "originalGo": original, "xUnit": tests, "order": order, "cli": cli, "originalHelpers": helpers, "originalDirectTests": direct_tests, "originalOracleBinaries": oracles}


def run_gate():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_groups()
    retained.validate_libraries()
    validate_upstream()
    source, files, protected = sources(), assets(), historical()
    asset = fingerprint(files)
    command = ["bash", "tools/verify-projects.sh"]
    start, utc = time.monotonic(), datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Final projects gate log: " + str(OUT / "final-gate.log"), flush=True)
    with (OUT / "final-gate.log").open("w") as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    asset_drift = files != assets() or asset != fingerprint(assets())
    historical_drift = any(not (ROOT / name).is_file() or sha(name) != digest for name, digest in protected.items())
    try:
        validate_upstream()
        upstream_drift = False
    except RuntimeError:
        upstream_drift = True
    status = result.returncode or int(source_drift or asset_drift or historical_drift or upstream_drift)
    report = {"command": command, "baseline": BASELINE, "startedAtUTC": utc, "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": status,
              "sourceSha256": source, "verificationAssetsSha256": asset, "sourceDrift": source_drift, "assetDrift": asset_drift, "historicalAssetDrift": historical_drift, "upstreamDrift": upstream_drift,
              "verificationAssetFiles": [p.relative_to(ROOT).as_posix() for p in files], "protectedHistoricalAssetFiles": protected, "log": "artifacts/projects-validation/final-gate.log"}
    (OUT / "final-gate.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: v for k, v in report.items() if k not in ("verificationAssetFiles", "protectedHistoricalAssetFiles")}), flush=True)
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
    OUT.mkdir(parents=True, exist_ok=True)
    if args.preflight:
        manifest_groups(); retained.validate_libraries(); historical(); validate_upstream()
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
