using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

using System;
using System.Net.Http;
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

        [FunctionName("SignalAnalyzer")]
        public static async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                Logger.Init(log);
                log.Info($"Starting at {DateTime.Now}");
                var httpClient = new HttpClient();
                var containerManager = new ContainerManager();
                var containerStatus = containerManager.GetStatus();
                var containerManagerUrl = Environment.GetEnvironmentVariable("containerManagerUrl");
                Logger.Log($"Status is {containerStatus}");
                if (containerStatus == ContainerStatus.Missing)
                {
                    await httpClient.GetAsync($"{containerManagerUrl}?action=start");
                }
                else if (containerStatus == ContainerStatus.Running)
                {
                    foreach (var marketChart in MarketCharts)
                    {
                        Logger.Log($"Retrieving signal for {marketChart.Market}");
                        var cryptoTrader = new CryptoTrader();
                        await cryptoTrader.RetrieveAndProcessSignal(Environment.GetEnvironmentVariable("containerUrl"), marketChart);
                    }
                    await httpClient.GetAsync($"{containerManagerUrl}?action=stop");
                }

                log.Info($"Finished at {DateTime.Now}");
            }
            catch (Exception e)
            {
                log.Info(e.ToString());
            }
        }
    }

    public class ContainerManager
    {
        public ContainerStatus GetStatus()
        {
            try
            {
                string resourceGroupName = "acicontainer";
                string containerGroupName = "chart-analyzer";
                IAzure azure = GetAzureContext(Environment.GetEnvironmentVariable("azureauth"));
                var containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);
                if (containerGroup == null)
                {
                    return ContainerStatus.Missing;
                }

                if (containerGroup.State != "Running")
                {
                    return ContainerStatus.Initializing;
                }
                return ContainerStatus.Running;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ContainerStatus.Unknown;
            }
        }

        private static IAzure GetAzureContext(string authFilePath)
        {
            IAzure azure;
            ISubscription sub;

            try
            {
                Logger.Log($"Authenticating with Azure using credentials in file at {authFilePath}");

                azure = Azure.Authenticate(authFilePath).WithDefaultSubscription();
                sub = azure.GetCurrentSubscription();

                Logger.Log($"Authenticated with subscription '{sub.DisplayName}' (ID: {sub.SubscriptionId})");
            }
            catch (Exception ex)
            {
                Logger.Log($"\nFailed to authenticate:\n{ex.Message}");

                if (String.IsNullOrEmpty(authFilePath))
                {
                    Logger.Log("Have you set the AZURE_AUTH_LOCATION environment variable?");
                }

                throw;
            }

            return azure;
        }

    }

    public enum ContainerStatus
    {
        Unknown,
        Missing,
        Running,
        Initializing
    }
}
