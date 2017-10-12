using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Spatial;

namespace AzureSearch.SDKHowTo
{
    /// <summary>
    /// Demo of Azure Search indexers for Azure SQL, Azure Table Storage, and Azure Cosmos DB.
    /// </summary>
    sealed class Program
    {
        private const string ConfigFilePath = "appsettings.json";

        private const string AzureSqlOption = "AzureSQL";
        private const string AzureTableStorageOption = "AzureTableStorage";
        private const string AzureCosmosDbOption = "AzureCosmosDB";

        private const string IndexName = "hotels";

        private const string AzureSqlConnectionStringProperty = "AzureSqlConnectionString";
        private const string AzureStorageConnectionStringProperty = "AzureStorageConnectionString";
        private const string AzureStorageTableNameProperty = "AzureStorageTableName";
        private const string AzureCosmosDbConnectionStringProperty = "AzureCosmosDbConnectionString";
        private const string AzureCosmosDbCollectionNameProperty = "AzureCosmosDbCollectionName";

        private const string AzureSqlTableName = "hotels";
        private const string AzureSqlHighWaterMarkColumnName = "RowVersion";
        private const string AzureSqlDataSourceName = "azure-sql";
        private const string AzureSqlIndexerName = "azure-sql-indexer";

        private const string AzureTableStorageDataSourceName = "azure-table-storage";
        private const string AzureTableStorageIndexerName = "azure-table-storage-indexer";

        private const string AzureCosmosDbDataSourceName = "azure-cosmos-db";
        private const string AzureCosmosDbIndexerName = "azure-cosmos-db-indexer";

        private const string AzureSoftDeleteColumnName = "IsDeleted";
        private const string AzureSoftDeleteMarkerValue = "true";

        /// <summary>
        /// Specifies a soft column delete policy to be used with Azure Search data sources.
        /// </summary>
        /// <remarks>
        /// If the soft delete column has the marker value, the entire row or record is considered to be
        /// deleted
        /// See https://docs.microsoft.com/en-us/rest/api/searchservice/create-data-source for more information.
        /// </remarks>
        private static DataDeletionDetectionPolicy SoftDeleteColumnPolicy = new SoftDeleteColumnDeletionDetectionPolicy
        {
            SoftDeleteColumnName = AzureSoftDeleteColumnName,
            SoftDeleteMarkerValue = AzureSoftDeleteMarkerValue
        };

        /// <summary>
        /// Specifies a schedule for how often an Azure Search indexer runs.
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/rest/api/searchservice/create-indexer for more information.
        /// </remarks>
        private static IndexingSchedule IndexerSchedule = new IndexingSchedule
        {
            Interval = TimeSpan.FromMinutes(30),
            StartTime = DateTimeOffset.Now
        };

        private static void Usage()
        {
            Console.Error.WriteLine($"Usage: DotNetHowToIndexers ({AzureSqlOption}|{AzureTableStorageOption}|{AzureCosmosDbOption})");
            Console.Error.WriteLine(AzureSqlOption);
            Console.Error.WriteLine($"\tSpecify the {AzureSqlConnectionStringProperty} property in {ConfigFilePath}");
            Console.Error.WriteLine(AzureTableStorageOption);
            Console.Error.WriteLine($"\tSpecify the {AzureStorageConnectionStringProperty} and {AzureStorageTableNameProperty} properties in {ConfigFilePath}");
            Console.Error.WriteLine(AzureCosmosDbOption);
            Console.Error.WriteLine($"\tSpecify the {AzureCosmosDbConnectionStringProperty} and {AzureCosmosDbCollectionNameProperty} properties in {ConfigFilePath}");
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Usage();
                return;
            }

            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile(ConfigFilePath);
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            Console.WriteLine("Deleting index...");
            DeleteIndexIfExists(serviceClient);

            Console.WriteLine("Creating index...");
            CreateIndex(serviceClient);

            string option = args[0];
            string indexerName;
            if (option.Equals(AzureSqlOption, StringComparison.OrdinalIgnoreCase))
            {
                indexerName = SetupAzureSqlIndexer(serviceClient, configuration);
            }
            else if (option.Equals(AzureTableStorageOption, StringComparison.OrdinalIgnoreCase))
            {
                indexerName = SetupAzureTableStorageIndexer(serviceClient, configuration);
            }
            else if (option.Equals(AzureCosmosDbOption, StringComparison.OrdinalIgnoreCase))
            {
                indexerName = SetupAzureCosmosDbIndexer(serviceClient, configuration);
            }
            else
            {
                Usage();
                return;
            }

            Console.WriteLine("Running indexer...");
            serviceClient.Indexers.Run(indexerName);

            Console.WriteLine("Done...");
            Console.ReadKey();
        }

        /// <summary>
        /// Creates an indexer which pulls data from an Azure SQL database and puts it
        /// inside the hotels index.
        /// </summary>
        private static string SetupAzureSqlIndexer(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            Console.WriteLine("Deleting Azure SQL data source if it exists...");
            DeleteDataSourceIfExists(serviceClient, AzureSqlDataSourceName);

            Console.WriteLine("Creating Azure SQL data source...");
            DataSource azureSqlDataSource = CreateAzureSqlDataSource(serviceClient, configuration);

            Console.WriteLine("Deleting Azure SQL indexer if it exists...");
            DeleteIndexerIfExists(serviceClient, AzureSqlIndexerName);

            Console.WriteLine("Creating Azure SQL indexer...");
            Indexer azureSqlIndexer = CreateIndexer(serviceClient, AzureSqlDataSourceName, AzureSqlIndexerName);

            return azureSqlIndexer.Name;
        }

        /// <summary>
        /// Creates an indexer which pulls data from Azure Table Storage and puts it
        /// inside the hotels index.
        /// </summary>
        private static string SetupAzureTableStorageIndexer(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            Console.WriteLine("Deleting Azure Table Storage data source if it exists...");
            DeleteDataSourceIfExists(serviceClient, AzureTableStorageDataSourceName);

            Console.WriteLine("Creating Azure Table Storage data source...");
            DataSource azureTableStorageDataSource = CreateAzureTableStorageDataSource(serviceClient, configuration);

            Console.WriteLine("Deleting Azure Table Storage indexer...");
            DeleteIndexerIfExists(serviceClient, AzureTableStorageIndexerName);

            Console.WriteLine("Creating Azure Table Storage indexer...");
            Indexer azureTableStorageIndexer = CreateIndexer(serviceClient, AzureTableStorageDataSourceName, AzureTableStorageIndexerName);

            return azureTableStorageIndexer.Name;
        }

        /// <summary>
        /// Creates an indexer which pulls data from Azure Cosmos DB and puts it
        /// inside the hotels index.
        /// </summary>
        private static string SetupAzureCosmosDbIndexer(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            Console.WriteLine("Deleting Azure Cosmos DB data source...");
            DeleteDataSourceIfExists(serviceClient, AzureCosmosDbDataSourceName);

            Console.WriteLine("Creating Cosmos DB data source...");
            DataSource azureCosmosDbDataSource = CreateAzureCosmosDbDataSource(serviceClient, configuration);

            Console.WriteLine("Deleting Cosmos DB indexer...");
            DeleteIndexerIfExists(serviceClient, AzureCosmosDbIndexerName);

            Console.WriteLine("Creating Cosmos DB indexer...");
            Indexer azureCosmosDbIndexer = CreateIndexer(serviceClient, AzureCosmosDbDataSourceName, AzureCosmosDbIndexerName);

            return azureCosmosDbIndexer.Name;
        }

        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        private static void DeleteIndexIfExists(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(IndexName))
            {
                serviceClient.Indexes.DeleteAsync(IndexName).Wait();
            }
        }

        private static void CreateIndex(SearchServiceClient serviceClient)
        {
            var definition = new Index()
            {
                Name = IndexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            serviceClient.Indexes.CreateAsync(definition).Wait();
        }

        private static void DeleteDataSourceIfExists(SearchServiceClient serviceClient, string dataSourceName)
        {
            if (serviceClient.DataSources.Exists(dataSourceName))
            {
                serviceClient.DataSources.Delete(dataSourceName);
            }
        }

        private static Indexer CreateIndexer(SearchServiceClient serviceClient, string dataSourceName, string indexerName)
        {
            Indexer indexer = new Indexer
            {
                DataSourceName = dataSourceName,
                Name = indexerName,
                Schedule = IndexerSchedule,
                TargetIndexName = IndexName
            };
            return serviceClient.Indexers.Create(indexer);
        }

        private static void DeleteIndexerIfExists(SearchServiceClient serviceClient, string indexerName)
        {
            if (serviceClient.Indexers.Exists(indexerName))
            {
                serviceClient.Indexers.Delete(indexerName);
            }
        }

        /// <summary>
        /// Creates an Azure Search data source for a Azure SQL database.
        /// </summary>
        /// <remarks>
        /// Requires a connection string be specified in the configuration file.
        /// The table name is assumed to be "hotels" as specified in the accompanying
        /// sql script.
        /// </remarks>
        private static DataSource CreateAzureSqlDataSource(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            DataSource azureSqlDataSource = DataSource.AzureSql(
                name: AzureSqlDataSourceName,
                sqlConnectionString: configuration[AzureSqlConnectionStringProperty],
                tableOrViewName: AzureSqlTableName,
                deletionDetectionPolicy: SoftDeleteColumnPolicy);
            azureSqlDataSource.DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy();
            return serviceClient.DataSources.Create(azureSqlDataSource);
        }

        /// <summary>
        /// Creates an Azure Search data source for Azure Table Storage.
        /// </summary>
        /// <remarks>
        /// Requires a connection string be specified in the configuration file.
        /// Requires a table name be specified in the configuration file.
        /// </remarks>
        private static DataSource CreateAzureTableStorageDataSource(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            DataSource azureTableStorageDataSource = DataSource.AzureTableStorage(
                name: AzureTableStorageDataSourceName,
                storageConnectionString: configuration[AzureStorageConnectionStringProperty],
                tableName: configuration[AzureStorageTableNameProperty],
                query: null,
                deletionDetectionPolicy: SoftDeleteColumnPolicy);
            return serviceClient.DataSources.Create(azureTableStorageDataSource);
        }

        /// <summary>
        /// Creates an Azure Search data source for Azure Cosmos DB.
        /// </summary>
        /// <remarks>
        /// Requires a connection string with a database name be specified in the configuration file.
        /// Requires an Azure Cosmos DB collection name be specified in the configuration file.
        /// </remarks>
        private static DataSource CreateAzureCosmosDbDataSource(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            DataSource azureCosmosDbDataSource = DataSource.DocumentDb(
                name: AzureCosmosDbDataSourceName,
                documentDbConnectionString: configuration[AzureCosmosDbConnectionStringProperty],
                collectionName: configuration[AzureCosmosDbCollectionNameProperty],
                deletionDetectionPolicy: SoftDeleteColumnPolicy);
            return serviceClient.DataSources.Create(azureCosmosDbDataSource);
        }
    }
}
