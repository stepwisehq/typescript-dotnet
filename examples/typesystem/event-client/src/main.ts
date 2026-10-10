import { assertCreated, dispatch, withAudit } from "./client";
import type { EventName, EventPayloads, Handlers, WireName, WirePayload } from "./model";

export const handlers: Handlers<EventPayloads> = {
  onOrderCreated: payload => { payload.quantity.toFixed(); },
  onOrderCancelled: payload => { payload.reason.toUpperCase(); }
};
export const wire: WireName<EventName> = "EVENT:ORDERCREATED";
export const wirePayload: WirePayload<"EVENT:ORDERCREATED"> = { id: "A-17", quantity: 2 };
export const sent = dispatch("orderCreated", wirePayload, "warehouse", "confirmed");
export const audit = withAudit(sent.kind, sent.payload.quantity);

export function ingest(value: unknown) {
  assertCreated(value);
  return dispatch("orderCreated", value, "validated");
}
