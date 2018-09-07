using CryBot.Core.Storage;
using CryBot.Core.Exchange.Models;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Trader
{
    public interface ITradersManager
    {
        Task<List<TraderState>> GetAllTraders();

        Task CreateTraderAsync(string market);
        Task<Chart> GetChartAsync(string market);
    }

    public class Chart
    {
        public List<Candle> Candles { get; set; }
        public List<Trade> Trades { get; set; }
    }
}
