#!/usr/bin/env python3
"""Hash foundation and retained regression executable sources, including shared assertions."""
import hashlib
import pathlib

root = pathlib.Path(__file__).resolve().parent.parent
files = sorted(path for base in ['src', 'tests', 'validation'] for path in (root/base).rglob('*')
    if path.is_file() and path.suffix in ['.cs', '.csproj']
    and not ({'bin', 'obj'} & set(path.relative_to(root).parts)))
digest = hashlib.sha256()
for path in files:
    digest.update(path.relative_to(root).as_posix().encode())
    digest.update(b'\0')
    digest.update(path.read_bytes())
    digest.update(b'\0')
print(digest.hexdigest())
