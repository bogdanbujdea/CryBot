export class TradeStatusValueConverter {
    toView(status: number) {
        switch (status) {
        case 0:
            return "None";
        case 1:
            return "Empty";
        case 2:
            return "Buying";
        case 3:
            return "Canceled";
        case 4:
            return "Bought";
        case 5:
            return "Selling";
        case 6:
            return "Completed";
        default:
        }
    }
}