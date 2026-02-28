using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexusForever.Database.World;

namespace NexusForever.Aspire.Database.Migrations.Design
{
    public class WorldContextFactory : IDesignTimeDbContextFactory<WorldContext>
    {
        public WorldContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__worlddb")
                ?? "server=127.0.0.1;port=3306;user=nexusforever;password=nexusforever;database=nexus_forever_world";

            var options = new DbContextOptionsBuilder<WorldContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
                .Options;

            return new WorldContext(options);
        }
    }
}
