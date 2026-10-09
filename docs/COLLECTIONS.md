# Checking generic collections

**Accepted within the fixed contract.** The [recorded proof](../validation/results/collections-verified.json)
contains 44/44 exact Linux x64 Native AOT comparisons, 12 shared checks, 18 exact
order replies per runtime, complete original-overlay behavior parity and 23,467
passing xUnit cases with no skips. The comprehensive gate took 658.892 seconds with
no source, verification-asset or historical-asset drift. The fixed
[contract](./COLLECTIONS-CONTRACT.md) and [status](./COLLECTIONS-STATUS.md) define the boundary.
This is type checking, not TypeScript emission or execution.

The Collections profile adds useful generic collection checking to the same ported
checker. The [grouped-records example](../examples/collections/grouped-records.ts)
uses generic grouping over recursive record arrays, contextual `filter`/`map`/`reduce`
callbacks, loops, a tuple summary, rest/spread calls and mapped utility types. Its
[invalid variant](../examples/collections/grouped-records-invalid.ts) deliberately
violates constraints, properties, tuple arity, reducer results, iteration, readonly
writes and mapped values. Eight invalid checks are not a promise of eight diagnostic
records: chains and related information are preserved separately.

## Fixed declaration environment

Every Collections checker parses fresh copies of these complete, unmodified files
at upstream revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`, in this order, followed
by one user TypeScript script:

| Logical file | SHA-256 |
| --- | --- |
| `/lib/lib.es5.d.ts` | `45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215` |
| `/lib/lib.decorators.d.ts` | `1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4` |
| `/lib/lib.decorators.legacy.d.ts` | `b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da` |

The exact bytes and notices are embedded with the
[library manifest](../src/TypeScript.Scanner/Checker/Libraries/manifest.json).
Ordinary managed/native checking does not launch Go, consult an upstream checkout,
or discover declaration files on disk. The decorator files are ES5's actual
reference closure; their presence does not enable source decorators.

The target is ESNext. `strictNullChecks`, `strictFunctionTypes`, `noImplicitAny` and
`noImplicitThis` are enabled. `strictBindCallApply`, `noImplicitReturns`,
`exactOptionalPropertyTypes`, `noUncheckedIndexedAccess` and `downlevelIteration` are
disabled. `noLib`/`noResolve` disable implicit discovery while the three explicit
library roots remain loaded. `skipLibCheck=true` skips whole-library declaration
checking; parsing, binding, global merging and demanded member/signature/constraint
resolution remain real. All four explicit roots have `defaultLibrary=false` under
this fixed host, even though three are declaration files.

## Supported surface and limits

The promised surface includes generic aliases/interfaces/functions/methods with
constraints, defaults, explicit arguments and inference; arrays/ReadonlyArray and
all their ES5 instance APIs; overloads and contextual callbacks; concrete labeled,
optional, rest and readonly tuples; identifier `T[]` and concrete tuple rest;
array/call spreads; numeric/string/symbol index signatures and element access/writes;
`keyof`, indexed access, plain mapped types, conditional types and `infer`.
The documented utilities are `Record`, `Partial`, `Required`, `Readonly`, `Pick`,
`Omit`, `Exclude`, `Extract` and `NonNullable`. Internal dependencies such as
`Awaited` and library binding parameters are resolved when demanded.
Ordinary function bodies, `if`/`else`, loops, unlabeled `break`/`continue`, scalar
operators and compound/increment/decrement writes are included.

This is a fixed capability boundary, not complete generic checking or all-loaded
ES5 API parity. Material exclusions remain:

- Multiple user files, imports/exports, module resolution, user namespaces and user
  declaration-file inputs. Ambient `declare function`/`declare` variables inside
  the admitted script are allowed; arbitrary user `.d.ts` files are not.
- Source classes, `new`, construct signatures/types, decorators, async/generators,
  custom iterators and ES2015 `Map`/`Set`/iteration environments. Array-like `for-of`
  follows the original fallback under the absent Iterable environment.
- Destructuring, optional chains, assertions, object spread, accessor declarations,
  computed property declarations, template-literal type syntax, key remapping,
  intrinsic string transforms, generic variadic tuples such as `[...T]`, and bare
  tuple-constrained type-parameter rest. Aliasing an excluded rest operand does not
  make it supported; array aliases and concrete tuples with generic elements remain
  supported.
- Source `typeof` type queries, semantic JSDoc/checking directives, triple-slash
  external references, JSX/JavaScript/JSON input, and `switch`/`try`/labels.

Ordinary expression `typeof value` is distinct from excluded type-query syntax.
The current user admission also rejects interpolated template expressions and
no-substitution template literals; ordinary quoted string literals are supported.
Symbol index signatures and element access are distinct from excluded computed
property declarations. Complete embedded declaration files do not promise `Intl`
or every other loaded global API. Unsupported demanded paths fail explicitly with
`UnsupportedSemanticFeatureException`; library declarations bypass the user syntax
gate so required internal/library semantics are not rejected by source-only limits.

## Managed API and diagnostics

Reference the [scanner library project](../src/TypeScript.Scanner/TypeScript.Scanner.csproj)
from a .NET 10 application. A minimal call uses the public
[checker profile API](../src/TypeScript.Scanner/Checker/Admission.cs):

```csharp
using System.Text;
using TypeScript.Checking;
using TypeScript.Scanner;

string source = "function id<T>(value:T):T{return value;} const answer=id(42);";
var file = Parser.ParseSourceFile(
    new SourceFileParseOptions("/main.ts", "/main.ts"),
    Encoding.UTF8.GetBytes(source), ScriptKind.TS);

foreach (var diagnostic in file.Diagnostics() ?? [])
    Console.WriteLine(diagnostic.String());

var checker = Checker.Create(file, CheckerProfile.Collections);
var checkDiagnostics = checker.GetDiagnostics();
var globalDiagnostics = checker.GetGlobalDiagnostics();
foreach (var diagnostic in checkDiagnostics ?? [])
    Console.WriteLine(diagnostic.String());
```

Parse diagnostics belong to the parsed source file; malformed inputs are rejected
by the checker admission gate. After creation, `file.BindDiagnostics()` exposes
binder diagnostics. `checker.GetDiagnostics()` returns **raw user checker
diagnostics**, and `GetGlobalDiagnostics()` exposes the checker global channel.
Neither is a public project `Program` diagnostic API. Diagnostic codes, byte ranges,
message chains and related information remain accessible on each `Diagnostic`.

The selected [node query APIs](../src/TypeScript.Scanner/Checker/Checker.cs)
include `GetTypeAtLocation` and `GetSymbolAtLocation`;
[TypeToString](../src/TypeScript.Scanner/Checker/Printer.cs) exposes type display.
[Additional public APIs](../src/TypeScript.Scanner/Checker/Exports.cs) expose
declared/symbol/property types, signatures/returns/predicates/resolved calls,
widening and assignability.
Node queries must belong to the user script. Types/signatures and checker-created
symbols must belong to that checker; genuine fixed-library bound symbols have
file-set ownership. `SourceFiles` exposes the ordered fixed four-file set, not a
project loader or authorization to query arbitrary library nodes publicly.

The validation executable separately implements the fixed upstream **Program-level**
policy: compose applicable bind/check diagnostics, sort/deduplicate and merge related
information, with declaration-file `skipLibCheck` disposition. It reports that result
as `files[].programSemanticDiagnostics`; raw checker diagnostics must not be renamed
Program semantics in an embedding application.

## Native stdin demonstration

Run these commands from the repository root. Building requires the .NET 10 SDK and
the Linux x64 Native AOT C/linker toolchain. Once published, ordinary execution uses
the native binary and embedded declarations, with no Go or upstream dependency.

```bash
repo_root=$(pwd)
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Collections.Validation/Collections.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" \
  -o artifacts/collections-demo

python3 - <<'PY' | artifacts/collections-demo/Collections.Validation > artifacts/collections-demo/results.jsonl
import base64, json
from pathlib import Path
for name in ("grouped-records.ts", "grouped-records-invalid.ts"):
    data = (Path("examples/collections") / name).read_bytes()
    print(json.dumps({
        "name": name, "profile": "Collections", "fileName": "/main.ts",
        "textBytesBase64": base64.b64encode(data).decode(),
        "storedObservations": False
    }))
PY

python3 - <<'PY'
import json
from pathlib import Path
names = ("grouped-records.ts", "grouped-records-invalid.ts")
lines = Path("artifacts/collections-demo/results.jsonl").read_text().splitlines()
for name, line in zip(names, lines, strict=True):
    response = json.loads(line)
    user = response["files"][3]
    print(name, "user Program codes:",
          [d["code"] for d in user["programSemanticDiagnostics"] or []])
    print("global:", [d["code"] for d in response["globalDiagnostics"] or []])
PY
```

The entry reads one JSON request per line and emits one JSON response per line.
`textBytesBase64` preserves exact source bytes; plain UTF-8 `text` is also accepted.
The validation host uses logical `/main.ts` and profile `Collections`.
Responses include options, four ordered file records with hashes/checking disposition
and parse/bind/Program diagnostics, global/configuration diagnostics, passive binding
observations and user query types. Default queries select user variable names;
`queryNodeIds` is an optional validation-protocol node-ID list, not source byte offsets.
`storedObservations=true` adds the large retained-state verification graph and is
unnecessary for ordinary demonstrations.

The expected valid result has no parse/bind/global/configuration/Program diagnostics.
The invalid example retains nonempty user Program diagnostics. These expectations are
separate from successful process exit: a type error is reported in JSON, whereas an
unsupported path or protocol failure terminates checking explicitly. The commands
above publish a separate demonstration binary. The final gate independently published
and ran [Collections.Validation](../artifacts/collections-validation/native/Collections.Validation)
against the exact shipped example bytes in the canonical corpus; its successful
[proof](../validation/results/collections-verified.json) does not imply this separate
publication directory was used.

## Focused verification and final acceptance

Focused checking uses
[Checker.Tests](../tests/Checker.Tests/) and the strict
[collection comparer](../tools/compare-collections.py) against a selected request/
expected group from the [corpus manifest](../validation/collections-corpus/manifest.json).
A managed comparer invocation uses `--managed` and a built validation DLL; a native
invocation supplies the native executable without that flag. Expected outputs are
independently generated from the pinned Go checker; no command regenerates accepted
historical goldens to fit the port.

```bash
dotnet test tests/Checker.Tests/Checker.Tests.csproj \
  --filter 'FullyQualifiedName~CollectionsControlsTests|FullyQualifiedName~CollectionsSupportTests'
python3 tools/compare-collections.py \
  --requests validation/collections-corpus/canonical-requests.jsonl \
  --expected validation/collections-corpus/canonical-expected.jsonl.gz \
  --executable artifacts/collections-demo/Collections.Validation \
  --output artifacts/collections-focused
```

The comparer command selects the published 44-case rich canonical group. It does
not replace the complete acceptance gate, which also checks the other runtimes,
shared assertions, order controls and source/asset fingerprints. Original
reproduction is explicit verification work requiring the pinned checkout and Go
oracle tooling. Ordinary native demonstration does not use it.
The focused xUnit command runs shared profile/API/support assertions. The manifest-driven
`CollectionsOracleTests` theory executed all 44 canonical cases in the recorded gate.

The recorded gate used frozen compiler/observer/corpus source and closed independent
reviews. To reproduce that comprehensive verification, use the gate and then its
separate recorder:

```bash
python3 tools/run-collections-gate.py
# Only after successful exit, review closure and unchanged source/assets:
python3 tools/record-collections-evidence.py
```

The [gate](./progress/collections-gate.md) performs one retained regression cascade,
all final manifest groups/shared assertions/order controls, original reproduction,
complete metadata-overlay behavioral parity, and seven actual Native AOT targets.
It fingerprints compiler source, exact libraries and shipped examples, verification
assets and executables before/after execution. The recorder writes the new collection
proof only after those checks; earlier accepted campaign proofs remain unchanged.
The [recorded evidence](../validation/results/collections-verified.json) accepts this
bounded collection contract; it does not establish all-language, all-library or whole
generic-checker parity.

## What a multi-file application still needs

The checker already parses/binds four fixed files and merges their script globals.
That does not provide a user project host: `noResolve` is set, only one user file has
public node-query ownership, and import/export aliases, relative module resolution,
cyclic module dependencies and per-user-file Program diagnostic traversal are not
promised. A bounded ES-module campaign covering relative imports, named/default
exports and re-exports, cycles and cross-file generic calls is the next informative
step. Classes/new, destructuring, optional/async syntax, custom iterators and broader
library surfaces are separate capability work; a file host alone does not implement
them or establish whole generic-checker parity.
