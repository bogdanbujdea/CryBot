import { ITrade } from "./ITrade";
import { Ticker } from "./Ticker";

export interface ITrader {
    ticker: Ticker;
    trades: ITrade[];
}