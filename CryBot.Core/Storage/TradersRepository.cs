using CryBot.Core.Exchange.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryBot.Core.Storage
{
    public class TradersRepository : ITradersRepository
    {
        public async Task<CryptoResponse<List<Market>>> GetTradedMarketsAsync()
        {
            try
            {
                var table = await GetTradersTable();
                var retrieve = TableOperation.Retrieve<Market>("bittrex", "");
                var result = await table.ExecuteAsync(retrieve);
                TableQuery<Market> query = new TableQuery<Market>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "bittrex"));
                var results = await table.ExecuteQuerySegmentedAsync(query, new TableContinuationToken());
                return new CryptoResponse<List<Market>>(results.Results);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new CryptoResponse<List<Market>>(e.Message);
            }
        }

        public async Task CreateTraderAsync(Market market)
        {
            try
            {
                var table = await GetTradersTable();
                var insertOperation = TableOperation.Insert(market);
                await table.ExecuteAsync(insertOperation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<CloudTable> GetTradersTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                "DefaultEndpointsProtocol=https;AccountName=crybot;AccountKey=tw+TB3korYKRw5QeHiD16wgy1H1DAKHEswWFuPcjWxnbMqn1OWjolaS0nScbOIfhC/OkCBYHCxBji1Bdq+arsg==;EndpointSuffix=core.windows.net");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference("traders");

            // Create the table if it doesn't exist.
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}