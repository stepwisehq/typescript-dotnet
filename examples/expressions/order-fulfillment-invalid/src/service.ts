import { Inventory, Order, Outcome, isPurchase } from "./model";

export function fulfill(orders: readonly Order[], stock: Inventory): Outcome[] {
  const outcomes: Outcome[] = [];
  const { available, ...previous } = stock;
  let remaining = available;
  orders: for (const [index, order] of orders.entries()) {
    if (index > 100) break orders;
    if (isPurchase(order) && order.quantity <= 0) {
      outcomes.push({ kind: "rejected", id: order.id, reason: "Quantity must be positive" });
      continue orders;
    }
    switch (order.kind) {
      case "purchase": {
        const { id, quantity, note = {} } = order;
        const label = note.text?.trim() ?? `Order ${id}`;
        if (quantity > remaining) {
          outcomes.push({ kind: "rejected", id, reason: "Insufficient stock" });
          break;
        }
        try {
          remaining -= quantity;
          outcomes.push({ kind: "accepted", id, quantity, label });
        } catch (error) {
          const reason = error instanceof Error ? error.message : "Unknown error";
          outcomes.push({ kind: "rejected", id, reason });
        } finally {
          stock.reserved = previous.reserved + available - remaining;
        }
        break;
      }
      case "cancel":
        outcomes.push({ kind: "rejected", id: order.id, reason: "Cancelled" });
        break;
      default: {
        const exhaustive: never = order;
        return exhaustive;
      }
    }
  }
  const updated = { ...stock, available: remaining };
  stock.available = updated.available;
  return [...outcomes];
}
