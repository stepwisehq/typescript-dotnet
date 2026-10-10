import { enumerate, loadJobs, run, streamJobs } from "./stream";
const seed = [{ id: "priority", weight: 3 }, { id: "normal", weight: 1 }];
export const sequence = enumerate(seed);
export const loaded = loadJobs(seed);
export const stream = streamJobs(seed);
export const result = run(seed);
export async function summary(): Promise<number> { return await result; }
