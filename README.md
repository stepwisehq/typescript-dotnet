# TypeScript compiler and language services in C#

A C# port of the pinned Go TypeScript compiler, with project checking, ordinary
Program emit and a real language server. The agreed compiler and language-services
contracts are complete and independently verified under **Linux x64 Native AOT**.
This is a pinned-upstream implementation, not a claim of complete repository,
all-platform or every historical TypeScript feature parity.

The reference is Microsoft TypeScript revision
[`fed0bf24149fb1ed36039212648bafdafc1ea10e`](https://github.com/microsoft/TypeScript/tree/fed0bf24149fb1ed36039212648bafdafc1ea10e/tsc).
See the [upstream record](./UPSTREAM.md), [current state](./docs/STATE.md),
[compiler assessment](./docs/EMIT-ASSESSMENT.md) and
[language-services assessment](./docs/LANGUAGE-SERVICES-ASSESSMENT.md).

## What it does

- Checks configured, multi-file projects using genuine compiler options, module
  resolution, project references and standard libraries.
- Emits JavaScript, declarations and source maps through the ported upstream
  transformations, printer and output infrastructure.
- Maintains editor projects, snapshots, overlays, unsaved/incremental document
  changes, parse/config caches, checker leases, autoimports and file watches.
- Serves diagnostics, completion/resolve, hover, signature help, definitions,
  references, implementations, rename, symbols, formatting, organize imports,
  registered code actions, semantic tokens, inlay hints, selection/folding ranges,
  call hierarchy and code lenses through the upstream LSP surface.
- Handles real stdio framing, negotiated positions and capabilities, cancellation,
  client requests, progress, registration, shutdown, signals and parent monitoring.

All **115 pinned standard-library declaration files** are embedded unchanged.
The core compiler and LSP need neither a Go process nor Node. Go and Node participate
in reference generation and differential verification; optional automatic type
acquisition uses npm, and configured content-mapper hosts can require external tools.
The library and CLI target .NET 10; [global.json](./global.json) pins the build SDK.

## Build and use

From the repository root, use .NET SDK 10.0.401 selected by [global.json](./global.json)
and the Linux Native AOT native compiler/zlib development prerequisites (on Ubuntu,
`clang` and `zlib1g-dev`):

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true \
  -o artifacts/tscheck
```

Compile a genuine TypeScript configuration, or check without writing outputs:

```sh
artifacts/tscheck/tscheck --config path/to/tsconfig.json --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --noEmit --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --listFilesOnly
```

Compiler options determine JavaScript, declaration and map outputs. Success exits
0; compiler errors exit 1 when emit is skipped and 2 otherwise. `--listFilesOnly`
is a command flag, not a tsconfig option. Existing `--project <tscheck.json>` retains
its check-only compatibility behavior. Configuration and graph inspection are
available through `--inspect-config`, `--inspect-project` and `--inspect-program`.
See the [configuration](./docs/CONFIGURATION.md), [resolution](./docs/RESOLUTION.md)
and [Program](./docs/PROGRAM.md) guides.

Start the public language server:

```sh
artifacts/tscheck/tscheck --lsp --stdio
```

Configure an LSP-capable editor to launch that executable with `--lsp --stdio`,
using the project directory as its working directory. Stdout carries only protocol
frames; stderr is separate. The [editor connection guide](./docs/progress/lsp-public-launch-readiness.md)
includes the pinned VS Code client's actual settings and local-executable route.
The Native AOT executable does not require the .NET runtime installed on the server host.

The long-lived server bypasses the one-shot CLI no-GC policy. Optional one-shot
`--no-gc-region` requests 14 GiB total/2 GiB LOH for checking, or 16 GiB/2 GiB for
emit, after a Linux preflight of at least 20 GiB available memory. It falls back to
ordinary GC when unavailable; these budgets are not process memory limits.
AVX2, checker-count and worker tuning are separate explicit choices, described in
[the emit performance summary](./docs/EMIT-PERFORMANCE-SUMMARY.md).

## Verification and limitations

The compiler, direct checks, original editor corpus and actual public server were
executed under Linux x64 Native AOT. Independent reviews bind source, resources,
actual compiler inputs, binaries and complete observations. The counts below use
separate units and should not be added into one test total.

| Evidence | Accepted scope |
| --- | --- |
| Retained managed checks | 2,226 cases, including selected parser tests; the full 18,799-case parser suite is also verified separately |
| Native no-emit comparison | 15,304 exact public records against pinned Go |
| Emit inventory | 15,330 entries: 15,285 exact public outputs, 39 preparation skips and six known external metadata differences; 13,571 original assertion outcomes are a separate channel |
| Language-services direct checks | 406 whole original functions and 80 authored controls; adapted/external channels remain separate |
| Original editor corpus | 4,571 identities: 4,152 passes, 415 original skips and four same pinned-Go failures |
| Public disk/stdio | Complete paired responses through document/config/dependency changes, 100 queries and 100 unsaved edits; separate signal/watchdog and shutdown roles |

The [language-services inventory](./docs/language-services-upstream-tests.json)
accounts for all 5,006 original function identities, including the editor corpus,
optional Replay skip, adapted watcher observation, Go generator disposition and 26
excluded platform-backend tests. The four pinned-reference failures are not passes:
three missing-golden outputs match as separate adapted evidence; the fourth fails
its original diagnostic precondition before later reference assertions can run.
Original skips and fixture/channel adaptations remain visible.

Reproduction commands and complete dispositions are in
[language-services verification](./docs/LANGUAGE-SERVICES-VERIFY.md),
[emit verification](./docs/EMIT-VERIFY.md),
[emit test inventory](./docs/EMIT-TEST-INVENTORY.md) and
[the final LSP proof](./validation/results/language-services-verified.json).
The original fourslash adapter runs actual C# Server state with the genuine Go
assertions and authoritative fixture filesystem; it does not use a shadow Go Session.
Separate public tests use real disk and redirected stdio.

Important boundaries remain:

- Linux x64 is the qualified Native AOT platform. Windows/macOS/BSD native watcher
  backends are outside this contract; no all-platform acceptance is claimed.
- Command-line build/watch and incremental build-info orchestration are outside
  scope. Incremental/composite command execution reports an explicit unsupported
  boundary; ordinary Program emit does not fabricate build information.
- Public embedding endpoints and generated-but-unregistered LSP methods are outside
  the accepted surface. Upstream code-action, semantic-token-delta and reference/
  symlink limitations remain preserved.
- Short local LSP timings and increasing RSS samples do not establish memory
  plateaus, leak-freedom or universal speed. Runtime ownership adaptations, including
  parser stack continuation and IPC threads, have explicit resource tradeoffs.

## Last VS Code comparison

The last external VS Code output/performance comparison **predates the LSP work**.
It was not rerun after the language-services changes. Its 10,031 emitted files
(150,466,543 bytes) and ten complete diagnostics matched pinned Go exactly.

| Best of five, explicitly tuned Linux x64 | Native | Go | Native overhead |
| --- | ---: | ---: | ---: |
| Check + emit | 10.18 s | 9.37 s | 8.6% |
| Check-only | 8.55 s | 7.13 s | 19.9% nominal |

The check-only margin is narrow at the timer's 0.01-second precision. These results
use documented AVX2, checker-count, memory and runtime settings; they are not
portable-default or universal guarantees. See [full measurements and reproduction](./docs/VSCODE-CHECK-STATUS.md).
The final small retained LSP-era emit workloads are a separate protection cohort,
with modest slowdowns on four workloads and faster ManyFiles; they do not refresh
or substitute for the VS Code evaluation.

## Source and attribution

[Source mappings](./source-map.json) distinguish translated upstream workers,
generated models/tables, authored support and verification adapters.
[Mapping conventions](./docs/SOURCE-MAPPING.md) and [upstream.lock.json](./upstream.lock.json)
record provenance. Translated Microsoft code retains the
[Apache 2.0 license](./licenses/TypeScript-LICENSE.txt) and
[notice](./licenses/TypeScript-NOTICE.txt); Go, x/text, x/sys, Unicode and watcher
attributions are retained in [licenses](./licenses/).

Earlier bounded checker profiles and subsystem campaigns are historical compatibility
and verification records, not restrictions on the ordinary compiler or LSP above.
Their [foundation](./docs/FOUNDATION-ASSESSMENT.md),
[binding](./docs/BINDING-COMPLETION-ASSESSMENT.md) and
[collection](./docs/COLLECTIONS-ASSESSMENT.md) assessments retain that history.
