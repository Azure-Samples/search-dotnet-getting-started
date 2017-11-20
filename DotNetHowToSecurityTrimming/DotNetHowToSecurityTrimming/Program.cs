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
using Microsoft.Graph;

namespace DotNetHowToSecurityTrimming
{
    static class Program
    {
        public static string ClientId;

        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;

        private static ConcurrentDictionary<string, List<string>> _groupsCache = new ConcurrentDictionary<string, List<string>>();
        private static MicrosoftGraphHelper _microsoftGraphHelper;

        // This sample shows how to use Azure Active Directory (AAD) together with Azure Search to restrict document access based on user group membership through Azure Search filters.
        static void Main(string[] args)
        {
            // Application Id as obtained by creating an application from https://apps.dev.microsoft.com
            // See also the guided setup:https://docs.microsoft.com/en-us/azure/active-directory/develop/guidedsetups/active-directory-windesktop
            ClientId = ConfigurationManager.AppSettings["ClientId"];
            _microsoftGraphHelper = new MicrosoftGraphHelper(ClientId);
            _microsoftGraphHelper.CreateGraphServiceClient();
            string tenant = ConfigurationManager.AppSettings["Tenant"];
            
            // Azure Search Initialization
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];
            string indexName = "securedfiles";

            Dictionary<Group, List<User>> groups = CreateGroupsWithUsers(tenant);

            // Create a group, a user and associate both
            _microsoftGraphHelper.CreateUsersAndGroups(groups).Wait();

            // Create a cache that contains the users and the list of groups they are part of
            Console.WriteLine("Refresh cache...\n");
            var users = groups.SelectMany(u => u.Value);
            RefreshCache(users);

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
            IndexDocuments(indexName, groups.Keys.Select(g => g.Id).ToList());

            foreach (var user in users)
            {
                // Retrieve user's groups so that a search filter could be built using the groups list
                Console.WriteLine("Get groups for user {0}...\n", user.UserPrincipalName);
                RefreshCacheIfRequired(user.UserPrincipalName);
                SearchQueryWithFilter(user.UserPrincipalName);
            }
        }

        private static Dictionary<Group, List<User>> CreateGroupsWithUsers(string tenant)
        {
            Group group = new Group()
            {
                DisplayName = "My First Prog Group",
                SecurityEnabled = true,
                MailEnabled = false,
                MailNickname = "group1"
            };

            User user1 = new User()
            {
                GivenName = "First User",
                Surname = "User1",
                MailNickname = "User1",
                DisplayName = "First User",
                UserPrincipalName = String.Format("user1@{0}", tenant),
                PasswordProfile = new PasswordProfile() { Password = "********" },
                AccountEnabled = true
            };
            User user2 = new User()
            {
                GivenName = "Second User",
                Surname = "User2",
                MailNickname = "User2",
                DisplayName = "Second User",
                UserPrincipalName = String.Format("user2@{0}", tenant),
                PasswordProfile = new PasswordProfile() { Password = "********" },
                AccountEnabled = true
            };

            List<User> users = new List<User>() { user1, user2 };
            Dictionary<Group, List<User>> groups = new Dictionary<Group, List<User>>() { { group, users } };

            group = new Group()
            {
                DisplayName = "My Second Prog Group",
                SecurityEnabled = true,
                MailEnabled = false,
                MailNickname = "group2"
            };

            User user3 = new User()
            {
                GivenName = "Third User",
                Surname = "User3",
                MailNickname = "User3",
                DisplayName = "Third User",
                UserPrincipalName = String.Format("user3@{0}", tenant),
                PasswordProfile = new PasswordProfile() { Password = "********" },
                AccountEnabled = true
            };

            groups.Add(group, new List<User>() { user3 });

            return groups;
        }

        private static async void RefreshCacheIfRequired(string user)
        {
            if (!_groupsCache.ContainsKey(user))
            {
                var groups = await _microsoftGraphHelper.GetGroupIdsForUser(user);
                _groupsCache[user] = groups;
            }
        }
        
        private static async void RefreshCache(IEnumerable<User> users)
        {
            HttpClient client = new HttpClient();
            var userGroups = await _microsoftGraphHelper.GetGroupsForUsers(client, users);
            _groupsCache = new ConcurrentDictionary<string, List<string>>(userGroups);
        }

        private static void SearchQueryWithFilter(string user)
        {
            // Using the filter below, the search result will contain all documents that their GroupIds field contain any one of the 
            // Ids in the groups list
            string filter = String.Format("groupIds/any(p:search.in(p, '{0}'))", string.Join(",", String.Join(",", _groupsCache[user])));
            SearchParameters parameters =
                new SearchParameters()
                {
                    Filter = filter,
                    Select = new[] { "name" }
                };

            DocumentSearchResult<SecuredFiles> results = _indexClient.Documents.Search<SecuredFiles>("*", parameters);

            Console.WriteLine("Results for groups '{0}' : {1}", _groupsCache[user], results.Results.Select(r => r.Document.Name));
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
