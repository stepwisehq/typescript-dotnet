# TypeScript compiler and language services in C#

> [!WARNING]
> This project was created as a case study and will not be actively maintained.
> It is provided as-is; ongoing fixes, updates, and support are not planned.

A C# port of the pinned Go TypeScript compiler, with project checking, ordinary
Program emit and a real language server. The agreed compiler and language-services
contracts are complete and independently verified under **Linux x64 Native AOT**.
This is a pinned-upstream implementation, not a claim of complete repository,
all-platform or every historical TypeScript feature parity.

Performance on vscode project:

| Best of five, explicitly tuned Linux x64 | C# Native AOT | Go | Native overhead |
| --- | ---: | ---: | ---: |
| Check + emit (same-host investigation) | 9.61 s | 8.52 s | 12.8% — within the 20% target |
| Check-only (same-host investigation) | 6.96 s | 6.15 s | 13.2% — within the 20% target |


The reference is Microsoft TypeScript revision
[`fed0bf24149fb1ed36039212648bafdafc1ea10e`](https://github.com/microsoft/TypeScript/tree/fed0bf24149fb1ed36039212648bafdafc1ea10e/tsc).
See the [upstream record](./UPSTREAM.md), [current state](./docs/STATE.md),
[compiler assessment](./docs/EMIT-ASSESSMENT.md) and
[language-services assessment](./docs/LANGUAGE-SERVICES-ASSESSMENT.md).


## Humon Scratch

I know, "*another* port 🙄".

But the lazy Rust port from last week struck close to home. I'm a bit of a TypeScript geek and I learned Golang way back during Stripe CTF3; a performance context and even clenching a 4th place in the document indexing round. Which some may remember was scaffolded in Scala.. Well I noped out of that and wrote it in Go 😂. And I'm a C# Stan.

So this Rust port claimed to burn up "**400k in ChatGPT!!!**" and Codex couldn't get the job done after months. Opus 5.5 to the rescue in 10 hours and you *could* do it for "**$20k!!**".

Wut?! Hold my 🍺

### Goals
* Use Codex
* Port TypeScript from Golang to C#
* Adhere closely to the Golang source
* Optimize it to within +20% of Golang projects performance on VSCODE
* Learn some stuff
* Do it cheap!

Way back it was curious to me the TypeScript team selected Golang instead of C#. IIRC DotNet native AOT was still early days and Golang had more portability.. Also a lot of judgement calls over the suitability and how closely the concepts mapped; all understandable in the era of hand-porting code LOL. But I always wondered how fast a C# implementation *could* be..

That last bit about doing it cheap.. I wanted to see if the process could be optimized through early high-level strategy and steering where necessary.

Not to bury the lede => **Cost was ~$75-100 USD**

### Overall Strategy

High-level port direction and agent instruction was formed with ChatGPT *outside* Codex. A few explicit goals were articulated and used to form the initial agent prompt:

* Astra 6 coordinating agent delegating research, implementation, and review to 6.1 Sol subagents
* Keep port as close to the Golang source as possible; maintain function bounderias, class names, file names, structure, etc
* Deviate where necessary for C# conventions
* Write readable and maintainable code; assume humans will be reviewing
* Implement the project as efficiently as possible; minimize rework, plan and task for work efficiency

At a high-level, the strategy is for Codex to approach this like I would. Initially a straight port maintaining as much syntax and architecture as possible. This loweres the cognitive load(context) when referencing the source duruing implementation and debugging.

ChatGPT synthesized this into the initial prompt which was fed into Codex. I would continue to consult with ChatGPT to generate steering prompts for Codex as the port progressed.

### Key Performance Decisions

After the checker was complete, and before Emit and LSP was implemented, I kicked off a performance epic with a target of +20% wall time over the Golang project when compiling VSCode.

Initially, and before the target was set, Codex became fixated on making and testing local improvements. This was creating a ton of churn; very inefficient. Eventually it may have decided to go bigger, but I steered it towards more holistic and comprehensive work being necessary to come close to the highly optimized Golang source.

You can find Codex's summary of the main performance work [here](docs/PERFORMANCE-EPIC-SUMMARY.md).

Memory allocation was, predicably, the biggest performance issue with the C# code. Here are two changes that made the biggest gap reduction:

**[Slice\<T\>](src/TypeScript.Scanner/Runtime/Slice.cs)**

During the first performance epic I pushed Codex towards introducing `Slice<T>`. It provided the storage/allocation/ownership semantics and syntax of Golang slice. This kept the C# code looking very syntatically similar to the Golang port, while providing very similar performance characteristics.

**[ByteStringView](src/TypeScript.Scanner/Runtime/ByteStringView.cs)**

The initial implementation used C# strings and substrings throughout. These create a ton of GC pressure as strings and substrings allocate on the heap, and they also create a lot of text encoding/decoding work for the CPU.

Typical performant Golang code involving UTF-8 uses byte buffers and byte comparisons throughout, as well as using slice to avoid unecessary copying.

System.Text.Json uses very similar techniques and as a good reference.

I pushed Codex to utilize these techniques comprehensively, and it created `ByteStringView` for this purpose.

### Execution as recalled by a humon

The basic setup was an Astra project coordinator delegating coding, research, and review tasks to 6.1 Sol sub agents.

I worked with ChatpGPT(Astra) to produce the prompt provided to the agent. I explained my desire to stick close to the Golang source and to execute the work as efficiently as possible.

The prompt was then provided to Codex.

As it got going I suggested it record a re-entry in case the session was lost or the VM crashed(which it did!). And provided a few more steering ideas as I observed the work. I installed Sqlite for it to use for coordination, but it opted to stick with files throughout TMK.

At this stage it was executing bounded vertical slices because I phrased the project as a "feasibility study". After it finished a campaign(what it started calling sprints?) I would take the summary back to ChatGPT and we'd decide the next chunk of work and a new prompt fed in to continue..

After a few rounds I determined the token burn was low enough to finish the entire port cheaply, and moved to have the entire checker completed. The agent continued to expand the outer implementation boundary one "bounded vertical slice" at a time though.. This proving hugely inefficient as it kept running into hacks and kludges it contructed to make the feature slices "work" without error 🙃

This was leading to tons of "churn" through test/analyze/fix cycles every time a missing bit of functionlity was hit. I directed it to start completing entire subsystem dependencies comprehensively and this made a big improvement.

At this point I also noticed the agent had created tons of internal state partiy tests. It put code into the Golang project to record states of caches and etc and.. It became fixated on differences that seemed like they would have no externally observable impact, or that the upstream test suite should catch eventually anyway..

So I directed it away from doing that which seemed to really accelerate work.

At this point it was give the "epic" of completing checker parity. A new efficiency issue popped up where after subagents checked in fixes for tests it would run the **entire** fracking regression suite it built. I steered it towards targetted testing and deferring full regression for end of epic and this proved super helpful.

From here on much less steering occured. Checker was finished and validated against the VSCODE project. And then we started the major performance push. Golang was allocating ~5.1G during the vscode project type check and we were doing like ~56G!!! Wall time was 6x+ 😭

Here is the summary of the work that got us within +20% Golang: [PERF EPIC SUMMARY](./docs/PERFORMANCE-EPIC-SUMMARY.md)

After checker the Emit and LSP epics were fed in and *laregely* required no stearing. The recorded perf epic lessons were used to get vscode complilation with emit to within +8.5% golang with no extre input from me..

The LSP epic took about 12 hours, completely unattended.

### Lessons Learned

### Approach saves $$$$
How work is carried out plays a huge role in the outcome; both the quality of results and how much token spend it took to get there. Some high-level strategy and a few process stearing can go a long way.

### Golang source is optimized
Hats off to the TypeScript team.. A lot has to do with idiomatic Golang being quite efficient in memory allocation and sharing compared to idtiomatic C#. But it's quite thoroughly adhered to and absolutely mopped the floor with the LLMs first checker parity build.

### Agent fixation is real
Agents can get fixated on a few tasks and start ignoring other responsibilities. The coordinator agent had a lot on its plate including being responsible for **efficient** execution of the project. However it was sometimes blind to obvious(to me) inefficiencies in the process that crept in.

I believe a layer above the coordinator focused on analyzing and steering the coordinator on process efficiency could have potentially replaced me taking that role.


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
The published Native AOT executable runs without an installed .NET runtime.

Optional automatic type acquisition uses npm. Configured content-mapper hosts may
require additional tools, depending on the selected mapper.

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

## Current VS Code comparison

A fresh post-LSP comparison on 2026-10-13 used the current build and pinned Go on
VS Code's main source project. All **10,031 emitted files / 150,466,543 bytes** and
all ten complete comparable diagnostics still match exactly. Both compilers exit 2
for those known diagnostics.

| Best of five, explicitly tuned Linux x64 | Native | Go | Native overhead |
| --- | ---: | ---: | ---: |
| Check + emit (same-host investigation) | 9.61 s | 8.52 s | 12.8% — within the 20% target |
| Check-only (same-host investigation) | 6.96 s | 6.15 s | 13.2% — within the 20% target |

The matched pre-LSP executable took 9.68 s for check + emit and 8.44 s for
check-only. Emit medians were 13.52 s current / 12.46 s pre-LSP, so the best-run
result does not establish unchanged performance. Earlier current-binary emit
cohorts missed the target by 42.3% and 34.3%; all remain separately preserved.
Same-binary variability is substantial and unexplained; no stable LSP regression
has been demonstrated. All samples, medians, ranges, memory and complete no-GC
qualification are in [current measurements and reproduction](./docs/VSCODE-CHECK-STATUS.md).
These use AVX2, eight checkers, minimum 128 workers and explicit one-shot no-GC
reservations; they are not portable-default or universal guarantees. Small retained
LSP-era benchmarks are a separate workload and do not substitute for this comparison.

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

## Licensing

Original project contributions are licensed under the [MIT License](./LICENSE).
Translated upstream code retains its [Apache 2.0 license](./licenses/TypeScript-LICENSE.txt)
and [notice](./licenses/TypeScript-NOTICE.txt). Other third-party code and data retain
their respective licenses and attributions in [licenses](./licenses/). The MIT License
does not replace those upstream or third-party terms.
