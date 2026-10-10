#!/usr/bin/env python3
"""Authored first semantic requests using exact disk demo bytes; no expected data."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "validation/typesystem-corpus"
sources = sorted(path for path in (ROOT / "examples/typesystem").rglob("*")
                 if path.suffix in (".ts", ".json"))
files = [{"Path": "/" + path.relative_to(ROOT).as_posix(), "Text": path.read_bytes().decode("utf-8")}
         for path in sources]
requests = []
for name, directory in (("typed-event-client-valid", "event-client"), ("typed-event-client-invalid", "event-client-invalid")):
    requests.append({"Name": name, "ConfigPath": f"/examples/typesystem/{directory}/tsconfig.json",
                     "Files": files, "Bind": True, "SingleThreaded": True,
                     "Semantic": {"PerFile": True, "Repeat": True}})
data = "".join(json.dumps(row, separators=(",", ":"), ensure_ascii=False) + "\n" for row in requests).encode()
provenance = {
    "baseline": "09630d6", "upstream": "fed0bf24149fb1ed36039212648bafdafc1ea10e",
    "classification": "authored-genuine-Program-semantic-inputs-not-original-test-bodies",
    "cases": len(requests), "requestsSha256": hashlib.sha256(data).hexdigest(),
    "sourceFiles": {path.relative_to(ROOT).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest() for path in sources},
    "optionsAndLibraries": "Actual tsconfig parsing and Bundled.WrapFS; strict ES2022 demos. No option or library substitution.",
    "observation": "Full retained Program records plus actual global-before/bulk/per-file/global-after/repeat semantic channels; no expected data derived from managed output."
}
rendered = (json.dumps(provenance, indent=2) + "\n").encode()
if "--check" in sys.argv:
    assert (OUT / "requests-vertical.jsonl").read_bytes() == data
    assert (OUT / "requests-vertical-provenance.json").read_bytes() == rendered
else:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "requests-vertical.jsonl").write_bytes(data)
    (OUT / "requests-vertical-provenance.json").write_bytes(rendered)
print(json.dumps({"cases": len(requests), "requestsSha256": provenance["requestsSha256"]}))
