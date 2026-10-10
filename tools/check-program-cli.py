#!/usr/bin/env python3
"""Compare ordinary --inspect-program with the pinned public Program observer.

Fixture paths/bytes must already exist on disk exactly as requested; no path or
message normalization. This bounded ordinary view preserves sources/traces and
projects public diagnostic policy, without acquiring a checker.
"""
import argparse
import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("retained_cli_roles", ROOT / "tools/check-resolution-cli.py")
roles = importlib.util.module_from_spec(spec)
spec.loader.exec_module(roles)
BOUNDARY = "Program source/library/type/reference construction, syntax/bind and construction diagnostics; semantic checking, emit, build/watch and content-mapper subprocess execution are not requested."


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def contents(item):
    return base64.b64decode(item["TextBase64"], validate=True) if item.get("TextBase64") is not None else item.get("Text", "").encode()


def attach_text(source, request):
    fixture = {row["Path"]: contents(row) for row in request["Files"]}
    owners = {}
    for owner in source["owners"]:
        name = roles.display(owner["nameBase64"])
        if name.startswith("bundled:///libs/"):
            path = ROOT / "src/TypeScript.Scanner/Bundled/Libs" / name.removeprefix("bundled:///libs/")
            if path.parent != ROOT / "src/TypeScript.Scanner/Bundled/Libs":
                raise ValueError("Unowned bundled diagnostic path")
            text = path.read_bytes()
        else:
            text = fixture[name]
        if len(text) != owner["textBytes"] or hashlib.sha256(text).hexdigest() != owner["textSha256"]:
            raise ValueError("Diagnostic owner text identity mismatch")
        owners[owner["id"]] = dict(owner, textBase64=base64.b64encode(text).decode())
    return owners


def merge_diagnostics(rows, owners):
    # The ordinary fixtures have unique source-comparator primary keys or exact
    # duplicate records. Reject a richer tie rather than approximate Go's chain
    # and related-info merge semantics in another general diagnostic framework.
    by_key = {}
    for row in rows:
        key = (b"" if row["file"] is None else base64.b64decode(owners[row["file"]]["nameBase64"], validate=True), row["pos"], row["end"], row["code"], row["category"])
        if key in by_key and by_key[key] != row:
            raise ValueError("Ordinary diagnostic fixture requires original comparator tie projection")
        by_key[key] = row
    return [by_key[key] for key in sorted(by_key)]


def project(source, request):
    if source["readDiagnostics"] or len(source["snapshots"]) != 1:
        raise ValueError("Ordinary Program fixture must produce exactly one actual construction")
    snapshot = source["snapshots"][0]
    owners = attach_text(source, request)
    categories = {"ConfigurationDiagnostics": snapshot["configDiagnostics"] or [], "SyntacticDiagnostics": snapshot["syntacticDiagnostics"] or [], "BindDiagnostics": snapshot["bindDiagnostics"] or [], "ConstructionDiagnostics": snapshot["programDiagnostics"] or []}
    categories["InclusionDiagnostics"] = merge_diagnostics([diag for file in snapshot["files"] for diag in file["includeDiagnostics"] or []], owners)
    aggregate = merge_diagnostics([diag for category in categories.values() for diag in category], owners)
    modules = []
    for row in snapshot["modules"]:
        modules.append(dict(ImporterPathBase64=row["pathBase64"], NameBase64=row["nameBase64"], Mode=roles.MODES[row["mode"]], Module=None if row["result"] is None else roles.module(row["result"], owners), Type=None))
    types = []
    for row in snapshot["typeDirectives"]:
        result = row["result"]
        types.append(dict(ImporterPathBase64=row["pathBase64"], NameBase64=row["nameBase64"], Mode=roles.MODES[row["mode"]], Module=None, Type=None if result is None else dict(FileBase64=roles.encoded(result["ResolvedFileName"]), PathBase64=roles.encoded(result["ResolvedPath"]), OriginalPathBase64=roles.encoded(result["OriginalPath"]), Primary=result["Primary"], ExternalLibraryImport=result["IsExternalLibraryImport"], Package=roles.package(result["PackageId"]), Diagnostics=roles.diagnostic(result["ResolutionDiagnostics"], owners))))
    # Association maps have an explicitly deterministic CLI presentation order;
    # actual Program source/reference order and trace streams are untouched.
    key = lambda row: (row["ImporterPathBase64"], row["NameBase64"], row["Mode"])
    sources = []
    for row in snapshot["files"]:
        metadata = row["metadata"]
        sources.append(dict(FileBase64=row["nameBase64"], PathBase64=row["pathBase64"], ScriptKind=roles.SCRIPT_KINDS[row["scriptKind"]], Declaration=row["declaration"], DefaultLibrary=row["defaultLibrary"], ExternalLibrary=row["externalLibrary"], ExternalModule=row["externalModule"], PackageType=roles.display(roles.encoded(metadata["PackageJsonType"])), PackageDirectoryBase64=roles.encoded(metadata["PackageJsonDirectory"]), ImpliedNodeFormat=roles.MODES[metadata["ImpliedNodeFormat"]], LibraryReferencesBase64=None if row["libReferences"] is None else [roles.encoded(reference["FileName"]) for reference in row["libReferences"]]))
    failure = snapshot["moduleResolutionErrorBase64"]
    return dict(ExitCode=2 if failure is not None else int(bool(aggregate)), Error=None if failure is None else roles.display(failure), Diagnostics=roles.diagnostic(aggregate, owners), Files=[roles.display(row["nameBase64"]) for row in snapshot["files"]], FileNamesBase64=[row["nameBase64"] for row in snapshot["files"]], Program=dict(Boundary=BOUNDARY, SemanticCheckingRequested=False, RootsBase64=[roles.encoded(path) for path in snapshot["roots"] or []], Sources=sources, Modules=sorted(modules, key=key), Types=sorted(types, key=key), References=[dict(PathBase64=row["pathBase64"], ConfigBase64=row["configBase64"], ParentConfigBase64=row["parentBase64"], Index=row["index"]) for row in snapshot["referenceTraversal"]], Traces=roles.traces(source["traces"]), **{name: roles.diagnostic(rows, owners) for name, rows in categories.items()}))


def actual_view(value):
    for trace in value["Program"]["Traces"] or []:
        for argument in trace["Arguments"] or []:
            if argument.pop("Kind") not in roles.KINDS:
                raise ValueError("Unknown CLI trace argument role")
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--requests", type=Path, required=True)
    parser.add_argument("--original", type=Path, required=True, help="Pinned public Program observer")
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--managed", action="store_true")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.executable = args.executable.resolve()
    args.original = args.original.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    initial_hashes = {"cli": digest(args.executable), "go": digest(args.original)}
    requests = [json.loads(line) for line in args.requests.read_text().splitlines() if line]
    if not requests or len({request["Name"] for request in requests}) != len(requests):
        raise ValueError("Ordinary requests must be nonempty with unique names")
    hashes = {}
    for request in requests:
        if not request.get("Bind") or request.get("Changes") or request.get("Dependency"):
            raise ValueError("Ordinary inspection requires Bind=true and no lifecycle/dependency request")
        if request["ConfigPath"] not in {file["Path"] for file in request["Files"]}:
            raise ValueError("Configuration must be an actual owned fixture file")
        for file in request["Files"]:
            if file.get("Symlink") or file.get("PathBase64"):
                raise ValueError("Ordinary scalar disk fixtures require explicit real files")
            path = Path(file["Path"])
            if not path.is_absolute() or path.read_bytes() != contents(file):
                raise ValueError("Original/disk fixture identity differs")
            hashes[str(path)] = digest(path)
    started = time.monotonic()
    original_command = [str(args.original)]
    original = subprocess.run(original_command, input=args.requests.read_bytes(), capture_output=True, cwd=ROOT, timeout=120)
    (args.output / "original.stdout").write_bytes(original.stdout)
    (args.output / "original.stderr").write_bytes(original.stderr)
    if original.returncode != 0:
        raise RuntimeError("Original observer failed")
    observations = [json.loads(line) for line in original.stdout.splitlines()]
    if len(observations) != len(requests):
        raise ValueError("Original observation count mismatch")
    cases, failures = [], []
    for request, source in zip(requests, observations):
        if source["name"] != request["Name"]:
            raise ValueError("Original response association mismatch")
        expected = project(source, request)
        command = (["dotnet"] if args.managed else []) + [str(args.executable), "--inspect-program", request["ConfigPath"]]
        process = subprocess.run(command, capture_output=True, cwd=ROOT, timeout=120)
        name = request["Name"]
        (args.output / (name + ".stdout")).write_bytes(process.stdout)
        (args.output / (name + ".stderr")).write_bytes(process.stderr)
        actual = actual_view(roles.compare.parse(process.stdout))
        (args.output / (name + ".expected.json")).write_text(json.dumps(expected, indent=2) + "\n")
        difference = roles.compare.difference(expected, actual)
        passed = difference is None and process.returncode == expected["ExitCode"] == actual["ExitCode"] and not process.stderr
        cases.append(dict(name=name, passed=passed, exitCode=process.returncode, expectedExitCode=expected["ExitCode"], differences=difference, command=command,
                          requestSha256=hashlib.sha256(json.dumps(request, separators=(",", ":"), ensure_ascii=False).encode()).hexdigest(),
                          stdoutSha256=hashlib.sha256(process.stdout).hexdigest(), stderrSha256=hashlib.sha256(process.stderr).hexdigest()))
        if not passed:
            failures.append(name)
    final_hashes = {"cli": digest(args.executable), "go": digest(args.original)}
    drift = initial_hashes != final_hashes
    if drift:
        failures.append("Executable drift")
    report = dict(cases=len(cases), passed=sum(row["passed"] for row in cases), failures=failures, records=cases, managed=args.managed,
                  seconds=round(time.monotonic()-started,3), requestsSha256=digest(args.requests), observerSha256=final_hashes["go"], executableSha256=final_hashes["cli"],
                  executableHashes=initial_hashes, finalExecutableHashes=final_hashes, executableDrift=drift, sourceFiles=hashes,
                  originalCommand=original_command, runnerSha256=digest(Path(__file__)),
                  originalStdoutSha256=digest(args.output / "original.stdout"), originalStderrSha256=digest(args.output / "original.stderr"),
                  projectionLimits="Full ordinary public DTO and diagnostics; static trace type names are validated but not compared to Go. Only exact duplicates or unique diagnostic comparator primary keys are admitted. No semantic checker acquisition or private-state observation.")
    (args.output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    return int(bool(failures))


if __name__ == "__main__":
    raise SystemExit(main())
