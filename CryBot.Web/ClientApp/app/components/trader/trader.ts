import * as signalR from '@aspnet/signalr';
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import { ITrader } from "../../models/api/ITrader";
import { ITraderResponse } from "../../models/api/ITraderResponse";
import { TraderChartModel } from "../../models/api/TraderChartModel";
import { Ticker } from '../../models/api/Ticker';
import { Chart } from "chart.js"
import * as moment from 'moment';
import { Candle } from "../../models/Candle";

@inject(HttpClient)
export class Trader {
    traderData: ITrader;
    profit: number = 0;
    market: string = "BTC-ETC";
    visible: boolean = false;
    myChart: Chart;
    chartData: TraderChartModel;
    myCanvas: HTMLCanvasElement;
    startIndex: number = 0;
    endIndex: number = 100;
    private connectionPromise?: Promise<void>;
    private chatHubConnection: signalR.HubConnection;

    tickerLog: Ticker[] = [];
    currentTicker: Ticker = new Ticker();

    constructor(http: HttpClient) {
        this.httpClient = http;
    }

    attached() {

        this.httpClient.fetch('api/traders/chart?market=' + this.market)
            .then(result => result.json() as Promise<TraderChartModel>)
            .then(chartData => {
                if (chartData) {

                    this.chartData = chartData;
                    let context = <CanvasRenderingContext2D>(this.myCanvas.getContext('2d'));
                    this.startIndex = 0;
                    let tempCandles =
                        chartData.candles.filter((u, i) => i > this.startIndex);
                    var tradeCandles = tempCandles.map(obj => ({...obj}));
                    tradeCandles.forEach(c => {
                        c.high = 0
                    }); 
                    let candles : {x: number, y: number, timestamp: Date }[] = [];
                    let i = 0;
                    tempCandles.forEach(c => {
                        i++;
                        candles.push({
                            x: i,
                            y: c.high,
                            timestamp: c.timestamp
                        });
                    });
                    this.myChart = new Chart(context, {
                        type: 'line',
                        data: {
                            labels: candles.map(c => moment(c.timestamp).format('lll')),
                            datasets: [ {
                                type: 'line',
                                label: 'Candles',
                                borderColor: 'green',
                                backgroundColor: 'green',
                                borderWidth: 1,
                                fill: true,
                                pointRadius: 0,
                                data: candles
                            }]
                        },
                        options: {
                            responsive: true,
                            title: {
                                display: true,
                                text: 'Chart ' + this.market
                            },
                            tooltips: {
                                mode: 'nearest',
                                intersect: true,
                            },
                            scales: {
                                xAxes: [{
                                    gridLines: {
                                        offsetGridLines: false,
                                    }
                                }, {
                                    id: 'x-axis-2',
                                    type: 'linear',
                                    position: 'bottom',
                                    display: false,
                                    ticks: {
                                        min: tempCandles.reduce((ya, u) => Math.min(ya, u.low), 1),
                                        max: tempCandles.reduce((ya, u) => Math.max(ya, u.high), 0),
                                    }
                                }],
                                yAxes: [{
                                    ticks: {
                                        min: tempCandles.reduce((ya, u) => Math.min(ya, u.low), 1),
                                        max: tempCandles.reduce((ya, u) => Math.max(ya, u.high), 0),
                                    }
                                }]
                            }
                        }
                    });
                }

            });
    }

    toggleVisibility() {
        this.visible = !this.visible;
    }

    previous() {

    }

    next() {
        let context = <CanvasRenderingContext2D>(this.myCanvas.getContext('2d'));
        this.startIndex += 15;
        this.endIndex += 15;
        let tempCandles = this.chartData.candles.filter((u, i) => i > this.startIndex).filter((u, i) => i < this.endIndex);
        let config = {
            type: 'line',
            data: {
                labels: tempCandles.map(c => moment(c.timestamp).format('lll')),
                datasets: [{
                    label: 'Open',
                    backgroundColor: 'blue',
                    borderColor: 'blue',
                    pointRadius: 1,
                    lineTension: 0,
                    borderWidth: 1,
                    data: tempCandles.map(c => c.open),
                    fill: false,
                }, {
                    label: 'Close',
                    backgroundColor: 'black',
                    borderColor: 'black',
                    pointRadius: 1,
                    lineTension: 0,
                    borderWidth: 1,
                    data: tempCandles.map(c => c.close),
                    fill: false,
                }, {
                    label: 'Low',
                    fill: false,
                    backgroundColor: 'red',
                    borderColor: 'red',
                    type: 'line',
                    pointRadius: 1,
                    lineTension: 0,
                    borderWidth: 1,
                    data: tempCandles.map(c => c.low)
                }, {
                    label: 'High',
                    backgroundColor: 'green',
                    borderColor: 'green',
                    pointRadius: 1,
                    lineTension: 0,
                    borderWidth: 1,
                    data: tempCandles.map(c => c.high),
                    fill: false,
                }]
            },
            options: {
                responsive: true,
                title: {
                    display: true,
                    text: 'Candles'
                },
                scales: {
                    yAxes: [{
                        ticks: {
                            min: tempCandles.reduce((ya, u) => Math.min(ya, u.low), 1),
                            max: tempCandles.reduce((ya, u) => Math.max(ya, u.high), 0),
                        }
                    }]
                }
            }
        };
        this.myChart = new Chart(context, config);
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