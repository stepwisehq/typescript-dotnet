import { cycleA } from "./a";
export function cycleB(n: number): number { return n > 0 ? cycleA(n - 1) : 1; }
