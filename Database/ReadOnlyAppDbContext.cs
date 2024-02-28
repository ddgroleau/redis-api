using Microsoft.EntityFrameworkCore;

namespace redis_api.Database
{
    public class ReadOnlyAppDbContext : AppDbContext
    {
        public override int SaveChanges()
        {
            throw new NotImplementedException("This database is read-only.");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellation = default)
        {
            throw new NotImplementedException("This database is read-only.");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer("Data Source=DANSMACHINE;Database=TodoFollower;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
    }
}
