# Legacy decorators — active

From accepted `34ade775`, maps owns the complete pinned
`fed0bf24149fb1ed36039212648bafdafc1ea10e`
[tstransforms/legacydecorators.go](../../upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go)
counterpart: all 48 functions and `allDecorators` payload in
[LegacyDecorators.cs](../../src/TypeScript.Scanner/Transformers/TsTransforms/LegacyDecorators.cs).
Namespace is `TypeScript.Transformers.TsTransforms`; entry point is
`LegacyDecoratorsTransformer.NewLegacyDecoratorsTransformer(TransformOptions)`.
`GetDecoratorsOfParameters(Node)` is the actual shared helper consumed by Metadata,
returning `Slice<Slice<Node>>` with the original this-parameter offset and lazy
allocated-length owner. Root owns central compilation and acceptance. Inventory
owns actual Program/full-output controls and independently reviews this source;
maps independently reviews foundation Metadata/TypeSerializer. ES Utilities is
deferred with its real async dependencies to the complete ES family.

The class alias dictionary uses node reference identity and resets at source-file
boundaries. Enclosing classes use original append/pop slice headers; conditional
Go defer covers the entire decorated-class function via scoped try/finally.
Decorator expression visitation temporarily removes only the applicable enclosing
class, then restores it. Private-identifier decorator placement preserves the
explicit new member-list owner before appending the static block. Source/comment
ranges, helper registration, decorator/parameter/metadata order, and ES2022 alias
static blocks follow the original. Immutable `allDecorators` has no pointer-identity
consumer, so nullable readonly value snapshots retain its two slice headers.
The synchronous recursive reference visitor and instance metadata grouping callback
are cached per transformer; the original class-comparison state is scoped/restored.

Required small collection dependency is the complete 11-function
[MultiMap.cs](../../src/TypeScript.Scanner/Collections/MultiMap.cs) counterpart of
[collections/multimap.go](../../upstream/TypeScript/tsc/internal/collections/multimap.go).
The initially quoted 13-function inventory was corrected against the actual pinned
75-line source. Zero dictionary reads return absent/nil group headers; first Add
allocates backing; negative capacity hints normalize to zero as Go runtime make
does. Group append preserves input order, removal keeps overlapping
append/capacity/tail behavior, and Clear retains backing. Keys/Values use value
sequences over standard dictionary struct enumerators rather than yield/list copies.

New source-name consumers retain borrowed `ByteStringValue` through class alias
unique-name construction and identifier-to-property-string-literal conversion.
Focused existing-file joins in
[Factory.cs](../../src/TypeScript.Scanner/Printer/Factory.cs) add borrowed unique-name
overloads and share the same generated-ID/flags/fallback worker.
[EmitContext.cs](../../src/TypeScript.Scanner/Printer/EmitContext.cs) adds the actual
variadic-slice helper consumer through a span worker, preserving required upstream
helper ownership without manufacturing an array copy. Existing string/params entry
points remain wrappers. These supporting deltas need independent narrow review.

All source bodies are written and the first central compilation exposed six
declaration joins, now corrected together using existing slice construction and
explicit unsigned subtree masks. Production is frozen for the next central build.
Inventory source review is otherwise clear; final hashes, seven shared authored
MultiMap assertions, combined pinned-Go witnesses and genuine Program output
comparisons are pending. There is no direct original legacy-decorator
unit-test file; actual transform-chain outputs must establish behavior. No local
production builds, managed execution or Native acceptance have run for this packet.
