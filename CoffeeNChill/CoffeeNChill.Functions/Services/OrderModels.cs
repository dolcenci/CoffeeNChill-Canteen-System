using System;
using Microsoft.Azure.Cosmos.Table;

namespace CoffeeNChill.Functions.Services
{
    public class Order
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string[] SelectedItemSKUs { get; set; }
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
    }

    public class OrderEntity : TableEntity
    {
        public OrderEntity()
        {
            PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            RowKey = Guid.NewGuid().ToString();
        }

        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string SelectedItemSKUs { get; set; } // Stored as JSON string
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
        public string Status { get; set; } // Received, Preparing, Ready, Collected
        public DateTime StatusUpdatedAt { get; set; }
    }
}