# Upstream reference

This feasibility port uses the Go TypeScript compiler from
[microsoft/TypeScript](https://github.com/microsoft/TypeScript), pinned at
[`fed0bf24149fb1ed36039212648bafdafc1ea10e`](https://github.com/microsoft/TypeScript/tree/fed0bf24149fb1ed36039212648bafdafc1ea10e/tsc).
This was the latest maintained `main` revision selected on 2026-10-08.
The machine-readable record is [upstream.lock.json](./upstream.lock.json).

The Go module is `github.com/microsoft/TypeScript/tsc` and requires Go 1.27.
The local checkout is `upstream/TypeScript`. It is ignored by this repository;
build instructions recreate the pinned checkout. The archived
`microsoft/typescript-go` staging repository is not used for implementation or
verification.

Source mapping and coverage conventions are described in
[SOURCE-MAPPING.md](./docs/SOURCE-MAPPING.md). Ported files preserve upstream
source boundaries where practical; omitted compiler-dependent code is explicit.
New compatibility support, tests, and harnesses are separately identified.

The upstream compiler is licensed under Apache License 2.0. The unmodified
[license](./licenses/TypeScript-LICENSE.txt) and
[notice file](./licenses/TypeScript-NOTICE.txt) are included. Translated files are
modified derivatives, not original Microsoft C# sources. Unicode data attribution
from the upstream notice applies to copied identifier/property tables. Preserving
the complete upstream notice does not imply that all its listed components are
implemented or included in this scanner port.

Regular-expression diagnostic spelling suggestions also require a small slice of
Go 1.27.1's `src/unicode/letter.go` and `src/unicode/tables.go` to reproduce observed
case-folding behavior. That separate source pin and archive checksum are recorded
in the lock file, with per-file hashes in the mapping. The
[Go BSD license](./licenses/Go-LICENSE.txt) is preserved. The narrow table extraction
is reproducible with [generate-unicode-case.py](./tools/generate-unicode-case.py).
