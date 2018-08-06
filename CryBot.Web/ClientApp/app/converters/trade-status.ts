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
            return "Bought";
        case 4:
            return "Selling";
        case 5:
            return "Completed";
        default:
        }
    }
}