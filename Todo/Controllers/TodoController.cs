
namespace redis_api.Todo.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Distributed;

[Route("api/todos")]
[ApiController]
public class TodoController(IDistributedCache cache) : ControllerBase
{
    private readonly IDistributedCache _cache = cache;

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        var todo = new Todo(name);
        await _cache.SetStringAsync(todo.Id.ToString(), name);
        return Created(Request.GetDisplayUrl(), todo.Id.ToString());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Read(string id)
    {
        var isGuid = Guid.TryParse(id, out _);
        if(!isGuid)
            return BadRequest();

        var value = await _cache.GetStringAsync(id);

        if(value == null)
            return NotFound();

        return Ok(new Todo(id,value.ToString()));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, string newName)
    {
        var isGuid = Guid.TryParse(id, out _);
        if(!isGuid)
            return BadRequest();

        await _cache.SetStringAsync(id, newName);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var isGuid = Guid.TryParse(id, out _);
        if(!isGuid)
            return BadRequest();

        await _cache.RemoveAsync(id);
        return NoContent();
    }

}
