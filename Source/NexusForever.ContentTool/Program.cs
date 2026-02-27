using System.Diagnostics;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.World;
using NexusForever.Database.Configuration.Model;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.Shared;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace NexusForever.ContentTool
{
    internal class Program
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            ConfigureLogging();

            var parser = new Parser(with => with.HelpWriter = Console.Out);
            var result = parser.ParseArguments<Parameters>(args);

            await result.MapResult(
                async paramsObj => await RunTool(paramsObj),
                _ => Task.CompletedTask);
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss} [${level:uppercase=true}] ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;
        }

        private static async Task RunTool(Parameters paramsObj)
        {
            var services = new ServiceCollection();
            
            // Setup configuration
            services.Configure<GameTableConfig>(options => 
            {
                options.Path = paramsObj.TblPath ?? "tbl";
            });

            // The WorldContext needs a connection string. 
            // For the tool, we might want to read the existing SQL files instead of a live DB if we want to be "offline".
            // But usually these tools connect to the dev DB.
            // For now, let's assume a dummy connection if not provided.
            services.AddSingleton<IConnectionString>(new ConnectionString { Value = paramsObj.DbPath ?? "Server=localhost;Database=world;Uid=root;Pwd=;" });
            
            services.AddSingleton<IGameTableManager, GameTableManager>();
            services.AddTransient<WorldContext>();
            
            var serviceProvider = services.BuildServiceProvider();

            if (paramsObj.Extract)
            {
                log.Info("Extraction not yet implemented in ContentTool (use MapGenerator for now).");
            }

            if (paramsObj.Report || paramsObj.Seed)
            {
                log.Info("Initialising GameTables...");
                var gameTableManager = serviceProvider.GetRequiredService<IGameTableManager>();
                await gameTableManager.Initialise();

                var reporter = new Reporter(gameTableManager, serviceProvider.GetRequiredService<WorldContext>());
                
                if (paramsObj.Report)
                {
                    await reporter.GenerateReport();
                }

                if (paramsObj.Seed)
                {
                    await reporter.GenerateSeedData(paramsObj.OutputDir);
                }
            }
        }
    }

    internal class ConnectionString : IConnectionString
    {
        public string Value { get; set; } = "";
    }
}
