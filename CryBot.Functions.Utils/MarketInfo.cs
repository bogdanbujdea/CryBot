namespace CryBot.Functions.Utils
{
    public class MarketInfo
    {
        public MarketInfo(string market, string chartUrl, int quantity, int leverage, int round)
        {
            Market = market;
            ChartUrl = chartUrl;
            Quantity = quantity;
            Leverage = leverage;
            Round = round;
        }

        public int Round { get; set; }

        public string Market { get; set; }

        public string ChartUrl { get; set; }

        public int Quantity { get; set; }

        public int Leverage { get; set; }
    }
}