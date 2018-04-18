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
            DataSource dataSource = DataSource.AzureSql(
                name: "azure-sql",
                sqlConnectionString: configuration["AzureSQLConnectionString"],
                tableOrViewName: "hotels",
                deletionDetectionPolicy: new SoftDeleteColumnDeletionDetectionPolicy(
                    softDeleteColumnName: "IsDeleted",
                    softDeleteMarkerValue: "true"));
            dataSource.DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy();
            // The data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            await searchService.DataSources.CreateOrUpdateAsync(dataSource);

            Console.WriteLine("Creating Azure SQL indexer...");
            Indexer indexer = new Indexer(
                name: "azure-sql-indexer",
                dataSourceName: dataSource.Name,
                targetIndexName: index.Name,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));
            // Indexers contain metadata about how much they have already indexed
            // If we already ran the sample, the indexer will remember that it already
            // indexed the sample data and not run again
            // To avoid this, reset the indexer if it exists
            exists = await searchService.Indexers.ExistsAsync(indexer.Name);
            if (exists)
            {
                await searchService.Indexers.ResetAsync(indexer.Name);
            }

            await searchService.Indexers.CreateOrUpdateAsync(indexer);

            // We created the indexer with a schedule, but we also
            // want to run it immediately
            Console.WriteLine("Running Azure SQL indexer...");

            try
            {
                await searchService.Indexers.RunAsync(indexer.Name);
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

