export enum StockState {
    Empty = 0,
    Available = 1,
    Reserved = 2
}

export abstract class StockItem {
    constructor(private readonly sku: string, protected quantity: number) {}

    get code(): string { return this.sku; }
    get count(): number { return this.quantity; }
    set count(value: number) { this.quantity = value; }

    abstract price(): number;
}

export class Product extends StockItem {
    constructor(sku: string, quantity: number, private readonly unitPrice: number) {
        super(sku, quantity);
    }

    override price(): number { return this.unitPrice * this.count; }
}

export namespace Product {
    export function create(sku: string): Product { return new Product(sku, 0, 0); }
}
