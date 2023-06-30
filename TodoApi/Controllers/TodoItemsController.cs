using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using TodoApi.Models;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private string _userName = "*";
    private readonly Task<RedisConnection> _redisConnectionFactory;
    private RedisConnection? _redisConnection;

    public TodoItemsController(TodoContext context, Task<RedisConnection> redisConnectionFactory)
    {
        _context = context;
        _redisConnectionFactory = redisConnectionFactory;
        if (User != null)
            _userName = User.Identity?.Name ?? "*";
        context.Database.EnsureCreated();
    }

    // GET: api/TodoItems
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TodoItemDTO>>> GetTodoItems()
    {
        List<TodoItemDTO> todoItems;
        // Get the cached items
        todoItems = await GetCache();
        // IF there are no cached items, get them from the database
        if (todoItems.Count == 0)
        {
            todoItems = await _context.TodoItems
                            .Select(x => ItemToDTO(x))
                            .WithPartitionKey(_userName)
                            .ToListAsync();

            // Set the cache
            await SetCache(todoItems);
            if (todoItems == null)
            {
                return NotFound();
            }
        }
        return Ok(todoItems);
    }

    // GET: api/TodoItems/5
    // <snippet_GetByID>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDTO>> GetTodoItem(long id)
    {
        var todoItems = await GetCache();
        TodoItemDTO? todoItem = null;
        if (todoItems.Count == 0)
        {
            todoItem = ItemToDTO(await _context.TodoItems.WithPartitionKey(_userName).FirstAsync(x => x.Id == id));
        }
        else
        {
            todoItem = todoItems.FirstOrDefault(x => x.Id == id);
        }

        if (todoItem == null)
        {
            return NotFound();
        }

        return Ok(todoItem);
    }
    // </snippet_GetByID>

    // PUT: api/TodoItems/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    // <snippet_Update>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutTodoItem(long id, TodoItemDTO todoDTO)
    {
        if (id != todoDTO.Id)
        {
            return BadRequest();
        }

        var todoItem = await _context.TodoItems.WithPartitionKey(_userName).FirstAsync(x => x.Id == id);
        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.Name = todoDTO.Name;
        todoItem.IsComplete = todoDTO.IsComplete;

        try
        {
            await _context.SaveChangesAsync();
            await ClearCache();
        }
        catch (DbUpdateConcurrencyException) when (!TodoItemExists(id))
        {
            return NotFound();
        }

        return NoContent();
    }
    // </snippet_Update>

    // POST: api/TodoItems
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    // <snippet_Create>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDTO>> PostTodoItem(TodoItemDTO todoDTO)
    {
        var todoItem = new TodoItem
        {
            Id = DateTime.Now.Ticks,
            IsComplete = todoDTO.IsComplete,
            Name = todoDTO.Name,
            UserName = _userName
        };

        _context.TodoItems.Add(todoItem);

        try
        {
            await _context.SaveChangesAsync();
            await ClearCache();
            return CreatedAtAction(
                nameof(GetTodoItem),
                new { id = todoItem.Id },
                ItemToDTO(todoItem));
        }
        catch
        {
            return NotFound();
        }
    }
    // </snippet_Create>

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItems.WithPartitionKey(_userName).FirstAsync(x => x.Id == id);
        if (todoItem == null)
        {
            return NotFound();
        }

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();
        await ClearCache();
        return NoContent();
    }

    private bool TodoItemExists(long id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }

    private static TodoItemDTO ItemToDTO(TodoItem todoItem) =>
       new TodoItemDTO
       {
           Id = todoItem.Id,
           Name = todoItem.Name,
           IsComplete = todoItem.IsComplete
       };

    // Clear Redis cache for user
    private async Task ClearCache()
    {
        _redisConnection = await _redisConnectionFactory;
        await _redisConnection.BasicRetryAsync(async (db) => await db.KeyDeleteAsync(_userName));
    }

    // Set Redis cache for user
    private async Task SetCache(List<TodoItemDTO> todoItems)
    {
        _redisConnection = await _redisConnectionFactory;
        await _redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync(_userName, JsonConvert.SerializeObject(todoItems)));
    }

    // Get Redis cache for user
    private async Task<List<TodoItemDTO>> GetCache()
    {
        _redisConnection = await _redisConnectionFactory;
        var todoItems = await _redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(_userName));
        return JsonConvert.DeserializeObject<List<TodoItemDTO>>(todoItems.ToString()) ?? new List<TodoItemDTO>();
    }
}