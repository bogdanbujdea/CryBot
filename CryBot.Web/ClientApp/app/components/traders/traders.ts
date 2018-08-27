import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import { ITrader } from "../../models/api/ITrader";
import { ITradersResponse } from "../../models/api/ITradersResponse";

@inject(HttpClient)
export class Traders {
    traders: ITrader[];
    totalProfit: number = 0;
    constructor(http: HttpClient) {

        http.fetch('api/traders')
            .then(result => result.json() as Promise<ITradersResponse>)
            .then(data => {
                if (data.isSuccessful) {
                    this.traders = data.traders;
                    var profit = 0;
                    for (var i = 0; i < data.traders.length; i++) {
                        profit += this.calculateProfitForTrader(data.traders[i]);
                    }
                    this.totalProfit = Math.round(profit * 100) / 100;
                }          
            });
    }

    
    calculateProfitForTrader(trader: ITrader): any {

        var profit = 0;
        trader.trades.forEach(t => {
            profit += t.profit;
        });
        profit = Math.round(profit * 100) / 100;
        return profit;
    }
}