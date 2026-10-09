# Bounded project-checking assessment

**Accepted bounded contract P1–P5.** Baseline `2598ceb`, pinned Go revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The repaired-source gate passed with
actual Linux x64 Native AOT execution, retained regressions and independent Sol
review. This completes the [contract](./PROJECTS-CONTRACT.md), not full Program,
module resolution, library or compiler parity. See the [reproducible guide](./PROJECTS.md)
and [durable evidence](../validation/results/projects-verified.json).

## Capability and boundary

The usable unit is a small, self-contained Linux project whose `.ts`
modules use the previously accepted collection/callable/type surface. A strict
`tscheck.json` lists roots; relative dependencies are discovered with the selected
upstream Bundler algorithm. One checker owns distinct file scopes and shared
cross-file symbol/type relationships. The native command reports diagnostics and
returns failure for invalid or unsupported projects. It produces no JavaScript or
declarations.

The valid demonstration contains five TypeScript files, 100 physical lines (86
nonblank), and 2,930 source bytes. The same-base invalid variant has five files,
110 lines (95 nonblank), and 3,354 bytes. These counts exclude the three library
declarations. This is integration evidence, not a large-project scalability test.

The genuine embedded ES5/decorators/decorators.legacy environment is unchanged.
`skipLibCheck` permits demand-driven use of its declarations, not a claim of whole
library checking or all API support. Packages, Node modes, external declarations,
project references, incremental compilation, emit and services remain excluded.
Inherited language exclusions include classes/constructors, destructuring,
async/generators, optional chaining, object spread and custom iterators. These
restrictions materially limit ordinary application use and must remain prominent.

## Acceptance ledger

| Contract row | Acceptance evidence |
| --- | --- |
| P1 configuration/loading/resolution | Selected mapped loader/resolver, file identity/search precedence, strict authored configuration, original/adapted helper assertions, missing/casing/mixed diagnostics and explicit unsupported controls pass. Non-scalar filesystem paths reject without replacement substitution. |
| P2 module semantics | All contracted forms are represented in the [coverage inventory](./projects-coverage.json); 48/48 native cases match independently regenerated original Program/default-resolver results, including semantic failures. |
| P3 cycles/identity/state | Recursive functions/types, alias/star cycles, collisions and shared targets pass. Valid/invalid reversed roots retain actual original discovery order. Fourteen selected cases yield 30 exact repeated/order executions per runtime; shared checks cover fresh/foreign ownership and cache identity. |
| P4 diagnostics/demo | Valid five-module example has zero diagnostics; invalid variant has eight expected diagnostics. Ordinary native CLI passes all ten controls with complete demo diagnostic equivalence, byte/UTF16 positions and expected 0/1/2 exit statuses. |
| P5 regressions/review/delivery | Independent source, observation, tooling and final evidence reviews closed. All 23,542 cumulative xUnit rows pass, zero skipped; all retained native regressions pass. Nine native ELF x64 executables and 115 historical fixture/proof files are fingerprinted and checked. |

The final gate completed in **780.934 seconds**, with no source, asset, historical
fixture/proof or upstream drift. It independently reproduced all 48 Go expectations,
executed all 48 native comparisons, ran 27 shared native assertion groups and 14
CLI projection controls, and verified the ordinary command. These verification
runtimes are not a compiler-performance benchmark.

## Test provenance

The [test inventory](./projects-tests.json) distinguishes **11 newly ported full
upstream test functions**, **4 adapted groups**, and **12 authored groups** in the
27 shared checks. The 48 project differential inputs are authored; no new whole
upstream compiler test is claimed from those inputs. The original 11 full functions
also execute in Go. Their preserved assertions include 224 literal values, six
identity checks and two rooted-probe checks.

The support applicability catalog contains 148 entries: 17 assertion-preserving
functions (11 new, six already covered by the retained foundation cascade), three
adapted tests, and 128 exclusions (116 test functions, nine benchmarks, three fuzz
entries). Exclusions have recorded dependency/scope reasons. The checker import-clause
test is the additional fourth adapted group. Cumulative xUnit totals include existing
data-driven foundation and semantic rows, not 23,542 original upstream test functions.

## Evidence limits

The project comparison observes loaded files/resolution edges, passive binding
relationships, module/export and reached alias identities, selected types/signatures,
and complete applicable diagnostic channels. It is not a census of every private
checker cache. Unrooted export-type module clones are compared as a validated
structural multiset; their allocation identity and cloned-table topology are not
claimed. Reached identities remain exact. Accessibility cache topology is not
directly observed. Retained collection tests separately protect their established
generic-cache surface.

Malformed syntax is reported with parse diagnostics and a failing command; it does
not establish complete semantic diagnostics for malformed programs. Native acceptance
applies only to Linux x64. Other platforms are unverified.

## Execution observations

Three explicit GPT-6.1 Sol owners were reused for checker logic, loading/resolution,
and independent oracle/corpus work. One owner builds the shared library; consumers
use immutable snapshots. Formatter/accessibility work was handed off as an exclusive
file/dependency unit when it became the serial constraint. Focused comparisons precede
the frozen comprehensive regression gate.

Integration exposed both compiler defects (nil Go maps represented by nullable
managed tables, demanded expression printer dispatch, omitted diagnostic arguments)
and harness defects (graph registration order and diagnostic filename relocation).
The original Go expectations were retained while their respective implementations
were repaired. Additional upstream tests were inventoried when newly required
specifier/package helpers entered scope.

The first full acceptance run was stopped, without acceptance, after a late
negative control proved that replacement-decoding a lone-surrogate import could
load a different existing filename. The bounded disk host now uses strict UTF8
path decoding and explicit unsupported-path rejection; scanner and ordinary
string-literal behavior remain unchanged. The original 48 expected project results
are unchanged. This finding shows that valid Unicode examples alone were insufficient:
future host-boundary reviews should challenge invalid encoding and collisions before
the expensive acceptance run. The fresh complete gate subsequently passed on the
repaired source.

The pre-acceptance ledger contains **11 central Debug library compiler invocations**
(including three failed integration compiles), totaling **59.76 seconds** of reported
build time. Adapter/CLI/check-target builds and Release/AOT publishing are separate;
this is not a count of every tool or build invocation. The preceding campaign reported
24 central builds, but the scopes differ, so this is not a controlled efficiency or
token-cost comparison. Compilation time was small compared with dependency analysis,
semantic observation work and comprehensive acceptance.

One expensive gate was stopped for the path-decoding defect; its partial successes
do not establish acceptance. The replacement run completed on repaired source in
780.934 seconds with all gates passing. The first coordinator timestamp was 2026-10-09 07:33:59 UTC. Elapsed wall time is not total model
effort; no token or monetary measurements are available. No user intervention or
package installation has been required to unblock the work.

## Next campaign

The next project-usability campaign should port genuine upstream configuration and
file discovery: a bounded tsconfig surface with `files`/`include`/`exclude`, original
config diagnostics, and explicit rejection of unsupported options. The current
authored configuration and explicit root list remain concrete barriers to opening
existing projects. Keep package resolution, references, incremental state and emit
separate unless that next contract explicitly includes them.

This would improve project entry, not remove the independent language/library
barriers: classes/constructors, destructuring and other inherited exclusions,
broader standard libraries, user declaration files and package dependencies still
prevent ordinary application checking. Progress here establishes the bounded
disk-to-diagnostics path, not whole-checker or general project parity.
