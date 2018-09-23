using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.Http;

using System;

using System.Net;
using System.Linq;
using System.Net.Http;

namespace ContainerManager
{
    public static class ContainerFunction
    {
        [FunctionName("ContainerFunction")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                Logger.Init(log);
                AzureContainerManager azureContainerManager = new AzureContainerManager();
                var action = req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, "action", StringComparison.OrdinalIgnoreCase) == 0)
                    .Value;

                if (action == "start")
                {
                    azureContainerManager.StartImageAnalyzer();
                    return req.CreateResponse(HttpStatusCode.OK, "started");
                }

                if (action == "stop")
                {
                    azureContainerManager.StopImageAnalyzer();
                    return req.CreateResponse(HttpStatusCode.OK, "stopped");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return req.CreateResponse(HttpStatusCode.InternalServerError, e.ToString());
            }
            return req.CreateResponse(HttpStatusCode.OK, "invalid");
        }
    }
}
