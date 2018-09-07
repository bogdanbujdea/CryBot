import { ITrade } from "./ITrade";
import { Candle } from "../Candle";

export class TraderChartModel {
    candles: Candle[] = [];
    trades: ITrade[] = [];
}