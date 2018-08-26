import * as moment from 'moment';

export class TimeAgoFormatValueConverter {
    toView(date: any) {
        if (date == '')
            return '';
        return moment(new Date(date)).fromNow();
    }
}