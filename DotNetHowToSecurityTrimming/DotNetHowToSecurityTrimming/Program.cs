#define HowToExample

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace DotNetHowToSecurityTrimming
{
    static class Program
    {
        public static string ClientId;

        private static List<string> UsersPrincipalNameGroupA;

        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;

        private static ConcurrentDictionary<string, List<string>> _groupsCache = new ConcurrentDictionary<string, List<string>>();
        private static MicrosoftGraphHelper _microsoftGraphHelper;

        static void Main(string[] args)
        {
            // Application Id as obtained by creating an application from https://apps.dev.microsoft.com
            // See also the guided setup:https://docs.microsoft.com/en-us/azure/active-directory/develop/guidedsetups/active-directory-windesktop
            ClientId = ConfigurationManager.AppSettings["ClientId"];
            _microsoftGraphHelper = new MicrosoftGraphHelper(ClientId);
            string tenant = ConfigurationManager.AppSettings["Tenant"];
            UsersPrincipalNameGroupA = new List<string>()
            {
                String.Format("user1@{0}", tenant),
                String.Format("user2@{0}", tenant),
                String.Format("user3@{0}", tenant)
            };
            
            // Azure Search Initialization
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];
            string indexName = "securedfiles";

            // Create a group, a user and associate both
            List<string> groups = _microsoftGraphHelper.CreateUsersAndGroups(UsersPrincipalNameGroupA).Result;

            // Create a cache that contains the users and the list of groups they are part of
            Console.WriteLine("Refresh cache...\n");
            RefreshCache();

            // Create an HTTP reference to the catalog index
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(apiKey));

            if (DeleteIndex(indexName))
            {
                Console.WriteLine("Creating index...\n");
                CreateIndex(indexName);
            }

            Console.WriteLine("Indexing documents...\n");
            // Index documents with relevant group ids
            IndexDocuments(indexName, groups);

            foreach (var user in UsersPrincipalNameGroupA)
            {
                // Retrieve user's groups so that a search filter could be built using the groups list
                Console.WriteLine("Get groups for user {0}...\n", user);
                RefreshCacheIfRequired(user);
                SearchQueryWithFilter(user);
            }
        }

        private static async void RefreshCacheIfRequired(string user)
        {
            if (!_groupsCache.ContainsKey(user))
            {
                var groups = await _microsoftGraphHelper.GetGroupIdsForUser(user);
                _groupsCache[user] = groups;
            }
        }
        
        private static async void RefreshCache()
        {
            HttpClient client = new HttpClient();
            string responseString = await _microsoftGraphHelper.SendRequestAndGetResponse(client, UsersPrincipalNameGroupA);

            BatchResult data = JsonConvert.DeserializeObject<BatchResult>(responseString);
            // Clear existing cache as new groups were retrieved
            _groupsCache.Clear();
            for (int i = 0; i < data.Responses.Count(); i++)
            {
                List<string> userGroups = new List<string>();
                for (int j = 0; j < data.Responses[i].Body.Value.Count(); j++)
                {
                    BatchResponseBodyValue value = data.Responses[i].Body.Value[j];
                    if (value.Type == "#microsoft.graph.group")
                    {
                        userGroups.Add(value.Id);
                    }
                }
                int id = Convert.ToInt32(data.Responses[i].Id);
                string key = UsersPrincipalNameGroupA[id];
                _groupsCache[key] = userGroups;
            }
        }

        private static void SearchQueryWithFilter(string user)
        {
            SearchParameters parameters;
            DocumentSearchResult<SecuredFiles> results;
            // Using the filter below, the search result will contain all documents, that their GroupIds field contain any one of the 
            // Ids in the groups list
            string filter = String.Format("groupIds/any(p:search.in(p, '{0}'))", string.Join(",", String.Join(",", _groupsCache[user])));
            parameters =
                new SearchParameters()
                {
                    Filter = filter,
                    Select = new[] { "name" }
                };

            results = _indexClient.Documents.Search<SecuredFiles>("*", parameters);

            Console.WriteLine("Results for groups '{0}' : {1}", _groupsCache[user], results.Results.Select(r => r.Document));
        }

        private static void IndexDocuments(string indexName, List<string> groups)
        {
            var actions = new IndexAction<SecuredFiles>[]
                            {
                                IndexAction.Upload(
                                    new SecuredFiles()
                                    {
                                        FileId = "1",
                                        Name = "secured_file_a",
                                        GroupIds = new[] { groups[0] }
                                    }),
                                IndexAction.Upload(
                                    new SecuredFiles()
                                    {
                                        FileId = "2",
                                        Name = "secured_file_b",
                                        GroupIds = new[] { groups[0] }
                                    }),
                                IndexAction.Upload(
                                    new SecuredFiles()
                                    {
                                        FileId = "3",
                                        Name = "secured_file_c",
                                        GroupIds = new[] { groups[1] }
                                    })
                            };

            var batch = IndexBatch.New(actions);

            try
            {
                _indexClient.Documents.Index(batch);
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

        private static bool DeleteIndex(string indexName)
        {
            try
            {
                _searchClient.Indexes.Delete(indexName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting index: {0}\r\n", ex.Message);
                Console.WriteLine("Did you remember to add your SearchServiceName and SearchServiceApiKey to the app.config?\r\n");
                return false;
            }

            return true;
        }

        private static void CreateIndex(string indexName)
        {
            // Create the Azure Search index based on the included schema
            try
            {
                var definition = new Index()
                {
                    Name = indexName,
                    Fields = FieldBuilder.BuildForType<SecuredFiles>()
                };

                _searchClient.Indexes.Create(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}\r\n", ex.Message);
                throw;
            }
        }
    }
}
