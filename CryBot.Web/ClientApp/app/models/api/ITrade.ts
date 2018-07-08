import { ICryptoOrder } from "./ICryptoOrder";

export interface ITrade {
    isActive: boolean;
    buyOrder: ICryptoOrder;
    sellOrder: ICryptoOrder;
    maxPricePerUnit: number;
    profit: number;
}