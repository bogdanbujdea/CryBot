export class OrderFormatValueConverter {
    toView(o: number) {
        if (o === 1)
            return "BUY";
        return "SELL";
    }
}