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
            _graph = CreateGraphServiceClient();

            // Create a group, a user and associate both
            List<string> groups = CreateUsersAndGroups().Result;

            // Create a cache that contains the users and the list of groups they are part of
            RefreshCache();

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
                _token = result.AccessToken;
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);
                return Task.FromResult(0);
            }));
        }

        private static async void RefreshCache()
        {
            HttpClient client = new HttpClient();

            string requestContent = BuildBatchRequest();
            //_token = "eyJ0eXAiOiJKV1QiLCJub25jZSI6IkFRQUJBQUFBQUFCSGg0a21TX2FLVDVYcmp6eFJBdEh6NHhGR3dzYkNJRlJySHYyT0RzMlZkLXR0b0V1MHp5ME42ZWZzbURUeDZPQl81X3l4ZVRjTDVTWDhwRVRjVkZrQzdtZUJSeGpFa0ZsSzJ3SUpDNzBjYVNBQSIsImFsZyI6IlJTMjU2IiwieDV0IjoiMktWY3V6cUFpZE9McVdTYW9sN3dnRlJHQ1lvIiwia2lkIjoiMktWY3V6cUFpZE9McVdTYW9sN3dnRlJHQ1lvIn0.eyJhdWQiOiJodHRwczovL2dyYXBoLm1pY3Jvc29mdC5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8xZmQ2NjM3MC1mZTU3LTQ5OWQtYmZkMy1iYWRlOWU4OTZmZTgvIiwiaWF0IjoxNTA4MTk5MDQ1LCJuYmYiOjE1MDgxOTkwNDUsImV4cCI6MTUwODIwMjk0NSwiYWNyIjoiMSIsImFpbyI6IkFTUUEyLzhHQUFBQXFxOEtwN011TXg1T1NNZmh2Ujk2N2ZESDRnWDhLMFl1M016d3I1NitpYUk9IiwiYW1yIjpbInB3ZCJdLCJhcHBfZGlzcGxheW5hbWUiOiJyZXZpdGFsYnRlc3R0b2tlbjIiLCJhcHBpZCI6ImExYjZmZjY2LWQwODctNDJkZC1hZjk5LTdkYWI1YjQzZTU2YiIsImFwcGlkYWNyIjoiMCIsImRldmljZWlkIjoiZDAzN2I5YTQtZTU0Zi00ZTdmLWI4N2ItOTA4YjVkZDhkNzkzIiwiZV9leHAiOjI2MjgwMCwiZmFtaWx5X25hbWUiOiJCYXJsZXR6IiwiZ2l2ZW5fbmFtZSI6IlJldml0YWwiLCJpcGFkZHIiOiIxNjcuMjIwLjAuMTk0IiwibmFtZSI6IlJldml0YWwgQmFybGV0eiIsIm9pZCI6ImY2YWUyOGI5LWRhOWItNDNlOC05YjUyLTE4OGE5N2RmNmEzOSIsInBsYXRmIjoiMyIsInB1aWQiOiIxMDAzM0ZGRkE0MEJBMEFDIiwic2NwIjoiRGlyZWN0b3J5LkFjY2Vzc0FzVXNlci5BbGwgRGlyZWN0b3J5LlJlYWQuQWxsIERpcmVjdG9yeS5SZWFkV3JpdGUuQWxsIEdyb3VwLlJlYWQuQWxsIFVzZXIuUmVhZCIsInNpZ25pbl9zdGF0ZSI6WyJrbXNpIl0sInN1YiI6ImljNnV2Ni1rc3FwOFNzV1VJblExajYtZHNtRzFES1praTVoQlhtcFZKUFEiLCJ0aWQiOiIxZmQ2NjM3MC1mZTU3LTQ5OWQtYmZkMy1iYWRlOWU4OTZmZTgiLCJ1bmlxdWVfbmFtZSI6InJldml0YWxiQHJldml0YWxzLm9ubWljcm9zb2Z0LmNvbSIsInVwbiI6InJldml0YWxiQHJldml0YWxzLm9ubWljcm9zb2Z0LmNvbSIsInV0aSI6InNFZnRWRFVJSlVLdUdzeDc4cGRKQUEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbIjYyZTkwMzk0LTY5ZjUtNDIzNy05MTkwLTAxMjE3NzE0NWUxMCJdfQ.IErAycrSRgZG-PallPOYJNS_jo_6h_KDuzTEhhbweCq7OYEGEFqrachF3H5ZjiT75LKwyaCm7zXMfqB4L76xt_EykjD7epKegeEIMHvGnWLYt8E7C_Wg2egmV375EFCi6HFaGfVmSSfOufsbJevSAiSPhn7wjFnGnIzGSYD3fnCWC9IVX5__ceec-gf98d-xWXaUWopCMaxaMKwIEUNwSewuVQHE-ayJhwNDU4tUIGw1msnbC5PzpUPkkAeWSP1joV3FqNeiUjYuSdpLMWt9Uw5gGEsx6v1qlwlIVInRBsN792T19Gx35FB9COI6KZ9jo_h2UY0b0eVJbhR2DiAHOQ";
            string responseString = await GetResponseContent(client, requestContent);

            Result data = JsonConvert.DeserializeObject<Result>(responseString);
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

        private static async Task<string> GetResponseContent(HttpClient client, string requestContent)
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
            //requestsBody += string.Format(requestsBodyFormat, "3", "revitalb@revitals.onmicrosoft.com");
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
                            FileId = "11",
                            Size = 126,
                            Name = "secured_file_a",
                            Type = "txt",
                            GroupIds = new[] { groups[0] }
                        }),
                    IndexAction.Upload(
                        new SecuredFiles()
                        {
                            FileId = "12",
                            Size = 1266,
                            Name = "secured_file_b",
                            Type = "txt",
                            GroupIds = new[] { groups[0] }
                        }),
                    IndexAction.Upload(
                        new SecuredFiles()
                        {
                            FileId = "13",
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

        private static async Task<List<string>> CreateUsersAndGroups()
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
                Group newGroup = await _graph.Groups.Request().AddAsync(group);
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
                User newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                user = new User();
                user.GivenName = "Second User";
                user.Surname = "User2";
                user.MailNickname = "User2";
                user.DisplayName = "Second User";
                user.UserPrincipalName = UsersPrincipalNameGroupA[1];
                user.PasswordProfile = new PasswordProfile() { Password = "Test2Test" };
                user.AccountEnabled = true;
                // Create AAD user
                newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                group = new Group();
                group.DisplayName = "My Second Prog Group";
                group.SecurityEnabled = true;
                group.MailEnabled = false;
                group.MailNickname = "group2";
                newGroup = await _graph.Groups.Request().AddAsync(group);
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
                var allUserGroupsRequest = _graph.Users["revitalb@revitals.onmicrosoft.com"].MemberOf.Request();

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
