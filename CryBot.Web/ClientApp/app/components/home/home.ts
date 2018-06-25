import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class Home {
    public version = "";

    constructor(http: HttpClient) {
        http.fetch('api/version')
            .then(result => result.text())
            .then(data => {
                this.version = data;
            });
    }
}