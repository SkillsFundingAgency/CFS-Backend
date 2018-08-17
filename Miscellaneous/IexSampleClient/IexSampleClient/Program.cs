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
        private const string TenantId = "<replace>";

        // Azure Active Directory Client Application Details
        private const string ClientId = "<replace>";
        private const string ClientSecret = "<replace>";
        private const string AppIdUri = "<replace>";

        // CFS Details
        private const string CFS_Endpoint = "<replace>";


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

                cfsClient.DefaultRequestHeaders.Add("Accept", "application/json");
                // Note: this is a simple example, should cache tokens and only refetch when expire
                string accessToken = await GetAccessToken();
                cfsClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));

                Console.WriteLine("\n\r===============================================================================================================================--");
                Console.WriteLine("Calling CFS Funding Streams Endpoint");
                Console.WriteLine("=====================================================================================================================================\n\r");
                
                HttpResponseMessage result = await cfsClient.GetAsync("api/providers/ukprn/103929/startYear/2018/endYear/2019/fundingStreams/PES/summary");

                if (result.IsSuccessStatusCode)
                {
                    string body = await result.Content.ReadAsStringAsync();

                    Console.WriteLine(FormatJson(body));
                }
                else
                {
                    Console.WriteLine("- Call to the endpoint failed. Status Code: {0}, Reason: {1}", result.StatusCode, result.ReasonPhrase);
                }

                Console.WriteLine("\n\r====================================================================================================================================");
                Console.WriteLine("Calling CFS Allocation lines Endpoint");
                Console.WriteLine("========================================================================================================================================\n\r");
                
                result = await cfsClient.GetAsync("api/providers/ukprn/103929/startYear/2018/endYear/2019/allocationLines/PES02/summary");

                if (result.IsSuccessStatusCode)
                {
                    string body = await result.Content.ReadAsStringAsync();

                    Console.WriteLine(FormatJson(body));
                }
                else
                {
                    Console.WriteLine("- Call to the endpoint failed. Status Code: {0}, Reason: {1}", result.StatusCode, result.ReasonPhrase);
                }

                Console.WriteLine("\n\r==================================================================================================================================");
                Console.WriteLine("Calling CFS Local authority Endpoint");
                Console.WriteLine("========================================================================================================================================\n\r");
               
                result = await cfsClient.GetAsync("api/providers/laCode/38/startYear/2018/endYear/2019/allocationLines/PES02/summary");

                if (result.IsSuccessStatusCode)
                {
                    string body = await result.Content.ReadAsStringAsync();

                    Console.WriteLine(FormatJson(body));
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

        private static string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
    }
}
