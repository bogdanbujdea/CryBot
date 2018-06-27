import {OrderType} from "./OrderType";

export interface IOrder {

    market: string;
    orderType: OrderType;
    price: number;
    commissionPaid: number;
    canceled: boolean;
    opened: Date;
    closed: Date;
    uuid: string;
    limit: number;
    quantity: number;
    quantityRemaining: number;
}