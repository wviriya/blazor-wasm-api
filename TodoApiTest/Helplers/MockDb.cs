using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApiTest.Helpers;

// Use In-memory provider for a mock database
// https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#in-memory-provider
// https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy
public class MockDb : IDbContextFactory<TodoContext>
{
    public TodoContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TodoContext>()
            .UseInMemoryDatabase($"InMemoryTestDb-{DateTime.Now.ToFileTimeUtc()}")
            .Options;

        return new TodoContext(options);
    }
}