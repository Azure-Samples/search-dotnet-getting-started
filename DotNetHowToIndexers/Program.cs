using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
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

            if (configuration["SearchServiceEndPoint"] == "Put your search service endpoint here")
            {
                Console.Error.WriteLine("Specify SearchServiceEndPoint in appsettings.json");
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

            SearchIndexClient indexClient = new SearchIndexClient(new Uri(configuration["SearchServiceEndPoint"]), new AzureKeyCredential(configuration["SearchServiceAdminApiKey"]));
            SearchIndexerClient indexerClient = new SearchIndexerClient(new Uri(configuration["SearchServiceEndPoint"]), new AzureKeyCredential(configuration["SearchServiceAdminApiKey"]));

            Console.WriteLine("Creating index...");
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Hotel));
            var searchIndex = new SearchIndex("hotels", searchFields);

            // If we have run the sample before, this index will be populated
            // We can clear the index by deleting it if it exists and creating
            // it again
            CleanupSearchIndexClientResources(indexClient, searchIndex);

            indexClient.CreateOrUpdateIndex(searchIndex);

            Console.WriteLine("Creating data source...");

            // The sample data set has a table name of "hotels"
            // The sample data set table has a "soft delete" column named IsDeleted
            // When this column is set to true and the indexer sees it, it will remove the
            // corresponding document from the search service
            // See this link for more information
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.search.models.softdeletecolumndeletiondetectionpolicy
            // The sample data set uses SQL integrated change tracking for change detection
            // This means that when the indexer runs, it will be able to detect which data has
            // changed since the last run using built in change tracking
            // See this link for more information
            // https://docs.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-tracking-sql-server
            var dataSource =
                new SearchIndexerDataSourceConnection(
                    "azure-sql",
                    SearchIndexerDataSourceType.AzureSql,
                    configuration["AzureSQLConnectionString"],
                    new SearchIndexerDataContainer("hotels"));

            // The data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            indexerClient.CreateDataSourceConnection(dataSource);

            Console.WriteLine("Creating Azure SQL indexer...");
            var indexer = new SearchIndexer("azure-sql", dataSource.Name, searchIndex.Name)
            {
                Description = "Data indexer",
            };

            // Indexers contain metadata about how much they have already indexed
            // If we already ran the sample, the indexer will remember that it already
            // indexed the sample data and not run again
            // To avoid this, reset the indexer if it exists
            CleanupSearchIndexerClientResources(indexerClient, indexer);

            indexerClient.CreateIndexer(indexer);

            // We created the indexer with a schedule, but we also
            // want to run it immediately
            Console.WriteLine("Running Azure SQL indexer...");

            try
            {
                await indexerClient.RunIndexerAsync(indexer.Name);
            }
            catch (CloudException e) when (e.Response.StatusCode == (HttpStatusCode)429)
            {
                Console.WriteLine("Failed to run indexer: {0}", e.Response.Content);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static void CleanupSearchIndexClientResources(SearchIndexClient indexClient, SearchIndex index)
        {
            try
            {
                if (indexClient.GetIndex(index.Name) != null)
                {
                    indexClient.DeleteIndex(index.Name);
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                //if exception occurred and status is "Not Found", this is work as expect
                Console.WriteLine("Failed to find index and this is because it's not there.");
            }
        }

        private static void CleanupSearchIndexerClientResources(SearchIndexerClient indexerClient, SearchIndexer indexer)
        {
            try
            {
                if (indexerClient.GetIndexer(indexer.Name) != null)
                {
                    indexerClient.ResetIndexer(indexer.Name);
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                //if exception occurred and status is "Not Found", this is work as expect
                Console.WriteLine("Failed to find indexer and this is because it's not there.");
            }
        }
    }
}
