import type { Envelope, EventName, EventPayloads } from "./model";

export function dispatch<K extends EventName>(kind: K, ...args: [payload: EventPayloads[K], ...tags: string[]]): Envelope<K> {
  const [payload, ...tags] = args;
  return { kind, payload, tags };
}

export function withAudit<T extends unknown[]>(...values: T): ["audit", ...T] {
  return ["audit", ...values];
}

export function assertCreated(value: unknown): asserts value is EventPayloads["orderCreated"] {
  if (typeof value !== "object" || value === null || !("id" in value) || !("quantity" in value)) {
    throw new Error("An order event must have id and quantity fields");
  }
  if (typeof value.id !== "string" || typeof value.quantity !== "number") {
    throw new Error("Invalid order event fields");
  }
}
