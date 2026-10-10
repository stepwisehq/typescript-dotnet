#!/usr/bin/env python3
"""Extract only original TestProgram literal tables; no target-derived expectations."""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "upstream/TypeScript/tsc/internal/compiler/program_test.go"
OUTPUT = ROOT / "validation/Program.Checks/ProgramOrderTables.Generated.cs"


def generate():
    source = SOURCE.read_text()
    libraries = re.findall(r'"([^"\n]+)"', source.split("var esnextLibs = []string{", 1)[1].split("\n}", 1)[0])
    table = source.split("var programTestCases = []programTest{", 1)[1].split("func TestProgram(", 1)[0]
    rows = []
    for part in table.split("testName:")[1:]:
        name = re.match(r'\s*("[^"]+")', part)[1]
        files = [(json.loads(path), json.loads(text)) for path, text in re.findall(r'fileName:\s*("(?:[^"\\]|\\.)*"), contents:\s*("(?:[^"\\]|\\.)*")', part)]
        expected = re.findall(r'"([^"\n]+)"', part.split("expectedFiles:", 1)[1].split("target:", 1)[0])
        if len(files) != 11 or len(expected) != 11 or "core.ScriptTargetESNext" not in part:
            raise ValueError("Changed original ordering table domain")
        rows.append((name, files, expected))
    if [json.loads(row[0]) for row in rows] != ["BasicFileOrdering", "FileOrderingImports", "FileOrderingCycles"] or len(libraries) != 99:
        raise ValueError("Changed original TestProgram domain")
    result = ["// Source-only literal extraction; all original assertions are in ProgramOrderChecks.cs.",
              "// Source SHA256 " + hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
              "namespace Program.Checks;", "internal static class ProgramOrderTables", "{",
              "    internal sealed record Row(string Name, (string Path, string Text)[] Files, string[] Expected);",
              "    internal static readonly string[] Libraries =", "    ["]
    result += ["        " + json.dumps(item) + "," for item in libraries]
    result += ["    ];", "    internal static readonly Row[] Rows =", "    ["]
    for name, files, expected in rows:
        result += ["        new(" + name + ",", "        ["]
        result += ["            (" + json.dumps(path) + ", " + json.dumps(text) + ")," for path, text in files]
        result += ["        ],", "        ["]
        result += ["            " + json.dumps(path) + "," for path in expected]
        result += ["        ]),"]
    return "\n".join(result + ["    ];", "}", ""])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    output = generate()
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text() != output:
            raise SystemExit("Original Program order tables are stale")
    else:
        OUTPUT.write_text(output)


if __name__ == "__main__":
    main()
