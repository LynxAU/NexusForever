using NexusForever.Aspire.AppHost;
using NexusForever.Database;
using NexusForever.Network.Internal.Static;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var rmq = builder.AddRabbitMQ("rmq")
            .WithManagementPlugin();

        var mysql = builder.AddMySql("mysql")
            .WithPhpMyAdmin()
            .WithDataVolume("mysql-data");

        var authdb      = mysql.AddDatabase("authdb");
        var characterdb = mysql.AddDatabase("characterdb");
        var worlddb     = mysql.AddDatabase("worlddb");
        var groupdb     = mysql.AddDatabase("groupdb");
        var chatdb      = mysql.AddDatabase("chatdb");

        var dbMigration = builder.AddProject<Projects.NexusForever_Aspire_Database_Migrations>("database-migrations")
            .WithReference(authdb)
            .WithReference(characterdb)
            .WithReference(worlddb)
            .WithReference(groupdb)
            .WithReference(chatdb)
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitFor(worlddb)
            .WaitFor(groupdb)
            .WaitFor(chatdb);

        builder.AddProject<Projects.NexusForever_AuthServer>("auth-server")
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithReference(authdb)
            .WaitFor(authdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_StsServer>("sts-server")
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithReference(authdb)
            .WaitFor(authdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_WorldServer>("world-server")
            .WithUrl("http://localhost:5000/Console.html", "Web Console")
            .WithEnvironment("urls", "http://localhost:5000")
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithNexusForeverDatabase("Character", DatabaseProvider.MySql, characterdb.Resource)
            .WithNexusForeverDatabase("World", DatabaseProvider.MySql, worlddb.Resource)
            .WithNexusForeverMessageBroker("WorldServer_1", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithEnvironment("Realm:RealmId", "1")

            .WithReference(authdb)
            .WithReference(characterdb)
            .WithReference(worlddb)
            .WithReference(rmq)
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitFor(worlddb)
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration);

        var characterApi = builder.AddProject<Projects.NexusForever_API_Character>("character-api")
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithNexusForeverDatabase("Character:0", DatabaseProvider.MySql, characterdb.Resource)
            .WithEnvironment("Database:Character:0:RealmId", "1")
            .WithEnvironment(c =>
            {
                c.EnvironmentVariables["urls"] = c.EnvironmentVariables["ASPNETCORE_URLS"];
            })

            .WithReference(authdb)
            .WithReference(characterdb)
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_Server_GroupServer>("group-server")
            .WithNexusForeverDatabase("Group", DatabaseProvider.MySql, groupdb.Resource)
            .WithNexusForeverMessageBroker("GroupServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithEnvironment(c =>
            {
                c.EnvironmentVariables["API:Character:Host"] = characterApi.Resource.GetEndpoint("http").Url;
            })

            .WithReference(rmq)
            .WithReference(groupdb)
            .WaitFor(rmq)
            .WaitFor(groupdb)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_ChatServer>("chat-server")
            .WithNexusForeverDatabase("Chat", DatabaseProvider.MySql, chatdb.Resource)
            .WithNexusForeverMessageBroker("ChatServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithEnvironment(c =>
            {
                c.EnvironmentVariables["API:Character:Host"] = characterApi.Resource.GetEndpoint("http").Url;
            })

            .WithReference(rmq)
            .WithReference(chatdb)
            .WaitFor(rmq)
            .WaitFor(chatdb)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        DistributedApplication host = builder.Build();
        await host.RunAsync();
    }
}