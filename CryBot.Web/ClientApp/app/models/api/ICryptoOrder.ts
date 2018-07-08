import { CryptoOrderType } from "../CryptoOrderType";

export interface ICryptoOrder {
    market: string;
    orderType: CryptoOrderType;
    price: number;
    quantity: number;
    pricePerUnit: number;
    commissionPaid: number;
    canceled: boolean;
    uuid: string;
    opened: Date;
    limit: number;
    quantityRemaining: number;
    closed: Date;
    isClosed: boolean;
}