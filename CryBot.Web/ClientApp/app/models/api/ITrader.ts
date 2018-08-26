import { ITrade } from "./ITrade";
import { Ticker } from "./Ticker";
import { Budget } from "./Budget";

export interface ITrader {
    currentTicker: Ticker;
    trades: ITrade[];
    market: string;
    budget: Budget;
}