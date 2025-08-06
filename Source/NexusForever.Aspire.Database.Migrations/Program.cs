using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusForever.Aspire.Database.Migrations.Service;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Chat;
using NexusForever.Database.Group;
using NexusForever.Database.World;

namespace NexusForever.Aspire.Database.Migrations
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddEnvironmentVariables();
                })
                .ConfigureServices((hb, sc) =>
                {
                    sc.AddHostedService<DatabaseMigrationHostedService>();
                    sc.AddHostedService<FinishHostedService>();

                    sc.AddScoped(sp =>
                    {
                        var options = sp.GetService<DbContextOptions<AuthContext>>();
                        return new AuthContext(options);
                    });
                    sc.AddScoped(sp =>
                    {
                        var options = sp.GetService<DbContextOptions<CharacterContext>>();
                        return new CharacterContext(options);
                    });
                    sc.AddScoped(sp =>
                    {
                        var options = sp.GetService<DbContextOptions<WorldContext>>();
                        return new WorldContext(options);
                    });

                    sc.AddDbContext<AuthContext>(options =>
                    {
                        var connectionString = hb.Configuration.GetConnectionString("authdb");
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                    sc.AddDbContext<CharacterContext>(options =>
                    {
                        var connectionString = hb.Configuration.GetConnectionString("characterdb");
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                    sc.AddDbContext<WorldContext>(options =>
                    {
                        var connectionString = hb.Configuration.GetConnectionString("worlddb");
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                    sc.AddDbContext<GroupContext>(options =>
                    {
                        var connectionString = hb.Configuration.GetConnectionString("groupdb");
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                    sc.AddDbContext<ChatContext>(options =>
                    {
                        var connectionString = hb.Configuration.GetConnectionString("chatdb");
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                });

            IHost host = builder.Build();
            await host.RunAsync();
        }
    }
}
