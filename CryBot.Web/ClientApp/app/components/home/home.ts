import { HttpClient, json } from 'aurelia-fetch-client';
import {IWallet} from "../../models/api/IWallet";
import {IWalletResponse} from "../../models/api/IWalletResponse";
import { inject } from 'aurelia-framework';
import { PushNotificationModel } from '../../models/PushNotificationModel';

@inject(HttpClient)
export class Home {
    public version = "";
    wallet: IWallet;
    http: HttpClient;
    pushIsSupported: boolean = 'serviceWorker' in navigator && 'PushManager' in window;
    vapidPublicKey: string = 'BHDbDuDNGUT_H8NWyI1Io_AXUsZ5aKoFcPSYgtUIQeEZz4e_xk-79h5QcorVC8Z7DlV8hfQWCBYPhiH_vduyxHc';

    constructor(http: HttpClient) {
        this.http = http;
        http.fetch('api/version')
            .then(result => result.text())
            .then(data => {
                this.version = data;
            });
        http.fetch('api/wallet')
            .then(result => result.json() as Promise<IWalletResponse>)
            .then(data => {
                if (data.isSuccessful)
                    this.wallet = data.wallet;
            });
    }

    private urlBase64ToUint8Array(base64String: string) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    subscribeToPushNotifications() {
        if (this.pushIsSupported) {           
            navigator.serviceWorker.ready
                .then(serviceWorkerRegistration => {
                    serviceWorkerRegistration.pushManager.getSubscription()
                        .then(subscription => {
                            if (subscription) {
                                // subscription present, no need to register subscription again
                                return;
                            }                            
                            return serviceWorkerRegistration.pushManager.subscribe({
                                userVisibleOnly: true,
                                applicationServerKey: this.urlBase64ToUint8Array(this.vapidPublicKey)
                            })
                                .then(subscription => {
                                    const rawKey = subscription.getKey ? subscription.getKey('p256dh') : '';
                                    const key = rawKey ? btoa(String.fromCharCode.apply(null, new Uint8Array(rawKey))) : '';
                                    const rawAuthSecret = subscription.getKey ? subscription.getKey('auth') : '';
                                    const authSecret = rawAuthSecret ? btoa(String.fromCharCode.apply(null, new Uint8Array(rawAuthSecret))) : '';
                                    const endpoint = subscription.endpoint;

                                    const pushNotificationSubscription= new PushNotificationModel(key, endpoint, authSecret);
                                    this.http.fetch('/api/notifications', {
                                        method: 'POST',
                                        body: json(pushNotificationSubscription)
                                    }).then(response => {
                                        if (response.ok) {
                                            console.log('Push notification registration created!');
                                        }
                                        else {
                                            console.log('Ooops something went wrong');
                                        }
                                    });
                                });
                        });
                }).catch(function(err) {
                    // registration failed :(
                    console.log('ServiceWorker registration failed: ', err);
                });
        }
    }
}