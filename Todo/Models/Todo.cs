namespace redis_api.Todo.Models;

public class Todo
{
    public Guid Id { get; set; }
    public string? Name { get; set; }

    public Todo(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public Todo(string id, string name)
    {
        var isGuid = Guid.TryParse(id, out var guidId);
        if(!isGuid)
            throw new ArgumentException("Invalid cache entry");

        Id = guidId;
        Name = name;
    }
}
