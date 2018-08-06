import * as moment from 'moment';

export class DateFormatValueConverter {
    toView(start: any, end: any, tradeStatus: any, sellOrderDate: any) {
        if (tradeStatus === 5)
            end = sellOrderDate;
        var duration = moment.duration(moment(end).diff(start));
        var hours = duration.asHours();
        return Math.round(hours);
    }
}