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
import { TradeAdvice } from "../../models/TradeAdvice";
import * as TradeStatus from "../../models/TradeStatus";

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
                    this.loadChart("all");
                }

            });
    }

    toggleVisibility() {
        this.visible = !this.visible;
    }

    lastMonth() {
        this.loadChart("month");
    }

    lastDay() {
        this.loadChart("24");
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

    loadChart(period: string): any {
        let context = <CanvasRenderingContext2D>(this.myCanvas.getContext('2d'));
        this.startIndex = 0;
        if (period == "all")
            this.startIndex = 0;
        else if (period == "24")
            this.startIndex = this.chartData.candles.length - 24;
        else
            this.startIndex = this.chartData.candles.length - 168;
        let tempCandles =
            this.chartData.candles.filter((u, i) => i > this.startIndex);
        let candles: { price: number, timestamp: Date, tradeAdvice: TradeAdvice }[] = [];
        let hold: { price: number, tradeAdvice: TradeAdvice, timestamp: Date }[] = [];
        let sell: { price: number, tradeAdvice: TradeAdvice, timestamp: Date }[] = [];
        let buy: { price: number, tradeAdvice: TradeAdvice, timestamp: Date }[] = [];
        let buyOrders: { price: number, timestamp: Date, tradeAdvice: TradeAdvice }[] = [];
        let sellOrders: { price: number, timestamp: Date, tradeAdvice: TradeAdvice }[] = [];
        let i = 0;
        let buyOrderIndex: number = 0;
        let sellOrderIndex: number = 0;
        tempCandles.forEach(c => {
            candles.push({
                price: c.high,
                timestamp: c.timestamp,
                tradeAdvice: c.emaAdvice
            });
            buyOrders.push({
                price: c.high,
                timestamp: c.timestamp,
                tradeAdvice: c.emaAdvice
            });
            sellOrders.push({
                price: c.high,
                timestamp: c.timestamp,
                tradeAdvice: c.emaAdvice
            });
            buyOrders[buyOrders.length - 1].price = 0;
            sellOrders[sellOrders.length - 1].price = 0;
            let buyTrade = this.chartData.trades[buyOrderIndex];
            if (buyOrderIndex < this.chartData.trades.length &&
                buyTrade.buyOrder.closed < c.timestamp) {
                candles.push({
                    price: buyTrade.buyOrder.pricePerUnit,
                    timestamp: buyTrade.buyOrder.closed,
                    tradeAdvice: c.emaAdvice
                });
                buyOrderIndex++;
                buyOrders.push(candles[candles.length - 1]);
            }
            let sellTrade = this.chartData.trades[sellOrderIndex];
            if (sellOrderIndex < this.chartData.trades.length &&
                sellTrade.sellOrder.closed < c.timestamp && sellTrade.status == TradeStatus.TradeStatus.Completed) {
                candles.push({
                    price: sellTrade.sellOrder.pricePerUnit,
                    timestamp: sellTrade.sellOrder.closed,
                    tradeAdvice: c.emaAdvice
                });
                sellOrderIndex++;
                sellOrders.push(candles[candles.length - 1]);
            }
        });
        
        candles.forEach(c => {

            hold.push({
                price: c.price,
                tradeAdvice: c.tradeAdvice,
                timestamp: c.timestamp
            });
            sell.push({
                price: c.price,
                tradeAdvice: c.tradeAdvice,
                timestamp: c.timestamp
            });
            buy.push({
                price: c.price,
                tradeAdvice: c.tradeAdvice,
                timestamp: c.timestamp
            });
            if (c.tradeAdvice != TradeAdvice.Buy) {
                buy[i].price = 0;
            }
            if (c.tradeAdvice == TradeAdvice.Buy) {
                hold[i].price = 0;
                sell[i].price = 0;
            }
            if (c.tradeAdvice == TradeAdvice.Sell) {
                buy[i].price = 0;
                hold[i].price = 0;
            }
            if (c.tradeAdvice == TradeAdvice.Hold) {
                buy[i].price = 0;
                sell[i].price = 0;
            }
            i++;
        });
        this.myChart = new Chart(context, {
            type: 'line',
            data: {
                labels: candles.map(c => moment(c.timestamp).format('lll')),
                datasets: [{
                    type: 'line',
                    label: 'Candles',
                    backgroundColor: 'orange',
                    pointStyle: 'circle',
                    borderColor: 'orange',
                    borderWidth: 1,
                    fill: false,
                    pointRadius: 1,
                    data: candles.map(c => c.price)
                }, {
                    type: 'line',
                    label: 'Hold',
                    backgroundColor: 'blue',
                    pointStyle: 'circle',
                    borderColor: 'blue',
                    borderWidth: 1,
                    showLine: false,
                    hidden: true,
                    fill: false,
                    pointRadius: 1,
                    data: hold.map(c => c.price)
                }, {
                    type: 'line',
                    label: 'Sell',
                    backgroundColor: 'pink',
                    pointStyle: 'circle',
                    borderColor: 'pink',
                    borderWidth: 1,
                    hidden: true,
                    showLine: false,
                    fill: false,
                    pointRadius: 3,
                    data: sell.map(c => c.price)
                }, {
                    type: 'line',
                    label: 'Buy',
                    backgroundColor: 'green',
                    pointStyle: 'circle',
                    borderColor: 'green',
                    borderWidth: 1,
                    hidden: true,
                    showLine: false,
                    fill: false,
                    pointRadius: 3,
                    data: buy.map(c => c.price)
                }, {
                    type: 'line',
                    label: 'Buy orders',
                    backgroundColor: 'blue',
                    pointStyle: 'triangle',
                    borderColor: 'blue',
                    borderWidth: 1,
                    hidden: false,
                    showLine: false,
                    fill: false,
                    pointRadius: 5,
                    data: buyOrders.map(c => c.price)
                }, {
                    type: 'line',
                    label: 'Sell orders',
                    backgroundColor: 'red',
                    pointStyle: 'triangle',
                    borderColor: 'red',
                    borderWidth: 1,
                    hidden: false,
                    showLine: false,
                    fill: false,
                    pointRadius: 5,
                    data: sellOrders.map(c => c.price)
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
                            min: candles.reduce((ya, u) => Math.min(ya, u.price), 1),
                            max: candles.reduce((ya, u) => Math.max(ya, u.price), 0),
                        }
                    }],
                    yAxes: [{
                        ticks: {
                            min: candles.reduce((ya, u) => Math.min(ya, u.price), 1),
                            max: candles.reduce((ya, u) => Math.max(ya, u.price), 0),
                        }
                    }]
                }
            }
        });
    }
}