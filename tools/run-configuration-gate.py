#!/usr/bin/env python3
"""Run configuration evidence first, then one retained project regression cascade."""
import argparse
import base64
import collections
import datetime
import gzip
import importlib.util
import json
import pathlib
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "artifacts/configuration-validation"
BASELINE = "2c850db"
MANIFEST = "validation/configuration-corpus/manifest.json"
GO = ROOT / "tools/.cache/configuration-oracle"
GO_TOOL = ROOT / "tools/.cache/go/bin/go"
NATIVE = OUT / "native/Configuration.Validation"
CLI = OUT / "cli-native/tscheck"

spec = importlib.util.spec_from_file_location("retained_projects_gate", ROOT / "tools/run-projects-gate.py")
retained = importlib.util.module_from_spec(spec)
spec.loader.exec_module(retained)
read, sha, sources, fingerprint = retained.read, retained.sha, retained.sources, retained.fingerprint
canonical_json = retained.retained.canonical_json
CURRENT_NATIVE_PATHS = [str(path.relative_to(ROOT)) for path in (NATIVE, CLI)]
NATIVE_PATHS = [*retained.NATIVE_PATHS, *CURRENT_NATIVE_PATHS]


def assets():
    files = set(retained.assets())
    for directory in ("validation/Configuration.Checks", "examples/configuration", "licenses"):
        files.update(path for path in (ROOT / directory).rglob("*") if path.is_file()
                     and not {"bin", "obj", "__pycache__"}.intersection(path.relative_to(ROOT).parts))
    return sorted(files)


def historical():
    names = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASELINE, "validation"], cwd=ROOT, text=True).splitlines()
    protected = {}
    for name in names:
        folder = pathlib.PurePosixPath(name).parts[1]
        if folder not in ("corpus", "results") and not folder.endswith("-corpus"):
            continue
        original = subprocess.check_output(["git", "show", BASELINE + ":" + name], cwd=ROOT)
        if (ROOT / name).read_bytes() != original:
            raise RuntimeError("Historical fixture/proof changed: " + name)
        protected[name] = sha(name)
    return protected


def current_native_snapshot():
    return {name: {"sha256": sha(name), "sizeBytes": (ROOT / name).stat().st_size} for name in CURRENT_NATIVE_PATHS}


def native_snapshot():
    return {**retained.native_snapshot(), **current_native_snapshot()}


def current_oracle_snapshot():
    return {str(path.relative_to(ROOT)): {"sha256": sha(path), "sizeBytes": path.stat().st_size} for path in (GO, GO_TOOL)}


def oracle_snapshot():
    return {**retained.oracle_snapshot(), **current_oracle_snapshot()}


def corpus_asset(name, digest):
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or path.parts[:2] != ("validation", "configuration-corpus") or sha(name) != digest:
        raise RuntimeError("Invalid configuration asset/digest: " + name)


def configuration_xunit():
    path = "artifacts/configuration-validation/configuration.trx"
    namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    tree = ET.parse(ROOT / path)
    counts = {key: int(value) for key, value in tree.find(".//" + namespace + "Counters").attrib.items()}
    if counts["failed"] or counts["passed"] != counts["total"] or counts["total"] <= 0:
        raise RuntimeError("Failed/skipped/empty configuration xUnit")
    classes = collections.Counter()
    for row in tree.findall(".//" + namespace + "UnitTest"):
        method = row.find(namespace + "TestMethod")
        if method is not None:
            classes[method.attrib["className"]] += 1
    return {"counts": counts, "classCaseCounts": dict(classes), "trx": path, "trxSha256": sha(path)}


def compare_group(group, executable, output):
    subprocess.run([sys.executable, "tools/compare-configuration.py", "--requests", group["requests"], "--expected", group["expected"],
                    "--executable", str(executable), "--output", str(output)], cwd=ROOT, check=True)



def obligations():
    """Hash reviewed assertion selection, independently of mutable runtime prose."""
    result = {}
    for name in ("docs/configuration-tests.json", "docs/configuration-json-tests.json", "docs/progress/configuration-locale-tests.json", "docs/progress/configuration-path-tests.json", "docs/progress/configuration-module-tests.json"):
        if not (ROOT / name).is_file():
            raise RuntimeError("Missing reviewed test inventory: " + name)
        inventory = read(name)
        entries = inventory.get("entries", inventory.get("functions", inventory.get("newTypedAssertions", [])))
        for row in entries:
            source = row.get("source")
            digest = row.get("sourceSha256")
            if source and digest and (ROOT / source).is_file() and sha(source) != digest:
                raise RuntimeError("Reviewed assertion source changed: " + source)
        result[name] = [{key: value for key, value in row.items() if key not in ("managed", "native", "xUnit", "originalGo", "evidence", "managedEvidence", "nativeEvidence", "xUnitEvidence", "execution")} for row in entries]
    result["dependencyOriginalAssertions"] = [{"package": package, "name": name, "source": source, "sourceSha256": sha(source)} for (package, name), source in sorted(dependency_obligations().items())]
    return result


def dependency_obligations():
    selected = {(row["package"], row["name"]): row["source"] for row in read("docs/configuration-tests.json")["entries"] if row["disposition"] == "dependency-assertion-preserving-port"}
    for name in ("ordered-map-test-provenance.json", "dependency-metadata-test-provenance.json", "module-test-provenance.json"):
        provenance = read("validation/Configuration.Checks/" + name)
        functions = provenance.get("assertionPreservingFunctions", list(provenance.get("functions", {})))
        sources = {source: digest for source, digest in provenance["sources"].items() if source.startswith("upstream/TypeScript/tsc/") and source.endswith("_test.go")}
        for function in functions:
            matches = [source for source, digest in sources.items() if sha(source) == digest and re.search(r"^func " + re.escape(function) + r"\(", (ROOT / source).read_text(), re.M)]
            if len(matches) != 1:
                raise RuntimeError("Dependency original assertion source missing/ambiguous: " + function)
            source = matches[0]
            package = "./" + str(pathlib.Path(source).parent.relative_to("upstream/TypeScript/tsc"))
            key = (package, function)
            if key in selected:
                raise RuntimeError("Duplicate dependency original obligation: " + function)
            selected[key] = source
    return selected


def validate_registry(groups, selected, scope):
    actual = {}
    for group in groups:
        for name in group["functions"]:
            key = (group["package"], name)
            if key in actual or key not in selected or group["sourceFiles"].get(selected[key]) != sha(selected[key]):
                raise RuntimeError("Unreviewed/duplicate original " + scope + " assertion")
            actual[key] = selected[key]
        if set(group["sourceFiles"]) != {actual[(group["package"], name)] for name in group["functions"]}:
            raise RuntimeError("Original " + scope + " source inventory differs")
    if not selected or actual != selected:
        raise RuntimeError("Applicable original " + scope + " registry incomplete")


def validate_original_registry(manifest):
    selected = {(row["package"], row["name"]): row["source"] for row in read("docs/configuration-tests.json")["entries"] if row["disposition"] == "assertion-preserving-port"}
    validate_registry(manifest["originalTests"], selected, "configuration")
    validate_registry(manifest["dependencyOriginalTests"], dependency_obligations(), "dependency")

def validate_demonstrations(manifest, requests, cli_rows):
    demonstrations = manifest["demonstrations"]
    if {row["name"] for row in demonstrations} != {"checking-demo-valid", "checking-demo-invalid"} or len(demonstrations) != 2:
        raise RuntimeError("Configuration demonstration inventory differs")
    cli = {row["name"]: row for row in cli_rows}
    expected = {row["name"]: row["result"] for row in (json.loads(line) for line in (ROOT / manifest["cliControls"]["expected"]).read_text().splitlines())}
    def file_bytes(row):
        if "text" in row and "textBytesBase64" in row:
            raise RuntimeError("Ambiguous demonstration file bytes")
        return base64.b64decode(row["textBytesBase64"], validate=True) if "textBytesBase64" in row else row["text"].encode("utf-8")
    for demo in demonstrations:
        directory = pathlib.PurePosixPath(demo["directory"])
        if directory.parts[:2] != ("examples", "projects") or ".." in directory.parts or demo["configuration"] != "tsconfig.json":
            raise RuntimeError("Invalid shipped configuration demonstration")
        files = [*sorted((ROOT / directory).rglob("*.ts")), ROOT / directory / demo["configuration"], ROOT / "examples/configuration/checking-base.json"]
        shipped = {"/repo/" + path.relative_to(ROOT).as_posix(): path.read_bytes() for path in files}
        if len(files) != 7:
            raise RuntimeError("Shipped demonstration source/config inventory differs")
        config_path = "/repo/" + (directory / demo["configuration"]).as_posix()
        for name in (demo["name"], demo["name"] + "-inspect", demo["name"] + "-check"):
            row = requests.get(name) if name == demo["name"] else cli.get(name)
            if row is None or row["mode"] != "file" or row["configPath"] != config_path or len(row["files"]) != len(shipped) or {file["path"]: file_bytes(file) for file in row["files"]} != shipped:
                raise RuntimeError("Demonstration fixture bytes differ from shipped files: " + name)
        required_exit = 0 if demo["name"] == "checking-demo-valid" else 1
        if demo["exitCode"] != required_exit:
            raise RuntimeError("Demonstration status label differs")
        for action in ("inspect", "check"):
            name = demo["name"] + "-" + action
            code = required_exit if action == "check" else 0
            result = expected[name]
            if cli[name]["cliAction"] != action or cli[name]["exitCode"] != code or result["ExitCode"] != code or (bool(result["Diagnostics"]) != bool(code)):
                raise RuntimeError("Original demonstration diagnostic/status obligation differs")


def manifest_groups():
    manifest = read(MANIFEST)
    if manifest["upstreamRevision"] != read("upstream.lock.json")["revision"]:
        raise RuntimeError("Configuration upstream pin differs")
    groups = manifest["groups"]
    if not groups or len({g["group"] for g in groups}) != len(groups):
        raise RuntimeError("Empty/duplicate configuration groups")
    requests = {}
    for group in groups:
        if not re.fullmatch(r"[A-Za-z0-9_-]+", group["group"]) or type(group["cases"]) is not int or group["cases"] <= 0:
            raise RuntimeError("Invalid configuration group")
        for key in ("requests", "expected"):
            corpus_asset(group[key], group[key + "Sha256"])
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            expected = [json.loads(line) for line in stream]
        if len(rows) != group["cases"] or len(expected) != len(rows):
            raise RuntimeError("Configuration group count differs")
        for row, result in zip(rows, expected):
            if row["name"] in requests or result["name"] != row["name"]:
                raise RuntimeError("Duplicate/mismatched configuration case")
            requests[row["name"]] = row
    if len(requests) != manifest["cases"]:
        raise RuntimeError("Configuration corpus inventory differs")
    shared = manifest["sharedChecks"]
    classes = manifest["corpusXUnitClassCounts"]
    for counts, total in ((shared["xUnitClassCounts"], shared["count"]), (classes, manifest["cases"])):
        if type(total) is not int or total <= 0 or not counts or any(type(v) is not int or v <= 0 for v in counts.values()) or sum(counts.values()) != total:
            raise RuntimeError("Configuration native/xUnit inventory differs")
    if classes.keys() & shared["xUnitClassCounts"].keys():
        raise RuntimeError("Configuration xUnit class overlap")
    registry = shared["groups"]
    if not registry or len({g["flag"] for g in registry}) != len(registry) or any(not re.fullmatch(r"--[a-z-]+", g["flag"]) or type(g["count"]) is not int or g["count"] <= 0 for g in registry) or sum(g["count"] for g in registry) != shared["count"]:
        raise RuntimeError("Configuration shared dispatch inventory differs")
    order = manifest["orderControls"]
    corpus_asset(order["requests"], order["requestsSha256"])
    rows = [json.loads(line) for line in (ROOT / order["requests"]).read_text().splitlines()]
    if type(order["cases"]) is not int or order["cases"] <= 1 or len(rows) != order["cases"] or order["sequencesPerRuntime"] != 2 or order["executionsPerRuntime"] != 2 * (order["cases"] + 1):
        raise RuntimeError("Configuration order inventory differs")
    if len({r["name"] for r in rows}) != len(rows) or any(canonical_json(r) != canonical_json(requests.get(r["name"])) for r in rows):
        raise RuntimeError("Order requests differ from canonical inputs")
    for key in ("cliControls", "osProjections", "jsonErrorProjection"):
        helper = manifest[key]
        for field in (("requests", "expected", "provenance") if key == "cliControls" else ("expected", "provenance")):
            corpus_asset(helper[field], helper[field + "Sha256"])
        provenance = read(helper["provenance"])
        if provenance.get("upstreamRevision", manifest["upstreamRevision"]) != manifest["upstreamRevision"]:
            raise RuntimeError("Helper provenance pin differs")
        for name, digest in provenance.get("sourceFiles", provenance.get("sources", {})).items():
            if sha(name) != digest:
                raise RuntimeError("Helper source changed: " + name)
    cli = manifest["cliControls"]
    rows = [json.loads(line) for line in (ROOT / cli["requests"]).read_text().splitlines()]
    original_rows = [row for row in rows if "unsupportedError" not in row]
    if type(cli["originalCases"]) is not int or len(rows) != cli["cases"] or len({r["name"] for r in rows}) != len(rows) or len(original_rows) != cli["originalCases"] or len(rows) - len(original_rows) != cli["authoredBoundaryCases"]:
        raise RuntimeError("CLI/original case inventory differs")
    if read(cli["provenance"])["originalCases"] != cli["originalCases"]:
        raise RuntimeError("CLI provenance original count differs")
    validate_demonstrations(manifest, requests, rows)
    names = set()
    for group in manifest["originalTests"] + manifest["dependencyOriginalTests"]:
        if not re.fullmatch(r"\./internal/[a-zA-Z0-9_/]+", group["package"]) or not group["functions"] or not group["sourceFiles"]:
            raise RuntimeError("Invalid original test package")
        source = ""
        for name, digest in group["sourceFiles"].items():
            if not name.startswith("upstream/TypeScript/tsc/") or not name.endswith("_test.go") or sha(name) != digest:
                raise RuntimeError("Original assertion source differs")
            source += (ROOT / name).read_text()
        for function in group["functions"]:
            identity = (group["package"], function)
            if identity in names or not re.search(r"^func " + re.escape(function) + r"\(", source, re.M):
                raise RuntimeError("Duplicate/missing original assertion")
            names.add(identity)
    if not names:
        raise RuntimeError("No original assertion registry")
    validate_original_registry(manifest)
    obligations()
    return manifest


def check_helper_output(name, helper, actual):
    expected = (ROOT / helper["expected"]).read_text()
    want, got = ([json.loads(line) for line in expected.splitlines()], [json.loads(line) for line in actual.splitlines()]) if name == "cliControls" else (json.loads(expected), json.loads(actual))
    if name == "cliControls":
        if len(want) != helper["originalCases"] or len(got) != helper["originalCases"]:
            raise RuntimeError("Original CLI projection count differs")
    if name == "jsonErrorProjection" and (helper["controls"] != 6 or want != {"controls": 6, "passed": 6}):
        raise RuntimeError("JSON error adapter controls differ")
    if canonical_json(want) != canonical_json(got):
        raise RuntimeError("Original configuration helper differs: " + name)


def reproduce_original():
    retained.validate_upstream()
    manifest = manifest_groups()
    (OUT / "oracle-before-configuration.json").write_text(json.dumps(current_oracle_snapshot(), indent=2) + "\n")
    for name, flag in (("jsonErrorProjection", "--json-error-controls"), ("osProjections", "--os-projections"), ("cliControls", "--cli-projections")):
        helper = manifest[name]
        data = None
        if name == "cliControls":
            rows = [json.loads(line) for line in (ROOT / helper["requests"]).read_text().splitlines()]
            rows = [{key: value for key, value in row.items() if key in ("name", "mode", "configPath", "files", "cliAction")} for row in rows if "unsupportedError" not in row]
            data = "".join(json.dumps(row, ensure_ascii=True) + "\n" for row in rows)
        result = subprocess.run([str(GO), flag], cwd=ROOT, input=data, text=True, capture_output=True)
        (OUT / ("original-go-" + name + ".json")).write_text(result.stdout)
        (OUT / ("original-go-" + name + ".stderr")).write_text(result.stderr)
        if result.returncode:
            raise RuntimeError("Original configuration helper failed")
        check_helper_output(name, helper, result.stdout)
    for group in manifest["groups"]:
        compare_group(group, GO, OUT / "original-go" / group["group"])
    tests = []
    for index, group in enumerate(manifest["originalTests"] + manifest["dependencyOriginalTests"]):
        command = [str(GO_TOOL), "test", "-json", group["package"], "-run", "^(" + "|".join(group["functions"]) + ")$", "-count=1"]
        start = time.monotonic()
        result = subprocess.run(command, cwd=ROOT / "upstream/TypeScript/tsc", text=True, capture_output=True)
        log = "artifacts/configuration-validation/original-tests-" + str(index) + ".jsonl"
        (ROOT / log).write_text(result.stdout)
        (ROOT / (log + ".stderr")).write_text(result.stderr)
        events = [json.loads(line) for line in result.stdout.splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if result.returncode or sorted(passed) != sorted(group["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original configuration assertions failed/skipped/incomplete")
        tests.append({"command": command, "functions": passed, "sourceFiles": group["sourceFiles"], "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": result.returncode, "log": log, "logSha256": sha(log)})
    (OUT / "original-tests.json").write_text(json.dumps(tests, indent=2) + "\n")
    retained.validate_upstream()


def execute_native(executable):
    manifest = manifest_groups()
    for group in manifest["groups"]:
        compare_group(group, executable, OUT / "native-corpus" / group["group"])
    registry = subprocess.run([str(executable), "--registry"], cwd=ROOT, text=True, capture_output=True, check=True)
    (OUT / "native-registry.json").write_text(registry.stdout)
    if canonical_json(json.loads(registry.stdout)) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Native registry differs from manifest")
    result = subprocess.run([str(executable), "--controls"], cwd=ROOT, text=True, capture_output=True)
    (OUT / "native-controls.txt").write_text(result.stdout)
    (OUT / "native-controls.stderr").write_text(result.stderr)
    if result.returncode or result.stdout.strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Native configuration shared checks failed/incomplete")
    subprocess.run([sys.executable, "tools/check-configuration-order.py", "--manifest", MANIFEST, "--go", str(GO), "--target", str(executable), "--output", str(OUT / "native-order")], cwd=ROOT, check=True)
    subprocess.run([sys.executable, "tools/check-configuration-cli.py", "--manifest", MANIFEST, "--executable", str(CLI), "--output", str(OUT / "native-cli")], cwd=ROOT, check=True)


def comparison_reports(manifest, stage):
    reports = {}
    for group in manifest["groups"]:
        path = "artifacts/configuration-validation/" + stage + "/" + group["group"] + "/report.json"
        value = read(path)
        if value["total"] != group["cases"] or value["exact"] != group["cases"] or value["failures"] or any(value[k] != group[k] for k in ("requestsSha256", "expectedSha256")) or value["command"] != [str(NATIVE if stage == "native-corpus" else GO)]:
            raise RuntimeError("Failed/incomplete configuration comparison")
        executable = NATIVE if stage == "native-corpus" else GO
        if value["executableSha256"] != sha(executable) or value["executableDrift"] or value["exitCode"]:
            raise RuntimeError("Configuration comparison binary changed/failed")
        reports[group["group"]] = {"report": path, "reportSha256": sha(path), **value}
    return reports


def check_execution():
    retained.validate_upstream()
    manifest = manifest_groups()
    old = retained.check_execution()
    native, original = comparison_reports(manifest, "native-corpus"), comparison_reports(manifest, "original-go")
    tests = configuration_xunit()
    for name, count in {**manifest["sharedChecks"]["xUnitClassCounts"], **manifest["corpusXUnitClassCounts"]}.items():
        if tests["classCaseCounts"].get(name) != count:
            raise RuntimeError("Incomplete configuration xUnit class: " + name)
    if tests["counts"]["total"] != manifest["cases"] + manifest["sharedChecks"]["count"]:
        raise RuntimeError("Unexpected configuration xUnit inventory")
    if (OUT / "native-controls.txt").read_text().strip() != str(manifest["sharedChecks"]["count"]) or canonical_json(read("artifacts/configuration-validation/native-registry.json")) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Incomplete native configuration shared registry/checks")
    order = read("artifacts/configuration-validation/native-order/report.json")
    for key in ("cases", "sequencesPerRuntime", "requestsSha256"):
        if order[key] != manifest["orderControls"][key]:
            raise RuntimeError("Configuration order inventory differs")
    if order["failures"] or order["originalGoExact"] != manifest["orderControls"]["executionsPerRuntime"] or order["targetExact"] != manifest["orderControls"]["executionsPerRuntime"]:
        raise RuntimeError("Configuration order checks failed")
    cli = read("artifacts/configuration-validation/native-cli/report.json")
    if cli["cases"] != manifest["cliControls"]["cases"] or cli["exact"] != cli["cases"] or cli["failures"] or cli["managed"] is not False or cli["manifestSha256"] != sha(MANIFEST) or cli["executableSha256"] != sha(CLI):
        raise RuntimeError("Configuration ordinary native CLI failed/incomplete")
    for field in ("originalGoCases", "authoredBoundaryCases", "lexicalProjectionControls"):
        required = manifest["cliControls"]["originalCases" if field == "originalGoCases" else field]
        if cli[field] != required:
            raise RuntimeError("Configuration CLI assertion inventory differs: " + field)
    if cli["executableDrift"]:
        raise RuntimeError("Configuration CLI executable changed")
    helpers = {}
    for name in ("osProjections", "cliControls", "jsonErrorProjection"):
        path = "artifacts/configuration-validation/original-go-" + name + ".json"
        check_helper_output(name, manifest[name], (ROOT / path).read_text())
        helpers[name] = {**manifest[name], "currentOutput": path, "currentOutputSha256": sha(path), "rawStderr": path.removesuffix(".json") + ".stderr", "rawStderrSha256": sha(path.removesuffix(".json") + ".stderr")}
    direct = read("artifacts/configuration-validation/original-tests.json")
    if len(direct) != len(manifest["originalTests"] + manifest["dependencyOriginalTests"]):
        raise RuntimeError("Original test package inventory differs")
    for expected, actual in zip(manifest["originalTests"] + manifest["dependencyOriginalTests"], direct):
        command = [str(GO_TOOL), "test", "-json", expected["package"], "-run", "^(" + "|".join(expected["functions"]) + ")$", "-count=1"]
        if actual["command"] != command or actual["exitCode"] or sorted(actual["functions"]) != sorted(expected["functions"]) or actual["sourceFiles"] != expected["sourceFiles"] or sha(actual["log"]) != actual["logSha256"]:
            raise RuntimeError("Original configuration test proof differs")
        events = [json.loads(line) for line in (ROOT / actual["log"]).read_text().splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if sorted(passed) != sorted(expected["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original configuration tests failed/skipped/incomplete")
    if read("artifacts/configuration-validation/native-before-configuration.json") != current_native_snapshot() or read("artifacts/configuration-validation/oracle-before-configuration.json") != current_oracle_snapshot():
        raise RuntimeError("Configuration native/oracle changed during execution")
    binaries, oracles = native_snapshot(), oracle_snapshot()
    (OUT / "native-after-configuration.json").write_text(json.dumps(binaries, indent=2) + "\n")
    (OUT / "oracle-after-configuration.json").write_text(json.dumps(oracles, indent=2) + "\n")
    return {"native": native, "originalGo": original, "configurationXUnit": tests, "retainedExecution": old, "order": order, "cli": cli, "originalHelpers": helpers, "originalDirectTests": direct, "originalOracleBinaries": oracles}


def run_gate():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_groups(); retained.retained.validate_libraries(); retained.validate_upstream()
    source, files, protected = sources(), assets(), historical()
    selected_obligations = obligations()
    asset = fingerprint(files)
    command = ["bash", "tools/verify-configuration.sh"]
    start, utc = time.monotonic(), datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Final configuration gate log: " + str(OUT / "final-gate.log"), flush=True)
    with (OUT / "final-gate.log").open("w") as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    asset_drift = files != assets() or asset != fingerprint(assets())
    historical_drift = any(not (ROOT / name).is_file() or sha(name) != digest for name, digest in protected.items())
    try:
        retained.validate_upstream(); upstream_drift = False
    except RuntimeError:
        upstream_drift = True
    obligation_drift = selected_obligations != obligations()
    status = result.returncode or int(source_drift or asset_drift or historical_drift or upstream_drift or obligation_drift)
    report = {"command": command, "baseline": BASELINE, "startedAtUTC": utc, "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": status, "sourceSha256": source, "verificationAssetsSha256": asset, "sourceDrift": source_drift, "assetDrift": asset_drift, "historicalAssetDrift": historical_drift, "upstreamDrift": upstream_drift, "obligationDrift": obligation_drift, "reviewedAssertionObligations": selected_obligations, "verificationAssetFiles": [p.relative_to(ROOT).as_posix() for p in files], "protectedHistoricalAssetFiles": protected, "log": "artifacts/configuration-validation/final-gate.log"}
    (OUT / "final-gate.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: v for k, v in report.items() if k not in ("verificationAssetFiles", "protectedHistoricalAssetFiles", "reviewedAssertionObligations")}), flush=True)
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
        manifest_groups(); retained.retained.validate_libraries(); historical(); retained.validate_upstream()
    elif args.reproduce_original:
        reproduce_original()
    elif args.capture_native:
        (ROOT / args.capture_native).write_text(json.dumps(current_native_snapshot(), indent=2) + "\n")
    elif args.execute_native:
        execute_native(args.executable)
    elif args.check_execution:
        check_execution()
    else:
        run_gate()


if __name__ == "__main__":
    main()
