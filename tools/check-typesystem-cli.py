#!/usr/bin/env python3
"""Compare the two unchanged disk typed-event-client programs with genuine Go Program diagnostics."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("exact_program_comparison", ROOT / "tools/compare-resolution.py")
compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compare)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--original", type=Path, required=True)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--managed", action="store_true")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    block = compare.parse(args.manifest.read_bytes())["cliControls"]
    paths = {name: ROOT / name for name in block["sourceFiles"]}
    if any(sha(path) != block["sourceFiles"][name] for name, path in paths.items()):
        raise ValueError("Typed event client source/config bytes differ")
    if sha(ROOT / block["requests"]) != block["requestsSha256"] or sha(ROOT / block["provenance"]) != block["provenanceSha256"]:
        raise ValueError("Typed event client request/provenance bytes differ")
    rows = [compare.parse(line) for line in (ROOT / block["requests"]).read_bytes().splitlines()]
    if len(rows) != block["cases"] or {row["Name"] for row in rows} != {"typed-event-client-valid", "typed-event-client-invalid"}:
        raise ValueError("Typed event client CLI registry differs")
    for row in rows:
        if set(row) != {"Name", "ConfigPath", "Files"} or row["Files"] != {str(path): path.read_text() for path in paths.values()}:
            raise ValueError("Typed event client request does not preserve exact disk filenames/text")
        if row["ConfigPath"] != str(ROOT / "examples/typesystem" / ("event-client" if row["Name"] == "typed-event-client-valid" else "event-client-invalid") / "tsconfig.json"):
            raise ValueError("Typed event client config association differs")
    args.output.mkdir(parents=True, exist_ok=True)
    original, target = args.original.resolve(), args.executable.resolve()
    before = {"original": sha(original), "target": sha(target)}
    request_bytes = (ROOT / block["requests"]).read_bytes()
    started = time.monotonic()
    source = subprocess.run([str(original)], input=request_bytes, capture_output=True, cwd=ROOT, timeout=120)
    (args.output / "original.stdout").write_bytes(source.stdout)
    (args.output / "original.stderr").write_bytes(source.stderr)
    if source.returncode:
        raise ValueError("Genuine Go typed-event-client Program failed")
    expected = [compare.parse(line) for line in source.stdout.splitlines()]
    if len(expected) != len(rows):
        raise ValueError("Genuine Go typed-event-client result count differs")
    records, failures = [], []
    for row, reply in zip(rows, expected):
        if reply["Name"] != row["Name"] or reply["Result"]["ExitCode"] != block["expectedExits"][row["Name"]]:
            raise ValueError("Genuine Go typed-event-client identity/positive-negative result differs")
        command = (["dotnet"] if args.managed else []) + [str(target), "--config", row["ConfigPath"], "--json"]
        result = subprocess.run(command, capture_output=True, cwd=ROOT, timeout=120)
        stem = args.output / row["Name"]
        stem.with_suffix(".stdout").write_bytes(result.stdout)
        stem.with_suffix(".stderr").write_bytes(result.stderr)
        try:
            difference = compare.difference(reply["Result"], compare.parse(result.stdout))
        except (ValueError, UnicodeError) as error:
            difference = {"invalidResponse": str(error)}
        passed = result.returncode == reply["Result"]["ExitCode"] and difference is None
        if not passed:
            failures.append({"name": row["Name"], "difference": difference, "exitCode": result.returncode})
        records.append({"name": row["Name"], "command": command, "exitCode": result.returncode,
                        "expectedExitCode": reply["Result"]["ExitCode"], "passed": passed,
                        "difference": difference, "requestSha256": hashlib.sha256(json.dumps(row, separators=(",", ":"), ensure_ascii=False).encode()).hexdigest(),
                        "stdoutSha256": sha(stem.with_suffix(".stdout")), "stderrSha256": sha(stem.with_suffix(".stderr")),
                        "originalDiagnostics": reply["Result"]["Diagnostics"]})
    after = {"original": sha(original), "target": sha(target)}
    drift = before != after or any(sha(path) != block["sourceFiles"][name] for name, path in paths.items())
    if drift:
        failures.append({"reason": "Typed event client executable/fixture drift"})
    report = {"cases": len(rows), "exact": sum(row["passed"] for row in records), "failures": failures,
              "managed": args.managed, "seconds": round(time.monotonic() - started, 3),
              "requestsSha256": block["requestsSha256"], "manifestSha256": sha(args.manifest),
              "runnerSha256": sha(Path(__file__)), "originalCommand": [str(original)], "originalExitCode": source.returncode,
              "originalStdoutSha256": sha(args.output / "original.stdout"), "originalStderrSha256": sha(args.output / "original.stderr"),
              "executableHashesBefore": before, "executableHashesAfter": after, "executableDrift": drift,
              "sourceFiles": block["sourceFiles"], "records": records}
    (args.output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in ("cases", "exact", "failures", "seconds")}))
    return int(bool(failures))


if __name__ == "__main__":
    raise SystemExit(main())
