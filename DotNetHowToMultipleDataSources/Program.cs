using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;

namespace AzureSearch.SDKHowTo
{
    /// <summary>
    /// Demo of Azure Search indexer for Azure SQL
    /// </summary>
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            if (configuration["SearchServiceName"] == "Put your search service name here")
            {
                Console.Error.WriteLine("Specify SearchServiceName in appsettings.json");
                Environment.Exit(-1);
            }

            if (configuration["SearchServiceAdminApiKey"] == "Put your primary or secondary API key here")
            {
                Console.Error.WriteLine("Specify SearchServiceAdminApiKey in appsettings.json");
                Environment.Exit(-1);
            }

            if (configuration["AzureSQLConnectionString"] == "Put your Azure SQL database connection string here")
            {
                Console.Error.WriteLine("Specify AzureSQLConnectionString in appsettings.json");
                Environment.Exit(-1);
            }

            if (configuration["CosmosDBConnectionString"] == "Put your Cosmos DB connection string here")
            {
                Console.Error.WriteLine("Specify CosmosDBConnectionString in appsettings.json");
                Environment.Exit(-1);
            }

            SearchServiceClient searchService = new SearchServiceClient(
                searchServiceName: configuration["SearchServiceName"],
                credentials: new SearchCredentials(configuration["SearchServiceAdminApiKey"]));

            Console.WriteLine("Creating index...");
            Index index = new Index(
                name: "hotels",
                fields: FieldBuilder.BuildForType<Hotel>());
            // If we have run the sample before, this index will be populated
            // We can clear the index by deleting it if it exists and creating
            // it again
            bool exists = await searchService.Indexes.ExistsAsync(index.Name);
            if (exists)
            {
                await searchService.Indexes.DeleteAsync(index.Name);
            }

            await searchService.Indexes.CreateAsync(index);

            Console.WriteLine("Creating data sources...");

            // The SQL sample data set has a table name of "hotels"
            // The SQL sample data set uses SQL integrated change tracking for change detection
            // This means that when the SQL indexer runs, it will be able to detect which data has
            // changed since the last run using built in change tracking
            // See this link for more information
            // https://docs.microsoft.com/sql/relational-databases/track-changes/about-change-tracking-sql-server
            DataSource sqlDataSource = DataSource.AzureSql(
                name: "azure-sql",
                sqlConnectionString: configuration["AzureSQLConnectionString"],
                tableOrViewName: "hotels");
            sqlDataSource.DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy();
            // The SQL data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            await searchService.DataSources.CreateOrUpdateAsync(sqlDataSource);

            // The JSON sample data set has a collection name of "hotels"
            // The JSON sample data set uses Cosmos DB change tracking for change detection
            // This means that when the Cosmos DB indexer runs, it will be able to detect which data has
            // changed since the last run using built in change tracking
            // See this link for more information
            // https://docs.microsoft.com/azure/search/search-howto-index-cosmosdb#indexing-changed-documents
            DataSource cosmosDbDataSource = DataSource.DocumentDb(
                name: "cosmos-db",
                documentDbConnectionString: configuration["CosmosDBConnectionString"],
                collectionName: "hotels",
                useChangeDetection: true);
            // The Cosmos DB data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            await searchService.DataSources.CreateOrUpdateAsync(cosmosDbDataSource);

            Console.WriteLine("Creating Azure SQL indexer...");
            Indexer sqlIndexer = new Indexer(
                name: "azure-sql-indexer",
                dataSourceName: sqlDataSource.Name,
                targetIndexName: index.Name,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));
            // Indexers contain metadata about how much they have already indexed
            // If we already ran the sample, the indexer will remember that it already
            // indexed the sample data and not run again
            // To avoid this, reset the indexer if it exists
            exists = await searchService.Indexers.ExistsAsync(sqlIndexer.Name);
            if (exists)
            {
                await searchService.Indexers.ResetAsync(sqlIndexer.Name);
            }

            await searchService.Indexers.CreateOrUpdateAsync(sqlIndexer);

            Console.WriteLine("Creating Cosmos DB indexer...");
            Indexer cosmosDbIndexer = new Indexer(
                name: "cosmos-db-indexer",
                dataSourceName: cosmosDbDataSource.Name,
                targetIndexName: index.Name,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));

            // Indexers contain metadata about how much they have already indexed
            // If we already ran the sample, the indexer will remember that it already
            // indexed the sample data and not run again
            // To avoid this, reset the indexer if it exists
            exists = await searchService.Indexers.ExistsAsync(cosmosDbIndexer.Name);
            if (exists)
            {
                await searchService.Indexers.ResetAsync(cosmosDbIndexer.Name);
            }

            await searchService.Indexers.CreateOrUpdateAsync(cosmosDbIndexer);

            // We created two indexer with schedules, but we also
            // want to run them immediately
            Console.WriteLine("Running Azure SQL and Cosmos DB indexers...");

            try
            {
                await searchService.Indexers.RunAsync(sqlIndexer.Name);
                await searchService.Indexers.RunAsync(cosmosDbIndexer.Name);

            }
            catch (CloudException e) when (e.Response.StatusCode == (HttpStatusCode)429)
            {
                Console.WriteLine("Failed to run indexer: {0}", e.Response.Content);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
