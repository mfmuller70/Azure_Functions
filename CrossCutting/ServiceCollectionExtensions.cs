using System;
using Eveneum;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting
{
    public static class ServiceCollectionExtensions
    {
        private const string DATABASE = "Churras";
        public static IServiceCollection AddEventStore(this IServiceCollection services) 
        {
            var client = new CosmosClient(Environment.GetEnvironmentVariable(nameof(EventStore)));

            client.CreateIfNotExists(DATABASE, "Bbqs").GetAwaiter().GetResult();
            client.CreateIfNotExists(DATABASE, "People").GetAwaiter().GetResult();
            client.CreateIfNotExists(DATABASE, "Lookups").GetAwaiter().GetResult();

            return services;
        }

        private async static Task CreateIfNotExists(this CosmosClient client, string database, string collection)
        {
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(database);
            await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(collection, "/StreamId"));
        }
    }
}
