# Public Linux stdio assessment and reproduction

Fresh final-source public Linux x64 Native execution passed both authored paired
workloads: [disk/stdio](../../artifacts/language-services/integration-7/public-stdio-final/comparison.json)
and [signal/watchdog](../../artifacts/language-services/integration-7/public-lifecycle-final/comparison.json).
Independent [stdio evidence review](../../artifacts/language-services/source-review/public-stdio-final-evidence.json)
and [lifecycle evidence review](../../artifacts/language-services/source-review/public-lifecycle-final-evidence.json)
verify complete payloads, raw frames, exact terminals and the current published CLI.
The actual final ELF SHA256 is `edd848db2d1595fd4cdb3173d291ed6212f67a1ae9bb550ea205ca4c4466aba9`,
bound to its [actual ILC inputs](../../artifacts/language-services/final-retained94/native/TypeScript.Check/actual-ilc-input.json).
Native98 direct/corpus and final retained managed/Native/compiler/emit/no-emit gates
also passed; performance cohort and final claim review are complete; the Linux x64 contract is accepted, with delivery commit pending.

Current public medians in milliseconds are Native cold10.713, unchanged-query0.553,
edit-plus-diagnostic1.480; Go cold43.007, query0.498, edit1.066. Native RSS rises
79,192→118,180kB with sampled threads39→40; Go50,744→69,032kB with22→22 threads.
These are one short local run, not a memory plateau/leak or universal performance
claim. Both current processes observe canceled-token/old-snapshot admissible races;
all other complete responses match exactly. The two exact shutdown roles and all
four signal/watchdog roles pass without forced kill.

To reproduce current results, substitute
`artifacts/language-services/final-retained94/native/TypeScript.Check/tscheck`
for the historical Native91 executable in the two commands below, and use new
output directories. The following Native91 records remain preserved historical
public evidence, not the current ELF identity.

The actual [Native executable](../../artifacts/language-services/integration-7/public-native-91/tscheck)
SHA256 is `3aa8544df3edcd5be0fda17f28bc15e986b36b43d5b912077c5d5f1d7c843226`;
the pinned Go CLI SHA256 is `fb9f349bdbeaad767473a3ab2893271bd106e978ed58a1c0b5ffcd5502450528`.
The [executed driver](../../tools/language-services-oracle/compare-public-lsp.py)
SHA256 is `195f3004818e68bc362d0d25715125c61737ffdafedaaec6a5d67905c46ee4ee`.
[Publication inputs](../../artifacts/language-services/integration-7/public-native-91-build.json)
bind the actual public ELF and two application ILC inputs. Later source/assertion
changes do not retroactively alter this immutable executable's acceptance scope.

[Program](../../src/TypeScript.Check/Program.cs) dispatches initial `--lsp` before
ordinary argument parsing and the optional one-shot no-GC region. The public
[project](../../src/TypeScript.Check/TypeScript.Check.csproj) uses normal server GC;
LSP does not enter a no-GC region. [LspCommand](../../src/TypeScript.Scanner/Lsp/LspCommand.cs)
uses real standard streams, disk FS wrapped with bundled libraries, and real
process/cancellation owners. [Scanner resources](../../src/TypeScript.Scanner/TypeScript.Scanner.csproj)
embed all bundled declarations. The [library manifest](../../src/TypeScript.Scanner/Bundled/manifest.json)
and [generator](../../tools/program-generate-bundled-libs.py) retain the pinned
`fed0bf24149fb1ed36039212648bafdafc1ea10e` registry and 115 original library files.
No validation fixture assets are required by the public executable.

## Connect an editor

For an editor with configurable LSP servers, set the executable to the absolute
path of the published `tscheck`, arguments to `["--lsp", "--stdio"]`, and transport
to standard input/output. Start it with the project directory as its working
directory and use that directory's file URI as the initialization root. Associate
TypeScript, TSX, JavaScript and JSX documents with this server. Let the client send
its actual supported capabilities; advertised semantic token types/modifiers and
position encodings determine the negotiated responses. Keep stdout exclusively for
LSP framing and capture stderr separately. The public executable includes its
standard libraries; it does not need the validation fixture or oracle process.

The pinned [VS Code client](../../upstream/TypeScript/packages/vscode-typescript/src/client.ts)
uses an executable with `--lsp` and its stdio transport. Its
[executable resolver](../../upstream/TypeScript/packages/vscode-typescript/src/util.ts)
accepts a directory containing an executable named `tsc` or `tsgo`, rather than an
arbitrary `tscheck` filename. To use that pinned extension's local-executable route
on Linux, put a `tsgo` symlink to the published `tscheck` in a dedicated directory
and set the documented settings:

```json
{
  "js/ts.experimental.useTsgo": true,
  "js/ts.tsdk.path": "/absolute/path/to/the/dedicated-directory"
}
```

Use user-level settings, or follow the extension's workspace trust and explicit
SDK-selection prompts for workspace settings. These are the pinned extension's
actual [documented settings](../../upstream/TypeScript/packages/vscode-typescript/README.md);
no custom extension option is required. The editor UI/package installation route
has not been executed in this gate. The verified evidence covers the same public
stdio protocol through the paired request driver.

From the repository root, the coordinator can publish the actual public CLI:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true \
  -o artifacts/language-services/public-native
```

To rerun the exact retained executables from the repository root, use a new output
directory. The driver resolves executable paths before changing child cwd to its
real temporary workspace; retain existing91d artifacts rather than overwriting them:

```sh
python3 tools/language-services-oracle/compare-public-lsp.py \
  --go-command '["artifacts/language-services/integration-7/pinned-tsgo","--lsp","--stdio"]' \
  --native-command '["artifacts/language-services/integration-7/public-native-91/tscheck","--lsp","--stdio"]' \
  --output artifacts/language-services/integration-7/public-stdio-reproduction \
  --repeats 100
```

For a fresh publication, use its actual output binary in `--native-command`; a new
binary needs new source/input hashes and execution evidence, not reused91d credit.

The [reviewed driver](../../tools/language-services-oracle/compare-public-lsp.py)
retains complete responses/raw frames and observes real document/config/dependency
changes, cancellation/stale outcomes,100 unchanged full diagnostic queries and100
unsaved edits with increasing versions5–104. Every edit's complete ordered report
is retained and compared; numeric/string assignment annotations alternate valid
reports and Error2322. The config phase retains suggestion7044/Hint rather than
filtering suggestion diagnostics. The run also records process resources,
then two exact termination roles. Successful shutdown plus exit notification is
observed with stdin kept open: pinned upstream dispatch returns cancellation, so
the expected status is1 with exactly `context canceled` plus newline. A distinct
small shutdown then input-EOF run expects status0 and empty stderr. This records
the upstream lifecycle quirk; it does not admit arbitrary nonzero exits or crashes.
Complete statuses/stderr remain evidence. Resource samples have no invented portable
threshold. Its [source-reviewed promotion](./lsp-public-stdio-driver-promotion.json)
remains authored evidence, separate from original tests and the candidate backend
whose cleanup force-kills children. Publishing alone is not execution acceptance.


Observed Native RSS rose77,996→119,132kB and sampled threads43→44; Go RSS rose
50,040→69,580kB and threads21→22. Cold-process-through-initialize, unchanged-query
and edit-plus-diagnostic timings are retained separately in each process artifact.
One short synthetic workspace run establishes neither a memory plateau nor an
allocation/leak, universal performance or external-project claim. Canceled versus
completed token requests and old versus new retained snapshots are the only
source-admissible race fields compared as separate outcomes; all other full
payloads match exactly. Both processes terminated without forced kill.

The earlier91/91b/91c logs retain authored expectation/sequencing failures before
candidate completion; no failed attempt was relabeled a pass. The corrected91d
record is separate. Historical dependency74 totals in the main verification guide
remain historical; later direct assertions and source fixes require the final
current-source gates before whole-epic acceptance.


Signal and declared-parent lifecycle are separate from91d: its initialize request
uses `processId:null`. The [additional authored lifecycle driver](../../tools/language-services-oracle/compare-public-lsp-lifecycle.py)
reuses the same actual transport and exercises SIGINT, SIGTERM, initialize.processId
and the explicit clientProcessId override. Its [source manifest](./lsp-public-lifecycle-driver.json)
records exact cancellation exit/stderr and owned editor-PID cleanup. Actual paired
[public lifecycle91 execution](../../artifacts/language-services/integration-7/public-lifecycle-91/comparison.json)
passed allfour roles, with full initialization and valid diagnostic payloads matching.
The [independent evidence review](../../artifacts/language-services/source-review/public-lifecycle-91-evidence.json)
checked31 raw frames perprocess/role, complete response identities, executable hashes
and exact stderr/status. All terminate with exit1: signals emit only `context canceled`
plus newline; watchdogs first emit the exact actual declared-parent-PID exit line.
No child required forced kill or timed out. This is authored Linux evidence, not
original-test credit or an OS PPID/conflicting-PID-precedence claim. The lifecycle
driver SHA256 is `41ea6071a79e3e53c9522cbfd74ac0ca8d97b3f9575497a0cf8006b01153dbfe`.
From the repository root, with the same immutable executables and new output:

```sh
python3 tools/language-services-oracle/compare-public-lsp-lifecycle.py \
  --go-command '["artifacts/language-services/integration-7/pinned-tsgo","--lsp","--stdio"]' \
  --native-command '["artifacts/language-services/integration-7/public-native-91/tscheck","--lsp","--stdio"]' \
  --output artifacts/language-services/integration-7/public-lifecycle-reproduction
```


Native94 reproduced empty transformed text in the original parallel mapper assertion,
and managed96 still failed after handler scheduling alone. The complete synchronous
request/response/duplex delivery correction now passes that original32-file assertion
in the [Native98 shared gate](../../artifacts/language-services/integration-7/native-gates-98.json).
Those earlier failures remain historical evidence. Public91 does not verify this
later source: fresh final public drivers and full current editor/retained compiler/emit gates have
now passed. Performance cohort and final claim review are complete. The Linux x64 contract is accepted; delivery commit remains a coordinator action. The [current assessment](../LANGUAGE-SERVICES-ASSESSMENT.md)
tracks final acceptance separately.
