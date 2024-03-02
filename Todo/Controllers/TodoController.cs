namespace redis_api.Todo.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Database;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

[Route("api/todos")]
[ApiController]
[Authorize]
public class TodoController(IDistributedCache _cache, AppDbContext _appContext, ReadOnlyAppDbContext _readOnlyAppContext) : ControllerBase
{
    private const int _maxRetryCount = 5;
    private const int _delayIntervalSeconds = 1;

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        var todo = new Todo(name);

        _appContext.Todos.Add(todo);
        await _appContext.SaveChangesAsync();

        await InvalidateCache(["all"]);
        await _cache.SetStringAsync(todo.Id.ToString(), name);

        await WaitForReadReplicaCreated(todo.Id);

        return Created(Request.GetDisplayUrl(), todo.Id.ToString());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Read(string id)
    {
        var isGuid = Guid.TryParse(id, out var guidId);
        if(!isGuid)
            return BadRequest();

        var value = await _cache.GetStringAsync(id);

        if(value != null)
            return Ok(new Todo(id, value.ToString()));

        var entity = await _readOnlyAppContext.Todos.FindAsync(guidId);
        if (entity != null)
        {
            await _cache.SetStringAsync(entity.Id.ToString(), entity.Name ?? "");
            return Ok(entity);
        }

        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> Read()
    {
        var allTodosJson = await _cache.GetStringAsync("all");
 
        var allTodos = JsonSerializer.Deserialize<IEnumerable<Todo>>(allTodosJson ?? "[]");

        if (allTodos?.Any() ?? false)
            return Ok(allTodos);

        var entities = await _readOnlyAppContext.Todos.AsNoTracking().ToListAsync();
        
        if (entities.Count > 0)
        {
            var json = JsonSerializer.Serialize(entities);
            await _cache.SetStringAsync("all",json);
            return Ok(entities);
        }

        return NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, string newName)
    {
        var isGuid = Guid.TryParse(id, out var guidId);
        if (!isGuid)
            return BadRequest();

        var entity = await _appContext.Todos.FindAsync(guidId);

        if (entity != null)
        {
            entity.Name = newName;
            await _appContext.SaveChangesAsync();

            await InvalidateCache(["all"]);
            await _cache.SetStringAsync(id, newName);
        }

        await WaitForReadReplicaUpdated("Name", newName, guidId);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var isGuid = Guid.TryParse(id, out var guidId);
        if(!isGuid)
            return BadRequest();

        var entity = await _appContext.Todos.FindAsync(guidId);

        if (entity != null)
        {
            _appContext.Todos.Remove(entity);
            await _appContext.SaveChangesAsync();
            await InvalidateCache(["all", id]);
        }

        await WaitForReadReplicaDeleted(guidId);

        return NoContent();
    }

    private async Task InvalidateCache(IEnumerable<string> keys)
    {
        var tasks = new List<Task>();
        foreach(string key in keys)
             tasks.Add(_cache.RemoveAsync(key));
        await Task.WhenAll(tasks);
    }

    private async Task WaitForReadReplicaCreated(Guid id)
    {
        Todo? readReplicaEntity = null;
        var retryCount = 0;

        while (readReplicaEntity is null && retryCount <= _maxRetryCount)
        {
            await Task.Delay(TimeSpan.FromSeconds(_delayIntervalSeconds));
            readReplicaEntity = await _readOnlyAppContext.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id.ToString() == id.ToString());
            retryCount++;
        }
    }

    private async Task WaitForReadReplicaUpdated(string propertyName, string value, Guid id)
    {
        bool isUpdated = false;
        var retryCount = 0;

        while (!isUpdated && retryCount <= _maxRetryCount)
        {
            await Task.Delay(TimeSpan.FromSeconds(_delayIntervalSeconds));
            var entity = await _readOnlyAppContext.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id.ToString() == id.ToString());
            var readValue = entity?.GetType()?.GetProperty(propertyName)?.GetValue(entity);
            isUpdated = readValue?.ToString() == value;
            retryCount++;
        }
    }

    private async Task WaitForReadReplicaDeleted(Guid id)
    {
        var readReplicaEntity = await _readOnlyAppContext.Todos.FindAsync(id);
        var retryCount = 0;

        while (readReplicaEntity != null && retryCount <= _maxRetryCount)
        {
            await Task.Delay(TimeSpan.FromSeconds(_delayIntervalSeconds));
            readReplicaEntity = await _readOnlyAppContext.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id.ToString() == id.ToString());
            retryCount++;
        }
    }

}
