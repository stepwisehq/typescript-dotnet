export type Order =
  | { kind: "purchase"; id: string; quantity: number; note?: { text?: string } }
  | { kind: "cancel"; id: string };
export interface Inventory { available: number; reserved: number; }
export type Outcome =
  | { kind: "accepted"; id: string; quantity: number; label: string }
  | { kind: "rejected"; id: string; reason: string };
export function isPurchase(order: Order): order is Extract<Order, { kind: "purchase" }> {
  return order.kind === "purchase";
}
