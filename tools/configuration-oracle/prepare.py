"""Install the standalone configuration oracle without changing upstream algorithms."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "tools/configuration-oracle"
OUTPUT = ROOT / "upstream/TypeScript/tsc/cmd/configuration-oracle"
OUTPUT.mkdir(parents=True, exist_ok=True)
for path in SOURCE.glob("*.go"):
    (OUTPUT / path.name).write_bytes(path.read_bytes())

# One consolidated observer reads exact stored private specs/cache payloads.
helper = ROOT / "upstream/TypeScript/tsc/internal/tsoptions/configuration_observe.go"
helper.write_bytes((SOURCE / "configuration_access.go.inc").read_bytes())
