#!/usr/bin/env python3
"""Ordinary disk CLI against pinned Go Program and actual loader observations.

No expected value comes from the target. Absolute fixture names are identical in
both environments, so no diagnostic text/path normalization is needed.
"""
import argparse
import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("resolution_compare", ROOT / "tools/compare-resolution.py")
compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compare)
BOUNDARY = "Source dependency graph; default libraries, automatic global types, project-reference/content-mapper execution and semantic checking are external."
MODES = {0: "None", 1: "CommonJS", 99: "ESNext"}
SCRIPT_KINDS = {0: "Unknown", 1: "JS", 2: "JSX", 3: "TS", 4: "TSX", 5: "External", 6: "JSON", 7: "Deferred"}
KINDS = {"string", "rooted-file", "rooted-directory", "rooted-path", "path-key", "package-directory", "null", "bool", "number"}
SYNTHETIC_KINDS = {"": "", "import-helpers": "importHelpers", "jsx-runtime": "jsxRuntime"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def encoded(value):
    return value["bytesBase64"]


def display(value):
    return base64.b64decode(value, validate=True).decode("utf-8", errors="replace")


def diagnostic(rows, owners):
    if rows is None:
        return None
    output = []
    for row in rows:
        owner = owners[row["file"]] if row["file"] is not None else None
        line = column = 0
        if owner is not None and row["pos"] >= 0:
            # These ordinary disk fixtures are scalar UTF-8. Source byte spans
            # stay unchanged; this mirrors the public ECMAScript UTF16 location.
            prefix = base64.b64decode(owner["textBase64"], validate=True)[:row["pos"]].decode("utf-8")
            lines = re.split(r"\r\n|[\r\n\u2028\u2029]", prefix)
            line, column = len(lines), len(lines[-1].encode("utf-16-le")) // 2 + 1
        output.append(dict(File=None if owner is None else display(owner["nameBase64"]), Code=row["code"],
                           Category=["Warning", "Error", "Suggestion", "Message"][row["category"]],
                           Message=display(row["textBase64"]), ByteStart=row["pos"], ByteEnd=row["end"], Line=line, Column=column,
                           Chain=diagnostic(row["chain"], owners), Related=diagnostic(row["related"], owners)))
    return output


def package(row):
    return {key + "Base64": encoded(row[key]) for key in ("Name", "SubModuleName", "Version", "PeerDependencies")}


def module(row, owners):
    return dict(File=display(encoded(row["ResolvedFileName"])), FileBase64=encoded(row["ResolvedFileName"]),
                PathBase64=encoded(row["ResolvedPath"]), OriginalPathBase64=encoded(row["OriginalPath"]),
                AlternateResultBase64=encoded(row["AlternateResult"]), Extension=display(encoded(row["Extension"])),
                UsingTsExtension=row["ResolvedUsingTsExtension"], UsingExtraExtensions=row["ResolvedUsingExtraExtensions"],
                ExternalLibraryImport=row["IsExternalLibraryImport"], CustomResolution=row["IsCustomResolution"],
                Package=package(row["PackageId"]), Diagnostics=diagnostic(row["ResolutionDiagnostics"], owners))


def traces(rows):
    if rows is None:
        return None
    result = []
    for row in rows:
        args = None if row["args"] is None else []
        for value in row["args"] or []:
            if isinstance(value, dict) and value.keys() == {"bytesBase64"}:
                args.append(dict(BytesBase64=encoded(value), Text=display(encoded(value))))
            elif value is None or type(value) in (bool, int):
                args.append(dict(BytesBase64=None, Text=None if value is None else str(value).lower()))
            else:
                raise ValueError("Unknown source trace argument payload")
        result.append(dict(Code=row["code"], Arguments=args))
    return result


def graph_result(source):
    graph, owners = source["graph"], {row["id"]: row for row in source["diagnosticOwners"]}
    files = {row["identity"]: row for row in graph["files"]}
    def file_name(identity):
        return None if identity is None else encoded(files[identity]["fileName"])
    rows = []
    for row in graph["files"]:
        metadata = row["metadata"]
        lib_refs = [encoded(edge["reference"]["FileName"]) for edge in graph["externalLibReferences"] if edge["file"] == row["identity"]]
        rows.append(dict(FileBase64=encoded(row["fileName"]), PathBase64=encoded(row["path"]),
                         ScriptKind=SCRIPT_KINDS[row["scriptKind"]], PackageType=display(encoded(metadata["PackageJsonType"])),
                         PackageDirectoryBase64=encoded(metadata["PackageJsonDirectory"]), ImpliedNodeFormat=MODES[metadata["ImpliedNodeFormat"]],
                         Declaration=row["isDeclarationFile"], ExternalModule=row["externalModule"], LibraryReferencesBase64=lib_refs or None))
    edges = [dict(ImporterBase64=file_name(row["file"]), ReferenceBase64=encoded(row["reference"]),
                  ByteStart=row["specifier"]["pos"], ByteEnd=row["specifier"]["end"], ImportIndex=row["importIndex"],
                  Mode=MODES[row["mode"]], Synthetic=SYNTHETIC_KINDS[row["syntheticKind"]], Scheduling=row["scheduling"],
                  TargetBase64=file_name(row["target"]), Resolution=module(row["resolution"], owners), Traces=traces(row["trace"]),
                  Error=None if row["errorBase64"] is None else display(row["errorBase64"])) for row in graph["edges"]]
    paths = []
    for row in graph["pathReferences"]:
        ds = diagnostic(row["diagnostic"], owners)
        paths.append(dict(ImporterBase64=file_name(row["file"]), ReferenceBase64=encoded(row["reference"]["FileName"]),
                          ByteStart=row["reference"]["pos"], ByteEnd=row["reference"]["end"], Scheduling=row["scheduling"], Mode="",
                          FileBase64=encoded(row["resolvedFile"]), PathBase64=encoded(row["resolvedPath"]), TargetBase64=file_name(row["target"]),
                          Diagnostic=None if ds is None else ds[0], Traces=None))
    types = []
    for row in graph["typeDirectives"]:
        resolution = row["resolution"]
        edge = dict(ImporterBase64=file_name(row["file"]), ReferenceBase64=encoded(row["reference"]["FileName"]),
                    ByteStart=row["reference"]["pos"], ByteEnd=row["reference"]["end"], Scheduling=row["scheduling"], Mode=MODES[row["mode"]],
                    FileBase64=None if resolution is None else encoded(resolution["ResolvedFileName"]),
                    PathBase64=None if resolution is None else encoded(resolution["ResolvedPath"]), TargetBase64=file_name(row["target"]),
                    Diagnostic=None, Traces=traces(row["trace"]))
        if resolution is not None:
            edge["TypeResolution"] = dict(FileBase64=encoded(resolution["ResolvedFileName"]), PathBase64=encoded(resolution["ResolvedPath"]),
                                          OriginalPathBase64=encoded(resolution["OriginalPath"]), Primary=resolution["Primary"],
                                          ExternalLibraryImport=resolution["IsExternalLibraryImport"], Package=package(resolution["PackageId"]),
                                          Diagnostics=diagnostic(resolution["ResolutionDiagnostics"], owners))
        types.append(edge)
    if source["parseDiagnostics"] is not None or source["configDiagnostics"] is not None:
        raise ValueError("Ordinary graph fixture configuration must be valid before graph inspection")
    diagnostics = []
    for source_file in graph["files"]:
        diagnostics += diagnostic(source_file["parseDiagnostics"], owners) or []
    diagnostics += diagnostic(graph["inclusionDiagnostics"], owners) or []
    failures = bool(diagnostics) or any(row["errorBase64"] is not None or encoded(row["resolution"]["ResolvedFileName"]) == "" for row in graph["edges"]) or any(row["diagnostic"] is not None for row in graph["pathReferences"]) or any(row["scheduling"] == "Unresolved" for row in graph["typeDirectives"])
    return dict(ExitCode=int(failures), Error=None, Files=[display(encoded(row["fileName"])) for row in graph["files"]],
                FileNamesBase64=[encoded(row["fileName"]) for row in graph["files"]], Diagnostics=diagnostics,
                Graph=dict(Boundary=BOUNDARY, RootsBase64=[encoded(row) for row in graph["roots"]], MissingFilesBase64=[encoded(row) for row in graph["missingFiles"]],
                           Sources=rows, Edges=edges, PathReferences=paths, TypeDirectives=types))


def cli_graph_view(actual):
    """Trace kind names are authored CLI descriptors; compare original payloads.

    Original graph traces retain actual values but do not serialize their static
    Go type. All descriptor kinds fail closed; the package-directory fixture
    separately checks the reached source producer's explicit descriptor.
    """
    actual = json.loads(json.dumps(actual))
    actual["Graph"]["MissingFilesBase64"].sort(key=lambda value: base64.b64decode(value, validate=True))
    for family in ("Edges", "TypeDirectives"):
        for edge in actual["Graph"][family]:
            for trace in edge["Traces"] or []:
                for argument in trace["Arguments"] or []:
                    if argument.pop("Kind") not in KINDS:
                        raise ValueError("Unknown CLI trace descriptor")
    return actual


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--managed", action="store_true")
    parser.add_argument("--go", required=True, type=Path, help="Pinned actual loader observer")
    parser.add_argument("--semantic-go", required=True, type=Path, help="Pinned genuine Program observer")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--current-checking-boundaries", type=Path,
                        help="Explicit authored boundary or current source-semantic promotion context; historical default stays unchanged")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    hashes = {"cli": digest(args.executable), "go": digest(args.go), "semanticGo": digest(args.semantic_go)}
    boundaries = None
    promoted_semantics = None
    boundary_hash = None
    if args.current_checking_boundaries is not None:
        boundary_hash = digest(args.current_checking_boundaries)
        context = compare.parse(args.current_checking_boundaries.read_bytes())
        if set(context) != {"classification", "cases"} or context["classification"] not in {
            "authored-current-semantic-boundaries", "authored-controls-promoted-to-pinned-go-semantics"
        }:
            raise ValueError("Unknown current checking boundary context")
        selected = context["cases"]
        if set(selected) != {"package-graph-check", "package-graph-invalid-check"}:
            raise ValueError("Current boundary context must replace exactly the two authored mixed-graph controls")
        if context["classification"] == "authored-current-semantic-boundaries":
            boundaries = selected
            for expected_boundary in boundaries.values():
                if set(expected_boundary) != {"ExitCode", "Error", "Diagnostics"} or expected_boundary["ExitCode"] != 2 or expected_boundary["Diagnostics"] != [] or not isinstance(expected_boundary["Error"], str) or not expected_boundary["Error"]:
                    raise ValueError("Current unsupported boundary must retain explicit failure and empty diagnostic payload")
        else:
            promoted_semantics = selected
            for promotion in promoted_semantics.values():
                if promotion != {"reference": "pinned-go-semantics", "ExitCode": 1}:
                    raise ValueError("Current promotion requires the exact source-semantic reference and error exit")
    failures, records, fixture_hashes = [], [], {}
    start = time.monotonic()
    cases = [("package-checking", "check", "semantic", 0), ("package-checking-invalid", "check", "semantic", 1),
             ("package-graph", "inspect", "graph", 0), ("package-graph-invalid", "inspect", "graph", 1),
             ("package-graph", "check", "unsupported", 2), ("package-graph-invalid", "check", "unsupported", 2),
             ("package-checking-missing-export", "check", "semantic", 1), ("package-checking-unicode", "check", "semantic", 0),
             ("package-checking-node-builtin", "check", "semantic", 1), ("package-checking-types-package", "check", "semantic", 1),
             ("package-directory-trace", "inspect", "graph", 1)]
    with tempfile.TemporaryDirectory(prefix="resolution-cli-") as directory:
        root = Path(directory)
        for case, action, reference, exit_code in cases:
            row_name = case + "-" + action
            if promoted_semantics is not None and row_name in promoted_semantics:
                if reference != "unsupported":
                    raise ValueError("Only the two historical authored capability controls may be promoted")
                reference, exit_code = "semantic", promoted_semantics[row_name]["ExitCode"]
            source_case = "package-checking" if case.startswith("package-checking-") and case != "package-checking-invalid" else case
            source_dir = ROOT / "examples/resolution" / source_case
            files = {}
            if case != "package-directory-trace":
                for path in sorted(source_dir.rglob("*")):
                    if path.is_file():
                        fixture_hashes[str(path.relative_to(ROOT))] = digest(path)
                        files[str(root / "examples/resolution" / case / path.relative_to(source_dir))] = path.read_text()
                base = ROOT / "examples/configuration/checking-base.json"
                fixture_hashes[str(base.relative_to(ROOT))] = digest(base)
                files[str(root / "examples/configuration/checking-base.json")] = base.read_text()
            config = root / "examples/resolution" / case / "tsconfig.json"
            if case == "package-checking-missing-export":
                files[str(config.parent / "src/main.ts")] += '\nimport { nonexistent } from "record-kit";\nexport const missing = nonexistent;\n'
            if case == "package-checking-unicode":
                old = str(config.parent / "src/main.ts")
                files[str(config.parent / "src/é-𐀀.ts")] = files.pop(old)
                value = json.loads(files[str(config)]); value["files"] = ["src/é-𐀀.ts"]
                files[str(config)] = json.dumps(value)
            if case == "package-checking-node-builtin":
                files[str(config.parent / "src/main.ts")] += '\nimport { readFile } from "node:fs";\nexport const builtin = readFile;\n'
            if case == "package-checking-types-package":
                files[str(config.parent / "src/main.ts")] += '\nimport { value } from "@types/typed-kit";\nexport const directTypes = value;\n'
                files[str(config.parent / "node_modules/@types/typed-kit/package.json")] = '{"name":"@types/typed-kit","version":"1.0.0","types":"index.ts"}'
                files[str(config.parent / "node_modules/@types/typed-kit/index.ts")] = 'export const value = 1;\n'
            if case == "package-directory-trace":
                files = {str(config): json.dumps({"compilerOptions": {"target": "esnext", "module": "esnext", "moduleResolution": "bundler", "noLib": True, "types": [], "traceResolution": True}, "files": ["src/main.ts"]}),
                         str(config.parent / "package.json"): '{"name":"trace-owner","imports":{"#known":"./src/dep.ts"}}',
                         str(config.parent / "src/main.ts"): 'import { value } from "#missing";\nexport const result = value;\n',
                         str(config.parent / "src/dep.ts"): 'export const value = 1;\n'}
            for path, text in files.items():
                destination = Path(path); destination.parent.mkdir(parents=True, exist_ok=True); destination.write_text(text)
            row_name = case + "-" + action
            request = {"Name": row_name, "ConfigPath": str(config), "Files": files}
            if reference == "graph":
                request = dict(request, Cwd=str(config.parent), ConfigText=files[str(config)], Graph=True, Queries=[], Files=[dict(Path=path, Text=text) for path, text in files.items()])
            raw = (json.dumps(request, ensure_ascii=True) + "\n").encode()
            stem = args.output / row_name
            stem.with_suffix(".request.jsonl").write_bytes(raw)
            expected = None
            if reference != "unsupported":
                oracle = args.go if reference == "graph" else args.semantic_go
                original = subprocess.run([str(oracle.resolve())], input=raw, capture_output=True, cwd=ROOT, timeout=120)
                stem.with_suffix(".original.jsonl").write_bytes(original.stdout)
                stem.with_suffix(".original.stderr.txt").write_bytes(original.stderr)
                if original.returncode != 0:
                    raise ValueError("Original observer failed for " + row_name)
                original_row = compare.parse(original.stdout)
                expected = graph_result(original_row) if reference == "graph" else original_row["Result"]
            command = (["dotnet"] if args.managed else []) + [str(args.executable.resolve()), "--inspect-project" if action == "inspect" else "--config", str(config)]
            if action == "check": command.append("--json")
            process = subprocess.run(command, capture_output=True, cwd=ROOT, timeout=120)
            stem.with_suffix(".stdout.json").write_bytes(process.stdout); stem.with_suffix(".stderr.txt").write_bytes(process.stderr)
            reason = None
            try:
                actual = compare.parse(process.stdout)
                if process.returncode != exit_code or actual["ExitCode"] != exit_code:
                    raise ValueError("Ordinary CLI exit differs")
                if reference == "unsupported":
                    error = "Checking requires noEmit, noLib and skipLibCheck; its three genuine libraries are loaded explicitly. Use --inspect-config to inspect this configuration without checking."
                    boundary = boundaries[row_name] if boundaries is not None else dict(ExitCode=2, Error=error, Diagnostics=[])
                    if actual != boundary:
                        raise ValueError("Explicit checking capability response differs")
                else:
                    view = cli_graph_view(actual) if reference == "graph" else actual
                    mismatch = compare.difference(expected, view)
                    if mismatch: raise ValueError(json.dumps(mismatch))
                if case == "package-directory-trace":
                    if not any(argument["Kind"] == "package-directory" for edge in actual["Graph"]["Edges"] for trace in edge["Traces"] or [] for argument in trace["Arguments"] or []):
                        raise ValueError("Actual package-directory descriptor was not reached")
            except (ValueError, KeyError, TypeError) as error:
                reason = str(error); failures.append(dict(name=row_name, reason=reason))
            records.append(dict(name=row_name, reference=reference, exitCode=process.returncode, expectedExitCode=exit_code, exact=reason is None,
                                requestSha256=hashlib.sha256(raw).hexdigest(), stdoutSha256=hashlib.sha256(process.stdout).hexdigest(), stderrSha256=hashlib.sha256(process.stderr).hexdigest()))
    drift = hashes != {"cli": digest(args.executable), "go": digest(args.go), "semanticGo": digest(args.semantic_go)}
    if boundary_hash is not None and boundary_hash != digest(args.current_checking_boundaries):
        drift = True
    if drift: failures.append(dict(reason="Executable drift"))
    report = dict(cases=len(cases), exact=sum(row["exact"] for row in records), failures=failures, records=records, managed=args.managed,
                  seconds=round(time.monotonic()-start, 3), executableHashes=hashes, executableDrift=drift, fixtureHashes=fixture_hashes,
                  runnerSha256=digest(Path(__file__)), semanticObserverSha256=digest(ROOT / "tools/resolution-cli-oracle/main.go"),
                  currentCheckingBoundaries=None if args.current_checking_boundaries is None else str(args.current_checking_boundaries),
                  currentCheckingBoundariesSha256=boundary_hash,
                  currentSemanticPromotions=[] if promoted_semantics is None else sorted(promoted_semantics),
                  projectionLimits="Public CLI diagnostics compared completely. Graph DTO fields/payloads compared, original static trace type names are not serialized; explicit package-directory descriptor separately asserted. Pointer topology and host operations remain covered by the full graph protocol, not the CLI.")
    (args.output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in ("cases", "exact", "failures", "seconds")}))
    return bool(failures)


if __name__ == "__main__":
    raise SystemExit(main())
