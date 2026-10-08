#!/usr/bin/env python3
"""Hash the parser executable/test snapshot; preserve the scanner hash algorithm."""
import hashlib
import pathlib

root = pathlib.Path(__file__).resolve().parent.parent
files = sorted(
    p for base in ["src", "tests", "validation/Scanner.Validation", "validation/Parser.Validation"]
    for p in (root / base).rglob("*")
    if p.is_file() and p.suffix in [".cs", ".csproj"]
    and not ({"bin", "obj"} & set(p.relative_to(root).parts))
)
digest = hashlib.sha256()
for path in files:
    digest.update(path.relative_to(root).as_posix().encode())
    digest.update(b"\0")
    digest.update(path.read_bytes())
    digest.update(b"\0")
print(digest.hexdigest())
