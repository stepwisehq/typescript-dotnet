import type { RecordLink } from "./links";

export interface Row {
    name: string;
    score: number;
    active: boolean;
    next?: Row;
    link?: RecordLink;
}

export type Groups<T> = Record<string, T[]>;

export default interface Summary {
    names: string[];
    total: number;
    pair: [string, number];
}
