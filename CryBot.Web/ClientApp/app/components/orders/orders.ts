import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class FetchOrders {
    public openOrders: IOrder[];

    constructor(http: HttpClient) {
        http.fetch('api/orders')
            .then(result => result.json() as Promise<IOrderResponse>)
            .then(data => {
                if (data.isSuccessful)
                    this.openOrders = data.orders;
            });
    }
}

interface IOrderResponse {
    errorMessage: string;
    isSuccessful: boolean;
    orders: IOrder[];
}

interface IOrder {

    market: string;
    orderType: OrderType;
    price: number;
    commissionPaid: number;
    canceled: boolean;
    opened: Date;
    closed: Date;
    uuid: string;
    limit: number;
    quantity: number;
    quantityRemaining: number;
}

enum OrderType {
    LimitBuy,
    LimitSell
}
