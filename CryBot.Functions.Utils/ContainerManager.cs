using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

using System;

namespace CryBot.Functions.Utils
{
    public class ContainerManager
    {
        public ContainerStatus GetStatus()
        {
            try
            {
                string resourceGroupName = "acicontainer";
                string containerGroupName = "chart-analyzer";
                IAzure azure = GetAzureContext(Environment.GetEnvironmentVariable("AzureAuthPath"));
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
}