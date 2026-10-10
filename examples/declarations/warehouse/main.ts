import { Product, StockItem, StockState } from "./domain";
import { Warehouse } from "./warehouse";

const item = new Product("BK-001", 3, 12);
const warehouse = new Warehouse("north", item);
const record: AuditRecord = warehouse;
const stock: StockItem = item;
const empty = Product.create("BK-002");

export const inventoryValue: number = stock.price();
export const counted: number = record.stockCount + empty.count;
export const state: StockState = warehouse.state;
