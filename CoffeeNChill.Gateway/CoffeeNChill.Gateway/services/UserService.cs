using BCrypt.Net;
using CoffeeNChill.Gateway.Models;
using Microsoft.Azure.Cosmos.Table;

namespace CoffeeNChill.Gateway.Services;

public class UserService
{
    private readonly CloudTable _usersTable;

    public UserService(CloudStorageAccount storageAccount)
    {
        var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        _usersTable = tableClient.GetTableReference("Users");
        _usersTable.CreateIfNotExists();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var query = new TableQuery<User>()
            .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, email.ToLowerInvariant()));

        var segment = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
        return segment.Results.FirstOrDefault();
    }

    public async Task<User> CreateUserAsync(string email, string password, string fullName, string role)
    {
        // Check if user exists
        var existing = await GetUserByEmailAsync(email);
        if (existing != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            PartitionKey = role,
            RowKey = email.ToLowerInvariant(),
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            CreatedDate = DateTime.UtcNow,
            Role = role
        };

        var insertOperation = TableOperation.Insert(user);
        await _usersTable.ExecuteAsync(insertOperation);
        return user;
    }

    public bool VerifyPassword(User user, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}