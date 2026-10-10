import { fulfill } from "./service";
import { Inventory, Order } from "./model";
const inventory: Inventory = { available: 12, reserved: 0 };
const orders: Order[] = [
  { kind: "purchase", id: "A-1", quantity: 3, note: { text: "priority" } },
  { kind: "cancel", id: "A-2" }
];
export const outcomes = fulfill(orders, inventory);
export const firstLabel = outcomes[0]?.kind === "accepted" ? outcomes[0].label : "none";
