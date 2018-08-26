import * as moment from 'moment';

export class ValueFormatValueConverter {
    toView(value: any) {
        return value >= 0 ? 'positive' : 'negative';
    }
}