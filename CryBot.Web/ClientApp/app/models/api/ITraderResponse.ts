import { ITrader } from "./ITrader";

export interface ITraderResponse {
    errorMessage: string;
    isSuccessful: boolean;
    trader: ITrader;
}