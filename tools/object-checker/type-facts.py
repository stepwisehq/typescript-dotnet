#!/usr/bin/env python3
"""Extract exact pinned checker.go TypeFacts enum to stdout; no source overwrite."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
source = (root / 'upstream/TypeScript/tsc/internal/checker/checker.go').read_text()
block = source.split('type TypeFacts uint32\n\nconst (\n', 1)[1].split('\n)', 1)[0]
print('    // checker.go:399. Exact TypeFacts constants.\n    [Flags]\n    private enum TypeFacts : uint\n    {')
count = 0
for line in block.splitlines():
    line = line.strip()
    if not line or line.startswith('//'):
        continue
    match = re.fullmatch(r'TypeFacts(\w+)\s+TypeFacts\s*=\s*(.+)', line)
    if not match:
        raise RuntimeError(f'Unrecognized constant: {line}')
    name, expression = match.groups()
    expression = re.sub(r'\bTypeFacts(\w+)\b', r'\1', expression).replace('^', '~')
    print(f'        {name} = {expression},')
    count += 1
if count != 76:
    raise RuntimeError(f'Unexpected enum count: {count}')
print('    }')
