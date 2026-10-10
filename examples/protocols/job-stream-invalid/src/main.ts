import { Job } from "../../job-stream/src/model";
import { run } from "../../job-stream/src/stream";
export const result: Promise<string> = run([{ id: "priority", weight: 3 }]);
export function* wrongNext(): Generator<Job, string, number> {
  const received: string = yield { id: "job", weight: 1 };
  return 3;
}
export async function wrongReturn(): number { return 1; }
export async function wrongResource(): Promise<void> {
  using resource = { close(): void {} };
  await using lease = { [Symbol.asyncDispose](): number { return 1; } };
}
