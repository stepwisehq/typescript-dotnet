"""Compact declaration-level overload obligations, independent actual-Go results."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CASES=[
('G3-mutable-reduction-overloads','''const values: number[] = [1, 2];
const noInitial = values.reduce((left, right) => left + right);
const sameInitial = values.reduce((left, right) => left + right, 0);
const explicit = values.reduce<string>((left, right) => left + right, "");
const inferred = values.reduce((left, right) => left + right, "");
const rightNoInitial = values.reduceRight((left, right) => left + right);
const rightSameInitial = values.reduceRight((left, right) => left + right, 0);
const rightExplicit = values.reduceRight<string>((left, right) => left + right, "");
const rightInferred = values.reduceRight((left, right) => left + right, "");
'''),
('G3-readonly-reduction-overloads','''const values: readonly number[] = [1, 2];
const noInitial = values.reduce((left, right) => left + right);
const sameInitial = values.reduce((left, right) => left + right, 0);
const explicit = values.reduce<string>((left, right) => left + right, "");
const inferred = values.reduce((left, right) => left + right, "");
const rightNoInitial = values.reduceRight((left, right) => left + right);
const rightSameInitial = values.reduceRight((left, right) => left + right, 0);
const rightExplicit = values.reduceRight<string>((left, right) => left + right, "");
const rightInferred = values.reduceRight((left, right) => left + right, "");
'''),
('G3-concat-splice-array-call-overloads','''const values: number[] = [1, 2];
const readonlyValues: readonly number[] = values;
const concatArrays = values.concat([3], readonlyValues);
const concatValues = values.concat(3, [4]);
const readonlyConcatArrays = readonlyValues.concat([3], values);
const readonlyConcatValues = readonlyValues.concat(3, [4]);
const optionalDelete = values.splice(1);
const explicitDelete = values.splice(0, 1);
const insertItems = values.splice(0, 1, 3, 4);
const optionalLength = Array();
const sized = Array(2);
const explicitSized = Array<number>(2);
const items = Array("a", "b");
const explicitItems = Array<number>(1, 2);
'''),
('G3-filter-predicate-overloads','''const values: (number | string)[] = [1, "a"];
const readonlyValues: readonly (number | string)[] = values;
const explicit = values.filter((value): value is number => typeof value === "number");
const inferred = values.filter(value => typeof value === "number");
const readonlyExplicit = readonlyValues.filter((value): value is string => typeof value === "string");
const readonlyInferred = readonlyValues.filter(value => typeof value === "string");
const wrong = values.filter((value: boolean): value is true => value);
const readonlyWrong = readonlyValues.filter((value: boolean): value is true => value);
''')]
if __name__=='__main__':
 out=ROOT/'validation/collections-corpus/overload-requests.jsonl'
 out.write_text(''.join(json.dumps({'name':name,'profile':'Collections','fileName':'/main.ts','text':text,'storedObservations':False},separators=(',',':'))+'\n' for name,text in CASES))
