using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using Microsoft.Rest.Azure;

namespace AzureSearch.SDKHowToSynonyms
{
    class Program
    {
        // This sample shows how to delete, create, upload documents and query an index with a synonym map
        static void Main(string[] args)
        {
            SearchServiceClient serviceClient = CreateSearchServiceClient();

            Console.WriteLine("Cleaning up resources...\n");
            CleanupResources(serviceClient);

            Console.WriteLine("Creating index...\n");
            CreateHotelsIndex(serviceClient);

            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient("hotels");

            Console.WriteLine("Uploading documents...\n");
            UploadDocuments(indexClient);

            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient();

            RunQueriesWithNonExistentTermsInIndex(indexClientForQueries);

            Console.WriteLine("Adding synonyms...\n");
            UploadSynonyms(serviceClient);

            Console.WriteLine("Enabling synonyms in the test index...\n");
            EnableSynonymsInHotelsIndexSafely(serviceClient);
            Thread.Sleep(10000); // Wait for the changes to propagate

            RunQueriesWithNonExistentTermsInIndex(indexClientForQueries);

            Console.WriteLine("Complete.  Press any key to end application...\n");

            Console.ReadKey();
        }

        private static SearchServiceClient CreateSearchServiceClient()
        {
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string adminApiKey = ConfigurationManager.AppSettings["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        private static SearchIndexClient CreateSearchIndexClient()
        {
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string queryApiKey = ConfigurationManager.AppSettings["SearchServiceQueryApiKey"];

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

        private static void CreateHotelsIndex(SearchServiceClient serviceClient)
        {
            var definition = new Index()
            {
                Name = "hotels",
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            serviceClient.Indexes.Create(definition);
        }

        private static void EnableSynonymsInHotelsIndexSafely(SearchServiceClient serviceClient)
        {
            int MaxNumTries = 3;

            for (int i = 0; i < MaxNumTries; ++i)
            {
                try
                {
                    Index index = serviceClient.Indexes.Get("hotels");
                    index = AddSynonymMapsToFields(index);

                    // The IfNotChanged condition ensures that the index is updated only if the ETags match.
                    serviceClient.Indexes.CreateOrUpdate(index, accessCondition: AccessCondition.IfNotChanged(index));

                    Console.WriteLine("Updated the index successfully.\n");
                    break;
                }
                catch (CloudException e) when (e.IsAccessConditionFailed())
                {
                    Console.WriteLine($"Index update failed : {e.Message}. Attempt({i}/{MaxNumTries}).\n");
                }
            }
        }

        private static Index AddSynonymMapsToFields(Index index)
        {
            index.Fields.First(f => f.Name == "category").SynonymMaps = new[] { "desc-synonymmap" };
            index.Fields.First(f => f.Name == "tags").SynonymMaps = new[] { "desc-synonymmap" };
            return index;
        }

        private static void UploadSynonyms(SearchServiceClient serviceClient)
        {
            var synonymMap = new SynonymMap()
            {
                Name = "desc-synonymmap",
                Format = "solr",
                Synonyms = "hotel, motel\ninternet,wifi\nfive star=>luxury\neconomy,inexpensive=>budget"
            };

            serviceClient.SynonymMaps.CreateOrUpdate(synonymMap);
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

        private static void RunQueriesWithNonExistentTermsInIndex(ISearchIndexClient indexClient)
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
            if (searchResults.Results.Count != 0)
            {
                foreach (SearchResult<Hotel> result in searchResults.Results)
                {
                    Console.WriteLine(result.Document);
                }
            } 
            else
            {
                Console.WriteLine("no document matched");
            }

            Console.WriteLine();
        }
    }
}
