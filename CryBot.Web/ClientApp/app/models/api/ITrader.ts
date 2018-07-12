import { ITrade } from "./ITrade";
import { Ticker } from "./Ticker";

export interface ITrader {
    currentTicker: Ticker;
    trades: ITrade[];
    market: string;
}