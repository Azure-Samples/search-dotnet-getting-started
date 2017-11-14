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

        private string BuildBatchRequest(List<string> users)
        {
            string requestBodyFormat = @"{{ ""requests"": [ {0} ]  }}";
            string requestsBodyFormat = @"{{
                    ""id"":""{0}"",
                    ""method"":""POST"",
                    ""url"":""users/{1}/microsoft.graph.getMemberGroups"",
                    ""body"": {
                        ""securityEnabledOnly"":true
                    },
                    ""headers"": {
                        ""Content-Type"":""application/json""
                    }
                }},";

            string requestsBody = null;
            for (int i = 0; i < users.Count; i++)
            {
                requestsBody += string.Format(requestsBodyFormat, i, users[i]);
            }
            return string.Format(requestBodyFormat, requestsBody);
        }

        public async Task<string> SendRequestAndGetResponse(HttpClient client, List<string> users)
        { 
            string requestContent = BuildBatchRequest(users);
            client.DefaultRequestHeaders.Add("Authorization", string.Format("bearer {0}", _token));
            var stringContent = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://graph.microsoft.com/beta/$batch", stringContent);

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        public async Task<List<string>> CreateUsersAndGroups(List<string> users)
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
                    UserPrincipalName = users[0],
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
                    UserPrincipalName = users[1],
                    PasswordProfile = new PasswordProfile() { Password = "********" },
                    AccountEnabled = true
                };
                // Create AAD user
                newUSer = await _graph.Users.Request().AddAsync(user);
                // Associate user with group
                await _graph.Groups[newGroup.Id].Members.References.Request().AddAsync(newUSer);

                group = new Group()
                {
                    DisplayName = "My Second Prog Group",
                    SecurityEnabled = true,
                    MailEnabled = false,
                    MailNickname = "group2"
                };
                // Create AAD group
                newGroup = await _graph.Groups.Request().AddAsync(group);
                groups.Add(newGroup.Id);

                user = new User()
                {
                    GivenName = "Third User",
                    Surname = "User3",
                    MailNickname = "User3",
                    DisplayName = "Third User",
                    UserPrincipalName = users[2],
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
