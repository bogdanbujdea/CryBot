using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace DemaSignal
{
    public static class SignalAnalyzer
    {
        [FunctionName("SignalAnalyzer")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                log.Info($"Starting at {DateTime.Now}");
                var bitmapAnalyzer = new BitmapAnalyzer();
                var signalType = await bitmapAnalyzer.GetLastSignal();
                await CheckSignalWithLast(signalType);
                var table = await GetSignalsTable();
                var insertOperation = TableOperation.Insert(new Signal { SignalType = signalType.ToString().ToLower(), Time = DateTime.UtcNow });
                await table.ExecuteAsync(insertOperation);
                log.Info($"Last signal at {DateTime.Now} is {signalType}");
            }
            catch (Exception e)
            {
                log.Info(e.ToString());
            }
        }

        private static async Task CheckSignalWithLast(SignalType signalType)
        {
            var table = await GetSignalsTable();
            var retrieve = TableOperation.Retrieve<Signal>("bittrex", "");
            await table.ExecuteAsync(retrieve);
            TableQuery<Signal> query = new TableQuery<Signal>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "bittrex"));
            var results = await table.ExecuteQuerySegmentedAsync(query, new TableContinuationToken());
            var lastResult = results.Results.OrderBy(o => o.Time).LastOrDefault();
            if (lastResult.SignalType != signalType.ToString().ToLower())
            {
                await new Mailman().SendMailAsync($"Signal got changed to {signalType}");
            }
        }

        private static async Task<CloudTable> GetSignalsTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("storageConnectionString"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference("signals");

            // Create the table if it doesn't exist.
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}
