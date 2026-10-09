import { cycleB } from "./b";
export function cycleA(n: number): number { return n > 0 ? cycleB(n - 1) : 0; }
