import { TradeAdvice } from "./TradeAdvice";

export class Candle {
    market: string = "";
    open: number = 0;
    close: number = 0;
    high: number = 0;
    low: number = 0;
    timestamp: Date;
    emaAdvice: TradeAdvice;
}