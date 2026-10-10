import { Product, StockItem } from "../warehouse/domain";

new Product("BK-001", "three", 12);
const item = new Product("BK-001", 3, 12);
item.sku;
item.quantity;
item.count = "many";
const incompleteAudit: AuditRecord = { warehouse: "south" };
new StockItem("BK-003", 1);

export { incompleteAudit };
