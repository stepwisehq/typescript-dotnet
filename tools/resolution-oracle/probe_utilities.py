"""Finite authored upstream utility probes; outputs are investigation evidence."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "artifacts/resolution-validation/utility-source-probes"
OUTPUT.mkdir(parents=True, exist_ok=True)
ROWS = []


def add(name, queries, options=None, files=None):
    config = {
        "compilerOptions": {"module": "esnext", "moduleResolution": "bundler"} | (options or {}),
        "files": ["src/main.ts"],
    }
    ROWS.append({
        "Name": name, "Cwd": "/repo", "ConfigPath": "/repo/tsconfig.json",
        "ConfigText": json.dumps(config, separators=(",", ":")),
        "Files": files or [{"Path": "/repo/src/main.ts", "Text": ""}], "Queries": queries,
    })


def query(name, containing, **extra):
    return {"Kind": "utility", "Name": name, "Containing": containing} | extra


extensions = [".ts", ".d.ts", ".mts", ".d.mts", ".cts", ".d.cts", ".tsx", ".jsx", ".js", ".mjs", ".cjs", ".json", ".svg"]
for label, options in [
    ("defaults", {}), ("strict", {"strict": True}), ("jsx-preserve", {"jsx": "preserve"}),
    ("allowJs", {"allowJs": True}), ("resolveJsonModule-false", {"resolveJsonModule": False}),
    ("arbitrary", {"allowArbitraryExtensions": True}),
]:
    add("utility-extension-" + label, [
        query(operation, "/repo/src/main.ts", Extension=extension, ExtraExtension=False)
        if operation == "resolution-diagnostic" else query(operation, "/repo/src/input" + extension)
        for extension in extensions
        for operation in ("resolution-diagnostic", "js-extension", "js-extension-typed")
    ], options)
add("utility-extra-and-declaration", [
    query("resolution-diagnostic", file, Extension=extension, ExtraExtension=extra)
    for file in ("/repo/src/main.ts", "/repo/src/main.d.ts")
    for extension in extensions for extra in (False, True)
], {"strict": True, "resolveJsonModule": False})
add("utility-package-spellings", [
    query(operation, name)
    for name in ("", "pkg", "pkg/sub", "@scope/pkg", "@scope/pkg/sub", "@scope", "a__b", "@types/scope__pkg", "@types/pkg", "é/子", "~ts-uri-escape~pkg.js")
    for operation in ("parse-package-name", "mangle-package-name", "unmangle-package-name", "types-package-name", "package-from-types")
])
add("utility-version-and-pattern", [
    query("versioned-types-key", name)
    for name in ("types@*", "types@>=6", "types@<1", "types@bad", "types", "types@", "types@6.0.0-dev", "types@>=5.0.0")
] + [query("compare-patterns", a, Other=b) for a, b in [("x", "x"), ("*", "x"), ("x*", "x"), ("x*", "x*y"), ("x*y", "x*"), ("x/a*", "x/*")]])
for module, resolution in [("node16", "node16"), ("nodenext", "nodenext"), ("esnext", "bundler")]:
    for mode in (0, 1, 99):
        add("conditions-" + resolution + "-" + str(mode), [query("conditions", "", Mode=mode)],
            {"module": module, "moduleResolution": resolution, "customConditions": ["custom", "types"]})
files = [
    {"Path": "/repo/src/main.ts", "Text": ""},
    {"Path": "/repo/node_modules/@types/a/package.json", "Text": '{"name":"@types/a","types":"index.d.ts"}'},
    {"Path": "/repo/node_modules/@types/a/index.d.ts", "Text": "export {};"},
    {"Path": "/repo/node_modules/@types/empty/package.json", "Text": '{"name":"@types/empty","typings":null}'},
    {"Path": "/other/@types/a/package.json", "Text": "{}"},
    {"Path": "/other/@types/b/package.json", "Text": "{}"},
]
for name, options in [
    ("default", {}), ("duplicate-roots", {"typeRoots": ["/repo/node_modules/@types", "/other/@types"]}),
    ("explicit-empty", {"types": []}), ("explicit-values", {"types": ["a", "a", "b"]}),
]:
    add("automatic-types-" + name, [query("automatic-type-names", "")], options, files)
data = b"".join((json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n").encode() for row in ROWS)
(OUTPUT / "requests.jsonl").write_bytes(data)
binary = ROOT / "tools/.cache/resolution-oracle"
process = subprocess.run([str(binary)], input=data, capture_output=True, check=False)
(OUTPUT / "original.jsonl").write_bytes(process.stdout)
(OUTPUT / "original.stderr").write_bytes(process.stderr)
report = {
    "exitCode": process.returncode, "originalOnly": True, "requests": len(ROWS),
    "queries": sum(len(row["Queries"]) for row in ROWS), "responses": len(process.stdout.splitlines()),
    "requestsSha256": hashlib.sha256(data).hexdigest(), "outputSha256": hashlib.sha256(process.stdout).hexdigest(),
    "oracleSha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
}
(OUTPUT / "report.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report))
raise SystemExit(process.returncode)
