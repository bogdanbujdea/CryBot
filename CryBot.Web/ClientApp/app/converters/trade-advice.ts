export class TradeAdviceValueConverter {
    toView(status: number) {
        switch (status) {
        case 0:
            return "None";
        case 1:
            return "Hold";
        case 2:
            return "Buy";
        case 3:
            return "Sell";
        case 4:
            return "Cancel";
        default:
        }
    }
}