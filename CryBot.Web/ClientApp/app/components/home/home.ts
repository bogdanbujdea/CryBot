import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import {IWalletResponse} from "../../models/IWalletResponse";
import {IWallet} from "../../models/IWallet";

@inject(HttpClient)
export class Home {
    public version = "";
    wallet: IWallet;

    constructor(http: HttpClient) {
        http.fetch('api/version')
            .then(result => result.text())
            .then(data => {
                this.version = data;
            });
        http.fetch('api/wallet')
            .then(result => result.json() as Promise<IWalletResponse>)
            .then(data => {
                if (data.isSuccessful)
                    this.wallet = data.wallet;
            });
    }

}