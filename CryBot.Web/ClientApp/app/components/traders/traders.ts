import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import { ITrader } from "../../models/api/ITrader";
import { ITradersResponse } from "../../models/api/ITradersResponse";
import {Trader} from "../trader/trader";

@inject(HttpClient)
export class Traders {
    traders: ITrader[];

    constructor(http: HttpClient) {

        http.fetch('api/traders')
            .then(result => result.json() as Promise<ITradersResponse>)
            .then(data => {
                if (data.isSuccessful) {
                    this.traders = data.traders;
                }          
            });
    }
}