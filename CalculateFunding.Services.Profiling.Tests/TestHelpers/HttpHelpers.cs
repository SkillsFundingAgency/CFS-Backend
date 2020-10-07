//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Security.Authentication;
//using System.Threading;

//namespace CalculateFunding.Api.Profiling.Tests.TestHelpers
//{
//	public static class HttpHelpers
//    {
//        public static HttpClient GetAuthorizedClient()
//        {
//            HttpClient client = new HttpClient();
//            client.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", GetAuthenticationResult(trustedClient: true).AccessToken);

//            client.BaseAddress = new Uri(ConfigurationManager.AppSettings["ApsUrl"]);

//            return client;
//        }

//        public static HttpClient GetUnauthorizedClient()
//        {
//            return new HttpClient
//            {
//                BaseAddress = new Uri(ConfigurationManager.AppSettings["ApsUrl"])
//            };
//        }

//        public static HttpClient GetNonTrustedClient()
//        {
//            HttpClient client = new HttpClient();
//            client.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", GetAuthenticationResult(trustedClient: false).AccessToken);

//            client.BaseAddress = new Uri(ConfigurationManager.AppSettings["ApsUrl"]);

//            return client;
//        }

//        private static AuthenticationResult GetAuthenticationResult(bool trustedClient = true)
//        {
//            string aadInstance = ConfigurationManager.AppSettings["ida:AadInstance"];
//            string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
//            string clientId = trustedClient
//                ? ConfigurationManager.AppSettings["ida:ClientId"]
//                : ConfigurationManager.AppSettings["ida:NonTrustedClientId"];
//            string appKey = trustedClient
//                ? ConfigurationManager.AppSettings["ida:AppKey"]
//                : ConfigurationManager.AppSettings["ida:NonTrustedAppKey"];
//            string resourceId = ConfigurationManager.AppSettings["ida:ResourceId"];

//            string authority = string.Format(aadInstance, tenant);
//            AuthenticationContext authContext = new AuthenticationContext(authority);
//            ClientCredential clientCredential = new ClientCredential(clientId, appKey);

//            AuthenticationResult authResult = null;
//            int retryCount = 0;
//            bool retry;

//            do
//            {
//                retry = false;
//                try
//                {
//                    authResult = authContext.AcquireTokenAsync(resourceId, clientCredential).Result;
//                    return authResult;
//                }
//                catch (AdalException ex)
//                {
//                    if (ex.ErrorCode == "temporarily_unavailable")
//                    {
//                        retry = true;
//                        retryCount++;
//                        Thread.Sleep(3000);
//                    }
//                }
//            } while (retry && retryCount < 3);

//            if (authResult == null)
//            {
//                throw new AuthenticationException("Could not authenticate with the OAUTH2 claims provider after several attempts");
//            }

//            return authResult;
//        }
//    }
//}