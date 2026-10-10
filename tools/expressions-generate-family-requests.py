#!/usr/bin/env python3
"""Finite authored E1-E5 public-behavior inputs. Never generates expected output."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'validation/expressions-corpus'
OPTIONS = dict(strict=True, noEmit=True, target='ES2022', module='ESNext',
               moduleResolution='Bundler', lib=['ES2022'], types=[])
rows = []
domains = {}

def case(name, domain, files, options=None, queries=(), roots=None, incremental=()):
    if isinstance(files, str):
        files = {"main.ts": files}
    opts = OPTIONS | (options or {})
    if opts.get('noLib'):
        opts.pop('lib', None)
    roots = roots or list(files)
    inputs = [{'Path': '/tsconfig.json', 'Text': json.dumps({'compilerOptions': opts, 'files': roots}, separators=(',', ':'))}]
    inputs += [{'Path': '/' + path, 'Text': text} for path, text in files.items()]
    semantic = dict(PerFile=True, Repeat=True)
    if queries:
        semantic['Queries'] = [dict(File='/' + file, Identifier=identifier, Operation=operation, Occurrence=occurrence)
                               for file, identifier, operation, occurrence in queries]
    if incremental:
        semantic['IncrementalFiles'] = ['/' + path for path in incremental]
    rows.append(dict(Name=name, ConfigPath='/tsconfig.json', Files=inputs, Bind=True,
                     SingleThreaded=True, Semantic=semantic))
    domains[name] = domain

def q(identifier, operation='type', occurrence=0, file='main.ts'):
    return (file, identifier, operation, occurrence)

case('nested-object-binding-default-rest', 'E1', '''const source = { id: 1, nested: { label: "ok" }, extra: true };
export const { id, nested: { label }, absent = 3, ...rest } = source as typeof source & { absent?: number };
''', queries=[q('id'), q('label'), q('absent'), q('rest', 'properties')])
case('array-binding-default-elision-rest', 'E1/E3', '''const tuple: [number, string?, ...boolean[]] = [1, "ok", true, false];
export const [head, label = "default", , ...tail] = tuple;
''', queries=[q('head'), q('label'), q('tail')])
case('binding-parameter-contextual-callback', 'E1/E2', '''export function summarize({ count, label = "x" }: { count: number; label?: string }, [first, ...tail]: readonly number[]) {
 return { count: count + first, label, rest: tail };
}
export const summary = summarize({ count: 2 }, [1, 3]);
export const labels = [{ id: 1, label: "a" }].map(({ id, label }) => label + id);
''', queries=[q('summarize'), q('summary', 'properties'), q('labels')])
case('binding-iterable', 'E1/E4', '''export const [first, second = 0, ...remaining] = new Set([1, 2, 3]);
''', queries=[q('first'), q('second'), q('remaining')])
case('binding-invalid-rest-and-source', 'E1', '''const [first, ...tail, last] = [1, 2, 3];
const { absent, ...rest } = 42;
const [value] = {};
const { key = "x" }: { key: number } = { key: 1 };
''')
case('binding-default-initializer-reference', 'E1/E5', '''export const [first = 1, second = first + 1] = [] as number[];
export const { left = 1, right = left + 1 } = {} as { left?: number; right?: number };
''', queries=[q('first'), q('second'), q('left'), q('right')])
case('object-assignment-pattern', 'E1/E5', '''let id = 0; let label = ""; let others: { active: boolean } = { active: false };
({ id, nested: { label }, ...others } = { id: 2, nested: { label: "ok" }, active: true });
export const assignedId = id; export const assignedLabel = label; export const assignedOthers = others;
''', queries=[q('assignedId'), q('assignedLabel'), q('assignedOthers', 'properties')])
case('array-assignment-default-rest', 'E1/E5', '''let head = 0; let tail: number[] = [];
[head = 1, ...tail] = [2, 3, 4];
export const assignedHead = head; export const assignedTail = tail;
''', queries=[q('assignedHead'), q('assignedTail')])
case('assignment-pattern-invalid-targets', 'E1', '''const fixed = 1; let text = "";
({ value: fixed } = { value: 2 });
[text] = [42];
[...text] = [1, 2];
({ value: missing } = { value: 1 });
''')
case('object-spread-overwrite-contextual', 'E2', '''const base = { value: 1, enabled: true };
export const merged = { ...base, value: "ok", ...{ label: "ready" } };
export const fromMaybe = { ...({ extra: 1 } as { extra: number } | undefined), fixed: true };
interface RecordView { value: string; enabled: boolean; label: string; }
export const checked: RecordView = merged;
''', queries=[q('merged', 'properties'), q('fromMaybe', 'properties')])
case('object-spread-union-and-generic', 'E2', '''export function add<T extends object>(value: T) { return { ...value, ready: true as const }; }
declare const variant: { kind: "left"; left: number } | { kind: "right"; right: string };
export const unionCopy = { ...variant, tag: 1 };
export const genericCopy = add({ id: 1 });
''', queries=[q('unionCopy'), q('genericCopy', 'properties')])
case('spread-invalid-operands', 'E2', '''export const badObject = { ...42 };
export const badArray = [...{ value: 1 }];
function take(value: number) {} take(...["bad"]);
''')
case('array-call-spread-tuple-context', 'E2', '''function take(left: number, right: string, ...flags: boolean[]) { return { left, right, flags }; }
const pair: [number, string] = [1, "ok"];
export const called = take(...pair, true);
export const expanded = [0, ...pair, false] as const;
''', queries=[q('called', 'properties'), q('expanded')])
case('computed-shorthand-object-method', 'E2/E3', '''const label = "ready"; const key: unique symbol = Symbol();
export const record = { label, [key]: 2, ["count"]: 1, read() { return this.count; } };
export const observed = record.read();
''', queries=[q('record', 'properties'), q('observed')])
case('optional-property-element-chain', 'E2/E5', '''declare const source: { list?: { value: number }[] } | undefined;
export const value = source?.list?.[0]?.value;
export const defaulted = value ?? 0;
''', queries=[q('value'), q('defaulted')])
case('optional-call-overload-this', 'E2', '''interface Service { run(this: Service, input: number): string; }
declare const service: Service | undefined;
declare const callback: ((value: string) => number) | undefined;
export const result = service?.run(1);
export const count = callback?.("ok");
''', queries=[q('result'), q('count')])
case('optional-chain-invalid-assignment-call', 'E2', '''declare let model: { value: number; run: number } | undefined;
model?.value = 1;
model?.run();
export const absent = model?.missing;
''')
case('literals-bigint-regex-templates', 'E3', '''export const big = 123n; export const regex = /item(?<id>\\d+)/u;
export const plain = `fixed`; export const interpolated = `item-${big}`;
export const numeric = -big + 2n;
''', queries=[q('big'), q('regex'), q('plain'), q('interpolated'), q('numeric')])
case('literal-target-negative', 'E3', '''export const big = 1n; export const regex = /(?<id>x)/u;
''', {'target': 'ES5'})
case('tagged-template-contextual', 'E3', '''function tag(strings: TemplateStringsArray, ...values: number[]): string { return strings.join("") + values.join(""); }
export const tagged = tag`item-${1}-${2}`;
export const invalid = tag`item-${"bad"}`;
''', queries=[q('tagged')])
case('non-null-assertion-satisfies', 'E3', '''declare const maybe: { value: number } | undefined;
export const present = maybe!.value;
export const checked = { id: 1, label: "ok" } satisfies { id: number; label: string };
export const asserted = ("x" as string | number) as string;
export const invalid = { id: "bad" } satisfies { id: number };
''', queries=[q('present'), q('checked', 'properties'), q('asserted')])
case('delete-and-exact-optional', 'E3/E5', '''let model: { required: number; optional?: string } = { required: 1, optional: "x" };
delete model.optional;
export const afterDelete = model.optional;
delete model.required;
declare const readonlyModel: { readonly value?: number }; delete readonlyModel.value;
''', {'exactOptionalPropertyTypes': True}, queries=[q('afterDelete')])
case('in-instanceof-private-brand', 'E3/E5', '''class Box { #key = 1; value = "box"; static has(value: object): value is Box { return #key in value; } }
export function read(value: Box | { label: string }) {
 if ("label" in value) { const label = value.label; return label; }
 if (value instanceof Box) { const boxed = value.value; return boxed; }
 return "none";
}
export function branded(value: object) { if (Box.has(value)) { const boxedValue = value.value; return boxedValue; } return "none"; }
''', queries=[q('label'), q('boxed'), q('boxedValue'), q('read')])
case('operator-arithmetic-bitwise-comma', 'E3', '''let numberValue = 2; let bigValue = 2n;
export const arithmetic = numberValue ** 2 + 3; export const bits = numberValue << 1 | 4;
export const bigBits = bigValue & 1n; export const comparison = numberValue < 3;
export const commaValue = (numberValue += 1, numberValue);
export const invalid = bigValue + numberValue;
export const badArithmetic = "x" - 1;
''', queries=[q('arithmetic'), q('bits'), q('bigBits'), q('comparison'), q('commaValue')])
case('logical-assignment-and-short-circuit', 'E3/E5', '''let text: string | undefined; text ??= "ready";
export const initialized = text;
let count = 0; count ||= 1; count &&= 2;
export const assigned = count;
declare const maybe: string | undefined;
export const fallback = maybe || "default"; export const conjunction = maybe && maybe.length;
''', queries=[q('initialized'), q('assigned'), q('fallback'), q('conjunction')])
case('meta-properties-source', 'E3', '''export const url = import.meta.url;
export function Target() { const constructor = new.target; return constructor; }
new Target();
''', queries=[q('url'), q('constructor')])
case('meta-properties-invalid-context', 'E3', '''export const invalid = new.target;
export const metadata = import.meta;
''', {'module': 'CommonJS', 'moduleResolution': 'Node10'})
case('conditional-contextual-function-result', 'E2/E3', '''function pick(flag: boolean) { return flag ? { kind: "left" as const, value: 1 } : { kind: "right" as const, label: "x" }; }
export const selected = pick(true);
export const mapped = [1, 2].map(value => ({ doubled: value * 2 }));
''', queries=[q('pick'), q('selected'), q('mapped')])
case('for-in-key-and-for-of-binding', 'E1/E4', '''const dictionary = { left: 1, right: 2 };
for (const key in dictionary) { const observedKey = key; }
for (const [index, value] of ["a", "b"].entries()) { const observedIndex = index; const observedValue = value; }
export function keys<T extends object>(value: T) { for (const key in value) { const genericKey = key; } }
''', queries=[q('observedKey'), q('observedIndex'), q('observedValue'), q('genericKey')])
case('for-in-for-of-invalid-targets', 'E4', '''for (const [key] in { value: 1 }) {}
for (const value of 42) {}
for (const value: string of [1, 2]) {}
for (const key: number in { value: 1 }) {}
''')
case('switch-discriminants-exhaustive', 'E4/E5', '''type Result = { kind: "ok"; value: number } | { kind: "error"; reason: string };
export function read(result: Result) {
 switch (result.kind) {
  case "ok": { const success = result.value; return success; }
  case "error": { const failure = result.reason; return failure.length; }
  default: { const exhaustive: never = result; return exhaustive; }
 }
}
''', queries=[q('success'), q('failure'), q('read')])
case('switch-fallthrough-invalid-case', 'E4', '''export function read(value: 1 | 2) {
 switch (value) { case 1: value = 2; case 2: return value; case "bad": return 0; }
}
''', {'noFallthroughCasesInSwitch': True, 'noImplicitReturns': True})
case('labels-break-continue-and-loops', 'E4/E5', '''export function collect(values: (string | number)[]) {
 let result = "";
 outer: for (const value of values) {
  if (typeof value !== "string") continue outer;
  const narrowed = value;
  for (let index = 0; index < 2; index++) { if (index === 1) break outer; result += narrowed; }
 }
 let count = 0; while (count < 2) count++; do { count--; } while (count > 0);
 return result;
}
''', queries=[q('narrowed'), q('collect')])
case('labels-break-continue-invalid', 'E4', '''label: { continue label; }
break missing;
continue;
''')
case('try-catch-finally-unknown', 'E4/E5', '''export function run(input: string | undefined) {
 try { if (!input) throw new Error("missing"); const present = input; return present.length; }
 catch (error) { const caught = error; if (error instanceof Error) { const message = error.message; return message.length; } return 0; }
 finally { input = undefined; }
}
''', queries=[q('present'), q('caught'), q('message'), q('run')])
case('catch-binding-and-options', 'E1/E4', '''export function read() { try { throw { message: "x" }; } catch ({ message }) { const caughtMessage = message; return caughtMessage; } }
''', {'useUnknownInCatchVariables': False}, queries=[q('caughtMessage')])
case('try-finally-definite-assignment', 'E4/E5', '''export function result(flag: boolean) { let value: number;
 try { if (flag) throw new Error(); value = 1; } catch { value = 2; } finally { value = 3; }
 const assigned = value; return assigned;
}
''', queries=[q('assigned')])
case('return-unreachable-and-empty-statements', 'E4', '''export function missing(flag: boolean): number { if (flag) return 1; }
export function unreachable(): number { return 1; debugger; ; return 2; }
''', {'noImplicitReturns': True, 'allowUnreachableCode': False})
case('with-statement-source-error', 'E4', '''with ({ value: 1 }) { value; }
''')
case('flow-typeof-equality-branch-merge', 'E5', '''export function run(value: string | number | null) {
 if (typeof value === "string") { const textBranch = value; }
 else if (value !== null) { const numberBranch = value; }
 else { const nullBranch = value; }
 const merged = value; return merged;
}
''', queries=[q('textBranch'), q('numberBranch'), q('nullBranch'), q('merged')])
case('flow-alias-discriminant-destructuring', 'E1/E5', '''type Result = { kind: "ok"; payload: number } | { kind: "error"; payload: string };
export function run(result: Result) { const { kind, payload } = result;
 if (kind === "ok") { const successPayload = payload; return successPayload; }
 const errorPayload = payload; return errorPayload;
}
''', queries=[q('successPayload'), q('errorPayload')])
case('flow-optional-chain-containment', 'E2/E5', '''export function run(value: { label: string } | undefined) {
 if (value?.label) { const presentValue = value; const presentLabel = value.label; }
 if (value?.label === undefined) { const maybeValue = value; }
 return value?.label;
}
''', queries=[q('presentValue'), q('presentLabel'), q('maybeValue')])
case('flow-in-unlisted-property', 'E5', '''export function read(value: object | null) {
 if (value && "key" in value) { const present = value; const key = value.key; return key; }
 return undefined;
}
''', queries=[q('present'), q('key')])
case('flow-predicate-assertion-effects', 'E5', '''function isText(value: unknown): value is string { return typeof value === "string"; }
function assertText(value: unknown): asserts value is string { if (!isText(value)) throw new Error(); }
export function run(value: unknown) { if (isText(value)) { const guarded = value; }
 assertText(value); const asserted = value; return asserted.length;
}
''', queries=[q('guarded'), q('asserted')])
case('flow-assignment-invalidation', 'E5', '''export function run(value: string | number) {
 if (typeof value === "string") { const before = value; value = 1; const after = value; }
 const joined = value; return joined;
}
''', queries=[q('before'), q('after'), q('joined')])
case('flow-loop-fixed-point', 'E5', '''export function run(value: string | number, repeat: boolean) {
 while (repeat) { if (typeof value === "string") value = value.length; else value = "again"; const inside = value; repeat = false; }
 const afterLoop = value; return afterLoop;
}
''', queries=[q('inside'), q('afterLoop')])
case('flow-closure-and-constant-reference', 'E5', '''export function run(value: string | undefined) {
 if (!value) value = "fallback";
 const stable = value;
 const read = () => { const captured = stable; return captured.length; };
 return read();
}
''', queries=[q('captured'), q('read')])
case('flow-switch-bypass-and-never-return', 'E4/E5', '''function fail(): never { throw new Error(); }
export function read(value: "a" | "b" | undefined) {
 switch (value) { case "a": return 1; case "b": return 2; }
 if (value === undefined) fail();
 const unreachableValue = value; return unreachableValue;
}
''', queries=[q('unreachableValue')])
case('directives-no-unused-and-recovery', 'E1/E3/E4', '''// @ts-expect-error
const bad: number = "x";
// @ts-expect-error
const valid: number = 1;
export function unused({ left, right }: { left: number; right: number }) { const [head, tail] = [left, 2]; return head; }
const { value: } = { value: 1 };
''', {'noUnusedLocals': True, 'noUnusedParameters': True})

case('flow-private-brand-derived-origin', 'E3/E5', '''class Base { #value = 1; static has(value: object): value is Base { return #value in value; } }
class Derived extends Base { #own = "x"; }
export function read(value: object) { if (Base.has(value)) { const branded = value; return branded; } return undefined; }
''', queries=[q('branded')])
case('flow-custom-instanceof-predicate', 'E3/E5', '''class Numeric { static [Symbol.hasInstance](value: unknown): value is { value: number } { return typeof value === "object" && value !== null && "value" in value; } }
export function read(value: unknown) { if (value instanceof Numeric) { const observed = value; const field = value.value; return field; } return 0; }
''', queries=[q('observed'), q('field')])
case('optional-call-assertion-effect', 'E2/E5', '''declare const assertText: ((value: unknown) => asserts value is string) | undefined;
export function read(value: unknown) { assertText?.(value); const afterOptionalAssert = value; return afterOptionalAssert; }
''', queries=[q('afterOptionalAssert')])
case('binding-indexed-access-option-and-reassignment', 'E1/E5', '''let values: number[] = [1, 2];
export const [head, second = 0, ...tail] = values;
let label: string | undefined; ({ label } = {} as { label?: string });
if (label) { label = undefined; const afterAssignment = label; }
''', {'noUncheckedIndexedAccess': True}, queries=[q('head'), q('second'), q('tail'), q('afterAssignment')])
case('flow-switch-typeof-default-return', 'E4/E5', '''export function read(value: string | number | boolean) {
 switch (typeof value) { case "string": { const text = value; return text.length; } case "number": { const numberValue = value; return numberValue; } default: { const booleanValue = value; return booleanValue ? 1 : 0; } }
}
''', queries=[q('text'), q('numberValue'), q('booleanValue')])
case('regex-invalid-flags-and-target', 'E3', '''export const duplicate = /value/gg;
export const unknown = /value/x;
export const modern = /(?<name>x)/d;
''', {'target': 'ES5'})
case('meta-properties-valid-constructor', 'E3', '''export const metadata = import.meta;
export class Factory { constructor() { const actualConstructor = new.target; } }
''', queries=[q('metadata'), q('actualConstructor')])

case('flow-switch-true-predicate-narrowing', 'E4/E5', '''export function read(value: string | number | null) {
 switch (true) {
  case typeof value === "string": { const textValue = value; return textValue.length; }
  case typeof value === "number": { const numericValue = value; return numericValue; }
  default: { const nullValue = value; return nullValue; }
 }
}
''', queries=[q('textValue'), q('numericValue'), q('nullValue'), q('read')])
case('flow-exhaustive-switch-without-default', 'E4/E5', '''export function complete(value: "left" | "right") {
 switch (value) { case "left": return 1; case "right": return 2; }
 const exhausted = value; return exhausted;
}
export function incomplete(value: "left" | "right" | "other"): number {
 switch (value) { case "left": return 1; case "right": return 2; }
}
''', {'noImplicitReturns': True}, queries=[q('exhausted'), q('complete'), q('incomplete')])
case('public-binding-property-and-meta-symbols', 'E1/E3', '''export const { source: alias } = { source: 1 };
export const metadata = import.meta;
export class Factory { constructor() { const actualConstructor = new.target; } }
''', queries=[q('source', 'symbol'), q('alias'), q('meta', 'symbol'), q('meta'), q('target', 'symbol'), q('target')])

payload = ''.join(json.dumps(row, ensure_ascii=False, separators=(',', ':')) + '\n' for row in rows).encode()
source_names = ['internal/checker/checker.go', 'internal/checker/flow.go', 'internal/checker/grammarchecks.go',
                'internal/checker/relater.go', 'internal/checker/inference.go', 'internal/compiler/program.go']
provenance = dict(baseline='5efa4e5', upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',
    classification='authored-source-family-public-Program-semantic-inputs-not-original-test-bodies',
    cases=len(rows), requestsSha256=hashlib.sha256(payload).hexdigest(), domains=domains,
    sourceFiles={('upstream/TypeScript/tsc/' + name): hashlib.sha256((ROOT/'upstream/TypeScript/tsc'/name).read_bytes()).hexdigest() for name in source_names},
    observation='Genuine Program/config/options/libraries with complete diagnostics and public inferred/narrowed identifier occurrence types. No source text or diagnostic normalization.',
    exclusions='Finite source-family evidence, not exhaustive compiler tests; async/yield/for-await/using/dynamic import/JS/JSX/decorators/operational advanced type closure external under E1-E6.')
provenance_bytes = (json.dumps(provenance, indent=2) + '\n').encode()
if '--check' in sys.argv:
    assert (OUT/'requests-families.jsonl').read_bytes() == payload
    assert (OUT/'requests-families-provenance.json').read_bytes() == provenance_bytes
else:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/'requests-families.jsonl').write_bytes(payload)
    (OUT/'requests-families-provenance.json').write_bytes(provenance_bytes)
print(json.dumps({'cases': len(rows), 'requestsSha256': provenance['requestsSha256']}))
