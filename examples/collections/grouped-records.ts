// Type checking only: group and summarize recursive records using the genuine ES5 library.
interface Row {
    name: string;
    score: number;
    active: boolean;
    next?: Row;
}

type Groups<T> = Record<string, T[]>;

function select<T extends { active: boolean }>(items: readonly T[]): T[] {
    return items.filter(item => item.active);
}

function group<T>(items: readonly T[], key: (value: T) => string): Groups<T> {
    const result: Groups<T> = {};
    for (const item of items) {
        const name = key(item);
        const bucket = result[name];
        if (bucket) {
            bucket.push(item);
        } else {
            result[name] = [item];
        }
    }
    return result;
}

function summarize(rows: readonly Row[]): [string[], number] {
    const names = select(rows).map(row => row.name);
    const total = rows.reduce((sum, row) => sum + row.score, 0);
    return [names, total];
}

function first<T>(...values: T[]): T | undefined {
    return values[0];
}

function sumRows(input: readonly Row[]): number {
    let total = 0;
    for (const row of input) {
        total = total + row.score;
    }
    return total;
}

function label(name: string, score: number): string {
    return name + score;
}

function tupleSummary(...pair: [string, number]): string {
    return pair[0] + pair[1];
}

const rows: Row[] = [
    { name: "Ada", score: 3, active: true },
    { name: "Lin", score: 4, active: false }
];
const grouped = group(rows, row => row.active ? "active" : "inactive");
const picked = grouped["active"].map(row => row.name);
const result = summarize(rows);
const head = first(...rows);
const loopTotal = sumRows(rows);
const pair: [string, number] = ["x", 1];
const text = label(...pair);
const tupleText = tupleSummary(...pair);

// View an existing record without writes and describe a typed partial update.
const view: Readonly<Row> = rows[0];
const patch: Partial<Row> = { score: 1 };
