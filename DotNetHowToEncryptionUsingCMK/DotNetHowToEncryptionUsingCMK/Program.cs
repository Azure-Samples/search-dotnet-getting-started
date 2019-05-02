#define HowToExample

namespace AzureSearch.SDKHowTo
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Spatial;

    class Program
    {
        // This sample shows how to create a synonym-map and an index that are encrypted with customer-managed key in Azure Key Vault
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            Console.WriteLine("Cleaning up resources...\n");
            CleanupResources(serviceClient);

            Console.WriteLine("Creating synonym-map encrypted with customer managed key...\n");
            CreateSynonymsEncryptedUsingCustomerManagedKey(serviceClient, configuration);

            Console.WriteLine("Creating index encrypted with customer managed key...\n");
            CreateHotelsIndexEncryptedUsingCustomerManagedKey(serviceClient, configuration);

            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient("hotels");

            Console.WriteLine("Uploading documents...\n");
            UploadDocuments(indexClient);

            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient(configuration);

            RunQueries(indexClientForQueries);

            Console.WriteLine("Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        private static SearchIndexClient CreateSearchIndexClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string queryApiKey = configuration["SearchServiceQueryApiKey"];

            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, "hotels", new SearchCredentials(queryApiKey));
            return indexClient;
        }

        private static void CleanupResources(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists("hotels"))
            {
                serviceClient.Indexes.Delete("hotels");
            }

            if (serviceClient.SynonymMaps.Exists("desc-synonymmap"))
            {
                serviceClient.SynonymMaps.Delete("desc-synonymmap");
            }
        }

        private static void CreateSynonymsEncryptedUsingCustomerManagedKey(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            var synonymMap = new SynonymMap()
            {
                Name = "desc-synonymmap",
                Synonyms = "hotel, motel\ninternet,wifi\nfive star=>luxury\neconomy,inexpensive=>budget",
                EncryptionKey = GetEncryptionKeyFromConfiguration(configuration)
            };

            serviceClient.SynonymMaps.CreateOrUpdate(synonymMap);
        }

        private static void CreateHotelsIndexEncryptedUsingCustomerManagedKey(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            var definition = new Index()
            {
                Name = "hotels",
                Fields = FieldBuilder.BuildForType<Hotel>(),
                EncryptionKey = GetEncryptionKeyFromConfiguration(configuration)
            };

            serviceClient.Indexes.Create(definition);
        }

        private static EncryptionKey GetEncryptionKeyFromConfiguration(IConfigurationRoot configuration)
        {
            Uri keyVaultKeyUri = new Uri(configuration["AzureKeyVaultKeyIdentifier"]);
            if (!keyVaultKeyUri.Host.Contains("vault.azure.net") || keyVaultKeyUri.Segments.Length != 4 || keyVaultKeyUri.Segments[1] != "keys/")
            {
                throw new ArgumentException("Invalid 'AzureKeyVaultKeyIdentifier' - Expected format: 'https://<key-vault-name>.vault.azure.net/keys/<key-name>/<key-version>'", "AzureKeyVaultKeyIdentifier");
            }

            var encryptionKey = new EncryptionKey
            {
                KeyVaultUri = $"{keyVaultKeyUri.Scheme}://{keyVaultKeyUri.Host}",
                KeyVaultKeyName = keyVaultKeyUri.Segments[2].Trim('/'),
                KeyVaultKeyVersion = keyVaultKeyUri.Segments[3].Trim('/')
            };

            string applicationId = configuration["AzureActiveDirectoryApplicationId"];
            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                encryptionKey.AccessCredentials = new AzureActiveDirectoryApplicationCredentials
                {
                    ApplicationId = applicationId,
                    ApplicationSecret = configuration["AzureActiveDirectoryApplicationSecret"]
                };
            }

            encryptionKey.Validate();
            return encryptionKey;
        }

        private static void UploadDocuments(ISearchIndexClient indexClient)
        {
            var hotels = new Hotel[]
            {
                new Hotel()
                { 
                    HotelId = "1", 
                    BaseRate = 199.0, 
                    Description = "Best hotel in town",
                    DescriptionFr = "Meilleur hôtel en ville",
                    HotelName = "Fancy Stay",
                    Category = "Luxury", 
                    Tags = new[] { "pool", "view", "wifi", "concierge" },
                    ParkingIncluded = false, 
                    SmokingAllowed = false,
                    LastRenovationDate = new DateTimeOffset(2010, 6, 27, 0, 0, 0, TimeSpan.Zero), 
                    Rating = 5, 
                    Location = GeographyPoint.Create(47.678581, -122.131577)
                },
                new Hotel()
                { 
                    HotelId = "2", 
                    BaseRate = 79.99,
                    Description = "Cheapest hotel in town",
                    DescriptionFr = "Hôtel le moins cher en ville",
                    HotelName = "Roach Motel",
                    Category = "Budget",
                    Tags = new[] { "motel", "budget" },
                    ParkingIncluded = true,
                    SmokingAllowed = true,
                    LastRenovationDate = new DateTimeOffset(1982, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    Rating = 1,
                    Location = GeographyPoint.Create(49.678581, -122.131577)
                },
                new Hotel() 
                { 
                    HotelId = "3", 
                    BaseRate = 129.99,
                    Description = "Close to town hall and the river"
                }
            };

            var batch = IndexBatch.Upload(hotels);

            try
            {
                indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine(
                    "Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }

            Console.WriteLine("Waiting for documents to be indexed...\n");
            Thread.Sleep(2000);
        }

        private static void RunQueries(ISearchIndexClient indexClient)
        {
            SearchParameters parameters;
            DocumentSearchResult<Hotel> results;

            Console.WriteLine("Search with terms nonexistent in the index:\n");

            parameters =
                new SearchParameters()
                {
                    SearchFields = new[] { "category", "tags" },
                    Select = new[] { "hotelName", "category", "tags" },
                };

            Console.WriteLine("Search the entire index for the phrase \"five star\":\n");
            results = indexClient.Documents.Search<Hotel>("\"five star\"", parameters);
            WriteDocuments(results);

            Console.WriteLine("Search the entire index for the term 'internet':\n");
            results = indexClient.Documents.Search<Hotel>("internet", parameters);
            WriteDocuments(results);

            Console.WriteLine("Search the entire index for the terms 'economy' AND 'hotel':\n");
            results = indexClient.Documents.Search<Hotel>("economy AND hotel", parameters);
            WriteDocuments(results);
        }

        private static void WriteDocuments(DocumentSearchResult<Hotel> searchResults)
        {
            foreach (SearchResult<Hotel> result in searchResults.Results)
            {
                Console.WriteLine(result.Document);
            }

            Console.WriteLine();
        }
    }
}
