using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Microsoft.Spatial;
using Microsoft.Rest.Azure;
using Azure.Search.Documents.Indexes;
using Azure;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace AzureSearch.SDKHowToSynonyms
{
    class Program
    {
        // This sample shows how to delete, create, upload documents and query an index with a synonym map
        static void Main(string[] args)
        {
            SearchIndexClient indexClient = CreateSearchIndexClient();

            Console.WriteLine("Cleaning up resources...\n");
            CleanupResources(indexClient);

            Console.WriteLine("Creating index...\n");
            CreateHotelsIndex(indexClient);

            SearchClient searchClient = indexClient.GetSearchClient("hotels");

            Console.WriteLine("Uploading documents...\n");
            UploadDocuments(searchClient);

            SearchClient searchClientForQueries = CreateSearchClientForQueries();

            RunQueriesWithNonExistentTermsInIndex(searchClientForQueries);

            Console.WriteLine("Adding synonyms...\n");
            UploadSynonyms(indexClient);

            Console.WriteLine("Enabling synonyms in the test index...\n");
            EnableSynonymsInHotelsIndexSafely(indexClient);
            Thread.Sleep(10000); // Wait for the changes to propagate

            RunQueriesWithNonExistentTermsInIndex(searchClientForQueries);

            Console.WriteLine("Complete.  Press any key to end application...\n");

            Console.ReadKey();
        }

        private static SearchIndexClient CreateSearchIndexClient()
        {
            string searchServiceEndPoint = ConfigurationManager.AppSettings["SearchServiceEndPoint"];
            string adminApiKey = ConfigurationManager.AppSettings["SearchServiceAdminApiKey"];

            SearchIndexClient indexClient = new SearchIndexClient(new Uri(searchServiceEndPoint), new AzureKeyCredential(adminApiKey));
            return indexClient;
        }

        private static SearchClient CreateSearchClientForQueries()
        {
            string searchServiceEndPoint = ConfigurationManager.AppSettings["SearchServiceEndPoint"];
            string queryApiKey = ConfigurationManager.AppSettings["SearchServiceQueryApiKey"];

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

        private static void CreateHotelsIndex(SearchIndexClient indexClient)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Hotel));
            var searchIndex = new SearchIndex("hotels", searchFields);

            indexClient.CreateOrUpdateIndex(searchIndex);
        }

        private static void EnableSynonymsInHotelsIndexSafely(SearchIndexClient indexClient)
        {
            int MaxNumTries = 3;

            for (int i = 0; i < MaxNumTries; ++i)
            {
                try
                {
                    SearchIndex index = indexClient.GetIndex("hotels");
                    index = AddSynonymMapsToFields(index);

                    // The IfNotChanged condition ensures that the index is updated only if the ETags match.
                    indexClient.CreateOrUpdateIndex(index);

                    Console.WriteLine("Updated the index successfully.\n");
                    break;
                }
                catch (CloudException)
                {
                    Console.WriteLine($"Index update failed : . Attempt({i}/{MaxNumTries}).\n");
                }
            }
        }

        private static SearchIndex AddSynonymMapsToFields(SearchIndex index)
        {
            index.Fields.First(f => f.Name == "Category").SynonymMapNames.Add("desc-synonymmap");
            index.Fields.First(f => f.Name == "Tags").SynonymMapNames.Add("desc-synonymmap");
            return index;
        }

        private static void UploadSynonyms(SearchIndexClient indexClient)
        {
            var synonymMap = new SynonymMap("desc-synonymmap", "hotel, motel\ninternet,wifi\nfive star=>luxury\neconomy,inexpensive=>budget");

            indexClient.CreateOrUpdateSynonymMap(synonymMap);
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

        private static void RunQueriesWithNonExistentTermsInIndex(SearchClient searchClient)
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
            var a = searchResults.TotalCount;
            if (searchResults.GetResults().Count() != 0)
            {
                foreach (SearchResult<Hotel> result in searchResults.GetResults())
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
