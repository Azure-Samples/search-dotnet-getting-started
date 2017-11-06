using System;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;

namespace AzureSearch.SDKHowTo
{
    /// <summary>
    /// Demo of Azure Search indexer for Azure SQL
    /// </summary>
    sealed class Program
    {
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            if (configuration["SearchServiceName"] == "Put your search service name here")
            {
                Console.Error.WriteLine("Specify SearchServiceName in appconfig.json");
                Environment.Exit(-1);
            }

            if (configuration["SearchServiceAdminApiKey"] == "Put your primary or secondary API key here")
            {
                Console.Error.WriteLine("Specify SearchServiceAdminApiKey in appconfig.json");
                Environment.Exit(-1);
            }

            if (configuration["AzureSqlConnectionString"] == "Put your Azure SQL database connection string here")
            {
                Console.Error.WriteLine("Specify AzureSqlConnectionString in appconfig.json");
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
            bool exists = searchService.Indexes.ExistsAsync(index.Name).GetAwaiter().GetResult();
            if (exists)
            {
                searchService.Indexes.DeleteAsync(index.Name).Wait();
            }
            searchService.Indexes.CreateAsync(index).Wait();

            Console.WriteLine("Creating data source...");

            // The sample data set has a table name of "hotels"
            // The sample data set table has a "soft delete" column named IsDeleted
            // When this column is set to true and the indexer sees it, it will remove the
            // corresponding document from the search service
            // See this link for more information
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.search.models.softdeletecolumndeletiondetectionpolicy
            // The sample data set uses Sql integrated change tracking for change detection
            // This means that when the indexer runs, it will be able to detect which data has
            // changed since the last run using built in change tracking
            // See this link for more information
            // https://docs.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-tracking-sql-server
            DataSource dataSource = DataSource.AzureSql(
                name: "azure-sql",
                sqlConnectionString: configuration["AzureSqlConnectionString"],
                tableOrViewName: "hotels",
                deletionDetectionPolicy: new SoftDeleteColumnDeletionDetectionPolicy(
                    softDeleteColumnName: "IsDeleted",
                    softDeleteMarkerValue: "true"));
            dataSource.DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy();
            // The data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            searchService.DataSources.CreateOrUpdateAsync(dataSource).Wait();

            Console.WriteLine("Creating Azure Sql indexer...");
            Indexer indexer = new Indexer(
                name: "azure-sql-indexer",
                dataSourceName: dataSource.Name,
                targetIndexName: index.Name,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));
            // Indexers contain metadata about how much they have already indexed
            // If we already ran the sample, the indexer will remember that it already
            // indexed the sample data and not run again
            // To avoid this, reset the indexer if it exists
            exists = searchService.Indexers.ExistsAsync(indexer.Name).GetAwaiter().GetResult();
            if (exists)
            {
                searchService.Indexers.ResetAsync(indexer.Name).Wait();
            }
            searchService.Indexers.CreateOrUpdateAsync(indexer).Wait();

            // We created the indexer with a schedule, but we also
            // want to run it immediately
            Console.WriteLine("Running Azure Sql indexer...");
            searchService.Indexers.RunAsync(indexer.Name).Wait();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}

