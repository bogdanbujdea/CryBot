import {ICoinBalance} from "./ICoinBalance";

export interface IWallet {
    coins: ICoinBalance[];
    bittrexBalance: ICoinBalance;
}