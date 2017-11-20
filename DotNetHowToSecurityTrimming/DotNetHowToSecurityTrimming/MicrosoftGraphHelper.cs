#define HowToExample

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace DotNetHowToSecurityTrimming
{
    public class MicrosoftGraphHelper
    {
        private GraphServiceClient _graph;
        private string _clientId;
        private string _token;

        public MicrosoftGraphHelper(string clientId)
        {
            _clientId = clientId;
        }

        public void CreateGraphServiceClient()
        {
            PublicClientApplication app = new PublicClientApplication(_clientId);
            string[] scopes = { "User.ReadWrite.All", "Group.ReadWrite.All", "Directory.ReadWrite.All" };

            // Instantiate the Microsoft Graph, and provide a way to acquire the token. If token expires, it will
            // be acquired again
            _graph = new GraphServiceClient(new DelegateAuthenticationProvider(
            async (requestMessage) =>
            {
                AuthenticationResult result = null;
                // If a user has already signed-in, we try first to acquire the token silently, and then if this fails
                // we try to acquire it with a user interaction.
                // In this sample there is one user that is signed-in and therefore we choose the first element.
                var user = app.Users.FirstOrDefault();
                if (user != null)
                {
                    try
                    {
                        // Attempts to acquire the access token from cache
                        result = await app.AcquireTokenSilentAsync(scopes, user);
                    }
                    catch (MsalClientException ex)
                    {
                        if (ex.ErrorCode == "interaction_required")
                        {
                            // Interactive request to acquire token
                            result = await app.AcquireTokenAsync(scopes);
                        }
                    }
                }
                else
                {
                    result = await app.AcquireTokenAsync(scopes);
                }
                _token = result.AccessToken;
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);
            }));
        }

        private string BuildGetMemberGroupsRequest(IEnumerable<User> users)
        {
            string requestBodyFormat = @"{{ ""requests"": [ {0} ]  }}";
            string requestsBodyFormat = @"{{
                    ""id"":""{0}"",
                    ""method"":""POST"",
                    ""url"":""users/{1}/microsoft.graph.getMemberGroups"",
                    ""body"": {{
                        ""securityEnabledOnly"":true
                    }},
                    ""headers"": {{
                        ""Content-Type"":""application/json""
                    }}
                }},";

            string requestsBody = null;
            for (int i = 0; i < users.Count(); i++)
            {
                requestsBody += string.Format(requestsBodyFormat, i, users.ElementAt(i).UserPrincipalName);
            }
            return string.Format(requestBodyFormat, requestsBody);
        }

        public async Task<Dictionary<string, List<string>>> GetGroupsForUsers(HttpClient client, IEnumerable<User> users)
        {
            string requestContent = BuildGetMemberGroupsRequest(users);
            client.DefaultRequestHeaders.Add("Authorization", string.Format("bearer {0}", _token));
            var stringContent = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://graph.microsoft.com/beta/$batch", stringContent);

            var responseString = await response.Content.ReadAsStringAsync();
            return ParseBatchGroupsForUsersResponse(users, responseString);
        }

        private static Dictionary<string, List<string>> ParseBatchGroupsForUsersResponse(IEnumerable<User> users, string responseString)
        {
            BatchResult data = JsonConvert.DeserializeObject<BatchResult>(responseString);

            Dictionary<string, List<string>> userGroupsMapping = new Dictionary<string, List<string>>();
            for (int i = 0; i < data.Responses.Count(); i++)
            {
                List<string> userGroups = new List<string>();
                for (int j = 0; j < data.Responses[i].Body.Value.Count(); j++)
                {
                    string value = data.Responses[i].Body.Value[j];
                    userGroups.Add(value);
                }
                int id = Convert.ToInt32(data.Responses[i].Id);
                string key = users.ElementAt(id).UserPrincipalName;
                userGroupsMapping[key] = userGroups;
            }

            return userGroupsMapping;
        }

        public async Task CreateUsersAndGroups(Dictionary<Group, List<User>> groups)
        {
            try
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    // Create AAD group
                    KeyValuePair<Group, List<User>> group = groups.ElementAt(i);
                    Group currentGroup = groups.Keys.ElementAt(i);
                    Group newGroup = await _graph.Groups.Request().AddAsync(currentGroup);
                    currentGroup.Id = newGroup.Id;
                    foreach (var user in group.Value)
                    {
                        // Create AAD user
                        User newUser = await _graph.Users.Request().AddAsync(user);
                        // Associate user with group
                        await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUser);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating users and groups: {0}\r\n", ex.Message);
                throw;
            }
        }

        public async Task<List<string>> GetGroupIdsForUser(string userPrincipalName)
        {
            List<string> groups = new List<string>();
            try
            {
                // Gets the request builder for MemberOf and build the request
                var allUserGroupsRequest = _graph.Users[userPrincipalName].GetMemberGroups(true).Request();

                while (allUserGroupsRequest != null)
                {
                    // Invoke the get request
                    var allUserGroups = await allUserGroupsRequest.PostAsync();
                    groups = allUserGroups.Select(g => g).ToList();
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
