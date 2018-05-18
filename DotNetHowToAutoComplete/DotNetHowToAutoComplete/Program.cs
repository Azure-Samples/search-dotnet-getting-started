using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using AzureSearch.SDKAutoComplete;

namespace AzureSearch.SDKAutocomplete
{
    class Program
    {
        // This sample shows how to get autocompleted terms.
        static void Main(string[] args)
        {
            SearchServiceClient serviceClient = CreateSearchServiceClient();

            Console.WriteLine("Cleaning up resources...\n");
            CleanupResources(serviceClient);

            Console.WriteLine("Creating index...\n");
            CreateHotelsIndex(serviceClient);

            SearchIndexClient searchIndexClient = CreateSearchIndexClient();

            Console.WriteLine("Uploading documents...\n");
            UploadDocuments(searchIndexClient);

            Console.WriteLine("Running autocomplete queries...\n");
            RunAutocompleteQueries(searchIndexClient);

            Console.WriteLine("Complete. Press any key to end application...\n");

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
        }

        private static void CreateHotelsIndex(SearchServiceClient serviceClient)
        {
            var definition = new Index()
            {
                Name = "hotels",
                Fields = FieldBuilder.BuildForType<Hotel>(),
                Suggesters = new[]
                    {
                        new Suggester(
                            name: "sg",
                            sourceFields: new[] { "hotelName", "description" })
                    }
            };

            serviceClient.Indexes.Create(definition);
        }

        private static void UploadDocuments(SearchIndexClient indexClient)
        {
            var hotels = new Hotel[]
            {
                new Hotel()
                {
                    HotelId = "1",
                    BaseRate = 199.0,
                    Description = "Best hotel in town",
                    DescriptionFr = "Meilleur hôtel en ville",
                    HotelName = "Fancy Stay Hotel",
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
                    BaseRate = 39.99,
                    Description = "High quality, low cost hostels, suitable for families & backpackers",
                    DescriptionFr = "Des auberges de qualité, abordables, adaptées aux familles et aux routards",
                    HotelName = "Youth hostel",
                    Category = "Budget",
                    Tags = new[] { "hostel", "free wifi" },
                    ParkingIncluded = false,
                    SmokingAllowed = false,
                    LastRenovationDate = new DateTimeOffset(2018, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    Rating = 4,
                    Location = GeographyPoint.Create(49.678581, -122.131577)
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

        private static void RunAutocompleteQueries(ISearchIndexClient indexClient)
        {
            AutocompleteParameters acparameters = new AutocompleteParameters
            {
                SearchFields = "hotelName,description"
            };

            Console.WriteLine("Autocomplete query with OneTerm mode:\n");

            AutocompleteResult response = indexClient.Documents.Autocomplete(AutocompleteMode.OneTerm, "best ho", "sg", autocompleteParameters: acparameters);

            WriteAutocompleteResults(response);

            Console.WriteLine("Autocomplete with OneTermWithContext mode:\n");

            response = indexClient.Documents.Autocomplete(AutocompleteMode.OneTermWithContext, "best ho", "sg", autocompleteParameters: acparameters);

            WriteAutocompleteResults(response);

            Console.WriteLine("Autocomplete with TwoTerms mode:\n");

            response = indexClient.Documents.Autocomplete(AutocompleteMode.TwoTerms, "best ho", "sg", autocompleteParameters: acparameters);

            WriteAutocompleteResults(response);

            Console.WriteLine("Autocomplete with OneTerm mode with fuzzy enabled:\n");

            acparameters.Fuzzy = true;
            response = indexClient.Documents.Autocomplete(AutocompleteMode.OneTerm, "best hostel", "sg", autocompleteParameters: acparameters);

            WriteAutocompleteResults(response);
        }

        private static void WriteAutocompleteResults(AutocompleteResult autocompleteResponse)
        {
            if (autocompleteResponse.Results.Count != 0)
            {
                foreach (AutocompleteItem item in autocompleteResponse.Results)
                {
                    Console.WriteLine("text: " + item.Text + " queryPlusText: " + item.QueryPlusText);
                }
            }
            else
            {
                Console.WriteLine("no text matched");
            }
            Console.WriteLine();
        }
    }
}
