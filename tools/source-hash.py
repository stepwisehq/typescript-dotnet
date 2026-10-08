#!/usr/bin/env python3
"""Hash the executable C# source/project snapshot, excluding generated build files."""
import hashlib,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
files=sorted(p for base in ['src','validation/Scanner.Validation','tests'] for p in (root/base).rglob('*') if p.is_file() and p.suffix in ['.cs','.csproj'] and not ({'bin','obj'}&set(p.relative_to(root).parts)))
h=hashlib.sha256()
for p in files:
 h.update(p.relative_to(root).as_posix().encode());h.update(b'\0');h.update(p.read_bytes());h.update(b'\0')
print(h.hexdigest())
