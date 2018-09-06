import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import { ITrader } from "../../models/api/ITrader";
import { ITraderResponse } from "../../models/api/ITraderResponse";
import { Ticker } from '../../models/api/Ticker';
import { Chart } from "chart.js"

@inject(HttpClient)
export class Trader {
    traderData: ITrader;
    profit: number = 0;
    market: string = "BTC-ETC";
    visible: boolean = false;
    chart: Chart;
    myCanvas: HTMLCanvasElement;
    private connectionPromise?: Promise<void>;
    private chatHubConnection: signalR.HubConnection;

    tickerLog: Ticker[] = [];
    currentTicker: Ticker = new Ticker();

    constructor(http: HttpClient) {
        this.httpClient = http;
    }

    attached() {
        
        let context = <CanvasRenderingContext2D>(this.myCanvas.getContext('2d'));
        var data = {
            labels: ["January", "February", "March", "April", "May", "June", "July"],
            datasets: [
                {
                    label: "My First dataset",
                    fillColor: "rgba(220,220,220,0.2)",
                    strokeColor: "rgba(220,220,220,1)",
                    pointColor: "rgba(220,220,220,1)",
                    pointStrokeColor: "#fff",
                    pointHighlightFill: "#fff",
                    pointHighlightStroke: "rgba(220,220,220,1)",
                    data: [65, 59, 80, 81, 56, 55, 40]
                },
                {
                    label: "My Second dataset",
                    fillColor: "rgba(151,187,205,0.2)",
                    strokeColor: "rgba(151,187,205,1)",
                    pointColor: "rgba(151,187,205,1)",
                    pointStrokeColor: "#fff",
                    pointHighlightFill: "#fff",
                    pointHighlightStroke: "rgba(151,187,205,1)",
                    data: [28, 48, 40, 19, 86, 27, 90]
                }
            ]
        };
        let myChart = new Chart(context, {
            type: 'line',
            data: data,
            options: {
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }]
                }
            }
        });
    }

    toggleVisibility() {
        this.visible = !this.visible;
    }

    activate(traderData: ITrader) {
        this.traderData = traderData;
        this.market = this.traderData.market;
        this.chatHubConnection = new signalR.HubConnectionBuilder().withUrl("/app").build();

        this.chatHubConnection.on('traderUpdate:' + this.market, (trader: ITrader) => {
            if (trader) {
                this.traderData = trader;
                this.calculateProfit();
            }
        });

        this.chatHubConnection.on('priceUpdate:' + this.market, (newTicker: Ticker) => {
            this.traderData.currentTicker = newTicker;
            this.tickerLog.unshift(newTicker);
            this.calculateProfit();
        });

        this.httpClient.fetch('api/traders?market=' + this.market)
            .then(result => result.json() as Promise<ITraderResponse>)
            .then(data => {
                if (data.isSuccessful) {
                    this.traderData = data.trader;
                    this.traderData.currentTicker = data.trader.currentTicker;
                    this.calculateProfit();
                }

            });
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
    }

    httpClient: HttpClient;

    calculateProfit(): any {

        this.profit = 0;
        this.traderData.trades.forEach(t => {
            this.profit += t.profit;
        });
        this.profit = Math.round(this.profit * 100) / 100;
    }

}