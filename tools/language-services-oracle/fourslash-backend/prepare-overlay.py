#!/usr/bin/env python3
"""Prepare additive candidate backend overlay; preserve original assertions/baselines."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'upstream/TypeScript/tsc'
DRAFT = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--js-only', action='store_true', help='Admit unchanged JS feature tests into the genuine Go reference without candidate host rewrites.')
args = parser.parse_args()
OUT = ROOT / 'artifacts/language-services' / ('fourslash-js-reference-overlay' if args.js_only else 'fourslash-backend-overlay')
OUT.mkdir(parents=True, exist_ok=True)
replace = {}

def rewrite(relative, substitutions):
    source = SOURCE / relative
    text = source.read_text()
    for old, new in substitutions:
        if text.count(old) != 1:
            raise RuntimeError(f'{relative}: expected exactly one admission anchor {old!r}')
        text = text.replace(old, new)
    destination = OUT / Path(relative).name
    destination.write_text(text)
    replace[str(source)] = str(destination)

if not args.js_only:
    rewrite('internal/testutil/lsptestutil/lspclient.go', [
        ('"io"', '"io"\n "os"'),
        ('Server       *lsp.Server', 'Server       *lsp.Server\n candidate *candidateBackend'),
        ('transport := newClientTransport()', 'if os.Getenv("TSDOTNET_FOURSLASH_SERVER") != "" { return newCandidateLSPClient(t, serverOpts, onServerRequest) }\n transport := newClientTransport()'),
        ('ctx := t.Context()', 'ctx := t.Context()\n if c.candidate != nil { ctx = c.ctx }'),
        ('t.Fatalf("Request cancelled: %v", ctx.Err())', 'if c.candidate != nil { t.Fatalf("Candidate transport ended before response: %v", context.Cause(ctx)) }; t.Fatalf("Request cancelled: %v", ctx.Err())'),
        ('c.Server.SetCompilerOptionsForInferredProjects(c.ctx, options)', 'if c.candidate != nil { c.candidateSetOptions(options); return }; c.Server.SetCompilerOptionsForInferredProjects(c.ctx, options)'),
    ])
    rewrite('internal/fourslash/fourslash.go', [
        ('<-f.client.Server.InitComplete()', 'f.client.WaitInitComplete()'),
    ])
    rewrite('internal/fourslash/statebaseline.go', [
        ('session := f.client.Server.Session()', 'if f.client.CandidateBackend() {\n files := make([]string,0,len(f.openFiles)); for name := range f.openFiles { files=append(files,string(name)) };\n io.WriteString(w,f.client.CandidateStateDiff(f.stateBaseline.isInitialized,files)); return\n }\n session := f.client.Server.Session()'),
    ])
    for name in ('control.go', 'filesystem.go', 'process.go', 'client.go'):
        replace[str(SOURCE / 'internal/testutil/lsptestutil' / ('candidate_' + name))] = str(DRAFT / name)
# Go interprets a _js_test.go suffix as GOOS=js. These are JS-language
# feature assertions, so admit their unchanged bodies under a neutral test name.
js_sources = sorted((SOURCE / 'internal/fourslash/tests').glob('*_js_test.go'))
if len(js_sources) != 13:
    raise RuntimeError(f'Expected 13 pinned JS filename admissions, found {len(js_sources)}')
for source in js_sources:
    admitted = source.with_name(source.name.removesuffix('_js_test.go') + '_js_case_test.go')
    if admitted.exists():
        raise RuntimeError(f'Admission collides with an original source: {admitted}')
    replace[str(admitted)] = str(source)
(OUT / 'overlay.json').write_text(json.dumps({'Replace': replace}, indent=2) + '\n')
