using CalculateFunding.Models;
using CalculateFunding.Services.Core.Interfaces.Proxies.External;
using CalculateFunding.Services.Core.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Proxies.External
{
    public class AzureBearerTokenProxy : IAzureBearerTokenProxy
    {
        public async Task<AzureBearerToken> FetchToken(AzureBearerTokenOptions azureBearerTokenOptions)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                IDictionary<string, string> formValues = new Dictionary<string, string>();
                formValues.Add("client_id", azureBearerTokenOptions.ClientId);
                formValues.Add("grant_type", azureBearerTokenOptions.GrantType);
                formValues.Add("scope", azureBearerTokenOptions.Scope);
                formValues.Add("client_secret", azureBearerTokenOptions.ClientSecret);

                using (HttpResponseMessage response = await client.PostAsync(azureBearerTokenOptions.Url, new FormUrlEncodedContent(formValues)))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        JObject jsonresult = JObject.Parse(await response.Content.ReadAsStringAsync());
                        string accessToken = (string)jsonresult["access_token"];
                        int expiryLength = (int)jsonresult["expires_in"];

                        return new AzureBearerToken
                        {
                            AccessToken = accessToken,
                            ExpiryLength = expiryLength
                        };
                    }

                    return null;
                }
            }
        }
    }
}
