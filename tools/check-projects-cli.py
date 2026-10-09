#!/usr/bin/env python3
"""Exercise the ordinary project CLI; no Go/upstream/runtime fixture dependency."""
import argparse
import base64
import gzip
import hashlib
import json
import pathlib
import subprocess
import tempfile
import time
import unicodedata

ROOT = pathlib.Path(__file__).resolve().parent.parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def diagnostic(value):
    required = {"File", "Code", "Category", "Message", "ByteStart", "ByteEnd", "Line", "Column", "Chain", "Related"}
    if set(value) != required or type(value["Code"]) is not int or not value["Message"]:
        raise RuntimeError("Incomplete ordinary CLI diagnostic")
    for key in ("ByteStart", "ByteEnd", "Line", "Column"):
        if type(value[key]) is not int:
            raise RuntimeError("Invalid CLI diagnostic location")
    if value["File"] is not None and value["ByteStart"] >= 0:
        if value["ByteEnd"] < value["ByteStart"] or value["Line"] < 1 or value["Column"] < 1:
            raise RuntimeError("Invalid CLI byte/display range")
    for key in ("Chain", "Related"):
        if value[key] is not None:
            if not isinstance(value[key], list):
                raise RuntimeError("Invalid CLI diagnostic chain")
            for child in value[key]:
                diagnostic(child)


def demo_expectations(manifest_path):
    manifest = json.loads(manifest_path.read_text())
    demos = {row["name"]: row for row in manifest["demonstrations"]}
    found = {}
    for group in manifest["groups"]:
        rows = (ROOT / group["requests"]).read_text().splitlines()
        opener = gzip.open if group["expected"].endswith(".gz") else open
        with opener(ROOT / group["expected"], "rt") as stream:
            for line in rows:
                request, expected = json.loads(line), json.loads(stream.readline())
                if request["name"] in demos:
                    found[pathlib.PurePosixPath(demos[request["name"]]["directory"]).name] = (request, expected)
            if stream.readline():
                raise RuntimeError("Extra canonical expected rows")
    if len(found) != len(demos):
        raise RuntimeError("Missing canonical ordinary CLI demonstration")
    return found


def relocate_type_imports(text, paths):
    """Only rendered import-type tokens outside quoted literal content relocate."""
    def quoted_end(start):
        quote, end = text[start], start + 1
        while end < len(text):
            if text[end] == "\\":
                end += 2
            elif text[end] == quote:
                return end + 1
            else:
                end += 1
        raise RuntimeError("Unterminated rendered type literal")

    output, index = [], 0
    while index < len(text):
        if text[index] in "\"'`":
            end = quoted_end(index)
            output.append(text[index:end])
            index = end
            continue
        previous = text[index - 1] if index else ""
        identifier = previous and (previous.isalnum() or previous in "_$.\u200c\u200d" or unicodedata.category(previous) in ("Mn", "Mc", "Pc"))
        boundary = not identifier
        if boundary and text.startswith('import("', index):
            start = index + len("import(")
            end = quoted_end(start)
            if end < len(text) and text[end] == ")":
                module = decode_module_path(text[start:end])
                if module in paths:
                    output.append("import(" + quote_module_path(paths[module]) + ")")
                    index = end + 1
                    continue
        output.append(text[index])
        index += 1
    return "".join(output)


def quote_module_path(text):
    # Selected printer.utilities.go EscapeString with a double quote and
    # NeverAsciiEscape: JS escape spellings, not generic JSON quoting.
    special = {8: "\\b", 9: "\\t", 10: "\\n", 11: "\\v", 12: "\\f", 13: "\\r",
               34: '\\"', 92: "\\\\", 0x85: "\\u0085", 0x2028: "\\u2028", 0x2029: "\\u2029"}
    result = ['"']
    for index, char in enumerate(text):
        code = ord(char)
        if code == 0:
            result.append("\\x00" if index + 1 < len(text) and text[index + 1] in "0123456789" else "\\0")
        elif code in special:
            result.append(special[code])
        elif code <= 0x1F or 0xD800 <= code <= 0xDFFF:
            result.append("\\u" + format(code, "04X"))
        else:
            result.append(char)
    result.append('"')
    return "".join(result)


def decode_module_path(literal):
    # The finite double-quoted JS escape output of the selected printer.
    text, index = literal[1:-1], 0
    result = []
    escaped = {"b": "\b", "t": "\t", "n": "\n", "v": "\v", "f": "\f", "r": "\r", '"': '"', "\\": "\\", "0": "\0"}
    while index < len(text):
        char = text[index]
        index += 1
        if char != "\\":
            result.append(char)
            continue
        if index == len(text):
            raise RuntimeError("Unterminated rendered module escape")
        escape = text[index]
        index += 1
        if escape in escaped:
            result.append(escaped[escape])
        elif escape in ("u", "x"):
            count = 4 if escape == "u" else 2
            digits = text[index:index + count]
            if len(digits) != count or any(c not in "0123456789abcdefABCDEF" for c in digits):
                raise RuntimeError("Invalid rendered module hex escape")
            result.append(chr(int(digits, 16)))
            index += count
        else:
            raise RuntimeError("Unknown rendered module escape")
    # Original UTF16 \u surrogate-pair output denotes one Unicode scalar path.
    return "".join(result).encode("utf-16-le", "surrogatepass").decode("utf-16-le")


def lexical_controls():
    # Independent fixed expectations: literal/prefix text must survive unchanged.
    cases = [
        ('import("/project/a").A', 'import("/actual/a").A'),
        ('"import(\\"/project/a\\")"', '"import(\\"/project/a\\")"'),
        ("'import(\"/project/a\")'", "'import(\"/project/a\")'"),
        ('myimport("/project/a")', 'myimport("/project/a")'),
        ('typeof import("/project/a") | "import(\\"/project/a\\")"', 'typeof import("/actual/a") | "import(\\"/project/a\\")"'),
        ('a\u0301import("/project/a")', 'a\u0301import("/project/a")'),
        ('a\u203fimport("/project/a")', 'a\u203fimport("/project/a")'),
        ('a\u200cimport("/project/a")', 'a\u200cimport("/project/a")'),
        ('a\u200dimport("/project/a")', 'a\u200dimport("/project/a")'),
        ('import("/project/a\\u2028b").T', 'import("/actual/a\\u2028b").T'),
        ('"import(\\"/project/a\\u2028b\\")"', '"import(\\"/project/a\\u2028b\\")"'),
        ('import("/project/a\\vb").T', 'import("/actual/a\\vb").T'),
    ]
    for source, expected in cases:
        if relocate_type_imports(source, {"/project/a": "/actual/a", "/project/a\u2028b": "/actual/a\u2028b", "/project/a\vb": "/actual/a\vb"}) != expected:
            raise RuntimeError("CLI rendered-type lexical relocation control failed")
    scalar_cases = [('"\\ud83d\\ude00"', "😀"), ('"\\x1F"', "\x1f")]
    for source, expected in scalar_cases:
        if decode_module_path(source) != expected:
            raise RuntimeError("CLI rendered module scalar decode control failed")
    return len(cases) + len(scalar_cases)


def cli_diagnostics(request, expected, directory):
    owners = {row["index"]: row for row in expected["files"]}

    def file_path(row):
        name = row["fileName"]
        if name == "/project/tsconfig.json":
            return str(directory / "tscheck.json")
        if name.startswith("/project/"):
            return str(directory / name[len("/project/"):])
        if name.startswith("/lib/"):
            return name
        raise RuntimeError("Unknown canonical CLI file owner")

    def display_position(row, pos):
        if row is None or pos < 0:
            return 0, 0
        name = row["fileName"]
        if name == "/project/tsconfig.json":
            text = request["config"]
        elif name.startswith("/project/"):
            text = request["files"][name[len("/project/"):]]
        else:
            raise RuntimeError("Unexpected library diagnostic in shipped demonstration")
        prefix = text.encode("utf-8")[:pos].decode("utf-8")
        lines = prefix.replace("\r\n", "\n").replace("\r", "\n").replace("\u2028", "\n").replace("\u2029", "\n").split("\n")
        return len(lines), len(lines[-1].encode("utf-16-le")) // 2 + 1

    def describe(value):
        owner = owners[value["fileIndex"]] if value["fileIndex"] is not None else None
        line, column = display_position(owner, value["pos"])
        message = value["text"]
        # Exactly reverse the rich protocol's reviewed filename-role projection.
        roles = {6053: [0], 2306: [0], 1149: [0, 1], 1261: [0, 1], 1409: [1], 1393: [1]}
        args = [base64.b64decode(arg, validate=True).decode("utf-8") for arg in value["argsBase64"] or []]
        for index in roles.get(value["code"], []):
            if index < len(args) and args[index].startswith("/project/"):
                argument = args[index]
                message = message.replace(argument, str(directory / argument[len("/project/"):]))
        if value["code"] == 1192 and args:
            for row in owners.values():
                if row["externalModule"] and row["fileName"].startswith("/project/"):
                    projected = '"' + row["fileName"].removesuffix(".ts") + '"'
                    if args[0] == projected:
                        message = message.replace(projected, '"' + file_path(row).removesuffix(".ts") + '"')
        if value["code"] == 2757:
            if not args or message != "Type '" + args[0] + "' has no call signatures.":
                raise RuntimeError("Unknown pinned type-display diagnostic template")
            paths = {row["fileName"].removesuffix(".ts"): file_path(row).removesuffix(".ts")
                     for row in owners.values() if row["externalModule"] and row["fileName"].startswith("/project/")}
            message = "Type '" + relocate_type_imports(args[0], paths) + "' has no call signatures."
        return {"File": file_path(owner) if owner is not None else None, "Code": value["code"],
                "Category": ["Warning", "Error", "Suggestion", "Message"][value["category"]],
                "Message": message, "ByteStart": value["pos"], "ByteEnd": value["end"], "Line": line, "Column": column,
                "Chain": None if value["chain"] is None else [describe(v) for v in value["chain"]],
                "Related": None if value["related"] is None else [describe(v) for v in value["related"]]}

    # Complete multiset across Program channels; recursive child order remains exact.
    values = list(expected["globalDiagnostics"] or []) + list(expected["programDiagnostics"] or []) + list(expected["configDiagnostics"] or [])
    for row in expected["files"]:
        values.extend(row["parseDiagnostics"] or [])
        # semanticDiagnostics already composes the same file's bind/check channels.
        if row["semanticDiagnostics"] is not None:
            values.extend(row["semanticDiagnostics"])
        else:
            values.extend(row["bindDiagnostics"] or [])
    return [describe(v) for v in values]


def run(executable, config, expected, output, name, managed=False):
    completed = subprocess.run((["dotnet"] if managed else []) + [str(executable), "--project", str(config), "--json"],
                               cwd=ROOT, text=True, capture_output=True)
    (output / (name + ".stdout.json")).write_text(completed.stdout)
    (output / (name + ".stderr.txt")).write_text(completed.stderr)
    value = json.loads(completed.stdout)
    if set(value) != {"ExitCode", "Error", "Diagnostics"} or type(value["ExitCode"]) is not int:
        raise RuntimeError("Incomplete ordinary CLI response: " + name)
    if completed.returncode != expected or value["ExitCode"] != expected:
        raise RuntimeError("Wrong ordinary CLI status: " + name)
    if not isinstance(value["Diagnostics"], list):
        raise RuntimeError("Invalid CLI diagnostic list")
    for item in value["Diagnostics"]:
        diagnostic(item)
    if expected == 0 and (value["Error"] is not None or value["Diagnostics"]):
        raise RuntimeError("Successful CLI returned diagnostics: " + name)
    if expected == 1 and (value["Error"] is not None or not value["Diagnostics"]):
        raise RuntimeError("Compiler failure lacks diagnostics: " + name)
    if expected == 2 and (not value["Error"] or value["Diagnostics"]):
        raise RuntimeError("Unsupported CLI response lacks a host/config error: " + name)
    return {"name": name, "exitCode": expected, "diagnosticCodes": [d["Code"] for d in value["Diagnostics"]],
            "stdoutSha256": digest(output / (name + ".stdout.json"))}


def check(executable, output, managed=False, manifest=None):
    output.mkdir(parents=True, exist_ok=True)
    start = time.monotonic()
    before = digest(executable)
    results = []
    lexical = lexical_controls()
    canonical = demo_expectations(manifest) if manifest is not None else None
    # Shipped examples run directly from disk, independently of validation protocol.
    for name, expected in (("grouped-records", 0), ("grouped-records-invalid", 1)):
        results.append(run(executable, ROOT / "examples/projects" / name / "tscheck.json",
                           expected, output, name, managed))
        if canonical is not None:
            request, reference = canonical[name]
            want = cli_diagnostics(request, reference, ROOT / "examples/projects" / name)
            got = json.loads((output / (name + ".stdout.json")).read_text())["Diagnostics"]
            key = lambda value: json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True)
            if sorted(map(key, want)) != sorted(map(key, got)):
                (output / (name + ".canonical-cli-diagnostics.json")).write_text(json.dumps(want, indent=2) + "\n")
                raise RuntimeError("Ordinary CLI diagnostics differ from canonical original: " + name)
    with tempfile.TemporaryDirectory(prefix="tscheck-cli-") as scratch:
        directory = pathlib.Path(scratch)
        cases = [
            ("missing-root", '{"files":["missing.ts"]}', None, 1),
            ("malformed-source", '{"files":["main.ts"]}', "const broken = ;", 1),
            ("unknown-config-option", '{"files":["main.ts"],"compilerOptions":{"unknown":true}}', "const x=1;", 2),
            ("malformed-unicode-config", '{"files":["\\ud800.ts"]}', None, 2),
            ("outside-root", '{"files":["../outside.ts"]}', None, 2),
            ("excluded-class", '{"files":["main.ts"]}', "class Excluded {}", 2),
        ]
        for name, config, source, expected in cases:
            case = directory / name
            case.mkdir()
            (case / "tscheck.json").write_text(config)
            if source is not None:
                (case / "main.ts").write_text(source)
            results.append(run(executable, case / "tscheck.json", expected, output, name, managed))
        # Preserved non-scalar module bytes must never alias replacement-character
        # disk names. This host boundary rejects them explicitly, without changing
        # the scanner's literal bytes or claiming original diagnostic equivalence.
        case = directory / "non-scalar-module-path"
        case.mkdir()
        (case / "tscheck.json").write_text('{"files":["main.ts"]}')
        (case / "main.ts").write_text('import {x} from "./\\uD800"; const y:number=x;')
        for name in ("\ufffd.ts", "\ufffd\ufffd\ufffd.ts"):
            (case / name).write_text("export const x=1;")
        results.append(run(executable, case / "tscheck.json", 2, output, "non-scalar-module-path", managed))
        # This genuine diagnostic checks byte ranges and UTF16 display positions.
        case = directory / "unicode-location"
        case.mkdir()
        source = 'const label="é😀"; const wrong:number="bad";'
        (case / "main.ts").write_text(source)
        (case / "tscheck.json").write_text('{"files":["main.ts"]}')
        row = run(executable, case / "tscheck.json", 1, output, "unicode-location", managed)
        value = json.loads((output / "unicode-location.stdout.json").read_text())
        matching = [d for d in value["Diagnostics"] if d["Code"] == 2322]
        prefix = source[:source.index("wrong")]
        if len(matching) != 1 or matching[0]["ByteStart"] != len(prefix.encode("utf-8")) or matching[0]["Line"] != 1 or matching[0]["Column"] != len(prefix.encode("utf-16-le")) // 2 + 1:
            raise RuntimeError("CLI Unicode byte/UTF16 location differs")
        results.append(row)
    after = digest(executable)
    if before != after:
        raise RuntimeError("Ordinary CLI executable changed during checking")
    report = {"cases": len(results), "exact": len(results), "failures": [], "managed": managed, "canonicalDemoDiagnostics": canonical is not None,
              "manifestSha256": None if manifest is None else digest(manifest), "lexicalProjectionControls": lexical,
              "executableSha256": before, "results": results,
              "elapsedSeconds": round(time.monotonic() - start, 3)}
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    parser.add_argument("--managed", action="store_true")
    parser.add_argument("--manifest", type=pathlib.Path)
    args = parser.parse_args()
    print(json.dumps(check(args.executable.resolve(), args.output.resolve(), args.managed, args.manifest.resolve() if args.manifest else None)))


if __name__ == "__main__":
    main()
