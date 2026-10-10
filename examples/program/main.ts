/// <reference path="./models.ts" />
const sample: ProgramDemo = { id: 1 };
const values: Model[] = [{ value: sample.id }];
const result = Promise.resolve(values);
