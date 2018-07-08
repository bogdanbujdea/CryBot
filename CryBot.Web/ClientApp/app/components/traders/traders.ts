import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import { ITrader } from "../../models/api/ITrader";
import { ITraderResponse } from "../../models/api/ITraderResponse";
import { Ticker } from '../../models/api/Ticker';

@inject(HttpClient)
export class Trader {
    trader: ITrader;
    market: string = "BTC-ETC";
    private connectionPromise?: Promise<void>;
    private chatHubConnection: signalR.HubConnection;

    tickerLog: Ticker[] = [];
    currentTicker: Ticker = new Ticker();

    constructor(http: HttpClient) {
        this.chatHubConnection = new signalR.HubConnectionBuilder().withUrl("/app").build();
        
        this.chatHubConnection.on('traderUpdate:' + this.market, (trader: ITrader) => {
            this.trader = trader;
        });
        this.chatHubConnection.on('priceUpdate:' + this.market, (newTicker: Ticker) => {
            console.log('hello');
            this.tickerLog.unshift(newTicker);            
        });
        http.fetch('api/traders?market=' + this.market)
            .then(result => result.json() as Promise<ITraderResponse>)
            .then(data => {
                if (data.isSuccessful)
                    this.trader = data.trader;
            });
    }

    activate() {
        this.connectionPromise = this.chatHubConnection.start();
    }

    deactivate() {
        this.connectionPromise = undefined;
        this.chatHubConnection.stop();
    }

    async sendMessage(): Promise<void> {
        if (!this.connectionPromise) {
            console.warn('Chat: No connection to the server.');
        }
        await this.connectionPromise;
        //this.chatHubConnection.invoke('sendMessage', this.currentTicker);
        //this.currentTicker.text = '';
    }
}