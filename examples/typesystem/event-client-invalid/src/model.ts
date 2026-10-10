export interface EventPayloads {
  orderCreated: { id: string; quantity: number };
  orderCancelled: { id: string; reason: string };
}

export type EventName = keyof EventPayloads;
export type WireName<K extends string> = `EVENT:${Uppercase<K>}`;
export type Handlers<P> = {
  [K in keyof P as K extends string ? `on${Capitalize<K>}` : never]: (payload: P[K]) => void;
};
export type WirePayload<N extends string> = {
  [K in EventName]: N extends WireName<K> ? EventPayloads[K] : never;
}[EventName];
export type Envelope<K extends EventName> = {
  kind: K;
  payload: EventPayloads[K];
  tags: string[];
};
