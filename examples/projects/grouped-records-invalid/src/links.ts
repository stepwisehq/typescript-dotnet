import type { Row } from "./models";

// The model and this link form a genuine recursive type relationship across files.
export interface RecordLink {
    row: Row;
    next?: RecordLink;
}

export function link(row: Row): RecordLink {
    return { row };
}
