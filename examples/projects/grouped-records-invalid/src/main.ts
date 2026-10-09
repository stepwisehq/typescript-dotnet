import summarize, { groupRows, select, first, sumRows, label, tupleSummary, linked } from "./index";
import * as grouping from "./grouping";
import type { Row } from "./models";

const rows: Row[] = [
    { name: "Ada", score: 3, active: true },
    { name: "Lin", score: 4, active: false }
];
const grouped = groupRows(rows, row => row.active ? "active" : "inactive");
const picked = grouped["active"].map(row => row.name);
const direct = grouping.group(rows, row => row.name);
const result = summarize(rows);
const head = first(...rows);
const loopTotal = sumRows(rows);
const text = label(...result.pair);
const tupleText = tupleSummary(...result.pair);
const recordLink = linked.link(rows[0]);
rows[0].link = recordLink;

// Genuine ES5 utilities provide a read-only view and a typed partial update.
const view: Readonly<Row> = rows[0];
const patch: Partial<Row> = { score: 1 };

// Eight independent checks preserve the valid application's source above.
import { missing } from "./index";
import { absent } from "./unresolved";
const invalidConstraint = select([{ active: "yes" }]);
const invalidArgument = label("score", "wrong");
const invalidCallback = groupRows(rows, row => row.score);
const invalidReturn: string = sumRows(rows);
const invalidProperty = result.missing;
const invalidValue = Row;
