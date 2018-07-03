import * as signalR from '@aspnet/signalr';
export class Traders {

    private connectionPromise?: Promise<void>;
    private chatHubConnection: signalR.HubConnection;

    tickerLog: Ticker[] = [];
    currentTicker: Ticker = new Ticker();

    constructor() {
        this.chatHubConnection = new signalR.HubConnectionBuilder().withUrl("/app").build();
        
        this.chatHubConnection.on('priceUpdate', (newTicker: Ticker) => {
            this.tickerLog.push(newTicker);
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

export class Ticker {
    market: string;
    last: number;
    ask: number;
    bid: number;
}