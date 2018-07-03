import { Aurelia, PLATFORM } from 'aurelia-framework';
import { Router, RouterConfiguration } from 'aurelia-router';

export class App {
    router: Router;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = 'CryBot.Web';
        config.map([{
            route: [ '', 'home' ],
            name: 'home',
            settings: { icon: 'home' },
            moduleId: PLATFORM.moduleName('../home/home'),
            nav: true,
            title: 'Home'
        }, {
            route: 'counter',
            name: 'counter',
            settings: { icon: 'education' },
            moduleId: PLATFORM.moduleName('../counter/counter'),
            nav: true,
            title: 'Counter'
        }, {
            route: 'orders',
            name: 'orders',
            settings: { icon: 'th-list' },
            moduleId: PLATFORM.moduleName('../orders/orders'),
            nav: true,
            title: 'Orders'
        }, {
            route: 'traders',
            name: 'traders',
            settings: { icon: 'th-list' },
            moduleId: PLATFORM.moduleName('../traders/traders'),
            nav: true,
            title: 'Traders'
        }]);

        this.router = router;
    }
}
