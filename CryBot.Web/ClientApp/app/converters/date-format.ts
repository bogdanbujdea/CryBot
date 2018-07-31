import * as moment from 'moment';

export class DateFormatValueConverter {
    toView(value: any) {
        return moment(value).fromNow();
    }
}