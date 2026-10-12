#!/usr/bin/env python3
"""Extract the actual pinned Go custom codec methods, without generated schema guesses."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_generated.go'
text = SOURCE.read_text()


def block_end(start):
    depth = 0
    quote = None
    escaped = False
    i = start
    while i < len(text):
        c = text[i]
        if quote:
            if quote == '`':
                if c == '`':
                    quote = None
            elif escaped:
                escaped = False
            elif c == '\\':
                escaped = True
            elif c == quote:
                quote = None
        elif text.startswith('//', i):
            i = text.find('\n', i)
            if i < 0:
                raise ValueError('unterminated Go comment')
            continue
        elif text.startswith('/*', i):
            i = text.find('*/', i + 2)
            if i < 0:
                raise ValueError('unterminated Go block comment')
            i += 2
            continue
        elif c in '\"\'`':
            quote = c
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return i
        i += 1
    raise ValueError('unterminated Go method')


methods = []
for match in re.finditer(r'^func \((\w+) \*(\w+)\) (UnmarshalJSONFrom|MarshalJSONTo)\([^\n]+\) error \{', text, re.M):
    receiver, type_name, method = match.groups()
    start = text.index('{', match.start())
    body = text[start + 1:block_end(start)]
    kind = 'custom'
    if method == 'UnmarshalJSONFrom':
        if re.fullmatch(r'\s*return unmarshalStruct\(\w+, dec\)\s*', body):
            kind = 'plain'
        elif 'scanDiscriminatedStruct(' in body:
            kind = 'discriminator'
    elif re.fullmatch(r'\s*return marshalUnion\(\w+, enc, "\w+", (true|false)\)\s*', body):
        kind = 'marshal-union'
    methods.append(dict(receiver=receiver, type=type_name, method=method,
                        line=text.count('\n', 0, match.start()) + 1,
                        kind=kind, body=body))
if len(methods) != 605:
    raise ValueError(f'pinned custom-method inventory changed: {len(methods)}')
path = Path(__file__).parent / 'codec-methods.json'
path.write_text(json.dumps(methods, indent=2) + '\n')
(Path(__file__).parent / 'codec-source-identity.json').write_text(json.dumps({
    'source': str(SOURCE.relative_to(ROOT)),
    'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    'pointerCustomMethods': len(methods),
    'valueLiteralWriters': 19,
}, indent=2) + '\n')
