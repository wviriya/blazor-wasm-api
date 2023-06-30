using Microsoft.EntityFrameworkCore;

namespace TodoApi.Models;

public class TodoContext : DbContext
{
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultContainer("TodoList");

        modelBuilder.Entity<TodoItem>()
            .ToContainer("TodoList");

        modelBuilder.Entity<TodoItem>()
            .HasNoDiscriminator();
        
        modelBuilder.Entity<TodoItem>()
            .HasPartitionKey(t => t.UserName);
    }

}