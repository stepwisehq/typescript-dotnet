import { dispatch, withAudit } from "./client";
import type { EventName, EventPayloads, Handlers, WireName, WirePayload } from "./model";

export const badWire: WireName<EventName> = "event:ordercreated";
export const badPayload: WirePayload<"EVENT:ORDERCREATED"> = { id: 17, quantity: "two" };
export const missingHandlers: Handlers<EventPayloads> = {};
export const wrongEvent = dispatch("orderCancelled", { id: "A-17", quantity: 2 });
export const wrongAudit: ["audit", number] = withAudit("text");
