#!/usr/bin/env python3
"""Compare existing resolution expectations under fresh reversed/repeated orders.

JSON object key order is immaterial; scalar types, all fields, array order and
null versus empty remain exact. Neither requests nor expectations are rewritten.
"""
import argparse
import gzip
import hashlib
import json
import pathlib
import subprocess
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def asset(path):
    result = pathlib.Path(path)
    return result if result.is_absolute() else ROOT / result


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def nonfinite(value):
    raise ValueError("Nonfinite JSON number: " + value)


def parse(raw):
    return json.loads(raw, object_pairs_hook=unique_object, parse_constant=nonfinite)


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"), allow_nan=False)


def load_rows(path):
    raw = gzip.decompress(path.read_bytes()) if path.suffix == ".gz" else path.read_bytes()
    lines = raw.splitlines()
    if not lines or any(not line for line in lines):
        raise ValueError("Empty request/expected row: " + str(path))
    return lines


def load_inputs(manifest_path):
    manifest = parse(manifest_path.read_bytes())
    order = manifest["orderControls"]
    count = order["cases"]
    if type(count) is not int or count <= 0 or order["sequencesPerRuntime"] != 2 or order["executionsPerRuntime"] != 2 * (count + 1):
        raise ValueError("Order inventory must contain two complete repeated sequences")
    fingerprints = {str(manifest_path): sha(manifest_path)}
    rows = {}
    total = 0
    for group in manifest["groups"]:
        request_path, expected_path = asset(group["requests"]), asset(group["expected"])
        for path, field in ((request_path, "requestsSha256"), (expected_path, "expectedSha256")):
            digest = sha(path)
            if digest != group[field]:
                raise ValueError("Canonical corpus digest mismatch: " + str(path))
            fingerprints[str(path)] = digest
        requests, expected = load_rows(request_path), load_rows(expected_path)
        if type(group["cases"]) is not int or len(requests) != group["cases"] or len(expected) != group["cases"]:
            raise ValueError("Canonical corpus count mismatch")
        for request, want in zip(requests, expected):
            name = parse(request)["Name"]
            if not isinstance(name, str) or not name or name in rows:
                raise ValueError("Missing or duplicate canonical request name")
            rows[name] = (request, canonical(parse(want)))
        total += len(requests)
    if total != manifest["cases"]:
        raise ValueError("Canonical manifest total mismatch")
    order_path = asset(order["requests"])
    digest = sha(order_path)
    if digest != order["requestsSha256"]:
        raise ValueError("Order request digest mismatch")
    fingerprints[str(order_path)] = digest
    names = []
    for request in load_rows(order_path):
        name = parse(request)["Name"]
        if name not in rows or request != rows[name][0] or name in names:
            raise ValueError("Order request differs from canonical corpus or repeats a name")
        names.append(name)
    if len(names) != count:
        raise ValueError("Order request count mismatch")
    return order, rows, names, fingerprints


def run_sequence(runtime, command, label, names, rows, output):
    stem = output / (runtime + "-" + label)
    failures = []
    exact = 0
    raw = b"\n".join(rows[name][0] for name in names) + b"\n"
    try:
        process = subprocess.run(command, input=raw, capture_output=True, cwd=ROOT, timeout=300)
        stdout, stderr, status = process.stdout, process.stderr, process.returncode
    except subprocess.TimeoutExpired as error:
        stdout, stderr, status = error.stdout or b"", error.stderr or b"", None
        failures.append({"runtime": runtime, "sequence": label, "exception": "Process timeout after 300 seconds"})
    stem.with_suffix(".jsonl").write_bytes(stdout)
    stem.with_suffix(".stderr").write_bytes(stderr)
    if status != 0:
        failures.append({"runtime": runtime, "sequence": label, "exitCode": status})
    lines = stdout.splitlines()
    if len(lines) != len(names):
        failures.append({"runtime": runtime, "sequence": label, "expectedRows": len(names), "actualRows": len(lines)})
    for index, name in enumerate(names):
        if index >= len(lines):
            continue
        try:
            matches = canonical(parse(lines[index])) == rows[name][1]
        except (ValueError, UnicodeDecodeError) as error:
            failures.append({"runtime": runtime, "sequence": label, "index": index, "name": name, "exception": str(error)})
            continue
        if matches:
            exact += 1
        else:
            failures.append({"runtime": runtime, "sequence": label, "index": index, "name": name, "difference": "Full canonical expectation mismatch"})
    return {
        "runtime": runtime, "sequence": label, "names": names, "exact": exact,
        "exitCode": status, "output": str(stem.with_suffix(".jsonl")),
        "outputSha256": hashlib.sha256(stdout).hexdigest(),
        "stderr": str(stem.with_suffix(".stderr")),
        "stderrSha256": hashlib.sha256(stderr).hexdigest(),
    }, failures


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--go", required=True)
    parser.add_argument("--target", required=True)
    parser.add_argument("--managed", action="store_true")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = pathlib.Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    started = time.monotonic()
    report = {"cases": 0, "sequencesPerRuntime": 2, "originalGoExact": 0, "targetExact": 0,
              "requestsSha256": None, "failures": [], "records": []}
    try:
        order, rows, names, assets = load_inputs(asset(args.manifest))
        report.update(cases=len(names), requestsSha256=order["requestsSha256"])
        binaries = {"originalGo": asset(args.go), "target": asset(args.target)}
        before = {name: sha(path) for name, path in binaries.items()}
        commands = {"originalGo": [str(binaries["originalGo"])],
                    "target": (["dotnet"] if args.managed else []) + [str(binaries["target"])]}
        report.update(commands=commands, binarySha256Before=before, assetSha256=assets)
        # Repeat after intervening resolution/graph environments, so the same work
        # detects process contamination as well as allocation-order changes.
        sequences = [("forward-repeat-first", names + [names[0]]),
                     ("reverse-repeat-last", list(reversed(names)) + [names[-1]])]
        for runtime, command in commands.items():
            for label, sequence in sequences:
                record, failures = run_sequence(runtime, command, label, sequence, rows, output)
                report["records"].append(record)
                report["failures"].extend(failures)
                report[runtime + "Exact"] += record["exact"]
        after = {name: sha(path) for name, path in binaries.items()}
        report["binarySha256After"] = after
        if before != after:
            report["failures"].append({"exception": "Runtime binary changed during order checks"})
        if any(sha(pathlib.Path(path)) != digest for path, digest in assets.items()):
            report["failures"].append({"exception": "Canonical/order corpus changed during order checks"})
        for runtime in commands:
            if report[runtime + "Exact"] != order["executionsPerRuntime"]:
                report["failures"].append({"runtime": runtime, "exception": "Incomplete exact execution inventory"})
    except (OSError, ValueError, KeyError, TypeError, EOFError) as error:
        report["failures"].append({"exception": str(error)})
    report["elapsedSeconds"] = round(time.monotonic() - started, 3)
    report["comparison"] = "Each original Go and target reply matches unchanged canonical expectation; exact scalar types, fields, arrays and null/empty. Two fresh repeated forward/reverse processes per runtime."
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in ("cases", "sequencesPerRuntime", "originalGoExact", "targetExact", "requestsSha256", "failures", "elapsedSeconds")}))
    return int(bool(report["failures"]))


if __name__ == "__main__":
    raise SystemExit(main())
