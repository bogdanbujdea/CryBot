import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';
import {IOrderResponse} from "../../models/IOrderResponse";
import {IOrder} from "../../models/IOrder";

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