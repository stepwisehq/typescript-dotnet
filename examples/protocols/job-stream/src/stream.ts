import { Job, Session, AsyncSession } from "./model";
export async function loadJobs(seed: readonly Job[]): Promise<Job[]> {
  return [...seed];
}
export function* enumerate(jobs: Iterable<Job>): Generator<Job, number, void> {
  let accepted = 0;
  for (const job of jobs) { yield job; accepted++; }
  return accepted;
}
export async function* streamJobs(seed: readonly Job[]): AsyncGenerator<Job, void, void> {
  for (const job of await loadJobs(seed)) { yield job; }
}
export async function total(jobs: AsyncIterable<Job>): Promise<number> {
  let weight = 0;
  for await (const job of jobs) { weight += job.weight; }
  return weight;
}
export async function run(seed: readonly Job[]): Promise<number> {
  using session = new Session();
  await using lease = new AsyncSession();
  return await total(streamJobs(seed));
}
