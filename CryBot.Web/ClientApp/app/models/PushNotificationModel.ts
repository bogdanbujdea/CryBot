export class PushNotificationModel {
    endpoint: string;
    key: string;
    authSecret: string;

    constructor(key: string, endpoint: string, authSecret: string ) {
        this.key = key;
        this.endpoint = endpoint;
        this.authSecret = authSecret;
    }
}