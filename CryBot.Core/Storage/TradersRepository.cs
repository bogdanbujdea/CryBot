using CryBot.Core.Infrastructure;
using CryBot.Core.Exchange.Models;

using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryBot.Core.Storage
{
    public class TradersRepository : ITradersRepository
    {
        private readonly EnvironmentConfig _config;

        public TradersRepository(IOptions<EnvironmentConfig> options)
        {
            _config = options.Value;
        }

        public async Task<CryptoResponse<List<Market>>> GetTradedMarketsAsync()
        {
            try
            {
                var table = await GetTradersTable();
                var retrieve = TableOperation.Retrieve<Market>("bittrex", "");
                await table.ExecuteAsync(retrieve);
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

        private async Task<CloudTable> GetTradersTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config.StorageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(_config.TradersTable);

            // Create the table if it doesn't exist.
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}