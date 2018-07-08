import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class Trader {
    trader: Trader;
    market: string = "BTC-ETC";
    private connectionPromise?: Promise<void>;
    private chatHubConnection: signalR.HubConnection;

    tickerLog: Ticker[] = [];
    currentTicker: Ticker = new Ticker();

    constructor(http: HttpClient) {
        this.chatHubConnection = new signalR.HubConnectionBuilder().withUrl("/app").build();
        
        this.chatHubConnection.on('traderUpdate:' + this.market, (trader: Trader) => {
            this.trader = trader;
        });
        this.chatHubConnection.on('priceUpdate:' + this.market, (newTicker: Ticker) => {
            console.log('hello');
            this.tickerLog.unshift(newTicker);            
        });
        http.fetch('api/traders?market=' + this.market)
            .then(result => result.json() as Promise<TraderResponse>)
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

export interface TraderResponse {
    errorMessage: string;
    isSuccessful: boolean;
    trader: Trader;
}

export class Ticker {
    market: string = "";
    last: number = 0;
    ask: number = 0;
    bid: number = 0;
}

export class CryptoOrder {
    market: string;
    orderType: CryptoOrderType;
    price: number;
    quantity: number;
    pricePerUnit: number;
    commissionPaid: number;
    canceled: boolean;
    uuid: string;
    opened: Date;
    limit: number;
    quantityRemaining: number;
    closed: Date;
    isClosed: boolean;
}

export enum CryptoOrderType {
    None,
    LimitBuy,
    LimitSell
}

export class Trade {
    isActive: boolean;
    buyOrder: CryptoOrder;
    sellOrder: CryptoOrder;
    maxPricePerUnit: number;
    profit: number;
}

export interface Trader {
    ticker: Ticker;
    trades: Trade[];
}