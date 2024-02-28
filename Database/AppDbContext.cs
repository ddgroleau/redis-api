namespace redis_api.Database;

using Todo.Models;
using Microsoft.EntityFrameworkCore;


public class AppDbContext : DbContext
{
    public DbSet<Todo> Todos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>().HasKey(t => t.Id);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer("Data Source=DANSMACHINE;Database=TodoLeader;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
}
