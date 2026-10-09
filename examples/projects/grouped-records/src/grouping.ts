import type { Row, Groups } from "./models";
import type Summary from "./models";

export function select<T extends { active: boolean }>(items: readonly T[]): T[] {
    return items.filter(item => item.active);
}

export function group<T>(items: readonly T[], key: (value: T) => string): Groups<T> {
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

export default function summarize(rows: readonly Row[]): Summary {
    const names = select(rows).map(row => row.name);
    const total = rows.reduce((sum, row) => sum + row.score, 0);
    return { names, total, pair: ["total", total] };
}

export function first<T>(...values: T[]): T | undefined {
    return values[0];
}

export function sumRows(input: readonly Row[]): number {
    let total = 0;
    for (const row of input) {
        total = total + row.score;
    }
    return total;
}

export function label(name: string, score: number): string {
    return name + score;
}

export function tupleSummary(...pair: [string, number]): string {
    return pair[0] + pair[1];
}
