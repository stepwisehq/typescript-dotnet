# JSX and const-enum inlining — active

From accepted ES checkpoint `13d29ca3`, this lane owns the complete pinned
[JSX48](../../upstream/TypeScript/tsc/internal/transformers/jsxtransforms/jsx.go)
and [ConstEnum3](../../upstream/TypeScript/tsc/internal/transformers/inliners/constenum.go)
functions. All51 bodies and all253 original entity records are written, initial
ownership/API reconciliation is complete, and source is formatted/build-ready.
First central compile exposed one JSX two-argument Append API join; it now uses
two actual Append calls on the original allocated capacity3 owner. Production
is refrozen. Foundation’s complete JSX48/ConstEnum3/EntryValues independent
source review is clear in [jsx-inliner.json](../../artifacts/emit/source-review/jsx-inliner.json).
No managed/Native acceptance claim.
Root owns all integration builds. Inventory owns the shared actual Program/full
public diagnostic and full-chain Go output batch; foundation owns CommonJS.

The agreed factories are `JSXTransformer.NewJSXTransformer` in JsxTransforms and
`ConstEnumInliningTransformer.NewConstEnumInliningTransformer` in Inliners. Actual
emitter order remains TS → conditional JSX → ES → UseStrict → Module → conditional
ConstEnum last. JSX uses real resolver import-reference marking; const enum uses
real resolver constants. Existing borrowed JSX import helpers and source text
headers remain through consumers, while generated names/comments own required
outputs. Required slice insertion, cloned attribute deletion, sorted import collection
and all original explicit capacity owners are retained. The required focused
OrderedMap `EntryValues` value enumerator preserves live order and captures each
key/value at the original yield step; both JSX consumers avoid iterator allocation.
Thirty-one actual Program/full-chain inputs (24 JSX, seven const-enum) are submitted
in [jsx_inliner_cases.json](../../tools/emit-oracle/jsx_inliner_cases.json) for the
shared Go batch. The combined75 reference cases passed and exact output/full
diagnostics are captured; inventory independently reviewed these31 managed tables.
The first managed invocation preserved prior287 passes but all75 new cases failed
before transformation because absent Go enum options serialize as empty strings.
Inventory corrected empty enum defaults, nil Files loading and raw UTF8 fixture
ingress. The independent all75 shared schema/consumer/materializer review is clear
in [script-shared-controls.json](../../artifacts/emit/source-review/script-shared-controls.json);
corrected integrated managed and actual Native AOT gates remain pending.
The complete independent CommonJS83 source review is clear in
[commonjs.json](../../artifacts/emit/source-review/commonjs.json), including required
prologue clones, ref export-variable state and final readable descriptor construction.

Second integrated run passed360 and exposed two const-enum string consumers: the
accepted evaluator retains string constants as ByteStringValue, while this new
inliner initially switched only on owned string. The borrowed case now feeds
NewStringLiteralWithTextValue directly; numeric/bigint algorithms and expected
outputs stay unchanged. Foundation reviews this narrow consumer fix before the
next coordinated run.

Final coordinated acceptance passed all362 emit cases plus retained regressions
(1,379 managed total). Actual Linux x64 Native AOT passed151 original functions
and243 authored controls. The corrected enum/nil-files fixture loader and borrowed
const-enum string consumer are independently reviewed; original expectations and
captured Go output/full diagnostics were unchanged. Root records the clean commit.
