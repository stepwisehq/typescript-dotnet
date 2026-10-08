#!/usr/bin/env python3
"""Hash executable sources for the checker and every earlier regression adapter."""
import hashlib,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
files=sorted(p for base in ['src','tests','validation/Scanner.Validation','validation/Parser.Validation','validation/Binder.Validation','validation/Checker.Validation'] for p in (root/base).rglob('*') if p.is_file() and p.suffix in ['.cs','.csproj'] and not ({'bin','obj'} & set(p.relative_to(root).parts)))
digest=hashlib.sha256()
for path in files:
 digest.update(path.relative_to(root).as_posix().encode());digest.update(b'\0');digest.update(path.read_bytes());digest.update(b'\0')
print(digest.hexdigest())
