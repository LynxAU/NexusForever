using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexusForever.Database.Character;

namespace NexusForever.Aspire.Database.Migrations.Design
{
    public class CharacterContextFactory : IDesignTimeDbContextFactory<CharacterContext>
    {
        public CharacterContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__characterdb")
                ?? "server=127.0.0.1;port=3306;user=nexusforever;password=nexusforever;database=nexus_forever_character";

            var options = new DbContextOptionsBuilder<CharacterContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
                .Options;

            return new CharacterContext(options);
        }
    }
}
