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
using System.Net.Http;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using Newtonsoft.Json.Converters;

namespace DotNetHowToSecurityTrimming
{
    static class Program
    {
        public static string ClientId;

        private static List<string> UsersPrincipalNameGroupA;
        private static List<string> UsersPrincipalNameGroupB;

        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;

        private static string _token;

        private static ConcurrentDictionary<string, List<string>> _groupsCache = new ConcurrentDictionary<string, List<string>>();
        private static GraphServiceClient _graph;

        static void Main(string[] args)
        {
            // Application Id as obtained by creating an application from https://apps.dev.microsoft.com
            // See also the guided setup:https://docs.microsoft.com/en-us/azure/active-directory/develop/guidedsetups/active-directory-windesktop
            ClientId = ConfigurationManager.AppSettings["ClientId"];
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

            // AAD Initialization
            _graph = CreateGraphServiceClient();

            // Create a group, a user and associate both
            List<string> groups = CreateUsersAndGroups().Result;

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

        private static void RefreshCacheIfRequired(string user)
        {
            if (!_groupsCache.ContainsKey(user))
            {
                var groups = GetGroupIdsForUser(user).Result;
                _groupsCache[user] = groups;
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
                // If a user has already signed-in, we try first to acquire the token silently, and then if this fails
                // we try to acquire it with a user interaction.
                var user = app.Users.FirstOrDefault();
                if (user != null)
                {
                    try
                    {
                        // Attempts to acquire the access token from cache
                        result = app.AcquireTokenSilentAsync(scopes, user).Result;
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
                _token = result.AccessToken;
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);
                return Task.FromResult(0);
            }));
        }

        private static async void RefreshCache()
        {
            HttpClient client = new HttpClient();
            // Get all the groups for the existing users
            string requestContent = BuildBatchRequest();
            string responseString = await SendRequestAndGetResponse(client, requestContent);

            Result data = JsonConvert.DeserializeObject<Result>(responseString);
            // Clear existing cache as new groups were retrieved
            _groupsCache.Clear();
            for (int i = 0; i < data.Responses.Count(); i++)
            {
                List<string> userGroups = new List<string>();
                for (int j = 0; j < data.Responses[i].Body.Value.Count(); j++)
                {
                    ResponseBodyValue value = data.Responses[i].Body.Value[j];
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

        private static async Task<string> SendRequestAndGetResponse(HttpClient client, string requestContent)
        {
            client.DefaultRequestHeaders.Add("Authorization", string.Format("bearer {0}", _token));
            var stringContent = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://graph.microsoft.com/beta/$batch", stringContent);

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        private static string BuildBatchRequest()
        {
            string requestBodyFormat = @"{{ ""requests"": [ {0} ]  }}";
            string requestsBodyFormat = @"{{
                    ""id"":""{0}"",
                    ""method"":""GET"",
                    ""url"":""users/{1}/memberOf""
                }},";

            string requestsBody = null;
            for (int i = 0; i < UsersPrincipalNameGroupA.Count; i++)
            {
                requestsBody += string.Format(requestsBodyFormat, i, UsersPrincipalNameGroupA[i]);
            }
            return string.Format(requestBodyFormat, requestsBody);
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

            Console.WriteLine("Results: {0} : {1}", results.Results.Select(r => r.Document));
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

        private static async Task<List<string>> CreateUsersAndGroups()
        {
            List<string> groups = new List<string>();

            try
            {
                Group group = new Group()
                {
                    DisplayName = "My First Prog Group",
                    SecurityEnabled = true,
                    MailEnabled = false,
                    MailNickname = "group1"
                };
                // Create AAD group
                Group newGroup = await _graph.Groups.Request().AddAsync(group);
                groups.Add(newGroup.Id);

                User user = new User()
                {
                    GivenName = "First User",
                    Surname = "User1",
                    MailNickname = "User1",
                    DisplayName = "First User",
                    UserPrincipalName = "User1@FirstUser.com",
                    PasswordProfile = new PasswordProfile() { Password = "********" },
                    AccountEnabled = true
                };
                // Create AAD user
                User newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                user = new User()
                {
                    GivenName = "Second User",
                    Surname = "User2",
                    MailNickname = "User2",
                    DisplayName = "Second User",
                    UserPrincipalName = "User2@FirstUser.com",
                    PasswordProfile = new PasswordProfile() { Password = "********" },
                    AccountEnabled = true
                };
                // Create AAD user
                newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                group = new Group();
                group.DisplayName = "My Second Prog Group";
                group.SecurityEnabled = true;
                group.MailEnabled = false;
                group.MailNickname = "group2";
                // Create AAD group
                newGroup = await _graph.Groups.Request().AddAsync(group);
                groups.Add(newGroup.Id);

                user = new User()
                {
                    GivenName = "Third User",
                    Surname = "User3",
                    MailNickname = "User3",
                    DisplayName = "Third User",
                    UserPrincipalName = "User3@FirstUser.com",
                    PasswordProfile = new PasswordProfile() { Password = "********" },
                    AccountEnabled = true
                };
                // Create AAD user
                newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating users and groups: {0}\r\n", ex.Message);
                throw;
            }
            return groups;
        }

        private static async Task<List<string>> GetGroupIdsForUser(string userPrincipalName)
        {
            List<string> groups = new List<string>();
            try
            {
                // Gets the request builder for MemberOf and build the request
                var allUserGroupsRequest = _graph.Users[userPrincipalName].MemberOf.Request();

                while (allUserGroupsRequest != null)
                {
                    // Invoke the get request
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
