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
        // This sample shows how to delete, create, upload documents and query an index
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteHotelsIndexIfExists(serviceClient);

            Console.WriteLine("{0}", "Creating index...\n");
            CreateHotelsIndex(serviceClient);

            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient("hotels");

            Console.WriteLine("{0}", "Uploading documents...\n");
            UploadDocuments(indexClient);

            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient(configuration);

            RunQueries(indexClientForQueries);

            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
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

        private static void DeleteHotelsIndexIfExists(SearchServiceClient serviceClient)
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
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            serviceClient.Indexes.Create(definition);
        }

#if HowToExample

        private static void UploadDocuments(ISearchIndexClient indexClient)
        {
            var hotels = new Hotel[]
            {
                new Hotel()
                { 
                    HotelId = "1", 
                    Description = "Best hotel in town",
                    DescriptionFr = "Meilleur hôtel en ville",
                    HotelName = "Fancy Stay",
                    Category = "Luxury", 
                    Tags = new[] { "pool", "view", "wifi", "concierge" },
                    ParkingIncluded = false, 
                    LastRenovationDate = new DateTimeOffset(2010, 6, 27, 0, 0, 0, TimeSpan.Zero), 
                    Rating = 5, 
                    Location = GeographyPoint.Create(47.678581, -122.131577),
                    Rooms = new Room[]
                    {
                        new Room()
                        {
                            RoomId = "1",
                            Description = "Deluxe Room, 2 Double Beds (Cityside)",
                            DescriptionFr = "Chambre Deluxe, 2 lits doubles (Côté ville)",
                            Type = "Deluxe",
                            BaseRate = 199.00,
                            BedOptions = "2 Double Beds",
                            SleepsCount = 4,
                            SmokingAllowed = false,
                            Tags = new[] { "city", "view" }
                        },
                        new Room()
                        {
                            RoomId = "2",
                            Description = "Deluxe Room, 1 King Bed (Waterfront)",
                            DescriptionFr = "Chambre Deluxe, 1 lit King-Size (Face à l'eau)",
                            Type = "Deluxe",
                            BaseRate = 249.00,
                            BedOptions = "1 King Bed",
                            SleepsCount = 3,
                            SmokingAllowed = false,
                            Tags = new[] { "water", "view" }
                        },
                        new Room()
                        {
                            RoomId = "3",
                            Description = "Deluxe Room, 2 Double Beds (Waterfront)",
                            DescriptionFr = "Chambre Deluxe, 1 lit King-Size (Face à l'eau)",
                            Type = "Deluxe",
                            BaseRate = 199.00,
                            BedOptions = "2 Double Beds",
                            SleepsCount = 4,
                            SmokingAllowed = false,
                            Tags = new[] { "water", "view" }
                        }
                    }
                },
                new Hotel()
                { 
                    HotelId = "2", 
                    Description = "Cheapest hotel in town",
                    DescriptionFr = "Hôtel le moins cher en ville",
                    HotelName = "Roach Motel",
                    Category = "Budget",
                    Tags = new[] { "motel", "budget" },
                    ParkingIncluded = true,
                    LastRenovationDate = new DateTimeOffset(1982, 4, 28, 0, 0, 0, TimeSpan.Zero),
                    Rating = 1,
                    Location = GeographyPoint.Create(49.678581, -122.131577),
                    Rooms = new Room[]
                    {
                        new Room()
                        {
                            RoomId = "1",
                            Description = "Standard Room, 1 Queen Bed",
                            DescriptionFr = "Chambre standard, 1 lit Queen-Size",
                            Type = "Standard",
                            BaseRate = 79.00,
                            BedOptions = "1 Queen Beds",
                            SleepsCount = 3,
                            SmokingAllowed = false
                        },
                        new Room()
                        {
                            RoomId = "2",
                            Description = "Standard Room, 2 Queen Beds",
                            DescriptionFr = "Chambre standard, 2 lit Queen-Size",
                            Type = "Deluxe",
                            BaseRate = 87.00,
                            BedOptions = "2 Queen Bed",
                            SleepsCount = 5,
                            SmokingAllowed = false
                        }
                    }
                },
                new Hotel() 
                { 
                    HotelId = "3", 
                    Description = "Close to town hall and the river",
                    Rooms = new Room[]
                    {
                        new Room()
                        {
                            RoomId = "1",
                            Description = "Standard Double Room",
                            DescriptionFr = "Chambre Double Standard",
                            Type = "Standard",
                            BaseRate = 129.00,
                            BedOptions = "2 Double Beds",
                            SleepsCount = 5,
                            SmokingAllowed = false
                        }
                    }
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

#else

        private static void UploadDocuments(ISearchIndexClient indexClient)
        {
            var actions =
                new IndexAction<Hotel>[]
                {
                    IndexAction.Upload(
                        new Hotel()
                        {
                            HotelId = "1",
                            Description = "Best hotel in town",
                            DescriptionFr = "Meilleur hôtel en ville",
                            HotelName = "Fancy Stay",
                            Category = "Luxury",
                            Tags = new[] { "pool", "view", "wifi", "concierge" },
                            ParkingIncluded = false,
                            LastRenovationDate = new DateTimeOffset(2010, 6, 27, 0, 0, 0, TimeSpan.Zero),
                            Rating = 5,
                            Location = GeographyPoint.Create(47.678581, -122.131577),
                            Rooms = new Room[]
                            {
                                new Room()
                                {
                                    RoomId = "1",
                                    Description = "Deluxe Room, 2 Double Beds (Cityside)",
                                    DescriptionFr = "Chambre Deluxe, 2 lits doubles (Côté ville)",
                                    Type = "Deluxe",
                                    BaseRate = 199.00,
                                    BedOptions = "2 Double Beds",
                                    SleepsCount = 4,
                                    SmokingAllowed = false,
                                    Tags = new[] { "city", "view" }
                                },
                                new Room()
                                {
                                    RoomId = "2",
                                    Description = "Deluxe Room, 1 King Bed (Waterfront)",
                                    DescriptionFr = "Chambre Deluxe, 1 lit King-Size (Face à l'eau)",
                                    Type = "Deluxe",
                                    BaseRate = 249.00,
                                    BedOptions = "1 King Bed",
                                    SleepsCount = 3,
                                    SmokingAllowed = false,
                                    Tags = new[] { "water", "view" }
                                },
                                new Room()
                                {
                                    RoomId = "3",
                                    Description = "Deluxe Room, 2 Double Beds (Waterfront)",
                                    DescriptionFr = "Chambre Deluxe, 1 lit King-Size (Face à l'eau)",
                                    Type = "Deluxe",
                                    BaseRate = 199.00,
                                    BedOptions = "2 Double Beds",
                                    SleepsCount = 4,
                                    SmokingAllowed = false,
                                    Tags = new[] { "water", "view" }
                                }
                            }
                        }),
                    IndexAction.Upload(
                        new Hotel()
                        {
                            HotelId = "2",
                            Description = "Cheapest hotel in town",
                            DescriptionFr = "Hôtel le moins cher en ville",
                            HotelName = "Roach Motel",
                            Category = "Budget",
                            Tags = new[] { "motel", "budget" },
                            ParkingIncluded = true,
                            LastRenovationDate = new DateTimeOffset(1982, 4, 28, 0, 0, 0, TimeSpan.Zero),
                            Rating = 1,
                            Location = GeographyPoint.Create(49.678581, -122.131577),
                            Rooms = new Room[]
                            {
                                new Room()
                                {
                                    RoomId = "1",
                                    Description = "Standard Room, 1 Queen Bed",
                                    DescriptionFr = "Chambre standard, 1 lit Queen-Size",
                                    Type = "Standard",
                                    BaseRate = 79.00,
                                    BedOptions = "1 Queen Beds",
                                    SleepsCount = 3,
                                    SmokingAllowed = false
                                },
                                new Room()
                                {
                                    RoomId = "2",
                                    Description = "Standard Room, 2 Queen Beds",
                                    DescriptionFr = "Chambre standard, 2 lit Queen-Size",
                                    Type = "Deluxe",
                                    BaseRate = 87.00,
                                    BedOptions = "2 Queen Bed",
                                    SleepsCount = 5,
                                    SmokingAllowed = false
                                }
                            }
                        }),
                    IndexAction.MergeOrUpload(
                        new Hotel()
                        {
                            HotelId = "3",
                            Description = "Close to town hall and the river",
                            Rooms = new Room[]
                            {
                                new Room()
                                {
                                    RoomId = "1",
                                    Description = "Standard Double Room",
                                    DescriptionFr = "Chambre Double Standard",
                                    Type = "Standard",
                                    BaseRate = 129.00,
                                    BedOptions = "2 Double Beds",
                                    SleepsCount = 5,
                                    SmokingAllowed = false
                                }
                            }
                        }),
                    IndexAction.Delete(new Hotel() { HotelId = "6" })
                };

            var batch = IndexBatch.New(actions);

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
#endif

        private static void RunQueries(ISearchIndexClient indexClient)
        {
            SearchParameters parameters;
            DocumentSearchResult<Hotel> results;

            Console.WriteLine("Search the entire index for the term 'budget' and return only the hotelName field:\n");

            parameters =
                new SearchParameters()
                {
                    Select = new[] { "hotelName" }
                };

            results = indexClient.Documents.Search<Hotel>("budget", parameters);

            WriteDocuments(results);

            Console.Write("Apply a filter to the index to find hotels cheaper than $150 per night, ");
            Console.WriteLine("and return the hotelId and description:\n");

            parameters =
                new SearchParameters()
                {
                    Filter = "rooms.baseRate lt 150",
                    Select = new[] { "hotelId", "description" }
                };

            results = indexClient.Documents.Search<Hotel>("*", parameters);

            WriteDocuments(results);

            Console.Write("Search the entire index, order by a specific field (lastRenovationDate) ");
            Console.Write("in descending order, take the top two results, and show only hotelName and ");
            Console.WriteLine("lastRenovationDate:\n");

            parameters =
                new SearchParameters()
                {
                    OrderBy = new[] { "lastRenovationDate desc" },
                    Select = new[] { "hotelName", "lastRenovationDate" },
                    Top = 2
                };

            results = indexClient.Documents.Search<Hotel>("*", parameters);

            WriteDocuments(results);

            Console.WriteLine("Search the entire index for the term 'motel':\n");

            parameters = new SearchParameters();
            results = indexClient.Documents.Search<Hotel>("motel", parameters);

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
