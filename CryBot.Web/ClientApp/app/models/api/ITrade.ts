import { ICryptoOrder } from "./ICryptoOrder";
import { TradeStatus } from "../TradeStatus";

export interface ITrade {
    isActive: boolean;
    buyOrder: ICryptoOrder;
    sellOrder: ICryptoOrder;
    maxPricePerUnit: number;
    profit: number;
    status: TradeStatus;
}