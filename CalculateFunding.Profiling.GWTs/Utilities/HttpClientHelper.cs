namespace CalculateFunding.Profiling.GWTs.Utilities
{
	using System;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Security.Authentication;
	using System.Threading;
	using Helpers;
	using Microsoft.IdentityModel.Clients.ActiveDirectory;
	using Options;

	public static class HttpClientHelper
    {
        public static HttpClient GetAuthorizedClient(string baseUrl)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", GetAuthenticationResult().AccessToken);

            return client;
        }

        private static AuthenticationResult GetAuthenticationResult()
        {
	        AzureAd azureAdInfo = ConfigHolder.GetAzureAdDto();

	        string aadInstance = azureAdInfo.AADInstance;
            string tenant = azureAdInfo.Tenant;
            string clientId = azureAdInfo.ClientId;
            string appKey = azureAdInfo.AppKey;
            string resourceId = azureAdInfo.ResourceId;

            string authority = string.Format(aadInstance, tenant);
            AuthenticationContext authContext = new AuthenticationContext(authority);
            ClientCredential clientCredential = new ClientCredential(clientId, appKey);

            AuthenticationResult authResult = null;
            int retryCount = 0;
            bool retry;

            do
            {
                retry = false;
                try
                {
                    authResult = authContext.AcquireTokenAsync(resourceId, clientCredential).Result;
                    return authResult;
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }
                }
            } while (retry && retryCount < 3);

            if (authResult == null)
            {
                throw new AuthenticationException("Could not authenticate with the OAUTH2 claims provider after several attempts");
            }

            return authResult;
        }
    }
}