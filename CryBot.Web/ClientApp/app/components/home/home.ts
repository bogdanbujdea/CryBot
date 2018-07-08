import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import {IWallet} from "../../models/api/IWallet";
import {IWalletResponse} from "../../models/api/IWalletResponse";

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