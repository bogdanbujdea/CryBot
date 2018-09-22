using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DemaSignal
{
    public static class SignalAnalyzer
    {
        private static readonly List<MarketInfo> MarketCharts = new List<MarketInfo>
        {
            new MarketInfo("XBTUSD", "https://www.tradingview.com/chart/WpYk6xkq/", 1500, 50),
            //new MarketInfo("XRPU18", "https://www.tradingview.com/chart/S1CVQjyz/", 1500, 50),
            //new MarketInfo("ETHXBT", "https://www.tradingview.com/chart/RkkgaHHm/", 1500, 50)
        };

        private static readonly AzureContainerManager AzureContainerManager = new AzureContainerManager();

        [FunctionName("SignalAnalyzer")]
        public static async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo myTimer, TraceWriter log)
        {
            try
            {                
                Logger.Init(log);
                log.Info($"Starting at {DateTime.Now}");
                var containerUrl = AzureContainerManager.StartImageAnalyzer();

                foreach (var marketChart in MarketCharts)
                {
                    var cryptoTrader = new CryptoTrader();
                    await cryptoTrader.RetrieveAndProcessSignal(containerUrl, marketChart);
                }

                AzureContainerManager.StopImageAnalyzer();

                log.Info($"Finished at {DateTime.Now}");
            }
            catch (Exception e)
            {
                log.Info(e.ToString());
            }
        }
    }
}
