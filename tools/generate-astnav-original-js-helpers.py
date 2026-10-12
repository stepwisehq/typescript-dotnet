#!/usr/bin/env python3
"""Retain the exact original AstNav JavaScript helper literals as test-only C# constants."""
import argparse, hashlib, json, re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'upstream/TypeScript/tsc/internal/astnav/tokens_test.go'
DEFAULT = ROOT/'validation/LanguageServices.Checks/AstNavOriginalChecks.Scripts.cs'
HELPERS = ('tsGetTokensAtPositions', 'tsGetTouchingPropertyName', 'tsFindPrecedingTokens')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=DEFAULT)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    source = SOURCE.read_text()
    result = '// Exact JavaScript helper literals from pinned internal/astnav/tokens_test.go.\nnamespace LanguageServices.Checks;\npublic static partial class AstNavOriginalChecks\n{\n'
    for index, helper in enumerate(HELPERS):
        body = source[source.index('func '+helper+'('):]
        script = re.search(r'script := `([\s\S]*?)`', body).group(1)
        result += f'    private const string Script{index} = '+json.dumps(script, ensure_ascii=True)+';\n'
    result += '}\n'
    if args.check:
        if args.output.read_text() != result: raise RuntimeError('Original helper transport differs')
    else:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(result)
    print(json.dumps({'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(), 'originalScripts':3, 'execution':'not performed'}))

if __name__ == '__main__': main()
