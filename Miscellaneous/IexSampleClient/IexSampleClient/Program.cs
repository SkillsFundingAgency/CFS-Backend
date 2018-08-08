using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IexSampleClient
{
    class Program
    {
        // Azure Active Directory Details
        private const string Authority = "https://login.microsoftonline.com/";
        private const string TenantId = "fad277c9-c60a-4da1-b5f3-b3b8b34a82f9";

        // Azure Active Directory Client Application Details
        private const string ClientId = "b3b2a71e-842f-4b01-9c6f-72def5020450";
        private const string ClientSecret = "0CNLxgvaEwsJQPi2W/KnXVQ6BpQ8GidzivAl5F0rlSo=";
        private const string AppIdUri = "https://calculatefundingserviceapidev";

        // CFS Details
        private const string CFS_Endpoint = "https://localhost:5009/";

        static void Main(string[] args)
        {
            try
            {
                CallCFS().Wait();
            }
            catch (AggregateException aEx)
            {
                Console.WriteLine(">>> Exception: {0}", aEx.GetBaseException().Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Exception: {0}", ex.Message);
            }
            finally
            {
                Console.WriteLine("\n\nFinished. Press [Enter] to exit");
                Console.ReadLine();
            }
        }

        private static async Task CallCFS()
        {
            using (HttpClient cfsClient = new HttpClient())
            {
                cfsClient.BaseAddress = new Uri(CFS_Endpoint);

                // Note: this is a simple example, should cache tokens and only refetch when expire
                string accessToken = await GetAccessToken();
                cfsClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));

                Console.WriteLine("Calling CFS Endpoint");
                HttpResponseMessage result = await cfsClient.GetAsync("/api/periods");

                if (result.IsSuccessStatusCode)
                {
                    string body = await result.Content.ReadAsStringAsync();
                    List<TimePeriod> periods = JsonConvert.DeserializeObject<List<TimePeriod>>(body);

                    Console.WriteLine("+ Call to the endpoint succeeded - found {0} time periods", periods.Count);
                }
                else
                {
                    Console.WriteLine("- Call to the endpoint failed. Status Code: {0}, Reason: {1}", result.StatusCode, result.ReasonPhrase);
                }
            }
        }

        private static async Task<string> GetAccessToken()
        {
            AuthenticationContext authContext = new AuthenticationContext(string.Format("{0}{1}", Authority, TenantId));
            ClientCredential clientCredential = new ClientCredential(ClientId, ClientSecret);
            AuthenticationResult authenticationResult = await authContext.AcquireTokenAsync(AppIdUri, clientCredential);

            return authenticationResult.AccessToken;
        }
    }
}
