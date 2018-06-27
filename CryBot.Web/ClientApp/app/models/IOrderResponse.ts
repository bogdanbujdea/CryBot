import { IOrder } from "./IOrder";

export interface IOrderResponse {
    errorMessage: string;
    isSuccessful: boolean;
    orders: IOrder[];
}