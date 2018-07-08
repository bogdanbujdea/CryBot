import {IWallet} from "./IWallet";

export interface IWalletResponse {

    isSuccessful: boolean;

    wallet: IWallet;
}