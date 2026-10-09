"""Finite authored public raw-JSON/mapper metadata witnesses; no expected generation."""
from pathlib import Path
import base64
import json

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "validation/configuration-corpus/raw-values-requests.jsonl"
b64 = lambda value: base64.b64encode(value).decode()
raw = lambda value: {"kind": "json-value", "bytesBase64": b64(value)}
number = {"kind": "number", "number": "1"}
fragments = {
    "object-whitespace": b' \n { "b" : [1, 2], "a" : true } \t',
    "number-spelling": b' 1.0 ',
    "escaped-string": b' "a\\/b\\u00e9" ',
    "escaped-surrogate": b' "\\ud800" ',
    "invalid-utf8": b'"a\xffb"',
    "raw-wtf8": b'"\xed\xa0\x80"',
    "duplicate-name": b'{"a":1,"a":2}',
    "malformed-number": b'0.',
    "malformed-object": b' { , } ',
    "trailing-value": b'0 1',
    "control-byte": b'"\x01"',
    "empty": b'',
}
rows = [{"name": "raw-value-" + name, "mode": "marshal", "value": raw(value)} for name, value in fragments.items()]
rows.append({"name": "raw-value-nil", "mode": "marshal", "value": {"kind": "json-value", "null": True}})
for name in ("malformed-number", "malformed-object"):
    rows.append({"name": "raw-list-" + name, "mode": "marshal", "value": {"kind": "list", "items": [number, raw(fragments[name])]}})
    rows.append({"name": "raw-ordered-map-" + name, "mode": "marshal", "value": {"kind": "ordered-map", "entries": [{"keyBase64": b64(b"a"), "value": number}, {"keyBase64": b64(b"x"), "value": raw(fragments[name])}]}})
for name in ("malformed-number", "malformed-object"):
    rows.append({"name": "raw-map-single-" + name, "mode": "marshal", "value": {"kind": "map", "entries": [{"keyBase64": b64(b"x"), "value": raw(fragments[name])}]}})
for name in ("object-whitespace", "empty", "malformed-object", "duplicate-name"):
    rows.append({"name": "mapper-options-" + name, "mode": "marshal", "constructor": "definition-raw", "value": raw(fragments[name])})
rows.append({"name": "mapper-options-nil", "mode": "marshal", "constructor": "definition-raw", "value": {"kind": "json-value", "null": True}})
for name, constructor, depth in (("root-limit", "json-value-depth", 10000), ("root-over-limit", "json-value-depth", 10001), ("nested-limit", "list-json-value-depth", 9999), ("nested-over-limit", "list-json-value-depth", 10000)):
    rows.append({"name": "raw-depth-" + name, "mode": "marshal", "constructor": constructor, "depth": depth})
callbacks = [b'{"name":{,}}', b'{"typescript":{,"contentMapper":null}}', b'{"typescript":{"contentMapper":{,}}}', b'{"name":0.}', b'{"dependencies":{,}}', b'{"name":0.', b'{"typescript":{"contentMapper":[{"name":{,}}]}}']
rows.append({"name": "package-raw-callback-grammar", "mode": "package-json", "argumentsBase64": [b64(value) for value in callbacks]})
for name, value in (("null", b"null"), ("empty-string", b'""'), ("empty-array", b"[]"), ("empty-object", b"{}"), ("false", b"false"), ("zero", b"0")):
    rows.append({"name": "mapper-options-" + name, "mode": "marshal", "constructor": "definition-raw", "value": raw(value)})
nil = {"kind": "json-value", "null": True}
empty = raw(b"")
for name, left, right in (("nil-nil", nil, nil), ("absent-wrapped-nil", {"kind": "null"}, nil), ("nil-empty", nil, empty), ("empty-empty", empty, empty), ("empty-null", empty, raw(b"null")), ("same-bytes", raw(b"{}"), raw(b"{}")), ("different-spelling", raw(b"{}"), raw(b" {} "))):
    rows.append({"name": "mapper-equality-" + name, "mode": "mapper-equality", "value": {"kind": "list", "items": [left, right]}})
for constructor in ("tsconfig-nil", "tsconfig-empty", "tsconfig-false", "acquisition-nil", "acquisition-empty", "build-nil", "build-zero", "compiler-nil", "compiler-empty", "compiler-zero", "reference-false", "reference-original"):
    rows.append({"name": "static-model-" + constructor, "mode": "marshal", "constructor": "static-" + constructor})
OUTPUT.write_text("".join(json.dumps(row, separators=(",", ":")) + "\n" for row in rows))
print(len(rows))
