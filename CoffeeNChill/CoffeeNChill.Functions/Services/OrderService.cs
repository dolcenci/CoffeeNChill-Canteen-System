using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services
{
    public class OrderService
    {
        private readonly CloudTable _orderTable;
        private readonly QueueClient _queueClient;
        private readonly ILogger _logger;

        public OrderService(string connectionString, string queueName, string tableName, ILogger logger)
        {
            // Setup Queue
            var queueServiceClient = new QueueServiceClient(connectionString);
            _queueClient = queueServiceClient.GetQueueClient(queueName);
            _queueClient.CreateIfNotExists();

            // Setup Table
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _orderTable = tableClient.GetTableReference(tableName);
            _orderTable.CreateIfNotExists();

            _logger = logger;
        }

        public async Task<string> AddOrderToQueueAsync(Order order)
        {
            try
            {
                // Generate OrderId if not provided
                if (string.IsNullOrEmpty(order.OrderId))
                {
                    order.OrderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                }

                // Serialize order to JSON
                var jsonString = JsonSerializer.Serialize(order);
                var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

                // Send to queue
                await _queueClient.SendMessageAsync(base64String);

                _logger.LogInformation($"Order {order.OrderId} added to queue successfully");
                return order.OrderId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding order to queue: {ex.Message}");
                throw;
            }
        }

        public async Task<OrderEntity> ProcessOrderFromQueueAsync(string messageText)
        {
            try
            {
                // Deserialize the order
                var order = JsonSerializer.Deserialize<Order>(messageText);

                if (order == null)
                {
                    throw new Exception("Failed to deserialize order");
                }

                // Create OrderEntity for table storage
                var orderEntity = new OrderEntity
                {
                    OrderId = order.OrderId,
                    CustomerName = order.CustomerName,
                    SelectedItemSKUs = JsonSerializer.Serialize(order.SelectedItemSKUs),
                    TotalPrice = order.TotalPrice,
                    OrderTimestamp = order.OrderTimestamp,
                    Status = "Received",
                    StatusUpdatedAt = DateTime.UtcNow
                };

                // Insert into table
                var insertOperation = TableOperation.Insert(orderEntity);
                await _orderTable.ExecuteAsync(insertOperation);

                _logger.LogInformation($"Order {order.OrderId} processed and saved with status: Received");
                return orderEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order from queue: {ex.Message}");
                throw;
            }
        }

        public async Task<OrderEntity> UpdateOrderStatusAsync(string orderId, string status)
        {
            try
            {
                // Query for the order
                var query = new TableQuery<OrderEntity>()
                    .Where(TableQuery.GenerateFilterCondition("OrderId", QueryComparisons.Equal, orderId));

                var segment = await _orderTable.ExecuteQuerySegmentedAsync(query, null);
                var orderEntity = segment.Results.FirstOrDefault();

                if (orderEntity == null)
                {
                    throw new Exception($"Order {orderId} not found");
                }

                // Update status
                orderEntity.Status = status;
                orderEntity.StatusUpdatedAt = DateTime.UtcNow;

                var replaceOperation = TableOperation.Replace(orderEntity);
                await _orderTable.ExecuteAsync(replaceOperation);

                _logger.LogInformation($"Order {orderId} status updated to: {status}");
                return orderEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");
                throw;
            }
        }
       
        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var orders = new List<OrderEntity>();
            var query = new TableQuery<OrderEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                var segment = await _orderTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                orders.AddRange(segment.Results);
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return orders;
        }
    }
}