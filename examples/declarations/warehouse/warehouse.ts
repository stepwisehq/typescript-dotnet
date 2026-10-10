import { Product, StockState } from "./domain";

export class Warehouse implements AuditRecord {
    constructor(public readonly warehouse: string, public readonly product: Product) {}

    get stockCount(): number { return this.product.count; }
    get state(): StockState {
        if (this.stockCount === 0) return StockState.Empty;
        return StockState.Available;
    }
}
