#!/usr/bin/env python3
"""Assemble a strict collections manifest from existing independent expectations.

Does not run a compiler, generate/rewrite expectations, or normalize observations.
Pruning is opt-in, limited to specified untracked exploratory expectation assets.
"""
import argparse
import gzip
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
CORPUS = ROOT / "validation/collections-corpus"
MANIFEST = CORPUS / "manifest.json"
PRUNE_NAMES = {"bootstrap-expected.jsonl.gz", "vertical-expected.jsonl.gz", "vertical-requests.jsonl"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def corpus_path(value):
    path = Path(value)
    if not path.is_absolute():
        path = ROOT / path
    # Assets are direct corpus children; reject symlinks and path escape.
    if path.is_symlink() or path.resolve().parent != CORPUS.resolve() or not path.is_file():
        raise ValueError("Expected an existing direct corpus asset: " + value)
    return path.resolve()


def relative(path):
    return str(path.relative_to(ROOT))


def json_rows(path, compressed=False):
    opener = gzip.open if compressed else open
    with opener(path, "rt", encoding="utf-8") as stream:
        for index, line in enumerate(stream, 1):
            if not line.strip():
                raise ValueError(f"Blank NDJSON row: {path.name}:{index}")
            row = json.loads(line)
            if not isinstance(row, dict):
                raise ValueError(f"Expected JSON object: {path.name}:{index}")
            yield row


def request_rows(path):
    rows = list(json_rows(path))
    names = [row.get("name") for row in rows]
    if not rows or any(not isinstance(name, str) or not name for name in names) or len(set(names)) != len(names):
        raise ValueError("Empty or duplicate request names: " + path.name)
    for row in rows:
        if row.get("profile") != "Collections" or row.get("fileName", "/main.ts") != "/main.ts" or type(row.get("storedObservations", False)) is not bool:
            raise ValueError("Unexpected profile, file or observation mode: " + row["name"])
    return rows


def group(name, requests, expected):
    if not re.fullmatch(r"[A-Za-z0-9_-]+", name):
        raise ValueError("Unsafe group name: " + name)
    requests, expected = corpus_path(requests), corpus_path(expected)
    if requests.suffix != ".jsonl" or not expected.name.endswith(".jsonl.gz"):
        raise ValueError("Require NDJSON requests and compressed NDJSON expectations")
    with expected.open("rb") as stream:
        header = stream.read(10)
    if len(header) != 10 or header[:3] != b"\x1f\x8b\x08" or header[4:8] != b"\0\0\0\0":
        raise ValueError("Expected deterministic gzip with mtime=0: " + expected.name)
    rows = request_rows(requests)
    modes = {row.get("storedObservations", False) for row in rows}
    if len(modes) != 1:
        raise ValueError("Mixed observation modes in one group: " + name)
    mode = modes.pop()
    count = 0
    for output in json_rows(expected, compressed=True):
        count += 1
        if count > len(rows):
            raise ValueError("Extra expected row: " + name)
        if len(output.get("files", [])) != 4 or bool(output.get("storedGraph")) != mode:
            raise ValueError("Expected four-file result and matching stored graph mode: " + rows[count - 1]["name"])
    if count != len(rows):
        raise ValueError("Missing expected rows: " + name)
    return {"group": name, "requests": relative(requests), "expected": relative(expected),
            "cases": count, "requestsSha256": digest(requests), "expectedSha256": digest(expected),
            "storedObservations": mode}


def assemble(args):
    groups = [group(*values) for values in args.group]
    if len({row["group"] for row in groups}) != len(groups):
        raise ValueError("Duplicate group name")
    total = sum(row["cases"] for row in groups)
    shared = {"CollectionsControlsTests": 4, "CollectionsSupportTests": 8}
    if args.shared_class:
        shared = {}
        for name, value in args.shared_class:
            if name in shared or not re.fullmatch(r"[A-Za-z][A-Za-z0-9_]*", name):
                raise ValueError("Duplicate or invalid shared xUnit class")
            shared[name] = int(value)
    if not shared or any(value <= 0 for value in shared.values()) or "CollectionsOracleTests" in shared:
        raise ValueError("Invalid shared/corpus xUnit registry")
    order = corpus_path(args.order_requests)
    parity = corpus_path(args.parity_requests)
    order_rows, parity_rows = request_rows(order), request_rows(parity)
    support = corpus_path("validation/collections-corpus/support-projections-actual-go.json")
    provenance = corpus_path("validation/collections-corpus/support-projections-provenance.json")
    support_groups = json.loads(support.read_text())
    if not isinstance(support_groups, dict) or len(support_groups) != 7 or shared.get("CollectionsSupportTests") != 8:
        raise ValueError("Original support projection/shared-body inventory differs")
    canonical, raw_canonical = {}, {}
    for entry in groups:
        path = ROOT / entry["requests"]
        for row, raw in zip(request_rows(path), path.read_bytes().splitlines(), strict=True):
            if row["name"] in canonical:
                raise ValueError("Duplicate canonical request name: " + row["name"])
            canonical[row["name"]] = row
            raw_canonical[row["name"]] = raw
    for row in order_rows + parity_rows:
        if row["name"] not in canonical or row != canonical[row["name"]]:
            raise ValueError("Order/parity request differs from canonical input: " + row["name"])
    if set(row["name"] for row in parity_rows) != set(canonical):
        raise ValueError("Parity input must cover every canonical request")
    for row, raw in zip(order_rows, order.read_bytes().splitlines(), strict=True):
        if raw != raw_canonical[row["name"]]:
            raise ValueError("Order request bytes differ from canonical input: " + row["name"])
    executions = args.order_executions
    expected_executions = 2 * (len(order_rows) + 1)
    if args.order_sequences != 2 or executions is not None and executions != expected_executions:
        raise ValueError("Order inventory must be two complete sequences with one trailing repeat each")
    executions = expected_executions
    manifest = {
        "schemaVersion": 1, "baseline": "cfa6d2f",
        "upstreamRevision": json.loads((ROOT / "upstream.lock.json").read_text())["revision"],
        "groups": groups, "cases": total,
        "sharedChecks": {"count": sum(shared.values()), "xUnitClassCounts": shared},
        "corpusXUnitClassCounts": {"CollectionsOracleTests": total},
        "orderControls": {"requests": relative(order), "requestsSha256": digest(order),
                          "cases": len(order_rows), "sequencesPerRuntime": args.order_sequences,
                          "executionsPerRuntime": executions},
        "instrumentationParity": {"requests": relative(parity), "requestsSha256": digest(parity),
                                  "cases": len(parity_rows)},
        "supportProjections": {"expected": relative(support), "provenance": relative(provenance),
                               "expectedSha256": digest(support), "provenanceSha256": digest(provenance),
                               "groups": 7, "sharedCases": 8},
        "provenance": "Manifest assembly only; independently produced expected bytes are hashed and never regenerated or normalized.",
    }
    return manifest


def validate(manifest):
    spec = importlib.util.spec_from_file_location("collection_gate_preflight", ROOT / "tools/run-collections-gate.py")
    gate = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate)
    original_read = gate.read
    gate.read = lambda path: manifest if path == gate.MANIFEST else original_read(path)
    # Reuse fail-closed demo bytes/diagnostics, count, provenance and group checks
    # without writing an invalid candidate manifest or running the gate.
    gate.manifest_groups()
    gate.validate_libraries()


def prune_candidates(args, manifest):
    keep = {MANIFEST, CORPUS / "support-projections-actual-go.json", CORPUS / "support-projections-provenance.json"}
    for row in manifest["groups"]:
        keep.update(ROOT / row[key] for key in ("requests", "expected"))
    keep.update(ROOT / manifest[key]["requests"] for key in ("orderControls", "instrumentationParity"))
    tracked = set(subprocess.check_output(["git", "ls-files", "-z", "--", "validation/collections-corpus"], cwd=ROOT).decode().split("\0"))
    paths = []
    for value in args.prune:
        path = corpus_path(value)
        if path in paths or path in keep or relative(path) in tracked or not (path.name in PRUNE_NAMES or path.name.endswith("-program-expected.jsonl.gz")):
            raise ValueError("Refusing selected, tracked, duplicate or nonexploratory prune asset: " + value)
        paths.append(path)
    return paths


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--group", nargs=3, action="append", required=True, metavar=("NAME", "REQUESTS", "EXPECTED"))
    parser.add_argument("--order-requests", required=True)
    parser.add_argument("--parity-requests", required=True)
    parser.add_argument("--order-sequences", type=int, default=2)
    parser.add_argument("--order-executions", type=int)
    parser.add_argument("--shared-class", nargs=2, action="append", metavar=("NAME", "COUNT"))
    parser.add_argument("--prune", action="append", default=[], metavar="EXPLORATORY_ASSET")
    parser.add_argument("--check-only", action="store_true", help="Validate and report without writing the manifest or pruning")
    args = parser.parse_args()
    manifest = assemble(args)
    validate(manifest)
    paths = prune_candidates(args, manifest)
    if not args.check_only:
        payload = json.dumps(manifest, indent=2) + "\n"
        temporary = MANIFEST.with_suffix(".json.tmp")
        temporary.write_text(payload)
        temporary.replace(MANIFEST)
        archive = ROOT / "artifacts/collections-validation/exploratory-corpus"
        for path in paths:
            archive.mkdir(parents=True, exist_ok=True)
            retained = archive / (digest(path) + "-" + path.name)
            if retained.exists() and digest(retained) != digest(path):
                raise ValueError("Existing exploratory retention differs: " + str(retained))
            if not retained.exists():
                shutil.copyfile(path, retained)
            if digest(retained) != digest(path):
                raise ValueError("Exploratory retention copy differs: " + str(retained))
            path.unlink()
    print(json.dumps({"cases": manifest["cases"], "groups": len(manifest["groups"]),
                      "sharedChecks": manifest["sharedChecks"]["count"], "checkOnly": args.check_only,
                      "pruned" if not args.check_only else "wouldPrune": [relative(path) for path in paths]}, indent=2))


if __name__ == "__main__":
    main()
