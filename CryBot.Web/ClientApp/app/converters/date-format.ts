import * as moment from 'moment';

export class DateFormatValueConverter {
    toView(date: any) {
        return moment(date).format('lll');
    }
}