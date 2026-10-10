import { fulfill } from "./service";
import { Inventory, Order } from "./model";
const inventory: Inventory = { available: "twelve", reserved: 0 };
const orders: Order[] = [{ kind: "purchase", id: "A-1", quantity: 3 }];
export const outcomes = fulfill(orders, inventory);
export const unsafeQuantity = orders[0].quantity;
const [head, ...tail, last] = outcomes;
export const invalidRest = { ...42 };
export const badResult: string = outcomes[0]?.kind === "accepted" ? outcomes[0].quantity : 0;
