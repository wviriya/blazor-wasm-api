using Microsoft.AspNetCore.Mvc;
using TodoApi.Controllers;
using TodoApi.Models;
using TodoApiTest.Helpers;

namespace TodoApiTest;

public class TodoApiTests
{
[Fact]
    public async Task GetTodoReturnsNotFoundIfNotExists()
    {
        // Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);

        // Act
        var result = await Todo.GetTodoItem(1);

        //Assert
        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Null(result.Value);
     }
    // </snippet_>

    [Fact]
    public async Task GetAllReturnsTodosFromDatabase()
    {
        // Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);

        context.TodoItems.Add(new TodoItem
        {
            Id = 1,
            Name = "Test 1",
            IsComplete = false
        });

        context.TodoItems.Add(new TodoItem
        {
            Id = 2,
            Name = "Test 2",
            IsComplete = true
        });

        await context.SaveChangesAsync();

        // Act
        var result = await Todo.GetTodoItems();

        //Assert
        var objResult = Assert.IsType<OkObjectResult>(result.Result);
        var todoItems = Assert.IsAssignableFrom<IEnumerable<TodoItemDTO>>(objResult.Value);

        Assert.NotEmpty(todoItems);
        Assert.Collection(todoItems, item1 =>
        {
            Assert.Equal("Test 1", item1.Name);
            Assert.False(item1.IsComplete);
        }, item2 =>
        {
            Assert.Equal("Test 2", item2.Name);
            Assert.True(item2.IsComplete);
        });
    }

    // <snippet_1>
    [Fact]
    public async Task GetTodoReturnsTodoFromDatabase()
    {
        // Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);
        
        context.TodoItems.Add(new TodoItem
        {
            Id = 1,
            Name = "Test 1",
            IsComplete = false
        });

        await context.SaveChangesAsync();

        // Act
        var result = await Todo.GetTodoItem(1);

        //Assert
        var objResult = Assert.IsType<OkObjectResult>(result.Result);
        var foundTodo = Assert.IsAssignableFrom<TodoItemDTO>(objResult.Value);
        Assert.Equal(1, foundTodo.Id);
    }
    // </snippet_1>

    // <snippet_3>
    [Fact]
    public async Task CreateTodoCreatesTodoInDatabase()
    {
        //Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);

        var newTodo = new TodoItemDTO
        {
            Id = 1,
            Name = "Test 1",
            IsComplete = false
        };

        //Act
        var result = await Todo.PostTodoItem(newTodo);
        
        //Assert
        var objResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.IsAssignableFrom<TodoItemDTO>(objResult.Value);
        Assert.NotEmpty(context.TodoItems);
        Assert.Collection(context.TodoItems, item =>
        {
            Assert.Equal("Test 1", item.Name);
            Assert.False(item.IsComplete);
        });
    }
    // </snippet_3>

    [Fact]
    public async Task UpdateTodoUpdatesTodoInDatabase()
    {
        //Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);

        context.TodoItems.Add(new TodoItem
        {
            Id = 1,
            Name = "Exiting test title",
            IsComplete = false
        });

        await context.SaveChangesAsync();

        var updatedTodo = new TodoItemDTO
        {
            Id = 1,
            Name = "Updated test title",
            IsComplete = true
        };

        //Act
        var result = await Todo.PutTodoItem(updatedTodo.Id, updatedTodo);

        //Assert
        Assert.IsType<NoContentResult>(result);
     //    Assert.Equal(204, httpResult.StatusCode);
    }

    [Fact]
    public async Task DeleteTodoDeletesTodoInDatabase()
    {
        //Arrange
        await using var context = new MockDb().CreateDbContext();
        TodoItemsController Todo = new TodoItemsController(context);

        var existingTodo = new TodoItem
        {
            Id = 1,
            Name = "Exiting test title",
            IsComplete = false
        };

        context.TodoItems.Add(existingTodo);

        await context.SaveChangesAsync();

        //Act
        var result = await Todo.DeleteTodoItem(existingTodo.Id);

        //Assert
        Assert.IsType<NoContentResult>(result);
     //    Assert.Equal(204, httpResult.StatusCode);
        Assert.Empty(context.TodoItems);
    }
}
