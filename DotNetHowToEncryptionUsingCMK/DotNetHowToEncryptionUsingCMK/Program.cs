#define HowToExample

using System;
using System.Linq;
using System.Threading;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Spatial;

namespace AzureSearch.SDKHowTo
{
    class Program
    {
        // This sample shows how to create a synonym-map and an index that are encrypted with customer-managed key in Azure Key Vault
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchIndexClient indexClient = CreateSearchIndexClient(configuration);

            Console.WriteLine("Cleaning up resources...\n");
            CleanupResources(indexClient);

            Console.WriteLine("Creating synonym-map encrypted with customer managed key...\n");
            CreateSynonymsEncryptedUsingCustomerManagedKey(indexClient, configuration);

            Console.WriteLine("Creating index encrypted with customer managed key...\n");
            CreateHotelsIndexEncryptedUsingCustomerManagedKey(indexClient, configuration);

            SearchIndex index = indexClient.GetIndex("hotels");
            index = AddSynonymMapsToFields(index);
            indexClient.CreateOrUpdateIndex(index);

            SearchClient searchClient = indexClient.GetSearchClient("hotels");

            Console.WriteLine("Uploading documents...\n");
            UploadDocuments(searchClient);

            SearchClient searchClientForQueries = CreateSearchClient(configuration);

            RunQueries(searchClientForQueries);

            Console.WriteLine("Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static SearchIndexClient CreateSearchIndexClient(IConfigurationRoot configuration)
        {
            string searchServiceEndPoint = configuration["SearchServiceEndPoint"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchIndexClient indexClient = new SearchIndexClient(new Uri(searchServiceEndPoint), new AzureKeyCredential(adminApiKey));
            return indexClient;
        }

        private static SearchClient CreateSearchClient(IConfigurationRoot configuration)
        {
            string searchServiceEndPoint = configuration["SearchServiceEndPoint"];
            string queryApiKey = configuration["SearchServiceQueryApiKey"];

            SearchClient searchClient = new SearchClient(new Uri(searchServiceEndPoint), "hotels", new AzureKeyCredential(queryApiKey));
            return searchClient;
        }

        private static void CleanupResources(SearchIndexClient indexClient)
        {
            try
            {
                if (indexClient.GetIndex("hotels") != null)
                {
                    indexClient.DeleteIndex("hotels");
                }
                if (indexClient.GetSynonymMapNames().Value.Contains("desc-synonymmap"))
                {
                    indexClient.DeleteSynonymMap("desc-synonymmap");
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                //if exception occurred and status is "Not Found", this is work as expect
                Console.WriteLine("Failed to find index and this is because it's not there.");
            }
        }

        private static void CreateSynonymsEncryptedUsingCustomerManagedKey(SearchIndexClient indexClient, IConfigurationRoot configuration)
        {
            var synonymMap = new SynonymMap("desc-synonymmap", "hotel, motel\ninternet,wifi\nfive star=>luxury\neconomy,inexpensive=>budget")
            {
                EncryptionKey = GetEncryptionKeyFromConfiguration(configuration)
            };

            indexClient.CreateOrUpdateSynonymMap(synonymMap);
        }

        private static SearchIndex AddSynonymMapsToFields(SearchIndex index)
        {
            //remove SynonymMaps attribute in class hotel, need Add maps manually.
            index.Fields.First(f => f.Name == "Category").SynonymMapNames.Add("desc-synonymmap");
            index.Fields.First(f => f.Name == "Tags").SynonymMapNames.Add("desc-synonymmap");
            return index;
        }

        private static void CreateHotelsIndexEncryptedUsingCustomerManagedKey(SearchIndexClient indexClient, IConfigurationRoot configuration)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Hotel));
            var definition = new SearchIndex("hotels", searchFields)
            {
                EncryptionKey = GetEncryptionKeyFromConfiguration(configuration)
            };

            indexClient.CreateOrUpdateIndex(definition);
        }

        private static SearchResourceEncryptionKey GetEncryptionKeyFromConfiguration(IConfigurationRoot configuration)
        {
            Uri keyVaultKeyUri = new Uri(configuration["AzureKeyVaultKeyIdentifier"]);

            if (!keyVaultKeyUri.Host.Contains("vault.azure.net") || keyVaultKeyUri.Segments.Length != 4 || keyVaultKeyUri.Segments[1] != "keys/")
            {
                throw new ArgumentException("Invalid 'AzureKeyVaultKeyIdentifier' - Expected format: 'https://<key-vault-name>.vault.azure.net/keys/<key-name>/<key-version>'", "AzureKeyVaultKeyIdentifier");
            }

            SearchResourceEncryptionKey encryptionKey = null;
            string applicationId = configuration["AzureActiveDirectoryApplicationId"];
            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                encryptionKey = new SearchResourceEncryptionKey(new Uri($"{keyVaultKeyUri.Scheme}://{keyVaultKeyUri.Host}"), keyVaultKeyUri.Segments[2].Trim('/'), keyVaultKeyUri.Segments[3].Trim('/'))
                {
                    ApplicationId = applicationId,
                    ApplicationSecret = configuration["AzureActiveDirectoryApplicationSecret"]
                };
            }


            return encryptionKey;
        }

        private static void UploadDocuments(SearchClient searchClient)
        {
            IndexDocumentsBatch<Hotel> batch = IndexDocumentsBatch.Create(
                IndexDocumentsAction.Upload(
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
                    }),
                IndexDocumentsAction.Upload(
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
                    }));
            try
            {
                IndexDocumentsResult result = searchClient.IndexDocuments(batch);
            }
            catch (Exception)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine("Failed to index some of the documents: {0}");
            }

            Console.WriteLine("Waiting for documents to be indexed...\n");
            Thread.Sleep(2000);
        }

        private static void RunQueries(SearchClient searchClient)
        {
            SearchOptions searchOptions;
            SearchResults<Hotel> results;

            Console.WriteLine("Search with terms nonexistent in the index:\n");

            searchOptions = new SearchOptions();
            searchOptions.SearchFields.Add("Category");
            searchOptions.SearchFields.Add("Tags");
            searchOptions.Select.Add("HotelName");
            searchOptions.Select.Add("Category");
            searchOptions.Select.Add("Tags");

            Console.WriteLine("Search the entire index for the phrase \"five star\":\n");
            results = searchClient.Search<Hotel>("\"five star\"", searchOptions);
            WriteDocuments(results);

            Console.WriteLine("Search the entire index for the term 'internet':\n");
            results = searchClient.Search<Hotel>("internet", searchOptions);
            WriteDocuments(results);

            Console.WriteLine("Search the entire index for the terms 'economy' AND 'hotel':\n");
            results = searchClient.Search<Hotel>("economy AND hotel", searchOptions);
            WriteDocuments(results);
        }

        private static void WriteDocuments(SearchResults<Hotel> searchResults)
        {
            foreach (SearchResult<Hotel> result in searchResults.GetResults())
            {
                Console.WriteLine(result.Document);
            }

            Console.WriteLine();
        }
    }
}
