import { ITrader } from "./ITrader";

export interface ITradersResponse {
    errorMessage: string;
    isSuccessful: boolean;
    traders: ITrader[];
}