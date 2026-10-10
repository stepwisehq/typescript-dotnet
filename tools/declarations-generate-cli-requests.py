#!/usr/bin/env python3
"""Explicit current CLI replacements for obsolete authored checking profiles."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "validation/declarations-corpus"
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def generate():
    historical = ROOT / "validation/configuration-corpus/cli-requests.jsonl"
    rows = [json.loads(line) for line in historical.read_text().splitlines()]
    base = ROOT / "examples/configuration/checking-base.json"
    old_names = [row["name"] for row in rows]
    replacements = []
    for row in rows:
        if row["name"].startswith("checking-demo-"):
            previous = copy.deepcopy(row)
            for file in row["files"]:
                if file["path"].endswith("/configuration/checking-base.json"):
                    file["text"] = base.read_text()
            replacements.append({"previous": previous["name"], "current": row["name"], "reason": "Explicit source ES5 library option replaces hidden host library roots.", "previousRequestSha256": hashlib.sha256(json.dumps(previous, sort_keys=True).encode()).hexdigest()})
        elif row["name"] == "check-unsupported-settings":
            replacements.append({"previous": row["name"], "current": "check-declaration-only-source-environment", "reason": "Authored project-reference/profile rejection retired; actual reference construction is retained Program responsibility. Current control checks genuine declaration-only semantic acquisition."})
            row.clear()
            row.update(name="check-declaration-only-source-environment", mode="file", logicalRoot="/project", configPath="/project/tsconfig.json", cliAction="check", exitCode=0,
                       files=[{"path":"/project/tsconfig.json", "text":"{\"compilerOptions\":{\"noEmit\":true,\"lib\":[\"es5\"],\"types\":[],\"skipLibCheck\":true},\"files\":[\"ambient.d.ts\"]}"},
                              {"path":"/project/ambient.d.ts", "text":"declare interface Ambient { value: string; }\n"}])
    requests = "".join(json.dumps(row, ensure_ascii=True, separators=(",", ":")) + "\n" for row in rows).encode()
    original = [row for row in rows if "unsupportedError" not in row]
    assert len(rows) == 14 and len(original) == 13
    provenance = {"upstreamRevision":"fed0bf24149fb1ed36039212648bafdafc1ea10e", "baseline":"e7b81a2", "classification":"current-authored-cli-replacements-not-upstream-original-bodies", "cases":len(rows), "originalCases":len(original), "authoredBoundaryCases":1,
                  "historicalRequests":str(historical.relative_to(ROOT)), "historicalRequestsSha256":sha(historical), "historicalExpected":"validation/configuration-corpus/cli-expected.jsonl", "historicalExpectedSha256":sha(ROOT / "validation/configuration-corpus/cli-expected.jsonl"),
                  "sourceFiles":{str(base.relative_to(ROOT)):sha(base), str(Path(__file__).relative_to(ROOT)):sha(Path(__file__))}, "replacements":replacements,
                  "unchangedNames":[name for name in old_names if name not in {row["previous"] for row in replacements}],
                  "retainedBoundary":"Raw non-scalar Linux directory path rejection remains directly checked; no arbitrary diagnostic/path normalization.", "observation":"Complete source Program/options/libraries/diagnostics and exits. Config-only controls retain actual configuration parsing. Historical captured corpus/proofs remain immutable."}
    return {OUT / "cli-requests-current.jsonl":requests, OUT / "cli-requests-current-provenance.json":(json.dumps(provenance, indent=2)+"\n").encode()}
def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument("--check",action="store_true");a=ap.parse_args()
    for path, data in generate().items():
        if a.check:
            if not path.exists() or path.read_bytes()!=data: raise SystemExit("CLI generated asset differs: "+str(path))
        else: path.write_bytes(data)
if __name__ == "__main__": main()
