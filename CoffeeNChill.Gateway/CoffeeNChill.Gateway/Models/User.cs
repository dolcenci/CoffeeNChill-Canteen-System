using Microsoft.Azure.Cosmos.Table;

namespace CoffeeNChill.Gateway.Models;

public class User : TableEntity
{
    public User()
    {
        PartitionKey = "Customer"; // Default role
        RowKey = Guid.NewGuid().ToString();
    }

    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string Role { get; set; } = "Customer";
}