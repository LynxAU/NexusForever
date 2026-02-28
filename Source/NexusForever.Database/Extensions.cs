using System;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Database
{
    public static class Extensions
    {
        // Keep EF provider setup independent from live DB reachability during server startup.
        private static readonly ServerVersion MySql80ServerVersion = new MySqlServerVersion(new Version(8, 0, 0));

        public static DbContextOptionsBuilder UseConfiguration(this DbContextOptionsBuilder optionsBuilder, IConnectionString connectionString)
        {
            switch (connectionString.Provider)
            {
                case DatabaseProvider.MySql:
                    optionsBuilder.UseMySql(connectionString.ConnectionString, MySql80ServerVersion, b =>
                    {
                        b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    break;
                default:
                    throw new NotSupportedException($"The requested database provider: {connectionString.Provider:G} is not supported.");
            }
            return optionsBuilder;
        }
    }
}
