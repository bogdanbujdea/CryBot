using CryBot.Functions.Utils;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using System;

namespace CryBot.Functions.ContainerTimer
{
    public static class ContainerStarter
    {
        [FunctionName("ContainerStarter")]
        public static void Run([TimerTrigger("0 */15 * * * *", RunOnStartup = true)]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                Logger.Init(log);
                log.Info($"Starting at {DateTime.Now}");

                var containerManager = new ContainerManager();
                var containerStatus = containerManager.GetStatus();
                Logger.Log($"Status is {containerStatus}");
                if (containerStatus == ContainerStatus.Missing)
                {
                    Logger.Log($"Starting container");
                    var azureContainerManager = new AzureContainerManager();
                    azureContainerManager.StartImageAnalyzer();
                }

                log.Info($"Finished at {DateTime.Now}");
            }
            catch (Exception e)
            {
                log.Info(e.ToString());
            }
        }
    }
}
