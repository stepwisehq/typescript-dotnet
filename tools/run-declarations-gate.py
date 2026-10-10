#!/usr/bin/env python3
"""Run Program managed/original evidence, native checks and one retained resolution cascade."""
import argparse
import base64
import collections
import datetime
import gzip
import hashlib
import os
import importlib.util
import json
import pathlib
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "artifacts/declarations-validation"
BASELINE = "e7b81a2"
MANIFEST = "validation/declarations-corpus/manifest.json"
GO = ROOT / "tools/.cache/program-oracle"
SEMANTIC_GO = ROOT / "tools/.cache/resolution-cli-oracle"
GO_TOOL = ROOT / "tools/.cache/go/bin/go"
NATIVE = OUT / "native/Declarations.Validation"
CLI = OUT / "cli-native/tscheck"
MANAGED = OUT / "managed/Declarations.Validation.dll"
MANAGED_CLI = OUT / "cli-managed/tscheck.dll"

spec = importlib.util.spec_from_file_location("retained_program_gate", ROOT / "tools/run-program-gate.py")
retained = importlib.util.module_from_spec(spec)
spec.loader.exec_module(retained)
read, sha, sources, fingerprint = retained.read, retained.sha, retained.sources, retained.fingerprint
canonical_json = retained.canonical_json
UPSTREAM_GUARD = retained.retained.retained.retained
LIBRARY_GUARD = UPSTREAM_GUARD.retained
if os.environ.get("DECLARATIONS_CURRENT_CHECKING") != "1":
    raise RuntimeError("Declaration gate requires the reviewed DECLARATIONS_CURRENT_CHECKING=1 context")
CURRENT_NATIVE_PATHS = [str(path.relative_to(ROOT)) for path in (NATIVE, CLI)]
NATIVE_PATHS = [*retained.NATIVE_PATHS, *CURRENT_NATIVE_PATHS]


def assets():
    files = set(retained.assets())
    if (ROOT / "package.json").is_file():
        files.add(ROOT / "package.json")
    for directory in ("validation/Declarations.Checks", "examples/declarations", "tools/program-oracle", "src/TypeScript.Scanner/Bundled", "licenses"):
        files.update(path for path in (ROOT / directory).rglob("*") if path.is_file()
                     and not {"bin", "obj", "__pycache__"}.intersection(path.relative_to(ROOT).parts))
    files.update(ROOT / name for name in ("docs/DECLARATIONS-CONTRACT.md", "docs/DECLARATIONS-VERIFY.md"))
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
    return {str(path.relative_to(ROOT)): {"sha256": sha(path), "sizeBytes": path.stat().st_size} for path in (GO, SEMANTIC_GO, GO_TOOL)}


def oracle_snapshot():
    return {**retained.oracle_snapshot(), **current_oracle_snapshot()}


def corpus_asset(name, digest):
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or path.parts[:2] != ("validation", "declarations-corpus") or sha(name) != digest:
        raise RuntimeError("Invalid program asset/digest: " + name)


def declarations_xunit():
    path = "artifacts/declarations-validation/declarations.trx"
    namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    tree = ET.parse(ROOT / path)
    counts = {key: int(value) for key, value in tree.find(".//" + namespace + "Counters").attrib.items()}
    if counts["failed"] or counts["passed"] != counts["total"] or counts["total"] <= 0:
        raise RuntimeError("Failed/skipped/empty program xUnit")
    classes = collections.Counter()
    for row in tree.findall(".//" + namespace + "UnitTest"):
        method = row.find(namespace + "TestMethod")
        if method is not None:
            classes[method.attrib["className"]] += 1
    return {"counts": counts, "classCaseCounts": dict(classes), "trx": path, "trxSha256": sha(path)}


def compare_group(group, executable, output, managed=False):
    subprocess.run([sys.executable, "tools/compare-programs.py", "--requests", group["requests"], "--expected", group["expected"],
                    "--executable", str(executable), "--output", str(output)] + (["--managed"] if managed else []), cwd=ROOT, check=True)



def obligations():
    inventory = read("docs/declarations-tests.json")
    full = inventory["directOriginalFunctions"]
    for row in full:
        if sha(row["source"]) != row["sourceSha256"]:
            raise RuntimeError("Original checker assertion source changed")
    mapper = read("validation/Declarations.Checks/mapper-semantic-restoration-provenance.json")
    for name, digest in mapper["sourceFiles"].items():
        if sha(name) != digest:
            raise RuntimeError("Restored mapper consumer/assertion source changed")
    def stable(value):
        omitted = {"execution", "status", "managed", "native", "xUnit", "proof", "managedReport", "originalReport", "representativeReport", "report"}
        if isinstance(value, dict):
            return {key: stable(item) for key, item in value.items() if key not in omitted}
        if isinstance(value, list):
            return [stable(item) for item in value]
        return value
    return {"contractSha256": sha("docs/DECLARATIONS-CONTRACT.md"), "checker": stable(full),
            "mapper": stable(mapper), "compilerInputSelection": stable(inventory["compilerInputs"]),
            "sourceFamilySelection": stable(inventory["authoredSourceFamilies"]),
            "externalConsumers": stable(inventory["authoredExternalSemanticControl"]),
            "retainedProgram": retained.obligations()}


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
    direct = read("docs/declarations-tests.json")["directOriginalFunctions"]
    mapper = read("validation/Declarations.Checks/mapper-semantic-restoration-provenance.json")
    selected = {(row["package"], row["name"]): row["source"] for row in direct}
    selected.update({("./internal/compiler", row["name"]): "upstream/TypeScript/tsc/internal/compiler/contentmapper_test.go" for row in mapper["functions"]})
    validate_registry(manifest["originalTests"], selected, "declaration and restored mapper")
    if manifest.get("adaptedOriginalTests") or manifest.get("dependencyOriginalTests"):
        raise RuntimeError("Unreviewed additional declaration original tests")


def corpus_class_counts(manifest):
    name = manifest["corpusXUnitClass"]
    if name != "Declarations.Tests.DeclarationsOracleTests":
        raise RuntimeError("Unexpected program corpus xUnit class")
    return {name: manifest["cases"]}


def checked_path(name, digest=None):
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or not path.parts or not (ROOT / path).is_file():
        raise RuntimeError("Invalid program evidence path: " + name)
    if digest is not None and sha(name) != digest:
        raise RuntimeError("Program producer/asset digest differs: " + name)
    return ROOT / path


def validate_cli(manifest):
    cli = manifest["cliControls"]
    if cli["cases"] != 2 or cli["runner"] != "tools/check-declarations-cli.py" or cli["expectedExits"] != {"warehouse-valid": 0, "warehouse-invalid": 1}:
        raise RuntimeError("Unreviewed warehouse checking registry")
    for key in ("requests", "runner", "provenance", "semanticObserver"):
        checked_path(cli[key], cli[key + "Sha256"])
    corpus_asset(cli["requests"], cli["requestsSha256"])
    shipped = {p.relative_to(ROOT).as_posix(): sha(p) for p in (ROOT / "examples/declarations").rglob("*") if p.is_file() and p.suffix in (".ts", ".json")}
    if cli["sourceFiles"] != shipped:
        raise RuntimeError("Warehouse checking bytes differ")
    rows = [json.loads(line) for line in (ROOT / cli["requests"]).read_text().splitlines()]
    if len(rows) != 2 or {r["Name"] for r in rows} != set(cli["expectedExits"]):
        raise RuntimeError("Warehouse CLI request registry differs")
    for row in rows:
        folder = "warehouse" if row["Name"] == "warehouse-valid" else "warehouse-invalid"
        if row["ConfigPath"] != str(ROOT / "examples/declarations" / folder / "tsconfig.json") or row["Files"] != {str(ROOT / name): (ROOT / name).read_text() for name in shipped}:
            raise RuntimeError("Warehouse CLI input differs from actual disk fixture")


def cli_command(output, managed=False):
    cli = manifest_groups()["cliControls"]
    command = [sys.executable, cli["runner"], "--manifest", MANIFEST, "--original", str(SEMANTIC_GO),
               "--executable", str(MANAGED_CLI if managed else CLI), "--output", str(output)]
    return command + (["--managed"] if managed else [])


def validate_modal_projection(manifest):
    projection = manifest["jsonErrorProjection"]
    if projection["controls"] != 6:
        raise RuntimeError("Typed SemanticError observation control inventory differs")
    for key in ("expected", "provenance"):
        checked_path(projection[key], projection[key + "Sha256"])
    if read(projection["expected"]) != {"controls": 6, "passed": 6}:
        raise RuntimeError("Typed SemanticError controls must prove all six passed")


def replay_modal_projection(manifest):
    projection = manifest["jsonErrorProjection"]
    result = subprocess.run([str(GO), "--json-error-controls"], cwd=ROOT, capture_output=True)
    stem = OUT / "json-error-controls"
    stem.with_suffix(".stdout").write_bytes(result.stdout)
    stem.with_suffix(".stderr").write_bytes(result.stderr)
    if result.returncode or json.loads(result.stdout) != read(projection["expected"]):
        raise RuntimeError("Typed SemanticError observation controls failed")
    report = {"command": [str(GO), "--json-error-controls"], "exitCode": result.returncode,
              "executableSha256": sha(GO), "expectedSha256": projection["expectedSha256"],
              "stdoutSha256": sha(stem.with_suffix(".stdout")), "stderrSha256": sha(stem.with_suffix(".stderr"))}
    (OUT / "json-error-controls-report.json").write_text(json.dumps(report, indent=2) + "\n")


def validate_project_paths():
    # Fixed projects actually invoked by this gate and its retained cascade.
    # Project tests live in Checker.Tests; there is no Projects.Tests project.
    for name in ("Declarations", "Program", "Resolution", "Configuration", "Parser", "Scanner", "Binder", "Checker"):
        checked_path(f"tests/{name}.Tests/{name}.Tests.csproj")
    for name in ("Declarations", "Program", "Resolution", "Configuration", "Projects", "Collections",
                 "BindingCompletion", "Foundation", "Parser", "Scanner", "Binder", "Checker"):
        checked_path(f"validation/{name}.Validation/{name}.Validation.csproj")
    checked_path("src/TypeScript.Check/TypeScript.Check.csproj")


def manifest_groups():
    validate_project_paths()
    manifest = read(MANIFEST)
    if manifest["upstream"] != read("upstream.lock.json")["revision"] or manifest["baseline"] != BASELINE:
        raise RuntimeError("Program upstream pin differs")
    observation = manifest["semanticObservation"]
    corpus_asset(observation["provenance"], observation["provenanceSha256"])
    display = read(observation["provenance"])
    if display["classification"] != "authored-public-property-display-observation-correction":
        raise RuntimeError("Unexpected semantic property observation")
    for name, expected in display["sourceFiles"].items():
        if sha(name) != expected:
            raise RuntimeError("Public property display observer changed: " + name)
    retirement = manifest["authoredControlRetirement"]
    corpus_asset(retirement["provenance"], retirement["provenanceSha256"])
    controls = read(retirement["provenance"])
    if controls["classification"] != "authored-capability-restriction-retirement-with-genuine-source-positive-replacement" or controls["replacement"]["name"] != "global-this-read-current-capability":
        raise RuntimeError("Unreviewed authored control retirement")
    if sha(controls["historicalAsset"]) != controls["historicalSha256"]:
        raise RuntimeError("Historical admission evidence changed")
    for name, expected in controls["sourceFiles"].items():
        if sha(name) != expected:
            raise RuntimeError("Reviewed admission adapter changed: " + name)
    groups = manifest["groups"]
    if not groups or len({g["name"] for g in groups}) != len(groups):
        raise RuntimeError("Empty/duplicate program groups")
    requests = {}
    for group in groups:
        if not re.fullmatch(r"[A-Za-z0-9_-]+", group["name"]) or type(group["cases"]) is not int or group["cases"] <= 0:
            raise RuntimeError("Invalid program group")
        for key in ("requests", "expected"):
            corpus_asset(group[key], group[key + "Sha256"])
        rows = [json.loads(line) for line in (ROOT / group["requests"]).read_text().splitlines()]
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            expected = [json.loads(line) for line in stream]
        if len(rows) != group["cases"] or len(expected) != len(rows):
            raise RuntimeError("Program group count differs")
        for row, result in zip(rows, expected):
            if row["Name"] in requests or result["name"] != row["Name"]:
                raise RuntimeError("Duplicate/mismatched program case")
            requests[row["Name"]] = row
    if len(requests) != manifest["cases"]:
        raise RuntimeError("Program corpus inventory differs")
    if controls["replacement"]["name"] not in requests:
        raise RuntimeError("Retired authored negative lacks its genuine positive replacement")
    shared = manifest["sharedChecks"]
    classes = corpus_class_counts(manifest)
    for counts, total in ((shared["xUnitClassCounts"], shared["count"]), (classes, manifest["cases"])):
        if type(total) is not int or total <= 0 or not counts or any(type(v) is not int or v <= 0 for v in counts.values()) or sum(counts.values()) != total:
            raise RuntimeError("Program native/xUnit inventory differs")
    if classes.keys() & shared["xUnitClassCounts"].keys():
        raise RuntimeError("Program xUnit class overlap")
    registry = shared["groups"]
    if not registry or len({g["flag"] for g in registry}) != len(registry) or any(not re.fullmatch(r"--[a-z-]+", g["flag"]) or type(g["count"]) is not int or g["count"] <= 0 for g in registry) or sum(g["count"] for g in registry) != shared["count"]:
        raise RuntimeError("Program shared dispatch inventory differs")
    originals = {row["name"] for row in read("docs/declarations-tests.json")["directOriginalFunctions"]}
    originals.update(row["name"] for row in read("validation/Declarations.Checks/mapper-semantic-restoration-provenance.json")["functions"])
    registered = [name for group in registry for name in group["names"]]
    if shared["count"] != 6 or len(registered) != 6 or len(set(registered)) != 6 or set(registered) != originals | {"Authored.ExternalSemanticConsumers"}:
        raise RuntimeError("Declaration shared groups omit original assertions/consumer control")
    class_counts = {}
    for group in registry:
        if len(group["names"]) != group["count"] or len(group["classNames"]) != 1 or group["classNames"][0] in class_counts:
            raise RuntimeError("Program shared group/class inventory differs")
        class_counts[group["classNames"][0]] = group["count"]
    if class_counts != shared["xUnitClassCounts"]:
        raise RuntimeError("Shared program class counts differ")
    order = manifest["orderControls"]
    corpus_asset(order["requests"], order["requestsSha256"])
    rows = [json.loads(line) for line in (ROOT / order["requests"]).read_text().splitlines()]
    if type(order["cases"]) is not int or order["cases"] <= 1 or len(rows) != order["cases"] or order["sequencesPerRuntime"] != 2 or order["executionsPerRuntime"] != 2 * (order["cases"] + 1):
        raise RuntimeError("Program order inventory differs")
    if len({r["Name"] for r in rows}) != len(rows) or any(canonical_json(r) != canonical_json(requests.get(r["Name"])) for r in rows):
        raise RuntimeError("Order requests differ from canonical inputs")
    if manifest["currentCheckingEnvironment"] != {"DECLARATIONS_CURRENT_CHECKING": "1"}:
        raise RuntimeError("Declaration checking context differs")
    migration = manifest["currentCheckingReplacement"]
    for key in ("configurationManifest", "resolutionManifest", "migration"):
        checked_path(migration[key], migration[key + "Sha256"])
    if migration["configurationManifest"] != "validation/declarations-corpus/configuration-current-manifest.json" or migration["resolutionManifest"] != "validation/declarations-corpus/resolution-current-manifest.json":
        raise RuntimeError("Unreviewed retained checking replacements")
    retained.retained.manifest_groups()
    retained.retained.retained.manifest_groups()
    validate_cli(manifest)
    validate_modal_projection(manifest)
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
    UPSTREAM_GUARD.validate_upstream()
    manifest = manifest_groups()
    (OUT / "oracle-before-declarations.json").write_text(json.dumps(current_oracle_snapshot(), indent=2) + "\n")
    replay_modal_projection(manifest)
    for group in manifest["groups"]:
        compare_group(group, GO, OUT / "original-go" / group["name"])
    tests = []
    for index, group in enumerate(manifest["originalTests"]):
        command = [str(GO_TOOL), "test", "-json", group["package"], "-run", "^(" + "|".join(group["functions"]) + ")$", "-count=1"]
        start = time.monotonic()
        result = subprocess.run(command, cwd=ROOT / "upstream/TypeScript/tsc", text=True, capture_output=True)
        log = "artifacts/declarations-validation/original-tests-" + str(index) + ".jsonl"
        (ROOT / log).write_text(result.stdout)
        (ROOT / (log + ".stderr")).write_text(result.stderr)
        events = [json.loads(line) for line in result.stdout.splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if result.returncode or sorted(passed) != sorted(group["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Original program assertions failed/skipped/incomplete")
        tests.append({"command": command, "functions": passed, "sourceFiles": group["sourceFiles"], "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": result.returncode, "log": log, "logSha256": sha(log), "stderrSha256": sha(log + ".stderr")})
    (OUT / "original-tests.json").write_text(json.dumps(tests, indent=2) + "\n")
    UPSTREAM_GUARD.validate_upstream()


def execute_stage(executable, managed=False):
    manifest = manifest_groups()
    for group in manifest["groups"]:
        compare_group(group, executable, OUT / ("managed-corpus" if managed else "native-corpus") / group["name"], managed)
    command = (["dotnet"] if managed else []) + [str(executable)]
    prefix = "managed" if managed else "native"
    registry = subprocess.run([*command, "--registry"], cwd=ROOT, text=True, capture_output=True, check=True)
    (OUT / (prefix + "-registry.json")).write_text(registry.stdout)
    if canonical_json(json.loads(registry.stdout)) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Native registry differs from manifest")
    result = subprocess.run([*command, "--controls"], cwd=ROOT, text=True, capture_output=True)
    (OUT / (prefix + "-controls.txt")).write_text(result.stdout)
    (OUT / (prefix + "-controls.stderr")).write_text(result.stderr)
    report = {"command": [*command, "--controls"], "exitCode": result.returncode,
              "executableSha256": sha(executable), "count": manifest["sharedChecks"]["count"],
              "stdoutSha256": sha(OUT / (prefix + "-controls.txt")), "stderrSha256": sha(OUT / (prefix + "-controls.stderr"))}
    (OUT / (prefix + "-controls-report.json")).write_text(json.dumps(report, indent=2) + "\n")
    if result.returncode or result.stdout.strip() != str(manifest["sharedChecks"]["count"]):
        raise RuntimeError("Program shared checks failed/incomplete")
    subprocess.run([sys.executable, "tools/check-program-order.py", "--manifest", MANIFEST, "--go", str(GO), "--target", str(executable), "--output", str(OUT / (prefix + "-order"))] + (["--managed"] if managed else []), cwd=ROOT, check=True)
    subprocess.run(cli_command(OUT / (prefix + "-cli"), managed), cwd=ROOT, check=True)


def comparison_reports(manifest, stage):
    executable = {"native-corpus": NATIVE, "managed-corpus": MANAGED, "original-go": GO}[stage]
    command = (["dotnet"] if stage == "managed-corpus" else []) + [str(executable)]
    reports = {}
    for group in manifest["groups"]:
        path = "artifacts/declarations-validation/" + stage + "/" + group["name"] + "/report.json"
        value = read(path)
        if value["total"] != group["cases"] or value["exact"] != group["cases"] or value["failures"] or any(value[k] != group[k] for k in ("requestsSha256", "expectedSha256")) or value["command"] != command:
            raise RuntimeError("Failed/incomplete Program comparison")
        if value["executableSha256"] != sha(executable) or value["executableDrift"] or value["exitCode"]:
            raise RuntimeError("Program comparison binary changed/failed")
        reports[group["name"]] = {"report": path, "reportSha256": sha(path), **value}
    return reports


def managed_snapshot():
    return {path.relative_to(ROOT).as_posix(): {"sha256": sha(path), "sizeBytes": path.stat().st_size}
            for directory in (MANAGED.parent, MANAGED_CLI.parent) for path in sorted(directory.iterdir()) if path.is_file()}


def check_trx(path):
    namespace = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    tree = ET.parse(ROOT / path)
    counts = {key: int(value) for key, value in tree.find(".//" + namespace + "Counters").attrib.items()}
    if counts["failed"] or counts["passed"] != counts["total"] or counts["total"] <= 0:
        raise RuntimeError("Failed/skipped/empty Program consumer TRX")
    names = [row.attrib["testName"] for row in tree.findall(".//" + namespace + "UnitTestResult")]
    return {"counts": counts, "names": names, "trx": path, "trxSha256": sha(path)}


def check_stage(manifest, managed=False):
    prefix = "managed" if managed else "native"
    executable, cli_executable = (MANAGED, MANAGED_CLI) if managed else (NATIVE, CLI)
    records = comparison_reports(manifest, prefix + "-corpus")
    if (OUT / (prefix + "-controls.txt")).read_text().strip() != str(manifest["sharedChecks"]["count"]) or canonical_json(read(OUT / (prefix + "-registry.json"))) != canonical_json(manifest["sharedChecks"]):
        raise RuntimeError("Incomplete Program shared registry/checks")
    control = read(OUT / (prefix + "-controls-report.json"))
    command = (["dotnet"] if managed else []) + [str(executable), "--controls"]
    if control["command"] != command or control["exitCode"] or control["executableSha256"] != sha(executable) or control["count"] != manifest["sharedChecks"]["count"]:
        raise RuntimeError("Program actual shared command/binary differs")
    for field in ("stdout", "stderr"):
        if control[field + "Sha256"] != sha(OUT / (prefix + "-controls." + ("txt" if field == "stdout" else "stderr"))):
            raise RuntimeError("Program shared artifact changed")
    order = read(OUT / (prefix + "-order/report.json"))
    for key in ("cases", "sequencesPerRuntime", "requestsSha256"):
        if order[key] != manifest["orderControls"][key]:
            raise RuntimeError("Program order inventory differs")
    if order["failures"] or order["originalGoExact"] != manifest["orderControls"]["executionsPerRuntime"] or order["targetExact"] != manifest["orderControls"]["executionsPerRuntime"]:
        raise RuntimeError("Program order checks failed")
    for label, target in (("originalGo", GO), ("target", executable)):
        if order["binarySha256Before"][label] != sha(target) or order["binarySha256After"][label] != sha(target):
            raise RuntimeError("Program order executable drift")
    target_command = (["dotnet"] if managed else []) + [str(executable)]
    if order["commands"] != {"originalGo": [str(GO)], "target": target_command} or any(row["exitCode"] for row in order["records"]):
        raise RuntimeError("Program order command failed/changed")
    cli = read(OUT / (prefix + "-cli/report.json"))
    expected = manifest["cliControls"]
    hashes = {"original": sha(SEMANTIC_GO), "target": sha(cli_executable)}
    if cli["cases"] != 2 or cli["exact"] != 2 or cli["failures"] or cli["managed"] is not managed or cli["requestsSha256"] != expected["requestsSha256"]:
        raise RuntimeError("Warehouse native/managed checking failed/incomplete")
    if cli["manifestSha256"] != sha(MANIFEST) or cli["executableDrift"] or cli["executableHashesBefore"] != hashes or cli["executableHashesAfter"] != hashes:
        raise RuntimeError("Warehouse actual executable/input drift")
    if cli["originalExitCode"] or cli["originalCommand"] != [str(SEMANTIC_GO)] or cli["runnerSha256"] != expected["runnerSha256"] or cli["sourceFiles"] != expected["sourceFiles"]:
        raise RuntimeError("Warehouse original producer changed")
    stem = OUT / (prefix + "-cli")
    for field in ("originalStdout", "originalStderr"):
        suffix = ".stdout" if field.endswith("Stdout") else ".stderr"
        if cli[field + "Sha256"] != sha(stem / ("original" + suffix)):
            raise RuntimeError("Warehouse complete Go diagnostics artifact changed")
    requests = [json.loads(line) for line in (ROOT / expected["requests"]).read_text().splitlines()]
    if len(cli["records"]) != 2:
        raise RuntimeError("Warehouse checks omit a disk program")
    for row, request in zip(cli["records"], requests):
        command = (["dotnet"] if managed else []) + [str(cli_executable), "--config", request["ConfigPath"], "--json"]
        if not row["passed"] or row["name"] != request["Name"] or row["command"] != command or row["difference"] is not None or row["exitCode"] != expected["expectedExits"][row["name"]]:
            raise RuntimeError("Warehouse exact diagnostic/exit association differs")
        for field in ("stdout", "stderr"):
            if row[field + "Sha256"] != sha(stem / (row["name"] + "." + field)):
                raise RuntimeError("Warehouse target artifact changed")
    return {"corpus": records, "order": order, "cli": cli,
            "registrySha256": sha(OUT / (prefix + "-registry.json")),
            "sharedControls": control,
            "controlsStdoutSha256": sha(OUT / (prefix + "-controls.txt")),
            "controlsStderrSha256": sha(OUT / (prefix + "-controls.stderr"))}


def check_original(manifest):
    original = comparison_reports(manifest, "original-go")
    direct = read(OUT / "original-tests.json")
    groups = manifest["originalTests"]
    if len(direct) != len(groups):
        raise RuntimeError("Program original test package inventory differs")
    for expected, actual in zip(groups, direct):
        command = [str(GO_TOOL), "test", "-json", expected["package"], "-run", "^(" + "|".join(expected["functions"]) + ")$", "-count=1"]
        if actual["command"] != command or actual["exitCode"] or sorted(actual["functions"]) != sorted(expected["functions"]) or actual["sourceFiles"] != expected["sourceFiles"] or sha(actual["log"]) != actual["logSha256"] or sha(actual["log"] + ".stderr") != actual["stderrSha256"]:
            raise RuntimeError("Program original test proof differs")
        events = [json.loads(line) for line in (ROOT / actual["log"]).read_text().splitlines()]
        passed = [row["Test"] for row in events if row["Action"] == "pass" and "Test" in row and "/" not in row["Test"]]
        if sorted(passed) != sorted(expected["functions"]) or any(row["Action"] in ("fail", "skip") for row in events):
            raise RuntimeError("Program original assertions failed/skipped/incomplete")
    projection = manifest["jsonErrorProjection"]
    modal = read(OUT / "json-error-controls-report.json")
    if modal["command"] != [str(GO), "--json-error-controls"] or modal["exitCode"] or modal["executableSha256"] != sha(GO) or modal["expectedSha256"] != projection["expectedSha256"]:
        raise RuntimeError("Typed SemanticError control proof executable differs")
    for field in ("stdout", "stderr"):
        if modal[field + "Sha256"] != sha(OUT / ("json-error-controls." + field)):
            raise RuntimeError("Typed SemanticError raw observation artifact changed")
    if json.loads((OUT / "json-error-controls.stdout").read_bytes()) != read(projection["expected"]):
        raise RuntimeError("Typed SemanticError observation controls differ")
    if read(OUT / "oracle-before-declarations.json") != current_oracle_snapshot():
        raise RuntimeError("Program original oracle/toolchain changed")
    return {"corpus": original, "directTests": direct, "jsonErrorProjection": modal}


def check_managed():
    manifest = manifest_groups()
    original = check_original(manifest)
    managed = check_stage(manifest, True)
    tests = declarations_xunit()
    classes = {**manifest["sharedChecks"]["xUnitClassCounts"], **corpus_class_counts(manifest)}
    if tests["classCaseCounts"] != classes or tests["counts"]["total"] != manifest["cases"] + manifest["sharedChecks"]["count"]:
        raise RuntimeError("Program xUnit actual class/case registry differs")
    controls = {}
    for name in ("binder", "checker", "scanner", "configuration-integration"):
        controls[name] = check_trx("artifacts/declarations-validation/retained-consumers/" + name + ".trx")
    if controls["configuration-integration"]["counts"]["total"] != 1:
        raise RuntimeError("Migrated authored source environment fixture omitted")
    if read(OUT / "managed-before-declarations.json") != managed_snapshot():
        raise RuntimeError("Program managed dependency binaries changed")
    return {"original": original, "managed": managed, "declarationsXUnit": tests, "retainedConsumers": controls}


def check_execution():
    UPSTREAM_GUARD.validate_upstream()
    manifest = manifest_groups()
    managed = check_managed()
    native = check_stage(manifest)
    old = retained.check_execution()
    if read(OUT / "native-before-declarations.json") != current_native_snapshot():
        raise RuntimeError("Program native executable changed during execution")
    binaries, oracles = native_snapshot(), oracle_snapshot()
    (OUT / "native-after-declarations.json").write_text(json.dumps(binaries, indent=2) + "\n")
    (OUT / "oracle-after-declarations.json").write_text(json.dumps(oracles, indent=2) + "\n")
    return {**managed, "native": native, "retainedExecution": old, "originalOracleBinaries": oracles,
            "currentCheckingEnvironment": manifest["currentCheckingEnvironment"],
            "currentCheckingReplacement": manifest["currentCheckingReplacement"],
            "managedBinaries": managed_snapshot()}


def run_gate():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_groups(); LIBRARY_GUARD.validate_libraries(); UPSTREAM_GUARD.validate_upstream()
    source, files, protected = sources(), assets(), historical()
    selected_obligations = obligations()
    asset = fingerprint(files)
    command = ["bash", "tools/verify-declarations.sh"]
    start, utc = time.monotonic(), datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Final program gate log: " + str(OUT / "final-gate.log"), flush=True)
    with (OUT / "final-gate.log").open("w") as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    asset_drift = files != assets() or asset != fingerprint(assets())
    historical_drift = any(not (ROOT / name).is_file() or sha(name) != digest for name, digest in protected.items())
    try:
        UPSTREAM_GUARD.validate_upstream(); upstream_drift = False
    except RuntimeError:
        upstream_drift = True
    obligation_error = None
    try:
        obligation_drift = selected_obligations != obligations()
    except (OSError, RuntimeError, ValueError, KeyError) as error:
        obligation_drift = True
        obligation_error = str(error)
    status = result.returncode or int(source_drift or asset_drift or historical_drift or upstream_drift or obligation_drift)
    report = {"command": command, "baseline": BASELINE, "startedAtUTC": utc, "elapsedSeconds": round(time.monotonic() - start, 3), "exitCode": status, "sourceSha256": source, "verificationAssetsSha256": asset, "sourceDrift": source_drift, "assetDrift": asset_drift, "historicalAssetDrift": historical_drift, "upstreamDrift": upstream_drift, "obligationDrift": obligation_drift, "obligationError": obligation_error, "reviewedAssertionObligations": selected_obligations, "verificationAssetFiles": [p.relative_to(ROOT).as_posix() for p in files], "protectedHistoricalAssetFiles": protected, "log": "artifacts/declarations-validation/final-gate.log"}
    (OUT / "final-gate.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: v for k, v in report.items() if k not in ("verificationAssetFiles", "protectedHistoricalAssetFiles", "reviewedAssertionObligations")}), flush=True)
    raise SystemExit(status)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--preflight", action="store_true")
    modes.add_argument("--reproduce-original", action="store_true")
    modes.add_argument("--capture-native")
    modes.add_argument("--capture-managed", action="store_true")
    modes.add_argument("--check-managed", action="store_true")
    modes.add_argument("--execute-native", action="store_true")
    modes.add_argument("--execute-managed", action="store_true")
    modes.add_argument("--check-execution", action="store_true")
    parser.add_argument("--executable", type=pathlib.Path, default=NATIVE)
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    if args.preflight:
        manifest_groups(); LIBRARY_GUARD.validate_libraries(); historical(); UPSTREAM_GUARD.validate_upstream()
    elif args.reproduce_original:
        reproduce_original()
    elif args.capture_managed:
        (OUT / "managed-before-declarations.json").write_text(json.dumps(managed_snapshot(), indent=2) + "\n")
    elif args.check_managed:
        check_managed()
    elif args.capture_native:
        (ROOT / args.capture_native).write_text(json.dumps(current_native_snapshot(), indent=2) + "\n")
    elif args.execute_native:
        execute_stage(args.executable)
    elif args.execute_managed:
        execute_stage(MANAGED, True)
    elif args.check_execution:
        check_execution()
    else:
        run_gate()


if __name__ == "__main__":
    main()
