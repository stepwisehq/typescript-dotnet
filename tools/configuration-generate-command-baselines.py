"""Embed exact pinned baseline bytes; never invoke managed/original producers."""
from pathlib import Path
import argparse
import base64
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
NAMES = ['watchInterval', 'watchFile', 'watchDirectory', 'fallbackPolling',
         'synchronousWatchDirectory', 'excludeDirectories', 'excludeFiles']
BASE = ROOT / 'upstream/TypeScript/tsc/testdata/baselines/reference/tsoptions/removedWatchOptions'
TEST = ROOT / 'upstream/TypeScript/tsc/internal/tsoptions/commandlineparser_test.go'
EXPECTED_TEST_SHA = '982216b41350c5e73fd6541c379c8f84486c43509d82a1c4bec161de44a45f7a'

def generate():
    assert hashlib.sha256(TEST.read_bytes()).hexdigest() == EXPECTED_TEST_SHA
    rows = []
    lines = ['// Exact original baseline bytes; generated metadata only, never managed output.',
             'namespace Configuration.Checks;', 'public static partial class CommandLineChecks',
             '{', '    private static readonly (string Name, string Base64)[] removedWatchBaselines =', '    [']
    for name in NAMES:
        file = BASE / (name + '.js')
        data = file.read_bytes()
        lines.append(f'        ("{name}", "{base64.b64encode(data).decode()}"),')
        rows.append({'source': str(file.relative_to(ROOT)), 'sha256': hashlib.sha256(data).hexdigest()})
    lines += ['    ];', '}']
    provenance = {'upstreamRevision': PIN, 'originalTest': str(TEST.relative_to(ROOT)),
                  'originalTestSha256': EXPECTED_TEST_SHA, 'baselines': rows,
                  'policy': 'All seven original baseline files retained byte-for-byte; no generated expectations.'}
    return {'CommandLineBaselines.Generated.cs': '\n'.join(lines) + '\n',
            'command-line-baseline-provenance.json': json.dumps(provenance, indent=2) + '\n'}

parser = argparse.ArgumentParser()
parser.add_argument('--check', action='store_true')
args = parser.parse_args()
for name, text in generate().items():
    file = ROOT / 'validation/Configuration.Checks' / name
    if args.check:
        assert file.read_text() == text, name
    else:
        file.write_text(text)
print('Seven original baseline byte streams checked' if args.check else 'Seven original baseline byte streams embedded')
