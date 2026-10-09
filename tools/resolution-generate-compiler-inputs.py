#!/usr/bin/env python3
"""Reuse pinned compiler fixture inputs; never generate expected observations.

The extraction below follows ParseTestFilesAndSymlinks, including its directive
removal and line join. This is input reuse, not the original compiler harness.
"""
from pathlib import Path
import argparse
import hashlib
import itertools
import json
import posixpath
import re

ROOT = Path(__file__).resolve().parents[1]
PIN = "fed0bf24149fb1ed36039212648bafdafc1ea10e"
CATALOG = ROOT / "docs/resolution-compiler-inputs.json"
DEST = ROOT / "validation/resolution-corpus/requests-compiler-inputs.jsonl"
PROVENANCE = DEST.with_name("requests-compiler-inputs-provenance.json")
HARNESS = "upstream/TypeScript/tsc/internal/testrunner/"
SOURCES = [HARNESS + "test_case_parser.go", HARNESS + "compiler_runner.go",
           HARNESS + "options_generated.go",
           "upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go"]
DIRECTIVE = re.compile(r"^//\s*@(\w+)\s*:\s*([^\r\n]*)")
LINK = re.compile(r"^//\s*@link\s*:\s*([^\r\n]*)\s*->\s*([^\r\n]*)")
REMOVED = {"classic", "node", "node10"}
HARNESS_ONLY = {"notypesandsymbols", "noimplicitreferences", "fullemitpaths",
                "currentdirectory", "symlink", "link"}
LIST_OPTIONS = {"customconditions", "typeroots", "types", "lib", "modulesuffixes"}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def dumps(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":"))


def absolute(path, cwd):
    # These inventoried harness paths are POSIX. Reject additional platform
    # syntax rather than silently treating a drive/UNC fixture as a POSIX path.
    if "\\" in path or re.match(r"^[A-Za-z]:", path):
        raise ValueError("Unaccounted platform fixture path: " + path)
    return posixpath.normpath(path if path.startswith("/") else cwd.rstrip("/") + "/" + path)


def extract(data, source):
    """Exact selected regular-compiler parser domain, not fourslash implicit files."""
    text = data.decode("utf-8", errors="strict")
    units, links, options = [], {}, {}
    name, content, cwd = "", "", "/.src"
    for line in re.split(r"\r?\n", text):
        link = LINK.match(line)
        if link:
            links[link[2].strip()] = link[1].strip()
            continue
        match = DIRECTIVE.match(line)
        if match:
            key, value = match[1].lower(), match[2].strip()
            if key == "filename":
                if name:
                    units.append((name, content))
                elif re.sub(r"/\*.*?\*/|//[^\n]*", "", content, flags=re.S).strip():
                    raise ValueError("Content before first Filename: " + source)
                name, content = value, ""
            else:
                if key == "currentdirectory":
                    cwd = absolute(value, "/.src")
                if key == "symlink" and name:
                    for link_name in value.split(","):
                        if link_name.strip():
                            links[link_name.strip()] = name
                else:
                    options[key] = value
            continue
        # Original parser inserts LF only if builder.Len()!=0: initial blank
        # lines disappear; trailing blank lines after content are preserved.
        content = content + ("\n" if content else "") + line
    if not name and not units:
        name = Path(source).name
    units.append((name, content))
    resolved = [(absolute(name, cwd), content) for name, content in units]
    if len({name for name, _ in resolved}) != len(resolved):
        raise ValueError("Duplicate virtual file path: " + source)
    return resolved, {absolute(k, cwd): absolute(v, cwd) for k, v in links.items()}, options, cwd


def variations(options, varying):
    keys, choices = [], []
    for key in sorted(options):
        values = [v.strip() for v in options[key].split(",") if v.strip()] if key in varying else [options[key]]
        # Only the literal variant syntax used by this inventory is selected.
        if key in varying and any(v == "*" or v.startswith(("!", "-")) for v in values):
            raise ValueError("Unaccounted harness wildcard variant for " + key)
        values = list(dict.fromkeys(values))
        keys.append(key)
        choices.append(values or [""])
    return [dict(zip(keys, values)) for values in itertools.product(*choices)]


def option_value(key, value, cwd):
    if key in LIST_OPTIONS:
        result = [v.strip() for v in value.split(",") if v.strip()]
        if key == "typeroots":
            result = [absolute(v, cwd) for v in result]
        return result
    if value.lower() in {"true", "false"}:
        return value.lower() == "true"
    if re.fullmatch(r"[0-9]+", value):
        return int(value)
    if key in {"baseurl", "outdir", "rootdir"}:
        return absolute(value, cwd)
    return value


def generate(catalog):
    source_bytes = {s: (ROOT / s).read_bytes() for s in SOURCES}
    varying = set(re.findall(r'"([a-z]+)"', source_bytes[SOURCES[2]].decode()))
    # Original declaration spelling is authoritative for authored JSON keys.
    declarations = (ROOT / "upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go").read_text()
    names = {name.lower(): name for name in re.findall(r'Name:\s*"([^"]+)"', declarations)}
    source_bytes["upstream/TypeScript/tsc/internal/tsoptions/declarations_generated.go"] = declarations.encode()
    requests, records, updated = [], [], []
    for index, original in enumerate(catalog["inputs"]):
        entry = dict(original)
        source = entry["source"]
        data = (ROOT / source).read_bytes()
        if sha(data) != entry["sourceSha256"]:
            raise ValueError("Pinned input digest changed: " + source)
        units, symlinks, options, cwd = extract(data, source)
        files = [{"Path": name, "Text": text} for name, text in units]
        for name, target in symlinks.items():
            if any(file["Path"] == name for file in files):
                raise ValueError("Symlink collides with source fixture: " + name)
            files.append({"Path": name, "Symlink": target})
        configs = [(name, text) for name, text in units if posixpath.basename(name).lower() in {"tsconfig.json", "jsconfig.json"}]
        variants = variations(options, varying)
        names_for_input, exclusions = [], []
        unknown = set(options) - HARNESS_ONLY - set(names)
        if unknown:
            raise ValueError("Unaccounted fixture directives: " + repr(unknown))
        # Embedded config supplies its own effective options/discovery. Preserve
        # it, and record the compiler harness's later directive overlay as an
        # explicit omitted harness operation instead of inventing merged state.
        if configs:
            config_path, config_text = configs[0]
            variants = [options]
        for variant_index, variant in enumerate(variants):
            removed = variant.get("moduleresolution", "").lower()
            if removed in REMOVED:
                exclusions.append({"variant": variant, "reason": "Explicit removed resolution mode; not rewritten to a supported mode."})
                continue
            request_name = f"compiler-input-{index + 1:03d}-{variant_index + 1:02d}-{Path(source).stem}"
            request_files = list(files)
            if not configs:
                values = {"newLine": "crlf", "skipDefaultLibCheck": True, "noErrorTruncation": True}
                values.update({names[key]: option_value(key, value, cwd) for key, value in variant.items() if key not in HARNESS_ONLY})
                last_text = units[-1][1]
                last_only = bool(variant.get("noimplicitreferences", "")) or "require(" in last_text or re.search(r"reference\spath", last_text) is not None
                roots = units[-1:] if last_only else units
                roots = [name for name, _ in roots if not name.endswith((".json", ".tsbuildinfo"))]
                config_path = absolute("__resolution_compiler_input__.json", cwd)
                if any(file["Path"] == config_path for file in files):
                    raise ValueError("Authored config collides with fixture: " + source)
                config_text = dumps({"compilerOptions": values, "files": roots})
                request_files.append({"Path": config_path, "Text": config_text})
            request = {"Name": request_name, "Cwd": cwd, "ConfigPath": config_path,
                       "ConfigText": config_text, "Files": request_files, "Queries": [], "Graph": True}
            requests.append(request)
            names_for_input.append(request_name)
            records.append({"name": request_name, "source": source, "sourceSha256": sha(data),
                            "variant": variant, "config": "original-first-embedded" if configs else "authored-directive-config",
                            "virtualFiles": [{"path": name, "contentSha256": sha(text.encode())} for name, text in units],
                            "symlinks": symlinks, "rootSelection": "original-config-discovery" if configs else "original-last-only-or-all-root-heuristic",
                            "requestSha256": sha((dumps(request) + "\n").encode())})
        entry["disposition"] = "reused-upstream-input-graph-differential" if names_for_input else "excluded-removed-mode-only-fixture"
        entry["requestNames"] = names_for_input
        entry["excludedVariants"] = exclusions
        entry["adaptations"] = {
            "extraction": "Original regular compiler harness removes directive lines, splits CRLF/LF and joins content with LF; original whole fixture SHA retained.",
            "environment": "Case-sensitive virtual filesystem, original currentDirectory or /.src, original symlink targets. No physical disk relocation.",
            "configuration": "Original first embedded tsconfig/jsconfig parsed unchanged; top-level compiler directive overrides and harness-only flags are retained as provenance but are not applied after discovery." if configs else "Authored JSON config encodes original retained directive variant and original root heuristic; original fixture source/package bytes are unchanged after harness extraction.",
            "unappliedCompilerDirectives": {key: value for key, value in options.items() if key not in HARNESS_ONLY} if configs else {},
            "harnessOnly": {key: value for key, value in options.items() if key in HARNESS_ONLY},
            "externalStages": "No checker/emit/baselines/global automatic types/default-library environment/project-reference builds; explicit lib references remain observed unscheduled metadata.",
        }
        entry["scope"] = "Original compiler inputs reused for selected genuine loader/resolver graph operations; original compiler assertions and Program validation are not claimed."
        # Runtime dispositions are owned evidence, not generator-derived claims.
        # Keep them on a deterministic --check/regeneration when input names match.
        entry["execution"] = original["execution"] if original.get("requestNames") == names_for_input else {"status": "inputs-generated-pinned-go-expectations-pending" if names_for_input else "excluded-not-executed"}
        updated.append(entry)
    if len(updated) != 76 or sum(not e["requestNames"] for e in updated) != 4:
        raise ValueError("Unexpected inventoried input/disposition count")
    if len({r["Name"] for r in requests}) != len(requests):
        raise ValueError("Duplicate generated request name")
    request_bytes = "".join(dumps(r) + "\n" for r in requests).encode()
    result = dict(catalog, inputs=updated)
    result["summary"] = {"fixtures": 76, "reusedFixtures": 72, "excludedFixtures": 4, "requests": len(requests), "originalCompilerAssertionsClaimed": 0}
    provenance = {"schemaVersion": 1, "upstream": PIN, "generator": "tools/resolution-generate-compiler-inputs.py",
                  "generatorSha256": sha(Path(__file__).read_bytes()), "sourceFiles": {s: sha(b) for s, b in source_bytes.items()},
                  "claim": "Input reuse only. Expected observations must be generated exclusively by the pinned original Go oracle.",
                  "requestsSha256": sha(request_bytes), "summary": result["summary"], "requests": records}
    return request_bytes, (json.dumps(provenance, indent=2) + "\n").encode(), (json.dumps(result, indent=2) + "\n").encode()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    catalog = json.loads(CATALOG.read_bytes())
    if catalog["upstream"] != PIN:
        raise SystemExit("Unexpected original pin")
    values = generate(catalog)
    for path, data in zip([DEST, PROVENANCE, CATALOG], values):
        if args.check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit("Generated input/provenance drift: " + str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(json.loads(values[1])["summary"])


if __name__ == "__main__":
    main()
