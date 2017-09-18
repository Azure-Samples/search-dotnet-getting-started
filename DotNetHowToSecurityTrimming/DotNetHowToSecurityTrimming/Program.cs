#define HowToExample

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace DotNetHowToSecurityTrimming
{
    static class Program
    {
        public static string ClientId;

        private static List<string> UsersPrincipalNameGroupA;
        private static List<string> UsersPrincipalNameGroupB;

        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;

        static void Main(string[] args)
        {
            // Application Id as obtained by creating an application from https://apps.dev.microsoft.com
            // See also the guided setup:https://docs.microsoft.com/en-us/azure/active-directory/develop/guidedsetups/active-directory-windesktop
            ClientId = ConfigurationManager.AppSettings["ClientId"];
            string tenant = ConfigurationManager.AppSettings["Tenant"];
            UsersPrincipalNameGroupA = new List<string>()
            {
                String.Format("user1@{0}", tenant),
                String.Format("user2@{0}", tenant)
            };

            UsersPrincipalNameGroupB = new List<string>()
            {
                String.Format("user3@{0}", tenant)
            };

            // Azure Search Initialization
            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];
            string indexName = "securedfiles";

            // AAD Initialization
            GraphServiceClient graph = CreateGraphServiceClient();

            // Create a group, a user and associate both
            List<string> groups = CreateUsersAndGroups(graph).Result;

            // Create an HTTP reference to the catalog index
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(apiKey));

            if (DeleteIndexingResources(indexName))
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
                var groupsIdForUser = GetGroupIdsForUser(graph, user).Result;
                SearchQueryWithFilter(groupsIdForUser);
            }
        }

        private static GraphServiceClient CreateGraphServiceClient()
        {
            PublicClientApplication app = new PublicClientApplication(ClientId);
            string[] scopes = { "User.Read", "Group.Read.All", "Directory.Read.All", "Directory.AccessAsUser.All", "Directory.ReadWrite.All" };

            // Instantiate the Microsoft Graph, and provide a way to acquire the token. If token expires, it will
            // be acquired again
            return new GraphServiceClient(new DelegateAuthenticationProvider(
            (requestMessage) =>
            {
                AuthenticationResult result = null;
                var u = app.Users.FirstOrDefault();
                if (u != null)
                {
                    try
                    {
                        // Attempts to acquire the access token from cache
                        result = app.AcquireTokenSilentAsync(scopes, app.Users.FirstOrDefault()).Result;
                    }
                    catch (MsalClientException ex)
                    {
                        if (ex.ErrorCode == "interaction_required")
                        {
                            // Interactive request to acquire token
                            result = app.AcquireTokenAsync(scopes).Result;
                        }
                    }
                }
                else
                {
                    result = app.AcquireTokenAsync(scopes).Result;
                }
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
                return Task.FromResult(0);
            }));
        }

        private static void SearchQueryWithFilter(List<string> groups)
        {
            SearchParameters parameters;
            DocumentSearchResult<SecuredFiles> results;
            // Using the filter below, the search result will contain all documents, that their GroupIds field contain any one of the 
            // Ids in the groups list
            string filter = String.Format("groupIds/any(p:search.in(p, '{0}'))", string.Join(",", groups.Select(g => g.ToString())));
            parameters =
                new SearchParameters()
                {
                    Filter = filter,
                    Select = new[] { "name" }
                };

            results = _indexClient.Documents.Search<SecuredFiles>("*", parameters);

            Console.WriteLine("Results: {0}", results.Results.Select(r => r.Document));
        }

        private static void IndexDocuments(string indexName, List<string> groups)
        {
            var actions =
                new IndexAction<SecuredFiles>[]
                {
                    IndexAction.Upload(
                        new SecuredFiles()
                        {
                            FileId = "1",
                            Size = 126,
                            Name = "secured_file_a",
                            Type = "txt",
                            GroupIds = new[] { groups[0] }
                        }),
                    IndexAction.Upload(
                        new SecuredFiles()
                        {
                            FileId = "2",
                            Size = 1266,
                            Name = "secured_file_b",
                            Type = "txt",
                            GroupIds = new[] { groups[0] }
                        }),
                    IndexAction.Upload(
                        new SecuredFiles()
                        {
                            FileId = "3",
                            Size = 140,
                            Name = "secured_file_c",
                            Type = "txt",
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

        private static bool DeleteIndexingResources(string indexName)
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

        private static async Task<List<string>> CreateUsersAndGroups(GraphServiceClient graph)
        {
            List<string> groups = new List<string>();

            try
            {
                Group group = new Group();
                group.DisplayName = "My First Prog Group";
                group.SecurityEnabled = true;
                group.MailEnabled = false;
                group.MailNickname = "group1";
                // Create AAD group
                Group newGroup = await graph.Groups.Request().AddAsync(group);
                groups.Add(newGroup.Id);

                User user = new User();
                user.GivenName = "First User";
                user.Surname = "User1";
                user.MailNickname = "User1";
                user.DisplayName = "First User";
                user.UserPrincipalName = UsersPrincipalNameGroupA[0];
                user.PasswordProfile = new PasswordProfile() { Password = "Test1Test" };
                user.AccountEnabled = true;
                // Create AAD user
                User newUSer = await graph.Users.Request().AddAsync(user);
                // Associate user with group
                await graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                user = new User();
                user.GivenName = "Second User";
                user.Surname = "User2";
                user.MailNickname = "User2";
                user.DisplayName = "Second User";
                user.UserPrincipalName = UsersPrincipalNameGroupA[1];
                user.PasswordProfile = new PasswordProfile() { Password = "Test2Test" };
                user.AccountEnabled = true;
                // Create AAD user
                newUSer = await graph.Users.Request().AddAsync(user);
                // Associate user with group
                await graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                group = new Group();
                group.DisplayName = "My Second Prog Group";
                group.SecurityEnabled = true;
                group.MailEnabled = false;
                group.MailNickname = "group2";
                newGroup = await graph.Groups.Request().AddAsync(group);
                // Create AAD group
                groups.Add(newGroup.Id);

                user = new User();
                user.GivenName = "Third User";
                user.Surname = "User3";
                user.MailNickname = "User3";
                user.DisplayName = "Third User";
                user.UserPrincipalName = UsersPrincipalNameGroupB[0];
                user.PasswordProfile = new PasswordProfile() { Password = "Test3Test" };
                user.AccountEnabled = true;
                // Create AAD user
                newUSer = await graph.Users.Request().AddAsync(user);
                // Associate user with group
                await graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating users and groups: {0}\r\n", ex.Message);
                throw;
            }
            return groups;
        }

        private static async Task<List<string>> GetGroupIdsForUser(GraphServiceClient graph, string userPrincipalName)
        {
            List<string> groups = new List<string>();
            try
            {
                var allUserGroupsRequest = graph.Users[userPrincipalName].MemberOf.Request();

                while (allUserGroupsRequest != null)
                {
                    var allUserGroups = await allUserGroupsRequest.GetAsync();
                    foreach (Group group in allUserGroups)
                    {
                        groups.Add(group.Id);
                    }
                    allUserGroupsRequest = allUserGroups.NextPageRequest;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving groups: {0}\r\n", ex.Message);
                throw;
            }
            return groups;
        }
    }
}
