using NexusForever.Database;
using NexusForever.Network.Internal.Static;

namespace NexusForever.Aspire.AppHost
{
    public static class ResourceBuilderExtensions
    {
        public static IResourceBuilder<T> WithNexusForeverDatabase<T>(this IResourceBuilder<T> builder, string database, DatabaseProvider databaseProvider, IResourceWithConnectionString resource) where T : IResourceWithEnvironment
        {
            builder.WithEnvironment("Database:{database}:Provider", databaseProvider.ToString());
            builder.WithEnvironment(c =>
            {
                c.EnvironmentVariables[$"Database:{database}:ConnectionString"] = new ConnectionStringReference(resource, false);
            });
            return builder;
        }

        public static IResourceBuilder<T> WithNexusForeverMessageBroker<T>(this IResourceBuilder<T> builder, string inputQueue, BrokerProvider brokerProvider, IResourceWithConnectionString resource) where T : IResourceWithEnvironment
        {
            builder.WithEnvironment("Network:Internal:InputQueue", inputQueue);
            builder.WithEnvironment("Network:Internal:Broker", brokerProvider.ToString());
            builder.WithEnvironment(c =>
            {
                c.EnvironmentVariables["Network:Internal:ConnectionString"] = new ConnectionStringReference(resource, false);
            });
            return builder;
        }
    }
}
