
namespace redis_api.Todo.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Database;
using System;

[Route("api/todos")]
[ApiController]
public class TodoController(IDistributedCache _cache, AppDbContext _appContext, ReadOnlyAppDbContext _readOnlyAppContext) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        var todo = new Todo(name);
        _appContext.Todos.Add(todo);
        await _appContext.SaveChangesAsync();
        await _cache.SetStringAsync(todo.Id.ToString(), name);
        return Created(Request.GetDisplayUrl(), todo.Id.ToString());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Read(string id)
    {
        var isGuid = Guid.TryParse(id, out var guidId);
        if(!isGuid)
            return BadRequest();

        var value = await _cache.GetStringAsync(id);

        // Cache Miss
        if(value == null)
        {
            var entity = await _readOnlyAppContext.Todos.FindAsync(guidId);
            if (entity != null)
            {
                await _cache.SetStringAsync(entity.Id.ToString(), entity.Name ?? "");
                return Ok(entity);
            }

            return NotFound();
        }

        return Ok(new Todo(id,value.ToString()));
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
            await _cache.SetStringAsync(id, newName);
        }

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
            await _cache.RemoveAsync(id);
        }

        return NoContent();
    }

}
