import { groupRecords, type RecordValue } from "record-kit";
const values: RecordValue[] = [{ key: "a", value: 1 }];
export const grouped = groupRecords(values);
export const first = grouped[0].value;
export const incompatible: string = first;
