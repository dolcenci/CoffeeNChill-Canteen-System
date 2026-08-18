using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services
{
    public class MenuItem : TableEntity
    {
        public MenuItem()
        {
            PartitionKey = "General";
            RowKey = Guid.NewGuid().ToString();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class MenuService
    {
        private readonly CloudTable _table;
        private readonly ILogger _logger;

        public MenuService(CloudStorageAccount storageAccount, ILogger logger)
        {
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _table = tableClient.GetTableReference("MenuItems");
            _table.CreateIfNotExists();
            _logger = logger;
        }

        public async Task<MenuItem> CreateMenuItem(MenuItem item)
        {
            try
            {
                var insertOperation = TableOperation.Insert(item);
                var result = await _table.ExecuteAsync(insertOperation);
                return result.Result as MenuItem;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating menu item: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MenuItem>> GetAllMenuItems()
        {
            var query = new TableQuery<MenuItem>();
            var results = new List<MenuItem>();
            TableContinuationToken continuationToken = null;

            do
            {
                var segment = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);
                results.AddRange(segment.Results);
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return results;
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategory(string category)
        {
            var query = new TableQuery<MenuItem>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, category));

            var results = new List<MenuItem>();
            TableContinuationToken continuationToken = null;

            do
            {
                var segment = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);
                results.AddRange(segment.Results);
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return results;
        }

        public async Task<MenuItem> UpdateMenuItem(string category, string id, double? price = null, bool? isAvailable = null)
        {
            var retrieveOperation = TableOperation.Retrieve<MenuItem>(category, id);
            var retrieveResult = await _table.ExecuteAsync(retrieveOperation);
            var existingItem = retrieveResult.Result as MenuItem;

            if (existingItem == null)
            {
                return null;
            }

            if (price.HasValue)
                existingItem.Price = price.Value;
            if (isAvailable.HasValue)
                existingItem.IsAvailable = isAvailable.Value;

            var replaceOperation = TableOperation.Replace(existingItem);
            var result = await _table.ExecuteAsync(replaceOperation);
            return result.Result as MenuItem;
        }

        public async Task<bool> DeleteMenuItem(string category, string id)
        {
            var retrieveOperation = TableOperation.Retrieve<MenuItem>(category, id);
            var retrieveResult = await _table.ExecuteAsync(retrieveOperation);
            var existingItem = retrieveResult.Result as MenuItem;

            if (existingItem == null)
            {
                return false;
            }

            var deleteOperation = TableOperation.Delete(existingItem);
            await _table.ExecuteAsync(deleteOperation);
            return true;
        }
    }
}