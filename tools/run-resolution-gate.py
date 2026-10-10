#!/usr/bin/env python3
"""Run resolution evidence first, then one retained configuration regression cascade."""
import argparse
import base64
import collections
import datetime
import gzip
import importlib.util
import json
import os
import pathlib
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "artifacts/resolution-validation"
BASELINE = "37bd57d"
CURRENT_CHECKING = os.environ.get("DECLARATIONS_CURRENT_CHECKING")
if CURRENT_CHECKING not in (None, "1"):
    raise RuntimeError("DECLARATIONS_CURRENT_CHECKING must be the explicit current-control value 1")
HISTORICAL_MANIFEST = "validation/resolution-corpus/manifest.json"
MANIFEST = "validation/declarations-corpus/resolution-current-manifest.json" if CURRENT_CHECKING else HISTORICAL_MANIFEST
LANGUAGE_CURRENT = os.environ.get("CHECKER_LANGUAGE_CURRENT")
if LANGUAGE_CURRENT not in (None, "1") or LANGUAGE_CURRENT and not CURRENT_CHECKING:
    raise RuntimeError("CHECKER_LANGUAGE_CURRENT requires the explicit reviewed current checking context")
if LANGUAGE_CURRENT:
    MANIFEST = "validation/checker-noemit-corpus/resolution-current-manifest.json"
GO = ROOT / "tools/.cache/resolution-oracle"
GO_TOOL = ROOT / "tools/.cache/go/bin/go"
SEMANTIC_GO = ROOT / "tools/.cache/resolution-cli-oracle"
PRISTINE_GO = ROOT / "tools/.cache/resolution-pristine-oracle"
NATIVE = OUT / "native/Resolution.Validation"
CLI = OUT / "cli-native/tscheck"

spec = importlib.util.spec_from_file_location("retained_configuration_gate", ROOT / "tools/run-configuration-gate.py")
retained = importlib.util.module_from_spec(spec)
spec.loader.exec_module(retained)
read, sha, sources, fingerprint = retained.read, retained.sha, retained.sources, retained.fingerprint
canonical_json = retained.canonical_json
CURRENT_NATIVE_PATHS = [str(path.relative_to(ROOT)) for path in (NATIVE, CLI)]
NATIVE_PATHS = [*retained.NATIVE_PATHS, *CURRENT_NATIVE_PATHS]


def assets():
    files = set(retained.assets())
    for directory in ("validation/Resolution.Checks", "examples/resolution", "tools/resolution-oracle", "tools/resolution-cli-oracle", "licenses"):
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
    return {str(path.relative_to(ROOT)): {"sha256": sha(path), "sizeBytes": path.stat().st_size} for path in (GO, GO_TOOL, SEMANTIC_GO, PRISTINE_GO)}


def oracle_snapshot():
    return {**retained.oracle_snapshot(), **current_oracle_snapshot()}


def corpus_asset(name, digest):
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or path.parts[:2] != ("validation", "resolution-corpus") or sha(name) != digest:
        raise RuntimeError("Invalid resolution asset/digest: " + name)


def resolution_xunit():
    path = "artifacts/resolution-validation/resolution.trx"
    namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    tree = ET.parse(ROOT / path)
    counts = {key: int(value) for key, value in tree.find(".//" + namespace + "Counters").attrib.items()}
    if counts["failed"] or counts["passed"] != counts["total"] or counts["total"] <= 0:
        raise RuntimeError("Failed/skipped/empty resolution xUnit")
    classes = collections.Counter()
    for row in tree.findall(".//" + namespace + "UnitTest"):
        method = row.find(namespace + "TestMethod")
        if method is not None:
            classes[method.attrib["className"]] += 1
    return {"counts": counts, "classCaseCounts": dict(classes), "trx": path, "trxSha256": sha(path)}


def compare_group(group, executable, output):
    subprocess.run([sys.executable, "tools/compare-resolution.py", "--requests", group["requests"], "--expected", group["expected"],
                    "--executable", str(executable), "--output", str(output)], cwd=ROOT, check=True)



def obligations():
    inventory = read("docs/resolution-tests.json")
    rows = inventory["functions"]
    excluded = {"execution", "evidence", "managed", "native", "xUnit", "originalGo"}
    for row in rows:
        if sha(row["source"]) != row["sourceSha256"]:
            raise RuntimeError("Reviewed resolution assertion source changed")
    return {"resolution": [{k: v for k, v in row.items() if k not in excluded} for row in rows],
            "retainedConfiguration": retained.obligations()}


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
    rows = read("docs/resolution-tests.json")["functions"]
    selected = {("./internal/module", row["name"]): row["source"] for row in rows
                if row["disposition"] == "complete-original-assertion-body-required"}
    if len(selected) != 38 or len(rows) != 38:
        raise RuntimeError("Complete Module original registry must contain all 38 bodies")
    if manifest.get("dependencyOriginalTests"):
        raise RuntimeError("Unreviewed extra resolution original assertion registry")
    validate_registry(manifest["originalTests"], selected, "resolution")


def corpus_class_counts(manifest):
    name = manifest["corpusXUnitClass"]
    if name != "Resolution.Tests.ResolutionOracleTests":
        raise RuntimeError("Unexpected resolution corpus xUnit class")
    return {name: manifest["cases"]}


def checked_path(name, digest=None):
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or not path.parts or not (ROOT / path).is_file():
        raise RuntimeError("Invalid resolution evidence path: " + name)
    if digest is not None and sha(name) != digest:
        raise RuntimeError("Resolution producer/asset digest differs: " + name)
    return ROOT / path


def validate_cli(manifest):
    cli = manifest["cliControls"]
    if type(cli["cases"]) is not int or cli["cases"] <= 0 or any(type(cli[k]) is not int or cli[k] < 0 for k in ("originalCases", "authoredBoundaryCases")) or cli["originalCases"] + cli["authoredBoundaryCases"] != cli["cases"]:
        raise RuntimeError("Resolution CLI case registry differs")
    for name in ("runner", "semanticObserver"):
        checked_path(cli[name], cli[name + "Sha256"])
    if cli["runner"] != "tools/check-resolution-cli.py" or cli["semanticObserver"] != "tools/resolution-cli-oracle/main.go":
        raise RuntimeError("Unexpected resolution CLI producer")
    if not cli["sourceFiles"]:
        raise RuntimeError("No shipped CLI fixture inventory")
    for name, digest in cli["sourceFiles"].items():
        if not name.startswith(("examples/resolution/", "examples/configuration/")):
            raise RuntimeError("Invalid shipped resolution fixture")
        checked_path(name, digest)
    shipped = {path.relative_to(ROOT).as_posix(): sha(path) for path in (ROOT / "examples/resolution").rglob("*") if path.is_file()}
    shipped["examples/configuration/checking-base.json"] = sha("examples/configuration/checking-base.json")
    if cli["sourceFiles"] != shipped:
        raise RuntimeError("CLI fixture registry omits/changes shipped source/config files")
    if CURRENT_CHECKING:
        expected = ("validation/checker-noemit-corpus/resolution-current-boundaries.json" if LANGUAGE_CURRENT
                    else "validation/declarations-corpus/resolution-current-boundaries.json")
        if cli.get("currentCheckingBoundaries") != expected:
            raise RuntimeError("Current resolution checking boundary context missing")
        checked_path(expected, cli["currentCheckingBoundariesSha256"])


def validate_graph_oracle(manifest):
    graph = manifest["graphOracle"]
    for key in ("overlay", "provenance"):
        checked_path(graph[key], graph[key + "Sha256"])
    if graph["pristineOracle"] != str(PRISTINE_GO.relative_to(ROOT)) or not graph["sourceFiles"]:
        raise RuntimeError("Graph/pristine oracle registry differs")
    for path, digest in graph["sourceFiles"].items():
        checked_path(path, digest)
    overlay = read(graph["overlay"])["Replace"]
    compiler = ROOT / "upstream/TypeScript/tsc/internal/compiler"
    expected = {str(compiler / name) for name in ("fileloader.go", "filesparser.go", "oracle_resolution_graph.go")}
    if set(overlay) != expected:
        raise RuntimeError("Graph overlay modifies unreviewed original producer")
    provenance = read(graph["provenance"])
    if provenance["noProductionSourceMutation"] is not True or len(provenance["inputs"]) != 2:
        raise RuntimeError("Graph overlay provenance differs")
    for row in provenance["inputs"]:
        checked_path(row["source"], row["sourceSha256"])
        checked_path(str(pathlib.Path(overlay[str(ROOT / row["source"])]).relative_to(ROOT)), row["replacementSha256"])
    if sha(overlay[str(compiler / "oracle_resolution_graph.go")]) != sha("tools/resolution-oracle/graph_access.go.inc"):
        raise RuntimeError("Graph helper differs from reviewed source")
    pristine_proof = read("artifacts/resolution-validation/pristine-oracle-provenance.json")
    if pristine_proof["noProductionOverlay"] is not True or pristine_proof["observerOnlyGraphEntry"] != "explicit rejection; ordinary APIs unchanged":
        raise RuntimeError("Pristine observer boundary differs")
    pristine_dir = "upstream/TypeScript/tsc/cmd/resolution-pristine-oracle/"
    if set(pristine_proof["files"]) != {pristine_dir + name for name in ("main.go", "extensions.go", "graph_unavailable.go")}:
        raise RuntimeError("Unexpected pristine original observer files")
    for path, digest in pristine_proof["files"].items():
        checked_path(path, digest)
    for name in ("main.go", "extensions.go"):
        if sha(pristine_dir + name) != sha("tools/resolution-oracle/" + name):
            raise RuntimeError("Pristine and graph observers use different ordinary APIs")
    selected = graph["ordinaryGroups"]
    groups = {row["name"]: row for row in manifest["groups"]}
    if not selected or len(set(selected)) != len(selected) or any(name not in groups for name in selected):
        raise RuntimeError("Invalid ordinary/pristine parity groups")
    # Every non-graph input is covered; no selective parity subset.
    ordinary = []
    for name, group in groups.items():
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        if any(bool(row.get("Graph")) for row in rows):
            if not all(bool(row.get("Graph")) for row in rows):
                raise RuntimeError("Mixed graph/ordinary group cannot prove pristine parity")
        else:
            ordinary.append(name)
    if set(selected) != set(ordinary):
        raise RuntimeError("Pristine parity omits ordinary source API inputs")


def cli_command(output):
    command = [sys.executable, "tools/check-resolution-cli.py", "--executable", str(CLI),
               "--go", str(GO), "--semantic-go", str(SEMANTIC_GO), "--output", str(output)]
    if CURRENT_CHECKING:
        command += ["--current-checking-boundaries", manifest_groups()["cliControls"]["currentCheckingBoundaries"]]
    return command


def reproduce_pristine(manifest):
    selected = set(manifest["graphOracle"]["ordinaryGroups"])
    for group in manifest["groups"]:
        if group["name"] in selected:
            compare_group(group, PRISTINE_GO, OUT / "pristine-go" / group["name"])
    graph_request = next(json.loads(line) for group in manifest["groups"] for line in (ROOT / group["requests"]).read_text().splitlines() if json.loads(line).get("Graph"))
    raw = (json.dumps(graph_request, ensure_ascii=True) + "\n").encode()
    result = subprocess.run([str(PRISTINE_GO)], input=raw, capture_output=True, cwd=ROOT)
    (OUT / "pristine-graph-rejection.request.jsonl").write_bytes(raw)
    (OUT / "pristine-graph-rejection.stdout").write_bytes(result.stdout)
    (OUT / "pristine-graph-rejection.stderr").write_bytes(result.stderr)
    if not result.returncode or result.stdout or b"Graph observer unavailable in pristine companion" not in result.stderr:
        raise RuntimeError("Pristine companion did not reject its unavailable graph observer")
    (OUT / "pristine-graph-rejection.json").write_text(json.dumps({"command": [str(PRISTINE_GO)], "exitCode": result.returncode,
        "executableSha256": sha(PRISTINE_GO), "requestSha256": sha(OUT / "pristine-graph-rejection.request.jsonl"),
        "stdoutSha256": sha(OUT / "pristine-graph-rejection.stdout"), "stderrSha256": sha(OUT / "pristine-graph-rejection.stderr")}, indent=2) + "\n")


def check_pristine(manifest):
    rows = {}
    for group in manifest["groups"]:
        if group["name"] not in manifest["graphOracle"]["ordinaryGroups"]:
            continue
        stem = OUT / "pristine-go" / group["name"]
        value = read(stem / "report.json")
        if value["command"] != [str(PRISTINE_GO)] or value["executableSha256"] != sha(PRISTINE_GO) or value["executableDrift"] or value["exitCode"] or value["total"] != group["cases"] or value["exact"] != group["cases"] or value["failures"] or any(value[k] != group[k] for k in ("requestsSha256", "expectedSha256")):
            raise RuntimeError("Incomplete/changed pristine original parity evidence")
        normal = OUT / "original-go" / group["name"] / "stdout.jsonl"
        pristine = stem / "stdout.jsonl"
        if normal.read_bytes() != pristine.read_bytes():
            raise RuntimeError("Graph overlay changes ordinary original response bytes")
        rows[group["name"]] = {"report": str((stem / "report.json").relative_to(ROOT)), "reportSha256": sha(stem / "report.json"),
                               "ordinaryOutputSha256": sha(normal), "pristineOutputSha256": sha(pristine), **value}
    rejection = read("artifacts/resolution-validation/pristine-graph-rejection.json")
    if rejection["command"] != [str(PRISTINE_GO)] or not rejection["exitCode"] or rejection["executableSha256"] != sha(PRISTINE_GO):
        raise RuntimeError("Pristine graph rejection executable/command differs")
    for field, suffix in (("request", "request.jsonl"), ("stdout", "stdout"), ("stderr", "stderr")):
        if rejection[field + "Sha256"] != sha(OUT / ("pristine-graph-rejection." + suffix)):
            raise RuntimeError("Pristine graph rejection artifact changed")
    raw = (OUT / "pristine-graph-rejection.request.jsonl").read_bytes()
    requests = [line for group in manifest["groups"] for line in (ROOT / group["requests"]).read_text().splitlines()]
    if not json.loads(raw).get("Graph") or canonical_json(json.loads(raw)) not in {canonical_json(json.loads(line)) for line in requests} or (OUT / "pristine-graph-rejection.stdout").read_bytes() or b"Graph observer unavailable in pristine companion" not in (OUT / "pristine-graph-rejection.stderr").read_bytes():
        raise RuntimeError("Pristine unavailable-graph operation changed")
    return {"ordinaryGroups": rows, "graphObserverRejection": rejection, "pristineObserverProvenance": read("artifacts/resolution-validation/pristine-oracle-provenance.json")}


def manifest_groups():
    manifest = read(MANIFEST)
    if LANGUAGE_CURRENT:
        promotion = manifest.get("currentSemanticCapabilities", {})
        original = "validation/declarations-corpus/resolution-current-manifest.json"
        provenance = "validation/checker-noemit-corpus/authored-capability-migration.json"
        digest = "a66b9e07699c34ba705ecb4e7a64671c5f6f8dbb0f8c42c7e4515dd485e92bd2"
        if promotion != {"provenance": provenance, "provenanceSha256": digest,
                         "originalCurrentManifest": original, "originalCurrentManifestSha256": sha(original)}:
            raise RuntimeError("Unreviewed source semantic capability promotion")
        checked_path(provenance, digest)
        expected = read(original)
        expected["cliControls"].update(
            runnerSha256="19602d5f874809deb440c9195a6079ae42a3beda393540c6dd6f21171d2c3e47",
            originalCases=11, authoredBoundaryCases=0,
            currentCheckingBoundaries="validation/checker-noemit-corpus/resolution-current-boundaries.json",
            currentCheckingBoundariesSha256="98009f97ec8c4f91c5fbe78380fface915dbfa479c8fc78d1f6c8222409ec457")
        if {k: v for k, v in manifest.items() if k != "currentSemanticCapabilities"} != expected:
            raise RuntimeError("Capability promotion changed unrelated resolution obligations")
    if CURRENT_CHECKING:
        historical = read(HISTORICAL_MANIFEST)
        replacement = manifest.get("currentCheckingReplacement", {})
        unchanged = {key: value for key, value in manifest.items()
                     if key not in ("currentCheckingReplacement", "currentSemanticCapabilities")}
        unchanged["cliControls"] = historical["cliControls"]
        if unchanged != historical or replacement.get("historicalManifest") != HISTORICAL_MANIFEST or replacement.get("historicalManifestSha256") != sha(HISTORICAL_MANIFEST):
            raise RuntimeError("Current checking context altered historical resolution obligations/corpus")
        if replacement.get("baseline") != "e7b81a2":
            raise RuntimeError("Unexpected current checking replacement baseline")
    if manifest["upstream"] != read("upstream.lock.json")["revision"] or manifest["baseline"] != BASELINE:
        raise RuntimeError("Resolution upstream pin differs")
    groups = manifest["groups"]
    if not groups or len({g["name"] for g in groups}) != len(groups):
        raise RuntimeError("Empty/duplicate resolution groups")
    requests = {}
    for group in groups:
        if not re.fullmatch(r"[A-Za-z0-9_-]+", group["name"]) or type(group["cases"]) is not int or group["cases"] <= 0:
            raise RuntimeError("Invalid resolution group")
        for key in ("requests", "expected"):
            corpus_asset(group[key], group[key + "Sha256"])
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            expected = [json.loads(line) for line in stream]
        if len(rows) != group["cases"] or len(expected) != len(rows):
            raise RuntimeError("Resolution group count differs")
        for row, result in zip(rows, expected):
            if row["Name"] in requests or result["name"] != row["Name"]:
                raise RuntimeError("Duplicate/mismatched resolution case")
            requests[row["Name"]] = row
    if len(requests) != manifest["cases"]:
        raise RuntimeError("Resolution corpus inventory differs")
    shared = manifest["sharedChecks"]
    classes = corpus_class_counts(manifest)
    for counts, total in ((shared["xUnitClassCounts"], shared["count"]), (classes, manifest["cases"])):
        if type(total) is not int or total <= 0 or not counts or any(type(v) is not int or v <= 0 for v in counts.values()) or sum(counts.values()) != total:
            raise RuntimeError("Resolution native/xUnit inventory differs")
    if classes.keys() & shared["xUnitClassCounts"].keys():
        raise RuntimeError("Resolution xUnit class overlap")
    registry = shared["groups"]
    if not registry or len({g["flag"] for g in registry}) != len(registry) or any(not re.fullmatch(r"--[a-z-]+", g["flag"]) or type(g["count"]) is not int or g["count"] <= 0 for g in registry) or sum(g["count"] for g in registry) != shared["count"]:
        raise RuntimeError("Resolution shared dispatch inventory differs")
    originals = {row["name"] for row in read("docs/resolution-tests.json")["functions"]}
    registered = [name for group in registry for name in group["originalNames"]]
    authored = {"Core.Deduplicate.NilAliasAndMutation", "Graph.UnicodeFileNameBinderAndDiagnosticRoles"}
    if shared["count"] != 40 or len(registered) != 40 or len(set(registered)) != 40 or set(registered) != originals | authored:
        raise RuntimeError("Shared resolver registry omits original bodies or adds unreviewed controls")
    class_counts = {}
    for group in registry:
        if len(group["originalNames"]) != group["count"] or len(group["classNames"]) != 1 or group["classNames"][0] in class_counts:
            raise RuntimeError("Resolution shared group/class inventory differs")
        class_counts[group["classNames"][0]] = group["count"]
    if class_counts != shared["xUnitClassCounts"]:
        raise RuntimeError("Shared resolution class counts differ")
    order = manifest["orderControls"]
    corpus_asset(order["requests"], order["requestsSha256"])
    rows = [json.loads(line) for line in (ROOT / order["requests"]).read_text().splitlines()]
    if type(order["cases"]) is not int or order["cases"] <= 1 or len(rows) != order["cases"] or order["sequencesPerRuntime"] != 2 or order["executionsPerRuntime"] != 2 * (order["cases"] + 1):
        raise RuntimeError("Resolution order inventory differs")
    if len({r["Name"] for r in rows}) != len(rows) or any(canonical_json(r) != canonical_json(requests.get(r["Name"])) for r in rows):
        raise RuntimeError("Order requests differ from canonical inputs")
    validate_cli(manifest)
    validate_graph_oracle(manifest)
    names = set()
    for group in manifest["originalTests"]:
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


def reproduce_original():
    retained.retained.validate_upstream()
    manifest = manifest_groups()
    (OUT / "oracle-before-resolution.json").write_text(json.dumps(current_oracle_snapshot(), indent=2) + "\n")
    reproduce_pristine(manifest)
    for group in manifest["groups"]:
        compare_group(group, GO, OUT / "original-go" / group["name"])
    tests = []
    for index, group in enumerate(manifest["originalTests"]):
        command = [str(GO_TOOL), "test", "-json", group["package"], "-run", "^(" + "|".join(group["functions"]) + ")$", "-count=1"]
        start = time.monotonic()
        result = subprocess.run(command, cwd=ROOT / "upstream/TypeScript/tsc", text=True, capture_output=True)
        log = "artifacts/resolution-validation/original-tests-" + str(index) + ".jsonl"
        (ROOT / log).write_text(result.stdout)
        (ROOT / (log + ".stderr")).write_text(result.stderr)
        events = [json.loads(line) for line in result.stdout.splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if result.returncode or sorted(passed) != sorted(group["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original resolution assertions failed/skipped/incomplete")
        tests.append({"command": command, "functions": passed, "sourceFiles": group["sourceFiles"], "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": result.returncode, "log": log, "logSha256": sha(log)})
    (OUT / "original-tests.json").write_text(json.dumps(tests, indent=2) + "\n")
    retained.retained.validate_upstream()


def execute_native(executable):
    manifest = manifest_groups()
    for group in manifest["groups"]:
        compare_group(group, executable, OUT / "native-corpus" / group["name"])
    registry = subprocess.run([str(executable), "--registry"], cwd=ROOT, text=True, capture_output=True, check=True)
    (OUT / "native-registry.json").write_text(registry.stdout)
    if canonical_json(json.loads(registry.stdout)) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Native registry differs from manifest")
    result = subprocess.run([str(executable), "--controls"], cwd=ROOT, text=True, capture_output=True)
    (OUT / "native-controls.txt").write_text(result.stdout)
    (OUT / "native-controls.stderr").write_text(result.stderr)
    if result.returncode or result.stdout.strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Native resolution shared checks failed/incomplete")
    subprocess.run([sys.executable, "tools/check-resolution-order.py", "--manifest", MANIFEST, "--go", str(GO), "--target", str(executable), "--output", str(OUT / "native-order")], cwd=ROOT, check=True)
    subprocess.run(cli_command(OUT / "native-cli"), cwd=ROOT, check=True)


def comparison_reports(manifest, stage):
    reports = {}
    for group in manifest["groups"]:
        path = "artifacts/resolution-validation/" + stage + "/" + group["name"] + "/report.json"
        value = read(path)
        if value["total"] != group["cases"] or value["exact"] != group["cases"] or value["failures"] or any(value[k] != group[k] for k in ("requestsSha256", "expectedSha256")) or value["command"] != [str(NATIVE if stage == "native-corpus" else GO)]:
            raise RuntimeError("Failed/incomplete resolution comparison")
        executable = NATIVE if stage == "native-corpus" else GO
        if value["executableSha256"] != sha(executable) or value["executableDrift"] or value["exitCode"]:
            raise RuntimeError("Resolution comparison binary changed/failed")
        reports[group["name"]] = {"report": path, "reportSha256": sha(path), **value}
    return reports


def check_execution():
    retained.retained.validate_upstream()
    manifest = manifest_groups()
    old = retained.check_execution()
    native, original = comparison_reports(manifest, "native-corpus"), comparison_reports(manifest, "original-go")
    tests = resolution_xunit()
    for name, count in {**manifest["sharedChecks"]["xUnitClassCounts"], **corpus_class_counts(manifest)}.items():
        if tests["classCaseCounts"].get(name) != count:
            raise RuntimeError("Incomplete resolution xUnit class: " + name)
    if tests["counts"]["total"] != manifest["cases"] + manifest["sharedChecks"]["count"]:
        raise RuntimeError("Unexpected resolution xUnit inventory")
    if (OUT / "native-controls.txt").read_text().strip() != str(manifest["sharedChecks"]["count"]) or canonical_json(read("artifacts/resolution-validation/native-registry.json")) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Incomplete native resolution shared registry/checks")
    order = read("artifacts/resolution-validation/native-order/report.json")
    for key in ("cases", "sequencesPerRuntime", "requestsSha256"):
        if order[key] != manifest["orderControls"][key]:
            raise RuntimeError("Resolution order inventory differs")
    if order["failures"] or order["originalGoExact"] != manifest["orderControls"]["executionsPerRuntime"] or order["targetExact"] != manifest["orderControls"]["executionsPerRuntime"]:
        raise RuntimeError("Resolution order checks failed")
    cli = read("artifacts/resolution-validation/native-cli/report.json")
    if cli["cases"] != manifest["cliControls"]["cases"] or cli["exact"] != cli["cases"] or cli["failures"] or cli["managed"] is not False:
        raise RuntimeError("Resolution ordinary native CLI failed/incomplete")
    if cli["executableDrift"] or cli["executableHashes"] != {"cli": sha(CLI), "go": sha(GO), "semanticGo": sha(SEMANTIC_GO)}:
        raise RuntimeError("Resolution CLI executable/oracle changed")
    if cli["runnerSha256"] != manifest["cliControls"]["runnerSha256"] or cli["semanticObserverSha256"] != manifest["cliControls"]["semanticObserverSha256"]:
        raise RuntimeError("Resolution CLI producer changed")
    if cli["fixtureHashes"] != manifest["cliControls"]["sourceFiles"]:
        raise RuntimeError("Resolution ordinary CLI shipped fixture bytes differ")
    if CURRENT_CHECKING and cli.get("currentCheckingBoundariesSha256") != manifest["cliControls"]["currentCheckingBoundariesSha256"]:
        raise RuntimeError("Current authored boundary observations changed/incomplete")
    original_cli = sum(row["reference"] != "unsupported" for row in cli["records"])
    if len(cli["records"]) != cli["cases"] or original_cli != manifest["cliControls"]["originalCases"] or cli["cases"] - original_cli != manifest["cliControls"]["authoredBoundaryCases"] or any(not row["exact"] for row in cli["records"]):
        raise RuntimeError("Resolution CLI original/boundary executions incomplete")
    for label, executable in (("originalGo", GO), ("target", NATIVE)):
        if order["binarySha256Before"][label] != sha(executable) or order["binarySha256After"][label] != sha(executable):
            raise RuntimeError("Resolution order executable drift")
    if order["commands"] != {"originalGo": [str(GO)], "target": [str(NATIVE)]} or any(row["exitCode"] for row in order["records"]):
        raise RuntimeError("Resolution order command failed/changed")
    check_pristine(manifest)
    direct = read("artifacts/resolution-validation/original-tests.json")
    if len(direct) != len(manifest["originalTests"]):
        raise RuntimeError("Original test package inventory differs")
    for expected, actual in zip(manifest["originalTests"], direct):
        command = [str(GO_TOOL), "test", "-json", expected["package"], "-run", "^(" + "|".join(expected["functions"]) + ")$", "-count=1"]
        if actual["command"] != command or actual["exitCode"] or sorted(actual["functions"]) != sorted(expected["functions"]) or actual["sourceFiles"] != expected["sourceFiles"] or sha(actual["log"]) != actual["logSha256"]:
            raise RuntimeError("Original resolution test proof differs")
        events = [json.loads(line) for line in (ROOT / actual["log"]).read_text().splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if sorted(passed) != sorted(expected["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original resolution tests failed/skipped/incomplete")
    if read("artifacts/resolution-validation/native-before-resolution.json") != current_native_snapshot() or read("artifacts/resolution-validation/oracle-before-resolution.json") != current_oracle_snapshot():
        raise RuntimeError("Resolution native/oracle changed during execution")
    binaries, oracles = native_snapshot(), oracle_snapshot()
    (OUT / "native-after-resolution.json").write_text(json.dumps(binaries, indent=2) + "\n")
    (OUT / "oracle-after-resolution.json").write_text(json.dumps(oracles, indent=2) + "\n")
    return {"native": native, "originalGo": original, "resolutionXUnit": tests, "retainedExecution": old, "order": order, "cli": cli, "originalDirectTests": direct, "originalOracleBinaries": oracles, "currentCheckingReplacement": manifest.get("currentCheckingReplacement"), "currentCheckingEnvironment": {"DECLARATIONS_CURRENT_CHECKING": "1"} if CURRENT_CHECKING else None, "pristineParity": check_pristine(manifest), "graphOracle": manifest["graphOracle"]}


def run_gate():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_groups(); retained.retained.retained.validate_libraries(); retained.retained.validate_upstream()
    source, files, protected = sources(), assets(), historical()
    selected_obligations = obligations()
    asset = fingerprint(files)
    command = ["bash", "tools/verify-resolution.sh"]
    start, utc = time.monotonic(), datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Final resolution gate log: " + str(OUT / "final-gate.log"), flush=True)
    with (OUT / "final-gate.log").open("w") as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    asset_drift = files != assets() or asset != fingerprint(assets())
    historical_drift = any(not (ROOT / name).is_file() or sha(name) != digest for name, digest in protected.items())
    try:
        retained.retained.validate_upstream(); upstream_drift = False
    except RuntimeError:
        upstream_drift = True
    obligation_error = None
    try:
        obligation_drift = selected_obligations != obligations()
    except (OSError, RuntimeError, ValueError, KeyError) as error:
        obligation_drift = True
        obligation_error = str(error)
    status = result.returncode or int(source_drift or asset_drift or historical_drift or upstream_drift or obligation_drift)
    report = {"command": command, "baseline": BASELINE, "startedAtUTC": utc, "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": status, "sourceSha256": source, "verificationAssetsSha256": asset, "sourceDrift": source_drift, "assetDrift": asset_drift, "historicalAssetDrift": historical_drift, "upstreamDrift": upstream_drift, "obligationDrift": obligation_drift, "obligationError": obligation_error, "reviewedAssertionObligations": selected_obligations, "verificationAssetFiles": [p.relative_to(ROOT).as_posix() for p in files], "protectedHistoricalAssetFiles": protected, "log": "artifacts/resolution-validation/final-gate.log"}
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
        manifest_groups(); retained.retained.retained.validate_libraries(); historical(); retained.retained.validate_upstream()
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
