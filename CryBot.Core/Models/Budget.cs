using CryBot.Core.Utilities;

namespace CryBot.Core.Models
{
    public class Budget
    {
        public decimal Available { get; set; }

        public decimal Earned { get; set; }

        public decimal Invested { get; set; }

        public decimal Profit { get; set; }

        public override string ToString()
        {
            return $"\t\t{Invested.RoundSatoshi()}\t\t{Available.RoundSatoshi()}\t\t{Earned.RoundSatoshi()}\t\t{Profit}%";
        }
    }
}