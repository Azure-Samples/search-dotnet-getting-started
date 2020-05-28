using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace AzureSearch.SDKHowTo
{
    class Program
    {
        private static SearchIndex searchIndex;

        // This sample shows how ETags work by performing conditional updates and deletes
        // on an Azure Search index.
        static async Task Main()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient searchServiceClient = GetSearchServiceClient(configuration);

            // Every top-level resource in Azure Search has an associated ETag that keeps track of which version
            // of the resource you're working on. When you first create a resource such as an index, its ETag is
            // empty.
            searchIndex = DefineSearchIndex();
            Console.WriteLine(
                $"Test searchIndex hasn't been created yet, so its ETag should be blank. ETag: '{searchIndex.ETag}'");

            // Once the resource exists in Azure Search, its ETag will be populated. Make sure to use the object
            // returned by the SearchServiceClient! Otherwise, you will still have the old object with the
            // blank ETag.
            Console.WriteLine("Creating searchIndex...\n");
            searchIndex = await searchServiceClient.CreateIndexAsync(searchIndex);
            Console.WriteLine($"Test searchIndex created; Its ETag should be populated. ETag: '{searchIndex.ETag}'");

            // ETags let you do some useful things you couldn't do otherwise. For example, by using an If-Match
            // condition, we can update an index using CreateOrUpdateIndexAsync() and be guaranteed that the update will only
            // succeed if the index already exists.
            SearchField searchField = new SearchField("name", SearchFieldDataType.String) { Analyzer = LexicalAnalyzerName.EnMicrosoft };
            searchIndex.Fields.Add(searchField);

            searchIndex = await searchServiceClient.CreateOrUpdateIndexAsync(searchIndex);
            Console.WriteLine(
                $"Test searchIndex updated; Its ETag should have changed since it was created. ETag: '{searchIndex.ETag}'");

            // More importantly, ETags protect you from concurrent updates to the same resource. If another
            // client tries to update the resource, it will fail as long as all clients are using the right
            // access conditions.
            SearchIndex indexForClient1 = searchIndex;
            SearchIndex indexForClient2 = await searchServiceClient.GetIndexAsync("test");

            Console.WriteLine("Simulating concurrent update. To start, both clients see the same ETag.");
            Console.WriteLine($"Client 1 ETag: '{indexForClient1.ETag}' Client 2 ETag: '{indexForClient2.ETag}'");

            // Client 1 successfully updates the index.
            indexForClient1.Fields.Add(new SearchField("a", SearchFieldDataType.Int32));
            indexForClient1 = await searchServiceClient.CreateOrUpdateIndexAsync(indexForClient1);
            Console.WriteLine($"Test searchIndex updated by client 1; ETag: '{indexForClient1.ETag}'");

            // Client 2 tries to update the index, but fails, thanks to the ETag check.
            try
            {
                indexForClient2.Fields.Add(new SearchField("b", SearchFieldDataType.Boolean));
                searchIndex = await searchServiceClient.CreateOrUpdateIndexAsync(indexForClient2);

                Console.WriteLine("Whoops; This shouldn't happen");
                Environment.Exit(1);
            }
            catch (RequestFailedException e) when (e.Status == 400)
            {
                Console.WriteLine("Client 2 failed to update the searchIndex, as expected.");
            }
            finally
            {
                // You can also use access conditions with Delete operations. For example, you can implement an
                // atomic version of the DeleteTestIndexIfExists method from this sample like this:
                Console.WriteLine("Deleting searchIndex...\n");
                await searchServiceClient.DeleteIndexAsync("test");

                // This is slightly better than using the Exists method since it makes only one round trip to
                // Azure Search instead of potentially two. It also avoids an extra Delete request in cases where
                // the resource is deleted concurrently, but this doesn't matter much since resource deletion in
                // Azure Search is idempotent.

                // And we're done! Bye!
                Console.WriteLine("Complete.  Press any key to end application...\n");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Configure the Azure Cognitive Search Endpoint and AdminApiKey in appsetting.json to create a SearchServicClient 
        /// which use to interact with Azure Cogntive Search Service.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private static SearchServiceClient GetSearchServiceClient(IConfigurationRoot configuration)
        {
            string endPoint = configuration["SearchServicEndpoint"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient searchServiceClient = new SearchServiceClient(new Uri(endPoint), new AzureKeyCredential(adminApiKey));

            return searchServiceClient;
        }

        private static SearchIndex DefineSearchIndex() =>
            new SearchIndex("test")
            {
                Fields = { new SimpleField("id", SearchFieldDataType.String) { IsKey = true } }
            };
    }
}